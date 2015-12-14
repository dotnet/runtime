// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

internal class NullableTest
{
    private static bool BoxUnboxToNQ(Enum o)
    {
        return Helper.Compare((LongE)o, Helper.Create(default(LongE)));
    }

    private static bool BoxUnboxToQ(Enum o)
    {
        return Helper.Compare((LongE?)o, Helper.Create(default(LongE)));
    }

    private static int Main()
    {
        LongE? s = Helper.Create(default(LongE));

        if (BoxUnboxToNQ(s) && BoxUnboxToQ(s))
            return ExitCode.Passed;
        else
            return ExitCode.Failed;
    }
}


