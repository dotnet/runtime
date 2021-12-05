// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Gdip = System.Drawing.SafeNativeMethods.Gdip;

namespace System.Drawing.Imaging
{
    // sdkinc\imaging.h
    public sealed class ImageCodecInfo
    {
        private Guid _clsid;
        private Guid _formatID;
        private string? _codecName;
        private string? _dllName;
        private string? _formatDescription;
        private string? _filenameExtension;
        private string? _mimeType;
        private ImageCodecFlags _flags;
        private int _version;
        private byte[][]? _signaturePatterns;
        private byte[][]? _signatureMasks;

        internal ImageCodecInfo()
        {
        }

        public Guid Clsid
        {
            get { return _clsid; }
            set { _clsid = value; }
        }

        public Guid FormatID
        {
            get { return _formatID; }
            set { _formatID = value; }
        }

        public string? CodecName
        {
            get { return _codecName; }
            set { _codecName = value; }
        }

        public string? DllName
        {
            get
            {
                return _dllName;
            }
            set
            {
                _dllName = value;
            }
        }

        public string? FormatDescription
        {
            get { return _formatDescription; }
            set { _formatDescription = value; }
        }

        public string? FilenameExtension
        {
            get { return _filenameExtension; }
            set { _filenameExtension = value; }
        }

        public string? MimeType
        {
            get { return _mimeType; }
            set { _mimeType = value; }
        }

        public ImageCodecFlags Flags
        {
            get { return _flags; }
            set { _flags = value; }
        }

        public int Version
        {
            get { return _version; }
            set { _version = value; }
        }

        [CLSCompliant(false)]
        public byte[][]? SignaturePatterns
        {
            get { return _signaturePatterns; }
            set { _signaturePatterns = value; }
        }

        [CLSCompliant(false)]
        public byte[][]? SignatureMasks
        {
            get { return _signatureMasks; }
            set { _signatureMasks = value; }
        }

        // Encoder/Decoder selection APIs

        public static ImageCodecInfo[] GetImageDecoders()
        {
            ImageCodecInfo[] imageCodecs;
            int numDecoders;
            int size;

            int status = Gdip.GdipGetImageDecodersSize(out numDecoders, out size);

            if (status != Gdip.Ok)
            {
                throw Gdip.StatusException(status);
            }

            IntPtr memory = Marshal.AllocHGlobal(size);

            try
            {
                status = Gdip.GdipGetImageDecoders(numDecoders, size, memory);

                if (status != Gdip.Ok)
                {
                    throw Gdip.StatusException(status);
                }

                imageCodecs = ImageCodecInfo.ConvertFromMemory(memory, numDecoders);
            }
            finally
            {
                Marshal.FreeHGlobal(memory);
            }

            return imageCodecs;
        }

        public static ImageCodecInfo[] GetImageEncoders()
        {
            ImageCodecInfo[] imageCodecs;
            int numEncoders;
            int size;

            int status = Gdip.GdipGetImageEncodersSize(out numEncoders, out size);

            if (status != Gdip.Ok)
            {
                throw Gdip.StatusException(status);
            }

            IntPtr memory = Marshal.AllocHGlobal(size);

            try
            {
                status = Gdip.GdipGetImageEncoders(numEncoders, size, memory);

                if (status != Gdip.Ok)
                {
                    throw Gdip.StatusException(status);
                }

                imageCodecs = ImageCodecInfo.ConvertFromMemory(memory, numEncoders);
            }
            finally
            {
                Marshal.FreeHGlobal(memory);
            }

            return imageCodecs;
        }

        private static unsafe ImageCodecInfo[] ConvertFromMemory(IntPtr memoryStart, int numCodecs)
        {
            ImageCodecInfo[] codecs = new ImageCodecInfo[numCodecs];

            int index;

            for (index = 0; index < numCodecs; index++)
            {
                ref readonly ImageCodecInfoPrivate codecp = ref ((ImageCodecInfoPrivate*)memoryStart)[index];

                var codec = new ImageCodecInfo();
                codec.Clsid = codecp.Clsid;
                codec.FormatID = codecp.FormatID;
                codec.CodecName = Marshal.PtrToStringUni(codecp.CodecName);
                codec.DllName = Marshal.PtrToStringUni(codecp.DllName);
                codec.FormatDescription = Marshal.PtrToStringUni(codecp.FormatDescription);
                codec.FilenameExtension = Marshal.PtrToStringUni(codecp.FilenameExtension);
                codec.MimeType = Marshal.PtrToStringUni(codecp.MimeType);

                codec.Flags = (ImageCodecFlags)codecp.Flags;
                codec.Version = (int)codecp.Version;

                codec.SignaturePatterns = new byte[codecp.SigCount][];
                codec.SignatureMasks = new byte[codecp.SigCount][];

                for (int j = 0; j < codecp.SigCount; j++)
                {
                    codec.SignaturePatterns[j] = new ReadOnlySpan<byte>((byte*)codecp.SigPattern + j * codecp.SigSize, codecp.SigSize).ToArray();
                    codec.SignatureMasks[j] = new ReadOnlySpan<byte>((byte*)codecp.SigMask + j * codecp.SigSize, codecp.SigSize).ToArray();
                }

                codecs[index] = codec;
            }

            return codecs;
        }
    }
}
