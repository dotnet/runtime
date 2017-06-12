// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// Copyright (C) 2009 Novell, Inc (http://www.novell.com)
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
// Author:
// 	Andy Hume <andyhume32@yahoo.co.uk>
//

using System;
using System.Drawing.Printing;
using Xunit;

namespace System.Drawing.Printing.Test
{
    public class PaperSourceTest
    {

        [Fact]
        public void KindTest()
        {
            PaperSource ps = new PaperSource();

            //
            // Set Custom
            ps.RawKind = (int)PaperSourceKind.Custom;
            Assert.Equal(PaperSourceKind.Custom, ps.Kind);
            Assert.Equal(257, ps.RawKind);

            //
            // An integer value of 256 and above returns Custom (0x257)
            ps.RawKind = 256;
            Assert.Equal(256, ps.RawKind);
            Assert.Equal(PaperSourceKind.Custom, ps.Kind);

            //
            // Zero
            ps.RawKind = 0;
            Assert.Equal((PaperSourceKind)0, ps.Kind);
            Assert.Equal(0, ps.RawKind);

            //
            // Well-known
            ps.RawKind = (int)PaperSourceKind.Upper;
            Assert.Equal(PaperSourceKind.Upper, ps.Kind);
            Assert.Equal((int)PaperSourceKind.Upper, ps.RawKind);

            //
            ps.RawKind = (int)PaperSourceKind.FormSource;
            Assert.Equal(PaperSourceKind.FormSource, ps.Kind);
            Assert.Equal((int)PaperSourceKind.FormSource, ps.RawKind);

            //
            // Too Big
            ps.RawKind = 999999;
            Assert.Equal(PaperSourceKind.Custom, ps.Kind);
            Assert.Equal(999999, ps.RawKind);

            //
            ps.RawKind = int.MaxValue;
            Assert.Equal(PaperSourceKind.Custom, ps.Kind);
            Assert.Equal(int.MaxValue, ps.RawKind);

            //
            // Negative -- Looks as if MSFT forgot to check for negative!
            ps.RawKind = -1;
            Assert.Equal((PaperSourceKind)(-1), ps.Kind);
            Assert.Equal(-1, ps.RawKind);

            //
            ps.RawKind = int.MinValue;
            Assert.Equal((PaperSourceKind)(int.MinValue), ps.Kind);
            Assert.Equal(int.MinValue, ps.RawKind);
        }
    }
}
