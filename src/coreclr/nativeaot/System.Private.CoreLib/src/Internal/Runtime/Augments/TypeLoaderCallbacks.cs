// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime;

using Internal.Metadata.NativeFormat;
using Internal.NativeFormat;
using Internal.Runtime.CompilerServices;

namespace Internal.Runtime.Augments
{
    [CLSCompliant(false)]
    public abstract class TypeLoaderCallbacks
    {
        public abstract bool TryGetOwningTypeForMethodDictionary(IntPtr dictionary, out RuntimeTypeHandle owningType);
        public abstract TypeManagerHandle GetModuleForMetadataReader(MetadataReader reader);
        public abstract bool TryGetConstructedGenericTypeForComponents(RuntimeTypeHandle genericTypeDefinitionHandle, RuntimeTypeHandle[] genericTypeArgumentHandles, out RuntimeTypeHandle runtimeTypeHandle);
        public abstract IntPtr GetThreadStaticGCDescForDynamicType(TypeManagerHandle handle, int index);
        public abstract IntPtr GenericLookupFromContextAndSignature(IntPtr context, IntPtr signature, out IntPtr auxResult);
        public abstract RuntimeMethodHandle GetRuntimeMethodHandleForComponents(RuntimeTypeHandle declaringTypeHandle, MethodHandle handle, RuntimeTypeHandle[] genericMethodArgs);
        public abstract IntPtr TryGetDefaultConstructorForType(RuntimeTypeHandle runtimeTypeHandle);
        public abstract IntPtr ResolveGenericVirtualMethodTarget(RuntimeTypeHandle targetTypeHandle, RuntimeMethodHandle declMethod);
        public abstract RuntimeFieldHandle GetRuntimeFieldHandleForComponents(RuntimeTypeHandle declaringTypeHandle, FieldHandle handle);
        public abstract bool TryGetPointerTypeForTargetType(RuntimeTypeHandle pointeeTypeHandle, out RuntimeTypeHandle pointerTypeHandle);
        public abstract bool TryGetArrayTypeForElementType(RuntimeTypeHandle elementTypeHandle, bool isMdArray, int rank, out RuntimeTypeHandle arrayTypeHandle);
        /// <summary>
        /// Convert an unboxing function pointer to a non-unboxing function pointer
        /// </summary>
        public abstract IntPtr ConvertUnboxingFunctionPointerToUnderlyingNonUnboxingPointer(IntPtr unboxingFunctionPointer, RuntimeTypeHandle declaringType);
    }
}
