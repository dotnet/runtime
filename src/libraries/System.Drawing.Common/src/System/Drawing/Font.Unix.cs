// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.
//
// System.Drawing.Fonts.cs
//
// Authors:
//    Alexandre Pigolkine (pigolkine@gmx.de)
//    Miguel de Icaza (miguel@ximian.com)
//    Todd Berman (tberman@sevenl.com)
//    Jordi Mas i Hernandez (jordi@ximian.com)
//    Ravindra (rkumar@novell.com)
//
// Copyright (C) 2004 Ximian, Inc. (http://www.ximian.com)
// Copyright (C) 2004, 2006 Novell, Inc (http://www.novell.com)
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

using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.InteropServices;
using System.ComponentModel;

namespace System.Drawing
{
    [Serializable]
#if !NETCORE
    [Editor ("System.Drawing.Design.FontEditor, " + Consts.AssemblySystem_Drawing_Design, typeof (System.Drawing.Design.UITypeEditor))]
    [TypeConverter (typeof (FontConverter))]
#endif
    public sealed class Font : MarshalByRefObject, ISerializable, ICloneable, IDisposable
    {
        private IntPtr fontObject = IntPtr.Zero;
        private string systemFontName;
        private string originalFontName;
        private float _size;

        private const byte DefaultCharSet = 1;
        private static int CharSetOffset = -1;

        private void CreateFont(string familyName, float emSize, FontStyle style, GraphicsUnit unit, byte charSet, bool isVertical)
        {
            originalFontName = familyName;
            FontFamily family;
            // NOTE: If family name is null, empty or invalid,
            // MS creates Microsoft Sans Serif font.
            try
            {
                family = new FontFamily(familyName);
            }
            catch (Exception)
            {
                family = FontFamily.GenericSansSerif;
            }

            setProperties(family, emSize, style, unit, charSet, isVertical);
            int status = SafeNativeMethods.Gdip.GdipCreateFont(family.NativeFamily, emSize, style, unit, out fontObject);

            if (status == SafeNativeMethods.Gdip.FontStyleNotFound)
                throw new ArgumentException(string.Format("Style {0} isn't supported by font {1}.", style.ToString(), familyName));

            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        private Font(SerializationInfo info, StreamingContext context)
        {
            string name;
            float size;
            FontStyle style;
            GraphicsUnit unit;

            name = (string)info.GetValue("Name", typeof(string));
            size = (float)info.GetValue("Size", typeof(float));
            style = (FontStyle)info.GetValue("Style", typeof(FontStyle));
            unit = (GraphicsUnit)info.GetValue("Unit", typeof(GraphicsUnit));

            CreateFont(name, size, style, unit, DefaultCharSet, false);
        }

        void ISerializable.GetObjectData(SerializationInfo si, StreamingContext context)
        {
            si.AddValue("Name", Name);
            si.AddValue("Size", Size);
            si.AddValue("Style", Style);
            si.AddValue("Unit", Unit);
        }

        ~Font()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (fontObject != IntPtr.Zero)
            {
                int status = SafeNativeMethods.Gdip.GdipDeleteFont(fontObject);
                fontObject = IntPtr.Zero;
                GC.SuppressFinalize(this);
                // check the status code (throw) at the last step
                SafeNativeMethods.Gdip.CheckStatus(status);
            }
        }

        internal void SetSystemFontName(string newSystemFontName)
        {
            systemFontName = newSystemFontName;
        }

        internal void unitConversion(GraphicsUnit fromUnit, GraphicsUnit toUnit, float nSrc, out float nTrg)
        {
            float inchs = 0;
            nTrg = 0;

            switch (fromUnit)
            {
                case GraphicsUnit.Display:
                    inchs = nSrc / 75f;
                    break;
                case GraphicsUnit.Document:
                    inchs = nSrc / 300f;
                    break;
                case GraphicsUnit.Inch:
                    inchs = nSrc;
                    break;
                case GraphicsUnit.Millimeter:
                    inchs = nSrc / 25.4f;
                    break;
                case GraphicsUnit.Pixel:
                case GraphicsUnit.World:
                    inchs = nSrc / Graphics.systemDpiX;
                    break;
                case GraphicsUnit.Point:
                    inchs = nSrc / 72f;
                    break;
                default:
                    throw new ArgumentException("Invalid GraphicsUnit");
            }

            switch (toUnit)
            {
                case GraphicsUnit.Display:
                    nTrg = inchs * 75;
                    break;
                case GraphicsUnit.Document:
                    nTrg = inchs * 300;
                    break;
                case GraphicsUnit.Inch:
                    nTrg = inchs;
                    break;
                case GraphicsUnit.Millimeter:
                    nTrg = inchs * 25.4f;
                    break;
                case GraphicsUnit.Pixel:
                case GraphicsUnit.World:
                    nTrg = inchs * Graphics.systemDpiX;
                    break;
                case GraphicsUnit.Point:
                    nTrg = inchs * 72;
                    break;
                default:
                    throw new ArgumentException("Invalid GraphicsUnit");
            }
        }

