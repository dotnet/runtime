// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using global::System;
using global::System.Reflection;
using global::System.Collections.Generic;

using global::Internal.Runtime.Augments;
using global::Internal.Runtime.CompilerServices;
using global::Internal.Runtime.TypeLoader;

using global::Internal.Reflection.Core.Execution;
using global::Internal.Reflection.Execution.MethodInvokers;
using global::Internal.Reflection.Execution.FieldAccessors;

using global::Internal.Metadata.NativeFormat;

using global::System.Runtime.InteropServices;

using global::Internal.Runtime;
using global::Internal.NativeFormat;

using System.Reflection.Runtime.General;
using System.Threading;

using CanonicalFormKind = global::Internal.TypeSystem.CanonicalFormKind;


using Debug = System.Diagnostics.Debug;

namespace Internal.Reflection.Execution
{
    //==========================================================================================================
    // These ExecutionEnvironment entrypoints provide access to the NUTC-generated blob information that
    // enables Reflection invoke and tie-ins to native Type artifacts.
    //
    // - Except when otherwise noted, ExecutionEnvironment methods use the "TryGet*" pattern rather than throwing exceptions.
    //
    // - All methods on this class must be multi-thread-safe. Reflection can and does invoke them on different threads with no synchronization of its own.
    //
    //==========================================================================================================
    internal sealed partial class ExecutionEnvironmentImplementation : ExecutionEnvironment
    {
        private static RuntimeTypeHandle GetOpenTypeDefinition(RuntimeTypeHandle typeHandle, out RuntimeTypeHandle[] typeArgumentsHandles)
        {
            if (RuntimeAugments.IsGenericType(typeHandle))
            {
                return RuntimeAugments.GetGenericInstantiation(typeHandle, out typeArgumentsHandles);
            }

            typeArgumentsHandles = null;
            return typeHandle;
        }

        private static RuntimeTypeHandle GetTypeDefinition(RuntimeTypeHandle typeHandle)
        {
            if (RuntimeAugments.IsGenericType(typeHandle))
                return RuntimeAugments.GetGenericDefinition(typeHandle);

            return typeHandle;
        }

        private static bool RuntimeTypeHandleIsNonDefault(RuntimeTypeHandle runtimeTypeHandle)
        {
            return ((IntPtr)RuntimeAugments.GetPointerFromTypeHandle(runtimeTypeHandle)) != IntPtr.Zero;
        }

        private static unsafe NativeReader GetNativeReaderForBlob(NativeFormatModuleInfo module, ReflectionMapBlob blob)
        {
            NativeReader reader;
            if (TryGetNativeReaderForBlob(module, blob, out reader))
            {
                return reader;
            }

            Debug.Assert(false);
            return default(NativeReader);
        }

        private static unsafe bool TryGetNativeReaderForBlob(NativeFormatModuleInfo module, ReflectionMapBlob blob, out NativeReader reader)
        {
            byte* pBlob;
            uint cbBlob;

            if (module.TryFindBlob((int)blob, out pBlob, out cbBlob))
            {
                reader = new NativeReader(pBlob, cbBlob);
                return true;
            }

            reader = default(NativeReader);
            return false;
        }

        /// <summary>
        /// Return the metadata handle for a TypeDef if the pay-for-policy enabled this type as browsable. This is used to obtain name and other information for types
        /// obtained via typeof() or Object.GetType(). This can include generic types (not to be confused with generic instances).
        ///
        /// Preconditions:
        ///    runtimeTypeHandle is a typedef (not a constructed type such as an array or generic instance.)
        /// </summary>
        /// <param name="runtimeTypeHandle">Runtime handle of the type in question</param>
        public sealed override QTypeDefinition GetMetadataForNamedType(RuntimeTypeHandle runtimeTypeHandle)
        {
            Debug.Assert(!RuntimeAugments.IsGenericType(runtimeTypeHandle));
            if (!TypeLoaderEnvironment.Instance.TryGetMetadataForNamedType(runtimeTypeHandle, out QTypeDefinition qTypeDefinition))
            {
                // This should be unreachable unless there's a compiler bug
                throw new InvalidOperationException();
            }
            return qTypeDefinition;
        }

        /// <summary>
        /// Return the RuntimeTypeHandle for the named type described in metadata. This is used to implement the Create and Invoke
        /// apis for types.
        ///
        /// Preconditions:
        ///    metadataReader + typeDefHandle  - a valid metadata reader + typeDefinitionHandle where "metadataReader" is one
        ///                                      of the metadata readers returned by ExecutionEnvironment.MetadataReaders.
        ///
        /// Note: Although this method has a "bool" return value like the other mapping table accessors, the Project N pay-for-play design
        /// guarantees that any type enabled for metadata also has a RuntimeTypeHandle underneath.
        /// </summary>
        /// <param name="qTypeDefinition">TypeDef handle for the type to look up</param>
        /// <param name="runtimeTypeHandle">Runtime type handle (MethodTable) for the given type</param>
        public sealed override unsafe bool TryGetNamedTypeForMetadata(QTypeDefinition qTypeDefinition, out RuntimeTypeHandle runtimeTypeHandle)
        {
            return TypeLoaderEnvironment.Instance.TryGetNamedTypeForMetadata(qTypeDefinition, out runtimeTypeHandle);
        }

        //
        // Given a RuntimeTypeHandle for any type E, return a RuntimeTypeHandle for type E[], if the pay for play policy denotes E[] as browsable. This is used to
        // implement Array.CreateInstance().
        //
        // Preconditions:
        //     elementTypeHandle is a valid RuntimeTypeHandle.
        //
        // This is not equivalent to calling TryGetMultiDimTypeForElementType() with a rank of 1!
        //
        public sealed override unsafe bool TryGetArrayTypeForElementType(RuntimeTypeHandle elementTypeHandle, out RuntimeTypeHandle arrayTypeHandle)
        {
            if (RuntimeAugments.IsGenericTypeDefinition(elementTypeHandle))
            {
                arrayTypeHandle = default(RuntimeTypeHandle);
                return false;
            }

            // For non-dynamic arrays try to look up the array type in the ArrayMap blobs;
            // attempt to dynamically create a new one if that doesn't succeed.
            return TypeLoaderEnvironment.Instance.TryGetArrayTypeForElementType(elementTypeHandle, false, -1, out arrayTypeHandle);
        }

        //
        // Given a RuntimeTypeHandle for any array type E[], return a RuntimeTypeHandle for type E, if the pay for play policy denoted E[] as browsable.
        //
        // Preconditions:
        //      arrayTypeHandle is a valid RuntimeTypeHandle of type array.
        //
        // This is not equivalent to calling TryGetMultiDimTypeElementType() with a rank of 1!
        //
        public sealed override RuntimeTypeHandle GetArrayTypeElementType(RuntimeTypeHandle arrayTypeHandle)
        {
            return RuntimeAugments.GetRelatedParameterTypeHandle(arrayTypeHandle);
        }


