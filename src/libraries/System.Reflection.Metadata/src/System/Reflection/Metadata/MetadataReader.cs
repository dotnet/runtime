// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection.Internal;
using System.Reflection.Metadata.Ecma335;
using System.Text;

namespace System.Reflection.Metadata
{
    /// <summary>
    /// Reads metadata as defined byte the ECMA 335 CLI specification.
    /// </summary>
    public sealed partial class MetadataReader
    {
        internal readonly NamespaceCache NamespaceCache;
        internal readonly MemoryBlock Block;

        // A row id of "mscorlib" AssemblyRef in a WinMD file (each WinMD file must have such a reference).
        internal readonly int WinMDMscorlibRef;

        // Keeps the underlying memory alive.
        private readonly object _memoryOwnerObj;

        private readonly MetadataReaderOptions _options;
        private Dictionary<TypeDefinitionHandle, ImmutableArray<TypeDefinitionHandle>> _lazyNestedTypesMap;
        
        #region Constructors

        /// <summary>
        /// Creates a metadata reader from the metadata stored at the given memory location.
        /// </summary>
        /// <remarks>
        /// The memory is owned by the caller and it must be kept memory alive and unmodified throughout the lifetime of the <see cref="MetadataReader"/>.
        /// </remarks>
        public unsafe MetadataReader(byte* metadata, int length)
            : this(metadata, length, MetadataReaderOptions.Default, utf8Decoder: null, memoryOwner: null)
        {
        }

        /// <summary>
        /// Creates a metadata reader from the metadata stored at the given memory location.
        /// </summary>
        /// <remarks>
        /// The memory is owned by the caller and it must be kept memory alive and unmodified throughout the lifetime of the <see cref="MetadataReader"/>.
        /// Use <see cref="PEReaderExtensions.GetMetadataReader(PortableExecutable.PEReader, MetadataReaderOptions)"/> to obtain 
        /// metadata from a PE image.
        /// </remarks>
        public unsafe MetadataReader(byte* metadata, int length, MetadataReaderOptions options)
            : this(metadata, length, options, utf8Decoder: null, memoryOwner: null)
        {
        }

        /// <summary>
        /// Creates a metadata reader from the metadata stored at the given memory location.
        /// </summary>
        /// <remarks>
        /// The memory is owned by the caller and it must be kept memory alive and unmodified throughout the lifetime of the <see cref="MetadataReader"/>.
        /// Use <see cref="PEReaderExtensions.GetMetadataReader(PortableExecutable.PEReader, MetadataReaderOptions, MetadataStringDecoder)"/> to obtain 
        /// metadata from a PE image.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="length"/> is not positive.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="metadata"/> is null.</exception>
        /// <exception cref="ArgumentException">The encoding of <paramref name="utf8Decoder"/> is not <see cref="UTF8Encoding"/>.</exception>
        /// <exception cref="PlatformNotSupportedException">The current platform is big-endian.</exception>
        /// <exception cref="BadImageFormatException">Bad metadata header.</exception>
        public unsafe MetadataReader(byte* metadata, int length, MetadataReaderOptions options, MetadataStringDecoder utf8Decoder)
            : this(metadata, length, options, utf8Decoder, memoryOwner: null)
        {
        }

        internal unsafe MetadataReader(byte* metadata, int length, MetadataReaderOptions options, MetadataStringDecoder utf8Decoder, object memoryOwner)
        {
            // Do not throw here when length is 0. We'll throw BadImageFormatException later on, so that the caller doesn't need to 
            // worry about the image (stream) being empty and can handle all image errors by catching BadImageFormatException.
            if (length < 0)
            {
                Throw.ArgumentOutOfRange(nameof(length));
            }

            if (metadata == null)
            {
                Throw.ArgumentNull(nameof(metadata));
            }

            if (utf8Decoder == null)
            {
                utf8Decoder = MetadataStringDecoder.DefaultUTF8;
            }

            if (!(utf8Decoder.Encoding is UTF8Encoding))
            {
                Throw.InvalidArgument(SR.MetadataStringDecoderEncodingMustBeUtf8, nameof(utf8Decoder));
            }

            Block = new MemoryBlock(metadata, length);

            _memoryOwnerObj = memoryOwner;
            _options = options;
            UTF8Decoder = utf8Decoder;

            var headerReader = new BlobReader(Block);
            ReadMetadataHeader(ref headerReader, out _versionString);
            _metadataKind = GetMetadataKind(_versionString);
            var streamHeaders = ReadStreamHeaders(ref headerReader);

            // storage header and stream headers:
            InitializeStreamReaders(Block, streamHeaders, out _metadataStreamKind, out var metadataTableStream, out var pdbStream);

            int[] externalTableRowCountsOpt;
            if (pdbStream.Length > 0)
            {
                int pdbStreamOffset = (int)(pdbStream.Pointer - metadata);
                ReadStandalonePortablePdbStream(pdbStream, pdbStreamOffset, out _debugMetadataHeader, out externalTableRowCountsOpt);
            }
            else
            {
                externalTableRowCountsOpt = null;
            }

            var tableReader = new BlobReader(metadataTableStream);

            ReadMetadataTableHeader(ref tableReader, out var heapSizes, out var metadataTableRowCounts, out _sortedTables);

            InitializeTableReaders(tableReader.GetMemoryBlockAt(0, tableReader.RemainingBytes), heapSizes, metadataTableRowCounts, externalTableRowCountsOpt);

            // This previously could occur in obfuscated assemblies but a check was added to prevent 
            // it getting to this point
            Debug.Assert(AssemblyTable.NumberOfRows <= 1);

            // Although the specification states that the module table will have exactly one row,
            // the native metadata reader would successfully read files containing more than one row.
            // Such files exist in the wild and may be produced by obfuscators.
            if (pdbStream.Length == 0 && ModuleTable.NumberOfRows < 1)
            {
                throw new BadImageFormatException(SR.Format(SR.ModuleTableInvalidNumberOfRows, this.ModuleTable.NumberOfRows));
            }

            //  read 
            NamespaceCache = new NamespaceCache(this);

            if (_metadataKind != MetadataKind.Ecma335)
            {
                WinMDMscorlibRef = FindMscorlibAssemblyRefNoProjection();
            }
        }

        #endregion

        #region Metadata Headers

        private readonly string _versionString;
        private readonly MetadataKind _metadataKind;
        private readonly MetadataStreamKind _metadataStreamKind;
        private readonly DebugMetadataHeader _debugMetadataHeader;

        internal StringHeap StringHeap;
        internal BlobHeap BlobHeap;
        internal GuidHeap GuidHeap;
        internal UserStringHeap UserStringHeap;

        /// <summary>
        /// True if the metadata stream has minimal delta format. Used for EnC.
        /// </summary>
        /// <remarks>
        /// The metadata stream has minimal delta format if "#JTD" stream is present.
        /// Minimal delta format uses large size (4B) when encoding table/heap references.
        /// The heaps in minimal delta only contain data of the delta, 
        /// there is no padding at the beginning of the heaps that would align them 
        /// with the original full metadata heaps.
        /// </remarks>
        internal bool IsMinimalDelta;

        /// <summary>
        /// Looks like this function reads beginning of the header described in
        /// ECMA-335 24.2.1 Metadata root
        /// </summary>
        private void ReadMetadataHeader(ref BlobReader memReader, out string versionString)
        {
            if (memReader.RemainingBytes < COR20Constants.MinimumSizeofMetadataHeader)
            {
                throw new BadImageFormatException(SR.MetadataHeaderTooSmall);
            }

            uint signature = memReader.ReadUInt32();
            if (signature != COR20Constants.COR20MetadataSignature)
            {
                throw new BadImageFormatException(SR.MetadataSignature);
            }

            // major version
            memReader.ReadUInt16();

            // minor version
            memReader.ReadUInt16();

            // reserved:
            memReader.ReadUInt32();

            int versionStringSize = memReader.ReadInt32();
            if (memReader.RemainingBytes < versionStringSize)
            {
                throw new BadImageFormatException(SR.NotEnoughSpaceForVersionString);
            }

            int numberOfBytesRead;
            versionString = memReader.GetMemoryBlockAt(0, versionStringSize).PeekUtf8NullTerminated(0, null, UTF8Decoder, out numberOfBytesRead, '\0');
            memReader.Offset += versionStringSize;
        }

        private MetadataKind GetMetadataKind(string versionString)
        {
            // Treat metadata as CLI raw metadata if the client doesn't want to see projections.
            if ((_options & MetadataReaderOptions.ApplyWindowsRuntimeProjections) == 0)
            {
                return MetadataKind.Ecma335;
            }

            if (!versionString.Contains("WindowsRuntime"))
            {
                return MetadataKind.Ecma335;
            }
            else if (versionString.Contains("CLR"))
            {
                return MetadataKind.ManagedWindowsMetadata;
            }
            else
            {
                return MetadataKind.WindowsMetadata;
            }
        }

