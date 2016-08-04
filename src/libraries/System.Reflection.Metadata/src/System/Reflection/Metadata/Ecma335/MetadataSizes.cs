// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection.Internal;

namespace System.Reflection.Metadata.Ecma335
{
    /// <summary>
    /// Provides information on sizes of various metadata structures.
    /// </summary>
    public sealed class MetadataSizes
    {
        private const int StreamAlignment = 4;

        // Call the length of the string (including the terminator) m (we require m <= 255);
        internal const int MaxMetadataVersionByteCount = 0xff - 1;

        internal readonly int MetadataVersionPaddedLength;

        internal const ulong SortedDebugTables =
            1UL << (int)TableIndex.LocalScope |
            1UL << (int)TableIndex.StateMachineMethod |
            1UL << (int)TableIndex.CustomDebugInformation;

        internal readonly bool IsEncDelta;
        internal readonly bool IsCompressed;

        internal readonly bool BlobReferenceIsSmall;
        internal readonly bool StringReferenceIsSmall;
        internal readonly bool GuidReferenceIsSmall;
        internal readonly bool CustomAttributeTypeCodedIndexIsSmall;
        internal readonly bool DeclSecurityCodedIndexIsSmall;
        internal readonly bool EventDefReferenceIsSmall;
        internal readonly bool FieldDefReferenceIsSmall;
        internal readonly bool GenericParamReferenceIsSmall;
        internal readonly bool HasConstantCodedIndexIsSmall;
        internal readonly bool HasCustomAttributeCodedIndexIsSmall;
        internal readonly bool HasFieldMarshalCodedIndexIsSmall;
        internal readonly bool HasSemanticsCodedIndexIsSmall;
        internal readonly bool ImplementationCodedIndexIsSmall;
        internal readonly bool MemberForwardedCodedIndexIsSmall;
        internal readonly bool MemberRefParentCodedIndexIsSmall;
        internal readonly bool MethodDefReferenceIsSmall;
        internal readonly bool MethodDefOrRefCodedIndexIsSmall;
        internal readonly bool ModuleRefReferenceIsSmall;
        internal readonly bool ParameterReferenceIsSmall;
        internal readonly bool PropertyDefReferenceIsSmall;
        internal readonly bool ResolutionScopeCodedIndexIsSmall;
        internal readonly bool TypeDefReferenceIsSmall;
        internal readonly bool TypeDefOrRefCodedIndexIsSmall;
        internal readonly bool TypeOrMethodDefCodedIndexIsSmall;

        internal readonly bool DocumentReferenceIsSmall;
        internal readonly bool LocalVariableReferenceIsSmall;
        internal readonly bool LocalConstantReferenceIsSmall;
        internal readonly bool ImportScopeReferenceIsSmall;
        internal readonly bool HasCustomDebugInformationCodedIndexIsSmall;

        /// <summary>
        /// Exact (unaligned) heap sizes.
        /// </summary>
        /// <remarks>Use <see cref="GetAlignedHeapSize(HeapIndex)"/> to get an aligned heap size.</remarks>
        public ImmutableArray<int> HeapSizes { get; }

        /// <summary>
        /// Table row counts. 
        /// </summary>
        public ImmutableArray<int> RowCounts { get; }

        /// <summary>
        /// External table row counts. 
        /// </summary>
        public ImmutableArray<int> ExternalRowCounts { get; }

        /// <summary>
        /// Non-empty tables that are emitted into the metadata table stream.
        /// </summary>
        internal readonly ulong PresentTablesMask;

        /// <summary>
        /// Non-empty tables stored in an external metadata table stream that might be referenced from the metadata table stream being emitted.
        /// </summary>
        internal readonly ulong ExternalTablesMask;

        /// <summary>
        /// Overall size of metadata stream storage (stream headers, table stream, heaps, additional streams).
        /// Aligned to <see cref="StreamAlignment"/>.
        /// </summary>
        internal readonly int MetadataStreamStorageSize;

        /// <summary>
        /// The size of metadata stream (#- or #~). Aligned.
        /// Aligned to <see cref="StreamAlignment"/>.
        /// </summary>
        internal readonly int MetadataTableStreamSize;

        /// <summary>
        /// The size of #Pdb stream. Aligned.
        /// </summary>
        internal readonly int StandalonePdbStreamSize;

