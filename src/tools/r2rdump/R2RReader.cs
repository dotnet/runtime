// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Xml.Serialization;

namespace R2RDump
{
    /// <summary>
    /// This structure represents a single precode fixup cell decoded from the
    /// nibble-oriented per-method fixup blob. Each method entrypoint fixup
    /// represents an array of cells that must be fixed up before the method
    /// can start executing.
    /// </summary>
    public struct FixupCell
    {
        [XmlAttribute("Index")]
        public int Index { get; set; }

        /// <summary>
        /// Zero-based index of the import table within the import tables section.
        /// </summary>
        public uint TableIndex;

        /// <summary>
        /// Zero-based offset of the entry in the import table; it must be a multiple
        /// of the target architecture pointer size.
        /// </summary>
        public uint CellOffset;

        public FixupCell(int index, uint tableIndex, uint cellOffset)
        {
            Index = index;
            TableIndex = tableIndex;
            CellOffset = cellOffset;
        }
    }

    public class R2RReader
    {
        /// <summary>
        /// Underlying PE image reader is used to access raw PE structures like header
        /// or section list.
        /// </summary>
        public readonly PEReader PEReader;

        /// <summary>
        /// MetadataReader is used to access the MSIL metadata in the R2R file.
        /// </summary>
        public readonly MetadataReader MetadataReader;

        /// <summary>
        /// Byte array containing the ReadyToRun image
        /// </summary>
        public byte[] Image { get; }

        /// <summary>
        /// Name of the image file
        /// </summary>
        public string Filename { get; set; }

        /// <summary>
        /// True if the image is ReadyToRun
        /// </summary>
        public bool IsR2R { get; set; }

        /// <summary>
        /// The type of target machine
        /// </summary>
        public Machine Machine { get; set; }

        /// <summary>
        /// The preferred address of the first byte of image when loaded into memory; 
        /// must be a multiple of 64K.
        /// </summary>
        public ulong ImageBase { get; set; }

        /// <summary>
        /// The ReadyToRun header
        /// </summary>
        public R2RHeader R2RHeader { get; }

        /// <summary>
        /// The runtime functions and method signatures of each method
        /// </summary>
        public IList<R2RMethod> R2RMethods { get; }

        /// <summary>
        /// The available types from READYTORUN_SECTION_AVAILABLE_TYPES
        /// </summary>
        public IList<string> AvailableTypes { get; }

        /// <summary>
        /// The compiler identifier string from READYTORUN_SECTION_COMPILER_IDENTIFIER
        /// </summary>
        public string CompilerIdentifier { get; }

        public IList<R2RImportSection> ImportSections { get; }

        public unsafe R2RReader() { }

        /// <summary>
        /// Initializes the fields of the R2RHeader and R2RMethods
        /// </summary>
        /// <param name="filename">PE image</param>
        /// <exception cref="BadImageFormatException">The Cor header flag must be ILLibrary</exception>
        public unsafe R2RReader(string filename)
        {
            Filename = filename;
            Image = File.ReadAllBytes(filename);

            fixed (byte* p = Image)
            {
                IntPtr ptr = (IntPtr)p;
                PEReader = new PEReader(p, Image.Length);

                IsR2R = ((PEReader.PEHeaders.CorHeader.Flags & CorFlags.ILLibrary) != 0);
                if (!IsR2R)
                {
                    throw new BadImageFormatException("The file is not a ReadyToRun image");
                }

                Machine = PEReader.PEHeaders.CoffHeader.Machine;
				if (!Machine.IsDefined(typeof(Machine), Machine))
                {
                    Machine = Machine.Amd64;
                    R2RDump.WriteWarning($"Invalid Machine: {Machine}");
                }
                ImageBase = PEReader.PEHeaders.PEHeader.ImageBase;

                // initialize R2RHeader
                DirectoryEntry r2rHeaderDirectory = PEReader.PEHeaders.CorHeader.ManagedNativeHeaderDirectory;
                int r2rHeaderOffset = GetOffset(r2rHeaderDirectory.RelativeVirtualAddress);
                R2RHeader = new R2RHeader(Image, r2rHeaderDirectory.RelativeVirtualAddress, r2rHeaderOffset);
                if (r2rHeaderDirectory.Size != R2RHeader.Size)
                {
                    throw new BadImageFormatException("The calculated size of the R2RHeader doesn't match the size saved in the ManagedNativeHeaderDirectory");
                }

                if (PEReader.HasMetadata)
                {
                    MetadataReader = PEReader.GetMetadataReader();

                    R2RMethods = new List<R2RMethod>();
                    if (R2RHeader.Sections.ContainsKey(R2RSection.SectionType.READYTORUN_SECTION_RUNTIME_FUNCTIONS))
                    {
                        int runtimeFunctionSize = CalculateRuntimeFunctionSize();
                        R2RSection runtimeFunctionSection = R2RHeader.Sections[R2RSection.SectionType.READYTORUN_SECTION_RUNTIME_FUNCTIONS];
                        uint nRuntimeFunctions = (uint)(runtimeFunctionSection.Size / runtimeFunctionSize);
                        int runtimeFunctionOffset = GetOffset(runtimeFunctionSection.RelativeVirtualAddress);
                        bool[] isEntryPoint = new bool[nRuntimeFunctions];

                        // initialize R2RMethods
                        ParseMethodDefEntrypoints(isEntryPoint);
                        ParseInstanceMethodEntrypoints(isEntryPoint);
                        ParseRuntimeFunctions(isEntryPoint, runtimeFunctionOffset, runtimeFunctionSize);
                    }

                    AvailableTypes = new List<string>();
                    ParseAvailableTypes();

                    CompilerIdentifier = ParseCompilerIdentifier();

                    ImportSections = new List<R2RImportSection>();
                    ParseImportSections();
                }
            }
        }

