// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Xunit;

public class Base
{
    public virtual int GetValue(int value)
    {
        return value + 33;
    }
}

public sealed class Derived : Base
{
    public override int GetValue(int value)
    {
        return value + 44;
    }
}

// We currently fail to devirtualize these two calls to GetValue
//
// In the array case we need to handle getting types from INDEX operations.
//
// In the list case we need to get a better type for the generic return value,
//  or handle the INDEX during late devirtualization, since inlining exposes
//  the underlying array. Better to do the former since it doesn't rely on
//  being able to inline.

public class Test_fromcollection
{
    static Derived[] arrayOfDerived = new Derived[3];
    static List<Derived> listOfDerived = new List<Derived>();

    [Fact]
    public static int TestEntryPoint()
    {
        for (int i = 0; i < 3; i++)
        {
            Derived d = new Derived();
            arrayOfDerived[i] = d;
            listOfDerived.Add(d);
        }

        int result = 9 + arrayOfDerived[1].GetValue(1) + listOfDerived[1].GetValue(2);

        return result;
    }
}
