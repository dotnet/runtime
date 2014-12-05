// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;

namespace System.Reflection.Metadata.Ecma335
{
    public static class MetadataTokens
    {
        /// <summary>
        /// Maximum number of tables that can be present in Ecma335 metadata.
        /// </summary>
        public static readonly int TableCount = TableIndexExtensions.Count;

        /// <summary>
        /// Maximum number of tables that can be present in Ecma335 metadata.
        /// </summary>
        public static readonly int HeapCount = HeapIndexExtensions.Count;

        /// <summary>
        /// Returns the row number of a metadata table entry that corresponds 
        /// to the specified <paramref name="handle"/> in the context of <paramref name="reader"/>.
        /// </summary>
        /// <returns>One based row number.</returns>
        /// <exception cref="ArgumentException">The <paramref name="handle"/> is not a valid metadata table handle.</exception>
        public static int GetRowNumber(this MetadataReader reader, Handle handle)
        {
            if (handle.IsHeapHandle)
            {
                ThrowTableHandleRequired();
            }

            if (handle.IsVirtual)
            {
                return MapVirtualHandleRowId(reader, handle);
            }

            return (int)handle.RowId;
        }

        /// <summary>
        /// Returns the offset of metadata heap data that corresponds 
        /// to the specified <paramref name="handle"/> in the context of <paramref name="reader"/>.
        /// </summary>
        /// <returns>Zero based offset, or -1 if <paramref name="handle"/> isn't a metadata heap handle.</returns>
        /// <exception cref="NotSupportedException">The operation is not supported for the specified <paramref name="handle"/>.</exception>
        /// <exception cref="ArgumentException">The <paramref name="handle"/> is invalid.</exception>
        public static int GetHeapOffset(this MetadataReader reader, Handle handle)
        {
            if (!handle.IsHeapHandle)
            {
                ThrowHeapHandleRequired();
            }

            if (handle.IsVirtual)
            {
                return MapVirtualHandleRowId(reader, handle);
            }

            return (int)handle.RowId;
        }

        /// <summary>
        /// Returns the metadata token of the specified <paramref name="handle"/> in the context of <paramref name="reader"/>.
        /// </summary>
        /// <returns>Metadata token.</returns>
        /// <exception cref="ArgumentException">
        /// Handle represents a metadata entity that doesn't have a token.
        /// A token can only be retrieved for a metadata table handle or a heap handle of type <see cref="HandleKind.UserString"/>.
        /// </exception>
        public static int GetToken(this MetadataReader reader, Handle handle)
        {
            if (!TokenTypeIds.IsEcmaToken(handle.value))
            {
                ThrowTableHandleOrUserStringRequired();
            }

            if (handle.IsVirtual)
            {
                return MapVirtualHandleRowId(reader, handle) | (int)(handle.value & TokenTypeIds.TokenTypeMask);
            }

            return (int)handle.value;
        }

        private static int MapVirtualHandleRowId(MetadataReader reader, Handle handle)
        {
            switch (handle.Kind)
            {
                case HandleKind.AssemblyReference:
                    // pretend that virtual rows immediately follow real rows:
                    return (int)(reader.AssemblyRefTable.NumberOfNonVirtualRows + 1 + handle.RowId);

                case HandleKind.String:
                case HandleKind.Blob:
                    // We could precalculate offsets for virtual strings and blobs as we are creating them
                    // if we wanted to implement this.
                    throw new NotSupportedException(MetadataResources.CantGetOffsetForVirtualHeapHandle);

                default:
                    throw new ArgumentException(MetadataResources.InvalidHandle, "handle");
            }
        }

        /// <summary>
        /// Returns the row number of a metadata table entry that corresponds 
        /// to the specified <paramref name="handle"/>.
        /// </summary>
        /// <returns>
        /// One based row number, or -1 if <paramref name="handle"/> can only be interpreted in a context of a specific <see cref="MetadataReader"/>.
        /// See <see cref="GetRowNumber(MetadataReader, Handle)"/>.
        /// </returns>
        public static int GetRowNumber(Handle handle)
        {
            if (handle.IsHeapHandle)
            {
                ThrowTableHandleRequired();
            }

            if (handle.IsVirtual)
            {
                return -1;
            }

            return (int)handle.RowId;
        }

        /// <summary>
        /// Returns the offset of metadata heap data that corresponds 
        /// to the specified <paramref name="handle"/>.
        /// </summary>
        /// <returns>
        /// Zero based offset, or -1 if <paramref name="handle"/> can only be interpreted in a context of a specific <see cref="MetadataReader"/>.
        /// See <see cref="GetHeapOffset(MetadataReader, Handle)"/>.
        /// </returns>
        public static int GetHeapOffset(Handle handle)
        {
            if (!handle.IsHeapHandle)
            {
                ThrowHeapHandleRequired();
            }

            if (handle.IsVirtual)
            {
                return -1;
            }

            return (int)handle.RowId;
        }

