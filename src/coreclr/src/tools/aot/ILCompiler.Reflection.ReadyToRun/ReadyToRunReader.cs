// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

using Internal.CorConstants;
using Internal.Runtime;
using Internal.ReadyToRunConstants;

using Debug = System.Diagnostics.Debug;
using System.Linq;

namespace ILCompiler.Reflection.ReadyToRun
{
    /// <summary>
    /// based on <a href="https://github.com/dotnet/coreclr/blob/master/src/inc/pedecoder.h">src/inc/pedecoder.h</a> IMAGE_FILE_MACHINE_NATIVE_OS_OVERRIDE
    /// </summary>
    public enum OperatingSystem
    {
        Apple = 0x4644,
        FreeBSD = 0xADC4,
        Linux = 0x7B79,
        NetBSD = 0x1993,
        SunOS = 0x1992,
        Windows = 0,
        Unknown = -1
    }

    public struct InstanceMethod
    {
        public byte Bucket;
        public ReadyToRunMethod Method;

        public InstanceMethod(byte bucket, ReadyToRunMethod method)
        {
            Bucket = bucket;
            Method = method;
        }
    }

    public sealed class ReadyToRunReader
    {
        private const string SystemModuleName = "System.Private.CoreLib";

        /// <summary>
        /// MetadataReader for the system module (normally System.Private.CoreLib)
        /// </summary>
        private MetadataReader _systemModuleReader;

        private readonly IAssemblyResolver _assemblyResolver;

        /// <summary>
        /// Reference assembly cache indexed by module indices as used in signatures
        /// </summary>
        private List<MetadataReader> _assemblyCache;

        /// <summary>
        /// Assembly headers for composite R2R images
        /// </summary>
        private List<ReadyToRunCoreHeader> _assemblyHeaders;

        // Header
        private OperatingSystem _operatingSystem;
        private Machine _machine;
        private Architecture _architecture;
        private int _pointerSize;
        private bool _composite;
        private ulong _imageBase;
        private int _readyToRunHeaderRVA;
        private ReadyToRunHeader _readyToRunHeader;
        private List<ReadyToRunCoreHeader> _readyToRunAssemblyHeaders;

        // DebugInfo
        private Dictionary<int, int> _runtimeFunctionIdToDebugOffset;

        // ManifestReferences
        private MetadataReader _manifestReader;
        private List<AssemblyReferenceHandle> _manifestReferences;
        private Dictionary<string, int> _manifestReferenceAssemblies;

        // ExceptionInfo
        private Dictionary<int, EHInfo> _runtimeFunctionToEHInfo;

        // Methods
        private Dictionary<ReadyToRunSection, List<ReadyToRunMethod>> _methods;
        private List<InstanceMethod> _instanceMethods;

        // ImportSections
        private List<ReadyToRunImportSection> _importSections;
        private Dictionary<int, string> _importCellNames;
        private Dictionary<int, ReadyToRunSignature> _importSignatures;

        // AvailableType
        private Dictionary<ReadyToRunSection, List<string>> _availableTypes;

        // CompilerIdentifier
        private string _compilerIdentifier;

        /// <summary>
        /// Underlying PE image reader is used to access raw PE structures like header
        /// or section list.
        /// </summary>
        public PEReader PEReader { get; private set; }

        /// <summary>
        /// Byte array containing the ReadyToRun image
        /// </summary>
        public byte[] Image { get; private set; }

        /// <summary>
        /// Name of the image file
        /// </summary>
        public string Filename { get; private set; }

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
        public Dictionary<string, int> ManifestReferenceAssemblies
        {
            get
            {
                EnsureManifestReferenceAssemblies();
                return _manifestReferenceAssemblies;
            }
        }

        /// <summary>
        /// The type of target machine
        /// </summary>
        public Machine Machine
        {
            get
            {
                EnsureHeader();
                return _machine;
            }
        }

        /// <summary>
        /// Targeting operating system for the R2R executable
        /// </summary>
        public OperatingSystem OperatingSystem
        {
            get
            {
                EnsureHeader();
                return _operatingSystem;
            }
        }

        /// <summary>
        /// Targeting processor architecture of the R2R executable
        /// </summary>
        public Architecture Architecture
        {
            get
            {
                EnsureHeader();
                return _architecture;
            }
        }

        /// <summary>
        /// Size of a pointer on the architecture
        /// </summary>
        public int TargetPointerSize
        {
            get
            {
                EnsureHeader();
                return _pointerSize;
            }
        }

        /// <summary>
        /// Return true when the executable is a composite R2R image.
        /// </summary>
        public bool Composite
        {
            get
            {
                EnsureHeader();
                return _composite;
            }
        }

        /// <summary>
        /// The preferred address of the first byte of image when loaded into memory;
        /// must be a multiple of 64K.
        /// </summary>
        public ulong ImageBase
        {
            get
            {
                EnsureHeader();
                return _imageBase;
            }
        }

        /// <summary>
        /// The ReadyToRun header
        /// </summary>
        public ReadyToRunHeader ReadyToRunHeader
        {
            get
            {
                EnsureHeader();
                return _readyToRunHeader;
            }
        }

