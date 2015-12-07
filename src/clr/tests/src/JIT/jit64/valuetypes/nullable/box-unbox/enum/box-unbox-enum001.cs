// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// <Area> Nullable - Box-Unbox </Area>
// <Title> Nullable type with unbox box expr  </Title>
// <Description>  
// checking type of IntE using is operator
// </Description> 
// <RelatedBugs> </RelatedBugs>  
//<Expects Status=success></Expects>
// <Code> 


using System.Runtime.InteropServices;
using System;

internal class NullableTest
{
    private static bool BoxUnboxToNQ(Enum o)
    {
        return Helper.Compare((IntE)o, Helper.Create(default(IntE)));
    }

    private static bool BoxUnboxToQ(Enum o)
    {
        return Helper.Compare((IntE?)o, Helper.Create(default(IntE)));
    }

    private static int Main()
    {
        IntE? s = Helper.Create(default(IntE));

        if (BoxUnboxToNQ(s) && BoxUnboxToQ(s))
            return ExitCode.Passed;
        else
            return ExitCode.Failed;
    }
}


