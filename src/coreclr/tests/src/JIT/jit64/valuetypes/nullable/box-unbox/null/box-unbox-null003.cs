// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// <Area> Nullable - Box-Unbox </Area>
// <Title> Nullable type with unbox box expr  </Title>
// <Description>  
// checking type of byte using is operator
// </Description> 
// <RelatedBugs> </RelatedBugs>  
//<Expects Status=success></Expects>
// <Code> 


using System.Runtime.InteropServices;
using System;

internal class NullableTest
{
    private static bool BoxUnboxToNQGen<T>(T o)
    {
        return ((object)o) == null;
    }

    private static bool BoxUnboxToQGen<T>(T? o) where T : struct
    {
        return ((T?)o) == null;
    }

    private static bool BoxUnboxToNQ(object o)
    {
        return o == null;
    }

    private static bool BoxUnboxToQ(object o)
    {
        return ((byte?)o) == null;
    }

    private static int Main()
    {
        byte? s = null;

        if (BoxUnboxToNQ(s) && BoxUnboxToQ(s) && BoxUnboxToNQGen(s) && BoxUnboxToQGen(s))
            return ExitCode.Passed;
        else
            return ExitCode.Failed;
    }
}


