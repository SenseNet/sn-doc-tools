using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SnDocumentGenerator.Writers;

internal class SvcRegFrontendWriter : SvcRegWriter
{
    public override void WriteIndex(string title, List<ServiceRegistrationMethodInfo> srm, TextWriter output, Options options)
    {
        if (!srm.Any())
            return;

        output.WriteLine($"## {title} ({srm.Count})");

        //var ordered = srm
        //    .OrderBy(o => o.Category, new CategoryComparer())
        //    .ThenBy(o => o.ClassName);
        var ordered = srm
            .OrderBy(x => x.GithubRepository)
            .ThenBy(x => x.Project.Name)
            .ThenBy(x => x.ClassName)
            .ThenBy(x => x.MethodSignature);

        output.WriteLine("| Category | Project | Class | Method |");
        output.WriteLine("| -------- | ------- | ----- | ------ |");
        foreach (var reg in ordered)
        {
            output.WriteLine("| {0} | {1} | {2} | [{3}](/services/{0}/{4}) |",
                reg.GithubRepository,
                reg.Project.Name,
                reg.ClassName,
                reg.MethodSignature
                    .Replace("<", "&lt;")
                    .Replace(">", "&gt;"),
                reg.MethodSignatureInLink
                );
        }
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
        //UNDONE: Not implemented
    }
}