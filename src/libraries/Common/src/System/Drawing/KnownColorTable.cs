// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Drawing
{
    internal static class KnownColorTable
    {
        // All non system colors (in order of definition in the KnownColor enum).
        private static readonly uint[] s_colorTable = new uint[]
        {
            0x00FFFFFF,     // Transparent
            0xFFF0F8FF,     // AliceBlue
            0xFFFAEBD7,     // AntiqueWhite
            0xFF00FFFF,     // Aqua
            0xFF7FFFD4,     // Aquamarine
            0xFFF0FFFF,     // Azure
            0xFFF5F5DC,     // Beige
            0xFFFFE4C4,     // Bisque
            0xFF000000,     // Black
            0xFFFFEBCD,     // BlanchedAlmond
            0xFF0000FF,     // Blue
            0xFF8A2BE2,     // BlueViolet
            0xFFA52A2A,     // Brown
            0xFFDEB887,     // BurlyWood
            0xFF5F9EA0,     // CadetBlue
            0xFF7FFF00,     // Chartreuse
            0xFFD2691E,     // Chocolate
            0xFFFF7F50,     // Coral
            0xFF6495ED,     // CornflowerBlue
            0xFFFFF8DC,     // Cornsilk
            0xFFDC143C,     // Crimson
            0xFF00FFFF,     // Cyan
            0xFF00008B,     // DarkBlue
            0xFF008B8B,     // DarkCyan
            0xFFB8860B,     // DarkGoldenrod
            0xFFA9A9A9,     // DarkGray
            0xFF006400,     // DarkGreen
            0xFFBDB76B,     // DarkKhaki
            0xFF8B008B,     // DarkMagenta
            0xFF556B2F,     // DarkOliveGreen
            0xFFFF8C00,     // DarkOrange
            0xFF9932CC,     // DarkOrchid
            0xFF8B0000,     // DarkRed
            0xFFE9967A,     // DarkSalmon
            0xFF8FBC8F,     // DarkSeaGreen
            0xFF483D8B,     // DarkSlateBlue
            0xFF2F4F4F,     // DarkSlateGray
            0xFF00CED1,     // DarkTurquoise
            0xFF9400D3,     // DarkViolet
            0xFFFF1493,     // DeepPink
            0xFF00BFFF,     // DeepSkyBlue
            0xFF696969,     // DimGray
            0xFF1E90FF,     // DodgerBlue
            0xFFB22222,     // Firebrick
            0xFFFFFAF0,     // FloralWhite
            0xFF228B22,     // ForestGreen
            0xFFFF00FF,     // Fuchsia
            0xFFDCDCDC,     // Gainsboro
            0xFFF8F8FF,     // GhostWhite
            0xFFFFD700,     // Gold
            0xFFDAA520,     // Goldenrod
            0xFF808080,     // Gray
            0xFF008000,     // Green
            0xFFADFF2F,     // GreenYellow
            0xFFF0FFF0,     // Honeydew
            0xFFFF69B4,     // HotPink
            0xFFCD5C5C,     // IndianRed
            0xFF4B0082,     // Indigo
            0xFFFFFFF0,     // Ivory
            0xFFF0E68C,     // Khaki
            0xFFE6E6FA,     // Lavender
            0xFFFFF0F5,     // LavenderBlush
            0xFF7CFC00,     // LawnGreen
            0xFFFFFACD,     // LemonChiffon
            0xFFADD8E6,     // LightBlue
            0xFFF08080,     // LightCoral
            0xFFE0FFFF,     // LightCyan
            0xFFFAFAD2,     // LightGoldenrodYellow
            0xFFD3D3D3,     // LightGray
            0xFF90EE90,     // LightGreen
            0xFFFFB6C1,     // LightPink
            0xFFFFA07A,     // LightSalmon
            0xFF20B2AA,     // LightSeaGreen
            0xFF87CEFA,     // LightSkyBlue
            0xFF778899,     // LightSlateGray
            0xFFB0C4DE,     // LightSteelBlue
            0xFFFFFFE0,     // LightYellow
            0xFF00FF00,     // Lime
            0xFF32CD32,     // LimeGreen
            0xFFFAF0E6,     // Linen
            0xFFFF00FF,     // Magenta
            0xFF800000,     // Maroon
            0xFF66CDAA,     // MediumAquamarine
            0xFF0000CD,     // MediumBlue
            0xFFBA55D3,     // MediumOrchid
            0xFF9370DB,     // MediumPurple
            0xFF3CB371,     // MediumSeaGreen
            0xFF7B68EE,     // MediumSlateBlue
            0xFF00FA9A,     // MediumSpringGreen
            0xFF48D1CC,     // MediumTurquoise
            0xFFC71585,     // MediumVioletRed
            0xFF191970,     // MidnightBlue
            0xFFF5FFFA,     // MintCream
            0xFFFFE4E1,     // MistyRose
            0xFFFFE4B5,     // Moccasin
            0xFFFFDEAD,     // NavajoWhite
            0xFF000080,     // Navy
            0xFFFDF5E6,     // OldLace
            0xFF808000,     // Olive
            0xFF6B8E23,     // OliveDrab
            0xFFFFA500,     // Orange
            0xFFFF4500,     // OrangeRed
            0xFFDA70D6,     // Orchid
            0xFFEEE8AA,     // PaleGoldenrod
            0xFF98FB98,     // PaleGreen
            0xFFAFEEEE,     // PaleTurquoise
            0xFFDB7093,     // PaleVioletRed
            0xFFFFEFD5,     // PapayaWhip
            0xFFFFDAB9,     // PeachPuff
            0xFFCD853F,     // Peru
            0xFFFFC0CB,     // Pink
            0xFFDDA0DD,     // Plum
            0xFFB0E0E6,     // PowderBlue
            0xFF800080,     // Purple
            0xFFFF0000,     // Red
            0xFFBC8F8F,     // RosyBrown
            0xFF4169E1,     // RoyalBlue
            0xFF8B4513,     // SaddleBrown
            0xFFFA8072,     // Salmon
            0xFFF4A460,     // SandyBrown
            0xFF2E8B57,     // SeaGreen
            0xFFFFF5EE,     // SeaShell
            0xFFA0522D,     // Sienna
            0xFFC0C0C0,     // Silver
            0xFF87CEEB,     // SkyBlue
            0xFF6A5ACD,     // SlateBlue
            0xFF708090,     // SlateGray
            0xFFFFFAFA,     // Snow
            0xFF00FF7F,     // SpringGreen
            0xFF4682B4,     // SteelBlue
            0xFFD2B48C,     // Tan
            0xFF008080,     // Teal
            0xFFD8BFD8,     // Thistle
            0xFFFF6347,     // Tomato
            0xFF40E0D0,     // Turquoise
            0xFFEE82EE,     // Violet
            0xFFF5DEB3,     // Wheat
            0xFFFFFFFF,     // White
            0xFFF5F5F5,     // WhiteSmoke
            0xFFFFFF00,     // Yellow
            0xFF9ACD32,     // YellowGreen
        };

        internal static Color ArgbToKnownColor(uint argb)
        {
            // Should be fully opaque (and as such we can skip the first entry
            // which is transparent).
            Debug.Assert((argb & Color.ARGBAlphaMask) == Color.ARGBAlphaMask);

            for (int index = 1; index < s_colorTable.Length; ++index)
            {
                if (s_colorTable[index] == argb)
                {
                    return Color.FromKnownColor((KnownColor)(index + (int)KnownColor.Transparent));
                }
            }

            // Not a known color
            return Color.FromArgb((int)argb);
        }

        public static uint KnownColorToArgb(KnownColor color)
        {
            Debug.Assert(color > 0 && color <= KnownColor.MenuHighlight);

            return Color.IsKnownColorSystem(color)
                ? GetSystemColorArgb(color)
                : s_colorTable[(int)color - (int)KnownColor.Transparent];
        }

#if FEATURE_WINDOWS_SYSTEM_COLORS

        private static ReadOnlySpan<byte> SystemColorIdTable => new byte[]
        {
            // In order of definition in KnownColor enum

            // The original group of contiguous system KnownColors
            (byte)Interop.User32.Win32SystemColors.ActiveBorder,
            (byte)Interop.User32.Win32SystemColors.ActiveCaption,
            (byte)Interop.User32.Win32SystemColors.ActiveCaptionText,
            (byte)Interop.User32.Win32SystemColors.AppWorkspace,
            (byte)Interop.User32.Win32SystemColors.Control,
            (byte)Interop.User32.Win32SystemColors.ControlDark,
            (byte)Interop.User32.Win32SystemColors.ControlDarkDark,
            (byte)Interop.User32.Win32SystemColors.ControlLight,
            (byte)Interop.User32.Win32SystemColors.ControlLightLight,
            (byte)Interop.User32.Win32SystemColors.ControlText,
            (byte)Interop.User32.Win32SystemColors.Desktop,
            (byte)Interop.User32.Win32SystemColors.GrayText,
            (byte)Interop.User32.Win32SystemColors.Highlight,
            (byte)Interop.User32.Win32SystemColors.HighlightText,
            (byte)Interop.User32.Win32SystemColors.HotTrack,
            (byte)Interop.User32.Win32SystemColors.InactiveBorder,
            (byte)Interop.User32.Win32SystemColors.InactiveCaption,
            (byte)Interop.User32.Win32SystemColors.InactiveCaptionText,
            (byte)Interop.User32.Win32SystemColors.Info,
            (byte)Interop.User32.Win32SystemColors.InfoText,
            (byte)Interop.User32.Win32SystemColors.Menu,
            (byte)Interop.User32.Win32SystemColors.MenuText,
            (byte)Interop.User32.Win32SystemColors.ScrollBar,
            (byte)Interop.User32.Win32SystemColors.Window,
            (byte)Interop.User32.Win32SystemColors.WindowFrame,
            (byte)Interop.User32.Win32SystemColors.WindowText,

            // The appended group of SystemColors (i.e. not sequential with WindowText above)
            (byte)Interop.User32.Win32SystemColors.ButtonFace,
            (byte)Interop.User32.Win32SystemColors.ButtonHighlight,
            (byte)Interop.User32.Win32SystemColors.ButtonShadow,
            (byte)Interop.User32.Win32SystemColors.GradientActiveCaption,
            (byte)Interop.User32.Win32SystemColors.GradientInactiveCaption,
            (byte)Interop.User32.Win32SystemColors.MenuBar,
            (byte)Interop.User32.Win32SystemColors.MenuHighlight
        };

        public static uint GetSystemColorArgb(KnownColor color)
            => ColorTranslator.COLORREFToARGB(Interop.User32.GetSysColor(GetSystemColorId(color)));

        private static int GetSystemColorId(KnownColor color)
        {
            Debug.Assert(Color.IsKnownColorSystem(color));

            return color < KnownColor.Transparent
                ? SystemColorIdTable[(int)color - (int)KnownColor.ActiveBorder]
                : SystemColorIdTable[(int)color - (int)KnownColor.ButtonFace + (int)KnownColor.WindowText];
        }
#else
        private static readonly uint[] s_staticSystemColors = new uint[]
        {
            // Hard-coded constants, based on default Windows settings.
            // (In order of definition in KnownColor enum.)

            // First contiguous set.

            0xFFD4D0C8,     // ActiveBorder
            0xFF0054E3,     // ActiveCaption
            0xFFFFFFFF,     // ActiveCaptionText
            0xFF808080,     // AppWorkspace
            0xFFECE9D8,     // Control
            0xFFACA899,     // ControlDark
            0xFF716F64,     // ControlDarkDark
            0xFFF1EFE2,     // ControlLight
            0xFFFFFFFF,     // ControlLightLight
            0xFF000000,     // ControlText
            0xFF004E98,     // Desktop
            0xFFACA899,     // GrayText
            0xFF316AC5,     // Highlight
            0xFFFFFFFF,     // HighlightText
            0xFF000080,     // HotTrack
            0xFFD4D0C8,     // InactiveBorder
            0xFF7A96DF,     // InactiveCaption
            0xFFD8E4F8,     // InactiveCaptionText
            0xFFFFFFE1,     // Info
            0xFF000000,     // InfoText
            0xFFFFFFFF,     // Menu
            0xFF000000,     // MenuText
            0xFFD4D0C8,     // ScrollBar
            0xFFFFFFFF,     // Window
            0xFF000000,     // WindowFrame
            0xFF000000,     // WindowText

            // Second contiguous set.

            0xFFF0F0F0,     // ButtonFace
            0xFFFFFFFF,     // ButtonHighlight
            0xFFA0A0A0,     // ButtonShadow
            0xFFB9D1EA,     // GradientActiveCaption
            0xFFD7E4F2,     // GradientInactiveCaption
            0xFFF0F0F0,     // MenuBar
            0xFF3399FF,     // MenuHighlight
        };

        public static uint GetSystemColorArgb(KnownColor color)
        {
            Debug.Assert(Color.IsKnownColorSystem(color));

            return color < KnownColor.Transparent
                ? s_staticSystemColors[(int)color - (int)KnownColor.ActiveBorder]
                : s_staticSystemColors[(int)color - (int)KnownColor.ButtonFace + (int)KnownColor.WindowText];
        }
#endif
    }
}
