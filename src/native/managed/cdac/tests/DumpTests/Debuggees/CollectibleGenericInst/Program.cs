// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;

/// <summary>
/// Debuggee for cDAC dump tests — exercises the RuntimeTypeSystem contract's
/// <c>GetConstructedType</c> loader-module computation for a generic instantiation
/// whose type argument lives in a collectible <see cref="AssemblyLoadContext"/>.
///
/// The runtime assigns such an instantiation to the loader module of the collectible
/// argument (not the generic definition's module), so the constructed type is
/// registered in that module's <c>AvailableParamTypes</c> table. This guards the
/// regression where cDAC searched the definition's module instead.
///
/// Loads a copy of this assembly into a collectible ALC, takes a type from it,
/// constructs <c>List&lt;CollectibleArg&gt;</c> over that type, roots the instance
/// through a GC handle, then crashes.
/// </summary>
internal static class Program
{
    private static void Main()
    {
        AssemblyLoadContext alc = new("cdac-collectible-alc", isCollectible: true);

        byte[] assemblyBytes = File.ReadAllBytes(typeof(Program).Assembly.Location);
        Assembly collectibleAssembly = alc.LoadFromStream(new MemoryStream(assemblyBytes));

        Type collectibleArgType = collectibleAssembly.GetType(typeof(CollectibleArg).FullName!)!;
        Type listType = typeof(List<>).MakeGenericType(collectibleArgType);
        object genericInstance = Activator.CreateInstance(listType)!;

        GCHandle handle = GCHandle.Alloc(genericInstance, GCHandleType.Normal);

        GC.KeepAlive(alc);
        GC.KeepAlive(collectibleAssembly);
        GC.KeepAlive(genericInstance);
        GC.KeepAlive(handle);

        Environment.FailFast("cDAC dump test: CollectibleGenericInst debuggee intentional crash");
    }

    // Loaded into a collectible ALC and used as the type argument of List<>.
    public class CollectibleArg
    {
    }
}
