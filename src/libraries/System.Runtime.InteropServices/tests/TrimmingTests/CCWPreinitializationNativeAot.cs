using System;
using System.Reflection;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

var comTypeInfo = StrategyBasedComWrappers.DefaultIUnknownInterfaceDetailsStrategy.GetIUnknownDerivedDetails(typeof(IComInterface).TypeHandle)!;
Type cwType = comTypeInfo.Implementation;
unsafe
{
    nint* vtable = (nint*)comTypeInfo.ManagedVirtualMethodTable;

    if (HasCctor(cwType))
        return -1;

    ComWrappers.GetIUnknownImpl(
        out nint queryInterface,
        out nint addRef,
        out nint release);
    if (vtable[0] != queryInterface || vtable[1] != addRef || vtable[2] != release)
        return -2;
}

return 100;

[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2070:UnrecognizedReflectionPattern",
        Justification = "Yep, we don't want to keep the cctor if it wasn't kept")]
static bool HasCctor(Type type)
{
    return type.GetConstructor(BindingFlags.NonPublic | BindingFlags.Static, null, Type.EmptyTypes, null) != null;
}

[GeneratedComInterface]
[Guid("ad358058-2b72-4801-8d98-043d44dc42c4")]
partial interface IComInterface
{
    int Method();
}