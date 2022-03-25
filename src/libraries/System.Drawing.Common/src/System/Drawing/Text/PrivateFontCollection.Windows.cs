// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Drawing.Text
{
    public partial class PrivateFontCollection
    {
        private static void GdiAddFontFile(string filename)
        {
            Interop.Gdi32.AddFontFile(filename);
        }
    }
}
