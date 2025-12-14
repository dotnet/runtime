// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using static Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers.ARM.LookupValues;
using static Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers.ARMContext;

namespace Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers.ARM;

internal class ARMUnwinder(Target target)
{
    private const uint MAX_PROLOG_SIZE = 16;
    private const uint MAX_EPILOG_SIZE = 16;

    private readonly Target _target = target;
    private readonly IExecutionManager _eman = target.Contracts.ExecutionManager;

    #region Entrypoint

    public bool Unwind(ref ARMContext context)
    {
        if (_eman.GetCodeBlockHandle(context.InstructionPointer.Value) is not CodeBlockHandle cbh)
        {
            throw new InvalidOperationException("Unwind failed, unable to find code block for the instruction pointer.");
        }

        uint startingPc = context.Pc;
        uint startingSp = context.Sp;

        TargetPointer imageBase = _eman.GetUnwindInfoBaseAddress(cbh);
        Data.RuntimeFunction functionEntry = _target.ProcessedData.GetOrAdd<Data.RuntimeFunction>(_eman.GetUnwindInfo(cbh));

        if ((functionEntry.UnwindData & 0x3) != 0)
        {
            if (!UnwindCompact(ref context, imageBase, functionEntry))
                return false;
        }
        else
        {
            if (!UnwindFull(ref context, imageBase, functionEntry))
                return false;
        }

        // PC == 0 means unwinding is finished.
        // Same if no forward progress is made
        if (context.Pc == 0 || (context.Pc == startingPc && context.Sp == startingSp))
            return false;

        return true;
    }

    #endregion
    #region Unwind Logic

