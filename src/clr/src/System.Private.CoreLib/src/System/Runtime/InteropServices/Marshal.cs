// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Security;
using System.Text;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using Win32Native = Microsoft.Win32.Win32Native;
using System.Diagnostics;
using System.Runtime.InteropServices.ComTypes;
using System.StubHelpers;

namespace System.Runtime.InteropServices
{
    public enum CustomQueryInterfaceMode
    {
        Ignore = 0,
        Allow = 1
    }

    /// <summary>
    /// This class contains methods that are mainly used to marshal between unmanaged
    /// and managed types.
    /// </summary>
    public static partial class Marshal
    {
#if FEATURE_COMINTEROP
        internal static Guid IID_IUnknown = new Guid("00000000-0000-0000-C000-000000000046");
#endif //FEATURE_COMINTEROP

        private const int LMEM_FIXED = 0;
        private const int LMEM_MOVEABLE = 2;
#if !FEATURE_PAL
        private const long HiWordMask = unchecked((long)0xffffffffffff0000L);
#endif //!FEATURE_PAL

        // Win32 has the concept of Atoms, where a pointer can either be a pointer
        // or an int.  If it's less than 64K, this is guaranteed to NOT be a 
        // pointer since the bottom 64K bytes are reserved in a process' page table.
        // We should be careful about deallocating this stuff.  Extracted to
        // a function to avoid C# problems with lack of support for IntPtr.
        // We have 2 of these methods for slightly different semantics for NULL.
        private static bool IsWin32Atom(IntPtr ptr)
        {
#if FEATURE_PAL
            return false;
#else
            long lPtr = (long)ptr;
            return 0 == (lPtr & HiWordMask);
#endif
        }

        /// <summary>
        /// The default character size for the system. This is always 2 because
        /// the framework only runs on UTF-16 systems.
        /// </summary>
        public static readonly int SystemDefaultCharSize = 2;

        /// <summary>
        /// The max DBCS character size for the system.
        /// </summary>
        public static readonly int SystemMaxDBCSCharSize = GetSystemMaxDBCSCharSize();

        /// <summary>
        /// Helper method to retrieve the system's maximum DBCS character size.
        /// </summary>
        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern int GetSystemMaxDBCSCharSize();

        public static unsafe string PtrToStringAnsi(IntPtr ptr)
        {
            if (IntPtr.Zero == ptr)
            {
                return null;
            }
            else if (IsWin32Atom(ptr))
            {
                return null;
            }

            int nb = Win32Native.lstrlenA(ptr);
            if (nb == 0)
            {
                return string.Empty;
            }

            return new string((sbyte*)ptr);
        }

        public static unsafe string PtrToStringAnsi(IntPtr ptr, int len)
        {
            if (ptr == IntPtr.Zero)
            {
                throw new ArgumentNullException(nameof(ptr));
            }
            if (len < 0)
            {
                throw new ArgumentException(null, nameof(len));
            }

            return new string((sbyte*)ptr, 0, len);
        }

        public static unsafe string PtrToStringUni(IntPtr ptr, int len)
        {
            if (ptr == IntPtr.Zero)
            {
                throw new ArgumentNullException(nameof(ptr));
            }
            if (len < 0)
            {
                throw new ArgumentException(SR.ArgumentOutOfRange_NeedNonNegNum, nameof(len));
            }

            return new string((char*)ptr, 0, len);
        }

        public static string PtrToStringAuto(IntPtr ptr, int len)
        {
            // Ansi platforms are no longer supported
            return PtrToStringUni(ptr, len);
        }

        public static unsafe string PtrToStringUni(IntPtr ptr)
        {
            if (IntPtr.Zero == ptr)
            {
                return null;
            }
            else if (IsWin32Atom(ptr))
            {
                return null;
            }

            return new string((char*)ptr);
        }

        public static string PtrToStringAuto(IntPtr ptr)
        {
            // Ansi platforms are no longer supported
            return PtrToStringUni(ptr);
        }

        public static unsafe string PtrToStringUTF8(IntPtr ptr)
        {
            if (IntPtr.Zero == ptr)
            {
                return null;
            }

            int nbBytes = StubHelpers.StubHelpers.strlen((sbyte*)ptr.ToPointer());
            return PtrToStringUTF8(ptr, nbBytes);
        }

        public static unsafe string PtrToStringUTF8(IntPtr ptr, int byteLen)
        {
            if (byteLen < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(byteLen), SR.ArgumentOutOfRange_NeedNonNegNum);
            }
            else if (IntPtr.Zero == ptr)
            {
                return null;
            }
            else if (IsWin32Atom(ptr))
            {
                return null;
            }
            else if (byteLen == 0)
            {
                return string.Empty;
            }

            byte* pByte = (byte*)ptr.ToPointer();
            return Encoding.UTF8.GetString(pByte, byteLen);
        }

        public static int SizeOf(object structure)
        {
            if (structure == null)
            {
                throw new ArgumentNullException(nameof(structure));
            }

            return SizeOfHelper(structure.GetType(), true);
        }

        public static int SizeOf<T>(T structure) => SizeOf((object)structure);

