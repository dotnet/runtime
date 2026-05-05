// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

using VerifyCS = PreferAsyncAwait.Tests.CSharpCodeFixVerifier<
    PreferAsyncAwait.PreferAsyncAwaitAnalyzer,
    PreferAsyncAwait.PreferAsyncAwaitCodeFixProvider>;

namespace PreferAsyncAwait.Tests;

public class PreferAsyncAwaitAnalyzerTests
{
    [Fact]
    public async Task NoDiagnosticForEmptySource()
        => await VerifyCS.VerifyAnalyzerAsync(string.Empty);

    [Fact]
    public async Task NoDiagnosticForAlreadyAsyncMethod()
    {
        const string source = """
            using System.Threading.Tasks;

            class C
            {
                async Task<int> M() { return await M2(); }
                async Task<int> M2() { return 42; }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task NoDiagnosticForNonTaskReturningMethod()
    {
        const string source = """
            class C
            {
                int M() { return M2(); }
                int M2() => 42;
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task NoDiagnosticForVoidMethod()
    {
        const string source = """
            class C
            {
                void M() { M2(); }
                void M2() { }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task NoDiagnosticForMethodReturningNonInvocation()
    {
        const string source = """
            using System.Threading.Tasks;

            class C
            {
                Task<int> M()
                {
                    var t = Task.FromResult(42);
                    return t;
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task NoDiagnosticForAbstractMethod()
    {
        const string source = """
            using System.Threading.Tasks;

            abstract class C
            {
                public abstract Task<int> M();
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task DiagnosticForGenericTaskBlockBody()
    {
        const string source = """
            using System.Threading.Tasks;

            class C
            {
                Task<int> {|#0:M|}() { return M2(); }
                async Task<int> M2() { return 42; }
            }
            """;

        const string fixedSource = """
            using System.Threading.Tasks;

            class C
            {
                async Task<int> M() { return await M2().ConfigureAwait(false); }
                async Task<int> M2() { return 42; }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(
            source,
            VerifyCS.Diagnostic(PreferAsyncAwaitAnalyzer.DiagnosticId).WithLocation(0).WithArguments("M"),
            fixedSource);
    }

    [Fact]
    public async Task DiagnosticForGenericTaskExpressionBody()
    {
        const string source = """
            using System.Threading.Tasks;

            class C
            {
                Task<int> {|#0:M|}() => M2();
                async Task<int> M2() { return 42; }
            }
            """;

        const string fixedSource = """
            using System.Threading.Tasks;

            class C
            {
                async Task<int> M() => await M2().ConfigureAwait(false);
                async Task<int> M2() { return 42; }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(
            source,
            VerifyCS.Diagnostic(PreferAsyncAwaitAnalyzer.DiagnosticId).WithLocation(0).WithArguments("M"),
            fixedSource);
    }

    [Fact]
    public async Task DiagnosticForNonGenericTaskBlockBody()
    {
        const string source = """
            using System.Threading.Tasks;

            class C
            {
                Task {|#0:M|}() { return M2(); }
                async Task M2() { }
            }
            """;

        const string fixedSource = """
            using System.Threading.Tasks;

            class C
            {
                async Task M() { await M2().ConfigureAwait(false); }
                async Task M2() { }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(
            source,
            VerifyCS.Diagnostic(PreferAsyncAwaitAnalyzer.DiagnosticId).WithLocation(0).WithArguments("M"),
            fixedSource);
    }

    [Fact]
    public async Task DiagnosticForNonGenericTaskExpressionBody()
    {
        const string source = """
            using System.Threading.Tasks;

            class C
            {
                Task {|#0:M|}() => M2();
                async Task M2() { }
            }
            """;

        const string fixedSource = """
            using System.Threading.Tasks;

            class C
            {
                async Task M() => await M2().ConfigureAwait(false);
                async Task M2() { }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(
            source,
            VerifyCS.Diagnostic(PreferAsyncAwaitAnalyzer.DiagnosticId).WithLocation(0).WithArguments("M"),
            fixedSource);
    }

    [Fact]
    public async Task DiagnosticForGenericValueTaskBlockBody()
    {
        const string source = """
            using System.Threading.Tasks;

            class C
            {
                ValueTask<int> {|#0:M|}() { return M2(); }
                async ValueTask<int> M2() { return 42; }
            }
            """;

        const string fixedSource = """
            using System.Threading.Tasks;

            class C
            {
                async ValueTask<int> M() { return await M2().ConfigureAwait(false); }
                async ValueTask<int> M2() { return 42; }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(
            source,
            VerifyCS.Diagnostic(PreferAsyncAwaitAnalyzer.DiagnosticId).WithLocation(0).WithArguments("M"),
            fixedSource);
    }

    [Fact]
    public async Task DiagnosticForNonGenericValueTaskBlockBody()
    {
        const string source = """
            using System.Threading.Tasks;

            class C
            {
                ValueTask {|#0:M|}() { return M2(); }
                async ValueTask M2() { }
            }
            """;

        const string fixedSource = """
            using System.Threading.Tasks;

            class C
            {
                async ValueTask M() { await M2().ConfigureAwait(false); }
                async ValueTask M2() { }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(
            source,
            VerifyCS.Diagnostic(PreferAsyncAwaitAnalyzer.DiagnosticId).WithLocation(0).WithArguments("M"),
            fixedSource);
    }

    [Fact]
    public async Task DiagnosticForMethodWithStatementsBeforeReturn()
    {
        const string source = """
            using System;
            using System.Threading.Tasks;

            class C
            {
                Task<int> {|#0:M|}()
                {
                    Console.WriteLine("hello");
                    return M2();
                }
                async Task<int> M2() { return 42; }
            }
            """;

        const string fixedSource = """
            using System;
            using System.Threading.Tasks;

            class C
            {
                async Task<int> M()
                {
                    Console.WriteLine("hello");
                    return await M2().ConfigureAwait(false);
                }
                async Task<int> M2() { return 42; }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(
            source,
            VerifyCS.Diagnostic(PreferAsyncAwaitAnalyzer.DiagnosticId).WithLocation(0).WithArguments("M"),
            fixedSource);
    }

    [Fact]
    public async Task DiagnosticForMultipleReturnStatements()
    {
        const string source = """
            using System.Threading.Tasks;

            class C
            {
                Task<int> {|#0:M|}(bool b)
                {
                    if (b)
                        return M2();
                    return M3();
                }
                async Task<int> M2() { return 1; }
                async Task<int> M3() { return 2; }
            }
            """;

        const string fixedSource = """
            using System.Threading.Tasks;

            class C
            {
                async Task<int> M(bool b)
                {
                    if (b)
                        return await M2().ConfigureAwait(false);
                    return await M3().ConfigureAwait(false);
                }
                async Task<int> M2() { return 1; }
                async Task<int> M3() { return 2; }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(
            source,
            VerifyCS.Diagnostic(PreferAsyncAwaitAnalyzer.DiagnosticId).WithLocation(0).WithArguments("M"),
            fixedSource);
    }

    [Fact]
    public async Task DiagnosticIgnoresReturnInsideLambda()
    {
        const string source = """
            using System;
            using System.Threading.Tasks;

            class C
            {
                Task<int> {|#0:M|}()
                {
                    Func<Task<int>> f = () {|#1:=>|} { return Task.FromResult(42); };
                    return f();
                }
            }
            """;

        const string fixedSource = """
            using System;
            using System.Threading.Tasks;

            class C
            {
                async Task<int> M()
                {
                    Func<Task<int>> f = async () => { return 42; };
                    return await f().ConfigureAwait(false);
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(
            source,
            new[]
            {
                VerifyCS.Diagnostic(PreferAsyncAwaitAnalyzer.DiagnosticId).WithLocation(0).WithArguments("M"),
                VerifyCS.Diagnostic(PreferAsyncAwaitAnalyzer.DiagnosticId).WithLocation(1).WithArguments("lambda"),
            },
            fixedSource);
    }

    [Fact]
    public async Task FixUnwrapsTaskFromResult()
    {
        const string source = """
            using System.Threading.Tasks;

            class C
            {
                Task<int> {|#0:M|}() => Task.FromResult(42);

                Task<string> {|#1:M2|}()
                {
                    return Task.FromResult("hello");
                }
            }
            """;

        const string fixedSource = """
            using System.Threading.Tasks;

            class C
            {
                async Task<int> M() => 42;

                async Task<string> M2()
                {
                    return "hello";
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(
            source,
            new[]
            {
                VerifyCS.Diagnostic(PreferAsyncAwaitAnalyzer.DiagnosticId).WithLocation(0).WithArguments("M"),
                VerifyCS.Diagnostic(PreferAsyncAwaitAnalyzer.DiagnosticId).WithLocation(1).WithArguments("M2"),
            },
            fixedSource);
    }

    [Fact]
    public async Task FixStripsAsTask()
    {
        const string source = """
            using System.Threading.Tasks;

            class C
            {
                Task {|#0:M|}() => GetValueTaskAsync().AsTask();

                Task<int> {|#1:M2|}()
                {
                    return GetValueTaskOfIntAsync().AsTask();
                }

                ValueTask GetValueTaskAsync() => default;
                ValueTask<int> GetValueTaskOfIntAsync() => default;
            }
            """;

        const string fixedSource = """
            using System.Threading.Tasks;

            class C
            {
                async Task M() => await GetValueTaskAsync().ConfigureAwait(false);

                async Task<int> M2()
                {
                    return await GetValueTaskOfIntAsync().ConfigureAwait(false);
                }

                ValueTask GetValueTaskAsync() => default;
                ValueTask<int> GetValueTaskOfIntAsync() => default;
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(
            source,
            new[]
            {
                VerifyCS.Diagnostic(PreferAsyncAwaitAnalyzer.DiagnosticId).WithLocation(0).WithArguments("M"),
                VerifyCS.Diagnostic(PreferAsyncAwaitAnalyzer.DiagnosticId).WithLocation(1).WithArguments("M2"),
            },
            fixedSource);
    }

    [Fact]
    public async Task NoDiagnosticWhenMixedReturnTypes()
    {
        const string source = """
            using System.Threading.Tasks;

            class C
            {
                Task<int> M(bool b)
                {
                    if (b)
                        return M2();
                    var t = Task.FromResult(42);
                    return t;
                }
                async Task<int> M2() { return 1; }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task NoDiagnosticForBareFireAndForgetCall()
    {
        const string source = """
            using System.Threading.Tasks;

            class C
            {
                Task<int> M()
                {
                    FireAndForget();
                    return M2();
                }
                Task FireAndForget() => Task.CompletedTask;
                async Task<int> M2() { return 42; }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task NoDiagnosticForAssignedTaskCall()
    {
        const string source = """
            using System.Threading.Tasks;

            class C
            {
                Task<int> M()
                {
                    Task t = FireAndForget();
                    return M2();
                }
                Task FireAndForget() => Task.CompletedTask;
                async Task<int> M2() { return 42; }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task DiagnosticAllowedWithExplicitDiscard()
    {
        const string source = """
            using System.Threading.Tasks;

            class C
            {
                Task<int> {|#0:M|}()
                {
                    _ = FireAndForget();
                    return M2();
                }
                Task FireAndForget() => Task.CompletedTask;
                async Task<int> M2() { return 42; }
            }
            """;

        const string fixedSource = """
            using System.Threading.Tasks;

            class C
            {
                async Task<int> M()
                {
                    _ = FireAndForget();
                    return await M2().ConfigureAwait(false);
                }
                Task FireAndForget() => Task.CompletedTask;
                async Task<int> M2() { return 42; }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(
            source,
            VerifyCS.Diagnostic(PreferAsyncAwaitAnalyzer.DiagnosticId).WithLocation(0).WithArguments("M"),
            fixedSource);
    }

    [Fact]
    public async Task NoDiagnosticForReassignedTaskVariable()
    {
        const string source = """
            using System.Threading.Tasks;

            class C
            {
                Task<int> M()
                {
                    Task t;
                    t = FireAndForget();
                    return M2();
                }
                Task FireAndForget() => Task.CompletedTask;
                async Task<int> M2() { return 42; }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task FireAndForgetInsideLambdaDoesNotBlockDiagnostic()
    {
        const string source = """
            using System;
            using System.Threading.Tasks;

            class C
            {
                Task<int> {|#0:M|}()
                {
                    Action a = () => { FireAndForget(); };
                    return M2();
                }
                Task FireAndForget() => Task.CompletedTask;
                async Task<int> M2() { return 42; }
            }
            """;

        const string fixedSource = """
            using System;
            using System.Threading.Tasks;

            class C
            {
                async Task<int> M()
                {
                    Action a = () => { FireAndForget(); };
                    return await M2().ConfigureAwait(false);
                }
                Task FireAndForget() => Task.CompletedTask;
                async Task<int> M2() { return 42; }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(
            source,
            VerifyCS.Diagnostic(PreferAsyncAwaitAnalyzer.DiagnosticId).WithLocation(0).WithArguments("M"),
            fixedSource);
    }

    [Fact]
    public async Task DiagnosticForLocalFunctionBlockBody()
    {
        const string source = """
            using System.Threading.Tasks;

            class C
            {
                void M()
                {
                    Task<int> {|#0:Local|}() { return M2(); }
                }
                async Task<int> M2() { return 42; }
            }
            """;

        const string fixedSource = """
            using System.Threading.Tasks;

            class C
            {
                void M()
                {
                    async Task<int> Local() { return await M2().ConfigureAwait(false); }
                }
                async Task<int> M2() { return 42; }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(
            source,
            VerifyCS.Diagnostic(PreferAsyncAwaitAnalyzer.DiagnosticId).WithLocation(0).WithArguments("Local"),
            fixedSource);
    }

    [Fact]
    public async Task DiagnosticForLocalFunctionExpressionBody()
    {
        const string source = """
            using System.Threading.Tasks;

            class C
            {
                void M()
                {
                    Task<int> {|#0:Local|}() => M2();
                }
                async Task<int> M2() { return 42; }
            }
            """;

        const string fixedSource = """
            using System.Threading.Tasks;

            class C
            {
                void M()
                {
                    async Task<int> Local() => await M2().ConfigureAwait(false);
                }
                async Task<int> M2() { return 42; }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(
            source,
            VerifyCS.Diagnostic(PreferAsyncAwaitAnalyzer.DiagnosticId).WithLocation(0).WithArguments("Local"),
            fixedSource);
    }

    [Fact]
    public async Task DiagnosticForParenthesizedLambdaExpressionBody()
    {
        const string source = """
            using System;
            using System.Threading.Tasks;

            class C
            {
                void M()
                {
                    Func<Task<int>> f = () {|#0:=>|} M2();
                }
                async Task<int> M2() { return 42; }
            }
            """;

        const string fixedSource = """
            using System;
            using System.Threading.Tasks;

            class C
            {
                void M()
                {
                    Func<Task<int>> f = async () => await M2().ConfigureAwait(false);
                }
                async Task<int> M2() { return 42; }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(
            source,
            VerifyCS.Diagnostic(PreferAsyncAwaitAnalyzer.DiagnosticId).WithLocation(0).WithArguments("lambda"),
            fixedSource);
    }

    [Fact]
    public async Task DiagnosticForParenthesizedLambdaBlockBody()
    {
        const string source = """
            using System;
            using System.Threading.Tasks;

            class C
            {
                void M()
                {
                    Func<Task<int>> f = () {|#0:=>|}
                    {
                        return M2();
                    };
                }
                async Task<int> M2() { return 42; }
            }
            """;

        const string fixedSource = """
            using System;
            using System.Threading.Tasks;

            class C
            {
                void M()
                {
                    Func<Task<int>> f = async () =>
                    {
                        return await M2().ConfigureAwait(false);
                    };
                }
                async Task<int> M2() { return 42; }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(
            source,
            VerifyCS.Diagnostic(PreferAsyncAwaitAnalyzer.DiagnosticId).WithLocation(0).WithArguments("lambda"),
            fixedSource);
    }

    [Fact]
    public async Task DiagnosticForSimpleLambdaExpressionBody()
    {
        const string source = """
            using System;
            using System.Threading.Tasks;

            class C
            {
                void M()
                {
                    Func<int, Task<int>> f = x {|#0:=>|} M2(x);
                }
                async Task<int> M2(int x) { return x; }
            }
            """;

        const string fixedSource = """
            using System;
            using System.Threading.Tasks;

            class C
            {
                void M()
                {
                    Func<int, Task<int>> f = async x => await M2(x).ConfigureAwait(false);
                }
                async Task<int> M2(int x) { return x; }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(
            source,
            VerifyCS.Diagnostic(PreferAsyncAwaitAnalyzer.DiagnosticId).WithLocation(0).WithArguments("lambda"),
            fixedSource);
    }

    [Fact]
    public async Task NoDiagnosticForAlreadyAsyncLocalFunction()
    {
        const string source = """
            using System.Threading.Tasks;

            class C
            {
                void M()
                {
                    async Task<int> Local() { return await M2(); }
                }
                async Task<int> M2() { return 42; }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task NoDiagnosticForAlreadyAsyncLambda()
    {
        const string source = """
            using System;
            using System.Threading.Tasks;

            class C
            {
                void M()
                {
                    Func<Task<int>> f = async () => await M2();
                }
                async Task<int> M2() { return 42; }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task DiagnosticForNonGenericTaskLambda()
    {
        const string source = """
            using System;
            using System.Threading.Tasks;

            class C
            {
                void M()
                {
                    Func<Task> f = () {|#0:=>|} M2();
                }
                async Task M2() { }
            }
            """;

        const string fixedSource = """
            using System;
            using System.Threading.Tasks;

            class C
            {
                void M()
                {
                    Func<Task> f = async () => await M2().ConfigureAwait(false);
                }
                async Task M2() { }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(
            source,
            VerifyCS.Diagnostic(PreferAsyncAwaitAnalyzer.DiagnosticId).WithLocation(0).WithArguments("lambda"),
            fixedSource);
    }

    [Fact]
    public async Task NoDiagnosticForLambdaWithFireAndForget()
    {
        const string source = """
            using System;
            using System.Threading.Tasks;

            class C
            {
                void M()
                {
                    Func<Task<int>> f = () =>
                    {
                        FireAndForget();
                        return M2();
                    };
                }
                Task FireAndForget() => Task.CompletedTask;
                async Task<int> M2() { return 42; }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task NoDiagnosticForConditionalTaskReturn()
    {
        const string source = """
            using System;
            using System.Threading;
            using System.Threading.Tasks;

            class C
            {
                Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
                {
                    return cancellationToken.IsCancellationRequested ?
                        Task.FromCanceled<int>(cancellationToken) :
                        Task.FromResult(Read(buffer, offset, count));
                }
                int Read(byte[] buffer, int offset, int count) => count;
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }
}
