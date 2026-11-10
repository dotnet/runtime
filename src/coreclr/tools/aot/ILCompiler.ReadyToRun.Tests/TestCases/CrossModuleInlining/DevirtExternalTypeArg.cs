// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

public struct LocalTag
{
}

public interface ITestInterface
{
    int GetId();
}

public class GenericImpl<T> : ITestInterface
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public virtual int GetId() => 1;
}

public static class DevirtExternalTypeArg
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Type GetExternalConstructedType() =>
        typeof(GenericImpl<ExternalLib.ExternalType>);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int CallVirtualOnExternalArg(GenericImpl<ExternalLib.ExternalType> obj) =>
        obj.GetId();

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int CallGuardedInterfaceOnLocalArg(ITestInterface obj)
    {
        if (obj.GetType() == typeof(GenericImpl<LocalTag>))
            return obj.GetId();
        return 0;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int CallInterfaceOnLocalArg()
    {
        ITestInterface obj = new GenericImpl<LocalTag>();
        return obj.GetId();
    }
}

public static class ExternalInterfaceCaller
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int CallGuardedInterfaceOnExternalArg(ITestInterface obj)
    {
        if (obj.GetType() == typeof(GenericImpl<ExternalLib.ExternalType>))
            return obj.GetId();
        return 0;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int CallInterfaceOnExternalArg()
    {
        ITestInterface obj = new GenericImpl<ExternalLib.ExternalType>();
        return obj.GetId();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool CallDevirtOnCrossModuleCreatedType(ExternalLib.PublicNonVersionableHolder<LocalTag> holder, bool flag, object other)
    {
        object obj = holder.GetDerived(flag);
        return obj.GetType() == other.GetType();
    }
}
