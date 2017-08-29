// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing.Imaging
{
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential)]
    internal class MetafileHeaderEmf
    {
        /// The ENHMETAHEADER structure is defined natively as a union with WmfHeader.  
        /// Extreme care should be taken if changing the layout of the corresponding managed 
        /// structures to minimize the risk of buffer overruns.  The affected managed classes 
        /// are the following: ENHMETAHEADER, MetaHeader, MetafileHeaderWmf, MetafileHeaderEmf.
        public MetafileType type = MetafileType.Invalid;
        public int size;
        public int version;
        public EmfPlusFlags emfPlusFlags = 0;
        public float dpiX;
        public float dpiY;
        public int X;
        public int Y;
        public int Width;
        public int Height;
        public SafeNativeMethods.ENHMETAHEADER EmfHeader;
        public int EmfPlusHeaderSize;
        public int LogicalDpiX;
        public int LogicalDpiY;
    }
}
