// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Drawing.Text;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Drawing
{
    /// <summary>
    /// Abstracts a group of type faces having a similar basic design but having certain variation in styles.
    /// </summary>
    public sealed class FontFamily : MarshalByRefObject, IDisposable
    {
        private const int LANG_NEUTRAL = 0;
        private IntPtr _nativeFamily;
        private bool _createDefaultOnFail;

#if DEBUG
        private static object s_lockObj = new object();
        private static int s_idCount = 0;
        private int _id;
#endif

        /// <summary>
        /// Sets the GDI+ native family.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2106:SecureAsserts")]
        private void SetNativeFamily(IntPtr family)
        {
            Debug.Assert(_nativeFamily == IntPtr.Zero, "Setting GDI+ native font family when already initialized.");
            Debug.Assert(family != IntPtr.Zero, "Setting GDI+ native font family to null.");

            _nativeFamily = family;
#if DEBUG
            lock (s_lockObj)
            {
                _id = ++s_idCount;
            }
#endif
        }

        ///<summary>
        /// Internal constructor to initialize the native GDI+ font to an existing one. Used to create generic fonts
        /// and by FontCollection class.
        ///</summary>
        internal FontFamily(IntPtr family)
        {
            SetNativeFamily(family);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='FontFamily'/> class with the specified name.
        ///
        /// The <paramref name="createDefaultOnFail"/> parameter determines how errors are handled when creating a
        /// font based on a font family that does not exist on the end user's system at run time. If this parameter is
        /// true, then a fall-back fontwill always be used instead. If this parameter is false, an exception will be thrown.
        /// </summary>
        internal FontFamily(string name, bool createDefaultOnFail)
        {
            _createDefaultOnFail = createDefaultOnFail;
            CreateFontFamily(name, null);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='FontFamily'/> class with the specified name.
        /// </summary>
        public FontFamily(string name)
        {
            CreateFontFamily(name, null);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='FontFamily'/> class in the specified
        /// <see cref='FontCollection'/> and with the specified name.
        /// </summary>
        public FontFamily(string name, FontCollection fontCollection)
        {
            CreateFontFamily(name, fontCollection);
        }

        /// <summary>
        /// Creates the native font family object. Note: GDI+ creates singleton font family objects (from the
        /// corresponding font file) and reference count them.
        /// </summary>
        private void CreateFontFamily(string name, FontCollection fontCollection)
        {
            IntPtr fontfamily = IntPtr.Zero;
            IntPtr nativeFontCollection = (fontCollection == null) ? IntPtr.Zero : fontCollection._nativeFontCollection;

            int status = SafeNativeMethods.Gdip.GdipCreateFontFamilyFromName(name, new HandleRef(fontCollection, nativeFontCollection), out fontfamily);

            if (status != SafeNativeMethods.Gdip.Ok)
            {
                if (_createDefaultOnFail)
                {
                    fontfamily = GetGdipGenericSansSerif();  // This throws if failed.
                }
                else
                {
                    // Special case this incredibly common error message to give more information
                    if (status == SafeNativeMethods.Gdip.FontFamilyNotFound)
                    {
                        throw new ArgumentException(SR.Format(SR.GdiplusFontFamilyNotFound, name));
                    }
                    else if (status == SafeNativeMethods.Gdip.NotTrueTypeFont)
                    {
                        throw new ArgumentException(SR.Format(SR.GdiplusNotTrueTypeFont, name));
                    }
                    else
                    {
                        throw SafeNativeMethods.Gdip.StatusException(status);
                    }
                }
            }

            SetNativeFamily(fontfamily);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='FontFamily'/> class from the specified generic font family.
        /// </summary>
        public FontFamily(GenericFontFamilies genericFamily)
        {
            IntPtr fontfamily = IntPtr.Zero;
            int status;

            switch (genericFamily)
            {
                case GenericFontFamilies.Serif:
                    {
                        status = SafeNativeMethods.Gdip.GdipGetGenericFontFamilySerif(out fontfamily);
                        break;
                    }
                case GenericFontFamilies.SansSerif:
                    {
                        status = SafeNativeMethods.Gdip.GdipGetGenericFontFamilySansSerif(out fontfamily);
                        break;
                    }
                case GenericFontFamilies.Monospace:
                default:
                    {
                        status = SafeNativeMethods.Gdip.GdipGetGenericFontFamilyMonospace(out fontfamily);
                        break;
                    }
            }

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            SetNativeFamily(fontfamily);
        }

        ~FontFamily()
        {
            Dispose(false);
        }

        /// <summary>
        /// The GDI+ native font family. It is shared by all FontFamily objects with same family name.
        /// </summary>
        internal IntPtr NativeFamily
        {
            get
            {
                return _nativeFamily;
            }
        }

        // The managed wrappers do not expose a Clone method, as it's really nothing more
        // than AddRef (it doesn't copy the underlying GpFont), and in a garbage collected
        // world, that's not very useful.

        public override bool Equals(object obj)
        {
            if (obj == this)
                return true;

            FontFamily ff = obj as FontFamily;

            if (ff == null)
                return false;

            // We can safely use the ptr to the native GDI+ FontFamily because it is common to 
            // all objects of the same family (singleton RO object).
            return ff.NativeFamily == NativeFamily;
        }

        /// <summary>
        /// Converts this <see cref='FontFamily'/> to a human-readable string.
        /// </summary>
        public override string ToString()
        {
            return string.Format(CultureInfo.CurrentCulture, "[{0}: Name={1}]", GetType().Name, Name);
        }

        /// <summary>
        /// Gets a hash code for this <see cref='FontFamily'/>.
        /// </summary>
        public override int GetHashCode()
        {
            return GetName(LANG_NEUTRAL).GetHashCode();
        }

        private static int CurrentLanguage
        {
            get
            {
                return System.Globalization.CultureInfo.CurrentUICulture.LCID;
            }
        }

        /// <summary>
        /// Disposes of this <see cref='FontFamily'/>.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (_nativeFamily != IntPtr.Zero)
            {
                try
                {
#if DEBUG
                    int status =
#endif
                    SafeNativeMethods.Gdip.GdipDeleteFontFamily(new HandleRef(this, _nativeFamily));
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
                    _nativeFamily = IntPtr.Zero;
                }
            }
        }

        /// <summary>
        /// Gets the name of this <see cref='FontFamily'/>.
        /// </summary>
        public String Name
        {
            get
            {
                return GetName(CurrentLanguage);
            }
        }

        /// <summary>
        /// Retuns the name of this <see cref='FontFamily'/> in the specified language.
        /// </summary>
        public String GetName(int language)
        {
            // LF_FACESIZE is 32
            StringBuilder name = new StringBuilder(32);

            int status = SafeNativeMethods.Gdip.GdipGetFamilyName(new HandleRef(this, NativeFamily), name, language);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            return name.ToString();
        }


        /// <summary>
        /// Returns an array that contains all of the <see cref='FontFamily'/> objects associated with the current
        /// graphics context.
        /// </summary>
        public static FontFamily[] Families
        {
            get
            {
                return new InstalledFontCollection().Families;
            }
        }

        /// <summary>
        /// Gets a generic SansSerif <see cref='FontFamily'/>.
        /// </summary>
        public static FontFamily GenericSansSerif
        {
            get
            {
                return new FontFamily(GetGdipGenericSansSerif());
            }
        }

        private static IntPtr GetGdipGenericSansSerif()
        {
            IntPtr fontfamily = IntPtr.Zero;

            int status = SafeNativeMethods.Gdip.GdipGetGenericFontFamilySansSerif(out fontfamily);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            return fontfamily;
        }

        /// <summary>
        /// Gets a generic Serif <see cref='FontFamily'/>.
        /// </summary>
        public static FontFamily GenericSerif
        {
            get
            {
                return new FontFamily(GetNativeGenericSerif());
            }
        }

        private static IntPtr GetNativeGenericSerif()
        {
            IntPtr fontfamily = IntPtr.Zero;

            int status = SafeNativeMethods.Gdip.GdipGetGenericFontFamilySerif(out fontfamily);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            return fontfamily;
        }

        /// <summary>
        /// Gets a generic monospace <see cref='FontFamily'/>.
        /// </summary>
        public static FontFamily GenericMonospace
        {
            get
            {
                return new FontFamily(GetNativeGenericMonospace());
            }
        }

        private static IntPtr GetNativeGenericMonospace()
        {
            IntPtr fontfamily = IntPtr.Zero;

            int status = SafeNativeMethods.Gdip.GdipGetGenericFontFamilyMonospace(out fontfamily);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            return fontfamily;
        }

        /// <summary>
        /// Returns an array that contains all of the <see cref='FontFamily'/> objects associated with the specified
        /// graphics context.
        /// </summary>
        [Obsolete("Do not use method GetFamilies, use property Families instead")]
        public static FontFamily[] GetFamilies(Graphics graphics)
        {
            if (graphics == null)
                throw new ArgumentNullException("graphics");

            return new InstalledFontCollection().Families;
        }

        /// <summary>
        /// Indicates whether the specified <see cref='FontStyle'/> is available.
        /// </summary>
        public bool IsStyleAvailable(FontStyle style)
        {
            int bresult;

            int status = SafeNativeMethods.Gdip.GdipIsStyleAvailable(new HandleRef(this, NativeFamily), style, out bresult);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            return bresult != 0;
        }

        /// <summary>
        /// Gets the size of the Em square for the specified style in font design units.
        /// </summary>
        public int GetEmHeight(FontStyle style)
        {
            int result = 0;

            int status = SafeNativeMethods.Gdip.GdipGetEmHeight(new HandleRef(this, NativeFamily), style, out result);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            return result;
        }


        /// <summary>
        /// Returns the ascender metric for Windows.
        /// </summary>
        public int GetCellAscent(FontStyle style)
        {
            int result = 0;

            int status = SafeNativeMethods.Gdip.GdipGetCellAscent(new HandleRef(this, NativeFamily), style, out result);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            return result;
        }

        /// <summary>
        /// Returns the descender metric for Windows.
        /// </summary>
        public int GetCellDescent(FontStyle style)
        {
            int result = 0;

            int status = SafeNativeMethods.Gdip.GdipGetCellDescent(new HandleRef(this, NativeFamily), style, out result);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            return result;
        }

        /// <summary>
        /// Returns the distance between two consecutive lines of text for this <see cref='FontFamily'/> with the
        /// specified <see cref='FontStyle'/>.
        /// </summary>
        public int GetLineSpacing(FontStyle style)
        {
            int result = 0;

            int status = SafeNativeMethods.Gdip.GdipGetLineSpacing(new HandleRef(this, NativeFamily), style, out result);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            return result;
        }
    }
}
