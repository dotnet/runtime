// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

internal class test
{
    public static int Main()
    {
        Int64 x;
        Int64 y;

        bool pass = true;

        x = -10;
        y = 4;

        x = x + y;
        if (x != -6)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4");
            Console.WriteLine("x = x + y   failed.\nx: {0}, \texpected: -6\n", x);
            pass = false;
        }

        x = -10;
        y = 4;

        x = x - y;
        if (x != -14)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4");
            Console.WriteLine("x = x - y   failed.\nx: {0}, \texpected: -14\n", x);
            pass = false;
        }

        x = -10;
        y = 4;

        x = x * y;
        if (x != -40)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4");
            Console.WriteLine("x = x * y   failed.\nx: {0}, \texpected: -40\n", x);
            pass = false;
        }

        x = -10;
        y = 4;

        x = x / y;
        if (x != -2)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4");
            Console.WriteLine("x = x / y   failed.\nx: {0}, \texpected: -2\n", x);
            pass = false;
        }

        x = -10;
        y = 4;

        x = x % y;
        if (x != -2)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4");
            Console.WriteLine("x = x % y   failed.\nx: {0}, \texpected: -2\n", x);
            pass = false;
        }

        x = -10;
        y = 4;

        x = x << (int)y;
        if (x != -160)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4");
            Console.WriteLine("x = x << (int)y   failed\nx: {0}, \texpected: -160\n", x);
            pass = false;
        }

        x = -10;
        y = 4;

        x = x >> (int)y;
        if (x != -1)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4");
            Console.WriteLine("x = x >> (int)y   failed\nx: {0}, \texpected: -1\n", x);
            pass = false;
        }

        x = -10;
        y = 4;

        x = x & y;
        if (x != 4)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4");
            Console.WriteLine("x = x & y   failed.\nx: {0}, \texpected: 4\n", x);
            pass = false;
        }

        x = -10;
        y = 4;

        x = x ^ y;
        if (x != -14)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4");
            Console.WriteLine("x = x ^ y   failed.\nx: {0}, \texpected: -14\n", x);
            pass = false;
        }

        x = -10;
        y = 4;

        x = x | y;
        if (x != -10)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4");
            Console.WriteLine("x = x | y   failed.\nx: {0}, \texpected: -10\n", x);
            pass = false;
        }

        x = -10;
        y = 4;

        x += x + y;
        if (x != -16)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4");
            Console.WriteLine("x += x + y   failed.\nx: {0}, \texpected: -16\n", x);
            pass = false;
        }

        x = -10;
        y = 4;

        x += x - y;
        if (x != -24)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4");
            Console.WriteLine("x += x - y   failed.\nx: {0}, \texpected: -24\n", x);
            pass = false;
        }

        x = -10;
        y = 4;

        x += x * y;
        if (x != -50)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4");
            Console.WriteLine("x += x * y   failed.\nx: {0}, \texpected: -50\n", x);
            pass = false;
        }

        x = -10;
        y = 4;

        x += x / y;
        if (x != -12)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4");
            Console.WriteLine("x += x / y   failed.\nx: {0}, \texpected: -12\n", x);
            pass = false;
        }

        x = -10;
        y = 4;

        x += x % y;
        if (x != -12)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4");
            Console.WriteLine("x += x % y   failed.\nx: {0}, \texpected: -12\n", x);
            pass = false;
        }

        x = -10;
        y = 4;

        x += x << (int)y;
        if (x != -170)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4");
            Console.WriteLine("x += x << (int)y   failed\nx: {0}, \texpected: -170\n", x);
            pass = false;
        }

        x = -10;
        y = 4;

        x += x >> (int)y;
        if (x != -11)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4");
            Console.WriteLine("x += x >> (int)y   failed\nx: {0}, \texpected: -11\n", x);
            pass = false;
        }

        x = -10;
        y = 4;

        x += x & y;
        if (x != -6)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4");
            Console.WriteLine("x += x & y   failed.\nx: {0}, \texpected: -6\n", x);
            pass = false;
        }

        x = -10;
        y = 4;

        x += x ^ y;
        if (x != -24)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4");
            Console.WriteLine("x += x ^ y   failed.\nx: {0}, \texpected: -24\n", x);
            pass = false;
        }

        x = -10;
        y = 4;

        x += x | y;
        if (x != -20)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4");
            Console.WriteLine("x += x | y   failed.\nx: {0}, \texpected: -20\n", x);
            pass = false;
        }

        x = -10;
        y = 4;

        x -= x + y;
        if (x != -4)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4");
            Console.WriteLine("x -= x + y   failed.\nx: {0}, \texpected: -4\n", x);
            pass = false;
        }

        x = -10;
        y = 4;

        x -= x - y;
        if (x != 4)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4");
            Console.WriteLine("x -= x - y   failed.\nx: {0}, \texpected: 4\n", x);
            pass = false;
        }

        x = -10;
        y = 4;

        x -= x * y;
        if (x != 30)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4");
            Console.WriteLine("x -= x * y   failed.\nx: {0}, \texpected: 30\n", x);
            pass = false;
        }

        x = -10;
        y = 4;

        x -= x / y;
        if (x != -8)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4");
            Console.WriteLine("x -= x / y   failed.\nx: {0}, \texpected: -8\n", x);
            pass = false;
        }

        x = -10;
        y = 4;

        x -= x % y;
        if (x != -8)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4");
            Console.WriteLine("x -= x % y   failed.\nx: {0}, \texpected: -8\n", x);
            pass = false;
        }

        x = -10;
        y = 4;

        x -= x << (int)y;
        if (x != 150)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4");
            Console.WriteLine("x -= x << (int)y   failed\nx: {0}, \texpected: 150\n", x);
            pass = false;
        }

        x = -10;
        y = 4;

        x -= x >> (int)y;
        if (x != -9)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4");
            Console.WriteLine("x -= x >> (int)y   failed\nx: {0}, \texpected: -9\n", x);
            pass = false;
        }

        x = -10;
        y = 4;

        x -= x & y;
        if (x != -14)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4");
            Console.WriteLine("x -= x & y   failed.\nx: {0}, \texpected: -14\n", x);
            pass = false;
        }

        x = -10;
        y = 4;

        x -= x ^ y;
        if (x != 4)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4");
            Console.WriteLine("x -= x ^ y   failed.\nx: {0}, \texpected: 4\n", x);
            pass = false;
        }

        x = -10;
        y = 4;

        x -= x | y;
        if (x != 0)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4");
            Console.WriteLine("x -= x | y   failed.\nx: {0}, \texpected: 0\n", x);
            pass = false;
        }

        x = -10;
        y = 4;

        x *= x + y;
        if (x != 60)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4");
            Console.WriteLine("x *= x + y   failed.\nx: {0}, \texpected: 60\n", x);
            pass = false;
        }

        x = -10;
        y = 4;

        x *= x - y;
        if (x != 140)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4");
            Console.WriteLine("x *= x - y   failed.\nx: {0}, \texpected: 140\n", x);
            pass = false;
        }

        x = -10;
        y = 4;

        x *= x * y;
        if (x != 400)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4");
            Console.WriteLine("x *= x * y   failed.\nx: {0}, \texpected: 400\n", x);
            pass = false;
        }

        x = -10;
        y = 4;

        x *= x / y;
        if (x != 20)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4");
            Console.WriteLine("x *= x / y   failed.\nx: {0}, \texpected: 20\n", x);
            pass = false;
        }

        x = -10;
        y = 4;

        x *= x % y;
        if (x != 20)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4");
            Console.WriteLine("x *= x % y   failed.\nx: {0}, \texpected: 20\n", x);
            pass = false;
        }

        x = -10;
        y = 4;

        x *= x << (int)y;
        if (x != 1600)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4");
            Console.WriteLine("x *= x << (int)y   failed\nx: {0}, \texpected: 1600\n", x);
            pass = false;
        }

        x = -10;
        y = 4;

        x *= x >> (int)y;
        if (x != 10)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4");
            Console.WriteLine("x *= x >> (int)y   failed\nx: {0}, \texpected: 10\n", x);
            pass = false;
        }

        x = -10;
        y = 4;

        x *= x & y;
        if (x != -40)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4");
            Console.WriteLine("x *= x & y   failed.\nx: {0}, \texpected: -40\n", x);
            pass = false;
        }

        x = -10;
        y = 4;

        x *= x ^ y;
        if (x != 140)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4");
            Console.WriteLine("x *= x ^ y   failed.\nx: {0}, \texpected: 140\n", x);
            pass = false;
        }

        x = -10;
        y = 4;

        x *= x | y;
        if (x != 100)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4");
            Console.WriteLine("x *= x | y   failed.\nx: {0}, \texpected: 100\n", x);
            pass = false;
        }

        x = -10;
        y = 4;

        x /= x + y;
        if (x != 1)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4");
            Console.WriteLine("x /= x + y   failed.\nx: {0}, \texpected: 1\n", x);
            pass = false;
        }

        x = -10;
        y = 4;

        x /= x - y;
        if (x != 0)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4");
            Console.WriteLine("x /= x - y   failed.\nx: {0}, \texpected: 0\n", x);
            pass = false;
        }

        x = -10;
        y = 4;

        x /= x * y;
        if (x != 0)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4");
            Console.WriteLine("x /= x * y   failed.\nx: {0}, \texpected: 0\n", x);
            pass = false;
        }

        x = -10;
        y = 4;

        x /= x / y;
        if (x != 5)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4");
            Console.WriteLine("x /= x / y   failed.\nx: {0}, \texpected: 5\n", x);
            pass = false;
        }

        x = -10;
        y = 4;

        x /= x % y;
        if (x != 5)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4");
            Console.WriteLine("x /= x % y   failed.\nx: {0}, \texpected: 5\n", x);
            pass = false;
        }

        x = -10;
        y = 4;

        x /= x << (int)y;
        if (x != 0)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4");
            Console.WriteLine("x /= x << (int)y   failed\nx: {0}, \texpected: 0\n", x);
            pass = false;
        }

        x = -10;
        y = 4;

        x /= x >> (int)y;
        if (x != 10)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4");
            Console.WriteLine("x /= x >> (int)y   failed\nx: {0}, \texpected: 10\n", x);
            pass = false;
        }

        x = -10;
        y = 4;

        x /= x & y;
        if (x != -2)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4");
            Console.WriteLine("x /= x & y   failed.\nx: {0}, \texpected: -2\n", x);
            pass = false;
        }

        x = -10;
        y = 4;

        x /= x ^ y;
        if (x != 0)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4");
            Console.WriteLine("x /= x ^ y   failed.\nx: {0}, \texpected: 0\n", x);
            pass = false;
        }

        x = -10;
        y = 4;

        x /= x | y;
        if (x != 1)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4");
            Console.WriteLine("x /= x | y   failed.\nx: {0}, \texpected: 1\n", x);
            pass = false;
        }

        x = -10;
        y = 4;

        x %= x + y;
        if (x != -4)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4");
            Console.WriteLine("x %= x + y   failed.\nx: {0}, \texpected: -4\n", x);
            pass = false;
        }

        x = -10;
        y = 4;

        x %= x - y;
        if (x != -10)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4");
            Console.WriteLine("x %= x - y   failed.\nx: {0}, \texpected: -10\n", x);
            pass = false;
        }

        x = -10;
        y = 4;

        x %= x * y;
        if (x != -10)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4");
            Console.WriteLine("x %= x * y   failed.\nx: {0}, \texpected: -10\n", x);
            pass = false;
        }

        x = -10;
        y = 4;

        x %= x / y;
        if (x != 0)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4");
            Console.WriteLine("x %= x / y   failed.\nx: {0}, \texpected: 0\n", x);
            pass = false;
        }

        x = -10;
        y = 4;

        x %= x % y;
        if (x != 0)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4");
            Console.WriteLine("x %= x % y   failed.\nx: {0}, \texpected: 0\n", x);
            pass = false;
        }

        x = -10;
        y = 4;

        x %= x << (int)y;
        if (x != -10)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4");
            Console.WriteLine("x %= x << (int)y   failed\nx: {0}, \texpected: -10\n", x);
            pass = false;
        }

        x = -10;
        y = 4;

        x %= x >> (int)y;
        if (x != 0)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4");
            Console.WriteLine("x %= x >> (int)y   failed\nx: {0}, \texpected: 0\n", x);
            pass = false;
        }

        x = -10;
        y = 4;

        x %= x & y;
        if (x != -2)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4");
            Console.WriteLine("x %= x & y   failed.\nx: {0}, \texpected: -2\n", x);
            pass = false;
        }

        x = -10;
        y = 4;

        x %= x ^ y;
        if (x != -10)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4");
            Console.WriteLine("x %= x ^ y   failed.\nx: {0}, \texpected: -10\n", x);
            pass = false;
        }

        x = -10;
        y = 4;

        x %= x | y;
        if (x != 0)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4");
            Console.WriteLine("x %= x | y   failed.\nx: {0}, \texpected: 0\n", x);
            pass = false;
        }

        x = -10;
        y = 4;

        /*
		x <<= (int)( x + y);
		if (x != -671088640)
		{
			Console.WriteLine("\nInitial parameters: x is -10 and y is 4");
			Console.WriteLine("x <<= (int)( x + y)   failed.\nx: {0}, \texpected: -671088640\n", x);
			pass = false;
		}

		x = -10;
		y = 4;

		x <<= (int)( x - y);
		if (x != -2621440)
		{
			Console.WriteLine("\nInitial parameters: x is -10 and y is 4");
			Console.WriteLine("x <<= (int)( x - y)   failed.\nx: {0}, \texpected: -2621440\n", x);
			pass = false;
		}

		x = -10;
		y = 4;

		x <<= (int)( x * y);
		if (x != -167772160)
		{
			Console.WriteLine("\nInitial parameters: x is -10 and y is 4");
			Console.WriteLine("x <<= (int)( x * y)   failed.\nx: {0}, \texpected: -167772160\n", x);
			pass = false;
		}

		x = -10;
		y = 4;

		x <<= (int)( x / y);
		if (x != -2147483648)
		{
			Console.WriteLine("\nInitial parameters: x is -10 and y is 4");
			Console.WriteLine("x <<= (int)( x / y)   failed.\nx: {0}, \texpected: -2147483648\n", x);
			pass = false;
		}

		x = -10;
		y = 4;

		x <<= (int)( x % y);
		if (x != -2147483648)
		{
			Console.WriteLine("\nInitial parameters: x is -10 and y is 4");
			Console.WriteLine("x <<= (int)( x % y)   failed.\nx: {0}, \texpected: -2147483648\n", x);
			pass = false;
		}

		x = -10;
		y = 4;

		x <<= (int)( x << (int)y);
		if (x != -10)
		{
			Console.WriteLine("\nInitial parameters: x is -10 and y is 4");
			Console.WriteLine("x <<= (int)( x << (int)y)   failed.\nx: {0}, \texpected: -10\n", x);
			pass = false;
		}
		*/

        x = -10;
        y = 4;

        x <<= (int)(x >> (int)y);
        if (x != 0)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4");
            Console.WriteLine("x <<= (int)( x >> (int)y)   failed.\nx: {0}, \texpected: 0\n", x);
            pass = false;
        }

        x = -10;
        y = 4;

        x <<= (int)(x & y);
        if (x != -160)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4");
            Console.WriteLine("x <<= (int)( x & y)   failed.\nx: {0}, \texpected: -160\n", x);
            pass = false;
        }

        x = -10;
        y = 4;

        /*
		x <<= (int)( x ^ y);
		if (x != -2621440)
		{
			Console.WriteLine("\nInitial parameters: x is -10 and y is 4");
			Console.WriteLine("x <<= (int)( x ^ y)   failed.\nx: {0}, \texpected: -2621440\n", x);
			pass = false;
		}

		x = -10;
		y = 4;

		x <<= (int)( x | y);
		if (x != -41943040)
		{
			Console.WriteLine("\nInitial parameters: x is -10 and y is 4");
			Console.WriteLine("x <<= (int)( x | y)   failed.\nx: {0}, \texpected: -41943040\n", x);
			pass = false;
		}
		*/

        x = -10;
        y = 4;

        x >>= (int)(x + y);
        if (x != -1)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4");
            Console.WriteLine("x >>= (int)( x + y)   failed.\nx: {0}, \texpected: -1\n", x);
            pass = false;
        }

        x = -10;
        y = 4;

        x >>= (int)(x - y);
        if (x != -1)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4");
            Console.WriteLine("x >>= (int)( x - y)   failed.\nx: {0}, \texpected: -1\n", x);
            pass = false;
        }

        x = -10;
        y = 4;

        x >>= (int)(x * y);
        if (x != -1)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4");
            Console.WriteLine("x >>= (int)( x * y)   failed.\nx: {0}, \texpected: -1\n", x);
            pass = false;
        }

        x = -10;
        y = 4;

        x >>= (int)(x / y);
        if (x != -1)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4");
            Console.WriteLine("x >>= (int)( x / y)   failed.\nx: {0}, \texpected: -1\n", x);
            pass = false;
        }

        x = -10;
        y = 4;

        x >>= (int)(x % y);
        if (x != -1)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4");
            Console.WriteLine("x >>= (int)( x % y)   failed.\nx: {0}, \texpected: -1\n", x);
            pass = false;
        }

        x = -10;
        y = 4;

        /*
		x >>= (int)( x << (int)y);
		if (x != -10)
		{
			Console.WriteLine("\nInitial parameters: x is -10 and y is 4");
			Console.WriteLine("x >>= (int)( x << (int)y)   failed.\nx: {0}, \texpected: -10\n", x);
			pass = false;
		}
		*/

        x = -10;
        y = 4;

        x >>= (int)(x >> (int)y);
        if (x != -1)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4");
            Console.WriteLine("x >>= (int)( x >> (int)y)   failed.\nx: {0}, \texpected: -1\n", x);
            pass = false;
        }

        x = -10;
        y = 4;

        x >>= (int)(x & y);
        if (x != -1)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4");
            Console.WriteLine("x >>= (int)( x & y)   failed.\nx: {0}, \texpected: -1\n", x);
            pass = false;
        }

        x = -10;
        y = 4;

        x >>= (int)(x ^ y);
        if (x != -1)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4");
            Console.WriteLine("x >>= (int)( x ^ y)   failed.\nx: {0}, \texpected: -1\n", x);
            pass = false;
        }

        x = -10;
        y = 4;

        x >>= (int)(x | y);
        if (x != -1)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4");
            Console.WriteLine("x >>= (int)( x | y)   failed.\nx: {0}, \texpected: -1\n", x);
            pass = false;
        }

        x = -10;
        y = 4;

        x &= x + y;
        if (x != -14)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4");
            Console.WriteLine("x &= x + y   failed.\nx: {0}, \texpected: -14\n", x);
            pass = false;
        }

        x = -10;
        y = 4;

        x &= x - y;
        if (x != -14)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4");
            Console.WriteLine("x &= x - y   failed.\nx: {0}, \texpected: -14\n", x);
            pass = false;
        }

        x = -10;
        y = 4;

        x &= x * y;
        if (x != -48)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4");
            Console.WriteLine("x &= x * y   failed.\nx: {0}, \texpected: -48\n", x);
            pass = false;
        }

        x = -10;
        y = 4;

        x &= x / y;
        if (x != -10)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4");
            Console.WriteLine("x &= x / y   failed.\nx: {0}, \texpected: -10\n", x);
            pass = false;
        }

        x = -10;
        y = 4;

        x &= x % y;
        if (x != -10)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4");
            Console.WriteLine("x &= x % y   failed.\nx: {0}, \texpected: -10\n", x);
            pass = false;
        }

        x = -10;
        y = 4;

        x &= x << (int)y;
        if (x != -160)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4");
            Console.WriteLine("x &= x << (int)y   failed\nx: {0}, \texpected: -160\n", x);
            pass = false;
        }

        x = -10;
        y = 4;

        x &= x >> (int)y;
        if (x != -10)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4");
            Console.WriteLine("x &= x >> (int)y   failed\nx: {0}, \texpected: -10\n", x);
            pass = false;
        }

        x = -10;
        y = 4;

        x &= x & y;
        if (x != 4)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4");
            Console.WriteLine("x &= x & y   failed.\nx: {0}, \texpected: 4\n", x);
            pass = false;
        }

        x = -10;
        y = 4;

        x &= x ^ y;
        if (x != -14)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4");
            Console.WriteLine("x &= x ^ y   failed.\nx: {0}, \texpected: -14\n", x);
            pass = false;
        }

        x = -10;
        y = 4;

        x &= x | y;
        if (x != -10)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4");
            Console.WriteLine("x &= x | y   failed.\nx: {0}, \texpected: -10\n", x);
            pass = false;
        }

        x = -10;
        y = 4;

        x ^= x + y;
        if (x != 12)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4");
            Console.WriteLine("x ^= x + y   failed.\nx: {0}, \texpected: 12\n", x);
            pass = false;
        }

        x = -10;
        y = 4;

        x ^= x - y;
        if (x != 4)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4");
            Console.WriteLine("x ^= x - y   failed.\nx: {0}, \texpected: 4\n", x);
            pass = false;
        }

        x = -10;
        y = 4;

        x ^= x * y;
        if (x != 46)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4");
            Console.WriteLine("x ^= x * y   failed.\nx: {0}, \texpected: 46\n", x);
            pass = false;
        }

        x = -10;
        y = 4;

        x ^= x / y;
        if (x != 8)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4");
            Console.WriteLine("x ^= x / y   failed.\nx: {0}, \texpected: 8\n", x);
            pass = false;
        }

        x = -10;
        y = 4;

        x ^= x % y;
        if (x != 8)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4");
            Console.WriteLine("x ^= x % y   failed.\nx: {0}, \texpected: 8\n", x);
            pass = false;
        }

        x = -10;
        y = 4;

        x ^= x << (int)y;
        if (x != 150)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4");
            Console.WriteLine("x ^= x << (int)y   failed\nx: {0}, \texpected: 150\n", x);
            pass = false;
        }

        x = -10;
        y = 4;

        x ^= x >> (int)y;
        if (x != 9)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4");
            Console.WriteLine("x ^= x >> (int)y   failed\nx: {0}, \texpected: 9\n", x);
            pass = false;
        }

        x = -10;
        y = 4;

        x ^= x & y;
        if (x != -14)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4");
            Console.WriteLine("x ^= x & y   failed.\nx: {0}, \texpected: -14\n", x);
            pass = false;
        }

        x = -10;
        y = 4;

        x ^= x ^ y;
        if (x != 4)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4");
            Console.WriteLine("x ^= x ^ y   failed.\nx: {0}, \texpected: 4\n", x);
            pass = false;
        }

        x = -10;
        y = 4;

        x ^= x | y;
        if (x != 0)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4");
            Console.WriteLine("x ^= x | y   failed.\nx: {0}, \texpected: 0\n", x);
            pass = false;
        }

        x = -10;
        y = 4;

        x |= x + y;
        if (x != -2)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4");
            Console.WriteLine("x |= x + y   failed.\nx: {0}, \texpected: -2\n", x);
            pass = false;
        }

        x = -10;
        y = 4;

        x |= x - y;
        if (x != -10)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4");
            Console.WriteLine("x |= x - y   failed.\nx: {0}, \texpected: -10\n", x);
            pass = false;
        }

        x = -10;
        y = 4;

        x |= x * y;
        if (x != -2)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4");
            Console.WriteLine("x |= x * y   failed.\nx: {0}, \texpected: -2\n", x);
            pass = false;
        }

        x = -10;
        y = 4;

        x |= x / y;
        if (x != -2)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4");
            Console.WriteLine("x |= x / y   failed.\nx: {0}, \texpected: -2\n", x);
            pass = false;
        }

        x = -10;
        y = 4;

        x |= x % y;
        if (x != -2)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4");
            Console.WriteLine("x |= x % y   failed.\nx: {0}, \texpected: -2\n", x);
            pass = false;
        }

        x = -10;
        y = 4;

        x |= x << (int)y;
        if (x != -10)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4");
            Console.WriteLine("x |= x << (int)y   failed\nx: {0}, \texpected: -10\n", x);
            pass = false;
        }

        x = -10;
        y = 4;

        x |= x >> (int)y;
        if (x != -1)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4");
            Console.WriteLine("x |= x >> (int)y   failed\nx: {0}, \texpected: -1\n", x);
            pass = false;
        }

        x = -10;
        y = 4;

        x |= x & y;
        if (x != -10)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4");
            Console.WriteLine("x |= x & y   failed.\nx: {0}, \texpected: -10\n", x);
            pass = false;
        }

        x = -10;
        y = 4;

        x |= x ^ y;
        if (x != -10)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4");
            Console.WriteLine("x |= x ^ y   failed.\nx: {0}, \texpected: -10\n", x);
            pass = false;
        }

        x = -10;
        y = 4;

        x |= x | y;
        if (x != -10)
        {
            Console.WriteLine("\nInitial parameters: x is -10 and y is 4");
            Console.WriteLine("x |= x | y   failed.\nx: {0}, \texpected: -10\n", x);
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
