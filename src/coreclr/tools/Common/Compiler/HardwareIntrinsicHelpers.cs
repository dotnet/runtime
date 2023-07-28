// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

using Internal.JitInterface;
using Internal.TypeSystem;

namespace ILCompiler
{
    public static partial class HardwareIntrinsicHelpers
    {
        /// <summary>
        /// Gets a value indicating whether this is a hardware intrinsic on the platform that we're compiling for.
        /// </summary>
        public static bool IsHardwareIntrinsic(MethodDesc method)
        {
            return !string.IsNullOrEmpty(InstructionSetSupport.GetHardwareIntrinsicId(method.Context.Target.Architecture, method.OwningType));
        }

        public static void AddRuntimeRequiredIsaFlagsToBuilder(InstructionSetSupportBuilder builder, int flags)
        {
            switch (builder.Architecture)
            {
                case TargetArchitecture.X86:
                case TargetArchitecture.X64:
                    XArchIntrinsicConstants.AddToBuilder(builder, flags);
                    break;
                case TargetArchitecture.ARM64:
                    Arm64IntrinsicConstants.AddToBuilder(builder, flags);
                    break;
                default:
                    Debug.Fail("Probably unimplemented");
                    break;
            }
        }

        // Keep these enumerations in sync with cpufeatures.h in the minipal.
        private static class XArchIntrinsicConstants
        {
            // SSE and SSE2 are baseline ISAs - they're always available
            public const int Aes = 0x0001;
            public const int Pclmulqdq = 0x0002;
            public const int Sse3 = 0x0004;
            public const int Ssse3 = 0x0008;
            public const int Sse41 = 0x0010;
            public const int Sse42 = 0x0020;
            public const int Popcnt = 0x0040;
            public const int Avx = 0x0080;
            public const int Fma = 0x0100;
            public const int Avx2 = 0x0200;
            public const int Bmi1 = 0x0400;
            public const int Bmi2 = 0x0800;
            public const int Lzcnt = 0x1000;
            public const int AvxVnni = 0x2000;
            public const int Movbe = 0x4000;
            public const int Avx512f = 0x8000;
            public const int Avx512f_vl = 0x10000;
            public const int Avx512bw = 0x20000;
            public const int Avx512bw_vl = 0x40000;
            public const int Avx512cd = 0x80000;
            public const int Avx512cd_vl = 0x100000;
            public const int Avx512dq = 0x200000;
            public const int Avx512dq_vl = 0x400000;
            public const int Avx512Vbmi = 0x800000;
            public const int Avx512Vbmi_vl = 0x1000000;
            public const int Serialize = 0x2000000;
            public const int VectorT128 = 0x4000000;
            public const int VectorT256 = 0x8000000;
            public const int VectorT512 = 0x10000000;

            public static void AddToBuilder(InstructionSetSupportBuilder builder, int flags)
            {
                if ((flags & Aes) != 0)
                    builder.AddSupportedInstructionSet("aes");
                if ((flags & Pclmulqdq) != 0)
                    builder.AddSupportedInstructionSet("pclmul");
                if ((flags & Sse3) != 0)
                    builder.AddSupportedInstructionSet("sse3");
                if ((flags & Ssse3) != 0)
                    builder.AddSupportedInstructionSet("ssse3");
                if ((flags & Sse41) != 0)
                    builder.AddSupportedInstructionSet("sse4.1");
                if ((flags & Sse42) != 0)
                    builder.AddSupportedInstructionSet("sse4.2");
                if ((flags & Popcnt) != 0)
                    builder.AddSupportedInstructionSet("popcnt");
                if ((flags & Avx) != 0)
                    builder.AddSupportedInstructionSet("avx");
                if ((flags & Fma) != 0)
                    builder.AddSupportedInstructionSet("fma");
                if ((flags & Avx2) != 0)
                    builder.AddSupportedInstructionSet("avx2");
                if ((flags & Bmi1) != 0)
                    builder.AddSupportedInstructionSet("bmi");
                if ((flags & Bmi2) != 0)
                    builder.AddSupportedInstructionSet("bmi2");
                if ((flags & Lzcnt) != 0)
                    builder.AddSupportedInstructionSet("lzcnt");
                if ((flags & AvxVnni) != 0)
                    builder.AddSupportedInstructionSet("avxvnni");
                if ((flags & Movbe) != 0)
                    builder.AddSupportedInstructionSet("movbe");
                if ((flags & Avx512f) != 0)
                    builder.AddSupportedInstructionSet("avx512f");
                if ((flags & Avx512f_vl) != 0)
                    builder.AddSupportedInstructionSet("avx512f_vl");
                if ((flags & Avx512bw) != 0)
                    builder.AddSupportedInstructionSet("avx512bw");
                if ((flags & Avx512bw_vl) != 0)
                    builder.AddSupportedInstructionSet("avx512bw_vl");
                if ((flags & Avx512cd) != 0)
                    builder.AddSupportedInstructionSet("avx512cd");
                if ((flags & Avx512cd_vl) != 0)
                    builder.AddSupportedInstructionSet("avx512cd_vl");
                if ((flags & Avx512dq) != 0)
                    builder.AddSupportedInstructionSet("avx512dq");
                if ((flags & Avx512dq_vl) != 0)
                    builder.AddSupportedInstructionSet("avx512dq_vl");
                if ((flags & Avx512Vbmi) != 0)
                    builder.AddSupportedInstructionSet("avx512vbmi");
                if ((flags & Avx512Vbmi_vl) != 0)
                    builder.AddSupportedInstructionSet("avx512vbmi_vl");
                if ((flags & Serialize) != 0)
                    builder.AddSupportedInstructionSet("serialize");
            }