        public IReadOnlyList<ReadyToRunCoreHeader> ReadyToRunAssemblyHeaders
        {
            get
            {
                EnsureHeader();
                return _readyToRunAssemblyHeaders;
            }
        }

        /// <summary>
        /// The runtime functions and method signatures of each method
        /// </summary>
        public Dictionary<ReadyToRunSection, List<ReadyToRunMethod>> Methods
        {
            get
            {
                EnsureMethods();
                return _methods;
            }
        }

        /// <summary>
        /// Parsed instance entrypoint table entries.
        /// </summary>
        public IReadOnlyList<InstanceMethod> InstanceMethods
        {
            get
            {
                EnsureMethods();
                return _instanceMethods;
            }
        }

        /// <summary>
        /// The available types from READYTORUN_SECTION_AVAILABLE_TYPES
        /// </summary>
        public Dictionary<ReadyToRunSection, List<string>> AvailableTypes
        {

            get
            {
                EnsureAvailableTypes();
                return _availableTypes;
            }

        }

        /// <summary>
        /// The compiler identifier string from READYTORUN_SECTION_COMPILER_IDENTIFIER
        /// </summary>
        public string CompilerIdentifier
        {
            get
            {
                EnsureCompilerIdentifier();
                return _compilerIdentifier;
            }
        }

        /// <summary>
        /// List of import sections present in the R2R executable.
        /// </summary>
        public IReadOnlyList<ReadyToRunImportSection> ImportSections
        {
            get
            {
                EnsureImportSections();
                return _importSections;
            }
        }

        /// <summary>
        /// Map from import cell addresses to their symbolic names.
        /// </summary>
        public IReadOnlyDictionary<int, string> ImportCellNames
        {
            get
            {
                EnsureImportSections();
                return _importCellNames;
            }

        }

        /// <summary>
        /// Map from import cell addresses to their symbolic names.
        /// </summary>
        public IReadOnlyDictionary<int, ReadyToRunSignature> ImportSignatures
        {
            get
            {
                EnsureImportSections();
                return _importSignatures;
            }

        }

        internal Dictionary<int, int> RuntimeFunctionToDebugInfo
        {
            get
            {
                EnsureDebugInfo();
                return _runtimeFunctionIdToDebugOffset;
            }
        }

        internal Dictionary<int, EHInfo> RuntimeFunctionToEHInfo
        {
            get
            {
                EnsureExceptionInfo();
                return _runtimeFunctionToEHInfo;
            }
        }

        internal List<AssemblyReferenceHandle> ManifestReferences
        {
            get
            {
                EnsureManifestReferences();
                return _manifestReferences;
            }
        }

        internal MetadataReader ManifestReader
        {
            get
            {
                EnsureManifestReferences();
                return _manifestReader;
            }
        }

        /// <summary>
        /// Initializes the fields of the R2RHeader and R2RMethods
        /// </summary>
        /// <param name="filename">PE image</param>
        /// <exception cref="BadImageFormatException">The Cor header flag must be ILLibrary</exception>
        public ReadyToRunReader(IAssemblyResolver assemblyResolver, MetadataReader metadata, PEReader peReader, string filename)
        {
            _assemblyResolver = assemblyResolver;
            PEReader = peReader;
            Filename = filename;
            Initialize(metadata);
        }

        /// <summary>
        /// Initializes the fields of the R2RHeader and R2RMethods
        /// </summary>
        /// <param name="filename">PE image</param>
        /// <exception cref="BadImageFormatException">The Cor header flag must be ILLibrary</exception>
        public unsafe ReadyToRunReader(IAssemblyResolver assemblyResolver, string filename)
        {
            _assemblyResolver = assemblyResolver;
            Filename = filename;
            Initialize(metadata: null);
        }

        private unsafe void Initialize(MetadataReader metadata)
        {
            _assemblyCache = new List<MetadataReader>();
            _assemblyHeaders = new List<ReadyToRunCoreHeader>();

            if (PEReader == null)
            {
                byte[] image = File.ReadAllBytes(Filename);
                Image = image;

                PEReader = new PEReader(Unsafe.As<byte[], ImmutableArray<byte>>(ref image));
            }
            else
            {
                ImmutableArray<byte> content = PEReader.GetEntireImage().GetContent();
                Image = Unsafe.As<ImmutableArray<byte>, byte[]>(ref content);
            }

            if (metadata == null && PEReader.HasMetadata)
            {
                metadata = PEReader.GetMetadataReader();
            }

            if (metadata != null)
            {
                if ((PEReader.PEHeaders.CorHeader.Flags & CorFlags.ILLibrary) == 0)
                {
                    if (!TryLocateNativeReadyToRunHeader())
                        throw new BadImageFormatException("The file is not a ReadyToRun image");

                    Debug.Assert(Composite);
                }
                else
                {
                    _assemblyCache.Add(metadata);

                    DirectoryEntry r2rHeaderDirectory = PEReader.PEHeaders.CorHeader.ManagedNativeHeaderDirectory;
                    _readyToRunHeaderRVA = r2rHeaderDirectory.RelativeVirtualAddress;
                    Debug.Assert(!Composite);
                }

            }
            else if (!TryLocateNativeReadyToRunHeader())
            {
                throw new BadImageFormatException($"ECMA metadata / RTR_HEADER not found in file '{Filename}'");
            }
        }

