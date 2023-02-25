// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// <Area> Nullable - Box-Unbox </Area>
// <Title> Nullable type with unbox box expr  </Title>
// <Description>  
// checking type of ExplicitFieldOffsetStruct using is operator
// </Description> 
// <RelatedBugs> </RelatedBugs>  
//<Expects Status=success></Expects>
// <Code> 


using System.Runtime.InteropServices;
using System;
using Xunit;

public class NullableTest
{
    private static bool BoxUnboxToNQ(object o)
    {
        return Helper.Compare((ExplicitFieldOffsetStruct)o, Helper.Create(default(ExplicitFieldOffsetStruct)));
    }

    private static bool BoxUnboxToQ(object o)
    {
        return Helper.Compare((ExplicitFieldOffsetStruct?)o, Helper.Create(default(ExplicitFieldOffsetStruct)));
    }

    [Fact]
    public static int TestEntryPoint()
    {
        ExplicitFieldOffsetStruct? s = Helper.Create(default(ExplicitFieldOffsetStruct));

        if (BoxUnboxToNQ(s) && BoxUnboxToQ(s))
            return ExitCode.Passed;
        else
            return ExitCode.Failed;
    }
}


