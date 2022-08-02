// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;

using ILCompiler.Logging;

using ILLink.Shared;
using ILLink.Shared.DataFlow;
using ILLink.Shared.TrimAnalysis;
using ILLink.Shared.TypeSystemProxy;

using Internal.IL;
using Internal.TypeSystem;

using MultiValue = ILLink.Shared.DataFlow.ValueSet<ILLink.Shared.DataFlow.SingleValue>;

#nullable enable

namespace ILCompiler.Dataflow
{
    /// <summary>
    /// Tracks information about the contents of a stack slot
    /// </summary>
    readonly struct StackSlot
    {
        public MultiValue Value { get; }

        public StackSlot()
        {
            Value = new MultiValue(UnknownValue.Instance);
        }

        public StackSlot(SingleValue value)
        {
            Value = new MultiValue(value);
        }

        public StackSlot(MultiValue value)
        {
            Value = value;
        }
    }

    abstract partial class MethodBodyScanner
    {
        protected readonly InterproceduralStateLattice InterproceduralStateLattice;
        protected static ValueSetLattice<SingleValue> MultiValueLattice => default;

        protected readonly FlowAnnotations _annotations;

        internal MultiValue ReturnValue { private set; get; }

        protected MethodBodyScanner(FlowAnnotations annotations)
        {
            _annotations = annotations;
            InterproceduralStateLattice = new InterproceduralStateLattice(annotations.ILProvider, default, default);
        }

        protected virtual void WarnAboutInvalidILInMethod(MethodIL method, int ilOffset)
        {
        }

        private void CheckForInvalidStack(Stack<StackSlot> stack, int depthRequired, MethodIL method, int ilOffset)
        {
            if (stack.Count < depthRequired)
            {
                WarnAboutInvalidILInMethod(method, ilOffset);
                while (stack.Count < depthRequired)
                    stack.Push(new StackSlot()); // Push dummy values to avoid crashes.
                                                 // Analysis of this method will be incorrect.
            }
        }

        private static void PushUnknown(Stack<StackSlot> stack)
        {
            stack.Push(new StackSlot());
        }

        private void PushUnknownAndWarnAboutInvalidIL(Stack<StackSlot> stack, MethodIL methodBody, int offset)
        {
            WarnAboutInvalidILInMethod(methodBody, offset);
            PushUnknown(stack);
        }

        private StackSlot PopUnknown(Stack<StackSlot> stack, int count, MethodIL method, int ilOffset)
        {
            if (count < 1)
                throw new InvalidOperationException();

            StackSlot topOfStack = default;
            CheckForInvalidStack(stack, count, method, ilOffset);

            for (int i = 0; i < count; ++i)
            {
                StackSlot slot = stack.Pop();
                if (i == 0)
                    topOfStack = slot;
            }
            return topOfStack;
        }

        private static StackSlot MergeStackElement(StackSlot a, StackSlot b)
        {
            return new StackSlot(MultiValueLattice.Meet(a.Value, b.Value));
        }

        // Merge stacks together. This may return the first stack, the stack length must be the same for the two stacks.
        private static Stack<StackSlot> MergeStack(Stack<StackSlot> a, Stack<StackSlot> b)
        {
            if (a.Count != b.Count)
            {
                // Force stacks to be of equal size to avoid crashes.
                // Analysis of this method will be incorrect.
                while (a.Count < b.Count)
                    a.Push(new StackSlot());

                while (b.Count < a.Count)
                    b.Push(new StackSlot());
            }

            Stack<StackSlot> newStack = new Stack<StackSlot>(a.Count);
            IEnumerator<StackSlot> aEnum = a.GetEnumerator();
            IEnumerator<StackSlot> bEnum = b.GetEnumerator();
            while (aEnum.MoveNext() && bEnum.MoveNext())
            {
                newStack.Push(MergeStackElement(aEnum.Current, bEnum.Current));
            }

            // The new stack is reversed. Use the copy constructor to reverse it back
            return new Stack<StackSlot>(newStack);
        }

        private static void ClearStack(ref Stack<StackSlot>? stack)
        {
            stack = null;
        }

        private static void NewKnownStack(Dictionary<int, Stack<StackSlot>> knownStacks, int newOffset, Stack<StackSlot> newStack)
        {
            // No need to merge in empty stacks
            if (newStack.Count == 0)
            {
                return;
            }

            if (knownStacks.ContainsKey(newOffset))
            {
                knownStacks[newOffset] = MergeStack(knownStacks[newOffset], newStack);
            }
            else
            {
                knownStacks.Add(newOffset, new Stack<StackSlot>(newStack.Reverse()));
            }
        }

        private struct BasicBlockIterator
        {
            readonly HashSet<int> _methodBranchTargets;
            int _currentBlockIndex;
            bool _foundEndOfPrevBlock;
            MethodIL _methodBody;

            public BasicBlockIterator(MethodIL methodBody)
            {
                _methodBranchTargets = methodBody.ComputeBranchTargets();
                _currentBlockIndex = -1;
                _foundEndOfPrevBlock = true;
                _methodBody = methodBody;
            }

            public int CurrentBlockIndex
            {
                get
                {
                    return _currentBlockIndex;
                }
            }

            public int MoveNext(int offset)
            {
                if (_foundEndOfPrevBlock || _methodBranchTargets.Contains(offset))
                {
                    _currentBlockIndex++;
                    _foundEndOfPrevBlock = false;
                }

                var reader = new ILReader(_methodBody.GetILBytes());
                reader.Seek(offset);
                ILOpcode opcode = reader.ReadILOpcode();
                if (opcode.IsControlFlowInstruction())
                {
                    _foundEndOfPrevBlock = true;
                }

                return CurrentBlockIndex;
            }
        }

        [Conditional("DEBUG")]
        static void ValidateNoReferenceToReference(ValueBasicBlockPair?[] locals, MethodIL method, int ilOffset)
        {
            for (int localVariableIndex = 0; localVariableIndex < locals.Length; localVariableIndex++)
            {
                ValueBasicBlockPair? localVariable = locals[localVariableIndex];
                if (localVariable == null)
                    continue;

                MultiValue localValue = localVariable.Value.Value;
                foreach (var val in localValue)
                {
                    if (val is LocalVariableReferenceValue reference)
                    {
                        ValueBasicBlockPair? referenceLocalVariable = locals[reference.LocalIndex];
                        if (referenceLocalVariable.HasValue
                            && referenceLocalVariable.Value.Value.Any(v => v is ReferenceValue))
                        {
                            throw new InvalidOperationException(MessageContainer.CreateErrorMessage(
                                $"In method {method.OwningMethod.GetDisplayName()}, local variable {localVariableIndex} references variable {reference.LocalIndex} which is a reference.",
                                (int)DiagnosticId.LinkerUnexpectedError,
                                origin: new MessageOrigin(method, ilOffset)).ToMSBuildString());
                        }
                    }
                }
            }
        }

