// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing.Text
{
    partial class PrivateFontCollection
    {
        private void GdiAddFontFile(string filename)
        {
            SafeNativeMethods.AddFontFile(filename);
        }
    }
}