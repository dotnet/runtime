// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Text;

using ILCompiler.Reflection.ReadyToRun;
using Internal.Runtime;

namespace R2RDump
{
    class TextDumper : Dumper
    {
        public TextDumper(ReadyToRunReader r2r, TextWriter writer, Disassembler disassembler, DumpOptions options)
            : base(r2r, writer, disassembler, options)
        {
        }

        internal override void Begin()
        {
            if (!_options.Normalize)
            {
                _writer.WriteLine($"Filename: {_r2r.Filename}");
                _writer.WriteLine($"OS: {_r2r.OperatingSystem}");
                _writer.WriteLine($"Machine: {_r2r.Machine}");
                _writer.WriteLine($"ImageBase: 0x{_r2r.ImageBase:X8}");
                SkipLine();
            }
        }

        internal override void End()
        {
            _writer.WriteLine("=============================================================");
            SkipLine();
        }

        internal override void WriteDivider(string title)
        {
            int len = Math.Max(61 - title.Length - 2, 2);
            _writer.WriteLine(new String('=', len / 2) + " " + title + " " + new String('=', (len + 1) / 2));
            SkipLine();
        }

        internal override void WriteSubDivider()
        {
            _writer.WriteLine("_______________________________________________");
            SkipLine();
        }

        internal override void SkipLine()
        {
            _writer.WriteLine();
        }

        /// <summary>
        /// Dumps the R2RHeader and all the sections in the header
        /// </summary>
        internal override void DumpHeader(bool dumpSections)
        {
            _writer.WriteLine(_r2r.ReadyToRunHeader.ToString());

            if (_options.Raw)
            {
                DumpBytes(_r2r.ReadyToRunHeader.RelativeVirtualAddress, (uint)_r2r.ReadyToRunHeader.Size);
            }
            SkipLine();
            if (dumpSections)
            {
                WriteDivider("R2R Sections");
                _writer.WriteLine($"{_r2r.ReadyToRunHeader.Sections.Count} sections");
                SkipLine();

                foreach (ReadyToRunSection section in NormalizedSections(_r2r.ReadyToRunHeader))
                {
                    DumpSection(section);
                }

                if (_r2r.Composite)
                {
                    WriteDivider("Component Assembly Sections");
                    int assemblyIndex = 0;
                    foreach (string assemblyName in _r2r.ManifestReferenceAssemblies.OrderBy(kvp => kvp.Value).Select(kvp => kvp.Key))
                    {
                        WriteDivider($@"Component Assembly [{assemblyIndex}]: {assemblyName}");
                        ReadyToRunCoreHeader assemblyHeader = _r2r.ReadyToRunAssemblyHeaders[assemblyIndex];
                        foreach (ReadyToRunSection section in NormalizedSections(assemblyHeader))
                        {
                            DumpSection(section);
                        }
                        assemblyIndex++;
                    }

                }
            }
            SkipLine();
        }

        /// <summary>
        /// Dumps one R2RSection
        /// </summary>
        internal override void DumpSection(ReadyToRunSection section)
        {
            WriteSubDivider();
            section.WriteTo(_writer, _options);

            if (_options.Raw)
            {
                DumpBytes(section.RelativeVirtualAddress, (uint)section.Size);
                SkipLine();
            }
            if (_options.SectionContents)
            {
                DumpSectionContents(section);
                SkipLine();
            }
        }

        internal override void DumpEntryPoints()
        {
            WriteDivider($@"R2R Entry Points");
            foreach (ReadyToRunMethod method in NormalizedMethods())
            {
                _writer.WriteLine(method.SignatureString);
            }
        }

        internal override void DumpAllMethods()
        {
            WriteDivider("R2R Methods");
            _writer.WriteLine($"{_r2r.Methods.Sum(kvp => kvp.Value.Count)} methods");
            SkipLine();
            foreach (ReadyToRunMethod method in NormalizedMethods())
            {
                TextWriter temp = _writer;
                _writer = new StringWriter();
                DumpMethod(method);
                temp.Write(_writer.ToString());
                _writer = temp;
            }
        }

        /// <summary>
        /// Dumps one R2RMethod.
        /// </summary>
        internal override void DumpMethod(ReadyToRunMethod method)
        {
            WriteSubDivider();
            method.WriteTo(_writer, _options);

            if (_options.GC && method.GcInfo != null)
            {
                _writer.WriteLine("GC info:");
                _writer.Write(method.GcInfo);

                if (_options.Raw)
                {
                    DumpBytes(method.GcInfo.Offset, (uint)method.GcInfo.Size, "", false);
                }
            }
            SkipLine();

            foreach (RuntimeFunction runtimeFunction in method.RuntimeFunctions)
            {
                DumpRuntimeFunction(runtimeFunction);
            }
        }