        /// <summary>
        /// Each runtime function entry has 3 fields for Amd64 machines (StartAddress, EndAddress, UnwindRVA), otherwise 2 fields (StartAddress, UnwindRVA)
        /// </summary>
        private int CalculateRuntimeFunctionSize()
        {
            if (Machine == Machine.Amd64)
            {
                return 3 * sizeof(int);
            }
            return 2 * sizeof(int);
        }

        /// <summary>
        /// Initialize non-generic R2RMethods with method signatures from MethodDefHandle, and runtime function indices from MethodDefEntryPoints
        /// </summary>
        private void ParseMethodDefEntrypoints(bool[] isEntryPoint)
        {
            if (!R2RHeader.Sections.ContainsKey(R2RSection.SectionType.READYTORUN_SECTION_METHODDEF_ENTRYPOINTS))
            {
                return;
            }
            int methodDefEntryPointsRVA = R2RHeader.Sections[R2RSection.SectionType.READYTORUN_SECTION_METHODDEF_ENTRYPOINTS].RelativeVirtualAddress;
            int methodDefEntryPointsOffset = GetOffset(methodDefEntryPointsRVA);
            NativeArray methodEntryPoints = new NativeArray(Image, (uint)methodDefEntryPointsOffset);
            uint nMethodEntryPoints = methodEntryPoints.GetCount();

            for (uint rid = 1; rid <= nMethodEntryPoints; rid++)
            {
                int offset = 0;
                if (methodEntryPoints.TryGetAt(Image, rid - 1, ref offset))
                {
                    int runtimeFunctionId;
                    FixupCell[] fixups;
                    GetRuntimeFunctionIndexFromOffset(offset, out runtimeFunctionId, out fixups);
                    R2RMethod method = new R2RMethod(R2RMethods.Count, MetadataReader, rid, runtimeFunctionId, null, null, fixups);

                    if (method.EntryPointRuntimeFunctionId < 0 || method.EntryPointRuntimeFunctionId >= isEntryPoint.Length)
                    {
                        throw new BadImageFormatException("EntryPointRuntimeFunctionId out of bounds");
                    }
                    isEntryPoint[method.EntryPointRuntimeFunctionId] = true;
                    R2RMethods.Add(method);
                }
            }
        }

