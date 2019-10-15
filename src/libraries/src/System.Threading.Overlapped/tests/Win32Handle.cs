// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

public class Win32Handle : SafeHandle
{
    private Win32Handle()
        : base(IntPtr.Zero, true)
    {
    }

    public Win32Handle(IntPtr handle)
        : base(handle, false)
    {
    }

    public override bool IsInvalid
    {
        get { return handle == IntPtr.Zero || handle == new IntPtr(-1); }
    }

    protected override bool ReleaseHandle()
    {
        return DllImport.CloseHandle(handle);
    }

    public void CloseWithoutDisposing()
    {
        DllImport.CloseHandle(handle);
    }


}