            public static int FromInstructionSet(InstructionSet instructionSet)
            {
                Debug.Assert(InstructionSet.X64_AES == InstructionSet.X86_AES);
                Debug.Assert(InstructionSet.X64_SSE41 == InstructionSet.X86_SSE41);
                Debug.Assert(InstructionSet.X64_LZCNT == InstructionSet.X86_LZCNT);

                return instructionSet switch
                {
                    // Optional ISAs - only available via opt-in or opportunistic light-up
                    InstructionSet.X64_AES => Aes,
                    InstructionSet.X64_AES_X64 => Aes,
                    InstructionSet.X64_PCLMULQDQ => Pclmulqdq,
                    InstructionSet.X64_PCLMULQDQ_X64 => Pclmulqdq,
                    InstructionSet.X64_SSE3 => Sse3,
                    InstructionSet.X64_SSE3_X64 => Sse3,
                    InstructionSet.X64_SSSE3 => Ssse3,
                    InstructionSet.X64_SSSE3_X64 => Ssse3,
                    InstructionSet.X64_SSE41 => Sse41,
                    InstructionSet.X64_SSE41_X64 => Sse41,
                    InstructionSet.X64_SSE42 => Sse42,
                    InstructionSet.X64_SSE42_X64 => Sse42,
                    InstructionSet.X64_POPCNT => Popcnt,
                    InstructionSet.X64_POPCNT_X64 => Popcnt,
                    InstructionSet.X64_AVX => Avx,
                    InstructionSet.X64_AVX_X64 => Avx,
                    InstructionSet.X64_FMA => Fma,
                    InstructionSet.X64_FMA_X64 => Fma,
                    InstructionSet.X64_AVX2 => Avx2,
                    InstructionSet.X64_AVX2_X64 => Avx2,
                    InstructionSet.X64_BMI1 => Bmi1,
                    InstructionSet.X64_BMI1_X64 => Bmi1,
                    InstructionSet.X64_BMI2 => Bmi2,
                    InstructionSet.X64_BMI2_X64 => Bmi2,
                    InstructionSet.X64_LZCNT => Lzcnt,
                    InstructionSet.X64_LZCNT_X64 => Lzcnt,
                    InstructionSet.X64_AVXVNNI => AvxVnni,
                    InstructionSet.X64_AVXVNNI_X64 => AvxVnni,
                    InstructionSet.X64_MOVBE => Movbe,
                    InstructionSet.X64_MOVBE_X64 => Movbe,
                    InstructionSet.X64_AVX512F => Avx512f,
                    InstructionSet.X64_AVX512F_X64 => Avx512f,
                    InstructionSet.X64_AVX512F_VL => Avx512f_vl,
                    InstructionSet.X64_AVX512F_VL_X64 => Avx512f_vl,
                    InstructionSet.X64_AVX512BW => Avx512bw,
                    InstructionSet.X64_AVX512BW_X64 => Avx512bw,
                    InstructionSet.X64_AVX512BW_VL => Avx512bw_vl,
                    InstructionSet.X64_AVX512BW_VL_X64 => Avx512bw_vl,
                    InstructionSet.X64_AVX512CD => Avx512cd,
                    InstructionSet.X64_AVX512CD_X64 => Avx512cd,
                    InstructionSet.X64_AVX512CD_VL => Avx512cd_vl,
                    InstructionSet.X64_AVX512CD_VL_X64 => Avx512cd_vl,
                    InstructionSet.X64_AVX512DQ => Avx512dq,
                    InstructionSet.X64_AVX512DQ_X64 => Avx512dq,
                    InstructionSet.X64_AVX512DQ_VL => Avx512dq_vl,
                    InstructionSet.X64_AVX512DQ_VL_X64 => Avx512dq_vl,
                    InstructionSet.X64_AVX512VBMI => Avx512Vbmi,
                    InstructionSet.X64_AVX512VBMI_X64 => Avx512Vbmi,
                    InstructionSet.X64_AVX512VBMI_VL => Avx512Vbmi_vl,
                    InstructionSet.X64_AVX512VBMI_VL_X64 => Avx512Vbmi_vl,
                    InstructionSet.X64_X86Serialize => Serialize,
                    InstructionSet.X64_X86Serialize_X64 => Serialize,

                    // Baseline ISAs - they're always available
                    InstructionSet.X64_SSE => 0,
                    InstructionSet.X64_SSE_X64 => 0,
                    InstructionSet.X64_SSE2 => 0,
                    InstructionSet.X64_SSE2_X64 => 0,

                    InstructionSet.X64_X86Base => 0,
                    InstructionSet.X64_X86Base_X64 => 0,

                    // Vector<T> Sizes
                    InstructionSet.X64_VectorT128 => VectorT128,
                    InstructionSet.X64_VectorT256 => VectorT256,
                    InstructionSet.X64_VectorT512 => VectorT512,

                    _ => throw new NotSupportedException(((InstructionSet_X64)instructionSet).ToString())
                };
            }
        }

