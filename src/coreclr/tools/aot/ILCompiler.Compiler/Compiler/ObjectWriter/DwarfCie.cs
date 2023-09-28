// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Buffers.Binary;

using Internal.TypeSystem;

using Melanzana.MachO;
using static ILCompiler.ObjectWriter.DwarfNative;

namespace ILCompiler.ObjectWriter
{
    public class DwarfCie
    {
        public byte PointerEncoding;
        public byte LsdaEncoding;
        public byte PersonalityEncoding;
        public string PersonalitySymbolName;
        public uint CodeAlignFactor;
        public int DataAlignFactor;
        public bool IsSignalFrame;
        public bool FdesHaveAugmentationData;
        public byte ReturnAddressRegister;
        public byte[] Instructions;
        public byte InitialCFAOffset;

        public DwarfCie(TargetArchitecture targetArchitecture)
        {
            IsSignalFrame = false;

            // Each FDE has LSDA pointer
            FdesHaveAugmentationData = true;

            // Unused
            PersonalityEncoding = 0;
            PersonalitySymbolName = null;

            // NOTE: Apple linker only knows how to handle DW_EH_PE_pcrel in combination with
            // DW_EH_PE_sdata4 or DW_EH_PE_ptr.
            PointerEncoding = DW_EH_PE_pcrel | DW_EH_PE_sdata4;
            LsdaEncoding = DW_EH_PE_pcrel | DW_EH_PE_sdata4;

            if (targetArchitecture == TargetArchitecture.ARM64)
            {
                CodeAlignFactor = 1;
                DataAlignFactor = -4;
                ReturnAddressRegister = 30; // LR
                Instructions = new byte[]
                {
                    DW_CFA_def_cfa,
                    31, // SP
                    0, // Offset from SP
                };
                InitialCFAOffset = 0;
            }
            else if (targetArchitecture == TargetArchitecture.X64)
            {
                CodeAlignFactor = 1;
                DataAlignFactor = -8;
                ReturnAddressRegister = 16; // RA
                Instructions = new byte[]
                {
                    DW_CFA_def_cfa,
                    7, // RSP
                    8, // Offset from RSP
                    DW_CFA_offset | 16, // RIP
                    1, // RIP is at -8
                };
                InitialCFAOffset = 8;
            }
            else
            {
                throw new NotSupportedException();
            }
        }
    }
}