        private static void StoreMethodLocalValue(
            ValueBasicBlockPair?[] valueCollection,
            in MultiValue valueToStore,
            int index,
            int curBasicBlock)
        {
            MultiValue value;

            ValueBasicBlockPair? existingValue = valueCollection[index];
            if (!existingValue.HasValue
                || existingValue.Value.BasicBlockIndex == curBasicBlock)
            {
                // If the previous value was stored in the current basic block, then we can safely 
                // overwrite the previous value with the new one.
                value = valueToStore;
            }
            else
            {
                // If the previous value came from a previous basic block, then some other use of 
                // the local could see the previous value, so we must merge the new value with the 
                // old value.
                value = MultiValueLattice.Meet(existingValue.Value.Value, valueToStore);
            }
            valueCollection[index] = new ValueBasicBlockPair(value, curBasicBlock);
        }

        private static void StoreMethodLocalValue<KeyType>(
            Dictionary<KeyType, ValueBasicBlockPair> valueCollection,
            in MultiValue valueToStore,
            KeyType collectionKey,
            int curBasicBlock,
            int? maxTrackedValues = null)
            where KeyType : notnull
        {
            if (valueCollection.TryGetValue(collectionKey, out ValueBasicBlockPair existingValue))
            {
                MultiValue value;

                if (existingValue.BasicBlockIndex == curBasicBlock)
                {
                    // If the previous value was stored in the current basic block, then we can safely 
                    // overwrite the previous value with the new one.
                    value = valueToStore;
                }
                else
                {
                    // If the previous value came from a previous basic block, then some other use of 
                    // the local could see the previous value, so we must merge the new value with the 
                    // old value.
                    value = MultiValueLattice.Meet(existingValue.Value, valueToStore);
                }
                valueCollection[collectionKey] = new ValueBasicBlockPair(value, curBasicBlock);
            }
            else if (maxTrackedValues == null || valueCollection.Count < maxTrackedValues)
            {
                // We're not currently tracking a value a this index, so store the value now.
                valueCollection[collectionKey] = new ValueBasicBlockPair(valueToStore, curBasicBlock);
            }
        }

        // Scans the method as well as any nested functions (local functions or lambdas) and state machines
        // reachable from it.
        public virtual void InterproceduralScan(MethodIL startingMethodBody)
        {
            MethodDesc startingMethod = startingMethodBody.OwningMethod;
            Debug.Assert(startingMethod.IsTypicalMethodDefinition);

            // We should never have created a DataFlowAnalyzedMethodNode for compiler generated methods
            // since their data flow analysis is handled as part of their parent method analysis.
            Debug.Assert(!CompilerGeneratedState.IsNestedFunctionOrStateMachineMember(startingMethod));

            // Note that the default value of a hoisted local will be MultiValueLattice.Top, not UnknownValue.Instance.
            // This ensures that there are no warnings for the "unassigned state" of a parameter.
            // Definite assignment should ensure that there is no way for this to be an analysis hole.
            var interproceduralState = InterproceduralStateLattice.Top;

            var oldInterproceduralState = interproceduralState.Clone();
            interproceduralState.TrackMethod(startingMethodBody);

            while (!interproceduralState.Equals(oldInterproceduralState))
            {
                oldInterproceduralState = interproceduralState.Clone();

                // Flow state through all methods encountered so far, as long as there
                // are changes discovered in the hoisted local state on entry to any method.
                foreach (var methodBodyValue in oldInterproceduralState.MethodBodies)
                    Scan(methodBodyValue.MethodBody, ref interproceduralState);
            }

#if DEBUG
            // Validate that the compiler-generated callees tracked by the compiler-generated state
            // are the same set of methods that we discovered and scanned above.
            if (_annotations.CompilerGeneratedState.TryGetCompilerGeneratedCalleesForUserMethod(startingMethod, out List<TypeSystemEntity>? compilerGeneratedCallees))
            {
                var calleeMethods = compilerGeneratedCallees.OfType<MethodDefinition>();
                // https://github.com/dotnet/linker/issues/2845
                // Disabled asserts due to a bug
                // Debug.Assert (interproceduralState.Count == 1 + calleeMethods.Count ());
                // foreach (var method in calleeMethods)
                // 	Debug.Assert (interproceduralState.Any (kvp => kvp.Key.Method == method));
            }
            else
            {
                Debug.Assert(interproceduralState.MethodBodies.Count() == 1);
            }
#endif
        }

        void TrackNestedFunctionReference(MethodDesc referencedMethod, ref InterproceduralState interproceduralState)
        {
            MethodDesc method = referencedMethod.GetTypicalMethodDefinition();

            if (!CompilerGeneratedNames.IsLambdaOrLocalFunction(method.Name))
                return;

            interproceduralState.TrackMethod(method);
        }

