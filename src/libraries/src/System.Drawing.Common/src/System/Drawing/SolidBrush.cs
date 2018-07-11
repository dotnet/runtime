// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.InteropServices;
using Gdip = System.Drawing.SafeNativeMethods.Gdip;

namespace System.Drawing
{
#if FEATURE_SYSTEM_EVENTS
    using System.Drawing.Internal;
#endif

    public sealed class SolidBrush : Brush
#if FEATURE_SYSTEM_EVENTS
        , ISystemColorTracker
#endif
    {
        // GDI+ doesn't understand system colors, so we need to cache the value here.
        private Color _color = Color.Empty;
        private bool _immutable;

        public SolidBrush(Color color)
        {
            _color = color;

            IntPtr nativeBrush = IntPtr.Zero;
            int status = Gdip.GdipCreateSolidFill(_color.ToArgb(), out nativeBrush);
            Gdip.CheckStatus(status);

            SetNativeBrushInternal(nativeBrush);

#if FEATURE_SYSTEM_EVENTS
            if (ColorUtil.IsSystemColor(_color))
            {
                SystemColorTracker.Add(this);
            }
#endif
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

#if FEATURE_SYSTEM_EVENTS
                    // NOTE: We never remove brushes from the active list, so if someone is
                    // changing their brush colors a lot, this could be a problem.
                    if (ColorUtil.IsSystemColor(value) && !ColorUtil.IsSystemColor(oldColor))
                    {
                        SystemColorTracker.Add(this);
                    }
#endif
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

#if FEATURE_SYSTEM_EVENTS
        void ISystemColorTracker.OnSystemColorChanged()
        {
            if (NativeBrush != IntPtr.Zero)
            {
                InternalSetColor(_color);
            }
        }
#endif
    }
}

