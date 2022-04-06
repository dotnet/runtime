// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System;
using System.Threading;
using System.Collections.Generic;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Reflection.Runtime.General;

using Internal.Runtime;
using Internal.Runtime.Augments;
using Internal.Runtime.CompilerServices;

using Internal.Metadata.NativeFormat;
using Internal.NativeFormat;
using Internal.TypeSystem;
using Internal.TypeSystem.NativeFormat;

using Debug = System.Diagnostics.Debug;

namespace Internal.Runtime.TypeLoader
{
    internal class Callbacks : TypeLoaderCallbacks
    {
        public override bool TryGetConstructedGenericTypeForComponents(RuntimeTypeHandle genericTypeDefinitionHandle, RuntimeTypeHandle[] genericTypeArgumentHandles, out RuntimeTypeHandle runtimeTypeHandle)
        {
            return TypeLoaderEnvironment.Instance.TryGetConstructedGenericTypeForComponents(genericTypeDefinitionHandle, genericTypeArgumentHandles, out runtimeTypeHandle);
        }

        public override int GetThreadStaticsSizeForDynamicType(int index, out int numTlsCells)
        {
            return TypeLoaderEnvironment.Instance.TryGetThreadStaticsSizeForDynamicType(index, out numTlsCells);
        }

        public override IntPtr GenericLookupFromContextAndSignature(IntPtr context, IntPtr signature, out IntPtr auxResult)
        {
            return TypeLoaderEnvironment.Instance.GenericLookupFromContextAndSignature(context, signature, out auxResult);
        }

        public override bool GetRuntimeMethodHandleComponents(RuntimeMethodHandle runtimeMethodHandle, out RuntimeTypeHandle declaringTypeHandle, out MethodNameAndSignature nameAndSignature, out RuntimeTypeHandle[] genericMethodArgs)
        {
            return TypeLoaderEnvironment.Instance.TryGetRuntimeMethodHandleComponents(runtimeMethodHandle, out declaringTypeHandle, out nameAndSignature, out genericMethodArgs);
        }

        public override bool CompareMethodSignatures(RuntimeSignature signature1, RuntimeSignature signature2)
        {
            return TypeLoaderEnvironment.Instance.CompareMethodSignatures(signature1, signature2);
        }

        public override IntPtr TryGetDefaultConstructorForType(RuntimeTypeHandle runtimeTypeHandle)
        {
            return TypeLoaderEnvironment.Instance.TryGetDefaultConstructorForType(runtimeTypeHandle);
        }

        public override IntPtr GetDelegateThunk(Delegate delegateObject, int thunkKind)
        {
            return CallConverterThunk.GetDelegateThunk(delegateObject, thunkKind);
        }

        public override bool TryGetGenericVirtualTargetForTypeAndSlot(RuntimeTypeHandle targetHandle, ref RuntimeTypeHandle declaringType, RuntimeTypeHandle[] genericArguments, ref string methodName, ref RuntimeSignature methodSignature, bool lookForDefaultImplementation, out IntPtr methodPointer, out IntPtr dictionaryPointer, out bool slotUpdated)
        {
            return TypeLoaderEnvironment.Instance.TryGetGenericVirtualTargetForTypeAndSlot(targetHandle, ref declaringType, genericArguments, ref methodName, ref methodSignature, lookForDefaultImplementation, out methodPointer, out dictionaryPointer, out slotUpdated);
        }

        public override bool GetRuntimeFieldHandleComponents(RuntimeFieldHandle runtimeFieldHandle, out RuntimeTypeHandle declaringTypeHandle, out string fieldName)
        {
            return TypeLoaderEnvironment.Instance.TryGetRuntimeFieldHandleComponents(runtimeFieldHandle, out declaringTypeHandle, out fieldName);
        }

        public override IntPtr ConvertUnboxingFunctionPointerToUnderlyingNonUnboxingPointer(IntPtr unboxingFunctionPointer, RuntimeTypeHandle declaringType)
        {
            return TypeLoaderEnvironment.ConvertUnboxingFunctionPointerToUnderlyingNonUnboxingPointer(unboxingFunctionPointer, declaringType);
        }

        public override bool TryGetPointerTypeForTargetType(RuntimeTypeHandle pointeeTypeHandle, out RuntimeTypeHandle pointerTypeHandle)
        {
            return TypeLoaderEnvironment.Instance.TryGetPointerTypeForTargetType(pointeeTypeHandle, out pointerTypeHandle);
        }

        public override bool TryGetArrayTypeForElementType(RuntimeTypeHandle elementTypeHandle, bool isMdArray, int rank, out RuntimeTypeHandle arrayTypeHandle)
        {
            return TypeLoaderEnvironment.Instance.TryGetArrayTypeForElementType(elementTypeHandle, isMdArray, rank, out arrayTypeHandle);
        }

