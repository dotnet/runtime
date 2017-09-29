// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32;
using System.Runtime.InteropServices;

namespace System
{
    partial struct Guid
    {
        public static Guid NewGuid()
        {
            Guid guid;
            Marshal.ThrowExceptionForHR(Win32Native.CoCreateGuid(out guid), new IntPtr(-1));
            return guid;
        }
    }
}
