using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.WindowsAzure.Storage;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Threading.Tasks;

namespace DotNETDevOps.EmailTemplating
{
    public class EmailService
    {
        private readonly EmailTemplatingOptions emailTemplateOptions;
        private readonly SendgridConfiguration sendgridConfiguration;
        private readonly EmailContext emailContext;
        private readonly ILogger logger;
        private readonly IHostingEnvironment hostingEnvironment;

        public EmailService(IOptions<EmailTemplatingOptions> emailTemplateOptions, IOptions<SendgridConfiguration> sendgridConfiguration, EmailContext emailContext, ILogger<EmailService> logger, IHostingEnvironment hostingEnvironment)
        {
            this.emailTemplateOptions = emailTemplateOptions.Value ?? throw new ArgumentNullException(nameof(emailTemplateOptions));
            this.sendgridConfiguration = sendgridConfiguration.Value ?? throw new ArgumentNullException(nameof(sendgridConfiguration));
            this.emailContext = emailContext ?? throw new ArgumentNullException(nameof(emailContext));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.hostingEnvironment = hostingEnvironment ?? throw new ArgumentNullException(nameof(hostingEnvironment));
        }

        public async Task SendAsync(string emailId, string to, string subject, string msg, bool opentrack = false, bool clicktrack = false, IDictionary<string, string> unique_args = null, ILogger logger = null, string from = null, DateTimeOffset? time = null, params LinkedResource[] linkedResources)
        {
            logger = logger ?? this.logger;

            var entity = new EmailSendtModel { EmailId = emailId, ContentMD5 = msg.CreateMD5(), TargetMD5 = to.CreateMD5(), SubjectMD5 = subject.CreateMD5() };

            using (logger.BeginScope(new Dictionary<string, object>
            {
                ["emailId"] = entity.EmailId,
                ["ContentMD5"] = entity.ContentMD5,
                ["TargetMD5"] = entity.TargetMD5,
                ["SubjectMD5"] = entity.SubjectMD5,
            }))
            {
                if (!hostingEnvironment.IsProduction() &&
                    !(
                        to.Contains(emailTemplateOptions.BlockNoneProductionIfNotIncludes, StringComparison.OrdinalIgnoreCase) ||
                        to.Contains("mailinator.com", StringComparison.OrdinalIgnoreCase)
                     ))
                {
                    logger.LogInformation("Only production environment can send external emails, skipping");
                    return;
                }


                emailContext.Emails.Add(entity);
                try
                {
                    await emailContext.SaveChangesAsync();
                }
                catch (StorageException ex)
                {
                    if (ex.RequestInformation.ExtendedErrorInformation.ErrorCode == "EntityAlreadyExists")
                    {
                        logger.LogInformation("Email have been send in the past, skipping");
                        return;
                    }
                    throw;
                }
                catch (Exception ex)
                {
                    throw;

                }

                var emailArchive = emailContext.StorageAccount.CreateCloudBlobClient().GetContainerReference("emails");
                await emailArchive.CreateIfNotExistsAsync();
                var persisted = emailArchive.GetBlockBlobReference($"{emailId.CreateMD5()}.html");
                persisted.Metadata["emailid"] = emailId;
                persisted.Metadata["content"] = entity.ContentMD5;
                persisted.Metadata["subject"] = entity.SubjectMD5;
                persisted.Metadata["target"] = entity.TargetMD5;

                await persisted.UploadTextAsync(msg);

                unique_args = unique_args ?? new Dictionary<string, string>
                {

                };

                unique_args["email_id"] = emailId;

                var options = JToken.FromObject(new
                {
                    unique_args = unique_args,
                    filters = new
                    {
                        opentrack = new { settings = new { enable = opentrack ? 1 : 0 } },
                        clicktrack = new { settings = new { enable = clicktrack ? 1 : 0 } }
                    }
                });

                if (time.HasValue)
                {
                    options["send_at"] = time.Value.ToUnixTimeSeconds();
                }

                logger.LogInformation("Sending email '{subject}' to {entity} with {@options}", subject, entity.TargetMD5, options);


                MailMessage mailMessage = new MailMessage() { Subject = subject };


                foreach (var email in to.ToLower().Split(',', ' ').Select(s => s.Trim()))
                {
                    mailMessage.To.Add(new MailAddress(email));
                }

                mailMessage.From = new MailAddress(from ?? emailTemplateOptions.DefaultFromAddress ?? throw new ArgumentException(nameof(from)));


                if (!mailMessage.To.Any(m => m.Address == emailTemplateOptions.BccAddress))
                    mailMessage.Bcc.Add(emailTemplateOptions.BccAddress);

                var view = AlternateView.CreateAlternateViewFromString(msg, null, MediaTypeNames.Text.Html);
                foreach (var lr in linkedResources)
                    view.LinkedResources.Add(lr);

                mailMessage.AlternateViews.Add(view);

                mailMessage.Headers.Add("X-SMTPAPI", options.ToString());


                SmtpClient smtpClient = new SmtpClient("smtp.sendgrid.net", 587)
                {
                    Credentials = new NetworkCredential(sendgridConfiguration.Username, sendgridConfiguration.Password)
                };

                await smtpClient.SendMailAsync(mailMessage);

                logger.LogInformation("Sending email '{subject}' to {entity} completed", subject, entity.TargetMD5);
            }

        }
    }
}
