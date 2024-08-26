using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.VisualBasic.CompilerServices;
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

    internal abstract class OperationWriter : WriterBase
    {
        // ReSharper disable once InconsistentNaming
        protected static readonly string CR = Environment.NewLine;

        public abstract void WriteIndex(string title, OperationInfo[] ops, TextWriter output, Options options);
        public abstract void WriteCheatSheet(string title, OperationInfo[] ops, TextWriter output, Options options);

        public abstract void WriteOperation(OperationInfo op, TextWriter output, Options options);

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
    }
}
