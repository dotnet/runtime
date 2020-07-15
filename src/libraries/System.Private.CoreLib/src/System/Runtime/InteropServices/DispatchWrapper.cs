// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Versioning;

namespace System.Runtime.InteropServices
{
    // Wrapper that is converted to a variant with VT_DISPATCH
    public sealed class DispatchWrapper
    {
        public DispatchWrapper(object? obj)
        {
            if (obj != null)
            {
                // Make sure this object has an IDispatch
                IntPtr pdisp = Marshal.GetIDispatchForObject(obj);

                // If we got here without throwing an exception, the QI for IDispatch succeeded.
                Marshal.Release(pdisp);

                WrappedObject = obj;
            }
        }

        [MinimumOSPlatform("windows7.0")]
        public object? WrappedObject { get; }
    }
}
