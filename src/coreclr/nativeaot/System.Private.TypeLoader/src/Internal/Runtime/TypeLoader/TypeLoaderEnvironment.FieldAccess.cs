// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System;
using System.Runtime;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

using System.Reflection.Runtime.General;

using Internal.Runtime;
using Internal.Runtime.Augments;
using Internal.Runtime.CompilerServices;

using Internal.Metadata.NativeFormat;
using Internal.NativeFormat;
using Internal.TypeSystem;

namespace Internal.Runtime.TypeLoader
{
    /// <summary>
    /// Helper structure describing all info needed to construct dynamic field accessors.
    /// </summary>
    public struct FieldAccessMetadata
    {
        /// <summary>
        /// Module containing the relevant metadata, null when not found
        /// </summary>
        public TypeManagerHandle MappingTableModule;

        /// <summary>
        /// Cookie for field access. This field is set to IntPtr.Zero when the value is not available.
        /// </summary>
        public IntPtr Cookie;

        /// <summary>
        /// Field access and characteristics bitmask.
        /// </summary>
        public FieldTableFlags Flags;

        /// <summary>
        /// Field offset, address or cookie based on field access type.
        /// </summary>
        public int Offset;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct ThreadStaticFieldOffsets
    {
        public uint StartingOffsetInTlsBlock;    // Offset in the TLS block containing the thread static fields of a given type
        public uint FieldOffset;                 // Offset of a thread static field from the start of its containing type's TLS fields block
                                                 // (in other words, the address of a field is 'TLS block + StartingOffsetInTlsBlock + FieldOffset')
    }

    public sealed partial class TypeLoaderEnvironment
    {
        /// <summary>
        /// Try to look up field access info for given canon in metadata blobs for all available modules.
        /// </summary>
        /// <param name="metadataReader">Metadata reader for the declaring type</param>
        /// <param name="runtimeTypeHandle">Declaring type for the method</param>
        /// <param name="fieldHandle">Field handle</param>
        /// <param name="fieldAccessMetadata">Output - metadata information for field accessor construction</param>
        /// <returns>true when found, false otherwise</returns>
        public static bool TryGetFieldAccessMetadata(
            MetadataReader metadataReader,
            RuntimeTypeHandle runtimeTypeHandle,
            FieldHandle fieldHandle,
            out FieldAccessMetadata fieldAccessMetadata)
        {
            fieldAccessMetadata = default(FieldAccessMetadata);

            if (TryGetFieldAccessMetadataFromFieldAccessMap(
                metadataReader,
                runtimeTypeHandle,
                fieldHandle,
                CanonicalFormKind.Specific,
                ref fieldAccessMetadata))
            {
                return true;
            }

            if (TryGetFieldAccessMetadataFromFieldAccessMap(
                metadataReader,
                runtimeTypeHandle,
                fieldHandle,
                CanonicalFormKind.Universal,
                ref fieldAccessMetadata))
            {
                return true;
            }

            TypeSystemContext context = TypeSystemContextFactory.Create();

            bool success = TryGetFieldAccessMetadataFromNativeFormatMetadata(
                metadataReader,
                runtimeTypeHandle,
                fieldHandle,
                context,
                ref fieldAccessMetadata);

            TypeSystemContextFactory.Recycle(context);

            return success;
        }