        /// <summary>
        /// Dumps one runtime function.
        /// </summary>
        internal override void DumpRuntimeFunction(RuntimeFunction rtf)
        {
            _writer.WriteLine(rtf.Method.SignatureString);
            rtf.WriteTo(_writer, _options);

            if (_options.Disasm)
            {
                DumpDisasm(rtf, _r2r.GetOffset(rtf.StartAddress));
            }

            if (_options.Raw)
            {
                _writer.WriteLine("Raw Bytes:");
                DumpBytes(rtf.StartAddress, (uint)rtf.Size);
            }
            if (_options.Unwind)
            {
                _writer.WriteLine("UnwindInfo:");
                _writer.Write(rtf.UnwindInfo);
                if (_options.Raw)
                {
                    DumpBytes(rtf.UnwindRVA, (uint)rtf.UnwindInfo.Size);
                }
            }
            SkipLine();
        }

        /// <summary>
        /// Dumps disassembly and register liveness
        /// </summary>
        internal override void DumpDisasm(RuntimeFunction rtf, int imageOffset)
        {
            string indentString = new string(' ', _disassembler.MnemonicIndentation);
            int codeOffset = rtf.CodeOffset;
            int rtfOffset = 0;

            while (rtfOffset < rtf.Size)
            {
                string instr;
                int instrSize = _disassembler.GetInstruction(rtf, imageOffset, rtfOffset, out instr);

                if (_r2r.Machine == Machine.Amd64 && ((ILCompiler.Reflection.ReadyToRun.Amd64.UnwindInfo)rtf.UnwindInfo).UnwindCodes.ContainsKey(codeOffset))
                {
                    ILCompiler.Reflection.ReadyToRun.Amd64.UnwindCode code = ((ILCompiler.Reflection.ReadyToRun.Amd64.UnwindInfo)rtf.UnwindInfo).UnwindCodes[codeOffset];                    
                    _writer.Write($"{indentString}{code.UnwindOp} {code.OpInfoStr}");
                    if (code.NextFrameOffset != -1)
                    {
                        _writer.WriteLine($"{indentString}{code.NextFrameOffset}");
                    }
                    _writer.WriteLine();
                }

                if (!_options.HideTransitions && rtf.Method.GcInfo?.Transitions != null && rtf.Method.GcInfo.Transitions.TryGetValue(codeOffset, out List<BaseGcTransition> transitionsForOffset))
                {
                    string[] formattedTransitions = new string[transitionsForOffset.Count];
                    for (int transitionIndex = 0; transitionIndex < formattedTransitions.Length; transitionIndex++)
                    {
                        formattedTransitions[transitionIndex] = transitionsForOffset[transitionIndex].ToString();
                    }
                    if (_options.Normalize)
                    {
                        Array.Sort(formattedTransitions);
                    }
                    foreach (string transition in formattedTransitions)
                    {
                        _writer.WriteLine($"{indentString}{transition}");
                    }
                }

                /* According to https://msdn.microsoft.com/en-us/library/ck9asaa9.aspx and src/vm/gcinfodecoder.cpp
                 * UnwindCode and GcTransition CodeOffsets are encoded with a -1 adjustment (that is, it's the offset of the start of the next instruction)
                 */
                _writer.Write(instr);

                rtfOffset += instrSize;
                codeOffset += instrSize;
            }
        }

        /// <summary>
        /// Prints a formatted string containing a block of bytes from the relative virtual address and size
        /// </summary>
        internal override void DumpBytes(int rva, uint size, string name = "Raw", bool convertToOffset = true)
        {
            int start = rva;
            if (convertToOffset)
                start = _r2r.GetOffset(rva);
            if (start > _r2r.Image.Length || start + size > _r2r.Image.Length)
            {
                throw new IndexOutOfRangeException();
            }

            _writer.Write("    ");
            if (rva % 16 != 0)
            {
                int floor = rva / 16 * 16;
                _writer.Write($"{floor:X8}:");
                _writer.Write(new String(' ', (rva - floor) * 3));
            }
            for (uint i = 0; i < size; i++)
            {
                if ((rva + i) % 16 == 0)
                {
                    _writer.Write($"{rva + i:X8}:");
                }
                _writer.Write($" {_r2r.Image[start + i]:X2}");
                if ((rva + i) % 16 == 15 && i != size - 1)
                {
                    SkipLine();
                    _writer.Write("    ");
                }
            }
            SkipLine();
        }

