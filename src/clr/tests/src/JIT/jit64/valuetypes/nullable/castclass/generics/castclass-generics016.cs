// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// <Area> Nullable - CastClass </Area>
// <Title> Nullable type with castclass expr  </Title>
// <Description>  
// checking type of Guid using cast expr
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
        return Helper.Compare((Guid)(ValueType)(object)o, Helper.Create(default(Guid)));
    }

    private static bool BoxUnboxToQ<T>(T o)
    {
        return Helper.Compare((Guid?)(ValueType)(object)o, Helper.Create(default(Guid)));
    }

    private static int Main()
    {
        Guid? s = Helper.Create(default(Guid));

        if (BoxUnboxToNQ(s) && BoxUnboxToQ(s))
            return ExitCode.Passed;
        else
            return ExitCode.Failed;
    }
}