        //
        // Given a RuntimeTypeHandle for any type E, return a RuntimeTypeHandle for type E[,,], if the pay for policy denotes E[,,] as browsable. This is used to
        // implement Type.MakeArrayType(Type, int).
        //
        // Preconditions:
        //     elementTypeHandle is a valid RuntimeTypeHandle.
        //
        // Calling this with rank 1 is not equivalent to calling TryGetArrayTypeForElementType()!
        //
        public sealed override unsafe bool TryGetMultiDimArrayTypeForElementType(RuntimeTypeHandle elementTypeHandle, int rank, out RuntimeTypeHandle arrayTypeHandle)
        {
            if (RuntimeAugments.IsGenericTypeDefinition(elementTypeHandle))
            {
                arrayTypeHandle = default(RuntimeTypeHandle);
                return false;
            }

            if ((rank < MDArray.MinRank) || (rank > MDArray.MaxRank))
            {
                throw new TypeLoadException(SR.Format(SR.MultiDim_Of_This_Rank_Not_Supported, rank));
            }

            return TypeLoaderEnvironment.Instance.TryGetArrayTypeForElementType(elementTypeHandle, true, rank, out arrayTypeHandle);
        }

        //
        // Given a RuntimeTypeHandle for any type E, return a RuntimeTypeHandle for type E*, if the pay-for-play policy denotes E* as browsable. This is used to
        // ensure that "typeof(E*)" and "typeof(E).MakePointerType()" returns the same Type object.
        //
        // Preconditions:
        //     targetTypeHandle is a valid RuntimeTypeHandle.
        //
        public sealed override unsafe bool TryGetPointerTypeForTargetType(RuntimeTypeHandle targetTypeHandle, out RuntimeTypeHandle pointerTypeHandle)
        {
            return TypeLoaderEnvironment.Instance.TryGetPointerTypeForTargetType(targetTypeHandle, out pointerTypeHandle);
        }

        //
        // Given a RuntimeTypeHandle for any pointer type E*, return a RuntimeTypeHandle for type E, if the pay-for-play policy denotes E* as browsable.
        // This is used to implement Type.GetElementType() for pointers.
        //
        // Preconditions:
        //      pointerTypeHandle is a valid RuntimeTypeHandle of type pointer.
        //
        public sealed override RuntimeTypeHandle GetPointerTypeTargetType(RuntimeTypeHandle pointerTypeHandle)
        {
            return RuntimeAugments.GetRelatedParameterTypeHandle(pointerTypeHandle);
        }

        public override bool TryGetFunctionPointerTypeForComponents(RuntimeTypeHandle returnTypeHandle, RuntimeTypeHandle[] parameterHandles, bool isUnmanaged, out RuntimeTypeHandle functionPointerTypeHandle)
        {
            return TypeLoaderEnvironment.Instance.TryGetFunctionPointerTypeForComponents(returnTypeHandle, parameterHandles, isUnmanaged, out functionPointerTypeHandle);
        }

        //
        // Given a RuntimeTypeHandle for any function pointer pointer type, return the composition of it.
        //
        // Preconditions:
        //      pointerTypeHandle is a valid RuntimeTypeHandle of type function pointer.
        //
        public override void GetFunctionPointerTypeComponents(RuntimeTypeHandle functionPointerHandle, out RuntimeTypeHandle returnTypeHandle, out RuntimeTypeHandle[] parameterHandles, out bool isUnmanaged)
        {
            returnTypeHandle = RuntimeAugments.GetFunctionPointerReturnType(functionPointerHandle);
            parameterHandles = RuntimeAugments.GetFunctionPointerParameterTypes(functionPointerHandle);
            isUnmanaged = RuntimeAugments.IsUnmanagedFunctionPointerType(functionPointerHandle);
        }

        //
        // Given a RuntimeTypeHandle for any type E, return a RuntimeTypeHandle for type E&, if the pay-for-play policy denotes E& as browsable. This is used to
        // ensure that "typeof(E&)" and "typeof(E).MakeByRefType()" returns the same Type object.
        //
        // Preconditions:
        //     targetTypeHandle is a valid RuntimeTypeHandle.
        //
        public sealed override unsafe bool TryGetByRefTypeForTargetType(RuntimeTypeHandle targetTypeHandle, out RuntimeTypeHandle byRefTypeHandle)
        {
            return TypeLoaderEnvironment.Instance.TryGetByRefTypeForTargetType(targetTypeHandle, out byRefTypeHandle);
        }

        //
        // Given a RuntimeTypeHandle for any byref type E&, return a RuntimeTypeHandle for type E, if the pay-for-play policy denotes E& as browsable.
        // This is used to implement Type.GetElementType() for byrefs.
        //
        // Preconditions:
        //      byRefTypeHandle is a valid RuntimeTypeHandle of a byref.
        //
        public sealed override unsafe RuntimeTypeHandle GetByRefTypeTargetType(RuntimeTypeHandle byRefTypeHandle)
        {
            return RuntimeAugments.GetRelatedParameterTypeHandle(byRefTypeHandle);
        }

        //
        // Given a RuntimeTypeHandle for a generic type G and a set of RuntimeTypeHandles T1, T2.., return the RuntimeTypeHandle for the generic
        // instance G<T1,T2...> if the pay-for-play policy denotes G<T1,T2...> as browsable. This is used to implement Type.MakeGenericType().
        //
        // Preconditions:
        //      runtimeTypeDefinitionHandle is a valid RuntimeTypeHandle for a generic type.
        //      genericTypeArgumentHandles is an array of valid RuntimeTypeHandles.
        //
        public sealed override unsafe bool TryGetConstructedGenericTypeForComponents(RuntimeTypeHandle genericTypeDefinitionHandle, RuntimeTypeHandle[] genericTypeArgumentHandles, out RuntimeTypeHandle runtimeTypeHandle)
        {
            if (TypeLoaderEnvironment.Instance.TryLookupConstructedGenericTypeForComponents(genericTypeDefinitionHandle, genericTypeArgumentHandles, out runtimeTypeHandle))
            {
                return true;
            }

            Type typeDefinition = Type.GetTypeFromHandle(genericTypeDefinitionHandle);

            Type[] typeArguments = new Type[genericTypeArgumentHandles.Length];
            for (int i = 0; i < genericTypeArgumentHandles.Length; i++)
            {
                // Early out if one of the arguments is a generic definition.
                // The reflection stack will use this to construct a Type that doesn't have a type handle.
                // Note: this is different from the validation we do in EnsureSatisfiesClassConstraints because this
                // should not throw.
                if (RuntimeAugments.IsGenericTypeDefinition(genericTypeArgumentHandles[i]))
                    return false;

                typeArguments[i] = Type.GetTypeFromHandle(genericTypeArgumentHandles[i]);
            }

            ConstraintValidator.EnsureSatisfiesClassConstraints(typeDefinition, typeArguments);

            return TypeLoaderEnvironment.Instance.TryGetConstructedGenericTypeForComponents(genericTypeDefinitionHandle, genericTypeArgumentHandles, out runtimeTypeHandle);
        }


