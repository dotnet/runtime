// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Drawing.Internal;
using System.Runtime.InteropServices;
using Gdip = System.Drawing.SafeNativeMethods.Gdip;

namespace System.Drawing
{
    public sealed class SolidBrush : Brush, ISystemColorTracker
    {
        // GDI+ doesn't understand system colors, so we need to cache the value here.
        private Color _color = Color.Empty;
        private bool _immutable;

        public SolidBrush(Color color)
        {
            _color = color;

            IntPtr nativeBrush;
            int status = Gdip.GdipCreateSolidFill(_color.ToArgb(), out nativeBrush);
            Gdip.CheckStatus(status);

            SetNativeBrushInternal(nativeBrush);

            if (_color.IsSystemColor)
            {
                SystemColorTracker.Add(this);
            }
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
            IntPtr clonedBrush;
            int status = Gdip.GdipCloneBrush(new HandleRef(this, NativeBrush), out clonedBrush);
            Gdip.CheckStatus(status);

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
                    int status = Gdip.GdipGetSolidFillColor(new HandleRef(this, NativeBrush), out colorARGB);
                    Gdip.CheckStatus(status);

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

                    // NOTE: We never remove brushes from the active list, so if someone is
                    // changing their brush colors a lot, this could be a problem.
                    if (value.IsSystemColor && !oldColor.IsSystemColor)
                    {
                        SystemColorTracker.Add(this);
                    }
                }
            }
        }

        // Sets the color even if the brush is considered immutable.
        private void InternalSetColor(Color value)
        {
            int status = Gdip.GdipSetSolidFillColor(new HandleRef(this, NativeBrush), value.ToArgb());
            Gdip.CheckStatus(status);

            _color = value;
        }

        void ISystemColorTracker.OnSystemColorChanged()
        {
            if (NativeBrush != IntPtr.Zero)
            {
                InternalSetColor(_color);
            }
        }
    }
}
