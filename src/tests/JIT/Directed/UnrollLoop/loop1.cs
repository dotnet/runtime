// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using Xunit;
namespace A
{
    public class B
    {
        public static int downBy1ge(int amount)
        {
            int i;
            int sum = 0;
            for (i = 8; i >= 1; i -= 1)
            {
                sum += amount;
            }
            return sum + i;
        }

        public static int downBy2ne(int amount)
        {
            int i;
            int sum = 0;
            for (i = 9; i != 1; i -= 2)
            {
                sum += amount;
            }
            return sum + i;
        }

        public static int upBy1le(int amount)
        {
            int i;
            int sum = 0;
            for (i = 1; i <= 8; i += 1)
            {
                sum += amount;
            }
            return sum + i;
        }

        public static int upBy1lt(int amount)
        {
            int i;
            int sum = 0;
            for (i = 1; i < 8; i += 1)
            {
                sum += amount;
            }
            return sum + i;
        }

        public static int downBy1gt(int amount)
        {
            int i;
            int sum = 0;
            for (i = 9; i > 2; i -= 1)
            {
                sum += amount;
            }
            return sum + i;
        }

        public static int upBy2le(int amount)
        {
            int i;
            int sum = 0;
            for (i = 1; i <= 9; i += 2)
            {
                sum += amount;
            }
            return sum + i;
        }

        public static int downBy2ge(int amount)
        {
            int i;
            int sum = 0;
            for (i = 9; i >= 1; i -= 2)
            {
                sum += amount;
            }
            return sum + i;
        }

        public static int upBy2lt(int amount)
        {
            int i;
            int sum = 0;
            for (i = 1; i < 9; i += 2)
            {
                sum += amount;
            }
            return sum + i;
        }

        public static int downBy2gt(int amount)
        {
            int i;
            int sum = 0;
            for (i = 10; i > 2; i -= 2)
            {
                sum += amount;
            }
            return sum + i;
        }

        public static int upBy1ne(int amount)
        {
            int i;
            int sum = 0;
            for (i = 1; i != 8; i += 1)
            {
                sum += amount;
            }
            return sum + i;
        }

        public static int downBy1ne(int amount)
        {
            int i;
            int sum = 0;
            for (i = 9; i != 2; i -= 1)
            {
                sum += amount;
            }
            return sum + i;
        }

        public static int upBy2ne(int amount)
        {
            int i;
            int sum = 0;
            for (i = 1; i != 9; i += 2)
            {
                sum += amount;
            }
            return sum + i;
        }

        public static int upBy3neWrap(int amount)
        {
            short i;
            int sum = 0;
            for (i = 1; i != 8; i += 3)
            {
                sum += amount;
            }
            return sum + i;
        }

        public static int downBy3neWrap(int amount)
        {
            short i;
            int sum = 0;
            for (i = 8; i != 1; i -= 3)
            {
                sum += amount;
            }
            return sum + i;
        }

        [Fact]
        public static int TestEntryPoint()
        {
            bool failed = false;

            if (upBy1le(10) != 89)
            {
                Console.WriteLine("upBy1le failed");
                failed = true;
            }
            if (downBy1ge(10) != 80)
            {
                Console.WriteLine("downBy1ge failed");
                failed = true;
            }
            if (upBy1lt(10) != 78)
            {
                Console.WriteLine("upBy1lt failed");
                failed = true;
            }
            if (downBy1gt(10) != 72)
            {
                Console.WriteLine("downBy1gt failed");
                failed = true;
            }
            if (upBy2le(10) != 61)
            {
                Console.WriteLine("upBy2le failed");
                failed = true;
            }
            if (downBy2ge(10) != 49)
            {
                Console.WriteLine("downBy2ge failed");
                failed = true;
            }
            if (upBy2lt(10) != 49)
            {
                Console.WriteLine("upBy2lt failed");
                failed = true;
            }
            if (downBy2gt(10) != 42)
            {
                Console.WriteLine("downBy2gt failed");
                failed = true;
            }
            if (upBy1ne(10) != 78)
            {
                Console.WriteLine("upBy1ne failed");
                failed = true;
            }
            if (downBy1ne(10) != 72)
            {
                Console.WriteLine("downBy1ne failed");
                failed = true;
            }
            if (upBy2ne(10) != 49)
            {
                Console.WriteLine("upBy2ne failed");
                failed = true;
            }
            if (downBy2ne(10) != 41)
            {
                Console.WriteLine("downBy2ne failed");
                failed = true;
            }
            if (upBy3neWrap(1) != 43701)
            {
                Console.WriteLine("upBy3neWrap failed");
                failed = true;
            }
            if (downBy3neWrap(1) != 43694)
            {
                Console.WriteLine("downBy3neWrap failed");
                failed = true;
            }
            if (!failed)
            {
                Console.WriteLine("Passed");
                return 100;
            }
            else
            {
                Console.WriteLine("Failed");
                return 1;
            }
        }
    }
}

