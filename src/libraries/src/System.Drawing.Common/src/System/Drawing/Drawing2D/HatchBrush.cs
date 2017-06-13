// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing.Drawing2D
{
    using System.Runtime.InteropServices;
    using System.Diagnostics;

    /**
     * Represent a HatchBrush brush object
     */
    /// <include file='doc\HatchBrush.uex' path='docs/doc[@for="HatchBrush"]/*' />
    /// <devdoc>
    ///    Defines a rectangular brush with a hatch
    ///    style, a foreground color, and a background color.
    /// </devdoc>
    public sealed class HatchBrush : Brush
    {
        /**
         * Create a new hatch brush object
         */
        /// <include file='doc\HatchBrush.uex' path='docs/doc[@for="HatchBrush.HatchBrush"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.Drawing.Drawing2D.HatchBrush'/> class with the specified <see cref='System.Drawing.Drawing2D.HatchStyle'/> and foreground color.
        ///    </para>
        /// </devdoc>
        public HatchBrush(HatchStyle hatchstyle, Color foreColor) :
            this(hatchstyle, foreColor, Color.FromArgb(unchecked((int)0xff000000)))
        {
        }

        /// <include file='doc\HatchBrush.uex' path='docs/doc[@for="HatchBrush.HatchBrush1"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.Drawing.Drawing2D.HatchBrush'/> class with the specified <see cref='System.Drawing.Drawing2D.HatchStyle'/>,
        ///       foreground color, and background color.
        ///    </para>
        /// </devdoc>
        public HatchBrush(HatchStyle hatchstyle, Color foreColor, Color backColor)
        {
            IntPtr brush = IntPtr.Zero;
            int status = SafeNativeMethods.Gdip.GdipCreateHatchBrush(unchecked((int)hatchstyle), foreColor.ToArgb(), backColor.ToArgb(), out brush);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            SetNativeBrushInternal(brush);
        }

        /// <devdoc>
        ///     Constructor to initialize this object from a GDI+ native reference.
        /// </devdoc>
        internal HatchBrush(IntPtr nativeBrush)
        {
            Debug.Assert(nativeBrush != IntPtr.Zero, "Initializing native brush with null.");
            SetNativeBrushInternal(nativeBrush);
        }

        /// <include file='doc\HatchBrush.uex' path='docs/doc[@for="HatchBrush.Clone"]/*' />
        /// <devdoc>
        ///    Creates an exact copy of this <see cref='System.Drawing.Drawing2D.HatchBrush'/>.
        /// </devdoc>
        public override object Clone()
        {
            IntPtr cloneBrush = IntPtr.Zero;

            int status = SafeNativeMethods.Gdip.GdipCloneBrush(new HandleRef(this, NativeBrush), out cloneBrush);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            return new HatchBrush(cloneBrush);
        }

        /**
         * Get hatch brush object attributes
         */
        /// <include file='doc\HatchBrush.uex' path='docs/doc[@for="HatchBrush.HatchStyle"]/*' />
        /// <devdoc>
        ///    Gets the hatch style of this <see cref='System.Drawing.Drawing2D.HatchBrush'/>.
        /// </devdoc>
        public HatchStyle HatchStyle
        {
            get
            {
                int hatchStyle = 0;

                int status = SafeNativeMethods.Gdip.GdipGetHatchStyle(new HandleRef(this, NativeBrush), out hatchStyle);

                if (status != SafeNativeMethods.Gdip.Ok)
                    throw SafeNativeMethods.Gdip.StatusException(status);

                return (HatchStyle)hatchStyle;
            }
        }

        /// <include file='doc\HatchBrush.uex' path='docs/doc[@for="HatchBrush.ForegroundColor"]/*' />
        /// <devdoc>
        ///    Gets the color of hatch lines drawn by this
        /// <see cref='System.Drawing.Drawing2D.HatchBrush'/>.
        /// </devdoc>
        public Color ForegroundColor
        {
            get
            {
                int forecol;

                int status = SafeNativeMethods.Gdip.GdipGetHatchForegroundColor(new HandleRef(this, NativeBrush), out forecol);

                if (status != SafeNativeMethods.Gdip.Ok)
                    throw SafeNativeMethods.Gdip.StatusException(status);

                return Color.FromArgb(forecol);
            }
        }

        /// <include file='doc\HatchBrush.uex' path='docs/doc[@for="HatchBrush.BackgroundColor"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Gets the color of spaces between the hatch
        ///       lines drawn by this <see cref='System.Drawing.Drawing2D.HatchBrush'/>.
        ///    </para>
        /// </devdoc>
        public Color BackgroundColor
        {
            get
            {
                int backcol;

                int status = SafeNativeMethods.Gdip.GdipGetHatchBackgroundColor(new HandleRef(this, NativeBrush), out backcol);

                if (status != SafeNativeMethods.Gdip.Ok)
                    throw SafeNativeMethods.Gdip.StatusException(status);

                return Color.FromArgb(backcol);
            }
        }
    }
}
