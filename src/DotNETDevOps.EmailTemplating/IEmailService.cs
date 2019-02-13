using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Mail;
using System.Threading.Tasks;

namespace DotNETDevOps.EmailTemplating
{
    public interface IEmailService
    {
        Task<bool> SendAsync(string emailId, string to, string subject, string msg, bool opentrack = false, bool clicktrack = false, IDictionary<string, string> unique_args = null, ILogger logger = null, string from = null, DateTimeOffset? time = null, params LinkedResource[] linkedResources);
    }
}
