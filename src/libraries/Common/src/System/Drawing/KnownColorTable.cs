// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Drawing
{
    internal static class KnownColorTable
    {
        public enum KnownColorCategory : uint
        {
            Unknown,
            System,
            Web,
        }

        public const int knownColorCount = 176;

        // All known colors (in order of definition in the KnownColor enum).
        // An entry's first element is the color category, and second element is color value.
        public static readonly uint[,] s_colorTable = new uint[knownColorCount, 2]
        {
            // "not a known color"
            { (uint)KnownColorCategory.Unknown, 0 },
            // "System" colors, Part 1
#if FEATURE_WINDOWS_SYSTEM_COLORS
            { (uint)KnownColorCategory.System, (uint)(byte)Interop.User32.Win32SystemColors.ActiveBorder },
            { (uint)KnownColorCategory.System, (uint)(byte)Interop.User32.Win32SystemColors.ActiveCaption },
            { (uint)KnownColorCategory.System, (uint)(byte)Interop.User32.Win32SystemColors.ActiveCaptionText },
            { (uint)KnownColorCategory.System, (uint)(byte)Interop.User32.Win32SystemColors.AppWorkspace },
            { (uint)KnownColorCategory.System, (uint)(byte)Interop.User32.Win32SystemColors.Control },
            { (uint)KnownColorCategory.System, (uint)(byte)Interop.User32.Win32SystemColors.ControlDark },
            { (uint)KnownColorCategory.System, (uint)(byte)Interop.User32.Win32SystemColors.ControlDarkDark },
            { (uint)KnownColorCategory.System, (uint)(byte)Interop.User32.Win32SystemColors.ControlLight },
            { (uint)KnownColorCategory.System, (uint)(byte)Interop.User32.Win32SystemColors.ControlLightLight },
            { (uint)KnownColorCategory.System, (uint)(byte)Interop.User32.Win32SystemColors.ControlText },
            { (uint)KnownColorCategory.System, (uint)(byte)Interop.User32.Win32SystemColors.Desktop },
            { (uint)KnownColorCategory.System, (uint)(byte)Interop.User32.Win32SystemColors.GrayText },
            { (uint)KnownColorCategory.System, (uint)(byte)Interop.User32.Win32SystemColors.Highlight },
            { (uint)KnownColorCategory.System, (uint)(byte)Interop.User32.Win32SystemColors.HighlightText },
            { (uint)KnownColorCategory.System, (uint)(byte)Interop.User32.Win32SystemColors.HotTrack },
            { (uint)KnownColorCategory.System, (uint)(byte)Interop.User32.Win32SystemColors.InactiveBorder },
            { (uint)KnownColorCategory.System, (uint)(byte)Interop.User32.Win32SystemColors.InactiveCaption },
            { (uint)KnownColorCategory.System, (uint)(byte)Interop.User32.Win32SystemColors.InactiveCaptionText },
            { (uint)KnownColorCategory.System, (uint)(byte)Interop.User32.Win32SystemColors.Info },
            { (uint)KnownColorCategory.System, (uint)(byte)Interop.User32.Win32SystemColors.InfoText },
            { (uint)KnownColorCategory.System, (uint)(byte)Interop.User32.Win32SystemColors.Menu },
            { (uint)KnownColorCategory.System, (uint)(byte)Interop.User32.Win32SystemColors.MenuText },
            { (uint)KnownColorCategory.System, (uint)(byte)Interop.User32.Win32SystemColors.ScrollBar },
            { (uint)KnownColorCategory.System, (uint)(byte)Interop.User32.Win32SystemColors.Window },
            { (uint)KnownColorCategory.System, (uint)(byte)Interop.User32.Win32SystemColors.WindowFrame },
            { (uint)KnownColorCategory.System, (uint)(byte)Interop.User32.Win32SystemColors.WindowText },
#else
            // Hard-coded constants, based on default Windows settings.
            { (uint)KnownColorCategory.System, 0xFFD4D0C8 },     // ActiveBorder
            { (uint)KnownColorCategory.System, 0xFF0054E3 },     // ActiveCaption
            { (uint)KnownColorCategory.System, 0xFFFFFFFF },     // ActiveCaptionText
            { (uint)KnownColorCategory.System, 0xFF808080 },     // AppWorkspace
            { (uint)KnownColorCategory.System, 0xFFECE9D8 },     // Control
            { (uint)KnownColorCategory.System, 0xFFACA899 },     // ControlDark
            { (uint)KnownColorCategory.System, 0xFF716F64 },     // ControlDarkDark
            { (uint)KnownColorCategory.System, 0xFFF1EFE2 },     // ControlLight
            { (uint)KnownColorCategory.System, 0xFFFFFFFF },     // ControlLightLight
            { (uint)KnownColorCategory.System, 0xFF000000 },     // ControlText
            { (uint)KnownColorCategory.System, 0xFF004E98 },     // Desktop
            { (uint)KnownColorCategory.System, 0xFFACA899 },     // GrayText
            { (uint)KnownColorCategory.System, 0xFF316AC5 },     // Highlight
            { (uint)KnownColorCategory.System, 0xFFFFFFFF },     // HighlightText
            { (uint)KnownColorCategory.System, 0xFF000080 },     // HotTrack
            { (uint)KnownColorCategory.System, 0xFFD4D0C8 },     // InactiveBorder
            { (uint)KnownColorCategory.System, 0xFF7A96DF },     // InactiveCaption
            { (uint)KnownColorCategory.System, 0xFFD8E4F8 },     // InactiveCaptionText
            { (uint)KnownColorCategory.System, 0xFFFFFFE1 },     // Info
            { (uint)KnownColorCategory.System, 0xFF000000 },     // InfoText
            { (uint)KnownColorCategory.System, 0xFFFFFFFF },     // Menu
            { (uint)KnownColorCategory.System, 0xFF000000 },     // MenuText
            { (uint)KnownColorCategory.System, 0xFFD4D0C8 },     // ScrollBar
            { (uint)KnownColorCategory.System, 0xFFFFFFFF },     // Window
            { (uint)KnownColorCategory.System, 0xFF000000 },     // WindowFrame
            { (uint)KnownColorCategory.System, 0xFF000000 },     // WindowText
#endif
            // "Web" Colors, Part 1
            { (uint)KnownColorCategory.Web, 0x00FFFFFF },     // Transparent
            { (uint)KnownColorCategory.Web, 0xFFF0F8FF },     // AliceBlue
            { (uint)KnownColorCategory.Web, 0xFFFAEBD7 },     // AntiqueWhite
            { (uint)KnownColorCategory.Web, 0xFF00FFFF },     // Aqua
            { (uint)KnownColorCategory.Web, 0xFF7FFFD4 },     // Aquamarine
            { (uint)KnownColorCategory.Web, 0xFFF0FFFF },     // Azure
            { (uint)KnownColorCategory.Web, 0xFFF5F5DC },     // Beige
            { (uint)KnownColorCategory.Web, 0xFFFFE4C4 },     // Bisque
            { (uint)KnownColorCategory.Web, 0xFF000000 },     // Black
            { (uint)KnownColorCategory.Web, 0xFFFFEBCD },     // BlanchedAlmond
            { (uint)KnownColorCategory.Web, 0xFF0000FF },     // Blue
            { (uint)KnownColorCategory.Web, 0xFF8A2BE2 },     // BlueViolet
            { (uint)KnownColorCategory.Web, 0xFFA52A2A },     // Brown
            { (uint)KnownColorCategory.Web, 0xFFDEB887 },     // BurlyWood
            { (uint)KnownColorCategory.Web, 0xFF5F9EA0 },     // CadetBlue
            { (uint)KnownColorCategory.Web, 0xFF7FFF00 },     // Chartreuse
            { (uint)KnownColorCategory.Web, 0xFFD2691E },     // Chocolate
            { (uint)KnownColorCategory.Web, 0xFFFF7F50 },     // Coral
            { (uint)KnownColorCategory.Web, 0xFF6495ED },     // CornflowerBlue
            { (uint)KnownColorCategory.Web, 0xFFFFF8DC },     // Cornsilk
            { (uint)KnownColorCategory.Web, 0xFFDC143C },     // Crimson
            { (uint)KnownColorCategory.Web, 0xFF00FFFF },     // Cyan
            { (uint)KnownColorCategory.Web, 0xFF00008B },     // DarkBlue
            { (uint)KnownColorCategory.Web, 0xFF008B8B },     // DarkCyan
            { (uint)KnownColorCategory.Web, 0xFFB8860B },     // DarkGoldenrod
            { (uint)KnownColorCategory.Web, 0xFFA9A9A9 },     // DarkGray
            { (uint)KnownColorCategory.Web, 0xFF006400 },     // DarkGreen
            { (uint)KnownColorCategory.Web, 0xFFBDB76B },     // DarkKhaki
            { (uint)KnownColorCategory.Web, 0xFF8B008B },     // DarkMagenta
            { (uint)KnownColorCategory.Web, 0xFF556B2F },     // DarkOliveGreen
            { (uint)KnownColorCategory.Web, 0xFFFF8C00 },     // DarkOrange
            { (uint)KnownColorCategory.Web, 0xFF9932CC },     // DarkOrchid
            { (uint)KnownColorCategory.Web, 0xFF8B0000 },     // DarkRed
            { (uint)KnownColorCategory.Web, 0xFFE9967A },     // DarkSalmon
            { (uint)KnownColorCategory.Web, 0xFF8FBC8F },     // DarkSeaGreen
            { (uint)KnownColorCategory.Web, 0xFF483D8B },     // DarkSlateBlue
            { (uint)KnownColorCategory.Web, 0xFF2F4F4F },     // DarkSlateGray
            { (uint)KnownColorCategory.Web, 0xFF00CED1 },     // DarkTurquoise
            { (uint)KnownColorCategory.Web, 0xFF9400D3 },     // DarkViolet
            { (uint)KnownColorCategory.Web, 0xFFFF1493 },     // DeepPink
            { (uint)KnownColorCategory.Web, 0xFF00BFFF },     // DeepSkyBlue
            { (uint)KnownColorCategory.Web, 0xFF696969 },     // DimGray
            { (uint)KnownColorCategory.Web, 0xFF1E90FF },     // DodgerBlue
            { (uint)KnownColorCategory.Web, 0xFFB22222 },     // Firebrick
            { (uint)KnownColorCategory.Web, 0xFFFFFAF0 },     // FloralWhite
            { (uint)KnownColorCategory.Web, 0xFF228B22 },     // ForestGreen
            { (uint)KnownColorCategory.Web, 0xFFFF00FF },     // Fuchsia
            { (uint)KnownColorCategory.Web, 0xFFDCDCDC },     // Gainsboro
            { (uint)KnownColorCategory.Web, 0xFFF8F8FF },     // GhostWhite
            { (uint)KnownColorCategory.Web, 0xFFFFD700 },     // Gold
            { (uint)KnownColorCategory.Web, 0xFFDAA520 },     // Goldenrod
            { (uint)KnownColorCategory.Web, 0xFF808080 },     // Gray
            { (uint)KnownColorCategory.Web, 0xFF008000 },     // Green
            { (uint)KnownColorCategory.Web, 0xFFADFF2F },     // GreenYellow
            { (uint)KnownColorCategory.Web, 0xFFF0FFF0 },     // Honeydew
            { (uint)KnownColorCategory.Web, 0xFFFF69B4 },     // HotPink
            { (uint)KnownColorCategory.Web, 0xFFCD5C5C },     // IndianRed
            { (uint)KnownColorCategory.Web, 0xFF4B0082 },     // Indigo
            { (uint)KnownColorCategory.Web, 0xFFFFFFF0 },     // Ivory
            { (uint)KnownColorCategory.Web, 0xFFF0E68C },     // Khaki
            { (uint)KnownColorCategory.Web, 0xFFE6E6FA },     // Lavender
            { (uint)KnownColorCategory.Web, 0xFFFFF0F5 },     // LavenderBlush
            { (uint)KnownColorCategory.Web, 0xFF7CFC00 },     // LawnGreen
            { (uint)KnownColorCategory.Web, 0xFFFFFACD },     // LemonChiffon
            { (uint)KnownColorCategory.Web, 0xFFADD8E6 },     // LightBlue
            { (uint)KnownColorCategory.Web, 0xFFF08080 },     // LightCoral
            { (uint)KnownColorCategory.Web, 0xFFE0FFFF },     // LightCyan
            { (uint)KnownColorCategory.Web, 0xFFFAFAD2 },     // LightGoldenrodYellow
            { (uint)KnownColorCategory.Web, 0xFFD3D3D3 },     // LightGray
            { (uint)KnownColorCategory.Web, 0xFF90EE90 },     // LightGreen
            { (uint)KnownColorCategory.Web, 0xFFFFB6C1 },     // LightPink
            { (uint)KnownColorCategory.Web, 0xFFFFA07A },     // LightSalmon
            { (uint)KnownColorCategory.Web, 0xFF20B2AA },     // LightSeaGreen
            { (uint)KnownColorCategory.Web, 0xFF87CEFA },     // LightSkyBlue
            { (uint)KnownColorCategory.Web, 0xFF778899 },     // LightSlateGray
            { (uint)KnownColorCategory.Web, 0xFFB0C4DE },     // LightSteelBlue
            { (uint)KnownColorCategory.Web, 0xFFFFFFE0 },     // LightYellow
            { (uint)KnownColorCategory.Web, 0xFF00FF00 },     // Lime
            { (uint)KnownColorCategory.Web, 0xFF32CD32 },     // LimeGreen
            { (uint)KnownColorCategory.Web, 0xFFFAF0E6 },     // Linen
            { (uint)KnownColorCategory.Web, 0xFFFF00FF },     // Magenta
            { (uint)KnownColorCategory.Web, 0xFF800000 },     // Maroon
            { (uint)KnownColorCategory.Web, 0xFF66CDAA },     // MediumAquamarine
            { (uint)KnownColorCategory.Web, 0xFF0000CD },     // MediumBlue
            { (uint)KnownColorCategory.Web, 0xFFBA55D3 },     // MediumOrchid
            { (uint)KnownColorCategory.Web, 0xFF9370DB },     // MediumPurple
            { (uint)KnownColorCategory.Web, 0xFF3CB371 },     // MediumSeaGreen
            { (uint)KnownColorCategory.Web, 0xFF7B68EE },     // MediumSlateBlue
            { (uint)KnownColorCategory.Web, 0xFF00FA9A },     // MediumSpringGreen
            { (uint)KnownColorCategory.Web, 0xFF48D1CC },     // MediumTurquoise
            { (uint)KnownColorCategory.Web, 0xFFC71585 },     // MediumVioletRed
            { (uint)KnownColorCategory.Web, 0xFF191970 },     // MidnightBlue
            { (uint)KnownColorCategory.Web, 0xFFF5FFFA },     // MintCream
            { (uint)KnownColorCategory.Web, 0xFFFFE4E1 },     // MistyRose
            { (uint)KnownColorCategory.Web, 0xFFFFE4B5 },     // Moccasin
            { (uint)KnownColorCategory.Web, 0xFFFFDEAD },     // NavajoWhite
            { (uint)KnownColorCategory.Web, 0xFF000080 },     // Navy
            { (uint)KnownColorCategory.Web, 0xFFFDF5E6 },     // OldLace
            { (uint)KnownColorCategory.Web, 0xFF808000 },     // Olive
            { (uint)KnownColorCategory.Web, 0xFF6B8E23 },     // OliveDrab
            { (uint)KnownColorCategory.Web, 0xFFFFA500 },     // Orange
            { (uint)KnownColorCategory.Web, 0xFFFF4500 },     // OrangeRed
            { (uint)KnownColorCategory.Web, 0xFFDA70D6 },     // Orchid
            { (uint)KnownColorCategory.Web, 0xFFEEE8AA },     // PaleGoldenrod
            { (uint)KnownColorCategory.Web, 0xFF98FB98 },     // PaleGreen
            { (uint)KnownColorCategory.Web, 0xFFAFEEEE },     // PaleTurquoise
            { (uint)KnownColorCategory.Web, 0xFFDB7093 },     // PaleVioletRed
            { (uint)KnownColorCategory.Web, 0xFFFFEFD5 },     // PapayaWhip
            { (uint)KnownColorCategory.Web, 0xFFFFDAB9 },     // PeachPuff
            { (uint)KnownColorCategory.Web, 0xFFCD853F },     // Peru
            { (uint)KnownColorCategory.Web, 0xFFFFC0CB },     // Pink
            { (uint)KnownColorCategory.Web, 0xFFDDA0DD },     // Plum
            { (uint)KnownColorCategory.Web, 0xFFB0E0E6 },     // PowderBlue
            { (uint)KnownColorCategory.Web, 0xFF800080 },     // Purple
            { (uint)KnownColorCategory.Web, 0xFFFF0000 },     // Red
            { (uint)KnownColorCategory.Web, 0xFFBC8F8F },     // RosyBrown
            { (uint)KnownColorCategory.Web, 0xFF4169E1 },     // RoyalBlue
            { (uint)KnownColorCategory.Web, 0xFF8B4513 },     // SaddleBrown
            { (uint)KnownColorCategory.Web, 0xFFFA8072 },     // Salmon
            { (uint)KnownColorCategory.Web, 0xFFF4A460 },     // SandyBrown
            { (uint)KnownColorCategory.Web, 0xFF2E8B57 },     // SeaGreen
            { (uint)KnownColorCategory.Web, 0xFFFFF5EE },     // SeaShell
            { (uint)KnownColorCategory.Web, 0xFFA0522D },     // Sienna
            { (uint)KnownColorCategory.Web, 0xFFC0C0C0 },     // Silver
            { (uint)KnownColorCategory.Web, 0xFF87CEEB },     // SkyBlue
            { (uint)KnownColorCategory.Web, 0xFF6A5ACD },     // SlateBlue
            { (uint)KnownColorCategory.Web, 0xFF708090 },     // SlateGray
            { (uint)KnownColorCategory.Web, 0xFFFFFAFA },     // Snow
            { (uint)KnownColorCategory.Web, 0xFF00FF7F },     // SpringGreen
            { (uint)KnownColorCategory.Web, 0xFF4682B4 },     // SteelBlue
            { (uint)KnownColorCategory.Web, 0xFFD2B48C },     // Tan
            { (uint)KnownColorCategory.Web, 0xFF008080 },     // Teal
            { (uint)KnownColorCategory.Web, 0xFFD8BFD8 },     // Thistle
            { (uint)KnownColorCategory.Web, 0xFFFF6347 },     // Tomato
            { (uint)KnownColorCategory.Web, 0xFF40E0D0 },     // Turquoise
            { (uint)KnownColorCategory.Web, 0xFFEE82EE },     // Violet
            { (uint)KnownColorCategory.Web, 0xFFF5DEB3 },     // Wheat
            { (uint)KnownColorCategory.Web, 0xFFFFFFFF },     // White
            { (uint)KnownColorCategory.Web, 0xFFF5F5F5 },     // WhiteSmoke
            { (uint)KnownColorCategory.Web, 0xFFFFFF00 },     // Yellow
            { (uint)KnownColorCategory.Web, 0xFF9ACD32 },     // YellowGreen
#if FEATURE_WINDOWS_SYSTEM_COLORS
            // "System" colors, Part 2
            { (uint)KnownColorCategory.System, (uint)(byte)Interop.User32.Win32SystemColors.ButtonFace },
            { (uint)KnownColorCategory.System, (uint)(byte)Interop.User32.Win32SystemColors.ButtonHighlight },
            { (uint)KnownColorCategory.System, (uint)(byte)Interop.User32.Win32SystemColors.ButtonShadow },
            { (uint)KnownColorCategory.System, (uint)(byte)Interop.User32.Win32SystemColors.GradientActiveCaption },
            { (uint)KnownColorCategory.System, (uint)(byte)Interop.User32.Win32SystemColors.GradientInactiveCaption },
            { (uint)KnownColorCategory.System, (uint)(byte)Interop.User32.Win32SystemColors.MenuBar },
            { (uint)KnownColorCategory.System, (uint)(byte)Interop.User32.Win32SystemColors.MenuHighlight },
#else
            { (uint)KnownColorCategory.System, 0xFFF0F0F0 },     // ButtonFace
            { (uint)KnownColorCategory.System, 0xFFFFFFFF },     // ButtonHighlight
            { (uint)KnownColorCategory.System, 0xFFA0A0A0 },     // ButtonShadow
            { (uint)KnownColorCategory.System, 0xFFB9D1EA },     // GradientActiveCaption
            { (uint)KnownColorCategory.System, 0xFFD7E4F2 },     // GradientInactiveCaption
            { (uint)KnownColorCategory.System, 0xFFF0F0F0 },     // MenuBar
            { (uint)KnownColorCategory.System, 0xFF3399FF },     // MenuHighlight
#endif
            // "Web" colors, Part 2
            { (uint)KnownColorCategory.Web, 0xFF663399 },     // RebeccaPurple
        };

        internal static Color ArgbToKnownColor(uint argb)
        {
            Debug.Assert((argb & Color.ARGBAlphaMask) == Color.ARGBAlphaMask);

            for (int index = 1; index < knownColorCount; ++index)
            {
                if (s_colorTable[index, 1] == argb)
                {
                    return Color.FromKnownColor((KnownColor)index);
                }
            }

            // Not a known color
            return Color.FromArgb((int)argb);
        }

        public static uint KnownColorToArgb(KnownColor color)
        {
            Debug.Assert(color > 0 && color <= KnownColor.RebeccaPurple);

            return s_colorTable[(int)color, 0] == (uint)KnownColorCategory.System
                 ? GetSystemColorArgb(color)
                 : s_colorTable[(int)color, 1];
        }

#if FEATURE_WINDOWS_SYSTEM_COLORS
        public static uint GetSystemColorArgb(KnownColor color)
        {
            Debug.Assert(Color.IsKnownColorSystem(color));

            return ColorTranslator.COLORREFToARGB(Interop.User32.GetSysColor((byte)s_colorTable[(int)color, 1]));
        }
#else
        public static uint GetSystemColorArgb(KnownColor color)
        {
            Debug.Assert(Color.IsKnownColorSystem(color));

            return s_colorTable[(int)color, 1];
        }
#endif
    }
}