        /// <summary>
        /// Try to look up field access info for given canon in metadata blobs for all available modules.
        /// </summary>
        /// <param name="metadataReader">Metadata reader for the declaring type</param>
        /// <param name="declaringTypeHandle">Declaring type for the method</param>
        /// <param name="fieldHandle">Field handle</param>
        /// <param name="canonFormKind">Canonical form to use</param>
        /// <param name="fieldAccessMetadata">Output - metadata information for field accessor construction</param>
        /// <returns>true when found, false otherwise</returns>
        private static unsafe bool TryGetFieldAccessMetadataFromFieldAccessMap(
            MetadataReader metadataReader,
            RuntimeTypeHandle declaringTypeHandle,
            FieldHandle fieldHandle,
            CanonicalFormKind canonFormKind,
            ref FieldAccessMetadata fieldAccessMetadata)
        {
            CanonicallyEquivalentEntryLocator canonWrapper = new CanonicallyEquivalentEntryLocator(declaringTypeHandle, canonFormKind);
            string fieldName = null;
            RuntimeTypeHandle declaringTypeHandleDefinition = TypeLoaderEnvironment.GetTypeDefinition(declaringTypeHandle);

            foreach (NativeFormatModuleInfo mappingTableModule in ModuleList.EnumerateModules(RuntimeAugments.GetModuleFromTypeHandle(declaringTypeHandle)))
            {
                NativeReader fieldMapReader;
                if (!TryGetNativeReaderForBlob(mappingTableModule, ReflectionMapBlob.FieldAccessMap, out fieldMapReader))
                    continue;

                NativeParser fieldMapParser = new NativeParser(fieldMapReader, 0);
                NativeHashtable fieldHashtable = new NativeHashtable(fieldMapParser);

                ExternalReferencesTable externalReferences = default(ExternalReferencesTable);
                if (!externalReferences.InitializeCommonFixupsTable(mappingTableModule))
                {
                    continue;
                }

                var lookup = fieldHashtable.Lookup(canonWrapper.LookupHashCode);

                NativeParser entryParser;
                while (!(entryParser = lookup.GetNext()).IsNull)
                {
                    // Grammar of a hash table entry:
                    // Flags + DeclaringType + MdHandle or Name + Cookie or Ordinal or Offset

                    FieldTableFlags entryFlags = (FieldTableFlags)entryParser.GetUnsigned();

                    if ((canonFormKind == CanonicalFormKind.Universal) != ((entryFlags & FieldTableFlags.IsUniversalCanonicalEntry) != 0))
                        continue;

                    RuntimeTypeHandle entryDeclaringTypeHandle = externalReferences.GetRuntimeTypeHandleFromIndex(entryParser.GetUnsigned());
                    if (!entryDeclaringTypeHandle.Equals(declaringTypeHandle)
                        && !canonWrapper.IsCanonicallyEquivalent(entryDeclaringTypeHandle))
                        continue;

                    if ((entryFlags & FieldTableFlags.HasMetadataHandle) != 0)
                    {
                        Handle entryFieldHandle = (((int)HandleType.Field << 24) | (int)entryParser.GetUnsigned()).AsHandle();
                        if (!fieldHandle.Equals(entryFieldHandle))
                            continue;
                    }
                    else
                    {
                        if (fieldName == null)
                        {
                            QTypeDefinition qTypeDefinition;

                            bool success = Instance.TryGetMetadataForNamedType(
                                declaringTypeHandleDefinition,
                                out qTypeDefinition);
                            Debug.Assert(success);

                            MetadataReader nativeFormatMetadataReader = qTypeDefinition.NativeFormatReader;

                            fieldName = nativeFormatMetadataReader.GetString(fieldHandle.GetField(nativeFormatMetadataReader).Name);
                        }

                        string entryFieldName = entryParser.GetString();

                        if (fieldName != entryFieldName)
                            continue;
                    }

                    int fieldOffset;
                    IntPtr fieldAddressCookie = IntPtr.Zero;

                    if (canonFormKind == CanonicalFormKind.Universal)
                    {
                        if (!TypeLoaderEnvironment.Instance.TryGetFieldOffset(declaringTypeHandle, entryParser.GetUnsigned() /* field ordinal */, out fieldOffset))
                        {
                            Debug.Assert(false);
                            return false;
                        }
                    }
                    else
                    {
                        if ((entryFlags & FieldTableFlags.FieldOffsetEncodedDirectly) != 0)
                        {
                            fieldOffset = (int)entryParser.GetUnsigned();
                        }
                        else
                        {
                            fieldOffset = 0;
                            fieldAddressCookie = externalReferences.GetAddressFromIndex(entryParser.GetUnsigned());

                            FieldTableFlags storageClass = entryFlags & FieldTableFlags.StorageClass;
                            if (storageClass == FieldTableFlags.GCStatic || storageClass == FieldTableFlags.ThreadStatic)
                                fieldOffset = (int)entryParser.GetUnsigned();
                        }
                    }

                    fieldAccessMetadata.MappingTableModule = mappingTableModule.Handle;
                    fieldAccessMetadata.Cookie = fieldAddressCookie;
                    fieldAccessMetadata.Flags = entryFlags;
                    fieldAccessMetadata.Offset = fieldOffset;
                    return true;
                }
            }

            return false;
        }

