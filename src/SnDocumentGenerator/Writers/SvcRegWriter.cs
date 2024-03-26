using System.Collections.Generic;
using System.IO;

namespace SnDocumentGenerator.Writers;

internal abstract class SvcRegWriter : WriterBase
{
    public abstract void WriteIndex(string title, List<ServiceRegistrationMethodInfo> srm, TextWriter output, Options options);

    public abstract void WriteCheatSheet(string title, List<ServiceRegistrationMethodInfo> srm, TextWriter output, Options options);

    public abstract void WriteServiceRegistrations(List<ServiceRegistrationMethodInfo> serviceRegistrations,
        Dictionary<string, ClassInfo> classes, Dictionary<string, EnumInfo> enums,
        string outputDir, Options options);
}