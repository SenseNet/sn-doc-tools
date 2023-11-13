namespace SnDocumentGenerator;

public class EnumInfo
{
    public ProjectInfo Project { get; set; }
    public string File { get; set; }
    public string Namespace { get; set; }
    public string Name { get; set; }
    public string NameInLink => Name;
    public string Category { get; set; }
    public string CategoryInLink { get; set; }
    public string ProjectName => Project?.Name ?? "";

    public string[] Members { get; set; }
}