// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;

namespace ILAssembler
{
#pragma warning disable CA1822 // Mark members as static
    internal sealed partial class GrammarActions
    {
        public (ImmutableArray<Diagnostic> Diagnostics, CompilationResult? Image) BuildImage()
        {
            // Default module name to output filename if no .module directive was provided
            if (_entityRegistry.Module.Name is null && _options.OutputFileName is not null)
            {
                _entityRegistry.Module.Name = _options.OutputFileName;
            }

            // Apply DebuggableAttribute AFTER all source declarations have been processed,
            // so that GetCoreLibAssemblyReference() can find the correct corelib assembly ref
            // declared in the source (e.g., System.Runtime) instead of creating a fallback mscorlib.
            if (_entityRegistry.Assembly is not null && (_options.Debug || _options.DebugMode is not null))
            {
                ApplyDebuggableAttribute();
            }

            // Return early if there are structural errors that prevent building valid metadata.
            // However, allow errors in method bodies (ILA0016-0019) to pass through so we can
            // emit the assembly with the errors reported.
            // In error-tolerant mode, continue despite errors.
            var structuralErrors = _diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error && !IsRecoverableError(d.Id));
            if (structuralErrors.Any() && !_options.ErrorTolerant)
            {
                return (_diagnostics.ToImmutable(), null);
            }

            // Check for vtable fixups and exports - collect export info
            var exports = ImmutableArray.CreateBuilder<VTableExportPEBuilder.ExportInfo>();
            foreach (EntityRegistry.MethodDefinitionEntity method in GetParsedMethods())
            {
                if (method.ExportOrdinal >= 0)
                {
                    exports.Add(new VTableExportPEBuilder.ExportInfo(
                        method.ExportOrdinal,
                        method.ExportAlias ?? method.Name,
                        MetadataTokens.GetToken(method.Handle),
                        method.VTableEntry,
                        method.VTableSlot));
                }
            }

            BlobBuilder ilStream = new();
            PseudoCustomAttributes.Lower(_entityRegistry, _diagnostics);
            Blob mvidFixup = _entityRegistry.WriteContentTo(_metadataBuilder, ilStream, _mappedFieldDataNames, _options.Deterministic);
            MetadataRootBuilder rootBuilder = new(_metadataBuilder, _options.MetadataVersion);

            // Compute metadata size from the MetadataSizes
            // We need this for data label fixup RVA calculations
            var sizes = rootBuilder.Sizes;
            int metadataSize = ComputeMetadataSize(sizes);

            // Apply command-line overrides
            Subsystem subsystem = _options.Subsystem ?? _subsystem;
            int fileAlignment = _options.FileAlignment ?? _alignment;
            long imageBase = _options.ImageBase ?? _imageBase;
            ushort majorSubsystemVersion = _options.SubsystemVersion?.Major ?? 4;
            ushort minorSubsystemVersion = _options.SubsystemVersion?.Minor ?? 0;
            Machine machine = _options.Machine ?? Machine.I386;

            // Build DllCharacteristics from options
            DllCharacteristics dllCharacteristics = DllCharacteristics.DynamicBase | DllCharacteristics.NxCompatible | DllCharacteristics.NoSeh | DllCharacteristics.TerminalServerAware;
            if (_options.AppContainer)
            {
                dllCharacteristics |= DllCharacteristics.AppContainer;
            }
            if (_options.HighEntropyVA)
            {
                dllCharacteristics |= DllCharacteristics.HighEntropyVirtualAddressSpace;
            }
            if (_options.StripReloc)
            {
                dllCharacteristics &= ~DllCharacteristics.DynamicBase;
            }

            Characteristics imageCharacteristics = Characteristics.ExecutableImage;
            if (_options.Dll)
            {
                imageCharacteristics |= Characteristics.Dll;
            }
            if (machine is Machine.I386 or Machine.Arm)
            {
                imageCharacteristics |= Characteristics.Bit32Machine;
            }
            else if (machine is Machine.Amd64 or Machine.Arm64)
            {
                imageCharacteristics |= Characteristics.LargeAddressAware;
            }

