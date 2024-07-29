// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// <Area> Nullable - Box-Unbox </Area>
// <Title> Nullable type with unbox box expr  </Title>
// <Description>  
// checking type of ulong using is operator
// </Description> 
// <RelatedBugs> </RelatedBugs>  
//<Expects Status=success></Expects>
// <Code> 


using System.Runtime.InteropServices;
using System;
using Xunit;

public class NullableTest
{
    private static bool BoxUnboxToNQ(IComparable o)
    {
        return Helper.Compare((ulong)o, Helper.Create(default(ulong)));
    }

    private static bool BoxUnboxToQ(IComparable o)
    {
        return Helper.Compare((ulong?)o, Helper.Create(default(ulong)));
    }

    [Fact]
    public static int TestEntryPoint()
    {
        ulong? s = Helper.Create(default(ulong));

        if (BoxUnboxToNQ(s) && BoxUnboxToQ(s))
            return ExitCode.Passed;
        else
            return ExitCode.Failed;
    }
}


