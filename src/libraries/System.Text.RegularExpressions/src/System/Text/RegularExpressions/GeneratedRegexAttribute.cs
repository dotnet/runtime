// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace System.Text.RegularExpressions;

/// <summary>Instructs the System.Text.RegularExpressions source generator to generate an implementation of the specified regular expression.</summary>
/// <remarks>
/// <para>
/// The generator associated with this attribute only supports C#.  It only supplies an implementation when applied to static, partial, parameterless, non-generic methods that
/// are typed to return <see cref="Regex"/>.
/// </para>
/// <para>
/// When the <see cref="Regex"/> supports case-insensitive matches (either by passing <see cref="RegexOptions.IgnoreCase"/> or using the inline `(?i)` switch in the pattern) the regex
/// engines will use an internal casing table to transform the passed in pattern into an equivalent case-sensitive one. For example, given the pattern `abc`, the engines
/// will transform it to the equivalent pattern `[Aa][Bb][Cc]`. The equivalences found in this internal casing table can change over time, for example in the case new characters are added to
/// a new version of Unicode. When using the source generator, this transformation happens at compile time, which means the casing table used to find the
/// equivalences will depend on the target framework at compile time. This differs from the rest of the <see cref="Regex"/> engines, which perform this transformation at run-time, meaning
/// they will always use casing table for the current runtime.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class GeneratedRegexAttribute : Attribute
{
    /// <summary>Initializes a new instance of the <see cref="GeneratedRegexAttribute"/> with the specified pattern.</summary>
    /// <param name="pattern">The regular expression pattern to match.</param>
    public GeneratedRegexAttribute([StringSyntax(StringSyntaxAttribute.Regex)] string pattern) : this(pattern, RegexOptions.None)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="GeneratedRegexAttribute"/> with the specified pattern and options.</summary>
    /// <param name="pattern">The regular expression pattern to match.</param>
    /// <param name="options">A bitwise combination of the enumeration values that modify the regular expression.</param>
    public GeneratedRegexAttribute([StringSyntax(StringSyntaxAttribute.Regex, nameof(options))] string pattern, RegexOptions options) : this(pattern, options, Timeout.Infinite)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="GeneratedRegexAttribute"/> with the specified pattern and options.</summary>
    /// <param name="pattern">The regular expression pattern to match.</param>
    /// <param name="options">A bitwise combination of the enumeration values that modify the regular expression.</param>
    /// <param name="cultureName">The name of a culture to be used for case sensitive comparisons. <paramref name="cultureName"/> is not case-sensitive.</param>
    /// <remarks>
    /// For a list of predefined culture names on Windows systems, see the Language tag column in the <see href="https://docs.microsoft.com/en-us/openspecs/windows_protocols/ms-lcid/a9eac961-e77d-41a6-90a5-ce1a8b0cdb9c">list of
    /// language/region names suported by Windows</see>. Culture names follow the standard defined by <see href="https://tools.ietf.org/html/bcp47">BCP 47</see>. In addition,
    /// starting with Windows 10, <paramref name="cultureName"/> can be any valid BCP-47 language tag.
    ///
    /// If <paramref name="cultureName"/> is <see cref="string.Empty"/>, the invariant culture will be used.
    /// </remarks>
    public GeneratedRegexAttribute([StringSyntax(StringSyntaxAttribute.Regex, nameof(options))] string pattern, RegexOptions options, string cultureName) : this(pattern, options, Timeout.Infinite, cultureName)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="GeneratedRegexAttribute"/> with the specified pattern, options, and timeout.</summary>
    /// <param name="pattern">The regular expression pattern to match.</param>
    /// <param name="options">A bitwise combination of the enumeration values that modify the regular expression.</param>
    /// <param name="matchTimeoutMilliseconds">A time-out interval (milliseconds), or <see cref="Timeout.Infinite"/> to indicate that the method should not time out.</param>
    public GeneratedRegexAttribute([StringSyntax(StringSyntaxAttribute.Regex, nameof(options))] string pattern, RegexOptions options, int matchTimeoutMilliseconds) : this(pattern, options, matchTimeoutMilliseconds, string.Empty /* Empty string means Invariant culture */)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="GeneratedRegexAttribute"/> with the specified pattern, options, and timeout.</summary>
    /// <param name="pattern">The regular expression pattern to match.</param>
    /// <param name="options">A bitwise combination of the enumeration values that modify the regular expression.</param>
    /// <param name="matchTimeoutMilliseconds">A time-out interval (milliseconds), or <see cref="Timeout.Infinite"/> to indicate that the method should not time out.</param>
    /// <param name="cultureName">The name of a culture to be used for case sensitive comparisons. <paramref name="cultureName"/> is not case-sensitive.</param>
    /// <remarks>
    /// For a list of predefined culture names on Windows systems, see the Language tag column in the <see href="https://docs.microsoft.com/en-us/openspecs/windows_protocols/ms-lcid/a9eac961-e77d-41a6-90a5-ce1a8b0cdb9c">list of
    /// language/region names suported by Windows</see>. Culture names follow the standard defined by <see href="https://tools.ietf.org/html/bcp47">BCP 47</see>. In addition,
    /// starting with Windows 10, <paramref name="cultureName"/> can be any valid BCP-47 language tag.
    ///
    /// If <paramref name="cultureName"/> is <see cref="string.Empty"/>, the invariant culture will be used.
    /// </remarks>
    public GeneratedRegexAttribute([StringSyntax(StringSyntaxAttribute.Regex, nameof(options))] string pattern, RegexOptions options, int matchTimeoutMilliseconds, string cultureName)
    {
        Pattern = pattern;
        Options = options;
        MatchTimeoutMilliseconds = matchTimeoutMilliseconds;
        CultureName = cultureName;
    }

    /// <summary>Gets the regular expression pattern to match.</summary>
    public string Pattern { get; }

    /// <summary>Gets a bitwise combination of the enumeration values that modify the regular expression.</summary>
    public RegexOptions Options { get; }

    /// <summary>Gets a time-out interval (milliseconds), or <see cref="Timeout.Infinite"/> to indicate that the method should not time out.</summary>
    public int MatchTimeoutMilliseconds { get; }

    /// <summary>Gets the name of the culture to be used for case sensitive comparisons.</summary>
    public string CultureName { get; }
}
