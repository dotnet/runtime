// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Reflection.PortableExecutable
{
    public enum Machine : ushort
    {
        /// <summary>
        /// The target CPU is unknown or not specified.
        /// </summary>
        Unknown = 0x0000,

        /// <summary>
        /// Intel 386.
        /// </summary>
        I386 = 0x014C,

        /// <summary>
        /// MIPS little-endian WCE v2
        /// </summary>
        WceMipsV2 = 0x0169,

        /// <summary>
        /// Alpha
        /// </summary>
        Alpha = 0x0184,

        /// <summary>
        /// Hitachi SH3 little endian
        /// </summary>
        SH3 = 0x01a2,

        /// <summary>
        /// Hitachi SH3 DSP.
        /// </summary>
        SH3Dsp = 0x01a3,

        /// <summary>
        /// Hitachi SH3 little endian.
        /// </summary>
        SH3E = 0x01a4,

        /// <summary>
        /// Hitachi SH4 little endian.
        /// </summary>
        SH4 = 0x01a6,

        /// <summary>
        /// Hitachi SH5.
        /// </summary>
        SH5 = 0x01a8,

        /// <summary>
        /// ARM little endian
        /// </summary>
        Arm = 0x01c0,

        /// <summary>
        /// Thumb.
        /// </summary>
        Thumb = 0x01c2,

        /// <summary>
        /// ARM Thumb-2 little endian.
        /// </summary>
        ArmThumb2 = 0x01c4,

        /// <summary>
        /// Matsushita AM33.
        /// </summary>
        AM33 = 0x01d3,

        /// <summary>
        /// IBM PowerPC little endian.
        /// </summary>
        PowerPC = 0x01F0,

        /// <summary>
        /// PowerPCFP
        /// </summary>
        PowerPCFP = 0x01f1,

        /// <summary>
        /// Intel 64
        /// </summary>
        IA64 = 0x0200,

        /// <summary>
        /// MIPS
        /// </summary>
        MIPS16 = 0x0266,

        /// <summary>
        /// ALPHA64
        /// </summary>
        Alpha64 = 0x0284,

        /// <summary>
        /// MIPS with FPU.
        /// </summary>
        MipsFpu = 0x0366,

        /// <summary>
        /// MIPS16 with FPU.
        /// </summary>
        MipsFpu16 = 0x0466,

        /// <summary>
        /// Infineon
        /// </summary>
        Tricore = 0x0520,

        /// <summary>
        /// EFI Byte Code
        /// </summary>
        Ebc = 0x0EBC,

        /// <summary>
        /// AMD64 (K8)
        /// </summary>
        Amd64 = 0x8664,

        /// <summary>
        /// M32R little-endian
        /// </summary>
        M32R = 0x9041,

        /// <summary>
        /// ARM64
        /// </summary>
        Arm64 = 0xAA64,

        /// <summary>
        /// LOONGARCH32
        /// </summary>
        LoongArch32 = 0x6232,

        /// <summary>
        /// LOONGARCH64
        /// </summary>
        LoongArch64 = 0x6264,
    }
}
