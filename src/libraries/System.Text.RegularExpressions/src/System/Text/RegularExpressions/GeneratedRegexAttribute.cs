// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace System.Text.RegularExpressions;

/// <summary>Instructs the System.Text.RegularExpressions source generator to generate an implementation of the specified regular expression.</summary>
/// <remarks>The generator associated with this attribute only supports C#.  It only supplies an implementation when applied to static, partial, parameterless, non-generic methods that are typed to return <see cref="Regex"/>.</remarks>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class GeneratedRegexAttribute : Attribute
{
    /// <summary>Initializes a new instance of the <see cref="GeneratedRegexAttribute"/> with the specified pattern.</summary>
    /// <param name="pattern">The regular expression pattern to match.</param>
    public GeneratedRegexAttribute([StringSyntax(StringSyntaxAttribute.Regex)] string pattern) : this (pattern, RegexOptions.None)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="GeneratedRegexAttribute"/> with the specified pattern and options.</summary>
    /// <param name="pattern">The regular expression pattern to match.</param>
    /// <param name="options">A bitwise combination of the enumeration values that modify the regular expression.</param>
    public GeneratedRegexAttribute([StringSyntax(StringSyntaxAttribute.Regex, nameof(options))] string pattern, RegexOptions options) : this (pattern, options, Timeout.Infinite)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="GeneratedRegexAttribute"/> with the specified pattern, options, and timeout.</summary>
    /// <param name="pattern">The regular expression pattern to match.</param>
    /// <param name="options">A bitwise combination of the enumeration values that modify the regular expression.</param>
    /// <param name="matchTimeoutMilliseconds">A time-out interval (milliseconds), or <see cref="Timeout.Infinite"/> to indicate that the method should not time out.</param>
    public GeneratedRegexAttribute([StringSyntax(StringSyntaxAttribute.Regex, nameof(options))] string pattern, RegexOptions options, int matchTimeoutMilliseconds)
    {
        Pattern = pattern;
        Options = options;
        MatchTimeoutMilliseconds = matchTimeoutMilliseconds;
    }

    /// <summary>Gets the regular expression pattern to match.</summary>
    public string Pattern { get; }

    /// <summary>Gets a bitwise combination of the enumeration values that modify the regular expression.</summary>
    public RegexOptions Options { get; }

    /// <summary>Gets a time-out interval (milliseconds), or <see cref="Timeout.Infinite"/> to indicate that the method should not time out.</summary>
    public int MatchTimeoutMilliseconds { get; }
}