        /// <summary>
        /// Initialize generic method instances with argument types and runtime function indices from InstanceMethodEntrypoints
        /// </summary>
        private void ParseInstanceMethodEntrypoints(bool[] isEntryPoint)
        {
            if (!R2RHeader.Sections.ContainsKey(R2RSection.SectionType.READYTORUN_SECTION_INSTANCE_METHOD_ENTRYPOINTS))
            {
                return;
            }
            R2RSection instMethodEntryPointSection = R2RHeader.Sections[R2RSection.SectionType.READYTORUN_SECTION_INSTANCE_METHOD_ENTRYPOINTS];
            int instMethodEntryPointsOffset = GetOffset(instMethodEntryPointSection.RelativeVirtualAddress);
            NativeParser parser = new NativeParser(Image, (uint)instMethodEntryPointsOffset);
            NativeHashtable instMethodEntryPoints = new NativeHashtable(Image, parser, (uint)(instMethodEntryPointsOffset + instMethodEntryPointSection.Size));
            NativeHashtable.AllEntriesEnumerator allEntriesEnum = instMethodEntryPoints.EnumerateAllEntries();
            NativeParser curParser = allEntriesEnum.GetNext();
            while (!curParser.IsNull())
            {
                uint methodFlags = curParser.GetCompressedData();
                uint rid = curParser.GetCompressedData();
                if ((methodFlags & (byte)R2RMethod.EncodeMethodSigFlags.ENCODE_METHOD_SIG_MethodInstantiation) != 0)
                {
                    uint nArgs = curParser.GetCompressedData();
                    R2RMethod.GenericElementTypes[] args = new R2RMethod.GenericElementTypes[nArgs];
                    uint[] tokens = new uint[nArgs];
                    for (int i = 0; i < nArgs; i++)
                    {
                        args[i] = (R2RMethod.GenericElementTypes)curParser.GetByte();
                        if (args[i] == R2RMethod.GenericElementTypes.ValueType)
                        {
                            tokens[i] = curParser.GetCompressedData();
                            tokens[i] = (tokens[i] >> 2);
                        }
                    }

                    int runtimeFunctionId;
                    FixupCell[] fixups;
                    GetRuntimeFunctionIndexFromOffset((int)curParser.Offset, out runtimeFunctionId, out fixups);
                    R2RMethod method = new R2RMethod(R2RMethods.Count, MetadataReader, rid, runtimeFunctionId, args, tokens, fixups);
                    if (method.EntryPointRuntimeFunctionId >= 0 && method.EntryPointRuntimeFunctionId < isEntryPoint.Length)
                    {
                        isEntryPoint[method.EntryPointRuntimeFunctionId] = true;
                    }
                    R2RMethods.Add(method);
                }
                curParser = allEntriesEnum.GetNext();
            }
        }

        /// <summary>
        /// Get the RVAs of the runtime functions for each method
        /// based on <a href="https://github.com/dotnet/coreclr/blob/master/src/zap/zapcode.cpp">ZapUnwindInfo::Save</a>
        /// </summary>
        private void ParseRuntimeFunctions(bool[] isEntryPoint, int runtimeFunctionOffset, int runtimeFunctionSize)
        {
            int curOffset = 0;
            foreach (R2RMethod method in R2RMethods)
            {
                int runtimeFunctionId = method.EntryPointRuntimeFunctionId;
                if (runtimeFunctionId == -1)
                    continue;
                curOffset = runtimeFunctionOffset + runtimeFunctionId * runtimeFunctionSize;
                BaseGcInfo gcInfo = null;
                int codeOffset = 0;
                do
                {
                    int startRva = NativeReader.ReadInt32(Image, ref curOffset);
                    int endRva = -1;
                    if (Machine == Machine.Amd64)
                    {
                        endRva = NativeReader.ReadInt32(Image, ref curOffset);
                    }
                    int unwindRva = NativeReader.ReadInt32(Image, ref curOffset);
                    int unwindOffset = GetOffset(unwindRva);

                    BaseUnwindInfo unwindInfo = null;
                    if (Machine == Machine.Amd64)
                    {
                        unwindInfo = new Amd64.UnwindInfo(Image, unwindOffset);
                        if (isEntryPoint[runtimeFunctionId])
                        {
                            gcInfo = new Amd64.GcInfo(Image, unwindOffset + unwindInfo.Size, Machine, R2RHeader.MajorVersion);
                        }
                    }
                    else if (Machine == Machine.I386)
                    {
                        unwindInfo = new x86.UnwindInfo(Image, unwindOffset);
                        if (isEntryPoint[runtimeFunctionId])
                        {
                            gcInfo = new x86.GcInfo(Image, unwindOffset, Machine, R2RHeader.MajorVersion);
                        }
                    }

                    RuntimeFunction rtf = new RuntimeFunction(runtimeFunctionId, startRva, endRva, unwindRva, codeOffset, method, unwindInfo, gcInfo);
                    method.RuntimeFunctions.Add(rtf);
                    runtimeFunctionId++;
                    codeOffset += rtf.Size;
                }
                while (runtimeFunctionId < isEntryPoint.Length && !isEntryPoint[runtimeFunctionId]);
            }
        }

