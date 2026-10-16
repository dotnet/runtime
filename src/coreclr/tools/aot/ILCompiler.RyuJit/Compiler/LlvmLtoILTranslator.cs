// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

using Internal.IL;
using Internal.JitInterface;
using Internal.TypeSystem;
using static ILCompiler.LlvmLtoWriter;

namespace ILCompiler
{
    internal sealed class LlvmLtoILTranslator
    {
        private enum MathIntrinsic
        {
            Abs,
            Sqrt,
            Floor,
            Ceiling,
            Truncate,
            CopySign
        }

        private sealed record Value(
            ScalarType Type,
            string Operand = "",
            ulong? Constant = null,
            MethodDesc Function = null,
            ScalarType? AddressType = null,
            bool Initialized = true,
            string AddressSlot = null);

        private sealed record Instruction(int Offset, int Next, ILOpcode Op, long Operand, int[] Targets);

        private sealed class State(Value[] arguments, Value[] locals)
        {
            public Value[] Arguments = arguments;
            public Value[] Locals = locals;
            public List<Value> Stack = new();
            public State Clone() => new((Value[])Arguments.Clone(), (Value[])Locals.Clone()) { Stack = new(Stack) };
        }

        private sealed class Block(int offset)
        {
            public int Offset = offset;
            public List<Instruction> Instructions = new();
            public State Entry;
        }

        private readonly LlvmLtoWriter _writer;
        private readonly MethodIL _il;
        private readonly ScalarType[] _locals;
        private readonly int _pointerBits;
        private readonly bool _unmanagedEntry;
        private readonly bool _nativeBody;
        private readonly SortedDictionary<int, Block> _blocks = new();
        // Null during analysis; allocated for the emission pass.
        private StringBuilder _body;
        private int _nextValue;

        internal ScalarType ReturnType { get; }
        internal ScalarType[] Parameters { get; }
        internal HashSet<MethodDesc> References { get; } = new();
        internal HashSet<string> NativeMathHelpers { get; } = new(StringComparer.Ordinal);

        internal LlvmLtoILTranslator(LlvmLtoWriter writer, MethodDesc method, MethodIL il, bool nativeBody = false)
        {
            _writer = writer;
            _il = il;
            _pointerBits = method.Context.Target.PointerSize * 8;
            _unmanagedEntry = method.IsUnmanagedCallersOnly;
            _nativeBody = nativeBody;
            ReturnType = ScalarType.From(method.Signature.ReturnType, allowVoid: true);
            Parameters = Enumerable.Range(0, method.Signature.Length).Select(i => ScalarType.From(method.Signature[i])).ToArray();
            _locals = il.GetLocals().Select(local => !local.IsPinned ? ScalarType.From(local.Type, allowByRef: true) : throw new UnsupportedException("Pinned local")).ToArray();
        }

        internal string Translate()
        {
            Decode();

            var initial = new State(Parameters.Select(t => new Value(t.StackType)).ToArray(),
                _locals.Select(t => new Value(t.StackType, Constant: t.Kind == ScalarKind.Float ? null : 0, Initialized: _il.IsInitLocals)).ToArray());
            _blocks[0].Entry = initial;

            foreach (Block block in _blocks.Values)
                if (block.Entry != null)
                    Process(block, block.Entry.Clone());

            _body = new StringBuilder();
            _nextValue = 0;
            _body.AppendLine("entry:");
            if (_unmanagedEntry)
            {
                _body.AppendLine($"  %reversePInvokeFrame = alloca [{CorInfoImpl.ReversePInvokeTransitionFramePointerCount} x ptr], align {_pointerBits / 8}");
                _body.AppendLine($"  call void {_writer.ReversePInvokeEnter}(ptr %reversePInvokeFrame)");
            }
            for (int i = 0; i < Parameters.Length; i++)
            {
                _body.AppendLine($"  %a{i} = alloca {Parameters[i].IR}");
                Value argument = Convert(new(_unmanagedEntry ? Parameters[i] : Parameters[i].AbiType, $"%arg{i}"), Parameters[i]);
                _body.AppendLine($"  store {Parameters[i].IR} {argument.Operand}, ptr %a{i}");
            }
            for (int i = 0; i < _locals.Length; i++)
            {
                _body.AppendLine($"  %l{i} = alloca {_locals[i].IR}");
                if (_il.IsInitLocals)
                    _body.AppendLine($"  store {_locals[i].IR} zeroinitializer, ptr %l{i}");
            }
            foreach (Block block in _blocks.Values)
                if (block.Entry != null)
                    for (int i = 0; i < block.Entry.Stack.Count; i++)
                        _body.AppendLine($"  %s{block.Offset}_{i} = alloca {block.Entry.Stack[i].Type.IR}");
            _body.AppendLine("  br label %b0");
            foreach (Block block in _blocks.Values)
            {
                if (block.Entry == null)
                    continue;
                State state = block.Entry.Clone();
                _body.AppendLine($"b{block.Offset}:");
                for (int i = 0; i < state.Stack.Count; i++)
                {
                    Value value = state.Stack[i];
                    state.Stack[i] = value with { Operand = Emit(value.Type, $"load {value.Type.IR}, ptr %s{block.Offset}_{i}").Operand };
                }
                Process(block, state);
            }
            return _body.ToString();
        }