        public static int SizeOf(Type t)
        {
            if (t == null)
            {
                throw new ArgumentNullException(nameof(t));
            }
            if (!(t is RuntimeType))
            {
                throw new ArgumentException(SR.Argument_MustBeRuntimeType, nameof(t));
            }
            if (t.IsGenericType)
            {
                throw new ArgumentException(SR.Argument_NeedNonGenericType, nameof(t));
            }

            return SizeOfHelper(t, throwIfNotMarshalable: true);
        }

        public static int SizeOf<T>() => SizeOf(typeof(T));

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern int SizeOfHelper(Type t, bool throwIfNotMarshalable);

        public static IntPtr OffsetOf(Type t, string fieldName)
        {
            if (t == null)
            {
                throw new ArgumentNullException(nameof(t));
            }

            FieldInfo f = t.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (f == null)
            {
                throw new ArgumentException(SR.Format(SR.Argument_OffsetOfFieldNotFound, t.FullName), nameof(fieldName));
            }

            if (!(f is RtFieldInfo rtField))
            {
                throw new ArgumentException(SR.Argument_MustBeRuntimeFieldInfo, nameof(fieldName));
            }

            return OffsetOfHelper(rtField);
        }

        public static IntPtr OffsetOf<T>(string fieldName) => OffsetOf(typeof(T), fieldName);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern IntPtr OffsetOfHelper(IRuntimeFieldInfo f);

        /// <summary>
        /// IMPORTANT NOTICE: This method does not do any verification on the array.
        /// It must be used with EXTREME CAUTION since passing in an array that is
        /// not pinned or in the fixed heap can cause unexpected results.
        /// </summary>
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern IntPtr UnsafeAddrOfPinnedArrayElement(Array arr, int index);

        public static IntPtr UnsafeAddrOfPinnedArrayElement<T>(T[] arr, int index)
        {
            return UnsafeAddrOfPinnedArrayElement((Array)arr, index);
        }

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

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern void CopyToNative(object source, int startIndex, IntPtr destination, int length);

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

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern void CopyToManaged(IntPtr source, object destination, int startIndex, int length);
        
