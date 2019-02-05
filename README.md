# DotNETDevOps.EmailTemplating

[![Build status](https://dev.azure.com/dotnet-devops/dotnetdevops/_apis/build/status/DotNETDevOps.EmailTemplating)](https://dev.azure.com/dotnet-devops/dotnetdevops/_build/latest?definitionId=3)

Small abstraction used for email templating and ensuring that you only send the template/email once. Using table storage to block additional sends.

```cs
	public interface IEmailService
    {
        Task SendAsync(string emailId, string to, string subject, string msg, bool opentrack = false, bool clicktrack = false, IDictionary<string, string> unique_args = null, ILogger logger = null, string from = null, DateTimeOffset? time = null, params LinkedResource[] linkedResources);
    }
```

allowing you to create an emailId that will be skipped if trying to send more than once.

```cs
 var emailId = $"BUSINESS_USER_UPDATED_INTERNAL_NOTIFICATION_{subject}";
 ```

 The project is hardcoded against sendgrid for now.