            // Compute stack reserve: command-line option overrides directive, which overrides default
            ulong sizeOfStackReserve = (ulong)(_options.StackReserve ?? (_stackReserve != 0 ? _stackReserve : 0x00100000));

            PEHeaderBuilder header = new(
                machine: machine,
                fileAlignment: fileAlignment,
                imageBase: (ulong)imageBase,
                subsystem: subsystem,
                majorSubsystemVersion: majorSubsystemVersion,
                minorSubsystemVersion: minorSubsystemVersion,
                dllCharacteristics: dllCharacteristics,
                imageCharacteristics: imageCharacteristics,
                sizeOfStackReserve: sizeOfStackReserve);

            MethodDefinitionHandle entryPoint = default;
            if (_entityRegistry.EntryPoint is not null)
            {
                entryPoint = (MethodDefinitionHandle)_entityRegistry.EntryPoint.Handle;
            }

            // Build debug directory if we have any debug info
            DebugDirectoryBuilder? debugDirectoryBuilder = BuildDebugDirectory(entryPoint, out int debugDataSize);

            // Use custom PE builder if we have vtable fixups, exports, or data label reference fixups
            if (_vtableFixups.Count > 0 || exports.Count > 0 || _mappedFieldDataReferenceFixups.Count > 0)
            {
                var vtableFixupInfos = BuildVTableFixupInfos();

                // Apply CorFlags from options or directive
                CorFlags corFlags = _options.CorFlags ?? _corflags;
                if (_options.Prefer32Bit)
                {
                    corFlags |= CorFlags.Prefers32Bit;
                }

                VTableExportPEBuilder peBuilder = new(
                    header,
                    rootBuilder,
                    ilStream,
                    _mappedFieldData,
                    _manifestResources,
                    debugDirectoryBuilder: debugDirectoryBuilder,
                    entryPoint: entryPoint,
                    flags: corFlags,
                    vtableFixups: vtableFixupInfos,
                    exports: exports.ToImmutable(),
                    mappedFieldDataOffsets: _mappedFieldDataNames,
                    dataLabelFixups: _mappedFieldDataReferenceFixups,
                    metadataSize: metadataSize,
                    debugDataSize: debugDataSize);

                return (_diagnostics.ToImmutable(), new CompilationResult(peBuilder, mvidFixup));
            }

            // Apply CorFlags from options or directive
            CorFlags standardCorFlags = _options.CorFlags ?? _corflags;
            if (_options.Prefer32Bit)
            {
                standardCorFlags |= CorFlags.Prefers32Bit;
            }

            // Deterministic ID provider for reproducible builds
            Func<IEnumerable<Blob>, BlobContentId>? deterministicIdProvider = _options.Deterministic
                ? GetDeterministicContentId
                : null;

            ManagedPEBuilder standardBuilder = new(
                header,
                rootBuilder,
                ilStream,
                _mappedFieldData,
                _manifestResources,
                flags: standardCorFlags,
                entryPoint: entryPoint,
                debugDirectoryBuilder: debugDirectoryBuilder,
                deterministicIdProvider: deterministicIdProvider);

            return (_diagnostics.ToImmutable(), new CompilationResult(standardBuilder, mvidFixup));
        }

        private static BlobContentId GetDeterministicContentId(IEnumerable<Blob> content)
        {
            using IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            foreach (Blob blob in content)
            {
                hash.AppendData(blob.GetBytes());
            }

            return BlobContentId.FromHash(hash.GetHashAndReset());
        }