        /// <summary>
        /// Returns the metadata token of the specified <paramref name="handle"/>.
        /// </summary>
        /// <returns>
        /// Metadata token, or 0 if <paramref name="handle"/> can only be interpreted in a context of a specific <see cref="MetadataReader"/>.
        /// See <see cref="GetToken(MetadataReader, Handle)"/>.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Handle represents a metadata entity that doesn't have a token.
        /// A token can only be retrieved for a metadata table handle or a heap handle of type <see cref="HandleKind.UserString"/>.
        /// </exception>
        public static int GetToken(Handle handle)
        {
            if (!TokenTypeIds.IsEcmaToken(handle.value))
            {
                ThrowTableHandleOrUserStringRequired();
            }

            if (handle.IsVirtual)
            {
                return 0;
            }

            return (int)handle.value;
        }

        /// <summary>
        /// Gets the <see cref="TableIndex"/> of the table corresponding to the specified <see cref="HandleKind"/>.
        /// </summary>
        /// <param name="type">Handle type.</param>
        /// <param name="index">Table index.</param>
        /// <returns>True if the handle type corresponds to an Ecma335 table, false otherwise.</returns>
        public static bool TryGetTableIndex(HandleKind type, out TableIndex index)
        {
            if ((int)type < TableIndexExtensions.Count)
            {
                index = (TableIndex)type;
                return true;
            }

            index = 0;
            return false;
        }

        /// <summary>
        /// Gets the <see cref="HeapIndex"/> of the heap corresponding to the specified <see cref="HandleKind"/>.
        /// </summary>
        /// <param name="type">Handle type.</param>
        /// <param name="index">Heap index.</param>
        /// <returns>True if the handle type corresponds to an Ecma335 heap, false otherwise.</returns>
        public static bool TryGetHeapIndex(HandleKind type, out HeapIndex index)
        {
            switch (type)
            {
                case HandleKind.UserString:
                    index = HeapIndex.UserString;
                    return true;

                case HandleKind.String:
                case HandleKind.NamespaceDefinition:
                    index = HeapIndex.String;
                    return true;

                case HandleKind.Blob:
                    index = HeapIndex.Blob;
                    return true;

                case HandleKind.Guid:
                    index = HeapIndex.Guid;
                    return true;

                default:
                    index = 0;
                    return false;
            }
        }

        #region Handle Factories 

        /// <summary>
        /// Creates a handle from a token value.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// <paramref name="token"/> is not a valid metadata token.
        /// It must encode a metadata table entity or an offset in <see cref="HandleKind.UserString"/> heap.
        /// </exception>
        public static Handle Handle(int token)
        {
            if (!TokenTypeIds.IsEcmaToken(unchecked((uint)token)))
            {
                ThrowInvalidToken();
            }

            return new Handle((uint)token);
        }

        /// <summary>
        /// Creates a handle from a token value.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// <paramref name="tableIndex"/> is not a valid table index.</exception>
        public static Handle Handle(TableIndex tableIndex, int rowNumber)
        {
            int token = ((int)tableIndex << TokenTypeIds.RowIdBitCount) | rowNumber;

            if (!TokenTypeIds.IsEcmaToken(unchecked((uint)token)))
            {
                ThrowInvalidTableIndex();
            }

            return new Handle((uint)token);
        }

        public static MethodDefinitionHandle MethodDefinitionHandle(int rowNumber)
        {
            return Metadata.MethodDefinitionHandle.FromRowId((uint)(rowNumber & TokenTypeIds.RIDMask));
        }

        public static MethodImplementationHandle MethodImplementationHandle(int rowNumber)
        {
            return Metadata.MethodImplementationHandle.FromRowId((uint)(rowNumber & TokenTypeIds.RIDMask));
        }

        public static MethodSpecificationHandle MethodSpecificationHandle(int rowNumber)
        {
            return Metadata.MethodSpecificationHandle.FromRowId((uint)(rowNumber & TokenTypeIds.RIDMask));
        }

        public static TypeDefinitionHandle TypeDefinitionHandle(int rowNumber)
        {
            return Metadata.TypeDefinitionHandle.FromRowId((uint)(rowNumber & TokenTypeIds.RIDMask));
        }

        public static ExportedTypeHandle ExportedTypeHandle(int rowNumber)
        {
            return Metadata.ExportedTypeHandle.FromRowId((uint)(rowNumber & TokenTypeIds.RIDMask));
        }

        public static TypeReferenceHandle TypeReferenceHandle(int rowNumber)
        {
            return Metadata.TypeReferenceHandle.FromRowId((uint)(rowNumber & TokenTypeIds.RIDMask));
        }

        public static TypeSpecificationHandle TypeSpecificationHandle(int rowNumber)
        {
            return Metadata.TypeSpecificationHandle.FromRowId((uint)(rowNumber & TokenTypeIds.RIDMask));
        }

