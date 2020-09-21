// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;

using Point = System.Numerics.Vector2;

namespace GitHub_21854
{
    public class test
    {
        static int Main(string[] args)
        {
            try {
                var unused = new object[] { Vector<int>.Zero };
            }
            catch (Exception e)
            {
                Console.WriteLine("FAILED with exception: " + e.Message);
                return -1;
            }
            return 100;
        }
    }
}
