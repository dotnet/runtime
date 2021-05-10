namespace System.CodeDom.Compiler.Tests
{
    public static class LineEndingsHelper
    {
        private const string CompiledNewline = @"
";
        public static readonly bool s_replaceNewlines = !StringComparer.Ordinal.Equals(CompiledNewline, Environment.NewLine);

        public static string Normalize(string expected)
        {
            if (!s_replaceNewlines)
                return expected;

            return expected.Replace(CompiledNewline, Environment.NewLine);
        }
    }
}
