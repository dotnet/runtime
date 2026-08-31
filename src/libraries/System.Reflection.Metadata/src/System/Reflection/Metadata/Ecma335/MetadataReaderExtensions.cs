// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Internal;

namespace System.Reflection.Metadata.Ecma335
{
    /// <summary>
    /// Provides extension methods for working with certain raw elements of the ECMA-335 metadata tables and heaps.
    /// </summary>
    public static class MetadataReaderExtensions
    {
        /// <summary>
        /// Returns the number of rows in the specified table.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="reader"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="tableIndex"/> is not a valid table index.</exception>
        public static int GetTableRowCount(this MetadataReader reader, TableIndex tableIndex)
        {
            if (reader is null)
            {
                Throw.ArgumentNull(nameof(reader));
            }

            if ((int)tableIndex >= MetadataTokens.TableCount)
            {
                Throw.TableIndexOutOfRange();
            }

            return reader.TableRowCounts[(int)tableIndex];
        }

        /// <summary>
        /// Returns the size of a row in the specified table.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="reader"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="tableIndex"/> is not a valid table index.</exception>
        public static int GetTableRowSize(this MetadataReader reader, TableIndex tableIndex)
        {
            if (reader is null)
            {
                Throw.ArgumentNull(nameof(reader));
            }

            return tableIndex switch
            {
                TableIndex.Module => reader.ModuleTable.RowSize,
                TableIndex.TypeRef => reader.TypeRefTable.RowSize,
                TableIndex.TypeDef => reader.TypeDefTable.RowSize,
                TableIndex.FieldPtr => reader.FieldPtrTable.RowSize,
                TableIndex.Field => reader.FieldTable.RowSize,
                TableIndex.MethodPtr => reader.MethodPtrTable.RowSize,
                TableIndex.MethodDef => reader.MethodDefTable.RowSize,
                TableIndex.ParamPtr => reader.ParamPtrTable.RowSize,
                TableIndex.Param => reader.ParamTable.RowSize,
                TableIndex.InterfaceImpl => reader.InterfaceImplTable.RowSize,
                TableIndex.MemberRef => reader.MemberRefTable.RowSize,
                TableIndex.Constant => reader.ConstantTable.RowSize,
                TableIndex.CustomAttribute => reader.CustomAttributeTable.RowSize,
                TableIndex.FieldMarshal => reader.FieldMarshalTable.RowSize,
                TableIndex.DeclSecurity => reader.DeclSecurityTable.RowSize,
                TableIndex.ClassLayout => reader.ClassLayoutTable.RowSize,
                TableIndex.FieldLayout => reader.FieldLayoutTable.RowSize,
                TableIndex.StandAloneSig => reader.StandAloneSigTable.RowSize,
                TableIndex.EventMap => reader.EventMapTable.RowSize,
                TableIndex.EventPtr => reader.EventPtrTable.RowSize,
                TableIndex.Event => reader.EventTable.RowSize,
                TableIndex.PropertyMap => reader.PropertyMapTable.RowSize,
                TableIndex.PropertyPtr => reader.PropertyPtrTable.RowSize,
                TableIndex.Property => reader.PropertyTable.RowSize,
                TableIndex.MethodSemantics => reader.MethodSemanticsTable.RowSize,
                TableIndex.MethodImpl => reader.MethodImplTable.RowSize,
                TableIndex.ModuleRef => reader.ModuleRefTable.RowSize,
                TableIndex.TypeSpec => reader.TypeSpecTable.RowSize,
                TableIndex.ImplMap => reader.ImplMapTable.RowSize,
                TableIndex.FieldRva => reader.FieldRvaTable.RowSize,
                TableIndex.EncLog => reader.EncLogTable.RowSize,
                TableIndex.EncMap => reader.EncMapTable.RowSize,
                TableIndex.Assembly => reader.AssemblyTable.RowSize,
                TableIndex.AssemblyProcessor => reader.AssemblyProcessorTable.RowSize,
                TableIndex.AssemblyOS => reader.AssemblyOSTable.RowSize,
                TableIndex.AssemblyRef => reader.AssemblyRefTable.RowSize,
                TableIndex.AssemblyRefProcessor => reader.AssemblyRefProcessorTable.RowSize,
                TableIndex.AssemblyRefOS => reader.AssemblyRefOSTable.RowSize,
                TableIndex.File => reader.FileTable.RowSize,
                TableIndex.ExportedType => reader.ExportedTypeTable.RowSize,
                TableIndex.ManifestResource => reader.ManifestResourceTable.RowSize,
                TableIndex.NestedClass => reader.NestedClassTable.RowSize,
                TableIndex.GenericParam => reader.GenericParamTable.RowSize,
                TableIndex.MethodSpec => reader.MethodSpecTable.RowSize,
                TableIndex.GenericParamConstraint => reader.GenericParamConstraintTable.RowSize,

                // debug tables
                TableIndex.Document => reader.DocumentTable.RowSize,
                TableIndex.MethodDebugInformation => reader.MethodDebugInformationTable.RowSize,
                TableIndex.LocalScope => reader.LocalScopeTable.RowSize,
                TableIndex.LocalVariable => reader.LocalVariableTable.RowSize,
                TableIndex.LocalConstant => reader.LocalConstantTable.RowSize,
                TableIndex.ImportScope => reader.ImportScopeTable.RowSize,
                TableIndex.StateMachineMethod => reader.StateMachineMethodTable.RowSize,
                TableIndex.CustomDebugInformation => reader.CustomDebugInformationTable.RowSize,

                _ => throw new ArgumentOutOfRangeException(nameof(tableIndex)),
            };
        }

