// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Gdip = System.Drawing.SafeNativeMethods.Gdip;

namespace System.Drawing.Imaging
{
    public sealed partial class CachedBitmap
    {
        internal static bool IsCachedBitmapSupported()
        {
            // CachedBitmap is only supported on libgdiplus 6.1 and above.
            // The function to check for the version is only present on libgdiplus 6.0 and above.
            if (Gdip.GetLibgdiplusVersion is null)
               return false;

            var version = new Version(Gdip.GetLibgdiplusVersion());
            return version.Major > 6 || version.Major == 6 && version.Minor >= 1;
        }
    }
}
