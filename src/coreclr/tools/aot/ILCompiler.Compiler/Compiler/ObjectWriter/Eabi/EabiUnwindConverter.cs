// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

using static ILCompiler.ObjectWriter.EabiNative;

namespace ILCompiler.ObjectWriter
{
    internal static class EabiUnwindConverter
    {
        private enum CFI_OPCODE
        {
            CFI_ADJUST_CFA_OFFSET,    // Offset is adjusted relative to the current one.
            CFI_DEF_CFA_REGISTER,     // New register is used to compute CFA
            CFI_REL_OFFSET,           // Register is saved at offset from the current CFA
            CFI_DEF_CFA               // Take address from register and add offset to it.
        }

        public static byte[] ConvertCFIToEabi(byte[] blobData)
        {
            if (blobData == null || blobData.Length == 0)
            {
                return blobData;
            }

            Debug.Assert(blobData.Length % 8 == 0);

            // The maximum sequence length of the ARM EHABI unwinding code is 1024
            // bytes.
            byte[] unwindData = new byte[1024];
            int unwindDataOffset = 0;
            int popOffset = 0;
            uint pendingPopMask = 0;
            uint pendingVPopMask = 0;
            int pendingSpAdjustment = 0;

            // Walk the CFI data backwards
            for (int offset = blobData.Length - 8; offset >= 0; offset -= 8)
            {
                CFI_OPCODE opcode = (CFI_OPCODE)blobData[offset + 1];
                short dwarfReg = BinaryPrimitives.ReadInt16LittleEndian(blobData.AsSpan(offset + 2));
                int cfiOffset = BinaryPrimitives.ReadInt32LittleEndian(blobData.AsSpan(offset + 4));

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
                        else if (dwarfReg >= 256 && dwarfReg <= 271)
                        {
                            dwarfReg -= 256;
                            EmitVPop((uint)(3u << (dwarfReg << 1)));
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

            return unwindData[..unwindDataOffset];

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
                if (pendingSpAdjustment != 0)
                {
                    Debug.Assert(pendingSpAdjustment > 0);
                    if (pendingSpAdjustment <= 0x100)
                    {
                        // vsp = vsp + (xxxxxx << 2) + 4.
                        unwindData[unwindDataOffset++] = (byte)((pendingSpAdjustment >> 2) - 1);
                    }
                    else if (pendingSpAdjustment <= 0x200)
                    {
                        // vsp = vsp + (0x3f << 2) + 4.
                        unwindData[unwindDataOffset++] = (byte)0x3f;
                        pendingSpAdjustment -= 0x100;
                        // vsp = vsp + (xxxxxx << 2) + 4.
                        unwindData[unwindDataOffset++] = (byte)((pendingSpAdjustment >> 2) - 1);
                    }
                    else
                    {
                        // vsp = vsp + 0x204 + (uleb128 << 2)
                        unwindData[unwindDataOffset++] = (byte)0xb2;
                        unwindDataOffset += DwarfHelper.WriteULEB128(unwindData.AsSpan(unwindDataOffset), (uint)((pendingSpAdjustment - 0x204) >> 2));
                    }
                    pendingSpAdjustment = 0;
                }
                else if (pendingPopMask != 0)
                {
                    // TODO: Efficient encodings!

                    if ((pendingPopMask & 0xFFF0) != 0)
                    {
                        ushort ins = (ushort)(0x8000u | (pendingPopMask >> 4));
                        unwindData[unwindDataOffset++] = (byte)(ins >> 8);
                        unwindData[unwindDataOffset++] = (byte)(ins & 0xff);
                    }

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
                    Debug.Fail("VPOP unwinding not implemented");
                }
            }
        }
    }
}