        public static byte ReadByte(object ptr, int ofs)
        {
            return ReadValueSlow(ptr, ofs, (IntPtr nativeHome, int offset) => ReadByte(nativeHome, offset));
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

        public static short ReadInt16(object ptr, int ofs)
        {
            return ReadValueSlow(ptr, ofs, (IntPtr nativeHome, int offset) => ReadInt16(nativeHome, offset));
        }

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
                    // unaligned read
                    short val;
                    byte* valPtr = (byte*)&val;
                    valPtr[0] = addr[0];
                    valPtr[1] = addr[1];
                    return val;
                }
            }
            catch (NullReferenceException)
            {
                // this method is documented to throw AccessViolationException on any AV
                throw new AccessViolationException();
            }
        }

        public static short ReadInt16(IntPtr ptr) => ReadInt16(ptr, 0);

        public static int ReadInt32(object ptr, int ofs)
        {
            return ReadValueSlow(ptr, ofs, (IntPtr nativeHome, int offset) => ReadInt32(nativeHome, offset));
        }

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
                    // unaligned read
                    int val;
                    byte* valPtr = (byte*)&val;
                    valPtr[0] = addr[0];
                    valPtr[1] = addr[1];
                    valPtr[2] = addr[2];
                    valPtr[3] = addr[3];
                    return val;
                }
            }
            catch (NullReferenceException)
            {
                // this method is documented to throw AccessViolationException on any AV
                throw new AccessViolationException();
            }
        }

        public static int ReadInt32(IntPtr ptr) => ReadInt32(ptr, 0);

        public static IntPtr ReadIntPtr(object ptr, int ofs)
        {
#if BIT64
            return (IntPtr)ReadInt64(ptr, ofs);
#else // 32
            return (IntPtr)ReadInt32(ptr, ofs);
#endif
        }

        public static IntPtr ReadIntPtr(IntPtr ptr, int ofs)
        {
#if BIT64
            return (IntPtr)ReadInt64(ptr, ofs);
#else // 32
            return (IntPtr)ReadInt32(ptr, ofs);
#endif
        }

        public static IntPtr ReadIntPtr(IntPtr ptr) => ReadIntPtr(ptr, 0);

        public static long ReadInt64([MarshalAs(UnmanagedType.AsAny), In] object ptr, int ofs)
        {
            return ReadValueSlow(ptr, ofs, (IntPtr nativeHome, int offset) => ReadInt64(nativeHome, offset));
        }

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
                    // unaligned read
                    long val;
                    byte* valPtr = (byte*)&val;
                    valPtr[0] = addr[0];
                    valPtr[1] = addr[1];
                    valPtr[2] = addr[2];
                    valPtr[3] = addr[3];
                    valPtr[4] = addr[4];
                    valPtr[5] = addr[5];
                    valPtr[6] = addr[6];
                    valPtr[7] = addr[7];
                    return val;
                }
            }
            catch (NullReferenceException)
            {
                // this method is documented to throw AccessViolationException on any AV
                throw new AccessViolationException();
            }
        }

        public static long ReadInt64(IntPtr ptr) => ReadInt64(ptr, 0);

        //====================================================================
        // Read value from marshaled object (marshaled using AsAny)
        // It's quite slow and can return back dangling pointers
        // It's only there for backcompact
        // People should instead use the IntPtr overloads
        //====================================================================
        private static unsafe T ReadValueSlow<T>(object ptr, int ofs, Func<IntPtr, int, T> readValueHelper)
        {
            // Consumers of this method are documented to throw AccessViolationException on any AV
            if (ptr == null)
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

        public static void WriteByte(object ptr, int ofs, byte val)
        {
            WriteValueSlow(ptr, ofs, val, (IntPtr nativeHome, int offset, byte value) => WriteByte(nativeHome, offset, value));
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
                    // unaligned write
                    byte* valPtr = (byte*)&val;
                    addr[0] = valPtr[0];
                    addr[1] = valPtr[1];
                }
            }
            catch (NullReferenceException)
            {
                // this method is documented to throw AccessViolationException on any AV
                throw new AccessViolationException();
            }
        }

        public static void WriteInt16(object ptr, int ofs, short val)
        {
            WriteValueSlow(ptr, ofs, val, (IntPtr nativeHome, int offset, short value) => Marshal.WriteInt16(nativeHome, offset, value));
        }

        public static void WriteInt16(IntPtr ptr, short val) => WriteInt16(ptr, 0, val);

        public static void WriteInt16(IntPtr ptr, int ofs, char val) => WriteInt16(ptr, ofs, (short)val);

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
                    // unaligned write
                    byte* valPtr = (byte*)&val;
                    addr[0] = valPtr[0];
                    addr[1] = valPtr[1];
                    addr[2] = valPtr[2];
                    addr[3] = valPtr[3];
                }
            }
            catch (NullReferenceException)
            {
                // this method is documented to throw AccessViolationException on any AV
                throw new AccessViolationException();
            }
        }

        public static void WriteInt32(object ptr, int ofs, int val)
        {
            WriteValueSlow(ptr, ofs, val, (IntPtr nativeHome, int offset, int value) => Marshal.WriteInt32(nativeHome, offset, value));
        }

        public static void WriteInt32(IntPtr ptr, int val) => WriteInt32(ptr, 0, val);

        public static void WriteIntPtr(IntPtr ptr, int ofs, IntPtr val)
        {
#if BIT64
            WriteInt64(ptr, ofs, (long)val);
#else // 32
            WriteInt32(ptr, ofs, (int)val);
#endif
        }

        public static void WriteIntPtr(object ptr, int ofs, IntPtr val)
        {
#if BIT64
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
                    // unaligned write
                    byte* valPtr = (byte*)&val;
                    addr[0] = valPtr[0];
                    addr[1] = valPtr[1];
                    addr[2] = valPtr[2];
                    addr[3] = valPtr[3];
                    addr[4] = valPtr[4];
                    addr[5] = valPtr[5];
                    addr[6] = valPtr[6];
                    addr[7] = valPtr[7];
                }
            }
            catch (NullReferenceException)
            {
                // this method is documented to throw AccessViolationException on any AV
                throw new AccessViolationException();
            }
        }

        public static void WriteInt64(object ptr, int ofs, long val)
        {
            WriteValueSlow(ptr, ofs, val, (IntPtr nativeHome, int offset, long value) => Marshal.WriteInt64(nativeHome, offset, value));
        }

        public static void WriteInt64(IntPtr ptr, long val) => WriteInt64(ptr, 0, val);

        /// <summary>
        /// Write value into marshaled object (marshaled using AsAny) and propagate the
        /// value back. This is quite slow and can return back dangling pointers. It is
        /// only here for backcompat. People should instead use the IntPtr overloads.
        /// </summary>
        private static unsafe void WriteValueSlow<T>(object ptr, int ofs, T val, Action<IntPtr, int, T> writeValueHelper)
        {
            // Consumers of this method are documented to throw AccessViolationException on any AV
            if (ptr == null)
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

        public static int GetHRForLastWin32Error()
        {
            int dwLastError = GetLastWin32Error();
            if ((dwLastError & 0x80000000) == 0x80000000)
            {
                return dwLastError;
            }
            
            return (dwLastError & 0x0000FFFF) | unchecked((int)0x80070000);
        }

        public static void Prelink(MethodInfo m)
        {
            if (m == null)
            {
                throw new ArgumentNullException(nameof(m));
            }
            if (!(m is RuntimeMethodInfo rmi))
            {
                throw new ArgumentException(SR.Argument_MustBeRuntimeMethodInfo, nameof(m));
            }

            InternalPrelink(rmi);
        }

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern void InternalPrelink(IRuntimeMethodInfo m);

        public static void PrelinkAll(Type c)
        {
            if (c == null)
            {
                throw new ArgumentNullException(nameof(c));
            }

            MethodInfo[] mi = c.GetMethods();
            if (mi != null)
            {
                for (int i = 0; i < mi.Length; i++)
                {
                    Prelink(mi[i]);
                }
            }
        }

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

        public static void StructureToPtr<T>(T structure, IntPtr ptr, bool fDeleteOld)
        {
            StructureToPtr((object)structure, ptr, fDeleteOld);
        }

        /// <summary>
        /// Marshals data from a native memory block to a preallocated structure class.
        /// </summary>
        public static void PtrToStructure(IntPtr ptr, object structure)
        {
            PtrToStructureHelper(ptr, structure, allowValueClasses: false);
        }

        public static void PtrToStructure<T>(IntPtr ptr, T structure)
        {
            PtrToStructure(ptr, (object)structure);
        }

        /// <summary>
        /// Creates a new instance of "structuretype" and marshals data from a
        /// native memory block to it.
        /// </summary>
        public static object PtrToStructure(IntPtr ptr, Type structureType)
        {
            if (ptr == IntPtr.Zero)
            {
                return null;
            }

            if (structureType == null)
            {
                throw new ArgumentNullException(nameof(structureType));
            }
            if (structureType.IsGenericType)
            {
                throw new ArgumentException(SR.Argument_NeedNonGenericType, nameof(structureType));
            }
            if (!(structureType.UnderlyingSystemType is RuntimeType rt))
            {
                throw new ArgumentException(SR.Arg_MustBeType, nameof(structureType));
            }

            object structure = rt.CreateInstanceDefaultCtor(publicOnly: false, skipCheckThis: false, fillCache: false, wrapExceptions: true);
            PtrToStructureHelper(ptr, structure, allowValueClasses: true);
            return structure;
        }

        public static T PtrToStructure<T>(IntPtr ptr) => (T)PtrToStructure(ptr, typeof(T));

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

        public static void DestroyStructure<T>(IntPtr ptr) => DestroyStructure(ptr, typeof(T));

#if FEATURE_COMINTEROP
        /// <summary>
        /// Returns the HInstance for this module.  Returns -1 if the module doesn't have
        /// an HInstance.  In Memory (Dynamic) Modules won't have an HInstance.
        /// </summary>
        public static IntPtr GetHINSTANCE(Module m)
        {
            if (m == null)
            {
                throw new ArgumentNullException(nameof(m));
            }

            if (m is RuntimeModule rtModule)
            {
                return GetHINSTANCE(rtModule.GetNativeHandle());
            }

            return (IntPtr)(-1);
        }

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern IntPtr GetHINSTANCE(RuntimeModule m);

#endif // FEATURE_COMINTEROP

        /// <summary>
        /// Throws a CLR exception based on the HRESULT.
        /// </summary>
        public static void ThrowExceptionForHR(int errorCode)
        {
            if (errorCode < 0)
            {
                ThrowExceptionForHRInternal(errorCode, IntPtr.Zero);
            }
        }

        public static void ThrowExceptionForHR(int errorCode, IntPtr errorInfo)
        {
            if (errorCode < 0)
            {
                ThrowExceptionForHRInternal(errorCode, errorInfo);
            }
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern void ThrowExceptionForHRInternal(int errorCode, IntPtr errorInfo);

        /// <summary>
        /// Converts the HRESULT to a CLR exception.
        /// </summary>
        public static Exception GetExceptionForHR(int errorCode)
        {
            if (errorCode >= 0)
            {
                return null;
            }

            return GetExceptionForHRInternal(errorCode, IntPtr.Zero);
        }
        public static Exception GetExceptionForHR(int errorCode, IntPtr errorInfo)
        {
            if (errorCode >= 0)
            {
                return null;
            }

            return GetExceptionForHRInternal(errorCode, errorInfo);
        }

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

            IntPtr pNewMem = Win32Native.LocalAlloc_NoSafeHandle(LMEM_FIXED, unchecked(numBytes));
            if (pNewMem == IntPtr.Zero)
            {
                throw new OutOfMemoryException();
            }

            return pNewMem;
        }

        public static IntPtr AllocHGlobal(int cb) => AllocHGlobal((IntPtr)cb);

        public static void FreeHGlobal(IntPtr hglobal)
        {
            if (!IsWin32Atom(hglobal))
            {
                if (IntPtr.Zero != Win32Native.LocalFree(hglobal))
                {
                    ThrowExceptionForHR(GetHRForLastWin32Error());
                }
            }
        }

        public static IntPtr ReAllocHGlobal(IntPtr pv, IntPtr cb)
        {
            IntPtr pNewMem = Win32Native.LocalReAlloc(pv, cb, LMEM_MOVEABLE);
            if (pNewMem == IntPtr.Zero)
            {
                throw new OutOfMemoryException();
            }

            return pNewMem;
        }
    
        public static unsafe IntPtr StringToHGlobalAnsi(string s)
        {
            if (s == null)
            {
                return IntPtr.Zero;
            }

            int nb = (s.Length + 1) * SystemMaxDBCSCharSize;

            // Overflow checking
            if (nb < s.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(s));
            }

            UIntPtr len = new UIntPtr((uint)nb);
            IntPtr hglobal = Win32Native.LocalAlloc_NoSafeHandle(LMEM_FIXED, len);
            if (hglobal == IntPtr.Zero)
            {
                throw new OutOfMemoryException();
            }

            s.ConvertToAnsi((byte*)hglobal, nb, false, false);
            return hglobal;
        }

        public static unsafe IntPtr StringToHGlobalUni(string s)
        {
            if (s == null)
            {
                return IntPtr.Zero;
            }

            int nb = (s.Length + 1) * 2;

            // Overflow checking
            if (nb < s.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(s));
            }

            UIntPtr len = new UIntPtr((uint)nb);
            IntPtr hglobal = Win32Native.LocalAlloc_NoSafeHandle(LMEM_FIXED, len);
            if (hglobal == IntPtr.Zero)
            {
                throw new OutOfMemoryException();
            }

            fixed (char* firstChar = s)
            {
                string.wstrcpy((char*)hglobal, firstChar, s.Length + 1);
            }
            return hglobal;
        }

        public static IntPtr StringToHGlobalAuto(string s)
        {
            // Ansi platforms are no longer supported
            return StringToHGlobalUni(s);
        }