        protected virtual void Scan(MethodIL methodBody, ref InterproceduralState interproceduralState)
        {
            MethodDesc thisMethod = methodBody.OwningMethod;

            ValueBasicBlockPair?[] locals = new ValueBasicBlockPair?[methodBody.GetLocals().Length];

            Dictionary<int, Stack<StackSlot>> knownStacks = new Dictionary<int, Stack<StackSlot>>();
            Stack<StackSlot>? currentStack = new Stack<StackSlot>(methodBody.MaxStack);

            ScanExceptionInformation(knownStacks, methodBody);

            BasicBlockIterator blockIterator = new BasicBlockIterator(methodBody);

            ReturnValue = new();
            ILReader reader = new ILReader(methodBody.GetILBytes());
            while (reader.HasNext)
            {
                ValidateNoReferenceToReference(locals, methodBody, reader.Offset);
                int curBasicBlock = blockIterator.MoveNext(reader.Offset);

                if (knownStacks.ContainsKey(reader.Offset))
                {
                    if (currentStack == null)
                    {
                        // The stack copy constructor reverses the stack
                        currentStack = new Stack<StackSlot>(knownStacks[reader.Offset].Reverse());
                    }
                    else
                    {
                        currentStack = MergeStack(currentStack, knownStacks[reader.Offset]);
                    }
                }

                if (currentStack == null)
                {
                    currentStack = new Stack<StackSlot>(methodBody.MaxStack);
                }

                int offset = reader.Offset;
                ILOpcode opcode = reader.ReadILOpcode();

                switch (opcode)
                {
                    case ILOpcode.add:
                    case ILOpcode.add_ovf:
                    case ILOpcode.add_ovf_un:
                    case ILOpcode.and:
                    case ILOpcode.div:
                    case ILOpcode.div_un:
                    case ILOpcode.mul:
                    case ILOpcode.mul_ovf:
                    case ILOpcode.mul_ovf_un:
                    case ILOpcode.or:
                    case ILOpcode.rem:
                    case ILOpcode.rem_un:
                    case ILOpcode.sub:
                    case ILOpcode.sub_ovf:
                    case ILOpcode.sub_ovf_un:
                    case ILOpcode.xor:
                    case ILOpcode.cgt:
                    case ILOpcode.cgt_un:
                    case ILOpcode.clt:
                    case ILOpcode.clt_un:
                    case ILOpcode.shl:
                    case ILOpcode.shr:
                    case ILOpcode.shr_un:
                    case ILOpcode.ceq:
                        PopUnknown(currentStack, 2, methodBody, offset);
                        PushUnknown(currentStack);
                        reader.Skip(opcode);
                        break;

                    case ILOpcode.dup:
                        currentStack.Push(currentStack.Peek());
                        break;

                    case ILOpcode.ldnull:
                        currentStack.Push(new StackSlot(NullValue.Instance));
                        break;


                    case ILOpcode.ldc_i4_0:
                    case ILOpcode.ldc_i4_1:
                    case ILOpcode.ldc_i4_2:
                    case ILOpcode.ldc_i4_3:
                    case ILOpcode.ldc_i4_4:
                    case ILOpcode.ldc_i4_5:
                    case ILOpcode.ldc_i4_6:
                    case ILOpcode.ldc_i4_7:
                    case ILOpcode.ldc_i4_8:
                        {
                            int value = opcode - ILOpcode.ldc_i4_0;
                            ConstIntValue civ = new ConstIntValue(value);
                            StackSlot slot = new StackSlot(civ);
                            currentStack.Push(slot);
                        }
                        break;

                    case ILOpcode.ldc_i4_m1:
                        {
                            ConstIntValue civ = new ConstIntValue(-1);
                            StackSlot slot = new StackSlot(civ);
                            currentStack.Push(slot);
                        }
                        break;

                    case ILOpcode.ldc_i4:
                        {
                            int value = (int)reader.ReadILUInt32();
                            ConstIntValue civ = new ConstIntValue(value);
                            StackSlot slot = new StackSlot(civ);
                            currentStack.Push(slot);
                        }
                        break;

                    case ILOpcode.ldc_i4_s:
                        {
                            int value = (sbyte)reader.ReadILByte();
                            ConstIntValue civ = new ConstIntValue(value);
                            StackSlot slot = new StackSlot(civ);
                            currentStack.Push(slot);
                        }
                        break;

                    case ILOpcode.arglist:
                    case ILOpcode.sizeof_:
                    case ILOpcode.ldc_i8:
                    case ILOpcode.ldc_r4:
                    case ILOpcode.ldc_r8:
                        PushUnknown(currentStack);
                        reader.Skip(opcode);
                        break;

                    case ILOpcode.ldftn:
                        {
                            if (methodBody.GetObject(reader.ReadILToken()) is MethodDesc methodOperand)
                            {
                                TrackNestedFunctionReference(methodOperand, ref interproceduralState);
                            }

                            PushUnknown(currentStack);
                        }
                        break;

                    case ILOpcode.ldarg:
                    case ILOpcode.ldarg_0:
                    case ILOpcode.ldarg_1:
                    case ILOpcode.ldarg_2:
                    case ILOpcode.ldarg_3:
                    case ILOpcode.ldarg_s:
                    case ILOpcode.ldarga:
                    case ILOpcode.ldarga_s:
                        ScanLdarg(opcode, opcode switch
                        {
                            ILOpcode.ldarg => reader.ReadILUInt16(),
                            ILOpcode.ldarga => reader.ReadILUInt16(),
                            ILOpcode.ldarg_s => reader.ReadILByte(),
                            ILOpcode.ldarga_s => reader.ReadILByte(),
                            _ => opcode - ILOpcode.ldarg_0
                        }, currentStack, thisMethod);
                        break;

                    case ILOpcode.ldloc:
                    case ILOpcode.ldloc_0:
                    case ILOpcode.ldloc_1:
                    case ILOpcode.ldloc_2:
                    case ILOpcode.ldloc_3:
                    case ILOpcode.ldloc_s:
                    case ILOpcode.ldloca:
                    case ILOpcode.ldloca_s:
                        ScanLdloc(methodBody, offset, opcode, opcode switch
                        {
                            ILOpcode.ldloc => reader.ReadILUInt16(),
                            ILOpcode.ldloca => reader.ReadILUInt16(),
                            ILOpcode.ldloc_s => reader.ReadILByte(),
                            ILOpcode.ldloca_s => reader.ReadILByte(),
                            _ => opcode - ILOpcode.ldloc_0
                        }, currentStack, locals);
                        break;

                    case ILOpcode.ldstr:
                        {
                            StackSlot slot = new StackSlot(new KnownStringValue((string)methodBody.GetObject(reader.ReadILToken())));
                            currentStack.Push(slot);
                        }
                        break;

                    case ILOpcode.ldtoken:
                        object obj = methodBody.GetObject(reader.ReadILToken());
                        ScanLdtoken(methodBody, obj, currentStack);
                        break;

                    case ILOpcode.ldind_i:
                    case ILOpcode.ldind_i1:
                    case ILOpcode.ldind_i2:
                    case ILOpcode.ldind_i4:
                    case ILOpcode.ldind_i8:
                    case ILOpcode.ldind_r4:
                    case ILOpcode.ldind_r8:
                    case ILOpcode.ldind_u1:
                    case ILOpcode.ldind_u2:
                    case ILOpcode.ldind_u4:
                    case ILOpcode.ldlen:
                    case ILOpcode.ldvirtftn:
                    case ILOpcode.localloc:
                    case ILOpcode.refanytype:
                    case ILOpcode.refanyval:
                    case ILOpcode.conv_i1:
                    case ILOpcode.conv_i2:
                    case ILOpcode.conv_i4:
                    case ILOpcode.conv_ovf_i1:
                    case ILOpcode.conv_ovf_i1_un:
                    case ILOpcode.conv_ovf_i2:
                    case ILOpcode.conv_ovf_i2_un:
                    case ILOpcode.conv_ovf_i4:
                    case ILOpcode.conv_ovf_i4_un:
                    case ILOpcode.conv_ovf_u:
                    case ILOpcode.conv_ovf_u_un:
                    case ILOpcode.conv_ovf_u1:
                    case ILOpcode.conv_ovf_u1_un:
                    case ILOpcode.conv_ovf_u2:
                    case ILOpcode.conv_ovf_u2_un:
                    case ILOpcode.conv_ovf_u4:
                    case ILOpcode.conv_ovf_u4_un:
                    case ILOpcode.conv_u1:
                    case ILOpcode.conv_u2:
                    case ILOpcode.conv_u4:
                    case ILOpcode.conv_i8:
                    case ILOpcode.conv_ovf_i8:
                    case ILOpcode.conv_ovf_i8_un:
                    case ILOpcode.conv_ovf_u8:
                    case ILOpcode.conv_ovf_u8_un:
                    case ILOpcode.conv_u8:
                    case ILOpcode.conv_i:
                    case ILOpcode.conv_ovf_i:
                    case ILOpcode.conv_ovf_i_un:
                    case ILOpcode.conv_u:
                    case ILOpcode.conv_r_un:
                    case ILOpcode.conv_r4:
                    case ILOpcode.conv_r8:
                    case ILOpcode.ldind_ref:
                    case ILOpcode.ldobj:
                    case ILOpcode.mkrefany:
                    case ILOpcode.unbox:
                    case ILOpcode.unbox_any:
                    case ILOpcode.box:
                    case ILOpcode.neg:
                    case ILOpcode.not:
                        PopUnknown(currentStack, 1, methodBody, offset);
                        PushUnknown(currentStack);
                        reader.Skip(opcode);
                        break;

                    case ILOpcode.isinst:
                    case ILOpcode.castclass:
                        // We can consider a NOP because the value doesn't change.
                        // It might change to NULL, but for the purposes of dataflow analysis
                        // it doesn't hurt much.
                        reader.Skip(opcode);
                        break;

                    case ILOpcode.ldfld:
                    case ILOpcode.ldsfld:
                    case ILOpcode.ldflda:
                    case ILOpcode.ldsflda:
                        ScanLdfld(methodBody, offset, opcode, (FieldDesc)methodBody.GetObject(reader.ReadILToken()), currentStack, ref interproceduralState);
                        break;

                    case ILOpcode.newarr:
                        {
                            StackSlot count = PopUnknown(currentStack, 1, methodBody, offset);
                            var arrayElement = (TypeDesc)methodBody.GetObject(reader.ReadILToken());
                            currentStack.Push(new StackSlot(ArrayValue.Create(count.Value, arrayElement)));
                        }
                        break;

                    case ILOpcode.stelem_i:
                    case ILOpcode.stelem_i1:
                    case ILOpcode.stelem_i2:
                    case ILOpcode.stelem_i4:
                    case ILOpcode.stelem_i8:
                    case ILOpcode.stelem_r4:
                    case ILOpcode.stelem_r8:
                    case ILOpcode.stelem:
                    case ILOpcode.stelem_ref:
                        ScanStelem(offset, currentStack, methodBody, curBasicBlock);
                        reader.Skip(opcode);
                        break;

                    case ILOpcode.ldelem_i:
                    case ILOpcode.ldelem_i1:
                    case ILOpcode.ldelem_i2:
                    case ILOpcode.ldelem_i4:
                    case ILOpcode.ldelem_i8:
                    case ILOpcode.ldelem_r4:
                    case ILOpcode.ldelem_r8:
                    case ILOpcode.ldelem_u1:
                    case ILOpcode.ldelem_u2:
                    case ILOpcode.ldelem_u4:
                    case ILOpcode.ldelem:
                    case ILOpcode.ldelem_ref:
                    case ILOpcode.ldelema:
                        ScanLdelem(opcode, offset, currentStack, methodBody, curBasicBlock);
                        reader.Skip(opcode);
                        break;

                    case ILOpcode.cpblk:
                    case ILOpcode.initblk:
                        PopUnknown(currentStack, 3, methodBody, offset);
                        reader.Skip(opcode);
                        break;

                    case ILOpcode.stfld:
                    case ILOpcode.stsfld:
                        ScanStfld(methodBody, offset, opcode, (FieldDesc)methodBody.GetObject(reader.ReadILToken()), currentStack, ref interproceduralState);
                        break;

                    case ILOpcode.cpobj:
                        PopUnknown(currentStack, 2, methodBody, offset);
                        reader.Skip(opcode);
                        break;

                    case ILOpcode.stind_i:
                    case ILOpcode.stind_i1:
                    case ILOpcode.stind_i2:
                    case ILOpcode.stind_i4:
                    case ILOpcode.stind_i8:
                    case ILOpcode.stind_r4:
                    case ILOpcode.stind_r8:
                    case ILOpcode.stind_ref:
                    case ILOpcode.stobj:
                        ScanIndirectStore(methodBody, offset, currentStack, locals, curBasicBlock);
                        reader.Skip(opcode);
                        break;

                    case ILOpcode.initobj:
                    case ILOpcode.pop:
                        PopUnknown(currentStack, 1, methodBody, offset);
                        reader.Skip(opcode);
                        break;

                    case ILOpcode.starg:
                    case ILOpcode.starg_s:
                        ScanStarg(methodBody, offset, opcode == ILOpcode.starg ? reader.ReadILUInt16() : reader.ReadILByte(), currentStack);
                        break;

                    case ILOpcode.stloc:
                    case ILOpcode.stloc_s:
                    case ILOpcode.stloc_0:
                    case ILOpcode.stloc_1:
                    case ILOpcode.stloc_2:
                    case ILOpcode.stloc_3:
                        ScanStloc(methodBody, offset, opcode switch
                        {
                            ILOpcode.stloc => reader.ReadILUInt16(),
                            ILOpcode.stloc_s => reader.ReadILByte(),
                            _ => opcode - ILOpcode.stloc_0,
                        }, currentStack, locals, curBasicBlock);
                        break;

                    case ILOpcode.constrained:
                    case ILOpcode.no:
                    case ILOpcode.readonly_:
                    case ILOpcode.tail:
                    case ILOpcode.unaligned:
                    case ILOpcode.volatile_:
                        reader.Skip(opcode);
                        break;

                    case ILOpcode.brfalse:
                    case ILOpcode.brfalse_s:
                    case ILOpcode.brtrue:
                    case ILOpcode.brtrue_s:
                        PopUnknown(currentStack, 1, methodBody, offset);
                        NewKnownStack(knownStacks, reader.ReadBranchDestination(opcode), currentStack);
                        break;

                    case ILOpcode.calli:
                        {
                            var signature = (MethodSignature)methodBody.GetObject(reader.ReadILToken());
                            if (!signature.IsStatic)
                            {
                                PopUnknown(currentStack, 1, methodBody, offset);
                            }

                            // Pop arguments
                            if (signature.Length > 0)
                                PopUnknown(currentStack, signature.Length, methodBody, offset);

                            // Pop function pointer
                            PopUnknown(currentStack, 1, methodBody, offset);

                            // Push return value
                            if (!signature.ReturnType.IsVoid)
                                PushUnknown(currentStack);
                        }
                        break;

                    case ILOpcode.call:
                    case ILOpcode.callvirt:
                    case ILOpcode.newobj:
                        {
                            MethodDesc methodOperand = (MethodDesc)methodBody.GetObject(reader.ReadILToken());
                            TrackNestedFunctionReference(methodOperand, ref interproceduralState);
                            HandleCall(methodBody, opcode, offset, methodOperand, currentStack, locals, ref interproceduralState, curBasicBlock);
                        }
                        break;

                    case ILOpcode.jmp:
                        // Not generated by mainstream compilers
                        reader.Skip(opcode);
                        break;

                    case ILOpcode.br:
                    case ILOpcode.br_s:
                        NewKnownStack(knownStacks, reader.ReadBranchDestination(opcode), currentStack);
                        ClearStack(ref currentStack);
                        break;

                    case ILOpcode.leave:
                    case ILOpcode.leave_s:
                        ClearStack(ref currentStack);
                        NewKnownStack(knownStacks, reader.ReadBranchDestination(opcode), new Stack<StackSlot>(methodBody.MaxStack));
                        break;

                    case ILOpcode.endfilter:
                    case ILOpcode.endfinally:
                    case ILOpcode.rethrow:
                    case ILOpcode.throw_:
                        ClearStack(ref currentStack);
                        break;

                    case ILOpcode.ret:
                        {
                            bool hasReturnValue = !methodBody.OwningMethod.Signature.ReturnType.IsVoid;
                            if (currentStack.Count != (hasReturnValue ? 1 : 0))
                            {
                                WarnAboutInvalidILInMethod(methodBody, offset);
                            }
                            if (hasReturnValue)
                            {
                                StackSlot retValue = PopUnknown(currentStack, 1, methodBody, offset);
                                // If the return value is a reference, treat it as the value itself for now
                                //	We can handle ref return values better later
                                ReturnValue = MultiValueLattice.Meet(ReturnValue, DereferenceValue(retValue.Value, locals, ref interproceduralState));
                            }
                            ClearStack(ref currentStack);
                            break;
                        }

                    case ILOpcode.switch_:
                        {
                            PopUnknown(currentStack, 1, methodBody, offset);

                            uint count = reader.ReadILUInt32();
                            int jmpBase = reader.Offset + (int)(4 * count);
                            for (uint i = 0; i < count; i++)
                            {
                                NewKnownStack(knownStacks, (int)reader.ReadILUInt32() + jmpBase, currentStack);
                            }
                            break;
                        }

                    case ILOpcode.beq:
                    case ILOpcode.beq_s:
                    case ILOpcode.bne_un:
                    case ILOpcode.bne_un_s:
                    case ILOpcode.bge:
                    case ILOpcode.bge_s:
                    case ILOpcode.bge_un:
                    case ILOpcode.bge_un_s:
                    case ILOpcode.bgt:
                    case ILOpcode.bgt_s:
                    case ILOpcode.bgt_un:
                    case ILOpcode.bgt_un_s:
                    case ILOpcode.ble:
                    case ILOpcode.ble_s:
                    case ILOpcode.ble_un:
                    case ILOpcode.ble_un_s:
                    case ILOpcode.blt:
                    case ILOpcode.blt_s:
                    case ILOpcode.blt_un:
                    case ILOpcode.blt_un_s:
                        PopUnknown(currentStack, 2, methodBody, offset);
                        NewKnownStack(knownStacks, reader.ReadBranchDestination(opcode), currentStack);
                        break;
                    default:
                        reader.Skip(opcode);
                        break;
                }
            }
        }

