// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;

using Internal.IL;
using Internal.TypeSystem;

namespace ILCompiler.Dataflow
{
    /// <summary>
    /// Tracks information about the contents of a stack slot
    /// </summary>
    readonly struct StackSlot
    {
        public ValueNode Value { get; }

        /// <summary>
        /// True if the value is on the stack as a byref
        /// </summary>
        public bool IsByRef { get; }

        public StackSlot(ValueNode value, bool isByRef = false)
        {
            Value = value;
            IsByRef = isByRef;
        }
    }

    abstract partial class MethodBodyScanner
    {
        internal ValueNode MethodReturnValue { private set; get; }

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
            StackSlot mergedSlot;
            if (b.Value == null)
            {
                mergedSlot = a;
            }
            else if (a.Value == null)
            {
                mergedSlot = b;
            }
            else
            {
                mergedSlot = new StackSlot(MergePointValue.MergeValues(a.Value, b.Value));
            }

            return mergedSlot;
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

        private static void ClearStack(ref Stack<StackSlot> stack)
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

        private static void StoreMethodLocalValue(
            ValueBasicBlockPair[] valueCollection,
            ValueNode valueToStore,
            int index,
            int curBasicBlock)
        {
            ValueBasicBlockPair newValue = new ValueBasicBlockPair { BasicBlockIndex = curBasicBlock };

            ValueBasicBlockPair existingValue = valueCollection[index];
            if (existingValue.Value != null
                && existingValue.BasicBlockIndex == curBasicBlock)
            {
                // If the previous value was stored in the current basic block, then we can safely 
                // overwrite the previous value with the new one.
                newValue.Value = valueToStore;
            }
            else
            {
                // If the previous value came from a previous basic block, then some other use of 
                // the local could see the previous value, so we must merge the new value with the 
                // old value.
                newValue.Value = MergePointValue.MergeValues(existingValue.Value, valueToStore);
            }
            valueCollection[index] = newValue;
        }

        private static void StoreMethodLocalValue<KeyType>(
            Dictionary<KeyType, ValueBasicBlockPair> valueCollection,
            ValueNode valueToStore,
            KeyType collectionKey,
            int curBasicBlock,
            int? maxTrackedValues = null)
        {
            ValueBasicBlockPair newValue = new ValueBasicBlockPair { BasicBlockIndex = curBasicBlock };

            ValueBasicBlockPair existingValue;
            if (valueCollection.TryGetValue(collectionKey, out existingValue))
            {
                if (existingValue.BasicBlockIndex == curBasicBlock)
                {
                    // If the previous value was stored in the current basic block, then we can safely 
                    // overwrite the previous value with the new one.
                    newValue.Value = valueToStore;
                }
                else
                {
                    // If the previous value came from a previous basic block, then some other use of 
                    // the local could see the previous value, so we must merge the new value with the 
                    // old value.
                    newValue.Value = MergePointValue.MergeValues(existingValue.Value, valueToStore);
                }
                valueCollection[collectionKey] = newValue;
            }
            else if (maxTrackedValues == null || valueCollection.Count < maxTrackedValues)
            {
                // We're not currently tracking a value a this index, so store the value now.
                newValue.Value = valueToStore;
                valueCollection[collectionKey] = newValue;
            }
        }

