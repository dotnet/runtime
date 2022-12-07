// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;

namespace System.Drawing
{
    // Minimal color conversion functionality, without a dependency on TypeConverter itself.
    internal static class ColorConverterCommon
    {
        public static Color ConvertFromString(string strValue, CultureInfo culture)
        {
            Debug.Assert(culture != null);

            string text = strValue.Trim();

            if (text.Length == 0)
            {
                return Color.Empty;
            }

            {
                Color c;
                // First, check to see if this is a standard name.
                //
                if (ColorTable.TryGetNamedColor(text, out c))
                {
                    return c;
                }
            }

            char sep = culture.TextInfo.ListSeparator[0];

            // If the value is a 6 digit hex number only, then
            // we want to treat the Alpha as 255, not 0
            //
            if (!text.Contains(sep))
            {
                // text can be '' (empty quoted string)
                if (text.Length >= 2 && (text[0] == '\'' || text[0] == '"') && text[0] == text[text.Length - 1])
                {
                    // In quotes means a named value
                    string colorName = text.Substring(1, text.Length - 2);
                    return Color.FromName(colorName);
                }
                else if ((text.Length == 7 && text[0] == '#') ||
                         (text.Length == 8 && (text.StartsWith("0x") || text.StartsWith("0X"))) ||
                         (text.Length == 8 && (text.StartsWith("&h") || text.StartsWith("&H"))))
                {
                    // Note: int.Parse will raise exception if value cannot be converted.
                    return PossibleKnownColor(Color.FromArgb(unchecked((int)(0xFF000000 | (uint)IntFromString(text, culture)))));
                }
            }

            // We support 1, 3, or 4 arguments:
            // 1 -- full ARGB encoded
            // 3 -- RGB
            // 4 -- ARGB
            ReadOnlySpan<char> textSpan = text;
            Span<Range> tokens = stackalloc Range[5];
            return textSpan.Split(tokens, sep) switch
            {
                1 => PossibleKnownColor(Color.FromArgb(IntFromString(textSpan[tokens[0]], culture))),
                3 => PossibleKnownColor(Color.FromArgb(IntFromString(textSpan[tokens[0]], culture), IntFromString(textSpan[tokens[1]], culture), IntFromString(textSpan[tokens[2]], culture))),
                4 => PossibleKnownColor(Color.FromArgb(IntFromString(textSpan[tokens[0]], culture), IntFromString(textSpan[tokens[1]], culture), IntFromString(textSpan[tokens[2]], culture), IntFromString(textSpan[tokens[3]], culture))),
                _ => throw new ArgumentException(SR.Format(SR.InvalidColor, text)),
            };
        }

        private static Color PossibleKnownColor(Color color)
        {
            // Now check to see if this color matches one of our known colors.
            // If it does, then substitute it. We can only do this for "Colors"
            // because system colors morph with user settings.
            //
            int targetARGB = color.ToArgb();

            foreach (Color c in ColorTable.Colors.Values)
            {
                if (c.ToArgb() == targetARGB)
                {
                    return c;
                }
            }
            return color;
        }

        private static int IntFromString(ReadOnlySpan<char> text, CultureInfo culture)
        {
            text = text.Trim();

            try
            {
                if (text[0] == '#')
                {
                    return Convert.ToInt32(text.Slice(1).ToString(), 16);
                }
                else if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase) || text.StartsWith("&h", StringComparison.OrdinalIgnoreCase))
                {
                    return Convert.ToInt32(text.Slice(2).ToString(), 16);
                }
                else
                {
                    Debug.Assert(culture != null);
                    var formatInfo = (NumberFormatInfo?)culture.GetFormat(typeof(NumberFormatInfo));
                    return int.Parse(text, provider: formatInfo);
                }
            }
            catch (Exception e)
            {
                throw new ArgumentException(SR.Format(SR.ConvertInvalidPrimitive, text.ToString(), nameof(Int32)), e);
            }
        }
    }
}
