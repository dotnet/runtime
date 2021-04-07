// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Drawing.Imaging
{
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    internal sealed class MetafileHeaderWmf
    {
        /// The ENHMETAHEADER structure is defined natively as a union with WmfHeader.
        /// Extreme care should be taken if changing the layout of the corresponding managed
        /// structures to minimize the risk of buffer overruns.  The affected managed classes
        /// are the following: ENHMETAHEADER, MetaHeader, MetafileHeaderWmf, MetafileHeaderEmf.
        public MetafileType type = MetafileType.Invalid;
        public int size = Marshal.SizeOf(typeof(MetafileHeaderWmf));
        public int version;
        public EmfPlusFlags emfPlusFlags;
        public float dpiX;
        public float dpiY;
        public int X;
        public int Y;
        public int Width;
        public int Height;

        //The below datatype, WmfHeader, file is defined natively
        //as a union with EmfHeader.  Since EmfHeader is a larger
        //structure, we need to pad the struct below so that this
        //will marshal correctly.
#pragma warning disable CS0618 // Legacy code: We don't care about using obsolete API's.
        [MarshalAs(UnmanagedType.Struct)]
#pragma warning restore CS0618
        public MetaHeader WmfHeader = new MetaHeader();
        public int dummy1;
        public int dummy2;
        public int dummy3;
        public int dummy4;
        public int dummy5;
        public int dummy6;
        public int dummy7;
        public int dummy8;
        public int dummy9;
        public int dummy10;
        public int dummy11;
        public int dummy12;
        public int dummy13;
        public int dummy14;
        public int dummy15;
        public int dummy16;

        public int EmfPlusHeaderSize;
        public int LogicalDpiX;
        public int LogicalDpiY;
    }
}
