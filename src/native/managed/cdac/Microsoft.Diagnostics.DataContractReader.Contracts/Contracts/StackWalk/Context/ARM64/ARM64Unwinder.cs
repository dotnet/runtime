// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using static Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers.ARM64Context;

namespace Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers.ARM64;

internal class ARM64Unwinder(Target target)
{
    #region Constants

    /// <summary>
    /// This table describes the size of each unwind code, in bytes, for unwind codes
    /// in the range 0xE0-0xFF.
    /// </summary>
    private static ReadOnlySpan<byte> UnwindCodeSizeTable =>
    [
        4, 1, 2, 1, 1, 1, 1, 3,
        1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1,
        2, 3, 4, 5, 1, 1, 1, 1,
    ];

    /// <summary>
    // This table describes the number of instructions represented by each unwind
    // code in the range 0xE0-0xFF.
    /// </summary>
    private static ReadOnlySpan<byte> UnwindCodeInstructionCountTable =>
    [
        1, 1, 1, 1, 1, 1, 1, 1,    // 0xE0-0xE7
        0,                         // 0xE8 - MSFT_OP_TRAP_FRAME
        0,                         // 0xE9 - MSFT_OP_MACHINE_FRAME
        0,                         // 0xEA - MSFT_OP_CONTEXT
        0,                         // 0xEB - MSFT_OP_EC_CONTEXT / MSFT_OP_RET_TO_GUEST (unused)
        0,                         // 0xEC - MSFT_OP_CLEAR_UNWOUND_TO_CALL
        0,                         // 0XED - MSFT_OP_RET_TO_GUEST_LEAF (unused)
        0, 0,                      // 0xEE-0xEF
        0, 0, 0, 0, 0, 0, 0, 0,    // 0xF0-0xF7
        1, 1, 1, 1, 1, 1, 1, 1,    // 0xF8-0xFF
    ];

    #endregion

    private readonly Target _target = target;
    private readonly IExecutionManager _eman = target.Contracts.ExecutionManager;

    #region Entrypoint

    public bool Unwind(ref ARM64Context context)
    {
        if (_eman.GetCodeBlockHandle(context.InstructionPointer.Value) is not CodeBlockHandle cbh)
            return false;

        TargetPointer imageBase = _eman.GetUnwindInfoBaseAddress(cbh);
        Data.RuntimeFunction functionEntry = _target.ProcessedData.GetOrAdd<Data.RuntimeFunction>(_eman.GetUnwindInfo(cbh));

        ulong startingPc = context.Pc;
        ulong startingSp = context.Sp;

        bool status = VirtualUnwind(ref context, imageBase, functionEntry);

        //
        // If we fail the unwind, clear the PC to 0. This is recognized by
        // many callers as a failure, given that RtlVirtualUnwind does not
        // return a status code.
        //

        if (!status)
        {
            context.Pc = 0;
        }

        // PC == 0 means unwinding is finished.
        // Same if no forward progress is made
        if (context.Pc == 0 || (startingPc == context.Pc && startingSp == context.Sp))
            return false;

        return true;
    }

    #endregion
    #region Unwinder

    private bool VirtualUnwind(ref ARM64Context context, TargetPointer imageBase, Data.RuntimeFunction functionEntry)
    {
        // FunctionEntry could be null if the function is a pure leaf/trivial function.
        // This is not a valid case for managed code and not handled here.

        bool status = true;
        uint controlPcRva;
        uint unwindType = functionEntry.UnwindData & 0x3u;

        //
        // Unwind type 3 refers to a chained record. The top 30 bits of the
        // unwind data contains the RVA of the parent pdata record.
        //
        if (unwindType == 3)
        {
            if ((functionEntry.UnwindData & 4) == 0)
            {
                functionEntry = _target.ProcessedData.GetOrAdd<Data.RuntimeFunction>(imageBase + functionEntry.UnwindData - 3);
                unwindType = functionEntry.UnwindData & 3u;

                UnwinderAssert(unwindType != 3);

                controlPcRva = functionEntry.BeginAddress;

            }
            else
            {
                // unsupported version
                return false;
            }

        }
        else
        {
            controlPcRva = (uint)(context.Pc - imageBase);
        }

        //
        // Identify the compact .pdata format versus the full .pdata+.xdata format.
        //
        if (unwindType != 0)
        {
            // managed code does not use compact .pdata format.
            UnwinderAssert(false, "Compact .pdata format is not currently supported.");
        }
        else
        {

            status = VirtualUnwindFull(ref context, controlPcRva, imageBase, functionEntry);
        }

        return status;
    }