        public override IntPtr UpdateFloatingDictionary(IntPtr context, IntPtr dictionaryPtr)
        {
            return TypeLoaderEnvironment.Instance.UpdateFloatingDictionary(context, dictionaryPtr);
        }

        /// <summary>
        /// Register a new runtime-allocated code thunk in the diagnostic stream.
        /// </summary>
        /// <param name="thunkAddress">Address of thunk to register</param>
        public override void RegisterThunk(IntPtr thunkAddress)
        {
            SerializedDebugData.RegisterTailCallThunk(thunkAddress);
        }
    }

    public static class RuntimeSignatureExtensions
    {
        public static IntPtr NativeLayoutSignature(this RuntimeSignature signature)
        {
            if (!signature.IsNativeLayoutSignature)
                Environment.FailFast("Not a valid native layout signature");

            NativeReader reader = TypeLoaderEnvironment.GetNativeLayoutInfoReader(signature);
            return reader.OffsetToAddress(signature.NativeLayoutOffset);
        }
    }

    public sealed partial class TypeLoaderEnvironment
    {
        [ThreadStatic]
        private static bool t_isReentrant;

        public static TypeLoaderEnvironment Instance { get; } = new TypeLoaderEnvironment();

        /// <summary>
        /// List of loaded binary modules is typically used to locate / process various metadata blobs
        /// and other per-module information.
        /// </summary>
        public readonly ModuleList ModuleList;

        // Cache the NativeReader in each module to avoid looking up the NativeLayoutInfo blob each
        // time we call GetNativeLayoutInfoReader(). The dictionary is a thread static variable to ensure
        // thread safety. Using ThreadStatic instead of a lock is ok as long as the NativeReader class is
        // small enough in size (which is the case today).
        [ThreadStatic]
        private static LowLevelDictionary<TypeManagerHandle, NativeReader> t_moduleNativeReaders;

        // Eager initialization called from LibraryInitializer for the assembly.
        internal static void Initialize()
        {
            RuntimeAugments.InitializeLookups(new Callbacks());
        }

        public TypeLoaderEnvironment()
        {
            ModuleList = new ModuleList();
        }

        // To keep the synchronization simple, we execute all type loading under a global lock
        private Lock _typeLoaderLock = new Lock();

        public void VerifyTypeLoaderLockHeld()
        {
            if (!_typeLoaderLock.IsAcquired)
                Environment.FailFast("TypeLoaderLock not held");
        }

        public void RunUnderTypeLoaderLock(Action action)
        {
            using (LockHolder.Hold(_typeLoaderLock))
            {
                action();
            }
        }

        public IntPtr GenericLookupFromContextAndSignature(IntPtr context, IntPtr signature, out IntPtr auxResult)
        {
            IntPtr result;

            using (LockHolder.Hold(_typeLoaderLock))
            {
                try
                {
                    if (t_isReentrant)
                        Environment.FailFast("Reentrant lazy generic lookup");
                    t_isReentrant = true;

                    result = TypeBuilder.BuildGenericLookupTarget(context, signature, out auxResult);

                    t_isReentrant = false;
                }
                catch
                {
                    // Catch and rethrow any exceptions instead of using finally block. Otherwise, filters that are run during
                    // the first pass of exception unwind may hit the re-entrancy fail fast above.

                    // TODO: Convert this to filter for better diagnostics once we switch to Roslyn

                    t_isReentrant = false;
                    throw;
                }
            }

            return result;
        }

        private bool EnsureTypeHandleForType(TypeDesc type)
        {
            if (type.RuntimeTypeHandle.IsNull())
            {
                using (LockHolder.Hold(_typeLoaderLock))
                {
                    // Now that we hold the lock, we may find that existing types can now find
                    // their associated RuntimeTypeHandle. Flush the type builder states as a way
                    // to force the reresolution of RuntimeTypeHandles which couldn't be found before.
                    type.Context.FlushTypeBuilderStates();
                    try
                    {
                        new TypeBuilder().BuildType(type);
                    }
                    catch (TypeBuilder.MissingTemplateException)
                    {
                        return false;
                    }
                }
            }

            // Returned type has to have a valid type handle value
            Debug.Assert(!type.RuntimeTypeHandle.IsNull());
            return !type.RuntimeTypeHandle.IsNull();
        }

        internal TypeDesc GetConstructedTypeFromParserAndNativeLayoutContext(ref NativeParser parser, NativeLayoutInfoLoadContext nativeLayoutContext)
        {
            TypeDesc parsedType = nativeLayoutContext.GetType(ref parser);
            if (parsedType == null)
                return null;

            if (!EnsureTypeHandleForType(parsedType))
                return null;

            return parsedType;
        }

