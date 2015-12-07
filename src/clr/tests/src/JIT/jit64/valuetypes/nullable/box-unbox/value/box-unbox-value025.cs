// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// <Area> Nullable - Box-Unbox </Area>
// <Title> Nullable type with unbox box expr  </Title>
// <Description>  
// checking type of NotEmptyStructQA using is operator
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
        return Helper.Compare((NotEmptyStructQA)o, Helper.Create(default(NotEmptyStructQA)));
    }

    private static bool BoxUnboxToQ(ValueType o)
    {
        return Helper.Compare((NotEmptyStructQA?)o, Helper.Create(default(NotEmptyStructQA)));
    }

    private static int Main()
    {
        NotEmptyStructQA? s = Helper.Create(default(NotEmptyStructQA));

        if (BoxUnboxToNQ(s) && BoxUnboxToQ(s))
            return ExitCode.Passed;
        else
            return ExitCode.Failed;
    }
}


