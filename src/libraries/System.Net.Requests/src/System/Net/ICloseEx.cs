// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net
{
    [Flags]
    internal enum CloseExState
    {
        Normal = 0x0,          // just a close
        Abort = 0x1,          // unconditionally release resources
        Silent = 0x2           // do not throw on close if possible
    }

    //
    // This is an advanced closing mechanism required by ConnectStream to work properly.
    //
    internal interface ICloseEx
    {
        void CloseEx(CloseExState closeState);
    }
}
