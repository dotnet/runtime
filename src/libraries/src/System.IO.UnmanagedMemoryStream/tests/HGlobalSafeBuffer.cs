// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