        private ImmutableArray<VTableExportPEBuilder.VTableFixupInfo> BuildVTableFixupInfos()
        {
            if (_vtableFixups.Count == 0)
                return ImmutableArray<VTableExportPEBuilder.VTableFixupInfo>.Empty;

            var builder = ImmutableArray.CreateBuilder<VTableExportPEBuilder.VTableFixupInfo>(_vtableFixups.Count);

            for (int entryIndex = 0; entryIndex < _vtableFixups.Count; entryIndex++)
            {
                var vtf = _vtableFixups[entryIndex];
                var methodTokens = ImmutableArray.CreateBuilder<int>(vtf.SlotCount);

                // Initialize with zeros
                for (int i = 0; i < vtf.SlotCount; i++)
                {
                    methodTokens.Add(0);
                }

                // Find methods that reference this vtable entry
                foreach (EntityRegistry.MethodDefinitionEntity method in GetParsedMethods())
                {
                    if (method.VTableEntry == entryIndex + 1 && // 1-based
                        method.VTableSlot > 0 &&
                        method.VTableSlot <= vtf.SlotCount)
                    {
                        methodTokens[method.VTableSlot - 1] = MetadataTokens.GetToken(method.Handle);
                    }
                }

                builder.Add(new VTableExportPEBuilder.VTableFixupInfo(
                    vtf.DataLabel,
                    vtf.SlotCount,
                    vtf.Flags,
                    methodTokens.ToImmutable()));
            }

            return builder.ToImmutable();
        }

        private IEnumerable<EntityRegistry.MethodDefinitionEntity> GetParsedMethods()
        {
            foreach (EntityRegistry.TypeDefinitionEntity type in _entityRegistry.GetSeenEntities(TableIndex.TypeDef))
            {
                foreach (EntityRegistry.MethodDefinitionEntity method in type.Methods)
                {
                    yield return method;
                }
            }
        }

        private DebugDirectoryBuilder? BuildDebugDirectory(MethodDefinitionHandle entryPoint, out int debugDataSize)
        {
            debugDataSize = 0;

            // Check if we have any methods with debug info
            bool hasDebugInfo = false;
            foreach (var entity in _entityRegistry.GetSeenEntities(TableIndex.MethodDef))
            {
                if (entity is EntityRegistry.MethodDefinitionEntity method &&
                    method.DebugInfo.SequencePoints.Count > 0)
                {
                    hasDebugInfo = true;
                    break;
                }
            }

            // Generate PDB if we have debug info OR if --debug/--pdb options are set
            bool generatePdb = hasDebugInfo || _options.Debug || _options.Pdb;
            if (!generatePdb)
            {
                return null;
            }

            // Build PDB metadata
            BuildPdbMetadata();

            // Get row counts from main metadata for the portable PDB
            var typeSystemRowCounts = _metadataBuilder.GetRowCounts();

            // Create the portable PDB
            var pdbBuilder = new PortablePdbBuilder(
                _pdbBuilder,
                typeSystemRowCounts,
                entryPoint,
                idProvider: _options.Deterministic ? GetDeterministicContentId : null);

            var pdbBlob = new BlobBuilder();
            var pdbContentId = pdbBuilder.Serialize(pdbBlob);

            // Create debug directory with embedded PDB
            var debugDirectoryBuilder = new DebugDirectoryBuilder();
            debugDirectoryBuilder.AddCodeViewEntry(
                $"assembly.pdb",
                pdbContentId,
                pdbBuilder.FormatVersion);
            debugDirectoryBuilder.AddEmbeddedPortablePdbEntry(pdbBlob, pdbBuilder.FormatVersion);

            // Calculate debug data size:
            // 2 debug directory entries (28 bytes each) + CodeView data (~24 bytes) + Embedded PDB data (compressed pdbBlob + 8 header)
            // CodeView entry: signature (4) + guid (16) + age (4) + path (variable, ~12 for "assembly.pdb\0")
            const int debugDirEntrySize = 28;
            int codeViewDataSize = 4 + 16 + 4 + "assembly.pdb".Length + 1; // signature + guid + age + path + null
            int embeddedPdbHeaderSize = 8; // MPDB signature (4) + uncompressed size (4)
            // The embedded PDB is compressed, estimate conservatively as same size
            int embeddedPdbDataSize = embeddedPdbHeaderSize + pdbBlob.Count;

            debugDataSize = (2 * debugDirEntrySize) + codeViewDataSize + embeddedPdbDataSize;

            return debugDirectoryBuilder;
        }