        /// <summary>
        /// Reads stream headers described in ECMA-335 24.2.2 Stream header
        /// </summary>
        private StreamHeader[] ReadStreamHeaders(ref BlobReader memReader)
        {
            // storage header:
            memReader.ReadUInt16();
            int streamCount = memReader.ReadInt16();

            var streamHeaders = new StreamHeader[streamCount];
            for (int i = 0; i < streamHeaders.Length; i++)
            {
                if (memReader.RemainingBytes < COR20Constants.MinimumSizeofStreamHeader)
                {
                    throw new BadImageFormatException(SR.StreamHeaderTooSmall);
                }

                streamHeaders[i].Offset = memReader.ReadUInt32();
                streamHeaders[i].Size = memReader.ReadInt32();
                streamHeaders[i].Name = memReader.ReadUtf8NullTerminated();
                bool aligned = memReader.TryAlign(4);

                if (!aligned || memReader.RemainingBytes == 0)
                {
                    throw new BadImageFormatException(SR.NotEnoughSpaceForStreamHeaderName);
                }
            }

            return streamHeaders;
        }

        private void InitializeStreamReaders(
            in MemoryBlock metadataRoot, 
            StreamHeader[] streamHeaders, 
            out MetadataStreamKind metadataStreamKind,
            out MemoryBlock metadataTableStream,
            out MemoryBlock standalonePdbStream)
        {
            metadataTableStream = default;
            standalonePdbStream = default;
            metadataStreamKind = MetadataStreamKind.Illegal;

            foreach (StreamHeader streamHeader in streamHeaders)
            {
                switch (streamHeader.Name)
                {
                    case COR20Constants.StringStreamName:
                        if (metadataRoot.Length < streamHeader.Offset + streamHeader.Size)
                        {
                            throw new BadImageFormatException(SR.NotEnoughSpaceForStringStream);
                        }

                        this.StringHeap = new StringHeap(metadataRoot.GetMemoryBlockAt((int)streamHeader.Offset, streamHeader.Size), _metadataKind);
                        break;

                    case COR20Constants.BlobStreamName:
                        if (metadataRoot.Length < streamHeader.Offset + streamHeader.Size)
                        {
                            throw new BadImageFormatException(SR.NotEnoughSpaceForBlobStream);
                        }

                        this.BlobHeap = new BlobHeap(metadataRoot.GetMemoryBlockAt((int)streamHeader.Offset, streamHeader.Size), _metadataKind);
                        break;

                    case COR20Constants.GUIDStreamName:
                        if (metadataRoot.Length < streamHeader.Offset + streamHeader.Size)
                        {
                            throw new BadImageFormatException(SR.NotEnoughSpaceForGUIDStream);
                        }

                        this.GuidHeap = new GuidHeap(metadataRoot.GetMemoryBlockAt((int)streamHeader.Offset, streamHeader.Size));
                        break;

                    case COR20Constants.UserStringStreamName:
                        if (metadataRoot.Length < streamHeader.Offset + streamHeader.Size)
                        {
                            throw new BadImageFormatException(SR.NotEnoughSpaceForBlobStream);
                        }

                        this.UserStringHeap = new UserStringHeap(metadataRoot.GetMemoryBlockAt((int)streamHeader.Offset, streamHeader.Size));
                        break;

                    case COR20Constants.CompressedMetadataTableStreamName:
                        if (metadataRoot.Length < streamHeader.Offset + streamHeader.Size)
                        {
                            throw new BadImageFormatException(SR.NotEnoughSpaceForMetadataStream);
                        }

                        metadataStreamKind = MetadataStreamKind.Compressed;
                        metadataTableStream = metadataRoot.GetMemoryBlockAt((int)streamHeader.Offset, streamHeader.Size);
                        break;

                    case COR20Constants.UncompressedMetadataTableStreamName:
                        if (metadataRoot.Length < streamHeader.Offset + streamHeader.Size)
                        {
                            throw new BadImageFormatException(SR.NotEnoughSpaceForMetadataStream);
                        }

                        metadataStreamKind = MetadataStreamKind.Uncompressed;
                        metadataTableStream = metadataRoot.GetMemoryBlockAt((int)streamHeader.Offset, streamHeader.Size);
                        break;

                    case COR20Constants.MinimalDeltaMetadataTableStreamName:
                        if (metadataRoot.Length < streamHeader.Offset + streamHeader.Size)
                        {
                            throw new BadImageFormatException(SR.NotEnoughSpaceForMetadataStream);
                        }

                        // the content of the stream is ignored
                        this.IsMinimalDelta = true;
                        break;

                    case COR20Constants.StandalonePdbStreamName:
                        if (metadataRoot.Length < streamHeader.Offset + streamHeader.Size)
                        {
                            throw new BadImageFormatException(SR.NotEnoughSpaceForMetadataStream);
                        }

                        standalonePdbStream = metadataRoot.GetMemoryBlockAt((int)streamHeader.Offset, streamHeader.Size);
                        break;

                    default:
                        // Skip unknown streams. Some obfuscators insert invalid streams.
                        continue;
                }
            }

            if (IsMinimalDelta && metadataStreamKind != MetadataStreamKind.Uncompressed)
            {
                throw new BadImageFormatException(SR.InvalidMetadataStreamFormat);
            }
        }

        #endregion

        #region Tables and Heaps

        private readonly TableMask _sortedTables;

        /// <summary>
        /// A row count for each possible table. May be indexed by <see cref="TableIndex"/>.
        /// </summary>
        internal int[] TableRowCounts;

        internal ModuleTableReader ModuleTable;
        internal TypeRefTableReader TypeRefTable;
        internal TypeDefTableReader TypeDefTable;
        internal FieldPtrTableReader FieldPtrTable;
        internal FieldTableReader FieldTable;
        internal MethodPtrTableReader MethodPtrTable;
        internal MethodTableReader MethodDefTable;
        internal ParamPtrTableReader ParamPtrTable;
        internal ParamTableReader ParamTable;
        internal InterfaceImplTableReader InterfaceImplTable;
        internal MemberRefTableReader MemberRefTable;
        internal ConstantTableReader ConstantTable;
        internal CustomAttributeTableReader CustomAttributeTable;
        internal FieldMarshalTableReader FieldMarshalTable;
        internal DeclSecurityTableReader DeclSecurityTable;
        internal ClassLayoutTableReader ClassLayoutTable;
        internal FieldLayoutTableReader FieldLayoutTable;
        internal StandAloneSigTableReader StandAloneSigTable;
        internal EventMapTableReader EventMapTable;
        internal EventPtrTableReader EventPtrTable;
        internal EventTableReader EventTable;
        internal PropertyMapTableReader PropertyMapTable;
        internal PropertyPtrTableReader PropertyPtrTable;
        internal PropertyTableReader PropertyTable;
        internal MethodSemanticsTableReader MethodSemanticsTable;
        internal MethodImplTableReader MethodImplTable;
        internal ModuleRefTableReader ModuleRefTable;
        internal TypeSpecTableReader TypeSpecTable;
        internal ImplMapTableReader ImplMapTable;
        internal FieldRVATableReader FieldRvaTable;
        internal EnCLogTableReader EncLogTable;
        internal EnCMapTableReader EncMapTable;
        internal AssemblyTableReader AssemblyTable;
        internal AssemblyProcessorTableReader AssemblyProcessorTable;              // unused
        internal AssemblyOSTableReader AssemblyOSTable;                            // unused
        internal AssemblyRefTableReader AssemblyRefTable;
        internal AssemblyRefProcessorTableReader AssemblyRefProcessorTable;        // unused
        internal AssemblyRefOSTableReader AssemblyRefOSTable;                      // unused
        internal FileTableReader FileTable;
        internal ExportedTypeTableReader ExportedTypeTable;
        internal ManifestResourceTableReader ManifestResourceTable;
        internal NestedClassTableReader NestedClassTable;
        internal GenericParamTableReader GenericParamTable;
        internal MethodSpecTableReader MethodSpecTable;
        internal GenericParamConstraintTableReader GenericParamConstraintTable;

        // debug tables
        internal DocumentTableReader DocumentTable;
        internal MethodDebugInformationTableReader MethodDebugInformationTable;
        internal LocalScopeTableReader LocalScopeTable;
        internal LocalVariableTableReader LocalVariableTable;
        internal LocalConstantTableReader LocalConstantTable;
        internal ImportScopeTableReader ImportScopeTable;
        internal StateMachineMethodTableReader StateMachineMethodTable;
        internal CustomDebugInformationTableReader CustomDebugInformationTable;

