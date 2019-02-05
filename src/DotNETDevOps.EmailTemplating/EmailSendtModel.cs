namespace DotNETDevOps.EmailTemplating
{
    public class EmailSendtModel
    {
        public string EmailId { get; set; }
        public string ContentMD5 { get; set; }
        public string SubjectMD5 { get; set; }
        public string TargetMD5 { get; set; }
        public string Version { get; set; } = "1.0";
    }
}
