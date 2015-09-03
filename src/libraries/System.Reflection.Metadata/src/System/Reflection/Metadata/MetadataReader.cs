// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection.Internal;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Reflection.Metadata
{
    /// <summary>
    /// Reads metadata as defined byte the ECMA 335 CLI specification.
    /// </summary>
    public sealed partial class MetadataReader
    {
        private readonly MetadataReaderOptions _options;
        internal readonly MetadataStringDecoder utf8Decoder;
        internal readonly NamespaceCache namespaceCache;
        private Dictionary<TypeDefinitionHandle, ImmutableArray<TypeDefinitionHandle>> _lazyNestedTypesMap;
        internal readonly MemoryBlock Block;

        // A row id of "mscorlib" AssemblyRef in a WinMD file (each WinMD file must have such a reference).
        internal readonly int WinMDMscorlibRef;

        #region Constructors

        /// <summary>
        /// Creates a metadata reader from the metadata stored at the given memory location.
        /// </summary>
        /// <remarks>
        /// The memory is owned by the caller and it must be kept memory alive and unmodified throughout the lifetime of the <see cref="MetadataReader"/>.
        /// </remarks>
        public unsafe MetadataReader(byte* metadata, int length)
            : this(metadata, length, MetadataReaderOptions.Default, null)
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
            : this(metadata, length, options, null)
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
        public unsafe MetadataReader(byte* metadata, int length, MetadataReaderOptions options, MetadataStringDecoder utf8Decoder)
        {
            if (length <= 0)
            {
                throw new ArgumentOutOfRangeException("length");
            }

            if (metadata == null)
            {
                throw new ArgumentNullException("metadata");
            }

            if (utf8Decoder == null)
            {
                utf8Decoder = MetadataStringDecoder.DefaultUTF8;
            }

            if (!(utf8Decoder.Encoding is UTF8Encoding))
            {
                throw new ArgumentException(SR.MetadataStringDecoderEncodingMustBeUtf8, "utf8Decoder");
            }

            if (!BitConverter.IsLittleEndian)
            {
                throw new PlatformNotSupportedException(SR.LitteEndianArchitectureRequired);
            }

            this.Block = new MemoryBlock(metadata, length);

            _options = options;
            this.utf8Decoder = utf8Decoder;

            BlobReader memReader = new BlobReader(this.Block);

            this.ReadMetadataHeader(ref memReader);

            // storage header and stream headers:
            MemoryBlock metadataTableStream;
            var streamHeaders = this.ReadStreamHeaders(ref memReader);
            this.InitializeStreamReaders(ref this.Block, streamHeaders, out metadataTableStream);

            memReader = new BlobReader(metadataTableStream);

            int[] metadataTableRowCounts;
            this.ReadMetadataTableHeader(ref memReader, out metadataTableRowCounts);
            this.InitializeTableReaders(memReader.GetMemoryBlockAt(0, memReader.RemainingBytes), metadataTableRowCounts);

            // This previously could occur in obfuscated assemblies but a check was added to prevent 
            // it getting to this point
            Debug.Assert(this.AssemblyTable.NumberOfRows <= 1);

            // Although the specification states that the module table will have exactly one row,
            // the native metadata reader would successfully read files containing more than one row.
            // Such files exist in the wild and may be produced by obfuscators.
            if (this.ModuleTable.NumberOfRows < 1)
            {
                throw new BadImageFormatException(SR.Format(SR.ModuleTableInvalidNumberOfRows, this.ModuleTable.NumberOfRows));
            }

            //  read 
            this.namespaceCache = new NamespaceCache(this);

            if (_metadataKind != MetadataKind.Ecma335)
            {
                this.WinMDMscorlibRef = FindMscorlibAssemblyRefNoProjection();
            }
        }

        #endregion

        #region Metadata Headers

        private MetadataHeader _metadataHeader;
        private MetadataKind _metadataKind;
        private MetadataStreamKind _metadataStreamKind;

        internal StringStreamReader StringStream;
        internal BlobStreamReader BlobStream;
        internal GuidStreamReader GuidStream;
        internal UserStringStreamReader UserStringStream;

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
        /// Ecma-335 24.2.1 Metadata root
        /// </summary>
        private void ReadMetadataHeader(ref BlobReader memReader)
        {
            if (memReader.RemainingBytes < COR20Constants.MinimumSizeofMetadataHeader)
            {
                throw new BadImageFormatException(SR.MetadataHeaderTooSmall);
            }

            _metadataHeader.Signature = memReader.ReadUInt32();
            if (_metadataHeader.Signature != COR20Constants.COR20MetadataSignature)
            {
                throw new BadImageFormatException(SR.MetadataSignature);
            }

            _metadataHeader.MajorVersion = memReader.ReadUInt16();
            _metadataHeader.MinorVersion = memReader.ReadUInt16();
            _metadataHeader.ExtraData = memReader.ReadUInt32();
            _metadataHeader.VersionStringSize = memReader.ReadInt32();
            if (memReader.RemainingBytes < _metadataHeader.VersionStringSize)
            {
                throw new BadImageFormatException(SR.NotEnoughSpaceForVersionString);
            }

            int numberOfBytesRead;
            _metadataHeader.VersionString = memReader.GetMemoryBlockAt(0, _metadataHeader.VersionStringSize).PeekUtf8NullTerminated(0, null, utf8Decoder, out numberOfBytesRead, '\0');
            memReader.SkipBytes(_metadataHeader.VersionStringSize);
            _metadataKind = GetMetadataKind(_metadataHeader.VersionString);
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
        /// Reads stream headers described in Ecma-335 24.2.2 Stream header
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

        private void InitializeStreamReaders(ref MemoryBlock metadataRoot, StreamHeader[] streamHeaders, out MemoryBlock metadataTableStream)
        {
            metadataTableStream = default(MemoryBlock);

            foreach (StreamHeader streamHeader in streamHeaders)
            {
                switch (streamHeader.Name)
                {
                    case COR20Constants.StringStreamName:
                        if (metadataRoot.Length < streamHeader.Offset + streamHeader.Size)
                        {
                            throw new BadImageFormatException(SR.NotEnoughSpaceForStringStream);
                        }

                        this.StringStream = new StringStreamReader(metadataRoot.GetMemoryBlockAt((int)streamHeader.Offset, streamHeader.Size), _metadataKind);
                        break;

                    case COR20Constants.BlobStreamName:
                        if (metadataRoot.Length < streamHeader.Offset + streamHeader.Size)
                        {
                            throw new BadImageFormatException(SR.NotEnoughSpaceForBlobStream);
                        }

                        this.BlobStream = new BlobStreamReader(metadataRoot.GetMemoryBlockAt((int)streamHeader.Offset, streamHeader.Size), _metadataKind);
                        break;

                    case COR20Constants.GUIDStreamName:
                        if (metadataRoot.Length < streamHeader.Offset + streamHeader.Size)
                        {
                            throw new BadImageFormatException(SR.NotEnoughSpaceForGUIDStream);
                        }

                        this.GuidStream = new GuidStreamReader(metadataRoot.GetMemoryBlockAt((int)streamHeader.Offset, streamHeader.Size));
                        break;

                    case COR20Constants.UserStringStreamName:
                        if (metadataRoot.Length < streamHeader.Offset + streamHeader.Size)
                        {
                            throw new BadImageFormatException(SR.NotEnoughSpaceForBlobStream);
                        }

                        this.UserStringStream = new UserStringStreamReader(metadataRoot.GetMemoryBlockAt((int)streamHeader.Offset, streamHeader.Size));
                        break;

                    case COR20Constants.CompressedMetadataTableStreamName:
                        if (metadataRoot.Length < streamHeader.Offset + streamHeader.Size)
                        {
                            throw new BadImageFormatException(SR.NotEnoughSpaceForMetadataStream);
                        }

                        _metadataStreamKind = MetadataStreamKind.Compressed;
                        metadataTableStream = metadataRoot.GetMemoryBlockAt((int)streamHeader.Offset, streamHeader.Size);
                        break;

                    case COR20Constants.UncompressedMetadataTableStreamName:
                        if (metadataRoot.Length < streamHeader.Offset + streamHeader.Size)
                        {
                            throw new BadImageFormatException(SR.NotEnoughSpaceForMetadataStream);
                        }

                        _metadataStreamKind = MetadataStreamKind.Uncompressed;
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

                    default:
                        // Skip unknown streams. Some obfuscators insert invalid streams.
                        continue;
                }
            }

            if (IsMinimalDelta && _metadataStreamKind != MetadataStreamKind.Uncompressed)
            {
                throw new BadImageFormatException(SR.InvalidMetadataStreamFormat);
            }
        }

        #endregion

        #region Tables and Heaps

        private MetadataTableHeader _MetadataTableHeader;

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

        private void ReadMetadataTableHeader(ref BlobReader memReader, out int[] metadataTableRowCounts)
        {
            if (memReader.RemainingBytes < MetadataStreamConstants.SizeOfMetadataTableHeader)
            {
                throw new BadImageFormatException(SR.MetadataTableHeaderTooSmall);
            }

            _MetadataTableHeader.Reserved = memReader.ReadUInt32();
            _MetadataTableHeader.MajorVersion = memReader.ReadByte();
            _MetadataTableHeader.MinorVersion = memReader.ReadByte();
            _MetadataTableHeader.HeapSizeFlags = (HeapSizeFlag)memReader.ReadByte();
            _MetadataTableHeader.RowId = memReader.ReadByte();
            _MetadataTableHeader.ValidTables = (TableMask)memReader.ReadUInt64();
            _MetadataTableHeader.SortedTables = (TableMask)memReader.ReadUInt64();
            ulong presentTables = (ulong)_MetadataTableHeader.ValidTables;

            // According to ECMA-335, MajorVersion and MinorVersion have fixed values and, 
            // based on recommendation in 24.1 Fixed fields: When writing these fields it 
            // is best that they be set to the value indicated, on reading they should be ignored.?
            // we will not be checking version values. We will continue checking that the set of 
            // present tables is within the set we understand.
            ulong validTables = (ulong)TableMask.V2_0_TablesMask;

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

            int numberOfTables = _MetadataTableHeader.GetNumberOfTablesPresent();
            if (memReader.RemainingBytes < numberOfTables * sizeof(int))
            {
                throw new BadImageFormatException(SR.TableRowCountSpaceTooSmall);
            }

            var rowCounts = new int[numberOfTables];
            for (int i = 0; i < rowCounts.Length; i++)
            {
                uint rowCount = memReader.ReadUInt32();
                if (rowCount > TokenTypeIds.RIDMask)
                {
                    throw new BadImageFormatException(SR.Format(SR.InvalidRowCount, rowCount));
                }

                rowCounts[i] = (int)rowCount;
            }

            metadataTableRowCounts = rowCounts;
        }

        private const int SmallIndexSize = 2;
        private const int LargeIndexSize = 4;

        private void InitializeTableReaders(MemoryBlock metadataTablesMemoryBlock, int[] compressedRowCounts)
        {
            // Only sizes of tables present in metadata are recorded in rowCountCompressedArray.
            // This array contains a slot for each possible table, not just those that are present in the metadata.
            int[] rowCounts = new int[TableIndexExtensions.Count];

            // Size of reference tags in each table.
            int[] referenceSizes = new int[TableIndexExtensions.Count];

            ulong validTables = (ulong)_MetadataTableHeader.ValidTables;
            int compressedRowCountIndex = 0;
            for (int i = 0; i < TableIndexExtensions.Count; i++)
            {
                bool fitsSmall;

                if ((validTables & 1UL) != 0)
                {
                    int rowCount = compressedRowCounts[compressedRowCountIndex++];
                    rowCounts[i] = rowCount;
                    fitsSmall = rowCount < MetadataStreamConstants.LargeTableRowCount;
                }
                else
                {
                    fitsSmall = true;
                }

                referenceSizes[i] = (fitsSmall && !IsMinimalDelta) ? SmallIndexSize : LargeIndexSize;
                validTables >>= 1;
            }

            this.TableRowCounts = rowCounts;

            // Compute ref sizes for tables that can have pointer tables for it
            int fieldRefSize = referenceSizes[(int)TableIndex.FieldPtr] > SmallIndexSize ? LargeIndexSize : referenceSizes[(int)TableIndex.Field];
            int methodRefSize = referenceSizes[(int)TableIndex.MethodPtr] > SmallIndexSize ? LargeIndexSize : referenceSizes[(int)TableIndex.MethodDef];
            int paramRefSize = referenceSizes[(int)TableIndex.ParamPtr] > SmallIndexSize ? LargeIndexSize : referenceSizes[(int)TableIndex.Param];
            int eventRefSize = referenceSizes[(int)TableIndex.EventPtr] > SmallIndexSize ? LargeIndexSize : referenceSizes[(int)TableIndex.Event];
            int propertyRefSize = referenceSizes[(int)TableIndex.PropertyPtr] > SmallIndexSize ? LargeIndexSize : referenceSizes[(int)TableIndex.Property];

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
            int stringHeapRefSize = (_MetadataTableHeader.HeapSizeFlags & HeapSizeFlag.StringHeapLarge) == HeapSizeFlag.StringHeapLarge ? LargeIndexSize : SmallIndexSize;
            int guidHeapRefSize = (_MetadataTableHeader.HeapSizeFlags & HeapSizeFlag.GuidHeapLarge) == HeapSizeFlag.GuidHeapLarge ? LargeIndexSize : SmallIndexSize;
            int blobHeapRefSize = (_MetadataTableHeader.HeapSizeFlags & HeapSizeFlag.BlobHeapLarge) == HeapSizeFlag.BlobHeapLarge ? LargeIndexSize : SmallIndexSize;

            // Populate the Table blocks
            int totalRequiredSize = 0;
            this.ModuleTable = new ModuleTableReader(rowCounts[(int)TableIndex.Module], stringHeapRefSize, guidHeapRefSize, metadataTablesMemoryBlock, totalRequiredSize);
            totalRequiredSize += this.ModuleTable.Block.Length;

            this.TypeRefTable = new TypeRefTableReader(rowCounts[(int)TableIndex.TypeRef], resolutionScopeRefSize, stringHeapRefSize, metadataTablesMemoryBlock, totalRequiredSize);
            totalRequiredSize += this.TypeRefTable.Block.Length;

            this.TypeDefTable = new TypeDefTableReader(rowCounts[(int)TableIndex.TypeDef], fieldRefSize, methodRefSize, typeDefOrRefRefSize, stringHeapRefSize, metadataTablesMemoryBlock, totalRequiredSize);
            totalRequiredSize += this.TypeDefTable.Block.Length;

            this.FieldPtrTable = new FieldPtrTableReader(rowCounts[(int)TableIndex.FieldPtr], referenceSizes[(int)TableIndex.Field], metadataTablesMemoryBlock, totalRequiredSize);
            totalRequiredSize += this.FieldPtrTable.Block.Length;

            this.FieldTable = new FieldTableReader(rowCounts[(int)TableIndex.Field], stringHeapRefSize, blobHeapRefSize, metadataTablesMemoryBlock, totalRequiredSize);
            totalRequiredSize += this.FieldTable.Block.Length;

            this.MethodPtrTable = new MethodPtrTableReader(rowCounts[(int)TableIndex.MethodPtr], referenceSizes[(int)TableIndex.MethodDef], metadataTablesMemoryBlock, totalRequiredSize);
            totalRequiredSize += this.MethodPtrTable.Block.Length;

            this.MethodDefTable = new MethodTableReader(rowCounts[(int)TableIndex.MethodDef], paramRefSize, stringHeapRefSize, blobHeapRefSize, metadataTablesMemoryBlock, totalRequiredSize);
            totalRequiredSize += this.MethodDefTable.Block.Length;

            this.ParamPtrTable = new ParamPtrTableReader(rowCounts[(int)TableIndex.ParamPtr], referenceSizes[(int)TableIndex.Param], metadataTablesMemoryBlock, totalRequiredSize);
            totalRequiredSize += this.ParamPtrTable.Block.Length;

            this.ParamTable = new ParamTableReader(rowCounts[(int)TableIndex.Param], stringHeapRefSize, metadataTablesMemoryBlock, totalRequiredSize);
            totalRequiredSize += this.ParamTable.Block.Length;

            this.InterfaceImplTable = new InterfaceImplTableReader(rowCounts[(int)TableIndex.InterfaceImpl], IsDeclaredSorted(TableMask.InterfaceImpl), referenceSizes[(int)TableIndex.TypeDef], typeDefOrRefRefSize, metadataTablesMemoryBlock, totalRequiredSize);
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

            this.ClassLayoutTable = new ClassLayoutTableReader(rowCounts[(int)TableIndex.ClassLayout], IsDeclaredSorted(TableMask.ClassLayout), referenceSizes[(int)TableIndex.TypeDef], metadataTablesMemoryBlock, totalRequiredSize);
            totalRequiredSize += this.ClassLayoutTable.Block.Length;

            this.FieldLayoutTable = new FieldLayoutTableReader(rowCounts[(int)TableIndex.FieldLayout], IsDeclaredSorted(TableMask.FieldLayout), referenceSizes[(int)TableIndex.Field], metadataTablesMemoryBlock, totalRequiredSize);
            totalRequiredSize += this.FieldLayoutTable.Block.Length;

            this.StandAloneSigTable = new StandAloneSigTableReader(rowCounts[(int)TableIndex.StandAloneSig], blobHeapRefSize, metadataTablesMemoryBlock, totalRequiredSize);
            totalRequiredSize += this.StandAloneSigTable.Block.Length;

            this.EventMapTable = new EventMapTableReader(rowCounts[(int)TableIndex.EventMap], referenceSizes[(int)TableIndex.TypeDef], eventRefSize, metadataTablesMemoryBlock, totalRequiredSize);
            totalRequiredSize += this.EventMapTable.Block.Length;

            this.EventPtrTable = new EventPtrTableReader(rowCounts[(int)TableIndex.EventPtr], referenceSizes[(int)TableIndex.Event], metadataTablesMemoryBlock, totalRequiredSize);
            totalRequiredSize += this.EventPtrTable.Block.Length;

            this.EventTable = new EventTableReader(rowCounts[(int)TableIndex.Event], typeDefOrRefRefSize, stringHeapRefSize, metadataTablesMemoryBlock, totalRequiredSize);
            totalRequiredSize += this.EventTable.Block.Length;

            this.PropertyMapTable = new PropertyMapTableReader(rowCounts[(int)TableIndex.PropertyMap], referenceSizes[(int)TableIndex.TypeDef], propertyRefSize, metadataTablesMemoryBlock, totalRequiredSize);
            totalRequiredSize += this.PropertyMapTable.Block.Length;

            this.PropertyPtrTable = new PropertyPtrTableReader(rowCounts[(int)TableIndex.PropertyPtr], referenceSizes[(int)TableIndex.Property], metadataTablesMemoryBlock, totalRequiredSize);
            totalRequiredSize += this.PropertyPtrTable.Block.Length;

            this.PropertyTable = new PropertyTableReader(rowCounts[(int)TableIndex.Property], stringHeapRefSize, blobHeapRefSize, metadataTablesMemoryBlock, totalRequiredSize);
            totalRequiredSize += this.PropertyTable.Block.Length;

            this.MethodSemanticsTable = new MethodSemanticsTableReader(rowCounts[(int)TableIndex.MethodSemantics], IsDeclaredSorted(TableMask.MethodSemantics), referenceSizes[(int)TableIndex.MethodDef], hasSemanticsRefSize, metadataTablesMemoryBlock, totalRequiredSize);
            totalRequiredSize += this.MethodSemanticsTable.Block.Length;

            this.MethodImplTable = new MethodImplTableReader(rowCounts[(int)TableIndex.MethodImpl], IsDeclaredSorted(TableMask.MethodImpl), referenceSizes[(int)TableIndex.TypeDef], methodDefOrRefRefSize, metadataTablesMemoryBlock, totalRequiredSize);
            totalRequiredSize += this.MethodImplTable.Block.Length;

            this.ModuleRefTable = new ModuleRefTableReader(rowCounts[(int)TableIndex.ModuleRef], stringHeapRefSize, metadataTablesMemoryBlock, totalRequiredSize);
            totalRequiredSize += this.ModuleRefTable.Block.Length;

            this.TypeSpecTable = new TypeSpecTableReader(rowCounts[(int)TableIndex.TypeSpec], blobHeapRefSize, metadataTablesMemoryBlock, totalRequiredSize);
            totalRequiredSize += this.TypeSpecTable.Block.Length;

            this.ImplMapTable = new ImplMapTableReader(rowCounts[(int)TableIndex.ImplMap], IsDeclaredSorted(TableMask.ImplMap), referenceSizes[(int)TableIndex.ModuleRef], memberForwardedRefSize, stringHeapRefSize, metadataTablesMemoryBlock, totalRequiredSize);
            totalRequiredSize += this.ImplMapTable.Block.Length;

            this.FieldRvaTable = new FieldRVATableReader(rowCounts[(int)TableIndex.FieldRva], IsDeclaredSorted(TableMask.FieldRva), referenceSizes[(int)TableIndex.Field], metadataTablesMemoryBlock, totalRequiredSize);
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

            this.AssemblyRefTable = new AssemblyRefTableReader((int)rowCounts[(int)TableIndex.AssemblyRef], stringHeapRefSize, blobHeapRefSize, metadataTablesMemoryBlock, totalRequiredSize, _metadataKind);
            totalRequiredSize += this.AssemblyRefTable.Block.Length;

            this.AssemblyRefProcessorTable = new AssemblyRefProcessorTableReader(rowCounts[(int)TableIndex.AssemblyRefProcessor], referenceSizes[(int)TableIndex.AssemblyRef], metadataTablesMemoryBlock, totalRequiredSize);
            totalRequiredSize += this.AssemblyRefProcessorTable.Block.Length;

            this.AssemblyRefOSTable = new AssemblyRefOSTableReader(rowCounts[(int)TableIndex.AssemblyRefOS], referenceSizes[(int)TableIndex.AssemblyRef], metadataTablesMemoryBlock, totalRequiredSize);
            totalRequiredSize += this.AssemblyRefOSTable.Block.Length;

            this.FileTable = new FileTableReader(rowCounts[(int)TableIndex.File], stringHeapRefSize, blobHeapRefSize, metadataTablesMemoryBlock, totalRequiredSize);
            totalRequiredSize += this.FileTable.Block.Length;

            this.ExportedTypeTable = new ExportedTypeTableReader(rowCounts[(int)TableIndex.ExportedType], implementationRefSize, stringHeapRefSize, metadataTablesMemoryBlock, totalRequiredSize);
            totalRequiredSize += this.ExportedTypeTable.Block.Length;

            this.ManifestResourceTable = new ManifestResourceTableReader(rowCounts[(int)TableIndex.ManifestResource], implementationRefSize, stringHeapRefSize, metadataTablesMemoryBlock, totalRequiredSize);
            totalRequiredSize += this.ManifestResourceTable.Block.Length;

            this.NestedClassTable = new NestedClassTableReader(rowCounts[(int)TableIndex.NestedClass], IsDeclaredSorted(TableMask.NestedClass), referenceSizes[(int)TableIndex.TypeDef], metadataTablesMemoryBlock, totalRequiredSize);
            totalRequiredSize += this.NestedClassTable.Block.Length;

            this.GenericParamTable = new GenericParamTableReader(rowCounts[(int)TableIndex.GenericParam], IsDeclaredSorted(TableMask.GenericParam), typeOrMethodDefRefSize, stringHeapRefSize, metadataTablesMemoryBlock, totalRequiredSize);
            totalRequiredSize += this.GenericParamTable.Block.Length;

            this.MethodSpecTable = new MethodSpecTableReader(rowCounts[(int)TableIndex.MethodSpec], methodDefOrRefRefSize, blobHeapRefSize, metadataTablesMemoryBlock, totalRequiredSize);
            totalRequiredSize += this.MethodSpecTable.Block.Length;

            this.GenericParamConstraintTable = new GenericParamConstraintTableReader(rowCounts[(int)TableIndex.GenericParamConstraint], IsDeclaredSorted(TableMask.GenericParamConstraint), referenceSizes[(int)TableIndex.GenericParam], typeDefOrRefRefSize, metadataTablesMemoryBlock, totalRequiredSize);
            totalRequiredSize += this.GenericParamConstraintTable.Block.Length;

            if (totalRequiredSize > metadataTablesMemoryBlock.Length)
            {
                throw new BadImageFormatException(SR.MetadataTablesTooSmall);
            }
        }

        private int ComputeCodedTokenSize(int largeRowSize, int[] rowCountArray, TableMask tablesReferenced)
        {
            if (IsMinimalDelta)
            {
                return LargeIndexSize;
            }

            bool isAllReferencedTablesSmall = true;
            ulong tablesReferencedMask = (ulong)tablesReferenced;
            for (int tableIndex = 0; tableIndex < TableIndexExtensions.Count; tableIndex++)
            {
                if ((tablesReferencedMask & 1UL) != 0)
                {
                    isAllReferencedTablesSmall = isAllReferencedTablesSmall && (rowCountArray[tableIndex] < largeRowSize);
                }

                tablesReferencedMask >>= 1;
            }

            return isAllReferencedTablesSmall ? SmallIndexSize : LargeIndexSize;
        }

        private bool IsDeclaredSorted(TableMask index)
        {
            return (_MetadataTableHeader.SortedTables & index) != 0;
        }

        #endregion

        #region Helpers

        // internal for testing
        internal NamespaceCache NamespaceCache
        {
            get { return namespaceCache; }
        }

        internal bool UseFieldPtrTable
        {
            get { return this.FieldPtrTable.NumberOfRows > 0; }
        }

        internal bool UseMethodPtrTable
        {
            get { return this.MethodPtrTable.NumberOfRows > 0; }
        }

        internal bool UseParamPtrTable
        {
            get { return this.ParamPtrTable.NumberOfRows > 0; }
        }

        internal bool UseEventPtrTable
        {
            get { return this.EventPtrTable.NumberOfRows > 0; }
        }

        internal bool UsePropertyPtrTable
        {
            get { return this.PropertyPtrTable.NumberOfRows > 0; }
        }

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
                lastEventRowId = (int)(this.UseEventPtrTable ? this.EventPtrTable.NumberOfRows : this.EventTable.NumberOfRows);
            }
            else
            {
                lastEventRowId = (int)this.EventMapTable.GetEventListStartFor(eventMapRowId + 1) - 1;
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

        #endregion

        #region Public APIs

        public MetadataReaderOptions Options
        {
            get { return _options; }
        }

        public string MetadataVersion
        {
            get { return _metadataHeader.VersionString; }
        }

        public MetadataKind MetadataKind
        {
            get { return _metadataKind; }
        }

        public MetadataStringComparer StringComparer
        {
            get { return new MetadataStringComparer(this); }
        }

        public bool IsAssembly
        {
            get { return this.AssemblyTable.NumberOfRows == 1; }
        }

        public AssemblyReferenceHandleCollection AssemblyReferences
        {
            get { return new AssemblyReferenceHandleCollection(this); }
        }

        public TypeDefinitionHandleCollection TypeDefinitions
        {
            get { return new TypeDefinitionHandleCollection((int)TypeDefTable.NumberOfRows); }
        }

        public TypeReferenceHandleCollection TypeReferences
        {
            get { return new TypeReferenceHandleCollection((int)TypeRefTable.NumberOfRows); }
        }

        public CustomAttributeHandleCollection CustomAttributes
        {
            get { return new CustomAttributeHandleCollection(this); }
        }

        public DeclarativeSecurityAttributeHandleCollection DeclarativeSecurityAttributes
        {
            get { return new DeclarativeSecurityAttributeHandleCollection(this); }
        }

        public MemberReferenceHandleCollection MemberReferences
        {
            get { return new MemberReferenceHandleCollection((int)MemberRefTable.NumberOfRows); }
        }

        public ManifestResourceHandleCollection ManifestResources
        {
            get { return new ManifestResourceHandleCollection((int)ManifestResourceTable.NumberOfRows); }
        }

        public AssemblyFileHandleCollection AssemblyFiles
        {
            get { return new AssemblyFileHandleCollection((int)FileTable.NumberOfRows); }
        }

        public ExportedTypeHandleCollection ExportedTypes
        {
            get { return new ExportedTypeHandleCollection((int)ExportedTypeTable.NumberOfRows); }
        }

        public MethodDefinitionHandleCollection MethodDefinitions
        {
            get { return new MethodDefinitionHandleCollection(this); }
        }

        public FieldDefinitionHandleCollection FieldDefinitions
        {
            get { return new FieldDefinitionHandleCollection(this); }
        }

        public EventDefinitionHandleCollection EventDefinitions
        {
            get { return new EventDefinitionHandleCollection(this); }
        }

        public PropertyDefinitionHandleCollection PropertyDefinitions
        {
            get { return new PropertyDefinitionHandleCollection(this); }
        }

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
            return StringStream.GetString(handle, utf8Decoder);
        }

        public string GetString(NamespaceDefinitionHandle handle)
        {
            if (handle.HasFullName)
            {
                return StringStream.GetString(handle.GetFullName(), utf8Decoder);
            }

            return namespaceCache.GetFullName(handle);
        }

        public byte[] GetBlobBytes(BlobHandle handle)
        {
            return BlobStream.GetBytes(handle);
        }

        public ImmutableArray<byte> GetBlobContent(BlobHandle handle)
        {
            // TODO: We can skip a copy for virtual blobs.
            byte[] bytes = GetBlobBytes(handle);
            return ImmutableByteArrayInterop.DangerousCreateFromUnderlyingArray(ref bytes);
        }

        public BlobReader GetBlobReader(BlobHandle handle)
        {
            return BlobStream.GetBlobReader(handle);
        }

        public string GetUserString(UserStringHandle handle)
        {
            return UserStringStream.GetString(handle);
        }

        public Guid GetGuid(GuidHandle handle)
        {
            return GuidStream.GetGuid(handle);
        }

        public ModuleDefinition GetModuleDefinition()
        {
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
            NamespaceData data = namespaceCache.GetRootNamespace();
            return new NamespaceDefinition(data);
        }

        public NamespaceDefinition GetNamespaceDefinition(NamespaceDefinitionHandle handle)
        {
            NamespaceData data = namespaceCache.GetNamespaceData(handle);
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
            Debug.Assert(!handle.IsNil);
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