        private void ReadMetadataTableHeader(ref BlobReader reader, out HeapSizes heapSizes, out int[] metadataTableRowCounts, out TableMask sortedTables)
        {
            if (reader.RemainingBytes < MetadataStreamConstants.SizeOfMetadataTableHeader)
            {
                throw new BadImageFormatException(SR.MetadataTableHeaderTooSmall);
            }

            // reserved (shall be ignored):
            reader.ReadUInt32();

            // major version (shall be ignored):
            reader.ReadByte();

            // minor version (shall be ignored):
            reader.ReadByte();

            // heap sizes:
            heapSizes = (HeapSizes)reader.ReadByte();

            // reserved (shall be ignored):
            reader.ReadByte();

            ulong presentTables = reader.ReadUInt64();
            sortedTables = (TableMask)reader.ReadUInt64();

            // According to ECMA-335, MajorVersion and MinorVersion have fixed values and, 
            // based on recommendation in 24.1 Fixed fields: When writing these fields it 
            // is best that they be set to the value indicated, on reading they should be ignored.
            // We will not be checking version values. We will continue checking that the set of 
            // present tables is within the set we understand.

            ulong validTables = (ulong)(TableMask.TypeSystemTables | TableMask.DebugTables);

            if ((presentTables & ~validTables) != 0)
            {
                throw new BadImageFormatException(SR.Format(SR.UnknownTables, presentTables));
            }

            if (_metadataStreamKind == MetadataStreamKind.Compressed)
            {
                // In general Ptr tables and EnC tables are not allowed in a compressed stream.
                // However when asked for a snapshot of the current metadata after an EnC change has been applied 
                // the CLR includes the EnCLog table into the snapshot. We need to be able to read the image,
                // so we'll allow the table here but pretend it's empty later.
                if ((presentTables & (ulong)(TableMask.PtrTables | TableMask.EnCMap)) != 0)
                {
                    throw new BadImageFormatException(SR.IllegalTablesInCompressedMetadataStream);
                }
            }

            metadataTableRowCounts = ReadMetadataTableRowCounts(ref reader, presentTables);

            if ((heapSizes & HeapSizes.ExtraData) == HeapSizes.ExtraData)
            {
                // Skip "extra data" used by some obfuscators. Although it is not mentioned in the CLI spec,
                // it is honored by the native metadata reader.
                reader.ReadUInt32();
            }
        }

        private static int[] ReadMetadataTableRowCounts(ref BlobReader memReader, ulong presentTableMask)
        {
            ulong currentTableBit = 1;

            var rowCounts = new int[MetadataTokens.TableCount];
            for (int i = 0; i < rowCounts.Length; i++)
            {
                if ((presentTableMask & currentTableBit) != 0)
                {
                    if (memReader.RemainingBytes < sizeof(uint))
                    {
                        throw new BadImageFormatException(SR.TableRowCountSpaceTooSmall);
                    }

                    uint rowCount = memReader.ReadUInt32();
                    if (rowCount > TokenTypeIds.RIDMask)
                    {
                        throw new BadImageFormatException(SR.Format(SR.InvalidRowCount, rowCount));
                    }

                    rowCounts[i] = (int)rowCount;
                }

                currentTableBit <<= 1;
            }

            return rowCounts;
        }

        // internal for testing
        internal static void ReadStandalonePortablePdbStream(MemoryBlock pdbStreamBlock, int pdbStreamOffset, out DebugMetadataHeader debugMetadataHeader, out int[] externalTableRowCounts)
        {
            var reader = new BlobReader(pdbStreamBlock);

            const int PdbIdSize = 20;
            byte[] pdbId = reader.ReadBytes(PdbIdSize);

            // ECMA-335 15.4.1.2:
            // The entry point to an application shall be static.
            // This entry point method can be a global method or it can appear inside a type. 
            // The entry point method shall either accept no arguments or a vector of strings.
            // The return type of the entry point method shall be void, int32, or unsigned int32. 
            // The entry point method cannot be defined in a generic class.
            uint entryPointToken = reader.ReadUInt32();
            int entryPointRowId = (int)(entryPointToken & TokenTypeIds.RIDMask);
            if (entryPointToken != 0 && ((entryPointToken & TokenTypeIds.TypeMask) != TokenTypeIds.MethodDef || entryPointRowId == 0))
            {
                throw new BadImageFormatException(SR.Format(SR.InvalidEntryPointToken, entryPointToken));
            }

            ulong externalTableMask = reader.ReadUInt64();

            // EnC & Ptr tables can't be referenced from standalone PDB metadata:
            const ulong validTables = (ulong)TableMask.ValidPortablePdbExternalTables;

            if ((externalTableMask & ~validTables) != 0)
            {
                throw new BadImageFormatException(SR.Format(SR.UnknownTables, externalTableMask));
            }

            externalTableRowCounts = ReadMetadataTableRowCounts(ref reader, externalTableMask);

            debugMetadataHeader = new DebugMetadataHeader(
                ImmutableByteArrayInterop.DangerousCreateFromUnderlyingArray(ref pdbId),
                MethodDefinitionHandle.FromRowId(entryPointRowId),
                idStartOffset: pdbStreamOffset);
        }

        private const int SmallIndexSize = 2;
        private const int LargeIndexSize = 4;

        private int GetReferenceSize(int[] rowCounts, TableIndex index)
        {
            return (rowCounts[(int)index] < MetadataStreamConstants.LargeTableRowCount && !IsMinimalDelta) ? SmallIndexSize : LargeIndexSize;
        }

