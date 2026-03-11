// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.RegularExpressions.Generator
{
    /// <summary>
    /// Immutable, structurally equatable representation of a type that declares a regex member.
    /// Mirrors the data in the generator's internal <c>RegexType</c> record.
    /// </summary>
    internal sealed record RegexTypeSpec(
        string Keyword,
        string Namespace,
        string Name,
        RegexTypeSpec? Parent);
}
