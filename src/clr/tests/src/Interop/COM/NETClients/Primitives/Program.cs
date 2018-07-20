﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace NetClient
{
    using System;

    class Program
    {
        static int Main(string[] doNotUse)
        {
            try
            {
                new NumericTests().Run();
                new ArrayTests().Run();
                new StringTests().Run();
                new ErrorTests().Run();
            }
            catch (Exception e)
            {
                Console.WriteLine($"Test Failure: {e}");
                return 101;
            }

            return 100;
        }
    }
}
