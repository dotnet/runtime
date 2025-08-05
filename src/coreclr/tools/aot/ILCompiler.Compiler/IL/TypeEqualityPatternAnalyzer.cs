// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

        public readonly int Token1 => IsTypeEqualityBranch ? _token1 : throw new UnreachableException();
        public readonly int Token2 => IsTwoTokens ? _token2 : throw new UnreachableException();

        public readonly bool IsDefault => _state == default;
        public readonly bool IsTypeEqualityCheck => _state is State.TypeEqualityCheck;
        public readonly bool IsTypeEqualityBranch => _state is State.Branch;
        public readonly bool IsTwoTokens => (_flags & Flags.TwoTokens) != 0;
        public readonly bool IsInequality => (_flags & Flags.Inequality) != 0;

        public void Advance(ILOpcode opcode, in ILReader reader, MethodIL methodIL)
        {
            switch (_state)
            {
                case 0:
                    if (opcode == ILOpcode.ldtoken)
                        (_state, _token1) = (State.LdToken, reader.PeekILToken());
                    return;
                case State.LdToken:
                    if (IsTypeGetTypeFromHandle(opcode, reader, methodIL))
                        _state = State.TypeOf;
                    else
                        break;
                    return;
                case State.TypeOf:
                    if (opcode == ILOpcode.ldtoken)
                        (_state, _token2) = (State.TypeOf_LdToken, reader.PeekILToken());
                    else if (IsArgumentOrLocalLoad(opcode))
                        _state = State.TypeOf_PushedOne;
                    else if (IsTypeEquals(opcode, reader, methodIL))
                        _state = State.TypeEqualityCheck;
                    else if (IsTypeInequals(opcode, reader, methodIL))
                        (_state, _flags) = (State.TypeEqualityCheck, _flags | Flags.Inequality);
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
                        _state = State.TypeEqualityCheck;
                    else if (IsTypeInequals(opcode, reader, methodIL))
                        (_state, _flags) = (State.TypeEqualityCheck, _flags | Flags.Inequality);
                    else
                        break;
                    return;
                case State.TypeOf_TypeOf:
                    if (IsTypeEquals(opcode, reader, methodIL))
                        (_state, _flags) = (State.TypeEqualityCheck, _flags | Flags.TwoTokens);
                    else if (IsTypeInequals(opcode, reader, methodIL))
                        (_state, _flags) = (State.TypeEqualityCheck, _flags | Flags.TwoTokens | Flags.Inequality);
                    else
                    {
                        _token1 = _token2;
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
                    if (opcode == ILOpcode.ldloc || opcode == ILOpcode.ldloc_s || (opcode >= ILOpcode.ldloc_0 && opcode <= ILOpcode.ldloc_3))
                        _state = State.TypeEqualityCheck;
                    else
                        throw new UnreachableException();
                    return;
                default:
                    throw new UnreachableException();
            }

            static bool IsTypeGetTypeFromHandle(ILOpcode opcode, in ILReader reader, MethodIL methodIL)
                => opcode == ILOpcode.call && methodIL.GetObject(reader.PeekILToken()) is MethodDesc method
                && method.IsIntrinsic && method.Name == "GetTypeFromHandle"
                && method.OwningType is MetadataType { Name: "Type", Namespace: "System" };

            static bool IsTypeEquals(ILOpcode opcode, in ILReader reader, MethodIL methodIL)
                => opcode == ILOpcode.call && methodIL.GetObject(reader.PeekILToken()) is MethodDesc method
                && method.IsIntrinsic && method.Name is "op_Equality"
                && method.OwningType is MetadataType { Name: "Type", Namespace: "System" };

            static bool IsTypeInequals(ILOpcode opcode, in ILReader reader, MethodIL methodIL)
                => opcode == ILOpcode.call && methodIL.GetObject(reader.PeekILToken()) is MethodDesc method
                && method.IsIntrinsic && method.Name is "op_Inequality"
                && method.OwningType is MetadataType { Name: "Type", Namespace: "System" };

            static bool IsObjectGetType(ILOpcode opcode, in ILReader reader, MethodIL methodIL)
                => opcode is ILOpcode.call or ILOpcode.callvirt && methodIL.GetObject(reader.PeekILToken()) is MethodDesc method
                && method.IsIntrinsic && method.Name is "GetType" && method.OwningType.IsObject;

            static bool IsArgumentOrLocalLoad(ILOpcode opcode)
                => opcode is (>= ILOpcode.ldloc_0 and <= ILOpcode.ldloc_3) or (>= ILOpcode.ldarg_0 and <= ILOpcode.ldarg_3);

            static bool IsStlocLdlocSequence(ILOpcode opcode, in ILReader reader)
            {
                if (opcode == ILOpcode.stloc || opcode == ILOpcode.stloc_s || (opcode >= ILOpcode.stloc_0 && opcode <= ILOpcode.stloc_3))
                {
                    ILReader nestedReader = reader;
                    int locIndex = opcode switch
                    {
                        ILOpcode.stloc => nestedReader.ReadILUInt16(),
                        ILOpcode.stloc_s => nestedReader.ReadILByte(),
                        _ => opcode - ILOpcode.stloc_0,
                    };
                    ILOpcode otherOpcode = nestedReader.ReadILOpcode();
                    return (otherOpcode == ILOpcode.ldloc || otherOpcode == ILOpcode.ldloc_s || (otherOpcode >= ILOpcode.ldloc_0 && otherOpcode <= ILOpcode.ldloc_3))
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
