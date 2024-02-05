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
        internal RuntimeTypeHandle GetNativeHandle()
        {
            // Create local copy to avoid a race condition
            RuntimeType type = m_type;
            if (type == null)
                throw new ArgumentNullException(null, SR.Arg_InvalidHandle);
            return new RuntimeTypeHandle(type);
        }

        // Returns type for interop with EE. The type is guaranteed to be non-null.
        internal RuntimeType GetTypeChecked()
        {
            // Create local copy to avoid a race condition
            RuntimeType type = m_type;
            if (type == null)
                throw new ArgumentNullException(null, SR.Arg_InvalidHandle);
            return type;
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern bool IsInstanceOfType(RuntimeType type, [NotNullWhen(true)] object? o);

        /// <summary>
        /// Returns a new <see cref="RuntimeTypeHandle"/> object created from a handle to a RuntimeType.
        /// </summary>
        /// <param name="value">An IntPtr handle to a RuntimeType to create a <see cref="RuntimeTypeHandle"/> object from.</param>
        /// <returns>A new <see cref="RuntimeTypeHandle"/> object that corresponds to the value parameter.</returns>
        public static RuntimeTypeHandle FromIntPtr(IntPtr value) => new RuntimeTypeHandle(Type.GetTypeFromHandleUnsafe(value));

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
        internal RuntimeType m_type;

        public override int GetHashCode()
            => m_type?.GetHashCode() ?? 0;

        public override bool Equals(object? obj)
            => (obj is RuntimeTypeHandle handle) && ReferenceEquals(handle.m_type, m_type);

        public bool Equals(RuntimeTypeHandle handle)
            => ReferenceEquals(handle.m_type, m_type);

        public IntPtr Value => m_type?.m_handle ?? 0;

        internal RuntimeTypeHandle(RuntimeType type)
        {
            m_type = type;
        }

        internal bool IsNullHandle()
        {
            return m_type == null;
        }

        internal static bool IsTypeDefinition(RuntimeType type)
        {
            CorElementType corElemType = GetCorElementType(type);
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
            return RuntimeHelpers.IsPrimitiveType(GetCorElementType(type));
        }

        internal static bool IsByRef(RuntimeType type)
        {
            CorElementType corElemType = GetCorElementType(type);
            return corElemType == CorElementType.ELEMENT_TYPE_BYREF;
        }

        internal static bool IsPointer(RuntimeType type)
        {
            CorElementType corElemType = GetCorElementType(type);
            return corElemType == CorElementType.ELEMENT_TYPE_PTR;
        }

        internal static bool IsArray(RuntimeType type)
        {
            CorElementType corElemType = GetCorElementType(type);
            return corElemType == CorElementType.ELEMENT_TYPE_ARRAY || corElemType == CorElementType.ELEMENT_TYPE_SZARRAY;
        }

        internal static bool IsSZArray(RuntimeType type)
        {
            CorElementType corElemType = GetCorElementType(type);
            return corElemType == CorElementType.ELEMENT_TYPE_SZARRAY;
        }

        internal static bool IsFunctionPointer(RuntimeType type)
        {
            CorElementType corElemType = GetCorElementType(type);
            return corElemType == CorElementType.ELEMENT_TYPE_FNPTR;
        }

        internal static bool HasElementType(RuntimeType type)
        {
            CorElementType corElemType = GetCorElementType(type);

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

        /// <summary>
        /// Given a RuntimeType, returns information about how to activate it via calli
        /// semantics. This method will ensure the type object is fully initialized within
        /// the VM, but it will not call any static ctors on the type.
        /// </summary>
        internal static void GetActivationInfo(
            RuntimeType rt,
            out delegate*<void*, object> pfnAllocator,
            out void* vAllocatorFirstArg,
            out delegate*<object, void> pfnCtor,
            out bool ctorIsPublic)
        {
            Debug.Assert(rt != null);

            delegate*<void*, object> pfnAllocatorTemp = default;
            void* vAllocatorFirstArgTemp = default;
            delegate*<object, void> pfnCtorTemp = default;
            Interop.BOOL fCtorIsPublicTemp = default;

            GetActivationInfo(
                ObjectHandleOnStack.Create(ref rt),
                &pfnAllocatorTemp, &vAllocatorFirstArgTemp,
                &pfnCtorTemp, &fCtorIsPublicTemp);

            pfnAllocator = pfnAllocatorTemp;
            vAllocatorFirstArg = vAllocatorFirstArgTemp;
            pfnCtor = pfnCtorTemp;
            ctorIsPublic = fCtorIsPublicTemp != Interop.BOOL.FALSE;
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "RuntimeTypeHandle_GetActivationInfo")]
        private static partial void GetActivationInfo(
            ObjectHandleOnStack pRuntimeType,
            delegate*<void*, object>* ppfnAllocator,
            void** pvAllocatorFirstArg,
            delegate*<object, void>* ppfnCtor,
            Interop.BOOL* pfCtorIsPublic);

#if FEATURE_COMINTEROP
        // Referenced by unmanaged layer (see GetActivationInfo).
        // First parameter is ComClassFactory*.
        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern object AllocateComObject(void* pClassFactory);
#endif

        internal RuntimeType GetRuntimeType()
        {
            return m_type;
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern CorElementType GetCorElementType(RuntimeType type);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern RuntimeAssembly GetAssembly(RuntimeType type);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern RuntimeModule GetModule(RuntimeType type);

        public ModuleHandle GetModuleHandle()
        {
            return new ModuleHandle(GetModule(m_type));
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern RuntimeType GetBaseType(RuntimeType type);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern TypeAttributes GetAttributes(RuntimeType type);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern RuntimeType GetElementType(RuntimeType type);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern bool CompareCanonicalHandles(RuntimeType left, RuntimeType right);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern int GetArrayRank(RuntimeType type);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern int GetToken(RuntimeType type);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern RuntimeMethodHandleInternal GetMethodAt(RuntimeType type, int slot);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern Type[] GetArgumentTypesFromFunctionPointer(RuntimeType type);

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

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern bool GetFields(RuntimeType type, IntPtr* result, int* count);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern Type[]? GetInterfaces(RuntimeType type);

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

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern int GetNumVirtualsAndStaticVirtuals(RuntimeType type);

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

        internal static bool IsComObject(RuntimeType type, bool isGenericCOM)
        {
#if FEATURE_COMINTEROP
            // We need to check the type handle values - not the instances - to determine if the runtime type is a ComObject.
            if (isGenericCOM)
                return type.TypeHandle.Value == typeof(__ComObject).TypeHandle.Value;

            return CanCastTo(type, (RuntimeType)typeof(__ComObject));
#else
            return false;
#endif
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
        private static extern void* _GetUtf8Name(RuntimeType type);

        internal static MdUtf8String GetUtf8Name(RuntimeType type)
        {
            return new MdUtf8String(_GetUtf8Name(type));
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern bool CanCastTo(RuntimeType type, RuntimeType target);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern RuntimeType GetDeclaringType(RuntimeType type);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern IRuntimeMethodInfo GetDeclaringMethod(RuntimeType type);

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
            RuntimeType type = GetTypeChecked();

            if (!IsGenericVariable(type))
                throw new InvalidOperationException(SR.Arg_NotGenericParameter);

            return GetGenericVariableIndex(type);
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern bool ContainsGenericVariables(RuntimeType handle);

        internal bool ContainsGenericVariables()
        {
            return ContainsGenericVariables(GetTypeChecked());
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern bool SatisfiesConstraints(RuntimeType paramType, IntPtr* pTypeContext, int typeContextLength, IntPtr* pMethodContext, int methodContextLength, RuntimeType toType);

        internal static bool SatisfiesConstraints(RuntimeType paramType, RuntimeType[]? typeContext, RuntimeType[]? methodContext, RuntimeType toType)
        {
            IntPtr[]? typeContextHandles = CopyRuntimeTypeHandles(typeContext, out int typeContextLength);
            IntPtr[]? methodContextHandles = CopyRuntimeTypeHandles(methodContext, out int methodContextLength);

            fixed (IntPtr* pTypeContextHandles = typeContextHandles, pMethodContextHandles = methodContextHandles)
            {
                bool result = SatisfiesConstraints(paramType, pTypeContextHandles, typeContextLength, pMethodContextHandles, methodContextLength, toType);

                GC.KeepAlive(typeContext);
                GC.KeepAlive(methodContext);

                return result;
            }
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern IntPtr _GetMetadataImport(RuntimeType type);

        internal static MetadataImport GetMetadataImport(RuntimeType type)
        {
            return new MetadataImport(_GetMetadataImport(type), type);
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
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern bool IsEquivalentTo(RuntimeType rtType1, RuntimeType rtType2);
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

        internal IntPtr m_handle;
    }

    internal sealed class RuntimeMethodInfoStub : IRuntimeMethodInfo
    {
        public RuntimeMethodInfoStub(RuntimeMethodHandleInternal methodHandleValue, object keepalive)
        {
            m_keepalive = keepalive;
            m_value = methodHandleValue;
        }

        public RuntimeMethodInfoStub(IntPtr methodHandleValue, object keepalive)
        {
            m_keepalive = keepalive;
            m_value = new RuntimeMethodHandleInternal(methodHandleValue);
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
            if (!(obj is RuntimeMethodHandle))
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
        private static extern IRuntimeMethodInfo? _GetCurrentMethod(ref StackCrawlMark stackMark);
        internal static IRuntimeMethodInfo? GetCurrentMethod(ref StackCrawlMark stackMark)
        {
            return _GetCurrentMethod(ref stackMark);
        }

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
        internal static extern RuntimeType GetDeclaringType(RuntimeMethodHandleInternal method);

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
        internal static extern int GetMethodDef(IRuntimeMethodInfo method);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern string GetName(RuntimeMethodHandleInternal method);

        internal static string GetName(IRuntimeMethodInfo method)
        {
            string name = GetName(method.Value);
            GC.KeepAlive(method);
            return name;
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern void* _GetUtf8Name(RuntimeMethodHandleInternal method);

        internal static MdUtf8String GetUtf8Name(RuntimeMethodHandleInternal method)
        {
            return new MdUtf8String(_GetUtf8Name(method));
        }

        [DebuggerStepThrough]
        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern object? InvokeMethod(object? target, void** arguments, Signature sig, bool isConstructor);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern object? ReboxFromNullable(object? src);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern object ReboxToNullable(object? src, RuntimeType destNullableType);

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
        internal static extern RuntimeMethodHandleInternal GetStubIfNeeded(RuntimeMethodHandleInternal method, RuntimeType declaringType, RuntimeType[]? methodInstantiation);

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
        internal static extern bool IsTypicalMethodDefinition(IRuntimeMethodInfo method);

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

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern RuntimeMethodBody? GetMethodBody(IRuntimeMethodInfo method, RuntimeType declaringType);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern bool IsConstructor(RuntimeMethodHandleInternal method);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern LoaderAllocator GetLoaderAllocator(RuntimeMethodHandleInternal method);
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
    }

    [NonVersionable]
    public unsafe struct RuntimeFieldHandle : IEquatable<RuntimeFieldHandle>, ISerializable
    {
        // Returns handle for interop with EE. The handle is guaranteed to be non-null.
        internal RuntimeFieldHandle GetNativeHandle()
        {
            // Create local copy to avoid a race condition
            IRuntimeFieldInfo field = m_ptr;
            if (field == null)
                throw new ArgumentNullException(null, SR.Arg_InvalidHandle);
            return new RuntimeFieldHandle(field);
        }

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
            if (!(obj is RuntimeFieldHandle))
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

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern string GetName(RtFieldInfo field);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern void* _GetUtf8Name(RuntimeFieldHandleInternal field);

        internal static MdUtf8String GetUtf8Name(RuntimeFieldHandleInternal field) { return new MdUtf8String(_GetUtf8Name(field)); }

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern FieldAttributes GetAttributes(RuntimeFieldHandleInternal field);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern RuntimeType GetApproxDeclaringType(RuntimeFieldHandleInternal field);

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
        internal static extern IntPtr GetStaticFieldAddress(RtFieldInfo field, out bool isBoxed);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern int GetToken(RtFieldInfo field);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern object? GetValue(RtFieldInfo field, object? instance, RuntimeType fieldType, RuntimeType? declaringType, bool domainInitialized);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern object? GetValueDirect(RtFieldInfo field, RuntimeType fieldType, void* pTypedRef, RuntimeType? contextType);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern void SetValue(RtFieldInfo field, object? obj, object? value, RuntimeType fieldType, RuntimeType? declaringType, bool domainInitialized);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern void SetValueDirect(RtFieldInfo field, RuntimeType fieldType, void* pTypedRef, object? value, RuntimeType? contextType);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern RuntimeFieldHandleInternal GetStaticFieldForGenericType(RuntimeFieldHandleInternal field, RuntimeType declaringType);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern bool AcquiresContextFromThis(RuntimeFieldHandleInternal field);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern LoaderAllocator GetLoaderAllocator(RuntimeFieldHandleInternal method);

        // ISerializable interface
        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            throw new PlatformNotSupportedException();
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

        #region Internal FCalls

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
            if (!(obj is ModuleHandle))
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

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern IRuntimeMethodInfo GetDynamicMethod(Reflection.Emit.DynamicMethod method, RuntimeModule module, string name, byte[] sig, Resolver resolver);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern int GetToken(RuntimeModule module);

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
                    if (!GetMetadataImport(module).IsValidToken(typeToken))
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
                if (!GetMetadataImport(module).IsValidToken(methodToken))
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
                    if (!GetMetadataImport(module).IsValidToken(fieldToken))
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

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern int GetMDStreamVersion(RuntimeModule module);

        public int MDStreamVersion => GetMDStreamVersion(GetRuntimeModule());

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern IntPtr _GetMetadataImport(RuntimeModule module);

        internal static MetadataImport GetMetadataImport(RuntimeModule module)
        {
            return new MetadataImport(_GetMetadataImport(module), module);
        }
        #endregion
    }

    internal sealed unsafe class Signature
    {
        #region FCalls
        [MemberNotNull(nameof(m_arguments))]
        [MemberNotNull(nameof(m_returnTypeORfieldType))]
        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern void GetSignature(
            void* pCorSig, int cCorSig,
            RuntimeFieldHandleInternal fieldHandle, IRuntimeMethodInfo? methodHandle, RuntimeType? declaringType);
        #endregion

        #region Private Data Members
        //
        // Keep the layout in sync with SignatureNative in the VM
        //
        internal RuntimeType[] m_arguments;
        internal RuntimeType? m_declaringType;
        internal RuntimeType m_returnTypeORfieldType;
        internal object? m_keepalive;
        internal void* m_sig;
        internal int m_managedCallingConventionAndArgIteratorFlags; // lowest byte is CallingConvention, upper 3 bytes are ArgIterator flags
        internal int m_nSizeOfArgStack;
        internal int m_csig;
        internal RuntimeMethodHandleInternal m_pMethod;
        #endregion

        #region Constructors
        public Signature(
            IRuntimeMethodInfo method,
            RuntimeType[] arguments,
            RuntimeType returnType,
            CallingConventions callingConvention)
        {
            m_pMethod = method.Value;
            m_arguments = arguments;
            m_returnTypeORfieldType = returnType;
            m_managedCallingConventionAndArgIteratorFlags = (byte)callingConvention;

            GetSignature(null, 0, default, method, null);
        }

        public Signature(IRuntimeMethodInfo methodHandle, RuntimeType declaringType)
        {
            GetSignature(null, 0, default, methodHandle, declaringType);
        }

        public Signature(IRuntimeFieldInfo fieldHandle, RuntimeType declaringType)
        {
            GetSignature(null, 0, fieldHandle.Value, null, declaringType);
            GC.KeepAlive(fieldHandle);
        }

        public Signature(void* pCorSig, int cCorSig, RuntimeType declaringType)
        {
            GetSignature(pCorSig, cCorSig, default, null, declaringType);
        }
        #endregion

        #region Internal Members
        internal CallingConventions CallingConvention => (CallingConventions)(byte)m_managedCallingConventionAndArgIteratorFlags;
        internal RuntimeType[] Arguments => m_arguments;
        internal RuntimeType ReturnType => m_returnTypeORfieldType;
        internal RuntimeType FieldType => m_returnTypeORfieldType;

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern bool CompareSig(Signature sig1, Signature sig2);

        internal Type[] GetCustomModifiers(int parameterIndex, bool required) =>
            GetCustomModifiersAtOffset(GetParameterOffset(parameterIndex), required);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal extern int GetParameterOffset(int parameterIndex);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal extern int GetTypeParameterOffset(int offset, int index);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal extern SignatureCallingConvention GetCallingConventionFromFunctionPointerAtOffset(int offset);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal extern Type[] GetCustomModifiersAtOffset(int offset, bool required);
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
