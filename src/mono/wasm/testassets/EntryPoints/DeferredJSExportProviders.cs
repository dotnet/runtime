// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using System.Threading.Tasks;

[assembly: SupportedOSPlatform("browser")]
[assembly: TypeMap<JSExportProvider>("CountingLibrary", typeof(AssemblyProviderTest.CountingAssemblyProvider))]

namespace AssemblyProviderTest;

internal sealed partial class CountingAssemblyProvider : JSExportProvider
{
    private int _asyncBindAttempts;

    public CountingAssemblyProvider() => Console.WriteLine("TestOutput -> lazy:constructor");

    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor, typeof(CountingAssemblyProvider))]
    public override ReadOnlySpan<string> GetExportNames()
    {
        RegisterSignature(this, [JSMarshalerType.Int32, JSMarshalerType.Int32, JSMarshalerType.Int32]);
        RegisterSignature(this, [JSMarshalerType.Int32]);
        RegisterSignature(this, [JSMarshalerType.Task(JSMarshalerType.Int32), JSMarshalerType.Int32]);
        return new string[]
        {
            "[CountingLibrary]AssemblyProviderTest.CountingExports:Add#1",
            "[CountingLibrary]AssemblyProviderTest.CountingExports:Unused#2",
            "[CountingLibrary]AssemblyProviderTest.CountingExports:RetryAsync#3"
        };
    }

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "RegisterSignature")]
    private static extern void RegisterSignature(JSExportProvider provider, ReadOnlySpan<JSMarshalerType> signatures);

    public override unsafe void Bind(string methodId)
    {
        switch (methodId)
        {
            case "0":
                Console.WriteLine("TestOutput -> lazy:bind");
                ProbeBindingReentry();
                JSFunctionBinding.BindManagedFunction(
                    "[CountingLibrary]AssemblyProviderTest.CountingExports:Add", 1,
                    [JSMarshalerType.Int32, JSMarshalerType.Int32, JSMarshalerType.Int32],
                    &Add);
                ProbeBindingReentry();
                return;
            case "1":
                throw new InvalidOperationException("An unused export must not be bound.");
            case "2":
                if (++_asyncBindAttempts == 1)
                    throw new InvalidOperationException("Deferred binding failure");

                JSFunctionBinding.BindManagedFunction(
                    "[CountingLibrary]AssemblyProviderTest.CountingExports:RetryAsync", 3,
                    [JSMarshalerType.Task(JSMarshalerType.Int32), JSMarshalerType.Int32],
                    &RetryAsync);
                return;
            default:
                throw new ArgumentOutOfRangeException(nameof(methodId), methodId, "Unknown JSExport method ID.");
        }
    }

    private static unsafe void RetryAsync(JSMarshalerArgument* arguments)
    {
        arguments[2].ToManaged(out int value);
        arguments[1].ToJS(Task.FromResult(value + 1), static (ref JSMarshalerArgument result, int value) => result.ToJS(value));
    }

    [JSImport("globalThis.probeDeferredBindingReentry")]
    private static partial void ProbeBindingReentry();

    private static unsafe void Add(JSMarshalerArgument* arguments)
    {
        try
        {
            arguments[2].ToManaged(out int left);
            arguments[3].ToManaged(out int right);
            if (left < 0)
            {
#if TEST_MONO
                DisposeOrdinaryProxies(false, false);
#endif
                throw new InvalidOperationException("Deferred export failure");
            }
            arguments[1].ToJS(left + right);
        }
        catch (Exception exception)
        {
            arguments[0].ToJS(exception);
        }

    }

#if TEST_MONO
    [JSImport("INTERNAL.forceDisposeProxies")]
    private static partial void DisposeOrdinaryProxies(bool disposeMethods, bool verbose);
#endif
}