    private bool UnwindFull(
        ref ARMContext context,
        TargetPointer imageBase,
        Data.RuntimeFunction functionEntry)
    {
        uint controlPcRva = (uint)(context.Pc - imageBase.Value);
        bool status = true;

        //
        // Unless we encounter a special frame, assume that any unwinding
        // will return us to the return address of a call and set the flag
        // appropriately (it will be cleared again if the special cases apply).
        //
        context.ContextFlags |= (uint)ContextFlagsValues.CONTEXT_UNWOUND_TO_CALL;

        //
        // Fetch the header word from the .xdata blob
        //
        TargetPointer unwindDataPtr = imageBase + functionEntry.UnwindData;
        uint headerWord = _target.Read<uint>(unwindDataPtr);
        unwindDataPtr += 4;

        //
        // Verify the version before we do anything else
        //
        if (((headerWord >> 18) & 3) != 0)
            return false;

        uint functionLength = headerWord & 0x3ffff;
        uint offsetInFunction = (controlPcRva - (functionEntry.BeginAddress & ~1u)) / 2u;

        if (offsetInFunction >= functionLength)
            return false;

        //
        // Determine the number of epilog scope records and the maximum number
        // of unwind codes.
        //

        uint unwindWords = (headerWord >> 28) & 15;
        uint epilogScopeCount = (headerWord >> 23) & 31;
        if (epilogScopeCount == 0 && unwindWords == 0)
        {
            epilogScopeCount = _target.Read<uint>(unwindDataPtr);
            unwindDataPtr += 4;
            unwindWords = (epilogScopeCount >> 16) & 0xff;
            epilogScopeCount &= 0xffff;
        }

        uint unwindIndex = 0;
        if ((headerWord & (1 << 21)) != 0)
        {
            unwindIndex = epilogScopeCount;
            epilogScopeCount = 0;
        }

        //
        // Unless we are in a prolog/epilog, we execute the unwind codes
        // that immediately follow the epilog scope list.
        //

        TargetPointer unwindCodePtr = unwindDataPtr + 4 * epilogScopeCount;
        TargetPointer unwindCodesEndPtr = unwindCodePtr + 4 * unwindWords;
        uint skipHalfwords = 0;

        //
        // If we're near the start of the function, and this function has a prolog,
        // compute the size of the prolog from the unwind codes. If we're in the
        // midst of it, we still execute starting at unwind code index 0, but we may
        // need to skip some to account for partial execution of the prolog.
        //

        if (offsetInFunction < MAX_PROLOG_SIZE && ((headerWord & (1 << 22)) == 0))
        {
            uint scopeSize = ComputeScopeSize(unwindCodePtr, unwindCodesEndPtr, isEpilog: false);

            if (offsetInFunction < scopeSize)
            {
                skipHalfwords = scopeSize - offsetInFunction;
            }
        }

        //
        // We're not in the prolog, now check to see if we are in the epilog.
        // In the simple case, the 'E' bit is set indicating there is a single
        // epilog that lives at the end of the function. If we're near the end
        // of the function, compute the actual size of the epilog from the
        // unwind codes. If we're in the midst of it, adjust the unwind code
        // pointer to the start of the codes and determine how many we need to skip.
        //
        if (skipHalfwords != 0)
        {
            // We found the prolog above, do nothing here.
        }
        else if ((headerWord & (1 << 21)) != 0)
        {
            if (offsetInFunction + MAX_EPILOG_SIZE >= functionLength)
            {
                uint scopeSize = ComputeScopeSize(unwindCodePtr + unwindIndex, unwindCodesEndPtr, isEpilog: true);
                uint scopeStart = functionLength - scopeSize;

                if (offsetInFunction >= scopeStart)
                {
                    unwindCodePtr += unwindIndex;
                    skipHalfwords = offsetInFunction - scopeStart;
                }
            }
        }

        //
        // In the multiple-epilog case, we scan forward to see if we are within
        // shooting distance of any of the epilogs. If we are, we compute the
        // actual size of the epilog from the unwind codes and proceed like the
        // simple case above.
        //

        else
        {
            for (uint scopeNum = 0; scopeNum < epilogScopeCount; scopeNum++)
            {
                headerWord = _target.Read<uint>(unwindDataPtr);
                unwindDataPtr += 4;

                //
                // The scope records are stored in order. If we hit a record that
                // starts after our current position, we must not be in an epilog.
                //

                uint scopeStart = headerWord & 0x3ffff;
                if (offsetInFunction < scopeStart)
                {
                    break;
                }

                if (offsetInFunction < scopeStart + MAX_EPILOG_SIZE)
                {
                    unwindIndex = headerWord >> 24;
                    uint scopeSize = ComputeScopeSize(unwindCodePtr + unwindIndex, unwindCodesEndPtr, isEpilog: true);

                    if (CheckCondition(ref context, headerWord >> 20) &&
                        offsetInFunction < scopeStart + scopeSize)
                    {

                        unwindCodePtr += unwindIndex;
                        skipHalfwords = offsetInFunction - scopeStart;
                        break;
                    }
                }
            }
        }

        //
        // Skip over unwind codes until we account for the number of halfwords
        // to skip.
        //
        while (unwindCodePtr < unwindCodesEndPtr && skipHalfwords > 0)
        {
            uint curCode = _target.Read<byte>(unwindCodePtr);
            if (curCode >= 0xfd)
                break;

            byte tableValue = UnwindOpTable[(int)curCode];
            skipHalfwords -= (uint)tableValue >> 4;
            unwindCodePtr += tableValue & 0xfu;
        }

        //
        // Now execute codes until we hit the end.
        //
        bool keepReading = true;
        while (unwindCodePtr < unwindCodesEndPtr && keepReading && status)
        {

            byte curCode = _target.Read<byte>(unwindCodePtr);
            unwindCodePtr++;

            //
            // 0x00-0x7f: 2-byte stack adjust ... add sp, sp, #0xval
            //
            if (curCode < 0x80)
            {
                context.Sp += (curCode & 0x7fu) * 4u;
            }

            //
            // 0x80-0xbf: 4-byte bitmasked pop ... pop {r0-r12, lr}
            //
            else if (curCode < 0xc0)
            {
                if (unwindCodePtr >= unwindCodesEndPtr)
                {
                    status = false;
                }
                else
                {
                    uint param = (uint)(((curCode & 0x20) << 9) |
                                        ((curCode & 0x1f) << 8) |
                                        _target.Read<byte>(unwindCodePtr));
                    unwindCodePtr++;
                    status = PopRegisterMask(ref context, (ushort)param);
                }
            }

            //
            // 0xc0-0xcf: 2-byte stack restore ... mov sp, rX
            //
            else if (curCode < 0xd0)
            {
                context.Sp = GetRegister(ref context, curCode & 0x0f);
            }

            else
            {
                uint param;
                switch (curCode)
                {

                    //
                    // 0xd0-0xd7: 2-byte range pop ... pop {r4-r7, lr}
                    //

                    case 0xd0:
                    case 0xd1:
                    case 0xd2:
                    case 0xd3:
                    case 0xd4:
                    case 0xd5:
                    case 0xd6:
                    case 0xd7:
                        status = PopRegisterMask(
                            ref context,
                            RangeToMask(4u, 4u + (curCode & 3u), curCode & 4u));
                        break;

                    //
                    // 0xd8-0xdf: 4-byte range pop ... pop {r4-r11, lr}
                    //

                    case 0xd8:
                    case 0xd9:
                    case 0xda:
                    case 0xdb:
                    case 0xdc:
                    case 0xdd:
                    case 0xde:
                    case 0xdf:
                        status = PopRegisterMask(
                            ref context,
                            RangeToMask(4u, 8u + (curCode & 3u), curCode & 4u));
                        break;

                    //
                    // 0xe0-0xe7: 4-byte range vpop ... vpop {d8-d15}
                    //

                    case 0xe0:
                    case 0xe1:
                    case 0xe2:
                    case 0xe3:
                    case 0xe4:
                    case 0xe5:
                    case 0xe6:
                    case 0xe7:
                        status = PopVfpRegisterRange(
                            ref context,
                            8u, 8u + (curCode & 0x07u));
                        break;

                    //
                    // 0xe8-0xeb: 4-byte stack adjust ... addw sp, sp, #0xval
                    //

                    case 0xe8:
                    case 0xe9:
                    case 0xea:
                    case 0xeb:
                        if (unwindCodePtr >= unwindCodesEndPtr)
                        {
                            status = false;
                            break;
                        }
                        context.Sp += 4u * 256u * (curCode & 3u);
                        context.Sp += 4u * _target.Read<byte>(unwindCodePtr);
                        unwindCodePtr++;
                        break;

                    //
                    // 0xec-0xed: 2-byte bitmasked pop ... pop {r0-r7,lr}
                    //

                    case 0xec:
                    case 0xed:
                        if (unwindCodePtr >= unwindCodesEndPtr)
                        {
                            status = false;
                            break;
                        }
                        status = PopRegisterMask(
                            ref context,
                            (ushort)(_target.Read<byte>(unwindCodePtr) | ((curCode << 14) & 0x4000u)));
                        unwindCodePtr++;
                        break;

                    //
                    // 0xee: 0-byte custom opcode
                    //

                    case 0xee:
                        if (unwindCodePtr >= unwindCodesEndPtr)
                        {
                            status = false;
                            break;
                        }
                        param = _target.Read<byte>(unwindCodePtr);
                        unwindCodePtr++;
                        if ((param & 0xf0) == 0x00)
                        {
                            status = UnwindCustom(
                                ref context,
                                (byte)(param & 0x0f));
                        }
                        else
                        {
                            status = false;
                        }
                        break;

                    //
                    // 0xef: 4-byte stack restore with post-increment ... ldr pc, [sp], #X
                    //
                    case 0xef:
                        if (unwindCodePtr >= unwindCodesEndPtr)
                        {
                            status = false;
                            break;
                        }
                        param = _target.Read<byte>(unwindCodePtr);
                        unwindCodePtr++;
                        if ((param & 0xf0) == 0x00)
                        {
                            status = PopRegisterMask(
                                ref context,
                                0x4000);
                            context.Sp += ((param & 15) - 1) * 4;
                        }
                        else
                        {
                            status = false;
                        }
                        break;

                    //
                    // 0xf5: 4-byte range vpop ... vpop {d0-d15}
                    //
                    case 0xf5:
                        if (unwindCodePtr >= unwindCodesEndPtr)
                        {
                            status = false;
                            break;
                        }
                        param = _target.Read<byte>(unwindCodePtr);
                        unwindCodePtr++;
                        status = PopVfpRegisterRange(
                            ref context,
                            param >> 4, param & 0x0f);
                        break;

                    //
                    // 0xf6: 4-byte range vpop ... vpop {d16-d31}
                    //
                    case 0xf6:
                        if (unwindCodePtr >= unwindCodesEndPtr)
                        {
                            status = false;
                            break;
                        }
                        param = _target.Read<byte>(unwindCodePtr);
                        unwindCodePtr++;
                        status = PopVfpRegisterRange(
                            ref context,
                            16 + (param >> 4), 16 + (param & 0x0f));
                        break;

                    //
                    // 0xf7: 2-byte stack adjust ... add sp, sp, <reg>
                    // 0xf9: 4-byte stack adjust ... add sp, sp, <reg>
                    //
                    case 0xf7:
                    case 0xf9:
                        if (unwindCodePtr + 2 > unwindCodesEndPtr)
                        {
                            status = false;
                            break;
                        }
                        context.Sp += 4u * 256u * _target.Read<byte>(unwindCodePtr);
                        context.Sp += 4u * _target.Read<byte>(unwindCodePtr + 1);
                        unwindCodePtr += 2;
                        break;

                    //
                    // 0xf8: 2-byte stack adjust ... add sp, sp, <reg>
                    // 0xfa: 4-byte stack adjust ... add sp, sp, <reg>
                    //
                    case 0xf8:
                    case 0xfa:
                        if (unwindCodePtr + 3 > unwindCodesEndPtr)
                        {
                            status = false;
                            break;
                        }
                        context.Sp += 4u * 256u * 256u * _target.Read<byte>(unwindCodePtr);
                        context.Sp += 4u * 256u * _target.Read<byte>(unwindCodePtr + 1);
                        context.Sp += 4u * _target.Read<byte>(unwindCodePtr + 2);
                        unwindCodePtr += 3;
                        break;

                    //
                    // 0xfb: 2-byte no-op/misc instruction
                    // 0xfc: 4-byte no-op/misc instruction
                    //
                    case 0xfb:
                    case 0xfc:
                        break;

                    //
                    // 0xfd: 2-byte end (epilog)
                    // 0xfe: 4-byte end (epilog)
                    // 0xff: generic end
                    //
                    case 0xfd:
                    case 0xfe:
                    case 0xff:
                        keepReading = false;
                        break;

                    default:
                        status = false;
                        break;
                }
            }
        }

        //
        // If we succeeded, post-process the results a bit
        //
        if (status)
        {

            //
            // Since we always POP to the LR, recover the final PC from there, unless
            // it was overwritten due to a special case custom unwinding operation.
            // Also set the establisher frame equal to the final stack pointer.
            //
            if ((context.ContextFlags & (uint)ContextFlagsValues.CONTEXT_UNWOUND_TO_CALL) != 0)
            {
                context.Pc = context.Lr;
            }
        }

        return status;
    }

