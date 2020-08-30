// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Xunit;

namespace System.Globalization.Tests
{
    /// <summary>
    /// Abstract class for Unicode IdnaTests
    /// </summary>
    public abstract class Unicode_IdnaTest : IConformanceIdnaTest
    {
        public virtual IdnType Type { get; set; }
        public virtual string Source { get; set; }
        public virtual ConformanceIdnaUnicodeTestResult UnicodeResult { get; set; }
        public virtual ConformanceIdnaTestResult ASCIIResult { get; set; }
        public virtual int LineNumber { get; set; }

        /// <summary>
        /// This will convert strings with escaped sequences to literal characters.  The input string is
        /// expected to have escaped sequences in the form of '\uXXXX'.
        ///
        /// Example: "a\u0020b" will be converted to 'a b'.
        /// </summary>
        protected static string EscapedToLiteralString(string escaped, int lineNumber)
        {
            var sb = new StringBuilder();

            for (int i = 0; i < escaped.Length; i++)
            {
                if (i + 1 < escaped.Length && escaped[i] == '\\' && escaped[i + 1] == 'u')
                {
                    // Verify that the escaped sequence is not malformed
                    Assert.True(i + 5 < escaped.Length, "There was a problem converting to literal string on Line " + lineNumber);

                    var codepoint = Convert.ToInt32(escaped.Substring(i + 2, 4), 16);
                    sb.Append((char)codepoint);
                    i += 5;
                }
                else
                {
                    sb.Append(escaped[i]);
                }
            }

            return sb.ToString().Trim();
        }
    }
}