        internal override void DumpSectionContents(ReadyToRunSection section)
        {
            switch (section.Type)
            {
                case ReadyToRunSectionType.AvailableTypes:
                    if (!_options.Naked)
                    {
                        uint availableTypesSectionOffset = (uint)_r2r.GetOffset(section.RelativeVirtualAddress);
                        NativeParser availableTypesParser = new NativeParser(_r2r.Image, availableTypesSectionOffset);
                        NativeHashtable availableTypes = new NativeHashtable(_r2r.Image, availableTypesParser, (uint)(availableTypesSectionOffset + section.Size));
                        _writer.WriteLine(availableTypes.ToString());
                    }

                    if (_r2r.AvailableTypes.TryGetValue(section, out List<string> sectionTypes))
                    {
                        _writer.WriteLine();
                        foreach (string name in sectionTypes)
                        {
                            _writer.WriteLine(name);
                        }
                    }
                    break;
                case ReadyToRunSectionType.MethodDefEntryPoints:
                    if (!_options.Naked)
                    {
                        NativeArray methodEntryPoints = new NativeArray(_r2r.Image, (uint)_r2r.GetOffset(section.RelativeVirtualAddress));
                        _writer.Write(methodEntryPoints.ToString());
                    }

                    if (_r2r.Methods.TryGetValue(section, out List<ReadyToRunMethod> sectionMethods))
                    {
                        _writer.WriteLine();
                        foreach (ReadyToRunMethod method in sectionMethods)
                        {
                            _writer.WriteLine($@"{MetadataTokens.GetToken(method.MethodHandle):X8}: {method.SignatureString}");
                        }
                    }
                    break;
                case ReadyToRunSectionType.InstanceMethodEntryPoints:
                    if (!_options.Naked)
                    {
                        uint instanceSectionOffset = (uint)_r2r.GetOffset(section.RelativeVirtualAddress);
                        NativeParser instanceParser = new NativeParser(_r2r.Image, instanceSectionOffset);
                        NativeHashtable instMethodEntryPoints = new NativeHashtable(_r2r.Image, instanceParser, (uint)(instanceSectionOffset + section.Size));
                        _writer.Write(instMethodEntryPoints.ToString());
                        _writer.WriteLine();
                    }
                    foreach (InstanceMethod instanceMethod in _r2r.InstanceMethods)
                    {
                        _writer.WriteLine($@"0x{instanceMethod.Bucket:X2} -> {instanceMethod.Method.SignatureString}");
                    }
                    break;
                case ReadyToRunSectionType.RuntimeFunctions:
                    int rtfOffset = _r2r.GetOffset(section.RelativeVirtualAddress);
                    int rtfEndOffset = rtfOffset + section.Size;
                    int rtfIndex = 0;
                    while (rtfOffset < rtfEndOffset)
                    {
                        int startRva = NativeReader.ReadInt32(_r2r.Image, ref rtfOffset);
                        int endRva = -1;
                        if (_r2r.Machine == Machine.Amd64)
                        {
                            endRva = NativeReader.ReadInt32(_r2r.Image, ref rtfOffset);
                        }
                        int unwindRva = NativeReader.ReadInt32(_r2r.Image, ref rtfOffset);
                        _writer.WriteLine($"Index: {rtfIndex}");
                        _writer.WriteLine($"        StartRva: 0x{startRva:X8}");
                        if (endRva != -1)
                            _writer.WriteLine($"        EndRva: 0x{endRva:X8}");
                        _writer.WriteLine($"        UnwindRva: 0x{unwindRva:X8}");
                        rtfIndex++;
                    }
                    break;
                case ReadyToRunSectionType.CompilerIdentifier:
                    _writer.WriteLine(_r2r.CompilerIdentifier);
                    break;
                case ReadyToRunSectionType.ImportSections:
                    if (_options.Naked)
                    {
                        DumpNakedImportSections();
                    }
                    else
                    {
                        foreach (ReadyToRunImportSection importSection in _r2r.ImportSections)
                        {
                            importSection.WriteTo(_writer);
                            if (_options.Raw && importSection.Entries.Count != 0)
                            {
                                if (importSection.SectionRVA != 0)
                                {
                                    _writer.WriteLine("Section Bytes:");
                                    DumpBytes(importSection.SectionRVA, (uint)importSection.SectionSize);
                                }
                                if (importSection.SignatureRVA != 0)
                                {
                                    _writer.WriteLine("Signature Bytes:");
                                    DumpBytes(importSection.SignatureRVA, (uint)importSection.Entries.Count * sizeof(int));
                                }
                                if (importSection.AuxiliaryDataRVA != 0 && importSection.AuxiliaryDataSize != 0)
                                {
                                    _writer.WriteLine("AuxiliaryData Bytes:");
                                    DumpBytes(importSection.AuxiliaryDataRVA, (uint)importSection.AuxiliaryDataSize);
                                }
                            }
                            foreach (ReadyToRunImportSection.ImportSectionEntry entry in importSection.Entries)
                            {
                                entry.WriteTo(_writer, _options);
                                _writer.WriteLine();
                            }
                            _writer.WriteLine();
                        }
                    }
                    break;
                case ReadyToRunSectionType.ManifestMetadata:
                    int assemblyRefCount = 0;
                    if (!_r2r.Composite)
                    {
                        MetadataReader globalReader = _r2r.GetGlobalMetadataReader();
                        assemblyRefCount = globalReader.GetTableRowCount(TableIndex.AssemblyRef) + 1;
                        _writer.WriteLine($"MSIL AssemblyRef's ({assemblyRefCount} entries):");
                        for (int assemblyRefIndex = 1; assemblyRefIndex < assemblyRefCount; assemblyRefIndex++)
                        {
                            AssemblyReference assemblyRef = globalReader.GetAssemblyReference(MetadataTokens.AssemblyReferenceHandle(assemblyRefIndex));
                            string assemblyRefName = globalReader.GetString(assemblyRef.Name);
                            _writer.WriteLine($"[ID 0x{assemblyRefIndex:X2}]: {assemblyRefName}");
                        }
                    }

                    _writer.WriteLine($"Manifest metadata AssemblyRef's ({_r2r.ManifestReferenceAssemblies.Count} entries):");
                    int manifestAsmIndex = 0;
                    foreach (string manifestReferenceAssembly in _r2r.ManifestReferenceAssemblies.OrderBy(kvp => kvp.Value).Select(kvp => kvp.Key))
                    {
                        _writer.WriteLine($"[ID 0x{manifestAsmIndex + assemblyRefCount + 1:X2}]: {manifestReferenceAssembly}");
                        manifestAsmIndex++;
                    }
                    break;
                case ReadyToRunSectionType.AttributePresence:
                    int attributesStartOffset = _r2r.GetOffset(section.RelativeVirtualAddress);
                    int attributesEndOffset = attributesStartOffset + section.Size;
                    NativeCuckooFilter attributes = new NativeCuckooFilter(_r2r.Image, attributesStartOffset, attributesEndOffset);
                    _writer.WriteLine("Attribute presence filter");
                    _writer.WriteLine(attributes.ToString());
                    break;
                case ReadyToRunSectionType.InliningInfo:
                    int iiOffset = _r2r.GetOffset(section.RelativeVirtualAddress);
                    int iiEndOffset = iiOffset + section.Size;
                    InliningInfoSection inliningInfoSection = new InliningInfoSection(_r2r, iiOffset, iiEndOffset);
                    _writer.WriteLine(inliningInfoSection.ToString());
                    break;
                case ReadyToRunSectionType.InliningInfo2:
                    int ii2Offset = _r2r.GetOffset(section.RelativeVirtualAddress);
                    int ii2EndOffset = ii2Offset + section.Size;
                    InliningInfoSection2 inliningInfoSection2 = new InliningInfoSection2(_r2r, ii2Offset, ii2EndOffset);
                    _writer.WriteLine(inliningInfoSection2.ToString());
                    break;
                case ReadyToRunSectionType.OwnerCompositeExecutable:
                    int oceOffset = _r2r.GetOffset(section.RelativeVirtualAddress);
                    if (_r2r.Image[oceOffset + section.Size - 1] != 0)
                    {
                        R2RDump.WriteWarning("String is not zero-terminated");
                    }
                    string ownerCompositeExecutable = Encoding.UTF8.GetString(_r2r.Image, oceOffset, section.Size - 1); // exclude the zero terminator
                    _writer.WriteLine("Composite executable: {0}", ownerCompositeExecutable.ToEscapedString());
                    break;
            }
        }

        private void DumpNakedImportSections()
        {
            List<ReadyToRunImportSection.ImportSectionEntry> entries = new List<ReadyToRunImportSection.ImportSectionEntry>();
            foreach (ReadyToRunImportSection importSection in _r2r.ImportSections)
            {
                entries.AddRange(importSection.Entries);
            }
            entries.Sort((e1, e2) => e1.Signature.CompareTo(e2.Signature));
            foreach (ReadyToRunImportSection.ImportSectionEntry entry in entries)
            {
                entry.WriteTo(_writer, _options);
                _writer.WriteLine();
            }
        }

        internal override void DumpQueryCount(string q, string title, int count)
        {
            _writer.WriteLine(count + " result(s) for \"" + q + "\"");
            SkipLine();
        }
    }
}
