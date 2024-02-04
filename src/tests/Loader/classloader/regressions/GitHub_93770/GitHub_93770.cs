// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Xunit;

namespace ReproGH93770;

// By default in Mono every class that implements interfaces has 19
// interface method table slots that are used to dispatch interface
// calls.  The methods are assigned to slots based on a hash of some
// metadata.  This test tries to create at least one IMT slot that has
// a collision by creating an interface with 20 virtual methods.
//
// The bug is that if the method also has a non-virtual generic
// method, the IMT slot with the collision calls the wrong vtable
// element.

public class ReproGH93770
{
    [Fact]
    public static void TestEntryPoint()
    {
        C c = new C();
        I1 i1 = c;
        Helper(i1);
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Helper(I1 i1)
    {
    var e = 0;
    var n = i1.M0();
    Assert.Equal(e, n);
    e++;
    n = i1.M1();
    Assert.Equal(e, n);
    e++;
    n = i1.M2();
    Assert.Equal(e, n);
    e++;
    n = i1.M3();
    Assert.Equal(e, n);
    e++;
    n = i1.M4();
    Assert.Equal(e, n);
    e++;
    n = i1.M5();
    Assert.Equal(e, n);
    e++;
    n = i1.M6();
    Assert.Equal(e, n);
    e++;
    n = i1.M7();
    Assert.Equal(e, n);
    e++;
    n = i1.M8();
    Assert.Equal(e, n);
    e++;
    n = i1.M9();
    Assert.Equal(e, n);
    e++;
    n = i1.M10();
    Assert.Equal(e, n);
    e++;
    n = i1.M11();
    Assert.Equal(e, n);
    e++;
    n = i1.M12();
    Assert.Equal(e, n);
    e++;
    n = i1.M13();
    Assert.Equal(e, n);
    e++;
    n = i1.M14();
    Assert.Equal(e, n);
    e++;
    n = i1.M15();
    Assert.Equal(e, n);
    e++;
    n = i1.M16();
    Assert.Equal(e, n);
    e++;
    n = i1.M17();
    Assert.Equal(e, n);
    e++;
    n = i1.M18();
    Assert.Equal(e, n);
    e++;
    n = i1.M19();
    Assert.Equal(e, n);
    e++;
    }
}

public interface I1 {
    public static T Id<T> (T t)=> t;

    public int M0() => 0;
    public int M1() => 1;
    public int M2() => 2;
    public int M3() => 3;
    public int M4() => 4;

    public int M5() => 5;
    public int M6() => 6;
    public int M7() => 7;
    public int M8() => 8;
    public int M9() => 9;
    public int M10() => 10;
    public int M11() => 11;
    public int M12() => 12;
    public int M13() => 13;
    public int M14() => 14;
    public int M15() => 15;
    public int M16() => 16;
    public int M17() => 17;
    public int M18() => 18;
    public int M19() => 19;

}

public class C : I1 {

}


