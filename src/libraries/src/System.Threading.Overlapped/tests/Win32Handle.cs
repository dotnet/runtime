// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