        private void InitializeTableReaders(MemoryBlock metadataTablesMemoryBlock, HeapSizes heapSizes, int[] rowCounts, int[] externalRowCountsOpt)
        {
            // Size of reference tags in each table.
            this.TableRowCounts = rowCounts;

            // TODO (https://github.com/dotnet/corefx/issues/2061): 
            // Shouldn't XxxPtr table be always the same size or smaller than the corresponding Xxx table?

            // Compute ref sizes for tables that can have pointer tables
            int fieldRefSizeSorted = GetReferenceSize(rowCounts, TableIndex.FieldPtr) > SmallIndexSize ? LargeIndexSize : GetReferenceSize(rowCounts, TableIndex.Field);
            int methodRefSizeSorted = GetReferenceSize(rowCounts, TableIndex.MethodPtr) > SmallIndexSize ? LargeIndexSize : GetReferenceSize(rowCounts, TableIndex.MethodDef);
            int paramRefSizeSorted = GetReferenceSize(rowCounts, TableIndex.ParamPtr) > SmallIndexSize ? LargeIndexSize : GetReferenceSize(rowCounts, TableIndex.Param);
            int eventRefSizeSorted = GetReferenceSize(rowCounts, TableIndex.EventPtr) > SmallIndexSize ? LargeIndexSize : GetReferenceSize(rowCounts, TableIndex.Event);
            int propertyRefSizeSorted = GetReferenceSize(rowCounts, TableIndex.PropertyPtr) > SmallIndexSize ? LargeIndexSize : GetReferenceSize(rowCounts, TableIndex.Property);

            // Compute the coded token ref sizes
            int typeDefOrRefRefSize = ComputeCodedTokenSize(TypeDefOrRefTag.LargeRowSize, rowCounts, TypeDefOrRefTag.TablesReferenced);
            int hasConstantRefSize = ComputeCodedTokenSize(HasConstantTag.LargeRowSize, rowCounts, HasConstantTag.TablesReferenced);
            int hasCustomAttributeRefSize = ComputeCodedTokenSize(HasCustomAttributeTag.LargeRowSize, rowCounts, HasCustomAttributeTag.TablesReferenced);
            int hasFieldMarshalRefSize = ComputeCodedTokenSize(HasFieldMarshalTag.LargeRowSize, rowCounts, HasFieldMarshalTag.TablesReferenced);
            int hasDeclSecurityRefSize = ComputeCodedTokenSize(HasDeclSecurityTag.LargeRowSize, rowCounts, HasDeclSecurityTag.TablesReferenced);
            int memberRefParentRefSize = ComputeCodedTokenSize(MemberRefParentTag.LargeRowSize, rowCounts, MemberRefParentTag.TablesReferenced);
            int hasSemanticsRefSize = ComputeCodedTokenSize(HasSemanticsTag.LargeRowSize, rowCounts, HasSemanticsTag.TablesReferenced);
            int methodDefOrRefRefSize = ComputeCodedTokenSize(MethodDefOrRefTag.LargeRowSize, rowCounts, MethodDefOrRefTag.TablesReferenced);
            int memberForwardedRefSize = ComputeCodedTokenSize(MemberForwardedTag.LargeRowSize, rowCounts, MemberForwardedTag.TablesReferenced);
            int implementationRefSize = ComputeCodedTokenSize(ImplementationTag.LargeRowSize, rowCounts, ImplementationTag.TablesReferenced);
            int customAttributeTypeRefSize = ComputeCodedTokenSize(CustomAttributeTypeTag.LargeRowSize, rowCounts, CustomAttributeTypeTag.TablesReferenced);
            int resolutionScopeRefSize = ComputeCodedTokenSize(ResolutionScopeTag.LargeRowSize, rowCounts, ResolutionScopeTag.TablesReferenced);
            int typeOrMethodDefRefSize = ComputeCodedTokenSize(TypeOrMethodDefTag.LargeRowSize, rowCounts, TypeOrMethodDefTag.TablesReferenced);

            // Compute HeapRef Sizes
            int stringHeapRefSize = (heapSizes & HeapSizes.StringHeapLarge) == HeapSizes.StringHeapLarge ? LargeIndexSize : SmallIndexSize;
            int guidHeapRefSize = (heapSizes & HeapSizes.GuidHeapLarge) == HeapSizes.GuidHeapLarge ? LargeIndexSize : SmallIndexSize;
            int blobHeapRefSize = (heapSizes & HeapSizes.BlobHeapLarge) == HeapSizes.BlobHeapLarge ? LargeIndexSize : SmallIndexSize;

            // Populate the Table blocks
            int totalRequiredSize = 0;
            this.ModuleTable = new ModuleTableReader(rowCounts[(int)TableIndex.Module], stringHeapRefSize, guidHeapRefSize, metadataTablesMemoryBlock, totalRequiredSize);
            totalRequiredSize += this.ModuleTable.Block.Length;

            this.TypeRefTable = new TypeRefTableReader(rowCounts[(int)TableIndex.TypeRef], resolutionScopeRefSize, stringHeapRefSize, metadataTablesMemoryBlock, totalRequiredSize);
            totalRequiredSize += this.TypeRefTable.Block.Length;

            this.TypeDefTable = new TypeDefTableReader(rowCounts[(int)TableIndex.TypeDef], fieldRefSizeSorted, methodRefSizeSorted, typeDefOrRefRefSize, stringHeapRefSize, metadataTablesMemoryBlock, totalRequiredSize);
            totalRequiredSize += this.TypeDefTable.Block.Length;

            this.FieldPtrTable = new FieldPtrTableReader(rowCounts[(int)TableIndex.FieldPtr], GetReferenceSize(rowCounts, TableIndex.Field), metadataTablesMemoryBlock, totalRequiredSize);
            totalRequiredSize += this.FieldPtrTable.Block.Length;

            this.FieldTable = new FieldTableReader(rowCounts[(int)TableIndex.Field], stringHeapRefSize, blobHeapRefSize, metadataTablesMemoryBlock, totalRequiredSize);
            totalRequiredSize += this.FieldTable.Block.Length;

            this.MethodPtrTable = new MethodPtrTableReader(rowCounts[(int)TableIndex.MethodPtr], GetReferenceSize(rowCounts, TableIndex.MethodDef), metadataTablesMemoryBlock, totalRequiredSize);
            totalRequiredSize += this.MethodPtrTable.Block.Length;

            this.MethodDefTable = new MethodTableReader(rowCounts[(int)TableIndex.MethodDef], paramRefSizeSorted, stringHeapRefSize, blobHeapRefSize, metadataTablesMemoryBlock, totalRequiredSize);
            totalRequiredSize += this.MethodDefTable.Block.Length;

            this.ParamPtrTable = new ParamPtrTableReader(rowCounts[(int)TableIndex.ParamPtr], GetReferenceSize(rowCounts, TableIndex.Param), metadataTablesMemoryBlock, totalRequiredSize);
            totalRequiredSize += this.ParamPtrTable.Block.Length;

            this.ParamTable = new ParamTableReader(rowCounts[(int)TableIndex.Param], stringHeapRefSize, metadataTablesMemoryBlock, totalRequiredSize);
            totalRequiredSize += this.ParamTable.Block.Length;

            this.InterfaceImplTable = new InterfaceImplTableReader(rowCounts[(int)TableIndex.InterfaceImpl], IsDeclaredSorted(TableMask.InterfaceImpl), GetReferenceSize(rowCounts, TableIndex.TypeDef), typeDefOrRefRefSize, metadataTablesMemoryBlock, totalRequiredSize);
            totalRequiredSize += this.InterfaceImplTable.Block.Length;

            this.MemberRefTable = new MemberRefTableReader(rowCounts[(int)TableIndex.MemberRef], memberRefParentRefSize, stringHeapRefSize, blobHeapRefSize, metadataTablesMemoryBlock, totalRequiredSize);
            totalRequiredSize += this.MemberRefTable.Block.Length;

            this.ConstantTable = new ConstantTableReader(rowCounts[(int)TableIndex.Constant], IsDeclaredSorted(TableMask.Constant), hasConstantRefSize, blobHeapRefSize, metadataTablesMemoryBlock, totalRequiredSize);
            totalRequiredSize += this.ConstantTable.Block.Length;

            this.CustomAttributeTable = new CustomAttributeTableReader(rowCounts[(int)TableIndex.CustomAttribute],
                                                                       IsDeclaredSorted(TableMask.CustomAttribute),
                                                                       hasCustomAttributeRefSize,
                                                                       customAttributeTypeRefSize,
                                                                       blobHeapRefSize,
                                                                       metadataTablesMemoryBlock,
                                                                       totalRequiredSize);
            totalRequiredSize += this.CustomAttributeTable.Block.Length;

            this.FieldMarshalTable = new FieldMarshalTableReader(rowCounts[(int)TableIndex.FieldMarshal], IsDeclaredSorted(TableMask.FieldMarshal), hasFieldMarshalRefSize, blobHeapRefSize, metadataTablesMemoryBlock, totalRequiredSize);
            totalRequiredSize += this.FieldMarshalTable.Block.Length;

            this.DeclSecurityTable = new DeclSecurityTableReader(rowCounts[(int)TableIndex.DeclSecurity], IsDeclaredSorted(TableMask.DeclSecurity), hasDeclSecurityRefSize, blobHeapRefSize, metadataTablesMemoryBlock, totalRequiredSize);
            totalRequiredSize += this.DeclSecurityTable.Block.Length;

            this.ClassLayoutTable = new ClassLayoutTableReader(rowCounts[(int)TableIndex.ClassLayout], IsDeclaredSorted(TableMask.ClassLayout), GetReferenceSize(rowCounts, TableIndex.TypeDef), metadataTablesMemoryBlock, totalRequiredSize);
            totalRequiredSize += this.ClassLayoutTable.Block.Length;

            this.FieldLayoutTable = new FieldLayoutTableReader(rowCounts[(int)TableIndex.FieldLayout], IsDeclaredSorted(TableMask.FieldLayout), GetReferenceSize(rowCounts, TableIndex.Field), metadataTablesMemoryBlock, totalRequiredSize);
            totalRequiredSize += this.FieldLayoutTable.Block.Length;

            this.StandAloneSigTable = new StandAloneSigTableReader(rowCounts[(int)TableIndex.StandAloneSig], blobHeapRefSize, metadataTablesMemoryBlock, totalRequiredSize);
            totalRequiredSize += this.StandAloneSigTable.Block.Length;

            this.EventMapTable = new EventMapTableReader(rowCounts[(int)TableIndex.EventMap], GetReferenceSize(rowCounts, TableIndex.TypeDef), eventRefSizeSorted, metadataTablesMemoryBlock, totalRequiredSize);
            totalRequiredSize += this.EventMapTable.Block.Length;

            this.EventPtrTable = new EventPtrTableReader(rowCounts[(int)TableIndex.EventPtr], GetReferenceSize(rowCounts, TableIndex.Event), metadataTablesMemoryBlock, totalRequiredSize);
            totalRequiredSize += this.EventPtrTable.Block.Length;

            this.EventTable = new EventTableReader(rowCounts[(int)TableIndex.Event], typeDefOrRefRefSize, stringHeapRefSize, metadataTablesMemoryBlock, totalRequiredSize);
            totalRequiredSize += this.EventTable.Block.Length;

            this.PropertyMapTable = new PropertyMapTableReader(rowCounts[(int)TableIndex.PropertyMap], GetReferenceSize(rowCounts, TableIndex.TypeDef), propertyRefSizeSorted, metadataTablesMemoryBlock, totalRequiredSize);
            totalRequiredSize += this.PropertyMapTable.Block.Length;

            this.PropertyPtrTable = new PropertyPtrTableReader(rowCounts[(int)TableIndex.PropertyPtr], GetReferenceSize(rowCounts, TableIndex.Property), metadataTablesMemoryBlock, totalRequiredSize);
            totalRequiredSize += this.PropertyPtrTable.Block.Length;

            this.PropertyTable = new PropertyTableReader(rowCounts[(int)TableIndex.Property], stringHeapRefSize, blobHeapRefSize, metadataTablesMemoryBlock, totalRequiredSize);
            totalRequiredSize += this.PropertyTable.Block.Length;

            this.MethodSemanticsTable = new MethodSemanticsTableReader(rowCounts[(int)TableIndex.MethodSemantics], IsDeclaredSorted(TableMask.MethodSemantics), GetReferenceSize(rowCounts, TableIndex.MethodDef), hasSemanticsRefSize, metadataTablesMemoryBlock, totalRequiredSize);
            totalRequiredSize += this.MethodSemanticsTable.Block.Length;

            this.MethodImplTable = new MethodImplTableReader(rowCounts[(int)TableIndex.MethodImpl], IsDeclaredSorted(TableMask.MethodImpl), GetReferenceSize(rowCounts, TableIndex.TypeDef), methodDefOrRefRefSize, metadataTablesMemoryBlock, totalRequiredSize);
            totalRequiredSize += this.MethodImplTable.Block.Length;

            this.ModuleRefTable = new ModuleRefTableReader(rowCounts[(int)TableIndex.ModuleRef], stringHeapRefSize, metadataTablesMemoryBlock, totalRequiredSize);
            totalRequiredSize += this.ModuleRefTable.Block.Length;

            this.TypeSpecTable = new TypeSpecTableReader(rowCounts[(int)TableIndex.TypeSpec], blobHeapRefSize, metadataTablesMemoryBlock, totalRequiredSize);
            totalRequiredSize += this.TypeSpecTable.Block.Length;

            this.ImplMapTable = new ImplMapTableReader(rowCounts[(int)TableIndex.ImplMap], IsDeclaredSorted(TableMask.ImplMap), GetReferenceSize(rowCounts, TableIndex.ModuleRef), memberForwardedRefSize, stringHeapRefSize, metadataTablesMemoryBlock, totalRequiredSize);
            totalRequiredSize += this.ImplMapTable.Block.Length;

            this.FieldRvaTable = new FieldRVATableReader(rowCounts[(int)TableIndex.FieldRva], IsDeclaredSorted(TableMask.FieldRva), GetReferenceSize(rowCounts, TableIndex.Field), metadataTablesMemoryBlock, totalRequiredSize);
            totalRequiredSize += this.FieldRvaTable.Block.Length;

            this.EncLogTable = new EnCLogTableReader(rowCounts[(int)TableIndex.EncLog], metadataTablesMemoryBlock, totalRequiredSize, _metadataStreamKind);
            totalRequiredSize += this.EncLogTable.Block.Length;

            this.EncMapTable = new EnCMapTableReader(rowCounts[(int)TableIndex.EncMap], metadataTablesMemoryBlock, totalRequiredSize);
            totalRequiredSize += this.EncMapTable.Block.Length;

            this.AssemblyTable = new AssemblyTableReader(rowCounts[(int)TableIndex.Assembly], stringHeapRefSize, blobHeapRefSize, metadataTablesMemoryBlock, totalRequiredSize);
            totalRequiredSize += this.AssemblyTable.Block.Length;

            this.AssemblyProcessorTable = new AssemblyProcessorTableReader(rowCounts[(int)TableIndex.AssemblyProcessor], metadataTablesMemoryBlock, totalRequiredSize);
            totalRequiredSize += this.AssemblyProcessorTable.Block.Length;

            this.AssemblyOSTable = new AssemblyOSTableReader(rowCounts[(int)TableIndex.AssemblyOS], metadataTablesMemoryBlock, totalRequiredSize);
            totalRequiredSize += this.AssemblyOSTable.Block.Length;

            this.AssemblyRefTable = new AssemblyRefTableReader(rowCounts[(int)TableIndex.AssemblyRef], stringHeapRefSize, blobHeapRefSize, metadataTablesMemoryBlock, totalRequiredSize, _metadataKind);
            totalRequiredSize += this.AssemblyRefTable.Block.Length;

            this.AssemblyRefProcessorTable = new AssemblyRefProcessorTableReader(rowCounts[(int)TableIndex.AssemblyRefProcessor], GetReferenceSize(rowCounts, TableIndex.AssemblyRef), metadataTablesMemoryBlock, totalRequiredSize);
            totalRequiredSize += this.AssemblyRefProcessorTable.Block.Length;

            this.AssemblyRefOSTable = new AssemblyRefOSTableReader(rowCounts[(int)TableIndex.AssemblyRefOS], GetReferenceSize(rowCounts, TableIndex.AssemblyRef), metadataTablesMemoryBlock, totalRequiredSize);
            totalRequiredSize += this.AssemblyRefOSTable.Block.Length;

            this.FileTable = new FileTableReader(rowCounts[(int)TableIndex.File], stringHeapRefSize, blobHeapRefSize, metadataTablesMemoryBlock, totalRequiredSize);
            totalRequiredSize += this.FileTable.Block.Length;

            this.ExportedTypeTable = new ExportedTypeTableReader(rowCounts[(int)TableIndex.ExportedType], implementationRefSize, stringHeapRefSize, metadataTablesMemoryBlock, totalRequiredSize);
            totalRequiredSize += this.ExportedTypeTable.Block.Length;

            this.ManifestResourceTable = new ManifestResourceTableReader(rowCounts[(int)TableIndex.ManifestResource], implementationRefSize, stringHeapRefSize, metadataTablesMemoryBlock, totalRequiredSize);
            totalRequiredSize += this.ManifestResourceTable.Block.Length;

            this.NestedClassTable = new NestedClassTableReader(rowCounts[(int)TableIndex.NestedClass], IsDeclaredSorted(TableMask.NestedClass), GetReferenceSize(rowCounts, TableIndex.TypeDef), metadataTablesMemoryBlock, totalRequiredSize);
            totalRequiredSize += this.NestedClassTable.Block.Length;

            this.GenericParamTable = new GenericParamTableReader(rowCounts[(int)TableIndex.GenericParam], IsDeclaredSorted(TableMask.GenericParam), typeOrMethodDefRefSize, stringHeapRefSize, metadataTablesMemoryBlock, totalRequiredSize);
            totalRequiredSize += this.GenericParamTable.Block.Length;

            this.MethodSpecTable = new MethodSpecTableReader(rowCounts[(int)TableIndex.MethodSpec], methodDefOrRefRefSize, blobHeapRefSize, metadataTablesMemoryBlock, totalRequiredSize);
            totalRequiredSize += this.MethodSpecTable.Block.Length;

            this.GenericParamConstraintTable = new GenericParamConstraintTableReader(rowCounts[(int)TableIndex.GenericParamConstraint], IsDeclaredSorted(TableMask.GenericParamConstraint), GetReferenceSize(rowCounts, TableIndex.GenericParam), typeDefOrRefRefSize, metadataTablesMemoryBlock, totalRequiredSize);
            totalRequiredSize += this.GenericParamConstraintTable.Block.Length;

            // debug tables:
            // Type-system metadata tables may be stored in a separate (external) metadata file.
            // We need to use the row counts of the external tables when referencing them.
            // Debug tables are local to the current metadata image and type system metadata tables are external and precede all debug tables.
            var combinedRowCounts = (externalRowCountsOpt != null) ? CombineRowCounts(rowCounts, externalRowCountsOpt, firstLocalTableIndex: TableIndex.Document) : rowCounts;

            int methodRefSizeCombined = GetReferenceSize(combinedRowCounts, TableIndex.MethodDef);
            int hasCustomDebugInformationRefSizeCombined = ComputeCodedTokenSize(HasCustomDebugInformationTag.LargeRowSize, combinedRowCounts, HasCustomDebugInformationTag.TablesReferenced);

            this.DocumentTable = new DocumentTableReader(rowCounts[(int)TableIndex.Document], guidHeapRefSize, blobHeapRefSize, metadataTablesMemoryBlock, totalRequiredSize);
            totalRequiredSize += this.DocumentTable.Block.Length;

            this.MethodDebugInformationTable = new MethodDebugInformationTableReader(rowCounts[(int)TableIndex.MethodDebugInformation], GetReferenceSize(rowCounts, TableIndex.Document), blobHeapRefSize, metadataTablesMemoryBlock, totalRequiredSize);
            totalRequiredSize += this.MethodDebugInformationTable.Block.Length;

            this.LocalScopeTable = new LocalScopeTableReader(rowCounts[(int)TableIndex.LocalScope], IsDeclaredSorted(TableMask.LocalScope), methodRefSizeCombined, GetReferenceSize(rowCounts, TableIndex.ImportScope), GetReferenceSize(rowCounts, TableIndex.LocalVariable), GetReferenceSize(rowCounts, TableIndex.LocalConstant), metadataTablesMemoryBlock, totalRequiredSize);
            totalRequiredSize += this.LocalScopeTable.Block.Length;

            this.LocalVariableTable = new LocalVariableTableReader(rowCounts[(int)TableIndex.LocalVariable], stringHeapRefSize, metadataTablesMemoryBlock, totalRequiredSize);
            totalRequiredSize += this.LocalVariableTable.Block.Length;

            this.LocalConstantTable = new LocalConstantTableReader(rowCounts[(int)TableIndex.LocalConstant], stringHeapRefSize, blobHeapRefSize, metadataTablesMemoryBlock, totalRequiredSize);
            totalRequiredSize += this.LocalConstantTable.Block.Length;

            this.ImportScopeTable = new ImportScopeTableReader(rowCounts[(int)TableIndex.ImportScope], GetReferenceSize(rowCounts, TableIndex.ImportScope), blobHeapRefSize, metadataTablesMemoryBlock, totalRequiredSize);
            totalRequiredSize += this.ImportScopeTable.Block.Length;

            this.StateMachineMethodTable = new StateMachineMethodTableReader(rowCounts[(int)TableIndex.StateMachineMethod], IsDeclaredSorted(TableMask.StateMachineMethod), methodRefSizeCombined, metadataTablesMemoryBlock, totalRequiredSize);
            totalRequiredSize += this.StateMachineMethodTable.Block.Length;

            this.CustomDebugInformationTable = new CustomDebugInformationTableReader(rowCounts[(int)TableIndex.CustomDebugInformation], IsDeclaredSorted(TableMask.CustomDebugInformation), hasCustomDebugInformationRefSizeCombined, guidHeapRefSize, blobHeapRefSize, metadataTablesMemoryBlock, totalRequiredSize);
            totalRequiredSize += this.CustomDebugInformationTable.Block.Length;

            if (totalRequiredSize > metadataTablesMemoryBlock.Length)
            {
                throw new BadImageFormatException(SR.MetadataTablesTooSmall);
            }
        }

