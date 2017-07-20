// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.
//
// System.Drawing.Text.PrivateFontCollection.cs
//
// (C) 2002 Ximian, Inc.  http://www.ximian.com
// Author: Everaldo Canuto everaldo.canuto@bol.com.br
//		Sanjay Gupta (gsanjay@novell.com)
//		Peter Dennis Bartok (pbartok@novell.com)
//
//
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

using System.IO;
using System.Runtime.InteropServices;

namespace System.Drawing.Text
{

    public sealed class PrivateFontCollection : FontCollection
    {

        // constructors

        public PrivateFontCollection()
        {
            Status status = SafeNativeMethods.Gdip.GdipNewPrivateFontCollection(out _nativeFontCollection);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        // methods
        public void AddFontFile(string filename)
        {
            if (filename == null)
                throw new ArgumentNullException("filename");

            // this ensure the filename is valid (or throw the correct exception)
            string fname = Path.GetFullPath(filename);

            if (!File.Exists(fname))
                throw new FileNotFoundException();

            // note: MS throw the same exception FileNotFoundException if the file exists but isn't a valid font file
            Status status = SafeNativeMethods.Gdip.GdipPrivateAddFontFile(_nativeFontCollection, fname);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void AddMemoryFont(IntPtr memory, int length)
        {
            // note: MS throw FileNotFoundException if something is bad with the data (except for a null pointer)
            Status status = SafeNativeMethods.Gdip.GdipPrivateAddMemoryFont(_nativeFontCollection, memory, length);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        // methods	
        protected override void Dispose(bool disposing)
        {
            if (_nativeFontCollection != IntPtr.Zero)
            {
                SafeNativeMethods.Gdip.GdipDeletePrivateFontCollection(ref _nativeFontCollection);

                // This must be zeroed out, otherwise our base will also call
                // the GDI+ delete method on unix platforms. We're keeping the
                // base.Dispose() call in case other cleanup ever gets added there
                _nativeFontCollection = IntPtr.Zero;
            }

            base.Dispose(disposing);
        }
    }
}