        private void Decode()
        {
            var instructions = new List<Instruction>();
            var leaders = new SortedSet<int> { 0 };
            var reader = new ILReader(_il.GetILBytes());
            if (reader.Size <= 0)
                throw new UnsupportedException("Empty method");
            while (reader.HasNext)
            {
                int offset = reader.Offset;
                ILOpcode op = reader.ReadILOpcode();
                long operand = 0;
                int[] targets = [];
                if (op >= ILOpcode.br_s && op <= ILOpcode.blt_un)
                {
                    targets = [reader.ReadBranchDestination(op)];
                    op = ILScalarSemantics.NormalizeBranch(op);
                }
                else if (op == ILOpcode.switch_)
                {
                    uint count = reader.ReadILUInt32();
                    if (count > (reader.Size - reader.Offset) / 4)
                        throw new UnsupportedException("Invalid switch table");
                    int end = reader.Offset + (int)count * 4;
                    targets = new int[count];
                    for (int i = 0; i < targets.Length; i++)
                        targets[i] = end + (int)reader.ReadILUInt32();
                }
                else if (op is ILOpcode.ldarg_s or ILOpcode.starg_s or ILOpcode.ldarga_s or ILOpcode.ldloc_s or ILOpcode.stloc_s or ILOpcode.ldloca_s)
                    operand = reader.ReadILByte();
                else if (op is ILOpcode.ldarg or ILOpcode.starg or ILOpcode.ldarga or ILOpcode.ldloc or ILOpcode.stloc or ILOpcode.ldloca)
                    operand = reader.ReadILUInt16();
                else if (op == ILOpcode.ldc_i4_s)
                    operand = (sbyte)reader.ReadILByte();
                else if (op is ILOpcode.ldc_i4 or ILOpcode.ldc_r4 or ILOpcode.call or ILOpcode.calli or ILOpcode.ldftn or ILOpcode.sizeof_ or ILOpcode.ldobj or ILOpcode.stobj or ILOpcode.initobj or ILOpcode.cpobj)
                    operand = reader.ReadILUInt32();
                else if (op is ILOpcode.ldc_i8 or ILOpcode.ldc_r8)
                    operand = (long)reader.ReadILUInt64();
                else
                    reader.Skip(op);
                if (reader.Offset > reader.Size)
                    throw new UnsupportedException("Truncated IL instruction");
                foreach (int target in targets)
                {
                    if (target <= offset || target >= reader.Size)
                        throw new UnsupportedException("Backedge or invalid branch: managed safepoints are not modelled");
                    leaders.Add(target);
                }
                if ((targets.Length != 0 || op is ILOpcode.ret or ILOpcode.switch_) && reader.Offset < reader.Size)
                    leaders.Add(reader.Offset);
                instructions.Add(new(offset, reader.Offset, op, operand, targets));
            }
            var boundaries = instructions.Select(i => i.Offset).ToHashSet();
            foreach (int leader in leaders)
            {
                if (!boundaries.Contains(leader))
                    throw new UnsupportedException("Branch into an IL operand");
                _blocks.Add(leader, new Block(leader));
            }
            Block current = _blocks[0];
            foreach (Instruction instruction in instructions)
            {
                if (_blocks.TryGetValue(instruction.Offset, out Block next))
                    current = next;
                current.Instructions.Add(instruction);
            }
        }

        private static Value MergeValue(Value left, Value right)
        {
            ScalarType type = left.Type;
            if (type != right.Type)
            {
                if (type.Kind != ScalarKind.Float || right.Type.Kind != ScalarKind.Float)
                    throw new UnsupportedException("Incompatible stack merge");
                type = new(ScalarKind.Float, Math.Max(type.Bits, right.Type.Bits));
            }
            return new(type, Constant: left.Constant == right.Constant ? left.Constant : null,
                Function: left.Function == right.Function ? left.Function : null,
                AddressType: left.AddressType == right.AddressType ? left.AddressType : null,
                Initialized: left.Initialized && right.Initialized,
                AddressSlot: left.AddressSlot == right.AddressSlot ? left.AddressSlot : null);
        }

