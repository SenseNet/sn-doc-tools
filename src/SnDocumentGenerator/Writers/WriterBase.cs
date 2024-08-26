using System;
using System.Collections.Generic;
using System.IO;

namespace SnDocumentGenerator.Writers;

internal class WriterBase
{
    protected class CategoryComparer : IComparer<string>
    {
        public int Compare(string x, string y)
        {
            if (x == null && y == null)
                return 0;
            if (x == null)
                return 1;
            if (y == null)
                return -1;
            return string.Compare(x, y, StringComparison.OrdinalIgnoreCase);
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






    public static string GetFrontendType(string type)
    {
        return $"`{GetJsonType(type)}`";
    }
    public static string GetJsonType(string type)
    {
        if (type == "System.Threading.Tasks.Task")
            return "void";
        if (type == "STT.Task")
            return "void";

        if (type.StartsWith("STT.Task<"))
            type = type.Substring(4);
        if (type.StartsWith("Task<"))
            type = type.Remove(0, "Task<".Length).TrimEnd('>');
        if (type.StartsWith("IEnumerable<"))
            type = type.Remove(0, "IEnumerable<".Length).TrimEnd('>') + "[]";
        if (type.StartsWith("ICollection<"))
            type = type.Remove(0, "ICollection<".Length).TrimEnd('>') + "[]";
        if (type.StartsWith("ODataArray<"))
            return type.Substring(11).Replace(">", "") + "[]";

        return type;
    }

}