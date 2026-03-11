// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using static Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers.LoongArch64Context;

namespace Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers.LoongArch64;

internal class LoongArch64Unwinder(Target target)
{
    #region Constants

    /// <summary>
    /// This table describes the size of each unwind code, in bytes.
    /// </summary>
    private static ReadOnlySpan<byte> UnwindCodeSizeTable =>
    [
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, // 00-1F
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, // 20-3F
        2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, // 40-5F
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, // 60-7F
        2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, // 80-9F
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, // A0-BF
        2, 2, 2, 2, 2, 2, 2, 2, 3, 2, 2, 2, 3, 2, 2, 2, 3, 2, 2, 2, 2, 2, 3, 2, 3, 2, 3, 2, 3, 2, 2, 2, // C0-DF
        4, 1, 3, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1  // E0-FF
    ];

    #endregion

    private readonly Target _target = target;
    private readonly IExecutionManager _eman = target.Contracts.ExecutionManager;

    #region Entrypoint

    public bool Unwind(ref LoongArch64Context context)
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
        // many callers as a failure, given that VirtualUnwind does not
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

    private bool VirtualUnwind(ref LoongArch64Context context, TargetPointer imageBase, Data.RuntimeFunction functionEntry)
    {
        // FunctionEntry could be null if the function is a pure leaf/trivial function.
        // This is not a valid case for managed code and not handled here.

        uint controlPcRva = (uint)(context.Pc - imageBase.Value);

        return VirtualUnwindFull(ref context, controlPcRva, imageBase, functionEntry);
    }

    private bool VirtualUnwindFull(
        ref LoongArch64Context context,
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
        // By default, unwinding is done by popping to the RA, then copying
        // that RA to the PC. However, some special opcodes require different
        // behavior.
        //
        bool finalPcFromRa = true;

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

        uint functionLength = headerWord & 0x3ffffu;
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
            scopeSize = ComputeScopeSize(unwindCodePtr, unwindCodesEndPtr);

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
                scopeSize = ComputeScopeSize(unwindCodePtr + unwindIndex, unwindCodesEndPtr);
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
                uint scopeStart = headerWord & 0x3ffffu;
                if (offsetInFunction < scopeStart)
                    break;

                unwindIndex = headerWord >> 22;
                if (offsetInFunction < scopeStart + (4 * unwindWords - unwindIndex))
                {
                    scopeSize = ComputeScopeSize(unwindCodePtr + unwindIndex, unwindCodesEndPtr);

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
            if (OpcodeIsEnd(curCode))
            {
                break;
            }
            unwindCodePtr += UnwindCodeSizeTable[curCode];
            skipWords--;
        }

        //
        // Now execute codes until we hit the end.
        //

        uint accumulatedSaveNexts = 0;
        while (unwindCodePtr < unwindCodesEndPtr)
        {
            byte curCode = _target.Read<byte>(unwindCodePtr);
            unwindCodePtr += 1;

            bool isEndCode = OpcodeIsEnd(curCode);
            if (!ProcessUnwindCode(ref context, curCode, ref unwindCodePtr, unwindCodesEndPtr, ref accumulatedSaveNexts))
            {
                return false;
            }

            if (isEndCode)
            {
                break;
            }
        }

        //
        // If we succeeded, post-process the final state.
        //

        if (finalPcFromRa)
        {
            context.Pc = context.Ra;
        }

        return true;
    }

    private uint ComputeScopeSize(TargetPointer unwindCodePtr, TargetPointer unwindCodesEndPtr)
    {
        //
        // Iterate through the unwind codes until we hit an end marker.
        // While iterating, accumulate the total scope size.
        //

        uint scopeSize = 0;
        while (unwindCodePtr < unwindCodesEndPtr)
        {
            byte opcode = _target.Read<byte>(unwindCodePtr);
            if (OpcodeIsEnd(opcode))
            {
                break;
            }

            unwindCodePtr += UnwindCodeSizeTable[opcode];
            scopeSize++;
        }

        return scopeSize;
    }

    private static bool OpcodeIsEnd(byte opcode)
    {
        return (opcode & 0xfe) == 0xe4;
    }

