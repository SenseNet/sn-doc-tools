using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SnDocumentGenerator.Writers;

internal class SvcRegBackendWriter : SvcRegWriter
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
            .OrderBy(x => x.MethodNamePostfix)
            .ThenBy(x => x.GithubRepository)
            .ThenBy(x => x.Project.Name)
            .ThenBy(x => x.ClassName)
            .ThenBy(x => x.MethodSignature);

        output.WriteLine("| Category | Repository | Project | Class | Method |");
        output.WriteLine("| -------- | ---------- | ------- | ----- | ------ |");
        foreach (var reg in ordered)
        {
            output.WriteLine("| {0} | {1} | {2} | {3} | [{4}](/services/{1}/{5}) |",
                reg.ExtensionTarget,
                reg.GithubRepository,
                reg.Project.Name,
                reg.ClassName,
                "**" + reg.MethodSignature
                    .Replace("<", "&lt;")
                    .Replace(">", "&gt;")
                    .Replace("(", "**("),
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
        var fileWriters = new Dictionary<string, TextWriter>();

        foreach (var reg in serviceRegistrations)
        {
            try
            {
                var categoryWriter = GetOrCreateWriter(serviceRegistrationsOutputDir, reg, fileWriters, options);
                WriteServiceRegistration(reg, categoryWriter, options);
            }
            catch// (Exception e)
            {
                //TODO: handle errors
            }
        }

        foreach (var fileWriter in fileWriters.Values)
        {
            fileWriter.Flush();
            fileWriter.Close();
        }
    }
    protected TextWriter GetOrCreateWriter(string outDir, ServiceRegistrationMethodInfo reg, Dictionary<string, TextWriter> writers, Options options)
    {
        var outFile = GetOutputFile(reg, options);
        if (!writers.TryGetValue(outFile, out var writer))
        {
            if (options.FileLevel == FileLevel.Operation)
            {
                var categoryPath = Path.Combine(outDir, reg.CategoryInLink);
                if (!Directory.Exists(categoryPath))
                    Directory.CreateDirectory(categoryPath);
            }
            writer = new StreamWriter(Path.Combine(outDir, outFile), false);
            writers.Add(outFile, writer);
            if (options.FileLevel == FileLevel.OperationNoCategories)
                WriteHead(reg.MethodSignature, writer);
            else
                WriteHead(reg.Category, writer);
        }

        return writer;
    }
    protected string GetOutputFile(ServiceRegistrationMethodInfo reg, Options options)
    {
        switch (options.FileLevel)
        {
            case FileLevel.Category:
                return $"{reg.CategoryInLink}.md";
            case FileLevel.Operation:
                return $"{reg.CategoryInLink}\\{reg.MethodSignatureInLink}.md";
            case FileLevel.OperationNoCategories:
                return $"{reg.MethodSignatureInLink}.md";
            default:
                throw GetNotSupportedFileLevelException(options.FileLevel);
        }
    }

    public void WriteServiceRegistration(ServiceRegistrationMethodInfo reg, TextWriter output, Options options)
    {
        output.WriteLine("## {0}", reg.MethodSignature.EscapeForMarkdown());

        var head = new List<string>
            {
                $"Extension method of `{reg.ExtensionTarget}`",
                $"- Repository: **{reg.GithubRepository}**",
                $"- Project: **{reg.Project.Name}**",
                $"- File: **{reg.FileRelative}**",
                $"- Class: **{reg.Namespace}.{reg.ClassName}**"
            };

        output.Write(string.Join(Environment.NewLine, head));
        output.WriteLine(".");

        output.WriteLine();
        if (!string.IsNullOrEmpty(reg.Documentation))
        {
            output.WriteLine(reg.Documentation);
        }
        output.WriteLine();

        if (reg.TypeParams.Length > 0)
        {
            output.WriteLine("### Type parameters:");
            foreach (var typeParam in reg.TypeParams)
            {
                output.WriteLine("- **{0}** ({1}): {2}",
                    typeParam.Name,
                    string.Join(", ", typeParam.Constraints),
                    typeParam.Documentation);
            }
        }

        if (reg.Parameters.Any(p => p.Type != "IServiceCollection") ||
            (reg.ReturnValue.Type != "void" && reg.ReturnValue.Type != "IServiceCollection"))
        {
            output.WriteLine("### Parameters:");
            var hasParameter = false;
            foreach (var prm in reg.Parameters)
            {
                if(!ServiceRegistrationMethodInfo.ExtensionTargets.Contains(prm.Type)) // hide fluent api input
                {
                    output.WriteLine("- **{0}** ({1}){2}: {3}", prm.Name, prm.Type.FormatType(),
                        prm.IsOptional ? " optional" : "", prm.Documentation);
                    hasParameter = true;
                }
            }
            if (reg.ReturnValue.Type != "void" && !ServiceRegistrationMethodInfo.ExtensionTargets.Contains(reg.ReturnValue.Type))
            {
                output.WriteLine("- **Return value** ({0}): {1}", reg.ReturnValue.Type.FormatType(),
                    reg.ReturnValue.Documentation);
                hasParameter = true;
            }
            if(!hasParameter)
                output.WriteLine("There are no parameters.");
        }


        output.WriteLine();
        if (0 < reg.Registrations.Length)
        {
            output.WriteLine("### Calls:");
            output.WriteLine("```csharp");
            foreach (var callingInfo in reg.Registrations)
            {
                output.WriteLine(callingInfo.ToString());
            }
            output.WriteLine("```");
        }
        if (0 < reg.CalledBy.Count)
        {
            output.WriteLine("### Called by:");
            output.WriteLine("```csharp");
            foreach (var callerInfo in reg.CalledBy)
            {
                output.WriteLine(callerInfo.MethodSignature);
            }
            output.WriteLine("```");
        }

        output.WriteLine();
    }

}