// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Globalization;

namespace System.Drawing
{
    /// <summary>
    /// Translates colors to and from GDI+ <see cref='Color'/> objects.
    /// </summary>
    public static class ColorTranslator
    {
        // COLORREF is 0x00BBGGRR
        internal const int COLORREF_RedShift = 0;
        internal const int COLORREF_GreenShift = 8;
        internal const int COLORREF_BlueShift = 16;

        private const int OleSystemColorFlag = unchecked((int)0x80000000);

        private static Dictionary<string, Color>? s_htmlSysColorTable;

        internal static uint COLORREFToARGB(uint value)
            => ((value >> COLORREF_RedShift) & 0xFF) << Color.ARGBRedShift
                | ((value >> COLORREF_GreenShift) & 0xFF) << Color.ARGBGreenShift
                | ((value >> COLORREF_BlueShift) & 0xFF) << Color.ARGBBlueShift
                | Color.ARGBAlphaMask; // COLORREF's are always fully opaque

        /// <summary>
        /// Translates the specified <see cref='Color'/> to a Win32 color.
        /// </summary>
        public static int ToWin32(Color c)
        {
            return c.R << COLORREF_RedShift | c.G << COLORREF_GreenShift | c.B << COLORREF_BlueShift;
        }

        /// <summary>
        /// Translates the specified <see cref='Color'/> to an Ole color.
        /// </summary>
        public static int ToOle(Color c)
        {
            // IMPORTANT: This signature is invoked directly by the runtime marshaler and cannot change without
            // also updating the runtime.

            // This method converts Color to an OLE_COLOR.
            // https://docs.microsoft.com/openspecs/office_file_formats/ms-oforms/4b8f4be0-3fff-4e42-9fc1-b9fd00251e8e

            if (c.IsKnownColor && c.IsSystemColor)
            {
                // Unfortunately KnownColor didn't keep the same ordering as the various GetSysColor()
                // COLOR_ * values, otherwise this could be greatly simplified.

                switch (c.ToKnownColor())
                {
                    case KnownColor.ActiveBorder:
                        return unchecked((int)0x8000000A);
                    case KnownColor.ActiveCaption:
                        return unchecked((int)0x80000002);
                    case KnownColor.ActiveCaptionText:
                        return unchecked((int)0x80000009);
                    case KnownColor.AppWorkspace:
                        return unchecked((int)0x8000000C);
                    case KnownColor.ButtonFace:
                        return unchecked((int)0x8000000F);
                    case KnownColor.ButtonHighlight:
                        return unchecked((int)0x80000014);
                    case KnownColor.ButtonShadow:
                        return unchecked((int)0x80000010);
                    case KnownColor.Control:
                        return unchecked((int)0x8000000F);
                    case KnownColor.ControlDark:
                        return unchecked((int)0x80000010);
                    case KnownColor.ControlDarkDark:
                        return unchecked((int)0x80000015);
                    case KnownColor.ControlLight:
                        return unchecked((int)0x80000016);
                    case KnownColor.ControlLightLight:
                        return unchecked((int)0x80000014);
                    case KnownColor.ControlText:
                        return unchecked((int)0x80000012);
                    case KnownColor.Desktop:
                        return unchecked((int)0x80000001);
                    case KnownColor.GradientActiveCaption:
                        return unchecked((int)0x8000001B);
                    case KnownColor.GradientInactiveCaption:
                        return unchecked((int)0x8000001C);
                    case KnownColor.GrayText:
                        return unchecked((int)0x80000011);
                    case KnownColor.Highlight:
                        return unchecked((int)0x8000000D);
                    case KnownColor.HighlightText:
                        return unchecked((int)0x8000000E);
                    case KnownColor.HotTrack:
                        return unchecked((int)0x8000001A);
                    case KnownColor.InactiveBorder:
                        return unchecked((int)0x8000000B);
                    case KnownColor.InactiveCaption:
                        return unchecked((int)0x80000003);
                    case KnownColor.InactiveCaptionText:
                        return unchecked((int)0x80000013);
                    case KnownColor.Info:
                        return unchecked((int)0x80000018);
                    case KnownColor.InfoText:
                        return unchecked((int)0x80000017);
                    case KnownColor.Menu:
                        return unchecked((int)0x80000004);
                    case KnownColor.MenuBar:
                        return unchecked((int)0x8000001E);
                    case KnownColor.MenuHighlight:
                        return unchecked((int)0x8000001D);
                    case KnownColor.MenuText:
                        return unchecked((int)0x80000007);
                    case KnownColor.ScrollBar:
                        return unchecked((int)0x80000000);
                    case KnownColor.Window:
                        return unchecked((int)0x80000005);
                    case KnownColor.WindowFrame:
                        return unchecked((int)0x80000006);
                    case KnownColor.WindowText:
                        return unchecked((int)0x80000008);
                }
            }

            return ToWin32(c);
        }