        // Given a RuntimeTypeHandle for a generic type G and a set of RuntimeTypeHandles T1, T2.., return the RuntimeTypeHandle for the generic
        // instance G<T1,T2...> if the pay-for-play policy denotes G<T1,T2...> as browsable. This is used to implement Type.MakeGenericType().
        //
        // Preconditions:
        //      runtimeTypeDefinitionHandle is a valid RuntimeTypeHandle for a generic type.
        //      genericTypeArgumentHandles is an array of valid RuntimeTypeHandles.
        //
        public sealed override unsafe bool TryGetConstructedGenericTypeForComponentsNoConstraintCheck(RuntimeTypeHandle genericTypeDefinitionHandle, RuntimeTypeHandle[] genericTypeArgumentHandles, out RuntimeTypeHandle runtimeTypeHandle)
        {
            return TypeLoaderEnvironment.Instance.TryGetConstructedGenericTypeForComponents(genericTypeDefinitionHandle, genericTypeArgumentHandles, out runtimeTypeHandle);
        }

        public sealed override MethodInvoker TryGetMethodInvoker(RuntimeTypeHandle declaringTypeHandle, QMethodDefinition methodHandle, RuntimeTypeHandle[] genericMethodTypeArgumentHandles)
        {
            MethodBase methodInfo = ReflectionCoreExecution.ExecutionDomain.GetMethod(declaringTypeHandle, methodHandle, genericMethodTypeArgumentHandles);

            // Validate constraints first. This is potentially useless work if the method already exists, but it prevents bad
            // inputs to reach the type loader (we don't have support to e.g. represent pointer types within the type loader)
            if (genericMethodTypeArgumentHandles != null && genericMethodTypeArgumentHandles.Length > 0)
                ConstraintValidator.EnsureSatisfiesClassConstraints((MethodInfo)methodInfo);

            MethodSignatureComparer methodSignatureComparer = new MethodSignatureComparer(methodHandle);

            MethodInvokeInfo methodInvokeInfo;
#if GENERICS_FORCE_USG
            // Stress mode to force the usage of universal canonical method targets for reflection invokes.
            // It is recommended to use "/SharedGenericsMode GenerateAllUniversalGenerics" NUTC command line argument when
            // compiling the application in order to effectively use the GENERICS_FORCE_USG mode.

            // If we are just trying to invoke a non-generic method on a non-generic type, we won't force the universal lookup
            if (!RuntimeAugments.IsGenericType(declaringTypeHandle) && (genericMethodTypeArgumentHandles == null || genericMethodTypeArgumentHandles.Length == 0))
                methodInvokeInfo = TryGetMethodInvokeInfo(declaringTypeHandle, methodHandle, genericMethodTypeArgumentHandles,
                    methodInfo, ref methodSignatureComparer, CanonicalFormKind.Specific);
            else
                methodInvokeInfo = TryGetMethodInvokeInfo(declaringTypeHandle, methodHandle, genericMethodTypeArgumentHandles,
                    methodInfo, ref methodSignatureComparer, CanonicalFormKind.Universal);
#else
            methodInvokeInfo = TryGetMethodInvokeInfo(declaringTypeHandle, methodHandle, genericMethodTypeArgumentHandles,
                methodInfo, ref methodSignatureComparer, CanonicalFormKind.Specific);

            // If we failed to get a MethodInvokeInfo for an exact method, or a canonically equivalent method, check if there is a universal canonically
            // equivalent entry that could be used (it will be much slower, and require a calling convention converter)
            methodInvokeInfo ??= TryGetMethodInvokeInfo(declaringTypeHandle, methodHandle, genericMethodTypeArgumentHandles,
                    methodInfo, ref methodSignatureComparer, CanonicalFormKind.Universal);
#endif

            if (methodInvokeInfo == null)
                return null;

            return MethodInvokerWithMethodInvokeInfo.CreateMethodInvoker(declaringTypeHandle, methodHandle, methodInvokeInfo);
        }

        //
        // Get the pointer of a dynamic method invocation thunk
        //
        private static IntPtr GetDynamicMethodInvoke(NativeFormatModuleInfo module, uint cookie)
        {
            ExternalReferencesTable extRefs = default(ExternalReferencesTable);
            extRefs.InitializeCommonFixupsTable(module);

            return extRefs.GetFunctionPointerFromIndex(cookie);
        }

        private static RuntimeTypeHandle[] GetTypeSequence(ref ExternalReferencesTable extRefs, ref NativeParser parser)
        {
            uint count = parser.GetUnsigned();
            RuntimeTypeHandle[] result = new RuntimeTypeHandle[count];
            for (uint i = 0; i < count; i++)
            {
                result[i] = extRefs.GetRuntimeTypeHandleFromIndex(parser.GetUnsigned());
            }
            return result;
        }

        private static IntPtr TryGetVirtualResolveData(NativeFormatModuleInfo module,
            RuntimeTypeHandle methodHandleDeclaringType, QMethodDefinition methodHandle, RuntimeTypeHandle[] genericArgs,
            ref MethodSignatureComparer methodSignatureComparer)
        {
            TypeLoaderEnvironment.VirtualResolveDataResult lookupResult;
            bool success = TypeLoaderEnvironment.TryGetVirtualResolveData(module, methodHandleDeclaringType, genericArgs, ref methodSignatureComparer, out lookupResult);
            if (!success)
                return IntPtr.Zero;
            else
            {
                GCHandle reader = Internal.TypeSystem.LockFreeObjectInterner.GetInternedObjectHandle(methodHandle.Reader);

                if (lookupResult.IsGVM)
                {
                    return (new OpenMethodResolver(lookupResult.DeclaringInvokeType, lookupResult.GVMHandle, reader, methodHandle.Token)).ToIntPtr();
                }
                else
                {
                    return (new OpenMethodResolver(lookupResult.DeclaringInvokeType, lookupResult.SlotIndex, reader, methodHandle.Token)).ToIntPtr();
                }
            }
        }

