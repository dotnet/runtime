// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices.ComTypes;
using System.StubHelpers;

namespace System.Runtime.InteropServices
{
    /// <summary>
    /// This class contains methods that are mainly used to marshal between unmanaged
    /// and managed types.
    /// </summary>
    public static partial class Marshal
    {
#if FEATURE_COMINTEROP
        /// <summary>
        /// IUnknown is {00000000-0000-0000-C000-000000000046}
        /// </summary>
        internal static Guid IID_IUnknown = new Guid(0, 0, 0, 0xC0, 0, 0, 0, 0, 0, 0, 0x46);
#endif //FEATURE_COMINTEROP

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern int SizeOfHelper(Type t, bool throwIfNotMarshalable);

        public static IntPtr OffsetOf(Type t, string fieldName)
        {
            if (t is null)
            {
                throw new ArgumentNullException(nameof(t));
            }

            FieldInfo? f = t.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (f is null)
            {
                throw new ArgumentException(SR.Format(SR.Argument_OffsetOfFieldNotFound, t.FullName), nameof(fieldName));
            }

            if (!(f is RtFieldInfo rtField))
            {
                throw new ArgumentException(SR.Argument_MustBeRuntimeFieldInfo, nameof(fieldName));
            }

            return OffsetOfHelper(rtField);
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern IntPtr OffsetOfHelper(IRuntimeFieldInfo f);

        public static byte ReadByte(object ptr, int ofs)
        {
            return ReadValueSlow(ptr, ofs, (IntPtr nativeHome, int offset) => ReadByte(nativeHome, offset));
        }

        public static short ReadInt16(object ptr, int ofs)
        {
            return ReadValueSlow(ptr, ofs, (IntPtr nativeHome, int offset) => ReadInt16(nativeHome, offset));
        }

        public static int ReadInt32(object ptr, int ofs)
        {
            return ReadValueSlow(ptr, ofs, (IntPtr nativeHome, int offset) => ReadInt32(nativeHome, offset));
        }

        public static long ReadInt64([MarshalAs(UnmanagedType.AsAny), In] object ptr, int ofs)
        {
            return ReadValueSlow(ptr, ofs, (IntPtr nativeHome, int offset) => ReadInt64(nativeHome, offset));
        }

        //====================================================================
        // Read value from marshaled object (marshaled using AsAny)
        // It's quite slow and can return back dangling pointers
        // It's only there for backcompact
        // People should instead use the IntPtr overloads
        //====================================================================
        private static unsafe T ReadValueSlow<T>(object ptr, int ofs, Func<IntPtr, int, T> readValueHelper)
        {
            // Consumers of this method are documented to throw AccessViolationException on any AV
            if (ptr is null)
            {
                throw new AccessViolationException();
            }

            const int Flags = 
                (int)AsAnyMarshaler.AsAnyFlags.In | 
                (int)AsAnyMarshaler.AsAnyFlags.IsAnsi | 
                (int)AsAnyMarshaler.AsAnyFlags.IsBestFit;

            MngdNativeArrayMarshaler.MarshalerState nativeArrayMarshalerState = new MngdNativeArrayMarshaler.MarshalerState();
            AsAnyMarshaler marshaler = new AsAnyMarshaler(new IntPtr(&nativeArrayMarshalerState));

            IntPtr pNativeHome = IntPtr.Zero;

            try
            {
                pNativeHome = marshaler.ConvertToNative(ptr, Flags);
                return readValueHelper(pNativeHome, ofs);
            }
            finally
            {
                marshaler.ClearNative(pNativeHome);
            }
        }

        public static void WriteByte(object ptr, int ofs, byte val)
        {
            WriteValueSlow(ptr, ofs, val, (IntPtr nativeHome, int offset, byte value) => WriteByte(nativeHome, offset, value));
        }

        public static void WriteInt16(object ptr, int ofs, short val)
        {
            WriteValueSlow(ptr, ofs, val, (IntPtr nativeHome, int offset, short value) => Marshal.WriteInt16(nativeHome, offset, value));
        }

        public static void WriteInt32(object ptr, int ofs, int val)
        {
            WriteValueSlow(ptr, ofs, val, (IntPtr nativeHome, int offset, int value) => Marshal.WriteInt32(nativeHome, offset, value));
        }

        public static void WriteInt64(object ptr, int ofs, long val)
        {
            WriteValueSlow(ptr, ofs, val, (IntPtr nativeHome, int offset, long value) => Marshal.WriteInt64(nativeHome, offset, value));
        }

        /// <summary>
        /// Write value into marshaled object (marshaled using AsAny) and propagate the
        /// value back. This is quite slow and can return back dangling pointers. It is
        /// only here for backcompat. People should instead use the IntPtr overloads.
        /// </summary>
        private static unsafe void WriteValueSlow<T>(object ptr, int ofs, T val, Action<IntPtr, int, T> writeValueHelper)
        {
            // Consumers of this method are documented to throw AccessViolationException on any AV
            if (ptr is null)
            {
                throw new AccessViolationException();
            }

            const int Flags = 
                (int)AsAnyMarshaler.AsAnyFlags.In | 
                (int)AsAnyMarshaler.AsAnyFlags.Out | 
                (int)AsAnyMarshaler.AsAnyFlags.IsAnsi | 
                (int)AsAnyMarshaler.AsAnyFlags.IsBestFit;

            MngdNativeArrayMarshaler.MarshalerState nativeArrayMarshalerState = new MngdNativeArrayMarshaler.MarshalerState();
            AsAnyMarshaler marshaler = new AsAnyMarshaler(new IntPtr(&nativeArrayMarshalerState));

            IntPtr pNativeHome = IntPtr.Zero;

            try
            {
                pNativeHome = marshaler.ConvertToNative(ptr, Flags);
                writeValueHelper(pNativeHome, ofs, val);
                marshaler.ConvertToManaged(ptr, pNativeHome);
            }
            finally
            {
                marshaler.ClearNative(pNativeHome);
            }
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern int GetLastWin32Error();

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern void SetLastWin32Error(int error);

        private static void PrelinkCore(MethodInfo m)
        {
            if (!(m is RuntimeMethodInfo rmi))
            {
                throw new ArgumentException(SR.Argument_MustBeRuntimeMethodInfo, nameof(m));
            }

            InternalPrelink(((IRuntimeMethodInfo)rmi).Value);
            GC.KeepAlive(rmi);
        }

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern void InternalPrelink(RuntimeMethodHandleInternal m);

        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern /* struct _EXCEPTION_POINTERS* */ IntPtr GetExceptionPointers();
        
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern int GetExceptionCode();

        /// <summary>
        /// Marshals data from a structure class to a native memory block. If the
        /// structure contains pointers to allocated blocks and "fDeleteOld" is
        /// true, this routine will call DestroyStructure() first. 
        /// </summary>
        [MethodImpl(MethodImplOptions.InternalCall), ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        public static extern void StructureToPtr(object structure, IntPtr ptr, bool fDeleteOld);

        private static object PtrToStructureHelper(IntPtr ptr, Type structureType)
        {
            var rt = (RuntimeType)structureType;
            object structure = rt.CreateInstanceDefaultCtor(publicOnly: false, skipCheckThis: false, fillCache: false, wrapExceptions: true);
            PtrToStructureHelper(ptr, structure, allowValueClasses: true);
            return structure;
        }

        /// <summary>
        /// Helper function to copy a pointer into a preallocated structure.
        /// </summary>
        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern void PtrToStructureHelper(IntPtr ptr, object structure, bool allowValueClasses);

        /// <summary>
        /// Frees all substructures pointed to by the native memory block.
        /// "structuretype" is used to provide layout information.
        /// </summary>
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern void DestroyStructure(IntPtr ptr, Type structuretype);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern bool IsPinnable(object? obj);

#if FEATURE_COMINTEROP
        /// <summary>
        /// Returns the HInstance for this module.  Returns -1 if the module doesn't have
        /// an HInstance.  In Memory (Dynamic) Modules won't have an HInstance.
        /// </summary>
        public static IntPtr GetHINSTANCE(Module m)
        {
            if (m is null)
            {
                throw new ArgumentNullException(nameof(m));
            }

            if (m is RuntimeModule rtModule)
            {
                return GetHINSTANCE(JitHelpers.GetQCallModuleOnStack(ref rtModule));
            }

            return (IntPtr)(-1);
        }

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern IntPtr GetHINSTANCE(QCallModule m);

#endif // FEATURE_COMINTEROP


        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern Exception GetExceptionForHRInternal(int errorCode, IntPtr errorInfo);

        public static IntPtr AllocHGlobal(IntPtr cb)
        {
            // For backwards compatibility on 32 bit platforms, ensure we pass values between 
            // int.MaxValue and uint.MaxValue to Windows.  If the binary has had the 
            // LARGEADDRESSAWARE bit set in the PE header, it may get 3 or 4 GB of user mode
            // address space.  It is remotely that those allocations could have succeeded,
            // though I couldn't reproduce that.  In either case, that means we should continue
            // throwing an OOM instead of an ArgumentOutOfRangeException for "negative" amounts of memory.
            UIntPtr numBytes;
#if BIT64
            numBytes = new UIntPtr(unchecked((ulong)cb.ToInt64()));
#else // 32
            numBytes = new UIntPtr(unchecked((uint)cb.ToInt32()));
#endif

            IntPtr pNewMem = Interop.Kernel32.LocalAlloc(Interop.Kernel32.LMEM_FIXED, unchecked(numBytes));
            if (pNewMem == IntPtr.Zero)
            {
                throw new OutOfMemoryException();
            }

            return pNewMem;
        }

        public static void FreeHGlobal(IntPtr hglobal)
        {
            if (!IsWin32Atom(hglobal))
            {
                if (IntPtr.Zero != Interop.Kernel32.LocalFree(hglobal))
                {
                    ThrowExceptionForHR(GetHRForLastWin32Error());
                }
            }
        }

        public static IntPtr ReAllocHGlobal(IntPtr pv, IntPtr cb)
        {
            IntPtr pNewMem = Interop.Kernel32.LocalReAlloc(pv, cb, Interop.Kernel32.LMEM_MOVEABLE);
            if (pNewMem == IntPtr.Zero)
            {
                throw new OutOfMemoryException();
            }

            return pNewMem;
        }

#if FEATURE_COMINTEROP
        /// <summary>
        /// Converts the CLR exception to an HRESULT. This function also sets
        /// up an IErrorInfo for the exception.
        /// </summary>
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern int GetHRForException(Exception? e);

        /// <summary>
        /// Given a managed object that wraps an ITypeInfo, return its name.
        /// </summary>
        public static string GetTypeInfoName(ITypeInfo typeInfo)
        {
            if (typeInfo is null)
            {
                throw new ArgumentNullException(nameof(typeInfo));
            }

            typeInfo.GetDocumentation(-1, out string strTypeLibName, out _, out _, out _);
            return strTypeLibName;
        }

        // This method is identical to Type.GetTypeFromCLSID. Since it's interop specific, we expose it
        // on Marshal for more consistent API surface.
        public static Type GetTypeFromCLSID(Guid clsid) => RuntimeType.GetTypeFromCLSIDImpl(clsid, null, throwOnError: false);

        /// <summary>
        /// Return the IUnknown* for an Object if the current context is the one
        /// where the RCW was first seen. Will return null otherwise.
        /// </summary>
        public static IntPtr /* IUnknown* */ GetIUnknownForObject(object o)
        {
            return GetIUnknownForObjectNative(o, false);
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern IntPtr /* IUnknown* */ GetIUnknownForObjectNative(object o, bool onlyInContext);

        /// <summary>
        /// Return the raw IUnknown* for a COM Object not related to current.
        /// Does not call AddRef.
        /// </summary>
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern IntPtr /* IUnknown* */ GetRawIUnknownForComObjectNoAddRef(object o);

        /// <summary>
        /// Return the IUnknown* representing the interface for the Object.
        /// Object o should support Type T
        /// </summary>
        public static IntPtr /* IUnknown* */ GetComInterfaceForObject(object o, Type T)
        {
            return GetComInterfaceForObjectNative(o, T, false, true);
        }

        // TODO-NULLABLE-GENERIC: T cannot be null
        public static IntPtr GetComInterfaceForObject<T, TInterface>(T o) => GetComInterfaceForObject(o!, typeof(TInterface));

        /// <summary>
        /// Return the IUnknown* representing the interface for the Object.
        /// Object o should support Type T, it refer the value of mode to
        /// invoke customized QueryInterface or not.
        /// </summary>
        public static IntPtr /* IUnknown* */ GetComInterfaceForObject(object o, Type T, CustomQueryInterfaceMode mode)
        {
            bool bEnableCustomizedQueryInterface = ((mode == CustomQueryInterfaceMode.Allow) ? true : false);
            return GetComInterfaceForObjectNative(o, T, false, bEnableCustomizedQueryInterface);
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern IntPtr /* IUnknown* */ GetComInterfaceForObjectNative(object o, Type t, bool onlyInContext, bool fEnalbeCustomizedQueryInterface);

        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern object GetObjectForIUnknown(IntPtr /* IUnknown* */ pUnk);

        /// <summary>
        /// Return a unique Object given an IUnknown.  This ensures that you receive a fresh
        /// object (we will not look in the cache to match up this IUnknown to an already
        /// existing object). This is useful in cases where you want to be able to call
        /// ReleaseComObject on a RCW and not worry about other active uses ofsaid RCW.
        /// </summary>
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern object GetUniqueObjectForIUnknown(IntPtr unknown);

        /// <summary>
        /// Return an Object for IUnknown, using the Type T.
        /// Type T should be either a COM imported Type or a sub-type of COM imported Type
        /// </summary>
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern object GetTypedObjectForIUnknown(IntPtr /* IUnknown* */ pUnk, Type t);

        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern IntPtr CreateAggregatedObject(IntPtr pOuter, object o);

        public static IntPtr CreateAggregatedObject<T>(IntPtr pOuter, T o)
        {
            // TODO-NULLABLE-GENERIC: T cannot be null
            return CreateAggregatedObject(pOuter, (object)o!);
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern void CleanupUnusedObjectsInCurrentContext();

        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern bool AreComObjectsAvailableForCleanup();

        /// <summary>
        /// Checks if the object is classic COM component.
        /// </summary>
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern bool IsComObject(object o);

#endif // FEATURE_COMINTEROP

        public static IntPtr AllocCoTaskMem(int cb)
        {
            IntPtr pNewMem = Interop.Ole32.CoTaskMemAlloc(new UIntPtr((uint)cb));
            if (pNewMem == IntPtr.Zero)
            {
                throw new OutOfMemoryException();
            }

            return pNewMem;
        }

        public static void FreeCoTaskMem(IntPtr ptr)
        {
            if (!IsWin32Atom(ptr))
            {
                Interop.Ole32.CoTaskMemFree(ptr);
            }
        }

        public static IntPtr ReAllocCoTaskMem(IntPtr pv, int cb)
        {
            IntPtr pNewMem = Interop.Ole32.CoTaskMemRealloc(pv, new UIntPtr((uint)cb));
            if (pNewMem == IntPtr.Zero && cb != 0)
            {
                throw new OutOfMemoryException();
            }

            return pNewMem;
        }

        internal static IntPtr AllocBSTR(int length)
        {
            IntPtr bstr = Interop.OleAut32.SysAllocStringLen(null, length);
            if (bstr == IntPtr.Zero)
            {
                throw new OutOfMemoryException();
            }
            return bstr;
        }

        public static void FreeBSTR(IntPtr ptr)
        {
            if (!IsWin32Atom(ptr))
            {
                Interop.OleAut32.SysFreeString(ptr);
            }
        }

        public static IntPtr StringToBSTR(string? s)
        {
            if (s is null)
            {
                return IntPtr.Zero;
            }

            // Overflow checking
            if (s.Length + 1 < s.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(s));
            }

            IntPtr bstr = Interop.OleAut32.SysAllocStringLen(s, s.Length);
            if (bstr == IntPtr.Zero)
            {
                throw new OutOfMemoryException();
            }

            return bstr;
        }

        public static string PtrToStringBSTR(IntPtr ptr)
        {
            return PtrToStringUni(ptr, (int)Interop.OleAut32.SysStringLen(ptr));
        }

#if FEATURE_COMINTEROP
        /// <summary>
        /// Release the COM component and if the reference hits 0 zombie this object.
        /// Further usage of this Object might throw an exception
        /// </summary>
        public static int ReleaseComObject(object o)
        {
            if (o is null)
            {
                // Match .NET Framework behaviour.
                throw new NullReferenceException();
            }
            if (!(o is __ComObject co))
            {
                throw new ArgumentException(SR.Argument_ObjNotComObject, nameof(o));
            }

            return co.ReleaseSelf();
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern int InternalReleaseComObject(object o);

        /// <summary>
        /// Release the COM component and zombie this object.
        /// Further usage of this Object might throw an exception
        /// </summary>
        public static int FinalReleaseComObject(object o)
        {
            if (o is null)
            {
                throw new ArgumentNullException(nameof(o));
            }
            if (!(o is __ComObject co))
            {
                throw new ArgumentException(SR.Argument_ObjNotComObject, nameof(o));
            }

            co.FinalReleaseSelf();
            return 0;
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern void InternalFinalReleaseComObject(object o);

        public static object? GetComObjectData(object obj, object key)
        {
            if (obj is null)
            {
                throw new ArgumentNullException(nameof(obj));
            }
            if (key is null)
            {
                throw new ArgumentNullException(nameof(key));
            }
            if (!(obj is __ComObject co))
            {
                throw new ArgumentException(SR.Argument_ObjNotComObject, nameof(obj));
            }
            if (obj.GetType().IsWindowsRuntimeObject)
            {
                throw new ArgumentException(SR.Argument_ObjIsWinRTObject, nameof(obj));
            }

            // Retrieve the data from the __ComObject.
            return co.GetData(key);
        }

        /// <summary>
        /// Sets data on the COM object. The data can only be set once for a given key
        /// and cannot be removed. This function returns true if the data has been added,
        /// false if the data could not be added because there already was data for the
        /// specified key.
        /// </summary>
        public static bool SetComObjectData(object obj, object key, object? data)
        {
            if (obj is null)
            {
                throw new ArgumentNullException(nameof(obj));
            }
            if (key is null)
            {
                throw new ArgumentNullException(nameof(key));
            }
            if (!(obj is __ComObject co))
            {
                throw new ArgumentException(SR.Argument_ObjNotComObject, nameof(obj));
            }
            if (obj.GetType().IsWindowsRuntimeObject)
            {
                throw new ArgumentException(SR.Argument_ObjIsWinRTObject, nameof(obj));
            }

            // Retrieve the data from the __ComObject.
            return co.SetData(key, data);
        }

        /// <summary>
        /// This method takes the given COM object and wraps it in an object
        /// of the specified type. The type must be derived from __ComObject.
        /// </summary>
        public static object? CreateWrapperOfType(object? o, Type t)
        {
            if (t is null)
            {
                throw new ArgumentNullException(nameof(t));
            }
            if (!t.IsCOMObject)
            {
                throw new ArgumentException(SR.Argument_TypeNotComObject, nameof(t));
            }
            if (t.IsGenericType)
            {
                throw new ArgumentException(SR.Argument_NeedNonGenericType, nameof(t));
            }
            if (t.IsWindowsRuntimeObject)
            {
                throw new ArgumentException(SR.Argument_TypeIsWinRTType, nameof(t));
            }

            if (o is null)
            {
                return null;
            }

            if (!o.GetType().IsCOMObject)
            {
                throw new ArgumentException(SR.Argument_ObjNotComObject, nameof(o));
            }
            if (o.GetType().IsWindowsRuntimeObject)
            {
                throw new ArgumentException(SR.Argument_ObjIsWinRTObject, nameof(o));
            }

            // Check to see if we have nothing to do.
            if (o.GetType() == t)
            {
                return o;
            }

            // Check to see if we already have a cached wrapper for this type.
            object? Wrapper = GetComObjectData(o, t);
            if (Wrapper is null)
            {
                // Create the wrapper for the specified type.
                Wrapper = InternalCreateWrapperOfType(o, t);

                // Attempt to cache the wrapper on the object.
                if (!SetComObjectData(o, t, Wrapper))
                {
                    // Another thead already cached the wrapper so use that one instead.
                    Wrapper = GetComObjectData(o, t);
                }
            }

            return Wrapper;
        }

        public static TWrapper CreateWrapperOfType<T, TWrapper>(T o)
        {
            // TODO-NULLABLE-GENERIC: T can be null
            return (TWrapper)CreateWrapperOfType(o, typeof(TWrapper))!;
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern object InternalCreateWrapperOfType(object o, Type t);

        /// <summary>
        /// check if the type is visible from COM.
        /// </summary>
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public static extern bool IsTypeVisibleFromCom(Type t);

        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern int /* HRESULT */ QueryInterface(IntPtr /* IUnknown */ pUnk, ref Guid iid, out IntPtr ppv);

        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern int /* ULONG */ AddRef(IntPtr /* IUnknown */ pUnk);

        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern int /* ULONG */ Release(IntPtr /* IUnknown */ pUnk);

        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern void GetNativeVariantForObject(object? obj, /* VARIANT * */ IntPtr pDstNativeVariant);

        public static void GetNativeVariantForObject<T>(T obj, IntPtr pDstNativeVariant)
        {
            // TODO-NULLABLE-GENERIC: T can be null
            GetNativeVariantForObject((object)obj!, pDstNativeVariant);
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern object? GetObjectForNativeVariant(/* VARIANT * */ IntPtr pSrcNativeVariant);

        public static T GetObjectForNativeVariant<T>(IntPtr pSrcNativeVariant)
        {
            // TODO-NULLABLE-GENERIC: T can be null
            return (T)GetObjectForNativeVariant(pSrcNativeVariant)!;
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern object?[] GetObjectsForNativeVariants(/* VARIANT * */ IntPtr aSrcNativeVariant, int cVars);

        // TODO-NULLABLE-GENERIC: T[] contents can be null
        public static T[] GetObjectsForNativeVariants<T>(IntPtr aSrcNativeVariant, int cVars)
        {
            object?[] objects = GetObjectsForNativeVariants(aSrcNativeVariant, cVars);

            T[]? result = new T[objects.Length];
            Array.Copy(objects, 0, result, 0, objects.Length);

            return result;
        }

        /// <summary>
        /// <para>Returns the first valid COM slot that GetMethodInfoForSlot will work on
        /// This will be 3 for IUnknown based interfaces and 7 for IDispatch based interfaces. </para>
        /// </summary>
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern int GetStartComSlot(Type t);

        /// <summary>
        /// <para>Returns the last valid COM slot that GetMethodInfoForSlot will work on. </para>
        /// </summary>
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern int GetEndComSlot(Type t);

        public static object BindToMoniker(string monikerName)
        {
            CreateBindCtx(0, out IBindCtx bindctx);

            MkParseDisplayName(bindctx, monikerName, out _, out IMoniker pmoniker);
            BindMoniker(pmoniker, 0, ref IID_IUnknown, out object obj);

            return obj;
        }

        [DllImport(Interop.Libraries.Ole32, PreserveSig = false)]
        private static extern void CreateBindCtx(uint reserved, out IBindCtx ppbc);

        [DllImport(Interop.Libraries.Ole32, PreserveSig = false)]
        private static extern void MkParseDisplayName(IBindCtx pbc, [MarshalAs(UnmanagedType.LPWStr)] string szUserName, out uint pchEaten, out IMoniker ppmk);

        [DllImport(Interop.Libraries.Ole32, PreserveSig = false)]
        private static extern void BindMoniker(IMoniker pmk, uint grfOpt, ref Guid iidResult, [MarshalAs(UnmanagedType.Interface)] out object ppvResult);

        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern void ChangeWrapperHandleStrength(object otp, bool fIsWeak);
#endif // FEATURE_COMINTEROP

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern Delegate GetDelegateForFunctionPointerInternal(IntPtr ptr, Type t);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern IntPtr GetFunctionPointerForDelegateInternal(Delegate d);
    }
}