#if FEATURE_COMINTEROP
        /// <summary>
        /// Converts the CLR exception to an HRESULT. This function also sets
        /// up an IErrorInfo for the exception.
        /// </summary>
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern int GetHRForException(Exception e);

        /// <summary>
        /// Converts the CLR exception to an HRESULT. This function also sets
        /// up an IErrorInfo for the exception.
        /// This function is only used in WinRT and converts ObjectDisposedException
        /// to RO_E_CLOSED
        /// </summary>
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern int GetHRForException_WinRT(Exception e);

        /// <summary>
        /// Given a managed object that wraps an ITypeInfo, return its name.
        /// </summary>
        public static string GetTypeInfoName(ITypeInfo typeInfo)
        {
            if (typeInfo == null)
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
#endif // FEATURE_COMINTEROP

        public static IntPtr /* IDispatch */ GetIDispatchForObject(object o) => throw new PlatformNotSupportedException();

#if FEATURE_COMINTEROP
        /// <summary>
        /// Return the IUnknown* representing the interface for the Object.
        /// Object o should support Type T
        /// </summary>
        public static IntPtr /* IUnknown* */ GetComInterfaceForObject(object o, Type T)
        {
            return GetComInterfaceForObjectNative(o, T, false, true);
        }

        public static IntPtr GetComInterfaceForObject<T, TInterface>(T o) => GetComInterfaceForObject(o, typeof(TInterface));

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
            return CreateAggregatedObject(pOuter, (object)o);
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
            IntPtr pNewMem = Win32Native.CoTaskMemAlloc(new UIntPtr((uint)cb));
            if (pNewMem == IntPtr.Zero)
            {
                throw new OutOfMemoryException();
            }

            return pNewMem;
        }

        public static unsafe IntPtr StringToCoTaskMemUni(string s)
        {
            if (s == null)
            {
                return IntPtr.Zero;
            }

            int nb = (s.Length + 1) * 2;

            // Overflow checking
            if (nb < s.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(s));
            }

            IntPtr hglobal = Win32Native.CoTaskMemAlloc(new UIntPtr((uint)nb));
            if (hglobal == IntPtr.Zero)
            {
                throw new OutOfMemoryException();
            }

            fixed (char* firstChar = s)
            {
                string.wstrcpy((char*)hglobal, firstChar, s.Length + 1);
            }
            return hglobal;
        }

        public static unsafe IntPtr StringToCoTaskMemUTF8(string s)
        {
            if (s == null)
            {
                return IntPtr.Zero;
            }

            int nb = Encoding.UTF8.GetMaxByteCount(s.Length);
            IntPtr pMem = Win32Native.CoTaskMemAlloc(new UIntPtr((uint)nb + 1));
            if (pMem == IntPtr.Zero)
            {
                throw new OutOfMemoryException();
            }

            fixed (char* firstChar = s)
            {
                byte* pbMem = (byte*)pMem;
                int nbWritten = Encoding.UTF8.GetBytes(firstChar, s.Length, pbMem, nb);
                pbMem[nbWritten] = 0;
            }
            return pMem;
        }

        public static IntPtr StringToCoTaskMemAuto(string s)
        {
            // Ansi platforms are no longer supported
            return StringToCoTaskMemUni(s);
        }

        public static unsafe IntPtr StringToCoTaskMemAnsi(string s)
        {
            if (s == null)
            {
                return IntPtr.Zero;
            }

            int nb = (s.Length + 1) * SystemMaxDBCSCharSize;

            // Overflow checking
            if (nb < s.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(s));
            }

            IntPtr hglobal = Win32Native.CoTaskMemAlloc(new UIntPtr((uint)nb));
            if (hglobal == IntPtr.Zero)
            {
                throw new OutOfMemoryException();
            }

            s.ConvertToAnsi((byte*)hglobal, nb, false, false);
            return hglobal;
        }

        public static void FreeCoTaskMem(IntPtr ptr)
        {
            if (!IsWin32Atom(ptr))
            {
                Win32Native.CoTaskMemFree(ptr);
            }
        }

        public static IntPtr ReAllocCoTaskMem(IntPtr pv, int cb)
        {
            IntPtr pNewMem = Win32Native.CoTaskMemRealloc(pv, new UIntPtr((uint)cb));
            if (pNewMem == IntPtr.Zero && cb != 0)
            {
                throw new OutOfMemoryException();
            }

            return pNewMem;
        }

        public static void FreeBSTR(IntPtr ptr)
        {
            if (!IsWin32Atom(ptr))
            {
                Win32Native.SysFreeString(ptr);
            }
        }

        public static IntPtr StringToBSTR(string s)
        {
            if (s == null)
            {
                return IntPtr.Zero;
            }

            // Overflow checking
            if (s.Length + 1 < s.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(s));
            }

            IntPtr bstr = Win32Native.SysAllocStringLen(s, s.Length);
            if (bstr == IntPtr.Zero)
            {
                throw new OutOfMemoryException();
            }

            return bstr;
        }

        public static string PtrToStringBSTR(IntPtr ptr)
        {
            return PtrToStringUni(ptr, (int)Win32Native.SysStringLen(ptr));
        }

