// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Gdip = System.Drawing.SafeNativeMethods.Gdip;

namespace System.Drawing.Drawing2D
{
    public partial class CustomLineCap
    {
        internal static CustomLineCap CreateCustomLineCapObject(IntPtr cap)
        {
            int status = Gdip.GdipGetCustomLineCapType(cap, out CustomLineCapType capType);
            if (status != Gdip.Ok)
            {
                Gdip.GdipDeleteCustomLineCap(cap);
                throw Gdip.StatusException(status);
            }

            switch (capType)
            {
                case CustomLineCapType.Default:
                    return new CustomLineCap(cap);

                case CustomLineCapType.AdjustableArrowCap:
                    return new AdjustableArrowCap(cap);
            }

            Gdip.GdipDeleteCustomLineCap(cap);
            throw Gdip.StatusException(Gdip.NotImplemented);
        }
    }
}
