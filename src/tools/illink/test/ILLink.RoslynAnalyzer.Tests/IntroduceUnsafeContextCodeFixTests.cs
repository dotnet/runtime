// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;
using VerifyCS = ILLink.RoslynAnalyzer.Tests.CSharpCodeFixVerifier<
    ILLink.RoslynAnalyzer.DynamicallyAccessedMembersAnalyzer,
    ILLink.CodeFix.IntroduceUnsafeContextCodeFixProvider>;

namespace ILLink.RoslynAnalyzer.Tests
{
    public class IntroduceUnsafeContextCodeFixTests
    {
        private static Solution SetOptions(Solution solution, ProjectId projectId)
        {
            Project project = solution.GetProject(projectId)!;
            var parseOptions = (CSharpParseOptions)project.ParseOptions!;
            parseOptions = parseOptions
                .WithLanguageVersion(LanguageVersion.Preview)
                .WithFeatures([.. parseOptions.Features, new("updated-memory-safety-rules", "")]);

            var compilationOptions = (CSharpCompilationOptions)project.CompilationOptions!;
            compilationOptions = compilationOptions.WithAllowUnsafe(true);

            return solution
                .WithProjectParseOptions(projectId, parseOptions)
                .WithProjectCompilationOptions(projectId, compilationOptions);
        }

        private static Task VerifyCodeFix(
            string source,
            string fixedSource,
            int codeActionIndex = 0,
            string? batchFixedSource = null)
        {
            var test = new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = fixedSource,
                BatchFixedCode = batchFixedSource ?? fixedSource,
                CodeActionIndex = codeActionIndex,
            };

            test.SolutionTransforms.Add(SetOptions);
            return test.RunAsync();
        }

