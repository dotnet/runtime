// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// <Area> Nullable - Box-Unbox </Area>
// <Title> Nullable type with unbox box expr  </Title>
// <Description>  
// checking type of ImplementOneInterfaceGen<int> using is operator
// </Description> 
// <RelatedBugs> </RelatedBugs>  
//<Expects Status=success></Expects>
// <Code> 


using System.Runtime.InteropServices;
using System;
using Xunit;

public class NullableTest
{
    private static bool BoxUnboxToNQ(IEmptyGen<int> o)
    {
        return Helper.Compare((ImplementOneInterfaceGen<int>)o, Helper.Create(default(ImplementOneInterfaceGen<int>)));
    }

    private static bool BoxUnboxToQ(IEmptyGen<int> o)
    {
        return Helper.Compare((ImplementOneInterfaceGen<int>?)o, Helper.Create(default(ImplementOneInterfaceGen<int>)));
    }

    [Fact]
    public static int TestEntryPoint()
    {
        ImplementOneInterfaceGen<int>? s = Helper.Create(default(ImplementOneInterfaceGen<int>));

        if (BoxUnboxToNQ(s) && BoxUnboxToQ(s))
            return ExitCode.Passed;
        else
            return ExitCode.Failed;
    }
}


