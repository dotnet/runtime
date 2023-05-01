// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Interop;
using Microsoft.Interop.Analyzers;
using Xunit;

using VerifyCS = LibraryImportGenerator.UnitTests.Verifiers.CSharpAnalyzerVerifier<Microsoft.Interop.Analyzers.GeneratedComInterfaceAttributeAnalyzer>;

namespace ComInterfaceGenerator.Unit.Tests
{
    [ActiveIssue("https://github.com/dotnet/runtime/issues/60650", TestRuntimes.Mono)]
    public class GeneratedComInterfaceAnalyzerTests
    {
        static string _usings = $$"""
            #pragma warning disable CS8019
            using System.Runtime.InteropServices.Marshalling;
            using System.Runtime.InteropServices;
            #pragma warning restore CS8019
            """;

        [ActiveIssue("https://github.com/dotnet/runtime/issues/60650", TestRuntimes.Mono)]
        public class InterfaceHasInterfaceTypeAttributeOnly
        {
            [Fact]
            public async Task IUnknown()
            {
                string snippet = $$$"""

                    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
                    interface IFoo
                    {
                        void Bar() {}
                    }
                    """;
                await VerifyCS.VerifyAnalyzerAsync(_usings + snippet);
            }

            [Fact]
            public async Task IUnknownShort()
            {
                string snippet = $$$"""

                    [InterfaceTypeAttribute((short)1)]
                    interface IFoo
                    {
                        void Bar() {}
                    }
                    """;
                await VerifyCS.VerifyAnalyzerAsync(_usings + snippet);
            }

            [Fact]
            public async Task IDispatch()
            {
                string snippet =
                    $$$"""

                [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIDispatch)]
                interface IFoo
                {
                    void Bar() {}
                }
                """;
                await VerifyCS.VerifyAnalyzerAsync(_usings + snippet);
            }

            [Fact]
            public async Task IDispatchShort()
            {
                string snippet =
                    $$$"""

                [InterfaceTypeAttribute((short)2)]
                interface IFoo
                {
                    void Bar() {}
                }
                """;
                await VerifyCS.VerifyAnalyzerAsync(_usings + snippet);
            }

            [Fact]
            public async Task IInspectable()
            {
                string snippet =
                    $$$"""

                [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIInspectable)]
                interface IFoo
                {
                    void Bar() {}
                }
                """;
                await VerifyCS.VerifyAnalyzerAsync(_usings + snippet);
            }

            [Fact]
            public async Task IInspectableShort()
            {
                string snippet =
                    $$$"""

                [InterfaceTypeAttribute((short)3)]
                interface IFoo
                {
                    void Bar() {}
                }
                """;
                await VerifyCS.VerifyAnalyzerAsync(_usings + snippet);
            }

            [Fact]
            public async Task IDual()
            {
                string snippet =
                    $$$"""

                [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsDual)]
                interface IFoo
                {
                    void Bar() {}
                }
                """;
                await VerifyCS.VerifyAnalyzerAsync(_usings + snippet);
            }

            [Fact]
            public async Task IDualShort()
            {
                string snippet =
                    $$$"""

                [InterfaceTypeAttribute((short)0)]
                interface IFoo
                {
                    void Bar() {}
                }
                """;
                await VerifyCS.VerifyAnalyzerAsync(_usings + snippet);
            }
        }

        [ActiveIssue("https://github.com/dotnet/runtime/issues/60650", TestRuntimes.Mono)]
        public class InterfaceHasGeneratedComInterfaceAttributeOnly
        {
            [Fact]
            public async Task Test()
            {
                string snippet =
                    $$$"""

                [GeneratedComInterface]
                interface IFoo
                {
                    void Bar() {}
                }
                """;
                await VerifyCS.VerifyAnalyzerAsync(_usings + snippet);
            }
        }

        [ActiveIssue("https://github.com/dotnet/runtime/issues/60650", TestRuntimes.Mono)]
        public class InterfaceHasGeneratedComInterfaceAttributeAndInterfaceTypeAttribute
        {
            [Fact]
            public async Task IUnknown()
            {
                string snippet =
                    $$$"""

                [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
                [GeneratedComInterface]
                interface IFoo
                {
                    void Bar() {}
                }
                """;
                await VerifyCS.VerifyAnalyzerAsync(_usings + snippet);
            }

