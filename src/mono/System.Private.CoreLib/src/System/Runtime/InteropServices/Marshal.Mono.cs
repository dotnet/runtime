// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;

namespace System.Runtime.InteropServices
{
    public partial class Marshal
    {
        /// <summary>
        /// Get the last platform invoke error on the current thread
        /// </summary>
        /// <returns>The last platform invoke error</returns>
        /// <remarks>
        /// The last platform invoke error corresponds to the error set by either the most recent platform
        /// invoke that was configured to set the last error or a call to <see cref="SetLastPInvokeError(int)" />.
        /// </remarks>
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public static extern int GetLastPInvokeError();

        /// <summary>
        /// Set the last platform invoke error on the current thread
        /// </summary>
        /// <param name="error">Error to set</param>
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public static extern void SetLastPInvokeError(int error);

        [RequiresDynamicCode("Marshalling code for the object might not be available. Use the DestroyStructure<T> overload instead.")]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static extern void DestroyStructure(IntPtr ptr, Type structuretype);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static extern IntPtr OffsetOf(Type t, string fieldName);

        [RequiresDynamicCode("Marshalling code for the object might not be available. Use the StructureToPtr<T> overload instead.")]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static extern void StructureToPtr(object structure, IntPtr ptr, bool fDeleteOld);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern bool IsPinnableType(QCallTypeHandle type);

        internal static bool IsPinnable(object? obj)
        {
            if (obj == null || obj is string)
                return true;
            var type = (obj.GetType() as RuntimeType)!;
            return IsPinnableType(new QCallTypeHandle(ref type));
            //Type type = obj.GetType ();
            //return !type.IsValueType || RuntimeTypeHandle.HasReferences (type as RuntimeType);
        }