        /// <summary>
        /// Returns the offset from the start of metadata to the specified table.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="reader"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="tableIndex"/> is not a valid table index.</exception>
        public static unsafe int GetTableMetadataOffset(this MetadataReader reader, TableIndex tableIndex)
        {
            if (reader is null)
            {
                Throw.ArgumentNull(nameof(reader));
            }

            return (int)(reader.GetTableMetadataBlock(tableIndex).Pointer - reader.Block.Pointer);
        }

        private static MemoryBlock GetTableMetadataBlock(this MetadataReader reader, TableIndex tableIndex)
        {
            Debug.Assert(reader != null);

            return tableIndex switch
            {
                TableIndex.Module => reader.ModuleTable.Block,
                TableIndex.TypeRef => reader.TypeRefTable.Block,
                TableIndex.TypeDef => reader.TypeDefTable.Block,
                TableIndex.FieldPtr => reader.FieldPtrTable.Block,
                TableIndex.Field => reader.FieldTable.Block,
                TableIndex.MethodPtr => reader.MethodPtrTable.Block,
                TableIndex.MethodDef => reader.MethodDefTable.Block,
                TableIndex.ParamPtr => reader.ParamPtrTable.Block,
                TableIndex.Param => reader.ParamTable.Block,
                TableIndex.InterfaceImpl => reader.InterfaceImplTable.Block,
                TableIndex.MemberRef => reader.MemberRefTable.Block,
                TableIndex.Constant => reader.ConstantTable.Block,
                TableIndex.CustomAttribute => reader.CustomAttributeTable.Block,
                TableIndex.FieldMarshal => reader.FieldMarshalTable.Block,
                TableIndex.DeclSecurity => reader.DeclSecurityTable.Block,
                TableIndex.ClassLayout => reader.ClassLayoutTable.Block,
                TableIndex.FieldLayout => reader.FieldLayoutTable.Block,
                TableIndex.StandAloneSig => reader.StandAloneSigTable.Block,
                TableIndex.EventMap => reader.EventMapTable.Block,
                TableIndex.EventPtr => reader.EventPtrTable.Block,
                TableIndex.Event => reader.EventTable.Block,
                TableIndex.PropertyMap => reader.PropertyMapTable.Block,
                TableIndex.PropertyPtr => reader.PropertyPtrTable.Block,
                TableIndex.Property => reader.PropertyTable.Block,
                TableIndex.MethodSemantics => reader.MethodSemanticsTable.Block,
                TableIndex.MethodImpl => reader.MethodImplTable.Block,
                TableIndex.ModuleRef => reader.ModuleRefTable.Block,
                TableIndex.TypeSpec => reader.TypeSpecTable.Block,
                TableIndex.ImplMap => reader.ImplMapTable.Block,
                TableIndex.FieldRva => reader.FieldRvaTable.Block,
                TableIndex.EncLog => reader.EncLogTable.Block,
                TableIndex.EncMap => reader.EncMapTable.Block,
                TableIndex.Assembly => reader.AssemblyTable.Block,
                TableIndex.AssemblyProcessor => reader.AssemblyProcessorTable.Block,
                TableIndex.AssemblyOS => reader.AssemblyOSTable.Block,
                TableIndex.AssemblyRef => reader.AssemblyRefTable.Block,
                TableIndex.AssemblyRefProcessor => reader.AssemblyRefProcessorTable.Block,
                TableIndex.AssemblyRefOS => reader.AssemblyRefOSTable.Block,
                TableIndex.File => reader.FileTable.Block,
                TableIndex.ExportedType => reader.ExportedTypeTable.Block,
                TableIndex.ManifestResource => reader.ManifestResourceTable.Block,
                TableIndex.NestedClass => reader.NestedClassTable.Block,
                TableIndex.GenericParam => reader.GenericParamTable.Block,
                TableIndex.MethodSpec => reader.MethodSpecTable.Block,
                TableIndex.GenericParamConstraint => reader.GenericParamConstraintTable.Block,

                // debug tables
                TableIndex.Document => reader.DocumentTable.Block,
                TableIndex.MethodDebugInformation => reader.MethodDebugInformationTable.Block,
                TableIndex.LocalScope => reader.LocalScopeTable.Block,
                TableIndex.LocalVariable => reader.LocalVariableTable.Block,
                TableIndex.LocalConstant => reader.LocalConstantTable.Block,
                TableIndex.ImportScope => reader.ImportScopeTable.Block,
                TableIndex.StateMachineMethod => reader.StateMachineMethodTable.Block,
                TableIndex.CustomDebugInformation => reader.CustomDebugInformationTable.Block,

                _ => throw new ArgumentOutOfRangeException(nameof(tableIndex)),
            };
        }

