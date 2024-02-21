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

        internal static int SizeOfHelper(RuntimeType t, [MarshalAs(UnmanagedType.Bool)] bool throwIfNotMarshalable)
            => SizeOfHelper(new QCallTypeHandle(ref t), throwIfNotMarshalable);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "MarshalNative_SizeOfHelper")]
        private static partial int SizeOfHelper(QCallTypeHandle t, [MarshalAs(UnmanagedType.Bool)] bool throwIfNotMarshalable);

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

            nint offset = OffsetOf(rtField.GetFieldHandle());
            GC.KeepAlive(rtField);
            return offset;
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "MarshalNative_OffsetOf")]
        private static partial nint OffsetOf(IntPtr pFD);

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("ReadByte(Object, Int32) may be unavailable in future releases.")]
        [RequiresDynamicCode("Marshalling code for the object might not be available")]
        public static byte ReadByte(object ptr, int ofs)
        {
            return ReadValueSlow(ptr, ofs, ReadByte);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("ReadInt16(Object, Int32) may be unavailable in future releases.")]
        [RequiresDynamicCode("Marshalling code for the object might not be available")]
        public static short ReadInt16(object ptr, int ofs)
        {
            return ReadValueSlow(ptr, ofs, ReadInt16);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("ReadInt32(Object, Int32) may be unavailable in future releases.")]
        [RequiresDynamicCode("Marshalling code for the object might not be available")]
        public static int ReadInt32(object ptr, int ofs)
        {
            return ReadValueSlow(ptr, ofs, ReadInt32);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("ReadInt64(Object, Int32) may be unavailable in future releases.")]
        [RequiresDynamicCode("Marshalling code for the object might not be available")]
#pragma warning disable CS0618 // Type or member is obsolete
        public static long ReadInt64([MarshalAs(UnmanagedType.AsAny), In] object ptr, int ofs)
