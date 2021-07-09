// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

using Internal.IL;
using Internal.TypeSystem;

using Debug = System.Diagnostics.Debug;

namespace Internal.IL.Stubs
{
    public class ILCodeStream
    {
        private const int StartOffsetNotSet = -1;

        private struct LabelAndOffset
        {
            public readonly ILCodeLabel Label;
            public readonly int Offset;
            public LabelAndOffset(ILCodeLabel label, int offset)
            {
                Label = label;
                Offset = offset;
            }
        }

        internal byte[] _instructions;
        internal int _length;
        internal int _startOffsetForLinking;
        internal ArrayBuilder<ILSequencePoint> _sequencePoints;

        private ArrayBuilder<LabelAndOffset> _offsetsNeedingPatching;

        private ILEmitter _emitter;

        internal ILCodeStream(ILEmitter emitter)
        {
            _instructions = Array.Empty<byte>();
            _startOffsetForLinking = StartOffsetNotSet;
            _emitter = emitter;
        }

        internal int RelativeToAbsoluteOffset(int relativeOffset)
        {
            Debug.Assert(_startOffsetForLinking != StartOffsetNotSet);
            return _startOffsetForLinking + relativeOffset;
        }

        private void EmitByte(byte b)
        {
            if (_instructions.Length == _length)
                Array.Resize<byte>(ref _instructions, 2 * _instructions.Length + 10);
            _instructions[_length++] = b;
        }

        private void EmitUInt16(ushort value)
        {
            EmitByte((byte)value);
            EmitByte((byte)(value >> 8));
        }

        private void EmitUInt32(int value)
        {
            EmitByte((byte)value);
            EmitByte((byte)(value >> 8));
            EmitByte((byte)(value >> 16));
            EmitByte((byte)(value >> 24));
        }

        public void Emit(ILOpcode opcode)
        {
            if ((int)opcode > 0x100)
                EmitByte((byte)ILOpcode.prefix1);
            EmitByte((byte)opcode);
        }

        public void Emit(ILOpcode opcode, ILToken token)
        {
            Emit(opcode);
            EmitUInt32((int)token);
        }

        public void EmitLdc(int value)
        {
            if (-1 <= value && value <= 8)
            {
                Emit((ILOpcode)(ILOpcode.ldc_i4_0 + value));
            }
            else if (value == (sbyte)value)
            {
                Emit(ILOpcode.ldc_i4_s);
                EmitByte((byte)value);
            }
            else
            {
                Emit(ILOpcode.ldc_i4);
                EmitUInt32(value);
            }
        }

        public void EmitLdArg(int index)
        {
            if (index < 4)
            {
                Emit((ILOpcode)(ILOpcode.ldarg_0 + index));
            }
            else
            {
                Emit(ILOpcode.ldarg);
                EmitUInt16((ushort)index);
            }
        }

        public void EmitLdArga(int index)
        {
            if (index < 0x100)
            {
                Emit(ILOpcode.ldarga_s);
                EmitByte((byte)index);
            }
            else
            {
                Emit(ILOpcode.ldarga);
                EmitUInt16((ushort)index);
            }
        }

        public void EmitLdLoc(ILLocalVariable variable)
        {
            int index = (int)variable;

            if (index < 4)
            {
                Emit((ILOpcode)(ILOpcode.ldloc_0 + index));
            }
            else if (index < 0x100)
            {
                Emit(ILOpcode.ldloc_s);
                EmitByte((byte)index);
            }
            else
            {
                Emit(ILOpcode.ldloc);
                EmitUInt16((ushort)index);
            }
        }

        public void EmitLdLoca(ILLocalVariable variable)
        {
            int index = (int)variable;

            if (index < 0x100)
            {
                Emit(ILOpcode.ldloca_s);
                EmitByte((byte)index);
            }
            else
            {
                Emit(ILOpcode.ldloca);
                EmitUInt16((ushort)index);
            }
        }

