using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SnDocumentGenerator.Writers;

internal class OptionsClassesBackendWriter : OptionsClassesWriter
{
    public override void WriteTable(string title, OptionsClassInfo[] ocs, TextWriter output, Options options)
    {
        if (!ocs.Any())
            return;

        output.WriteLine($"## {title} ({ocs.Length} classes)");

        var ordered = ocs.OrderBy(o => o.File).ThenBy(o => o.ClassName);
        output.WriteLine("| OptionClass | Category | Repository | Project | File | Directory |");
        output.WriteLine("| ----------- | -------- | ---------- | ------- | ---- | --------- |");
        foreach (var oc in ordered)
        {
            if (options.FileLevel == FileLevel.Category)
            {
                output.WriteLine("| [{0}](/options/{1}#{2}) | [{3}](/options/{1}) | {4} | {5} | {6} | {7} | ",
                    oc.ClassName,
                    oc.CategoryInLink,
                    oc.ClassNameInLink,
                    oc.Category,
                    oc.GithubRepository,
                    oc.ProjectName,
                    Path.GetFileName(oc.FileRelative),
                    Path.GetDirectoryName(oc.FileRelative));
            }
            else if (options.FileLevel == FileLevel.Operation)
            {
                output.WriteLine("| [{0}](/options/{1}/{2}) | {3} | {4} | {5} | {6} | {7} |",
                    oc.ClassName,
                    oc.CategoryInLink,
                    oc.ClassNameInLink,
                    oc.Category,
                    oc.GithubRepository,
                    oc.ProjectName,
                    Path.GetFileName(oc.FileRelative),
                    Path.GetDirectoryName(oc.FileRelative));
            }
            else if (options.FileLevel == FileLevel.OperationNoCategories)
            {
                output.WriteLine("| [{0}](/options/{1}) | {2} | {3} | {4} | {5} | {6} |",
                    oc.ClassName,
                    oc.ClassNameInLink,
                    oc.Category,
                    oc.GithubRepository,
                    oc.ProjectName,
                    Path.GetFileName(oc.FileRelative),
                    Path.GetDirectoryName(oc.FileRelative));
            }
            else
            {
                throw GetNotSupportedFileLevelException(options.FileLevel);
            }
        }
    }
    public override void WriteTree(string title, OptionsClassInfo[] ocs, TextWriter output, Options options)
    {
        output.WriteLine($"## {title} ({ocs.Length} classes)");
        output.WriteLine("### ... coming soon.");
    }
    public override void WriteConfigurationExamples(OptionsClassInfo[] ocs, TextWriter output)
    {
        output.WriteLine("### ... coming soon.");
    }
    public override void WriteOptionClass(OptionsClassInfo oc,
        IDictionary<string, ClassInfo> classes, IDictionary<string, EnumInfo> enums,
        TextWriter output, Options options)
    {
        output.WriteLine("## {0}", oc.ClassName);

        var head = new List<string>
            {
                $"- Repository: **{oc.GithubRepository}**",
                $"- Project: **{oc.ProjectName}**",
                $"- File: **{oc.FileRelative}**",
                $"- Class: **{oc.Namespace}.{oc.ClassName}**",
                $"- Section: **{oc.ConfigSection}**",
            };

        output.Write(string.Join(Environment.NewLine, head));
        output.WriteLine(".");

        output.WriteLine();
        if (!string.IsNullOrEmpty(oc.Documentation))
        {
            output.WriteLine(oc.Documentation);
        }

        output.WriteLine();

        output.WriteLine("### Properties:");
        foreach (var prop in oc.Properties)
        {
            var defaultValue = prop.Initializer == null
                ? ""
                : $"Default value: **{prop.Initializer.Replace("=", "").Trim()}**";
            output.WriteLine("- **{0}** ({1}): {2}. {3}", prop.Name, prop.Type.FormatType(),
                prop.Documentation.Trim('.'), defaultValue);
        }

        output.WriteLine();

        WriteOptionsExample(oc, classes, enums, output);

        WriteEnvironmentVariablesExample(oc, output);
    }
}