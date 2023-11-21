﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;

namespace System.Reflection.Emit
{
    internal sealed class ILGeneratorImpl : ILGenerator
    {
        private const int DefaultSize = 16;
        private readonly MethodBuilder _methodBuilder;
        private readonly BlobBuilder _builder;
        private readonly InstructionEncoder _il;
        private readonly ControlFlowBuilder _cfBuilder;
        private bool _hasDynamicStackAllocation;
        private int _maxStackSize;
        private int _currentStack;
        private List<LocalBuilder> _locals = new();
        private Dictionary<Label, LabelHandle> _labelTable = new(2);
        private List<KeyValuePair<MemberInfo, BlobWriter>> _memberReferences = new();
        private List<ExceptionBlock> _exceptionStack = new();

        internal ILGeneratorImpl(MethodBuilder methodBuilder, int size)
        {
            _methodBuilder = methodBuilder;
            // For compat, runtime implementation doesn't throw for negative or zero value.
            _builder = new BlobBuilder(Math.Max(size, DefaultSize));
            _cfBuilder = new ControlFlowBuilder();
            _il = new InstructionEncoder(_builder, _cfBuilder);
        }

        internal int GetMaxStackSize() => _maxStackSize;
        internal List<KeyValuePair<MemberInfo, BlobWriter>> GetMemberReferences() => _memberReferences;
        internal InstructionEncoder Instructions => _il;
        internal bool HasDynamicStackAllocation => _hasDynamicStackAllocation;
        internal List<LocalBuilder> Locals => _locals;

        public override int ILOffset => _il.Offset;

        public override void BeginCatchBlock(Type? exceptionType)
        {
            if (_exceptionStack.Count < 1)
            {
                throw new NotSupportedException(SR.Argument_NotInExceptionBlock);
            }

            ExceptionBlock currentExBlock = _exceptionStack[_exceptionStack.Count - 1];
            if (currentExBlock.State == ExceptionState.Filter)
            {
                // Filter block  should be followed by catch block with null exception type
                if (exceptionType != null)
                {
                    throw new ArgumentException(SR.Argument_ShouldNotSpecifyExceptionType);
                }

                Emit(OpCodes.Endfilter);
                MarkLabel(currentExBlock.HandleStart);
            }
            else
            {
                ArgumentNullException.ThrowIfNull(exceptionType);

                Emit(OpCodes.Leave, currentExBlock.EndLabel);
                if (currentExBlock.State == ExceptionState.Try)
                {
                    MarkLabel(currentExBlock.TryEnd);
                }
                else if (currentExBlock.State == ExceptionState.Catch)
                {
                    MarkLabel(currentExBlock.HandleEnd);
                }

                currentExBlock.HandleStart = DefineLabel();
                currentExBlock.HandleEnd = DefineLabel();
                ModuleBuilderImpl module = (ModuleBuilderImpl)_methodBuilder.Module;
                _cfBuilder.AddCatchRegion(_labelTable[currentExBlock.TryStart], _labelTable[currentExBlock.TryEnd],
                    _labelTable[currentExBlock.HandleStart], _labelTable[currentExBlock.HandleEnd], module.GetTypeHandle(exceptionType));
                MarkLabel(currentExBlock.HandleStart);
            }

            currentExBlock.State = ExceptionState.Catch;
        }

        public override void BeginExceptFilterBlock()
        {
            if (_exceptionStack.Count < 1)
            {
                throw new NotSupportedException(SR.Argument_NotInExceptionBlock);
            }

            ExceptionBlock currentExBlock = _exceptionStack[_exceptionStack.Count - 1];
            Emit(OpCodes.Leave, currentExBlock.EndLabel);
            if (currentExBlock.State == ExceptionState.Try)
            {
                MarkLabel(currentExBlock.TryEnd);
            }
            else if (currentExBlock.State == ExceptionState.Catch)
            {
                MarkLabel(currentExBlock.HandleEnd);
            }

            currentExBlock.FilterStart = DefineLabel();
            currentExBlock.HandleStart = DefineLabel();
            currentExBlock.HandleEnd = DefineLabel();
            _cfBuilder.AddFilterRegion(_labelTable[currentExBlock.TryStart], _labelTable[currentExBlock.TryEnd],
                _labelTable[currentExBlock.HandleStart], _labelTable[currentExBlock.HandleEnd], _labelTable[currentExBlock.FilterStart]);
            currentExBlock.State = ExceptionState.Filter;
            MarkLabel(currentExBlock.FilterStart);
        }

