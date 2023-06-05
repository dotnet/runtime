// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Xunit;
using VerifyCS = Microsoft.Interop.UnitTests.Verifiers.CSharpAnalyzerVerifier<
       Microsoft.Interop.Analyzers.ComHostingDoesNotSupportGeneratedComInterfaceAnalyzer>;

namespace ComInterfaceGenerator.Unit.Tests
{
    public class ComHostingDoesNotSupportGeneratedComInterfaceTests
    {
        [InlineData(true)]
        [InlineData(false)]
        [Theory]
        public async Task ComVisibleType_ComImportInterfacesOnly_DoesNotReportDiagnostic(bool enableComHosting)
        {
            string source = """
                using System.Runtime.InteropServices;

                [ComImport]
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

               [GeneratedComInterface]
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

               [GeneratedComInterface]
               [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
               public interface I
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
                        (".editorconfig", $"build_property.EnableComHosting = {enableComHosting}")
                    }
                }
            };

            return test.RunAsync();
        }
    }
}
