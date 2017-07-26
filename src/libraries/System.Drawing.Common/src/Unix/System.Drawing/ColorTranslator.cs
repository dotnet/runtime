// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.
//
// System.Drawing.ColorTranslator.cs
//
// Authors:
//    Dennis Hayes (dennish@raytek.com)
//    Ravindra (rkumar@novell.com)
//    Sebastien Pouliot  <sebastien@ximian.com>
//
// Copyright (C) 2001 Ximian, Inc.  http://www.ximian.com
// Copyright (C) 2004,2006-2007 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System.ComponentModel;
using System.Globalization;

namespace System.Drawing
{

    public sealed class ColorTranslator
    {

        private ColorTranslator()
        {
        }

        public static Color FromHtml(string htmlColor)
        {
            if (string.IsNullOrEmpty(htmlColor))
                return Color.Empty;

            switch (htmlColor.ToLowerInvariant())
            {
                case "buttonface":
                case "threedface":
                    return SystemColors.Control;
                case "buttonhighlight":
                case "threedlightshadow":
                    return SystemColors.ControlLightLight;
                case "buttonshadow":
                    return SystemColors.ControlDark;
                case "captiontext":
                    return SystemColors.ActiveCaptionText;
                case "threeddarkshadow":
                    return SystemColors.ControlDarkDark;
                case "threedhighlight":
                    return SystemColors.ControlLight;
                case "background":
                    return SystemColors.Desktop;
                case "buttontext":
                    return SystemColors.ControlText;
                case "infobackground":
                    return SystemColors.Info;
                // special case for Color.LightGray versus html's LightGrey (#340917)
                case "lightgrey":
                    return Color.LightGray;
            }

            if (htmlColor[0] == '#' && htmlColor.Length == 4)
            {
                char r = htmlColor[1], g = htmlColor[2], b = htmlColor[3];
                htmlColor = new string(new char[] { '#', r, r, g, g, b, b });
            }
#if NETCORE
            return ColorConverterCommon.ConvertFromString(htmlColor, CultureInfo.CurrentCulture);
#else
            TypeConverter converter = TypeDescriptor.GetConverter (typeof (Color));
            return (Color) converter.ConvertFromString (htmlColor);
#endif
        }

        internal static Color FromBGR(int bgr)
        {
            Color result = Color.FromArgb(0xFF, (bgr & 0xFF), ((bgr >> 8) & 0xFF), ((bgr >> 16) & 0xFF));
            Color known = KnownColors.FindColorMatch(result);
            return (known.IsEmpty) ? result : known;
        }

        public static Color FromOle(int oleColor)
        {
            // OleColor format is BGR
            return FromBGR(oleColor);
        }

        public static Color FromWin32(int win32Color)
        {
            // Win32Color format is BGR
            return FromBGR(win32Color);
        }

        public static string ToHtml(Color c)
        {
            if (c.IsEmpty)
                return String.Empty;

            if (c.IsSystemColor)
            {
                KnownColor kc = c.ToKnownColor();
                switch (kc)
                {
                    case KnownColor.ActiveBorder:
                    case KnownColor.ActiveCaption:
                    case KnownColor.AppWorkspace:
                    case KnownColor.GrayText:
                    case KnownColor.Highlight:
                    case KnownColor.HighlightText:
                    case KnownColor.InactiveBorder:
                    case KnownColor.InactiveCaption:
                    case KnownColor.InactiveCaptionText:
                    case KnownColor.InfoText:
                    case KnownColor.Menu:
                    case KnownColor.MenuText:
                    case KnownColor.ScrollBar:
                    case KnownColor.Window:
                    case KnownColor.WindowFrame:
                    case KnownColor.WindowText:
                        return KnownColors.GetName(kc).ToLowerInvariant();

                    case KnownColor.ActiveCaptionText:
                        return "captiontext";
                    case KnownColor.Control:
                        return "buttonface";
                    case KnownColor.ControlDark:
                        return "buttonshadow";
                    case KnownColor.ControlDarkDark:
                        return "threeddarkshadow";
                    case KnownColor.ControlLight:
                        return "buttonface";
                    case KnownColor.ControlLightLight:
                        return "buttonhighlight";
                    case KnownColor.ControlText:
                        return "buttontext";
                    case KnownColor.Desktop:
                        return "background";
                    case KnownColor.HotTrack:
                        return "highlight";
                    case KnownColor.Info:
                        return "infobackground";

                    default:
                        return String.Empty;
                }
            }

            if (c.IsNamedColor)
            {
                if (c == Color.LightGray)
                    return "LightGrey";
                else
                    return c.Name;
            }

            return FormatHtml(c.R, c.G, c.B);
        }

        static char GetHexNumber(int b)
        {
            return (char)(b > 9 ? 55 + b : 48 + b);
        }

        static string FormatHtml(int r, int g, int b)
        {
            char[] htmlColor = new char[7];
            htmlColor[0] = '#';
            htmlColor[1] = GetHexNumber((r >> 4) & 15);
            htmlColor[2] = GetHexNumber(r & 15);
            htmlColor[3] = GetHexNumber((g >> 4) & 15);
            htmlColor[4] = GetHexNumber(g & 15);
            htmlColor[5] = GetHexNumber((b >> 4) & 15);
            htmlColor[6] = GetHexNumber(b & 15);

            return new string(htmlColor);
        }

        public static int ToOle(Color c)
        {
            // OleColor format is BGR, same as Win32
            return ((c.B << 16) | (c.G << 8) | c.R);
        }

        public static int ToWin32(Color c)
        {
            // Win32Color format is BGR, Same as OleColor
            return ((c.B << 16) | (c.G << 8) | c.R);
        }
    }
}
