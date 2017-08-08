// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace System.Drawing.Drawing2D
{    
    public partial class CustomLineCap
    {
        internal static CustomLineCap CreateCustomLineCapObject(IntPtr cap)
        {
            int status = SafeNativeMethods.Gdip.GdipGetCustomLineCapType(new HandleRef(null, cap), out CustomLineCapType capType);

            if (status != SafeNativeMethods.Gdip.Ok)
            {
                SafeNativeMethods.Gdip.GdipDeleteCustomLineCap(new HandleRef(null, cap));
                throw SafeNativeMethods.Gdip.StatusException(status);
            }

            switch (capType)
            {
                case CustomLineCapType.Default:
                    return new CustomLineCap(cap);

                case CustomLineCapType.AdjustableArrowCap:
                    return new AdjustableArrowCap(cap);
            }

            SafeNativeMethods.Gdip.GdipDeleteCustomLineCap(new HandleRef(null, cap));
            throw SafeNativeMethods.Gdip.StatusException(SafeNativeMethods.Gdip.NotImplemented);
        }
    }
}