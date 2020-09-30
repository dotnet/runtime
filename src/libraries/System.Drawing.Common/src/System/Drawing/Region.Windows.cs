// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Drawing.Drawing2D;
using System.Drawing.Internal;
using System.Runtime.InteropServices;

namespace System.Drawing
{
    public partial class Region
    {
        public void ReleaseHrgn(IntPtr regionHandle)
        {
            if (regionHandle == IntPtr.Zero)
            {
                throw new ArgumentNullException(nameof(regionHandle));
            }

            Interop.Gdi32.DeleteObject(new HandleRef(this, regionHandle));
        }
    }
}
