using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
            var allTypes = parserResult.Classes;
            var enums = parserResult.Enums;
            var serviceRegistrationMethods = parserResult.ServiceRegistrationMethods;

            Console.WriteLine(" ".PadRight(Console.BufferWidth - 1));

            Console.Write("Finalize operation structures ...");
            operations = operations
                .Where(x => x.IsValid)
                //.Where(x=> !string.IsNullOrEmpty(x.Documentation))
                .ToList();

            var testOps = operations.Where(o => o.Project?.IsTestProject ?? true).ToArray();
            var fwOps = operations.Where(o => o.ProjectType == ProjectType.OldNetFramework || o.ProjectType == ProjectType.Unknown).ToArray();
            var coreOps = operations.Except(testOps).Except(fwOps).ToArray();

            SetOperationLinks(options.All ? operations : coreOps);
            Console.WriteLine(" ok");

            Console.Write("Mapping extension method call hierarchy ...");
            var mapper = new CallHierarchyMapper(allTypes);
            mapper.Map(serviceRegistrationMethods);
            Console.WriteLine(" ok");

            using (var writer = new StreamWriter(Path.Combine(options.Output, "generation.txt"), false))
                WriteGenerationInfo(writer, options, operations, coreOps, ref optionsClasses, serviceRegistrationMethods, mapper);

            Console.Write("Writing frontend files ...");
            WriteOutput(operations, coreOps, fwOps, testOps, optionsClasses, serviceRegistrationMethods, allTypes, enums, false, options);
            Console.WriteLine(" ok");
            Console.Write("Writing backend files ...");
            WriteOutput(operations, coreOps, fwOps, testOps, optionsClasses, serviceRegistrationMethods, allTypes, enums, true, options);
            Console.WriteLine(" ok");
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

        private static readonly string HorizontalLine = "--------------------------------------------------------";
        private static void WriteGenerationInfo(TextWriter writer, Options options,
            List<OperationInfo> operations, OperationInfo[] coreOps, ref OptionsClassInfo[] optionClasses,
            List<ServiceRegistrationMethodInfo> serviceRegistrationMethods, CallHierarchyMapper callHierarchyMapper)
        {
            writer.WriteLine("Path:              {0}", options.Input);
            writer.WriteLine("Operations:        {0}", operations.Count);
            writer.WriteLine("Options classes:   {0}", optionClasses.Length);
            writer.WriteLine("Extension methods: {0}", serviceRegistrationMethods.Count);

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
            var issuedOptionsClasses = new List<(OptionsClassInfo oc, List<string> properties)>();
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
            var issuedExtensionMethods = new List<(ServiceRegistrationMethodInfo reg, List<string> properties)>();
            foreach (var reg in serviceRegistrationMethods)
            {
                var properties = new List<string>();
                if (string.IsNullOrEmpty(reg.Documentation))
                    properties.Add("<method summary>");
                foreach (var typeParam in reg.TypeParams)
                    if (string.IsNullOrEmpty(typeParam.Documentation))
                        properties.Add($"<typeParam: {typeParam.Name}>");
                foreach (var parameter in reg.Parameters.Skip(1))
                    if (string.IsNullOrEmpty(parameter.Documentation))
                        properties.Add(parameter.Name);
                if (properties.Count > 0)
                    issuedExtensionMethods.Add((reg, properties));
            }
            writer.WriteLine();

            // -------------------------------------------------------------------------------------

            writer.WriteLine(HorizontalLine);
            writer.WriteLine("MISSING DOCUMENTATION");
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
                writer.WriteLine("'{0}'\t{1}\t{2}", item.oc.File, item.oc.ClassName, string.Join(", ", item.properties));
            }

            writer.WriteLine();
            writer.WriteLine($"Missing documentation of extension methods (count: {issuedExtensionMethods.Count}):");
            writer.WriteLine("File\tClassName\tProperty");
            foreach (var item in issuedExtensionMethods)
            {
                writer.WriteLine("'{0}'\t{1}\t{2}", item.reg.File, item.reg.Method.Identifier.Text, string.Join(", ", item.properties));
            }
            writer.WriteLine();

            // -------------------------------------------------------------------------------------
            var problems = GetOptionsClassProblems(ref optionClasses);
            if (problems.Any())
            {
                writer.WriteLine(HorizontalLine);
                foreach (var message in problems)
                    writer.WriteLine(message);
                writer.WriteLine();
            }

            // -------------------------------------------------------------------------------------

            writer.WriteLine(HorizontalLine);
            writer.WriteLine("Operation descriptions in attributes:");
            writer.WriteLine("Description\tMethodName\tFile");
            foreach (var op in coreOps)
            {
                if (!string.IsNullOrEmpty(op.Description))
                    writer.WriteLine("'{0}'\t{1}\t{2}", op.Description, op.MethodName, op.File);
            }
            writer.WriteLine();

            // -------------------------------------------------------------------------------------

            writer.WriteLine(HorizontalLine);
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
                            .Where(OperationFrontendWriter.IsAllowedParameter)
                            .Select(x => $"{OperationFrontendWriter.GetFrontendType(x.Type).Replace("`", "")} {x.Name}")),
                        OperationFrontendWriter.GetFrontendType(op.ReturnValue.Type).Replace("`", ""));
                }
            }
            writer.WriteLine();

            // -------------------------------------------------------------------------------------

            writer.WriteLine(HorizontalLine);
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
            writer.WriteLine();

            // -------------------------------------------------------------------------------------

            writer.WriteLine(HorizontalLine);
            writer.WriteLine("EXTENSION METHODS CHEAT SHEET (PUBLIC):");
            WriteExtensionMethodsCheatSheet(writer, serviceRegistrationMethods.Where(x => x.IsPublic));
            writer.WriteLine("EXTENSION METHODS CHEAT SHEET (INTERNAL):");
            WriteExtensionMethodsCheatSheet(writer, serviceRegistrationMethods.Where(x => !x.IsPublic));
        }

        private static void WriteExtensionMethodsCheatSheet(TextWriter writer, IEnumerable<ServiceRegistrationMethodInfo> serviceRegistrationMethods)
        {
            var lastRepo = string.Empty;
            var lastProject = string.Empty;
            var lastClass = string.Empty;
            foreach (var item in serviceRegistrationMethods)
            {
                if (lastRepo != item.GithubRepository)
                {
                    lastRepo = item.GithubRepository;
                    writer.WriteLine(lastRepo);
                }

                var fullProjectName = $"    {item.Project.Name}";
                if (lastProject != fullProjectName)
                {
                    lastProject = fullProjectName;
                    writer.WriteLine(fullProjectName);
                }

                var fullClassName = $"        {item.ClassName} (namespace: {item.Namespace})";
                if (lastClass != fullClassName)
                {
                    lastClass = fullClassName;
                    writer.WriteLine(fullClassName);
                }

                writer.WriteLine("            {0}", item.GetMethodSignature(false, true));

                if (item.Registrations.Length > 0)
                {
                    writer.WriteLine("                Calls:");
                    foreach (var registration in item.Registrations)
                        writer.WriteLine("                    {0}", registration);
                }
                if (item.CalledBy.Count > 0)
                {
                    writer.WriteLine("                Called by:");
                    foreach (var parent in item.CalledBy)
                        writer.WriteLine("                    {0}", parent.GetMethodSignature(false, true));
                }
            }
        }

        private static string FormatParameterList(ParameterListSyntax parameters)
        {
            return $"({string.Join(", ", parameters.Parameters.Select(x => x.ToString()))})";
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
            List<ServiceRegistrationMethodInfo> serviceRegistrationMethods,
            Dictionary<string, ClassInfo> allTypes, Dictionary<string, EnumInfo> enums,
            bool forBackend, Options options)
        {
            var outputDir = Path.Combine(options.Output, forBackend ? "backend" : "frontend");
            if (!Directory.Exists(outputDir))
                Directory.CreateDirectory(outputDir);

            var operationsOutputDir = Path.Combine(outputDir, "ODataOperations");
            if (!Directory.Exists(operationsOutputDir))
                Directory.CreateDirectory(operationsOutputDir);

            var operationWriter = forBackend
                ? (OperationWriter)new OperationBackendWriter()
                : new OperationFrontendWriter();

            using (var headWriter = new StreamWriter(Path.Combine(operationsOutputDir, "index.md"), false))
            {
                operationWriter.WriteHead("Api references", headWriter);
                if (options.All)
                {
                    operationWriter.WriteIndex(".NET Standard / Core Operations", coreOps, headWriter, options);
                    operationWriter.WriteIndex(".NET Framework Operations", fwOps, headWriter, options);
                    operationWriter.WriteIndex("Test Operations", testOps, headWriter, options);
                }
                else
                {
                    operationWriter.WriteIndex("Operations", coreOps, headWriter, options);
                }
            }
            using (var treeWriter = new StreamWriter(Path.Combine(operationsOutputDir, "cheatsheet.md"), false))
            {
                operationWriter.WriteHead("Api references", treeWriter);
                if (options.All)
                {
                    operationWriter.WriteCheatSheet(".NET Standard / Core Operations", coreOps, treeWriter, options);
                    operationWriter.WriteCheatSheet(".NET Framework Operations", fwOps, treeWriter, options);
                    operationWriter.WriteCheatSheet("Test Operations", testOps, treeWriter, options);
                }
                else
                {
                    operationWriter.WriteCheatSheet("CHEAT SHEET", coreOps, treeWriter, options);
                }
            }
            operationWriter.WriteOperations(options.All ? operations.ToArray() : coreOps, operationsOutputDir, options);

            /* ======================================================================== */

            var optionClassesOutputDir = Path.Combine(outputDir, "OptionClasses");
            if (!Directory.Exists(optionClassesOutputDir))
                Directory.CreateDirectory(optionClassesOutputDir);

            var optionsClassesWriter = forBackend
                ? (OptionsClassesWriter)new OptionsClassesBackendWriter()
                : new OptionsClassesFrontendWriter();

            using (var headWriter = new StreamWriter(Path.Combine(optionClassesOutputDir, "configuration-index.md"), false))
            {
                optionsClassesWriter.WriteHead("Option class references", headWriter);
                optionsClassesWriter.WriteIndex("Option classes", optionClasses, headWriter, options);
            }
            using (var treeWriter = new StreamWriter(Path.Combine(optionClassesOutputDir, "cheatsheet.md"), false))
            {
                optionsClassesWriter.WriteHead("Option class references", treeWriter);
                optionsClassesWriter.WriteCheatSheet("CHEAT SHEET", optionClasses, treeWriter, options);
            }
            optionsClassesWriter.WriteOptionClasses(optionClasses, allTypes, enums, optionClassesOutputDir, options);

            /* ======================================================================== */

            var serviceRegistrationsOutputDir = Path.Combine(outputDir, "ServiceRegistrations");
            if (!Directory.Exists(serviceRegistrationsOutputDir))
                Directory.CreateDirectory(serviceRegistrationsOutputDir);

            var serviceRegistrationsWriter = forBackend
                ? (SvcRegWriter)new SvcRegBackendWriter()
                : new SvcRegFrontendWriter();

            using (var headWriter = new StreamWriter(Path.Combine(serviceRegistrationsOutputDir, "serviceregistrations-index.md"), false))
            {
                serviceRegistrationsWriter.WriteHead("Service registration references", headWriter);
                serviceRegistrationsWriter.WriteIndex("Service registrations", serviceRegistrationMethods, headWriter, options);
            }
            using (var treeWriter = new StreamWriter(Path.Combine(serviceRegistrationsOutputDir, "cheatsheet.md"), false))
            {
                serviceRegistrationsWriter.WriteHead("Service registration references", treeWriter);
                serviceRegistrationsWriter.WriteCheatSheet("CHEAT SHEET", serviceRegistrationMethods, treeWriter, options);
            }
            serviceRegistrationsWriter.WriteServiceRegistrations(serviceRegistrationMethods, allTypes, enums, serviceRegistrationsOutputDir, options);
        }
    }
}
