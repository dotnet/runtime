// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

internal class test
{
    public static int Main()
    {
        float x;
        float y;

        bool pass = true;

        x = -10.0F;
        y = 4.0F;
        x = x + y;
        if (x != -6)
        {
            Console.WriteLine("\nInitial parameters: x is -10.0F and y is 4.0F");
            Console.WriteLine("x = x + y failed.	x: {0}, \texpected: -6\n", x);
            pass = false;
        }

        x = -10.0F;
        y = 4.0F;
        x = x - y;
        if (x != -14)
        {
            Console.WriteLine("\nInitial parameters: x is -10.0F and y is 4.0F");
            Console.WriteLine("x = x - y failed.	x: {0}, \texpected: -14\n", x);
            pass = false;
        }

        x = -10.0F;
        y = 4.0F;
        x = x * y;
        if (x != -40)
        {
            Console.WriteLine("\nInitial parameters: x is -10.0F and y is 4.0F");
            Console.WriteLine("x = x * y failed.	x: {0}, \texpected: -40\n", x);
            pass = false;
        }

        x = -10.0F;
        y = 4.0F;
        x = x / y;
        if (x != -2.5)
        {
            Console.WriteLine("\nInitial parameters: x is -10.0F and y is 4.0F");
            Console.WriteLine("x = x / y failed.	x: {0}, \texpected: -2.5\n", x);
            pass = false;
        }

        x = -10.0F;
        y = 4.0F;
        x = x % y;
        if (x != -2)
        {
            Console.WriteLine("\nInitial parameters: x is -10.0F and y is 4.0F");
            Console.WriteLine("x = x % y failed.	x: {0}, \texpected: -2\n", x);
            pass = false;
        }

        x = -10.0F;
        y = 4.0F;
        x += x + y;
        if (x != -16)
        {
            Console.WriteLine("\nInitial parameters: x is -10.0F and y is 4.0F");
            Console.WriteLine("x += x + y failed.	x: {0}, \texpected: -16\n", x);
            pass = false;
        }

        x = -10.0F;
        y = 4.0F;
        x += x - y;
        if (x != -24)
        {
            Console.WriteLine("\nInitial parameters: x is -10.0F and y is 4.0F");
            Console.WriteLine("x += x - y failed.	x: {0}, \texpected: -24\n", x);
            pass = false;
        }

        x = -10.0F;
        y = 4.0F;
        x += x * y;
        if (x != -50)
        {
            Console.WriteLine("\nInitial parameters: x is -10.0F and y is 4.0F");
            Console.WriteLine("x += x * y failed.	x: {0}, \texpected: -50\n", x);
            pass = false;
        }

        x = -10.0F;
        y = 4.0F;
        x += x / y;
        if (x != -12.5)
        {
            Console.WriteLine("\nInitial parameters: x is -10.0F and y is 4.0F");
            Console.WriteLine("x += x / y failed.	x: {0}, \texpected: -12.5\n", x);
            pass = false;
        }

        x = -10.0F;
        y = 4.0F;
        x += x % y;
        if (x != -12)
        {
            Console.WriteLine("\nInitial parameters: x is -10.0F and y is 4.0F");
            Console.WriteLine("x += x % y failed.	x: {0}, \texpected: -12\n", x);
            pass = false;
        }

        x = -10.0F;
        y = 4.0F;
        x -= x + y;
        if (x != -4)
        {
            Console.WriteLine("\nInitial parameters: x is -10.0F and y is 4.0F");
            Console.WriteLine("x -= x + y failed.	x: {0}, \texpected: -4\n", x);
            pass = false;
        }

        x = -10.0F;
        y = 4.0F;
        x -= x - y;
        if (x != 4)
        {
            Console.WriteLine("\nInitial parameters: x is -10.0F and y is 4.0F");
            Console.WriteLine("x -= x - y failed.	x: {0}, \texpected: 4\n", x);
            pass = false;
        }

        x = -10.0F;
        y = 4.0F;
        x -= x * y;
        if (x != 30)
        {
            Console.WriteLine("\nInitial parameters: x is -10.0F and y is 4.0F");
            Console.WriteLine("x -= x * y failed.	x: {0}, \texpected: 30\n", x);
            pass = false;
        }

        x = -10.0F;
        y = 4.0F;
        x -= x / y;
        if (x != -7.5)
        {
            Console.WriteLine("\nInitial parameters: x is -10.0F and y is 4.0F");
            Console.WriteLine("x -= x / y failed.	x: {0}, \texpected: -7.5\n", x);
            pass = false;
        }

        x = -10.0F;
        y = 4.0F;
        x -= x % y;
        if (x != -8)
        {
            Console.WriteLine("\nInitial parameters: x is -10.0F and y is 4.0F");
            Console.WriteLine("x -= x % y failed.	x: {0}, \texpected: -8\n", x);
            pass = false;
        }

        x = -10.0F;
        y = 4.0F;
        x *= x + y;
        if (x != 60)
        {
            Console.WriteLine("\nInitial parameters: x is -10.0F and y is 4.0F");
            Console.WriteLine("x *= x + y failed.	x: {0}, \texpected: 60\n", x);
            pass = false;
        }

        x = -10.0F;
        y = 4.0F;
        x *= x - y;
        if (x != 140)
        {
            Console.WriteLine("\nInitial parameters: x is -10.0F and y is 4.0F");
            Console.WriteLine("x *= x - y failed.	x: {0}, \texpected: 140\n", x);
            pass = false;
        }

        x = -10.0F;
        y = 4.0F;
        x *= x * y;
        if (x != 400)
        {
            Console.WriteLine("\nInitial parameters: x is -10.0F and y is 4.0F");
            Console.WriteLine("x *= x * y failed.	x: {0}, \texpected: 400\n", x);
            pass = false;
        }

        x = -10.0F;
        y = 4.0F;
        x *= x / y;
        if (x != 25)
        {
            Console.WriteLine("\nInitial parameters: x is -10.0F and y is 4.0F");
            Console.WriteLine("x *= x / y failed.	x: {0}, \texpected: 25\n", x);
            pass = false;
        }

        x = -10.0F;
        y = 4.0F;
        x *= x % y;
        if (x != 20)
        {
            Console.WriteLine("\nInitial parameters: x is -10.0F and y is 4.0F");
            Console.WriteLine("x *= x % y failed.	x: {0}, \texpected: 20\n", x);
            pass = false;
        }

        x = -10.0F;
        y = 4.0F;
        x /= x + y;
        if (!x.Equals(1.66666663F))
        {
            Console.WriteLine("\nInitial parameters: x is -10.0F and y is 4.0F");
            Console.WriteLine("x /= x + y failed.	x: {0}, \texpected: 1.66666663F\n", x);
            pass = false;
        }

        x = -10.0F;
        y = 4.0F;
        x /= x - y;
        if (!x.Equals(0.714285731F))
        {
            Console.WriteLine("\nInitial parameters: x is -10.0F and y is 4.0F");
            Console.WriteLine("x /= x - y failed.	x: {0}, \texpected: 0.714285731F\n", x);
            pass = false;
        }

        x = -10.0F;
        y = 4.0F;
        x /= x * y;
        if (x != 0.25)
        {
            Console.WriteLine("\nInitial parameters: x is -10.0F and y is 4.0F");
            Console.WriteLine("x /= x * y failed.	x: {0}, \texpected: 0.25\n", x);
            pass = false;
        }

        x = -10.0F;
        y = 4.0F;
        x /= x / y;
        if (x != 4)
        {
            Console.WriteLine("\nInitial parameters: x is -10.0F and y is 4.0F");
            Console.WriteLine("x /= x / y failed.	x: {0}, \texpected: 4\n", x);
            pass = false;
        }

        x = -10.0F;
        y = 4.0F;
        x /= x % y;
        if (x != 5)
        {
            Console.WriteLine("\nInitial parameters: x is -10.0F and y is 4.0F");
            Console.WriteLine("x /= x % y failed.	x: {0}, \texpected: 5\n", x);
            pass = false;
        }

        if (pass)
        {
            Console.WriteLine("PASSED");
            return 100;
        }
        else
            return 1;
    }
}
