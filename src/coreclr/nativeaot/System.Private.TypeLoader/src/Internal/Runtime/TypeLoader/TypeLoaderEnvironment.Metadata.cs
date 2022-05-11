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
using Internal.TypeSystem.NativeFormat;
#if ECMA_METADATA_SUPPORT
using Internal.TypeSystem.Ecma;
#endif

namespace Internal.Runtime.TypeLoader
{
    /// <summary>
    /// This structure represents metadata-based information used to construct method invokers.
    /// TypeLoaderEnvironment.TryGetMethodInvokeMetadata fills in this structure based on metadata lookup across
    /// all currently registered binary modules.
    /// </summary>
    public struct MethodInvokeMetadata
    {
        /// <summary>
        /// module containing the relevant metadata, null when not found
        /// </summary>
        public NativeFormatModuleInfo MappingTableModule;

        /// <summary>
        /// Method entrypoint
        /// </summary>
        public IntPtr MethodEntryPoint;

        /// <summary>
        /// Raw method entrypoint
        /// </summary>
        public IntPtr RawMethodEntryPoint;

        /// <summary>
        /// Method dictionary for components
        /// </summary>
        public IntPtr DictionaryComponent;

        /// <summary>
        /// Dynamic invoke cookie
        /// </summary>
        public uint DynamicInvokeCookie;

        /// <summary>
        /// Invoke flags
        /// </summary>
        public InvokeTableFlags InvokeTableFlags;
    }

    public sealed partial class TypeLoaderEnvironment
    {
        /// <summary>
        /// Compare two arrays sequentially.
        /// </summary>
        /// <param name="seq1">First array to compare</param>
        /// <param name="seq2">Second array to compare</param>
        /// <returns>
        /// true = arrays have the same values and Equals holds for all pairs of elements
        /// with the same indices
        /// </returns>
        private static bool SequenceEqual<T>(T[] seq1, T[] seq2)
        {
            if (seq1.Length != seq2.Length)
                return false;
            for (int i = 0; i < seq1.Length; i++)
                if (!seq1[i].Equals(seq2[i]))
                    return false;
            return true;
        }

        /// <summary>
        /// Locate blob with given ID and create native reader on it.
        /// </summary>
        /// <param name="module">Address of module to search for the blob</param>
        /// <param name="blob">Blob ID within blob map for the module</param>
        /// <returns>Native reader for the blob (asserts and returns an empty native reader when not found)</returns>
        internal static unsafe NativeReader GetNativeReaderForBlob(NativeFormatModuleInfo module, ReflectionMapBlob blob)
        {
            NativeReader reader;
            if (TryGetNativeReaderForBlob(module, blob, out reader))
            {
                return reader;
            }

            Debug.Assert(false);
            return default(NativeReader);
        }

        /// <summary>
        /// Return the metadata handle for a TypeRef if this type was referenced indirectly by other type that pay-for-play has denoted as browsable
        /// (for example, as part of a method signature.)
        ///
        /// This is only used in "debug" builds to provide better MissingMetadataException diagnostics.
        ///
        /// Preconditions:
        ///    runtimeTypeHandle is a typedef (not a constructed type such as an array or generic instance.)
        /// </summary>
        /// <param name="runtimeTypeHandle">MethodTable of the type in question</param>
        /// <param name="metadataReader">Metadata reader for the type</param>
        /// <param name="typeRefHandle">Located TypeRef handle</param>
        public static unsafe bool TryGetTypeReferenceForNamedType(RuntimeTypeHandle runtimeTypeHandle, out MetadataReader metadataReader, out TypeReferenceHandle typeRefHandle)
        {
            int hashCode = runtimeTypeHandle.GetHashCode();

            // Iterate over all modules, starting with the module that defines the MethodTable
            foreach (NativeFormatModuleInfo module in ModuleList.EnumerateModules(RuntimeAugments.GetModuleFromTypeHandle(runtimeTypeHandle)))
            {
                NativeReader typeMapReader;
                if (TryGetNativeReaderForBlob(module, ReflectionMapBlob.TypeMap, out typeMapReader))
                {
                    NativeParser typeMapParser = new NativeParser(typeMapReader, 0);
                    NativeHashtable typeHashtable = new NativeHashtable(typeMapParser);

                    ExternalReferencesTable externalReferences = default(ExternalReferencesTable);
                    externalReferences.InitializeCommonFixupsTable(module);

                    var lookup = typeHashtable.Lookup(hashCode);
                    NativeParser entryParser;
                    while (!(entryParser = lookup.GetNext()).IsNull)
                    {
                        RuntimeTypeHandle foundType = externalReferences.GetRuntimeTypeHandleFromIndex(entryParser.GetUnsigned());
                        if (foundType.Equals(runtimeTypeHandle))
                        {
                            Handle entryMetadataHandle = entryParser.GetUnsigned().AsHandle();
                            if (entryMetadataHandle.HandleType == HandleType.TypeReference)
                            {
                                metadataReader = module.MetadataReader;
                                typeRefHandle = entryMetadataHandle.ToTypeReferenceHandle(metadataReader);
                                return true;
                            }
                        }
                    }
                }
            }

            metadataReader = null;
            typeRefHandle = default(TypeReferenceHandle);

            return false;
        }

        /// <summary>
        /// Return the RuntimeTypeHandle for the named type referenced by another type that pay-for-play denotes as browsable (for example,
        /// in a member signature.) This will only find the typehandle if it is not defined in the current module, and is primarily used
        /// to find non-browsable types.
        ///
        /// This is used to ensure that we can produce a Type object if requested and that it match up with the analogous
        /// Type obtained via typeof().
        ///
        ///
        /// Preconditions:
        ///    metadataReader + typeRefHandle  - a valid metadata reader + typeReferenceHandle where "metadataReader" is one
        ///                                      of the metadata readers returned by ExecutionEnvironment.MetadataReaders.
        ///
        /// Note: Although this method has a "bool" return value like the other mapping table accessors, the pay-for-play design
        /// guarantees that any type that has a metadata TypeReference to it also has a RuntimeTypeHandle underneath.
        /// </summary>
        /// <param name="metadataReader">Metadata reader for module containing the type reference</param>
        /// <param name="typeRefHandle">TypeRef handle to look up</param>
        /// <param name="runtimeTypeHandle">Resolved MethodTable for the type reference</param>
        /// <param name="searchAllModules">Search all modules</param>
        public static unsafe bool TryGetNamedTypeForTypeReference(MetadataReader metadataReader, TypeReferenceHandle typeRefHandle, out RuntimeTypeHandle runtimeTypeHandle, bool searchAllModules = false)
        {
            int hashCode = typeRefHandle.ComputeHashCode(metadataReader);
            NativeFormatModuleInfo typeRefModule = ModuleList.Instance.GetModuleInfoForMetadataReader(metadataReader);
            return TryGetNamedTypeForTypeReference_Inner(metadataReader, typeRefModule, typeRefHandle, hashCode, typeRefModule, out runtimeTypeHandle);
        }

        /// <summary>
        /// Return the RuntimeTypeHandle for the named type referenced by another type that pay-for-play denotes as browsable (for example,
        /// in a member signature.) This lookup will attempt to resolve to an MethodTable in any module to cover situations where the type
        /// does not have a TypeDefinition (non-browsable type) as well as cases where it does.
        ///
        /// Preconditions:
        ///    metadataReader + typeRefHandle  - a valid metadata reader + typeReferenceHandle where "metadataReader" is one
        ///                                      of the metadata readers returned by ExecutionEnvironment.MetadataReaders.
        ///
        /// Note: Although this method has a "bool" return value like the other mapping table accessors, the pay-for-play design
        /// guarantees that any type that has a metadata TypeReference to it also has a RuntimeTypeHandle underneath.
        /// </summary>
        /// <param name="metadataReader">Metadata reader for module containing the type reference</param>
        /// <param name="typeRefHandle">TypeRef handle to look up</param>
        /// <param name="runtimeTypeHandle">Resolved MethodTable for the type reference</param>
        public static unsafe bool TryResolveNamedTypeForTypeReference(MetadataReader metadataReader, TypeReferenceHandle typeRefHandle, out RuntimeTypeHandle runtimeTypeHandle)
        {
            int hashCode = typeRefHandle.ComputeHashCode(metadataReader);
            NativeFormatModuleInfo typeRefModule = ModuleList.Instance.GetModuleInfoForMetadataReader(metadataReader);
            runtimeTypeHandle = default(RuntimeTypeHandle);

            foreach (NativeFormatModuleInfo module in ModuleList.EnumerateModules(typeRefModule.Handle))
            {
                if (TryGetNamedTypeForTypeReference_Inner(metadataReader, typeRefModule, typeRefHandle, hashCode, module, out runtimeTypeHandle))
                    return true;
            }

            return false;
        }

