// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

using Internal.TypeSystem;
using Internal.IL;
using Internal.IL.Stubs;
using Internal.JitInterface;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler
{
    public static partial class HardwareIntrinsicHelpers
    {
        public static bool IsIsSupportedMethod(MethodDesc method)
        {
            return method.Name == "get_IsSupported";
        }

        /// <summary>
        /// Generates IL for the IsSupported property that reads this information from a field initialized by the runtime
        /// at startup. Only works for intrinsics that the code generator can generate detection code for.
        /// </summary>
        public static MethodIL EmitIsSupportedIL(MethodDesc method, FieldDesc isSupportedField)
        {
            Debug.Assert(IsIsSupportedMethod(method));
            Debug.Assert(isSupportedField.IsStatic && isSupportedField.FieldType.IsWellKnownType(WellKnownType.Int32));

            string id = InstructionSetSupport.GetHardwareIntrinsicId(method.Context.Target.Architecture, method.OwningType);

            int flag = 0;

            switch (method.Context.Target.Architecture)
            {
                case TargetArchitecture.X86:
                case TargetArchitecture.X64:
                    flag = XArchIntrinsicConstants.FromHardwareIntrinsicId(id);
                    break;

                case TargetArchitecture.ARM64:
                    flag = Arm64IntrinsicConstants.FromHardwareIntrinsicId(id);
                    break;

                default:
                    Debug.Fail("Unsupported Architecture");
                    break;
            }

            var emit = new ILEmitter();
            ILCodeStream codeStream = emit.NewCodeStream();

            codeStream.Emit(ILOpcode.ldsfld, emit.NewToken(isSupportedField));
            codeStream.EmitLdc(flag);
            codeStream.Emit(ILOpcode.and);
            codeStream.EmitLdc(0);
            codeStream.Emit(ILOpcode.cgt_un);
            codeStream.Emit(ILOpcode.ret);

            return emit.Link(method);
        }

        public static int GetRuntimeRequiredIsaFlags(InstructionSetSupport instructionSetSupport)
        {
            switch (instructionSetSupport.Architecture)
            {
                case TargetArchitecture.X86:
                case TargetArchitecture.X64:
                    return XArchIntrinsicConstants.FromInstructionSetFlags(instructionSetSupport.SupportedFlags);

                case TargetArchitecture.ARM64:
                    return Arm64IntrinsicConstants.FromInstructionSetFlags(instructionSetSupport.SupportedFlags);

                default:
                    Debug.Fail("Unsupported Architecture");
                    return 0;
            }
        }

        // Keep these enumerations in sync with startup.cpp in the native runtime.
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

            public static int FromHardwareIntrinsicId(string id)
            {
                return id switch
                {
                    "Aes" => Aes,
                    "Pclmulqdq" => Pclmulqdq,
                    "Sse3" => Sse3,
                    "Ssse3" => Ssse3,
                    "Sse41" => Sse41,
                    "Sse42" => Sse42,
                    "Popcnt" => Popcnt,
                    "Avx" => Avx,
                    "Fma" => Fma,
                    "Avx2" => Avx2,
                    "Bmi1" => Bmi1,
                    "Bmi2" => Bmi2,
                    "Lzcnt" => Lzcnt,
                    "AvxVnni" => AvxVnni,
                    _ => throw new NotSupportedException(),
                };
            }

            public static int FromInstructionSetFlags(InstructionSetFlags instructionSets)
            {
                int result = 0;

                Debug.Assert(InstructionSet.X64_AES == InstructionSet.X86_AES);
                Debug.Assert(InstructionSet.X64_SSE41 == InstructionSet.X86_SSE41);
                Debug.Assert(InstructionSet.X64_LZCNT == InstructionSet.X86_LZCNT);

                foreach (InstructionSet instructionSet in instructionSets)
                {
                    result |= instructionSet switch
                    {
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
                        InstructionSet.X64_LZCNT_X64 => Popcnt,
                        InstructionSet.X64_AVXVNNI => AvxVnni,
                        InstructionSet.X64_AVXVNNI_X64 => AvxVnni,

                        // SSE and SSE2 are baseline ISAs - they're always available
                        InstructionSet.X64_SSE => 0,
                        InstructionSet.X64_SSE_X64 => 0,
                        InstructionSet.X64_SSE2 => 0,
                        InstructionSet.X64_SSE2_X64 => 0,
                        InstructionSet.X64_X86Base => 0,
                        InstructionSet.X64_X86Base_X64 => 0,

                        _ => throw new NotSupportedException(instructionSet.ToString())
                    };
                }

                return result;
            }
        }

        private static class Arm64IntrinsicConstants
        {
            public const int ArmBase = 0x0001;
            public const int ArmBase_Arm64 = 0x0002;
            public const int AdvSimd = 0x0004;
            public const int AdvSimd_Arm64 = 0x0008;
            public const int Aes = 0x0010;
            public const int Crc32 = 0x0020;
            public const int Crc32_Arm64 = 0x0040;
            public const int Sha1 = 0x0080;
            public const int Sha256 = 0x0100;
            public const int Atomics = 0x0200;
            public const int Vector64 = 0x0400;
            public const int Vector128 = 0x0800;
            public const int Rcpc = 0x1000;

            public static int FromHardwareIntrinsicId(string id)
            {
                return id switch
                {
                    "ArmBase" => ArmBase,
                    "ArmBase_Arm64" => ArmBase_Arm64,
                    "AdvSimd" => AdvSimd,
                    "AdvSimd_Arm64" => AdvSimd_Arm64,
                    "Aes" => Aes,
                    "Crc32" => Crc32,
                    "Crc32_Arm64" => Crc32_Arm64,
                    "Sha1" => Sha1,
                    "Sha256" => Sha256,
                    "Atomics" => Atomics,
                    "Vector64" => Vector64,
                    "Vector128" => Vector128,
                    "Rcpc" => Rcpc,
               _ => throw new NotSupportedException(),
                };
            }

            public static int FromInstructionSetFlags(InstructionSetFlags instructionSets)
            {
                int result = 0;

                foreach (InstructionSet instructionSet in instructionSets)
                {
                    result |= instructionSet switch
                    {
                        InstructionSet.ARM64_ArmBase => ArmBase,
                        InstructionSet.ARM64_ArmBase_Arm64 => ArmBase_Arm64,
                        InstructionSet.ARM64_AdvSimd => AdvSimd,
                        InstructionSet.ARM64_AdvSimd_Arm64 => AdvSimd_Arm64,
                        InstructionSet.ARM64_Aes => Aes,
                        InstructionSet.ARM64_Crc32 => Crc32,
                        InstructionSet.ARM64_Crc32_Arm64 => Crc32_Arm64,
                        InstructionSet.ARM64_Sha1 => Sha1,
                        InstructionSet.ARM64_Sha256 => Sha256,
                        InstructionSet.ARM64_Atomics => Atomics,
                        InstructionSet.ARM64_Vector64 => Vector64,
                        InstructionSet.ARM64_Vector128 => Vector128,
                        InstructionSet.ARM64_Rcpc => Rcpc,
                        _ => throw new NotSupportedException()
                    };
                }

                return result;
            }
        }
    }
}