        public void EmitStLoc(ILLocalVariable variable)
        {
            int index = (int)variable;

            if (index < 4)
            {
                Emit((ILOpcode)(ILOpcode.stloc_0 + index));
            }
            else if (index < 0x100)
            {
                Emit(ILOpcode.stloc_s);
                EmitByte((byte)index);
            }
            else
            {
                Emit(ILOpcode.stloc);
                EmitUInt16((ushort)index);
            }
        }

        public void Emit(ILOpcode opcode, ILCodeLabel label)
        {
            Debug.Assert(opcode == ILOpcode.br || opcode == ILOpcode.brfalse ||
                opcode == ILOpcode.brtrue || opcode == ILOpcode.beq ||
                opcode == ILOpcode.bge || opcode == ILOpcode.bgt ||
                opcode == ILOpcode.ble || opcode == ILOpcode.blt ||
                opcode == ILOpcode.bne_un || opcode == ILOpcode.bge_un ||
                opcode == ILOpcode.bgt_un || opcode == ILOpcode.ble_un ||
                opcode == ILOpcode.blt_un || opcode == ILOpcode.leave);

            Emit(opcode);
            _offsetsNeedingPatching.Add(new LabelAndOffset(label, _length));
            EmitUInt32(4);
        }

        public void EmitSwitch(ILCodeLabel[] labels)
        {
            Emit(ILOpcode.switch_);
            EmitUInt32(labels.Length);

            int remainingBytes = labels.Length * 4;
            foreach (var label in labels)
            {
                _offsetsNeedingPatching.Add(new LabelAndOffset(label, _length));
                EmitUInt32(remainingBytes);
                remainingBytes -= 4;
            }
        }

        public void EmitUnaligned()
        {
            Emit(ILOpcode.unaligned);
            EmitByte(1);
        }

        public void EmitLdInd(TypeDesc type)
        {
            switch (type.UnderlyingType.Category)
            {
                case TypeFlags.SByte:
                    Emit(ILOpcode.ldind_i1);
                    break;
                case TypeFlags.Byte:
                case TypeFlags.Boolean:
                    Emit(ILOpcode.ldind_u1);
                    break;
                case TypeFlags.Int16:
                    Emit(ILOpcode.ldind_i2);
                    break;
                case TypeFlags.Char:
                case TypeFlags.UInt16:
                    Emit(ILOpcode.ldind_u2);
                    break;
                case TypeFlags.UInt32:
                    Emit(ILOpcode.ldind_u4);
                    break;
                case TypeFlags.Int32:
                    Emit(ILOpcode.ldind_i4);
                    break;
                case TypeFlags.UInt64:
                case TypeFlags.Int64:
                    Emit(ILOpcode.ldind_i8);
                    break;
                case TypeFlags.Single:
                    Emit(ILOpcode.ldind_r4);
                    break;
                case TypeFlags.Double:
                    Emit(ILOpcode.ldind_r8);
                    break;
                case TypeFlags.IntPtr:
                case TypeFlags.UIntPtr:
                case TypeFlags.Pointer:
                case TypeFlags.FunctionPointer:
                    Emit(ILOpcode.ldind_i);
                    break;
                case TypeFlags.Array:
                case TypeFlags.SzArray:
                case TypeFlags.Class:
                case TypeFlags.Interface:
                    Emit(ILOpcode.ldind_ref);
                    break;
                case TypeFlags.ValueType:
                case TypeFlags.Nullable:
                case TypeFlags.SignatureMethodVariable:
                case TypeFlags.SignatureTypeVariable:
                    Emit(ILOpcode.ldobj, _emitter.NewToken(type));
                    break;
                default:
                    Debug.Fail("Unexpected TypeDesc category");
                    break;
            }
        }
        public void EmitStInd(TypeDesc type)
        {
            switch (type.UnderlyingType.Category)
            {
                case TypeFlags.Byte:
                case TypeFlags.SByte:
                case TypeFlags.Boolean:
                    Emit(ILOpcode.stind_i1);
                    break;
                case TypeFlags.Char:
                case TypeFlags.UInt16:
                case TypeFlags.Int16:
                    Emit(ILOpcode.stind_i2);
                    break;
                case TypeFlags.UInt32:
                case TypeFlags.Int32:
                    Emit(ILOpcode.stind_i4);
                    break;
                case TypeFlags.UInt64:
                case TypeFlags.Int64:
                    Emit(ILOpcode.stind_i8);
                    break;
                case TypeFlags.Single:
                    Emit(ILOpcode.stind_r4);
                    break;
                case TypeFlags.Double:
                    Emit(ILOpcode.stind_r8);
                    break;
                case TypeFlags.IntPtr:
                case TypeFlags.UIntPtr:
                case TypeFlags.Pointer:
                case TypeFlags.FunctionPointer:
                    Emit(ILOpcode.stind_i);
                    break;
                case TypeFlags.Array:
                case TypeFlags.SzArray:
                case TypeFlags.Class:
                case TypeFlags.Interface:
                    Emit(ILOpcode.stind_ref);
                    break;
                case TypeFlags.ValueType:
                case TypeFlags.Nullable:
                    Emit(ILOpcode.stobj, _emitter.NewToken(type));
                    break;
                default:
                    Debug.Fail("Unexpected TypeDesc category");
                    break;
            }
        }