        //
        // Parse a native layout signature pointed to by "signature" in the executable image, optionally using
        // "typeArgs" and "methodArgs" for generic type parameter substitution.  The first field in "signature"
        // must be an encoded type but any data beyond that is user-defined and returned in "remainingSignature"
        //
        internal bool GetTypeFromSignatureAndContext(RuntimeSignature signature, RuntimeTypeHandle[] typeArgs, RuntimeTypeHandle[] methodArgs, out RuntimeTypeHandle createdType, out RuntimeSignature remainingSignature)
        {
            NativeReader reader = GetNativeLayoutInfoReader(signature);
            NativeParser parser = new NativeParser(reader, signature.NativeLayoutOffset);

            bool result = GetTypeFromSignatureAndContext(ref parser, new TypeManagerHandle(signature.ModuleHandle), typeArgs, methodArgs, out createdType);

            remainingSignature = RuntimeSignature.CreateFromNativeLayoutSignature(signature, parser.Offset);

            return result;
        }

        internal bool GetTypeFromSignatureAndContext(ref NativeParser parser, TypeManagerHandle moduleHandle, RuntimeTypeHandle[] typeArgs, RuntimeTypeHandle[] methodArgs, out RuntimeTypeHandle createdType)
        {
            createdType = default(RuntimeTypeHandle);
            TypeSystemContext context = TypeSystemContextFactory.Create();

            TypeDesc parsedType = TryParseNativeSignatureWorker(context, moduleHandle, ref parser, typeArgs, methodArgs, false) as TypeDesc;
            if (parsedType == null)
                return false;

            if (!EnsureTypeHandleForType(parsedType))
                return false;

            createdType = parsedType.RuntimeTypeHandle;

            TypeSystemContextFactory.Recycle(context);
            return true;
        }

        //
        // Parse a native layout signature pointed to by "signature" in the executable image, optionally using
        // "typeArgs" and "methodArgs" for generic type parameter substitution.  The first field in "signature"
        // must be an encoded method but any data beyond that is user-defined and returned in "remainingSignature"
        //
        public bool GetMethodFromSignatureAndContext(RuntimeSignature signature, RuntimeTypeHandle[] typeArgs, RuntimeTypeHandle[] methodArgs, out RuntimeTypeHandle createdType, out MethodNameAndSignature nameAndSignature, out RuntimeTypeHandle[] genericMethodTypeArgumentHandles, out RuntimeSignature remainingSignature)
        {
            NativeReader reader = GetNativeLayoutInfoReader(signature);
            NativeParser parser = new NativeParser(reader, signature.NativeLayoutOffset);

            bool result = GetMethodFromSignatureAndContext(ref parser, new TypeManagerHandle(signature.ModuleHandle), typeArgs, methodArgs, out createdType, out nameAndSignature, out genericMethodTypeArgumentHandles);

            remainingSignature = RuntimeSignature.CreateFromNativeLayoutSignature(signature, parser.Offset);

            return result;
        }

        internal bool GetMethodFromSignatureAndContext(ref NativeParser parser, TypeManagerHandle moduleHandle, RuntimeTypeHandle[] typeArgs, RuntimeTypeHandle[] methodArgs, out RuntimeTypeHandle createdType, out MethodNameAndSignature nameAndSignature, out RuntimeTypeHandle[] genericMethodTypeArgumentHandles)
        {
            createdType = default(RuntimeTypeHandle);
            nameAndSignature = null;
            genericMethodTypeArgumentHandles = null;

            TypeSystemContext context = TypeSystemContextFactory.Create();

            MethodDesc parsedMethod = TryParseNativeSignatureWorker(context, moduleHandle, ref parser, typeArgs, methodArgs, true) as MethodDesc;
            if (parsedMethod == null)
                return false;

            if (!EnsureTypeHandleForType(parsedMethod.OwningType))
                return false;

            createdType = parsedMethod.OwningType.RuntimeTypeHandle;
            nameAndSignature = parsedMethod.NameAndSignature;
            if (!parsedMethod.IsMethodDefinition && parsedMethod.Instantiation.Length > 0)
            {
                genericMethodTypeArgumentHandles = new RuntimeTypeHandle[parsedMethod.Instantiation.Length];
                for (int i = 0; i < parsedMethod.Instantiation.Length; ++i)
                {
                    if (!EnsureTypeHandleForType(parsedMethod.Instantiation[i]))
                        return false;

                    genericMethodTypeArgumentHandles[i] = parsedMethod.Instantiation[i].RuntimeTypeHandle;
                }
            }

            TypeSystemContextFactory.Recycle(context);

            return true;
        }