        /// <summary>
        /// Verifies that no fix is offered, because neither shape of unsafe context would compile.
        /// </summary>
        private static Task VerifyNoCodeFix(string source)
        {
            var test = new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = source,
            };
            test.SolutionTransforms.Add(SetOptions);
            return test.RunAsync();
        }

        [Fact]
        public async Task ExpressionStatementIsWrappedInABlock()
        {
            await VerifyCodeFix(
                """
                public class C
                {
                    public static unsafe void Unsafe() { }

                    public void M()
                    {
                        {|CS9362:Unsafe()|};
                    }
                }
                """,
                """
                public class C
                {
                    public static unsafe void Unsafe() { }

                    public void M()
                    {
                        unsafe
                        {
                            Unsafe();
                        }
                    }
                }
                """);
        }

        [Fact]
        public async Task LocalDeclarationIsSplitSoTheLocalStaysVisible()
        {
            await VerifyCodeFix(
                """
                public class C
                {
                    public static unsafe int Unsafe() => 0;

                    public int M()
                    {
                        int x = {|CS9362:Unsafe()|};
                        return x;
                    }
                }
                """,
                """
                public class C
                {
                    public static unsafe int Unsafe() => 0;

                    public int M()
                    {
                        int x;
                        unsafe
                        {
                            x = Unsafe();
                        }
                        return x;
                    }
                }
                """);
        }

        [Fact]
        public async Task StackAllocDeclarationKeepsItsInitializer()
        {
            // A ref struct local's ref-safety scope comes from its initializer. Re-declaring it without one
            // cannot reproduce that scope: leaving 'scoped' off lets it escape to the caller, and adding 'scoped'
            // narrows it to the enclosing block, which is narrower than the current method 'stackalloc' implies.
            await VerifyCodeFix(
                """
                using System;
                using System.Runtime.CompilerServices;

                public class C
                {
                    [SkipLocalsInit]
                    public int M()
                    {
                        Span<byte> s = {|CS9361:stackalloc byte[10]|};
                        return s.Length;
                    }
                }
                """,
                """
                using System;
                using System.Runtime.CompilerServices;

                public class C
                {
                    [SkipLocalsInit]
                    public int M()
                    {
                        Span<byte> s = unsafe(stackalloc byte[10]);
                        return s.Length;
                    }
                }
                """);
        }

        [Fact]
        public async Task ExpressionBodiedMemberUsesTheExpressionForm()
        {
            await VerifyCodeFix(
                """
                public class C
                {
                    public static unsafe int Unsafe() => 0;

                    public int M() => {|CS9362:Unsafe()|};
                }
                """,
                """
                public class C
                {
                    public static unsafe int Unsafe() => 0;

                    public int M() => unsafe(Unsafe());
                }
                """);
        }

        [Fact]
        public async Task AwaitInTheStatementForcesTheExpressionForm()
        {
            await VerifyCodeFix(
                """
                using System.Threading.Tasks;

                public class C
                {
                    public static unsafe int Unsafe() => 0;

                    public async Task<int> M()
                    {
                        int x = {|CS9362:Unsafe()|} + await Task.FromResult(1);
                        return x;
                    }
                }
                """,
                """
                using System.Threading.Tasks;

                public class C
                {
                    public static unsafe int Unsafe() => 0;

                    public async Task<int> M()
                    {
                        int x = unsafe(Unsafe()) + await Task.FromResult(1);
                        return x;
                    }
                }
                """);
        }

        [Fact]
        public async Task DenseBodyIsWrappedWholeWhenEveryUseSiteIsFixedAtOnce()
        {
            await VerifyCodeFix(
                """
                public class C
                {
                    public static unsafe void Unsafe() { }

                    public void M()
                    {
                        {|CS9362:Unsafe()|};
                        {|CS9362:Unsafe()|};
                        {|CS9362:Unsafe()|};
                    }
                }
                """,
                """
                public class C
                {
                    public static unsafe void Unsafe() { }

                    public void M()
                    {
                        unsafe
                        {
                            Unsafe();
                        }
                        unsafe
                        {
                            Unsafe();
                        }
                        unsafe
                        {
                            Unsafe();
                        }
                    }
                }
                """,
                // Fixing one use site at a time keeps to that use site, but a body that needs this many regions
                // reads better as a single one.
                batchFixedSource:
                """
                public class C
                {
                    public static unsafe void Unsafe() { }

                    public void M()
                    {
                        unsafe
                        {
                            Unsafe();
                            Unsafe();
                            Unsafe();
                        }
                    }
                }
                """);
        }

        [Fact]
        public async Task SparseBodyIsNotWrappedWhole()
        {
            await VerifyCodeFix(
                """
                public class C
                {
                    public static unsafe int Unsafe() => 0;

                    public int M()
                    {
                        int a = 1;
                        return a + {|CS9362:Unsafe()|};
                    }
                }
                """,
                """
                public class C
                {
                    public static unsafe int Unsafe() => 0;

                    public int M()
                    {
                        int a = 1;
                        unsafe
                        {
                            return a + Unsafe();
                        }
                    }
                }
                """);
        }

        [Fact]
        public async Task TheBodyWideFixIsOfferedAsAnAlternative()
        {
            await VerifyCodeFix(
                """
                public class C
                {
                    public static unsafe int Unsafe() => 0;

                    public int M()
                    {
                        int a = 1;
                        return a + {|CS9362:Unsafe()|};
                    }
                }
                """,
                """
                public class C
                {
                    public static unsafe int Unsafe() => 0;

                    public int M()
                    {
                        unsafe
                        {
                            int a = 1;
                            return a + Unsafe();
                        }
                    }
                }
                """,
                codeActionIndex: 1);
        }

        [Fact]
        public async Task ABodyThatCannotBeWrappedIsFixedPerUseSite()
        {
            await VerifyCodeFix(
                """
                using System.Collections.Generic;

                public class C
                {
                    public static unsafe int Unsafe() => 0;

                    public IEnumerable<int> M()
                    {
                        yield return {|CS9362:Unsafe()|};
                        yield return {|CS9362:Unsafe()|};
                        yield return {|CS9362:Unsafe()|};
                    }
                }
                """,
                """
                using System.Collections.Generic;

                public class C
                {
                    public static unsafe int Unsafe() => 0;

                    public IEnumerable<int> M()
                    {
                        yield return unsafe(Unsafe());
                        yield return unsafe(Unsafe());
                        yield return unsafe(Unsafe());
                    }
                }
                """);
        }

        [Fact]
        public async Task SeveralUseSitesInOneStatementShareOneBlock()
        {
            await VerifyCodeFix(
                """
                public class C
                {
                    public static unsafe int Unsafe() => 0;

                    public int M()
                    {
                        return {|CS9362:Unsafe()|} + {|CS9362:Unsafe()|};
                    }
                }
                """,
                """
                public class C
                {
                    public static unsafe int Unsafe() => 0;

                    public int M()
                    {
                        unsafe
                        {
                            return Unsafe() + Unsafe();
                        }
                    }
                }
                """);
        }

        [Fact]
        public async Task PointerDereferenceIsWrappedInABlock()
        {
            await VerifyCodeFix(
                """
                public class C
                {
                    public int M(int* p)
                    {
                        return {|CS9360:*|}p;
                    }
                }
                """,
                """
                public class C
                {
                    public int M(int* p)
                    {
                        unsafe
                        {
                            return *p;
                        }
                    }
                }
                """);
        }

        [Fact]
        public async Task ConstructorConstraintIsWrappedInABlock()
        {
            await VerifyCodeFix(
                """
                public class C
                {
                    public class HasUnsafeConstructor
                    {
                        public unsafe HasUnsafeConstructor() { }
                    }

                    public static T Make<T>() where T : new() => new T();

                    public void M()
                    {
                        {|CS9376:Make<HasUnsafeConstructor>()|};
                    }
                }
                """,
                """
                public class C
                {
                    public class HasUnsafeConstructor
                    {
                        public unsafe HasUnsafeConstructor() { }
                    }

                    public static T Make<T>() where T : new() => new T();

                    public void M()
                    {
                        unsafe
                        {
                            Make<HasUnsafeConstructor>();
                        }
                    }
                }
                """);
        }

        [Fact]
        public async Task EmbeddedStatementBecomesAnUnsafeBlock()
        {
            await VerifyCodeFix(
                """
                public class C
                {
                    public static unsafe void Unsafe() { }

                    public void M(bool condition)
                    {
                        if (condition)
                            {|CS9362:Unsafe()|};
                    }
                }
                """,
                """
                public class C
                {
                    public static unsafe void Unsafe() { }

                    public void M(bool condition)
                    {
                        if (condition)
                            unsafe
                            {
                                Unsafe();
                            }
                    }
                }
                """);
        }

        [Fact]
        public async Task SwitchSectionStatementIsWrappedInABlock()
        {
            await VerifyCodeFix(
                """
                public class C
                {
                    public static unsafe int Unsafe() => 0;

                    public int M(int key)
                    {
                        switch (key)
                        {
                            case 1:
                                return {|CS9362:Unsafe()|};
                            default:
                                return 0;
                        }
                    }
                }
                """,
                """
                public class C
                {
                    public static unsafe int Unsafe() => 0;

                    public int M(int key)
                    {
                        switch (key)
                        {
                            case 1:
                                unsafe
                                {
                                    return Unsafe();
                                }
                            default:
                                return 0;
                        }
                    }
                }
                """);
        }

        [Fact]
        public async Task LabelStaysOutsideTheBlock()
        {
            await VerifyCodeFix(
                """
                public class C
                {
                    public static unsafe int Unsafe() => 0;

                    public int M()
                    {
                        int a;
                    top:
                        a = {|CS9362:Unsafe()|};
                        if (a < 0)
                            goto top;
                        return a;
                    }
                }
                """,
                """
                public class C
                {
                    public static unsafe int Unsafe() => 0;

                    public int M()
                    {
                        int a;
                    top:
                        unsafe
                        {
                            a = Unsafe();
                        }
                        if (a < 0)
                            goto top;
                        return a;
                    }
                }
                """);
        }

        [Fact]
        public async Task CatchFilterUsesTheExpressionForm()
        {
            await VerifyCodeFix(
                """
                using System;

                public class C
                {
                    public static unsafe int Unsafe() => 0;

                    public void M()
                    {
                        try
                        {
                        }
                        catch (Exception) when ({|CS9362:Unsafe()|} == 0)
                        {
                        }
                    }
                }
                """,
                """
                using System;

                public class C
                {
                    public static unsafe int Unsafe() => 0;

                    public void M()
                    {
                        try
                        {
                        }
                        catch (Exception) when (unsafe(Unsafe()) == 0)
                        {
                        }
                    }
                }
                """);
        }

        [Fact]
        public async Task FieldInitializerUsesTheExpressionForm()
        {
            await VerifyCodeFix(
                """
                public class C
                {
                    public static unsafe int Unsafe() => 0;

                    private static readonly int s_value = {|CS9362:Unsafe()|};

                    public int M() => s_value;
                }
                """,
                """
                public class C
                {
                    public static unsafe int Unsafe() => 0;

                    private static readonly int s_value = unsafe(Unsafe());

                    public int M() => s_value;
                }
                """);
        }

        [Fact]
        public async Task ExpressionBodiedLocalFunctionUsesTheExpressionForm()
        {
            await VerifyCodeFix(
                """
                public class C
                {
                    public static unsafe int Unsafe() => 0;

                    public int M()
                    {
                        int Local() => {|CS9362:Unsafe()|};
                        return Local();
                    }
                }
                """,
                """
                public class C
                {
                    public static unsafe int Unsafe() => 0;

                    public int M()
                    {
                        int Local() => unsafe(Unsafe());
                        return Local();
                    }
                }
                """);
        }

        [Fact]
        public async Task LambdaInsideAStatementIsCoveredByTheBlock()
        {
            await VerifyCodeFix(
                """
                using System;

                public class C
                {
                    public static unsafe int Unsafe() => 0;

                    public int M()
                    {
                        Func<int> f = () => {|CS9362:Unsafe()|};
                        return f();
                    }
                }
                """,
                """
                using System;

                public class C
                {
                    public static unsafe int Unsafe() => 0;

                    public int M()
                    {
                        Func<int> f;
                        unsafe
                        {
                            f = () => Unsafe();
                        }
                        return f();
                    }
                }
                """);
        }

        [Fact]
        public async Task OutVariableUsedLaterForcesTheExpressionForm()
        {
            await VerifyCodeFix(
                """
                public class C
                {
                    public static unsafe int Unsafe() => 0;

                    public static bool TryGet(int input, out int result)
                    {
                        result = input;
                        return true;
                    }

                    public int M()
                    {
                        if (TryGet({|CS9362:Unsafe()|}, out int value))
                            return value;

                        return value;
                    }
                }
                """,
                """
                public class C
                {
                    public static unsafe int Unsafe() => 0;

                    public static bool TryGet(int input, out int result)
                    {
                        result = input;
                        return true;
                    }

                    public int M()
                    {
                        if (TryGet(unsafe(Unsafe()), out int value))
                            return value;

                        return value;
                    }
                }
                """);
        }

        [Fact]
        public async Task RefLocalForcesTheExpressionForm()
        {
            await VerifyCodeFix(
                """
                public class C
                {
                    public static unsafe int Unsafe() => 0;

                    public int M(int[] values)
                    {
                        ref int slot = ref values[{|CS9362:Unsafe()|}];
                        slot = 1;
                        return slot;
                    }
                }
                """,
                """
                public class C
                {
                    public static unsafe int Unsafe() => 0;

                    public int M(int[] values)
                    {
                        ref int slot = ref values[unsafe(Unsafe())];
                        slot = 1;
                        return slot;
                    }
                }
                """);
        }

        [Fact]
        public async Task UsingDeclarationForcesTheExpressionForm()
        {
            await VerifyCodeFix(
                """
                using System.IO;

                public class C
                {
                    public static unsafe int Unsafe() => 0;

                    public long M()
                    {
                        using var stream = new MemoryStream({|CS9362:Unsafe()|});
                        return stream.Length;
                    }
                }
                """,
                """
                using System.IO;

                public class C
                {
                    public static unsafe int Unsafe() => 0;

                    public long M()
                    {
                        using var stream = new MemoryStream(unsafe(Unsafe()));
                        return stream.Length;
                    }
                }
                """);
        }

        [Fact]
        public async Task RefStructLocalKeepsItsInitializer()
        {
            await VerifyCodeFix(
                """
                using System;

                public class C
                {
                    public static unsafe Span<byte> Unsafe() => default;

                    public Span<byte> M()
                    {
                        Span<byte> span = {|CS9362:Unsafe()|};
                        return span;
                    }
                }
                """,
                """
                using System;

                public class C
                {
                    public static unsafe Span<byte> Unsafe() => default;

                    public Span<byte> M()
                    {
                        Span<byte> span = unsafe(Unsafe());
                        return span;
                    }
                }
                """);
        }

        [Fact]
        public async Task DirectivesForceTheExpressionForm()
        {
            await VerifyCodeFix(
                """
                public class C
                {
                    public static unsafe int Unsafe() => 0;

                    public int M()
                    {
                #if NEVER
                        return 0;
                #else
                        return {|CS9362:Unsafe()|};
                #endif
                    }
                }
                """,
                """
                public class C
                {
                    public static unsafe int Unsafe() => 0;

                    public int M()
                    {
                #if NEVER
                        return 0;
                #else
                        return unsafe(Unsafe());
                #endif
                    }
                }
                """);
        }

        [Fact]
        public async Task ImplicitlyTypedLocalGetsAnExplicitType()
        {
            await VerifyCodeFix(
                """
                using System.Collections.Generic;

                public class C
                {
                    public static unsafe List<string> Unsafe() => null;

                    public int M()
                    {
                        var items = {|CS9362:Unsafe()|};
                        return items.Count;
                    }
                }
                """,
                """
                using System.Collections.Generic;

                public class C
                {
                    public static unsafe List<string> Unsafe() => null;

                    public int M()
                    {
                        List<string> items;
                        unsafe
                        {
                            items = Unsafe();
                        }
                        return items.Count;
                    }
                }
                """);
        }

        [Fact]
        public async Task CommentsAboveTheStatementStayAboveTheBlock()
        {
            await VerifyCodeFix(
                """
                public class C
                {
                    public static unsafe void Unsafe() { }

                    public void M()
                    {
                        // Explains the call below.
                        {|CS9362:Unsafe()|};
                    }
                }
                """,
                """
                public class C
                {
                    public static unsafe void Unsafe() { }

                    public void M()
                    {
                        // Explains the call below.
                        unsafe
                        {
                            Unsafe();
                        }
                    }
                }
                """);
        }

        [Fact]
        public async Task ConstructorInitializerUsesTheExpressionForm()
        {
            await VerifyCodeFix(
                """
                public class B
                {
                    public B(int value) { }
                }

                public class C : B
                {
                    public static unsafe int Unsafe() => 0;

                    public C() : base({|CS9362:Unsafe()|})
                    {
                    }
                }
                """,
                """
                public class B
                {
                    public B(int value) { }
                }

                public class C : B
                {
                    public static unsafe int Unsafe() => 0;

                    public C() : base(unsafe(Unsafe()))
                    {
                    }
                }
                """);
        }

        [Fact]
        public async Task ExpressionBodiedPropertyUsesTheExpressionForm()
        {
            await VerifyCodeFix(
                """
                public class C
                {
                    public static unsafe int Unsafe() => 0;

                    public int P => {|CS9362:Unsafe()|};
                }
                """,
                """
                public class C
                {
                    public static unsafe int Unsafe() => 0;

                    public int P => unsafe(Unsafe());
                }
                """);
        }

        [Fact]
        public async Task StatementInsideALambdaBlockIsWrapped()
        {
            await VerifyCodeFix(
                """
                using System;

                public class C
                {
                    public static unsafe int Unsafe() => 0;

                    public Func<int> M() => () =>
                    {
                        return {|CS9362:Unsafe()|};
                    };
                }
                """,
                """
                using System;

                public class C
                {
                    public static unsafe int Unsafe() => 0;

                    public Func<int> M() => () =>
                    {
                        unsafe
                        {
                            return Unsafe();
                        }
                    };
                }
                """);
        }

        [Fact]
        public async Task LocalDeclarationThatIsNeverReadAgainIsWrappedWhole()
        {
            await VerifyCodeFix(
                """
                public class C
                {
                    public static unsafe int Unsafe() => 0;

                    public void M()
                    {
                        int unused = {|CS9362:Unsafe()|};
                    }
                }
                """,
                """
                public class C
                {
                    public static unsafe int Unsafe() => 0;

                    public void M()
                    {
                        unsafe
                        {
                            int unused = Unsafe();
                        }
                    }
                }
                """);
        }

        [Fact]
        public async Task PropertyAccessIsWrappedTogetherWithItsConsumer()
        {
            // 'unsafe(receiver.Property)' keeps the access as a place, so the accessor call stays outside the
            // region and the diagnostic survives. The region has to cover something that reads it.
            await VerifyCodeFix(
                """
                public class C
                {
                    public unsafe int UnsafeProperty => 0;

                    public int M(C other) => Add({|CS9362:other.UnsafeProperty|}, 1);

                    private static int Add(int left, int right) => left + right;
                }
                """,
                """
                public class C
                {
                    public unsafe int UnsafeProperty => 0;

                    public int M(C other) => unsafe(Add(other.UnsafeProperty, 1));

                    private static int Add(int left, int right) => left + right;
                }
                """);
        }

        [Fact]
        public async Task ConditionalAccessIsWrappedAsAWhole()
        {
            await VerifyCodeFix(
                """
                public class C
                {
                    public unsafe string UnsafeProperty => "x";

                    public int? M(C other) => other?{|CS9362:.UnsafeProperty|}.Length;
                }
                """,
                """
                public class C
                {
                    public unsafe string UnsafeProperty => "x";

                    public int? M(C other) => unsafe(other?.UnsafeProperty.Length);
                }
                """);
        }

        [Fact]
        public async Task AwaitInsideTheUseSiteLeavesTheDiagnostic()
        {
            // Neither shape works here: a block would trap the await, and the expression form would put it in an
            // unsafe context. The developer has to restructure the statement.
            await VerifyNoCodeFix(
                """
                using System.Threading.Tasks;

                public class C
                {
                    public static unsafe int Unsafe(int value) => value;

                    public async Task<int> M()
                    {
                        int x = {|CS9362:Unsafe(await Task.FromResult(1))|};
                        return x;
                    }
                }
                """);
        }

        [Fact]
        public async Task VoidExpressionBodyLeavesTheDiagnostic()
        {
            // 'unsafe(...)' is not one of the forms allowed where a statement expression is required, so a void
            // expression body cannot be fixed without turning it into a block body.
            await VerifyNoCodeFix(
                """
                public class C
                {
                    public static unsafe void Unsafe() { }

                    public void M() => {|CS9362:Unsafe()|};
                }
                """);
        }

        [Fact]
        public async Task OutVariableInAnInitializerForcesTheExpressionForm()
        {
            // Splitting the declaration would hoist 'ok' but leave 'value' inside the block.
            await VerifyCodeFix(
                """
                public class C
                {
                    public static unsafe int Unsafe() => 0;

                    public static bool TryGet(int input, out int result)
                    {
                        result = input;
                        return true;
                    }

                    public int M()
                    {
                        bool ok = TryGet({|CS9362:Unsafe()|}, out int value);
                        return ok ? value : 0;
                    }
                }
                """,
                """
                public class C
                {
                    public static unsafe int Unsafe() => 0;

                    public static bool TryGet(int input, out int result)
                    {
                        result = input;
                        return true;
                    }

                    public int M()
                    {
                        bool ok = TryGet(unsafe(Unsafe()), out int value);
                        return ok ? value : 0;
                    }
                }
                """);
        }

        [Fact]
        public async Task LocalMentionedOnlyByNameOfStaysVisible()
        {
            // 'nameof' produces a constant, so data flow analysis does not report the local as read.
            await VerifyCodeFix(
                """
                public class C
                {
                    public static unsafe int Unsafe() => 0;

                    public string M()
                    {
                        int value = {|CS9362:Unsafe()|};
                        return nameof(value);
                    }
                }
                """,
                """
                public class C
                {
                    public static unsafe int Unsafe() => 0;

                    public string M()
                    {
                        int value;
                        unsafe
                        {
                            value = Unsafe();
                        }
                        return nameof(value);
                    }
                }
                """);
        }

        [Fact]
        public async Task VoidExpressionBodyLeavesTheDiagnosticEvenWhenTheCallReturnsAValue()
        {
            // The arrow body of a void method is a statement-expression position regardless of what the call
            // itself returns.
            await VerifyNoCodeFix(
                """
                public class C
                {
                    public static unsafe int Unsafe() => 0;

                    public void M() => {|CS9362:Unsafe()|};
                }
                """);
        }

        [Fact]
        public async Task VoidLambdaBodyLeavesTheDiagnostic()
        {
            await VerifyNoCodeFix(
                """
                using System;

                public class C
                {
                    public static unsafe int Unsafe() => 0;

                    public Action M() => () => {|CS9362:Unsafe()|};
                }
                """);
        }

        [Fact]
        public async Task ParenthesizedPropertyAccessIsWrappedTogetherWithItsConsumer()
        {
            // Parentheses pass the place through, so they do not count as consuming the access.
            await VerifyCodeFix(
                """
                public class C
                {
                    public unsafe int UnsafeProperty => 0;

                    public int M(C other) => Add(({|CS9362:other.UnsafeProperty|}), 1);

                    private static int Add(int left, int right) => left + right;
                }
                """,
                """
                public class C
                {
                    public unsafe int UnsafeProperty => 0;

                    public int M(C other) => unsafe(Add((other.UnsafeProperty), 1));

                    private static int Add(int left, int right) => left + right;
                }
                """);
        }

        [Fact]
        public async Task LocalMentionedByNameOfInAnotherSwitchSectionStaysVisible()
        {
            await VerifyCodeFix(
                """
                public class C
                {
                    public static unsafe int Unsafe() => 0;

                    public string M(int key)
                    {
                        switch (key)
                        {
                            case 1:
                                int value = {|CS9362:Unsafe()|};
                                return value.ToString();
                            default:
                                return nameof(value);
                        }
                    }
                }
                """,
                """
                public class C
                {
                    public static unsafe int Unsafe() => 0;

                    public string M(int key)
                    {
                        switch (key)
                        {
                            case 1:
                                int value;
                                unsafe
                                {
                                    value = Unsafe();
                                }
                                return value.ToString();
                            default:
                                return nameof(value);
                        }
                    }
                }
                """);
        }

        [Fact]
        public async Task CommentsAroundTheInitializerSurviveTheSplit()
        {
            await VerifyCodeFix(
                """
                public class C
                {
                    public static unsafe int Unsafe() => 0;

                    public int M()
                    {
                        int value =
                            // Why this call is fine.
                            {|CS9362:Unsafe()|};
                        return value;
                    }
                }
                """,
                """
                public class C
                {
                    public static unsafe int Unsafe() => 0;

                    public int M()
                    {
                        int value;
                        unsafe
                        {
                            value =
                                    // Why this call is fine.
                                    Unsafe();
                        }
                        return value;
                    }
                }
                """);
        }

        [Fact]
        public async Task CollectionInitializerElementIsWrappedWithTheCreation()
        {
            await VerifyCodeFix(
                """
                using System.Collections.Generic;

                public class C
                {
                    public static unsafe int UnsafeProperty => 0;

                    private static readonly List<int> s_values = new List<int> { {|CS9362:UnsafeProperty|} };

                    public int M() => s_values.Count;
                }
                """,
                """
                using System.Collections.Generic;

                public class C
                {
                    public static unsafe int UnsafeProperty => 0;

                    private static readonly List<int> s_values = unsafe(new List<int> { UnsafeProperty });

                    public int M() => s_values.Count;
                }
                """);
        }

        [Fact]
        public async Task ObjectInitializerMemberIsWrappedWithTheCreation()
        {
            await VerifyCodeFix(
                """
                public class C
                {
                    public static unsafe int UnsafeProperty => 0;

                    public class Holder
                    {
                        public int Value { get; set; }
                    }

                    private static readonly Holder s_holder = new Holder { Value = {|CS9362:UnsafeProperty|} };

                    public int M() => s_holder.Value;
                }
                """,
                """
                public class C
                {
                    public static unsafe int UnsafeProperty => 0;

                    public class Holder
                    {
                        public int Value { get; set; }
                    }

                    private static readonly Holder s_holder = unsafe(new Holder { Value = UnsafeProperty });

                    public int M() => s_holder.Value;
                }
                """);
        }

        [Fact]
        public async Task IndexInitializerKeyIsWrappedWithTheCreation()
        {
            await VerifyCodeFix(
                """
                using System.Collections.Generic;

                public class C
                {
                    public static unsafe int UnsafeProperty => 0;

                    private static readonly Dictionary<int, int> s_map =
                        new Dictionary<int, int> { [{|CS9362:UnsafeProperty|}] = 1 };

                    public int M() => s_map.Count;
                }
                """,
                """
                using System.Collections.Generic;

                public class C
                {
                    public static unsafe int UnsafeProperty => 0;

                    private static readonly Dictionary<int, int> s_map =
                        unsafe(new Dictionary<int, int> { [UnsafeProperty] = 1 });

                    public int M() => s_map.Count;
                }
                """);
        }

        [Fact]
        public async Task ArrayInitializerShorthandLeavesTheDiagnostic()
        {
            // There is nothing to wrap: the braces are only meaningful in the slot they occupy.
            await VerifyNoCodeFix(
                """
                public class C
                {
                    public static unsafe int UnsafeProperty => 0;

                    private static readonly int[] s_values = { {|CS9362:UnsafeProperty|} };

                    public int M() => s_values.Length;
                }
                """);
        }

        [Fact]
        public async Task ConstructorInitializerIsNotConsolidatedIntoTheBody()
        {
            // The body does not contain the use site, so it must not be offered or chosen for it.
            await VerifyCodeFix(
                """
                public class B
                {
                    public B(int value) { }
                }

                public class C : B
                {
                    public static unsafe int Unsafe() => 0;

                    public C() : base({|CS9362:Unsafe()|})
                    {
                        {|CS9362:Unsafe()|};
                        {|CS9362:Unsafe()|};
                        {|CS9362:Unsafe()|};
                    }
                }
                """,
                """
                public class B
                {
                    public B(int value) { }
                }

                public class C : B
                {
                    public static unsafe int Unsafe() => 0;

                    public C() : base(unsafe(Unsafe()))
                    {
                        unsafe
                        {
                            Unsafe();
                        }
                        unsafe
                        {
                            Unsafe();
                        }
                        unsafe
                        {
                            Unsafe();
                        }
                    }
                }
                """,
                batchFixedSource:
                """
                public class B
                {
                    public B(int value) { }
                }

                public class C : B
                {
                    public static unsafe int Unsafe() => 0;

                    public C() : base(unsafe(Unsafe()))
                    {
                        unsafe
                        {
                            Unsafe();
                            Unsafe();
                            Unsafe();
                        }
                    }
                }
                """);
        }
    }
}
#endif