        private static unsafe bool TryGetNamedTypeForTypeReference_Inner(MetadataReader metadataReader,
            NativeFormatModuleInfo typeRefModule,
            TypeReferenceHandle typeRefHandle,
            int hashCode,
            NativeFormatModuleInfo module,
            out RuntimeTypeHandle runtimeTypeHandle)
        {
            Debug.Assert(typeRefModule == ModuleList.Instance.GetModuleInfoForMetadataReader(metadataReader));

            NativeReader typeMapReader;
            if (TryGetNativeReaderForBlob(module, ReflectionMapBlob.TypeMap, out typeMapReader))
            {
                NativeParser typeMapParser = new NativeParser(typeMapReader, 0);
                NativeHashtable typeHashtable = new NativeHashtable(typeMapParser);

                ExternalReferencesTable externalReferences = default(ExternalReferencesTable);
                externalReferences.InitializeCommonFixupsTable(module);

                var lookup = typeHashtable.Lookup(hashCode);
                NativeParser entryParser;
                while (!(entryParser = lookup.GetNext()).IsNull)
                {
                    var foundTypeIndex = entryParser.GetUnsigned();
                    var handle = entryParser.GetUnsigned().AsHandle();

                    if (module == typeRefModule)
                    {
                        if (handle.Equals(typeRefHandle))
                        {
                            runtimeTypeHandle = externalReferences.GetRuntimeTypeHandleFromIndex(foundTypeIndex);
                            return true;
                        }
                    }
                    else if (handle.HandleType == HandleType.TypeReference)
                    {
                        MetadataReader mrFoundHandle = module.MetadataReader;
                        // We found a type reference handle in another module.. see if it matches
                        if (MetadataReaderHelpers.CompareTypeReferenceAcrossModules(typeRefHandle, metadataReader, handle.ToTypeReferenceHandle(mrFoundHandle), mrFoundHandle))
                        {
                            runtimeTypeHandle = externalReferences.GetRuntimeTypeHandleFromIndex(foundTypeIndex);
                            return true;
                        }
                    }
                    else if (handle.HandleType == HandleType.TypeDefinition)
                    {
                        // We found a type definition handle in another module. See if it matches
                        MetadataReader mrFoundHandle = module.MetadataReader;
                        // We found a type definition handle in another module.. see if it matches
                        if (MetadataReaderHelpers.CompareTypeReferenceToDefinition(typeRefHandle, metadataReader, handle.ToTypeDefinitionHandle(mrFoundHandle), mrFoundHandle))
                        {
                            runtimeTypeHandle = externalReferences.GetRuntimeTypeHandleFromIndex(foundTypeIndex);
                            return true;
                        }
                    }
                }
            }

            runtimeTypeHandle = default(RuntimeTypeHandle);
            return false;
        }

