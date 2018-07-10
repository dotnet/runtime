// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Gdip = System.Drawing.SafeNativeMethods.Gdip;

namespace System.Drawing
{
    public static class SystemPens
    {
        private static readonly object s_systemPensKey = new object();

        public static Pen ActiveBorder => FromSystemColor(SystemColors.ActiveBorder);
        public static Pen ActiveCaption => FromSystemColor(SystemColors.ActiveCaption);
        public static Pen ActiveCaptionText => FromSystemColor(SystemColors.ActiveCaptionText);
        public static Pen AppWorkspace => FromSystemColor(SystemColors.AppWorkspace);

        public static Pen ButtonFace => FromSystemColor(SystemColors.ButtonFace);
        public static Pen ButtonHighlight => FromSystemColor(SystemColors.ButtonHighlight);

        public static Pen ButtonShadow => FromSystemColor(SystemColors.ButtonShadow);

        public static Pen Control => FromSystemColor(SystemColors.Control);
        public static Pen ControlText => FromSystemColor(SystemColors.ControlText);
        public static Pen ControlDark => FromSystemColor(SystemColors.ControlDark);
        public static Pen ControlDarkDark => FromSystemColor(SystemColors.ControlDarkDark);
        public static Pen ControlLight => FromSystemColor(SystemColors.ControlLight);
        public static Pen ControlLightLight => FromSystemColor(SystemColors.ControlLightLight);

        public static Pen Desktop => FromSystemColor(SystemColors.Desktop);

        public static Pen GradientActiveCaption => FromSystemColor(SystemColors.GradientActiveCaption);
        public static Pen GradientInactiveCaption => FromSystemColor(SystemColors.GradientInactiveCaption);
        public static Pen GrayText => FromSystemColor(SystemColors.GrayText);

        public static Pen Highlight => FromSystemColor(SystemColors.Highlight);
        public static Pen HighlightText => FromSystemColor(SystemColors.HighlightText);
        public static Pen HotTrack => FromSystemColor(SystemColors.HotTrack);

        public static Pen InactiveBorder => FromSystemColor(SystemColors.InactiveBorder);
        public static Pen InactiveCaption => FromSystemColor(SystemColors.InactiveCaption);
        public static Pen InactiveCaptionText => FromSystemColor(SystemColors.InactiveCaptionText);
        public static Pen Info => FromSystemColor(SystemColors.Info);
        public static Pen InfoText => FromSystemColor(SystemColors.InfoText);

        public static Pen Menu => FromSystemColor(SystemColors.Menu);
        public static Pen MenuBar => FromSystemColor(SystemColors.MenuBar);
        public static Pen MenuHighlight => FromSystemColor(SystemColors.MenuHighlight);
        public static Pen MenuText => FromSystemColor(SystemColors.MenuText);

        public static Pen ScrollBar => FromSystemColor(SystemColors.ScrollBar);

        public static Pen Window => FromSystemColor(SystemColors.Window);
        public static Pen WindowFrame => FromSystemColor(SystemColors.WindowFrame);
        public static Pen WindowText => FromSystemColor(SystemColors.WindowText);

        public static Pen FromSystemColor(Color c)
        {
            if (!ColorUtil.IsSystemColor(c))
            {
                throw new ArgumentException(SR.Format(SR.ColorNotSystemColor, c.ToString()));
            }

            Pen[] systemPens = (Pen[])Gdip.ThreadData[s_systemPensKey];
            if (systemPens == null)
            {
                systemPens = new Pen[(int)KnownColor.WindowText + (int)KnownColor.MenuHighlight - (int)KnownColor.YellowGreen];
                Gdip.ThreadData[s_systemPensKey] = systemPens;
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

