// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// <Area> Nullable - Box-Unbox </Area>
// <Title> Nullable type with unbox box expr  </Title>
// <Description>  
// checking type of ImplementTwoInterface using is operator
// </Description> 
// <RelatedBugs> </RelatedBugs>  
//<Expects Status=success></Expects>
// <Code> 


using System.Runtime.InteropServices;
using System;
using Xunit;

namespace box_unbox_value039;
public class NullableTest
{
    private static bool BoxUnboxToNQ(ValueType o)
    {
        return Helper.Compare((ImplementTwoInterface)o, Helper.Create(default(ImplementTwoInterface)));
    }

    private static bool BoxUnboxToQ(ValueType o)
    {
        return Helper.Compare((ImplementTwoInterface?)o, Helper.Create(default(ImplementTwoInterface)));
    }

    [Fact]
    [OuterLoop]
    public static int TestEntryPoint()
    {
        ImplementTwoInterface? s = Helper.Create(default(ImplementTwoInterface));

        if (BoxUnboxToNQ(s) && BoxUnboxToQ(s))
            return ExitCode.Passed;
        else
            return ExitCode.Failed;
    }
}


