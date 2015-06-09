// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
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

        if ((float)(f1 / 2.0f) != 0.0f)
        {
            Console.WriteLine("epsilon/2 failed");
        }
        if ((float)(f1 * 0.5f) != 0.0f)
        {
            Console.WriteLine("epsilon * 0.5 failed");
        }

        return 100;
    }
}
// </Code>