        //
        // Returns the native layout info reader
        //
        internal static unsafe NativeReader GetNativeLayoutInfoReader(NativeFormatModuleInfo module)
        {
            return GetNativeLayoutInfoReader(module.Handle);
        }

        //
        // Returns the native layout info reader
        //
        internal static unsafe NativeReader GetNativeLayoutInfoReader(RuntimeSignature signature)
        {
            Debug.Assert(signature.IsNativeLayoutSignature);
            return GetNativeLayoutInfoReader(new TypeManagerHandle(signature.ModuleHandle));
        }

        //
        // Returns the native layout info reader
        //
        internal static unsafe NativeReader GetNativeLayoutInfoReader(TypeManagerHandle moduleHandle)
        {
            Debug.Assert(!moduleHandle.IsNull);

            if (t_moduleNativeReaders == null)
                t_moduleNativeReaders = new LowLevelDictionary<TypeManagerHandle, NativeReader>();

            NativeReader result;
            if (t_moduleNativeReaders.TryGetValue(moduleHandle, out result))
                return result;

            byte* pBlob;
            uint cbBlob;
            if (RuntimeAugments.FindBlob(moduleHandle, (int)ReflectionMapBlob.NativeLayoutInfo, new IntPtr(&pBlob), new IntPtr(&cbBlob)))
                result = new NativeReader(pBlob, cbBlob);

            t_moduleNativeReaders.Add(moduleHandle, result);
            return result;
        }

        private static RuntimeTypeHandle[] GetTypeSequence(ref ExternalReferencesTable extRefs, ref NativeParser parser)
        {
            uint count = parser.GetUnsigned();
            RuntimeTypeHandle[] result = new RuntimeTypeHandle[count];
            for (uint i = 0; i < count; i++)
                result[i] = extRefs.GetRuntimeTypeHandleFromIndex(parser.GetUnsigned());

            return result;
        }

        private static RuntimeTypeHandle[] TypeDescsToRuntimeHandles(Instantiation types)
        {
            var result = new RuntimeTypeHandle[types.Length];
            for (int i = 0; i < types.Length; i++)
                result[i] = types[i].RuntimeTypeHandle;

            return result;
        }

        public bool TryGetConstructedGenericTypeForComponents(RuntimeTypeHandle genericTypeDefinitionHandle, RuntimeTypeHandle[] genericTypeArgumentHandles, out RuntimeTypeHandle runtimeTypeHandle)
        {
            if (TryLookupConstructedGenericTypeForComponents(genericTypeDefinitionHandle, genericTypeArgumentHandles, out runtimeTypeHandle))
                return true;

            using (LockHolder.Hold(_typeLoaderLock))
            {
                return TypeBuilder.TryBuildGenericType(genericTypeDefinitionHandle, genericTypeArgumentHandles, out runtimeTypeHandle);
            }
        }

        // Get an array RuntimeTypeHandle given an element's RuntimeTypeHandle and rank. Pass false for isMdArray, and rank == -1 for SzArrays
        public bool TryGetArrayTypeForElementType(RuntimeTypeHandle elementTypeHandle, bool isMdArray, int rank, out RuntimeTypeHandle arrayTypeHandle)
        {
            if (TryGetArrayTypeForElementType_LookupOnly(elementTypeHandle, isMdArray, rank, out arrayTypeHandle))
            {
                return true;
            }

            using (LockHolder.Hold(_typeLoaderLock))
            {
                if (isMdArray && (rank < MDArray.MinRank) && (rank > MDArray.MaxRank))
                {
                    arrayTypeHandle = default(RuntimeTypeHandle);
                    return false;
                }

                if (TypeSystemContext.GetArrayTypesCache(isMdArray, rank).TryGetValue(elementTypeHandle, out arrayTypeHandle))
                    return true;

                return TypeBuilder.TryBuildArrayType(elementTypeHandle, isMdArray, rank, out arrayTypeHandle);
            }
        }

        // Looks up an array RuntimeTypeHandle given an element's RuntimeTypeHandle and rank. A rank of -1 indicates SzArray
        internal static bool TryGetArrayTypeForElementType_LookupOnly(RuntimeTypeHandle elementTypeHandle, bool isMdArray, int rank, out RuntimeTypeHandle arrayTypeHandle)
        {
            if (isMdArray && (rank < MDArray.MinRank) && (rank > MDArray.MaxRank))
            {
                arrayTypeHandle = default(RuntimeTypeHandle);
                return false;
            }

            if (TypeSystemContext.GetArrayTypesCache(isMdArray, rank).TryGetValue(elementTypeHandle, out arrayTypeHandle))
                return true;

            if (!isMdArray &&
                !RuntimeAugments.IsDynamicType(elementTypeHandle) &&
                TryGetArrayTypeForNonDynamicElementType(elementTypeHandle, out arrayTypeHandle))
            {
                TypeSystemContext.GetArrayTypesCache(isMdArray, rank).AddOrGetExisting(arrayTypeHandle);
                return true;
            }

            return false;
        }

