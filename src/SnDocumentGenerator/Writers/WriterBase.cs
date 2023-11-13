using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using static System.Collections.Specialized.BitVector32;

namespace SnDocumentGenerator.Writers
{
    /// <summary>Defines constants for file level</summary>
    public enum FileLevel
    {
        /// <summary>One category per file</summary>
        Category,
        /// <summary>One operation per file. Categories are directories.</summary>
        Operation,
        /// <summary>One operation per file. Everything is in one directory.</summary>
        OperationNoCategories
    }

    internal abstract class WriterBase
    {
        // ReSharper disable once InconsistentNaming
        protected static readonly string CR = Environment.NewLine;

        public abstract void WriteTable(string title, OperationInfo[] ops, TextWriter output, Options options);
        public abstract void WriteTable(string title, OptionsClassInfo[] ocs, TextWriter output, Options options);
        public abstract void WriteTree(string title, OperationInfo[] ops, TextWriter output, Options options);
        public abstract void WriteTree(string title, OptionsClassInfo[] ocs, TextWriter output, Options options);

        public abstract void WriteOperation(OperationInfo op, TextWriter output, Options options);
        public abstract void WriteOptionClass(OptionsClassInfo op, IDictionary<string, ClassInfo> classes, TextWriter output, Options options);

        public virtual void WriteAttribute(string name, List<string> values, string prefix, TextWriter output)
        {
            if (values.Count == 0)
                return;
            values = values.Select(x => x.Replace(prefix, string.Empty)).ToList();
            output.WriteLine("- **{0}**: {1}", name, string.Join(", ", values));
        }

        public void WriteOperations(IEnumerable<OperationInfo> operations, string outputDir, Options options)
        {
            var fileWriters = new Dictionary<string, TextWriter>();

            foreach (var op in operations)
            {
                try
                {
                    var categoryWriter = GetOrCreateWriter(outputDir, op, fileWriters, options);
                    WriteOperation(op, categoryWriter, options);
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
        protected TextWriter GetOrCreateWriter(string outDir, OperationInfo op, Dictionary<string, TextWriter> writers, Options options)
        {
            var outFile = GetOutputFile(op, options);
            if (!writers.TryGetValue(outFile, out var writer))
            {
                if (options.FileLevel == FileLevel.Operation)
                {
                    var categoryPath = Path.Combine(outDir, op.CategoryInLink);
                    if (!Directory.Exists(categoryPath))
                        Directory.CreateDirectory(categoryPath);
                }
                writer = new StreamWriter(Path.Combine(outDir, outFile), false);
                writers.Add(outFile, writer);
                if(options.FileLevel == FileLevel.OperationNoCategories)
                    WriteHead(op.OperationName, writer);
                else
                    WriteHead(op.Category, writer);
            }

            return writer;
        }
        protected string GetOutputFile(OperationInfo op, Options options)
        {
            switch (options.FileLevel)
            {
                case FileLevel.Category:
                    return $"{op.CategoryInLink}.md";
                case FileLevel.Operation:
                    return $"{op.CategoryInLink}\\{op.OperationNameInLink}.md";
                case FileLevel.OperationNoCategories:
                    return $"{op.OperationNameInLink}.md";
                default:
                    throw GetNotSupportedFileLevelException(options.FileLevel);
            }
        }

        public void WriteOptionClasses(IEnumerable<OptionsClassInfo> optionClasses, IDictionary<string, ClassInfo> classes, string outputDir, Options options)
        {
            var fileWriters = new Dictionary<string, TextWriter>();

            foreach (var oc in optionClasses)
            {
                try
                {
                    var categoryWriter = GetOrCreateWriter(outputDir, oc, fileWriters, options);
                    WriteOptionClass(oc, classes, categoryWriter, options);
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
        protected TextWriter GetOrCreateWriter(string outDir, OptionsClassInfo oc, Dictionary<string, TextWriter> writers, Options options)
        {
            var outFile = GetOutputFile(oc, options);
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
                if(options.FileLevel == FileLevel.OperationNoCategories)
                    WriteHead(oc.ClassName, writer);
                else
                    WriteHead(oc.Category, writer);
            }

            return writer;
        }
        protected string GetOutputFile(OptionsClassInfo oc, Options options)
        {
            switch (options.FileLevel)
            {
                case FileLevel.Category:
                    return $"{oc.CategoryInLink}.md";
                case FileLevel.Operation:
                    return $"{oc.CategoryInLink}\\{oc.ClassNameInLink}.md";
                case FileLevel.OperationNoCategories:
                    return $"{oc.ClassNameInLink}.md";
                default:
                    throw GetNotSupportedFileLevelException(options.FileLevel);
            }
        }


        public void WriteHead(string title, TextWriter writer)
        {
            writer.WriteLine("---");
            writer.WriteLine($"title: {title}");
            writer.WriteLine($"metaTitle: \"sensenet API - {title}\"");
            writer.WriteLine($"metaDescription: \"{title}\"");
            writer.WriteLine("---");
            writer.WriteLine();
        }

        protected Exception GetNotSupportedFileLevelException(FileLevel fileLevel)
        {
            return new NotSupportedException($"FileLevel.{fileLevel} is not supported.");
        }


        protected void WriteOptionsExample(OptionsClassInfo oc, IDictionary<string, ClassInfo> classes, TextWriter output)
        {
            //output.WriteLine("### Configuration example:");
            //output.WriteLine("``` json");
            //output.WriteLine("{");
            //WriteSection(oc.ConfigSection.Split(':'), 0, "  ", oc.Properties, output);
            //output.WriteLine("}");
            //output.WriteLine("```");

            var exampleObject = BuildExampleObject(oc, classes);

            var root = new Dictionary<string, object>();
            var names = oc.ConfigSection.Split(':');
            var currentLevel = root;
            for (int i = 0; i < names.Length-1; i++)
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
        private object BuildExampleObject(ClassInfo oc, IDictionary<string, ClassInfo> classes)
        {
            var result = new Dictionary<string, object>();
            foreach (var property in oc.Properties)
                result.Add(property.Name, GetPropertyExample(oc, property, classes));
            return result;
        }
        private object GetPropertyExample(ClassInfo @class, OptionsPropertyInfo property,
            IDictionary<string, ClassInfo> classes)
        {
            if (property.TypeIsEnum) return $"\"_enum_value_of_{property.TypeFullName}_\"";

            var type = FrontendWriter.GetJsonType(property.Type);
            if (type == "string") return "_value_";
            if (type == "string[]") return new[] {"_value_"};
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
                    return new[] {new object()};
                return new[] { BuildExampleObject(nestedObject, classes) };
            }

            if(nestedObject == null)
                return new object();
            return BuildExampleObject(nestedObject, classes);
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

            foreach (var @namespace in @class.UsingDirectives.Union(new []{@class.Namespace}))
                if (classes.TryGetValue($"{@namespace}.{property.Type}", out var classInfo))
                    return classInfo;

            return null;
        }


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

            var type = FrontendWriter.GetJsonType(property.Type);
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

            if(type.EndsWith("[]")) return "[{ }, { }]";

            return "{ }";
        }

        protected void WriteEnvironmentVariablesExample(OptionsClassInfo oc, TextWriter output)
        {
            output.WriteLine("### Environment variables example:");
            output.WriteLine("```");
            var prefix = oc.ConfigSection.Replace(":", "__");
            foreach (var prop in oc.Properties)
                output.WriteLine($"{prefix}__{prop.Name}=\"_{prop.Type}_value_\"");
            output.WriteLine("```");
        }
    }
}