        internal MetadataSizes(
            ImmutableArray<int> rowCounts,
            ImmutableArray<int> externalRowCounts,
            ImmutableArray<int> heapSizes,
            int metadataVersionByteCount,
            bool isStandaloneDebugMetadata)
        {
            Debug.Assert(rowCounts.Length == MetadataTokens.TableCount);
            Debug.Assert(externalRowCounts.Length == MetadataTokens.TableCount);
            Debug.Assert(heapSizes.Length == MetadataTokens.HeapCount);

            RowCounts = rowCounts;
            ExternalRowCounts = externalRowCounts;
            HeapSizes = heapSizes;

            // +1 for NUL terminator
            MetadataVersionPaddedLength = BitArithmetic.Align(metadataVersionByteCount + 1, 4);

            PresentTablesMask = ComputeNonEmptyTableMask(rowCounts);
            ExternalTablesMask = ComputeNonEmptyTableMask(externalRowCounts);

            // Auto-detect EnC delta from presence of EnC tables.
            // EnC delta tables are stored as uncompressed metadata table stream, other metadata use compressed stream.
            // We could support uncompress non-EnC metadata in future if we needed to.
            bool isEncDelta = IsPresent(TableIndex.EncLog) || IsPresent(TableIndex.EncMap);
            bool isCompressed = !isEncDelta;

            IsEncDelta = isEncDelta;
            IsCompressed = isCompressed;

            BlobReferenceIsSmall = isCompressed && heapSizes[(int)HeapIndex.Blob] <= ushort.MaxValue;
            StringReferenceIsSmall = isCompressed && heapSizes[(int)HeapIndex.String] <= ushort.MaxValue;
            GuidReferenceIsSmall = isCompressed && heapSizes[(int)HeapIndex.Guid] <= ushort.MaxValue;

            // table can either be present or external, it can't be both:
            Debug.Assert((PresentTablesMask & ExternalTablesMask) == 0);

            CustomAttributeTypeCodedIndexIsSmall = IsReferenceSmall(3, TableIndex.MethodDef, TableIndex.MemberRef);
            DeclSecurityCodedIndexIsSmall = IsReferenceSmall(2, TableIndex.MethodDef, TableIndex.TypeDef);
            EventDefReferenceIsSmall = IsReferenceSmall(0, TableIndex.Event);
            FieldDefReferenceIsSmall = IsReferenceSmall(0, TableIndex.Field);
            GenericParamReferenceIsSmall = IsReferenceSmall(0, TableIndex.GenericParam);
            HasConstantCodedIndexIsSmall = IsReferenceSmall(2, TableIndex.Field, TableIndex.Param, TableIndex.Property);

            HasCustomAttributeCodedIndexIsSmall = IsReferenceSmall(5,
                TableIndex.MethodDef,
                TableIndex.Field,
                TableIndex.TypeRef,
                TableIndex.TypeDef,
                TableIndex.Param,
                TableIndex.InterfaceImpl,
                TableIndex.MemberRef,
                TableIndex.Module,
                TableIndex.DeclSecurity,
                TableIndex.Property,
                TableIndex.Event,
                TableIndex.StandAloneSig,
                TableIndex.ModuleRef,
                TableIndex.TypeSpec,
                TableIndex.Assembly,
                TableIndex.AssemblyRef,
                TableIndex.File,
                TableIndex.ExportedType,
                TableIndex.ManifestResource,
                TableIndex.GenericParam,
                TableIndex.GenericParamConstraint,
                TableIndex.MethodSpec);

            HasFieldMarshalCodedIndexIsSmall = IsReferenceSmall(1, TableIndex.Field, TableIndex.Param);
            HasSemanticsCodedIndexIsSmall = IsReferenceSmall(1, TableIndex.Event, TableIndex.Property);
            ImplementationCodedIndexIsSmall = IsReferenceSmall(2, TableIndex.File, TableIndex.AssemblyRef, TableIndex.ExportedType);
            MemberForwardedCodedIndexIsSmall = IsReferenceSmall(1, TableIndex.Field, TableIndex.MethodDef);
            MemberRefParentCodedIndexIsSmall = IsReferenceSmall(3, TableIndex.TypeDef, TableIndex.TypeRef, TableIndex.ModuleRef, TableIndex.MethodDef, TableIndex.TypeSpec);
            MethodDefReferenceIsSmall = IsReferenceSmall(0, TableIndex.MethodDef);
            MethodDefOrRefCodedIndexIsSmall = IsReferenceSmall(1, TableIndex.MethodDef, TableIndex.MemberRef);
            ModuleRefReferenceIsSmall = IsReferenceSmall(0, TableIndex.ModuleRef);
            ParameterReferenceIsSmall = IsReferenceSmall(0, TableIndex.Param);
            PropertyDefReferenceIsSmall = IsReferenceSmall(0, TableIndex.Property);
            ResolutionScopeCodedIndexIsSmall = IsReferenceSmall(2, TableIndex.Module, TableIndex.ModuleRef, TableIndex.AssemblyRef, TableIndex.TypeRef);
            TypeDefReferenceIsSmall = IsReferenceSmall(0, TableIndex.TypeDef);
            TypeDefOrRefCodedIndexIsSmall = IsReferenceSmall(2, TableIndex.TypeDef, TableIndex.TypeRef, TableIndex.TypeSpec);
            TypeOrMethodDefCodedIndexIsSmall = IsReferenceSmall(1, TableIndex.TypeDef, TableIndex.MethodDef);

            DocumentReferenceIsSmall = IsReferenceSmall(0, TableIndex.Document);
            LocalVariableReferenceIsSmall = IsReferenceSmall(0, TableIndex.LocalVariable);
            LocalConstantReferenceIsSmall = IsReferenceSmall(0, TableIndex.LocalConstant);
            ImportScopeReferenceIsSmall = IsReferenceSmall(0, TableIndex.ImportScope);

            HasCustomDebugInformationCodedIndexIsSmall = IsReferenceSmall(5,
                TableIndex.MethodDef,
                TableIndex.Field,
                TableIndex.TypeRef,
                TableIndex.TypeDef,
                TableIndex.Param,
                TableIndex.InterfaceImpl,
                TableIndex.MemberRef,
                TableIndex.Module,
                TableIndex.DeclSecurity,
                TableIndex.Property,
                TableIndex.Event,
                TableIndex.StandAloneSig,
                TableIndex.ModuleRef,
                TableIndex.TypeSpec,
                TableIndex.Assembly,
                TableIndex.AssemblyRef,
                TableIndex.File,
                TableIndex.ExportedType,
                TableIndex.ManifestResource,
                TableIndex.GenericParam,
                TableIndex.GenericParamConstraint,
                TableIndex.MethodSpec,
                TableIndex.Document,
                TableIndex.LocalScope,
                TableIndex.LocalVariable,
                TableIndex.LocalConstant,
                TableIndex.ImportScope);

            int size = CalculateTableStreamHeaderSize();

            const byte small = 2;
            const byte large = 4;

            byte blobReferenceSize = BlobReferenceIsSmall ? small : large;
            byte stringReferenceSize = StringReferenceIsSmall ? small : large;
            byte guidReferenceSize = GuidReferenceIsSmall ? small : large;
            byte customAttributeTypeCodedIndexSize = CustomAttributeTypeCodedIndexIsSmall ? small : large;
            byte declSecurityCodedIndexSize = DeclSecurityCodedIndexIsSmall ? small : large;
            byte eventDefReferenceSize = EventDefReferenceIsSmall ? small : large;
            byte fieldDefReferenceSize = FieldDefReferenceIsSmall ? small : large;
            byte genericParamReferenceSize = GenericParamReferenceIsSmall ? small : large;
            byte hasConstantCodedIndexSize = HasConstantCodedIndexIsSmall ? small : large;
            byte hasCustomAttributeCodedIndexSize = HasCustomAttributeCodedIndexIsSmall ? small : large;
            byte hasFieldMarshalCodedIndexSize = HasFieldMarshalCodedIndexIsSmall ? small : large;
            byte hasSemanticsCodedIndexSize = HasSemanticsCodedIndexIsSmall ? small : large;
            byte implementationCodedIndexSize = ImplementationCodedIndexIsSmall ? small : large;
            byte memberForwardedCodedIndexSize = MemberForwardedCodedIndexIsSmall ? small : large;
            byte memberRefParentCodedIndexSize = MemberRefParentCodedIndexIsSmall ? small : large;
            byte methodDefReferenceSize = MethodDefReferenceIsSmall ? small : large;
            byte methodDefOrRefCodedIndexSize = MethodDefOrRefCodedIndexIsSmall ? small : large;
            byte moduleRefReferenceSize = ModuleRefReferenceIsSmall ? small : large;
            byte parameterReferenceSize = ParameterReferenceIsSmall ? small : large;
            byte propertyDefReferenceSize = PropertyDefReferenceIsSmall ? small : large;
            byte resolutionScopeCodedIndexSize = ResolutionScopeCodedIndexIsSmall ? small : large;
            byte typeDefReferenceSize = TypeDefReferenceIsSmall ? small : large;
            byte typeDefOrRefCodedIndexSize = TypeDefOrRefCodedIndexIsSmall ? small : large;
            byte typeOrMethodDefCodedIndexSize = TypeOrMethodDefCodedIndexIsSmall ? small : large;
            byte documentReferenceSize = DocumentReferenceIsSmall ? small : large;
            byte localVariableReferenceSize = LocalVariableReferenceIsSmall ? small : large;
            byte localConstantReferenceSize = LocalConstantReferenceIsSmall ? small : large;
            byte importScopeReferenceSize = ImportScopeReferenceIsSmall ? small : large;
            byte hasCustomDebugInformationCodedIndexSize = HasCustomDebugInformationCodedIndexIsSmall ? small : large;

            size += GetTableSize(TableIndex.Module, 2 + 3 * guidReferenceSize + stringReferenceSize);
            size += GetTableSize(TableIndex.TypeRef, resolutionScopeCodedIndexSize + stringReferenceSize + stringReferenceSize);
            size += GetTableSize(TableIndex.TypeDef, 4 + stringReferenceSize + stringReferenceSize + typeDefOrRefCodedIndexSize + fieldDefReferenceSize + methodDefReferenceSize);
            Debug.Assert(rowCounts[(int)TableIndex.FieldPtr] == 0);
            size += GetTableSize(TableIndex.Field, 2 + stringReferenceSize + blobReferenceSize);
            Debug.Assert(rowCounts[(int)TableIndex.MethodPtr] == 0);
            size += GetTableSize(TableIndex.MethodDef, 8 + stringReferenceSize + blobReferenceSize + parameterReferenceSize);
            Debug.Assert(rowCounts[(int)TableIndex.ParamPtr] == 0);
            size += GetTableSize(TableIndex.Param, 4 + stringReferenceSize);
            size += GetTableSize(TableIndex.InterfaceImpl, typeDefReferenceSize + typeDefOrRefCodedIndexSize);
            size += GetTableSize(TableIndex.MemberRef, memberRefParentCodedIndexSize + stringReferenceSize + blobReferenceSize);
            size += GetTableSize(TableIndex.Constant, 2 + hasConstantCodedIndexSize + blobReferenceSize);
            size += GetTableSize(TableIndex.CustomAttribute, hasCustomAttributeCodedIndexSize + customAttributeTypeCodedIndexSize + blobReferenceSize);
            size += GetTableSize(TableIndex.FieldMarshal, hasFieldMarshalCodedIndexSize + blobReferenceSize);
            size += GetTableSize(TableIndex.DeclSecurity, 2 + declSecurityCodedIndexSize + blobReferenceSize);
            size += GetTableSize(TableIndex.ClassLayout, 6 + typeDefReferenceSize);
            size += GetTableSize(TableIndex.FieldLayout, 4 + fieldDefReferenceSize);
            size += GetTableSize(TableIndex.StandAloneSig, blobReferenceSize);
            size += GetTableSize(TableIndex.EventMap, typeDefReferenceSize + eventDefReferenceSize);
            Debug.Assert(rowCounts[(int)TableIndex.EventPtr] == 0);
            size += GetTableSize(TableIndex.Event, 2 + stringReferenceSize + typeDefOrRefCodedIndexSize);
            size += GetTableSize(TableIndex.PropertyMap, typeDefReferenceSize + propertyDefReferenceSize);
            Debug.Assert(rowCounts[(int)TableIndex.PropertyPtr] == 0);
            size += GetTableSize(TableIndex.Property, 2 + stringReferenceSize + blobReferenceSize);
            size += GetTableSize(TableIndex.MethodSemantics, 2 + methodDefReferenceSize + hasSemanticsCodedIndexSize);
            size += GetTableSize(TableIndex.MethodImpl, typeDefReferenceSize + methodDefOrRefCodedIndexSize + methodDefOrRefCodedIndexSize);
            size += GetTableSize(TableIndex.ModuleRef, stringReferenceSize);
            size += GetTableSize(TableIndex.TypeSpec, blobReferenceSize);
            size += GetTableSize(TableIndex.ImplMap, 2 + memberForwardedCodedIndexSize + stringReferenceSize + moduleRefReferenceSize);
            size += GetTableSize(TableIndex.FieldRva, 4 + fieldDefReferenceSize);
            size += GetTableSize(TableIndex.EncLog, 8);
            size += GetTableSize(TableIndex.EncMap, 4);
            size += GetTableSize(TableIndex.Assembly, 16 + blobReferenceSize + stringReferenceSize + stringReferenceSize);
            Debug.Assert(rowCounts[(int)TableIndex.AssemblyProcessor] == 0);
            Debug.Assert(rowCounts[(int)TableIndex.AssemblyOS] == 0);
            size += GetTableSize(TableIndex.AssemblyRef, 12 + blobReferenceSize + stringReferenceSize + stringReferenceSize + blobReferenceSize);
            Debug.Assert(rowCounts[(int)TableIndex.AssemblyRefProcessor] == 0);
            Debug.Assert(rowCounts[(int)TableIndex.AssemblyRefOS] == 0);
            size += GetTableSize(TableIndex.File, 4 + stringReferenceSize + blobReferenceSize);
            size += GetTableSize(TableIndex.ExportedType, 8 + stringReferenceSize + stringReferenceSize + implementationCodedIndexSize);
            size += GetTableSize(TableIndex.ManifestResource, 8 + stringReferenceSize + implementationCodedIndexSize);
            size += GetTableSize(TableIndex.NestedClass, typeDefReferenceSize + typeDefReferenceSize);
            size += GetTableSize(TableIndex.GenericParam, 4 + typeOrMethodDefCodedIndexSize + stringReferenceSize);
            size += GetTableSize(TableIndex.MethodSpec, methodDefOrRefCodedIndexSize + blobReferenceSize);
            size += GetTableSize(TableIndex.GenericParamConstraint, genericParamReferenceSize + typeDefOrRefCodedIndexSize);

            size += GetTableSize(TableIndex.Document, blobReferenceSize + guidReferenceSize + blobReferenceSize + guidReferenceSize);
            size += GetTableSize(TableIndex.MethodDebugInformation, documentReferenceSize + blobReferenceSize);
            size += GetTableSize(TableIndex.LocalScope, methodDefReferenceSize + importScopeReferenceSize + localVariableReferenceSize + localConstantReferenceSize + 4 + 4);
            size += GetTableSize(TableIndex.LocalVariable, 2 + 2 + stringReferenceSize);
            size += GetTableSize(TableIndex.LocalConstant, stringReferenceSize + blobReferenceSize);
            size += GetTableSize(TableIndex.ImportScope, importScopeReferenceSize + blobReferenceSize);
            size += GetTableSize(TableIndex.StateMachineMethod, methodDefReferenceSize + methodDefReferenceSize);
            size += GetTableSize(TableIndex.CustomDebugInformation, hasCustomDebugInformationCodedIndexSize + guidReferenceSize + blobReferenceSize);

            // +1 for terminating 0 byte
            size = BitArithmetic.Align(size + 1, StreamAlignment);

            MetadataTableStreamSize = size;

            size += GetAlignedHeapSize(HeapIndex.String);
            size += GetAlignedHeapSize(HeapIndex.UserString);
            size += GetAlignedHeapSize(HeapIndex.Guid);
            size += GetAlignedHeapSize(HeapIndex.Blob);

            StandalonePdbStreamSize = isStandaloneDebugMetadata ? CalculateStandalonePdbStreamSize() : 0;
            size += StandalonePdbStreamSize;

            MetadataStreamStorageSize = size;
        }