#pragma warning restore CS0618 // Type or member is obsolete
        {
            return ReadValueSlow(ptr, ofs, ReadInt64);
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
            WriteValueSlow(ptr, ofs, val, WriteByte);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("WriteInt16(Object, Int32, Int16) may be unavailable in future releases.")]
        [RequiresDynamicCode("Marshalling code for the object might not be available")]
        public static void WriteInt16(object ptr, int ofs, short val)
        {
            WriteValueSlow(ptr, ofs, val, WriteInt16);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("WriteInt32(Object, Int32, Int32) may be unavailable in future releases.")]
        [RequiresDynamicCode("Marshalling code for the object might not be available")]
        public static void WriteInt32(object ptr, int ofs, int val)
        {
            WriteValueSlow(ptr, ofs, val, WriteInt32);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("WriteInt64(Object, Int32, Int64) may be unavailable in future releases.")]
        [RequiresDynamicCode("Marshalling code for the object might not be available")]
        public static void WriteInt64(object ptr, int ofs, long val)
        {
            WriteValueSlow(ptr, ofs, val, WriteInt64);
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
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static unsafe void StructureToPtr(object structure, IntPtr ptr, bool fDeleteOld)
        {
            ArgumentNullException.ThrowIfNull(ptr);
            ArgumentNullException.ThrowIfNull(structure);

            MethodTable* pMT = RuntimeHelpers.GetMethodTable(structure);

            if (pMT->HasInstantiation)
                throw new ArgumentException(SR.Argument_NeedNonGenericObject, nameof(structure));

            delegate*<ref byte, byte*, int, ref CleanupWorkListElement?, void> structMarshalStub;
            nuint size;
            if (!TryGetStructMarshalStub((IntPtr)pMT, &structMarshalStub, &size))
                throw new ArgumentException(SR.Argument_MustHaveLayoutOrBeBlittable, nameof(structure));

            if (structMarshalStub != null)
            {
                if (fDeleteOld)
                {
                    structMarshalStub(ref structure.GetRawData(), (byte*)ptr, MarshalOperation.Cleanup, ref Unsafe.NullRef<CleanupWorkListElement?>());
                }

                structMarshalStub(ref structure.GetRawData(), (byte*)ptr, MarshalOperation.Marshal, ref Unsafe.NullRef<CleanupWorkListElement?>());
            }
            else
            {
                Buffer.Memmove(ref *(byte*)ptr, ref structure.GetRawData(), size);
            }
        }

        /// <summary>
        /// Helper function to copy a pointer into a preallocated structure.
        /// </summary>
        private static unsafe void PtrToStructureHelper(IntPtr ptr, object structure, bool allowValueClasses)
        {
            MethodTable* pMT = RuntimeHelpers.GetMethodTable(structure);

            if (!allowValueClasses && pMT->IsValueType)
                throw new ArgumentException(SR.Argument_StructMustNotBeValueClass, nameof(structure));

            delegate*<ref byte, byte*, int, ref CleanupWorkListElement?, void> structMarshalStub;
            nuint size;
            if (!TryGetStructMarshalStub((IntPtr)pMT, &structMarshalStub, &size))
                throw new ArgumentException(SR.Argument_MustHaveLayoutOrBeBlittable, nameof(structure));

            if (structMarshalStub != null)
            {
                structMarshalStub(ref structure.GetRawData(), (byte*)ptr, MarshalOperation.Unmarshal, ref Unsafe.NullRef<CleanupWorkListElement?>());
            }
            else
            {
                Buffer.Memmove(ref structure.GetRawData(), ref *(byte*)ptr, size);
            }
        }

        /// <summary>
        /// Frees all substructures pointed to by the native memory block.
        /// nameof(structuretype) is used to provide layout information.
        /// </summary>
        [RequiresDynamicCode("Marshalling code for the object might not be available. Use the DestroyStructure<T> overload instead.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static unsafe void DestroyStructure(IntPtr ptr, Type structuretype)
        {
            ArgumentNullException.ThrowIfNull(ptr);
            ArgumentNullException.ThrowIfNull(structuretype);

            if (structuretype is not RuntimeType rt)
                throw new ArgumentException(SR.Argument_MustBeRuntimeType, nameof(structuretype));

            if (rt.IsGenericType)
                throw new ArgumentException(SR.Argument_NeedNonGenericType, nameof(structuretype));

            delegate*<ref byte, byte*, int, ref CleanupWorkListElement?, void> structMarshalStub;
            nuint size;
            if (!TryGetStructMarshalStub(rt.GetUnderlyingNativeHandle(), &structMarshalStub, &size))
                throw new ArgumentException(SR.Argument_MustHaveLayoutOrBeBlittable, nameof(structuretype));

            GC.KeepAlive(rt);

            if (structMarshalStub != null)
            {
                structMarshalStub(ref Unsafe.NullRef<byte>(), (byte*)ptr, MarshalOperation.Cleanup, ref Unsafe.NullRef<CleanupWorkListElement?>());
            }
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "MarshalNative_TryGetStructMarshalStub")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static unsafe partial bool TryGetStructMarshalStub(IntPtr th, delegate*<ref byte, byte*, int, ref CleanupWorkListElement?, void>* structMarshalStub, nuint* size);

        // Note: Callers are required to keep obj alive
        internal static unsafe bool IsPinnable(object? obj)
            => (obj == null) || !RuntimeHelpers.GetMethodTable(obj)->ContainsGCPointers;

#if TARGET_WINDOWS
        [FeatureCheck(typeof(RequiresUnreferencedCodeAttribute))]
        internal static bool IsBuiltInComSupported { get; } = IsBuiltInComSupportedInternal();

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "MarshalNative_IsBuiltInComSupported")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool IsBuiltInComSupportedInternal();

        /// <summary>
        /// Returns the HInstance for this module.  Returns -1 if the module doesn't have
        /// an HInstance.  In Memory (Dynamic) Modules won't have an HInstance.
        /// </summary>
        [RequiresAssemblyFiles("Windows only assigns HINSTANCE to assemblies loaded from disk. " +
            "This API will return -1 for modules without a file on disk.")]
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

        internal static Exception GetExceptionForHRInternal(int errorCode, IntPtr errorInfo)
        {
            Exception? exception = null;
            GetExceptionForHRInternal(errorCode, errorInfo, ObjectHandleOnStack.Create(ref exception));
            return exception!;
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "MarshalNative_GetExceptionForHR")]
        private static partial void GetExceptionForHRInternal(int errorCode, IntPtr errorInfo, ObjectHandleOnStack exception);

#if FEATURE_COMINTEROP
        /// <summary>
        /// Converts the CLR exception to an HRESULT. This function also sets
        /// up an IErrorInfo for the exception.
        /// </summary>
        public static int GetHRForException(Exception? e)
            => GetHRForException(ObjectHandleOnStack.Create(ref e));

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "MarshalNative_GetHRForException")]
        private static partial int GetHRForException(ObjectHandleOnStack exception);

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