        /// <summary>
        /// Translates an Ole color value to a GDI+ <see cref='Color'/>.
        /// </summary>
        public static Color FromOle(int oleColor)
        {
            // IMPORTANT: This signature is invoked directly by the runtime marshaler and cannot change without
            // also updating the runtime.

            if ((oleColor & OleSystemColorFlag) != 0)
            {
                switch (oleColor)
                {
                    case unchecked((int)0x8000000A):
                        return Color.FromKnownColor(KnownColor.ActiveBorder);
                    case unchecked((int)0x80000002):
                        return Color.FromKnownColor(KnownColor.ActiveCaption);
                    case unchecked((int)0x80000009):
                        return Color.FromKnownColor(KnownColor.ActiveCaptionText);
                    case unchecked((int)0x8000000C):
                        return Color.FromKnownColor(KnownColor.AppWorkspace);
                    case unchecked((int)0x8000000F):
                        return Color.FromKnownColor(KnownColor.Control);
                    case unchecked((int)0x80000010):
                        return Color.FromKnownColor(KnownColor.ControlDark);
                    case unchecked((int)0x80000015):
                        return Color.FromKnownColor(KnownColor.ControlDarkDark);
                    case unchecked((int)0x80000016):
                        return Color.FromKnownColor(KnownColor.ControlLight);
                    case unchecked((int)0x80000014):
                        return Color.FromKnownColor(KnownColor.ControlLightLight);
                    case unchecked((int)0x80000012):
                        return Color.FromKnownColor(KnownColor.ControlText);
                    case unchecked((int)0x80000001):
                        return Color.FromKnownColor(KnownColor.Desktop);
                    case unchecked((int)0x8000001B):
                        return Color.FromKnownColor(KnownColor.GradientActiveCaption);
                    case unchecked((int)0x8000001C):
                        return Color.FromKnownColor(KnownColor.GradientInactiveCaption);
                    case unchecked((int)0x80000011):
                        return Color.FromKnownColor(KnownColor.GrayText);
                    case unchecked((int)0x8000000D):
                        return Color.FromKnownColor(KnownColor.Highlight);
                    case unchecked((int)0x8000000E):
                        return Color.FromKnownColor(KnownColor.HighlightText);
                    case unchecked((int)0x8000001A):
                        return Color.FromKnownColor(KnownColor.HotTrack);
                    case unchecked((int)0x8000000B):
                        return Color.FromKnownColor(KnownColor.InactiveBorder);
                    case unchecked((int)0x80000003):
                        return Color.FromKnownColor(KnownColor.InactiveCaption);
                    case unchecked((int)0x80000013):
                        return Color.FromKnownColor(KnownColor.InactiveCaptionText);
                    case unchecked((int)0x80000018):
                        return Color.FromKnownColor(KnownColor.Info);
                    case unchecked((int)0x80000017):
                        return Color.FromKnownColor(KnownColor.InfoText);
                    case unchecked((int)0x80000004):
                        return Color.FromKnownColor(KnownColor.Menu);
                    case unchecked((int)0x8000001E):
                        return Color.FromKnownColor(KnownColor.MenuBar);
                    case unchecked((int)0x8000001D):
                        return Color.FromKnownColor(KnownColor.MenuHighlight);
                    case unchecked((int)0x80000007):
                        return Color.FromKnownColor(KnownColor.MenuText);
                    case unchecked((int)0x80000000):
                        return Color.FromKnownColor(KnownColor.ScrollBar);
                    case unchecked((int)0x80000005):
                        return Color.FromKnownColor(KnownColor.Window);
                    case unchecked((int)0x80000006):
                        return Color.FromKnownColor(KnownColor.WindowFrame);
                    case unchecked((int)0x80000008):
                        return Color.FromKnownColor(KnownColor.WindowText);
                }
            }

            // When we don't find a system color, we treat the color as a COLORREF
            return KnownColorTable.ArgbToKnownColor(COLORREFToARGB((uint)oleColor));
        }

