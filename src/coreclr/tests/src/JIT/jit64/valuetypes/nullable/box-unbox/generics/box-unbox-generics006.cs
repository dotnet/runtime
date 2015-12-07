// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

internal class NullableTest
{
    private static bool BoxUnboxToNQ<T>(T o)
    {
        return Helper.Compare((ushort)(object)o, Helper.Create(default(ushort)));
    }

    private static bool BoxUnboxToQ<T>(T o)
    {
        return Helper.Compare((ushort?)(object)o, Helper.Create(default(ushort)));
    }

    private static int Main()
    {
        ushort? s = Helper.Create(default(ushort));

        if (BoxUnboxToNQ(s) && BoxUnboxToQ(s))
            return ExitCode.Passed;
        else
            return ExitCode.Failed;
    }
}


