// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Numerics;
using System.Runtime.CompilerServices;

public class Runtime_71118
{
    public static int Main()
    {
        return Problem(new ClassWithVtor4 { Vtor4FieldTwo = new Vector4(1, 2, 3, 4) }) ? 101 : 100;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool Problem(ClassWithVtor4 a)
    {
        return CallForVtor4(a.Vtor4FieldTwo) != a.Vtor4FieldTwo.X;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static float CallForVtor4(Vector4 vtor) => vtor.X;

    class ClassWithVtor4
    {
        public Vector4 Vtor4FieldOne;
        public Vector4 Vtor4FieldTwo;
    }
}