        private static void PrelinkCore(MethodInfo m)
        {
            if (!(m is RuntimeMethodInfo))
            {
                throw new ArgumentException(SR.Argument_MustBeRuntimeMethodInfo, nameof(m));
            }

            PrelinkInternal(m);
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void PtrToStructureInternal(IntPtr ptr, object structure, bool allowValueClasses);

        private static void PtrToStructureHelper(IntPtr ptr, object? structure, bool allowValueClasses)
        {
            if (structure == null)
                throw new ArgumentNullException(nameof(structure));
            if (ptr == IntPtr.Zero)
                throw new ArgumentNullException(nameof(ptr));
            PtrToStructureInternal(ptr, structure, allowValueClasses);
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern void GetDelegateForFunctionPointerInternal(QCallTypeHandle t, IntPtr ptr, ObjectHandleOnStack res);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern IntPtr GetFunctionPointerForDelegateInternal(Delegate d);

        private static Delegate GetDelegateForFunctionPointerInternal(IntPtr ptr, Type t)
        {
            RuntimeType rttype = (RuntimeType)t;
            Delegate? res = null;
            GetDelegateForFunctionPointerInternal(new QCallTypeHandle(ref rttype), ptr, ObjectHandleOnStack.Create(ref res));
            return res!;
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void PrelinkInternal(MethodInfo m);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern int SizeOfHelper(QCallTypeHandle t, bool throwIfNotMarshalable);

        private static int SizeOfHelper(Type t, bool throwIfNotMarshalable)
        {
            RuntimeType rttype = (RuntimeType)t;
            return SizeOfHelper(new QCallTypeHandle(ref rttype), throwIfNotMarshalable);
        }

        public static IntPtr GetExceptionPointers()
        {
            throw new PlatformNotSupportedException();
        }

        private sealed class MarshalerInstanceKeyComparer : IEqualityComparer<(Type, string)>
        {
            public bool Equals((Type, string) lhs, (Type, string) rhs)
            {
                return lhs.CompareTo(rhs) == 0;
            }

            public int GetHashCode((Type, string) key)
            {
                return key.GetHashCode();
            }
        }

        private static Dictionary<(Type, string), ICustomMarshaler>? MarshalerInstanceCache;

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2070:UnrecognizedReflectionPattern",
            Justification = "Implementation detail of MarshalAs.CustomMarshaler")]
        internal static ICustomMarshaler? GetCustomMarshalerInstance(Type type, string cookie)
        {
            var key = (type, cookie);

            Dictionary<(Type, string), ICustomMarshaler> cache =
                Volatile.Read(ref MarshalerInstanceCache) ??
                Interlocked.CompareExchange(ref MarshalerInstanceCache, new Dictionary<(Type, string), ICustomMarshaler>(new MarshalerInstanceKeyComparer()), null) ??
                MarshalerInstanceCache;

            ICustomMarshaler? result;
            bool gotExistingInstance;
            lock (cache)
                gotExistingInstance = cache.TryGetValue(key, out result);

            if (!gotExistingInstance)
            {
                RuntimeMethodInfo? getInstanceMethod;
                try
                {
                    getInstanceMethod = (RuntimeMethodInfo?)type.GetMethod(
                        "GetInstance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.InvokeMethod,
                        null, new Type[] { typeof(string) }, null
                    );
                }
                catch (AmbiguousMatchException)
                {
                    throw new ApplicationException($"Custom marshaler '{type.FullName}' implements multiple static GetInstance methods that take a single string parameter.");
                }

                if ((getInstanceMethod == null) ||
                    (getInstanceMethod.ReturnType != typeof(ICustomMarshaler)))
                {
                    throw new ApplicationException($"Custom marshaler '{type.FullName}' does not implement a static GetInstance method that takes a single string parameter and returns an ICustomMarshaler.");
                }

                if (getInstanceMethod.ContainsGenericParameters)
                {
                    throw new System.TypeLoadException($"Custom marshaler '{type.FullName}' contains unassigned generic type parameter(s).");
                }

                Exception? exc;
                try
                {
                    result = (ICustomMarshaler?)getInstanceMethod.InternalInvoke(null, new object[] { cookie }, out exc);
                }
                catch (Exception e)
                {
                    // FIXME: mscorlib's legacyUnhandledExceptionPolicy is apparently 1,
                    //  so exceptions are thrown instead of being passed through the outparam
                    exc = e;
                    result = null;
                }

                if (exc != null)
                {
                    var edi = System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(exc);
                    edi.Throw();
                }

                if (result == null)
                    throw new ApplicationException($"A call to GetInstance() for custom marshaler '{type.FullName}' returned null, which is not allowed.");

                lock (cache)
                    cache[key] = result;
            }

            return result;
        }

        #region PlatformNotSupported

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("GetExceptionCode() may be unavailable in future releases.")]
        public static int GetExceptionCode()
        {
            throw new PlatformNotSupportedException();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("ReadByte(Object, Int32) may be unavailable in future releases.")]
        [RequiresDynamicCode("Marshalling code for the object might not be available")]
        public static byte ReadByte(object ptr, int ofs)
        {
            throw new PlatformNotSupportedException();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("ReadInt16(Object, Int32) may be unavailable in future releases.")]
        [RequiresDynamicCode("Marshalling code for the object might not be available")]
        public static short ReadInt16(object ptr, int ofs)
        {
            throw new PlatformNotSupportedException();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("ReadInt32(Object, Int32) may be unavailable in future releases.")]
        [RequiresDynamicCode("Marshalling code for the object might not be available")]
        public static int ReadInt32(object ptr, int ofs)
        {
            throw new PlatformNotSupportedException();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("ReadInt64(Object, Int32) may be unavailable in future releases.")]
        [RequiresDynamicCode("Marshalling code for the object might not be available")]
        public static long ReadInt64(object ptr, int ofs)
        {
            throw new PlatformNotSupportedException();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("WriteByte(Object, Int32, Byte) may be unavailable in future releases.")]
        [RequiresDynamicCode("Marshalling code for the object might not be available")]
        public static void WriteByte(object ptr, int ofs, byte val)
        {
            throw new PlatformNotSupportedException();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("WriteInt16(Object, Int32, Int16) may be unavailable in future releases.")]
        [RequiresDynamicCode("Marshalling code for the object might not be available")]
        public static void WriteInt16(object ptr, int ofs, short val)
        {
            throw new PlatformNotSupportedException();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("WriteInt32(Object, Int32, Int32) may be unavailable in future releases.")]
        [RequiresDynamicCode("Marshalling code for the object might not be available")]
        public static void WriteInt32(object ptr, int ofs, int val)
        {
            throw new PlatformNotSupportedException();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("WriteInt64(Object, Int32, Int64) may be unavailable in future releases.")]
        [RequiresDynamicCode("Marshalling code for the object might not be available")]
        public static void WriteInt64(object ptr, int ofs, long val)
        {
            throw new PlatformNotSupportedException();
        }

        #endregion
    }
}