        void setProperties(FontFamily family, float emSize, FontStyle style, GraphicsUnit unit, byte charSet, bool isVertical)
        {
            _name = family.Name;
            _fontFamily = family;
            _size = emSize;

            // MS throws ArgumentException, if unit is set to GraphicsUnit.Display
            _unit = unit;
            _style = style;
            _gdiCharSet = charSet;
            _gdiVerticalFont = isVertical;

            unitConversion(unit, GraphicsUnit.Point, emSize, out _sizeInPoints);

            _bold = _italic = _strikeout = _underline = false;

            if ((style & FontStyle.Bold) == FontStyle.Bold)
                _bold = true;

            if ((style & FontStyle.Italic) == FontStyle.Italic)
                _italic = true;

            if ((style & FontStyle.Strikeout) == FontStyle.Strikeout)
                _strikeout = true;

            if ((style & FontStyle.Underline) == FontStyle.Underline)
                _underline = true;
        }

        public static Font FromHfont(IntPtr hfont)
        {
            IntPtr newObject;
            FontStyle newStyle = FontStyle.Regular;
            float newSize;
            SafeNativeMethods.LOGFONT lf = new SafeNativeMethods.LOGFONT();

            // Sanity. Should we throw an exception?
            if (hfont == IntPtr.Zero)
            {
                Font result = new Font("Arial", (float)10.0, FontStyle.Regular);
                return (result);
            }

            // If we're on Unix we use our private gdiplus API to avoid Wine 
            // dependencies in S.D
            int s = SafeNativeMethods.Gdip.GdipCreateFontFromHfont(hfont, out newObject, ref lf);
            SafeNativeMethods.Gdip.CheckStatus(s);

            if (lf.lfItalic != 0)
            {
                newStyle |= FontStyle.Italic;
            }

            if (lf.lfUnderline != 0)
            {
                newStyle |= FontStyle.Underline;
            }

            if (lf.lfStrikeOut != 0)
            {
                newStyle |= FontStyle.Strikeout;
            }

            if (lf.lfWeight > 400)
            {
                newStyle |= FontStyle.Bold;
            }

            if (lf.lfHeight < 0)
            {
                newSize = lf.lfHeight * -1;
            }
            else
            {
                newSize = lf.lfHeight;
            }

            return (new Font(newObject, lf.lfFaceName, newStyle, newSize));
        }

        public IntPtr ToHfont()
        {
            if (fontObject == IntPtr.Zero)
                throw new ArgumentException("Object has been disposed.");

            return fontObject;
        }

        internal Font(IntPtr newFontObject, string familyName, FontStyle style, float size)
        {
            FontFamily fontFamily;

            try
            {
                fontFamily = new FontFamily(familyName);
            }
            catch (Exception)
            {
                fontFamily = FontFamily.GenericSansSerif;
            }

            setProperties(fontFamily, size, style, GraphicsUnit.Pixel, 0, false);
            fontObject = newFontObject;
        }