        /// <summary>
        /// Try to look up method invoke info in metadata for all registered modules, construct
        /// the calling convention converter as appropriate and fill in MethodInvokeInfo.
        /// </summary>
        /// <param name="declaringTypeHandle">Runtime handle of declaring type for the method</param>
        /// <param name="methodHandle">Handle of method to look up</param>
        /// <param name="genericMethodTypeArgumentHandles">Runtime handles of generic method arguments</param>
        /// <param name="methodInfo">MethodInfo of method to look up</param>
        /// <param name="methodSignatureComparer">Helper structure used for comparing signatures</param>
        /// <param name="canonFormKind">Requested canon form</param>
        /// <returns>Constructed method invoke info, null on failure</returns>
        private static unsafe MethodInvokeInfo TryGetMethodInvokeInfo(
            RuntimeTypeHandle declaringTypeHandle,
            QMethodDefinition methodHandle,
            RuntimeTypeHandle[] genericMethodTypeArgumentHandles,
            MethodBase methodInfo,
            ref MethodSignatureComparer methodSignatureComparer,
            CanonicalFormKind canonFormKind)
        {
            MethodInvokeMetadata methodInvokeMetadata;

            if (!TypeLoaderEnvironment.TryGetMethodInvokeMetadata(
                declaringTypeHandle,
                methodHandle,
                genericMethodTypeArgumentHandles,
                ref methodSignatureComparer,
                canonFormKind,
                out methodInvokeMetadata))
            {
                // Method invoke info not found
                return null;
            }

            if ((methodInvokeMetadata.InvokeTableFlags & InvokeTableFlags.CallingConventionMask) != 0)
            {
                // MethodInvokeInfo found, but it references a method with a native calling convention.
                return null;
            }

            IntPtr dynamicInvokeMethod;
            Debug.Assert((methodInvokeMetadata.InvokeTableFlags & InvokeTableFlags.NeedsParameterInterpretation) == 0);
            dynamicInvokeMethod = GetDynamicMethodInvoke(
                methodInvokeMetadata.MappingTableModule,
                methodInvokeMetadata.DynamicInvokeCookie);

            IntPtr resolver = IntPtr.Zero;
            if ((methodInvokeMetadata.InvokeTableFlags & InvokeTableFlags.HasVirtualInvoke) != 0)
            {
                resolver = TryGetVirtualResolveData(ModuleList.Instance.GetModuleInfoForMetadataReader(methodHandle.NativeFormatReader),
                    declaringTypeHandle, methodHandle, genericMethodTypeArgumentHandles,
                    ref methodSignatureComparer);

                // Unable to find virtual resolution information, cannot return valid MethodInvokeInfo
                if (resolver == IntPtr.Zero)
                    return null;
            }

            var methodInvokeInfo = new MethodInvokeInfo(methodInfo, dynamicInvokeMethod)
            {
                LdFtnResult = methodInvokeMetadata.MethodEntryPoint,
                VirtualResolveData = resolver,
            };
            return methodInvokeInfo;
        }

        private static RuntimeTypeHandle GetExactDeclaringType(RuntimeTypeHandle dstType, RuntimeTypeHandle srcType)
        {
            // The fact that for generic types we rely solely on the template type in the mapping table causes
            // trouble for lookups from method pointer to the declaring type and method metadata handle.

            // Suppose we have following code:
            // class Base<T> { void Frob() { } }
            // class Derived<T> : Base<T> { }
            // Let's pick Base<object>, Derived<object> as the template.
            // Now if someone calls TryGetMethodForOriginalLdFtnResult with a pointer to the Frob method and a RuntimeTypeHandle
            // of the Derived<string> object instance, we are expected to return the metadata handle for Frob with *Base*<string>
            // as the declaring type. The table obviously only has an entry for Frob with Base<object>.

            // This method needs to return "true" and "Base<string>" for cases like this.

            RuntimeTypeHandle dstTypeDef = GetTypeDefinition(dstType);

            while (!srcType.IsNull())
            {
                if (RuntimeAugments.IsAssignableFrom(dstType, srcType))
                {
                    return dstType;
                }

                if (!dstTypeDef.IsNull() && RuntimeAugments.IsGenericType(srcType))
                {
                    RuntimeTypeHandle srcTypeDef = GetTypeDefinition(srcType);;

                    // Compare TypeDefs. We don't look at the generic components. We already know that the right type
                    // to return must be somewhere in the inheritance chain.
                    if (dstTypeDef.Equals(srcTypeDef))
                    {
                        // Return the *other* type handle since dstType is instantiated over different arguments
                        return srcType;
                    }
                }

                if (!RuntimeAugments.TryGetBaseType(srcType, out srcType))
                {
                    break;
                }
            }

            Debug.Assert(false);
            return default(RuntimeTypeHandle);
        }

        private struct FunctionPointerOffsetPair : IComparable<FunctionPointerOffsetPair>
        {
            public FunctionPointerOffsetPair(IntPtr functionPointer, uint offset)
            {
                FunctionPointer = functionPointer;
                Offset = offset;
            }

            public int CompareTo(FunctionPointerOffsetPair other)
            {
                unsafe
                {
                    void* fptr = FunctionPointer.ToPointer();
                    void* otherFptr = other.FunctionPointer.ToPointer();

                    if (fptr < otherFptr)
                        return -1;
                    else if (fptr == otherFptr)
                        return Offset.CompareTo(other.Offset);
                    else
                        return 1;
                }
            }

            public readonly IntPtr FunctionPointer;
            public readonly uint Offset;
        }

        private struct FunctionPointersToOffsets
        {
            public FunctionPointerOffsetPair[] Data;

            public bool TryGetOffsetsRange(IntPtr functionPointer, out int firstParserOffsetIndex, out int lastParserOffsetIndex)
            {
                firstParserOffsetIndex = -1;
                lastParserOffsetIndex = -1;

                if (Data == null)
                    return false;

                int binarySearchIndex = Array.BinarySearch(Data, new FunctionPointerOffsetPair(functionPointer, 0));

                // Array.BinarySearch will return either a positive number which is the first index in a range
                // or a negative number which is the bitwise complement of the start of the range
                // or a negative number which doesn't correspond to the range at all.
                if (binarySearchIndex < 0)
                    binarySearchIndex = ~binarySearchIndex;

                if (binarySearchIndex >= Data.Length || Data[binarySearchIndex].FunctionPointer != functionPointer)
                    return false;

                // binarySearchIndex now contains the index of the start of a range of matching function pointers and offsets
                firstParserOffsetIndex = binarySearchIndex;
                lastParserOffsetIndex = binarySearchIndex;
                while ((lastParserOffsetIndex < (Data.Length - 1)) && Data[lastParserOffsetIndex + 1].FunctionPointer == functionPointer)
                {
                    lastParserOffsetIndex++;
                }
                return true;
            }
        }

        // ldftn reverse lookup hash. Must be cleared and reset if the module list changes. (All sets to
        // this variable must happen under a lock)
        private volatile KeyValuePair<NativeFormatModuleInfo, FunctionPointersToOffsets>[] _ldftnReverseLookup_InvokeMap;
        private Func<NativeFormatModuleInfo, FunctionPointersToOffsets> _computeLdFtnLookupInvokeMapInvokeMap = ComputeLdftnReverseLookup_InvokeMap;

