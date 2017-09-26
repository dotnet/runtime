// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace System.Drawing
{
    public static partial class SystemFonts
    {
        public static Font CaptionFont
        {
            get
            {
                Font captionFont = null;

                var data = new NativeMethods.NONCLIENTMETRICS();
                bool result = UnsafeNativeMethods.SystemParametersInfo(NativeMethods.SPI_GETNONCLIENTMETRICS, data.cbSize, data, 0);

                if (result)
                {
                    captionFont = GetFontFromData(data.lfCaptionFont);
                }

                captionFont.SetSystemFontName(nameof(CaptionFont));
                return captionFont;
            }
        }

        public static Font SmallCaptionFont
        {
            get
            {
                Font smcaptionFont = null;

                var data = new NativeMethods.NONCLIENTMETRICS();
                bool result = UnsafeNativeMethods.SystemParametersInfo(NativeMethods.SPI_GETNONCLIENTMETRICS, data.cbSize, data, 0);

                if (result)
                {
                    smcaptionFont = GetFontFromData(data.lfSmCaptionFont);
                }

                smcaptionFont.SetSystemFontName(nameof(SmallCaptionFont));
                return smcaptionFont;
            }
        }

        public static Font MenuFont
        {
            get
            {
                Font menuFont = null;

                var data = new NativeMethods.NONCLIENTMETRICS();
                bool result = UnsafeNativeMethods.SystemParametersInfo(NativeMethods.SPI_GETNONCLIENTMETRICS, data.cbSize, data, 0);

                if (result)
                {
                    menuFont = GetFontFromData(data.lfMenuFont);
                }

                menuFont.SetSystemFontName(nameof(MenuFont));
                return menuFont;
            }
        }

        public static Font StatusFont
        {
            get
            {
                Font statusFont = null;

                var data = new NativeMethods.NONCLIENTMETRICS();
                bool result = UnsafeNativeMethods.SystemParametersInfo(NativeMethods.SPI_GETNONCLIENTMETRICS, data.cbSize, data, 0);

                if (result)
                {
                    statusFont = GetFontFromData(data.lfStatusFont);
                }

                statusFont.SetSystemFontName(nameof(StatusFont));
                return statusFont;
            }
        }

        public static Font MessageBoxFont
        {
            get
            {
                Font messageBoxFont = null;

                var data = new NativeMethods.NONCLIENTMETRICS();
                bool result = UnsafeNativeMethods.SystemParametersInfo(NativeMethods.SPI_GETNONCLIENTMETRICS, data.cbSize, data, 0);

                if (result)
                {
                    messageBoxFont = GetFontFromData(data.lfMessageFont);
                }

                messageBoxFont.SetSystemFontName(nameof(MessageBoxFont));
                return messageBoxFont;
            }
        }

        private static bool IsCriticalFontException(Exception ex)
        {
            return !(
                // In any of these cases we'll handle the exception.
                ex is ExternalException ||
                ex is ArgumentException ||
                ex is OutOfMemoryException || // GDI+ throws this one for many reasons other than actual OOM.
                ex is InvalidOperationException ||
                ex is NotImplementedException ||
                ex is FileNotFoundException);
        }

        public static Font IconTitleFont
        {
            get
            {
                Font iconTitleFont = null;

                var itfont = new SafeNativeMethods.LOGFONT();
                bool result = UnsafeNativeMethods.SystemParametersInfo(NativeMethods.SPI_GETICONTITLELOGFONT, Marshal.SizeOf(itfont), itfont, 0);

                if (result)
                {
                    iconTitleFont = GetFontFromData(itfont);
                }

                iconTitleFont.SetSystemFontName(nameof(IconTitleFont));
                return iconTitleFont;
            }
        }

        public static Font DefaultFont
        {
            get
            {
                Font defaultFont = null;
                
                // For Arabic systems, always return Tahoma 8.
                bool systemDefaultLCIDIsArabic = (UnsafeNativeMethods.GetSystemDefaultLCID() & 0x3ff) == 0x0001;
                if (systemDefaultLCIDIsArabic)
                {
                    try
                    {
                        defaultFont = new Font("Tahoma", 8);
                    }
                    catch (Exception ex) when (!IsCriticalFontException(ex)) { }
                }
    
                // First try DEFAULT_GUI.
                if (defaultFont == null)
                {
                    IntPtr handle = UnsafeNativeMethods.GetStockObject(NativeMethods.DEFAULT_GUI_FONT);
                    try
                    {
                        using (Font fontInWorldUnits = Font.FromHfont(handle))
                        {
                            defaultFont = FontInPoints(fontInWorldUnits);
                        }
                    }
                    catch (ArgumentException)
                    {
                    }
                }

                // If DEFAULT_GUI didn't work, try Tahoma.
                if (defaultFont == null)
                {
                    try
                    {
                        defaultFont = new Font("Tahoma", 8);
                    }
                    catch (ArgumentException)
                    {
                    }
                }

                // Use GenericSansSerif as a last resort - this will always work.
                if (defaultFont == null)
                {
                    defaultFont = new Font(FontFamily.GenericSansSerif, 8);
                }

                if (defaultFont.Unit != GraphicsUnit.Point)
                {
                    defaultFont = FontInPoints(defaultFont);
                }

                Debug.Assert(defaultFont != null, "defaultFont wasn't set.");

                defaultFont.SetSystemFontName(nameof(DefaultFont));
                return defaultFont;
            }
        }

        public static Font DialogFont
        {
            get
            {
                Font dialogFont = null;

                if ((UnsafeNativeMethods.GetSystemDefaultLCID() & 0x3ff) == 0x0011)
                {
                    // Always return DefaultFont for Japanese cultures.
                    dialogFont = DefaultFont;
                }
                else
                {
                    try
                    {
                        // Use MS Shell Dlg 2, 8pt for anything other than than Japanese.
                        dialogFont = new Font("MS Shell Dlg 2", 8);
                    }
                    catch (ArgumentException)
                    {
                    }
                }

                if (dialogFont == null)
                {
                    dialogFont = DefaultFont;
                }
                else if (dialogFont.Unit != GraphicsUnit.Point)
                {
                    dialogFont = FontInPoints(dialogFont);
                }

                // For Japanese cultures, SystemFonts.DefaultFont returns a new Font object every time it is invoked.
                // So for Japanese we return the DefaultFont with its SystemFontName set to DialogFont.
                dialogFont.SetSystemFontName(nameof(DialogFont));
                return dialogFont;
            }
        }

        private static Font FontInPoints(Font font)
        {
            return new Font(font.FontFamily, font.SizeInPoints, font.Style, GraphicsUnit.Point, font.GdiCharSet, font.GdiVerticalFont);
        }

        private static Font GetFontFromData(SafeNativeMethods.LOGFONT logFont)
        {
            if (logFont == null)
            {
                return null;
            }

            Font font = null;
            try
            {
                font = Font.FromLogFont(logFont);
            }
            catch (Exception ex) when (!IsCriticalFontException(ex)) { }

            return
                font == null ? DefaultFont :
                font.Unit != GraphicsUnit.Point ? FontInPoints(font) :
                font;
        }
    }
}
