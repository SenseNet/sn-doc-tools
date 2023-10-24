using System.Diagnostics;

namespace SnDocumentGenerator
{
    [DebuggerDisplay("{Type} {Name}")]
    public class OperationParameterInfo
    {
        public string Type { get; set; }
        public string Name { get; set; }
        public bool IsOptional { get; set; }
        public string Documentation { get; set; }
        public string Example { get; set; }
    }
}