            [Fact]
            public async Task IUnknownShort()
            {
                string snippet =
                    $$$"""

                [InterfaceTypeAttribute((short)1)]
                [GeneratedComInterface]
                interface IFoo
                {
                    void Bar() {}
                }
                """;
                await VerifyCS.VerifyAnalyzerAsync(_usings + snippet);
            }

            [Fact]
            public async Task IDispatch()
            {
                string snippet =
                    $$$"""

                [{|#0:InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIDispatch)|}]
                [GeneratedComInterface]
                interface IFoo
                {
                    void Bar() {}
                }
                """;
                await VerifyCS.VerifyAnalyzerAsync(
                    _usings + snippet,
                    VerifyCS.Diagnostic(AnalyzerDiagnostics.InterfaceTypeNotSupported)
                        .WithLocation(0)
                        .WithArguments(TypeNames.ComInterfaceTypeAttribute + "." + nameof(ComInterfaceType.InterfaceIsIDispatch)));
            }

            [Fact]
            public async Task IDispatchShort()
            {
                string snippet =
                    $$$"""

                [{|#0:InterfaceTypeAttribute((short)2)|}]
                [GeneratedComInterface]
                interface IFoo
                {
                    void Bar() {}
                }
                """;
                await VerifyCS.VerifyAnalyzerAsync(
                    _usings + snippet,
                    VerifyCS.Diagnostic(AnalyzerDiagnostics.InterfaceTypeNotSupported)
                        .WithLocation(0)
                        .WithArguments("2"));
            }

            [Fact]
            public async Task IInspectable()
            {
                string snippet =
                    $$$"""

                [{|#0:InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIInspectable)|}]
                [GeneratedComInterface]
                interface IFoo
                {
                    void Bar() {}
                }
                """;
                await VerifyCS.VerifyAnalyzerAsync(
                    _usings + snippet,
                    VerifyCS.Diagnostic(AnalyzerDiagnostics.InterfaceTypeNotSupported)
                        .WithLocation(0)
                        .WithArguments(TypeNames.ComInterfaceTypeAttribute + "." + nameof(ComInterfaceType.InterfaceIsIInspectable)));
            }

            [Fact]
            public async Task IInspectableShort()
            {
                string snippet =
                    $$$"""

                [{|#0:InterfaceTypeAttribute((short)3)|}]
                [GeneratedComInterface]
                interface IFoo
                {
                    void Bar() {}
                }
                """;
                await VerifyCS.VerifyAnalyzerAsync(
                    _usings + snippet,
                    VerifyCS.Diagnostic(AnalyzerDiagnostics.InterfaceTypeNotSupported)
                        .WithLocation(0)
                        .WithArguments("3"));
            }

            [Fact]
            public async Task IDual()
            {
                string snippet =
                    $$$"""

                [{|#0:InterfaceTypeAttribute(ComInterfaceType.InterfaceIsDual)|}]
                [GeneratedComInterface]
                interface IFoo
                {
                    void Bar() {}
                }
                """;
                await VerifyCS.VerifyAnalyzerAsync(
                    _usings + snippet,
                    VerifyCS.Diagnostic(AnalyzerDiagnostics.InterfaceTypeNotSupported)
                        .WithLocation(0)
                        .WithArguments(TypeNames.ComInterfaceTypeAttribute + "." + nameof(ComInterfaceType.InterfaceIsDual)));
            }

            [Fact]
            public async Task IDualShort()
            {
                string snippet =
                    $$$"""

                [{|#0:InterfaceTypeAttribute((short)0)|}]
                [GeneratedComInterface]
                interface IFoo
                {
                    void Bar() {}
                }
                """;
                await VerifyCS.VerifyAnalyzerAsync(
                    _usings + snippet,
                    VerifyCS.Diagnostic(AnalyzerDiagnostics.InterfaceTypeNotSupported)
                        .WithLocation(0)
                        .WithArguments("0"));
            }
        }

        [ActiveIssue("https://github.com/dotnet/runtime/issues/60650", TestRuntimes.Mono)]
        public class PartialInterfaceHasGeneratedComInterfaceAttributeAndInterfaceTypeAttribute
        {
            [Fact]
            public async Task IUnknown()
            {
                string snippet =
                    $$$"""

                [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
                partial interface IFoo
                {
                    void Bar() {}
                }

                [GeneratedComInterface]
                partial interface IFoo { }
                """;
                await VerifyCS.VerifyAnalyzerAsync(_usings + snippet);
            }

