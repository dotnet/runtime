using System;
using System.Reflection;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

Type cwType = StrategyBasedComWrappers.DefaultIUnknownInterfaceDetailsStrategy.GetIUnknownDerivedDetails(typeof(IComInterface).TypeHandle).Implementation;
if (HasCctor(cwType))
    return -1;
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