// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace System.Drawing
{
    public static partial class SystemFonts
    {
        public static Font GetFontByName(string systemFontName)
        {
            if (nameof(CaptionFont).Equals(systemFontName))
            {
                return CaptionFont;
            }
            else if (nameof(DefaultFont).Equals(systemFontName))
            {
                return DefaultFont;
            }
            else if (nameof(DialogFont).Equals(systemFontName))
            {
                return DialogFont;
            }
            else if (nameof(IconTitleFont).Equals(systemFontName))
            {
                return IconTitleFont;
            }
            else if (nameof(MenuFont).Equals(systemFontName))
            {
                return MenuFont;
            }
            else if (nameof(MessageBoxFont).Equals(systemFontName))
            {
                return MessageBoxFont;
            }
            else if (nameof(SmallCaptionFont).Equals(systemFontName))
            {
                return SmallCaptionFont;
            }
            else if (nameof(StatusFont).Equals(systemFontName))
            {
                return StatusFont;
            }

            return null;
        }
    }
}