            [Fact]
            public async Task IUnknownShort()
            {
                string snippet =
                    $$$"""

                [InterfaceTypeAttribute((short)1)]
                partial interface IFoo
                {
                    void Bar() {}
                }

                [GeneratedComInterface]
                partial interface IFoo { }
                """;
                await VerifyCS.VerifyAnalyzerAsync(_usings + snippet);
            }

            [Fact]
            public async Task IDispatch()
            {
                string snippet =
                    $$$"""

                [{|#0:InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIDispatch)|}]
                partial interface IFoo
                {
                    void Bar() {}
                }

                [GeneratedComInterface]
                partial interface IFoo { }
                """;
                await VerifyCS.VerifyAnalyzerAsync(
                    _usings + snippet,
                    VerifyCS.Diagnostic(AnalyzerDiagnostics.InterfaceTypeNotSupported)
                        .WithLocation(0)
                        .WithArguments(TypeNames.ComInterfaceTypeAttribute + "." + nameof(ComInterfaceType.InterfaceIsIDispatch)));
            }

            [Fact]
            public async Task IDispatchShort()
            {
                string snippet =
                    $$$"""

                [{|#0:InterfaceTypeAttribute((short)2)|}]
                partial interface IFoo
                {
                    void Bar() {}
                }

                [GeneratedComInterface]
                partial interface IFoo { }
                """;
                await VerifyCS.VerifyAnalyzerAsync(
                    _usings + snippet,
                    VerifyCS.Diagnostic(AnalyzerDiagnostics.InterfaceTypeNotSupported)
                        .WithLocation(0)
                        .WithArguments("2"));
            }

            [Fact]
            public async Task IInspectable()
            {
                string snippet =
                    $$$"""

                [{|#0:InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIInspectable)|}]
                partial interface IFoo
                {
                    void Bar() {}
                }

                [GeneratedComInterface]
                partial interface IFoo { }
                """;
                await VerifyCS.VerifyAnalyzerAsync(
                    _usings + snippet,
                    VerifyCS.Diagnostic(AnalyzerDiagnostics.InterfaceTypeNotSupported)
                        .WithLocation(0)
                        .WithArguments(TypeNames.ComInterfaceTypeAttribute + "." + nameof(ComInterfaceType.InterfaceIsIInspectable)));
            }

            [Fact]
            public async Task IInspectableShort()
            {
                string snippet =
                    $$$"""

                [{|#0:InterfaceTypeAttribute((short)3)|}]
                partial interface IFoo
                {
                    void Bar() {}
                }

                [GeneratedComInterface]
                partial interface IFoo { }
                """;
                await VerifyCS.VerifyAnalyzerAsync(
                    _usings + snippet,
                    VerifyCS.Diagnostic(AnalyzerDiagnostics.InterfaceTypeNotSupported)
                        .WithLocation(0)
                        .WithArguments("3"));
            }

            [Fact]
            public async Task IDual()
            {
                string snippet =
                    $$$"""

                [{|#0:InterfaceTypeAttribute(ComInterfaceType.InterfaceIsDual)|}]
                partial interface IFoo
                {
                    void Bar() {}
                }

                [GeneratedComInterface]
                partial interface IFoo { }
                """;
                await VerifyCS.VerifyAnalyzerAsync(
                    _usings + snippet,
                    VerifyCS.Diagnostic(AnalyzerDiagnostics.InterfaceTypeNotSupported)
                        .WithLocation(0)
                        .WithArguments(TypeNames.ComInterfaceTypeAttribute + "." + nameof(ComInterfaceType.InterfaceIsDual)));
            }

            [Fact]
            public async Task IDualShort()
            {
                string snippet =
                    $$$"""

                [{|#0:InterfaceTypeAttribute((short)0)|}]
                partial interface IFoo
                {
                    void Bar() {}
                }

                [GeneratedComInterface]
                partial interface IFoo { }
                """;
                await VerifyCS.VerifyAnalyzerAsync(
                    _usings + snippet,
                    VerifyCS.Diagnostic(AnalyzerDiagnostics.InterfaceTypeNotSupported)
                        .WithLocation(0)
                        .WithArguments("0"));
            }
        }
    }
}
