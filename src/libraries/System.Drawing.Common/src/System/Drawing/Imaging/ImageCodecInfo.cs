// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace System.Drawing.Imaging
{
    // sdkinc\imaging.h
    public sealed class ImageCodecInfo
    {
        private Guid _clsid;
        private Guid _formatID;
        private string _codecName;
        private string _dllName;
        private string _formatDescription;
        private string _filenameExtension;
        private string _mimeType;
        private ImageCodecFlags _flags;
        private int _version;
        private byte[][] _signaturePatterns;
        private byte[][] _signatureMasks;

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

        public string CodecName
        {
            get { return _codecName; }
            set { _codecName = value; }
        }

        public string DllName
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

        public string FormatDescription
        {
            get { return _formatDescription; }
            set { _formatDescription = value; }
        }

        public string FilenameExtension
        {
            get { return _filenameExtension; }
            set { _filenameExtension = value; }
        }

        public string MimeType
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
        public byte[][] SignaturePatterns
        {
            get { return _signaturePatterns; }
            set { _signaturePatterns = value; }
        }

        [CLSCompliant(false)]
        public byte[][] SignatureMasks
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

            int status = SafeNativeMethods.Gdip.GdipGetImageDecodersSize(out numDecoders, out size);

            if (status != SafeNativeMethods.Gdip.Ok)
            {
                throw SafeNativeMethods.Gdip.StatusException(status);
            }

            IntPtr memory = Marshal.AllocHGlobal(size);

            try
            {
                status = SafeNativeMethods.Gdip.GdipGetImageDecoders(numDecoders, size, memory);

                if (status != SafeNativeMethods.Gdip.Ok)
                {
                    throw SafeNativeMethods.Gdip.StatusException(status);
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

            int status = SafeNativeMethods.Gdip.GdipGetImageEncodersSize(out numEncoders, out size);

            if (status != SafeNativeMethods.Gdip.Ok)
            {
                throw SafeNativeMethods.Gdip.StatusException(status);
            }

            IntPtr memory = Marshal.AllocHGlobal(size);

            try
            {
                status = SafeNativeMethods.Gdip.GdipGetImageEncoders(numEncoders, size, memory);

                if (status != SafeNativeMethods.Gdip.Ok)
                {
                    throw SafeNativeMethods.Gdip.StatusException(status);
                }

                imageCodecs = ImageCodecInfo.ConvertFromMemory(memory, numEncoders);
            }
            finally
            {
                Marshal.FreeHGlobal(memory);
            }

            return imageCodecs;
        }

        private static ImageCodecInfo[] ConvertFromMemory(IntPtr memoryStart, int numCodecs)
        {
            ImageCodecInfo[] codecs = new ImageCodecInfo[numCodecs];

            int index;

            for (index = 0; index < numCodecs; index++)
            {
                IntPtr curcodec = (IntPtr)((long)memoryStart + (int)Marshal.SizeOf(typeof(ImageCodecInfoPrivate)) * index);
                ImageCodecInfoPrivate codecp = new ImageCodecInfoPrivate();
                Marshal.PtrToStructure(curcodec, codecp);

                codecs[index] = new ImageCodecInfo();
                codecs[index].Clsid = codecp.Clsid;
                codecs[index].FormatID = codecp.FormatID;
                codecs[index].CodecName = Marshal.PtrToStringUni(codecp.CodecName);
                codecs[index].DllName = Marshal.PtrToStringUni(codecp.DllName);
                codecs[index].FormatDescription = Marshal.PtrToStringUni(codecp.FormatDescription);
                codecs[index].FilenameExtension = Marshal.PtrToStringUni(codecp.FilenameExtension);
                codecs[index].MimeType = Marshal.PtrToStringUni(codecp.MimeType);

                codecs[index].Flags = (ImageCodecFlags)codecp.Flags;
                codecs[index].Version = (int)codecp.Version;

                codecs[index].SignaturePatterns = new byte[codecp.SigCount][];
                codecs[index].SignatureMasks = new byte[codecp.SigCount][];

                for (int j = 0; j < codecp.SigCount; j++)
                {
                    codecs[index].SignaturePatterns[j] = new byte[codecp.SigSize];
                    codecs[index].SignatureMasks[j] = new byte[codecp.SigSize];

                    Marshal.Copy((IntPtr)((long)codecp.SigMask + j * codecp.SigSize), codecs[index].SignatureMasks[j], 0, codecp.SigSize);
                    Marshal.Copy((IntPtr)((long)codecp.SigPattern + j * codecp.SigSize), codecs[index].SignaturePatterns[j], 0, codecp.SigSize);
                }
            }

            return codecs;
        }
    }
}