        public override Label BeginExceptionBlock()
        {
            ExceptionBlock currentExBlock = new ExceptionBlock();
            currentExBlock.TryStart = DefineLabel();
            currentExBlock.TryEnd = DefineLabel();
            currentExBlock.EndLabel = DefineLabel(); // End label of the whole exception block
            MarkLabel(currentExBlock.TryStart);
            currentExBlock.State = ExceptionState.Try;
            _exceptionStack.Add(currentExBlock);
            return currentExBlock.EndLabel;
        }

        public override void BeginFaultBlock()
        {
            if (_exceptionStack.Count < 1)
            {
                throw new NotSupportedException(SR.Argument_NotInExceptionBlock);
            }

            ExceptionBlock currentExBlock = _exceptionStack[_exceptionStack.Count - 1];
            Emit(OpCodes.Leave, currentExBlock.EndLabel);
            if (currentExBlock.State == ExceptionState.Try)
            {
                MarkLabel(currentExBlock.TryEnd);
            }
            else if (currentExBlock.State == ExceptionState.Catch)
            {
                MarkLabel(currentExBlock.HandleEnd);
            }

            currentExBlock.HandleStart = DefineLabel();
            currentExBlock.HandleEnd = DefineLabel();
            _cfBuilder.AddFaultRegion(_labelTable[currentExBlock.TryStart], _labelTable[currentExBlock.TryEnd],
                               _labelTable[currentExBlock.HandleStart], _labelTable[currentExBlock.HandleEnd]);
            currentExBlock.State = ExceptionState.Fault;
            MarkLabel(currentExBlock.HandleStart);
        }

        public override void BeginFinallyBlock()
        {
            if (_exceptionStack.Count < 1)
            {
                throw new NotSupportedException(SR.Argument_NotInExceptionBlock);
            }

            ExceptionBlock currentExBlock = _exceptionStack[_exceptionStack.Count - 1];
            Label finallyEndLabel = DefineLabel();
            if (currentExBlock.State == ExceptionState.Try)
            {
                Emit(OpCodes.Leave, finallyEndLabel);
            }
            else if (currentExBlock.State == ExceptionState.Catch)
            {
                Emit(OpCodes.Leave, currentExBlock.EndLabel);
                MarkLabel(currentExBlock.HandleEnd);
                currentExBlock.TryEnd = DefineLabel(); // need to nest the catch block within finally
            }

            MarkLabel(currentExBlock.TryEnd);
            currentExBlock.HandleStart = DefineLabel();
            currentExBlock.HandleEnd = finallyEndLabel;
            _cfBuilder.AddFinallyRegion(_labelTable[currentExBlock.TryStart], _labelTable[currentExBlock.TryEnd],
                               _labelTable[currentExBlock.HandleStart], _labelTable[currentExBlock.HandleEnd]);
            currentExBlock.State = ExceptionState.Finally;
            MarkLabel(currentExBlock.HandleStart);
        }

        public override void BeginScope() => throw new NotImplementedException();

        public override LocalBuilder DeclareLocal(Type localType, bool pinned)
        {
            if (_methodBuilder is not MethodBuilderImpl methodBuilder)
                throw new NotSupportedException();

            ArgumentNullException.ThrowIfNull(localType);

            LocalBuilder local = new LocalBuilderImpl(_locals.Count, localType, methodBuilder, pinned);
            _locals.Add(local);

            return local;
        }

        public override Label DefineLabel()
        {
            LabelHandle metadataLabel = _il.DefineLabel();
            Label emitLabel = CreateLabel(metadataLabel.Id);
            _labelTable.Add(emitLabel, metadataLabel);
            return emitLabel;
        }
        private void UpdateStackSize(OpCode opCode)
        {
            _currentStack += opCode.EvaluationStackDelta;
            _maxStackSize = Math.Max(_maxStackSize, _currentStack);
        }

