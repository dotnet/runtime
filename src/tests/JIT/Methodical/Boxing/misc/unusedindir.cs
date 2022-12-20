// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace Test_unusedindir_cs
{
public class Program
{
    struct NotPromoted
    {
        public int a, b, c, d, e, f;
    }

    class TypeWithStruct
    {
        public NotPromoted small;

        public TypeWithStruct() => small.c = 100;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Escape(bool b)
    {
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static NotPromoted Test(TypeWithStruct obj)
    {
        NotPromoted t = obj.small;
        // Try to create an OBJ(ADDR(LCL_VAR)) tree that gtTryRemoveBoxUpstreamEffects
        // does not remove due to a spurios exception side effect nor it parents it to
        // a COMMA like many other unused trees are.
        Escape(Unsafe.As<NotPromoted, NotPromoted>(ref t).GetType() == typeof(NotPromoted));
        return t;
    }

    [Fact]
    public static int TestEntryPoint() => Test(new TypeWithStruct()).c;
}
}
