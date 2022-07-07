// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Security;
using System.Text;

namespace System.Runtime.InteropServices
{
    /// <summary>
    /// This class contains methods that are mainly used to marshal between unmanaged
    /// and managed types.
    /// </summary>
    public static partial class Marshal
    {
        /// <summary>
        /// The default character size for the system. This is always 2 because
        /// the framework only runs on UTF-16 systems.
        /// </summary>
        public static readonly int SystemDefaultCharSize = 2;

        /// <summary>
        /// The max DBCS character size for the system.
        /// </summary>
        public static readonly int SystemMaxDBCSCharSize = GetSystemMaxDBCSCharSize();

        public static IntPtr AllocHGlobal(int cb) => AllocHGlobal((nint)cb);

        public static unsafe string? PtrToStringAnsi(IntPtr ptr)
        {
            if (IsNullOrWin32Atom(ptr))
            {
                return null;
            }

            return new string((sbyte*)ptr);
        }

        public static unsafe string PtrToStringAnsi(IntPtr ptr, int len)
        {
            ArgumentNullException.ThrowIfNull(ptr);
            if (len < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(len), len, SR.ArgumentOutOfRange_NeedNonNegNum);
            }

            return new string((sbyte*)ptr, 0, len);
        }

        public static unsafe string? PtrToStringUni(IntPtr ptr)
        {
            if (IsNullOrWin32Atom(ptr))
            {
                return null;
            }

            return new string((char*)ptr);
        }

        public static unsafe string PtrToStringUni(IntPtr ptr, int len)
        {
            ArgumentNullException.ThrowIfNull(ptr);
            if (len < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(len), len, SR.ArgumentOutOfRange_NeedNonNegNum);
            }