        private void Transfer(State state, int target)
        {
            if (!_blocks.TryGetValue(target, out Block block))
                throw new UnsupportedException("Fallthrough beyond the method");
            if (_body == null)
            {
                if (block.Entry == null)
                    block.Entry = state.Clone();
                else
                {
                    if (block.Entry.Stack.Count != state.Stack.Count)
                        throw new UnsupportedException("Incompatible stack height");
                    for (int i = 0; i < state.Stack.Count; i++)
                        block.Entry.Stack[i] = MergeValue(block.Entry.Stack[i], state.Stack[i]);
                    for (int i = 0; i < state.Locals.Length; i++)
                        block.Entry.Locals[i] = MergeValue(block.Entry.Locals[i], state.Locals[i]);
                    for (int i = 0; i < state.Arguments.Length; i++)
                        block.Entry.Arguments[i] = MergeValue(block.Entry.Arguments[i], state.Arguments[i]);
                }
            }
            else
            {
                for (int i = 0; i < state.Stack.Count; i++)
                {
                    Value value = Convert(state.Stack[i], block.Entry.Stack[i].Type);
                    _body.AppendLine($"  store {value.Type.IR} {value.Operand}, ptr %s{target}_{i}");
                }
            }
        }

        private Value Emit(ScalarType type, string instruction, ulong? constant = null)
        {
            string name = $"%v{_nextValue++}";
            _body?.AppendLine($"  {name} = {instruction}");
            return new(type, name, constant);
        }

        private static Value Integer(ScalarType type, ulong value)
        {
            value &= ILScalarSemantics.Mask(type.Bits);
            return new(type, ILScalarSemantics.Signed(value, type.Bits).ToString(CultureInfo.InvariantCulture), value);
        }

        private Value Convert(Value value, ScalarType target, bool signed = true)
        {
            if (value.Type.IR == target.IR)
                return value with { Type = target };
            if (value.Type.Kind == ScalarKind.Pointer && target.IsInteger)
                return Emit(target, $"ptrtoint ptr {value.Operand} to {target.IR}") with { Function = value.Function, AddressType = value.AddressType, AddressSlot = value.AddressSlot };
            if (value.Type.IsInteger && target.Kind == ScalarKind.Pointer)
                return Emit(target, $"inttoptr {value.Type.IR} {value.Operand} to ptr") with { Function = value.Function, AddressType = value.AddressType, AddressSlot = value.AddressSlot };
            if (value.Type.IsInteger && target.IsInteger)
            {
                string operation = target.Bits < value.Type.Bits ? "trunc" : signed ? "sext" : "zext";
                ulong? constant = value.Constant.HasValue ? (signed ? (ulong)ILScalarSemantics.Signed(value.Constant.Value, value.Type.Bits) : value.Constant.Value) & ILScalarSemantics.Mask(target.Bits) : null;
                return Emit(target, $"{operation} {value.Type.IR} {value.Operand} to {target.IR}", constant);
            }
            if (value.Type.Kind == ScalarKind.Float && target.Kind == ScalarKind.Float)
                return Emit(target, $"{(target.Bits < value.Type.Bits ? "fptrunc" : "fpext")} {value.Type.IR} {value.Operand} to {target.IR}");
            throw new UnsupportedException("Incompatible scalar conversion");
        }

        private Value Load(ScalarType storage, string slot, Value description)
        {
            if (!description.Initialized)
                throw new UnsupportedException("Read of a possibly uninitialized local");
            Value value = Emit(storage, $"load {storage.IR}, ptr {slot}");
            value = Convert(value, storage.StackType, !storage.Unsigned);
            return value with { Constant = description.Constant, Function = description.Function, AddressType = description.AddressType, AddressSlot = description.AddressSlot };
        }

        private Value Store(ScalarType storage, string slot, Value value)
        {
            Value stored = Convert(value, storage);
            _body?.AppendLine($"  store {storage.IR} {stored.Operand}, ptr {slot}");
            return Convert(stored, storage.StackType, !storage.Unsigned);
        }

        private static Value Pop(State state)
        {
            if (state.Stack.Count == 0)
                throw new UnsupportedException("Invalid evaluation stack");
            Value value = state.Stack[^1];
            state.Stack.RemoveAt(state.Stack.Count - 1);
            return value;
        }

        private void Push(State state, Value value)
        {
            state.Stack.Add(value);
            if (state.Stack.Count > _il.MaxStack)
                throw new UnsupportedException("Evaluation stack exceeds maxstack");
        }

        // Instruction execution is shared between the analysis and emission passes.
        private void Process(Block block, State state)
        {
            foreach (Instruction instruction in block.Instructions)
            {
                if (Execute(instruction, state))
                    return;
            }
            int next = block.Instructions[^1].Next;
            Transfer(state, next);
            _body?.AppendLine($"  br label %b{next}");
        }

