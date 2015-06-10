// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

/*
set COMPLUS_JITStress=1
*/
namespace Test
{
    using System;

    class AA
    {
        public static void Method1()
        {
            uint L = 0;
#pragma warning disable 1718
            while (L == L)
#pragma warning restore 1718
            {
                L = 1;
                try
                {
                    throw new NullReferenceException();
                }
                catch (InvalidOperationException)
                {
                    return;
                }
            }
        }
        static int Main()
        {
            try
            {
                AA.Method1();
                Console.WriteLine("failed");
                return 1;
            }
            catch (NullReferenceException)
            {
                Console.WriteLine("passed");
                return 100;
            }
        }
    }
}
