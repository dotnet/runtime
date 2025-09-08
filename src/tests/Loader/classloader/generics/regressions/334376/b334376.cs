// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This test verifies the types load correcly.
// Previously the types that have comments //TypeLoadException did not load.
// TypeLoadException was thrown when type decl has form RType<T0> : IType<RType<ref type>>

using System;
using System.Collections.Generic;
using System.Text;
using Xunit;


interface IType<T0>
{
}

class RType1<T0> : IType<RType1<string>>
{
}

class RType2<T0> : IType<RType2<int>>
{
}

class RType3<T0> : IType<RType3<DateTime>>
{
}

class RType4
{
}

struct VType1<T0> : IType<RType1<string>>
{
}

struct VType2<T0> : IType<VType2<string>>
{
}

struct VType3<T0> : IType<VType3<int>>
{
}

public class Program
{
    [Fact]
    public static void TestEntryPoint()
    {
        RType2<int> rtype2      = new RType2<int>();                // type loads
        RType3<DateTime> rtype3 = new RType3<DateTime>();           // type loads
        RType1<string> rtype1   = new RType1<string>();             // TypeLoadException

        VType3<string> vtype3   = new VType3<string>();             // type loads
        VType1<string> vtype1   = new VType1<string>();             // TypeLoadException
        
        VType2<string> vtype2   = new VType2<string>();             // TypeLoadException

        // we need this to get rid of compiler warning 
        // warning CS0219: The variable 'vtype3' is assigned but its value is never used
        vtype3.ToString();
        vtype1.ToString();
        vtype2.ToString();
    }
}

