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
        public async Task TypeWithComVisibleTrue_RemovesComVisibleAttribute()
        {
            string source = """
                using System.Runtime.InteropServices;
                using System.Runtime.InteropServices.Marshalling;

                [GeneratedComInterface]
                [Guid("0B7171CD-04A3-41B6-AD10-FE86D52197DD")]
                public partial interface I
                {
                }

                [ComVisible(true)]
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
        public async Task TypeWithComVisibleFalse_PreservesComVisibleAttribute()
        {
            string source = """
                using System.Runtime.InteropServices;
                using System.Runtime.InteropServices.Marshalling;

                [GeneratedComInterface]
                [Guid("0B7171CD-04A3-41B6-AD10-FE86D52197DD")]
                public partial interface I
                {
                }

                [ComVisible(false)]
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

                [ComVisible(false)]
                [GeneratedComClass]
                partial class C : I
                {
                }
                """;

            await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
        }

        [Fact]
        public async Task TypeWithComVisibleTrueOnSecondPartialDeclaration_RemovesComVisibleAttribute()
        {
            string source = """
                using System.Runtime.InteropServices;
                using System.Runtime.InteropServices.Marshalling;

                [GeneratedComInterface]
                [Guid("0B7171CD-04A3-41B6-AD10-FE86D52197DD")]
                public partial interface I
                {
                }

                partial class [|C|] : I
                {
                }

                [ComVisible(true)]
                partial class C
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

                partial class C
                {
                }
                """;

            await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
        }

        [Fact]
        public async Task TypeWithComVisibleTrueInSeparateFile_RemovesComVisibleAttribute()
        {
            string mainSource = """
                using System.Runtime.InteropServices;
                using System.Runtime.InteropServices.Marshalling;

                [GeneratedComInterface]
                [Guid("0B7171CD-04A3-41B6-AD10-FE86D52197DD")]
                public partial interface I
                {
                }

                partial class [|C|] : I
                {
                }
                """;

            string mainFixedSource = """
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

            string secondSource = """
                using System.Runtime.InteropServices;

                [ComVisible(true)]
                partial class C
                {
                }
                """;

            string secondFixedSource = """
                using System.Runtime.InteropServices;

                partial class C
                {
                }
                """;

            var test = new VerifyCS.Test
            {
                TestCode = mainSource,
                FixedCode = mainFixedSource,
            };
            test.TestState.Sources.Add(("C.Second.cs", secondSource));
            test.FixedState.Sources.Add(("C.Second.cs", secondFixedSource));
            await test.RunAsync();
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
