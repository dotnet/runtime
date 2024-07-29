// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// <Area> Nullable - CastClass </Area>
// <Title> Nullable type with castclass expr  </Title>
// <Description>  
// checking type of sbyte using cast expr
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
        return Helper.Compare((sbyte)(IComparable)o, Helper.Create(default(sbyte)));
    }

    private static bool BoxUnboxToQ(object o)
    {
        return Helper.Compare((sbyte?)(IComparable)o, Helper.Create(default(sbyte)));
    }

    [Fact]
    public static int TestEntryPoint()
    {
        sbyte? s = Helper.Create(default(sbyte));

        if (BoxUnboxToNQ(s) && BoxUnboxToQ(s))
            return ExitCode.Passed;
        else
            return ExitCode.Failed;
    }
}


