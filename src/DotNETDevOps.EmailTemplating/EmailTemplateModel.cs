using System.Collections.Generic;

namespace DotNETDevOps.EmailTemplating
{
    public class EmailTemplateModel
    {
        public string EmailIdHash { get; set; }
        public string EmailResourceId { get; set; }
        public string PermaLink { get; set; }
        public string AccessToken { get; set; }
        public Dictionary<string, string> Tags { get; set; } = new Dictionary<string, string>();
    }
}
