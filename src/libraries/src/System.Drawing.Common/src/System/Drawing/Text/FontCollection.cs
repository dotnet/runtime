// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing.Text
{
    using System.Diagnostics;
    using System.Runtime.InteropServices;

    /// <summary>
    /// When inherited, enumerates the FontFamily objects in a collection of fonts.
    /// </summary>
    public abstract class FontCollection : IDisposable
    {
        internal IntPtr nativeFontCollection;


        internal FontCollection()
        {
            nativeFontCollection = IntPtr.Zero;
        }

        /// <summary>
        /// Disposes of this <see cref='System.Drawing.Text.FontCollection'/>
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
        }

        /// <summary>
        /// Gets the array of <see cref='System.Drawing.FontFamily'/> objects associated
        /// with this <see cref='System.Drawing.Text.FontCollection'/>.
        /// </summary>
        public FontFamily[] Families
        {
            get
            {
                int numSought = 0;

                int status = SafeNativeMethods.Gdip.GdipGetFontCollectionFamilyCount(new HandleRef(this, nativeFontCollection), out numSought);

                if (status != SafeNativeMethods.Gdip.Ok)
                    throw SafeNativeMethods.Gdip.StatusException(status);

                IntPtr[] gpfamilies = new IntPtr[numSought];

                int numFound = 0;

                status = SafeNativeMethods.Gdip.GdipGetFontCollectionFamilyList(new HandleRef(this, nativeFontCollection), numSought, gpfamilies,
                                                             out numFound);

                if (status != SafeNativeMethods.Gdip.Ok)
                    throw SafeNativeMethods.Gdip.StatusException(status);


                Debug.Assert(numSought == numFound, "GDI+ can't give a straight answer about how many fonts there are");
                FontFamily[] families = new FontFamily[numFound];
                for (int f = 0; f < numFound; f++)
                {
                    IntPtr native;
                    SafeNativeMethods.Gdip.GdipCloneFontFamily(new HandleRef(null, (IntPtr)gpfamilies[f]), out native);
                    families[f] = new FontFamily(native);
                }

                return families;
            }
        }

        ~FontCollection()
        {
            Dispose(false);
        }
    }
}
