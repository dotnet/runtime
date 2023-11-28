// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

public sealed class X: IComparable<X>
{
    int ival;

    public X(int i)
    {
        ival = i;
    }

    public int CompareTo(X x)
    {
        return ival - x.ival;
    }
    
    public bool Equals(X x)
    {
        return ival == x.ival;
    }
}

public class Y<T> where T : IComparable<T>
{
    public static int C(T x, T y)
    {
        // IL here is
        // ldarga 0
        // ldarg  1
        // constrained ... callvirt ...
        // 
        // The ldarga blocks both caller-arg direct sub and type
        // propagation since the jit thinks arg0 might be redefined.
        //
        // For ref types the ldarga is undone in codegen just before
        // the call so we end up with *(&arg0) and we know this is
        // arg0. Ideally we'd also understand that this pattern can't
        // lead to reassignment, but our view of the callee and what
        // it does with address-taken args is quite limited.
        //
        // Even if we can't propagate the caller's value or type, we
        // might be able to retype the generic __Canon for arg0 as the
        // more specific type that the caller is using (here, X).
        //
        // An interesting variant on this would be to derive from X
        // (say with XD) and have the caller pass instances of XD
        // instead of instances of X. We'd need to make sure we retype
        // arg0 as X and not XD.
        return x.CompareTo(y);
    }
}

public class Z
{
    [Fact]
    public static int TestEntryPoint()
    {
        // Ideally inlining Y.C would enable the interface call in Y
        // to be devirtualized, since we know the exact type of the
        // first argument. We can't get this yet.
        int result = Y<X>.C(new X(103), new X(3));
        return result;
    }
}
