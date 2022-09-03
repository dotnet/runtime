// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class User32
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public unsafe struct LOGFONT
        {
            private const int LF_FACESIZE = 32;

            public int lfHeight;
            public int lfWidth;
            public int lfEscapement;
            public int lfOrientation;
            public int lfWeight;
            public byte lfItalic;
            public byte lfUnderline;
            public byte lfStrikeOut;
            public byte lfCharSet;
            public byte lfOutPrecision;
            public byte lfClipPrecision;
            public byte lfQuality;
            public byte lfPitchAndFamily;
            private fixed char _lfFaceName[LF_FACESIZE];
            public Span<char> lfFaceName => MemoryMarshal.CreateSpan(ref _lfFaceName[0], LF_FACESIZE);

            public override string ToString()
            {
                return
                    "lfHeight=" + lfHeight + ", " +
                    "lfWidth=" + lfWidth + ", " +
                    "lfEscapement=" + lfEscapement + ", " +
                    "lfOrientation=" + lfOrientation + ", " +
                    "lfWeight=" + lfWeight + ", " +
                    "lfItalic=" + lfItalic + ", " +
                    "lfUnderline=" + lfUnderline + ", " +
                    "lfStrikeOut=" + lfStrikeOut + ", " +
                    "lfCharSet=" + lfCharSet + ", " +
                    "lfOutPrecision=" + lfOutPrecision + ", " +
                    "lfClipPrecision=" + lfClipPrecision + ", " +
                    "lfQuality=" + lfQuality + ", " +
                    "lfPitchAndFamily=" + lfPitchAndFamily + ", " +
                    "lfFaceName=" + lfFaceName.ToString();
            }
        }
    }
}