        public bool TryGetPointerTypeForTargetType(RuntimeTypeHandle pointeeTypeHandle, out RuntimeTypeHandle pointerTypeHandle)
        {
            // There are no lookups for pointers in static modules. All pointer EETypes will be created at this level.
            // It's possible to have multiple pointer EETypes representing the same pointer type with the same element type
            // The caching of pointer types is done at the reflection layer (in the RuntimeTypeUnifier) and
            // here in the TypeSystemContext layer

            if (TypeSystemContext.PointerTypesCache.TryGetValue(pointeeTypeHandle, out pointerTypeHandle))
                return true;

            using (LockHolder.Hold(_typeLoaderLock))
            {
                return TypeBuilder.TryBuildPointerType(pointeeTypeHandle, out pointerTypeHandle);
            }
        }

        public bool TryGetByRefTypeForTargetType(RuntimeTypeHandle pointeeTypeHandle, out RuntimeTypeHandle byRefTypeHandle)
        {
            // There are no lookups for ByRefs in static modules. All ByRef EETypes will be created at this level.
            // It's possible to have multiple ByRef EETypes representing the same ByRef type with the same element type
            // The caching of ByRef types is done at the reflection layer (in the RuntimeTypeUnifier) and
            // here in the TypeSystemContext layer

            if (TypeSystemContext.ByRefTypesCache.TryGetValue(pointeeTypeHandle, out byRefTypeHandle))
                return true;

            using (LockHolder.Hold(_typeLoaderLock))
            {
                return TypeBuilder.TryBuildByRefType(pointeeTypeHandle, out byRefTypeHandle);
            }
        }

        public int GetCanonicalHashCode(RuntimeTypeHandle typeHandle, CanonicalFormKind kind)
        {
            TypeSystemContext context = TypeSystemContextFactory.Create();
            TypeDesc type = context.ResolveRuntimeTypeHandle(typeHandle);
            int hashCode = type.ConvertToCanonForm(kind).GetHashCode();
            TypeSystemContextFactory.Recycle(context);

            return hashCode;
        }

        private object TryParseNativeSignatureWorker(TypeSystemContext typeSystemContext, TypeManagerHandle moduleHandle, ref NativeParser parser, RuntimeTypeHandle[] typeGenericArgumentHandles, RuntimeTypeHandle[] methodGenericArgumentHandles, bool isMethodSignature)
        {
            Instantiation typeGenericArguments = typeSystemContext.ResolveRuntimeTypeHandles(typeGenericArgumentHandles ?? Array.Empty<RuntimeTypeHandle>());
            Instantiation methodGenericArguments = typeSystemContext.ResolveRuntimeTypeHandles(methodGenericArgumentHandles ?? Array.Empty<RuntimeTypeHandle>());

            NativeLayoutInfoLoadContext nativeLayoutContext = new NativeLayoutInfoLoadContext();
            nativeLayoutContext._module = ModuleList.GetModuleInfoByHandle(moduleHandle);
            nativeLayoutContext._typeSystemContext = typeSystemContext;
            nativeLayoutContext._typeArgumentHandles = typeGenericArguments;
            nativeLayoutContext._methodArgumentHandles = methodGenericArguments;

            if (isMethodSignature)
                return nativeLayoutContext.GetMethod(ref parser);
            else
                return nativeLayoutContext.GetType(ref parser);
        }

        public bool TryGetGenericMethodDictionaryForComponents(RuntimeTypeHandle declaringTypeHandle, RuntimeTypeHandle[] genericMethodArgHandles, MethodNameAndSignature nameAndSignature, out IntPtr methodDictionary)
        {
            if (TryLookupGenericMethodDictionaryForComponents(declaringTypeHandle, nameAndSignature, genericMethodArgHandles, out methodDictionary))
                return true;

            using (LockHolder.Hold(_typeLoaderLock))
            {
                return TypeBuilder.TryBuildGenericMethod(declaringTypeHandle, genericMethodArgHandles, nameAndSignature, out methodDictionary);
            }
        }

        public bool TryGetFieldOffset(RuntimeTypeHandle declaringTypeHandle, uint fieldOrdinal, out int fieldOffset)
        {
            fieldOffset = int.MinValue;

            // No use going further for non-generic types... TypeLoader doesn't have offset answers for non-generic types!
            if (!declaringTypeHandle.IsGenericType())
                return false;

            using (LockHolder.Hold(_typeLoaderLock))
            {
                return TypeBuilder.TryGetFieldOffset(declaringTypeHandle, fieldOrdinal, out fieldOffset);
            }
        }

