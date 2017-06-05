// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing
{
    using System.Diagnostics;
    using System.Drawing.Internal;
    using System.Runtime.InteropServices;

    /// <include file='doc\SolidBrush.uex' path='docs/doc[@for="SolidBrush"]/*' />
    /// <devdoc>
    ///    <para>
    ///       Defines a brush made up of a single color. Brushes are
    ///       used to fill graphics shapes such as rectangles, ellipses, pies, polygons, and paths.
    ///    </para>
    /// </devdoc>
    public sealed class SolidBrush : Brush, ISystemColorTracker
    {
        // GDI+ doesn't understand system colors, so we need to cache the value here
        private Color _color = Color.Empty;
        private bool _immutable;

        /**
         * Create a new solid fill brush object
         */
        /// <include file='doc\SolidBrush.uex' path='docs/doc[@for="SolidBrush.SolidBrush"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.Drawing.SolidBrush'/> class of the specified
        ///       color.
        ///    </para>
        /// </devdoc>
        public SolidBrush(Color color)
        {
            _color = color;

            IntPtr brush = IntPtr.Zero;
            int status = SafeNativeMethods.Gdip.GdipCreateSolidFill(_color.ToArgb(), out brush);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            SetNativeBrushInternal(brush);

#if FEATURE_SYSTEM_EVENTS
            if (color.IsSystemColor)
            {
                SystemColorTracker.Add(this);
            }
#endif
        }

        internal SolidBrush(Color color, bool immutable) : this(color)
        {
            _immutable = immutable;
        }

        /// <devdoc>
        ///     Constructor to initialized this object from a GDI+ Brush native pointer.
        /// </devdoc>
        internal SolidBrush(IntPtr nativeBrush)
        {
            Debug.Assert(nativeBrush != IntPtr.Zero, "Initializing native brush with null.");
            SetNativeBrushInternal(nativeBrush);
        }

        /// <include file='doc\SolidBrush.uex' path='docs/doc[@for="SolidBrush.Clone"]/*' />
        /// <devdoc>
        ///    Creates an exact copy of this <see cref='System.Drawing.SolidBrush'/>.
        /// </devdoc>
        public override object Clone()
        {
            IntPtr cloneBrush = IntPtr.Zero;

            int status = SafeNativeMethods.Gdip.GdipCloneBrush(new HandleRef(this, NativeBrush), out cloneBrush);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            // We intentionally lose the "immutable" bit.

            return new SolidBrush(cloneBrush);
        }


        /// <include file='doc\SolidBrush.uex' path='docs/doc[@for="SolidBrush.Dispose"]/*' />
        protected override void Dispose(bool disposing)
        {
            if (!disposing)
            {
                _immutable = false;
            }
            else if (_immutable)
            {
                throw new ArgumentException(SR.Format(SR.CantChangeImmutableObjects, "Brush"));
            }

            base.Dispose(disposing);
        }

        /// <include file='doc\SolidBrush.uex' path='docs/doc[@for="SolidBrush.Color"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The color of this <see cref='System.Drawing.SolidBrush'/>.
        ///    </para>
        /// </devdoc>
        public Color Color
        {
            get
            {
                if (_color == Color.Empty)
                {
                    int colorARGB = 0;
                    int status = SafeNativeMethods.Gdip.GdipGetSolidFillColor(new HandleRef(this, NativeBrush), out colorARGB);

                    if (status != SafeNativeMethods.Gdip.Ok)
                        throw SafeNativeMethods.Gdip.StatusException(status);

                    _color = Color.FromArgb(colorARGB);
                }

                // GDI+ doesn't understand system colors, so we can't use GdipGetSolidFillColor in the general case
                return _color;
            }

            set
            {
                if (_immutable)
                {
                    throw new ArgumentException(SR.Format(SR.CantChangeImmutableObjects, "Brush"));
                }

                if (_color != value)
                {
                    Color oldColor = _color;
                    InternalSetColor(value);

#if FEATURE_SYSTEM_EVENTS
                    // NOTE: We never remove brushes from the active list, so if someone is
                    // changing their brush colors a lot, this could be a problem.
                    if (value.IsSystemColor && !oldColor.IsSystemColor)
                    {
                        SystemColorTracker.Add(this);
                    }
#endif
                }
            }
        }

        // Sets the color even if the brush is considered immutable
        private void InternalSetColor(Color value)
        {
            int status = SafeNativeMethods.Gdip.GdipSetSolidFillColor(new HandleRef(this, NativeBrush), value.ToArgb());

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            _color = value;
        }

        /// <include file='doc\SolidBrush.uex' path='docs/doc[@for="SolidBrush.ISystemColorTracker.OnSystemColorChanged"]/*' />
        /// <internalonly/>
        void ISystemColorTracker.OnSystemColorChanged()
        {
            if (NativeBrush != IntPtr.Zero)
            {
                InternalSetColor(_color);
            }
        }
    }
}

