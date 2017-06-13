// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing.Drawing2D
{
    using System.Runtime.InteropServices;

    /// <include file='doc\AdjustableArrowCap.uex' path='docs/doc[@for="AdjustableArrowCap"]/*' />
    /// <devdoc>
    ///    Represents an adjustable arrow-shaped line
    ///    cap.
    /// </devdoc>
    public sealed class AdjustableArrowCap : CustomLineCap
    {
        internal AdjustableArrowCap(IntPtr nativeCap) :
            base(nativeCap)
        { }

        /// <include file='doc\AdjustableArrowCap.uex' path='docs/doc[@for="AdjustableArrowCap.AdjustableArrowCap"]/*' />
        /// <devdoc>
        ///    Initializes a new instance of the <see cref='System.Drawing.Drawing2D.AdjustableArrowCap'/> class with the specified width and
        ///    height.
        /// </devdoc>
        public AdjustableArrowCap(float width,
                                  float height) :
            this(width, height, true)
        { }

        /// <include file='doc\AdjustableArrowCap.uex' path='docs/doc[@for="AdjustableArrowCap.AdjustableArrowCap1"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.Drawing.Drawing2D.AdjustableArrowCap'/> class with the specified width,
        ///       height, and fill property.
        ///    </para>
        /// </devdoc>
        public AdjustableArrowCap(float width,
                                  float height,
                                  bool isFilled)
        {
            IntPtr nativeCap = IntPtr.Zero;

            int status = SafeNativeMethods.Gdip.GdipCreateAdjustableArrowCap(
                                height, width, isFilled, out nativeCap);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            SetNativeLineCap(nativeCap);
        }

        private void _SetHeight(float height)
        {
            int status = SafeNativeMethods.Gdip.GdipSetAdjustableArrowCapHeight(new HandleRef(this, nativeCap), height);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);
        }

        private float _GetHeight()
        {
            float height;
            int status = SafeNativeMethods.Gdip.GdipGetAdjustableArrowCapHeight(new HandleRef(this, nativeCap), out height);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            return height;
        }

        /// <include file='doc\AdjustableArrowCap.uex' path='docs/doc[@for="AdjustableArrowCap.Height"]/*' />
        /// <devdoc>
        ///    Gets or sets the height of the arrow cap.
        /// </devdoc>
        public float Height
        {
            get { return _GetHeight(); }
            set { _SetHeight(value); }
        }

        private void _SetWidth(float width)
        {
            int status = SafeNativeMethods.Gdip.GdipSetAdjustableArrowCapWidth(new HandleRef(this, nativeCap), width);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);
        }

        private float _GetWidth()
        {
            float width;
            int status = SafeNativeMethods.Gdip.GdipGetAdjustableArrowCapWidth(new HandleRef(this, nativeCap), out width);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            return width;
        }

        /// <include file='doc\AdjustableArrowCap.uex' path='docs/doc[@for="AdjustableArrowCap.Width"]/*' />
        /// <devdoc>
        ///    Gets or sets the width of the arrow cap.
        /// </devdoc>
        public float Width
        {
            get { return _GetWidth(); }
            set { _SetWidth(value); }
        }

        private void _SetMiddleInset(float middleInset)
        {
            int status = SafeNativeMethods.Gdip.GdipSetAdjustableArrowCapMiddleInset(new HandleRef(this, nativeCap), middleInset);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);
        }

        private float _GetMiddleInset()
        {
            float middleInset;
            int status = SafeNativeMethods.Gdip.GdipGetAdjustableArrowCapMiddleInset(new HandleRef(this, nativeCap), out middleInset);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            return middleInset;
        }

        /// <include file='doc\AdjustableArrowCap.uex' path='docs/doc[@for="AdjustableArrowCap.MiddleInset"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Gets or set the number of pixels between the outline of the arrow cap and the fill.
        ///    </para>
        /// </devdoc>
        public float MiddleInset
        {
            get { return _GetMiddleInset(); }
            set { _SetMiddleInset(value); }
        }

        private void _SetFillState(bool isFilled)
        {
            int status = SafeNativeMethods.Gdip.GdipSetAdjustableArrowCapFillState(new HandleRef(this, nativeCap), isFilled);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);
        }

        private bool _IsFilled()
        {
            bool isFilled = false;
            int status = SafeNativeMethods.Gdip.GdipGetAdjustableArrowCapFillState(new HandleRef(this, nativeCap), out isFilled);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            return isFilled;
        }

        /// <include file='doc\AdjustableArrowCap.uex' path='docs/doc[@for="AdjustableArrowCap.Filled"]/*' />
        /// <devdoc>
        ///    Gets or sets a value indicating whether the
        ///    arrow cap is filled.
        /// </devdoc>
        public bool Filled
        {
            get { return _IsFilled(); }
            set { _SetFillState(value); }
        }
    }
}
