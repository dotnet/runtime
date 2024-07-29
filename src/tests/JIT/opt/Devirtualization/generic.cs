// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

interface Ix
{
    int F();
    int G();
}

public class B<T> : Ix
{
    int Ix.F() 
    { 
        if (typeof(T) == typeof(string))
        {
            return 3; 
        }
        else
        {
            return 5;
        }
    }
    
    public virtual int G()
    {
        if (typeof(T) == typeof(object))
        {
            return 7; 
        }
        else
        {
            return 11;
        }

    }
}

public class D : B<string>, Ix
{
    int Ix.F() { return 13; }
}

class E : D
{
    public sealed override int G() { return 17; }
}

// K overrides E.G for interface purposes, even though it is sealed
class K : E, Ix
{
    int Ix.G() { return 19; }
}

sealed class J : E, Ix
{
    int Ix.F() { return 21; }
}

public class Z
{
    static int IxF(Ix x) { return x.F(); }
    static int IxG(Ix x) { return x.G(); }

    [Fact]
    public static int TestEntryPoint()
    {
        E e = new E();
        K k = new K();
        J j = new J();
        E q = k;

        int callsBFs = IxF(new B<string>());
        int callsBFo = IxF(new B<object>());
        int callsBGo = IxG(new B<object>());
        int callsBGs = IxG(new B<string>()) + IxG(new D());
        int callsDF  = IxF(new D()) + IxF(e) + IxF(k) + IxF(q);
        int callsEG  = IxG(e) + IxG(j);
        int callsKG  = IxG(k) + IxG(q);
        int callsJF  = IxF(j);

        int expected = 3 + 5 + 7 + 2 * 11 + 4 * 13 + 2 * 17 + 2 * 19 + 21;
        int val = callsBFs + callsBFo + callsDF + callsBGs + callsBGo + callsEG + callsKG + callsJF;

        return val - expected + 100;
    }
}


