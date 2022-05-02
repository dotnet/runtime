// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.Versioning;
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

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2070:UnrecognizedReflectionPattern",
            Justification = "Trimming doesn't affect types eligible for marshalling. Different exception for invalid inputs doesn't matter.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static IntPtr OffsetOf(Type t, string fieldName)
        {
            ArgumentNullException.ThrowIfNull(t);

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

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("ReadByte(Object, Int32) may be unavailable in future releases.")]
        [RequiresDynamicCode("Marshalling code for the object might not be available")]
        public static byte ReadByte(object ptr, int ofs)
        {
            return ReadValueSlow(ptr, ofs, (IntPtr nativeHome, int offset) => ReadByte(nativeHome, offset));
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("ReadInt16(Object, Int32) may be unavailable in future releases.")]
        [RequiresDynamicCode("Marshalling code for the object might not be available")]
        public static short ReadInt16(object ptr, int ofs)
        {
            return ReadValueSlow(ptr, ofs, (IntPtr nativeHome, int offset) => ReadInt16(nativeHome, offset));
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("ReadInt32(Object, Int32) may be unavailable in future releases.")]
        [RequiresDynamicCode("Marshalling code for the object might not be available")]
        public static int ReadInt32(object ptr, int ofs)
        {
            return ReadValueSlow(ptr, ofs, (IntPtr nativeHome, int offset) => ReadInt32(nativeHome, offset));
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("ReadInt64(Object, Int32) may be unavailable in future releases.")]
        [RequiresDynamicCode("Marshalling code for the object might not be available")]
        public static long ReadInt64([MarshalAs(UnmanagedType.AsAny), In] object ptr, int ofs)
        {
            return ReadValueSlow(ptr, ofs, (IntPtr nativeHome, int offset) => ReadInt64(nativeHome, offset));
        }

        /// <summary>Read value from marshaled object (marshaled using AsAny).</summary>
        /// <remarks>
        /// It's quite slow and can return back dangling pointers. It's only there for backcompat.
        /// People should instead use the IntPtr overloads.
        /// </remarks>
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

            MngdNativeArrayMarshaler.MarshalerState nativeArrayMarshalerState = default;
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

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("WriteByte(Object, Int32, Byte) may be unavailable in future releases.")]
        [RequiresDynamicCode("Marshalling code for the object might not be available")]
        public static void WriteByte(object ptr, int ofs, byte val)
        {
            WriteValueSlow(ptr, ofs, val, (IntPtr nativeHome, int offset, byte value) => WriteByte(nativeHome, offset, value));
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("WriteInt16(Object, Int32, Int16) may be unavailable in future releases.")]
        [RequiresDynamicCode("Marshalling code for the object might not be available")]
        public static void WriteInt16(object ptr, int ofs, short val)
        {
            WriteValueSlow(ptr, ofs, val, (IntPtr nativeHome, int offset, short value) => Marshal.WriteInt16(nativeHome, offset, value));
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("WriteInt32(Object, Int32, Int32) may be unavailable in future releases.")]
        [RequiresDynamicCode("Marshalling code for the object might not be available")]
        public static void WriteInt32(object ptr, int ofs, int val)
        {
            WriteValueSlow(ptr, ofs, val, (IntPtr nativeHome, int offset, int value) => Marshal.WriteInt32(nativeHome, offset, value));
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("WriteInt64(Object, Int32, Int64) may be unavailable in future releases.")]
        [RequiresDynamicCode("Marshalling code for the object might not be available")]
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

            MngdNativeArrayMarshaler.MarshalerState nativeArrayMarshalerState = default;
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

        /// <summary>
        /// Get the last platform invoke error on the current thread
        /// </summary>
        /// <returns>The last platform invoke error</returns>
        /// <remarks>
        /// The last platform invoke error corresponds to the error set by either the most recent platform
        /// invoke that was configured to set the last error or a call to <see cref="SetLastPInvokeError(int)" />.
        /// </remarks>
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern int GetLastPInvokeError();

        /// <summary>
        /// Set the last platform invoke error on the current thread
        /// </summary>
        /// <param name="error">Error to set</param>
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern void SetLastPInvokeError(int error);

        private static void PrelinkCore(MethodInfo m)
        {
            if (!(m is RuntimeMethodInfo rmi))
            {
                throw new ArgumentException(SR.Argument_MustBeRuntimeMethodInfo, nameof(m));
            }

            InternalPrelink(((IRuntimeMethodInfo)rmi).Value);
            GC.KeepAlive(rmi);
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "MarshalNative_Prelink")]
        private static partial void InternalPrelink(RuntimeMethodHandleInternal m);

        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern /* struct _EXCEPTION_POINTERS* */ IntPtr GetExceptionPointers();

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("GetExceptionCode() may be unavailable in future releases.")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern int GetExceptionCode();

        /// <summary>
        /// Marshals data from a structure class to a native memory block. If the
        /// structure contains pointers to allocated blocks and "fDeleteOld" is
        /// true, this routine will call DestroyStructure() first.
        /// </summary>
        [RequiresDynamicCode("Marshalling code for the object might not be available. Use the StructureToPtr<T> overload instead.")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static extern void StructureToPtr(object structure, IntPtr ptr, bool fDeleteOld);

        /// <summary>
        /// Helper function to copy a pointer into a preallocated structure.
        /// </summary>
        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern void PtrToStructureHelper(IntPtr ptr, object structure, bool allowValueClasses);

        /// <summary>
        /// Frees all substructures pointed to by the native memory block.
        /// "structuretype" is used to provide layout information.
        /// </summary>
        [RequiresDynamicCode("Marshalling code for the object might not be available. Use the DestroyStructure<T> overload instead.")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static extern void DestroyStructure(IntPtr ptr, Type structuretype);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern bool IsPinnable(object? obj);

#if TARGET_WINDOWS
        internal static bool IsBuiltInComSupported { get; } = IsBuiltInComSupportedInternal();

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "MarshalNative_IsBuiltInComSupported")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool IsBuiltInComSupportedInternal();

        /// <summary>
        /// Returns the HInstance for this module.  Returns -1 if the module doesn't have
        /// an HInstance.  In Memory (Dynamic) Modules won't have an HInstance.
        /// </summary>
        public static IntPtr GetHINSTANCE(Module m)
        {
            ArgumentNullException.ThrowIfNull(m);

            if (m is RuntimeModule rtModule)
            {
                return GetHINSTANCE(new QCallModule(ref rtModule));
            }

            return (IntPtr)(-1);
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "MarshalNative_GetHINSTANCE")]
        private static partial IntPtr GetHINSTANCE(QCallModule m);

