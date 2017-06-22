// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace System.Drawing
{
    public static class SystemFonts
    {
        private static readonly object s_systemFontsKey = new object();

        public static Font CaptionFont
        {
            get
            {
                Font captionFont = null;

                var data = new NativeMethods.NONCLIENTMETRICS();
                bool result = UnsafeNativeMethods.SystemParametersInfo(NativeMethods.SPI_GETNONCLIENTMETRICS, data.cbSize, data, 0);

                if (result && data.lfCaptionFont != null)
                {
                    try
                    {
                        captionFont = Font.FromLogFont(data.lfCaptionFont);
                    }
                    catch (Exception ex)
                    {
                        if (IsCriticalFontException(ex))
                        {
                            throw;
                        }
                    }

                    if (captionFont == null)
                    {
                        captionFont = DefaultFont;
                    }
                    else if (captionFont.Unit != GraphicsUnit.Point)
                    {
                        captionFont = FontInPoints(captionFont);
                    }
                }

                captionFont.SetSystemFontName("CaptionFont");
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

                if (result && data.lfSmCaptionFont != null)
                {
                    try
                    {
                        smcaptionFont = Font.FromLogFont(data.lfSmCaptionFont);
                    }
                    catch (Exception ex)
                    {
                        if (IsCriticalFontException(ex))
                        {
                            throw;
                        }
                    }

                    if (smcaptionFont == null)
                    {
                        smcaptionFont = DefaultFont;
                    }
                    else if (smcaptionFont.Unit != GraphicsUnit.Point)
                    {
                        smcaptionFont = FontInPoints(smcaptionFont);
                    }
                }

                smcaptionFont.SetSystemFontName("SmallCaptionFont");
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

                if (result && data.lfMenuFont != null)
                {
                    try
                    {
                        menuFont = Font.FromLogFont(data.lfMenuFont);
                    }
                    catch (Exception ex)
                    {
                        if (IsCriticalFontException(ex))
                        {
                            throw;
                        }
                    }

                    if (menuFont == null)
                    {
                        menuFont = DefaultFont;
                    }
                    else if (menuFont.Unit != GraphicsUnit.Point)
                    {
                        menuFont = FontInPoints(menuFont);
                    }
                }

                menuFont.SetSystemFontName("MenuFont");
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

                if (result && data.lfStatusFont != null)
                {
                    try
                    {
                        statusFont = Font.FromLogFont(data.lfStatusFont);
                    }
                    catch (Exception ex)
                    {
                        if (IsCriticalFontException(ex))
                        {
                            throw;
                        }
                    }

                    if (statusFont == null)
                    {
                        statusFont = DefaultFont;
                    }
                    else if (statusFont.Unit != GraphicsUnit.Point)
                    {
                        statusFont = FontInPoints(statusFont);
                    }
                }

                statusFont.SetSystemFontName("StatusFont");
                return statusFont;
            }
        }

        public static Font MessageBoxFont
        {
            get
            {
                Font messageboxFont = null;

                var data = new NativeMethods.NONCLIENTMETRICS();
                bool result = UnsafeNativeMethods.SystemParametersInfo(NativeMethods.SPI_GETNONCLIENTMETRICS, data.cbSize, data, 0);

                if (result && data.lfMessageFont != null)
                {
                    try
                    {
                        messageboxFont = Font.FromLogFont(data.lfMessageFont);
                    }
                    catch (Exception ex)
                    {
                        if (IsCriticalFontException(ex))
                        {
                            throw;
                        }
                    }

                    if (messageboxFont == null)
                    {
                        messageboxFont = DefaultFont;
                    }
                    else if (messageboxFont.Unit != GraphicsUnit.Point)
                    {
                        messageboxFont = FontInPoints(messageboxFont);
                    }
                }

                messageboxFont.SetSystemFontName("MessageBoxFont");
                return messageboxFont;
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
                Font icontitleFont = null;

                var itfont = new SafeNativeMethods.LOGFONT();
                bool result = UnsafeNativeMethods.SystemParametersInfo(NativeMethods.SPI_GETICONTITLELOGFONT, Marshal.SizeOf(itfont), itfont, 0);

                if (result && itfont != null)
                {
                    try
                    {
                        icontitleFont = Font.FromLogFont(itfont);
                    }
                    catch (Exception ex)
                    {
                        if (IsCriticalFontException(ex))
                        {
                            throw;
                        }
                    }

                    if (icontitleFont == null)
                    {
                        icontitleFont = DefaultFont;
                    }
                    else if (icontitleFont.Unit != GraphicsUnit.Point)
                    {
                        icontitleFont = FontInPoints(icontitleFont);
                    }
                }

                icontitleFont.SetSystemFontName("IconTitleFont");
                return icontitleFont;
            }
        }

        public static Font DefaultFont
        {
            get
            {
                Font defaultFont = null;

                //special case defaultfont for arabic systems too
                bool systemDefaultLCIDIsArabic = false;

                // For Japanese on Win9x get the MS UI Gothic font
                if (Environment.OSVersion.Platform == System.PlatformID.Win32NT &&
                    Environment.OSVersion.Version.Major <= 4)
                {
                    if ((UnsafeNativeMethods.GetSystemDefaultLCID() & 0x3ff) == 0x0011)
                    {
                        try
                        {
                            defaultFont = new Font("MS UI Gothic", 9);
                        }
                        //fall through here if this fails and we'll get the default
                        //font via the DEFAULT_GUI method
                        catch (Exception ex)
                        {
                            if (IsCriticalFontException(ex))
                            {
                                throw;
                            }
                        }
                    }
                }

                if (defaultFont == null)
                {
                    systemDefaultLCIDIsArabic = ((UnsafeNativeMethods.GetSystemDefaultLCID() & 0x3ff) == 0x0001);
                }

                // For arabic systems, regardless of the platform, always return Tahoma 8.
                if (systemDefaultLCIDIsArabic)
                {
                    Debug.Assert(defaultFont == null);
                    // Try Tahoma 8.
                    try
                    {
                        defaultFont = new Font("Tahoma", 8);
                    }
                    catch (Exception ex)
                    {
                        if (IsCriticalFontException(ex))
                        {
                            throw;
                        }
                    }
                }

                //
                // Neither Japanese on Win9x nor Arabic.
                // First try DEFAULT_GUI, then Tahoma 8, then GenericSansSerif 8.
                //

                // first, try DEFAULT_GUI font.
                //
                if (defaultFont == null)
                {
                    IntPtr handle = UnsafeNativeMethods.GetStockObject(NativeMethods.DEFAULT_GUI_FONT);
                    try
                    {
                        Font fontInWorldUnits = Font.FromHfont(handle);

                        try
                        {
                            defaultFont = FontInPoints(fontInWorldUnits);
                        }
                        finally
                        {
                            fontInWorldUnits.Dispose();
                        }
                    }
                    catch (ArgumentException)
                    {
                    }
                }

                // If DEFAULT_GUI didn't work, we try Tahoma.
                //
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

                // Last resort, we use the GenericSansSerif - this will
                // always work.
                //
                if (defaultFont == null)
                {
                    defaultFont = new Font(FontFamily.GenericSansSerif, 8);
                }

                if (defaultFont.Unit != GraphicsUnit.Point)
                {
                    defaultFont = FontInPoints(defaultFont);
                }

                Debug.Assert(defaultFont != null, "defaultFont wasn't set!");

                defaultFont.SetSystemFontName("DefaultFont");
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
                    // for JAPANESE culture always return DefaultFont
                    dialogFont = DefaultFont;
                }
                else if (Environment.OSVersion.Platform == System.PlatformID.Win32Windows)
                {
                    // use DefaultFont for Win9X
                    dialogFont = DefaultFont;
                }
                else
                {
                    try
                    {
                        // use MS Shell Dlg 2, 8pt for anything else than Japanese and Win9x
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

                //
                // JAPANESE or Win9x: SystemFonts.DefaultFont returns a new Font object every time it is invoked.
                // So for JAPANESE or Win9x we return the DefaultFont w/ its SystemFontName set to DialogFont.
                //
                dialogFont.SetSystemFontName("DialogFont");
                return dialogFont;
            }
        }

        private static Font FontInPoints(Font font)
        {
            return new Font(font.FontFamily, font.SizeInPoints, font.Style, GraphicsUnit.Point, font.GdiCharSet, font.GdiVerticalFont);
        }

        public static Font GetFontByName(string systemFontName)
        {
            if ("CaptionFont".Equals(systemFontName))
            {
                return CaptionFont;
            }
            else if ("DefaultFont".Equals(systemFontName))
            {
                return DefaultFont;
            }
            else if ("DialogFont".Equals(systemFontName))
            {
                return DialogFont;
            }
            else if ("IconTitleFont".Equals(systemFontName))
            {
                return IconTitleFont;
            }
            else if ("MenuFont".Equals(systemFontName))
            {
                return MenuFont;
            }
            else if ("MessageBoxFont".Equals(systemFontName))
            {
                return MessageBoxFont;
            }
            else if ("SmallCaptionFont".Equals(systemFontName))
            {
                return SmallCaptionFont;
            }
            else if ("StatusFont".Equals(systemFontName))
            {
                return StatusFont;
            }
            else
            {
                return null;
            }
        }
    }
}

