// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Numerics;
using System.Runtime.CompilerServices;

public class Runtime_71601
{
    public static int Main()
    {
        if (ProblemWithPrimitiveSrc())
        {
            return 101;
        }

        if (ProblemWithPrimitiveDst())
        {
            return 102;
        }

        return 100;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool ProblemWithPrimitiveSrc()
    {
        WrapTuple p = new WrapTuple { FieldOne = { Value = 1 }, FieldTwo = { Value = 2 } };
        Wrap a = p.FieldTwo;
        Wrap b = WrapTuple.GetFieldTwo(ref p);

        if (a.Value != b.Value)
        {
            return true;
        }

        a = p.FieldOne;
        b = WrapTuple.GetFieldOne(ref p);

        if (a.Value != b.Value)
        {
            return true;
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool ProblemWithPrimitiveDst()
    {
        WrapTuple p = new WrapTuple { FieldOne = { Value = 1 }, FieldTwo = { Value = 2 } };
        Wrap a = p.FieldTwo;
        WrapTuple.GetFieldTwo(ref p) = a;

        if (a.Value == p.FieldOne.Value)
        {
            return true;
        }

        WrapTuple.GetFieldOne(ref p) = a;

        if (a.Value != p.FieldOne.Value)
        {
            return true;
        }

        return false;
    }

    struct WrapTuple
    {
        public Wrap FieldOne;
        public Wrap FieldTwo;

        public static ref Wrap GetFieldOne(ref WrapTuple t) => ref Unsafe.Add(ref t.FieldTwo, -1);
        public static ref Wrap GetFieldTwo(ref WrapTuple t) => ref Unsafe.Add(ref t.FieldOne, 1);
    }

    struct Wrap
    {
        public int Value;
    }
}
