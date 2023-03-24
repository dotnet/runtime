// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

using Internal.TypeSystem;

namespace Internal.IL
{
    public static class ILStackHelper
    {
        /// <summary>
        /// Validates that the CIL evaluation stack is properly balanced.
        /// </summary>
        [Conditional("DEBUG")]
        public static void CheckStackBalance(this MethodIL methodIL)
        {
            methodIL.ComputeMaxStack();
        }

        /// <summary>
        /// Computes the maximum number of items that can be pushed onto the CIL evaluation stack.
        /// </summary>
        public static int ComputeMaxStack(this MethodIL methodIL)
        {
            const int StackHeightNotSet = int.MinValue;

            byte[] ilbytes = methodIL.GetILBytes();
            int currentOffset = 0;
            int stackHeight = 0;
            int maxStack = 0;

            // TODO: Use Span<T> for this and stackalloc the array if reasonably sized
            int[] stackHeights = new int[ilbytes.Length];
            for (int i = 0; i < stackHeights.Length; i++)
                stackHeights[i] = StackHeightNotSet;

            // Catch and filter clauses have a known non-zero stack height.
            foreach (ILExceptionRegion region in methodIL.GetExceptionRegions())
            {
                if (region.Kind == ILExceptionRegionKind.Catch)
                {
                    stackHeights[region.HandlerOffset] = 1;
                }
                else if (region.Kind == ILExceptionRegionKind.Filter)
                {
                    stackHeights[region.FilterOffset] = 1;
                    stackHeights[region.HandlerOffset] = 1;
                }
            }

            while (currentOffset < ilbytes.Length)
            {
                ILOpcode opcode = (ILOpcode)ilbytes[currentOffset];
                if (opcode == ILOpcode.prefix1)
                    opcode = 0x100 + (ILOpcode)ilbytes[currentOffset + 1];

                // The stack height could be unknown if the previous instruction
                // was an unconditional control transfer.
                // In that case we check if we have a known stack height due to
                // this instruction being a target of a previous branch or an EH block.
                if (stackHeight == StackHeightNotSet)
                    stackHeight = stackHeights[currentOffset];

                // If we still don't know the stack height, ECMA-335 III.1.7.5
                // "Backward branch constraint" demands the evaluation stack be empty.
                if (stackHeight == StackHeightNotSet)
                    stackHeight = 0;

                // Remeber the stack height at this offset.
                Debug.Assert(stackHeights[currentOffset] == StackHeightNotSet
                    || stackHeights[currentOffset] == stackHeight);
                stackHeights[currentOffset] = stackHeight;

                bool isVariableSize = false;
                switch (opcode)
                {
                    case ILOpcode.arglist:
                    case ILOpcode.dup:
                    case ILOpcode.ldc_i4:
                    case ILOpcode.ldc_i4_0:
                    case ILOpcode.ldc_i4_1:
                    case ILOpcode.ldc_i4_2:
                    case ILOpcode.ldc_i4_3:
                    case ILOpcode.ldc_i4_4:
                    case ILOpcode.ldc_i4_5:
                    case ILOpcode.ldc_i4_6:
                    case ILOpcode.ldc_i4_7:
                    case ILOpcode.ldc_i4_8:
                    case ILOpcode.ldc_i4_m1:
                    case ILOpcode.ldc_i4_s:
                    case ILOpcode.ldc_i8:
                    case ILOpcode.ldc_r4:
                    case ILOpcode.ldc_r8:
                    case ILOpcode.ldftn:
                    case ILOpcode.ldnull:
                    case ILOpcode.ldsfld:
                    case ILOpcode.ldsflda:
                    case ILOpcode.ldstr:
                    case ILOpcode.ldtoken:
                    case ILOpcode.ldarg:
                    case ILOpcode.ldarg_0:
                    case ILOpcode.ldarg_1:
                    case ILOpcode.ldarg_2:
                    case ILOpcode.ldarg_3:
                    case ILOpcode.ldarg_s:
                    case ILOpcode.ldarga:
                    case ILOpcode.ldarga_s:
                    case ILOpcode.ldloc:
                    case ILOpcode.ldloc_0:
                    case ILOpcode.ldloc_1:
                    case ILOpcode.ldloc_2:
                    case ILOpcode.ldloc_3:
                    case ILOpcode.ldloc_s:
                    case ILOpcode.ldloca:
                    case ILOpcode.ldloca_s:
                    case ILOpcode.sizeof_:
                        stackHeight += 1;
                        break;

                    case ILOpcode.add:
                    case ILOpcode.add_ovf:
                    case ILOpcode.add_ovf_un:
                    case ILOpcode.and:
                    case ILOpcode.ceq:
                    case ILOpcode.cgt:
                    case ILOpcode.cgt_un:
                    case ILOpcode.clt:
                    case ILOpcode.clt_un:
                    case ILOpcode.div:
                    case ILOpcode.div_un:
                    case ILOpcode.initobj:
                    case ILOpcode.ldelem:
                    case ILOpcode.ldelem_i:
                    case ILOpcode.ldelem_i1:
                    case ILOpcode.ldelem_i2:
                    case ILOpcode.ldelem_i4:
                    case ILOpcode.ldelem_i8:
                    case ILOpcode.ldelem_r4:
                    case ILOpcode.ldelem_r8:
                    case ILOpcode.ldelem_ref:
                    case ILOpcode.ldelem_u1:
                    case ILOpcode.ldelem_u2:
                    case ILOpcode.ldelem_u4:
                    case ILOpcode.ldelema:
                    case ILOpcode.mkrefany:
                    case ILOpcode.mul:
                    case ILOpcode.mul_ovf:
                    case ILOpcode.mul_ovf_un:
                    case ILOpcode.or:
                    case ILOpcode.pop:
                    case ILOpcode.rem:
                    case ILOpcode.rem_un:
                    case ILOpcode.shl:
                    case ILOpcode.shr:
                    case ILOpcode.shr_un:
                    case ILOpcode.stsfld:
                    case ILOpcode.sub:
                    case ILOpcode.sub_ovf:
                    case ILOpcode.sub_ovf_un:
                    case ILOpcode.xor:
                    case ILOpcode.starg:
                    case ILOpcode.starg_s:
                    case ILOpcode.stloc:
                    case ILOpcode.stloc_0:
                    case ILOpcode.stloc_1:
                    case ILOpcode.stloc_2:
                    case ILOpcode.stloc_3:
                    case ILOpcode.stloc_s:
                        Debug.Assert(stackHeight > 0);
                        stackHeight -= 1;
                        break;

                    case ILOpcode.throw_:
                        Debug.Assert(stackHeight > 0);
                        stackHeight = StackHeightNotSet;
                        break;

                    case ILOpcode.br:
                    case ILOpcode.leave:
                    case ILOpcode.brfalse:
                    case ILOpcode.brtrue:
                    case ILOpcode.beq:
                    case ILOpcode.bge:
                    case ILOpcode.bge_un:
                    case ILOpcode.bgt:
                    case ILOpcode.bgt_un:
                    case ILOpcode.ble:
                    case ILOpcode.ble_un:
                    case ILOpcode.blt:
                    case ILOpcode.blt_un:
                    case ILOpcode.bne_un:
                        {
                            int target = currentOffset + ReadInt32(ilbytes, currentOffset + 1) + 5;

                            int adjustment;
                            bool isConditional;
                            if (opcode == ILOpcode.br || opcode == ILOpcode.leave)
                            {
                                isConditional = false;
                                adjustment = 0;
                            }
                            else if (opcode == ILOpcode.brfalse || opcode == ILOpcode.brtrue)
                            {
                                isConditional = true;
                                adjustment = 1;
                            }
                            else
                            {
                                isConditional = true;
                                adjustment = 2;
                            }

                            Debug.Assert(stackHeight >= adjustment);
                            stackHeight -= adjustment;

                            Debug.Assert(stackHeights[target] == StackHeightNotSet
                                || stackHeights[target] == stackHeight);

                            // Forward branch carries information about stack height at a future
                            // offset. We need to remember it.
                            if (target > currentOffset)
                                stackHeights[target] = stackHeight;

                            if (!isConditional)
                                stackHeight = StackHeightNotSet;
                        }
                        break;

                    case ILOpcode.br_s:
                    case ILOpcode.leave_s:
                    case ILOpcode.brfalse_s:
                    case ILOpcode.brtrue_s:
                    case ILOpcode.beq_s:
                    case ILOpcode.bge_s:
                    case ILOpcode.bge_un_s:
                    case ILOpcode.bgt_s:
                    case ILOpcode.bgt_un_s:
                    case ILOpcode.ble_s:
                    case ILOpcode.ble_un_s:
                    case ILOpcode.blt_s:
                    case ILOpcode.blt_un_s:
                    case ILOpcode.bne_un_s:
                        {
                            int target = currentOffset + (sbyte)ilbytes[currentOffset + 1] + 2;

                            int adjustment;
                            bool isConditional;
                            if (opcode == ILOpcode.br_s || opcode == ILOpcode.leave_s)
                            {
                                isConditional = false;
                                adjustment = 0;
                            }
                            else if (opcode == ILOpcode.brfalse_s || opcode == ILOpcode.brtrue_s)
                            {
                                isConditional = true;
                                adjustment = 1;
                            }
                            else
                            {
                                isConditional = true;
                                adjustment = 2;
                            }

                            Debug.Assert(stackHeight >= adjustment);
                            stackHeight -= adjustment;

                            Debug.Assert(stackHeights[target] == StackHeightNotSet
                                || stackHeights[target] == stackHeight);

                            // Forward branch carries information about stack height at a future
                            // offset. We need to remember it.
                            if (target > currentOffset)
                                stackHeights[target] = stackHeight;

                            if (!isConditional)
                                stackHeight = StackHeightNotSet;
                        }
                        break;

                    case ILOpcode.call:
                    case ILOpcode.calli:
                    case ILOpcode.callvirt:
                    case ILOpcode.newobj:
                        {
                            int token = ReadILToken(ilbytes, currentOffset + 1);
                            object obj = methodIL.GetObject(token);
                            MethodSignature sig = obj is MethodSignature ?
                                (MethodSignature)obj :
                                ((MethodDesc)obj).Signature;
                            int adjustment = sig.Length;
                            if (opcode == ILOpcode.newobj)
                            {
                                adjustment--;
                            }
                            else
                            {
                                if (opcode == ILOpcode.calli)
                                    adjustment++;
                                if (!sig.IsStatic)
                                    adjustment++;
                                if (!sig.ReturnType.IsVoid)
                                    adjustment--;
                            }

                            Debug.Assert(stackHeight >= adjustment);
                            stackHeight -= adjustment;
                        }
                        break;

                    case ILOpcode.ret:
                        {
                            bool hasReturnValue = !methodIL.OwningMethod.Signature.ReturnType.IsVoid;
                            if (hasReturnValue)
                                stackHeight -= 1;

                            Debug.Assert(stackHeight == 0);

                            stackHeight = StackHeightNotSet;
                        }
                        break;

                    case ILOpcode.cpobj:
                    case ILOpcode.stfld:
                    case ILOpcode.stind_i:
                    case ILOpcode.stind_i1:
                    case ILOpcode.stind_i2:
                    case ILOpcode.stind_i4:
                    case ILOpcode.stind_i8:
                    case ILOpcode.stind_r4:
                    case ILOpcode.stind_r8:
                    case ILOpcode.stind_ref:
                    case ILOpcode.stobj:
                        Debug.Assert(stackHeight > 1);
                        stackHeight -= 2;
                        break;

                    case ILOpcode.cpblk:
                    case ILOpcode.initblk:
                    case ILOpcode.stelem:
                    case ILOpcode.stelem_i:
                    case ILOpcode.stelem_i1:
                    case ILOpcode.stelem_i2:
                    case ILOpcode.stelem_i4:
                    case ILOpcode.stelem_i8:
                    case ILOpcode.stelem_r4:
                    case ILOpcode.stelem_r8:
                    case ILOpcode.stelem_ref:
                        Debug.Assert(stackHeight > 2);
                        stackHeight -= 3;
                        break;

                    case ILOpcode.break_:
                    case ILOpcode.constrained:
                    case ILOpcode.no:
                    case ILOpcode.nop:
                    case ILOpcode.readonly_:
                    case ILOpcode.tail:
                    case ILOpcode.unaligned:
                    case ILOpcode.volatile_:
                        break;

                    case ILOpcode.endfilter:
                        Debug.Assert(stackHeight > 0);
                        stackHeight = StackHeightNotSet;
                        break;

                    case ILOpcode.jmp:
                    case ILOpcode.rethrow:
                    case ILOpcode.endfinally:
                        stackHeight = StackHeightNotSet;
                        break;

                    case ILOpcode.box:
                    case ILOpcode.castclass:
                    case ILOpcode.ckfinite:
                    case ILOpcode.conv_i:
                    case ILOpcode.conv_i1:
                    case ILOpcode.conv_i2:
                    case ILOpcode.conv_i4:
                    case ILOpcode.conv_i8:
                    case ILOpcode.conv_ovf_i:
                    case ILOpcode.conv_ovf_i_un:
                    case ILOpcode.conv_ovf_i1:
                    case ILOpcode.conv_ovf_i1_un:
                    case ILOpcode.conv_ovf_i2:
                    case ILOpcode.conv_ovf_i2_un:
                    case ILOpcode.conv_ovf_i4:
                    case ILOpcode.conv_ovf_i4_un:
                    case ILOpcode.conv_ovf_i8:
                    case ILOpcode.conv_ovf_i8_un:
                    case ILOpcode.conv_ovf_u:
                    case ILOpcode.conv_ovf_u_un:
                    case ILOpcode.conv_ovf_u1:
                    case ILOpcode.conv_ovf_u1_un:
                    case ILOpcode.conv_ovf_u2:
                    case ILOpcode.conv_ovf_u2_un:
                    case ILOpcode.conv_ovf_u4:
                    case ILOpcode.conv_ovf_u4_un:
                    case ILOpcode.conv_ovf_u8:
                    case ILOpcode.conv_ovf_u8_un:
                    case ILOpcode.conv_r_un:
                    case ILOpcode.conv_r4:
                    case ILOpcode.conv_r8:
                    case ILOpcode.conv_u:
                    case ILOpcode.conv_u1:
                    case ILOpcode.conv_u2:
                    case ILOpcode.conv_u4:
                    case ILOpcode.conv_u8:
                    case ILOpcode.isinst:
                    case ILOpcode.ldfld:
                    case ILOpcode.ldflda:
                    case ILOpcode.ldind_i:
                    case ILOpcode.ldind_i1:
                    case ILOpcode.ldind_i2:
                    case ILOpcode.ldind_i4:
                    case ILOpcode.ldind_i8:
                    case ILOpcode.ldind_r4:
                    case ILOpcode.ldind_r8:
                    case ILOpcode.ldind_ref:
                    case ILOpcode.ldind_u1:
                    case ILOpcode.ldind_u2:
                    case ILOpcode.ldind_u4:
                    case ILOpcode.ldlen:
                    case ILOpcode.ldobj:
                    case ILOpcode.ldvirtftn:
                    case ILOpcode.localloc:
                    case ILOpcode.neg:
                    case ILOpcode.newarr:
                    case ILOpcode.not:
                    case ILOpcode.refanytype:
                    case ILOpcode.refanyval:
                    case ILOpcode.unbox:
                    case ILOpcode.unbox_any:
                        Debug.Assert(stackHeight > 0);
                        break;

                    case ILOpcode.switch_:
                        Debug.Assert(stackHeight > 0);
                        isVariableSize = true;
                        stackHeight -= 1;
                        currentOffset += 1 + (ReadInt32(ilbytes, currentOffset + 1) * 4) + 4;
                        break;

                    default:
                        Debug.Fail("Unknown instruction");
                        break;
                }

                if (!isVariableSize)
                    currentOffset += opcode.GetSize();

                maxStack = Math.Max(maxStack, stackHeight);
            }

            return maxStack;
        }

        private static int ReadInt32(byte[] ilBytes, int offset)
        {
            return ilBytes[offset]
                + (ilBytes[offset + 1] << 8)
                + (ilBytes[offset + 2] << 16)
                + (ilBytes[offset + 3] << 24);
        }

        private static int ReadILToken(byte[] ilBytes, int offset)
        {
            return ReadInt32(ilBytes, offset);
        }
    }
}
