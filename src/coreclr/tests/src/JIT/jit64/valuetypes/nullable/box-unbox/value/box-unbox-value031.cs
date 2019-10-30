// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// <Area> Nullable - Box-Unbox </Area>
// <Title> Nullable type with unbox box expr  </Title>
// <Description>  
// checking type of NotEmptyStructConstrainedGenQA<int> using is operator
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
        return Helper.Compare((NotEmptyStructConstrainedGenQA<int>)o, Helper.Create(default(NotEmptyStructConstrainedGenQA<int>)));
    }

    private static bool BoxUnboxToQ(ValueType o)
    {
        return Helper.Compare((NotEmptyStructConstrainedGenQA<int>?)o, Helper.Create(default(NotEmptyStructConstrainedGenQA<int>)));
    }

    private static int Main()
    {
        NotEmptyStructConstrainedGenQA<int>? s = Helper.Create(default(NotEmptyStructConstrainedGenQA<int>));

        if (BoxUnboxToNQ(s) && BoxUnboxToQ(s))
            return ExitCode.Passed;
        else
            return ExitCode.Failed;
    }
}


