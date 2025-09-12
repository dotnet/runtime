// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Text;

using static ILCompiler.ObjectWriter.EabiNative;

namespace ILCompiler.ObjectWriter
{
    internal static class EabiUnwindConverter
    {
        /// <summary>
        /// Convert from the DWARF CFI opcodes produced by JIT into the ARM EHABI
        /// opcodes for exception unwinding.
        /// </summary>
        /// <param name="blobData">DWARF CFI blob from JIT</param>
        /// <returns>
        /// ARM EHABI unwind code, as specified by Exception Handling ABI for the Arm
        /// Architecture, 2023Q3, section 10.3.
        /// </returns>
        public static byte[] ConvertCFIToEabi(byte[] blobData)
        {
            if (blobData == null || blobData.Length == 0)
            {
                return blobData;
            }

            Debug.Assert(blobData.Length % 8 == 0);

            // The maximum sequence length of the ARM EHABI unwinding code is 1024
            // bytes.
            byte[] unwindData = ArrayPool<byte>.Shared.Rent(1024);
            int unwindDataOffset = 0;

            // The DWARF CFI data produced by the JIT describe the method prolog that
            // saves registers, adjusts the stack, and optionally set ups the frame
            // register. In contrast, the ARM EHABI unwind code describes how the epilog
            // would do the unwinding. It lacks the code offsets so it cannot unwind
            // inside either the prolog, or the epilog. The runtime code detects these
            // cases when doing the asynchronous unwinding.
            //
            // In order to convert between the two formats we thus need to reverse
            // the order of operatations. The EHABI unwind codes closely mirror the
            // ARM instructions and they efficiently describe the POP/VPOP operation on
            // multiple registers. In order to get the most compact representation we
            // record the pending opertations at each code offset and only flush the
            // unwind code when necessary.

            // Adjustment to VSP made by VPOP instruction relative to the DWARF CFI
            // which uses an explicit CFI_ADJUST_CFA_OFFSET opcode.
            int popOffset = 0;

            // Mask of pending Rn registers popped at current code offset.
            uint pendingPopMask = 0;

            // Mask of pending Dn vector registers popped at current code offset.
            uint pendingVPopMask = 0;

            // Stack adjustment by add/sub sp, X instructions
            int pendingSpAdjustment = 0;

            int lastCodeOffset = blobData[blobData.Length - 8];

            // Walk the CFI data backwards
            for (int offset = blobData.Length - 8; offset >= 0; offset -= 8)
            {
                byte codeOffset = blobData[offset];
                CFI_OPCODE opcode = (CFI_OPCODE)blobData[offset + 1];
                short dwarfReg = BinaryPrimitives.ReadInt16LittleEndian(blobData.AsSpan(offset + 2));
                int cfiOffset = BinaryPrimitives.ReadInt32LittleEndian(blobData.AsSpan(offset + 4));

                if (lastCodeOffset != codeOffset)
                {
                    Debug.Assert(popOffset == 0);
                    FlushPendingOperation();
                    lastCodeOffset = codeOffset;
                }

                switch (opcode)
                {
                    case CFI_OPCODE.CFI_DEF_CFA_REGISTER:
                        Debug.Assert(dwarfReg != 13); // SP
                        Debug.Assert(dwarfReg < 15);

                        FlushPendingOperation();
                        // Set vsp = r[nnnn]
                        unwindData[unwindDataOffset++] = (byte)(0x90 | dwarfReg);
                        break;

                    case CFI_OPCODE.CFI_REL_OFFSET:
                        Debug.Assert(cfiOffset == popOffset);
                        if (dwarfReg >= 0 && dwarfReg <= 15)
                        {
                            EmitPop((uint)(1u << dwarfReg));
                            popOffset += 4;
                        }
                        else if (dwarfReg >= 256 && dwarfReg <= 287)
                        {
                            dwarfReg -= 256;
                            EmitVPop((uint)(1u << dwarfReg));
                            popOffset += 8;
                        }
                        else
                        {
                            Debug.Fail("Unknown register");
                        }
                        break;

                    case CFI_OPCODE.CFI_ADJUST_CFA_OFFSET:
                        cfiOffset -= popOffset;
                        popOffset = 0;
                        if (cfiOffset != 0)
                        {
                            EmitSpAdjustment(cfiOffset);
                        }
                        break;
                }
            }

            FlushPendingOperation();

            var result = unwindData[..unwindDataOffset];
            ArrayPool<byte>.Shared.Return(unwindData);
            return result;

            void EmitPop(uint popMask)
            {
                if (pendingPopMask == 0)
                    FlushPendingOperation();
                pendingPopMask |= popMask;
            }

            void EmitVPop(uint vpopMask)
            {
                if (pendingVPopMask == 0)
                    FlushPendingOperation();
                pendingVPopMask |= vpopMask;
            }

            void EmitSpAdjustment(int spAdjustment)
            {
                if (pendingSpAdjustment == 0)
                    FlushPendingOperation();
                pendingSpAdjustment += spAdjustment;
            }

            void FlushPendingOperation()
            {
                if (pendingSpAdjustment > 0)
                {
                    Debug.Assert((pendingSpAdjustment & 3) == 0);
                    if (pendingSpAdjustment <= 0x100)
                    {
                        // vsp = vsp + (xxxxxx << 2) + 4.
                        // 00xxxxxx
                        unwindData[unwindDataOffset++] = (byte)((pendingSpAdjustment >> 2) - 1);
                    }
                    else if (pendingSpAdjustment <= 0x200)
                    {
                        // vsp = vsp + (0x3f << 2) + 4.
                        // 00111111
                        unwindData[unwindDataOffset++] = (byte)0x3f;
                        pendingSpAdjustment -= 0x100;
                        // vsp = vsp + (xxxxxx << 2) + 4.
                        // 00xxxxxx
                        unwindData[unwindDataOffset++] = (byte)((pendingSpAdjustment >> 2) - 1);
                    }
                    else
                    {
                        // vsp = vsp + 0x204 + (uleb128 << 2)
                        // 10110010 uleb128
                        unwindData[unwindDataOffset++] = (byte)0xb2;
                        unwindDataOffset += DwarfHelper.WriteULEB128(unwindData.AsSpan(unwindDataOffset), (uint)((pendingSpAdjustment - 0x204) >> 2));
                    }

                    pendingSpAdjustment = 0;
                }
                else if (pendingSpAdjustment < 0)
                {
                    while (pendingSpAdjustment < -0x100)
                    {
                        // vsp = vsp - (0x3f << 2) - 4.
                        // 01111111
                        unwindData[unwindDataOffset++] = 0x7f;
                        pendingSpAdjustment += 0x100;
                    }
                    // vsp = vsp - (xxxxxx << 2) - 4.
                    // 01xxxxxx
                    unwindData[unwindDataOffset++] = (byte)(0x40 | ((-pendingSpAdjustment >> 2) - 1));

                    pendingSpAdjustment = 0;
                }
                else if (pendingPopMask != 0)
                {
                    // Try to use efficient encoding if we have a consecutive run of
                    // r4-rN registers for N <= 11, and either no high registers or r14.
                    if ((pendingPopMask & 0x10) == 0x10 &&
                        ((pendingPopMask & 0xF000) == 0 || (pendingPopMask & 0xF000) == 0x4000))
                    {
                        uint r5AndHigher = (pendingPopMask & 0xFF0) >> 5;
                        int bitRunLength = BitOperations.TrailingZeroCount(~r5AndHigher);
                        // No gaps...
                        if ((r5AndHigher & ((1 << bitRunLength) - 1)) == r5AndHigher)
                        {
                            if ((pendingPopMask & 0xF000) == 0)
                            {
                                // Pop r4-r[4+nnn]
                                // 10100nnn
                                unwindData[unwindDataOffset++] = (byte)(0xA0 | bitRunLength);
                            }
                            else
                            {
                                // Pop r4-r[4+nnn], r14
                                // 10101nnn
                                unwindData[unwindDataOffset++] = (byte)(0xA8 | bitRunLength);
                            }

                            pendingPopMask &= 0xF;
                        }
                    }

                    // Pop up to 12 integer registers under masks {r15-r12}, {r11-r4}
                    // 1000iiii iiiiiiii
                    if ((pendingPopMask & 0xFFF0) != 0)
                    {
                        ushort ins = (ushort)(0x8000u | (pendingPopMask >> 4));
                        unwindData[unwindDataOffset++] = (byte)(ins >> 8);
                        unwindData[unwindDataOffset++] = (byte)(ins & 0xff);
                    }

                    // Pop integer registers under mask {r3, r2, r1, r0}
                    // 10110001 0000iiii
                    if ((pendingPopMask & 0xF) != 0)
                    {
                        ushort ins = (ushort)(0xB100u | (pendingPopMask & 0xf));
                        unwindData[unwindDataOffset++] = (byte)(ins >> 8);
                        unwindData[unwindDataOffset++] = (byte)(ins & 0xff);
                    }

                    pendingPopMask = 0;
                }
                else if (pendingVPopMask != 0)
                {
                    // Find consecutive bit runs

                    // Pop VFP double precision registers D[16+ssss]-D[16+ssss+cccc] saved (as if) by VPUSH
                    // 11001000 sssscccc
                    uint mask = pendingVPopMask >> 16;
                    while (mask > 0)
                    {
                        int leadingZeros = BitOperations.LeadingZeroCount(mask);
                        int bitRunLength = BitOperations.LeadingZeroCount(~(mask << leadingZeros));
                        leadingZeros -= 16; // Using uint but working only with low 16 bits
                        unwindData[unwindDataOffset++] = 0xc8;
                        unwindData[unwindDataOffset++] = (byte)(((16 - leadingZeros - bitRunLength) << 4) | (bitRunLength - 1));
                        mask &= (uint)(1u << (16 - leadingZeros - bitRunLength)) - 1u;
                    }

                    // Pop VFP double precision registers D[ssss]-D[ssss+cccc] saved (as if) by VPUSH
                    // 11001001 sssscccc
                    mask = pendingVPopMask & 0xffff;
                    while (mask > 0)
                    {
                        int leadingZeros = BitOperations.LeadingZeroCount(mask);
                        int bitRunLength = BitOperations.LeadingZeroCount(~(mask << leadingZeros));
                        leadingZeros -= 16; // Using uint but working only with low 16 bits
                        unwindData[unwindDataOffset++] = 0xc9;
                        unwindData[unwindDataOffset++] = (byte)(((16 - leadingZeros - bitRunLength) << 4) | (bitRunLength - 1));
                        mask &= (uint)(1u << (16 - leadingZeros - bitRunLength)) - 1u;
                    }

                    pendingVPopMask = 0;
                }
            }
        }
    }
}
