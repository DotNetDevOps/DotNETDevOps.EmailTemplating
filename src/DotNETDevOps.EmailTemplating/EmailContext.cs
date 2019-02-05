using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using SInnovations.Azure.TableStorageRepository;
using SInnovations.Azure.TableStorageRepository.TableRepositories;

namespace DotNETDevOps.EmailTemplating
{
    public class EmailContext : TableStorageContext
    {
        public EmailContext(
           ILoggerFactory logFactory,
           IEntityTypeConfigurationsContainer container,
           CloudStorageAccount storage) : base(logFactory, container, storage)
        {
            this.InsertionMode = InsertionMode.Add;

        }
        protected override void OnModelCreating(TableStorageModelBuilder modelbuilder)
        {
            modelbuilder.Entity<EmailSendtModel>()
              .HasKeys(k => new { k.EmailId })
              .WithKeyPropertyTransformation(k => k.EmailId, s => s.CreateMD5())
              .ToTable("emails");
            modelbuilder.Entity<EmailTemplateModel>()
             .HasKeys(k => new { k.EmailIdHash })
             .WithKeyPropertyTransformation(k => k.EmailIdHash, s => $"template__{s}")
             .WithPropertyOf(k => k.Tags)
             .ToTable("emails");

            base.OnModelCreating(modelbuilder);
        }

        public ITableRepository<EmailSendtModel> Emails { get; set; }
        public ITableRepository<EmailTemplateModel> EmailsTemplateModels { get; set; }
    }
}
