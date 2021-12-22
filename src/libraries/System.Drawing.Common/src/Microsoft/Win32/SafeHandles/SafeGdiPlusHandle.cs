// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using Gdip = System.Drawing.SafeNativeMethods.Gdip;

namespace Microsoft.Win32.SafeHandles
{
    internal abstract class SafeGdiPlusHandle : SafeHandle
    {
        public SafeGdiPlusHandle(bool ownsHandle) : base(IntPtr.Zero, ownsHandle)
        {
        }

        public override bool IsInvalid => handle == IntPtr.Zero;

        protected abstract int ReleaseHandleImpl();

        protected override bool ReleaseHandle()
        {
            int releaseResult = Gdip.Ok;
            try
            {
                if (!Gdip.Initialized)
                {
                    releaseResult = ReleaseHandleImpl();
                }
                Debug.Assert(releaseResult == Gdip.Ok, $"GDI+ returned an error status: {releaseResult}");
            }
            catch (Exception ex) when (!ClientUtils.IsSecurityOrCriticalException(ex))
            {
                // Catch all non fatal exceptions. This includes exceptions like EntryPointNotFoundException, that is thrown
                // on Windows Nano.
            }
            return releaseResult == Gdip.Ok;
        }
    }
}
