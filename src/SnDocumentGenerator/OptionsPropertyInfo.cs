using System;
using System.Collections.Generic;
using System.Text;

namespace SnDocumentGenerator
{
    public class OptionsPropertyInfo
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public bool HasGetter { get; set; }
        public bool HasSetter { get; set; }
        public string Initializer { get; set; }
        public string Documentation { get; set; }
    }
}
