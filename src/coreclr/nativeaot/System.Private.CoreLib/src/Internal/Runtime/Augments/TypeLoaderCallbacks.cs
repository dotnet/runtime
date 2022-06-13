// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime;
using System.Collections.Generic;

using Internal.Runtime.CompilerServices;
using Internal.NativeFormat;
using Internal.Metadata.NativeFormat;

namespace Internal.Runtime.Augments
{
    [CLSCompliant(false)]
    [System.Runtime.CompilerServices.ReflectionBlocked]
    public abstract class TypeLoaderCallbacks
    {
        public abstract TypeManagerHandle GetModuleForMetadataReader(MetadataReader reader);
        public abstract bool TryGetConstructedGenericTypeForComponents(RuntimeTypeHandle genericTypeDefinitionHandle, RuntimeTypeHandle[] genericTypeArgumentHandles, out RuntimeTypeHandle runtimeTypeHandle);
        public abstract int GetThreadStaticsSizeForDynamicType(int index, out int numTlsCells);
        public abstract IntPtr GenericLookupFromContextAndSignature(IntPtr context, IntPtr signature, out IntPtr auxResult);
        public abstract bool GetRuntimeMethodHandleComponents(RuntimeMethodHandle runtimeMethodHandle, out RuntimeTypeHandle declaringTypeHandle, out MethodNameAndSignature nameAndSignature, out RuntimeTypeHandle[] genericMethodArgs);
        public abstract RuntimeMethodHandle GetRuntimeMethodHandleForComponents(RuntimeTypeHandle declaringTypeHandle, string methodName, RuntimeSignature methodSignature, RuntimeTypeHandle[] genericMethodArgs);
        public abstract bool CompareMethodSignatures(RuntimeSignature signature1, RuntimeSignature signature2);
#if FEATURE_UNIVERSAL_GENERICS
        public abstract IntPtr GetDelegateThunk(Delegate delegateObject, int thunkKind);
#endif
        public abstract IntPtr TryGetDefaultConstructorForType(RuntimeTypeHandle runtimeTypeHandle);
        public abstract bool TryGetGenericVirtualTargetForTypeAndSlot(RuntimeTypeHandle targetHandle, ref RuntimeTypeHandle declaringType, RuntimeTypeHandle[] genericArguments, ref string methodName, ref RuntimeSignature methodSignature, bool lookForDefaultImplementations, out IntPtr methodPointer, out IntPtr dictionaryPointer, out bool slotUpdated);
        public abstract bool GetRuntimeFieldHandleComponents(RuntimeFieldHandle runtimeFieldHandle, out RuntimeTypeHandle declaringTypeHandle, out string fieldName);
        public abstract RuntimeFieldHandle GetRuntimeFieldHandleForComponents(RuntimeTypeHandle declaringTypeHandle, string fieldName);
        public abstract bool TryGetPointerTypeForTargetType(RuntimeTypeHandle pointeeTypeHandle, out RuntimeTypeHandle pointerTypeHandle);
        public abstract bool TryGetArrayTypeForElementType(RuntimeTypeHandle elementTypeHandle, bool isMdArray, int rank, out RuntimeTypeHandle arrayTypeHandle);
        public abstract IntPtr UpdateFloatingDictionary(IntPtr context, IntPtr dictionaryPtr);

        /// <summary>
        /// Register a new runtime-allocated code thunk in the diagnostic stream.
        /// </summary>
        /// <param name="thunkAddress">Address of thunk to register</param>
        public abstract void RegisterThunk(IntPtr thunkAddress);

        /// <summary>
        /// Convert an unboxing function pointer to a non-unboxing function pointer
        /// </summary>
        public abstract IntPtr ConvertUnboxingFunctionPointerToUnderlyingNonUnboxingPointer(IntPtr unboxingFunctionPointer, RuntimeTypeHandle declaringType);
    }
}
