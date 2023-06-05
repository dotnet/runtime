// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Xunit;
using VerifyCS = Microsoft.Interop.UnitTests.Verifiers.CSharpCodeFixVerifier<
       Microsoft.Interop.Analyzers.AddGeneratedComClassAnalyzer,
          Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

namespace ComInterfaceGenerator.Unit.Tests
{
    public class AddGeneratedComClassTests
    {
        [Fact]
        public async Task TypeThatImplementsGeneratedComInterfaceType_ReportsDiagnostic()
        {
            string source = """
               using System.Runtime.InteropServices;

               [GeneratedComInterface]
               public interface I
               {
               }

               class [|C|] : I
               {
               }
               """;

            string fixedSource = """
                using System.Runtime.InteropServices;
                
                [GeneratedComInterface]
                public interface I
                {
                }

                [GeneratedComClass]
                class C : I
                {
                }
                """;

            await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
        }

        [Fact]
        public async Task TypeThatDoesNotImplementGeneratedComInterfaceType_DoesNotReportDiagnostic()
        {
            string source = """
                using System.Runtime.InteropServices;

                [ComImport]
                public interface I
                {
                }

                public interface NonComInterface
                {
                }

                class C : I
                {
                }
                """;

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }
    }
}
