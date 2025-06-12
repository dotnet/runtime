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
            public const int Sse42 = (1 << 0);
            public const int Avx = (1 << 1);
            public const int Avx2 = (1 << 2);
            public const int Avx512 = (1 << 3);

            public const int Avx512v2 = (1 << 4);
            public const int Avx512v3 = (1 << 5);
            public const int Avx10v1 = (1 << 6);
            public const int Avx10v2 = (1 << 7);
            public const int Apx = (1 << 8);

            public const int Aes = (1 << 9);
            public const int Avx512Vp2intersect = (1 << 10);
            public const int AvxIfma = (1 << 11);
            public const int AvxVnni = (1 << 12);
            public const int Gfni = (1 << 13);
            public const int Sha = (1 << 14);
            public const int Vaes = (1 << 15);
            public const int WaitPkg = (1 << 16);
            public const int X86Serialize = (1 << 17);

            public static void AddToBuilder(InstructionSetSupportBuilder builder, int flags)
            {
                if ((flags & Sse42) != 0)
                    builder.AddSupportedInstructionSet("sse42");
                if ((flags & Avx) != 0)
                    builder.AddSupportedInstructionSet("avx");
                if ((flags & Avx2) != 0)
                    builder.AddSupportedInstructionSet("avx2");
                if ((flags & Avx512) != 0)
                    builder.AddSupportedInstructionSet("avx512");
                if ((flags & Avx512v2) != 0)
                    builder.AddSupportedInstructionSet("avx512v2");
                if ((flags & Avx512v3) != 0)
                    builder.AddSupportedInstructionSet("avx512v3");
                if ((flags & Avx10v1) != 0)
                    builder.AddSupportedInstructionSet("avx10v1");
                if ((flags & Avx10v2) != 0)
                    builder.AddSupportedInstructionSet("avx10v2");
                if ((flags & Apx) != 0)
                    builder.AddSupportedInstructionSet("apx");

                if ((flags & Aes) != 0)
                {
                    builder.AddSupportedInstructionSet("aes");
                    if ((flags & Vaes) != 0)
                    {
                        builder.AddSupportedInstructionSet("aes_v256");
                        if ((flags & Avx512) != 0)
                            builder.AddSupportedInstructionSet("vaes_v512");
                    }
                }
                if ((flags & Avx512Vp2intersect) != 0)
                    builder.AddSupportedInstructionSet("avx512vp2intersect");
                if ((flags & AvxIfma) != 0)
                    builder.AddSupportedInstructionSet("avxifma");
                if ((flags & AvxVnni) != 0)
                    builder.AddSupportedInstructionSet("avxvnni");
                if ((flags & Gfni) != 0)
                {
                    builder.AddSupportedInstructionSet("gfni");
                    if ((flags & Avx) != 0)
                        builder.AddSupportedInstructionSet("gfni_v256");
                    if ((flags & Avx512) != 0)
                        builder.AddSupportedInstructionSet("gfni_v512");
                }
                if ((flags & Sha) != 0)
                    builder.AddSupportedInstructionSet("sha");
                if ((flags & WaitPkg) != 0)
                    builder.AddSupportedInstructionSet("waitpkg");
                if ((flags & X86Serialize) != 0)
                    builder.AddSupportedInstructionSet("x86serialize");
            }

            public static int FromInstructionSet(InstructionSet instructionSet)
            {
                Debug.Assert(InstructionSet.X64_AES == InstructionSet.X86_AES);
                Debug.Assert(InstructionSet.X64_SSE42 == InstructionSet.X86_SSE42);
                Debug.Assert(InstructionSet.X64_AVX2 == InstructionSet.X86_AVX2);

                return instructionSet switch
                {
                    // Optional ISAs - only available via opt-in or opportunistic light-up
                    InstructionSet.X64_SSE42 => Sse42,
                    InstructionSet.X64_SSE42_X64 => Sse42,

                    InstructionSet.X64_AVX => Avx,
                    InstructionSet.X64_AVX_X64 => Avx,

                    InstructionSet.X64_AVX2 => Avx2,
                    InstructionSet.X64_AVX2_X64 => Avx2,

                    InstructionSet.X64_AVX512 => Avx512,
                    InstructionSet.X64_AVX512_X64 => Avx512,

                    InstructionSet.X64_AVX512v2 => Avx512v2,
                    InstructionSet.X64_AVX512v2_X64 => Avx512v2,

                    InstructionSet.X64_AVX512v3 => Avx512v3,
                    InstructionSet.X64_AVX512v3_X64 => Avx512v3,

                    InstructionSet.X64_AVX10v1 => Avx10v1,
                    InstructionSet.X64_AVX10v1_X64 => Avx10v1,

                    InstructionSet.X64_AVX10v2 => Avx10v2,
                    InstructionSet.X64_AVX10v2_X64 => Avx10v2,

                    InstructionSet.X64_APX => Apx,

                    InstructionSet.X64_AES => Aes,
                    InstructionSet.X64_AES_X64 => Aes,
                    InstructionSet.X64_AES_V256 => (Vaes | Avx),
                    InstructionSet.X64_AES_V512 => (Vaes | Avx512),

                    InstructionSet.X64_AVX512VP2INTERSECT => Avx512Vp2intersect,
                    InstructionSet.X64_AVX512VP2INTERSECT_X64 => Avx512Vp2intersect,

                    InstructionSet.X64_AVXIFMA => AvxIfma,
                    InstructionSet.X64_AVXIFMA_X64 => AvxIfma,

                    InstructionSet.X64_AVXVNNI => AvxVnni,
                    InstructionSet.X64_AVXVNNI_X64 => AvxVnni,

                    InstructionSet.X64_GFNI => Gfni,
                    InstructionSet.X64_GFNI_X64 => Gfni,
                    InstructionSet.X64_GFNI_V256 => (Gfni | Avx),
                    InstructionSet.X64_GFNI_V512 => (Gfni | Avx512),

                    InstructionSet.X64_SHA => Sha,
                    InstructionSet.X64_SHA_X64 => Sha,

                    InstructionSet.X64_WAITPKG => WaitPkg,
                    InstructionSet.X64_WAITPKG_X64 => WaitPkg,

                    InstructionSet.X64_X86Serialize => X86Serialize,
                    InstructionSet.X64_X86Serialize_X64 => X86Serialize,

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
            public const int Aes = (1 << 0);
            public const int Crc32 = (1 << 1);
            public const int Dp = (1 << 2);
            public const int Rdm = (1 << 3);
            public const int Sha1 = (1 << 4);
            public const int Sha256 = (1 << 5);
            public const int Atomics = (1 << 6);
            public const int Rcpc = (1 << 7);
            public const int Rcpc2 = (1 << 8);
            public const int Sve = (1 << 9);
            public const int Sve2 = (1 << 10);

            public static void AddToBuilder(InstructionSetSupportBuilder builder, int flags)
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

            public static int FromInstructionSet(InstructionSet instructionSet)
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
            public const int Zba = (1 << 0);
            public const int Zbb = (1 << 1);

            public static void AddToBuilder(InstructionSetSupportBuilder builder, int flags)
            {
                if ((flags & Zba) != 0)
                    builder.AddSupportedInstructionSet("zba");
                if ((flags & Zbb) != 0)
                    builder.AddSupportedInstructionSet("zbb");
            }

            public static int FromInstructionSet(InstructionSet instructionSet)
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
