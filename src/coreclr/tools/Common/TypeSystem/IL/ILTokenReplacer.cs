// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;

namespace Internal.IL
{
    public class ILTokenReplacer
    {
        public static void Replace(byte[] ilStream, Func<int, int> tokenReplaceFunc)
        {
            int currentOffset = 0;
            for (; currentOffset < ilStream.Length;)
            {
                ILOpcode opCode = (ILOpcode)ReadILByte();
            again:

                switch (opCode)
                {
                    case ILOpcode.ldarg_s:
                    case ILOpcode.ldarga_s:
                    case ILOpcode.starg_s:
                    case ILOpcode.ldloc_s:
                    case ILOpcode.ldloca_s:
                    case ILOpcode.stloc_s:
                    case ILOpcode.ldc_i4_s:
                    case ILOpcode.unaligned:
                    case ILOpcode.no:
                        SkipIL(1);
                        break;
                    case ILOpcode.ldarg:
                    case ILOpcode.ldarga:
                    case ILOpcode.starg:
                    case ILOpcode.ldloc:
                    case ILOpcode.ldloca:
                    case ILOpcode.stloc:
                        SkipIL(2);
                        break;
                    case ILOpcode.ldc_i4:
                    case ILOpcode.ldc_r4:
                        SkipIL(4);
                        break;
                    case ILOpcode.ldc_i8:
                    case ILOpcode.ldc_r8:
                        SkipIL(8);
                        break;
                    case ILOpcode.jmp:
                    case ILOpcode.call:
                    case ILOpcode.calli:
                    case ILOpcode.callvirt:
                    case ILOpcode.cpobj:
                    case ILOpcode.ldobj:
                    case ILOpcode.ldstr:
                    case ILOpcode.newobj:
                    case ILOpcode.castclass:
                    case ILOpcode.isinst:
                    case ILOpcode.unbox:
                    case ILOpcode.ldfld:
                    case ILOpcode.ldflda:
                    case ILOpcode.stfld:
                    case ILOpcode.ldsfld:
                    case ILOpcode.ldsflda:
                    case ILOpcode.stsfld:
                    case ILOpcode.stobj:
                    case ILOpcode.box:
                    case ILOpcode.newarr:
                    case ILOpcode.ldelema:
                    case ILOpcode.ldelem:
                    case ILOpcode.stelem:
                    case ILOpcode.unbox_any:
                    case ILOpcode.refanyval:
                    case ILOpcode.mkrefany:
                    case ILOpcode.ldtoken:
                    case ILOpcode.ldftn:
                    case ILOpcode.ldvirtftn:
                    case ILOpcode.initobj:
                    case ILOpcode.constrained:
                    case ILOpcode.sizeof_:
                        ReplaceToken();
                        break;
                    case ILOpcode.prefix1:
                        opCode = (ILOpcode)(0x100 + ReadILByte());
                        goto again;
                    case ILOpcode.br_s:
                    case ILOpcode.leave_s:
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
                        SkipIL(1);
                        break;
                    case ILOpcode.br:
                    case ILOpcode.leave:
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
                        SkipIL(4);
                        break;
                    case ILOpcode.switch_:
                        {
                            uint count = ReadILUInt32();
                            SkipIL(checked((int)(count * 4)));
                        }
                        break;
                    default:
                        continue;
                }

            }

            byte ReadILByte()
            {
                byte b = ilStream[currentOffset++];
                return b;
            }
            void SkipIL(int countToSkip)
            {
                currentOffset += countToSkip;
            }
            UInt32 ReadILUInt32()
            {
                var result = (UInt32)BinaryPrimitives.ReadInt32LittleEndian(ilStream.AsSpan(currentOffset, 4));
                currentOffset += 4;
                return result;
            }

            void ReplaceToken()
            {
                var tokenSpan = ilStream.AsSpan(currentOffset, 4);

                // Replace token in IL stream with a new token provided by the tokenReplaceFunc
                //
                // This is by the StandaloneMethodMetadata logic to create method local tokens
                // and by the IL provider used for cross module inlining to create tokens which are 
                // stable and contained within the R2R module instead of being in a module separated
                // by a version boundary.
                int token = BinaryPrimitives.ReadInt32LittleEndian(tokenSpan);
                var alternateToken = tokenReplaceFunc(token);
                BinaryPrimitives.WriteInt32LittleEndian(tokenSpan, alternateToken);

                currentOffset += 4;
            }
        }
    }
}
