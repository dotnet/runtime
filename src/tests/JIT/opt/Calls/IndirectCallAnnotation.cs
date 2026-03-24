// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Xunit;

public class IndirectCallAnnotation
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public virtual int VirtualCallee() => 42;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Caller(IndirectCallAnnotation obj)
    {
        // Verify that the indirect call target is annotated with the method name.
        // ARM64: blr {{.*}}VirtualCallee()
        return obj.VirtualCallee();
    }

    [Fact]
    public static int TestEntryPoint()
    {
        return Caller(new IndirectCallAnnotation()) == 42 ? 100 : 0;
    }
}
