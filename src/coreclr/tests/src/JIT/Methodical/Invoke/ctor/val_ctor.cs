// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace JitTest
{
    internal struct TestStruct
    {
        private long _m_testParam;
        private static long s_m_sum = 0;

        private TestStruct(ulong testParam)
        {
            _m_testParam = (long)testParam;
            s_m_sum += _m_testParam;
            if (s_m_sum < 100)
            {
                //In IL, this will be changed to newobj
                TestStruct ts = new TestStruct(testParam + 1);
            }
        }

        private static int Main()
        {
            try
            {
                //In IL, this will be changed to newobj
                TestStruct test = new TestStruct(0);
                if (s_m_sum != 105)
                {
                    Console.WriteLine("Failed");
                    return 1;
                }
            }
            catch
            {
                Console.WriteLine("Failed w/ exception");
                return 2;
            }
            Console.WriteLine("Passed");
            return 100;
        }
    }
}
