// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using Gdip = System.Drawing.SafeNativeMethods.Gdip;

namespace System.Drawing.Drawing2D
{
    public sealed partial class AdjustableArrowCap : CustomLineCap
    {
        internal override object CoreClone()
        {
            IntPtr clonedCap;
            int status = Gdip.GdipCloneCustomLineCap(new HandleRef(this, nativeCap), out clonedCap);
            Gdip.CheckStatus(status);

            return new AdjustableArrowCap(clonedCap);
        }
    }
}
