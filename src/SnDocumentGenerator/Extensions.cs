namespace SnDocumentGenerator
{
    public static class Extensions
    {
        public static string FormatType(this string src)
        {
            if(src.Contains('<'))
                return $"`{src}`";
            return src;
        }
    }
}