    private bool ProcessUnwindCode(ref LoongArch64Context context, byte curCode, ref TargetPointer unwindCodePtr, TargetPointer unwindCodesEndPtr, ref uint accumulatedSaveNexts)
    {
        try
        {
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
                context.Sp += 16u * (uint)(curCode & 0x1f);
            }

            //
            // alloc_m (11000xxx|xxxxxxxx): allocate large stack with size < 32k (2^11 * 16)
            //

            else if (curCode <= 0xc7)
            {
                if (accumulatedSaveNexts != 0)
                {
                    // invalid sequence
                    return false;
                }
                if (unwindCodePtr >= unwindCodesEndPtr)
                {
                    return false;
                }
                byte nextCode = _target.Read<byte>(unwindCodePtr);
                unwindCodePtr += 1;
                context.Sp += 16u * ((curCode & 7u) << 8);
                context.Sp += 16u * nextCode;
            }

            //
            // save_reg (11010000|000xxxxx|zzzzzzzz): save reg r(1+#X) at [sp+#Z*8], offset <= 2047
            //

            else if (curCode == 0xd0)
            {
                if (accumulatedSaveNexts != 0)
                {
                    // invalid sequence
                    return false;
                }
                if (unwindCodePtr + 1 >= unwindCodesEndPtr)
                {
                    return false;
                }
                byte nextCode = _target.Read<byte>(unwindCodePtr);
                unwindCodePtr += 1;
                byte nextCode1 = _target.Read<byte>(unwindCodePtr);
                unwindCodePtr += 1;

                uint regNum = 1u + nextCode;
                uint offset = 8u * nextCode1;
                SetRegisterFromOffset(ref context, regNum, context.Sp + offset);
            }

            //
            // save_freg (11011100|0xxxzzzz|zzzzzzzz): save reg f(24+#X) at [sp+#Z*8], offset <= 32767
            //

            else if (curCode == 0xdc)
            {
                if (accumulatedSaveNexts != 0)
                {
                    // invalid sequence
                    return false;
                }
                if (unwindCodePtr + 1 >= unwindCodesEndPtr)
                {
                    return false;
                }
                byte nextCode = _target.Read<byte>(unwindCodePtr);
                unwindCodePtr += 1;
                byte nextCode1 = _target.Read<byte>(unwindCodePtr);
                unwindCodePtr += 1;
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
                if (unwindCodePtr + 2 >= unwindCodesEndPtr)
                {
                    return false;
                }
                byte nextCode1 = _target.Read<byte>(unwindCodePtr);
                unwindCodePtr += 1;
                byte nextCode2 = _target.Read<byte>(unwindCodePtr);
                unwindCodePtr += 1;
                byte nextCode3 = _target.Read<byte>(unwindCodePtr);
                unwindCodePtr += 1;
                context.Sp += 16u * (uint)((nextCode1 << 16) | (nextCode2 << 8) | nextCode3);
            }

            //
            // set_fp (11100001): set up fp: with: ori fp,sp,0
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
            // add_fp (11100010|000xxxxx|xxxxxxxx): set up fp with: addi.d fp,sp,#x*8
            //

            else if (curCode == 0xe2)
            {
                if (accumulatedSaveNexts != 0)
                {
                    // invalid sequence
                    return false;
                }
                if (unwindCodePtr + 1 >= unwindCodesEndPtr)
                {
                    return false;
                }
                byte nextCode = _target.Read<byte>(unwindCodePtr);
                unwindCodePtr += 1;
                byte nextCode1 = _target.Read<byte>(unwindCodePtr);
                unwindCodePtr += 1;
                context.Sp = context.Fp - 8u * ((uint)((nextCode << 8) | nextCode1));
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

                return true;
            }

            //
            // end_c (11100101): end of unwind code in current chained scope.
            //          Continue unwinding parent scope.
            //

            else if (curCode == 0xe5)
            {
            }

            //
            // custom_0 (111010xx): restore custom structure
            //

            else if (curCode >= 0xe8 && curCode <= 0xeb)
            {
                if (accumulatedSaveNexts != 0)
                {
                    // invalid sequence
                    return false;
                }

                return false;
            }

            //
            // Anything else is invalid
            //

            else
            {
                // invalid sequence
                return false;
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    private void SetRegisterFromOffset(ref LoongArch64Context context, uint regNum, ulong address)
    {
        try
        {
            ulong value = _target.ReadPointer(address).Value;
            SetRegisterValue(ref context, regNum, value);
        }
        catch
        {
            // Ignore read failures
        }
    }

    private static void SetRegisterValue(ref LoongArch64Context context, uint regNum, ulong value)
    {
        switch (regNum)
        {
            case 0: context.R0 = value; break;
            case 1: context.Ra = value; break;
            case 2: context.Tp = value; break;
            case 3: context.Sp = value; break;
            case 4: context.A0 = value; break;
            case 5: context.A1 = value; break;
            case 6: context.A2 = value; break;
            case 7: context.A3 = value; break;
            case 8: context.A4 = value; break;
            case 9: context.A5 = value; break;
            case 10: context.A6 = value; break;
            case 11: context.A7 = value; break;
            case 12: context.T0 = value; break;
            case 13: context.T1 = value; break;
            case 14: context.T2 = value; break;
            case 15: context.T3 = value; break;
            case 16: context.T4 = value; break;
            case 17: context.T5 = value; break;
            case 18: context.T6 = value; break;
            case 19: context.T7 = value; break;
            case 20: context.T8 = value; break;
            case 21: context.X0 = value; break;
            case 22: context.Fp = value; break;
            case 23: context.S0 = value; break;
            case 24: context.S1 = value; break;
            case 25: context.S2 = value; break;
            case 26: context.S3 = value; break;
            case 27: context.S4 = value; break;
            case 28: context.S5 = value; break;
            case 29: context.S6 = value; break;
            case 30: context.S7 = value; break;
            case 31: context.S8 = value; break;
        }
    }

    #endregion
}