        private enum FieldAccessStaticDataKind
        {
            NonGC,
            GC,
            TLS
        }

        private static class FieldAccessFlags
        {
            public const int RemoteStaticFieldRVA = unchecked((int)0x80000000);
        }

        /// <summary>
        /// This structure describes one static field in an external module. It is represented
        /// by an indirection cell pointer and an offset within the cell - the final address
        /// of the static field is essentially *IndirectionCell + Offset.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct RemoteStaticFieldDescriptor
        {
            public unsafe IntPtr* IndirectionCell;
            public int Offset;
        }

        /// <summary>
        /// Resolve a given 32-bit integer (staticFieldRVA) representing a static field address.
        /// For "local" static fields residing in the module given by moduleHandle, staticFieldRVA
        /// directly contains the RVA of the static field. For remote static fields residing in other
        /// modules, staticFieldRVA has the highest bit set (FieldAccessFlags.RemoteStaticFieldRVA)
        /// and it contains the RVA of a RemoteStaticFieldDescriptor structure residing in the module
        /// given by moduleHandle that holds a pointer to the indirection cell
        /// of the remote static field and its offset within the cell.
        /// </summary>
        /// <param name="moduleHandle">Reference module handle used for static field lookup</param>
        /// <param name="staticFieldRVA">
        /// RVA of static field for local fields; for remote fields, RVA of a RemoteStaticFieldDescriptor
        /// structure for the field or-ed with the FieldAccessFlags.RemoteStaticFieldRVA bit
        /// </param>
        public static unsafe IntPtr RvaToNonGenericStaticFieldAddress(TypeManagerHandle moduleHandle, int staticFieldRVA)
        {
            // TODO: implement
            throw new NotImplementedException();
        }

