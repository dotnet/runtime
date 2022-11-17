// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
#if NET7_0_OR_GREATER
using System.Runtime.InteropServices.Marshalling;
#endif

namespace System.Drawing.Imaging
{
    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    internal sealed class MetafileHeaderWmf
    {
        /// The ENHMETAHEADER structure is defined natively as a union with WmfHeader.
        /// Extreme care should be taken if changing the layout of the corresponding managed
        /// structures to minimize the risk of buffer overruns.  The affected managed classes
        /// are the following: ENHMETAHEADER, MetaHeader, MetafileHeaderWmf, MetafileHeaderEmf.
        public MetafileType type = MetafileType.Invalid;
        public int size = Marshal.SizeOf<MetafileHeaderWmf>();
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

#if NET7_0_OR_GREATER
        [CustomMarshaller(typeof(MetafileHeaderWmf), MarshalMode.ManagedToUnmanagedRef, typeof(InPlaceMarshaller))]
        internal static class Marshaller
        {
            internal unsafe struct InPlaceMarshaller
            {
                [StructLayout(LayoutKind.Sequential, Pack = 8)]
                internal struct Native
                {
                    /// The ENHMETAHEADER structure is defined natively as a union with WmfHeader.
                    /// Extreme care should be taken if changing the layout of the corresponding managed
                    /// structures to minimize the risk of buffer overruns.  The affected managed classes
                    /// are the following: ENHMETAHEADER, MetaHeader, MetafileHeaderWmf, MetafileHeaderEmf.
                    internal MetafileType type;
                    internal int size;
                    internal int version;
                    internal EmfPlusFlags emfPlusFlags;
                    internal float dpiX;
                    internal float dpiY;
                    internal int X;
                    internal int Y;
                    internal int Width;
                    internal int Height;
                    internal WmfMetaHeader WmfHeader;
                    internal int dummy1;
                    internal int dummy2;
                    internal int dummy3;
                    internal int dummy4;
                    internal int dummy5;
                    internal int dummy6;
                    internal int dummy7;
                    internal int dummy8;
                    internal int dummy9;
                    internal int dummy10;
                    internal int dummy11;
                    internal int dummy12;
                    internal int dummy13;
                    internal int dummy14;
                    internal int dummy15;
                    internal int dummy16;
                    internal int EmfPlusHeaderSize;
                    internal int LogicalDpiX;
                    internal int LogicalDpiY;
                }

                private MetafileHeaderWmf? _managed;
                private Native _native;

                public InPlaceMarshaller()
                {
                    _managed = null;
                    Unsafe.SkipInit(out _native);
                }

                public void FromManaged(MetafileHeaderWmf managed)
                {
                    _managed = managed;
                    _native.type = managed.type;
                    _native.size = managed.size;
                    _native.version = managed.version;
                    _native.emfPlusFlags = managed.emfPlusFlags;
                    _native.dpiX = managed.dpiX;
                    _native.dpiY = managed.dpiY;
                    _native.X = managed.X;
                    _native.Y = managed.Y;
                    _native.Width = managed.Width;
                    _native.Height = managed.Height;
                    _native.WmfHeader = managed.WmfHeader.GetNativeValue();
                    _native.dummy16 = managed.dummy16;
                    _native.EmfPlusHeaderSize = managed.EmfPlusHeaderSize;
                    _native.LogicalDpiX = managed.LogicalDpiX;
                    _native.LogicalDpiY = managed.LogicalDpiY;
                }

                public Native ToUnmanaged() => _native;

                public void FromUnmanaged(Native value) => _native = value;

                public MetafileHeaderWmf ToManaged()
                {
                    Debug.Assert(_managed is not null);
                    _managed.type = _native.type;
                    _managed.size = _native.size;
                    _managed.version = _native.version;
                    _managed.emfPlusFlags = _native.emfPlusFlags;
                    _managed.dpiX = _native.dpiX;
                    _managed.dpiY = _native.dpiY;
                    _managed.X = _native.X;
                    _managed.Y = _native.Y;
                    _managed.Width = _native.Width;
                    _managed.Height = _native.Height;
                    _managed.WmfHeader = new MetaHeader(_native.WmfHeader);
                    _managed.dummy16 = _native.dummy16;
                    _managed.EmfPlusHeaderSize = _native.EmfPlusHeaderSize;
                    _managed.LogicalDpiX = _native.LogicalDpiX;
                    _managed.LogicalDpiY = _native.LogicalDpiY;
                    return _managed;
                }

                public void Free() { }
            }
    }
#endif
    }
}