        /// <summary>
        /// Initialize a lookup array of module to function pointer/parser offset pair arrays. Do so in a manner that will allow
        /// future work which will invalidate the cache (by setting it to null)
        /// </summary>
        /// <param name="ldftnReverseLookupStatic">pointer to static which holds cache value. This is treated as a volatile variable</param>
        /// <param name="lookupComputer"></param>
        /// <returns></returns>
        private KeyValuePair<NativeFormatModuleInfo, FunctionPointersToOffsets>[] GetLdFtnReverseLookups_Helper(ref KeyValuePair<NativeFormatModuleInfo, FunctionPointersToOffsets>[] ldftnReverseLookupStatic, Func<NativeFormatModuleInfo, FunctionPointersToOffsets> lookupComputer)
        {
            KeyValuePair<NativeFormatModuleInfo, FunctionPointersToOffsets>[] ldFtnReverseLookup = Volatile.Read(ref ldftnReverseLookupStatic);

            if (ldFtnReverseLookup != null)
                return ldFtnReverseLookup;
            else
            {
                lock (this)
                {
                    ldFtnReverseLookup = Volatile.Read(ref ldftnReverseLookupStatic);

                    // double checked lock, safe due to use of volatile on s_ldftnReverseHashes
                    if (ldFtnReverseLookup != null)
                        return ldFtnReverseLookup;

                    // FUTURE: add a module load callback to invalidate this cache if a new module is loaded.
                    while (true)
                    {
                        int size = 0;
                        foreach (NativeFormatModuleInfo module in ModuleList.EnumerateModules())
                        {
                            size++;
                        }

                        ldFtnReverseLookup = new KeyValuePair<NativeFormatModuleInfo, FunctionPointersToOffsets>[size];
                        int index = 0;
                        bool restart = false;
                        foreach (NativeFormatModuleInfo module in ModuleList.EnumerateModules())
                        {
                            // If the module list changes during execution of this code, rebuild from scratch
                            if (index >= ldFtnReverseLookup.Length)
                            {
                                restart = true;
                                break;
                            }

                            ldFtnReverseLookup[index] = new KeyValuePair<NativeFormatModuleInfo, FunctionPointersToOffsets>(module, lookupComputer(module));
                            index++;
                        }

                        if (restart)
                            continue;

                        // unless we need to repeat the module enumeration, only execute the body of this while loop once.
                        break;
                    }

                    Volatile.Write(ref ldftnReverseLookupStatic, ldFtnReverseLookup);
                    return ldFtnReverseLookup;
                }
            }
        }

        private KeyValuePair<NativeFormatModuleInfo, FunctionPointersToOffsets>[] GetLdFtnReverseLookups_InvokeMap()
        {
#pragma warning disable 0420 // GetLdFtnReverseLookups_Helper treats its first parameter as volatile by using explicit Volatile operations
            return GetLdFtnReverseLookups_Helper(ref _ldftnReverseLookup_InvokeMap, _computeLdFtnLookupInvokeMapInvokeMap);
#pragma warning restore 0420
        }

        internal unsafe void GetFunctionPointerAndInstantiationArgumentForOriginalLdFtnResult(IntPtr originalLdFtnResult, out IntPtr canonOriginalLdFtnResult, out IntPtr instantiationArgument)
        {
            if (FunctionPointerOps.IsGenericMethodPointer(originalLdFtnResult))
            {
                GenericMethodDescriptor* realTargetData = FunctionPointerOps.ConvertToGenericDescriptor(originalLdFtnResult);
                canonOriginalLdFtnResult = RuntimeAugments.GetCodeTarget(realTargetData->MethodFunctionPointer);
                instantiationArgument = realTargetData->InstantiationArgument;
            }
            else
            {
                canonOriginalLdFtnResult = RuntimeAugments.GetCodeTarget(originalLdFtnResult);
                instantiationArgument = IntPtr.Zero;
            }
        }

        internal bool TryGetMethodForOriginalLdFtnResult(IntPtr originalLdFtnResult, ref RuntimeTypeHandle declaringTypeHandle, out QMethodDefinition methodHandle, out RuntimeTypeHandle[] genericMethodTypeArgumentHandles)
        {
            GetFunctionPointerAndInstantiationArgumentForOriginalLdFtnResult(originalLdFtnResult, out IntPtr canonOriginalLdFtnResult, out IntPtr instantiationArgument);

            if (instantiationArgument != IntPtr.Zero)
            {
                // Search TemplateMethodMap
                if (TryGetMethodForOriginalLdFtnResult_GenericMethodWithInstantiationArgument(instantiationArgument, ref declaringTypeHandle, out methodHandle, out genericMethodTypeArgumentHandles))
                    return true;
            }

            // Search InvokeMap
            foreach (KeyValuePair<NativeFormatModuleInfo, FunctionPointersToOffsets> perModuleLookup in GetLdFtnReverseLookups_InvokeMap())
            {
                int startIndex;
                int endIndex;

                if (perModuleLookup.Value.TryGetOffsetsRange(canonOriginalLdFtnResult, out startIndex, out endIndex))
                {
                    for (int curIndex = startIndex; curIndex <= endIndex; curIndex++)
                    {
                        uint parserOffset = perModuleLookup.Value.Data[curIndex].Offset;
                        if (TryGetMethodForOriginalLdFtnResult_InvokeMap_Inner(perModuleLookup.Key, forStartAddress: false, canonOriginalLdFtnResult, instantiationArgument, parserOffset, ref declaringTypeHandle, out methodHandle, out genericMethodTypeArgumentHandles))
                            return true;
                    }
                }
            }

            methodHandle = default(QMethodDefinition);
            genericMethodTypeArgumentHandles = null;
            return false;
        }

        internal bool TryGetMethodForStartAddress(IntPtr methodStartAddress, ref RuntimeTypeHandle declaringTypeHandle, out QMethodDefinition methodHandle)
        {
            // Search InvokeMap
            foreach (KeyValuePair<NativeFormatModuleInfo, FunctionPointersToOffsets> perModuleLookup in GetLdFtnReverseLookups_InvokeMap())
            {
                int startIndex;
                int endIndex;

                if (perModuleLookup.Value.TryGetOffsetsRange(methodStartAddress, out startIndex, out endIndex))
                {
                    for (int curIndex = startIndex; curIndex <= endIndex; curIndex++)
                    {
                        uint parserOffset = perModuleLookup.Value.Data[curIndex].Offset;
                        if (TryGetMethodForOriginalLdFtnResult_InvokeMap_Inner(perModuleLookup.Key, forStartAddress: true, methodStartAddress, IntPtr.Zero, parserOffset, ref declaringTypeHandle, out methodHandle, out _))
                        {
                            if (RuntimeAugments.IsGenericType(declaringTypeHandle))
                                declaringTypeHandle = RuntimeAugments.GetGenericDefinition(declaringTypeHandle);
                            return true;
                        }
                    }
                }
            }

            methodHandle = default(QMethodDefinition);
            return false;
        }