        public Font(Font prototype, FontStyle newStyle)
        {
            // no null checks, MS throws a NullReferenceException if original is null
            setProperties(prototype.FontFamily, prototype.Size, newStyle, prototype.Unit, prototype.GdiCharSet, prototype.GdiVerticalFont);

            int status = SafeNativeMethods.Gdip.GdipCreateFont(_fontFamily.NativeFamily, Size, Style, Unit, out fontObject);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public Font(FontFamily family, float emSize, GraphicsUnit unit)
            : this(family, emSize, FontStyle.Regular, unit, DefaultCharSet, false)
        {
        }

        public Font(string familyName, float emSize, GraphicsUnit unit)
            : this(new FontFamily(familyName), emSize, FontStyle.Regular, unit, DefaultCharSet, false)
        {
        }

        public Font(FontFamily family, float emSize)
            : this(family, emSize, FontStyle.Regular, GraphicsUnit.Point, DefaultCharSet, false)
        {
        }

        public Font(FontFamily family, float emSize, FontStyle style)
            : this(family, emSize, style, GraphicsUnit.Point, DefaultCharSet, false)
        {
        }

        public Font(FontFamily family, float emSize, FontStyle style, GraphicsUnit unit)
            : this(family, emSize, style, unit, DefaultCharSet, false)
        {
        }

        public Font(FontFamily family, float emSize, FontStyle style, GraphicsUnit unit, byte gdiCharSet)
            : this(family, emSize, style, unit, gdiCharSet, false)
        {
        }

        public Font(FontFamily family, float emSize, FontStyle style,
                GraphicsUnit unit, byte gdiCharSet, bool gdiVerticalFont)
        {
            if (family == null)
                throw new ArgumentNullException("family");

            int status;
            setProperties(family, emSize, style, unit, gdiCharSet, gdiVerticalFont);
            status = SafeNativeMethods.Gdip.GdipCreateFont(family.NativeFamily, emSize, style, unit, out fontObject);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public Font(string familyName, float emSize)
            : this(familyName, emSize, FontStyle.Regular, GraphicsUnit.Point, DefaultCharSet, false)
        {
        }

        public Font(string familyName, float emSize, FontStyle style)
            : this(familyName, emSize, style, GraphicsUnit.Point, DefaultCharSet, false)
        {
        }

        public Font(string familyName, float emSize, FontStyle style, GraphicsUnit unit)
            : this(familyName, emSize, style, unit, DefaultCharSet, false)
        {
        }

        public Font(string familyName, float emSize, FontStyle style, GraphicsUnit unit, byte gdiCharSet)
            : this(familyName, emSize, style, unit, gdiCharSet, false)
        {
        }

        public Font(string familyName, float emSize, FontStyle style,
                GraphicsUnit unit, byte gdiCharSet, bool gdiVerticalFont)
        {
            CreateFont(familyName, emSize, style, unit, gdiCharSet, gdiVerticalFont);
        }
        internal Font(string familyName, float emSize, string systemName)
            : this(familyName, emSize, FontStyle.Regular, GraphicsUnit.Point, DefaultCharSet, false)
        {
            systemFontName = systemName;
        }
        public object Clone()
        {
            return new Font(this, Style);
        }

        internal IntPtr NativeObject
        {
            get
            {
                return fontObject;
            }
        }

        private bool _bold;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool Bold
        {
            get
            {
                return _bold;
            }
        }

        private FontFamily _fontFamily;

        [Browsable(false)]
        public FontFamily FontFamily
        {
            get
            {
                return _fontFamily;
            }
        }

        private byte _gdiCharSet;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public byte GdiCharSet
        {
            get
            {
                return _gdiCharSet;
            }
        }

        private bool _gdiVerticalFont;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool GdiVerticalFont
        {
            get
            {
                return _gdiVerticalFont;
            }
        }

        [Browsable(false)]
        public int Height
        {
            get
            {
                return (int)Math.Ceiling(GetHeight());
            }
        }

        [Browsable(false)]
        public bool IsSystemFont
        {
            get
            {
                return !string.IsNullOrEmpty(systemFontName);
            }
        }

        private bool _italic;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool Italic
        {
            get
            {
                return _italic;
            }
        }

        private string _name;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
#if !NETCORE
        [Editor ("System.Drawing.Design.FontNameEditor, " + Consts.AssemblySystem_Drawing_Design, typeof (System.Drawing.Design.UITypeEditor))]
        [TypeConverter (typeof (FontConverter.FontNameConverter))]
#endif
        public string Name
        {
            get
            {
                return _name;
            }
        }

        public float Size
        {
            get
            {
                return _size;
            }
        }

        private float _sizeInPoints;

        [Browsable(false)]
        public float SizeInPoints
        {
            get
            {
                return _sizeInPoints;
            }
        }

        private bool _strikeout;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool Strikeout
        {
            get
            {
                return _strikeout;
            }
        }

        private FontStyle _style;

        [Browsable(false)]
        public FontStyle Style
        {
            get
            {
                return _style;
            }
        }

        [Browsable(false)]
        public string SystemFontName
        {
            get
            {
                return systemFontName;
            }
        }

        [Browsable(false)]
        public string OriginalFontName
        {
            get
            {
                return originalFontName;
            }
        }
        private bool _underline;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool Underline
        {
            get
            {
                return _underline;
            }
        }

        private GraphicsUnit _unit;

#if !NETCORE
        [TypeConverter (typeof (FontConverter.FontUnitConverter))]
#endif
        public GraphicsUnit Unit
        {
            get
            {
                return _unit;
            }
        }

        public override bool Equals(object obj)
        {
            Font fnt = (obj as Font);
            if (fnt == null)
                return false;

            if (fnt.FontFamily.Equals(FontFamily) && fnt.Size == Size &&
                fnt.Style == Style && fnt.Unit == Unit &&
                fnt.GdiCharSet == GdiCharSet &&
                fnt.GdiVerticalFont == GdiVerticalFont)
                return true;
            else
                return false;
        }

        private int _hashCode;

        public override int GetHashCode()
        {
            if (_hashCode == 0)
            {
                _hashCode = 17;
                unchecked
                {
                    _hashCode = _hashCode * 23 + _name.GetHashCode();
                    _hashCode = _hashCode * 23 + FontFamily.GetHashCode();
                    _hashCode = _hashCode * 23 + _size.GetHashCode();
                    _hashCode = _hashCode * 23 + _unit.GetHashCode();
                    _hashCode = _hashCode * 23 + _style.GetHashCode();
                    _hashCode = _hashCode * 23 + _gdiCharSet;
                    _hashCode = _hashCode * 23 + _gdiVerticalFont.GetHashCode();
                }
            }

            return _hashCode;
        }

        [MonoTODO("The hdc parameter has no direct equivalent in libgdiplus.")]
        public static Font FromHdc(IntPtr hdc)
        {
            throw new NotImplementedException();
        }

        [MonoTODO("The returned font may not have all it's properties initialized correctly.")]
        public static Font FromLogFont(object lf, IntPtr hdc)
        {
            IntPtr newObject;
            SafeNativeMethods.LOGFONT o = (SafeNativeMethods.LOGFONT)lf;
            int status = SafeNativeMethods.Gdip.GdipCreateFontFromLogfont(hdc, ref o, out newObject);
            SafeNativeMethods.Gdip.CheckStatus(status);
            return new Font(newObject, "Microsoft Sans Serif", FontStyle.Regular, 10);
        }

        public float GetHeight()
        {
            return GetHeight(Graphics.systemDpiY);
        }

        public static Font FromLogFont(object lf)
        {
            return FromLogFont(lf, IntPtr.Zero);
        }

        public void ToLogFont(object logFont)
        {
            // Unix - We don't have a window we could associate the DC with
            // so we use an image instead
            using (Bitmap img = new Bitmap(1, 1, Imaging.PixelFormat.Format32bppArgb))
            {
                using (Graphics g = Graphics.FromImage(img))
                {
                    ToLogFont(logFont, g);
                }
            }
        }

        public void ToLogFont(object logFont, Graphics graphics)
        {
            if (graphics == null)
                throw new ArgumentNullException("graphics");

            if (logFont == null)
            {
                throw new AccessViolationException("logFont");
            }

            Type st = logFont.GetType();
            if (!st.GetTypeInfo().IsLayoutSequential)
                throw new ArgumentException("logFont", "Layout must be sequential.");

            // note: there is no exception if 'logFont' isn't big enough
            Type lf = typeof(LOGFONT);
            int size = Marshal.SizeOf(logFont);
            if (size >= Marshal.SizeOf(lf))
            {
                int status;
                IntPtr copy = Marshal.AllocHGlobal(size);
                try
                {
                    Marshal.StructureToPtr(logFont, copy, false);

                    status = SafeNativeMethods.Gdip.GdipGetLogFont(NativeObject, graphics.NativeObject, logFont);
                    if (status != SafeNativeMethods.Gdip.Ok)
                    {
                        // reset to original values
                        Marshal.PtrToStructure(copy, logFont);
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(copy);
                }

                if (CharSetOffset == -1)
                {
                    // not sure why this methods returns an IntPtr since it's an offset
                    // anyway there's no issue in downcasting the result into an int32
                    CharSetOffset = (int)Marshal.OffsetOf(lf, "lfCharSet");
                }

                // note: Marshal.WriteByte(object,*) methods are unimplemented on Mono
                GCHandle gch = GCHandle.Alloc(logFont, GCHandleType.Pinned);
                try
                {
                    IntPtr ptr = gch.AddrOfPinnedObject();
                    // if GDI+ lfCharSet is 0, then we return (S.D.) 1, otherwise the value is unchanged
                    if (Marshal.ReadByte(ptr, CharSetOffset) == 0)
                    {
                        // set lfCharSet to 1 
                        Marshal.WriteByte(ptr, CharSetOffset, 1);
                    }
                }
                finally
                {
                    gch.Free();
                }

                // now we can throw, if required
                SafeNativeMethods.Gdip.CheckStatus(status);
            }
        }

        public float GetHeight(Graphics graphics)
        {
            if (graphics == null)
                throw new ArgumentNullException("graphics");

            float size;
            int status = SafeNativeMethods.Gdip.GdipGetFontHeight(fontObject, graphics.NativeObject, out size);
            SafeNativeMethods.Gdip.CheckStatus(status);
            return size;
        }

        public float GetHeight(float dpi)
        {
            float size;
            int status = SafeNativeMethods.Gdip.GdipGetFontHeightGivenDPI(fontObject, dpi, out size);
            SafeNativeMethods.Gdip.CheckStatus(status);
            return size;
        }

        public override String ToString()
        {
            return String.Format("[Font: Name={0}, Size={1}, Units={2}, GdiCharSet={3}, GdiVerticalFont={4}]", _name, Size, (int)_unit, _gdiCharSet, _gdiVerticalFont);
        }
    }
}
