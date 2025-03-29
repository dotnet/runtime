// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Runtime.General;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

using Internal.Metadata.NativeFormat;
using Internal.NativeFormat;
using Internal.Runtime.Augments;
using Internal.Runtime.CompilerServices;
using Internal.TypeSystem;

using Debug = System.Diagnostics.Debug;

namespace Internal.Runtime.TypeLoader
{
    public sealed partial class TypeLoaderEnvironment
    {
        #region Ldtoken Hashtables
        private struct RuntimeFieldHandleKey : IEquatable<RuntimeFieldHandleKey>
        {
            private RuntimeTypeHandle _declaringType;
            private FieldHandle _handle;

            public RuntimeFieldHandleKey(RuntimeTypeHandle declaringType, FieldHandle fieldHandle)
            {
                _declaringType = declaringType;
                _handle = fieldHandle;
            }

            public override bool Equals(object obj)
            {
                if (obj is RuntimeFieldHandleKey other)
                {
                    return Equals(other);
                }
                return false;
            }

            public bool Equals(RuntimeFieldHandleKey other)
            {
                return other._declaringType.Equals(_declaringType) && other._handle.Equals(_handle);
            }

            public override int GetHashCode() => _declaringType.GetHashCode() ^ _handle.GetHashCode();
        }

        private struct RuntimeMethodHandleKey : IEquatable<RuntimeMethodHandleKey>
        {
            private RuntimeTypeHandle _declaringType;
            private MethodHandle _handle;
            private RuntimeTypeHandle[] _genericArgs;

            public RuntimeMethodHandleKey(RuntimeTypeHandle declaringType, MethodHandle handle, RuntimeTypeHandle[] genericArgs)
            {
                // genericArgs will be null if this is a (typical or not) method definition
                // genericArgs are non-null only for instantiated generic methods.
                Debug.Assert(genericArgs == null || genericArgs.Length > 0);

                _declaringType = declaringType;
                _handle = handle;
                _genericArgs = genericArgs;
            }

            public override bool Equals(object obj)
            {
                if (obj is RuntimeMethodHandleKey other)
                {
                    return Equals(other);
                }
                return false;
            }

            public bool Equals(RuntimeMethodHandleKey other)
            {
                if (!_declaringType.Equals(other._declaringType) || !_handle.Equals(other._handle))
                    return false;

                if ((_genericArgs == null) != (other._genericArgs == null))
                    return false;

                if (_genericArgs != null)
                {
                    if (_genericArgs.Length != other._genericArgs.Length)
                        return false;

                    for (int i = 0; i < _genericArgs.Length; i++)
                        if (!_genericArgs[i].Equals(other._genericArgs[i]))
                            return false;
                }

                return true;
            }

            public override int GetHashCode()
                => _handle.GetHashCode() ^ (_genericArgs == null
                ? _declaringType.GetHashCode()
                : TypeHashingAlgorithms.ComputeGenericInstanceHashCode(_declaringType.GetHashCode(), _genericArgs));
        }

        private LowLevelDictionary<RuntimeFieldHandleKey, RuntimeFieldHandle> _runtimeFieldHandles = new LowLevelDictionary<RuntimeFieldHandleKey, RuntimeFieldHandle>();
        private LowLevelDictionary<RuntimeMethodHandleKey, RuntimeMethodHandle> _runtimeMethodHandles = new LowLevelDictionary<RuntimeMethodHandleKey, RuntimeMethodHandle>();
        #endregion


        #region Field Ldtoken Functions
        public unsafe RuntimeFieldHandle GetRuntimeFieldHandleForComponents(RuntimeTypeHandle declaringTypeHandle, int handle)
        {
            return GetRuntimeFieldHandleForComponents(declaringTypeHandle, handle.AsHandle().ToFieldHandle(null));
        }

        public unsafe RuntimeFieldHandle GetRuntimeFieldHandleForComponents(RuntimeTypeHandle declaringTypeHandle, FieldHandle handle)
        {
            RuntimeFieldHandleKey key = new RuntimeFieldHandleKey(declaringTypeHandle, handle);

            lock (_runtimeFieldHandles)
            {
                if (!_runtimeFieldHandles.TryGetValue(key, out RuntimeFieldHandle runtimeFieldHandle))
                {
                    FieldHandleInfo* fieldData = (FieldHandleInfo*)MemoryHelpers.AllocateMemory(sizeof(FieldHandleInfo));
                    fieldData->DeclaringType = declaringTypeHandle;
                    fieldData->Handle = handle;
                    runtimeFieldHandle = RuntimeFieldHandle.FromIntPtr((nint)fieldData);

                    _runtimeFieldHandles.Add(key, runtimeFieldHandle);
                }

                return runtimeFieldHandle;
            }
        }

        public unsafe bool TryGetRuntimeFieldHandleComponents(RuntimeFieldHandle runtimeFieldHandle, out RuntimeTypeHandle declaringTypeHandle, out FieldHandle fieldHandle)
        {
            FieldHandleInfo* fieldData = (FieldHandleInfo*)runtimeFieldHandle.Value;
            declaringTypeHandle = fieldData->DeclaringType;
            fieldHandle = fieldData->Handle;
            return true;
        }
        #endregion