        /// <summary>
        /// Given a RuntimeTypeHandle for any non-dynamic type E, return a RuntimeTypeHandle for type E[]
        /// if the pay for play policy denotes E[] as browsable. This is used to implement Array.CreateInstance().
        /// This is not equivalent to calling TryGetMultiDimTypeForElementType() with a rank of 1!
        ///
        /// Preconditions:
        ///     elementTypeHandle is a valid RuntimeTypeHandle.
        /// </summary>
        /// <param name="elementTypeHandle">MethodTable of the array element type</param>
        /// <param name="arrayTypeHandle">Resolved MethodTable of the array type</param>
        public static unsafe bool TryGetArrayTypeForNonDynamicElementType(RuntimeTypeHandle elementTypeHandle, out RuntimeTypeHandle arrayTypeHandle)
        {
            arrayTypeHandle = new RuntimeTypeHandle();

            int arrayHashcode = TypeHashingAlgorithms.ComputeArrayTypeHashCode(elementTypeHandle.GetHashCode(), -1);

            // Note: ReflectionMapBlob.ArrayMap may not exist in the module that contains the element type.
            // So we must enumerate all loaded modules in order to find ArrayMap and the array type for
            // the given element.
            foreach (NativeFormatModuleInfo module in ModuleList.EnumerateModules())
            {
                NativeReader arrayMapReader;
                if (TryGetNativeReaderForBlob(module, ReflectionMapBlob.ArrayMap, out arrayMapReader))
                {
                    NativeParser arrayMapParser = new NativeParser(arrayMapReader, 0);
                    NativeHashtable arrayHashtable = new NativeHashtable(arrayMapParser);

                    ExternalReferencesTable externalReferences = default(ExternalReferencesTable);
                    externalReferences.InitializeCommonFixupsTable(module);

                    var lookup = arrayHashtable.Lookup(arrayHashcode);
                    NativeParser entryParser;
                    while (!(entryParser = lookup.GetNext()).IsNull)
                    {
                        RuntimeTypeHandle foundArrayType = externalReferences.GetRuntimeTypeHandleFromIndex(entryParser.GetUnsigned());
                        RuntimeTypeHandle foundArrayElementType = RuntimeAugments.GetRelatedParameterTypeHandle(foundArrayType);
                        if (foundArrayElementType.Equals(elementTypeHandle))
                        {
                            arrayTypeHandle = foundArrayType;
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// The array table only holds some of the precomputed array types, others may be found in the template type.
        /// As our system requires us to find the RuntimeTypeHandle of templates we must not be in a situation where we fail to find
        /// a RuntimeTypeHandle for a type which is actually in the template table. This function fixes that problem for arrays.
        /// </summary>
        /// <param name="arrayType"></param>
        /// <param name="arrayTypeHandle"></param>
        /// <returns></returns>
        public bool TryGetArrayTypeHandleForNonDynamicArrayTypeFromTemplateTable(ArrayType arrayType, out RuntimeTypeHandle arrayTypeHandle)
        {
            arrayTypeHandle = default(RuntimeTypeHandle);

            // Only SzArray types have templates.
            if (!arrayType.IsSzArray)
                return false;

            // If we can't find a RuntimeTypeHandle for the element type, we can't find the array in the template table.
            if (!arrayType.ParameterType.RetrieveRuntimeTypeHandleIfPossible())
                return false;

            unsafe
            {
                // If the elementType is a dynamic type it cannot exist in the template table.
                if (arrayType.ParameterType.RuntimeTypeHandle.ToEETypePtr()->IsDynamicType)
                    return false;
            }

            // Try to find out if the type exists as a template
            var canonForm = arrayType.ConvertToCanonForm(CanonicalFormKind.Specific);
            var hashCode = canonForm.GetHashCode();
            foreach (var module in ModuleList.EnumerateModules())
            {
                ExternalReferencesTable externalFixupsTable;

                NativeHashtable typeTemplatesHashtable = LoadHashtable(module, ReflectionMapBlob.TypeTemplateMap, out externalFixupsTable);

                if (typeTemplatesHashtable.IsNull)
                    continue;

                var enumerator = typeTemplatesHashtable.Lookup(hashCode);

                NativeParser entryParser;
                while (!(entryParser = enumerator.GetNext()).IsNull)
                {
                    RuntimeTypeHandle candidateTemplateTypeHandle = externalFixupsTable.GetRuntimeTypeHandleFromIndex(entryParser.GetUnsigned());
                    TypeDesc foundType = arrayType.Context.ResolveRuntimeTypeHandle(candidateTemplateTypeHandle);
                    if (foundType == arrayType)
                    {
                        arrayTypeHandle = candidateTemplateTypeHandle;

                        // This lookup in the template table is fairly slow, so if we find the array here, add it to the dynamic array cache, so that
                        // we can find it faster in the future.
                        if (arrayType.IsSzArray)
                            TypeSystemContext.GetArrayTypesCache(false, -1).AddOrGetExisting(arrayTypeHandle);
                        return true;
                    }
                }
            }

            return false;
        }

        // Lazy loadings of hashtables (load on-demand only)
        private static unsafe NativeHashtable LoadHashtable(NativeFormatModuleInfo module, ReflectionMapBlob hashtableBlobId, out ExternalReferencesTable externalFixupsTable)
        {
            // Load the common fixups table
            externalFixupsTable = default(ExternalReferencesTable);
            if (!externalFixupsTable.InitializeCommonFixupsTable(module))
                return default(NativeHashtable);

            // Load the hashtable
            byte* pBlob;
            uint cbBlob;
            if (!module.TryFindBlob(hashtableBlobId, out pBlob, out cbBlob))
                return default(NativeHashtable);

            NativeReader reader = new NativeReader(pBlob, cbBlob);
            NativeParser parser = new NativeParser(reader, 0);
            return new NativeHashtable(parser);
        }

        /// <summary>
        /// Locate the static constructor context given the runtime type handle (MethodTable) for the type in question.
        /// </summary>
        /// <param name="typeHandle">MethodTable of the type to look up</param>
        public static unsafe IntPtr TryGetStaticClassConstructionContext(RuntimeTypeHandle typeHandle)
        {
            if (RuntimeAugments.HasCctor(typeHandle))
            {
                if (RuntimeAugments.IsDynamicType(typeHandle))
                {
                    // For dynamic types, its always possible to get the non-gc static data section directly.
                    byte* ptr = (byte*)Instance.TryGetNonGcStaticFieldData(typeHandle);

                    // what we have now is the base address of the non-gc statics of the type
                    // what we need is the cctor context, which is just before that
                    ptr = ptr - sizeof(System.Runtime.CompilerServices.StaticClassConstructionContext);

                    return (IntPtr)ptr;
                }
                else
                {
                    // Non-dynamic types do not provide a way to directly get at the non-gc static region.
                    // Use the CctorContextMap instead.

                    var moduleHandle = RuntimeAugments.GetModuleFromTypeHandle(typeHandle);
                    NativeFormatModuleInfo module = ModuleList.Instance.GetModuleInfoByHandle(moduleHandle);
                    Debug.Assert(!moduleHandle.IsNull);

                    NativeReader typeMapReader;
                    if (TryGetNativeReaderForBlob(module, ReflectionMapBlob.CCtorContextMap, out typeMapReader))
                    {
                        NativeParser typeMapParser = new NativeParser(typeMapReader, 0);
                        NativeHashtable typeHashtable = new NativeHashtable(typeMapParser);

                        ExternalReferencesTable externalReferences = default(ExternalReferencesTable);
                        externalReferences.InitializeCommonFixupsTable(module);

                        var lookup = typeHashtable.Lookup(typeHandle.GetHashCode());
                        NativeParser entryParser;
                        while (!(entryParser = lookup.GetNext()).IsNull)
                        {
                            RuntimeTypeHandle foundType = externalReferences.GetRuntimeTypeHandleFromIndex(entryParser.GetUnsigned());
                            if (foundType.Equals(typeHandle))
                            {
                                byte* pNonGcStaticBase = (byte*)externalReferences.GetIntPtrFromIndex(entryParser.GetUnsigned());

                                // cctor context is located before the non-GC static base
                                return (IntPtr)(pNonGcStaticBase - sizeof(System.Runtime.CompilerServices.StaticClassConstructionContext));
                            }
                        }
                    }
                }

                // If the type has a lazy/deferred Cctor, the compiler must have missed emitting
                // a data structure if we reach this.
                Debug.Assert(false);
            }

            return IntPtr.Zero;
        }

        /// <summary>
        /// Construct the native reader for a given blob in a specified module.
        /// </summary>
        /// <param name="module">Containing binary module for the blob</param>
        /// <param name="blob">Blob ID to fetch from the module</param>
        /// <param name="reader">Native reader created for the module blob</param>
        /// <returns>true when the blob was found in the module, false when not</returns>
        private static unsafe bool TryGetNativeReaderForBlob(NativeFormatModuleInfo module, ReflectionMapBlob blob, out NativeReader reader)
        {
            byte* pBlob;
            uint cbBlob;

            if (module.TryFindBlob(blob, out pBlob, out cbBlob))
            {
                reader = new NativeReader(pBlob, cbBlob);
                return true;
            }

            reader = default(NativeReader);
            return false;
        }

        /// <summary>
        /// Look up the default constructor for a given type. Should not be called by code which has already initialized
        /// the type system.
        /// </summary>
        /// <param name="type">TypeDesc for the type in question</param>
        /// <returns>Function pointer representing the constructor, IntPtr.Zero when not found</returns>
        internal static IntPtr TryGetDefaultConstructorForType(TypeDesc type)
        {
            // Try to find the default constructor in metadata first
            IntPtr result = IntPtr.Zero;

#if SUPPORTS_NATIVE_METADATA_TYPE_LOADING
            result = TryGetDefaultConstructorForTypeViaMetadata_Inner(type);
#endif

            DefType defType = type as DefType;
            if ((result == IntPtr.Zero) && (defType != null))
            {
#if GENERICS_FORCE_USG
                // In force USG mode, prefer universal matches over canon specific matches.
                CanonicalFormKind firstCanonFormKind = CanonicalFormKind.Universal;
                CanonicalFormKind secondCanonFormKind = CanonicalFormKind.Specific;
#else
                CanonicalFormKind firstCanonFormKind = CanonicalFormKind.Specific;
                CanonicalFormKind secondCanonFormKind = CanonicalFormKind.Universal;
#endif

                CanonicallyEquivalentEntryLocator canonHelperSpecific = new CanonicallyEquivalentEntryLocator(defType, firstCanonFormKind);

                foreach (NativeFormatModuleInfo module in ModuleList.EnumerateModules())
                {
                    result = TryGetDefaultConstructorForType_Inner(module, ref canonHelperSpecific);

                    if (result != IntPtr.Zero)
                        break;
                }

                if (result == IntPtr.Zero)
                {
                    CanonicallyEquivalentEntryLocator canonHelperUniversal = new CanonicallyEquivalentEntryLocator(defType, secondCanonFormKind);

                    foreach (NativeFormatModuleInfo module in ModuleList.EnumerateModules())
                    {
                        result = TryGetDefaultConstructorForType_Inner(module, ref canonHelperUniversal);

                        if (result != IntPtr.Zero)
                            break;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Look up the default constructor for a given type. Should not be called by code which has already initialized
        /// the type system.
        /// </summary>
        /// <param name="runtimeTypeHandle">Type handle (MethodTable) for the type in question</param>
        /// <returns>Function pointer representing the constructor, IntPtr.Zero when not found</returns>
        public IntPtr TryGetDefaultConstructorForType(RuntimeTypeHandle runtimeTypeHandle)
        {
            CanonicallyEquivalentEntryLocator canonHelperSpecific = new CanonicallyEquivalentEntryLocator(runtimeTypeHandle, CanonicalFormKind.Specific);

            foreach (NativeFormatModuleInfo module in ModuleList.EnumerateModules(RuntimeAugments.GetModuleFromTypeHandle(runtimeTypeHandle)))
            {
                IntPtr result = TryGetDefaultConstructorForType_Inner(module, ref canonHelperSpecific);

                if (result != IntPtr.Zero)
                    return result;
            }

            CanonicallyEquivalentEntryLocator canonHelperUniversal = new CanonicallyEquivalentEntryLocator(runtimeTypeHandle, CanonicalFormKind.Universal);

            foreach (NativeFormatModuleInfo module in ModuleList.EnumerateModules(RuntimeAugments.GetModuleFromTypeHandle(runtimeTypeHandle)))
            {
                IntPtr result = TryGetDefaultConstructorForType_Inner(module, ref canonHelperUniversal);

                if (result != IntPtr.Zero)
                    return result;
            }

            // Try to find the default constructor in metadata last (this is costly as it requires spinning up a TypeLoaderContext, and
            // currently also the _typeLoaderLock) (TODO when the _typeLoaderLock is no longer necessary to correctly use the type system
            // context, remove the use of the lock here.)
            using (LockHolder.Hold(_typeLoaderLock))
            {
                TypeSystemContext context = TypeSystemContextFactory.Create();

                TypeDesc type = context.ResolveRuntimeTypeHandle(runtimeTypeHandle);
                IntPtr result = TryGetDefaultConstructorForTypeViaMetadata_Inner(type);

                TypeSystemContextFactory.Recycle(context);

                if (result != IntPtr.Zero)
                    return result;
            }

            return IntPtr.Zero;
        }

        /// <summary>
        /// Lookup default constructor via the typesystem api surface and such
        /// </summary>
        private static IntPtr TryGetDefaultConstructorForTypeViaMetadata_Inner(TypeDesc type)
        {
            IntPtr metadataLookupResult = IntPtr.Zero;

            DefType defType = type as DefType;

            if (defType != null)
            {
                if (!defType.IsValueType && defType is MetadataType)
                {
                    MethodDesc defaultConstructor = ((MetadataType)defType).GetDefaultConstructor();
                    if (defaultConstructor != null)
                    {
                        TypeLoaderEnvironment.TryGetMethodAddressFromMethodDesc(defaultConstructor, out metadataLookupResult, out _, out _);
                    }
                }
            }

            return metadataLookupResult;
        }

        /// <summary>
        /// Attempt to locate the default type constructor in a given module.
        /// </summary>
        /// <param name="mappingTableModule">Module to search for the constructor</param>
        /// <param name="canonHelper">Canonically equivalent entry locator representing the type</param>
        /// <returns>Function pointer representing the constructor, IntPtr.Zero when not found</returns>
        internal static unsafe IntPtr TryGetDefaultConstructorForType_Inner(NativeFormatModuleInfo mappingTableModule, ref CanonicallyEquivalentEntryLocator canonHelper)
        {
            NativeReader invokeMapReader;
            if (TryGetNativeReaderForBlob(mappingTableModule, ReflectionMapBlob.InvokeMap, out invokeMapReader))
            {
                NativeParser invokeMapParser = new NativeParser(invokeMapReader, 0);
                NativeHashtable invokeHashtable = new NativeHashtable(invokeMapParser);

                ExternalReferencesTable externalReferences = default(ExternalReferencesTable);
                externalReferences.InitializeCommonFixupsTable(mappingTableModule);

                var lookup = invokeHashtable.Lookup(canonHelper.LookupHashCode);
                NativeParser entryParser;
                while (!(entryParser = lookup.GetNext()).IsNull)
                {
                    InvokeTableFlags entryFlags = (InvokeTableFlags)entryParser.GetUnsigned();
                    if ((entryFlags & InvokeTableFlags.IsDefaultConstructor) == 0)
                        continue;

                    entryParser.GetUnsigned(); // Skip method handle or the NameAndSig cookie

                    RuntimeTypeHandle entryType = externalReferences.GetRuntimeTypeHandleFromIndex(entryParser.GetUnsigned());
                    if (!canonHelper.IsCanonicallyEquivalent(entryType))
                        continue;

                    return externalReferences.GetFunctionPointerFromIndex(entryParser.GetUnsigned());
                }
            }

            // If not found in the invoke map, try the default constructor map
            NativeReader defaultCtorMapReader;
            if (TryGetNativeReaderForBlob(mappingTableModule, ReflectionMapBlob.DefaultConstructorMap, out defaultCtorMapReader))
            {
                NativeParser defaultCtorMapParser = new NativeParser(defaultCtorMapReader, 0);
                NativeHashtable defaultCtorHashtable = new NativeHashtable(defaultCtorMapParser);

                ExternalReferencesTable externalReferencesForDefaultCtorMap = default(ExternalReferencesTable);
                externalReferencesForDefaultCtorMap.InitializeCommonFixupsTable(mappingTableModule);
                var lookup = defaultCtorHashtable.Lookup(canonHelper.LookupHashCode);
                NativeParser defaultCtorParser;
                while (!(defaultCtorParser = lookup.GetNext()).IsNull)
                {
                    RuntimeTypeHandle entryType = externalReferencesForDefaultCtorMap.GetRuntimeTypeHandleFromIndex(defaultCtorParser.GetUnsigned());
                    if (!canonHelper.IsCanonicallyEquivalent(entryType))
                        continue;

                    return externalReferencesForDefaultCtorMap.GetFunctionPointerFromIndex(defaultCtorParser.GetUnsigned());
                }
            }
            return IntPtr.Zero;
        }

        /// <summary>
        /// Try to resolve a member reference in all registered binary modules containing metadata.
        /// </summary>
        /// <param name="metadataReader">Metadata reader for the member reference</param>
        /// <param name="memberReferenceHandle">Member reference handle (method, field, property, event) to resolve</param>
        /// <param name="resolvedMetadataReader">Metadata reader for the resolved reference</param>
        /// <param name="resolvedContainingTypeHandle">Resolved runtime handle to the containing type</param>
        /// <param name="resolvedMemberHandle">Resolved handle to the referenced member</param>
        /// <returns>true when the lookup was successful; false when not</returns>
        public static bool TryResolveMemberReference(
            MetadataReader metadataReader,
            MemberReferenceHandle memberReferenceHandle,
            out MetadataReader resolvedMetadataReader,
            out RuntimeTypeHandle resolvedContainingTypeHandle,
            out Handle resolvedMemberHandle)
        {
            // TODO
            resolvedMetadataReader = null;
            resolvedContainingTypeHandle = default(RuntimeTypeHandle);
            resolvedMemberHandle = default(Handle);
            return false;
        }

        /// <summary>
        /// Get the information necessary to resolve to metadata given a vtable slot and a type that defines that vtable slot
        /// </summary>
        /// <param name="context">type context to use.</param>
        /// <param name="type">Type that defines the vtable slot. (Derived types are not valid here)</param>
        /// <param name="vtableSlot">vtable slot index</param>
        /// <param name="methodNameAndSig">output name/sig of method</param>
        /// <returns></returns>
        public static unsafe bool TryGetMethodMethodNameAndSigFromVTableSlotForPregeneratedOrTemplateType(TypeSystemContext context, RuntimeTypeHandle type, int vtableSlot, out MethodNameAndSignature methodNameAndSig)
        {
            //
            // See comment in TryGetVirtualResolveData for more details on the semantics of the vtable slot and method declaring type in the VirtualInvokeMap table
            //

            int logicalSlot = vtableSlot;
            MethodTable* ptrType = type.ToEETypePtr();
            RuntimeTypeHandle openOrNonGenericTypeDefinition = default(RuntimeTypeHandle);

            // Compute the logical slot by removing space reserved for generic dictionary pointers
            if (ptrType->IsInterface && ptrType->IsGeneric)
            {
                openOrNonGenericTypeDefinition = RuntimeAugments.GetGenericDefinition(type);
                logicalSlot--;
            }
            else
            {
                MethodTable* searchForSharedGenericTypesInParentHierarchy = ptrType;
                while (searchForSharedGenericTypesInParentHierarchy != null)
                {
                    // See if this type is shared generic. If so, adjust the slot by 1.
                    if (searchForSharedGenericTypesInParentHierarchy->IsGeneric)
                    {
                        RuntimeTypeHandle[] genericTypeArgs;
                        RuntimeTypeHandle genericDefinition = RuntimeAugments.GetGenericInstantiation(searchForSharedGenericTypesInParentHierarchy->ToRuntimeTypeHandle(),
                                                                                                      out genericTypeArgs);

                        if (Instance.ConversionToCanonFormIsAChange(genericTypeArgs, CanonicalFormKind.Specific))
                        {
                            // Shared generic types have a slot dedicated to holding the generic dictionary.
                            logicalSlot--;
                        }
                        if (openOrNonGenericTypeDefinition.IsNull())
                            openOrNonGenericTypeDefinition = genericDefinition;
                    }
                    else if (searchForSharedGenericTypesInParentHierarchy->IsArray)
                    {
                        // Arrays are like shared generics
                        RuntimeTypeHandle arrayElementTypeHandle = searchForSharedGenericTypesInParentHierarchy->RelatedParameterType->ToRuntimeTypeHandle();

                        TypeDesc arrayElementType = context.ResolveRuntimeTypeHandle(arrayElementTypeHandle);
                        TypeDesc canonFormOfArrayElementType = context.ConvertToCanon(arrayElementType, CanonicalFormKind.Specific);

                        if (canonFormOfArrayElementType != arrayElementType)
                        {
                            logicalSlot--;
                        }
                    }

                    // Walk to parent
                    searchForSharedGenericTypesInParentHierarchy = searchForSharedGenericTypesInParentHierarchy->BaseType;
                }
            }

            if (openOrNonGenericTypeDefinition.IsNull())
                openOrNonGenericTypeDefinition = type;

            TypeManagerHandle moduleHandle = RuntimeAugments.GetModuleFromTypeHandle(openOrNonGenericTypeDefinition);
            NativeFormatModuleInfo module = ModuleList.Instance.GetModuleInfoByHandle(moduleHandle);

            return TryGetMethodNameAndSigFromVirtualResolveData(module, openOrNonGenericTypeDefinition, logicalSlot, out methodNameAndSig);
        }

        public struct VirtualResolveDataResult
        {
            public RuntimeTypeHandle DeclaringInvokeType;
            public ushort SlotIndex;
            public RuntimeMethodHandle GVMHandle;
            public bool IsGVM;
        }

        public static bool TryGetVirtualResolveData(NativeFormatModuleInfo module,
            RuntimeTypeHandle methodHandleDeclaringType, RuntimeTypeHandle[] genericArgs,
            ref MethodSignatureComparer methodSignatureComparer,
            out VirtualResolveDataResult lookupResult)
        {
            lookupResult = default(VirtualResolveDataResult);
            NativeReader invokeMapReader = GetNativeReaderForBlob(module, ReflectionMapBlob.VirtualInvokeMap);
            NativeParser invokeMapParser = new NativeParser(invokeMapReader, 0);
            NativeHashtable invokeHashtable = new NativeHashtable(invokeMapParser);
            ExternalReferencesTable externalReferences = default(ExternalReferencesTable);
            externalReferences.InitializeCommonFixupsTable(module);

            //
            // The vtable entries for each instantiated type might not necessarily exist.
            // Example 1:
            //      If there's a call to Foo<string>.Method1 and a call to Foo<int>.Method2, Foo<string> will
            //      not have Method2 in its vtable and Foo<int> will not have Method1.
            // Example 2:
            //      If there's a call to Foo<string>.Method1 and a call to Foo<object>.Method2, given that both
            //      of these instantiations share the same canonical form, Foo<__Canon> will have both method
            //      entries, and therefore Foo<string> and Foo<object> will have both entries too.
            // For this reason, the entries that we write to the map will be based on the canonical form
            // of the method's containing type instead of the open type definition.
            //

            CanonicallyEquivalentEntryLocator canonHelper = new CanonicallyEquivalentEntryLocator(
                methodHandleDeclaringType,
                CanonicalFormKind.Specific);

            var lookup = invokeHashtable.Lookup(canonHelper.LookupHashCode);

            NativeParser entryParser;
            while (!(entryParser = lookup.GetNext()).IsNull)
            {
                // Grammar of an entry in the hash table:
                // Virtual Method uses a normal slot
                // TypeKey + NameAndSig metadata offset into the native layout metadata + (NumberOfStepsUpParentHierarchyToType << 1) + slot
                // OR
                // Generic Virtual Method
                // TypeKey + NameAndSig metadata offset into the native layout metadata + (NumberOfStepsUpParentHierarchyToType << 1 + 1)

                RuntimeTypeHandle entryType = externalReferences.GetRuntimeTypeHandleFromIndex(entryParser.GetUnsigned());
                if (!canonHelper.IsCanonicallyEquivalent(entryType))
                    continue;

                uint nameAndSigPointerToken = entryParser.GetUnsigned();

                MethodNameAndSignature nameAndSig;
                if (!TypeLoaderEnvironment.Instance.TryGetMethodNameAndSignatureFromNativeLayoutOffset(module.Handle, nameAndSigPointerToken, out nameAndSig))
                {
                    Debug.Assert(false);
                    continue;
                }

                if (!methodSignatureComparer.IsMatchingNativeLayoutMethodNameAndSignature(nameAndSig.Name, nameAndSig.Signature))
                {
                    continue;
                }

                uint parentHierarchyAndFlag = entryParser.GetUnsigned();
                uint parentHierarchy = parentHierarchyAndFlag >> 1;
                RuntimeTypeHandle declaringTypeOfVirtualInvoke = methodHandleDeclaringType;
                for (uint iType = 0; iType < parentHierarchy; iType++)
                {
                    if (!RuntimeAugments.TryGetBaseType(declaringTypeOfVirtualInvoke, out declaringTypeOfVirtualInvoke))
                    {
                        Debug.Assert(false); // This will only fail if the virtual invoke data is malformed as specifies that a type
                        // has a deeper inheritance hierarchy than it actually does.
                        return false;
                    }
                }

                bool isGenericVirtualMethod = ((parentHierarchyAndFlag & VirtualInvokeTableEntry.FlagsMask) == VirtualInvokeTableEntry.GenericVirtualMethod);

                Debug.Assert(isGenericVirtualMethod == ((genericArgs != null) && genericArgs.Length > 0));

                if (isGenericVirtualMethod)
                {
                    RuntimeSignature methodName;
                    RuntimeSignature methodSignature;

                    if (!TypeLoaderEnvironment.Instance.TryGetMethodNameAndSignaturePointersFromNativeLayoutSignature(module.Handle, nameAndSigPointerToken, out methodName, out methodSignature))
                    {
                        Debug.Assert(false);
                        return false;
                    }

                    RuntimeMethodHandle gvmSlot = TypeLoaderEnvironment.Instance.GetRuntimeMethodHandleForComponents(declaringTypeOfVirtualInvoke, methodName.NativeLayoutSignature(), methodSignature, genericArgs);

                    lookupResult = new VirtualResolveDataResult
                    {
                        DeclaringInvokeType = declaringTypeOfVirtualInvoke,
                        SlotIndex = 0,
                        GVMHandle = gvmSlot,
                        IsGVM = true
                    };
                    return true;
                }
                else
                {
                    uint slot = entryParser.GetUnsigned();

                    lookupResult = new VirtualResolveDataResult
                    {
                        DeclaringInvokeType = declaringTypeOfVirtualInvoke,
                        SlotIndex = checked((ushort)slot),
                        GVMHandle = default(RuntimeMethodHandle),
                        IsGVM = false
                    };
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Given a virtual logical slot and its open defining type, get information necessary to acquire the associated metadata from the mapping tables.
        /// </summary>
        /// <param name="module">Module to look in</param>
        /// <param name="declaringType">Declaring type that is known to define the slot</param>
        /// <param name="logicalSlot">The logical slot that the method goes in. For this method, the logical
        /// slot is defined as the nth virtual method defined in order on the type (including base types).
        /// VTable slots reserved for dictionary pointers are ignored.</param>
        /// <param name="methodNameAndSig">The name and signature of the method</param>
        /// <returns>true if a definition is found, false if not</returns>
        private static unsafe bool TryGetMethodNameAndSigFromVirtualResolveData(NativeFormatModuleInfo module,
            RuntimeTypeHandle declaringType, int logicalSlot, out MethodNameAndSignature methodNameAndSig)
        {
            //
            // See comment in TryGetVirtualResolveData for more details on the semantics of the vtable slot and method declaring type in the VirtualInvokeMap table
            //

            NativeReader invokeMapReader = GetNativeReaderForBlob(module, ReflectionMapBlob.VirtualInvokeMap);
            NativeParser invokeMapParser = new NativeParser(invokeMapReader, 0);
            NativeHashtable invokeHashtable = new NativeHashtable(invokeMapParser);
            ExternalReferencesTable externalReferences = default(ExternalReferencesTable);
            externalReferences.InitializeCommonFixupsTable(module);

            CanonicallyEquivalentEntryLocator canonHelper = new CanonicallyEquivalentEntryLocator(declaringType, CanonicalFormKind.Specific);

            methodNameAndSig = default(MethodNameAndSignature);

            var lookup = invokeHashtable.Lookup(canonHelper.LookupHashCode);
            NativeParser entryParser;
            while (!(entryParser = lookup.GetNext()).IsNull)
            {
                // Grammar of an entry in the hash table:
                // Virtual Method uses a normal slot
                // TypeKey + NameAndSig metadata offset into the native layout metadata + (NumberOfStepsUpParentHierarchyToType << 1) + slot
                // OR
                // Generic Virtual Method
                // TypeKey + NameAndSig metadata offset into the native layout metadata + (NumberOfStepsUpParentHierarchyToType << 1 + 1)

                RuntimeTypeHandle entryType = externalReferences.GetRuntimeTypeHandleFromIndex(entryParser.GetUnsigned());
                if (!canonHelper.IsCanonicallyEquivalent(entryType))
                    continue;

                uint nameAndSigPointerToken = entryParser.GetUnsigned();

                uint parentHierarchyAndFlag = entryParser.GetUnsigned();
                bool isGenericVirtualMethod = ((parentHierarchyAndFlag & VirtualInvokeTableEntry.FlagsMask) == VirtualInvokeTableEntry.GenericVirtualMethod);

                // We're looking for a method with a specific slot. By definition, it isn't a GVM as we define GVM as not having slots in the vtable
                if (isGenericVirtualMethod)
                    continue;

                uint mappingTableSlot = entryParser.GetUnsigned();

                // Slot doesn't match
                if (logicalSlot != mappingTableSlot)
                    continue;

                if (!TypeLoaderEnvironment.Instance.TryGetMethodNameAndSignatureFromNativeLayoutOffset(module.Handle, nameAndSigPointerToken, out methodNameAndSig))
                {
                    Debug.Assert(false);
                    continue;
                }

                return true;
            }
            return false;
        }

        /// <summary>
        /// Try to look up method invoke info for given canon.
        /// </summary>
        /// <param name="declaringTypeHandle">Declaring type for the method</param>
        /// <param name="methodHandle">Method handle</param>
        /// <param name="genericMethodTypeArgumentHandles">Handles of generic argument types</param>
        /// <param name="methodSignatureComparer">Helper class used to compare method signatures</param>
        /// <param name="canonFormKind">Canonical form to use</param>
        /// <param name="methodInvokeMetadata">Output - metadata information for method invoker construction</param>
        /// <returns>true when found, false otherwise</returns>
        public static bool TryGetMethodInvokeMetadata(
            RuntimeTypeHandle declaringTypeHandle,
            QMethodDefinition methodHandle,
            RuntimeTypeHandle[] genericMethodTypeArgumentHandles,
            ref MethodSignatureComparer methodSignatureComparer,
            CanonicalFormKind canonFormKind,
            out MethodInvokeMetadata methodInvokeMetadata)
        {
            if (methodHandle.IsNativeFormatMetadataBased)
            {
                if (TryGetMethodInvokeMetadataFromInvokeMap(
                    methodHandle.NativeFormatReader,
                    declaringTypeHandle,
                    methodHandle.NativeFormatHandle,
                    genericMethodTypeArgumentHandles,
                    ref methodSignatureComparer,
                    canonFormKind,
                    out methodInvokeMetadata))
                {
                    return true;
                }
            }

            TypeSystemContext context = TypeSystemContextFactory.Create();

            bool success = TryGetMethodInvokeMetadataFromNativeFormatMetadata(
                declaringTypeHandle,
                methodHandle,
                genericMethodTypeArgumentHandles,
                ref methodSignatureComparer,
                context,
                canonFormKind,
                out methodInvokeMetadata);

            TypeSystemContextFactory.Recycle(context);

            return success;
        }

#if SUPPORTS_NATIVE_METADATA_TYPE_LOADING
        /// <summary>
        /// Try to look up method invoke info for given canon in InvokeMap blobs for all available modules.
        /// </summary>
        /// <param name="typicalMethodDesc">Metadata MethodDesc to look for</param>
        /// <param name="method">method to search for</param>
        /// <param name="methodSignatureComparer">Helper class used to compare method signatures</param>
        /// <param name="canonFormKind">Canonical form to use</param>
        /// <param name="methodEntryPoint">Output - Output code address</param>
        /// <param name="foundAddressType">Output - The type of method address match found. A canonical address may require extra parameters to call.</param>
        /// <returns>true when found, false otherwise</returns>
        private static bool TryGetMethodInvokeDataFromInvokeMap(
            NativeFormatMethod typicalMethodDesc,
            MethodDesc method,
            ref MethodSignatureComparer methodSignatureComparer,
            CanonicalFormKind canonFormKind,
            out IntPtr methodEntryPoint,
            out MethodAddressType foundAddressType)
        {
            methodEntryPoint = IntPtr.Zero;
            foundAddressType = MethodAddressType.None;

            CanonicallyEquivalentEntryLocator canonHelper = new CanonicallyEquivalentEntryLocator(method.OwningType.GetClosestDefType(), canonFormKind);

            TypeManagerHandle methodHandleModule = typicalMethodDesc.MetadataUnit.RuntimeModule;

            foreach (NativeFormatModuleInfo module in ModuleList.EnumerateModules(methodHandleModule))
            {
                NativeReader invokeMapReader;
                if (!TryGetNativeReaderForBlob(module, ReflectionMapBlob.InvokeMap, out invokeMapReader))
                {
                    continue;
                }

                NativeParser invokeMapParser = new NativeParser(invokeMapReader, 0);
                NativeHashtable invokeHashtable = new NativeHashtable(invokeMapParser);

                ExternalReferencesTable externalReferences = default(ExternalReferencesTable);
                externalReferences.InitializeCommonFixupsTable(module);

                var lookup = invokeHashtable.Lookup(canonHelper.LookupHashCode);
                var entryData = new InvokeMapEntryDataEnumerator<TypeSystemTypeComparator, bool>(
                    new TypeSystemTypeComparator(method),
                    canonFormKind,
                    module.Handle,
                    typicalMethodDesc.Handle,
                    methodHandleModule);

                NativeParser entryParser;
                while (!(entryParser = lookup.GetNext()).IsNull)
                {
                    entryData.GetNext(ref entryParser, ref externalReferences, ref methodSignatureComparer, canonHelper);

                    if (!entryData.IsMatchingOrCompatibleEntry())
                        continue;

                    IntPtr rawMethodEntryPoint;
                    bool needsDictionaryForCall;

                    if (entryData.GetMethodEntryPoint(
                        out methodEntryPoint,
                        out needsDictionaryForCall,
                        out rawMethodEntryPoint))
                    {
                        // At this time, because we don't have any logic which generates a true fat function pointer
                        // in the TypeSystemTypeComparator, rawMethodEntryPoint should always be the same as methodEntryPoint
                        Debug.Assert(rawMethodEntryPoint == methodEntryPoint);

                        if (canonFormKind == CanonicalFormKind.Universal)
                        {
                            foundAddressType = MethodAddressType.UniversalCanonical;
                        }
                        else
                        {
                            Debug.Assert(canonFormKind == CanonicalFormKind.Specific);

                            if (needsDictionaryForCall)
                            {
                                foundAddressType = MethodAddressType.Canonical;
                            }
                            else
                            {
                                if (method.OwningType.IsValueType && method.OwningType != method.OwningType.ConvertToCanonForm(canonFormKind) && !method.Signature.IsStatic)
                                {
                                    // The entrypoint found is the unboxing stub for a non-generic instance method on a structure
                                    foundAddressType = MethodAddressType.Canonical;
                                }
                                else
                                {
                                    foundAddressType = MethodAddressType.Exact; // We may or may not have found a canonical method here, but if its exactly callable... its close enough
                                }
                            }
                        }
                    }

                    return true;
                }
            }

            return false;
        }
#endif

        /// <summary>
        /// Try to look up method invoke info for given canon in InvokeMap blobs for all available modules.
        /// </summary>
        /// <param name="metadataReader">Metadata reader for the declaring type</param>
        /// <param name="declaringTypeHandle">Declaring type for the method</param>
        /// <param name="methodHandle">Method handle</param>
        /// <param name="genericMethodTypeArgumentHandles">Handles of generic argument types</param>
        /// <param name="methodSignatureComparer">Helper class used to compare method signatures</param>
        /// <param name="canonFormKind">Canonical form to use</param>
        /// <param name="methodInvokeMetadata">Output - metadata information for method invoker construction</param>
        /// <returns>true when found, false otherwise</returns>
        private static bool TryGetMethodInvokeMetadataFromInvokeMap(
            MetadataReader metadataReader,
            RuntimeTypeHandle declaringTypeHandle,
            MethodHandle methodHandle,
            RuntimeTypeHandle[] genericMethodTypeArgumentHandles,
            ref MethodSignatureComparer methodSignatureComparer,
            CanonicalFormKind canonFormKind,
            out MethodInvokeMetadata methodInvokeMetadata)
        {
            CanonicallyEquivalentEntryLocator canonHelper = new CanonicallyEquivalentEntryLocator(declaringTypeHandle, canonFormKind);
            TypeManagerHandle methodHandleModule = ModuleList.Instance.GetModuleForMetadataReader(metadataReader);

            foreach (NativeFormatModuleInfo module in ModuleList.EnumerateModules(RuntimeAugments.GetModuleFromTypeHandle(declaringTypeHandle)))
            {
                NativeReader invokeMapReader;
                if (!TryGetNativeReaderForBlob(module, ReflectionMapBlob.InvokeMap, out invokeMapReader))
                {
                    continue;
                }

                NativeParser invokeMapParser = new NativeParser(invokeMapReader, 0);
                NativeHashtable invokeHashtable = new NativeHashtable(invokeMapParser);

                ExternalReferencesTable externalReferences = default(ExternalReferencesTable);
                externalReferences.InitializeCommonFixupsTable(module);

                var lookup = invokeHashtable.Lookup(canonHelper.LookupHashCode);
                var entryData = new InvokeMapEntryDataEnumerator<PreloadedTypeComparator, IntPtr>(
                    new PreloadedTypeComparator(declaringTypeHandle, genericMethodTypeArgumentHandles),
                    canonFormKind,
                    module.Handle,
                    methodHandle,
                    methodHandleModule);

                NativeParser entryParser;
                while (!(entryParser = lookup.GetNext()).IsNull)
                {
                    entryData.GetNext(ref entryParser, ref externalReferences, ref methodSignatureComparer, canonHelper);

                    if (!entryData.IsMatchingOrCompatibleEntry())
                        continue;

                    if (entryData.GetMethodEntryPoint(
                        out methodInvokeMetadata.MethodEntryPoint,
                        out methodInvokeMetadata.DictionaryComponent,
                        out methodInvokeMetadata.RawMethodEntryPoint))
                    {
                        methodInvokeMetadata.MappingTableModule = module;
                        methodInvokeMetadata.DynamicInvokeCookie = entryData._dynamicInvokeCookie;
                        methodInvokeMetadata.InvokeTableFlags = entryData._flags;

                        return true;
                    }
                }
            }

            methodInvokeMetadata = default(MethodInvokeMetadata);
            return false;
        }

        /// <summary>
        /// Look up method entry point based on native format metadata information.
        /// </summary>
        /// <param name="declaringTypeHandle">Declaring type for the method</param>
        /// <param name="methodHandle">Method handle</param>
        /// <param name="genericMethodTypeArgumentHandles">Handles of generic argument types</param>
        /// <param name="methodSignatureComparer">Helper class used to compare method signatures</param>
        /// <param name="typeSystemContext">Type system context to use</param>
        /// <param name="canonFormKind">Canonical form to use</param>
        /// <param name="methodInvokeMetadata">Output - metadata information for method invoker construction</param>
        /// <returns>true when found, false otherwise</returns>
        private static bool TryGetMethodInvokeMetadataFromNativeFormatMetadata(
            RuntimeTypeHandle declaringTypeHandle,
            QMethodDefinition methodHandle,
            RuntimeTypeHandle[] genericMethodTypeArgumentHandles,
            ref MethodSignatureComparer methodSignatureComparer,
            TypeSystemContext typeSystemContext,
            CanonicalFormKind canonFormKind,
            out MethodInvokeMetadata methodInvokeMetadata)
        {
            methodInvokeMetadata = default(MethodInvokeMetadata);

#if SUPPORTS_NATIVE_METADATA_TYPE_LOADING
            TypeDesc declaringType = typeSystemContext.ResolveRuntimeTypeHandle(declaringTypeHandle);
            TypeDesc declaringTypeDefinition = declaringType.GetTypeDefinition();

            if (declaringTypeDefinition == null)
                return false;


            MethodDesc methodOnType = null;

            if (declaringTypeDefinition is NativeFormatType)
            {
                NativeFormatType nativeFormatType = ((NativeFormatType)declaringTypeDefinition);
                Debug.Assert(methodHandle.NativeFormatReader == nativeFormatType.MetadataReader);
                methodOnType = nativeFormatType.MetadataUnit.GetMethod(methodHandle.NativeFormatHandle, nativeFormatType);
            }
            else
            {
                EcmaType ecmaType = ((EcmaType)declaringTypeDefinition);
                Debug.Assert(methodHandle.EcmaFormatReader == ecmaType.MetadataReader);
                methodOnType = ecmaType.EcmaModule.GetMethod(methodHandle.EcmaFormatHandle);
            }

            if (methodOnType == null)
            {
                return false;
            }

            if (declaringTypeDefinition != declaringType)
            {
                // If we reach here, then the method is on a generic type, and we just found the uninstantiated form
                // Get the method on the instantiated type and continue
                methodOnType = typeSystemContext.GetMethodForInstantiatedType(methodOnType, (InstantiatedType)declaringType);
            }

            if (genericMethodTypeArgumentHandles.Length > 0)
            {
                // If we reach here, this is a generic method, instantiate and continue
                methodOnType = typeSystemContext.GetInstantiatedMethod(methodOnType, typeSystemContext.ResolveRuntimeTypeHandles(genericMethodTypeArgumentHandles));
            }

            IntPtr entryPoint = IntPtr.Zero;
            IntPtr unboxingStubAddress = IntPtr.Zero;
            MethodAddressType foundAddressType = MethodAddressType.None;
#if SUPPORT_DYNAMIC_CODE
            if (foundAddressType == MethodAddressType.None)
                MethodEntrypointStubs.TryGetMethodEntrypoint(methodOnType, out entryPoint, out unboxingStubAddress, out foundAddressType);
#endif
            if (foundAddressType == MethodAddressType.None)
                return false;

            // Only find a universal canon implementation if searching for one
            if (foundAddressType == MethodAddressType.UniversalCanonical &&
                !((canonFormKind == CanonicalFormKind.Universal) || (canonFormKind == CanonicalFormKind.Any)))
            {
                return false;
            }

            // TODO: This will probably require additional work to smoothly use unboxing stubs
            // in vtables - for plain reflection invoke everything seems to work
            // without additional changes thanks to the "NeedsParameterInterpretation" flag.
            if (methodHandle.IsNativeFormatMetadataBased)
                methodInvokeMetadata.MappingTableModule = ((NativeFormatType)declaringTypeDefinition).MetadataUnit.RuntimeModuleInfo;
            else
                methodInvokeMetadata.MappingTableModule = null; // MappingTableModule is only used if NeedsParameterInterpretation isn't set
            methodInvokeMetadata.MethodEntryPoint = entryPoint;
            methodInvokeMetadata.RawMethodEntryPoint = entryPoint;
            // TODO: methodInvokeMetadata.DictionaryComponent
            // TODO: methodInvokeMetadata.DefaultValueString
            // TODO: methodInvokeMetadata.DynamicInvokeCookie

            methodInvokeMetadata.InvokeTableFlags =
                InvokeTableFlags.HasMetadataHandle |
                InvokeTableFlags.HasEntrypoint |
                InvokeTableFlags.NeedsParameterInterpretation;
            if (methodOnType.Signature.GenericParameterCount != 0)
            {
                methodInvokeMetadata.InvokeTableFlags |= InvokeTableFlags.IsGenericMethod;
            }
            if (canonFormKind == CanonicalFormKind.Universal)
            {
                methodInvokeMetadata.InvokeTableFlags |= InvokeTableFlags.IsUniversalCanonicalEntry;
            }
            /* TODO
            if (methodOnType.HasDefaultParameters)
            {
                methodInvokeMetadata.InvokeTableFlags |= InvokeTableFlags.HasDefaultParameters;
            }
            */

            return true;
#else
            return false;
#endif
        }

        // Api surface for controlling invoke map enumeration.
        private interface IInvokeMapEntryDataDeclaringTypeAndGenericMethodParameterHandling<TDictionaryComponentType>
        {
            bool GetTypeDictionary(out TDictionaryComponentType dictionary);
            bool GetMethodDictionary(MethodNameAndSignature nameAndSignature, out TDictionaryComponentType dictionary);
            bool IsUninterestingDictionaryComponent(TDictionaryComponentType dictionary);
            bool CompareMethodInstantiation(RuntimeTypeHandle[] methodInstantiation);
            bool CanInstantiationsShareCode(RuntimeTypeHandle[] methodInstantiation, CanonicalFormKind canonFormKind);
            IntPtr ProduceFatFunctionPointerMethodEntryPoint(IntPtr methodEntrypoint, TDictionaryComponentType dictionary);
        }

        // Comparator for invoke map when used to find an invoke map entry and the search data is a set of
        // pre-loaded types, and metadata handles.
        private struct PreloadedTypeComparator : IInvokeMapEntryDataDeclaringTypeAndGenericMethodParameterHandling<IntPtr>
        {
            private readonly RuntimeTypeHandle _declaringTypeHandle;
            private readonly RuntimeTypeHandle[] _genericMethodTypeArgumentHandles;

            public PreloadedTypeComparator(RuntimeTypeHandle declaringTypeHandle, RuntimeTypeHandle[] genericMethodTypeArgumentHandles)
            {
                _declaringTypeHandle = declaringTypeHandle;
                _genericMethodTypeArgumentHandles = genericMethodTypeArgumentHandles;
            }

            public bool GetTypeDictionary(out IntPtr dictionary)
            {
                dictionary = RuntimeAugments.GetPointerFromTypeHandle(_declaringTypeHandle);
                Debug.Assert(dictionary != IntPtr.Zero);
                return true;
            }

            public bool GetMethodDictionary(MethodNameAndSignature nameAndSignature, out IntPtr dictionary)
            {
                return TypeLoaderEnvironment.Instance.TryGetGenericMethodDictionaryForComponents(_declaringTypeHandle,
                    _genericMethodTypeArgumentHandles,
                    nameAndSignature,
                    out dictionary);
            }

            public bool IsUninterestingDictionaryComponent(IntPtr dictionary)
            {
                return dictionary == IntPtr.Zero;
            }

            public IntPtr ProduceFatFunctionPointerMethodEntryPoint(IntPtr methodEntrypoint, IntPtr dictionary)
            {
                return FunctionPointerOps.GetGenericMethodFunctionPointer(methodEntrypoint, dictionary);
            }

            public bool CompareMethodInstantiation(RuntimeTypeHandle[] methodInstantiation)
            {
                return SequenceEqual(_genericMethodTypeArgumentHandles, methodInstantiation);
            }

            public bool CanInstantiationsShareCode(RuntimeTypeHandle[] methodInstantiation, CanonicalFormKind canonFormKind)
            {
                return TypeLoaderEnvironment.Instance.CanInstantiationsShareCode(methodInstantiation, _genericMethodTypeArgumentHandles, canonFormKind);
            }
        }

        // Comparator for invoke map when used to find an invoke map entry and the search data is
        // a type system object with Metadata
        private struct TypeSystemTypeComparator : IInvokeMapEntryDataDeclaringTypeAndGenericMethodParameterHandling<bool>
        {
            private readonly MethodDesc _targetMethod;

            public TypeSystemTypeComparator(MethodDesc targetMethod)
            {
                _targetMethod = targetMethod;
            }

            public bool GetTypeDictionary(out bool dictionary)
            {
                // The true is to indicate a dictionary is necessary
                dictionary = true;
                return true;
            }

            public bool GetMethodDictionary(MethodNameAndSignature nameAndSignature, out bool dictionary)
            {
                // The true is to indicate a dictionary is necessary
                dictionary = true;
                return true;
            }

            public bool IsUninterestingDictionaryComponent(bool dictionary)
            {
                return dictionary == false;
            }

            public IntPtr ProduceFatFunctionPointerMethodEntryPoint(IntPtr methodEntrypoint, bool dictionary)
            {
                // We don't actually want to produce the fat function pointer here. We want to delay until its actually needed
                return methodEntrypoint;
            }

            public bool CompareMethodInstantiation(RuntimeTypeHandle[] methodInstantiation)
            {
                if (!_targetMethod.HasInstantiation)
                    return false;

                if (_targetMethod.Instantiation.Length != methodInstantiation.Length)
                    return false;

                int i = 0;
                foreach (TypeDesc instantiationType in _targetMethod.Instantiation)
                {
                    TypeDesc genericArg2 = _targetMethod.Context.ResolveRuntimeTypeHandle(methodInstantiation[i]);
                    if (instantiationType != genericArg2)
                    {
                        return false;
                    }
                    i++;
                }

                return true;
            }

            public bool CanInstantiationsShareCode(RuntimeTypeHandle[] methodInstantiation, CanonicalFormKind canonFormKind)
            {
                if (!_targetMethod.HasInstantiation)
                    return false;

                if (_targetMethod.Instantiation.Length != methodInstantiation.Length)
                    return false;

                int i = 0;
                foreach (TypeDesc instantiationType in _targetMethod.Instantiation)
                {
                    TypeSystemContext context = _targetMethod.Context;
                    TypeDesc genericArg2 = context.ResolveRuntimeTypeHandle(methodInstantiation[i]);
                    if (context.ConvertToCanon(instantiationType, canonFormKind) != context.ConvertToCanon(genericArg2, canonFormKind))
                    {
                        return false;
                    }
                    i++;
                }

                return true;
            }
        }

        // Enumerator for discovering methods in the InvokeMap. This is generic to allow highly efficient
        // searching of this table with multiple different input data formats.
        private struct InvokeMapEntryDataEnumerator<TLookupMethodInfo, TDictionaryComponentType> where TLookupMethodInfo : IInvokeMapEntryDataDeclaringTypeAndGenericMethodParameterHandling<TDictionaryComponentType>
        {
            // Read-only inputs
            private TLookupMethodInfo _lookupMethodInfo;
            private readonly CanonicalFormKind _canonFormKind;
            private readonly TypeManagerHandle _moduleHandle;
            private readonly TypeManagerHandle _moduleForMethodHandle;
            private readonly MethodHandle _methodHandle;

            // Parsed data from entry in the hashtable
            public InvokeTableFlags _flags;
            public RuntimeTypeHandle _entryType;
            public IntPtr _methodEntrypoint;
            public uint _dynamicInvokeCookie;
            public IntPtr _entryDictionary;
            public RuntimeTypeHandle[] _methodInstantiation;

            // Computed data
            private bool _hasEntryPoint;
            private bool _isMatchingMethodHandleAndDeclaringType;
            private MethodNameAndSignature _nameAndSignature;
            private RuntimeTypeHandle[] _entryMethodInstantiation;

            public InvokeMapEntryDataEnumerator(
                TLookupMethodInfo lookupMethodInfo,
                CanonicalFormKind canonFormKind,
                TypeManagerHandle moduleHandle,
                MethodHandle methodHandle,
                TypeManagerHandle moduleForMethodHandle)
            {
                _lookupMethodInfo = lookupMethodInfo;
                _canonFormKind = canonFormKind;
                _moduleHandle = moduleHandle;
                _methodHandle = methodHandle;
                _moduleForMethodHandle = moduleForMethodHandle;

                _flags = 0;
                _entryType = default(RuntimeTypeHandle);
                _methodEntrypoint = IntPtr.Zero;
                _dynamicInvokeCookie = 0xffffffff;
                _hasEntryPoint = false;
                _isMatchingMethodHandleAndDeclaringType = false;
                _entryDictionary = IntPtr.Zero;
                _methodInstantiation = null;
                _nameAndSignature = null;
                _entryMethodInstantiation = null;
            }

            public void GetNext(
                ref NativeParser entryParser,
                ref ExternalReferencesTable extRefTable,
                ref MethodSignatureComparer methodSignatureComparer,
                CanonicallyEquivalentEntryLocator canonHelper)
            {
                // Read flags and reset members data
                _flags = (InvokeTableFlags)entryParser.GetUnsigned();
                _hasEntryPoint = ((_flags & InvokeTableFlags.HasEntrypoint) != 0);
                _isMatchingMethodHandleAndDeclaringType = false;
                _entryType = default(RuntimeTypeHandle);
                _methodEntrypoint = IntPtr.Zero;
                _dynamicInvokeCookie = 0xffffffff;
                _entryDictionary = IntPtr.Zero;
                _methodInstantiation = null;
                _nameAndSignature = null;
                _entryMethodInstantiation = null;

                // If the current entry is not a canonical entry of the same canonical form kind we are looking for, then this cannot be a match
                if (((_flags & InvokeTableFlags.IsUniversalCanonicalEntry) != 0) != (_canonFormKind == CanonicalFormKind.Universal))
                    return;

                if ((_flags & InvokeTableFlags.HasMetadataHandle) != 0)
                {
                    // Metadata handles are not known cross module, and cannot be compared across modules.
                    if (_moduleHandle != _moduleForMethodHandle)
                        return;

                    Handle entryMethodHandle = (((uint)HandleType.Method << 24) | entryParser.GetUnsigned()).AsHandle();
                    if (!_methodHandle.Equals(entryMethodHandle))
                        return;
                }
                else
                {
                    uint nameAndSigToken = entryParser.GetUnsigned();
                    MethodNameAndSignature nameAndSig;
                    if (!TypeLoaderEnvironment.Instance.TryGetMethodNameAndSignatureFromNativeLayoutOffset(_moduleHandle, nameAndSigToken, out nameAndSig))
                    {
                        Debug.Assert(false);
                        return;
                    }
                    Debug.Assert(nameAndSig.Signature.IsNativeLayoutSignature);
                    if (!methodSignatureComparer.IsMatchingNativeLayoutMethodNameAndSignature(nameAndSig.Name, nameAndSig.Signature))
                        return;
                }

                _entryType = extRefTable.GetRuntimeTypeHandleFromIndex(entryParser.GetUnsigned());
                if (!canonHelper.IsCanonicallyEquivalent(_entryType))
                    return;

                // Method handle and entry type match at this point. Continue reading data from the entry...
                _isMatchingMethodHandleAndDeclaringType = true;

                if (_hasEntryPoint)
                    _methodEntrypoint = extRefTable.GetFunctionPointerFromIndex(entryParser.GetUnsigned());

                if ((_flags & InvokeTableFlags.NeedsParameterInterpretation) == 0)
                    _dynamicInvokeCookie = entryParser.GetUnsigned();

                if ((_flags & InvokeTableFlags.IsGenericMethod) == 0)
                    return;

                if ((_flags & InvokeTableFlags.IsUniversalCanonicalEntry) != 0)
                {
                    Debug.Assert((_hasEntryPoint || ((_flags & InvokeTableFlags.HasVirtualInvoke) != 0)) && ((_flags & InvokeTableFlags.RequiresInstArg) != 0));

                    uint nameAndSigPointerToken = entryParser.GetUnsigned();
                    if (!TypeLoaderEnvironment.Instance.TryGetMethodNameAndSignatureFromNativeLayoutOffset(_moduleHandle, nameAndSigPointerToken, out _nameAndSignature))
                    {
                        Debug.Assert(false);    //Error
                        _isMatchingMethodHandleAndDeclaringType = false;
                    }
                }
                else if (((_flags & InvokeTableFlags.RequiresInstArg) != 0) && _hasEntryPoint)
                    _entryDictionary = extRefTable.GetGenericDictionaryFromIndex(entryParser.GetUnsigned());
                else
                    _methodInstantiation = GetTypeSequence(ref extRefTable, ref entryParser);
            }

            public bool IsMatchingOrCompatibleEntry()
            {
                // Check if method handle and entry type were matching or compatible
                if (!_isMatchingMethodHandleAndDeclaringType)
                    return false;

                // Nothing special about non-generic methods.
                if ((_flags & InvokeTableFlags.IsGenericMethod) == 0)
                    return true;

                // A universal canonical method entry can share code with any method instantiation (no need to call CanInstantiationsShareCode())
                if ((_flags & InvokeTableFlags.IsUniversalCanonicalEntry) != 0)
                {
                    Debug.Assert(_canonFormKind == CanonicalFormKind.Universal);
                    return true;
                }

                // Generic non-shareable method or abstract methods: check for the canonical equivalency of the method
                // instantiation arguments that we read from the entry
                if (((_flags & InvokeTableFlags.RequiresInstArg) == 0) || !_hasEntryPoint)
                    return _lookupMethodInfo.CanInstantiationsShareCode(_methodInstantiation, _canonFormKind);

                // Generic shareable method: check for canonical equivalency of the method instantiation arguments.
                // The method instantiation arguments are extracted from the generic dictionary pointer that we read from the entry.
                Debug.Assert(_entryDictionary != IntPtr.Zero);
                return GetNameAndSignatureAndMethodInstantiation() && _lookupMethodInfo.CanInstantiationsShareCode(_entryMethodInstantiation, _canonFormKind);
            }

            public bool GetMethodEntryPoint(out IntPtr methodEntrypoint, out TDictionaryComponentType dictionaryComponent, out IntPtr rawMethodEntrypoint)
            {
                // Debug-only sanity check before proceeding (IsMatchingOrCompatibleEntry is called from TryGetDynamicMethodInvokeInfo)
                Debug.Assert(IsMatchingOrCompatibleEntry());

                rawMethodEntrypoint = _methodEntrypoint;
                methodEntrypoint = IntPtr.Zero;

                if (!GetDictionaryComponent(out dictionaryComponent) || !GetMethodEntryPointComponent(dictionaryComponent, out methodEntrypoint))
                    return false;

                return true;
            }

            private bool GetDictionaryComponent(out TDictionaryComponentType dictionaryComponent)
            {
                dictionaryComponent = default(TDictionaryComponentType);

                if (((_flags & InvokeTableFlags.RequiresInstArg) == 0) || !_hasEntryPoint)
                    return true;

                // Dictionary for non-generic method is the type handle of the declaring type
                if ((_flags & InvokeTableFlags.IsGenericMethod) == 0)
                {
                    return _lookupMethodInfo.GetTypeDictionary(out dictionaryComponent);
                }

                // Dictionary for generic method (either found statically or constructed dynamically)
                return GetNameAndSignatureAndMethodInstantiation() && _lookupMethodInfo.GetMethodDictionary(_nameAndSignature, out dictionaryComponent);
            }

            private bool GetMethodEntryPointComponent(TDictionaryComponentType dictionaryComponent, out IntPtr methodEntrypoint)
            {
                methodEntrypoint = _methodEntrypoint;

                if (_lookupMethodInfo.IsUninterestingDictionaryComponent(dictionaryComponent))
                    return true;

                // Do not use a fat function-pointer for universal canonical methods because the converter data block already holds the
                // dictionary pointer so it serves as its own instantiating stub
                if ((_flags & InvokeTableFlags.IsUniversalCanonicalEntry) == 0)
                    methodEntrypoint = _lookupMethodInfo.ProduceFatFunctionPointerMethodEntryPoint(_methodEntrypoint, dictionaryComponent);

                return true;
            }

            private bool GetNameAndSignatureAndMethodInstantiation()
            {
                if (_nameAndSignature != null)
                {
                    Debug.Assert(((_flags & InvokeTableFlags.IsUniversalCanonicalEntry) != 0) || (_entryMethodInstantiation != null && _entryMethodInstantiation.Length > 0));
                    return true;
                }

                if ((_flags & InvokeTableFlags.IsUniversalCanonicalEntry) != 0)
                {
                    // _nameAndSignature should have been read from the InvokeMap entry directly!
                    Debug.Fail("Universal canonical entries do NOT have dictionary entries!");
                    return false;
                }

                RuntimeTypeHandle dummy1;
                bool success = TypeLoaderEnvironment.Instance.TryGetGenericMethodComponents(_entryDictionary, out dummy1, out _nameAndSignature, out _entryMethodInstantiation);
                Debug.Assert(success && dummy1.Equals(_entryType) && _nameAndSignature != null && _entryMethodInstantiation != null && _entryMethodInstantiation.Length > 0);
                return success;
            }
        }

        public static ModuleInfo GetModuleInfoForType(TypeDesc type)
        {
            for (;;)
            {
                type = type.GetTypeDefinition();
#if SUPPORTS_NATIVE_METADATA_TYPE_LOADING
                NativeFormatType nativeType = type as NativeFormatType;
                if (nativeType != null)
                {
                    MetadataReader metadataReader = nativeType.MetadataReader;
                    ModuleInfo moduleInfo = ModuleList.Instance.GetModuleInfoForMetadataReader(metadataReader);

                    return moduleInfo;
                }
#endif
#if ECMA_METADATA_SUPPORT
                Internal.TypeSystem.Ecma.EcmaType ecmaType = type as Internal.TypeSystem.Ecma.EcmaType;
                if (ecmaType != null)
                {
                    return ecmaType.EcmaModule.RuntimeModuleInfo;
                }
#endif
                ArrayType arrayType = type as ArrayType;
                if (arrayType != null)
                {
                    // Arrays are defined in the core shared library
                    return ModuleList.Instance.SystemModule;
                }
                InstantiatedType instantiatedType = type as InstantiatedType;
                if (instantiatedType != null)
                {
                    type = instantiatedType.GetTypeDefinition();
                }
                ParameterizedType parameterizedType = type as ParameterizedType;
                if (parameterizedType != null)
                {
                    type = parameterizedType.ParameterType;
                    continue;
                }
                // Unable to resolve the native type
                return ModuleList.Instance.SystemModule;
            }
        }

        public bool TryGetMetadataForTypeMethodNameAndSignature(RuntimeTypeHandle declaringTypeHandle, MethodNameAndSignature nameAndSignature, out QMethodDefinition methodHandle)
        {
            if (!nameAndSignature.Signature.IsNativeLayoutSignature)
            {
                ModuleInfo moduleInfo = nameAndSignature.Signature.GetModuleInfo();

#if ECMA_METADATA_SUPPORT
                if (moduleInfo is NativeFormatModuleInfo)
#endif
                {
                    methodHandle = new QMethodDefinition(((NativeFormatModuleInfo)moduleInfo).MetadataReader, nameAndSignature.Signature.Token.AsHandle().ToMethodHandle(null));
                }
#if ECMA_METADATA_SUPPORT
                else
                {
                    methodHandle = new QMethodDefinition(((EcmaModuleInfo)moduleInfo).MetadataReader, (System.Reflection.Metadata.MethodDefinitionHandle)System.Reflection.Metadata.Ecma335.MetadataTokens.Handle(nameAndSignature.Signature.Token));
                }
#endif
                // When working with method signature that draw directly from metadata, just return the metadata token
                return true;
            }

            QTypeDefinition qTypeDefinition;
            RuntimeTypeHandle metadataLookupTypeHandle = GetTypeDefinition(declaringTypeHandle);
            methodHandle = default(QMethodDefinition);

            if (!TryGetMetadataForNamedType(metadataLookupTypeHandle, out qTypeDefinition))
                return false;

            MetadataReader reader = qTypeDefinition.NativeFormatReader;
            TypeDefinitionHandle typeDefinitionHandle = qTypeDefinition.NativeFormatHandle;

            TypeDefinition typeDefinition = typeDefinitionHandle.GetTypeDefinition(reader);

            Debug.Assert(nameAndSignature.Signature.IsNativeLayoutSignature);

            foreach (MethodHandle mh in typeDefinition.Methods)
            {
                Method method = mh.GetMethod(reader);
                if (method.Name.StringEquals(nameAndSignature.Name, reader))
                {
                    MethodSignatureComparer methodSignatureComparer = new MethodSignatureComparer(reader, mh);
                    if (methodSignatureComparer.IsMatchingNativeLayoutMethodSignature(nameAndSignature.Signature))
                    {
                        methodHandle = new QMethodDefinition(reader, mh);
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
