using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SnDocumentGenerator.Parser;
using SnDocumentGenerator.Writers;

namespace SnDocumentGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 2 && args.Length != 3 && args.Length != 4)
            {
                Console.WriteLine("Usage: SnDocumentGenerator <InputDir> <OutputDir> [-cat|-op|-flat] [-all]");
                return;
            }

            var options = new Options
            {
                Input = args[0],
                Output = args[1],
                All = args.Contains("-all", StringComparer.OrdinalIgnoreCase),
                ShowAst = false,
            };
            if (args.Contains("-op", StringComparer.OrdinalIgnoreCase))
                options.FileLevel = FileLevel.Operation;
            else if (args.Contains("-cat", StringComparer.OrdinalIgnoreCase))
                options.FileLevel = FileLevel.Category;
            else if (args.Contains("-flat", StringComparer.OrdinalIgnoreCase))
                options.FileLevel = FileLevel.OperationNoCategories;

            if (Directory.Exists(options.Output))
                Directory.Delete(options.Output, true);
            Directory.CreateDirectory(options.Output);

            Run(options);
        }

        private static void Run(Options options)
        {
            var parser = new OperationParser(options);
            var parserResult =  parser.Parse();
            var operations = parserResult.Operations;
            var optionsClasses = parserResult.OptionsClasses.ToArray();
            var classes = parserResult.Classes;
            var enums = parserResult.Enums;

            Console.WriteLine(" ".PadRight(Console.BufferWidth - 1));

            operations = operations
                .Where(x => x.IsValid)
                //.Where(x=> !string.IsNullOrEmpty(x.Documentation))
                .ToList();

            var testOps = operations.Where(o => o.Project?.IsTestProject ?? true).ToArray();
            var fwOps = operations.Where(o => o.ProjectType == ProjectType.OldNetFramework || o.ProjectType == ProjectType.Unknown).ToArray();
            var coreOps = operations.Except(testOps).Except(fwOps).ToArray();

            SetOperationLinks(options.All ? operations : coreOps);

            using (var writer = new StreamWriter(Path.Combine(options.Output, "generation.txt"), false))
                WriteGenerationInfo(writer, options, operations, coreOps, ref optionsClasses);

            WriteOutput(operations, coreOps, fwOps, testOps, optionsClasses, classes, enums, false, options);
            WriteOutput(operations, coreOps, fwOps, testOps, optionsClasses, classes, enums, true, options);
        }

        private static void SetOperationLinks(IEnumerable<OperationInfo> operations)
        {
            var ops = new Dictionary<string, OperationInfo>();
            foreach (var op in operations)
            {
                var nameBase = op.OperationName.ToLowerInvariant();
                var index = 1;
                var name = nameBase;
                while (ops.ContainsKey(name))
                    name = nameBase + ++index;

                ops.Add(name, op);
                op.OperationNameInLink = name;
            }
        }

        private static void WriteGenerationInfo(TextWriter writer, Options options,
            List<OperationInfo> operations, OperationInfo[] coreOps, ref OptionsClassInfo[] optionClasses)
        {
            writer.WriteLine("Path:            {0}", options.Input);
            writer.WriteLine("Operations:      {0}", operations.Count);
            writer.WriteLine("Options classes: {0}", optionClasses.Length);

            var issuedOperations = new List<(OperationInfo op, List<string> parameters)>();
            foreach (var op in coreOps)
            {
                var parameters = new List<string>();
                if (string.IsNullOrEmpty(op.Documentation))
                    parameters.Add("<summary>");
                for (var i = 1; i < op.Parameters.Count; i++)
                    if (string.IsNullOrEmpty(op.Parameters[i].Documentation))
                        parameters.Add(op.Parameters[i].Name);
                if (!op.IsAction && string.IsNullOrEmpty(op.ReturnValue.Documentation))
                    parameters.Add("<returns>");
                if (parameters.Count > 1)
                    issuedOperations.Add((op, parameters));
            }
            var issuedOptionsClasses = new List<(OptionsClassInfo op, List<string> properties)>();
            foreach (var oc in optionClasses)
            {
                var properties = new List<string>();
                if (string.IsNullOrEmpty(oc.Documentation))
                    properties.Add("<class summary>");
                foreach (var property in oc.Properties)
                    if (string.IsNullOrEmpty(property.Documentation))
                        properties.Add(property.Name);
                if (properties.Count > 0)
                    issuedOptionsClasses.Add((oc, properties));
            }

            writer.WriteLine();
            writer.WriteLine($"Missing documentation of operations (except the first 'content' parameter) (count: {issuedOperations.Count}):");
            writer.WriteLine("File\tMethodName\tParameter");
            foreach (var item in issuedOperations)
            {
                writer.WriteLine("'{0}'\t{1}\t{2}", item.op.File, item.op.MethodName, string.Join(", ", item.parameters));
            }

            writer.WriteLine();
            writer.WriteLine($"Missing documentation of options classes (count: {issuedOptionsClasses.Count}):");
            writer.WriteLine("File\tClassName\tProperty");
            foreach (var item in issuedOptionsClasses)
            {
                writer.WriteLine("'{0}'\t{1}\t{2}", item.op.File, item.op.ClassName, string.Join(", ", item.properties));
            }

            var problems = GetOptionsClassProblems(ref optionClasses);
            if (problems.Any())
            {
                writer.WriteLine();
                foreach (var message in problems)
                    writer.WriteLine(message);
            }

            //writer.WriteLine();
            //writer.WriteLine("Unnecessary doc of requested resource (content parameter):");
            //writer.WriteLine("File\tMethodName\tDescription of content param");
            //foreach (var op in coreOps)
            //{
            //    var desc = op.Parameters[0].Documentation;
            //    if (!string.IsNullOrEmpty(desc))
            //        writer.WriteLine("'{0}'\t{1}\t{2}", op.File, op.MethodName, desc);
            //}

            writer.WriteLine();
            writer.WriteLine("Operation descriptions:");
            writer.WriteLine("Description\tMethodName\tFile");
            foreach (var op in coreOps)
            {
                if (!string.IsNullOrEmpty(op.Description))
                    writer.WriteLine("'{0}'\t{1}\t{2}", op.Description, op.MethodName, op.File);
            }

            writer.WriteLine();
            writer.WriteLine("Functions and parameters:");
            writer.WriteLine("File\tMethodName\tParameters");
            foreach (var op in coreOps)
            {
                if (!op.IsAction)
                    writer.WriteLine("{0}\t{1}\t{2}", op.File, op.MethodName,
                        string.Join(", ", op.Parameters.Skip(1).Select(x => $"{x.Type} {x.Name}")));
            }

            writer.WriteLine();
            writer.WriteLine("Actions and parameters:");
            writer.WriteLine("File\tMethodName\tParameters");
            foreach (var op in coreOps)
            {
                if (op.IsAction)
                    writer.WriteLine("{0}\t{1}\t{2}", op.File, op.MethodName,
                        string.Join(", ", op.Parameters.Skip(1).Select(x => $"{x.Type} {x.Name}")));
            }

            writer.WriteLine();
            writer.WriteLine("Options classes and properties:");
            writer.WriteLine("File\tClassName\tProperties");
            foreach (var oc in optionClasses)
            {
                writer.WriteLine("{0}\t{1}\t{2}", oc.File, oc.ClassName,
                    string.Join(", ", oc.Properties.Select(x => $"{x.Type} {x.Name}")));
            }

            writer.WriteLine();
            writer.WriteLine("ODATA CHEAT SHEET:");
            foreach (var opGroup in coreOps.GroupBy(x => x.Category).OrderBy(x => x.Key))
            {
                writer.WriteLine("  {0}", opGroup.Key);
                foreach (var op in opGroup.OrderBy(x => x.OperationName))
                {
                    //if (op.IsAction && op.Parameters.Count > 1)
                    writer.WriteLine("    {0} {1}({2}) : {3}",
                        op.IsAction ? "POST" : "GET ",
                        op.OperationName,
                        string.Join(", ", op.Parameters.Skip(1)
                            .Where(FrontendWriter.IsAllowedParameter)
                            .Select(x => $"{FrontendWriter.GetFrontendType(x.Type).Replace("`", "")} {x.Name}")),
                        FrontendWriter.GetFrontendType(op.ReturnValue.Type).Replace("`", ""));
                }
            }
            
            writer.WriteLine();
            writer.WriteLine("OPTION CLASSES CHEAT SHEET:");
            foreach (var optionsClass in optionClasses)
            {
                writer.WriteLine("  {0}", optionsClass.ClassName);
                foreach (var property in optionsClass.Properties/*.OrderBy(x => x.Name)*/)
                {
                    writer.WriteLine("    {0} {1} {{{2} }} {3}",
                        property.Type,
                        property.Name,
                        $"{(property.HasGetter ? " get;" : "")}{(property.HasSetter ? " set;" : "")}",
                        property.Initializer ?? "");
                }
            }
        }
        private static List<string> GetOptionsClassProblems(ref OptionsClassInfo[] optionClasses)
        {
            var messages = new List<string>();
            var classesToRemove = new List<OptionsClassInfo>();
            for (var i = 0; i < optionClasses.Length - 1; i++)
            {
                for (var j = i + 1; j < optionClasses.Length; j++)
                {
                    if (optionClasses[i].ConfigSection == optionClasses[j].ConfigSection)
                    {
                        if (!CheckPropertyTypes(optionClasses[i], optionClasses[j]))
                        {
                            messages.Add(
                                $"ERROR! Duplicated section '{optionClasses[i].ConfigSection}' and property type violation found in these options classes:\r\n" +
                                $"\t{optionClasses[i].ClassName}: {optionClasses[i].File}\r\n" +
                                $"\t\t{string.Join("; ", optionClasses[i].Properties.Select(x => $"{x.Type} {x.Name}"))}\r\n" +
                                $"\t{optionClasses[j].ClassName}: {optionClasses[j].File}\r\n" +
                                $"\t\t{string.Join("; ", optionClasses[j].Properties.Select(x => $"{x.Type} {x.Name}"))}\r\n" +
                                $"\tDocumentations of these classes are skipped.");
                            classesToRemove.Add(optionClasses[i]);
                            classesToRemove.Add(optionClasses[j]);
                        }
                        else
                        {
                            messages.Add(
                                $"WARNING! Duplicated section '{optionClasses[i].ConfigSection}' found in these options classes:\r\n" +
                                $"\t{optionClasses[i].ClassName}: {optionClasses[i].File}\r\n" +
                                $"\t\t{string.Join("; ", optionClasses[i].Properties.Select(x => $"{x.Type} {x.Name}"))}\r\n" +
                                $"\t{optionClasses[j].ClassName}: {optionClasses[j].File}\r\n" +
                                $"\t\t{string.Join("; ", optionClasses[j].Properties.Select(x => $"{x.Type} {x.Name}"))}");
                        }
                    }
                }
            }

            if (classesToRemove.Count > 0)
                optionClasses = optionClasses.Except(classesToRemove).ToArray();

            return messages;
        }

        private static bool CheckPropertyTypes(OptionsClassInfo class1, OptionsClassInfo class2)
        {
            foreach (var prop1 in class1.Properties)
            {
                var prop2 = class2.Properties.FirstOrDefault(x => x.Name == prop1.Name);
                if (prop2 != null && prop2.Type != prop1.Type)
                    return false;
            }

            return true;
        }

        private static void WriteOutput(List<OperationInfo> operations,
            OperationInfo[] coreOps, OperationInfo[] fwOps, OperationInfo[] testOps,
            OptionsClassInfo[] optionClasses,
            Dictionary<string, ClassInfo> classes, Dictionary<string, EnumInfo> enums,
            bool forBackend, Options options)
        {
            var outputDir = Path.Combine(options.Output, forBackend ? "backend" : "frontend");
            if (!Directory.Exists(outputDir))
                Directory.CreateDirectory(outputDir);

            var operationsOutputDir = Path.Combine(outputDir, "ODataOperations");
            if (!Directory.Exists(operationsOutputDir))
                Directory.CreateDirectory(operationsOutputDir);

            var optionClassesOutputDir = Path.Combine(outputDir, "OptionClasses");
            if (!Directory.Exists(optionClassesOutputDir))
                Directory.CreateDirectory(optionClassesOutputDir);

            var writer = forBackend ? (WriterBase)new BackendWriter() : new FrontendWriter();

            using (var headWriter = new StreamWriter(Path.Combine(operationsOutputDir, "index.md"), false))
            {
                writer.WriteHead("Api references", headWriter);
                if (options.All)
                {
                    writer.WriteTable(".NET Standard / Core Operations", coreOps, headWriter, options);
                    writer.WriteTable(".NET Framework Operations", fwOps, headWriter, options);
                    writer.WriteTable("Test Operations", testOps, headWriter, options);
                }
                else
                {
                    writer.WriteTable("Operations", coreOps, headWriter, options);
                }
            }
            using (var treeWriter = new StreamWriter(Path.Combine(operationsOutputDir, "cheatsheet.md"), false))
            {
                writer.WriteHead("Api references", treeWriter);
                if (options.All)
                {
                    writer.WriteTree(".NET Standard / Core Operations", coreOps, treeWriter, options);
                    writer.WriteTree(".NET Framework Operations", fwOps, treeWriter, options);
                    writer.WriteTree("Test Operations", testOps, treeWriter, options);
                }
                else
                {
                    writer.WriteTree("CHEAT SHEET", coreOps, treeWriter, options);
                }
            }
            writer.WriteOperations(options.All ? operations.ToArray() : coreOps, operationsOutputDir, options);

            using (var headWriter = new StreamWriter(Path.Combine(optionClassesOutputDir, "configuration-index.md"), false))
            {
                writer.WriteHead("Option class references", headWriter);
                writer.WriteTable("Option classes", optionClasses, headWriter, options);
            }
            //using (var treeWriter = new StreamWriter(Path.Combine(optionClassesOutputDir, "cheatsheet.md"), false))
            //{
            //    writer.WriteHead("Option class references", treeWriter);
            //    writer.WriteTree("CHEAT SHEET", optionClasses, treeWriter, options);
            //}
            writer.WriteOptionClasses(optionClasses, classes, enums, optionClassesOutputDir, options);
        }
    }
}
