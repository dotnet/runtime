// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Globalization;

namespace System.Drawing
{
    /// <summary>
    /// Translates colors to and from GDI+ <see cref='Color'/> objects.
    /// </summary>
    public static class ColorTranslator
    {
        private const int Win32RedShift = 0;
        private const int Win32GreenShift = 8;
        private const int Win32BlueShift = 16;

        private static Hashtable s_htmlSysColorTable;

        /// <summary>
        /// Translates the specified <see cref='Color'/> to a Win32 color.
        /// </summary>
        public static int ToWin32(Color c)
        {
            return c.R << Win32RedShift | c.G << Win32GreenShift | c.B << Win32BlueShift;
        }

        /// <summary>
        /// Translates the specified <see cref='Color'/> to an Ole color.
        /// </summary>
        public static int ToOle(Color c)
        {
            //    WARNING!!! WARNING!!! WARNING!!! WARNING!!! 
            //    WARNING!!! WARNING!!! WARNING!!! WARNING!!!
            //    We must never have another method called ToOle() with a different signature.
            //    This is so that we can push into the runtime a custom marshaller for OLE_COLOR to Color.

            if (ColorUtil.GetIsKnownColor(c))
            {
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
            //    WARNING!!! WARNING!!! WARNING!!! WARNING!!! 
            //    WARNING!!! WARNING!!! WARNING!!! WARNING!!!
            //    We must never have another method called ToOle() with a different signature.
            //    This is so that we can push into the runtime a custom marshaller for OLE_COLOR to Color.

            switch (oleColor)
            {
                case unchecked((int)0x8000000A):
                    return ColorUtil.FromKnownColor(KnownColor.ActiveBorder);
                case unchecked((int)0x80000002):
                    return ColorUtil.FromKnownColor(KnownColor.ActiveCaption);
                case unchecked((int)0x80000009):
                    return ColorUtil.FromKnownColor(KnownColor.ActiveCaptionText);
                case unchecked((int)0x8000000C):
                    return ColorUtil.FromKnownColor(KnownColor.AppWorkspace);
                case unchecked((int)0x8000000F):
                    return ColorUtil.FromKnownColor(KnownColor.Control);
                case unchecked((int)0x80000010):
                    return ColorUtil.FromKnownColor(KnownColor.ControlDark);
                case unchecked((int)0x80000015):
                    return ColorUtil.FromKnownColor(KnownColor.ControlDarkDark);
                case unchecked((int)0x80000016):
                    return ColorUtil.FromKnownColor(KnownColor.ControlLight);
                case unchecked((int)0x80000014):
                    return ColorUtil.FromKnownColor(KnownColor.ControlLightLight);
                case unchecked((int)0x80000012):
                    return ColorUtil.FromKnownColor(KnownColor.ControlText);
                case unchecked((int)0x80000001):
                    return ColorUtil.FromKnownColor(KnownColor.Desktop);
                case unchecked((int)0x8000001B):
                    return ColorUtil.FromKnownColor(KnownColor.GradientActiveCaption);
                case unchecked((int)0x8000001C):
                    return ColorUtil.FromKnownColor(KnownColor.GradientInactiveCaption);
                case unchecked((int)0x80000011):
                    return ColorUtil.FromKnownColor(KnownColor.GrayText);
                case unchecked((int)0x8000000D):
                    return ColorUtil.FromKnownColor(KnownColor.Highlight);
                case unchecked((int)0x8000000E):
                    return ColorUtil.FromKnownColor(KnownColor.HighlightText);
                case unchecked((int)0x8000001A):
                    return ColorUtil.FromKnownColor(KnownColor.HotTrack);
                case unchecked((int)0x8000000B):
                    return ColorUtil.FromKnownColor(KnownColor.InactiveBorder);
                case unchecked((int)0x80000003):
                    return ColorUtil.FromKnownColor(KnownColor.InactiveCaption);
                case unchecked((int)0x80000013):
                    return ColorUtil.FromKnownColor(KnownColor.InactiveCaptionText);
                case unchecked((int)0x80000018):
                    return ColorUtil.FromKnownColor(KnownColor.Info);
                case unchecked((int)0x80000017):
                    return ColorUtil.FromKnownColor(KnownColor.InfoText);
                case unchecked((int)0x80000004):
                    return ColorUtil.FromKnownColor(KnownColor.Menu);
                case unchecked((int)0x8000001E):
                    return ColorUtil.FromKnownColor(KnownColor.MenuBar);
                case unchecked((int)0x8000001D):
                    return ColorUtil.FromKnownColor(KnownColor.MenuHighlight);
                case unchecked((int)0x80000007):
                    return ColorUtil.FromKnownColor(KnownColor.MenuText);
                case unchecked((int)0x80000000):
                    return ColorUtil.FromKnownColor(KnownColor.ScrollBar);
                case unchecked((int)0x80000005):
                    return ColorUtil.FromKnownColor(KnownColor.Window);
                case unchecked((int)0x80000006):
                    return ColorUtil.FromKnownColor(KnownColor.WindowFrame);
                case unchecked((int)0x80000008):
                    return ColorUtil.FromKnownColor(KnownColor.WindowText);
            }

            Color color = Color.FromArgb((byte)((oleColor >> Win32RedShift) & 0xFF),
                                         (byte)((oleColor >> Win32GreenShift) & 0xFF),
                                         (byte)((oleColor >> Win32BlueShift) & 0xFF));

            return KnownColorTable.ArgbToKnownColor(color.ToArgb());
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

                object o = s_htmlSysColorTable[htmlColor.ToLower(CultureInfo.InvariantCulture)];
                if (o != null)
                {
                    c = (Color)o;
                }
            }

            // resort to type converter which will handle named colors
            if (c.IsEmpty)
            {
                try
                {
                    c = ColorConverterCommon.ConvertFromString(htmlColor, CultureInfo.CurrentCulture);
                }
                catch(Exception ex)
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

            if (ColorUtil.IsSystemColor(c))
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
                        colorString = "buttonface";
                        break;
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
                colorString = "#" + c.R.ToString("X2", null) +
                                    c.G.ToString("X2", null) +
                                    c.B.ToString("X2", null);
            }

            return colorString;
        }