        private bool Execute(Instruction instruction, State state)
        {
            ILOpcode op = instruction.Op;
            if (op == ILOpcode.nop)
                return false;
            if (op >= ILOpcode.ldarg_0 && op <= ILOpcode.ldarg_3 || op is ILOpcode.ldarg or ILOpcode.ldarg_s or ILOpcode.starg or ILOpcode.starg_s or ILOpcode.ldarga or ILOpcode.ldarga_s)
            {
                int index = op >= ILOpcode.ldarg_0 && op <= ILOpcode.ldarg_3 ? op - ILOpcode.ldarg_0 : (int)instruction.Operand;
                if ((uint)index >= (uint)Parameters.Length)
                    throw new UnsupportedException("Invalid argument index");
                string slot = $"%a{index}";
                if (op is ILOpcode.starg or ILOpcode.starg_s)
                    state.Arguments[index] = Store(Parameters[index], slot, Pop(state));
                else if (op is ILOpcode.ldarga or ILOpcode.ldarga_s)
                    Push(state, Address(Parameters[index], slot));
                else
                    Push(state, Load(Parameters[index], slot, state.Arguments[index]));
                return false;
            }
            if (op >= ILOpcode.ldloc_0 && op <= ILOpcode.stloc_3 || op is ILOpcode.ldloc or ILOpcode.ldloc_s or ILOpcode.stloc or ILOpcode.stloc_s or ILOpcode.ldloca or ILOpcode.ldloca_s)
            {
                bool store = op >= ILOpcode.stloc_0 && op <= ILOpcode.stloc_3 || op is ILOpcode.stloc or ILOpcode.stloc_s;
                int index = op >= ILOpcode.ldloc_0 && op <= ILOpcode.stloc_3 ? op - (store ? ILOpcode.stloc_0 : ILOpcode.ldloc_0) : (int)instruction.Operand;
                if ((uint)index >= (uint)_locals.Length)
                    throw new UnsupportedException("Invalid local index");
                string slot = $"%l{index}";
                if (store)
                    state.Locals[index] = Store(_locals[index], slot, Pop(state));
                else if (op is ILOpcode.ldloca or ILOpcode.ldloca_s)
                    Push(state, Address(_locals[index], slot));
                else
                    Push(state, Load(_locals[index], slot, state.Locals[index]));
                return false;
            }
            if (op >= ILOpcode.ldc_i4_m1 && op <= ILOpcode.ldc_i4_8 || op is ILOpcode.ldc_i4 or ILOpcode.ldc_i4_s or ILOpcode.ldc_i8)
            {
                long value = op >= ILOpcode.ldc_i4_m1 && op <= ILOpcode.ldc_i4_8 ? op - ILOpcode.ldc_i4_0 : instruction.Operand;
                Push(state, Integer(new(ScalarKind.Integer, op == ILOpcode.ldc_i8 ? 64 : 32), (ulong)value));
                return false;
            }
            if (op is ILOpcode.ldc_r4 or ILOpcode.ldc_r8)
            {
                int bits = op == ILOpcode.ldc_r4 ? 32 : 64;
                // Bitcast constants retain negative zero and NaN payloads.
                ulong value = (ulong)instruction.Operand & ILScalarSemantics.Mask(bits);
                Push(state, Emit(new(ScalarKind.Float, bits), $"bitcast i{bits} {value.ToString(CultureInfo.InvariantCulture)} to {(bits == 32 ? "float" : "double")}"));
                return false;
            }
            if (op == ILOpcode.sizeof_)
            {
                TypeDesc type = (TypeDesc)_il.GetObject((int)instruction.Operand);
                Push(state, Integer(new(ScalarKind.Integer, 32), (uint)type.GetElementSize().AsInt));
                return false;
            }
            if (op is ILOpcode.dup or ILOpcode.pop)
            {
                Value value = Pop(state);
                if (op == ILOpcode.dup)
                {
                    Push(state, value);
                    Push(state, value);
                }
                return false;
            }
            if (op == ILOpcode.ret)
            {
                if (ReturnType.Kind == ScalarKind.Void)
                {
                    if (_unmanagedEntry)
                        _body?.AppendLine($"  call void {_writer.ReversePInvokeExit}(ptr %reversePInvokeFrame)");
                    _body?.AppendLine("  ret void");
                }
                else
                {
                    Value value = Pop(state);
                    if (value.AddressType != null)
                        throw new UnsupportedException("Local address escapes the method");
                    value = Convert(value, ReturnType);
                    ScalarType returnType = _unmanagedEntry ? ReturnType : ReturnType.AbiType;
                    value = Convert(value, returnType, !ReturnType.Unsigned);
                    if (_unmanagedEntry)
                        _body?.AppendLine($"  call void {_writer.ReversePInvokeExit}(ptr %reversePInvokeFrame)");
                    _body?.AppendLine($"  ret {returnType.IR} {value.Operand}");
                }
                if (state.Stack.Count != 0)
                    throw new UnsupportedException("Invalid return stack");
                return true;
            }
            if (op >= ILOpcode.br_s && op <= ILOpcode.blt_un_s)
            {
                if (op == ILOpcode.br_s)
                {
                    Transfer(state, instruction.Targets[0]);
                    _body?.AppendLine($"  br label %b{instruction.Targets[0]}");
                }
                else
                {
                    Value condition;
                    if (op is ILOpcode.brtrue_s or ILOpcode.brfalse_s)
                    {
                        Value value = Pop(state);
                        if (!value.Type.IsInteger)
                            throw new UnsupportedException("Invalid branch condition");
                        condition = Emit(new(ScalarKind.Integer, 1), $"icmp {(op == ILOpcode.brtrue_s ? "ne" : "eq")} {value.Type.IR} {value.Operand}, 0");
                    }
                    else
                    {
                        Value right = Pop(state), left = Pop(state);
                        condition = Compare(op, left, right);
                    }
                    Transfer(state, instruction.Targets[0]);
                    Transfer(state, instruction.Next);
                    _body?.AppendLine($"  br i1 {condition.Operand}, label %b{instruction.Targets[0]}, label %b{instruction.Next}");
                }
                return true;
            }
            if (op == ILOpcode.switch_)
            {
                Value value = Pop(state);
                if (value.Type != new ScalarType(ScalarKind.Integer, 32))
                    throw new UnsupportedException("Switch needs int32");
                foreach (int target in instruction.Targets.Distinct())
                    Transfer(state, target);
                Transfer(state, instruction.Next);
                _body?.AppendLine($"  switch i32 {value.Operand}, label %b{instruction.Next} [");
                for (int i = 0; i < instruction.Targets.Length; i++)
                    _body?.AppendLine($"    i32 {i}, label %b{instruction.Targets[i]}");
                _body?.AppendLine("  ]");
                return true;
            }
            if (op is ILOpcode.ceq or ILOpcode.cgt or ILOpcode.cgt_un or ILOpcode.clt or ILOpcode.clt_un)
            {
                Value right = Pop(state), left = Pop(state);
                Push(state, Convert(Compare(op, left, right), new(ScalarKind.Integer, 32), false));
                return false;
            }
            if (op is ILOpcode.neg or ILOpcode.not)
            {
                Value value = Pop(state);
                if (value.Type.Kind == ScalarKind.Float)
                {
                    if (op != ILOpcode.neg)
                        throw new UnsupportedException("Bitwise operation on floating point");
                    Push(state, Emit(value.Type, $"fneg {value.Type.IR} {value.Operand}"));
                }
                else
                {
                    if (!value.Type.IsInteger)
                        throw new UnsupportedException("Invalid unary operand");
                    ulong? constant = value.Constant.HasValue ? (op == ILOpcode.neg ? unchecked(0UL - value.Constant.Value) : ~value.Constant.Value) & ILScalarSemantics.Mask(value.Type.Bits) : null;
                    Push(state, Emit(value.Type, op == ILOpcode.neg ? $"sub {value.Type.IR} 0, {value.Operand}" : $"xor {value.Type.IR} {value.Operand}, -1", constant));
                }
                return false;
            }
            if (op is ILOpcode.conv_i or ILOpcode.conv_u or ILOpcode.conv_i1 or ILOpcode.conv_u1 or ILOpcode.conv_i2 or ILOpcode.conv_u2 or ILOpcode.conv_i4 or ILOpcode.conv_u4 or ILOpcode.conv_i8 or ILOpcode.conv_u8 or ILOpcode.conv_r4 or ILOpcode.conv_r8 or ILOpcode.conv_r_un)
            {
                Push(state, Conversion(op, Pop(state)));
                return false;
            }
            if (op == ILOpcode.ldftn)
            {
                MethodDesc target = (MethodDesc)_il.GetObject((int)instruction.Operand);
                string name = _writer.GetAddress(target);
                References.Add(target);
                Push(state, Emit(new(ScalarKind.NativeInt, _pointerBits), $"ptrtoint ptr {Quote(name)} to i{_pointerBits}") with { Function = target });
                return false;
            }
            if (op is ILOpcode.call or ILOpcode.calli)
            {
                MethodDesc target;
                MethodSignature signature;
                if (op == ILOpcode.calli)
                {
                    Value pointer = Pop(state);
                    if (pointer.Function == null)
                        throw new UnsupportedException("Unknown indirect target could require EH, GC, or a transition");
                    target = pointer.Function;
                    signature = (MethodSignature)_il.GetObject((int)instruction.Operand);
                    if (signature.Flags != MethodSignatureFlags.Static)
                        throw new UnsupportedException("Unmanaged calli transition");
                }
                else
                {
                    target = (MethodDesc)_il.GetObject((int)instruction.Operand);
                    signature = target.Signature;
                    if (TryMathIntrinsic(target, state))
                        return false;
                }
                Export callee = _writer.GetCallee(target, _nativeBody);
                if (signature.Length != callee.Parameters.Length || ScalarType.From(signature.ReturnType, allowVoid: true).IR != callee.ReturnType.IR)
                    throw new UnsupportedException("Indirect signature mismatch");
                var arguments = new Value[callee.Parameters.Length];
                for (int i = arguments.Length - 1; i >= 0; i--)
                {
                    if (ScalarType.From(signature[i]).IR != callee.Parameters[i].IR)
                        throw new UnsupportedException("Indirect parameter mismatch");
                    Value value = Pop(state);
                    if (value.AddressType != null)
                        throw new UnsupportedException("Local address passed to another method");
                    value = Convert(value, callee.Parameters[i]);
                    value = Convert(value, callee.Parameters[i].AbiType, !callee.Parameters[i].Unsigned);
                    arguments[i] = value;
                }
                References.Add(target);
                // RyuJIT may have inlined the callee and exposed its references here.
                References.UnionWith(callee.References);
                NativeMathHelpers.UnionWith(callee.NativeMathHelpers);
                string call = $"call {callee.ReturnType.AbiType.IR} {Quote(callee.Symbol)}({string.Join(", ", arguments.Select(value => $"{value.Type.IR} {value.Operand}"))})";
                if (callee.ReturnType.Kind == ScalarKind.Void)
                    _body?.AppendLine($"  {call}");
                else
                    Push(state, Convert(Convert(Emit(callee.ReturnType.AbiType, call), callee.ReturnType), callee.ReturnType.StackType, !callee.ReturnType.Unsigned));
                return false;
            }
            if (op >= ILOpcode.ldind_i1 && op <= ILOpcode.stind_r8 || op is ILOpcode.stind_i or ILOpcode.ldobj or ILOpcode.stobj or ILOpcode.initobj or ILOpcode.cpobj)
            {
                Memory(instruction, state);
                return false;
            }
            if (op is ILOpcode.add or ILOpcode.sub or ILOpcode.mul or ILOpcode.div or ILOpcode.div_un or ILOpcode.rem or ILOpcode.rem_un or ILOpcode.and or ILOpcode.or or ILOpcode.xor or ILOpcode.shl or ILOpcode.shr or ILOpcode.shr_un)
            {
                Value right = Pop(state), left = Pop(state);
                Push(state, Binary(op, left, right));
                return false;
            }
            throw new UnsupportedException($"Unsupported IL opcode: {op}");
        }

