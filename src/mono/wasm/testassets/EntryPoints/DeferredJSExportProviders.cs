// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;

[assembly: SupportedOSPlatform("browser")]
[assembly: TypeMap<JSExportCatalog>("CountingLibrary", typeof(AssemblyProviderTest.CountingCatalog))]
[assembly: TypeMap<JSExportProvider>("[CountingLibrary]AssemblyProviderTest.CountingExports:Add#1", typeof(AssemblyProviderTest.CountingAddProvider))]
[assembly: TypeMap<JSExportProvider>("[CountingLibrary]AssemblyProviderTest.CountingExports:Add", typeof(AssemblyProviderTest.CountingAddProvider))]
[assembly: TypeMap<JSExportProvider>("[CountingLibrary]AssemblyProviderTest.CountingExports:Unused#2", typeof(AssemblyProviderTest.UnusedProvider))]
[assembly: TypeMap<JSExportProvider>("[CountingLibrary]AssemblyProviderTest.CountingExports:Unused", typeof(AssemblyProviderTest.UnusedProvider))]

namespace AssemblyProviderTest;

internal sealed class CountingCatalog : JSExportCatalog
{
    public CountingCatalog() { }

    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor, typeof(CountingCatalog))]
    public override string[] GetExportNames() =>
        ["[CountingLibrary]AssemblyProviderTest.CountingExports:Add#1", "[CountingLibrary]AssemblyProviderTest.CountingExports:Unused#2"];
}

internal sealed class CountingAddProvider : JSExportProvider
{
    public CountingAddProvider() => Console.WriteLine("TestOutput -> lazy:constructor");

    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor, typeof(CountingAddProvider))]
    public override unsafe void Bind()
    {
        Console.WriteLine("TestOutput -> lazy:bind");
        JSFunctionBinding.BindManagedFunction(
            "[CountingLibrary]AssemblyProviderTest.CountingExports:Add", 1,
            [JSMarshalerType.Int32, JSMarshalerType.Int32, JSMarshalerType.Int32],
            &Add);
    }

    private static unsafe void Add(JSMarshalerArgument* arguments)
    {
        try
        {
            arguments[2].ToManaged(out int left);
            arguments[3].ToManaged(out int right);
            if (left < 0)
                throw new InvalidOperationException("Deferred export failure");
            arguments[1].ToJS(left + right);
        }
        catch (Exception exception)
        {
            arguments[0].ToJS(exception);
        }
    }
}

internal sealed class UnusedProvider : JSExportProvider
{
    public UnusedProvider() => throw new InvalidOperationException("An unused export provider must not be activated.");

    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor, typeof(UnusedProvider))]
    public override void Bind() => throw new InvalidOperationException("An unused export must not be bound.");
}
