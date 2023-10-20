using System.Diagnostics;

namespace SnDocumentGenerator
{
    public enum ProjectType { Unknown, NETStandard, NETCore, NETFramework }

    [DebuggerDisplay("{Name} ({TypeName})")]
    public class ProjectInfo
    {
        public string Path { get; set; }
        public string Name { get; set; }
        public ProjectType Type { get; set; }
        public string TypeName { get; set; }
        public bool IsTestProject { get; set; }
    }
}
