// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Code adapted from https://blogs.msdn.microsoft.com/haibo_luo/2010/04/19/ilvisualizer-2010-solution

using System.Reflection.Emit;
using System.Reflection;

namespace System.Linq.Expressions.Tests
{
    public abstract class ILInstruction
    {
        internal ILInstruction(int offset, OpCode opCode)
        {
            Offset = offset;
            OpCode = opCode;
        }

        public int Offset { get; }
        public OpCode OpCode { get; }

        public abstract void Accept(ILInstructionVisitor visitor);
    }

    public sealed class InlineNoneInstruction : ILInstruction
    {
        internal InlineNoneInstruction(int offset, OpCode opCode)
            : base(offset, opCode) { }

        public override void Accept(ILInstructionVisitor visitor) => visitor.VisitInlineNoneInstruction(this);
    }

    public sealed class InlineBrTargetInstruction : ILInstruction
    {
        internal InlineBrTargetInstruction(int offset, OpCode opCode, int delta)
            : base(offset, opCode)
        {
            Delta = delta;
        }

        public int Delta { get; }
        public int TargetOffset => Offset + Delta + 1 + 4;

        public override void Accept(ILInstructionVisitor visitor) => visitor.VisitInlineBrTargetInstruction(this);
    }

    public sealed class ShortInlineBrTargetInstruction : ILInstruction
    {
        internal ShortInlineBrTargetInstruction(int offset, OpCode opCode, sbyte delta)
            : base(offset, opCode)
        {
            Delta = delta;
        }

        public sbyte Delta { get; }
        public int TargetOffset => Offset + Delta + 1 + 1;

        public override void Accept(ILInstructionVisitor visitor) => visitor.VisitShortInlineBrTargetInstruction(this);
    }

    public sealed class InlineSwitchInstruction : ILInstruction
    {
        private readonly int[] _deltas;
        private int[] _targetOffsets;

        internal InlineSwitchInstruction(int offset, OpCode opCode, int[] deltas)
            : base(offset, opCode)
        {
            _deltas = deltas;
        }

        public int[] Deltas => (int[])_deltas.Clone();
        public int[] TargetOffsets
        {
            get
            {
                if (_targetOffsets == null)
                {
                    int cases = _deltas.Length;
                    int itself = 1 + 4 + 4 * cases;
                    _targetOffsets = new int[cases];
                    for (int i = 0; i < cases; i++)
                        _targetOffsets[i] = Offset + _deltas[i] + itself;
                }

                return _targetOffsets;
            }
        }

        public override void Accept(ILInstructionVisitor visitor) => visitor.VisitInlineSwitchInstruction(this);
    }

    public sealed class InlineIInstruction : ILInstruction
    {
        internal InlineIInstruction(int offset, OpCode opCode, int value)
            : base(offset, opCode)
        {
            Value = value;
        }

        public int Value { get; }

        public override void Accept(ILInstructionVisitor visitor) => visitor.VisitInlineIInstruction(this);
    }

    public sealed class InlineI8Instruction : ILInstruction
    {
        internal InlineI8Instruction(int offset, OpCode opCode, long value)
            : base(offset, opCode)
        {
            Value = value;
        }

        public long Value { get; }

        public override void Accept(ILInstructionVisitor visitor) => visitor.VisitInlineI8Instruction(this);
    }

    public sealed class ShortInlineIInstruction : ILInstruction
    {
        internal ShortInlineIInstruction(int offset, OpCode opCode, sbyte value)
            : base(offset, opCode)
        {
            Value = value;
        }

        public sbyte Value { get; }

        public override void Accept(ILInstructionVisitor visitor) => visitor.VisitShortInlineIInstruction(this);
    }

    public sealed class InlineRInstruction : ILInstruction
    {
        internal InlineRInstruction(int offset, OpCode opCode, double value)
            : base(offset, opCode)
        {
            Value = value;
        }

        public double Value { get; }

        public override void Accept(ILInstructionVisitor visitor) => visitor.VisitInlineRInstruction(this);
    }

    public sealed class ShortInlineRInstruction : ILInstruction
    {
        internal ShortInlineRInstruction(int offset, OpCode opCode, float value)
            : base(offset, opCode)
        {
            Value = value;
        }