        /// <summary>
        /// Try to look up non-gc/gc static effective field bases for a non-generic non-dynamic type.
        /// </summary>
        /// <param name="declaringTypeHandle">Declaring type for the method</param>
        /// <param name="fieldAccessKind">type of static base to find</param>
        /// <param name="staticsRegionAddress">Output - statics region address info</param>
        /// <returns>true when found, false otherwise</returns>
        private static unsafe bool TryGetStaticFieldBaseFromFieldAccessMap(
            RuntimeTypeHandle declaringTypeHandle,
            FieldAccessStaticDataKind fieldAccessKind,
            out IntPtr staticsRegionAddress)
        {
            staticsRegionAddress = IntPtr.Zero;
            byte* comparableStaticRegionAddress = null;

            CanonicallyEquivalentEntryLocator canonWrapper = new CanonicallyEquivalentEntryLocator(declaringTypeHandle, CanonicalFormKind.Specific);

            // This function only finds results for non-dynamic, non-generic types
            if (RuntimeAugments.IsDynamicType(declaringTypeHandle) || RuntimeAugments.IsGenericType(declaringTypeHandle))
                return false;

            foreach (NativeFormatModuleInfo mappingTableModule in ModuleList.EnumerateModules(RuntimeAugments.GetModuleFromTypeHandle(declaringTypeHandle)))
            {
                NativeReader fieldMapReader;
                if (!TryGetNativeReaderForBlob(mappingTableModule, ReflectionMapBlob.FieldAccessMap, out fieldMapReader))
                    continue;

                NativeParser fieldMapParser = new NativeParser(fieldMapReader, 0);
                NativeHashtable fieldHashtable = new NativeHashtable(fieldMapParser);

                ExternalReferencesTable externalReferences = default(ExternalReferencesTable);
                if (!externalReferences.InitializeCommonFixupsTable(mappingTableModule))
                {
                    continue;
                }

                var lookup = fieldHashtable.Lookup(canonWrapper.LookupHashCode);

                NativeParser entryParser;
                while (!(entryParser = lookup.GetNext()).IsNull)
                {
                    // Grammar of a hash table entry:
                    // Flags + DeclaringType + MdHandle or Name + Cookie or Ordinal or Offset

                    FieldTableFlags entryFlags = (FieldTableFlags)entryParser.GetUnsigned();
                    FieldTableFlags storageClass = entryFlags & FieldTableFlags.StorageClass;

                    Debug.Assert((entryFlags & FieldTableFlags.IsUniversalCanonicalEntry) == 0);

                    if (storageClass == FieldTableFlags.Instance)
                        continue;

                    switch (fieldAccessKind)
                    {
                        case FieldAccessStaticDataKind.NonGC:
                            if (storageClass != FieldTableFlags.NonGCStatic)
                                continue;
                            break;
                        case FieldAccessStaticDataKind.GC:
                            if (storageClass != FieldTableFlags.GCStatic)
                                continue;
                            break;

                        case FieldAccessStaticDataKind.TLS:
                        default:
                            // TODO! TLS statics
                            Environment.FailFast("TLS static field access not yet supported");
                            return false;
                    }

                    RuntimeTypeHandle entryDeclaringTypeHandle = externalReferences.GetRuntimeTypeHandleFromIndex(entryParser.GetUnsigned());
                    if (!entryDeclaringTypeHandle.Equals(declaringTypeHandle))
                        continue;

                    if ((entryFlags & FieldTableFlags.HasMetadataHandle) != 0)
                    {
                        // skip metadata handle
                        entryParser.GetUnsigned();
                    }
                    else
                    {
                        // skip field name
                        entryParser.SkipString();
                    }

                    int cookieOrOffsetOrOrdinal = (int)entryParser.GetUnsigned();
                    int fieldOffset = (int)externalReferences.GetRvaFromIndex((uint)cookieOrOffsetOrOrdinal);

                    IntPtr fieldAddress = RvaToNonGenericStaticFieldAddress(
                        mappingTableModule.Handle, fieldOffset);

                    if ((comparableStaticRegionAddress == null) || (comparableStaticRegionAddress > fieldAddress.ToPointer()))
                    {
                        comparableStaticRegionAddress = (byte*)fieldAddress.ToPointer();
                    }
                }

                // Static fields for a type can only be found in at most one module
                if (comparableStaticRegionAddress != null)
                    break;
            }

            if (comparableStaticRegionAddress != null)
            {
                staticsRegionAddress = new IntPtr(comparableStaticRegionAddress);
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Try to figure out field access information based on type metadata for native format types.
        /// </summary>
        /// <param name="metadataReader">Metadata reader for the declaring type</param>
        /// <param name="declaringTypeHandle">Declaring type for the method</param>
        /// <param name="fieldHandle">Field handle</param>
        /// <param name="context">Type system context</param>
        /// <param name="fieldAccessMetadata">Output - metadata information for field accessor construction</param>
        /// <returns>true when found, false otherwise</returns>
        private static bool TryGetFieldAccessMetadataFromNativeFormatMetadata(
            MetadataReader metadataReader,
            RuntimeTypeHandle declaringTypeHandle,
            FieldHandle fieldHandle,
            TypeSystemContext context,
            ref FieldAccessMetadata fieldAccessMetadata)
        {
            Field field = metadataReader.GetField(fieldHandle);
            string fieldName = metadataReader.GetString(field.Name);

            TypeDesc declaringType = context.ResolveRuntimeTypeHandle(declaringTypeHandle);

#if SUPPORTS_NATIVE_METADATA_TYPE_LOADING
            if (declaringType is MetadataType)
            {
                return TryGetFieldAccessMetadataForNativeFormatType(declaringType, fieldName, ref fieldAccessMetadata);
            }
#endif

            return false;
        }

        /// <summary>
        /// Locate field on native format type and fill in the field access flags and offset.
        /// </summary>
        /// <param name="type">Metadata reader for the declaring type</param>
        /// <param name="fieldName">Field name</param>
        /// <param name="fieldAccessMetadata">Output - metadata information for field accessor construction</param>
        /// <returns>true when found, false otherwise</returns>
        private static bool TryGetFieldAccessMetadataForNativeFormatType(
            TypeDesc type,
            string fieldName,
            ref FieldAccessMetadata fieldAccessMetadata)
        {
#if SUPPORTS_NATIVE_METADATA_TYPE_LOADING
            FieldDesc fieldDesc = type.GetField(fieldName);
            if (fieldDesc == null)
            {
                return false;
            }

            fieldAccessMetadata.MappingTableModule = default(TypeManagerHandle);

#if SUPPORTS_R2R_LOADING
            fieldAccessMetadata.MappingTableModule = ModuleList.Instance.GetModuleForMetadataReader(((NativeFormatType)type.GetTypeDefinition()).MetadataReader);
#endif
            fieldAccessMetadata.Offset = fieldDesc.Offset.AsInt;
            fieldAccessMetadata.Flags = FieldTableFlags.HasMetadataHandle;

            if (fieldDesc.IsThreadStatic)
            {
                // Specify that the data is thread local
                fieldAccessMetadata.Flags |= FieldTableFlags.ThreadStatic;

                // Specify that the general purpose field access routine that only relies on offset should be used.
                fieldAccessMetadata.Flags |= FieldTableFlags.IsUniversalCanonicalEntry;
            }
            else if (fieldDesc.IsStatic)
            {
                uint nonGcStaticsRVA = 0;
                uint gcStaticsRVA = 0;
                bool nonGenericCase = false;

                if (type is MetadataType)
                {
                    // Static fields on Non-Generic types are contained within the module, and their offsets
                    // are adjusted by their static rva base.
                    nonGenericCase = true;

#if SUPPORTS_R2R_LOADING
                    if (!TryGetStaticsTableEntry((MetadataType)type, nonGcStaticsRVA: out nonGcStaticsRVA, gcStaticsRVA: out gcStaticsRVA))
#endif
                    {
                        Environment.FailFast(
                            "Failed to locate statics table entry for field '" +
                            fieldName +
                            "' on type " +
                            type.ToString());
                    }
                }

                if (fieldDesc.HasGCStaticBase)
                {
                    if ((gcStaticsRVA == 0) && nonGenericCase)
                    {
                        Environment.FailFast(
                            "GC statics region was not found for field '" +
                            fieldName +
                            "' on type " +
                            type.ToString());
                    }
                    fieldAccessMetadata.Offset += (int)gcStaticsRVA;
                    fieldAccessMetadata.Flags |= FieldTableFlags.GCStatic;
                }
                else
                {
                    if ((nonGcStaticsRVA == 0) && nonGenericCase)
                    {
                        Environment.FailFast(
                            "Non-GC statics region was not found for field '" +
                            fieldName +
                            "' on type " +
                            type.ToString());
                    }
                    fieldAccessMetadata.Offset += (int)nonGcStaticsRVA;
                    fieldAccessMetadata.Flags |= FieldTableFlags.NonGCStatic;
                }
                return true;
            }
            else
            {
                // Instance field
                fieldAccessMetadata.Flags |= FieldTableFlags.Instance;
            }

            return true;
#else
            return false;
#endif
        }
    }
}