        public void EmitStElem(TypeDesc type)
        {
            switch (type.UnderlyingType.Category)
            {
                case TypeFlags.Byte:
                case TypeFlags.SByte:
                case TypeFlags.Boolean:
                    Emit(ILOpcode.stelem_i1);
                    break;
                case TypeFlags.Char:
                case TypeFlags.UInt16:
                case TypeFlags.Int16:
                    Emit(ILOpcode.stelem_i2);
                    break;
                case TypeFlags.UInt32:
                case TypeFlags.Int32:
                    Emit(ILOpcode.stelem_i4);
                    break;
                case TypeFlags.UInt64:
                case TypeFlags.Int64:
                    Emit(ILOpcode.stelem_i8);
                    break;
                case TypeFlags.Single:
                    Emit(ILOpcode.stelem_r4);
                    break;
                case TypeFlags.Double:
                    Emit(ILOpcode.stelem_r8);
                    break;
                case TypeFlags.IntPtr:
                case TypeFlags.UIntPtr:
                case TypeFlags.Pointer:
                case TypeFlags.FunctionPointer:
                    Emit(ILOpcode.stelem_i);
                    break;
                case TypeFlags.Array:
                case TypeFlags.SzArray:
                case TypeFlags.Class:
                case TypeFlags.Interface:
                    Emit(ILOpcode.stelem_ref);
                    break;
                case TypeFlags.ValueType:
                case TypeFlags.Nullable:
                    Emit(ILOpcode.stelem, _emitter.NewToken(type));
                    break;
                default:
                    Debug.Fail("Unexpected TypeDesc category");
                    break;
            }
        }

        public void EmitLdElem(TypeDesc type)
        {
            switch (type.UnderlyingType.Category)
            {
                case TypeFlags.Byte:
                case TypeFlags.SByte:
                case TypeFlags.Boolean:
                    Emit(ILOpcode.ldelem_i1);
                    break;
                case TypeFlags.Char:
                case TypeFlags.UInt16:
                case TypeFlags.Int16:
                    Emit(ILOpcode.ldelem_i2);
                    break;
                case TypeFlags.UInt32:
                case TypeFlags.Int32:
                    Emit(ILOpcode.ldelem_i4);
                    break;
                case TypeFlags.UInt64:
                case TypeFlags.Int64:
                    Emit(ILOpcode.ldelem_i8);
                    break;
                case TypeFlags.Single:
                    Emit(ILOpcode.ldelem_r4);
                    break;
                case TypeFlags.Double:
                    Emit(ILOpcode.ldelem_r8);
                    break;
                case TypeFlags.IntPtr:
                case TypeFlags.UIntPtr:
                case TypeFlags.Pointer:
                case TypeFlags.FunctionPointer:
                    Emit(ILOpcode.ldelem_i);
                    break;
                case TypeFlags.Array:
                case TypeFlags.SzArray:
                case TypeFlags.Class:
                case TypeFlags.Interface:
                    Emit(ILOpcode.ldelem_ref);
                    break;
                case TypeFlags.ValueType:
                case TypeFlags.Nullable:
                    Emit(ILOpcode.ldelem, _emitter.NewToken(type));
                    break;
                default:
                    Debug.Fail("Unexpected TypeDesc category");
                    break;
            }
        }