            return new string((char*)ptr, 0, len);
        }

        public static unsafe string? PtrToStringUTF8(IntPtr ptr)
        {
            if (IsNullOrWin32Atom(ptr))
            {
                return null;
            }

            int nbBytes = string.strlen((byte*)ptr);
            return string.CreateStringFromEncoding((byte*)ptr, nbBytes, Encoding.UTF8);
        }

        public static unsafe string PtrToStringUTF8(IntPtr ptr, int byteLen)
        {
            ArgumentNullException.ThrowIfNull(ptr);
            if (byteLen < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(byteLen), byteLen, SR.ArgumentOutOfRange_NeedNonNegNum);
            }

            return string.CreateStringFromEncoding((byte*)ptr, byteLen, Encoding.UTF8);
        }

        [RequiresDynamicCode("Marshalling code for the object might not be available. Use the SizeOf<T> overload instead.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static int SizeOf(object structure)
        {
            ArgumentNullException.ThrowIfNull(structure);

            return SizeOfHelper(structure.GetType(), throwIfNotMarshalable: true);
        }

        public static int SizeOf<T>(T structure)
        {
            ArgumentNullException.ThrowIfNull(structure);

            return SizeOfHelper(structure.GetType(), throwIfNotMarshalable: true);
        }

        [RequiresDynamicCode("Marshalling code for the object might not be available. Use the SizeOf<T> overload instead.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static int SizeOf(Type t)
        {
            ArgumentNullException.ThrowIfNull(t);

            if (t is not RuntimeType)
            {
                throw new ArgumentException(SR.Argument_MustBeRuntimeType, nameof(t));
            }
            if (t.IsGenericType)
            {
                throw new ArgumentException(SR.Argument_NeedNonGenericType, nameof(t));
            }

            return SizeOfHelper(t, throwIfNotMarshalable: true);
        }

        public static int SizeOf<T>()
        {
            Type t = typeof(T);
            if (t.IsGenericType)
            {
                throw new ArgumentException(SR.Argument_NeedNonGenericType, nameof(T));
            }

            return SizeOfHelper(t, throwIfNotMarshalable: true);
        }

        public static unsafe int QueryInterface(IntPtr pUnk, ref Guid iid, out IntPtr ppv)
        {
            ArgumentNullException.ThrowIfNull(pUnk);

            fixed (Guid* pIID = &iid)
            fixed (IntPtr* p = &ppv)
            {
                return ((delegate* unmanaged<IntPtr, Guid*, IntPtr*, int>)(*(*(void***)pUnk + 0 /* IUnknown.QueryInterface slot */)))(pUnk, pIID, p);
            }
        }

        public static unsafe int AddRef(IntPtr pUnk)
        {
            ArgumentNullException.ThrowIfNull(pUnk);

            return ((delegate* unmanaged<IntPtr, int>)(*(*(void***)pUnk + 1 /* IUnknown.AddRef slot */)))(pUnk);
        }

        public static unsafe int Release(IntPtr pUnk)
        {
            ArgumentNullException.ThrowIfNull(pUnk);

            return ((delegate* unmanaged<IntPtr, int>)(*(*(void***)pUnk + 2 /* IUnknown.Release slot */)))(pUnk);
        }

        /// <summary>
        /// IMPORTANT NOTICE: This method does not do any verification on the array.
        /// It must be used with EXTREME CAUTION since passing in invalid index or
        /// an array that is not pinned can cause unexpected results.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static unsafe IntPtr UnsafeAddrOfPinnedArrayElement(Array arr, int index)
        {
            ArgumentNullException.ThrowIfNull(arr);

            void* pRawData = Unsafe.AsPointer(ref MemoryMarshal.GetArrayDataReference(arr));
            return (IntPtr)((byte*)pRawData + (uint)index * (nuint)arr.GetElementSize());
        }

        public static unsafe IntPtr UnsafeAddrOfPinnedArrayElement<T>(T[] arr, int index)
        {
            ArgumentNullException.ThrowIfNull(arr);

            void* pRawData = Unsafe.AsPointer(ref MemoryMarshal.GetArrayDataReference(arr));
            return (IntPtr)((byte*)pRawData + (uint)index * (nuint)Unsafe.SizeOf<T>());
        }

        public static IntPtr OffsetOf<T>(string fieldName) => OffsetOf(typeof(T), fieldName);

        public static void Copy(int[] source, int startIndex, IntPtr destination, int length)
        {
            CopyToNative(source, startIndex, destination, length);
        }

        public static void Copy(char[] source, int startIndex, IntPtr destination, int length)
        {
            CopyToNative(source, startIndex, destination, length);
        }

        public static void Copy(short[] source, int startIndex, IntPtr destination, int length)
        {
            CopyToNative(source, startIndex, destination, length);
        }

        public static void Copy(long[] source, int startIndex, IntPtr destination, int length)
        {
            CopyToNative(source, startIndex, destination, length);
        }

        public static void Copy(float[] source, int startIndex, IntPtr destination, int length)
        {
            CopyToNative(source, startIndex, destination, length);
        }

        public static void Copy(double[] source, int startIndex, IntPtr destination, int length)
        {
            CopyToNative(source, startIndex, destination, length);
        }

        public static void Copy(byte[] source, int startIndex, IntPtr destination, int length)
        {
            CopyToNative(source, startIndex, destination, length);
        }

        public static void Copy(IntPtr[] source, int startIndex, IntPtr destination, int length)
        {
            CopyToNative(source, startIndex, destination, length);
        }

        private static unsafe void CopyToNative<T>(T[] source, int startIndex, IntPtr destination, int length)
        {
            ArgumentNullException.ThrowIfNull(source);

            ArgumentNullException.ThrowIfNull(destination);

            // The rest of the argument validation is done by CopyTo
            new Span<T>(source, startIndex, length).CopyTo(new Span<T>((void*)destination, length));
        }

        public static void Copy(IntPtr source, int[] destination, int startIndex, int length)
        {
            CopyToManaged(source, destination, startIndex, length);
        }

        public static void Copy(IntPtr source, char[] destination, int startIndex, int length)
        {
            CopyToManaged(source, destination, startIndex, length);
        }

        public static void Copy(IntPtr source, short[] destination, int startIndex, int length)
        {
            CopyToManaged(source, destination, startIndex, length);
        }

        public static void Copy(IntPtr source, long[] destination, int startIndex, int length)
        {
            CopyToManaged(source, destination, startIndex, length);
        }

        public static void Copy(IntPtr source, float[] destination, int startIndex, int length)
        {
            CopyToManaged(source, destination, startIndex, length);
        }

        public static void Copy(IntPtr source, double[] destination, int startIndex, int length)
        {
            CopyToManaged(source, destination, startIndex, length);
        }

        public static void Copy(IntPtr source, byte[] destination, int startIndex, int length)
        {
            CopyToManaged(source, destination, startIndex, length);
        }

        public static void Copy(IntPtr source, IntPtr[] destination, int startIndex, int length)
        {
            CopyToManaged(source, destination, startIndex, length);
        }

        private static unsafe void CopyToManaged<T>(IntPtr source, T[] destination, int startIndex, int length)
        {
            ArgumentNullException.ThrowIfNull(destination);

            ArgumentNullException.ThrowIfNull(source);
            if (startIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(startIndex), SR.ArgumentOutOfRange_StartIndex);
            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length), SR.ArgumentOutOfRange_NeedNonNegNum);

            // The rest of the argument validation is done by CopyTo

            new Span<T>((void*)source, length).CopyTo(new Span<T>(destination, startIndex, length));
        }

        public static unsafe byte ReadByte(IntPtr ptr, int ofs)
        {
            try
            {
                byte* addr = (byte*)ptr + ofs;
                return *addr;
            }
            catch (NullReferenceException)
            {
                // this method is documented to throw AccessViolationException on any AV
                throw new AccessViolationException();
            }
        }

        public static byte ReadByte(IntPtr ptr) => ReadByte(ptr, 0);

        public static unsafe short ReadInt16(IntPtr ptr, int ofs)
        {
            try
            {
                byte* addr = (byte*)ptr + ofs;
                if ((unchecked((int)addr) & 0x1) == 0)
                {
                    // aligned read
                    return *((short*)addr);
                }
                else
                {
                    return Unsafe.ReadUnaligned<short>(addr);
                }
            }
            catch (NullReferenceException)
            {
                // this method is documented to throw AccessViolationException on any AV
                throw new AccessViolationException();
            }
        }

        public static short ReadInt16(IntPtr ptr) => ReadInt16(ptr, 0);

        public static unsafe int ReadInt32(IntPtr ptr, int ofs)
        {
            try
            {
                byte* addr = (byte*)ptr + ofs;
                if ((unchecked((int)addr) & 0x3) == 0)
                {
                    // aligned read
                    return *((int*)addr);
                }
                else
                {
                    return Unsafe.ReadUnaligned<int>(addr);
                }
            }
            catch (NullReferenceException)
            {
                // this method is documented to throw AccessViolationException on any AV
                throw new AccessViolationException();
            }
        }

        public static int ReadInt32(IntPtr ptr) => ReadInt32(ptr, 0);

        [RequiresDynamicCode("Marshalling code for the object might not be available")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("ReadIntPtr(Object, Int32) may be unavailable in future releases.")]
        public static IntPtr ReadIntPtr(object ptr, int ofs)
        {
#if TARGET_64BIT
            return (IntPtr)ReadInt64(ptr, ofs);
#else // 32
            return (IntPtr)ReadInt32(ptr, ofs);
#endif
        }

        public static IntPtr ReadIntPtr(IntPtr ptr, int ofs)
        {
#if TARGET_64BIT
            return (IntPtr)ReadInt64(ptr, ofs);
#else // 32
            return (IntPtr)ReadInt32(ptr, ofs);
#endif
        }

        public static IntPtr ReadIntPtr(IntPtr ptr) => ReadIntPtr(ptr, 0);

        public static unsafe long ReadInt64(IntPtr ptr, int ofs)
        {
            try
            {
                byte* addr = (byte*)ptr + ofs;
                if ((unchecked((int)addr) & 0x7) == 0)
                {
                    // aligned read
                    return *((long*)addr);
                }
                else
                {
                    return Unsafe.ReadUnaligned<long>(addr);
                }
            }
            catch (NullReferenceException)
            {
                // this method is documented to throw AccessViolationException on any AV
                throw new AccessViolationException();
            }
        }

        public static long ReadInt64(IntPtr ptr) => ReadInt64(ptr, 0);

        public static unsafe void WriteByte(IntPtr ptr, int ofs, byte val)
        {
            try
            {
                byte* addr = (byte*)ptr + ofs;
                *addr = val;
            }
            catch (NullReferenceException)
            {
                // this method is documented to throw AccessViolationException on any AV
                throw new AccessViolationException();
            }
        }

        public static void WriteByte(IntPtr ptr, byte val) => WriteByte(ptr, 0, val);

        public static unsafe void WriteInt16(IntPtr ptr, int ofs, short val)
        {
            try
            {
                byte* addr = (byte*)ptr + ofs;
                if ((unchecked((int)addr) & 0x1) == 0)
                {
                    // aligned write
                    *((short*)addr) = val;
                }
                else
                {
                    Unsafe.WriteUnaligned(addr, val);
                }
            }
            catch (NullReferenceException)
            {
                // this method is documented to throw AccessViolationException on any AV
                throw new AccessViolationException();
            }
        }

        public static void WriteInt16(IntPtr ptr, short val) => WriteInt16(ptr, 0, val);

        public static void WriteInt16(IntPtr ptr, int ofs, char val) => WriteInt16(ptr, ofs, (short)val);

        [RequiresDynamicCode("Marshalling code for the object might not be available")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("WriteInt16(Object, Int32, Char) may be unavailable in future releases.")]
        public static void WriteInt16([In, Out]object ptr, int ofs, char val) => WriteInt16(ptr, ofs, (short)val);

        public static void WriteInt16(IntPtr ptr, char val) => WriteInt16(ptr, 0, (short)val);

        public static unsafe void WriteInt32(IntPtr ptr, int ofs, int val)
        {
            try
            {
                byte* addr = (byte*)ptr + ofs;
                if ((unchecked((int)addr) & 0x3) == 0)
                {
                    // aligned write
                    *((int*)addr) = val;
                }
                else
                {
                    Unsafe.WriteUnaligned(addr, val);
                }
            }
            catch (NullReferenceException)
            {
                // this method is documented to throw AccessViolationException on any AV
                throw new AccessViolationException();
            }
        }

        public static void WriteInt32(IntPtr ptr, int val) => WriteInt32(ptr, 0, val);

        public static void WriteIntPtr(IntPtr ptr, int ofs, IntPtr val)
        {
#if TARGET_64BIT
            WriteInt64(ptr, ofs, (long)val);
#else // 32
            WriteInt32(ptr, ofs, (int)val);
#endif
        }

        [RequiresDynamicCode("Marshalling code for the object might not be available")]
        [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("WriteIntPtr(Object, Int32, IntPtr) may be unavailable in future releases.")]
        public static void WriteIntPtr(object ptr, int ofs, IntPtr val)
        {
#if TARGET_64BIT
            WriteInt64(ptr, ofs, (long)val);
#else // 32
            WriteInt32(ptr, ofs, (int)val);
#endif
        }

        public static void WriteIntPtr(IntPtr ptr, IntPtr val) => WriteIntPtr(ptr, 0, val);

        public static unsafe void WriteInt64(IntPtr ptr, int ofs, long val)
        {
            try
            {
                byte* addr = (byte*)ptr + ofs;
                if ((unchecked((int)addr) & 0x7) == 0)
                {
                    // aligned write
                    *((long*)addr) = val;
                }
                else
                {
                    Unsafe.WriteUnaligned(addr, val);
                }
            }
            catch (NullReferenceException)
            {
                // this method is documented to throw AccessViolationException on any AV
                throw new AccessViolationException();
            }
        }

        public static void WriteInt64(IntPtr ptr, long val) => WriteInt64(ptr, 0, val);

        public static void Prelink(MethodInfo m)
        {
            ArgumentNullException.ThrowIfNull(m);

            PrelinkCore(m);
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2070:UnrecognizedReflectionPattern",
            Justification = "This only needs to prelink methods that are actually used")]
        public static void PrelinkAll(Type c)
        {
            ArgumentNullException.ThrowIfNull(c);

            MethodInfo[] mi = c.GetMethods();

            for (int i = 0; i < mi.Length; i++)
            {
                Prelink(mi[i]);
            }
        }

        [UnconditionalSuppressMessage("AotAnalysis", "IL3050:AotUnfriendlyApi",
            Justification = "AOT compilers can see the T.")]
        public static void StructureToPtr<T>([DisallowNull] T structure, IntPtr ptr, bool fDeleteOld)
        {
            StructureToPtr((object)structure!, ptr, fDeleteOld);
        }

        /// <summary>
        /// Creates a new instance of "structuretype" and marshals data from a
        /// native memory block to it.
        /// </summary>
        [RequiresDynamicCode("Marshalling code for the object might not be available")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object? PtrToStructure(IntPtr ptr,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
            Type structureType)
        {
            ArgumentNullException.ThrowIfNull(structureType);

            if (ptr == IntPtr.Zero)
            {
                return null;
            }

            if (structureType.IsGenericType)
            {
                throw new ArgumentException(SR.Argument_NeedNonGenericType, nameof(structureType));
            }
            if (structureType is not RuntimeType)
            {
                throw new ArgumentException(SR.Argument_MustBeRuntimeType, nameof(structureType));
            }

            object structure = Activator.CreateInstance(structureType, nonPublic: true)!;
            PtrToStructureHelper(ptr, structure, allowValueClasses: true);
            return structure;
        }

        /// <summary>
        /// Marshals data from a native memory block to a preallocated structure class.
        /// </summary>
        [RequiresDynamicCode("Marshalling code for the object might not be available")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void PtrToStructure(IntPtr ptr, object structure)
        {
            PtrToStructureHelper(ptr, structure, allowValueClasses: false);
        }

        public static void PtrToStructure<T>(IntPtr ptr, [DisallowNull] T structure)
        {
            PtrToStructureHelper(ptr, structure, allowValueClasses: false);
        }

        public static T? PtrToStructure<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]T>(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero)
            {
                // Compat: this was originally implemented as a call to the non-generic version+cast.
                // It would throw for non-nullable valuetypes here and return null for Nullable<T> even
                // though it's generic.
                return (T)(object)null!;
            }

            Type structureType = typeof(T);
            if (structureType.IsGenericType)
            {
                throw new ArgumentException(SR.Argument_NeedNonGenericType, nameof(T));
            }

            object structure = Activator.CreateInstance(structureType, nonPublic: true)!;
            PtrToStructureHelper(ptr, structure, allowValueClasses: true);
            return (T)structure;
        }

        [UnconditionalSuppressMessage("AotAnalysis", "IL3050:AotUnfriendlyApi",
            Justification = "AOT compilers can see the T.")]
        public static void DestroyStructure<T>(IntPtr ptr) => DestroyStructure(ptr, typeof(T));

        // CoreCLR has a different implementation for Windows only
#if !CORECLR || !TARGET_WINDOWS
        [RequiresAssemblyFiles("Windows only assigns HINSTANCE to assemblies loaded from disk. " +
            "This API will return -1 for modules without a file on disk.")]
        public static IntPtr GetHINSTANCE(Module m)
        {
            ArgumentNullException.ThrowIfNull(m);

            return (IntPtr)(-1);
        }
#endif

        /// <summary>
        /// Converts the HRESULT to a CLR exception.
        /// </summary>
        public static Exception? GetExceptionForHR(int errorCode) => GetExceptionForHR(errorCode, IntPtr.Zero);

        public static Exception? GetExceptionForHR(int errorCode, IntPtr errorInfo)
        {
            if (errorCode >= 0)
            {
                return null;
            }

            return GetExceptionForHRInternal(errorCode, errorInfo);
        }

#if !CORECLR
#pragma warning disable IDE0060
        private static Exception? GetExceptionForHRInternal(int errorCode, IntPtr errorInfo)
        {
            switch (errorCode)
            {
                case HResults.COR_E_AMBIGUOUSMATCH:
                    return new System.Reflection.AmbiguousMatchException();
                case HResults.COR_E_APPLICATION:
                    return new System.ApplicationException();
                case HResults.COR_E_ARGUMENT:
                    return new System.ArgumentException();
                case HResults.COR_E_ARGUMENTOUTOFRANGE:
                    return new System.ArgumentOutOfRangeException();
                case HResults.COR_E_ARITHMETIC:
                    return new System.ArithmeticException();
                case HResults.COR_E_ARRAYTYPEMISMATCH:
                    return new System.ArrayTypeMismatchException();
                case HResults.COR_E_BADEXEFORMAT:
                    return new System.BadImageFormatException();
                case HResults.COR_E_BADIMAGEFORMAT:
                    return new System.BadImageFormatException();
                //case HResults.COR_E_CODECONTRACTFAILED:
                //return new System.Diagnostics.Contracts.ContractException ();
                //case HResults.COR_E_COMEMULATE:
                case HResults.COR_E_CUSTOMATTRIBUTEFORMAT:
                    return new System.Reflection.CustomAttributeFormatException();
                case HResults.COR_E_DATAMISALIGNED:
                    return new System.DataMisalignedException();
                case HResults.COR_E_DIRECTORYNOTFOUND:
                    return new System.IO.DirectoryNotFoundException();
                case HResults.COR_E_DIVIDEBYZERO:
                    return new System.DivideByZeroException();
                case HResults.COR_E_DLLNOTFOUND:
                    return new System.DllNotFoundException();
                case HResults.COR_E_DUPLICATEWAITOBJECT:
                    return new System.DuplicateWaitObjectException();
                case HResults.COR_E_ENDOFSTREAM:
                    return new System.IO.EndOfStreamException();
                case HResults.COR_E_ENTRYPOINTNOTFOUND:
                    return new System.EntryPointNotFoundException();
                case HResults.COR_E_EXCEPTION:
                    return new System.Exception();
                case HResults.COR_E_EXECUTIONENGINE:
#pragma warning disable CS0618 // ExecutionEngineException is obsolete
                    return new System.ExecutionEngineException();
#pragma warning restore CS0618
                case HResults.COR_E_FIELDACCESS:
                    return new System.FieldAccessException();
                case HResults.COR_E_FILELOAD:
                    return new System.IO.FileLoadException();
                case HResults.COR_E_FILENOTFOUND:
                    return new System.IO.FileNotFoundException();
                case HResults.COR_E_FORMAT:
                    return new System.FormatException();
                case HResults.COR_E_INDEXOUTOFRANGE:
                    return new System.IndexOutOfRangeException();
                case HResults.COR_E_INSUFFICIENTEXECUTIONSTACK:
                    return new System.InsufficientExecutionStackException();
                case HResults.COR_E_INVALIDCAST:
                    return new System.InvalidCastException();
                case HResults.COR_E_INVALIDFILTERCRITERIA:
                    return new System.Reflection.InvalidFilterCriteriaException();
                case HResults.COR_E_INVALIDOLEVARIANTTYPE:
                    return new System.Runtime.InteropServices.InvalidOleVariantTypeException();
                case HResults.COR_E_INVALIDOPERATION:
                    return new System.InvalidOperationException();
                case HResults.COR_E_INVALIDPROGRAM:
                    return new System.InvalidProgramException();
                case HResults.COR_E_IO:
                    return new System.IO.IOException();
                case HResults.COR_E_MARSHALDIRECTIVE:
                    return new System.Runtime.InteropServices.MarshalDirectiveException();
                case HResults.COR_E_MEMBERACCESS:
                    return new System.MemberAccessException();
                case HResults.COR_E_METHODACCESS:
                    return new System.MethodAccessException();
                case HResults.COR_E_MISSINGFIELD:
                    return new System.MissingFieldException();
                case HResults.COR_E_MISSINGMANIFESTRESOURCE:
                    return new System.Resources.MissingManifestResourceException();
                case HResults.COR_E_MISSINGMEMBER:
                    return new System.MissingMemberException();
                case HResults.COR_E_MISSINGMETHOD:
                    return new System.MissingMethodException();
                case HResults.COR_E_MULTICASTNOTSUPPORTED:
                    return new System.MulticastNotSupportedException();
                case HResults.COR_E_NOTFINITENUMBER:
                    return new System.NotFiniteNumberException();
                case HResults.COR_E_NOTSUPPORTED:
                    return new System.NotSupportedException();
                case HResults.E_POINTER:
                    return new System.NullReferenceException();
                case HResults.COR_E_OBJECTDISPOSED:
                    return new System.ObjectDisposedException("");
                case HResults.COR_E_OPERATIONCANCELED:
                    return new System.OperationCanceledException();
                case HResults.COR_E_OUTOFMEMORY:
                    return new System.OutOfMemoryException();
                case HResults.COR_E_OVERFLOW:
                    return new System.OverflowException();
                case HResults.COR_E_PATHTOOLONG:
                    return new System.IO.PathTooLongException();
                case HResults.COR_E_PLATFORMNOTSUPPORTED:
                    return new System.PlatformNotSupportedException();
                case HResults.COR_E_RANK:
                    return new System.RankException();
                case HResults.COR_E_REFLECTIONTYPELOAD:
                    return new System.MissingMethodException();
                case HResults.COR_E_RUNTIMEWRAPPED:
                    return new System.MissingMethodException();
                case HResults.COR_E_SECURITY:
                    return new System.Security.SecurityException();
                case HResults.COR_E_SERIALIZATION:
                    return new System.Runtime.Serialization.SerializationException();
                case HResults.COR_E_STACKOVERFLOW:
                    return new System.StackOverflowException();
                case HResults.COR_E_SYNCHRONIZATIONLOCK:
                    return new System.Threading.SynchronizationLockException();
                case HResults.COR_E_SYSTEM:
                    return new System.SystemException();
                case HResults.COR_E_TARGET:
                    return new System.Reflection.TargetException();
                case HResults.COR_E_TARGETINVOCATION:
                    return new System.MissingMethodException();
                case HResults.COR_E_TARGETPARAMCOUNT:
                    return new System.Reflection.TargetParameterCountException();
                case HResults.COR_E_THREADABORTED:
                    return new System.Threading.ThreadAbortException();
                case HResults.COR_E_THREADINTERRUPTED:
                    return new System.Threading.ThreadInterruptedException();
                case HResults.COR_E_THREADSTART:
                    return new System.Threading.ThreadStartException();
                case HResults.COR_E_THREADSTATE:
                    return new System.Threading.ThreadStateException();
                case HResults.COR_E_TYPEACCESS:
                    return new System.TypeAccessException();
                case HResults.COR_E_TYPEINITIALIZATION:
                    return new System.TypeInitializationException("");
                case HResults.COR_E_TYPELOAD:
                    return new System.TypeLoadException();
                case HResults.COR_E_TYPEUNLOADED:
                    return new System.TypeUnloadedException();
                case HResults.COR_E_UNAUTHORIZEDACCESS:
                    return new System.UnauthorizedAccessException();
                //case HResults.COR_E_UNSUPPORTEDFORMAT:
                case HResults.COR_E_VERIFICATION:
                    return new System.Security.VerificationException();
                //case HResults.E_INVALIDARG:
                case HResults.E_NOTIMPL:
                    return new System.NotImplementedException();
                //case HResults.E_POINTER:
                case HResults.RO_E_CLOSED:
                    return new System.ObjectDisposedException("");
                case HResults.COR_E_ABANDONEDMUTEX:
                case HResults.COR_E_AMBIGUOUSIMPLEMENTATION:
                case HResults.COR_E_CANNOTUNLOADAPPDOMAIN:
                case HResults.COR_E_CONTEXTMARSHAL:
                //case HResults.COR_E_HOSTPROTECTION:
                case HResults.COR_E_INSUFFICIENTMEMORY:
                case HResults.COR_E_INVALIDCOMOBJECT:
                case HResults.COR_E_KEYNOTFOUND:
                case HResults.COR_E_MISSINGSATELLITEASSEMBLY:
                case HResults.COR_E_SAFEARRAYRANKMISMATCH:
                case HResults.COR_E_SAFEARRAYTYPEMISMATCH:
                //case HResults.COR_E_SAFEHANDLEMISSINGATTRIBUTE:
                //case HResults.COR_E_SEMAPHOREFULL:
                //case HResults.COR_E_THREADSTOP:
                case HResults.COR_E_TIMEOUT:
                case HResults.COR_E_WAITHANDLECANNOTBEOPENED:
                case HResults.DISP_E_OVERFLOW:
                case HResults.E_BOUNDS:
                case HResults.E_CHANGED_STATE:
                case HResults.E_FAIL:
                case HResults.E_HANDLE:
                case HResults.ERROR_MRM_MAP_NOT_FOUND:
                case HResults.TYPE_E_TYPEMISMATCH:
                case HResults.CO_E_NOTINITIALIZED:
                case HResults.RPC_E_CHANGED_MODE:
                    return new COMException("", errorCode);

                case HResults.STG_E_PATHNOTFOUND:
                case HResults.CTL_E_PATHNOTFOUND:
                    {
                        return new System.IO.DirectoryNotFoundException
                        {
                            HResult = errorCode
                        };
                    }
                case HResults.FUSION_E_CACHEFILE_FAILED:
                case HResults.FUSION_E_INVALID_NAME:
                case HResults.FUSION_E_PRIVATE_ASM_DISALLOWED:
                case HResults.FUSION_E_REF_DEF_MISMATCH:
                case HResults.ERROR_TOO_MANY_OPEN_FILES:
                case HResults.ERROR_SHARING_VIOLATION:
                case HResults.ERROR_LOCK_VIOLATION:
                case HResults.ERROR_OPEN_FAILED:
                case HResults.ERROR_DISK_CORRUPT:
                case HResults.ERROR_UNRECOGNIZED_VOLUME:
                case HResults.ERROR_DLL_INIT_FAILED:
                case HResults.MSEE_E_ASSEMBLYLOADINPROGRESS:
                case HResults.ERROR_FILE_INVALID:
                    {
                        return new System.IO.FileLoadException
                        {
                            HResult = errorCode
                        };
                    }
                case HResults.CTL_E_FILENOTFOUND:
                    {
                        return new System.IO.FileNotFoundException
                        {
                            HResult = errorCode
                        };
                    }
                default:
                    return new COMException("", errorCode);
            }
        }
#pragma warning restore IDE0060
#endif

        /// <summary>
        /// Throws a CLR exception based on the HRESULT.
        /// </summary>
        public static void ThrowExceptionForHR(int errorCode)
        {
            if (errorCode < 0)
            {
                throw GetExceptionForHR(errorCode)!;
            }
        }

        public static void ThrowExceptionForHR(int errorCode, IntPtr errorInfo)
        {
            if (errorCode < 0)
            {
                throw GetExceptionForHR(errorCode, errorInfo)!;
            }
        }

        public static IntPtr SecureStringToBSTR(SecureString s)
        {
            if (s is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            }

            return s.MarshalToBSTR();
        }

        public static IntPtr SecureStringToCoTaskMemAnsi(SecureString s)
        {
            if (s is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            }

            return s.MarshalToString(globalAlloc: false, unicode: false);
        }

        public static IntPtr SecureStringToCoTaskMemUnicode(SecureString s)
        {
            if (s is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            }

            return s.MarshalToString(globalAlloc: false, unicode: true);
        }

        public static IntPtr SecureStringToGlobalAllocAnsi(SecureString s)
        {
            if (s is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            }

            return s.MarshalToString(globalAlloc: true, unicode: false);
        }

        public static IntPtr SecureStringToGlobalAllocUnicode(SecureString s)
        {
            if (s is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            }

            return s.MarshalToString(globalAlloc: true, unicode: true);
        }

        public static unsafe IntPtr StringToHGlobalAnsi(string? s)
        {
            if (s is null)
            {
                return IntPtr.Zero;
            }

            long lnb = (s.Length + 1) * (long)SystemMaxDBCSCharSize;
            int nb = (int)lnb;

            // Overflow checking
            if (nb != lnb)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.s);
            }

            IntPtr ptr = AllocHGlobal((IntPtr)nb);

            StringToAnsiString(s, (byte*)ptr, nb);
            return ptr;
        }

        public static unsafe IntPtr StringToHGlobalUni(string? s)
        {
            if (s is null)
            {
                return IntPtr.Zero;
            }

            int nb = (s.Length + 1) * 2;

            // Overflow checking
            if (nb < s.Length)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.s);
            }

            IntPtr ptr = AllocHGlobal((IntPtr)nb);

            s.CopyTo(new Span<char>((char*)ptr, s.Length));
            ((char*)ptr)[s.Length] = '\0';

            return ptr;
        }

        private static unsafe IntPtr StringToHGlobalUTF8(string? s)
        {
            if (s is null)
            {
                return IntPtr.Zero;
            }

            int nb = Encoding.UTF8.GetMaxByteCount(s.Length);

            IntPtr ptr = AllocHGlobal(checked(nb + 1));

            byte* pbMem = (byte*)ptr;
            int nbWritten = Encoding.UTF8.GetBytes(s, new Span<byte>(pbMem, nb));
            pbMem[nbWritten] = 0;

            return ptr;
        }

        public static unsafe IntPtr StringToCoTaskMemUni(string? s)
        {
            if (s is null)
            {
                return IntPtr.Zero;
            }

            int nb = (s.Length + 1) * 2;

            // Overflow checking
            if (nb < s.Length)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.s);
            }

            IntPtr ptr = AllocCoTaskMem(nb);

            s.CopyTo(new Span<char>((char*)ptr, s.Length));
            ((char*)ptr)[s.Length] = '\0';

            return ptr;
        }

        public static unsafe IntPtr StringToCoTaskMemUTF8(string? s)
        {
            if (s is null)
            {
                return IntPtr.Zero;
            }

            int nb = Encoding.UTF8.GetMaxByteCount(s.Length);

            IntPtr ptr = AllocCoTaskMem(checked(nb + 1));

            byte* pbMem = (byte*)ptr;
            int nbWritten = Encoding.UTF8.GetBytes(s, new Span<byte>(pbMem, nb));
            pbMem[nbWritten] = 0;

            return ptr;
        }

        public static unsafe IntPtr StringToCoTaskMemAnsi(string? s)
        {
            if (s is null)
            {
                return IntPtr.Zero;
            }

            long lnb = (s.Length + 1) * (long)SystemMaxDBCSCharSize;
            int nb = (int)lnb;

            // Overflow checking
            if (nb != lnb)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.s);
            }

            IntPtr ptr = AllocCoTaskMem(nb);

            StringToAnsiString(s, (byte*)ptr, nb);
            return ptr;
        }

        /// <summary>
        /// Generates a GUID for the specified type. If the type has a GUID in the
        /// metadata then it is returned otherwise a stable guid is generated based
        /// on the fully qualified name of the type.
        /// </summary>
        public static Guid GenerateGuidForType(Type type)
        {
            ArgumentNullException.ThrowIfNull(type);

            if (type is not RuntimeType)
            {
                throw new ArgumentException(SR.Argument_MustBeRuntimeType, nameof(type));
            }

            return type.GUID;
        }

        /// <summary>
        /// This method generates a PROGID for the specified type. If the type has
        /// a PROGID in the metadata then it is returned otherwise a stable PROGID
        /// is generated based on the fully qualified name of the type.
        /// </summary>
        public static string? GenerateProgIdForType(Type type)
        {
            ArgumentNullException.ThrowIfNull(type);

            if (type.IsImport)
            {
                throw new ArgumentException(SR.Argument_TypeMustNotBeComImport, nameof(type));
            }
            if (type.IsGenericType)
            {
                throw new ArgumentException(SR.Argument_NeedNonGenericType, nameof(type));
            }

            ProgIdAttribute? progIdAttribute = type.GetCustomAttribute<ProgIdAttribute>();
            if (progIdAttribute != null)
            {
                return progIdAttribute.Value ?? string.Empty;
            }

            // If there is no prog ID attribute then use the full name of the type as the prog id.
            return type.FullName;
        }

        [RequiresDynamicCode("Marshalling code for the delegate might not be available. Use the GetDelegateForFunctionPointer<TDelegate> overload instead.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static Delegate GetDelegateForFunctionPointer(IntPtr ptr, Type t)
        {
            ArgumentNullException.ThrowIfNull(t);

            ArgumentNullException.ThrowIfNull(ptr);
            if (t is not RuntimeType)
            {
                throw new ArgumentException(SR.Argument_MustBeRuntimeType, nameof(t));
            }
            if (t.IsGenericType)
            {
                throw new ArgumentException(SR.Argument_NeedNonGenericType, nameof(t));
            }

            // For backward compatibility, we allow lookup of existing delegate to
            // function pointer mappings using abstract MulticastDelegate type. We will check
            // for the non-abstract delegate type later if no existing mapping is found.
            if (t.BaseType != typeof(MulticastDelegate) && t != typeof(MulticastDelegate))
            {
                throw new ArgumentException(SR.Arg_MustBeDelegate, nameof(t));
            }

            return GetDelegateForFunctionPointerInternal(ptr, t);
        }

        public static TDelegate GetDelegateForFunctionPointer<TDelegate>(IntPtr ptr)
        {
            ArgumentNullException.ThrowIfNull(ptr);

            Type t = typeof(TDelegate);
            if (t.IsGenericType)
            {
                throw new ArgumentException(SR.Argument_NeedNonGenericType, nameof(TDelegate));
            }

            // For backward compatibility, we allow lookup of existing delegate to
            // function pointer mappings using abstract MulticastDelegate type. We will check
            // for the non-abstract delegate type later if no existing mapping is found.
            if (t.BaseType != typeof(MulticastDelegate) && t != typeof(MulticastDelegate))
            {
                throw new ArgumentException(SR.Arg_MustBeDelegate, nameof(TDelegate));
            }

            return (TDelegate)(object)GetDelegateForFunctionPointerInternal(ptr, t);
        }

        [RequiresDynamicCode("Marshalling code for the delegate might not be available. Use the GetFunctionPointerForDelegate<TDelegate> overload instead.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static IntPtr GetFunctionPointerForDelegate(Delegate d)
        {
            ArgumentNullException.ThrowIfNull(d);

            return GetFunctionPointerForDelegateInternal(d);
        }

        [UnconditionalSuppressMessage("AotAnalysis", "IL3050:AotUnfriendlyApi",
            Justification = "AOT compilers can see the T.")]
        public static IntPtr GetFunctionPointerForDelegate<TDelegate>(TDelegate d) where TDelegate : notnull
        {
            return GetFunctionPointerForDelegate((Delegate)(object)d);
        }

        public static int GetHRForLastWin32Error()
        {
            int dwLastError = GetLastPInvokeError();
            if ((dwLastError & 0x80000000) == 0x80000000)
            {
                return dwLastError;
            }

            return (dwLastError & 0x0000FFFF) | unchecked((int)0x80070000);
        }

        public static unsafe void ZeroFreeBSTR(IntPtr s)
        {
            if (s == IntPtr.Zero)
            {
                return;
            }
            NativeMemory.Clear((void*)s, SysStringByteLen(s));
            FreeBSTR(s);
        }

        public static unsafe void ZeroFreeCoTaskMemAnsi(IntPtr s)
        {
            ZeroFreeCoTaskMemUTF8(s);
        }

        public static unsafe void ZeroFreeCoTaskMemUnicode(IntPtr s)
        {
            if (s == IntPtr.Zero)
            {
                return;
            }
            NativeMemory.Clear((void*)s, (nuint)string.wcslen((char*)s) * sizeof(char));
            FreeCoTaskMem(s);
        }

        public static unsafe void ZeroFreeCoTaskMemUTF8(IntPtr s)
        {
            if (s == IntPtr.Zero)
            {
                return;
            }
            NativeMemory.Clear((void*)s, (nuint)string.strlen((byte*)s));
            FreeCoTaskMem(s);
        }

        public static unsafe void ZeroFreeGlobalAllocAnsi(IntPtr s)
        {
            if (s == IntPtr.Zero)
            {
                return;
            }
            NativeMemory.Clear((void*)s, (nuint)string.strlen((byte*)s));
            FreeHGlobal(s);
        }

        public static unsafe void ZeroFreeGlobalAllocUnicode(IntPtr s)
        {
            if (s == IntPtr.Zero)
            {
                return;
            }
            NativeMemory.Clear((void*)s, (nuint)string.wcslen((char*)s) * sizeof(char));
            FreeHGlobal(s);
        }

        public static unsafe IntPtr StringToBSTR(string? s)
        {
            if (s is null)
            {
                return IntPtr.Zero;
            }

            IntPtr bstr = AllocBSTR(s.Length);

            s.CopyTo(new Span<char>((char*)bstr, s.Length)); // AllocBSTR already included the null terminator

            return bstr;
        }

        public static string PtrToStringBSTR(IntPtr ptr)
        {
            ArgumentNullException.ThrowIfNull(ptr);

            return PtrToStringUni(ptr, (int)(SysStringByteLen(ptr) / sizeof(char)));
        }

        internal static unsafe uint SysStringByteLen(IntPtr s)
        {
            return *(((uint*)s) - 1);
        }

        [SupportedOSPlatform("windows")]
        public static Type? GetTypeFromCLSID(Guid clsid) => GetTypeFromCLSID(clsid, null, throwOnError: false);

        /// <summary>
        /// Initializes the underlying handle of a newly created <see cref="SafeHandle" /> to the provided value.
        /// </summary>
        /// <param name="safeHandle"><see cref="SafeHandle"/> instance to update</param>
        /// <param name="handle">Pre-existing handle</param>
        public static void InitHandle(SafeHandle safeHandle, IntPtr handle)
        {
            // To help maximize performance of P/Invokes, don't check if safeHandle is null.
            safeHandle.SetHandle(handle);
        }

        public static int GetLastWin32Error()
        {
            return GetLastPInvokeError();
        }

        /// <summary>
        /// Gets the system error message for the last PInvoke error code.
        /// </summary>
        /// <returns>The error message associated with the last PInvoke error code.</returns>
        public static string GetLastPInvokeErrorMessage()
        {
            return GetPInvokeErrorMessage(GetLastPInvokeError());
        }
    }
}
