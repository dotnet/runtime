// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

using Internal.ReadyToRunConstants;
using Internal.Runtime;

using Debug = System.Diagnostics.Debug;

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

    public class ReadyToRunAssembly
    {
        private ReadyToRunReader _reader;
        internal List<string> _availableTypes;
        internal List<ReadyToRunMethod> _methods;

        internal ReadyToRunAssembly(ReadyToRunReader reader)
        {
            _reader = reader;
        }

        public IReadOnlyList<string> AvailableTypes
        {
            get
            {
                _reader.EnsureAvailableTypes();
                return _availableTypes;
            }
        }

        public IReadOnlyList<ReadyToRunMethod> Methods
        {
            get
            {
                _reader.EnsureMethods();
                return _methods;
            }
        }
    }

    public sealed class ReadyToRunReader
    {
        public const int GuidByteSize = 16;

        private const string SystemModuleName = "System.Private.CoreLib";

        /// <summary>
        /// MetadataReader for the system module (normally System.Private.CoreLib)
        /// </summary>
        private IAssemblyMetadata _systemModuleReader;

        private readonly IAssemblyResolver _assemblyResolver;

        /// <summary>
        /// Reference assembly cache indexed by module indices as used in signatures
        /// </summary>
        private List<IAssemblyMetadata> _assemblyCache;

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
        private List<ReadyToRunAssembly> _readyToRunAssemblies;

        // DebugInfo
        private Dictionary<int, int> _runtimeFunctionIdToDebugOffset;

        // ManifestReferences
        private MetadataReader _manifestReader;
        private List<AssemblyReferenceHandle> _manifestReferences;
        private Dictionary<string, int> _manifestReferenceAssemblies;
        private IAssemblyMetadata _manifestAssemblyMetadata;

        // ExceptionInfo
        private Dictionary<int, EHInfo> _runtimeFunctionToEHInfo;

        // Methods
        private List<InstanceMethod> _instanceMethods;

        // PgoData
        private Dictionary<PgoInfoKey, PgoInfo> _pgoInfos;

        // ImportSections
        private List<ReadyToRunImportSection> _importSections;
        private Dictionary<int, ReadyToRunSignature> _importSignatures;

        // CompilerIdentifier
        private string _compilerIdentifier;

        /// <summary>
        /// Underlying PE image reader is used to access raw PE structures like header
        /// or section list.
        /// </summary>
        public PEReader CompositeReader { get; private set; }

        /// <summary>
        /// Byte array containing the ReadyToRun image
        /// </summary>
        public byte[] Image { get; private set; }
        private PinningReference ImagePin;

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

        public IReadOnlyList<ReadyToRunAssembly> ReadyToRunAssemblies
        {
            get
            {
                EnsureHeader();
                return _readyToRunAssemblies;
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

        internal IAssemblyMetadata R2RManifestMetadata
        {
            get
            {
                EnsureManifestReferences();
                return _manifestAssemblyMetadata;
            }
        }

        /// <summary>
        /// Initializes the fields of the R2RHeader and R2RMethods
        /// </summary>
        /// <param name="filename">PE image</param>
        /// <exception cref="BadImageFormatException">The Cor header flag must be ILLibrary</exception>
        public ReadyToRunReader(IAssemblyResolver assemblyResolver, IAssemblyMetadata metadata, PEReader peReader, string filename)
        {
            _assemblyResolver = assemblyResolver;
            CompositeReader = peReader;
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

        public static bool IsReadyToRunImage(PEReader peReader)
        {
            if (peReader.PEHeaders == null)
                return false;

            if (peReader.PEHeaders.CorHeader == null)
                return false;

            if ((peReader.PEHeaders.CorHeader.Flags & CorFlags.ILLibrary) == 0)
            {
                return peReader.TryGetReadyToRunHeader(out _);
            }
            else
            {
                return peReader.PEHeaders.CorHeader.ManagedNativeHeaderDirectory.Size != 0;
            }
        }

        class PinningReference
        {
            GCHandle _pinnedObject;
            public PinningReference(object o)
            {
                _pinnedObject = GCHandle.Alloc(o, GCHandleType.Pinned);
            }

            ~PinningReference()
            {
                if (_pinnedObject.IsAllocated)
                    _pinnedObject.Free();
            }
        }
        private unsafe void Initialize(IAssemblyMetadata metadata)
        {
            _assemblyCache = new List<IAssemblyMetadata>();

            if (CompositeReader == null)
            {
                byte[] image = File.ReadAllBytes(Filename);
                Image = image;
                ImagePin = new PinningReference(image);

                CompositeReader = new PEReader(Unsafe.As<byte[], ImmutableArray<byte>>(ref image));
            }
            else
            {
                ImmutableArray<byte> content = CompositeReader.GetEntireImage().GetContent();
                Image = Unsafe.As<ImmutableArray<byte>, byte[]>(ref content);
                ImagePin = new PinningReference(Image);
            }

            if (metadata == null && CompositeReader.HasMetadata)
            {
                metadata = new StandaloneAssemblyMetadata(CompositeReader);
            }

            if (metadata != null)
            {
                if ((CompositeReader.PEHeaders.CorHeader.Flags & CorFlags.ILLibrary) == 0)
                {
                    if (!TryLocateNativeReadyToRunHeader())
                        throw new BadImageFormatException("The file is not a ReadyToRun image");

                    Debug.Assert(Composite);
                }
                else
                {
                    _assemblyCache.Add(metadata);

                    DirectoryEntry r2rHeaderDirectory = CompositeReader.PEHeaders.CorHeader.ManagedNativeHeaderDirectory;
                    _readyToRunHeaderRVA = r2rHeaderDirectory.RelativeVirtualAddress;
                    Debug.Assert(!Composite);
                }

            }
            else if (!TryLocateNativeReadyToRunHeader())
            {
                throw new BadImageFormatException($"ECMA metadata / RTR_HEADER not found in file '{Filename}'");
            }
        }

        internal void EnsureMethods()
        {
            EnsureHeader();
            if (_instanceMethods != null)
            {
                return;
            }

            if (ReadyToRunHeader.Sections.TryGetValue(ReadyToRunSectionType.PgoInstrumentationData, out _))
            {
                ParsePgoMethods();
            }

            _instanceMethods = new List<InstanceMethod>();
            foreach (ReadyToRunAssembly assembly in _readyToRunAssemblies)
            {
                assembly._methods = new List<ReadyToRunMethod>();
            }

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
                foreach (var method in Methods)
                {
                    if (!_runtimeFunctionToMethod.ContainsKey(method.EntryPointRuntimeFunctionId))
                        _runtimeFunctionToMethod.Add(method.EntryPointRuntimeFunctionId, method);
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
            _composite = CompositeReader.TryGetReadyToRunHeader(out _readyToRunHeaderRVA);

            return _composite;
        }

        private IAssemblyMetadata GetSystemModuleMetadataReader()
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

        public IAssemblyMetadata GetGlobalMetadata()
        {
            EnsureHeader();
            return (_composite ? null : _assemblyCache[0]);
        }

        public string GetGlobalAssemblyName()
        {
            MetadataReader mdReader = GetGlobalMetadata().MetadataReader;
            return mdReader.GetString(mdReader.GetAssemblyDefinition().Name);
        }

        private unsafe void EnsureHeader()
        {
            if (_readyToRunHeader != null)
            {
                return;
            }
            uint machine = (uint)CompositeReader.PEHeaders.CoffHeader.Machine;
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

                case (Machine) 0x6264: /* LoongArch64 */
                    _architecture = (Architecture) 6; /* LoongArch64 */
                    _pointerSize = 8;
                    break;

                default:
                    throw new NotImplementedException(Machine.ToString());
            }


            _imageBase = CompositeReader.PEHeaders.PEHeader.ImageBase;

            // Initialize R2RHeader
            Debug.Assert(_readyToRunHeaderRVA != 0);
            int r2rHeaderOffset = GetOffset(_readyToRunHeaderRVA);
            _readyToRunHeader = new ReadyToRunHeader(Image, _readyToRunHeaderRVA, r2rHeaderOffset);

            _readyToRunAssemblies = new List<ReadyToRunAssembly>();
            if (_composite)
            {
                ParseComponentAssemblies();
            }
            else
            {
                _readyToRunAssemblies.Add(new ReadyToRunAssembly(this));
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
                    _manifestAssemblyMetadata = new ManifestAssemblyMetadata(CompositeReader, _manifestReader);
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
        private void ParseMethodDefEntrypoints(Action<ReadyToRunSection, IAssemblyMetadata> methodDefSectionReader)
        {
            ReadyToRunSection methodEntryPointSection;
            if (ReadyToRunHeader.Sections.TryGetValue(ReadyToRunSectionType.MethodDefEntryPoints, out methodEntryPointSection))
            {
                methodDefSectionReader(methodEntryPointSection, GetGlobalMetadata());
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
        /// <param name="componentReader">Assembly metadata reader representing this method entrypoint section</param>
        /// <param name="isEntryPoint">Set to true for each runtime function index representing a method entrypoint</param>
        private void ParseMethodDefEntrypointsSection(ReadyToRunSection section, IAssemblyMetadata componentReader, bool[] isEntryPoint)
        {
            int assemblyIndex = GetAssemblyIndex(section);
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
                    ReadyToRunMethod method = new ReadyToRunMethod(this, componentReader, methodHandle, runtimeFunctionId, owningType: null, constrainedType: null, instanceArgs: null, fixupOffset: fixupOffset);

                    if (method.EntryPointRuntimeFunctionId < 0 || method.EntryPointRuntimeFunctionId >= isEntryPoint.Length)
                    {
                        throw new BadImageFormatException("EntryPointRuntimeFunctionId out of bounds");
                    }
                    isEntryPoint[method.EntryPointRuntimeFunctionId] = true;
                    _readyToRunAssemblies[assemblyIndex]._methods.Add(method);
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
        private void ParseMethodDefEntrypointsSectionCustom<TType, TMethod, TGenericContext>(IR2RSignatureTypeProvider<TType, TMethod, TGenericContext> provider, Dictionary<TMethod, ReadyToRunMethod> foundMethods, ReadyToRunSection section, IAssemblyMetadata metadataReader)
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
                    var customMethod = provider.GetMethodFromMethodDef(metadataReader.MetadataReader, MetadataTokens.MethodDefinitionHandle((int)rid), default(TType));

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
                IAssemblyMetadata mdReader = GetGlobalMetadata();
                var decoder = new R2RSignatureDecoder<TType, TMethod, TGenericContext>(provider, default(TGenericContext), mdReader?.MetadataReader, this, (int)curParser.Offset);

                TMethod customMethod = decoder.ParseMethod();

                int runtimeFunctionId;
                int? fixupOffset;
                GetRuntimeFunctionIndexFromOffset((int)decoder.Offset, out runtimeFunctionId, out fixupOffset);
                ReadyToRunMethod r2rMethod = _runtimeFunctionToMethod[runtimeFunctionId];
                if (!Object.ReferenceEquals(customMethod, null) && !foundMethods.ContainsKey(customMethod))
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
                IAssemblyMetadata mdReader = GetGlobalMetadata();
                bool updateMDReaderFromOwnerType = true;
                SignatureFormattingOptions dummyOptions = new SignatureFormattingOptions();
                SignatureDecoder decoder = new SignatureDecoder(_assemblyResolver, dummyOptions, mdReader?.MetadataReader, this, (int)curParser.Offset);

                string owningType = null;

                uint methodFlags = decoder.ReadUInt();

                if ((methodFlags & (uint)ReadyToRunMethodSigFlags.READYTORUN_METHOD_SIG_UpdateContext) != 0)
                {
                    int moduleIndex = (int)decoder.ReadUInt();
                    mdReader = OpenReferenceAssembly(moduleIndex);

                    decoder = new SignatureDecoder(_assemblyResolver, dummyOptions, mdReader.MetadataReader, this, (int)curParser.Offset);

                    decoder.ReadUInt(); // Skip past methodFlags
                    decoder.ReadUInt(); // And moduleIndex
                    updateMDReaderFromOwnerType = false;
                }

                if ((methodFlags & (uint)ReadyToRunMethodSigFlags.READYTORUN_METHOD_SIG_OwnerType) != 0)
                {
                    if (updateMDReaderFromOwnerType)
                    {
                        mdReader = decoder.GetMetadataReaderFromModuleOverride() ?? mdReader;
                        if ((_composite) && mdReader == null)
                        {
                            // The only types that don't have module overrides on them in composite images are primitive types within the system module
                            mdReader = GetSystemModuleMetadataReader();
                        }
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
                _instanceMethods.Add(new InstanceMethod(curParser.LowHashcode, method));
                curParser = allEntriesEnum.GetNext();
            }
        }

        public IEnumerable<ReadyToRunMethod> Methods
        {
            get
            {
                EnsureMethods();
                return _readyToRunAssemblies.SelectMany(assembly => assembly.Methods).Concat(_instanceMethods.Select(im => im.Method));
            }
        }

        public IEnumerable<PgoInfo> AllPgoInfos
        {
            get
            {
                EnsureMethods();

                if (_pgoInfos != null)
                    return _pgoInfos.Values;
                else
                    return Array.Empty<PgoInfo>();
            }
        }

        public PgoInfo GetPgoInfoByKey(PgoInfoKey key)
        {
            EnsureMethods();
            if (_pgoInfos != null)
            {
                _pgoInfos.TryGetValue(key, out var returnValue);
                return returnValue;
            }

            return null;
        }

        /// <summary>
        /// Initialize generic method instances with argument types and runtime function indices from InstanceMethodEntrypoints
        /// </summary>
        private void ParsePgoMethods()
        {
            _pgoInfos = new Dictionary<PgoInfoKey, PgoInfo>();
            if (!ReadyToRunHeader.Sections.TryGetValue(ReadyToRunSectionType.PgoInstrumentationData, out ReadyToRunSection pgoInstrumentationDataSection))
            {
                return;
            }
            int pgoInstrumentationDataOffset = GetOffset(pgoInstrumentationDataSection.RelativeVirtualAddress);
            NativeParser parser = new NativeParser(Image, (uint)pgoInstrumentationDataOffset);
            NativeHashtable pgoInstrumentationData = new NativeHashtable(Image, parser, (uint)(pgoInstrumentationDataOffset + pgoInstrumentationDataSection.Size));
            NativeHashtable.AllEntriesEnumerator allEntriesEnum = pgoInstrumentationData.EnumerateAllEntries();
            NativeParser curParser = allEntriesEnum.GetNext();
            while (!curParser.IsNull())
            {
                IAssemblyMetadata mdReader = GetGlobalMetadata();
                SignatureFormattingOptions dummyOptions = new SignatureFormattingOptions();
                SignatureDecoder decoder = new SignatureDecoder(_assemblyResolver, dummyOptions, mdReader?.MetadataReader, this, (int)curParser.Offset);

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

                GetPgoOffsetAndVersion(decoder.Offset, out int pgoFormatVersion, out int pgoOffset);

                PgoInfoKey key = new PgoInfoKey(mdReader, owningType, methodHandle, methodTypeArgs);
                PgoInfo info = new PgoInfo(key, this, pgoFormatVersion, Image, pgoOffset);

                // Since we do non-assembly qualified name based matching for generic instantiations, we can have conflicts.
                // This is rare, so the current strategy is to just ignore them. This will allow the tooling to work, although
                // the text output for PGO will be slightly wrong.
                if (!_pgoInfos.ContainsKey(key))
                    _pgoInfos.Add(key, info);
                curParser = allEntriesEnum.GetNext();
            }

            return;

            // Broken out into helper function in case we ever implement pgo data that isn't tied to a signature
            void GetPgoOffsetAndVersion(int offset, out int version, out int pgoDataOffset)
            {
                version = 0;
                // get the id of the entry point runtime function from the MethodEntryPoints NativeArray
                uint versionAndFlags = 0; // the RUNTIME_FUNCTIONS index
                offset = (int)NativeReader.DecodeUnsigned(Image, (uint)offset, ref versionAndFlags);

                switch (versionAndFlags & 3)
                {
                    case 3:
                        uint val = 0;
                        NativeReader.DecodeUnsigned(Image, (uint)offset, ref val);
                        offset -= (int)val;
                        break;
                    case 1:
                        // Offset already correct
                        break;
                    default:
                        throw new Exception("Invalid Pgo format");
                }

                version = (int)(versionAndFlags >> 2);
                pgoDataOffset = offset;
            }
        }

        private void CountRuntimeFunctions(bool[] isEntryPoint)
        {
            foreach (ReadyToRunMethod method in Methods)
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

        public int GetAssemblyIndex(ReadyToRunSection section)
        {
            EnsureHeader();
            if (_composite)
            {
                for (int assemblyIndex = 0; assemblyIndex < _readyToRunAssemblyHeaders.Count; assemblyIndex++)
                {
                    ReadyToRunSection toMatch;
                    if (_readyToRunAssemblyHeaders[assemblyIndex].Sections.TryGetValue(section.Type, out toMatch) && section.RelativeVirtualAddress == toMatch.RelativeVirtualAddress)
                    {
                        return assemblyIndex;
                    }
                }
                return -1;
            }
            else
            {
                return 0;
            }
        }

        public Guid GetAssemblyMvid(int assemblyIndex)
        {
            EnsureHeader();
            if (_composite)
            {
                if (!ReadyToRunHeader.Sections.TryGetValue(ReadyToRunSectionType.ManifestAssemblyMvids, out ReadyToRunSection mvidSection))
                {
                    return Guid.Empty;
                }
                int mvidOffset = GetOffset(mvidSection.RelativeVirtualAddress) + GuidByteSize * assemblyIndex;
                byte[] mvidBytes = new byte[ReadyToRunReader.GuidByteSize];
                for (int i = 0; i < mvidBytes.Length; i++)
                {
                    mvidBytes[i] = Image[mvidOffset + i];
                }
                return new Guid(mvidBytes);
            }
            else
            {
                Debug.Assert(assemblyIndex == 0);
                MetadataReader mdReader = GetGlobalMetadata().MetadataReader;
                return mdReader.GetGuid(mdReader.GetModuleDefinition().Mvid);
            }
        }

        /// <summary>
        /// Iterates through a native hashtable to get all RIDs
        /// </summary>
        internal void EnsureAvailableTypes()
        {
            EnsureHeader();
            if (_readyToRunAssemblies[0]._availableTypes != null)
            {
                return;
            }
            ReadyToRunSection availableTypesSection;
            if (ReadyToRunHeader.Sections.TryGetValue(ReadyToRunSectionType.AvailableTypes, out availableTypesSection))
            {
                ParseAvailableTypesSection(0, availableTypesSection, GetGlobalMetadata());
            }
            else if (_readyToRunAssemblyHeaders != null)
            {
                for (int assemblyIndex = 0; assemblyIndex < _readyToRunAssemblyHeaders.Count; assemblyIndex++)
                {
                    if (_readyToRunAssemblyHeaders[assemblyIndex].Sections.TryGetValue(
                        ReadyToRunSectionType.AvailableTypes, out availableTypesSection))
                    {
                        ParseAvailableTypesSection(assemblyIndex, availableTypesSection, OpenReferenceAssembly(assemblyIndex + 1));
                    }
                }
            }
        }

        /// <summary>
        /// Parse a single available types section. For composite R2R images this method is called multiple times
        /// as available types are stored separately for each component assembly of the composite R2R executable.
        /// </summary>
        /// <param name="availableTypesSection"></param>
        private void ParseAvailableTypesSection(int assemblyIndex, ReadyToRunSection availableTypesSection, IAssemblyMetadata metadataReader)
        {
            _readyToRunAssemblies[assemblyIndex]._availableTypes = new List<string>();
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
                    string exportedTypeName = GetExportedTypeFullName(metadataReader.MetadataReader, exportedTypeHandle);

                    _readyToRunAssemblies[assemblyIndex]._availableTypes.Add("exported " + exportedTypeName);
                }
                else
                {
                    TypeDefinitionHandle typeDefHandle = MetadataTokens.TypeDefinitionHandle((int)rid);
                    string typeDefName = MetadataNameFormatter.FormatHandle(metadataReader.MetadataReader, typeDefHandle);
                    _readyToRunAssemblies[assemblyIndex]._availableTypes.Add(typeDefName);
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
                _readyToRunAssemblies.Add(new ReadyToRunAssembly(this));
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
                ReadyToRunImportSectionFlags flags = (ReadyToRunImportSectionFlags)NativeReader.ReadUInt16(Image, ref offset);
                ReadyToRunImportSectionType type = (ReadyToRunImportSectionType)NativeReader.ReadByte(Image, ref offset);
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
                    ReadyToRunSignature signature = MetadataNameFormatter.FormatSignature(_assemblyResolver, this, sigOffset);
                    entries.Add(new ReadyToRunImportSection.ImportSectionEntry(entries.Count, entryOffset, entryOffset + rva, section, sigRva, signature));
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
            return CompositeReader.GetOffset(rva);
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

            int assemblyRefCount = (_composite ? 0 : _assemblyCache[0].MetadataReader.GetTableRowCount(TableIndex.AssemblyRef));
            AssemblyReferenceHandle assemblyReferenceHandle = default(AssemblyReferenceHandle);
            metadataReader = null;
            if (refAsmIndex <= (assemblyRefCount))
            {
                metadataReader = _assemblyCache[0].MetadataReader;
                assemblyReferenceHandle = MetadataTokens.AssemblyReferenceHandle(refAsmIndex);
            }
            else
            {
                int index = refAsmIndex - assemblyRefCount;
                if (ReadyToRunHeader.MajorVersion > 6 || (ReadyToRunHeader.MajorVersion == 6 && ReadyToRunHeader.MinorVersion >= 3))
                {
                    if (index == 1)
                    {
                        metadataReader = ManifestReader;
                        assemblyReferenceHandle = default(AssemblyReferenceHandle);
                    }
                    index--;
                }
                if (index > 0)
                {
                    metadataReader = ManifestReader;
                    assemblyReferenceHandle = ManifestReferences[index - 1];
                }
             }

            return assemblyReferenceHandle;
        }

        internal string GetReferenceAssemblyName(int refAsmIndex)
        {
            AssemblyReferenceHandle handle = GetAssemblyAtIndex(refAsmIndex, out MetadataReader reader);

            if (handle.IsNil)
                return reader.GetString(reader.GetAssemblyDefinition().Name);

            return reader.GetString(reader.GetAssemblyReference((AssemblyReferenceHandle)handle).Name);
        }

        /// <summary>
        /// Open a given reference assembly (relative to this ECMA metadata file).
        /// </summary>
        /// <param name="refAsmIndex">Reference assembly index</param>
        /// <returns>MetadataReader instance representing the reference assembly</returns>
        internal IAssemblyMetadata OpenReferenceAssembly(int refAsmIndex)
        {
            IAssemblyMetadata result = (refAsmIndex < _assemblyCache.Count ? _assemblyCache[refAsmIndex] : null);
            if (result == null)
            {
                AssemblyReferenceHandle assemblyReferenceHandle = GetAssemblyAtIndex(refAsmIndex, out MetadataReader metadataReader);

                if (assemblyReferenceHandle.IsNil)
                {
                    result = R2RManifestMetadata;
                }
                else
                {
                    result = _assemblyResolver.FindAssembly(metadataReader, assemblyReferenceHandle, Filename);
                    if (result == null)
                    {
                        string name = metadataReader.GetString(metadataReader.GetAssemblyReference(assemblyReferenceHandle).Name);
                        throw new Exception($"Missing reference assembly: {name}");
                    }
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
