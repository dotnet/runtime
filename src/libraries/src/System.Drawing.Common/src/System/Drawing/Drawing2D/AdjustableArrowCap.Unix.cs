// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace System.Drawing.Drawing2D
{
    public sealed partial class AdjustableArrowCap : CustomLineCap
    {
        public override object Clone()
        {
            IntPtr clonedCap;
            int status = SafeNativeMethods.Gdip.GdipCloneCustomLineCap(new HandleRef(this, nativeCap), out clonedCap);
            SafeNativeMethods.Gdip.CheckStatus(status);

            return new AdjustableArrowCap(clonedCap);
        }
    }
}