        private Value Address(ScalarType type, string slot)
            => Emit(new(ScalarKind.NativeInt, _pointerBits), $"ptrtoint ptr {slot} to i{_pointerBits}") with { AddressType = type, AddressSlot = slot };

        private bool TryMathIntrinsic(MethodDesc method, State state)
        {
            if (method.OwningType is not MetadataType type || type.Module != method.Context.SystemModule ||
                type.Namespace != "System"u8 || (type.Name != "Math"u8 && type.Name != "MathF"u8) ||
                method.Signature.Flags != MethodSignatureFlags.Static)
                return false;
            MathIntrinsic? intrinsic = method.GetName() switch
            {
                "Abs" => MathIntrinsic.Abs,
                "Sqrt" => MathIntrinsic.Sqrt,
                "Floor" => MathIntrinsic.Floor,
                "Ceiling" => MathIntrinsic.Ceiling,
                "Truncate" => MathIntrinsic.Truncate,
                "CopySign" => MathIntrinsic.CopySign,
                _ => null,
            };
            if (intrinsic == null || method.Signature.ReturnType.Category is not (TypeFlags.Single or TypeFlags.Double))
                return false;
            // CoreLib's CopySign is a managed bit-manipulation implementation;
            // unlike the other recognized APIs it is not marked [Intrinsic].
            if (!method.IsIntrinsic && intrinsic != MathIntrinsic.CopySign)
                return false;
            ScalarType resultType = ScalarType.From(method.Signature.ReturnType);
            int count = intrinsic == MathIntrinsic.CopySign ? 2 : 1;
            if (method.Signature.Length != count)
                throw new UnsupportedException("Math intrinsic signature mismatch");
            string[] operands = new string[count];
            for (int i = count - 1; i >= 0; i--)
            {
                if (ScalarType.From(method.Signature[i]) != resultType)
                    throw new UnsupportedException("Math intrinsic argument mismatch");
                Value value = Convert(Pop(state), resultType);
                operands[i] = $"{resultType.IR} {value.Operand}";
            }
            string operation = intrinsic.Value switch
            {
                MathIntrinsic.Abs => "fabs",
                MathIntrinsic.Sqrt => "sqrt",
                MathIntrinsic.Floor => "floor",
                MathIntrinsic.Ceiling => "ceil",
                MathIntrinsic.Truncate => "trunc",
                MathIntrinsic.CopySign => "copysign",
                _ => throw new UnsupportedException("Unsupported math intrinsic"),
            };
            string name = $"llvm.{operation}.f{resultType.Bits}";
            _writer.Intrinsics[name] = $"declare {resultType.IR} @{name}({string.Join(", ", Enumerable.Repeat(resultType.IR, count))})";
            Push(state, Emit(resultType, $"call {resultType.IR} @{name}({string.Join(", ", operands)})"));
            References.Add(method);
            NativeMathHelpers.Add(operation + (resultType.Bits == 32 ? "f" : ""));
            return true;
        }

