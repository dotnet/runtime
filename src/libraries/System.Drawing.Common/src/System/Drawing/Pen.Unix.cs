// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using Gdip = System.Drawing.SafeNativeMethods.Gdip;

namespace System.Drawing
{
    public partial class Pen
    {
        // libgdiplus does not implement GdipGetPenCustomEndCap, so we cache the last-known value here.
        // Note that this value is not necessarily in sync with the true native value of this property,
        // as it could have been set outside of the CustomEndCap property on this type.
        private CustomLineCap _cachedEndCap;

        /// <summary>
        /// Gets or sets a custom cap style to use at the beginning of lines drawn with this <see cref='Pen'/>.
        /// </summary>
        public CustomLineCap CustomStartCap
        {
            get
            {
                IntPtr lineCap = IntPtr.Zero;
                int status = Gdip.GdipGetPenCustomStartCap(new HandleRef(this, NativePen), out lineCap);
                Gdip.CheckStatus(status);
                if (lineCap == IntPtr.Zero)
                {
                    throw new ArgumentException(SR.GdiplusInvalidParameter);
                }

                return CustomLineCap.CreateCustomLineCapObject(lineCap);
            }
            set
            {
                if (_immutable)
                {
                    throw new ArgumentException(SR.Format(SR.CantChangeImmutableObjects, nameof(Pen)));
                }

                int status = Gdip.GdipSetPenCustomStartCap(new HandleRef(this, NativePen),
                                                              new HandleRef(value, (value == null) ? IntPtr.Zero : value.nativeCap));
                Gdip.CheckStatus(status);
            }
        }

        /// <summary>
        /// Gets or sets a custom cap style to use at the end of lines drawn with this <see cref='Pen'/>.
        /// </summary>
        public CustomLineCap CustomEndCap
        {
            get
            {
                // If the CustomEndCap has never been set, this accessor should throw.
                if (_cachedEndCap == null)
                {
                    throw new ArgumentException(SR.GdiplusInvalidParameter);
                }

                return _cachedEndCap;
            }
            set
            {
                if (_immutable)
                {
                    throw new ArgumentException(SR.Format(SR.CantChangeImmutableObjects, nameof(Pen)));
                }

                // Windows GDI+ clones the CustomLineCap before storing it in the Pen.
                CustomLineCap clone = value == null ? null : (CustomLineCap)value.Clone();

                int status = Gdip.GdipSetPenCustomEndCap(
                    new HandleRef(this, NativePen),
                    new HandleRef(clone, (clone == null) ? IntPtr.Zero : clone.nativeCap));
                Gdip.CheckStatus(status);
                _cachedEndCap = clone;
            }
        }
    }
}
