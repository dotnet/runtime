// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Drawing
{
    public sealed partial class Bitmap : System.Drawing.Image
    {
        public Bitmap(System.Drawing.Image original) { }
        public Bitmap(System.Drawing.Image original, System.Drawing.Size newSize) { }
        public Bitmap(System.Drawing.Image original, int width, int height) { }
        public Bitmap(int width, int height) { }
        public Bitmap(int width, int height, System.Drawing.Graphics g) { }
        public Bitmap(int width, int height, System.Drawing.Imaging.PixelFormat format) { }
        public Bitmap(int width, int height, int stride, System.Drawing.Imaging.PixelFormat format, System.IntPtr scan0) { }
        public Bitmap(System.IO.Stream stream) { }
        public Bitmap(System.IO.Stream stream, bool useIcm) { }
        public Bitmap(string filename) { }
        public Bitmap(string filename, bool useIcm) { }
        public Bitmap(System.Type type, string resource) { }
        public System.Drawing.Bitmap Clone(System.Drawing.Rectangle rect, System.Drawing.Imaging.PixelFormat format) { throw null; }
        public System.Drawing.Bitmap Clone(System.Drawing.RectangleF rect, System.Drawing.Imaging.PixelFormat format) { throw null; }
        public static System.Drawing.Bitmap FromHicon(System.IntPtr hicon) { throw null; }
        public static System.Drawing.Bitmap FromResource(System.IntPtr hinstance, string bitmapName) { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(2))]
        public System.IntPtr GetHbitmap() { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(2))]
        public System.IntPtr GetHbitmap(System.Drawing.Color background) { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(2))]
        public System.IntPtr GetHicon() { throw null; }
        public System.Drawing.Color GetPixel(int x, int y) { throw null; }
        public System.Drawing.Imaging.BitmapData LockBits(System.Drawing.Rectangle rect, System.Drawing.Imaging.ImageLockMode flags, System.Drawing.Imaging.PixelFormat format) { throw null; }
        public System.Drawing.Imaging.BitmapData LockBits(System.Drawing.Rectangle rect, System.Drawing.Imaging.ImageLockMode flags, System.Drawing.Imaging.PixelFormat format, System.Drawing.Imaging.BitmapData bitmapData) { throw null; }
        public void MakeTransparent() { }
        public void MakeTransparent(System.Drawing.Color transparentColor) { }
        public void SetPixel(int x, int y, System.Drawing.Color color) { }
        public void SetResolution(float xDpi, float yDpi) { }
        public void UnlockBits(System.Drawing.Imaging.BitmapData bitmapdata) { }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(1))]
    public partial class BitmapSuffixInSameAssemblyAttribute : System.Attribute
    {
        public BitmapSuffixInSameAssemblyAttribute() { }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(1))]
    public partial class BitmapSuffixInSatelliteAssemblyAttribute : System.Attribute
    {
        public BitmapSuffixInSatelliteAssemblyAttribute() { }
    }
    public abstract partial class Brush : System.MarshalByRefObject, System.ICloneable, System.IDisposable
    {
        protected Brush() { }
        public abstract object Clone();
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        ~Brush() { }
        protected internal void SetNativeBrush(System.IntPtr brush) { }
    }
    public static partial class Brushes
    {
        public static System.Drawing.Brush AliceBlue { get { throw null; } }
        public static System.Drawing.Brush AntiqueWhite { get { throw null; } }
        public static System.Drawing.Brush Aqua { get { throw null; } }
        public static System.Drawing.Brush Aquamarine { get { throw null; } }
        public static System.Drawing.Brush Azure { get { throw null; } }
        public static System.Drawing.Brush Beige { get { throw null; } }
        public static System.Drawing.Brush Bisque { get { throw null; } }
        public static System.Drawing.Brush Black { get { throw null; } }
        public static System.Drawing.Brush BlanchedAlmond { get { throw null; } }
        public static System.Drawing.Brush Blue { get { throw null; } }
        public static System.Drawing.Brush BlueViolet { get { throw null; } }
        public static System.Drawing.Brush Brown { get { throw null; } }
        public static System.Drawing.Brush BurlyWood { get { throw null; } }
        public static System.Drawing.Brush CadetBlue { get { throw null; } }
        public static System.Drawing.Brush Chartreuse { get { throw null; } }
        public static System.Drawing.Brush Chocolate { get { throw null; } }
        public static System.Drawing.Brush Coral { get { throw null; } }
        public static System.Drawing.Brush CornflowerBlue { get { throw null; } }
        public static System.Drawing.Brush Cornsilk { get { throw null; } }
        public static System.Drawing.Brush Crimson { get { throw null; } }
        public static System.Drawing.Brush Cyan { get { throw null; } }
        public static System.Drawing.Brush DarkBlue { get { throw null; } }
        public static System.Drawing.Brush DarkCyan { get { throw null; } }
        public static System.Drawing.Brush DarkGoldenrod { get { throw null; } }
        public static System.Drawing.Brush DarkGray { get { throw null; } }
        public static System.Drawing.Brush DarkGreen { get { throw null; } }
        public static System.Drawing.Brush DarkKhaki { get { throw null; } }
        public static System.Drawing.Brush DarkMagenta { get { throw null; } }
        public static System.Drawing.Brush DarkOliveGreen { get { throw null; } }
        public static System.Drawing.Brush DarkOrange { get { throw null; } }
        public static System.Drawing.Brush DarkOrchid { get { throw null; } }
        public static System.Drawing.Brush DarkRed { get { throw null; } }
        public static System.Drawing.Brush DarkSalmon { get { throw null; } }
        public static System.Drawing.Brush DarkSeaGreen { get { throw null; } }
        public static System.Drawing.Brush DarkSlateBlue { get { throw null; } }
        public static System.Drawing.Brush DarkSlateGray { get { throw null; } }
        public static System.Drawing.Brush DarkTurquoise { get { throw null; } }
        public static System.Drawing.Brush DarkViolet { get { throw null; } }
        public static System.Drawing.Brush DeepPink { get { throw null; } }
        public static System.Drawing.Brush DeepSkyBlue { get { throw null; } }
        public static System.Drawing.Brush DimGray { get { throw null; } }
        public static System.Drawing.Brush DodgerBlue { get { throw null; } }
        public static System.Drawing.Brush Firebrick { get { throw null; } }
        public static System.Drawing.Brush FloralWhite { get { throw null; } }
        public static System.Drawing.Brush ForestGreen { get { throw null; } }
        public static System.Drawing.Brush Fuchsia { get { throw null; } }
        public static System.Drawing.Brush Gainsboro { get { throw null; } }
        public static System.Drawing.Brush GhostWhite { get { throw null; } }
        public static System.Drawing.Brush Gold { get { throw null; } }
        public static System.Drawing.Brush Goldenrod { get { throw null; } }
        public static System.Drawing.Brush Gray { get { throw null; } }
        public static System.Drawing.Brush Green { get { throw null; } }
        public static System.Drawing.Brush GreenYellow { get { throw null; } }
        public static System.Drawing.Brush Honeydew { get { throw null; } }
        public static System.Drawing.Brush HotPink { get { throw null; } }
        public static System.Drawing.Brush IndianRed { get { throw null; } }
        public static System.Drawing.Brush Indigo { get { throw null; } }
        public static System.Drawing.Brush Ivory { get { throw null; } }
        public static System.Drawing.Brush Khaki { get { throw null; } }
        public static System.Drawing.Brush Lavender { get { throw null; } }
        public static System.Drawing.Brush LavenderBlush { get { throw null; } }
        public static System.Drawing.Brush LawnGreen { get { throw null; } }
        public static System.Drawing.Brush LemonChiffon { get { throw null; } }
        public static System.Drawing.Brush LightBlue { get { throw null; } }
        public static System.Drawing.Brush LightCoral { get { throw null; } }
        public static System.Drawing.Brush LightCyan { get { throw null; } }
        public static System.Drawing.Brush LightGoldenrodYellow { get { throw null; } }
        public static System.Drawing.Brush LightGray { get { throw null; } }
        public static System.Drawing.Brush LightGreen { get { throw null; } }
        public static System.Drawing.Brush LightPink { get { throw null; } }
        public static System.Drawing.Brush LightSalmon { get { throw null; } }
        public static System.Drawing.Brush LightSeaGreen { get { throw null; } }
        public static System.Drawing.Brush LightSkyBlue { get { throw null; } }
        public static System.Drawing.Brush LightSlateGray { get { throw null; } }
        public static System.Drawing.Brush LightSteelBlue { get { throw null; } }
        public static System.Drawing.Brush LightYellow { get { throw null; } }
        public static System.Drawing.Brush Lime { get { throw null; } }
        public static System.Drawing.Brush LimeGreen { get { throw null; } }
        public static System.Drawing.Brush Linen { get { throw null; } }
        public static System.Drawing.Brush Magenta { get { throw null; } }
        public static System.Drawing.Brush Maroon { get { throw null; } }
        public static System.Drawing.Brush MediumAquamarine { get { throw null; } }
        public static System.Drawing.Brush MediumBlue { get { throw null; } }
        public static System.Drawing.Brush MediumOrchid { get { throw null; } }
        public static System.Drawing.Brush MediumPurple { get { throw null; } }
        public static System.Drawing.Brush MediumSeaGreen { get { throw null; } }
        public static System.Drawing.Brush MediumSlateBlue { get { throw null; } }
        public static System.Drawing.Brush MediumSpringGreen { get { throw null; } }
        public static System.Drawing.Brush MediumTurquoise { get { throw null; } }
        public static System.Drawing.Brush MediumVioletRed { get { throw null; } }
        public static System.Drawing.Brush MidnightBlue { get { throw null; } }
        public static System.Drawing.Brush MintCream { get { throw null; } }
        public static System.Drawing.Brush MistyRose { get { throw null; } }
        public static System.Drawing.Brush Moccasin { get { throw null; } }
        public static System.Drawing.Brush NavajoWhite { get { throw null; } }
        public static System.Drawing.Brush Navy { get { throw null; } }
        public static System.Drawing.Brush OldLace { get { throw null; } }
        public static System.Drawing.Brush Olive { get { throw null; } }
        public static System.Drawing.Brush OliveDrab { get { throw null; } }
        public static System.Drawing.Brush Orange { get { throw null; } }
        public static System.Drawing.Brush OrangeRed { get { throw null; } }
        public static System.Drawing.Brush Orchid { get { throw null; } }
        public static System.Drawing.Brush PaleGoldenrod { get { throw null; } }
        public static System.Drawing.Brush PaleGreen { get { throw null; } }
        public static System.Drawing.Brush PaleTurquoise { get { throw null; } }
        public static System.Drawing.Brush PaleVioletRed { get { throw null; } }
        public static System.Drawing.Brush PapayaWhip { get { throw null; } }
        public static System.Drawing.Brush PeachPuff { get { throw null; } }
        public static System.Drawing.Brush Peru { get { throw null; } }
        public static System.Drawing.Brush Pink { get { throw null; } }
        public static System.Drawing.Brush Plum { get { throw null; } }
        public static System.Drawing.Brush PowderBlue { get { throw null; } }
        public static System.Drawing.Brush Purple { get { throw null; } }
        public static System.Drawing.Brush Red { get { throw null; } }
        public static System.Drawing.Brush RosyBrown { get { throw null; } }
        public static System.Drawing.Brush RoyalBlue { get { throw null; } }
        public static System.Drawing.Brush SaddleBrown { get { throw null; } }
        public static System.Drawing.Brush Salmon { get { throw null; } }
        public static System.Drawing.Brush SandyBrown { get { throw null; } }
        public static System.Drawing.Brush SeaGreen { get { throw null; } }
        public static System.Drawing.Brush SeaShell { get { throw null; } }
        public static System.Drawing.Brush Sienna { get { throw null; } }
        public static System.Drawing.Brush Silver { get { throw null; } }
        public static System.Drawing.Brush SkyBlue { get { throw null; } }
        public static System.Drawing.Brush SlateBlue { get { throw null; } }
        public static System.Drawing.Brush SlateGray { get { throw null; } }
        public static System.Drawing.Brush Snow { get { throw null; } }
        public static System.Drawing.Brush SpringGreen { get { throw null; } }
        public static System.Drawing.Brush SteelBlue { get { throw null; } }
        public static System.Drawing.Brush Tan { get { throw null; } }
        public static System.Drawing.Brush Teal { get { throw null; } }
        public static System.Drawing.Brush Thistle { get { throw null; } }
        public static System.Drawing.Brush Tomato { get { throw null; } }
        public static System.Drawing.Brush Transparent { get { throw null; } }
        public static System.Drawing.Brush Turquoise { get { throw null; } }
        public static System.Drawing.Brush Violet { get { throw null; } }
        public static System.Drawing.Brush Wheat { get { throw null; } }
        public static System.Drawing.Brush White { get { throw null; } }
        public static System.Drawing.Brush WhiteSmoke { get { throw null; } }
        public static System.Drawing.Brush Yellow { get { throw null; } }
        public static System.Drawing.Brush YellowGreen { get { throw null; } }
    }
    public sealed partial class BufferedGraphics : System.IDisposable
    {
        internal BufferedGraphics() { }
        public System.Drawing.Graphics Graphics { get { throw null; } }
        public void Dispose() { }
        ~BufferedGraphics() { }
        public void Render() { }
        public void Render(System.Drawing.Graphics target) { }
        public void Render(System.IntPtr targetDC) { }
    }
    public sealed partial class BufferedGraphicsContext : System.IDisposable
    {
        public BufferedGraphicsContext() { }
        public System.Drawing.Size MaximumBuffer { get { throw null; } set { } }
        public System.Drawing.BufferedGraphics Allocate(System.Drawing.Graphics targetGraphics, System.Drawing.Rectangle targetRectangle) { throw null; }
        public System.Drawing.BufferedGraphics Allocate(System.IntPtr targetDC, System.Drawing.Rectangle targetRectangle) { throw null; }
        public void Dispose() { }
        ~BufferedGraphicsContext() { }
        public void Invalidate() { }
    }
    public static partial class BufferedGraphicsManager
    {
        public static System.Drawing.BufferedGraphicsContext Current { get { throw null; } }
    }
    public partial struct CharacterRange
    {
        private int _dummy;
        public CharacterRange(int First, int Length) { throw null; }
        public int First { get { throw null; } set { } }
        public int Length { get { throw null; } set { } }
        public override bool Equals(object obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Drawing.CharacterRange cr1, System.Drawing.CharacterRange cr2) { throw null; }
        public static bool operator !=(System.Drawing.CharacterRange cr1, System.Drawing.CharacterRange cr2) { throw null; }
    }
    public sealed partial class ColorTranslator
    {
        internal ColorTranslator() { }
        public static System.Drawing.Color FromHtml(string htmlColor) { throw null; }
        public static System.Drawing.Color FromOle(int oleColor) { throw null; }
        public static System.Drawing.Color FromWin32(int win32Color) { throw null; }
        public static string ToHtml(System.Drawing.Color c) { throw null; }
        public static int ToOle(System.Drawing.Color c) { throw null; }
        public static int ToWin32(System.Drawing.Color c) { throw null; }
    }
    public enum ContentAlignment
    {
        BottomCenter = 512,
        BottomLeft = 256,
        BottomRight = 1024,
        MiddleCenter = 32,
        MiddleLeft = 16,
        MiddleRight = 64,
        TopCenter = 2,
        TopLeft = 1,
        TopRight = 4,
    }
    public enum CopyPixelOperation
    {
        Blackness = 66,
        CaptureBlt = 1073741824,
        DestinationInvert = 5570569,
        MergeCopy = 12583114,
        MergePaint = 12255782,
        NoMirrorBitmap = -2147483648,
        NotSourceCopy = 3342344,
        NotSourceErase = 1114278,
        PatCopy = 15728673,
        PatInvert = 5898313,
        PatPaint = 16452105,
        SourceAnd = 8913094,
        SourceCopy = 13369376,
        SourceErase = 4457256,
        SourceInvert = 6684742,
        SourcePaint = 15597702,
        Whiteness = 16711778,
    }
    public sealed partial class Font : System.MarshalByRefObject, System.ICloneable, System.IDisposable, System.Runtime.Serialization.ISerializable
    {
        public Font(System.Drawing.Font prototype, System.Drawing.FontStyle newStyle) { }
        public Font(System.Drawing.FontFamily family, float emSize) { }
        public Font(System.Drawing.FontFamily family, float emSize, System.Drawing.FontStyle style) { }
        public Font(System.Drawing.FontFamily family, float emSize, System.Drawing.FontStyle style, System.Drawing.GraphicsUnit unit) { }
        public Font(System.Drawing.FontFamily family, float emSize, System.Drawing.FontStyle style, System.Drawing.GraphicsUnit unit, byte gdiCharSet) { }
        public Font(System.Drawing.FontFamily family, float emSize, System.Drawing.FontStyle style, System.Drawing.GraphicsUnit unit, byte gdiCharSet, bool gdiVerticalFont) { }
        public Font(System.Drawing.FontFamily family, float emSize, System.Drawing.GraphicsUnit unit) { }
        public Font(string familyName, float emSize) { }
        public Font(string familyName, float emSize, System.Drawing.FontStyle style) { }
        public Font(string familyName, float emSize, System.Drawing.FontStyle style, System.Drawing.GraphicsUnit unit) { }
        public Font(string familyName, float emSize, System.Drawing.FontStyle style, System.Drawing.GraphicsUnit unit, byte gdiCharSet) { }
        public Font(string familyName, float emSize, System.Drawing.FontStyle style, System.Drawing.GraphicsUnit unit, byte gdiCharSet, bool gdiVerticalFont) { }
        public Font(string familyName, float emSize, System.Drawing.GraphicsUnit unit) { }
        [System.ComponentModel.DesignerSerializationVisibilityAttribute((System.ComponentModel.DesignerSerializationVisibility)(0))]
        public bool Bold { get { throw null; } }
        [System.ComponentModel.BrowsableAttribute(false)]
        public System.Drawing.FontFamily FontFamily { get { throw null; } }
        [System.ComponentModel.DesignerSerializationVisibilityAttribute((System.ComponentModel.DesignerSerializationVisibility)(0))]
        public byte GdiCharSet { get { throw null; } }
        [System.ComponentModel.DesignerSerializationVisibilityAttribute((System.ComponentModel.DesignerSerializationVisibility)(0))]
        public bool GdiVerticalFont { get { throw null; } }
        [System.ComponentModel.BrowsableAttribute(false)]
        public int Height { get { throw null; } }
        [System.ComponentModel.BrowsableAttribute(false)]
        public bool IsSystemFont { get { throw null; } }
        [System.ComponentModel.DesignerSerializationVisibilityAttribute((System.ComponentModel.DesignerSerializationVisibility)(0))]
        public bool Italic { get { throw null; } }
        [System.ComponentModel.DesignerSerializationVisibilityAttribute((System.ComponentModel.DesignerSerializationVisibility)(0))]
        public string Name { get { throw null; } }
        [System.ComponentModel.BrowsableAttribute(false)]
        public string OriginalFontName { get { throw null; } }
        public float Size { get { throw null; } }
        [System.ComponentModel.BrowsableAttribute(false)]
        public float SizeInPoints { get { throw null; } }
        [System.ComponentModel.DesignerSerializationVisibilityAttribute((System.ComponentModel.DesignerSerializationVisibility)(0))]
        public bool Strikeout { get { throw null; } }
        [System.ComponentModel.BrowsableAttribute(false)]
        public System.Drawing.FontStyle Style { get { throw null; } }
        [System.ComponentModel.BrowsableAttribute(false)]
        public string SystemFontName { get { throw null; } }
        [System.ComponentModel.DesignerSerializationVisibilityAttribute((System.ComponentModel.DesignerSerializationVisibility)(0))]
        public bool Underline { get { throw null; } }
        public System.Drawing.GraphicsUnit Unit { get { throw null; } }
        public object Clone() { throw null; }
        public void Dispose() { }
        public override bool Equals(object obj) { throw null; }
        ~Font() { }
        public static System.Drawing.Font FromHdc(System.IntPtr hdc) { throw null; }
        public static System.Drawing.Font FromHfont(System.IntPtr hfont) { throw null; }
        public static System.Drawing.Font FromLogFont(object lf) { throw null; }
        public static System.Drawing.Font FromLogFont(object lf, System.IntPtr hdc) { throw null; }
        public override int GetHashCode() { throw null; }
        public float GetHeight() { throw null; }
        public float GetHeight(System.Drawing.Graphics graphics) { throw null; }
        public float GetHeight(float dpi) { throw null; }
        void System.Runtime.Serialization.ISerializable.GetObjectData(System.Runtime.Serialization.SerializationInfo si, System.Runtime.Serialization.StreamingContext context) { }
        public System.IntPtr ToHfont() { throw null; }
        public void ToLogFont(object logFont) { }
        public void ToLogFont(object logFont, System.Drawing.Graphics graphics) { }
        public override string ToString() { throw null; }
    }
    public sealed partial class FontFamily : System.MarshalByRefObject, System.IDisposable
    {
        public FontFamily(System.Drawing.Text.GenericFontFamilies genericFamily) { }
        public FontFamily(string name) { }
        public FontFamily(string name, System.Drawing.Text.FontCollection fontCollection) { }
        public static System.Drawing.FontFamily[] Families { get { throw null; } }
        public static System.Drawing.FontFamily GenericMonospace { get { throw null; } }
        public static System.Drawing.FontFamily GenericSansSerif { get { throw null; } }
        public static System.Drawing.FontFamily GenericSerif { get { throw null; } }
        public string Name { get { throw null; } }
        public void Dispose() { }
        public override bool Equals(object obj) { throw null; }
        ~FontFamily() { }
        public int GetCellAscent(System.Drawing.FontStyle style) { throw null; }
        public int GetCellDescent(System.Drawing.FontStyle style) { throw null; }
        public int GetEmHeight(System.Drawing.FontStyle style) { throw null; }
        [System.ObsoleteAttribute("Do not use method GetFamilies, use property Families instead")]
        public static System.Drawing.FontFamily[] GetFamilies(System.Drawing.Graphics graphics) { throw null; }
        public override int GetHashCode() { throw null; }
        public int GetLineSpacing(System.Drawing.FontStyle style) { throw null; }
        public string GetName(int language) { throw null; }
        public bool IsStyleAvailable(System.Drawing.FontStyle style) { throw null; }
        public override string ToString() { throw null; }
    }
    [System.FlagsAttribute]
    public enum FontStyle
    {
        Bold = 1,
        Italic = 2,
        Regular = 0,
        Strikeout = 8,
        Underline = 4,
    }
    public sealed partial class Graphics : System.MarshalByRefObject, System.Drawing.IDeviceContext, System.IDisposable
    {
        internal Graphics() { }
        public System.Drawing.Region Clip { get { throw null; } set { } }
        public System.Drawing.RectangleF ClipBounds { get { throw null; } }
        public System.Drawing.Drawing2D.CompositingMode CompositingMode { get { throw null; } set { } }
        public System.Drawing.Drawing2D.CompositingQuality CompositingQuality { get { throw null; } set { } }
        public float DpiX { get { throw null; } }
        public float DpiY { get { throw null; } }
        public System.Drawing.Drawing2D.InterpolationMode InterpolationMode { get { throw null; } set { } }
        public bool IsClipEmpty { get { throw null; } }
        public bool IsVisibleClipEmpty { get { throw null; } }
        public float PageScale { get { throw null; } set { } }
        public System.Drawing.GraphicsUnit PageUnit { get { throw null; } set { } }
        public System.Drawing.Drawing2D.PixelOffsetMode PixelOffsetMode { get { throw null; } set { } }
        public System.Drawing.Point RenderingOrigin { get { throw null; } set { } }
        public System.Drawing.Drawing2D.SmoothingMode SmoothingMode { get { throw null; } set { } }
        public int TextContrast { get { throw null; } set { } }
        public System.Drawing.Text.TextRenderingHint TextRenderingHint { get { throw null; } set { } }
        public System.Drawing.Drawing2D.Matrix Transform { get { throw null; } set { } }
        public System.Drawing.RectangleF VisibleClipBounds { get { throw null; } }
        public void AddMetafileComment(byte[] data) { }
        public System.Drawing.Drawing2D.GraphicsContainer BeginContainer() { throw null; }
        public System.Drawing.Drawing2D.GraphicsContainer BeginContainer(System.Drawing.Rectangle dstrect, System.Drawing.Rectangle srcrect, System.Drawing.GraphicsUnit unit) { throw null; }
        public System.Drawing.Drawing2D.GraphicsContainer BeginContainer(System.Drawing.RectangleF dstrect, System.Drawing.RectangleF srcrect, System.Drawing.GraphicsUnit unit) { throw null; }
        public void Clear(System.Drawing.Color color) { }
        public void CopyFromScreen(System.Drawing.Point upperLeftSource, System.Drawing.Point upperLeftDestination, System.Drawing.Size blockRegionSize) { }
        public void CopyFromScreen(System.Drawing.Point upperLeftSource, System.Drawing.Point upperLeftDestination, System.Drawing.Size blockRegionSize, System.Drawing.CopyPixelOperation copyPixelOperation) { }
        public void CopyFromScreen(int sourceX, int sourceY, int destinationX, int destinationY, System.Drawing.Size blockRegionSize) { }
        public void CopyFromScreen(int sourceX, int sourceY, int destinationX, int destinationY, System.Drawing.Size blockRegionSize, System.Drawing.CopyPixelOperation copyPixelOperation) { }
        public void Dispose() { }
        public void DrawArc(System.Drawing.Pen pen, System.Drawing.Rectangle rect, float startAngle, float sweepAngle) { }
        public void DrawArc(System.Drawing.Pen pen, System.Drawing.RectangleF rect, float startAngle, float sweepAngle) { }
        public void DrawArc(System.Drawing.Pen pen, int x, int y, int width, int height, int startAngle, int sweepAngle) { }
        public void DrawArc(System.Drawing.Pen pen, float x, float y, float width, float height, float startAngle, float sweepAngle) { }
        public void DrawBezier(System.Drawing.Pen pen, System.Drawing.Point pt1, System.Drawing.Point pt2, System.Drawing.Point pt3, System.Drawing.Point pt4) { }
        public void DrawBezier(System.Drawing.Pen pen, System.Drawing.PointF pt1, System.Drawing.PointF pt2, System.Drawing.PointF pt3, System.Drawing.PointF pt4) { }
        public void DrawBezier(System.Drawing.Pen pen, float x1, float y1, float x2, float y2, float x3, float y3, float x4, float y4) { }
        public void DrawBeziers(System.Drawing.Pen pen, System.Drawing.PointF[] points) { }
        public void DrawBeziers(System.Drawing.Pen pen, System.Drawing.Point[] points) { }
        public void DrawClosedCurve(System.Drawing.Pen pen, System.Drawing.PointF[] points) { }
        public void DrawClosedCurve(System.Drawing.Pen pen, System.Drawing.PointF[] points, float tension, System.Drawing.Drawing2D.FillMode fillmode) { }
        public void DrawClosedCurve(System.Drawing.Pen pen, System.Drawing.Point[] points) { }
        public void DrawClosedCurve(System.Drawing.Pen pen, System.Drawing.Point[] points, float tension, System.Drawing.Drawing2D.FillMode fillmode) { }
        public void DrawCurve(System.Drawing.Pen pen, System.Drawing.PointF[] points) { }
        public void DrawCurve(System.Drawing.Pen pen, System.Drawing.PointF[] points, int offset, int numberOfSegments) { }
        public void DrawCurve(System.Drawing.Pen pen, System.Drawing.PointF[] points, int offset, int numberOfSegments, float tension) { }
        public void DrawCurve(System.Drawing.Pen pen, System.Drawing.PointF[] points, float tension) { }
        public void DrawCurve(System.Drawing.Pen pen, System.Drawing.Point[] points) { }
        public void DrawCurve(System.Drawing.Pen pen, System.Drawing.Point[] points, int offset, int numberOfSegments, float tension) { }
        public void DrawCurve(System.Drawing.Pen pen, System.Drawing.Point[] points, float tension) { }
        public void DrawEllipse(System.Drawing.Pen pen, System.Drawing.Rectangle rect) { }
        public void DrawEllipse(System.Drawing.Pen pen, System.Drawing.RectangleF rect) { }
        public void DrawEllipse(System.Drawing.Pen pen, int x, int y, int width, int height) { }
        public void DrawEllipse(System.Drawing.Pen pen, float x, float y, float width, float height) { }
        public void DrawIcon(System.Drawing.Icon icon, System.Drawing.Rectangle targetRect) { }
        public void DrawIcon(System.Drawing.Icon icon, int x, int y) { }
        public void DrawIconUnstretched(System.Drawing.Icon icon, System.Drawing.Rectangle targetRect) { }
        public void DrawImage(System.Drawing.Image image, System.Drawing.Point point) { }
        public void DrawImage(System.Drawing.Image image, System.Drawing.PointF point) { }
        public void DrawImage(System.Drawing.Image image, System.Drawing.PointF[] destPoints) { }
        public void DrawImage(System.Drawing.Image image, System.Drawing.PointF[] destPoints, System.Drawing.RectangleF srcRect, System.Drawing.GraphicsUnit srcUnit) { }
        public void DrawImage(System.Drawing.Image image, System.Drawing.PointF[] destPoints, System.Drawing.RectangleF srcRect, System.Drawing.GraphicsUnit srcUnit, System.Drawing.Imaging.ImageAttributes imageAttr) { }
        public void DrawImage(System.Drawing.Image image, System.Drawing.PointF[] destPoints, System.Drawing.RectangleF srcRect, System.Drawing.GraphicsUnit srcUnit, System.Drawing.Imaging.ImageAttributes imageAttr, System.Drawing.Graphics.DrawImageAbort callback) { }
        public void DrawImage(System.Drawing.Image image, System.Drawing.PointF[] destPoints, System.Drawing.RectangleF srcRect, System.Drawing.GraphicsUnit srcUnit, System.Drawing.Imaging.ImageAttributes imageAttr, System.Drawing.Graphics.DrawImageAbort callback, int callbackData) { }
        public void DrawImage(System.Drawing.Image image, System.Drawing.Point[] destPoints) { }
        public void DrawImage(System.Drawing.Image image, System.Drawing.Point[] destPoints, System.Drawing.Rectangle srcRect, System.Drawing.GraphicsUnit srcUnit) { }
        public void DrawImage(System.Drawing.Image image, System.Drawing.Point[] destPoints, System.Drawing.Rectangle srcRect, System.Drawing.GraphicsUnit srcUnit, System.Drawing.Imaging.ImageAttributes imageAttr) { }
        public void DrawImage(System.Drawing.Image image, System.Drawing.Point[] destPoints, System.Drawing.Rectangle srcRect, System.Drawing.GraphicsUnit srcUnit, System.Drawing.Imaging.ImageAttributes imageAttr, System.Drawing.Graphics.DrawImageAbort callback) { }
        public void DrawImage(System.Drawing.Image image, System.Drawing.Point[] destPoints, System.Drawing.Rectangle srcRect, System.Drawing.GraphicsUnit srcUnit, System.Drawing.Imaging.ImageAttributes imageAttr, System.Drawing.Graphics.DrawImageAbort callback, int callbackData) { }
        public void DrawImage(System.Drawing.Image image, System.Drawing.Rectangle rect) { }
        public void DrawImage(System.Drawing.Image image, System.Drawing.Rectangle destRect, System.Drawing.Rectangle srcRect, System.Drawing.GraphicsUnit srcUnit) { }
        public void DrawImage(System.Drawing.Image image, System.Drawing.Rectangle destRect, int srcX, int srcY, int srcWidth, int srcHeight, System.Drawing.GraphicsUnit srcUnit) { }
        public void DrawImage(System.Drawing.Image image, System.Drawing.Rectangle destRect, int srcX, int srcY, int srcWidth, int srcHeight, System.Drawing.GraphicsUnit srcUnit, System.Drawing.Imaging.ImageAttributes imageAttr) { }
        public void DrawImage(System.Drawing.Image image, System.Drawing.Rectangle destRect, int srcX, int srcY, int srcWidth, int srcHeight, System.Drawing.GraphicsUnit srcUnit, System.Drawing.Imaging.ImageAttributes imageAttr, System.Drawing.Graphics.DrawImageAbort callback) { }
        public void DrawImage(System.Drawing.Image image, System.Drawing.Rectangle destRect, int srcX, int srcY, int srcWidth, int srcHeight, System.Drawing.GraphicsUnit srcUnit, System.Drawing.Imaging.ImageAttributes imageAttrs, System.Drawing.Graphics.DrawImageAbort callback, System.IntPtr callbackData) { }
        public void DrawImage(System.Drawing.Image image, System.Drawing.Rectangle destRect, float srcX, float srcY, float srcWidth, float srcHeight, System.Drawing.GraphicsUnit srcUnit) { }
        public void DrawImage(System.Drawing.Image image, System.Drawing.Rectangle destRect, float srcX, float srcY, float srcWidth, float srcHeight, System.Drawing.GraphicsUnit srcUnit, System.Drawing.Imaging.ImageAttributes imageAttrs) { }
        public void DrawImage(System.Drawing.Image image, System.Drawing.Rectangle destRect, float srcX, float srcY, float srcWidth, float srcHeight, System.Drawing.GraphicsUnit srcUnit, System.Drawing.Imaging.ImageAttributes imageAttrs, System.Drawing.Graphics.DrawImageAbort callback) { }
        public void DrawImage(System.Drawing.Image image, System.Drawing.Rectangle destRect, float srcX, float srcY, float srcWidth, float srcHeight, System.Drawing.GraphicsUnit srcUnit, System.Drawing.Imaging.ImageAttributes imageAttrs, System.Drawing.Graphics.DrawImageAbort callback, System.IntPtr callbackData) { }
        public void DrawImage(System.Drawing.Image image, System.Drawing.RectangleF rect) { }
        public void DrawImage(System.Drawing.Image image, System.Drawing.RectangleF destRect, System.Drawing.RectangleF srcRect, System.Drawing.GraphicsUnit srcUnit) { }
        public void DrawImage(System.Drawing.Image image, int x, int y) { }
        public void DrawImage(System.Drawing.Image image, int x, int y, System.Drawing.Rectangle srcRect, System.Drawing.GraphicsUnit srcUnit) { }
        public void DrawImage(System.Drawing.Image image, int x, int y, int width, int height) { }
        public void DrawImage(System.Drawing.Image image, float x, float y) { }
        public void DrawImage(System.Drawing.Image image, float x, float y, System.Drawing.RectangleF srcRect, System.Drawing.GraphicsUnit srcUnit) { }
        public void DrawImage(System.Drawing.Image image, float x, float y, float width, float height) { }
        public void DrawImageUnscaled(System.Drawing.Image image, System.Drawing.Point point) { }
        public void DrawImageUnscaled(System.Drawing.Image image, System.Drawing.Rectangle rect) { }
        public void DrawImageUnscaled(System.Drawing.Image image, int x, int y) { }
        public void DrawImageUnscaled(System.Drawing.Image image, int x, int y, int width, int height) { }
        public void DrawImageUnscaledAndClipped(System.Drawing.Image image, System.Drawing.Rectangle rect) { }
        public void DrawLine(System.Drawing.Pen pen, System.Drawing.Point pt1, System.Drawing.Point pt2) { }
        public void DrawLine(System.Drawing.Pen pen, System.Drawing.PointF pt1, System.Drawing.PointF pt2) { }
        public void DrawLine(System.Drawing.Pen pen, int x1, int y1, int x2, int y2) { }
        public void DrawLine(System.Drawing.Pen pen, float x1, float y1, float x2, float y2) { }
        public void DrawLines(System.Drawing.Pen pen, System.Drawing.PointF[] points) { }
        public void DrawLines(System.Drawing.Pen pen, System.Drawing.Point[] points) { }
        public void DrawPath(System.Drawing.Pen pen, System.Drawing.Drawing2D.GraphicsPath path) { }
        public void DrawPie(System.Drawing.Pen pen, System.Drawing.Rectangle rect, float startAngle, float sweepAngle) { }
        public void DrawPie(System.Drawing.Pen pen, System.Drawing.RectangleF rect, float startAngle, float sweepAngle) { }
        public void DrawPie(System.Drawing.Pen pen, int x, int y, int width, int height, int startAngle, int sweepAngle) { }
        public void DrawPie(System.Drawing.Pen pen, float x, float y, float width, float height, float startAngle, float sweepAngle) { }
        public void DrawPolygon(System.Drawing.Pen pen, System.Drawing.PointF[] points) { }
        public void DrawPolygon(System.Drawing.Pen pen, System.Drawing.Point[] points) { }
        public void DrawRectangle(System.Drawing.Pen pen, System.Drawing.Rectangle rect) { }
        public void DrawRectangle(System.Drawing.Pen pen, int x, int y, int width, int height) { }
        public void DrawRectangle(System.Drawing.Pen pen, float x, float y, float width, float height) { }
        public void DrawRectangles(System.Drawing.Pen pen, System.Drawing.RectangleF[] rects) { }
        public void DrawRectangles(System.Drawing.Pen pen, System.Drawing.Rectangle[] rects) { }
        public void DrawString(string s, System.Drawing.Font font, System.Drawing.Brush brush, System.Drawing.PointF point) { }
        public void DrawString(string s, System.Drawing.Font font, System.Drawing.Brush brush, System.Drawing.PointF point, System.Drawing.StringFormat format) { }
        public void DrawString(string s, System.Drawing.Font font, System.Drawing.Brush brush, System.Drawing.RectangleF layoutRectangle) { }
        public void DrawString(string s, System.Drawing.Font font, System.Drawing.Brush brush, System.Drawing.RectangleF layoutRectangle, System.Drawing.StringFormat format) { }
        public void DrawString(string s, System.Drawing.Font font, System.Drawing.Brush brush, float x, float y) { }
        public void DrawString(string s, System.Drawing.Font font, System.Drawing.Brush brush, float x, float y, System.Drawing.StringFormat format) { }
        public void EndContainer(System.Drawing.Drawing2D.GraphicsContainer container) { }
        public void EnumerateMetafile(System.Drawing.Imaging.Metafile metafile, System.Drawing.Point destPoint, System.Drawing.Graphics.EnumerateMetafileProc callback) { }
        public void EnumerateMetafile(System.Drawing.Imaging.Metafile metafile, System.Drawing.Point destPoint, System.Drawing.Graphics.EnumerateMetafileProc callback, System.IntPtr callbackData) { }
        public void EnumerateMetafile(System.Drawing.Imaging.Metafile metafile, System.Drawing.Point destPoint, System.Drawing.Graphics.EnumerateMetafileProc callback, System.IntPtr callbackData, System.Drawing.Imaging.ImageAttributes imageAttr) { }
        public void EnumerateMetafile(System.Drawing.Imaging.Metafile metafile, System.Drawing.Point destPoint, System.Drawing.Rectangle srcRect, System.Drawing.GraphicsUnit srcUnit, System.Drawing.Graphics.EnumerateMetafileProc callback) { }
        public void EnumerateMetafile(System.Drawing.Imaging.Metafile metafile, System.Drawing.Point destPoint, System.Drawing.Rectangle srcRect, System.Drawing.GraphicsUnit srcUnit, System.Drawing.Graphics.EnumerateMetafileProc callback, System.IntPtr callbackData) { }
        public void EnumerateMetafile(System.Drawing.Imaging.Metafile metafile, System.Drawing.Point destPoint, System.Drawing.Rectangle srcRect, System.Drawing.GraphicsUnit unit, System.Drawing.Graphics.EnumerateMetafileProc callback, System.IntPtr callbackData, System.Drawing.Imaging.ImageAttributes imageAttr) { }
        public void EnumerateMetafile(System.Drawing.Imaging.Metafile metafile, System.Drawing.PointF destPoint, System.Drawing.Graphics.EnumerateMetafileProc callback) { }
        public void EnumerateMetafile(System.Drawing.Imaging.Metafile metafile, System.Drawing.PointF destPoint, System.Drawing.Graphics.EnumerateMetafileProc callback, System.IntPtr callbackData) { }
        public void EnumerateMetafile(System.Drawing.Imaging.Metafile metafile, System.Drawing.PointF destPoint, System.Drawing.Graphics.EnumerateMetafileProc callback, System.IntPtr callbackData, System.Drawing.Imaging.ImageAttributes imageAttr) { }
        public void EnumerateMetafile(System.Drawing.Imaging.Metafile metafile, System.Drawing.PointF destPoint, System.Drawing.RectangleF srcRect, System.Drawing.GraphicsUnit srcUnit, System.Drawing.Graphics.EnumerateMetafileProc callback) { }
        public void EnumerateMetafile(System.Drawing.Imaging.Metafile metafile, System.Drawing.PointF destPoint, System.Drawing.RectangleF srcRect, System.Drawing.GraphicsUnit srcUnit, System.Drawing.Graphics.EnumerateMetafileProc callback, System.IntPtr callbackData) { }
        public void EnumerateMetafile(System.Drawing.Imaging.Metafile metafile, System.Drawing.PointF destPoint, System.Drawing.RectangleF srcRect, System.Drawing.GraphicsUnit unit, System.Drawing.Graphics.EnumerateMetafileProc callback, System.IntPtr callbackData, System.Drawing.Imaging.ImageAttributes imageAttr) { }
        public void EnumerateMetafile(System.Drawing.Imaging.Metafile metafile, System.Drawing.PointF[] destPoints, System.Drawing.Graphics.EnumerateMetafileProc callback) { }
        public void EnumerateMetafile(System.Drawing.Imaging.Metafile metafile, System.Drawing.PointF[] destPoints, System.Drawing.Graphics.EnumerateMetafileProc callback, System.IntPtr callbackData) { }
        public void EnumerateMetafile(System.Drawing.Imaging.Metafile metafile, System.Drawing.PointF[] destPoints, System.Drawing.Graphics.EnumerateMetafileProc callback, System.IntPtr callbackData, System.Drawing.Imaging.ImageAttributes imageAttr) { }
        public void EnumerateMetafile(System.Drawing.Imaging.Metafile metafile, System.Drawing.PointF[] destPoints, System.Drawing.RectangleF srcRect, System.Drawing.GraphicsUnit srcUnit, System.Drawing.Graphics.EnumerateMetafileProc callback) { }
        public void EnumerateMetafile(System.Drawing.Imaging.Metafile metafile, System.Drawing.PointF[] destPoints, System.Drawing.RectangleF srcRect, System.Drawing.GraphicsUnit srcUnit, System.Drawing.Graphics.EnumerateMetafileProc callback, System.IntPtr callbackData) { }
        public void EnumerateMetafile(System.Drawing.Imaging.Metafile metafile, System.Drawing.PointF[] destPoints, System.Drawing.RectangleF srcRect, System.Drawing.GraphicsUnit unit, System.Drawing.Graphics.EnumerateMetafileProc callback, System.IntPtr callbackData, System.Drawing.Imaging.ImageAttributes imageAttr) { }
        public void EnumerateMetafile(System.Drawing.Imaging.Metafile metafile, System.Drawing.Point[] destPoints, System.Drawing.Graphics.EnumerateMetafileProc callback) { }
        public void EnumerateMetafile(System.Drawing.Imaging.Metafile metafile, System.Drawing.Point[] destPoints, System.Drawing.Graphics.EnumerateMetafileProc callback, System.IntPtr callbackData) { }
        public void EnumerateMetafile(System.Drawing.Imaging.Metafile metafile, System.Drawing.Point[] destPoints, System.Drawing.Graphics.EnumerateMetafileProc callback, System.IntPtr callbackData, System.Drawing.Imaging.ImageAttributes imageAttr) { }
        public void EnumerateMetafile(System.Drawing.Imaging.Metafile metafile, System.Drawing.Point[] destPoints, System.Drawing.Rectangle srcRect, System.Drawing.GraphicsUnit srcUnit, System.Drawing.Graphics.EnumerateMetafileProc callback) { }
        public void EnumerateMetafile(System.Drawing.Imaging.Metafile metafile, System.Drawing.Point[] destPoints, System.Drawing.Rectangle srcRect, System.Drawing.GraphicsUnit srcUnit, System.Drawing.Graphics.EnumerateMetafileProc callback, System.IntPtr callbackData) { }
        public void EnumerateMetafile(System.Drawing.Imaging.Metafile metafile, System.Drawing.Point[] destPoints, System.Drawing.Rectangle srcRect, System.Drawing.GraphicsUnit unit, System.Drawing.Graphics.EnumerateMetafileProc callback, System.IntPtr callbackData, System.Drawing.Imaging.ImageAttributes imageAttr) { }
        public void EnumerateMetafile(System.Drawing.Imaging.Metafile metafile, System.Drawing.Rectangle destRect, System.Drawing.Graphics.EnumerateMetafileProc callback) { }
        public void EnumerateMetafile(System.Drawing.Imaging.Metafile metafile, System.Drawing.Rectangle destRect, System.Drawing.Graphics.EnumerateMetafileProc callback, System.IntPtr callbackData) { }
        public void EnumerateMetafile(System.Drawing.Imaging.Metafile metafile, System.Drawing.Rectangle destRect, System.Drawing.Graphics.EnumerateMetafileProc callback, System.IntPtr callbackData, System.Drawing.Imaging.ImageAttributes imageAttr) { }
        public void EnumerateMetafile(System.Drawing.Imaging.Metafile metafile, System.Drawing.Rectangle destRect, System.Drawing.Rectangle srcRect, System.Drawing.GraphicsUnit srcUnit, System.Drawing.Graphics.EnumerateMetafileProc callback) { }
        public void EnumerateMetafile(System.Drawing.Imaging.Metafile metafile, System.Drawing.Rectangle destRect, System.Drawing.Rectangle srcRect, System.Drawing.GraphicsUnit srcUnit, System.Drawing.Graphics.EnumerateMetafileProc callback, System.IntPtr callbackData) { }
        public void EnumerateMetafile(System.Drawing.Imaging.Metafile metafile, System.Drawing.Rectangle destRect, System.Drawing.Rectangle srcRect, System.Drawing.GraphicsUnit unit, System.Drawing.Graphics.EnumerateMetafileProc callback, System.IntPtr callbackData, System.Drawing.Imaging.ImageAttributes imageAttr) { }
        public void EnumerateMetafile(System.Drawing.Imaging.Metafile metafile, System.Drawing.RectangleF destRect, System.Drawing.Graphics.EnumerateMetafileProc callback) { }
        public void EnumerateMetafile(System.Drawing.Imaging.Metafile metafile, System.Drawing.RectangleF destRect, System.Drawing.Graphics.EnumerateMetafileProc callback, System.IntPtr callbackData) { }
        public void EnumerateMetafile(System.Drawing.Imaging.Metafile metafile, System.Drawing.RectangleF destRect, System.Drawing.Graphics.EnumerateMetafileProc callback, System.IntPtr callbackData, System.Drawing.Imaging.ImageAttributes imageAttr) { }
        public void EnumerateMetafile(System.Drawing.Imaging.Metafile metafile, System.Drawing.RectangleF destRect, System.Drawing.RectangleF srcRect, System.Drawing.GraphicsUnit srcUnit, System.Drawing.Graphics.EnumerateMetafileProc callback) { }
        public void EnumerateMetafile(System.Drawing.Imaging.Metafile metafile, System.Drawing.RectangleF destRect, System.Drawing.RectangleF srcRect, System.Drawing.GraphicsUnit srcUnit, System.Drawing.Graphics.EnumerateMetafileProc callback, System.IntPtr callbackData) { }
        public void EnumerateMetafile(System.Drawing.Imaging.Metafile metafile, System.Drawing.RectangleF destRect, System.Drawing.RectangleF srcRect, System.Drawing.GraphicsUnit unit, System.Drawing.Graphics.EnumerateMetafileProc callback, System.IntPtr callbackData, System.Drawing.Imaging.ImageAttributes imageAttr) { }
        public void ExcludeClip(System.Drawing.Rectangle rect) { }
        public void ExcludeClip(System.Drawing.Region region) { }
        public void FillClosedCurve(System.Drawing.Brush brush, System.Drawing.PointF[] points) { }
        public void FillClosedCurve(System.Drawing.Brush brush, System.Drawing.PointF[] points, System.Drawing.Drawing2D.FillMode fillmode) { }
        public void FillClosedCurve(System.Drawing.Brush brush, System.Drawing.PointF[] points, System.Drawing.Drawing2D.FillMode fillmode, float tension) { }
        public void FillClosedCurve(System.Drawing.Brush brush, System.Drawing.Point[] points) { }
        public void FillClosedCurve(System.Drawing.Brush brush, System.Drawing.Point[] points, System.Drawing.Drawing2D.FillMode fillmode) { }
        public void FillClosedCurve(System.Drawing.Brush brush, System.Drawing.Point[] points, System.Drawing.Drawing2D.FillMode fillmode, float tension) { }
        public void FillEllipse(System.Drawing.Brush brush, System.Drawing.Rectangle rect) { }
        public void FillEllipse(System.Drawing.Brush brush, System.Drawing.RectangleF rect) { }
        public void FillEllipse(System.Drawing.Brush brush, int x, int y, int width, int height) { }
        public void FillEllipse(System.Drawing.Brush brush, float x, float y, float width, float height) { }
        public void FillPath(System.Drawing.Brush brush, System.Drawing.Drawing2D.GraphicsPath path) { }
        public void FillPie(System.Drawing.Brush brush, System.Drawing.Rectangle rect, float startAngle, float sweepAngle) { }
        public void FillPie(System.Drawing.Brush brush, int x, int y, int width, int height, int startAngle, int sweepAngle) { }
        public void FillPie(System.Drawing.Brush brush, float x, float y, float width, float height, float startAngle, float sweepAngle) { }
        public void FillPolygon(System.Drawing.Brush brush, System.Drawing.PointF[] points) { }
        public void FillPolygon(System.Drawing.Brush brush, System.Drawing.PointF[] points, System.Drawing.Drawing2D.FillMode fillMode) { }
        public void FillPolygon(System.Drawing.Brush brush, System.Drawing.Point[] points) { }
        public void FillPolygon(System.Drawing.Brush brush, System.Drawing.Point[] points, System.Drawing.Drawing2D.FillMode fillMode) { }
        public void FillRectangle(System.Drawing.Brush brush, System.Drawing.Rectangle rect) { }
        public void FillRectangle(System.Drawing.Brush brush, System.Drawing.RectangleF rect) { }
        public void FillRectangle(System.Drawing.Brush brush, int x, int y, int width, int height) { }
        public void FillRectangle(System.Drawing.Brush brush, float x, float y, float width, float height) { }
        public void FillRectangles(System.Drawing.Brush brush, System.Drawing.RectangleF[] rects) { }
        public void FillRectangles(System.Drawing.Brush brush, System.Drawing.Rectangle[] rects) { }
        public void FillRegion(System.Drawing.Brush brush, System.Drawing.Region region) { }
        ~Graphics() { }
        public void Flush() { }
        public void Flush(System.Drawing.Drawing2D.FlushIntention intention) { }
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(2))]
        public static System.Drawing.Graphics FromHdc(System.IntPtr hdc) { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(2))]
        public static System.Drawing.Graphics FromHdc(System.IntPtr hdc, System.IntPtr hdevice) { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(2))]
        public static System.Drawing.Graphics FromHdcInternal(System.IntPtr hdc) { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(2))]
        public static System.Drawing.Graphics FromHwnd(System.IntPtr hwnd) { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(2))]
        public static System.Drawing.Graphics FromHwndInternal(System.IntPtr hwnd) { throw null; }
        public static System.Drawing.Graphics FromImage(System.Drawing.Image image) { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
        public object GetContextInfo() { throw null; }
        public static System.IntPtr GetHalftonePalette() { throw null; }
        public System.IntPtr GetHdc() { throw null; }
        public System.Drawing.Color GetNearestColor(System.Drawing.Color color) { throw null; }
        public void IntersectClip(System.Drawing.Rectangle rect) { }
        public void IntersectClip(System.Drawing.RectangleF rect) { }
        public void IntersectClip(System.Drawing.Region region) { }
        public bool IsVisible(System.Drawing.Point point) { throw null; }
        public bool IsVisible(System.Drawing.PointF point) { throw null; }
        public bool IsVisible(System.Drawing.Rectangle rect) { throw null; }
        public bool IsVisible(System.Drawing.RectangleF rect) { throw null; }
        public bool IsVisible(int x, int y) { throw null; }
        public bool IsVisible(int x, int y, int width, int height) { throw null; }
        public bool IsVisible(float x, float y) { throw null; }
        public bool IsVisible(float x, float y, float width, float height) { throw null; }
        public System.Drawing.Region[] MeasureCharacterRanges(string text, System.Drawing.Font font, System.Drawing.RectangleF layoutRect, System.Drawing.StringFormat stringFormat) { throw null; }
        public System.Drawing.SizeF MeasureString(string text, System.Drawing.Font font) { throw null; }
        public System.Drawing.SizeF MeasureString(string text, System.Drawing.Font font, System.Drawing.PointF origin, System.Drawing.StringFormat stringFormat) { throw null; }
        public System.Drawing.SizeF MeasureString(string text, System.Drawing.Font font, System.Drawing.SizeF layoutArea) { throw null; }
        public System.Drawing.SizeF MeasureString(string text, System.Drawing.Font font, System.Drawing.SizeF layoutArea, System.Drawing.StringFormat stringFormat) { throw null; }
        public System.Drawing.SizeF MeasureString(string text, System.Drawing.Font font, System.Drawing.SizeF layoutArea, System.Drawing.StringFormat stringFormat, out int charactersFitted, out int linesFilled) { throw null; }
        public System.Drawing.SizeF MeasureString(string text, System.Drawing.Font font, int width) { throw null; }
        public System.Drawing.SizeF MeasureString(string text, System.Drawing.Font font, int width, System.Drawing.StringFormat format) { throw null; }
        public void MultiplyTransform(System.Drawing.Drawing2D.Matrix matrix) { }
        public void MultiplyTransform(System.Drawing.Drawing2D.Matrix matrix, System.Drawing.Drawing2D.MatrixOrder order) { }
        public void ReleaseHdc() { }
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(2))]
        public void ReleaseHdc(System.IntPtr hdc) { }
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
        public void ReleaseHdcInternal(System.IntPtr hdc) { }
        public void ResetClip() { }
        public void ResetTransform() { }
        public void Restore(System.Drawing.Drawing2D.GraphicsState gstate) { }
        public void RotateTransform(float angle) { }
        public void RotateTransform(float angle, System.Drawing.Drawing2D.MatrixOrder order) { }
        public System.Drawing.Drawing2D.GraphicsState Save() { throw null; }
        public void ScaleTransform(float sx, float sy) { }
        public void ScaleTransform(float sx, float sy, System.Drawing.Drawing2D.MatrixOrder order) { }
        public void SetClip(System.Drawing.Drawing2D.GraphicsPath path) { }
        public void SetClip(System.Drawing.Drawing2D.GraphicsPath path, System.Drawing.Drawing2D.CombineMode combineMode) { }
        public void SetClip(System.Drawing.Graphics g) { }
        public void SetClip(System.Drawing.Graphics g, System.Drawing.Drawing2D.CombineMode combineMode) { }
        public void SetClip(System.Drawing.Rectangle rect) { }
        public void SetClip(System.Drawing.Rectangle rect, System.Drawing.Drawing2D.CombineMode combineMode) { }
        public void SetClip(System.Drawing.RectangleF rect) { }
        public void SetClip(System.Drawing.RectangleF rect, System.Drawing.Drawing2D.CombineMode combineMode) { }
        public void SetClip(System.Drawing.Region region, System.Drawing.Drawing2D.CombineMode combineMode) { }
        public void TransformPoints(System.Drawing.Drawing2D.CoordinateSpace destSpace, System.Drawing.Drawing2D.CoordinateSpace srcSpace, System.Drawing.PointF[] pts) { }
        public void TransformPoints(System.Drawing.Drawing2D.CoordinateSpace destSpace, System.Drawing.Drawing2D.CoordinateSpace srcSpace, System.Drawing.Point[] pts) { }
        public void TranslateClip(int dx, int dy) { }
        public void TranslateClip(float dx, float dy) { }
        public void TranslateTransform(float dx, float dy) { }
        public void TranslateTransform(float dx, float dy, System.Drawing.Drawing2D.MatrixOrder order) { }
        public delegate bool DrawImageAbort(System.IntPtr callbackdata);
        public delegate bool EnumerateMetafileProc(System.Drawing.Imaging.EmfPlusRecordType recordType, int flags, int dataSize, System.IntPtr data, System.Drawing.Imaging.PlayRecordCallback callbackData);
    }
    public enum GraphicsUnit
    {
        Display = 1,
        Document = 5,
        Inch = 4,
        Millimeter = 6,
        Pixel = 2,
        Point = 3,
        World = 0,
    }
    public sealed partial class Icon : System.MarshalByRefObject, System.ICloneable, System.IDisposable, System.Runtime.Serialization.ISerializable
    {
        public Icon(System.Drawing.Icon original, System.Drawing.Size size) { }
        public Icon(System.Drawing.Icon original, int width, int height) { }
        public Icon(System.IO.Stream stream) { }
        public Icon(System.IO.Stream stream, System.Drawing.Size size) { }
        public Icon(System.IO.Stream stream, int width, int height) { }
        public Icon(string fileName) { }
        public Icon(string fileName, System.Drawing.Size size) { }
        public Icon(string fileName, int width, int height) { }
        public Icon(System.Type type, string resource) { }
        [System.ComponentModel.BrowsableAttribute(false)]
        public System.IntPtr Handle { get { throw null; } }
        [System.ComponentModel.BrowsableAttribute(false)]
        public int Height { get { throw null; } }
        public System.Drawing.Size Size { get { throw null; } }
        [System.ComponentModel.BrowsableAttribute(false)]
        public int Width { get { throw null; } }
        public object Clone() { throw null; }
        public void Dispose() { }
        public static System.Drawing.Icon ExtractAssociatedIcon(string filePath) { throw null; }
        ~Icon() { }
        public static System.Drawing.Icon FromHandle(System.IntPtr handle) { throw null; }
        public void Save(System.IO.Stream outputStream) { }
        void System.Runtime.Serialization.ISerializable.GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public System.Drawing.Bitmap ToBitmap() { throw null; }
        public override string ToString() { throw null; }
    }
    public partial interface IDeviceContext : System.IDisposable
    {
        System.IntPtr GetHdc();
        void ReleaseHdc();
    }
    [System.ComponentModel.ImmutableObjectAttribute(true)]
    public abstract partial class Image : System.MarshalByRefObject, System.ICloneable, System.IDisposable, System.Runtime.Serialization.ISerializable
    {
        internal Image() { }
        [System.ComponentModel.BrowsableAttribute(false)]
        public int Flags { get { throw null; } }
        [System.ComponentModel.BrowsableAttribute(false)]
        public System.Guid[] FrameDimensionsList { get { throw null; } }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.DefaultValueAttribute(false)]
        [System.ComponentModel.DesignerSerializationVisibilityAttribute((System.ComponentModel.DesignerSerializationVisibility)(0))]
        public int Height { get { throw null; } }
        public float HorizontalResolution { get { throw null; } }
        [System.ComponentModel.BrowsableAttribute(false)]
        public System.Drawing.Imaging.ColorPalette Palette { get { throw null; } set { } }
        public System.Drawing.SizeF PhysicalDimension { get { throw null; } }
        public System.Drawing.Imaging.PixelFormat PixelFormat { get { throw null; } }
        [System.ComponentModel.BrowsableAttribute(false)]
        public int[] PropertyIdList { get { throw null; } }
        [System.ComponentModel.BrowsableAttribute(false)]
        public System.Drawing.Imaging.PropertyItem[] PropertyItems { get { throw null; } }
        public System.Drawing.Imaging.ImageFormat RawFormat { get { throw null; } }
        public System.Drawing.Size Size { get { throw null; } }
        [System.ComponentModel.DefaultValueAttribute(null)]
        [System.ComponentModel.LocalizableAttribute(false)]
        public object Tag { get { throw null; } set { } }
        public float VerticalResolution { get { throw null; } }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.DefaultValueAttribute(false)]
        [System.ComponentModel.DesignerSerializationVisibilityAttribute((System.ComponentModel.DesignerSerializationVisibility)(0))]
        public int Width { get { throw null; } }
        public object Clone() { throw null; }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        ~Image() { }
        public static System.Drawing.Image FromFile(string filename) { throw null; }
        public static System.Drawing.Image FromFile(string filename, bool useEmbeddedColorManagement) { throw null; }
        public static System.Drawing.Bitmap FromHbitmap(System.IntPtr hbitmap) { throw null; }
        public static System.Drawing.Bitmap FromHbitmap(System.IntPtr hbitmap, System.IntPtr hpalette) { throw null; }
        public static System.Drawing.Image FromStream(System.IO.Stream stream) { throw null; }
        public static System.Drawing.Image FromStream(System.IO.Stream stream, bool useEmbeddedColorManagement) { throw null; }
        public static System.Drawing.Image FromStream(System.IO.Stream stream, bool useEmbeddedColorManagement, bool validateImageData) { throw null; }
        public System.Drawing.RectangleF GetBounds(ref System.Drawing.GraphicsUnit pageUnit) { throw null; }
        public System.Drawing.Imaging.EncoderParameters GetEncoderParameterList(System.Guid encoder) { throw null; }
        public int GetFrameCount(System.Drawing.Imaging.FrameDimension dimension) { throw null; }
        public static int GetPixelFormatSize(System.Drawing.Imaging.PixelFormat pixfmt) { throw null; }
        public System.Drawing.Imaging.PropertyItem GetPropertyItem(int propid) { throw null; }
        public System.Drawing.Image GetThumbnailImage(int thumbWidth, int thumbHeight, System.Drawing.Image.GetThumbnailImageAbort callback, System.IntPtr callbackData) { throw null; }
        public static bool IsAlphaPixelFormat(System.Drawing.Imaging.PixelFormat pixfmt) { throw null; }
        public static bool IsCanonicalPixelFormat(System.Drawing.Imaging.PixelFormat pixfmt) { throw null; }
        public static bool IsExtendedPixelFormat(System.Drawing.Imaging.PixelFormat pixfmt) { throw null; }
        public void RemovePropertyItem(int propid) { }
        public void RotateFlip(System.Drawing.RotateFlipType rotateFlipType) { }
        public void Save(System.IO.Stream stream, System.Drawing.Imaging.ImageCodecInfo encoder, System.Drawing.Imaging.EncoderParameters encoderParams) { }
        public void Save(System.IO.Stream stream, System.Drawing.Imaging.ImageFormat format) { }
        public void Save(string filename) { }
        public void Save(string filename, System.Drawing.Imaging.ImageCodecInfo encoder, System.Drawing.Imaging.EncoderParameters encoderParams) { }
        public void Save(string filename, System.Drawing.Imaging.ImageFormat format) { }
        public void SaveAdd(System.Drawing.Image image, System.Drawing.Imaging.EncoderParameters encoderParams) { }
        public void SaveAdd(System.Drawing.Imaging.EncoderParameters encoderParams) { }
        public int SelectActiveFrame(System.Drawing.Imaging.FrameDimension dimension, int frameIndex) { throw null; }
        public void SetPropertyItem(System.Drawing.Imaging.PropertyItem propitem) { }
        void System.Runtime.Serialization.ISerializable.GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public delegate bool GetThumbnailImageAbort();
    }
    public sealed partial class ImageAnimator
    {
        internal ImageAnimator() { }
        public static void Animate(System.Drawing.Image image, System.EventHandler onFrameChangedHandler) { }
        public static bool CanAnimate(System.Drawing.Image image) { throw null; }
        public static void StopAnimate(System.Drawing.Image image, System.EventHandler onFrameChangedHandler) { }
        public static void UpdateFrames() { }
        public static void UpdateFrames(System.Drawing.Image image) { }
    }
    public sealed partial class Pen : System.MarshalByRefObject, System.ICloneable, System.IDisposable
    {
        public Pen(System.Drawing.Brush brush) { }
        public Pen(System.Drawing.Brush brush, float width) { }
        public Pen(System.Drawing.Color color) { }
        public Pen(System.Drawing.Color color, float width) { }
        public System.Drawing.Drawing2D.PenAlignment Alignment { get { throw null; } set { } }
        public System.Drawing.Brush Brush { get { throw null; } set { } }
        public System.Drawing.Color Color { get { throw null; } set { } }
        public float[] CompoundArray { get { throw null; } set { } }
        public System.Drawing.Drawing2D.CustomLineCap CustomEndCap { get { throw null; } set { } }
        public System.Drawing.Drawing2D.CustomLineCap CustomStartCap { get { throw null; } set { } }
        public System.Drawing.Drawing2D.DashCap DashCap { get { throw null; } set { } }
        public float DashOffset { get { throw null; } set { } }
        public float[] DashPattern { get { throw null; } set { } }
        public System.Drawing.Drawing2D.DashStyle DashStyle { get { throw null; } set { } }
        public System.Drawing.Drawing2D.LineCap EndCap { get { throw null; } set { } }
        public System.Drawing.Drawing2D.LineJoin LineJoin { get { throw null; } set { } }
        public float MiterLimit { get { throw null; } set { } }
        public System.Drawing.Drawing2D.PenType PenType { get { throw null; } }
        public System.Drawing.Drawing2D.LineCap StartCap { get { throw null; } set { } }
        public System.Drawing.Drawing2D.Matrix Transform { get { throw null; } set { } }
        public float Width { get { throw null; } set { } }
        public object Clone() { throw null; }
        public void Dispose() { }
        ~Pen() { }
        public void MultiplyTransform(System.Drawing.Drawing2D.Matrix matrix) { }
        public void MultiplyTransform(System.Drawing.Drawing2D.Matrix matrix, System.Drawing.Drawing2D.MatrixOrder order) { }
        public void ResetTransform() { }
        public void RotateTransform(float angle) { }
        public void RotateTransform(float angle, System.Drawing.Drawing2D.MatrixOrder order) { }
        public void ScaleTransform(float sx, float sy) { }
        public void ScaleTransform(float sx, float sy, System.Drawing.Drawing2D.MatrixOrder order) { }
        public void SetLineCap(System.Drawing.Drawing2D.LineCap startCap, System.Drawing.Drawing2D.LineCap endCap, System.Drawing.Drawing2D.DashCap dashCap) { }
        public void TranslateTransform(float dx, float dy) { }
        public void TranslateTransform(float dx, float dy, System.Drawing.Drawing2D.MatrixOrder order) { }
    }
    public static partial class Pens
    {
        public static System.Drawing.Pen AliceBlue { get { throw null; } }
        public static System.Drawing.Pen AntiqueWhite { get { throw null; } }
        public static System.Drawing.Pen Aqua { get { throw null; } }
        public static System.Drawing.Pen Aquamarine { get { throw null; } }
        public static System.Drawing.Pen Azure { get { throw null; } }
        public static System.Drawing.Pen Beige { get { throw null; } }
        public static System.Drawing.Pen Bisque { get { throw null; } }
        public static System.Drawing.Pen Black { get { throw null; } }
        public static System.Drawing.Pen BlanchedAlmond { get { throw null; } }
        public static System.Drawing.Pen Blue { get { throw null; } }
        public static System.Drawing.Pen BlueViolet { get { throw null; } }
        public static System.Drawing.Pen Brown { get { throw null; } }
        public static System.Drawing.Pen BurlyWood { get { throw null; } }
        public static System.Drawing.Pen CadetBlue { get { throw null; } }
        public static System.Drawing.Pen Chartreuse { get { throw null; } }
        public static System.Drawing.Pen Chocolate { get { throw null; } }
        public static System.Drawing.Pen Coral { get { throw null; } }
        public static System.Drawing.Pen CornflowerBlue { get { throw null; } }
        public static System.Drawing.Pen Cornsilk { get { throw null; } }
        public static System.Drawing.Pen Crimson { get { throw null; } }
        public static System.Drawing.Pen Cyan { get { throw null; } }
        public static System.Drawing.Pen DarkBlue { get { throw null; } }
        public static System.Drawing.Pen DarkCyan { get { throw null; } }
        public static System.Drawing.Pen DarkGoldenrod { get { throw null; } }
        public static System.Drawing.Pen DarkGray { get { throw null; } }
        public static System.Drawing.Pen DarkGreen { get { throw null; } }
        public static System.Drawing.Pen DarkKhaki { get { throw null; } }
        public static System.Drawing.Pen DarkMagenta { get { throw null; } }
        public static System.Drawing.Pen DarkOliveGreen { get { throw null; } }
        public static System.Drawing.Pen DarkOrange { get { throw null; } }
        public static System.Drawing.Pen DarkOrchid { get { throw null; } }
        public static System.Drawing.Pen DarkRed { get { throw null; } }
        public static System.Drawing.Pen DarkSalmon { get { throw null; } }
        public static System.Drawing.Pen DarkSeaGreen { get { throw null; } }
        public static System.Drawing.Pen DarkSlateBlue { get { throw null; } }
        public static System.Drawing.Pen DarkSlateGray { get { throw null; } }
        public static System.Drawing.Pen DarkTurquoise { get { throw null; } }
        public static System.Drawing.Pen DarkViolet { get { throw null; } }
        public static System.Drawing.Pen DeepPink { get { throw null; } }
        public static System.Drawing.Pen DeepSkyBlue { get { throw null; } }
        public static System.Drawing.Pen DimGray { get { throw null; } }
        public static System.Drawing.Pen DodgerBlue { get { throw null; } }
        public static System.Drawing.Pen Firebrick { get { throw null; } }
        public static System.Drawing.Pen FloralWhite { get { throw null; } }
        public static System.Drawing.Pen ForestGreen { get { throw null; } }
        public static System.Drawing.Pen Fuchsia { get { throw null; } }
        public static System.Drawing.Pen Gainsboro { get { throw null; } }
        public static System.Drawing.Pen GhostWhite { get { throw null; } }
        public static System.Drawing.Pen Gold { get { throw null; } }
        public static System.Drawing.Pen Goldenrod { get { throw null; } }
        public static System.Drawing.Pen Gray { get { throw null; } }
        public static System.Drawing.Pen Green { get { throw null; } }
        public static System.Drawing.Pen GreenYellow { get { throw null; } }
        public static System.Drawing.Pen Honeydew { get { throw null; } }
        public static System.Drawing.Pen HotPink { get { throw null; } }
        public static System.Drawing.Pen IndianRed { get { throw null; } }
        public static System.Drawing.Pen Indigo { get { throw null; } }
        public static System.Drawing.Pen Ivory { get { throw null; } }
        public static System.Drawing.Pen Khaki { get { throw null; } }
        public static System.Drawing.Pen Lavender { get { throw null; } }
        public static System.Drawing.Pen LavenderBlush { get { throw null; } }
        public static System.Drawing.Pen LawnGreen { get { throw null; } }
        public static System.Drawing.Pen LemonChiffon { get { throw null; } }
        public static System.Drawing.Pen LightBlue { get { throw null; } }
        public static System.Drawing.Pen LightCoral { get { throw null; } }
        public static System.Drawing.Pen LightCyan { get { throw null; } }
        public static System.Drawing.Pen LightGoldenrodYellow { get { throw null; } }
        public static System.Drawing.Pen LightGray { get { throw null; } }
        public static System.Drawing.Pen LightGreen { get { throw null; } }
        public static System.Drawing.Pen LightPink { get { throw null; } }
        public static System.Drawing.Pen LightSalmon { get { throw null; } }
        public static System.Drawing.Pen LightSeaGreen { get { throw null; } }
        public static System.Drawing.Pen LightSkyBlue { get { throw null; } }
        public static System.Drawing.Pen LightSlateGray { get { throw null; } }
        public static System.Drawing.Pen LightSteelBlue { get { throw null; } }
        public static System.Drawing.Pen LightYellow { get { throw null; } }
        public static System.Drawing.Pen Lime { get { throw null; } }
        public static System.Drawing.Pen LimeGreen { get { throw null; } }
        public static System.Drawing.Pen Linen { get { throw null; } }
        public static System.Drawing.Pen Magenta { get { throw null; } }
        public static System.Drawing.Pen Maroon { get { throw null; } }
        public static System.Drawing.Pen MediumAquamarine { get { throw null; } }
        public static System.Drawing.Pen MediumBlue { get { throw null; } }
        public static System.Drawing.Pen MediumOrchid { get { throw null; } }
        public static System.Drawing.Pen MediumPurple { get { throw null; } }
        public static System.Drawing.Pen MediumSeaGreen { get { throw null; } }
        public static System.Drawing.Pen MediumSlateBlue { get { throw null; } }
        public static System.Drawing.Pen MediumSpringGreen { get { throw null; } }
        public static System.Drawing.Pen MediumTurquoise { get { throw null; } }
        public static System.Drawing.Pen MediumVioletRed { get { throw null; } }
        public static System.Drawing.Pen MidnightBlue { get { throw null; } }
        public static System.Drawing.Pen MintCream { get { throw null; } }
        public static System.Drawing.Pen MistyRose { get { throw null; } }
        public static System.Drawing.Pen Moccasin { get { throw null; } }
        public static System.Drawing.Pen NavajoWhite { get { throw null; } }
        public static System.Drawing.Pen Navy { get { throw null; } }
        public static System.Drawing.Pen OldLace { get { throw null; } }
        public static System.Drawing.Pen Olive { get { throw null; } }
        public static System.Drawing.Pen OliveDrab { get { throw null; } }
        public static System.Drawing.Pen Orange { get { throw null; } }
        public static System.Drawing.Pen OrangeRed { get { throw null; } }
        public static System.Drawing.Pen Orchid { get { throw null; } }
        public static System.Drawing.Pen PaleGoldenrod { get { throw null; } }
        public static System.Drawing.Pen PaleGreen { get { throw null; } }
        public static System.Drawing.Pen PaleTurquoise { get { throw null; } }
        public static System.Drawing.Pen PaleVioletRed { get { throw null; } }
        public static System.Drawing.Pen PapayaWhip { get { throw null; } }
        public static System.Drawing.Pen PeachPuff { get { throw null; } }
        public static System.Drawing.Pen Peru { get { throw null; } }
        public static System.Drawing.Pen Pink { get { throw null; } }
        public static System.Drawing.Pen Plum { get { throw null; } }
        public static System.Drawing.Pen PowderBlue { get { throw null; } }
        public static System.Drawing.Pen Purple { get { throw null; } }
        public static System.Drawing.Pen Red { get { throw null; } }
        public static System.Drawing.Pen RosyBrown { get { throw null; } }
        public static System.Drawing.Pen RoyalBlue { get { throw null; } }
        public static System.Drawing.Pen SaddleBrown { get { throw null; } }
        public static System.Drawing.Pen Salmon { get { throw null; } }
        public static System.Drawing.Pen SandyBrown { get { throw null; } }
        public static System.Drawing.Pen SeaGreen { get { throw null; } }
        public static System.Drawing.Pen SeaShell { get { throw null; } }
        public static System.Drawing.Pen Sienna { get { throw null; } }
        public static System.Drawing.Pen Silver { get { throw null; } }
        public static System.Drawing.Pen SkyBlue { get { throw null; } }
        public static System.Drawing.Pen SlateBlue { get { throw null; } }
        public static System.Drawing.Pen SlateGray { get { throw null; } }
        public static System.Drawing.Pen Snow { get { throw null; } }
        public static System.Drawing.Pen SpringGreen { get { throw null; } }
        public static System.Drawing.Pen SteelBlue { get { throw null; } }
        public static System.Drawing.Pen Tan { get { throw null; } }
        public static System.Drawing.Pen Teal { get { throw null; } }
        public static System.Drawing.Pen Thistle { get { throw null; } }
        public static System.Drawing.Pen Tomato { get { throw null; } }
        public static System.Drawing.Pen Transparent { get { throw null; } }
        public static System.Drawing.Pen Turquoise { get { throw null; } }
        public static System.Drawing.Pen Violet { get { throw null; } }
        public static System.Drawing.Pen Wheat { get { throw null; } }
        public static System.Drawing.Pen White { get { throw null; } }
        public static System.Drawing.Pen WhiteSmoke { get { throw null; } }
        public static System.Drawing.Pen Yellow { get { throw null; } }
        public static System.Drawing.Pen YellowGreen { get { throw null; } }
    }
    public sealed partial class Region : System.MarshalByRefObject, System.IDisposable
    {
        public Region() { }
        public Region(System.Drawing.Drawing2D.GraphicsPath path) { }
        public Region(System.Drawing.Drawing2D.RegionData rgnData) { }
        public Region(System.Drawing.Rectangle rect) { }
        public Region(System.Drawing.RectangleF rect) { }
        public System.Drawing.Region Clone() { throw null; }
        public void Complement(System.Drawing.Drawing2D.GraphicsPath path) { }
        public void Complement(System.Drawing.Rectangle rect) { }
        public void Complement(System.Drawing.RectangleF rect) { }
        public void Complement(System.Drawing.Region region) { }
        public void Dispose() { }
        public bool Equals(System.Drawing.Region region, System.Drawing.Graphics g) { throw null; }
        public void Exclude(System.Drawing.Drawing2D.GraphicsPath path) { }
        public void Exclude(System.Drawing.Rectangle rect) { }
        public void Exclude(System.Drawing.RectangleF rect) { }
        public void Exclude(System.Drawing.Region region) { }
        ~Region() { }
        public static System.Drawing.Region FromHrgn(System.IntPtr hrgn) { throw null; }
        public System.Drawing.RectangleF GetBounds(System.Drawing.Graphics g) { throw null; }
        public System.IntPtr GetHrgn(System.Drawing.Graphics g) { throw null; }
        public System.Drawing.Drawing2D.RegionData GetRegionData() { throw null; }
        public System.Drawing.RectangleF[] GetRegionScans(System.Drawing.Drawing2D.Matrix matrix) { throw null; }
        public void Intersect(System.Drawing.Drawing2D.GraphicsPath path) { }
        public void Intersect(System.Drawing.Rectangle rect) { }
        public void Intersect(System.Drawing.RectangleF rect) { }
        public void Intersect(System.Drawing.Region region) { }
        public bool IsEmpty(System.Drawing.Graphics g) { throw null; }
        public bool IsInfinite(System.Drawing.Graphics g) { throw null; }
        public bool IsVisible(System.Drawing.Point point) { throw null; }
        public bool IsVisible(System.Drawing.Point point, System.Drawing.Graphics g) { throw null; }
        public bool IsVisible(System.Drawing.PointF point) { throw null; }
        public bool IsVisible(System.Drawing.PointF point, System.Drawing.Graphics g) { throw null; }
        public bool IsVisible(System.Drawing.Rectangle rect) { throw null; }
        public bool IsVisible(System.Drawing.Rectangle rect, System.Drawing.Graphics g) { throw null; }
        public bool IsVisible(System.Drawing.RectangleF rect) { throw null; }
        public bool IsVisible(System.Drawing.RectangleF rect, System.Drawing.Graphics g) { throw null; }
        public bool IsVisible(int x, int y, System.Drawing.Graphics g) { throw null; }
        public bool IsVisible(int x, int y, int width, int height) { throw null; }
        public bool IsVisible(int x, int y, int width, int height, System.Drawing.Graphics g) { throw null; }
        public bool IsVisible(float x, float y) { throw null; }
        public bool IsVisible(float x, float y, System.Drawing.Graphics g) { throw null; }
        public bool IsVisible(float x, float y, float width, float height) { throw null; }
        public bool IsVisible(float x, float y, float width, float height, System.Drawing.Graphics g) { throw null; }
        public void MakeEmpty() { }
        public void MakeInfinite() { }
        public void ReleaseHrgn(System.IntPtr regionHandle) { }
        public void Transform(System.Drawing.Drawing2D.Matrix matrix) { }
        public void Translate(int dx, int dy) { }
        public void Translate(float dx, float dy) { }
        public void Union(System.Drawing.Drawing2D.GraphicsPath path) { }
        public void Union(System.Drawing.Rectangle rect) { }
        public void Union(System.Drawing.RectangleF rect) { }
        public void Union(System.Drawing.Region region) { }
        public void Xor(System.Drawing.Drawing2D.GraphicsPath path) { }
        public void Xor(System.Drawing.Rectangle rect) { }
        public void Xor(System.Drawing.RectangleF rect) { }
        public void Xor(System.Drawing.Region region) { }
    }
    public enum RotateFlipType
    {
        Rotate180FlipNone = 2,
        Rotate180FlipX = 6,
        Rotate180FlipXY = 0,
        Rotate180FlipY = 4,
        Rotate270FlipNone = 3,
        Rotate270FlipX = 7,
        Rotate270FlipXY = 1,
        Rotate270FlipY = 5,
        Rotate90FlipNone = 1,
        Rotate90FlipX = 5,
        Rotate90FlipXY = 3,
        Rotate90FlipY = 7,
        RotateNoneFlipNone = 0,
        RotateNoneFlipX = 4,
        RotateNoneFlipXY = 2,
        RotateNoneFlipY = 6,
    }
    public sealed partial class SolidBrush : System.Drawing.Brush
    {
        public SolidBrush(System.Drawing.Color color) { }
        public System.Drawing.Color Color { get { throw null; } set { } }
        public override object Clone() { throw null; }
        protected override void Dispose(bool disposing) { }
    }
    public enum StringAlignment
    {
        Center = 1,
        Far = 2,
        Near = 0,
    }
    public enum StringDigitSubstitute
    {
        National = 2,
        None = 1,
        Traditional = 3,
        User = 0,
    }
    public sealed partial class StringFormat : System.MarshalByRefObject, System.ICloneable, System.IDisposable
    {
        public StringFormat() { }
        public StringFormat(System.Drawing.StringFormat format) { }
        public StringFormat(System.Drawing.StringFormatFlags options) { }
        public StringFormat(System.Drawing.StringFormatFlags options, int language) { }
        public System.Drawing.StringAlignment Alignment { get { throw null; } set { } }
        public int DigitSubstitutionLanguage { get { throw null; } }
        public System.Drawing.StringDigitSubstitute DigitSubstitutionMethod { get { throw null; } }
        public System.Drawing.StringFormatFlags FormatFlags { get { throw null; } set { } }
        public static System.Drawing.StringFormat GenericDefault { get { throw null; } }
        public static System.Drawing.StringFormat GenericTypographic { get { throw null; } }
        public System.Drawing.Text.HotkeyPrefix HotkeyPrefix { get { throw null; } set { } }
        public System.Drawing.StringAlignment LineAlignment { get { throw null; } set { } }
        public System.Drawing.StringTrimming Trimming { get { throw null; } set { } }
        public object Clone() { throw null; }
        public void Dispose() { }
        ~StringFormat() { }
        public float[] GetTabStops(out float firstTabOffset) { throw null; }
        public void SetDigitSubstitution(int language, System.Drawing.StringDigitSubstitute substitute) { }
        public void SetMeasurableCharacterRanges(System.Drawing.CharacterRange[] ranges) { }
        public void SetTabStops(float firstTabOffset, float[] tabStops) { }
        public override string ToString() { throw null; }
    }
    [System.FlagsAttribute]
    public enum StringFormatFlags
    {
        DirectionRightToLeft = 1,
        DirectionVertical = 2,
        DisplayFormatControl = 32,
        FitBlackBox = 4,
        LineLimit = 8192,
        MeasureTrailingSpaces = 2048,
        NoClip = 16384,
        NoFontFallback = 1024,
        NoWrap = 4096,
    }
    public enum StringTrimming
    {
        Character = 1,
        EllipsisCharacter = 3,
        EllipsisPath = 5,
        EllipsisWord = 4,
        None = 0,
        Word = 2,
    }
    public enum StringUnit
    {
        Display = 1,
        Document = 5,
        Em = 32,
        Inch = 4,
        Millimeter = 6,
        Pixel = 2,
        Point = 3,
        World = 0,
    }
    public static partial class SystemBrushes
    {
        public static System.Drawing.Brush ActiveBorder { get { throw null; } }
        public static System.Drawing.Brush ActiveCaption { get { throw null; } }
        public static System.Drawing.Brush ActiveCaptionText { get { throw null; } }
        public static System.Drawing.Brush AppWorkspace { get { throw null; } }
        public static System.Drawing.Brush ButtonFace { get { throw null; } }
        public static System.Drawing.Brush ButtonHighlight { get { throw null; } }
        public static System.Drawing.Brush ButtonShadow { get { throw null; } }
        public static System.Drawing.Brush Control { get { throw null; } }
        public static System.Drawing.Brush ControlDark { get { throw null; } }
        public static System.Drawing.Brush ControlDarkDark { get { throw null; } }
        public static System.Drawing.Brush ControlLight { get { throw null; } }
        public static System.Drawing.Brush ControlLightLight { get { throw null; } }
        public static System.Drawing.Brush ControlText { get { throw null; } }
        public static System.Drawing.Brush Desktop { get { throw null; } }
        public static System.Drawing.Brush GradientActiveCaption { get { throw null; } }
        public static System.Drawing.Brush GradientInactiveCaption { get { throw null; } }
        public static System.Drawing.Brush GrayText { get { throw null; } }
        public static System.Drawing.Brush Highlight { get { throw null; } }
        public static System.Drawing.Brush HighlightText { get { throw null; } }
        public static System.Drawing.Brush HotTrack { get { throw null; } }
        public static System.Drawing.Brush InactiveBorder { get { throw null; } }
        public static System.Drawing.Brush InactiveCaption { get { throw null; } }
        public static System.Drawing.Brush InactiveCaptionText { get { throw null; } }
        public static System.Drawing.Brush Info { get { throw null; } }
        public static System.Drawing.Brush InfoText { get { throw null; } }
        public static System.Drawing.Brush Menu { get { throw null; } }
        public static System.Drawing.Brush MenuBar { get { throw null; } }
        public static System.Drawing.Brush MenuHighlight { get { throw null; } }
        public static System.Drawing.Brush MenuText { get { throw null; } }
        public static System.Drawing.Brush ScrollBar { get { throw null; } }
        public static System.Drawing.Brush Window { get { throw null; } }
        public static System.Drawing.Brush WindowFrame { get { throw null; } }
        public static System.Drawing.Brush WindowText { get { throw null; } }
        public static System.Drawing.Brush FromSystemColor(System.Drawing.Color c) { throw null; }
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
    public static partial class SystemFonts
    {
        public static System.Drawing.Font CaptionFont { get { throw null; } }
        public static System.Drawing.Font DefaultFont { get { throw null; } }
        public static System.Drawing.Font DialogFont { get { throw null; } }
        public static System.Drawing.Font IconTitleFont { get { throw null; } }
        public static System.Drawing.Font MenuFont { get { throw null; } }
        public static System.Drawing.Font MessageBoxFont { get { throw null; } }
        public static System.Drawing.Font SmallCaptionFont { get { throw null; } }
        public static System.Drawing.Font StatusFont { get { throw null; } }
        public static System.Drawing.Font GetFontByName(string systemFontName) { throw null; }
    }
    public static partial class SystemIcons
    {
        public static System.Drawing.Icon Application { get { throw null; } }
        public static System.Drawing.Icon Asterisk { get { throw null; } }
        public static System.Drawing.Icon Error { get { throw null; } }
        public static System.Drawing.Icon Exclamation { get { throw null; } }
        public static System.Drawing.Icon Hand { get { throw null; } }
        public static System.Drawing.Icon Information { get { throw null; } }
        public static System.Drawing.Icon Question { get { throw null; } }
        public static System.Drawing.Icon Shield { get { throw null; } }
        public static System.Drawing.Icon Warning { get { throw null; } }
        public static System.Drawing.Icon WinLogo { get { throw null; } }
    }
    public static partial class SystemPens
    {
        public static System.Drawing.Pen ActiveBorder { get { throw null; } }
        public static System.Drawing.Pen ActiveCaption { get { throw null; } }
        public static System.Drawing.Pen ActiveCaptionText { get { throw null; } }
        public static System.Drawing.Pen AppWorkspace { get { throw null; } }
        public static System.Drawing.Pen ButtonFace { get { throw null; } }
        public static System.Drawing.Pen ButtonHighlight { get { throw null; } }
        public static System.Drawing.Pen ButtonShadow { get { throw null; } }
        public static System.Drawing.Pen Control { get { throw null; } }
        public static System.Drawing.Pen ControlDark { get { throw null; } }
        public static System.Drawing.Pen ControlDarkDark { get { throw null; } }
        public static System.Drawing.Pen ControlLight { get { throw null; } }
        public static System.Drawing.Pen ControlLightLight { get { throw null; } }
        public static System.Drawing.Pen ControlText { get { throw null; } }
        public static System.Drawing.Pen Desktop { get { throw null; } }
        public static System.Drawing.Pen GradientActiveCaption { get { throw null; } }
        public static System.Drawing.Pen GradientInactiveCaption { get { throw null; } }
        public static System.Drawing.Pen GrayText { get { throw null; } }
        public static System.Drawing.Pen Highlight { get { throw null; } }
        public static System.Drawing.Pen HighlightText { get { throw null; } }
        public static System.Drawing.Pen HotTrack { get { throw null; } }
        public static System.Drawing.Pen InactiveBorder { get { throw null; } }
        public static System.Drawing.Pen InactiveCaption { get { throw null; } }
        public static System.Drawing.Pen InactiveCaptionText { get { throw null; } }
        public static System.Drawing.Pen Info { get { throw null; } }
        public static System.Drawing.Pen InfoText { get { throw null; } }
        public static System.Drawing.Pen Menu { get { throw null; } }
        public static System.Drawing.Pen MenuBar { get { throw null; } }
        public static System.Drawing.Pen MenuHighlight { get { throw null; } }
        public static System.Drawing.Pen MenuText { get { throw null; } }
        public static System.Drawing.Pen ScrollBar { get { throw null; } }
        public static System.Drawing.Pen Window { get { throw null; } }
        public static System.Drawing.Pen WindowFrame { get { throw null; } }
        public static System.Drawing.Pen WindowText { get { throw null; } }
        public static System.Drawing.Pen FromSystemColor(System.Drawing.Color c) { throw null; }
    }
    public sealed partial class TextureBrush : System.Drawing.Brush
    {
        public TextureBrush(System.Drawing.Image bitmap) { }
        public TextureBrush(System.Drawing.Image image, System.Drawing.Drawing2D.WrapMode wrapMode) { }
        public TextureBrush(System.Drawing.Image image, System.Drawing.Drawing2D.WrapMode wrapMode, System.Drawing.Rectangle dstRect) { }
        public TextureBrush(System.Drawing.Image image, System.Drawing.Drawing2D.WrapMode wrapMode, System.Drawing.RectangleF dstRect) { }
        public TextureBrush(System.Drawing.Image image, System.Drawing.Rectangle dstRect) { }
        public TextureBrush(System.Drawing.Image image, System.Drawing.Rectangle dstRect, System.Drawing.Imaging.ImageAttributes imageAttr) { }
        public TextureBrush(System.Drawing.Image image, System.Drawing.RectangleF dstRect) { }
        public TextureBrush(System.Drawing.Image image, System.Drawing.RectangleF dstRect, System.Drawing.Imaging.ImageAttributes imageAttr) { }
        public System.Drawing.Image Image { get { throw null; } }
        public System.Drawing.Drawing2D.Matrix Transform { get { throw null; } set { } }
        public System.Drawing.Drawing2D.WrapMode WrapMode { get { throw null; } set { } }
        public override object Clone() { throw null; }
        public void MultiplyTransform(System.Drawing.Drawing2D.Matrix matrix) { }
        public void MultiplyTransform(System.Drawing.Drawing2D.Matrix matrix, System.Drawing.Drawing2D.MatrixOrder order) { }
        public void ResetTransform() { }
        public void RotateTransform(float angle) { }
        public void RotateTransform(float angle, System.Drawing.Drawing2D.MatrixOrder order) { }
        public void ScaleTransform(float sx, float sy) { }
        public void ScaleTransform(float sx, float sy, System.Drawing.Drawing2D.MatrixOrder order) { }
        public void TranslateTransform(float dx, float dy) { }
        public void TranslateTransform(float dx, float dy, System.Drawing.Drawing2D.MatrixOrder order) { }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(4))]
    public partial class ToolboxBitmapAttribute : System.Attribute
    {
        public static readonly System.Drawing.ToolboxBitmapAttribute Default;
        public ToolboxBitmapAttribute(string imageFile) { }
        public ToolboxBitmapAttribute(System.Type t) { }
        public ToolboxBitmapAttribute(System.Type t, string name) { }
        public override bool Equals(object value) { throw null; }
        public override int GetHashCode() { throw null; }
        public System.Drawing.Image GetImage(object component) { throw null; }
        public System.Drawing.Image GetImage(object component, bool large) { throw null; }
        public System.Drawing.Image GetImage(System.Type type) { throw null; }
        public System.Drawing.Image GetImage(System.Type type, bool large) { throw null; }
        public System.Drawing.Image GetImage(System.Type type, string imgName, bool large) { throw null; }
        public static System.Drawing.Image GetImageFromResource(System.Type t, string imageName, bool large) { throw null; }
    }
}
namespace System.Drawing.Design
{
    public sealed partial class CategoryNameCollection : System.Collections.ReadOnlyCollectionBase
    {
        public CategoryNameCollection(System.Drawing.Design.CategoryNameCollection value) { }
        public CategoryNameCollection(string[] value) { }
        public string this[int index] { get { throw null; } }
        public bool Contains(string value) { throw null; }
        public void CopyTo(string[] array, int index) { }
        public int IndexOf(string value) { throw null; }
    }
}
namespace System.Drawing.Drawing2D
{
    public sealed partial class AdjustableArrowCap : System.Drawing.Drawing2D.CustomLineCap
    {
        public AdjustableArrowCap(float width, float height) : base (default(System.Drawing.Drawing2D.GraphicsPath), default(System.Drawing.Drawing2D.GraphicsPath)) { }
        public AdjustableArrowCap(float width, float height, bool isFilled) : base (default(System.Drawing.Drawing2D.GraphicsPath), default(System.Drawing.Drawing2D.GraphicsPath)) { }
        public bool Filled { get { throw null; } set { } }
        public float Height { get { throw null; } set { } }
        public float MiddleInset { get { throw null; } set { } }
        public float Width { get { throw null; } set { } }
    }
    public sealed partial class Blend
    {
        public Blend() { }
        public Blend(int count) { }
        public float[] Factors { get { throw null; } set { } }
        public float[] Positions { get { throw null; } set { } }
    }
    public sealed partial class ColorBlend
    {
        public ColorBlend() { }
        public ColorBlend(int count) { }
        public System.Drawing.Color[] Colors { get { throw null; } set { } }
        public float[] Positions { get { throw null; } set { } }
    }
    public enum CombineMode
    {
        Complement = 5,
        Exclude = 4,
        Intersect = 1,
        Replace = 0,
        Union = 2,
        Xor = 3,
    }
    public enum CompositingMode
    {
        SourceCopy = 1,
        SourceOver = 0,
    }
    public enum CompositingQuality
    {
        AssumeLinear = 4,
        Default = 0,
        GammaCorrected = 3,
        HighQuality = 2,
        HighSpeed = 1,
        Invalid = -1,
    }
    public enum CoordinateSpace
    {
        Device = 2,
        Page = 1,
        World = 0,
    }
    public partial class CustomLineCap : System.MarshalByRefObject, System.ICloneable, System.IDisposable
    {
        public CustomLineCap(System.Drawing.Drawing2D.GraphicsPath fillPath, System.Drawing.Drawing2D.GraphicsPath strokePath) { }
        public CustomLineCap(System.Drawing.Drawing2D.GraphicsPath fillPath, System.Drawing.Drawing2D.GraphicsPath strokePath, System.Drawing.Drawing2D.LineCap baseCap) { }
        public CustomLineCap(System.Drawing.Drawing2D.GraphicsPath fillPath, System.Drawing.Drawing2D.GraphicsPath strokePath, System.Drawing.Drawing2D.LineCap baseCap, float baseInset) { }
        public System.Drawing.Drawing2D.LineCap BaseCap { get { throw null; } set { } }
        public float BaseInset { get { throw null; } set { } }
        public System.Drawing.Drawing2D.LineJoin StrokeJoin { get { throw null; } set { } }
        public float WidthScale { get { throw null; } set { } }
        public object Clone() { throw null; }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        ~CustomLineCap() { }
        public void GetStrokeCaps(out System.Drawing.Drawing2D.LineCap startCap, out System.Drawing.Drawing2D.LineCap endCap) { throw null; }
        public void SetStrokeCaps(System.Drawing.Drawing2D.LineCap startCap, System.Drawing.Drawing2D.LineCap endCap) { }
    }
    public enum DashCap
    {
        Flat = 0,
        Round = 2,
        Triangle = 3,
    }
    public enum DashStyle
    {
        Custom = 5,
        Dash = 1,
        DashDot = 3,
        DashDotDot = 4,
        Dot = 2,
        Solid = 0,
    }
    public enum FillMode
    {
        Alternate = 0,
        Winding = 1,
    }
    public enum FlushIntention
    {
        Flush = 0,
        Sync = 1,
    }
    public sealed partial class GraphicsContainer : System.MarshalByRefObject
    {
        internal GraphicsContainer() { }
    }
    public sealed partial class GraphicsPath : System.MarshalByRefObject, System.ICloneable, System.IDisposable
    {
        public GraphicsPath() { }
        public GraphicsPath(System.Drawing.Drawing2D.FillMode fillMode) { }
        public GraphicsPath(System.Drawing.PointF[] pts, byte[] types) { }
        public GraphicsPath(System.Drawing.PointF[] pts, byte[] types, System.Drawing.Drawing2D.FillMode fillMode) { }
        public GraphicsPath(System.Drawing.Point[] pts, byte[] types) { }
        public GraphicsPath(System.Drawing.Point[] pts, byte[] types, System.Drawing.Drawing2D.FillMode fillMode) { }
        public System.Drawing.Drawing2D.FillMode FillMode { get { throw null; } set { } }
        public System.Drawing.Drawing2D.PathData PathData { get { throw null; } }
        public System.Drawing.PointF[] PathPoints { get { throw null; } }
        public byte[] PathTypes { get { throw null; } }
        public int PointCount { get { throw null; } }
        public void AddArc(System.Drawing.Rectangle rect, float startAngle, float sweepAngle) { }
        public void AddArc(System.Drawing.RectangleF rect, float startAngle, float sweepAngle) { }
        public void AddArc(int x, int y, int width, int height, float startAngle, float sweepAngle) { }
        public void AddArc(float x, float y, float width, float height, float startAngle, float sweepAngle) { }
        public void AddBezier(System.Drawing.Point pt1, System.Drawing.Point pt2, System.Drawing.Point pt3, System.Drawing.Point pt4) { }
        public void AddBezier(System.Drawing.PointF pt1, System.Drawing.PointF pt2, System.Drawing.PointF pt3, System.Drawing.PointF pt4) { }
        public void AddBezier(int x1, int y1, int x2, int y2, int x3, int y3, int x4, int y4) { }
        public void AddBezier(float x1, float y1, float x2, float y2, float x3, float y3, float x4, float y4) { }
        public void AddBeziers(System.Drawing.PointF[] points) { }
        public void AddBeziers(params System.Drawing.Point[] points) { }
        public void AddClosedCurve(System.Drawing.PointF[] points) { }
        public void AddClosedCurve(System.Drawing.PointF[] points, float tension) { }
        public void AddClosedCurve(System.Drawing.Point[] points) { }
        public void AddClosedCurve(System.Drawing.Point[] points, float tension) { }
        public void AddCurve(System.Drawing.PointF[] points) { }
        public void AddCurve(System.Drawing.PointF[] points, int offset, int numberOfSegments, float tension) { }
        public void AddCurve(System.Drawing.PointF[] points, float tension) { }
        public void AddCurve(System.Drawing.Point[] points) { }
        public void AddCurve(System.Drawing.Point[] points, int offset, int numberOfSegments, float tension) { }
        public void AddCurve(System.Drawing.Point[] points, float tension) { }
        public void AddEllipse(System.Drawing.Rectangle rect) { }
        public void AddEllipse(System.Drawing.RectangleF rect) { }
        public void AddEllipse(int x, int y, int width, int height) { }
        public void AddEllipse(float x, float y, float width, float height) { }
        public void AddLine(System.Drawing.Point pt1, System.Drawing.Point pt2) { }
        public void AddLine(System.Drawing.PointF pt1, System.Drawing.PointF pt2) { }
        public void AddLine(int x1, int y1, int x2, int y2) { }
        public void AddLine(float x1, float y1, float x2, float y2) { }
        public void AddLines(System.Drawing.PointF[] points) { }
        public void AddLines(System.Drawing.Point[] points) { }
        public void AddPath(System.Drawing.Drawing2D.GraphicsPath addingPath, bool connect) { }
        public void AddPie(System.Drawing.Rectangle rect, float startAngle, float sweepAngle) { }
        public void AddPie(int x, int y, int width, int height, float startAngle, float sweepAngle) { }
        public void AddPie(float x, float y, float width, float height, float startAngle, float sweepAngle) { }
        public void AddPolygon(System.Drawing.PointF[] points) { }
        public void AddPolygon(System.Drawing.Point[] points) { }
        public void AddRectangle(System.Drawing.Rectangle rect) { }
        public void AddRectangle(System.Drawing.RectangleF rect) { }
        public void AddRectangles(System.Drawing.RectangleF[] rects) { }
        public void AddRectangles(System.Drawing.Rectangle[] rects) { }
        public void AddString(string s, System.Drawing.FontFamily family, int style, float emSize, System.Drawing.Point origin, System.Drawing.StringFormat format) { }
        public void AddString(string s, System.Drawing.FontFamily family, int style, float emSize, System.Drawing.PointF origin, System.Drawing.StringFormat format) { }
        public void AddString(string s, System.Drawing.FontFamily family, int style, float emSize, System.Drawing.Rectangle layoutRect, System.Drawing.StringFormat format) { }
        public void AddString(string s, System.Drawing.FontFamily family, int style, float emSize, System.Drawing.RectangleF layoutRect, System.Drawing.StringFormat format) { }
        public void ClearMarkers() { }
        public object Clone() { throw null; }
        public void CloseAllFigures() { }
        public void CloseFigure() { }
        public void Dispose() { }
        ~GraphicsPath() { }
        public void Flatten() { }
        public void Flatten(System.Drawing.Drawing2D.Matrix matrix) { }
        public void Flatten(System.Drawing.Drawing2D.Matrix matrix, float flatness) { }
        public System.Drawing.RectangleF GetBounds() { throw null; }
        public System.Drawing.RectangleF GetBounds(System.Drawing.Drawing2D.Matrix matrix) { throw null; }
        public System.Drawing.RectangleF GetBounds(System.Drawing.Drawing2D.Matrix matrix, System.Drawing.Pen pen) { throw null; }
        public System.Drawing.PointF GetLastPoint() { throw null; }
        public bool IsOutlineVisible(System.Drawing.Point point, System.Drawing.Pen pen) { throw null; }
        public bool IsOutlineVisible(System.Drawing.Point pt, System.Drawing.Pen pen, System.Drawing.Graphics graphics) { throw null; }
        public bool IsOutlineVisible(System.Drawing.PointF point, System.Drawing.Pen pen) { throw null; }
        public bool IsOutlineVisible(System.Drawing.PointF pt, System.Drawing.Pen pen, System.Drawing.Graphics graphics) { throw null; }
        public bool IsOutlineVisible(int x, int y, System.Drawing.Pen pen) { throw null; }
        public bool IsOutlineVisible(int x, int y, System.Drawing.Pen pen, System.Drawing.Graphics graphics) { throw null; }
        public bool IsOutlineVisible(float x, float y, System.Drawing.Pen pen) { throw null; }
        public bool IsOutlineVisible(float x, float y, System.Drawing.Pen pen, System.Drawing.Graphics graphics) { throw null; }
        public bool IsVisible(System.Drawing.Point point) { throw null; }
        public bool IsVisible(System.Drawing.Point pt, System.Drawing.Graphics graphics) { throw null; }
        public bool IsVisible(System.Drawing.PointF point) { throw null; }
        public bool IsVisible(System.Drawing.PointF pt, System.Drawing.Graphics graphics) { throw null; }
        public bool IsVisible(int x, int y) { throw null; }
        public bool IsVisible(int x, int y, System.Drawing.Graphics graphics) { throw null; }
        public bool IsVisible(float x, float y) { throw null; }
        public bool IsVisible(float x, float y, System.Drawing.Graphics graphics) { throw null; }
        public void Reset() { }
        public void Reverse() { }
        public void SetMarkers() { }
        public void StartFigure() { }
        public void Transform(System.Drawing.Drawing2D.Matrix matrix) { }
        public void Warp(System.Drawing.PointF[] destPoints, System.Drawing.RectangleF srcRect) { }
        public void Warp(System.Drawing.PointF[] destPoints, System.Drawing.RectangleF srcRect, System.Drawing.Drawing2D.Matrix matrix) { }
        public void Warp(System.Drawing.PointF[] destPoints, System.Drawing.RectangleF srcRect, System.Drawing.Drawing2D.Matrix matrix, System.Drawing.Drawing2D.WarpMode warpMode) { }
        public void Warp(System.Drawing.PointF[] destPoints, System.Drawing.RectangleF srcRect, System.Drawing.Drawing2D.Matrix matrix, System.Drawing.Drawing2D.WarpMode warpMode, float flatness) { }
        public void Widen(System.Drawing.Pen pen) { }
        public void Widen(System.Drawing.Pen pen, System.Drawing.Drawing2D.Matrix matrix) { }
        public void Widen(System.Drawing.Pen pen, System.Drawing.Drawing2D.Matrix matrix, float flatness) { }
    }
    public sealed partial class GraphicsPathIterator : System.MarshalByRefObject, System.IDisposable
    {
        public GraphicsPathIterator(System.Drawing.Drawing2D.GraphicsPath path) { }
        public int Count { get { throw null; } }
        public int SubpathCount { get { throw null; } }
        public int CopyData(ref System.Drawing.PointF[] points, ref byte[] types, int startIndex, int endIndex) { throw null; }
        public void Dispose() { }
        public int Enumerate(ref System.Drawing.PointF[] points, ref byte[] types) { throw null; }
        ~GraphicsPathIterator() { }
        public bool HasCurve() { throw null; }
        public int NextMarker(System.Drawing.Drawing2D.GraphicsPath path) { throw null; }
        public int NextMarker(out int startIndex, out int endIndex) { throw null; }
        public int NextPathType(out byte pathType, out int startIndex, out int endIndex) { throw null; }
        public int NextSubpath(System.Drawing.Drawing2D.GraphicsPath path, out bool isClosed) { throw null; }
        public int NextSubpath(out int startIndex, out int endIndex, out bool isClosed) { throw null; }
        public void Rewind() { }
    }
    public sealed partial class GraphicsState : System.MarshalByRefObject
    {
        internal GraphicsState() { }
    }
    public sealed partial class HatchBrush : System.Drawing.Brush
    {
        public HatchBrush(System.Drawing.Drawing2D.HatchStyle hatchstyle, System.Drawing.Color foreColor) { }
        public HatchBrush(System.Drawing.Drawing2D.HatchStyle hatchstyle, System.Drawing.Color foreColor, System.Drawing.Color backColor) { }
        public System.Drawing.Color BackgroundColor { get { throw null; } }
        public System.Drawing.Color ForegroundColor { get { throw null; } }
        public System.Drawing.Drawing2D.HatchStyle HatchStyle { get { throw null; } }
        public override object Clone() { throw null; }
    }
    public enum HatchStyle
    {
        BackwardDiagonal = 3,
        Cross = 4,
        DarkDownwardDiagonal = 20,
        DarkHorizontal = 29,
        DarkUpwardDiagonal = 21,
        DarkVertical = 28,
        DashedDownwardDiagonal = 30,
        DashedHorizontal = 32,
        DashedUpwardDiagonal = 31,
        DashedVertical = 33,
        DiagonalBrick = 38,
        DiagonalCross = 5,
        Divot = 42,
        DottedDiamond = 44,
        DottedGrid = 43,
        ForwardDiagonal = 2,
        Horizontal = 0,
        HorizontalBrick = 39,
        LargeCheckerBoard = 50,
        LargeConfetti = 35,
        LargeGrid = 4,
        LightDownwardDiagonal = 18,
        LightHorizontal = 25,
        LightUpwardDiagonal = 19,
        LightVertical = 24,
        Max = 4,
        Min = 0,
        NarrowHorizontal = 27,
        NarrowVertical = 26,
        OutlinedDiamond = 51,
        Percent05 = 6,
        Percent10 = 7,
        Percent20 = 8,
        Percent25 = 9,
        Percent30 = 10,
        Percent40 = 11,
        Percent50 = 12,
        Percent60 = 13,
        Percent70 = 14,
        Percent75 = 15,
        Percent80 = 16,
        Percent90 = 17,
        Plaid = 41,
        Shingle = 45,
        SmallCheckerBoard = 49,
        SmallConfetti = 34,
        SmallGrid = 48,
        SolidDiamond = 52,
        Sphere = 47,
        Trellis = 46,
        Vertical = 1,
        Wave = 37,
        Weave = 40,
        WideDownwardDiagonal = 22,
        WideUpwardDiagonal = 23,
        ZigZag = 36,
    }
    public enum InterpolationMode
    {
        Bicubic = 4,
        Bilinear = 3,
        Default = 0,
        High = 2,
        HighQualityBicubic = 7,
        HighQualityBilinear = 6,
        Invalid = -1,
        Low = 1,
        NearestNeighbor = 5,
    }
    public sealed partial class LinearGradientBrush : System.Drawing.Brush
    {
        public LinearGradientBrush(System.Drawing.Point point1, System.Drawing.Point point2, System.Drawing.Color color1, System.Drawing.Color color2) { }
        public LinearGradientBrush(System.Drawing.PointF point1, System.Drawing.PointF point2, System.Drawing.Color color1, System.Drawing.Color color2) { }
        public LinearGradientBrush(System.Drawing.Rectangle rect, System.Drawing.Color color1, System.Drawing.Color color2, System.Drawing.Drawing2D.LinearGradientMode linearGradientMode) { }
        public LinearGradientBrush(System.Drawing.Rectangle rect, System.Drawing.Color color1, System.Drawing.Color color2, float angle) { }
        public LinearGradientBrush(System.Drawing.Rectangle rect, System.Drawing.Color color1, System.Drawing.Color color2, float angle, bool isAngleScaleable) { }
        public LinearGradientBrush(System.Drawing.RectangleF rect, System.Drawing.Color color1, System.Drawing.Color color2, System.Drawing.Drawing2D.LinearGradientMode linearGradientMode) { }
        public LinearGradientBrush(System.Drawing.RectangleF rect, System.Drawing.Color color1, System.Drawing.Color color2, float angle) { }
        public LinearGradientBrush(System.Drawing.RectangleF rect, System.Drawing.Color color1, System.Drawing.Color color2, float angle, bool isAngleScaleable) { }
        public System.Drawing.Drawing2D.Blend Blend { get { throw null; } set { } }
        public bool GammaCorrection { get { throw null; } set { } }
        public System.Drawing.Drawing2D.ColorBlend InterpolationColors { get { throw null; } set { } }
        public System.Drawing.Color[] LinearColors { get { throw null; } set { } }
        public System.Drawing.RectangleF Rectangle { get { throw null; } }
        public System.Drawing.Drawing2D.Matrix Transform { get { throw null; } set { } }
        public System.Drawing.Drawing2D.WrapMode WrapMode { get { throw null; } set { } }
        public override object Clone() { throw null; }
        public void MultiplyTransform(System.Drawing.Drawing2D.Matrix matrix) { }
        public void MultiplyTransform(System.Drawing.Drawing2D.Matrix matrix, System.Drawing.Drawing2D.MatrixOrder order) { }
        public void ResetTransform() { }
        public void RotateTransform(float angle) { }
        public void RotateTransform(float angle, System.Drawing.Drawing2D.MatrixOrder order) { }
        public void ScaleTransform(float sx, float sy) { }
        public void ScaleTransform(float sx, float sy, System.Drawing.Drawing2D.MatrixOrder order) { }
        public void SetBlendTriangularShape(float focus) { }
        public void SetBlendTriangularShape(float focus, float scale) { }
        public void SetSigmaBellShape(float focus) { }
        public void SetSigmaBellShape(float focus, float scale) { }
        public void TranslateTransform(float dx, float dy) { }
        public void TranslateTransform(float dx, float dy, System.Drawing.Drawing2D.MatrixOrder order) { }
    }
    public enum LinearGradientMode
    {
        BackwardDiagonal = 3,
        ForwardDiagonal = 2,
        Horizontal = 0,
        Vertical = 1,
    }
    public enum LineCap
    {
        AnchorMask = 240,
        ArrowAnchor = 20,
        Custom = 255,
        DiamondAnchor = 19,
        Flat = 0,
        NoAnchor = 16,
        Round = 2,
        RoundAnchor = 18,
        Square = 1,
        SquareAnchor = 17,
        Triangle = 3,
    }
    public enum LineJoin
    {
        Bevel = 1,
        Miter = 0,
        MiterClipped = 3,
        Round = 2,
    }
    public sealed partial class Matrix : System.MarshalByRefObject, System.IDisposable
    {
        public Matrix() { }
        public Matrix(System.Drawing.Rectangle rect, System.Drawing.Point[] plgpts) { }
        public Matrix(System.Drawing.RectangleF rect, System.Drawing.PointF[] plgpts) { }
        public Matrix(float m11, float m12, float m21, float m22, float dx, float dy) { }
        public float[] Elements { get { throw null; } }
        public bool IsIdentity { get { throw null; } }
        public bool IsInvertible { get { throw null; } }
        public float OffsetX { get { throw null; } }
        public float OffsetY { get { throw null; } }
        public System.Drawing.Drawing2D.Matrix Clone() { throw null; }
        public void Dispose() { }
        public override bool Equals(object obj) { throw null; }
        ~Matrix() { }
        public override int GetHashCode() { throw null; }
        public void Invert() { }
        public void Multiply(System.Drawing.Drawing2D.Matrix matrix) { }
        public void Multiply(System.Drawing.Drawing2D.Matrix matrix, System.Drawing.Drawing2D.MatrixOrder order) { }
        public void Reset() { }
        public void Rotate(float angle) { }
        public void Rotate(float angle, System.Drawing.Drawing2D.MatrixOrder order) { }
        public void RotateAt(float angle, System.Drawing.PointF point) { }
        public void RotateAt(float angle, System.Drawing.PointF point, System.Drawing.Drawing2D.MatrixOrder order) { }
        public void Scale(float scaleX, float scaleY) { }
        public void Scale(float scaleX, float scaleY, System.Drawing.Drawing2D.MatrixOrder order) { }
        public void Shear(float shearX, float shearY) { }
        public void Shear(float shearX, float shearY, System.Drawing.Drawing2D.MatrixOrder order) { }
        public void TransformPoints(System.Drawing.PointF[] pts) { }
        public void TransformPoints(System.Drawing.Point[] pts) { }
        public void TransformVectors(System.Drawing.PointF[] pts) { }
        public void TransformVectors(System.Drawing.Point[] pts) { }
        public void Translate(float offsetX, float offsetY) { }
        public void Translate(float offsetX, float offsetY, System.Drawing.Drawing2D.MatrixOrder order) { }
        public void VectorTransformPoints(System.Drawing.Point[] pts) { }
    }
    public enum MatrixOrder
    {
        Append = 1,
        Prepend = 0,
    }
    public sealed partial class PathData
    {
        public PathData() { }
        public System.Drawing.PointF[] Points { get { throw null; } set { } }
        public byte[] Types { get { throw null; } set { } }
    }
    public sealed partial class PathGradientBrush : System.Drawing.Brush
    {
        public PathGradientBrush(System.Drawing.Drawing2D.GraphicsPath path) { }
        public PathGradientBrush(System.Drawing.PointF[] points) { }
        public PathGradientBrush(System.Drawing.PointF[] points, System.Drawing.Drawing2D.WrapMode wrapMode) { }
        public PathGradientBrush(System.Drawing.Point[] points) { }
        public PathGradientBrush(System.Drawing.Point[] points, System.Drawing.Drawing2D.WrapMode wrapMode) { }
        public System.Drawing.Drawing2D.Blend Blend { get { throw null; } set { } }
        public System.Drawing.Color CenterColor { get { throw null; } set { } }
        public System.Drawing.PointF CenterPoint { get { throw null; } set { } }
        public System.Drawing.PointF FocusScales { get { throw null; } set { } }
        public System.Drawing.Drawing2D.ColorBlend InterpolationColors { get { throw null; } set { } }
        public System.Drawing.RectangleF Rectangle { get { throw null; } }
        public System.Drawing.Color[] SurroundColors { get { throw null; } set { } }
        public System.Drawing.Drawing2D.Matrix Transform { get { throw null; } set { } }
        public System.Drawing.Drawing2D.WrapMode WrapMode { get { throw null; } set { } }
        public override object Clone() { throw null; }
        public void MultiplyTransform(System.Drawing.Drawing2D.Matrix matrix) { }
        public void MultiplyTransform(System.Drawing.Drawing2D.Matrix matrix, System.Drawing.Drawing2D.MatrixOrder order) { }
        public void ResetTransform() { }
        public void RotateTransform(float angle) { }
        public void RotateTransform(float angle, System.Drawing.Drawing2D.MatrixOrder order) { }
        public void ScaleTransform(float sx, float sy) { }
        public void ScaleTransform(float sx, float sy, System.Drawing.Drawing2D.MatrixOrder order) { }
        public void SetBlendTriangularShape(float focus) { }
        public void SetBlendTriangularShape(float focus, float scale) { }
        public void SetSigmaBellShape(float focus) { }
        public void SetSigmaBellShape(float focus, float scale) { }
        public void TranslateTransform(float dx, float dy) { }
        public void TranslateTransform(float dx, float dy, System.Drawing.Drawing2D.MatrixOrder order) { }
    }
    public enum PathPointType
    {
        Bezier = 3,
        Bezier3 = 3,
        CloseSubpath = 128,
        DashMode = 16,
        Line = 1,
        PathMarker = 32,
        PathTypeMask = 7,
        Start = 0,
    }
    public enum PenAlignment
    {
        Center = 0,
        Inset = 1,
        Left = 3,
        Outset = 2,
        Right = 4,
    }
    public enum PenType
    {
        HatchFill = 1,
        LinearGradient = 4,
        PathGradient = 3,
        SolidColor = 0,
        TextureFill = 2,
    }
    public enum PixelOffsetMode
    {
        Default = 0,
        Half = 4,
        HighQuality = 2,
        HighSpeed = 1,
        Invalid = -1,
        None = 3,
    }
    public enum QualityMode
    {
        Default = 0,
        High = 2,
        Invalid = -1,
        Low = 1,
    }
    public sealed partial class RegionData
    {
        internal RegionData() { }
        public byte[] Data { get { throw null; } set { } }
    }
    public enum SmoothingMode
    {
        AntiAlias = 4,
        Default = 0,
        HighQuality = 2,
        HighSpeed = 1,
        Invalid = -1,
        None = 3,
    }
    public enum WarpMode
    {
        Bilinear = 1,
        Perspective = 0,
    }
    public enum WrapMode
    {
        Clamp = 4,
        Tile = 0,
        TileFlipX = 1,
        TileFlipXY = 3,
        TileFlipY = 2,
    }
}
namespace System.Drawing.Imaging
{
    public sealed partial class BitmapData
    {
        public BitmapData() { }
        public int Height { get { throw null; } set { } }
        public System.Drawing.Imaging.PixelFormat PixelFormat { get { throw null; } set { } }
        public int Reserved { get { throw null; } set { } }
        public System.IntPtr Scan0 { get { throw null; } set { } }
        public int Stride { get { throw null; } set { } }
        public int Width { get { throw null; } set { } }
    }
    public enum ColorAdjustType
    {
        Any = 6,
        Bitmap = 1,
        Brush = 2,
        Count = 5,
        Default = 0,
        Pen = 3,
        Text = 4,
    }
    public enum ColorChannelFlag
    {
        ColorChannelC = 0,
        ColorChannelK = 3,
        ColorChannelLast = 4,
        ColorChannelM = 1,
        ColorChannelY = 2,
    }
    public sealed partial class ColorMap
    {
        public ColorMap() { }
        public System.Drawing.Color NewColor { get { throw null; } set { } }
        public System.Drawing.Color OldColor { get { throw null; } set { } }
    }
    public enum ColorMapType
    {
        Brush = 1,
        Default = 0,
    }
    public sealed partial class ColorMatrix
    {
        public ColorMatrix() { }
        [System.CLSCompliantAttribute(false)]
        public ColorMatrix(float[][] newColorMatrix) { }
        public float this[int row, int column] { get { throw null; } set { } }
        public float Matrix00 { get { throw null; } set { } }
        public float Matrix01 { get { throw null; } set { } }
        public float Matrix02 { get { throw null; } set { } }
        public float Matrix03 { get { throw null; } set { } }
        public float Matrix04 { get { throw null; } set { } }
        public float Matrix10 { get { throw null; } set { } }
        public float Matrix11 { get { throw null; } set { } }
        public float Matrix12 { get { throw null; } set { } }
        public float Matrix13 { get { throw null; } set { } }
        public float Matrix14 { get { throw null; } set { } }
        public float Matrix20 { get { throw null; } set { } }
        public float Matrix21 { get { throw null; } set { } }
        public float Matrix22 { get { throw null; } set { } }
        public float Matrix23 { get { throw null; } set { } }
        public float Matrix24 { get { throw null; } set { } }
        public float Matrix30 { get { throw null; } set { } }
        public float Matrix31 { get { throw null; } set { } }
        public float Matrix32 { get { throw null; } set { } }
        public float Matrix33 { get { throw null; } set { } }
        public float Matrix34 { get { throw null; } set { } }
        public float Matrix40 { get { throw null; } set { } }
        public float Matrix41 { get { throw null; } set { } }
        public float Matrix42 { get { throw null; } set { } }
        public float Matrix43 { get { throw null; } set { } }
        public float Matrix44 { get { throw null; } set { } }
    }
    public enum ColorMatrixFlag
    {
        AltGrays = 2,
        Default = 0,
        SkipGrays = 1,
    }
    public enum ColorMode
    {
        Argb32Mode = 0,
        Argb64Mode = 1,
    }
    public sealed partial class ColorPalette
    {
        internal ColorPalette() { }
        public System.Drawing.Color[] Entries { get { throw null; } }
        public int Flags { get { throw null; } }
    }
    public enum EmfPlusRecordType
    {
        BeginContainer = 16423,
        BeginContainerNoParams = 16424,
        Clear = 16393,
        Comment = 16387,
        DrawArc = 16402,
        DrawBeziers = 16409,
        DrawClosedCurve = 16407,
        DrawCurve = 16408,
        DrawDriverString = 16438,
        DrawEllipse = 16399,
        DrawImage = 16410,
        DrawImagePoints = 16411,
        DrawLines = 16397,
        DrawPath = 16405,
        DrawPie = 16401,
        DrawRects = 16395,
        DrawString = 16412,
        EmfAbortPath = 68,
        EmfAlphaBlend = 114,
        EmfAngleArc = 41,
        EmfArcTo = 55,
        EmfBeginPath = 59,
        EmfBitBlt = 76,
        EmfChord = 46,
        EmfCloseFigure = 61,
        EmfColorCorrectPalette = 111,
        EmfColorMatchToTargetW = 121,
        EmfCreateBrushIndirect = 39,
        EmfCreateColorSpace = 99,
        EmfCreateColorSpaceW = 122,
        EmfCreateDibPatternBrushPt = 94,
        EmfCreateMonoBrush = 93,
        EmfCreatePalette = 49,
        EmfCreatePen = 38,
        EmfDeleteColorSpace = 101,
        EmfDeleteObject = 40,
        EmfDrawEscape = 105,
        EmfEllipse = 42,
        EmfEndPath = 60,
        EmfEof = 14,
        EmfExcludeClipRect = 29,
        EmfExtCreateFontIndirect = 82,
        EmfExtCreatePen = 95,
        EmfExtEscape = 106,
        EmfExtFloodFill = 53,
        EmfExtSelectClipRgn = 75,
        EmfExtTextOutA = 83,
        EmfExtTextOutW = 84,
        EmfFillPath = 62,
        EmfFillRgn = 71,
        EmfFlattenPath = 65,
        EmfForceUfiMapping = 109,
        EmfFrameRgn = 72,
        EmfGdiComment = 70,
        EmfGlsBoundedRecord = 103,
        EmfGlsRecord = 102,
        EmfGradientFill = 118,
        EmfHeader = 1,
        EmfIntersectClipRect = 30,
        EmfInvertRgn = 73,
        EmfLineTo = 54,
        EmfMaskBlt = 78,
        EmfMax = 122,
        EmfMin = 1,
        EmfModifyWorldTransform = 36,
        EmfMoveToEx = 27,
        EmfNamedEscpae = 110,
        EmfOffsetClipRgn = 26,
        EmfPaintRgn = 74,
        EmfPie = 47,
        EmfPixelFormat = 104,
        EmfPlgBlt = 79,
        EmfPlusRecordBase = 16384,
        EmfPolyBezier = 2,
        EmfPolyBezier16 = 85,
        EmfPolyBezierTo = 5,
        EmfPolyBezierTo16 = 88,
        EmfPolyDraw = 56,
        EmfPolyDraw16 = 92,
        EmfPolygon = 3,
        EmfPolygon16 = 86,
        EmfPolyline = 4,
        EmfPolyline16 = 87,
        EmfPolyLineTo = 6,
        EmfPolylineTo16 = 89,
        EmfPolyPolygon = 8,
        EmfPolyPolygon16 = 91,
        EmfPolyPolyline = 7,
        EmfPolyPolyline16 = 90,
        EmfPolyTextOutA = 96,
        EmfPolyTextOutW = 97,
        EmfRealizePalette = 52,
        EmfRectangle = 43,
        EmfReserved069 = 69,
        EmfReserved117 = 117,
        EmfResizePalette = 51,
        EmfRestoreDC = 34,
        EmfRoundArc = 45,
        EmfRoundRect = 44,
        EmfSaveDC = 33,
        EmfScaleViewportExtEx = 31,
        EmfScaleWindowExtEx = 32,
        EmfSelectClipPath = 67,
        EmfSelectObject = 37,
        EmfSelectPalette = 48,
        EmfSetArcDirection = 57,
        EmfSetBkColor = 25,
        EmfSetBkMode = 18,
        EmfSetBrushOrgEx = 13,
        EmfSetColorAdjustment = 23,
        EmfSetColorSpace = 100,
        EmfSetDIBitsToDevice = 80,
        EmfSetIcmMode = 98,
        EmfSetIcmProfileA = 112,
        EmfSetIcmProfileW = 113,
        EmfSetLayout = 115,
        EmfSetLinkedUfis = 119,
        EmfSetMapMode = 17,
        EmfSetMapperFlags = 16,
        EmfSetMetaRgn = 28,
        EmfSetMiterLimit = 58,
        EmfSetPaletteEntries = 50,
        EmfSetPixelV = 15,
        EmfSetPolyFillMode = 19,
        EmfSetROP2 = 20,
        EmfSetStretchBltMode = 21,
        EmfSetTextAlign = 22,
        EmfSetTextColor = 24,
        EmfSetTextJustification = 120,
        EmfSetViewportExtEx = 11,
        EmfSetViewportOrgEx = 12,
        EmfSetWindowExtEx = 9,
        EmfSetWindowOrgEx = 10,
        EmfSetWorldTransform = 35,
        EmfSmallTextOut = 108,
        EmfStartDoc = 107,
        EmfStretchBlt = 77,
        EmfStretchDIBits = 81,
        EmfStrokeAndFillPath = 63,
        EmfStrokePath = 64,
        EmfTransparentBlt = 116,
        EmfWidenPath = 66,
        EndContainer = 16425,
        EndOfFile = 16386,
        FillClosedCurve = 16406,
        FillEllipse = 16398,
        FillPath = 16404,
        FillPie = 16400,
        FillPolygon = 16396,
        FillRects = 16394,
        FillRegion = 16403,
        GetDC = 16388,
        Header = 16385,
        Invalid = 16384,
        Max = 16438,
        Min = 16385,
        MultiFormatEnd = 16391,
        MultiFormatSection = 16390,
        MultiFormatStart = 16389,
        MultiplyWorldTransform = 16428,
        Object = 16392,
        OffsetClip = 16437,
        ResetClip = 16433,
        ResetWorldTransform = 16427,
        Restore = 16422,
        RotateWorldTransform = 16431,
        Save = 16421,
        ScaleWorldTransform = 16430,
        SetAntiAliasMode = 16414,
        SetClipPath = 16435,
        SetClipRect = 16434,
        SetClipRegion = 16436,
        SetCompositingMode = 16419,
        SetCompositingQuality = 16420,
        SetInterpolationMode = 16417,
        SetPageTransform = 16432,
        SetPixelOffsetMode = 16418,
        SetRenderingOrigin = 16413,
        SetTextContrast = 16416,
        SetTextRenderingHint = 16415,
        SetWorldTransform = 16426,
        Total = 16439,
        TranslateWorldTransform = 16429,
        WmfAnimatePalette = 66614,
        WmfArc = 67607,
        WmfBitBlt = 67874,
        WmfChord = 67632,
        WmfCreateBrushIndirect = 66300,
        WmfCreateFontIndirect = 66299,
        WmfCreatePalette = 65783,
        WmfCreatePatternBrush = 66041,
        WmfCreatePenIndirect = 66298,
        WmfCreateRegion = 67327,
        WmfDeleteObject = 66032,
        WmfDibBitBlt = 67904,
        WmfDibCreatePatternBrush = 65858,
        WmfDibStretchBlt = 68417,
        WmfEllipse = 66584,
        WmfEscape = 67110,
        WmfExcludeClipRect = 66581,
        WmfExtFloodFill = 66888,
        WmfExtTextOut = 68146,
        WmfFillRegion = 66088,
        WmfFloodFill = 66585,
        WmfFrameRegion = 66601,
        WmfIntersectClipRect = 66582,
        WmfInvertRegion = 65834,
        WmfLineTo = 66067,
        WmfMoveTo = 66068,
        WmfOffsetCilpRgn = 66080,
        WmfOffsetViewportOrg = 66065,
        WmfOffsetWindowOrg = 66063,
        WmfPaintRegion = 65835,
        WmfPatBlt = 67101,
        WmfPie = 67610,
        WmfPolygon = 66340,
        WmfPolyline = 66341,
        WmfPolyPolygon = 66872,
        WmfRealizePalette = 65589,
        WmfRecordBase = 65536,
        WmfRectangle = 66587,
        WmfResizePalette = 65849,
        WmfRestoreDC = 65831,
        WmfRoundRect = 67100,
        WmfSaveDC = 65566,
        WmfScaleViewportExt = 66578,
        WmfScaleWindowExt = 66576,
        WmfSelectClipRegion = 65836,
        WmfSelectObject = 65837,
        WmfSelectPalette = 66100,
        WmfSetBkColor = 66049,
        WmfSetBkMode = 65794,
        WmfSetDibToDev = 68915,
        WmfSetLayout = 65865,
        WmfSetMapMode = 65795,
        WmfSetMapperFlags = 66097,
        WmfSetPalEntries = 65591,
        WmfSetPixel = 66591,
        WmfSetPolyFillMode = 65798,
        WmfSetRelAbs = 65797,
        WmfSetROP2 = 65796,
        WmfSetStretchBltMode = 65799,
        WmfSetTextAlign = 65838,
        WmfSetTextCharExtra = 65800,
        WmfSetTextColor = 66057,
        WmfSetTextJustification = 66058,
        WmfSetViewportExt = 66062,
        WmfSetViewportOrg = 66061,
        WmfSetWindowExt = 66060,
        WmfSetWindowOrg = 66059,
        WmfStretchBlt = 68387,
        WmfStretchDib = 69443,
        WmfTextOut = 66849,
    }
    public enum EmfType
    {
        EmfOnly = 3,
        EmfPlusDual = 5,
        EmfPlusOnly = 4,
    }
    public sealed partial class Encoder
    {
        public static readonly System.Drawing.Imaging.Encoder ChrominanceTable;
        public static readonly System.Drawing.Imaging.Encoder ColorDepth;
        public static readonly System.Drawing.Imaging.Encoder Compression;
        public static readonly System.Drawing.Imaging.Encoder LuminanceTable;
        public static readonly System.Drawing.Imaging.Encoder Quality;
        public static readonly System.Drawing.Imaging.Encoder RenderMethod;
        public static readonly System.Drawing.Imaging.Encoder SaveFlag;
        public static readonly System.Drawing.Imaging.Encoder ScanMethod;
        public static readonly System.Drawing.Imaging.Encoder Transformation;
        public static readonly System.Drawing.Imaging.Encoder Version;
        public Encoder(System.Guid guid) { }
        public System.Guid Guid { get { throw null; } }
    }
    public sealed partial class EncoderParameter : System.IDisposable
    {
        public EncoderParameter(System.Drawing.Imaging.Encoder encoder, byte value) { }
        public EncoderParameter(System.Drawing.Imaging.Encoder encoder, byte value, bool undefined) { }
        public EncoderParameter(System.Drawing.Imaging.Encoder encoder, byte[] value) { }
        public EncoderParameter(System.Drawing.Imaging.Encoder encoder, byte[] value, bool undefined) { }
        public EncoderParameter(System.Drawing.Imaging.Encoder encoder, short value) { }
        public EncoderParameter(System.Drawing.Imaging.Encoder encoder, short[] value) { }
        public EncoderParameter(System.Drawing.Imaging.Encoder encoder, int numberValues, System.Drawing.Imaging.EncoderParameterValueType type, System.IntPtr value) { }
        public EncoderParameter(System.Drawing.Imaging.Encoder encoder, int numerator, int denominator) { }
        [System.ObsoleteAttribute("This constructor has been deprecated. Use EncoderParameter(Encoder encoder, int numberValues, EncoderParameterValueType type, IntPtr value) instead.  http://go.microsoft.com/fwlink/?linkid=14202")]
        public EncoderParameter(System.Drawing.Imaging.Encoder encoder, int NumberOfValues, int Type, int Value) { }
        public EncoderParameter(System.Drawing.Imaging.Encoder encoder, int numerator1, int demoninator1, int numerator2, int demoninator2) { }
        public EncoderParameter(System.Drawing.Imaging.Encoder encoder, int[] numerator, int[] denominator) { }
        public EncoderParameter(System.Drawing.Imaging.Encoder encoder, int[] numerator1, int[] denominator1, int[] numerator2, int[] denominator2) { }
        public EncoderParameter(System.Drawing.Imaging.Encoder encoder, long value) { }
        public EncoderParameter(System.Drawing.Imaging.Encoder encoder, long rangebegin, long rangeend) { }
        public EncoderParameter(System.Drawing.Imaging.Encoder encoder, long[] value) { }
        public EncoderParameter(System.Drawing.Imaging.Encoder encoder, long[] rangebegin, long[] rangeend) { }
        public EncoderParameter(System.Drawing.Imaging.Encoder encoder, string value) { }
        public System.Drawing.Imaging.Encoder Encoder { get { throw null; } set { } }
        public int NumberOfValues { get { throw null; } }
        public System.Drawing.Imaging.EncoderParameterValueType Type { get { throw null; } }
        public System.Drawing.Imaging.EncoderParameterValueType ValueType { get { throw null; } }
        public void Dispose() { }
        ~EncoderParameter() { }
    }
    public sealed partial class EncoderParameters : System.IDisposable
    {
        public EncoderParameters() { }
        public EncoderParameters(int count) { }
        public System.Drawing.Imaging.EncoderParameter[] Param { get { throw null; } set { } }
        public void Dispose() { }
    }
    public enum EncoderParameterValueType
    {
        ValueTypeAscii = 2,
        ValueTypeByte = 1,
        ValueTypeLong = 4,
        ValueTypeLongRange = 6,
        ValueTypeRational = 5,
        ValueTypeRationalRange = 8,
        ValueTypeShort = 3,
        ValueTypeUndefined = 7,
    }
    public enum EncoderValue
    {
        ColorTypeCMYK = 0,
        ColorTypeYCCK = 1,
        CompressionCCITT3 = 3,
        CompressionCCITT4 = 4,
        CompressionLZW = 2,
        CompressionNone = 6,
        CompressionRle = 5,
        Flush = 20,
        FrameDimensionPage = 23,
        FrameDimensionResolution = 22,
        FrameDimensionTime = 21,
        LastFrame = 19,
        MultiFrame = 18,
        RenderNonProgressive = 12,
        RenderProgressive = 11,
        ScanMethodInterlaced = 7,
        ScanMethodNonInterlaced = 8,
        TransformFlipHorizontal = 16,
        TransformFlipVertical = 17,
        TransformRotate180 = 14,
        TransformRotate270 = 15,
        TransformRotate90 = 13,
        VersionGif87 = 9,
        VersionGif89 = 10,
    }
    public sealed partial class FrameDimension
    {
        public FrameDimension(System.Guid guid) { }
        public System.Guid Guid { get { throw null; } }
        public static System.Drawing.Imaging.FrameDimension Page { get { throw null; } }
        public static System.Drawing.Imaging.FrameDimension Resolution { get { throw null; } }
        public static System.Drawing.Imaging.FrameDimension Time { get { throw null; } }
        public override bool Equals(object o) { throw null; }
        public override int GetHashCode() { throw null; }
        public override string ToString() { throw null; }
    }
    public sealed partial class ImageAttributes : System.ICloneable, System.IDisposable
    {
        public ImageAttributes() { }
        public void ClearBrushRemapTable() { }
        public void ClearColorKey() { }
        public void ClearColorKey(System.Drawing.Imaging.ColorAdjustType type) { }
        public void ClearColorMatrix() { }
        public void ClearColorMatrix(System.Drawing.Imaging.ColorAdjustType type) { }
        public void ClearGamma() { }
        public void ClearGamma(System.Drawing.Imaging.ColorAdjustType type) { }
        public void ClearNoOp() { }
        public void ClearNoOp(System.Drawing.Imaging.ColorAdjustType type) { }
        public void ClearOutputChannel() { }
        public void ClearOutputChannel(System.Drawing.Imaging.ColorAdjustType type) { }
        public void ClearOutputChannelColorProfile() { }
        public void ClearOutputChannelColorProfile(System.Drawing.Imaging.ColorAdjustType type) { }
        public void ClearRemapTable() { }
        public void ClearRemapTable(System.Drawing.Imaging.ColorAdjustType type) { }
        public void ClearThreshold() { }
        public void ClearThreshold(System.Drawing.Imaging.ColorAdjustType type) { }
        public object Clone() { throw null; }
        public void Dispose() { }
        ~ImageAttributes() { }
        public void GetAdjustedPalette(System.Drawing.Imaging.ColorPalette palette, System.Drawing.Imaging.ColorAdjustType type) { }
        public void SetBrushRemapTable(System.Drawing.Imaging.ColorMap[] map) { }
        public void SetColorKey(System.Drawing.Color colorLow, System.Drawing.Color colorHigh) { }
        public void SetColorKey(System.Drawing.Color colorLow, System.Drawing.Color colorHigh, System.Drawing.Imaging.ColorAdjustType type) { }
        public void SetColorMatrices(System.Drawing.Imaging.ColorMatrix newColorMatrix, System.Drawing.Imaging.ColorMatrix grayMatrix) { }
        public void SetColorMatrices(System.Drawing.Imaging.ColorMatrix newColorMatrix, System.Drawing.Imaging.ColorMatrix grayMatrix, System.Drawing.Imaging.ColorMatrixFlag flags) { }
        public void SetColorMatrices(System.Drawing.Imaging.ColorMatrix newColorMatrix, System.Drawing.Imaging.ColorMatrix grayMatrix, System.Drawing.Imaging.ColorMatrixFlag mode, System.Drawing.Imaging.ColorAdjustType type) { }
        public void SetColorMatrix(System.Drawing.Imaging.ColorMatrix newColorMatrix) { }
        public void SetColorMatrix(System.Drawing.Imaging.ColorMatrix newColorMatrix, System.Drawing.Imaging.ColorMatrixFlag flags) { }
        public void SetColorMatrix(System.Drawing.Imaging.ColorMatrix newColorMatrix, System.Drawing.Imaging.ColorMatrixFlag mode, System.Drawing.Imaging.ColorAdjustType type) { }
        public void SetGamma(float gamma) { }
        public void SetGamma(float gamma, System.Drawing.Imaging.ColorAdjustType type) { }
        public void SetNoOp() { }
        public void SetNoOp(System.Drawing.Imaging.ColorAdjustType type) { }
        public void SetOutputChannel(System.Drawing.Imaging.ColorChannelFlag flags) { }
        public void SetOutputChannel(System.Drawing.Imaging.ColorChannelFlag flags, System.Drawing.Imaging.ColorAdjustType type) { }
        public void SetOutputChannelColorProfile(string colorProfileFilename) { }
        public void SetOutputChannelColorProfile(string colorProfileFilename, System.Drawing.Imaging.ColorAdjustType type) { }
        public void SetRemapTable(System.Drawing.Imaging.ColorMap[] map) { }
        public void SetRemapTable(System.Drawing.Imaging.ColorMap[] map, System.Drawing.Imaging.ColorAdjustType type) { }
        public void SetThreshold(float threshold) { }
        public void SetThreshold(float threshold, System.Drawing.Imaging.ColorAdjustType type) { }
        public void SetWrapMode(System.Drawing.Drawing2D.WrapMode mode) { }
        public void SetWrapMode(System.Drawing.Drawing2D.WrapMode mode, System.Drawing.Color color) { }
        public void SetWrapMode(System.Drawing.Drawing2D.WrapMode mode, System.Drawing.Color color, bool clamp) { }
    }
    [System.FlagsAttribute]
    public enum ImageCodecFlags
    {
        BlockingDecode = 32,
        Builtin = 65536,
        Decoder = 2,
        Encoder = 1,
        SeekableEncode = 16,
        SupportBitmap = 4,
        SupportVector = 8,
        System = 131072,
        User = 262144,
    }
    public sealed partial class ImageCodecInfo
    {
        internal ImageCodecInfo() { }
        public System.Guid Clsid { get { throw null; } set { } }
        public string CodecName { get { throw null; } set { } }
        public string DllName { get { throw null; } set { } }
        public string FilenameExtension { get { throw null; } set { } }
        public System.Drawing.Imaging.ImageCodecFlags Flags { get { throw null; } set { } }
        public string FormatDescription { get { throw null; } set { } }
        public System.Guid FormatID { get { throw null; } set { } }
        public string MimeType { get { throw null; } set { } }
        [System.CLSCompliantAttribute(false)]
        public byte[][] SignatureMasks { get { throw null; } set { } }
        [System.CLSCompliantAttribute(false)]
        public byte[][] SignaturePatterns { get { throw null; } set { } }
        public int Version { get { throw null; } set { } }
        public static System.Drawing.Imaging.ImageCodecInfo[] GetImageDecoders() { throw null; }
        public static System.Drawing.Imaging.ImageCodecInfo[] GetImageEncoders() { throw null; }
    }
    [System.FlagsAttribute]
    public enum ImageFlags
    {
        Caching = 131072,
        ColorSpaceCmyk = 32,
        ColorSpaceGray = 64,
        ColorSpaceRgb = 16,
        ColorSpaceYcbcr = 128,
        ColorSpaceYcck = 256,
        HasAlpha = 2,
        HasRealDpi = 4096,
        HasRealPixelSize = 8192,
        HasTranslucent = 4,
        None = 0,
        PartiallyScalable = 8,
        ReadOnly = 65536,
        Scalable = 1,
    }
    public sealed partial class ImageFormat
    {
        public ImageFormat(System.Guid guid) { }
        public static System.Drawing.Imaging.ImageFormat Bmp { get { throw null; } }
        public static System.Drawing.Imaging.ImageFormat Emf { get { throw null; } }
        public static System.Drawing.Imaging.ImageFormat Exif { get { throw null; } }
        public static System.Drawing.Imaging.ImageFormat Gif { get { throw null; } }
        public System.Guid Guid { get { throw null; } }
        public static System.Drawing.Imaging.ImageFormat Icon { get { throw null; } }
        public static System.Drawing.Imaging.ImageFormat Jpeg { get { throw null; } }
        public static System.Drawing.Imaging.ImageFormat MemoryBmp { get { throw null; } }
        public static System.Drawing.Imaging.ImageFormat Png { get { throw null; } }
        public static System.Drawing.Imaging.ImageFormat Tiff { get { throw null; } }
        public static System.Drawing.Imaging.ImageFormat Wmf { get { throw null; } }
        public override bool Equals(object o) { throw null; }
        public override int GetHashCode() { throw null; }
        public override string ToString() { throw null; }
    }
    public enum ImageLockMode
    {
        ReadOnly = 1,
        ReadWrite = 3,
        UserInputBuffer = 4,
        WriteOnly = 2,
    }
    public sealed partial class Metafile : System.Drawing.Image
    {
        public Metafile(System.IntPtr henhmetafile, bool deleteEmf) { }
        public Metafile(System.IntPtr referenceHdc, System.Drawing.Imaging.EmfType emfType) { }
        public Metafile(System.IntPtr referenceHdc, System.Drawing.Imaging.EmfType emfType, string description) { }
        public Metafile(System.IntPtr hmetafile, System.Drawing.Imaging.WmfPlaceableFileHeader wmfHeader) { }
        public Metafile(System.IntPtr hmetafile, System.Drawing.Imaging.WmfPlaceableFileHeader wmfHeader, bool deleteWmf) { }
        public Metafile(System.IntPtr referenceHdc, System.Drawing.Rectangle frameRect) { }
        public Metafile(System.IntPtr referenceHdc, System.Drawing.Rectangle frameRect, System.Drawing.Imaging.MetafileFrameUnit frameUnit) { }
        public Metafile(System.IntPtr referenceHdc, System.Drawing.Rectangle frameRect, System.Drawing.Imaging.MetafileFrameUnit frameUnit, System.Drawing.Imaging.EmfType type) { }
        public Metafile(System.IntPtr referenceHdc, System.Drawing.Rectangle frameRect, System.Drawing.Imaging.MetafileFrameUnit frameUnit, System.Drawing.Imaging.EmfType type, string desc) { }
        public Metafile(System.IntPtr referenceHdc, System.Drawing.RectangleF frameRect) { }
        public Metafile(System.IntPtr referenceHdc, System.Drawing.RectangleF frameRect, System.Drawing.Imaging.MetafileFrameUnit frameUnit) { }
        public Metafile(System.IntPtr referenceHdc, System.Drawing.RectangleF frameRect, System.Drawing.Imaging.MetafileFrameUnit frameUnit, System.Drawing.Imaging.EmfType type) { }
        public Metafile(System.IntPtr referenceHdc, System.Drawing.RectangleF frameRect, System.Drawing.Imaging.MetafileFrameUnit frameUnit, System.Drawing.Imaging.EmfType type, string description) { }
        public Metafile(System.IO.Stream stream) { }
        public Metafile(System.IO.Stream stream, System.IntPtr referenceHdc) { }
        public Metafile(System.IO.Stream stream, System.IntPtr referenceHdc, System.Drawing.Imaging.EmfType type) { }
        public Metafile(System.IO.Stream stream, System.IntPtr referenceHdc, System.Drawing.Imaging.EmfType type, string description) { }
        public Metafile(System.IO.Stream stream, System.IntPtr referenceHdc, System.Drawing.Rectangle frameRect) { }
        public Metafile(System.IO.Stream stream, System.IntPtr referenceHdc, System.Drawing.Rectangle frameRect, System.Drawing.Imaging.MetafileFrameUnit frameUnit) { }
        public Metafile(System.IO.Stream stream, System.IntPtr referenceHdc, System.Drawing.Rectangle frameRect, System.Drawing.Imaging.MetafileFrameUnit frameUnit, System.Drawing.Imaging.EmfType type) { }
        public Metafile(System.IO.Stream stream, System.IntPtr referenceHdc, System.Drawing.Rectangle frameRect, System.Drawing.Imaging.MetafileFrameUnit frameUnit, System.Drawing.Imaging.EmfType type, string description) { }
        public Metafile(System.IO.Stream stream, System.IntPtr referenceHdc, System.Drawing.RectangleF frameRect) { }
        public Metafile(System.IO.Stream stream, System.IntPtr referenceHdc, System.Drawing.RectangleF frameRect, System.Drawing.Imaging.MetafileFrameUnit frameUnit) { }
        public Metafile(System.IO.Stream stream, System.IntPtr referenceHdc, System.Drawing.RectangleF frameRect, System.Drawing.Imaging.MetafileFrameUnit frameUnit, System.Drawing.Imaging.EmfType type) { }
        public Metafile(System.IO.Stream stream, System.IntPtr referenceHdc, System.Drawing.RectangleF frameRect, System.Drawing.Imaging.MetafileFrameUnit frameUnit, System.Drawing.Imaging.EmfType type, string description) { }
        public Metafile(string filename) { }
        public Metafile(string fileName, System.IntPtr referenceHdc) { }
        public Metafile(string fileName, System.IntPtr referenceHdc, System.Drawing.Imaging.EmfType type) { }
        public Metafile(string fileName, System.IntPtr referenceHdc, System.Drawing.Imaging.EmfType type, string description) { }
        public Metafile(string fileName, System.IntPtr referenceHdc, System.Drawing.Rectangle frameRect) { }
        public Metafile(string fileName, System.IntPtr referenceHdc, System.Drawing.Rectangle frameRect, System.Drawing.Imaging.MetafileFrameUnit frameUnit) { }
        public Metafile(string fileName, System.IntPtr referenceHdc, System.Drawing.Rectangle frameRect, System.Drawing.Imaging.MetafileFrameUnit frameUnit, System.Drawing.Imaging.EmfType type) { }
        public Metafile(string fileName, System.IntPtr referenceHdc, System.Drawing.Rectangle frameRect, System.Drawing.Imaging.MetafileFrameUnit frameUnit, System.Drawing.Imaging.EmfType type, string description) { }
        public Metafile(string fileName, System.IntPtr referenceHdc, System.Drawing.Rectangle frameRect, System.Drawing.Imaging.MetafileFrameUnit frameUnit, string description) { }
        public Metafile(string fileName, System.IntPtr referenceHdc, System.Drawing.RectangleF frameRect) { }
        public Metafile(string fileName, System.IntPtr referenceHdc, System.Drawing.RectangleF frameRect, System.Drawing.Imaging.MetafileFrameUnit frameUnit) { }
        public Metafile(string fileName, System.IntPtr referenceHdc, System.Drawing.RectangleF frameRect, System.Drawing.Imaging.MetafileFrameUnit frameUnit, System.Drawing.Imaging.EmfType type) { }
        public Metafile(string fileName, System.IntPtr referenceHdc, System.Drawing.RectangleF frameRect, System.Drawing.Imaging.MetafileFrameUnit frameUnit, System.Drawing.Imaging.EmfType type, string description) { }
        public Metafile(string fileName, System.IntPtr referenceHdc, System.Drawing.RectangleF frameRect, System.Drawing.Imaging.MetafileFrameUnit frameUnit, string desc) { }
        public System.IntPtr GetHenhmetafile() { throw null; }
        public System.Drawing.Imaging.MetafileHeader GetMetafileHeader() { throw null; }
        public static System.Drawing.Imaging.MetafileHeader GetMetafileHeader(System.IntPtr henhmetafile) { throw null; }
        public static System.Drawing.Imaging.MetafileHeader GetMetafileHeader(System.IntPtr hmetafile, System.Drawing.Imaging.WmfPlaceableFileHeader wmfHeader) { throw null; }
        public static System.Drawing.Imaging.MetafileHeader GetMetafileHeader(System.IO.Stream stream) { throw null; }
        public static System.Drawing.Imaging.MetafileHeader GetMetafileHeader(string fileName) { throw null; }
        public void PlayRecord(System.Drawing.Imaging.EmfPlusRecordType recordType, int flags, int dataSize, byte[] data) { }
    }
    public enum MetafileFrameUnit
    {
        Document = 5,
        GdiCompatible = 7,
        Inch = 4,
        Millimeter = 6,
        Pixel = 2,
        Point = 3,
    }
    public sealed partial class MetafileHeader
    {
        internal MetafileHeader() { }
        public System.Drawing.Rectangle Bounds { get { throw null; } }
        public float DpiX { get { throw null; } }
        public float DpiY { get { throw null; } }
        public int EmfPlusHeaderSize { get { throw null; } }
        public int LogicalDpiX { get { throw null; } }
        public int LogicalDpiY { get { throw null; } }
        public int MetafileSize { get { throw null; } }
        public System.Drawing.Imaging.MetafileType Type { get { throw null; } }
        public int Version { get { throw null; } }
        public System.Drawing.Imaging.MetaHeader WmfHeader { get { throw null; } }
        public bool IsDisplay() { throw null; }
        public bool IsEmf() { throw null; }
        public bool IsEmfOrEmfPlus() { throw null; }
        public bool IsEmfPlus() { throw null; }
        public bool IsEmfPlusDual() { throw null; }
        public bool IsEmfPlusOnly() { throw null; }
        public bool IsWmf() { throw null; }
        public bool IsWmfPlaceable() { throw null; }
    }
    public enum MetafileType
    {
        Emf = 3,
        EmfPlusDual = 5,
        EmfPlusOnly = 4,
        Invalid = 0,
        Wmf = 1,
        WmfPlaceable = 2,
    }
    public sealed partial class MetaHeader
    {
        public MetaHeader() { }
        public short HeaderSize { get { throw null; } set { } }
        public int MaxRecord { get { throw null; } set { } }
        public short NoObjects { get { throw null; } set { } }
        public short NoParameters { get { throw null; } set { } }
        public int Size { get { throw null; } set { } }
        public short Type { get { throw null; } set { } }
        public short Version { get { throw null; } set { } }
    }
    [System.FlagsAttribute]
    public enum PaletteFlags
    {
        GrayScale = 2,
        Halftone = 4,
        HasAlpha = 1,
    }
    public enum PixelFormat
    {
        Alpha = 262144,
        Canonical = 2097152,
        DontCare = 0,
        Extended = 1048576,
        Format16bppArgb1555 = 397319,
        Format16bppGrayScale = 1052676,
        Format16bppRgb555 = 135173,
        Format16bppRgb565 = 135174,
        Format1bppIndexed = 196865,
        Format24bppRgb = 137224,
        Format32bppArgb = 2498570,
        Format32bppPArgb = 925707,
        Format32bppRgb = 139273,
        Format48bppRgb = 1060876,
        Format4bppIndexed = 197634,
        Format64bppArgb = 3424269,
        Format64bppPArgb = 1851406,
        Format8bppIndexed = 198659,
        Gdi = 131072,
        Indexed = 65536,
        Max = 15,
        PAlpha = 524288,
        Undefined = 0,
    }
    public delegate void PlayRecordCallback(System.Drawing.Imaging.EmfPlusRecordType recordType, int flags, int dataSize, System.IntPtr recordData);
    public sealed partial class PropertyItem
    {
        internal PropertyItem() { }
        public int Id { get { throw null; } set { } }
        public int Len { get { throw null; } set { } }
        public short Type { get { throw null; } set { } }
        public byte[] Value { get { throw null; } set { } }
    }
    public sealed partial class WmfPlaceableFileHeader
    {
        public WmfPlaceableFileHeader() { }
        public short BboxBottom { get { throw null; } set { } }
        public short BboxLeft { get { throw null; } set { } }
        public short BboxRight { get { throw null; } set { } }
        public short BboxTop { get { throw null; } set { } }
        public short Checksum { get { throw null; } set { } }
        public short Hmf { get { throw null; } set { } }
        public short Inch { get { throw null; } set { } }
        public int Key { get { throw null; } set { } }
        public int Reserved { get { throw null; } set { } }
    }
}
namespace System.Drawing.Printing
{
    public enum Duplex
    {
        Default = -1,
        Horizontal = 3,
        Simplex = 1,
        Vertical = 2,
    }
    public partial class InvalidPrinterException : System.SystemException
    {
        public InvalidPrinterException(System.Drawing.Printing.PrinterSettings settings) { }
        protected InvalidPrinterException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    }
    public partial class Margins : System.ICloneable
    {
        public Margins() { }
        public Margins(int left, int right, int top, int bottom) { }
        public int Bottom { get { throw null; } set { } }
        public int Left { get { throw null; } set { } }
        public int Right { get { throw null; } set { } }
        public int Top { get { throw null; } set { } }
        public object Clone() { throw null; }
        public override bool Equals(object obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Drawing.Printing.Margins m1, System.Drawing.Printing.Margins m2) { throw null; }
        public static bool operator !=(System.Drawing.Printing.Margins m1, System.Drawing.Printing.Margins m2) { throw null; }
        public override string ToString() { throw null; }
    }
    public partial class PageSettings : System.ICloneable
    {
        public PageSettings() { }
        public PageSettings(System.Drawing.Printing.PrinterSettings printerSettings) { }
        public System.Drawing.Rectangle Bounds { get { throw null; } }
        public bool Color { get { throw null; } set { } }
        public float HardMarginX { get { throw null; } }
        public float HardMarginY { get { throw null; } }
        public bool Landscape { get { throw null; } set { } }
        public System.Drawing.Printing.Margins Margins { get { throw null; } set { } }
        public System.Drawing.Printing.PaperSize PaperSize { get { throw null; } set { } }
        public System.Drawing.Printing.PaperSource PaperSource { get { throw null; } set { } }
        public System.Drawing.RectangleF PrintableArea { get { throw null; } }
        public System.Drawing.Printing.PrinterResolution PrinterResolution { get { throw null; } set { } }
        public System.Drawing.Printing.PrinterSettings PrinterSettings { get { throw null; } set { } }
        public object Clone() { throw null; }
        public void CopyToHdevmode(System.IntPtr hdevmode) { }
        public void SetHdevmode(System.IntPtr hdevmode) { }
        public override string ToString() { throw null; }
    }
    public enum PaperKind
    {
        A2 = 66,
        A3 = 8,
        A3Extra = 63,
        A3ExtraTransverse = 68,
        A3Rotated = 76,
        A3Transverse = 67,
        A4 = 9,
        A4Extra = 53,
        A4Plus = 60,
        A4Rotated = 77,
        A4Small = 10,
        A4Transverse = 55,
        A5 = 11,
        A5Extra = 64,
        A5Rotated = 78,
        A5Transverse = 61,
        A6 = 70,
        A6Rotated = 83,
        APlus = 57,
        B4 = 12,
        B4Envelope = 33,
        B4JisRotated = 79,
        B5 = 13,
        B5Envelope = 34,
        B5Extra = 65,
        B5JisRotated = 80,
        B5Transverse = 62,
        B6Envelope = 35,
        B6Jis = 88,
        B6JisRotated = 89,
        BPlus = 58,
        C3Envelope = 29,
        C4Envelope = 30,
        C5Envelope = 28,
        C65Envelope = 32,
        C6Envelope = 31,
        CSheet = 24,
        Custom = 0,
        DLEnvelope = 27,
        DSheet = 25,
        ESheet = 26,
        Executive = 7,
        Folio = 14,
        GermanLegalFanfold = 41,
        GermanStandardFanfold = 40,
        InviteEnvelope = 47,
        IsoB4 = 42,
        ItalyEnvelope = 36,
        JapaneseDoublePostcard = 69,
        JapaneseDoublePostcardRotated = 82,
        JapaneseEnvelopeChouNumber3 = 73,
        JapaneseEnvelopeChouNumber3Rotated = 86,
        JapaneseEnvelopeChouNumber4 = 74,
        JapaneseEnvelopeChouNumber4Rotated = 87,
        JapaneseEnvelopeKakuNumber2 = 71,
        JapaneseEnvelopeKakuNumber2Rotated = 84,
        JapaneseEnvelopeKakuNumber3 = 72,
        JapaneseEnvelopeKakuNumber3Rotated = 85,
        JapaneseEnvelopeYouNumber4 = 91,
        JapaneseEnvelopeYouNumber4Rotated = 92,
        JapanesePostcard = 43,
        JapanesePostcardRotated = 81,
        Ledger = 4,
        Legal = 5,
        LegalExtra = 51,
        Letter = 1,
        LetterExtra = 50,
        LetterExtraTransverse = 56,
        LetterPlus = 59,
        LetterRotated = 75,
        LetterSmall = 2,
        LetterTransverse = 54,
        MonarchEnvelope = 37,
        Note = 18,
        Number10Envelope = 20,
        Number11Envelope = 21,
        Number12Envelope = 22,
        Number14Envelope = 23,
        Number9Envelope = 19,
        PersonalEnvelope = 38,
        Prc16K = 93,
        Prc16KRotated = 106,
        Prc32K = 94,
        Prc32KBig = 95,
        Prc32KBigRotated = 108,
        Prc32KRotated = 107,
        PrcEnvelopeNumber1 = 96,
        PrcEnvelopeNumber10 = 105,
        PrcEnvelopeNumber10Rotated = 118,
        PrcEnvelopeNumber1Rotated = 109,
        PrcEnvelopeNumber2 = 97,
        PrcEnvelopeNumber2Rotated = 110,
        PrcEnvelopeNumber3 = 98,
        PrcEnvelopeNumber3Rotated = 111,
        PrcEnvelopeNumber4 = 99,
        PrcEnvelopeNumber4Rotated = 112,
        PrcEnvelopeNumber5 = 100,
        PrcEnvelopeNumber5Rotated = 113,
        PrcEnvelopeNumber6 = 101,
        PrcEnvelopeNumber6Rotated = 114,
        PrcEnvelopeNumber7 = 102,
        PrcEnvelopeNumber7Rotated = 115,
        PrcEnvelopeNumber8 = 103,
        PrcEnvelopeNumber8Rotated = 116,
        PrcEnvelopeNumber9 = 104,
        PrcEnvelopeNumber9Rotated = 117,
        Quarto = 15,
        Standard10x11 = 45,
        Standard10x14 = 16,
        Standard11x17 = 17,
        Standard12x11 = 90,
        Standard15x11 = 46,
        Standard9x11 = 44,
        Statement = 6,
        Tabloid = 3,
        TabloidExtra = 52,
        USStandardFanfold = 39,
    }
    public partial class PaperSize
    {
        public PaperSize() { }
        public PaperSize(string name, int width, int height) { }
        public int Height { get { throw null; } set { } }
        public System.Drawing.Printing.PaperKind Kind { get { throw null; } }
        public string PaperName { get { throw null; } set { } }
        public int RawKind { get { throw null; } set { } }
        public int Width { get { throw null; } set { } }
        public override string ToString() { throw null; }
    }
    public partial class PaperSource
    {
        public PaperSource() { }
        public System.Drawing.Printing.PaperSourceKind Kind { get { throw null; } }
        public int RawKind { get { throw null; } set { } }
        public string SourceName { get { throw null; } set { } }
        public override string ToString() { throw null; }
    }
    public enum PaperSourceKind
    {
        AutomaticFeed = 7,
        Cassette = 14,
        Custom = 257,
        Envelope = 5,
        FormSource = 15,
        LargeCapacity = 11,
        LargeFormat = 10,
        Lower = 2,
        Manual = 4,
        ManualFeed = 6,
        Middle = 3,
        SmallFormat = 9,
        TractorFeed = 8,
        Upper = 1,
    }
    public sealed partial class PreviewPageInfo
    {
        public PreviewPageInfo(System.Drawing.Image image, System.Drawing.Size physicalSize) { }
        public System.Drawing.Image Image { get { throw null; } }
        public System.Drawing.Size PhysicalSize { get { throw null; } }
    }
    public partial class PreviewPrintController : System.Drawing.Printing.PrintController
    {
        public PreviewPrintController() { }
        public override bool IsPreview { get { throw null; } }
        public virtual bool UseAntiAlias { get { throw null; } set { } }
        public System.Drawing.Printing.PreviewPageInfo[] GetPreviewPageInfo() { throw null; }
        public override void OnEndPage(System.Drawing.Printing.PrintDocument document, System.Drawing.Printing.PrintPageEventArgs e) { }
        public override void OnEndPrint(System.Drawing.Printing.PrintDocument document, System.Drawing.Printing.PrintEventArgs e) { }
        public override System.Drawing.Graphics OnStartPage(System.Drawing.Printing.PrintDocument document, System.Drawing.Printing.PrintPageEventArgs e) { throw null; }
        public override void OnStartPrint(System.Drawing.Printing.PrintDocument document, System.Drawing.Printing.PrintEventArgs e) { }
    }
    public enum PrintAction
    {
        PrintToFile = 0,
        PrintToPreview = 1,
        PrintToPrinter = 2,
    }
    public abstract partial class PrintController
    {
        protected PrintController() { }
        public virtual bool IsPreview { get { throw null; } }
        public virtual void OnEndPage(System.Drawing.Printing.PrintDocument document, System.Drawing.Printing.PrintPageEventArgs e) { }
        public virtual void OnEndPrint(System.Drawing.Printing.PrintDocument document, System.Drawing.Printing.PrintEventArgs e) { }
        public virtual System.Drawing.Graphics OnStartPage(System.Drawing.Printing.PrintDocument document, System.Drawing.Printing.PrintPageEventArgs e) { throw null; }
        public virtual void OnStartPrint(System.Drawing.Printing.PrintDocument document, System.Drawing.Printing.PrintEventArgs e) { }
    }
    public partial class PrintDocument : System.ComponentModel.Component
    {
        public PrintDocument() { }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.DesignerSerializationVisibilityAttribute((System.ComponentModel.DesignerSerializationVisibility)(0))]
        public System.Drawing.Printing.PageSettings DefaultPageSettings { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute("document")]
        public string DocumentName { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(false)]
        public bool OriginAtMargins { get { throw null; } set { } }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.DesignerSerializationVisibilityAttribute((System.ComponentModel.DesignerSerializationVisibility)(0))]
        public System.Drawing.Printing.PrintController PrintController { get { throw null; } set { } }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.DesignerSerializationVisibilityAttribute((System.ComponentModel.DesignerSerializationVisibility)(0))]
        public System.Drawing.Printing.PrinterSettings PrinterSettings { get { throw null; } set { } }
        public event System.Drawing.Printing.PrintEventHandler BeginPrint { add { } remove { } }
        public event System.Drawing.Printing.PrintEventHandler EndPrint { add { } remove { } }
        public event System.Drawing.Printing.PrintPageEventHandler PrintPage { add { } remove { } }
        public event System.Drawing.Printing.QueryPageSettingsEventHandler QueryPageSettings { add { } remove { } }
        protected virtual void OnBeginPrint(System.Drawing.Printing.PrintEventArgs e) { }
        protected virtual void OnEndPrint(System.Drawing.Printing.PrintEventArgs e) { }
        protected virtual void OnPrintPage(System.Drawing.Printing.PrintPageEventArgs e) { }
        protected virtual void OnQueryPageSettings(System.Drawing.Printing.QueryPageSettingsEventArgs e) { }
        public void Print() { }
        public override string ToString() { throw null; }
    }
    public partial class PrinterResolution
    {
        public PrinterResolution() { }
        public System.Drawing.Printing.PrinterResolutionKind Kind { get { throw null; } set { } }
        public int X { get { throw null; } set { } }
        public int Y { get { throw null; } set { } }
        public override string ToString() { throw null; }
    }
    public enum PrinterResolutionKind
    {
        Custom = 0,
        Draft = -1,
        High = -4,
        Low = -2,
        Medium = -3,
    }
    public partial class PrinterSettings : System.ICloneable
    {
        public PrinterSettings() { }
        public bool CanDuplex { get { throw null; } }
        public bool Collate { get { throw null; } set { } }
        public short Copies { get { throw null; } set { } }
        public System.Drawing.Printing.PageSettings DefaultPageSettings { get { throw null; } }
        public System.Drawing.Printing.Duplex Duplex { get { throw null; } set { } }
        public int FromPage { get { throw null; } set { } }
        public static System.Drawing.Printing.PrinterSettings.StringCollection InstalledPrinters { get { throw null; } }
        public bool IsDefaultPrinter { get { throw null; } }
        public bool IsPlotter { get { throw null; } }
        public bool IsValid { get { throw null; } }
        public int LandscapeAngle { get { throw null; } }
        public int MaximumCopies { get { throw null; } }
        public int MaximumPage { get { throw null; } set { } }
        public int MinimumPage { get { throw null; } set { } }
        public System.Drawing.Printing.PrinterSettings.PaperSizeCollection PaperSizes { get { throw null; } }
        public System.Drawing.Printing.PrinterSettings.PaperSourceCollection PaperSources { get { throw null; } }
        public string PrinterName { get { throw null; } set { } }
        public System.Drawing.Printing.PrinterSettings.PrinterResolutionCollection PrinterResolutions { get { throw null; } }
        public string PrintFileName { get { throw null; } set { } }
        public System.Drawing.Printing.PrintRange PrintRange { get { throw null; } set { } }
        public bool PrintToFile { get { throw null; } set { } }
        public bool SupportsColor { get { throw null; } }
        public int ToPage { get { throw null; } set { } }
        public object Clone() { throw null; }
        public System.Drawing.Graphics CreateMeasurementGraphics() { throw null; }
        public System.Drawing.Graphics CreateMeasurementGraphics(bool honorOriginAtMargins) { throw null; }
        public System.Drawing.Graphics CreateMeasurementGraphics(System.Drawing.Printing.PageSettings pageSettings) { throw null; }
        public System.Drawing.Graphics CreateMeasurementGraphics(System.Drawing.Printing.PageSettings pageSettings, bool honorOriginAtMargins) { throw null; }
        public System.IntPtr GetHdevmode() { throw null; }
        public System.IntPtr GetHdevmode(System.Drawing.Printing.PageSettings pageSettings) { throw null; }
        public System.IntPtr GetHdevnames() { throw null; }
        public bool IsDirectPrintingSupported(System.Drawing.Image image) { throw null; }
        public bool IsDirectPrintingSupported(System.Drawing.Imaging.ImageFormat imageFormat) { throw null; }
        public void SetHdevmode(System.IntPtr hdevmode) { }
        public void SetHdevnames(System.IntPtr hdevnames) { }
        public override string ToString() { throw null; }
        public partial class PaperSizeCollection : System.Collections.ICollection, System.Collections.IEnumerable
        {
            public PaperSizeCollection(System.Drawing.Printing.PaperSize[] array) { }
            public int Count { get { throw null; } }
            public virtual System.Drawing.Printing.PaperSize this[int index] { get { throw null; } }
            int System.Collections.ICollection.Count { get { throw null; } }
            bool System.Collections.ICollection.IsSynchronized { get { throw null; } }
            object System.Collections.ICollection.SyncRoot { get { throw null; } }
            [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
            public int Add(System.Drawing.Printing.PaperSize paperSize) { throw null; }
            public void CopyTo(System.Drawing.Printing.PaperSize[] paperSizes, int index) { }
            public System.Collections.IEnumerator GetEnumerator() { throw null; }
            void System.Collections.ICollection.CopyTo(System.Array array, int index) { }
            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        }
        public partial class PaperSourceCollection : System.Collections.ICollection, System.Collections.IEnumerable
        {
            public PaperSourceCollection(System.Drawing.Printing.PaperSource[] array) { }
            public int Count { get { throw null; } }
            public virtual System.Drawing.Printing.PaperSource this[int index] { get { throw null; } }
            int System.Collections.ICollection.Count { get { throw null; } }
            bool System.Collections.ICollection.IsSynchronized { get { throw null; } }
            object System.Collections.ICollection.SyncRoot { get { throw null; } }
            [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
            public int Add(System.Drawing.Printing.PaperSource paperSource) { throw null; }
            public void CopyTo(System.Drawing.Printing.PaperSource[] paperSources, int index) { }
            public System.Collections.IEnumerator GetEnumerator() { throw null; }
            void System.Collections.ICollection.CopyTo(System.Array array, int index) { }
            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        }
        public partial class PrinterResolutionCollection : System.Collections.ICollection, System.Collections.IEnumerable
        {
            public PrinterResolutionCollection(System.Drawing.Printing.PrinterResolution[] array) { }
            public int Count { get { throw null; } }
            public virtual System.Drawing.Printing.PrinterResolution this[int index] { get { throw null; } }
            int System.Collections.ICollection.Count { get { throw null; } }
            bool System.Collections.ICollection.IsSynchronized { get { throw null; } }
            object System.Collections.ICollection.SyncRoot { get { throw null; } }
            [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
            public int Add(System.Drawing.Printing.PrinterResolution printerResolution) { throw null; }
            public void CopyTo(System.Drawing.Printing.PrinterResolution[] printerResolutions, int index) { }
            public System.Collections.IEnumerator GetEnumerator() { throw null; }
            void System.Collections.ICollection.CopyTo(System.Array array, int index) { }
            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        }
        public partial class StringCollection : System.Collections.ICollection, System.Collections.IEnumerable
        {
            public StringCollection(string[] array) { }
            public int Count { get { throw null; } }
            public virtual string this[int index] { get { throw null; } }
            int System.Collections.ICollection.Count { get { throw null; } }
            bool System.Collections.ICollection.IsSynchronized { get { throw null; } }
            object System.Collections.ICollection.SyncRoot { get { throw null; } }
            [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
            public int Add(string value) { throw null; }
            public void CopyTo(string[] strings, int index) { }
            public System.Collections.IEnumerator GetEnumerator() { throw null; }
            void System.Collections.ICollection.CopyTo(System.Array array, int index) { }
            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        }
    }
    public enum PrinterUnit
    {
        Display = 0,
        HundredthsOfAMillimeter = 2,
        TenthsOfAMillimeter = 3,
        ThousandthsOfAnInch = 1,
    }
    public sealed partial class PrinterUnitConvert
    {
        internal PrinterUnitConvert() { }
        public static double Convert(double value, System.Drawing.Printing.PrinterUnit fromUnit, System.Drawing.Printing.PrinterUnit toUnit) { throw null; }
        public static System.Drawing.Point Convert(System.Drawing.Point value, System.Drawing.Printing.PrinterUnit fromUnit, System.Drawing.Printing.PrinterUnit toUnit) { throw null; }
        public static System.Drawing.Printing.Margins Convert(System.Drawing.Printing.Margins value, System.Drawing.Printing.PrinterUnit fromUnit, System.Drawing.Printing.PrinterUnit toUnit) { throw null; }
        public static System.Drawing.Rectangle Convert(System.Drawing.Rectangle value, System.Drawing.Printing.PrinterUnit fromUnit, System.Drawing.Printing.PrinterUnit toUnit) { throw null; }
        public static System.Drawing.Size Convert(System.Drawing.Size value, System.Drawing.Printing.PrinterUnit fromUnit, System.Drawing.Printing.PrinterUnit toUnit) { throw null; }
        public static int Convert(int value, System.Drawing.Printing.PrinterUnit fromUnit, System.Drawing.Printing.PrinterUnit toUnit) { throw null; }
    }
    public partial class PrintEventArgs : System.ComponentModel.CancelEventArgs
    {
        public PrintEventArgs() { }
        public System.Drawing.Printing.PrintAction PrintAction { get { throw null; } }
    }
    public delegate void PrintEventHandler(object sender, System.Drawing.Printing.PrintEventArgs e);
    public partial class PrintPageEventArgs : System.EventArgs
    {
        public PrintPageEventArgs(System.Drawing.Graphics graphics, System.Drawing.Rectangle marginBounds, System.Drawing.Rectangle pageBounds, System.Drawing.Printing.PageSettings pageSettings) { }
        public bool Cancel { get { throw null; } set { } }
        public System.Drawing.Graphics Graphics { get { throw null; } }
        public bool HasMorePages { get { throw null; } set { } }
        public System.Drawing.Rectangle MarginBounds { get { throw null; } }
        public System.Drawing.Rectangle PageBounds { get { throw null; } }
        public System.Drawing.Printing.PageSettings PageSettings { get { throw null; } }
    }
    public delegate void PrintPageEventHandler(object sender, System.Drawing.Printing.PrintPageEventArgs e);
    public enum PrintRange
    {
        AllPages = 0,
        CurrentPage = 4194304,
        Selection = 1,
        SomePages = 2,
    }
    public partial class QueryPageSettingsEventArgs : System.Drawing.Printing.PrintEventArgs
    {
        public QueryPageSettingsEventArgs(System.Drawing.Printing.PageSettings pageSettings) { }
        public System.Drawing.Printing.PageSettings PageSettings { get { throw null; } set { } }
    }
    public delegate void QueryPageSettingsEventHandler(object sender, System.Drawing.Printing.QueryPageSettingsEventArgs e);
    public partial class StandardPrintController : System.Drawing.Printing.PrintController
    {
        public StandardPrintController() { }
        public override void OnEndPage(System.Drawing.Printing.PrintDocument document, System.Drawing.Printing.PrintPageEventArgs e) { }
        public override void OnEndPrint(System.Drawing.Printing.PrintDocument document, System.Drawing.Printing.PrintEventArgs e) { }
        public override System.Drawing.Graphics OnStartPage(System.Drawing.Printing.PrintDocument document, System.Drawing.Printing.PrintPageEventArgs e) { throw null; }
        public override void OnStartPrint(System.Drawing.Printing.PrintDocument document, System.Drawing.Printing.PrintEventArgs e) { }
    }
}
namespace System.Drawing.Text
{
    public abstract partial class FontCollection : System.IDisposable
    {
        internal FontCollection() { }
        public System.Drawing.FontFamily[] Families { get { throw null; } }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        ~FontCollection() { }
    }
    public enum GenericFontFamilies
    {
        Monospace = 2,
        SansSerif = 1,
        Serif = 0,
    }
    public enum HotkeyPrefix
    {
        Hide = 2,
        None = 0,
        Show = 1,
    }
    public sealed partial class InstalledFontCollection : System.Drawing.Text.FontCollection
    {
        public InstalledFontCollection() { }
    }
    public sealed partial class PrivateFontCollection : System.Drawing.Text.FontCollection
    {
        public PrivateFontCollection() { }
        public void AddFontFile(string filename) { }
        public void AddMemoryFont(System.IntPtr memory, int length) { }
        protected override void Dispose(bool disposing) { }
    }
    public enum TextRenderingHint
    {
        AntiAlias = 4,
        AntiAliasGridFit = 3,
        ClearTypeGridFit = 5,
        SingleBitPerPixel = 2,
        SingleBitPerPixelGridFit = 1,
        SystemDefault = 0,
    }
}

// Removed stuff:
/*
namespace System.Drawing.Design
{
    public delegate System.Drawing.Design.ToolboxItem ToolboxItemCreatorCallback(object serializedObject, string format);
    public partial class PropertyValueUIItem
    {
        public PropertyValueUIItem(System.Drawing.Image uiItemImage, System.Drawing.Design.PropertyValueUIItemInvokeHandler handler, string tooltip) { }
        public virtual System.Drawing.Image Image { get { throw null; } }
        public virtual System.Drawing.Design.PropertyValueUIItemInvokeHandler InvokeHandler { get { throw null; } }
        public virtual string ToolTip { get { throw null; } }
        public virtual void Reset() { }
    }
    public partial interface IPropertyValueUIService
    {
        event System.EventHandler PropertyUIValueItemsChanged;
        void AddPropertyValueUIHandler(System.Drawing.Design.PropertyValueUIHandler newHandler);
        System.Drawing.Design.PropertyValueUIItem[] GetPropertyUIValueItems(System.ComponentModel.ITypeDescriptorContext context, System.ComponentModel.PropertyDescriptor propDesc);
        void NotifyPropertyValueUIItemsChanged();
        void RemovePropertyValueUIHandler(System.Drawing.Design.PropertyValueUIHandler newHandler);
    }
    public partial class PaintValueEventArgs : System.EventArgs
    {
        public PaintValueEventArgs(System.ComponentModel.ITypeDescriptorContext context, object value, System.Drawing.Graphics graphics, System.Drawing.Rectangle bounds) { }
        public System.Drawing.Rectangle Bounds { get { throw null; } }
        public System.ComponentModel.ITypeDescriptorContext Context { get { throw null; } }
        public System.Drawing.Graphics Graphics { get { throw null; } }
        public object Value { get { throw null; } }
    }
    public delegate void PropertyValueUIHandler(System.ComponentModel.ITypeDescriptorContext context, System.ComponentModel.PropertyDescriptor propDesc, System.Collections.ArrayList valueUIItemList);
    public delegate void PropertyValueUIItemInvokeHandler(System.ComponentModel.ITypeDescriptorContext context, System.ComponentModel.PropertyDescriptor descriptor, System.Drawing.Design.PropertyValueUIItem invokedItem);
    public partial class UITypeEditor
    {
        public UITypeEditor() { }
        public virtual bool IsDropDownResizable { get { throw null; } }
        public virtual object EditValue(System.ComponentModel.ITypeDescriptorContext context, System.IServiceProvider provider, object value) { throw null; }
        public object EditValue(System.IServiceProvider provider, object value) { throw null; }
        public System.Drawing.Design.UITypeEditorEditStyle GetEditStyle() { throw null; }
        public virtual System.Drawing.Design.UITypeEditorEditStyle GetEditStyle(System.ComponentModel.ITypeDescriptorContext context) { throw null; }
        public bool GetPaintValueSupported() { throw null; }
        public virtual bool GetPaintValueSupported(System.ComponentModel.ITypeDescriptorContext context) { throw null; }
        public virtual void PaintValue(System.Drawing.Design.PaintValueEventArgs e) { }
        public void PaintValue(object value, System.Drawing.Graphics canvas, System.Drawing.Rectangle rectangle) { }
    }
    public enum UITypeEditorEditStyle
    {
        DropDown = 3,
        Modal = 2,
        None = 1,
    }
    [System.Runtime.InteropServices.GuidAttribute("4BACD258-DE64-4048-BC4E-FEDBEF9ACB76")]
    [System.Runtime.InteropServices.InterfaceTypeAttribute((System.Runtime.InteropServices.ComInterfaceType)(1))]
    public partial interface IToolboxService
    {
        System.Drawing.Design.CategoryNameCollection CategoryNames { get; }
        string SelectedCategory { get; set; }
        void AddCreator(System.Drawing.Design.ToolboxItemCreatorCallback creator, string format);
        void AddCreator(System.Drawing.Design.ToolboxItemCreatorCallback creator, string format, System.ComponentModel.Design.IDesignerHost host);
        void AddLinkedToolboxItem(System.Drawing.Design.ToolboxItem toolboxItem, System.ComponentModel.Design.IDesignerHost host);
        void AddLinkedToolboxItem(System.Drawing.Design.ToolboxItem toolboxItem, string category, System.ComponentModel.Design.IDesignerHost host);
        void AddToolboxItem(System.Drawing.Design.ToolboxItem toolboxItem);
        void AddToolboxItem(System.Drawing.Design.ToolboxItem toolboxItem, string category);
        System.Drawing.Design.ToolboxItem DeserializeToolboxItem(object serializedObject);
        System.Drawing.Design.ToolboxItem DeserializeToolboxItem(object serializedObject, System.ComponentModel.Design.IDesignerHost host);
        System.Drawing.Design.ToolboxItem GetSelectedToolboxItem();
        System.Drawing.Design.ToolboxItem GetSelectedToolboxItem(System.ComponentModel.Design.IDesignerHost host);
        System.Drawing.Design.ToolboxItemCollection GetToolboxItems();
        System.Drawing.Design.ToolboxItemCollection GetToolboxItems(System.ComponentModel.Design.IDesignerHost host);
        System.Drawing.Design.ToolboxItemCollection GetToolboxItems(string category);
        System.Drawing.Design.ToolboxItemCollection GetToolboxItems(string category, System.ComponentModel.Design.IDesignerHost host);
        bool IsSupported(object serializedObject, System.Collections.ICollection filterAttributes);
        bool IsSupported(object serializedObject, System.ComponentModel.Design.IDesignerHost host);
        bool IsToolboxItem(object serializedObject);
        bool IsToolboxItem(object serializedObject, System.ComponentModel.Design.IDesignerHost host);
        void Refresh();
        void RemoveCreator(string format);
        void RemoveCreator(string format, System.ComponentModel.Design.IDesignerHost host);
        void RemoveToolboxItem(System.Drawing.Design.ToolboxItem toolboxItem);
        void RemoveToolboxItem(System.Drawing.Design.ToolboxItem toolboxItem, string category);
        void SelectedToolboxItemUsed();
        object SerializeToolboxItem(System.Drawing.Design.ToolboxItem toolboxItem);
        bool SetCursor();
        void SetSelectedToolboxItem(System.Drawing.Design.ToolboxItem toolboxItem);
    }
    public partial class ToolboxItem : System.Runtime.Serialization.ISerializable
    {
        public ToolboxItem() { }
        public ToolboxItem(System.Type toolType) { }
        public System.Reflection.AssemblyName AssemblyName { get { throw null; } set { } }
        public System.Drawing.Bitmap Bitmap { get { throw null; } set { } }
        public string Company { get { throw null; } set { } }
        public virtual string ComponentType { get { throw null; } }
        public System.Reflection.AssemblyName[] DependentAssemblies { get { throw null; } set { } }
        public string Description { get { throw null; } set { } }
        public string DisplayName { get { throw null; } set { } }
        public System.Collections.ICollection Filter { get { throw null; } set { } }
        public bool IsTransient { get { throw null; } set { } }
        public virtual bool Locked { get { throw null; } }
        public System.Drawing.Bitmap OriginalBitmap { get { throw null; } set { } }
        public System.Collections.IDictionary Properties { get { throw null; } }
        public string TypeName { get { throw null; } set { } }
        public virtual string Version { get { throw null; } }
        public event System.Drawing.Design.ToolboxComponentsCreatedEventHandler ComponentsCreated { add { } remove { } }
        public event System.Drawing.Design.ToolboxComponentsCreatingEventHandler ComponentsCreating { add { } remove { } }
        protected void CheckUnlocked() { }
        public System.ComponentModel.IComponent[] CreateComponents() { throw null; }
        public System.ComponentModel.IComponent[] CreateComponents(System.ComponentModel.Design.IDesignerHost host) { throw null; }
        public System.ComponentModel.IComponent[] CreateComponents(System.ComponentModel.Design.IDesignerHost host, System.Collections.IDictionary defaultValues) { throw null; }
        protected virtual System.ComponentModel.IComponent[] CreateComponentsCore(System.ComponentModel.Design.IDesignerHost host) { throw null; }
        protected virtual System.ComponentModel.IComponent[] CreateComponentsCore(System.ComponentModel.Design.IDesignerHost host, System.Collections.IDictionary defaultValues) { throw null; }
        protected virtual void Deserialize(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public override bool Equals(object obj) { throw null; }
        protected virtual object FilterPropertyValue(string propertyName, object value) { throw null; }
        public override int GetHashCode() { throw null; }
        public System.Type GetType(System.ComponentModel.Design.IDesignerHost host) { throw null; }
        protected virtual System.Type GetType(System.ComponentModel.Design.IDesignerHost host, System.Reflection.AssemblyName assemblyName, string typeName, bool reference) { throw null; }
        public virtual void Initialize(System.Type type) { }
        public virtual void Lock() { }
        protected virtual void OnComponentsCreated(System.Drawing.Design.ToolboxComponentsCreatedEventArgs args) { }
        protected virtual void OnComponentsCreating(System.Drawing.Design.ToolboxComponentsCreatingEventArgs args) { }
        protected virtual void Serialize(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        void System.Runtime.Serialization.ISerializable.GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public override string ToString() { throw null; }
        protected void ValidatePropertyType(string propertyName, object value, System.Type expectedType, bool allowNull) { }
        protected virtual object ValidatePropertyValue(string propertyName, object value) { throw null; }
    }
    public partial interface IToolboxItemProvider
    {
        System.Drawing.Design.ToolboxItemCollection Items { get; }
    }
    public partial interface IToolboxUser
    {
        bool GetToolSupported(System.Drawing.Design.ToolboxItem tool);
        void ToolPicked(System.Drawing.Design.ToolboxItem tool);
    }
    public sealed partial class ToolboxItemCollection : System.Collections.ReadOnlyCollectionBase
    {
        public ToolboxItemCollection(System.Drawing.Design.ToolboxItemCollection value) { }
        public ToolboxItemCollection(System.Drawing.Design.ToolboxItem[] value) { }
        public System.Drawing.Design.ToolboxItem this[int index] { get { throw null; } }
        public bool Contains(System.Drawing.Design.ToolboxItem value) { throw null; }
        public void CopyTo(System.Drawing.Design.ToolboxItem[] array, int index) { }
        public int IndexOf(System.Drawing.Design.ToolboxItem value) { throw null; }
    }
    public partial class ToolboxComponentsCreatedEventArgs : System.EventArgs
    {
        public ToolboxComponentsCreatedEventArgs(System.ComponentModel.IComponent[] components) { }
        public System.ComponentModel.IComponent[] Components { get { throw null; } }
    }
    public delegate void ToolboxComponentsCreatedEventHandler(object sender, System.Drawing.Design.ToolboxComponentsCreatedEventArgs e);
    public partial class ToolboxComponentsCreatingEventArgs : System.EventArgs
    {
        public ToolboxComponentsCreatingEventArgs(System.ComponentModel.Design.IDesignerHost host) { }
        public System.ComponentModel.Design.IDesignerHost DesignerHost { get { throw null; } }
    }
    public delegate void ToolboxComponentsCreatingEventHandler(object sender, System.Drawing.Design.ToolboxComponentsCreatingEventArgs e);

}

*/
