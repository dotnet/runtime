// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// <Area> Nullable - Box-Unbox </Area>
// <Title> Nullable type with unbox box expr  </Title>
// <Description>  
// checking type of EmptyStructGen<int> using is operator
// </Description> 
// <RelatedBugs> </RelatedBugs>  
//<Expects Status=success></Expects>
// <Code> 


using System.Runtime.InteropServices;
using System;

internal class NullableTest
{
    private static bool BoxUnboxToNQ(object o)
    {
        return Helper.Compare((EmptyStructGen<int>)o, Helper.Create(default(EmptyStructGen<int>)));
    }

    private static bool BoxUnboxToQ(object o)
    {
        return Helper.Compare((EmptyStructGen<int>?)o, Helper.Create(default(EmptyStructGen<int>)));
    }

    private static int Main()
    {
        EmptyStructGen<int>? s = Helper.Create(default(EmptyStructGen<int>));

        if (BoxUnboxToNQ(s) && BoxUnboxToQ(s))
            return ExitCode.Passed;
        else
            return ExitCode.Failed;
    }
}


