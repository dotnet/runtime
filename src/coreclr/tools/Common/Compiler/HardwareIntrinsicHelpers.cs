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
            // Matches logic in
            // https://github.com/dotnet/runtime/blob/5c40bb5636b939fb548492fdeb9d501b599ac5f5/src/coreclr/vm/methodtablebuilder.cpp#L1491-L1512
            TypeDesc owningType = method.OwningType;
            if (owningType.IsIntrinsic && !owningType.HasInstantiation)
            {
                var owningMdType = (MetadataType)owningType;
                DefType containingType = owningMdType.ContainingType;
                string ns = containingType?.ContainingType?.Namespace ??
                            containingType?.Namespace ??
                            owningMdType.Namespace;
                return method.Context.Target.Architecture switch
                {
                    TargetArchitecture.ARM64 => ns == "System.Runtime.Intrinsics.Arm",
                    TargetArchitecture.X64 or TargetArchitecture.X86 => ns == "System.Runtime.Intrinsics.X86",
                    _ => false,
                };
            }

            return false;
        }

        public static void AddRuntimeRequiredIsaFlagsToBuilder(InstructionSetSupportBuilder builder, long flags)
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
                case TargetArchitecture.RiscV64:
                    RiscV64IntrinsicConstants.AddToBuilder(builder, flags);
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
            public const long Aes = (1L << 0);
            public const long Pclmulqdq = (1L << 1);
            public const long Sse3 = (1L << 2);
            public const long Ssse3 = (1L << 3);
            public const long Sse41 = (1L << 4);
            public const long Sse42 = (1L << 5);
            public const long Popcnt = (1L << 6);
            public const long Avx = (1L << 7);
            public const long Fma = (1L << 8);
            public const long Avx2 = (1L << 9);
            public const long Bmi1 = (1L << 10);
            public const long Bmi2 = (1L << 11);
            public const long Lzcnt = (1L << 12);
            public const long AvxVnni = (1L << 13);
            public const long Movbe = (1L << 14);
            public const long Avx512 = (1L << 15);
            public const long Avx512Vbmi = (1L << 16);
            public const long Serialize = (1L << 17);
            public const long Avx10v1 = (1L << 18);
            public const long Apx = (1L << 19);
            public const long Vpclmulqdq = (1L << 20);
            public const long Avx10v2 = (1L << 21);
            public const long Gfni = (1L << 22);
            public const long Avx512Bitalg = (1L << 23);
            public const long Avx512Bf16 = (1L << 24);
            public const long Avx512Fp16 = (1L << 25);
            public const long Avx512Ifma = (1L << 26);
            public const long Avx512Vbmi2 = (1L << 27);
            public const long Avx512Vnni = (1L << 28);
            public const long Avx512Vp2intersect = (1L << 29);
            public const long Avx512Vpopcntdq = (1L << 30);
            public const long AvxIfma = (1L << 31);
            public const long F16c = (1L << 32);
            public const long Sha = (1L << 33);
            public const long Vaes = (1L << 34);
            public const long WaitPkg = (1L << 35);

            public static void AddToBuilder(InstructionSetSupportBuilder builder, long flags)
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
                if ((flags & Avx512) != 0)
                    builder.AddSupportedInstructionSet("avx512");
                if ((flags & Avx512Vbmi) != 0)
                    builder.AddSupportedInstructionSet("avx512vbmi");
                if ((flags & Serialize) != 0)
                    builder.AddSupportedInstructionSet("serialize");
                if ((flags & Avx10v1) != 0)
                    builder.AddSupportedInstructionSet("avx10v1");
                if ((flags & Apx) != 0)
                    builder.AddSupportedInstructionSet("apx");
                if ((flags & Vpclmulqdq) != 0)
                {
                    builder.AddSupportedInstructionSet("vpclmul");
                    if ((flags & Avx512) != 0)
                        builder.AddSupportedInstructionSet("vpclmul_v512");
                }
                if ((flags & Avx10v2) != 0)
                    builder.AddSupportedInstructionSet("avx10v2");
                if ((flags & Gfni) != 0)
                {
                    builder.AddSupportedInstructionSet("gfni");
                    if ((flags & Avx) != 0)
                        builder.AddSupportedInstructionSet("gfni_v256");
                    if ((flags & Avx512) != 0)
                        builder.AddSupportedInstructionSet("gfni_v512");
                }
                if ((flags & Avx512Bitalg) != 0)
                    builder.AddSupportedInstructionSet("avx512bitalg");
                if ((flags & Avx512Bf16) != 0)
                    builder.AddSupportedInstructionSet("avx512bf16");
                if ((flags & Avx512Fp16) != 0)
                    builder.AddSupportedInstructionSet("avx512fp16");
                if ((flags & Avx512Ifma) != 0)
                    builder.AddSupportedInstructionSet("avx512ifma");
                if ((flags & Avx512Vbmi2) != 0)
                    builder.AddSupportedInstructionSet("avx512vbmi2");
                if ((flags & Avx512Vnni) != 0)
                    builder.AddSupportedInstructionSet("avx512vnni");
                if ((flags & Avx512Vp2intersect) != 0)
                    builder.AddSupportedInstructionSet("avx512vp2intersect");
                if ((flags & Avx512Vpopcntdq) != 0)
                    builder.AddSupportedInstructionSet("avx512vpopcntdq");
                if ((flags & AvxIfma) != 0)
                    builder.AddSupportedInstructionSet("avxifma");
                if ((flags & F16c) != 0)
                    builder.AddSupportedInstructionSet("f16c");
                if ((flags & Sha) != 0)
                    builder.AddSupportedInstructionSet("sha");
                if ((flags & Vaes) != 0)
                {
                    builder.AddSupportedInstructionSet("vaes");
                    if ((flags & Avx512) != 0)
                        builder.AddSupportedInstructionSet("vaes_v512");
                }
                if ((flags & WaitPkg) != 0)
                    builder.AddSupportedInstructionSet("waitpkg");
            }

            public static long FromInstructionSet(InstructionSet instructionSet)
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
                    InstructionSet.X64_AVX512 => Avx512,
                    InstructionSet.X64_AVX512_X64 => Avx512,
                    InstructionSet.X64_AVX512VBMI => Avx512Vbmi,
                    InstructionSet.X64_AVX512VBMI_X64 => Avx512Vbmi,
                    InstructionSet.X64_X86Serialize => Serialize,
                    InstructionSet.X64_X86Serialize_X64 => Serialize,
                    InstructionSet.X64_AVX10v1 => Avx10v1,
                    InstructionSet.X64_AVX10v1_X64 => Avx10v1,
                    InstructionSet.X64_APX => Apx,
                    InstructionSet.X64_PCLMULQDQ_V256 => Vpclmulqdq,
                    InstructionSet.X64_PCLMULQDQ_V512 => (Vpclmulqdq | Avx512),
                    InstructionSet.X64_AVX10v2 => Avx10v2,
                    InstructionSet.X64_AVX10v2_X64 => Avx10v2,
                    InstructionSet.X64_GFNI => Gfni,
                    InstructionSet.X64_GFNI_X64 => Gfni,
                    InstructionSet.X64_GFNI_V256 => (Gfni | Avx),
                    InstructionSet.X64_GFNI_V512 => (Gfni | Avx512),
                    InstructionSet.X64_AES_V256 => (Vaes | Avx),
                    InstructionSet.X64_AES_V512 => (Vaes | Avx512),
                    InstructionSet.X64_AVXIFMA => AvxIfma,
                    InstructionSet.X64_AVXIFMA_X64 => AvxIfma,
                    InstructionSet.X64_F16C => F16c,
                    InstructionSet.X64_F16C_X64 => F16c,
                    InstructionSet.X64_SHA => Sha,
                    InstructionSet.X64_SHA_X64 => Sha,
                    InstructionSet.X64_WAITPKG => WaitPkg,
                    InstructionSet.X64_WAITPKG_X64 => WaitPkg,
                    InstructionSet.X64_AVX512BITALG => Avx512Bitalg,
                    InstructionSet.X64_AVX512BITALG_X64 => Avx512Bitalg,
                    InstructionSet.X64_AVX512BF16 => Avx512Bf16,
                    InstructionSet.X64_AVX512BF16_X64 => Avx512Bf16,
                    InstructionSet.X64_AVX512FP16 => Avx512Fp16,
                    InstructionSet.X64_AVX512FP16_X64 => Avx512Fp16,
                    InstructionSet.X64_AVX512IFMA => Avx512Ifma,
                    InstructionSet.X64_AVX512VBMI2 => Avx512Vbmi2,
                    InstructionSet.X64_AVX512VBMI2_X64 => Avx512Vbmi2,
                    InstructionSet.X64_AVX512VNNI => Avx512Vnni,
                    InstructionSet.X64_AVX512VP2INTERSECT => Avx512Vp2intersect,
                    InstructionSet.X64_AVX512VP2INTERSECT_X64 => Avx512Vp2intersect,
                    InstructionSet.X64_AVX512VPOPCNTDQ => Avx512Vpopcntdq,
                    InstructionSet.X64_AVX512VPOPCNTDQ_X64 => Avx512Vpopcntdq,

                    // Baseline ISAs - they're always available
                    InstructionSet.X64_X86Base => 0,
                    InstructionSet.X64_X86Base_X64 => 0,

                    // Vector<T> Sizes
                    InstructionSet.X64_VectorT128 => 0,
                    InstructionSet.X64_VectorT256 => Avx2,
                    InstructionSet.X64_VectorT512 => Avx512,

                    _ => throw new NotSupportedException(((InstructionSet_X64)instructionSet).ToString())
                };
            }
        }

        // Keep these enumerations in sync with cpufeatures.h in the minipal.
        private static class Arm64IntrinsicConstants
        {
            public const long Aes = (1L << 0);
            public const long Crc32 = (1L << 1);
            public const long Dp = (1L << 2);
            public const long Rdm = (1L << 3);
            public const long Sha1 = (1L << 4);
            public const long Sha256 = (1L << 5);
            public const long Atomics = (1L << 6);
            public const long Rcpc = (1L << 7);
            public const long Rcpc2 = (1L << 8);
            public const long Sve = (1L << 9);
            public const long Sve2 = (1L << 10);

            public static void AddToBuilder(InstructionSetSupportBuilder builder, long flags)
            {
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
                if ((flags & Rcpc2) != 0)
                    builder.AddSupportedInstructionSet("rcpc2");
                if ((flags & Sve) != 0)
                    builder.AddSupportedInstructionSet("sve");
                if ((flags & Sve2) != 0)
                    builder.AddSupportedInstructionSet("sve2");
            }

            public static long FromInstructionSet(InstructionSet instructionSet)
            {
                return instructionSet switch
                {

                    // Baseline ISAs - they're always available
                    InstructionSet.ARM64_ArmBase => 0,
                    InstructionSet.ARM64_ArmBase_Arm64 => 0,
                    InstructionSet.ARM64_AdvSimd => 0,
                    InstructionSet.ARM64_AdvSimd_Arm64 => 0,

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
                    InstructionSet.ARM64_Rcpc2 => Rcpc2,
                    InstructionSet.ARM64_Sve => Sve,
                    InstructionSet.ARM64_Sve_Arm64 => Sve,
                    InstructionSet.ARM64_Sve2 => Sve2,
                    InstructionSet.ARM64_Sve2_Arm64 => Sve2,

                    // Vector<T> Sizes
                    InstructionSet.ARM64_VectorT128 => 0,

                    _ => throw new NotSupportedException(((InstructionSet_ARM64)instructionSet).ToString())
                };
            }
        }

        // Keep these enumerations in sync with cpufeatures.h in the minipal.
        private static class RiscV64IntrinsicConstants
        {
            public const long Zba = (1L << 0);
            public const long Zbb = (1L << 1);

            public static void AddToBuilder(InstructionSetSupportBuilder builder, long flags)
            {
                if ((flags & Zba) != 0)
                    builder.AddSupportedInstructionSet("zba");
                if ((flags & Zbb) != 0)
                    builder.AddSupportedInstructionSet("zbb");
            }

            public static long FromInstructionSet(InstructionSet instructionSet)
            {
                return instructionSet switch
                {
                    // Baseline ISAs - they're always available
                    InstructionSet.RiscV64_RiscV64Base => 0,

                    // Optional ISAs - only available via opt-in or opportunistic light-up
                    InstructionSet.RiscV64_Zba => Zba,
                    InstructionSet.RiscV64_Zbb => Zbb,

                    _ => throw new NotSupportedException(((InstructionSet_RiscV64)instructionSet).ToString())
                };
            }
        }
    }
}