        private static int[] CombineRowCounts(int[] local, int[] external, TableIndex firstLocalTableIndex)
        {
            Debug.Assert(local.Length == external.Length);

            var rowCounts = new int[local.Length];
            for (int i = 0; i < (int)firstLocalTableIndex; i++)
            {
                rowCounts[i] = external[i];
            }

            for (int i = (int)firstLocalTableIndex; i < rowCounts.Length; i++)
            {
                rowCounts[i] = local[i];
            }

            return rowCounts;
        }

        private int ComputeCodedTokenSize(int largeRowSize, int[] rowCounts, TableMask tablesReferenced)
        {
            if (IsMinimalDelta)
            {
                return LargeIndexSize;
            }

            bool isAllReferencedTablesSmall = true;
            ulong tablesReferencedMask = (ulong)tablesReferenced;
            for (int tableIndex = 0; tableIndex < MetadataTokens.TableCount; tableIndex++)
            {
                if ((tablesReferencedMask & 1) != 0)
                {
                    isAllReferencedTablesSmall = isAllReferencedTablesSmall && (rowCounts[tableIndex] < largeRowSize);
                }

                tablesReferencedMask >>= 1;
            }

            return isAllReferencedTablesSmall ? SmallIndexSize : LargeIndexSize;
        }

        private bool IsDeclaredSorted(TableMask index)
        {
            return (_sortedTables & index) != 0;
        }

