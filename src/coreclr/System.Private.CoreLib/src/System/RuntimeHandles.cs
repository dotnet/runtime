// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Runtime.Serialization;
using System.Runtime.Versioning;
using System.Threading;

namespace System
{
    [NonVersionable]
    public unsafe partial struct RuntimeTypeHandle : IEquatable<RuntimeTypeHandle>, ISerializable
    {
        // Returns handle for interop with EE. The handle is guaranteed to be non-null.
        internal RuntimeTypeHandle GetNativeHandle() =>
            new RuntimeTypeHandle(GetRuntimeTypeChecked());

        // Returns type for interop with EE. The type is guaranteed to be non-null.
        internal RuntimeType GetRuntimeTypeChecked() =>
            m_type ?? throw new ArgumentNullException(null, SR.Arg_InvalidHandle);

        /// <summary>
        /// Returns a new <see cref="RuntimeTypeHandle"/> object created from a handle to a RuntimeType.
        /// </summary>
        /// <param name="value">An IntPtr handle to a RuntimeType to create a <see cref="RuntimeTypeHandle"/> object from.</param>
        /// <returns>A new <see cref="RuntimeTypeHandle"/> object that corresponds to the value parameter.</returns>
        public static RuntimeTypeHandle FromIntPtr(IntPtr value) =>
            new RuntimeTypeHandle(GetRuntimeTypeFromHandleMaybeNull(value));

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "RuntimeTypeHandle_GetRuntimeTypeFromHandleSlow")]
        private static partial void GetRuntimeTypeFromHandleSlow(
            IntPtr handle,
            ObjectHandleOnStack typeObject);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static RuntimeType GetRuntimeTypeFromHandleSlow(IntPtr handle)
        {
            RuntimeType? typeObject = null;
            GetRuntimeTypeFromHandleSlow(handle, ObjectHandleOnStack.Create(ref typeObject));
            return typeObject!;
        }

        // implementation of CORINFO_HELP_GETSYNCFROMCLASSHANDLE, CORINFO_HELP_TYPEHANDLE_TO_RUNTIMETYPE, CORINFO_HELP_TYPEHANDLE_TO_RUNTIMETYPEHANDLE
        internal static unsafe RuntimeType GetRuntimeTypeFromHandle(IntPtr handle)
        {
            TypeHandle h = new((void*)handle);
            return (h.IsTypeDesc
                ? h.AsTypeDesc()->ExposedClassObject
                : h.AsMethodTable()->AuxiliaryData->ExposedClassObject) ?? GetRuntimeTypeFromHandleSlow(handle);
        }

        // implementation of CORINFO_HELP_TYPEHANDLE_TO_RUNTIMETYPE_MAYBENULL, CORINFO_HELP_TYPEHANDLE_TO_RUNTIMETYPEHANDLE_MAYBENULL
        internal static RuntimeType? GetRuntimeTypeFromHandleMaybeNull(IntPtr handle)
        {
            if (handle == IntPtr.Zero)
            {
                return null;
            }

            return GetRuntimeTypeFromHandle(handle);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe RuntimeType GetRuntimeType(MethodTable* pMT)
        {
            return pMT->AuxiliaryData->ExposedClassObject ?? GetRuntimeTypeFromHandleSlow((IntPtr)pMT);
        }

        /// <summary>
        /// Returns the internal pointer representation of a <see cref="RuntimeTypeHandle"/> object.
        /// </summary>
        /// <param name="value">A <see cref="RuntimeTypeHandle"/> object to retrieve an internal pointer representation from.</param>
        /// <returns>An <see cref="IntPtr"/> object that represents a <see cref="RuntimeTypeHandle"/> object.</returns>
        [Intrinsic]
        public static IntPtr ToIntPtr(RuntimeTypeHandle value) => value.Value;

        public static bool operator ==(RuntimeTypeHandle left, object? right) => left.Equals(right);

        public static bool operator ==(object? left, RuntimeTypeHandle right) => right.Equals(left);

        public static bool operator !=(RuntimeTypeHandle left, object? right) => !left.Equals(right);

        public static bool operator !=(object? left, RuntimeTypeHandle right) => !right.Equals(left);

        // This is the RuntimeType for the type
        internal RuntimeType? m_type;

        public override int GetHashCode()
            => m_type?.GetHashCode() ?? 0;

        public override bool Equals(object? obj)
            => (obj is RuntimeTypeHandle handle) && ReferenceEquals(handle.m_type, m_type);

        public bool Equals(RuntimeTypeHandle handle)
            => ReferenceEquals(handle.m_type, m_type);

        public IntPtr Value => m_type?.m_handle ?? 0;

        internal RuntimeTypeHandle(RuntimeType? type)
        {
            m_type = type;
        }

        internal bool IsNullHandle()
        {
            return m_type == null;
        }

        internal static bool IsTypeDefinition(RuntimeType type)
        {
            CorElementType corElemType = type.GetCorElementType();
            if (!((corElemType >= CorElementType.ELEMENT_TYPE_VOID && corElemType < CorElementType.ELEMENT_TYPE_PTR) ||
                    corElemType == CorElementType.ELEMENT_TYPE_VALUETYPE ||
                    corElemType == CorElementType.ELEMENT_TYPE_CLASS ||
                    corElemType == CorElementType.ELEMENT_TYPE_TYPEDBYREF ||
                    corElemType == CorElementType.ELEMENT_TYPE_I ||
                    corElemType == CorElementType.ELEMENT_TYPE_U ||
                    corElemType == CorElementType.ELEMENT_TYPE_OBJECT))
                return false;

            if (type.IsConstructedGenericType)
                return false;

            return true;
        }

        internal static bool IsPrimitive(RuntimeType type)
        {
            return RuntimeHelpers.IsPrimitiveType(type.GetCorElementType());
        }

        internal static bool IsByRef(RuntimeType type)
        {
            CorElementType corElemType = type.GetCorElementType();
            return corElemType == CorElementType.ELEMENT_TYPE_BYREF;
        }

        internal static bool IsPointer(RuntimeType type)
        {
            CorElementType corElemType = type.GetCorElementType();
            return corElemType == CorElementType.ELEMENT_TYPE_PTR;
        }

        internal static bool IsArray(RuntimeType type)
        {
            CorElementType corElemType = type.GetCorElementType();
            return corElemType == CorElementType.ELEMENT_TYPE_ARRAY || corElemType == CorElementType.ELEMENT_TYPE_SZARRAY;
        }

        internal static bool IsSZArray(RuntimeType type)
        {
            CorElementType corElemType = type.GetCorElementType();
            return corElemType == CorElementType.ELEMENT_TYPE_SZARRAY;
        }

        internal static bool IsFunctionPointer(RuntimeType type)
        {
            CorElementType corElemType = type.GetCorElementType();
            return corElemType == CorElementType.ELEMENT_TYPE_FNPTR;
        }

        internal static bool HasElementType(RuntimeType type)
        {
            CorElementType corElemType = type.GetCorElementType();

            return corElemType == CorElementType.ELEMENT_TYPE_ARRAY || corElemType == CorElementType.ELEMENT_TYPE_SZARRAY // IsArray
                   || (corElemType == CorElementType.ELEMENT_TYPE_PTR)                                          // IsPointer
                   || (corElemType == CorElementType.ELEMENT_TYPE_BYREF);                                      // IsByRef
        }

        // ** WARNING **
        // Caller bears responsibility for ensuring that the provided Types remain
        // GC-reachable while the unmanaged handles are being manipulated. The caller
        // may need to make a defensive copy of the input array to ensure it's not
        // mutated by another thread, and this defensive copy should be passed to
        // a KeepAlive routine.
        internal static ReadOnlySpan<IntPtr> CopyRuntimeTypeHandles(RuntimeTypeHandle[]? inHandles, Span<IntPtr> stackScratch)
        {
            if (inHandles == null || inHandles.Length == 0)
            {
                return default;
            }

            Span<IntPtr> outHandles = inHandles.Length <= stackScratch.Length ?
                stackScratch.Slice(0, inHandles.Length) :
                new IntPtr[inHandles.Length];
            for (int i = 0; i < inHandles.Length; i++)
            {
                outHandles[i] = inHandles[i].Value;
            }
            return outHandles;
        }

        // ** WARNING **
        // Caller bears responsibility for ensuring that the provided Types remain
        // GC-reachable while the unmanaged handles are being manipulated. The caller
        // may need to make a defensive copy of the input array to ensure it's not
        // mutated by another thread, and this defensive copy should be passed to
        // a KeepAlive routine.
        internal static IntPtr[]? CopyRuntimeTypeHandles(Type[]? inHandles, out int length)
        {
            if (inHandles == null || inHandles.Length == 0)
            {
                length = 0;
                return null;
            }

            IntPtr[] outHandles = new IntPtr[inHandles.Length];
            for (int i = 0; i < inHandles.Length; i++)
            {
                outHandles[i] = inHandles[i].TypeHandle.Value;
            }
            length = outHandles.Length;
            return outHandles;
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2067:ParameterDoesntMeetParameterRequirements",
            Justification = "The parameter 'type' is passed by ref to QCallTypeHandle which only instantiates" +
                            "the type using the public parameterless constructor and doesn't modify it")]
        internal static object CreateInstanceForAnotherGenericParameter(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] RuntimeType type,
            RuntimeType genericParameter)
        {
            Debug.Assert(type.GetConstructor(Type.EmptyTypes) is ConstructorInfo c && c.IsPublic,
                $"CreateInstanceForAnotherGenericParameter requires {nameof(type)} to have a public parameterless constructor so it can be annotated for trimming without preserving private constructors.");

            object? instantiatedObject = null;

            IntPtr typeHandle = genericParameter.TypeHandle.Value;
            CreateInstanceForAnotherGenericParameter(
                new QCallTypeHandle(ref type),
                &typeHandle,
                1,
                ObjectHandleOnStack.Create(ref instantiatedObject));

            GC.KeepAlive(genericParameter);
            return instantiatedObject!;
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2067:ParameterDoesntMeetParameterRequirements",
            Justification = "The parameter 'type' is passed by ref to QCallTypeHandle which only instantiates" +
                            "the type using the public parameterless constructor and doesn't modify it")]
        internal static object CreateInstanceForAnotherGenericParameter(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] RuntimeType type,
            RuntimeType genericParameter1,
            RuntimeType genericParameter2)
        {
            Debug.Assert(type.GetConstructor(Type.EmptyTypes) is ConstructorInfo c && c.IsPublic,
                $"CreateInstanceForAnotherGenericParameter requires {nameof(type)} to have a public parameterless constructor so it can be annotated for trimming without preserving private constructors.");

            object? instantiatedObject = null;

            IntPtr* pTypeHandles = stackalloc IntPtr[]
            {
                genericParameter1.TypeHandle.Value,
                genericParameter2.TypeHandle.Value
            };

            CreateInstanceForAnotherGenericParameter(
                new QCallTypeHandle(ref type),
                pTypeHandles,
                2,
                ObjectHandleOnStack.Create(ref instantiatedObject));

            GC.KeepAlive(genericParameter1);
            GC.KeepAlive(genericParameter2);

            return instantiatedObject!;
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "RuntimeTypeHandle_CreateInstanceForAnotherGenericParameter")]
        private static partial void CreateInstanceForAnotherGenericParameter(
            QCallTypeHandle baseType,
            IntPtr* pTypeHandles,
            int cTypeHandles,
            ObjectHandleOnStack instantiatedObject);

        internal static unsafe object InternalAlloc(MethodTable* pMT)
        {
            object? result = null;
            InternalAlloc(pMT, ObjectHandleOnStack.Create(ref result));
            return result!;
        }

        internal static object InternalAlloc(RuntimeType type)
        {
            Debug.Assert(!type.GetNativeTypeHandle().IsTypeDesc);
            object result = InternalAlloc(type.GetNativeTypeHandle().AsMethodTable());
            GC.KeepAlive(type);
            return result;
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "RuntimeTypeHandle_InternalAlloc")]
        private static unsafe partial void InternalAlloc(MethodTable* pMT, ObjectHandleOnStack result);

        internal static object InternalAllocNoChecks(RuntimeType type)
        {
            Debug.Assert(!type.GetNativeTypeHandle().IsTypeDesc);
            object? result = null;
            InternalAllocNoChecks(type.GetNativeTypeHandle().AsMethodTable(), ObjectHandleOnStack.Create(ref result));
            GC.KeepAlive(type);
            return result!;
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "RuntimeTypeHandle_InternalAllocNoChecks")]
        private static unsafe partial void InternalAllocNoChecks(MethodTable* pMT, ObjectHandleOnStack result);

        /// <summary>
        /// Given a RuntimeType, returns information about how to activate it via calli
        /// semantics. This method will ensure the type object is fully initialized within
        /// the VM, but it will not call any static ctors on the type.
        /// </summary>
        internal static void GetActivationInfo(
            RuntimeType rt,
            out delegate*<void*, object> pfnAllocator,
            out void* vAllocatorFirstArg,
            out delegate*<object, void> pfnRefCtor,
            out delegate*<ref byte, void> pfnValueCtor,
            out bool ctorIsPublic)
        {
            Debug.Assert(rt != null);

            delegate*<void*, object> pfnAllocatorTemp = default;
            void* vAllocatorFirstArgTemp = default;
            delegate*<object, void> pfnRefCtorTemp = default;
            delegate*<ref byte, void> pfnValueCtorTemp = default;
            Interop.BOOL fCtorIsPublicTemp = default;

            GetActivationInfo(
                ObjectHandleOnStack.Create(ref rt),
                &pfnAllocatorTemp, &vAllocatorFirstArgTemp,
                &pfnRefCtorTemp, &pfnValueCtorTemp, &fCtorIsPublicTemp);

            pfnAllocator = pfnAllocatorTemp;
            vAllocatorFirstArg = vAllocatorFirstArgTemp;
            pfnRefCtor = pfnRefCtorTemp;
            pfnValueCtor = pfnValueCtorTemp;
            ctorIsPublic = fCtorIsPublicTemp != Interop.BOOL.FALSE;
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "RuntimeTypeHandle_GetActivationInfo")]
        private static partial void GetActivationInfo(
            ObjectHandleOnStack pRuntimeType,
            delegate*<void*, object>* ppfnAllocator,
            void** pvAllocatorFirstArg,
            delegate*<object, void>* ppfnRefCtor,
            delegate*<ref byte, void>* ppfnValueCtor,
            Interop.BOOL* pfCtorIsPublic);

