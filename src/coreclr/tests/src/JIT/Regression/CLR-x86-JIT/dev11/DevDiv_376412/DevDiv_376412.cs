// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

// <StdHeader/>
// <Description>
// Section 4.1
// If the result of a floating-point operation is too large
// for the destination format, the result of the operation 
// becomes positive infinity or negative infinity.
// </Description>

// <Expects Status=success></Expects>

// <Code> 
using System;

public class MyClass
{
    public static int Main()
    {

        bool failed = false;
        float f1 = float.MaxValue;
        float f2 = float.PositiveInfinity;
        float f3 = float.NegativeInfinity;

        if ((float)(f1 + (f1 * 1.0e-7f)) != f2)
        {
            Console.WriteLine("Error-1: ((float)(f1 + (f1 * 1.0e-7f)) != f2)");
            failed = true;
        }
        if ((float)(f1 - (-f1 * 1.0e-7f)) != f2)
        {
            Console.WriteLine("Error-2: ((float)(f1 - (-f1 * 1.0e-7f)) != f2)");
            failed = true;
        }
        if ((float)(f1 * (1.0f + 1.0e-7f)) != f2)
        {
            Console.WriteLine("Error-3: ((float)(f1 * (1.0f + 1.0e-7f)) != f2)");
            failed = true;
        }
        if ((float)(f1 / (1.0f - 1.0e-7f)) != f2)
        {
            Console.WriteLine("Error-4: ((float)(f1 / (1.0f - 1.0e-7f)) != f2)");
            failed = true;
        }
        if ((float)(-f1 + (-(f1 * 1.0e-7f))) != f3)
        {
            Console.WriteLine("Error-5: ((float)(-f1 + (-(f1 * 1.0e-7f))) != f3)");
            failed = true;
        }
        if ((float)(-f1 - (f1 * 1.0e-7f)) != f3)
        {
            Console.WriteLine("Error-6: ((float)(-f1 - (f1 * 1.0e-7f)) != f3)");
            failed = true;
        }
        if ((float)(-f1 * (1.0f + 1.0e-7f)) != f3)
        {
            Console.WriteLine("Error-7: ((float)(-f1 * (1.0f + 1.0e-7f)) != f3)");
            failed = true;
        }
        if ((float)(-f1 / (1.0f - 1.0e-7f)) != f3)
        {
            Console.WriteLine("Error-8: ((float)(-f1 / (1.0f - 1.0e-7f)) != f3)");
            failed = true;
        }

        if (!failed)
        {
            Console.WriteLine("PASSED");
            return 100;
        }
        return 101;
    }
}
// </Code>