        public unsafe IntPtr UpdateFloatingDictionary(IntPtr context, IntPtr dictionaryPtr)
        {
            IntPtr newFloatingDictionary;
            bool isNewlyAllocatedDictionary;
            bool isTypeContext = context != dictionaryPtr;

            if (isTypeContext)
            {
                // Look for the exact base type that owns the dictionary. We may be having
                // a virtual method run on a derived type and the generic lookup are performed
                // on the base type's dictionary.
                MethodTable* pEEType = (MethodTable*)context.ToPointer();
                context = (IntPtr)EETypeCreator.GetBaseEETypeForDictionaryPtr(pEEType, dictionaryPtr);
            }

            using (LockHolder.Hold(_typeLoaderLock))
            {
                // Check if some other thread already allocated a floating dictionary and updated the fixed portion
                if (*(IntPtr*)dictionaryPtr != IntPtr.Zero)
                    return *(IntPtr*)dictionaryPtr;

                try
                {
                    if (t_isReentrant)
                        Environment.FailFast("Reentrant update to floating dictionary");
                    t_isReentrant = true;

                    newFloatingDictionary = TypeBuilder.TryBuildFloatingDictionary(context, isTypeContext, dictionaryPtr, out isNewlyAllocatedDictionary);

                    t_isReentrant = false;
                }
                catch
                {
                    // Catch and rethrow any exceptions instead of using finally block. Otherwise, filters that are run during
                    // the first pass of exception unwind may hit the re-entrancy fail fast above.

                    // TODO: Convert this to filter for better diagnostics once we switch to Roslyn

                    t_isReentrant = false;
                    throw;
                }
            }

            if (newFloatingDictionary == IntPtr.Zero)
            {
                Environment.FailFast("Unable to update floating dictionary");
                return IntPtr.Zero;
            }

            // The pointer to the floating dictionary is the first slot of the fixed dictionary.
            if (Interlocked.CompareExchange(ref *(IntPtr*)dictionaryPtr, newFloatingDictionary, IntPtr.Zero) != IntPtr.Zero)
            {
                // Some other thread beat us and updated the pointer to the floating dictionary.
                // Free the one allocated by the current thread
                if (isNewlyAllocatedDictionary)
                    MemoryHelpers.FreeMemory(newFloatingDictionary);
            }

            return *(IntPtr*)dictionaryPtr;
        }

        public bool CanInstantiationsShareCode(RuntimeTypeHandle[] genericArgHandles1, RuntimeTypeHandle[] genericArgHandles2, CanonicalFormKind kind)
        {
            if (genericArgHandles1.Length != genericArgHandles2.Length)
                return false;

            bool match = true;

            TypeSystemContext context = TypeSystemContextFactory.Create();

            for (int i = 0; i < genericArgHandles1.Length; i++)
            {
                TypeDesc genericArg1 = context.ResolveRuntimeTypeHandle(genericArgHandles1[i]);
                TypeDesc genericArg2 = context.ResolveRuntimeTypeHandle(genericArgHandles2[i]);

                if (context.ConvertToCanon(genericArg1, kind) != context.ConvertToCanon(genericArg2, kind))
                {
                    match = false;
                    break;
                }
            }

            TypeSystemContextFactory.Recycle(context);

            return match;
        }

        public bool ConversionToCanonFormIsAChange(RuntimeTypeHandle[] genericArgHandles, CanonicalFormKind kind)
        {
            // Todo: support for universal canon type?

            TypeSystemContext context = TypeSystemContextFactory.Create();

            Instantiation genericArgs = context.ResolveRuntimeTypeHandles(genericArgHandles);
            bool result;
            context.ConvertInstantiationToCanonForm(genericArgs, kind, out result);

            TypeSystemContextFactory.Recycle(context);

            return result;
        }

        // get the generics hash table and external references table for a module
        // TODO multi-file: consider whether we want to cache this info
        private static unsafe bool GetHashtableFromBlob(NativeFormatModuleInfo module, ReflectionMapBlob blobId, out NativeHashtable hashtable, out ExternalReferencesTable externalReferencesLookup)
        {
            byte* pBlob;
            uint cbBlob;

            hashtable = default(NativeHashtable);
            externalReferencesLookup = default(ExternalReferencesTable);

            if (!module.TryFindBlob(blobId, out pBlob, out cbBlob))
                return false;

            NativeReader reader = new NativeReader(pBlob, cbBlob);
            NativeParser parser = new NativeParser(reader, 0);

            hashtable = new NativeHashtable(parser);

            return externalReferencesLookup.InitializeNativeReferences(module);
        }