        private void EnsureMethods()
        {
            if (_methods != null)
            {
                return;
            }

            _methods = new Dictionary<ReadyToRunSection, List<ReadyToRunMethod>>();
            _instanceMethods = new List<InstanceMethod>();

            if (ReadyToRunHeader.Sections.TryGetValue(ReadyToRunSectionType.RuntimeFunctions, out ReadyToRunSection runtimeFunctionSection))
            {
                int runtimeFunctionSize = CalculateRuntimeFunctionSize();
                uint nRuntimeFunctions = (uint)(runtimeFunctionSection.Size / runtimeFunctionSize);
                bool[] isEntryPoint = new bool[nRuntimeFunctions];

                // initialize R2RMethods
                ParseMethodDefEntrypoints((section, reader) => ParseMethodDefEntrypointsSection(section, reader, isEntryPoint));
                ParseInstanceMethodEntrypoints(isEntryPoint);
                CountRuntimeFunctions(isEntryPoint);
            }
        }

        private Dictionary<int, ReadyToRunMethod> _runtimeFunctionToMethod = null;

        private void EnsureEntrypointRuntimeFunctionToReadyToRunMethodDict()
        {
            EnsureMethods();

            if (_runtimeFunctionToMethod == null)
            {
                _runtimeFunctionToMethod = new Dictionary<int, ReadyToRunMethod>();
                foreach (var section in _methods)
                {
                    foreach (var method in section.Value)
                    {
                        if (!_runtimeFunctionToMethod.ContainsKey(method.EntryPointRuntimeFunctionId))
                            _runtimeFunctionToMethod.Add(method.EntryPointRuntimeFunctionId, method);
                    }
                }
            }
        }

        public IReadOnlyDictionary<TMethod, ReadyToRunMethod> GetCustomMethodToRuntimeFunctionMapping<TType, TMethod, TGenericContext>(IR2RSignatureTypeProvider<TType, TMethod, TGenericContext> provider)
        {
            EnsureEntrypointRuntimeFunctionToReadyToRunMethodDict();

            Dictionary<TMethod, ReadyToRunMethod> customMethods = new Dictionary<TMethod, ReadyToRunMethod>();
            if (ReadyToRunHeader.Sections.TryGetValue(ReadyToRunSectionType.RuntimeFunctions, out ReadyToRunSection runtimeFunctionSection))
            {
                ParseMethodDefEntrypoints((section, reader) => ParseMethodDefEntrypointsSectionCustom<TType, TMethod, TGenericContext>(provider, customMethods, section, reader));
                ParseInstanceMethodEntrypointsCustom<TType, TMethod, TGenericContext>(provider, customMethods);
            }

            return customMethods;
        }

        private bool TryLocateNativeReadyToRunHeader()
        {
            PEExportTable exportTable = PEReader.GetExportTable();
            if (exportTable.TryGetValue("RTR_HEADER", out _readyToRunHeaderRVA))
            {
                _composite = true;
                return true;
            }
            return false;
        }

        private MetadataReader GetSystemModuleMetadataReader()
        {
            if (_systemModuleReader == null)
            {
                if (_assemblyResolver != null)
                {
                    _systemModuleReader = _assemblyResolver.FindAssembly(SystemModuleName, Filename);
                }
            }
            return _systemModuleReader;
        }

        public MetadataReader GetGlobalMetadataReader()
        {
            EnsureHeader();
            return (_composite ? null : _assemblyCache[0]);
        }

        private unsafe void EnsureHeader()
        {
            if (_readyToRunHeader != null)
            {
                return;
            }
            uint machine = (uint)PEReader.PEHeaders.CoffHeader.Machine;
            _operatingSystem = OperatingSystem.Unknown;
            foreach (OperatingSystem os in Enum.GetValues(typeof(OperatingSystem)))
            {
                _machine = (Machine)(machine ^ (uint)os);
                if (Enum.IsDefined(typeof(Machine), _machine))
                {
                    _operatingSystem = os;
                    break;
                }
            }
            if (_operatingSystem == OperatingSystem.Unknown)
            {
                throw new BadImageFormatException($"Invalid Machine: {machine}");
            }

            switch (_machine)
            {
                case Machine.I386:
                    _architecture = Architecture.X86;
                    _pointerSize = 4;
                    break;

                case Machine.Amd64:
                    _architecture = Architecture.X64;
                    _pointerSize = 8;
                    break;

                case Machine.Arm:
                case Machine.Thumb:
                case Machine.ArmThumb2:
                    _architecture = Architecture.Arm;
                    _pointerSize = 4;
                    break;

                case Machine.Arm64:
                    _architecture = Architecture.Arm64;
                    _pointerSize = 8;
                    break;

                default:
                    throw new NotImplementedException(Machine.ToString());
            }


            _imageBase = PEReader.PEHeaders.PEHeader.ImageBase;

            // Initialize R2RHeader
            Debug.Assert(_readyToRunHeaderRVA != 0);
            int r2rHeaderOffset = GetOffset(_readyToRunHeaderRVA);
            _readyToRunHeader = new ReadyToRunHeader(Image, _readyToRunHeaderRVA, r2rHeaderOffset);

            if (_composite)
            {
                ParseComponentAssemblies();
            }
        }