        #endregion

        #region Helpers

        internal bool UseFieldPtrTable => FieldPtrTable.NumberOfRows > 0;
        internal bool UseMethodPtrTable => MethodPtrTable.NumberOfRows > 0;
        internal bool UseParamPtrTable => ParamPtrTable.NumberOfRows > 0;
        internal bool UseEventPtrTable => EventPtrTable.NumberOfRows > 0;
        internal bool UsePropertyPtrTable => PropertyPtrTable.NumberOfRows > 0;

        internal void GetFieldRange(TypeDefinitionHandle typeDef, out int firstFieldRowId, out int lastFieldRowId)
        {
            int typeDefRowId = typeDef.RowId;

            firstFieldRowId = this.TypeDefTable.GetFieldStart(typeDefRowId);
            if (firstFieldRowId == 0)
            {
                firstFieldRowId = 1;
                lastFieldRowId = 0;
            }
            else if (typeDefRowId == this.TypeDefTable.NumberOfRows)
            {
                lastFieldRowId = (this.UseFieldPtrTable) ? this.FieldPtrTable.NumberOfRows : this.FieldTable.NumberOfRows;
            }
            else
            {
                lastFieldRowId = this.TypeDefTable.GetFieldStart(typeDefRowId + 1) - 1;
            }
        }

        internal void GetMethodRange(TypeDefinitionHandle typeDef, out int firstMethodRowId, out int lastMethodRowId)
        {
            int typeDefRowId = typeDef.RowId;
            firstMethodRowId = this.TypeDefTable.GetMethodStart(typeDefRowId);
            if (firstMethodRowId == 0)
            {
                firstMethodRowId = 1;
                lastMethodRowId = 0;
            }
            else if (typeDefRowId == this.TypeDefTable.NumberOfRows)
            {
                lastMethodRowId = (this.UseMethodPtrTable) ? this.MethodPtrTable.NumberOfRows : this.MethodDefTable.NumberOfRows;
            }
            else
            {
                lastMethodRowId = this.TypeDefTable.GetMethodStart(typeDefRowId + 1) - 1;
            }
        }

        internal void GetEventRange(TypeDefinitionHandle typeDef, out int firstEventRowId, out int lastEventRowId)
        {
            int eventMapRowId = this.EventMapTable.FindEventMapRowIdFor(typeDef);
            if (eventMapRowId == 0)
            {
                firstEventRowId = 1;
                lastEventRowId = 0;
                return;
            }

            firstEventRowId = this.EventMapTable.GetEventListStartFor(eventMapRowId);
            if (eventMapRowId == this.EventMapTable.NumberOfRows)
            {
                lastEventRowId = this.UseEventPtrTable ? this.EventPtrTable.NumberOfRows : this.EventTable.NumberOfRows;
            }
            else
            {
                lastEventRowId = this.EventMapTable.GetEventListStartFor(eventMapRowId + 1) - 1;
            }
        }

        internal void GetPropertyRange(TypeDefinitionHandle typeDef, out int firstPropertyRowId, out int lastPropertyRowId)
        {
            int propertyMapRowId = this.PropertyMapTable.FindPropertyMapRowIdFor(typeDef);
            if (propertyMapRowId == 0)
            {
                firstPropertyRowId = 1;
                lastPropertyRowId = 0;
                return;
            }

            firstPropertyRowId = this.PropertyMapTable.GetPropertyListStartFor(propertyMapRowId);
            if (propertyMapRowId == this.PropertyMapTable.NumberOfRows)
            {
                lastPropertyRowId = (this.UsePropertyPtrTable) ? this.PropertyPtrTable.NumberOfRows : this.PropertyTable.NumberOfRows;
            }
            else
            {
                lastPropertyRowId = this.PropertyMapTable.GetPropertyListStartFor(propertyMapRowId + 1) - 1;
            }
        }

        internal void GetParameterRange(MethodDefinitionHandle methodDef, out int firstParamRowId, out int lastParamRowId)
        {
            int rid = methodDef.RowId;

            firstParamRowId = this.MethodDefTable.GetParamStart(rid);
            if (firstParamRowId == 0)
            {
                firstParamRowId = 1;
                lastParamRowId = 0;
            }
            else if (rid == this.MethodDefTable.NumberOfRows)
            {
                lastParamRowId = (this.UseParamPtrTable ? this.ParamPtrTable.NumberOfRows : this.ParamTable.NumberOfRows);
            }
            else
            {
                lastParamRowId = this.MethodDefTable.GetParamStart(rid + 1) - 1;
            }
        }

        internal void GetLocalVariableRange(LocalScopeHandle scope, out int firstVariableRowId, out int lastVariableRowId)
        {
            int scopeRowId = scope.RowId;

            firstVariableRowId = this.LocalScopeTable.GetVariableStart(scopeRowId);
            if (firstVariableRowId == 0)
            {
                firstVariableRowId = 1;
                lastVariableRowId = 0;
            }
            else if (scopeRowId == this.LocalScopeTable.NumberOfRows)
            {
                lastVariableRowId = this.LocalVariableTable.NumberOfRows;
            }
            else
            {
                lastVariableRowId = this.LocalScopeTable.GetVariableStart(scopeRowId + 1) - 1;
            }
        }

        internal void GetLocalConstantRange(LocalScopeHandle scope, out int firstConstantRowId, out int lastConstantRowId)
        {
            int scopeRowId = scope.RowId;

            firstConstantRowId = this.LocalScopeTable.GetConstantStart(scopeRowId);
            if (firstConstantRowId == 0)
            {
                firstConstantRowId = 1;
                lastConstantRowId = 0;
            }
            else if (scopeRowId == this.LocalScopeTable.NumberOfRows)
            {
                lastConstantRowId = this.LocalConstantTable.NumberOfRows;
            }
            else
            {
                lastConstantRowId = this.LocalScopeTable.GetConstantStart(scopeRowId + 1) - 1;
            }
        }

