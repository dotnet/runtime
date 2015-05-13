// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;
using Xunit;

namespace System.IO.Tests
{
    internal sealed class HGlobalSafeBuffer : SafeBuffer
    {
        internal HGlobalSafeBuffer(int capacity) : base(true)
        {
            SetHandle(Marshal.AllocHGlobal(capacity));
            Initialize((ulong)capacity);
        }

        protected override bool ReleaseHandle()
        {
            Marshal.FreeHGlobal(handle);
            return true;
        }
    }
}