#if FEATURE_COMINTEROP
        /// <summary>
        /// Release the COM component and if the reference hits 0 zombie this object.
        /// Further usage of this Object might throw an exception
        /// </summary>
        public static int ReleaseComObject(object o)
        {
            if (o == null)
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
            if (o == null)
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

        public static object GetComObjectData(object obj, object key)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }
            if (key == null)
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
        public static bool SetComObjectData(object obj, object key, object data)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }
            if (key == null)
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
        public static object CreateWrapperOfType(object o, Type t)
        {
            if (t == null)
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

            if (o == null)
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
            object Wrapper = GetComObjectData(o, t);
            if (Wrapper == null)
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
            return (TWrapper)CreateWrapperOfType(o, typeof(TWrapper));
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
        public static extern void GetNativeVariantForObject(object obj, /* VARIANT * */ IntPtr pDstNativeVariant);

        public static void GetNativeVariantForObject<T>(T obj, IntPtr pDstNativeVariant)
        {
            GetNativeVariantForObject((object)obj, pDstNativeVariant);
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern object GetObjectForNativeVariant(/* VARIANT * */ IntPtr pSrcNativeVariant);

        public static T GetObjectForNativeVariant<T>(IntPtr pSrcNativeVariant)
        {
            return (T)GetObjectForNativeVariant(pSrcNativeVariant);
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern object[] GetObjectsForNativeVariants(/* VARIANT * */ IntPtr aSrcNativeVariant, int cVars);

        public static T[] GetObjectsForNativeVariants<T>(IntPtr aSrcNativeVariant, int cVars)
        {
            object[] objects = GetObjectsForNativeVariants(aSrcNativeVariant, cVars);
            T[] result = null;

            if (objects != null)
            {
                result = new T[objects.Length];
                Array.Copy(objects, result, objects.Length);
            }

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
#endif // FEATURE_COMINTEROP

        /// <summary>
        /// Generates a GUID for the specified type. If the type has a GUID in the
        /// metadata then it is returned otherwise a stable guid is generated based
        /// on the fully qualified name of the type.
        /// </summary>
        public static Guid GenerateGuidForType(Type type) => type.GUID;

        /// <summary>
        /// This method generates a PROGID for the specified type. If the type has
        /// a PROGID in the metadata then it is returned otherwise a stable PROGID
        /// is generated based on the fully qualified name of the type.
        /// </summary>
        public static string GenerateProgIdForType(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }
            if (type.IsImport)
            {
                throw new ArgumentException(SR.Argument_TypeMustNotBeComImport, nameof(type));
            }
            if (type.IsGenericType)
            {
                throw new ArgumentException(SR.Argument_NeedNonGenericType, nameof(type));
            }

            IList<CustomAttributeData> cas = CustomAttributeData.GetCustomAttributes(type);
            for (int i = 0; i < cas.Count; i++)
            {
                if (cas[i].Constructor.DeclaringType == typeof(ProgIdAttribute))
                {
                    // Retrieve the PROGID string from the ProgIdAttribute.
                    IList<CustomAttributeTypedArgument> caConstructorArgs = cas[i].ConstructorArguments;
                    Debug.Assert(caConstructorArgs.Count == 1, "caConstructorArgs.Count == 1");

                    CustomAttributeTypedArgument progIdConstructorArg = caConstructorArgs[0];
                    Debug.Assert(progIdConstructorArg.ArgumentType == typeof(string), "progIdConstructorArg.ArgumentType == typeof(String)");

                    string strProgId = (string)progIdConstructorArg.Value;

                    if (strProgId == null)
                        strProgId = string.Empty;

                    return strProgId;
                }
            }

            // If there is no prog ID attribute then use the full name of the type as the prog id.
            return type.FullName;
        }

#if FEATURE_COMINTEROP
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

        /// <summary>
        /// Private method called from EE upon use of license/ICF2 marshaling.
        /// </summary>
        private static IntPtr LoadLicenseManager()
        {
            Type t = Type.GetType("System.ComponentModel.LicenseManager, System", throwOnError: true);
            return t.TypeHandle.Value;
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern void ChangeWrapperHandleStrength(object otp, bool fIsWeak);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern void InitializeWrapperForWinRT(object o, ref IntPtr pUnk);

#if FEATURE_COMINTEROP_WINRT_MANAGED_ACTIVATION
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern void InitializeManagedWinRTFactoryObject(object o, RuntimeType runtimeClassType);
#endif

        /// <summary>
        /// Create activation factory and wraps it with a unique RCW.
        /// </summary>
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern object GetNativeActivationFactory(Type type);

#endif // FEATURE_COMINTEROP

        public static Delegate GetDelegateForFunctionPointer(IntPtr ptr, Type t)
        {
            if (ptr == IntPtr.Zero)
            {
                throw new ArgumentNullException(nameof(ptr));
            }
            if (t == null)
            {
                throw new ArgumentNullException(nameof(t));
            }
            if (!(t is RuntimeType))
            {
                throw new ArgumentException(SR.Argument_MustBeRuntimeType, nameof(t));
            }
            if (t.IsGenericType)
            {
                throw new ArgumentException(SR.Argument_NeedNonGenericType, nameof(t));
            }

            Type c = t.BaseType;
            if (c == null || (c != typeof(Delegate) && c != typeof(MulticastDelegate)))
            {
                throw new ArgumentException(SR.Arg_MustBeDelegate, nameof(t));
            }

            return GetDelegateForFunctionPointerInternal(ptr, t);
        }

        public static TDelegate GetDelegateForFunctionPointer<TDelegate>(IntPtr ptr)
        {
            return (TDelegate)(object)GetDelegateForFunctionPointer(ptr, typeof(TDelegate));
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern Delegate GetDelegateForFunctionPointerInternal(IntPtr ptr, Type t);

        public static IntPtr GetFunctionPointerForDelegate(Delegate d)
        {
            if (d == null)
            {
                throw new ArgumentNullException(nameof(d));
            }

            return GetFunctionPointerForDelegateInternal(d);
        }

        public static IntPtr GetFunctionPointerForDelegate<TDelegate>(TDelegate d)
        {
            return GetFunctionPointerForDelegate((Delegate)(object)d);
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern IntPtr GetFunctionPointerForDelegateInternal(Delegate d);

        public static IntPtr SecureStringToBSTR(SecureString s)
        {
            if (s == null)
            {
                throw new ArgumentNullException(nameof(s));
            }

            return s.MarshalToBSTR();
        }

        public static IntPtr SecureStringToCoTaskMemAnsi(SecureString s)
        {
            if (s == null)
            {
                throw new ArgumentNullException(nameof(s));
            }

            return s.MarshalToString(globalAlloc: false, unicode: false);
        }

        public static IntPtr SecureStringToCoTaskMemUnicode(SecureString s)
        {
            if (s == null)
            {
                throw new ArgumentNullException(nameof(s));
            }

            return s.MarshalToString(globalAlloc: false, unicode: true);
        }
        
        public static void ZeroFreeBSTR(IntPtr s)
        {
            RuntimeImports.RhZeroMemory(s, (UIntPtr)(Win32Native.SysStringLen(s) * 2));
            FreeBSTR(s);
        }

        public static void ZeroFreeCoTaskMemAnsi(IntPtr s)
        {
            RuntimeImports.RhZeroMemory(s, (UIntPtr)(Win32Native.lstrlenA(s)));
            FreeCoTaskMem(s);
        }

        public static void ZeroFreeCoTaskMemUnicode(IntPtr s)
        {
            RuntimeImports.RhZeroMemory(s, (UIntPtr)(Win32Native.lstrlenW(s) * 2));
            FreeCoTaskMem(s);
        }

        public static unsafe void ZeroFreeCoTaskMemUTF8(IntPtr s)
        {
            RuntimeImports.RhZeroMemory(s, (UIntPtr)System.StubHelpers.StubHelpers.strlen((sbyte*)s));
            FreeCoTaskMem(s);
        }

        public static IntPtr SecureStringToGlobalAllocAnsi(SecureString s)
        {
            if (s == null)
            {
                throw new ArgumentNullException(nameof(s));
            }

            return s.MarshalToString(globalAlloc: true, unicode: false);
        }

        public static IntPtr SecureStringToGlobalAllocUnicode(SecureString s)
        {
            if (s == null)
            {
                throw new ArgumentNullException(nameof(s));
            }

            return s.MarshalToString(globalAlloc: true, unicode: true); ;
        }

        public static void ZeroFreeGlobalAllocAnsi(IntPtr s)
        {
            RuntimeImports.RhZeroMemory(s, (UIntPtr)(Win32Native.lstrlenA(s)));
            FreeHGlobal(s);
        }

        public static void ZeroFreeGlobalAllocUnicode(IntPtr s)
        {
            RuntimeImports.RhZeroMemory(s, (UIntPtr)(Win32Native.lstrlenW(s) * 2));
            FreeHGlobal(s);
        }

        /// APIs for managing Native Libraries 

        /// <summary>
        /// NativeLibrary Loader: Simple API
        /// This method is a wrapper around OS loader, using "default" flags.
        /// </summary>
        /// <param name="libraryPath">The name of the native library to be loaded</param>
        /// <returns>The handle for the loaded native library</returns>  
        /// <exception cref="System.ArgumentNullException">If libraryPath is null</exception>
        /// <exception cref="System.DllNotFoundException ">If the library can't be found.</exception>
        /// <exception cref="System.BadImageFormatException">If the library is not valid.</exception>
        public static IntPtr LoadLibrary(string libraryPath)
        {
            if (libraryPath == null)
                throw new ArgumentNullException(nameof(libraryPath));

            return LoadLibraryFromPath(libraryPath, throwOnError: true);
        }

        /// <summary>
        /// NativeLibrary Loader: Simple API that doesn't throw
        /// </summary>
        /// <param name="libraryPath">The name of the native library to be loaded</param>
        /// <param name="handle">The out-parameter for the loaded native library handle</param>
        /// <returns>True on successful load, false otherwise</returns>  
        /// <exception cref="System.ArgumentNullException">If libraryPath is null</exception>
        public static bool TryLoadLibrary(string libraryPath, out IntPtr handle)
        {
            if (libraryPath == null)
                throw new ArgumentNullException(nameof(libraryPath));

            handle = LoadLibraryFromPath(libraryPath, throwOnError: false);
            return handle != IntPtr.Zero;
        }

        /// <summary>
        /// NativeLibrary Loader: High-level API
        /// Given a library name, this function searches specific paths based on the 
        /// runtime configuration, input parameters, and attributes of the calling assembly.
        /// If DllImportSearchPath parameter is non-null, the flags in this enumeration are used.
        /// Otherwise, the flags specified by the DefaultDllImportSearchPaths attribute on the 
        /// calling assembly (if any) are used. 
        /// This LoadLibrary() method does not invoke the managed call-backs for native library resolution: 
        /// * AssemblyLoadContext.LoadUnmanagedDll()
        /// </summary>
        /// <param name="libraryName">The name of the native library to be loaded</param>
        /// <param name="assembly">The assembly loading the native library</param>
        /// <param name="searchPath">The search path</param>
        /// <returns>The handle for the loaded library</returns>  
        /// <exception cref="System.ArgumentNullException">If libraryPath or assembly is null</exception>
        /// <exception cref="System.ArgumentException">If assembly is not a RuntimeAssembly</exception>
        /// <exception cref="System.DllNotFoundException ">If the library can't be found.</exception>
        /// <exception cref="System.BadImageFormatException">If the library is not valid.</exception>        
        public static IntPtr LoadLibrary(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
        {
            if (libraryName == null)
                throw new ArgumentNullException(nameof(libraryName));
            if (assembly == null)
                throw new ArgumentNullException(nameof(assembly));
            if (!(assembly is RuntimeAssembly))
                throw new ArgumentException(SR.Argument_MustBeRuntimeAssembly);
            
            return LoadLibraryByName(libraryName, 
                                     ((RuntimeAssembly)assembly).GetNativeHandle(), 
                                     searchPath.HasValue, 
                                     (uint) searchPath.GetValueOrDefault(), 
                                     throwOnError: true);
        }

        /// <summary>
        /// NativeLibrary Loader: High-level API that doesn't throw.
        /// </summary>
        /// <param name="libraryName">The name of the native library to be loaded</param>
        /// <param name="searchPath">The search path</param>
        /// <param name="assembly">The assembly loading the native library</param>
        /// <param name="handle">The out-parameter for the loaded native library handle</param>
        /// <returns>True on successful load, false otherwise</returns>  
        /// <exception cref="System.ArgumentNullException">If libraryPath or assembly is null</exception>
        /// <exception cref="System.ArgumentException">If assembly is not a RuntimeAssembly</exception>
        public static bool TryLoadLibrary(string libraryName, Assembly assembly, DllImportSearchPath? searchPath, out IntPtr handle)
        {
            if (libraryName == null)
                throw new ArgumentNullException(nameof(libraryName));
            if (assembly == null)
                throw new ArgumentNullException(nameof(assembly));
            if (!(assembly is RuntimeAssembly))
                throw new ArgumentException(SR.Argument_MustBeRuntimeAssembly);
            
            handle = LoadLibraryByName(libraryName, 
                                       ((RuntimeAssembly)assembly).GetNativeHandle(), 
                                       searchPath.HasValue, 
                                       (uint) searchPath.GetValueOrDefault(),
                                       throwOnError: false);
            return handle != IntPtr.Zero;
        }

        /// <summary>
        /// Free a loaded library
        /// Given a library handle, free it.
        /// No action if the input handle is null.
        /// </summary>
        /// <param name="handle">The native library handle to be freed</param>
        /// <exception cref="System.InvalidOperationException">If the operation fails</exception>
        public static void FreeLibrary(IntPtr handle)
        {
            FreeNativeLibrary(handle);
        }

        /// <summary>
        /// Get the address of an exported Symbol
        /// This is a simple wrapper around OS calls, and does not perform any name mangling.
        /// </summary>
        /// <param name="handle">The native library handle</param>
        /// <param name="name">The name of the exported symbol</param>
        /// <returns>The address of the symbol</returns>  
        /// <exception cref="System.ArgumentNullException">If handle or name is null</exception>
        /// <exception cref="System.EntryPointNotFoundException">If the symbol is not found</exception>
        public static IntPtr GetLibraryExport(IntPtr handle, string name)
        {
            if (handle == IntPtr.Zero) 
                throw new ArgumentNullException(nameof(handle));
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            return GetNativeLibraryExport(handle, name, throwOnError: true);
        }

        /// <summary>
        /// Get the address of an exported Symbol, but do not throw
        /// </summary>
        /// <param name="handle">The  native library handle</param>
        /// <param name="name">The name of the exported symbol</param>
        /// <param name="address"> The out-parameter for the symbol address, if it exists</param>
        /// <returns>True on success, false otherwise</returns>  
        /// <exception cref="System.ArgumentNullException">If handle or name is null</exception>
        public static bool TryGetLibraryExport(IntPtr handle, string name, out IntPtr address)
        {
            if (handle == IntPtr.Zero) 
                throw new ArgumentNullException(nameof(handle));
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            address = GetNativeLibraryExport(handle, name, throwOnError: false);
            return address != IntPtr.Zero;
        }

        /// External functions that implement the NativeLibrary interface

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        internal static extern IntPtr LoadLibraryFromPath(string libraryName, bool throwOnError);
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        internal static extern IntPtr LoadLibraryByName(string libraryName, RuntimeAssembly callingAssembly, 
                                                        bool hasDllImportSearchPathFlag, uint dllImportSearchPathFlag, 
                                                        bool throwOnError);
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        internal static extern void FreeNativeLibrary(IntPtr handle);
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        internal static extern IntPtr GetNativeLibraryExport(IntPtr handle, string symbolName, bool throwOnError);
    }
}
