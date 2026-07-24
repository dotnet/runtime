// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Threading.Tasks;
using ILLink.CodeFix;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests
{
    /// <summary>
    /// Verifies unsafe-context migration across compiler diagnostics and syntax positions.
    /// </summary>
    public class AddUnsafeContextCodeFixTests
    {
        [Fact]
        public async Task SplitsLocalDeclarationAndAddsSafetyComment()
        {
            var source = """
                class C
                {
                    static unsafe int Read() => 0;

                    static int M()
                    {
                        int value = {|CS9362:Read()|};
                        return value;
                    }
                }
                """;
            var fixedSource = """
                class C
                {
                    static unsafe int Read() => 0;

                    static int M()
                    {
                        int value;
                        unsafe
                        {
                            // SAFETY: Audit
                            value = Read();
                        }
                        return value;
                    }
                }
                """;

            await CreateTest(source, fixedSource).RunAsync();
        }

        [Fact]
        public async Task InitializerOutVariableKeepsItsScope()
        {
            var source = """
                class C
                {
                    static unsafe int Get(out int other)
                    {
                        other = 0;
                        return 0;
                    }

                    static void M()
                    {
                        int value = {|CS9362:Get(out int other)|};
                        System.Console.WriteLine(value + other);
                    }
                }
                """;
            var fixedSource = """
                class C
                {
                    static unsafe int Get(out int other)
                    {
                        other = 0;
                        return 0;
                    }

                    static void M()
                    {
                        int value = unsafe(/* SAFETY: Audit */ Get(out int other));
                        System.Console.WriteLine(value + other);
                    }
                }
                """;

            await CreateTest(source, fixedSource).RunAsync();
        }

        [Fact]
        public async Task UnsafeImplicitConversionPreservesSelectedOperator()
        {
            var source = """
                struct Convertible
                {
                    public static unsafe implicit operator short(Convertible value) => 1;
                    public static explicit operator int(Convertible value) => 2;
                }

                class C
                {
                    static int Value = {|CS9362:new Convertible()|};
                }
                """;
            var fixedSource = """
                struct Convertible
                {
                    public static unsafe implicit operator short(Convertible value) => 1;
                    public static explicit operator int(Convertible value) => 2;
                }

                class C
                {
                    static int Value = unsafe(/* SAFETY: Audit */ (short)new Convertible());
                }
                """;

            await CreateTest(source, fixedSource).RunAsync();
        }

        [Fact]
        public async Task StackAllocForwardDeclarationIsScoped()
        {
            var source = """
                [module: System.Runtime.CompilerServices.SkipLocalsInit]

                class C
                {
                    static void M()
                    {
                        System.Span<byte> bytes = {|CS9361:stackalloc byte[10]|};
                        bytes.Clear();
                    }
                }
                """;
            var fixedSource = """
                [module: System.Runtime.CompilerServices.SkipLocalsInit]

                class C
                {
                    static void M()
                    {
                        scoped System.Span<byte> bytes;
                        unsafe
                        {
                            // SAFETY: Audit
                            bytes = stackalloc byte[10];
                        }
                        bytes.Clear();
                    }
                }
                """;

            await CreateTest(source, fixedSource).RunAsync();
        }

        [Fact]
        public async Task ExtendsUnsafeStatementAroundLabelForOutVariable()
        {
            var source = """
                class C
                {
                    static unsafe void Get(out int value) => value = 0;
                    static void Use(int value) { }

                    static void M()
                    {
                    Label:
                        {|CS9362:Get(out int value)|};
                        Use(value);
                    }
                }
                """;
            var fixedSource = """
                class C
                {
                    static unsafe void Get(out int value) => value = 0;
                    static void Use(int value) { }

                    static void M()
                    {
                        unsafe
                        {
                            // SAFETY: Audit
                        Label:
                            Get(out int value);
                            Use(value);
                        }
                    }
                }
                """;

            await CreateTest(source, fixedSource).RunAsync();
        }

        [Fact]
        public async Task NoFixWhenGotoWouldEnterUnsafeStatement()
        {
            var source = """
                class C
                {
                    static unsafe void Work() { }

                    static void M()
                    {
                        goto Label;
                    Label:
                        {|CS9362:Work()|};
                    }
                }
                """;

            var test = UnsafeMigrationTestHelpers
                .CreateCodeFixTest<DynamicallyAccessedMembersAnalyzer, AddUnsafeContextCodeFixProvider>(
                    source);
            await test.RunAsync();
        }

        [Fact]
        public async Task WrapsPointerDereferenceStatement()
        {
            var source = """
                class C
                {
                    static void M(int* pointer)
                    {
                        System.Console.WriteLine({|CS9360:*|}pointer);
                    }
                }
                """;
            var fixedSource = """
                class C
                {
                    static void M(int* pointer)
                    {
                        unsafe
                        {
                            // SAFETY: Audit
                            System.Console.WriteLine(*pointer);
                        }
                    }
                }
                """;

            await CreateTest(source, fixedSource).RunAsync();
        }

        [Fact]
        public async Task UsesUnsafeExpressionForCatchFilter()
        {
            var source = """
                class C
                {
                    static unsafe bool Filter(System.Exception exception) => true;

                    static void M()
                    {
                        try
                        {
                        }
                        catch (System.Exception exception) when ({|CS9362:Filter(exception)|})
                        {
                        }
                    }
                }
                """;
            var fixedSource = """
                class C
                {
                    static unsafe bool Filter(System.Exception exception) => true;

                    static void M()
                    {
                        try
                        {
                        }
                        catch (System.Exception exception) when (unsafe(/* SAFETY: Audit */ Filter(exception)))
                        {
                        }
                    }
                }
                """;

            await CreateTest(source, fixedSource).RunAsync();
        }

        [Fact]
        public async Task UsesUnsafeExpressionForFieldInitializer()
        {
            var source = """
                class C
                {
                    static unsafe int Read() => 0;
                    static int Value = {|CS9362:Read()|};
                }
                """;
            var fixedSource = """
                class C
                {
                    static unsafe int Read() => 0;
                    static int Value = unsafe(/* SAFETY: Audit */ Read());
                }
                """;

            await CreateTest(source, fixedSource).RunAsync();
        }

        [Fact]
        public async Task UnsafeImplicitConversionStaysInsideExpression()
        {
            var source = """
                class Convertible
                {
                    public static unsafe implicit operator int(Convertible value) => 0;
                }

                class C
                {
                    static int Value = {|CS9362:new Convertible()|};
                }
                """;
            var fixedSource = """
                class Convertible
                {
                    public static unsafe implicit operator int(Convertible value) => 0;
                }

                class C
                {
                    static int Value = unsafe(/* SAFETY: Audit */ (int)new Convertible());
                }
                """;

            await CreateTest(source, fixedSource).RunAsync();
        }

        [Fact]
        public async Task ConditionalAccessWrapsTheCompleteExpression()
        {
            var source = """
                class Data
                {
                    public unsafe int Value { get; }
                }

                class C
                {
                    static Data s_data = new();
                    static int? Value = s_data?{|CS9362:.Value|};
                }
                """;
            var fixedSource = """
                class Data
                {
                    public unsafe int Value { get; }
                }

                class C
                {
                    static Data s_data = new();
                    static int? Value = unsafe(/* SAFETY: Audit */ s_data?.Value);
                }
                """;

            await CreateTest(source, fixedSource).RunAsync();
        }

        [Fact]
        public async Task ObjectInitializerWrapsTheCompleteCreation()
        {
            var source = """
                class Data
                {
                    public unsafe int Value { get; set; }
                }

                class C
                {
                    static Data Value = new() { {|CS9362:Value|} = 1 };
                }
                """;
            var fixedSource = """
                class Data
                {
                    public unsafe int Value { get; set; }
                }

                class C
                {
                    static Data Value = unsafe(/* SAFETY: Audit */ new() { Value = 1 });
                }
                """;

            await CreateTest(source, fixedSource).RunAsync();
        }

        [Fact]
        public async Task CollectionInitializerWrapsTheCompleteCreation()
        {
            var source = """
                class Collection : System.Collections.Generic.IEnumerable<int>
                {
                    public unsafe void Add(int value) { }
                    public System.Collections.Generic.IEnumerator<int> GetEnumerator() => null;
                    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
                }

                class C
                {
                    static Collection Value = new() { {|CS9362:1|} };
                }
                """;
            var fixedSource = """
                class Collection : System.Collections.Generic.IEnumerable<int>
                {
                    public unsafe void Add(int value) { }
                    public System.Collections.Generic.IEnumerator<int> GetEnumerator() => null;
                    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
                }

                class C
                {
                    static Collection Value = unsafe(/* SAFETY: Audit */ new() { 1 });
                }
                """;

            await CreateTest(source, fixedSource).RunAsync();
        }

        [Fact]
        public async Task UsesUnsafeExpressionForUsingDeclarationInitializer()
        {
            var source = """
                class Resource : System.IDisposable
                {
                    public void Dispose() { }
                }

                class C
                {
                    static unsafe Resource Create() => new();

                    static void M()
                    {
                        using Resource resource = {|CS9362:Create()|};
                        System.Console.WriteLine(resource);
                    }
                }
                """;
            var fixedSource = """
                class Resource : System.IDisposable
                {
                    public void Dispose() { }
                }

                class C
                {
                    static unsafe Resource Create() => new();

                    static void M()
                    {
                        using Resource resource = unsafe(/* SAFETY: Audit */ Create());
                        System.Console.WriteLine(resource);
                    }
                }
                """;

            await CreateTest(source, fixedSource).RunAsync();
        }

        [Fact]
        public async Task UsesUnsafeExpressionForScopedRefInitializer()
        {
            var source = """
                class C
                {
                    static int s_value;
                    static unsafe ref int GetReference() => ref s_value;

                    static void M()
                    {
                        scoped ref int value = ref {|CS9362:GetReference()|};
                        value++;
                    }
                }
                """;
            var fixedSource = """
                class C
                {
                    static int s_value;
                    static unsafe ref int GetReference() => ref s_value;

                    static void M()
                    {
                        scoped ref int value = ref unsafe(/* SAFETY: Audit */ GetReference());
                        value++;
                    }
                }
                """;

            await CreateTest(source, fixedSource).RunAsync();
        }

        [Fact]
        public async Task AwaitUsesUnsafeExpression()
        {
            var source = """
                class C
                {
                    static unsafe System.Threading.Tasks.Task WorkAsync() => System.Threading.Tasks.Task.CompletedTask;

                    static async System.Threading.Tasks.Task M()
                    {
                        await {|CS9362:WorkAsync()|};
                    }
                }
                """;
            var fixedSource = """
                class C
                {
                    static unsafe System.Threading.Tasks.Task WorkAsync() => System.Threading.Tasks.Task.CompletedTask;

                    static async System.Threading.Tasks.Task M()
                    {
                        await unsafe(/* SAFETY: Audit */ WorkAsync());
                    }
                }
                """;

            await CreateTest(source, fixedSource).RunAsync();
        }

        [Fact]
        public async Task ExpressionBodiedAwaitUsesUnsafeExpression()
        {
            var source = """
                class C
                {
                    static unsafe System.Threading.Tasks.Task WorkAsync() => System.Threading.Tasks.Task.CompletedTask;
                    static async System.Threading.Tasks.Task M() => await {|CS9362:WorkAsync()|};
                }
                """;
            var fixedSource = """
                class C
                {
                    static unsafe System.Threading.Tasks.Task WorkAsync() => System.Threading.Tasks.Task.CompletedTask;
                    static async System.Threading.Tasks.Task M() => await unsafe(/* SAFETY: Audit */ WorkAsync());
                }
                """;

            await CreateTest(source, fixedSource).RunAsync();
        }

        [Fact]
        public async Task AwaitUsingInitializerUsesUnsafeExpression()
        {
            var source = """
                class Resource : System.IAsyncDisposable
                {
                    public System.Threading.Tasks.ValueTask DisposeAsync() => default;
                }

                class C
                {
                    static unsafe Resource Create() => new();

                    static async System.Threading.Tasks.Task M()
                    {
                        await using Resource resource = {|CS9362:Create()|};
                        await System.Threading.Tasks.Task.Yield();
                    }
                }
                """;
            var fixedSource = """
                class Resource : System.IAsyncDisposable
                {
                    public System.Threading.Tasks.ValueTask DisposeAsync() => default;
                }

                class C
                {
                    static unsafe Resource Create() => new();

                    static async System.Threading.Tasks.Task M()
                    {
                        await using Resource resource = unsafe(/* SAFETY: Audit */ Create());
                        await System.Threading.Tasks.Task.Yield();
                    }
                }
                """;

            await CreateTest(source, fixedSource).RunAsync();
        }

        [Fact]
        public async Task AwaitedLocalInitializerUsesUnsafeExpression()
        {
            var source = """
                class C
                {
                    static unsafe System.Threading.Tasks.Task<int> GetAsync() => System.Threading.Tasks.Task.FromResult(0);

                    static async System.Threading.Tasks.Task<int> M()
                    {
                        int value = await {|CS9362:GetAsync()|};
                        return value;
                    }
                }
                """;
            var fixedSource = """
                class C
                {
                    static unsafe System.Threading.Tasks.Task<int> GetAsync() => System.Threading.Tasks.Task.FromResult(0);

                    static async System.Threading.Tasks.Task<int> M()
                    {
                        int value = await unsafe(/* SAFETY: Audit */ GetAsync());
                        return value;
                    }
                }
                """;

            await CreateTest(source, fixedSource).RunAsync();
        }

        [Fact]
        public async Task NoFixWhenUnsafeAwaitPatternSpansAwaitExpression()
        {
            var source = """
                class Awaitable
                {
                    public unsafe Awaiter GetAwaiter() => default;
                }

                struct Awaiter : System.Runtime.CompilerServices.INotifyCompletion
                {
                    public bool IsCompleted => true;
                    public void OnCompleted(System.Action continuation) { }
                    public void GetResult() { }
                }

                class C
                {
                    static async System.Threading.Tasks.Task M()
                    {
                        {|CS9362:await new Awaitable()|};
                    }
                }
                """;

            var test = UnsafeMigrationTestHelpers
                .CreateCodeFixTest<DynamicallyAccessedMembersAnalyzer, AddUnsafeContextCodeFixProvider>(
                    source);
            await test.RunAsync();
        }

        [Fact]
        public async Task NoFixForExpressionBodiedUnsafeAwaitPattern()
        {
            var source = """
                class Awaitable
                {
                    public unsafe Awaiter GetAwaiter() => default;
                }

                struct Awaiter : System.Runtime.CompilerServices.INotifyCompletion
                {
                    public bool IsCompleted => true;
                    public void OnCompleted(System.Action continuation) { }
                    public void GetResult() { }
                }

                class C
                {
                    static async System.Threading.Tasks.Task M() =>
                        {|CS9362:await new Awaitable()|};
                }
                """;

            var test = UnsafeMigrationTestHelpers
                .CreateCodeFixTest<DynamicallyAccessedMembersAnalyzer, AddUnsafeContextCodeFixProvider>(
                    source);
            await test.RunAsync();
        }

        [Fact]
        public async Task NoFixForAwaitUsingWithUnsafeDispose()
        {
            var source = """
                class Resource
                {
                    public unsafe System.Threading.Tasks.ValueTask DisposeAsync() => default;
                }

                class C
                {
                    static async System.Threading.Tasks.Task M()
                    {
                        {|CS9362:await using Resource resource = new();|}
                        await System.Threading.Tasks.Task.Yield();
                    }
                }
                """;

            var test = UnsafeMigrationTestHelpers
                .CreateCodeFixTest<DynamicallyAccessedMembersAnalyzer, AddUnsafeContextCodeFixProvider>(
                    source);
            await test.RunAsync();
        }

        [Fact]
        public async Task NoFixWhenUsingRangeWouldHideGenericLocalFunction()
        {
            var source = """
                ref struct Resource
                {
                    public unsafe void Dispose() { }
                }

                class C
                {
                    static void M()
                    {
                        Local<int>();
                        {|CS9362:using Resource resource = new();|}
                        void Local<T>() { }
                    }
                }
                """;

            var test = UnsafeMigrationTestHelpers
                .CreateCodeFixTest<DynamicallyAccessedMembersAnalyzer, AddUnsafeContextCodeFixProvider>(
                    source);
            await test.RunAsync();
        }

        [Fact]
        public async Task NoFixWhenRangeCrossesConditionalDirective()
        {
            var source = """
                #define A

                class C
                {
                    static unsafe void Get(out int value) => value = 0;

                    static void M()
                    {
                #if A
                        {|CS9362:Get(out int value)|};
                #endif
                        System.Console.WriteLine(value);
                    }
                }
                """;

            var test = UnsafeMigrationTestHelpers
                .CreateCodeFixTest<DynamicallyAccessedMembersAnalyzer, AddUnsafeContextCodeFixProvider>(
                    source);
            await test.RunAsync();
        }

        [Fact]
        public async Task NoFixWhenUsingRangeWouldHideLocalFunction()
        {
            var source = """
                ref struct Resource
                {
                    public unsafe void Dispose() { }
                }

                class C
                {
                    static void M()
                    {
                        Local();
                        {|CS9362:using Resource resource = new();|}
                        void Local() { }
                    }
                }
                """;

            var test = UnsafeMigrationTestHelpers
                .CreateCodeFixTest<DynamicallyAccessedMembersAnalyzer, AddUnsafeContextCodeFixProvider>(
                    source);
            await test.RunAsync();
        }

        [Fact]
        public async Task NoFixForUnsafeForeachContainingYield()
        {
            var source = """
                class Enumerable
                {
                    public unsafe Enumerator GetEnumerator() => default;

                    public struct Enumerator
                    {
                        public int Current => 0;
                        public bool MoveNext() => false;
                    }
                }

                class C
                {
                    static System.Collections.Generic.IEnumerable<int> M()
                    {
                        {|CS9362:foreach|} (int value in new Enumerable())
                        {
                            yield return value;
                        }
                    }
                }
                """;

            var test = UnsafeMigrationTestHelpers
                .CreateCodeFixTest<DynamicallyAccessedMembersAnalyzer, AddUnsafeContextCodeFixProvider>(
                    source);
            await test.RunAsync();
        }

        [Fact]
        public async Task YieldUsesUnsafeExpression()
        {
            var source = """
                class C
                {
                    static unsafe int Read() => 0;

                    static System.Collections.Generic.IEnumerable<int> M()
                    {
                        yield return {|CS9362:Read()|};
                    }
                }
                """;
            var fixedSource = """
                class C
                {
                    static unsafe int Read() => 0;

                    static System.Collections.Generic.IEnumerable<int> M()
                    {
                        yield return unsafe(/* SAFETY: Audit */ Read());
                    }
                }
                """;

            await CreateTest(source, fixedSource).RunAsync();
        }

        [Fact]
        public async Task ConvertsExpressionBodiedMemberToUnsafeStatement()
        {
            var source = """
                class C
                {
                    static unsafe int Read() => 0;
                    static int M() => {|CS9362:Read()|};
                }
                """;
            var fixedSource = """
                class C
                {
                    static unsafe int Read() => 0;
                    static int M()
                    {
                        unsafe
                        {
                            // SAFETY: Audit
                            return Read();
                        }
                    }
                }
                """;

            await CreateTest(source, fixedSource).RunAsync();
        }

        [Fact]
        public async Task ConvertsAsyncTaskExpressionBodyToExpressionStatement()
        {
            var source = """
                class C
                {
                    static unsafe System.Threading.Tasks.Task WorkAsync() =>
                        System.Threading.Tasks.Task.CompletedTask;

                    static async System.Threading.Tasks.Task M() => {|CS9362:WorkAsync()|};
                }
                """;
            var fixedSource = """
                class C
                {
                    static unsafe System.Threading.Tasks.Task WorkAsync() =>
                        System.Threading.Tasks.Task.CompletedTask;

                    static async System.Threading.Tasks.Task M()
                    {
                        unsafe
                        {
                            // SAFETY: Audit
                            WorkAsync();
                        }
                    }
                }
                """;

            await CreateTest(source, fixedSource).RunAsync();
        }

        [Fact]
        public async Task ConvertsAsyncTaskValueExpressionBodyToExpressionStatement()
        {
            var source = """
                class C
                {
                    static unsafe int Work() => 0;
                    static async System.Threading.Tasks.Task M() => {|CS9362:Work()|};
                }
                """;
            var fixedSource = """
                class C
                {
                    static unsafe int Work() => 0;
                    static async System.Threading.Tasks.Task M()
                    {
                        unsafe
                        {
                            // SAFETY: Audit
                            Work();
                        }
                    }
                }
                """;

            await CreateTest(source, fixedSource).RunAsync();
        }

        [Fact]
        public async Task PreservesOutVariableScope()
        {
            var source = """
                class C
                {
                    static unsafe bool TryGet(out int value)
                    {
                        value = 0;
                        return true;
                    }

                    static void M()
                    {
                        if ({|CS9362:TryGet(out int value)|})
                        {
                        }

                        System.Console.WriteLine(value);
                    }
                }
                """;
            var fixedSource = """
                class C
                {
                    static unsafe bool TryGet(out int value)
                    {
                        value = 0;
                        return true;
                    }

                    static void M()
                    {
                        if (unsafe(/* SAFETY: Audit */ TryGet(out int value)))
                        {
                        }

                        System.Console.WriteLine(value);
                    }
                }
                """;

            await CreateTest(source, fixedSource).RunAsync();
        }

        [Fact]
        public async Task ExtendsUnsafeStatementForOutVariableInExpressionStatement()
        {
            var source = """
                class C
                {
                    static unsafe void Get(out int value) => value = 0;

                    static void M()
                    {
                        {|CS9362:Get(out int value)|};
                        System.Console.WriteLine(value);
                    }
                }
                """;
            var fixedSource = """
                class C
                {
                    static unsafe void Get(out int value) => value = 0;

                    static void M()
                    {
                        unsafe
                        {
                            // SAFETY: Audit
                            Get(out int value);
                            System.Console.WriteLine(value);
                        }
                    }
                }
                """;

            await CreateTest(source, fixedSource).RunAsync();
        }

        [Fact]
        public async Task ExtendsUnsafeStatementForUsingDeclarationDispose()
        {
            var source = """
                ref struct Resource
                {
                    public unsafe void Dispose() { }
                    public void Use() { }
                }

                class C
                {
                    static void M()
                    {
                        {|CS9362:using Resource resource = new();|}
                        resource.Use();
                    }
                }
                """;
            var fixedSource = """
                ref struct Resource
                {
                    public unsafe void Dispose() { }
                    public void Use() { }
                }

                class C
                {
                    static void M()
                    {
                        unsafe
                        {
                            // SAFETY: Audit
                            using Resource resource = new();
                            resource.Use();
                        }
                    }
                }
                """;

            await CreateTest(source, fixedSource).RunAsync();
        }

        [Fact]
        public async Task UsesUnsafeExpressionInsideDirective()
        {
            var source = """
                class C
                {
                    static unsafe int Read() => 0;

                    static int M()
                    {
                        int value =
                #if true
                            {|CS9362:Read()|}
                #else
                            0
                #endif
                            ;
                        return value;
                    }
                }
                """;
            var fixedSource = """
                class C
                {
                    static unsafe int Read() => 0;

                    static int M()
                    {
                        int value =
                #if true
                            unsafe(/* SAFETY: Audit */ Read())
                #else
                            0
                #endif
                            ;
                        return value;
                    }
                }
                """;

            await CreateTest(source, fixedSource).RunAsync();
        }

        [Fact]
        public async Task UnsafeStatementStaysInsideConditionalDirective()
        {
            var source = """
                class C
                {
                    static unsafe void Work() { }

                    static void M()
                    {
                #if true
                        {|CS9362:Work()|};
                #endif
                    }
                }
                """;
            var fixedSource = """
                class C
                {
                    static unsafe void Work() { }

                    static void M()
                    {
                #if true
                        unsafe
                        {
                            // SAFETY: Audit
                            Work();
                        }
                #endif
                    }
                }
                """;

            await CreateTest(source, fixedSource).RunAsync();
        }

        [Fact]
        public async Task TopLevelLocalKeepsItsScope()
        {
            var source = """
                int value = {|CS9362:Read()|};
                System.Console.WriteLine(value);

                static unsafe int Read() => 0;
                """;
            var fixedSource = """
                int value = unsafe(/* SAFETY: Audit */ Read());
                System.Console.WriteLine(value);

                static unsafe int Read() => 0;
                """;

            var test = CreateTest(source, fixedSource);
            test.TestState.OutputKind = OutputKind.ConsoleApplication;
            await test.RunAsync();
        }

        [Fact]
        public async Task TopLevelOutVariableKeepsItsScope()
        {
            var source = """
                {|CS9362:Get(out int value)|};
                System.Console.WriteLine(value);

                static unsafe void Get(out int value) => value = 0;
                """;
            var fixedSource = """
                unsafe
                {
                    // SAFETY: Audit
                    Get(out int value);
                    System.Console.WriteLine(value);

                    static unsafe void Get(out int value) => value = 0;
                }
                """;

            var test = CreateTest(source, fixedSource);
            test.TestState.OutputKind = OutputKind.ConsoleApplication;
            await test.RunAsync();
        }

        [Fact]
        public async Task NoFixWhenTopLevelGotoWouldEnterUnsafeStatement()
        {
            var source = """
                goto Label;
                Label:
                {|CS9362:Work()|};

                static unsafe void Work() { }
                """;

            var test = UnsafeMigrationTestHelpers
                .CreateCodeFixTest<DynamicallyAccessedMembersAnalyzer, AddUnsafeContextCodeFixProvider>(
                    source);
            test.TestState.OutputKind = OutputKind.ConsoleApplication;
            await test.RunAsync();
        }

        [Fact]
        public async Task UsesUnsafeExpressionInConstructorArgument()
        {
            var source = """
                class B
                {
                    protected B(int value) { }
                }

                class C : B
                {
                    static unsafe int Read() => 0;
                    C() : base({|CS9362:Read()|}) { }
                }
                """;
            var fixedSource = """
                class B
                {
                    protected B(int value) { }
                }

                class C : B
                {
                    static unsafe int Read() => 0;
                    C() : base(unsafe(/* SAFETY: Audit */ Read())) { }
                }
                """;

            await CreateTest(source, fixedSource).RunAsync();
        }

        [Fact]
        public async Task MarksConstructorUnsafeForUnsafeBaseCall()
        {
            var source = """
                class B
                {
                    protected unsafe B() { }
                }

                class C : B
                {
                    {|CS9362:public C() { }|}
                }
                """;
            var fixedSource = """
                class B
                {
                    protected unsafe B() { }
                }

                class C : B
                {
                    /// <safety>TODO: Audit</safety>
                    public unsafe C() { }
                }
                """;

            await CreateTest(source, fixedSource).RunAsync();
        }

        [Fact]
        public async Task MarksConstructorUnsafeForLegacyPointerBaseCall()
        {
            var source = """
                class C : LegacyBase
                {
                    public C(int* pointer) {|CS9363:: base(pointer)|} { }
                }
                """;
            var fixedSource = """
                class C : LegacyBase
                {
                    /// <safety>TODO: Audit</safety>
                    public unsafe C(int* pointer) : base(pointer) { }
                }
                """;

            var test = CreateTest(source, fixedSource);
            test.TestState.AdditionalReferences.Add(CreateLegacyReference());
            await test.RunAsync();
        }

        [Fact]
        public async Task AddsUnsafeToUsingAliasForConstructorConstraint()
        {
            var source = """
                using {|CS9376:Alias|} = Generic<UnsafeConstructor>;

                class UnsafeConstructor
                {
                    public unsafe UnsafeConstructor() { }
                }

                class Generic<T> where T : new() { }
                """;
            var fixedSource = """
                using unsafe Alias = Generic<UnsafeConstructor>;

                class UnsafeConstructor
                {
                    public unsafe UnsafeConstructor() { }
                }

                class Generic<T> where T : new() { }
                """;

            await CreateTest(source, fixedSource).RunAsync();
        }

        [Fact]
        public async Task WrapsConstructorConstraintInvocation()
        {
            var source = """
                class UnsafeConstructor
                {
                    public unsafe UnsafeConstructor() { }
                }

                class C
                {
                    static void Create<T>() where T : new() { }

                    static void M()
                    {
                        {|CS9376:Create<UnsafeConstructor>()|};
                    }
                }
                """;
            var fixedSource = """
                class UnsafeConstructor
                {
                    public unsafe UnsafeConstructor() { }
                }

                class C
                {
                    static void Create<T>() where T : new() { }

                    static void M()
                    {
                        unsafe
                        {
                            // SAFETY: Audit
                            Create<UnsafeConstructor>();
                        }
                    }
                }
                """;

            await CreateTest(source, fixedSource).RunAsync();
        }

        [Fact]
        public async Task FixesLegacyPointerSignatureDiagnostic()
        {
            var source = """
                class C
                {
                    static void M()
                    {
                        {|CS9363:Legacy.GetPointer()|};
                    }
                }
                """;
            var fixedSource = """
                class C
                {
                    static void M()
                    {
                        unsafe
                        {
                            // SAFETY: Audit
                            Legacy.GetPointer();
                        }
                    }
                }
                """;

            var test = CreateTest(source, fixedSource);
            test.TestState.AdditionalReferences.Add(CreateLegacyReference());
            await test.RunAsync();
        }

        [Fact]
        public async Task OneUnsafeStatementFixesOverlappingDiagnostics()
        {
            var source = """
                class C
                {
                    static void M()
                    {
                        Legacy.Callback();
                    }
                }
                """;
            var fixedSource = """
                class C
                {
                    static void M()
                    {
                        unsafe
                        {
                            // SAFETY: Audit
                            Legacy.Callback();
                        }
                    }
                }
                """;

            var test = CreateTest(source, fixedSource);
            test.TestState.AdditionalReferences.Add(CreateLegacyReference());
            test.TestState.ExpectedDiagnostics.Add(
                Microsoft.CodeAnalysis.Testing.DiagnosticResult
                    .CompilerError(AddUnsafeContextCodeFixProvider.UnsafeMemberOperationCompatDiagnosticId)
                    .WithSpan(5, 9, 5, 24)
                    .WithArguments("Legacy.Callback"));
            test.TestState.ExpectedDiagnostics.Add(
                Microsoft.CodeAnalysis.Testing.DiagnosticResult
                    .CompilerError(AddUnsafeContextCodeFixProvider.UnsafeOperationDiagnosticId)
                    .WithSpan(5, 9, 5, 26));
            await test.RunAsync();
        }

        [Fact]
        public async Task UnsafePropertyReadUsesCastInsideUnsafeExpression()
        {
            var source = """
                class Data
                {
                    public unsafe int Value { get; }
                }

                class C
                {
                    static Data s_data = new();
                    static int Value = {|CS9362:s_data.Value|};
                }
                """;
            var fixedSource = """
                class Data
                {
                    public unsafe int Value { get; }
                }

                class C
                {
                    static Data s_data = new();
                    static int Value = unsafe(/* SAFETY: Audit */ (int)s_data.Value);
                }
                """;

            await CreateTest(source, fixedSource).RunAsync();
        }

        [Fact]
        public async Task UnsafeIndexerReadUsesCastInsideUnsafeExpression()
        {
            var source = """
                class Data
                {
                    public unsafe bool this[int index] => true;
                }

                class C
                {
                    static Data s_data = new();

                    static void M()
                    {
                        try
                        {
                        }
                        catch (System.Exception) when ({|CS9362:s_data[0]|})
                        {
                        }
                    }
                }
                """;
            var fixedSource = """
                class Data
                {
                    public unsafe bool this[int index] => true;
                }

                class C
                {
                    static Data s_data = new();

                    static void M()
                    {
                        try
                        {
                        }
                        catch (System.Exception) when (unsafe(/* SAFETY: Audit */ (bool)s_data[0]))
                        {
                        }
                    }
                }
                """;

            await CreateTest(source, fixedSource).RunAsync();
        }

        [Fact]
        public async Task MethodGroupConversionUsesCastInsideUnsafeExpression()
        {
            var source = """
                class C
                {
                    static unsafe void Work() { }
                    static System.Action s_action = {|CS9362:Work|};
                }
                """;
            var fixedSource = """
                class C
                {
                    static unsafe void Work() { }
                    static System.Action s_action = unsafe(/* SAFETY: Audit */ (System.Action)Work);
                }
                """;

            await CreateTest(source, fixedSource).RunAsync();
        }

        [Fact]
        public async Task MethodGroupConversionInYieldReturnUsesCast()
        {
            var source = """
                class C
                {
                    static unsafe void Work() { }

                    static System.Collections.Generic.IEnumerable<System.Action> M()
                    {
                        yield return {|CS9362:Work|};
                    }
                }
                """;
            var fixedSource = """
                class C
                {
                    static unsafe void Work() { }

                    static System.Collections.Generic.IEnumerable<System.Action> M()
                    {
                        yield return unsafe(/* SAFETY: Audit */ (System.Action)Work);
                    }
                }
                """;

            await CreateTest(source, fixedSource).RunAsync();
        }

        [Fact]
        public async Task DeconstructionUsesUnsafeStatement()
        {
            var source = """
                class D
                {
                    public unsafe void Deconstruct(out int first, out int second)
                    {
                        first = 0;
                        second = 0;
                    }
                }

                class C
                {
                    static void M()
                    {
                        var (first, second) = {|CS9362:new D()|};
                        System.Console.WriteLine(first + second);
                    }
                }
                """;
            var fixedSource = """
                class D
                {
                    public unsafe void Deconstruct(out int first, out int second)
                    {
                        first = 0;
                        second = 0;
                    }
                }

                class C
                {
                    static void M()
                    {
                        unsafe
                        {
                            // SAFETY: Audit
                            var (first, second) = new D();
                            System.Console.WriteLine(first + second);
                        }
                    }
                }
                """;

            await CreateTest(source, fixedSource).RunAsync();
        }

        [Fact]
        public async Task NoFixWhenUnsafeExpressionIsAlreadyPresent()
        {
            var source = """
                class Data
                {
                    public unsafe int Value { get; }
                }

                class C
                {
                    static Data s_data = new();
                    static int Value = unsafe(/* SAFETY: Audit */ {|CS9362:s_data.Value|});
                }
                """;

            var test = UnsafeMigrationTestHelpers
                .CreateCodeFixTest<DynamicallyAccessedMembersAnalyzer, AddUnsafeContextCodeFixProvider>(
                    source);
            await test.RunAsync();
        }

        [Fact]
        public async Task NoFixForUnsafePropertyAssignmentInAwaitStatement()
        {
            var source = """
                class Data
                {
                    public unsafe int Value { get; set; }
                }

                class C
                {
                    static Data s_data = new();
                    static async System.Threading.Tasks.Task<int> GetAsync() => 1;

                    static async System.Threading.Tasks.Task M()
                    {
                        {|CS9362:{|CS9362:s_data.Value|}|} += await GetAsync();
                    }
                }
                """;

            var test = UnsafeMigrationTestHelpers
                .CreateCodeFixTest<DynamicallyAccessedMembersAnalyzer, AddUnsafeContextCodeFixProvider>(
                    source);
            await test.RunAsync();
        }

        [Fact]
        public async Task NoFixForRefReturningUnsafeProperty()
        {
            var source = """
                class Data
                {
                    static int s_value;
                    public unsafe ref int Value => ref s_value;
                }

                class C
                {
                    static Data s_data = new();
                    static int Value = {|CS9362:s_data.Value|};
                }
                """;

            var test = UnsafeMigrationTestHelpers
                .CreateCodeFixTest<DynamicallyAccessedMembersAnalyzer, AddUnsafeContextCodeFixProvider>(
                    source);
            await test.RunAsync();
        }

        [Fact]
        public async Task MergesAdjacentGeneratedUnsafeStatements()
        {
            var source = """
                class C
                {
                    static unsafe void Work() { }

                    static void M()
                    {
                        {|CS9362:Work()|};
                        {|CS9362:Work()|};
                    }
                }
                """;
            var fixedSource = """
                class C
                {
                    static unsafe void Work() { }

                    static void M()
                    {
                        unsafe
                        {
                            // SAFETY: Audit
                            Work();
                            Work();
                        }
                    }
                }
                """;
            // Fix all evaluates every diagnostic against the original document, so it cannot merge the regions that
            // the individual fixes produce.
            var batchFixedSource = """
                class C
                {
                    static unsafe void Work() { }

                    static void M()
                    {
                        unsafe
                        {
                            // SAFETY: Audit
                            Work();
                        }

                        unsafe
                        {
                            // SAFETY: Audit
                            Work();
                        }
                    }
                }
                """;

            var test = CreateTest(source, fixedSource);
            test.BatchFixedCode = batchFixedSource;
            await test.RunAsync();
        }

        [Fact]
        public async Task MergesGeneratedUnsafeStatementsSeparatedBySafeStatement()
        {
            var source = """
                class C
                {
                    static unsafe void Work() { }

                    static void M()
                    {
                        {|CS9362:Work()|};
                        System.Console.WriteLine();
                        {|CS9362:Work()|};
                    }
                }
                """;
            var fixedSource = """
                class C
                {
                    static unsafe void Work() { }

                    static void M()
                    {
                        unsafe
                        {
                            // SAFETY: Audit
                            Work();
                            System.Console.WriteLine();
                            Work();
                        }
                    }
                }
                """;
            var batchFixedSource = """
                class C
                {
                    static unsafe void Work() { }

                    static void M()
                    {
                        unsafe
                        {
                            // SAFETY: Audit
                            Work();
                        }

                        System.Console.WriteLine();
                        unsafe
                        {
                            // SAFETY: Audit
                            Work();
                        }
                    }
                }
                """;

            var test = CreateTest(source, fixedSource);
            test.BatchFixedCode = batchFixedSource;
            await test.RunAsync();
        }

        [Fact]
        public async Task DoesNotMergeIntoAuditedUnsafeStatement()
        {
            var source = """
                class C
                {
                    static unsafe void Work() { }

                    static void M()
                    {
                        unsafe
                        {
                            // SAFETY: Reviewed by the runtime team.
                            Work();
                        }
                        {|CS9362:Work()|};
                    }
                }
                """;
            var fixedSource = """
                class C
                {
                    static unsafe void Work() { }

                    static void M()
                    {
                        unsafe
                        {
                            // SAFETY: Reviewed by the runtime team.
                            Work();
                        }
                        unsafe
                        {
                            // SAFETY: Audit
                            Work();
                        }
                    }
                }
                """;

            await CreateTest(source, fixedSource).RunAsync();
        }

        [Fact]
        public async Task DoesNotMergeAcrossADeclaringStatement()
        {
            var source = """
                class C
                {
                    static unsafe void Work() { }

                    static void M()
                    {
                        {|CS9362:Work()|};
                        int value = 5;
                        {|CS9362:Work()|};
                        System.Console.WriteLine(value);
                    }
                }
                """;
            var fixedSource = """
                class C
                {
                    static unsafe void Work() { }

                    static void M()
                    {
                        unsafe
                        {
                            // SAFETY: Audit
                            Work();
                        }

                        int value = 5;
                        unsafe
                        {
                            // SAFETY: Audit
                            Work();
                        }

                        System.Console.WriteLine(value);
                    }
                }
                """;

            await CreateTest(source, fixedSource).RunAsync();
        }

        [Fact]
        public async Task MergingKeepsCommentsFromTheAbsorbedRegion()
        {
            var source = """
                class C
                {
                    static unsafe void Work() { }

                    static void M()
                    {
                        unsafe
                        {
                            // SAFETY: Audit
                            // Only the first call needs the pointer.
                            Work();
                        }
                        {|CS9362:Work()|};
                    }
                }
                """;
            var fixedSource = """
                class C
                {
                    static unsafe void Work() { }

                    static void M()
                    {
                        // Only the first call needs the pointer.
                        unsafe
                        {
                            // SAFETY: Audit
                            Work();
                            Work();
                        }
                    }
                }
                """;

            await CreateTest(source, fixedSource).RunAsync();
        }

        [Fact]
        public async Task ParenthesizesUnaryOperandOfInsertedCast()
        {
            var source = """
                struct Ptr
                {
                    public static implicit operator Ptr(int value) => default;
                }

                class C
                {
                    static int* GetPointer() => null;
                    static Ptr s_ptr = {|CS9360:*|}GetPointer();
                }
                """;
            var fixedSource = """
                struct Ptr
                {
                    public static implicit operator Ptr(int value) => default;
                }

                class C
                {
                    static int* GetPointer() => null;
                    static Ptr s_ptr = unsafe(/* SAFETY: Audit */ (Ptr)(*GetPointer()));
                }
                """;

            await CreateTest(source, fixedSource).RunAsync();
        }

        private static CSharpCodeFixVerifier<DynamicallyAccessedMembersAnalyzer, AddUnsafeContextCodeFixProvider>.Test CreateTest(
            string source,
            string fixedSource) =>
            UnsafeMigrationTestHelpers
                .CreateCodeFixTest<DynamicallyAccessedMembersAnalyzer, AddUnsafeContextCodeFixProvider>(
                    source,
                    fixedSource);

        private static MetadataReference CreateLegacyReference()
        {
            var syntaxTree = SyntaxFactory.ParseSyntaxTree(
                """
                public static class Legacy
                {
                    public static unsafe int* GetPointer() => null;
                    public static unsafe delegate*<void> Callback;
                }

                public class LegacyBase
                {
                    public unsafe LegacyBase(int* pointer) { }
                }
                """,
                new CSharpParseOptions(LanguageVersion.Preview));
            CSharpCompilation compilation = CSharpCompilation.Create(
                "Legacy",
                [syntaxTree],
                SourceGenerators.Tests.LiveReferencePack.GetMetadataReferences(),
                new CSharpCompilationOptions(
                    OutputKind.DynamicallyLinkedLibrary,
                    allowUnsafe: true));
            return compilation.EmitToImageReference();
        }
    }
}
#endif