        private void BuildPdbMetadata()
        {
            // Add documents and sequence points to the PDB metadata builder
            foreach (var entity in _entityRegistry.GetSeenEntities(TableIndex.MethodDef))
            {
                if (entity is not EntityRegistry.MethodDefinitionEntity method)
                {
                    continue;
                }

                var debugInfo = method.DebugInfo;
                if (debugInfo.SequencePoints.Count == 0)
                {
                    // Add empty debug info entry for methods without sequence points
                    _pdbBuilder.AddMethodDebugInformation(default, default);
                    continue;
                }

                // Get or create document handle
                DocumentHandle documentHandle = default;
                if (debugInfo.DocumentPath is not null)
                {
                    if (!_documentHandles.TryGetValue(debugInfo.DocumentPath, out documentHandle))
                    {
                        var nameHandle = _pdbBuilder.GetOrAddDocumentName(debugInfo.DocumentPath);
                        var languageGuidHandle = _currentLanguageGuid != Guid.Empty
                            ? _pdbBuilder.GetOrAddGuid(_currentLanguageGuid)
                            : default;
                        documentHandle = _pdbBuilder.AddDocument(
                            nameHandle,
                            default, // hash algorithm
                            default, // hash
                            languageGuidHandle);
                        _documentHandles[debugInfo.DocumentPath] = documentHandle;
                    }
                }

                // Encode sequence points
                var sequencePointsBlob = EncodeSequencePoints(debugInfo.SequencePoints);
                var sequencePointsBlobHandle = _pdbBuilder.GetOrAddBlob(sequencePointsBlob);

                _pdbBuilder.AddMethodDebugInformation(documentHandle, sequencePointsBlobHandle);
            }
        }

        private static BlobBuilder EncodeSequencePoints(List<EntityRegistry.SequencePoint> sequencePoints)
        {
            var builder = new BlobBuilder();

            if (sequencePoints.Count == 0)
            {
                return builder;
            }

            // LocalSignature (not used here, write 0)
            builder.WriteCompressedInteger(0);

            int previousOffset = 0;
            int previousStartLine = -1;
            int previousStartColumn = -1;

            foreach (var sp in sequencePoints)
            {
                // IL offset delta
                int offsetDelta = sp.ILOffset - previousOffset;
                builder.WriteCompressedInteger(offsetDelta);
                previousOffset = sp.ILOffset;

                if (sp.IsHidden)
                {
                    // Hidden sequence point: delta lines = 0, delta columns = 0
                    builder.WriteCompressedInteger(0);
                    builder.WriteCompressedInteger(0);
                }
                else
                {
                    // Delta lines
                    int deltaLines = sp.EndLine - sp.StartLine;
                    builder.WriteCompressedInteger(deltaLines);

                    // Delta columns
                    int deltaColumns = sp.EndColumn - sp.StartColumn;
                    if (deltaLines == 0)
                    {
                        builder.WriteCompressedInteger(deltaColumns);
                    }
                    else
                    {
                        builder.WriteCompressedSignedInteger(deltaColumns);
                    }

                    // Start line delta (signed)
                    if (previousStartLine < 0)
                    {
                        builder.WriteCompressedInteger(sp.StartLine);
                    }
                    else
                    {
                        builder.WriteCompressedSignedInteger(sp.StartLine - previousStartLine);
                    }

                    // Start column delta (signed)
                    if (previousStartColumn < 0)
                    {
                        builder.WriteCompressedInteger(sp.StartColumn);
                    }
                    else
                    {
                        builder.WriteCompressedSignedInteger(sp.StartColumn - previousStartColumn);
                    }

                    previousStartLine = sp.StartLine;
                    previousStartColumn = sp.StartColumn;
                }
            }

            return builder;
        }

