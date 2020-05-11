// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Gdip = System.Drawing.SafeNativeMethods.Gdip;

namespace System.Drawing.Imaging
{
    /// <summary>
    /// Stores a <see cref="Bitmap"/> in a format that is optimized for display on a particular device.
    /// </summary>
    public sealed partial class CachedBitmap : IDisposable
    {
        internal static readonly bool IsSupported = IsCachedBitmapSupported();
        internal IntPtr nativeCachedBitmap;

        /// <summary>
        /// Initializes a new instance of the <see cref="CachedBitmap"/> class.
        /// </summary>
        /// <param name="bitmap">The bitmap to take the pixel data from.</param>
        /// <param name="graphics">A <see cref="Graphics"/> object, representing the display device to optimize the bitmap for.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="bitmap"/> is <see langword="null" />.
        /// - or -
        /// <paramref name="graphics"/> is <see langword="null" />
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        /// The installed version of libgdiplus is lower than 6.1. This does not apply on Windows.
        /// </exception>
        public CachedBitmap(Bitmap bitmap, Graphics graphics)
        {
            if (bitmap is null)
                throw new ArgumentNullException(nameof(bitmap));

            if (graphics is null)
                throw new ArgumentNullException(nameof(graphics));

            if (!IsSupported)
                throw new PlatformNotSupportedException(SR.CachedBitmapNotSupported);

            int status = Gdip.GdipCreateCachedBitmap(new HandleRef(bitmap, bitmap.nativeImage),
                new HandleRef(graphics, graphics.NativeGraphics),
                out nativeCachedBitmap);

            Gdip.CheckStatus(status);
        }

        /// <summary>
        /// Releases all resources used by this <see cref="CachedBitmap"/>.
        /// </summary>
        public void Dispose()
        {
            if (nativeCachedBitmap != IntPtr.Zero)
            {
                int status = Gdip.GdipDeleteCachedBitmap(new HandleRef(this, nativeCachedBitmap));
                nativeCachedBitmap = IntPtr.Zero;
                Gdip.CheckStatus(status);
            }
        }
    }
}
