// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Reflection.Emit.Tests;

public class DynamicMethodTests
{
    /// <summary>
    /// Reproduces https://github.com/dotnet/runtime/issues/78365
    /// </summary>
    [Fact]
    public void ConvertDynamicMethodDelegateToAnotherDelegateType()
    {
        DynamicMethod dynamicMethod = new("GetLength", typeof(int), new[] { typeof(string) });
        ILGenerator il = dynamicMethod.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Callvirt, typeof(string).GetProperty(nameof(string.Length))!.GetMethod);
        il.Emit(OpCodes.Ret);

        Func<string, int> getLength = dynamicMethod.CreateDelegate<Func<string, int>>();
        Assert.Equal(2, getLength("bb"));

        Func<int> getTargetLength = getLength.Method.CreateDelegate<Func<int>>("ccc");
        Assert.Equal(3, getTargetLength());

        Assert.Equal(getLength, getTargetLength.Method.CreateDelegate<Func<string, int>>());
    }
}
