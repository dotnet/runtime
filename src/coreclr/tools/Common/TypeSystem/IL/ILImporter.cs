// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.TypeSystem;

namespace Internal.IL
{
    internal sealed partial class ILImporter
    {
        private BasicBlock[] _basicBlocks; // Maps IL offset to basic block

        private BasicBlock _currentBasicBlock;
        private int _currentOffset;

        private BasicBlock _pendingBasicBlocks;

        //
        // IL stream reading
        //

        private byte ReadILByte()
        {
            if (_currentOffset >= _ilBytes.Length)
                ReportMethodEndInsideInstruction();

            return _ilBytes[_currentOffset++];
        }

        private ushort ReadILUInt16()
        {
            if (_currentOffset + 1 >= _ilBytes.Length)
                ReportMethodEndInsideInstruction();

            ushort val = (ushort)(_ilBytes[_currentOffset] + (_ilBytes[_currentOffset + 1] << 8));
            _currentOffset += 2;
            return val;
        }

        private uint ReadILUInt32()
        {
            if (_currentOffset + 3 >= _ilBytes.Length)
                ReportMethodEndInsideInstruction();

            uint val = (uint)(_ilBytes[_currentOffset] + (_ilBytes[_currentOffset + 1] << 8) + (_ilBytes[_currentOffset + 2] << 16) + (_ilBytes[_currentOffset + 3] << 24));
            _currentOffset += 4;
            return val;
        }

        private int ReadILToken()
        {
            return (int)ReadILUInt32();
        }

        private ulong ReadILUInt64()
        {
            ulong value = ReadILUInt32();
            value |= (((ulong)ReadILUInt32()) << 32);
            return value;
        }

        private unsafe float ReadILFloat()
        {
            uint value = ReadILUInt32();
            return *(float*)(&value);
        }

        private unsafe double ReadILDouble()
        {
            ulong value = ReadILUInt64();
            return *(double*)(&value);
        }

        private void SkipIL(int bytes)
        {
            if (_currentOffset + (bytes - 1) >= _ilBytes.Length)
                ReportMethodEndInsideInstruction();

            _currentOffset += bytes;
        }

        //
        // Basic block identification
        //

        private void FindBasicBlocks()
        {
            _basicBlocks = new BasicBlock[_ilBytes.Length];

            CreateBasicBlock(0);

            FindJumpTargets();

            FindEHTargets();
        }

        private BasicBlock CreateBasicBlock(int offset)
        {
            BasicBlock basicBlock = _basicBlocks[offset];
            if (basicBlock == null)
            {
                basicBlock = new BasicBlock() { StartOffset = offset };
                _basicBlocks[offset] = basicBlock;
            }

            return basicBlock;
        }

        private void FindJumpTargets()
        {
            _currentOffset = 0;

            while (_currentOffset < _ilBytes.Length)
            {
                MarkInstructionBoundary();

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
                        SkipIL(4);
                        break;
                    case ILOpcode.prefix1:
                        opCode = (ILOpcode)(0x100 + ReadILByte());
                        goto again;
                    case ILOpcode.br_s:
                    case ILOpcode.leave_s:
                        {
                            int delta = (sbyte)ReadILByte();
                            int target = _currentOffset + delta;
                            if ((uint)target < (uint)_basicBlocks.Length)
                            {
                                CreateBasicBlock(target);
                                OnLeaveTargetCreated(target);
                            }
                            else
                                ReportInvalidBranchTarget(target);
                        }
                        break;
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
                        {
                            int delta = (sbyte)ReadILByte();
                            int target = _currentOffset + delta;
                            if ((uint)target < (uint)_basicBlocks.Length)
                                CreateBasicBlock(target);
                            else
                                ReportInvalidBranchTarget(target);
                            CreateBasicBlock(_currentOffset);
                        }
                        break;
                    case ILOpcode.br:
                    case ILOpcode.leave:
                        {
                            int delta = (int)ReadILUInt32();
                            int target = _currentOffset + delta;
                            if ((uint)target < (uint)_basicBlocks.Length)
                            {
                                CreateBasicBlock(target);
                                OnLeaveTargetCreated(target);
                            }
                            else
                                ReportInvalidBranchTarget(target);
                        }
                        break;
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
                        {
                            int delta = (int)ReadILUInt32();
                            int target = _currentOffset + delta;
                            if ((uint)target < (uint)_basicBlocks.Length)
                                CreateBasicBlock(target);
                            else
                                ReportInvalidBranchTarget(target);
                            CreateBasicBlock(_currentOffset);
                        }
                        break;
                    case ILOpcode.switch_:
                        {
                            uint count = ReadILUInt32();
                            int jmpBase = _currentOffset + (int)(4 * count);
                            for (uint i = 0; i < count; i++)
                            {
                                int delta = (int)ReadILUInt32();
                                int target = jmpBase + delta;
                                if ((uint)target < (uint)_basicBlocks.Length)
                                    CreateBasicBlock(target);
                                else
                                    ReportInvalidBranchTarget(target);
                            }
                            CreateBasicBlock(_currentOffset);
                        }
                        break;
                    default:
                        continue;
                }
            }
        }

