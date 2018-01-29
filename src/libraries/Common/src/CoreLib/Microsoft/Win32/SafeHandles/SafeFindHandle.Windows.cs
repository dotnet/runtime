// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Security;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Microsoft.Win32;

namespace Microsoft.Win32.SafeHandles
{
    internal sealed class SafeFindHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        internal SafeFindHandle() : base(true) { }

        override protected bool ReleaseHandle()
        {
            return Interop.Kernel32.FindClose(handle);
        }
    }
}