        /// <summary>
        /// Translates an Win32 color value to a GDI+ <see cref='Color'/>.
        /// </summary>
        public static Color FromWin32(int win32Color)
        {
            return FromOle(win32Color);
        }

        /// <summary>
        /// Translates an Html color representation to a GDI+ <see cref='Color'/>.
        /// </summary>
        public static Color FromHtml(string htmlColor)
        {
            Color c = Color.Empty;

            // empty color
            if ((htmlColor == null) || (htmlColor.Length == 0))
                return c;

            // #RRGGBB or #RGB
            if ((htmlColor[0] == '#') &&
                ((htmlColor.Length == 7) || (htmlColor.Length == 4)))
            {
                if (htmlColor.Length == 7)
                {
                    c = Color.FromArgb(Convert.ToInt32(htmlColor.Substring(1, 2), 16),
                                       Convert.ToInt32(htmlColor.Substring(3, 2), 16),
                                       Convert.ToInt32(htmlColor.Substring(5, 2), 16));
                }
                else
                {
                    string r = char.ToString(htmlColor[1]);
                    string g = char.ToString(htmlColor[2]);
                    string b = char.ToString(htmlColor[3]);

                    c = Color.FromArgb(Convert.ToInt32(r + r, 16),
                                       Convert.ToInt32(g + g, 16),
                                       Convert.ToInt32(b + b, 16));
                }
            }

            // special case. Html requires LightGrey, but .NET uses LightGray
            if (c.IsEmpty && string.Equals(htmlColor, "LightGrey", StringComparison.OrdinalIgnoreCase))
            {
                c = Color.LightGray;
            }

            // System color
            if (c.IsEmpty)
            {
                if (s_htmlSysColorTable == null)
                {
                    InitializeHtmlSysColorTable();
                }

                s_htmlSysColorTable!.TryGetValue(htmlColor.ToLowerInvariant(), out c);
            }

            // resort to type converter which will handle named colors
            if (c.IsEmpty)
            {
                try
                {
                    c = ColorConverterCommon.ConvertFromString(htmlColor, CultureInfo.CurrentCulture);
                }
                catch (Exception ex)
                {
                    throw new ArgumentException(ex.Message, nameof(htmlColor), ex);
                }
            }

            return c;
        }

