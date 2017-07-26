// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.
//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
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
// Authors:
//
//   Jordi Mas i Hernadez <jordimash@gmail.com>
//
//


using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace System.Drawing
{
    public sealed class BufferedGraphics : IDisposable
    {
        private Rectangle size;
        private Bitmap membmp = null;
        private Graphics target = null;
        private Graphics source = null;

        private BufferedGraphics()
        {

        }

        internal BufferedGraphics(Graphics targetGraphics, Rectangle targetRectangle)
        {
            size = targetRectangle;
            target = targetGraphics;
            membmp = new Bitmap(size.Width, size.Height);
        }

        ~BufferedGraphics()
        {
            Dispose(false);
        }

        public Graphics Graphics
        {
            get
            {
                if (source == null)
                {
                    source = Graphics.FromImage(membmp);
                }

                return source;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            System.GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing == false)
                return;

            if (membmp != null)
            {
                membmp.Dispose();
                membmp = null;
            }

            if (source != null)
            {
                source.Dispose();
                source = null;
            }

            target = null;
        }

        public void Render()
        {
            Render(target);
        }

        public void Render(Graphics target)
        {
            if (target == null)
                return;

            target.DrawImage(membmp, size);
        }

        [MonoTODO("The targetDC parameter has no equivalent in libgdiplus.")]
        public void Render(IntPtr targetDC)
        {
            throw new NotImplementedException();
        }
    }
}

