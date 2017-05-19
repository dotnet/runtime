// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing
{
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Drawing.Internal;
    using System.Globalization;
    using System.Runtime.InteropServices;
    using System.Runtime.Serialization;
    using System.Security.Permissions;

    /*
    * Represent a font object
    */

    /// <include file='doc\Font.uex' path='docs/doc[@for="Font"]/*' />
    /// <devdoc>
    ///    Defines a particular format for text,
    ///    including font face, size, and style attributes.
    /// </devdoc>
    [ComVisible(true)]
    public sealed class Font : MarshalByRefObject, ICloneable, ISerializable, IDisposable
    {
        private const int LogFontCharSetOffset = 23;
        private const int LogFontNameOffset = 28;

        private IntPtr _nativeFont;
        private float _fontSize;
        private FontStyle _fontStyle;
        private FontFamily _fontFamily;
        private GraphicsUnit _fontUnit;
        private byte _gdiCharSet = SafeNativeMethods.DEFAULT_CHARSET;
        private bool _gdiVerticalFont;
        private string _systemFontName = "";
        private string _originalFontName;

        ///<devdoc>
        ///     Creates the GDI+ native font object.
        ///</devdoc>
        private void CreateNativeFont()
        {
            Debug.Assert(_nativeFont == IntPtr.Zero, "nativeFont already initialized, this will generate a handle leak.");
            Debug.Assert(_fontFamily != null, "fontFamily not initialized.");

            // Note: GDI+ creates singleton font family objects (from the corresponding font file) and reference count them so
            // if creating the font object from an external FontFamily, this object's FontFamily will share the same native object.
            int status = SafeNativeMethods.Gdip.GdipCreateFont(
                                    new HandleRef(this, _fontFamily.NativeFamily),
                                    _fontSize,
                                    _fontStyle,
                                    _fontUnit,
                                    out _nativeFont);

            // Special case this common error message to give more information
            if (status == SafeNativeMethods.Gdip.FontStyleNotFound)
            {
                throw new ArgumentException(SR.Format(SR.GdiplusFontStyleNotFound, _fontFamily.Name, _fontStyle.ToString()));
            }
            else if (status != SafeNativeMethods.Gdip.Ok)
            {
                throw SafeNativeMethods.Gdip.StatusException(status);
            }
        }

        /// <include file='doc\Font.uex' path='docs/doc[@for="Font.ISerializable.GetObjectData"]/*' />
        /// <devdoc>
        ///     ISerializable private implementation
        /// </devdoc>
        /// <internalonly/>
        [SecurityPermissionAttribute(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter)]
        void ISerializable.GetObjectData(SerializationInfo si, StreamingContext context)
        {
            // Serialize the original Font name rather than the fallback font name if we have one
            si.AddValue("Name", String.IsNullOrEmpty(OriginalFontName) ? Name : OriginalFontName);
            si.AddValue("Size", Size);
            si.AddValue("Style", Style);
            si.AddValue("Unit", Unit);
        }

        /// <include file='doc\Font.uex' path='docs/doc[@for="Font.Font"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.Drawing.Font'/> class from
        ///       the specified existing <see cref='System.Drawing.Font'/> and <see cref='System.Drawing.FontStyle'/>.
        ///    </para>
        /// </devdoc>
        public Font(Font prototype, FontStyle newStyle)
        {
            // Copy over the originalFontName because it won't get initialized
            _originalFontName = prototype.OriginalFontName;
            Initialize(prototype.FontFamily, prototype.Size, newStyle, prototype.Unit, SafeNativeMethods.DEFAULT_CHARSET, false);
        }

        /// <include file='doc\Font.uex' path='docs/doc[@for="Font.Font1"]/*' />
        /// <devdoc>
        ///    Initializes a new instance of the <see cref='System.Drawing.Font'/> class with
        ///    the specified attributes.
        /// </devdoc>
        public Font(FontFamily family, float emSize, FontStyle style, GraphicsUnit unit)
        {
            Initialize(family, emSize, style, unit, SafeNativeMethods.DEFAULT_CHARSET, false);
        }

        /// <include file='doc\Font.uex' path='docs/doc[@for="Font.Font9"]/*' />
        /// <devdoc>
        ///    Initializes a new instance of the <see cref='System.Drawing.Font'/> class with
        ///    the specified attributes.
        /// </devdoc>
        public Font(FontFamily family, float emSize, FontStyle style, GraphicsUnit unit, byte gdiCharSet)
        {
            Initialize(family, emSize, style, unit, gdiCharSet, false);
        }

        /// <include file='doc\Font.uex' path='docs/doc[@for="Font.Font11"]/*' />
        /// <devdoc>
        ///    Initializes a new instance of the <see cref='System.Drawing.Font'/> class with
        ///    the specified attributes.
        /// </devdoc>
        public Font(FontFamily family, float emSize, FontStyle style, GraphicsUnit unit, byte gdiCharSet, bool gdiVerticalFont)
        {
            Initialize(family, emSize, style, unit, gdiCharSet, gdiVerticalFont);
        }

        /// <include file='doc\Font.uex' path='docs/doc[@for="Font.Font10"]/*' />
        /// <devdoc>
        ///    Initializes a new instance of the <see cref='System.Drawing.Font'/> class with
        ///    the specified attributes.
        /// </devdoc>
        public Font(string familyName, float emSize, FontStyle style, GraphicsUnit unit, byte gdiCharSet)
        {
            Initialize(familyName, emSize, style, unit, gdiCharSet, IsVerticalName(familyName));
        }


        /// <include file='doc\Font.uex' path='docs/doc[@for="Font.Font12"]/*' />
        /// <devdoc>
        ///    Initializes a new instance of the <see cref='System.Drawing.Font'/> class with
        ///    the specified attributes.
        /// </devdoc>
        public Font(string familyName, float emSize, FontStyle style, GraphicsUnit unit, byte gdiCharSet, bool gdiVerticalFont)
        {
            if (float.IsNaN(emSize) || float.IsInfinity(emSize) || emSize <= 0)
            {
                throw new ArgumentException(SR.Format(SR.InvalidBoundArgument, "emSize", emSize, 0, "System.Single.MaxValue"), "emSize");
            }

            Initialize(familyName, emSize, style, unit, gdiCharSet, gdiVerticalFont);
        }

        /// <include file='doc\Font.uex' path='docs/doc[@for="Font.Font2"]/*' />
        /// <devdoc>
        ///    Initializes a new instance of the <see cref='System.Drawing.Font'/> class with
        ///    the specified attributes.
        /// </devdoc>
        public Font(FontFamily family, float emSize, FontStyle style)
        {
            Initialize(family, emSize, style, GraphicsUnit.Point, SafeNativeMethods.DEFAULT_CHARSET, false);
        }

        /// <include file='doc\Font.uex' path='docs/doc[@for="Font.Font3"]/*' />
        /// <devdoc>
        ///    Initializes a new instance of the <see cref='System.Drawing.Font'/> class with
        ///    the specified attributes.
        /// </devdoc>
        public Font(FontFamily family, float emSize, GraphicsUnit unit)
        {
            Initialize(family, emSize, FontStyle.Regular, unit, SafeNativeMethods.DEFAULT_CHARSET, false);
        }

        /// <include file='doc\Font.uex' path='docs/doc[@for="Font.Font4"]/*' />
        /// <devdoc>
        ///    Initializes a new instance of the <see cref='System.Drawing.Font'/> class with
        ///    the specified attributes.
        /// </devdoc>
        public Font(FontFamily family, float emSize)
        {
            Initialize(family, emSize, FontStyle.Regular, GraphicsUnit.Point, SafeNativeMethods.DEFAULT_CHARSET, false);
        }

        /// <include file='doc\Font.uex' path='docs/doc[@for="Font.Font5"]/*' />
        /// <devdoc>
        ///    Initializes a new instance of the <see cref='System.Drawing.Font'/> class with
        ///    the specified attributes.
        /// </devdoc>
        public Font(string familyName, float emSize, FontStyle style, GraphicsUnit unit)
        {
            Initialize(familyName, emSize, style, unit, SafeNativeMethods.DEFAULT_CHARSET, IsVerticalName(familyName));
        }

        /// <include file='doc\Font.uex' path='docs/doc[@for="Font.Font6"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.Drawing.Font'/> class with
        ///       the specified
        ///       attributes.
        ///    </para>
        /// </devdoc>
        public Font(string familyName, float emSize, FontStyle style)
        {
            Initialize(familyName, emSize, style, GraphicsUnit.Point, SafeNativeMethods.DEFAULT_CHARSET, IsVerticalName(familyName));
        }

        /// <include file='doc\Font.uex' path='docs/doc[@for="Font.Font7"]/*' />
        /// <devdoc>
        ///    Initializes a new instance of the <see cref='System.Drawing.Font'/> class with
        ///    the specified attributes.
        /// </devdoc>
        public Font(string familyName, float emSize, GraphicsUnit unit)
        {
            Initialize(familyName, emSize, FontStyle.Regular, unit, SafeNativeMethods.DEFAULT_CHARSET, IsVerticalName(familyName));
        }

        /// <include file='doc\Font.uex' path='docs/doc[@for="Font.Font8"]/*' />
        /// <devdoc>
        ///    Initializes a new instance of the <see cref='System.Drawing.Font'/> class with
        ///    the specified attributes.
        /// </devdoc>
        public Font(string familyName, float emSize)
        {
            Initialize(familyName, emSize, FontStyle.Regular, GraphicsUnit.Point, SafeNativeMethods.DEFAULT_CHARSET, IsVerticalName(familyName));
        }

        /// <devdoc>
        ///     Constructor to initialize fields from an exisiting native GDI+ object reference.
        ///     Used by ToLogFont.
        /// </devdoc>
        private Font(IntPtr nativeFont, byte gdiCharSet, bool gdiVerticalFont)
        {
            Debug.Assert(_nativeFont == IntPtr.Zero, "GDI+ native font already initialized, this will generate a handle leak");
            Debug.Assert(nativeFont != IntPtr.Zero, "nativeFont is null");

            int status = 0;
            float size = 0;
            GraphicsUnit unit = GraphicsUnit.Point;
            FontStyle style = FontStyle.Regular;
            IntPtr nativeFamily = IntPtr.Zero;

            _nativeFont = nativeFont;

            status = SafeNativeMethods.Gdip.GdipGetFontUnit(new HandleRef(this, nativeFont), out unit);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            status = SafeNativeMethods.Gdip.GdipGetFontSize(new HandleRef(this, nativeFont), out size);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            status = SafeNativeMethods.Gdip.GdipGetFontStyle(new HandleRef(this, nativeFont), out style);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            status = SafeNativeMethods.Gdip.GdipGetFamily(new HandleRef(this, nativeFont), out nativeFamily);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            SetFontFamily(new FontFamily(nativeFamily));

            Initialize(_fontFamily, size, style, unit, gdiCharSet, gdiVerticalFont);
        }

        /// <devdoc>
        ///     Initializes this object's fields.
        /// </devdoc>
        private void Initialize(string familyName, float emSize, FontStyle style, GraphicsUnit unit, byte gdiCharSet, bool gdiVerticalFont)
        {
            _originalFontName = familyName;

            SetFontFamily(new FontFamily(StripVerticalName(familyName), true /* createDefaultOnFail */ ));
            Initialize(_fontFamily, emSize, style, unit, gdiCharSet, gdiVerticalFont);
        }

        /// <devdoc>
        ///     Initializes this object's fields.
        /// </devdoc>
        private void Initialize(FontFamily family, float emSize, FontStyle style, GraphicsUnit unit, byte gdiCharSet, bool gdiVerticalFont)
        {
            if (family == null)
            {
                throw new ArgumentNullException("family");
            }

            if (float.IsNaN(emSize) || float.IsInfinity(emSize) || emSize <= 0)
            {
                throw new ArgumentException(SR.Format(SR.InvalidBoundArgument, "emSize", emSize, 0, "System.Single.MaxValue"), "emSize");
            }

            int status;

            _fontSize = emSize;
            _fontStyle = style;
            _fontUnit = unit;
            _gdiCharSet = gdiCharSet;
            _gdiVerticalFont = gdiVerticalFont;

            if (_fontFamily == null)
            {
                // GDI+ FontFamily is a singleton object.
                SetFontFamily(new FontFamily(family.NativeFamily));
            }

            if (_nativeFont == IntPtr.Zero)
            {
                CreateNativeFont();
            }

            // Get actual size.
            status = SafeNativeMethods.Gdip.GdipGetFontSize(new HandleRef(this, _nativeFont), out _fontSize);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);
        }

        /// <include file='doc\Font.uex' path='docs/doc[@for="Font.FromHfont"]/*' />
        /// <devdoc>
        ///    Creates a <see cref='System.Drawing.Font'/> from the specified Windows
        ///    handle.
        /// </devdoc>
        public static Font FromHfont(IntPtr hfont)
        {
            SafeNativeMethods.LOGFONT lf = new SafeNativeMethods.LOGFONT();
            SafeNativeMethods.GetObject(new HandleRef(null, hfont), lf);

            Font result;
            IntPtr screenDC = UnsafeNativeMethods.GetDC(NativeMethods.NullHandleRef);
            try
            {
                result = Font.FromLogFont(lf, screenDC);
            }
            finally
            {
                UnsafeNativeMethods.ReleaseDC(NativeMethods.NullHandleRef, new HandleRef(null, screenDC));
            }

            return result;
        }


        /// <include file='doc\Font.uex' path='docs/doc[@for="Font.FromLogFont"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Font FromLogFont(object lf)
        {
            IntPtr screenDC = UnsafeNativeMethods.GetDC(NativeMethods.NullHandleRef);
            Font result;
            try
            {
                result = Font.FromLogFont(lf, screenDC);
            }
            finally
            {
                UnsafeNativeMethods.ReleaseDC(NativeMethods.NullHandleRef, new HandleRef(null, screenDC));
            }
            return result;
        }

        /// <include file='doc\Font.uex' path='docs/doc[@for="Font.FromLogFont1"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Font FromLogFont(object lf, IntPtr hdc)
        {
            IntPtr font = IntPtr.Zero;
            int status;

            if (Marshal.SystemDefaultCharSize == 1)
                status = SafeNativeMethods.Gdip.GdipCreateFontFromLogfontA(new HandleRef(null, hdc), lf, out font);
            else
                status = SafeNativeMethods.Gdip.GdipCreateFontFromLogfontW(new HandleRef(null, hdc), lf, out font);

            // Special case this incredibly common error message to give more information
            if (status == SafeNativeMethods.Gdip.NotTrueTypeFont)
                throw new ArgumentException(SR.Format(SR.GdiplusNotTrueTypeFont_NoName));
            else if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            // GDI+ returns font = 0 even though the status is Ok.
            if (font == IntPtr.Zero)
                throw new ArgumentException(SR.Format(SR.GdiplusNotTrueTypeFont, lf.ToString()));

            bool gdiVerticalFont;
            if (Marshal.SystemDefaultCharSize == 1)
            {
#pragma warning disable 0618
                gdiVerticalFont = (Marshal.ReadByte(lf, LogFontNameOffset) == (byte)(short)'@');
#pragma warning restore 0618
            }
            else
            {
#pragma warning disable 0618
                gdiVerticalFont = (Marshal.ReadInt16(lf, LogFontNameOffset) == (short)'@');
#pragma warning restore 0618
            }
#pragma warning disable 0618
            return new Font(font, Marshal.ReadByte(lf, LogFontCharSetOffset), gdiVerticalFont);
#pragma warning restore 0618
        }

        /// <include file='doc\Font.uex' path='docs/doc[@for="Font.FromHdc"]/*' />
        /// <devdoc>
        ///    Creates a Font from the specified Windows
        ///    handle to a device context.
        /// </devdoc>
        public static Font FromHdc(IntPtr hdc)
        {
            IntPtr font = IntPtr.Zero;

            int status = SafeNativeMethods.Gdip.GdipCreateFontFromDC(new HandleRef(null, hdc), ref font);

            // Special case this incredibly common error message to give more information
            if (status == SafeNativeMethods.Gdip.NotTrueTypeFont)
                throw new ArgumentException(SR.Format(SR.GdiplusNotTrueTypeFont_NoName));
            else if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            return new Font(font, 0, false);
        }


        /// <include file='doc\Font.uex' path='docs/doc[@for="Font.Clone"]/*' />
        /// <devdoc>
        ///    Creates an exact copy of this <see cref='System.Drawing.Font'/>.
        /// </devdoc>
        public object Clone()
        {
            IntPtr cloneFont = IntPtr.Zero;

            int status = SafeNativeMethods.Gdip.GdipCloneFont(new HandleRef(this, _nativeFont), out cloneFont);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            Font newCloneFont = new Font(cloneFont, _gdiCharSet, _gdiVerticalFont);

            return newCloneFont;
        }


        /// <devdoc>
        ///     Get native GDI+ object pointer.
        ///     This property triggers the creation of the GDI+ native object if not initialized yet.
        /// </devdoc>
        internal IntPtr NativeFont
        {
            get
            {
                Debug.Assert(_nativeFont != IntPtr.Zero, "this.nativeFont == IntPtr.Zero.");
                return _nativeFont;
            }
        }

        /// <include file='doc\Font.uex' path='docs/doc[@for="Font.FontFamily"]/*' />
        /// <devdoc>
        ///    Gets the <see cref='System.Drawing.FontFamily'/> of this <see cref='System.Drawing.Font'/>.
        /// </devdoc>
        [Browsable(false)]
        public FontFamily FontFamily
        {
            get
            {
                Debug.Assert(_fontFamily != null, "fontFamily should never be null");
                return _fontFamily;
            }
        }

        private void SetFontFamily(FontFamily family)
        {
            _fontFamily = family;

            // GDI+ creates ref-counted singleton FontFamily objects based on the family name so all managed 
            // objects with same family name share the underlying GDI+ native pointer.  The unmanged object is
            // destroyed when its ref-count gets to zero.
            // Make sure this.fontFamily is not finalized so the underlying singleton object is kept alive.
            GC.SuppressFinalize(_fontFamily);
        }

        /// <include file='doc\Font.uex' path='docs/doc[@for="Font.Finalize"]/*' />
        /// <devdoc>
        ///    Cleans up Windows resources for this <see cref='System.Drawing.Font'/>.
        /// </devdoc>
        ~Font()
        {
            Dispose(false);
        }

        /// <include file='doc\Font.uex' path='docs/doc[@for="Font.Dispose"]/*' />
        /// <devdoc>
        ///    Cleans up Windows resources for this <see cref='System.Drawing.Font'/>.
        /// </devdoc>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (_nativeFont != IntPtr.Zero)
            {
                try
                {
#if DEBUG
                    int status =
#endif
                    SafeNativeMethods.Gdip.GdipDeleteFont(new HandleRef(this, _nativeFont));
#if DEBUG
                    Debug.Assert(status == SafeNativeMethods.Gdip.Ok, "GDI+ returned an error status: " + status.ToString(CultureInfo.InvariantCulture));
#endif
                }
                catch (Exception ex)
                {
                    if (ClientUtils.IsCriticalException(ex))
                    {
                        throw;
                    }

                    Debug.Fail("Exception thrown during Dispose: " + ex.ToString());
                }
                finally
                {
                    _nativeFont = IntPtr.Zero;
                }
            }
        }

        private static bool IsVerticalName(string familyName)
        {
            return familyName != null && familyName.Length > 0 && familyName[0] == '@';
        }

        /// <include file='doc\Font.uex' path='docs/doc[@for="Font.Bold"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Gets a value indicating whether this <see cref='System.Drawing.Font'/> is bold.
        ///    </para>
        /// </devdoc>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool Bold
        {
            get
            {
                return (Style & FontStyle.Bold) != 0;
            }
        }

        /// <include file='doc\Font.uex' path='docs/doc[@for="Font.GdiCharSet"]/*' />
        /// <devdoc>
        ///     Returns the GDI char set for this instance of a font. This will only
        ///     be valid if this font was created from a classic GDI font definition,
        ///     like a LOGFONT or HFONT, or it was passed into the constructor.
        ///
        ///     This is here for compatability with native Win32 intrinsic controls
        ///     on non-Unicode platforms.
        /// </devdoc>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public byte GdiCharSet
        {
            get
            {
                return _gdiCharSet;
            }
        }

        /// <include file='doc\Font.uex' path='docs/doc[@for="Font.GdiVerticalFont"]/*' />
        /// <devdoc>
        ///     Determines if this font was created to represt a GDI vertical font.
        ///     his will only be valid if this font was created from a classic GDI
        ///     font definition, like a LOGFONT or HFONT, or it was passed into the 
        ///     constructor.
        ///
        ///     This is here for compatability with native Win32 intrinsic controls
        ///     on non-Unicode platforms.
        /// </devdoc>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool GdiVerticalFont
        {
            get
            {
                return _gdiVerticalFont;
            }
        }

        /// <include file='doc\Font.uex' path='docs/doc[@for="Font.Italic"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Gets a value indicating whether this <see cref='System.Drawing.Font'/> is Italic.
        ///    </para>
        /// </devdoc>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool Italic
        {
            get
            {
                return (Style & FontStyle.Italic) != 0;
            }
        }

        /// <include file='doc\Font.uex' path='docs/doc[@for="Font.Name"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Gets the face name of this <see cref='System.Drawing.Font'/> .
        ///    </para>
        /// </devdoc>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Name
        {
            get { return FontFamily.Name; }
        }

        /// <include file='doc\Font.uex' path='docs/doc[@for="Font.OriginalFontName"]/*' />
        /// <devdoc>
        ///    <para>
        ///       This property is required by the framework and not intended to be used directly.
        ///    </para>
        /// </devdoc>
        [Browsable(false)]
        public string OriginalFontName
        {
            get { return _originalFontName; }
        }

        /// <include file='doc\Font.uex' path='docs/doc[@for="Font.Strikeout"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Gets a value indicating whether this <see cref='System.Drawing.Font'/> is strikeout (has a line
        ///       through it).
        ///    </para>
        /// </devdoc>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool Strikeout
        {
            get
            {
                return (Style & FontStyle.Strikeout) != 0;
            }
        }

        /// <include file='doc\Font.uex' path='docs/doc[@for="Font.Underline"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Gets a value indicating whether this <see cref='System.Drawing.Font'/> is underlined.
        ///    </para>
        /// </devdoc>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool Underline
        {
            get
            {
                return (Style & FontStyle.Underline) != 0;
            }
        }

        /// <include file='doc\Font.uex' path='docs/doc[@for="Font.Equals"]/*' />
        /// <devdoc>
        ///    Returns a value indicating whether the
        ///    specified object is a <see cref='System.Drawing.Font'/> equivalent to this <see cref='System.Drawing.Font'/>.
        /// </devdoc>
        public override bool Equals(object obj)
        {
            if (obj == this)
            {
                return true;
            }

            Font font = obj as Font;

            if (font == null)
            {
                return false;
            }

            // Note: If this and/or the passed-in font are disposed, this method can still return true since we check for cached properties
            // here.

            // We need to call properties on the passed-in object since it could be a proxy in a remoting scenario and proxies don't
            // have access to private/internal fields.
            return font.FontFamily.Equals(FontFamily) &&
                font.GdiVerticalFont == GdiVerticalFont &&
                font.GdiCharSet == GdiCharSet &&
                font.Style == Style &&
                font.Size == Size &&
                font.Unit == Unit;
        }



        /// <include file='doc\Font.uex' path='docs/doc[@for="Font.GetHashCode"]/*' />
        /// <devdoc>
        ///    Gets the hash code for this <see cref='System.Drawing.Font'/>.
        /// </devdoc>
        public override int GetHashCode()
        {
            return unchecked((int)((((UInt32)_fontStyle << 13) | ((UInt32)_fontStyle >> 19)) ^
                         (((UInt32)_fontUnit << 26) | ((UInt32)_fontUnit >> 6)) ^
                         (((UInt32)_fontSize << 7) | ((UInt32)_fontSize >> 25))));
        }

        private static string StripVerticalName(string familyName)
        {
            if (familyName != null && familyName.Length > 1 && familyName[0] == '@')
            {
                return familyName.Substring(1);
            }
            return familyName;
        }

        /// <include file='doc\Font.uex' path='docs/doc[@for="Font.ToString"]/*' />
        /// <devdoc>
        ///    Returns a human-readable string
        ///    representation of this <see cref='System.Drawing.Font'/>.
        /// </devdoc>
        public override string ToString()
        {
            return string.Format(CultureInfo.CurrentCulture, "[{0}: Name={1}, Size={2}, Units={3}, GdiCharSet={4}, GdiVerticalFont={5}]",
                                    GetType().Name,
                                    FontFamily.Name,
                                    _fontSize,
                                    (int)_fontUnit,
                                    _gdiCharSet,
                                    _gdiVerticalFont);
        }



        // Operations

        /// <include file='doc\Font.uex' path='docs/doc[@for="Font.ToLogFont"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public void ToLogFont(object logFont)
        {
            IntPtr screenDC = UnsafeNativeMethods.GetDC(NativeMethods.NullHandleRef);
            try
            {
                Graphics graphics = Graphics.FromHdcInternal(screenDC);

                try
                {
                    ToLogFont(logFont, graphics);
                }
                finally
                {
                    graphics.Dispose();
                }
            }
            finally
            {
                UnsafeNativeMethods.ReleaseDC(NativeMethods.NullHandleRef, new HandleRef(null, screenDC));
            }
        }

        /// <include file='doc\Font.uex' path='docs/doc[@for="Font.ToLogFont1"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public unsafe void ToLogFont(object logFont, Graphics graphics)
        {
            if (graphics == null)
                throw new ArgumentNullException("graphics");

            int status;

            // handle proper marshalling of LogFontName as Unicode or ANSI
            if (Marshal.SystemDefaultCharSize == 1)
                status = SafeNativeMethods.Gdip.GdipGetLogFontA(new HandleRef(this, NativeFont), new HandleRef(graphics, graphics.NativeGraphics), logFont);
            else
                status = SafeNativeMethods.Gdip.GdipGetLogFontW(new HandleRef(this, NativeFont), new HandleRef(graphics, graphics.NativeGraphics), logFont);

            // append "@" to the begining of the string if we are 
            // a gdiVerticalFont.
            //
            if (_gdiVerticalFont)
            {
                if (Marshal.SystemDefaultCharSize == 1)
                {
                    // copy contents of name, over 1 byte
                    //
                    for (int i = 30; i >= 0; i--)
                    {
#pragma warning disable 0618
                        Marshal.WriteByte(logFont,
                                          LogFontNameOffset + i + 1,
                                          Marshal.ReadByte(logFont, LogFontNameOffset + i));
#pragma warning restore 0618
                    }

                    // write ANSI '@' sign at begining of name
                    //
#pragma warning disable 0618
                    Marshal.WriteByte(logFont, LogFontNameOffset, (byte)(int)'@');
#pragma warning restore 0618
                }
                else
                {
                    // copy contents of name, over 2 bytes (UNICODE)
                    //
                    for (int i = 60; i >= 0; i -= 2)
                    {
#pragma warning disable 0618
                        Marshal.WriteInt16(logFont,
                                           LogFontNameOffset + i + 2,
                                           Marshal.ReadInt16(logFont, LogFontNameOffset + i));
#pragma warning restore 0618
                    }

                    // write UNICODE '@' sign at begining of name
                    //
#pragma warning disable 0618
                    Marshal.WriteInt16(logFont, LogFontNameOffset, (short)'@');
#pragma warning restore 0618
                }
            }
#pragma warning disable 0618
            if (Marshal.ReadByte(logFont, LogFontCharSetOffset) == 0)
            {
                Marshal.WriteByte(logFont, LogFontCharSetOffset, _gdiCharSet);
            }
#pragma warning restore 0618

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);
        }

        /// <include file='doc\Font.uex' path='docs/doc[@for="Font.ToHfont"]/*' />
        /// <devdoc>
        ///    Returns a handle to this <see cref='System.Drawing.Font'/>.
        /// </devdoc>
        public IntPtr ToHfont()
        {
            SafeNativeMethods.LOGFONT lf = new SafeNativeMethods.LOGFONT();

            ToLogFont(lf);

            IntPtr handle = IntUnsafeNativeMethods.IntCreateFontIndirect(lf);

            if (handle == IntPtr.Zero)
            {
                throw new Win32Exception();
            }

            return handle;
        }

        /// <include file='doc\Font.uex' path='docs/doc[@for="Font.GetHeight"]/*' />
        /// <devdoc>
        ///    Returns the height of this Font in the
        ///    specified graphics context.
        /// </devdoc>
        public float GetHeight(Graphics graphics)
        {
            if (graphics == null)
                throw new ArgumentNullException("graphics");

            float ht;

            int status = SafeNativeMethods.Gdip.GdipGetFontHeight(new HandleRef(this, NativeFont), new HandleRef(graphics, graphics.NativeGraphics), out ht);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            return ht;
        }

        /// <include file='doc\Font.uex' path='docs/doc[@for="Font.GetHeight1"]/*' />
        /// <devdoc>
        /// </devdoc>
        public float GetHeight()
        {
            IntPtr screenDC = UnsafeNativeMethods.GetDC(NativeMethods.NullHandleRef);
            float height = 0.0f;
            try
            {
                using (Graphics graphics = Graphics.FromHdcInternal(screenDC))
                {
                    height = GetHeight(graphics);
                }
            }
            finally
            {
                UnsafeNativeMethods.ReleaseDC(NativeMethods.NullHandleRef, new HandleRef(null, screenDC));
            }

            return height;
        }


        /// <include file='doc\Font.uex' path='docs/doc[@for="Font.GetHeight2"]/*' />
        /// <devdoc>
        /// </devdoc>
        public float GetHeight(float dpi)
        {
            float ht;

            int status = SafeNativeMethods.Gdip.GdipGetFontHeightGivenDPI(new HandleRef(this, NativeFont), dpi, out ht);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            return ht;
        }


        /// <include file='doc\Font.uex' path='docs/doc[@for="Font.Style"]/*' />
        /// <devdoc>
        ///    Gets style information for this <see cref='System.Drawing.Font'/>.
        /// </devdoc>
        [
        Browsable(false)
        ]
        public FontStyle Style
        {
            get
            {
                return _fontStyle;
            }
        }

        // Return value is in Unit (the unit the font was created in)
        /// <include file='doc\Font.uex' path='docs/doc[@for="Font.Size"]/*' />
        /// <devdoc>
        ///    Gets the size of this <see cref='System.Drawing.Font'/>.
        /// </devdoc>
        public float Size
        {
            get
            {
                return _fontSize;
            }
        }

        /// <include file='doc\Font.uex' path='docs/doc[@for="Font.SizeInPoints"]/*' />
        /// <devdoc>
        ///    Gets the size, in points, of this <see cref='System.Drawing.Font'/>.
        /// </devdoc>
        [Browsable(false)]
        public float SizeInPoints
        {
            get
            {
                if (Unit == GraphicsUnit.Point)
                    return Size;
                else
                {
                    float emHeightInPoints;

                    IntPtr screenDC = UnsafeNativeMethods.GetDC(NativeMethods.NullHandleRef);

                    try
                    {
                        using (Graphics graphics = Graphics.FromHdcInternal(screenDC))
                        {
                            float pixelsPerPoint = (float)(graphics.DpiY / 72.0);
                            float lineSpacingInPixels = GetHeight(graphics);
                            float emHeightInPixels = lineSpacingInPixels * FontFamily.GetEmHeight(Style) / FontFamily.GetLineSpacing(Style);

                            emHeightInPoints = emHeightInPixels / pixelsPerPoint;
                        }
                    }
                    finally
                    {
                        UnsafeNativeMethods.ReleaseDC(NativeMethods.NullHandleRef, new HandleRef(null, screenDC));
                    }

                    return emHeightInPoints;
                }
            }
        }

        /// <include file='doc\Font.uex' path='docs/doc[@for="Font.Unit"]/*' />
        /// <devdoc>
        ///    Gets the unit of measure for this <see cref='System.Drawing.Font'/>.
        /// </devdoc>
        public GraphicsUnit Unit
        {
            get
            {
                return _fontUnit;
            }
        }

        /// <include file='doc\Font.uex' path='docs/doc[@for="Font.Height"]/*' />
        /// <devdoc>
        ///    Gets the height of this <see cref='System.Drawing.Font'/>.
        /// </devdoc>
        [
        Browsable(false)
        ]
        public int Height
        {
            get
            {
                return (int)Math.Ceiling(GetHeight());
            }
        }

        /// <include file='doc\Font.uex' path='docs/doc[@for="Font.IsSystemFont"]/*' />
        /// <devdoc>
        ///    Returns true if this <see cref='System.Drawing.Font'/> is a SystemFont.
        /// </devdoc>
        [
        Browsable(false)
        ]
        public bool IsSystemFont
        {
            get
            {
                return !String.IsNullOrEmpty(_systemFontName);
            }
        }

        /// <include file='doc\Font.uex' path='docs/doc[@for="Font.SystemFontName"]/*' />
        /// <devdoc>
        ///    Gets the name of this <see cref='System.Drawing.SystemFont'/>.
        /// </devdoc>
        [
        Browsable(false)
        ]
        public string SystemFontName
        {
            get
            {
                return _systemFontName;
            }
        }

        // This is used by SystemFonts when constructing a system Font objects.
        internal void SetSystemFontName(string systemFontName)
        {
            _systemFontName = systemFontName;
        }
    }
}

