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
        [StructLayout(LayoutKind.Sequential)]
        private struct FieldHandleInfo
        {
            public RuntimeTypeHandle DeclaringType;
            public FieldHandle Handle;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct DynamicMethodHandleInfo
        {
            public IntPtr DeclaringType;
            public IntPtr MethodName;
            public RuntimeSignature MethodSignature;
            public int NumGenericArgs;
            public IntPtr GenericArgsArray;
        }


        #region String conversions
        private static unsafe string GetStringFromMemoryInNativeFormat(IntPtr pointerToDataStream)
        {
            byte* dataStream = (byte*)pointerToDataStream;
            uint stringLen = NativePrimitiveDecoder.DecodeUnsigned(ref dataStream);
            return Encoding.UTF8.GetString(dataStream, checked((int)stringLen));
        }

        /// <summary>
        /// From a string, get a pointer to an allocated memory location that holds a NativeFormat encoded string.
        /// This is used for the creation of RuntimeFieldHandles from metadata.
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public unsafe IntPtr GetNativeFormatStringForString(string str)
        {
            using (_typeLoaderLock.EnterScope())
            {
                IntPtr result;
                if (_nativeFormatStrings.TryGetValue(str, out result))
                    return result;

                NativePrimitiveEncoder stringEncoder = default;
                stringEncoder.Init();
                byte[] utf8Bytes = Encoding.UTF8.GetBytes(str);
                stringEncoder.WriteUnsigned(checked((uint)utf8Bytes.Length));
                foreach (byte b in utf8Bytes)
                    stringEncoder.WriteByte(b);

                void* allocatedNativeFormatString = MemoryHelpers.AllocateMemory(stringEncoder.Size);
                unsafe
                {
                    stringEncoder.Save((byte*)allocatedNativeFormatString, stringEncoder.Size);
                }
                _nativeFormatStrings.Add(str, (IntPtr)allocatedNativeFormatString);
                return (IntPtr)allocatedNativeFormatString;
            }
        }

        private LowLevelDictionary<string, IntPtr> _nativeFormatStrings = new LowLevelDictionary<string, IntPtr>();
        #endregion


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
            private string _methodName;
            private RuntimeSignature _signature;
            private RuntimeTypeHandle[] _genericArgs;
            private int _hashcode;

            public RuntimeMethodHandleKey(RuntimeTypeHandle declaringType, string methodName, RuntimeSignature signature, RuntimeTypeHandle[] genericArgs)
            {
                // genericArgs will be null if this is a (typical or not) method definition
                // genericArgs are non-null only for instantiated generic methods.
                Debug.Assert(genericArgs == null || genericArgs.Length > 0);

                _declaringType = declaringType;
                _methodName = methodName;
                _signature = signature;
                _genericArgs = genericArgs;
                int methodNameHashCode = methodName == null ? 0 : methodName.GetHashCode();
                _hashcode = methodNameHashCode ^ signature.GetHashCode();

                if (genericArgs != null)
                    _hashcode ^= TypeHashingAlgorithms.ComputeGenericInstanceHashCode(declaringType.GetHashCode(), genericArgs);
                else
                    _hashcode ^= declaringType.GetHashCode();
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
                if (!_declaringType.Equals(other._declaringType) || _methodName != other._methodName || !_signature.Equals(other._signature))
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

            public override int GetHashCode() { return _hashcode; }
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
        /// <summary>
        /// Create a runtime method handle from name, signature and generic arguments. If the methodSignature
        /// is constructed from a metadata token, the methodName should be IntPtr.Zero, as it already encodes the method
        /// name.
        /// </summary>
        public unsafe RuntimeMethodHandle GetRuntimeMethodHandleForComponents(RuntimeTypeHandle declaringTypeHandle, IntPtr methodName, RuntimeSignature methodSignature, RuntimeTypeHandle[] genericMethodArgs)
        {
            string methodNameStr = methodName == IntPtr.Zero ? null : GetStringFromMemoryInNativeFormat(methodName);

            RuntimeMethodHandleKey key = new RuntimeMethodHandleKey(declaringTypeHandle, methodNameStr, methodSignature, genericMethodArgs);
            RuntimeMethodHandle runtimeMethodHandle = default(RuntimeMethodHandle);

            lock (_runtimeMethodHandles)
            {
                if (!_runtimeMethodHandles.TryGetValue(key, out runtimeMethodHandle))
                {
                    int sizeToAllocate = sizeof(DynamicMethodHandleInfo);
                    int numGenericMethodArgs = genericMethodArgs == null ? 0 : genericMethodArgs.Length;
                    // Use checked arithmetics to ensure there aren't any overflows/truncations
                    sizeToAllocate = checked(sizeToAllocate + (numGenericMethodArgs > 0 ? sizeof(IntPtr) * (numGenericMethodArgs - 1) : 0));

                    DynamicMethodHandleInfo* methodData = (DynamicMethodHandleInfo*)MemoryHelpers.AllocateMemory(sizeToAllocate);
                    methodData->DeclaringType = *(IntPtr*)&declaringTypeHandle;
                    methodData->MethodName = methodName;
                    methodData->MethodSignature = methodSignature;
                    methodData->NumGenericArgs = numGenericMethodArgs;
                    IntPtr* genericArgPtr = &(methodData->GenericArgsArray);
                    for (int i = 0; i < numGenericMethodArgs; i++)
                    {
                        RuntimeTypeHandle currentArg = genericMethodArgs[i];
                        genericArgPtr[i] = *(IntPtr*)&currentArg;
                    }

                    // Special flag in the handle value to indicate it was dynamically allocated, and doesn't point into the InvokeMap blob
                    IntPtr runtimeMethodHandleValue = (IntPtr)methodData + 1;
                    runtimeMethodHandle = *(RuntimeMethodHandle*)&runtimeMethodHandleValue;

                    _runtimeMethodHandles.Add(key, runtimeMethodHandle);
                }

                return runtimeMethodHandle;
            }
        }
        public RuntimeMethodHandle GetRuntimeMethodHandleForComponents(RuntimeTypeHandle declaringTypeHandle, string methodName, RuntimeSignature methodSignature, RuntimeTypeHandle[] genericMethodArgs)
        {
            IntPtr nameAsIntPtr = GetNativeFormatStringForString(methodName);
            return GetRuntimeMethodHandleForComponents(declaringTypeHandle, nameAsIntPtr, methodSignature, genericMethodArgs);
        }

        public MethodDesc GetMethodDescForRuntimeMethodHandle(TypeSystemContext context, RuntimeMethodHandle runtimeMethodHandle)
        {
            return runtimeMethodHandle.IsDynamic() ?
                GetMethodDescForDynamicRuntimeMethodHandle(context, runtimeMethodHandle) :
                GetMethodDescForStaticRuntimeMethodHandle(context, runtimeMethodHandle);
        }

        public bool TryGetRuntimeMethodHandleComponents(RuntimeMethodHandle runtimeMethodHandle, out RuntimeTypeHandle declaringTypeHandle, out MethodNameAndSignature nameAndSignature, out RuntimeTypeHandle[] genericMethodArgs)
        {
            return runtimeMethodHandle.IsDynamic() ?
                TryGetDynamicRuntimeMethodHandleComponents(runtimeMethodHandle, out declaringTypeHandle, out nameAndSignature, out genericMethodArgs) :
                TryGetStaticRuntimeMethodHandleComponents(runtimeMethodHandle, out declaringTypeHandle, out nameAndSignature, out genericMethodArgs);
        }

        private unsafe bool TryGetDynamicRuntimeMethodHandleComponents(RuntimeMethodHandle runtimeMethodHandle, out RuntimeTypeHandle declaringTypeHandle, out MethodNameAndSignature nameAndSignature, out RuntimeTypeHandle[] genericMethodArgs)
        {
            IntPtr runtimeMethodHandleValue = *(IntPtr*)&runtimeMethodHandle;

            // Special flag in the handle value to indicate it was dynamically allocated, and doesn't point into the InvokeMap blob
            Debug.Assert((runtimeMethodHandleValue & 0x1) == 0x1);

            DynamicMethodHandleInfo* methodData = (DynamicMethodHandleInfo*)(runtimeMethodHandleValue - 1);

            declaringTypeHandle = *(RuntimeTypeHandle*)&(methodData->DeclaringType);
            genericMethodArgs = null;

            if (methodData->NumGenericArgs > 0)
            {
                IntPtr* genericArgPtr = &(methodData->GenericArgsArray);
                genericMethodArgs = new RuntimeTypeHandle[methodData->NumGenericArgs];
                for (int i = 0; i < methodData->NumGenericArgs; i++)
                {
                    genericMethodArgs[i] = *(RuntimeTypeHandle*)&(genericArgPtr[i]);
                }
            }

            if (methodData->MethodSignature.IsNativeLayoutSignature)
            {
                // MethodName points to the method name in NativeLayout format, so we parse it using a NativeParser
                IntPtr methodNamePtr = methodData->MethodName;
                string name = GetStringFromMemoryInNativeFormat(methodNamePtr);

                nameAndSignature = new MethodNameAndSignature(name, methodData->MethodSignature);
            }
            else
            {
                ModuleInfo moduleInfo = methodData->MethodSignature.GetModuleInfo();
                var metadataReader = ((NativeFormatModuleInfo)moduleInfo).MetadataReader;
                var methodHandle = methodData->MethodSignature.Token.AsHandle().ToMethodHandle(metadataReader);
                var method = methodHandle.GetMethod(metadataReader);
                var name = metadataReader.GetConstantStringValue(method.Name).Value;
                nameAndSignature = new MethodNameAndSignature(name, methodData->MethodSignature);
            }

            return true;
        }
        public MethodDesc GetMethodDescForDynamicRuntimeMethodHandle(TypeSystemContext context, RuntimeMethodHandle runtimeMethodHandle)
        {
            bool success = TryGetDynamicRuntimeMethodHandleComponents(runtimeMethodHandle, out RuntimeTypeHandle declaringTypeHandle,
                out MethodNameAndSignature nameAndSignature, out RuntimeTypeHandle[] genericMethodArgs);
            Debug.Assert(success);

            DefType type = (DefType)context.ResolveRuntimeTypeHandle(declaringTypeHandle);

            if (genericMethodArgs != null)
            {
                Instantiation methodInst = context.ResolveRuntimeTypeHandles(genericMethodArgs);
                return context.ResolveGenericMethodInstantiation(unboxingStub: false, type, nameAndSignature, methodInst);
            }

            return context.ResolveRuntimeMethod(unboxingStub: false, type, nameAndSignature);
        }

        private unsafe bool TryGetStaticRuntimeMethodHandleComponents(RuntimeMethodHandle runtimeMethodHandle, out RuntimeTypeHandle declaringTypeHandle, out MethodNameAndSignature nameAndSignature, out RuntimeTypeHandle[] genericMethodArgs)
        {
            declaringTypeHandle = default(RuntimeTypeHandle);
            nameAndSignature = null;
            genericMethodArgs = null;

            TypeSystemContext context = TypeSystemContextFactory.Create();

            MethodDesc parsedMethod = GetMethodDescForStaticRuntimeMethodHandle(context, runtimeMethodHandle);

            if (!EnsureTypeHandleForType(parsedMethod.OwningType))
                return false;

            declaringTypeHandle = parsedMethod.OwningType.RuntimeTypeHandle;
            nameAndSignature = parsedMethod.NameAndSignature;
            if (!parsedMethod.IsMethodDefinition && parsedMethod.Instantiation.Length > 0)
            {
                genericMethodArgs = new RuntimeTypeHandle[parsedMethod.Instantiation.Length];
                for (int i = 0; i < parsedMethod.Instantiation.Length; ++i)
                {
                    if (!EnsureTypeHandleForType(parsedMethod.Instantiation[i]))
                        return false;

                    genericMethodArgs[i] = parsedMethod.Instantiation[i].RuntimeTypeHandle;
                }
            }

            TypeSystemContextFactory.Recycle(context);
            return true;
        }

        public unsafe MethodDesc GetMethodDescForStaticRuntimeMethodHandle(TypeSystemContext context, RuntimeMethodHandle runtimeMethodHandle)
        {
            // Make sure it's not a dynamically allocated RuntimeMethodHandle before we attempt to use it to parse native layout data
            Debug.Assert(((*(IntPtr*)&runtimeMethodHandle).ToInt64() & 0x1) == 0);

            RuntimeMethodHandleInfo* methodData = *(RuntimeMethodHandleInfo**)&runtimeMethodHandle;
            RuntimeSignature signature;

            // The native layout info signature is a pair.
            // The first is a pointer that points to the TypeManager indirection cell.
            // The second is the offset into the native layout info blob in that TypeManager, where the native signature is encoded.
            IntPtr* nativeLayoutInfoSignatureData = (IntPtr*)methodData->NativeLayoutInfoSignature;

            signature = RuntimeSignature.CreateFromNativeLayoutSignature(
                new TypeManagerHandle(*(IntPtr*)nativeLayoutInfoSignatureData[0]),
                (uint)nativeLayoutInfoSignatureData[1].ToInt32());

            RuntimeSignature remainingSignature;
            return GetMethodFromSignatureAndContext(context, signature, null, null, out remainingSignature);
        }
        #endregion
    }
}
