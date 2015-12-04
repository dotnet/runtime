// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// <Area> Nullable - CastClass </Area>
// <Title> Nullable type with castclass expr  </Title>
// <Description>  
// checking type of EmptyStruct using cast expr
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
        return ((ValueType)(object)o) == null;
    }

    private static bool BoxUnboxToQGen<T>(T? o) where T : struct
    {
        return ((T?)(object)(ValueType)o) == null;
    }

    private static bool BoxUnboxToNQ(object o)
    {
        return ((ValueType)o) == null;
    }

    private static bool BoxUnboxToQ(object o)
    {
        return ((EmptyStruct?)(ValueType)o) == null;
    }

    private static int Main()
    {
        EmptyStruct? s = null;

        if (BoxUnboxToNQ(s) && BoxUnboxToQ(s) && BoxUnboxToNQGen(s) && BoxUnboxToQGen(s))
            return ExitCode.Passed;
        else
            return ExitCode.Failed;
    }
}


