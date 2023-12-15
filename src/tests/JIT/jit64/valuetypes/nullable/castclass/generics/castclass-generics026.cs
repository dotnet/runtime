// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// <Area> Nullable - CastClass </Area>
// <Title> Nullable type with castclass expr  </Title>
// <Description>  
// checking type of EmptyStructGen<int> using cast expr
// </Description> 
// <RelatedBugs> </RelatedBugs>  
//<Expects Status=success></Expects>
// <Code> 


using System.Runtime.InteropServices;
using System;
using Xunit;

public class NullableTest
{
    private static bool BoxUnboxToNQ<T>(T o)
    {
        return Helper.Compare((EmptyStructGen<int>)(ValueType)(object)o, Helper.Create(default(EmptyStructGen<int>)));
    }

    private static bool BoxUnboxToQ<T>(T o)
    {
        return Helper.Compare((EmptyStructGen<int>?)(ValueType)(object)o, Helper.Create(default(EmptyStructGen<int>)));
    }

    [Fact]
    public static int TestEntryPoint()
    {
        EmptyStructGen<int>? s = Helper.Create(default(EmptyStructGen<int>));

        if (BoxUnboxToNQ(s) && BoxUnboxToQ(s))
            return ExitCode.Passed;
        else
            return ExitCode.Failed;
    }
}