        private void EnsureDebugInfo()
        {
            if (_runtimeFunctionIdToDebugOffset != null)
            {
                return;
            }
            _runtimeFunctionIdToDebugOffset = new Dictionary<int, int>();
            if (!ReadyToRunHeader.Sections.TryGetValue(ReadyToRunSectionType.DebugInfo, out ReadyToRunSection debugInfoSection))
            {
                return;
            }

            int debugInfoSectionOffset = GetOffset(debugInfoSection.RelativeVirtualAddress);

            NativeArray debugInfoArray = new NativeArray(Image, (uint)debugInfoSectionOffset);
            for (uint i = 0; i < debugInfoArray.GetCount(); ++i)
            {
                int offset = 0;
                if (!debugInfoArray.TryGetAt(Image, i, ref offset))
                {
                    continue;
                }

                _runtimeFunctionIdToDebugOffset.Add((int)i, offset);
            }
        }

        private unsafe void EnsureManifestReferences()
        {
            if (_manifestReferences != null)
            {
                return;
            }
            _manifestReferences = new List<AssemblyReferenceHandle>();
            if (ReadyToRunHeader.Sections.TryGetValue(ReadyToRunSectionType.ManifestMetadata, out ReadyToRunSection manifestMetadata))
            {
                fixed (byte* image = Image)
                {
                    _manifestReader = new MetadataReader(image + GetOffset(manifestMetadata.RelativeVirtualAddress), manifestMetadata.Size);
                    int assemblyRefCount = _manifestReader.GetTableRowCount(TableIndex.AssemblyRef);
                    for (int assemblyRefIndex = 1; assemblyRefIndex <= assemblyRefCount; assemblyRefIndex++)
                    {
                        AssemblyReferenceHandle asmRefHandle = MetadataTokens.AssemblyReferenceHandle(assemblyRefIndex);
                        _manifestReferences.Add(asmRefHandle);
                    }
                }
            }
        }

        private void EnsureManifestReferenceAssemblies()
        {
            if (_manifestReferenceAssemblies != null)
            {
                return;
            }
            EnsureManifestReferences();
            _manifestReferenceAssemblies = new Dictionary<string, int>(_manifestReferences.Count);
            for (int assemblyIndex = 0; assemblyIndex < _manifestReferences.Count; assemblyIndex++)
            {
                string assemblyName = ManifestReader.GetString(ManifestReader.GetAssemblyReference(_manifestReferences[assemblyIndex]).Name);
                _manifestReferenceAssemblies.Add(assemblyName, assemblyIndex);
            }
        }

        private unsafe void EnsureExceptionInfo()
        {
            if (_runtimeFunctionToEHInfo != null)
            {
                return;
            }
            _runtimeFunctionToEHInfo = new Dictionary<int, EHInfo>();
            if (ReadyToRunHeader.Sections.TryGetValue(ReadyToRunSectionType.ExceptionInfo, out ReadyToRunSection exceptionInfoSection))
            {
                int offset = GetOffset(exceptionInfoSection.RelativeVirtualAddress);
                int length = exceptionInfoSection.Size;
                int methodRva = BitConverter.ToInt32(Image, offset);
                int ehInfoRva = BitConverter.ToInt32(Image, offset + sizeof(uint));
                while ((length -= 2 * sizeof(uint)) >= 8)
                {
                    offset += 2 * sizeof(uint);
                    int nextMethodRva = BitConverter.ToInt32(Image, offset);
                    int nextEhInfoRva = BitConverter.ToInt32(Image, offset + sizeof(uint));
                    _runtimeFunctionToEHInfo.Add(methodRva, new EHInfo(this, ehInfoRva, methodRva, GetOffset(ehInfoRva), (nextEhInfoRva - ehInfoRva) / EHClause.Length));
                    methodRva = nextMethodRva;
                    ehInfoRva = nextEhInfoRva;
                }
            }
        }

        public bool InputArchitectureSupported()
        {
            return Machine != Machine.ArmThumb2; // CoreDisTools often fails to decode when disassembling ARM images (see https://github.com/dotnet/coreclr/issues/19637)
        }

        // TODO: Fix R2RDump issue where an R2R image cannot be dissassembled with the x86 CoreDisTools
        // For the short term, we want to error out with a decent message explaining the unexpected error
        // Issue https://github.com/dotnet/coreclr/issues/19564
        public bool DisassemblerArchitectureSupported()
        {
            System.Runtime.InteropServices.Architecture val = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture;
            return val != System.Runtime.InteropServices.Architecture.X86;
        }