        private static void InitializeHtmlSysColorTable()
        {
            s_htmlSysColorTable = new Hashtable(26);
            s_htmlSysColorTable["activeborder"] = ColorUtil.FromKnownColor(KnownColor.ActiveBorder);
            s_htmlSysColorTable["activecaption"] = ColorUtil.FromKnownColor(KnownColor.ActiveCaption);
            s_htmlSysColorTable["appworkspace"] = ColorUtil.FromKnownColor(KnownColor.AppWorkspace);
            s_htmlSysColorTable["background"] = ColorUtil.FromKnownColor(KnownColor.Desktop);
            s_htmlSysColorTable["buttonface"] = ColorUtil.FromKnownColor(KnownColor.Control);
            s_htmlSysColorTable["buttonhighlight"] = ColorUtil.FromKnownColor(KnownColor.ControlLightLight);
            s_htmlSysColorTable["buttonshadow"] = ColorUtil.FromKnownColor(KnownColor.ControlDark);
            s_htmlSysColorTable["buttontext"] = ColorUtil.FromKnownColor(KnownColor.ControlText);
            s_htmlSysColorTable["captiontext"] = ColorUtil.FromKnownColor(KnownColor.ActiveCaptionText);
            s_htmlSysColorTable["graytext"] = ColorUtil.FromKnownColor(KnownColor.GrayText);
            s_htmlSysColorTable["highlight"] = ColorUtil.FromKnownColor(KnownColor.Highlight);
            s_htmlSysColorTable["highlighttext"] = ColorUtil.FromKnownColor(KnownColor.HighlightText);
            s_htmlSysColorTable["inactiveborder"] = ColorUtil.FromKnownColor(KnownColor.InactiveBorder);
            s_htmlSysColorTable["inactivecaption"] = ColorUtil.FromKnownColor(KnownColor.InactiveCaption);
            s_htmlSysColorTable["inactivecaptiontext"] = ColorUtil.FromKnownColor(KnownColor.InactiveCaptionText);
            s_htmlSysColorTable["infobackground"] = ColorUtil.FromKnownColor(KnownColor.Info);
            s_htmlSysColorTable["infotext"] = ColorUtil.FromKnownColor(KnownColor.InfoText);
            s_htmlSysColorTable["menu"] = ColorUtil.FromKnownColor(KnownColor.Menu);
            s_htmlSysColorTable["menutext"] = ColorUtil.FromKnownColor(KnownColor.MenuText);
            s_htmlSysColorTable["scrollbar"] = ColorUtil.FromKnownColor(KnownColor.ScrollBar);
            s_htmlSysColorTable["threeddarkshadow"] = ColorUtil.FromKnownColor(KnownColor.ControlDarkDark);
            s_htmlSysColorTable["threedface"] = ColorUtil.FromKnownColor(KnownColor.Control);
            s_htmlSysColorTable["threedhighlight"] = ColorUtil.FromKnownColor(KnownColor.ControlLight);
            s_htmlSysColorTable["threedlightshadow"] = ColorUtil.FromKnownColor(KnownColor.ControlLightLight);
            s_htmlSysColorTable["window"] = ColorUtil.FromKnownColor(KnownColor.Window);
            s_htmlSysColorTable["windowframe"] = ColorUtil.FromKnownColor(KnownColor.WindowFrame);
            s_htmlSysColorTable["windowtext"] = ColorUtil.FromKnownColor(KnownColor.WindowText);
        }
    }
}

