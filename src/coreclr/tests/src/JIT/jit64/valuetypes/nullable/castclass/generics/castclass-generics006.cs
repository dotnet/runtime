// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// <Area> Nullable - CastClass </Area>
// <Title> Nullable type with castclass expr  </Title>
// <Description>  
// checking type of ushort using cast expr
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
        return Helper.Compare((ushort)(ValueType)(object)o, Helper.Create(default(ushort)));
    }

    private static bool BoxUnboxToQ<T>(T o)
    {
        return Helper.Compare((ushort?)(ValueType)(object)o, Helper.Create(default(ushort)));
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