        #endregion

        #region Public APIs

        /// <summary>
        /// Pointer to the underlying data.
        /// </summary>
        public unsafe byte* MetadataPointer => Block.Pointer;

        /// <summary>
        /// Length of the underlying data.
        /// </summary>
        public int MetadataLength => Block.Length;

        /// <summary>
        /// Options passed to the constructor.
        /// </summary>
        public MetadataReaderOptions Options => _options;

        /// <summary>
        /// Version string read from metadata header.
        /// </summary>
        public string MetadataVersion => _versionString;

        /// <summary>
        /// Information decoded from #Pdb stream, or null if the stream is not present.
        /// </summary>
        public DebugMetadataHeader DebugMetadataHeader => _debugMetadataHeader;

        /// <summary>
        /// The kind of the metadata (plain ECMA335, WinMD, etc.).
        /// </summary>
        public MetadataKind MetadataKind => _metadataKind;

        /// <summary>
        /// Comparer used to compare strings stored in metadata.
        /// </summary>
        public MetadataStringComparer StringComparer => new MetadataStringComparer(this);

        /// <summary>
        /// The decoder used by the reader to produce <see cref="string"/> instances from UTF8 encoded byte sequences.
        /// </summary>
        public MetadataStringDecoder UTF8Decoder { get; }

        /// <summary>
        /// Returns true if the metadata represent an assembly.
        /// </summary>
        public bool IsAssembly => AssemblyTable.NumberOfRows == 1;

        public AssemblyReferenceHandleCollection AssemblyReferences => new AssemblyReferenceHandleCollection(this);
        public TypeDefinitionHandleCollection TypeDefinitions => new TypeDefinitionHandleCollection(TypeDefTable.NumberOfRows);
        public TypeReferenceHandleCollection TypeReferences => new TypeReferenceHandleCollection(TypeRefTable.NumberOfRows);
        public CustomAttributeHandleCollection CustomAttributes => new CustomAttributeHandleCollection(this);
        public DeclarativeSecurityAttributeHandleCollection DeclarativeSecurityAttributes => new DeclarativeSecurityAttributeHandleCollection(this);
        public MemberReferenceHandleCollection MemberReferences => new MemberReferenceHandleCollection(MemberRefTable.NumberOfRows);
        public ManifestResourceHandleCollection ManifestResources => new ManifestResourceHandleCollection(ManifestResourceTable.NumberOfRows);
        public AssemblyFileHandleCollection AssemblyFiles => new AssemblyFileHandleCollection(FileTable.NumberOfRows);
        public ExportedTypeHandleCollection ExportedTypes => new ExportedTypeHandleCollection(ExportedTypeTable.NumberOfRows);
        public MethodDefinitionHandleCollection MethodDefinitions => new MethodDefinitionHandleCollection(this);
        public FieldDefinitionHandleCollection FieldDefinitions => new FieldDefinitionHandleCollection(this);
        public EventDefinitionHandleCollection EventDefinitions => new EventDefinitionHandleCollection(this);
        public PropertyDefinitionHandleCollection PropertyDefinitions => new PropertyDefinitionHandleCollection(this);
        public DocumentHandleCollection Documents => new DocumentHandleCollection(this);
        public MethodDebugInformationHandleCollection MethodDebugInformation => new MethodDebugInformationHandleCollection(this);
        public LocalScopeHandleCollection LocalScopes => new LocalScopeHandleCollection(this, 0);
        public LocalVariableHandleCollection LocalVariables => new LocalVariableHandleCollection(this, default(LocalScopeHandle));
        public LocalConstantHandleCollection LocalConstants => new LocalConstantHandleCollection(this, default(LocalScopeHandle));
        public ImportScopeCollection ImportScopes => new ImportScopeCollection(this);
        public CustomDebugInformationHandleCollection CustomDebugInformation => new CustomDebugInformationHandleCollection(this);

        public AssemblyDefinition GetAssemblyDefinition()
        {
            if (!IsAssembly)
            {
                throw new InvalidOperationException(SR.MetadataImageDoesNotRepresentAnAssembly);
            }

            return new AssemblyDefinition(this);
        }

        public string GetString(StringHandle handle)
        {
            return StringHeap.GetString(handle, UTF8Decoder);
        }

        public string GetString(NamespaceDefinitionHandle handle)
        {
            if (handle.HasFullName)
            {
                return StringHeap.GetString(handle.GetFullName(), UTF8Decoder);
            }

            return NamespaceCache.GetFullName(handle);
        }

        public byte[] GetBlobBytes(BlobHandle handle)
        {
            return BlobHeap.GetBytes(handle);
        }

        public ImmutableArray<byte> GetBlobContent(BlobHandle handle)
        {
            // TODO: We can skip a copy for virtual blobs.
            byte[] bytes = GetBlobBytes(handle);
            return ImmutableByteArrayInterop.DangerousCreateFromUnderlyingArray(ref bytes);
        }

        public BlobReader GetBlobReader(BlobHandle handle)
        {
            return BlobHeap.GetBlobReader(handle);
        }

        public BlobReader GetBlobReader(StringHandle handle)
        {
            return StringHeap.GetBlobReader(handle);
        }

        public string GetUserString(UserStringHandle handle)
        {
            return UserStringHeap.GetString(handle);
        }

        public Guid GetGuid(GuidHandle handle)
        {
            return GuidHeap.GetGuid(handle);
        }

        public ModuleDefinition GetModuleDefinition()
        {
            if (_debugMetadataHeader != null)
            {
                throw new InvalidOperationException(SR.StandaloneDebugMetadataImageDoesNotContainModuleTable);
            }

            return new ModuleDefinition(this);
        }

        public AssemblyReference GetAssemblyReference(AssemblyReferenceHandle handle)
        {
            return new AssemblyReference(this, handle.Value);
        }

        public TypeDefinition GetTypeDefinition(TypeDefinitionHandle handle)
        {
            // PERF: This code pattern is JIT friendly and results in very efficient code.
            return new TypeDefinition(this, GetTypeDefTreatmentAndRowId(handle));
        }

        public NamespaceDefinition GetNamespaceDefinitionRoot()
        {
            NamespaceData data = NamespaceCache.GetRootNamespace();
            return new NamespaceDefinition(data);
        }

        public NamespaceDefinition GetNamespaceDefinition(NamespaceDefinitionHandle handle)
        {
            NamespaceData data = NamespaceCache.GetNamespaceData(handle);
            return new NamespaceDefinition(data);
        }

        private uint GetTypeDefTreatmentAndRowId(TypeDefinitionHandle handle)
        {
            // PERF: This code pattern is JIT friendly and results in very efficient code.
            if (_metadataKind == MetadataKind.Ecma335)
            {
                return (uint)handle.RowId;
            }

            return CalculateTypeDefTreatmentAndRowId(handle);
        }

        public TypeReference GetTypeReference(TypeReferenceHandle handle)
        {
            // PERF: This code pattern is JIT friendly and results in very efficient code.
            return new TypeReference(this, GetTypeRefTreatmentAndRowId(handle));
        }

        private uint GetTypeRefTreatmentAndRowId(TypeReferenceHandle handle)
        {
            // PERF: This code pattern is JIT friendly and results in very efficient code.
            if (_metadataKind == MetadataKind.Ecma335)
            {
                return (uint)handle.RowId;
            }

            return CalculateTypeRefTreatmentAndRowId(handle);
        }

        public ExportedType GetExportedType(ExportedTypeHandle handle)
        {
            return new ExportedType(this, handle.RowId);
        }

        public CustomAttributeHandleCollection GetCustomAttributes(EntityHandle handle)
        {
            return new CustomAttributeHandleCollection(this, handle);
        }

        public CustomAttribute GetCustomAttribute(CustomAttributeHandle handle)
        {
            // PERF: This code pattern is JIT friendly and results in very efficient code.
            return new CustomAttribute(this, GetCustomAttributeTreatmentAndRowId(handle));
        }

        private uint GetCustomAttributeTreatmentAndRowId(CustomAttributeHandle handle)
        {
            // PERF: This code pattern is JIT friendly and results in very efficient code.
            if (_metadataKind == MetadataKind.Ecma335)
            {
                return (uint)handle.RowId;
            }

            return TreatmentAndRowId((byte)CustomAttributeTreatment.WinMD, handle.RowId);
        }

        public DeclarativeSecurityAttribute GetDeclarativeSecurityAttribute(DeclarativeSecurityAttributeHandle handle)
        {
            // PERF: This code pattern is JIT friendly and results in very efficient code.
            return new DeclarativeSecurityAttribute(this, handle.RowId);
        }

