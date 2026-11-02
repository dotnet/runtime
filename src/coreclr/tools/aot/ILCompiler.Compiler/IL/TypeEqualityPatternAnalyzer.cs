// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

using Internal.IL;
using Internal.TypeSystem;

namespace ILCompiler
{
    /// <summary>
    /// Simple state machine to analyze IL sequences that represent runtime type equality checks.
    /// </summary>
    internal struct TypeEqualityPatternAnalyzer
    {
        // Captures following sequence:
        //
        // ldtoken Foo
        // call GetTypeFromHandle
        // One Of:
        //     ldtoken Bar
        //     call GetTypeFromHandle
        //   Or:
        //     ldarg/ldloc X
        //     Optional:
        //       call object.GetType()
        //   Or:
        //     (nothing)
        // End One Of
        // call op_Equality/op_Inequality
        // Optional:
        //   stloc X
        //   ldloc X
        // brtrue/brfalse

        private enum State : byte
        {
            LdToken = 1,
            TypeOf,

            TypeOf_LdToken,
            TypeOf_TypeOf,
            TypeOf_PushedOne,

            TypeEqualityCheck,
            TypeEqualityCheck_StlocLdloc,

            Branch,
        }

        private enum Flags : byte
        {
            TwoTokens = 1,
            Inequality = 2,
        }

        private State _state;
        private Flags _flags;
        private int _token1;
        private int _token2;
        private int _token1Offset;
        private int _token2Offset;
        private int _equalityOffset;

        public readonly int Token1 => IsTypeEqualityBranch ? _token1 : throw new UnreachableException();
        public readonly int Token2 => IsTwoTokens ? _token2 : throw new UnreachableException();

        public readonly bool IsDefault => _state == default;
        public readonly bool IsTypeEqualityCheck => _state is State.TypeEqualityCheck;
        public readonly bool IsTypeEqualityBranch => _state is State.Branch;
        public readonly bool IsTwoTokens => (_flags & Flags.TwoTokens) != 0;
        public readonly bool IsInequality => (_flags & Flags.Inequality) != 0;
        public readonly int Token1Offset => _token1Offset;
        public readonly int Token2Offset => _token2Offset;
        public readonly int EqualityOffset => IsTypeEqualityBranch ? _equalityOffset : throw new UnreachableException();

        public readonly MetadataType GetConditionalType(MethodIL methodIL)
        {
            if (!IsTypeEqualityBranch)
                return null;

            TypeDesc candidate = (TypeDesc)methodIL.GetObject(Token1);
            if (IsTwoTokens)
            {
                TypeDesc second = (TypeDesc)methodIL.GetObject(Token2);
                if (candidate.ContainsSignatureVariables() || candidate.IsRuntimeDeterminedSubtype
                    || candidate.IsCanonicalSubtype(CanonicalFormKind.Any))
                    candidate = second;
                else if (!(second.ContainsSignatureVariables() || second.IsRuntimeDeterminedSubtype
                    || second.IsCanonicalSubtype(CanonicalFormKind.Any)))
                    return null;
            }

            // Runtime-created generic and parameterized types need additional conditions.
            // Validate operand removal only after identifying an eligible literal type.
            if (candidate is not MetadataType metadata || metadata.IsModuleType || candidate.HasInstantiation || candidate.IsGenericDefinition
                || candidate.IsCanonicalSubtype(CanonicalFormKind.Any))
                return null;

            return CanRemoveTypeOperands(methodIL) ? metadata : null;
        }

        public readonly bool CanRemoveTypeOperands(MethodIL methodIL)
            => CanRemoveTypeOperand(methodIL, Token1)
                && (!IsTwoTokens || CanRemoveTypeOperand(methodIL, Token2));

        private static bool CanRemoveTypeOperand(MethodIL methodIL, int token)
        {
            // A direct type parameter is already a valid type supplied by the caller. This is
            // different from forming a new constructed type such as G<T> inside shared code.
            if (((TypeDesc)methodIL.GetMethodILDefinition().GetObject(token)).IsSignatureVariable)
                return true;

            TypeDesc type = (TypeDesc)methodIL.GetObject(token);
            if (type.IsGenericDefinition || type.ContainsSignatureVariables(treatGenericParameterLikeSignatureVariable: true) || type.IsRuntimeDeterminedSubtype
                || type.IsCanonicalSubtype(CanonicalFormKind.Any))
                return false;

            // Validate closed constructed types before removing their loads. If validation fails,
            // retain the original comparison and its existing type-loading behavior.
            try
            {
                ((CompilerTypeSystemContext)type.Context).EnsureLoadableType(type);
                return true;
            }
            catch (TypeSystemException)
            {
                return false;
            }
        }