        /// <summary>
        /// Each runtime function entry has 3 fields for Amd64 machines (StartAddress, EndAddress, UnwindRVA), otherwise 2 fields (StartAddress, UnwindRVA)
        /// </summary>
        internal int CalculateRuntimeFunctionSize()
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
        private void ParseMethodDefEntrypoints(Action<ReadyToRunSection, MetadataReader> methodDefSectionReader)
        {
            ReadyToRunSection methodEntryPointSection;
            if (ReadyToRunHeader.Sections.TryGetValue(ReadyToRunSectionType.MethodDefEntryPoints, out methodEntryPointSection))
            {
                methodDefSectionReader(methodEntryPointSection, GetGlobalMetadataReader());
            }
            else if (ReadyToRunAssemblyHeaders != null)
            {
                for (int assemblyIndex = 0; assemblyIndex < ReadyToRunAssemblyHeaders.Count; assemblyIndex++)
                {
                    if (ReadyToRunAssemblyHeaders[assemblyIndex].Sections.TryGetValue(ReadyToRunSectionType.MethodDefEntryPoints, out methodEntryPointSection))
                    {
                        methodDefSectionReader(methodEntryPointSection, OpenReferenceAssembly(assemblyIndex + 1));
                    }
                }
            }
        }

        /// <summary>
        /// Parse a single method def entrypoint section. For composite R2R images, this method is called multiple times
        /// are method entrypoints are stored separately for each component assembly of the composite R2R executable.
        /// </summary>
        /// <param name="section">Method entrypoint section to parse</param>
        /// <param name="metadataReader">ECMA metadata reader representing this method entrypoint section</param>
        /// <param name="isEntryPoint">Set to true for each runtime function index representing a method entrypoint</param>
        private void ParseMethodDefEntrypointsSection(ReadyToRunSection section, MetadataReader metadataReader, bool[] isEntryPoint)
        {
            int methodDefEntryPointsOffset = GetOffset(section.RelativeVirtualAddress);
            NativeArray methodEntryPoints = new NativeArray(Image, (uint)methodDefEntryPointsOffset);
            uint nMethodEntryPoints = methodEntryPoints.GetCount();

            for (uint rid = 1; rid <= nMethodEntryPoints; rid++)
            {
                int offset = 0;
                if (methodEntryPoints.TryGetAt(Image, rid - 1, ref offset))
                {
                    EntityHandle methodHandle = MetadataTokens.MethodDefinitionHandle((int)rid);
                    int runtimeFunctionId;
                    int? fixupOffset;
                    GetRuntimeFunctionIndexFromOffset(offset, out runtimeFunctionId, out fixupOffset);
                    ReadyToRunMethod method = new ReadyToRunMethod(this, this.PEReader, metadataReader, methodHandle, runtimeFunctionId, owningType: null, constrainedType: null, instanceArgs: null, fixupOffset: fixupOffset);

                    if (method.EntryPointRuntimeFunctionId < 0 || method.EntryPointRuntimeFunctionId >= isEntryPoint.Length)
                    {
                        throw new BadImageFormatException("EntryPointRuntimeFunctionId out of bounds");
                    }
                    isEntryPoint[method.EntryPointRuntimeFunctionId] = true;
                    if (!_methods.TryGetValue(section, out List<ReadyToRunMethod> sectionMethods))
                    {
                        sectionMethods = new List<ReadyToRunMethod>();
                        _methods.Add(section, sectionMethods);
                    }
                    sectionMethods.Add(method);
                }
            }
        }

        /// <summary>
        /// Parse a single method def entrypoint section. For composite R2R images, this method is called multiple times
        /// are method entrypoints are stored separately for each component assembly of the composite R2R executable.
        /// </summary>
        /// <param name="section">Method entrypoint section to parse</param>
        /// <param name="metadataReader">ECMA metadata reader representing this method entrypoint section</param>
        /// <param name="isEntryPoint">Set to true for each runtime function index representing a method entrypoint</param>
        private void ParseMethodDefEntrypointsSectionCustom<TType, TMethod, TGenericContext>(IR2RSignatureTypeProvider<TType, TMethod, TGenericContext> provider, Dictionary<TMethod, ReadyToRunMethod> foundMethods, ReadyToRunSection section, MetadataReader metadataReader)
        {
            int methodDefEntryPointsOffset = GetOffset(section.RelativeVirtualAddress);
            NativeArray methodEntryPoints = new NativeArray(Image, (uint)methodDefEntryPointsOffset);
            uint nMethodEntryPoints = methodEntryPoints.GetCount();

            for (uint rid = 1; rid <= nMethodEntryPoints; rid++)
            {
                int offset = 0;
                if (methodEntryPoints.TryGetAt(Image, rid - 1, ref offset))
                {
                    EntityHandle methodHandle = MetadataTokens.MethodDefinitionHandle((int)rid);
                    int runtimeFunctionId;
                    int? fixupOffset;
                    GetRuntimeFunctionIndexFromOffset(offset, out runtimeFunctionId, out fixupOffset);
                    ReadyToRunMethod r2rMethod = _runtimeFunctionToMethod[runtimeFunctionId];
                    var customMethod = provider.GetMethodFromMethodDef(metadataReader, MetadataTokens.MethodDefinitionHandle((int)rid), default(TType));
                    
                    if (!Object.ReferenceEquals(customMethod, null) && !foundMethods.ContainsKey(customMethod))
                        foundMethods.Add(customMethod, r2rMethod);
                }
            }
        }

