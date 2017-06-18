// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing
{
    using System.Diagnostics;

    /// <include file='doc\SystemPens.uex' path='docs/doc[@for="SystemPens"]/*' />
    /// <devdoc>
    ///     Pens for select Windows system-wide colors.  Whenever possible, try to use
    ///     SystemPens and SystemBrushes rather than SystemColors.
    /// </devdoc>
    public sealed class SystemPens
    {
        private static readonly object s_systemPensKey = new object();

        /// <include file='doc\SystemPens.uex' path='docs/doc[@for="SystemPens.ActiveBorder"]/*' />
        /// <devdoc>
        ///     Pen is the color of the filled area of an active window border.
        /// </devdoc>
        public static Pen ActiveBorder
        {
            get
            {
                return FromSystemColor(SystemColors.ActiveBorder);
            }
        }

        /// <include file='doc\SystemPens.uex' path='docs/doc[@for="SystemPens.ActiveCaption"]/*' />
        /// <devdoc>
        ///     Pen is the color of the background of an active title bar caption.
        /// </devdoc>
        public static Pen ActiveCaption
        {
            get
            {
                return FromSystemColor(SystemColors.ActiveCaption);
            }
        }

        /// <include file='doc\SystemPens.uex' path='docs/doc[@for="SystemPens.ActiveCaptionText"]/*' />
        /// <devdoc>
        ///     Pen is the color of the active window's caption text.
        /// </devdoc>
        public static Pen ActiveCaptionText
        {
            get
            {
                return FromSystemColor(SystemColors.ActiveCaptionText);
            }
        }

        /// <include file='doc\SystemPens.uex' path='docs/doc[@for="SystemPens.AppWorkspace"]/*' />
        /// <devdoc>
        ///     Pen is the color of the application workspace.  The application workspace
        ///     is the area in a multiple document view that is not being occupied
        ///     by documents.
        /// </devdoc>
        public static Pen AppWorkspace
        {
            get
            {
                return FromSystemColor(SystemColors.AppWorkspace);
            }
        }

        /// <include file='doc\SystemPens.uex' path='docs/doc[@for="SystemPens.ButtonFace"]/*' />
        /// <devdoc>
        ///     Pen for the ButtonFace system color.
        /// </devdoc>
        public static Pen ButtonFace
        {
            get
            {
                return FromSystemColor(SystemColors.ButtonFace);
            }
        }

        /// <include file='doc\SystemPens.uex' path='docs/doc[@for="SystemPens.ButtonHighlight"]/*' />
        /// <devdoc>
        ///     Pen for the ButtonHighlight system color.
        /// </devdoc>
        public static Pen ButtonHighlight
        {
            get
            {
                return FromSystemColor(SystemColors.ButtonHighlight);
            }
        }

        /// <include file='doc\SystemPens.uex' path='docs/doc[@for="SystemPens.ButtonShadow"]/*' />
        /// <devdoc>
        ///     Pen for the ButtonShadow system color.
        /// </devdoc>
        public static Pen ButtonShadow
        {
            get
            {
                return FromSystemColor(SystemColors.ButtonShadow);
            }
        }

        /// <include file='doc\SystemPens.uex' path='docs/doc[@for="SystemPens.Control"]/*' />
        /// <devdoc>
        ///     Pen is the color of a button or control.
        /// </devdoc>
        public static Pen Control
        {
            get
            {
                return FromSystemColor(SystemColors.Control);
            }
        }

        /// <include file='doc\SystemPens.uex' path='docs/doc[@for="SystemPens.ControlText"]/*' />
        /// <devdoc>
        ///     Pen is the color of the text on a button or control.
        /// </devdoc>
        public static Pen ControlText
        {
            get
            {
                return FromSystemColor(SystemColors.ControlText);
            }
        }

        /// <include file='doc\SystemPens.uex' path='docs/doc[@for="SystemPens.ControlDark"]/*' />
        /// <devdoc>
        ///     Pen is the color of the shadow part of a 3D element
        /// </devdoc>
        public static Pen ControlDark
        {
            get
            {
                return FromSystemColor(SystemColors.ControlDark);
            }
        }

        /// <include file='doc\SystemPens.uex' path='docs/doc[@for="SystemPens.ControlDarkDark"]/*' />
        /// <devdoc>
        ///     Pen is the color of the darkest part of a 3D element
        /// </devdoc>
        public static Pen ControlDarkDark
        {
            get
            {
                return FromSystemColor(SystemColors.ControlDarkDark);
            }
        }

        /// <include file='doc\SystemPens.uex' path='docs/doc[@for="SystemPens.ControlLight"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static Pen ControlLight
        {
            get
            {
                return FromSystemColor(SystemColors.ControlLight);
            }
        }

        /// <include file='doc\SystemPens.uex' path='docs/doc[@for="SystemPens.ControlLightLight"]/*' />
        /// <devdoc>
        ///     Pen is the color of the lightest part of a 3D element
        /// </devdoc>
        public static Pen ControlLightLight
        {
            get
            {
                return FromSystemColor(SystemColors.ControlLightLight);
            }
        }

        /// <include file='doc\SystemPens.uex' path='docs/doc[@for="SystemPens.Desktop"]/*' />
        /// <devdoc>
        ///     Pen is the color of the desktop.
        /// </devdoc>
        public static Pen Desktop
        {
            get
            {
                return FromSystemColor(SystemColors.Desktop);
            }
        }

        /// <include file='doc\SystemPens.uex' path='docs/doc[@for="SystemPens.GradientActiveCaption"]/*' />
        /// <devdoc>
        ///     Pen for the GradientActiveCaption system color.
        /// </devdoc>
        public static Pen GradientActiveCaption
        {
            get
            {
                return FromSystemColor(SystemColors.GradientActiveCaption);
            }
        }

        /// <include file='doc\SystemPens.uex' path='docs/doc[@for="SystemPens.GradientInactiveCaption"]/*' />
        /// <devdoc>
        ///     Pen for the GradientInactiveCaption system color.
        /// </devdoc>
        public static Pen GradientInactiveCaption
        {
            get
            {
                return FromSystemColor(SystemColors.GradientInactiveCaption);
            }
        }

        /// <include file='doc\SystemPens.uex' path='docs/doc[@for="SystemPens.GrayText"]/*' />
        /// <devdoc>
        ///     Pen is the color of disabled text.
        /// </devdoc>
        public static Pen GrayText
        {
            get
            {
                return FromSystemColor(SystemColors.GrayText);
            }
        }

        /// <include file='doc\SystemPens.uex' path='docs/doc[@for="SystemPens.Highlight"]/*' />
        /// <devdoc>
        ///     Pen is the color of a highlighted background.
        /// </devdoc>
        public static Pen Highlight
        {
            get
            {
                return FromSystemColor(SystemColors.Highlight);
            }
        }

        /// <include file='doc\SystemPens.uex' path='docs/doc[@for="SystemPens.HighlightText"]/*' />
        /// <devdoc>
        ///     Pen is the color of highlighted text.
        /// </devdoc>
        public static Pen HighlightText
        {
            get
            {
                return FromSystemColor(SystemColors.HighlightText);
            }
        }

        /// <include file='doc\SystemPens.uex' path='docs/doc[@for="SystemPens.HotTrack"]/*' />
        /// <devdoc>
        ///     Pen is the color used to represent hot tracking.
        /// </devdoc>
        public static Pen HotTrack
        {
            get
            {
                return FromSystemColor(SystemColors.HotTrack);
            }
        }

        /// <include file='doc\SystemPens.uex' path='docs/doc[@for="SystemPens.InactiveBorder"]/*' />
        /// <devdoc>
        ///     Pen is the color if an inactive window border.
        /// </devdoc>
        public static Pen InactiveBorder
        {
            get
            {
                return FromSystemColor(SystemColors.InactiveBorder);
            }
        }

        /// <include file='doc\SystemPens.uex' path='docs/doc[@for="SystemPens.InactiveCaption"]/*' />
        /// <devdoc>
        ///     Pen is the color of an inactive caption bar.
        /// </devdoc>
        public static Pen InactiveCaption
        {
            get
            {
                return FromSystemColor(SystemColors.InactiveCaption);
            }
        }

        /// <include file='doc\SystemPens.uex' path='docs/doc[@for="SystemPens.InactiveCaptionText"]/*' />
        /// <devdoc>
        ///     Pen is the color of an inactive window's caption text.
        /// </devdoc>
        public static Pen InactiveCaptionText
        {
            get
            {
                return FromSystemColor(SystemColors.InactiveCaptionText);
            }
        }

        /// <include file='doc\SystemPens.uex' path='docs/doc[@for="SystemPens.Info"]/*' />
        /// <devdoc>
        ///     Pen is the color of the info tooltip's background.
        /// </devdoc>
        public static Pen Info
        {
            get
            {
                return FromSystemColor(SystemColors.Info);
            }
        }

        /// <include file='doc\SystemPens.uex' path='docs/doc[@for="SystemPens.InfoText"]/*' />
        /// <devdoc>
        ///     Pen is the color of the info tooltip's text.
        /// </devdoc>
        public static Pen InfoText
        {
            get
            {
                return FromSystemColor(SystemColors.InfoText);
            }
        }

        /// <include file='doc\SystemPens.uex' path='docs/doc[@for="SystemPens.Menu"]/*' />
        /// <devdoc>
        ///     Pen is the color of the background of a menu.
        /// </devdoc>
        public static Pen Menu
        {
            get
            {
                return FromSystemColor(SystemColors.Menu);
            }
        }

        /// <include file='doc\SystemPens.uex' path='docs/doc[@for="SystemPens.MenuBar"]/*' />
        /// <devdoc>
        ///     Pen is the color of the background of a menu bar.
        /// </devdoc>
        public static Pen MenuBar
        {
            get
            {
                return FromSystemColor(SystemColors.MenuBar);
            }
        }

        /// <include file='doc\SystemPens.uex' path='docs/doc[@for="SystemPens.MenuHighlight"]/*' />
        /// <devdoc>
        ///     Pen for the MenuHighlight system color.
        /// </devdoc>
        public static Pen MenuHighlight
        {
            get
            {
                return FromSystemColor(SystemColors.MenuHighlight);
            }
        }

        /// <include file='doc\SystemPens.uex' path='docs/doc[@for="SystemPens.MenuText"]/*' />
        /// <devdoc>
        ///     Pen is the color of the menu text.
        /// </devdoc>
        public static Pen MenuText
        {
            get
            {
                return FromSystemColor(SystemColors.MenuText);
            }
        }

        /// <include file='doc\SystemPens.uex' path='docs/doc[@for="SystemPens.ScrollBar"]/*' />
        /// <devdoc>
        ///     Pen is the color of the scroll bar area that is not being used by the
        ///     thumb button.
        /// </devdoc>
        public static Pen ScrollBar
        {
            get
            {
                return FromSystemColor(SystemColors.ScrollBar);
            }
        }

        /// <include file='doc\SystemPens.uex' path='docs/doc[@for="SystemPens.Window"]/*' />
        /// <devdoc>
        ///     Pen is the color of the client area of a window.
        /// </devdoc>
        public static Pen Window
        {
            get
            {
                return FromSystemColor(SystemColors.Window);
            }
        }

        /// <include file='doc\SystemPens.uex' path='docs/doc[@for="SystemPens.WindowFrame"]/*' />
        /// <devdoc>
        ///     Pen is the color of the window frame.
        /// </devdoc>
        public static Pen WindowFrame
        {
            get
            {
                return FromSystemColor(SystemColors.WindowFrame);
            }
        }

        /// <include file='doc\SystemPens.uex' path='docs/doc[@for="SystemPens.WindowText"]/*' />
        /// <devdoc>
        ///     Pen is the color of a window's text.
        /// </devdoc>
        public static Pen WindowText
        {
            get
            {
                return FromSystemColor(SystemColors.WindowText);
            }
        }

        /// <include file='doc\SystemPens.uex' path='docs/doc[@for="SystemPens.FromSystemColor"]/*' />
        /// <devdoc>
        ///     Retrieves a pen given a system color.  An error will be raised
        ///     if the color provide is not a system color.
        /// </devdoc>
        public static Pen FromSystemColor(Color c)
        {
            if (!c.IsSystemColor)
            {
                throw new ArgumentException(SR.Format(SR.ColorNotSystemColor, c.ToString()));
            }

            Pen[] systemPens = (Pen[])SafeNativeMethods.Gdip.ThreadData[s_systemPensKey];
            if (systemPens == null)
            {
                systemPens = new Pen[(int)KnownColor.WindowText + (int)KnownColor.MenuHighlight - (int)KnownColor.YellowGreen];
                SafeNativeMethods.Gdip.ThreadData[s_systemPensKey] = systemPens;
            }

            int idx = (int)c.ToKnownColor();
            if (idx > (int)KnownColor.YellowGreen)
            {
                idx -= (int)KnownColor.YellowGreen - (int)KnownColor.WindowText;
            }
            idx--;
            Debug.Assert(idx >= 0 && idx < systemPens.Length, "System colors have been added but our system color array has not been expanded.");

            if (systemPens[idx] == null)
            {
                systemPens[idx] = new Pen(c, true);
            }

            return systemPens[idx];
        }
    }
}

