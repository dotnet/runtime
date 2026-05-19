// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.RegularExpressions
{
    /// <summary>Provides enumerated values to use to set regular expression options.</summary>
    /// <remarks>
    /// <para>
    /// Several options provided by members of the <see cref="RegexOptions"/> enumeration (in particular,
    /// <see cref="RegexOptions.ExplicitCapture"/>, <see cref="RegexOptions.IgnoreCase"/>,
    /// <see cref="RegexOptions.Multiline"/>, and <see cref="RegexOptions.Singleline"/>) can instead be
    /// provided by using an inline option character in the regular expression pattern. For details, see
    /// <see href="https://learn.microsoft.com/dotnet/standard/base-types/regular-expression-options">
    /// Regular Expression Options</see>.
    /// </para>
    /// </remarks>
    [Flags]
#if SYSTEM_TEXT_REGULAREXPRESSIONS
    public
#else
    internal
#endif
    enum RegexOptions
    {
        /// <summary>
        /// Specifies that no options are set. For more information about the default behavior of the regular
        /// expression engine, see the "Default Options" section in the
        /// <see href="https://learn.microsoft.com/dotnet/standard/base-types/regular-expression-options">
        /// Regular Expression Options</see> article.
        /// </summary>
        None = 0x0000,

        /// <summary>
        /// Specifies case-insensitive matching. For more information, see the "Case-Insensitive Matching"
        /// section in the
        /// <see href="https://learn.microsoft.com/dotnet/standard/base-types/regular-expression-options">
        /// Regular Expression Options</see> article.
        /// </summary>
        IgnoreCase = 0x0001, // "i"

        /// <summary>
        /// Multiline mode. Changes the meaning of <c>^</c> and <c>$</c> so they match at the beginning
        /// and end, respectively, of any line, and not just the beginning and end of the entire string.
        /// For more information, see the "Multiline Mode" section in the
        /// <see href="https://learn.microsoft.com/dotnet/standard/base-types/regular-expression-options">
        /// Regular Expression Options</see> article.
        /// </summary>
        Multiline = 0x0002, // "m"

        /// <summary>
        /// Specifies that the only valid captures are explicitly named or numbered groups of the form
        /// <c>(?&lt;name&gt;...)</c>. This allows unnamed parentheses to act as noncapturing groups without
        /// the syntactic clumsiness of the expression <c>(?:...)</c>. For more information, see the
        /// "Explicit Captures Only" section in the
        /// <see href="https://learn.microsoft.com/dotnet/standard/base-types/regular-expression-options">
        /// Regular Expression Options</see> article.
        /// </summary>
        ExplicitCapture = 0x0004, // "n"

        /// <summary>
        /// Specifies that the regular expression is compiled to MSIL code, instead of being interpreted.
        /// Compiled regular expressions maximize run-time performance at the expense of initialization time.
        /// For more information, see the "Compiled Regular Expressions" section in the
        /// <see href="https://learn.microsoft.com/dotnet/standard/base-types/regular-expression-options">
        /// Regular Expression Options</see> article.
        /// </summary>
        Compiled = 0x0008,

        /// <summary>
        /// Specifies single-line mode. Changes the meaning of the dot (<c>.</c>) so it matches every character
        /// (instead of every character except <c>\n</c>). For more information, see the "Single-line Mode"
        /// section in the
        /// <see href="https://learn.microsoft.com/dotnet/standard/base-types/regular-expression-options">
        /// Regular Expression Options</see> article.
        /// </summary>
        Singleline = 0x0010, // "s"

        /// <summary>
        /// Eliminates unescaped white space from the pattern and enables comments marked with <c>#</c>.
        /// However, this value does not affect or eliminate white space in
        /// <see href="https://learn.microsoft.com/dotnet/standard/base-types/character-classes-in-regular-expressions">
        /// character classes</see>, numeric
        /// <see href="https://learn.microsoft.com/dotnet/standard/base-types/quantifiers-in-regular-expressions">
        /// quantifiers</see>, or tokens that mark the beginning of individual
        /// <see href="https://learn.microsoft.com/dotnet/standard/base-types/regular-expression-language-quick-reference">
        /// regular expression language elements</see>. For more information, see the "Ignore White Space"
        /// section of the
        /// <see href="https://learn.microsoft.com/dotnet/standard/base-types/regular-expression-options">
        /// Regular Expression Options</see> article.
        /// </summary>
        IgnorePatternWhitespace = 0x0020, // "x"

        /// <summary>
        /// Specifies that the search will be from right to left instead of from left to right. For more
        /// information, see the "Right-to-Left Mode" section in the
        /// <see href="https://learn.microsoft.com/dotnet/standard/base-types/regular-expression-options">
        /// Regular Expression Options</see> article.
        /// </summary>
        RightToLeft = 0x0040,

        /// <summary>
        /// Enables ECMAScript-compliant behavior for the expression. This value can be used only in
        /// conjunction with the <see cref="IgnoreCase"/>, <see cref="Multiline"/>, and
        /// <see cref="Compiled"/> values. The use of this value with any other values results in an
        /// exception.
        /// </summary>
        /// <remarks>
        /// For more information on the <see cref="ECMAScript"/> option, see the "ECMAScript Matching
        /// Behavior" section in the
        /// <see href="https://learn.microsoft.com/dotnet/standard/base-types/regular-expression-options">
        /// Regular Expression Options</see> article.
        /// </remarks>
        ECMAScript = 0x0100,

        /// <summary>
        /// Specifies that cultural differences in language are ignored. For more information, see the
        /// "Comparison Using the Invariant Culture" section in the
        /// <see href="https://learn.microsoft.com/dotnet/standard/base-types/regular-expression-options">
        /// Regular Expression Options</see> article.
        /// </summary>
        CultureInvariant = 0x0200,

        /// <summary>
        /// Enable matching using an approach that avoids backtracking and guarantees linear-time processing
        /// in the length of the input. For more information, see the
        /// <see href="https://learn.microsoft.com/dotnet/standard/base-types/regular-expression-options">
        /// Regular Expression Options</see> article.
        /// </summary>
        /// <remarks>
        /// Certain features aren't available when this option is set, including balancing groups,
        /// backreferences, positive and negative lookaheads and lookbehinds, and atomic groups.
        /// Capture groups are also ignored, such that the only capture available is that for
        /// the top-level match.
        /// </remarks>
        NonBacktracking = 0x0400,

        /// <summary>
        /// Make <c>^</c>, <c>$</c>, <c>\Z</c>, and <c>.</c> recognize all common newline sequences
        /// (<c>\r\n</c>, <c>\r</c>, <c>\n</c>, <c>\v</c> (VT), <c>\f</c> (FF), and the Unicode newlines <c>\u0085</c>, <c>\u2028</c>, <c>\u2029</c>)
        /// instead of only <c>\n</c>. For more information, see the
        /// <see href="https://learn.microsoft.com/dotnet/standard/base-types/regular-expression-options">
        /// Regular Expression Options</see> article.
        /// </summary>
        AnyNewLine = 0x0800,
    }
}
