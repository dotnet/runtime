// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

// <StdHeader/>
// <Description>
// Section 7.6
// For an operation of the form -x, operator overload
// resolution is applied to select a specific operator
// implementation.  The operand is converted to the
// parameter type of the selected operator, and the
// type of the result is the return type of the operator.
// </Description>
//<Expects Status=success></Expects>

// <Code> 

using System;

class MyClass
{

    public static int Main()
    {
        long test1 = long.MinValue;
        long test2 = 0;
        try
        {
            checked
            {
                test2 = -test1;
            }
        }
        catch (OverflowException)
        {
            return 100;
        }
        return 1;
    }
}
// </Code> 