// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Buffers.Binary;

using ILCompiler.DependencyAnalysis;
using Internal.Text;

using static ILCompiler.ObjectWriter.DwarfNative;

namespace ILCompiler.ObjectWriter
{
    internal sealed class DwarfEhFrame
    {
        private SectionWriter _sectionWriter;
        private bool _is64Bit;
        private Dictionary<DwarfCie, uint> _cieOffset;

        public DwarfEhFrame(SectionWriter sectionWriter, bool is64Bit)
        {
            _sectionWriter = sectionWriter;
            _is64Bit = is64Bit;
            _cieOffset = new Dictionary<DwarfCie, uint>();
        }

        public void AddCie(DwarfCie cie)
        {
            _cieOffset.Add(cie, (uint)_sectionWriter.Position);
            WriteCie(cie);
        }

        public void AddFde(DwarfFde fde)
        {
            uint cieOffset;

            if (!_cieOffset.TryGetValue(fde.Cie, out cieOffset))
            {
                AddCie(fde.Cie);
                cieOffset = _cieOffset[fde.Cie];
            }

            WriteFde(fde, cieOffset);
        }

        private void WriteCie(DwarfCie cie)
        {
            Utf8StringBuilder augmentationString = new Utf8StringBuilder();
            uint augmentationLength = 0;
            Span<byte> tempBuffer = stackalloc byte[8];

            if (cie.FdesHaveAugmentationData)
            {
                augmentationString.Append('z');
            }
            if (cie.PersonalitySymbolName != null)
            {
                augmentationString.Append('P');
                augmentationLength += 1u + AddressSize(cie.PersonalityEncoding);
            }
            if (cie.LsdaEncoding != 0)
            {
                augmentationString.Append('L');
                augmentationLength++;
            }
            if (cie.PointerEncoding != 0)
            {
                augmentationString.Append('R');
                augmentationLength++;
            }
            if (cie.IsSignalFrame)
            {
                augmentationString.Append('S');
            }

            uint length =
                4u + // Length
                4u + // CIE Offset (0)
                1u + // Version
                (uint)augmentationString.Length + 1u +
                DwarfHelper.SizeOfULEB128(cie.CodeAlignFactor) +
                DwarfHelper.SizeOfSLEB128(cie.DataAlignFactor) +
                DwarfHelper.SizeOfULEB128(cie.ReturnAddressRegister) +
                (uint)(augmentationLength > 0 ? DwarfHelper.SizeOfULEB128(augmentationLength) + augmentationLength : 0) +
                (uint)cie.Instructions.Length;
            uint padding = ((length + 7u) & ~7u) - length;

            BinaryPrimitives.WriteUInt32LittleEndian(tempBuffer, length + padding - 4u);
            _sectionWriter.Write(tempBuffer);

            _sectionWriter.WriteByte(cie.ReturnAddressRegister < 0x7f ? (byte)1u : (byte)3u); // Version
            _sectionWriter.Write(augmentationString.UnderlyingArray);

            _sectionWriter.WriteULEB128(cie.CodeAlignFactor);
            _sectionWriter.WriteSLEB128(cie.DataAlignFactor);
            _sectionWriter.WriteULEB128(cie.ReturnAddressRegister);

            _sectionWriter.WriteULEB128(augmentationLength);
            if (cie.PersonalitySymbolName != null)
            {
                _sectionWriter.WriteByte(cie.PersonalityEncoding);
                WriteAddress(cie.PersonalityEncoding, cie.PersonalitySymbolName);
            }
            if (cie.LsdaEncoding != 0)
            {
                _sectionWriter.WriteByte(cie.LsdaEncoding);
            }
            if (cie.PointerEncoding != 0)
            {
                _sectionWriter.WriteByte(cie.PointerEncoding);
            }

            _sectionWriter.Write(cie.Instructions);

            _sectionWriter.Write(stackalloc byte[(int)padding]);
        }

        private void WriteFde(DwarfFde fde, uint cieOffset)
        {
            Span<byte> tempBuffer = stackalloc byte[8];

            uint augmentationLength =
                fde.Cie.FdesHaveAugmentationData ?
                    1u + // Length
                    (fde.Cie.PersonalityEncoding != 0 ? AddressSize(fde.Cie.PersonalityEncoding) : 0) +
                    (fde.Cie.LsdaEncoding != 0 ? AddressSize(fde.Cie.LsdaEncoding) : 0) : 0;

            uint length =
                4u + // Length
                4u + // CIE offset
                AddressSize(fde.Cie.PointerEncoding) + // PC start
                AddressSize(fde.Cie.PointerEncoding) + // PC end
                augmentationLength +
                (uint)fde.Instructions.Length;
            uint padding = ((length + 7u) & ~7u) - length;

            BinaryPrimitives.WriteUInt32LittleEndian(tempBuffer, length + padding - 4u);
            _sectionWriter.Write(tempBuffer.Slice(0, 4));
            BinaryPrimitives.WriteUInt32LittleEndian(tempBuffer, (uint)(_sectionWriter.Position - cieOffset));
            _sectionWriter.Write(tempBuffer.Slice(0, 4));
            WriteAddress(fde.Cie.PointerEncoding, fde.PcStartSymbolName, fde.PcStartSymbolOffset);
            WriteSize(fde.Cie.PointerEncoding, fde.PcLength);

            if (fde.Cie.FdesHaveAugmentationData)
            {
                _sectionWriter.WriteByte((byte)(augmentationLength - 1));
                if (fde.Cie.PersonalityEncoding != 0)
                {
                    WriteAddress(fde.Cie.PersonalityEncoding, fde.PersonalitySymbolName);
                }
                if (fde.Cie.LsdaEncoding != 0)
                {
                    WriteAddress(fde.Cie.LsdaEncoding, fde.LsdaSymbolName);
                }
            }

            _sectionWriter.Write(fde.Instructions);
            _sectionWriter.Write(stackalloc byte[(int)padding]);
        }

        private uint AddressSize(byte encoding)
        {
            switch (encoding & 0xf)
            {
                case DW_EH_PE_ptr: return _is64Bit ? 8u : 4u;
                case DW_EH_PE_sdata4: return 4u;
                default:
                    throw new NotSupportedException();
            }
        }

        private void WriteAddress(byte encoding, string symbolName, long symbolOffset = 0)
        {
            if (symbolName != null)
            {
                RelocType relocationType = encoding switch
                {
                    DW_EH_PE_pcrel | DW_EH_PE_sdata4 => RelocType.IMAGE_REL_BASED_RELPTR32,
                    DW_EH_PE_absptr => RelocType.IMAGE_REL_BASED_DIR64,
                    _ => throw new NotSupportedException()
                };
                _sectionWriter.EmitSymbolReference(relocationType, symbolName, symbolOffset);
            }
            else
            {
                Span<byte> address = stackalloc byte[(int)AddressSize(encoding)];
                _sectionWriter.Write(address);
            }
        }

        private void WriteSize(byte encoding, ulong size)
        {
            if (AddressSize(encoding) == 4)
            {
                _sectionWriter.WriteLittleEndian<uint>((uint)size);
            }
            else
            {
                _sectionWriter.WriteLittleEndian<ulong>(size);
            }
        }
    }
}