        private static void ScanExceptionInformation(Dictionary<int, Stack<StackSlot>> knownStacks, MethodIL methodBody)
        {
            foreach (ILExceptionRegion exceptionClause in methodBody.GetExceptionRegions())
            {
                Stack<StackSlot> catchStack = new Stack<StackSlot>(1);
                catchStack.Push(new StackSlot());

                if (exceptionClause.Kind == ILExceptionRegionKind.Filter)
                {
                    NewKnownStack(knownStacks, exceptionClause.FilterOffset, catchStack);
                    NewKnownStack(knownStacks, exceptionClause.HandlerOffset, catchStack);
                }
                if (exceptionClause.Kind == ILExceptionRegionKind.Catch)
                {
                    NewKnownStack(knownStacks, exceptionClause.HandlerOffset, catchStack);
                }
            }
        }

        protected abstract SingleValue GetMethodParameterValue(MethodDesc method, int parameterIndex);

        private void ScanLdarg(ILOpcode opcode, int paramNum, Stack<StackSlot> currentStack, MethodDesc thisMethod)
        {
            bool isByRef;

            if (!thisMethod.Signature.IsStatic && paramNum == 0)
            {
                isByRef = thisMethod.OwningType.IsValueType;
            }
            else
            {
                // This is semantically wrong if it returns true - we would representing a reference parameter as a reference to a parameter - but it should be fine for now
                isByRef = thisMethod.Signature[paramNum - (thisMethod.Signature.IsStatic ? 0 : 1)].IsByRefOrPointer();
            }

            isByRef |= opcode == ILOpcode.ldarga || opcode == ILOpcode.ldarga_s;

            StackSlot slot = new StackSlot(
                isByRef
                ? new ParameterReferenceValue(thisMethod, paramNum)
                : GetMethodParameterValue(thisMethod, paramNum));
            currentStack.Push(slot);
        }

