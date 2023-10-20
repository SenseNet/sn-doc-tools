namespace SnDocumentGenerator
{
    public static class Extensions
    {
        public static string FormatType(this string src)
        {
            return src.Contains('<') ? $"`{src}`" : src;
        }
    }
}
