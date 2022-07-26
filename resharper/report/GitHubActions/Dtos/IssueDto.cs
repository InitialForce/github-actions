namespace InitialForce.GitHubActions.ReSharper.Report.Dtos
{
    public record IssueDto
    {
        public string Type { get; set; } = null!;
        public string FilePath { get; set; } = null!;
        public string Severity { get; set; } = null!;
        public string Message { get; set; } = null!;
        public int? Column { get; set; }
        public int? EndColumn { get; set; }
        public int? Line { get; set; }
        public string Project { get; set; } = null!;
    }
}
