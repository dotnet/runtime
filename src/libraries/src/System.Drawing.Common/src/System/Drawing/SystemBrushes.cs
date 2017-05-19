// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing
{
    using System.Diagnostics;

    /// <include file='doc\SystemBrushes.uex' path='docs/doc[@for="SystemBrushes"]/*' />
    /// <devdoc>
    ///     Brushes for select Windows system-wide colors.  Whenever possible, try to use
    ///     SystemPens and SystemBrushes rather than SystemColors.
    /// </devdoc>
    public sealed class SystemBrushes
    {
        private static readonly object s_systemBrushesKey = new object();

        private SystemBrushes()
        {
        }

        /// <include file='doc\SystemBrushes.uex' path='docs/doc[@for="SystemBrushes.ActiveBorder"]/*' />
        /// <devdoc>
        ///     Brush is the color of the active window border.
        /// </devdoc>
        public static Brush ActiveBorder
        {
            get
            {
                return FromSystemColor(SystemColors.ActiveBorder);
            }
        }

        /// <include file='doc\SystemBrushes.uex' path='docs/doc[@for="SystemBrushes.ActiveCaption"]/*' />
        /// <devdoc>
        ///     Brush is the color of the active caption bar.
        /// </devdoc>
        public static Brush ActiveCaption
        {
            get
            {
                return FromSystemColor(SystemColors.ActiveCaption);
            }
        }

        /// <include file='doc\SystemBrushes.uex' path='docs/doc[@for="SystemBrushes.ActiveCaptionText"]/*' />
        /// <devdoc>
        ///     Brush is the color of the active caption bar.
        /// </devdoc>
        public static Brush ActiveCaptionText
        {
            get
            {
                return FromSystemColor(SystemColors.ActiveCaptionText);
            }
        }

        /// <include file='doc\SystemBrushes.uex' path='docs/doc[@for="SystemBrushes.AppWorkspace"]/*' />
        /// <devdoc>
        ///     Brush is the color of the app workspace window.
        /// </devdoc>
        public static Brush AppWorkspace
        {
            get
            {
                return FromSystemColor(SystemColors.AppWorkspace);
            }
        }

        /// <include file='doc\SystemBrushes.uex' path='docs/doc[@for="SystemBrushes.ButtonFace"]/*' />
        /// <devdoc>
        ///     Brush for the ButtonFace system color.
        /// </devdoc>
        public static Brush ButtonFace
        {
            get
            {
                return FromSystemColor(SystemColors.ButtonFace);
            }
        }

        /// <include file='doc\SystemBrushes.uex' path='docs/doc[@for="SystemBrushes.ButtonHighlight"]/*' />
        /// <devdoc>
        ///     Brush for the ButtonHighlight system color.
        /// </devdoc>
        public static Brush ButtonHighlight
        {
            get
            {
                return FromSystemColor(SystemColors.ButtonHighlight);
            }
        }

        /// <include file='doc\SystemBrushes.uex' path='docs/doc[@for="SystemBrushes.ButtonShadow"]/*' />
        /// <devdoc>
        ///     Brush for the ButtonShadow system color.
        /// </devdoc>
        public static Brush ButtonShadow
        {
            get
            {
                return FromSystemColor(SystemColors.ButtonShadow);
            }
        }

        /// <include file='doc\SystemBrushes.uex' path='docs/doc[@for="SystemBrushes.Control"]/*' />
        /// <devdoc>
        ///     Brush is the control color, which is the surface color for 3D elements.
        /// </devdoc>
        public static Brush Control
        {
            get
            {
                return FromSystemColor(SystemColors.Control);
            }
        }

        /// <include file='doc\SystemBrushes.uex' path='docs/doc[@for="SystemBrushes.ControlLightLight"]/*' />
        /// <devdoc>
        ///     Brush is the lighest part of a 3D element.
        /// </devdoc>
        public static Brush ControlLightLight
        {
            get
            {
                return FromSystemColor(SystemColors.ControlLightLight);
            }
        }

        /// <include file='doc\SystemBrushes.uex' path='docs/doc[@for="SystemBrushes.ControlLight"]/*' />
        /// <devdoc>
        ///     Brush is the highlight part of a 3D element.
        /// </devdoc>
        public static Brush ControlLight
        {
            get
            {
                return FromSystemColor(SystemColors.ControlLight);
            }
        }

        /// <include file='doc\SystemBrushes.uex' path='docs/doc[@for="SystemBrushes.ControlDark"]/*' />
        /// <devdoc>
        ///     Brush is the shadow part of a 3D element.
        /// </devdoc>
        public static Brush ControlDark
        {
            get
            {
                return FromSystemColor(SystemColors.ControlDark);
            }
        }

        /// <include file='doc\SystemBrushes.uex' path='docs/doc[@for="SystemBrushes.ControlDarkDark"]/*' />
        /// <devdoc>
        ///     Brush is the darkest part of a 3D element.
        /// </devdoc>
        public static Brush ControlDarkDark
        {
            get
            {
                return FromSystemColor(SystemColors.ControlDarkDark);
            }
        }

        /// <include file='doc\SystemBrushes.uex' path='docs/doc[@for="SystemBrushes.ControlText"]/*' />
        /// <devdoc>
        ///     Brush is the color of text on controls.
        /// </devdoc>
        public static Brush ControlText
        {
            get
            {
                return FromSystemColor(SystemColors.ControlText);
            }
        }

        /// <include file='doc\SystemBrushes.uex' path='docs/doc[@for="SystemBrushes.Desktop"]/*' />
        /// <devdoc>
        ///     Brush is the color of the desktop.
        /// </devdoc>
        public static Brush Desktop
        {
            get
            {
                return FromSystemColor(SystemColors.Desktop);
            }
        }

        /// <include file='doc\SystemBrushes.uex' path='docs/doc[@for="SystemBrushes.GradientActiveCaption"]/*' />
        /// <devdoc>
        ///     Brush for the GradientActiveCaption system color.
        /// </devdoc>
        public static Brush GradientActiveCaption
        {
            get
            {
                return FromSystemColor(SystemColors.GradientActiveCaption);
            }
        }

        /// <include file='doc\SystemBrushes.uex' path='docs/doc[@for="SystemBrushes.GradientInactiveCaption"]/*' />
        /// <devdoc>
        ///     Brush for the GradientInactiveCaption system color.
        /// </devdoc>
        public static Brush GradientInactiveCaption
        {
            get
            {
                return FromSystemColor(SystemColors.GradientInactiveCaption);
            }
        }

        /// <include file='doc\SystemBrushes.uex' path='docs/doc[@for="SystemBrushes.GrayText"]/*' />
        /// <devdoc>
        ///     Brush for the GrayText system color.
        /// </devdoc>
        public static Brush GrayText
        {
            get
            {
                return FromSystemColor(SystemColors.GrayText);
            }
        }

        /// <include file='doc\SystemBrushes.uex' path='docs/doc[@for="SystemBrushes.Highlight"]/*' />
        /// <devdoc>
        ///     Brush is the color of the background of highlighted elements.
        /// </devdoc>
        public static Brush Highlight
        {
            get
            {
                return FromSystemColor(SystemColors.Highlight);
            }
        }

        /// <include file='doc\SystemBrushes.uex' path='docs/doc[@for="SystemBrushes.HighlightText"]/*' />
        /// <devdoc>
        ///     Brush is the color of the foreground of highlighted elements.
        /// </devdoc>
        public static Brush HighlightText
        {
            get
            {
                return FromSystemColor(SystemColors.HighlightText);
            }
        }

        /// <include file='doc\SystemBrushes.uex' path='docs/doc[@for="SystemBrushes.HotTrack"]/*' />
        /// <devdoc>
        ///     Brush is the color used to represent hot tracking.
        /// </devdoc>
        public static Brush HotTrack
        {
            get
            {
                return FromSystemColor(SystemColors.HotTrack);
            }
        }

        /// <include file='doc\SystemBrushes.uex' path='docs/doc[@for="SystemBrushes.InactiveCaption"]/*' />
        /// <devdoc>
        ///     Brush is the color of an inactive caption bar.
        /// </devdoc>
        public static Brush InactiveCaption
        {
            get
            {
                return FromSystemColor(SystemColors.InactiveCaption);
            }
        }

        /// <include file='doc\SystemBrushes.uex' path='docs/doc[@for="SystemBrushes.InactiveBorder"]/*' />
        /// <devdoc>
        ///     Brush is the color if an inactive window border.
        /// </devdoc>
        public static Brush InactiveBorder
        {
            get
            {
                return FromSystemColor(SystemColors.InactiveBorder);
            }
        }

        /// <include file='doc\SystemBrushes.uex' path='docs/doc[@for="SystemBrushes.InactiveCaptionText"]/*' />
        /// <devdoc>
        ///     Brush is the color of an inactive caption text.
        /// </devdoc>
        public static Brush InactiveCaptionText
        {
            get
            {
                return FromSystemColor(SystemColors.InactiveCaptionText);
            }
        }

        /// <include file='doc\SystemBrushes.uex' path='docs/doc[@for="SystemBrushes.Info"]/*' />
        /// <devdoc>
        ///     Brush is the color of the background of the info tooltip.
        /// </devdoc>
        public static Brush Info
        {
            get
            {
                return FromSystemColor(SystemColors.Info);
            }
        }

        /// <include file='doc\SystemBrushes.uex' path='docs/doc[@for="SystemBrushes.InfoText"]/*' />
        /// <devdoc>
        ///     Brush is the color of the info tooltip's text.
        /// </devdoc>
        public static Brush InfoText
        {
            get
            {
                return FromSystemColor(SystemColors.InfoText);
            }
        }

        /// <include file='doc\SystemBrushes.uex' path='docs/doc[@for="SystemBrushes.Menu"]/*' />
        /// <devdoc>
        ///     Brush is the color of the menu background.
        /// </devdoc>
        public static Brush Menu
        {
            get
            {
                return FromSystemColor(SystemColors.Menu);
            }
        }

        /// <include file='doc\SystemBrushes.uex' path='docs/doc[@for="SystemBrushes.MenuBar"]/*' />
        /// <devdoc>
        ///     Brush is the color of the menu background.
        /// </devdoc>
        public static Brush MenuBar
        {
            get
            {
                return FromSystemColor(SystemColors.MenuBar);
            }
        }

        /// <include file='doc\SystemBrushes.uex' path='docs/doc[@for="SystemBrushes.MenuHighlight"]/*' />
        /// <devdoc>
        ///     Brush for the MenuHighlight system color.
        /// </devdoc>
        public static Brush MenuHighlight
        {
            get
            {
                return FromSystemColor(SystemColors.MenuHighlight);
            }
        }

        /// <include file='doc\SystemBrushes.uex' path='docs/doc[@for="SystemBrushes.MenuText"]/*' />
        /// <devdoc>
        ///     Brush is the color of the menu text.
        /// </devdoc>
        public static Brush MenuText
        {
            get
            {
                return FromSystemColor(SystemColors.MenuText);
            }
        }

        /// <include file='doc\SystemBrushes.uex' path='docs/doc[@for="SystemBrushes.ScrollBar"]/*' />
        /// <devdoc>
        ///     Brush is the color of the scroll bar area that is not being used by the
        ///     thumb button.
        /// </devdoc>
        public static Brush ScrollBar
        {
            get
            {
                return FromSystemColor(SystemColors.ScrollBar);
            }
        }

        /// <include file='doc\SystemBrushes.uex' path='docs/doc[@for="SystemBrushes.Window"]/*' />
        /// <devdoc>
        ///     Brush is the color of the window background.
        /// </devdoc>
        public static Brush Window
        {
            get
            {
                return FromSystemColor(SystemColors.Window);
            }
        }

        /// <include file='doc\SystemBrushes.uex' path='docs/doc[@for="SystemBrushes.WindowFrame"]/*' />
        /// <devdoc>
        ///     Brush is the color of the thin frame drawn around a window.
        /// </devdoc>
        public static Brush WindowFrame
        {
            get
            {
                return FromSystemColor(SystemColors.WindowFrame);
            }
        }

        /// <include file='doc\SystemBrushes.uex' path='docs/doc[@for="SystemBrushes.WindowText"]/*' />
        /// <devdoc>
        ///     Brush is the color of text on controls.
        /// </devdoc>
        public static Brush WindowText
        {
            get
            {
                return FromSystemColor(SystemColors.WindowText);
            }
        }

        /// <include file='doc\SystemBrushes.uex' path='docs/doc[@for="SystemBrushes.FromSystemColor"]/*' />
        /// <devdoc>
        ///     Retrieves a brush given a system color.  An error will be raised
        ///     if the color provide is not a system color.
        /// </devdoc>
        public static Brush FromSystemColor(Color c)
        {
            if (!c.IsSystemColor)
            {
                throw new ArgumentException(SR.Format(SR.ColorNotSystemColor, c.ToString()));
            }

            Brush[] systemBrushes = (Brush[])SafeNativeMethods.Gdip.ThreadData[s_systemBrushesKey];
            if (systemBrushes == null)
            {
                systemBrushes = new Brush[(int)KnownColor.WindowText + (int)KnownColor.MenuHighlight - (int)KnownColor.YellowGreen];
                SafeNativeMethods.Gdip.ThreadData[s_systemBrushesKey] = systemBrushes;
            }
            int idx = (int)c.ToKnownColor();
            if (idx > (int)KnownColor.YellowGreen)
            {
                idx -= (int)KnownColor.YellowGreen - (int)KnownColor.WindowText;
            }
            idx--;

            Debug.Assert(idx >= 0 && idx < systemBrushes.Length, "System colors have been added but our system color array has not been expanded.");

            if (systemBrushes[idx] == null)
            {
                systemBrushes[idx] = new SolidBrush(c, true);
            }

            return systemBrushes[idx];
        }
    }
}

