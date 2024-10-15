// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.RegularExpressions
{
    [Flags]
#if SYSTEM_TEXT_REGULAREXPRESSIONS
    public
#else
    internal
#endif
    enum RegexOptions
    {
        /// <summary>Use default behavior.</summary>
        None = 0x0000,

        /// <summary>Use case-insensitive matching.</summary>
        IgnoreCase = 0x0001, // "i"

        /// <summary>
        /// Use multiline mode, where ^ and $ match the beginning and end of each line
        /// (instead of the beginning and end of the input string).
        /// </summary>
        Multiline = 0x0002, // "m"

        /// <summary>
        /// Do not capture unnamed groups. The only valid captures are explicitly named
        /// or numbered groups of the form (?&lt;name&gt; subexpression).
        /// </summary>
        ExplicitCapture = 0x0004, // "n"

        /// <summary>Compile the regular expression to Microsoft intermediate language (MSIL).</summary>
        Compiled = 0x0008,

        /// <summary>
        /// Use single-line mode, where the period (.) matches every character (instead of every character except \n).
        /// </summary>
        Singleline = 0x0010, // "s"

        /// <summary>Exclude unescaped white space from the pattern, and enable comments after a number sign (#).</summary>
        IgnorePatternWhitespace = 0x0020, // "x"

        /// <summary>Change the search direction. Search moves from right to left instead of from left to right.</summary>
        RightToLeft = 0x0040,

        /// <summary>Enable ECMAScript-compliant behavior for the expression.</summary>
        ECMAScript = 0x0100,

        /// <summary>Ignore cultural differences in language.</summary>
        CultureInvariant = 0x0200,

        /// <summary>
        /// Enable matching using an approach that avoids backtracking and guarantees linear-time processing
        /// in the length of the input.
        /// </summary>
        /// <remarks>
        /// Certain features aren't available when this option is set, including balancing groups,
        /// backreferences, positive and negative lookaheads and lookbehinds, and atomic groups.
        /// Capture groups are also ignored, such that the only capture available is that for
        /// the top-level match.
        /// </remarks>
        NonBacktracking = 0x0400,
    }
}
