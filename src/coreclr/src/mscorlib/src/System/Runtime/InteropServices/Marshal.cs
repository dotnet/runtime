// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================================
**
**
**
** Purpose: This class contains methods that are mainly used to marshal 
**          between unmanaged and managed types.
**
**
=============================================================================*/

namespace System.Runtime.InteropServices
{    
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Reflection.Emit;
    using System.Security;
    using System.Security.Permissions;
    using System.Text;
    using System.Threading;
    using System.Runtime.Remoting;
    using System.Runtime.CompilerServices;
    using System.Globalization;
    using System.Runtime.ConstrainedExecution;
    using System.Runtime.Versioning;
    using Win32Native = Microsoft.Win32.Win32Native;
    using Microsoft.Win32.SafeHandles;
    using System.Diagnostics;
    using System.Diagnostics.Contracts;
    using System.Runtime.InteropServices.ComTypes;

    [Serializable]
    public enum CustomQueryInterfaceMode
    {
        Ignore  = 0,
        Allow = 1
    }

    //========================================================================
    // All public methods, including PInvoke, are protected with linkchecks.  
    // Remove the default demands for all PInvoke methods with this global 
    // declaration on the class.
    //========================================================================

    public static partial class Marshal
    { 
        //====================================================================
        // Defines used inside the Marshal class.
        //====================================================================
        private const int LMEM_FIXED = 0;
        private const int LMEM_MOVEABLE = 2;
#if !FEATURE_PAL
        private const long HIWORDMASK = unchecked((long)0xffffffffffff0000L);
#endif //!FEATURE_PAL
#if FEATURE_COMINTEROP
        private static Guid IID_IUnknown = new Guid("00000000-0000-0000-C000-000000000046");
#endif //FEATURE_COMINTEROP

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
            return 0 == (lPtr & HIWORDMASK);
#endif
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        private static bool IsNotWin32Atom(IntPtr ptr)
        {
#if FEATURE_PAL
            return true;
#else
            long lPtr = (long)ptr;
            return 0 != (lPtr & HIWORDMASK);
#endif
        }

        //====================================================================
        // The default character size for the system. This is always 2 because
        // the framework only runs on UTF-16 systems.
        //====================================================================
        public static readonly int SystemDefaultCharSize = 2;

        //====================================================================
        // The max DBCS character size for the system.
        //====================================================================
        public static readonly int SystemMaxDBCSCharSize = GetSystemMaxDBCSCharSize();


        //====================================================================
        // The name, title and description of the assembly that will contain 
        // the dynamically generated interop types. 
        //====================================================================
        private const String s_strConvertedTypeInfoAssemblyName   = "InteropDynamicTypes";
        private const String s_strConvertedTypeInfoAssemblyTitle  = "Interop Dynamic Types";
        private const String s_strConvertedTypeInfoAssemblyDesc   = "Type dynamically generated from ITypeInfo's";
        private const String s_strConvertedTypeInfoNameSpace      = "InteropDynamicTypes";


        //====================================================================
        // Helper method to retrieve the system's maximum DBCS character size.
        //====================================================================
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern int GetSystemMaxDBCSCharSize();
        
        unsafe public static String PtrToStringAnsi(IntPtr ptr)
        {
            if (IntPtr.Zero == ptr) {
                return null;
            }
            else if (IsWin32Atom(ptr)) {
                return null;
            }
            else {
                int nb = Win32Native.lstrlenA(ptr);
                if( nb == 0) {
                    return string.Empty;
                }
                else {
                    return new String((sbyte *)ptr);
                }
            }
        }

        unsafe public static String PtrToStringAnsi(IntPtr ptr, int len)
        {
            if (ptr == IntPtr.Zero)
                throw new ArgumentNullException(nameof(ptr));
            if (len < 0)
                throw new ArgumentException(null, nameof(len));

            return new String((sbyte *)ptr, 0, len); 
        }

        unsafe public static String PtrToStringUni(IntPtr ptr, int len)
        {
            if (ptr == IntPtr.Zero)
                throw new ArgumentNullException(nameof(ptr));
            if (len < 0)
                throw new ArgumentException(null, nameof(len));

            return new String((char *)ptr, 0, len);
        }
    
        public static String PtrToStringAuto(IntPtr ptr, int len)
        {
            // Ansi platforms are no longer supported
            return PtrToStringUni(ptr, len);
        }    
        
        unsafe public static String PtrToStringUni(IntPtr ptr)
        {
            if (IntPtr.Zero == ptr) {
                return null;
            }
            else if (IsWin32Atom(ptr)) {
                return null;
            } 
            else {
                return new String((char *)ptr);
            }
        }
        
        public static String PtrToStringAuto(IntPtr ptr)
        {
            // Ansi platforms are no longer supported
            return PtrToStringUni(ptr);
        }

        unsafe public static String PtrToStringUTF8(IntPtr ptr)
        {
            int nbBytes = System.StubHelpers.StubHelpers.strlen((sbyte*)ptr.ToPointer());
            return PtrToStringUTF8(ptr, nbBytes);
        }