        /// <summary>
        /// Initialize generic method instances with argument types and runtime function indices from InstanceMethodEntrypoints
        /// </summary>
        private void ParseInstanceMethodEntrypointsCustom<TType, TMethod, TGenericContext>(IR2RSignatureTypeProvider<TType, TMethod, TGenericContext> provider, Dictionary<TMethod, ReadyToRunMethod> foundMethods)
        {
            if (!ReadyToRunHeader.Sections.TryGetValue(ReadyToRunSectionType.InstanceMethodEntryPoints, out ReadyToRunSection instMethodEntryPointSection))
            {
                return;
            }
            int instMethodEntryPointsOffset = GetOffset(instMethodEntryPointSection.RelativeVirtualAddress);
            NativeParser parser = new NativeParser(Image, (uint)instMethodEntryPointsOffset);
            NativeHashtable instMethodEntryPoints = new NativeHashtable(Image, parser, (uint)(instMethodEntryPointsOffset + instMethodEntryPointSection.Size));
            NativeHashtable.AllEntriesEnumerator allEntriesEnum = instMethodEntryPoints.EnumerateAllEntries();
            NativeParser curParser = allEntriesEnum.GetNext();
            while (!curParser.IsNull())
            {
                MetadataReader mdReader = _composite ? null : _assemblyCache[0];
                var decoder = new R2RSignatureDecoder<TType, TMethod, TGenericContext>(provider, default(TGenericContext), mdReader, this, (int)curParser.Offset);

                TMethod customMethod = decoder.ParseMethod();

                int runtimeFunctionId;
                int? fixupOffset;
                GetRuntimeFunctionIndexFromOffset((int)decoder.Offset, out runtimeFunctionId, out fixupOffset);
                ReadyToRunMethod r2rMethod = _runtimeFunctionToMethod[runtimeFunctionId];
                if (!Object.ReferenceEquals(customMethod, null) && !foundMethods.ContainsKey(customMethod))
                    foundMethods.Add(customMethod, r2rMethod);
                foundMethods.Add(customMethod, r2rMethod);
                curParser = allEntriesEnum.GetNext();
            }
        }