        private void UpdateStackSize(OpCode opCode, int stackChange)
        {
            _currentStack += opCode.EvaluationStackDelta;
            _currentStack += stackChange;
            _maxStackSize = Math.Max(_maxStackSize, _currentStack);
        }

        public void EmitOpcode(OpCode opcode)
        {
            if (opcode == OpCodes.Localloc)
            {
                _hasDynamicStackAllocation = true;
            }

            _il.OpCode((ILOpCode)opcode.Value);
            UpdateStackSize(opcode);
        }

        public override void Emit(OpCode opcode) => EmitOpcode(opcode);

        public override void Emit(OpCode opcode, byte arg)
        {
            EmitOpcode(opcode);
            _builder.WriteByte(arg);
        }

        public override void Emit(OpCode opcode, double arg)
        {
            EmitOpcode(opcode);
            _builder.WriteDouble(arg);
        }

        public override void Emit(OpCode opcode, float arg)
        {
            EmitOpcode(opcode);
            _builder.WriteSingle(arg);
        }

        public override void Emit(OpCode opcode, short arg)
        {
            EmitOpcode(opcode);
            _builder.WriteInt16(arg);
        }

        public override void Emit(OpCode opcode, int arg)
        {
            // Special-case several opcodes that have shorter variants for common values.
            if (opcode.Equals(OpCodes.Ldc_I4))
            {
                if (arg >= -1 && arg <= 8)
                {
                    EmitOpcode(arg switch
                    {
                        -1 => OpCodes.Ldc_I4_M1,
                        0 => OpCodes.Ldc_I4_0,
                        1 => OpCodes.Ldc_I4_1,
                        2 => OpCodes.Ldc_I4_2,
                        3 => OpCodes.Ldc_I4_3,
                        4 => OpCodes.Ldc_I4_4,
                        5 => OpCodes.Ldc_I4_5,
                        6 => OpCodes.Ldc_I4_6,
                        7 => OpCodes.Ldc_I4_7,
                        _ => OpCodes.Ldc_I4_8
                    });
                    return;
                }

                if (arg >= -128 && arg <= 127)
                {
                    Emit(OpCodes.Ldc_I4_S, (sbyte)arg);
                    return;
                }
            }
            else if (opcode.Equals(OpCodes.Ldarg))
            {
                if ((uint)arg <= 3)
                {
                    EmitOpcode(arg switch
                    {
                        0 => OpCodes.Ldarg_0,
                        1 => OpCodes.Ldarg_1,
                        2 => OpCodes.Ldarg_2,
                        _ => OpCodes.Ldarg_3,
                    });
                    return;
                }

                if ((uint)arg <= byte.MaxValue)
                {
                    Emit(OpCodes.Ldarg_S, (byte)arg);
                    return;
                }

                if ((uint)arg <= ushort.MaxValue) // this will be true except on misuse of the opcode
                {
                    Emit(OpCodes.Ldarg, (short)arg);
                    return;
                }
            }
            else if (opcode.Equals(OpCodes.Ldarga))
            {
                if ((uint)arg <= byte.MaxValue)
                {
                    Emit(OpCodes.Ldarga_S, (byte)arg);
                    return;
                }

                if ((uint)arg <= ushort.MaxValue) // this will be true except on misuse of the opcode
                {
                    Emit(OpCodes.Ldarga, (short)arg);
                    return;
                }
            }
            else if (opcode.Equals(OpCodes.Starg))
            {
                if ((uint)arg <= byte.MaxValue)
                {
                    Emit(OpCodes.Starg_S, (byte)arg);
                    return;
                }

                if ((uint)arg <= ushort.MaxValue) // this will be true except on misuse of the opcode
                {
                    Emit(OpCodes.Starg, (short)arg);
                    return;
                }
            }

            // For everything else, put the opcode followed by the arg onto the stream of instructions.
            EmitOpcode(opcode);
            _builder.WriteInt32(arg);
        }

        public override void Emit(OpCode opcode, long arg)
        {
            EmitOpcode(opcode);
            _il.CodeBuilder.WriteInt64(arg);
        }