        /// <summary>
        /// Returns the size of the specified heap.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="reader"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="heapIndex"/> is not a valid heap index.</exception>
        public static int GetHeapSize(this MetadataReader reader, HeapIndex heapIndex)
        {
            if (reader is null)
            {
                Throw.ArgumentNull(nameof(reader));
            }

            return reader.GetMetadataBlock(heapIndex).Length;
        }

        /// <summary>
        /// Returns the offset from the start of metadata to the specified heap.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="reader"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="heapIndex"/> is not a valid heap index.</exception>
        public static unsafe int GetHeapMetadataOffset(this MetadataReader reader, HeapIndex heapIndex)
        {
            if (reader is null)
            {
                Throw.ArgumentNull(nameof(reader));
            }

            return (int)(reader.GetMetadataBlock(heapIndex).Pointer - reader.Block.Pointer);
        }

        /// <summary>
        /// Returns the size of the specified heap.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="reader"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="heapIndex"/> is not a valid heap index.</exception>
        private static MemoryBlock GetMetadataBlock(this MetadataReader reader, HeapIndex heapIndex)
        {
            Debug.Assert(reader != null);

            return heapIndex switch
            {
                HeapIndex.UserString => reader.UserStringHeap.Block,
                HeapIndex.String => reader.StringHeap.Block,
                HeapIndex.Blob => reader.BlobHeap.Block,
                HeapIndex.Guid => reader.GuidHeap.Block,
                _ => throw new ArgumentOutOfRangeException(nameof(heapIndex)),
            };
        }

        /// <summary>
        /// Returns the a handle to the UserString that follows the given one in the UserString heap or a nil handle if it is the last one.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="reader"/> is null.</exception>
        public static UserStringHandle GetNextHandle(this MetadataReader reader, UserStringHandle handle)
        {
            if (reader is null)
            {
                Throw.ArgumentNull(nameof(reader));
            }

            return reader.UserStringHeap.GetNextHandle(handle);
        }

        /// <summary>
        /// Returns the a handle to the Blob that follows the given one in the Blob heap or a nil handle if it is the last one.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="reader"/> is null.</exception>
        public static BlobHandle GetNextHandle(this MetadataReader reader, BlobHandle handle)
        {
            if (reader is null)
            {
                Throw.ArgumentNull(nameof(reader));
            }

            return reader.BlobHeap.GetNextHandle(handle);
        }

        /// <summary>
        /// Returns the a handle to the String that follows the given one in the String heap or a nil handle if it is the last one.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="reader"/> is null.</exception>
        public static StringHandle GetNextHandle(this MetadataReader reader, StringHandle handle)
        {
            if (reader is null)
            {
                Throw.ArgumentNull(nameof(reader));
            }

            return reader.StringHeap.GetNextHandle(handle);
        }

        /// <summary>
        /// Enumerates entries of EnC log.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="reader"/> is null.</exception>
        public static IEnumerable<EditAndContinueLogEntry> GetEditAndContinueLogEntries(this MetadataReader reader)
        {
            if (reader is null)
            {
                Throw.ArgumentNull(nameof(reader));
            }

            return Core(reader);

            static IEnumerable<EditAndContinueLogEntry> Core(MetadataReader reader)
            {
                for (int rid = 1; rid <= reader.EncLogTable.NumberOfRows; rid++)
                {
                    yield return new EditAndContinueLogEntry(
                        new EntityHandle(reader.EncLogTable.GetToken(rid)),
                        reader.EncLogTable.GetFuncCode(rid));
                }
            }
        }