        /// <summary>
        /// Initialize generic method instances with argument types and runtime function indices from InstanceMethodEntrypoints
        /// </summary>
        private void ParseInstanceMethodEntrypoints(bool[] isEntryPoint)
        {
            if (!ReadyToRunHeader.Sections.TryGetValue(ReadyToRunSectionType.InstanceMethodEntryPoints, out ReadyToRunSection instMethodEntryPointSection))
            {
                return;
            }
            int instMethodEntryPointsOffset = GetOffset(instMethodEntryPointSection.RelativeVirtualAddress);
            NativeParser parser = new NativeParser(Image, (uint)instMethodEntryPointsOffset);
            NativeHashtable instMethodEntryPoints = new NativeHashtable(Image, parser, (uint)(instMethodEntryPointsOffset + instMethodEntryPointSection.Size));
            NativeHashtable.AllEntriesEnumerator allEntriesEnum = instMethodEntryPoints.EnumerateAllEntries();
            NativeParser curParser = allEntriesEnum.GetNext();
            while (!curParser.IsNull())
            {
                MetadataReader mdReader = _composite ? null : _assemblyCache[0];
                SignatureDecoder decoder = new SignatureDecoder(_assemblyResolver, mdReader, this, (int)curParser.Offset);

                string owningType = null;

                uint methodFlags = decoder.ReadUInt();
                if ((methodFlags & (uint)ReadyToRunMethodSigFlags.READYTORUN_METHOD_SIG_OwnerType) != 0)
                {
                    mdReader = decoder.GetMetadataReaderFromModuleOverride() ?? mdReader;
                    if ((_composite) && mdReader == null)
                    {
                        // The only types that don't have module overrides on them in composite images are primitive types within the system module
                        mdReader = GetSystemModuleMetadataReader();
                    }
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
                int? fixupOffset;
                GetRuntimeFunctionIndexFromOffset((int)decoder.Offset, out runtimeFunctionId, out fixupOffset);
                ReadyToRunMethod method = new ReadyToRunMethod(
                    this,
                    this.PEReader,
                    mdReader,
                    methodHandle,
                    runtimeFunctionId,
                    owningType,
                    constrainedType,
                    methodTypeArgs,
                    fixupOffset);
                if (method.EntryPointRuntimeFunctionId >= 0 && method.EntryPointRuntimeFunctionId < isEntryPoint.Length)
                {
                    isEntryPoint[method.EntryPointRuntimeFunctionId] = true;
                }
                if (!Methods.TryGetValue(instMethodEntryPointSection, out List<ReadyToRunMethod> sectionMethods))
                {
                    sectionMethods = new List<ReadyToRunMethod>();
                    Methods.Add(instMethodEntryPointSection, sectionMethods);
                }
                sectionMethods.Add(method);
                _instanceMethods.Add(new InstanceMethod(curParser.LowHashcode, method));
                curParser = allEntriesEnum.GetNext();
            }
        }

        private void CountRuntimeFunctions(bool[] isEntryPoint)
        {
            foreach (ReadyToRunMethod method in Methods.Values.SelectMany(sectionMethods => sectionMethods))
            {
                int runtimeFunctionId = method.EntryPointRuntimeFunctionId;
                if (runtimeFunctionId == -1)
                    continue;

                int count = 0;
                int i = runtimeFunctionId;
                do
                {
                    count++;
                    i++;
                } while (i < isEntryPoint.Length && !isEntryPoint[i]);
                method.RuntimeFunctionCount = count;
            }
        }

        /// <summary>
        /// Iterates through a native hashtable to get all RIDs
        /// </summary>
        private void EnsureAvailableTypes()
        {
            if (_availableTypes != null)
            {
                return;
            }
            _availableTypes = new Dictionary<ReadyToRunSection, List<string>>();
            ReadyToRunSection availableTypesSection;
            if (ReadyToRunHeader.Sections.TryGetValue(ReadyToRunSectionType.AvailableTypes, out availableTypesSection))
            {
                ParseAvailableTypesSection(availableTypesSection, GetGlobalMetadataReader());
            }
            else if (_readyToRunAssemblyHeaders != null)
            {
                for (int assemblyIndex = 0; assemblyIndex < _readyToRunAssemblyHeaders.Count; assemblyIndex++)
                {
                    if (_readyToRunAssemblyHeaders[assemblyIndex].Sections.TryGetValue(
                        ReadyToRunSectionType.AvailableTypes, out availableTypesSection))
                    {
                        ParseAvailableTypesSection(availableTypesSection, OpenReferenceAssembly(assemblyIndex + 1));
                    }
                }
            }
        }

        /// <summary>
        /// Parse a single available types section. For composite R2R images this method is called multiple times
        /// as available types are stored separately for each component assembly of the composite R2R executable.
        /// </summary>
        /// <param name="availableTypesSection"></param>
        private void ParseAvailableTypesSection(ReadyToRunSection availableTypesSection, MetadataReader metadataReader)
        {
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
                    string exportedTypeName = GetExportedTypeFullName(metadataReader, exportedTypeHandle);
                    if (!AvailableTypes.TryGetValue(availableTypesSection, out List<string> sectionTypes))
                    {
                        sectionTypes = new List<string>();
                        AvailableTypes.Add(availableTypesSection, sectionTypes);
                    }
                    sectionTypes.Add("exported " + exportedTypeName);
                }
                else
                {
                    TypeDefinitionHandle typeDefHandle = MetadataTokens.TypeDefinitionHandle((int)rid);
                    string typeDefName = MetadataNameFormatter.FormatHandle(metadataReader, typeDefHandle);
                    if (!AvailableTypes.TryGetValue(availableTypesSection, out List<string> sectionTypes))
                    {
                        sectionTypes = new List<string>();
                        AvailableTypes.Add(availableTypesSection, sectionTypes);
                    }
                    sectionTypes.Add(typeDefName);
                }

                curParser = allEntriesEnum.GetNext();
            }
        }

        /// <summary>
        /// Converts the bytes in the compiler identifier section to characters in a string
        /// </summary>
        private void EnsureCompilerIdentifier()
        {
            if (_compilerIdentifier != null)
            {
                return;
            }
            if (!ReadyToRunHeader.Sections.TryGetValue(ReadyToRunSectionType.CompilerIdentifier, out ReadyToRunSection compilerIdentifierSection))
            {
                return;
            }
            byte[] identifier = new byte[compilerIdentifierSection.Size - 1];
            int identifierOffset = GetOffset(compilerIdentifierSection.RelativeVirtualAddress);
            Array.Copy(Image, identifierOffset, identifier, 0, compilerIdentifierSection.Size - 1);
            _compilerIdentifier = Encoding.UTF8.GetString(identifier);
        }

        /// <summary>
        /// Decode the ReadyToRun section READYTORUN_SECTION_ASSEMBLIES containing a list of per assembly R2R core headers
        /// for each assembly comprising the composite R2R executable.
        /// </summary>
        private void ParseComponentAssemblies()
        {
            ReadyToRunSection componentAssembliesSection;
            if (!ReadyToRunHeader.Sections.TryGetValue(ReadyToRunSectionType.ComponentAssemblies, out componentAssembliesSection))
            {
                return;
            }

            _readyToRunAssemblyHeaders = new List<ReadyToRunCoreHeader>();

            int offset = GetOffset(componentAssembliesSection.RelativeVirtualAddress);
            int numberOfAssemblyHeaderRVAs = componentAssembliesSection.Size / ComponentAssembly.Size;

            for (int assemblyIndex = 0; assemblyIndex < numberOfAssemblyHeaderRVAs; assemblyIndex++)
            {
                ComponentAssembly assembly = new ComponentAssembly(Image, ref offset);
                int headerOffset = GetOffset(assembly.AssemblyHeaderRVA);

                ReadyToRunCoreHeader assemblyHeader = new ReadyToRunCoreHeader(Image, ref headerOffset);
                _readyToRunAssemblyHeaders.Add(assemblyHeader);
            }
        }

