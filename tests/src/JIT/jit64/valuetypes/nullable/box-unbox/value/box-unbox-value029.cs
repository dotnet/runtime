// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// <Area> Nullable - Box-Unbox </Area>
// <Title> Nullable type with unbox box expr  </Title>
// <Description>  
// checking type of NotEmptyStructConstrainedGenA<int> using is operator
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
        return Helper.Compare((NotEmptyStructConstrainedGenA<int>)o, Helper.Create(default(NotEmptyStructConstrainedGenA<int>)));
    }

    private static bool BoxUnboxToQ(ValueType o)
    {
        return Helper.Compare((NotEmptyStructConstrainedGenA<int>?)o, Helper.Create(default(NotEmptyStructConstrainedGenA<int>)));
    }

    private static int Main()
    {
        NotEmptyStructConstrainedGenA<int>? s = Helper.Create(default(NotEmptyStructConstrainedGenA<int>));

        if (BoxUnboxToNQ(s) && BoxUnboxToQ(s))
            return ExitCode.Passed;
        else
            return ExitCode.Failed;
    }
}


