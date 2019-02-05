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
    }
}
