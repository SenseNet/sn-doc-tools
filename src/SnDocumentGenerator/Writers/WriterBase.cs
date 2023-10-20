using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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
        public abstract void WriteOptionClass(OptionsClassInfo op, TextWriter output, Options options);

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
                    //UNDONE: handle errors
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

        public void WriteOptionClasses(IEnumerable<OptionsClassInfo> optionClasses, string outputDir, Options options)
        {
            var fileWriters = new Dictionary<string, TextWriter>();

            foreach (var oc in optionClasses)
            {
                try
                {
                    var categoryWriter = GetOrCreateWriter(outputDir, oc, fileWriters, options);
                    WriteOptionClass(oc, categoryWriter, options);
                }
                catch// (Exception e)
                {
                    //UNDONE: handle errors
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
    }
}