        /// <summary>
        /// Iterates through a native hashtable to get all RIDs
        /// </summary>
        private void ParseAvailableTypes()
        {
            if (!R2RHeader.Sections.ContainsKey(R2RSection.SectionType.READYTORUN_SECTION_AVAILABLE_TYPES))
            {
                return;
            }
            R2RSection availableTypesSection = R2RHeader.Sections[R2RSection.SectionType.READYTORUN_SECTION_AVAILABLE_TYPES];
            int availableTypesOffset = GetOffset(availableTypesSection.RelativeVirtualAddress);
            NativeParser parser = new NativeParser(Image, (uint)availableTypesOffset);
            NativeHashtable availableTypes = new NativeHashtable(Image, parser, (uint)(availableTypesOffset + availableTypesSection.Size));
            NativeHashtable.AllEntriesEnumerator allEntriesEnum = availableTypes.EnumerateAllEntries();
            NativeParser curParser = allEntriesEnum.GetNext();
            while (!curParser.IsNull())
            {
                uint rid = curParser.GetUnsigned();
                rid = rid >> 1;
                TypeDefinitionHandle typeDefHandle = MetadataTokens.TypeDefinitionHandle((int)rid);
                AvailableTypes.Add(GetTypeDefFullName(MetadataReader, typeDefHandle));
                curParser = allEntriesEnum.GetNext();
            }
        }

        /// <summary>
        /// Converts the bytes in the compiler identifier section to characters in a string
        /// </summary>
        private string ParseCompilerIdentifier()
        {
            if (!R2RHeader.Sections.ContainsKey(R2RSection.SectionType.READYTORUN_SECTION_COMPILER_IDENTIFIER))
            {
                return "";
            }
            R2RSection compilerIdentifierSection = R2RHeader.Sections[R2RSection.SectionType.READYTORUN_SECTION_COMPILER_IDENTIFIER];
            byte[] identifier = new byte[compilerIdentifierSection.Size - 1];
            int identifierOffset = GetOffset(compilerIdentifierSection.RelativeVirtualAddress);
            Array.Copy(Image, identifierOffset, identifier, 0, compilerIdentifierSection.Size - 1);
            return Encoding.UTF8.GetString(identifier);
        }

        /// <summary>
        /// based on <a href="https://github.com/dotnet/coreclr/blob/master/src/zap/zapimport.cpp">ZapImportSectionsTable::Save</a>
        /// </summary>
        private void ParseImportSections()
        {
            if (!R2RHeader.Sections.ContainsKey(R2RSection.SectionType.READYTORUN_SECTION_IMPORT_SECTIONS))
            {
                return;
            }
            R2RSection importSectionsSection = R2RHeader.Sections[R2RSection.SectionType.READYTORUN_SECTION_IMPORT_SECTIONS];
            int offset = GetOffset(importSectionsSection.RelativeVirtualAddress);
            int endOffset = offset + importSectionsSection.Size;
            while (offset < endOffset)
            {
                int rva = NativeReader.ReadInt32(Image, ref offset);
                int sectionOffset = GetOffset(rva);
                int startOffset = sectionOffset;
                int size = NativeReader.ReadInt32(Image, ref offset);
                R2RImportSection.CorCompileImportFlags flags = (R2RImportSection.CorCompileImportFlags)NativeReader.ReadUInt16(Image, ref offset);
                byte type = NativeReader.ReadByte(Image, ref offset);
                byte entrySize = NativeReader.ReadByte(Image, ref offset);
                int entryCount = 0;
                if (entrySize != 0)
                {
                    entryCount = size / entrySize;
                }
                int signatureRVA = NativeReader.ReadInt32(Image, ref offset);

                int signatureOffset = 0;
                if (signatureRVA != 0)
                {
                    signatureOffset = GetOffset(signatureRVA);
                }
                List<R2RImportSection.ImportSectionEntry> entries = new List<R2RImportSection.ImportSectionEntry>();
                switch (flags)
                {
                    case R2RImportSection.CorCompileImportFlags.CORCOMPILE_IMPORT_FLAGS_EAGER:
                        {
                            int tempSignatureOffset = signatureOffset;
                            int firstSigRva = NativeReader.ReadInt32(Image, ref tempSignatureOffset);
                            uint sigRva = 0;
                            while (sigRva != firstSigRva)
                            {
                                int entryOffset = sectionOffset - startOffset;
                                sigRva = NativeReader.ReadUInt32(Image, ref signatureOffset);
                                long section = NativeReader.ReadInt64(Image, ref sectionOffset);
                                int sigOff = GetOffset((int)sigRva);
                                int sigSampleLength = Math.Min(8, Image.Length - sigOff);
                                byte[] signatureSample = new byte[sigSampleLength];
                                Array.Copy(Image, sigOff, signatureSample, 0, sigSampleLength);
                                entries.Add(new R2RImportSection.ImportSectionEntry(entries.Count, entryOffset, section, sigRva, signatureSample));
                            }
                        }
                        break;
                    case R2RImportSection.CorCompileImportFlags.CORCOMPILE_IMPORT_FLAGS_CODE:
                    case R2RImportSection.CorCompileImportFlags.CORCOMPILE_IMPORT_FLAGS_PCODE:
                        for (int i = 0; i < entryCount; i++)
                        {
                            int entryOffset = sectionOffset - startOffset;
                            long section = NativeReader.ReadInt64(Image, ref sectionOffset);
                            uint sigRva = NativeReader.ReadUInt32(Image, ref signatureOffset);
                            int sigOff = GetOffset((int)sigRva);
                            int sigSampleLength = Math.Min(8, Image.Length - sigOff);
                            byte[] signatureSample = new byte[sigSampleLength];
                            Array.Copy(Image, sigOff, signatureSample, 0, sigSampleLength);
                            entries.Add(new R2RImportSection.ImportSectionEntry(entries.Count, entryOffset, section, sigRva, signatureSample));
                        }
                        break;
                }

                int auxDataRVA = NativeReader.ReadInt32(Image, ref offset);
                int auxDataOffset = 0;
                if (auxDataRVA != 0)
                {
                    auxDataOffset = GetOffset(auxDataRVA);
                }
                ImportSections.Add(new R2RImportSection(ImportSections.Count, Image, rva, size, flags, type, entrySize, signatureRVA, entries, auxDataRVA, auxDataOffset, Machine, R2RHeader.MajorVersion));
            }
        }

