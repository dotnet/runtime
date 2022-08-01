// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

using Internal.IL;
using Internal.TypeSystem;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler.Dataflow
{
    internal static class ScannerExtensions
    {
        public static bool IsControlFlowInstruction(this ILOpcode opcode)
        {
            switch (opcode)
            {
                case ILOpcode.br_s:
                case ILOpcode.br:
                case ILOpcode.brfalse_s:
                case ILOpcode.brtrue_s:
                case ILOpcode.beq_s:
                case ILOpcode.bge_s:
                case ILOpcode.bgt_s:
                case ILOpcode.ble_s:
                case ILOpcode.blt_s:
                case ILOpcode.bne_un_s:
                case ILOpcode.bge_un_s:
                case ILOpcode.bgt_un_s:
                case ILOpcode.ble_un_s:
                case ILOpcode.blt_un_s:
                case ILOpcode.brfalse:
                case ILOpcode.brtrue:
                case ILOpcode.beq:
                case ILOpcode.bge:
                case ILOpcode.bgt:
                case ILOpcode.ble:
                case ILOpcode.blt:
                case ILOpcode.bne_un:
                case ILOpcode.bge_un:
                case ILOpcode.bgt_un:
                case ILOpcode.ble_un:
                case ILOpcode.blt_un:
                case ILOpcode.switch_:
                case ILOpcode.leave:
                case ILOpcode.leave_s:
                case ILOpcode.endfilter:
                case ILOpcode.endfinally:
                case ILOpcode.throw_:
                case ILOpcode.rethrow:
                    return true;
            }
            return false;
        }

        public static HashSet<int> ComputeBranchTargets(this MethodIL methodBody)
        {
            HashSet<int> branchTargets = new HashSet<int>();
            var reader = new ILReader(methodBody.GetILBytes());
            while (reader.HasNext)
            {
                ILOpcode opcode = reader.ReadILOpcode();
                if (opcode >= ILOpcode.br_s && opcode <= ILOpcode.blt_un)
                {
                    branchTargets.Add(reader.ReadBranchDestination(opcode));
                }
                else if (opcode == ILOpcode.switch_)
                {
                    uint count = reader.ReadILUInt32();
                    int jmpBase = reader.Offset + (int)(4 * count);
                    for (uint i = 0; i < count; i++)
                    {
                        branchTargets.Add((int)reader.ReadILUInt32() + jmpBase);
                    }
                }
                else
                {
                    reader.Skip(opcode);
                }
            }
            foreach (ILExceptionRegion einfo in methodBody.GetExceptionRegions())
            {
                if (einfo.Kind == ILExceptionRegionKind.Filter)
                {
                    branchTargets.Add(einfo.FilterOffset);
                }
                branchTargets.Add(einfo.HandlerOffset);
            }
            return branchTargets;
        }
    }
}
