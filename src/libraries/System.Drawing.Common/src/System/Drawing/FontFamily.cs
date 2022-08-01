// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing.Text;
using System.Globalization;
using System.Runtime.InteropServices;
using Gdip = System.Drawing.SafeNativeMethods.Gdip;

namespace System.Drawing
{
    /// <summary>
    /// Abstracts a group of type faces having a similar basic design but having certain variation in styles.
    /// </summary>
    public sealed class FontFamily : MarshalByRefObject, IDisposable
    {
        private const int NeutralLanguage = 0;
        private IntPtr _nativeFamily;
        private readonly bool _createDefaultOnFail;

#if DEBUG
        private static readonly object s_lockObj = new object();
        private static int s_idCount;
        private int _id;
#endif

        private void SetNativeFamily(IntPtr family)
        {
            Debug.Assert(_nativeFamily == IntPtr.Zero, "Setting GDI+ native font family when already initialized.");

            _nativeFamily = family;
#if DEBUG
            lock (s_lockObj)
            {
                _id = ++s_idCount;
            }
#endif
        }

        internal FontFamily(IntPtr family) => SetNativeFamily(family);

        /// <summary>
        /// Initializes a new instance of the <see cref='FontFamily'/> class with the specified name.
        ///
        /// The <paramref name="createDefaultOnFail"/> parameter determines how errors are handled when creating a
        /// font based on a font family that does not exist on the end user's system at run time. If this parameter is
        /// true, then a fall-back font will always be used instead. If this parameter is false, an exception will be thrown.
        /// </summary>
        internal FontFamily(string name, bool createDefaultOnFail)
        {
            _createDefaultOnFail = createDefaultOnFail;
            CreateFontFamily(name, null);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='FontFamily'/> class with the specified name.
        /// </summary>
        public FontFamily(string name) => CreateFontFamily(name, null);

        /// <summary>
        /// Initializes a new instance of the <see cref='FontFamily'/> class in the specified
        /// <see cref='FontCollection'/> and with the specified name.
        /// </summary>
        public FontFamily(string name, FontCollection? fontCollection) => CreateFontFamily(name, fontCollection);

        // Creates the native font family object.
        // Note: GDI+ creates singleton font family objects (from the corresponding font file) and reference count them.
        private void CreateFontFamily(string name, FontCollection? fontCollection)
        {
            IntPtr fontfamily;
            IntPtr nativeFontCollection = (fontCollection == null) ? IntPtr.Zero : fontCollection._nativeFontCollection;

            int status = Gdip.GdipCreateFontFamilyFromName(name, new HandleRef(fontCollection, nativeFontCollection), out fontfamily);

            if (status != Gdip.Ok)
            {
                if (_createDefaultOnFail)
                {
                    fontfamily = GetGdipGenericSansSerif(); // This throws if failed.
                }
                else
                {
                    // Special case this incredibly common error message to give more information.
                    if (status == Gdip.FontFamilyNotFound)
                    {
                        throw new ArgumentException(SR.Format(SR.GdiplusFontFamilyNotFound, name));
                    }
                    else if (status == Gdip.NotTrueTypeFont)
                    {
                        throw new ArgumentException(SR.Format(SR.GdiplusNotTrueTypeFont, name));
                    }
                    else
                    {
                        throw Gdip.StatusException(status);
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
            IntPtr nativeFamily;
            int status;

            switch (genericFamily)
            {
                case GenericFontFamilies.Serif:
                    status = Gdip.GdipGetGenericFontFamilySerif(out nativeFamily);
                    break;
                case GenericFontFamilies.SansSerif:
                    status = Gdip.GdipGetGenericFontFamilySansSerif(out nativeFamily);
                    break;
                case GenericFontFamilies.Monospace:
                default:
                    status = Gdip.GdipGetGenericFontFamilyMonospace(out nativeFamily);
                    break;
            }
            Gdip.CheckStatus(status);

            SetNativeFamily(nativeFamily);
        }

        ~FontFamily() => Dispose(false);

        internal IntPtr NativeFamily => _nativeFamily;

        /// <summary>
        /// Converts this <see cref='FontFamily'/> to a human-readable string.
        /// </summary>
        public override string ToString() => $"[{GetType().Name}: Name={Name}]";

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj == this)
            {
                return true;
            }

            // if obj = null then (obj is FontFamily) = false.
            if (!(obj is FontFamily otherFamily))
            {
                return false;
            }

            // We can safely use the ptr to the native GDI+ FontFamily because in windows it is common to
            // all objects of the same family (singleton RO object).
            return otherFamily.NativeFamily == NativeFamily;
        }

        /// <summary>
        /// Gets a hash code for this <see cref='FontFamily'/>.
        /// </summary>
        public override int GetHashCode() => GetName(NeutralLanguage).GetHashCode();

        private static int CurrentLanguage => CultureInfo.CurrentUICulture.LCID;

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
                    int status = !Gdip.Initialized ? Gdip.Ok :
#endif
                    Gdip.GdipDeleteFontFamily(new HandleRef(this, _nativeFamily));
#if DEBUG
                    Debug.Assert(status == Gdip.Ok, $"GDI+ returned an error status: {status.ToString(CultureInfo.InvariantCulture)}");
#endif
                }
                catch (Exception ex) when (!ClientUtils.IsCriticalException(ex))
                {
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
        public string Name => GetName(CurrentLanguage);

        /// <summary>
        /// Returns the name of this <see cref='FontFamily'/> in the specified language.
        /// </summary>
        public unsafe string GetName(int language)
        {
            char* name = stackalloc char[32]; // LF_FACESIZE is 32
            int status = Gdip.GdipGetFamilyName(new HandleRef(this, NativeFamily), name, language);
            Gdip.CheckStatus(status);
            return Marshal.PtrToStringUni((IntPtr)name)!;
        }

        /// <summary>
        /// Returns an array that contains all of the <see cref='FontFamily'/> objects associated with the current
        /// graphics context.
        /// </summary>
        public static FontFamily[] Families => new InstalledFontCollection().Families;

        /// <summary>
        /// Gets a generic SansSerif <see cref='FontFamily'/>.
        /// </summary>
        public static FontFamily GenericSansSerif => new FontFamily(GetGdipGenericSansSerif());

        private static IntPtr GetGdipGenericSansSerif()
        {
            IntPtr nativeFamily;
            int status = Gdip.GdipGetGenericFontFamilySansSerif(out nativeFamily);
            Gdip.CheckStatus(status);

            return nativeFamily;
        }

        /// <summary>
        /// Gets a generic Serif <see cref='FontFamily'/>.
        /// </summary>
        public static FontFamily GenericSerif => new FontFamily(GenericFontFamilies.Serif);

        /// <summary>
        /// Gets a generic monospace <see cref='FontFamily'/>.
        /// </summary>
        public static FontFamily GenericMonospace => new FontFamily(GenericFontFamilies.Monospace);

        /// <summary>
        /// Returns an array that contains all of the <see cref='FontFamily'/> objects associated with the specified
        /// graphics context.
        /// </summary>
        [Obsolete("FontFamily.GetFamilies has been deprecated. Use Families instead.")]
        public static FontFamily[] GetFamilies(Graphics graphics)
        {
            ArgumentNullException.ThrowIfNull(graphics);

            return new InstalledFontCollection().Families;
        }

        /// <summary>
        /// Indicates whether the specified <see cref='FontStyle'/> is available.
        /// </summary>
        public bool IsStyleAvailable(FontStyle style)
        {
            int bresult;
            int status = Gdip.GdipIsStyleAvailable(new HandleRef(this, NativeFamily), style, out bresult);
            Gdip.CheckStatus(status);

            return bresult != 0;
        }

        /// <summary>
        /// Gets the size of the Em square for the specified style in font design units.
        /// </summary>
        public int GetEmHeight(FontStyle style)
        {
            int result;
            int status = Gdip.GdipGetEmHeight(new HandleRef(this, NativeFamily), style, out result);
            Gdip.CheckStatus(status);

            return result;
        }

        /// <summary>
        /// Returns the ascender metric for Windows.
        /// </summary>
        public int GetCellAscent(FontStyle style)
        {
            int result;
            int status = Gdip.GdipGetCellAscent(new HandleRef(this, NativeFamily), style, out result);
            Gdip.CheckStatus(status);

            return result;
        }

        /// <summary>
        /// Returns the descender metric for Windows.
        /// </summary>
        public int GetCellDescent(FontStyle style)
        {
            int result;
            int status = Gdip.GdipGetCellDescent(new HandleRef(this, NativeFamily), style, out result);
            Gdip.CheckStatus(status);

            return result;
        }

        /// <summary>
        /// Returns the distance between two consecutive lines of text for this <see cref='FontFamily'/> with the
        /// specified <see cref='FontStyle'/>.
        /// </summary>
        public int GetLineSpacing(FontStyle style)
        {
            int result;
            int status = Gdip.GdipGetLineSpacing(new HandleRef(this, NativeFamily), style, out result);
            Gdip.CheckStatus(status);

            return result;
        }
    }
}
