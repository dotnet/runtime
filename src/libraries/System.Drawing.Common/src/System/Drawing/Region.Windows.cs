// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Drawing.Drawing2D;
using System.Drawing.Internal;
using System.Runtime.InteropServices;

namespace System.Drawing
{
    partial class Region
    {
        public void ReleaseHrgn(IntPtr regionHandle)
        {
            if (regionHandle == IntPtr.Zero)
            {
                throw new ArgumentNullException(nameof(regionHandle));
            }

            // On Windows HRGN are (old) GDI objects. Deskop .NET does not check the return code of IntDeleteObject
            SafeNativeMethods.IntDeleteObject(new HandleRef(this, regionHandle));
        }
    }
}
