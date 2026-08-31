// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

[ComVisible(true)]
[Guid(Server.Contract.Guids.ConsumeNETServerTesting)]
public class ConsumeNETServerTesting : Server.Contract.IConsumeNETServer
{
    private IntPtr _ccw;
    private object _rcwUnwrapped;

    public ConsumeNETServerTesting()
    {
        _ccw = Marshal.GetIUnknownForObject(this);

        // At this point, the CCW has not been marked as COM-activated,
        // so the returned RCW will be unwrapped.
        _rcwUnwrapped = Marshal.GetObjectForIUnknown(_ccw);
    }

    public IntPtr GetCCW()
    {
        return _ccw;
    }

    public object GetRCW()
    {
        return _rcwUnwrapped;
    }

    public void ReleaseResources()
    {
        Marshal.Release(_ccw);
        _ccw = IntPtr.Zero;
        _rcwUnwrapped = null;
    }

    public bool EqualByCCW(object obj)
    {
        IntPtr ccwMaybe = Marshal.GetIUnknownForObject(obj);
        bool areEqual = ccwMaybe == _ccw;
        Marshal.Release(ccwMaybe);

        return areEqual;
    }

    public bool NotEqualByRCW(object obj)
    {
        return _rcwUnwrapped != obj;
    }
}