    private unsafe bool UnwindCustom(
        ref ARMContext context,
        byte opcode)
    {
        ARM_CONTEXT_OFFSETS offsets;

        // Determine which set of offsets to use
        switch (opcode)
        {
            case 0:
                offsets = TrapFrameOffsets;
                break;
            case 1:
                offsets = MachineFrameOffsets;
                break;
            case 2:
                offsets = ContextOffsets;
                break;
            default:
                return false;
        }

        // Handle general registers first
        for (int regIndex = 0; regIndex < 13; regIndex++)
        {
            if (offsets.RegOffset[regIndex] != OFFSET_NONE)
            {
                TargetPointer sourceAddress = context.Sp + offsets.RegOffset[regIndex];
                SetRegister(ref context, regIndex, _target.Read<uint>(sourceAddress));
            }
        }

        for (int fpRegIndex = 0; fpRegIndex < 32; fpRegIndex++)
        {
            if (offsets.FpRegOffset[fpRegIndex] != OFFSET_NONE)
            {
                TargetPointer sourceAddress = context.Sp + offsets.FpRegOffset[fpRegIndex];
                context.D[fpRegIndex] = _target.Read<ulong>(sourceAddress);
            }
        }

        // Link register and PC next
        if (offsets.LrOffset != OFFSET_NONE)
        {
            TargetPointer sourceAddress = context.Sp + offsets.LrOffset;
            context.Lr = _target.Read<uint>(sourceAddress);
        }
        if (offsets.PcOffset != OFFSET_NONE)
        {
            TargetPointer sourceAddress = context.Sp + offsets.PcOffset;
            context.Pc = _target.Read<uint>(sourceAddress);

            //
            // If we pull the PC out of one of these, this means we are not
            // unwinding from a call, but rather from another frame.
            //
            context.ContextFlags &= ~(uint)ContextFlagsValues.CONTEXT_UNWOUND_TO_CALL;
        }

        // Finally the stack pointer
        if (offsets.SpOffset != OFFSET_NONE)
        {
            TargetPointer sourceAddress = context.Sp + offsets.SpOffset;
            context.Sp = _target.Read<uint>(sourceAddress);
        }
        else
        {
            context.Sp += offsets.TotalSize;
        }

        return true;
    }

