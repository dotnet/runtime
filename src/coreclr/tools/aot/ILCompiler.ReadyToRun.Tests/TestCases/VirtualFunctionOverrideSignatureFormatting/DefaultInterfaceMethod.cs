// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

public interface IDefaultInterfaceMethod<TSelf> where TSelf : IDefaultInterfaceMethod<TSelf>
{
    static virtual int GetValue() => 1;
}

public struct DefaultInterfaceMethodImpl : IDefaultInterfaceMethod<DefaultInterfaceMethodImpl>
{
}

public static class VirtualFunctionOverrideSignatureFormatting
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int Call<T>() where T : IDefaultInterfaceMethod<T> => T.GetValue();

    public static int Run() => Call<DefaultInterfaceMethodImpl>();
}
