// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.
//
// System.Drawing.SystemIcons.cs
//
// Authors:
//   Dennis Hayes (dennish@Raytek.com)
//   Andreas Nahr (ClassDevelopment@A-SoftTech.com)
//   Sebastien Pouliot  <sebastien@ximian.com>
//
// (C) 2002 Ximian, Inc
// Copyright (C) 2004, 2006 Novell, Inc (http://www.novell.com)
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

namespace System.Drawing
{

    // LAME: I don't see why the "old" (win 2.x) names were exposed in the fx :|

    public sealed class SystemIcons
    {

        static Icon[] icons;
        private const int Application_Winlogo = 0;
        private const int Asterisk_Information = 1;
        private const int Error_Hand = 2;
        private const int Exclamation_Warning = 3;
        private const int Question_ = 4;
        private const int Shield_ = 5;

        static SystemIcons()
        {
            // we minimize the # of icons to load since most of them are duplicates
            icons = new Icon[6];
            // we use an internal .ctor to ensure the SystemIcons can't de disposed
#if NETCORE
            // TODO: Decide which icons to use for this.
            icons[Application_Winlogo] = new Icon("placeholder.ico", true);
            icons[Asterisk_Information] = new Icon("placeholder.ico", true);
            icons[Error_Hand] = new Icon("placeholder.ico", true);
            icons[Exclamation_Warning] = new Icon("placeholder.ico", true);
            icons[Question_] = new Icon("placeholder.ico", true);
            icons[Shield_] = new Icon("placeholder.ico", true);
#else
            icons[Application_Winlogo] = new Icon("Mono.ico", true);
            icons[Asterisk_Information] = new Icon("Information.ico", true);
            icons[Error_Hand] = new Icon("Error.ico", true);
            icons[Exclamation_Warning] = new Icon("Warning.ico", true);
            icons[Question_] = new Icon("Question.ico", true);
            icons[Shield_] = new Icon("Shield.ico", true);
#endif
        }

        private SystemIcons()
        {
        }

        // note: same as WinLogo (for Mono)
        public static Icon Application
        {
            get { return icons[Application_Winlogo]; }
        }

        // note: same as Information
        public static Icon Asterisk
        {
            get { return icons[Asterisk_Information]; }
        }

        // note: same as Hand
        public static Icon Error
        {
            get { return icons[Error_Hand]; }
        }

        // same as Warning
        public static Icon Exclamation
        {
            get { return icons[Exclamation_Warning]; }
        }

        // note: same as Error
        public static Icon Hand
        {
            get { return icons[Error_Hand]; }
        }

        // note: same as Asterisk
        public static Icon Information
        {
            get { return icons[Asterisk_Information]; }
        }

        public static Icon Question
        {
            get { return icons[Question_]; }
        }

        // note: same as Exclamation
        public static Icon Warning
        {
            get { return icons[Exclamation_Warning]; }
        }

        // note: same as Application (for Mono)
        public static Icon WinLogo
        {
            get { return icons[Application_Winlogo]; }
        }

        public static Icon Shield
        {
            get { return icons[Shield_]; }
        }
    }
}