    private bool UnwindCompact(
        ref ARMContext context,
        TargetPointer imageBase,
        Data.RuntimeFunction functionEntry)
    {
        uint controlPcRva = (uint)(context.Pc - imageBase.Value);
        bool status = true;

        //
        // Compact records always describe an unwind to a call.
        //
        context.ContextFlags |= (uint)ContextFlagsValues.CONTEXT_UNWOUND_TO_CALL;

        //
        // Extract the basic information about how to do a full unwind.
        //
        uint unwindData = functionEntry.UnwindData;
        uint functionLength = (unwindData >> 2) & 0x7ff;
        uint retBits = (unwindData >> 13) & 3;
        uint hBit = (unwindData >> 15) & 1;
        uint cBit = (unwindData >> 21) & 1;
        uint stackAdjust = (unwindData >> 22) & 0x3ff;

        //
        // Determine push/pop masks based on this information. This comes
        // from a mix of the C, L, R, and Reg fields.
        //
        uint vfpSaveCount = RegisterMaskLookup[(int)((unwindData >> 16) & 0x3f)];
        uint pushMask = vfpSaveCount & 0xffff;
        uint popMask = vfpSaveCount & 0xffff;
        vfpSaveCount >>= 16;

        //
        // If the stack adjustment is folded into the push/pop, encode this
        // by setting one of the low 4 bits of the push/pop mask and recovering
        // the actual stack adjustment.
        //
        if (stackAdjust >= 0x3f4)
        {
            pushMask |= stackAdjust & 4;
            popMask |= stackAdjust & 8;
            stackAdjust = (stackAdjust & 3) + 1;
        }

        //
        // If we're near the start of the function (within 9 halfwords),
        // see if we are within the prolog.
        //
        // N.B. If the low 2 bits of the UnwindData are 2, then we have
        // no prolog.
        //
        uint offsetInFunction = (controlPcRva - (functionEntry.BeginAddress & ~1u)) / 2;
        uint offsetInScope = 0;

        uint computeFramePointerLength = 0;
        uint pushPopParamsLength = 0;
        uint pushPopFloatingPointLength = 0;
        uint pushPopIntegerLength = 0;
        uint stackAdjustLength = 0;

        if (offsetInFunction < 9 && (unwindData & 3) != 2)
        {

            //
            // Compute sizes for each opcode in the prolog.
            //
            pushPopParamsLength = (hBit != 0) ? 1u : 0u;
            pushPopIntegerLength = (pushMask == 0) ? 0u :
                                   ((pushMask & 0xbf00) == 0) ? 1u : 2u;
            computeFramePointerLength = (cBit == 0) ? 0u :
                                        ((pushMask & ~0x4800) == 0) ? 1u : 2u;
            pushPopFloatingPointLength = (vfpSaveCount != 0) ? 2u : 0u;
            stackAdjustLength = (stackAdjust == 0 || (pushMask & 4) != 0) ? 0u :
                                (stackAdjust < 0x80) ? 1u : 2u;

            //
            // Compute the total prolog length and determine if we are within
            // its scope.
            //
            // N.B. We must execute prolog operations backwards to unwind, so
            // our final scope offset in this case is the distance from the end.
            //

            uint prologLength = pushPopParamsLength +
                           pushPopIntegerLength +
                           computeFramePointerLength +
                           pushPopFloatingPointLength +
                           stackAdjustLength;

            if (offsetInFunction < prologLength)
            {
                offsetInScope = prologLength - offsetInFunction;
            }
        }

        //
        // If we're near the end of the function (within 8 halfwords), see if
        // we are within the epilog.
        //
        // N.B. If Ret == 3, then we have no epilog.
        //
        if (offsetInScope == 0 && offsetInFunction + 8 >= functionLength && retBits != 3)
        {

            //
            // Compute sizes for each opcode in the epilog.
            //
            stackAdjustLength = (stackAdjust == 0 || (popMask & 8) != 0) ? 0u :
                                (stackAdjust < 0x80) ? 1u : 2u;
            pushPopFloatingPointLength = (vfpSaveCount != 0) ? 2u : 0u;
            computeFramePointerLength = 0;
            pushPopIntegerLength = (popMask == 0 || (hBit != 0 && retBits == 0 && popMask == 0x8000)) ? 0u :
                                   ((popMask & 0x7f00) == 0) ? 1u : 2u;
            pushPopParamsLength = (hBit == 0) ? 0 : (retBits == 0) ? 2u : 1u;
            uint returnLength = retBits;

            //
            // Compute the total epilog length and determine if we are within
            // its scope.
            //

            uint epilogLength = stackAdjustLength +
                           pushPopFloatingPointLength +
                           pushPopIntegerLength +
                           pushPopParamsLength +
                           returnLength;

            uint scopeStart = functionLength - epilogLength;
            if (offsetInFunction > scopeStart)
            {
                offsetInScope = offsetInFunction - scopeStart;
                pushMask = popMask & 0x1fff;
                if (hBit == 0)
                {
                    pushMask |= (popMask >> 1) & 0x4000;
                }
            }
        }

        //
        // Process operations backwards, in the order: stack deallocation,
        // VFP register popping, integer register popping, parameter home
        // area recovery.
        //
        // First case is simple: we process everything with no regard for
        // the current offset within the scope.
        //
        if (offsetInScope == 0)
        {

            context.Sp += 4 * stackAdjust;
            if (vfpSaveCount != 0)
            {
                status = PopVfpRegisterRange(ref context, 8, 8 + vfpSaveCount - 1);
            }
            pushMask &= 0xfff0;
            if (pushMask != 0)
            {
                status = PopRegisterMask(ref context, (ushort)pushMask);
            }
            if (hBit != 0)
            {
                context.Sp += 4 * 4;
            }
        }

        //
        // Second case is more complex: we must step along each operation
        // to ensure it should be executed.
        //

        else
        {

            uint currentOffset = 0;
            if (currentOffset >= offsetInScope && stackAdjustLength != 0)
            {
                context.Sp += 4 * stackAdjust;
            }
            currentOffset += stackAdjustLength;

            if (currentOffset >= offsetInScope && pushPopFloatingPointLength != 0)
            {
                status = PopVfpRegisterRange(ref context, 8, 8 + vfpSaveCount - 1);
            }
            currentOffset += pushPopFloatingPointLength;

            //
            // N.B. We don't need to undo any side effects of frame pointer linkage
            //

            currentOffset += computeFramePointerLength;

            //
            // N.B. In the epilog case above, we copied PopMask to PushMask
            //

            if (currentOffset >= offsetInScope && pushPopIntegerLength != 0)
            {
                pushMask &= 0xfff0;
                status = PopRegisterMask(ref context, (ushort)pushMask);
                if (stackAdjustLength == 0)
                {
                    context.Sp += 4 * stackAdjust;
                }
            }
            currentOffset += pushPopIntegerLength;

            //
            // N.B. In the epilog case, we also need to pop the return address
            //

            if (currentOffset >= offsetInScope && pushPopParamsLength != 0)
            {
                if (pushPopParamsLength == 2)
                {
                    status = PopRegisterMask(ref context, 1 << 14);
                }
                context.Sp += 4 * 4;
            }
        }

        //
        // If we succeeded, post-process the results a bit
        //

        if (status)
        {

            //
            // Since we always POP to the LR, recover the final PC from there.
            // Also set the establisher frame equal to the final stack pointer.
            //

            context.Pc = context.Lr;
        }

        return status;
    }

