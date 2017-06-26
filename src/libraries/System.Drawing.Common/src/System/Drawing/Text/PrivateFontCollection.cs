// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing.Text
{
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using System.Globalization;

    /// <summary>
    /// Encapsulates a collection of <see cref='System.Drawing.Font'/> objecs.
    /// </summary>
    public sealed class PrivateFontCollection : FontCollection
    {
        /// <summary>
        /// Initializes a new instance of the <see cref='System.Drawing.Text.PrivateFontCollection'/> class.
        /// </summary>
        public PrivateFontCollection()
        {
            nativeFontCollection = IntPtr.Zero;

            int status = SafeNativeMethods.Gdip.GdipNewPrivateFontCollection(out nativeFontCollection);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);
        }

        /// <summary>
        /// Cleans up Windows resources for this <see cref='System.Drawing.Text.PrivateFontCollection'/>.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (nativeFontCollection != IntPtr.Zero)
            {
                try
                {
#if DEBUG
                    int status =
#endif
                    SafeNativeMethods.Gdip.GdipDeletePrivateFontCollection(out nativeFontCollection);
#if DEBUG
                    Debug.Assert(status == SafeNativeMethods.Gdip.Ok, "GDI+ returned an error status: " + status.ToString(CultureInfo.InvariantCulture));
#endif        
                }
                catch (Exception ex)
                {
                    if (ClientUtils.IsSecurityOrCriticalException(ex))
                    {
                        throw;
                    }

                    Debug.Fail("Exception thrown during Dispose: " + ex.ToString());
                }
                finally
                {
                    nativeFontCollection = IntPtr.Zero;
                }
            }

            base.Dispose(disposing);
        }

        /// <summary>
        /// Adds a font from the specified file to this <see cref='System.Drawing.Text.PrivateFontCollection'/>.
        /// </summary>
        public void AddFontFile(string filename)
        {
            int status = SafeNativeMethods.Gdip.GdipPrivateAddFontFile(new HandleRef(this, nativeFontCollection), filename);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            // Register private font with GDI as well so pure GDI-based controls (TextBox, Button for instance) can access it.
            SafeNativeMethods.AddFontFile(filename);
        }

        /// <summary>
        /// Adds a font contained in system memory to this <see cref='System.Drawing.Text.PrivateFontCollection'/>.
        /// </summary>
        public void AddMemoryFont(IntPtr memory, int length)
        {
            int status = SafeNativeMethods.Gdip.GdipPrivateAddMemoryFont(new HandleRef(this, nativeFontCollection), new HandleRef(null, memory), length);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);
        }
    }
}