#endif // TARGET_WINDOWS


        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern Exception GetExceptionForHRInternal(int errorCode, IntPtr errorInfo);

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
        [SupportedOSPlatform("windows")]
        public static string GetTypeInfoName(ITypeInfo typeInfo)
        {
            ArgumentNullException.ThrowIfNull(typeInfo);

            typeInfo.GetDocumentation(-1, out string strTypeLibName, out _, out _, out _);
            return strTypeLibName;
        }

        // This method is identical to Type.GetTypeFromCLSID. Since it's interop specific, we expose it
        // on Marshal for more consistent API surface.
        internal static Type? GetTypeFromCLSID(Guid clsid, string? server, bool throwOnError)
        {
            if (!IsBuiltInComSupported)
            {
                throw new NotSupportedException(SR.NotSupported_COM);
            }

            // Note: "throwOnError" is a vacuous parameter. Any errors due to the CLSID not being registered or the server not being found will happen
            // on the Activator.CreateInstance() call. GetTypeFromCLSID() merely wraps the data in a Type object without any validation.

            Type? type = null;
            GetTypeFromCLSID(clsid, server, ObjectHandleOnStack.Create(ref type));
            return type;
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "MarshalNative_GetTypeFromCLSID", StringMarshalling = StringMarshalling.Utf16)]
        private static partial void GetTypeFromCLSID(in Guid clsid, string? server, ObjectHandleOnStack retType);

        /// <summary>
        /// Return the IUnknown* for an Object if the current context is the one
        /// where the RCW was first seen. Will return null otherwise.
        /// </summary>
        [SupportedOSPlatform("windows")]
        public static IntPtr /* IUnknown* */ GetIUnknownForObject(object o)
        {
            ArgumentNullException.ThrowIfNull(o);

            return GetIUnknownForObjectNative(o);
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern IntPtr /* IUnknown* */ GetIUnknownForObjectNative(object o);

        /// <summary>
        /// Return the IDispatch* for an Object.
        /// </summary>
        [SupportedOSPlatform("windows")]
        public static IntPtr /* IDispatch */ GetIDispatchForObject(object o)
        {
            ArgumentNullException.ThrowIfNull(o);

            return GetIDispatchForObjectNative(o);
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern IntPtr /* IDispatch* */ GetIDispatchForObjectNative(object o);

        /// <summary>
        /// Return the IUnknown* representing the interface for the Object.
        /// Object o should support Type T
        /// </summary>
        [SupportedOSPlatform("windows")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static IntPtr /* IUnknown* */ GetComInterfaceForObject(object o, Type T)
        {
            ArgumentNullException.ThrowIfNull(o);
            ArgumentNullException.ThrowIfNull(T);

            return GetComInterfaceForObjectNative(o, T, true);
        }

        [SupportedOSPlatform("windows")]
        public static IntPtr GetComInterfaceForObject<T, TInterface>([DisallowNull] T o) => GetComInterfaceForObject(o!, typeof(TInterface));

        /// <summary>
        /// Return the IUnknown* representing the interface for the Object.
        /// Object o should support Type T, it refer the value of mode to
        /// invoke customized QueryInterface or not.
        /// </summary>
        [SupportedOSPlatform("windows")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static IntPtr /* IUnknown* */ GetComInterfaceForObject(object o, Type T, CustomQueryInterfaceMode mode)
        {
            ArgumentNullException.ThrowIfNull(o);
            ArgumentNullException.ThrowIfNull(T);

            bool bEnableCustomizedQueryInterface = ((mode == CustomQueryInterfaceMode.Allow) ? true : false);
            return GetComInterfaceForObjectNative(o, T, bEnableCustomizedQueryInterface);
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern IntPtr /* IUnknown* */ GetComInterfaceForObjectNative(object o, Type t, bool fEnableCustomizedQueryInterface);

        /// <summary>
        /// Return the managed object representing the IUnknown*
        /// </summary>
        [SupportedOSPlatform("windows")]
        public static object GetObjectForIUnknown(IntPtr /* IUnknown* */ pUnk)
        {
            ArgumentNullException.ThrowIfNull(pUnk);

            return GetObjectForIUnknownNative(pUnk);
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern object GetObjectForIUnknownNative(IntPtr /* IUnknown* */ pUnk);

        [SupportedOSPlatform("windows")]
        public static object GetUniqueObjectForIUnknown(IntPtr unknown)
        {
            ArgumentNullException.ThrowIfNull(unknown);

            return GetUniqueObjectForIUnknownNative(unknown);
        }

        /// <summary>
        /// Return a unique Object given an IUnknown.  This ensures that you receive a fresh
        /// object (we will not look in the cache to match up this IUnknown to an already
        /// existing object). This is useful in cases where you want to be able to call
        /// ReleaseComObject on a RCW and not worry about other active uses ofsaid RCW.
        /// </summary>
        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern object GetUniqueObjectForIUnknownNative(IntPtr unknown);

        /// <summary>
        /// Return an Object for IUnknown, using the Type T.
        /// Type T should be either a COM imported Type or a sub-type of COM imported Type
        /// </summary>
        [SupportedOSPlatform("windows")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern object GetTypedObjectForIUnknown(IntPtr /* IUnknown* */ pUnk, Type t);

        [SupportedOSPlatform("windows")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static IntPtr CreateAggregatedObject(IntPtr pOuter, object o)
        {
            if (!IsBuiltInComSupported)
            {
                throw new NotSupportedException(SR.NotSupported_COM);
            }

            return CreateAggregatedObjectNative(pOuter, o);
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern IntPtr CreateAggregatedObjectNative(IntPtr pOuter, object o);

        [SupportedOSPlatform("windows")]
        public static IntPtr CreateAggregatedObject<T>(IntPtr pOuter, T o) where T : notnull
        {
            if (!IsBuiltInComSupported)
            {
                throw new NotSupportedException(SR.NotSupported_COM);
            }

            return CreateAggregatedObject(pOuter, (object)o);
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern void CleanupUnusedObjectsInCurrentContext();

        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern bool AreComObjectsAvailableForCleanup();

        /// <summary>
        /// Checks if the object is classic COM component.
        /// </summary>
        public static bool IsComObject(object o)
        {
            ArgumentNullException.ThrowIfNull(o);

            return o is __ComObject;
        }

        /// <summary>
        /// Release the COM component and if the reference hits 0 zombie this object.
        /// Further usage of this Object might throw an exception
        /// </summary>
        [SupportedOSPlatform("windows")]
        public static int ReleaseComObject(object o)
        {
            if (!IsBuiltInComSupported)
            {
                throw new NotSupportedException(SR.NotSupported_COM);
            }

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
        [SupportedOSPlatform("windows")]
        public static int FinalReleaseComObject(object o)
        {
            if (!IsBuiltInComSupported)
            {
                throw new NotSupportedException(SR.NotSupported_COM);
            }

            ArgumentNullException.ThrowIfNull(o);
            if (!(o is __ComObject co))
            {
                throw new ArgumentException(SR.Argument_ObjNotComObject, nameof(o));
            }

            co.FinalReleaseSelf();
            return 0;
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern void InternalFinalReleaseComObject(object o);

        [SupportedOSPlatform("windows")]
        public static object? GetComObjectData(object obj, object key)
        {
            if (!IsBuiltInComSupported)
            {
                throw new NotSupportedException(SR.NotSupported_COM);
            }

            ArgumentNullException.ThrowIfNull(obj);
            ArgumentNullException.ThrowIfNull(key);
            if (!(obj is __ComObject co))
            {
                throw new ArgumentException(SR.Argument_ObjNotComObject, nameof(obj));
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
        [SupportedOSPlatform("windows")]
        public static bool SetComObjectData(object obj, object key, object? data)
        {
            if (!IsBuiltInComSupported)
            {
                throw new NotSupportedException(SR.NotSupported_COM);
            }

            ArgumentNullException.ThrowIfNull(obj);
            ArgumentNullException.ThrowIfNull(key);
            if (!(obj is __ComObject co))
            {
                throw new ArgumentException(SR.Argument_ObjNotComObject, nameof(obj));
            }

            // Retrieve the data from the __ComObject.
            return co.SetData(key, data);
        }

        /// <summary>
        /// This method takes the given COM object and wraps it in an object
        /// of the specified type. The type must be derived from __ComObject.
        /// </summary>
        [SupportedOSPlatform("windows")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [return: NotNullIfNotNull("o")]
        public static object? CreateWrapperOfType(object? o, Type t)
        {
            if (!IsBuiltInComSupported)
            {
                throw new NotSupportedException(SR.NotSupported_COM);
            }

            ArgumentNullException.ThrowIfNull(t);
            if (!t.IsCOMObject)
            {
                throw new ArgumentException(SR.Argument_TypeNotComObject, nameof(t));
            }
            if (t.IsGenericType)
            {
                throw new ArgumentException(SR.Argument_NeedNonGenericType, nameof(t));
            }

            if (o is null)
            {
                return null;
            }

            if (!o.GetType().IsCOMObject)
            {
                throw new ArgumentException(SR.Argument_ObjNotComObject, nameof(o));
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
                    Wrapper = GetComObjectData(o, t)!;
                }
            }

            return Wrapper;
        }

        [SupportedOSPlatform("windows")]
        public static TWrapper CreateWrapperOfType<T, TWrapper>(T? o)
        {
            if (!IsBuiltInComSupported)
            {
                throw new NotSupportedException(SR.NotSupported_COM);
            }

            return (TWrapper)CreateWrapperOfType(o, typeof(TWrapper))!;
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern object InternalCreateWrapperOfType(object o, Type t);

        /// <summary>
        /// check if the type is visible from COM.
        /// </summary>
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern bool IsTypeVisibleFromCom(Type t);

        [SupportedOSPlatform("windows")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void GetNativeVariantForObject(object? obj, /* VARIANT * */ IntPtr pDstNativeVariant)
        {
            if (!IsBuiltInComSupported)
            {
                throw new NotSupportedException(SR.NotSupported_COM);
            }

            GetNativeVariantForObjectNative(obj, pDstNativeVariant);
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern void GetNativeVariantForObjectNative(object? obj, /* VARIANT * */ IntPtr pDstNativeVariant);

        [SupportedOSPlatform("windows")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void GetNativeVariantForObject<T>(T? obj, IntPtr pDstNativeVariant)
        {
            if (!IsBuiltInComSupported)
            {
                throw new NotSupportedException(SR.NotSupported_COM);
            }

            GetNativeVariantForObject((object?)obj, pDstNativeVariant);
        }

        [SupportedOSPlatform("windows")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object? GetObjectForNativeVariant(/* VARIANT * */ IntPtr pSrcNativeVariant)
        {
            if (!IsBuiltInComSupported)
            {
                throw new NotSupportedException(SR.NotSupported_COM);
            }

            return GetObjectForNativeVariantNative(pSrcNativeVariant);
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern object? GetObjectForNativeVariantNative(/* VARIANT * */ IntPtr pSrcNativeVariant);

        [SupportedOSPlatform("windows")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static T? GetObjectForNativeVariant<T>(IntPtr pSrcNativeVariant)
        {
            if (!IsBuiltInComSupported)
            {
                throw new NotSupportedException(SR.NotSupported_COM);
            }

            return (T?)GetObjectForNativeVariant(pSrcNativeVariant);
        }

        [SupportedOSPlatform("windows")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object?[] GetObjectsForNativeVariants(/* VARIANT * */ IntPtr aSrcNativeVariant, int cVars)
        {
            if (!IsBuiltInComSupported)
            {
                throw new NotSupportedException(SR.NotSupported_COM);
            }

            return GetObjectsForNativeVariantsNative(aSrcNativeVariant, cVars);
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern object?[] GetObjectsForNativeVariantsNative(/* VARIANT * */ IntPtr aSrcNativeVariant, int cVars);

        [SupportedOSPlatform("windows")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static T[] GetObjectsForNativeVariants<T>(IntPtr aSrcNativeVariant, int cVars)
        {
            if (!IsBuiltInComSupported)
            {
                throw new NotSupportedException(SR.NotSupported_COM);
            }

            object?[] objects = GetObjectsForNativeVariants(aSrcNativeVariant, cVars);

            T[] result = new T[objects.Length];
            Array.Copy(objects, result, objects.Length);

            return result;
        }

        /// <summary>
        /// <para>Returns the first valid COM slot that GetMethodInfoForSlot will work on
        /// This will be 3 for IUnknown based interfaces and 7 for IDispatch based interfaces. </para>
        /// </summary>
        [SupportedOSPlatform("windows")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern int GetStartComSlot(Type t);

        /// <summary>
        /// <para>Returns the last valid COM slot that GetMethodInfoForSlot will work on. </para>
        /// </summary>
        [SupportedOSPlatform("windows")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern int GetEndComSlot(Type t);

        [RequiresUnreferencedCode("Built-in COM support is not trim compatible", Url = "https://aka.ms/dotnet-illink/com")]
        [SupportedOSPlatform("windows")]
        public static object BindToMoniker(string monikerName)
        {
            if (!IsBuiltInComSupported)
            {
                throw new NotSupportedException(SR.NotSupported_COM);
            }

            ThrowExceptionForHR(CreateBindCtx(0, out IntPtr bindctx));

            try
            {
                ThrowExceptionForHR(MkParseDisplayName(bindctx, monikerName, out _, out IntPtr pmoniker));
                try
                {
                    ThrowExceptionForHR(BindMoniker(pmoniker, 0, ref IID_IUnknown, out IntPtr ptr));
                    try
                    {
                        return GetObjectForIUnknown(ptr);
                    }
                    finally
                    {
                        Release(ptr);
                    }
                }
                finally
                {
                    Release(pmoniker);
                }
            }
            finally
            {
                Release(bindctx);
            }
        }
        // Revist after https://github.com/mono/linker/issues/1989 is fixed
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2050:UnrecognizedReflectionPattern",
            Justification = "The calling method is annotated with RequiresUnreferencedCode")]
        [LibraryImport(Interop.Libraries.Ole32)]
        private static partial int CreateBindCtx(uint reserved, out IntPtr ppbc);

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2050:UnrecognizedReflectionPattern",
            Justification = "The calling method is annotated with RequiresUnreferencedCode")]
        [LibraryImport(Interop.Libraries.Ole32)]
        private static partial int MkParseDisplayName(IntPtr pbc, [MarshalAs(UnmanagedType.LPWStr)] string szUserName, out uint pchEaten, out IntPtr ppmk);

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2050:UnrecognizedReflectionPattern",
            Justification = "The calling method is annotated with RequiresUnreferencedCode")]
        [LibraryImport(Interop.Libraries.Ole32)]
        private static partial int BindMoniker(IntPtr pmk, uint grfOpt, ref Guid iidResult, out IntPtr ppvResult);

        [SupportedOSPlatform("windows")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern void ChangeWrapperHandleStrength(object otp, bool fIsWeak);
#endif // FEATURE_COMINTEROP

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern Delegate GetDelegateForFunctionPointerInternal(IntPtr ptr, Type t);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern IntPtr GetFunctionPointerForDelegateInternal(Delegate d);

#if DEBUG // Used for testing in Checked or Debug
        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "MarshalNative_GetIsInCooperativeGCModeFunctionPointer")]
        internal static unsafe partial delegate* unmanaged<int> GetIsInCooperativeGCModeFunctionPointer();
#endif
    }
}