        public void Scan(MethodIL methodBody)
        {
            MethodDesc thisMethod = methodBody.OwningMethod;

            ValueBasicBlockPair[] locals = new ValueBasicBlockPair[methodBody.GetLocals().Length];

            Dictionary<int, Stack<StackSlot>> knownStacks = new Dictionary<int, Stack<StackSlot>>();
            Stack<StackSlot> currentStack = new Stack<StackSlot>(methodBody.MaxStack);

            ScanExceptionInformation(knownStacks, methodBody);

            BasicBlockIterator blockIterator = new BasicBlockIterator(methodBody);

            MethodReturnValue = null;
            ILReader reader = new ILReader(methodBody.GetILBytes());
            while (reader.HasNext)
            {
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
                    case ILOpcode.ldftn:
                    case ILOpcode.sizeof_:
                    case ILOpcode.ldc_i8:
                    case ILOpcode.ldc_r4:
                    case ILOpcode.ldc_r8:
                        PushUnknown(currentStack);
                        reader.Skip(opcode);
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
                        ScanLdfld(methodBody, offset, opcode, (FieldDesc)methodBody.GetObject(reader.ReadILToken()), currentStack);
                        break;

                    case ILOpcode.newarr:
                        {
                            StackSlot count = PopUnknown(currentStack, 1, methodBody, offset);
                            var arrayElement = (TypeDesc)methodBody.GetObject(reader.ReadILToken());
                            currentStack.Push(new StackSlot(new ArrayValue(count.Value, arrayElement)));
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
                        ScanStfld(methodBody, offset, opcode, (FieldDesc)methodBody.GetObject(reader.ReadILToken()), currentStack);
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
                        ScanIndirectStore(methodBody, offset, currentStack);
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
                        ScanStloc(methodBody, offset, opcode switch {
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
                        HandleCall(methodBody, opcode, offset, (MethodDesc)methodBody.GetObject(reader.ReadILToken()), currentStack, curBasicBlock);
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
                                MethodReturnValue = MergePointValue.MergeValues(MethodReturnValue, retValue.Value);
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

        protected abstract ValueNode GetMethodParameterValue(MethodDesc method, int parameterIndex);

        private void ScanLdarg(ILOpcode opcode, int paramNum, Stack<StackSlot> currentStack, MethodDesc thisMethod)
        {
            bool isByRef;

            if (!thisMethod.Signature.IsStatic && paramNum == 0)
            {
                isByRef = thisMethod.OwningType.IsValueType;
            }
            else
            {
                isByRef = thisMethod.Signature[paramNum - (thisMethod.Signature.IsStatic ? 0 : 1)].IsByRefOrPointer();
            }

            isByRef |= opcode == ILOpcode.ldarga || opcode == ILOpcode.ldarga_s;

            StackSlot slot = new StackSlot(GetMethodParameterValue(thisMethod, paramNum), isByRef);
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
            HandleStoreParameter(methodBody, offset, index, valueToStore.Value);
        }

        private void ScanLdloc(
            MethodIL methodBody,
            int offset,
            ILOpcode operation,
            int index,
            Stack<StackSlot> currentStack,
            ValueBasicBlockPair[] locals)
        {
            bool isByRef = operation == ILOpcode.ldloca || operation == ILOpcode.ldloca_s
                || methodBody.GetLocals()[index].Type.IsByRefOrPointer();

            ValueBasicBlockPair localValue = locals[index];
            if (localValue.Value != null)
            {
                ValueNode valueToPush = localValue.Value;
                currentStack.Push(new StackSlot(valueToPush, isByRef));
            }
            else
            {
                currentStack.Push(new StackSlot(null, isByRef));
            }
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
                    StackSlot slot = new StackSlot(new RuntimeTypeHandleValue(type));
                    currentStack.Push(slot);
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
            ValueBasicBlockPair[] locals,
            int curBasicBlock)
        {
            StackSlot valueToStore = PopUnknown(currentStack, 1, methodBody, offset);
            StoreMethodLocalValue(locals, valueToStore.Value, index, curBasicBlock);
        }

        private void ScanIndirectStore(
            MethodIL methodBody,
            int offset,
            Stack<StackSlot> currentStack)
        {
            StackSlot valueToStore = PopUnknown(currentStack, 1, methodBody, offset);
            StackSlot destination = PopUnknown(currentStack, 1, methodBody, offset);

            foreach (var uniqueDestination in destination.Value.UniqueValues())
            {
                if (uniqueDestination.Kind == ValueNodeKind.LoadField)
                {
                    HandleStoreField(methodBody, offset, ((LoadFieldValue)uniqueDestination).Field, valueToStore.Value);
                }
                else if (uniqueDestination.Kind == ValueNodeKind.MethodParameter)
                {
                    HandleStoreParameter(methodBody, offset, ((MethodParameterValue)uniqueDestination).ParameterIndex, valueToStore.Value);
                }
            }

        }

        protected abstract ValueNode GetFieldValue(MethodIL method, FieldDesc field);

        private void ScanLdfld(
            MethodIL methodBody,
            int offset,
            ILOpcode opcode,
            FieldDesc field,
            Stack<StackSlot> currentStack
            )
        {
            if (opcode == ILOpcode.ldfld || opcode == ILOpcode.ldflda)
                PopUnknown(currentStack, 1, methodBody, offset);

            bool isByRef = opcode == ILOpcode.ldflda || opcode == ILOpcode.ldsflda;

            StackSlot slot = new StackSlot(GetFieldValue(methodBody, field), isByRef);
            currentStack.Push(slot);
        }

        protected virtual void HandleStoreField(MethodIL method, int offset, FieldDesc field, ValueNode valueToStore)
        {
        }

        protected virtual void HandleStoreParameter(MethodIL method, int offset, int index, ValueNode valueToStore)
        {
        }

        private void ScanStfld(
            MethodIL methodBody,
            int offset,
            ILOpcode opcode,
            FieldDesc field,
            Stack<StackSlot> currentStack)
        {
            StackSlot valueToStoreSlot = PopUnknown(currentStack, 1, methodBody, offset);
            if (opcode == ILOpcode.stfld)
                PopUnknown(currentStack, 1, methodBody, offset);

            HandleStoreField(methodBody, offset, field, valueToStoreSlot.Value);
        }

        private ValueNodeList PopCallArguments(
            Stack<StackSlot> currentStack,
            MethodDesc methodCalled,
            MethodIL containingMethodBody,
            bool isNewObj, int ilOffset,
            out ValueNode newObjValue)
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

        private void HandleCall(
            MethodIL callingMethodBody,
            ILOpcode opcode,
            int offset,
            MethodDesc calledMethod,
            Stack<StackSlot> currentStack,
            int curBasicBlock)
        {
            bool isNewObj = opcode == ILOpcode.newobj;

            ValueNode newObjValue;
            ValueNodeList methodParams = PopCallArguments(currentStack, calledMethod, callingMethodBody, isNewObj,
                                                           offset, out newObjValue);

            ValueNode methodReturnValue;
            bool handledFunction = HandleCall(
                callingMethodBody,
                calledMethod,
                opcode,
                offset,
                methodParams,
                out methodReturnValue);

            // Handle the return value or newobj result
            if (!handledFunction)
            {
                if (isNewObj)
                {
                    if (newObjValue == null)
                        PushUnknown(currentStack);
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

            if (methodReturnValue != null)
                currentStack.Push(new StackSlot(methodReturnValue, calledMethod.Signature.ReturnType.IsByRefOrPointer()));

            foreach (var param in methodParams)
            {
                if (param is ArrayValue arr)
                {
                    MarkArrayValuesAsUnknown(arr, curBasicBlock);
                }
            }
        }

        public abstract bool HandleCall(
            MethodIL callingMethodBody,
            MethodDesc calledMethod,
            ILOpcode operation,
            int offset,
            ValueNodeList methodParams,
            out ValueNode methodReturnValue);

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
            foreach (var array in arrayToStoreIn.Value.UniqueValues())
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
            if (arrayToLoadFrom.Value is not ArrayValue arr)
            {
                PushUnknown(currentStack);
                return;
            }
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


            ValueBasicBlockPair arrayIndexValue;
            arr.IndexValues.TryGetValue(index.Value, out arrayIndexValue);
            if (arrayIndexValue.Value != null)
            {
                ValueNode valueToPush = arrayIndexValue.Value;
                currentStack.Push(new StackSlot(valueToPush, isByRef));
            }
            else
            {
                currentStack.Push(new StackSlot(null, isByRef));
            }
        }
    }
}