        unsafe public static String PtrToStringUTF8(IntPtr ptr,int byteLen)
        {
            if (byteLen < 0)
            {
                throw new ArgumentException(null, nameof(byteLen));
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
            else
            {
                byte* pByte = (byte*)ptr.ToPointer();
                return Encoding.UTF8.GetString(pByte, byteLen);
            }
        }

        //====================================================================
        // SizeOf()
        //====================================================================
        [System.Runtime.InteropServices.ComVisible(true)]
        public static int SizeOf(Object structure)
        {
            if (structure == null)
                throw new ArgumentNullException(nameof(structure));
            // we never had a check for generics here
            Contract.EndContractBlock();

            return SizeOfHelper(structure.GetType(), true);
        }

        public static int SizeOf<T>(T structure)
        {
            return SizeOf((object)structure);
        }

        [Pure]
        public static int SizeOf(Type t)
        {
            if (t == null)
                throw new ArgumentNullException(nameof(t));
            if (!(t is RuntimeType))
                throw new ArgumentException(Environment.GetResourceString("Argument_MustBeRuntimeType"), nameof(t));
            if (t.IsGenericType)
                throw new ArgumentException(Environment.GetResourceString("Argument_NeedNonGenericType"), nameof(t));
            Contract.EndContractBlock();

            return SizeOfHelper(t, true);
        }

        public static int SizeOf<T>()
        {
            return SizeOf(typeof(T));
        }

        /// <summary>
        /// Returns the aligned size of an instance of a value type.
        /// </summary>
        /// <typeparam name="T">Provide a value type to figure out its size</typeparam>
        /// <returns>The aligned size of T in bytes.</returns>
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        internal static uint AlignedSizeOf<T>() where T : struct
        {
            uint size = SizeOfType(typeof(T));
            if (size == 1 || size == 2)
            {
                return size;
            }
            if (IntPtr.Size == 8 && size == 4)
            {
                return size;
            }
            return AlignedSizeOfType(typeof(T));
        }

        // Type must be a value type with no object reference fields.  We only
        // assert this, due to the lack of a suitable generic constraint.
        [MethodImpl(MethodImplOptions.InternalCall)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        internal static extern uint SizeOfType(Type type);

        // Type must be a value type with no object reference fields.  We only
        // assert this, due to the lack of a suitable generic constraint.
        [MethodImpl(MethodImplOptions.InternalCall)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        private static extern uint AlignedSizeOfType(Type type);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern int SizeOfHelper(Type t, bool throwIfNotMarshalable);

        //====================================================================
        // OffsetOf()
        //====================================================================
        public static IntPtr OffsetOf(Type t, String fieldName)
        {
            if (t == null)
                throw new ArgumentNullException(nameof(t));
            Contract.EndContractBlock();
            
            FieldInfo f = t.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (f == null)
                throw new ArgumentException(Environment.GetResourceString("Argument_OffsetOfFieldNotFound", t.FullName), nameof(fieldName));
            RtFieldInfo rtField = f as RtFieldInfo;
            if (rtField == null)
                throw new ArgumentException(Environment.GetResourceString("Argument_MustBeRuntimeFieldInfo"), nameof(fieldName));

            return OffsetOfHelper(rtField);
        }
        public static IntPtr OffsetOf<T>(string fieldName)
        {
            return OffsetOf(typeof(T), fieldName);
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern IntPtr OffsetOfHelper(IRuntimeFieldInfo f);

        //====================================================================
        // UnsafeAddrOfPinnedArrayElement()
        //
        // IMPORTANT NOTICE: This method does not do any verification on the
        // array. It must be used with EXTREME CAUTION since passing in 
        // an array that is not pinned or in the fixed heap can cause 
        // unexpected results !
        //====================================================================
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public static extern IntPtr UnsafeAddrOfPinnedArrayElement(Array arr, int index);

        public static IntPtr UnsafeAddrOfPinnedArrayElement<T>(T[] arr, int index)
        {
            return UnsafeAddrOfPinnedArrayElement((Array)arr, index);
        }

        //====================================================================
        // Copy blocks from CLR arrays to native memory.
        //====================================================================
        public static void Copy(int[]     source, int startIndex, IntPtr destination, int length)
        {
            CopyToNative(source, startIndex, destination, length);
        }
        public static void Copy(char[]    source, int startIndex, IntPtr destination, int length)
        {
            CopyToNative(source, startIndex, destination, length);
        }
        public static void Copy(short[]   source, int startIndex, IntPtr destination, int length)
        {
            CopyToNative(source, startIndex, destination, length);
        }
        public static void Copy(long[]    source, int startIndex, IntPtr destination, int length)
        {
            CopyToNative(source, startIndex, destination, length);
        }
        public static void Copy(float[]   source, int startIndex, IntPtr destination, int length)
        {
            CopyToNative(source, startIndex, destination, length);
        }
        public static void Copy(double[]  source, int startIndex, IntPtr destination, int length)
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
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void CopyToNative(Object source, int startIndex, IntPtr destination, int length);

        //====================================================================
        // Copy blocks from native memory to CLR arrays
        //====================================================================
        public static void Copy(IntPtr source, int[]     destination, int startIndex, int length)
        {
            CopyToManaged(source, destination, startIndex, length);
        }
        public static void Copy(IntPtr source, char[]    destination, int startIndex, int length)
        {
            CopyToManaged(source, destination, startIndex, length);
        }
        public static void Copy(IntPtr source, short[]   destination, int startIndex, int length)
        {
            CopyToManaged(source, destination, startIndex, length);
        }
        public static void Copy(IntPtr source, long[]    destination, int startIndex, int length)
        {
            CopyToManaged(source, destination, startIndex, length);
        }
        public static void Copy(IntPtr source, float[]   destination, int startIndex, int length)
        {
            CopyToManaged(source, destination, startIndex, length);
        }
        public static void Copy(IntPtr source, double[]  destination, int startIndex, int length)
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
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void CopyToManaged(IntPtr source, Object destination, int startIndex, int length);

        //====================================================================
        // Read from memory
        //====================================================================
        public static byte ReadByte([MarshalAs(UnmanagedType.AsAny), In] Object ptr, int ofs)
        {
            throw new PlatformNotSupportedException();
        }

        public static unsafe byte ReadByte(IntPtr ptr, int ofs)
        {
            try
            {
                byte *addr = (byte *)ptr + ofs;
                return *addr;
            }
            catch (NullReferenceException)
            {
                // this method is documented to throw AccessViolationException on any AV
                throw new AccessViolationException();
            }
        }

        public static byte ReadByte(IntPtr ptr)
        {
            return ReadByte(ptr,0);
        }

        public static short ReadInt16([MarshalAs(UnmanagedType.AsAny),In] Object ptr, int ofs)
        {
            throw new PlatformNotSupportedException();
        }
 
        public static unsafe short ReadInt16(IntPtr ptr, int ofs)
        {
            try
            {
                byte *addr = (byte *)ptr + ofs;
                if ((unchecked((int)addr) & 0x1) == 0)
                {
                    // aligned read
                    return *((short *)addr);
                }
                else
                {
                    // unaligned read
                    short val;
                    byte *valPtr = (byte *)&val;
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

        public static short ReadInt16(IntPtr ptr)
        {
            return ReadInt16(ptr, 0);
        }

        public static int ReadInt32([MarshalAs(UnmanagedType.AsAny),In] Object ptr, int ofs)
        {
            throw new PlatformNotSupportedException();
        }
 
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        public static unsafe int ReadInt32(IntPtr ptr, int ofs)
        {
            try
            {
                byte *addr = (byte *)ptr + ofs;
                if ((unchecked((int)addr) & 0x3) == 0)
                {
                    // aligned read
                    return *((int *)addr);
                }
                else
                {
                    // unaligned read
                    int val;
                    byte *valPtr = (byte *)&val;
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
    
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        public static int ReadInt32(IntPtr ptr)
        {
            return ReadInt32(ptr,0);
        }
       
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        public static IntPtr ReadIntPtr([MarshalAs(UnmanagedType.AsAny),In] Object ptr, int ofs)
        {
            #if BIT64
                return (IntPtr) ReadInt64(ptr, ofs);
            #else // 32
                return (IntPtr) ReadInt32(ptr, ofs);
            #endif
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        public static IntPtr ReadIntPtr(IntPtr ptr, int ofs)
        {
            #if BIT64
                return (IntPtr) ReadInt64(ptr, ofs);
            #else // 32
                return (IntPtr) ReadInt32(ptr, ofs);
            #endif
        }
    
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        public static IntPtr ReadIntPtr(IntPtr ptr)
        {
            #if BIT64
                return (IntPtr) ReadInt64(ptr, 0);
            #else // 32
                return (IntPtr) ReadInt32(ptr, 0);
            #endif
        }

        public static long ReadInt64([MarshalAs(UnmanagedType.AsAny),In] Object ptr, int ofs)
        {
            throw new PlatformNotSupportedException();
        }

        public static unsafe long ReadInt64(IntPtr ptr, int ofs)
        {
            try
            {
                byte *addr = (byte *)ptr + ofs;
                if ((unchecked((int)addr) & 0x7) == 0)
                {
                    // aligned read
                    return *((long *)addr);
                }
                else
                {
                    // unaligned read
                    long val;
                    byte *valPtr = (byte *)&val;
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
    
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        public static long ReadInt64(IntPtr ptr)
        {
            return ReadInt64(ptr,0);
        }
    
    
        //====================================================================
        // Write to memory
        //====================================================================
        public static unsafe void WriteByte(IntPtr ptr, int ofs, byte val)
        {
            try
            {
                byte *addr = (byte *)ptr + ofs;
                *addr = val;
            }
            catch (NullReferenceException)
            {
                // this method is documented to throw AccessViolationException on any AV
                throw new AccessViolationException();
            }
        }

        public static void WriteByte([MarshalAs(UnmanagedType.AsAny),In,Out] Object ptr, int ofs, byte val)
        {
            throw new PlatformNotSupportedException();
        }

        public static void WriteByte(IntPtr ptr, byte val)
        {
            WriteByte(ptr, 0, val);
        }
    
        public static unsafe void WriteInt16(IntPtr ptr, int ofs, short val)
        {
            try
            {
                byte *addr = (byte *)ptr + ofs;
                if ((unchecked((int)addr) & 0x1) == 0)
                {
                    // aligned write
                    *((short *)addr) = val;
                }
                else
                {
                    // unaligned write
                    byte *valPtr = (byte *)&val;
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

        public static void WriteInt16([MarshalAs(UnmanagedType.AsAny),In,Out] Object ptr, int ofs, short val)
        {
            throw new PlatformNotSupportedException();
        }

        public static void WriteInt16(IntPtr ptr, short val)
        {
            WriteInt16(ptr, 0, val);
        }    
    
        public static void WriteInt16(IntPtr ptr, int ofs, char val)
        {
            WriteInt16(ptr, ofs, (short)val);
        }
        
        public static void WriteInt16([In,Out]Object ptr, int ofs, char val)
        {
            WriteInt16(ptr, ofs, (short)val);
        }
    
        public static void WriteInt16(IntPtr ptr, char val)
        {
            WriteInt16(ptr, 0, (short)val);
        }
    
        public static unsafe void WriteInt32(IntPtr ptr, int ofs, int val)
        {
            try
            {
                byte *addr = (byte *)ptr + ofs;
                if ((unchecked((int)addr) & 0x3) == 0)
                {
                    // aligned write
                    *((int *)addr) = val;
                }
                else
                {
                    // unaligned write
                    byte *valPtr = (byte *)&val;
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

        public static void WriteInt32([MarshalAs(UnmanagedType.AsAny),In,Out] Object ptr, int ofs, int val)
        {
            throw new PlatformNotSupportedException();
        }

        public static void WriteInt32(IntPtr ptr, int val)
        {
            WriteInt32(ptr,0,val);
        }    
    
        public static void WriteIntPtr(IntPtr ptr, int ofs, IntPtr val)
        {
            #if BIT64
                WriteInt64(ptr, ofs, (long)val);
            #else // 32
                WriteInt32(ptr, ofs, (int)val);
            #endif
        }
        
        public static void WriteIntPtr([MarshalAs(UnmanagedType.AsAny),In,Out] Object ptr, int ofs, IntPtr val)
        {
            #if BIT64
                WriteInt64(ptr, ofs, (long)val);
            #else // 32
                WriteInt32(ptr, ofs, (int)val);
            #endif
        }
        
        public static void WriteIntPtr(IntPtr ptr, IntPtr val)
        {
            #if BIT64
                WriteInt64(ptr, 0, (long)val);
            #else // 32
                WriteInt32(ptr, 0, (int)val);
            #endif
        }

        public static unsafe void WriteInt64(IntPtr ptr, int ofs, long val)
        {
            try
            {
                byte *addr = (byte *)ptr + ofs;
                if ((unchecked((int)addr) & 0x7) == 0)
                {
                    // aligned write
                    *((long *)addr) = val;
                }
                else
                {
                    // unaligned write
                    byte *valPtr = (byte *)&val;
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

        public static void WriteInt64([MarshalAs(UnmanagedType.AsAny),In,Out] Object ptr, int ofs, long val)
        {
            throw new PlatformNotSupportedException();
        }

        public static void WriteInt64(IntPtr ptr, long val)
        {
            WriteInt64(ptr, 0, val);
        }
    
    
        //====================================================================
        // GetLastWin32Error
        //====================================================================
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        public static extern int GetLastWin32Error();
    

        //====================================================================
        // SetLastWin32Error
        //====================================================================
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        internal static extern void SetLastWin32Error(int error);
    

        //====================================================================
        // GetHRForLastWin32Error
        //====================================================================
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        public static int GetHRForLastWin32Error()
        {
            int dwLastError = GetLastWin32Error();
            if ((dwLastError & 0x80000000) == 0x80000000)
                return dwLastError;
            else
                return (dwLastError & 0x0000FFFF) | unchecked((int)0x80070000);
        }


        //====================================================================
        // Prelink
        //====================================================================
        public static void Prelink(MethodInfo m)
        {
            if (m == null) 
                throw new ArgumentNullException(nameof(m));
            Contract.EndContractBlock();

            RuntimeMethodInfo rmi = m as RuntimeMethodInfo;

            if (rmi == null)
                throw new ArgumentException(Environment.GetResourceString("Argument_MustBeRuntimeMethodInfo"));

            InternalPrelink(rmi);
        }
    
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode), SuppressUnmanagedCodeSecurity]
        private static extern void InternalPrelink(IRuntimeMethodInfo m);

        public static void PrelinkAll(Type c)
        {
            if (c == null)
                throw new ArgumentNullException(nameof(c));
            Contract.EndContractBlock();

            MethodInfo[] mi = c.GetMethods();
            if (mi != null) 
            {
                for (int i = 0; i < mi.Length; i++) 
                {
                    Prelink(mi[i]);
                }
            }
        }
    
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public static extern int GetExceptionCode();


        //====================================================================
        // Marshals data from a structure class to a native memory block.
        // If the structure contains pointers to allocated blocks and
        // "fDeleteOld" is true, this routine will call DestroyStructure() first. 
        //====================================================================
        [MethodImplAttribute(MethodImplOptions.InternalCall), ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        [System.Runtime.InteropServices.ComVisible(true)]
        public static extern void StructureToPtr(Object structure, IntPtr ptr, bool fDeleteOld);

        public static void StructureToPtr<T>(T structure, IntPtr ptr, bool fDeleteOld)
        {
            StructureToPtr((object)structure, ptr, fDeleteOld);
        }

        //====================================================================
        // Marshals data from a native memory block to a preallocated structure class.
        //====================================================================
        [System.Runtime.InteropServices.ComVisible(true)]
        public static void PtrToStructure(IntPtr ptr, Object structure)
        {
            PtrToStructureHelper(ptr, structure, false);
        }

        public static void PtrToStructure<T>(IntPtr ptr, T structure)
        {
            PtrToStructure(ptr, (object)structure);
        }
        
        //====================================================================
        // Creates a new instance of "structuretype" and marshals data from a
        // native memory block to it.
        //====================================================================
        [System.Runtime.InteropServices.ComVisible(true)]
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
        public static Object PtrToStructure(IntPtr ptr, Type structureType)
        {
            if (ptr == IntPtr.Zero) return null;

            if (structureType == null)
                throw new ArgumentNullException(nameof(structureType));

            if (structureType.IsGenericType)
                throw new ArgumentException(Environment.GetResourceString("Argument_NeedNonGenericType"), nameof(structureType));

            RuntimeType rt = structureType.UnderlyingSystemType as RuntimeType;

            if (rt == null)
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBeType"), nameof(structureType));

            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;

            Object structure = rt.CreateInstanceDefaultCtor(false /*publicOnly*/, false /*skipCheckThis*/, false /*fillCache*/, ref stackMark);
            PtrToStructureHelper(ptr, structure, true);
            return structure;
        }

        public static T PtrToStructure<T>(IntPtr ptr)
        {
            return (T)PtrToStructure(ptr, typeof(T));
        }

        //====================================================================
        // Helper function to copy a pointer into a preallocated structure.
        //====================================================================
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void PtrToStructureHelper(IntPtr ptr, Object structure, bool allowValueClasses);


        //====================================================================
        // Freeds all substructures pointed to by the native memory block.
        // "structureclass" is used to provide layout information.
        //====================================================================
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [System.Runtime.InteropServices.ComVisible(true)]
        public static extern void DestroyStructure(IntPtr ptr, Type structuretype);

        public static void DestroyStructure<T>(IntPtr ptr)
        {
            DestroyStructure(ptr, typeof(T));
        }

#if FEATURE_COMINTEROP
        //====================================================================
        // Returns the HInstance for this module.  Returns -1 if the module 
        // doesn't have an HInstance.  In Memory (Dynamic) Modules won't have 
        // an HInstance.
        //====================================================================
        public static IntPtr GetHINSTANCE(Module m)
        {
            if (m == null)
                throw new ArgumentNullException(nameof(m));
            Contract.EndContractBlock();

            RuntimeModule rtModule = m as RuntimeModule;
            if (rtModule == null)
            {
                ModuleBuilder mb = m as ModuleBuilder;
                if (mb != null)
                    rtModule = mb.InternalModule;
            }

            if (rtModule == null)
                throw new ArgumentNullException(nameof(m),Environment.GetResourceString("Argument_MustBeRuntimeModule"));

            return GetHINSTANCE(rtModule.GetNativeHandle());
        }    

        [SuppressUnmanagedCodeSecurity]
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode), SuppressUnmanagedCodeSecurity]
        private extern static IntPtr GetHINSTANCE(RuntimeModule m);

#endif // FEATURE_COMINTEROP
        //====================================================================
        // Throws a CLR exception based on the HRESULT.
        //====================================================================
        public static void ThrowExceptionForHR(int errorCode)
        {
            if (errorCode < 0)
                ThrowExceptionForHRInternal(errorCode, IntPtr.Zero);
        }
        public static void ThrowExceptionForHR(int errorCode, IntPtr errorInfo)
        {
            if (errorCode < 0)
                ThrowExceptionForHRInternal(errorCode, errorInfo);
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void ThrowExceptionForHRInternal(int errorCode, IntPtr errorInfo);


        //====================================================================
        // Converts the HRESULT to a CLR exception.
        //====================================================================
        public static Exception GetExceptionForHR(int errorCode)
        {
            if (errorCode < 0)
                return GetExceptionForHRInternal(errorCode, IntPtr.Zero);
            else 
                return null;
        }
        public static Exception GetExceptionForHR(int errorCode, IntPtr errorInfo)
        {
            if (errorCode < 0)
                return GetExceptionForHRInternal(errorCode, errorInfo);
            else 
                return null;
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern Exception GetExceptionForHRInternal(int errorCode, IntPtr errorInfo);


        //====================================================================
        // Memory allocation and deallocation.
        //====================================================================
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        public static IntPtr AllocHGlobal(IntPtr cb)
        {
            // For backwards compatibility on 32 bit platforms, ensure we pass values between 
            // Int32.MaxValue and UInt32.MaxValue to Windows.  If the binary has had the 
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

            if (pNewMem == IntPtr.Zero) {
                throw new OutOfMemoryException();
            }
            return pNewMem;
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        public static IntPtr AllocHGlobal(int cb)
        {
            return AllocHGlobal((IntPtr)cb);
        }
        
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        public static void FreeHGlobal(IntPtr hglobal)
        {
            if (IsNotWin32Atom(hglobal)) {
                if (IntPtr.Zero != Win32Native.LocalFree(hglobal)) {
                    ThrowExceptionForHR(GetHRForLastWin32Error());
                }
            }
        }

        public static IntPtr ReAllocHGlobal(IntPtr pv, IntPtr cb)
        {
            IntPtr pNewMem = Win32Native.LocalReAlloc(pv, cb, LMEM_MOVEABLE);
            if (pNewMem == IntPtr.Zero) {
                throw new OutOfMemoryException();
            }
            return pNewMem;
        }


        //====================================================================
        // String convertions.
        //====================================================================          
        unsafe public static IntPtr StringToHGlobalAnsi(String s)
        {
            if (s == null)
            {
                return IntPtr.Zero;
            }
            else
            {
                int nb = (s.Length + 1) * SystemMaxDBCSCharSize;

                // Overflow checking
                if (nb < s.Length)
                    throw new ArgumentOutOfRangeException(nameof(s));

                UIntPtr len = new UIntPtr((uint)nb);
                IntPtr hglobal = Win32Native.LocalAlloc_NoSafeHandle(LMEM_FIXED, len);
                
                if (hglobal == IntPtr.Zero)
                {
                    throw new OutOfMemoryException();
                }
                else
                {
                    s.ConvertToAnsi((byte *)hglobal, nb, false, false);
                    return hglobal;
                }
            }
        }    

        unsafe public static IntPtr StringToHGlobalUni(String s)
        {
            if (s == null)
            {
                return IntPtr.Zero;
            }
            else
            {
                int nb = (s.Length + 1) * 2;

                // Overflow checking
                if (nb < s.Length)
                    throw new ArgumentOutOfRangeException(nameof(s));

                UIntPtr len = new UIntPtr((uint)nb);
                IntPtr hglobal = Win32Native.LocalAlloc_NoSafeHandle(LMEM_FIXED, len);
                
                if (hglobal == IntPtr.Zero)
                {
                    throw new OutOfMemoryException();
                }
                else
                {
                    fixed (char* firstChar = s)
                    {
                        String.wstrcpy((char*)hglobal, firstChar, s.Length + 1);
                    }
                    return hglobal;
                }
            }
        }

        public static IntPtr StringToHGlobalAuto(String s)
        {
            // Ansi platforms are no longer supported
            return StringToHGlobalUni(s);
        }

#if FEATURE_COMINTEROP

        //====================================================================
        // Converts the CLR exception to an HRESULT. This function also sets
        // up an IErrorInfo for the exception.
        //====================================================================
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public static extern int GetHRForException(Exception e);

        //====================================================================
        // Converts the CLR exception to an HRESULT. This function also sets
        // up an IErrorInfo for the exception.
        // This function is only used in WinRT and converts ObjectDisposedException
        // to RO_E_CLOSED
        //====================================================================
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern int GetHRForException_WinRT(Exception e);

		internal static readonly Guid ManagedNameGuid = new Guid("{0F21F359-AB84-41E8-9A78-36D110E6D2F9}"); 

        //====================================================================
        // Given a managed object that wraps an ITypeInfo, return its name
        //====================================================================
        public static String GetTypeInfoName(ITypeInfo typeInfo)
        {
            if (typeInfo == null)
                throw new ArgumentNullException(nameof(typeInfo));
            Contract.EndContractBlock();
            
            String strTypeLibName = null;
            String strDocString = null;
            int dwHelpContext = 0;
            String strHelpFile = null;

            typeInfo.GetDocumentation(-1, out strTypeLibName, out strDocString, out dwHelpContext, out strHelpFile);

            return strTypeLibName;
        }

        // This method is identical to Type.GetTypeFromCLSID. Since it's interop specific, we expose it
        // on Marshal for more consistent API surface.
        public static Type GetTypeFromCLSID(Guid clsid)
        {
            return RuntimeType.GetTypeFromCLSIDImpl(clsid, null, false);
        }

        //====================================================================
        // return the IUnknown* for an Object if the current context
        // is the one where the RCW was first seen. Will return null 
        // otherwise.
        //====================================================================
        public static IntPtr /* IUnknown* */ GetIUnknownForObject(Object o)
        {
            return GetIUnknownForObjectNative(o, false);
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern IntPtr /* IUnknown* */ GetIUnknownForObjectNative(Object o, bool onlyInContext);

        //====================================================================
        // return the raw IUnknown* for a COM Object not related to current 
        // context
        // Does not call AddRef
        //====================================================================
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern IntPtr /* IUnknown* */ GetRawIUnknownForComObjectNoAddRef(Object o);
#endif // FEATURE_COMINTEROP

        //====================================================================
        // return the IDispatch* for an Object
        //====================================================================
        public static IntPtr /* IDispatch */ GetIDispatchForObject(Object o)
        {
            throw new PlatformNotSupportedException();
        }
        
#if FEATURE_COMINTEROP

        //====================================================================
        // return the IUnknown* representing the interface for the Object
        // Object o should support Type T
        //====================================================================
        public static IntPtr /* IUnknown* */ GetComInterfaceForObject(Object o, Type T)
        {
            return GetComInterfaceForObjectNative(o, T, false, true);
        }

        public static IntPtr GetComInterfaceForObject<T, TInterface>(T o)
        {
            return GetComInterfaceForObject(o, typeof(TInterface));
        }

        //====================================================================
        // return the IUnknown* representing the interface for the Object
        // Object o should support Type T, it refer the value of mode to 
        // invoke customized QueryInterface or not
        //====================================================================
        public static IntPtr /* IUnknown* */ GetComInterfaceForObject(Object o, Type T, CustomQueryInterfaceMode mode)
        {
            bool bEnableCustomizedQueryInterface = ((mode == CustomQueryInterfaceMode.Allow) ? true : false);
            return GetComInterfaceForObjectNative(o, T, false, bEnableCustomizedQueryInterface);
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern IntPtr /* IUnknown* */ GetComInterfaceForObjectNative(Object o, Type t, bool onlyInContext, bool fEnalbeCustomizedQueryInterface);

        //====================================================================
        // return an Object for IUnknown
        //====================================================================
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public static extern Object GetObjectForIUnknown(IntPtr /* IUnknown* */ pUnk);

        //====================================================================
        // Return a unique Object given an IUnknown.  This ensures that you
        //  receive a fresh object (we will not look in the cache to match up this
        //  IUnknown to an already existing object).  This is useful in cases
        //  where you want to be able to call ReleaseComObject on a RCW
        //  and not worry about other active uses of said RCW.
        //====================================================================
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public static extern Object GetUniqueObjectForIUnknown(IntPtr unknown);

        //====================================================================
        // return an Object for IUnknown, using the Type T, 
        //  NOTE: 
        //  Type T should be either a COM imported Type or a sub-type of COM 
        //  imported Type
        //====================================================================
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public static extern Object GetTypedObjectForIUnknown(IntPtr /* IUnknown* */ pUnk, Type t);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public static extern IntPtr CreateAggregatedObject(IntPtr pOuter, Object o);

        public static IntPtr CreateAggregatedObject<T>(IntPtr pOuter, T o)
        {
            return CreateAggregatedObject(pOuter, (object)o);
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public static extern void CleanupUnusedObjectsInCurrentContext();

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public static extern bool AreComObjectsAvailableForCleanup();

        //====================================================================
        // check if the object is classic COM component
        //====================================================================
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public static extern bool IsComObject(Object o);

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

        unsafe public static IntPtr StringToCoTaskMemUni(String s)
        {
            if (s == null)
            {
                return IntPtr.Zero;
            }
            else
            {
                int nb = (s.Length + 1) * 2;

                // Overflow checking
                if (nb < s.Length)
                    throw new ArgumentOutOfRangeException(nameof(s));
                
                IntPtr hglobal = Win32Native.CoTaskMemAlloc(new UIntPtr((uint)nb));

                if (hglobal == IntPtr.Zero)
                {
                    throw new OutOfMemoryException();
                }
                else
                {
                    fixed (char* firstChar = s)
                    {
                        String.wstrcpy((char *)hglobal, firstChar, s.Length + 1);
                    }
                    return hglobal;
                }
            }
        }

        unsafe public static IntPtr StringToCoTaskMemUTF8(String s)
        {
            const int MAX_UTF8_CHAR_SIZE = 3;
            if (s == null)
            {
                return IntPtr.Zero;
            }
            else
            {
                int nb = (s.Length + 1) * MAX_UTF8_CHAR_SIZE;

                // Overflow checking
                if (nb < s.Length)
                    throw new ArgumentOutOfRangeException(nameof(s));

                IntPtr pMem = Win32Native.CoTaskMemAlloc(new UIntPtr((uint)nb +1));

                if (pMem == IntPtr.Zero)
                {
                    throw new OutOfMemoryException();
                }
                else
                {
                    byte* pbMem = (byte*)pMem;
                    int nbWritten = s.GetBytesFromEncoding(pbMem, nb, Encoding.UTF8);
                    pbMem[nbWritten] = 0;
                    return pMem;
                }
            }
        }

        public static IntPtr StringToCoTaskMemAuto(String s)
        {
            // Ansi platforms are no longer supported
            return StringToCoTaskMemUni(s);
        } 
   
        unsafe public static IntPtr StringToCoTaskMemAnsi(String s)
        {
            if (s == null)
            {
                return IntPtr.Zero;
            }
            else
            {
                int nb = (s.Length + 1) * SystemMaxDBCSCharSize;

                // Overflow checking
                if (nb < s.Length)
                    throw new ArgumentOutOfRangeException(nameof(s));

                IntPtr hglobal = Win32Native.CoTaskMemAlloc(new UIntPtr((uint)nb));

                if (hglobal == IntPtr.Zero)
                {
                    throw new OutOfMemoryException();
                }
                else
                {
                    s.ConvertToAnsi((byte *)hglobal, nb, false, false);
                    return hglobal;
                }
            }
        }

        public static void FreeCoTaskMem(IntPtr ptr)
        {
            if (IsNotWin32Atom(ptr)) {
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

        //====================================================================
        // BSTR allocation and dealocation.
        //====================================================================      
        public static void FreeBSTR(IntPtr ptr)
        {
            if (IsNotWin32Atom(ptr))
            {
                Win32Native.SysFreeString(ptr);
            }
        }

        public static IntPtr StringToBSTR(String s)
        {
            if (s == null)
                return IntPtr.Zero;

            // Overflow checking
            if (s.Length + 1 < s.Length)
                throw new ArgumentOutOfRangeException(nameof(s));

            IntPtr bstr = Win32Native.SysAllocStringLen(s, s.Length);
            if (bstr == IntPtr.Zero)
                throw new OutOfMemoryException();

            return bstr;
        }

        public static String PtrToStringBSTR(IntPtr ptr)
        {
            return PtrToStringUni(ptr, (int)Win32Native.SysStringLen(ptr));
        }

#if FEATURE_COMINTEROP
        //====================================================================
        // release the COM component and if the reference hits 0 zombie this object
        // further usage of this Object might throw an exception
        //====================================================================
        public static int ReleaseComObject(Object o)
        {
            __ComObject co = null;
            
            // Make sure the obj is an __ComObject.
            try
            {
                co = (__ComObject)o;
            }
            catch (InvalidCastException)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_ObjNotComObject"), nameof(o));
            }
            
            return co.ReleaseSelf();
        }    

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern int InternalReleaseComObject(Object o);

        
        //====================================================================
        // release the COM component and zombie this object
        // further usage of this Object might throw an exception
        //====================================================================
        public static Int32 FinalReleaseComObject(Object o)
        {
            if (o == null)
                throw new ArgumentNullException(nameof(o));
            Contract.EndContractBlock();

            __ComObject co = null;

            // Make sure the obj is an __ComObject.
            try
            {
                co = (__ComObject)o;
            }
            catch (InvalidCastException)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_ObjNotComObject"), nameof(o));
            }
            
            co.FinalReleaseSelf();

            return 0;
        }    

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void InternalFinalReleaseComObject(Object o);
#endif // FEATURE_COMINTEROP

        //====================================================================
        // This method retrieves data from the COM object.
        //====================================================================
        public static Object GetComObjectData(Object obj, Object key)
        {
            throw new PlatformNotSupportedException();
        }

        //====================================================================
        // This method sets data on the COM object. The data can only be set 
        // once for a given key and cannot be removed. This function returns
        // true if the data has been added, false if the data could not be
        // added because there already was data for the specified key.
        //====================================================================
        public static bool SetComObjectData(Object obj, Object key, Object data)
        {
            throw new PlatformNotSupportedException();
        }

#if FEATURE_COMINTEROP
        //====================================================================
        // This method takes the given COM object and wraps it in an object
        // of the specified type. The type must be derived from __ComObject.
        //====================================================================
        public static Object CreateWrapperOfType(Object o, Type t)
        {
            // Validate the arguments.
            if (t == null)
                throw new ArgumentNullException(nameof(t));
            if (!t.IsCOMObject)
                throw new ArgumentException(Environment.GetResourceString("Argument_TypeNotComObject"), nameof(t));
            if (t.IsGenericType)
                throw new ArgumentException(Environment.GetResourceString("Argument_NeedNonGenericType"), nameof(t));
            Contract.EndContractBlock();

            if (t.IsWindowsRuntimeObject)
                throw new ArgumentException(Environment.GetResourceString("Argument_TypeIsWinRTType"), nameof(t));

            // Check for the null case.
            if (o == null)
                return null;

            // Make sure the object is a COM object.
            if (!o.GetType().IsCOMObject)
                throw new ArgumentException(Environment.GetResourceString("Argument_ObjNotComObject"), nameof(o));
            if (o.GetType().IsWindowsRuntimeObject)
                throw new ArgumentException(Environment.GetResourceString("Argument_ObjIsWinRTObject"), nameof(o));

            // Check to see if the type of the object is the requested type.
            if (o.GetType() == t)
                return o;

            // Check to see if we already have a cached wrapper for this type.
            Object Wrapper = GetComObjectData(o, t);
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

        //====================================================================
        // Helper method called from CreateWrapperOfType.
        //====================================================================
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern Object InternalCreateWrapperOfType(Object o, Type t);

        //====================================================================
        // IUnknown Helpers
        //====================================================================
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public static extern int /* HRESULT */ QueryInterface(IntPtr /* IUnknown */ pUnk, ref Guid iid, out IntPtr ppv);    

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public static extern int /* ULONG */ AddRef(IntPtr /* IUnknown */ pUnk );
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        public static extern int /* ULONG */ Release(IntPtr /* IUnknown */ pUnk );

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public static extern void GetNativeVariantForObject(Object obj, /* VARIANT * */ IntPtr pDstNativeVariant);

        public static void GetNativeVariantForObject<T>(T obj, IntPtr pDstNativeVariant)
        {
            GetNativeVariantForObject((object)obj, pDstNativeVariant);
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public static extern Object GetObjectForNativeVariant(/* VARIANT * */ IntPtr pSrcNativeVariant );

        public static T GetObjectForNativeVariant<T>(IntPtr pSrcNativeVariant)
        {
            return (T)GetObjectForNativeVariant(pSrcNativeVariant);
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public static extern Object[] GetObjectsForNativeVariants(/* VARIANT * */ IntPtr aSrcNativeVariant, int cVars );

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
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public static extern int GetStartComSlot(Type t);

#endif // FEATURE_COMINTEROP

        //====================================================================
        // This method generates a GUID for the specified type. If the type
        // has a GUID in the metadata then it is returned otherwise a stable
        // guid GUID is generated based on the fully qualified name of the 
        // type.
        //====================================================================
        public static Guid GenerateGuidForType(Type type)
        {
            return type.GUID;
        }

        //====================================================================
        // This method generates a PROGID for the specified type. If the type
        // has a PROGID in the metadata then it is returned otherwise a stable
        // PROGID is generated based on the fully qualified name of the 
        // type.
        //====================================================================
        public static String GenerateProgIdForType(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            if (type.IsImport)
                throw new ArgumentException(Environment.GetResourceString("Argument_TypeMustNotBeComImport"), nameof(type));
            if (type.IsGenericType)
                throw new ArgumentException(Environment.GetResourceString("Argument_NeedNonGenericType"), nameof(type));
            Contract.EndContractBlock();

            IList<CustomAttributeData> cas = CustomAttributeData.GetCustomAttributes(type);
            for (int i = 0; i < cas.Count; i ++)
            {
                if (cas[i].Constructor.DeclaringType == typeof(ProgIdAttribute))
                {
                    // Retrieve the PROGID string from the ProgIdAttribute.
                    IList<CustomAttributeTypedArgument> caConstructorArgs = cas[i].ConstructorArguments;
                    Debug.Assert(caConstructorArgs.Count == 1, "caConstructorArgs.Count == 1");
                    
                    CustomAttributeTypedArgument progIdConstructorArg = caConstructorArgs[0];                    
                    Debug.Assert(progIdConstructorArg.ArgumentType == typeof(String), "progIdConstructorArg.ArgumentType == typeof(String)");
                    
                    String strProgId = (String)progIdConstructorArg.Value;
                    
                    if (strProgId == null)
                        strProgId = String.Empty;    
                    
                    return strProgId;
                }
            }

            // If there is no prog ID attribute then use the full name of the type as the prog id.
            return type.FullName;
        }

#if FEATURE_COMINTEROP
        //====================================================================
        // This method binds to the specified moniker.
        //====================================================================
        public static Object BindToMoniker(String monikerName)
        {
            Object obj = null;
            IBindCtx bindctx = null;
            CreateBindCtx(0, out bindctx);

            UInt32 cbEaten;
            IMoniker pmoniker = null;
            MkParseDisplayName(bindctx, monikerName, out cbEaten, out pmoniker);

            BindMoniker(pmoniker, 0, ref IID_IUnknown, out obj);
            return obj;
        }

        [DllImport(Microsoft.Win32.Win32Native.OLE32, PreserveSig = false)]
        [SuppressUnmanagedCodeSecurity]
        private static extern void CreateBindCtx(UInt32 reserved, out IBindCtx ppbc);

        [DllImport(Microsoft.Win32.Win32Native.OLE32, PreserveSig = false)]
        [SuppressUnmanagedCodeSecurity]
        private static extern void MkParseDisplayName(IBindCtx pbc, [MarshalAs(UnmanagedType.LPWStr)] String szUserName, out UInt32 pchEaten, out IMoniker ppmk);

        [DllImport(Microsoft.Win32.Win32Native.OLE32, PreserveSig = false)]
        [SuppressUnmanagedCodeSecurity]
        private static extern void BindMoniker(IMoniker pmk, UInt32 grfOpt, ref Guid iidResult, [MarshalAs(UnmanagedType.Interface)] out Object ppvResult);

        //========================================================================
        // Private method called from EE upon use of license/ICF2 marshaling.
        //========================================================================
        private static IntPtr LoadLicenseManager()
        {
            Assembly sys = Assembly.Load("System, Version="+ ThisAssembly.Version + 
                ", Culture=neutral, PublicKeyToken=" + AssemblyRef.EcmaPublicKeyToken);
            Type t = sys.GetType("System.ComponentModel.LicenseManager");
            if (t == null || !t.IsVisible) 
                return IntPtr.Zero;
            return t.TypeHandle.Value;
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public static extern void ChangeWrapperHandleStrength(Object otp, bool fIsWeak);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void InitializeWrapperForWinRT(object o, ref IntPtr pUnk);

#if FEATURE_COMINTEROP_WINRT_MANAGED_ACTIVATION
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void InitializeManagedWinRTFactoryObject(object o, RuntimeType runtimeClassType);
#endif

        //========================================================================
        // Create activation factory and wraps it with a unique RCW
        //========================================================================
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern object GetNativeActivationFactory(Type type);

#endif // FEATURE_COMINTEROP

        public static Delegate GetDelegateForFunctionPointer(IntPtr ptr, Type t)
        {
            // Validate the parameters
            if (ptr == IntPtr.Zero)
                throw new ArgumentNullException(nameof(ptr));
            
            if (t == null)
                throw new ArgumentNullException(nameof(t));
            Contract.EndContractBlock();
            
            if ((t as RuntimeType) == null)
                throw new ArgumentException(Environment.GetResourceString("Argument_MustBeRuntimeType"), nameof(t));           

            if (t.IsGenericType)
                throw new ArgumentException(Environment.GetResourceString("Argument_NeedNonGenericType"), nameof(t));
            
            Type c = t.BaseType;
            if (c == null || (c != typeof(Delegate) && c != typeof(MulticastDelegate)))
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBeDelegate"), nameof(t));

            return GetDelegateForFunctionPointerInternal(ptr, t);
        }

        public static TDelegate GetDelegateForFunctionPointer<TDelegate>(IntPtr ptr)
        {
            return (TDelegate)(object)GetDelegateForFunctionPointer(ptr, typeof(TDelegate));
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern Delegate GetDelegateForFunctionPointerInternal(IntPtr ptr, Type t);

        public static IntPtr GetFunctionPointerForDelegate(Delegate d)
        {
            if (d == null)
                throw new ArgumentNullException(nameof(d));
            Contract.EndContractBlock();

            return GetFunctionPointerForDelegateInternal(d);
        }

        public static IntPtr GetFunctionPointerForDelegate<TDelegate>(TDelegate d)
        {
            return GetFunctionPointerForDelegate((Delegate)(object)d);
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern IntPtr GetFunctionPointerForDelegateInternal(Delegate d);

        public static IntPtr SecureStringToBSTR(SecureString s) {
            if( s == null) {
                throw new ArgumentNullException(nameof(s));
            }
            Contract.EndContractBlock();
            
#if FEATURE_COMINTEROP
            return s.MarshalToBSTR();
#else
            throw new PlatformNotSupportedException();
#endif
        }

        public static IntPtr SecureStringToCoTaskMemAnsi(SecureString s) {
            if( s == null) {
                throw new ArgumentNullException(nameof(s));
            }
            Contract.EndContractBlock();

            return s.MarshalToString(globalAlloc: false, unicode: false);
        }

        public static IntPtr SecureStringToCoTaskMemUnicode(SecureString s)
        {
            if( s == null) {
                throw new ArgumentNullException(nameof(s));
            }
            Contract.EndContractBlock();

            return  s.MarshalToString(globalAlloc: false, unicode: true);
        }

#if FEATURE_COMINTEROP
        public static void ZeroFreeBSTR(IntPtr s)
        {
            Win32Native.ZeroMemory(s, (UIntPtr)(Win32Native.SysStringLen(s) * 2));
            FreeBSTR(s);
        }
#endif

        public static void ZeroFreeCoTaskMemAnsi(IntPtr s)
        {
            Win32Native.ZeroMemory(s, (UIntPtr)(Win32Native.lstrlenA(s)));
            FreeCoTaskMem(s);
        }

        public static void ZeroFreeCoTaskMemUnicode(IntPtr s)
        {
            Win32Native.ZeroMemory(s, (UIntPtr)(Win32Native.lstrlenW(s) * 2));
            FreeCoTaskMem(s);
        }

        unsafe public static void ZeroFreeCoTaskMemUTF8(IntPtr s)
        {
            Win32Native.ZeroMemory(s, (UIntPtr)System.StubHelpers.StubHelpers.strlen((sbyte*)s));
            FreeCoTaskMem(s);
        }

        public static IntPtr SecureStringToGlobalAllocAnsi(SecureString s) {
            if( s == null) {
                throw new ArgumentNullException(nameof(s));
            }
            Contract.EndContractBlock();

            return s.MarshalToString(globalAlloc: true, unicode: false);
        }

        public static IntPtr SecureStringToGlobalAllocUnicode(SecureString s) {
            if( s == null) {
                throw new ArgumentNullException(nameof(s));
            }
            Contract.EndContractBlock();

            return s.MarshalToString(globalAlloc: true, unicode: true);;
        }

        public static void ZeroFreeGlobalAllocAnsi(IntPtr s) {
            Win32Native.ZeroMemory(s, (UIntPtr)(Win32Native.lstrlenA(s)));
            FreeHGlobal(s);
        }

        public static void ZeroFreeGlobalAllocUnicode(IntPtr s) {
            Win32Native.ZeroMemory(s, (UIntPtr)(Win32Native.lstrlenW(s) * 2));
            FreeHGlobal(s);
        }
    }
}