    #endregion
    #region Unwind Helpers

    private uint ComputeScopeSize(
        TargetPointer unwindCodePtr,
        TargetPointer unwindCodesEndPtr,
        bool isEpilog)
    {
        //
        // Iterate through the unwind codes until we hit an end marker.
        // While iterating, accumulate the total scope size.
        //
        uint scopeSize = 0;
        byte opcode = _target.Read<byte>(unwindCodePtr);
        while (unwindCodePtr < unwindCodesEndPtr && opcode < 0xfd)
        {
            byte tableValue = UnwindOpTable[opcode];
            scopeSize += (uint)tableValue >> 4;
            unwindCodePtr += tableValue & 0xfu;
            opcode = _target.Read<byte>(unwindCodePtr);
        }

        //
        // Handle the special epilog-only end codes.
        //
        if (opcode >= 0xfd && opcode <= 0xfe && isEpilog)
        {
            scopeSize += opcode - 0xfcu;
        }
        return scopeSize;
    }

    private static bool CheckCondition(
        ref ARMContext context,
        uint condition)
    {
        int value = (ConditionTable[(int)condition & 0xf] >> (int)(context.Cpsr >> 28)) & 1;
        return value != 0;
    }

    private static ushort RangeToMask(uint start, uint stop, uint lr)
    {
        ushort mask = 0;
        if (start <= stop)
        {
            mask |= (ushort)(((1 << (int)(stop + 1)) - 1) - ((1 << (int)start) - 1));
        }
        if (lr != 0)
        {
            mask |= 1 << 14;
        }
        return mask;
    }

