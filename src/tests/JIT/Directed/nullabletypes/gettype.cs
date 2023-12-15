// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Collections.Generic;
using Xunit;

class C<T>
{
    public IEnumerable<T> Data { get; set; }

    public C() { }

    public bool Check()
    {
	return Data.ElementAt(0).GetType() == typeof(bool);
    }
}

public class P
{
    [Fact]
    public static int TestEntryPoint()
    {
        C<bool?> c = new();

        // Try a nullable with value
        c.Data = new List<bool?> { true };
        if(!c.Check())
            return 666;

        // Try a nullable without value. Should throw NRE
        c.Data = new List<bool?> { new Nullable<bool>() };

        bool thrown = false;
        try
        {
            c.Check();
        }
        catch(NullReferenceException)
        {
            thrown = true;
        }
        if(!thrown)
            return 667;
        return 100;
    }
}