        public void Advance(ILOpcode opcode, in ILReader reader, MethodIL methodIL)
        {
            switch (_state)
            {
                case 0:
                    if (opcode == ILOpcode.ldtoken)
                    {
                        (_state, _token1) = (State.LdToken, reader.PeekILToken());
                        _token1Offset = reader.Offset - 1;
                    }
                    return;
                case State.LdToken:
                    if (IsTypeGetTypeFromHandle(opcode, reader, methodIL))
                        _state = State.TypeOf;
                    else
                        break;
                    return;
                case State.TypeOf:
                    if (opcode == ILOpcode.ldtoken)
                    {
                        (_state, _token2) = (State.TypeOf_LdToken, reader.PeekILToken());
                        _token2Offset = reader.Offset - 1;
                    }
                    else if (IsArgumentOrLocalLoad(opcode))
                        _state = State.TypeOf_PushedOne;
                    else if (IsTypeEquals(opcode, reader, methodIL))
                        (_state, _equalityOffset) = (State.TypeEqualityCheck, reader.Offset - 1);
                    else if (IsTypeInequals(opcode, reader, methodIL))
                        (_state, _flags, _equalityOffset) = (State.TypeEqualityCheck, _flags | Flags.Inequality, reader.Offset - 1);
                    else
                        break;
                    return;
                case State.TypeOf_LdToken:
                    if (IsTypeGetTypeFromHandle(opcode, reader, methodIL))
                        _state = State.TypeOf_TypeOf;
                    else
                        break;
                    return;
                case State.TypeOf_PushedOne:
                    if (IsObjectGetType(opcode, reader, methodIL))
                    {
                        // Nothing, state stays the same
                    }
                    else if (IsTypeEquals(opcode, reader, methodIL))
                        (_state, _equalityOffset) = (State.TypeEqualityCheck, reader.Offset - 1);
                    else if (IsTypeInequals(opcode, reader, methodIL))
                        (_state, _flags, _equalityOffset) = (State.TypeEqualityCheck, _flags | Flags.Inequality, reader.Offset - 1);
                    else
                        break;
                    return;
                case State.TypeOf_TypeOf:
                    if (IsTypeEquals(opcode, reader, methodIL))
                        (_state, _flags, _equalityOffset) = (State.TypeEqualityCheck, _flags | Flags.TwoTokens, reader.Offset - 1);
                    else if (IsTypeInequals(opcode, reader, methodIL))
                        (_state, _flags, _equalityOffset) = (State.TypeEqualityCheck, _flags | Flags.TwoTokens | Flags.Inequality, reader.Offset - 1);
                    else
                    {
                        _token1 = _token2;
                        _token1Offset = _token2Offset;
                        goto case State.TypeOf;
                    }
                    return;
                case State.TypeEqualityCheck:
                    if (opcode is ILOpcode.brfalse or ILOpcode.brfalse_s or ILOpcode.brtrue or ILOpcode.brtrue_s)
                        _state = State.Branch;
                    else if (IsStlocLdlocSequence(opcode, reader))
                        _state = State.TypeEqualityCheck_StlocLdloc;
                    else
                        break;
                    return;
                case State.TypeEqualityCheck_StlocLdloc:
                    _state = opcode is ILOpcode.ldloc or ILOpcode.ldloc_s or (>= ILOpcode.ldloc_0 and <= ILOpcode.ldloc_3) ? State.TypeEqualityCheck
                        : throw new UnreachableException();
                    return;
                default:
                    throw new UnreachableException();
            }

            static bool IsSystemType(TypeDesc t)
                => t is MetadataType mdType && mdType.Name == "Type"u8 && mdType.Namespace == "System"u8;

            static bool IsTypeGetTypeFromHandle(ILOpcode opcode, in ILReader reader, MethodIL methodIL)
                => opcode == ILOpcode.call && methodIL.GetObject(reader.PeekILToken()) is MethodDesc method
                && method.IsIntrinsic && method.Name == "GetTypeFromHandle"u8
                && IsSystemType(method.OwningType);

            static bool IsTypeEquals(ILOpcode opcode, in ILReader reader, MethodIL methodIL)
                => opcode == ILOpcode.call && methodIL.GetObject(reader.PeekILToken()) is MethodDesc method
                && method.IsIntrinsic && method.Name == "op_Equality"u8
                && IsSystemType(method.OwningType);

            static bool IsTypeInequals(ILOpcode opcode, in ILReader reader, MethodIL methodIL)
                => opcode == ILOpcode.call && methodIL.GetObject(reader.PeekILToken()) is MethodDesc method
                && method.IsIntrinsic && method.Name == "op_Inequality"u8
                && IsSystemType(method.OwningType);

            static bool IsObjectGetType(ILOpcode opcode, in ILReader reader, MethodIL methodIL)
                => opcode is ILOpcode.call or ILOpcode.callvirt && methodIL.GetObject(reader.PeekILToken()) is MethodDesc method
                && method.IsIntrinsic && method.Name == "GetType"u8 && method.OwningType.IsObject;

            static bool IsArgumentOrLocalLoad(ILOpcode opcode)
                => opcode is (>= ILOpcode.ldloc_0 and <= ILOpcode.ldloc_3) or (>= ILOpcode.ldarg_0 and <= ILOpcode.ldarg_3)
                   or ILOpcode.ldloc or ILOpcode.ldloc_s or ILOpcode.ldarg or ILOpcode.ldarg_s;

            static bool IsStlocLdlocSequence(ILOpcode opcode, in ILReader reader)
            {
                if (opcode is ILOpcode.stloc or ILOpcode.stloc_s or (>= ILOpcode.stloc_0 and <= ILOpcode.stloc_3))
                {
                    ILReader nestedReader = reader;
                    int locIndex = opcode switch
                    {
                        ILOpcode.stloc => nestedReader.ReadILUInt16(),
                        ILOpcode.stloc_s => nestedReader.ReadILByte(),
                        _ => opcode - ILOpcode.stloc_0,
                    };
                    ILOpcode otherOpcode = nestedReader.ReadILOpcode();

                    return (otherOpcode is ILOpcode.ldloc or ILOpcode.ldloc_s or (>= ILOpcode.ldloc_0 and <= ILOpcode.ldloc_3))
                        && otherOpcode switch
                        {
                            ILOpcode.ldloc => nestedReader.ReadILUInt16(),
                            ILOpcode.ldloc_s => nestedReader.ReadILByte(),
                            _ => otherOpcode - ILOpcode.ldloc_0,
                        } == locIndex;
                }
                return false;
            }

            _state = default;
            _flags = default;

            Advance(opcode, reader, methodIL);
        }
    }
}