#if FEATURE_COMINTEROP
        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "RuntimeTypeHandle_AllocateComObject")]
        private static partial void AllocateComObject(void* pClassFactory, ObjectHandleOnStack result);

        // Referenced by unmanaged layer (see GetActivationInfo).
        // First parameter is ComClassFactory*.
        private static object AllocateComObject(void* pClassFactory)
        {
            object? result = null;
            AllocateComObject(pClassFactory, ObjectHandleOnStack.Create(ref result));
            return result!;
        }
#endif // FEATURE_COMINTEROP

        internal RuntimeType GetRuntimeType()
        {
            return m_type!;
        }

        internal static RuntimeAssembly GetAssembly(RuntimeType type)
        {
            return GetAssemblyIfExists(type) ?? GetAssemblyWorker(type);

            [MethodImpl(MethodImplOptions.NoInlining)]
            static RuntimeAssembly GetAssemblyWorker(RuntimeType type)
            {
                RuntimeAssembly? assembly = null;
                GetAssemblySlow(ObjectHandleOnStack.Create(ref type), ObjectHandleOnStack.Create(ref assembly));
                return assembly!;
            }
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern RuntimeAssembly? GetAssemblyIfExists(RuntimeType type);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "RuntimeTypeHandle_GetAssemblySlow")]
        private static partial void GetAssemblySlow(ObjectHandleOnStack type, ObjectHandleOnStack assembly);

        internal static RuntimeModule GetModule(RuntimeType type)
        {
            return GetModuleIfExists(type) ?? GetModuleWorker(type);

            [MethodImpl(MethodImplOptions.NoInlining)]
            static RuntimeModule GetModuleWorker(RuntimeType type)
            {
                RuntimeModule? module = null;
                GetModuleSlow(ObjectHandleOnStack.Create(ref type), ObjectHandleOnStack.Create(ref module));
                return module!;
            }
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern RuntimeModule? GetModuleIfExists(RuntimeType type);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "RuntimeTypeHandle_GetModuleSlow")]
        private static partial void GetModuleSlow(ObjectHandleOnStack type, ObjectHandleOnStack module);

        public ModuleHandle GetModuleHandle()
        {
            if (m_type is null)
            {
                throw new ArgumentNullException(SR.Arg_InvalidHandle);
            }

            return new ModuleHandle(GetModule(m_type));
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern TypeAttributes GetAttributes(RuntimeType type);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern IntPtr GetElementTypeHandle(IntPtr handle);

        internal static RuntimeType? GetElementType(RuntimeType type)
        {
            IntPtr handle = GetElementTypeHandle(type.GetUnderlyingNativeHandle());
            if (handle == IntPtr.Zero)
            {
                return null;
            }

            RuntimeType result = GetRuntimeTypeFromHandle(handle);
            GC.KeepAlive(type);
            return result;
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern bool CompareCanonicalHandles(RuntimeType left, RuntimeType right);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern int GetArrayRank(RuntimeType type);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern int GetToken(RuntimeType type);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "RuntimeTypeHandle_GetMethodAt")]
        private static unsafe partial IntPtr GetMethodAt(MethodTable* pMT, int slot);

        internal static RuntimeMethodHandleInternal GetMethodAt(RuntimeType type, int slot)
        {
            TypeHandle typeHandle = type.GetNativeTypeHandle();
            if (typeHandle.IsTypeDesc)
            {
                throw new ArgumentException(SR.Arg_InvalidHandle);
            }

            if (slot < 0)
            {
                throw new ArgumentException(SR.Arg_ArgumentOutOfRangeException);
            }

            return new RuntimeMethodHandleInternal(GetMethodAt(typeHandle.AsMethodTable(), slot));
        }

        internal static Type[] GetArgumentTypesFromFunctionPointer(RuntimeType type)
        {
            Debug.Assert(type.IsFunctionPointer);
            Type[]? argTypes = null;
            GetArgumentTypesFromFunctionPointer(new QCallTypeHandle(ref type), ObjectHandleOnStack.Create(ref argTypes));
            return argTypes!;
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "RuntimeTypeHandle_GetArgumentTypesFromFunctionPointer")]
        private static partial void GetArgumentTypesFromFunctionPointer(QCallTypeHandle type, ObjectHandleOnStack argTypes);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern bool IsUnmanagedFunctionPointer(RuntimeType type);

        // This is managed wrapper for MethodTable::IntroducedMethodIterator
        internal struct IntroducedMethodEnumerator
        {
            private bool _firstCall;
            private RuntimeMethodHandleInternal _handle;

            internal IntroducedMethodEnumerator(RuntimeType type)
            {
                _handle = GetFirstIntroducedMethod(type);
                _firstCall = true;
            }

            public bool MoveNext()
            {
                if (_firstCall)
                {
                    _firstCall = false;
                }
                else if (_handle.Value != IntPtr.Zero)
                {
                    GetNextIntroducedMethod(ref _handle);
                }
                return !(_handle.Value == IntPtr.Zero);
            }

            public RuntimeMethodHandleInternal Current => _handle;

            // Glue to make this work nicely with C# foreach statement
            public IntroducedMethodEnumerator GetEnumerator()
            {
                return this;
            }
        }

        internal static IntroducedMethodEnumerator GetIntroducedMethods(RuntimeType type)
        {
            return new IntroducedMethodEnumerator(type);
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern RuntimeMethodHandleInternal GetFirstIntroducedMethod(RuntimeType type);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern void GetNextIntroducedMethod(ref RuntimeMethodHandleInternal method);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "RuntimeTypeHandle_GetFields")]
        private static partial Interop.BOOL GetFields(MethodTable* pMT, Span<IntPtr> data, ref int usedCount);

        internal static bool GetFields(RuntimeType type, Span<IntPtr> buffer, out int count)
        {
            Debug.Assert(!IsGenericVariable(type));

            TypeHandle typeHandle = type.GetNativeTypeHandle();
            if (typeHandle.IsTypeDesc)
            {
                count = 0;
                return true;
            }

            int countLocal = buffer.Length;
            bool success = GetFields(typeHandle.AsMethodTable(), buffer, ref countLocal) != Interop.BOOL.FALSE;
            GC.KeepAlive(type);
            count = countLocal;
            return success;
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "RuntimeTypeHandle_GetInterfaces")]
        private static unsafe partial void GetInterfaces(MethodTable* pMT, ObjectHandleOnStack result);

        internal static Type[] GetInterfaces(RuntimeType type)
        {
            Debug.Assert(!IsGenericVariable(type));

            TypeHandle typeHandle = type.GetNativeTypeHandle();
            if (typeHandle.IsTypeDesc)
            {
                return [];
            }

            Type[] result = [];
            GetInterfaces(typeHandle.AsMethodTable(), ObjectHandleOnStack.Create(ref result));
            GC.KeepAlive(type);
            return result;
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "RuntimeTypeHandle_GetConstraints")]
        private static partial void GetConstraints(QCallTypeHandle handle, ObjectHandleOnStack types);

        internal Type[] GetConstraints()
        {
            Type[]? types = null;
            RuntimeTypeHandle nativeHandle = GetNativeHandle();

            GetConstraints(new QCallTypeHandle(ref nativeHandle), ObjectHandleOnStack.Create(ref types));

            return types!;
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "QCall_GetGCHandleForTypeHandle")]
        private static partial IntPtr GetGCHandle(QCallTypeHandle handle, GCHandleType type);

        internal IntPtr GetGCHandle(GCHandleType type)
        {
            RuntimeTypeHandle nativeHandle = GetNativeHandle();
            return GetGCHandle(new QCallTypeHandle(ref nativeHandle), type);
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "QCall_FreeGCHandleForTypeHandle")]
        private static partial IntPtr FreeGCHandle(QCallTypeHandle typeHandle, IntPtr objHandle);

        internal IntPtr FreeGCHandle(IntPtr objHandle)
        {
            RuntimeTypeHandle nativeHandle = GetNativeHandle();
            return FreeGCHandle(new QCallTypeHandle(ref nativeHandle), objHandle);
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern int GetNumVirtuals(RuntimeType type);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "RuntimeTypeHandle_GetNumVirtualsAndStaticVirtuals")]
        private static partial int GetNumVirtualsAndStaticVirtuals(QCallTypeHandle type);

        internal static int GetNumVirtualsAndStaticVirtuals(RuntimeType type)
        {
            Debug.Assert(type != null);
            return GetNumVirtualsAndStaticVirtuals(new QCallTypeHandle(ref type));
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "RuntimeTypeHandle_VerifyInterfaceIsImplemented")]
        private static partial void VerifyInterfaceIsImplemented(QCallTypeHandle handle, QCallTypeHandle interfaceHandle);

        internal void VerifyInterfaceIsImplemented(RuntimeTypeHandle interfaceHandle)
        {
            RuntimeTypeHandle nativeHandle = GetNativeHandle();
            RuntimeTypeHandle nativeInterfaceHandle = interfaceHandle.GetNativeHandle();
            VerifyInterfaceIsImplemented(new QCallTypeHandle(ref nativeHandle), new QCallTypeHandle(ref nativeInterfaceHandle));
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "RuntimeTypeHandle_GetInterfaceMethodImplementation")]
        private static partial RuntimeMethodHandleInternal GetInterfaceMethodImplementation(QCallTypeHandle handle, QCallTypeHandle interfaceHandle, RuntimeMethodHandleInternal interfaceMethodHandle);

        internal RuntimeMethodHandleInternal GetInterfaceMethodImplementation(RuntimeTypeHandle interfaceHandle, RuntimeMethodHandleInternal interfaceMethodHandle)
        {
            RuntimeTypeHandle nativeHandle = GetNativeHandle();
            RuntimeTypeHandle nativeInterfaceHandle = interfaceHandle.GetNativeHandle();
            return GetInterfaceMethodImplementation(new QCallTypeHandle(ref nativeHandle), new QCallTypeHandle(ref nativeInterfaceHandle), interfaceMethodHandle);
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "RuntimeTypeHandle_IsVisible")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool _IsVisible(QCallTypeHandle typeHandle);

        internal static bool IsVisible(RuntimeType type)
        {
            return _IsVisible(new QCallTypeHandle(ref type));
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "RuntimeTypeHandle_ConstructName")]
        private static partial void ConstructName(QCallTypeHandle handle, TypeNameFormatFlags formatFlags, StringHandleOnStack retString);

        internal string ConstructName(TypeNameFormatFlags formatFlags)
        {
            string? name = null;
            RuntimeTypeHandle nativeHandle = GetNativeHandle();
            ConstructName(new QCallTypeHandle(ref nativeHandle), formatFlags, new StringHandleOnStack(ref name));
            return name!;
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern unsafe void* GetUtf8NameInternal(MethodTable* pMT);

        // Since the returned string is a pointer into metadata, the caller should
        // ensure the passed in type is alive for at least as long as returned result is
        // needed.
        internal static MdUtf8String GetUtf8Name(RuntimeType type)
        {
            TypeHandle th = type.GetNativeTypeHandle();
            if (th.IsTypeDesc || th.AsMethodTable()->IsArray)
            {
                throw new ArgumentException(SR.Arg_InvalidHandle);
            }

            void* name = GetUtf8NameInternal(th.AsMethodTable());
            if (name is null)
            {
                throw new BadImageFormatException();
            }
            return new MdUtf8String(name);
        }

        internal static bool CanCastTo(RuntimeType type, RuntimeType target)
        {
            bool ret = TypeHandle.CanCastToForReflection(type.GetNativeTypeHandle(), target.GetNativeTypeHandle());
            GC.KeepAlive(type);
            GC.KeepAlive(target);
            return ret;
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "RuntimeTypeHandle_GetDeclaringTypeHandleForGenericVariable")]
        private static partial IntPtr GetDeclaringTypeHandleForGenericVariable(IntPtr typeHandle);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "RuntimeTypeHandle_GetDeclaringTypeHandle")]
        private static partial IntPtr GetDeclaringTypeHandle(IntPtr typeHandle);

        internal static RuntimeType? GetDeclaringType(RuntimeType type)
        {
            IntPtr retTypeHandle = IntPtr.Zero;
            TypeHandle typeHandle = type.GetNativeTypeHandle();
            if (typeHandle.IsTypeDesc)
            {
                CorElementType elementType = (CorElementType)typeHandle.GetCorElementType();
                if (elementType is CorElementType.ELEMENT_TYPE_VAR or CorElementType.ELEMENT_TYPE_MVAR)
                {
                    retTypeHandle = GetDeclaringTypeHandleForGenericVariable(type.GetUnderlyingNativeHandle());
                }
            }
            else
            {
                retTypeHandle = GetDeclaringTypeHandle(type.GetUnderlyingNativeHandle());
            }

            if (retTypeHandle == IntPtr.Zero)
            {
                return null;
            }

            RuntimeType result = GetRuntimeTypeFromHandle(retTypeHandle);
            GC.KeepAlive(type);
            return result;
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "RuntimeTypeHandle_GetDeclaringMethodForGenericParameter")]
        private static partial void GetDeclaringMethodForGenericParameter(QCallTypeHandle typeHandle, ObjectHandleOnStack result);

        internal static IRuntimeMethodInfo? GetDeclaringMethodForGenericParameter(RuntimeType type)
        {
            Debug.Assert(IsGenericVariable(type));

            IRuntimeMethodInfo? method = null;
            GetDeclaringMethodForGenericParameter(new QCallTypeHandle(ref type), ObjectHandleOnStack.Create(ref method));
            return method;
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "RuntimeTypeHandle_GetInstantiation")]
        internal static partial void GetInstantiation(QCallTypeHandle type, ObjectHandleOnStack types, Interop.BOOL fAsRuntimeTypeArray);

        internal RuntimeType[] GetInstantiationInternal()
        {
            RuntimeType[]? types = null;
            RuntimeTypeHandle nativeHandle = GetNativeHandle();
            GetInstantiation(new QCallTypeHandle(ref nativeHandle), ObjectHandleOnStack.Create(ref types), Interop.BOOL.TRUE);
            return types!;
        }

        internal Type[] GetInstantiationPublic()
        {
            Type[]? types = null;
            RuntimeTypeHandle nativeHandle = GetNativeHandle();
            GetInstantiation(new QCallTypeHandle(ref nativeHandle), ObjectHandleOnStack.Create(ref types), Interop.BOOL.FALSE);
            return types!;
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "RuntimeTypeHandle_Instantiate")]
        private static partial void Instantiate(QCallTypeHandle handle, IntPtr* pInst, int numGenericArgs, ObjectHandleOnStack type);

        internal RuntimeType Instantiate(RuntimeType inst)
        {
            IntPtr ptr = inst.TypeHandle.Value;

            RuntimeType? type = null;
            RuntimeTypeHandle nativeHandle = GetNativeHandle();
            Instantiate(new QCallTypeHandle(ref nativeHandle), &ptr, 1, ObjectHandleOnStack.Create(ref type));
            GC.KeepAlive(inst);
            return type!;
        }

        internal RuntimeType Instantiate(Type[]? inst)
        {
            IntPtr[]? instHandles = CopyRuntimeTypeHandles(inst, out int instCount);

            fixed (IntPtr* pInst = instHandles)
            {
                RuntimeType? type = null;
                RuntimeTypeHandle nativeHandle = GetNativeHandle();
                Instantiate(new QCallTypeHandle(ref nativeHandle), pInst, instCount, ObjectHandleOnStack.Create(ref type));
                GC.KeepAlive(inst);
                return type!;
            }
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "RuntimeTypeHandle_MakeArray")]
        private static partial void MakeArray(QCallTypeHandle handle, int rank, ObjectHandleOnStack type);

        internal RuntimeType MakeArray(int rank)
        {
            RuntimeType? type = null;
            RuntimeTypeHandle nativeHandle = GetNativeHandle();
            MakeArray(new QCallTypeHandle(ref nativeHandle), rank, ObjectHandleOnStack.Create(ref type));
            return type!;
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "RuntimeTypeHandle_MakeSZArray")]
        private static partial void MakeSZArray(QCallTypeHandle handle, ObjectHandleOnStack type);

        internal RuntimeType MakeSZArray()
        {
            RuntimeType? type = null;
            RuntimeTypeHandle nativeHandle = GetNativeHandle();
            MakeSZArray(new QCallTypeHandle(ref nativeHandle), ObjectHandleOnStack.Create(ref type));
            return type!;
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "RuntimeTypeHandle_MakeByRef")]
        private static partial void MakeByRef(QCallTypeHandle handle, ObjectHandleOnStack type);

        internal RuntimeType MakeByRef()
        {
            RuntimeType? type = null;
            RuntimeTypeHandle nativeHandle = GetNativeHandle();
            MakeByRef(new QCallTypeHandle(ref nativeHandle), ObjectHandleOnStack.Create(ref type));
            return type!;
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "RuntimeTypeHandle_MakePointer")]
        private static partial void MakePointer(QCallTypeHandle handle, ObjectHandleOnStack type);

        internal RuntimeType MakePointer()
        {
            RuntimeType? type = null;
            RuntimeTypeHandle nativeHandle = GetNativeHandle();
            MakePointer(new QCallTypeHandle(ref nativeHandle), ObjectHandleOnStack.Create(ref type));
            return type!;
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "RuntimeTypeHandle_IsCollectible")]
        internal static partial Interop.BOOL IsCollectible(QCallTypeHandle handle);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "RuntimeTypeHandle_GetGenericTypeDefinition")]
        internal static partial void GetGenericTypeDefinition(QCallTypeHandle type, ObjectHandleOnStack retType);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern bool IsGenericVariable(RuntimeType type);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern int GetGenericVariableIndex(RuntimeType type);

        internal int GetGenericVariableIndex()
        {
            RuntimeType type = GetRuntimeTypeChecked();

            if (!IsGenericVariable(type))
                throw new InvalidOperationException(SR.Arg_NotGenericParameter);

            return GetGenericVariableIndex(type);
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern bool ContainsGenericVariables(RuntimeType handle);

        internal bool ContainsGenericVariables()
        {
            return ContainsGenericVariables(GetRuntimeTypeChecked());
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "RuntimeTypeHandle_SatisfiesConstraints")]
        private static partial Interop.BOOL SatisfiesConstraints(QCallTypeHandle paramType, QCallTypeHandle pTypeContext, RuntimeMethodHandleInternal pMethodContext, QCallTypeHandle toType);

        internal static bool SatisfiesConstraints(RuntimeType paramType, RuntimeType? typeContext, RuntimeMethodInfo? methodContext, RuntimeType toType)
        {
            RuntimeMethodHandleInternal methodContextRaw = ((IRuntimeMethodInfo?)methodContext)?.Value ?? RuntimeMethodHandleInternal.EmptyHandle;
            bool result = SatisfiesConstraints(new QCallTypeHandle(ref paramType), new QCallTypeHandle(ref typeContext!), methodContextRaw, new QCallTypeHandle(ref toType)) != Interop.BOOL.FALSE;
            GC.KeepAlive(methodContext);
            return result;
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "RuntimeTypeHandle_RegisterCollectibleTypeDependency")]
        private static partial void RegisterCollectibleTypeDependency(QCallTypeHandle type, QCallAssembly assembly);

        internal static void RegisterCollectibleTypeDependency(RuntimeType type, RuntimeAssembly? assembly)
        {
            RegisterCollectibleTypeDependency(new QCallTypeHandle(ref type), new QCallAssembly(ref assembly!));
        }

        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            throw new PlatformNotSupportedException();
        }

