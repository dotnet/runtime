// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Threading;

namespace DelegateTest
{
    class DelegateCommon
    {
        public static string TestMethod(int input)
        {
            return input.ToString();
        }
    }

    class BeginInvokeEndInvokeTest
    {
        public delegate string AsyncMethodCaller(int input);
        static int Main(string[] args)
        {
            IAsyncResult result = null;
            AsyncMethodCaller caller = new AsyncMethodCaller(DelegateCommon.TestMethod);

            try
            {
                result = caller.BeginInvoke(123, null, null);
            }
            catch (PlatformNotSupportedException)
            {
                // Expected
            }
            catch (Exception ex)
            {
                Console.WriteLine("BeginInvoke resulted in unexpected exception: {0}", ex.ToString());
                Console.WriteLine("FAILED!");
                return -1;
            }

            try
            {
                caller.EndInvoke(result);
            }
            catch (PlatformNotSupportedException)
            {
                // Expected
            }
            catch (Exception ex)
            {
                Console.WriteLine("EndInvoke resulted in unexpected exception: {0}", ex.ToString());
                Console.WriteLine("FAILED!");
                return -1;
            }

            return 100;
        }
    }
}
