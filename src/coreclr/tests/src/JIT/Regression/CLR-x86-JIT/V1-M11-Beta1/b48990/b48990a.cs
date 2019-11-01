// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

// <StdHeader/>
// <Description>
// Section 4.1
// If the result of a floating-point operation is too small for 
// the destination format, the result of the operation becomes
// positive zero or negative zero.
// </Description>

// <Expects Status=success></Expects>

// <Code> 
using System;

public class MyClass
{
    public static int Main()
    {

        float f1 = float.Epsilon;
        double d1 = double.Epsilon;

        if ((float)(f1 / 2.0f) != 0.0f)
        {
            return 1;
        }
        if ((float)(f1 * 0.5f) != 0.0f)
        {
            return 1;
        }
        if ((double)(d1 / 2.0d) != (double)0.0d)
        {
            return 1;
        }
        if ((double)(d1 * 0.5d) != (double)0.0d)
        {
            return 1;
        }

        return 100;
    }
}
// </Code>
