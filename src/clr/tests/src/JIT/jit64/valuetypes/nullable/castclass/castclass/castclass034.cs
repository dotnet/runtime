// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// <Area> Nullable - CastClass </Area>
// <Title> Nullable type with castclass expr  </Title>
// <Description>  
// checking type of ExplicitFieldOffsetStruct using cast expr
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
        return Helper.Compare((ExplicitFieldOffsetStruct)(ValueType)o, Helper.Create(default(ExplicitFieldOffsetStruct)));
    }

    private static bool BoxUnboxToQ(object o)
    {
        return Helper.Compare((ExplicitFieldOffsetStruct?)(ValueType)o, Helper.Create(default(ExplicitFieldOffsetStruct)));
    }

    private static int Main()
    {
        ExplicitFieldOffsetStruct? s = Helper.Create(default(ExplicitFieldOffsetStruct));

        if (BoxUnboxToNQ(s) && BoxUnboxToQ(s))
            return ExitCode.Passed;
        else
            return ExitCode.Failed;
    }
}


