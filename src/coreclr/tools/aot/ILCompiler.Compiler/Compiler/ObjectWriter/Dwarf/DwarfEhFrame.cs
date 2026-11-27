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
    /// <summary>
    /// Writes CIEs and FDEs in the .eh_frame format, or in the .debug_frame format when the
    /// symbol of the .debug_frame section is given. The .debug_frame format has no
    /// augmentation data, uses absolute addresses and refers to CIEs by their section offset.
    /// </summary>
    internal sealed class DwarfEhFrame
    {
        private readonly SectionWriter _sectionWriter;
        private readonly bool _is64Bit;
        private readonly Utf8String _debugFrameSectionSymbolName;
        private readonly Dictionary<DwarfCie, uint> _cieOffset = new();

        public DwarfEhFrame(SectionWriter sectionWriter, bool is64Bit, Utf8String debugFrameSectionSymbolName = default)
        {
            _sectionWriter = sectionWriter;
            _is64Bit = is64Bit;
            _debugFrameSectionSymbolName = debugFrameSectionSymbolName;
        }

        private bool IsDebugFrame => !_debugFrameSectionSymbolName.IsNull;

        private uint PointerSize => _is64Bit ? 8u : 4u;

        public void AddCie(DwarfCie cie)
        {
            _cieOffset.Add(cie, (uint)_sectionWriter.Position);
            WriteCie(cie);
        }

        public void AddFde(in DwarfFde fde)
        {
            uint cieOffset;

            if (!_cieOffset.TryGetValue(fde.Cie, out cieOffset))
            {
                AddCie(fde.Cie);
                cieOffset = _cieOffset[fde.Cie];
            }

            WriteFde(fde, cieOffset);
        }

        private static uint PaddingSize(uint length, uint alignment)
        {
            return ((length + alignment - 1u) & ~(alignment - 1u)) - length;
        }

        private void WriteCie(DwarfCie cie)
        {
            if (IsDebugFrame)
            {
                WriteDebugFrameCie(cie);
                return;
            }

            Utf8StringBuilder augmentationString = new Utf8StringBuilder();
            uint augmentationLength = 0;

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
                (uint)augmentationString.Length + 1u + // null-terminator
                DwarfHelper.SizeOfULEB128(cie.CodeAlignFactor) +
                DwarfHelper.SizeOfSLEB128(cie.DataAlignFactor) +
                DwarfHelper.SizeOfULEB128(cie.ReturnAddressRegister) +
                (uint)(augmentationLength > 0 ? DwarfHelper.SizeOfULEB128(augmentationLength) + augmentationLength : 0) +
                (uint)cie.Instructions.Length;
            uint padding = PaddingSize(length, AddressSize(cie.PointerEncoding));

            _sectionWriter.WriteLittleEndian<uint>(length + padding - 4u);
            _sectionWriter.WriteLittleEndian<uint>(0);

            _sectionWriter.WriteByte(cie.ReturnAddressRegister < 0x7F ? (byte)1u : (byte)3u); // Version
            _sectionWriter.Write(augmentationString.AsSpan());
            _sectionWriter.WriteByte(0); // null-terminator

            _sectionWriter.WriteULEB128(cie.CodeAlignFactor);
            _sectionWriter.WriteSLEB128(cie.DataAlignFactor);
            _sectionWriter.WriteULEB128(cie.ReturnAddressRegister);

            _sectionWriter.WriteULEB128(augmentationLength);
            if (cie.PersonalitySymbolName != null)
            {
                _sectionWriter.WriteByte(cie.PersonalityEncoding);
                WriteAddress(cie.PersonalityEncoding, new Utf8String(cie.PersonalitySymbolName));
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

            _sectionWriter.WritePadding((int)padding);
        }

        private void WriteDebugFrameCie(DwarfCie cie)
        {
            uint length =
                4u + // Length
                4u + // CIE id
                1u + // Version
                1u + // Empty augmentation string
                DwarfHelper.SizeOfULEB128(cie.CodeAlignFactor) +
                DwarfHelper.SizeOfSLEB128(cie.DataAlignFactor) +
                DwarfHelper.SizeOfULEB128(cie.ReturnAddressRegister) +
                (uint)cie.Instructions.Length;
            uint padding = PaddingSize(length, PointerSize);

            _sectionWriter.WriteLittleEndian<uint>(length + padding - 4u);
            _sectionWriter.WriteLittleEndian<uint>(0xFFFFFFFFu);

            _sectionWriter.WriteByte(cie.ReturnAddressRegister < 0x7F ? (byte)1u : (byte)3u); // Version
            _sectionWriter.WriteByte(0); // Empty augmentation string

            _sectionWriter.WriteULEB128(cie.CodeAlignFactor);
            _sectionWriter.WriteSLEB128(cie.DataAlignFactor);
            _sectionWriter.WriteULEB128(cie.ReturnAddressRegister);

            _sectionWriter.Write(cie.Instructions);
            _sectionWriter.WritePadding((int)padding);
        }

        private void WriteDebugFrameFde(in DwarfFde fde, uint cieOffset)
        {
            uint length =
                4u + // Length
                4u + // CIE pointer
                PointerSize + // PC start
                PointerSize + // PC range
                (uint)fde.Instructions.Length;
            uint padding = PaddingSize(length, PointerSize);

            _sectionWriter.WriteLittleEndian<uint>(length + padding - 4u);
            _sectionWriter.EmitSymbolReference(RelocType.IMAGE_REL_BASED_HIGHLOW, _debugFrameSectionSymbolName, cieOffset);
            _sectionWriter.EmitSymbolReference(_is64Bit ? RelocType.IMAGE_REL_BASED_DIR64 : RelocType.IMAGE_REL_BASED_HIGHLOW, fde.PcStartSymbolName, fde.PcStartSymbolOffset);
            if (_is64Bit)
                _sectionWriter.WriteLittleEndian<ulong>(fde.PcLength);
            else
                _sectionWriter.WriteLittleEndian<uint>((uint)fde.PcLength);

            _sectionWriter.Write(fde.Instructions);
            _sectionWriter.WritePadding((int)padding);
        }

        private void WriteFde(in DwarfFde fde, uint cieOffset)
        {
            if (IsDebugFrame)
            {
                WriteDebugFrameFde(fde, cieOffset);
                return;
            }

            uint augmentationLength =
                fde.Cie.FdesHaveAugmentationData ?
                    1u + // Length
                    (fde.Cie.PersonalityEncoding != 0 ? AddressSize(fde.Cie.PersonalityEncoding) : 0) +
                    (fde.Cie.LsdaEncoding != 0 ? AddressSize(fde.Cie.LsdaEncoding) : 0) : 0;

            uint pointerEncodingSize = AddressSize(fde.Cie.PointerEncoding);
            uint length =
                4u + // Length
                4u + // CIE offset
                pointerEncodingSize + // PC start
                pointerEncodingSize + // PC end
                augmentationLength +
                (uint)fde.Instructions.Length;
            uint padding = PaddingSize(length, pointerEncodingSize);

            _sectionWriter.WriteLittleEndian<uint>(length + padding - 4u);
            _sectionWriter.WriteLittleEndian<uint>((uint)(_sectionWriter.Position - cieOffset));
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
            _sectionWriter.WritePadding((int)padding);
        }

        private uint AddressSize(byte encoding)
        {
            switch (encoding & 0xF)
            {
                case DW_EH_PE_ptr: return _is64Bit ? 8u : 4u;
                case DW_EH_PE_sdata4: return 4u;
                default:
                    throw new NotSupportedException();
            }
        }

        private void WriteAddress(byte encoding, Utf8String symbolName, long symbolOffset = 0)
        {
            if (!symbolName.IsNull)
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
                _sectionWriter.WritePadding((int)AddressSize(encoding));
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
