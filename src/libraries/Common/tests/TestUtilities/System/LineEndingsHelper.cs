namespace System
{
    public static class LineEndingsHelper
    {
        public const string CompiledNewline = @"
";
        public static readonly bool s_consistentNewlines = StringComparer.Ordinal.Equals(CompiledNewline, Environment.NewLine);

        static public bool IsNewLineConsistent
        {
            get { return s_consistentNewlines; }
        }

        public static string Normalize(string expected)
        {
            if (s_consistentNewlines)
                return expected;

            return expected.Replace(CompiledNewline, Environment.NewLine);
        }
    }
}
