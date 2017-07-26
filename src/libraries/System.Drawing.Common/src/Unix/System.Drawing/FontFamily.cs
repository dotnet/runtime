// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.
//
// System.Drawing.FontFamily.cs
//
// Author:
//   Dennis Hayes (dennish@Raytek.com)
//   Alexandre Pigolkine (pigolkine@gmx.de)
//   Peter Dennis Bartok (pbartok@novell.com)
//
// Copyright (C) 2002/2004 Ximian, Inc http://www.ximian.com
// Copyright (C) 2004 - 2006 Novell, Inc (http://www.novell.com)
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

using System.Drawing.Text;
using System.Text;
using System.Runtime.InteropServices;

namespace System.Drawing
{

    public sealed class FontFamily : MarshalByRefObject, IDisposable
    {

        //static private FontFamily genericMonospace;
        //static private FontFamily genericSansSerif;
        //static private FontFamily genericSerif;
        private string name;
        private IntPtr nativeFontFamily = IntPtr.Zero;

        internal FontFamily(IntPtr fntfamily)
        {
            nativeFontFamily = fntfamily;
        }

        internal void refreshName()
        {
            StringBuilder sb;

            if (nativeFontFamily == IntPtr.Zero)
                return;

            sb = new StringBuilder(GDIPlus.FACESIZE);
            Status status = SafeNativeMethods.Gdip.GdipGetFamilyName(nativeFontFamily, sb, 0);
            SafeNativeMethods.Gdip.CheckStatus(status);
            name = sb.ToString();
        }

        ~FontFamily()
        {
            Dispose();
        }

        internal IntPtr NativeObject
        {
            get
            {
                return nativeFontFamily;
            }
        }

        internal IntPtr NativeFamily
        {
            get
            {
                return nativeFontFamily;
            }
        }

        public FontFamily(GenericFontFamilies genericFamily)
        {
            Status status;
            switch (genericFamily)
            {
                case GenericFontFamilies.SansSerif:
                    status = SafeNativeMethods.Gdip.GdipGetGenericFontFamilySansSerif(out nativeFontFamily);
                    break;
                case GenericFontFamilies.Serif:
                    status = SafeNativeMethods.Gdip.GdipGetGenericFontFamilySerif(out nativeFontFamily);
                    break;
                case GenericFontFamilies.Monospace:
                default:    // Undocumented default 
                    status = SafeNativeMethods.Gdip.GdipGetGenericFontFamilyMonospace(out nativeFontFamily);
                    break;
            }
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public FontFamily(string name) : this(name, null)
        {
        }

        public FontFamily(string name, FontCollection fontCollection)
        {
            IntPtr handle = (fontCollection == null) ? IntPtr.Zero : fontCollection._nativeFontCollection;
            Status status = SafeNativeMethods.Gdip.GdipCreateFontFamilyFromName(name, handle, out nativeFontFamily);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public string Name
        {
            get
            {
                if (nativeFontFamily == IntPtr.Zero)
                    throw new ArgumentException("Name", "Object was disposed.");
                if (name == null)
                    refreshName();
                return name;
            }
        }

        public static FontFamily GenericMonospace
        {
            get { return new FontFamily(GenericFontFamilies.Monospace); }
        }

        public static FontFamily GenericSansSerif
        {
            get { return new FontFamily(GenericFontFamilies.SansSerif); }
        }

        public static FontFamily GenericSerif
        {
            get { return new FontFamily(GenericFontFamilies.Serif); }
        }

        public int GetCellAscent(FontStyle style)
        {
            short outProperty;
            Status status = SafeNativeMethods.Gdip.GdipGetCellAscent(nativeFontFamily, (int)style, out outProperty);
            SafeNativeMethods.Gdip.CheckStatus(status);

            return (int)outProperty;
        }

        public int GetCellDescent(FontStyle style)
        {
            short outProperty;
            Status status = SafeNativeMethods.Gdip.GdipGetCellDescent(nativeFontFamily, (int)style, out outProperty);
            SafeNativeMethods.Gdip.CheckStatus(status);

            return (int)outProperty;
        }

        public int GetEmHeight(FontStyle style)
        {
            short outProperty;
            Status status = SafeNativeMethods.Gdip.GdipGetEmHeight(nativeFontFamily, (int)style, out outProperty);
            SafeNativeMethods.Gdip.CheckStatus(status);

            return (int)outProperty;
        }

        public int GetLineSpacing(FontStyle style)
        {
            short outProperty;
            Status status = SafeNativeMethods.Gdip.GdipGetLineSpacing(nativeFontFamily, (int)style, out outProperty);
            SafeNativeMethods.Gdip.CheckStatus(status);

            return (int)outProperty;
        }

        [MonoDocumentationNote("When used with libgdiplus this method always return true (styles are created on demand).")]
        public bool IsStyleAvailable(FontStyle style)
        {
            bool outProperty;
            Status status = SafeNativeMethods.Gdip.GdipIsStyleAvailable(nativeFontFamily, (int)style, out outProperty);
            SafeNativeMethods.Gdip.CheckStatus(status);

            return outProperty;
        }

        public void Dispose()
        {
            if (nativeFontFamily != IntPtr.Zero)
            {
                Status status = SafeNativeMethods.Gdip.GdipDeleteFontFamily(nativeFontFamily);
                nativeFontFamily = IntPtr.Zero;
                GC.SuppressFinalize(this);
                // check the status code (throw) at the last step
                SafeNativeMethods.Gdip.CheckStatus(status);
            }
        }

        public override bool Equals(object obj)
        {
            FontFamily o = (obj as FontFamily);
            if (o == null)
                return false;

            return (Name == o.Name);
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }


        public static FontFamily[] Families
        {
            get { return new InstalledFontCollection().Families; }
        }

        public static FontFamily[] GetFamilies(Graphics graphics)
        {
            if (graphics == null)
                throw new ArgumentNullException("graphics");

            InstalledFontCollection fntcol = new InstalledFontCollection();
            return fntcol.Families;
        }

        [MonoLimitation("The language parameter is ignored. We always return the name using the default system language.")]
        public string GetName(int language)
        {
            return Name;
        }

        public override string ToString()
        {
            return String.Concat("[FontFamily: Name=", Name, "]");
        }
    }
}

