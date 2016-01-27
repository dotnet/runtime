// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System {
    
    //Only contains static methods.  Does not require serialization
    
    using System;
    using System.Runtime.CompilerServices;
    using System.Runtime.ConstrainedExecution;
    using System.Runtime.InteropServices;
    using System.Runtime.Versioning;
    using System.Diagnostics.Contracts;
    using System.Security;
    using System.Runtime;

[System.Runtime.InteropServices.ComVisible(true)]
    public static class Buffer
    {
        // Copies from one primitive array to another primitive array without
        // respecting types.  This calls memmove internally.  The count and 
        // offset parameters here are in bytes.  If you want to use traditional
        // array element indices and counts, use Array.Copy.
        [System.Security.SecuritySafeCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public static extern void BlockCopy(Array src, int srcOffset,
            Array dst, int dstOffset, int count);

        // A very simple and efficient memmove that assumes all of the
        // parameter validation has already been done.  The count and offset
        // parameters here are in bytes.  If you want to use traditional
        // array element indices and counts, use Array.Copy.
        [System.Security.SecuritySafeCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void InternalBlockCopy(Array src, int srcOffsetBytes,
            Array dst, int dstOffsetBytes, int byteCount);

        // This is ported from the optimized CRT assembly in memchr.asm. The JIT generates 
        // pretty good code here and this ends up being within a couple % of the CRT asm.
        // It is however cross platform as the CRT hasn't ported their fast version to 64-bit
        // platforms.
        //
        [System.Security.SecurityCritical]  // auto-generated
        internal unsafe static int IndexOfByte(byte* src, byte value, int index, int count)
        {
            Contract.Assert(src != null, "src should not be null");

            byte* pByte = src + index;

            // Align up the pointer to sizeof(int).
            while (((int)pByte & 3) != 0)
            {
                if (count == 0)
                    return -1;
                else if (*pByte == value)
                    return (int) (pByte - src);

                count--;
                pByte++;
            }

            // Fill comparer with value byte for comparisons
            //
            // comparer = 0/0/value/value
            uint comparer = (((uint)value << 8) + (uint)value);
            // comparer = value/value/value/value
            comparer = (comparer << 16) + comparer;

            // Run through buffer until we hit a 4-byte section which contains
            // the byte we're looking for or until we exhaust the buffer.
            while (count > 3)
            {
                // Test the buffer for presence of value. comparer contains the byte
                // replicated 4 times.
                uint t1 = *(uint*)pByte;
                t1 = t1 ^ comparer;
                uint t2 = 0x7efefeff + t1;
                t1 = t1 ^ 0xffffffff;
                t1 = t1 ^ t2;
                t1 = t1 & 0x81010100;

                // if t1 is zero then these 4-bytes don't contain a match
                if (t1 != 0)
                {
                    // We've found a match for value, figure out which position it's in.
                    int foundIndex = (int) (pByte - src);
                    if (pByte[0] == value)
                        return foundIndex;
                    else if (pByte[1] == value)
                        return foundIndex + 1;
                    else if (pByte[2] == value)
                        return foundIndex + 2;
                    else if (pByte[3] == value)
                        return foundIndex + 3;
                }

                count -= 4;
                pByte += 4;

            }

            // Catch any bytes that might be left at the tail of the buffer
            while (count > 0)
            {
                if (*pByte == value)
                    return (int) (pByte - src);

                count--;
                pByte++;
            }

            // If we don't have a match return -1;
            return -1;
        }
        
        // Returns a bool to indicate if the array is of primitive data types
        // or not.
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern bool IsPrimitiveTypeArray(Array array);

        // Gets a particular byte out of the array.  The array must be an
        // array of primitives.  
        //
        // This essentially does the following: 
        // return ((byte*)array) + index.
        //
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern byte _GetByte(Array array, int index);

        [System.Security.SecuritySafeCritical]  // auto-generated
        public static byte GetByte(Array array, int index)
        {
            // Is the array present?
            if (array == null)
                throw new ArgumentNullException("array");

            // Is it of primitive types?
            if (!IsPrimitiveTypeArray(array))
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBePrimArray"), "array");

            // Is the index in valid range of the array?
            if (index < 0 || index >= _ByteLength(array))
                throw new ArgumentOutOfRangeException("index");

            return _GetByte(array, index);
        }

        // Sets a particular byte in an the array.  The array must be an
        // array of primitives.  
        //
        // This essentially does the following: 
        // *(((byte*)array) + index) = value.
        //
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void _SetByte(Array array, int index, byte value);

        [System.Security.SecuritySafeCritical]  // auto-generated
        public static void SetByte(Array array, int index, byte value)
        {
            // Is the array present?
            if (array == null)
                throw new ArgumentNullException("array");

            // Is it of primitive types?
            if (!IsPrimitiveTypeArray(array))
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBePrimArray"), "array");

            // Is the index in valid range of the array?
            if (index < 0 || index >= _ByteLength(array))
                throw new ArgumentOutOfRangeException("index");

            // Make the FCall to do the work
            _SetByte(array, index, value);
        }

    
        // Gets a particular byte out of the array.  The array must be an
        // array of primitives.  
        //
        // This essentially does the following: 
        // return array.length * sizeof(array.UnderlyingElementType).
        //
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern int _ByteLength(Array array);

        [System.Security.SecuritySafeCritical]  // auto-generated
        public static int ByteLength(Array array)
        {
            // Is the array present?
            if (array == null)
                throw new ArgumentNullException("array");

            // Is it of primitive types?
            if (!IsPrimitiveTypeArray(array))
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBePrimArray"), "array");

            return _ByteLength(array);
        }

        [System.Security.SecurityCritical]  // auto-generated
        internal unsafe static void ZeroMemory(byte* src, long len)
        {
            while(len-- > 0)
                *(src + len) = 0;
        }

        [System.Security.SecurityCritical]  // auto-generated
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        internal unsafe static void Memcpy(byte[] dest, int destIndex, byte* src, int srcIndex, int len) {
            Contract.Assert( (srcIndex >= 0) && (destIndex >= 0) && (len >= 0), "Index and length must be non-negative!");
            Contract.Assert(dest.Length - destIndex >= len, "not enough bytes in dest");
            // If dest has 0 elements, the fixed statement will throw an 
            // IndexOutOfRangeException.  Special-case 0-byte copies.
            if (len==0)
                return;
            fixed(byte* pDest = dest) {
                Memcpy(pDest + destIndex, src + srcIndex, len);
            }
        }

        [SecurityCritical]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        internal unsafe static void Memcpy(byte* pDest, int destIndex, byte[] src, int srcIndex, int len)
        {
            Contract.Assert( (srcIndex >= 0) && (destIndex >= 0) && (len >= 0), "Index and length must be non-negative!");        
            Contract.Assert(src.Length - srcIndex >= len, "not enough bytes in src");
            // If dest has 0 elements, the fixed statement will throw an 
            // IndexOutOfRangeException.  Special-case 0-byte copies.
            if (len==0)
                return;
            fixed(byte* pSrc = src) {
                Memcpy(pDest + destIndex, pSrc + srcIndex, len);
            }
        }

        // This is tricky to get right AND fast, so lets make it useful for the whole Fx.
        // E.g. System.Runtime.WindowsRuntime!WindowsRuntimeBufferExtensions.MemCopy uses it.

        // This method has a slightly different behavior on arm and other platforms.
        // On arm this method behaves like memcpy and does not handle overlapping buffers.
        // While on other platforms it behaves like memmove and handles overlapping buffers.
        // This behavioral difference is unfortunate but intentional because
        // 1. This method is given access to other internal dlls and this close to release we do not want to change it.
        // 2. It is difficult to get this right for arm and again due to release dates we would like to visit it later.
        [FriendAccessAllowed]
        [System.Security.SecurityCritical]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
#if ARM
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal unsafe static extern void Memcpy(byte* dest, byte* src, int len);
#else // ARM
        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
        internal unsafe static void Memcpy(byte* dest, byte* src, int len) {
            Contract.Assert(len >= 0, "Negative length in memcopy!");
            Memmove(dest, src, (uint)len);
        }
#endif // ARM

        // This method has different signature for x64 and other platforms and is done for performance reasons.
        [System.Security.SecurityCritical]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
#if WIN64
        internal unsafe static void Memmove(byte* dest, byte* src, ulong len)
#else
        internal unsafe static void Memmove(byte* dest, byte* src, uint len)
#endif
        {
            // P/Invoke into the native version when the buffers are overlapping and the copy needs to be performed backwards
            // This check can produce false positives for lengths greater than Int32.MaxInt. It is fine because we want to use PInvoke path for the large lengths anyway.
#if WIN64
            if ((ulong)dest - (ulong)src < len) goto PInvoke;
#else
            if (((uint)dest - (uint)src) < len) goto PInvoke;
#endif
            //
            // This is portable version of memcpy. It mirrors what the hand optimized assembly versions of memcpy typically do.
            //
            // Ideally, we would just use the cpblk IL instruction here. Unfortunately, cpblk IL instruction is not as efficient as
            // possible yet and so we have this implementation here for now.
            //

            switch (len)
            {
            case 0:
                return;
            case 1:
                *dest = *src;
                return;
            case 2:
                *(short *)dest = *(short *)src;
                return;
            case 3:
                *(short *)dest = *(short *)src;
                *(dest + 2) = *(src + 2);
                return;
            case 4:
                *(int *)dest = *(int *)src;
                return;
            case 5:
                *(int*)dest = *(int*)src;
                *(dest + 4) = *(src + 4);
                return;
            case 6:
                *(int*)dest = *(int*)src;
                *(short*)(dest + 4) = *(short*)(src + 4);
                return;
            case 7:
                *(int*)dest = *(int*)src;
                *(short*)(dest + 4) = *(short*)(src + 4);
                *(dest + 6) = *(src + 6);
                return;
            case 8:
#if WIN64
                *(long*)dest = *(long*)src;
#else
                *(int*)dest = *(int*)src;
                *(int*)(dest + 4) = *(int*)(src + 4);
#endif
                return;
            case 9:
#if WIN64
                *(long*)dest = *(long*)src;
#else
                *(int*)dest = *(int*)src;
                *(int*)(dest + 4) = *(int*)(src + 4);
#endif
                *(dest + 8) = *(src + 8);
                return;
            case 10:
#if WIN64
                *(long*)dest = *(long*)src;
#else
                *(int*)dest = *(int*)src;
                *(int*)(dest + 4) = *(int*)(src + 4);
#endif
                *(short*)(dest + 8) = *(short*)(src + 8);
                return;
            case 11:
#if WIN64
                *(long*)dest = *(long*)src;
#else
                *(int*)dest = *(int*)src;
                *(int*)(dest + 4) = *(int*)(src + 4);
#endif
                *(short*)(dest + 8) = *(short*)(src + 8);
                *(dest + 10) = *(src + 10);
                return;
            case 12:
#if WIN64
                *(long*)dest = *(long*)src;
#else
                *(int*)dest = *(int*)src;
                *(int*)(dest + 4) = *(int*)(src + 4);
#endif
                *(int*)(dest + 8) = *(int*)(src + 8);
                return;
            case 13:
#if WIN64
                *(long*)dest = *(long*)src;
#else
                *(int*)dest = *(int*)src;
                *(int*)(dest + 4) = *(int*)(src + 4);
#endif
                *(int*)(dest + 8) = *(int*)(src + 8);
                *(dest + 12) = *(src + 12);
                return;
            case 14:
#if WIN64
                *(long*)dest = *(long*)src;
#else
                *(int*)dest = *(int*)src;
                *(int*)(dest + 4) = *(int*)(src + 4);
#endif
                *(int*)(dest + 8) = *(int*)(src + 8);
                *(short*)(dest + 12) = *(short*)(src + 12);
                return;
            case 15:
#if WIN64
                *(long*)dest = *(long*)src;
#else
                *(int*)dest = *(int*)src;
                *(int*)(dest + 4) = *(int*)(src + 4);
#endif
                *(int*)(dest + 8) = *(int*)(src + 8);
                *(short*)(dest + 12) = *(short*)(src + 12);
                *(dest + 14) = *(src + 14);
                return;
            case 16:
#if WIN64
                *(long*)dest = *(long*)src;
                *(long*)(dest + 8) = *(long*)(src + 8);
#else
                *(int*)dest = *(int*)src;
                *(int*)(dest + 4) = *(int*)(src + 4);
                *(int*)(dest + 8) = *(int*)(src + 8);
                *(int*)(dest + 12) = *(int*)(src + 12);
#endif
                return;
            default:
                break;
            }

            // P/Invoke into the native version for large lengths
            if (len >= 512) goto PInvoke;

            if (((int)dest & 3) != 0)
            {
                if (((int)dest & 1) != 0)
                {
                    *dest = *src;
                    src++;
                    dest++;
                    len--;
                    if (((int)dest & 2) == 0)
                        goto Aligned;
                }
                *(short *)dest = *(short *)src;
                src += 2;
                dest += 2;
                len -= 2;
            Aligned: ;
            }

#if WIN64
            if (((int)dest & 4) != 0)
            {
                *(int *)dest = *(int *)src;
                src += 4;
                dest += 4;
                len -= 4;
            }
#endif

#if WIN64
            ulong count = len / 16;
#else
            uint count = len / 16;
#endif
            while (count > 0)
            {
#if WIN64
                ((long*)dest)[0] = ((long*)src)[0];
                ((long*)dest)[1] = ((long*)src)[1];
#else
                ((int*)dest)[0] = ((int*)src)[0];
                ((int*)dest)[1] = ((int*)src)[1];
                ((int*)dest)[2] = ((int*)src)[2];
                ((int*)dest)[3] = ((int*)src)[3];
#endif
                dest += 16;
                src += 16;
                count--;
            }

            if ((len & 8) != 0)
            {
#if WIN64
                ((long*)dest)[0] = ((long*)src)[0];
#else
                ((int*)dest)[0] = ((int*)src)[0];
                ((int*)dest)[1] = ((int*)src)[1];
#endif
                dest += 8;
                src += 8;
           }
           if ((len & 4) != 0) 
           {
                ((int*)dest)[0] = ((int*)src)[0];
                dest += 4;
                src += 4;
           }
           if ((len & 2) != 0) 
           {
                ((short*)dest)[0] = ((short*)src)[0];
                dest += 2;
                src += 2;
           }
           if ((len & 1) != 0)
                *dest = *src;

            return;

            PInvoke:
            _Memmove(dest, src, len);

        }

        // Non-inlinable wrapper around the QCall that avoids poluting the fast path
        // with P/Invoke prolog/epilog.
        [SecurityCritical]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        [MethodImplAttribute(MethodImplOptions.NoInlining)]
#if WIN64
        private unsafe static void _Memmove(byte* dest, byte* src, ulong len)
#else
        private unsafe static void _Memmove(byte* dest, byte* src, uint len)
#endif
        {
            __Memmove(dest, src, len);
        }

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        [SecurityCritical]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
#if WIN64
        extern private unsafe static void __Memmove(byte* dest, byte* src, ulong len);
#else
        extern private unsafe static void __Memmove(byte* dest, byte* src, uint len);
#endif


        // The attributes on this method are chosen for best JIT performance. 
        // Please do not edit unless intentional.
        [System.Security.SecurityCritical]
        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
        [CLSCompliant(false)]
        public static unsafe void MemoryCopy(void* source, void* destination, long destinationSizeInBytes, long sourceBytesToCopy)
        {
            if (sourceBytesToCopy > destinationSizeInBytes)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.sourceBytesToCopy);
            }
#if WIN64
            Memmove((byte*)destination, (byte*)source, checked((ulong) sourceBytesToCopy));
#else
            Memmove((byte*)destination, (byte*)source, checked((uint)sourceBytesToCopy));
#endif // WIN64
        }


        // The attributes on this method are chosen for best JIT performance. 
        // Please do not edit unless intentional.
        [System.Security.SecurityCritical]
        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
        [CLSCompliant(false)]
        public static unsafe void MemoryCopy(void* source, void* destination, ulong destinationSizeInBytes, ulong sourceBytesToCopy)
        {
            if (sourceBytesToCopy > destinationSizeInBytes)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.sourceBytesToCopy);
            }
#if WIN64
            Memmove((byte*)destination, (byte*)source, sourceBytesToCopy);
#else
            Memmove((byte*)destination, (byte*)source, checked((uint)sourceBytesToCopy));
#endif // WIN64
        }
    }
}
