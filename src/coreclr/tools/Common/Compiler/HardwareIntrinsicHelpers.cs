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
        public static int NUM_PARTS = 3;

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
                string ns = owningMdType.ContainingType?.Namespace ?? owningMdType.Namespace;
                return method.Context.Target.Architecture switch
                {
                    TargetArchitecture.ARM64 => ns == "System.Runtime.Intrinsics.Arm",
                    TargetArchitecture.X64 or TargetArchitecture.X86 => ns == "System.Runtime.Intrinsics.X86",
                    _ => false,
                };
            }

            return false;
        }

        public static void AddRuntimeRequiredIsaFlagsToBuilder(InstructionSetSupportBuilder builder, HardwareIntrinsicConstants flags)
        {
            switch (builder.Architecture)
            {
                case TargetArchitecture.X86:
                case TargetArchitecture.X64:
                    XArchIntrinsicFeatures.AddToBuilder(builder, flags);
                    break;
                case TargetArchitecture.ARM64:
                    Arm64IntrinsicFeatures.AddToBuilder(builder, flags);
                    break;
                default:
                    Debug.Fail("Probably unimplemented");
                    break;
            }
        }

        // Keep these enumerations in sync with cpufeatures.h in the minipal.
        // private static class XArchIntrinsicConstants
        // {

        //     public static void AddToBuilder(InstructionSetSupportBuilder builder, int flags)
        //     {
        //         if ((flags & Aes) != 0)
        //             builder.AddSupportedInstructionSet("aes");
        //         if ((flags & Pclmulqdq) != 0)
        //             builder.AddSupportedInstructionSet("pclmul");
        //         if ((flags & Sse3) != 0)
        //             builder.AddSupportedInstructionSet("sse3");
        //         if ((flags & Ssse3) != 0)
        //             builder.AddSupportedInstructionSet("ssse3");
        //         if ((flags & Sse41) != 0)
        //             builder.AddSupportedInstructionSet("sse4.1");
        //         if ((flags & Sse42) != 0)
        //             builder.AddSupportedInstructionSet("sse4.2");
        //         if ((flags & Popcnt) != 0)
        //             builder.AddSupportedInstructionSet("popcnt");
        //         if ((flags & Avx) != 0)
        //             builder.AddSupportedInstructionSet("avx");
        //         if ((flags & Fma) != 0)
        //             builder.AddSupportedInstructionSet("fma");
        //         if ((flags & Avx2) != 0)
        //             builder.AddSupportedInstructionSet("avx2");
        //         if ((flags & Bmi1) != 0)
        //             builder.AddSupportedInstructionSet("bmi");
        //         if ((flags & Bmi2) != 0)
        //             builder.AddSupportedInstructionSet("bmi2");
        //         if ((flags & Lzcnt) != 0)
        //             builder.AddSupportedInstructionSet("lzcnt");
        //         if ((flags & AvxVnni) != 0)
        //             builder.AddSupportedInstructionSet("avxvnni");
        //         if ((flags & Movbe) != 0)
        //             builder.AddSupportedInstructionSet("movbe");
        //         if ((flags & Avx512f) != 0)
        //             builder.AddSupportedInstructionSet("avx512f");
        //         if ((flags & Avx512f_vl) != 0)
        //             builder.AddSupportedInstructionSet("avx512f_vl");
        //         if ((flags & Avx512bw) != 0)
        //             builder.AddSupportedInstructionSet("avx512bw");
        //         if ((flags & Avx512bw_vl) != 0)
        //             builder.AddSupportedInstructionSet("avx512bw_vl");
        //         if ((flags & Avx512cd) != 0)
        //             builder.AddSupportedInstructionSet("avx512cd");
        //         if ((flags & Avx512cd_vl) != 0)
        //             builder.AddSupportedInstructionSet("avx512cd_vl");
        //         if ((flags & Avx512dq) != 0)
        //             builder.AddSupportedInstructionSet("avx512dq");
        //         if ((flags & Avx512dq_vl) != 0)
        //             builder.AddSupportedInstructionSet("avx512dq_vl");
        //         if ((flags & Avx512Vbmi) != 0)
        //             builder.AddSupportedInstructionSet("avx512vbmi");
        //         if ((flags & Avx512Vbmi_vl) != 0)
        //             builder.AddSupportedInstructionSet("avx512vbmi_vl");
        //         if ((flags & Serialize) != 0)
        //             builder.AddSupportedInstructionSet("serialize");
        //         if ((flags & Avx10v1) != 0)
        //             builder.AddSupportedInstructionSet("avx10v1");
        //         if ((flags & Avx10v1_v256) != 0)
        //             builder.AddSupportedInstructionSet("avx10v1_v256");
        //         if ((flags & Avx10v1_v512) != 0)
        //             builder.AddSupportedInstructionSet("avx10v1_v512");
        //     }

        //     public static int FromInstructionSet(InstructionSet instructionSet)
        //     {
        //         Debug.Assert(InstructionSet.X64_AES == InstructionSet.X86_AES);
        //         Debug.Assert(InstructionSet.X64_SSE41 == InstructionSet.X86_SSE41);
        //         Debug.Assert(InstructionSet.X64_LZCNT == InstructionSet.X86_LZCNT);

        //         return instructionSet switch
        //         {
        //             // Optional ISAs - only available via opt-in or opportunistic light-up
        //             InstructionSet.X64_AES => Aes,
        //             InstructionSet.X64_AES_X64 => Aes,
        //             InstructionSet.X64_PCLMULQDQ => Pclmulqdq,
        //             InstructionSet.X64_PCLMULQDQ_X64 => Pclmulqdq,
        //             InstructionSet.X64_SSE3 => Sse3,
        //             InstructionSet.X64_SSE3_X64 => Sse3,
        //             InstructionSet.X64_SSSE3 => Ssse3,
        //             InstructionSet.X64_SSSE3_X64 => Ssse3,
        //             InstructionSet.X64_SSE41 => Sse41,
        //             InstructionSet.X64_SSE41_X64 => Sse41,
        //             InstructionSet.X64_SSE42 => Sse42,
        //             InstructionSet.X64_SSE42_X64 => Sse42,
        //             InstructionSet.X64_POPCNT => Popcnt,
        //             InstructionSet.X64_POPCNT_X64 => Popcnt,
        //             InstructionSet.X64_AVX => Avx,
        //             InstructionSet.X64_AVX_X64 => Avx,
        //             InstructionSet.X64_FMA => Fma,
        //             InstructionSet.X64_FMA_X64 => Fma,
        //             InstructionSet.X64_AVX2 => Avx2,
        //             InstructionSet.X64_AVX2_X64 => Avx2,
        //             InstructionSet.X64_BMI1 => Bmi1,
        //             InstructionSet.X64_BMI1_X64 => Bmi1,
        //             InstructionSet.X64_BMI2 => Bmi2,
        //             InstructionSet.X64_BMI2_X64 => Bmi2,
        //             InstructionSet.X64_LZCNT => Lzcnt,
        //             InstructionSet.X64_LZCNT_X64 => Lzcnt,
        //             InstructionSet.X64_AVXVNNI => AvxVnni,
        //             InstructionSet.X64_AVXVNNI_X64 => AvxVnni,
        //             InstructionSet.X64_MOVBE => Movbe,
        //             InstructionSet.X64_MOVBE_X64 => Movbe,
        //             InstructionSet.X64_AVX512F => Avx512f,
        //             InstructionSet.X64_AVX512F_X64 => Avx512f,
        //             InstructionSet.X64_AVX512F_VL => Avx512f_vl,
        //             InstructionSet.X64_AVX512F_VL_X64 => Avx512f_vl,
        //             InstructionSet.X64_AVX512BW => Avx512bw,
        //             InstructionSet.X64_AVX512BW_X64 => Avx512bw,
        //             InstructionSet.X64_AVX512BW_VL => Avx512bw_vl,
        //             InstructionSet.X64_AVX512BW_VL_X64 => Avx512bw_vl,
        //             InstructionSet.X64_AVX512CD => Avx512cd,
        //             InstructionSet.X64_AVX512CD_X64 => Avx512cd,
        //             InstructionSet.X64_AVX512CD_VL => Avx512cd_vl,
        //             InstructionSet.X64_AVX512CD_VL_X64 => Avx512cd_vl,
        //             InstructionSet.X64_AVX512DQ => Avx512dq,
        //             InstructionSet.X64_AVX512DQ_X64 => Avx512dq,
        //             InstructionSet.X64_AVX512DQ_VL => Avx512dq_vl,
        //             InstructionSet.X64_AVX512DQ_VL_X64 => Avx512dq_vl,
        //             InstructionSet.X64_AVX512VBMI => Avx512Vbmi,
        //             InstructionSet.X64_AVX512VBMI_X64 => Avx512Vbmi,
        //             InstructionSet.X64_AVX512VBMI_VL => Avx512Vbmi_vl,
        //             InstructionSet.X64_AVX512VBMI_VL_X64 => Avx512Vbmi_vl,
        //             InstructionSet.X64_X86Serialize => Serialize,
        //             InstructionSet.X64_X86Serialize_X64 => Serialize,
        //             InstructionSet.X64_AVX10v1 => Avx10v1,
        //             InstructionSet.X64_AVX10v1_X64 => Avx10v1,
        //             InstructionSet.X64_AVX10v1_V256 => Avx10v1_v256,
        //             InstructionSet.X64_AVX10v1_V256_X64 => Avx10v1_v256,
        //             InstructionSet.X64_AVX10v1_V512 => Avx10v1_v512,
        //             InstructionSet.X64_AVX10v1_V512_X64 => Avx10v1_v512,

        //             // Baseline ISAs - they're always available
        //             InstructionSet.X64_SSE => 0,
        //             InstructionSet.X64_SSE_X64 => 0,
        //             InstructionSet.X64_SSE2 => 0,
        //             InstructionSet.X64_SSE2_X64 => 0,

        //             InstructionSet.X64_X86Base => 0,
        //             InstructionSet.X64_X86Base_X64 => 0,

        //             // Vector<T> Sizes
        //             InstructionSet.X64_VectorT128 => VectorT128,
        //             InstructionSet.X64_VectorT256 => VectorT256,
        //             InstructionSet.X64_VectorT512 => VectorT512,

        //             _ => throw new NotSupportedException(((InstructionSet_X64)instructionSet).ToString())
        //         };
        //     }
        // }

        public class HardwareIntrinsicConstants
        {
            public uint[] parts = new uint[NUM_PARTS];

            public HardwareIntrinsicConstants(){}
        }

        public static void SetFlag(ref HardwareIntrinsicConstants constants, int flag)
        {
            constants.parts[flag / 32] |= (uint)(1 << (flag % 32));
        }

        public static void ClearFlag(ref HardwareIntrinsicConstants constants, int flag)
        {
            constants.parts[flag / 32] &= (uint)~(1 << (flag % 32));
        }

        public static bool IsFlagSet(ref HardwareIntrinsicConstants constants, int flag)
        {
            return (constants.parts[flag / 32] & (1 << (flag % 32))) != 0;
        }

        public static class XArchIntrinsicFeatures
        {

            public const int Aes = 0;
            public const int Pclmulqdq = 1;
            public const int Sse3 = 2;
            public const int Ssse3 = 3;
            public const int Sse41 = 4;
            public const int Sse42 = 5;
            public const int Popcnt = 6;
            public const int Avx = 7;
            public const int Fma = 8;
            public const int Avx2 = 9;
            public const int Bmi1 = 10;
            public const int Bmi2 = 11;
            public const int Lzcnt = 12;
            public const int AvxVnni = 13;
            public const int Movbe = 14;
            public const int Avx512f = 15;
            public const int Avx512f_vl = 16;
            public const int Avx512bw = 17;
            public const int Avx512bw_vl = 18;
            public const int Avx512cd = 19;
            public const int Avx512cd_vl = 20;
            public const int Avx512dq = 21;
            public const int Avx512dq_vl = 22;
            public const int Avx512Vbmi = 23;
            public const int Avx512Vbmi_vl = 24;
            public const int Serialize = 25;
            public const int VectorT128 = 26;
            public const int VectorT256 = 27;
            public const int VectorT512 = 28;
            public const int Avx10v1 = 29;
            public const int Avx10v1_v256 = 30;
            public const int Avx10v1_v512 = 31;

            public static void AddToBuilder(InstructionSetSupportBuilder builder, HardwareIntrinsicConstants flags)
            {
                if (IsFlagSet(ref flags, Aes))
                    builder.AddSupportedInstructionSet("aes");
                if (IsFlagSet(ref flags, Pclmulqdq))
                    builder.AddSupportedInstructionSet("pclmul");
                if (IsFlagSet(ref flags, Sse3))
                    builder.AddSupportedInstructionSet("sse3");
                if (IsFlagSet(ref flags, Ssse3))
                    builder.AddSupportedInstructionSet("ssse3");
                if (IsFlagSet(ref flags, Sse41))
                    builder.AddSupportedInstructionSet("sse4.1");
                if (IsFlagSet(ref flags, Sse42))
                    builder.AddSupportedInstructionSet("sse4.2");
                if (IsFlagSet(ref flags, Popcnt))
                    builder.AddSupportedInstructionSet("popcnt");
                if (IsFlagSet(ref flags, Avx))
                    builder.AddSupportedInstructionSet("avx");
                if (IsFlagSet(ref flags, Fma))
                    builder.AddSupportedInstructionSet("fma");
                if (IsFlagSet(ref flags, Avx2))
                    builder.AddSupportedInstructionSet("avx2");
                if (IsFlagSet(ref flags, Bmi1))
                    builder.AddSupportedInstructionSet("bmi");
                if (IsFlagSet(ref flags, Bmi2))
                    builder.AddSupportedInstructionSet("bmi2");
                if (IsFlagSet(ref flags, Lzcnt))
                    builder.AddSupportedInstructionSet("lzcnt");
                if (IsFlagSet(ref flags, AvxVnni))
                    builder.AddSupportedInstructionSet("avxvnni");
                if (IsFlagSet(ref flags, Movbe))
                    builder.AddSupportedInstructionSet("movbe");
                if (IsFlagSet(ref flags, Avx512f))
                    builder.AddSupportedInstructionSet("avx512f");
                if (IsFlagSet(ref flags, Avx512f_vl))
                    builder.AddSupportedInstructionSet("avx512f_vl");
                if (IsFlagSet(ref flags, Avx512bw))
                    builder.AddSupportedInstructionSet("avx512bw");
                if (IsFlagSet(ref flags, Avx512bw_vl))
                    builder.AddSupportedInstructionSet("avx512bw_vl");
                if (IsFlagSet(ref flags, Avx512cd))
                    builder.AddSupportedInstructionSet("avx512cd");
                if (IsFlagSet(ref flags, Avx512cd_vl))
                    builder.AddSupportedInstructionSet("avx512cd_vl");
                if (IsFlagSet(ref flags, Avx512dq))
                    builder.AddSupportedInstructionSet("avx512dq");
                if (IsFlagSet(ref flags, Avx512dq_vl))
                    builder.AddSupportedInstructionSet("avx512dq_vl");
                if (IsFlagSet(ref flags, Avx512Vbmi))
                    builder.AddSupportedInstructionSet("avx512vbmi");
                if (IsFlagSet(ref flags, Avx512Vbmi_vl))
                    builder.AddSupportedInstructionSet("avx512vbmi_vl");
                if (IsFlagSet(ref flags, Serialize))
                    builder.AddSupportedInstructionSet("serialize");
                if (IsFlagSet(ref flags, VectorT128))
                    builder.AddSupportedInstructionSet("VectorT128");
                if (IsFlagSet(ref flags, VectorT256))
                    builder.AddSupportedInstructionSet("VectorT256");
                if (IsFlagSet(ref flags, VectorT512))
                    builder.AddSupportedInstructionSet("VectorT512");
                if (IsFlagSet(ref flags, Avx10v1))
                    builder.AddSupportedInstructionSet("avx10v1");
                if (IsFlagSet(ref flags, Avx10v1_v256))
                    builder.AddSupportedInstructionSet("avx10v1_v256");
                if (IsFlagSet(ref flags, Avx10v1_v512))
                    builder.AddSupportedInstructionSet("avx10v1_v512");
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
                    InstructionSet.X64_AVX10v1 => Avx10v1,
                    InstructionSet.X64_AVX10v1_X64 => Avx10v1,
                    InstructionSet.X64_AVX10v1_V256 => Avx10v1_v256,
                    InstructionSet.X64_AVX10v1_V256_X64 => Avx10v1_v256,
                    InstructionSet.X64_AVX10v1_V512 => Avx10v1_v512,
                    InstructionSet.X64_AVX10v1_V512_X64 => Avx10v1_v512,

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

                    _ => throw new NotSupportedException(((InstructionSet)instructionSet).ToString())
                };
            }
        }

        // Keep these enumerations in sync with cpufeatures.h in the minipal.
        private static class Arm64IntrinsicFeatures
        {
            public const int AdvSimd = 0;
            public const int Aes = 1;
            public const int Crc32 = 2;
            public const int Dp = 3;
            public const int Rdm = 4;
            public const int Sha1 = 5;
            public const int Sha256 = 6;
            public const int Atomics = 6;
            public const int Rcpc = 7;
            public const int VectorT128 = 8;
            public const int Rcpc2 = 10;
            public const int Sve = 11;

            public static void AddToBuilder(InstructionSetSupportBuilder builder, HardwareIntrinsicConstants flags)
            {
                if (IsFlagSet(ref flags, AdvSimd))
                    builder.AddSupportedInstructionSet("neon");
                if (IsFlagSet(ref flags, Aes))
                    builder.AddSupportedInstructionSet("aes");
                if (IsFlagSet(ref flags, Crc32))
                    builder.AddSupportedInstructionSet("crc");
                if (IsFlagSet(ref flags, Dp))
                    builder.AddSupportedInstructionSet("dotprod");
                if (IsFlagSet(ref flags, Rdm))
                    builder.AddSupportedInstructionSet("rdma");
                if (IsFlagSet(ref flags, Sha1))
                    builder.AddSupportedInstructionSet("sha1");
                if (IsFlagSet(ref flags, Sha256))
                    builder.AddSupportedInstructionSet("sha2");
                if (IsFlagSet(ref flags, Atomics))
                    builder.AddSupportedInstructionSet("lse");
                if (IsFlagSet(ref flags, Rcpc))
                    builder.AddSupportedInstructionSet("rcpc");
                if (IsFlagSet(ref flags, Rcpc2))
                    builder.AddSupportedInstructionSet("rcpc2");
                if (IsFlagSet(ref flags, Sve))
                    builder.AddSupportedInstructionSet("sve");
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
                    InstructionSet.ARM64_Rcpc2 => Rcpc2,
                    InstructionSet.ARM64_Sve => Sve,
                    InstructionSet.ARM64_Sve_Arm64 => Sve,

                    // Vector<T> Sizes
                    InstructionSet.ARM64_VectorT128 => VectorT128,

                    _ => throw new NotSupportedException(((InstructionSet_ARM64)instructionSet).ToString())
                };
            }
        }
    }
}
