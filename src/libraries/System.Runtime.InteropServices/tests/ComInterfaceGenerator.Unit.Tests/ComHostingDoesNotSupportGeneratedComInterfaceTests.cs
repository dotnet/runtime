// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Xunit;
using VerifyCS = Microsoft.Interop.UnitTests.Verifiers.CSharpAnalyzerVerifier<
       Microsoft.Interop.Analyzers.ComHostingDoesNotSupportGeneratedComInterfaceAnalyzer>;

namespace ComInterfaceGenerator.Unit.Tests
{
    [ActiveIssue("https://github.com/dotnet/runtime/issues/60650", TestRuntimes.Mono)]
    public class ComHostingDoesNotSupportGeneratedComInterfaceTests
    {
        [InlineData(true)]
        [InlineData(false)]
        [Theory]
        public async Task ComVisibleType_ComImportInterfacesOnly_DoesNotReportDiagnostic(bool enableComHosting)
        {
            string source = """
                using System.Runtime.InteropServices;

                [ComVisible(true)]
                [Guid("12D46FF1-E21A-45E4-8407-0573B30962FE")]
                [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
                public interface I
                {
                }

                [ComVisible(true)]
                public class C : I
                {
                }
                                
                """;

            await VerifyAnalyzerAsync(source, enableComHosting);
        }

        [Fact]
        public async Task ComVisibleType_GeneratedComInterface_NoHosting_DoesNotReportDiagnostic()
        {
            string source = """
               using System.Runtime.InteropServices;
               using System.Runtime.InteropServices.Marshalling;

               [GeneratedComInterface]
               [Guid("0B7171CD-04A3-41B6-AD10-FE86D52197DD")]
               [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
               public interface I
               {
               }

               [ComVisible(true)]
               public class C : I
               {
               }
       
               """;

            await VerifyAnalyzerAsync(source, enableComHosting: false);
        }

        [Fact]
        public async Task ComVisibleType_GeneratedComInterface_EnabledHosting_ReportsDiagnostic()
        {
            string source = """
               using System.Runtime.InteropServices;
               using System.Runtime.InteropServices.Marshalling;

               [GeneratedComInterface]
               [Guid("0B7171CD-04A3-41B6-AD10-FE86D52197DD")]
               [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
               public partial interface I
               {
               }

               [ComVisible(true)]
               public class [|C|] : I
               {
               }
       
               """;

            await VerifyAnalyzerAsync(source, enableComHosting: true);
        }

        [Fact]
        public async Task ComVisibleType_GeneratedComInterface_TransitiveInterface_EnabledHosting_ReportsDiagnostic()
        {
            string source = """
               using System.Runtime.InteropServices;
               using System.Runtime.InteropServices.Marshalling;

               [GeneratedComInterface]
               [Guid("0B7171CD-04A3-41B6-AD10-FE86D52197DD")]
               [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
               public partial interface I
               {
               }

               public interface J : I
               {
               }

               [ComVisible(true)]
               public class [|C|] : I
               {
               }
       
               """;

            await VerifyAnalyzerAsync(source, enableComHosting: true);
        }

        private static Task VerifyAnalyzerAsync(string source, bool enableComHosting)
        {
            var test = new VerifyCS.Test
            {
                TestState =
                {
                    Sources = { source },
                    AnalyzerConfigFiles =
                    {
                        ("/.editorconfig", $"""
                                            is_global = true
                                            build_property.EnableComHosting = {enableComHosting}
                                            """)
                    }
                }
            };

            return test.RunAsync();
        }
    }
}
