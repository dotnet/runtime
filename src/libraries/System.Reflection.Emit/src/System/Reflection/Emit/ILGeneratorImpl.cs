// Licensed to the .NET Foundation under one or more agreements.
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
        private readonly MethodBuilderImpl _methodBuilder;
        private readonly ModuleBuilderImpl _moduleBuilder;
        private readonly BlobBuilder _builder;
        private readonly InstructionEncoder _il;
        private readonly ControlFlowBuilder _cfBuilder;
        private bool _hasDynamicStackAllocation;
        private int _maxStackDepth;
        private int _currentStackDepth; // Current stack labelStartDepth
        private int _targetDepth;  // Stack labelStartDepth at a target of the previous instruction (when it is branching)
        // Adjustment to add to _maxStackDepth for incorrect/invalid IL. For example, when branch
        // instructions branches backward with non zero stack depths targeting the same label.
        private int _depthAdjustment;
        private List<LocalBuilder> _locals = new();
        private Dictionary<Label, LabelInfo> _labelTable = new(2);
        private List<KeyValuePair<object, BlobWriter>> _memberReferences = new();
        private List<ExceptionBlock> _exceptionStack = new();

        internal ILGeneratorImpl(MethodBuilderImpl methodBuilder, int size)
        {
            _methodBuilder = methodBuilder;
            _moduleBuilder = (ModuleBuilderImpl)methodBuilder.Module;
            // For compat, runtime implementation doesn't throw for negative or zero value.
            _builder = new BlobBuilder(Math.Max(size, DefaultSize));
            _cfBuilder = new ControlFlowBuilder();
            _il = new InstructionEncoder(_builder, _cfBuilder);
        }

        internal int GetMaxStack() => Math.Min(ushort.MaxValue, _maxStackDepth + _depthAdjustment);
        internal List<KeyValuePair<object, BlobWriter>> GetMemberReferences() => _memberReferences;
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
                _cfBuilder.AddCatchRegion(GetMetaLabel(currentExBlock.TryStart), GetMetaLabel(currentExBlock.TryEnd),
                    GetMetaLabel(currentExBlock.HandleStart), GetMetaLabel(currentExBlock.HandleEnd), _moduleBuilder.GetTypeHandle(exceptionType));
                MarkLabel(currentExBlock.HandleStart);
            }

            // Stack depth for "catch" starts at one.
            _currentStackDepth = 1;
            currentExBlock.State = ExceptionState.Catch;
        }

        private LabelHandle GetMetaLabel(Label label) => _labelTable[label]._metaLabel;

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
            _cfBuilder.AddFilterRegion(GetMetaLabel(currentExBlock.TryStart), GetMetaLabel(currentExBlock.TryEnd),
                GetMetaLabel(currentExBlock.HandleStart), GetMetaLabel(currentExBlock.HandleEnd), GetMetaLabel(currentExBlock.FilterStart));
            currentExBlock.State = ExceptionState.Filter;
            MarkLabel(currentExBlock.FilterStart);
            // Stack depth for "filter" starts at one.
            _currentStackDepth = 1;
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
            // Stack depth for "try" starts at zero.
            _currentStackDepth = 0;
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
            _cfBuilder.AddFaultRegion(GetMetaLabel(currentExBlock.TryStart), GetMetaLabel(currentExBlock.TryEnd),
                GetMetaLabel(currentExBlock.HandleStart), GetMetaLabel(currentExBlock.HandleEnd));
            currentExBlock.State = ExceptionState.Fault;
            MarkLabel(currentExBlock.HandleStart);
            // Stack depth for "fault" starts at zero.
            _currentStackDepth = 0;
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
            _cfBuilder.AddFinallyRegion(GetMetaLabel(currentExBlock.TryStart), GetMetaLabel(currentExBlock.TryEnd),
                GetMetaLabel(currentExBlock.HandleStart), GetMetaLabel(currentExBlock.HandleEnd));
            currentExBlock.State = ExceptionState.Finally;
            MarkLabel(currentExBlock.HandleStart);
            // Stack depth for "finally" starts at zero.
            _currentStackDepth = 0;
        }

        public override void BeginScope()
        {
            // TODO: No-op, will be implemented wit PDB support
        }

        public override LocalBuilder DeclareLocal(Type localType, bool pinned)
        {
            ArgumentNullException.ThrowIfNull(localType);

            LocalBuilder local = new LocalBuilderImpl(_locals.Count, localType, _methodBuilder, pinned);
            _locals.Add(local);

            return local;
        }

        public override Label DefineLabel()
        {
            LabelHandle metadataLabel = _il.DefineLabel();
            Label emitLabel = CreateLabel(metadataLabel.Id);
            _labelTable.Add(emitLabel, new LabelInfo(metadataLabel));
            return emitLabel;
        }

        private void UpdateStackSize(OpCode opCode)
        {
            UpdateStackSize(opCode.EvaluationStackDelta);

            if (UnconditionalJump(opCode))
            {
                _currentStackDepth = 0;
            }
        }

        private static bool UnconditionalJump(OpCode opCode) =>
            opCode.FlowControl == FlowControl.Throw || opCode.FlowControl == FlowControl.Return || opCode == OpCodes.Jmp;

        private void UpdateStackSize(int stackChange)
        {
            _currentStackDepth += stackChange;
            _maxStackDepth = Math.Max(_maxStackDepth, _currentStackDepth);
            // Record the "target" stack depth at this instruction.
            _targetDepth = _currentStackDepth;
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
            // Puts the opcode onto the IL stream followed by the metadata token represented by str.
            EmitOpcode(opcode);
            _il.Token(_moduleBuilder.GetStringMetadataToken(str));
        }

        public override void Emit(OpCode opcode, ConstructorInfo con)
        {
            ArgumentNullException.ThrowIfNull(con);

            if (!(opcode.Equals(OpCodes.Call) || opcode.Equals(OpCodes.Callvirt) || opcode.Equals(OpCodes.Newobj)))
            {
                throw new ArgumentException(SR.Argument_NotMethodCallOpcode, nameof(opcode));
            }

            int stackChange = 0;
            if (opcode.StackBehaviourPush == StackBehaviour.Varpush)
            {
                // Instruction must be one of call or callvirt.
                Debug.Assert(opcode.Equals(OpCodes.Call) ||
                             opcode.Equals(OpCodes.Callvirt),
                             "Unexpected opcode encountered for StackBehaviour of VarPush.");
                stackChange++;
            }

            if (opcode.StackBehaviourPop == StackBehaviour.Varpop)
            {
                // Instruction must be one of call, callvirt or newobj.
                Debug.Assert(opcode.Equals(OpCodes.Call) ||
                             opcode.Equals(OpCodes.Callvirt) ||
                             opcode.Equals(OpCodes.Newobj),
                             "Unexpected opcode encountered for StackBehaviour of VarPop.");

                if (con is ConstructorBuilderImpl builder)
                {
                    stackChange -= builder._methodBuilder.ParameterCount;
                }
                else
                {
                    stackChange -= con.GetParameters().Length;
                }
            }

            EmitOpcode(opcode);
            UpdateStackSize(stackChange);
            WriteOrReserveToken(_moduleBuilder.TryGetConstructorHandle(con), con);
        }

        private void WriteOrReserveToken(EntityHandle handle, object member)
        {
            if (handle.IsNil)
            {
                // The member is a `***BuilderImpl` and its token is not yet defined.
                // Reserve the token bytes and write them later when its ready
                _memberReferences.Add(new KeyValuePair<object, BlobWriter>
                    (member, new BlobWriter(_il.CodeBuilder.ReserveBytes(sizeof(int)))));
            }
            else
            {
                _il.Token(MetadataTokens.GetToken(handle));
            }
        }

        private void AdjustDepth(OpCode opcode, LabelInfo label)
        {
            int labelStartDepth = label._startDepth;
            int targetDepth = _targetDepth;
            Debug.Assert(labelStartDepth >= -1);
            Debug.Assert(targetDepth >= -1);
            if (labelStartDepth < targetDepth)
            {
                // Either unknown depth for this label or this branch location has a larger depth than previously recorded.
                // In the latter case, the IL is (likely) invalid, but we just compensate for it using _depthAdjustment.
                if (labelStartDepth >= 0)
                {
                    _depthAdjustment += targetDepth - labelStartDepth;
                }

                // Keep the target depth, it will used as starting stack size from the marked location.
                label._startDepth = targetDepth;
            }

            // If it is unconditionally branching to a new location, for the next instruction invocation stack should be empty.
            // if this location is marked with a label, the starting stack size will be adjusted with label._startDepth.
            if (UnconditionalBranching(opcode))
            {
                _currentStackDepth = 0;
            }
        }

        private static bool UnconditionalBranching(OpCode opcode) =>
            opcode.FlowControl == FlowControl.Branch;

        public override void Emit(OpCode opcode, Label label)
        {
            if (_labelTable.TryGetValue(label, out LabelInfo? labelInfo))
            {
                _il.Branch((ILOpCode)opcode.Value, labelInfo._metaLabel);
                UpdateStackSize(opcode);
                AdjustDepth(opcode, labelInfo);
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
                LabelInfo labelInfo = _labelTable[label];
                switchEncoder.Branch(labelInfo._metaLabel);
                AdjustDepth(opcode, labelInfo);
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

        public override void Emit(OpCode opcode, SignatureHelper signature)
        {
            ArgumentNullException.ThrowIfNull(signature);

            EmitOpcode(opcode);
            // The only IL instruction that has VarPop behaviour, that takes a Signature
            // token as a parameter is Calli. Pop the parameters and the native function pointer.
            if (opcode.StackBehaviourPop == StackBehaviour.Varpop)
            {
                Debug.Assert(opcode.Equals(OpCodes.Calli), "Unexpected opcode encountered for StackBehaviour VarPop.");
                // Pop the arguments. Used reflection since ArgumentCount property is not public.
                PropertyInfo argCountProperty = typeof(SignatureHelper).GetProperty("ArgumentCount", BindingFlags.NonPublic | BindingFlags.Instance)!;
                int stackChange = -(int)argCountProperty.GetValue(signature)!;
                // Pop native function pointer off the stack.
                stackChange--;
                UpdateStackSize(stackChange);
            }
            _il.Token(_moduleBuilder.GetSignatureMetadataToken(signature));
        }

        public override void Emit(OpCode opcode, FieldInfo field)
        {
            ArgumentNullException.ThrowIfNull(field);

            EmitOpcode(opcode);
            WriteOrReserveToken(_moduleBuilder.TryGetFieldHandle(field), field);
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
                EmitOpcode(opcode);
                WriteOrReserveToken(_moduleBuilder.TryGetMethodHandle(meth), meth);
            }
        }

        public override void Emit(OpCode opcode, Type cls)
        {
            ArgumentNullException.ThrowIfNull(cls);

            EmitOpcode(opcode);
            WriteOrReserveToken(_moduleBuilder.TryGetTypeHandle(cls), cls);
        }

        public override void EmitCall(OpCode opcode, MethodInfo methodInfo, Type[]? optionalParameterTypes)
        {
            ArgumentNullException.ThrowIfNull(methodInfo);

            if (!(opcode.Equals(OpCodes.Call) || opcode.Equals(OpCodes.Callvirt) || opcode.Equals(OpCodes.Newobj)))
            {
                throw new ArgumentException(SR.Argument_NotMethodCallOpcode, nameof(opcode));
            }

            EmitOpcode(opcode);
            UpdateStackSize(GetStackChange(opcode, methodInfo, optionalParameterTypes));
            if (optionalParameterTypes == null || optionalParameterTypes.Length == 0)
            {
                WriteOrReserveToken(_moduleBuilder.TryGetMethodHandle(methodInfo), methodInfo);
            }
            else
            {
                WriteOrReserveToken(_moduleBuilder.TryGetMethodHandle(methodInfo, optionalParameterTypes),
                    new KeyValuePair<MethodInfo, Type[]>(methodInfo, optionalParameterTypes));
            }
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
            else if (methodInfo is ArrayMethod sm)
            {
                stackChange -= sm.ParameterTypes.Length;
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

        public override void EmitCalli(OpCode opcode, CallingConventions callingConvention,
            Type? returnType, Type[]? parameterTypes, Type[]? optionalParameterTypes)
        {
            if (optionalParameterTypes != null && optionalParameterTypes.Length > 0)
            {
                if ((callingConvention & CallingConventions.VarArgs) == 0)
                {
                    // Client should not supply optional parameter in default calling convention
                    throw new InvalidOperationException(SR.InvalidOperation_NotAVarArgCallingConvention);
                }
            }

            int stackChange = GetStackChange(returnType, parameterTypes);

            // Pop off VarArg arguments.
            if (optionalParameterTypes != null)
            {
                stackChange -= optionalParameterTypes.Length;
            }
            // Pop the this parameter if the method has a this parameter.
            if ((callingConvention & CallingConventions.HasThis) == CallingConventions.HasThis)
            {
                stackChange--;
            }

            UpdateStackSize(stackChange);
            EmitOpcode(OpCodes.Calli);
            _il.Token(_moduleBuilder.GetSignatureToken(callingConvention, returnType, parameterTypes, optionalParameterTypes));
        }

        public override void EmitCalli(OpCode opcode, CallingConvention unmanagedCallConv, Type? returnType, Type[]? parameterTypes)
        {
            int stackChange = GetStackChange(returnType, parameterTypes);
            UpdateStackSize(stackChange);
            Emit(OpCodes.Calli);
            _il.Token(_moduleBuilder.GetSignatureToken(unmanagedCallConv, returnType, parameterTypes));
        }

        private static int GetStackChange(Type? returnType, Type[]? parameterTypes)
        {
            int stackChange = 0;
            // If there is a non-void return type, push one.
            if (returnType != typeof(void))
            {
                stackChange++;
            }
            // Pop off arguments if any.
            if (parameterTypes != null)
            {
                stackChange -= parameterTypes.Length;
            }
            // Pop the native function pointer.
            stackChange--;
            return stackChange;
        }

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

        public override void EndScope()
        {
            // TODO: No-op, will be implemented wit PDB support
        }

        public override void MarkLabel(Label loc)
        {
            if (_labelTable.TryGetValue(loc, out LabelInfo? labelInfo))
            {
                if (labelInfo._position != -1)
                {
                    throw new ArgumentException(SR.Argument_RedefinedLabel);
                }

                _il.MarkLabel(labelInfo._metaLabel);
                labelInfo._position = _il.Offset;
                int depth = labelInfo._startDepth;
                if (depth < 0)
                {
                    // Unknown start depth for this label, indicating that it hasn't been used yet.
                    // Or we're in the Backward branch constraint case mentioned in ECMA-335 III.1.7.5.
                    // But the constraint is not enforced by any mainstream .NET runtime and they are not
                    // respected by .NET compilers. The _depthAdjustment field will compensate for violations
                    // of this constraint, as we discover them, check AdjustDepth method for detail. Here
                    // we assume a depth of zero. If a (later) branch to this label has a positive stack
                    // depth, we'll record that as the new depth and add the delta into _depthAdjustment.
                    labelInfo._startDepth = _currentStackDepth;
                }
                else if (depth < _currentStackDepth)
                {
                    // A branch location with smaller stack targets this label. In this case, the IL is invalid
                    // but we just compensate for it.
                    _depthAdjustment += _currentStackDepth - depth;
                    labelInfo._startDepth = _currentStackDepth;
                }
                else if (depth > _currentStackDepth)
                {
                    // A branch location with larger stack depth targets this label, can be invalid IL.
                    // Either case adjust the current stack depth.
                    _currentStackDepth = depth;
                }
            }
            else
            {
                throw new ArgumentException(SR.Argument_InvalidLabel);
            }
        }

        public override void UsingNamespace(string usingNamespace)
        {
            // TODO: No-op, will be implemented wit PDB support
        }
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

    internal sealed class LabelInfo
    {
        internal LabelInfo(LabelHandle metaLabel)
        {
            _position = -1;
            _startDepth = -1;
            _metaLabel = metaLabel;
        }
        internal int _position; // Position in the il stream, with -1 meaning unknown.
        internal int _startDepth; // Stack labelStartDepth, with -1 meaning unknown.
        internal LabelHandle _metaLabel;
    }
}
