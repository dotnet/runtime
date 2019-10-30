// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// This test is primarily for detecting bad reverse copy prop on 
// newobj calls.   If the intended target has any interference with the sources 
// of the newobj call, then you can't safely do the reverse copy prop.
//
// s = new S(ref s);
//
// It is similar to restrictions on general return value optimizations using
// a hidden parameter.
//
// Reference: TF Bug 1187059

using System;

public struct  S {
    public int a;
    public int b;

    // Straight forward constructor
    public S(int a, int b) {
        this.a = a;
        this.b = b;
    }
    // Swapping constructor
    public S(ref S s) {
        this.a = s.b;
        this.b = s.a;
    }
    // Swapping constructor
    public S(ref U u) {
        this.a = u.s.b;
        this.b = u.s.a;
    }
    // Swapping constructor
    public unsafe S(ref V v) {
        this.a = v.sRef->b;
        this.b = v.sRef->a;
    }
    // Swapping constructor
    public unsafe S(V v) {
        this.a = v.sRef->b;
        this.b = v.sRef->a;
    }

}

public struct T {
    public S s;

    // Straight forward constructor
    public T(int a, int b) {
        s = new S(a, b);
    }
}

public class U {
    public S s;

    // Straight forward constructor
    public U(int a, int b) {
        s = new S(a, b);
    }
}

public unsafe struct V {
    public S *sRef;

    // Straight forward constructor
    public unsafe V(S * sRef) {
        this.sRef = sRef;
    }
}


public class Bug {
    static S ss;
    public static unsafe int Main() {
        int fail = 0;

        S s = new S(1, 2);
        s = new S(ref s);
        if ((s.a != 2) || (s.b != 1))
            fail += 0x1;

        ss = new S(1, 2);
        ss = new S(ref ss);
        if ((ss.a != 2) || (ss.b != 1))
            fail += 0x2;

        T t = new T(1, 2);
        t.s = new S(ref t.s);
        if ((t.s.a != 2) || (t.s.b != 1))
            fail += 0x4;

        U u = new U(1, 2);
        u.s = new S(ref u.s);
        if ((u.s.a != 2) || (u.s.b != 1))
            fail += 0x8;

        u = new U(1, 2);
        u.s = new S(ref u);
        if ((u.s.a != 2) || (u.s.b != 1))
            fail += 0x10;

        s = new S(1, 2);
        V v = new V(&s);
        s = new S(v);
        if ((s.a != 2) || (s.b != 1))
            fail += 0x20;

        if (fail != 0)
        {
            Console.WriteLine("Fail 0x{0:X}", fail);
            return -1;
        }
        else
        {
            Console.WriteLine("Pass");
            return 100;
        }
    }
}

