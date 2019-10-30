// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// <Area> Nullable - Box-Unbox </Area>
// <Title> Nullable type with unbox box expr  </Title>
// <Description>  
// checking type of WithMultipleGCHandleStruct using is operator
// </Description> 
// <RelatedBugs> </RelatedBugs>  
//<Expects Status=success></Expects>
// <Code> 


using System.Runtime.InteropServices;
using System;

internal class NullableTest
{
    private static bool BoxUnboxToNQ(ValueType o)
    {
        return Helper.Compare((WithMultipleGCHandleStruct)o, Helper.Create(default(WithMultipleGCHandleStruct)));
    }

    private static bool BoxUnboxToQ(ValueType o)
    {
        return Helper.Compare((WithMultipleGCHandleStruct?)o, Helper.Create(default(WithMultipleGCHandleStruct)));
    }

    private static int Main()
    {
        WithMultipleGCHandleStruct? s = Helper.Create(default(WithMultipleGCHandleStruct));

        if (BoxUnboxToNQ(s) && BoxUnboxToQ(s))
            return ExitCode.Passed;
        else
            return ExitCode.Failed;
    }
}


