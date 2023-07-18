// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System
{
    public static class LineEndingsHelper
    {
        private const string CompiledNewline = @"
";
        private static readonly bool s_consistentNewlines = StringComparer.Ordinal.Equals(CompiledNewline, Environment.NewLine);

        public static string Normalize(string expected)
        {
            if (s_consistentNewlines)
                return expected;

            return expected.Replace("\r\n", "\n").Replace("\n", Environment.NewLine);
        }
    }
}
