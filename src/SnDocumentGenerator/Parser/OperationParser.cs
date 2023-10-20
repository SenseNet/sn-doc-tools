using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;

namespace SnDocumentGenerator.Parser
{
    internal class OperationParser
    {
        private Options _options;

        public OperationParser(Options options)
        {
            _options = options;
        }

        public (List<OperationInfo> Operations, List<OptionsClassInfo> OptionsClasses) Parse()
        {
            var operations = new List<OperationInfo>();
            var optionsClasses = new List<OptionsClassInfo>();

            var input = Path.GetFullPath(_options.Input);
            //string rootPath;
            if (File.Exists(input))
            {
                if (Path.GetExtension(input) != ".cs")
                    throw new NotSupportedException("Only csharp file (*.cs extension) is supported.");
                //rootPath = Path.GetDirectoryName(input);
                AddOperationsFromFile(input, input, operations, optionsClasses, null, _options.ShowAst);
            }
            else
            {
                if (!Directory.Exists(input))
                    throw new ArgumentException("Unknown file or directory: " + input);
                //rootPath = input.TrimEnd('\\', '/');
                AddOperationsFromDirectory(input, input, operations, optionsClasses, null, _options.ShowAst);
            }

            return (operations, optionsClasses);
        }

        private void AddOperationsFromDirectory(string root, string path, List<OperationInfo> operations,
            List<OptionsClassInfo> optionsClasses, ProjectInfo currentProject, bool showAst)
        {
            if (path.EndsWith("\\obj", StringComparison.OrdinalIgnoreCase))
                return;
            if (path.EndsWith("\\lut", StringComparison.OrdinalIgnoreCase))
                return;
            if (path.EndsWith("\\.git", StringComparison.OrdinalIgnoreCase))
                return;
            if (path.EndsWith("\\.vs", StringComparison.OrdinalIgnoreCase))
                return;

            var projectPath = Directory.GetFiles(path, "*.csproj").FirstOrDefault();
            if (projectPath != null)
                currentProject = CreateProject(projectPath);

            foreach (var directory in Directory.GetDirectories(path))
                AddOperationsFromDirectory(root, directory, operations, optionsClasses, currentProject, showAst);
            foreach (var file in Directory.GetFiles(path, "*.cs"))
                AddOperationsFromFile(root, file, operations, optionsClasses, currentProject, showAst);
        }

        private ProjectInfo CreateProject(string projectPath)
        {
            var path = Path.GetDirectoryName(projectPath);
            var name = Path.GetFileNameWithoutExtension(projectPath);
            var typeName = GetProjectTypeName(projectPath);
            var type = GetProjectType(typeName);
            var isTest = name.EndsWith("test", StringComparison.OrdinalIgnoreCase) ||
                         name.EndsWith("tests", StringComparison.OrdinalIgnoreCase);

            return new ProjectInfo
            {
                Path = path,
                Name = name,
                TypeName = typeName,
                Type = type,
                IsTestProject = isTest
            };
        }

        private string GetProjectTypeName(string projectPath)
        {
            var src = File.ReadAllLines(projectPath).Select(x => x.Trim()).ToArray();
            var targetFwLine = src.FirstOrDefault(x => x.StartsWith("<TargetFramework>"));
            if (targetFwLine != null)
            {
                var typeName = targetFwLine
                    .Replace("</TargetFramework>", "", StringComparison.OrdinalIgnoreCase)
                    .Replace("<TargetFramework>", "", StringComparison.OrdinalIgnoreCase);
                return typeName;
            }

            targetFwLine = src.FirstOrDefault(x => x.StartsWith("<TargetFrameworks>"));
            if (targetFwLine != null)
            {
                var typeName = targetFwLine
                    .Replace("</TargetFrameworks>", "", StringComparison.OrdinalIgnoreCase)
                    .Replace("<TargetFrameworks>", "", StringComparison.OrdinalIgnoreCase);
                return typeName;
            }

            var targetFwVersionLine = src.FirstOrDefault(x => x.StartsWith("<TargetFrameworkVersion>"));
            if (targetFwVersionLine != null)
            {
                var version = targetFwVersionLine
                    .Replace("</TargetFrameworkVersion>", "", StringComparison.OrdinalIgnoreCase)
                    .Replace("<TargetFrameworkVersion>v", "", StringComparison.OrdinalIgnoreCase)
                    .Replace("<TargetFrameworkVersion>", "", StringComparison.OrdinalIgnoreCase);
                return "netframework" + version;
            }

            throw new ApplicationException("Unknown project: " + projectPath);
        }

        private ProjectType GetProjectType(string typeName)
        {
            if (typeName.StartsWith("netcoreapp"))
                return ProjectType.NETCore;
            if (typeName.StartsWith("netstandard"))
                return ProjectType.NETStandard;
            if (typeName.StartsWith("netframework"))
                return ProjectType.NETFramework;
            return ProjectType.Unknown;
        }

        private void AddOperationsFromFile(string root, string path, List<OperationInfo> operations,
            List<OptionsClassInfo> optionsClasses, ProjectInfo currentProject, bool showAst)
        {
            if (path.Length > root.Length)
            {
                var line = path.Substring(root.Length + 1).PadRight(Console.BufferWidth - 1);
                Console.Write(line + "\r");
            }

            var code = new StreamReader(path).ReadToEnd();
            var tree = CSharpSyntaxTree.ParseText(code);
            var walker = new MainWalker(path, showAst);
            walker.Visit(tree.GetRoot());

            foreach (var op in walker.Operations)
                op.Project = currentProject;
            operations.AddRange(walker.Operations);
            if (walker.Operations.Count > 0)
                Console.WriteLine("{0}: {1}", path, walker.Operations.Count);

            if (walker.OptionsClasses.Count > 0)
            {
                foreach (var optionsClass in walker.OptionsClasses)
                {
                    optionsClass.Project = currentProject;
                    optionsClasses.Add(optionsClass);
                }

                if (walker.OptionsClasses.Count > 0)
                    Console.WriteLine("{0}: OPTIONS CLASSES: {1}", path, walker.OptionsClasses.Count);
            }
        }

    }
}
