// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Globalization;
using System.IO;
using Gdip = System.Drawing.SafeNativeMethods.Gdip;

namespace System.Drawing.Text
{
    /// <summary>
    /// Encapsulates a collection of <see cref='System.Drawing.Font'/> objects.
    /// </summary>
    public sealed partial class PrivateFontCollection : FontCollection
    {
        /// <summary>
        /// Initializes a new instance of the <see cref='System.Drawing.Text.PrivateFontCollection'/> class.
        /// </summary>
        public PrivateFontCollection() : base()
        {
            int status = Gdip.GdipNewPrivateFontCollection(out _nativeFontCollection);
            Gdip.CheckStatus(status);
        }

        /// <summary>
        /// Cleans up Windows resources for this <see cref='System.Drawing.Text.PrivateFontCollection'/>.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (_nativeFontCollection != IntPtr.Zero)
            {
                try
                {
#if DEBUG
                    int status =
#endif
                    Gdip.GdipDeletePrivateFontCollection(ref _nativeFontCollection);
#if DEBUG
                    Debug.Assert(status == Gdip.Ok, "GDI+ returned an error status: " + status.ToString(CultureInfo.InvariantCulture));
#endif        
                }
                catch (Exception ex) when (!ClientUtils.IsSecurityOrCriticalException(ex))
                {
                }
                finally
                {
                    _nativeFontCollection = IntPtr.Zero;
                }
            }

            base.Dispose(disposing);
        }

        /// <summary>
        /// Adds a font from the specified file to this <see cref='System.Drawing.Text.PrivateFontCollection'/>.
        /// </summary>
        public void AddFontFile(string filename)
        {
            if (_nativeFontCollection == IntPtr.Zero)
            {
                // This is the default behavior on Desktop. The ArgumentException originates from GdipPrivateAddFontFile which would
                // refuse the null pointer.
                throw new ArgumentException();
            }

            if (filename == null)
            {
                // This is the default behavior on Desktop. The name "path" originates from Path.GetFullPath or similar which would refuse
                // a null value.
                throw new ArgumentNullException("path");
            }

            // this ensure the filename is valid (or throw the correct exception)
            string fullPath = Path.GetFullPath(filename);

            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException();
            }

            int status = Gdip.GdipPrivateAddFontFile(new HandleRef(this, _nativeFontCollection), fullPath);
            Gdip.CheckStatus(status);

            // Register private font with GDI as well so pure GDI-based controls (TextBox, Button for instance) can access it.
            // This is a no-op on Unix which has GDI+ (libgdiplus), not GDI; and we don't have System.Windows.Forms
            // on Unix.
            this.GdiAddFontFile(filename);
        }

        /// <summary>
        /// Adds a font contained in system memory to this <see cref='System.Drawing.Text.PrivateFontCollection'/>.
        /// </summary>
        public void AddMemoryFont(IntPtr memory, int length)
        {
            int status = Gdip.GdipPrivateAddMemoryFont(new HandleRef(this, _nativeFontCollection), new HandleRef(null, memory), length);
            Gdip.CheckStatus(status);
        }
    }
}