        private static FunctionPointersToOffsets ComputeLdftnReverseLookup_InvokeMap(NativeFormatModuleInfo mappingTableModule)
        {
            FunctionPointersToOffsets functionPointerToOffsetInInvokeMap = new FunctionPointersToOffsets();

            NativeReader invokeMapReader;
            if (!TryGetNativeReaderForBlob(mappingTableModule, ReflectionMapBlob.InvokeMap, out invokeMapReader))
            {
                return functionPointerToOffsetInInvokeMap;
            }

            ExternalReferencesTable externalReferences = default(ExternalReferencesTable);
            externalReferences.InitializeCommonFixupsTable(mappingTableModule);

            NativeParser invokeMapParser = new NativeParser(invokeMapReader, 0);
            NativeHashtable invokeHashtable = new NativeHashtable(invokeMapParser);

            LowLevelList<FunctionPointerOffsetPair> functionPointers = new LowLevelList<FunctionPointerOffsetPair>();

            var lookup = invokeHashtable.EnumerateAllEntries();
            NativeParser entryParser;
            while (!(entryParser = lookup.GetNext()).IsNull)
            {
                uint parserOffset = entryParser.Offset;
                Debug.Assert(entryParser.Reader == invokeMapParser.Reader);

                InvokeTableFlags entryFlags = (InvokeTableFlags)entryParser.GetUnsigned();

                bool hasEntrypoint = ((entryFlags & InvokeTableFlags.HasEntrypoint) != 0);
                if (!hasEntrypoint)
                    continue;

                entryParser.SkipInteger(); // entryMethodHandleOrNameAndSigRaw
                entryParser.SkipInteger(); // entryDeclaringTypeRaw

                IntPtr entryMethodEntrypoint = externalReferences.GetFunctionPointerFromIndex(entryParser.GetUnsigned());
                functionPointers.Add(new FunctionPointerOffsetPair(entryMethodEntrypoint, parserOffset));

                // Add resolved stub targets to the reverse LdFtn lookup map for the purpose of reflection-based
                // stack trace resolution - the reverse LdFtn lookup internally used by the reflection
                // method resolution will work off an IP address on the stack which is an address
                // within the actual method, not the stub.
                IntPtr targetAddress = RuntimeAugments.GetCodeTarget(entryMethodEntrypoint);
                if (targetAddress != IntPtr.Zero && targetAddress != entryMethodEntrypoint)
                {
                    functionPointers.Add(new FunctionPointerOffsetPair(targetAddress, parserOffset));
                }
                IntPtr targetAddress2;
                if (TypeLoaderEnvironment.TryGetTargetOfUnboxingAndInstantiatingStub(entryMethodEntrypoint, out targetAddress2) &&
                    targetAddress2 != entryMethodEntrypoint &&
                    targetAddress2 != targetAddress)
                {
                    functionPointers.Add(new FunctionPointerOffsetPair(targetAddress2, parserOffset));
                }
            }

            functionPointerToOffsetInInvokeMap.Data = functionPointers.ToArray();
            Array.Sort(functionPointerToOffsetInInvokeMap.Data);

            return functionPointerToOffsetInInvokeMap;
        }

        private unsafe bool TryGetMethodForOriginalLdFtnResult_InvokeMap_Inner(NativeFormatModuleInfo mappingTableModule, bool forStartAddress, IntPtr canonOriginalLdFtnResult, IntPtr instantiationArgument, uint parserOffset, ref RuntimeTypeHandle declaringTypeHandle, out QMethodDefinition methodHandle, out RuntimeTypeHandle[] genericMethodTypeArgumentHandles)
        {
            methodHandle = default(QMethodDefinition);
            genericMethodTypeArgumentHandles = null;

            NativeReader invokeMapReader;
            if (!TryGetNativeReaderForBlob(mappingTableModule, ReflectionMapBlob.InvokeMap, out invokeMapReader))
            {
                // This should have succeeded otherwise, how did we get a parser offset as an input parameter?
                Debug.Assert(false);
                return false;
            }

            ExternalReferencesTable externalReferences = default(ExternalReferencesTable);
            externalReferences.InitializeCommonFixupsTable(mappingTableModule);

            NativeParser entryParser = new NativeParser(invokeMapReader, parserOffset);

            InvokeTableFlags entryFlags = (InvokeTableFlags)entryParser.GetUnsigned();

            // If the passed in method was a fat function pointer, but the entry in the mapping table doesn't need
            // an instantiation argument (or the other way around), trivially reject it.
            if (!forStartAddress && ((instantiationArgument == IntPtr.Zero) != ((entryFlags & InvokeTableFlags.RequiresInstArg) == 0)))
                return false;

            Debug.Assert((entryFlags & InvokeTableFlags.HasEntrypoint) != 0);

            uint entryMethodHandleOrNameAndSigRaw = entryParser.GetUnsigned();
            uint entryDeclaringTypeRaw = entryParser.GetUnsigned();

            IntPtr entryMethodEntrypoint = externalReferences.GetFunctionPointerFromIndex(entryParser.GetUnsigned());

            if ((entryFlags & InvokeTableFlags.NeedsParameterInterpretation) == 0)
                entryParser.SkipInteger(); // skip dynamic invoke cookie

            if (forStartAddress)
            {
                declaringTypeHandle = externalReferences.GetRuntimeTypeHandleFromIndex(entryDeclaringTypeRaw);
            }
            else
            {
#if DEBUG
                IntPtr targetAddress;
                Debug.Assert(entryMethodEntrypoint == canonOriginalLdFtnResult ||
                    RuntimeAugments.GetCodeTarget(entryMethodEntrypoint) == canonOriginalLdFtnResult ||
                    TypeLoaderEnvironment.TryGetTargetOfUnboxingAndInstantiatingStub(entryMethodEntrypoint, out targetAddress) &&
                        targetAddress == canonOriginalLdFtnResult);
#endif

                if ((entryFlags & InvokeTableFlags.RequiresInstArg) == 0 && declaringTypeHandle.IsNull())
                    declaringTypeHandle = externalReferences.GetRuntimeTypeHandleFromIndex(entryDeclaringTypeRaw);

                if ((entryFlags & InvokeTableFlags.IsGenericMethod) != 0)
                {
                    if ((entryFlags & InvokeTableFlags.RequiresInstArg) != 0)
                    {
                        bool success = TypeLoaderEnvironment.Instance.TryGetGenericMethodComponents(instantiationArgument, out declaringTypeHandle, out genericMethodTypeArgumentHandles);
                        Debug.Assert(success);
                    }
                    else
                        genericMethodTypeArgumentHandles = GetTypeSequence(ref externalReferences, ref entryParser);
                }
                else
                {
                    genericMethodTypeArgumentHandles = null;
                    if ((entryFlags & InvokeTableFlags.RequiresInstArg) != 0)
                        declaringTypeHandle = RuntimeAugments.CreateRuntimeTypeHandle(instantiationArgument);
                }

                RuntimeTypeHandle entryType = externalReferences.GetRuntimeTypeHandleFromIndex(entryDeclaringTypeRaw);
                declaringTypeHandle = GetExactDeclaringType(entryType, declaringTypeHandle);
            }

            if ((entryFlags & InvokeTableFlags.HasMetadataHandle) != 0)
            {
                RuntimeTypeHandle declaringTypeHandleDefinition = GetTypeDefinition(declaringTypeHandle);
                QTypeDefinition qTypeDefinition = GetMetadataForNamedType(declaringTypeHandleDefinition);

                MethodHandle nativeFormatMethodHandle =
                    (((int)HandleType.Method << 24) | (int)entryMethodHandleOrNameAndSigRaw).AsMethodHandle();

                methodHandle = new QMethodDefinition(qTypeDefinition.NativeFormatReader, nativeFormatMethodHandle);
            }
            else
            {
#if FEATURE_SHARED_LIBRARY
                uint nameAndSigOffset = entryMethodHandleOrNameAndSigRaw;
                MethodNameAndSignature nameAndSig;
                if (!TypeLoaderEnvironment.Instance.TryGetMethodNameAndSignatureFromNativeLayoutOffset(mappingTableModule.Handle, nameAndSigOffset, out nameAndSig))
                {
                    Debug.Assert(false);
                    return false;
                }

                if (!TypeLoaderEnvironment.Instance.TryGetMetadataForTypeMethodNameAndSignature(declaringTypeHandle, nameAndSig, out methodHandle))
                {
                    Debug.Assert(false);
                    return false;
                }
#else
                throw NotImplemented.ByDesign;
#endif
            }

            return true;
        }

