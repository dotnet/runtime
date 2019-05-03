// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
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

        /// <summary>
        /// Fixup cell signature (textual representation of the typesystem object).
        /// </summary>
        public string Signature;

        public FixupCell(int index, uint tableIndex, uint cellOffset, string signature)
        {
            Index = index;
            TableIndex = tableIndex;
            CellOffset = cellOffset;
            Signature = signature;
        }
    }

    /// <summary>
    /// based on <a href="https://github.com/dotnet/coreclr/blob/master/src/inc/pedecoder.h">src/inc/pedecoder.h</a> IMAGE_FILE_MACHINE_NATIVE_OS_OVERRIDE
    /// </summary>
    public enum OperatingSystem
    {
        Apple = 0x4644,
        FreeBSD = 0xADC4,
        Linux = 0x7B79,
        NetBSD = 0x1993,
        Windows = 0,
        Unknown = -1
    }

    public struct InstanceMethod
    {
        public byte Bucket;
        public R2RMethod Method;

        public InstanceMethod(byte bucket, R2RMethod method)
        {
            Bucket = bucket;
            Method = method;
        }
    }

    public class EcmaMetadataReader
    {
        /// <summary>
        /// Option are used to specify details of signature formatting.
        /// </summary>
        public readonly DumpOptions Options;

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
        /// Extra reference assemblies parsed from the manifest metadata.
        /// Only used by R2R assemblies with larger version bubble.
        /// The manifest contains extra assembly references created by resolved
        /// inlines and facades (non-existent in the source MSIL).
        /// In module overrides, these assembly references are represented
        /// by indices larger than the number of AssemblyRef rows in MetadataReader.
        /// The list originates in the top-level R2R image and is copied
        /// to all reference assemblies for the sake of simplicity.
        /// </summary>
        public readonly List<string> ManifestReferenceAssemblies;

        /// <summary>
        /// Byte array containing the ReadyToRun image
        /// </summary>
        public byte[] Image { get; }

        /// <summary>
        /// Name of the image file
        /// </summary>
        public string Filename { get; set; }

        /// <summary>
        /// The default constructor initializes an empty metadata reader.
        /// </summary>
        public EcmaMetadataReader()
        {
        }

        /// <summary>
        /// Open an MSIL binary and locate the metadata blob.
        /// </summary>
        /// <param name="options">Ambient options to use</param>
        /// <param name="filename">PE image</param>
        /// <param name="manifestReferenceAssemblies">List of reference assemblies from the R2R metadata manifest</param>
        /// <exception cref="BadImageFormatException">The Cor header flag must be ILLibrary</exception>
        public unsafe EcmaMetadataReader(DumpOptions options, string filename, List<string> manifestReferenceAssemblies)
        {
            Options = options;
            Filename = filename;
            ManifestReferenceAssemblies = manifestReferenceAssemblies;
            Image = File.ReadAllBytes(filename);

            fixed (byte* p = Image)
            {
                IntPtr ptr = (IntPtr)p;
                PEReader = new PEReader(p, Image.Length);

                if (!PEReader.HasMetadata)
                {
                    throw new Exception($"ECMA metadata not found in file '{filename}'");
                }

                MetadataReader = PEReader.GetMetadataReader();
            }
        }

        /// <summary>
        /// Open a given reference assembly (relative to this ECMA metadata file).
        /// </summary>
        /// <param name="refAsmIndex">Reference assembly index</param>
        /// <returns>EcmaMetadataReader instance representing the reference assembly</returns>
        public EcmaMetadataReader OpenReferenceAssembly(int refAsmIndex)
        {
            if (refAsmIndex == 0)
            {
                return this;
            }

            int assemblyRefCount = MetadataReader.GetTableRowCount(TableIndex.AssemblyRef);
            string name;
            if (refAsmIndex <= assemblyRefCount)
            {
                AssemblyReference asmRef = MetadataReader.GetAssemblyReference(MetadataTokens.AssemblyReferenceHandle(refAsmIndex));
                name = MetadataReader.GetString(asmRef.Name);
            }
            else
            {
                name = ManifestReferenceAssemblies[refAsmIndex - assemblyRefCount - 2];
            }

            EcmaMetadataReader ecmaReader;
            if (!Options.AssemblyCache.TryGetValue(name, out ecmaReader))
            {
                string assemblyPath = Options.FindAssembly(name, Filename);
                if (assemblyPath == null)
                {
                    throw new Exception($"Missing reference assembly: {name}");
                }
                ecmaReader = new EcmaMetadataReader(Options, assemblyPath, ManifestReferenceAssemblies);
                Options.AssemblyCache.Add(name, ecmaReader);
            }
            return ecmaReader;
        }
    }

    public class R2RReader : EcmaMetadataReader
    {
        /// <summary>
        /// True if the image is ReadyToRun
        /// </summary>
        public bool IsR2R { get; set; }

        /// <summary>
        /// The type of target machine
        /// </summary>
        public Machine Machine { get; set; }

        /// <summary>
        /// Targeting operating system for the R2R executable
        /// </summary>
        public OperatingSystem OS { get; set; }

        /// <summary>
        /// Targeting processor architecture of the R2R executable
        /// </summary>
        public Architecture Architecture { get; set; }

        /// <summary>
        /// Pointer size in bytes for the target architecture
        /// </summary>
        public int PointerSize { get; set; }

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
        /// Parsed instance entrypoint table entries.
        /// </summary>
        public IList<InstanceMethod> InstanceMethods { get; }

        /// <summary>
        /// The available types from READYTORUN_SECTION_AVAILABLE_TYPES
        /// </summary>
        public IList<string> AvailableTypes { get; }

        /// <summary>
        /// The compiler identifier string from READYTORUN_SECTION_COMPILER_IDENTIFIER
        /// </summary>
        public string CompilerIdentifier { get; }

        /// <summary>
        /// Exception lookup table is used to map runtime function addresses to EH clauses.
        /// </summary>
        public EHLookupTable EHLookupTable { get; }

        /// <summary>
        /// List of import sections present in the R2R executable.
        /// </summary>
        public IList<R2RImportSection> ImportSections { get; }

        /// <summary>
        /// Map from import cell addresses to their symbolic names.
        /// </summary>
        public Dictionary<int, string> ImportCellNames { get; }

        private Dictionary<int, DebugInfo> _runtimeFunctionToDebugInfo = new Dictionary<int, DebugInfo>();

        public R2RReader() { }

        /// <summary>
        /// Initializes the fields of the R2RHeader and R2RMethods
        /// </summary>
        /// <param name="filename">PE image</param>
        /// <exception cref="BadImageFormatException">The Cor header flag must be ILLibrary</exception>
        public unsafe R2RReader(DumpOptions options, string filename)
            : base(options, filename, new List<string>())
        {
            IsR2R = ((PEReader.PEHeaders.CorHeader.Flags & CorFlags.ILLibrary) != 0);
            if (!IsR2R)
            {
                throw new BadImageFormatException("The file is not a ReadyToRun image");
            }

            uint machine = (uint)PEReader.PEHeaders.CoffHeader.Machine;
            OS = OperatingSystem.Unknown;
            foreach (OperatingSystem os in Enum.GetValues(typeof(OperatingSystem)))
            {
                Machine = (Machine)(machine ^ (uint)os);
                if (Enum.IsDefined(typeof(Machine), Machine))
                {
                    OS = os;
                    break;
                }
            }
            if (OS == OperatingSystem.Unknown)
            {
                throw new BadImageFormatException($"Invalid Machine: {machine}");
            }

            switch (Machine)
            {
                case Machine.I386:
                    Architecture = Architecture.X86;
                    PointerSize = 4;
                    break;

                case Machine.Amd64:
                    Architecture = Architecture.X64;
                    PointerSize = 8;
                    break;

                case Machine.Arm:
                case Machine.Thumb:
                case Machine.ArmThumb2:
                    Architecture = Architecture.Arm;
                    PointerSize = 4;
                    break;

                case Machine.Arm64:
                    Architecture = Architecture.Arm64;
                    PointerSize = 8;
                    break;

                default:
                    throw new NotImplementedException(Machine.ToString());
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

            ParseDebugInfo();

            if (R2RHeader.Sections.ContainsKey(R2RSection.SectionType.READYTORUN_SECTION_MANIFEST_METADATA))
            {
                R2RSection manifestMetadata = R2RHeader.Sections[R2RSection.SectionType.READYTORUN_SECTION_MANIFEST_METADATA];
                fixed (byte* image = Image)
                {
                    MetadataReader manifestReader = new MetadataReader(image + GetOffset(manifestMetadata.RelativeVirtualAddress), manifestMetadata.Size);
                    int assemblyRefCount = manifestReader.GetTableRowCount(TableIndex.AssemblyRef);
                    for (int assemblyRefIndex = 1; assemblyRefIndex <= assemblyRefCount; assemblyRefIndex++)
                    {
                        AssemblyReferenceHandle asmRefHandle = MetadataTokens.AssemblyReferenceHandle(assemblyRefIndex);
                        AssemblyReference asmRef = manifestReader.GetAssemblyReference(asmRefHandle);
                        string asmRefName = manifestReader.GetString(asmRef.Name);
                        ManifestReferenceAssemblies.Add(asmRefName);
                    }
                }
            }

            if (R2RHeader.Sections.ContainsKey(R2RSection.SectionType.READYTORUN_SECTION_EXCEPTION_INFO))
            {
                R2RSection exceptionInfoSection = R2RHeader.Sections[R2RSection.SectionType.READYTORUN_SECTION_EXCEPTION_INFO];
                EHLookupTable = new EHLookupTable(Image, GetOffset(exceptionInfoSection.RelativeVirtualAddress), exceptionInfoSection.Size);
            }

            ImportSections = new List<R2RImportSection>();
            ImportCellNames = new Dictionary<int, string>();
            ParseImportSections();

            R2RMethods = new List<R2RMethod>();
            InstanceMethods = new List<InstanceMethod>();

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
        }

        public bool InputArchitectureSupported()
        {
            return Machine != Machine.ArmThumb2; // CoreDisTools often fails to decode when disassembling ARM images (see https://github.com/dotnet/coreclr/issues/19637)
        }

        // TODO: Fix R2RDump issue where an R2R image cannot be dissassembled with the x86 CoreDisTools
        // For the short term, we want to error out with a decent message explaining the unexpected error
        // Issue #19564: https://github.com/dotnet/coreclr/issues/19564
        public bool DisassemblerArchitectureSupported()
        {
            System.Runtime.InteropServices.Architecture val = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture;
            return val != System.Runtime.InteropServices.Architecture.X86;
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
                    EntityHandle methodHandle = MetadataTokens.MethodDefinitionHandle((int)rid);
                    int runtimeFunctionId;
                    FixupCell[] fixups;
                    GetRuntimeFunctionIndexFromOffset(offset, out runtimeFunctionId, out fixups);
                    R2RMethod method = new R2RMethod(R2RMethods.Count, this, methodHandle, runtimeFunctionId, owningType: null, constrainedType: null, instanceArgs: null, fixups: fixups);

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
                SignatureDecoder decoder = new SignatureDecoder(Options, this, (int)curParser.Offset);

                string owningType = null;

                uint methodFlags = decoder.ReadUInt();
                if ((methodFlags & (uint)ReadyToRunMethodSigFlags.READYTORUN_METHOD_SIG_OwnerType) != 0)
                {
                    owningType = decoder.ReadTypeSignatureNoEmit();
                }
                if ((methodFlags & (uint)ReadyToRunMethodSigFlags.READYTORUN_METHOD_SIG_SlotInsteadOfToken) != 0)
                {
                    throw new NotImplementedException();
                }
                EntityHandle methodHandle;
                int rid = (int)decoder.ReadUInt();
                if ((methodFlags & (uint)ReadyToRunMethodSigFlags.READYTORUN_METHOD_SIG_MemberRefToken) != 0)
                {
                    methodHandle = MetadataTokens.MemberReferenceHandle(rid);
                }
                else
                {
                    methodHandle = MetadataTokens.MethodDefinitionHandle(rid);
                }
                string[] methodTypeArgs = null;
                if ((methodFlags & (uint)ReadyToRunMethodSigFlags.READYTORUN_METHOD_SIG_MethodInstantiation) != 0)
                {
                    uint typeArgCount = decoder.ReadUInt();
                    methodTypeArgs = new string[typeArgCount];
                    for (int typeArgIndex = 0; typeArgIndex < typeArgCount; typeArgIndex++)
                    {
                        methodTypeArgs[typeArgIndex] = decoder.ReadTypeSignatureNoEmit();
                    }
                }

                string constrainedType = null;
                if ((methodFlags & (uint)ReadyToRunMethodSigFlags.READYTORUN_METHOD_SIG_Constrained) != 0)
                {
                    constrainedType = decoder.ReadTypeSignatureNoEmit();
                }

                int runtimeFunctionId;
                FixupCell[] fixups;
                GetRuntimeFunctionIndexFromOffset((int)decoder.Offset, out runtimeFunctionId, out fixups);
                R2RMethod method = new R2RMethod(R2RMethods.Count, this, methodHandle, runtimeFunctionId, owningType, constrainedType, methodTypeArgs, fixups);
                if (method.EntryPointRuntimeFunctionId >= 0 && method.EntryPointRuntimeFunctionId < isEntryPoint.Length)
                {
                    isEntryPoint[method.EntryPointRuntimeFunctionId] = true;
                }
                R2RMethods.Add(method);
                InstanceMethods.Add(new InstanceMethod(curParser.LowHashcode, method));
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
                            try
                            {
                                gcInfo = new Amd64.GcInfo(Image, unwindOffset + unwindInfo.Size, Machine, R2RHeader.MajorVersion);
                            }
                            catch (IndexOutOfRangeException)
                            {
                                Console.WriteLine($"Warning: Could not parse GC Info for method: {method.SignatureString}");
                            }

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
                    else if (Machine == Machine.ArmThumb2)
                    {
                        unwindInfo = new Arm.UnwindInfo(Image, unwindOffset);
                        if (isEntryPoint[runtimeFunctionId])
                        {
                            gcInfo = new Amd64.GcInfo(Image, unwindOffset + unwindInfo.Size, Machine, R2RHeader.MajorVersion); // Arm and Arm64 use the same GcInfo format as x64
                        }
                    }
                    else if (Machine == Machine.Arm64)
                    {
                        unwindInfo = new Arm64.UnwindInfo(Image, unwindOffset);
                        if (isEntryPoint[runtimeFunctionId])
                        {
                            gcInfo = new Amd64.GcInfo(Image, unwindOffset + unwindInfo.Size, Machine, R2RHeader.MajorVersion);
                        }
                    }

                    EHInfo ehInfo = null;

                    EHInfoLocation ehInfoLocation;
                    if (EHLookupTable != null && EHLookupTable.RuntimeFunctionToEHInfoMap.TryGetValue(startRva, out ehInfoLocation))
                    {
                        ehInfo = new EHInfo(this, ehInfoLocation.EHInfoRVA, startRva, GetOffset(ehInfoLocation.EHInfoRVA), ehInfoLocation.ClauseCount);
                    }

                    RuntimeFunction rtf = new RuntimeFunction(
                        runtimeFunctionId,
                        startRva,
                        endRva,
                        unwindRva,
                        codeOffset,
                        method,
                        unwindInfo,
                        gcInfo,
                        ehInfo,
                        _runtimeFunctionToDebugInfo.GetValueOrDefault(runtimeFunctionId));

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

                bool isExportedType = (rid & 1) != 0;
                rid = rid >> 1;

                if (isExportedType)
                {
                    ExportedTypeHandle exportedTypeHandle = MetadataTokens.ExportedTypeHandle((int)rid);
                    string exportedTypeName = GetExportedTypeFullName(MetadataReader, exportedTypeHandle);
                    AvailableTypes.Add("exported " + exportedTypeName);
                }
                else
                {
                    TypeDefinitionHandle typeDefHandle = MetadataTokens.TypeDefinitionHandle((int)rid);
                    string typeDefName = MetadataNameFormatter.FormatHandle(MetadataReader, typeDefHandle);
                    AvailableTypes.Add(typeDefName);
                }

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
                CorCompileImportFlags flags = (CorCompileImportFlags)NativeReader.ReadUInt16(Image, ref offset);
                byte type = NativeReader.ReadByte(Image, ref offset);
                byte entrySize = NativeReader.ReadByte(Image, ref offset);
                if (entrySize == 0)
                {
                    switch (Machine)
                    {
                        case Machine.I386:
                        case Machine.ArmThumb2:
                            entrySize = 4;
                            break;

                        case Machine.Amd64:
                        case Machine.Arm64:
                            entrySize = 8;
                            break;

                        default:
                            throw new NotImplementedException(Machine.ToString());
                    }
                }
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
                for (int i = 0; i < entryCount; i++)
                {
                    int entryOffset = sectionOffset - startOffset;
                    long section = NativeReader.ReadInt64(Image, ref sectionOffset);
                    uint sigRva = NativeReader.ReadUInt32(Image, ref signatureOffset);
                    int sigOffset = GetOffset((int)sigRva);
                    string cellName = MetadataNameFormatter.FormatSignature(Options, this, sigOffset);
                    entries.Add(new R2RImportSection.ImportSectionEntry(entries.Count, entryOffset, entryOffset + rva, section, sigRva, cellName));
                    ImportCellNames.Add(rva + entrySize * i, cellName);
                }

                int auxDataRVA = NativeReader.ReadInt32(Image, ref offset);
                int auxDataOffset = 0;
                if (auxDataRVA != 0)
                {
                    auxDataOffset = GetOffset(auxDataRVA);
                }
                ImportSections.Add(new R2RImportSection(ImportSections.Count, this, rva, size, flags, type, entrySize, signatureRVA, entries, auxDataRVA, auxDataOffset, Machine, R2RHeader.MajorVersion));
            }
        }

        private void ParseDebugInfo()
        {
            if (!R2RHeader.Sections.ContainsKey(R2RSection.SectionType.READYTORUN_SECTION_DEBUG_INFO))
            {
                return;
            }

            R2RSection debugInfoSection = R2RHeader.Sections[R2RSection.SectionType.READYTORUN_SECTION_DEBUG_INFO];
            int debugInfoSectionOffset = GetOffset(debugInfoSection.RelativeVirtualAddress);

            NativeArray debugInfoArray = new NativeArray(Image, (uint)debugInfoSectionOffset);
            for (uint i = 0; i < debugInfoArray.GetCount(); ++i)
            {
                int offset = 0;
                if (!debugInfoArray.TryGetAt(Image, i, ref offset))
                {
                    continue;
                }

                var debugInfo = new DebugInfo(Image, offset, Machine);
                _runtimeFunctionToDebugInfo.Add((int)i, debugInfo);
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
        /// Get the full name of an ExportedType, including namespace
        /// </summary>
        public static string GetExportedTypeFullName(MetadataReader mdReader, ExportedTypeHandle handle)
        {
            string typeNamespace = "";
            string typeStr = "";
            try
            {
                ExportedType exportedType = mdReader.GetExportedType(handle);
                typeStr = "." + mdReader.GetString(exportedType.Name) + typeStr;
                typeNamespace = mdReader.GetString(exportedType.Namespace);
            }
            catch (BadImageFormatException)
            {
                return null;
            }
            return typeNamespace + typeStr;
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
                    R2RImportSection importSection = ImportSections[(int)curTableIndex];
                    R2RImportSection.ImportSectionEntry entry = importSection.Entries[(int)fixupIndex];
                    cells.Add(new FixupCell(cells.Count, curTableIndex, fixupIndex, entry.Signature));

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