        private void ScanStarg(
            MethodIL methodBody,
            int offset,
            int index,
            Stack<StackSlot> currentStack
            )
        {
            var valueToStore = PopUnknown(currentStack, 1, methodBody, offset);
            var targetValue = GetMethodParameterValue(methodBody.OwningMethod, index);
            if (targetValue is MethodParameterValue targetParameterValue)
                HandleStoreParameter(methodBody, offset, targetParameterValue, valueToStore.Value);

            // If the targetValue is MethodThisValue do nothing - it should never happen really, and if it does, there's nothing we can track there
        }

        private void ScanLdloc(
            MethodIL methodBody,
            int offset,
            ILOpcode operation,
            int index,
            Stack<StackSlot> currentStack,
            ValueBasicBlockPair?[] locals)
        {
            bool isByRef = operation == ILOpcode.ldloca || operation == ILOpcode.ldloca_s;

            ValueBasicBlockPair? localValue = locals[index];
            StackSlot newSlot;
            if (isByRef)
            {
                newSlot = new StackSlot(new LocalVariableReferenceValue(index));
            }
            else if (localValue.HasValue)
                newSlot = new StackSlot(localValue.Value.Value);
            else
                newSlot = new StackSlot(UnknownValue.Instance);
            currentStack.Push(newSlot);
        }

