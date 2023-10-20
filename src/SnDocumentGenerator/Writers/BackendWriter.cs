using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SnDocumentGenerator.Writers
{
    internal class BackendWriter : WriterBase
    {
        public override void WriteTable(string title, OperationInfo[] ops, TextWriter output, Options options)
        {
            if (!ops.Any())
                return;

            output.WriteLine($"## {title} ({ops.Length})");

            var ordered = ops.OrderBy(o => o.File).ThenBy(o => o.OperationName);
            output.WriteLine("| Operation | Category | Type | Repository | Project | File | Directory |");
            output.WriteLine("| --------- | -------- | ---- | ---------- | ------- | ---- | --------- |");
            foreach (var op in ordered)
            {
                if (options.FileLevel == FileLevel.Category)
                {
                    output.WriteLine("| [{0}](/restapi/{1}#{2}) | [{3}](/restapi/{1}) | {4} | {5} | {6} | {7} | {8} |",
                        op.OperationName,
                        op.CategoryInLink,
                        op.OperationNameInLink,
                        op.Category,
                        op.IsAction ? "Action" : "Function",
                        op.GithubRepository,
                        op.ProjectName,
                        Path.GetFileName(op.FileRelative),
                        Path.GetDirectoryName(op.FileRelative));
                }
                else if (options.FileLevel == FileLevel.Operation)
                {
                    output.WriteLine("| [{0}](/restapi/{1}/{2}) | {3} | {4} | {5} | {6} | {7} | {8} |",
                        op.OperationName,
                        op.CategoryInLink,
                        op.OperationNameInLink,
                        op.Category,
                        op.IsAction ? "Action" : "Function",
                        op.GithubRepository,
                        op.ProjectName,
                        Path.GetFileName(op.FileRelative),
                        Path.GetDirectoryName(op.FileRelative));
                }
                else if (options.FileLevel == FileLevel.OperationNoCategories)
                {
                    output.WriteLine("| [{0}](/restapi/{2}) | {3} | {4} | {5} | {6} | {7} | {8} |",
                        op.OperationName,
                        op.CategoryInLink,
                        op.OperationNameInLink,
                        op.Category,
                        op.IsAction ? "Action" : "Function",
                        op.GithubRepository,
                        op.ProjectName,
                        Path.GetFileName(op.FileRelative),
                        Path.GetDirectoryName(op.FileRelative));
                }
                else
                {
                    throw GetNotSupportedFileLevelException(options.FileLevel);
                }
            }
        }
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
                    output.WriteLine("| [{0}](/options/{2}) | {3} | {4} | {5} | {6} | {7} |",
                        oc.ClassName,
                        oc.CategoryInLink,
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

        public override void WriteTree(string title, OperationInfo[] ops, TextWriter output, Options options)
        {
            output.WriteLine($"## {title} ({ops.Length})");
            output.WriteLine($"### ... coming soon.");
        }
        public override void WriteTree(string title, OptionsClassInfo[] ocs, TextWriter output, Options options)
        {
            output.WriteLine($"## {title} ({ocs.Length} classes)");
            output.WriteLine($"### ... coming soon.");
        }

        public override void WriteOperation(OperationInfo op, TextWriter output, Options options)
        {
            output.WriteLine("## {0}", op.OperationName);
            List<string> head;

            head = new List<string>
            {
                op.IsAction ? "- Type: **ACTION**" : "- Type: **FUNCTION**",
                $"- Repository: **{op.GithubRepository}**",
                $"- Project: **{op.ProjectName}**",
                $"- File: **{op.FileRelative}**",
                $"- Class: **{op.Namespace}.{op.ClassName}**",
                $"- Method: **{op.MethodName}**"
            };
            if (op.Icon != null)
                head.Add($"- Icon: **{op.Icon}**");

            output.Write(string.Join(Environment.NewLine, head));
            output.WriteLine(".");

            if (!string.IsNullOrEmpty(op.Description) && !options.HideDescription)
            {
                output.WriteLine();
                output.WriteLine(op.Description);
            }

            output.WriteLine();
            if (!string.IsNullOrEmpty(op.Documentation))
            {
                output.WriteLine(op.Documentation);
            }
            output.WriteLine();

            output.WriteLine("### Parameters:");
            foreach (var prm in op.Parameters)
                output.WriteLine("- **{0}** ({1}){2}: {3}", prm.Name, prm.Type.FormatType(),
                    prm.IsOptional ? " optional" : "", prm.Documentation);
            if (op.ReturnValue.Type != "void")
                output.WriteLine("- **Return value** ({0}): {1}", op.ReturnValue.Type.FormatType(),
                    op.ReturnValue.Documentation);

            output.WriteLine();
            if (0 < op.ContentTypes.Count + op.AllowedRoles.Count + op.RequiredPermissions.Count +
                op.RequiredPolicies.Count + op.Scenarios.Count)
            {
                output.WriteLine("### Requirements:");
                WriteAttribute("ContentTypes", op.ContentTypes, "N.CT.", output);
                WriteAttribute("AllowedRoles", op.AllowedRoles, "N.R.", output);
                WriteAttribute("RequiredPermissions", op.RequiredPermissions, "N.P.", output);
                WriteAttribute("RequiredPolicies", op.RequiredPolicies, "N.Pol.", output);
                WriteAttribute("Scenarios", op.Scenarios, "N.S.", output);
            }

            output.WriteLine();
        }

        public override void WriteOptionClass(OptionsClassInfo oc, TextWriter output, Options options)
        {
            output.WriteLine("## {0}", oc.ClassName);
            List<string> head;

            head = new List<string>
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

            WriteOptionsExample(oc, output);
        }
    }
}