        private void Promote(ref Value left, ref Value right)
        {
            if (left.Type.Kind == ScalarKind.Float && right.Type.Kind == ScalarKind.Float)
            {
                var common = new ScalarType(ScalarKind.Float, Math.Max(left.Type.Bits, right.Type.Bits));
                left = Convert(left, common);
                right = Convert(right, common);
            }
            else if (left.Type.Kind == ScalarKind.NativeInt && right.Type == new ScalarType(ScalarKind.Integer, 32))
                right = Convert(right, left.Type);
            else if (right.Type.Kind == ScalarKind.NativeInt && left.Type == new ScalarType(ScalarKind.Integer, 32))
                left = Convert(left, right.Type);
            if (left.Type != right.Type)
                throw new UnsupportedException("Incompatible operand types");
        }

        private Value Compare(ILOpcode op, Value left, Value right)
        {
            Promote(ref left, ref right);
            bool floating = left.Type.Kind == ScalarKind.Float;
            if (!floating && !left.Type.IsInteger)
                throw new UnsupportedException("Invalid comparison");
            string predicate = floating ? ILScalarSemantics.FloatPredicate(op) : ILScalarSemantics.IntegerPredicate(op);
            ulong? constant = left.Constant.HasValue && right.Constant.HasValue ?
                (ILScalarSemantics.Compare(op, left.Type.Bits, left.Constant.Value, right.Constant.Value) ? 1UL : 0UL) : null;
            return Emit(new(ScalarKind.Integer, 1), $"{(floating ? "fcmp" : "icmp")} {predicate} {left.Type.IR} {left.Operand}, {right.Operand}", constant);
        }

