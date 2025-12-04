// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Buffers.Binary;
using System.Numerics;
using System.Reflection;
using ILCompiler.DependencyAnalysis;
using ILCompiler.DependencyAnalysisFramework;
using Internal.TypeSystem;
using static ILCompiler.DependencyAnalysis.RelocType;
using static ILCompiler.ObjectWriter.EabiNative;
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
    /// to accomodate the section indexes that don't fit within the regular
    /// section number field.
    /// </remarks>
    internal sealed partial class ElfObjectWriter : UnixObjectWriter
    {
        private Dictionary<int, (SectionWriter ExidxSectionWriter, SectionWriter ExtabSectionWriter)> _armUnwindSections;
        private static readonly ObjectNodeSection ArmUnwindIndexSection = new ObjectNodeSection(".ARM.exidx", SectionType.UnwindData);
        private static readonly ObjectNodeSection ArmUnwindTableSection = new ObjectNodeSection(".ARM.extab", SectionType.ReadOnly);

        private protected override void CreateEhSections()
        {
            // ARM creates the EHABI sections lazily in EmitUnwindInfo
            if (_machine is not EM_ARM)
            {
                base.CreateEhSections();
            }
        }

        private protected override void EmitUnwindInfo(
            SectionWriter sectionWriter,
            INodeWithCodeInfo nodeWithCodeInfo,
            string currentSymbolName)
        {
            if (_machine is not EM_ARM)
            {
                base.EmitUnwindInfo(sectionWriter, nodeWithCodeInfo, currentSymbolName);
                return;
            }

            if (nodeWithCodeInfo.FrameInfos is FrameInfo[] frameInfos &&
                nodeWithCodeInfo is ISymbolDefinitionNode)
            {
                SectionWriter exidxSectionWriter;
                SectionWriter extabSectionWriter;

                if (ShouldShareSymbol((ObjectNode)nodeWithCodeInfo))
                {
                    exidxSectionWriter = GetOrCreateSection(ArmUnwindIndexSection, currentSymbolName, $"_unwind0{currentSymbolName}");
                    extabSectionWriter = GetOrCreateSection(ArmUnwindTableSection, currentSymbolName, $"_extab0{currentSymbolName}");
                    _sections[exidxSectionWriter.SectionIndex].LinkSection = _sections[sectionWriter.SectionIndex];
                }
                else
                {
                    _armUnwindSections ??= new();
                    if (_armUnwindSections.TryGetValue(sectionWriter.SectionIndex, out var unwindSections))
                    {
                        exidxSectionWriter = unwindSections.ExidxSectionWriter;
                        extabSectionWriter = unwindSections.ExtabSectionWriter;
                    }
                    else
                    {
                        string sectionName = _sections[sectionWriter.SectionIndex].Name;
                        exidxSectionWriter = GetOrCreateSection(new ObjectNodeSection($"{ArmUnwindIndexSection.Name}{sectionName}", ArmUnwindIndexSection.Type));
                        extabSectionWriter = GetOrCreateSection(new ObjectNodeSection($"{ArmUnwindTableSection.Name}{sectionName}", ArmUnwindTableSection.Type));
                        _sections[exidxSectionWriter.SectionIndex].LinkSection = _sections[sectionWriter.SectionIndex];
                        _armUnwindSections.Add(sectionWriter.SectionIndex, (exidxSectionWriter, extabSectionWriter));
                    }
                }

                long mainLsdaOffset = 0;
                Span<byte> unwindWord = stackalloc byte[4];
                for (int i = 0; i < frameInfos.Length; i++)
                {
                    FrameInfo frameInfo = frameInfos[i];
                    int start = frameInfo.StartOffset;
                    int end = frameInfo.EndOffset;
                    byte[] blob = frameInfo.BlobData;

                    string framSymbolName = $"_fram{i}{currentSymbolName}";
                    string extabSymbolName = $"_extab{i}{currentSymbolName}";

                    sectionWriter.EmitSymbolDefinition(framSymbolName, start);

                    // Emit the index info
                    exidxSectionWriter.EmitSymbolReference(IMAGE_REL_ARM_PREL31, framSymbolName);
                    exidxSectionWriter.EmitSymbolReference(IMAGE_REL_ARM_PREL31, extabSymbolName);

                    Span<byte> armUnwindInfo = EabiUnwindConverter.ConvertCFIToEabi(blob);
                    string personalitySymbolName;

                    if (armUnwindInfo.Length <= 3)
                    {
                        personalitySymbolName = "__aeabi_unwind_cpp_pr0";
                        unwindWord[3] = 0x80;
                        unwindWord[2] = (byte)(armUnwindInfo.Length > 0 ? armUnwindInfo[0] : 0xB0);
                        unwindWord[1] = (byte)(armUnwindInfo.Length > 1 ? armUnwindInfo[1] : 0xB0);
                        unwindWord[0] = (byte)(armUnwindInfo.Length > 2 ? armUnwindInfo[2] : 0xB0);
                        armUnwindInfo = Span<byte>.Empty;
                    }
                    else
                    {
                        Debug.Assert(armUnwindInfo.Length <= 1024);
                        personalitySymbolName = "__aeabi_unwind_cpp_pr1";
                        unwindWord[3] = 0x81;
                        unwindWord[2] = (byte)(((armUnwindInfo.Length - 2) + 3) / 4);
                        unwindWord[1] = armUnwindInfo[0];
                        unwindWord[0] = armUnwindInfo[1];
                        armUnwindInfo = armUnwindInfo.Slice(2);
                    }

                    extabSectionWriter.EmitAlignment(4);
                    extabSectionWriter.EmitSymbolDefinition(extabSymbolName);

                    // ARM EHABI requires emitting a dummy relocation to the personality routine
                    // to tell the linker to preserve it.
                    extabSectionWriter.EmitRelocation(0, unwindWord, IMAGE_REL_BASED_ABSOLUTE, personalitySymbolName, 0);

                    // Emit the unwinding code. First word specifies the personality routine,
                    // format and first few bytes of the unwind code. For longer unwind codes
                    // the other words follow. They are padded with the "finish" instruction
                    // (0xB0).
                    extabSectionWriter.Write(unwindWord);
                    while (armUnwindInfo.Length > 0)
                    {
                        unwindWord[3] = (byte)(armUnwindInfo.Length > 0 ? armUnwindInfo[0] : 0xB0);
                        unwindWord[2] = (byte)(armUnwindInfo.Length > 1 ? armUnwindInfo[1] : 0xB0);
                        unwindWord[1] = (byte)(armUnwindInfo.Length > 2 ? armUnwindInfo[2] : 0xB0);
                        unwindWord[0] = (byte)(armUnwindInfo.Length > 3 ? armUnwindInfo[3] : 0xB0);
                        extabSectionWriter.Write(unwindWord);
                        armUnwindInfo = armUnwindInfo.Length > 3 ? armUnwindInfo.Slice(4) : Span<byte>.Empty;
                    }

                    // Emit our LSDA info directly into the exception table
                    EmitLsda(nodeWithCodeInfo, frameInfos, i, extabSectionWriter, ref mainLsdaOffset);
                }
            }
        }
    }
}
