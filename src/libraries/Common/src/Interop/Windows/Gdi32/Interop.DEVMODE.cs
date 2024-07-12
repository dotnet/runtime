// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
#if NET
using System.Runtime.InteropServices.Marshalling;
#endif

internal static partial class Interop
{
    internal static partial class Gdi32
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public sealed class DEVMODE
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string? dmDeviceName;
            public short dmSpecVersion;
            public short dmDriverVersion;
            public short dmSize;
            public short dmDriverExtra;
            public int dmFields;
            public short dmOrientation;
            public short dmPaperSize;
            public short dmPaperLength;
            public short dmPaperWidth;
            public short dmScale;
            public short dmCopies;
            public short dmDefaultSource;
            public short dmPrintQuality;
            public short dmColor;
            public short dmDuplex;
            public short dmYResolution;
            public short dmTTOption;
            public short dmCollate;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string? dmFormName;
            public short dmLogPixels;
            public int dmBitsPerPel;
            public int dmPelsWidth;
            public int dmPelsHeight;
            public int dmDisplayFlags;
            public int dmDisplayFrequency;
            public int dmICMMethod;
            public int dmICMIntent;
            public int dmMediaType;
            public int dmDitherType;
            public int dmICCManufacturer;
            public int dmICCModel;
            public int dmPanningWidth;
            public int dmPanningHeight;


            public override string ToString()
            {
                return "[DEVMODE: "
                + "dmDeviceName=" + dmDeviceName
                + ", dmSpecVersion=" + dmSpecVersion
                + ", dmDriverVersion=" + dmDriverVersion
                + ", dmSize=" + dmSize
                + ", dmDriverExtra=" + dmDriverExtra
                + ", dmFields=" + dmFields
                + ", dmOrientation=" + dmOrientation
                + ", dmPaperSize=" + dmPaperSize
                + ", dmPaperLength=" + dmPaperLength
                + ", dmPaperWidth=" + dmPaperWidth
                + ", dmScale=" + dmScale
                + ", dmCopies=" + dmCopies
                + ", dmDefaultSource=" + dmDefaultSource
                + ", dmPrintQuality=" + dmPrintQuality
                + ", dmColor=" + dmColor
                + ", dmDuplex=" + dmDuplex
                + ", dmYResolution=" + dmYResolution
                + ", dmTTOption=" + dmTTOption
                + ", dmCollate=" + dmCollate
                + ", dmFormName=" + dmFormName
                + ", dmLogPixels=" + dmLogPixels
                + ", dmBitsPerPel=" + dmBitsPerPel
                + ", dmPelsWidth=" + dmPelsWidth
                + ", dmPelsHeight=" + dmPelsHeight
                + ", dmDisplayFlags=" + dmDisplayFlags
                + ", dmDisplayFrequency=" + dmDisplayFrequency
                + ", dmICMMethod=" + dmICMMethod
                + ", dmICMIntent=" + dmICMIntent
                + ", dmMediaType=" + dmMediaType
                + ", dmDitherType=" + dmDitherType
                + ", dmICCManufacturer=" + dmICCManufacturer
                + ", dmICCModel=" + dmICCModel
                + ", dmPanningWidth=" + dmPanningWidth
                + ", dmPanningHeight=" + dmPanningHeight
                + "]";
            }
        }
    }
}