        public static MemberReferenceHandle MemberReferenceHandle(int rowNumber)
        {
            return Metadata.MemberReferenceHandle.FromRowId((uint)(rowNumber & TokenTypeIds.RIDMask));
        }

        public static FieldDefinitionHandle FieldDefinitionHandle(int rowNumber)
        {
            return Metadata.FieldDefinitionHandle.FromRowId((uint)(rowNumber & TokenTypeIds.RIDMask));
        }

        public static EventDefinitionHandle EventDefinitionHandle(int rowNumber)
        {
            return Metadata.EventDefinitionHandle.FromRowId((uint)(rowNumber & TokenTypeIds.RIDMask));
        }

        public static PropertyDefinitionHandle PropertyDefinitionHandle(int rowNumber)
        {
            return Metadata.PropertyDefinitionHandle.FromRowId((uint)(rowNumber & TokenTypeIds.RIDMask));
        }

        public static StandaloneSignatureHandle StandaloneSignatureHandle(int rowNumber)
        {
            return Metadata.StandaloneSignatureHandle.FromRowId((uint)(rowNumber & TokenTypeIds.RIDMask));
        }

        public static ParameterHandle ParameterHandle(int rowNumber)
        {
            return Metadata.ParameterHandle.FromRowId((uint)(rowNumber & TokenTypeIds.RIDMask));
        }

        public static GenericParameterHandle GenericParameterHandle(int rowNumber)
        {
            return Metadata.GenericParameterHandle.FromRowId((uint)(rowNumber & TokenTypeIds.RIDMask));
        }

        public static GenericParameterConstraintHandle GenericParameterConstraintHandle(int rowNumber)
        {
            return Metadata.GenericParameterConstraintHandle.FromRowId((uint)(rowNumber & TokenTypeIds.RIDMask));
        }

        public static ModuleReferenceHandle ModuleReferenceHandle(int rowNumber)
        {
            return Metadata.ModuleReferenceHandle.FromRowId((uint)(rowNumber & TokenTypeIds.RIDMask));
        }

        public static AssemblyReferenceHandle AssemblyReferenceHandle(int rowNumber)
        {
            return Metadata.AssemblyReferenceHandle.FromRowId((uint)(rowNumber & TokenTypeIds.RIDMask));
        }

        public static CustomAttributeHandle CustomAttributeHandle(int rowNumber)
        {
            return Metadata.CustomAttributeHandle.FromRowId((uint)(rowNumber & TokenTypeIds.RIDMask));
        }

        public static DeclarativeSecurityAttributeHandle DeclarativeSecurityAttributeHandle(int rowNumber)
        {
            return Metadata.DeclarativeSecurityAttributeHandle.FromRowId((uint)(rowNumber & TokenTypeIds.RIDMask));
        }

        public static ConstantHandle ConstantHandle(int rowNumber)
        {
            return Metadata.ConstantHandle.FromRowId((uint)(rowNumber & TokenTypeIds.RIDMask));
        }

        public static ManifestResourceHandle ManifestResourceHandle(int rowNumber)
        {
            return Metadata.ManifestResourceHandle.FromRowId((uint)(rowNumber & TokenTypeIds.RIDMask));
        }

        public static AssemblyFileHandle AssemblyFileHandle(int rowNumber)
        {
            return Metadata.AssemblyFileHandle.FromRowId((uint)(rowNumber & TokenTypeIds.RIDMask));
        }

        public static UserStringHandle UserStringHandle(int offset)
        {
            return Metadata.UserStringHandle.FromIndex((uint)(offset & TokenTypeIds.RIDMask));
        }

        public static StringHandle StringHandle(int offset)
        {
            return Metadata.StringHandle.FromIndex((uint)(offset & TokenTypeIds.RIDMask));
        }

        public static BlobHandle BlobHandle(int offset)
        {
            return Metadata.BlobHandle.FromIndex((uint)(offset & TokenTypeIds.RIDMask));
        }

        public static GuidHandle GuidHandle(int offset)
        {
            return Metadata.GuidHandle.FromIndex((uint)(offset & TokenTypeIds.RIDMask));
        }

        #endregion

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowTableHandleRequired()
        {
            throw new ArgumentException(MetadataResources.NotMetadataTableHandle, "handle");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowHeapHandleRequired()
        {
            throw new ArgumentException(MetadataResources.NotMetadataHeapHandle, "handle");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowTableHandleOrUserStringRequired()
        {
            throw new ArgumentException(MetadataResources.NotMetadataTableOrUserStringHandle, "handle");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowInvalidToken()
        {
            throw new ArgumentException(MetadataResources.InvalidToken, "token");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowInvalidTableIndex()
        {
            throw new ArgumentOutOfRangeException("tableIndex");
        }
    }
}
