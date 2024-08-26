using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace SnDocumentGenerator.Writers;

internal class OptionsClassesFrontendWriter : OptionsClassesWriter
{
    public override void WriteIndex(string title, OptionsClassInfo[] ocs, TextWriter output, Options options)
    {
        if (!ocs.Any())
            return;

        output.WriteLine($"## {title} ({ocs.Length} sections)");

        var ordered = ocs
            .OrderBy(o => o.Category, new CategoryComparer())
            .ThenBy(o => o.ClassName);
        output.WriteLine("| ClassName | Application | Section |");
        output.WriteLine("| --------- | ----------- | ------- |");

        foreach (Occ enumValue in Enum.GetValues<Occ>())
        {
            var classNamesByCategory = base.OptionClassesInCategories
                .Where(x => x.Value.Contains(enumValue))
                .Select(x => x.Key)
                .OrderBy(x => x)
                .ToArray();

            foreach (var className in classNamesByCategory)
            {
                var oc = ocs.FirstOrDefault(x => x.ClassName == className);
                if (oc == null)
                    continue;
                output.WriteLine("| [{0}](/configuration/{1}/{2}) | {3} | {4} |",
                    oc.ClassName,
                    base.OptionsClassCategoryNames[(int)enumValue],
                    oc.ClassNameInLink,
                    enumValue,
                    oc.ConfigSection);
            }
        }
    }
    public override void WriteCheatSheet(string title, OptionsClassInfo[] ocs, TextWriter output, Options options)
    {
        if (!ocs.Any())
            return;
        var examples = CreateOptionsExample(ocs, true);

        output.WriteLine($"## {title} ({ocs.Length} sections)");
        output.WriteLine("This article contains configuration examples, grouped by github repositories. " +
                         "Some of these can be combined into a single configuration file, " +
                         "but this is determined by the application.");
        output.WriteLine();
        output.WriteLine("**WARNING** These are sample configurations containing example values. " +
                         "Do not use it without modifying it to reflect your environment.");
        foreach (var item in examples)
        {
            output.WriteLine($"## {item.Key}");
            var json = JsonSerializer.Serialize(item.Value, new JsonSerializerOptions { WriteIndented = true });
            output.WriteLine("``` json");
            output.WriteLine(json);
            output.WriteLine("```");
        }
    }
    public override void WriteConfigurationExamples(OptionsClassInfo[] ocs, TextWriter output)
    {
        if (!ocs.Any())
            return;
        var examples = CreateOptionsExample(ocs, false);

        //output.WriteLine($"## {title} ({ocs.Length} sections)");
        output.WriteLine("## Configuration example");
        output.WriteLine();
        output.WriteLine("**WARNING** This is a sample configuration containing example values. " +
                         "Do not use it without modifying it to reflect your environment.");
        foreach (var item in examples)
        {
            var json = JsonSerializer.Serialize(item.Value, new JsonSerializerOptions { WriteIndented = true });
            output.WriteLine("``` json");
            output.WriteLine(json);
            output.WriteLine("```");
        }
    }
    public override void WriteOptionClass(OptionsClassInfo oc, IDictionary<string, ClassInfo> classes, IDictionary<string, EnumInfo> enums, TextWriter output, Options options)
    {
        output.WriteLine("## {0}", oc.ClassName);

        output.WriteLine();
        if (!string.IsNullOrEmpty(oc.Documentation))
        {
            output.WriteLine(oc.Documentation);
        }
        output.WriteLine();

        WriteOptionsExample(oc, classes, enums, output);

        WriteEnvironmentVariablesExample(oc, output);

        output.WriteLine("### Properties:");
        foreach (var prop in oc.Properties.Where(x => !x.TypeIsBackendOnly))
            output.WriteLine("- **{0}** ({1}): {2}", prop.Name, GetFrontendType(prop.Type), prop.Documentation);

        output.WriteLine();
    }
}