        #region Method Ldtoken Functions
        public unsafe RuntimeMethodHandle GetRuntimeMethodHandleForComponents(RuntimeTypeHandle declaringTypeHandle, int handle, RuntimeTypeHandle[] genericMethodArgs)
            => GetRuntimeMethodHandleForComponents(declaringTypeHandle, handle.AsHandle().ToMethodHandle(null), genericMethodArgs);

        /// <summary>
        /// Create a runtime method handle from name, signature and generic arguments. If the methodSignature
        /// is constructed from a metadata token, the methodName should be IntPtr.Zero, as it already encodes the method
        /// name.
        /// </summary>
        public unsafe RuntimeMethodHandle GetRuntimeMethodHandleForComponents(RuntimeTypeHandle declaringTypeHandle, MethodHandle handle, RuntimeTypeHandle[] genericMethodArgs)
        {
            RuntimeMethodHandleKey key = new RuntimeMethodHandleKey(declaringTypeHandle, handle, genericMethodArgs);

            lock (_runtimeMethodHandles)
            {
                if (!_runtimeMethodHandles.TryGetValue(key, out RuntimeMethodHandle runtimeMethodHandle))
                {
                    int sizeToAllocate = sizeof(MethodHandleInfo);
                    int numGenericMethodArgs = genericMethodArgs == null ? 0 : genericMethodArgs.Length;
                    // Use checked arithmetics to ensure there aren't any overflows/truncations
                    sizeToAllocate = checked(sizeToAllocate + (numGenericMethodArgs > 0 ? sizeof(IntPtr) * (numGenericMethodArgs - 1) : 0));

                    MethodHandleInfo* methodData = (MethodHandleInfo*)MemoryHelpers.AllocateMemory(sizeToAllocate);
                    methodData->DeclaringType = declaringTypeHandle;
                    methodData->Handle = handle;
                    methodData->NumGenericArgs = numGenericMethodArgs;
                    RuntimeTypeHandle* genericArgPtr = &methodData->FirstArgument;
                    for (int i = 0; i < numGenericMethodArgs; i++)
                    {
                        RuntimeTypeHandle currentArg = genericMethodArgs[i];
                        genericArgPtr[i] = currentArg;
                    }

                    runtimeMethodHandle = RuntimeMethodHandle.FromIntPtr((nint)methodData);

                    _runtimeMethodHandles.Add(key, runtimeMethodHandle);
                }

                return runtimeMethodHandle;
            }
        }

        public MethodDesc GetMethodDescForRuntimeMethodHandle(TypeSystemContext context, RuntimeMethodHandle runtimeMethodHandle)
        {
            bool success = TryGetRuntimeMethodHandleComponents(runtimeMethodHandle, out RuntimeTypeHandle declaringTypeHandle,
                out MethodHandle handle, out RuntimeTypeHandle[] genericMethodArgs);
            Debug.Assert(success);

            MetadataReader reader = ModuleList.Instance.GetMetadataReaderForModule(RuntimeAugments.GetModuleFromTypeHandle(declaringTypeHandle));
            MethodNameAndSignature nameAndSignature = new MethodNameAndSignature(reader, handle);

            DefType type = (DefType)context.ResolveRuntimeTypeHandle(declaringTypeHandle);

            if (genericMethodArgs != null)
            {
                Instantiation methodInst = context.ResolveRuntimeTypeHandles(genericMethodArgs);
                return context.ResolveGenericMethodInstantiation(unboxingStub: false, type, nameAndSignature, methodInst);
            }

            return context.ResolveRuntimeMethod(unboxingStub: false, type, nameAndSignature);
        }

        public unsafe bool TryGetRuntimeMethodHandleComponents(RuntimeMethodHandle runtimeMethodHandle, out RuntimeTypeHandle declaringTypeHandle, out QMethodDefinition handle, out RuntimeTypeHandle[] genericMethodArgs)
        {
            if (TryGetRuntimeMethodHandleComponents(runtimeMethodHandle, out declaringTypeHandle, out MethodHandle methodHandle, out genericMethodArgs))
            {
                MetadataReader reader = ModuleList.Instance.GetMetadataReaderForModule(RuntimeAugments.GetModuleFromTypeHandle(declaringTypeHandle));
                handle = new QMethodDefinition(reader, methodHandle);
                return true;
            }
            handle = default;
            return false;
        }

        public unsafe bool TryGetRuntimeMethodHandleComponents(RuntimeMethodHandle runtimeMethodHandle, out RuntimeTypeHandle declaringTypeHandle, out MethodHandle handle, out RuntimeTypeHandle[] genericMethodArgs)
        {
            MethodHandleInfo* methodData = (MethodHandleInfo*)runtimeMethodHandle.Value;

            declaringTypeHandle = methodData->DeclaringType;
            handle = methodData->Handle;

            if (methodData->NumGenericArgs > 0)
            {
                RuntimeTypeHandle* genericArgPtr = (RuntimeTypeHandle*)&methodData->FirstArgument;
                genericMethodArgs = new RuntimeTypeHandle[methodData->NumGenericArgs];
                for (int i = 0; i < methodData->NumGenericArgs; i++)
                {
                    genericMethodArgs[i] = genericArgPtr[i];
                }
            }
            else
            {
                genericMethodArgs = null;
            }
            return true;
        }
        #endregion
    }
}