        /// <summary>
        /// Get the index in the image byte array corresponding to the RVA
        /// </summary>
        /// <param name="rva">The relative virtual address</param>
        public int GetOffset(int rva)
        {
            int index = PEReader.PEHeaders.GetContainingSectionIndex(rva);
            if (index == -1)
            {
                throw new BadImageFormatException("Failed to convert invalid RVA to offset: " + rva);
            }
            SectionHeader containingSection = PEReader.PEHeaders.SectionHeaders[index];
            return rva - containingSection.VirtualAddress + containingSection.PointerToRawData;
        }

        /// <summary>
        /// Get the full name of a type, including parent classes and namespace
        /// </summary>
        public static string GetTypeDefFullName(MetadataReader mdReader, TypeDefinitionHandle handle)
        {
            TypeDefinition typeDef;
            string typeStr = "";
            do
            {
                typeDef = mdReader.GetTypeDefinition(handle);
                typeStr = "." + mdReader.GetString(typeDef.Name) + typeStr;
                handle = typeDef.GetDeclaringType();
            }
            while (!handle.IsNil);

            return mdReader.GetString(typeDef.Namespace) + typeStr;
        }

        /// <summary>
        /// Reads the method entrypoint from the offset. Used for non-generic methods
        /// based on <a href="https://github.com/dotnet/coreclr/blob/master/src/debug/daccess/nidump.cpp">NativeImageDumper::DumpReadyToRunMethods</a>
        /// </summary>
        private void GetRuntimeFunctionIndexFromOffset(int offset, out int runtimeFunctionIndex, out FixupCell[] fixupCells)
        {
            fixupCells = null;

            // get the id of the entry point runtime function from the MethodEntryPoints NativeArray
            uint id = 0; // the RUNTIME_FUNCTIONS index
            offset = (int)NativeReader.DecodeUnsigned(Image, (uint)offset, ref id);
            if ((id & 1) != 0)
            {
                if ((id & 2) != 0)
                {
                    uint val = 0;
                    NativeReader.DecodeUnsigned(Image, (uint)offset, ref val);
                    offset -= (int)val;
                }

                fixupCells = DecodeFixupCells(offset);

                id >>= 2;
            }
            else
            {
                id >>= 1;
            }

            runtimeFunctionIndex = (int)id;
        }

        private FixupCell[] DecodeFixupCells(int offset)
        {
            List<FixupCell> cells = new List<FixupCell>();
            NibbleReader reader = new NibbleReader(Image, offset);

            // The following algorithm has been loosely ported from CoreCLR,
            // src\vm\ceeload.inl, BOOL Module::FixupDelayListAux
            uint curTableIndex = reader.ReadUInt();

            while (true)
            {
                uint fixupIndex = reader.ReadUInt(); // Accumulate the real rva from the delta encoded rva

                while (true)
                {
                    cells.Add(new FixupCell(cells.Count, curTableIndex, fixupIndex));

                    uint delta = reader.ReadUInt();

                    // Delta of 0 means end of entries in this table
                    if (delta == 0)
                        break;

                    fixupIndex += delta;
                }

                uint tableIndex = reader.ReadUInt();

                if (tableIndex == 0)
                    break;

                curTableIndex = curTableIndex + tableIndex;

            } // Done with all entries in this table

            return cells.ToArray();
        }
    }
}