        public float Value { get; }

        public override void Accept(ILInstructionVisitor visitor) => visitor.VisitShortInlineRInstruction(this);
    }

    public sealed class InlineFieldInstruction : ILInstruction
    {
        private readonly ITokenResolver _resolver;
        private FieldInfo _field;

        internal InlineFieldInstruction(ITokenResolver resolver, int offset, OpCode opCode, int token)
            : base(offset, opCode)
        {
            _resolver = resolver;
            Token = token;
        }

        public FieldInfo Field => _field ??= _resolver.AsField(Token);
        public int Token { get; }

        public override void Accept(ILInstructionVisitor visitor) => visitor.VisitInlineFieldInstruction(this);
    }

    public sealed class InlineMethodInstruction : ILInstruction
    {
        private readonly ITokenResolver _resolver;
        private MethodBase _method;

        internal InlineMethodInstruction(int offset, OpCode opCode, int token, ITokenResolver resolver)
            : base(offset, opCode)
        {
            _resolver = resolver;
            Token = token;
        }

        public MethodBase Method => _method ??= _resolver.AsMethod(Token);
        public int Token { get; }

        public override void Accept(ILInstructionVisitor visitor) => visitor.VisitInlineMethodInstruction(this);
    }

    public sealed class InlineTypeInstruction : ILInstruction
    {
        private readonly ITokenResolver _resolver;
        private Type _type;

        internal InlineTypeInstruction(int offset, OpCode opCode, int token, ITokenResolver resolver)
            : base(offset, opCode)
        {
            _resolver = resolver;
            Token = token;
        }

        public Type Type => _type ??= _resolver.AsType(Token);
        public int Token { get; }

        public override void Accept(ILInstructionVisitor visitor) => visitor.VisitInlineTypeInstruction(this);
    }

    public sealed class InlineSigInstruction : ILInstruction
    {
        private readonly ITokenResolver _resolver;
        private byte[] _signature;

        internal InlineSigInstruction(int offset, OpCode opCode, int token, ITokenResolver resolver)
            : base(offset, opCode)
        {
            _resolver = resolver;
            Token = token;
        }

        public byte[] Signature => _signature ??= _resolver.AsSignature(Token);
        public int Token { get; }

        public override void Accept(ILInstructionVisitor visitor) => visitor.VisitInlineSigInstruction(this);
    }

    public sealed class InlineTokInstruction : ILInstruction
    {
        private readonly ITokenResolver _resolver;
        private MemberInfo _member;

        internal InlineTokInstruction(int offset, OpCode opCode, int token, ITokenResolver resolver)
            : base(offset, opCode)
        {
            _resolver = resolver;
            Token = token;
        }

        public MemberInfo Member => _member ??= _resolver.AsMember(Token);
        public int Token { get; }

        public override void Accept(ILInstructionVisitor visitor) => visitor.VisitInlineTokInstruction(this);
    }

    public sealed class InlineStringInstruction : ILInstruction
    {
        private readonly ITokenResolver _resolver;
        private string _string;

        internal InlineStringInstruction(int offset, OpCode opCode, int token, ITokenResolver resolver)
            : base(offset, opCode)
        {
            _resolver = resolver;
            Token = token;
        }

        public string String => _string ??= _resolver.AsString(Token);
        public int Token { get; }

        public override void Accept(ILInstructionVisitor visitor) => visitor.VisitInlineStringInstruction(this);
    }

    public sealed class InlineVarInstruction : ILInstruction
    {
        internal InlineVarInstruction(int offset, OpCode opCode, ushort ordinal)
            : base(offset, opCode)
        {
            Ordinal = ordinal;
        }

        public ushort Ordinal { get; }

        public override void Accept(ILInstructionVisitor visitor) => visitor.VisitInlineVarInstruction(this);
    }

    public sealed class ShortInlineVarInstruction : ILInstruction
    {
        internal ShortInlineVarInstruction(int offset, OpCode opCode, byte ordinal)
            : base(offset, opCode)
        {
            Ordinal = ordinal;
        }

        public byte Ordinal { get; }

        public override void Accept(ILInstructionVisitor visitor) => visitor.VisitShortInlineVarInstruction(this);
    }
}
