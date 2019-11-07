// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing.Imaging
{
    public sealed class Encoder
    {
        public static readonly Encoder Compression = new Encoder(new Guid(unchecked((int)0xe09d739d), unchecked((short)0xccd4), unchecked((short)0x44ee), new byte[] { 0x8e, 0xba, 0x3f, 0xbf, 0x8b, 0xe4, 0xfc, 0x58 }));
        public static readonly Encoder ColorDepth = new Encoder(new Guid(0x66087055, unchecked((short)0xad66), unchecked((short)0x4c7c), new byte[] { 0x9a, 0x18, 0x38, 0xa2, 0x31, 0x0b, 0x83, 0x37 }));
        public static readonly Encoder ScanMethod = new Encoder(new Guid(0x3a4e2661, (short)0x3109, (short)0x4e56, new byte[] { 0x85, 0x36, 0x42, 0xc1, 0x56, 0xe7, 0xdc, 0xfa }));
        public static readonly Encoder Version = new Encoder(new Guid(0x24d18c76, unchecked((short)0x814a), unchecked((short)0x41a4), new byte[] { 0xbf, 0x53, 0x1c, 0x21, 0x9c, 0xcc, 0xf7, 0x97 }));
        public static readonly Encoder RenderMethod = new Encoder(new Guid(0x6d42c53a, (short)0x229a, (short)0x4825, new byte[] { 0x8b, 0xb7, 0x5c, 0x99, 0xe2, 0xb9, 0xa8, 0xb8 }));
        public static readonly Encoder Quality = new Encoder(new Guid(0x1d5be4b5, unchecked((short)0xfa4a), unchecked((short)0x452d), new byte[] { 0x9c, 0xdd, 0x5d, 0xb3, 0x51, 0x05, 0xe7, 0xeb }));
        public static readonly Encoder Transformation = new Encoder(new Guid(unchecked((int)0x8d0eb2d1), unchecked((short)0xa58e), unchecked((short)0x4ea8), new byte[] { 0xaa, 0x14, 0x10, 0x80, 0x74, 0xb7, 0xb6, 0xf9 }));
        public static readonly Encoder LuminanceTable = new Encoder(new Guid(unchecked((int)0xedb33bce), unchecked((short)0x0266), unchecked((short)0x4a77), new byte[] { 0xb9, 0x04, 0x27, 0x21, 0x60, 0x99, 0xe7, 0x17 }));
        public static readonly Encoder ChrominanceTable = new Encoder(new Guid(unchecked((int)0xf2e455dc), unchecked((short)0x09b3), unchecked((short)0x4316), new byte[] { 0x82, 0x60, 0x67, 0x6a, 0xda, 0x32, 0x48, 0x1c }));
        public static readonly Encoder SaveFlag = new Encoder(new Guid(unchecked((int)0x292266fc), unchecked((short)0xac40), unchecked((short)0x47bf), new byte[] { 0x8c, 0xfc, 0xa8, 0x5b, 0x89, 0xa6, 0x55, 0xde }));

        private Guid _guid;

        public Encoder(Guid guid)
        {
            _guid = guid;
        }

        public Guid Guid
        {
            get { return _guid; }
        }
    }
}
