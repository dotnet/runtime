// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Runtime_64125
{
    [StructLayout(LayoutKind.Explicit, Size=1)]
    struct Struct1Bytes
    {
    }

    [StructLayout(LayoutKind.Explicit, Size=2)]
    struct Struct2Bytes
    {
    }

    [StructLayout(LayoutKind.Explicit, Size=3)]
    struct Struct3Bytes
    {
    }

    [StructLayout(LayoutKind.Explicit, Size=4)]
    struct Struct4Bytes
    {
    }

    [StructLayout(LayoutKind.Explicit, Size=7)]
    struct Struct7Bytes
    {
    }

    [StructLayout(LayoutKind.Explicit, Size=8)]
    struct Struct8Bytes
    {
    }

    [StructLayout(LayoutKind.Explicit, Size=15)]
    struct Struct15Bytes
    {
    }

    [StructLayout(LayoutKind.Explicit, Size=16)]
    struct Struct16Bytes
    {
    }

    [StructLayout(LayoutKind.Explicit, Size=31)]
    struct Struct31Bytes
    {
    }

    [StructLayout(LayoutKind.Explicit, Size=32)]
    struct Struct32Bytes
    {
    }

    [StructLayout(LayoutKind.Explicit, Size=63)]
    struct Struct63Bytes
    {
    }

    [StructLayout(LayoutKind.Explicit, Size=64)]
    struct Struct64Bytes
    {
    }

    [StructLayout(LayoutKind.Explicit, Size=127)]
    struct Struct127Bytes
    {
    }

    [StructLayout(LayoutKind.Explicit, Size=128)]
    struct Struct128Bytes
    {
    }

    [StructLayout(LayoutKind.Explicit, Size=65648)]
    struct AnyOffset
    {
        [FieldOffset(252)]
        public byte fieldAtOffset252;
        [FieldOffset(255)]
        public byte fieldAtOffset255;
        [FieldOffset(504)]
        public byte fieldAtOffset504;
        [FieldOffset(1008)]
        public byte fieldAtOffset1008;
        [FieldOffset(4095)]
        public byte fieldAtOffset4095;
        [FieldOffset(8190)]
        public byte fieldAtOffset8190;
        [FieldOffset(16380)]
        public byte fieldAtOffset16380;
        [FieldOffset(32760)]
        public byte fieldAtOffset32760;
        [FieldOffset(65520)]
        public byte fieldAtOffset65520;
    }

    class AnyLocation
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy1BytesFromLocationAtOffset252ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct1Bytes*)&dst->fieldAtOffset252 = *(Struct1Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy1BytesFromLocationAtOffset252ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct1Bytes*)&dst->fieldAtOffset255 = *(Struct1Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy1BytesFromLocationAtOffset252ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct1Bytes*)&dst->fieldAtOffset504 = *(Struct1Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy1BytesFromLocationAtOffset252ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct1Bytes*)&dst->fieldAtOffset1008 = *(Struct1Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy1BytesFromLocationAtOffset252ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct1Bytes*)&dst->fieldAtOffset4095 = *(Struct1Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy1BytesFromLocationAtOffset252ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct1Bytes*)&dst->fieldAtOffset8190 = *(Struct1Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy1BytesFromLocationAtOffset252ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct1Bytes*)&dst->fieldAtOffset16380 = *(Struct1Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy1BytesFromLocationAtOffset252ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct1Bytes*)&dst->fieldAtOffset32760 = *(Struct1Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy1BytesFromLocationAtOffset252ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct1Bytes*)&dst->fieldAtOffset65520 = *(Struct1Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy1BytesFromLocationAtOffset255ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct1Bytes*)&dst->fieldAtOffset252 = *(Struct1Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy1BytesFromLocationAtOffset255ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct1Bytes*)&dst->fieldAtOffset255 = *(Struct1Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy1BytesFromLocationAtOffset255ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct1Bytes*)&dst->fieldAtOffset504 = *(Struct1Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy1BytesFromLocationAtOffset255ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct1Bytes*)&dst->fieldAtOffset1008 = *(Struct1Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy1BytesFromLocationAtOffset255ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct1Bytes*)&dst->fieldAtOffset4095 = *(Struct1Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy1BytesFromLocationAtOffset255ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct1Bytes*)&dst->fieldAtOffset8190 = *(Struct1Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy1BytesFromLocationAtOffset255ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct1Bytes*)&dst->fieldAtOffset16380 = *(Struct1Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy1BytesFromLocationAtOffset255ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct1Bytes*)&dst->fieldAtOffset32760 = *(Struct1Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy1BytesFromLocationAtOffset255ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct1Bytes*)&dst->fieldAtOffset65520 = *(Struct1Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy1BytesFromLocationAtOffset504ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct1Bytes*)&dst->fieldAtOffset252 = *(Struct1Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy1BytesFromLocationAtOffset504ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct1Bytes*)&dst->fieldAtOffset255 = *(Struct1Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy1BytesFromLocationAtOffset504ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct1Bytes*)&dst->fieldAtOffset504 = *(Struct1Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy1BytesFromLocationAtOffset504ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct1Bytes*)&dst->fieldAtOffset1008 = *(Struct1Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy1BytesFromLocationAtOffset504ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct1Bytes*)&dst->fieldAtOffset4095 = *(Struct1Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy1BytesFromLocationAtOffset504ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct1Bytes*)&dst->fieldAtOffset8190 = *(Struct1Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy1BytesFromLocationAtOffset504ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct1Bytes*)&dst->fieldAtOffset16380 = *(Struct1Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy1BytesFromLocationAtOffset504ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct1Bytes*)&dst->fieldAtOffset32760 = *(Struct1Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy1BytesFromLocationAtOffset504ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct1Bytes*)&dst->fieldAtOffset65520 = *(Struct1Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy1BytesFromLocationAtOffset1008ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct1Bytes*)&dst->fieldAtOffset252 = *(Struct1Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy1BytesFromLocationAtOffset1008ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct1Bytes*)&dst->fieldAtOffset255 = *(Struct1Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy1BytesFromLocationAtOffset1008ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct1Bytes*)&dst->fieldAtOffset504 = *(Struct1Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy1BytesFromLocationAtOffset1008ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct1Bytes*)&dst->fieldAtOffset1008 = *(Struct1Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy1BytesFromLocationAtOffset1008ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct1Bytes*)&dst->fieldAtOffset4095 = *(Struct1Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy1BytesFromLocationAtOffset1008ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct1Bytes*)&dst->fieldAtOffset8190 = *(Struct1Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy1BytesFromLocationAtOffset1008ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct1Bytes*)&dst->fieldAtOffset16380 = *(Struct1Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy1BytesFromLocationAtOffset1008ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct1Bytes*)&dst->fieldAtOffset32760 = *(Struct1Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy1BytesFromLocationAtOffset1008ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct1Bytes*)&dst->fieldAtOffset65520 = *(Struct1Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy1BytesFromLocationAtOffset4095ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct1Bytes*)&dst->fieldAtOffset252 = *(Struct1Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy1BytesFromLocationAtOffset4095ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct1Bytes*)&dst->fieldAtOffset255 = *(Struct1Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy1BytesFromLocationAtOffset4095ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct1Bytes*)&dst->fieldAtOffset504 = *(Struct1Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy1BytesFromLocationAtOffset4095ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct1Bytes*)&dst->fieldAtOffset1008 = *(Struct1Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy1BytesFromLocationAtOffset4095ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct1Bytes*)&dst->fieldAtOffset4095 = *(Struct1Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy1BytesFromLocationAtOffset4095ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct1Bytes*)&dst->fieldAtOffset8190 = *(Struct1Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy1BytesFromLocationAtOffset4095ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct1Bytes*)&dst->fieldAtOffset16380 = *(Struct1Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy1BytesFromLocationAtOffset4095ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct1Bytes*)&dst->fieldAtOffset32760 = *(Struct1Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy1BytesFromLocationAtOffset4095ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct1Bytes*)&dst->fieldAtOffset65520 = *(Struct1Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy1BytesFromLocationAtOffset8190ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct1Bytes*)&dst->fieldAtOffset252 = *(Struct1Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy1BytesFromLocationAtOffset8190ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct1Bytes*)&dst->fieldAtOffset255 = *(Struct1Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy1BytesFromLocationAtOffset8190ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct1Bytes*)&dst->fieldAtOffset504 = *(Struct1Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy1BytesFromLocationAtOffset8190ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct1Bytes*)&dst->fieldAtOffset1008 = *(Struct1Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy1BytesFromLocationAtOffset8190ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct1Bytes*)&dst->fieldAtOffset4095 = *(Struct1Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy1BytesFromLocationAtOffset8190ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct1Bytes*)&dst->fieldAtOffset8190 = *(Struct1Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy1BytesFromLocationAtOffset8190ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct1Bytes*)&dst->fieldAtOffset16380 = *(Struct1Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy1BytesFromLocationAtOffset8190ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct1Bytes*)&dst->fieldAtOffset32760 = *(Struct1Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy1BytesFromLocationAtOffset8190ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct1Bytes*)&dst->fieldAtOffset65520 = *(Struct1Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy1BytesFromLocationAtOffset16380ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct1Bytes*)&dst->fieldAtOffset252 = *(Struct1Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy1BytesFromLocationAtOffset16380ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct1Bytes*)&dst->fieldAtOffset255 = *(Struct1Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy1BytesFromLocationAtOffset16380ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct1Bytes*)&dst->fieldAtOffset504 = *(Struct1Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy1BytesFromLocationAtOffset16380ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct1Bytes*)&dst->fieldAtOffset1008 = *(Struct1Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy1BytesFromLocationAtOffset16380ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct1Bytes*)&dst->fieldAtOffset4095 = *(Struct1Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy1BytesFromLocationAtOffset16380ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct1Bytes*)&dst->fieldAtOffset8190 = *(Struct1Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy1BytesFromLocationAtOffset16380ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct1Bytes*)&dst->fieldAtOffset16380 = *(Struct1Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy1BytesFromLocationAtOffset16380ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct1Bytes*)&dst->fieldAtOffset32760 = *(Struct1Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy1BytesFromLocationAtOffset16380ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct1Bytes*)&dst->fieldAtOffset65520 = *(Struct1Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy1BytesFromLocationAtOffset32760ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct1Bytes*)&dst->fieldAtOffset252 = *(Struct1Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy1BytesFromLocationAtOffset32760ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct1Bytes*)&dst->fieldAtOffset255 = *(Struct1Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy1BytesFromLocationAtOffset32760ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct1Bytes*)&dst->fieldAtOffset504 = *(Struct1Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy1BytesFromLocationAtOffset32760ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct1Bytes*)&dst->fieldAtOffset1008 = *(Struct1Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy1BytesFromLocationAtOffset32760ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct1Bytes*)&dst->fieldAtOffset4095 = *(Struct1Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy1BytesFromLocationAtOffset32760ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct1Bytes*)&dst->fieldAtOffset8190 = *(Struct1Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy1BytesFromLocationAtOffset32760ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct1Bytes*)&dst->fieldAtOffset16380 = *(Struct1Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy1BytesFromLocationAtOffset32760ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct1Bytes*)&dst->fieldAtOffset32760 = *(Struct1Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy1BytesFromLocationAtOffset32760ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct1Bytes*)&dst->fieldAtOffset65520 = *(Struct1Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy1BytesFromLocationAtOffset65520ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct1Bytes*)&dst->fieldAtOffset252 = *(Struct1Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy1BytesFromLocationAtOffset65520ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct1Bytes*)&dst->fieldAtOffset255 = *(Struct1Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy1BytesFromLocationAtOffset65520ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct1Bytes*)&dst->fieldAtOffset504 = *(Struct1Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy1BytesFromLocationAtOffset65520ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct1Bytes*)&dst->fieldAtOffset1008 = *(Struct1Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy1BytesFromLocationAtOffset65520ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct1Bytes*)&dst->fieldAtOffset4095 = *(Struct1Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy1BytesFromLocationAtOffset65520ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct1Bytes*)&dst->fieldAtOffset8190 = *(Struct1Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy1BytesFromLocationAtOffset65520ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct1Bytes*)&dst->fieldAtOffset16380 = *(Struct1Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy1BytesFromLocationAtOffset65520ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct1Bytes*)&dst->fieldAtOffset32760 = *(Struct1Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy1BytesFromLocationAtOffset65520ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct1Bytes*)&dst->fieldAtOffset65520 = *(Struct1Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy2BytesFromLocationAtOffset252ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct2Bytes*)&dst->fieldAtOffset252 = *(Struct2Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy2BytesFromLocationAtOffset252ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct2Bytes*)&dst->fieldAtOffset255 = *(Struct2Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy2BytesFromLocationAtOffset252ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct2Bytes*)&dst->fieldAtOffset504 = *(Struct2Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy2BytesFromLocationAtOffset252ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct2Bytes*)&dst->fieldAtOffset1008 = *(Struct2Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy2BytesFromLocationAtOffset252ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct2Bytes*)&dst->fieldAtOffset4095 = *(Struct2Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy2BytesFromLocationAtOffset252ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct2Bytes*)&dst->fieldAtOffset8190 = *(Struct2Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy2BytesFromLocationAtOffset252ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct2Bytes*)&dst->fieldAtOffset16380 = *(Struct2Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy2BytesFromLocationAtOffset252ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct2Bytes*)&dst->fieldAtOffset32760 = *(Struct2Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy2BytesFromLocationAtOffset252ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct2Bytes*)&dst->fieldAtOffset65520 = *(Struct2Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy2BytesFromLocationAtOffset255ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct2Bytes*)&dst->fieldAtOffset252 = *(Struct2Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy2BytesFromLocationAtOffset255ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct2Bytes*)&dst->fieldAtOffset255 = *(Struct2Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy2BytesFromLocationAtOffset255ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct2Bytes*)&dst->fieldAtOffset504 = *(Struct2Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy2BytesFromLocationAtOffset255ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct2Bytes*)&dst->fieldAtOffset1008 = *(Struct2Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy2BytesFromLocationAtOffset255ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct2Bytes*)&dst->fieldAtOffset4095 = *(Struct2Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy2BytesFromLocationAtOffset255ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct2Bytes*)&dst->fieldAtOffset8190 = *(Struct2Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy2BytesFromLocationAtOffset255ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct2Bytes*)&dst->fieldAtOffset16380 = *(Struct2Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy2BytesFromLocationAtOffset255ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct2Bytes*)&dst->fieldAtOffset32760 = *(Struct2Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy2BytesFromLocationAtOffset255ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct2Bytes*)&dst->fieldAtOffset65520 = *(Struct2Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy2BytesFromLocationAtOffset504ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct2Bytes*)&dst->fieldAtOffset252 = *(Struct2Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy2BytesFromLocationAtOffset504ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct2Bytes*)&dst->fieldAtOffset255 = *(Struct2Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy2BytesFromLocationAtOffset504ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct2Bytes*)&dst->fieldAtOffset504 = *(Struct2Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy2BytesFromLocationAtOffset504ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct2Bytes*)&dst->fieldAtOffset1008 = *(Struct2Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy2BytesFromLocationAtOffset504ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct2Bytes*)&dst->fieldAtOffset4095 = *(Struct2Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy2BytesFromLocationAtOffset504ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct2Bytes*)&dst->fieldAtOffset8190 = *(Struct2Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy2BytesFromLocationAtOffset504ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct2Bytes*)&dst->fieldAtOffset16380 = *(Struct2Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy2BytesFromLocationAtOffset504ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct2Bytes*)&dst->fieldAtOffset32760 = *(Struct2Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy2BytesFromLocationAtOffset504ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct2Bytes*)&dst->fieldAtOffset65520 = *(Struct2Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy2BytesFromLocationAtOffset1008ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct2Bytes*)&dst->fieldAtOffset252 = *(Struct2Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy2BytesFromLocationAtOffset1008ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct2Bytes*)&dst->fieldAtOffset255 = *(Struct2Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy2BytesFromLocationAtOffset1008ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct2Bytes*)&dst->fieldAtOffset504 = *(Struct2Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy2BytesFromLocationAtOffset1008ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct2Bytes*)&dst->fieldAtOffset1008 = *(Struct2Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy2BytesFromLocationAtOffset1008ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct2Bytes*)&dst->fieldAtOffset4095 = *(Struct2Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy2BytesFromLocationAtOffset1008ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct2Bytes*)&dst->fieldAtOffset8190 = *(Struct2Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy2BytesFromLocationAtOffset1008ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct2Bytes*)&dst->fieldAtOffset16380 = *(Struct2Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy2BytesFromLocationAtOffset1008ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct2Bytes*)&dst->fieldAtOffset32760 = *(Struct2Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy2BytesFromLocationAtOffset1008ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct2Bytes*)&dst->fieldAtOffset65520 = *(Struct2Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy2BytesFromLocationAtOffset4095ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct2Bytes*)&dst->fieldAtOffset252 = *(Struct2Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy2BytesFromLocationAtOffset4095ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct2Bytes*)&dst->fieldAtOffset255 = *(Struct2Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy2BytesFromLocationAtOffset4095ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct2Bytes*)&dst->fieldAtOffset504 = *(Struct2Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy2BytesFromLocationAtOffset4095ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct2Bytes*)&dst->fieldAtOffset1008 = *(Struct2Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy2BytesFromLocationAtOffset4095ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct2Bytes*)&dst->fieldAtOffset4095 = *(Struct2Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy2BytesFromLocationAtOffset4095ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct2Bytes*)&dst->fieldAtOffset8190 = *(Struct2Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy2BytesFromLocationAtOffset4095ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct2Bytes*)&dst->fieldAtOffset16380 = *(Struct2Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy2BytesFromLocationAtOffset4095ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct2Bytes*)&dst->fieldAtOffset32760 = *(Struct2Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy2BytesFromLocationAtOffset4095ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct2Bytes*)&dst->fieldAtOffset65520 = *(Struct2Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy2BytesFromLocationAtOffset8190ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct2Bytes*)&dst->fieldAtOffset252 = *(Struct2Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy2BytesFromLocationAtOffset8190ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct2Bytes*)&dst->fieldAtOffset255 = *(Struct2Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy2BytesFromLocationAtOffset8190ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct2Bytes*)&dst->fieldAtOffset504 = *(Struct2Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy2BytesFromLocationAtOffset8190ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct2Bytes*)&dst->fieldAtOffset1008 = *(Struct2Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy2BytesFromLocationAtOffset8190ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct2Bytes*)&dst->fieldAtOffset4095 = *(Struct2Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy2BytesFromLocationAtOffset8190ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct2Bytes*)&dst->fieldAtOffset8190 = *(Struct2Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy2BytesFromLocationAtOffset8190ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct2Bytes*)&dst->fieldAtOffset16380 = *(Struct2Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy2BytesFromLocationAtOffset8190ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct2Bytes*)&dst->fieldAtOffset32760 = *(Struct2Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy2BytesFromLocationAtOffset8190ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct2Bytes*)&dst->fieldAtOffset65520 = *(Struct2Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy2BytesFromLocationAtOffset16380ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct2Bytes*)&dst->fieldAtOffset252 = *(Struct2Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy2BytesFromLocationAtOffset16380ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct2Bytes*)&dst->fieldAtOffset255 = *(Struct2Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy2BytesFromLocationAtOffset16380ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct2Bytes*)&dst->fieldAtOffset504 = *(Struct2Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy2BytesFromLocationAtOffset16380ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct2Bytes*)&dst->fieldAtOffset1008 = *(Struct2Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy2BytesFromLocationAtOffset16380ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct2Bytes*)&dst->fieldAtOffset4095 = *(Struct2Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy2BytesFromLocationAtOffset16380ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct2Bytes*)&dst->fieldAtOffset8190 = *(Struct2Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy2BytesFromLocationAtOffset16380ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct2Bytes*)&dst->fieldAtOffset16380 = *(Struct2Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy2BytesFromLocationAtOffset16380ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct2Bytes*)&dst->fieldAtOffset32760 = *(Struct2Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy2BytesFromLocationAtOffset16380ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct2Bytes*)&dst->fieldAtOffset65520 = *(Struct2Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy2BytesFromLocationAtOffset32760ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct2Bytes*)&dst->fieldAtOffset252 = *(Struct2Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy2BytesFromLocationAtOffset32760ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct2Bytes*)&dst->fieldAtOffset255 = *(Struct2Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy2BytesFromLocationAtOffset32760ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct2Bytes*)&dst->fieldAtOffset504 = *(Struct2Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy2BytesFromLocationAtOffset32760ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct2Bytes*)&dst->fieldAtOffset1008 = *(Struct2Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy2BytesFromLocationAtOffset32760ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct2Bytes*)&dst->fieldAtOffset4095 = *(Struct2Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy2BytesFromLocationAtOffset32760ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct2Bytes*)&dst->fieldAtOffset8190 = *(Struct2Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy2BytesFromLocationAtOffset32760ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct2Bytes*)&dst->fieldAtOffset16380 = *(Struct2Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy2BytesFromLocationAtOffset32760ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct2Bytes*)&dst->fieldAtOffset32760 = *(Struct2Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy2BytesFromLocationAtOffset32760ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct2Bytes*)&dst->fieldAtOffset65520 = *(Struct2Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy2BytesFromLocationAtOffset65520ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct2Bytes*)&dst->fieldAtOffset252 = *(Struct2Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy2BytesFromLocationAtOffset65520ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct2Bytes*)&dst->fieldAtOffset255 = *(Struct2Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy2BytesFromLocationAtOffset65520ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct2Bytes*)&dst->fieldAtOffset504 = *(Struct2Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy2BytesFromLocationAtOffset65520ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct2Bytes*)&dst->fieldAtOffset1008 = *(Struct2Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy2BytesFromLocationAtOffset65520ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct2Bytes*)&dst->fieldAtOffset4095 = *(Struct2Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy2BytesFromLocationAtOffset65520ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct2Bytes*)&dst->fieldAtOffset8190 = *(Struct2Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy2BytesFromLocationAtOffset65520ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct2Bytes*)&dst->fieldAtOffset16380 = *(Struct2Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy2BytesFromLocationAtOffset65520ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct2Bytes*)&dst->fieldAtOffset32760 = *(Struct2Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy2BytesFromLocationAtOffset65520ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct2Bytes*)&dst->fieldAtOffset65520 = *(Struct2Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy3BytesFromLocationAtOffset252ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct3Bytes*)&dst->fieldAtOffset252 = *(Struct3Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy3BytesFromLocationAtOffset252ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct3Bytes*)&dst->fieldAtOffset255 = *(Struct3Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy3BytesFromLocationAtOffset252ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct3Bytes*)&dst->fieldAtOffset504 = *(Struct3Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy3BytesFromLocationAtOffset252ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct3Bytes*)&dst->fieldAtOffset1008 = *(Struct3Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy3BytesFromLocationAtOffset252ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct3Bytes*)&dst->fieldAtOffset4095 = *(Struct3Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy3BytesFromLocationAtOffset252ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct3Bytes*)&dst->fieldAtOffset8190 = *(Struct3Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy3BytesFromLocationAtOffset252ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct3Bytes*)&dst->fieldAtOffset16380 = *(Struct3Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy3BytesFromLocationAtOffset252ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct3Bytes*)&dst->fieldAtOffset32760 = *(Struct3Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy3BytesFromLocationAtOffset252ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct3Bytes*)&dst->fieldAtOffset65520 = *(Struct3Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy3BytesFromLocationAtOffset255ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct3Bytes*)&dst->fieldAtOffset252 = *(Struct3Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy3BytesFromLocationAtOffset255ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct3Bytes*)&dst->fieldAtOffset255 = *(Struct3Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy3BytesFromLocationAtOffset255ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct3Bytes*)&dst->fieldAtOffset504 = *(Struct3Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy3BytesFromLocationAtOffset255ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct3Bytes*)&dst->fieldAtOffset1008 = *(Struct3Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy3BytesFromLocationAtOffset255ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct3Bytes*)&dst->fieldAtOffset4095 = *(Struct3Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy3BytesFromLocationAtOffset255ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct3Bytes*)&dst->fieldAtOffset8190 = *(Struct3Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy3BytesFromLocationAtOffset255ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct3Bytes*)&dst->fieldAtOffset16380 = *(Struct3Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy3BytesFromLocationAtOffset255ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct3Bytes*)&dst->fieldAtOffset32760 = *(Struct3Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy3BytesFromLocationAtOffset255ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct3Bytes*)&dst->fieldAtOffset65520 = *(Struct3Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy3BytesFromLocationAtOffset504ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct3Bytes*)&dst->fieldAtOffset252 = *(Struct3Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy3BytesFromLocationAtOffset504ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct3Bytes*)&dst->fieldAtOffset255 = *(Struct3Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy3BytesFromLocationAtOffset504ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct3Bytes*)&dst->fieldAtOffset504 = *(Struct3Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy3BytesFromLocationAtOffset504ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct3Bytes*)&dst->fieldAtOffset1008 = *(Struct3Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy3BytesFromLocationAtOffset504ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct3Bytes*)&dst->fieldAtOffset4095 = *(Struct3Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy3BytesFromLocationAtOffset504ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct3Bytes*)&dst->fieldAtOffset8190 = *(Struct3Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy3BytesFromLocationAtOffset504ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct3Bytes*)&dst->fieldAtOffset16380 = *(Struct3Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy3BytesFromLocationAtOffset504ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct3Bytes*)&dst->fieldAtOffset32760 = *(Struct3Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy3BytesFromLocationAtOffset504ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct3Bytes*)&dst->fieldAtOffset65520 = *(Struct3Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy3BytesFromLocationAtOffset1008ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct3Bytes*)&dst->fieldAtOffset252 = *(Struct3Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy3BytesFromLocationAtOffset1008ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct3Bytes*)&dst->fieldAtOffset255 = *(Struct3Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy3BytesFromLocationAtOffset1008ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct3Bytes*)&dst->fieldAtOffset504 = *(Struct3Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy3BytesFromLocationAtOffset1008ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct3Bytes*)&dst->fieldAtOffset1008 = *(Struct3Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy3BytesFromLocationAtOffset1008ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct3Bytes*)&dst->fieldAtOffset4095 = *(Struct3Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy3BytesFromLocationAtOffset1008ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct3Bytes*)&dst->fieldAtOffset8190 = *(Struct3Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy3BytesFromLocationAtOffset1008ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct3Bytes*)&dst->fieldAtOffset16380 = *(Struct3Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy3BytesFromLocationAtOffset1008ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct3Bytes*)&dst->fieldAtOffset32760 = *(Struct3Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy3BytesFromLocationAtOffset1008ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct3Bytes*)&dst->fieldAtOffset65520 = *(Struct3Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy3BytesFromLocationAtOffset4095ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct3Bytes*)&dst->fieldAtOffset252 = *(Struct3Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy3BytesFromLocationAtOffset4095ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct3Bytes*)&dst->fieldAtOffset255 = *(Struct3Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy3BytesFromLocationAtOffset4095ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct3Bytes*)&dst->fieldAtOffset504 = *(Struct3Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy3BytesFromLocationAtOffset4095ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct3Bytes*)&dst->fieldAtOffset1008 = *(Struct3Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy3BytesFromLocationAtOffset4095ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct3Bytes*)&dst->fieldAtOffset4095 = *(Struct3Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy3BytesFromLocationAtOffset4095ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct3Bytes*)&dst->fieldAtOffset8190 = *(Struct3Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy3BytesFromLocationAtOffset4095ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct3Bytes*)&dst->fieldAtOffset16380 = *(Struct3Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy3BytesFromLocationAtOffset4095ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct3Bytes*)&dst->fieldAtOffset32760 = *(Struct3Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy3BytesFromLocationAtOffset4095ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct3Bytes*)&dst->fieldAtOffset65520 = *(Struct3Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy3BytesFromLocationAtOffset8190ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct3Bytes*)&dst->fieldAtOffset252 = *(Struct3Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy3BytesFromLocationAtOffset8190ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct3Bytes*)&dst->fieldAtOffset255 = *(Struct3Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy3BytesFromLocationAtOffset8190ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct3Bytes*)&dst->fieldAtOffset504 = *(Struct3Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy3BytesFromLocationAtOffset8190ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct3Bytes*)&dst->fieldAtOffset1008 = *(Struct3Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy3BytesFromLocationAtOffset8190ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct3Bytes*)&dst->fieldAtOffset4095 = *(Struct3Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy3BytesFromLocationAtOffset8190ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct3Bytes*)&dst->fieldAtOffset8190 = *(Struct3Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy3BytesFromLocationAtOffset8190ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct3Bytes*)&dst->fieldAtOffset16380 = *(Struct3Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy3BytesFromLocationAtOffset8190ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct3Bytes*)&dst->fieldAtOffset32760 = *(Struct3Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy3BytesFromLocationAtOffset8190ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct3Bytes*)&dst->fieldAtOffset65520 = *(Struct3Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy3BytesFromLocationAtOffset16380ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct3Bytes*)&dst->fieldAtOffset252 = *(Struct3Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy3BytesFromLocationAtOffset16380ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct3Bytes*)&dst->fieldAtOffset255 = *(Struct3Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy3BytesFromLocationAtOffset16380ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct3Bytes*)&dst->fieldAtOffset504 = *(Struct3Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy3BytesFromLocationAtOffset16380ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct3Bytes*)&dst->fieldAtOffset1008 = *(Struct3Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy3BytesFromLocationAtOffset16380ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct3Bytes*)&dst->fieldAtOffset4095 = *(Struct3Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy3BytesFromLocationAtOffset16380ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct3Bytes*)&dst->fieldAtOffset8190 = *(Struct3Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy3BytesFromLocationAtOffset16380ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct3Bytes*)&dst->fieldAtOffset16380 = *(Struct3Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy3BytesFromLocationAtOffset16380ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct3Bytes*)&dst->fieldAtOffset32760 = *(Struct3Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy3BytesFromLocationAtOffset16380ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct3Bytes*)&dst->fieldAtOffset65520 = *(Struct3Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy3BytesFromLocationAtOffset32760ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct3Bytes*)&dst->fieldAtOffset252 = *(Struct3Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy3BytesFromLocationAtOffset32760ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct3Bytes*)&dst->fieldAtOffset255 = *(Struct3Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy3BytesFromLocationAtOffset32760ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct3Bytes*)&dst->fieldAtOffset504 = *(Struct3Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy3BytesFromLocationAtOffset32760ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct3Bytes*)&dst->fieldAtOffset1008 = *(Struct3Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy3BytesFromLocationAtOffset32760ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct3Bytes*)&dst->fieldAtOffset4095 = *(Struct3Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy3BytesFromLocationAtOffset32760ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct3Bytes*)&dst->fieldAtOffset8190 = *(Struct3Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy3BytesFromLocationAtOffset32760ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct3Bytes*)&dst->fieldAtOffset16380 = *(Struct3Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy3BytesFromLocationAtOffset32760ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct3Bytes*)&dst->fieldAtOffset32760 = *(Struct3Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy3BytesFromLocationAtOffset32760ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct3Bytes*)&dst->fieldAtOffset65520 = *(Struct3Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy3BytesFromLocationAtOffset65520ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct3Bytes*)&dst->fieldAtOffset252 = *(Struct3Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy3BytesFromLocationAtOffset65520ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct3Bytes*)&dst->fieldAtOffset255 = *(Struct3Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy3BytesFromLocationAtOffset65520ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct3Bytes*)&dst->fieldAtOffset504 = *(Struct3Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy3BytesFromLocationAtOffset65520ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct3Bytes*)&dst->fieldAtOffset1008 = *(Struct3Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy3BytesFromLocationAtOffset65520ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct3Bytes*)&dst->fieldAtOffset4095 = *(Struct3Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy3BytesFromLocationAtOffset65520ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct3Bytes*)&dst->fieldAtOffset8190 = *(Struct3Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy3BytesFromLocationAtOffset65520ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct3Bytes*)&dst->fieldAtOffset16380 = *(Struct3Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy3BytesFromLocationAtOffset65520ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct3Bytes*)&dst->fieldAtOffset32760 = *(Struct3Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy3BytesFromLocationAtOffset65520ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct3Bytes*)&dst->fieldAtOffset65520 = *(Struct3Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy4BytesFromLocationAtOffset252ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct4Bytes*)&dst->fieldAtOffset252 = *(Struct4Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy4BytesFromLocationAtOffset252ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct4Bytes*)&dst->fieldAtOffset255 = *(Struct4Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy4BytesFromLocationAtOffset252ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct4Bytes*)&dst->fieldAtOffset504 = *(Struct4Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy4BytesFromLocationAtOffset252ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct4Bytes*)&dst->fieldAtOffset1008 = *(Struct4Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy4BytesFromLocationAtOffset252ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct4Bytes*)&dst->fieldAtOffset4095 = *(Struct4Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy4BytesFromLocationAtOffset252ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct4Bytes*)&dst->fieldAtOffset8190 = *(Struct4Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy4BytesFromLocationAtOffset252ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct4Bytes*)&dst->fieldAtOffset16380 = *(Struct4Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy4BytesFromLocationAtOffset252ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct4Bytes*)&dst->fieldAtOffset32760 = *(Struct4Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy4BytesFromLocationAtOffset252ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct4Bytes*)&dst->fieldAtOffset65520 = *(Struct4Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy4BytesFromLocationAtOffset255ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct4Bytes*)&dst->fieldAtOffset252 = *(Struct4Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy4BytesFromLocationAtOffset255ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct4Bytes*)&dst->fieldAtOffset255 = *(Struct4Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy4BytesFromLocationAtOffset255ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct4Bytes*)&dst->fieldAtOffset504 = *(Struct4Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy4BytesFromLocationAtOffset255ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct4Bytes*)&dst->fieldAtOffset1008 = *(Struct4Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy4BytesFromLocationAtOffset255ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct4Bytes*)&dst->fieldAtOffset4095 = *(Struct4Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy4BytesFromLocationAtOffset255ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct4Bytes*)&dst->fieldAtOffset8190 = *(Struct4Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy4BytesFromLocationAtOffset255ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct4Bytes*)&dst->fieldAtOffset16380 = *(Struct4Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy4BytesFromLocationAtOffset255ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct4Bytes*)&dst->fieldAtOffset32760 = *(Struct4Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy4BytesFromLocationAtOffset255ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct4Bytes*)&dst->fieldAtOffset65520 = *(Struct4Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy4BytesFromLocationAtOffset504ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct4Bytes*)&dst->fieldAtOffset252 = *(Struct4Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy4BytesFromLocationAtOffset504ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct4Bytes*)&dst->fieldAtOffset255 = *(Struct4Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy4BytesFromLocationAtOffset504ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct4Bytes*)&dst->fieldAtOffset504 = *(Struct4Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy4BytesFromLocationAtOffset504ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct4Bytes*)&dst->fieldAtOffset1008 = *(Struct4Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy4BytesFromLocationAtOffset504ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct4Bytes*)&dst->fieldAtOffset4095 = *(Struct4Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy4BytesFromLocationAtOffset504ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct4Bytes*)&dst->fieldAtOffset8190 = *(Struct4Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy4BytesFromLocationAtOffset504ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct4Bytes*)&dst->fieldAtOffset16380 = *(Struct4Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy4BytesFromLocationAtOffset504ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct4Bytes*)&dst->fieldAtOffset32760 = *(Struct4Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy4BytesFromLocationAtOffset504ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct4Bytes*)&dst->fieldAtOffset65520 = *(Struct4Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy4BytesFromLocationAtOffset1008ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct4Bytes*)&dst->fieldAtOffset252 = *(Struct4Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy4BytesFromLocationAtOffset1008ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct4Bytes*)&dst->fieldAtOffset255 = *(Struct4Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy4BytesFromLocationAtOffset1008ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct4Bytes*)&dst->fieldAtOffset504 = *(Struct4Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy4BytesFromLocationAtOffset1008ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct4Bytes*)&dst->fieldAtOffset1008 = *(Struct4Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy4BytesFromLocationAtOffset1008ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct4Bytes*)&dst->fieldAtOffset4095 = *(Struct4Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy4BytesFromLocationAtOffset1008ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct4Bytes*)&dst->fieldAtOffset8190 = *(Struct4Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy4BytesFromLocationAtOffset1008ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct4Bytes*)&dst->fieldAtOffset16380 = *(Struct4Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy4BytesFromLocationAtOffset1008ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct4Bytes*)&dst->fieldAtOffset32760 = *(Struct4Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy4BytesFromLocationAtOffset1008ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct4Bytes*)&dst->fieldAtOffset65520 = *(Struct4Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy4BytesFromLocationAtOffset4095ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct4Bytes*)&dst->fieldAtOffset252 = *(Struct4Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy4BytesFromLocationAtOffset4095ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct4Bytes*)&dst->fieldAtOffset255 = *(Struct4Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy4BytesFromLocationAtOffset4095ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct4Bytes*)&dst->fieldAtOffset504 = *(Struct4Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy4BytesFromLocationAtOffset4095ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct4Bytes*)&dst->fieldAtOffset1008 = *(Struct4Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy4BytesFromLocationAtOffset4095ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct4Bytes*)&dst->fieldAtOffset4095 = *(Struct4Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy4BytesFromLocationAtOffset4095ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct4Bytes*)&dst->fieldAtOffset8190 = *(Struct4Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy4BytesFromLocationAtOffset4095ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct4Bytes*)&dst->fieldAtOffset16380 = *(Struct4Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy4BytesFromLocationAtOffset4095ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct4Bytes*)&dst->fieldAtOffset32760 = *(Struct4Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy4BytesFromLocationAtOffset4095ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct4Bytes*)&dst->fieldAtOffset65520 = *(Struct4Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy4BytesFromLocationAtOffset8190ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct4Bytes*)&dst->fieldAtOffset252 = *(Struct4Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy4BytesFromLocationAtOffset8190ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct4Bytes*)&dst->fieldAtOffset255 = *(Struct4Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy4BytesFromLocationAtOffset8190ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct4Bytes*)&dst->fieldAtOffset504 = *(Struct4Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy4BytesFromLocationAtOffset8190ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct4Bytes*)&dst->fieldAtOffset1008 = *(Struct4Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy4BytesFromLocationAtOffset8190ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct4Bytes*)&dst->fieldAtOffset4095 = *(Struct4Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy4BytesFromLocationAtOffset8190ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct4Bytes*)&dst->fieldAtOffset8190 = *(Struct4Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy4BytesFromLocationAtOffset8190ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct4Bytes*)&dst->fieldAtOffset16380 = *(Struct4Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy4BytesFromLocationAtOffset8190ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct4Bytes*)&dst->fieldAtOffset32760 = *(Struct4Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy4BytesFromLocationAtOffset8190ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct4Bytes*)&dst->fieldAtOffset65520 = *(Struct4Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy4BytesFromLocationAtOffset16380ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct4Bytes*)&dst->fieldAtOffset252 = *(Struct4Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy4BytesFromLocationAtOffset16380ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct4Bytes*)&dst->fieldAtOffset255 = *(Struct4Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy4BytesFromLocationAtOffset16380ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct4Bytes*)&dst->fieldAtOffset504 = *(Struct4Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy4BytesFromLocationAtOffset16380ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct4Bytes*)&dst->fieldAtOffset1008 = *(Struct4Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy4BytesFromLocationAtOffset16380ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct4Bytes*)&dst->fieldAtOffset4095 = *(Struct4Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy4BytesFromLocationAtOffset16380ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct4Bytes*)&dst->fieldAtOffset8190 = *(Struct4Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy4BytesFromLocationAtOffset16380ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct4Bytes*)&dst->fieldAtOffset16380 = *(Struct4Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy4BytesFromLocationAtOffset16380ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct4Bytes*)&dst->fieldAtOffset32760 = *(Struct4Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy4BytesFromLocationAtOffset16380ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct4Bytes*)&dst->fieldAtOffset65520 = *(Struct4Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy4BytesFromLocationAtOffset32760ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct4Bytes*)&dst->fieldAtOffset252 = *(Struct4Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy4BytesFromLocationAtOffset32760ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct4Bytes*)&dst->fieldAtOffset255 = *(Struct4Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy4BytesFromLocationAtOffset32760ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct4Bytes*)&dst->fieldAtOffset504 = *(Struct4Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy4BytesFromLocationAtOffset32760ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct4Bytes*)&dst->fieldAtOffset1008 = *(Struct4Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy4BytesFromLocationAtOffset32760ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct4Bytes*)&dst->fieldAtOffset4095 = *(Struct4Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy4BytesFromLocationAtOffset32760ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct4Bytes*)&dst->fieldAtOffset8190 = *(Struct4Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy4BytesFromLocationAtOffset32760ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct4Bytes*)&dst->fieldAtOffset16380 = *(Struct4Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy4BytesFromLocationAtOffset32760ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct4Bytes*)&dst->fieldAtOffset32760 = *(Struct4Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy4BytesFromLocationAtOffset32760ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct4Bytes*)&dst->fieldAtOffset65520 = *(Struct4Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy4BytesFromLocationAtOffset65520ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct4Bytes*)&dst->fieldAtOffset252 = *(Struct4Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy4BytesFromLocationAtOffset65520ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct4Bytes*)&dst->fieldAtOffset255 = *(Struct4Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy4BytesFromLocationAtOffset65520ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct4Bytes*)&dst->fieldAtOffset504 = *(Struct4Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy4BytesFromLocationAtOffset65520ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct4Bytes*)&dst->fieldAtOffset1008 = *(Struct4Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy4BytesFromLocationAtOffset65520ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct4Bytes*)&dst->fieldAtOffset4095 = *(Struct4Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy4BytesFromLocationAtOffset65520ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct4Bytes*)&dst->fieldAtOffset8190 = *(Struct4Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy4BytesFromLocationAtOffset65520ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct4Bytes*)&dst->fieldAtOffset16380 = *(Struct4Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy4BytesFromLocationAtOffset65520ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct4Bytes*)&dst->fieldAtOffset32760 = *(Struct4Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy4BytesFromLocationAtOffset65520ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct4Bytes*)&dst->fieldAtOffset65520 = *(Struct4Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy7BytesFromLocationAtOffset252ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct7Bytes*)&dst->fieldAtOffset252 = *(Struct7Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy7BytesFromLocationAtOffset252ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct7Bytes*)&dst->fieldAtOffset255 = *(Struct7Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy7BytesFromLocationAtOffset252ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct7Bytes*)&dst->fieldAtOffset504 = *(Struct7Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy7BytesFromLocationAtOffset252ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct7Bytes*)&dst->fieldAtOffset1008 = *(Struct7Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy7BytesFromLocationAtOffset252ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct7Bytes*)&dst->fieldAtOffset4095 = *(Struct7Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy7BytesFromLocationAtOffset252ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct7Bytes*)&dst->fieldAtOffset8190 = *(Struct7Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy7BytesFromLocationAtOffset252ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct7Bytes*)&dst->fieldAtOffset16380 = *(Struct7Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy7BytesFromLocationAtOffset252ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct7Bytes*)&dst->fieldAtOffset32760 = *(Struct7Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy7BytesFromLocationAtOffset252ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct7Bytes*)&dst->fieldAtOffset65520 = *(Struct7Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy7BytesFromLocationAtOffset255ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct7Bytes*)&dst->fieldAtOffset252 = *(Struct7Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy7BytesFromLocationAtOffset255ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct7Bytes*)&dst->fieldAtOffset255 = *(Struct7Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy7BytesFromLocationAtOffset255ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct7Bytes*)&dst->fieldAtOffset504 = *(Struct7Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy7BytesFromLocationAtOffset255ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct7Bytes*)&dst->fieldAtOffset1008 = *(Struct7Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy7BytesFromLocationAtOffset255ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct7Bytes*)&dst->fieldAtOffset4095 = *(Struct7Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy7BytesFromLocationAtOffset255ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct7Bytes*)&dst->fieldAtOffset8190 = *(Struct7Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy7BytesFromLocationAtOffset255ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct7Bytes*)&dst->fieldAtOffset16380 = *(Struct7Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy7BytesFromLocationAtOffset255ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct7Bytes*)&dst->fieldAtOffset32760 = *(Struct7Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy7BytesFromLocationAtOffset255ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct7Bytes*)&dst->fieldAtOffset65520 = *(Struct7Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy7BytesFromLocationAtOffset504ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct7Bytes*)&dst->fieldAtOffset252 = *(Struct7Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy7BytesFromLocationAtOffset504ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct7Bytes*)&dst->fieldAtOffset255 = *(Struct7Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy7BytesFromLocationAtOffset504ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct7Bytes*)&dst->fieldAtOffset504 = *(Struct7Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy7BytesFromLocationAtOffset504ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct7Bytes*)&dst->fieldAtOffset1008 = *(Struct7Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy7BytesFromLocationAtOffset504ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct7Bytes*)&dst->fieldAtOffset4095 = *(Struct7Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy7BytesFromLocationAtOffset504ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct7Bytes*)&dst->fieldAtOffset8190 = *(Struct7Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy7BytesFromLocationAtOffset504ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct7Bytes*)&dst->fieldAtOffset16380 = *(Struct7Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy7BytesFromLocationAtOffset504ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct7Bytes*)&dst->fieldAtOffset32760 = *(Struct7Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy7BytesFromLocationAtOffset504ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct7Bytes*)&dst->fieldAtOffset65520 = *(Struct7Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy7BytesFromLocationAtOffset1008ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct7Bytes*)&dst->fieldAtOffset252 = *(Struct7Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy7BytesFromLocationAtOffset1008ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct7Bytes*)&dst->fieldAtOffset255 = *(Struct7Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy7BytesFromLocationAtOffset1008ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct7Bytes*)&dst->fieldAtOffset504 = *(Struct7Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy7BytesFromLocationAtOffset1008ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct7Bytes*)&dst->fieldAtOffset1008 = *(Struct7Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy7BytesFromLocationAtOffset1008ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct7Bytes*)&dst->fieldAtOffset4095 = *(Struct7Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy7BytesFromLocationAtOffset1008ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct7Bytes*)&dst->fieldAtOffset8190 = *(Struct7Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy7BytesFromLocationAtOffset1008ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct7Bytes*)&dst->fieldAtOffset16380 = *(Struct7Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy7BytesFromLocationAtOffset1008ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct7Bytes*)&dst->fieldAtOffset32760 = *(Struct7Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy7BytesFromLocationAtOffset1008ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct7Bytes*)&dst->fieldAtOffset65520 = *(Struct7Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy7BytesFromLocationAtOffset4095ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct7Bytes*)&dst->fieldAtOffset252 = *(Struct7Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy7BytesFromLocationAtOffset4095ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct7Bytes*)&dst->fieldAtOffset255 = *(Struct7Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy7BytesFromLocationAtOffset4095ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct7Bytes*)&dst->fieldAtOffset504 = *(Struct7Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy7BytesFromLocationAtOffset4095ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct7Bytes*)&dst->fieldAtOffset1008 = *(Struct7Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy7BytesFromLocationAtOffset4095ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct7Bytes*)&dst->fieldAtOffset4095 = *(Struct7Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy7BytesFromLocationAtOffset4095ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct7Bytes*)&dst->fieldAtOffset8190 = *(Struct7Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy7BytesFromLocationAtOffset4095ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct7Bytes*)&dst->fieldAtOffset16380 = *(Struct7Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy7BytesFromLocationAtOffset4095ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct7Bytes*)&dst->fieldAtOffset32760 = *(Struct7Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy7BytesFromLocationAtOffset4095ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct7Bytes*)&dst->fieldAtOffset65520 = *(Struct7Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy7BytesFromLocationAtOffset8190ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct7Bytes*)&dst->fieldAtOffset252 = *(Struct7Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy7BytesFromLocationAtOffset8190ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct7Bytes*)&dst->fieldAtOffset255 = *(Struct7Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy7BytesFromLocationAtOffset8190ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct7Bytes*)&dst->fieldAtOffset504 = *(Struct7Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy7BytesFromLocationAtOffset8190ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct7Bytes*)&dst->fieldAtOffset1008 = *(Struct7Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy7BytesFromLocationAtOffset8190ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct7Bytes*)&dst->fieldAtOffset4095 = *(Struct7Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy7BytesFromLocationAtOffset8190ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct7Bytes*)&dst->fieldAtOffset8190 = *(Struct7Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy7BytesFromLocationAtOffset8190ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct7Bytes*)&dst->fieldAtOffset16380 = *(Struct7Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy7BytesFromLocationAtOffset8190ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct7Bytes*)&dst->fieldAtOffset32760 = *(Struct7Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy7BytesFromLocationAtOffset8190ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct7Bytes*)&dst->fieldAtOffset65520 = *(Struct7Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy7BytesFromLocationAtOffset16380ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct7Bytes*)&dst->fieldAtOffset252 = *(Struct7Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy7BytesFromLocationAtOffset16380ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct7Bytes*)&dst->fieldAtOffset255 = *(Struct7Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy7BytesFromLocationAtOffset16380ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct7Bytes*)&dst->fieldAtOffset504 = *(Struct7Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy7BytesFromLocationAtOffset16380ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct7Bytes*)&dst->fieldAtOffset1008 = *(Struct7Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy7BytesFromLocationAtOffset16380ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct7Bytes*)&dst->fieldAtOffset4095 = *(Struct7Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy7BytesFromLocationAtOffset16380ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct7Bytes*)&dst->fieldAtOffset8190 = *(Struct7Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy7BytesFromLocationAtOffset16380ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct7Bytes*)&dst->fieldAtOffset16380 = *(Struct7Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy7BytesFromLocationAtOffset16380ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct7Bytes*)&dst->fieldAtOffset32760 = *(Struct7Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy7BytesFromLocationAtOffset16380ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct7Bytes*)&dst->fieldAtOffset65520 = *(Struct7Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy7BytesFromLocationAtOffset32760ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct7Bytes*)&dst->fieldAtOffset252 = *(Struct7Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy7BytesFromLocationAtOffset32760ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct7Bytes*)&dst->fieldAtOffset255 = *(Struct7Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy7BytesFromLocationAtOffset32760ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct7Bytes*)&dst->fieldAtOffset504 = *(Struct7Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy7BytesFromLocationAtOffset32760ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct7Bytes*)&dst->fieldAtOffset1008 = *(Struct7Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy7BytesFromLocationAtOffset32760ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct7Bytes*)&dst->fieldAtOffset4095 = *(Struct7Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy7BytesFromLocationAtOffset32760ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct7Bytes*)&dst->fieldAtOffset8190 = *(Struct7Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy7BytesFromLocationAtOffset32760ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct7Bytes*)&dst->fieldAtOffset16380 = *(Struct7Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy7BytesFromLocationAtOffset32760ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct7Bytes*)&dst->fieldAtOffset32760 = *(Struct7Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy7BytesFromLocationAtOffset32760ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct7Bytes*)&dst->fieldAtOffset65520 = *(Struct7Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy7BytesFromLocationAtOffset65520ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct7Bytes*)&dst->fieldAtOffset252 = *(Struct7Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy7BytesFromLocationAtOffset65520ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct7Bytes*)&dst->fieldAtOffset255 = *(Struct7Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy7BytesFromLocationAtOffset65520ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct7Bytes*)&dst->fieldAtOffset504 = *(Struct7Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy7BytesFromLocationAtOffset65520ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct7Bytes*)&dst->fieldAtOffset1008 = *(Struct7Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy7BytesFromLocationAtOffset65520ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct7Bytes*)&dst->fieldAtOffset4095 = *(Struct7Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy7BytesFromLocationAtOffset65520ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct7Bytes*)&dst->fieldAtOffset8190 = *(Struct7Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy7BytesFromLocationAtOffset65520ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct7Bytes*)&dst->fieldAtOffset16380 = *(Struct7Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy7BytesFromLocationAtOffset65520ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct7Bytes*)&dst->fieldAtOffset32760 = *(Struct7Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy7BytesFromLocationAtOffset65520ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct7Bytes*)&dst->fieldAtOffset65520 = *(Struct7Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy8BytesFromLocationAtOffset252ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct8Bytes*)&dst->fieldAtOffset252 = *(Struct8Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy8BytesFromLocationAtOffset252ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct8Bytes*)&dst->fieldAtOffset255 = *(Struct8Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy8BytesFromLocationAtOffset252ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct8Bytes*)&dst->fieldAtOffset504 = *(Struct8Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy8BytesFromLocationAtOffset252ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct8Bytes*)&dst->fieldAtOffset1008 = *(Struct8Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy8BytesFromLocationAtOffset252ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct8Bytes*)&dst->fieldAtOffset4095 = *(Struct8Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy8BytesFromLocationAtOffset252ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct8Bytes*)&dst->fieldAtOffset8190 = *(Struct8Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy8BytesFromLocationAtOffset252ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct8Bytes*)&dst->fieldAtOffset16380 = *(Struct8Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy8BytesFromLocationAtOffset252ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct8Bytes*)&dst->fieldAtOffset32760 = *(Struct8Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy8BytesFromLocationAtOffset252ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct8Bytes*)&dst->fieldAtOffset65520 = *(Struct8Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy8BytesFromLocationAtOffset255ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct8Bytes*)&dst->fieldAtOffset252 = *(Struct8Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy8BytesFromLocationAtOffset255ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct8Bytes*)&dst->fieldAtOffset255 = *(Struct8Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy8BytesFromLocationAtOffset255ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct8Bytes*)&dst->fieldAtOffset504 = *(Struct8Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy8BytesFromLocationAtOffset255ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct8Bytes*)&dst->fieldAtOffset1008 = *(Struct8Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy8BytesFromLocationAtOffset255ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct8Bytes*)&dst->fieldAtOffset4095 = *(Struct8Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy8BytesFromLocationAtOffset255ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct8Bytes*)&dst->fieldAtOffset8190 = *(Struct8Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy8BytesFromLocationAtOffset255ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct8Bytes*)&dst->fieldAtOffset16380 = *(Struct8Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy8BytesFromLocationAtOffset255ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct8Bytes*)&dst->fieldAtOffset32760 = *(Struct8Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy8BytesFromLocationAtOffset255ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct8Bytes*)&dst->fieldAtOffset65520 = *(Struct8Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy8BytesFromLocationAtOffset504ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct8Bytes*)&dst->fieldAtOffset252 = *(Struct8Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy8BytesFromLocationAtOffset504ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct8Bytes*)&dst->fieldAtOffset255 = *(Struct8Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy8BytesFromLocationAtOffset504ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct8Bytes*)&dst->fieldAtOffset504 = *(Struct8Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy8BytesFromLocationAtOffset504ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct8Bytes*)&dst->fieldAtOffset1008 = *(Struct8Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy8BytesFromLocationAtOffset504ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct8Bytes*)&dst->fieldAtOffset4095 = *(Struct8Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy8BytesFromLocationAtOffset504ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct8Bytes*)&dst->fieldAtOffset8190 = *(Struct8Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy8BytesFromLocationAtOffset504ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct8Bytes*)&dst->fieldAtOffset16380 = *(Struct8Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy8BytesFromLocationAtOffset504ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct8Bytes*)&dst->fieldAtOffset32760 = *(Struct8Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy8BytesFromLocationAtOffset504ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct8Bytes*)&dst->fieldAtOffset65520 = *(Struct8Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy8BytesFromLocationAtOffset1008ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct8Bytes*)&dst->fieldAtOffset252 = *(Struct8Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy8BytesFromLocationAtOffset1008ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct8Bytes*)&dst->fieldAtOffset255 = *(Struct8Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy8BytesFromLocationAtOffset1008ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct8Bytes*)&dst->fieldAtOffset504 = *(Struct8Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy8BytesFromLocationAtOffset1008ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct8Bytes*)&dst->fieldAtOffset1008 = *(Struct8Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy8BytesFromLocationAtOffset1008ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct8Bytes*)&dst->fieldAtOffset4095 = *(Struct8Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy8BytesFromLocationAtOffset1008ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct8Bytes*)&dst->fieldAtOffset8190 = *(Struct8Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy8BytesFromLocationAtOffset1008ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct8Bytes*)&dst->fieldAtOffset16380 = *(Struct8Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy8BytesFromLocationAtOffset1008ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct8Bytes*)&dst->fieldAtOffset32760 = *(Struct8Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy8BytesFromLocationAtOffset1008ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct8Bytes*)&dst->fieldAtOffset65520 = *(Struct8Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy8BytesFromLocationAtOffset4095ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct8Bytes*)&dst->fieldAtOffset252 = *(Struct8Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy8BytesFromLocationAtOffset4095ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct8Bytes*)&dst->fieldAtOffset255 = *(Struct8Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy8BytesFromLocationAtOffset4095ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct8Bytes*)&dst->fieldAtOffset504 = *(Struct8Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy8BytesFromLocationAtOffset4095ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct8Bytes*)&dst->fieldAtOffset1008 = *(Struct8Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy8BytesFromLocationAtOffset4095ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct8Bytes*)&dst->fieldAtOffset4095 = *(Struct8Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy8BytesFromLocationAtOffset4095ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct8Bytes*)&dst->fieldAtOffset8190 = *(Struct8Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy8BytesFromLocationAtOffset4095ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct8Bytes*)&dst->fieldAtOffset16380 = *(Struct8Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy8BytesFromLocationAtOffset4095ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct8Bytes*)&dst->fieldAtOffset32760 = *(Struct8Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy8BytesFromLocationAtOffset4095ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct8Bytes*)&dst->fieldAtOffset65520 = *(Struct8Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy8BytesFromLocationAtOffset8190ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct8Bytes*)&dst->fieldAtOffset252 = *(Struct8Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy8BytesFromLocationAtOffset8190ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct8Bytes*)&dst->fieldAtOffset255 = *(Struct8Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy8BytesFromLocationAtOffset8190ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct8Bytes*)&dst->fieldAtOffset504 = *(Struct8Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy8BytesFromLocationAtOffset8190ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct8Bytes*)&dst->fieldAtOffset1008 = *(Struct8Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy8BytesFromLocationAtOffset8190ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct8Bytes*)&dst->fieldAtOffset4095 = *(Struct8Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy8BytesFromLocationAtOffset8190ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct8Bytes*)&dst->fieldAtOffset8190 = *(Struct8Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy8BytesFromLocationAtOffset8190ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct8Bytes*)&dst->fieldAtOffset16380 = *(Struct8Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy8BytesFromLocationAtOffset8190ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct8Bytes*)&dst->fieldAtOffset32760 = *(Struct8Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy8BytesFromLocationAtOffset8190ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct8Bytes*)&dst->fieldAtOffset65520 = *(Struct8Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy8BytesFromLocationAtOffset16380ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct8Bytes*)&dst->fieldAtOffset252 = *(Struct8Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy8BytesFromLocationAtOffset16380ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct8Bytes*)&dst->fieldAtOffset255 = *(Struct8Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy8BytesFromLocationAtOffset16380ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct8Bytes*)&dst->fieldAtOffset504 = *(Struct8Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy8BytesFromLocationAtOffset16380ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct8Bytes*)&dst->fieldAtOffset1008 = *(Struct8Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy8BytesFromLocationAtOffset16380ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct8Bytes*)&dst->fieldAtOffset4095 = *(Struct8Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy8BytesFromLocationAtOffset16380ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct8Bytes*)&dst->fieldAtOffset8190 = *(Struct8Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy8BytesFromLocationAtOffset16380ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct8Bytes*)&dst->fieldAtOffset16380 = *(Struct8Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy8BytesFromLocationAtOffset16380ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct8Bytes*)&dst->fieldAtOffset32760 = *(Struct8Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy8BytesFromLocationAtOffset16380ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct8Bytes*)&dst->fieldAtOffset65520 = *(Struct8Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy8BytesFromLocationAtOffset32760ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct8Bytes*)&dst->fieldAtOffset252 = *(Struct8Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy8BytesFromLocationAtOffset32760ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct8Bytes*)&dst->fieldAtOffset255 = *(Struct8Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy8BytesFromLocationAtOffset32760ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct8Bytes*)&dst->fieldAtOffset504 = *(Struct8Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy8BytesFromLocationAtOffset32760ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct8Bytes*)&dst->fieldAtOffset1008 = *(Struct8Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy8BytesFromLocationAtOffset32760ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct8Bytes*)&dst->fieldAtOffset4095 = *(Struct8Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy8BytesFromLocationAtOffset32760ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct8Bytes*)&dst->fieldAtOffset8190 = *(Struct8Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy8BytesFromLocationAtOffset32760ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct8Bytes*)&dst->fieldAtOffset16380 = *(Struct8Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy8BytesFromLocationAtOffset32760ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct8Bytes*)&dst->fieldAtOffset32760 = *(Struct8Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy8BytesFromLocationAtOffset32760ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct8Bytes*)&dst->fieldAtOffset65520 = *(Struct8Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy8BytesFromLocationAtOffset65520ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct8Bytes*)&dst->fieldAtOffset252 = *(Struct8Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy8BytesFromLocationAtOffset65520ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct8Bytes*)&dst->fieldAtOffset255 = *(Struct8Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy8BytesFromLocationAtOffset65520ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct8Bytes*)&dst->fieldAtOffset504 = *(Struct8Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy8BytesFromLocationAtOffset65520ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct8Bytes*)&dst->fieldAtOffset1008 = *(Struct8Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy8BytesFromLocationAtOffset65520ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct8Bytes*)&dst->fieldAtOffset4095 = *(Struct8Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy8BytesFromLocationAtOffset65520ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct8Bytes*)&dst->fieldAtOffset8190 = *(Struct8Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy8BytesFromLocationAtOffset65520ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct8Bytes*)&dst->fieldAtOffset16380 = *(Struct8Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy8BytesFromLocationAtOffset65520ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct8Bytes*)&dst->fieldAtOffset32760 = *(Struct8Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy8BytesFromLocationAtOffset65520ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct8Bytes*)&dst->fieldAtOffset65520 = *(Struct8Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy15BytesFromLocationAtOffset252ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct15Bytes*)&dst->fieldAtOffset252 = *(Struct15Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy15BytesFromLocationAtOffset252ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct15Bytes*)&dst->fieldAtOffset255 = *(Struct15Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy15BytesFromLocationAtOffset252ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct15Bytes*)&dst->fieldAtOffset504 = *(Struct15Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy15BytesFromLocationAtOffset252ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct15Bytes*)&dst->fieldAtOffset1008 = *(Struct15Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy15BytesFromLocationAtOffset252ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct15Bytes*)&dst->fieldAtOffset4095 = *(Struct15Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy15BytesFromLocationAtOffset252ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct15Bytes*)&dst->fieldAtOffset8190 = *(Struct15Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy15BytesFromLocationAtOffset252ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct15Bytes*)&dst->fieldAtOffset16380 = *(Struct15Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy15BytesFromLocationAtOffset252ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct15Bytes*)&dst->fieldAtOffset32760 = *(Struct15Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy15BytesFromLocationAtOffset252ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct15Bytes*)&dst->fieldAtOffset65520 = *(Struct15Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy15BytesFromLocationAtOffset255ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct15Bytes*)&dst->fieldAtOffset252 = *(Struct15Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy15BytesFromLocationAtOffset255ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct15Bytes*)&dst->fieldAtOffset255 = *(Struct15Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy15BytesFromLocationAtOffset255ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct15Bytes*)&dst->fieldAtOffset504 = *(Struct15Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy15BytesFromLocationAtOffset255ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct15Bytes*)&dst->fieldAtOffset1008 = *(Struct15Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy15BytesFromLocationAtOffset255ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct15Bytes*)&dst->fieldAtOffset4095 = *(Struct15Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy15BytesFromLocationAtOffset255ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct15Bytes*)&dst->fieldAtOffset8190 = *(Struct15Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy15BytesFromLocationAtOffset255ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct15Bytes*)&dst->fieldAtOffset16380 = *(Struct15Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy15BytesFromLocationAtOffset255ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct15Bytes*)&dst->fieldAtOffset32760 = *(Struct15Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy15BytesFromLocationAtOffset255ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct15Bytes*)&dst->fieldAtOffset65520 = *(Struct15Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy15BytesFromLocationAtOffset504ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct15Bytes*)&dst->fieldAtOffset252 = *(Struct15Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy15BytesFromLocationAtOffset504ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct15Bytes*)&dst->fieldAtOffset255 = *(Struct15Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy15BytesFromLocationAtOffset504ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct15Bytes*)&dst->fieldAtOffset504 = *(Struct15Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy15BytesFromLocationAtOffset504ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct15Bytes*)&dst->fieldAtOffset1008 = *(Struct15Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy15BytesFromLocationAtOffset504ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct15Bytes*)&dst->fieldAtOffset4095 = *(Struct15Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy15BytesFromLocationAtOffset504ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct15Bytes*)&dst->fieldAtOffset8190 = *(Struct15Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy15BytesFromLocationAtOffset504ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct15Bytes*)&dst->fieldAtOffset16380 = *(Struct15Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy15BytesFromLocationAtOffset504ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct15Bytes*)&dst->fieldAtOffset32760 = *(Struct15Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy15BytesFromLocationAtOffset504ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct15Bytes*)&dst->fieldAtOffset65520 = *(Struct15Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy15BytesFromLocationAtOffset1008ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct15Bytes*)&dst->fieldAtOffset252 = *(Struct15Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy15BytesFromLocationAtOffset1008ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct15Bytes*)&dst->fieldAtOffset255 = *(Struct15Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy15BytesFromLocationAtOffset1008ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct15Bytes*)&dst->fieldAtOffset504 = *(Struct15Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy15BytesFromLocationAtOffset1008ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct15Bytes*)&dst->fieldAtOffset1008 = *(Struct15Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy15BytesFromLocationAtOffset1008ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct15Bytes*)&dst->fieldAtOffset4095 = *(Struct15Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy15BytesFromLocationAtOffset1008ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct15Bytes*)&dst->fieldAtOffset8190 = *(Struct15Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy15BytesFromLocationAtOffset1008ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct15Bytes*)&dst->fieldAtOffset16380 = *(Struct15Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy15BytesFromLocationAtOffset1008ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct15Bytes*)&dst->fieldAtOffset32760 = *(Struct15Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy15BytesFromLocationAtOffset1008ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct15Bytes*)&dst->fieldAtOffset65520 = *(Struct15Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy15BytesFromLocationAtOffset4095ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct15Bytes*)&dst->fieldAtOffset252 = *(Struct15Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy15BytesFromLocationAtOffset4095ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct15Bytes*)&dst->fieldAtOffset255 = *(Struct15Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy15BytesFromLocationAtOffset4095ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct15Bytes*)&dst->fieldAtOffset504 = *(Struct15Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy15BytesFromLocationAtOffset4095ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct15Bytes*)&dst->fieldAtOffset1008 = *(Struct15Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy15BytesFromLocationAtOffset4095ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct15Bytes*)&dst->fieldAtOffset4095 = *(Struct15Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy15BytesFromLocationAtOffset4095ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct15Bytes*)&dst->fieldAtOffset8190 = *(Struct15Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy15BytesFromLocationAtOffset4095ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct15Bytes*)&dst->fieldAtOffset16380 = *(Struct15Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy15BytesFromLocationAtOffset4095ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct15Bytes*)&dst->fieldAtOffset32760 = *(Struct15Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy15BytesFromLocationAtOffset4095ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct15Bytes*)&dst->fieldAtOffset65520 = *(Struct15Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy15BytesFromLocationAtOffset8190ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct15Bytes*)&dst->fieldAtOffset252 = *(Struct15Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy15BytesFromLocationAtOffset8190ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct15Bytes*)&dst->fieldAtOffset255 = *(Struct15Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy15BytesFromLocationAtOffset8190ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct15Bytes*)&dst->fieldAtOffset504 = *(Struct15Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy15BytesFromLocationAtOffset8190ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct15Bytes*)&dst->fieldAtOffset1008 = *(Struct15Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy15BytesFromLocationAtOffset8190ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct15Bytes*)&dst->fieldAtOffset4095 = *(Struct15Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy15BytesFromLocationAtOffset8190ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct15Bytes*)&dst->fieldAtOffset8190 = *(Struct15Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy15BytesFromLocationAtOffset8190ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct15Bytes*)&dst->fieldAtOffset16380 = *(Struct15Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy15BytesFromLocationAtOffset8190ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct15Bytes*)&dst->fieldAtOffset32760 = *(Struct15Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy15BytesFromLocationAtOffset8190ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct15Bytes*)&dst->fieldAtOffset65520 = *(Struct15Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy15BytesFromLocationAtOffset16380ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct15Bytes*)&dst->fieldAtOffset252 = *(Struct15Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy15BytesFromLocationAtOffset16380ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct15Bytes*)&dst->fieldAtOffset255 = *(Struct15Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy15BytesFromLocationAtOffset16380ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct15Bytes*)&dst->fieldAtOffset504 = *(Struct15Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy15BytesFromLocationAtOffset16380ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct15Bytes*)&dst->fieldAtOffset1008 = *(Struct15Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy15BytesFromLocationAtOffset16380ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct15Bytes*)&dst->fieldAtOffset4095 = *(Struct15Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy15BytesFromLocationAtOffset16380ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct15Bytes*)&dst->fieldAtOffset8190 = *(Struct15Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy15BytesFromLocationAtOffset16380ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct15Bytes*)&dst->fieldAtOffset16380 = *(Struct15Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy15BytesFromLocationAtOffset16380ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct15Bytes*)&dst->fieldAtOffset32760 = *(Struct15Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy15BytesFromLocationAtOffset16380ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct15Bytes*)&dst->fieldAtOffset65520 = *(Struct15Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy15BytesFromLocationAtOffset32760ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct15Bytes*)&dst->fieldAtOffset252 = *(Struct15Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy15BytesFromLocationAtOffset32760ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct15Bytes*)&dst->fieldAtOffset255 = *(Struct15Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy15BytesFromLocationAtOffset32760ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct15Bytes*)&dst->fieldAtOffset504 = *(Struct15Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy15BytesFromLocationAtOffset32760ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct15Bytes*)&dst->fieldAtOffset1008 = *(Struct15Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy15BytesFromLocationAtOffset32760ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct15Bytes*)&dst->fieldAtOffset4095 = *(Struct15Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy15BytesFromLocationAtOffset32760ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct15Bytes*)&dst->fieldAtOffset8190 = *(Struct15Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy15BytesFromLocationAtOffset32760ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct15Bytes*)&dst->fieldAtOffset16380 = *(Struct15Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy15BytesFromLocationAtOffset32760ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct15Bytes*)&dst->fieldAtOffset32760 = *(Struct15Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy15BytesFromLocationAtOffset32760ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct15Bytes*)&dst->fieldAtOffset65520 = *(Struct15Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy15BytesFromLocationAtOffset65520ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct15Bytes*)&dst->fieldAtOffset252 = *(Struct15Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy15BytesFromLocationAtOffset65520ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct15Bytes*)&dst->fieldAtOffset255 = *(Struct15Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy15BytesFromLocationAtOffset65520ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct15Bytes*)&dst->fieldAtOffset504 = *(Struct15Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy15BytesFromLocationAtOffset65520ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct15Bytes*)&dst->fieldAtOffset1008 = *(Struct15Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy15BytesFromLocationAtOffset65520ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct15Bytes*)&dst->fieldAtOffset4095 = *(Struct15Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy15BytesFromLocationAtOffset65520ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct15Bytes*)&dst->fieldAtOffset8190 = *(Struct15Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy15BytesFromLocationAtOffset65520ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct15Bytes*)&dst->fieldAtOffset16380 = *(Struct15Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy15BytesFromLocationAtOffset65520ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct15Bytes*)&dst->fieldAtOffset32760 = *(Struct15Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy15BytesFromLocationAtOffset65520ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct15Bytes*)&dst->fieldAtOffset65520 = *(Struct15Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy16BytesFromLocationAtOffset252ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct16Bytes*)&dst->fieldAtOffset252 = *(Struct16Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy16BytesFromLocationAtOffset252ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct16Bytes*)&dst->fieldAtOffset255 = *(Struct16Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy16BytesFromLocationAtOffset252ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct16Bytes*)&dst->fieldAtOffset504 = *(Struct16Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy16BytesFromLocationAtOffset252ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct16Bytes*)&dst->fieldAtOffset1008 = *(Struct16Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy16BytesFromLocationAtOffset252ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct16Bytes*)&dst->fieldAtOffset4095 = *(Struct16Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy16BytesFromLocationAtOffset252ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct16Bytes*)&dst->fieldAtOffset8190 = *(Struct16Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy16BytesFromLocationAtOffset252ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct16Bytes*)&dst->fieldAtOffset16380 = *(Struct16Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy16BytesFromLocationAtOffset252ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct16Bytes*)&dst->fieldAtOffset32760 = *(Struct16Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy16BytesFromLocationAtOffset252ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct16Bytes*)&dst->fieldAtOffset65520 = *(Struct16Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy16BytesFromLocationAtOffset255ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct16Bytes*)&dst->fieldAtOffset252 = *(Struct16Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy16BytesFromLocationAtOffset255ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct16Bytes*)&dst->fieldAtOffset255 = *(Struct16Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy16BytesFromLocationAtOffset255ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct16Bytes*)&dst->fieldAtOffset504 = *(Struct16Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy16BytesFromLocationAtOffset255ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct16Bytes*)&dst->fieldAtOffset1008 = *(Struct16Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy16BytesFromLocationAtOffset255ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct16Bytes*)&dst->fieldAtOffset4095 = *(Struct16Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy16BytesFromLocationAtOffset255ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct16Bytes*)&dst->fieldAtOffset8190 = *(Struct16Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy16BytesFromLocationAtOffset255ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct16Bytes*)&dst->fieldAtOffset16380 = *(Struct16Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy16BytesFromLocationAtOffset255ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct16Bytes*)&dst->fieldAtOffset32760 = *(Struct16Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy16BytesFromLocationAtOffset255ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct16Bytes*)&dst->fieldAtOffset65520 = *(Struct16Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy16BytesFromLocationAtOffset504ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct16Bytes*)&dst->fieldAtOffset252 = *(Struct16Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy16BytesFromLocationAtOffset504ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct16Bytes*)&dst->fieldAtOffset255 = *(Struct16Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy16BytesFromLocationAtOffset504ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct16Bytes*)&dst->fieldAtOffset504 = *(Struct16Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy16BytesFromLocationAtOffset504ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct16Bytes*)&dst->fieldAtOffset1008 = *(Struct16Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy16BytesFromLocationAtOffset504ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct16Bytes*)&dst->fieldAtOffset4095 = *(Struct16Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy16BytesFromLocationAtOffset504ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct16Bytes*)&dst->fieldAtOffset8190 = *(Struct16Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy16BytesFromLocationAtOffset504ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct16Bytes*)&dst->fieldAtOffset16380 = *(Struct16Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy16BytesFromLocationAtOffset504ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct16Bytes*)&dst->fieldAtOffset32760 = *(Struct16Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy16BytesFromLocationAtOffset504ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct16Bytes*)&dst->fieldAtOffset65520 = *(Struct16Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy16BytesFromLocationAtOffset1008ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct16Bytes*)&dst->fieldAtOffset252 = *(Struct16Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy16BytesFromLocationAtOffset1008ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct16Bytes*)&dst->fieldAtOffset255 = *(Struct16Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy16BytesFromLocationAtOffset1008ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct16Bytes*)&dst->fieldAtOffset504 = *(Struct16Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy16BytesFromLocationAtOffset1008ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct16Bytes*)&dst->fieldAtOffset1008 = *(Struct16Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy16BytesFromLocationAtOffset1008ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct16Bytes*)&dst->fieldAtOffset4095 = *(Struct16Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy16BytesFromLocationAtOffset1008ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct16Bytes*)&dst->fieldAtOffset8190 = *(Struct16Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy16BytesFromLocationAtOffset1008ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct16Bytes*)&dst->fieldAtOffset16380 = *(Struct16Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy16BytesFromLocationAtOffset1008ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct16Bytes*)&dst->fieldAtOffset32760 = *(Struct16Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy16BytesFromLocationAtOffset1008ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct16Bytes*)&dst->fieldAtOffset65520 = *(Struct16Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy16BytesFromLocationAtOffset4095ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct16Bytes*)&dst->fieldAtOffset252 = *(Struct16Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy16BytesFromLocationAtOffset4095ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct16Bytes*)&dst->fieldAtOffset255 = *(Struct16Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy16BytesFromLocationAtOffset4095ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct16Bytes*)&dst->fieldAtOffset504 = *(Struct16Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy16BytesFromLocationAtOffset4095ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct16Bytes*)&dst->fieldAtOffset1008 = *(Struct16Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy16BytesFromLocationAtOffset4095ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct16Bytes*)&dst->fieldAtOffset4095 = *(Struct16Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy16BytesFromLocationAtOffset4095ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct16Bytes*)&dst->fieldAtOffset8190 = *(Struct16Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy16BytesFromLocationAtOffset4095ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct16Bytes*)&dst->fieldAtOffset16380 = *(Struct16Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy16BytesFromLocationAtOffset4095ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct16Bytes*)&dst->fieldAtOffset32760 = *(Struct16Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy16BytesFromLocationAtOffset4095ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct16Bytes*)&dst->fieldAtOffset65520 = *(Struct16Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy16BytesFromLocationAtOffset8190ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct16Bytes*)&dst->fieldAtOffset252 = *(Struct16Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy16BytesFromLocationAtOffset8190ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct16Bytes*)&dst->fieldAtOffset255 = *(Struct16Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy16BytesFromLocationAtOffset8190ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct16Bytes*)&dst->fieldAtOffset504 = *(Struct16Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy16BytesFromLocationAtOffset8190ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct16Bytes*)&dst->fieldAtOffset1008 = *(Struct16Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy16BytesFromLocationAtOffset8190ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct16Bytes*)&dst->fieldAtOffset4095 = *(Struct16Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy16BytesFromLocationAtOffset8190ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct16Bytes*)&dst->fieldAtOffset8190 = *(Struct16Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy16BytesFromLocationAtOffset8190ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct16Bytes*)&dst->fieldAtOffset16380 = *(Struct16Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy16BytesFromLocationAtOffset8190ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct16Bytes*)&dst->fieldAtOffset32760 = *(Struct16Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy16BytesFromLocationAtOffset8190ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct16Bytes*)&dst->fieldAtOffset65520 = *(Struct16Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy16BytesFromLocationAtOffset16380ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct16Bytes*)&dst->fieldAtOffset252 = *(Struct16Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy16BytesFromLocationAtOffset16380ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct16Bytes*)&dst->fieldAtOffset255 = *(Struct16Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy16BytesFromLocationAtOffset16380ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct16Bytes*)&dst->fieldAtOffset504 = *(Struct16Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy16BytesFromLocationAtOffset16380ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct16Bytes*)&dst->fieldAtOffset1008 = *(Struct16Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy16BytesFromLocationAtOffset16380ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct16Bytes*)&dst->fieldAtOffset4095 = *(Struct16Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy16BytesFromLocationAtOffset16380ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct16Bytes*)&dst->fieldAtOffset8190 = *(Struct16Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy16BytesFromLocationAtOffset16380ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct16Bytes*)&dst->fieldAtOffset16380 = *(Struct16Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy16BytesFromLocationAtOffset16380ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct16Bytes*)&dst->fieldAtOffset32760 = *(Struct16Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy16BytesFromLocationAtOffset16380ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct16Bytes*)&dst->fieldAtOffset65520 = *(Struct16Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy16BytesFromLocationAtOffset32760ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct16Bytes*)&dst->fieldAtOffset252 = *(Struct16Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy16BytesFromLocationAtOffset32760ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct16Bytes*)&dst->fieldAtOffset255 = *(Struct16Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy16BytesFromLocationAtOffset32760ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct16Bytes*)&dst->fieldAtOffset504 = *(Struct16Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy16BytesFromLocationAtOffset32760ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct16Bytes*)&dst->fieldAtOffset1008 = *(Struct16Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy16BytesFromLocationAtOffset32760ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct16Bytes*)&dst->fieldAtOffset4095 = *(Struct16Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy16BytesFromLocationAtOffset32760ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct16Bytes*)&dst->fieldAtOffset8190 = *(Struct16Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy16BytesFromLocationAtOffset32760ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct16Bytes*)&dst->fieldAtOffset16380 = *(Struct16Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy16BytesFromLocationAtOffset32760ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct16Bytes*)&dst->fieldAtOffset32760 = *(Struct16Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy16BytesFromLocationAtOffset32760ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct16Bytes*)&dst->fieldAtOffset65520 = *(Struct16Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy16BytesFromLocationAtOffset65520ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct16Bytes*)&dst->fieldAtOffset252 = *(Struct16Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy16BytesFromLocationAtOffset65520ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct16Bytes*)&dst->fieldAtOffset255 = *(Struct16Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy16BytesFromLocationAtOffset65520ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct16Bytes*)&dst->fieldAtOffset504 = *(Struct16Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy16BytesFromLocationAtOffset65520ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct16Bytes*)&dst->fieldAtOffset1008 = *(Struct16Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy16BytesFromLocationAtOffset65520ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct16Bytes*)&dst->fieldAtOffset4095 = *(Struct16Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy16BytesFromLocationAtOffset65520ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct16Bytes*)&dst->fieldAtOffset8190 = *(Struct16Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy16BytesFromLocationAtOffset65520ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct16Bytes*)&dst->fieldAtOffset16380 = *(Struct16Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy16BytesFromLocationAtOffset65520ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct16Bytes*)&dst->fieldAtOffset32760 = *(Struct16Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy16BytesFromLocationAtOffset65520ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct16Bytes*)&dst->fieldAtOffset65520 = *(Struct16Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy31BytesFromLocationAtOffset252ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct31Bytes*)&dst->fieldAtOffset252 = *(Struct31Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy31BytesFromLocationAtOffset252ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct31Bytes*)&dst->fieldAtOffset255 = *(Struct31Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy31BytesFromLocationAtOffset252ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct31Bytes*)&dst->fieldAtOffset504 = *(Struct31Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy31BytesFromLocationAtOffset252ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct31Bytes*)&dst->fieldAtOffset1008 = *(Struct31Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy31BytesFromLocationAtOffset252ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct31Bytes*)&dst->fieldAtOffset4095 = *(Struct31Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy31BytesFromLocationAtOffset252ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct31Bytes*)&dst->fieldAtOffset8190 = *(Struct31Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy31BytesFromLocationAtOffset252ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct31Bytes*)&dst->fieldAtOffset16380 = *(Struct31Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy31BytesFromLocationAtOffset252ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct31Bytes*)&dst->fieldAtOffset32760 = *(Struct31Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy31BytesFromLocationAtOffset252ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct31Bytes*)&dst->fieldAtOffset65520 = *(Struct31Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy31BytesFromLocationAtOffset255ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct31Bytes*)&dst->fieldAtOffset252 = *(Struct31Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy31BytesFromLocationAtOffset255ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct31Bytes*)&dst->fieldAtOffset255 = *(Struct31Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy31BytesFromLocationAtOffset255ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct31Bytes*)&dst->fieldAtOffset504 = *(Struct31Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy31BytesFromLocationAtOffset255ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct31Bytes*)&dst->fieldAtOffset1008 = *(Struct31Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy31BytesFromLocationAtOffset255ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct31Bytes*)&dst->fieldAtOffset4095 = *(Struct31Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy31BytesFromLocationAtOffset255ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct31Bytes*)&dst->fieldAtOffset8190 = *(Struct31Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy31BytesFromLocationAtOffset255ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct31Bytes*)&dst->fieldAtOffset16380 = *(Struct31Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy31BytesFromLocationAtOffset255ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct31Bytes*)&dst->fieldAtOffset32760 = *(Struct31Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy31BytesFromLocationAtOffset255ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct31Bytes*)&dst->fieldAtOffset65520 = *(Struct31Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy31BytesFromLocationAtOffset504ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct31Bytes*)&dst->fieldAtOffset252 = *(Struct31Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy31BytesFromLocationAtOffset504ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct31Bytes*)&dst->fieldAtOffset255 = *(Struct31Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy31BytesFromLocationAtOffset504ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct31Bytes*)&dst->fieldAtOffset504 = *(Struct31Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy31BytesFromLocationAtOffset504ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct31Bytes*)&dst->fieldAtOffset1008 = *(Struct31Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy31BytesFromLocationAtOffset504ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct31Bytes*)&dst->fieldAtOffset4095 = *(Struct31Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy31BytesFromLocationAtOffset504ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct31Bytes*)&dst->fieldAtOffset8190 = *(Struct31Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy31BytesFromLocationAtOffset504ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct31Bytes*)&dst->fieldAtOffset16380 = *(Struct31Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy31BytesFromLocationAtOffset504ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct31Bytes*)&dst->fieldAtOffset32760 = *(Struct31Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy31BytesFromLocationAtOffset504ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct31Bytes*)&dst->fieldAtOffset65520 = *(Struct31Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy31BytesFromLocationAtOffset1008ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct31Bytes*)&dst->fieldAtOffset252 = *(Struct31Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy31BytesFromLocationAtOffset1008ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct31Bytes*)&dst->fieldAtOffset255 = *(Struct31Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy31BytesFromLocationAtOffset1008ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct31Bytes*)&dst->fieldAtOffset504 = *(Struct31Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy31BytesFromLocationAtOffset1008ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct31Bytes*)&dst->fieldAtOffset1008 = *(Struct31Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy31BytesFromLocationAtOffset1008ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct31Bytes*)&dst->fieldAtOffset4095 = *(Struct31Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy31BytesFromLocationAtOffset1008ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct31Bytes*)&dst->fieldAtOffset8190 = *(Struct31Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy31BytesFromLocationAtOffset1008ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct31Bytes*)&dst->fieldAtOffset16380 = *(Struct31Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy31BytesFromLocationAtOffset1008ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct31Bytes*)&dst->fieldAtOffset32760 = *(Struct31Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy31BytesFromLocationAtOffset1008ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct31Bytes*)&dst->fieldAtOffset65520 = *(Struct31Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy31BytesFromLocationAtOffset4095ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct31Bytes*)&dst->fieldAtOffset252 = *(Struct31Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy31BytesFromLocationAtOffset4095ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct31Bytes*)&dst->fieldAtOffset255 = *(Struct31Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy31BytesFromLocationAtOffset4095ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct31Bytes*)&dst->fieldAtOffset504 = *(Struct31Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy31BytesFromLocationAtOffset4095ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct31Bytes*)&dst->fieldAtOffset1008 = *(Struct31Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy31BytesFromLocationAtOffset4095ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct31Bytes*)&dst->fieldAtOffset4095 = *(Struct31Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy31BytesFromLocationAtOffset4095ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct31Bytes*)&dst->fieldAtOffset8190 = *(Struct31Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy31BytesFromLocationAtOffset4095ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct31Bytes*)&dst->fieldAtOffset16380 = *(Struct31Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy31BytesFromLocationAtOffset4095ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct31Bytes*)&dst->fieldAtOffset32760 = *(Struct31Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy31BytesFromLocationAtOffset4095ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct31Bytes*)&dst->fieldAtOffset65520 = *(Struct31Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy31BytesFromLocationAtOffset8190ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct31Bytes*)&dst->fieldAtOffset252 = *(Struct31Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy31BytesFromLocationAtOffset8190ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct31Bytes*)&dst->fieldAtOffset255 = *(Struct31Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy31BytesFromLocationAtOffset8190ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct31Bytes*)&dst->fieldAtOffset504 = *(Struct31Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy31BytesFromLocationAtOffset8190ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct31Bytes*)&dst->fieldAtOffset1008 = *(Struct31Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy31BytesFromLocationAtOffset8190ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct31Bytes*)&dst->fieldAtOffset4095 = *(Struct31Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy31BytesFromLocationAtOffset8190ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct31Bytes*)&dst->fieldAtOffset8190 = *(Struct31Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy31BytesFromLocationAtOffset8190ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct31Bytes*)&dst->fieldAtOffset16380 = *(Struct31Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy31BytesFromLocationAtOffset8190ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct31Bytes*)&dst->fieldAtOffset32760 = *(Struct31Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy31BytesFromLocationAtOffset8190ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct31Bytes*)&dst->fieldAtOffset65520 = *(Struct31Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy31BytesFromLocationAtOffset16380ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct31Bytes*)&dst->fieldAtOffset252 = *(Struct31Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy31BytesFromLocationAtOffset16380ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct31Bytes*)&dst->fieldAtOffset255 = *(Struct31Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy31BytesFromLocationAtOffset16380ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct31Bytes*)&dst->fieldAtOffset504 = *(Struct31Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy31BytesFromLocationAtOffset16380ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct31Bytes*)&dst->fieldAtOffset1008 = *(Struct31Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy31BytesFromLocationAtOffset16380ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct31Bytes*)&dst->fieldAtOffset4095 = *(Struct31Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy31BytesFromLocationAtOffset16380ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct31Bytes*)&dst->fieldAtOffset8190 = *(Struct31Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy31BytesFromLocationAtOffset16380ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct31Bytes*)&dst->fieldAtOffset16380 = *(Struct31Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy31BytesFromLocationAtOffset16380ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct31Bytes*)&dst->fieldAtOffset32760 = *(Struct31Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy31BytesFromLocationAtOffset16380ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct31Bytes*)&dst->fieldAtOffset65520 = *(Struct31Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy31BytesFromLocationAtOffset32760ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct31Bytes*)&dst->fieldAtOffset252 = *(Struct31Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy31BytesFromLocationAtOffset32760ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct31Bytes*)&dst->fieldAtOffset255 = *(Struct31Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy31BytesFromLocationAtOffset32760ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct31Bytes*)&dst->fieldAtOffset504 = *(Struct31Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy31BytesFromLocationAtOffset32760ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct31Bytes*)&dst->fieldAtOffset1008 = *(Struct31Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy31BytesFromLocationAtOffset32760ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct31Bytes*)&dst->fieldAtOffset4095 = *(Struct31Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy31BytesFromLocationAtOffset32760ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct31Bytes*)&dst->fieldAtOffset8190 = *(Struct31Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy31BytesFromLocationAtOffset32760ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct31Bytes*)&dst->fieldAtOffset16380 = *(Struct31Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy31BytesFromLocationAtOffset32760ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct31Bytes*)&dst->fieldAtOffset32760 = *(Struct31Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy31BytesFromLocationAtOffset32760ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct31Bytes*)&dst->fieldAtOffset65520 = *(Struct31Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy31BytesFromLocationAtOffset65520ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct31Bytes*)&dst->fieldAtOffset252 = *(Struct31Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy31BytesFromLocationAtOffset65520ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct31Bytes*)&dst->fieldAtOffset255 = *(Struct31Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy31BytesFromLocationAtOffset65520ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct31Bytes*)&dst->fieldAtOffset504 = *(Struct31Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy31BytesFromLocationAtOffset65520ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct31Bytes*)&dst->fieldAtOffset1008 = *(Struct31Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy31BytesFromLocationAtOffset65520ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct31Bytes*)&dst->fieldAtOffset4095 = *(Struct31Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy31BytesFromLocationAtOffset65520ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct31Bytes*)&dst->fieldAtOffset8190 = *(Struct31Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy31BytesFromLocationAtOffset65520ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct31Bytes*)&dst->fieldAtOffset16380 = *(Struct31Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy31BytesFromLocationAtOffset65520ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct31Bytes*)&dst->fieldAtOffset32760 = *(Struct31Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy31BytesFromLocationAtOffset65520ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct31Bytes*)&dst->fieldAtOffset65520 = *(Struct31Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy32BytesFromLocationAtOffset252ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct32Bytes*)&dst->fieldAtOffset252 = *(Struct32Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy32BytesFromLocationAtOffset252ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct32Bytes*)&dst->fieldAtOffset255 = *(Struct32Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy32BytesFromLocationAtOffset252ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct32Bytes*)&dst->fieldAtOffset504 = *(Struct32Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy32BytesFromLocationAtOffset252ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct32Bytes*)&dst->fieldAtOffset1008 = *(Struct32Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy32BytesFromLocationAtOffset252ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct32Bytes*)&dst->fieldAtOffset4095 = *(Struct32Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy32BytesFromLocationAtOffset252ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct32Bytes*)&dst->fieldAtOffset8190 = *(Struct32Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy32BytesFromLocationAtOffset252ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct32Bytes*)&dst->fieldAtOffset16380 = *(Struct32Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy32BytesFromLocationAtOffset252ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct32Bytes*)&dst->fieldAtOffset32760 = *(Struct32Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy32BytesFromLocationAtOffset252ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct32Bytes*)&dst->fieldAtOffset65520 = *(Struct32Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy32BytesFromLocationAtOffset255ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct32Bytes*)&dst->fieldAtOffset252 = *(Struct32Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy32BytesFromLocationAtOffset255ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct32Bytes*)&dst->fieldAtOffset255 = *(Struct32Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy32BytesFromLocationAtOffset255ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct32Bytes*)&dst->fieldAtOffset504 = *(Struct32Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy32BytesFromLocationAtOffset255ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct32Bytes*)&dst->fieldAtOffset1008 = *(Struct32Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy32BytesFromLocationAtOffset255ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct32Bytes*)&dst->fieldAtOffset4095 = *(Struct32Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy32BytesFromLocationAtOffset255ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct32Bytes*)&dst->fieldAtOffset8190 = *(Struct32Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy32BytesFromLocationAtOffset255ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct32Bytes*)&dst->fieldAtOffset16380 = *(Struct32Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy32BytesFromLocationAtOffset255ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct32Bytes*)&dst->fieldAtOffset32760 = *(Struct32Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy32BytesFromLocationAtOffset255ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct32Bytes*)&dst->fieldAtOffset65520 = *(Struct32Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy32BytesFromLocationAtOffset504ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct32Bytes*)&dst->fieldAtOffset252 = *(Struct32Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy32BytesFromLocationAtOffset504ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct32Bytes*)&dst->fieldAtOffset255 = *(Struct32Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy32BytesFromLocationAtOffset504ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct32Bytes*)&dst->fieldAtOffset504 = *(Struct32Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy32BytesFromLocationAtOffset504ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct32Bytes*)&dst->fieldAtOffset1008 = *(Struct32Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy32BytesFromLocationAtOffset504ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct32Bytes*)&dst->fieldAtOffset4095 = *(Struct32Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy32BytesFromLocationAtOffset504ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct32Bytes*)&dst->fieldAtOffset8190 = *(Struct32Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy32BytesFromLocationAtOffset504ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct32Bytes*)&dst->fieldAtOffset16380 = *(Struct32Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy32BytesFromLocationAtOffset504ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct32Bytes*)&dst->fieldAtOffset32760 = *(Struct32Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy32BytesFromLocationAtOffset504ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct32Bytes*)&dst->fieldAtOffset65520 = *(Struct32Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy32BytesFromLocationAtOffset1008ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct32Bytes*)&dst->fieldAtOffset252 = *(Struct32Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy32BytesFromLocationAtOffset1008ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct32Bytes*)&dst->fieldAtOffset255 = *(Struct32Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy32BytesFromLocationAtOffset1008ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct32Bytes*)&dst->fieldAtOffset504 = *(Struct32Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy32BytesFromLocationAtOffset1008ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct32Bytes*)&dst->fieldAtOffset1008 = *(Struct32Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy32BytesFromLocationAtOffset1008ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct32Bytes*)&dst->fieldAtOffset4095 = *(Struct32Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy32BytesFromLocationAtOffset1008ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct32Bytes*)&dst->fieldAtOffset8190 = *(Struct32Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy32BytesFromLocationAtOffset1008ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct32Bytes*)&dst->fieldAtOffset16380 = *(Struct32Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy32BytesFromLocationAtOffset1008ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct32Bytes*)&dst->fieldAtOffset32760 = *(Struct32Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy32BytesFromLocationAtOffset1008ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct32Bytes*)&dst->fieldAtOffset65520 = *(Struct32Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy32BytesFromLocationAtOffset4095ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct32Bytes*)&dst->fieldAtOffset252 = *(Struct32Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy32BytesFromLocationAtOffset4095ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct32Bytes*)&dst->fieldAtOffset255 = *(Struct32Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy32BytesFromLocationAtOffset4095ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct32Bytes*)&dst->fieldAtOffset504 = *(Struct32Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy32BytesFromLocationAtOffset4095ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct32Bytes*)&dst->fieldAtOffset1008 = *(Struct32Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy32BytesFromLocationAtOffset4095ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct32Bytes*)&dst->fieldAtOffset4095 = *(Struct32Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy32BytesFromLocationAtOffset4095ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct32Bytes*)&dst->fieldAtOffset8190 = *(Struct32Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy32BytesFromLocationAtOffset4095ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct32Bytes*)&dst->fieldAtOffset16380 = *(Struct32Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy32BytesFromLocationAtOffset4095ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct32Bytes*)&dst->fieldAtOffset32760 = *(Struct32Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy32BytesFromLocationAtOffset4095ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct32Bytes*)&dst->fieldAtOffset65520 = *(Struct32Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy32BytesFromLocationAtOffset8190ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct32Bytes*)&dst->fieldAtOffset252 = *(Struct32Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy32BytesFromLocationAtOffset8190ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct32Bytes*)&dst->fieldAtOffset255 = *(Struct32Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy32BytesFromLocationAtOffset8190ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct32Bytes*)&dst->fieldAtOffset504 = *(Struct32Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy32BytesFromLocationAtOffset8190ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct32Bytes*)&dst->fieldAtOffset1008 = *(Struct32Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy32BytesFromLocationAtOffset8190ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct32Bytes*)&dst->fieldAtOffset4095 = *(Struct32Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy32BytesFromLocationAtOffset8190ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct32Bytes*)&dst->fieldAtOffset8190 = *(Struct32Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy32BytesFromLocationAtOffset8190ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct32Bytes*)&dst->fieldAtOffset16380 = *(Struct32Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy32BytesFromLocationAtOffset8190ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct32Bytes*)&dst->fieldAtOffset32760 = *(Struct32Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy32BytesFromLocationAtOffset8190ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct32Bytes*)&dst->fieldAtOffset65520 = *(Struct32Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy32BytesFromLocationAtOffset16380ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct32Bytes*)&dst->fieldAtOffset252 = *(Struct32Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy32BytesFromLocationAtOffset16380ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct32Bytes*)&dst->fieldAtOffset255 = *(Struct32Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy32BytesFromLocationAtOffset16380ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct32Bytes*)&dst->fieldAtOffset504 = *(Struct32Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy32BytesFromLocationAtOffset16380ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct32Bytes*)&dst->fieldAtOffset1008 = *(Struct32Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy32BytesFromLocationAtOffset16380ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct32Bytes*)&dst->fieldAtOffset4095 = *(Struct32Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy32BytesFromLocationAtOffset16380ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct32Bytes*)&dst->fieldAtOffset8190 = *(Struct32Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy32BytesFromLocationAtOffset16380ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct32Bytes*)&dst->fieldAtOffset16380 = *(Struct32Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy32BytesFromLocationAtOffset16380ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct32Bytes*)&dst->fieldAtOffset32760 = *(Struct32Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy32BytesFromLocationAtOffset16380ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct32Bytes*)&dst->fieldAtOffset65520 = *(Struct32Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy32BytesFromLocationAtOffset32760ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct32Bytes*)&dst->fieldAtOffset252 = *(Struct32Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy32BytesFromLocationAtOffset32760ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct32Bytes*)&dst->fieldAtOffset255 = *(Struct32Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy32BytesFromLocationAtOffset32760ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct32Bytes*)&dst->fieldAtOffset504 = *(Struct32Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy32BytesFromLocationAtOffset32760ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct32Bytes*)&dst->fieldAtOffset1008 = *(Struct32Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy32BytesFromLocationAtOffset32760ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct32Bytes*)&dst->fieldAtOffset4095 = *(Struct32Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy32BytesFromLocationAtOffset32760ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct32Bytes*)&dst->fieldAtOffset8190 = *(Struct32Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy32BytesFromLocationAtOffset32760ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct32Bytes*)&dst->fieldAtOffset16380 = *(Struct32Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy32BytesFromLocationAtOffset32760ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct32Bytes*)&dst->fieldAtOffset32760 = *(Struct32Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy32BytesFromLocationAtOffset32760ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct32Bytes*)&dst->fieldAtOffset65520 = *(Struct32Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy32BytesFromLocationAtOffset65520ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct32Bytes*)&dst->fieldAtOffset252 = *(Struct32Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy32BytesFromLocationAtOffset65520ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct32Bytes*)&dst->fieldAtOffset255 = *(Struct32Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy32BytesFromLocationAtOffset65520ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct32Bytes*)&dst->fieldAtOffset504 = *(Struct32Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy32BytesFromLocationAtOffset65520ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct32Bytes*)&dst->fieldAtOffset1008 = *(Struct32Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy32BytesFromLocationAtOffset65520ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct32Bytes*)&dst->fieldAtOffset4095 = *(Struct32Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy32BytesFromLocationAtOffset65520ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct32Bytes*)&dst->fieldAtOffset8190 = *(Struct32Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy32BytesFromLocationAtOffset65520ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct32Bytes*)&dst->fieldAtOffset16380 = *(Struct32Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy32BytesFromLocationAtOffset65520ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct32Bytes*)&dst->fieldAtOffset32760 = *(Struct32Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy32BytesFromLocationAtOffset65520ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct32Bytes*)&dst->fieldAtOffset65520 = *(Struct32Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy63BytesFromLocationAtOffset252ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct63Bytes*)&dst->fieldAtOffset252 = *(Struct63Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy63BytesFromLocationAtOffset252ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct63Bytes*)&dst->fieldAtOffset255 = *(Struct63Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy63BytesFromLocationAtOffset252ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct63Bytes*)&dst->fieldAtOffset504 = *(Struct63Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy63BytesFromLocationAtOffset252ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct63Bytes*)&dst->fieldAtOffset1008 = *(Struct63Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy63BytesFromLocationAtOffset252ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct63Bytes*)&dst->fieldAtOffset4095 = *(Struct63Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy63BytesFromLocationAtOffset252ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct63Bytes*)&dst->fieldAtOffset8190 = *(Struct63Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy63BytesFromLocationAtOffset252ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct63Bytes*)&dst->fieldAtOffset16380 = *(Struct63Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy63BytesFromLocationAtOffset252ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct63Bytes*)&dst->fieldAtOffset32760 = *(Struct63Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy63BytesFromLocationAtOffset252ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct63Bytes*)&dst->fieldAtOffset65520 = *(Struct63Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy63BytesFromLocationAtOffset255ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct63Bytes*)&dst->fieldAtOffset252 = *(Struct63Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy63BytesFromLocationAtOffset255ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct63Bytes*)&dst->fieldAtOffset255 = *(Struct63Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy63BytesFromLocationAtOffset255ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct63Bytes*)&dst->fieldAtOffset504 = *(Struct63Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy63BytesFromLocationAtOffset255ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct63Bytes*)&dst->fieldAtOffset1008 = *(Struct63Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy63BytesFromLocationAtOffset255ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct63Bytes*)&dst->fieldAtOffset4095 = *(Struct63Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy63BytesFromLocationAtOffset255ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct63Bytes*)&dst->fieldAtOffset8190 = *(Struct63Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy63BytesFromLocationAtOffset255ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct63Bytes*)&dst->fieldAtOffset16380 = *(Struct63Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy63BytesFromLocationAtOffset255ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct63Bytes*)&dst->fieldAtOffset32760 = *(Struct63Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy63BytesFromLocationAtOffset255ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct63Bytes*)&dst->fieldAtOffset65520 = *(Struct63Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy63BytesFromLocationAtOffset504ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct63Bytes*)&dst->fieldAtOffset252 = *(Struct63Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy63BytesFromLocationAtOffset504ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct63Bytes*)&dst->fieldAtOffset255 = *(Struct63Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy63BytesFromLocationAtOffset504ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct63Bytes*)&dst->fieldAtOffset504 = *(Struct63Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy63BytesFromLocationAtOffset504ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct63Bytes*)&dst->fieldAtOffset1008 = *(Struct63Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy63BytesFromLocationAtOffset504ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct63Bytes*)&dst->fieldAtOffset4095 = *(Struct63Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy63BytesFromLocationAtOffset504ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct63Bytes*)&dst->fieldAtOffset8190 = *(Struct63Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy63BytesFromLocationAtOffset504ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct63Bytes*)&dst->fieldAtOffset16380 = *(Struct63Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy63BytesFromLocationAtOffset504ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct63Bytes*)&dst->fieldAtOffset32760 = *(Struct63Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy63BytesFromLocationAtOffset504ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct63Bytes*)&dst->fieldAtOffset65520 = *(Struct63Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy63BytesFromLocationAtOffset1008ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct63Bytes*)&dst->fieldAtOffset252 = *(Struct63Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy63BytesFromLocationAtOffset1008ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct63Bytes*)&dst->fieldAtOffset255 = *(Struct63Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy63BytesFromLocationAtOffset1008ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct63Bytes*)&dst->fieldAtOffset504 = *(Struct63Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy63BytesFromLocationAtOffset1008ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct63Bytes*)&dst->fieldAtOffset1008 = *(Struct63Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy63BytesFromLocationAtOffset1008ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct63Bytes*)&dst->fieldAtOffset4095 = *(Struct63Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy63BytesFromLocationAtOffset1008ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct63Bytes*)&dst->fieldAtOffset8190 = *(Struct63Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy63BytesFromLocationAtOffset1008ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct63Bytes*)&dst->fieldAtOffset16380 = *(Struct63Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy63BytesFromLocationAtOffset1008ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct63Bytes*)&dst->fieldAtOffset32760 = *(Struct63Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy63BytesFromLocationAtOffset1008ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct63Bytes*)&dst->fieldAtOffset65520 = *(Struct63Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy63BytesFromLocationAtOffset4095ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct63Bytes*)&dst->fieldAtOffset252 = *(Struct63Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy63BytesFromLocationAtOffset4095ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct63Bytes*)&dst->fieldAtOffset255 = *(Struct63Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy63BytesFromLocationAtOffset4095ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct63Bytes*)&dst->fieldAtOffset504 = *(Struct63Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy63BytesFromLocationAtOffset4095ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct63Bytes*)&dst->fieldAtOffset1008 = *(Struct63Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy63BytesFromLocationAtOffset4095ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct63Bytes*)&dst->fieldAtOffset4095 = *(Struct63Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy63BytesFromLocationAtOffset4095ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct63Bytes*)&dst->fieldAtOffset8190 = *(Struct63Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy63BytesFromLocationAtOffset4095ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct63Bytes*)&dst->fieldAtOffset16380 = *(Struct63Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy63BytesFromLocationAtOffset4095ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct63Bytes*)&dst->fieldAtOffset32760 = *(Struct63Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy63BytesFromLocationAtOffset4095ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct63Bytes*)&dst->fieldAtOffset65520 = *(Struct63Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy63BytesFromLocationAtOffset8190ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct63Bytes*)&dst->fieldAtOffset252 = *(Struct63Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy63BytesFromLocationAtOffset8190ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct63Bytes*)&dst->fieldAtOffset255 = *(Struct63Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy63BytesFromLocationAtOffset8190ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct63Bytes*)&dst->fieldAtOffset504 = *(Struct63Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy63BytesFromLocationAtOffset8190ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct63Bytes*)&dst->fieldAtOffset1008 = *(Struct63Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy63BytesFromLocationAtOffset8190ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct63Bytes*)&dst->fieldAtOffset4095 = *(Struct63Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy63BytesFromLocationAtOffset8190ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct63Bytes*)&dst->fieldAtOffset8190 = *(Struct63Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy63BytesFromLocationAtOffset8190ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct63Bytes*)&dst->fieldAtOffset16380 = *(Struct63Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy63BytesFromLocationAtOffset8190ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct63Bytes*)&dst->fieldAtOffset32760 = *(Struct63Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy63BytesFromLocationAtOffset8190ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct63Bytes*)&dst->fieldAtOffset65520 = *(Struct63Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy63BytesFromLocationAtOffset16380ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct63Bytes*)&dst->fieldAtOffset252 = *(Struct63Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy63BytesFromLocationAtOffset16380ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct63Bytes*)&dst->fieldAtOffset255 = *(Struct63Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy63BytesFromLocationAtOffset16380ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct63Bytes*)&dst->fieldAtOffset504 = *(Struct63Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy63BytesFromLocationAtOffset16380ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct63Bytes*)&dst->fieldAtOffset1008 = *(Struct63Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy63BytesFromLocationAtOffset16380ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct63Bytes*)&dst->fieldAtOffset4095 = *(Struct63Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy63BytesFromLocationAtOffset16380ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct63Bytes*)&dst->fieldAtOffset8190 = *(Struct63Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy63BytesFromLocationAtOffset16380ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct63Bytes*)&dst->fieldAtOffset16380 = *(Struct63Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy63BytesFromLocationAtOffset16380ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct63Bytes*)&dst->fieldAtOffset32760 = *(Struct63Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy63BytesFromLocationAtOffset16380ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct63Bytes*)&dst->fieldAtOffset65520 = *(Struct63Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy63BytesFromLocationAtOffset32760ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct63Bytes*)&dst->fieldAtOffset252 = *(Struct63Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy63BytesFromLocationAtOffset32760ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct63Bytes*)&dst->fieldAtOffset255 = *(Struct63Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy63BytesFromLocationAtOffset32760ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct63Bytes*)&dst->fieldAtOffset504 = *(Struct63Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy63BytesFromLocationAtOffset32760ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct63Bytes*)&dst->fieldAtOffset1008 = *(Struct63Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy63BytesFromLocationAtOffset32760ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct63Bytes*)&dst->fieldAtOffset4095 = *(Struct63Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy63BytesFromLocationAtOffset32760ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct63Bytes*)&dst->fieldAtOffset8190 = *(Struct63Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy63BytesFromLocationAtOffset32760ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct63Bytes*)&dst->fieldAtOffset16380 = *(Struct63Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy63BytesFromLocationAtOffset32760ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct63Bytes*)&dst->fieldAtOffset32760 = *(Struct63Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy63BytesFromLocationAtOffset32760ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct63Bytes*)&dst->fieldAtOffset65520 = *(Struct63Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy63BytesFromLocationAtOffset65520ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct63Bytes*)&dst->fieldAtOffset252 = *(Struct63Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy63BytesFromLocationAtOffset65520ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct63Bytes*)&dst->fieldAtOffset255 = *(Struct63Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy63BytesFromLocationAtOffset65520ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct63Bytes*)&dst->fieldAtOffset504 = *(Struct63Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy63BytesFromLocationAtOffset65520ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct63Bytes*)&dst->fieldAtOffset1008 = *(Struct63Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy63BytesFromLocationAtOffset65520ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct63Bytes*)&dst->fieldAtOffset4095 = *(Struct63Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy63BytesFromLocationAtOffset65520ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct63Bytes*)&dst->fieldAtOffset8190 = *(Struct63Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy63BytesFromLocationAtOffset65520ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct63Bytes*)&dst->fieldAtOffset16380 = *(Struct63Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy63BytesFromLocationAtOffset65520ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct63Bytes*)&dst->fieldAtOffset32760 = *(Struct63Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy63BytesFromLocationAtOffset65520ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct63Bytes*)&dst->fieldAtOffset65520 = *(Struct63Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy64BytesFromLocationAtOffset252ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct64Bytes*)&dst->fieldAtOffset252 = *(Struct64Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy64BytesFromLocationAtOffset252ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct64Bytes*)&dst->fieldAtOffset255 = *(Struct64Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy64BytesFromLocationAtOffset252ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct64Bytes*)&dst->fieldAtOffset504 = *(Struct64Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy64BytesFromLocationAtOffset252ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct64Bytes*)&dst->fieldAtOffset1008 = *(Struct64Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy64BytesFromLocationAtOffset252ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct64Bytes*)&dst->fieldAtOffset4095 = *(Struct64Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy64BytesFromLocationAtOffset252ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct64Bytes*)&dst->fieldAtOffset8190 = *(Struct64Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy64BytesFromLocationAtOffset252ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct64Bytes*)&dst->fieldAtOffset16380 = *(Struct64Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy64BytesFromLocationAtOffset252ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct64Bytes*)&dst->fieldAtOffset32760 = *(Struct64Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy64BytesFromLocationAtOffset252ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct64Bytes*)&dst->fieldAtOffset65520 = *(Struct64Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy64BytesFromLocationAtOffset255ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct64Bytes*)&dst->fieldAtOffset252 = *(Struct64Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy64BytesFromLocationAtOffset255ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct64Bytes*)&dst->fieldAtOffset255 = *(Struct64Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy64BytesFromLocationAtOffset255ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct64Bytes*)&dst->fieldAtOffset504 = *(Struct64Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy64BytesFromLocationAtOffset255ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct64Bytes*)&dst->fieldAtOffset1008 = *(Struct64Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy64BytesFromLocationAtOffset255ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct64Bytes*)&dst->fieldAtOffset4095 = *(Struct64Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy64BytesFromLocationAtOffset255ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct64Bytes*)&dst->fieldAtOffset8190 = *(Struct64Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy64BytesFromLocationAtOffset255ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct64Bytes*)&dst->fieldAtOffset16380 = *(Struct64Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy64BytesFromLocationAtOffset255ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct64Bytes*)&dst->fieldAtOffset32760 = *(Struct64Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy64BytesFromLocationAtOffset255ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct64Bytes*)&dst->fieldAtOffset65520 = *(Struct64Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy64BytesFromLocationAtOffset504ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct64Bytes*)&dst->fieldAtOffset252 = *(Struct64Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy64BytesFromLocationAtOffset504ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct64Bytes*)&dst->fieldAtOffset255 = *(Struct64Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy64BytesFromLocationAtOffset504ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct64Bytes*)&dst->fieldAtOffset504 = *(Struct64Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy64BytesFromLocationAtOffset504ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct64Bytes*)&dst->fieldAtOffset1008 = *(Struct64Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy64BytesFromLocationAtOffset504ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct64Bytes*)&dst->fieldAtOffset4095 = *(Struct64Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy64BytesFromLocationAtOffset504ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct64Bytes*)&dst->fieldAtOffset8190 = *(Struct64Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy64BytesFromLocationAtOffset504ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct64Bytes*)&dst->fieldAtOffset16380 = *(Struct64Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy64BytesFromLocationAtOffset504ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct64Bytes*)&dst->fieldAtOffset32760 = *(Struct64Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy64BytesFromLocationAtOffset504ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct64Bytes*)&dst->fieldAtOffset65520 = *(Struct64Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy64BytesFromLocationAtOffset1008ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct64Bytes*)&dst->fieldAtOffset252 = *(Struct64Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy64BytesFromLocationAtOffset1008ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct64Bytes*)&dst->fieldAtOffset255 = *(Struct64Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy64BytesFromLocationAtOffset1008ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct64Bytes*)&dst->fieldAtOffset504 = *(Struct64Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy64BytesFromLocationAtOffset1008ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct64Bytes*)&dst->fieldAtOffset1008 = *(Struct64Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy64BytesFromLocationAtOffset1008ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct64Bytes*)&dst->fieldAtOffset4095 = *(Struct64Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy64BytesFromLocationAtOffset1008ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct64Bytes*)&dst->fieldAtOffset8190 = *(Struct64Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy64BytesFromLocationAtOffset1008ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct64Bytes*)&dst->fieldAtOffset16380 = *(Struct64Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy64BytesFromLocationAtOffset1008ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct64Bytes*)&dst->fieldAtOffset32760 = *(Struct64Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy64BytesFromLocationAtOffset1008ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct64Bytes*)&dst->fieldAtOffset65520 = *(Struct64Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy64BytesFromLocationAtOffset4095ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct64Bytes*)&dst->fieldAtOffset252 = *(Struct64Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy64BytesFromLocationAtOffset4095ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct64Bytes*)&dst->fieldAtOffset255 = *(Struct64Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy64BytesFromLocationAtOffset4095ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct64Bytes*)&dst->fieldAtOffset504 = *(Struct64Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy64BytesFromLocationAtOffset4095ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct64Bytes*)&dst->fieldAtOffset1008 = *(Struct64Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy64BytesFromLocationAtOffset4095ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct64Bytes*)&dst->fieldAtOffset4095 = *(Struct64Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy64BytesFromLocationAtOffset4095ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct64Bytes*)&dst->fieldAtOffset8190 = *(Struct64Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy64BytesFromLocationAtOffset4095ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct64Bytes*)&dst->fieldAtOffset16380 = *(Struct64Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy64BytesFromLocationAtOffset4095ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct64Bytes*)&dst->fieldAtOffset32760 = *(Struct64Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy64BytesFromLocationAtOffset4095ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct64Bytes*)&dst->fieldAtOffset65520 = *(Struct64Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy64BytesFromLocationAtOffset8190ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct64Bytes*)&dst->fieldAtOffset252 = *(Struct64Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy64BytesFromLocationAtOffset8190ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct64Bytes*)&dst->fieldAtOffset255 = *(Struct64Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy64BytesFromLocationAtOffset8190ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct64Bytes*)&dst->fieldAtOffset504 = *(Struct64Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy64BytesFromLocationAtOffset8190ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct64Bytes*)&dst->fieldAtOffset1008 = *(Struct64Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy64BytesFromLocationAtOffset8190ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct64Bytes*)&dst->fieldAtOffset4095 = *(Struct64Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy64BytesFromLocationAtOffset8190ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct64Bytes*)&dst->fieldAtOffset8190 = *(Struct64Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy64BytesFromLocationAtOffset8190ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct64Bytes*)&dst->fieldAtOffset16380 = *(Struct64Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy64BytesFromLocationAtOffset8190ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct64Bytes*)&dst->fieldAtOffset32760 = *(Struct64Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy64BytesFromLocationAtOffset8190ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct64Bytes*)&dst->fieldAtOffset65520 = *(Struct64Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy64BytesFromLocationAtOffset16380ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct64Bytes*)&dst->fieldAtOffset252 = *(Struct64Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy64BytesFromLocationAtOffset16380ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct64Bytes*)&dst->fieldAtOffset255 = *(Struct64Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy64BytesFromLocationAtOffset16380ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct64Bytes*)&dst->fieldAtOffset504 = *(Struct64Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy64BytesFromLocationAtOffset16380ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct64Bytes*)&dst->fieldAtOffset1008 = *(Struct64Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy64BytesFromLocationAtOffset16380ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct64Bytes*)&dst->fieldAtOffset4095 = *(Struct64Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy64BytesFromLocationAtOffset16380ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct64Bytes*)&dst->fieldAtOffset8190 = *(Struct64Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy64BytesFromLocationAtOffset16380ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct64Bytes*)&dst->fieldAtOffset16380 = *(Struct64Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy64BytesFromLocationAtOffset16380ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct64Bytes*)&dst->fieldAtOffset32760 = *(Struct64Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy64BytesFromLocationAtOffset16380ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct64Bytes*)&dst->fieldAtOffset65520 = *(Struct64Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy64BytesFromLocationAtOffset32760ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct64Bytes*)&dst->fieldAtOffset252 = *(Struct64Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy64BytesFromLocationAtOffset32760ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct64Bytes*)&dst->fieldAtOffset255 = *(Struct64Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy64BytesFromLocationAtOffset32760ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct64Bytes*)&dst->fieldAtOffset504 = *(Struct64Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy64BytesFromLocationAtOffset32760ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct64Bytes*)&dst->fieldAtOffset1008 = *(Struct64Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy64BytesFromLocationAtOffset32760ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct64Bytes*)&dst->fieldAtOffset4095 = *(Struct64Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy64BytesFromLocationAtOffset32760ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct64Bytes*)&dst->fieldAtOffset8190 = *(Struct64Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy64BytesFromLocationAtOffset32760ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct64Bytes*)&dst->fieldAtOffset16380 = *(Struct64Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy64BytesFromLocationAtOffset32760ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct64Bytes*)&dst->fieldAtOffset32760 = *(Struct64Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy64BytesFromLocationAtOffset32760ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct64Bytes*)&dst->fieldAtOffset65520 = *(Struct64Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy64BytesFromLocationAtOffset65520ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct64Bytes*)&dst->fieldAtOffset252 = *(Struct64Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy64BytesFromLocationAtOffset65520ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct64Bytes*)&dst->fieldAtOffset255 = *(Struct64Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy64BytesFromLocationAtOffset65520ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct64Bytes*)&dst->fieldAtOffset504 = *(Struct64Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy64BytesFromLocationAtOffset65520ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct64Bytes*)&dst->fieldAtOffset1008 = *(Struct64Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy64BytesFromLocationAtOffset65520ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct64Bytes*)&dst->fieldAtOffset4095 = *(Struct64Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy64BytesFromLocationAtOffset65520ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct64Bytes*)&dst->fieldAtOffset8190 = *(Struct64Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy64BytesFromLocationAtOffset65520ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct64Bytes*)&dst->fieldAtOffset16380 = *(Struct64Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy64BytesFromLocationAtOffset65520ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct64Bytes*)&dst->fieldAtOffset32760 = *(Struct64Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy64BytesFromLocationAtOffset65520ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct64Bytes*)&dst->fieldAtOffset65520 = *(Struct64Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy127BytesFromLocationAtOffset252ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct127Bytes*)&dst->fieldAtOffset252 = *(Struct127Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy127BytesFromLocationAtOffset252ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct127Bytes*)&dst->fieldAtOffset255 = *(Struct127Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy127BytesFromLocationAtOffset252ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct127Bytes*)&dst->fieldAtOffset504 = *(Struct127Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy127BytesFromLocationAtOffset252ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct127Bytes*)&dst->fieldAtOffset1008 = *(Struct127Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy127BytesFromLocationAtOffset252ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct127Bytes*)&dst->fieldAtOffset4095 = *(Struct127Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy127BytesFromLocationAtOffset252ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct127Bytes*)&dst->fieldAtOffset8190 = *(Struct127Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy127BytesFromLocationAtOffset252ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct127Bytes*)&dst->fieldAtOffset16380 = *(Struct127Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy127BytesFromLocationAtOffset252ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct127Bytes*)&dst->fieldAtOffset32760 = *(Struct127Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy127BytesFromLocationAtOffset252ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct127Bytes*)&dst->fieldAtOffset65520 = *(Struct127Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy127BytesFromLocationAtOffset255ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct127Bytes*)&dst->fieldAtOffset252 = *(Struct127Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy127BytesFromLocationAtOffset255ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct127Bytes*)&dst->fieldAtOffset255 = *(Struct127Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy127BytesFromLocationAtOffset255ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct127Bytes*)&dst->fieldAtOffset504 = *(Struct127Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy127BytesFromLocationAtOffset255ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct127Bytes*)&dst->fieldAtOffset1008 = *(Struct127Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy127BytesFromLocationAtOffset255ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct127Bytes*)&dst->fieldAtOffset4095 = *(Struct127Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy127BytesFromLocationAtOffset255ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct127Bytes*)&dst->fieldAtOffset8190 = *(Struct127Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy127BytesFromLocationAtOffset255ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct127Bytes*)&dst->fieldAtOffset16380 = *(Struct127Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy127BytesFromLocationAtOffset255ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct127Bytes*)&dst->fieldAtOffset32760 = *(Struct127Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy127BytesFromLocationAtOffset255ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct127Bytes*)&dst->fieldAtOffset65520 = *(Struct127Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy127BytesFromLocationAtOffset504ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct127Bytes*)&dst->fieldAtOffset252 = *(Struct127Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy127BytesFromLocationAtOffset504ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct127Bytes*)&dst->fieldAtOffset255 = *(Struct127Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy127BytesFromLocationAtOffset504ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct127Bytes*)&dst->fieldAtOffset504 = *(Struct127Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy127BytesFromLocationAtOffset504ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct127Bytes*)&dst->fieldAtOffset1008 = *(Struct127Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy127BytesFromLocationAtOffset504ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct127Bytes*)&dst->fieldAtOffset4095 = *(Struct127Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy127BytesFromLocationAtOffset504ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct127Bytes*)&dst->fieldAtOffset8190 = *(Struct127Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy127BytesFromLocationAtOffset504ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct127Bytes*)&dst->fieldAtOffset16380 = *(Struct127Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy127BytesFromLocationAtOffset504ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct127Bytes*)&dst->fieldAtOffset32760 = *(Struct127Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy127BytesFromLocationAtOffset504ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct127Bytes*)&dst->fieldAtOffset65520 = *(Struct127Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy127BytesFromLocationAtOffset1008ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct127Bytes*)&dst->fieldAtOffset252 = *(Struct127Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy127BytesFromLocationAtOffset1008ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct127Bytes*)&dst->fieldAtOffset255 = *(Struct127Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy127BytesFromLocationAtOffset1008ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct127Bytes*)&dst->fieldAtOffset504 = *(Struct127Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy127BytesFromLocationAtOffset1008ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct127Bytes*)&dst->fieldAtOffset1008 = *(Struct127Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy127BytesFromLocationAtOffset1008ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct127Bytes*)&dst->fieldAtOffset4095 = *(Struct127Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy127BytesFromLocationAtOffset1008ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct127Bytes*)&dst->fieldAtOffset8190 = *(Struct127Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy127BytesFromLocationAtOffset1008ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct127Bytes*)&dst->fieldAtOffset16380 = *(Struct127Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy127BytesFromLocationAtOffset1008ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct127Bytes*)&dst->fieldAtOffset32760 = *(Struct127Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy127BytesFromLocationAtOffset1008ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct127Bytes*)&dst->fieldAtOffset65520 = *(Struct127Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy127BytesFromLocationAtOffset4095ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct127Bytes*)&dst->fieldAtOffset252 = *(Struct127Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy127BytesFromLocationAtOffset4095ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct127Bytes*)&dst->fieldAtOffset255 = *(Struct127Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy127BytesFromLocationAtOffset4095ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct127Bytes*)&dst->fieldAtOffset504 = *(Struct127Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy127BytesFromLocationAtOffset4095ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct127Bytes*)&dst->fieldAtOffset1008 = *(Struct127Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy127BytesFromLocationAtOffset4095ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct127Bytes*)&dst->fieldAtOffset4095 = *(Struct127Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy127BytesFromLocationAtOffset4095ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct127Bytes*)&dst->fieldAtOffset8190 = *(Struct127Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy127BytesFromLocationAtOffset4095ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct127Bytes*)&dst->fieldAtOffset16380 = *(Struct127Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy127BytesFromLocationAtOffset4095ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct127Bytes*)&dst->fieldAtOffset32760 = *(Struct127Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy127BytesFromLocationAtOffset4095ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct127Bytes*)&dst->fieldAtOffset65520 = *(Struct127Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy127BytesFromLocationAtOffset8190ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct127Bytes*)&dst->fieldAtOffset252 = *(Struct127Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy127BytesFromLocationAtOffset8190ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct127Bytes*)&dst->fieldAtOffset255 = *(Struct127Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy127BytesFromLocationAtOffset8190ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct127Bytes*)&dst->fieldAtOffset504 = *(Struct127Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy127BytesFromLocationAtOffset8190ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct127Bytes*)&dst->fieldAtOffset1008 = *(Struct127Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy127BytesFromLocationAtOffset8190ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct127Bytes*)&dst->fieldAtOffset4095 = *(Struct127Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy127BytesFromLocationAtOffset8190ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct127Bytes*)&dst->fieldAtOffset8190 = *(Struct127Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy127BytesFromLocationAtOffset8190ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct127Bytes*)&dst->fieldAtOffset16380 = *(Struct127Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy127BytesFromLocationAtOffset8190ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct127Bytes*)&dst->fieldAtOffset32760 = *(Struct127Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy127BytesFromLocationAtOffset8190ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct127Bytes*)&dst->fieldAtOffset65520 = *(Struct127Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy127BytesFromLocationAtOffset16380ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct127Bytes*)&dst->fieldAtOffset252 = *(Struct127Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy127BytesFromLocationAtOffset16380ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct127Bytes*)&dst->fieldAtOffset255 = *(Struct127Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy127BytesFromLocationAtOffset16380ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct127Bytes*)&dst->fieldAtOffset504 = *(Struct127Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy127BytesFromLocationAtOffset16380ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct127Bytes*)&dst->fieldAtOffset1008 = *(Struct127Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy127BytesFromLocationAtOffset16380ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct127Bytes*)&dst->fieldAtOffset4095 = *(Struct127Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy127BytesFromLocationAtOffset16380ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct127Bytes*)&dst->fieldAtOffset8190 = *(Struct127Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy127BytesFromLocationAtOffset16380ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct127Bytes*)&dst->fieldAtOffset16380 = *(Struct127Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy127BytesFromLocationAtOffset16380ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct127Bytes*)&dst->fieldAtOffset32760 = *(Struct127Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy127BytesFromLocationAtOffset16380ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct127Bytes*)&dst->fieldAtOffset65520 = *(Struct127Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy127BytesFromLocationAtOffset32760ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct127Bytes*)&dst->fieldAtOffset252 = *(Struct127Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy127BytesFromLocationAtOffset32760ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct127Bytes*)&dst->fieldAtOffset255 = *(Struct127Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy127BytesFromLocationAtOffset32760ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct127Bytes*)&dst->fieldAtOffset504 = *(Struct127Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy127BytesFromLocationAtOffset32760ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct127Bytes*)&dst->fieldAtOffset1008 = *(Struct127Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy127BytesFromLocationAtOffset32760ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct127Bytes*)&dst->fieldAtOffset4095 = *(Struct127Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy127BytesFromLocationAtOffset32760ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct127Bytes*)&dst->fieldAtOffset8190 = *(Struct127Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy127BytesFromLocationAtOffset32760ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct127Bytes*)&dst->fieldAtOffset16380 = *(Struct127Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy127BytesFromLocationAtOffset32760ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct127Bytes*)&dst->fieldAtOffset32760 = *(Struct127Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy127BytesFromLocationAtOffset32760ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct127Bytes*)&dst->fieldAtOffset65520 = *(Struct127Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy127BytesFromLocationAtOffset65520ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct127Bytes*)&dst->fieldAtOffset252 = *(Struct127Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy127BytesFromLocationAtOffset65520ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct127Bytes*)&dst->fieldAtOffset255 = *(Struct127Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy127BytesFromLocationAtOffset65520ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct127Bytes*)&dst->fieldAtOffset504 = *(Struct127Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy127BytesFromLocationAtOffset65520ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct127Bytes*)&dst->fieldAtOffset1008 = *(Struct127Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy127BytesFromLocationAtOffset65520ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct127Bytes*)&dst->fieldAtOffset4095 = *(Struct127Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy127BytesFromLocationAtOffset65520ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct127Bytes*)&dst->fieldAtOffset8190 = *(Struct127Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy127BytesFromLocationAtOffset65520ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct127Bytes*)&dst->fieldAtOffset16380 = *(Struct127Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy127BytesFromLocationAtOffset65520ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct127Bytes*)&dst->fieldAtOffset32760 = *(Struct127Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy127BytesFromLocationAtOffset65520ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct127Bytes*)&dst->fieldAtOffset65520 = *(Struct127Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy128BytesFromLocationAtOffset252ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct128Bytes*)&dst->fieldAtOffset252 = *(Struct128Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy128BytesFromLocationAtOffset252ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct128Bytes*)&dst->fieldAtOffset255 = *(Struct128Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy128BytesFromLocationAtOffset252ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct128Bytes*)&dst->fieldAtOffset504 = *(Struct128Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy128BytesFromLocationAtOffset252ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct128Bytes*)&dst->fieldAtOffset1008 = *(Struct128Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy128BytesFromLocationAtOffset252ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct128Bytes*)&dst->fieldAtOffset4095 = *(Struct128Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy128BytesFromLocationAtOffset252ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct128Bytes*)&dst->fieldAtOffset8190 = *(Struct128Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy128BytesFromLocationAtOffset252ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct128Bytes*)&dst->fieldAtOffset16380 = *(Struct128Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy128BytesFromLocationAtOffset252ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct128Bytes*)&dst->fieldAtOffset32760 = *(Struct128Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy128BytesFromLocationAtOffset252ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct128Bytes*)&dst->fieldAtOffset65520 = *(Struct128Bytes*)&src->fieldAtOffset252;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy128BytesFromLocationAtOffset255ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct128Bytes*)&dst->fieldAtOffset252 = *(Struct128Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy128BytesFromLocationAtOffset255ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct128Bytes*)&dst->fieldAtOffset255 = *(Struct128Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy128BytesFromLocationAtOffset255ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct128Bytes*)&dst->fieldAtOffset504 = *(Struct128Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy128BytesFromLocationAtOffset255ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct128Bytes*)&dst->fieldAtOffset1008 = *(Struct128Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy128BytesFromLocationAtOffset255ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct128Bytes*)&dst->fieldAtOffset4095 = *(Struct128Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy128BytesFromLocationAtOffset255ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct128Bytes*)&dst->fieldAtOffset8190 = *(Struct128Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy128BytesFromLocationAtOffset255ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct128Bytes*)&dst->fieldAtOffset16380 = *(Struct128Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy128BytesFromLocationAtOffset255ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct128Bytes*)&dst->fieldAtOffset32760 = *(Struct128Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy128BytesFromLocationAtOffset255ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct128Bytes*)&dst->fieldAtOffset65520 = *(Struct128Bytes*)&src->fieldAtOffset255;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy128BytesFromLocationAtOffset504ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct128Bytes*)&dst->fieldAtOffset252 = *(Struct128Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy128BytesFromLocationAtOffset504ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct128Bytes*)&dst->fieldAtOffset255 = *(Struct128Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy128BytesFromLocationAtOffset504ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct128Bytes*)&dst->fieldAtOffset504 = *(Struct128Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy128BytesFromLocationAtOffset504ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct128Bytes*)&dst->fieldAtOffset1008 = *(Struct128Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy128BytesFromLocationAtOffset504ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct128Bytes*)&dst->fieldAtOffset4095 = *(Struct128Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy128BytesFromLocationAtOffset504ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct128Bytes*)&dst->fieldAtOffset8190 = *(Struct128Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy128BytesFromLocationAtOffset504ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct128Bytes*)&dst->fieldAtOffset16380 = *(Struct128Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy128BytesFromLocationAtOffset504ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct128Bytes*)&dst->fieldAtOffset32760 = *(Struct128Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy128BytesFromLocationAtOffset504ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct128Bytes*)&dst->fieldAtOffset65520 = *(Struct128Bytes*)&src->fieldAtOffset504;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy128BytesFromLocationAtOffset1008ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct128Bytes*)&dst->fieldAtOffset252 = *(Struct128Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy128BytesFromLocationAtOffset1008ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct128Bytes*)&dst->fieldAtOffset255 = *(Struct128Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy128BytesFromLocationAtOffset1008ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct128Bytes*)&dst->fieldAtOffset504 = *(Struct128Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy128BytesFromLocationAtOffset1008ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct128Bytes*)&dst->fieldAtOffset1008 = *(Struct128Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy128BytesFromLocationAtOffset1008ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct128Bytes*)&dst->fieldAtOffset4095 = *(Struct128Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy128BytesFromLocationAtOffset1008ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct128Bytes*)&dst->fieldAtOffset8190 = *(Struct128Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy128BytesFromLocationAtOffset1008ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct128Bytes*)&dst->fieldAtOffset16380 = *(Struct128Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy128BytesFromLocationAtOffset1008ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct128Bytes*)&dst->fieldAtOffset32760 = *(Struct128Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy128BytesFromLocationAtOffset1008ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct128Bytes*)&dst->fieldAtOffset65520 = *(Struct128Bytes*)&src->fieldAtOffset1008;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy128BytesFromLocationAtOffset4095ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct128Bytes*)&dst->fieldAtOffset252 = *(Struct128Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy128BytesFromLocationAtOffset4095ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct128Bytes*)&dst->fieldAtOffset255 = *(Struct128Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy128BytesFromLocationAtOffset4095ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct128Bytes*)&dst->fieldAtOffset504 = *(Struct128Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy128BytesFromLocationAtOffset4095ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct128Bytes*)&dst->fieldAtOffset1008 = *(Struct128Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy128BytesFromLocationAtOffset4095ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct128Bytes*)&dst->fieldAtOffset4095 = *(Struct128Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy128BytesFromLocationAtOffset4095ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct128Bytes*)&dst->fieldAtOffset8190 = *(Struct128Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy128BytesFromLocationAtOffset4095ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct128Bytes*)&dst->fieldAtOffset16380 = *(Struct128Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy128BytesFromLocationAtOffset4095ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct128Bytes*)&dst->fieldAtOffset32760 = *(Struct128Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy128BytesFromLocationAtOffset4095ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct128Bytes*)&dst->fieldAtOffset65520 = *(Struct128Bytes*)&src->fieldAtOffset4095;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy128BytesFromLocationAtOffset8190ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct128Bytes*)&dst->fieldAtOffset252 = *(Struct128Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy128BytesFromLocationAtOffset8190ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct128Bytes*)&dst->fieldAtOffset255 = *(Struct128Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy128BytesFromLocationAtOffset8190ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct128Bytes*)&dst->fieldAtOffset504 = *(Struct128Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy128BytesFromLocationAtOffset8190ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct128Bytes*)&dst->fieldAtOffset1008 = *(Struct128Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy128BytesFromLocationAtOffset8190ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct128Bytes*)&dst->fieldAtOffset4095 = *(Struct128Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy128BytesFromLocationAtOffset8190ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct128Bytes*)&dst->fieldAtOffset8190 = *(Struct128Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy128BytesFromLocationAtOffset8190ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct128Bytes*)&dst->fieldAtOffset16380 = *(Struct128Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy128BytesFromLocationAtOffset8190ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct128Bytes*)&dst->fieldAtOffset32760 = *(Struct128Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy128BytesFromLocationAtOffset8190ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct128Bytes*)&dst->fieldAtOffset65520 = *(Struct128Bytes*)&src->fieldAtOffset8190;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy128BytesFromLocationAtOffset16380ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct128Bytes*)&dst->fieldAtOffset252 = *(Struct128Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy128BytesFromLocationAtOffset16380ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct128Bytes*)&dst->fieldAtOffset255 = *(Struct128Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy128BytesFromLocationAtOffset16380ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct128Bytes*)&dst->fieldAtOffset504 = *(Struct128Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy128BytesFromLocationAtOffset16380ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct128Bytes*)&dst->fieldAtOffset1008 = *(Struct128Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy128BytesFromLocationAtOffset16380ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct128Bytes*)&dst->fieldAtOffset4095 = *(Struct128Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy128BytesFromLocationAtOffset16380ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct128Bytes*)&dst->fieldAtOffset8190 = *(Struct128Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy128BytesFromLocationAtOffset16380ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct128Bytes*)&dst->fieldAtOffset16380 = *(Struct128Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy128BytesFromLocationAtOffset16380ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct128Bytes*)&dst->fieldAtOffset32760 = *(Struct128Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy128BytesFromLocationAtOffset16380ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct128Bytes*)&dst->fieldAtOffset65520 = *(Struct128Bytes*)&src->fieldAtOffset16380;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy128BytesFromLocationAtOffset32760ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct128Bytes*)&dst->fieldAtOffset252 = *(Struct128Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy128BytesFromLocationAtOffset32760ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct128Bytes*)&dst->fieldAtOffset255 = *(Struct128Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy128BytesFromLocationAtOffset32760ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct128Bytes*)&dst->fieldAtOffset504 = *(Struct128Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy128BytesFromLocationAtOffset32760ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct128Bytes*)&dst->fieldAtOffset1008 = *(Struct128Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy128BytesFromLocationAtOffset32760ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct128Bytes*)&dst->fieldAtOffset4095 = *(Struct128Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy128BytesFromLocationAtOffset32760ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct128Bytes*)&dst->fieldAtOffset8190 = *(Struct128Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy128BytesFromLocationAtOffset32760ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct128Bytes*)&dst->fieldAtOffset16380 = *(Struct128Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy128BytesFromLocationAtOffset32760ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct128Bytes*)&dst->fieldAtOffset32760 = *(Struct128Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy128BytesFromLocationAtOffset32760ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct128Bytes*)&dst->fieldAtOffset65520 = *(Struct128Bytes*)&src->fieldAtOffset32760;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy128BytesFromLocationAtOffset65520ToLocationAtOffset252(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct128Bytes*)&dst->fieldAtOffset252 = *(Struct128Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy128BytesFromLocationAtOffset65520ToLocationAtOffset255(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct128Bytes*)&dst->fieldAtOffset255 = *(Struct128Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy128BytesFromLocationAtOffset65520ToLocationAtOffset504(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct128Bytes*)&dst->fieldAtOffset504 = *(Struct128Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy128BytesFromLocationAtOffset65520ToLocationAtOffset1008(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct128Bytes*)&dst->fieldAtOffset1008 = *(Struct128Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy128BytesFromLocationAtOffset65520ToLocationAtOffset4095(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct128Bytes*)&dst->fieldAtOffset4095 = *(Struct128Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy128BytesFromLocationAtOffset65520ToLocationAtOffset8190(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct128Bytes*)&dst->fieldAtOffset8190 = *(Struct128Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy128BytesFromLocationAtOffset65520ToLocationAtOffset16380(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct128Bytes*)&dst->fieldAtOffset16380 = *(Struct128Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy128BytesFromLocationAtOffset65520ToLocationAtOffset32760(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct128Bytes*)&dst->fieldAtOffset32760 = *(Struct128Bytes*)&src->fieldAtOffset65520;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Copy128BytesFromLocationAtOffset65520ToLocationAtOffset65520(AnyOffset* dst, AnyOffset* src)
        {
            *(Struct128Bytes*)&dst->fieldAtOffset65520 = *(Struct128Bytes*)&src->fieldAtOffset65520;
        }

    }

    class Program
    {
        static unsafe int Main(string[] args)
        {
            var anyLocation = new AnyLocation();

            var src = new AnyOffset();
            var dst = new AnyOffset();

            anyLocation.Copy1BytesFromLocationAtOffset252ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy1BytesFromLocationAtOffset252ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy1BytesFromLocationAtOffset252ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy1BytesFromLocationAtOffset252ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy1BytesFromLocationAtOffset252ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy1BytesFromLocationAtOffset252ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy1BytesFromLocationAtOffset252ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy1BytesFromLocationAtOffset252ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy1BytesFromLocationAtOffset252ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy1BytesFromLocationAtOffset255ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy1BytesFromLocationAtOffset255ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy1BytesFromLocationAtOffset255ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy1BytesFromLocationAtOffset255ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy1BytesFromLocationAtOffset255ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy1BytesFromLocationAtOffset255ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy1BytesFromLocationAtOffset255ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy1BytesFromLocationAtOffset255ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy1BytesFromLocationAtOffset255ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy1BytesFromLocationAtOffset504ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy1BytesFromLocationAtOffset504ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy1BytesFromLocationAtOffset504ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy1BytesFromLocationAtOffset504ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy1BytesFromLocationAtOffset504ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy1BytesFromLocationAtOffset504ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy1BytesFromLocationAtOffset504ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy1BytesFromLocationAtOffset504ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy1BytesFromLocationAtOffset504ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy1BytesFromLocationAtOffset1008ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy1BytesFromLocationAtOffset1008ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy1BytesFromLocationAtOffset1008ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy1BytesFromLocationAtOffset1008ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy1BytesFromLocationAtOffset1008ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy1BytesFromLocationAtOffset1008ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy1BytesFromLocationAtOffset1008ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy1BytesFromLocationAtOffset1008ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy1BytesFromLocationAtOffset1008ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy1BytesFromLocationAtOffset4095ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy1BytesFromLocationAtOffset4095ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy1BytesFromLocationAtOffset4095ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy1BytesFromLocationAtOffset4095ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy1BytesFromLocationAtOffset4095ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy1BytesFromLocationAtOffset4095ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy1BytesFromLocationAtOffset4095ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy1BytesFromLocationAtOffset4095ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy1BytesFromLocationAtOffset4095ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy1BytesFromLocationAtOffset8190ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy1BytesFromLocationAtOffset8190ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy1BytesFromLocationAtOffset8190ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy1BytesFromLocationAtOffset8190ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy1BytesFromLocationAtOffset8190ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy1BytesFromLocationAtOffset8190ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy1BytesFromLocationAtOffset8190ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy1BytesFromLocationAtOffset8190ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy1BytesFromLocationAtOffset8190ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy1BytesFromLocationAtOffset16380ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy1BytesFromLocationAtOffset16380ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy1BytesFromLocationAtOffset16380ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy1BytesFromLocationAtOffset16380ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy1BytesFromLocationAtOffset16380ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy1BytesFromLocationAtOffset16380ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy1BytesFromLocationAtOffset16380ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy1BytesFromLocationAtOffset16380ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy1BytesFromLocationAtOffset16380ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy1BytesFromLocationAtOffset32760ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy1BytesFromLocationAtOffset32760ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy1BytesFromLocationAtOffset32760ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy1BytesFromLocationAtOffset32760ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy1BytesFromLocationAtOffset32760ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy1BytesFromLocationAtOffset32760ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy1BytesFromLocationAtOffset32760ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy1BytesFromLocationAtOffset32760ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy1BytesFromLocationAtOffset32760ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy1BytesFromLocationAtOffset65520ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy1BytesFromLocationAtOffset65520ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy1BytesFromLocationAtOffset65520ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy1BytesFromLocationAtOffset65520ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy1BytesFromLocationAtOffset65520ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy1BytesFromLocationAtOffset65520ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy1BytesFromLocationAtOffset65520ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy1BytesFromLocationAtOffset65520ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy1BytesFromLocationAtOffset65520ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy2BytesFromLocationAtOffset252ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy2BytesFromLocationAtOffset252ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy2BytesFromLocationAtOffset252ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy2BytesFromLocationAtOffset252ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy2BytesFromLocationAtOffset252ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy2BytesFromLocationAtOffset252ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy2BytesFromLocationAtOffset252ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy2BytesFromLocationAtOffset252ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy2BytesFromLocationAtOffset252ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy2BytesFromLocationAtOffset255ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy2BytesFromLocationAtOffset255ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy2BytesFromLocationAtOffset255ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy2BytesFromLocationAtOffset255ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy2BytesFromLocationAtOffset255ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy2BytesFromLocationAtOffset255ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy2BytesFromLocationAtOffset255ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy2BytesFromLocationAtOffset255ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy2BytesFromLocationAtOffset255ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy2BytesFromLocationAtOffset504ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy2BytesFromLocationAtOffset504ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy2BytesFromLocationAtOffset504ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy2BytesFromLocationAtOffset504ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy2BytesFromLocationAtOffset504ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy2BytesFromLocationAtOffset504ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy2BytesFromLocationAtOffset504ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy2BytesFromLocationAtOffset504ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy2BytesFromLocationAtOffset504ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy2BytesFromLocationAtOffset1008ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy2BytesFromLocationAtOffset1008ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy2BytesFromLocationAtOffset1008ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy2BytesFromLocationAtOffset1008ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy2BytesFromLocationAtOffset1008ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy2BytesFromLocationAtOffset1008ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy2BytesFromLocationAtOffset1008ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy2BytesFromLocationAtOffset1008ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy2BytesFromLocationAtOffset1008ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy2BytesFromLocationAtOffset4095ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy2BytesFromLocationAtOffset4095ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy2BytesFromLocationAtOffset4095ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy2BytesFromLocationAtOffset4095ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy2BytesFromLocationAtOffset4095ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy2BytesFromLocationAtOffset4095ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy2BytesFromLocationAtOffset4095ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy2BytesFromLocationAtOffset4095ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy2BytesFromLocationAtOffset4095ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy2BytesFromLocationAtOffset8190ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy2BytesFromLocationAtOffset8190ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy2BytesFromLocationAtOffset8190ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy2BytesFromLocationAtOffset8190ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy2BytesFromLocationAtOffset8190ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy2BytesFromLocationAtOffset8190ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy2BytesFromLocationAtOffset8190ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy2BytesFromLocationAtOffset8190ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy2BytesFromLocationAtOffset8190ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy2BytesFromLocationAtOffset16380ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy2BytesFromLocationAtOffset16380ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy2BytesFromLocationAtOffset16380ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy2BytesFromLocationAtOffset16380ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy2BytesFromLocationAtOffset16380ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy2BytesFromLocationAtOffset16380ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy2BytesFromLocationAtOffset16380ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy2BytesFromLocationAtOffset16380ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy2BytesFromLocationAtOffset16380ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy2BytesFromLocationAtOffset32760ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy2BytesFromLocationAtOffset32760ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy2BytesFromLocationAtOffset32760ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy2BytesFromLocationAtOffset32760ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy2BytesFromLocationAtOffset32760ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy2BytesFromLocationAtOffset32760ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy2BytesFromLocationAtOffset32760ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy2BytesFromLocationAtOffset32760ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy2BytesFromLocationAtOffset32760ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy2BytesFromLocationAtOffset65520ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy2BytesFromLocationAtOffset65520ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy2BytesFromLocationAtOffset65520ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy2BytesFromLocationAtOffset65520ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy2BytesFromLocationAtOffset65520ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy2BytesFromLocationAtOffset65520ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy2BytesFromLocationAtOffset65520ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy2BytesFromLocationAtOffset65520ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy2BytesFromLocationAtOffset65520ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy3BytesFromLocationAtOffset252ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy3BytesFromLocationAtOffset252ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy3BytesFromLocationAtOffset252ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy3BytesFromLocationAtOffset252ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy3BytesFromLocationAtOffset252ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy3BytesFromLocationAtOffset252ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy3BytesFromLocationAtOffset252ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy3BytesFromLocationAtOffset252ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy3BytesFromLocationAtOffset252ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy3BytesFromLocationAtOffset255ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy3BytesFromLocationAtOffset255ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy3BytesFromLocationAtOffset255ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy3BytesFromLocationAtOffset255ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy3BytesFromLocationAtOffset255ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy3BytesFromLocationAtOffset255ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy3BytesFromLocationAtOffset255ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy3BytesFromLocationAtOffset255ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy3BytesFromLocationAtOffset255ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy3BytesFromLocationAtOffset504ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy3BytesFromLocationAtOffset504ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy3BytesFromLocationAtOffset504ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy3BytesFromLocationAtOffset504ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy3BytesFromLocationAtOffset504ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy3BytesFromLocationAtOffset504ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy3BytesFromLocationAtOffset504ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy3BytesFromLocationAtOffset504ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy3BytesFromLocationAtOffset504ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy3BytesFromLocationAtOffset1008ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy3BytesFromLocationAtOffset1008ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy3BytesFromLocationAtOffset1008ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy3BytesFromLocationAtOffset1008ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy3BytesFromLocationAtOffset1008ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy3BytesFromLocationAtOffset1008ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy3BytesFromLocationAtOffset1008ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy3BytesFromLocationAtOffset1008ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy3BytesFromLocationAtOffset1008ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy3BytesFromLocationAtOffset4095ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy3BytesFromLocationAtOffset4095ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy3BytesFromLocationAtOffset4095ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy3BytesFromLocationAtOffset4095ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy3BytesFromLocationAtOffset4095ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy3BytesFromLocationAtOffset4095ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy3BytesFromLocationAtOffset4095ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy3BytesFromLocationAtOffset4095ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy3BytesFromLocationAtOffset4095ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy3BytesFromLocationAtOffset8190ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy3BytesFromLocationAtOffset8190ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy3BytesFromLocationAtOffset8190ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy3BytesFromLocationAtOffset8190ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy3BytesFromLocationAtOffset8190ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy3BytesFromLocationAtOffset8190ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy3BytesFromLocationAtOffset8190ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy3BytesFromLocationAtOffset8190ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy3BytesFromLocationAtOffset8190ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy3BytesFromLocationAtOffset16380ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy3BytesFromLocationAtOffset16380ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy3BytesFromLocationAtOffset16380ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy3BytesFromLocationAtOffset16380ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy3BytesFromLocationAtOffset16380ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy3BytesFromLocationAtOffset16380ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy3BytesFromLocationAtOffset16380ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy3BytesFromLocationAtOffset16380ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy3BytesFromLocationAtOffset16380ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy3BytesFromLocationAtOffset32760ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy3BytesFromLocationAtOffset32760ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy3BytesFromLocationAtOffset32760ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy3BytesFromLocationAtOffset32760ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy3BytesFromLocationAtOffset32760ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy3BytesFromLocationAtOffset32760ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy3BytesFromLocationAtOffset32760ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy3BytesFromLocationAtOffset32760ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy3BytesFromLocationAtOffset32760ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy3BytesFromLocationAtOffset65520ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy3BytesFromLocationAtOffset65520ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy3BytesFromLocationAtOffset65520ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy3BytesFromLocationAtOffset65520ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy3BytesFromLocationAtOffset65520ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy3BytesFromLocationAtOffset65520ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy3BytesFromLocationAtOffset65520ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy3BytesFromLocationAtOffset65520ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy3BytesFromLocationAtOffset65520ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy4BytesFromLocationAtOffset252ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy4BytesFromLocationAtOffset252ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy4BytesFromLocationAtOffset252ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy4BytesFromLocationAtOffset252ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy4BytesFromLocationAtOffset252ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy4BytesFromLocationAtOffset252ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy4BytesFromLocationAtOffset252ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy4BytesFromLocationAtOffset252ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy4BytesFromLocationAtOffset252ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy4BytesFromLocationAtOffset255ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy4BytesFromLocationAtOffset255ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy4BytesFromLocationAtOffset255ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy4BytesFromLocationAtOffset255ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy4BytesFromLocationAtOffset255ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy4BytesFromLocationAtOffset255ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy4BytesFromLocationAtOffset255ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy4BytesFromLocationAtOffset255ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy4BytesFromLocationAtOffset255ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy4BytesFromLocationAtOffset504ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy4BytesFromLocationAtOffset504ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy4BytesFromLocationAtOffset504ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy4BytesFromLocationAtOffset504ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy4BytesFromLocationAtOffset504ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy4BytesFromLocationAtOffset504ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy4BytesFromLocationAtOffset504ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy4BytesFromLocationAtOffset504ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy4BytesFromLocationAtOffset504ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy4BytesFromLocationAtOffset1008ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy4BytesFromLocationAtOffset1008ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy4BytesFromLocationAtOffset1008ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy4BytesFromLocationAtOffset1008ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy4BytesFromLocationAtOffset1008ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy4BytesFromLocationAtOffset1008ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy4BytesFromLocationAtOffset1008ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy4BytesFromLocationAtOffset1008ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy4BytesFromLocationAtOffset1008ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy4BytesFromLocationAtOffset4095ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy4BytesFromLocationAtOffset4095ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy4BytesFromLocationAtOffset4095ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy4BytesFromLocationAtOffset4095ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy4BytesFromLocationAtOffset4095ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy4BytesFromLocationAtOffset4095ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy4BytesFromLocationAtOffset4095ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy4BytesFromLocationAtOffset4095ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy4BytesFromLocationAtOffset4095ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy4BytesFromLocationAtOffset8190ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy4BytesFromLocationAtOffset8190ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy4BytesFromLocationAtOffset8190ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy4BytesFromLocationAtOffset8190ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy4BytesFromLocationAtOffset8190ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy4BytesFromLocationAtOffset8190ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy4BytesFromLocationAtOffset8190ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy4BytesFromLocationAtOffset8190ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy4BytesFromLocationAtOffset8190ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy4BytesFromLocationAtOffset16380ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy4BytesFromLocationAtOffset16380ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy4BytesFromLocationAtOffset16380ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy4BytesFromLocationAtOffset16380ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy4BytesFromLocationAtOffset16380ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy4BytesFromLocationAtOffset16380ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy4BytesFromLocationAtOffset16380ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy4BytesFromLocationAtOffset16380ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy4BytesFromLocationAtOffset16380ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy4BytesFromLocationAtOffset32760ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy4BytesFromLocationAtOffset32760ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy4BytesFromLocationAtOffset32760ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy4BytesFromLocationAtOffset32760ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy4BytesFromLocationAtOffset32760ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy4BytesFromLocationAtOffset32760ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy4BytesFromLocationAtOffset32760ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy4BytesFromLocationAtOffset32760ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy4BytesFromLocationAtOffset32760ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy4BytesFromLocationAtOffset65520ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy4BytesFromLocationAtOffset65520ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy4BytesFromLocationAtOffset65520ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy4BytesFromLocationAtOffset65520ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy4BytesFromLocationAtOffset65520ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy4BytesFromLocationAtOffset65520ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy4BytesFromLocationAtOffset65520ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy4BytesFromLocationAtOffset65520ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy4BytesFromLocationAtOffset65520ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy7BytesFromLocationAtOffset252ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy7BytesFromLocationAtOffset252ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy7BytesFromLocationAtOffset252ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy7BytesFromLocationAtOffset252ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy7BytesFromLocationAtOffset252ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy7BytesFromLocationAtOffset252ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy7BytesFromLocationAtOffset252ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy7BytesFromLocationAtOffset252ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy7BytesFromLocationAtOffset252ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy7BytesFromLocationAtOffset255ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy7BytesFromLocationAtOffset255ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy7BytesFromLocationAtOffset255ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy7BytesFromLocationAtOffset255ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy7BytesFromLocationAtOffset255ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy7BytesFromLocationAtOffset255ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy7BytesFromLocationAtOffset255ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy7BytesFromLocationAtOffset255ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy7BytesFromLocationAtOffset255ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy7BytesFromLocationAtOffset504ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy7BytesFromLocationAtOffset504ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy7BytesFromLocationAtOffset504ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy7BytesFromLocationAtOffset504ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy7BytesFromLocationAtOffset504ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy7BytesFromLocationAtOffset504ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy7BytesFromLocationAtOffset504ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy7BytesFromLocationAtOffset504ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy7BytesFromLocationAtOffset504ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy7BytesFromLocationAtOffset1008ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy7BytesFromLocationAtOffset1008ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy7BytesFromLocationAtOffset1008ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy7BytesFromLocationAtOffset1008ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy7BytesFromLocationAtOffset1008ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy7BytesFromLocationAtOffset1008ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy7BytesFromLocationAtOffset1008ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy7BytesFromLocationAtOffset1008ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy7BytesFromLocationAtOffset1008ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy7BytesFromLocationAtOffset4095ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy7BytesFromLocationAtOffset4095ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy7BytesFromLocationAtOffset4095ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy7BytesFromLocationAtOffset4095ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy7BytesFromLocationAtOffset4095ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy7BytesFromLocationAtOffset4095ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy7BytesFromLocationAtOffset4095ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy7BytesFromLocationAtOffset4095ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy7BytesFromLocationAtOffset4095ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy7BytesFromLocationAtOffset8190ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy7BytesFromLocationAtOffset8190ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy7BytesFromLocationAtOffset8190ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy7BytesFromLocationAtOffset8190ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy7BytesFromLocationAtOffset8190ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy7BytesFromLocationAtOffset8190ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy7BytesFromLocationAtOffset8190ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy7BytesFromLocationAtOffset8190ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy7BytesFromLocationAtOffset8190ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy7BytesFromLocationAtOffset16380ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy7BytesFromLocationAtOffset16380ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy7BytesFromLocationAtOffset16380ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy7BytesFromLocationAtOffset16380ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy7BytesFromLocationAtOffset16380ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy7BytesFromLocationAtOffset16380ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy7BytesFromLocationAtOffset16380ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy7BytesFromLocationAtOffset16380ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy7BytesFromLocationAtOffset16380ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy7BytesFromLocationAtOffset32760ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy7BytesFromLocationAtOffset32760ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy7BytesFromLocationAtOffset32760ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy7BytesFromLocationAtOffset32760ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy7BytesFromLocationAtOffset32760ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy7BytesFromLocationAtOffset32760ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy7BytesFromLocationAtOffset32760ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy7BytesFromLocationAtOffset32760ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy7BytesFromLocationAtOffset32760ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy7BytesFromLocationAtOffset65520ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy7BytesFromLocationAtOffset65520ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy7BytesFromLocationAtOffset65520ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy7BytesFromLocationAtOffset65520ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy7BytesFromLocationAtOffset65520ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy7BytesFromLocationAtOffset65520ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy7BytesFromLocationAtOffset65520ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy7BytesFromLocationAtOffset65520ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy7BytesFromLocationAtOffset65520ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy8BytesFromLocationAtOffset252ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy8BytesFromLocationAtOffset252ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy8BytesFromLocationAtOffset252ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy8BytesFromLocationAtOffset252ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy8BytesFromLocationAtOffset252ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy8BytesFromLocationAtOffset252ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy8BytesFromLocationAtOffset252ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy8BytesFromLocationAtOffset252ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy8BytesFromLocationAtOffset252ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy8BytesFromLocationAtOffset255ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy8BytesFromLocationAtOffset255ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy8BytesFromLocationAtOffset255ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy8BytesFromLocationAtOffset255ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy8BytesFromLocationAtOffset255ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy8BytesFromLocationAtOffset255ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy8BytesFromLocationAtOffset255ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy8BytesFromLocationAtOffset255ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy8BytesFromLocationAtOffset255ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy8BytesFromLocationAtOffset504ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy8BytesFromLocationAtOffset504ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy8BytesFromLocationAtOffset504ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy8BytesFromLocationAtOffset504ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy8BytesFromLocationAtOffset504ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy8BytesFromLocationAtOffset504ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy8BytesFromLocationAtOffset504ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy8BytesFromLocationAtOffset504ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy8BytesFromLocationAtOffset504ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy8BytesFromLocationAtOffset1008ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy8BytesFromLocationAtOffset1008ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy8BytesFromLocationAtOffset1008ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy8BytesFromLocationAtOffset1008ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy8BytesFromLocationAtOffset1008ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy8BytesFromLocationAtOffset1008ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy8BytesFromLocationAtOffset1008ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy8BytesFromLocationAtOffset1008ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy8BytesFromLocationAtOffset1008ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy8BytesFromLocationAtOffset4095ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy8BytesFromLocationAtOffset4095ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy8BytesFromLocationAtOffset4095ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy8BytesFromLocationAtOffset4095ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy8BytesFromLocationAtOffset4095ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy8BytesFromLocationAtOffset4095ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy8BytesFromLocationAtOffset4095ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy8BytesFromLocationAtOffset4095ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy8BytesFromLocationAtOffset4095ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy8BytesFromLocationAtOffset8190ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy8BytesFromLocationAtOffset8190ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy8BytesFromLocationAtOffset8190ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy8BytesFromLocationAtOffset8190ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy8BytesFromLocationAtOffset8190ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy8BytesFromLocationAtOffset8190ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy8BytesFromLocationAtOffset8190ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy8BytesFromLocationAtOffset8190ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy8BytesFromLocationAtOffset8190ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy8BytesFromLocationAtOffset16380ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy8BytesFromLocationAtOffset16380ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy8BytesFromLocationAtOffset16380ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy8BytesFromLocationAtOffset16380ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy8BytesFromLocationAtOffset16380ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy8BytesFromLocationAtOffset16380ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy8BytesFromLocationAtOffset16380ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy8BytesFromLocationAtOffset16380ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy8BytesFromLocationAtOffset16380ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy8BytesFromLocationAtOffset32760ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy8BytesFromLocationAtOffset32760ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy8BytesFromLocationAtOffset32760ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy8BytesFromLocationAtOffset32760ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy8BytesFromLocationAtOffset32760ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy8BytesFromLocationAtOffset32760ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy8BytesFromLocationAtOffset32760ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy8BytesFromLocationAtOffset32760ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy8BytesFromLocationAtOffset32760ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy8BytesFromLocationAtOffset65520ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy8BytesFromLocationAtOffset65520ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy8BytesFromLocationAtOffset65520ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy8BytesFromLocationAtOffset65520ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy8BytesFromLocationAtOffset65520ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy8BytesFromLocationAtOffset65520ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy8BytesFromLocationAtOffset65520ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy8BytesFromLocationAtOffset65520ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy8BytesFromLocationAtOffset65520ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy15BytesFromLocationAtOffset252ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy15BytesFromLocationAtOffset252ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy15BytesFromLocationAtOffset252ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy15BytesFromLocationAtOffset252ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy15BytesFromLocationAtOffset252ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy15BytesFromLocationAtOffset252ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy15BytesFromLocationAtOffset252ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy15BytesFromLocationAtOffset252ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy15BytesFromLocationAtOffset252ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy15BytesFromLocationAtOffset255ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy15BytesFromLocationAtOffset255ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy15BytesFromLocationAtOffset255ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy15BytesFromLocationAtOffset255ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy15BytesFromLocationAtOffset255ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy15BytesFromLocationAtOffset255ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy15BytesFromLocationAtOffset255ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy15BytesFromLocationAtOffset255ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy15BytesFromLocationAtOffset255ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy15BytesFromLocationAtOffset504ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy15BytesFromLocationAtOffset504ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy15BytesFromLocationAtOffset504ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy15BytesFromLocationAtOffset504ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy15BytesFromLocationAtOffset504ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy15BytesFromLocationAtOffset504ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy15BytesFromLocationAtOffset504ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy15BytesFromLocationAtOffset504ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy15BytesFromLocationAtOffset504ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy15BytesFromLocationAtOffset1008ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy15BytesFromLocationAtOffset1008ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy15BytesFromLocationAtOffset1008ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy15BytesFromLocationAtOffset1008ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy15BytesFromLocationAtOffset1008ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy15BytesFromLocationAtOffset1008ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy15BytesFromLocationAtOffset1008ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy15BytesFromLocationAtOffset1008ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy15BytesFromLocationAtOffset1008ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy15BytesFromLocationAtOffset4095ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy15BytesFromLocationAtOffset4095ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy15BytesFromLocationAtOffset4095ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy15BytesFromLocationAtOffset4095ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy15BytesFromLocationAtOffset4095ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy15BytesFromLocationAtOffset4095ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy15BytesFromLocationAtOffset4095ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy15BytesFromLocationAtOffset4095ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy15BytesFromLocationAtOffset4095ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy15BytesFromLocationAtOffset8190ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy15BytesFromLocationAtOffset8190ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy15BytesFromLocationAtOffset8190ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy15BytesFromLocationAtOffset8190ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy15BytesFromLocationAtOffset8190ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy15BytesFromLocationAtOffset8190ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy15BytesFromLocationAtOffset8190ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy15BytesFromLocationAtOffset8190ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy15BytesFromLocationAtOffset8190ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy15BytesFromLocationAtOffset16380ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy15BytesFromLocationAtOffset16380ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy15BytesFromLocationAtOffset16380ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy15BytesFromLocationAtOffset16380ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy15BytesFromLocationAtOffset16380ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy15BytesFromLocationAtOffset16380ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy15BytesFromLocationAtOffset16380ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy15BytesFromLocationAtOffset16380ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy15BytesFromLocationAtOffset16380ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy15BytesFromLocationAtOffset32760ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy15BytesFromLocationAtOffset32760ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy15BytesFromLocationAtOffset32760ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy15BytesFromLocationAtOffset32760ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy15BytesFromLocationAtOffset32760ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy15BytesFromLocationAtOffset32760ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy15BytesFromLocationAtOffset32760ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy15BytesFromLocationAtOffset32760ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy15BytesFromLocationAtOffset32760ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy15BytesFromLocationAtOffset65520ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy15BytesFromLocationAtOffset65520ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy15BytesFromLocationAtOffset65520ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy15BytesFromLocationAtOffset65520ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy15BytesFromLocationAtOffset65520ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy15BytesFromLocationAtOffset65520ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy15BytesFromLocationAtOffset65520ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy15BytesFromLocationAtOffset65520ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy15BytesFromLocationAtOffset65520ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy16BytesFromLocationAtOffset252ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy16BytesFromLocationAtOffset252ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy16BytesFromLocationAtOffset252ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy16BytesFromLocationAtOffset252ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy16BytesFromLocationAtOffset252ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy16BytesFromLocationAtOffset252ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy16BytesFromLocationAtOffset252ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy16BytesFromLocationAtOffset252ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy16BytesFromLocationAtOffset252ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy16BytesFromLocationAtOffset255ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy16BytesFromLocationAtOffset255ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy16BytesFromLocationAtOffset255ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy16BytesFromLocationAtOffset255ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy16BytesFromLocationAtOffset255ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy16BytesFromLocationAtOffset255ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy16BytesFromLocationAtOffset255ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy16BytesFromLocationAtOffset255ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy16BytesFromLocationAtOffset255ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy16BytesFromLocationAtOffset504ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy16BytesFromLocationAtOffset504ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy16BytesFromLocationAtOffset504ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy16BytesFromLocationAtOffset504ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy16BytesFromLocationAtOffset504ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy16BytesFromLocationAtOffset504ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy16BytesFromLocationAtOffset504ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy16BytesFromLocationAtOffset504ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy16BytesFromLocationAtOffset504ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy16BytesFromLocationAtOffset1008ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy16BytesFromLocationAtOffset1008ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy16BytesFromLocationAtOffset1008ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy16BytesFromLocationAtOffset1008ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy16BytesFromLocationAtOffset1008ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy16BytesFromLocationAtOffset1008ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy16BytesFromLocationAtOffset1008ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy16BytesFromLocationAtOffset1008ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy16BytesFromLocationAtOffset1008ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy16BytesFromLocationAtOffset4095ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy16BytesFromLocationAtOffset4095ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy16BytesFromLocationAtOffset4095ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy16BytesFromLocationAtOffset4095ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy16BytesFromLocationAtOffset4095ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy16BytesFromLocationAtOffset4095ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy16BytesFromLocationAtOffset4095ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy16BytesFromLocationAtOffset4095ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy16BytesFromLocationAtOffset4095ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy16BytesFromLocationAtOffset8190ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy16BytesFromLocationAtOffset8190ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy16BytesFromLocationAtOffset8190ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy16BytesFromLocationAtOffset8190ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy16BytesFromLocationAtOffset8190ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy16BytesFromLocationAtOffset8190ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy16BytesFromLocationAtOffset8190ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy16BytesFromLocationAtOffset8190ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy16BytesFromLocationAtOffset8190ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy16BytesFromLocationAtOffset16380ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy16BytesFromLocationAtOffset16380ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy16BytesFromLocationAtOffset16380ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy16BytesFromLocationAtOffset16380ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy16BytesFromLocationAtOffset16380ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy16BytesFromLocationAtOffset16380ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy16BytesFromLocationAtOffset16380ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy16BytesFromLocationAtOffset16380ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy16BytesFromLocationAtOffset16380ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy16BytesFromLocationAtOffset32760ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy16BytesFromLocationAtOffset32760ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy16BytesFromLocationAtOffset32760ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy16BytesFromLocationAtOffset32760ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy16BytesFromLocationAtOffset32760ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy16BytesFromLocationAtOffset32760ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy16BytesFromLocationAtOffset32760ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy16BytesFromLocationAtOffset32760ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy16BytesFromLocationAtOffset32760ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy16BytesFromLocationAtOffset65520ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy16BytesFromLocationAtOffset65520ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy16BytesFromLocationAtOffset65520ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy16BytesFromLocationAtOffset65520ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy16BytesFromLocationAtOffset65520ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy16BytesFromLocationAtOffset65520ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy16BytesFromLocationAtOffset65520ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy16BytesFromLocationAtOffset65520ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy16BytesFromLocationAtOffset65520ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy31BytesFromLocationAtOffset252ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy31BytesFromLocationAtOffset252ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy31BytesFromLocationAtOffset252ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy31BytesFromLocationAtOffset252ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy31BytesFromLocationAtOffset252ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy31BytesFromLocationAtOffset252ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy31BytesFromLocationAtOffset252ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy31BytesFromLocationAtOffset252ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy31BytesFromLocationAtOffset252ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy31BytesFromLocationAtOffset255ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy31BytesFromLocationAtOffset255ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy31BytesFromLocationAtOffset255ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy31BytesFromLocationAtOffset255ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy31BytesFromLocationAtOffset255ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy31BytesFromLocationAtOffset255ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy31BytesFromLocationAtOffset255ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy31BytesFromLocationAtOffset255ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy31BytesFromLocationAtOffset255ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy31BytesFromLocationAtOffset504ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy31BytesFromLocationAtOffset504ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy31BytesFromLocationAtOffset504ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy31BytesFromLocationAtOffset504ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy31BytesFromLocationAtOffset504ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy31BytesFromLocationAtOffset504ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy31BytesFromLocationAtOffset504ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy31BytesFromLocationAtOffset504ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy31BytesFromLocationAtOffset504ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy31BytesFromLocationAtOffset1008ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy31BytesFromLocationAtOffset1008ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy31BytesFromLocationAtOffset1008ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy31BytesFromLocationAtOffset1008ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy31BytesFromLocationAtOffset1008ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy31BytesFromLocationAtOffset1008ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy31BytesFromLocationAtOffset1008ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy31BytesFromLocationAtOffset1008ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy31BytesFromLocationAtOffset1008ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy31BytesFromLocationAtOffset4095ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy31BytesFromLocationAtOffset4095ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy31BytesFromLocationAtOffset4095ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy31BytesFromLocationAtOffset4095ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy31BytesFromLocationAtOffset4095ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy31BytesFromLocationAtOffset4095ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy31BytesFromLocationAtOffset4095ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy31BytesFromLocationAtOffset4095ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy31BytesFromLocationAtOffset4095ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy31BytesFromLocationAtOffset8190ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy31BytesFromLocationAtOffset8190ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy31BytesFromLocationAtOffset8190ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy31BytesFromLocationAtOffset8190ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy31BytesFromLocationAtOffset8190ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy31BytesFromLocationAtOffset8190ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy31BytesFromLocationAtOffset8190ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy31BytesFromLocationAtOffset8190ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy31BytesFromLocationAtOffset8190ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy31BytesFromLocationAtOffset16380ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy31BytesFromLocationAtOffset16380ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy31BytesFromLocationAtOffset16380ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy31BytesFromLocationAtOffset16380ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy31BytesFromLocationAtOffset16380ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy31BytesFromLocationAtOffset16380ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy31BytesFromLocationAtOffset16380ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy31BytesFromLocationAtOffset16380ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy31BytesFromLocationAtOffset16380ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy31BytesFromLocationAtOffset32760ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy31BytesFromLocationAtOffset32760ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy31BytesFromLocationAtOffset32760ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy31BytesFromLocationAtOffset32760ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy31BytesFromLocationAtOffset32760ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy31BytesFromLocationAtOffset32760ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy31BytesFromLocationAtOffset32760ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy31BytesFromLocationAtOffset32760ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy31BytesFromLocationAtOffset32760ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy31BytesFromLocationAtOffset65520ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy31BytesFromLocationAtOffset65520ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy31BytesFromLocationAtOffset65520ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy31BytesFromLocationAtOffset65520ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy31BytesFromLocationAtOffset65520ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy31BytesFromLocationAtOffset65520ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy31BytesFromLocationAtOffset65520ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy31BytesFromLocationAtOffset65520ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy31BytesFromLocationAtOffset65520ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy32BytesFromLocationAtOffset252ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy32BytesFromLocationAtOffset252ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy32BytesFromLocationAtOffset252ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy32BytesFromLocationAtOffset252ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy32BytesFromLocationAtOffset252ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy32BytesFromLocationAtOffset252ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy32BytesFromLocationAtOffset252ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy32BytesFromLocationAtOffset252ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy32BytesFromLocationAtOffset252ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy32BytesFromLocationAtOffset255ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy32BytesFromLocationAtOffset255ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy32BytesFromLocationAtOffset255ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy32BytesFromLocationAtOffset255ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy32BytesFromLocationAtOffset255ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy32BytesFromLocationAtOffset255ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy32BytesFromLocationAtOffset255ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy32BytesFromLocationAtOffset255ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy32BytesFromLocationAtOffset255ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy32BytesFromLocationAtOffset504ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy32BytesFromLocationAtOffset504ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy32BytesFromLocationAtOffset504ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy32BytesFromLocationAtOffset504ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy32BytesFromLocationAtOffset504ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy32BytesFromLocationAtOffset504ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy32BytesFromLocationAtOffset504ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy32BytesFromLocationAtOffset504ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy32BytesFromLocationAtOffset504ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy32BytesFromLocationAtOffset1008ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy32BytesFromLocationAtOffset1008ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy32BytesFromLocationAtOffset1008ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy32BytesFromLocationAtOffset1008ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy32BytesFromLocationAtOffset1008ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy32BytesFromLocationAtOffset1008ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy32BytesFromLocationAtOffset1008ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy32BytesFromLocationAtOffset1008ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy32BytesFromLocationAtOffset1008ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy32BytesFromLocationAtOffset4095ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy32BytesFromLocationAtOffset4095ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy32BytesFromLocationAtOffset4095ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy32BytesFromLocationAtOffset4095ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy32BytesFromLocationAtOffset4095ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy32BytesFromLocationAtOffset4095ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy32BytesFromLocationAtOffset4095ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy32BytesFromLocationAtOffset4095ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy32BytesFromLocationAtOffset4095ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy32BytesFromLocationAtOffset8190ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy32BytesFromLocationAtOffset8190ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy32BytesFromLocationAtOffset8190ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy32BytesFromLocationAtOffset8190ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy32BytesFromLocationAtOffset8190ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy32BytesFromLocationAtOffset8190ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy32BytesFromLocationAtOffset8190ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy32BytesFromLocationAtOffset8190ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy32BytesFromLocationAtOffset8190ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy32BytesFromLocationAtOffset16380ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy32BytesFromLocationAtOffset16380ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy32BytesFromLocationAtOffset16380ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy32BytesFromLocationAtOffset16380ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy32BytesFromLocationAtOffset16380ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy32BytesFromLocationAtOffset16380ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy32BytesFromLocationAtOffset16380ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy32BytesFromLocationAtOffset16380ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy32BytesFromLocationAtOffset16380ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy32BytesFromLocationAtOffset32760ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy32BytesFromLocationAtOffset32760ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy32BytesFromLocationAtOffset32760ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy32BytesFromLocationAtOffset32760ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy32BytesFromLocationAtOffset32760ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy32BytesFromLocationAtOffset32760ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy32BytesFromLocationAtOffset32760ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy32BytesFromLocationAtOffset32760ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy32BytesFromLocationAtOffset32760ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy32BytesFromLocationAtOffset65520ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy32BytesFromLocationAtOffset65520ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy32BytesFromLocationAtOffset65520ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy32BytesFromLocationAtOffset65520ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy32BytesFromLocationAtOffset65520ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy32BytesFromLocationAtOffset65520ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy32BytesFromLocationAtOffset65520ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy32BytesFromLocationAtOffset65520ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy32BytesFromLocationAtOffset65520ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy63BytesFromLocationAtOffset252ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy63BytesFromLocationAtOffset252ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy63BytesFromLocationAtOffset252ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy63BytesFromLocationAtOffset252ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy63BytesFromLocationAtOffset252ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy63BytesFromLocationAtOffset252ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy63BytesFromLocationAtOffset252ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy63BytesFromLocationAtOffset252ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy63BytesFromLocationAtOffset252ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy63BytesFromLocationAtOffset255ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy63BytesFromLocationAtOffset255ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy63BytesFromLocationAtOffset255ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy63BytesFromLocationAtOffset255ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy63BytesFromLocationAtOffset255ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy63BytesFromLocationAtOffset255ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy63BytesFromLocationAtOffset255ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy63BytesFromLocationAtOffset255ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy63BytesFromLocationAtOffset255ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy63BytesFromLocationAtOffset504ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy63BytesFromLocationAtOffset504ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy63BytesFromLocationAtOffset504ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy63BytesFromLocationAtOffset504ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy63BytesFromLocationAtOffset504ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy63BytesFromLocationAtOffset504ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy63BytesFromLocationAtOffset504ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy63BytesFromLocationAtOffset504ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy63BytesFromLocationAtOffset504ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy63BytesFromLocationAtOffset1008ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy63BytesFromLocationAtOffset1008ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy63BytesFromLocationAtOffset1008ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy63BytesFromLocationAtOffset1008ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy63BytesFromLocationAtOffset1008ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy63BytesFromLocationAtOffset1008ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy63BytesFromLocationAtOffset1008ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy63BytesFromLocationAtOffset1008ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy63BytesFromLocationAtOffset1008ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy63BytesFromLocationAtOffset4095ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy63BytesFromLocationAtOffset4095ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy63BytesFromLocationAtOffset4095ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy63BytesFromLocationAtOffset4095ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy63BytesFromLocationAtOffset4095ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy63BytesFromLocationAtOffset4095ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy63BytesFromLocationAtOffset4095ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy63BytesFromLocationAtOffset4095ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy63BytesFromLocationAtOffset4095ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy63BytesFromLocationAtOffset8190ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy63BytesFromLocationAtOffset8190ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy63BytesFromLocationAtOffset8190ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy63BytesFromLocationAtOffset8190ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy63BytesFromLocationAtOffset8190ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy63BytesFromLocationAtOffset8190ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy63BytesFromLocationAtOffset8190ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy63BytesFromLocationAtOffset8190ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy63BytesFromLocationAtOffset8190ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy63BytesFromLocationAtOffset16380ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy63BytesFromLocationAtOffset16380ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy63BytesFromLocationAtOffset16380ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy63BytesFromLocationAtOffset16380ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy63BytesFromLocationAtOffset16380ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy63BytesFromLocationAtOffset16380ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy63BytesFromLocationAtOffset16380ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy63BytesFromLocationAtOffset16380ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy63BytesFromLocationAtOffset16380ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy63BytesFromLocationAtOffset32760ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy63BytesFromLocationAtOffset32760ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy63BytesFromLocationAtOffset32760ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy63BytesFromLocationAtOffset32760ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy63BytesFromLocationAtOffset32760ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy63BytesFromLocationAtOffset32760ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy63BytesFromLocationAtOffset32760ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy63BytesFromLocationAtOffset32760ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy63BytesFromLocationAtOffset32760ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy63BytesFromLocationAtOffset65520ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy63BytesFromLocationAtOffset65520ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy63BytesFromLocationAtOffset65520ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy63BytesFromLocationAtOffset65520ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy63BytesFromLocationAtOffset65520ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy63BytesFromLocationAtOffset65520ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy63BytesFromLocationAtOffset65520ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy63BytesFromLocationAtOffset65520ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy63BytesFromLocationAtOffset65520ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy64BytesFromLocationAtOffset252ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy64BytesFromLocationAtOffset252ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy64BytesFromLocationAtOffset252ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy64BytesFromLocationAtOffset252ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy64BytesFromLocationAtOffset252ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy64BytesFromLocationAtOffset252ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy64BytesFromLocationAtOffset252ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy64BytesFromLocationAtOffset252ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy64BytesFromLocationAtOffset252ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy64BytesFromLocationAtOffset255ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy64BytesFromLocationAtOffset255ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy64BytesFromLocationAtOffset255ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy64BytesFromLocationAtOffset255ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy64BytesFromLocationAtOffset255ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy64BytesFromLocationAtOffset255ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy64BytesFromLocationAtOffset255ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy64BytesFromLocationAtOffset255ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy64BytesFromLocationAtOffset255ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy64BytesFromLocationAtOffset504ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy64BytesFromLocationAtOffset504ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy64BytesFromLocationAtOffset504ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy64BytesFromLocationAtOffset504ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy64BytesFromLocationAtOffset504ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy64BytesFromLocationAtOffset504ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy64BytesFromLocationAtOffset504ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy64BytesFromLocationAtOffset504ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy64BytesFromLocationAtOffset504ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy64BytesFromLocationAtOffset1008ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy64BytesFromLocationAtOffset1008ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy64BytesFromLocationAtOffset1008ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy64BytesFromLocationAtOffset1008ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy64BytesFromLocationAtOffset1008ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy64BytesFromLocationAtOffset1008ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy64BytesFromLocationAtOffset1008ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy64BytesFromLocationAtOffset1008ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy64BytesFromLocationAtOffset1008ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy64BytesFromLocationAtOffset4095ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy64BytesFromLocationAtOffset4095ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy64BytesFromLocationAtOffset4095ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy64BytesFromLocationAtOffset4095ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy64BytesFromLocationAtOffset4095ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy64BytesFromLocationAtOffset4095ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy64BytesFromLocationAtOffset4095ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy64BytesFromLocationAtOffset4095ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy64BytesFromLocationAtOffset4095ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy64BytesFromLocationAtOffset8190ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy64BytesFromLocationAtOffset8190ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy64BytesFromLocationAtOffset8190ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy64BytesFromLocationAtOffset8190ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy64BytesFromLocationAtOffset8190ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy64BytesFromLocationAtOffset8190ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy64BytesFromLocationAtOffset8190ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy64BytesFromLocationAtOffset8190ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy64BytesFromLocationAtOffset8190ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy64BytesFromLocationAtOffset16380ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy64BytesFromLocationAtOffset16380ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy64BytesFromLocationAtOffset16380ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy64BytesFromLocationAtOffset16380ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy64BytesFromLocationAtOffset16380ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy64BytesFromLocationAtOffset16380ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy64BytesFromLocationAtOffset16380ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy64BytesFromLocationAtOffset16380ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy64BytesFromLocationAtOffset16380ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy64BytesFromLocationAtOffset32760ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy64BytesFromLocationAtOffset32760ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy64BytesFromLocationAtOffset32760ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy64BytesFromLocationAtOffset32760ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy64BytesFromLocationAtOffset32760ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy64BytesFromLocationAtOffset32760ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy64BytesFromLocationAtOffset32760ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy64BytesFromLocationAtOffset32760ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy64BytesFromLocationAtOffset32760ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy64BytesFromLocationAtOffset65520ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy64BytesFromLocationAtOffset65520ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy64BytesFromLocationAtOffset65520ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy64BytesFromLocationAtOffset65520ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy64BytesFromLocationAtOffset65520ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy64BytesFromLocationAtOffset65520ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy64BytesFromLocationAtOffset65520ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy64BytesFromLocationAtOffset65520ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy64BytesFromLocationAtOffset65520ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy127BytesFromLocationAtOffset252ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy127BytesFromLocationAtOffset252ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy127BytesFromLocationAtOffset252ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy127BytesFromLocationAtOffset252ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy127BytesFromLocationAtOffset252ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy127BytesFromLocationAtOffset252ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy127BytesFromLocationAtOffset252ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy127BytesFromLocationAtOffset252ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy127BytesFromLocationAtOffset252ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy127BytesFromLocationAtOffset255ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy127BytesFromLocationAtOffset255ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy127BytesFromLocationAtOffset255ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy127BytesFromLocationAtOffset255ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy127BytesFromLocationAtOffset255ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy127BytesFromLocationAtOffset255ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy127BytesFromLocationAtOffset255ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy127BytesFromLocationAtOffset255ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy127BytesFromLocationAtOffset255ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy127BytesFromLocationAtOffset504ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy127BytesFromLocationAtOffset504ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy127BytesFromLocationAtOffset504ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy127BytesFromLocationAtOffset504ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy127BytesFromLocationAtOffset504ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy127BytesFromLocationAtOffset504ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy127BytesFromLocationAtOffset504ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy127BytesFromLocationAtOffset504ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy127BytesFromLocationAtOffset504ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy127BytesFromLocationAtOffset1008ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy127BytesFromLocationAtOffset1008ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy127BytesFromLocationAtOffset1008ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy127BytesFromLocationAtOffset1008ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy127BytesFromLocationAtOffset1008ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy127BytesFromLocationAtOffset1008ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy127BytesFromLocationAtOffset1008ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy127BytesFromLocationAtOffset1008ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy127BytesFromLocationAtOffset1008ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy127BytesFromLocationAtOffset4095ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy127BytesFromLocationAtOffset4095ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy127BytesFromLocationAtOffset4095ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy127BytesFromLocationAtOffset4095ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy127BytesFromLocationAtOffset4095ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy127BytesFromLocationAtOffset4095ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy127BytesFromLocationAtOffset4095ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy127BytesFromLocationAtOffset4095ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy127BytesFromLocationAtOffset4095ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy127BytesFromLocationAtOffset8190ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy127BytesFromLocationAtOffset8190ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy127BytesFromLocationAtOffset8190ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy127BytesFromLocationAtOffset8190ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy127BytesFromLocationAtOffset8190ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy127BytesFromLocationAtOffset8190ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy127BytesFromLocationAtOffset8190ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy127BytesFromLocationAtOffset8190ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy127BytesFromLocationAtOffset8190ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy127BytesFromLocationAtOffset16380ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy127BytesFromLocationAtOffset16380ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy127BytesFromLocationAtOffset16380ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy127BytesFromLocationAtOffset16380ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy127BytesFromLocationAtOffset16380ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy127BytesFromLocationAtOffset16380ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy127BytesFromLocationAtOffset16380ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy127BytesFromLocationAtOffset16380ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy127BytesFromLocationAtOffset16380ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy127BytesFromLocationAtOffset32760ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy127BytesFromLocationAtOffset32760ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy127BytesFromLocationAtOffset32760ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy127BytesFromLocationAtOffset32760ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy127BytesFromLocationAtOffset32760ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy127BytesFromLocationAtOffset32760ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy127BytesFromLocationAtOffset32760ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy127BytesFromLocationAtOffset32760ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy127BytesFromLocationAtOffset32760ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy127BytesFromLocationAtOffset65520ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy127BytesFromLocationAtOffset65520ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy127BytesFromLocationAtOffset65520ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy127BytesFromLocationAtOffset65520ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy127BytesFromLocationAtOffset65520ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy127BytesFromLocationAtOffset65520ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy127BytesFromLocationAtOffset65520ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy127BytesFromLocationAtOffset65520ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy127BytesFromLocationAtOffset65520ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy128BytesFromLocationAtOffset252ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy128BytesFromLocationAtOffset252ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy128BytesFromLocationAtOffset252ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy128BytesFromLocationAtOffset252ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy128BytesFromLocationAtOffset252ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy128BytesFromLocationAtOffset252ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy128BytesFromLocationAtOffset252ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy128BytesFromLocationAtOffset252ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy128BytesFromLocationAtOffset252ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy128BytesFromLocationAtOffset255ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy128BytesFromLocationAtOffset255ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy128BytesFromLocationAtOffset255ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy128BytesFromLocationAtOffset255ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy128BytesFromLocationAtOffset255ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy128BytesFromLocationAtOffset255ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy128BytesFromLocationAtOffset255ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy128BytesFromLocationAtOffset255ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy128BytesFromLocationAtOffset255ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy128BytesFromLocationAtOffset504ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy128BytesFromLocationAtOffset504ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy128BytesFromLocationAtOffset504ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy128BytesFromLocationAtOffset504ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy128BytesFromLocationAtOffset504ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy128BytesFromLocationAtOffset504ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy128BytesFromLocationAtOffset504ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy128BytesFromLocationAtOffset504ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy128BytesFromLocationAtOffset504ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy128BytesFromLocationAtOffset1008ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy128BytesFromLocationAtOffset1008ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy128BytesFromLocationAtOffset1008ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy128BytesFromLocationAtOffset1008ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy128BytesFromLocationAtOffset1008ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy128BytesFromLocationAtOffset1008ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy128BytesFromLocationAtOffset1008ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy128BytesFromLocationAtOffset1008ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy128BytesFromLocationAtOffset1008ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy128BytesFromLocationAtOffset4095ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy128BytesFromLocationAtOffset4095ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy128BytesFromLocationAtOffset4095ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy128BytesFromLocationAtOffset4095ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy128BytesFromLocationAtOffset4095ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy128BytesFromLocationAtOffset4095ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy128BytesFromLocationAtOffset4095ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy128BytesFromLocationAtOffset4095ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy128BytesFromLocationAtOffset4095ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy128BytesFromLocationAtOffset8190ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy128BytesFromLocationAtOffset8190ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy128BytesFromLocationAtOffset8190ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy128BytesFromLocationAtOffset8190ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy128BytesFromLocationAtOffset8190ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy128BytesFromLocationAtOffset8190ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy128BytesFromLocationAtOffset8190ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy128BytesFromLocationAtOffset8190ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy128BytesFromLocationAtOffset8190ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy128BytesFromLocationAtOffset16380ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy128BytesFromLocationAtOffset16380ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy128BytesFromLocationAtOffset16380ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy128BytesFromLocationAtOffset16380ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy128BytesFromLocationAtOffset16380ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy128BytesFromLocationAtOffset16380ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy128BytesFromLocationAtOffset16380ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy128BytesFromLocationAtOffset16380ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy128BytesFromLocationAtOffset16380ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy128BytesFromLocationAtOffset32760ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy128BytesFromLocationAtOffset32760ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy128BytesFromLocationAtOffset32760ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy128BytesFromLocationAtOffset32760ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy128BytesFromLocationAtOffset32760ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy128BytesFromLocationAtOffset32760ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy128BytesFromLocationAtOffset32760ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy128BytesFromLocationAtOffset32760ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy128BytesFromLocationAtOffset32760ToLocationAtOffset65520(&dst, &src);
            anyLocation.Copy128BytesFromLocationAtOffset65520ToLocationAtOffset252(&dst, &src);
            anyLocation.Copy128BytesFromLocationAtOffset65520ToLocationAtOffset255(&dst, &src);
            anyLocation.Copy128BytesFromLocationAtOffset65520ToLocationAtOffset504(&dst, &src);
            anyLocation.Copy128BytesFromLocationAtOffset65520ToLocationAtOffset1008(&dst, &src);
            anyLocation.Copy128BytesFromLocationAtOffset65520ToLocationAtOffset4095(&dst, &src);
            anyLocation.Copy128BytesFromLocationAtOffset65520ToLocationAtOffset8190(&dst, &src);
            anyLocation.Copy128BytesFromLocationAtOffset65520ToLocationAtOffset16380(&dst, &src);
            anyLocation.Copy128BytesFromLocationAtOffset65520ToLocationAtOffset32760(&dst, &src);
            anyLocation.Copy128BytesFromLocationAtOffset65520ToLocationAtOffset65520(&dst, &src);

            return 100;
        }
    }
}
