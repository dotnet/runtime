// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

// <StdHeader/>
// <Description>
// Section 7.6
// The ++ and -- operators also support postfix
// notation. The result of x++ or x-- is the value
// of x before the operation, whereas the result
// of ++x or --X is the value of x after the operation.
// In either case, x itself has the same value after the
// operation.
// </Description>
//<Expects Status=success></Expects>

// <Code> 

using System;

class MyClass
{

    public static int Main()
    {

        float test1 = 2.0f;
        float test2 = test1++;
        float test3 = ++test1;

        if ((test2 == 2.0f) && (test3 == 4.0f))
        {
            return 100;
        }
        else
        {
            return 1;
        }

        //return 1;
    }
}
// </Code> 
