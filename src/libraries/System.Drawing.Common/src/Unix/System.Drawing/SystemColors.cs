// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.
//
// System.Drawing.SystemColors
//
// Copyright (C) 2002 Ximian, Inc (http://www.ximian.com)
// Copyright (C) 2004-2005, 2007 Novell, Inc (http://www.novell.com)
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
//    Gonzalo Paniagua Javier (gonzalo@ximian.com)
//    Peter Dennis Bartok (pbartok@novell.com)
//    Sebastien Pouliot  <sebastien@ximian.com>
//

namespace System.Drawing
{

    public sealed class SystemColors
    {

        private SystemColors()
        {
        }

        static public Color ActiveBorder
        {
            get { return KnownColors.FromKnownColor(KnownColor.ActiveBorder); }
        }

        static public Color ActiveCaption
        {
            get { return KnownColors.FromKnownColor(KnownColor.ActiveCaption); }
        }

        static public Color ActiveCaptionText
        {
            get { return KnownColors.FromKnownColor(KnownColor.ActiveCaptionText); }
        }

        static public Color AppWorkspace
        {
            get { return KnownColors.FromKnownColor(KnownColor.AppWorkspace); }
        }

        static public Color Control
        {
            get { return KnownColors.FromKnownColor(KnownColor.Control); }
        }

        static public Color ControlDark
        {
            get { return KnownColors.FromKnownColor(KnownColor.ControlDark); }
        }

        static public Color ControlDarkDark
        {
            get { return KnownColors.FromKnownColor(KnownColor.ControlDarkDark); }
        }

        static public Color ControlLight
        {
            get { return KnownColors.FromKnownColor(KnownColor.ControlLight); }
        }

        static public Color ControlLightLight
        {
            get { return KnownColors.FromKnownColor(KnownColor.ControlLightLight); }
        }

        static public Color ControlText
        {
            get { return KnownColors.FromKnownColor(KnownColor.ControlText); }
        }

        static public Color Desktop
        {
            get { return KnownColors.FromKnownColor(KnownColor.Desktop); }
        }

        static public Color GrayText
        {
            get { return KnownColors.FromKnownColor(KnownColor.GrayText); }
        }

        static public Color Highlight
        {
            get { return KnownColors.FromKnownColor(KnownColor.Highlight); }
        }

        static public Color HighlightText
        {
            get { return KnownColors.FromKnownColor(KnownColor.HighlightText); }
        }

        static public Color HotTrack
        {
            get { return KnownColors.FromKnownColor(KnownColor.HotTrack); }
        }

        static public Color InactiveBorder
        {
            get { return KnownColors.FromKnownColor(KnownColor.InactiveBorder); }
        }

        static public Color InactiveCaption
        {
            get { return KnownColors.FromKnownColor(KnownColor.InactiveCaption); }
        }

        static public Color InactiveCaptionText
        {
            get { return KnownColors.FromKnownColor(KnownColor.InactiveCaptionText); }
        }

        static public Color Info
        {
            get { return KnownColors.FromKnownColor(KnownColor.Info); }
        }

        static public Color InfoText
        {
            get { return KnownColors.FromKnownColor(KnownColor.InfoText); }
        }

        static public Color Menu
        {
            get { return KnownColors.FromKnownColor(KnownColor.Menu); }
        }

        static public Color MenuText
        {
            get { return KnownColors.FromKnownColor(KnownColor.MenuText); }
        }

        static public Color ScrollBar
        {
            get { return KnownColors.FromKnownColor(KnownColor.ScrollBar); }
        }

        static public Color Window
        {
            get { return KnownColors.FromKnownColor(KnownColor.Window); }
        }

        static public Color WindowFrame
        {
            get { return KnownColors.FromKnownColor(KnownColor.WindowFrame); }
        }

        static public Color WindowText
        {
            get { return KnownColors.FromKnownColor(KnownColor.WindowText); }
        }
        static public Color ButtonFace
        {
            get { return KnownColors.FromKnownColor(KnownColor.ButtonFace); }
        }

        static public Color ButtonHighlight
        {
            get { return KnownColors.FromKnownColor(KnownColor.ButtonHighlight); }
        }

        static public Color ButtonShadow
        {
            get { return KnownColors.FromKnownColor(KnownColor.ButtonShadow); }
        }

        static public Color GradientActiveCaption
        {
            get { return KnownColors.FromKnownColor(KnownColor.GradientActiveCaption); }
        }

        static public Color GradientInactiveCaption
        {
            get { return KnownColors.FromKnownColor(KnownColor.GradientInactiveCaption); }
        }

        static public Color MenuBar
        {
            get { return KnownColors.FromKnownColor(KnownColor.MenuBar); }
        }

        static public Color MenuHighlight
        {
            get { return KnownColors.FromKnownColor(KnownColor.MenuHighlight); }
        }
    }
}
