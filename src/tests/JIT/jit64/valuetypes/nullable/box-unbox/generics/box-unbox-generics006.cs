// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// <Area> Nullable - Box-Unbox </Area>
// <Title> Nullable type with unbox box expr  </Title>
// <Description>  
// checking type of ushort using is operator
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
        return Helper.Compare((ushort)(object)o, Helper.Create(default(ushort)));
    }

    private static bool BoxUnboxToQ<T>(T o)
    {
        return Helper.Compare((ushort?)(object)o, Helper.Create(default(ushort)));
    }

    [Fact]
    public static int TestEntryPoint()
    {
        ushort? s = Helper.Create(default(ushort));

        if (BoxUnboxToNQ(s) && BoxUnboxToQ(s))
            return ExitCode.Passed;
        else
            return ExitCode.Failed;
    }
}