        private static unsafe bool TryGetMethodForOriginalLdFtnResult_GenericMethodWithInstantiationArgument(IntPtr instantiationArgument, ref RuntimeTypeHandle declaringTypeHandle, out QMethodDefinition methodHandle, out RuntimeTypeHandle[] genericMethodTypeArgumentHandles)
        {
            MethodNameAndSignature nameAndSig;
            bool success = TypeLoaderEnvironment.Instance.TryGetGenericMethodComponents(instantiationArgument, out declaringTypeHandle, out nameAndSig, out genericMethodTypeArgumentHandles);
            if (success)
            {
                if (TypeLoaderEnvironment.Instance.TryGetMetadataForTypeMethodNameAndSignature(declaringTypeHandle, nameAndSig, out methodHandle))
                {
                    return true;
                }
            }

            methodHandle = default(QMethodDefinition);

            return false;
        }

        public sealed override FieldAccessor TryGetFieldAccessor(
            MetadataReader metadataReader,
            RuntimeTypeHandle declaringTypeHandle,
            RuntimeTypeHandle fieldTypeHandle,
            FieldHandle fieldHandle)
        {
            FieldAccessMetadata fieldAccessMetadata;

            if (!TypeLoaderEnvironment.TryGetFieldAccessMetadata(
                metadataReader,
                declaringTypeHandle,
                fieldHandle,
                out fieldAccessMetadata))
            {
                return null;
            }

            FieldTableFlags fieldBase = fieldAccessMetadata.Flags & FieldTableFlags.StorageClass;
            switch (fieldBase)
            {
                case FieldTableFlags.Instance:
                    {
                        int fieldOffsetDelta = RuntimeAugments.IsValueType(declaringTypeHandle) ? IntPtr.Size : 0;

                        return RuntimeAugments.IsValueType(fieldTypeHandle) ?
                            (FieldAccessor)new ValueTypeFieldAccessorForInstanceFields(
                                fieldAccessMetadata.Offset + fieldOffsetDelta, declaringTypeHandle, fieldTypeHandle) :
                            (RuntimeAugments.IsUnmanagedPointerType(fieldTypeHandle) || RuntimeAugments.IsFunctionPointerType(fieldTypeHandle)) ?
                                (FieldAccessor)new PointerTypeFieldAccessorForInstanceFields(
                                    fieldAccessMetadata.Offset + fieldOffsetDelta, declaringTypeHandle, fieldTypeHandle) :
                                (FieldAccessor)new ReferenceTypeFieldAccessorForInstanceFields(
                                    fieldAccessMetadata.Offset + fieldOffsetDelta, declaringTypeHandle, fieldTypeHandle);
                    }

                case FieldTableFlags.NonGCStatic:
                case FieldTableFlags.GCStatic:
                case FieldTableFlags.ThreadStatic:
                    {
                        int fieldOffset;
                        IntPtr staticsBase;

                        if (RuntimeAugments.IsGenericType(declaringTypeHandle))
                        {
                            unsafe
                            {
                                fieldOffset = fieldAccessMetadata.Offset;
                                staticsBase = fieldBase switch
                                {
                                    FieldTableFlags.GCStatic => TypeLoaderEnvironment.Instance.TryGetGcStaticFieldData(declaringTypeHandle),
                                    FieldTableFlags.NonGCStatic => TypeLoaderEnvironment.Instance.TryGetNonGcStaticFieldData(declaringTypeHandle),
                                    _ => TypeLoaderEnvironment.Instance.TryGetThreadStaticFieldData(declaringTypeHandle),
                                };
                            }
                        }
                        else
                        {
                            Debug.Assert((fieldAccessMetadata.Flags & FieldTableFlags.IsUniversalCanonicalEntry) == 0);

                            if (fieldBase != FieldTableFlags.NonGCStatic)
                            {
                                fieldOffset = fieldAccessMetadata.Offset;
                                staticsBase = fieldAccessMetadata.Cookie;
                            }
                            else
                            {
                                // The fieldAccessMetadata.Cookie value points directly to the field's data. We'll use that as the 'staticsBase'
                                // and just use a field offset of zero.
                                fieldOffset = 0;
                                staticsBase = fieldAccessMetadata.Cookie;
                            }
                        }

                        IntPtr cctorContext = TryGetStaticClassConstructionContext(declaringTypeHandle);

                        return RuntimeAugments.IsValueType(fieldTypeHandle) ?
                            (FieldAccessor)new ValueTypeFieldAccessorForStaticFields(cctorContext, staticsBase, fieldOffset, fieldAccessMetadata.Flags, fieldTypeHandle) :
                            (RuntimeAugments.IsUnmanagedPointerType(fieldTypeHandle) || RuntimeAugments.IsFunctionPointerType(fieldTypeHandle)) ?
                                (FieldAccessor)new PointerTypeFieldAccessorForStaticFields(cctorContext, staticsBase, fieldOffset, fieldAccessMetadata.Flags, fieldTypeHandle) :
                                (FieldAccessor)new ReferenceTypeFieldAccessorForStaticFields(cctorContext, staticsBase, fieldOffset, fieldAccessMetadata.Flags, fieldTypeHandle);
                    }
            }

            return null;
        }

        //
        // This resolves RuntimeMethodHandles for methods declared on non-generic types (declaringTypeHandle is an output of this method.)
        //
        public sealed override unsafe bool TryGetMethodFromHandle(RuntimeMethodHandle runtimeMethodHandle, out RuntimeTypeHandle declaringTypeHandle, out QMethodDefinition methodHandle, out RuntimeTypeHandle[] genericMethodTypeArgumentHandles)
        {
            MethodNameAndSignature nameAndSignature;
            methodHandle = default(QMethodDefinition);
            if (!TypeLoaderEnvironment.Instance.TryGetRuntimeMethodHandleComponents(runtimeMethodHandle, out declaringTypeHandle, out nameAndSignature, out genericMethodTypeArgumentHandles))
                return false;

            return TypeLoaderEnvironment.Instance.TryGetMetadataForTypeMethodNameAndSignature(declaringTypeHandle, nameAndSignature, out methodHandle);
        }

        //
        // This resolves RuntimeMethodHandles for methods declared on generic types (declaringTypeHandle is an input of this method.)
        //
        public sealed override bool TryGetMethodFromHandleAndType(RuntimeMethodHandle runtimeMethodHandle, RuntimeTypeHandle declaringTypeHandle, out QMethodDefinition methodHandle, out RuntimeTypeHandle[] genericMethodTypeArgumentHandles)
        {
            return TryGetMethodFromHandle(runtimeMethodHandle, out _, out methodHandle, out genericMethodTypeArgumentHandles);
        }

