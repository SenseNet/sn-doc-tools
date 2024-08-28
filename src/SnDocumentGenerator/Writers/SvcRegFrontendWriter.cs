using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SnDocumentGenerator.Writers;

internal class SvcRegFrontendWriter : SvcRegWriter
{
    public override void WriteIndex(string title, List<ServiceRegistrationMethodInfo> srm, TextWriter output, Options options)
    {
        // Frontend does not support any service registrations
    }

    public override void WriteCheatSheet(string title, List<ServiceRegistrationMethodInfo> srm, TextWriter output, Options options)
    {
        output.WriteLine($"## {title} ({srm.Count} methods)");
        output.WriteLine("### ... coming soon.");
    }

    public override void WriteServiceRegistrations(List<ServiceRegistrationMethodInfo> serviceRegistrations,
        Dictionary<string, ClassInfo> classes, Dictionary<string, EnumInfo> enums,
        string serviceRegistrationsOutputDir, Options options)
    {
        // Frontend does not support any service registrations
    }
}