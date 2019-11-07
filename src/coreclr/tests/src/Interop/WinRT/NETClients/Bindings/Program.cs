// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace NetClient
{
    using System;

    class Program
    {
        static int Main(string[] args)
        {
            if (!TestLibrary.Utilities.IsWinRTSupported || !TestLibrary.Utilities.IsWindows10Version1809OrGreater)
            {
                Console.WriteLine("XAML Islands are unsupported on this platform.");
                return 100;
            }

            try
            {
                BindingTests.RunTest();
            }
            catch (System.Exception ex)
            {
                Console.WriteLine(ex);
                return 101;
            }
            return 100;
        }
    }
}