        public Constant GetConstant(ConstantHandle handle)
        {
            return new Constant(this, handle.RowId);
        }

        public MethodDefinition GetMethodDefinition(MethodDefinitionHandle handle)
        {
            // PERF: This code pattern is JIT friendly and results in very efficient code.
            return new MethodDefinition(this, GetMethodDefTreatmentAndRowId(handle));
        }

        private uint GetMethodDefTreatmentAndRowId(MethodDefinitionHandle handle)
        {
            // PERF: This code pattern is JIT friendly and results in very efficient code.
            if (_metadataKind == MetadataKind.Ecma335)
            {
                return (uint)handle.RowId;
            }

            return CalculateMethodDefTreatmentAndRowId(handle);
        }

        public FieldDefinition GetFieldDefinition(FieldDefinitionHandle handle)
        {
            // PERF: This code pattern is JIT friendly and results in very efficient code.
            return new FieldDefinition(this, GetFieldDefTreatmentAndRowId(handle));
        }

        private uint GetFieldDefTreatmentAndRowId(FieldDefinitionHandle handle)
        {
            // PERF: This code pattern is JIT friendly and results in very efficient code.
            if (_metadataKind == MetadataKind.Ecma335)
            {
                return (uint)handle.RowId;
            }

            return CalculateFieldDefTreatmentAndRowId(handle);
        }

        public PropertyDefinition GetPropertyDefinition(PropertyDefinitionHandle handle)
        {
            return new PropertyDefinition(this, handle);
        }

        public EventDefinition GetEventDefinition(EventDefinitionHandle handle)
        {
            return new EventDefinition(this, handle);
        }

        public MethodImplementation GetMethodImplementation(MethodImplementationHandle handle)
        {
            return new MethodImplementation(this, handle);
        }

        public MemberReference GetMemberReference(MemberReferenceHandle handle)
        {
            // PERF: This code pattern is JIT friendly and results in very efficient code.
            return new MemberReference(this, GetMemberRefTreatmentAndRowId(handle));
        }

        private uint GetMemberRefTreatmentAndRowId(MemberReferenceHandle handle)
        {
            // PERF: This code pattern is JIT friendly and results in very efficient code.
            if (_metadataKind == MetadataKind.Ecma335)
            {
                return (uint)handle.RowId;
            }

            return CalculateMemberRefTreatmentAndRowId(handle);
        }

        public MethodSpecification GetMethodSpecification(MethodSpecificationHandle handle)
        {
            return new MethodSpecification(this, handle);
        }

        public Parameter GetParameter(ParameterHandle handle)
        {
            return new Parameter(this, handle);
        }

        public GenericParameter GetGenericParameter(GenericParameterHandle handle)
        {
            return new GenericParameter(this, handle);
        }

        public GenericParameterConstraint GetGenericParameterConstraint(GenericParameterConstraintHandle handle)
        {
            return new GenericParameterConstraint(this, handle);
        }

        public ManifestResource GetManifestResource(ManifestResourceHandle handle)
        {
            return new ManifestResource(this, handle);
        }

        public AssemblyFile GetAssemblyFile(AssemblyFileHandle handle)
        {
            return new AssemblyFile(this, handle);
        }

        public StandaloneSignature GetStandaloneSignature(StandaloneSignatureHandle handle)
        {
            return new StandaloneSignature(this, handle);
        }

        public TypeSpecification GetTypeSpecification(TypeSpecificationHandle handle)
        {
            return new TypeSpecification(this, handle);
        }

        public ModuleReference GetModuleReference(ModuleReferenceHandle handle)
        {
            return new ModuleReference(this, handle);
        }

        public InterfaceImplementation GetInterfaceImplementation(InterfaceImplementationHandle handle)
        {
            return new InterfaceImplementation(this, handle);
        }

        internal TypeDefinitionHandle GetDeclaringType(MethodDefinitionHandle methodDef)
        {
            int methodRowId;
            if (UseMethodPtrTable)
            {
                methodRowId = MethodPtrTable.GetRowIdForMethodDefRow(methodDef.RowId);
            }
            else
            {
                methodRowId = methodDef.RowId;
            }

            return TypeDefTable.FindTypeContainingMethod(methodRowId, MethodDefTable.NumberOfRows);
        }

        internal TypeDefinitionHandle GetDeclaringType(FieldDefinitionHandle fieldDef)
        {
            int fieldRowId;
            if (UseFieldPtrTable)
            {
                fieldRowId = FieldPtrTable.GetRowIdForFieldDefRow(fieldDef.RowId);
            }
            else
            {
                fieldRowId = fieldDef.RowId;
            }

            return TypeDefTable.FindTypeContainingField(fieldRowId, FieldTable.NumberOfRows);
        }

        private static readonly ObjectPool<StringBuilder> s_stringBuilderPool = new ObjectPool<StringBuilder>(() => new StringBuilder());

        public string GetString(DocumentNameBlobHandle handle)
        {
            return BlobHeap.GetDocumentName(handle);
        }

        public Document GetDocument(DocumentHandle handle)
        {
            return new Document(this, handle);
        }

        public MethodDebugInformation GetMethodDebugInformation(MethodDebugInformationHandle handle)
        {
            return new MethodDebugInformation(this, handle);
        }

        public MethodDebugInformation GetMethodDebugInformation(MethodDefinitionHandle handle)
        {
            return new MethodDebugInformation(this, MethodDebugInformationHandle.FromRowId(handle.RowId));
        }

        public LocalScope GetLocalScope(LocalScopeHandle handle)
        {
            return new LocalScope(this, handle);
        }

        public LocalVariable GetLocalVariable(LocalVariableHandle handle)
        {
            return new LocalVariable(this, handle);
        }

        public LocalConstant GetLocalConstant(LocalConstantHandle handle)
        {
            return new LocalConstant(this, handle);
        }

        public ImportScope GetImportScope(ImportScopeHandle handle)
        {
            return new ImportScope(this, handle);
        }

        public CustomDebugInformation GetCustomDebugInformation(CustomDebugInformationHandle handle)
        {
            return new CustomDebugInformation(this, handle);
        }

        public CustomDebugInformationHandleCollection GetCustomDebugInformation(EntityHandle handle)
        {
            return new CustomDebugInformationHandleCollection(this, handle);
        }

        public LocalScopeHandleCollection GetLocalScopes(MethodDefinitionHandle handle)
        {
            return new LocalScopeHandleCollection(this, handle.RowId);
        }

        public LocalScopeHandleCollection GetLocalScopes(MethodDebugInformationHandle handle)
        {
            return new LocalScopeHandleCollection(this, handle.RowId);
        }

        #endregion

        #region Nested Types

        private void InitializeNestedTypesMap()
        {
            var groupedNestedTypes = new Dictionary<TypeDefinitionHandle, ImmutableArray<TypeDefinitionHandle>.Builder>();

            int numberOfNestedTypes = NestedClassTable.NumberOfRows;
            ImmutableArray<TypeDefinitionHandle>.Builder builder = null;
            TypeDefinitionHandle previousEnclosingClass = default(TypeDefinitionHandle);

            for (int i = 1; i <= numberOfNestedTypes; i++)
            {
                TypeDefinitionHandle enclosingClass = NestedClassTable.GetEnclosingClass(i);

                Debug.Assert(!enclosingClass.IsNil);

                if (enclosingClass != previousEnclosingClass)
                {
                    if (!groupedNestedTypes.TryGetValue(enclosingClass, out builder))
                    {
                        builder = ImmutableArray.CreateBuilder<TypeDefinitionHandle>();
                        groupedNestedTypes.Add(enclosingClass, builder);
                    }

                    previousEnclosingClass = enclosingClass;
                }
                else
                {
                    Debug.Assert(builder == groupedNestedTypes[enclosingClass]);
                }

                builder.Add(NestedClassTable.GetNestedClass(i));
            }

            var nestedTypesMap = new Dictionary<TypeDefinitionHandle, ImmutableArray<TypeDefinitionHandle>>();
            foreach (var group in groupedNestedTypes)
            {
                nestedTypesMap.Add(group.Key, group.Value.ToImmutable());
            }

            _lazyNestedTypesMap = nestedTypesMap;
        }

        /// <summary>
        /// Returns an array of types nested in the specified type.
        /// </summary>
        internal ImmutableArray<TypeDefinitionHandle> GetNestedTypes(TypeDefinitionHandle typeDef)
        {
            if (_lazyNestedTypesMap == null)
            {
                InitializeNestedTypesMap();
            }

            ImmutableArray<TypeDefinitionHandle> nestedTypes;
            if (_lazyNestedTypesMap.TryGetValue(typeDef, out nestedTypes))
            {
                return nestedTypes;
            }

            return ImmutableArray<TypeDefinitionHandle>.Empty;
        }
        #endregion
    }
}