        public void EmitLabel(ILCodeLabel label)
        {
            label.Place(this, _length);
        }

        public void BeginTry(ILExceptionRegionBuilder builder)
        {
            Debug.Assert(builder._beginTryStream == null);
            builder._beginTryStream = this;
            builder._beginTryOffset = _length;
        }

        public void EndTry(ILExceptionRegionBuilder builder)
        {
            Debug.Assert(builder._endTryStream == null);
            builder._endTryStream = this;
            builder._endTryOffset = _length;
        }

        public void BeginHandler(ILExceptionRegionBuilder builder)
        {
            Debug.Assert(builder._beginHandlerStream == null);
            builder._beginHandlerStream = this;
            builder._beginHandlerOffset = _length;
        }

        public void EndHandler(ILExceptionRegionBuilder builder)
        {
            Debug.Assert(builder._endHandlerStream == null);
            builder._endHandlerStream = this;
            builder._endHandlerOffset = _length;
        }

        internal void PatchLabels()
        {
            for (int i = 0; i < _offsetsNeedingPatching.Count; i++)
            {
                LabelAndOffset patch = _offsetsNeedingPatching[i];

                Debug.Assert(patch.Label.IsPlaced);
                Debug.Assert(_startOffsetForLinking != StartOffsetNotSet);

                int offset = patch.Offset;

                int delta = _instructions[offset + 3] << 24 |
                    _instructions[offset + 2] << 16 |
                    _instructions[offset + 1] << 8 |
                    _instructions[offset];

                int value = patch.Label.AbsoluteOffset - _startOffsetForLinking - patch.Offset - delta;

                _instructions[offset] = (byte)value;
                _instructions[offset + 1] = (byte)(value >> 8);
                _instructions[offset + 2] = (byte)(value >> 16);
                _instructions[offset + 3] = (byte)(value >> 24);
            }
        }

        public void DefineSequencePoint(string document, int lineNumber)
        {
            // Last sequence point defined for this offset wins.
            if (_sequencePoints.Count > 0 && _sequencePoints[_sequencePoints.Count - 1].Offset == _length)
            {
                _sequencePoints[_sequencePoints.Count - 1] = new ILSequencePoint(_length, document, lineNumber);
            }
            else
            {
                _sequencePoints.Add(new ILSequencePoint(_length, document, lineNumber));
            }
        }
    }

    public class ILExceptionRegionBuilder
    {
        internal ILCodeStream _beginTryStream;
        internal int _beginTryOffset;

        internal ILCodeStream _endTryStream;
        internal int _endTryOffset;

        internal ILCodeStream _beginHandlerStream;
        internal int _beginHandlerOffset;

        internal ILCodeStream _endHandlerStream;
        internal int _endHandlerOffset;

        internal ILExceptionRegionBuilder()
        {
        }

        internal int TryOffset => _beginTryStream.RelativeToAbsoluteOffset(_beginTryOffset);
        internal int TryLength => _endTryStream.RelativeToAbsoluteOffset(_endTryOffset) - TryOffset;
        internal int HandlerOffset => _beginHandlerStream.RelativeToAbsoluteOffset(_beginHandlerOffset);
        internal int HandlerLength => _endHandlerStream.RelativeToAbsoluteOffset(_endHandlerOffset) - HandlerOffset;
        
        internal bool IsDefined =>
            _beginTryStream != null && _endTryStream != null
            && _beginHandlerStream != null && _endHandlerStream != null;
    }

    /// <summary>
    /// Represent a token. Use one of the overloads of <see cref="ILEmitter.NewToken"/>
    /// to create a new token.
    /// </summary>
    public enum ILToken { }

    /// <summary>
    /// Represents a local variable. Use <see cref="ILEmitter.NewLocal"/> to create a new local variable.
    /// </summary>
    public enum ILLocalVariable { }

    public class ILStubMethodIL : MethodIL
    {
        private readonly byte[] _ilBytes;
        private readonly LocalVariableDefinition[] _locals;
        private readonly Object[] _tokens;
        private readonly MethodDesc _method;
        private readonly ILExceptionRegion[] _exceptionRegions;
        private readonly MethodDebugInformation _debugInformation;