        internal bool IsStandaloneDebugMetadata => StandalonePdbStreamSize > 0;

        internal bool IsPresent(TableIndex table) => (PresentTablesMask & (1UL << (int)table)) != 0;

        /// <summary>
        /// Metadata header size.
        /// Includes:
        /// - metadata storage signature
        /// - storage header
        /// - stream headers
        /// </summary>
        internal int MetadataHeaderSize
        {
            get
            {
                const int RegularStreamHeaderSizes = 76;
                const int EncDeltaMarkerStreamHeaderSize = 16;
                const int StandalonePdbStreamHeaderSize = 16;

                Debug.Assert(RegularStreamHeaderSizes ==
                    GetMetadataStreamHeaderSize("#~") +
                    GetMetadataStreamHeaderSize("#Strings") +
                    GetMetadataStreamHeaderSize("#US") +
                    GetMetadataStreamHeaderSize("#GUID") +
                    GetMetadataStreamHeaderSize("#Blob"));

                Debug.Assert(EncDeltaMarkerStreamHeaderSize == GetMetadataStreamHeaderSize("#JTD"));
                Debug.Assert(StandalonePdbStreamHeaderSize == GetMetadataStreamHeaderSize("#Pdb"));

                return
                    sizeof(uint) +                 // signature
                    sizeof(ushort) +               // major version
                    sizeof(ushort) +               // minor version
                    sizeof(uint) +                 // reserved
                    sizeof(uint) +                 // padded metadata version length
                    MetadataVersionPaddedLength +  // metadata version
                    sizeof(ushort) +               // storage header: reserved
                    sizeof(ushort) +               // stream count
                    (IsStandaloneDebugMetadata ? StandalonePdbStreamHeaderSize : 0) + 
                    RegularStreamHeaderSizes +
                    (IsEncDelta ? EncDeltaMarkerStreamHeaderSize : 0);
            }
        }

