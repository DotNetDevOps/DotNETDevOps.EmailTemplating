using IdentityModel.Client;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.WindowsAzure.Storage;
using SInnovations.Azure.TableStorageRepository.Queryable;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace DotNETDevOps.EmailTemplating.Razor
{
    public class EmailResource
    {
        public string Id { get; set; }
    }
    public class ViewEmailRequirement : IAuthorizationRequirement
    {

    }
    public class ViewEmailRequirementHandlerOptions
    {
        public string ClientId { get; set; }
        public string AllowedRoles { get; set; }
    }
    public class ViewEmailRequirementHandler : AuthorizationHandler<ViewEmailRequirement, EmailResource>
    {
        private readonly IOptions<ViewEmailRequirementHandlerOptions> options;

        public ViewEmailRequirementHandler(IOptions<ViewEmailRequirementHandlerOptions> options)
        {
            this.options = options ?? throw new ArgumentNullException(nameof(options));
        }

        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ViewEmailRequirement requirement, EmailResource resource)
        {
            if (context.User.FindFirstValue("email_id") == resource.Id)
                context.Succeed(requirement);

            if (context.User.FindFirstValue("client_id") == options.Value.ClientId && !context.User.HasClaim(c => c.Type == "sub"))
            {
                context.Succeed(requirement);
            }
             
            if( !string.IsNullOrEmpty(options.Value.AllowedRoles) && options.Value.AllowedRoles.Split(',').Any(role=> context.User.IsInRole(role))){
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }

    public class EmailPageModel : PageModel
    {
        protected readonly ILogger logger;
        private readonly EmailTemplatingOptions options;

        public EmailPageModel(ILogger logger, IOptions<EmailTemplatingOptions> options)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.options = options.Value ?? throw new ArgumentNullException(nameof(options));
        }
        public EmailTemplateModel EmailModel { get; set; }

        public override Task OnPageHandlerSelectionAsync(PageHandlerSelectedContext context)
        {

            return base.OnPageHandlerSelectionAsync(context);
        }
        public override async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
        {
           
           // var opidc = context.HttpContext.RequestServices.GetRequiredService<IOptions<OidcClientConfiguration>>();
           // var endpoints = context.HttpContext.RequestServices.GetRequiredService<IOptions<EndpointsOptions>>();
            var authorizationService = context.HttpContext.RequestServices.GetRequiredService<IAuthorizationService>();


            var emailId = context.RouteData.Values["emailId"] as string ?? context.HttpContext.Request.Query["emailId"];

            var emailresourceId = $"https://{new Uri(options.Authority).Host}/emails/{emailId}";

            var authorizationResult = await authorizationService
                .AuthorizeAsync(User, new EmailResource() { Id = emailresourceId }, new ViewEmailRequirement());

            if (!authorizationResult.Succeeded)
            {
                context.Result = this.Forbid();
                return;
            }


            var generate = context.HttpContext.Request.Query["generate"] == "true";
            if (!generate)
            {
                await context.HttpContext.AuthenticateAsync("Email");


                var emails = context.HttpContext.RequestServices.GetService<EmailContext>();

                var emailArchive = emails.StorageAccount.CreateCloudBlobClient().GetContainerReference("emails");
                var persisted = emailArchive.GetBlockBlobReference($"{emailId}.html");

                context.Result = this.Content(await persisted.DownloadTextAsync(), "text/html", Encoding.UTF8);

                return;


           //    EmailModel = await emails.EmailsTemplateModels.Where(k => k.EmailIdHash == emailId).FirstOrDefaultAsync();
            }
            else
            {

            }
            await base.OnPageHandlerExecutionAsync(context, next);
        }

        public async Task GenerateEmailModel(string emailId, Dictionary<string, string> tokenParameters, Dictionary<string, string> tags, bool regenerate = false)
        {
            var httpClientFactory = HttpContext.RequestServices.GetRequiredService<IHttpClientFactory>();
            var discoveryCache = HttpContext.RequestServices.GetRequiredService<IDiscoveryCache>();
           // var opidc = HttpContext.RequestServices.GetRequiredService<IOptions<OidcClientConfiguration>>();
           // var endpoints = HttpContext.RequestServices.GetRequiredService<IOptions<EndpointsOptions>>();
            var emails = HttpContext.RequestServices.GetService<EmailContext>();

            var http = httpClientFactory.CreateClient();
            var disco = await discoveryCache.GetAsync();

            var emailresourceId = $"https://{new Uri(options.Authority).Host}/emails/{emailId}";
            tokenParameters["emailId"] = emailresourceId;
            //  tokenParameters["scope"] = $"https://{new Uri(opidc.Value.Authority).Host}/identity";

            var t = await http.RequestTokenAsync(new TokenRequest
            {
                GrantType = "emailtemplate",
                Address = disco.TokenEndpoint,
                ClientId = options.ClientId,
                ClientSecret = options.ClientSecret,
                Parameters = tokenParameters
            });



            EmailModel = new EmailTemplateModel
            {
                EmailIdHash = emailId,
                EmailResourceId = emailresourceId,
                PermaLink = t.IsError ? null : $"{options.BaseAddress}{PageContext.ActionDescriptor.ViewEnginePath}?emailId={emailId}&access_token={t.AccessToken}",
                AccessToken = t.AccessToken,
                Tags = tags
            };

            try
            {

                emails.EmailsTemplateModels.Add(EmailModel);
                await emails.EmailsTemplateModels.SaveChangesAsync();
            }
            catch (StorageException ex)
            {
                if (ex.RequestInformation.ExtendedErrorInformation.ErrorCode == "EntityAlreadyExists")
                {
                    if (regenerate)
                    {
                        emails.EmailsTemplateModels.Update(EmailModel);
                        await emails.EmailsTemplateModels.SaveChangesAsync();
                    }
                    else
                    {
                        logger.LogInformation("Email template model have been created in the past, skipping");
                    }

                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

    }
}
