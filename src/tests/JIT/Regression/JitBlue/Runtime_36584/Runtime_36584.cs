// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

// Finally cloning creates new throw merge candidates that
// need to be properly counted.

public class Runtime_36584
{
    static int x;

    static void ThrowHelper()
    {
        throw new Exception();
    }

    [Fact]
    public static int TestEntryPoint()
    {
        x = 100;

        if (x != 100)
        {
            ThrowHelper();
        }

        if (x != 100)
        {
            ThrowHelper();
        }

        if (x != 100)
        {
            try 
            {
                x++;
            }
            // This finally will be cloned
            finally 
            {
                if (x != 100)
                {
                    ThrowHelper();
                }

                if (x != 100)
                {
                    ThrowHelper();
                }
            }
        }

        return x;
    }
}