        /// <summary>
        /// based on <a href="https://github.com/dotnet/coreclr/blob/master/src/zap/zapimport.cpp">ZapImportSectionsTable::Save</a>
        /// </summary>
        private void EnsureImportSections()
        {
            if (_importSections != null)
            {
                return;
            }
            _importSections = new List<ReadyToRunImportSection>();
            _importCellNames = new Dictionary<int, string>();
            _importSignatures = new Dictionary<int, ReadyToRunSignature>();
            if (!ReadyToRunHeader.Sections.TryGetValue(ReadyToRunSectionType.ImportSections, out ReadyToRunSection importSectionsSection))
            {
                return;
            }
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
                List<ReadyToRunImportSection.ImportSectionEntry> entries = new List<ReadyToRunImportSection.ImportSectionEntry>();
                for (int i = 0; i < entryCount; i++)
                {
                    int entryOffset = sectionOffset - startOffset;
                    long section = NativeReader.ReadInt64(Image, ref sectionOffset);
                    uint sigRva = NativeReader.ReadUInt32(Image, ref signatureOffset);
                    int sigOffset = GetOffset((int)sigRva);
                    ReadyToRunSignature signature;
                    string cellName = MetadataNameFormatter.FormatSignature(_assemblyResolver, this, sigOffset, out signature);
                    entries.Add(new ReadyToRunImportSection.ImportSectionEntry(entries.Count, entryOffset, entryOffset + rva, section, sigRva, cellName));
                    _importCellNames.Add(rva + entrySize * i, cellName);
                    _importSignatures.Add(rva + entrySize * i, signature);
                }

                int auxDataRVA = NativeReader.ReadInt32(Image, ref offset);
                int auxDataOffset = 0;
                if (auxDataRVA != 0)
                {
                    auxDataOffset = GetOffset(auxDataRVA);
                }
                _importSections.Add(new ReadyToRunImportSection(_importSections.Count, this, rva, size, flags, type, entrySize, signatureRVA, entries, auxDataRVA, auxDataOffset, Machine, ReadyToRunHeader.MajorVersion));
            }
        }

        /// <summary>
        /// Get the index in the image byte array corresponding to the RVA
        /// </summary>
        /// <param name="rva">The relative virtual address</param>
        public int GetOffset(int rva)
        {
            return PEReader.GetOffset(rva);
        }

        /// <summary>
        /// Get the full name of an ExportedType, including namespace
        /// </summary>
        private static string GetExportedTypeFullName(MetadataReader mdReader, ExportedTypeHandle handle)
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
        private void GetRuntimeFunctionIndexFromOffset(int offset, out int runtimeFunctionIndex, out int? fixupOffset)
        {
            fixupOffset = null;

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

                fixupOffset = offset;

                id >>= 2;
            }
            else
            {
                id >>= 1;
            }

            runtimeFunctionIndex = (int)id;
        }

        private AssemblyReferenceHandle GetAssemblyAtIndex(int refAsmIndex, out MetadataReader metadataReader)
        {
            Debug.Assert(refAsmIndex != 0);

            int assemblyRefCount = (_composite ? 0 : _assemblyCache[0].GetTableRowCount(TableIndex.AssemblyRef) + 1);
            AssemblyReferenceHandle assemblyReferenceHandle;
            if (refAsmIndex < assemblyRefCount)
            {
                metadataReader = _assemblyCache[0];
                assemblyReferenceHandle = MetadataTokens.AssemblyReferenceHandle(refAsmIndex);
            }
            else
            {
                metadataReader = ManifestReader;
                assemblyReferenceHandle = ManifestReferences[refAsmIndex - assemblyRefCount - 1];
            }

            return assemblyReferenceHandle;
        }

        internal string GetReferenceAssemblyName(int refAsmIndex)
        {
            AssemblyReferenceHandle handle = GetAssemblyAtIndex(refAsmIndex, out MetadataReader reader);
            return reader.GetString(reader.GetAssemblyReference(handle).Name);
        }

        /// <summary>
        /// Open a given reference assembly (relative to this ECMA metadata file).
        /// </summary>
        /// <param name="refAsmIndex">Reference assembly index</param>
        /// <returns>MetadataReader instance representing the reference assembly</returns>
        internal MetadataReader OpenReferenceAssembly(int refAsmIndex)
        {
            MetadataReader result = (refAsmIndex < _assemblyCache.Count ? _assemblyCache[refAsmIndex] : null);
            if (result == null)
            {
                AssemblyReferenceHandle assemblyReferenceHandle = GetAssemblyAtIndex(refAsmIndex, out MetadataReader metadataReader);

                result = _assemblyResolver.FindAssembly(metadataReader, assemblyReferenceHandle, Filename);
                if (result == null)
                {
                    string name = metadataReader.GetString(metadataReader.GetAssemblyReference(assemblyReferenceHandle).Name);
                    throw new Exception($"Missing reference assembly: {name}");
                }
                while (_assemblyCache.Count <= refAsmIndex)
                {
                    _assemblyCache.Add(null);
                }
                _assemblyCache[refAsmIndex] = result;
            }
            return result;
        }
    }
}