        public static unsafe void GetFieldAlignmentAndSize(RuntimeTypeHandle fieldType, out int alignment, out int size)
        {
            MethodTable* typePtr = fieldType.ToEETypePtr();
            if (typePtr->IsValueType)
            {
                size = (int)typePtr->ValueTypeSize;
            }
            else
            {
                size = IntPtr.Size;
            }

            alignment = (int)typePtr->FieldAlignmentRequirement;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct UnboxingAndInstantiatingStubMapEntry
        {
            public uint StubMethodRva;
            public uint MethodRva;
        }

        public static unsafe bool TryGetTargetOfUnboxingAndInstantiatingStub(IntPtr maybeInstantiatingAndUnboxingStub, out IntPtr targetMethod)
        {
            targetMethod = RuntimeAugments.GetTargetOfUnboxingAndInstantiatingStub(maybeInstantiatingAndUnboxingStub);
            return (targetMethod != IntPtr.Zero);
        }

        public bool TryComputeHasInstantiationDeterminedSize(RuntimeTypeHandle typeHandle, out bool hasInstantiationDeterminedSize)
        {
            TypeSystemContext context = TypeSystemContextFactory.Create();
            bool success = TryComputeHasInstantiationDeterminedSize(typeHandle, context, out hasInstantiationDeterminedSize);
            TypeSystemContextFactory.Recycle(context);

            return success;
        }

        public bool TryComputeHasInstantiationDeterminedSize(RuntimeTypeHandle typeHandle, TypeSystemContext context, out bool hasInstantiationDeterminedSize)
        {
            Debug.Assert(RuntimeAugments.IsGenericType(typeHandle) || RuntimeAugments.IsGenericTypeDefinition(typeHandle));
            DefType type = (DefType)context.ResolveRuntimeTypeHandle(typeHandle);

            return TryComputeHasInstantiationDeterminedSize(type, out hasInstantiationDeterminedSize);
        }

        internal static bool TryComputeHasInstantiationDeterminedSize(DefType type, out bool hasInstantiationDeterminedSize)
        {
            Debug.Assert(type.HasInstantiation);

            NativeLayoutInfoLoadContext loadContextUniversal;
            NativeLayoutInfo universalLayoutInfo;
            NativeParser parser = type.GetOrCreateTypeBuilderState().GetParserForUniversalNativeLayoutInfo(out loadContextUniversal, out universalLayoutInfo);
            if (parser.IsNull)
            {
                hasInstantiationDeterminedSize = false;
#if SUPPORTS_NATIVE_METADATA_TYPE_LOADING
                MetadataType typeDefinition = type.GetTypeDefinition() as MetadataType;
                if (typeDefinition != null)
                {
                    TypeDesc [] universalCanonInstantiation = new TypeDesc[type.Instantiation.Length];
                    TypeSystemContext context = type.Context;
                    TypeDesc universalCanonType = context.UniversalCanonType;
                    for (int i = 0 ; i < universalCanonInstantiation.Length; i++)
                         universalCanonInstantiation[i] = universalCanonType;

                    DefType universalCanonForm = typeDefinition.MakeInstantiatedType(universalCanonInstantiation);
                    hasInstantiationDeterminedSize = universalCanonForm.InstanceFieldSize.IsIndeterminate;
                    return true;
                }
#endif
                return false;
            }

            int? flags = (int?)parser.GetUnsignedForBagElementKind(BagElementKind.TypeFlags);

            hasInstantiationDeterminedSize = flags.HasValue ?
                (((NativeFormat.TypeFlags)flags) & NativeFormat.TypeFlags.HasInstantiationDeterminedSize) != 0 :
                false;

            return true;
        }

        public bool TryResolveSingleMetadataFixup(ModuleInfo module, int metadataToken, MetadataFixupKind fixupKind, out IntPtr fixupResolution)
        {
#if SUPPORTS_NATIVE_METADATA_TYPE_LOADING
            using (LockHolder.Hold(_typeLoaderLock))
            {
                try
                {
                    return TypeBuilder.TryResolveSingleMetadataFixup((NativeFormatModuleInfo)module, metadataToken, fixupKind, out fixupResolution);
                }
                catch (Exception ex)
                {
                    Environment.FailFast("Failed to resolve metadata token " +
                        ((uint)metadataToken).LowLevelToString() + ": " + ex.Message);
#else
                    Environment.FailFast("Failed to resolve metadata token " +
                        ((uint)metadataToken).LowLevelToString());
#endif
                    fixupResolution = IntPtr.Zero;
                    return false;
#if SUPPORTS_NATIVE_METADATA_TYPE_LOADING
                }
            }
#endif
        }

        public bool TryDispatchMethodOnTarget(NativeFormatModuleInfo module, int metadataToken, RuntimeTypeHandle targetInstanceType, out IntPtr methodAddress)
        {
            using (LockHolder.Hold(_typeLoaderLock))
            {
                return TryDispatchMethodOnTarget_Inner(
                    module,
                    metadataToken,
                    targetInstanceType,
                    out methodAddress);
            }
        }

#if SUPPORTS_NATIVE_METADATA_TYPE_LOADING
        internal DispatchCellInfo ConvertDispatchCellInfo(NativeFormatModuleInfo module, DispatchCellInfo cellInfo)
        {
            using (LockHolder.Hold(_typeLoaderLock))
            {
                return ConvertDispatchCellInfo_Inner(
                    module,
                    cellInfo);
            }
        }
#endif

        internal unsafe bool TryResolveTypeSlotDispatch(MethodTable* targetType, MethodTable* interfaceType, ushort slot, out IntPtr methodAddress)
        {
            using (LockHolder.Hold(_typeLoaderLock))
            {
                return TryResolveTypeSlotDispatch_Inner(targetType, interfaceType, slot, out methodAddress);
            }
        }

        public unsafe bool TryGetOrCreateNamedTypeForMetadata(
            QTypeDefinition qTypeDefinition,
            out RuntimeTypeHandle runtimeTypeHandle)
        {
            if (TryGetNamedTypeForMetadata(qTypeDefinition, out runtimeTypeHandle))
            {
                return true;
            }

#if SUPPORTS_NATIVE_METADATA_TYPE_LOADING
            using (LockHolder.Hold(_typeLoaderLock))
            {
                IntPtr runtimeTypeHandleAsIntPtr;
                TypeBuilder.ResolveSingleTypeDefinition(qTypeDefinition, out runtimeTypeHandleAsIntPtr);
                runtimeTypeHandle = *(RuntimeTypeHandle*)&runtimeTypeHandleAsIntPtr;
                return true;
            }
#else
            return false;
#endif
        }

        public static IntPtr ConvertUnboxingFunctionPointerToUnderlyingNonUnboxingPointer(IntPtr unboxingFunctionPointer, RuntimeTypeHandle declaringType)
        {
            if (FunctionPointerOps.IsGenericMethodPointer(unboxingFunctionPointer))
            {
                // Handle shared generic methods
                unsafe
                {
                    GenericMethodDescriptor* functionPointerDescriptor = FunctionPointerOps.ConvertToGenericDescriptor(unboxingFunctionPointer);
                    IntPtr nonUnboxingTarget = RuntimeAugments.GetCodeTarget(functionPointerDescriptor->MethodFunctionPointer);
                    Debug.Assert(nonUnboxingTarget != functionPointerDescriptor->MethodFunctionPointer);
                    Debug.Assert(nonUnboxingTarget == RuntimeAugments.GetCodeTarget(nonUnboxingTarget));
                    return FunctionPointerOps.GetGenericMethodFunctionPointer(nonUnboxingTarget, functionPointerDescriptor->InstantiationArgument);
                }
            }

            // GetCodeTarget will look through simple unboxing stubs (ones that consist of adjusting the this pointer and then
            // jumping to the target.
            IntPtr exactTarget = RuntimeAugments.GetCodeTarget(unboxingFunctionPointer);
            if (RuntimeAugments.IsGenericType(declaringType))
            {
                IntPtr fatFunctionPointerTarget;

                // This check looks for unboxing and instantiating stubs generated via the compiler backend
                if (TypeLoaderEnvironment.TryGetTargetOfUnboxingAndInstantiatingStub(exactTarget, out fatFunctionPointerTarget))
                {
                    // If this is an unboxing and instantiating stub, use seperate table, find target, and create fat function pointer
                    exactTarget = FunctionPointerOps.GetGenericMethodFunctionPointer(fatFunctionPointerTarget,
                                                                                        declaringType.ToIntPtr());
                }
                else
                {
                    IntPtr newExactTarget;
                    // This check looks for unboxing and instantiating stubs generated dynamically as thunks in the calling convention converter
                    if (CallConverterThunk.TryGetNonUnboxingFunctionPointerFromUnboxingAndInstantiatingStub(exactTarget,
                        declaringType, out newExactTarget))
                    {
                        // CallingConventionConverter determined non-unboxing stub
                        exactTarget = newExactTarget;
                    }
                    else
                    {
                        // Target method was a method on a generic, but it wasn't a shared generic, and thus none of the above
                        // complex unboxing stub digging logic was necessary. Do nothing, and use exactTarget as discovered
                        // from GetCodeTarget
                    }
                }
            }

            return exactTarget;
        }
    }
}