        /// <summary>
        /// Translates the specified <see cref='Color'/> to an Html string color representation.
        /// </summary>
        public static string ToHtml(Color c)
        {
            string colorString = string.Empty;

            if (c.IsEmpty)
                return colorString;

            if (c.IsSystemColor)
            {
                switch (c.ToKnownColor())
                {
                    case KnownColor.ActiveBorder:
                        colorString = "activeborder";
                        break;
                    case KnownColor.GradientActiveCaption:
                    case KnownColor.ActiveCaption:
                        colorString = "activecaption";
                        break;
                    case KnownColor.AppWorkspace:
                        colorString = "appworkspace";
                        break;
                    case KnownColor.Desktop:
                        colorString = "background";
                        break;
                    case KnownColor.Control:
                    case KnownColor.ControlLight:
                        colorString = "buttonface";
                        break;
                    case KnownColor.ControlDark:
                        colorString = "buttonshadow";
                        break;
                    case KnownColor.ControlText:
                        colorString = "buttontext";
                        break;
                    case KnownColor.ActiveCaptionText:
                        colorString = "captiontext";
                        break;
                    case KnownColor.GrayText:
                        colorString = "graytext";
                        break;
                    case KnownColor.HotTrack:
                    case KnownColor.Highlight:
                        colorString = "highlight";
                        break;
                    case KnownColor.MenuHighlight:
                    case KnownColor.HighlightText:
                        colorString = "highlighttext";
                        break;
                    case KnownColor.InactiveBorder:
                        colorString = "inactiveborder";
                        break;
                    case KnownColor.GradientInactiveCaption:
                    case KnownColor.InactiveCaption:
                        colorString = "inactivecaption";
                        break;
                    case KnownColor.InactiveCaptionText:
                        colorString = "inactivecaptiontext";
                        break;
                    case KnownColor.Info:
                        colorString = "infobackground";
                        break;
                    case KnownColor.InfoText:
                        colorString = "infotext";
                        break;
                    case KnownColor.MenuBar:
                    case KnownColor.Menu:
                        colorString = "menu";
                        break;
                    case KnownColor.MenuText:
                        colorString = "menutext";
                        break;
                    case KnownColor.ScrollBar:
                        colorString = "scrollbar";
                        break;
                    case KnownColor.ControlDarkDark:
                        colorString = "threeddarkshadow";
                        break;
                    case KnownColor.ControlLightLight:
                        colorString = "buttonhighlight";
                        break;
                    case KnownColor.Window:
                        colorString = "window";
                        break;
                    case KnownColor.WindowFrame:
                        colorString = "windowframe";
                        break;
                    case KnownColor.WindowText:
                        colorString = "windowtext";
                        break;
                }
            }
            else if (c.IsNamedColor)
            {
                if (c == Color.LightGray)
                {
                    // special case due to mismatch between Html and enum spelling
                    colorString = "LightGrey";
                }
                else
                {
                    colorString = c.Name;
                }
            }
            else
            {
                colorString = $"#{c.R:X2}{c.G:X2}{c.B:X2}";
            }

            return colorString;
        }

        private static void InitializeHtmlSysColorTable()
        {
            s_htmlSysColorTable = new Dictionary<string, Color>(27)
            {
                ["activeborder"] = Color.FromKnownColor(KnownColor.ActiveBorder),
                ["activecaption"] = Color.FromKnownColor(KnownColor.ActiveCaption),
                ["appworkspace"] = Color.FromKnownColor(KnownColor.AppWorkspace),
                ["background"] = Color.FromKnownColor(KnownColor.Desktop),
                ["buttonface"] = Color.FromKnownColor(KnownColor.Control),
                ["buttonhighlight"] = Color.FromKnownColor(KnownColor.ControlLightLight),
                ["buttonshadow"] = Color.FromKnownColor(KnownColor.ControlDark),
                ["buttontext"] = Color.FromKnownColor(KnownColor.ControlText),
                ["captiontext"] = Color.FromKnownColor(KnownColor.ActiveCaptionText),
                ["graytext"] = Color.FromKnownColor(KnownColor.GrayText),
                ["highlight"] = Color.FromKnownColor(KnownColor.Highlight),
                ["highlighttext"] = Color.FromKnownColor(KnownColor.HighlightText),
                ["inactiveborder"] = Color.FromKnownColor(KnownColor.InactiveBorder),
                ["inactivecaption"] = Color.FromKnownColor(KnownColor.InactiveCaption),
                ["inactivecaptiontext"] = Color.FromKnownColor(KnownColor.InactiveCaptionText),
                ["infobackground"] = Color.FromKnownColor(KnownColor.Info),
                ["infotext"] = Color.FromKnownColor(KnownColor.InfoText),
                ["menu"] = Color.FromKnownColor(KnownColor.Menu),
                ["menutext"] = Color.FromKnownColor(KnownColor.MenuText),
                ["scrollbar"] = Color.FromKnownColor(KnownColor.ScrollBar),
                ["threeddarkshadow"] = Color.FromKnownColor(KnownColor.ControlDarkDark),
                ["threedface"] = Color.FromKnownColor(KnownColor.Control),
                ["threedhighlight"] = Color.FromKnownColor(KnownColor.ControlLight),
                ["threedlightshadow"] = Color.FromKnownColor(KnownColor.ControlLightLight),
                ["window"] = Color.FromKnownColor(KnownColor.Window),
                ["windowframe"] = Color.FromKnownColor(KnownColor.WindowFrame),
                ["windowtext"] = Color.FromKnownColor(KnownColor.WindowText)
            };
        }
    }
}
