// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using static Microsoft.Interop.Analyzers.CustomTypeMarshallerAnalyzer;

using VerifyCS = LibraryImportGenerator.UnitTests.Verifiers.CSharpCodeFixVerifier<
    Microsoft.Interop.Analyzers.CustomTypeMarshallerAnalyzer,
    Microsoft.Interop.Analyzers.CustomTypeMarshallerFixer>;

namespace LibraryImportGenerator.UnitTests
{
    [ActiveIssue("https://github.com/dotnet/runtime/issues/60650", TestRuntimes.Mono)]
    public class CustomTypeMarshallerFixerTests
    {
    }
}