        private const int MaxStackNotSet = -1;
        private int _maxStack;

        public ILStubMethodIL(MethodDesc owningMethod, byte[] ilBytes, LocalVariableDefinition[] locals, Object[] tokens, ILExceptionRegion[] exceptionRegions = null, MethodDebugInformation debugInfo = null)
        {
            _ilBytes = ilBytes;
            _locals = locals;
            _tokens = tokens;
            _method = owningMethod;
            _maxStack = MaxStackNotSet;

            if (exceptionRegions == null)
                exceptionRegions = Array.Empty<ILExceptionRegion>();
            _exceptionRegions = exceptionRegions;

            if (debugInfo == null)
                debugInfo = MethodDebugInformation.None;
            _debugInformation = debugInfo;
        }

        public ILStubMethodIL(ILStubMethodIL methodIL)
        {
            _ilBytes = methodIL._ilBytes;
            _locals = methodIL._locals;
            _tokens = methodIL._tokens;
            _method = methodIL._method;
            _debugInformation = methodIL._debugInformation;
            _exceptionRegions = methodIL._exceptionRegions;
            _maxStack = methodIL._maxStack;
        }

        public override MethodDesc OwningMethod
        {
            get
            {
                return _method;
            }
        }

        public override byte[] GetILBytes()
        {
            return _ilBytes;
        }

        public override MethodDebugInformation GetDebugInfo()
        {
            return _debugInformation;
        }

        public override int MaxStack
        {
            get
            {
                if (_maxStack == MaxStackNotSet)
                    _maxStack = this.ComputeMaxStack();
                return _maxStack;
            }
        }

        public override ILExceptionRegion[] GetExceptionRegions()
        {
            return _exceptionRegions;
        }
        public override bool IsInitLocals
        {
            get
            {
                return true;
            }
        }

        public override LocalVariableDefinition[] GetLocals()
        {
            return _locals;
        }
        public override Object GetObject(int token, NotFoundBehavior notFoundBehavior)
        {
            return _tokens[(token & 0xFFFFFF) - 1];
        }
    }

    public class ILCodeLabel
    {
        private ILCodeStream _codeStream;
        private int _offsetWithinCodeStream;

        internal bool IsPlaced
        {
            get
            {
                return _codeStream != null;
            }
        }

        internal int AbsoluteOffset
        {
            get
            {
                Debug.Assert(IsPlaced);
                return _codeStream.RelativeToAbsoluteOffset(_offsetWithinCodeStream);
            }
        }

        internal ILCodeLabel()
        {
        }

        internal void Place(ILCodeStream codeStream, int offsetWithinCodeStream)
        {
            Debug.Assert(!IsPlaced);
            _codeStream = codeStream;
            _offsetWithinCodeStream = offsetWithinCodeStream;
        }
    }

    public class ILEmitter
    {
        private ArrayBuilder<ILCodeStream> _codeStreams;
        private ArrayBuilder<LocalVariableDefinition> _locals;
        private ArrayBuilder<Object> _tokens;
        private ArrayBuilder<ILExceptionRegionBuilder> _finallyRegions;

        public ILEmitter()
        {
        }

        public ILCodeStream NewCodeStream()
        {
            ILCodeStream stream = new ILCodeStream(this);
            _codeStreams.Add(stream);
            return stream;
        }

        private ILToken NewToken(Object value, int tokenType)
        {
            Debug.Assert(value != null);
            _tokens.Add(value);
            return (ILToken)(_tokens.Count | tokenType);
        }

        public ILToken NewToken(TypeDesc value)
        {
            return NewToken(value, 0x01000000);
        }

        public ILToken NewToken(MethodDesc value)
        {
            return NewToken(value, 0x0a000000);
        }

        public ILToken NewToken(FieldDesc value)
        {
            return NewToken(value, 0x0a000000);
        }

        public ILToken NewToken(string value)
        {
            return NewToken(value, 0x70000000);
        }

