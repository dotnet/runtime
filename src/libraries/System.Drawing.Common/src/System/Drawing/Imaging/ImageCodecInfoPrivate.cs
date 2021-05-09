// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Drawing.Imaging
{
    using System.Runtime.InteropServices;

    // sdkinc\imaging.h
    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    internal sealed class ImageCodecInfoPrivate
    {
#pragma warning disable CS0618 // Legacy code: We don't care about using obsolete API's.
        [MarshalAs(UnmanagedType.Struct)]
#pragma warning restore CS0618
        public Guid Clsid;
#pragma warning disable CS0618 // Legacy code: We don't care about using obsolete API's.
        [MarshalAs(UnmanagedType.Struct)]
#pragma warning restore CS0618
        public Guid FormatID;

        public IntPtr CodecName = IntPtr.Zero;
        public IntPtr DllName = IntPtr.Zero;
        public IntPtr FormatDescription = IntPtr.Zero;
        public IntPtr FilenameExtension = IntPtr.Zero;
        public IntPtr MimeType = IntPtr.Zero;

        public int Flags;
        public int Version;
        public int SigCount;
        public int SigSize;

        public IntPtr SigPattern = IntPtr.Zero;
        public IntPtr SigMask = IntPtr.Zero;
    }
}
