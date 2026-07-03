// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Threading.Tasks;
using ILLink.Shared;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;
using Xunit;
using VerifyCS = ILLink.RoslynAnalyzer.Tests.CSharpCodeFixVerifier<
    ILLink.RoslynAnalyzer.MissingUnsafeContextAnalyzer,
    ILLink.CodeFix.AddUnsafeContextCodeFixProvider>;

namespace ILLink.RoslynAnalyzer.Tests
{
    public class MissingUnsafeContextCodeFixTests
    {
        internal static Task VerifyCodeFix(string source, string fixedSource)
        {
            var test = new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = fixedSource,
                CompilerDiagnostics = CompilerDiagnostics.None,
            };
            test.SolutionTransforms.Add(UnnecessaryUnsafeModifierTests.SetOptions);
            test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", SourceText.From($"""
                is_global = true
                build_property.{MSBuildPropertyOptionNames.EnableUnsafeAnalyzer} = true
                """)));
            return test.RunAsync();
        }

        [Fact]
        public Task WrapsVoidCallInUnsafeBlock() => VerifyCodeFix(
            """
            class C
            {
                static unsafe void M1() { }
                void M2()
                {
                    {|IL5006:M1()|};
                }
            }
            """,
            """
            class C
            {
                static unsafe void M1() { }
                void M2()
                {
                    unsafe
                    {
                        // SAFETY: Audit
                        M1();
                    }
                }
            }
            """);

        [Fact]
        public Task WrapsValueCallInUnsafeExpression() => VerifyCodeFix(
            """
            class C
            {
                static unsafe int M1() => 0;
                void M2()
                {
                    int x = {|IL5006:M1()|};
                    _ = x;
                }
            }
            """,
            """
            class C
            {
                static unsafe int M1() => 0;
                void M2()
                {
                    int x = /* SAFETY: Audit */ unsafe(M1());
                    _ = x;
                }
            }
            """);

        [Fact]
        public Task WrapsStackallocSpanInUnsafeExpression() => VerifyCodeFix(
            """
            using System;
            using System.Runtime.CompilerServices;
            class C
            {
                [SkipLocalsInit]
                static int M()
                {
                    Span<byte> s = {|IL5006:stackalloc byte[10]|};
                    return s.Length;
                }
            }
            """,
            """
            using System;
            using System.Runtime.CompilerServices;
            class C
            {
                [SkipLocalsInit]
                static int M()
                {
                    Span<byte> s = /* SAFETY: Audit */ unsafe(stackalloc byte[10]);
                    return s.Length;
                }
            }
            """);

        [Fact]
        public Task ConvertsVoidExpressionBodiedMemberToBlock() => VerifyCodeFix(
            """
            class C
            {
                static unsafe void M1() { }
                void M2() => {|IL5006:M1()|};
            }
            """,
            """
            class C
            {
                static unsafe void M1() { }
                void M2()
                {
                    unsafe
                    {
                        // SAFETY: Audit
                        M1();
                    }
                }
            }
            """);

        [Fact]
        public Task WrapsCallWithOutVarInExpressionKeepingScope() => VerifyCodeFix(
            """
            class C
            {
                static unsafe int M1(out int h) { h = 1; return 0; }
                int M2()
                {
                    int x = {|IL5006:M1(out var h)|};
                    return x + h;
                }
            }
            """,
            """
            class C
            {
                static unsafe int M1(out int h) { h = 1; return 0; }
                int M2()
                {
                    int x = /* SAFETY: Audit */ unsafe(M1(out var h));
                    return x + h;
                }
            }
            """);
    }
}
#endif