    private bool VirtualUnwindFull(
        ref ARM64Context context,
        uint controlPcRva,
        TargetPointer imageBase,
        Data.RuntimeFunction functionEntry)
    {
        //
        // Unless a special frame is encountered, assume that any unwinding
        // will return us to the return address of a call and set the flag
        // appropriately (it will be cleared again if the special cases apply).
        //
        context.ContextFlags |= (uint)ContextFlagsValues.CONTEXT_UNWOUND_TO_CALL;

        //
        // By default, unwinding is done by popping to the LR, then copying
        // that LR to the PC. However, some special opcodes require different
        // behavior.
        //
        bool finalPcFromLr = true;

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
        {
            // unsupported version
            return false;
        }

        uint functionLength = headerWord & 0x3ffff;
        uint offsetInFunction = (controlPcRva - functionEntry.BeginAddress) / 4;

        //
        // Determine the number of epilog scope records and the maximum number
        // of unwind codes.
        //
        uint unwindWords = (headerWord >> 27) & 31;
        uint epilogScopeCount = (headerWord >> 22) & 31;
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
        // Exception data is not supported in this implementation and is not used by managed code.
        // If it were, it should be extracted here.
        //

        //
        // Unless we are in a prolog/epilog, we execute the unwind codes
        // that immediately follow the epilog scope list.
        //

        TargetPointer unwindCodePtr = unwindDataPtr + 4 * epilogScopeCount;
        TargetPointer unwindCodesEndPtr = unwindCodePtr + 4 * unwindWords;
        uint skipWords = 0;

        //
        // If we're near the start of the function, and this function has a prolog,
        // compute the size of the prolog from the unwind codes. If we're in the
        // midst of it, we still execute starting at unwind code index 0, but we may
        // need to skip some to account for partial execution of the prolog.
        //
        // N.B. As an optimization here, note that each byte of unwind codes can
        //      describe at most one 32-bit instruction. Thus, the largest prologue
        //      that could possibly be described by UnwindWords (which is 4 * the
        //      number of unwind code bytes) is 4 * UnwindWords words. If
        //      OffsetInFunction is larger than this value, it is guaranteed to be
        //      in the body of the function.
        //
        uint scopeSize;
        if (offsetInFunction < 4 * unwindWords)
        {
            scopeSize = ComputeScopeSize(unwindCodePtr, unwindCodesEndPtr, isEpilog: false);

            if (offsetInFunction < scopeSize)
            {
                skipWords = scopeSize - offsetInFunction;
            }
        }

        if (skipWords > 0)
        {
            // Found that we are in the middle of a prolog, no need to check for epilog scopes
        }

        //
        // We're not in the prolog, now check to see if we are in the epilog.
        // In the simple case, the 'E' bit is set indicating there is a single
        // epilog that lives at the end of the function. If we're near the end
        // of the function, compute the actual size of the epilog from the
        // unwind codes. If we're in the midst of it, adjust the unwind code
        // pointer to the start of the codes and determine how many we need to skip.
        //
        // N.B. Similar to the prolog case above, the maximum number of halfwords
        //      that an epilog can cover is limited by UnwindWords. In the epilog
        //      case, however, the starting index within the unwind code table is
        //      non-zero, and so the maximum number of unwind codes that can pertain
        //      to an epilog is (UnwindWords * 4 - UnwindIndex), thus further
        //      constraining the bounds of the epilog.
        //
        else if ((headerWord & (1 << 21)) != 0)
        {
            if (offsetInFunction + (4 * unwindWords - unwindIndex) >= functionLength)
            {
                scopeSize = ComputeScopeSize(unwindCodePtr + unwindIndex, unwindCodesEndPtr, isEpilog: true);
                uint scopeStart = functionLength - scopeSize;

                //
                // N.B. This code assumes that no handleable exceptions can occur in
                //      the prolog or in a chained shrink-wrapping prolog region.
                //
                if (offsetInFunction >= scopeStart)
                {
                    unwindCodePtr += unwindIndex;
                    skipWords = offsetInFunction - scopeStart;
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
                    break;

                unwindIndex = headerWord >> 22;
                if (offsetInFunction < scopeStart + (4 * unwindWords - unwindIndex))
                {
                    scopeSize = ComputeScopeSize(unwindCodePtr + unwindIndex, unwindCodesEndPtr, isEpilog: true);

                    if (offsetInFunction < scopeStart + scopeSize)
                    {
                        unwindCodePtr += unwindIndex;
                        skipWords = offsetInFunction - scopeStart;
                        break;
                    }
                }
            }
        }

        //
        // Skip over unwind codes until we account for the number of halfwords
        // to skip.
        //
        while (unwindCodePtr < unwindCodesEndPtr && skipWords > 0)
        {
            byte curCode = _target.Read<byte>(unwindCodePtr);
            if (OPCODE_IS_END(curCode))
                break;

            unwindCodePtr += GetUnwindCodeSize(curCode);
            skipWords--;
        }

        //
        // Now execute codes until we hit the end.
        //
        bool status = true;
        uint accumulatedSaveNexts = 0;
        while (unwindCodePtr < unwindCodesEndPtr && status)
        {
            byte curCode = _target.Read<byte>(unwindCodePtr);
            unwindCodePtr += 1;

            //
            // alloc_s (000xxxxx): allocate small stack with size < 1024 (2^5 * 16)
            //
            if (curCode <= 0x1f)
            {
                if (accumulatedSaveNexts != 0)
                {
                    // invalid sequence
                    return false;
                }
                context.Sp += 16u * (curCode & 0x1fu);
            }

            //
            // save_r19r20_x (001zzzzz): save <r19,r20> pair at [sp-#Z*8]!, pre-indexed offset >= -248
            //
            else if (curCode <= 0x3f)
            {
                status = RestoreRegisterRange(
                            ref context,
                            -8 * (curCode & 0x1f),
                            19,
                            2 + (2 * accumulatedSaveNexts));
                accumulatedSaveNexts = 0;
            }

            //
            // save_fplr (01zzzzzz): save <r29,lr> pair at [sp+#Z*8], offset <= 504
            //
            else if (curCode <= 0x7f)
            {
                if (accumulatedSaveNexts != 0)
                {
                    // invalid sequence
                    return false;
                }
                status = RestoreRegisterRange(
                            ref context,
                            8 * (curCode & 0x3f),
                            29,
                            2);
            }

            //
            // save_fplr_x (10zzzzzz): save <r29,lr> pair at [sp-(#Z+1)*8]!, pre-indexed offset >= -512
            //
            else if (curCode <= 0xbf)
            {
                if (accumulatedSaveNexts != 0)
                {
                    // invalid sequence
                    return false;
                }
                status = RestoreRegisterRange(
                            ref context,
                            -8 * ((curCode & 0x3f) + 1),
                            29,
                            2);
            }

            //
            // alloc_m (11000xxx|xxxxxxxx): allocate large stack with size < 32k (2^11 * 16).
            //
            else if (curCode <= 0xc7)
            {
                if (accumulatedSaveNexts != 0)
                {
                    // invalid sequence
                    return false;
                }
                context.Sp += 16u * ((curCode & 7u) << 8);
                context.Sp += 16u * _target.Read<byte>(unwindCodePtr);
                unwindCodePtr++;
            }

            //
            // save_regp (110010xx|xxzzzzzz): save r(19+#X) pair at [sp+#Z*8], offset <= 504
            //
            else if (curCode <= 0xcb)
            {
                byte nextCode = _target.Read<byte>(unwindCodePtr);
                unwindCodePtr++;
                status = RestoreRegisterRange(
                            ref context,
                            8 * (nextCode & 0x3f),
                            19u + ((curCode & 3u) << 2) + (uint)(nextCode >>> 6),
                            2 + (2 * accumulatedSaveNexts));
                accumulatedSaveNexts = 0;
            }

            //
            // save_regp_x (110011xx|xxzzzzzz): save pair r(19+#X) at [sp-(#Z+1)*8]!, pre-indexed offset >= -512
            //
            else if (curCode <= 0xcf)
            {
                byte nextCode = _target.Read<byte>(unwindCodePtr);
                unwindCodePtr++;
                status = RestoreRegisterRange(
                            ref context,
                            -8 * ((nextCode & 0x3f) + 1),
                            19u + ((curCode & 3u) << 2) + (uint)(nextCode >>> 6),
                            2 + (2 * accumulatedSaveNexts));
                accumulatedSaveNexts = 0;
            }

            //
            // save_reg (110100xx|xxzzzzzz): save reg r(19+#X) at [sp+#Z*8], offset <= 504
            //
            else if (curCode <= 0xd3)
            {
                if (accumulatedSaveNexts != 0)
                {
                    // invalid sequence
                    return false;
                }
                byte nextCode = _target.Read<byte>(unwindCodePtr);
                unwindCodePtr++;
                status = RestoreRegisterRange(
                            ref context,
                            8 * (nextCode & 0x3f),
                            19u + ((curCode & 3u) << 2) + (uint)(nextCode >> 6),
                            1);
            }

            //
            // save_reg_x (1101010x|xxxzzzzz): save reg r(19+#X) at [sp-(#Z+1)*8]!, pre-indexed offset >= -256
            //
            else if (curCode <= 0xd5)
            {
                if (accumulatedSaveNexts != 0)
                {
                    // invalid sequence
                    return false;
                }
                byte nextCode = _target.Read<byte>(unwindCodePtr);
                unwindCodePtr++;
                status = RestoreRegisterRange(
                            ref context,
                            -8 * ((nextCode & 0x1f) + 1),
                            19u + ((curCode & 1u) << 3) + (uint)(nextCode >>> 5),
                            1);
            }

            //
            // save_lrpair (1101011x|xxzzzzzz): save pair <r19+2*#X,lr> at [sp+#Z*8], offset <= 504
            //
            else if (curCode <= 0xd7)
            {
                if (accumulatedSaveNexts != 0)
                {
                    // invalid sequence
                    return false;
                }
                byte nextCode = _target.Read<byte>(unwindCodePtr);
                unwindCodePtr++;
                status = RestoreRegisterRange(
                            ref context,
                            8 * (nextCode & 0x3f),
                            19u + 2 * (((curCode & 1u) << 2) + (uint)(nextCode >>> 6)),
                            1);
                if (status)
                {
                    RestoreRegisterRange(
                            ref context,
                            8 * (nextCode & 0x3f) + 8,
                            30,
                            1);
                }
            }

            //
            // save_fregp (1101100x|xxzzzzzz): save pair d(8+#X) at [sp+#Z*8], offset <= 504
            //
            else if (curCode <= 0xd9)
            {
                byte nextCode = _target.Read<byte>(unwindCodePtr);
                unwindCodePtr++;
                status = RestoreFpRegisterRange(
                            ref context,
                            8 * (nextCode & 0x3f),
                            8u + ((curCode & 1u) << 2) + (uint)(nextCode >>> 6),
                            2 + (2 * accumulatedSaveNexts));
                accumulatedSaveNexts = 0;
            }

            //
            // save_fregp_x (1101101x|xxzzzzzz): save pair d(8+#X), at [sp-(#Z+1)*8]!, pre-indexed offset >= -512
            //
            else if (curCode <= 0xdb)
            {
                byte nextCode = _target.Read<byte>(unwindCodePtr);
                unwindCodePtr++;
                status = RestoreFpRegisterRange(
                            ref context,
                            -8 * ((nextCode & 0x3f) + 1),
                            8u + ((curCode & 1u) << 2) + (uint)(nextCode >>> 6),
                            2 + (2 * accumulatedSaveNexts));
                accumulatedSaveNexts = 0;
            }

            //
            // save_freg (1101110x|xxzzzzzz): save reg d(9+#X) at [sp+#Z*8], offset <= 504
            //
            else if (curCode <= 0xdd)
            {
                if (accumulatedSaveNexts != 0)
                {
                    // invalid sequence
                    return false;
                }
                byte nextCode = _target.Read<byte>(unwindCodePtr);
                unwindCodePtr++;
                status = RestoreFpRegisterRange(
                            ref context,
                            8 * (nextCode & 0x3f),
                            8u + ((curCode & 1u) << 2) + (uint)(nextCode >>> 6),
                            1);
            }

            //
            // save_freg_x (11011110|xxxzzzzz): save reg d(8+#X) at [sp-(#Z+1)*8]!, pre-indexed offset >= -256
            //
            else if (curCode == 0xde)
            {
                if (accumulatedSaveNexts != 0)
                {
                    // invalid sequence
                    return false;
                }
                byte nextCode = _target.Read<byte>(unwindCodePtr);
                unwindCodePtr++;
                status = RestoreFpRegisterRange(
                            ref context,
                            -8 * ((nextCode & 0x1f) + 1),
                            8 + (uint)(nextCode >>> 5),
                            1);
            }

            //
            // alloc_l (11100000|xxxxxxxx|xxxxxxxx|xxxxxxxx): allocate large stack with size < 256M
            //
            else if (curCode == 0xe0)
            {
                if (accumulatedSaveNexts != 0)
                {
                    // invalid sequence
                    return false;
                }
                context.Sp += 16u * ((uint)_target.Read<byte>(unwindCodePtr) << 16);
                unwindCodePtr++;
                context.Sp += 16 * ((uint)_target.Read<byte>(unwindCodePtr) << 8);
                unwindCodePtr++;
                context.Sp += 16 * (uint)_target.Read<byte>(unwindCodePtr);
                unwindCodePtr++;
            }

            //
            // set_fp (11100001): set up r29: with: mov r29,sp
            //
            else if (curCode == 0xe1)
            {
                if (accumulatedSaveNexts != 0)
                {
                    // invalid sequence
                    return false;
                }
                context.Sp = context.Fp;
            }

            //
            // add_fp (11100010|xxxxxxxx): set up r29 with: add r29,sp,#x*8
            //
            else if (curCode == 0xe2)
            {
                if (accumulatedSaveNexts != 0)
                {
                    // invalid sequence
                    return false;
                }
                context.Sp = context.Fp - 8u * _target.Read<byte>(unwindCodePtr);
                unwindCodePtr++;
            }

            //
            // nop (11100011): no unwind operation is required
            //
            else if (curCode == 0xe3)
            {
                if (accumulatedSaveNexts != 0)
                {
                    // invalid sequence
                    return false;
                }
            }

            //
            // end (11100100): end of unwind code
            //
            else if (curCode == 0xe4)
            {
                if (accumulatedSaveNexts != 0)
                {
                    // invalid sequence
                    return false;
                }

                break;
            }

            //
            // end_c (11100101): end of unwind code in current chained scope.
            //          Continue unwinding parent scope.
            //
            else if (curCode == 0xe5)
            {
                // no-op
            }

            //
            // save_next_pair (11100110): save next non-volatile Int or FP register pair.
            //
            else if (curCode == 0xe6)
            {
                accumulatedSaveNexts += 1;
            }

            //
            //      11100111 ' 0pxrrrrr ' ffoooooo
            //      p: 0/1 - single/pair
            //      x: 0/1 - positive offset / negative offset with writeback
            //      r: register number
            //      f: 00/01/10 - X / D / Q
            //      o: offset * 16 for x=1 or p=1 or f=Q / else offset * 8
            //
            else if (curCode == 0xe7)
            {
                byte val2 = _target.Read<byte>(unwindCodePtr);
                unwindCodePtr += 1;
                byte val1 = _target.Read<byte>(unwindCodePtr);
                unwindCodePtr += 1;
                SaveAnyUnwindCode op = new SaveAnyUnwindCode(val1, val2);

                //
                // save_next_pair only permited for pairs.
                //
                if ((op.p == 0) && accumulatedSaveNexts != 0)
                {
                    // invalid sequence
                    return false;
                }

                if (op.fixedOp != 0)
                {
                    // invalid sequence
                    return false;
                }

                int spOffset = op.o + op.x;
                spOffset *= ((op.x == 1) || (op.f == 2) || (op.p == 1)) ? 16 : 8;
                spOffset *= op.x == 1 ? -1 : 1;
                uint regCount = 1u + op.p + (2u * accumulatedSaveNexts);
                switch (op.f)
                {
                    case 0:
                        status = RestoreRegisterRange(
                                    ref context,
                                    spOffset,
                                    op.r,
                                    regCount);
                        break;

                    case 1:
                        status = RestoreFpRegisterRange(
                                    ref context,
                                    spOffset,
                                    op.r,
                                    regCount);
                        break;

                    case 2:
                        status = RestoreSimdRegisterRange(
                                    ref context,
                                    spOffset,
                                    op.r,
                                    regCount);
                        break;

                    default:
                        // invalid sequence
                        return false;
                }

                accumulatedSaveNexts = 0;
            }

            //
            // custom_0 (111010xx): restore custom structure
            //
            else if (curCode >= 0xe8 && curCode <= 0xec)
            {
                if (accumulatedSaveNexts != 0)
                {
                    // invalid sequence
                    return false;
                }

                status = UnwindCustom(ref context, curCode);
                finalPcFromLr = false;
            }

            //
            // pac (11111100): function has pointer authentication
            //
            else if (curCode == 0xfc)
            {
                if (accumulatedSaveNexts != 0)
                {
                    // invalid sequence
                    return false;
                }

                //
                // TODO: Implement support for UnwindFlags RTL_VIRTUAL_UNWIND2_VALIDATE_PAC.
                //
            }

            //
            // future/nop: the following ranges represent encodings reserved for
            //      future extension. They are treated as a nop and, therefore, no
            //      unwind action is taken.
            //
            //      11111000|yyyyyyyy
            //      11111001|yyyyyyyy|yyyyyyyy
            //      11111010|yyyyyyyy|yyyyyyyy|yyyyyyyy
            //      11111011|yyyyyyyy|yyyyyyyy|yyyyyyyy|yyyyyyyy
            //      111111xx
            //
            else if (curCode >= 0xf8)
            {
                if (accumulatedSaveNexts != 0)
                {
                    // invalid sequence
                    return false;
                }

                if (curCode <= 0xfb)
                {
                    unwindCodePtr += 1u + (curCode & 0x3u);
                }
            }

            //
            // Anything else is invalid
            //
            else
            {
                // invalid sequence
                return false;
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
            if (finalPcFromLr)
            {
                context.Pc = context.Lr;
            }
        }

        return status;
    }

    private unsafe bool UnwindCustom(
        ref ARM64Context context,
        byte customCode)
    {
        ulong startingSp = context.Sp;

        switch (customCode)
        {
            //
            // Trap frame case
            //
            case 0xE8: // MSFT_OP_TRAP_FRAME:
            {
                //
                // Restore X0-X18, and D0-D7
                //
                TargetPointer sourceAddress = startingSp + /*offsetof(ARM64_KTRAP_FRAME, X)*/ 0x0A0;
                for (uint regIndex = 0; regIndex < 19; regIndex++)
                {
                    SetRegister(ref context, regIndex, _target.Read<ulong>(sourceAddress));
                    sourceAddress += sizeof(ulong);
                }

                sourceAddress = startingSp + /*offsetof(ARM64_KTRAP_FRAME, VfpState)*/ 0x010;
                TargetPointer vfpStateAddress = _target.Read<ulong>(sourceAddress);
                if (vfpStateAddress != 0)
                {
                    sourceAddress = vfpStateAddress + /*offsetof(KARM64_VFP_STATE, Fpcr)*/ 0x08;
                    uint Fpcr = _target.Read<uint>(sourceAddress);
                    sourceAddress = vfpStateAddress + /*offsetof(KARM64_VFP_STATE, Fpcr)*/ 0x0C;
                    uint Fpsr = _target.Read<uint>(sourceAddress);
                    if (Fpcr != uint.MaxValue && Fpsr != uint.MaxValue)
                    {
                        context.Fpcr = Fpcr;
                        context.Fpsr = Fpsr;

                        sourceAddress = vfpStateAddress + /*offsetof(KARM64_VFP_STATE, V)*/ 0x10;
                        for (uint regIndex = 0; regIndex < 32; regIndex++)
                        {
                            context.V[regIndex * 2] = _target.Read<ulong>(sourceAddress);
                            context.V[(regIndex * 2) + 1] = _target.Read<ulong>(sourceAddress + 8);
                            sourceAddress += 2 * sizeof(ulong);
                        }
                    }
                }

                //
                // Restore R11, R12, SP, LR, PC, and the status registers
                //
                sourceAddress = startingSp + /*offsetof(ARM64_KTRAP_FRAME, Spsr)*/ 0x090;
                context.Cpsr = _target.Read<uint>(sourceAddress);

                sourceAddress = startingSp + /*offsetof(ARM64_KTRAP_FRAME, Sp)*/ 0x098;
                context.Sp = _target.Read<ulong>(sourceAddress);

                sourceAddress = startingSp + /*offsetof(ARM64_KTRAP_FRAME, Lr)*/ 0x138;
                context.Lr = _target.Read<ulong>(sourceAddress);

                sourceAddress = startingSp + /*offsetof(ARM64_KTRAP_FRAME, Fp)*/ 0x140;
                context.Fp = _target.Read<ulong>(sourceAddress);

                sourceAddress = startingSp + /*offsetof(ARM64_KTRAP_FRAME, Pc)*/ 0x148;
                context.Pc = _target.Read<ulong>(sourceAddress);

                //
                // Clear the unwound-to-call flag
                //
                context.ContextFlags &= (uint)~ContextFlagsValues.CONTEXT_UNWOUND_TO_CALL;
                break;
            }

            //
            // Machine frame case
            //
            case 0xE9:  // MSFT_OP_MACHINE_FRAME:
            {
                //
                // Restore the SP and PC, and clear the unwound-to-call flag
                //
                context.Sp = _target.Read<ulong>(startingSp + 0);
                context.Pc = _target.Read<ulong>(startingSp + 8);
                context.ContextFlags &= (uint)~ContextFlagsValues.CONTEXT_UNWOUND_TO_CALL;
                break;
            }

            //
            // Context case
            //
            case 0xEA:  // MSFT_OP_CONTEXT:
            {
                //
                // Restore X0-X28, and D0-D31
                //
                TargetPointer sourceAddress = startingSp + (uint)Marshal.OffsetOf<ARM64Context>(nameof(ARM64Context.X0));
                for (uint regIndex = 0; regIndex < 29; regIndex++)
                {
                    SetRegister(ref context, regIndex, _target.Read<ulong>(sourceAddress));
                    sourceAddress += sizeof(ulong);
                }

                sourceAddress = startingSp + (uint)Marshal.OffsetOf<ARM64Context>(nameof(ARM64Context.V));
                for (uint regIndex = 0; regIndex < 32; regIndex++)
                {
                    context.V[regIndex * 2] = _target.Read<ulong>(sourceAddress);
                    context.V[(regIndex * 2) + 1] = _target.Read<ulong>(sourceAddress + 8);
                    sourceAddress += 2 * sizeof(ulong);
                }

                //
                // Restore SP, LR, PC, and the status registers
                //
                sourceAddress = startingSp + (uint)Marshal.OffsetOf<ARM64Context>(nameof(ARM64Context.Cpsr));
                context.Cpsr = _target.Read<uint>(sourceAddress);

                sourceAddress = startingSp + (uint)Marshal.OffsetOf<ARM64Context>(nameof(ARM64Context.Fp));
                context.Fp = _target.Read<ulong>(sourceAddress);

                sourceAddress = startingSp + (uint)Marshal.OffsetOf<ARM64Context>(nameof(ARM64Context.Lr));
                context.Lr = _target.Read<ulong>(sourceAddress);

                sourceAddress = startingSp + (uint)Marshal.OffsetOf<ARM64Context>(nameof(ARM64Context.Sp));
                context.Sp = _target.Read<ulong>(sourceAddress);

                sourceAddress = startingSp + (uint)Marshal.OffsetOf<ARM64Context>(nameof(ARM64Context.Pc));
                context.Pc = _target.Read<ulong>(sourceAddress);

                sourceAddress = startingSp + (uint)Marshal.OffsetOf<ARM64Context>(nameof(ARM64Context.Fpcr));
                context.Fpcr = _target.Read<uint>(sourceAddress);

                sourceAddress = startingSp + (uint)Marshal.OffsetOf<ARM64Context>(nameof(ARM64Context.Fpsr));
                context.Fpsr = _target.Read<uint>(sourceAddress);

                //
                // Inherit the unwound-to-call flag from this context
                //
                sourceAddress = startingSp + (uint)Marshal.OffsetOf<ARM64Context>(nameof(ARM64Context.ContextFlags));
                context.ContextFlags &= (uint)~ContextFlagsValues.CONTEXT_UNWOUND_TO_CALL;
                context.ContextFlags |=
                                _target.Read<uint>(sourceAddress) & (uint)ContextFlagsValues.CONTEXT_UNWOUND_TO_CALL;
                break;
            }

            case 0xEB:  // MSFT_OP_EC_CONTEXT:
                // NOTE: for .NET, the arm64ec context restoring is not implemented
                UnwinderAssert(false);
                return false;

            case 0xec:  // MSFT_OP_CLEAR_UNWOUND_TO_CALL
                context.ContextFlags &= (uint)~ContextFlagsValues.CONTEXT_UNWOUND_TO_CALL;
                context.Pc = context.Lr;
                break;

            default:
                return false;
        }

        return true;
    }

    #endregion
    #region Helpers

    private uint ComputeScopeSize(
        TargetPointer unwindCodePtr,
        TargetPointer unwindCodesEndPtr,
        bool isEpilog)
    {
        uint scopeSize = 0;
        byte opcode;

        //
        // Iterate through the unwind codes until we hit an end marker.
        // While iterating, accumulate the total scope size.
        //

        while (unwindCodePtr < unwindCodesEndPtr)
        {
            opcode = _target.Read<byte>(unwindCodePtr);
            if (OPCODE_IS_END(opcode))
                break;

            unwindCodePtr += GetUnwindCodeSize(opcode);
            scopeSize += GetUnwindCodeScopeSize(opcode);
        }

        //
        // Epilogs have one extra instruction at the end that needs to be
        // accounted for.
        //
        if (isEpilog)
            scopeSize++;

        return scopeSize;
    }

    private static uint GetUnwindCodeSize(byte unwindCode)
    {
        if (unwindCode < 0xC0)
            return 1;

        if (unwindCode < 0xE0)
            return 2;

        return UnwindCodeSizeTable[unwindCode - 0xE0];
    }

    private static uint GetUnwindCodeScopeSize(byte unwindCode)
    {
        if (unwindCode < 0xE0)
            return 1;

        return UnwindCodeInstructionCountTable[unwindCode - 0xE0];
    }

    /// <summary>
    /// Restores a series of integer registers from the stack.
    /// </summary>
    /// <param name="context">ref of the context.</param>
    /// <param name="spOffset">
    /// Specifies a stack offset. Positive values are simply used
    /// as a base offset. Negative values assume a pre-decrement behavior:
    /// a 0 offset is used for restoration, but the absolute value of the
    /// offset is added to the final Sp.
    /// </param>
    /// <param name="firstRegister">Specifies the index of the first register to restore.</param>
    /// <param name="registerCount">Specifies the number of registers to restore.</param>
    private bool RestoreRegisterRange(
        ref ARM64Context context,
        int spOffset,
        uint firstRegister /* in range (0, 30) */,
        uint registerCount /* in range (1, 31 - firstRegister) */)
    {
        if (firstRegister + registerCount > 31)
        {
            // invalid register range
            return false;
        }

        //
        // Compute the source address
        //
        TargetPointer curAddress = context.Sp;
        if (spOffset >= 0)
        {
            curAddress += (uint)spOffset;
        }

        //
        // Restore the registers
        //
        for (uint regIndex = 0; regIndex < registerCount; regIndex++)
        {
            SetRegister(ref context, firstRegister + regIndex, _target.Read<ulong>(curAddress));
            curAddress += 8;
        }
        if (spOffset < 0)
        {
            context.Sp -= (ulong)spOffset;
        }

        return true;
    }

    /// <summary>
    /// Restores a series of floating-point registers from the stack.
    /// </summary>
    /// <param name="context">ref of the context.</param>
    /// <param name="spOffset">
    /// Specifies a stack offset. Positive values are simply used
    /// as a base offset. Negative values assume a pre-decrement behavior:
    /// a 0 offset is used for restoration, but the absolute value of the
    /// offset is added to the final Sp.
    /// </param>
    /// <param name="firstRegister">Specifies the index of the first register to restore.</param>
    /// <param name="registerCount">Specifies the number of registers to restore.</param>
    private unsafe bool RestoreFpRegisterRange(
        ref ARM64Context context,
        int spOffset,
        uint firstRegister,
        uint registerCount)
    {
        if (firstRegister + registerCount > 32)
        {
            // invalid register range
            return false;
        }

        //
        // Compute the source address
        //
        TargetPointer curAddress = context.Sp;
        if (spOffset >= 0)
        {
            curAddress += (uint)spOffset;
        }

        //
        // Restore the registers
        //
        for (uint regIndex = 0; regIndex < registerCount; regIndex++)
        {
            // double register values to only index into the low 64 bits of each 128-bit register
            context.V[(firstRegister + regIndex) * 2] = _target.Read<ulong>(curAddress);
            curAddress += 8;
        }
        if (spOffset < 0)
        {
            context.Sp -= (ulong)spOffset;
        }

        return true;
    }

    /// <summary>
    /// Restores a series of full SIMD (Q) registers from the stack.
    /// </summary>
    /// <param name="context">ref of the context.</param>
    /// <param name="spOffset">
    /// Specifies a stack offset. Positive values are simply used
    /// as a base offset. Negative values assume a pre-decrement behavior:
    /// a 0 offset is used for restoration, but the absolute value of the
    /// offset is added to the final Sp.
    /// </param>
    /// <param name="firstRegister">Specifies the index of the first register to restore.</param>
    /// <param name="registerCount">Specifies the number of registers to restore.</param>
    private unsafe bool RestoreSimdRegisterRange(
        ref ARM64Context context,
        int spOffset,
        uint firstRegister,
        uint registerCount)
    {
        if (firstRegister + registerCount > 32)
        {
            // invalid register range
            return false;
        }

        //
        // Compute the source address
        //
        TargetPointer curAddress = context.Sp;
        if (spOffset >= 0)
        {
            curAddress += (uint)spOffset;
        }

        //
        // Restore the registers
        //
        for (uint regIndex = 0; regIndex < registerCount; regIndex++)
        {
            // V indexes are 64-bit values of the 128-bit registers
            // double register values to write the low 64 bits of the 128-bit register
            context.V[(firstRegister + regIndex) * 2] = _target.Read<ulong>(curAddress);
            curAddress += 8;
            // double register values + 1 to write the high 64 bits of the 128-bit register
            context.V[((firstRegister + regIndex) * 2) + 1] = _target.Read<ulong>(curAddress);
            curAddress += 8;
        }
        if (spOffset < 0)
        {
            context.Sp -= (ulong)spOffset;
        }

        return true;
    }

    private static void SetRegister(ref ARM64Context context, uint regIndex, ulong value)
    {
        switch (regIndex)
        {
            case 0: context.X0 = value; break;
            case 1: context.X1 = value; break;
            case 2: context.X2 = value; break;
            case 3: context.X3 = value; break;
            case 4: context.X4 = value; break;
            case 5: context.X5 = value; break;
            case 6: context.X6 = value; break;
            case 7: context.X7 = value; break;
            case 8: context.X8 = value; break;
            case 9: context.X9 = value; break;
            case 10: context.X10 = value; break;
            case 11: context.X11 = value; break;
            case 12: context.X12 = value; break;
            case 13: context.X13 = value; break;
            case 14: context.X14 = value; break;
            case 15: context.X15 = value; break;
            case 16: context.X16 = value; break;
            case 17: context.X17 = value; break;
            case 18: context.X18 = value; break;
            case 19: context.X19 = value; break;
            case 20: context.X20 = value; break;
            case 21: context.X21 = value; break;
            case 22: context.X22 = value; break;
            case 23: context.X23 = value; break;
            case 24: context.X24 = value; break;
            case 25: context.X25 = value; break;
            case 26: context.X26 = value; break;
            case 27: context.X27 = value; break;
            case 28: context.X28 = value; break;
            case 29: context.Fp = value; break;
            case 30: context.Lr = value; break;
            default: throw new ArgumentOutOfRangeException(nameof(regIndex));
        }
    }

    private static bool OPCODE_IS_END(byte opcode)
    {
        return (opcode & 0xFE) == 0xE4;
    }

    private static void UnwinderAssert([DoesNotReturnIf(false)] bool condition, string? message = null)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    #endregion
    #region Structs

    private struct SaveAnyUnwindCode(byte val1, byte val2)
    {
        public byte o = (byte)(val1 & 0x3f);
        public byte f = (byte)(val1 >> 6);
        public byte r = (byte)(val2 & 0x1f);
        public byte x = (byte)((val1 >> 5) & 0x1);
        public byte p = (byte)((val1 >> 6) & 0x1);
        public byte fixedOp = (byte)((val1 >> 7) & 0x1);
    }

    #endregion
}