        /// <summary>
        /// Add DebuggableAttribute to the assembly based on debug options.
        /// - /DEBUG: 0x101 = Default | DisableOptimizations
        /// - /DEBUG=OPT: 0x03 = Default | IgnoreSymbolStoreSequencePoints
        /// - /DEBUG=IMPL: 0x103 = Default | DisableOptimizations | EnableEditAndContinue
        /// </summary>
        private void ApplyDebuggableAttribute()
        {
            if (_entityRegistry.Assembly is null)
            {
                return;
            }

            // DebuggingModes enum values from System.Diagnostics.DebuggableAttribute:
            // None = 0x00, Default = 0x01, IgnoreSymbolStoreSequencePoints = 0x02,
            // EnableEditAndContinue = 0x04, DisableOptimizations = 0x100
            const int DebuggingModesDefault = 0x101;  // Default | DisableOptimizations
            const int DebuggingModesOpt = 0x03;       // Default | IgnoreSymbolStoreSequencePoints
            const int DebuggingModesImpl = 0x103;     // Default | DisableOptimizations | EnableEditAndContinue

            int debuggingModes = _options.DebugMode switch
            {
                DebugMode.Opt => DebuggingModesOpt,
                DebugMode.Impl => DebuggingModesImpl,
                _ => DebuggingModesDefault
            };

            // Get reference to core library
            var coreAsmRef = _entityRegistry.GetCoreLibAssemblyReference();

            // Create reference to System.Diagnostics.DebuggableAttribute
            var debuggableAttrType = _entityRegistry.GetOrCreateTypeReference(
                coreAsmRef,
                new TypeName(null, "System.Diagnostics.DebuggableAttribute"));

            // Create reference to nested type DebuggingModes
            var debuggingModesType = _entityRegistry.GetOrCreateTypeReference(
                debuggableAttrType,
                new TypeName(null, "DebuggingModes"));

            // Create constructor signature: .ctor(DebuggingModes)
            BlobBuilder ctorSig = new();
            var sigEncoder = new BlobEncoder(ctorSig);
            sigEncoder.MethodSignature(SignatureCallingConvention.Default, 0, isInstanceMethod: true)
                .Parameters(1,
                    returnType => returnType.Void(),
                    parameters => parameters.AddParameter().Type().Type(debuggingModesType.Handle, isValueType: true));

            var ctor = _entityRegistry.CreateLazilyRecordedMemberReference(debuggableAttrType, ".ctor", ctorSig);

            // Create custom attribute blob: prolog (0x0001) + int32 value + named args count (0x0000)
            BlobBuilder attrValue = new();
            attrValue.WriteUInt16(0x0001); // Prolog
            attrValue.WriteInt32(debuggingModes); // DebuggingModes value
            attrValue.WriteUInt16(0x0000); // No named arguments

            // Create and attach the custom attribute
            var customAttr = _entityRegistry.CreateCustomAttribute(ctor, attrValue);
            customAttr.Owner = _entityRegistry.Assembly;
        }

