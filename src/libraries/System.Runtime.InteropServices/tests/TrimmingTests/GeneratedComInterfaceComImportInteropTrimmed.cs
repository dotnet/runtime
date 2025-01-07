// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

Type comObject = RemoveTypeTrimAnalysis(typeof(ComObject));

// Ensure that the interop details strategy and all of its nested types are fully trimmed away.
if (GetTypeWithoutTrimAnalysis("System.Runtime.InteropServices.Marshalling.ComImportInteropInterfaceDetailsStrategy", comObject.Assembly) != null)
{
    return -1;
}

// Ensure that the ComInterop object field is trimmed away as well.
if (comObject.GetFields(BindingFlags.NonPublic | BindingFlags.Instance).Any(f => f.Name == "_runtimeCallableWrapper"))
{
    return -2;
}

var comWrappers = new StrategyBasedComWrappers();

var managedObject = new ComClass();
var nativeObject = comWrappers.GetOrCreateComInterfaceForObject(managedObject, CreateComInterfaceFlags.None);
var wrapper = (IComInterface)comWrappers.GetOrCreateObjectForComInstance(nativeObject, CreateObjectFlags.None);
Marshal.Release(nativeObject);

return wrapper.Method();

[MethodImpl(MethodImplOptions.NoInlining)]
static Type RemoveTypeTrimAnalysis(Type type) => type;

[MethodImpl(MethodImplOptions.NoInlining)]
static Type GetTypeWithoutTrimAnalysis(string typeName, Assembly assembly)
{
    return assembly.GetType(typeName, throwOnError: false);
}

[GeneratedComInterface]
[Guid("ad358058-2b72-4801-8d98-043d44dc42c4")]
partial interface IComInterface
{
    int Method();
}

[GeneratedComClass]
partial class ComClass : IComInterface
{
    public int Method() => 100;
}
