// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.
//
// Copyright (C) 2005, 2007 Novell, Inc (http://www.novell.com)
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
//    Jordi Mas i Hernandez <jordimash@gmail.com>
//    Sebastien Pouliot  <sebastien@ximian.com>
//


namespace System.Drawing
{

    public static partial class SystemFonts
    {
        public static Font CaptionFont
        {
            get { return new Font("Microsoft Sans Serif", 11, "CaptionFont"); }
        }

        public static Font DefaultFont
        {
            get { return new Font("Microsoft Sans Serif", 8.25f, "DefaultFont"); }
        }

        public static Font DialogFont
        {
            get { return new Font("Tahoma", 8, "DialogFont"); }
        }

        public static Font IconTitleFont
        {
            get { return new Font("Microsoft Sans Serif", 11, "IconTitleFont"); }
        }

        public static Font MenuFont
        {
            get { return new Font("Microsoft Sans Serif", 11, "MenuFont"); }
        }

        public static Font MessageBoxFont
        {
            get { return new Font("Microsoft Sans Serif", 11, "MessageBoxFont"); }
        }

        public static Font SmallCaptionFont
        {
            get { return new Font("Microsoft Sans Serif", 11, "SmallCaptionFont"); }
        }

        public static Font StatusFont
        {
            get { return new Font("Microsoft Sans Serif", 11, "StatusFont"); }
        }
    }
}

