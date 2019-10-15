// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class Kernel32
    {
        // Declaration for C# representation of Win32 COMMTIMEOUTS
        // structure associated with a file handle to a serial communications resource.
        //
        // Currently the only set fields are ReadTotalTimeoutConstant
        // and WriteTotalTimeoutConstant.
        //
        // https://msdn.microsoft.com/en-us/library/windows/desktop/aa363190.aspx
        internal struct COMMTIMEOUTS
        {
            public int ReadIntervalTimeout;
            public int ReadTotalTimeoutMultiplier;
            public int ReadTotalTimeoutConstant;
            public int WriteTotalTimeoutMultiplier;
            public int WriteTotalTimeoutConstant;
        }
    }
}