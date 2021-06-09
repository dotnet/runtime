// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Drawing
{
    [System.ComponentModel.EditorAttribute("System.Drawing.Design.ColorEditor, System.Drawing.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", "System.Drawing.Design.UITypeEditor, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    [System.ComponentModel.TypeConverterAttribute("System.Drawing.ColorConverter, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public readonly partial struct Color : System.IEquatable<System.Drawing.Color>
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public static readonly System.Drawing.Color Empty;
        public byte A { get { throw null; } }
        public static System.Drawing.Color AliceBlue { get { throw null; } }
        public static System.Drawing.Color AntiqueWhite { get { throw null; } }
        public static System.Drawing.Color Aqua { get { throw null; } }
        public static System.Drawing.Color Aquamarine { get { throw null; } }
        public static System.Drawing.Color Azure { get { throw null; } }
        public byte B { get { throw null; } }
        public static System.Drawing.Color Beige { get { throw null; } }
        public static System.Drawing.Color Bisque { get { throw null; } }
        public static System.Drawing.Color Black { get { throw null; } }
        public static System.Drawing.Color BlanchedAlmond { get { throw null; } }
        public static System.Drawing.Color Blue { get { throw null; } }
        public static System.Drawing.Color BlueViolet { get { throw null; } }
        public static System.Drawing.Color Brown { get { throw null; } }
        public static System.Drawing.Color BurlyWood { get { throw null; } }
        public static System.Drawing.Color CadetBlue { get { throw null; } }
        public static System.Drawing.Color Chartreuse { get { throw null; } }
        public static System.Drawing.Color Chocolate { get { throw null; } }
        public static System.Drawing.Color Coral { get { throw null; } }
        public static System.Drawing.Color CornflowerBlue { get { throw null; } }
        public static System.Drawing.Color Cornsilk { get { throw null; } }
        public static System.Drawing.Color Crimson { get { throw null; } }
        public static System.Drawing.Color Cyan { get { throw null; } }
        public static System.Drawing.Color DarkBlue { get { throw null; } }
        public static System.Drawing.Color DarkCyan { get { throw null; } }
        public static System.Drawing.Color DarkGoldenrod { get { throw null; } }
        public static System.Drawing.Color DarkGray { get { throw null; } }
        public static System.Drawing.Color DarkGreen { get { throw null; } }
        public static System.Drawing.Color DarkKhaki { get { throw null; } }
        public static System.Drawing.Color DarkMagenta { get { throw null; } }
        public static System.Drawing.Color DarkOliveGreen { get { throw null; } }
        public static System.Drawing.Color DarkOrange { get { throw null; } }
        public static System.Drawing.Color DarkOrchid { get { throw null; } }
        public static System.Drawing.Color DarkRed { get { throw null; } }
        public static System.Drawing.Color DarkSalmon { get { throw null; } }
        public static System.Drawing.Color DarkSeaGreen { get { throw null; } }
        public static System.Drawing.Color DarkSlateBlue { get { throw null; } }
        public static System.Drawing.Color DarkSlateGray { get { throw null; } }
        public static System.Drawing.Color DarkTurquoise { get { throw null; } }
        public static System.Drawing.Color DarkViolet { get { throw null; } }
        public static System.Drawing.Color DeepPink { get { throw null; } }
        public static System.Drawing.Color DeepSkyBlue { get { throw null; } }
        public static System.Drawing.Color DimGray { get { throw null; } }
        public static System.Drawing.Color DodgerBlue { get { throw null; } }
        public static System.Drawing.Color Firebrick { get { throw null; } }
        public static System.Drawing.Color FloralWhite { get { throw null; } }
        public static System.Drawing.Color ForestGreen { get { throw null; } }
        public static System.Drawing.Color Fuchsia { get { throw null; } }
        public byte G { get { throw null; } }
        public static System.Drawing.Color Gainsboro { get { throw null; } }
        public static System.Drawing.Color GhostWhite { get { throw null; } }
        public static System.Drawing.Color Gold { get { throw null; } }
        public static System.Drawing.Color Goldenrod { get { throw null; } }
        public static System.Drawing.Color Gray { get { throw null; } }
        public static System.Drawing.Color Green { get { throw null; } }
        public static System.Drawing.Color GreenYellow { get { throw null; } }
        public static System.Drawing.Color Honeydew { get { throw null; } }
        public static System.Drawing.Color HotPink { get { throw null; } }
        public static System.Drawing.Color IndianRed { get { throw null; } }
        public static System.Drawing.Color Indigo { get { throw null; } }
        public bool IsEmpty { get { throw null; } }
        public bool IsKnownColor { get { throw null; } }
        public bool IsNamedColor { get { throw null; } }
        public bool IsSystemColor { get { throw null; } }
        public static System.Drawing.Color Ivory { get { throw null; } }
        public static System.Drawing.Color Khaki { get { throw null; } }
        public static System.Drawing.Color Lavender { get { throw null; } }
        public static System.Drawing.Color LavenderBlush { get { throw null; } }
        public static System.Drawing.Color LawnGreen { get { throw null; } }
        public static System.Drawing.Color LemonChiffon { get { throw null; } }
        public static System.Drawing.Color LightBlue { get { throw null; } }
        public static System.Drawing.Color LightCoral { get { throw null; } }
        public static System.Drawing.Color LightCyan { get { throw null; } }
        public static System.Drawing.Color LightGoldenrodYellow { get { throw null; } }
        public static System.Drawing.Color LightGray { get { throw null; } }
        public static System.Drawing.Color LightGreen { get { throw null; } }
        public static System.Drawing.Color LightPink { get { throw null; } }
        public static System.Drawing.Color LightSalmon { get { throw null; } }
        public static System.Drawing.Color LightSeaGreen { get { throw null; } }
        public static System.Drawing.Color LightSkyBlue { get { throw null; } }
        public static System.Drawing.Color LightSlateGray { get { throw null; } }
        public static System.Drawing.Color LightSteelBlue { get { throw null; } }
        public static System.Drawing.Color LightYellow { get { throw null; } }
        public static System.Drawing.Color Lime { get { throw null; } }
        public static System.Drawing.Color LimeGreen { get { throw null; } }
        public static System.Drawing.Color Linen { get { throw null; } }
        public static System.Drawing.Color Magenta { get { throw null; } }
        public static System.Drawing.Color Maroon { get { throw null; } }
        public static System.Drawing.Color MediumAquamarine { get { throw null; } }
        public static System.Drawing.Color MediumBlue { get { throw null; } }
        public static System.Drawing.Color MediumOrchid { get { throw null; } }
        public static System.Drawing.Color MediumPurple { get { throw null; } }
        public static System.Drawing.Color MediumSeaGreen { get { throw null; } }
        public static System.Drawing.Color MediumSlateBlue { get { throw null; } }
        public static System.Drawing.Color MediumSpringGreen { get { throw null; } }
        public static System.Drawing.Color MediumTurquoise { get { throw null; } }
        public static System.Drawing.Color MediumVioletRed { get { throw null; } }
        public static System.Drawing.Color MidnightBlue { get { throw null; } }
        public static System.Drawing.Color MintCream { get { throw null; } }
        public static System.Drawing.Color MistyRose { get { throw null; } }
        public static System.Drawing.Color Moccasin { get { throw null; } }
        public string Name { get { throw null; } }
        public static System.Drawing.Color NavajoWhite { get { throw null; } }
        public static System.Drawing.Color Navy { get { throw null; } }
        public static System.Drawing.Color OldLace { get { throw null; } }
        public static System.Drawing.Color Olive { get { throw null; } }
        public static System.Drawing.Color OliveDrab { get { throw null; } }
        public static System.Drawing.Color Orange { get { throw null; } }
        public static System.Drawing.Color OrangeRed { get { throw null; } }
        public static System.Drawing.Color Orchid { get { throw null; } }
        public static System.Drawing.Color PaleGoldenrod { get { throw null; } }
        public static System.Drawing.Color PaleGreen { get { throw null; } }
        public static System.Drawing.Color PaleTurquoise { get { throw null; } }
        public static System.Drawing.Color PaleVioletRed { get { throw null; } }
        public static System.Drawing.Color PapayaWhip { get { throw null; } }
        public static System.Drawing.Color PeachPuff { get { throw null; } }
        public static System.Drawing.Color Peru { get { throw null; } }
        public static System.Drawing.Color Pink { get { throw null; } }
        public static System.Drawing.Color Plum { get { throw null; } }
        public static System.Drawing.Color PowderBlue { get { throw null; } }
        public static System.Drawing.Color Purple { get { throw null; } }
        public byte R { get { throw null; } }
        public static System.Drawing.Color RebeccaPurple { get { throw null; } }
        public static System.Drawing.Color Red { get { throw null; } }
        public static System.Drawing.Color RosyBrown { get { throw null; } }
        public static System.Drawing.Color RoyalBlue { get { throw null; } }
        public static System.Drawing.Color SaddleBrown { get { throw null; } }
        public static System.Drawing.Color Salmon { get { throw null; } }
        public static System.Drawing.Color SandyBrown { get { throw null; } }
        public static System.Drawing.Color SeaGreen { get { throw null; } }
        public static System.Drawing.Color SeaShell { get { throw null; } }
        public static System.Drawing.Color Sienna { get { throw null; } }
        public static System.Drawing.Color Silver { get { throw null; } }
        public static System.Drawing.Color SkyBlue { get { throw null; } }
        public static System.Drawing.Color SlateBlue { get { throw null; } }
        public static System.Drawing.Color SlateGray { get { throw null; } }
        public static System.Drawing.Color Snow { get { throw null; } }
        public static System.Drawing.Color SpringGreen { get { throw null; } }
        public static System.Drawing.Color SteelBlue { get { throw null; } }
        public static System.Drawing.Color Tan { get { throw null; } }
        public static System.Drawing.Color Teal { get { throw null; } }
        public static System.Drawing.Color Thistle { get { throw null; } }
        public static System.Drawing.Color Tomato { get { throw null; } }
        public static System.Drawing.Color Transparent { get { throw null; } }
        public static System.Drawing.Color Turquoise { get { throw null; } }
        public static System.Drawing.Color Violet { get { throw null; } }
        public static System.Drawing.Color Wheat { get { throw null; } }
        public static System.Drawing.Color White { get { throw null; } }
        public static System.Drawing.Color WhiteSmoke { get { throw null; } }
        public static System.Drawing.Color Yellow { get { throw null; } }
        public static System.Drawing.Color YellowGreen { get { throw null; } }
        public bool Equals(System.Drawing.Color other) { throw null; }
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] object? obj) { throw null; }
        public static System.Drawing.Color FromArgb(int argb) { throw null; }
        public static System.Drawing.Color FromArgb(int alpha, System.Drawing.Color baseColor) { throw null; }
        public static System.Drawing.Color FromArgb(int red, int green, int blue) { throw null; }
        public static System.Drawing.Color FromArgb(int alpha, int red, int green, int blue) { throw null; }
        public static System.Drawing.Color FromKnownColor(System.Drawing.KnownColor color) { throw null; }
        public static System.Drawing.Color FromName(string name) { throw null; }
        public float GetBrightness() { throw null; }
        public override int GetHashCode() { throw null; }
        public float GetHue() { throw null; }
        public float GetSaturation() { throw null; }
        public static bool operator ==(System.Drawing.Color left, System.Drawing.Color right) { throw null; }
        public static bool operator !=(System.Drawing.Color left, System.Drawing.Color right) { throw null; }
        public int ToArgb() { throw null; }
        public System.Drawing.KnownColor ToKnownColor() { throw null; }
        public override string ToString() { throw null; }
    }
    public static partial class ColorTranslator
    {
        public static System.Drawing.Color FromHtml(string htmlColor) { throw null; }
        public static System.Drawing.Color FromOle(int oleColor) { throw null; }
        public static System.Drawing.Color FromWin32(int win32Color) { throw null; }
        public static string ToHtml(System.Drawing.Color c) { throw null; }
        public static int ToOle(System.Drawing.Color c) { throw null; }
        public static int ToWin32(System.Drawing.Color c) { throw null; }
    }
    public enum KnownColor
    {
        ActiveBorder = 1,
        ActiveCaption = 2,
        ActiveCaptionText = 3,
        AppWorkspace = 4,
        Control = 5,
        ControlDark = 6,
        ControlDarkDark = 7,
        ControlLight = 8,
        ControlLightLight = 9,
        ControlText = 10,
        Desktop = 11,
        GrayText = 12,
        Highlight = 13,
        HighlightText = 14,
        HotTrack = 15,
        InactiveBorder = 16,
        InactiveCaption = 17,
        InactiveCaptionText = 18,
        Info = 19,
        InfoText = 20,
        Menu = 21,
        MenuText = 22,
        ScrollBar = 23,
        Window = 24,
        WindowFrame = 25,
        WindowText = 26,
        Transparent = 27,
        AliceBlue = 28,
        AntiqueWhite = 29,
        Aqua = 30,
        Aquamarine = 31,
        Azure = 32,
        Beige = 33,
        Bisque = 34,
        Black = 35,
        BlanchedAlmond = 36,
        Blue = 37,
        BlueViolet = 38,
        Brown = 39,
        BurlyWood = 40,
        CadetBlue = 41,
        Chartreuse = 42,
        Chocolate = 43,
        Coral = 44,
        CornflowerBlue = 45,
        Cornsilk = 46,
        Crimson = 47,
        Cyan = 48,
        DarkBlue = 49,
        DarkCyan = 50,
        DarkGoldenrod = 51,
        DarkGray = 52,
        DarkGreen = 53,
        DarkKhaki = 54,
        DarkMagenta = 55,
        DarkOliveGreen = 56,
        DarkOrange = 57,
        DarkOrchid = 58,
        DarkRed = 59,
        DarkSalmon = 60,
        DarkSeaGreen = 61,
        DarkSlateBlue = 62,
        DarkSlateGray = 63,
        DarkTurquoise = 64,
        DarkViolet = 65,
        DeepPink = 66,
        DeepSkyBlue = 67,
        DimGray = 68,
        DodgerBlue = 69,
        Firebrick = 70,
        FloralWhite = 71,
        ForestGreen = 72,
        Fuchsia = 73,
        Gainsboro = 74,
        GhostWhite = 75,
        Gold = 76,
        Goldenrod = 77,
        Gray = 78,
        Green = 79,
        GreenYellow = 80,
        Honeydew = 81,
        HotPink = 82,
        IndianRed = 83,
        Indigo = 84,
        Ivory = 85,
        Khaki = 86,
        Lavender = 87,
        LavenderBlush = 88,
        LawnGreen = 89,
        LemonChiffon = 90,
        LightBlue = 91,
        LightCoral = 92,
        LightCyan = 93,
        LightGoldenrodYellow = 94,
        LightGray = 95,
        LightGreen = 96,
        LightPink = 97,
        LightSalmon = 98,
        LightSeaGreen = 99,
        LightSkyBlue = 100,
        LightSlateGray = 101,
        LightSteelBlue = 102,
        LightYellow = 103,
        Lime = 104,
        LimeGreen = 105,
        Linen = 106,
        Magenta = 107,
        Maroon = 108,
        MediumAquamarine = 109,
        MediumBlue = 110,
        MediumOrchid = 111,
        MediumPurple = 112,
        MediumSeaGreen = 113,
        MediumSlateBlue = 114,
        MediumSpringGreen = 115,
        MediumTurquoise = 116,
        MediumVioletRed = 117,
        MidnightBlue = 118,
        MintCream = 119,
        MistyRose = 120,
        Moccasin = 121,
        NavajoWhite = 122,
        Navy = 123,
        OldLace = 124,
        Olive = 125,
        OliveDrab = 126,
        Orange = 127,
        OrangeRed = 128,
        Orchid = 129,
        PaleGoldenrod = 130,
        PaleGreen = 131,
        PaleTurquoise = 132,
        PaleVioletRed = 133,
        PapayaWhip = 134,
        PeachPuff = 135,
        Peru = 136,
        Pink = 137,
        Plum = 138,
        PowderBlue = 139,
        Purple = 140,
        Red = 141,
        RosyBrown = 142,
        RoyalBlue = 143,
        SaddleBrown = 144,
        Salmon = 145,
        SandyBrown = 146,
        SeaGreen = 147,
        SeaShell = 148,
        Sienna = 149,
        Silver = 150,
        SkyBlue = 151,
        SlateBlue = 152,
        SlateGray = 153,
        Snow = 154,
        SpringGreen = 155,
        SteelBlue = 156,
        Tan = 157,
        Teal = 158,
        Thistle = 159,
        Tomato = 160,
        Turquoise = 161,
        Violet = 162,
        Wheat = 163,
        White = 164,
        WhiteSmoke = 165,
        Yellow = 166,
        YellowGreen = 167,
        ButtonFace = 168,
        ButtonHighlight = 169,
        ButtonShadow = 170,
        GradientActiveCaption = 171,
        GradientInactiveCaption = 172,
        MenuBar = 173,
        MenuHighlight = 174,
        RebeccaPurple = 175,
    }
    [System.ComponentModel.TypeConverterAttribute("System.Drawing.PointConverter, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public partial struct Point : System.IEquatable<System.Drawing.Point>
    {
        private int _dummyPrimitive;
        public static readonly System.Drawing.Point Empty;
        public Point(System.Drawing.Size sz) { throw null; }
        public Point(int dw) { throw null; }
        public Point(int x, int y) { throw null; }
        [System.ComponentModel.BrowsableAttribute(false)]
        public readonly bool IsEmpty { get { throw null; } }
        public int X { readonly get { throw null; } set { } }
        public int Y { readonly get { throw null; } set { } }
        public static System.Drawing.Point Add(System.Drawing.Point pt, System.Drawing.Size sz) { throw null; }
        public static System.Drawing.Point Ceiling(System.Drawing.PointF value) { throw null; }
        public readonly bool Equals(System.Drawing.Point other) { throw null; }
        public override readonly bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] object? obj) { throw null; }
        public override readonly int GetHashCode() { throw null; }
        public void Offset(System.Drawing.Point p) { }
        public void Offset(int dx, int dy) { }
        public static System.Drawing.Point operator +(System.Drawing.Point pt, System.Drawing.Size sz) { throw null; }
        public static bool operator ==(System.Drawing.Point left, System.Drawing.Point right) { throw null; }
        public static explicit operator System.Drawing.Size (System.Drawing.Point p) { throw null; }
        public static implicit operator System.Drawing.PointF (System.Drawing.Point p) { throw null; }
        public static bool operator !=(System.Drawing.Point left, System.Drawing.Point right) { throw null; }
        public static System.Drawing.Point operator -(System.Drawing.Point pt, System.Drawing.Size sz) { throw null; }
        public static System.Drawing.Point Round(System.Drawing.PointF value) { throw null; }
        public static System.Drawing.Point Subtract(System.Drawing.Point pt, System.Drawing.Size sz) { throw null; }
        public override readonly string ToString() { throw null; }
        public static System.Drawing.Point Truncate(System.Drawing.PointF value) { throw null; }
    }
    public partial struct PointF : System.IEquatable<System.Drawing.PointF>
    {
        private int _dummyPrimitive;
        public static readonly System.Drawing.PointF Empty;
        public PointF(float x, float y) { throw null; }
        public PointF(System.Numerics.Vector2 vector) { throw null; }
        [System.ComponentModel.BrowsableAttribute(false)]
        public readonly bool IsEmpty { get { throw null; } }
        public float X { readonly get { throw null; } set { } }
        public float Y { readonly get { throw null; } set { } }
        public static System.Drawing.PointF Add(System.Drawing.PointF pt, System.Drawing.Size sz) { throw null; }
        public static System.Drawing.PointF Add(System.Drawing.PointF pt, System.Drawing.SizeF sz) { throw null; }
        public readonly bool Equals(System.Drawing.PointF other) { throw null; }
        public override readonly bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] object? obj) { throw null; }
        public override readonly int GetHashCode() { throw null; }
        public static explicit operator System.Numerics.Vector2(System.Drawing.PointF point) { throw null; }
        public static explicit operator System.Drawing.PointF(System.Numerics.Vector2 vector) { throw null; }
        public static System.Drawing.PointF operator +(System.Drawing.PointF pt, System.Drawing.Size sz) { throw null; }
        public static System.Drawing.PointF operator +(System.Drawing.PointF pt, System.Drawing.SizeF sz) { throw null; }
        public static bool operator ==(System.Drawing.PointF left, System.Drawing.PointF right) { throw null; }
        public static bool operator !=(System.Drawing.PointF left, System.Drawing.PointF right) { throw null; }
        public static System.Drawing.PointF operator -(System.Drawing.PointF pt, System.Drawing.Size sz) { throw null; }
        public static System.Drawing.PointF operator -(System.Drawing.PointF pt, System.Drawing.SizeF sz) { throw null; }
        public static System.Drawing.PointF Subtract(System.Drawing.PointF pt, System.Drawing.Size sz) { throw null; }
        public static System.Drawing.PointF Subtract(System.Drawing.PointF pt, System.Drawing.SizeF sz) { throw null; }
        public override readonly string ToString() { throw null; }
        public System.Numerics.Vector2 ToVector2() { throw null; }
    }
    [System.ComponentModel.TypeConverterAttribute("System.Drawing.RectangleConverter, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public partial struct Rectangle : System.IEquatable<System.Drawing.Rectangle>
    {
        private int _dummyPrimitive;
        public static readonly System.Drawing.Rectangle Empty;
        public Rectangle(System.Drawing.Point location, System.Drawing.Size size) { throw null; }
        public Rectangle(int x, int y, int width, int height) { throw null; }
        [System.ComponentModel.BrowsableAttribute(false)]
        public readonly int Bottom { get { throw null; } }
        public int Height { readonly get { throw null; } set { } }
        [System.ComponentModel.BrowsableAttribute(false)]
        public readonly bool IsEmpty { get { throw null; } }
        [System.ComponentModel.BrowsableAttribute(false)]
        public readonly int Left { get { throw null; } }
        [System.ComponentModel.BrowsableAttribute(false)]
        public System.Drawing.Point Location { readonly get { throw null; } set { } }
        [System.ComponentModel.BrowsableAttribute(false)]
        public readonly int Right { get { throw null; } }
        [System.ComponentModel.BrowsableAttribute(false)]
        public System.Drawing.Size Size { readonly get { throw null; } set { } }
        [System.ComponentModel.BrowsableAttribute(false)]
        public readonly int Top { get { throw null; } }
        public int Width { readonly get { throw null; } set { } }
        public int X { readonly get { throw null; } set { } }
        public int Y { readonly get { throw null; } set { } }
        public static System.Drawing.Rectangle Ceiling(System.Drawing.RectangleF value) { throw null; }
        public readonly bool Contains(System.Drawing.Point pt) { throw null; }
        public readonly bool Contains(System.Drawing.Rectangle rect) { throw null; }
        public readonly bool Contains(int x, int y) { throw null; }
        public readonly bool Equals(System.Drawing.Rectangle other) { throw null; }
        public override readonly bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] object? obj) { throw null; }
        public static System.Drawing.Rectangle FromLTRB(int left, int top, int right, int bottom) { throw null; }
        public override readonly int GetHashCode() { throw null; }
        public static System.Drawing.Rectangle Inflate(System.Drawing.Rectangle rect, int x, int y) { throw null; }
        public void Inflate(System.Drawing.Size size) { }
        public void Inflate(int width, int height) { }
        public void Intersect(System.Drawing.Rectangle rect) { }
        public static System.Drawing.Rectangle Intersect(System.Drawing.Rectangle a, System.Drawing.Rectangle b) { throw null; }
        public readonly bool IntersectsWith(System.Drawing.Rectangle rect) { throw null; }
        public void Offset(System.Drawing.Point pos) { }
        public void Offset(int x, int y) { }
        public static bool operator ==(System.Drawing.Rectangle left, System.Drawing.Rectangle right) { throw null; }
        public static bool operator !=(System.Drawing.Rectangle left, System.Drawing.Rectangle right) { throw null; }
        public static System.Drawing.Rectangle Round(System.Drawing.RectangleF value) { throw null; }
        public override readonly string ToString() { throw null; }
        public static System.Drawing.Rectangle Truncate(System.Drawing.RectangleF value) { throw null; }
        public static System.Drawing.Rectangle Union(System.Drawing.Rectangle a, System.Drawing.Rectangle b) { throw null; }
    }
    public partial struct RectangleF : System.IEquatable<System.Drawing.RectangleF>
    {
        private int _dummyPrimitive;
        public static readonly System.Drawing.RectangleF Empty;
        public RectangleF(System.Drawing.PointF location, System.Drawing.SizeF size) { throw null; }
        public RectangleF(float x, float y, float width, float height) { throw null; }
        public RectangleF(System.Numerics.Vector4 vector) { throw null; }
        [System.ComponentModel.BrowsableAttribute(false)]
        public readonly float Bottom { get { throw null; } }
        public float Height { readonly get { throw null; } set { } }
        [System.ComponentModel.BrowsableAttribute(false)]
        public readonly bool IsEmpty { get { throw null; } }
        [System.ComponentModel.BrowsableAttribute(false)]
        public readonly float Left { get { throw null; } }
        [System.ComponentModel.BrowsableAttribute(false)]
        public System.Drawing.PointF Location { readonly get { throw null; } set { } }
        [System.ComponentModel.BrowsableAttribute(false)]
        public readonly float Right { get { throw null; } }
        [System.ComponentModel.BrowsableAttribute(false)]
        public System.Drawing.SizeF Size { readonly get { throw null; } set { } }
        [System.ComponentModel.BrowsableAttribute(false)]
        public readonly float Top { get { throw null; } }
        public float Width { readonly get { throw null; } set { } }
        public float X { readonly get { throw null; } set { } }
        public float Y { readonly get { throw null; } set { } }
        public readonly bool Contains(System.Drawing.PointF pt) { throw null; }
        public readonly bool Contains(System.Drawing.RectangleF rect) { throw null; }
        public readonly bool Contains(float x, float y) { throw null; }
        public readonly bool Equals(System.Drawing.RectangleF other) { throw null; }
        public override readonly bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] object? obj) { throw null; }
        public static System.Drawing.RectangleF FromLTRB(float left, float top, float right, float bottom) { throw null; }
        public override readonly int GetHashCode() { throw null; }
        public static System.Drawing.RectangleF Inflate(System.Drawing.RectangleF rect, float x, float y) { throw null; }
        public void Inflate(System.Drawing.SizeF size) { }
        public void Inflate(float x, float y) { }
        public void Intersect(System.Drawing.RectangleF rect) { }
        public static System.Drawing.RectangleF Intersect(System.Drawing.RectangleF a, System.Drawing.RectangleF b) { throw null; }
        public readonly bool IntersectsWith(System.Drawing.RectangleF rect) { throw null; }
        public void Offset(System.Drawing.PointF pos) { }
        public void Offset(float x, float y) { }
        public static explicit operator System.Numerics.Vector4(System.Drawing.RectangleF rectangle) { throw null; }
        public static explicit operator System.Drawing.RectangleF(System.Numerics.Vector4 vector) { throw null; }
        public static bool operator ==(System.Drawing.RectangleF left, System.Drawing.RectangleF right) { throw null; }
        public static implicit operator System.Drawing.RectangleF (System.Drawing.Rectangle r) { throw null; }
        public static bool operator !=(System.Drawing.RectangleF left, System.Drawing.RectangleF right) { throw null; }
        public override readonly string ToString() { throw null; }
        public System.Numerics.Vector4 ToVector4() { throw null; }
        public static System.Drawing.RectangleF Union(System.Drawing.RectangleF a, System.Drawing.RectangleF b) { throw null; }
    }
    [System.ComponentModel.TypeConverterAttribute("System.Drawing.SizeConverter, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public partial struct Size : System.IEquatable<System.Drawing.Size>
    {
        private int _dummyPrimitive;
        public static readonly System.Drawing.Size Empty;
        public Size(System.Drawing.Point pt) { throw null; }
        public Size(int width, int height) { throw null; }
        public int Height { readonly get { throw null; } set { } }
        [System.ComponentModel.BrowsableAttribute(false)]
        public readonly bool IsEmpty { get { throw null; } }
        public int Width { readonly get { throw null; } set { } }
        public static System.Drawing.Size Add(System.Drawing.Size sz1, System.Drawing.Size sz2) { throw null; }
        public static System.Drawing.Size Ceiling(System.Drawing.SizeF value) { throw null; }
        public readonly bool Equals(System.Drawing.Size other) { throw null; }
        public override readonly bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] object? obj) { throw null; }
        public override readonly int GetHashCode() { throw null; }
        public static System.Drawing.Size operator +(System.Drawing.Size sz1, System.Drawing.Size sz2) { throw null; }
        public static System.Drawing.Size operator /(System.Drawing.Size left, int right) { throw null; }
        public static System.Drawing.SizeF operator /(System.Drawing.Size left, float right) { throw null; }
        public static bool operator ==(System.Drawing.Size sz1, System.Drawing.Size sz2) { throw null; }
        public static explicit operator System.Drawing.Point (System.Drawing.Size size) { throw null; }
        public static implicit operator System.Drawing.SizeF (System.Drawing.Size p) { throw null; }
        public static bool operator !=(System.Drawing.Size sz1, System.Drawing.Size sz2) { throw null; }
        public static System.Drawing.Size operator *(System.Drawing.Size left, int right) { throw null; }
        public static System.Drawing.SizeF operator *(System.Drawing.Size left, float right) { throw null; }
        public static System.Drawing.Size operator *(int left, System.Drawing.Size right) { throw null; }
        public static System.Drawing.SizeF operator *(float left, System.Drawing.Size right) { throw null; }
        public static System.Drawing.Size operator -(System.Drawing.Size sz1, System.Drawing.Size sz2) { throw null; }
        public static System.Drawing.Size Round(System.Drawing.SizeF value) { throw null; }
        public static System.Drawing.Size Subtract(System.Drawing.Size sz1, System.Drawing.Size sz2) { throw null; }
        public override readonly string ToString() { throw null; }
        public static System.Drawing.Size Truncate(System.Drawing.SizeF value) { throw null; }
    }
    [System.ComponentModel.TypeConverterAttribute("System.Drawing.SizeFConverter, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public partial struct SizeF : System.IEquatable<System.Drawing.SizeF>
    {
        private int _dummyPrimitive;
        public static readonly System.Drawing.SizeF Empty;
        public SizeF(System.Drawing.PointF pt) { throw null; }
        public SizeF(System.Drawing.SizeF size) { throw null; }
        public SizeF(float width, float height) { throw null; }
        public SizeF(System.Numerics.Vector2 vector) { throw null; }
        public float Height { readonly get { throw null; } set { } }
        [System.ComponentModel.BrowsableAttribute(false)]
        public readonly bool IsEmpty { get { throw null; } }
        public float Width { readonly get { throw null; } set { } }
        public static System.Drawing.SizeF Add(System.Drawing.SizeF sz1, System.Drawing.SizeF sz2) { throw null; }
        public readonly bool Equals(System.Drawing.SizeF other) { throw null; }
        public override readonly bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] object? obj) { throw null; }
        public override readonly int GetHashCode() { throw null; }
        public static explicit operator System.Numerics.Vector2(System.Drawing.SizeF size) { throw null; }
        public static explicit operator System.Drawing.SizeF(System.Numerics.Vector2 vector) { throw null; }
        public static System.Drawing.SizeF operator +(System.Drawing.SizeF sz1, System.Drawing.SizeF sz2) { throw null; }
        public static System.Drawing.SizeF operator /(System.Drawing.SizeF left, float right) { throw null; }
        public static bool operator ==(System.Drawing.SizeF sz1, System.Drawing.SizeF sz2) { throw null; }
        public static explicit operator System.Drawing.PointF (System.Drawing.SizeF size) { throw null; }
        public static bool operator !=(System.Drawing.SizeF sz1, System.Drawing.SizeF sz2) { throw null; }
        public static System.Drawing.SizeF operator *(System.Drawing.SizeF left, float right) { throw null; }
        public static System.Drawing.SizeF operator *(float left, System.Drawing.SizeF right) { throw null; }
        public static System.Drawing.SizeF operator -(System.Drawing.SizeF sz1, System.Drawing.SizeF sz2) { throw null; }
        public static System.Drawing.SizeF Subtract(System.Drawing.SizeF sz1, System.Drawing.SizeF sz2) { throw null; }
        public readonly System.Drawing.PointF ToPointF() { throw null; }
        public readonly System.Drawing.Size ToSize() { throw null; }
        public override readonly string ToString() { throw null; }
        public System.Numerics.Vector2 ToVector2() { throw null; }
    }
    public static partial class SystemColors
    {
        public static System.Drawing.Color ActiveBorder { get { throw null; } }
        public static System.Drawing.Color ActiveCaption { get { throw null; } }
        public static System.Drawing.Color ActiveCaptionText { get { throw null; } }
        public static System.Drawing.Color AppWorkspace { get { throw null; } }
        public static System.Drawing.Color ButtonFace { get { throw null; } }
        public static System.Drawing.Color ButtonHighlight { get { throw null; } }
        public static System.Drawing.Color ButtonShadow { get { throw null; } }
        public static System.Drawing.Color Control { get { throw null; } }
        public static System.Drawing.Color ControlDark { get { throw null; } }
        public static System.Drawing.Color ControlDarkDark { get { throw null; } }
        public static System.Drawing.Color ControlLight { get { throw null; } }
        public static System.Drawing.Color ControlLightLight { get { throw null; } }
        public static System.Drawing.Color ControlText { get { throw null; } }
        public static System.Drawing.Color Desktop { get { throw null; } }
        public static System.Drawing.Color GradientActiveCaption { get { throw null; } }
        public static System.Drawing.Color GradientInactiveCaption { get { throw null; } }
        public static System.Drawing.Color GrayText { get { throw null; } }
        public static System.Drawing.Color Highlight { get { throw null; } }
        public static System.Drawing.Color HighlightText { get { throw null; } }
        public static System.Drawing.Color HotTrack { get { throw null; } }
        public static System.Drawing.Color InactiveBorder { get { throw null; } }
        public static System.Drawing.Color InactiveCaption { get { throw null; } }
        public static System.Drawing.Color InactiveCaptionText { get { throw null; } }
        public static System.Drawing.Color Info { get { throw null; } }
        public static System.Drawing.Color InfoText { get { throw null; } }
        public static System.Drawing.Color Menu { get { throw null; } }
        public static System.Drawing.Color MenuBar { get { throw null; } }
        public static System.Drawing.Color MenuHighlight { get { throw null; } }
        public static System.Drawing.Color MenuText { get { throw null; } }
        public static System.Drawing.Color ScrollBar { get { throw null; } }
        public static System.Drawing.Color Window { get { throw null; } }
        public static System.Drawing.Color WindowFrame { get { throw null; } }
        public static System.Drawing.Color WindowText { get { throw null; } }
    }
}
