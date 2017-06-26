// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing.Text
{
    public sealed class InstalledFontCollection : FontCollection
    {
        public InstalledFontCollection() : base()
        {
            int status = SafeNativeMethods.Gdip.GdipNewInstalledFontCollection(out _nativeFontCollection);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }
    }
}
