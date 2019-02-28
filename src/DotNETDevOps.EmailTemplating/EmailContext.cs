using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using SInnovations.Azure.TableStorageRepository;
using SInnovations.Azure.TableStorageRepository.TableRepositories;
using System;
using System.Globalization;

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
              .WithKeyTransformation(k => k.EmailId, s => s.CreateMD5())
              .ToTable("emails");
            modelbuilder.Entity<EmailTemplateModel>()
             .HasKeys(k => new { k.EmailIdHash })
             .WithKeyTransformation(k => k.EmailIdHash, s => $"template__{s}")
             .WithPropertyOf(k => k.Tags)
             .ToTable("emails");

            base.OnModelCreating(modelbuilder);
        }

        public ITableRepository<EmailSendtModel> Emails { get; set; }
        public ITableRepository<EmailTemplateModel> EmailsTemplateModels { get; set; }
    }

    internal static class DatetimeExtensions
    {
        public static DateTimeOffset Trim(this DateTimeOffset date, long roundTicks)
        {
            return new DateTimeOffset(date.Ticks - date.Ticks % roundTicks, date.Offset);
        }
        public static string TrimToDayReversedDateTime(this DateTimeOffset time)
        {
            if (time == DateTimeOffset.MaxValue)
                return "";

            var inverseTimeKey = DateTimeOffset.MaxValue.Subtract(time.Trim(TimeSpan.TicksPerDay)).Ticks;

            return inverseTimeKey.ToString("d19", CultureInfo.InvariantCulture);

        }
    }
}
