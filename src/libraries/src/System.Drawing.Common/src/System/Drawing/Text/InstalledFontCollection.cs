// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing.Text
{
    /// <summary>
    /// Represents the fonts installed on the system.
    /// </summary>
    public sealed class InstalledFontCollection : FontCollection
    {
        /// <summary>
        /// Initializes a new instance of the <see cref='System.Drawing.Text.InstalledFontCollection'/> class.
        /// </summary>
        public InstalledFontCollection()
        {
            nativeFontCollection = IntPtr.Zero;

            int status = SafeNativeMethods.Gdip.GdipNewInstalledFontCollection(out nativeFontCollection);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);
        }
    }
}

