// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.IL;
using Internal.TypeSystem;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler
{
    internal class ILPatternAnalyzer<T> where T : ILPatternAnalyzerTraits
    {
        public readonly MethodIL Method;
        public T State;
        public readonly byte[] ILBytes;

        public ILPatternAnalyzer(T state, MethodIL method, byte[] methodBytes)
        {
            Method = method;
            State = state;
            ILBytes = methodBytes;
        }

        public bool IsLdTokenConsumedByTypeEqualityCheck(int offset)
        {
            // 'offset' points to an ldtoken instruction.
            //
            // This could be ldtoken in the first parameter position of a type equality check
            // or in the second parameter position. Check for the second parameter position
            // first since it's the cheapest.

            var reader = new ILReader(ILBytes, offset);

            // If it's not a typeof() it cannot be any of the patterns below, bail
            if (!TryReadTypeOf(ref reader, out _))
                return false;

            // If it's typeof followed by a call to op_Equality/op_Inequality, we're done.
            if (TryReadTypeEqualityOrInequalityOperation(ref reader))
                return true;

            // This might still be ldtoken associated with the first parameter of the equality
            // check. This checks for a couple patterns that we know how to analyze such as:
            // typeof(X) == typeof(Y)
            // typeof(X) == local
            // typeof(X) == local.GetType
            if (TryAnalyzeTypeEquality_TokenToken(offset, offsetIsAtTypeEquals: false, out int _,  out _)
                || TryAnalyzeTypeEquality_TokenOther(offset, offsetIsAtTypeEquals: false, out int _))
            {
                return true;
            }

            return false;
        }

        public bool TryAnalyzeTypeEquality_TokenToken(int offset, bool offsetIsAtTypeEquals, out TypeDesc type1, out TypeDesc type2)
        {
            if (TryAnalyzeTypeEquality_TokenToken(offset, offsetIsAtTypeEquals, out int type1tok, out int type2tok))
            {
                type1 = (TypeDesc)Method.GetObject(type1tok);
                type2 = (TypeDesc)Method.GetObject(type2tok);
                return true;
            }
            type1 = type2 = null;
            return false;
        }

        public bool TryAnalyzeTypeEquality_TokenToken(int offset, bool offsetIsAtTypeEquals, out int type1, out int type2)
        {
            // We expect to see a sequence:
            // -> offset may point to here
            // ldtoken Foo
            // call GetTypeFromHandle
            // ldtoken Bar
            // call GetTypeFromHandle
            // -> or offset may point to here

            type1 = 0;
            type2 = 0;

            if (offsetIsAtTypeEquals)
            {
                const int SequenceLength = 20;
                if (offset < SequenceLength)
                    return false;

                offset -= SequenceLength;

                if (!State.IsInstructionStart(offset))
                    return false;
            }

            ILReader reader = new ILReader(ILBytes, offset);

            if (!TryReadTypeOf(ref reader, out type1))
                return false;

            if (!TryReadTypeOf(ref reader, out type2))
                return false;

            if (!offsetIsAtTypeEquals)
            {
                if (!TryReadTypeEqualityOrInequalityOperation(ref reader))
                    return false;
            }
            else
            {
                Debug.Assert(TryReadTypeEqualityOrInequalityOperation(ref reader));
            }

            return true;
        }

        public bool TryAnalyzeTypeEquality_TokenOther(int offset, bool offsetIsAtTypeEquals, out TypeDesc type)
        {
            if (TryAnalyzeTypeEquality_TokenOther(offset, offsetIsAtTypeEquals, out int typeTok))
            {
                type = (TypeDesc)Method.GetObject(typeTok);
                return true;
            }
            type = null;
            return false;
        }

        public bool TryAnalyzeTypeEquality_TokenOther(int offset, bool offsetIsAtTypeEquals, out int type)
        {
            return TryExpandTypeEquality_TokenOther(offset, offsetIsAtTypeEquals, 1, expectGetType: false, out type)
                || TryExpandTypeEquality_TokenOther(offset, offsetIsAtTypeEquals, 2, expectGetType: false, out type)
                || TryExpandTypeEquality_TokenOther(offset, offsetIsAtTypeEquals, 3, expectGetType: false, out type)
                || TryExpandTypeEquality_TokenOther(offset, offsetIsAtTypeEquals, 1, expectGetType: true, out type)
                || TryExpandTypeEquality_TokenOther(offset, offsetIsAtTypeEquals, 2, expectGetType: true, out type)
                || TryExpandTypeEquality_TokenOther(offset, offsetIsAtTypeEquals, 3, expectGetType: true, out type);
        }

        private bool TryExpandTypeEquality_TokenOther(int offset, bool offsetIsAtTypeEquals, int ldInstructionSize, bool expectGetType, out int type)
        {
            // We expect to see a sequence:
            // -> offset may point to here
            // ldtoken Foo
            // call GetTypeFromHandle
            // ldloc.X/ldloc_s X/ldarg.X/ldarg_s X
            // [optional] call Object.GetType
            // -> or offset may point to here
            //
            // The ldtoken part can potentially be in the second argument position

            type = 0;

            if (offsetIsAtTypeEquals)
            {
                int sequenceLength = 5 + 5 + ldInstructionSize + (expectGetType ? 5 : 0);
                if (offset < sequenceLength)
                    return false;

                offset -= sequenceLength;

                if (!State.IsInstructionStart(offset))
                    return false;
            }

            ILReader reader = new ILReader(ILBytes, offset);

            // Is the ldtoken in the first position?
            if (reader.PeekILOpcode() == ILOpcode.ldtoken)
            {
                if (!TryReadTypeOf(ref reader, out type))
                    return false;
            }

            ILOpcode opcode = reader.ReadILOpcode();
            if (ldInstructionSize == 1 && opcode is (>= ILOpcode.ldloc_0 and <= ILOpcode.ldloc_3) or (>= ILOpcode.ldarg_0 and <= ILOpcode.ldarg_3))
            {
                // Nothing to read
            }
            else if (ldInstructionSize == 2 && opcode is ILOpcode.ldloc_s or ILOpcode.ldarg_s)
            {
                reader.ReadILByte();
            }
            else if (ldInstructionSize == 3 && opcode is ILOpcode.ldloc or ILOpcode.ldarg)
            {
                reader.ReadILUInt16();
            }
            else
            {
                return false;
            }

            if (State.IsBasicBlockStart(reader.Offset))
                return false;

            if (expectGetType)
            {
                if (reader.ReadILOpcode() is not ILOpcode.callvirt and not ILOpcode.call)
                    return false;

                // We don't actually mind if this is not Object.GetType
                reader.ReadILToken();

                if (State.IsBasicBlockStart(reader.Offset))
                    return false;
            }

            // If the ldtoken wasn't in the first position, it must be in the other
            if (type == 0)
            {
                if (!TryReadTypeOf(ref reader, out type))
                    return false;
            }

            if (!offsetIsAtTypeEquals)
            {
                if (!TryReadTypeEqualityOrInequalityOperation(ref reader))
                    return false;
            }
            else
            {
                Debug.Assert(TryReadTypeEqualityOrInequalityOperation(ref reader));
            }

            return true;
        }

        private bool TryReadTypeOf(ref ILReader reader, out int token)
        {
            token = 0;

            if (reader.ReadILOpcode() != ILOpcode.ldtoken)
                return false;

            token = reader.ReadILToken();

            if (State.IsBasicBlockStart(reader.Offset))
                return false;

            if (reader.ReadILOpcode() != ILOpcode.call)
                return false;

            MethodDesc method = (MethodDesc)Method.GetObject(reader.ReadILToken());

            if (!method.IsIntrinsic || method.Name != "GetTypeFromHandle")
                return false;

            if (State.IsBasicBlockStart(reader.Offset))
                return false;

            return true;
        }

        private bool TryReadTypeEqualityOrInequalityOperation(ref ILReader reader)
        {
            ILOpcode opcode = reader.ReadILOpcode();
            if (opcode != ILOpcode.call)
                return false;

            var method = (MethodDesc)Method.GetObject(reader.ReadILToken());
            if (method.IsIntrinsic && method.Name is "op_Equality" or "op_Inequality")
            {
                MetadataType owningType = method.OwningType as MetadataType;
                if (owningType != null)
                {
                    return owningType.Name == "Type" && owningType.Namespace == "System";
                }
            }

            return false;
        }
    }

    internal interface ILPatternAnalyzerTraits
    {
        bool IsInstructionStart(int offset);
        bool IsBasicBlockStart(int offset);
    }
}