        private Value Binary(ILOpcode op, Value left, Value right)
        {
            if (op is ILOpcode.shl or ILOpcode.shr or ILOpcode.shr_un)
            {
                if (!left.Type.IsInteger || !right.Type.IsInteger || (right.Type.Bits != 32 && right.Type.Kind != ScalarKind.NativeInt))
                    throw new UnsupportedException("Invalid shift");
                right = Convert(right, left.Type, false);
                right = Emit(right.Type, $"and {right.Type.IR} {right.Operand}, {left.Type.Bits - 1}", right.Constant & (ulong)(left.Type.Bits - 1));
            }
            else
                Promote(ref left, ref right);
            bool floating = left.Type.Kind == ScalarKind.Float;
            string operation;
            if (floating)
                operation = op switch
                {
                    ILOpcode.add => "fadd",
                    ILOpcode.sub => "fsub",
                    ILOpcode.mul => "fmul",
                    ILOpcode.div => "fdiv",
                    ILOpcode.rem => "frem",
                    _ => throw new UnsupportedException("Invalid floating operation"),
                };
            else
            {
                if (!left.Type.IsInteger || !right.Type.IsInteger)
                    throw new UnsupportedException("Invalid integer operation");
                if (op is ILOpcode.div or ILOpcode.div_un or ILOpcode.rem or ILOpcode.rem_un &&
                    !ILScalarSemantics.IsDivisionSafe(op, left.Type.Bits, left.Constant, right.Constant))
                    throw new UnsupportedException("Division can throw or overflow");
                operation = op switch
                {
                    ILOpcode.add => "add",
                    ILOpcode.sub => "sub",
                    ILOpcode.mul => "mul",
                    ILOpcode.and => "and",
                    ILOpcode.or => "or",
                    ILOpcode.xor => "xor",
                    ILOpcode.shl => "shl",
                    ILOpcode.shr => "ashr",
                    ILOpcode.shr_un => "lshr",
                    ILOpcode.div => "sdiv",
                    ILOpcode.div_un => "udiv",
                    ILOpcode.rem => "srem",
                    ILOpcode.rem_un => "urem",
                    _ => throw new UnsupportedException("Invalid integer operation"),
                };
            }
            ulong? constant = null;
            if (!floating && left.Constant.HasValue && right.Constant.HasValue &&
                ILScalarSemantics.TryBinary(op, left.Type.Bits, left.Constant.Value, right.Constant.Value, out ulong result))
                constant = result;
            return Emit(left.Type, $"{operation} {left.Type.IR} {left.Operand}, {right.Operand}", constant);
        }

        private Value Conversion(ILOpcode op, Value value)
        {
            bool unsigned = op is ILOpcode.conv_u or ILOpcode.conv_u1 or ILOpcode.conv_u2 or ILOpcode.conv_u4 or ILOpcode.conv_u8 or ILOpcode.conv_r_un;
            if (op is ILOpcode.conv_r4 or ILOpcode.conv_r8 or ILOpcode.conv_r_un)
            {
                var target = new ScalarType(ScalarKind.Float, op == ILOpcode.conv_r4 ? 32 : 64);
                if (value.Type.Kind == ScalarKind.Float)
                    return Convert(value, target);
                if (!value.Type.IsInteger)
                    throw new UnsupportedException("Invalid integer-to-float conversion");
                return Emit(target, $"{(unsigned ? "uitofp" : "sitofp")} {value.Type.IR} {value.Operand} to {target.IR}");
            }
            int bits = op switch
            {
                ILOpcode.conv_i1 or ILOpcode.conv_u1 => 8,
                ILOpcode.conv_i2 or ILOpcode.conv_u2 => 16,
                ILOpcode.conv_i4 or ILOpcode.conv_u4 => 32,
                ILOpcode.conv_i or ILOpcode.conv_u => _pointerBits,
                _ => 64,
            };
            var targetType = new ScalarType(op is ILOpcode.conv_i or ILOpcode.conv_u ? ScalarKind.NativeInt : ScalarKind.Integer, bits);
            if (value.Type.Kind == ScalarKind.Float)
            {
                // RyuJIT saturates to the destination range, including small integers (fgMorphExpandCast). Plain LLVM fptosi can produce poison.
                int saturationBits = bits;
                string name = $"llvm.fpto{(unsigned ? "ui" : "si")}.sat.i{saturationBits}.f{value.Type.Bits}";
                _writer.Intrinsics[name] = $"declare i{saturationBits} @{name}({value.Type.IR})";
                value = Emit(new(ScalarKind.Integer, saturationBits), $"call i{saturationBits} @{name}({value.Type.IR} {value.Operand})");
            }
            value = Convert(value, targetType, !unsigned);
            return Convert(value, targetType.StackType, !unsigned);
        }