        public override void Emit(OpCode opcode, string str)
        {
            // Puts the opcode onto the IL stream followed by the metadata token
            // represented by str.
            ModuleBuilder modBuilder = (ModuleBuilder)_methodBuilder.Module;
            int tempVal = modBuilder.GetStringMetadataToken(str);
            EmitOpcode(opcode);
            _il.Token(tempVal);
        }

        public override void Emit(OpCode opcode, ConstructorInfo con)
        {
            ArgumentNullException.ThrowIfNull(con);

            if (!(opcode.Equals(OpCodes.Call) || opcode.Equals(OpCodes.Callvirt) || opcode.Equals(OpCodes.Newobj)))
            {
                throw new ArgumentException(SR.Argument_NotMethodCallOpcode, nameof(opcode));
            }

            int stackChange = 0;
            // Push the return value
            stackChange++;
            // Pop the parameters.
            if (con is ConstructorBuilderImpl builder)
            {
                stackChange -= builder._methodBuilder.ParameterCount;
            }
            else
            {
                stackChange -= con.GetParameters().Length;
            }
            // Pop the this parameter if the constructor is non-static and the
            // instruction is not newobj.
            if (!con.IsStatic && !opcode.Equals(OpCodes.Newobj))
            {
                stackChange--;
            }

            UpdateStackSize(opcode, stackChange);
            _il.OpCode((ILOpCode)opcode.Value);
            _memberReferences.Add(new KeyValuePair<MemberInfo, BlobWriter>
                (con, new BlobWriter(_il.CodeBuilder.ReserveBytes(sizeof(int)))));
        }

        public override void Emit(OpCode opcode, Label label)
        {
            if (_labelTable.TryGetValue(label, out LabelHandle labelHandle))
            {
                _il.Branch((ILOpCode)opcode.Value, labelHandle);
                UpdateStackSize(opcode);
            }
            else
            {
                throw new ArgumentException(SR.Argument_InvalidLabel);
            }
        }

        public override void Emit(OpCode opcode, Label[] labels)
        {
            ArgumentNullException.ThrowIfNull(labels);

            if (!opcode.Equals(OpCodes.Switch))
            {
                throw new ArgumentException(SR.Argument_MustBeSwitchOpCode, nameof(opcode));
            }

            SwitchInstructionEncoder switchEncoder = _il.Switch(labels.Length);
            UpdateStackSize(opcode);

            foreach (Label label in labels)
            {
                switchEncoder.Branch(_labelTable[label]);
            }
        }

        public override void Emit(OpCode opcode, LocalBuilder local)
        {
            ArgumentNullException.ThrowIfNull(local);

            if (local is not LocalBuilderImpl localBuilder || localBuilder.GetMethodBuilder() != _methodBuilder)
            {
                throw new ArgumentException(SR.Argument_UnmatchedMethodForLocal, nameof(local));
            }

            int tempVal = local.LocalIndex;
            string name = opcode.Name!;

            if (name.StartsWith("ldloca"))
            {
                _il.LoadLocalAddress(tempVal);
            }
            else if (name.StartsWith("ldloc"))
            {
                _il.LoadLocal(tempVal);
            }
            else if (name.StartsWith("stloc"))
            {
                _il.StoreLocal(tempVal);
            }

            UpdateStackSize(opcode);
        }

        public override void Emit(OpCode opcode, SignatureHelper signature) => throw new NotImplementedException();

        public override void Emit(OpCode opcode, FieldInfo field)
        {
            ArgumentNullException.ThrowIfNull(field);

            EmitMember(opcode, field);
        }

        public override void Emit(OpCode opcode, MethodInfo meth)
        {
            ArgumentNullException.ThrowIfNull(meth);

            if (opcode.Equals(OpCodes.Call) || opcode.Equals(OpCodes.Callvirt) || opcode.Equals(OpCodes.Newobj))
            {
                EmitCall(opcode, meth, null);
            }
            else
            {
                EmitMember(opcode, meth);
            }
        }

        private void EmitMember(OpCode opcode, MemberInfo member)
        {
            EmitOpcode(opcode);
            _memberReferences.Add(new KeyValuePair<MemberInfo, BlobWriter>
                (member, new BlobWriter(_il.CodeBuilder.ReserveBytes(sizeof(int)))));
        }

