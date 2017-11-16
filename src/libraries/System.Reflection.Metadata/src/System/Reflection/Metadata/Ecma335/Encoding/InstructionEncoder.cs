// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Reflection.Metadata.Ecma335
{
    /// <summary>
    /// Encodes instructions.
    /// </summary>
    public readonly struct InstructionEncoder
    {
        /// <summary>
        /// Underlying builder where encoded instructions are written to.
        /// </summary>
        public BlobBuilder CodeBuilder { get; }

        /// <summary>
        /// Builder tracking labels, branches and exception handlers.
        /// </summary>
        /// <remarks>
        /// If null the encoder doesn't support construction of control flow.
        /// </remarks>
        public ControlFlowBuilder ControlFlowBuilder { get; }

        /// <summary>
        /// Creates an encoder backed by code and control-flow builders.
        /// </summary>
        /// <param name="codeBuilder">Builder to write encoded instructions to.</param>
        /// <param name="controlFlowBuilder">
        /// Builder tracking labels, branches and exception handlers.
        /// Must be specified to be able to use some of the control-flow factory methods of <see cref="InstructionEncoder"/>,
        /// such as <see cref="Branch(ILOpCode, LabelHandle)"/>, <see cref="DefineLabel"/>, <see cref="MarkLabel(LabelHandle)"/> etc.
        /// </param>
        public InstructionEncoder(BlobBuilder codeBuilder, ControlFlowBuilder controlFlowBuilder = null)
        {
            if (codeBuilder == null)
            {
                Throw.BuilderArgumentNull();
            }

            CodeBuilder = codeBuilder;
            ControlFlowBuilder = controlFlowBuilder;
        }

        /// <summary>
        /// Offset of the next encoded instruction.
        /// </summary>
        public int Offset => CodeBuilder.Count;

        /// <summary>
        /// Encodes specified op-code.
        /// </summary>
        public void OpCode(ILOpCode code)
        {
            if (unchecked((byte)code) == (ushort)code)
            {
                CodeBuilder.WriteByte((byte)code);
            }
            else
            {
                // IL opcodes that occupy two bytes are written to
                // the byte stream with the high-order byte first,
                // in contrast to the little-endian format of the
                // numeric arguments and tokens.
                CodeBuilder.WriteUInt16BE((ushort)code);
            }
        }

        /// <summary>
        /// Encodes a token.
        /// </summary>
        public void Token(EntityHandle handle)
        {
            Token(MetadataTokens.GetToken(handle));
        }

        /// <summary>
        /// Encodes a token.
        /// </summary>
        public void Token(int token)
        {
            CodeBuilder.WriteInt32(token);
        }

        /// <summary>
        /// Encodes <code>ldstr</code> instruction and its operand.
        /// </summary>
        public void LoadString(UserStringHandle handle)
        {
            OpCode(ILOpCode.Ldstr);
            Token(MetadataTokens.GetToken(handle));
        }

        /// <summary>
        /// Encodes <code>call</code> instruction and its operand.
        /// </summary>
        public void Call(EntityHandle methodHandle)
        {
            if (methodHandle.Kind != HandleKind.MethodDefinition &&
                methodHandle.Kind != HandleKind.MethodSpecification &&
                methodHandle.Kind != HandleKind.MemberReference)
            {
                Throw.InvalidArgument_Handle(nameof(methodHandle));
            }

            OpCode(ILOpCode.Call);
            Token(methodHandle);
        }

        /// <summary>
        /// Encodes <code>call</code> instruction and its operand.
        /// </summary>
        public void Call(MethodDefinitionHandle methodHandle)
        {
            OpCode(ILOpCode.Call);
            Token(methodHandle);
        }

        /// <summary>
        /// Encodes <code>call</code> instruction and its operand.
        /// </summary>
        public void Call(MethodSpecificationHandle methodHandle)
        {
            OpCode(ILOpCode.Call);
            Token(methodHandle);
        }

        /// <summary>
        /// Encodes <code>call</code> instruction and its operand.
        /// </summary>
        public void Call(MemberReferenceHandle methodHandle)
        {
            OpCode(ILOpCode.Call);
            Token(methodHandle);
        }

        /// <summary>
        /// Encodes <code>calli</code> instruction and its operand.
        /// </summary>
        public void CallIndirect(StandaloneSignatureHandle signature)
        {
            OpCode(ILOpCode.Calli);
            Token(signature);
        }

        /// <summary>
        /// Encodes <see cref="int"/> constant load instruction.
        /// </summary>
        public void LoadConstantI4(int value)
        {
            ILOpCode code;
            switch (value)
            {
                case -1: code = ILOpCode.Ldc_i4_m1; break;
                case 0: code = ILOpCode.Ldc_i4_0; break;
                case 1: code = ILOpCode.Ldc_i4_1; break;
                case 2: code = ILOpCode.Ldc_i4_2; break;
                case 3: code = ILOpCode.Ldc_i4_3; break;
                case 4: code = ILOpCode.Ldc_i4_4; break;
                case 5: code = ILOpCode.Ldc_i4_5; break;
                case 6: code = ILOpCode.Ldc_i4_6; break;
                case 7: code = ILOpCode.Ldc_i4_7; break;
                case 8: code = ILOpCode.Ldc_i4_8; break;

                default:
                    if (unchecked((sbyte)value == value))
                    {
                        OpCode(ILOpCode.Ldc_i4_s);
                        CodeBuilder.WriteSByte((sbyte)value);
                    }
                    else
                    {
                        OpCode(ILOpCode.Ldc_i4);
                        CodeBuilder.WriteInt32(value);
                    }

                    return;
            }

            OpCode(code);
        }

        /// <summary>
        /// Encodes <see cref="long"/> constant load instruction.
        /// </summary>
        public void LoadConstantI8(long value)
        {
            OpCode(ILOpCode.Ldc_i8);
            CodeBuilder.WriteInt64(value);
        }

        /// <summary>
        /// Encodes <see cref="float"/> constant load instruction.
        /// </summary>
        public void LoadConstantR4(float value)
        {
            OpCode(ILOpCode.Ldc_r4);
            CodeBuilder.WriteSingle(value);
        }

        /// <summary>
        /// Encodes <see cref="double"/> constant load instruction.
        /// </summary>
        public void LoadConstantR8(double value)
        {
            OpCode(ILOpCode.Ldc_r8);
            CodeBuilder.WriteDouble(value);
        }

        /// <summary>
        /// Encodes local variable load instruction.
        /// </summary>
        /// <param name="slotIndex">Index of the local variable slot.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="slotIndex"/> is negative.</exception>
        public void LoadLocal(int slotIndex)
        {
            switch (slotIndex)
            {
                case 0: OpCode(ILOpCode.Ldloc_0); break;
                case 1: OpCode(ILOpCode.Ldloc_1); break;
                case 2: OpCode(ILOpCode.Ldloc_2); break;
                case 3: OpCode(ILOpCode.Ldloc_3); break;

                default:
                    if (unchecked((uint)slotIndex) <= byte.MaxValue)
                    {
                        OpCode(ILOpCode.Ldloc_s);
                        CodeBuilder.WriteByte((byte)slotIndex);
                    }
                    else if (slotIndex > 0)
                    {
                        OpCode(ILOpCode.Ldloc);
                        CodeBuilder.WriteInt32(slotIndex);
                    }
                    else
                    {
                        Throw.ArgumentOutOfRange(nameof(slotIndex));
                    }

                    break;
            }
        }

        /// <summary>
        /// Encodes local variable store instruction.
        /// </summary>
        /// <param name="slotIndex">Index of the local variable slot.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="slotIndex"/> is negative.</exception>
        public void StoreLocal(int slotIndex)
        {
            switch (slotIndex)
            {
                case 0: OpCode(ILOpCode.Stloc_0); break;
                case 1: OpCode(ILOpCode.Stloc_1); break;
                case 2: OpCode(ILOpCode.Stloc_2); break;
                case 3: OpCode(ILOpCode.Stloc_3); break;

                default:
                    if (unchecked((uint)slotIndex) <= byte.MaxValue)
                    {
                        OpCode(ILOpCode.Stloc_s);
                        CodeBuilder.WriteByte((byte)slotIndex);
                    }
                    else if (slotIndex > 0)
                    {
                        OpCode(ILOpCode.Stloc);
                        CodeBuilder.WriteInt32(slotIndex);
                    }
                    else
                    {
                        Throw.ArgumentOutOfRange(nameof(slotIndex));
                    }

                    break;
            }
        }

        /// <summary>
        /// Encodes local variable address load instruction.
        /// </summary>
        /// <param name="slotIndex">Index of the local variable slot.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="slotIndex"/> is negative.</exception>
        public void LoadLocalAddress(int slotIndex)
        {
            if (unchecked((uint)slotIndex) <= byte.MaxValue)
            {
                OpCode(ILOpCode.Ldloca_s);
                CodeBuilder.WriteByte((byte)slotIndex);
            }
            else if (slotIndex > 0)
            {
                OpCode(ILOpCode.Ldloca);
                CodeBuilder.WriteInt32(slotIndex);
            }
            else
            {
                Throw.ArgumentOutOfRange(nameof(slotIndex));
            }
        }

        /// <summary>
        /// Encodes argument load instruction.
        /// </summary>
        /// <param name="argumentIndex">Index of the argument.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="argumentIndex"/> is negative.</exception>
        public void LoadArgument(int argumentIndex)
        {
            switch (argumentIndex)
            {
                case 0: OpCode(ILOpCode.Ldarg_0); break;
                case 1: OpCode(ILOpCode.Ldarg_1); break;
                case 2: OpCode(ILOpCode.Ldarg_2); break;
                case 3: OpCode(ILOpCode.Ldarg_3); break;

                default:
                    if (unchecked((uint)argumentIndex) <= byte.MaxValue)
                    {
                        OpCode(ILOpCode.Ldarg_s);
                        CodeBuilder.WriteByte((byte)argumentIndex);
                    }
                    else if (argumentIndex > 0)
                    {
                        OpCode(ILOpCode.Ldarg);
                        CodeBuilder.WriteInt32(argumentIndex);
                    }
                    else
                    {
                        Throw.ArgumentOutOfRange(nameof(argumentIndex));
                    }

                    break;
            }
        }

        /// <summary>
        /// Encodes argument address load instruction.
        /// </summary>
        /// <param name="argumentIndex">Index of the argument.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="argumentIndex"/> is negative.</exception>
        public void LoadArgumentAddress(int argumentIndex)
        {
            if (unchecked((uint)argumentIndex) <= byte.MaxValue)
            {
                OpCode(ILOpCode.Ldarga_s);
                CodeBuilder.WriteByte((byte)argumentIndex);
            }
            else if (argumentIndex > 0)
            {
                OpCode(ILOpCode.Ldarga);
                CodeBuilder.WriteInt32(argumentIndex);
            }
            else
            {
                Throw.ArgumentOutOfRange(nameof(argumentIndex));
            }
        }

        /// <summary>
        /// Encodes argument store instruction.
        /// </summary>
        /// <param name="argumentIndex">Index of the argument.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="argumentIndex"/> is negative.</exception>
        public void StoreArgument(int argumentIndex)
        {
            if (unchecked((uint)argumentIndex) <= byte.MaxValue)
            {
                OpCode(ILOpCode.Starg_s);
                CodeBuilder.WriteByte((byte)argumentIndex);
            }
            else if (argumentIndex > 0)
            {
                OpCode(ILOpCode.Starg);
                CodeBuilder.WriteInt32(argumentIndex);
            }
            else
            {
                Throw.ArgumentOutOfRange(nameof(argumentIndex));
            }
        }

        /// <summary>
        /// Defines a label that can later be used to mark and refer to a location in the instruction stream.
        /// </summary>
        /// <returns>Label handle.</returns>
        /// <exception cref="InvalidOperationException"><see cref="ControlFlowBuilder"/> is null.</exception>
        public LabelHandle DefineLabel()
        {
            return GetBranchBuilder().AddLabel();
        }

        /// <summary>
        /// Encodes a branch instruction.
        /// </summary>
        /// <param name="code">Branch instruction to encode.</param>
        /// <param name="label">Label of the target location in instruction stream.</param>
        /// <exception cref="ArgumentException"><paramref name="code"/> is not a branch instruction.</exception>
        /// <exception cref="ArgumentException"><paramref name="label"/> was not defined by this encoder.</exception>
        /// <exception cref="InvalidOperationException"><see cref="ControlFlowBuilder"/> is null.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="label"/> has default value.</exception>
        public void Branch(ILOpCode code, LabelHandle label)
        {
            // throws if code is not a branch:
            int size = code.GetBranchOperandSize();

            GetBranchBuilder().AddBranch(Offset, label, code);
            OpCode(code);

            // -1 points in the middle of the branch instruction and is thus invalid.
            // We want to produce invalid IL so that if the caller doesn't patch the branches 
            // the branch instructions will be invalid in an obvious way.
            if (size == 1)
            {
                CodeBuilder.WriteSByte(-1);
            }
            else
            {
                Debug.Assert(size == 4);
                CodeBuilder.WriteInt32(-1);
            }
        }

        /// <summary>
        /// Associates specified label with the current IL offset.
        /// </summary>
        /// <param name="label">Label to mark.</param>
        /// <remarks>
        /// A single label may be marked multiple times, the last offset wins.
        /// </remarks>
        /// <exception cref="InvalidOperationException"><see cref="ControlFlowBuilder"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="label"/> was not defined by this encoder.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="label"/> has default value.</exception>
        public void MarkLabel(LabelHandle label)
        {
            GetBranchBuilder().MarkLabel(Offset, label);
        }

        private ControlFlowBuilder GetBranchBuilder()
        {
            if (ControlFlowBuilder == null)
            {
                Throw.ControlFlowBuilderNotAvailable();
            }

            return ControlFlowBuilder;
        }
    }
}
