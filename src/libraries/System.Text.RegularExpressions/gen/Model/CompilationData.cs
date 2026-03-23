// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis.CSharp;

namespace System.Text.RegularExpressions.Generator
{
    internal record struct CompilationData(bool AllowUnsafe, bool CheckOverflow, LanguageVersion LanguageVersion);
}
