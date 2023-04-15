// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// <Area> Nullable - Box-Unbox </Area>
// <Title> Nullable type with unbox box expr  </Title>
// <Description>  
// checking type of UIntPtr using is operator
// </Description> 
// <RelatedBugs> </RelatedBugs>  
//<Expects Status=success></Expects>
// <Code> 


using System.Runtime.InteropServices;
using System;
using Xunit;

public class NullableTest
{
    private static bool BoxUnboxToNQ(ValueType o)
    {
        return Helper.Compare((UIntPtr)o, Helper.Create(default(UIntPtr)));
    }

    private static bool BoxUnboxToQ(ValueType o)
    {
        return Helper.Compare((UIntPtr?)o, Helper.Create(default(UIntPtr)));
    }

    [Fact]
    public static int TestEntryPoint()
    {
        UIntPtr? s = Helper.Create(default(UIntPtr));

        if (BoxUnboxToNQ(s) && BoxUnboxToQ(s))
            return ExitCode.Passed;
        else
            return ExitCode.Failed;
    }
}


