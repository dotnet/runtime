// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


namespace System 
{
    using System;
    using System.Reflection;
    using System.Reflection.Emit;
    using System.Runtime;
    using System.Runtime.ConstrainedExecution;
    using System.Diagnostics;
    using System.Runtime.Serialization;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Runtime.Versioning;
    using System.Text;
    using System.Globalization;
    using System.Security;
    using System.Security.Permissions;
    using Microsoft.Win32.SafeHandles;
    using System.Diagnostics.Contracts;
    using StackCrawlMark = System.Threading.StackCrawlMark;

    [Serializable]
    [System.Runtime.InteropServices.ComVisible(true)]
    public unsafe struct RuntimeTypeHandle : ISerializable
    {
        // Returns handle for interop with EE. The handle is guaranteed to be non-null.
        internal RuntimeTypeHandle GetNativeHandle()
        {
            // Create local copy to avoid a race condition
            RuntimeType type = m_type;
            if (type == null)
                throw new ArgumentNullException(null, Environment.GetResourceString("Arg_InvalidHandle"));
            return new RuntimeTypeHandle(type);
        }

        // Returns type for interop with EE. The type is guaranteed to be non-null.
        internal RuntimeType GetTypeChecked()
        {
            // Create local copy to avoid a race condition
            RuntimeType type = m_type;
            if (type == null)
                throw new ArgumentNullException(null, Environment.GetResourceString("Arg_InvalidHandle"));
            return type;
        }

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal extern static bool IsInstanceOfType(RuntimeType type, Object o);
        
        [System.Security.SecuritySafeCritical]  // auto-generated
        internal unsafe static Type GetTypeHelper(Type typeStart, Type[] genericArgs, IntPtr pModifiers, int cModifiers)
        {
            Type type = typeStart;

            if (genericArgs != null)
            {
                type = type.MakeGenericType(genericArgs);
            }

            if (cModifiers > 0)
            {
                int* arModifiers = (int*)pModifiers.ToPointer();
                for(int i = 0; i < cModifiers; i++)
                {
                    if ((CorElementType)Marshal.ReadInt32((IntPtr)arModifiers, i * sizeof(int)) == CorElementType.Ptr)
                        type = type.MakePointerType();
                    
                    else if ((CorElementType)Marshal.ReadInt32((IntPtr)arModifiers, i * sizeof(int)) == CorElementType.ByRef)
                        type = type.MakeByRefType();

                    else if ((CorElementType)Marshal.ReadInt32((IntPtr)arModifiers, i * sizeof(int)) == CorElementType.SzArray)
                        type = type.MakeArrayType();

                    else
                        type = type.MakeArrayType(Marshal.ReadInt32((IntPtr)arModifiers, ++i * sizeof(int)));
                }
            }
            
            return type;
        }

        public static bool operator ==(RuntimeTypeHandle left, object right) { return left.Equals(right); }
        
        public static bool operator ==(object left, RuntimeTypeHandle right) { return right.Equals(left); }
        
        public static bool operator !=(RuntimeTypeHandle left, object right) { return !left.Equals(right); }

        public static bool operator !=(object left, RuntimeTypeHandle right) { return !right.Equals(left); }

        internal static RuntimeTypeHandle EmptyHandle
        {
            get
            {
                return new RuntimeTypeHandle(null);
            }
        }
        

        // This is the RuntimeType for the type
        private RuntimeType m_type;