        // Keep these enumerations in sync with cpufeatures.h in the minipal.
        private static class Arm64IntrinsicConstants
        {
            public const int AdvSimd = 0x0001;
            public const int Aes = 0x0002;
            public const int Crc32 = 0x0004;
            public const int Dp = 0x0008;
            public const int Rdm = 0x0010;
            public const int Sha1 = 0x0020;
            public const int Sha256 = 0x0040;
            public const int Atomics = 0x0080;
            public const int Rcpc = 0x0100;
            public const int VectorT128 = 0x0200;

            public static void AddToBuilder(InstructionSetSupportBuilder builder, int flags)
            {
                if ((flags & AdvSimd) != 0)
                    builder.AddSupportedInstructionSet("neon");
                if ((flags & Aes) != 0)
                    builder.AddSupportedInstructionSet("aes");
                if ((flags & Crc32) != 0)
                    builder.AddSupportedInstructionSet("crc");
                if ((flags & Dp) != 0)
                    builder.AddSupportedInstructionSet("dotprod");
                if ((flags & Rdm) != 0)
                    builder.AddSupportedInstructionSet("rdma");
                if ((flags & Sha1) != 0)
                    builder.AddSupportedInstructionSet("sha1");
                if ((flags & Sha256) != 0)
                    builder.AddSupportedInstructionSet("sha2");
                if ((flags & Atomics) != 0)
                    builder.AddSupportedInstructionSet("lse");
                if ((flags & Rcpc) != 0)
                    builder.AddSupportedInstructionSet("rcpc");
            }

            public static int FromInstructionSet(InstructionSet instructionSet)
            {
                return instructionSet switch
                {

                    // Baseline ISAs - they're always available
                    InstructionSet.ARM64_ArmBase => 0,
                    InstructionSet.ARM64_ArmBase_Arm64 => 0,
                    InstructionSet.ARM64_AdvSimd => AdvSimd,
                    InstructionSet.ARM64_AdvSimd_Arm64 => AdvSimd,

                    // Optional ISAs - only available via opt-in or opportunistic light-up
                    InstructionSet.ARM64_Aes => Aes,
                    InstructionSet.ARM64_Aes_Arm64 => Aes,
                    InstructionSet.ARM64_Crc32 => Crc32,
                    InstructionSet.ARM64_Crc32_Arm64 => Crc32,
                    InstructionSet.ARM64_Dp => Dp,
                    InstructionSet.ARM64_Dp_Arm64 => Dp,
                    InstructionSet.ARM64_Rdm => Rdm,
                    InstructionSet.ARM64_Rdm_Arm64 => Rdm,
                    InstructionSet.ARM64_Sha1 => Sha1,
                    InstructionSet.ARM64_Sha1_Arm64 => Sha1,
                    InstructionSet.ARM64_Sha256 => Sha256,
                    InstructionSet.ARM64_Sha256_Arm64 => Sha256,
                    InstructionSet.ARM64_Atomics => Atomics,
                    InstructionSet.ARM64_Rcpc => Rcpc,

                    // Vector<T> Sizes
                    InstructionSet.ARM64_VectorT128 => VectorT128,

                    _ => throw new NotSupportedException(((InstructionSet_ARM64)instructionSet).ToString())
                };
            }
        }
    }
}