        public ILToken NewToken(MethodSignature value)
        {
            return NewToken(value, 0x11000000);
        }

        public ILLocalVariable NewLocal(TypeDesc localType, bool isPinned = false)
        {
            int index = _locals.Count;
            _locals.Add(new LocalVariableDefinition(localType, isPinned));
            return (ILLocalVariable)index;
        }

        public ILCodeLabel NewCodeLabel()
        {
            var newLabel = new ILCodeLabel();
            return newLabel;
        }

        public ILExceptionRegionBuilder NewFinallyRegion()
        {
            var region = new ILExceptionRegionBuilder();
            _finallyRegions.Add(region);
            return region;
        }

        public MethodIL Link(MethodDesc owningMethod)
        {
            int totalLength = 0;
            int numSequencePoints = 0;

            for (int i = 0; i < _codeStreams.Count; i++)
            {
                ILCodeStream ilCodeStream = _codeStreams[i];
                ilCodeStream._startOffsetForLinking = totalLength;
                totalLength += ilCodeStream._length;
                numSequencePoints += ilCodeStream._sequencePoints.Count;
            }

            byte[] ilInstructions = new byte[totalLength];
            int copiedLength = 0;
            for (int i = 0; i < _codeStreams.Count; i++)
            {
                ILCodeStream ilCodeStream = _codeStreams[i];
                ilCodeStream.PatchLabels();
                Array.Copy(ilCodeStream._instructions, 0, ilInstructions, copiedLength, ilCodeStream._length);
                copiedLength += ilCodeStream._length;
            }

            MethodDebugInformation debugInfo = null;
            if (numSequencePoints > 0)
            {
                ILSequencePoint[] sequencePoints = new ILSequencePoint[numSequencePoints];
                int copiedSequencePointLength = 0;
                for (int codeStreamIndex = 0; codeStreamIndex < _codeStreams.Count; codeStreamIndex++)
                {
                    ILCodeStream ilCodeStream = _codeStreams[codeStreamIndex];

                    for (int sequencePointIndex = 0; sequencePointIndex < ilCodeStream._sequencePoints.Count; sequencePointIndex++)
                    {
                        ILSequencePoint sequencePoint = ilCodeStream._sequencePoints[sequencePointIndex];
                        sequencePoints[copiedSequencePointLength] = new ILSequencePoint(
                            ilCodeStream._startOffsetForLinking + sequencePoint.Offset,
                            sequencePoint.Document,
                            sequencePoint.LineNumber);
                        copiedSequencePointLength++;
                    }
                }

                debugInfo = new EmittedMethodDebugInformation(sequencePoints);
            }

            ILExceptionRegion[] exceptionRegions = null;

            int numberOfExceptionRegions = _finallyRegions.Count;
            if (numberOfExceptionRegions > 0)
            {
                exceptionRegions = new ILExceptionRegion[numberOfExceptionRegions];

                for (int i = 0; i < _finallyRegions.Count; i++)
                {
                    ILExceptionRegionBuilder region = _finallyRegions[i];

                    Debug.Assert(region.IsDefined);

                    exceptionRegions[i] = new ILExceptionRegion(ILExceptionRegionKind.Finally,
                        region.TryOffset, region.TryLength, region.HandlerOffset, region.HandlerLength,
                        classToken: 0, filterOffset: 0);
                }
            }

            var result = new ILStubMethodIL(owningMethod, ilInstructions, _locals.ToArray(), _tokens.ToArray(), exceptionRegions, debugInfo);
            result.CheckStackBalance();
            return result;
        }

        private class EmittedMethodDebugInformation : MethodDebugInformation
        {
            private readonly ILSequencePoint[] _sequencePoints;

            public EmittedMethodDebugInformation(ILSequencePoint[] sequencePoints)
            {
                _sequencePoints = sequencePoints;
            }

            public override IEnumerable<ILSequencePoint> GetSequencePoints()
            {
                return _sequencePoints;
            }
        }
    }

    public abstract partial class ILStubMethod : MethodDesc
    {
        public abstract MethodIL EmitIL();

        public override bool HasCustomAttribute(string attributeNamespace, string attributeName)
        {
            return false;
        }
    }
}
