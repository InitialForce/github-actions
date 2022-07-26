namespace InitialForce.GitHubActions.ReSharper.Report.Dtos
{
    public record IssueDto
    {
        public string Type { get; set; }
        public string FilePath { get; set; }
        public string Severity { get; set; }
        public string Message { get; set; }
        public int? Column { get; set; }
        public int? EndColumn { get; set; }
        public int? Line { get; set; }
        public string Project { get; set; }
    }
}
