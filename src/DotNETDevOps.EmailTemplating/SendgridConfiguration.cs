namespace DotNETDevOps.EmailTemplating
{
    public class SendgridConfiguration
    {    

        public string Username { get; set; }
        public string Password { get; set; }
       
        
    }
    public class EmailTemplatingOptions
    {
        public string DefaultFromAddress { get;  set; }
        public string BccAddress { get;  set; }
        public string BlockNoneProductionIfNotIncludes { get;  set; }
        public string Authority { get; set; }
        public string BaseAddress { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
    }
}