        internal static int GetMetadataStreamHeaderSize(string streamName)
        {
            return
                sizeof(int) + // offset
                sizeof(int) + // size
                BitArithmetic.Align(streamName.Length + 1, 4); // zero-terminated name, padding
        }

        /// <summary>
        /// Total size of metadata (header and all streams).
        /// </summary>
        internal int MetadataSize => MetadataHeaderSize + MetadataStreamStorageSize;

        /// <summary>
        /// Returns aligned size of the specified heap.
        /// </summary>
        public int GetAlignedHeapSize(HeapIndex index)
        {
            int i = (int)index;
            if (i < 0 || i > HeapSizes.Length)
            {
                Throw.ArgumentOutOfRange(nameof(index));
            }

            return BitArithmetic.Align(HeapSizes[i], StreamAlignment);
        }

        internal int CalculateTableStreamHeaderSize()
        {
            int result = sizeof(int) +        // Reserved
                         sizeof(short) +      // Version (major, minor)      
                         sizeof(byte) +       // Heap index sizes
                         sizeof(byte) +       // Bit width of RowId
                         sizeof(long) +       // Valid table mask
                         sizeof(long);        // Sorted table mask

            // present table row counts
            for (int i = 0; i < RowCounts.Length; i++)
            {
                if (((1UL << i) & PresentTablesMask) != 0)
                {
                    result += sizeof(int);
                }
            }

            return result;
        }

