// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Runtime.Serialization;
using System.Threading;

namespace System
{
    public unsafe struct RuntimeTypeHandle : ISerializable
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

        internal static Type GetTypeHelper(Type typeStart, Type[]? genericArgs, IntPtr pModifiers, int cModifiers)
        {
            Type type = typeStart;

            if (genericArgs != null)
            {
                type = type.MakeGenericType(genericArgs);
            }

            if (cModifiers > 0)
            {
                int* arModifiers = (int*)pModifiers.ToPointer();
                for (int i = 0; i < cModifiers; i++)
                {
                    if ((CorElementType)Marshal.ReadInt32((IntPtr)arModifiers, i * sizeof(int)) == CorElementType.ELEMENT_TYPE_PTR)
                        type = type.MakePointerType();
                    else if ((CorElementType)Marshal.ReadInt32((IntPtr)arModifiers, i * sizeof(int)) == CorElementType.ELEMENT_TYPE_BYREF)
                        type = type.MakeByRefType();
                    else if ((CorElementType)Marshal.ReadInt32((IntPtr)arModifiers, i * sizeof(int)) == CorElementType.ELEMENT_TYPE_SZARRAY)
                        type = type.MakeArrayType();
                    else
                        type = type.MakeArrayType(Marshal.ReadInt32((IntPtr)arModifiers, ++i * sizeof(int)));
                }
            }

            return type;
        }

        public static bool operator ==(RuntimeTypeHandle left, object? right) => left.Equals(right);

        public static bool operator ==(object? left, RuntimeTypeHandle right) => right.Equals(left);

        public static bool operator !=(RuntimeTypeHandle left, object? right) => !left.Equals(right);

        public static bool operator !=(object? left, RuntimeTypeHandle right) => !right.Equals(left);

        // This is the RuntimeType for the type
        internal RuntimeType m_type;

        public override int GetHashCode()
        {
            return m_type != null ? m_type.GetHashCode() : 0;
        }

        public override bool Equals(object? obj)
        {
            if (!(obj is RuntimeTypeHandle))
                return false;

            RuntimeTypeHandle handle = (RuntimeTypeHandle)obj;
            return handle.m_type == m_type;
        }

        public bool Equals(RuntimeTypeHandle handle)
        {
            return handle.m_type == m_type;
        }

        public IntPtr Value => m_type != null ? m_type.m_handle : IntPtr.Zero;

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern IntPtr GetValueInternal(RuntimeTypeHandle handle);

        internal RuntimeTypeHandle(RuntimeType type)
        {
            m_type = type;
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

            if (HasInstantiation(type) && !IsGenericTypeDefinition(type))
                return false;

            return true;
        }

        internal static bool IsPrimitive(RuntimeType type)
        {
            CorElementType corElemType = GetCorElementType(type);
            return (corElemType >= CorElementType.ELEMENT_TYPE_BOOLEAN && corElemType <= CorElementType.ELEMENT_TYPE_R8) ||
                    corElemType == CorElementType.ELEMENT_TYPE_I ||
                    corElemType == CorElementType.ELEMENT_TYPE_U;
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

        internal static bool HasElementType(RuntimeType type)
        {
            CorElementType corElemType = GetCorElementType(type);

            return corElemType == CorElementType.ELEMENT_TYPE_ARRAY || corElemType == CorElementType.ELEMENT_TYPE_SZARRAY // IsArray
                   || (corElemType == CorElementType.ELEMENT_TYPE_PTR)                                          // IsPointer
                   || (corElemType == CorElementType.ELEMENT_TYPE_BYREF);                                      // IsByRef
        }

        internal static IntPtr[]? CopyRuntimeTypeHandles(RuntimeTypeHandle[]? inHandles, out int length)
        {
            if (inHandles == null || inHandles.Length == 0)
            {
                length = 0;
                return null;
            }

            IntPtr[] outHandles = new IntPtr[inHandles.Length];
            for (int i = 0; i < inHandles.Length; i++)
            {
                outHandles[i] = inHandles[i].Value;
            }
            length = outHandles.Length;
            return outHandles;
        }

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
                outHandles[i] = inHandles[i].GetTypeHandleInternal().Value;
            }
            length = outHandles.Length;
            return outHandles;
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern object CreateInstance(RuntimeType type, bool publicOnly, bool wrapExceptions, ref bool canBeCached, ref RuntimeMethodHandleInternal ctor, ref bool hasNoDefaultCtor);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern object Allocate(RuntimeType type);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern object CreateInstanceForAnotherGenericParameter(RuntimeType type, RuntimeType genericParameter);

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
            return new ModuleHandle(RuntimeTypeHandle.GetModule(m_type));
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

