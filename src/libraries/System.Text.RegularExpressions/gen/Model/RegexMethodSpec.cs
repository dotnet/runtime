// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.RegularExpressions.Generator
{
    /// <summary>
    /// Incremental cache key for a parsed regex method.
    /// </summary>
    public readonly partial struct RegexMethodSpec
    {
        internal RegexMethodSpec(RegexGenerator.RegexMethod method)
        {
            Method = method;
        }

        internal RegexGenerator.RegexMethod Method { get; }
    }
}
