// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing.Imaging
{
    using System.Runtime.InteropServices;

    // sdkinc\imaging.h
    /// <include file='doc\ImageCodecInfo.uex' path='docs/doc[@for="ImageCodecInfo"]/*' />
    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
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

        /// <include file='doc\ImageCodecInfo.uex' path='docs/doc[@for="ImageCodecInfo.Clsid"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public Guid Clsid
        {
            get { return _clsid; }
            set { _clsid = value; }
        }
        /// <include file='doc\ImageCodecInfo.uex' path='docs/doc[@for="ImageCodecInfo.FormatID"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public Guid FormatID
        {
            get { return _formatID; }
            set { _formatID = value; }
        }
        /// <include file='doc\ImageCodecInfo.uex' path='docs/doc[@for="ImageCodecInfo.CodecName"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public string CodecName
        {
            get { return _codecName; }
            set { _codecName = value; }
        }
        /// <include file='doc\ImageCodecInfo.uex' path='docs/doc[@for="ImageCodecInfo.DllName"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
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
        /// <include file='doc\ImageCodecInfo.uex' path='docs/doc[@for="ImageCodecInfo.FormatDescription"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public string FormatDescription
        {
            get { return _formatDescription; }
            set { _formatDescription = value; }
        }
        /// <include file='doc\ImageCodecInfo.uex' path='docs/doc[@for="ImageCodecInfo.FilenameExtension"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public string FilenameExtension
        {
            get { return _filenameExtension; }
            set { _filenameExtension = value; }
        }
        /// <include file='doc\ImageCodecInfo.uex' path='docs/doc[@for="ImageCodecInfo.MimeType"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public string MimeType
        {
            get { return _mimeType; }
            set { _mimeType = value; }
        }
        /// <include file='doc\ImageCodecInfo.uex' path='docs/doc[@for="ImageCodecInfo.Flags"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public ImageCodecFlags Flags
        {
            get { return _flags; }
            set { _flags = value; }
        }
        /// <include file='doc\ImageCodecInfo.uex' path='docs/doc[@for="ImageCodecInfo.Version"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public int Version
        {
            get { return _version; }
            set { _version = value; }
        }
        /// <include file='doc\ImageCodecInfo.uex' path='docs/doc[@for="ImageCodecInfo.SignaturePatterns"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [CLSCompliant(false)]
        public byte[][] SignaturePatterns
        {
            get { return _signaturePatterns; }
            set { _signaturePatterns = value; }
        }
        /// <include file='doc\ImageCodecInfo.uex' path='docs/doc[@for="ImageCodecInfo.SignatureMasks"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [CLSCompliant(false)]
        public byte[][] SignatureMasks
        {
            get { return _signatureMasks; }
            set { _signatureMasks = value; }
        }

        // Encoder/Decoder selection APIs

        /// <include file='doc\ImageCodecInfo.uex' path='docs/doc[@for="ImageCodecInfo.GetImageDecoders"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
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

        /// <include file='doc\ImageCodecInfo.uex' path='docs/doc[@for="ImageCodecInfo.GetImageEncoders"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
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

        /* FxCop rule 'AvoidBuildingNonCallableCode' - Left here in case it is needed in the future.
         internal static ImageCodecInfoPrivate ConvertToMemory(ImageCodecInfo imagecs)
         {
             ImageCodecInfoPrivate imagecsp = new ImageCodecInfoPrivate();
             
             imagecsp.Clsid = imagecs.Clsid;
             imagecsp.FormatID = imagecs.FormatID;
             
             imagecsp.CodecName = Marshal.StringToHGlobalUni(imagecs.CodecName);
             imagecsp.DllName = Marshal.StringToHGlobalUni(imagecs.DllName);
             imagecsp.FormatDescription = Marshal.StringToHGlobalUni(imagecs.FormatDescription);
             imagecsp.FilenameExtension = Marshal.StringToHGlobalUni(imagecs.FilenameExtension);
             imagecsp.MimeType = Marshal.StringToHGlobalUni(imagecs.MimeType);
             
             imagecsp.Flags = (int)imagecs.Flags;
             imagecsp.Version = (int)imagecs.Version;
             imagecsp.SigCount = imagecs.SignaturePatterns.Length;
             imagecsp.SigSize = imagecs.SignaturePatterns[0].Length;
             
             imagecsp.SigPattern = Marshal.AllocHGlobal(imagecsp.SigCount*imagecsp.SigSize);
             imagecsp.SigMask = Marshal.AllocHGlobal(imagecsp.SigCount*imagecsp.SigSize);
             
             for (int i=0; i<imagecsp.SigCount; i++)
             {
                 Marshal.Copy(imagecs.SignaturePatterns[i], 
                              0, 
                              (IntPtr)((long)imagecsp.SigPattern + i*imagecsp.SigSize), 
                              imagecsp.SigSize);
                                  
                 Marshal.Copy(imagecs.SignatureMasks[i], 
                              0, 
                              (IntPtr)((long)imagecsp.SigMask + i*imagecsp.SigSize), 
                              imagecsp.SigSize);
             }
                          
             return imagecsp;
         }

         internal static void FreeMemory(ImageCodecInfoPrivate imagecodecp)
         {
             Marshal.FreeHGlobal(imagecodecp.CodecName);
             Marshal.FreeHGlobal(imagecodecp.FormatDescription);
             Marshal.FreeHGlobal(imagecodecp.FilenameExtension);
             Marshal.FreeHGlobal(imagecodecp.MimeType);
             Marshal.FreeHGlobal(imagecodecp.SigPattern);
             Marshal.FreeHGlobal(imagecodecp.SigMask);
         }
         */

        private static ImageCodecInfo[] ConvertFromMemory(IntPtr memoryStart, int numCodecs)
        {
            ImageCodecInfo[] codecs = new ImageCodecInfo[numCodecs];

            int index;

            for (index = 0; index < numCodecs; index++)
            {
                IntPtr curcodec = (IntPtr)((long)memoryStart + (int)Marshal.SizeOf(typeof(ImageCodecInfoPrivate)) * index);
                ImageCodecInfoPrivate codecp = new ImageCodecInfoPrivate();
                UnsafeNativeMethods.PtrToStructure(curcodec, codecp);

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
