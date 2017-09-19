// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.
//
// MetaHeader class testing unit
//
// Authors:
//	Sebastien Pouliot  <sebastien@ximian.com>
//
// Copyright (C) 2006 Novell, Inc (http://www.novell.com)
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

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Security.Permissions;
using Xunit;

namespace MonoTests.System.Drawing.Imaging
{

    public class MetaHeaderTest
    {

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void DefaultValues()
        {
            MetaHeader mh = new MetaHeader();
            Assert.Equal(0, mh.HeaderSize);
            Assert.Equal(0, mh.MaxRecord);
            Assert.Equal(0, mh.NoObjects);
            Assert.Equal(0, mh.NoParameters);
            Assert.Equal(0, mh.Size);
            Assert.Equal(0, mh.Type);
            Assert.Equal(0, mh.Version);
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void Min()
        {
            MetaHeader mh = new MetaHeader();
            mh.HeaderSize = short.MinValue;
            Assert.Equal(short.MinValue, mh.HeaderSize);
            mh.MaxRecord = int.MinValue;
            Assert.Equal(int.MinValue, mh.MaxRecord);
            mh.NoObjects = short.MinValue;
            Assert.Equal(short.MinValue, mh.NoObjects);
            mh.NoParameters = short.MinValue;
            Assert.Equal(short.MinValue, mh.NoParameters);
            mh.Size = int.MinValue;
            Assert.Equal(int.MinValue, mh.Size);
            mh.Type = short.MinValue;
            Assert.Equal(short.MinValue, mh.Type);
            mh.Version = short.MinValue;
            Assert.Equal(short.MinValue, mh.Version);
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void Max()
        {
            MetaHeader mh = new MetaHeader();
            mh.HeaderSize = short.MaxValue;
            Assert.Equal(short.MaxValue, mh.HeaderSize);
            mh.MaxRecord = int.MaxValue;
            Assert.Equal(int.MaxValue, mh.MaxRecord);
            mh.NoObjects = short.MaxValue;
            Assert.Equal(short.MaxValue, mh.NoObjects);
            mh.NoParameters = short.MaxValue;
            Assert.Equal(short.MaxValue, mh.NoParameters);
            mh.Size = int.MaxValue;
            Assert.Equal(int.MaxValue, mh.Size);
            mh.Type = short.MaxValue;
            Assert.Equal(short.MaxValue, mh.Type);
            mh.Version = short.MaxValue;
            Assert.Equal(short.MaxValue, mh.Version);
        }
    }
}