        partial void OnLeaveTargetCreated(int target);

        private void FindEHTargets()
        {
            for (int i = 0; i < _exceptionRegions.Length; i++)
            {
                var r = _exceptionRegions[i];

                CreateBasicBlock(r.ILRegion.TryOffset).TryStart = true;
                if (r.ILRegion.Kind == ILExceptionRegionKind.Filter)
                    CreateBasicBlock(r.ILRegion.FilterOffset).FilterStart = true;
                CreateBasicBlock(r.ILRegion.HandlerOffset).HandlerStart = true;
            }
        }

        //
        // Basic block importing
        //

        private void ImportBasicBlocks()
        {
            _pendingBasicBlocks = _basicBlocks[0];
            _basicBlocks[0].State = BasicBlock.ImportState.IsPending;
            while (_pendingBasicBlocks != null)
            {
                BasicBlock basicBlock = _pendingBasicBlocks;
                _pendingBasicBlocks = basicBlock.Next;

                StartImportingBasicBlock(basicBlock);
                ImportBasicBlock(basicBlock);
                EndImportingBasicBlock(basicBlock);
            }
        }

        private void MarkBasicBlock(BasicBlock basicBlock)
        {
            if (basicBlock.State == BasicBlock.ImportState.Unmarked)
            {
                // Link
                basicBlock.Next = _pendingBasicBlocks;
                _pendingBasicBlocks = basicBlock;

                basicBlock.State = BasicBlock.ImportState.IsPending;
            }
        }

