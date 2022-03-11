// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Drawing.Imaging
{
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;

#if NET7_0_OR_GREATER
    [NativeMarshalling(typeof(PinningMarshaller))]
#endif
    [StructLayout(LayoutKind.Sequential)]
    internal sealed class MetafileHeaderEmf
    {
        /// The ENHMETAHEADER structure is defined natively as a union with WmfHeader.
        /// Extreme care should be taken if changing the layout of the corresponding managed
        /// structures to minimize the risk of buffer overruns.  The affected managed classes
        /// are the following: ENHMETAHEADER, MetaHeader, MetafileHeaderWmf, MetafileHeaderEmf.
        public MetafileType type = MetafileType.Invalid;
        public int size;
        public int version;
        public EmfPlusFlags emfPlusFlags;
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

        internal ref byte GetPinnableReference() => ref Unsafe.As<MetafileType, byte>(ref type);

#if NET7_0_OR_GREATER
        [CustomTypeMarshaller(typeof(MetafileHeaderEmf), Direction = CustomTypeMarshallerDirection.In, Features = CustomTypeMarshallerFeatures.TwoStageMarshalling)]
        internal unsafe struct PinningMarshaller
        {
            private readonly MetafileHeaderEmf _managed;
            public PinningMarshaller(MetafileHeaderEmf managed)
            {
                _managed = managed;
            }

            public ref byte GetPinnableReference() => ref (_managed is null ? ref Unsafe.NullRef<byte>() : ref _managed.GetPinnableReference());

            public void* ToNativeValue() => Unsafe.AsPointer(ref GetPinnableReference());
        }
#endif
    }
}
