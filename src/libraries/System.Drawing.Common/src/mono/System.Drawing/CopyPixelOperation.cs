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
//   Jordi Mas i Hernandez <jordimash@gmail.com>
//
//


using System;
using System.Runtime.InteropServices;

namespace System.Drawing
{
    [ComVisibleAttribute(true)]
    public enum CopyPixelOperation
    {
        Blackness = 0x00000042,
        CaptureBlt = 0x40000000,
        DestinationInvert = 0x00550009,
        MergeCopy = 0x00C000CA,
        MergePaint = 0x00BB0226,
        NoMirrorBitmap = -2147483648,
        NotSourceCopy = 0x00330008,
        NotSourceErase = 0x001100A6,
        PatCopy = 0x00F00021,
        PatInvert = 0x005A0049,
        PatPaint = 0x00FB0A09,
        SourceAnd = 0x008800C6,
        SourceCopy = 0x00CC0020,
        SourceErase = 0x00440328,
        SourceInvert = 0x00660046,
        SourcePaint = 0x00EE0086,
        Whiteness = 0x00FF0062,
    }
}


