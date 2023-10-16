// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Reflection.Emit
{
    internal sealed class ILGeneratorImpl : ILGenerator
    {
        private const int DefaultSize = 16;
        private readonly MethodBuilder _methodBuilder;
        private readonly BlobBuilder _builder;
        private readonly InstructionEncoder _il;
        private bool _hasDynamicStackAllocation;
        private int _maxStackSize;

        internal ILGeneratorImpl(MethodBuilder methodBuilder, int size)
        {
            _methodBuilder = methodBuilder;
            // For compat, runtime implementation doesn't throw for negative or zero value.
            _builder = new BlobBuilder(Math.Max(size, DefaultSize));
            _il = new InstructionEncoder(_builder, new ControlFlowBuilder());
        }

        internal int GetMaxStackSize() => _maxStackSize;

        internal InstructionEncoder Instructions => _il;
        internal bool HasDynamicStackAllocation => _hasDynamicStackAllocation;

        public override int ILOffset => _il.Offset;

        public override void BeginCatchBlock(Type? exceptionType) => throw new NotImplementedException();
        public override void BeginExceptFilterBlock() => throw new NotImplementedException();
        public override Label BeginExceptionBlock() => throw new NotImplementedException();
        public override void BeginFaultBlock() => throw new NotImplementedException();
        public override void BeginFinallyBlock() => throw new NotImplementedException();
        public override void BeginScope() => throw new NotImplementedException();
        public override LocalBuilder DeclareLocal(Type localType, bool pinned) => throw new NotImplementedException();
        public override Label DefineLabel() => throw new NotImplementedException();

        public override void Emit(OpCode opcode)
        {
            if (opcode == OpCodes.Localloc)
            {
                _hasDynamicStackAllocation = true;
            }
            _il.OpCode((ILOpCode)opcode.Value);

            // TODO: for now only count the Opcodes emitted, in order to calculate it correctly we might need to make internal Opcode APIs public
            // https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/Reflection/Emit/Opcode.cs#L48
            _maxStackSize++;
        }

        public override void Emit(OpCode opcode, byte arg)
        {
            _il.OpCode((ILOpCode)opcode.Value);
            _builder.WriteByte(arg);
        }

        public override void Emit(OpCode opcode, double arg)
        {
            _il.OpCode((ILOpCode)opcode.Value);
            _builder.WriteDouble(arg);
        }

        public override void Emit(OpCode opcode, float arg)
        {
            _il.OpCode((ILOpCode)opcode.Value);
            _builder.WriteSingle(arg);
        }

        public override void Emit(OpCode opcode, short arg)
        {
            _il.OpCode((ILOpCode)opcode.Value);
            _builder.WriteInt16(arg);
        }

        public override void Emit(OpCode opcode, int arg)
        {
            // Special-case several opcodes that have shorter variants for common values.
            if (opcode.Equals(OpCodes.Ldc_I4))
            {
                if (arg >= -1 && arg <= 8)
                {
                    _il.OpCode(arg switch
                    {
                        -1 => ILOpCode.Ldc_i4_m1,
                        0 => ILOpCode.Ldc_i4_0,
                        1 => ILOpCode.Ldc_i4_1,
                        2 => ILOpCode.Ldc_i4_2,
                        3 => ILOpCode.Ldc_i4_3,
                        4 => ILOpCode.Ldc_i4_4,
                        5 => ILOpCode.Ldc_i4_5,
                        6 => ILOpCode.Ldc_i4_6,
                        7 => ILOpCode.Ldc_i4_7,
                        _ => ILOpCode.Ldc_i4_8,
                    });
                    return;
                }

                if (arg >= -128 && arg <= 127)
                {
                    _il.OpCode(ILOpCode.Ldc_i4_s);
                    _builder.WriteSByte((sbyte)arg) ;
                    return;
                }
            }
            else if (opcode.Equals(OpCodes.Ldarg))
            {
                if ((uint)arg <= 3)
                {
                    _il.OpCode(arg switch
                    {
                        0 => ILOpCode.Ldarg_0,
                        1 => ILOpCode.Ldarg_1,
                        2 => ILOpCode.Ldarg_2,
                        _ => ILOpCode.Ldarg_3,
                    });
                    return;
                }

                if ((uint)arg <= byte.MaxValue)
                {
                    _il.OpCode(ILOpCode.Ldarg_s);
                    _builder.WriteByte((byte)arg);
                    return;
                }

                if ((uint)arg <= ushort.MaxValue) // this will be true except on misuse of the opcode
                {
                    _il.OpCode(ILOpCode.Ldarg);
                    _builder.WriteInt16((short)arg);
                    return;
                }
            }
            else if (opcode.Equals(OpCodes.Ldarga))
            {
                if ((uint)arg <= byte.MaxValue)
                {
                    _il.OpCode(ILOpCode.Ldarga_s);
                    _builder.WriteByte((byte)arg);
                    return;
                }

                if ((uint)arg <= ushort.MaxValue) // this will be true except on misuse of the opcode
                {
                    _il.OpCode(ILOpCode.Ldarga);
                    _builder.WriteInt16((short)arg);
                    return;
                }
            }
            else if (opcode.Equals(OpCodes.Starg))
            {
                if ((uint)arg <= byte.MaxValue)
                {
                    _il.OpCode(ILOpCode.Starg_s);
                    _builder.WriteByte((byte)arg);
                    return;
                }

                if ((uint)arg <= ushort.MaxValue) // this will be true except on misuse of the opcode
                {
                    _il.OpCode(ILOpCode.Starg);
                    _builder.WriteInt16((short)arg);
                    return;
                }
            }

            // For everything else, put the opcode followed by the arg onto the stream of instructions.
            _il.OpCode((ILOpCode)opcode.Value);
            _builder.WriteInt32(arg);
        }

        public override void Emit(OpCode opcode, long arg)
        {
            _il.OpCode((ILOpCode)opcode.Value);
            _il.CodeBuilder.WriteInt64(arg);
        }

        public override void Emit(OpCode opcode, string str)
        {
            // Puts the opcode onto the IL stream followed by the metadata token
            // represented by str.
            ModuleBuilder modBuilder = (ModuleBuilder)_methodBuilder.Module;
            int tempVal = modBuilder.GetStringMetadataToken(str);
            _il.OpCode((ILOpCode)opcode.Value);
            _il.Token(tempVal);
        }

        public override void Emit(OpCode opcode, ConstructorInfo con) => throw new NotImplementedException();
        public override void Emit(OpCode opcode, Label label) => throw new NotImplementedException();
        public override void Emit(OpCode opcode, Label[] labels) => throw new NotImplementedException();
        public override void Emit(OpCode opcode, LocalBuilder local) => throw new NotImplementedException();
        public override void Emit(OpCode opcode, SignatureHelper signature) => throw new NotImplementedException();
        public override void Emit(OpCode opcode, FieldInfo field) => throw new NotImplementedException();
        public override void Emit(OpCode opcode, MethodInfo meth) => throw new NotImplementedException();
        public override void Emit(OpCode opcode, Type cls) => throw new NotImplementedException();
        public override void EmitCall(OpCode opcode, MethodInfo methodInfo, Type[]? optionalParameterTypes) => throw new NotImplementedException();
        public override void EmitCalli(OpCode opcode, CallingConventions callingConvention, Type? returnType, Type[]? parameterTypes, Type[]? optionalParameterTypes) => throw new NotImplementedException();
        public override void EmitCalli(OpCode opcode, CallingConvention unmanagedCallConv, Type? returnType, Type[]? parameterTypes) => throw new NotImplementedException();
        public override void EndExceptionBlock() => throw new NotImplementedException();
        public override void EndScope() => throw new NotImplementedException();
        public override void MarkLabel(Label loc) => throw new NotImplementedException();
        public override void UsingNamespace(string usingNamespace) => throw new NotImplementedException();
    }
}