#pragma warning disable IDE0060
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
#pragma warning restore IDE0060

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

            return GetIUnknownForObject(ObjectHandleOnStack.Create(ref o));
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "MarshalNative_GetIUnknownForObject")]
        private static partial IntPtr /* IUnknown* */ GetIUnknownForObject(ObjectHandleOnStack o);

        /// <summary>
        /// Return the IDispatch* for an Object.
        /// </summary>
        [SupportedOSPlatform("windows")]
        public static IntPtr /* IDispatch */ GetIDispatchForObject(object o)
        {
            ArgumentNullException.ThrowIfNull(o);

            return GetIDispatchForObject(ObjectHandleOnStack.Create(ref o));
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "MarshalNative_GetIDispatchForObject")]
        private static partial IntPtr /* IDispatch* */ GetIDispatchForObject(ObjectHandleOnStack o);

        /// <summary>
        /// Return the IUnknown* representing the interface for the Object.
        /// Object o should support Type T
        /// </summary>
        [SupportedOSPlatform("windows")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static IntPtr /* IUnknown* */ GetComInterfaceForObject(object o, Type T)
            => GetComInterfaceForObject(o, T, CustomQueryInterfaceMode.Allow);

        [SupportedOSPlatform("windows")]
        public static IntPtr GetComInterfaceForObject<T, TInterface>([DisallowNull] T o)
            => GetComInterfaceForObject(o!, typeof(TInterface), CustomQueryInterfaceMode.Allow);

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

            if (T is not RuntimeType rt)
                throw new ArgumentException(SR.Argument_MustBeRuntimeType, nameof(T));

            return GetComInterfaceForObject(ObjectHandleOnStack.Create(ref o), new QCallTypeHandle(ref rt), fEnableCustomizedQueryInterface: mode == CustomQueryInterfaceMode.Allow);
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "MarshalNative_GetComInterfaceForObject")]
        private static partial IntPtr /* IUnknown* */ GetComInterfaceForObject(ObjectHandleOnStack o, QCallTypeHandle t, [MarshalAs(UnmanagedType.Bool)] bool fEnableCustomizedQueryInterface);

        /// <summary>
        /// Return the managed object representing the IUnknown*
        /// </summary>
        [SupportedOSPlatform("windows")]
        public static object GetObjectForIUnknown(IntPtr /* IUnknown* */ pUnk)
        {
            ArgumentNullException.ThrowIfNull(pUnk);

            object? retObject = null;
            GetObjectForIUnknown(pUnk, ObjectHandleOnStack.Create(ref retObject));
            return retObject!;
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "MarshalNative_GetObjectForIUnknown")]
        private static partial void GetObjectForIUnknown(IntPtr /* IUnknown* */ pUnk, ObjectHandleOnStack retObject);

        [SupportedOSPlatform("windows")]
        public static object GetUniqueObjectForIUnknown(IntPtr unknown)
        {
            ArgumentNullException.ThrowIfNull(unknown);

            object? retObject = null;
            GetUniqueObjectForIUnknown(unknown, ObjectHandleOnStack.Create(ref retObject));
            return retObject!;
        }

        /// <summary>
        /// Return a unique Object given an IUnknown.  This ensures that you receive a fresh
        /// object (we will not look in the cache to match up this IUnknown to an already
        /// existing object). This is useful in cases where you want to be able to call
        /// ReleaseComObject on a RCW and not worry about other active uses ofsaid RCW.
        /// </summary>
        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "MarshalNative_GetUniqueObjectForIUnknown")]
        private static partial void GetUniqueObjectForIUnknown(IntPtr unknown, ObjectHandleOnStack retObject);

        /// <summary>
        /// Return an Object for IUnknown, using the Type T.
        /// Type T should be either a COM imported Type or a sub-type of COM imported Type
        /// </summary>
        [SupportedOSPlatform("windows")]
        public static object GetTypedObjectForIUnknown(IntPtr /* IUnknown* */ pUnk, Type t)
        {
            ArgumentNullException.ThrowIfNull(pUnk);
            ArgumentNullException.ThrowIfNull(t);

            if (t is not RuntimeType rt)
                throw new ArgumentException(SR.Argument_MustBeRuntimeType, nameof(t));

            object? retObject = null;
            GetTypedObjectForIUnknown(pUnk, new QCallTypeHandle(ref rt), ObjectHandleOnStack.Create(ref retObject));
            return retObject!;
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "MarshalNative_GetTypedObjectForIUnknown")]
        private static partial void GetTypedObjectForIUnknown(IntPtr /* IUnknown* */ pUnk, QCallTypeHandle t, ObjectHandleOnStack retObject);

        [SupportedOSPlatform("windows")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static IntPtr CreateAggregatedObject(IntPtr pOuter, object o)
        {
            if (!IsBuiltInComSupported)
            {
                throw new NotSupportedException(SR.NotSupported_COM);
            }

            ArgumentNullException.ThrowIfNull(pOuter);
            ArgumentNullException.ThrowIfNull(o);

            return CreateAggregatedObject(pOuter, ObjectHandleOnStack.Create(ref o));
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "MarshalNative_CreateAggregatedObject")]
        private static partial IntPtr CreateAggregatedObject(IntPtr pOuter, ObjectHandleOnStack o);

        [SupportedOSPlatform("windows")]
        public static IntPtr CreateAggregatedObject<T>(IntPtr pOuter, T o) where T : notnull
        {
            if (!IsBuiltInComSupported)
            {
                throw new NotSupportedException(SR.NotSupported_COM);
            }

            return CreateAggregatedObject(pOuter, (object)o);
        }

        public static void CleanupUnusedObjectsInCurrentContext()
            => InternalCleanupUnusedObjectsInCurrentContext();

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "MarshalNative_CleanupUnusedObjectsInCurrentContext")]
        private static partial void InternalCleanupUnusedObjectsInCurrentContext();

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

            return ReleaseComObject(ObjectHandleOnStack.Create(ref co));
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "MarshalNative_ReleaseComObject")]
        private static partial int ReleaseComObject(ObjectHandleOnStack o);

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

            FinalReleaseComObject(ObjectHandleOnStack.Create(ref co));
            return 0;
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "MarshalNative_FinalReleaseComObject")]
        private static partial void FinalReleaseComObject(ObjectHandleOnStack o);

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
        [return: NotNullIfNotNull(nameof(o))]
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
                    // Another thread already cached the wrapper so use that one instead.
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

        private static object InternalCreateWrapperOfType(object o, Type t)
        {
            if (t is not RuntimeType rt)
                throw new ArgumentException(SR.Argument_MustBeRuntimeType, nameof(t));

            object? retObject = null;
            InternalCreateWrapperOfType(ObjectHandleOnStack.Create(ref o), new QCallTypeHandle(ref rt), ObjectHandleOnStack.Create(ref retObject));
            return retObject!;
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "MarshalNative_InternalCreateWrapperOfType")]
        private static partial void InternalCreateWrapperOfType(ObjectHandleOnStack o, QCallTypeHandle rt, ObjectHandleOnStack retObject);

        /// <summary>
        /// check if the type is visible from COM.
        /// </summary>
        public static bool IsTypeVisibleFromCom(Type t)
        {
            ArgumentNullException.ThrowIfNull(t);

            if (t is not RuntimeType rt)
                throw new ArgumentException(SR.Argument_MustBeRuntimeType, nameof(t));

            return IsTypeVisibleFromCom(new QCallTypeHandle(ref rt));
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "MarshalNative_IsTypeVisibleFromCom")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool IsTypeVisibleFromCom(QCallTypeHandle rt);

        [SupportedOSPlatform("windows")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void GetNativeVariantForObject(object? obj, /* VARIANT * */ IntPtr pDstNativeVariant)
        {
            if (!IsBuiltInComSupported)
            {
                throw new NotSupportedException(SR.NotSupported_COM);
            }

            ArgumentNullException.ThrowIfNull(pDstNativeVariant);

            GetNativeVariantForObject(ObjectHandleOnStack.Create(ref obj), pDstNativeVariant);
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "MarshalNative_GetNativeVariantForObject")]
        private static partial void GetNativeVariantForObject(ObjectHandleOnStack obj, /* VARIANT * */ IntPtr pDstNativeVariant);

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

            ArgumentNullException.ThrowIfNull(pSrcNativeVariant);

            object? retObject = null;
            GetObjectForNativeVariant(pSrcNativeVariant, ObjectHandleOnStack.Create(ref retObject));
            return retObject;
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "MarshalNative_GetObjectForNativeVariant")]
        private static partial void GetObjectForNativeVariant(/* VARIANT * */ IntPtr pSrcNativeVariant, ObjectHandleOnStack retObject);

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

            ArgumentNullException.ThrowIfNull(aSrcNativeVariant);
            ArgumentOutOfRangeException.ThrowIfNegative(cVars);

            object?[]? retArray = null;
            GetObjectsForNativeVariants(aSrcNativeVariant, cVars, ObjectHandleOnStack.Create(ref retArray));
            return retArray!;
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "MarshalNative_GetObjectsForNativeVariants")]
        private static partial void GetObjectsForNativeVariants(/* VARIANT * */ IntPtr aSrcNativeVariant, int cVars, ObjectHandleOnStack retArray);

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
        public static int GetStartComSlot(Type t)
        {
            ArgumentNullException.ThrowIfNull(t);

            if (t is not RuntimeType rt)
                throw new ArgumentException(SR.Argument_MustBeRuntimeType, nameof(t));

            return GetStartComSlot(new QCallTypeHandle(ref rt));
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "MarshalNative_GetStartComSlot")]
        private static partial int GetStartComSlot(QCallTypeHandle rt);

        /// <summary>
        /// <para>Returns the last valid COM slot that GetMethodInfoForSlot will work on. </para>
        /// </summary>
        [SupportedOSPlatform("windows")]
        public static int GetEndComSlot(Type t)
        {
            ArgumentNullException.ThrowIfNull(t);

            if (t is not RuntimeType rt)
                throw new ArgumentException(SR.Argument_MustBeRuntimeType, nameof(t));

            return GetEndComSlot(new QCallTypeHandle(ref rt));
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "MarshalNative_GetEndComSlot")]
        private static partial int GetEndComSlot(QCallTypeHandle rt);

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
        [LibraryImport(Interop.Libraries.Ole32)]
        private static partial int CreateBindCtx(uint reserved, out IntPtr ppbc);

        [LibraryImport(Interop.Libraries.Ole32)]
        private static partial int MkParseDisplayName(IntPtr pbc, [MarshalAs(UnmanagedType.LPWStr)] string szUserName, out uint pchEaten, out IntPtr ppmk);

        [LibraryImport(Interop.Libraries.Ole32)]
        private static partial int BindMoniker(IntPtr pmk, uint grfOpt, ref Guid iidResult, out IntPtr ppvResult);

        [SupportedOSPlatform("windows")]
        public static void ChangeWrapperHandleStrength(object otp, bool fIsWeak)
        {
            ArgumentNullException.ThrowIfNull(otp);

            ChangeWrapperHandleStrength(ObjectHandleOnStack.Create(ref otp), fIsWeak);
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "MarshalNative_ChangeWrapperHandleStrength")]
        private static partial void ChangeWrapperHandleStrength(ObjectHandleOnStack otp, [MarshalAs(UnmanagedType.Bool)] bool fIsWeak);
#endif // FEATURE_COMINTEROP

        internal static Delegate GetDelegateForFunctionPointerInternal(IntPtr ptr, RuntimeType t)
        {
            Delegate? retDelegate = null;
            GetDelegateForFunctionPointerInternal(ptr, new QCallTypeHandle(ref t), ObjectHandleOnStack.Create(ref retDelegate));
            return retDelegate!;
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "MarshalNative_GetDelegateForFunctionPointerInternal")]
        private static partial void GetDelegateForFunctionPointerInternal(IntPtr ptr, QCallTypeHandle t, ObjectHandleOnStack retDelegate);

        internal static IntPtr GetFunctionPointerForDelegateInternal(Delegate d)
        {
            return GetFunctionPointerForDelegateInternal(ObjectHandleOnStack.Create(ref d));
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "MarshalNative_GetFunctionPointerForDelegateInternal")]
        private static partial IntPtr GetFunctionPointerForDelegateInternal(ObjectHandleOnStack d);

#if DEBUG // Used for testing in Checked or Debug
        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "MarshalNative_GetIsInCooperativeGCModeFunctionPointer")]
        internal static unsafe partial delegate* unmanaged<int> GetIsInCooperativeGCModeFunctionPointer();
#endif
    }
}
