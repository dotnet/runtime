namespace WebAssemblyInfo
{
    public static class Extensions
    {
        public static string Indent(this string str, string indent)
        {
            return indent + str.Replace("\n", "\n" + indent);
        }
    }
}