        public override void Emit(OpCode opcode, Type cls)
        {
            ArgumentNullException.ThrowIfNull(cls);

            EmitOpcode(opcode);
            ModuleBuilder module = (ModuleBuilder)_methodBuilder.Module;
            _il.Token(module.GetTypeMetadataToken(cls));
        }

        public override void EmitCall(OpCode opcode, MethodInfo methodInfo, Type[]? optionalParameterTypes)
        {
            ArgumentNullException.ThrowIfNull(methodInfo);

            if (!(opcode.Equals(OpCodes.Call) || opcode.Equals(OpCodes.Callvirt) || opcode.Equals(OpCodes.Newobj)))
            {
                throw new ArgumentException(SR.Argument_NotMethodCallOpcode, nameof(opcode));
            }

            _il.OpCode((ILOpCode)opcode.Value);
            UpdateStackSize(opcode, GetStackChange(opcode, methodInfo, optionalParameterTypes));
            _memberReferences.Add(new KeyValuePair<MemberInfo, BlobWriter>
                (methodInfo, new BlobWriter(_il.CodeBuilder.ReserveBytes(sizeof(int)))));
        }

        private static int GetStackChange(OpCode opcode, MethodInfo methodInfo, Type[]? optionalParameterTypes)
        {
            int stackChange = 0;

            // Push the return value if there is one.
            if (methodInfo.ReturnType != typeof(void))
            {
                stackChange++;
            }

            // Pop the parameters.
            if (methodInfo is MethodBuilderImpl builder)
            {
                stackChange -= builder.ParameterCount;
            }
            else
            {
                stackChange -= methodInfo.GetParameters().Length;
            }

            // Pop the this parameter if the method is non-static and the
            // instruction is not newobj.
            if (!methodInfo.IsStatic && !opcode.Equals(OpCodes.Newobj))
            {
                stackChange--;
            }

            // Pop the optional parameters off the stack.
            if (optionalParameterTypes != null)
            {
                stackChange -= optionalParameterTypes.Length;
            }

            return stackChange;
        }

        public override void EmitCalli(OpCode opcode, CallingConventions callingConvention, Type? returnType, Type[]? parameterTypes, Type[]? optionalParameterTypes) => throw new NotImplementedException();
        public override void EmitCalli(OpCode opcode, CallingConvention unmanagedCallConv, Type? returnType, Type[]? parameterTypes) => throw new NotImplementedException();

        public override void EndExceptionBlock()
        {
            if (_exceptionStack.Count < 1)
            {
                throw new NotSupportedException(SR.Argument_NotInExceptionBlock);
            }

            ExceptionBlock currentExBlock = _exceptionStack[_exceptionStack.Count - 1];
            ExceptionState state = currentExBlock.State;
            Label endLabel = currentExBlock.EndLabel;

            if (state == ExceptionState.Filter || state == ExceptionState.Try)
            {
                throw new InvalidOperationException(SR.Argument_BadExceptionCodeGen);
            }

            if (state == ExceptionState.Catch)
            {
                Emit(OpCodes.Leave, endLabel);
                MarkLabel(currentExBlock.HandleEnd);
            }
            else if (state == ExceptionState.Finally || state == ExceptionState.Fault)
            {
                Emit(OpCodes.Endfinally);
                MarkLabel(currentExBlock.HandleEnd);
            }

            MarkLabel(endLabel);
            currentExBlock.State = ExceptionState.Done;
            _exceptionStack.Remove(currentExBlock);
        }

        public override void EndScope() => throw new NotImplementedException();

        public override void MarkLabel(Label loc)
        {
            if (_labelTable.TryGetValue(loc, out LabelHandle labelHandle))
            {
                _il.MarkLabel(labelHandle);
            }
            else
            {
                throw new ArgumentException(SR.Argument_InvalidLabel);
            }
        }

        public override void UsingNamespace(string usingNamespace) => throw new NotImplementedException();
    }

    internal sealed class ExceptionBlock
    {
        public Label TryStart;
        public Label TryEnd;
        public Label HandleStart;
        public Label HandleEnd;
        public Label FilterStart;
        public Label EndLabel;
        public ExceptionState State;
    }

    internal enum ExceptionState
    {
        Undefined,
        Try,
        Filter,
        Catch,
        Finally,
        Fault,
        Done
    }
}
