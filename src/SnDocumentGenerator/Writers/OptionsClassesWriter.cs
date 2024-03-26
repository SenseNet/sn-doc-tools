using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SnDocumentGenerator.Writers;

internal abstract class OptionsClassesWriter : WriterBase
{
    public abstract void WriteIndex(string title, OptionsClassInfo[] ocs, TextWriter output, Options options);

    public abstract void WriteCheatSheet(string title, OptionsClassInfo[] ocs, TextWriter output, Options options);

    public abstract void WriteConfigurationExamples(OptionsClassInfo[] ocs, TextWriter output);

    public abstract void WriteOptionClass(OptionsClassInfo op, IDictionary<string, ClassInfo> classes,
        IDictionary<string, EnumInfo> enums, TextWriter output, Options options);

    /* ============================================================================ */

    public void WriteOptionClasses(OptionsClassInfo[] optionClasses,
        IDictionary<string, ClassInfo> classes, IDictionary<string, EnumInfo> enums,
        string outputDir, Options options)
    {
        var fileWriters = new Dictionary<string, TextWriter>();

        foreach (var oc in optionClasses)
        {
            try
            {
                var writers = GetOrCreateWriters(outputDir, oc, optionClasses, fileWriters, options);
                foreach (var writer in writers)
                    WriteOptionClass(oc, classes, enums, writer, options);
            }
            catch// (Exception e)
            {
                //TODO: handle errors
                throw;
            }
        }

        foreach (var fileWriter in fileWriters.Values)
        {
            fileWriter.Flush();
            fileWriter.Close();
        }
    }
    protected TextWriter[] GetOrCreateWriters(string outDir, OptionsClassInfo oc, OptionsClassInfo[] optionClasses, Dictionary<string, TextWriter> writers, Options options)
    {
        var outFiles = GetOutputFiles(oc, outDir, optionClasses, options);
        var result = new List<TextWriter>();
        foreach (var outFile in outFiles)
        {
            if (!writers.TryGetValue(outFile, out var writer))
            {
                if (options.FileLevel == FileLevel.Operation)
                {
                    var categoryPath = Path.Combine(outDir, oc.CategoryInLink);
                    if (!Directory.Exists(categoryPath))
                        Directory.CreateDirectory(categoryPath);
                }
                writer = new StreamWriter(Path.Combine(outDir, outFile), false);
                writers.Add(outFile, writer);
                result.Add(writer);
                if (options.FileLevel == FileLevel.OperationNoCategories)
                    WriteHead(oc.ClassName, writer);
                else
                    WriteHead(oc.Category, writer);
            }

        }
        return result.ToArray();
    }
    protected string[] GetOutputFiles(OptionsClassInfo oc, string outDir, OptionsClassInfo[] optionClasses, Options options)
    {
        //switch (options.FileLevel)
        //{
        //    case FileLevel.Category:
        //        return $"{oc.CategoryInLink}.md";
        //    case FileLevel.Operation:
        //        return $"{oc.CategoryInLink}\\{oc.ClassNameInLink}.md";
        //    case FileLevel.OperationNoCategories:
        //        return $"{oc.ClassNameInLink}.md";
        //    default:
        //        throw GetNotSupportedFileLevelException(options.FileLevel);
        //}
        var categories = GetOptionsClassCategories(oc);
        EnsureOptionClassCategories(categories, outDir, optionClasses, options);
        var names = categories.Select(c => $"{OptionsClassCategoryNames[(int)c]}\\{oc.ClassNameInLink}.md").ToArray();
        return names;
    }
    private void EnsureOptionClassCategories(Occ[] categories, string outDir, OptionsClassInfo[] optionClasses, Options options)
    {
        foreach (var category in categories)
        {
            var categoryName = OptionsClassCategoryNames[(int)category];
            var directory = Path.Combine(outDir, categoryName);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                CreateCategoryFile(category, directory, optionClasses, options);
            }
        }
    }
    private void CreateCategoryFile(Occ category, string directory, OptionsClassInfo[] optionClasses, Options options)
    {
        var fileName = directory + ".md";
        using var writer = new StreamWriter(fileName, false);
        var text = OptionClassCategoryFiles[category];
        writer.WriteLine(text);

        var optionClassesInCategory = optionClasses
            .Where(x => OptionClassesInCategories[x.ClassName].Contains(category)).ToArray();

        WriteConfigurationExamples(optionClassesInCategory, writer);
    }

    protected enum Occ { SenseNet, PreviewGenerator, IdentityServer, SnIO, TaskManagement, SearchService }
    protected readonly string[] OptionsClassCategoryNames =
    {
            "sensenet", "previewgenerator", "identityserver", "sn-io", "taskmanagement", "searchservice"
        };
    protected readonly Dictionary<Occ, string> OptionClassCategoryFiles = new()
        {
            {Occ.SenseNet, @"---
title: ""Main sensenet service""
metaTitle: ""sensenet - Configuring the main sensenet service""
metaDescription: ""Configuring the main sensenet instance""
---

This section contains configuration for the main sensenet service.
"},
            {Occ.PreviewGenerator, @"---
title: ""Preview Generator""
metaTitle: ""sensenet - Configuring the Preview Generator""
metaDescription: ""Configuring the Preview Generator""
---

This section contains configuration for the sensenet Preview Generator.
"},
            {Occ.IdentityServer, @"---
title: ""IdentityServer""
metaTitle: ""sensenet - Configuring the IdentityServer authentication service""
metaDescription: ""Configuring the IdentityServer authentication service""
---

This section contains configuration for the sensenet authentication service.
"},
            {Occ.SnIO, @"---
title: ""Import/export tool""
metaTitle: ""sensenet - Configuring the import/export tool""
metaDescription: ""Configuring the import/export tool""
---

This section contains configuration for the import/export tool.
"},
            {Occ.TaskManagement, @"---
title: ""TaskManagement""
metaTitle: ""sensenet - Configuring sensenet TaskManagement""
metaDescription: ""Configuring sensenet TaskManagement""
---

This section contains configuration for sensenet TaskManagement.
"},
            {Occ.SearchService, @"---
title: ""SearchService""
metaTitle: ""sensenet - Configuring sensenet SearchService""
metaDescription: ""Configuring sensenet SearchService""
---

This section contains configuration for sensenet SearchService.
"}

        };
    protected readonly Dictionary<string, Occ[]> OptionClassesInCategories = new()
        {
            {"AsposeOptions", new[] {Occ.SenseNet, Occ.PreviewGenerator}},
            {"AsposePreviewGeneratorOptions", new[] {Occ.PreviewGenerator}},

            {"AuthenticationOptions", new[] {Occ.SenseNet}},
            {"BlobStorageOptions", new[] {Occ.SenseNet}},
            {"ClientRequestOptions", new[] {Occ.SenseNet}},
            {"ClientStoreOptions", new[] {Occ.SenseNet}},
            {"CryptographyOptions", new[] {Occ.SenseNet}},
            {"DataOptions", new[] {Occ.SenseNet}},
            {"EmailOptions", new[] {Occ.SenseNet}},
            {"ExclusiveLockOptions", new[] {Occ.SenseNet}},
            {"GrpcClientOptions", new[] {Occ.SenseNet, Occ.SearchService}},
            {"HttpRequestOptions", new[] {Occ.SenseNet}},
            {"IndexingOptions", new[] {Occ.SenseNet}},
            {"MessagingOptions", new[] {Occ.SenseNet, Occ.SearchService}},
            {"MsSqlDatabaseInstallationOptions", new[] {Occ.SenseNet}},
            {"MultiFactorOptions", new[] {Occ.SenseNet}},
            {"RabbitMqOptions", new[] {Occ.SenseNet, Occ.SearchService}},
            {"RegistrationOptions", new[] {Occ.SenseNet}},
            {"RetrierOptions", new[] {Occ.SenseNet}},
            {"StatisticsOptions", new[] {Occ.SenseNet}},

            {"CentralizedOptions", new[] {Occ.SearchService}},

            {"DisplaySettings", new[] {Occ.SnIO}},
            {"FsReaderArgs", new[] {Occ.SnIO}},
            {"FsWriterArgs", new[] {Occ.SnIO}},
            {"RepositoryReaderArgs", new[] {Occ.SnIO}},
            {"RepositoryWriterArgs", new[] {Occ.SnIO}},

            {"TaskManagementOptions", new[] {Occ.TaskManagement}},
            {"TaskManagementWebOptions", new[] {Occ.TaskManagement}},

            {"NotificationOptions", new[] {Occ.IdentityServer}},
            {"RecaptchaOptions", new[] {Occ.IdentityServer}},
            {"LoginPageOptions", new[] {Occ.IdentityServer}},

            {"SemanticKernelOptions", new[] {Occ.SenseNet}},
            {"AzureVisionOptions", new[] {Occ.SenseNet}},
        };
    private Occ[] GetOptionsClassCategories(OptionsClassInfo oc)
    {
        if (OptionClassesInCategories.TryGetValue(oc.ClassName, out var categories))
            return categories;
        throw new Exception($"Options class '{oc.ClassName}' is not categorized.");
    }

    protected readonly char[] ConfigSectionSplitChars = ":/.".ToCharArray();

    protected void WriteOptionsExample(OptionsClassInfo oc,
        IDictionary<string, ClassInfo> classes, IDictionary<string, EnumInfo> enums, TextWriter output)
    {
        var exampleObject = BuildExampleObject(oc, classes, enums);

        var root = new Dictionary<string, object>();
        var names = oc.ConfigSection.Split(ConfigSectionSplitChars, StringSplitOptions.RemoveEmptyEntries);
        var currentLevel = root;
        for (int i = 0; i < names.Length - 1; i++)
        {
            var newLevel = new Dictionary<string, object>();
            currentLevel.Add(names[i], newLevel);
            currentLevel = newLevel;
        }
        currentLevel.Add(names.Last(), exampleObject);

        output.WriteLine("### Configuration example:");
        output.WriteLine("``` json");
        output.WriteLine(JsonConvert.SerializeObject(root, Formatting.Indented));
        output.WriteLine("```");
    }
    private object BuildExampleObject(ClassInfo oc, IDictionary<string, ClassInfo> classes, IDictionary<string, EnumInfo> enums)
    {
        var result = new Dictionary<string, object>();
        foreach (var property in oc.Properties.Where(x => !x.TypeIsBackendOnly))
            result.Add(property.Name, GetPropertyExample(oc, property, classes, enums));
        return result;
    }
    private object GetPropertyExample(ClassInfo @class, OptionsPropertyInfo property,
        IDictionary<string, ClassInfo> classes, IDictionary<string, EnumInfo> enums)
    {
        if (property.TypeIsEnum)
            return GetEnumMembers(@class, property, classes, enums) ?? $"\"_enum_value_of_{property.TypeFullName}_\"";

        var type = OperationFrontendWriter.GetJsonType(property.Type);
        if (type == "string") return "_value_";
        if (type == "string[]") return new[] { "_value_" };
        if (type == "bool") return true;
        if (type == "bool?") return true;
        if (type == "DateTime") return new DateTime(2013, 11, 14);
        if (type == "int") return 0;
        if (type == "int?") return 0;
        if (type == "long") return 0;
        if (type == "float") return 0.0f;
        if (type == "double") return 0.0d;

        var nestedObject = GetNestedObject(@class, property, classes);

        if (type.EndsWith("[]"))
        {
            if (nestedObject == null)
                return new[] { new object() };
            return new[] { BuildExampleObject(nestedObject, classes, enums) };
        }

        if (nestedObject == null)
            return new object();
        return BuildExampleObject(nestedObject, classes, enums);
    }

    private string GetEnumMembers(ClassInfo @class, OptionsPropertyInfo property,
        IDictionary<string, ClassInfo> classes, IDictionary<string, EnumInfo> enums)
    {
        if (!enums.TryGetValue(property.TypeFullName, out var enumInfo))
            return null;
        return string.Join(" | ", enumInfo.Members);
    }

    private ClassInfo GetNestedObject(ClassInfo @class, OptionsPropertyInfo property, IDictionary<string, ClassInfo> classes)
    {
        var typeFullName = property.TypeFullName;
        if (typeFullName.Contains(".IEnumerable<") ||
            typeFullName.Contains(".ICollection<") ||
            typeFullName.Contains(".ODataArray<"))
        {
            var p = typeFullName.IndexOf('<');
            typeFullName = typeFullName.Substring(p + 1).TrimEnd('>');
        }

        if (typeFullName.Contains('.'))
        {
            if (classes.TryGetValue(typeFullName, out var classInfo))
                return classInfo;
            return null;
        }

        foreach (var @namespace in @class.UsingDirectives.Union(new[] { @class.Namespace }))
            if (classes.TryGetValue($"{@namespace}.{property.Type}", out var classInfo))
                return classInfo;

        return null;
    }

    /*
    private void WriteSection(string[] sections, int sectionIndex, string indent, List<OptionsPropertyInfo> properties, TextWriter output)
    {
        if (sectionIndex >= sections.Length)
        {
            WriteProperties(indent, properties, output);
            return;
        }
        output.WriteLine($"{indent}\"{sections[sectionIndex]}\": {{");
        WriteSection(sections, sectionIndex + 1, indent + "  ", properties, output);
        output.WriteLine($"{indent}}}");
    }
    private void WriteProperties(string indent, List<OptionsPropertyInfo> properties, TextWriter output)
    {
        var index = 0;
        foreach (var property in properties)
        {
            if (property.Type.StartsWith("Func<"))
                continue;

            output.WriteLine(
                $"{indent}\"{property.Name}\": {GetPropertyExample(property)}{(index++ < properties.Count - 1 ? "," : "")}");
        }
    }
    protected string GetPropertyExample(OptionsPropertyInfo property)
    {
        if (property.TypeIsEnum) return $"\"_enum_value_of_{property.TypeFullName}_\"";

        var type = OperationFrontendWriter.GetJsonType(property.Type);
        if (type == "string") return "\"_value_\"";
        if (type == "string[]") return "[\"_value1_\", \"_value2_\"]";
        if (type == "bool") return "true";
        if (type == "bool?") return "true";
        if (type == "DateTime") return "2010-04-21";
        if (type == "int") return "0";
        if (type == "int?") return "0";
        if (type == "long") return "0";
        if (type == "float") return "0.0";
        if (type == "double") return "0.0";

        if (type.EndsWith("[]")) return "[{ }, { }]";

        return "{ }";
    }
    */

    protected void WriteEnvironmentVariablesExample(OptionsClassInfo oc, TextWriter output)
    {
        output.WriteLine("### Environment variables example:");
        output.WriteLine("```");
        var prefix = oc.ConfigSection.Replace(":", "__");
        foreach (var prop in oc.Properties.Where(x => !x.TypeIsBackendOnly))
            output.WriteLine($"{prefix}__{prop.Name}=\"_{prop.Type}_value_\"");
        output.WriteLine("```");
    }




    protected Dictionary<string, Dictionary<string, object>> CreateOptionsExample(OptionsClassInfo[] ocs, bool groupByGithubRepositories)
    {
        var result = new Dictionary<string, Dictionary<string, object>>();
        foreach (var oc in ocs)
        {
            var key = groupByGithubRepositories ? oc.GithubRepository : "all";
            if (!result.TryGetValue(key, out var currentLevel))
            {
                currentLevel = new Dictionary<string, object>();
                result.Add(key, currentLevel);
            }

            var path = oc.ConfigSection.Split(':');
            foreach (var segment in path)
            {
                if (!currentLevel.TryGetValue(segment, out var level))
                {
                    level = new Dictionary<string, object>();
                    currentLevel.Add(segment, level);
                }
                currentLevel = (Dictionary<string, object>)level;
            }
            foreach (var property in oc.Properties.Where(x => !x.TypeIsBackendOnly))
            {
                currentLevel[property.Name] = GetPropertyExampleByType(property);
            }
        }

        return result;
    }
    private object GetPropertyExampleByType(OptionsPropertyInfo property)
    {
        if (property.TypeIsEnum)
            return $"_enum_value_of_{property.TypeFullName}_";

        switch (property.Type)
        {
            case "bool": return true;
            case "bool?": return true;
            case "int": return 0;
            case "int?": return 0;
            case "long": return 0;
            case "float": return 0.1;
            case "double": return 0.1;
            case "DateTime": return new DateTime(2023, 10, 19, 9, 45, 18);
            case "string": return "_stringValue_";
            case "string[]": return new[] { "_value1_", "_value2_" };
        }

        if (property.Type.EndsWith("[]"))
            return new[] { new object(), new object() };

        return new object();
    }

}