        /// <summary>
        /// Enumerates entries of EnC map.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="reader"/> is null.</exception>
        public static IEnumerable<EntityHandle> GetEditAndContinueMapEntries(this MetadataReader reader)
        {
            if (reader is null)
            {
                Throw.ArgumentNull(nameof(reader));
            }

            return Core(reader);

            static IEnumerable<EntityHandle> Core(MetadataReader reader)
            {
                for (int rid = 1; rid <= reader.EncMapTable.NumberOfRows; rid++)
                {
                    yield return new EntityHandle(reader.EncMapTable.GetToken(rid));
                }
            }
        }

        /// <summary>
        /// Enumerate types that define one or more properties.
        /// </summary>
        /// <returns>
        /// The resulting sequence corresponds exactly to entries in PropertyMap table,
        /// i.e. n-th returned <see cref="TypeDefinitionHandle"/> is stored in n-th row of PropertyMap.
        /// </returns>
        public static IEnumerable<TypeDefinitionHandle> GetTypesWithProperties(this MetadataReader reader)
        {
            if (reader is null)
            {
                Throw.ArgumentNull(nameof(reader));
            }

            return Core(reader);

            static IEnumerable<TypeDefinitionHandle> Core(MetadataReader reader)
            {
                for (int rid = 1; rid <= reader.PropertyMapTable.NumberOfRows; rid++)
                {
                    yield return reader.PropertyMapTable.GetParentType(rid);
                }
            }
        }

        /// <summary>
        /// Enumerate types that define one or more events.
        /// </summary>
        /// <returns>
        /// The resulting sequence corresponds exactly to entries in EventMap table,
        /// i.e. n-th returned <see cref="TypeDefinitionHandle"/> is stored in n-th row of EventMap.
        /// </returns>
        public static IEnumerable<TypeDefinitionHandle> GetTypesWithEvents(this MetadataReader reader)
        {
            if (reader is null)
            {
                Throw.ArgumentNull(nameof(reader));
            }

            return Core(reader);

            static IEnumerable<TypeDefinitionHandle> Core(MetadataReader reader)
            {
                for (int rid = 1; rid <= reader.EventMapTable.NumberOfRows; rid++)
                {
                    yield return reader.EventMapTable.GetParentType(rid);
                }
            }
        }

        /// <summary>
        /// Given a type handle and a raw type kind found in a signature blob determines whether the target type is a value type or a reference type.
        /// </summary>
        public static SignatureTypeKind ResolveSignatureTypeKind(this MetadataReader reader, EntityHandle typeHandle, byte rawTypeKind)
        {
            if (reader is null)
            {
                Throw.ArgumentNull(nameof(reader));
            }

            var typeKind = (SignatureTypeKind)rawTypeKind;

            switch (typeKind)
            {
                case SignatureTypeKind.Unknown:
                    return SignatureTypeKind.Unknown;

                case SignatureTypeKind.Class:
                case SignatureTypeKind.ValueType:
                    break;

                default:
                    // If read from metadata by the decoder the value would have been checked already.
                    // So it is the callers error to pass in an invalid value, not bad metadata.
                    throw new ArgumentOutOfRangeException(nameof(rawTypeKind));
            }

            switch (typeHandle.Kind)
            {
                case HandleKind.TypeDefinition:
                    // WinRT projections don't apply to TypeDefs
                    return typeKind;

                case HandleKind.TypeReference:
                    var treatment = reader.GetTypeReference((TypeReferenceHandle)typeHandle).SignatureTreatment;
                    return treatment switch
                    {
                        TypeRefSignatureTreatment.ProjectedToClass => SignatureTypeKind.Class,
                        TypeRefSignatureTreatment.ProjectedToValueType => SignatureTypeKind.ValueType,
                        TypeRefSignatureTreatment.None => typeKind,
                        _ => throw ExceptionUtilities.UnexpectedValue(treatment),
                    };
                case HandleKind.TypeSpecification:
                    // TODO: https://github.com/dotnet/runtime/issues/17146
                    // We need more work here in differentiating case because instantiations can project class
                    // to value type as in IReference<T> -> Nullable<T>. Unblocking Roslyn work where the differentiation
                    // feature is not used. Note that the use-case of custom-mods will not hit this because there is no
                    // CLASS | VALUETYPE before the modifier token and so it always comes in unresolved.
                    return SignatureTypeKind.Unknown;

                default:
                    throw new ArgumentOutOfRangeException(nameof(typeHandle), SR.Format(SR.UnexpectedHandleKind, typeHandle.Kind));
            }
        }
    }
}
