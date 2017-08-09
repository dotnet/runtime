// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace System.Drawing.Drawing2D
{
    public sealed partial class AdjustableArrowCap : CustomLineCap
    {
        internal AdjustableArrowCap(IntPtr nativeCap) : base(nativeCap) { }

        public AdjustableArrowCap(float width, float height) : this(width, height, true) { }

        public AdjustableArrowCap(float width, float height, bool isFilled)
        {
            IntPtr nativeCap;
            int status = SafeNativeMethods.Gdip.GdipCreateAdjustableArrowCap(height, width, isFilled, out nativeCap);
            SafeNativeMethods.Gdip.CheckStatus(status);
            SetNativeLineCap(nativeCap);
        }

        public float Height
        {
            get
            {
                int status = SafeNativeMethods.Gdip.GdipGetAdjustableArrowCapHeight(new HandleRef(this, nativeCap), out float height);
                SafeNativeMethods.Gdip.CheckStatus(status);
                return height;
            }
            set
            {
                int status = SafeNativeMethods.Gdip.GdipSetAdjustableArrowCapHeight(new HandleRef(this, nativeCap), value);
                SafeNativeMethods.Gdip.CheckStatus(status);
            }
        }

        public float Width
        {
            get
            {
                int status = SafeNativeMethods.Gdip.GdipGetAdjustableArrowCapWidth(new HandleRef(this, nativeCap), out float width);
                SafeNativeMethods.Gdip.CheckStatus(status);
                return width;
            }
            set
            {
                int status = SafeNativeMethods.Gdip.GdipSetAdjustableArrowCapWidth(new HandleRef(this, nativeCap), value);
                SafeNativeMethods.Gdip.CheckStatus(status);
            }
        }

        public float MiddleInset
        {
            get
            {
                int status = SafeNativeMethods.Gdip.GdipGetAdjustableArrowCapMiddleInset(new HandleRef(this, nativeCap), out float middleInset);
                SafeNativeMethods.Gdip.CheckStatus(status);
                return middleInset;
            }
            set
            {
                int status = SafeNativeMethods.Gdip.GdipSetAdjustableArrowCapMiddleInset(new HandleRef(this, nativeCap), value);
                SafeNativeMethods.Gdip.CheckStatus(status);
            }
        }

        public bool Filled
        {
            get
            {
                int status = SafeNativeMethods.Gdip.GdipGetAdjustableArrowCapFillState(new HandleRef(this, nativeCap), out bool isFilled);
                SafeNativeMethods.Gdip.CheckStatus(status);
                return isFilled;
            }
            set
            {
                int status = SafeNativeMethods.Gdip.GdipSetAdjustableArrowCapFillState(new HandleRef(this, nativeCap), value);
                SafeNativeMethods.Gdip.CheckStatus(status);
            }
        }
    }
}
