// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System;
using System.Diagnostics;
using System.Reflection.Runtime.General;

using Internal.Metadata.NativeFormat;
using Internal.NativeFormat;
using Internal.Runtime.Augments;
using Internal.Runtime.CompilerServices;
using Internal.TypeSystem;

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
        /// Return the metadata handle for a TypeDef if the pay-for-policy enabled this type as browsable. This is used to obtain name and other information for types
        /// obtained via typeof() or Object.GetType(). This can include generic types (not to be confused with generic instances).
        ///
        /// Preconditions:
        ///    runtimeTypeHandle is a typedef (not a constructed type such as an array or generic instance.)
        /// </summary>
        /// <param name="runtimeTypeHandle">Runtime handle of the type in question</param>
        /// <param name="qTypeDefinition">TypeDef handle for the type</param>
        public static unsafe bool TryGetMetadataForNamedType(RuntimeTypeHandle runtimeTypeHandle, out QTypeDefinition qTypeDefinition)
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
                            if (entryMetadataHandle.HandleType == HandleType.TypeDefinition)
                            {
                                MetadataReader metadataReader = module.MetadataReader;
                                qTypeDefinition = new QTypeDefinition(metadataReader, entryMetadataHandle.ToTypeDefinitionHandle(metadataReader));
                                return true;
                            }
                        }
                    }
                }
            }

            qTypeDefinition = default;
            return false;
        }

        /// <summary>
        /// Return the RuntimeTypeHandle for the named type described in metadata. This is used to implement the Create and Invoke
        /// apis for types.
        ///
        /// Preconditions:
        ///    metadataReader + typeDefHandle  - a valid metadata reader + typeDefinitionHandle where "metadataReader" is one
        ///                                      of the metadata readers returned by ExecutionEnvironment.MetadataReaders.
        ///
        /// Note: Although this method has a "bool" return value like the other mapping table accessors, the pay-for-play design
        /// guarantees that any type enabled for metadata also has a RuntimeTypeHandle underneath.
        /// </summary>
        /// <param name="qTypeDefinition">TypeDef handle for the type to look up</param>
        /// <param name="runtimeTypeHandle">Runtime type handle (MethodTable) for the given type</param>
        public static unsafe bool TryGetNamedTypeForMetadata(QTypeDefinition qTypeDefinition, out RuntimeTypeHandle runtimeTypeHandle)
        {
            if (qTypeDefinition.IsNativeFormatMetadataBased)
            {
                MetadataReader metadataReader = qTypeDefinition.NativeFormatReader;
                TypeDefinitionHandle typeDefHandle = qTypeDefinition.NativeFormatHandle;
                int hashCode = typeDefHandle.ComputeHashCode(metadataReader);

                NativeFormatModuleInfo module = ModuleList.Instance.GetModuleInfoForMetadataReader(metadataReader);

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
                        if (entryParser.GetUnsigned().AsHandle().Equals(typeDefHandle))
                        {
                            runtimeTypeHandle = externalReferences.GetRuntimeTypeHandleFromIndex(foundTypeIndex);
                            return true;
                        }
                    }
                }
            }

            runtimeTypeHandle = default;
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
        public static unsafe bool TryGetArrayTypeForNonDynamicElementType(RuntimeTypeHandle elementTypeHandle, bool isMdArray, int rank, out RuntimeTypeHandle arrayTypeHandle)
        {
            arrayTypeHandle = default(RuntimeTypeHandle);

            Debug.Assert(isMdArray || rank == -1);
            int arrayHashcode = VersionResilientHashCode.ArrayTypeHashCode(elementTypeHandle.GetHashCode(), rank == -1 ? 1 : rank);

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
                        if (foundArrayElementType.Equals(elementTypeHandle)
                            && rank == RuntimeAugments.GetArrayRankOrMinusOneForSzArray(foundArrayType))
                        {
                            arrayTypeHandle = foundArrayType;
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public static unsafe bool TryGetByRefTypeForNonDynamicElementType(RuntimeTypeHandle elementTypeHandle, out RuntimeTypeHandle pointerTypeHandle)
        {
            int byRefHashcode = VersionResilientHashCode.ByrefTypeHashCode(elementTypeHandle.GetHashCode());
            return TryGetParameterizedTypeForNonDynamicElementType(elementTypeHandle, byRefHashcode, ReflectionMapBlob.ByRefTypeMap, out pointerTypeHandle);
        }

        public static unsafe bool TryGetPointerTypeForNonDynamicElementType(RuntimeTypeHandle elementTypeHandle, out RuntimeTypeHandle pointerTypeHandle)
        {
            int pointerHashcode = VersionResilientHashCode.PointerTypeHashCode(elementTypeHandle.GetHashCode());
            return TryGetParameterizedTypeForNonDynamicElementType(elementTypeHandle, pointerHashcode, ReflectionMapBlob.PointerTypeMap, out pointerTypeHandle);
        }

        private static unsafe bool TryGetParameterizedTypeForNonDynamicElementType(RuntimeTypeHandle elementTypeHandle, int hashCode, ReflectionMapBlob blob, out RuntimeTypeHandle parameterizedTypeHandle)
        {
            foreach (NativeFormatModuleInfo module in ModuleList.EnumerateModules())
            {
                NativeReader mapReader;
                if (TryGetNativeReaderForBlob(module, blob, out mapReader))
                {
                    NativeParser mapParser = new NativeParser(mapReader, 0);
                    NativeHashtable hashtable = new NativeHashtable(mapParser);

                    ExternalReferencesTable externalReferences = default(ExternalReferencesTable);
                    externalReferences.InitializeCommonFixupsTable(module);

                    var lookup = hashtable.Lookup(hashCode);
                    NativeParser entryParser;
                    while (!(entryParser = lookup.GetNext()).IsNull)
                    {
                        RuntimeTypeHandle foundParameterizedType = externalReferences.GetRuntimeTypeHandleFromIndex(entryParser.GetUnsigned());
                        RuntimeTypeHandle foundElementType = RuntimeAugments.GetRelatedParameterTypeHandle(foundParameterizedType);
                        if (foundElementType.Equals(elementTypeHandle))
                        {
                            parameterizedTypeHandle = foundParameterizedType;
                            return true;
                        }
                    }
                }
            }

            parameterizedTypeHandle = default;
            return false;
        }

        public bool TryGetStaticFunctionPointerTypeForComponents(RuntimeTypeHandle returnTypeHandle, RuntimeTypeHandle[] parameterHandles, bool isUnmanaged, out RuntimeTypeHandle runtimeTypeHandle)
        {
            int hashCode = TypeHashingAlgorithms.ComputeMethodSignatureHashCode(returnTypeHandle.GetHashCode(), parameterHandles);

            foreach (NativeFormatModuleInfo module in ModuleList.EnumerateModules())
            {
                if (TryGetNativeReaderForBlob(module, ReflectionMapBlob.FunctionPointerTypeMap, out NativeReader fnPtrMapReader))
                {
                    NativeParser fnPtrMapParser = new NativeParser(fnPtrMapReader, 0);
                    NativeHashtable fnPtrHashtable = new NativeHashtable(fnPtrMapParser);

                    ExternalReferencesTable externalReferences = default(ExternalReferencesTable);
                    externalReferences.InitializeCommonFixupsTable(module);

                    var lookup = fnPtrHashtable.Lookup(hashCode);
                    NativeParser entryParser;
                    while (!(entryParser = lookup.GetNext()).IsNull)
                    {
                        uint foundFnPtrTypeIndex = entryParser.GetUnsigned();
                        RuntimeTypeHandle foundTypeHandle = externalReferences.GetRuntimeTypeHandleFromIndex(foundFnPtrTypeIndex);

                        if (RuntimeAugments.GetFunctionPointerParameterCount(foundTypeHandle) != parameterHandles.Length)
                            continue;

                        if (!RuntimeAugments.GetFunctionPointerReturnType(foundTypeHandle).Equals(returnTypeHandle))
                            continue;

                        if (RuntimeAugments.IsUnmanagedFunctionPointerType(foundTypeHandle) != isUnmanaged)
                            continue;

                        for (int i = 0; i < parameterHandles.Length; i++)
                            if (!parameterHandles[i].Equals(RuntimeAugments.GetFunctionPointerParameterType(foundTypeHandle, i)))
                                continue;

                        runtimeTypeHandle = foundTypeHandle;
                        return true;
                    }
                }
            }

            runtimeTypeHandle = default;
            return false;
        }

        /// <summary>
        /// Locate the static constructor context given the runtime type handle (MethodTable) for the type in question.
        /// </summary>
        /// <param name="typeHandle">MethodTable of the type to look up</param>
        public static unsafe IntPtr GetStaticClassConstructionContext(RuntimeTypeHandle typeHandle)
        {
            if (RuntimeAugments.IsDynamicType(typeHandle))
            {
                if (!typeHandle.IsDynamicTypeWithCctor())
                    return IntPtr.Zero;

                // For dynamic types, its always possible to get the non-gc static data section directly.
                byte* ptr = (byte*)Instance.TryGetNonGcStaticFieldData(typeHandle);

                // what we have now is the base address of the non-gc statics of the type
                // what we need is the cctor context, which is just before that
                ptr -= sizeof(System.Runtime.CompilerServices.StaticClassConstructionContext);

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
            if (type is DefType defType)
            {
                CanonicallyEquivalentEntryLocator canonHelperSpecific = new CanonicallyEquivalentEntryLocator(defType);

                foreach (NativeFormatModuleInfo module in ModuleList.EnumerateModules())
                {
                    IntPtr result = TryGetDefaultConstructorForType_Inner(module, ref canonHelperSpecific);

                    if (result != IntPtr.Zero)
                        return result;
                }
            }

            return IntPtr.Zero;
        }

        /// <summary>
        /// Look up the default constructor for a given type. Should not be called by code which has already initialized
        /// the type system.
        /// </summary>
        /// <param name="runtimeTypeHandle">Type handle (MethodTable) for the type in question</param>
        /// <returns>Function pointer representing the constructor, IntPtr.Zero when not found</returns>
        public IntPtr TryGetDefaultConstructorForType(RuntimeTypeHandle runtimeTypeHandle)
        {
            CanonicallyEquivalentEntryLocator canonHelperSpecific = new CanonicallyEquivalentEntryLocator(runtimeTypeHandle);

            foreach (NativeFormatModuleInfo module in ModuleList.EnumerateModules(RuntimeAugments.GetModuleFromTypeHandle(runtimeTypeHandle)))
            {
                IntPtr result = TryGetDefaultConstructorForType_Inner(module, ref canonHelperSpecific);

                if (result != IntPtr.Zero)
                    return result;
            }

            return IntPtr.Zero;
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

            return IntPtr.Zero;
        }

        public struct VirtualResolveDataResult
        {
            public RuntimeTypeHandle DeclaringInvokeType;
            public ushort SlotIndex;
            public RuntimeMethodHandle GVMHandle;
            public bool IsGVM;
        }

        public static bool TryGetVirtualResolveData(
            RuntimeTypeHandle methodHandleDeclaringType, QMethodDefinition method, RuntimeTypeHandle[] genericArgs,
            out VirtualResolveDataResult lookupResult)
        {
            NativeFormatModuleInfo module = ModuleList.Instance.GetModuleInfoForMetadataReader(method.NativeFormatReader);

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

            CanonicallyEquivalentEntryLocator canonHelper = new CanonicallyEquivalentEntryLocator(methodHandleDeclaringType);

            var lookup = invokeHashtable.Lookup(canonHelper.LookupHashCode);

            NativeParser entryParser;
            while (!(entryParser = lookup.GetNext()).IsNull)
            {
                // Grammar of an entry in the hash table:
                // Virtual Method uses a normal slot
                // TypeKey + Handle + (NumberOfStepsUpParentHierarchyToType << 1) + slot
                // OR
                // Generic Virtual Method
                // TypeKey + Handle + (NumberOfStepsUpParentHierarchyToType << 1 + 1)

                RuntimeTypeHandle entryType = externalReferences.GetRuntimeTypeHandleFromIndex(entryParser.GetUnsigned());
                if (!canonHelper.IsCanonicallyEquivalent(entryType))
                    continue;

                int token = (int)entryParser.GetUnsigned();

                if (!token.AsHandle().ToMethodHandle(module.MetadataReader).Equals(method.NativeFormatHandle))
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
                    RuntimeMethodHandle gvmSlot = TypeLoaderEnvironment.Instance.GetRuntimeMethodHandleForComponents(declaringTypeOfVirtualInvoke, token, genericArgs);

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
        /// Try to look up method invoke info for given canon.
        /// </summary>
        /// <param name="declaringTypeHandle">Declaring type for the method</param>
        /// <param name="methodHandle">Method handle</param>
        /// <param name="genericMethodTypeArgumentHandles">Handles of generic argument types</param>
        /// <param name="methodInvokeMetadata">Output - metadata information for method invoker construction</param>
        /// <returns>true when found, false otherwise</returns>
        public static bool TryGetMethodInvokeMetadata(
            RuntimeTypeHandle declaringTypeHandle,
            QMethodDefinition methodHandle,
            RuntimeTypeHandle[] genericMethodTypeArgumentHandles,
            out MethodInvokeMetadata methodInvokeMetadata)
        {
            if (methodHandle.IsNativeFormatMetadataBased)
            {
                if (TryGetMethodInvokeMetadataFromInvokeMap(
                    methodHandle.NativeFormatReader,
                    declaringTypeHandle,
                    methodHandle.NativeFormatHandle,
                    genericMethodTypeArgumentHandles,
                    out methodInvokeMetadata))
                {
                    return true;
                }
            }

            methodInvokeMetadata = default;
            return false;
        }

        /// <summary>
        /// Try to look up method invoke info for given canon in InvokeMap blobs for all available modules.
        /// </summary>
        /// <param name="metadataReader">Metadata reader for the declaring type</param>
        /// <param name="declaringTypeHandle">Declaring type for the method</param>
        /// <param name="methodHandle">Method handle</param>
        /// <param name="genericMethodTypeArgumentHandles">Handles of generic argument types</param>
        /// <param name="methodInvokeMetadata">Output - metadata information for method invoker construction</param>
        /// <returns>true when found, false otherwise</returns>
        private static bool TryGetMethodInvokeMetadataFromInvokeMap(
            MetadataReader metadataReader,
            RuntimeTypeHandle declaringTypeHandle,
            MethodHandle methodHandle,
            RuntimeTypeHandle[] genericMethodTypeArgumentHandles,
            out MethodInvokeMetadata methodInvokeMetadata)
        {
            CanonicallyEquivalentEntryLocator canonHelper = new CanonicallyEquivalentEntryLocator(declaringTypeHandle);
            TypeManagerHandle methodHandleModule = ModuleList.Instance.GetModuleForMetadataReader(metadataReader);

            foreach (NativeFormatModuleInfo module in ModuleList.EnumerateModules(RuntimeAugments.GetModuleFromTypeHandle(declaringTypeHandle)))
            {
                // Metadata handles cannot be compared across modules.
                if (module.Handle != methodHandleModule)
                    continue;

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
                NativeParser entryParser;
                while (!(entryParser = lookup.GetNext()).IsNull)
                {
                    InvokeTableFlags flags = (InvokeTableFlags)entryParser.GetUnsigned();
                    Handle entryMethodHandle = (((uint)HandleType.Method << 25) | entryParser.GetUnsigned()).AsHandle();
                    if (!methodHandle.Equals(entryMethodHandle))
                        continue;

                    RuntimeTypeHandle entryType = externalReferences.GetRuntimeTypeHandleFromIndex(entryParser.GetUnsigned());
                    if (!canonHelper.IsCanonicallyEquivalent(entryType))
                        continue;

                    bool hasEntryPoint = (flags & InvokeTableFlags.HasEntrypoint) != 0;
                    IntPtr methodEntryPoint = hasEntryPoint
                        ? externalReferences.GetFunctionPointerFromIndex(entryParser.GetUnsigned())
                        : IntPtr.Zero;
                    uint dynamicInvokeCookie = (flags & InvokeTableFlags.NeedsParameterInterpretation) == 0
                        ? entryParser.GetUnsigned()
                        : uint.MaxValue;

                    bool isGenericMethod = (flags & InvokeTableFlags.IsGenericMethod) != 0;
                    if (isGenericMethod)
                    {
                        RuntimeTypeHandle[] methodInstantiation = GetTypeSequence(ref externalReferences, ref entryParser);
                        if (!TypeLoaderEnvironment.Instance.CanInstantiationsShareCode(methodInstantiation, genericMethodTypeArgumentHandles))
                            continue;
                    }

                    if (hasEntryPoint && (flags & InvokeTableFlags.RequiresInstArg) != 0)
                    {
                        IntPtr dictionary;
                        if (isGenericMethod)
                        {
                            if (!TypeLoaderEnvironment.Instance.TryGetGenericMethodDictionaryForComponents(
                                declaringTypeHandle, genericMethodTypeArgumentHandles,
                                new MethodNameAndSignature(metadataReader, methodHandle), out dictionary))
                                continue;
                        }
                        else
                        {
                            dictionary = RuntimeAugments.GetPointerFromTypeHandle(declaringTypeHandle);
                            Debug.Assert(dictionary != IntPtr.Zero);
                        }

                        if (dictionary != IntPtr.Zero)
                            methodEntryPoint = FunctionPointerOps.GetGenericMethodFunctionPointer(methodEntryPoint, dictionary);
                    }

                    methodInvokeMetadata = new MethodInvokeMetadata
                    {
                        MappingTableModule = module,
                        MethodEntryPoint = methodEntryPoint,
                        DynamicInvokeCookie = dynamicInvokeCookie,
                        InvokeTableFlags = flags,
                    };
                    return true;
                }
            }

            methodInvokeMetadata = default;
            return false;
        }
    }
}
