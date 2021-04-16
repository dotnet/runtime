// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Drawing
{
    [DebuggerDisplay("{NameAndARGBValue}")]
    [Editor("System.Drawing.Design.ColorEditor, System.Drawing.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
            "System.Drawing.Design.UITypeEditor, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    [Serializable]
    [TypeConverter("System.Drawing.ColorConverter, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    [TypeForwardedFrom("System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public readonly struct Color : IEquatable<Color>
    {
        public static readonly Color Empty;

        // -------------------------------------------------------------------
        //  static list of "web" colors...
        //
        public static Color Transparent => new Color(KnownColor.Transparent);

        public static Color AliceBlue => new Color(KnownColor.AliceBlue);

        public static Color AntiqueWhite => new Color(KnownColor.AntiqueWhite);

        public static Color Aqua => new Color(KnownColor.Aqua);

        public static Color Aquamarine => new Color(KnownColor.Aquamarine);

        public static Color Azure => new Color(KnownColor.Azure);

        public static Color Beige => new Color(KnownColor.Beige);

        public static Color Bisque => new Color(KnownColor.Bisque);

        public static Color Black => new Color(KnownColor.Black);

        public static Color BlanchedAlmond => new Color(KnownColor.BlanchedAlmond);

        public static Color Blue => new Color(KnownColor.Blue);

        public static Color BlueViolet => new Color(KnownColor.BlueViolet);

        public static Color Brown => new Color(KnownColor.Brown);

        public static Color BurlyWood => new Color(KnownColor.BurlyWood);

        public static Color CadetBlue => new Color(KnownColor.CadetBlue);

        public static Color Chartreuse => new Color(KnownColor.Chartreuse);

        public static Color Chocolate => new Color(KnownColor.Chocolate);

        public static Color Coral => new Color(KnownColor.Coral);

        public static Color CornflowerBlue => new Color(KnownColor.CornflowerBlue);

        public static Color Cornsilk => new Color(KnownColor.Cornsilk);

        public static Color Crimson => new Color(KnownColor.Crimson);

        public static Color Cyan => new Color(KnownColor.Cyan);

        public static Color DarkBlue => new Color(KnownColor.DarkBlue);

        public static Color DarkCyan => new Color(KnownColor.DarkCyan);

        public static Color DarkGoldenrod => new Color(KnownColor.DarkGoldenrod);

        public static Color DarkGray => new Color(KnownColor.DarkGray);

        public static Color DarkGreen => new Color(KnownColor.DarkGreen);

        public static Color DarkKhaki => new Color(KnownColor.DarkKhaki);

        public static Color DarkMagenta => new Color(KnownColor.DarkMagenta);

        public static Color DarkOliveGreen => new Color(KnownColor.DarkOliveGreen);

        public static Color DarkOrange => new Color(KnownColor.DarkOrange);

        public static Color DarkOrchid => new Color(KnownColor.DarkOrchid);

        public static Color DarkRed => new Color(KnownColor.DarkRed);

        public static Color DarkSalmon => new Color(KnownColor.DarkSalmon);

        public static Color DarkSeaGreen => new Color(KnownColor.DarkSeaGreen);

        public static Color DarkSlateBlue => new Color(KnownColor.DarkSlateBlue);

        public static Color DarkSlateGray => new Color(KnownColor.DarkSlateGray);

        public static Color DarkTurquoise => new Color(KnownColor.DarkTurquoise);

        public static Color DarkViolet => new Color(KnownColor.DarkViolet);

        public static Color DeepPink => new Color(KnownColor.DeepPink);

        public static Color DeepSkyBlue => new Color(KnownColor.DeepSkyBlue);

        public static Color DimGray => new Color(KnownColor.DimGray);

        public static Color DodgerBlue => new Color(KnownColor.DodgerBlue);

        public static Color Firebrick => new Color(KnownColor.Firebrick);

        public static Color FloralWhite => new Color(KnownColor.FloralWhite);

        public static Color ForestGreen => new Color(KnownColor.ForestGreen);

        public static Color Fuchsia => new Color(KnownColor.Fuchsia);

        public static Color Gainsboro => new Color(KnownColor.Gainsboro);

        public static Color GhostWhite => new Color(KnownColor.GhostWhite);

        public static Color Gold => new Color(KnownColor.Gold);

        public static Color Goldenrod => new Color(KnownColor.Goldenrod);

        public static Color Gray => new Color(KnownColor.Gray);

        public static Color Green => new Color(KnownColor.Green);

        public static Color GreenYellow => new Color(KnownColor.GreenYellow);

        public static Color Honeydew => new Color(KnownColor.Honeydew);

        public static Color HotPink => new Color(KnownColor.HotPink);

        public static Color IndianRed => new Color(KnownColor.IndianRed);

        public static Color Indigo => new Color(KnownColor.Indigo);

        public static Color Ivory => new Color(KnownColor.Ivory);

        public static Color Khaki => new Color(KnownColor.Khaki);

        public static Color Lavender => new Color(KnownColor.Lavender);

        public static Color LavenderBlush => new Color(KnownColor.LavenderBlush);

        public static Color LawnGreen => new Color(KnownColor.LawnGreen);

        public static Color LemonChiffon => new Color(KnownColor.LemonChiffon);

        public static Color LightBlue => new Color(KnownColor.LightBlue);

        public static Color LightCoral => new Color(KnownColor.LightCoral);

        public static Color LightCyan => new Color(KnownColor.LightCyan);

        public static Color LightGoldenrodYellow => new Color(KnownColor.LightGoldenrodYellow);

        public static Color LightGreen => new Color(KnownColor.LightGreen);

        public static Color LightGray => new Color(KnownColor.LightGray);

        public static Color LightPink => new Color(KnownColor.LightPink);

        public static Color LightSalmon => new Color(KnownColor.LightSalmon);

        public static Color LightSeaGreen => new Color(KnownColor.LightSeaGreen);

        public static Color LightSkyBlue => new Color(KnownColor.LightSkyBlue);

        public static Color LightSlateGray => new Color(KnownColor.LightSlateGray);

        public static Color LightSteelBlue => new Color(KnownColor.LightSteelBlue);

        public static Color LightYellow => new Color(KnownColor.LightYellow);

        public static Color Lime => new Color(KnownColor.Lime);

        public static Color LimeGreen => new Color(KnownColor.LimeGreen);

        public static Color Linen => new Color(KnownColor.Linen);

        public static Color Magenta => new Color(KnownColor.Magenta);

        public static Color Maroon => new Color(KnownColor.Maroon);

        public static Color MediumAquamarine => new Color(KnownColor.MediumAquamarine);

        public static Color MediumBlue => new Color(KnownColor.MediumBlue);

        public static Color MediumOrchid => new Color(KnownColor.MediumOrchid);

        public static Color MediumPurple => new Color(KnownColor.MediumPurple);

        public static Color MediumSeaGreen => new Color(KnownColor.MediumSeaGreen);

        public static Color MediumSlateBlue => new Color(KnownColor.MediumSlateBlue);

        public static Color MediumSpringGreen => new Color(KnownColor.MediumSpringGreen);

        public static Color MediumTurquoise => new Color(KnownColor.MediumTurquoise);

        public static Color MediumVioletRed => new Color(KnownColor.MediumVioletRed);

        public static Color MidnightBlue => new Color(KnownColor.MidnightBlue);

        public static Color MintCream => new Color(KnownColor.MintCream);

        public static Color MistyRose => new Color(KnownColor.MistyRose);

        public static Color Moccasin => new Color(KnownColor.Moccasin);

        public static Color NavajoWhite => new Color(KnownColor.NavajoWhite);

        public static Color Navy => new Color(KnownColor.Navy);

        public static Color OldLace => new Color(KnownColor.OldLace);

        public static Color Olive => new Color(KnownColor.Olive);

        public static Color OliveDrab => new Color(KnownColor.OliveDrab);

        public static Color Orange => new Color(KnownColor.Orange);

        public static Color OrangeRed => new Color(KnownColor.OrangeRed);

        public static Color Orchid => new Color(KnownColor.Orchid);

        public static Color PaleGoldenrod => new Color(KnownColor.PaleGoldenrod);

        public static Color PaleGreen => new Color(KnownColor.PaleGreen);

        public static Color PaleTurquoise => new Color(KnownColor.PaleTurquoise);

        public static Color PaleVioletRed => new Color(KnownColor.PaleVioletRed);

        public static Color PapayaWhip => new Color(KnownColor.PapayaWhip);

        public static Color PeachPuff => new Color(KnownColor.PeachPuff);

        public static Color Peru => new Color(KnownColor.Peru);

        public static Color Pink => new Color(KnownColor.Pink);

        public static Color Plum => new Color(KnownColor.Plum);

        public static Color PowderBlue => new Color(KnownColor.PowderBlue);

        public static Color Purple => new Color(KnownColor.Purple);

        public static Color RebeccaPurple => new Color(KnownColor.RebeccaPurple);

        public static Color Red => new Color(KnownColor.Red);

        public static Color RosyBrown => new Color(KnownColor.RosyBrown);

        public static Color RoyalBlue => new Color(KnownColor.RoyalBlue);

        public static Color SaddleBrown => new Color(KnownColor.SaddleBrown);

        public static Color Salmon => new Color(KnownColor.Salmon);

        public static Color SandyBrown => new Color(KnownColor.SandyBrown);

        public static Color SeaGreen => new Color(KnownColor.SeaGreen);

        public static Color SeaShell => new Color(KnownColor.SeaShell);

        public static Color Sienna => new Color(KnownColor.Sienna);

        public static Color Silver => new Color(KnownColor.Silver);

        public static Color SkyBlue => new Color(KnownColor.SkyBlue);

        public static Color SlateBlue => new Color(KnownColor.SlateBlue);

        public static Color SlateGray => new Color(KnownColor.SlateGray);

        public static Color Snow => new Color(KnownColor.Snow);

        public static Color SpringGreen => new Color(KnownColor.SpringGreen);

        public static Color SteelBlue => new Color(KnownColor.SteelBlue);

        public static Color Tan => new Color(KnownColor.Tan);

        public static Color Teal => new Color(KnownColor.Teal);

        public static Color Thistle => new Color(KnownColor.Thistle);

        public static Color Tomato => new Color(KnownColor.Tomato);

        public static Color Turquoise => new Color(KnownColor.Turquoise);

        public static Color Violet => new Color(KnownColor.Violet);

        public static Color Wheat => new Color(KnownColor.Wheat);

        public static Color White => new Color(KnownColor.White);

        public static Color WhiteSmoke => new Color(KnownColor.WhiteSmoke);

        public static Color Yellow => new Color(KnownColor.Yellow);

        public static Color YellowGreen => new Color(KnownColor.YellowGreen);
        //
        //  end "web" colors
        // -------------------------------------------------------------------

        // NOTE : The "zero" pattern (all members being 0) must represent
        //      : "not set". This allows "Color c;" to be correct.

        private const short StateKnownColorValid = 0x0001;
        private const short StateARGBValueValid = 0x0002;
        private const short StateValueMask = StateARGBValueValid;
        private const short StateNameValid = 0x0008;
        private const long NotDefinedValue = 0;

        // Shift counts and bit masks for A, R, G, B components in ARGB mode

        internal const int ARGBAlphaShift = 24;
        internal const int ARGBRedShift = 16;
        internal const int ARGBGreenShift = 8;
        internal const int ARGBBlueShift = 0;
        internal const uint ARGBAlphaMask = 0xFFu << ARGBAlphaShift;
        internal const uint ARGBRedMask = 0xFFu << ARGBRedShift;
        internal const uint ARGBGreenMask = 0xFFu << ARGBGreenShift;
        internal const uint ARGBBlueMask = 0xFFu << ARGBBlueShift;

        // User supplied name of color. Will not be filled in if
        // we map to a "knowncolor"
        private readonly string? name; // Do not rename (binary serialization)

        // Standard 32bit sRGB (ARGB)
        private readonly long value; // Do not rename (binary serialization)

        // Ignored, unless "state" says it is valid
        private readonly short knownColor; // Do not rename (binary serialization)

        // State flags.
        private readonly short state; // Do not rename (binary serialization)

        internal Color(KnownColor knownColor)
        {
            value = 0;
            state = StateKnownColorValid;
            name = null;
            this.knownColor = unchecked((short)knownColor);
        }

        private Color(long value, short state, string? name, KnownColor knownColor)
        {
            this.value = value;
            this.state = state;
            this.name = name;
            this.knownColor = unchecked((short)knownColor);
        }

        public byte R => unchecked((byte)(Value >> ARGBRedShift));

        public byte G => unchecked((byte)(Value >> ARGBGreenShift));

        public byte B => unchecked((byte)(Value >> ARGBBlueShift));

        public byte A => unchecked((byte)(Value >> ARGBAlphaShift));

        public bool IsKnownColor => (state & StateKnownColorValid) != 0;

        public bool IsEmpty => state == 0;

        public bool IsNamedColor => ((state & StateNameValid) != 0) || IsKnownColor;

        public bool IsSystemColor => IsKnownColor && IsKnownColorSystem((KnownColor)knownColor);

        internal static bool IsKnownColorSystem(KnownColor knownColor)
            => KnownColorTable.s_colorKindTable[(int)knownColor] == KnownColorTable.KnownColorKindSystem;

        // Used for the [DebuggerDisplay]. Inlining in the attribute is possible, but
        // against best practices as the current project language parses the string with
        // language specific heuristics.

        private string NameAndARGBValue => $"{{Name={Name}, ARGB=({A}, {R}, {G}, {B})}}";

        public string Name
        {
            get
            {
                if ((state & StateNameValid) != 0)
                {
                    Debug.Assert(name != null);
                    return name;
                }

                if (IsKnownColor)
                {
                    string tablename = KnownColorNames.KnownColorToName((KnownColor)knownColor);
                    Debug.Assert(tablename != null, $"Could not find known color '{(KnownColor)knownColor}' in the KnownColorTable");

                    return tablename;
                }

                // if we reached here, just encode the value
                //
                return Convert.ToString(value, 16);
            }
        }

        private long Value
        {
            get
            {
                if ((state & StateValueMask) != 0)
                {
                    return value;
                }

                // This is the only place we have system colors value exposed
                if (IsKnownColor)
                {
                    return KnownColorTable.KnownColorToArgb((KnownColor)knownColor);
                }

                return NotDefinedValue;
            }
        }

        private static void CheckByte(int value, string name)
        {
            static void ThrowOutOfByteRange(int v, string n) =>
                throw new ArgumentException(SR.Format(SR.InvalidEx2BoundArgument, n, v, byte.MinValue, byte.MaxValue));

            if (unchecked((uint)value) > byte.MaxValue)
                ThrowOutOfByteRange(value, name);
        }

        private static Color FromArgb(uint argb) => new Color(argb, StateARGBValueValid, null, (KnownColor)0);

        public static Color FromArgb(int argb) => FromArgb(unchecked((uint)argb));

        public static Color FromArgb(int alpha, int red, int green, int blue)
        {
            CheckByte(alpha, nameof(alpha));
            CheckByte(red, nameof(red));
            CheckByte(green, nameof(green));
            CheckByte(blue, nameof(blue));

            return FromArgb(
                (uint)alpha << ARGBAlphaShift |
                (uint)red << ARGBRedShift |
                (uint)green << ARGBGreenShift |
                (uint)blue << ARGBBlueShift
            );
        }

        public static Color FromArgb(int alpha, Color baseColor)
        {
            CheckByte(alpha, nameof(alpha));

            return FromArgb(
                (uint)alpha << ARGBAlphaShift |
                (uint)baseColor.Value & ~ARGBAlphaMask
            );
        }

        public static Color FromArgb(int red, int green, int blue) => FromArgb(byte.MaxValue, red, green, blue);

        public static Color FromKnownColor(KnownColor color) =>
            color <= 0 || color > KnownColor.RebeccaPurple ? FromName(color.ToString()) : new Color(color);

        public static Color FromName(string name)
        {
            // try to get a known color first
            if (ColorTable.TryGetNamedColor(name, out Color color))
                return color;

            // otherwise treat it as a named color
            return new Color(NotDefinedValue, StateNameValid, name, (KnownColor)0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void GetRgbValues(out int r, out int g, out int b)
        {
            uint value = (uint)Value;
            r = (int)(value & ARGBRedMask) >> ARGBRedShift;
            g = (int)(value & ARGBGreenMask) >> ARGBGreenShift;
            b = (int)(value & ARGBBlueMask) >> ARGBBlueShift;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void MinMaxRgb(out int min, out int max, int r, int g, int b)
        {
            if (r > g)
            {
                max = r;
                min = g;
            }
            else
            {
                max = g;
                min = r;
            }
            if (b > max)
            {
                max = b;
            }
            else if (b < min)
            {
                min = b;
            }
        }

        public float GetBrightness()
        {
            GetRgbValues(out int r, out int g, out int b);

            MinMaxRgb(out int min, out int max, r, g, b);

            return (max + min) / (byte.MaxValue * 2f);
        }

        public float GetHue()
        {
            GetRgbValues(out int r, out int g, out int b);

            if (r == g && g == b)
                return 0f;

            MinMaxRgb(out int min, out int max, r, g, b);

            float delta = max - min;
            float hue;

            if (r == max)
                hue = (g - b) / delta;
            else if (g == max)
                hue = (b - r) / delta + 2f;
            else
                hue = (r - g) / delta + 4f;

            hue *= 60f;
            if (hue < 0f)
                hue += 360f;

            return hue;
        }

        public float GetSaturation()
        {
            GetRgbValues(out int r, out int g, out int b);

            if (r == g && g == b)
                return 0f;

            MinMaxRgb(out int min, out int max, r, g, b);

            int div = max + min;
            if (div > byte.MaxValue)
                div = byte.MaxValue * 2 - max - min;

            return (max - min) / (float)div;
        }

        public int ToArgb() => unchecked((int)Value);

        public KnownColor ToKnownColor() => (KnownColor)knownColor;

        public override string ToString()
        {
            if (IsNamedColor)
            {
                return nameof(Color) + " [" + Name + "]";
            }
            else if ((state & StateValueMask) != 0)
            {
                return nameof(Color) + " [A=" + A.ToString() + ", R=" + R.ToString() + ", G=" + G.ToString() + ", B=" + B.ToString() + "]";
            }
            else
            {
                return nameof(Color) + " [Empty]";
            }
        }

        public static bool operator ==(Color left, Color right) =>
            left.value == right.value
                && left.state == right.state
                && left.knownColor == right.knownColor
                && left.name == right.name;

        public static bool operator !=(Color left, Color right) => !(left == right);

        public override bool Equals([NotNullWhen(true)] object? obj) => obj is Color other && Equals(other);

        public bool Equals(Color other) => this == other;

        public override int GetHashCode()
        {
            // Three cases:
            // 1. We don't have a name. All relevant data, including this fact, is in the remaining fields.
            // 2. We have a known name. The name will be the same instance of any other with the same
            // knownColor value, so we can ignore it for hashing. Note this also hashes different to
            // an unnamed color with the same ARGB value.
            // 3. Have an unknown name. Will differ from other unknown-named colors only by name, so we
            // can usefully use the names hash code alone.
            if (name != null && !IsKnownColor)
                return name.GetHashCode();

            return HashCode.Combine(value.GetHashCode(), state.GetHashCode(), knownColor.GetHashCode());
        }
    }
}
