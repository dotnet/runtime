// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Reflection;
using ILCompiler.DependencyAnalysis;
using ILCompiler.DependencyAnalysisFramework;
using Internal.Text;
using Internal.TypeSystem;
using static ILCompiler.DependencyAnalysis.RelocType;
using static ILCompiler.ObjectWriter.ElfNative;

namespace ILCompiler.ObjectWriter
{
    /// <summary>
    /// ELF object file format writer for Linux/Unix targets.
    /// </summary>
    /// <remarks>
    /// ELF object format is described by the official specification hosted
    /// at https://refspecs.linuxfoundation.org/elf/elf.pdf. Different
    /// architectures specify the details in the ABI specification.
    ///
    /// Like COFF there are several quirks related to large number of sections
    /// (> 65279). Some of the fields in the ELF file header are moved to the
    /// first (NULL) section header. The symbol table that is normally a single
    /// section in the file is extended with a second .symtab_shndx section
    /// to accommodate the section indexes that don't fit within the regular
    /// section number field.
    /// </remarks>
    internal sealed partial class ElfObjectWriter : UnixObjectWriter
    {
        private static readonly ObjectNodeSection CompactUnwindRecordsSection = new ObjectNodeSection(CompactUnwindRecordsNode.SectionName, SectionType.ReadOnly);

        // GNU ld and lld place arm64 range extension thunks between input sections. Only arm64 opts in:
        // the split has not been validated with the arm and loongarch64 linkers and runtime.
        private protected override bool LinkerPlacesRangeExtensionThunks => _nodeFactory.Target.Architecture == TargetArchitecture.ARM64;

        private protected override void CreateEhSections()
        {
            base.CreateEhSections();

            if (_nodeFactory.CompactUnwindRecords is not null)
            {
                _hiddenUndefinedSymbols.Add(CompactUnwindRecordsNode.StartSymbolName);
                _hiddenUndefinedSymbols.Add(CompactUnwindRecordsNode.StopSymbolName);
            }
        }

        private protected override void AddCompactUnwindRecords(SectionWriter codeSectionWriter, INodeWithCodeInfo nodeWithCodeInfo, Utf8String currentSymbolName, bool shareSymbol, Utf8String[] lsdaSymbolNames)
        {
            FrameInfo[] frameInfos = nodeWithCodeInfo.FrameInfos;

            // The records of a method belong to its COMDAT group so that the linker keeps them with the method
            SectionWriter recordsSectionWriter = shareSymbol ?
                GetOrCreateSection(CompactUnwindRecordsSection, currentSymbolName, Utf8String.Concat("_cunwind0"u8, currentSymbolName.AsSpan())) :
                GetOrCreateSection(CompactUnwindRecordsSection);
            recordsSectionWriter.EmitAlignment(4);

            Span<byte> i_str = stackalloc byte[16];
            for (int i = 0; i < frameInfos.Length; i++)
            {
                FrameInfo frameInfo = frameInfos[i];

                // ARM method symbols include the Thumb bit, while the record refers to the first instruction
                Utf8String codeSymbolName = currentSymbolName;
                long codeOffset = frameInfo.StartOffset;
                if (_machine is EM_ARM)
                {
                    codeSymbolName = _utf8StringBuilder.Clear().Append("_fram"u8).Append(FormatUtf8Int(i_str, i)).Append(currentSymbolName).ToUtf8String();
                    codeSectionWriter.EmitSymbolDefinition(codeSymbolName, frameInfo.StartOffset);
                    codeOffset = 0;
                }

                recordsSectionWriter.EmitSymbolReference(IMAGE_REL_BASED_RELPTR32, codeSymbolName, codeOffset);
                recordsSectionWriter.WriteLittleEndian<uint>((uint)(frameInfo.EndOffset - frameInfo.StartOffset));
                recordsSectionWriter.EmitSymbolReference(IMAGE_REL_BASED_RELPTR32, lsdaSymbolNames[i]);
            }
        }
    }
}
