// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net.Http;

namespace System.Net.Http.WinHttpHandlerUnitTests
{
    internal class FakeSafeWinHttpHandle : Interop.WinHttp.SafeWinHttpHandle
    {
        public FakeSafeWinHttpHandle(bool markAsValid)
        {
            if (markAsValid)
            {
                SetHandle(Marshal.AllocHGlobal(1));
            }
            else
            {
                SetHandleAsInvalid();
            }
        }

        protected override bool ReleaseHandle()
        {
            Marshal.FreeHGlobal(this.handle);
            return true;
        }
    }
}