        private void ImportBasicBlock(BasicBlock basicBlock)
        {
            _currentBasicBlock = basicBlock;
            _currentOffset = basicBlock.StartOffset;

            for (;;)
            {
                StartImportingInstruction();

                ILOpcode opCode = (ILOpcode)ReadILByte();

            again:
                switch (opCode)
                {
                    case ILOpcode.nop:
                        ImportNop();
                        break;
                    case ILOpcode.break_:
                        ImportBreak();
                        break;
                    case ILOpcode.ldarg_0:
                    case ILOpcode.ldarg_1:
                    case ILOpcode.ldarg_2:
                    case ILOpcode.ldarg_3:
                        ImportLoadVar(opCode - ILOpcode.ldarg_0, true);
                        break;
                    case ILOpcode.ldloc_0:
                    case ILOpcode.ldloc_1:
                    case ILOpcode.ldloc_2:
                    case ILOpcode.ldloc_3:
                        ImportLoadVar(opCode - ILOpcode.ldloc_0, false);
                        break;
                    case ILOpcode.stloc_0:
                    case ILOpcode.stloc_1:
                    case ILOpcode.stloc_2:
                    case ILOpcode.stloc_3:
                        ImportStoreVar(opCode - ILOpcode.stloc_0, false);
                        break;
                    case ILOpcode.ldarg_s:
                        ImportLoadVar(ReadILByte(), true);
                        break;
                    case ILOpcode.ldarga_s:
                        ImportAddressOfVar(ReadILByte(), true);
                        break;
                    case ILOpcode.starg_s:
                        ImportStoreVar(ReadILByte(), true);
                        break;
                    case ILOpcode.ldloc_s:
                        ImportLoadVar(ReadILByte(), false);
                        break;
                    case ILOpcode.ldloca_s:
                        ImportAddressOfVar(ReadILByte(), false);
                        break;
                    case ILOpcode.stloc_s:
                        ImportStoreVar(ReadILByte(), false);
                        break;
                    case ILOpcode.ldnull:
                        ImportLoadNull();
                        break;
                    case ILOpcode.ldc_i4_m1:
                        ImportLoadInt(-1, StackValueKind.Int32);
                        break;
                    case ILOpcode.ldc_i4_0:
                    case ILOpcode.ldc_i4_1:
                    case ILOpcode.ldc_i4_2:
                    case ILOpcode.ldc_i4_3:
                    case ILOpcode.ldc_i4_4:
                    case ILOpcode.ldc_i4_5:
                    case ILOpcode.ldc_i4_6:
                    case ILOpcode.ldc_i4_7:
                    case ILOpcode.ldc_i4_8:
                        ImportLoadInt(opCode - ILOpcode.ldc_i4_0, StackValueKind.Int32);
                        break;
                    case ILOpcode.ldc_i4_s:
                        ImportLoadInt((sbyte)ReadILByte(), StackValueKind.Int32);
                        break;
                    case ILOpcode.ldc_i4:
                        ImportLoadInt((int)ReadILUInt32(), StackValueKind.Int32);
                        break;
                    case ILOpcode.ldc_i8:
                        ImportLoadInt((long)ReadILUInt64(), StackValueKind.Int64);
                        break;
                    case ILOpcode.ldc_r4:
                        ImportLoadFloat(ReadILFloat());
                        break;
                    case ILOpcode.ldc_r8:
                        ImportLoadFloat(ReadILDouble());
                        break;
                    case ILOpcode.dup:
                        ImportDup();
                        break;
                    case ILOpcode.pop:
                        ImportPop();
                        break;
                    case ILOpcode.jmp:
                        ImportJmp(ReadILToken());
                        EndImportingInstruction();
                        return;
                    case ILOpcode.call:
                        ImportCall(opCode, ReadILToken());
                        break;
                    case ILOpcode.calli:
                        ImportCalli(ReadILToken());
                        break;
                    case ILOpcode.ret:
                        ImportReturn();
                        EndImportingInstruction();
                        return;
                    case ILOpcode.br_s:
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
                        {
                            int delta = (sbyte)ReadILByte();
                            ImportBranch(opCode + (ILOpcode.br - ILOpcode.br_s),
                                _basicBlocks[_currentOffset + delta], (opCode != ILOpcode.br_s) ? _basicBlocks[_currentOffset] : null);
                        }
                        EndImportingInstruction();
                        return;
                    case ILOpcode.br:
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
                        {
                            int delta = (int)ReadILUInt32();
                            ImportBranch(opCode,
                                _basicBlocks[_currentOffset + delta], (opCode != ILOpcode.br) ? _basicBlocks[_currentOffset] : null);
                        }
                        EndImportingInstruction();
                        return;
                    case ILOpcode.switch_:
                        {
                            uint count = ReadILUInt32();
                            int jmpBase = _currentOffset + (int)(4 * count);
                            int[] jmpDelta = new int[count];
                            for (uint i = 0; i < count; i++)
                                jmpDelta[i] = (int)ReadILUInt32();

                            ImportSwitchJump(jmpBase, jmpDelta, _basicBlocks[_currentOffset]);
                        }
                        EndImportingInstruction();
                        return;
                    case ILOpcode.ldind_i1:
                        ImportLoadIndirect(WellKnownType.SByte);
                        break;
                    case ILOpcode.ldind_u1:
                        ImportLoadIndirect(WellKnownType.Byte);
                        break;
                    case ILOpcode.ldind_i2:
                        ImportLoadIndirect(WellKnownType.Int16);
                        break;
                    case ILOpcode.ldind_u2:
                        ImportLoadIndirect(WellKnownType.UInt16);
                        break;
                    case ILOpcode.ldind_i4:
                        ImportLoadIndirect(WellKnownType.Int32);
                        break;
                    case ILOpcode.ldind_u4:
                        ImportLoadIndirect(WellKnownType.UInt32);
                        break;
                    case ILOpcode.ldind_i8:
                        ImportLoadIndirect(WellKnownType.Int64);
                        break;
                    case ILOpcode.ldind_i:
                        ImportLoadIndirect(WellKnownType.IntPtr);
                        break;
                    case ILOpcode.ldind_r4:
                        ImportLoadIndirect(WellKnownType.Single);
                        break;
                    case ILOpcode.ldind_r8:
                        ImportLoadIndirect(WellKnownType.Double);
                        break;
                    case ILOpcode.ldind_ref:
                        ImportLoadIndirect(null);
                        break;
                    case ILOpcode.stind_ref:
                        ImportStoreIndirect(null);
                        break;
                    case ILOpcode.stind_i1:
                        ImportStoreIndirect(WellKnownType.SByte);
                        break;
                    case ILOpcode.stind_i2:
                        ImportStoreIndirect(WellKnownType.Int16);
                        break;
                    case ILOpcode.stind_i4:
                        ImportStoreIndirect(WellKnownType.Int32);
                        break;
                    case ILOpcode.stind_i8:
                        ImportStoreIndirect(WellKnownType.Int64);
                        break;
                    case ILOpcode.stind_r4:
                        ImportStoreIndirect(WellKnownType.Single);
                        break;
                    case ILOpcode.stind_r8:
                        ImportStoreIndirect(WellKnownType.Double);
                        break;
                    case ILOpcode.add:
                    case ILOpcode.sub:
                    case ILOpcode.mul:
                    case ILOpcode.div:
                    case ILOpcode.div_un:
                    case ILOpcode.rem:
                    case ILOpcode.rem_un:
                    case ILOpcode.and:
                    case ILOpcode.or:
                    case ILOpcode.xor:
                        ImportBinaryOperation(opCode);
                        break;
                    case ILOpcode.shl:
                    case ILOpcode.shr:
                    case ILOpcode.shr_un:
                        ImportShiftOperation(opCode);
                        break;
                    case ILOpcode.neg:
                    case ILOpcode.not:
                        ImportUnaryOperation(opCode);
                        break;
                    case ILOpcode.conv_i1:
                        ImportConvert(WellKnownType.SByte, false, false);
                        break;
                    case ILOpcode.conv_i2:
                        ImportConvert(WellKnownType.Int16, false, false);
                        break;
                    case ILOpcode.conv_i4:
                        ImportConvert(WellKnownType.Int32, false, false);
                        break;
                    case ILOpcode.conv_i8:
                        ImportConvert(WellKnownType.Int64, false, false);
                        break;
                    case ILOpcode.conv_r4:
                        ImportConvert(WellKnownType.Single, false, false);
                        break;
                    case ILOpcode.conv_r8:
                        ImportConvert(WellKnownType.Double, false, false);
                        break;
                    case ILOpcode.conv_u4:
                        ImportConvert(WellKnownType.UInt32, false, true);
                        break;
                    case ILOpcode.conv_u8:
                        ImportConvert(WellKnownType.UInt64, false, true);
                        break;
                    case ILOpcode.callvirt:
                        ImportCall(opCode, ReadILToken());
                        break;
                    case ILOpcode.cpobj:
                        ImportCpOpj(ReadILToken());
                        break;
                    case ILOpcode.ldobj:
                        ImportLoadIndirect(ReadILToken());
                        break;
                    case ILOpcode.ldstr:
                        ImportLoadString(ReadILToken());
                        break;
                    case ILOpcode.newobj:
                        ImportCall(opCode, ReadILToken());
                        break;
                    case ILOpcode.castclass:
                    case ILOpcode.isinst:
                        ImportCasting(opCode, ReadILToken());
                        break;
                    case ILOpcode.conv_r_un:
                        ImportConvert(WellKnownType.Double, false, true);
                        break;
                    case ILOpcode.unbox:
                        ImportUnbox(ReadILToken(), opCode);
                        break;
                    case ILOpcode.throw_:
                        ImportThrow();
                        EndImportingInstruction();
                        return;
                    case ILOpcode.ldfld:
                        ImportLoadField(ReadILToken(), false);
                        break;
                    case ILOpcode.ldflda:
                        ImportAddressOfField(ReadILToken(), false);
                        break;
                    case ILOpcode.stfld:
                        ImportStoreField(ReadILToken(), false);
                        break;
                    case ILOpcode.ldsfld:
                        ImportLoadField(ReadILToken(), true);
                        break;
                    case ILOpcode.ldsflda:
                        ImportAddressOfField(ReadILToken(), true);
                        break;
                    case ILOpcode.stsfld:
                        ImportStoreField(ReadILToken(), true);
                        break;
                    case ILOpcode.stobj:
                        ImportStoreIndirect(ReadILToken());
                        break;
                    case ILOpcode.conv_ovf_i1_un:
                        ImportConvert(WellKnownType.SByte, true, true);
                        break;
                    case ILOpcode.conv_ovf_i2_un:
                        ImportConvert(WellKnownType.Int16, true, true);
                        break;
                    case ILOpcode.conv_ovf_i4_un:
                        ImportConvert(WellKnownType.Int32, true, true);
                        break;
                    case ILOpcode.conv_ovf_i8_un:
                        ImportConvert(WellKnownType.Int64, true, true);
                        break;
                    case ILOpcode.conv_ovf_u1_un:
                        ImportConvert(WellKnownType.Byte, true, true);
                        break;
                    case ILOpcode.conv_ovf_u2_un:
                        ImportConvert(WellKnownType.UInt16, true, true);
                        break;
                    case ILOpcode.conv_ovf_u4_un:
                        ImportConvert(WellKnownType.UInt32, true, true);
                        break;
                    case ILOpcode.conv_ovf_u8_un:
                        ImportConvert(WellKnownType.UInt64, true, true);
                        break;
                    case ILOpcode.conv_ovf_i_un:
                        ImportConvert(WellKnownType.IntPtr, true, true);
                        break;
                    case ILOpcode.conv_ovf_u_un:
                        ImportConvert(WellKnownType.UIntPtr, true, true);
                        break;
                    case ILOpcode.box:
                        ImportBox(ReadILToken());
                        break;
                    case ILOpcode.newarr:
                        ImportNewArray(ReadILToken());
                        break;
                    case ILOpcode.ldlen:
                        ImportLoadLength();
                        break;
                    case ILOpcode.ldelema:
                        ImportAddressOfElement(ReadILToken());
                        break;
                    case ILOpcode.ldelem_i1:
                        ImportLoadElement(WellKnownType.SByte);
                        break;
                    case ILOpcode.ldelem_u1:
                        ImportLoadElement(WellKnownType.Byte);
                        break;
                    case ILOpcode.ldelem_i2:
                        ImportLoadElement(WellKnownType.Int16);
                        break;
                    case ILOpcode.ldelem_u2:
                        ImportLoadElement(WellKnownType.UInt16);
                        break;
                    case ILOpcode.ldelem_i4:
                        ImportLoadElement(WellKnownType.Int32);
                        break;
                    case ILOpcode.ldelem_u4:
                        ImportLoadElement(WellKnownType.UInt32);
                        break;
                    case ILOpcode.ldelem_i8:
                        ImportLoadElement(WellKnownType.Int64);
                        break;
                    case ILOpcode.ldelem_i:
                        ImportLoadElement(WellKnownType.IntPtr);
                        break;
                    case ILOpcode.ldelem_r4:
                        ImportLoadElement(WellKnownType.Single);
                        break;
                    case ILOpcode.ldelem_r8:
                        ImportLoadElement(WellKnownType.Double);
                        break;
                    case ILOpcode.ldelem_ref:
                        ImportLoadElement(null);
                        break;
                    case ILOpcode.stelem_i:
                        ImportStoreElement(WellKnownType.IntPtr);
                        break;
                    case ILOpcode.stelem_i1:
                        ImportStoreElement(WellKnownType.SByte);
                        break;
                    case ILOpcode.stelem_i2:
                        ImportStoreElement(WellKnownType.Int16);
                        break;
                    case ILOpcode.stelem_i4:
                        ImportStoreElement(WellKnownType.Int32);
                        break;
                    case ILOpcode.stelem_i8:
                        ImportStoreElement(WellKnownType.Int64);
                        break;
                    case ILOpcode.stelem_r4:
                        ImportStoreElement(WellKnownType.Single);
                        break;
                    case ILOpcode.stelem_r8:
                        ImportStoreElement(WellKnownType.Double);
                        break;
                    case ILOpcode.stelem_ref:
                        ImportStoreElement(null);
                        break;
                    case ILOpcode.ldelem:
                        ImportLoadElement(ReadILToken());
                        break;
                    case ILOpcode.stelem:
                        ImportStoreElement(ReadILToken());
                        break;
                    case ILOpcode.unbox_any:
                        ImportUnbox(ReadILToken(), opCode);
                        break;
                    case ILOpcode.conv_ovf_i1:
                        ImportConvert(WellKnownType.SByte, true, false);
                        break;
                    case ILOpcode.conv_ovf_u1:
                        ImportConvert(WellKnownType.Byte, true, true);
                        break;
                    case ILOpcode.conv_ovf_i2:
                        ImportConvert(WellKnownType.Int16, true, false);
                        break;
                    case ILOpcode.conv_ovf_u2:
                        ImportConvert(WellKnownType.UInt16, true, true);
                        break;
                    case ILOpcode.conv_ovf_i4:
                        ImportConvert(WellKnownType.Int32, true, false);
                        break;
                    case ILOpcode.conv_ovf_u4:
                        ImportConvert(WellKnownType.UInt32, true, true);
                        break;
                    case ILOpcode.conv_ovf_i8:
                        ImportConvert(WellKnownType.Int64, true, false);
                        break;
                    case ILOpcode.conv_ovf_u8:
                        ImportConvert(WellKnownType.UInt64, true, true);
                        break;
                    case ILOpcode.refanyval:
                        ImportRefAnyVal(ReadILToken());
                        break;
                    case ILOpcode.ckfinite:
                        ImportCkFinite();
                        break;
                    case ILOpcode.mkrefany:
                        ImportMkRefAny(ReadILToken());
                        break;
                    case ILOpcode.ldtoken:
                        ImportLdToken(ReadILToken());
                        break;
                    case ILOpcode.conv_u2:
                        ImportConvert(WellKnownType.UInt16, false, true);
                        break;
                    case ILOpcode.conv_u1:
                        ImportConvert(WellKnownType.Byte, false, true);
                        break;
                    case ILOpcode.conv_i:
                        ImportConvert(WellKnownType.IntPtr, false, false);
                        break;
                    case ILOpcode.conv_ovf_i:
                        ImportConvert(WellKnownType.IntPtr, true, false);
                        break;
                    case ILOpcode.conv_ovf_u:
                        ImportConvert(WellKnownType.UIntPtr, true, true);
                        break;
                    case ILOpcode.add_ovf:
                    case ILOpcode.add_ovf_un:
                    case ILOpcode.mul_ovf:
                    case ILOpcode.mul_ovf_un:
                    case ILOpcode.sub_ovf:
                    case ILOpcode.sub_ovf_un:
                        ImportBinaryOperation(opCode);
                        break;
                    case ILOpcode.endfinally: //both endfinally and endfault
                        ImportEndFinally();
                        EndImportingInstruction();
                        return;
                    case ILOpcode.leave:
                        {
                            int delta = (int)ReadILUInt32();
                            ImportLeave(_basicBlocks[_currentOffset + delta]);
                        }
                        EndImportingInstruction();
                        return;
                    case ILOpcode.leave_s:
                        {
                            int delta = (sbyte)ReadILByte();
                            ImportLeave(_basicBlocks[_currentOffset + delta]);
                        }
                        EndImportingInstruction();
                        return;
                    case ILOpcode.stind_i:
                        ImportStoreIndirect(WellKnownType.IntPtr);
                        break;
                    case ILOpcode.conv_u:
                        ImportConvert(WellKnownType.UIntPtr, false, true);
                        break;
                    case ILOpcode.prefix1:
                        opCode = (ILOpcode)(0x100 + ReadILByte());
                        goto again;
                    case ILOpcode.arglist:
                        ImportArgList();
                        break;
                    case ILOpcode.ceq:
                    case ILOpcode.cgt:
                    case ILOpcode.cgt_un:
                    case ILOpcode.clt:
                    case ILOpcode.clt_un:
                        ImportCompareOperation(opCode);
                        break;
                    case ILOpcode.ldftn:
                    case ILOpcode.ldvirtftn:
                        ImportLdFtn(ReadILToken(), opCode);
                        break;
                    case ILOpcode.ldarg:
                        ImportLoadVar(ReadILUInt16(), true);
                        break;
                    case ILOpcode.ldarga:
                        ImportAddressOfVar(ReadILUInt16(), true);
                        break;
                    case ILOpcode.starg:
                        ImportStoreVar(ReadILUInt16(), true);
                        break;
                    case ILOpcode.ldloc:
                        ImportLoadVar(ReadILUInt16(), false);
                        break;
                    case ILOpcode.ldloca:
                        ImportAddressOfVar(ReadILUInt16(), false);
                        break;
                    case ILOpcode.stloc:
                        ImportStoreVar(ReadILUInt16(), false);
                        break;
                    case ILOpcode.localloc:
                        ImportLocalAlloc();
                        break;
                    case ILOpcode.endfilter:
                        ImportEndFilter();
                        EndImportingInstruction();
                        return;
                    case ILOpcode.unaligned:
                        ImportUnalignedPrefix(ReadILByte());
                        continue;
                    case ILOpcode.volatile_:
                        ImportVolatilePrefix();
                        continue;
                    case ILOpcode.tail:
                        ImportTailPrefix();
                        continue;
                    case ILOpcode.initobj:
                        ImportInitObj(ReadILToken());
                        break;
                    case ILOpcode.constrained:
                        ImportConstrainedPrefix(ReadILToken());
                        continue;
                    case ILOpcode.cpblk:
                        ImportCpBlk();
                        break;
                    case ILOpcode.initblk:
                        ImportInitBlk();
                        break;
                    case ILOpcode.no:
                        ImportNoPrefix(ReadILByte());
                        continue;
                    case ILOpcode.rethrow:
                        ImportRethrow();
                        EndImportingInstruction();
                        return;
                    case ILOpcode.sizeof_:
                        ImportSizeOf(ReadILToken());
                        break;
                    case ILOpcode.refanytype:
                        ImportRefAnyType();
                        break;
                    case ILOpcode.readonly_:
                        ImportReadOnlyPrefix();
                        continue;
                    default:
                        ReportInvalidInstruction(opCode);
                        EndImportingInstruction();
                        return;
                }

                EndImportingInstruction();

                // Check if control falls through the end of method.
                if (_currentOffset == _basicBlocks.Length)
                {
                    ReportFallthroughAtEndOfMethod();
                    return;
                }

                BasicBlock nextBasicBlock = _basicBlocks[_currentOffset];
                if (nextBasicBlock != null)
                {
                    ImportFallthrough(nextBasicBlock);
                    return;
                }
            }
        }

        private void ImportLoadIndirect(WellKnownType wellKnownType)
        {
            ImportLoadIndirect(GetWellKnownType(wellKnownType));
        }

        private void ImportStoreIndirect(WellKnownType wellKnownType)
        {
            ImportStoreIndirect(GetWellKnownType(wellKnownType));
        }

        private void ImportLoadElement(WellKnownType wellKnownType)
        {
            ImportLoadElement(GetWellKnownType(wellKnownType));
        }

        private void ImportStoreElement(WellKnownType wellKnownType)
        {
            ImportStoreElement(GetWellKnownType(wellKnownType));
        }
    }
}
