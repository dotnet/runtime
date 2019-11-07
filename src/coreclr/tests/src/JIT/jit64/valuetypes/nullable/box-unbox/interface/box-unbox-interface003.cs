// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

internal class NullableTest
{
    private static bool BoxUnboxToNQ(IEmptyGen<int> o)
    {
        return Helper.Compare((ImplementOneInterfaceGen<int>)o, Helper.Create(default(ImplementOneInterfaceGen<int>)));
    }

    private static bool BoxUnboxToQ(IEmptyGen<int> o)
    {
        return Helper.Compare((ImplementOneInterfaceGen<int>?)o, Helper.Create(default(ImplementOneInterfaceGen<int>)));
    }

    private static int Main()
    {
        ImplementOneInterfaceGen<int>? s = Helper.Create(default(ImplementOneInterfaceGen<int>));

        if (BoxUnboxToNQ(s) && BoxUnboxToQ(s))
            return ExitCode.Passed;
        else
            return ExitCode.Failed;
    }
}