        /// <summary>
        /// Computes the total metadata size from MetadataSizes.
        /// This replicates the internal MetadataSizes.MetadataSize calculation.
        /// </summary>
        private static int ComputeMetadataSize(MetadataSizes sizes)
        {
            // Metadata header size (fixed structure):
            // - signature (4)
            // - major/minor version (4)
            // - reserved (4)
            // - version string length (4)
            // - version string padded to 4 bytes ("v4.0.30319" = 12 bytes padded)
            // - storage header (4)
            // - 5 stream headers (#~, #Strings, #US, #GUID, #Blob) = 76 bytes
            // Total header: ~108 bytes
            const int metadataHeaderSize = 108;

            // Stream storage: heaps (#Strings, #US, #GUID, #Blob) - we can get aligned sizes
            int heapStorageSize = 0;
            heapStorageSize += sizes.GetAlignedHeapSize(HeapIndex.String);
            heapStorageSize += sizes.GetAlignedHeapSize(HeapIndex.UserString);
            heapStorageSize += sizes.GetAlignedHeapSize(HeapIndex.Guid);
            heapStorageSize += sizes.GetAlignedHeapSize(HeapIndex.Blob);

            // Table stream (#~): header + table data
            // Header: Reserved(4) + Version(2) + HeapSizes(1) + RowIdBitWidth(1) + ValidMask(8) + SortedMask(8)
            //         + 4 bytes per present table for row counts
            int tableStreamSize = 24; // base header
            var rowCounts = sizes.RowCounts;

            // Count present tables and add 4 bytes each for row count
            for (int i = 0; i < rowCounts.Length; i++)
            {
                if (rowCounts[i] > 0)
                {
                    tableStreamSize += 4;
                }
            }

            // Add table data size with estimated row sizes
            // Row sizes depend on index sizes (2 or 4 bytes) which we don't have access to
            // For small assemblies, all indexes are 2 bytes
            tableStreamSize += rowCounts[(int)TableIndex.Module] * 10;       // 2+2+2+2+2
            tableStreamSize += rowCounts[(int)TableIndex.TypeRef] * 6;       // 2+2+2
            tableStreamSize += rowCounts[(int)TableIndex.TypeDef] * 14;      // 4+2+2+2+2+2
            tableStreamSize += rowCounts[(int)TableIndex.Field] * 6;         // 2+2+2
            tableStreamSize += rowCounts[(int)TableIndex.MethodDef] * 14;    // 4+2+2+2+2+2
            tableStreamSize += rowCounts[(int)TableIndex.Param] * 6;         // 2+2+2
            tableStreamSize += rowCounts[(int)TableIndex.InterfaceImpl] * 4; // 2+2
            tableStreamSize += rowCounts[(int)TableIndex.MemberRef] * 6;     // 2+2+2
            tableStreamSize += rowCounts[(int)TableIndex.Constant] * 6;      // 2+2+2
            tableStreamSize += rowCounts[(int)TableIndex.CustomAttribute] * 6; // 2+2+2
            tableStreamSize += rowCounts[(int)TableIndex.FieldMarshal] * 4;  // 2+2
            tableStreamSize += rowCounts[(int)TableIndex.DeclSecurity] * 6;  // 2+2+2
            tableStreamSize += rowCounts[(int)TableIndex.ClassLayout] * 8;   // 2+4+2
            tableStreamSize += rowCounts[(int)TableIndex.FieldLayout] * 6;   // 4+2
            tableStreamSize += rowCounts[(int)TableIndex.StandAloneSig] * 2; // 2
            tableStreamSize += rowCounts[(int)TableIndex.EventMap] * 4;      // 2+2
            tableStreamSize += rowCounts[(int)TableIndex.Event] * 6;         // 2+2+2
            tableStreamSize += rowCounts[(int)TableIndex.PropertyMap] * 4;   // 2+2
            tableStreamSize += rowCounts[(int)TableIndex.Property] * 6;      // 2+2+2
            tableStreamSize += rowCounts[(int)TableIndex.MethodSemantics] * 6; // 2+2+2
            tableStreamSize += rowCounts[(int)TableIndex.MethodImpl] * 6;    // 2+2+2
            tableStreamSize += rowCounts[(int)TableIndex.ModuleRef] * 2;     // 2
            tableStreamSize += rowCounts[(int)TableIndex.TypeSpec] * 2;      // 2
            tableStreamSize += rowCounts[(int)TableIndex.ImplMap] * 8;       // 2+2+2+2
            tableStreamSize += rowCounts[(int)TableIndex.FieldRva] * 6;      // 4+2
            tableStreamSize += rowCounts[(int)TableIndex.Assembly] * 22;     // 16+2+2+2
            tableStreamSize += rowCounts[(int)TableIndex.AssemblyRef] * 20;  // 12+2+2+2+2
            tableStreamSize += rowCounts[(int)TableIndex.File] * 8;          // 4+2+2
            tableStreamSize += rowCounts[(int)TableIndex.ExportedType] * 14; // 8+2+2+2
            tableStreamSize += rowCounts[(int)TableIndex.ManifestResource] * 12; // 8+2+2
            tableStreamSize += rowCounts[(int)TableIndex.NestedClass] * 4;   // 2+2
            tableStreamSize += rowCounts[(int)TableIndex.GenericParam] * 8;  // 4+2+2
            tableStreamSize += rowCounts[(int)TableIndex.MethodSpec] * 4;    // 2+2
            tableStreamSize += rowCounts[(int)TableIndex.GenericParamConstraint] * 4; // 2+2

            // Align table stream to 4 bytes (includes +1 for terminating 0 byte)
            tableStreamSize = ((tableStreamSize + 1) + 3) & ~3;

            return metadataHeaderSize + heapStorageSize + tableStreamSize;
        }

    }
}
