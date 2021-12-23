// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Gdip = System.Drawing.SafeNativeMethods.Gdip;

namespace Microsoft.Win32.SafeHandles
{
    internal class SafeBrushHandle : SafeGdiPlusHandle
    {
        public SafeBrushHandle(IntPtr preexistingHandle, bool ownsHandle) : base(ownsHandle)
        {
            SetHandle(preexistingHandle);
        }

        public SafeBrushHandle() : base(true)
        {
        }

        protected override int ReleaseHandleImpl() => Gdip.GdipDeleteBrush(handle);
    }
}