        internal const int PdbIdSize = 20;

        internal int CalculateStandalonePdbStreamSize()
        {
            int result = 
                PdbIdSize +                                                         // PDB ID
                sizeof(int) +                                                       // EntryPoint
                sizeof(long) +                                                      // ReferencedTypeSystemTables
                BitArithmetic.CountBits(ExternalTablesMask) * sizeof(int); // External row counts

            Debug.Assert(result % StreamAlignment == 0);
            return result;
        }

        private static ulong ComputeNonEmptyTableMask(ImmutableArray<int> rowCounts)
        {
            ulong mask = 0;
            for (int i = 0; i < rowCounts.Length; i++)
            {
                if (rowCounts[i] > 0)
                {
                    mask |= (1UL << i);
                }
            }

            return mask;
        }

        private int GetTableSize(TableIndex index, int rowSize)
        {
            return RowCounts[(int)index] * rowSize;
        }

        private bool IsReferenceSmall(int tagBitSize, params TableIndex[] tables)
        {
            const int smallBitCount = 16;
            return IsCompressed && ReferenceFits(smallBitCount - tagBitSize, tables);
        }

        private bool ReferenceFits(int bitCount, TableIndex[] tables)
        {
            int maxIndex = (1 << bitCount) - 1;
            foreach (TableIndex table in tables)
            {
                // table can be either local or external, but not both:
                Debug.Assert(RowCounts[(int)table] == 0 || ExternalRowCounts[(int)table] == 0);

                if (RowCounts[(int)table] + ExternalRowCounts[(int)table] > maxIndex)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