    private unsafe bool PopVfpRegisterRange(
        ref ARMContext context,
        uint regStart,
        uint regStop)
    {
        for (uint regIndex = regStart; regIndex <= regStop; regIndex++)
        {
            context.D[regIndex] = _target.Read<ulong>(context.Sp);
            context.Sp += 8;
        }
        return true;
    }

    private bool PopRegisterMask(
        ref ARMContext context,
        ushort regMask)
    {
        // Pop each register in sequence
        for (int regIndex = 0; regIndex < 15; regIndex++)
        {
            if ((regMask & (1 << regIndex)) != 0)
            {
                SetRegister(ref context, regIndex, _target.Read<uint>(context.Sp));
                context.Sp += 4;
            }
        }

        // If we popped LR, move it to the PC.
        if ((regMask & 0x4000) != 0)
        {
            context.Pc = context.Lr;
        }
        return true;
    }

    private static void SetRegister(ref ARMContext context, int regIndex, uint value)
    {
        switch (regIndex)
        {
            case 0: context.R0 = value; break;
            case 1: context.R1 = value; break;
            case 2: context.R2 = value; break;
            case 3: context.R3 = value; break;
            case 4: context.R4 = value; break;
            case 5: context.R5 = value; break;
            case 6: context.R6 = value; break;
            case 7: context.R7 = value; break;
            case 8: context.R8 = value; break;
            case 9: context.R9 = value; break;
            case 10: context.R10 = value; break;
            case 11: context.R11 = value; break;
            case 12: context.R12 = value; break;
            case 13: context.Sp = value; break;
            case 14: context.Lr = value; break;
            case 15: context.Pc = value; break;
            case 16: context.Cpsr = value; break;
            default: throw new ArgumentOutOfRangeException(nameof(regIndex));
        }
    }

    private static uint GetRegister(ref ARMContext context, int regIndex) =>
    regIndex switch
    {
        0 => context.R0,
        1 => context.R1,
        2 => context.R2,
        3 => context.R3,
        4 => context.R4,
        5 => context.R5,
        6 => context.R6,
        7 => context.R7,
        8 => context.R8,
        9 => context.R9,
        10 => context.R10,
        11 => context.R11,
        12 => context.R12,
        13 => context.Sp,
        14 => context.Lr,
        15 => context.Pc,
        16 => context.Cpsr,
        _ => throw new ArgumentOutOfRangeException(nameof(regIndex)),
    };

    #endregion
}