        public override int GetHashCode()
        {
            return m_type != null ? m_type.GetHashCode() : 0;
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        public override bool Equals(object obj)
        {
            if(!(obj is RuntimeTypeHandle))
                return false;

            RuntimeTypeHandle handle =(RuntimeTypeHandle)obj;
            return handle.m_type == m_type;
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        public bool Equals(RuntimeTypeHandle handle)
        {
            return handle.m_type == m_type;
        }

        public IntPtr Value
        {
            [SecurityCritical]
            get
            {
                return m_type != null ? m_type.m_handle : IntPtr.Zero;
            }
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern IntPtr GetValueInternal(RuntimeTypeHandle handle);

        internal RuntimeTypeHandle(RuntimeType type)
        {
            m_type = type;
        }

        internal bool IsNullHandle() 
        {
            return m_type == null; 
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        internal static bool IsPrimitive(RuntimeType type)
        {
            CorElementType corElemType = GetCorElementType(type);
            return (corElemType >= CorElementType.Boolean && corElemType <= CorElementType.R8) ||
                    corElemType == CorElementType.I ||
                    corElemType == CorElementType.U;
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        internal static bool IsByRef(RuntimeType type)
        {
            CorElementType corElemType = GetCorElementType(type);
            return (corElemType == CorElementType.ByRef);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        internal static bool IsPointer(RuntimeType type)
        {
            CorElementType corElemType = GetCorElementType(type);
            return (corElemType == CorElementType.Ptr);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        internal static bool IsArray(RuntimeType type)
        {
            CorElementType corElemType = GetCorElementType(type);
            return (corElemType == CorElementType.Array || corElemType == CorElementType.SzArray);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        internal static bool IsSzArray(RuntimeType type)
        {
            CorElementType corElemType = GetCorElementType(type);
            return (corElemType == CorElementType.SzArray);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        internal static bool HasElementType(RuntimeType type)
        {
            CorElementType corElemType = GetCorElementType(type);

            return ((corElemType == CorElementType.Array || corElemType == CorElementType.SzArray) // IsArray
                   || (corElemType == CorElementType.Ptr)                                          // IsPointer
                   || (corElemType == CorElementType.ByRef));                                      // IsByRef
        }

        [SecurityCritical]
        internal static IntPtr[] CopyRuntimeTypeHandles(RuntimeTypeHandle[] inHandles, out int length)
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

        [SecurityCritical]
        internal static IntPtr[] CopyRuntimeTypeHandles(Type[] inHandles, out int length)
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

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern Object CreateInstance(RuntimeType type, bool publicOnly, bool noCheck, ref bool canBeCached, ref RuntimeMethodHandleInternal ctor, ref bool bNeedSecurityCheck);

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern Object CreateCaInstance(RuntimeType type, IRuntimeMethodInfo ctor);

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern Object Allocate(RuntimeType type);

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern Object CreateInstanceForAnotherGenericParameter(RuntimeType type, RuntimeType genericParameter);
        
        internal RuntimeType GetRuntimeType()
        {
            return m_type;
        }

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal extern static CorElementType GetCorElementType(RuntimeType type);

        [System.Security.SecuritySafeCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal extern static RuntimeAssembly GetAssembly(RuntimeType type);

        [System.Security.SecuritySafeCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        internal extern static RuntimeModule GetModule(RuntimeType type);

        [CLSCompliant(false)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        public ModuleHandle GetModuleHandle()
        {
            return new ModuleHandle(RuntimeTypeHandle.GetModule(m_type));
        }
        
        [System.Security.SecuritySafeCritical]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal extern static RuntimeType GetBaseType(RuntimeType type);

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal extern static TypeAttributes GetAttributes(RuntimeType type); 

        [System.Security.SecuritySafeCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal extern static RuntimeType GetElementType(RuntimeType type);

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal extern static bool CompareCanonicalHandles(RuntimeType left, RuntimeType right);

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal extern static int GetArrayRank(RuntimeType type); 

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal extern static int GetToken(RuntimeType type); 
        
        [System.Security.SecuritySafeCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal extern static RuntimeMethodHandleInternal GetMethodAt(RuntimeType type, int slot);

        // This is managed wrapper for MethodTable::IntroducedMethodIterator
        internal struct IntroducedMethodEnumerator
        {
            bool                    _firstCall;
            RuntimeMethodHandleInternal _handle;

            [System.Security.SecuritySafeCritical]  // auto-generated
            internal IntroducedMethodEnumerator(RuntimeType type)
            {
                _handle = RuntimeTypeHandle.GetFirstIntroducedMethod(type);
                _firstCall = true;
            }
        
            [System.Security.SecuritySafeCritical]  // auto-generated
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

            public RuntimeMethodHandleInternal Current
            { 
                get {
                    return _handle;
                }
            }

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

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern RuntimeMethodHandleInternal GetFirstIntroducedMethod(RuntimeType type);

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void GetNextIntroducedMethod(ref RuntimeMethodHandleInternal method);
        
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal extern static bool GetFields(RuntimeType type, IntPtr* result, int* count);
        
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal extern static Type[] GetInterfaces(RuntimeType type);
        
        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private extern static void GetConstraints(RuntimeTypeHandle handle, ObjectHandleOnStack types);

        [System.Security.SecuritySafeCritical]  // auto-generated
        internal Type[] GetConstraints()
        {
            Type[] types = null;
            GetConstraints(GetNativeHandle(), JitHelpers.GetObjectHandleOnStack(ref types));

            return types;
        }

        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private extern static IntPtr GetGCHandle(RuntimeTypeHandle handle, GCHandleType type);

        [System.Security.SecurityCritical]  // auto-generated
        internal IntPtr GetGCHandle(GCHandleType type)
        {
            return GetGCHandle(GetNativeHandle(), type);
        }

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal extern static int GetNumVirtuals(RuntimeType type); 

        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private extern static void VerifyInterfaceIsImplemented(RuntimeTypeHandle handle, RuntimeTypeHandle interfaceHandle);

        [System.Security.SecuritySafeCritical]  // auto-generated
        internal void VerifyInterfaceIsImplemented(RuntimeTypeHandle interfaceHandle)
        {
            VerifyInterfaceIsImplemented(GetNativeHandle(), interfaceHandle.GetNativeHandle());
        }

        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private extern static int GetInterfaceMethodImplementationSlot(RuntimeTypeHandle handle, RuntimeTypeHandle interfaceHandle, RuntimeMethodHandleInternal interfaceMethodHandle);

        [System.Security.SecuritySafeCritical]  // auto-generated
        internal int GetInterfaceMethodImplementationSlot(RuntimeTypeHandle interfaceHandle, RuntimeMethodHandleInternal interfaceMethodHandle)
        {
            return GetInterfaceMethodImplementationSlot(GetNativeHandle(), interfaceHandle.GetNativeHandle(), interfaceMethodHandle);
        }

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal extern static bool IsComObject(RuntimeType type, bool isGenericCOM); 

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal extern static bool IsContextful(RuntimeType type); 

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal extern static bool IsInterface(RuntimeType type);

        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        [return: MarshalAs(UnmanagedType.Bool)]
        private extern static bool _IsVisible(RuntimeTypeHandle typeHandle);
        
        [System.Security.SecuritySafeCritical]  // auto-generated
        internal static bool IsVisible(RuntimeType type)
        {
            return _IsVisible(new RuntimeTypeHandle(type));
        }

        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsSecurityCritical(RuntimeTypeHandle typeHandle);

        [System.Security.SecuritySafeCritical]  // auto-generated
        internal bool IsSecurityCritical()
        {
            return IsSecurityCritical(GetNativeHandle());
        }


        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsSecuritySafeCritical(RuntimeTypeHandle typeHandle);

        [System.Security.SecuritySafeCritical]  // auto-generated
        internal bool IsSecuritySafeCritical()
        {
            return IsSecuritySafeCritical(GetNativeHandle());
        }

        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsSecurityTransparent(RuntimeTypeHandle typeHandle);

        [System.Security.SecuritySafeCritical]  // auto-generated
        internal bool IsSecurityTransparent()
        {
            return IsSecurityTransparent(GetNativeHandle());
        }

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal extern static bool HasProxyAttribute(RuntimeType type);

        [System.Security.SecuritySafeCritical]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal extern static bool IsValueType(RuntimeType type);

        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private extern static void ConstructName(RuntimeTypeHandle handle, TypeNameFormatFlags formatFlags, StringHandleOnStack retString);

        [System.Security.SecuritySafeCritical]  // auto-generated
        internal string ConstructName(TypeNameFormatFlags formatFlags)
        {
            string name = null;
            ConstructName(GetNativeHandle(), formatFlags, JitHelpers.GetStringHandleOnStack(ref name));
            return name;
        }

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private extern static void* _GetUtf8Name(RuntimeType type);

        [System.Security.SecuritySafeCritical]  // auto-generated
        internal static Utf8String GetUtf8Name(RuntimeType type)
        {
            return new Utf8String(_GetUtf8Name(type));
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal extern static bool CanCastTo(RuntimeType type, RuntimeType target);

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal extern static RuntimeType GetDeclaringType(RuntimeType type);
        
        [System.Security.SecuritySafeCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal extern static IRuntimeMethodInfo GetDeclaringMethod(RuntimeType type);
        
        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private extern static void GetDefaultConstructor(RuntimeTypeHandle handle, ObjectHandleOnStack method);

        [System.Security.SecuritySafeCritical]  // auto-generated
        internal IRuntimeMethodInfo GetDefaultConstructor()       
        {
            IRuntimeMethodInfo ctor = null;
            GetDefaultConstructor(GetNativeHandle(), JitHelpers.GetObjectHandleOnStack(ref ctor));
            return ctor;
        }
       
        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private extern static void GetTypeByName(string name, bool throwOnError, bool ignoreCase, bool reflectionOnly, StackCrawlMarkHandle stackMark, 
            IntPtr pPrivHostBinder,
            bool loadTypeFromPartialName, ObjectHandleOnStack type, ObjectHandleOnStack keepalive);

        // Wrapper function to reduce the need for ifdefs.
        internal static RuntimeType GetTypeByName(string name, bool throwOnError, bool ignoreCase, bool reflectionOnly, ref StackCrawlMark stackMark, bool loadTypeFromPartialName)
        {
            return GetTypeByName(name, throwOnError, ignoreCase, reflectionOnly, ref stackMark, IntPtr.Zero, loadTypeFromPartialName);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        internal static RuntimeType GetTypeByName(string name, bool throwOnError, bool ignoreCase, bool reflectionOnly, ref StackCrawlMark stackMark,
                                                  IntPtr pPrivHostBinder,
                                                  bool loadTypeFromPartialName)
        {
            if (name == null || name.Length == 0)
            {
                if (throwOnError)
                    throw new TypeLoadException(Environment.GetResourceString("Arg_TypeLoadNullStr"));

                return null;
            }

            RuntimeType type = null;

            Object keepAlive = null;
            GetTypeByName(name, throwOnError, ignoreCase, reflectionOnly,
                JitHelpers.GetStackCrawlMarkHandle(ref stackMark),
                pPrivHostBinder,
                loadTypeFromPartialName, JitHelpers.GetObjectHandleOnStack(ref type), JitHelpers.GetObjectHandleOnStack(ref keepAlive));
            GC.KeepAlive(keepAlive);

            return type;
        }

        internal static Type GetTypeByName(string name, ref StackCrawlMark stackMark)
        {
            return GetTypeByName(name, false, false, false, ref stackMark, false);
        }

        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private extern static void GetTypeByNameUsingCARules(string name, RuntimeModule scope, ObjectHandleOnStack type);

        [System.Security.SecuritySafeCritical]  // auto-generated
        internal static RuntimeType GetTypeByNameUsingCARules(string name, RuntimeModule scope)
        {
            if (name == null || name.Length == 0)
                throw new ArgumentException("name"); 
            Contract.EndContractBlock();

            RuntimeType type = null;
            GetTypeByNameUsingCARules(name, scope.GetNativeHandle(), JitHelpers.GetObjectHandleOnStack(ref type));

            return type;
        }

        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        internal extern static void GetInstantiation(RuntimeTypeHandle type, ObjectHandleOnStack types, bool fAsRuntimeTypeArray);

        [System.Security.SecuritySafeCritical]  // auto-generated
        internal RuntimeType[] GetInstantiationInternal()
        {
            RuntimeType[] types = null;
            GetInstantiation(GetNativeHandle(), JitHelpers.GetObjectHandleOnStack(ref types), true);
            return types;
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        internal Type[] GetInstantiationPublic()
        {
            Type[] types = null;
            GetInstantiation(GetNativeHandle(), JitHelpers.GetObjectHandleOnStack(ref types), false);
            return types;
        }

        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private extern static void Instantiate(RuntimeTypeHandle handle, IntPtr* pInst, int numGenericArgs, ObjectHandleOnStack type);

        [System.Security.SecurityCritical]  // auto-generated
        internal RuntimeType Instantiate(Type[] inst)
        {
            // defensive copy to be sure array is not mutated from the outside during processing
            int instCount;
            IntPtr []instHandles = CopyRuntimeTypeHandles(inst, out instCount);

            fixed (IntPtr* pInst = instHandles)
            {
                RuntimeType type = null;
                Instantiate(GetNativeHandle(), pInst, instCount, JitHelpers.GetObjectHandleOnStack(ref type));
                GC.KeepAlive(inst);
                return type;
            }
        }

        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private extern static void MakeArray(RuntimeTypeHandle handle, int rank, ObjectHandleOnStack type);

        [System.Security.SecuritySafeCritical]  // auto-generated
        internal RuntimeType MakeArray(int rank)
        {
            RuntimeType type = null;
            MakeArray(GetNativeHandle(), rank, JitHelpers.GetObjectHandleOnStack(ref type));
            return type;
        }

        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private extern static void MakeSZArray(RuntimeTypeHandle handle, ObjectHandleOnStack type);

        [System.Security.SecuritySafeCritical]  // auto-generated
        internal RuntimeType MakeSZArray()
        {
            RuntimeType type = null;
            MakeSZArray(GetNativeHandle(), JitHelpers.GetObjectHandleOnStack(ref type));
            return type;
        }
        
        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private extern static void MakeByRef(RuntimeTypeHandle handle, ObjectHandleOnStack type);

        [System.Security.SecuritySafeCritical]  // auto-generated
        internal RuntimeType MakeByRef()
        {
            RuntimeType type = null;
            MakeByRef(GetNativeHandle(), JitHelpers.GetObjectHandleOnStack(ref type));
            return type;
        }
       
        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private extern static void MakePointer(RuntimeTypeHandle handle, ObjectHandleOnStack type);

        [System.Security.SecurityCritical]  // auto-generated
        internal RuntimeType MakePointer()
        {
            RuntimeType type = null;
            MakePointer(GetNativeHandle(), JitHelpers.GetObjectHandleOnStack(ref type));
            return type;
        }

        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        internal extern static bool IsCollectible(RuntimeTypeHandle handle);
        
#if FEATURE_CORECLR
        [System.Security.SecuritySafeCritical] // auto-generated
#endif
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal extern static bool HasInstantiation(RuntimeType type);

        internal bool HasInstantiation()
        {
            return HasInstantiation(GetTypeChecked());
        }

        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private extern static void GetGenericTypeDefinition(RuntimeTypeHandle type, ObjectHandleOnStack retType);

        [System.Security.SecuritySafeCritical]  // auto-generated
        internal static RuntimeType GetGenericTypeDefinition(RuntimeType type)
        {
            RuntimeType retType = type;

            if (HasInstantiation(retType) && !IsGenericTypeDefinition(retType))
                GetGenericTypeDefinition(retType.GetTypeHandleInternal(), JitHelpers.GetObjectHandleOnStack(ref retType));

            return retType;
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal extern static bool IsGenericTypeDefinition(RuntimeType type);
       
        [System.Security.SecuritySafeCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal extern static bool IsGenericVariable(RuntimeType type);

        internal bool IsGenericVariable()
        {
            return IsGenericVariable(GetTypeChecked());
        }

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private extern static int GetGenericVariableIndex(RuntimeType type);

        [System.Security.SecuritySafeCritical]  // auto-generated
        internal int GetGenericVariableIndex()
        {
            RuntimeType type = GetTypeChecked();

            if (!IsGenericVariable(type))
                throw new InvalidOperationException(Environment.GetResourceString("Arg_NotGenericParameter"));

            return GetGenericVariableIndex(type);
        }

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal extern static bool ContainsGenericVariables(RuntimeType handle);

        [System.Security.SecuritySafeCritical]  // auto-generated
        internal bool ContainsGenericVariables()
        {
            return ContainsGenericVariables(GetTypeChecked());
        }
        
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private extern static bool SatisfiesConstraints(RuntimeType paramType, IntPtr *pTypeContext, int typeContextLength, IntPtr *pMethodContext, int methodContextLength, RuntimeType toType);

        [System.Security.SecurityCritical]
        internal static bool SatisfiesConstraints(RuntimeType paramType, RuntimeType[] typeContext, RuntimeType[] methodContext, RuntimeType toType)
        {
            int typeContextLength;
            int methodContextLength;
            IntPtr[] typeContextHandles = CopyRuntimeTypeHandles(typeContext, out typeContextLength);
            IntPtr[] methodContextHandles = CopyRuntimeTypeHandles(methodContext, out methodContextLength);
            
            fixed (IntPtr *pTypeContextHandles = typeContextHandles, pMethodContextHandles = methodContextHandles)
            {
                bool result = SatisfiesConstraints(paramType, pTypeContextHandles, typeContextLength, pMethodContextHandles, methodContextLength, toType);

                GC.KeepAlive(typeContext);
                GC.KeepAlive(methodContext);

                return result;
            }
        }

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private extern static IntPtr _GetMetadataImport(RuntimeType type);

        [System.Security.SecurityCritical]  // auto-generated
        internal static MetadataImport GetMetadataImport(RuntimeType type)
        {
            return new MetadataImport(_GetMetadataImport(type), type);
        }
        
        [System.Security.SecurityCritical]  // auto-generated
        private RuntimeTypeHandle(SerializationInfo info, StreamingContext context)
        {
            if(info == null) 
                throw new ArgumentNullException("info");
            Contract.EndContractBlock();

            RuntimeType m = (RuntimeType)info.GetValue("TypeObj", typeof(RuntimeType));

            m_type = m;

            if (m_type == null)
                throw new SerializationException(Environment.GetResourceString("Serialization_InsufficientState"));
        }

        [System.Security.SecurityCritical]
        public void GetObjectData(SerializationInfo info, StreamingContext context) 
        {
            if(info == null) 
                throw new ArgumentNullException("info");
            Contract.EndContractBlock();

            if (m_type == null)
                throw new SerializationException(Environment.GetResourceString("Serialization_InvalidFieldState")); 

            info.AddValue("TypeObj", m_type, typeof(RuntimeType));
        }

#if !FEATURE_CORECLR
        [System.Security.SecuritySafeCritical]
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern bool IsEquivalentTo(RuntimeType rtType1, RuntimeType rtType2);

        [System.Security.SecuritySafeCritical]
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern bool IsEquivalentType(RuntimeType type);
#endif // FEATURE_CORECLR
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
        internal static RuntimeMethodHandleInternal EmptyHandle
        {
            get
            {
                return new RuntimeMethodHandleInternal();
            }
        }

        internal bool IsNullHandle()
        {
            return m_handle.IsNull();
        }

        internal IntPtr Value
        {
            [SecurityCritical]
            get
            {
                return m_handle;
            }
        }

        [SecurityCritical]
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

        [SecurityCritical]
        public RuntimeMethodInfoStub(IntPtr methodHandleValue, object keepalive)
        {
            m_keepalive = keepalive;
            m_value = new RuntimeMethodHandleInternal(methodHandleValue);
        }

        object m_keepalive;

        // These unused variables are used to ensure that this class has the same layout as RuntimeMethodInfo
#pragma warning disable 169
        object m_a;
        object m_b;
        object m_c;
        object m_d;
        object m_e;
        object m_f;
        object m_g;
#if FEATURE_REMOTING
        object m_h;
#endif
#pragma warning restore 169
        public RuntimeMethodHandleInternal m_value;

        RuntimeMethodHandleInternal IRuntimeMethodInfo.Value
        {
            get
            {
                return m_value;
            }
        }
    }

    internal interface IRuntimeMethodInfo
    {
        RuntimeMethodHandleInternal Value
        {
            get;
        }
    }

    [Serializable]
    [System.Runtime.InteropServices.ComVisible(true)]
    public unsafe struct RuntimeMethodHandle : ISerializable
    {
        // Returns handle for interop with EE. The handle is guaranteed to be non-null.
        internal static IRuntimeMethodInfo EnsureNonNullMethodInfo(IRuntimeMethodInfo method)
        {
            if (method == null)
                throw new ArgumentNullException(null, Environment.GetResourceString("Arg_InvalidHandle"));
            return method;
        }

        internal static RuntimeMethodHandle EmptyHandle
        {
            get { return new RuntimeMethodHandle(); }
        }

        private IRuntimeMethodInfo m_value;
        
        internal RuntimeMethodHandle(IRuntimeMethodInfo method)
        {
            m_value = method;
        }

        internal IRuntimeMethodInfo GetMethodInfo()
        {
            return m_value;
        }

        // Used by EE
        [SecurityCritical]
        private static IntPtr GetValueInternal(RuntimeMethodHandle rmh)
        {
            return rmh.Value;
        }
        
        // ISerializable interface
        [System.Security.SecurityCritical]  // auto-generated
        private RuntimeMethodHandle(SerializationInfo info, StreamingContext context)
        {
            if(info == null)
                throw new ArgumentNullException("info");
            Contract.EndContractBlock();
            
            MethodBase m =(MethodBase)info.GetValue("MethodObj", typeof(MethodBase));

            m_value = m.MethodHandle.m_value;

            if (m_value == null)
                throw new SerializationException(Environment.GetResourceString("Serialization_InsufficientState"));
        }

        [System.Security.SecurityCritical]
        public void GetObjectData(SerializationInfo info, StreamingContext context) 
        {
            if (info == null) 
                throw new ArgumentNullException("info");
            Contract.EndContractBlock();

            if (m_value == null)
                throw new SerializationException(Environment.GetResourceString("Serialization_InvalidFieldState"));
            
            // This is either a RuntimeMethodInfo or a RuntimeConstructorInfo
            MethodBase methodInfo = RuntimeType.GetMethodBase(m_value);

            info.AddValue("MethodObj", methodInfo, typeof(MethodBase));
        }

        public IntPtr Value
        {
            [SecurityCritical]
            get
            {
                return m_value != null ? m_value.Value.Value : IntPtr.Zero;
            }
        }

        [SecuritySafeCritical]
        public override int GetHashCode()
        {
            return ValueType.GetHashCodeOfPtr(Value);
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        [SecuritySafeCritical]
        public override bool Equals(object obj)
        {
            if (!(obj is RuntimeMethodHandle))
                return false;

            RuntimeMethodHandle handle = (RuntimeMethodHandle)obj;

            return handle.Value == Value;
        }

        public static bool operator ==(RuntimeMethodHandle left, RuntimeMethodHandle right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(RuntimeMethodHandle left, RuntimeMethodHandle right)
        {
            return !left.Equals(right);
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        [SecuritySafeCritical]
        public bool Equals(RuntimeMethodHandle handle)
        {
            return handle.Value == Value;
        }

        [Pure]
        internal bool IsNullHandle() 
        { 
            return m_value == null; 
        }

        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        internal extern static IntPtr GetFunctionPointer(RuntimeMethodHandleInternal handle);

        [System.Security.SecurityCritical]  // auto-generated
        public IntPtr GetFunctionPointer()
        {
            IntPtr ptr = GetFunctionPointer(EnsureNonNullMethodInfo(m_value).Value);
            GC.KeepAlive(m_value);
            return ptr;
        }

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal unsafe extern static void CheckLinktimeDemands(IRuntimeMethodInfo method, RuntimeModule module, bool isDecoratedTargetSecurityTransparent);

        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        internal extern static bool IsCAVisibleFromDecoratedType(
            RuntimeTypeHandle attrTypeHandle,
            IRuntimeMethodInfo attrCtor,
            RuntimeTypeHandle sourceTypeHandle,
            RuntimeModule sourceModule);

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern IRuntimeMethodInfo _GetCurrentMethod(ref StackCrawlMark stackMark);
        [System.Security.SecuritySafeCritical]  // auto-generated
        internal static IRuntimeMethodInfo GetCurrentMethod(ref StackCrawlMark stackMark)
        {
            return _GetCurrentMethod(ref stackMark);
        }
        
        [Pure]
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern MethodAttributes GetAttributes(RuntimeMethodHandleInternal method);

        [System.Security.SecurityCritical]  // auto-generated
        internal static MethodAttributes GetAttributes(IRuntimeMethodInfo method)
        {
            MethodAttributes retVal = RuntimeMethodHandle.GetAttributes(method.Value);
            GC.KeepAlive(method);
            return retVal;
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern MethodImplAttributes GetImplAttributes(IRuntimeMethodInfo method);
        
        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private extern static void ConstructInstantiation(IRuntimeMethodInfo method, TypeNameFormatFlags format, StringHandleOnStack retString);

        [System.Security.SecuritySafeCritical]  // auto-generated
        internal static string ConstructInstantiation(IRuntimeMethodInfo method, TypeNameFormatFlags format)
        {
            string name = null;
            ConstructInstantiation(EnsureNonNullMethodInfo(method), format, JitHelpers.GetStringHandleOnStack(ref name));
            return name;
        }

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal extern static RuntimeType GetDeclaringType(RuntimeMethodHandleInternal method);

        [System.Security.SecuritySafeCritical]  // auto-generated
        internal static RuntimeType GetDeclaringType(IRuntimeMethodInfo method)
        {
            RuntimeType type = RuntimeMethodHandle.GetDeclaringType(method.Value);
            GC.KeepAlive(method);
            return type;
        }

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal extern static int GetSlot(RuntimeMethodHandleInternal method);

        [System.Security.SecurityCritical]  // auto-generated
        internal static int GetSlot(IRuntimeMethodInfo method)
        {
            Contract.Requires(method != null);

            int slot = RuntimeMethodHandle.GetSlot(method.Value);
            GC.KeepAlive(method);
            return slot;
        }
        
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal extern static int GetMethodDef(IRuntimeMethodInfo method);

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal extern static string GetName(RuntimeMethodHandleInternal method);

        [System.Security.SecurityCritical]  // auto-generated
        internal static string GetName(IRuntimeMethodInfo method)
        {
            string name = RuntimeMethodHandle.GetName(method.Value);
            GC.KeepAlive(method);
            return name;
        }

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private extern static void* _GetUtf8Name(RuntimeMethodHandleInternal method);

        [System.Security.SecurityCritical]  // auto-generated
        internal static Utf8String GetUtf8Name(RuntimeMethodHandleInternal method)
        {
            return new Utf8String(_GetUtf8Name(method));
        }

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern bool MatchesNameHash(RuntimeMethodHandleInternal method, uint hash);

        [System.Security.SecuritySafeCritical]  // auto-generated
        [DebuggerStepThroughAttribute]
        [Diagnostics.DebuggerHidden]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal extern static object InvokeMethod(object target, object[] arguments, Signature sig, bool constructor);

#region Private Invocation Helpers
        [System.Security.SecurityCritical]  // auto-generated
        internal static INVOCATION_FLAGS GetSecurityFlags(IRuntimeMethodInfo handle)
        {
            return (INVOCATION_FLAGS)RuntimeMethodHandle.GetSpecialSecurityFlags(handle);
        }

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        static extern internal uint GetSpecialSecurityFlags(IRuntimeMethodInfo method);

#if !FEATURE_CORECLR
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        static extern internal void PerformSecurityCheck(Object obj, RuntimeMethodHandleInternal method, RuntimeType parent, uint invocationFlags);

        [System.Security.SecurityCritical]
        static internal void PerformSecurityCheck(Object obj, IRuntimeMethodInfo method, RuntimeType parent, uint invocationFlags)
        {
            RuntimeMethodHandle.PerformSecurityCheck(obj, method.Value, parent, invocationFlags);
            GC.KeepAlive(method);
            return;
        }
#endif //!FEATURE_CORECLR
#endregion

        [System.Security.SecuritySafeCritical]  // auto-generated
        [DebuggerStepThroughAttribute]
        [Diagnostics.DebuggerHidden]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]        
        internal extern static void SerializationInvoke(IRuntimeMethodInfo method,
            Object target, SerializationInfo info, ref StreamingContext context);

        // This returns true if the token is SecurityTransparent: 
        // just the token - does not consider including module/type etc.
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern bool _IsTokenSecurityTransparent(RuntimeModule module, int metaDataToken);
        
#if FEATURE_CORECLR
        [System.Security.SecuritySafeCritical] // auto-generated
#else
        [System.Security.SecurityCritical]
#endif
        internal static bool IsTokenSecurityTransparent(Module module, int metaDataToken)
        {
            return _IsTokenSecurityTransparent(module.ModuleHandle.GetRuntimeModule(), metaDataToken);
        }

        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool _IsSecurityCritical(IRuntimeMethodInfo method);

        [System.Security.SecuritySafeCritical]  // auto-generated
        internal static bool IsSecurityCritical(IRuntimeMethodInfo method)
        {
            return _IsSecurityCritical(method);
        }

        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool _IsSecuritySafeCritical(IRuntimeMethodInfo method);

        [System.Security.SecuritySafeCritical]  // auto-generated
        internal static bool IsSecuritySafeCritical(IRuntimeMethodInfo method)
        {
            return _IsSecuritySafeCritical(method);
        }

        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool _IsSecurityTransparent(IRuntimeMethodInfo method);

        [System.Security.SecuritySafeCritical]  // auto-generated
        internal static bool IsSecurityTransparent(IRuntimeMethodInfo method)
        {
            return _IsSecurityTransparent(method);
        }

        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
		[SuppressUnmanagedCodeSecurity]
        private extern static void GetMethodInstantiation(RuntimeMethodHandleInternal method, ObjectHandleOnStack types, bool fAsRuntimeTypeArray);

        [System.Security.SecuritySafeCritical]  // auto-generated
        internal static RuntimeType[] GetMethodInstantiationInternal(IRuntimeMethodInfo method)
        {
            RuntimeType[] types = null;
            GetMethodInstantiation(EnsureNonNullMethodInfo(method).Value, JitHelpers.GetObjectHandleOnStack(ref types), true);
            GC.KeepAlive(method);
            return types;
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        internal static RuntimeType[] GetMethodInstantiationInternal(RuntimeMethodHandleInternal method)
        {
            RuntimeType[] types = null;
            GetMethodInstantiation(method, JitHelpers.GetObjectHandleOnStack(ref types), true);
            return types;
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        internal static Type[] GetMethodInstantiationPublic(IRuntimeMethodInfo method)
        {
            RuntimeType[] types = null;
            GetMethodInstantiation(EnsureNonNullMethodInfo(method).Value, JitHelpers.GetObjectHandleOnStack(ref types), false);
            GC.KeepAlive(method);
            return types;
        }

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal extern static bool HasMethodInstantiation(RuntimeMethodHandleInternal method);

        [System.Security.SecuritySafeCritical]  // auto-generated
        internal static bool HasMethodInstantiation(IRuntimeMethodInfo method)
        {
            bool fRet = RuntimeMethodHandle.HasMethodInstantiation(method.Value);
            GC.KeepAlive(method);
            return fRet;
        }

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal extern static RuntimeMethodHandleInternal GetStubIfNeeded(RuntimeMethodHandleInternal method, RuntimeType declaringType, RuntimeType[] methodInstantiation);
        
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal extern static RuntimeMethodHandleInternal GetMethodFromCanonical(RuntimeMethodHandleInternal method, RuntimeType declaringType);
        
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal extern static bool IsGenericMethodDefinition(RuntimeMethodHandleInternal method);

        [System.Security.SecuritySafeCritical]  // auto-generated
        internal static bool IsGenericMethodDefinition(IRuntimeMethodInfo method)
        {
            bool fRet = RuntimeMethodHandle.IsGenericMethodDefinition(method.Value);
            GC.KeepAlive(method);
            return fRet;
        }


        [System.Security.SecuritySafeCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal extern static bool IsTypicalMethodDefinition(IRuntimeMethodInfo method);

        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private extern static void GetTypicalMethodDefinition(IRuntimeMethodInfo method, ObjectHandleOnStack outMethod);
 
        [System.Security.SecuritySafeCritical]  // auto-generated
        internal static IRuntimeMethodInfo GetTypicalMethodDefinition(IRuntimeMethodInfo method)
        {
            if (!IsTypicalMethodDefinition(method))
                GetTypicalMethodDefinition(method, JitHelpers.GetObjectHandleOnStack(ref method));

            return method;
        }

        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private extern static void StripMethodInstantiation(IRuntimeMethodInfo method, ObjectHandleOnStack outMethod);
 
        [System.Security.SecuritySafeCritical]  // auto-generated
        internal static IRuntimeMethodInfo StripMethodInstantiation(IRuntimeMethodInfo method)
        {
            IRuntimeMethodInfo strippedMethod = method;

            StripMethodInstantiation(method, JitHelpers.GetObjectHandleOnStack(ref strippedMethod));

            return strippedMethod;
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal extern static bool IsDynamicMethod(RuntimeMethodHandleInternal method);

        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        internal extern static void Destroy(RuntimeMethodHandleInternal method);

        [System.Security.SecuritySafeCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal extern static Resolver GetResolver(RuntimeMethodHandleInternal method);

        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private static extern void GetCallerType(StackCrawlMarkHandle stackMark, ObjectHandleOnStack retType);
    
        [System.Security.SecuritySafeCritical]  // auto-generated
        internal static RuntimeType GetCallerType(ref StackCrawlMark stackMark)
        {
            RuntimeType type = null;
            GetCallerType(JitHelpers.GetStackCrawlMarkHandle(ref stackMark), JitHelpers.GetObjectHandleOnStack(ref type));
            return type;
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        [System.Security.SecuritySafeCritical]  // auto-generated
        internal extern static MethodBody GetMethodBody(IRuntimeMethodInfo method, RuntimeType declaringType);

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal extern static bool IsConstructor(RuntimeMethodHandleInternal method);

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal extern static LoaderAllocator GetLoaderAllocator(RuntimeMethodHandleInternal method);
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
        internal static RuntimeFieldHandleInternal EmptyHandle
        {
            get
            {
                return new RuntimeFieldHandleInternal();
            }
        }

        internal bool IsNullHandle()
        {
            return m_handle.IsNull();
        }

        internal IntPtr Value
        {
            [SecurityCritical]
            get
            {
                return m_handle;
            }
        }

        [SecurityCritical]
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
        [SecuritySafeCritical]
        public RuntimeFieldInfoStub(IntPtr methodHandleValue, object keepalive)
        {
            m_keepalive = keepalive;
            m_fieldHandle = new RuntimeFieldHandleInternal(methodHandleValue);
        }

        // These unused variables are used to ensure that this class has the same layout as RuntimeFieldInfo
#pragma warning disable 169
        object m_keepalive;
        object m_c;
        object m_d;
        int m_b;
        object m_e;
#if FEATURE_REMOTING
        object m_f;
#endif
        RuntimeFieldHandleInternal m_fieldHandle;
#pragma warning restore 169

        RuntimeFieldHandleInternal IRuntimeFieldInfo.Value
        {
            get
            {
                return m_fieldHandle;
            }
        }
    }

    [Serializable]
    [System.Runtime.InteropServices.ComVisible(true)]
    public unsafe struct RuntimeFieldHandle : ISerializable
    {
        // Returns handle for interop with EE. The handle is guaranteed to be non-null.
        internal RuntimeFieldHandle GetNativeHandle()
        {
            // Create local copy to avoid a race condition
            IRuntimeFieldInfo field = m_ptr;
            if (field == null)
                throw new ArgumentNullException(null, Environment.GetResourceString("Arg_InvalidHandle"));
            return new RuntimeFieldHandle(field);
        }

        private IRuntimeFieldInfo m_ptr;

        internal RuntimeFieldHandle(IRuntimeFieldInfo fieldInfo)
        {
            m_ptr = fieldInfo;
        }

        internal IRuntimeFieldInfo GetRuntimeFieldInfo()
        {
            return m_ptr;
        }

        public IntPtr Value
        {
            [SecurityCritical]
            get
            {
                return m_ptr != null ? m_ptr.Value.Value : IntPtr.Zero;
            }
        }

        internal bool IsNullHandle() 
        {
            return m_ptr == null; 
        }

        [SecuritySafeCritical]
        public override int GetHashCode()
        {
            return ValueType.GetHashCodeOfPtr(Value);
        }
        
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        [SecuritySafeCritical]
        public override bool Equals(object obj)
        {
            if (!(obj is RuntimeFieldHandle))
                return false;

            RuntimeFieldHandle handle = (RuntimeFieldHandle)obj;

            return handle.Value == Value;
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        [SecuritySafeCritical]
        public unsafe bool Equals(RuntimeFieldHandle handle)
        {
            return handle.Value == Value;
        }

        public static bool operator ==(RuntimeFieldHandle left, RuntimeFieldHandle right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(RuntimeFieldHandle left, RuntimeFieldHandle right)
        {
            return !left.Equals(right);
        }

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]        
        internal static extern String GetName(RtFieldInfo field); 

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]        
        private static extern unsafe void* _GetUtf8Name(RuntimeFieldHandleInternal field); 

        [System.Security.SecuritySafeCritical]  // auto-generated
        internal static unsafe Utf8String GetUtf8Name(RuntimeFieldHandleInternal field) { return new Utf8String(_GetUtf8Name(field)); }

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern bool MatchesNameHash(RuntimeFieldHandleInternal handle, uint hash);

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern FieldAttributes GetAttributes(RuntimeFieldHandleInternal field); 
        
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern RuntimeType GetApproxDeclaringType(RuntimeFieldHandleInternal field);

        [System.Security.SecurityCritical]  // auto-generated
        internal static RuntimeType GetApproxDeclaringType(IRuntimeFieldInfo field)
        {
            RuntimeType type = GetApproxDeclaringType(field.Value);
            GC.KeepAlive(field);
            return type;
        }

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]        
        internal static extern int GetToken(RtFieldInfo field); 

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]        
        internal static extern Object GetValue(RtFieldInfo field, Object instance, RuntimeType fieldType, RuntimeType declaringType, ref bool domainInitialized);

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]        
        internal static extern Object GetValueDirect(RtFieldInfo field, RuntimeType fieldType, void *pTypedRef, RuntimeType contextType);
        
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]        
        internal static extern void SetValue(RtFieldInfo field, Object obj, Object value, RuntimeType fieldType, FieldAttributes fieldAttr, RuntimeType declaringType, ref bool domainInitialized);

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void SetValueDirect(RtFieldInfo field, RuntimeType fieldType, void* pTypedRef, Object value, RuntimeType contextType);

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern RuntimeFieldHandleInternal GetStaticFieldForGenericType(RuntimeFieldHandleInternal field, RuntimeType declaringType);

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern bool AcquiresContextFromThis(RuntimeFieldHandleInternal field);

        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsSecurityCritical(RuntimeFieldHandle fieldHandle);

        [System.Security.SecuritySafeCritical]  // auto-generated
        internal bool IsSecurityCritical()
        {
            return IsSecurityCritical(GetNativeHandle());
        }

        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsSecuritySafeCritical(RuntimeFieldHandle fieldHandle);

        [System.Security.SecuritySafeCritical]  // auto-generated
        internal bool IsSecuritySafeCritical()
        {
            return IsSecuritySafeCritical(GetNativeHandle());
        }

        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsSecurityTransparent(RuntimeFieldHandle fieldHandle);

        [System.Security.SecuritySafeCritical]  // auto-generated
        internal bool IsSecurityTransparent()
        {
            return IsSecurityTransparent(GetNativeHandle());
        }

        [SecurityCritical]
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        internal static extern void CheckAttributeAccess(RuntimeFieldHandle fieldHandle, RuntimeModule decoratedTarget);

        // ISerializable interface
        [System.Security.SecurityCritical]  // auto-generated
        private RuntimeFieldHandle(SerializationInfo info, StreamingContext context)
        {
            if(info==null)
                throw new ArgumentNullException("info");
            Contract.EndContractBlock();
            
            FieldInfo f =(RuntimeFieldInfo) info.GetValue("FieldObj", typeof(RuntimeFieldInfo));
            
            if( f == null)
                throw new SerializationException(Environment.GetResourceString("Serialization_InsufficientState"));

            m_ptr = f.FieldHandle.m_ptr;

            if (m_ptr == null)
                throw new SerializationException(Environment.GetResourceString("Serialization_InsufficientState"));
        }

        [System.Security.SecurityCritical]
        public void GetObjectData(SerializationInfo info, StreamingContext context) 
        {
            if (info == null) 
                throw new ArgumentNullException("info");
            Contract.EndContractBlock();

            if (m_ptr == null)
                throw new SerializationException(Environment.GetResourceString("Serialization_InvalidFieldState"));

            RuntimeFieldInfo fldInfo = (RuntimeFieldInfo)RuntimeType.GetFieldInfo(this.GetRuntimeFieldInfo()); 
            
            info.AddValue("FieldObj",fldInfo, typeof(RuntimeFieldInfo));
        }
    }

[System.Runtime.InteropServices.ComVisible(true)]
    public unsafe struct ModuleHandle
    {
        // Returns handle for interop with EE. The handle is guaranteed to be non-null.
#region Public Static Members
        public static readonly ModuleHandle EmptyHandle = GetEmptyMH();
#endregion

        unsafe static private ModuleHandle GetEmptyMH()
        {
            return new ModuleHandle();
        }

#region Private Data Members
        private RuntimeModule m_ptr;
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

        internal bool IsNullHandle() 
        { 
            return m_ptr == null; 
        }
        
        public override int GetHashCode()
        {           
            return m_ptr != null ? m_ptr.GetHashCode() : 0;
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        public override bool Equals(object obj)
        {
            if (!(obj is ModuleHandle))
                return false;

            ModuleHandle handle = (ModuleHandle)obj;

            return handle.m_ptr == m_ptr;
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        public unsafe bool Equals(ModuleHandle handle)
        {
            return handle.m_ptr == m_ptr;
        }

        public static bool operator ==(ModuleHandle left, ModuleHandle right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ModuleHandle left, ModuleHandle right)
        {
            return !left.Equals(right);
        }

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern IRuntimeMethodInfo GetDynamicMethod(DynamicMethod method, RuntimeModule module, string name, byte[] sig, Resolver resolver);

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern int GetToken(RuntimeModule module);

        private static void ValidateModulePointer(RuntimeModule module)
        {
            // Make sure we have a valid Module to resolve against.
            if (module == null)
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_NullModuleHandle"));
        }

        // SQL-CLR LKG9 Compiler dependency
        public RuntimeTypeHandle GetRuntimeTypeHandleFromMetadataToken(int typeToken) { return ResolveTypeHandle(typeToken); }
        public RuntimeTypeHandle ResolveTypeHandle(int typeToken) 
        {
            return new RuntimeTypeHandle(ResolveTypeHandleInternal(GetRuntimeModule(), typeToken, null, null));
        }
        public RuntimeTypeHandle ResolveTypeHandle(int typeToken, RuntimeTypeHandle[] typeInstantiationContext, RuntimeTypeHandle[] methodInstantiationContext) 
        {
            return new RuntimeTypeHandle(ModuleHandle.ResolveTypeHandleInternal(GetRuntimeModule(), typeToken, typeInstantiationContext, methodInstantiationContext));
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        internal static RuntimeType ResolveTypeHandleInternal(RuntimeModule module, int typeToken, RuntimeTypeHandle[] typeInstantiationContext, RuntimeTypeHandle[] methodInstantiationContext)
        {
            ValidateModulePointer(module);
            if (!ModuleHandle.GetMetadataImport(module).IsValidToken(typeToken))
                throw new ArgumentOutOfRangeException("metadataToken",
                    Environment.GetResourceString("Argument_InvalidToken", typeToken, new ModuleHandle(module)));
            
            int typeInstCount, methodInstCount;
            IntPtr[] typeInstantiationContextHandles = RuntimeTypeHandle.CopyRuntimeTypeHandles(typeInstantiationContext, out typeInstCount);
            IntPtr[] methodInstantiationContextHandles = RuntimeTypeHandle.CopyRuntimeTypeHandles(methodInstantiationContext, out methodInstCount);

            fixed (IntPtr* typeInstArgs = typeInstantiationContextHandles, methodInstArgs = methodInstantiationContextHandles)
            {
                RuntimeType type = null;
                ResolveType(module, typeToken, typeInstArgs, typeInstCount, methodInstArgs, methodInstCount, JitHelpers.GetObjectHandleOnStack(ref type));
                GC.KeepAlive(typeInstantiationContext);
                GC.KeepAlive(methodInstantiationContext);
                return type;
            }
        }

        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private extern static void ResolveType(RuntimeModule module,
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
        public RuntimeMethodHandle ResolveMethodHandle(int methodToken, RuntimeTypeHandle[] typeInstantiationContext, RuntimeTypeHandle[] methodInstantiationContext)
        {
            return new RuntimeMethodHandle(ResolveMethodHandleInternal(GetRuntimeModule(), methodToken, typeInstantiationContext, methodInstantiationContext));
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        internal static IRuntimeMethodInfo ResolveMethodHandleInternal(RuntimeModule module, int methodToken, RuntimeTypeHandle[] typeInstantiationContext, RuntimeTypeHandle[] methodInstantiationContext)
        {
            int typeInstCount, methodInstCount;

            IntPtr[] typeInstantiationContextHandles = RuntimeTypeHandle.CopyRuntimeTypeHandles(typeInstantiationContext, out typeInstCount);
            IntPtr[] methodInstantiationContextHandles = RuntimeTypeHandle.CopyRuntimeTypeHandles(methodInstantiationContext, out methodInstCount);

            RuntimeMethodHandleInternal handle = ResolveMethodHandleInternalCore(module, methodToken, typeInstantiationContextHandles, typeInstCount, methodInstantiationContextHandles, methodInstCount);
            IRuntimeMethodInfo retVal = new RuntimeMethodInfoStub(handle, RuntimeMethodHandle.GetLoaderAllocator(handle));
            GC.KeepAlive(typeInstantiationContext);
            GC.KeepAlive(methodInstantiationContext);
            return retVal;
        }

        [System.Security.SecurityCritical]  // auto-generated
        internal static RuntimeMethodHandleInternal ResolveMethodHandleInternalCore(RuntimeModule module, int methodToken, IntPtr[] typeInstantiationContext, int typeInstCount, IntPtr[] methodInstantiationContext, int methodInstCount)
        {
            ValidateModulePointer(module);
            if (!ModuleHandle.GetMetadataImport(module.GetNativeHandle()).IsValidToken(methodToken))
                throw new ArgumentOutOfRangeException("metadataToken",
                    Environment.GetResourceString("Argument_InvalidToken", methodToken, new ModuleHandle(module)));

            fixed (IntPtr* typeInstArgs = typeInstantiationContext, methodInstArgs = methodInstantiationContext)
            {
                return ResolveMethod(module.GetNativeHandle(), methodToken, typeInstArgs, typeInstCount, methodInstArgs, methodInstCount);
            }
        }

        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private extern static RuntimeMethodHandleInternal ResolveMethod(RuntimeModule module,
                                                        int methodToken,
                                                        IntPtr* typeInstArgs, 
                                                        int typeInstCount,
                                                        IntPtr* methodInstArgs,
                                                        int methodInstCount);

        // SQL-CLR LKG9 Compiler dependency
        public RuntimeFieldHandle GetRuntimeFieldHandleFromMetadataToken(int fieldToken) { return ResolveFieldHandle(fieldToken); }
        public RuntimeFieldHandle ResolveFieldHandle(int fieldToken) { return new RuntimeFieldHandle(ResolveFieldHandleInternal(GetRuntimeModule(), fieldToken, null, null)); }
        public RuntimeFieldHandle ResolveFieldHandle(int fieldToken, RuntimeTypeHandle[] typeInstantiationContext, RuntimeTypeHandle[] methodInstantiationContext)
            { return new RuntimeFieldHandle(ResolveFieldHandleInternal(GetRuntimeModule(), fieldToken, typeInstantiationContext, methodInstantiationContext)); }

        [System.Security.SecuritySafeCritical]  // auto-generated
        internal static IRuntimeFieldInfo ResolveFieldHandleInternal(RuntimeModule module, int fieldToken, RuntimeTypeHandle[] typeInstantiationContext, RuntimeTypeHandle[] methodInstantiationContext)
        {
            ValidateModulePointer(module);
            if (!ModuleHandle.GetMetadataImport(module.GetNativeHandle()).IsValidToken(fieldToken))
                throw new ArgumentOutOfRangeException("metadataToken",
                    Environment.GetResourceString("Argument_InvalidToken", fieldToken, new ModuleHandle(module)));
            
            // defensive copy to be sure array is not mutated from the outside during processing
            int typeInstCount, methodInstCount;
            IntPtr [] typeInstantiationContextHandles = RuntimeTypeHandle.CopyRuntimeTypeHandles(typeInstantiationContext, out typeInstCount);
            IntPtr [] methodInstantiationContextHandles = RuntimeTypeHandle.CopyRuntimeTypeHandles(methodInstantiationContext, out methodInstCount);

            fixed (IntPtr* typeInstArgs = typeInstantiationContextHandles, methodInstArgs = methodInstantiationContextHandles)
            {
                IRuntimeFieldInfo field = null;
                ResolveField(module.GetNativeHandle(), fieldToken, typeInstArgs, typeInstCount, methodInstArgs, methodInstCount, JitHelpers.GetObjectHandleOnStack(ref field));
                GC.KeepAlive(typeInstantiationContext);
                GC.KeepAlive(methodInstantiationContext);
                return field;
            }
        }

        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private extern static void ResolveField(RuntimeModule module,
                                                      int fieldToken,
                                                      IntPtr* typeInstArgs, 
                                                      int typeInstCount,
                                                      IntPtr* methodInstArgs,
                                                      int methodInstCount,
                                                      ObjectHandleOnStack retField);

        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private extern static bool _ContainsPropertyMatchingHash(RuntimeModule module, int propertyToken, uint hash);

        [System.Security.SecurityCritical]  // auto-generated
        internal static bool ContainsPropertyMatchingHash(RuntimeModule module, int propertyToken, uint hash)
        {
            return _ContainsPropertyMatchingHash(module.GetNativeHandle(), propertyToken, hash);
        }

        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private extern static void GetAssembly(RuntimeModule handle, ObjectHandleOnStack retAssembly);

        [System.Security.SecuritySafeCritical]  // auto-generated
        internal static RuntimeAssembly GetAssembly(RuntimeModule module)
        {
            RuntimeAssembly retAssembly = null;
            GetAssembly(module.GetNativeHandle(), JitHelpers.GetObjectHandleOnStack(ref retAssembly));
            return retAssembly;
        }

        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        internal extern static void GetModuleType(RuntimeModule handle, ObjectHandleOnStack type);

        [System.Security.SecuritySafeCritical]  // auto-generated
        internal static RuntimeType GetModuleType(RuntimeModule module)
        {
            RuntimeType type = null;
            GetModuleType(module.GetNativeHandle(), JitHelpers.GetObjectHandleOnStack(ref type));
            return type;
        }
 
        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private extern static void GetPEKind(RuntimeModule handle, out int peKind, out int machine);
   
        // making this internal, used by Module.GetPEKind
        [System.Security.SecuritySafeCritical]  // auto-generated
        internal static void GetPEKind(RuntimeModule module, out PortableExecutableKinds peKind, out ImageFileMachine machine)
        {
            int lKind, lMachine;
            GetPEKind(module.GetNativeHandle(), out lKind, out lMachine);
            peKind = (PortableExecutableKinds)lKind;
            machine = (ImageFileMachine)lMachine;
        }
   
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]        
        internal extern static int GetMDStreamVersion(RuntimeModule module);

        public int MDStreamVersion
        {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get { return GetMDStreamVersion(GetRuntimeModule().GetNativeHandle()); }
        }
        
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private extern static IntPtr _GetMetadataImport(RuntimeModule module);

        [System.Security.SecurityCritical]  // auto-generated
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
            Generics            = 0x10,
            HasThis             = 0x20,
            ExplicitThis        = 0x40,
            CallConvMask        = 0x0F,
            Default             = 0x00,
            C                   = 0x01,
            StdCall             = 0x02,
            ThisCall            = 0x03,
            FastCall            = 0x04,
            Vararg              = 0x05,
            Field               = 0x06,
            LocalSig            = 0x07,
            Property            = 0x08,
            Unmgd               = 0x09,
            GenericInst         = 0x0A,
            Max                 = 0x0B,
        }
#endregion

#region FCalls
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]        
        private extern void GetSignature(
            void* pCorSig, int cCorSig,
            RuntimeFieldHandleInternal fieldHandle, IRuntimeMethodInfo methodHandle, RuntimeType declaringType);

#endregion

#region Private Data Members
        //
        // Keep the layout in sync with SignatureNative in the VM
        //
        internal RuntimeType[] m_arguments;
        internal RuntimeType m_declaringType;
        internal RuntimeType m_returnTypeORfieldType;
        internal object m_keepalive;
        [SecurityCritical]
        internal void* m_sig;
        internal int m_managedCallingConventionAndArgIteratorFlags; // lowest byte is CallingConvention, upper 3 bytes are ArgIterator flags
        internal int m_nSizeOfArgStack;
        internal int m_csig;
        internal RuntimeMethodHandleInternal m_pMethod;
#endregion

#region Constructors
        [System.Security.SecuritySafeCritical]  // auto-generated
        public Signature (
            IRuntimeMethodInfo method,
            RuntimeType[] arguments,
            RuntimeType returnType,
            CallingConventions callingConvention)
        {
            m_pMethod = method.Value;
            m_arguments = arguments;
            m_returnTypeORfieldType = returnType;
            m_managedCallingConventionAndArgIteratorFlags = (byte)callingConvention;

            GetSignature(null, 0, new RuntimeFieldHandleInternal(), method, null);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public Signature(IRuntimeMethodInfo methodHandle, RuntimeType declaringType)
        {
            GetSignature(null, 0, new RuntimeFieldHandleInternal(), methodHandle, declaringType);
        }

        [System.Security.SecurityCritical]  // auto-generated
        public Signature(IRuntimeFieldInfo fieldHandle, RuntimeType declaringType)
        {
            GetSignature(null, 0, fieldHandle.Value, null, declaringType);
            GC.KeepAlive(fieldHandle);
        }

        [System.Security.SecurityCritical]  // auto-generated
        public Signature(void* pCorSig, int cCorSig, RuntimeType declaringType)
        {
            GetSignature(pCorSig, cCorSig, new RuntimeFieldHandleInternal(), null, declaringType);
        }
#endregion

#region Internal Members
        internal CallingConventions CallingConvention { get { return (CallingConventions)(byte)m_managedCallingConventionAndArgIteratorFlags; } }
        internal RuntimeType[] Arguments { get { return m_arguments; } }
        internal RuntimeType ReturnType { get { return m_returnTypeORfieldType; } }
        internal RuntimeType FieldType { get { return m_returnTypeORfieldType; } }

        [System.Security.SecuritySafeCritical]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern bool CompareSig(Signature sig1, Signature sig2);

        [System.Security.SecuritySafeCritical]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
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
        internal abstract RuntimeType GetJitContext(ref int securityControlFlags);
        internal abstract byte[] GetCodeInfo(ref int stackSize, ref int initLocals, ref int EHCount);
        internal abstract byte[] GetLocalsSignature();
        [System.Security.SecurityCritical] // takes a pointer parameter
        internal abstract unsafe void GetEHInfo(int EHNumber, void* exception);
        internal abstract unsafe byte[] GetRawEHInfo();
        // token resolution
        internal abstract String GetStringLiteral(int token);
        [System.Security.SecurityCritical] // passes a pointer out
        internal abstract void ResolveToken(int token, out IntPtr typeHandle, out IntPtr methodHandle, out IntPtr fieldHandle);
        internal abstract byte[] ResolveSignature(int token, int fromMethod);
        // 
        internal abstract MethodInfo GetDynamicMethod();
#if FEATURE_COMPRESSEDSTACK
        internal abstract CompressedStack GetSecurityContext();
#endif
    }

}
