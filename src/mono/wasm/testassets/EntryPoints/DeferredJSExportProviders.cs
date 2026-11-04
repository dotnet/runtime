// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;

[assembly: SupportedOSPlatform("browser")]
[assembly: TypeMap<JSExportProvider>("CountingLibrary", typeof(AssemblyProviderTest.CountingAssemblyProvider))]

namespace AssemblyProviderTest;

internal sealed partial class CountingAssemblyProvider : JSExportProvider
{
    public CountingAssemblyProvider() => Console.WriteLine("TestOutput -> lazy:constructor");

    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor, typeof(CountingAssemblyProvider))]
    public override ReadOnlySpan<string> GetExportNames() =>
        new string[] { "[CountingLibrary]AssemblyProviderTest.CountingExports:Add#1", "[CountingLibrary]AssemblyProviderTest.CountingExports:Unused#2" };

    public override unsafe void Bind(string methodId)
    {
        switch (methodId)
        {
            case "0":
                Console.WriteLine("TestOutput -> lazy:bind");
                JSFunctionBinding.BindManagedFunction(
                    "[CountingLibrary]AssemblyProviderTest.CountingExports:Add", 1,
                    [JSMarshalerType.Int32, JSMarshalerType.Int32, JSMarshalerType.Int32],
                    &Add);
                return;
            case "1":
                throw new InvalidOperationException("An unused export must not be bound.");
            default:
                throw new ArgumentOutOfRangeException(nameof(methodId), methodId, "Unknown JSExport method ID.");
        }
    }

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