        // This is managed wrapper for MethodTable::IntroducedMethodIterator
        internal struct IntroducedMethodEnumerator
        {
            private bool _firstCall;
            private RuntimeMethodHandleInternal _handle;

            internal IntroducedMethodEnumerator(RuntimeType type)
            {
                _handle = RuntimeTypeHandle.GetFirstIntroducedMethod(type);
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
                    RuntimeTypeHandle.GetNextIntroducedMethod(ref _handle);
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

        [DllImport(RuntimeHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern void GetConstraints(QCallTypeHandle handle, ObjectHandleOnStack types);

        internal Type[] GetConstraints()
        {
            Type[]? types = null;
            RuntimeTypeHandle nativeHandle = GetNativeHandle();

            GetConstraints(new QCallTypeHandle(ref nativeHandle), ObjectHandleOnStack.Create(ref types));

            return types!;
        }

        [DllImport(RuntimeHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern IntPtr GetGCHandle(QCallTypeHandle handle, GCHandleType type);

        internal IntPtr GetGCHandle(GCHandleType type)
        {
            RuntimeTypeHandle nativeHandle = GetNativeHandle();
            return GetGCHandle(new QCallTypeHandle(ref nativeHandle), type);
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern int GetNumVirtuals(RuntimeType type);

        [DllImport(RuntimeHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern void VerifyInterfaceIsImplemented(QCallTypeHandle handle, QCallTypeHandle interfaceHandle);

        internal void VerifyInterfaceIsImplemented(RuntimeTypeHandle interfaceHandle)
        {
            RuntimeTypeHandle nativeHandle = GetNativeHandle();
            RuntimeTypeHandle nativeInterfaceHandle = interfaceHandle.GetNativeHandle();
            VerifyInterfaceIsImplemented(new QCallTypeHandle(ref nativeHandle), new QCallTypeHandle(ref nativeInterfaceHandle));
        }

        [DllImport(RuntimeHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern RuntimeMethodHandleInternal GetInterfaceMethodImplementation(QCallTypeHandle handle, QCallTypeHandle interfaceHandle, RuntimeMethodHandleInternal interfaceMethodHandle);

        internal RuntimeMethodHandleInternal GetInterfaceMethodImplementation(RuntimeTypeHandle interfaceHandle, RuntimeMethodHandleInternal interfaceMethodHandle)
        {
            RuntimeTypeHandle nativeHandle = GetNativeHandle();
            RuntimeTypeHandle nativeInterfaceHandle = interfaceHandle.GetNativeHandle();
            return GetInterfaceMethodImplementation(new QCallTypeHandle(ref nativeHandle), new QCallTypeHandle(ref nativeInterfaceHandle), interfaceMethodHandle);
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern bool IsComObject(RuntimeType type, bool isGenericCOM);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern bool IsInterface(RuntimeType type);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern bool IsByRefLike(RuntimeType type);

        [DllImport(RuntimeHelpers.QCall, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool _IsVisible(QCallTypeHandle typeHandle);

        internal static bool IsVisible(RuntimeType type)
        {
            return _IsVisible(new QCallTypeHandle(ref type));
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern bool IsValueType(RuntimeType type);

        [DllImport(RuntimeHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern void ConstructName(QCallTypeHandle handle, TypeNameFormatFlags formatFlags, StringHandleOnStack retString);

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

        [DllImport(RuntimeHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern void GetTypeByName(string name, bool throwOnError, bool ignoreCase, StackCrawlMarkHandle stackMark,
            ObjectHandleOnStack assemblyLoadContext,
            bool loadTypeFromPartialName, ObjectHandleOnStack type, ObjectHandleOnStack keepalive);

        // Wrapper function to reduce the need for ifdefs.
        internal static RuntimeType? GetTypeByName(string name, bool throwOnError, bool ignoreCase, ref StackCrawlMark stackMark, bool loadTypeFromPartialName)
        {
            return GetTypeByName(name, throwOnError, ignoreCase, ref stackMark, AssemblyLoadContext.CurrentContextualReflectionContext!, loadTypeFromPartialName);
        }

        internal static RuntimeType? GetTypeByName(string name, bool throwOnError, bool ignoreCase, ref StackCrawlMark stackMark,
                                                  AssemblyLoadContext assemblyLoadContext,
                                                  bool loadTypeFromPartialName)
        {
            if (string.IsNullOrEmpty(name))
            {
                if (throwOnError)
                    throw new TypeLoadException(SR.Arg_TypeLoadNullStr);

                return null;
            }

            RuntimeType? type = null;
            object? keepAlive = null;
            AssemblyLoadContext assemblyLoadContextStack = assemblyLoadContext;
            GetTypeByName(name, throwOnError, ignoreCase,
                new StackCrawlMarkHandle(ref stackMark),
                ObjectHandleOnStack.Create(ref assemblyLoadContextStack),
                loadTypeFromPartialName, ObjectHandleOnStack.Create(ref type), ObjectHandleOnStack.Create(ref keepAlive));
            GC.KeepAlive(keepAlive);

            return type;
        }

        [DllImport(RuntimeHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern void GetTypeByNameUsingCARules(string name, QCallModule scope, ObjectHandleOnStack type);

        internal static RuntimeType GetTypeByNameUsingCARules(string name, RuntimeModule scope)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException(null, nameof(name));

            RuntimeType type = null!;
            GetTypeByNameUsingCARules(name, new QCallModule(ref scope), ObjectHandleOnStack.Create(ref type));

            return type;
        }

        [DllImport(RuntimeHelpers.QCall, CharSet = CharSet.Unicode)]
        internal static extern void GetInstantiation(QCallTypeHandle type, ObjectHandleOnStack types, Interop.BOOL fAsRuntimeTypeArray);

        internal RuntimeType[] GetInstantiationInternal()
        {
            RuntimeType[] types = null!;
            RuntimeTypeHandle nativeHandle = GetNativeHandle();
            GetInstantiation(new QCallTypeHandle(ref nativeHandle), ObjectHandleOnStack.Create(ref types), Interop.BOOL.TRUE);
            return types;
        }

        internal Type[] GetInstantiationPublic()
        {
            Type[] types = null!;
            RuntimeTypeHandle nativeHandle = GetNativeHandle();
            GetInstantiation(new QCallTypeHandle(ref nativeHandle), ObjectHandleOnStack.Create(ref types), Interop.BOOL.FALSE);
            return types;
        }

        [DllImport(RuntimeHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern void Instantiate(QCallTypeHandle handle, IntPtr* pInst, int numGenericArgs, ObjectHandleOnStack type);

        internal RuntimeType Instantiate(Type[]? inst)
        {
            // defensive copy to be sure array is not mutated from the outside during processing
            int instCount;
            IntPtr[]? instHandles = CopyRuntimeTypeHandles(inst, out instCount);

            fixed (IntPtr* pInst = instHandles)
            {
                RuntimeType type = null!;
                RuntimeTypeHandle nativeHandle = GetNativeHandle();
                Instantiate(new QCallTypeHandle(ref nativeHandle), pInst, instCount, ObjectHandleOnStack.Create(ref type));
                GC.KeepAlive(inst);
                return type;
            }
        }

        [DllImport(RuntimeHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern void MakeArray(QCallTypeHandle handle, int rank, ObjectHandleOnStack type);

        internal RuntimeType MakeArray(int rank)
        {
            RuntimeType type = null!;
            RuntimeTypeHandle nativeHandle = GetNativeHandle();
            MakeArray(new QCallTypeHandle(ref nativeHandle), rank, ObjectHandleOnStack.Create(ref type));
            return type;
        }

        [DllImport(RuntimeHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern void MakeSZArray(QCallTypeHandle handle, ObjectHandleOnStack type);

        internal RuntimeType MakeSZArray()
        {
            RuntimeType type = null!;
            RuntimeTypeHandle nativeHandle = GetNativeHandle();
            MakeSZArray(new QCallTypeHandle(ref nativeHandle), ObjectHandleOnStack.Create(ref type));
            return type;
        }

        [DllImport(RuntimeHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern void MakeByRef(QCallTypeHandle handle, ObjectHandleOnStack type);

        internal RuntimeType MakeByRef()
        {
            RuntimeType type = null!;
            RuntimeTypeHandle nativeHandle = GetNativeHandle();
            MakeByRef(new QCallTypeHandle(ref nativeHandle), ObjectHandleOnStack.Create(ref type));
            return type;
        }

        [DllImport(RuntimeHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern void MakePointer(QCallTypeHandle handle, ObjectHandleOnStack type);

        internal RuntimeType MakePointer()
        {
            RuntimeType type = null!;
            RuntimeTypeHandle nativeHandle = GetNativeHandle();
            MakePointer(new QCallTypeHandle(ref nativeHandle), ObjectHandleOnStack.Create(ref type));
            return type;
        }

        [DllImport(RuntimeHelpers.QCall, CharSet = CharSet.Unicode)]
        internal static extern Interop.BOOL IsCollectible(QCallTypeHandle handle);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern bool HasInstantiation(RuntimeType type);

        [DllImport(RuntimeHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern void GetGenericTypeDefinition(QCallTypeHandle type, ObjectHandleOnStack retType);

        internal static RuntimeType GetGenericTypeDefinition(RuntimeType type)
        {
            RuntimeType retType = type;

            if (HasInstantiation(retType) && !IsGenericTypeDefinition(retType))
            {
                RuntimeTypeHandle nativeHandle = retType.GetTypeHandleInternal();
                GetGenericTypeDefinition(new QCallTypeHandle(ref nativeHandle), ObjectHandleOnStack.Create(ref retType));
            }

            return retType;
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern bool IsGenericTypeDefinition(RuntimeType type);

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
            int typeContextLength;
            int methodContextLength;
            IntPtr[]? typeContextHandles = CopyRuntimeTypeHandles(typeContext, out typeContextLength);
            IntPtr[]? methodContextHandles = CopyRuntimeTypeHandles(methodContext, out methodContextLength);

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

    internal class RuntimeMethodInfoStub : IRuntimeMethodInfo
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
#pragma warning disable CA1823, 414
        private object m_a = null!;
        private object m_b = null!;
        private object m_c = null!;
        private object m_d = null!;
        private object m_e = null!;
        private object m_f = null!;
        private object m_g = null!;
#pragma warning restore CA1823, 414

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

    public unsafe struct RuntimeMethodHandle : ISerializable
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

        // Used by EE
        private static IntPtr GetValueInternal(RuntimeMethodHandle rmh)
        {
            return rmh.Value;
        }

        // ISerializable interface
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            throw new PlatformNotSupportedException();
        }

        public IntPtr Value => m_value != null ? m_value.Value.Value : IntPtr.Zero;

        public override int GetHashCode()
        {
            return ValueType.GetHashCodeOfPtr(Value);
        }

        public override bool Equals(object? obj)
        {
            if (!(obj is RuntimeMethodHandle))
                return false;

            RuntimeMethodHandle handle = (RuntimeMethodHandle)obj;

            return handle.Value == Value;
        }

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

        [DllImport(RuntimeHelpers.QCall, CharSet = CharSet.Unicode)]
        internal static extern IntPtr GetFunctionPointer(RuntimeMethodHandleInternal handle);

        public IntPtr GetFunctionPointer()
        {
            IntPtr ptr = GetFunctionPointer(EnsureNonNullMethodInfo(m_value!).Value);
            GC.KeepAlive(m_value);
            return ptr;
        }

        [DllImport(RuntimeHelpers.QCall, CharSet = CharSet.Unicode)]
        internal static extern Interop.BOOL GetIsCollectible(RuntimeMethodHandleInternal handle);

        [DllImport(RuntimeHelpers.QCall, CharSet = CharSet.Unicode)]
        internal static extern Interop.BOOL IsCAVisibleFromDecoratedType(
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
            MethodAttributes retVal = RuntimeMethodHandle.GetAttributes(method.Value);
            GC.KeepAlive(method);
            return retVal;
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern MethodImplAttributes GetImplAttributes(IRuntimeMethodInfo method);

        [DllImport(RuntimeHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern void ConstructInstantiation(RuntimeMethodHandleInternal method, TypeNameFormatFlags format, StringHandleOnStack retString);

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
            RuntimeType type = RuntimeMethodHandle.GetDeclaringType(method.Value);
            GC.KeepAlive(method);
            return type;
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern int GetSlot(RuntimeMethodHandleInternal method);

        internal static int GetSlot(IRuntimeMethodInfo method)
        {
            Debug.Assert(method != null);

            int slot = RuntimeMethodHandle.GetSlot(method.Value);
            GC.KeepAlive(method);
            return slot;
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern int GetMethodDef(IRuntimeMethodInfo method);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern string GetName(RuntimeMethodHandleInternal method);

        internal static string GetName(IRuntimeMethodInfo method)
        {
            string name = RuntimeMethodHandle.GetName(method.Value);
            GC.KeepAlive(method);
            return name;
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern void* _GetUtf8Name(RuntimeMethodHandleInternal method);

        internal static MdUtf8String GetUtf8Name(RuntimeMethodHandleInternal method)
        {
            return new MdUtf8String(_GetUtf8Name(method));
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern bool MatchesNameHash(RuntimeMethodHandleInternal method, uint hash);

        [DebuggerStepThroughAttribute]
        [Diagnostics.DebuggerHidden]
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern object InvokeMethod(object? target, object[]? arguments, Signature sig, bool constructor, bool wrapExceptions);

        [DllImport(RuntimeHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern void GetMethodInstantiation(RuntimeMethodHandleInternal method, ObjectHandleOnStack types, Interop.BOOL fAsRuntimeTypeArray);

        internal static RuntimeType[] GetMethodInstantiationInternal(IRuntimeMethodInfo method)
        {
            RuntimeType[] types = null!;
            GetMethodInstantiation(EnsureNonNullMethodInfo(method).Value, ObjectHandleOnStack.Create(ref types), Interop.BOOL.TRUE);
            GC.KeepAlive(method);
            return types;
        }

        internal static RuntimeType[] GetMethodInstantiationInternal(RuntimeMethodHandleInternal method)
        {
            RuntimeType[] types = null!;
            GetMethodInstantiation(method, ObjectHandleOnStack.Create(ref types), Interop.BOOL.TRUE);
            return types;
        }

        internal static Type[] GetMethodInstantiationPublic(IRuntimeMethodInfo method)
        {
            RuntimeType[] types = null!;
            GetMethodInstantiation(EnsureNonNullMethodInfo(method).Value, ObjectHandleOnStack.Create(ref types), Interop.BOOL.FALSE);
            GC.KeepAlive(method);
            return types;
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern bool HasMethodInstantiation(RuntimeMethodHandleInternal method);

        internal static bool HasMethodInstantiation(IRuntimeMethodInfo method)
        {
            bool fRet = RuntimeMethodHandle.HasMethodInstantiation(method.Value);
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
            bool fRet = RuntimeMethodHandle.IsGenericMethodDefinition(method.Value);
            GC.KeepAlive(method);
            return fRet;
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern bool IsTypicalMethodDefinition(IRuntimeMethodInfo method);

        [DllImport(RuntimeHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern void GetTypicalMethodDefinition(RuntimeMethodHandleInternal method, ObjectHandleOnStack outMethod);

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

        [DllImport(RuntimeHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern void StripMethodInstantiation(RuntimeMethodHandleInternal method, ObjectHandleOnStack outMethod);

        internal static IRuntimeMethodInfo StripMethodInstantiation(IRuntimeMethodInfo method)
        {
            IRuntimeMethodInfo strippedMethod = method;

            StripMethodInstantiation(method.Value, ObjectHandleOnStack.Create(ref strippedMethod));
            GC.KeepAlive(method);

            return strippedMethod;
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern bool IsDynamicMethod(RuntimeMethodHandleInternal method);

        [DllImport(RuntimeHelpers.QCall, CharSet = CharSet.Unicode)]
        internal static extern void Destroy(RuntimeMethodHandleInternal method);

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
    internal class RuntimeFieldInfoStub : IRuntimeFieldInfo
    {
        // These unused variables are used to ensure that this class has the same layout as RuntimeFieldInfo
#pragma warning disable 414
        private object m_keepalive = null!;
        private object m_c = null!;
        private object m_d = null!;
        private int m_b;
        private object m_e = null!;
        private RuntimeFieldHandleInternal m_fieldHandle;
#pragma warning restore 414

        RuntimeFieldHandleInternal IRuntimeFieldInfo.Value => m_fieldHandle;
    }

    public unsafe struct RuntimeFieldHandle : ISerializable
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
            return ValueType.GetHashCodeOfPtr(Value);
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

        public static bool operator ==(RuntimeFieldHandle left, RuntimeFieldHandle right) => left.Equals(right);

        public static bool operator !=(RuntimeFieldHandle left, RuntimeFieldHandle right) => !left.Equals(right);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern string GetName(RtFieldInfo field);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern void* _GetUtf8Name(RuntimeFieldHandleInternal field);

        internal static MdUtf8String GetUtf8Name(RuntimeFieldHandleInternal field) { return new MdUtf8String(_GetUtf8Name(field)); }

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern bool MatchesNameHash(RuntimeFieldHandleInternal handle, uint hash);

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
        internal static extern int GetToken(RtFieldInfo field);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern object? GetValue(RtFieldInfo field, object? instance, RuntimeType fieldType, RuntimeType? declaringType, ref bool domainInitialized);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern object? GetValueDirect(RtFieldInfo field, RuntimeType fieldType, void* pTypedRef, RuntimeType? contextType);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern void SetValue(RtFieldInfo field, object? obj, object? value, RuntimeType fieldType, FieldAttributes fieldAttr, RuntimeType? declaringType, ref bool domainInitialized);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern void SetValueDirect(RtFieldInfo field, RuntimeType fieldType, void* pTypedRef, object? value, RuntimeType? contextType);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern RuntimeFieldHandleInternal GetStaticFieldForGenericType(RuntimeFieldHandleInternal field, RuntimeType declaringType);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern bool AcquiresContextFromThis(RuntimeFieldHandleInternal field);

        // ISerializable interface
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            throw new PlatformNotSupportedException();
        }
    }

    public unsafe struct ModuleHandle
    {
        // Returns handle for interop with EE. The handle is guaranteed to be non-null.
        #region Public Static Members
        public static readonly ModuleHandle EmptyHandle = GetEmptyMH();
        #endregion

        private static ModuleHandle GetEmptyMH()
        {
            return default;
        }

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

        public override bool Equals(object? obj)
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
        internal static extern IRuntimeMethodInfo GetDynamicMethod(System.Reflection.Emit.DynamicMethod method, RuntimeModule module, string name, byte[] sig, Resolver resolver);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern int GetToken(RuntimeModule module);

        private static void ValidateModulePointer(RuntimeModule module)
        {
            // Make sure we have a valid Module to resolve against.
            if (module == null)
                throw new InvalidOperationException(SR.InvalidOperation_NullModuleHandle);
        }

        // SQL-CLR LKG9 Compiler dependency
        public RuntimeTypeHandle GetRuntimeTypeHandleFromMetadataToken(int typeToken) { return ResolveTypeHandle(typeToken); }
        public RuntimeTypeHandle ResolveTypeHandle(int typeToken)
        {
            return new RuntimeTypeHandle(ResolveTypeHandleInternal(GetRuntimeModule(), typeToken, null, null));
        }
        public RuntimeTypeHandle ResolveTypeHandle(int typeToken, RuntimeTypeHandle[]? typeInstantiationContext, RuntimeTypeHandle[]? methodInstantiationContext)
        {
            return new RuntimeTypeHandle(ModuleHandle.ResolveTypeHandleInternal(GetRuntimeModule(), typeToken, typeInstantiationContext, methodInstantiationContext));
        }

        internal static RuntimeType ResolveTypeHandleInternal(RuntimeModule module, int typeToken, RuntimeTypeHandle[]? typeInstantiationContext, RuntimeTypeHandle[]? methodInstantiationContext)
        {
            ValidateModulePointer(module);
            if (!ModuleHandle.GetMetadataImport(module).IsValidToken(typeToken))
                throw new ArgumentOutOfRangeException(nameof(typeToken),
                    SR.Format(SR.Argument_InvalidToken, typeToken, new ModuleHandle(module)));

            int typeInstCount, methodInstCount;
            IntPtr[]? typeInstantiationContextHandles = RuntimeTypeHandle.CopyRuntimeTypeHandles(typeInstantiationContext, out typeInstCount);
            IntPtr[]? methodInstantiationContextHandles = RuntimeTypeHandle.CopyRuntimeTypeHandles(methodInstantiationContext, out methodInstCount);

            fixed (IntPtr* typeInstArgs = typeInstantiationContextHandles, methodInstArgs = methodInstantiationContextHandles)
            {
                RuntimeType type = null!;
                ResolveType(new QCallModule(ref module), typeToken, typeInstArgs, typeInstCount, methodInstArgs, methodInstCount, ObjectHandleOnStack.Create(ref type));
                GC.KeepAlive(typeInstantiationContext);
                GC.KeepAlive(methodInstantiationContext);
                return type;
            }
        }

        [DllImport(RuntimeHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern void ResolveType(QCallModule module,
                                                            int typeToken,
                                                            IntPtr* typeInstArgs,
                                                            int typeInstCount,
                                                            IntPtr* methodInstArgs,
                                                            int methodInstCount,
                                                            ObjectHandleOnStack type);

        // SQL-CLR LKG9 Compiler dependency
        public RuntimeMethodHandle GetRuntimeMethodHandleFromMetadataToken(int methodToken) { return ResolveMethodHandle(methodToken); }
        public RuntimeMethodHandle ResolveMethodHandle(int methodToken) { return ResolveMethodHandle(methodToken, null, null); }
        internal static IRuntimeMethodInfo ResolveMethodHandleInternal(RuntimeModule module, int methodToken) { return ModuleHandle.ResolveMethodHandleInternal(module, methodToken, null, null); }
        public RuntimeMethodHandle ResolveMethodHandle(int methodToken, RuntimeTypeHandle[]? typeInstantiationContext, RuntimeTypeHandle[]? methodInstantiationContext)
        {
            return new RuntimeMethodHandle(ResolveMethodHandleInternal(GetRuntimeModule(), methodToken, typeInstantiationContext, methodInstantiationContext));
        }

        internal static IRuntimeMethodInfo ResolveMethodHandleInternal(RuntimeModule module, int methodToken, RuntimeTypeHandle[]? typeInstantiationContext, RuntimeTypeHandle[]? methodInstantiationContext)
        {
            int typeInstCount, methodInstCount;

            IntPtr[]? typeInstantiationContextHandles = RuntimeTypeHandle.CopyRuntimeTypeHandles(typeInstantiationContext, out typeInstCount);
            IntPtr[]? methodInstantiationContextHandles = RuntimeTypeHandle.CopyRuntimeTypeHandles(methodInstantiationContext, out methodInstCount);

            RuntimeMethodHandleInternal handle = ResolveMethodHandleInternalCore(module, methodToken, typeInstantiationContextHandles, typeInstCount, methodInstantiationContextHandles, methodInstCount);
            IRuntimeMethodInfo retVal = new RuntimeMethodInfoStub(handle, RuntimeMethodHandle.GetLoaderAllocator(handle));
            GC.KeepAlive(typeInstantiationContext);
            GC.KeepAlive(methodInstantiationContext);
            return retVal;
        }

        internal static RuntimeMethodHandleInternal ResolveMethodHandleInternalCore(RuntimeModule module, int methodToken, IntPtr[]? typeInstantiationContext, int typeInstCount, IntPtr[]? methodInstantiationContext, int methodInstCount)
        {
            ValidateModulePointer(module);
            if (!ModuleHandle.GetMetadataImport(module.GetNativeHandle()).IsValidToken(methodToken))
                throw new ArgumentOutOfRangeException(nameof(methodToken),
                    SR.Format(SR.Argument_InvalidToken, methodToken, new ModuleHandle(module)));

            fixed (IntPtr* typeInstArgs = typeInstantiationContext, methodInstArgs = methodInstantiationContext)
            {
                return ResolveMethod(new QCallModule(ref module), methodToken, typeInstArgs, typeInstCount, methodInstArgs, methodInstCount);
            }
        }

        [DllImport(RuntimeHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern RuntimeMethodHandleInternal ResolveMethod(QCallModule module,
                                                        int methodToken,
                                                        IntPtr* typeInstArgs,
                                                        int typeInstCount,
                                                        IntPtr* methodInstArgs,
                                                        int methodInstCount);

        // SQL-CLR LKG9 Compiler dependency
        public RuntimeFieldHandle GetRuntimeFieldHandleFromMetadataToken(int fieldToken) { return ResolveFieldHandle(fieldToken); }
        public RuntimeFieldHandle ResolveFieldHandle(int fieldToken) { return new RuntimeFieldHandle(ResolveFieldHandleInternal(GetRuntimeModule(), fieldToken, null, null)); }
        public RuntimeFieldHandle ResolveFieldHandle(int fieldToken, RuntimeTypeHandle[]? typeInstantiationContext, RuntimeTypeHandle[]? methodInstantiationContext)
        { return new RuntimeFieldHandle(ResolveFieldHandleInternal(GetRuntimeModule(), fieldToken, typeInstantiationContext, methodInstantiationContext)); }

        internal static IRuntimeFieldInfo ResolveFieldHandleInternal(RuntimeModule module, int fieldToken, RuntimeTypeHandle[]? typeInstantiationContext, RuntimeTypeHandle[]? methodInstantiationContext)
        {
            ValidateModulePointer(module);
            if (!ModuleHandle.GetMetadataImport(module.GetNativeHandle()).IsValidToken(fieldToken))
                throw new ArgumentOutOfRangeException(nameof(fieldToken),
                    SR.Format(SR.Argument_InvalidToken, fieldToken, new ModuleHandle(module)));

            // defensive copy to be sure array is not mutated from the outside during processing
            int typeInstCount, methodInstCount;
            IntPtr[]? typeInstantiationContextHandles = RuntimeTypeHandle.CopyRuntimeTypeHandles(typeInstantiationContext, out typeInstCount);
            IntPtr[]? methodInstantiationContextHandles = RuntimeTypeHandle.CopyRuntimeTypeHandles(methodInstantiationContext, out methodInstCount);

            fixed (IntPtr* typeInstArgs = typeInstantiationContextHandles, methodInstArgs = methodInstantiationContextHandles)
            {
                IRuntimeFieldInfo field = null!;
                ResolveField(new QCallModule(ref module), fieldToken, typeInstArgs, typeInstCount, methodInstArgs, methodInstCount, ObjectHandleOnStack.Create(ref field));
                GC.KeepAlive(typeInstantiationContext);
                GC.KeepAlive(methodInstantiationContext);
                return field;
            }
        }

        [DllImport(RuntimeHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern void ResolveField(QCallModule module,
                                                      int fieldToken,
                                                      IntPtr* typeInstArgs,
                                                      int typeInstCount,
                                                      IntPtr* methodInstArgs,
                                                      int methodInstCount,
                                                      ObjectHandleOnStack retField);

        [DllImport(RuntimeHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern Interop.BOOL _ContainsPropertyMatchingHash(QCallModule module, int propertyToken, uint hash);

        internal static bool ContainsPropertyMatchingHash(RuntimeModule module, int propertyToken, uint hash)
        {
            return _ContainsPropertyMatchingHash(new QCallModule(ref module), propertyToken, hash) != Interop.BOOL.FALSE;
        }

        [DllImport(RuntimeHelpers.QCall, CharSet = CharSet.Unicode)]
        internal static extern void GetModuleType(QCallModule handle, ObjectHandleOnStack type);

        internal static RuntimeType GetModuleType(RuntimeModule module)
        {
            RuntimeType type = null!;
            GetModuleType(new QCallModule(ref module), ObjectHandleOnStack.Create(ref type));
            return type;
        }

        [DllImport(RuntimeHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern void GetPEKind(QCallModule handle, int* peKind, int* machine);

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

        public int MDStreamVersion => GetMDStreamVersion(GetRuntimeModule().GetNativeHandle());

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern IntPtr _GetMetadataImport(RuntimeModule module);

        internal static MetadataImport GetMetadataImport(RuntimeModule module)
        {
            return new MetadataImport(_GetMetadataImport(module.GetNativeHandle()), module);
        }
        #endregion
    }

    internal unsafe class Signature
    {
        #region Definitions
        internal enum MdSigCallingConvention : byte
        {
            Generics = 0x10,
            HasThis = 0x20,
            ExplicitThis = 0x40,
            CallConvMask = 0x0F,
            Default = 0x00,
            C = 0x01,
            StdCall = 0x02,
            ThisCall = 0x03,
            FastCall = 0x04,
            Vararg = 0x05,
            Field = 0x06,
            LocalSig = 0x07,
            Property = 0x08,
            Unmgd = 0x09,
            GenericInst = 0x0A,
            Max = 0x0B,
        }
        #endregion

        #region FCalls
        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern void GetSignature(
            void* pCorSig, int cCorSig,
            RuntimeFieldHandleInternal fieldHandle, IRuntimeMethodInfo? methodHandle, RuntimeType? declaringType);

        #endregion

        #region Private Data Members
        //
        // Keep the layout in sync with SignatureNative in the VM
        //
        internal RuntimeType[] m_arguments = null!;
        internal RuntimeType m_declaringType = null!; // seems not used
        internal RuntimeType m_returnTypeORfieldType = null!;
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

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal extern Type[] GetCustomModifiers(int position, bool required);
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