        private static void ScanLdtoken(MethodIL methodBody, object operand, Stack<StackSlot> currentStack)
        {
            if (operand is TypeDesc type)
            {
                if (type.IsGenericParameter)
                {
                    StackSlot slot = new StackSlot(new RuntimeTypeHandleForGenericParameterValue((GenericParameterDesc)type));
                    currentStack.Push(slot);
                }
                else
                {
                    // Note that Nullable types without a generic argument (i.e. Nullable<>) will be RuntimeTypeHandleValue / SystemTypeValue
                    if (type.HasInstantiation && !type.IsGenericDefinition && type.IsTypeOf(ILLink.Shared.TypeSystemProxy.WellKnownType.System_Nullable_T))
                    {
                        switch (type.Instantiation[0])
                        {
                            case GenericParameterDesc genericParam:
                                var nullableDam = new RuntimeTypeHandleForNullableValueWithDynamicallyAccessedMembers(new TypeProxy(type),
                                    new RuntimeTypeHandleForGenericParameterValue(genericParam));
                                currentStack.Push(new StackSlot(nullableDam));
                                return;
                            case MetadataType underlyingType:
                                var nullableType = new RuntimeTypeHandleForNullableSystemTypeValue(new TypeProxy(type), new SystemTypeValue(underlyingType));
                                currentStack.Push(new StackSlot(nullableType));
                                return;
                            default:
                                PushUnknown(currentStack);
                                return;
                        }
                    }
                    else
                    {
                        var typeHandle = new RuntimeTypeHandleValue(new TypeProxy(type));
                        currentStack.Push(new StackSlot(typeHandle));
                        return;
                    }
                }
            }
            else if (operand is MethodDesc method)
            {
                StackSlot slot = new StackSlot(new RuntimeMethodHandleValue(method));
                currentStack.Push(slot);
            }
            else
            {
                PushUnknown(currentStack);
            }
        }

        private void ScanStloc(
            MethodIL methodBody,
            int offset,
            int index,
            Stack<StackSlot> currentStack,
            ValueBasicBlockPair?[] locals,
            int curBasicBlock)
        {
            StackSlot valueToStore = PopUnknown(currentStack, 1, methodBody, offset);
            StoreMethodLocalValue(locals, valueToStore.Value, index, curBasicBlock);
        }

        private void ScanIndirectStore(
            MethodIL methodBody,
            int offset,
            Stack<StackSlot> currentStack,
            ValueBasicBlockPair?[] locals,
            int curBasicBlock)
        {
            StackSlot valueToStore = PopUnknown(currentStack, 1, methodBody, offset);
            StackSlot destination = PopUnknown(currentStack, 1, methodBody, offset);

            StoreInReference(destination.Value, valueToStore.Value, methodBody, offset, locals, curBasicBlock);
        }

        /// <summary>
        /// Handles storing the source value in a target <see cref="ReferenceValue"/> or MultiValue of ReferenceValues.
        /// </summary>
        /// <param name="target">A set of <see cref="ReferenceValue"/> that a value is being stored into</param>
        /// <param name="source">The value to store</param>
        /// <param name="method">The method body that contains the operation causing the store</param>
        /// <param name="offset">The instruction offset causing the store</param>
        /// <exception cref="LinkerFatalErrorException">Throws if <paramref name="target"/> is not a valid target for an indirect store.</exception>
        protected void StoreInReference(MultiValue target, MultiValue source, MethodIL method, int offset, ValueBasicBlockPair?[] locals, int curBasicBlock)
        {
            foreach (var value in target)
            {
                switch (value)
                {
                    case LocalVariableReferenceValue localReference:
                        StoreMethodLocalValue(locals, source, localReference.LocalIndex, curBasicBlock);
                        break;
                    case FieldReferenceValue fieldReference
                when GetFieldValue(fieldReference.FieldDefinition).AsSingleValue() is FieldValue fieldValue:
                        HandleStoreField(method, offset, fieldValue, source);
                        break;
                    case ParameterReferenceValue parameterReference
                when GetMethodParameterValue(parameterReference.MethodDefinition, parameterReference.ParameterIndex) is MethodParameterValue parameterValue:
                        HandleStoreParameter(method, offset, parameterValue, source);
                        break;
                    case ParameterReferenceValue parameterReference
                    when GetMethodParameterValue(parameterReference.MethodDefinition, parameterReference.ParameterIndex) is MethodThisParameterValue thisParameterValue:
                        HandleStoreMethodThisParameter(method, offset, thisParameterValue, source);
                        break;
                    case MethodReturnValue methodReturnValue:
                        // Ref returns don't have special ReferenceValue values, so assume if the target here is a MethodReturnValue then it must be a ref return value
                        HandleStoreMethodReturnValue(method, offset, methodReturnValue, source);
                        break;
                    case IValueWithStaticType valueWithStaticType:
                        if (valueWithStaticType.StaticType is not null && FlowAnnotations.IsTypeInterestingForDataflow(valueWithStaticType.StaticType))
                            throw new InvalidOperationException(MessageContainer.CreateErrorMessage(
                                $"Unhandled StoreReference call. Unhandled attempt to store a value in {value} of type {value.GetType()}.",
                                (int)DiagnosticId.LinkerUnexpectedError,
                                origin: new MessageOrigin(method, offset)).ToMSBuildString());
                        // This should only happen for pointer derefs, which can't point to interesting types
                        break;
                    default:
                        // These cases should only be refs to array elements
                        // References to array elements are not yet tracked and since we don't allow annotations on arrays, they won't cause problems
                        break;
                }
            }

        }