        //
        // This resolves RuntimeFieldHandles for fields declared on non-generic types (declaringTypeHandle is an output of this method.)
        //
        public sealed override unsafe bool TryGetFieldFromHandle(RuntimeFieldHandle runtimeFieldHandle, out RuntimeTypeHandle declaringTypeHandle, out FieldHandle fieldHandle)
        {
            fieldHandle = default(FieldHandle);

            string fieldName;
            if (!TypeLoaderEnvironment.Instance.TryGetRuntimeFieldHandleComponents(runtimeFieldHandle, out declaringTypeHandle, out fieldName))
                return false;

            RuntimeTypeHandle metadataLookupTypeHandle = GetTypeDefinition(declaringTypeHandle);

            QTypeDefinition qTypeDefinition = GetMetadataForNamedType(metadataLookupTypeHandle);

            // TODO! Handle ecma style types
            MetadataReader reader = qTypeDefinition.NativeFormatReader;
            TypeDefinitionHandle typeDefinitionHandle = qTypeDefinition.NativeFormatHandle;

            TypeDefinition typeDefinition = typeDefinitionHandle.GetTypeDefinition(reader);
            foreach (FieldHandle fh in typeDefinition.Fields)
            {
                Field field = fh.GetField(reader);
                if (field.Name.StringEquals(fieldName, reader))
                {
                    fieldHandle = fh;
                    return true;
                }
            }

            return false;
        }

        //
        // This resolves RuntimeFieldHandles for fields declared on generic types (declaringTypeHandle is an input of this method.)
        //
        public sealed override bool TryGetFieldFromHandleAndType(RuntimeFieldHandle runtimeFieldHandle, RuntimeTypeHandle declaringTypeHandle, out FieldHandle fieldHandle)
        {
            return TryGetFieldFromHandle(runtimeFieldHandle, out _, out fieldHandle);
        }

        /// <summary>
        /// Locate the static constructor context given the runtime type handle (MethodTable) for the type in question.
        /// </summary>
        /// <param name="typeHandle">MethodTable of the type to look up</param>
        internal static unsafe IntPtr TryGetStaticClassConstructionContext(RuntimeTypeHandle typeHandle)
        {
            return TypeLoaderEnvironment.TryGetStaticClassConstructionContext(typeHandle);
        }

        private struct MethodParametersInfo
        {
            private MetadataReader _metadataReader;
            private MethodBase _methodBase;
            private MethodHandle _methodHandle;

            private Handle[] _returnTypeAndParametersHandlesCache;
            private Type[] _returnTypeAndParametersTypesCache;

            public MethodParametersInfo(MethodBase methodBase)
            {
                _metadataReader = null;
                _methodBase = methodBase;
                _methodHandle = default(MethodHandle);
                _returnTypeAndParametersHandlesCache = null;
                _returnTypeAndParametersTypesCache = null;
            }

            public MethodParametersInfo(MetadataReader metadataReader, MethodBase methodBase, MethodHandle methodHandle)
            {
                _metadataReader = metadataReader;
                _methodBase = methodBase;
                _methodHandle = methodHandle;
                _returnTypeAndParametersHandlesCache = null;
                _returnTypeAndParametersTypesCache = null;
            }

            public LowLevelList<RuntimeTypeHandle> ParameterTypeHandles
            {
                get
                {
                    ParameterInfo[] parameters = _methodBase.GetParametersNoCopy();
                    LowLevelList<RuntimeTypeHandle> result = new LowLevelList<RuntimeTypeHandle>(parameters.Length);

                    for (int i = 0; i < parameters.Length; i++)
                    {
                        Type parameterType = parameters[i].ParameterType;

                        if (parameterType.IsByRef)
                            result.Add(parameterType.GetElementType().TypeHandle);
                        else if (parameterType.IsEnum && !parameters[i].HasDefaultValue)
                            result.Add(Enum.GetUnderlyingType(parameterType).TypeHandle);
                        else
                            result.Add(parameterType.TypeHandle);
                    }

                    return result;
                }
            }

            public RuntimeTypeHandle ReturnTypeHandle
            {
                get
                {
                    MethodInfo reflectionMethodInfo = _methodBase as MethodInfo;
                    Type returnType = reflectionMethodInfo != null ? reflectionMethodInfo.ReturnType : typeof(void);
                    if (returnType.IsByRef)
                        returnType = returnType.GetElementType();
                    return returnType.TypeHandle;
                }
            }

            public LowLevelList<RuntimeTypeHandle> ReturnTypeAndParameterTypeHandles
            {
                get
                {
                    LowLevelList<RuntimeTypeHandle> result = ParameterTypeHandles;
                    result.Insert(0, ReturnTypeHandle);
                    return result;
                }
            }

            public bool[] ReturnTypeAndParametersByRefFlags
            {
                get
                {
                    ParameterInfo[] parameters = _methodBase.GetParametersNoCopy();
                    bool[] result = new bool[parameters.Length + 1];

                    MethodInfo reflectionMethodInfo = _methodBase as MethodInfo;
                    Type returnType = reflectionMethodInfo != null ? reflectionMethodInfo.ReturnType : typeof(void);
                    result[0] = returnType.IsByRef;

                    for (int i = 0; i < parameters.Length; i++)
                        result[i + 1] = parameters[i].ParameterType.IsByRef;

                    return result;
                }
            }

            private void GetReturnTypeAndParameterTypesAndMDHandles(ref Handle[] handles, ref Type[] types)
            {
                if (_returnTypeAndParametersTypesCache == null)
                {
                    Debug.Assert(_metadataReader != null && !_methodHandle.Equals(default(MethodHandle)));

                    _returnTypeAndParametersHandlesCache = new Handle[_methodBase.GetParametersNoCopy().Length + 1];
                    _returnTypeAndParametersTypesCache = new Type[_methodBase.GetParametersNoCopy().Length + 1];

                    MethodSignature signature = _methodHandle.GetMethod(_metadataReader).Signature.GetMethodSignature(_metadataReader);

                    // Check the return type for generic vars
                    MethodInfo reflectionMethodInfo = _methodBase as MethodInfo;
                    _returnTypeAndParametersTypesCache[0] = reflectionMethodInfo != null ? reflectionMethodInfo.ReturnType : typeof(void);
                    _returnTypeAndParametersHandlesCache[0] = signature.ReturnType;

                    // Check the method parameters for generic vars
                    int index = 1;
                    foreach (Handle paramSigHandle in signature.Parameters)
                    {
                        _returnTypeAndParametersHandlesCache[index] = paramSigHandle;
                        _returnTypeAndParametersTypesCache[index] = _methodBase.GetParametersNoCopy()[index - 1].ParameterType;
                        index++;
                    }
                }

                handles = _returnTypeAndParametersHandlesCache;
                types = _returnTypeAndParametersTypesCache;
                Debug.Assert(handles != null && types != null);
            }
        }
    }
}
