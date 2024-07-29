// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// <Area> Nullable - Box-Unbox </Area>
// <Title> Nullable type with unbox box expr  </Title>
// <Description>  
// checking type of LongE using is operator
// </Description> 
// <RelatedBugs> </RelatedBugs>  
//<Expects Status=success></Expects>
// <Code> 


using System.Runtime.InteropServices;
using System;
using Xunit;

public class NullableTest
{
    private static bool BoxUnboxToNQ(Enum o)
    {
        return Helper.Compare((LongE)o, Helper.Create(default(LongE)));
    }

    private static bool BoxUnboxToQ(Enum o)
    {
        return Helper.Compare((LongE?)o, Helper.Create(default(LongE)));
    }

    [Fact]
    public static int TestEntryPoint()
    {
        LongE? s = Helper.Create(default(LongE));

        if (BoxUnboxToNQ(s) && BoxUnboxToQ(s))
            return ExitCode.Passed;
        else
            return ExitCode.Failed;
    }
}