        protected abstract MultiValue GetFieldValue(FieldDesc field);

        private void ScanLdfld(
            MethodIL methodBody,
            int offset,
            ILOpcode opcode,
            FieldDesc field,
            Stack<StackSlot> currentStack,
            ref InterproceduralState interproceduralState)
        {
            if (opcode == ILOpcode.ldfld || opcode == ILOpcode.ldflda)
                PopUnknown(currentStack, 1, methodBody, offset);

            bool isByRef = opcode == ILOpcode.ldflda || opcode == ILOpcode.ldsflda;

            MultiValue value;
            if (isByRef)
            {
                value = new FieldReferenceValue(field);
            }
            else if (CompilerGeneratedState.IsHoistedLocal(field))
            {
                value = interproceduralState.GetHoistedLocal(new HoistedLocalKey(field));
            }
            else
            {
                value = GetFieldValue(field);
            }
            currentStack.Push(new StackSlot(value));
        }

        protected virtual void HandleStoreField(MethodIL method, int offset, FieldValue field, MultiValue valueToStore)
        {
        }

        protected virtual void HandleStoreParameter(MethodIL method, int offset, MethodParameterValue parameter, MultiValue valueToStore)
        {
        }

        protected virtual void HandleStoreMethodThisParameter(MethodIL method, int offset, MethodThisParameterValue thisParameter, MultiValue sourceValue)
        {
        }

        protected virtual void HandleStoreMethodReturnValue(MethodIL method, int offset, MethodReturnValue thisParameter, MultiValue sourceValue)
        {
        }

        private void ScanStfld(
            MethodIL methodBody,
            int offset,
            ILOpcode opcode,
            FieldDesc field,
            Stack<StackSlot> currentStack,
            ref InterproceduralState interproceduralState)
        {
            StackSlot valueToStoreSlot = PopUnknown(currentStack, 1, methodBody, offset);
            if (opcode == ILOpcode.stfld)
                PopUnknown(currentStack, 1, methodBody, offset);

            if (CompilerGeneratedState.IsHoistedLocal(field))
            {
                interproceduralState.SetHoistedLocal(new HoistedLocalKey(field), valueToStoreSlot.Value);
                return;
            }

            foreach (var value in GetFieldValue(field))
            {
                // GetFieldValue may return different node types, in which case they can't be stored to.
                // At least not yet.
                if (value is not FieldValue fieldValue)
                    continue;

                HandleStoreField(methodBody, offset, fieldValue, valueToStoreSlot.Value);
            }
        }

        private ValueNodeList PopCallArguments(
            Stack<StackSlot> currentStack,
            MethodDesc methodCalled,
            MethodIL containingMethodBody,
            bool isNewObj, int ilOffset,
            out SingleValue? newObjValue)
        {
            newObjValue = null;

            int countToPop = 0;
            if (!isNewObj && !methodCalled.Signature.IsStatic)
                countToPop++;
            countToPop += methodCalled.Signature.Length;

            ValueNodeList methodParams = new ValueNodeList(countToPop);
            for (int iParam = 0; iParam < countToPop; ++iParam)
            {
                StackSlot slot = PopUnknown(currentStack, 1, containingMethodBody, ilOffset);
                methodParams.Add(slot.Value);
            }

            if (isNewObj)
            {
                newObjValue = UnknownValue.Instance;
                methodParams.Add(newObjValue);
            }
            methodParams.Reverse();
            return methodParams;
        }

        internal MultiValue DereferenceValue(MultiValue maybeReferenceValue, ValueBasicBlockPair?[] locals, ref InterproceduralState interproceduralState)
        {
            MultiValue dereferencedValue = MultiValueLattice.Top;
            foreach (var value in maybeReferenceValue)
            {
                switch (value)
                {
                    case FieldReferenceValue fieldReferenceValue:
                        dereferencedValue = MultiValue.Meet(
                            dereferencedValue,
                            CompilerGeneratedState.IsHoistedLocal(fieldReferenceValue.FieldDefinition)
                                ? interproceduralState.GetHoistedLocal(new HoistedLocalKey(fieldReferenceValue.FieldDefinition))
                                : GetFieldValue(fieldReferenceValue.FieldDefinition));
                        break;
                    case ParameterReferenceValue parameterReferenceValue:
                        dereferencedValue = MultiValue.Meet(
                            dereferencedValue,
                            GetMethodParameterValue(parameterReferenceValue.MethodDefinition, parameterReferenceValue.ParameterIndex));
                        break;
                    case LocalVariableReferenceValue localVariableReferenceValue:
                        var valueBasicBlockPair = locals[localVariableReferenceValue.LocalIndex];
                        if (valueBasicBlockPair.HasValue)
                            dereferencedValue = MultiValue.Meet(dereferencedValue, valueBasicBlockPair.Value.Value);
                        else
                            dereferencedValue = MultiValue.Meet(dereferencedValue, UnknownValue.Instance);
                        break;
                    case ReferenceValue referenceValue:
                        throw new NotImplementedException($"Unhandled dereference of ReferenceValue of type {referenceValue.GetType().FullName}");
                    default:
                        dereferencedValue = MultiValue.Meet(dereferencedValue, value);
                        break;
                }
            }
            return dereferencedValue;
        }

