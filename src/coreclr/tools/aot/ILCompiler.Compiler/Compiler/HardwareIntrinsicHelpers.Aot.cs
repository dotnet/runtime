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
        public static MethodIL EmitIsSupportedIL(MethodDesc method, FieldDesc isSupportedField, InstructionSet instructionSet)
        {
            Debug.Assert(IsIsSupportedMethod(method));
            Debug.Assert(isSupportedField.IsStatic && isSupportedField.FieldType.IsWellKnownType(WellKnownType.Int32));

            int flag = 0;

            switch (method.Context.Target.Architecture)
            {
                case TargetArchitecture.X86:
                case TargetArchitecture.X64:
                    flag = XArchIntrinsicConstants.FromInstructionSet(instructionSet);
                    break;

                case TargetArchitecture.ARM64:
                    flag = Arm64IntrinsicConstants.FromInstructionSet(instructionSet);
                    break;

                default:
                    Debug.Fail("Unsupported Architecture");
                    break;
            }

            var emit = new ILEmitter();
            ILCodeStream codeStream = emit.NewCodeStream();

            if(!uint.IsPow2((uint)flag))
            {
                // These are the ISAs managed by multiple-bit flags.
                // we need to emit different IL to handle the checks.
                // For now just Avx10v1_V512 = (Avx10v1 | Avx512)
                // (isSupportedField & flag) == flag
                codeStream.Emit(ILOpcode.ldsfld, emit.NewToken(isSupportedField));
                codeStream.EmitLdc(flag);
                codeStream.Emit(ILOpcode.and);
                codeStream.EmitLdc(flag);
                codeStream.Emit(ILOpcode.ceq);
                codeStream.Emit(ILOpcode.ret);
            }
            else
            {
                // (isSupportedField & flag) >= (unsigned)0
                codeStream.Emit(ILOpcode.ldsfld, emit.NewToken(isSupportedField));
                codeStream.EmitLdc(flag);
                codeStream.Emit(ILOpcode.and);
                codeStream.EmitLdc(0);
                codeStream.Emit(ILOpcode.cgt_un);
                codeStream.Emit(ILOpcode.ret);
            }

            return emit.Link(method);
        }

        public static int GetRuntimeRequiredIsaFlags(InstructionSetSupport instructionSetSupport)
        {
            int result = 0;
            switch (instructionSetSupport.Architecture)
            {
                case TargetArchitecture.X86:
                case TargetArchitecture.X64:
                    foreach (InstructionSet instructionSet in instructionSetSupport.SupportedFlags)
                        result |= XArchIntrinsicConstants.FromInstructionSet(instructionSet);
                    break;

                case TargetArchitecture.ARM64:
                    foreach (InstructionSet instructionSet in instructionSetSupport.SupportedFlags)
                        result |= Arm64IntrinsicConstants.FromInstructionSet(instructionSet);
                    break;

                default:
                    Debug.Fail("Unsupported Architecture");
                    break;
            }
            return result;
        }
    }
}
