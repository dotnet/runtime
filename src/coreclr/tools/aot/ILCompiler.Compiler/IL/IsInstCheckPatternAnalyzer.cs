// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

using Internal.IL;
using Internal.TypeSystem;

namespace ILCompiler
{
    /// <summary>
    /// Simple state machine to analyze IL sequences that represent isinst checks.
    /// </summary>
    internal struct IsInstCheckPatternAnalyzer
    {
        // Captures following sequence:
        //
        // isinst Foo
        // Optional:
        //     stloc Y
        //     ldloc Y
        // Optional:
        //     ldnull
        //     cgt.un
        //     Optional:
        //         stloc X
        //         ldloc X
        // brfalse
        private enum State : byte
        {
            IsInst = 1,

            IsInstStLoc,

            IsInstLdnull,
            IsInstLdnullCgt,
            IsInstLdnullCgtStLoc,
            IsInstLdnullCgtStLocLdLoc,

            Branch,
        }

        private State _state;
        private int _token;

        public readonly bool IsIsInstBranch => _state is State.Branch;
        public int Token => _token;

        public void Advance(ILOpcode opcode, in ILReader reader, MethodIL methodIL)
        {
            switch (_state)
            {
                case 0:
                    if (opcode == ILOpcode.isinst)
                        (_state, _token) = (State.IsInst, reader.PeekILToken());
                    return;
                case State.IsInst:
                    if (opcode is ILOpcode.brfalse or ILOpcode.brfalse_s)
                        _state = State.Branch;
                    else if (opcode == ILOpcode.ldnull)
                        _state = State.IsInstLdnull;
                    else if (IsStlocLdlocSequence(opcode, reader))
                        _state = State.IsInstStLoc;
                    else
                        break;
                    return;
                case State.IsInstLdnull:
                    if (opcode == ILOpcode.cgt_un)
                        _state = State.IsInstLdnullCgt;
                    else
                        break;
                    return;
                case State.IsInstLdnullCgt:
                    if (IsStlocLdlocSequence(opcode, reader))
                        _state = State.IsInstLdnullCgtStLoc;
                    else
                        break;
                    return;
                case State.IsInstLdnullCgtStLoc:
                    if (opcode == ILOpcode.ldloc || opcode == ILOpcode.ldloc_s || (opcode >= ILOpcode.ldloc_0 && opcode <= ILOpcode.ldloc_3))
                        _state = State.IsInstLdnullCgtStLocLdLoc;
                    else
                        throw new UnreachableException();
                    return;
                case State.IsInstLdnullCgtStLocLdLoc:
                    if (opcode is ILOpcode.brfalse or ILOpcode.brfalse_s)
                        _state = State.Branch;
                    else
                        break;
                    return;
                case State.IsInstStLoc:
                    if (opcode == ILOpcode.ldloc || opcode == ILOpcode.ldloc_s || (opcode >= ILOpcode.ldloc_0 && opcode <= ILOpcode.ldloc_3))
                        _state = State.IsInst;
                    else
                        throw new UnreachableException();
                    return;
                default:
                    throw new UnreachableException();
            }

            _state = default;

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

            Advance(opcode, reader, methodIL);
        }
    }
}