        /// <summary>
        /// Assigns a MethodParameterValue to the location of each parameter passed by reference. (i.e. assigns the value to x when passing `ref x` as a parameter)
        /// </summary>
        protected void AssignRefAndOutParameters(
            MethodIL callingMethodBody,
            MethodDesc calledMethod,
            ValueNodeList methodArguments,
            int offset,
            ValueBasicBlockPair?[] locals,
            int curBasicBlock)
        {
            int parameterOffset = !calledMethod.Signature.IsStatic ? 1 : 0;
            int parameterIndex = 0;
            for (int ilArgumentIndex = parameterOffset; ilArgumentIndex < methodArguments.Count; ilArgumentIndex++, parameterIndex++)
            {
                if (calledMethod.ParameterReferenceKind(ilArgumentIndex) is not (ReferenceKind.Ref or ReferenceKind.Out))
                    continue;
                SingleValue newByRefValue = _annotations.GetMethodParameterValue(calledMethod, parameterIndex);
                StoreInReference(methodArguments[ilArgumentIndex], newByRefValue, callingMethodBody, offset, locals, curBasicBlock);
            }
        }

        private void HandleCall(
            MethodIL callingMethodBody,
            ILOpcode opcode,
            int offset,
            MethodDesc calledMethod,
            Stack<StackSlot> currentStack,
            ValueBasicBlockPair?[] locals,
            ref InterproceduralState interproceduralState,
            int curBasicBlock)
        {
            bool isNewObj = opcode == ILOpcode.newobj;

            SingleValue? newObjValue;
            ValueNodeList methodArguments = PopCallArguments(currentStack, calledMethod, callingMethodBody, isNewObj,
                                                             offset, out newObjValue);

            // Multi-dimensional array access is represented as a call to a special Get method on the array (runtime provided method)
            // We don't track multi-dimensional arrays in any way, so return unknown value.
            if (calledMethod is ArrayMethod { Kind: ArrayMethodKind.Get or ArrayMethodKind.Address })
            {
                currentStack.Push(new StackSlot(UnknownValue.Instance));
                return;
            }

            var dereferencedMethodParams = new List<MultiValue>();
            foreach (var argument in methodArguments)
                dereferencedMethodParams.Add(DereferenceValue(argument, locals, ref interproceduralState));
            MultiValue methodReturnValue;
            bool handledFunction = HandleCall(
                callingMethodBody,
                calledMethod,
                opcode,
                offset,
                new ValueNodeList(dereferencedMethodParams),
                out methodReturnValue);

            // Handle the return value or newobj result
            if (!handledFunction)
            {
                if (isNewObj)
                {
                    if (newObjValue == null)
                        methodReturnValue = UnknownValue.Instance;
                    else
                        methodReturnValue = newObjValue;
                }
                else
                {
                    if (!calledMethod.Signature.ReturnType.IsVoid)
                    {
                        methodReturnValue = UnknownValue.Instance;
                    }
                }
            }

            if (isNewObj || !calledMethod.Signature.ReturnType.IsVoid)
                currentStack.Push(new StackSlot(methodReturnValue));

            AssignRefAndOutParameters(callingMethodBody, calledMethod, methodArguments, offset, locals, curBasicBlock);

            foreach (var param in methodArguments)
            {
                foreach (var v in param)
                {
                    if (v is ArrayValue arr)
                    {
                        MarkArrayValuesAsUnknown(arr, curBasicBlock);
                    }
                }
            }
        }

        public abstract bool HandleCall(
            MethodIL callingMethodBody,
            MethodDesc calledMethod,
            ILOpcode operation,
            int offset,
            ValueNodeList methodParams,
            out MultiValue methodReturnValue);

        // Limit tracking array values to 32 values for performance reasons. There are many arrays much longer than 32 elements in .NET, but the interesting ones for the linker are nearly always less than 32 elements.
        private const int MaxTrackedArrayValues = 32;

        private static void MarkArrayValuesAsUnknown(ArrayValue arrValue, int curBasicBlock)
        {
            // Since we can't know the current index we're storing the value at, clear all indices.
            // That way we won't accidentally think we know the value at a given index when we cannot.
            foreach (var knownIndex in arrValue.IndexValues.Keys)
            {
                // Don't pass MaxTrackedArrayValues since we are only looking at keys we've already seen.
                StoreMethodLocalValue(arrValue.IndexValues, UnknownValue.Instance, knownIndex, curBasicBlock);
            }
        }

        private void ScanStelem(
            int offset,
            Stack<StackSlot> currentStack,
            MethodIL methodBody,
            int curBasicBlock)
        {
            StackSlot valueToStore = PopUnknown(currentStack, 1, methodBody, offset);
            StackSlot indexToStoreAt = PopUnknown(currentStack, 1, methodBody, offset);
            StackSlot arrayToStoreIn = PopUnknown(currentStack, 1, methodBody, offset);
            int? indexToStoreAtInt = indexToStoreAt.Value.AsConstInt();
            foreach (var array in arrayToStoreIn.Value)
            {
                if (array is ArrayValue arrValue)
                {
                    if (indexToStoreAtInt == null)
                    {
                        MarkArrayValuesAsUnknown(arrValue, curBasicBlock);
                    }
                    else
                    {
                        // When we know the index, we can record the value at that index.
                        StoreMethodLocalValue(arrValue.IndexValues, valueToStore.Value, indexToStoreAtInt.Value, curBasicBlock, MaxTrackedArrayValues);
                    }
                }
            }
        }

        private void ScanLdelem(
            ILOpcode opcode,
            int offset,
            Stack<StackSlot> currentStack,
            MethodIL methodBody,
            int curBasicBlock)
        {
            StackSlot indexToLoadFrom = PopUnknown(currentStack, 1, methodBody, offset);
            StackSlot arrayToLoadFrom = PopUnknown(currentStack, 1, methodBody, offset);
            if (arrayToLoadFrom.Value.AsSingleValue() is not ArrayValue arr)
            {
                PushUnknown(currentStack);
                return;
            }
            // We don't yet handle arrays of references or pointers
            bool isByRef = opcode == ILOpcode.ldelema;

            int? index = indexToLoadFrom.Value.AsConstInt();
            if (index == null)
            {
                PushUnknown(currentStack);
                if (isByRef)
                {
                    MarkArrayValuesAsUnknown(arr, curBasicBlock);
                }
                return;
            }
            // Don't try to track refs to array elements. Set it as unknown, then push unknown to the stack
            else if (isByRef)
            {
                arr.IndexValues[index.Value] = new ValueBasicBlockPair(UnknownValue.Instance, curBasicBlock);
                PushUnknown(currentStack);
            }
            else if (arr.IndexValues.TryGetValue(index.Value, out ValueBasicBlockPair arrayIndexValue))
                currentStack.Push(new StackSlot(arrayIndexValue.Value));
            else
                PushUnknown(currentStack);
        }
    }
}
