// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Xunit;
using VerifyCS = Microsoft.Interop.UnitTests.Verifiers.CSharpCodeFixVerifier<
       Microsoft.Interop.Analyzers.AddGeneratedComClassAnalyzer,
       Microsoft.Interop.Analyzers.AddGeneratedComClassFixer>;

namespace ComInterfaceGenerator.Unit.Tests
{
    public class AddGeneratedComClassTests
    {
        [Fact]
        public async Task TypeThatImplementsGeneratedComInterfaceType_ReportsDiagnostic()
        {
            string source = """
               using System.Runtime.InteropServices;
               using System.Runtime.InteropServices.Marshalling;

               [GeneratedComInterface]
               [Guid("0B7171CD-04A3-41B6-AD10-FE86D52197DD")]
               public partial interface I
               {
               }

               class [|C|] : I
               {
               }
               """;

            string fixedSource = """
                using System.Runtime.InteropServices;
                using System.Runtime.InteropServices.Marshalling;
                
                [GeneratedComInterface]
                [Guid("0B7171CD-04A3-41B6-AD10-FE86D52197DD")]
                public partial interface I
                {
                }

                [GeneratedComClass]
                partial class C : I
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
                [Guid("0B7171CD-04A3-41B6-AD10-FE86D52197DD")]
                public interface I
                {
                }

                public interface NonComInterface
                {
                }

                class C : I
                {
                }

                class D : NonComInterface
                {
                }
                """;

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [Fact]
        public async Task TypeThatImplementsGeneratedComInterfaceTypeTranstively_ReportsDiagnostic()
        {
            string source = """
                using System.Runtime.InteropServices;
                using System.Runtime.InteropServices.Marshalling;

                [GeneratedComInterface]
                [Guid("0B7171CD-04A3-41B6-AD10-FE86D52197DD")]
                public partial interface I
                {
                }

                public interface J : I
                {
                }

                class [|C|] : J
                {
                }
                """;

            string fixedSource = """
                using System.Runtime.InteropServices;
                using System.Runtime.InteropServices.Marshalling;
    
                [GeneratedComInterface]
                [Guid("0B7171CD-04A3-41B6-AD10-FE86D52197DD")]
                public partial interface I
                {
                }
                
                public interface J : I
                {
                }

                [GeneratedComClass]
                partial class C : J
                {
                }
                """;

            await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
        }

        [Fact]
        public async Task TypeThatInheritsFromGeneratedComClassType_ReportsDiagnostic()
        {
            string source = """
                using System.Runtime.InteropServices;
                using System.Runtime.InteropServices.Marshalling;

                [GeneratedComClass]
                partial class J
                {
                }

                class [|C|] : J
                {
                }
                """;

            string fixedSource = """
                using System.Runtime.InteropServices;
                using System.Runtime.InteropServices.Marshalling;
                
                [GeneratedComClass]
                partial class J
                {
                }

                [GeneratedComClass]
                partial class C : J
                {
                }
                """;

            await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
        }
    }
}
