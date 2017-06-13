// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing.Text
{
    /// <include file='doc\InstalledFontCollection.uex' path='docs/doc[@for="InstalledFontCollection"]/*' />
    /// <devdoc>
    ///    <para>
    ///       Represents the fonts installed on the
    ///       system.
    ///    </para>
    /// </devdoc>
    public sealed class InstalledFontCollection : FontCollection
    {
        /// <include file='doc\InstalledFontCollection.uex' path='docs/doc[@for="InstalledFontCollection.InstalledFontCollection"]/*' />
        /// <devdoc>
        ///    Initializes a new instance of the <see cref='System.Drawing.Text.InstalledFontCollection'/> class.
        /// </devdoc>
        public InstalledFontCollection()
        {
            nativeFontCollection = IntPtr.Zero;

            int status = SafeNativeMethods.Gdip.GdipNewInstalledFontCollection(out nativeFontCollection);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);
        }
    }
}

