// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System;
using System.Diagnostics;
using System.Reflection.Runtime.General;

using Internal.Metadata.NativeFormat;
using Internal.NativeFormat;
using Internal.Runtime.Augments;
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

    public sealed partial class TypeLoaderEnvironment
    {
        /// <summary>
        /// Try to look up field access info for given canon in metadata blobs for all available modules.
        /// </summary>
        /// <param name="declaringTypeHandle">Declaring type for the method</param>
        /// <param name="fieldHandle">Field handle</param>
        /// <param name="fieldAccessMetadata">Output - metadata information for field accessor construction</param>
        /// <returns>true when found, false otherwise</returns>
        public static unsafe bool TryGetFieldAccessMetadataFromFieldAccessMap(
            RuntimeTypeHandle declaringTypeHandle,
            FieldHandle fieldHandle,
            out FieldAccessMetadata fieldAccessMetadata)
        {
            fieldAccessMetadata = default;

            CanonicallyEquivalentEntryLocator canonWrapper = new CanonicallyEquivalentEntryLocator(declaringTypeHandle);

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

                    RuntimeTypeHandle entryDeclaringTypeHandle = externalReferences.GetRuntimeTypeHandleFromIndex(entryParser.GetUnsigned());
                    if (!entryDeclaringTypeHandle.Equals(declaringTypeHandle)
                        && !canonWrapper.IsCanonicallyEquivalent(entryDeclaringTypeHandle))
                        continue;

                    Handle entryFieldHandle = (((int)HandleType.Field << 25) | (int)entryParser.GetUnsigned()).AsHandle();
                    if (!fieldHandle.Equals(entryFieldHandle))
                        continue;

                    int fieldOffset;
                    IntPtr fieldAddressCookie = IntPtr.Zero;

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

                    fieldAccessMetadata.MappingTableModule = mappingTableModule.Handle;
                    fieldAccessMetadata.Cookie = fieldAddressCookie;
                    fieldAccessMetadata.Flags = entryFlags;
                    fieldAccessMetadata.Offset = fieldOffset;
                    return true;
                }
            }

            return false;
        }
    }
}
