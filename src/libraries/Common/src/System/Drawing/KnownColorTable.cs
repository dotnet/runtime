// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Drawing
{
    internal static class KnownColorTable
    {
        public const int KnownColorCount = 176;
        public const uint KnownColorCategorySystem = 0;
        public const uint KnownColorCategoryWeb = 1;
        public const uint KnownColorCategoryUnknown = 2;

        // All known colors (in order of definition in the KnownColor enum).
        // An entry's first element is the color category, and second element is color value.
        public static readonly uint[,] s_colorTable = new uint[KnownColorCount, 2]
        {
            // "not a known color"
            { KnownColorCategoryUnknown, 0 },
            // "System" colors, Part 1
#if FEATURE_WINDOWS_SYSTEM_COLORS
            { KnownColorCategorySystem, (uint)(byte)Interop.User32.Win32SystemColors.ActiveBorder },
            { KnownColorCategorySystem, (uint)(byte)Interop.User32.Win32SystemColors.ActiveCaption },
            { KnownColorCategorySystem, (uint)(byte)Interop.User32.Win32SystemColors.ActiveCaptionText },
            { KnownColorCategorySystem, (uint)(byte)Interop.User32.Win32SystemColors.AppWorkspace },
            { KnownColorCategorySystem, (uint)(byte)Interop.User32.Win32SystemColors.Control },
            { KnownColorCategorySystem, (uint)(byte)Interop.User32.Win32SystemColors.ControlDark },
            { KnownColorCategorySystem, (uint)(byte)Interop.User32.Win32SystemColors.ControlDarkDark },
            { KnownColorCategorySystem, (uint)(byte)Interop.User32.Win32SystemColors.ControlLight },
            { KnownColorCategorySystem, (uint)(byte)Interop.User32.Win32SystemColors.ControlLightLight },
            { KnownColorCategorySystem, (uint)(byte)Interop.User32.Win32SystemColors.ControlText },
            { KnownColorCategorySystem, (uint)(byte)Interop.User32.Win32SystemColors.Desktop },
            { KnownColorCategorySystem, (uint)(byte)Interop.User32.Win32SystemColors.GrayText },
            { KnownColorCategorySystem, (uint)(byte)Interop.User32.Win32SystemColors.Highlight },
            { KnownColorCategorySystem, (uint)(byte)Interop.User32.Win32SystemColors.HighlightText },
            { KnownColorCategorySystem, (uint)(byte)Interop.User32.Win32SystemColors.HotTrack },
            { KnownColorCategorySystem, (uint)(byte)Interop.User32.Win32SystemColors.InactiveBorder },
            { KnownColorCategorySystem, (uint)(byte)Interop.User32.Win32SystemColors.InactiveCaption },
            { KnownColorCategorySystem, (uint)(byte)Interop.User32.Win32SystemColors.InactiveCaptionText },
            { KnownColorCategorySystem, (uint)(byte)Interop.User32.Win32SystemColors.Info },
            { KnownColorCategorySystem, (uint)(byte)Interop.User32.Win32SystemColors.InfoText },
            { KnownColorCategorySystem, (uint)(byte)Interop.User32.Win32SystemColors.Menu },
            { KnownColorCategorySystem, (uint)(byte)Interop.User32.Win32SystemColors.MenuText },
            { KnownColorCategorySystem, (uint)(byte)Interop.User32.Win32SystemColors.ScrollBar },
            { KnownColorCategorySystem, (uint)(byte)Interop.User32.Win32SystemColors.Window },
            { KnownColorCategorySystem, (uint)(byte)Interop.User32.Win32SystemColors.WindowFrame },
            { KnownColorCategorySystem, (uint)(byte)Interop.User32.Win32SystemColors.WindowText },
#else
            // Hard-coded constants, based on default Windows settings.
            { KnownColorCategorySystem, 0xFFD4D0C8 },     // ActiveBorder
            { KnownColorCategorySystem, 0xFF0054E3 },     // ActiveCaption
            { KnownColorCategorySystem, 0xFFFFFFFF },     // ActiveCaptionText
            { KnownColorCategorySystem, 0xFF808080 },     // AppWorkspace
            { KnownColorCategorySystem, 0xFFECE9D8 },     // Control
            { KnownColorCategorySystem, 0xFFACA899 },     // ControlDark
            { KnownColorCategorySystem, 0xFF716F64 },     // ControlDarkDark
            { KnownColorCategorySystem, 0xFFF1EFE2 },     // ControlLight
            { KnownColorCategorySystem, 0xFFFFFFFF },     // ControlLightLight
            { KnownColorCategorySystem, 0xFF000000 },     // ControlText
            { KnownColorCategorySystem, 0xFF004E98 },     // Desktop
            { KnownColorCategorySystem, 0xFFACA899 },     // GrayText
            { KnownColorCategorySystem, 0xFF316AC5 },     // Highlight
            { KnownColorCategorySystem, 0xFFFFFFFF },     // HighlightText
            { KnownColorCategorySystem, 0xFF000080 },     // HotTrack
            { KnownColorCategorySystem, 0xFFD4D0C8 },     // InactiveBorder
            { KnownColorCategorySystem, 0xFF7A96DF },     // InactiveCaption
            { KnownColorCategorySystem, 0xFFD8E4F8 },     // InactiveCaptionText
            { KnownColorCategorySystem, 0xFFFFFFE1 },     // Info
            { KnownColorCategorySystem, 0xFF000000 },     // InfoText
            { KnownColorCategorySystem, 0xFFFFFFFF },     // Menu
            { KnownColorCategorySystem, 0xFF000000 },     // MenuText
            { KnownColorCategorySystem, 0xFFD4D0C8 },     // ScrollBar
            { KnownColorCategorySystem, 0xFFFFFFFF },     // Window
            { KnownColorCategorySystem, 0xFF000000 },     // WindowFrame
            { KnownColorCategorySystem, 0xFF000000 },     // WindowText
#endif
            // "Web" Colors, Part 1
            { KnownColorCategoryWeb, 0x00FFFFFF },     // Transparent
            { KnownColorCategoryWeb, 0xFFF0F8FF },     // AliceBlue
            { KnownColorCategoryWeb, 0xFFFAEBD7 },     // AntiqueWhite
            { KnownColorCategoryWeb, 0xFF00FFFF },     // Aqua
            { KnownColorCategoryWeb, 0xFF7FFFD4 },     // Aquamarine
            { KnownColorCategoryWeb, 0xFFF0FFFF },     // Azure
            { KnownColorCategoryWeb, 0xFFF5F5DC },     // Beige
            { KnownColorCategoryWeb, 0xFFFFE4C4 },     // Bisque
            { KnownColorCategoryWeb, 0xFF000000 },     // Black
            { KnownColorCategoryWeb, 0xFFFFEBCD },     // BlanchedAlmond
            { KnownColorCategoryWeb, 0xFF0000FF },     // Blue
            { KnownColorCategoryWeb, 0xFF8A2BE2 },     // BlueViolet
            { KnownColorCategoryWeb, 0xFFA52A2A },     // Brown
            { KnownColorCategoryWeb, 0xFFDEB887 },     // BurlyWood
            { KnownColorCategoryWeb, 0xFF5F9EA0 },     // CadetBlue
            { KnownColorCategoryWeb, 0xFF7FFF00 },     // Chartreuse
            { KnownColorCategoryWeb, 0xFFD2691E },     // Chocolate
            { KnownColorCategoryWeb, 0xFFFF7F50 },     // Coral
            { KnownColorCategoryWeb, 0xFF6495ED },     // CornflowerBlue
            { KnownColorCategoryWeb, 0xFFFFF8DC },     // Cornsilk
            { KnownColorCategoryWeb, 0xFFDC143C },     // Crimson
            { KnownColorCategoryWeb, 0xFF00FFFF },     // Cyan
            { KnownColorCategoryWeb, 0xFF00008B },     // DarkBlue
            { KnownColorCategoryWeb, 0xFF008B8B },     // DarkCyan
            { KnownColorCategoryWeb, 0xFFB8860B },     // DarkGoldenrod
            { KnownColorCategoryWeb, 0xFFA9A9A9 },     // DarkGray
            { KnownColorCategoryWeb, 0xFF006400 },     // DarkGreen
            { KnownColorCategoryWeb, 0xFFBDB76B },     // DarkKhaki
            { KnownColorCategoryWeb, 0xFF8B008B },     // DarkMagenta
            { KnownColorCategoryWeb, 0xFF556B2F },     // DarkOliveGreen
            { KnownColorCategoryWeb, 0xFFFF8C00 },     // DarkOrange
            { KnownColorCategoryWeb, 0xFF9932CC },     // DarkOrchid
            { KnownColorCategoryWeb, 0xFF8B0000 },     // DarkRed
            { KnownColorCategoryWeb, 0xFFE9967A },     // DarkSalmon
            { KnownColorCategoryWeb, 0xFF8FBC8F },     // DarkSeaGreen
            { KnownColorCategoryWeb, 0xFF483D8B },     // DarkSlateBlue
            { KnownColorCategoryWeb, 0xFF2F4F4F },     // DarkSlateGray
            { KnownColorCategoryWeb, 0xFF00CED1 },     // DarkTurquoise
            { KnownColorCategoryWeb, 0xFF9400D3 },     // DarkViolet
            { KnownColorCategoryWeb, 0xFFFF1493 },     // DeepPink
            { KnownColorCategoryWeb, 0xFF00BFFF },     // DeepSkyBlue
            { KnownColorCategoryWeb, 0xFF696969 },     // DimGray
            { KnownColorCategoryWeb, 0xFF1E90FF },     // DodgerBlue
            { KnownColorCategoryWeb, 0xFFB22222 },     // Firebrick
            { KnownColorCategoryWeb, 0xFFFFFAF0 },     // FloralWhite
            { KnownColorCategoryWeb, 0xFF228B22 },     // ForestGreen
            { KnownColorCategoryWeb, 0xFFFF00FF },     // Fuchsia
            { KnownColorCategoryWeb, 0xFFDCDCDC },     // Gainsboro
            { KnownColorCategoryWeb, 0xFFF8F8FF },     // GhostWhite
            { KnownColorCategoryWeb, 0xFFFFD700 },     // Gold
            { KnownColorCategoryWeb, 0xFFDAA520 },     // Goldenrod
            { KnownColorCategoryWeb, 0xFF808080 },     // Gray
            { KnownColorCategoryWeb, 0xFF008000 },     // Green
            { KnownColorCategoryWeb, 0xFFADFF2F },     // GreenYellow
            { KnownColorCategoryWeb, 0xFFF0FFF0 },     // Honeydew
            { KnownColorCategoryWeb, 0xFFFF69B4 },     // HotPink
            { KnownColorCategoryWeb, 0xFFCD5C5C },     // IndianRed
            { KnownColorCategoryWeb, 0xFF4B0082 },     // Indigo
            { KnownColorCategoryWeb, 0xFFFFFFF0 },     // Ivory
            { KnownColorCategoryWeb, 0xFFF0E68C },     // Khaki
            { KnownColorCategoryWeb, 0xFFE6E6FA },     // Lavender
            { KnownColorCategoryWeb, 0xFFFFF0F5 },     // LavenderBlush
            { KnownColorCategoryWeb, 0xFF7CFC00 },     // LawnGreen
            { KnownColorCategoryWeb, 0xFFFFFACD },     // LemonChiffon
            { KnownColorCategoryWeb, 0xFFADD8E6 },     // LightBlue
            { KnownColorCategoryWeb, 0xFFF08080 },     // LightCoral
            { KnownColorCategoryWeb, 0xFFE0FFFF },     // LightCyan
            { KnownColorCategoryWeb, 0xFFFAFAD2 },     // LightGoldenrodYellow
            { KnownColorCategoryWeb, 0xFFD3D3D3 },     // LightGray
            { KnownColorCategoryWeb, 0xFF90EE90 },     // LightGreen
            { KnownColorCategoryWeb, 0xFFFFB6C1 },     // LightPink
            { KnownColorCategoryWeb, 0xFFFFA07A },     // LightSalmon
            { KnownColorCategoryWeb, 0xFF20B2AA },     // LightSeaGreen
            { KnownColorCategoryWeb, 0xFF87CEFA },     // LightSkyBlue
            { KnownColorCategoryWeb, 0xFF778899 },     // LightSlateGray
            { KnownColorCategoryWeb, 0xFFB0C4DE },     // LightSteelBlue
            { KnownColorCategoryWeb, 0xFFFFFFE0 },     // LightYellow
            { KnownColorCategoryWeb, 0xFF00FF00 },     // Lime
            { KnownColorCategoryWeb, 0xFF32CD32 },     // LimeGreen
            { KnownColorCategoryWeb, 0xFFFAF0E6 },     // Linen
            { KnownColorCategoryWeb, 0xFFFF00FF },     // Magenta
            { KnownColorCategoryWeb, 0xFF800000 },     // Maroon
            { KnownColorCategoryWeb, 0xFF66CDAA },     // MediumAquamarine
            { KnownColorCategoryWeb, 0xFF0000CD },     // MediumBlue
            { KnownColorCategoryWeb, 0xFFBA55D3 },     // MediumOrchid
            { KnownColorCategoryWeb, 0xFF9370DB },     // MediumPurple
            { KnownColorCategoryWeb, 0xFF3CB371 },     // MediumSeaGreen
            { KnownColorCategoryWeb, 0xFF7B68EE },     // MediumSlateBlue
            { KnownColorCategoryWeb, 0xFF00FA9A },     // MediumSpringGreen
            { KnownColorCategoryWeb, 0xFF48D1CC },     // MediumTurquoise
            { KnownColorCategoryWeb, 0xFFC71585 },     // MediumVioletRed
            { KnownColorCategoryWeb, 0xFF191970 },     // MidnightBlue
            { KnownColorCategoryWeb, 0xFFF5FFFA },     // MintCream
            { KnownColorCategoryWeb, 0xFFFFE4E1 },     // MistyRose
            { KnownColorCategoryWeb, 0xFFFFE4B5 },     // Moccasin
            { KnownColorCategoryWeb, 0xFFFFDEAD },     // NavajoWhite
            { KnownColorCategoryWeb, 0xFF000080 },     // Navy
            { KnownColorCategoryWeb, 0xFFFDF5E6 },     // OldLace
            { KnownColorCategoryWeb, 0xFF808000 },     // Olive
            { KnownColorCategoryWeb, 0xFF6B8E23 },     // OliveDrab
            { KnownColorCategoryWeb, 0xFFFFA500 },     // Orange
            { KnownColorCategoryWeb, 0xFFFF4500 },     // OrangeRed
            { KnownColorCategoryWeb, 0xFFDA70D6 },     // Orchid
            { KnownColorCategoryWeb, 0xFFEEE8AA },     // PaleGoldenrod
            { KnownColorCategoryWeb, 0xFF98FB98 },     // PaleGreen
            { KnownColorCategoryWeb, 0xFFAFEEEE },     // PaleTurquoise
            { KnownColorCategoryWeb, 0xFFDB7093 },     // PaleVioletRed
            { KnownColorCategoryWeb, 0xFFFFEFD5 },     // PapayaWhip
            { KnownColorCategoryWeb, 0xFFFFDAB9 },     // PeachPuff
            { KnownColorCategoryWeb, 0xFFCD853F },     // Peru
            { KnownColorCategoryWeb, 0xFFFFC0CB },     // Pink
            { KnownColorCategoryWeb, 0xFFDDA0DD },     // Plum
            { KnownColorCategoryWeb, 0xFFB0E0E6 },     // PowderBlue
            { KnownColorCategoryWeb, 0xFF800080 },     // Purple
            { KnownColorCategoryWeb, 0xFFFF0000 },     // Red
            { KnownColorCategoryWeb, 0xFFBC8F8F },     // RosyBrown
            { KnownColorCategoryWeb, 0xFF4169E1 },     // RoyalBlue
            { KnownColorCategoryWeb, 0xFF8B4513 },     // SaddleBrown
            { KnownColorCategoryWeb, 0xFFFA8072 },     // Salmon
            { KnownColorCategoryWeb, 0xFFF4A460 },     // SandyBrown
            { KnownColorCategoryWeb, 0xFF2E8B57 },     // SeaGreen
            { KnownColorCategoryWeb, 0xFFFFF5EE },     // SeaShell
            { KnownColorCategoryWeb, 0xFFA0522D },     // Sienna
            { KnownColorCategoryWeb, 0xFFC0C0C0 },     // Silver
            { KnownColorCategoryWeb, 0xFF87CEEB },     // SkyBlue
            { KnownColorCategoryWeb, 0xFF6A5ACD },     // SlateBlue
            { KnownColorCategoryWeb, 0xFF708090 },     // SlateGray
            { KnownColorCategoryWeb, 0xFFFFFAFA },     // Snow
            { KnownColorCategoryWeb, 0xFF00FF7F },     // SpringGreen
            { KnownColorCategoryWeb, 0xFF4682B4 },     // SteelBlue
            { KnownColorCategoryWeb, 0xFFD2B48C },     // Tan
            { KnownColorCategoryWeb, 0xFF008080 },     // Teal
            { KnownColorCategoryWeb, 0xFFD8BFD8 },     // Thistle
            { KnownColorCategoryWeb, 0xFFFF6347 },     // Tomato
            { KnownColorCategoryWeb, 0xFF40E0D0 },     // Turquoise
            { KnownColorCategoryWeb, 0xFFEE82EE },     // Violet
            { KnownColorCategoryWeb, 0xFFF5DEB3 },     // Wheat
            { KnownColorCategoryWeb, 0xFFFFFFFF },     // White
            { KnownColorCategoryWeb, 0xFFF5F5F5 },     // WhiteSmoke
            { KnownColorCategoryWeb, 0xFFFFFF00 },     // Yellow
            { KnownColorCategoryWeb, 0xFF9ACD32 },     // YellowGreen
#if FEATURE_WINDOWS_SYSTEM_COLORS
            // "System" colors, Part 2
            { KnownColorCategorySystem, (uint)(byte)Interop.User32.Win32SystemColors.ButtonFace },
            { KnownColorCategorySystem, (uint)(byte)Interop.User32.Win32SystemColors.ButtonHighlight },
            { KnownColorCategorySystem, (uint)(byte)Interop.User32.Win32SystemColors.ButtonShadow },
            { KnownColorCategorySystem, (uint)(byte)Interop.User32.Win32SystemColors.GradientActiveCaption },
            { KnownColorCategorySystem, (uint)(byte)Interop.User32.Win32SystemColors.GradientInactiveCaption },
            { KnownColorCategorySystem, (uint)(byte)Interop.User32.Win32SystemColors.MenuBar },
            { KnownColorCategorySystem, (uint)(byte)Interop.User32.Win32SystemColors.MenuHighlight },
#else
            { KnownColorCategorySystem, 0xFFF0F0F0 },     // ButtonFace
            { KnownColorCategorySystem, 0xFFFFFFFF },     // ButtonHighlight
            { KnownColorCategorySystem, 0xFFA0A0A0 },     // ButtonShadow
            { KnownColorCategorySystem, 0xFFB9D1EA },     // GradientActiveCaption
            { KnownColorCategorySystem, 0xFFD7E4F2 },     // GradientInactiveCaption
            { KnownColorCategorySystem, 0xFFF0F0F0 },     // MenuBar
            { KnownColorCategorySystem, 0xFF3399FF },     // MenuHighlight
#endif
            // "Web" colors, Part 2
            { KnownColorCategoryWeb, 0xFF663399 },     // RebeccaPurple
        };

        internal static Color ArgbToKnownColor(uint argb)
        {
            Debug.Assert((argb & Color.ARGBAlphaMask) == Color.ARGBAlphaMask);

            for (int index = 1; index < KnownColorCount; ++index)
            {
                if (s_colorTable[index, 0] == KnownColorCategoryWeb && s_colorTable[index, 1] == argb)
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

            return s_colorTable[(int)color, 0] == KnownColorCategorySystem
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
