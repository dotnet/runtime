// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net.Http;
using System.Threading;

namespace System.Net.Http.WinHttpHandlerUnitTests
{
    internal class FakeSafeWinHttpHandle : Interop.WinHttp.SafeWinHttpHandle
    {
        private static int s_HandlesOpen = 0;
        
        public FakeSafeWinHttpHandle(bool markAsValid)
        {
            if (markAsValid)
            {
                SetHandle(Marshal.AllocHGlobal(1));
                Interlocked.Increment(ref s_HandlesOpen);
            }
            else
            {
                SetHandleAsInvalid();
            }
        }

        public static int HandlesOpen
        {
            get
            {
                return s_HandlesOpen;
            }
        }

        public static void ForceGarbageCollection()
        {
            // Make several passes through the FReachable list.
            for (int i = 0; i < 10; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }
        
        protected override bool ReleaseHandle()
        {
            Interlocked.Decrement(ref s_HandlesOpen);
            
            return base.ReleaseHandle();
        }
    }
}
