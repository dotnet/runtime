// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

interface Ix<T> where T : class
{
    T F();
}

class Base : Ix<string>
{
    public virtual string F() { return "B"; }
}

class Derived : Base
{
    public override string F() { return "D"; }
}

class Bx
{
    public Ix<string> Get() { return new Derived(); }
}

public class Z
{
    static string X(Base b)
    {
        return b.F();
    }

    [Fact]
    public static int TestEntryPoint()
    {
        // Would like to be able to late devirtualize the call to F
        // here after inlining Get exposes the exact type of the
        // object, but since the return type of Get is a (shared)
        // interface type, we need the exact context for F to do so
        // safely.
        // 
        // Unfortunately we lose track of that context, because when
        // we import the call to F, it is not an inline candidate.
        string s = new Bx().Get().F();
        return (int) s[0] + 32;
    }
}

