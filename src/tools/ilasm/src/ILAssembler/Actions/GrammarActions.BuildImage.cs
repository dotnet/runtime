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

            Machine machine = VTableFixupSupport.GetEffectiveMachine(_options.Machine ?? Machine.I386);
            ImmutableArray<ValidatedVTableFixup> validatedVTableFixups =
                ValidateVTableFixups(machine);
            ImmutableArray<ValidatedVTableAssociation> validatedVTableAssociations =
                ValidateVTableAssociations(validatedVTableFixups);
            ImmutableArray<VTableExportPEBuilder.DataLabelFixup> validatedDataLabelFixups =
                ValidateDataLabelFixups();
            ImmutableArray<ValidatedExport> validatedExports =
                ValidateExports(validatedVTableAssociations, machine);
            PseudoCustomAttributes.Lower(_entityRegistry, _diagnostics);

            // Return early if there are structural errors that prevent building valid metadata.
            // However, allow errors in method bodies (ILA0016-0019) to pass through so we can
            // emit the assembly with the errors reported.
            // In error-tolerant mode, continue despite errors.
            var structuralErrors = _diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error && !IsRecoverableError(d.Id));
            if (structuralErrors.Any() && !_options.ErrorTolerant)
            {
                return (_diagnostics.ToImmutable(), null);
            }

            BlobBuilder ilStream = new();
            Blob mvidFixup = _entityRegistry.WriteContentTo(_metadataBuilder, ilStream, _mappedFieldDataNames, _options.Deterministic);
            // MetadataRootBuilder only supports module-wide validation suppression, which is
            // required because wrapped GenericParam numbers intentionally violate table ordering.
            bool suppressMetadataValidation =
                _options.ErrorTolerant &&
                _diagnostics.Any(diagnostic => diagnostic.Id == DiagnosticIds.TooManyGenericParameters);
            MetadataRootBuilder rootBuilder = new(
                _metadataBuilder,
                _options.MetadataVersion,
                suppressValidation: suppressMetadataValidation);

            // Apply command-line overrides
            Subsystem subsystem = _options.Subsystem ?? _subsystem;
            int fileAlignment = _options.FileAlignment ?? _alignment;
            long imageBase = _options.ImageBase ?? _imageBase;
            ushort majorSubsystemVersion = _options.SubsystemVersion?.Major ?? 4;
            ushort minorSubsystemVersion = _options.SubsystemVersion?.Minor ?? 0;

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

            Characteristics imageCharacteristics = Characteristics.ExecutableImage |
                (machine is Machine.Amd64 or Machine.IA64 or Machine.Arm64 or Machine.LoongArch64 or Machine.RiscV64
                    ? Characteristics.LargeAddressAware
                    : Characteristics.Bit32Machine);
            if (_options.Dll)
            {
                imageCharacteristics |= Characteristics.Dll;
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
            DebugDirectoryBuilder? debugDirectoryBuilder = BuildDebugDirectory(entryPoint, out _);

            Func<IEnumerable<Blob>, BlobContentId>? deterministicIdProvider = _options.Deterministic
                ? GetDeterministicContentId
                : null;

            // Use custom PE builder if we have vtable fixups, exports, or data label reference fixups
            if (validatedVTableFixups.Length > 0 ||
                validatedExports.Length > 0 ||
                validatedDataLabelFixups.Length > 0)
            {
                ImmutableArray<VTableExportPEBuilder.VTableFixupInfo> vtableFixupInfos =
                    BuildVTableFixupInfos(
                        validatedVTableFixups,
                        validatedVTableAssociations);
                ImmutableArray<VTableExportPEBuilder.ExportInfo> exports =
                    BuildExportInfos(validatedExports);

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
                    deterministicIdProvider: deterministicIdProvider,
                    vtableFixups: vtableFixupInfos,
                    exports: exports,
                    dataLabelFixups: validatedDataLabelFixups);

                return (_diagnostics.ToImmutable(), new CompilationResult(peBuilder, mvidFixup));
            }

            // Apply CorFlags from options or directive
            CorFlags standardCorFlags = _options.CorFlags ?? _corflags;
            if (_options.Prefer32Bit)
            {
                standardCorFlags |= CorFlags.Prefers32Bit;
            }

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

        private ImmutableArray<ValidatedVTableFixup> ValidateVTableFixups(Machine machine)
        {
            if (_vtableFixups.Count == 0)
            {
                return ImmutableArray<ValidatedVTableFixup>.Empty;
            }

            var builder = ImmutableArray.CreateBuilder<ValidatedVTableFixup>(_vtableFixups.Count);

            for (int i = 0; i < _vtableFixups.Count; i++)
            {
                VTableFixupDeclaration declaration = _vtableFixups[i];
                if (!declaration.HasValidSlotCount)
                {
                    continue;
                }

                VTableFixupSupport.VTableFixupEntry entry = declaration.Entry;
                const ushort WidthMask =
                    VTableFixupSupport.COR_VTABLE_32BIT |
                    VTableFixupSupport.COR_VTABLE_64BIT;
                ushort width = (ushort)(entry.Flags & WidthMask);
                ushort expectedWidth = VTableFixupSupport.GetPointerSize(machine) == sizeof(long)
                    ? VTableFixupSupport.COR_VTABLE_64BIT
                    : VTableFixupSupport.COR_VTABLE_32BIT;
                if (width != expectedWidth)
                {
                    ReportError(
                        DiagnosticIds.InvalidVTableWidth,
                        string.Format(
                            DiagnosticMessageTemplates.InvalidVTableWidth,
                            width,
                            machine,
                            expectedWidth == VTableFixupSupport.COR_VTABLE_64BIT
                                ? "int64"
                                : "int32"),
                        declaration.Context);
                    continue;
                }

                if (!_mappedFieldDataNames.TryGetValue(entry.DataLabel, out int dataOffset))
                {
                    ReportError(
                        DiagnosticIds.LabelNotFound,
                        string.Format(DiagnosticMessageTemplates.LabelNotFound, entry.DataLabel),
                        declaration.Context);
                    continue;
                }

                int availableBytes = GetAvailableMappedFieldDataBytes(dataOffset);
                int requiredBytes = checked(
                    entry.SlotCount * VTableFixupSupport.GetSlotSize(entry.Flags));
                if (requiredBytes > availableBytes)
                {
                    ReportError(
                        DiagnosticIds.InsufficientVTableData,
                        string.Format(
                            DiagnosticMessageTemplates.InsufficientVTableData,
                            entry.DataLabel,
                            availableBytes,
                            requiredBytes),
                        declaration.Context);
                    continue;
                }

                builder.Add(new ValidatedVTableFixup(i + 1, entry, dataOffset));
            }

            return builder.ToImmutable();
        }

        private int GetAvailableMappedFieldDataBytes(int dataOffset)
        {
            int endOffset = _mappedFieldData.Count;
            foreach (int otherOffset in _mappedFieldDataNames.Values)
            {
                if (otherOffset > dataOffset && otherOffset < endOffset)
                {
                    endOffset = otherOffset;
                }
            }

            return endOffset - dataOffset;
        }

        private ImmutableArray<VTableExportPEBuilder.DataLabelFixup> ValidateDataLabelFixups()
        {
            var builder =
                ImmutableArray.CreateBuilder<VTableExportPEBuilder.DataLabelFixup>(
                    _mappedFieldDataReferenceFixups.Count);

            foreach (CILParser.DataLabelReferenceValue reference in _mappedFieldDataReferenceFixups)
            {
                if (!_mappedFieldDataNames.TryGetValue(reference.TargetLabel, out int targetOffset))
                {
                    ReportError(
                        DiagnosticIds.LabelNotFound,
                        string.Format(
                            DiagnosticMessageTemplates.LabelNotFound,
                            reference.TargetLabel),
                        reference.Location);
                    continue;
                }

                builder.Add(new VTableExportPEBuilder.DataLabelFixup(
                    reference.DataOffset,
                    targetOffset,
                    reference.PointerSize));
            }

            return builder.ToImmutable();
        }

        private ImmutableArray<ValidatedVTableAssociation> ValidateVTableAssociations(
            ImmutableArray<ValidatedVTableFixup> validatedVTableFixups)
        {
            var fixupsByOriginalIndex = new Dictionary<int, (int SerializedIndex, ValidatedVTableFixup Fixup)>(
                validatedVTableFixups.Length);
            for (int i = 0; i < validatedVTableFixups.Length; i++)
            {
                fixupsByOriginalIndex.Add(
                    validatedVTableFixups[i].OriginalIndex,
                    (i + 1, validatedVTableFixups[i]));
            }

            var builder = ImmutableArray.CreateBuilder<ValidatedVTableAssociation>();
            foreach (EntityRegistry.MethodDefinitionEntity method in GetParsedMethods())
            {
                if (!_vtableEntryDirectiveContexts.TryGetValue(method, out ParserRuleContext? context))
                {
                    continue;
                }

                if (!fixupsByOriginalIndex.TryGetValue(
                    method.VTableEntry,
                    out (int SerializedIndex, ValidatedVTableFixup Fixup) fixup))
                {
                    ReportError(
                        DiagnosticIds.InvalidVTableEntry,
                        string.Format(
                            DiagnosticMessageTemplates.InvalidVTableEntry,
                            method.Name,
                            method.VTableEntry),
                        context);
                    continue;
                }

                if (method.VTableSlot <= 0 || method.VTableSlot > fixup.Fixup.Entry.SlotCount)
                {
                    ReportError(
                        DiagnosticIds.InvalidVTableEntry,
                        string.Format(
                            DiagnosticMessageTemplates.InvalidVTableSlot,
                            method.Name,
                            method.VTableSlot,
                            method.VTableEntry,
                            fixup.Fixup.Entry.SlotCount),
                        context);
                    continue;
                }

                builder.Add(new ValidatedVTableAssociation(method, fixup.SerializedIndex));
            }

            return builder.ToImmutable();
        }

        private ImmutableArray<ValidatedExport> ValidateExports(
            ImmutableArray<ValidatedVTableAssociation> validatedVTableAssociations,
            Machine machine)
        {
            if (_vtableFixups.Count == 0)
            {
                return ImmutableArray<ValidatedExport>.Empty;
            }

            var associationsByMethod =
                new Dictionary<EntityRegistry.MethodDefinitionEntity, ValidatedVTableAssociation>(
                    validatedVTableAssociations.Length);
            foreach (ValidatedVTableAssociation association in validatedVTableAssociations)
            {
                associationsByMethod.Add(association.Method, association);
            }

            var candidates = ImmutableArray.CreateBuilder<ValidatedExport>();
            foreach (EntityRegistry.MethodDefinitionEntity method in GetParsedMethods())
            {
                if (method.ExportOrdinal < 0)
                {
                    continue;
                }

                ParserRuleContext context = _exportDirectiveContexts[method];
                string exportName = method.ExportAlias ?? method.Name;
                if (!VTableExportPEBuilder.IsExportMachineSupported(machine))
                {
                    ReportError(
                        DiagnosticIds.UnsupportedNativeExportMachine,
                        string.Format(
                            DiagnosticMessageTemplates.UnsupportedNativeExportMachine,
                            machine),
                        context);
                    continue;
                }

                if (!associationsByMethod.TryGetValue(
                    method,
                    out ValidatedVTableAssociation association))
                {
                    if (!_vtableEntryDirectiveContexts.ContainsKey(method))
                    {
                        ReportError(
                            DiagnosticIds.InvalidVTableExport,
                            string.Format(
                                DiagnosticMessageTemplates.InvalidVTableExport,
                                exportName),
                            context);
                    }

                    continue;
                }

                candidates.Add(new ValidatedExport(method, association.VTableEntryIndex));
            }

            if (candidates.Count == 0)
            {
                return ImmutableArray<ValidatedExport>.Empty;
            }

            var exportsByOrdinal = new Dictionary<int, ValidatedExport>();
            var nonConflictingExports = ImmutableArray.CreateBuilder<ValidatedExport>(candidates.Count);
            foreach (ValidatedExport export in candidates)
            {
                if (exportsByOrdinal.TryGetValue(
                    export.Method.ExportOrdinal,
                    out ValidatedExport existingExport) &&
                    (existingExport.VTableEntryIndex != export.VTableEntryIndex ||
                     existingExport.Method.VTableSlot != export.Method.VTableSlot))
                {
                    ReportError(
                        DiagnosticIds.DuplicateExportOrdinal,
                        string.Format(
                            DiagnosticMessageTemplates.DuplicateExportOrdinal,
                            export.Method.ExportAlias ?? export.Method.Name,
                            export.Method.ExportOrdinal),
                        _exportDirectiveContexts[export.Method]);
                    continue;
                }

                exportsByOrdinal.TryAdd(export.Method.ExportOrdinal, export);
                nonConflictingExports.Add(export);
            }

            int baseOrdinal = nonConflictingExports.Min(export => export.Method.ExportOrdinal);
            var validatedExports =
                ImmutableArray.CreateBuilder<ValidatedExport>(nonConflictingExports.Count);
            foreach (ValidatedExport export in nonConflictingExports)
            {
                long ordinalIndex = (long)export.Method.ExportOrdinal - baseOrdinal;
                if (ordinalIndex > ushort.MaxValue)
                {
                    ReportError(
                        DiagnosticIds.ExportOrdinalRangeTooLarge,
                        string.Format(
                            DiagnosticMessageTemplates.ExportOrdinalRangeTooLarge,
                            export.Method.ExportOrdinal,
                            baseOrdinal,
                            ushort.MaxValue),
                        _exportDirectiveContexts[export.Method]);
                    continue;
                }

                validatedExports.Add(export);
            }

            return validatedExports.ToImmutable();
        }

        private static ImmutableArray<VTableExportPEBuilder.VTableFixupInfo> BuildVTableFixupInfos(
            ImmutableArray<ValidatedVTableFixup> validatedVTableFixups,
            ImmutableArray<ValidatedVTableAssociation> validatedVTableAssociations)
        {
            var builder =
                ImmutableArray.CreateBuilder<VTableExportPEBuilder.VTableFixupInfo>(
                    validatedVTableFixups.Length);

            for (int i = 0; i < validatedVTableFixups.Length; i++)
            {
                ValidatedVTableFixup fixup = validatedVTableFixups[i];
                var methodTokens = ImmutableArray.CreateBuilder<int>(fixup.Entry.SlotCount);
                methodTokens.Count = fixup.Entry.SlotCount;

                foreach (ValidatedVTableAssociation association in validatedVTableAssociations)
                {
                    if (association.VTableEntryIndex == i + 1)
                    {
                        methodTokens[association.Method.VTableSlot - 1] =
                            MetadataTokens.GetToken(association.Method.Handle);
                    }
                }

                builder.Add(new VTableExportPEBuilder.VTableFixupInfo(
                    fixup.DataOffset,
                    fixup.Entry.SlotCount,
                    fixup.Entry.Flags,
                    methodTokens.MoveToImmutable()));
            }

            return builder.MoveToImmutable();
        }

        private static ImmutableArray<VTableExportPEBuilder.ExportInfo> BuildExportInfos(
            ImmutableArray<ValidatedExport> validatedExports)
        {
            var builder =
                ImmutableArray.CreateBuilder<VTableExportPEBuilder.ExportInfo>(
                    validatedExports.Length);

            foreach (ValidatedExport export in validatedExports)
            {
                builder.Add(new VTableExportPEBuilder.ExportInfo(
                    export.Method.ExportOrdinal,
                    export.Method.ExportAlias ?? export.Method.Name,
                    export.VTableEntryIndex,
                    export.Method.VTableSlot));
            }

            return builder.MoveToImmutable();
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

            Func<IEnumerable<Blob>, BlobContentId> pdbIdProvider = _options.Deterministic
                ? GetDeterministicContentId
                : _ => new BlobContentId(Guid.NewGuid(), 0x04030201);

            // Create the portable PDB
            var pdbBuilder = new PortablePdbBuilder(
                _pdbBuilder,
                typeSystemRowCounts,
                entryPoint,
                idProvider: pdbIdProvider);

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

    }
}
