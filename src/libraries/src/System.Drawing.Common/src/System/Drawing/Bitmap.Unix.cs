// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.
//
// System.Drawing.Bitmap.cs
//
// Copyright (C) 2002 Ximian, Inc.  http://www.ximian.com
// Copyright (C) 2004 Novell, Inc.  http://www.novell.com
//
// Authors: 
//    Alexandre Pigolkine (pigolkine@gmx.de)
//    Christian Meyer (Christian.Meyer@cs.tum.edu)
//    Miguel de Icaza (miguel@ximian.com)
//    Jordi Mas i Hernandez (jmas@softcatala.org)
//    Ravindra (rkumar@novell.com)
//

//
// Copyright (C) 2004-2005 Novell, Inc (http://www.novell.com)
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
using System.Drawing.Imaging;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.InteropServices;
using System.ComponentModel;

namespace System.Drawing
{
#if !NETCORE
    [Editor ("System.Drawing.Design.BitmapEditor, " + Consts.AssemblySystem_Drawing_Design, typeof (System.Drawing.Design.UITypeEditor))]
#endif
    public sealed partial class Bitmap
    {
        #region constructors

        // Usually called when cloning images that need to have
        // not only the handle saved, but also the underlying stream
        // (when using MS GDI+ and IStream we must ensure the stream stays alive for all the life of the Image)
        internal Bitmap(IntPtr ptr, Stream stream)
        {
            nativeImage = ptr;
        }

        public Bitmap(Stream stream, bool useIcm)
        {
            // false: stream is owned by user code
            nativeImage = InitializeFromStream(stream);
        }

        public Bitmap(Type type, string resource)
        {
            if (resource == null)
                throw new ArgumentException(nameof(resource));

            // For compatibility with the .NET Framework
            if (type == null)
                throw new NullReferenceException();

            Stream s = type.GetTypeInfo().Assembly.GetManifestResourceStream(type, resource);
            if (s == null)
            {
                string msg = string.Format("Resource '{0}' was not found.", resource);
                throw new FileNotFoundException(msg);
            }

            nativeImage = InitializeFromStream(s);
        }
        #endregion
    }
}
