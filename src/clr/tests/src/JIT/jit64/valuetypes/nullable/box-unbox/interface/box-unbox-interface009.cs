// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// <Area> Nullable - Box-Unbox </Area>
// <Title> Nullable type with unbox box expr  </Title>
// <Description>  
// checking type of sbyte using is operator
// </Description> 
// <RelatedBugs> </RelatedBugs>  
//<Expects Status=success></Expects>
// <Code> 


using System.Runtime.InteropServices;
using System;

internal class NullableTest
{
    private static bool BoxUnboxToNQ(IComparable o)
    {
        return Helper.Compare((sbyte)o, Helper.Create(default(sbyte)));
    }

    private static bool BoxUnboxToQ(IComparable o)
    {
        return Helper.Compare((sbyte?)o, Helper.Create(default(sbyte)));
    }

    private static int Main()
    {
        sbyte? s = Helper.Create(default(sbyte));

        if (BoxUnboxToNQ(s) && BoxUnboxToQ(s))
            return ExitCode.Passed;
        else
            return ExitCode.Failed;
    }
}


