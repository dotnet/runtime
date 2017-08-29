// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.Drawing
{
    public sealed class SolidBrush : Brush
    {
        // GDI+ doesn't understand system colors, so we need to cache the value here.
        private Color _color = Color.Empty;
        private bool _immutable;

        public SolidBrush(Color color)
        {
            _color = color;

            IntPtr nativeBrush = IntPtr.Zero;
            int status = SafeNativeMethods.Gdip.GdipCreateSolidFill(_color.ToArgb(), out nativeBrush);
            SafeNativeMethods.Gdip.CheckStatus(status);

            SetNativeBrushInternal(nativeBrush);
        }

        internal SolidBrush(Color color, bool immutable) : this(color)
        {
            _immutable = immutable;
        }

        internal SolidBrush(IntPtr nativeBrush)
        {
            Debug.Assert(nativeBrush != IntPtr.Zero, "Initializing native brush with null.");
            SetNativeBrushInternal(nativeBrush);
        }

        public override object Clone()
        {
            IntPtr clonedBrush = IntPtr.Zero;
            int status = SafeNativeMethods.Gdip.GdipCloneBrush(new HandleRef(this, NativeBrush), out clonedBrush);
            SafeNativeMethods.Gdip.CheckStatus(status);

            // Clones of immutable brushes are not immutable.
            return new SolidBrush(clonedBrush);
        }

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

        public Color Color
        {
            get
            {
                if (_color == Color.Empty)
                {
                    int colorARGB;
                    int status = SafeNativeMethods.Gdip.GdipGetSolidFillColor(new HandleRef(this, NativeBrush), out colorARGB);
                    SafeNativeMethods.Gdip.CheckStatus(status);

                    _color = Color.FromArgb(colorARGB);
                }

                // GDI+ doesn't understand system colors, so we can't use GdipGetSolidFillColor in the general case.
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
                }
            }
        }

        // Sets the color even if the brush is considered immutable.
        private void InternalSetColor(Color value)
        {
            int status = SafeNativeMethods.Gdip.GdipSetSolidFillColor(new HandleRef(this, NativeBrush), value.ToArgb());
            SafeNativeMethods.Gdip.CheckStatus(status);

            _color = value;
        }
    }
}

