// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Drawing.Imaging
{
    using System.Runtime.InteropServices;

    // sdkinc\imaging.h
    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    internal struct ImageCodecInfoPrivate
    {
        public Guid Clsid;

        public Guid FormatID;

        public IntPtr CodecName;
        public IntPtr DllName;
        public IntPtr FormatDescription;
        public IntPtr FilenameExtension;
        public IntPtr MimeType;

        public int Flags;
        public int Version;
        public int SigCount;
        public int SigSize;

        public IntPtr SigPattern;
        public IntPtr SigMask;
    }
}
