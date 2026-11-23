// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace System.Private.DataContractSerialization.Generators.Tests;

public class SyncReaderGeneratorTests
{
    private const string Foundation = """
        using System;
        using System.Threading;
        using System.Threading.Tasks;
        using System.Xml;

        namespace System.Xml
        {
            internal interface IXmlStreamIO { }
            internal readonly struct SyncXmlStreamIO : IXmlStreamIO { }
            internal readonly struct AsyncXmlStreamIO : IXmlStreamIO { }
        }

        namespace System.Runtime.Serialization
        {
            internal interface IReflectionSerialization
            {
                static abstract ValueTask<int> ReadValue();
            }
            internal readonly struct SyncReflectionSerialization : IReflectionSerialization
            {
                public static ValueTask<int> ReadValue() => new ValueTask<int>(7);
            }
            internal readonly struct AsyncReflectionSerialization : IReflectionSerialization
            {
                public static ValueTask<int> ReadValue() => new ValueTask<int>(9);
            }
            internal static class ReflectionSerialization
            {
                internal static T Complete<T>(ValueTask<T> operation)
                {
                    if (!operation.IsCompleted)
                        throw new InvalidOperationException("incomplete leaf");
                    return operation.GetAwaiter().GetResult();
                }

                internal static void Complete(ValueTask operation)
                {
                    if (!operation.IsCompleted)
                        throw new InvalidOperationException("incomplete leaf");
                    operation.GetAwaiter().GetResult();
                }
            }
        }
        """;

    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported))]
    public async Task LoweringPreservesCompletedValueTaskBranches()
    {
        const string Source = """
            namespace System.Runtime.Serialization
            {
                internal sealed partial class ReflectionXmlReader
                {
                    public int Read() => ReadMembersSync();
                    public ValueTask<int> ReadAsync() => ReadMembers<AsyncReflectionSerialization>();

                    private static async ValueTask<int> ReadMembers<TMode>()
                        where TMode : struct, IReflectionSerialization
                    {
                        ValueTask<int> value = TMode.ReadValue();
                        if (value.IsCompletedSuccessfully)
                        {
                            return value.Result + 1;
                        }

                        return await value.ConfigureAwait(false) + 1;
                    }
                }
            }
            """;

        Assembly assembly = await CompileAsync(Source);
        Type type = assembly.GetType("System.Runtime.Serialization.ReflectionXmlReader", throwOnError: true)!;
        object reader = Activator.CreateInstance(type)!;
        Assert.Equal(8, type.GetMethod("Read")!.Invoke(reader, null));
        ValueTask<int> operation = (ValueTask<int>)type.GetMethod("ReadAsync")!.Invoke(reader, null)!;
        Assert.Equal(10, await operation);
    }

    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported))]
    public async Task ReflectionMemberLoopUsesItsSerializationPolicy()
    {
        const string Source = """
            namespace System.Runtime.Serialization
            {
                internal sealed partial class ReflectionXmlReader
                {
                    public int Read() => ReadMembersSync(3);
                    public ValueTask<int> ReadAsync() => ReadMembers<AsyncReflectionSerialization>(3);

                    private static async ValueTask<int> ReadMembers<TMode>(int count)
                        where TMode : struct, IReflectionSerialization
                    {
                        int result = 0;
                        for (int index = 0; index < count; index++)
                        {
                            result += await TMode.ReadValue().ConfigureAwait(false);
                        }
                        return result;
                    }
                }
            }
            """;
        Assembly assembly = await CompileAsync(Source);
        Type type = assembly.GetType("System.Runtime.Serialization.ReflectionXmlReader", throwOnError: true)!;
        object reader = Activator.CreateInstance(type)!;
        Assert.Equal(21, type.GetMethod("Read")!.Invoke(reader, null));
        ValueTask<int> operation = (ValueTask<int>)type.GetMethod("ReadAsync")!.Invoke(reader, null)!;
        Assert.Equal(27, await operation);
    }

    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported))]
    public async Task LoweringPreservesLoopsOverloadsEvaluationOrderAndExceptionHandling()
    {
        const string Source = """
            namespace System.Xml
            {
                internal sealed partial class XmlBufferReader
                {
                    public int Trace { get; private set; }
                    public int Read() => ParseCoreSync(4);
                    public int ReadSource() => System.Runtime.Serialization.ReflectionSerialization.Complete(ParseCore<SyncXmlStreamIO>(4, CancellationToken.None));

                    private async ValueTask<int> ParseCore<TMode>(int count, CancellationToken cancellationToken)
                        where TMode : struct, IXmlStreamIO
                    {
                        int result = 0;
                        try
                        {
                            for (int index = 0; index < count; index++)
                            {
                                switch (index % 3)
                                {
                                    case 0:
                                        result += await Leaf<TMode>(1, cancellationToken).ConfigureAwait(false);
                                        break;
                                    case 1:
                                        if (await Leaf<TMode>(2, cancellationToken).ConfigureAwait(false) == 2 &&
                                            await Positive<TMode>(3, cancellationToken).ConfigureAwait(false))
                                        {
                                            result += await ParseValue<TMode>(await Leaf<TMode>(4, cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
                                        }
                                        break;
                                    default:
                                        throw new FormatException("stop");
                                }
                            }
                        }
                        catch (FormatException exception) when (exception.Message == "stop")
                        {
                            result += 7;
                        }
                        finally
                        {
                            result += 10;
                        }
                        return result + 2 * await new ValueTask<int>(3 + 4) + await Helper.ParseCore<TMode>(cancellationToken) +
                            await DefaultValue<int, TMode>();
                    }

                    private ValueTask<int> Leaf<TMode>(int value, CancellationToken cancellationToken)
                        where TMode : struct, IXmlStreamIO
                    {
                        if (nameof(TMode) != "TMode")
                            throw new InvalidOperationException();
                        Trace = Trace * 10 + value;
                        return new ValueTask<int>(value);
                    }

                    private async ValueTask<bool> Positive<TMode>(int value, CancellationToken cancellationToken)
                        where TMode : struct, IXmlStreamIO =>
                        await Leaf<TMode>(value, cancellationToken).ConfigureAwait(false) > 0;

                    private async ValueTask<int> ParseValue<TMode>(int value, CancellationToken cancellationToken)
                        where TMode : struct, IXmlStreamIO
                    {
                        await Finish<TMode>(value, cancellationToken).ConfigureAwait(false);
                        return value;
                    }

                    private async ValueTask<string> ParseValue<TMode>(string value, CancellationToken cancellationToken)
                        where TMode : struct, IXmlStreamIO
                    {
                        await Finish<TMode>(value.Length, cancellationToken).ConfigureAwait(false);
                        return value;
                    }

                    private ValueTask Finish<TMode>(int value, CancellationToken cancellationToken)
                        where TMode : struct, IXmlStreamIO =>
                        value != 0 ? FinishLeaf(value) : ValueTask.CompletedTask;

                    private ValueTask FinishLeaf(int value)
                    {
                        Trace += value;
                        return ValueTask.CompletedTask;
                    }

                    private ValueTask<T> DefaultValue<T, TMode>() where T : struct where TMode : struct, IXmlStreamIO => default;
                }

                internal static class Helper
                {
                    internal static ValueTask<int> ParseCore<TMode>(CancellationToken cancellationToken)
                        where TMode : struct, IXmlStreamIO => new ValueTask<int>(5);
                }
            }
            """;

        Assembly assembly = await CompileAsync(Source);
        Type type = assembly.GetType("System.Xml.XmlBufferReader", throwOnError: true)!;
        object lowered = Activator.CreateInstance(type)!;
        object original = Activator.CreateInstance(type)!;
        Assert.Equal(41, type.GetMethod("Read")!.Invoke(lowered, null));
        Assert.Equal(41, type.GetMethod("ReadSource")!.Invoke(original, null));
        Assert.Equal(1238, type.GetProperty("Trace")!.GetValue(lowered));
        Assert.Equal(1238, type.GetProperty("Trace")!.GetValue(original));
    }

    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported))]
    public async Task LoweringPreservesOtherGenericConstraintsAndVirtualDispatch()
    {
        const string Source = """
            namespace System.Xml
            {
                internal abstract partial class XmlBaseReader
                {
                    public int Read() => ReadContentAsCoreSync(3);
                    public int SideEffects { get; private set; }
                    private void CheckReadMode<TMode>(CancellationToken cancellationToken) where TMode : struct, IXmlStreamIO
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        if (typeof(TMode) == typeof(AsyncXmlStreamIO) && ++SideEffects != 0)
                            throw new InvalidOperationException();
                    }
                    protected virtual async ValueTask<T> ReadContentAsCore<T, TMode>(T value, CancellationToken cancellationToken)
                        where T : struct
                        where TMode : struct, IXmlStreamIO
                    {
                        CheckReadMode<TMode>(cancellationToken);
                        if (typeof(TMode) == typeof(AsyncXmlStreamIO))
                        {
                            await Task.CompletedTask.ConfigureAwait(false);
                        }
                        await ValueTask.CompletedTask.ConfigureAwait(false);
                        return value;
                    }
                }
            }

            namespace System.Runtime.Serialization.Json
            {
                internal sealed partial class XmlJsonReader : XmlBaseReader
                {
                    protected override async ValueTask<T> ReadContentAsCore<T, TMode>(T value, CancellationToken cancellationToken)
                    {
                        value = await base.ReadContentAsCore<T, TMode>(value, cancellationToken).ConfigureAwait(false);
                        return typeof(T) == typeof(int) ? (T)(object)((int)(object)value + 1) : value;
                    }
                }
            }
            """;

        Assembly assembly = await CompileAsync(Source);
        Type type = assembly.GetType("System.Runtime.Serialization.Json.XmlJsonReader", throwOnError: true)!;
        object reader = Activator.CreateInstance(type)!;
        Assert.Equal(4, type.GetMethod("Read")!.Invoke(reader, null));
        Assert.Equal(0, type.GetProperty("SideEffects")!.GetValue(reader));
    }

    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported))]
    public async Task LoweringDoesNotChangeTrueAsyncSuspension()
    {
        const string Source = """
            namespace System.Xml
            {
                internal sealed partial class XmlUTF8TextReader
                {
                    private static readonly TaskCompletionSource<int> s_source = new(TaskCreationOptions.RunContinuationsAsynchronously);
                    public int Read() => ParseCoreSync();
                    public ValueTask<int> ReadAsync(CancellationToken cancellationToken) => ParseCore<AsyncXmlStreamIO>(cancellationToken);
                    public static void Release() => s_source.SetResult(7);
                    private async ValueTask<int> ParseCore<TMode>(CancellationToken cancellationToken)
                        where TMode : struct, IXmlStreamIO =>
                        1 + await Leaf<TMode>(cancellationToken).ConfigureAwait(false);
                    private ValueTask<int> Leaf<TMode>(CancellationToken cancellationToken)
                        where TMode : struct, IXmlStreamIO
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        return typeof(TMode) == typeof(SyncXmlStreamIO) ? new ValueTask<int>(7) : new ValueTask<int>(s_source.Task);
                    }
                }
            }
            """;

        Assembly assembly = await CompileAsync(Source);
        Type type = assembly.GetType("System.Xml.XmlUTF8TextReader", throwOnError: true)!;
        object reader = Activator.CreateInstance(type)!;
        Assert.Equal(8, type.GetMethod("Read")!.Invoke(reader, null));
        CancellationToken canceledToken = new CancellationToken(canceled: true);
        ValueTask<int> canceled = (ValueTask<int>)type.GetMethod("ReadAsync")!.Invoke(reader, [canceledToken])!;
        OperationCanceledException exception = await Assert.ThrowsAsync<OperationCanceledException>(async () => { await canceled; });
        Assert.Equal(canceledToken, exception.CancellationToken);
        ValueTask<int> operation = (ValueTask<int>)type.GetMethod("ReadAsync")!.Invoke(reader, [CancellationToken.None])!;
        Assert.False(operation.IsCompleted);
        type.GetMethod("Release")!.Invoke(null, null);
        Assert.Equal(8, await operation);
    }

    [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported))]
    [InlineData(false)]
    [InlineData(true)]
    public async Task NongeneratedLeavesPreserveFaultsAndRejectIncompleteOperationsWithoutBlocking(bool faulted)
    {
        string source = $$"""
            namespace System.Xml
            {
                internal sealed partial class XmlBinaryReader
                {
                    public int Read() => ParseCoreSync();
                    private async ValueTask<int> ParseCore<TMode>(CancellationToken cancellationToken)
                        where TMode : struct, IXmlStreamIO =>
                        await Leaf().ConfigureAwait(false);
                    private ValueTask<int> Leaf() => {{(faulted ? "ValueTask.FromException<int>(new FormatException(\"faulted leaf\"))" : "new ValueTask<int>(new TaskCompletionSource<int>().Task)")}};
                }
            }
            """;

        Assembly assembly = await CompileAsync(source);
        Type type = assembly.GetType("System.Xml.XmlBinaryReader", throwOnError: true)!;
        TargetInvocationException exception = Assert.Throws<TargetInvocationException>(() => type.GetMethod("Read")!.Invoke(Activator.CreateInstance(type), null));
        if (faulted)
        {
            Assert.Equal("faulted leaf", Assert.IsType<FormatException>(exception.InnerException).Message);
        }
        else
        {
            Assert.Equal("incomplete leaf", Assert.IsType<InvalidOperationException>(exception.InnerException).Message);
        }
    }

    [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported))]
    [InlineData("System.Xml", "XmlBinaryReader")]
    [InlineData("System.Xml", "XmlUTF8TextReader")]
    [InlineData("System.Runtime.Serialization.Json", "XmlJsonReader")]
    public async Task ReadEntryPointsAreLoweredDirectlyWithoutAWrapper(string namespaceName, string className)
    {
        string source = $$"""
            namespace System.Xml
            {
                internal abstract class Readable
                {
                    public abstract bool Read();
                }
            }
            namespace {{namespaceName}}
            {
                internal sealed partial class {{className}} : Readable
                {
                    private async ValueTask<bool> ReadCore<TMode>(CancellationToken cancellationToken)
                        where TMode : struct, IXmlStreamIO =>
                        await new ValueTask<bool>(true);
                }
            }
            """;
        Assembly assembly = await CompileAsync(source);
        Type type = assembly.GetType(namespaceName + "." + className, throwOnError: true)!;
        Assert.True((bool)type.GetMethod("Read")!.Invoke(Activator.CreateInstance(type), null)!);
        Assert.Null(type.GetMethod("ReadCoreSync", BindingFlags.Instance | BindingFlags.NonPublic));
    }

    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported))]
    public async Task ValueHandleSigningSharesOneSwitchAndPreservesAsyncSuspension()
    {
        const string Source = """
            namespace System.Xml
            {
                internal sealed partial class ValueHandle
                {
                    private static readonly TaskCompletionSource<bool> s_release = new(TaskCreationOptions.RunContinuationsAsynchronously);
                    public int Value { get; private set; }
                    public void Write() => SignSync(7);
                    public ValueTask WriteAsync() => Sign<AsyncXmlStreamIO>(9, CancellationToken.None);
                    public static void Release() => s_release.SetResult(true);
                    private async ValueTask Sign<TMode>(int value, CancellationToken cancellationToken)
                        where TMode : struct, IXmlStreamIO
                    {
                        switch (value)
                        {
                            case 7:
                            case 9:
                                if (typeof(TMode) == typeof(SyncXmlStreamIO))
                                    Value = value;
                                else
                                {
                                    await s_release.Task.ConfigureAwait(false);
                                    Value = value;
                                }
                                break;
                            default:
                                throw new InvalidOperationException();
                        }
                    }
                }
            }
            """;
        Assembly assembly = await CompileAsync(Source);
        Type type = assembly.GetType("System.Xml.ValueHandle", throwOnError: true)!;
        object value = Activator.CreateInstance(type)!;
        type.GetMethod("Write")!.Invoke(value, null);
        Assert.Equal(7, type.GetProperty("Value")!.GetValue(value));
        ValueTask operation = (ValueTask)type.GetMethod("WriteAsync")!.Invoke(value, null)!;
        Assert.False(operation.IsCompleted);
        type.GetMethod("Release")!.Invoke(null, null);
        await operation;
        Assert.Equal(9, type.GetProperty("Value")!.GetValue(value));
    }

    [Theory]
    [InlineData("await Task.CompletedTask.ConfigureAwait(false); return 1;", "await must consume a ValueTask")]
    [InlineData("await new ValueTask<int>(1).ConfigureAwait(true); return 1;", "only ConfigureAwait(false) is supported")]
    [InlineData("async ValueTask<int> Nested() { return await new ValueTask<int>(1); } return await Nested();", "nested functions, await using, and await foreach are not supported")]
    [InlineData("ValueTask<int> operation = Leaf<TMode>(); return await operation;", "ValueTask calls must be awaited, returned, or passed to the completed-only adapter")]
    [InlineData("Func<ValueTask<int>> operation = Leaf<TMode>; return await operation();", "method groups are not supported")]
    [InlineData("if (typeof(TMode) == typeof(AsyncXmlStreamIO) && int.TryParse(\"1\", out int value)) return value; value = 0; return value;", "declare out and pattern variables before a mode-only condition")]
    public async Task UnsupportedLoweringShapesHaveActionableDiagnostics(string body, string message)
    {
        string source = $$"""
            namespace System.Xml
            {
                internal sealed partial class XmlBufferReader
                {
                    private async ValueTask<int> ParseCore<TMode>() where TMode : struct, IXmlStreamIO
                    {
                        {{body}}
                    }
                    private ValueTask<int> Leaf<TMode>() where TMode : struct, IXmlStreamIO => new ValueTask<int>(1);
                }
            }
            """;
        CSharpCompilation compilation = CreateCompilation(source);
        ImmutableArray<Diagnostic> diagnostics = await compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new SyncReaderLoweringAnalyzer()))
            .GetAnalyzerDiagnosticsAsync();
        Assert.Contains(diagnostics, diagnostic => diagnostic.Id == "DCSG001" && diagnostic.Severity == DiagnosticSeverity.Error &&
            diagnostic.GetMessage().Contains(message, StringComparison.Ordinal));
        GeneratorDriverRunResult result = CreateDriver().RunGenerators(compilation).GetRunResult();
        Assert.Empty(result.GeneratedTrees);
        Assert.Empty(result.Diagnostics);
    }

    [Theory]
    [InlineData("System.Xml", "OtherReader", true, 0)]
    [InlineData("Other", "XmlBufferReader", true, 0)]
    [InlineData("System.Xml", "XmlBufferReader", false, 1)]
    public async Task LoweringScopeIsExactAndRequiresPartialClasses(string namespaceName, string className, bool partial, int diagnosticCount)
    {
        string source = $$"""
            namespace {{namespaceName}}
            {
                internal {{(partial ? "partial" : "")}} class {{className}}
                {
                    private async ValueTask<int> ParseCore<TMode>() where TMode : struct, IXmlStreamIO =>
                        await new ValueTask<int>(1);
                }
            }
            """;
        CSharpCompilation compilation = CreateCompilation(source);
        ImmutableArray<Diagnostic> diagnostics = await compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new SyncReaderLoweringAnalyzer()))
            .GetAnalyzerDiagnosticsAsync();
        Assert.Equal(diagnosticCount, diagnostics.Length);
        Assert.Empty(CreateDriver().RunGenerators(compilation).GetRunResult().GeneratedTrees);
    }

    [Fact]
    public void UnrelatedChangesDoNotInvalidateTheGeneratedSource()
    {
        const string Source = """
            namespace System.Xml
            {
                internal partial class XmlBufferReader
                {
                    private async ValueTask<int> ParseCore<TMode>() where TMode : struct, IXmlStreamIO =>
                        await new ValueTask<int>(1);
                }
            }
            """;
        CSharpCompilation compilation = CreateCompilation(Source);
        GeneratorDriver driver = CreateDriver().RunGenerators(compilation);
        string first = Assert.Single(driver.GetRunResult().Results).GeneratedSources.Single().SourceText.ToString();
        driver = driver.RunGenerators(compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText("internal class Unrelated { }")));
        GeneratorRunResult result = Assert.Single(driver.GetRunResult().Results);
        Assert.Equal(first, Assert.Single(result.GeneratedSources).SourceText.ToString());
        Assert.All(result.TrackedSteps["SyncReaderSources"].SelectMany(step => step.Outputs),
            output => Assert.Equal(IncrementalStepRunReason.Unchanged, output.Reason));
    }

    [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported))]
    [InlineData("ReflectionReader", 8, "ValueTask")]
    [InlineData("ReflectionClassWriter", 2, "ValueTask")]
    [InlineData("ReflectionClassWriter", 2, "Task")]
    public void ContractDelegatesCompleteInlineAndThrowDirectly(string className, int expected, string taskType)
    {
        const string Source = """
            namespace System.Runtime.Serialization
            {
                internal delegate ValueTask ReflectionWriter();
                internal sealed class ReflectionClassWriter
                {
                    private static ReflectionWriter Sequence(ReflectionWriter[] operations) =>
                        async () =>
                        {
                            foreach (ReflectionWriter operation in operations)
                                await operation().ConfigureAwait(false);
                        };

                    public static int Execute()
                    {
                        int count = 0;
                        ReflectionWriter sequence = Sequence(new ReflectionWriter[]
                        {
                            () => { count++; return ValueTask.CompletedTask; },
                            () => { count++; return ValueTask.CompletedTask; }
                        });
                        sequence().GetAwaiter().GetResult();
                        return Count();
                        int Count() => count;
                    }

                    public static bool ThrowsDirectly()
                    {
                        ReflectionWriter sequence = Sequence(new ReflectionWriter[]
                        {
                            () => throw new FormatException("direct")
                        });
                        try { sequence(); return false; }
                        catch (FormatException exception) { return exception.Message == "direct"; }
                    }
                }

                internal sealed class ReflectionReader
                {
                    private static Func<ValueTask<int>> Create(bool fail) => async () =>
                    {
                        int value = await new ValueTask<int>(7).ConfigureAwait(false);
                        if (fail)
                            throw new FormatException("direct");
                        return value + 1;
                    };

                    public static int Execute() => Create(false)().Result;
                    public static bool ThrowsDirectly()
                    {
                        Func<ValueTask<int>> read = Create(true);
                        try { read(); return false; }
                        catch (FormatException exception) { return exception.Message == "direct"; }
                    }
                }
            }
            """;
        string source = Source.Replace("ValueTask ReflectionWriter", taskType + " ReflectionWriter")
            .Replace("ValueTask.CompletedTask", taskType + ".CompletedTask");
        GeneratorDriver driver = CSharpGeneratorDriver.Create([new SyncContractGenerator().AsSourceGenerator()])
            .RunGeneratorsAndUpdateCompilation(CreateCompilation(source), out Compilation compilation, out ImmutableArray<Diagnostic> diagnostics);
        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.Equal(4, driver.GetRunResult().GeneratedTrees.Length);
        foreach (SyntaxTree tree in driver.GetRunResult().GeneratedTrees)
        {
            Assert.DoesNotContain(tree.GetRoot().DescendantNodes(), node => node is AwaitExpressionSyntax);
            Assert.DoesNotContain(tree.GetRoot().DescendantNodes().OfType<AnonymousFunctionExpressionSyntax>(),
                lambda => lambda.AsyncKeyword.IsKind(SyntaxKind.AsyncKeyword));
        }
        using MemoryStream output = new MemoryStream();
        Microsoft.CodeAnalysis.Emit.EmitResult emitted = compilation.Emit(output);
        Assert.True(emitted.Success, string.Join(Environment.NewLine, emitted.Diagnostics));
        Assembly assembly = Assembly.Load(output.ToArray());
        Type original = assembly.GetType("System.Runtime.Serialization." + className, throwOnError: true)!;
        Type synchronous = assembly.GetType("System.Runtime.Serialization.Sync" + className, throwOnError: true)!;
        Assert.Equal(expected, synchronous.GetMethod("Execute")!.Invoke(null, null));
        Assert.False((bool)original.GetMethod("ThrowsDirectly")!.Invoke(null, null)!);
        Assert.True((bool)synchronous.GetMethod("ThrowsDirectly")!.Invoke(null, null)!);
    }

    [Theory]
    [InlineData("await Task.Delay(1); return 1;")]
    [InlineData("return await new ValueTask<int>(1).ConfigureAwait(true);")]
    [InlineData("return await new ValueTask<int>(Task.FromResult(1));")]
    [InlineData("Func<int> nested = () => 1; return nested();")]
    public async Task ContractLoweringRejectsUnsupportedAwaitShapes(string body)
    {
        string source = $$"""
            namespace System.Runtime.Serialization
            {
                internal class ReflectionReader
                {
                    private Func<ValueTask<int>> Create() => async () => { {{body}} };
                }
            }
            """;
        CSharpCompilation compilation = CreateCompilation(source);
        ImmutableArray<Diagnostic> diagnostics = await compilation.WithAnalyzers(
            ImmutableArray.Create<DiagnosticAnalyzer>(new SyncContractLoweringAnalyzer())).GetAnalyzerDiagnosticsAsync();
        Assert.Contains(diagnostics, diagnostic => diagnostic.Id == "DCSG002");
        GeneratorDriver driver = CSharpGeneratorDriver.Create([new SyncContractGenerator().AsSourceGenerator()]).RunGenerators(compilation);
        SyntaxTree generatedPolicy = Assert.Single(driver.GetRunResult().GeneratedTrees);
        Assert.Equal("SyncReflectionPolicy", Assert.Single(generatedPolicy.GetRoot().DescendantNodes().OfType<StructDeclarationSyntax>()).Identifier.ValueText);
        Assert.Empty(generatedPolicy.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>());
        Assert.Empty(driver.GetRunResult().Diagnostics);
    }

    [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported))]
    [InlineData("ValueTask")]
    [InlineData("Task")]
    public void ContractLoweringUsesPlainDelegatesAndPreservesVoidSelection(string taskType)
    {
        const string Source = """
            namespace System.Runtime.Serialization
            {
                internal delegate VOID_TASK ReflectionWriter(int value);
                internal delegate System.Threading.Tasks.ValueTask<object> ReflectionValueReader(int value);
                internal class ReflectionClassWriter
                {
                    private static int s_count;
                    private delegate int CountReader();
                    private struct State
                    {
                        internal int Value;
                        internal State(int value) => Value = value;
                    }

                    private static System.Func<int, global::System.Threading.Tasks.VOID_TASK> Create(bool skip) =>
                        value => skip ? VOID_TASK.CompletedTask : Write(value);
                    private static global::System.Threading.Tasks.VOID_TASK Write(int value)
                    {
                        s_count += value;
                        return VOID_TASK.CompletedTask;
                    }
                    private static CountReader ReadCount() => () => new State(s_count).Value;
                    public static int Execute()
                    {
                        Create(true)(11).GetAwaiter().GetResult();
                        Create(false)(3).GetAwaiter().GetResult();
                        return ReadCount()();
                    }
                }
            }
            """;
        GeneratorDriver driver = CSharpGeneratorDriver.Create([new SyncContractGenerator().AsSourceGenerator()])
            .RunGeneratorsAndUpdateCompilation(CreateCompilation(Source.Replace("VOID_TASK", taskType)), out Compilation compilation, out ImmutableArray<Diagnostic> diagnostics);
        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        using MemoryStream output = new MemoryStream();
        Microsoft.CodeAnalysis.Emit.EmitResult emitted = compilation.Emit(output);
        Assert.True(emitted.Success, string.Join(Environment.NewLine, emitted.Diagnostics));
        Assembly assembly = Assembly.Load(output.ToArray());
        Type type = assembly.GetType("System.Runtime.Serialization.SyncReflectionClassWriter", throwOnError: true)!;
        Assert.Equal(3, type.GetMethod("Execute")!.Invoke(null, null));
        Assert.NotNull(type.GetNestedType("State", BindingFlags.NonPublic));
        Assert.NotNull(type.GetNestedType("CountReader", BindingFlags.NonPublic));
        Assert.Equal(typeof(Action<int>), type.GetMethod("Create", BindingFlags.Static | BindingFlags.NonPublic)!.ReturnType);
        Assert.Equal(typeof(void), type.GetMethod("Write", BindingFlags.Static | BindingFlags.NonPublic)!.ReturnType);
        Assert.Equal(typeof(void), assembly.GetType("System.Runtime.Serialization.SyncReflectionWriter", throwOnError: true)!.GetMethod("Invoke")!.ReturnType);
        Assert.Equal(typeof(object), assembly.GetType("System.Runtime.Serialization.SyncReflectionValueReader", throwOnError: true)!.GetMethod("Invoke")!.ReturnType);
        Assert.Equal(typeof(int), assembly.GetType("System.Runtime.Serialization.SyncReflectionPolicy", throwOnError: true)!.GetMethod("ReadValue")!.ReturnType);
    }

    [Theory]
    [InlineData("ValueTask.FromException(new FormatException())", false)]
    [InlineData("new ValueTask(Task.FromException(new FormatException()))", false)]
    [InlineData("ValueTask.CompletedTask", false)]
    [InlineData("ValueTask.FromException(new FormatException())", true)]
    [InlineData("new ValueTask(Task.FromException(new FormatException()))", true)]
    [InlineData("ValueTask.CompletedTask", true)]
    public async Task ContractLoweringRejectsUnloweredVoidOperations(string operation, bool indirect)
    {
        string source = $$"""
            namespace System.Runtime.Serialization
            {
                internal delegate ValueTask ExternalCallback();
                internal static class External
                {
                    internal static ValueTask Operation() => {{operation}};
                }
                internal class ReflectionClassWriter
                {
                    private static Func<ValueTask> Create() => () =>
                        {{(indirect ? "new ExternalCallback(External.Operation)()" : "External.Operation()")}};
                }
            }
            """;
        CSharpCompilation compilation = CreateCompilation(source);
        ImmutableArray<Diagnostic> diagnostics = await compilation.WithAnalyzers(
            ImmutableArray.Create<DiagnosticAnalyzer>(new SyncContractLoweringAnalyzer())).GetAnalyzerDiagnosticsAsync();
        Assert.Contains(diagnostics, diagnostic => diagnostic.Id == "DCSG002" &&
            diagnostic.GetMessage().Contains("void operations must call a lowered synchronous policy or delegate", StringComparison.Ordinal));
        GeneratorDriver driver = CSharpGeneratorDriver.Create([new SyncContractGenerator().AsSourceGenerator()]).RunGenerators(compilation);
        Assert.Single(driver.GetRunResult().GeneratedTrees);
        Assert.Empty(driver.GetRunResult().Diagnostics);
    }

    [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported))]
    [InlineData(false, -4)]
    [InlineData(true, 10)]
    public void ContractLoweringComposesWithTheGeneratedMemberLoop(bool required, int expected)
    {
        const string Source = """
            namespace System.Runtime.Serialization
            {
                internal delegate ValueTask<object> ReflectionObjectReader(int value);
                internal sealed partial class ReflectionXmlReader
                {
                    private struct WithRequired { }
                    private struct WithoutRequired { }
                    private ReflectionObjectReader Create<TMode>(bool required) where TMode : struct, IReflectionSerialization =>
                        required ? (value) => new ValueTask<object>(ReadMembersSync<WithRequired>(value)) :
                            (value) => new ValueTask<object>(ReadMembersSync<WithoutRequired>(value));
                    private static async ValueTask<object> ReadMembers<TMode, TPlan>(int value)
                        where TMode : struct, IReflectionSerialization where TPlan : struct
                    {
                        int delta = await TMode.ReadValue().ConfigureAwait(false);
                        return typeof(TPlan) == typeof(WithRequired) ? value + delta : value - delta;
                    }
                    public object Execute(bool required) => Create<SyncReflectionSerialization>(required)(3).Result;
                }
            }
            """;
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [new SyncReaderGenerator().AsSourceGenerator(), new SyncContractGenerator().AsSourceGenerator()])
            .RunGeneratorsAndUpdateCompilation(CreateCompilation(Source), out Compilation compilation, out ImmutableArray<Diagnostic> diagnostics);
        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        using MemoryStream output = new MemoryStream();
        Microsoft.CodeAnalysis.Emit.EmitResult emitted = compilation.Emit(output);
        Assert.True(emitted.Success, string.Join(Environment.NewLine, emitted.Diagnostics));
        Assembly assembly = Assembly.Load(output.ToArray());
        Type type = assembly.GetType("System.Runtime.Serialization.SyncReflectionXmlReader", throwOnError: true)!;
        object reader = Activator.CreateInstance(type)!;
        Assert.Equal(expected, type.GetMethod("Execute")!.Invoke(reader, new object[] { required }));
    }

    private static CSharpCompilation CreateCompilation(string source)
    {
        string corelib = typeof(object).Assembly.Location;
        string directory = Path.GetDirectoryName(corelib)!;
        return CSharpCompilation.Create(nameof(SyncReaderGeneratorTests),
            [CSharpSyntaxTree.ParseText(Foundation + source)],
            [
                MetadataReference.CreateFromFile(corelib),
                MetadataReference.CreateFromFile(Path.Combine(directory, "netstandard.dll")),
                MetadataReference.CreateFromFile(Path.Combine(directory, "System.Runtime.dll"))
            ],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    private static GeneratorDriver CreateDriver() => CSharpGeneratorDriver.Create(
        [new SyncReaderGenerator().AsSourceGenerator()],
        driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));

    private static async Task<Assembly> CompileAsync(string source)
    {
        GeneratorDriver driver = CreateDriver().RunGeneratorsAndUpdateCompilation(CreateCompilation(source), out Compilation compilation, out ImmutableArray<Diagnostic> diagnostics);
        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.Empty(await compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new SyncReaderLoweringAnalyzer())).GetAnalyzerDiagnosticsAsync());
        foreach (SyntaxTree tree in driver.GetRunResult().GeneratedTrees)
        {
            Assert.DoesNotContain(tree.GetRoot().DescendantNodes(), node => node is AwaitExpressionSyntax);
            Assert.DoesNotContain(tree.GetRoot().DescendantNodes().OfType<MemberAccessExpressionSyntax>(), member =>
                member.Name.Identifier.ValueText == "CompletedTask");
            Assert.All(tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>(), method =>
            {
                Assert.DoesNotContain(method.Modifiers, modifier => modifier.IsKind(SyntaxKind.AsyncKeyword));
                Assert.DoesNotContain(method.ParameterList.Parameters, parameter => parameter.Type!.ToString().Contains(nameof(CancellationToken), StringComparison.Ordinal));
                Assert.True(method.Identifier.ValueText == "Read" || method.Identifier.ValueText.EndsWith("Sync", StringComparison.Ordinal));
            });
        }

        using MemoryStream output = new MemoryStream();
        Microsoft.CodeAnalysis.Emit.EmitResult result = compilation.Emit(output);
        Assert.True(result.Success, string.Join(Environment.NewLine, result.Diagnostics));
        return Assembly.Load(output.ToArray());
    }
}