        private void Memory(Instruction instruction, State state)
        {
            ILOpcode op = instruction.Op;
            ScalarType storage = op switch
            {
                ILOpcode.ldind_i1 or ILOpcode.stind_i1 => new(ScalarKind.Integer, 8),
                ILOpcode.ldind_u1 => new(ScalarKind.Integer, 8, true),
                ILOpcode.ldind_i2 or ILOpcode.stind_i2 => new(ScalarKind.Integer, 16),
                ILOpcode.ldind_u2 => new(ScalarKind.Integer, 16, true),
                ILOpcode.ldind_i4 or ILOpcode.ldind_u4 or ILOpcode.stind_i4 => new(ScalarKind.Integer, 32),
                ILOpcode.ldind_i8 or ILOpcode.stind_i8 => new(ScalarKind.Integer, 64),
                ILOpcode.ldind_i or ILOpcode.stind_i => new(ScalarKind.NativeInt, _pointerBits),
                ILOpcode.ldind_r4 or ILOpcode.stind_r4 => new(ScalarKind.Float, 32),
                ILOpcode.ldind_r8 or ILOpcode.stind_r8 => new(ScalarKind.Float, 64),
                ILOpcode.ldobj or ILOpcode.stobj or ILOpcode.initobj or ILOpcode.cpobj => ScalarType.From((TypeDesc)_il.GetObject((int)instruction.Operand)),
                _ => throw new UnsupportedException("Managed reference memory operation"),
            };
            bool store = op >= ILOpcode.stind_i1 && op <= ILOpcode.stind_r8 || op is ILOpcode.stind_i or ILOpcode.stobj or ILOpcode.initobj or ILOpcode.cpobj;
            Value value = null;
            if (op == ILOpcode.initobj)
                value = new(storage, "zeroinitializer", storage.Kind == ScalarKind.Float ? null : 0);
            else if (op == ILOpcode.cpobj)
            {
                Value source = Pop(state);
                var (sourceType, sourceSlot, sourceDescription) = GetLocalAddress(source, storage, state);
                value = Load(sourceType, sourceSlot, sourceDescription);
            }
            else if (store)
                value = Pop(state);
            Value address = Pop(state);
            var (slotType, slot, description) = GetLocalAddress(address, storage, state);
            if (store)
            {
                Value stored = Convert(value, storage);
                _body?.AppendLine($"  store {storage.IR} {stored.Operand}, ptr {slot}, align 1");
                Value newDescription = new(slotType.StackType);
                if (storage.IR == slotType.IR)
                    newDescription = Convert(stored, slotType.StackType, !slotType.Unsigned);
                SetSlot(state, slot, newDescription);
            }
            else
            {
                if (!description.Initialized)
                    throw new UnsupportedException("Read of an uninitialized local through a byref");
                if (storage == slotType)
                    Push(state, Load(storage, slot, description));
                else
                    Push(state, Convert(Emit(storage, $"load {storage.IR}, ptr {slot}, align 1"), storage.StackType, !storage.Unsigned));
            }
        }

        private (ScalarType, string, Value) GetLocalAddress(Value address, ScalarType access, State state)
        {
            if (!address.AddressType.HasValue || address.AddressSlot == null)
                throw new UnsupportedException("Unproven pointer dereference can fault");
            if (address.AddressType.Value.Bits != access.Bits)
                throw new UnsupportedException("Access extends outside a scalar local");
            int index = int.Parse(address.AddressSlot.AsSpan(2), CultureInfo.InvariantCulture);
            bool argument = address.AddressSlot[1] == 'a';
            return (argument ? Parameters[index] : _locals[index], address.AddressSlot, argument ? state.Arguments[index] : state.Locals[index]);
        }

        private static void SetSlot(State state, string slot, Value value)
        {
            int index = int.Parse(slot.AsSpan(2), CultureInfo.InvariantCulture);
            if (slot[1] == 'a')
                state.Arguments[index] = value;
            else
                state.Locals[index] = value;
        }
    }
}
