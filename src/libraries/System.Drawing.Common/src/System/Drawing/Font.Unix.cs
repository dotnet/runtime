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
using Gdip = System.Drawing.SafeNativeMethods.Gdip;

namespace System.Drawing
{
#if !NETCORE
    [Editor ("System.Drawing.Design.FontEditor, " + Consts.AssemblySystem_Drawing_Design, typeof (System.Drawing.Design.UITypeEditor))]
    [TypeConverter (typeof (FontConverter))]
#endif
    public sealed partial class Font
    {
        private const byte DefaultCharSet = 1;
        private static int CharSetOffset = -1;

        private void CreateFont(string familyName, float emSize, FontStyle style, GraphicsUnit unit, byte charSet, bool isVertical)
        {
            _originalFontName = familyName;
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

            Initialize(family, emSize, style, unit, charSet, isVertical);
            int status = Gdip.GdipCreateFont(new HandleRef(this, family.NativeFamily), emSize, style, unit, out _nativeFont);

            if (status == Gdip.FontStyleNotFound)
                throw new ArgumentException(string.Format("Style {0} isn't supported by font {1}.", style.ToString(), familyName));

            Gdip.CheckStatus(status);
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

        private void Initialize(string familyName, float emSize, FontStyle style, GraphicsUnit unit, byte charSet, bool isVertical)
        {
            _originalFontName = familyName;
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

            Initialize(family, emSize, style, unit, charSet, isVertical);
        }

        private void Initialize(FontFamily family, float emSize, FontStyle style, GraphicsUnit unit, byte charSet, bool isVertical)
        {
            _fontFamily = family;
            _fontSize = emSize;

            // MS throws ArgumentException, if unit is set to GraphicsUnit.Display
            _fontUnit = unit;
            _fontStyle = style;
            _gdiCharSet = charSet;
            _gdiVerticalFont = isVertical;

            unitConversion(unit, GraphicsUnit.Point, emSize, out _fontSizeInPoints);
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
            int s = Gdip.GdipCreateFontFromHfont(hfont, out newObject, ref lf);
            Gdip.CheckStatus(s);

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
            if (_nativeFont == IntPtr.Zero)
                throw new ArgumentException("Object has been disposed.");

            return _nativeFont;
        }

        internal Font(IntPtr nativeFont, string familyName, FontStyle style, float size)
        {
            Initialize(familyName, size, style, GraphicsUnit.Pixel, 0, false);
            _nativeFont = nativeFont;
        }

        public Font(Font prototype, FontStyle newStyle)
        {
            // no null checks, MS throws a NullReferenceException if original is null
            Initialize(prototype.FontFamily, prototype.Size, newStyle, prototype.Unit, prototype.GdiCharSet, prototype.GdiVerticalFont);

            int status = Gdip.GdipCreateFont(new HandleRef(_fontFamily, _fontFamily.NativeFamily), Size, Style, Unit, out _nativeFont);
            Gdip.CheckStatus(status);
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
                throw new ArgumentNullException(nameof(family));

            int status;
            Initialize(family, emSize, style, unit, gdiCharSet, gdiVerticalFont);
            status = Gdip.GdipCreateFont(new HandleRef(this, family.NativeFamily), emSize, style, unit, out _nativeFont);
            Gdip.CheckStatus(status);
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
            _systemFontName = systemName;
        }

        public object Clone()
        {
            return new Font(this, Style);
        }

        private float _fontSizeInPoints;

        [Browsable(false)]
        public float SizeInPoints => _fontSizeInPoints;

        public static Font FromHdc(IntPtr hdc)
        {
            throw new NotImplementedException();
        }

        public static Font FromLogFont(object lf, IntPtr hdc)
        {
            IntPtr newObject;
            SafeNativeMethods.LOGFONT o = (SafeNativeMethods.LOGFONT)lf;
            int status = Gdip.GdipCreateFontFromLogfont(hdc, ref o, out newObject);
            Gdip.CheckStatus(status);
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
                throw new ArgumentNullException(nameof(graphics));

            if (logFont == null)
            {
                throw new AccessViolationException(nameof(logFont));
            }

            Type st = logFont.GetType();
            if (!st.GetTypeInfo().IsLayoutSequential)
                throw new ArgumentException(nameof(logFont), "Layout must be sequential.");

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

                    status = Gdip.GdipGetLogFontW(new HandleRef(this, NativeFont), new HandleRef(graphics, graphics.NativeGraphics), logFont);
                    if (status != Gdip.Ok)
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
                Gdip.CheckStatus(status);
            }
        }
    }
}