#if FEATURE_TYPEEQUIVALENCE
        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "RuntimeTypeHandle_IsEquivalentTo")]
        private static partial Interop.BOOL IsEquivalentTo(QCallTypeHandle rtType1, QCallTypeHandle rtType2);

        internal static bool IsEquivalentTo(RuntimeType rtType1, RuntimeType rtType2)
            => IsEquivalentTo(new QCallTypeHandle(ref rtType1), new QCallTypeHandle(ref rtType2)) == Interop.BOOL.TRUE;
#endif // FEATURE_TYPEEQUIVALENCE
    }

    // This type is used to remove the expense of having a managed reference object that is dynamically
    // created when we can prove that we don't need that object. Use of this type requires code to ensure
    // that the underlying native resource is not freed.
    // Cases in which this may be used:
    //  1. When native code calls managed code passing one of these as a parameter
    //  2. When managed code acquires one of these from an IRuntimeMethodInfo, and ensure that the IRuntimeMethodInfo is preserved
    //     across the lifetime of the RuntimeMethodHandleInternal instance
    //  3. When another object is used to keep the RuntimeMethodHandleInternal alive. See delegates, CreateInstance cache, Signature structure
    // When in doubt, do not use.
    internal struct RuntimeMethodHandleInternal
    {
        internal static RuntimeMethodHandleInternal EmptyHandle => default;

        internal bool IsNullHandle()
        {
            return m_handle == IntPtr.Zero;
        }

        internal IntPtr Value => m_handle;

        internal RuntimeMethodHandleInternal(IntPtr value)
        {
            m_handle = value;
        }

        private IntPtr m_handle;
    }

    internal sealed class RuntimeMethodInfoStub : IRuntimeMethodInfo
    {
        public RuntimeMethodInfoStub(RuntimeMethodHandleInternal methodHandleValue, object keepalive)
        {
            m_keepalive = keepalive;
            m_value = methodHandleValue;
        }

        private readonly object m_keepalive;

        // These unused variables are used to ensure that this class has the same layout as RuntimeMethodInfo
#pragma warning disable CA1823, 414, 169, IDE0044
        private object? m_a;
        private object? m_b;
        private object? m_c;
        private object? m_d;
        private object? m_e;
        private object? m_f;
        private object? m_g;
        private object? m_h;
#pragma warning restore CA1823, 414, 169, IDE0044

        public RuntimeMethodHandleInternal m_value;

        RuntimeMethodHandleInternal IRuntimeMethodInfo.Value => m_value;

        // implementation of CORINFO_HELP_METHODDESC_TO_STUBRUNTIMEMETHOD
        [StackTraceHidden]
        [DebuggerStepThrough]
        [DebuggerHidden]
        internal static object FromPtr(IntPtr pMD)
        {
            RuntimeMethodHandleInternal handle = new(pMD);
            return new RuntimeMethodInfoStub(handle, RuntimeMethodHandle.GetLoaderAllocator(handle));
        }
    }

    internal interface IRuntimeMethodInfo
    {
        RuntimeMethodHandleInternal Value
        {
            get;
        }
    }

    [NonVersionable]
    public unsafe partial struct RuntimeMethodHandle : IEquatable<RuntimeMethodHandle>, ISerializable
    {
        // Returns handle for interop with EE. The handle is guaranteed to be non-null.
        internal static IRuntimeMethodInfo EnsureNonNullMethodInfo(IRuntimeMethodInfo method)
        {
            if (method == null)
                throw new ArgumentNullException(null, SR.Arg_InvalidHandle);
            return method;
        }

        private readonly IRuntimeMethodInfo m_value;

        internal RuntimeMethodHandle(IRuntimeMethodInfo method)
        {
            m_value = method;
        }

        internal IRuntimeMethodInfo GetMethodInfo()
        {
            return m_value;
        }

        // ISerializable interface
        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            throw new PlatformNotSupportedException();
        }

        public IntPtr Value => m_value != null ? m_value.Value.Value : IntPtr.Zero;

        public override int GetHashCode()
        {
            return HashCode.Combine(Value);
        }

        public override bool Equals(object? obj)
        {
            if (obj is not RuntimeMethodHandle)
                return false;

            RuntimeMethodHandle handle = (RuntimeMethodHandle)obj;

            return handle.Value == Value;
        }

        /// <summary>
        /// Returns a new <see cref="RuntimeMethodHandle"/> object created from a handle to a RuntimeMethodInfo.
        /// </summary>
        /// <param name="value">An IntPtr handle to a RuntimeMethodInfo to create a <see cref="RuntimeMethodHandle"/> object from.</param>
        /// <returns>A new <see cref="RuntimeMethodHandle"/> object that corresponds to the value parameter.</returns>
        public static RuntimeMethodHandle FromIntPtr(IntPtr value)
        {
            var handle = new RuntimeMethodHandleInternal(value);
            var methodInfo = new RuntimeMethodInfoStub(handle, GetLoaderAllocator(handle));
            return new RuntimeMethodHandle(methodInfo);
        }

        /// <summary>
        /// Returns the internal pointer representation of a <see cref="RuntimeMethodHandle"/> object.
        /// </summary>
        /// <param name="value">A <see cref="RuntimeMethodHandle"/> object to retrieve an internal pointer representation from.</param>
        /// <returns>An <see cref="IntPtr"/> object that represents a <see cref="RuntimeMethodHandle"/> object.</returns>
        public static IntPtr ToIntPtr(RuntimeMethodHandle value) => value.Value;

        public static bool operator ==(RuntimeMethodHandle left, RuntimeMethodHandle right) => left.Equals(right);

        public static bool operator !=(RuntimeMethodHandle left, RuntimeMethodHandle right) => !left.Equals(right);

        public bool Equals(RuntimeMethodHandle handle)
        {
            return handle.Value == Value;
        }

        internal bool IsNullHandle()
        {
            return m_value == null;
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "RuntimeMethodHandle_GetFunctionPointer")]
        internal static partial IntPtr GetFunctionPointer(RuntimeMethodHandleInternal handle);

        public IntPtr GetFunctionPointer()
        {
            IntPtr ptr = GetFunctionPointer(EnsureNonNullMethodInfo(m_value!).Value);
            GC.KeepAlive(m_value);
            return ptr;
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "RuntimeMethodHandle_GetIsCollectible")]
        internal static partial Interop.BOOL GetIsCollectible(RuntimeMethodHandleInternal handle);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "RuntimeMethodHandle_IsCAVisibleFromDecoratedType")]
        internal static partial Interop.BOOL IsCAVisibleFromDecoratedType(
            QCallTypeHandle attrTypeHandle,
            RuntimeMethodHandleInternal attrCtor,
            QCallTypeHandle sourceTypeHandle,
            QCallModule sourceModule);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern MethodAttributes GetAttributes(RuntimeMethodHandleInternal method);

        internal static MethodAttributes GetAttributes(IRuntimeMethodInfo method)
        {
            MethodAttributes retVal = GetAttributes(method.Value);
            GC.KeepAlive(method);
            return retVal;
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern MethodImplAttributes GetImplAttributes(IRuntimeMethodInfo method);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "RuntimeMethodHandle_ConstructInstantiation")]
        private static partial void ConstructInstantiation(RuntimeMethodHandleInternal method, TypeNameFormatFlags format, StringHandleOnStack retString);

        internal static string ConstructInstantiation(IRuntimeMethodInfo method, TypeNameFormatFlags format)
        {
            string? name = null;
            IRuntimeMethodInfo methodInfo = EnsureNonNullMethodInfo(method);
            ConstructInstantiation(methodInfo.Value, format, new StringHandleOnStack(ref name));
            GC.KeepAlive(methodInfo);
            return name!;
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern unsafe MethodTable* GetMethodTable(RuntimeMethodHandleInternal method);

        internal static unsafe RuntimeType GetDeclaringType(RuntimeMethodHandleInternal method)
        {
            Debug.Assert(!method.IsNullHandle());
            MethodTable* pMT = GetMethodTable(method);
            return RuntimeTypeHandle.GetRuntimeType(pMT);
        }

        internal static RuntimeType GetDeclaringType(IRuntimeMethodInfo method)
        {
            RuntimeType type = GetDeclaringType(method.Value);
            GC.KeepAlive(method);
            return type;
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern int GetSlot(RuntimeMethodHandleInternal method);

        internal static int GetSlot(IRuntimeMethodInfo method)
        {
            Debug.Assert(method != null);

            int slot = GetSlot(method.Value);
            GC.KeepAlive(method);
            return slot;
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern int GetMethodDef(RuntimeMethodHandleInternal method);

        internal static int GetMethodDef(IRuntimeMethodInfo method)
        {
            Debug.Assert(method != null);

            int token = GetMethodDef(method.Value);
            GC.KeepAlive(method);
            return token;
        }

        internal static string GetName(RuntimeMethodHandleInternal method)
            => GetUtf8Name(method).ToString();

        internal static string GetName(IRuntimeMethodInfo method)
        {
            string name = GetName(method.Value);
            GC.KeepAlive(method);
            return name;
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern void* GetUtf8NameInternal(RuntimeMethodHandleInternal method);

        // Since the returned string is a pointer into metadata, the caller should
        // ensure the passed in type is alive for at least as long as returned result is
        // needed.
        internal static MdUtf8String GetUtf8Name(RuntimeMethodHandleInternal method)
        {
            void* name = GetUtf8NameInternal(method);
            if (name is null)
            {
                throw new BadImageFormatException();
            }
            return new MdUtf8String(name);
        }

        [DebuggerStepThrough]
        [DebuggerHidden]
        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "RuntimeMethodHandle_InvokeMethod")]
        private static partial void InvokeMethod(ObjectHandleOnStack target, void** arguments, ObjectHandleOnStack sig, Interop.BOOL isConstructor, ObjectHandleOnStack result);

        [DebuggerStepThrough]
        [DebuggerHidden]
        internal static object? InvokeMethod(object? target, void** arguments, Signature sig, bool isConstructor)
        {
            object? result = null;
            InvokeMethod(
                ObjectHandleOnStack.Create(ref target),
                arguments,
                ObjectHandleOnStack.Create(ref sig),
                isConstructor ? Interop.BOOL.TRUE : Interop.BOOL.FALSE,
                ObjectHandleOnStack.Create(ref result));
            return result;
        }

        /// <summary>
        /// For a true boxed Nullable{T}, re-box to a boxed {T} or null, otherwise just return the input.
        /// </summary>
        internal static object? ReboxFromNullable(object? src)
        {
            // If src is null or not NullableOfT, just return that state.
            if (src is null)
            {
                return null;
            }

            MethodTable* pMT = RuntimeHelpers.GetMethodTable(src);
            if (!pMT->IsNullable)
            {
                return src;
            }

            return CastHelpers.ReboxFromNullable(pMT, src);
        }

        /// <summary>
        /// Convert a boxed value of {T} (which is either {T} or null) to a true boxed Nullable{T}.
        /// </summary>
        internal static object ReboxToNullable(object? src, RuntimeType destNullableType)
        {
            Debug.Assert(destNullableType.IsNullableOfT);
            MethodTable* pMT = destNullableType.GetNativeTypeHandle().AsMethodTable();
            object obj = RuntimeTypeHandle.InternalAlloc(pMT);
            GC.KeepAlive(destNullableType); // The obj instance will keep the type alive.

            CastHelpers.Unbox_Nullable(
                ref obj.GetRawData(),
                pMT,
                src);
            return obj;
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "RuntimeMethodHandle_GetMethodInstantiation")]
        private static partial void GetMethodInstantiation(RuntimeMethodHandleInternal method, ObjectHandleOnStack types, Interop.BOOL fAsRuntimeTypeArray);

        internal static RuntimeType[] GetMethodInstantiationInternal(IRuntimeMethodInfo method)
        {
            RuntimeType[]? types = null;
            GetMethodInstantiation(EnsureNonNullMethodInfo(method).Value, ObjectHandleOnStack.Create(ref types), Interop.BOOL.TRUE);
            GC.KeepAlive(method);
            return types!;
        }

        internal static RuntimeType[] GetMethodInstantiationInternal(RuntimeMethodHandleInternal method)
        {
            RuntimeType[]? types = null;
            GetMethodInstantiation(method, ObjectHandleOnStack.Create(ref types), Interop.BOOL.TRUE);
            return types!;
        }

        internal static Type[] GetMethodInstantiationPublic(IRuntimeMethodInfo method)
        {
            RuntimeType[]? types = null;
            GetMethodInstantiation(EnsureNonNullMethodInfo(method).Value, ObjectHandleOnStack.Create(ref types), Interop.BOOL.FALSE);
            GC.KeepAlive(method);
            return types!;
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern bool HasMethodInstantiation(RuntimeMethodHandleInternal method);

        internal static bool HasMethodInstantiation(IRuntimeMethodInfo method)
        {
            bool fRet = HasMethodInstantiation(method.Value);
            GC.KeepAlive(method);
            return fRet;
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern RuntimeMethodHandleInternal GetStubIfNeededInternal(RuntimeMethodHandleInternal method, RuntimeType declaringType);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "RuntimeMethodHandle_GetStubIfNeededSlow")]
        private static partial RuntimeMethodHandleInternal GetStubIfNeededSlow(RuntimeMethodHandleInternal method, QCallTypeHandle declaringTypeHandle, ObjectHandleOnStack methodInstantiation);

        internal static RuntimeMethodHandleInternal GetStubIfNeeded(RuntimeMethodHandleInternal method, RuntimeType declaringType, RuntimeType[]? methodInstantiation)
        {
            if (methodInstantiation is null)
            {
                RuntimeMethodHandleInternal handle = GetStubIfNeededInternal(method, declaringType);
                if (!handle.IsNullHandle())
                    return handle;
            }

            return GetStubIfNeededWorker(method, declaringType, methodInstantiation);

            [MethodImpl(MethodImplOptions.NoInlining)]
            static RuntimeMethodHandleInternal GetStubIfNeededWorker(RuntimeMethodHandleInternal method, RuntimeType declaringType, RuntimeType[]? methodInstantiation)
                => GetStubIfNeededSlow(method, new QCallTypeHandle(ref declaringType), ObjectHandleOnStack.Create(ref methodInstantiation));
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern RuntimeMethodHandleInternal GetMethodFromCanonical(RuntimeMethodHandleInternal method, RuntimeType declaringType);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern bool IsGenericMethodDefinition(RuntimeMethodHandleInternal method);

        internal static bool IsGenericMethodDefinition(IRuntimeMethodInfo method)
        {
            bool fRet = IsGenericMethodDefinition(method.Value);
            GC.KeepAlive(method);
            return fRet;
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern bool IsTypicalMethodDefinition(IRuntimeMethodInfo method);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "RuntimeMethodHandle_GetTypicalMethodDefinition")]
        private static partial void GetTypicalMethodDefinition(RuntimeMethodHandleInternal method, ObjectHandleOnStack outMethod);

        internal static IRuntimeMethodInfo GetTypicalMethodDefinition(IRuntimeMethodInfo method)
        {
            if (!IsTypicalMethodDefinition(method))
            {
                GetTypicalMethodDefinition(method.Value, ObjectHandleOnStack.Create(ref method));
                GC.KeepAlive(method);
            }

            return method;
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern int GetGenericParameterCount(RuntimeMethodHandleInternal method);

        internal static int GetGenericParameterCount(IRuntimeMethodInfo method) => GetGenericParameterCount(method.Value);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "RuntimeMethodHandle_StripMethodInstantiation")]
        private static partial void StripMethodInstantiation(RuntimeMethodHandleInternal method, ObjectHandleOnStack outMethod);

        internal static IRuntimeMethodInfo StripMethodInstantiation(IRuntimeMethodInfo method)
        {
            IRuntimeMethodInfo strippedMethod = method;

            StripMethodInstantiation(method.Value, ObjectHandleOnStack.Create(ref strippedMethod));
            GC.KeepAlive(method);

            return strippedMethod;
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern bool IsDynamicMethod(RuntimeMethodHandleInternal method);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "RuntimeMethodHandle_Destroy")]
        internal static partial void Destroy(RuntimeMethodHandleInternal method);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern Resolver GetResolver(RuntimeMethodHandleInternal method);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "RuntimeMethodHandle_GetMethodBody")]
        private static partial void GetMethodBody(RuntimeMethodHandleInternal method, QCallTypeHandle declaringType, ObjectHandleOnStack result);

        internal static RuntimeMethodBody? GetMethodBody(IRuntimeMethodInfo method, RuntimeType declaringType)
        {
            RuntimeMethodBody? result = null;
            GetMethodBody(method.Value, new QCallTypeHandle(ref declaringType), ObjectHandleOnStack.Create(ref result));
            GC.KeepAlive(method);
            return result;
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern bool IsConstructor(RuntimeMethodHandleInternal method);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern LoaderAllocator GetLoaderAllocatorInternal(RuntimeMethodHandleInternal method);

        internal static LoaderAllocator GetLoaderAllocator(RuntimeMethodHandleInternal method)
        {
            if (method.IsNullHandle())
            {
                throw new ArgumentNullException(SR.Arg_InvalidHandle);
            }

            return GetLoaderAllocatorInternal(method);
        }
    }

    // This type is used to remove the expense of having a managed reference object that is dynamically
    // created when we can prove that we don't need that object. Use of this type requires code to ensure
    // that the underlying native resource is not freed.
    // Cases in which this may be used:
    //  1. When native code calls managed code passing one of these as a parameter
    //  2. When managed code acquires one of these from an RtFieldInfo, and ensure that the RtFieldInfo is preserved
    //     across the lifetime of the RuntimeFieldHandleInternal instance
    //  3. When another object is used to keep the RuntimeFieldHandleInternal alive.
    // When in doubt, do not use.
    internal struct RuntimeFieldHandleInternal
    {
        internal bool IsNullHandle()
        {
            return m_handle == IntPtr.Zero;
        }

        internal IntPtr Value => m_handle;

        internal RuntimeFieldHandleInternal(IntPtr value)
        {
            m_handle = value;
        }

        internal IntPtr m_handle;
    }

    internal interface IRuntimeFieldInfo
    {
        RuntimeFieldHandleInternal Value
        {
            get;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal sealed class RuntimeFieldInfoStub : IRuntimeFieldInfo
    {
        public RuntimeFieldInfoStub(RuntimeFieldHandleInternal fieldHandle, object keepalive)
        {
            m_keepalive = keepalive;
            m_fieldHandle = fieldHandle;
        }

        private readonly object m_keepalive;

        // These unused variables are used to ensure that this class has the same layout as RuntimeFieldInfo
#pragma warning disable 414, 169, IDE0044
        private object? m_c;
        private object? m_d;
        private int m_b;
        private object? m_e;
        private object? m_f;
        private RuntimeFieldHandleInternal m_fieldHandle;
#pragma warning restore 414, 169, IDE0044

        RuntimeFieldHandleInternal IRuntimeFieldInfo.Value => m_fieldHandle;

        // implementation of CORINFO_HELP_FIELDDESC_TO_STUBRUNTIMEFIELD
        [StackTraceHidden]
        [DebuggerStepThrough]
        [DebuggerHidden]
        internal static object FromPtr(IntPtr pFD)
        {
            RuntimeFieldHandleInternal handle = new(pFD);
            return new RuntimeFieldInfoStub(handle, RuntimeFieldHandle.GetLoaderAllocator(handle));
        }
    }

    [NonVersionable]
    public unsafe partial struct RuntimeFieldHandle : IEquatable<RuntimeFieldHandle>, ISerializable
    {
        // Returns handle for interop with EE. The handle is guaranteed to be non-null.
        internal RuntimeFieldHandle GetNativeHandle() =>
            new RuntimeFieldHandle(m_ptr ?? throw new ArgumentNullException(null, SR.Arg_InvalidHandle));

        private readonly IRuntimeFieldInfo m_ptr;

        internal RuntimeFieldHandle(IRuntimeFieldInfo fieldInfo)
        {
            m_ptr = fieldInfo;
        }

        internal IRuntimeFieldInfo GetRuntimeFieldInfo()
        {
            return m_ptr;
        }

        public IntPtr Value => m_ptr != null ? m_ptr.Value.Value : IntPtr.Zero;

        internal bool IsNullHandle()
        {
            return m_ptr == null;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Value);
        }

        public override bool Equals(object? obj)
        {
            if (obj is not RuntimeFieldHandle)
                return false;

            RuntimeFieldHandle handle = (RuntimeFieldHandle)obj;

            return handle.Value == Value;
        }

        public bool Equals(RuntimeFieldHandle handle)
        {
            return handle.Value == Value;
        }

        /// <summary>
        /// Returns a new <see cref="RuntimeFieldHandle"/> object created from a handle to a RuntimeFieldInfo.
        /// </summary>
        /// <param name="value">An IntPtr handle to a RuntimeFieldInfo to create a <see cref="RuntimeFieldHandle"/> object from.</param>
        /// <returns>A new <see cref="RuntimeFieldHandle"/> object that corresponds to the value parameter.</returns>
        public static RuntimeFieldHandle FromIntPtr(IntPtr value)
        {
            var handle = new RuntimeFieldHandleInternal(value);
            var fieldInfo = new RuntimeFieldInfoStub(handle, GetLoaderAllocator(handle));
            return new RuntimeFieldHandle(fieldInfo);
        }

        /// <summary>
        /// Returns the internal pointer representation of a <see cref="RuntimeFieldHandle"/> object.
        /// </summary>
        /// <param name="value">A <see cref="RuntimeFieldHandle"/> object to retrieve an internal pointer representation from.</param>
        /// <returns>An <see cref="IntPtr"/> object that represents a <see cref="RuntimeFieldHandle"/> object.</returns>
        public static IntPtr ToIntPtr(RuntimeFieldHandle value) => value.Value;

        public static bool operator ==(RuntimeFieldHandle left, RuntimeFieldHandle right) => left.Equals(right);

        public static bool operator !=(RuntimeFieldHandle left, RuntimeFieldHandle right) => !left.Equals(right);

        internal static string GetName(IRuntimeFieldInfo field)
        {
            string name = GetUtf8Name(field.Value).ToString();
            GC.KeepAlive(field);
            return name;
       }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern void* GetUtf8NameInternal(RuntimeFieldHandleInternal field);

        // Since the returned string is a pointer into metadata, the caller should
        // ensure the passed in type is alive for at least as long as returned result is
        // needed.
        internal static MdUtf8String GetUtf8Name(RuntimeFieldHandleInternal field)
        {
            void* name = GetUtf8NameInternal(field);
            if (name is null)
            {
                throw new BadImageFormatException();
            }
            return new MdUtf8String(name);
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern FieldAttributes GetAttributes(RuntimeFieldHandleInternal field);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern MethodTable* GetApproxDeclaringMethodTable(RuntimeFieldHandleInternal field);

        internal static RuntimeType GetApproxDeclaringType(RuntimeFieldHandleInternal field)
        {
            Debug.Assert(!field.IsNullHandle());
            MethodTable* pMT = GetApproxDeclaringMethodTable(field);
            Debug.Assert(pMT != null);

            return RuntimeTypeHandle.GetRuntimeType(pMT);
        }

        internal static RuntimeType GetApproxDeclaringType(IRuntimeFieldInfo field)
        {
            RuntimeType type = GetApproxDeclaringType(field.Value);
            GC.KeepAlive(field);
            return type;
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern bool IsFastPathSupported(RtFieldInfo field);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern int GetInstanceFieldOffset(RtFieldInfo field);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern IntPtr GetStaticFieldAddress(RtFieldInfo field);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "RuntimeFieldHandle_GetRVAFieldInfo")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool GetRVAFieldInfo(RuntimeFieldHandleInternal field, out void* address, out uint size);

        internal static ref byte GetFieldDataReference(object target, RuntimeFieldInfo field)
        {
            ByteRef fieldDataRef = default;
            GetFieldDataReference(((RtFieldInfo)field).GetFieldDesc(), ObjectHandleOnStack.Create(ref target), ByteRefOnStack.Create(ref fieldDataRef));
            Debug.Assert(!Unsafe.IsNullRef(ref fieldDataRef.Get()));
            GC.KeepAlive(field);
            return ref fieldDataRef.Get();
        }

        internal static ref byte GetFieldDataReference(ref byte target, RuntimeFieldInfo field)
        {
            Debug.Assert(!Unsafe.IsNullRef(ref target));
            int offset = GetInstanceFieldOffset((RtFieldInfo)field);
            return ref Unsafe.AddByteOffset(ref target, offset);
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "RuntimeFieldHandle_GetFieldDataReference")]
        private static unsafe partial void GetFieldDataReference(IntPtr fieldDesc, ObjectHandleOnStack target, ByteRefOnStack fieldDataRef);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern int GetToken(IntPtr fieldDesc);

        internal static int GetToken(RtFieldInfo field)
        {
            int tk = GetToken(field.GetFieldDesc());
            GC.KeepAlive(field);
            return tk;
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "RuntimeFieldHandle_GetValue")]
        private static partial void GetValue(
            IntPtr fieldDesc,
            ObjectHandleOnStack instance,
            QCallTypeHandle fieldType,
            QCallTypeHandle declaringType,
            [MarshalAs(UnmanagedType.Bool)] ref bool isClassInitialized,
            ObjectHandleOnStack result);

        internal static object? GetValue(RtFieldInfo field, object? instance, RuntimeType fieldType, RuntimeType? declaringType, ref bool isClassInitialized)
        {
            if (field is null || fieldType is null)
            {
                throw new ArgumentNullException(SR.Arg_InvalidHandle);
            }

            object? result = null;
            GetValue(field.GetFieldDesc(), ObjectHandleOnStack.Create(ref instance), new QCallTypeHandle(ref fieldType), new QCallTypeHandle(ref declaringType!), ref isClassInitialized, ObjectHandleOnStack.Create(ref result));
            GC.KeepAlive(field);
            return result;
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "RuntimeFieldHandle_GetValueDirect")]
        private static partial void GetValueDirect(
            IntPtr fieldDesc,
            void* pTypedRef,
            QCallTypeHandle fieldType,
            QCallTypeHandle declaringType,
            ObjectHandleOnStack result);

        internal static object? GetValueDirect(RtFieldInfo field, RuntimeType fieldType, TypedReference typedRef, RuntimeType? contextType)
        {
            if (field is null || fieldType is null)
            {
                throw new ArgumentNullException(SR.Arg_InvalidHandle);
            }

            object? result = null;
            GetValueDirect(field.GetFieldDesc(), &typedRef, new QCallTypeHandle(ref fieldType), new QCallTypeHandle(ref contextType!), ObjectHandleOnStack.Create(ref result));
            GC.KeepAlive(field);
            return result;
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "RuntimeFieldHandle_SetValue")]
        private static partial void SetValue(
            IntPtr fieldDesc,
            ObjectHandleOnStack instance,
            ObjectHandleOnStack value,
            QCallTypeHandle fieldType,
            QCallTypeHandle declaringType,
            [MarshalAs(UnmanagedType.Bool)] ref bool isClassInitialized);

        internal static void SetValue(RtFieldInfo field, object? obj, object? value, RuntimeType fieldType, RuntimeType? declaringType, ref bool isClassInitialized)
        {
            if (field is null || fieldType is null)
            {
                throw new ArgumentNullException(SR.Arg_InvalidHandle);
            }

            SetValue(field.GetFieldDesc(), ObjectHandleOnStack.Create(ref obj), ObjectHandleOnStack.Create(ref value), new QCallTypeHandle(ref fieldType), new QCallTypeHandle(ref declaringType!), ref isClassInitialized);
            GC.KeepAlive(field);
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "RuntimeFieldHandle_SetValueDirect")]
        private static partial void SetValueDirect(
            IntPtr fieldDesc,
            void* pTypedRef,
            ObjectHandleOnStack value,
            QCallTypeHandle fieldType,
            QCallTypeHandle declaringType);

        internal static void SetValueDirect(RtFieldInfo field, RuntimeType fieldType, TypedReference typedRef, object? value, RuntimeType? contextType)
        {
            if (field is null || fieldType is null)
            {
                throw new ArgumentNullException(SR.Arg_InvalidHandle);
            }

            SetValueDirect(field.GetFieldDesc(), &typedRef, ObjectHandleOnStack.Create(ref value), new QCallTypeHandle(ref fieldType), new QCallTypeHandle(ref contextType!));
            GC.KeepAlive(field);
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern unsafe RuntimeFieldHandleInternal GetStaticFieldForGenericType(RuntimeFieldHandleInternal field, MethodTable* pMT);

        internal static RuntimeFieldHandleInternal GetStaticFieldForGenericType(RuntimeFieldHandleInternal field, RuntimeType declaringType)
        {
            TypeHandle th = declaringType.GetNativeTypeHandle();
            Debug.Assert(!th.IsTypeDesc);
            return GetStaticFieldForGenericType(field, th.AsMethodTable());
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern bool AcquiresContextFromThis(RuntimeFieldHandleInternal field);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern LoaderAllocator GetLoaderAllocatorInternal(RuntimeFieldHandleInternal field);

        internal static LoaderAllocator GetLoaderAllocator(RuntimeFieldHandleInternal field)
        {
            if (field.IsNullHandle())
            {
                throw new ArgumentNullException(SR.Arg_InvalidHandle);
            }

            return GetLoaderAllocatorInternal(field);
        }

        // ISerializable interface
        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            throw new PlatformNotSupportedException();
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "RuntimeFieldHandle_GetEnCFieldAddr")]
        private static partial void* GetEnCFieldAddr(ObjectHandleOnStack tgt, void* pFD);

        // implementation of CORINFO_HELP_GETFIELDADDR
        [StackTraceHidden]
        [DebuggerStepThrough]
        [DebuggerHidden]
        internal static unsafe void* GetFieldAddr(object tgt, void* pFD)
        {
            void* addr = GetEnCFieldAddr(ObjectHandleOnStack.Create(ref tgt), pFD);
            if (addr == null)
                throw new NullReferenceException();
            return addr;
        }

        // implementation of CORINFO_HELP_GETSTATICFIELDADDR
        [StackTraceHidden]
        [DebuggerStepThrough]
        [DebuggerHidden]
        internal static unsafe void* GetStaticFieldAddr(void* pFD)
        {
            object? nullTarget = null;
            void* addr = GetEnCFieldAddr(ObjectHandleOnStack.Create(ref nullTarget), pFD);
            if (addr == null)
                throw new NullReferenceException();
            return addr;
        }
    }

    public unsafe partial struct ModuleHandle : IEquatable<ModuleHandle>
    {
        #region Public Static Members
        public static readonly ModuleHandle EmptyHandle;
        #endregion

        #region Private Data Members
        private readonly RuntimeModule m_ptr;
        #endregion

        #region Constructor
        internal ModuleHandle(RuntimeModule module)
        {
            m_ptr = module;
        }
        #endregion

        internal RuntimeModule GetRuntimeModule()
        {
            return m_ptr;
        }

        public override int GetHashCode()
        {
            return m_ptr != null ? m_ptr.GetHashCode() : 0;
        }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj is not ModuleHandle)
                return false;

            ModuleHandle handle = (ModuleHandle)obj;

            return handle.m_ptr == m_ptr;
        }

        public bool Equals(ModuleHandle handle)
        {
            return handle.m_ptr == m_ptr;
        }

        public static bool operator ==(ModuleHandle left, ModuleHandle right) => left.Equals(right);

        public static bool operator !=(ModuleHandle left, ModuleHandle right) => !left.Equals(right);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "ModuleHandle_GetDynamicMethod", StringMarshalling = StringMarshalling.Utf8)]
        private static partial void GetDynamicMethod(
            QCallModule module,
            string name,
            byte[] sig,
            int sigLen,
            ObjectHandleOnStack resolver,
            ObjectHandleOnStack result);

        internal static IRuntimeMethodInfo GetDynamicMethod(RuntimeModule module, string name, byte[] sig, Resolver resolver)
        {
            IRuntimeMethodInfo? methodInfo = null;
            GetDynamicMethod(
                new QCallModule(ref module),
                name,
                sig,
                sig.Length,
                ObjectHandleOnStack.Create(ref resolver),
                ObjectHandleOnStack.Create(ref methodInfo));
            return methodInfo!;
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "ModuleHandle_GetToken")]
        [SuppressGCTransition]
        private static partial int GetToken(QCallModule module);

        internal static int GetToken(RuntimeModule module)
            => GetToken(new QCallModule(ref module));

        private static void ValidateModulePointer(RuntimeModule module)
        {
            // Make sure we have a valid Module to resolve against.
            if (module is null)
            {
                // Local function to allow inlining of simple null check.
                ThrowInvalidOperationException();
            }

            [StackTraceHidden]
            [DoesNotReturn]
            static void ThrowInvalidOperationException() => throw new InvalidOperationException(SR.InvalidOperation_NullModuleHandle);
        }

        // SQL-CLR LKG9 Compiler dependency
        [RequiresUnreferencedCode("Trimming changes metadata tokens")]
        public RuntimeTypeHandle GetRuntimeTypeHandleFromMetadataToken(int typeToken) { return ResolveTypeHandle(typeToken); }
        [RequiresUnreferencedCode("Trimming changes metadata tokens")]
        public RuntimeTypeHandle ResolveTypeHandle(int typeToken) => ResolveTypeHandle(typeToken, null, null);
        [RequiresUnreferencedCode("Trimming changes metadata tokens")]
        public RuntimeTypeHandle ResolveTypeHandle(int typeToken, RuntimeTypeHandle[]? typeInstantiationContext, RuntimeTypeHandle[]? methodInstantiationContext)
        {
            RuntimeModule module = GetRuntimeModule();
            ValidateModulePointer(module);

            scoped ReadOnlySpan<IntPtr> typeInstantiationContextHandles = default;
            scoped ReadOnlySpan<IntPtr> methodInstantiationContextHandles = default;

            // defensive copy of user-provided array, per CopyRuntimeTypeHandles contract
            if (typeInstantiationContext?.Length > 0)
            {
                typeInstantiationContext = (RuntimeTypeHandle[]?)typeInstantiationContext.Clone();
                typeInstantiationContextHandles = RuntimeTypeHandle.CopyRuntimeTypeHandles(typeInstantiationContext, stackScratch: stackalloc IntPtr[8]);
            }
            if (methodInstantiationContext?.Length > 0)
            {
                methodInstantiationContext = (RuntimeTypeHandle[]?)methodInstantiationContext.Clone();
                methodInstantiationContextHandles = RuntimeTypeHandle.CopyRuntimeTypeHandles(methodInstantiationContext, stackScratch: stackalloc IntPtr[8]);
            }

            fixed (IntPtr* typeInstArgs = typeInstantiationContextHandles, methodInstArgs = methodInstantiationContextHandles)
            {
                try
                {
                    RuntimeType? type = null;
                    ResolveType(new QCallModule(ref module), typeToken, typeInstArgs, typeInstantiationContextHandles.Length, methodInstArgs, methodInstantiationContextHandles.Length, ObjectHandleOnStack.Create(ref type));
                    GC.KeepAlive(typeInstantiationContext);
                    GC.KeepAlive(methodInstantiationContext);
                    return new RuntimeTypeHandle(type!);
                }
                catch (Exception)
                {
                    if (!module.MetadataImport.IsValidToken(typeToken))
                        throw new ArgumentOutOfRangeException(nameof(typeToken),
                            SR.Format(SR.Argument_InvalidToken, typeToken, new ModuleHandle(module)));
                    throw;
                }
            }
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "ModuleHandle_ResolveType")]
        private static partial void ResolveType(QCallModule module,
                                                            int typeToken,
                                                            IntPtr* typeInstArgs,
                                                            int typeInstCount,
                                                            IntPtr* methodInstArgs,
                                                            int methodInstCount,
                                                            ObjectHandleOnStack type);

        // SQL-CLR LKG9 Compiler dependency
        [RequiresUnreferencedCode("Trimming changes metadata tokens")]
        public RuntimeMethodHandle GetRuntimeMethodHandleFromMetadataToken(int methodToken) { return ResolveMethodHandle(methodToken); }
        [RequiresUnreferencedCode("Trimming changes metadata tokens")]
        public RuntimeMethodHandle ResolveMethodHandle(int methodToken) => ResolveMethodHandle(methodToken, null, null);
        [RequiresUnreferencedCode("Trimming changes metadata tokens")]
        public RuntimeMethodHandle ResolveMethodHandle(int methodToken, RuntimeTypeHandle[]? typeInstantiationContext, RuntimeTypeHandle[]? methodInstantiationContext)
        {
            RuntimeModule module = GetRuntimeModule();
            // defensive copy of user-provided array, per CopyRuntimeTypeHandles contract
            typeInstantiationContext = (RuntimeTypeHandle[]?)typeInstantiationContext?.Clone();
            methodInstantiationContext = (RuntimeTypeHandle[]?)methodInstantiationContext?.Clone();

            ReadOnlySpan<IntPtr> typeInstantiationContextHandles = RuntimeTypeHandle.CopyRuntimeTypeHandles(typeInstantiationContext, stackScratch: stackalloc IntPtr[8]);
            ReadOnlySpan<IntPtr> methodInstantiationContextHandles = RuntimeTypeHandle.CopyRuntimeTypeHandles(methodInstantiationContext, stackScratch: stackalloc IntPtr[8]);

            RuntimeMethodHandleInternal handle = ResolveMethodHandleInternal(module, methodToken, typeInstantiationContextHandles, methodInstantiationContextHandles);
            IRuntimeMethodInfo retVal = new RuntimeMethodInfoStub(handle, RuntimeMethodHandle.GetLoaderAllocator(handle));
            GC.KeepAlive(typeInstantiationContext);
            GC.KeepAlive(methodInstantiationContext);
            return new RuntimeMethodHandle(retVal);
        }

        internal static RuntimeMethodHandleInternal ResolveMethodHandleInternal(RuntimeModule module, int methodToken, ReadOnlySpan<IntPtr> typeInstantiationContext, ReadOnlySpan<IntPtr> methodInstantiationContext)
        {
            ValidateModulePointer(module);

            try
            {
                fixed (IntPtr* typeInstArgs = typeInstantiationContext, methodInstArgs = methodInstantiationContext)
                {
                    return ResolveMethod(new QCallModule(ref module), methodToken, typeInstArgs, typeInstantiationContext.Length, methodInstArgs, methodInstantiationContext.Length);
                }
            }
            catch (Exception)
            {
                if (!module.MetadataImport.IsValidToken(methodToken))
                    throw new ArgumentOutOfRangeException(nameof(methodToken),
                        SR.Format(SR.Argument_InvalidToken, methodToken, new ModuleHandle(module)));
                throw;
            }
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "ModuleHandle_ResolveMethod")]
        private static partial RuntimeMethodHandleInternal ResolveMethod(QCallModule module,
                                                        int methodToken,
                                                        IntPtr* typeInstArgs,
                                                        int typeInstCount,
                                                        IntPtr* methodInstArgs,
                                                        int methodInstCount);

        // SQL-CLR LKG9 Compiler dependency
        [RequiresUnreferencedCode("Trimming changes metadata tokens")]
        public RuntimeFieldHandle GetRuntimeFieldHandleFromMetadataToken(int fieldToken) { return ResolveFieldHandle(fieldToken); }
        [RequiresUnreferencedCode("Trimming changes metadata tokens")]
        public RuntimeFieldHandle ResolveFieldHandle(int fieldToken) => ResolveFieldHandle(fieldToken, null, null);
        [RequiresUnreferencedCode("Trimming changes metadata tokens")]
        public RuntimeFieldHandle ResolveFieldHandle(int fieldToken, RuntimeTypeHandle[]? typeInstantiationContext, RuntimeTypeHandle[]? methodInstantiationContext)
        {
            RuntimeModule module = GetRuntimeModule();
            ValidateModulePointer(module);

            scoped ReadOnlySpan<IntPtr> typeInstantiationContextHandles = default;
            scoped ReadOnlySpan<IntPtr> methodInstantiationContextHandles = default;

            // defensive copy of user-provided array, per CopyRuntimeTypeHandles contract
            if (typeInstantiationContext?.Length > 0)
            {
                typeInstantiationContext = (RuntimeTypeHandle[]?)typeInstantiationContext.Clone();
                typeInstantiationContextHandles = RuntimeTypeHandle.CopyRuntimeTypeHandles(typeInstantiationContext, stackScratch: stackalloc IntPtr[8]);
            }
            if (methodInstantiationContext?.Length > 0)
            {
                methodInstantiationContext = (RuntimeTypeHandle[]?)methodInstantiationContext.Clone();
                methodInstantiationContextHandles = RuntimeTypeHandle.CopyRuntimeTypeHandles(methodInstantiationContext, stackScratch: stackalloc IntPtr[8]);
            }

            fixed (IntPtr* typeInstArgs = typeInstantiationContextHandles, methodInstArgs = methodInstantiationContextHandles)
            {
                try
                {
                    IRuntimeFieldInfo? field = null;
                    ResolveField(new QCallModule(ref module), fieldToken, typeInstArgs, typeInstantiationContextHandles.Length, methodInstArgs, methodInstantiationContextHandles.Length, ObjectHandleOnStack.Create(ref field));
                    GC.KeepAlive(typeInstantiationContext);
                    GC.KeepAlive(methodInstantiationContext);
                    return new RuntimeFieldHandle(field!);
                }
                catch (Exception)
                {
                    if (!module.MetadataImport.IsValidToken(fieldToken))
                        throw new ArgumentOutOfRangeException(nameof(fieldToken),
                            SR.Format(SR.Argument_InvalidToken, fieldToken, new ModuleHandle(module)));
                    throw;
                }
            }
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "ModuleHandle_ResolveField")]
        private static partial void ResolveField(QCallModule module,
                                                      int fieldToken,
                                                      IntPtr* typeInstArgs,
                                                      int typeInstCount,
                                                      IntPtr* methodInstArgs,
                                                      int methodInstCount,
                                                      ObjectHandleOnStack retField);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "ModuleHandle_GetModuleType")]
        internal static partial void GetModuleType(QCallModule handle, ObjectHandleOnStack type);

        internal static RuntimeType GetModuleType(RuntimeModule module)
        {
            RuntimeType? type = null;
            GetModuleType(new QCallModule(ref module), ObjectHandleOnStack.Create(ref type));
            return type!;
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "ModuleHandle_GetPEKind")]
        private static partial void GetPEKind(QCallModule handle, int* peKind, int* machine);

        // making this internal, used by Module.GetPEKind
        internal static void GetPEKind(RuntimeModule module, out PortableExecutableKinds peKind, out ImageFileMachine machine)
        {
            int lKind, lMachine;
            GetPEKind(new QCallModule(ref module), &lKind, &lMachine);
            peKind = (PortableExecutableKinds)lKind;
            machine = (ImageFileMachine)lMachine;
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "ModuleHandle_GetMDStreamVersion")]
        [SuppressGCTransition]
        private static partial int GetMDStreamVersion(QCallModule module);

        internal static int GetMDStreamVersion(RuntimeModule module)
            => GetMDStreamVersion(new QCallModule(ref module));

        public int MDStreamVersion => GetMDStreamVersion(GetRuntimeModule());
    }

    internal sealed unsafe partial class Signature
    {
        #region Private Data Members
        //
        // Keep the layout in sync with SignatureNative in the VM
        //
        private RuntimeType[]? _arguments;
        private RuntimeType _declaringType;
        private RuntimeType _returnTypeORfieldType;
#pragma warning disable CA1823, 169
        private object? _keepAlive;
#pragma warning restore CA1823, 169
        private void* _sig;
        private int _csig;
        private int _managedCallingConventionAndArgIteratorFlags; // lowest byte is CallingConvention, upper 3 bytes are ArgIterator flags
#pragma warning disable CA1823, 169
        private int _nSizeOfArgStack;
#pragma warning restore CA1823, 169
        private RuntimeMethodHandleInternal _pMethod;
        #endregion

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "Signature_Init")]
        private static partial void Init(
            ObjectHandleOnStack _this,
            void* pCorSig, int cCorSig,
            RuntimeFieldHandleInternal fieldHandle,
            RuntimeMethodHandleInternal methodHandle);

        [MemberNotNull(nameof(_returnTypeORfieldType))]
        private void Init(
            void* pCorSig, int cCorSig,
            RuntimeFieldHandleInternal fieldHandle,
            RuntimeMethodHandleInternal methodHandle)
        {
            Signature _this = this;
            Init(ObjectHandleOnStack.Create(ref _this),
                pCorSig, cCorSig,
                fieldHandle,
                methodHandle);
            Debug.Assert(_returnTypeORfieldType != null);
        }

        #region Constructors
        public Signature(
            IRuntimeMethodInfo methodHandle,
            RuntimeType[] arguments,
            RuntimeType returnType,
            CallingConventions callingConvention)
        {
            _arguments = arguments;
            _returnTypeORfieldType = returnType;
            _managedCallingConventionAndArgIteratorFlags = (int)callingConvention;
            Debug.Assert((_managedCallingConventionAndArgIteratorFlags & 0xffffff00) == 0);
            _pMethod = methodHandle.Value;

            _declaringType = RuntimeMethodHandle.GetDeclaringType(_pMethod);
            Init(null, 0, default, _pMethod);
            GC.KeepAlive(methodHandle);
        }

        public Signature(IRuntimeMethodInfo methodHandle, RuntimeType declaringType)
        {
            _declaringType = declaringType;
            Init(null, 0, default, methodHandle.Value);
            GC.KeepAlive(methodHandle);
        }

        public Signature(IRuntimeFieldInfo fieldHandle, RuntimeType declaringType)
        {
            _declaringType = declaringType;
            Init(null, 0, fieldHandle.Value, default);
            GC.KeepAlive(fieldHandle);
        }

        public Signature(void* pCorSig, int cCorSig, RuntimeType declaringType)
        {
            _declaringType = declaringType;
            Init(pCorSig, cCorSig, default, default);
        }
        #endregion

        #region Internal Members
        internal CallingConventions CallingConvention => (CallingConventions)(_managedCallingConventionAndArgIteratorFlags & 0xff);
        internal RuntimeType[] Arguments
        {
            get
            {
                Debug.Assert(_arguments != null);
                return _arguments;
            }
        }
        internal RuntimeType ReturnType => _returnTypeORfieldType;
        internal RuntimeType FieldType => _returnTypeORfieldType;

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "Signature_AreEqual")]
        private static partial Interop.BOOL AreEqual(
            void* sig1, int csig1, QCallTypeHandle type1,
            void* sig2, int csig2, QCallTypeHandle type2);

        internal static bool AreEqual(Signature sig1, Signature sig2)
        {
            return AreEqual(
                sig1._sig, sig1._csig, new QCallTypeHandle(ref sig1._declaringType),
                sig2._sig, sig2._csig, new QCallTypeHandle(ref sig2._declaringType)) != Interop.BOOL.FALSE;
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern unsafe int GetParameterOffsetInternal(void* sig, int csig, int parameterIndex);

        internal int GetParameterOffset(int parameterIndex)
        {
            int offsetMaybe = GetParameterOffsetInternal(_sig, _csig, parameterIndex);
            // If the result is negative, it is an error code.
            if (offsetMaybe < 0)
                Marshal.ThrowExceptionForHR(offsetMaybe, new IntPtr(-1));
            return offsetMaybe;
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern unsafe int GetTypeParameterOffsetInternal(void* sig, int csig, int offset, int index);

        internal int GetTypeParameterOffset(int offset, int index)
        {
            if (offset < 0)
            {
                Debug.Assert(offset == -1);
                return offset;
            }

            int offsetMaybe = GetTypeParameterOffsetInternal(_sig, _csig, offset, index);
            // If the result is negative and not -1, it is an error code.
            if (offsetMaybe < 0 && offsetMaybe != -1)
                Marshal.ThrowExceptionForHR(offsetMaybe, new IntPtr(-1));
            return offsetMaybe;
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern unsafe int GetCallingConventionFromFunctionPointerAtOffsetInternal(void* sig, int csig, int offset);

        internal SignatureCallingConvention GetCallingConventionFromFunctionPointerAtOffset(int offset)
        {
            if (offset < 0)
            {
                Debug.Assert(offset == -1);
                return SignatureCallingConvention.Default;
            }

            int callConvMaybe = GetCallingConventionFromFunctionPointerAtOffsetInternal(_sig, _csig, offset);
            // If the result is negative, it is an error code.
            if (callConvMaybe < 0)
                Marshal.ThrowExceptionForHR(callConvMaybe, new IntPtr(-1));
            return (SignatureCallingConvention)callConvMaybe;
        }

        internal Type[] GetCustomModifiers(int parameterIndex, bool required) =>
            GetCustomModifiersAtOffset(GetParameterOffset(parameterIndex), required);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "Signature_GetCustomModifiersAtOffset")]
        private static partial void GetCustomModifiersAtOffset(
            ObjectHandleOnStack sigObj,
            int offset,
            Interop.BOOL required,
            ObjectHandleOnStack result);

        internal Type[] GetCustomModifiersAtOffset(int offset, bool required)
        {
            Signature _this = this;
            Type[]? result = null;
            GetCustomModifiersAtOffset(
                ObjectHandleOnStack.Create(ref _this),
                offset,
                required ? Interop.BOOL.TRUE : Interop.BOOL.FALSE,
                ObjectHandleOnStack.Create(ref result));
            return result!;
        }
        #endregion
    }

    internal abstract class Resolver
    {
        internal struct CORINFO_EH_CLAUSE
        {
            internal int Flags;
            internal int TryOffset;
            internal int TryLength;
            internal int HandlerOffset;
            internal int HandlerLength;
            internal int ClassTokenOrFilterOffset;
        }

        // ILHeader info
        internal abstract RuntimeType? GetJitContext(out int securityControlFlags);
        internal abstract byte[] GetCodeInfo(out int stackSize, out int initLocals, out int EHCount);
        internal abstract byte[] GetLocalsSignature();
        internal abstract unsafe void GetEHInfo(int EHNumber, void* exception);
        internal abstract byte[]? GetRawEHInfo();
        // token resolution
        internal abstract string? GetStringLiteral(int token);
        internal abstract void ResolveToken(int token, out IntPtr typeHandle, out IntPtr methodHandle, out IntPtr fieldHandle);
        internal abstract byte[]? ResolveSignature(int token, int fromMethod);
        //
        internal abstract MethodInfo GetDynamicMethod();
    }
}
