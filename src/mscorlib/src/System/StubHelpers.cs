// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


namespace  System.StubHelpers {

    using System.Text;
    using Microsoft.Win32;
    using System.Security;
    using System.Collections.Generic;
    using System.Runtime;
    using System.Runtime.InteropServices;
#if FEATURE_COMINTEROP
    using System.Runtime.InteropServices.WindowsRuntime;
#endif // FEATURE_COMINTEROP
    using System.Runtime.CompilerServices;
    using System.Runtime.ConstrainedExecution;
    using System.Diagnostics.Contracts;

    [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
    internal static class AnsiCharMarshaler
    {
        // The length of the returned array is an approximation based on the length of the input string and the system
        // character set. It is only guaranteed to be larger or equal to cbLength, don't depend on the exact value.
        [System.Security.SecurityCritical]
        unsafe static internal byte[] DoAnsiConversion(string str, bool fBestFit, bool fThrowOnUnmappableChar, out int cbLength)
        {
            byte[] buffer = new byte[(str.Length + 1) * Marshal.SystemMaxDBCSCharSize];
            fixed (byte *bufferPtr = buffer)
            {
                cbLength = str.ConvertToAnsi(bufferPtr, buffer.Length, fBestFit, fThrowOnUnmappableChar);
            }
            return buffer;
        }

        [System.Security.SecurityCritical]
        unsafe static internal byte ConvertToNative(char managedChar, bool fBestFit, bool fThrowOnUnmappableChar)
        {
            int cbAllocLength = (1 + 1) * Marshal.SystemMaxDBCSCharSize;
            byte* bufferPtr = stackalloc byte[cbAllocLength];

            int cbLength = managedChar.ToString().ConvertToAnsi(bufferPtr, cbAllocLength, fBestFit, fThrowOnUnmappableChar);

            BCLDebug.Assert(cbLength > 0, "Zero bytes returned from DoAnsiConversion in AnsiCharMarshaler.ConvertToNative");
            return bufferPtr[0];
        }

        static internal char ConvertToManaged(byte nativeChar)
        {
            byte[] bytes = new byte[1] { nativeChar };
            string str = Encoding.Default.GetString(bytes);
            return str[0];
        }
    }  // class AnsiCharMarshaler

    [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
    internal static class CSTRMarshaler
    {
        [System.Security.SecurityCritical]  // auto-generated
        static internal unsafe IntPtr ConvertToNative(int flags, string strManaged, IntPtr pNativeBuffer)
        {
            if (null == strManaged)
            {
                return IntPtr.Zero;
            }

            StubHelpers.CheckStringLength(strManaged.Length);

            int nb;
            byte *pbNativeBuffer = (byte *)pNativeBuffer;

            if (pbNativeBuffer != null || Marshal.SystemMaxDBCSCharSize == 1)
            {
                // If we are marshaling into a stack buffer or we can accurately estimate the size of the required heap
                // space, we will use a "1-pass" mode where we convert the string directly into the unmanaged buffer.

                // + 1 for the null character from the user
                nb = (strManaged.Length + 1) * Marshal.SystemMaxDBCSCharSize;

                // Use the pre-allocated buffer (allocated by localloc IL instruction) if not NULL, 
                // otherwise fallback to AllocCoTaskMem
                if (pbNativeBuffer == null)
                {
                    // + 1 for the null character we put in
                    pbNativeBuffer = (byte*)Marshal.AllocCoTaskMem(nb + 1);
                }

                nb = strManaged.ConvertToAnsi(pbNativeBuffer, nb + 1, 0 != (flags & 0xFF), 0 != (flags >> 8));
            }
            else
            {
                // Otherwise we use a slower "2-pass" mode where we first marshal the string into an intermediate buffer
                // (managed byte array) and then allocate exactly the right amount of unmanaged memory. This is to avoid
                // wasting memory on systems with multibyte character sets where the buffer we end up with is often much
                // smaller than the upper bound for the given managed string.

                byte[] bytes = AnsiCharMarshaler.DoAnsiConversion(strManaged, 0 != (flags & 0xFF), 0 != (flags >> 8), out nb);

                // + 1 for the null character from the user.  + 1 for the null character we put in.
                pbNativeBuffer = (byte*)Marshal.AllocCoTaskMem(nb + 2);

                Buffer.Memcpy(pbNativeBuffer, 0, bytes, 0, nb);
            }

            pbNativeBuffer[nb]     = 0x00;
            pbNativeBuffer[nb + 1] = 0x00;

            return (IntPtr)pbNativeBuffer;
        }  

        [System.Security.SecurityCritical]  // auto-generated
        static internal unsafe string ConvertToManaged(IntPtr cstr)
        {
            if (IntPtr.Zero == cstr)
                return null;
            else
                return new String((sbyte*)cstr);
        }

        [System.Security.SecurityCritical]  // auto-generated
        static internal void ClearNative(IntPtr pNative)
        {
            Win32Native.CoTaskMemFree(pNative);
        }
    }  // class CSTRMarshaler

    [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
    internal static class UTF8Marshaler
    {
        const int MAX_UTF8_CHAR_SIZE = 3;
        [System.Security.SecurityCritical]
        static internal unsafe IntPtr ConvertToNative(int flags, string strManaged, IntPtr pNativeBuffer)
        {
            if (null == strManaged)
            {
                return IntPtr.Zero;
            }
            StubHelpers.CheckStringLength(strManaged.Length);

            int nb;
            byte* pbNativeBuffer = (byte*)pNativeBuffer;

            // If we are marshaling into a stack buffer allocated by the ILStub
            // we will use a "1-pass" mode where we convert the string directly into the unmanaged buffer.   
            // else we will allocate the precise native heap memory.
            if (pbNativeBuffer != null)
            {
                // this is the number of bytes allocated by the ILStub.
                nb = (strManaged.Length + 1) * MAX_UTF8_CHAR_SIZE;

                // nb is the actual number of bytes written by Encoding.GetBytes.
                // use nb to de-limit the string since we are allocating more than 
                // required on stack
                nb = strManaged.GetBytesFromEncoding(pbNativeBuffer, nb, Encoding.UTF8);
            }
            // required bytes > 260 , allocate required bytes on heap
            else
            {
                nb = Encoding.UTF8.GetByteCount(strManaged);
                // + 1 for the null character.
                pbNativeBuffer = (byte*)Marshal.AllocCoTaskMem(nb + 1);
                strManaged.GetBytesFromEncoding(pbNativeBuffer, nb, Encoding.UTF8);
            }
            pbNativeBuffer[nb] = 0x0;
            return (IntPtr)pbNativeBuffer;
        }

        [System.Security.SecurityCritical]
        static internal unsafe string ConvertToManaged(IntPtr cstr)
        {
            if (IntPtr.Zero == cstr)
                return null;
            int nbBytes = StubHelpers.strlen((sbyte*)cstr);
            return String.CreateStringFromEncoding((byte*)cstr, nbBytes, Encoding.UTF8);
        }

        [System.Security.SecurityCritical]
        static internal void ClearNative(IntPtr pNative)
        {
            if (pNative != IntPtr.Zero)
            {
                Win32Native.CoTaskMemFree(pNative);
            }
        }
    }

    [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
    internal static class UTF8BufferMarshaler
    {
        [System.Security.SecurityCritical]
        static internal unsafe IntPtr ConvertToNative(StringBuilder sb, IntPtr pNativeBuffer, int flags)
        {
            if (null == sb)
            {
                return IntPtr.Zero;
            }

            // Convert to string first  
            string strManaged = sb.ToString();

            // Get byte count 
            int nb = Encoding.UTF8.GetByteCount(strManaged);

            // EmitConvertSpaceCLRToNative allocates memory
            byte* pbNativeBuffer = (byte*)pNativeBuffer;
            nb = strManaged.GetBytesFromEncoding(pbNativeBuffer, nb, Encoding.UTF8);

            pbNativeBuffer[nb] = 0x0;
            return (IntPtr)pbNativeBuffer;
        }

        [System.Security.SecurityCritical]
        static internal unsafe void ConvertToManaged(StringBuilder sb, IntPtr pNative)
        {
            if (pNative == null)
                return;

            int nbBytes = StubHelpers.strlen((sbyte*)pNative);
            int numChar = Encoding.UTF8.GetCharCount((byte*)pNative, nbBytes);

            // +1 GetCharCount return 0 if the pNative points to a 
            // an empty buffer.We still need to allocate an empty 
            // buffer with a '\0' to distingiush it from null.
            // Note that pinning on (char *pinned = new char[0])
            // return null and  Encoding.UTF8.GetChars do not like 
            // null argument.
            char[] cCharBuffer = new char[numChar + 1];
            cCharBuffer[numChar] = '\0';
            fixed (char* pBuffer = cCharBuffer)
            {
                numChar = Encoding.UTF8.GetChars((byte*)pNative, nbBytes, pBuffer, numChar);
                // replace string builder internal buffer
                sb.ReplaceBufferInternal(pBuffer, numChar);
            }
        }
    }

#if FEATURE_COMINTEROP

    [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
    internal static class BSTRMarshaler
    {
        [System.Security.SecurityCritical]  // auto-generated
        static internal unsafe IntPtr ConvertToNative(string strManaged, IntPtr pNativeBuffer)
        {
            if (null == strManaged)
            {
                return IntPtr.Zero;
            }
            else
            {
                StubHelpers.CheckStringLength(strManaged.Length);

                byte trailByte;
                bool hasTrailByte = strManaged.TryGetTrailByte(out trailByte);

                uint lengthInBytes = (uint)strManaged.Length * 2;

                if (hasTrailByte)
                {
                    // this is an odd-sized string with a trailing byte stored in its sync block
                    lengthInBytes++;
                }

                byte *ptrToFirstChar;

                if (pNativeBuffer != IntPtr.Zero)
                {
                    // If caller provided a buffer, construct the BSTR manually. The size
                    // of the buffer must be at least (lengthInBytes + 6) bytes.
#if _DEBUG
                    uint length = *((uint *)pNativeBuffer.ToPointer());
                    BCLDebug.Assert(length >= lengthInBytes + 6, "BSTR localloc'ed buffer is too small");
#endif // _DEBUG

                    // set length
                    *((uint *)pNativeBuffer.ToPointer()) = lengthInBytes;

                    ptrToFirstChar = (byte *)pNativeBuffer.ToPointer() + 4;
                }
                else
                {
                    // If not provided, allocate the buffer using SysAllocStringByteLen so
                    // that odd-sized strings will be handled as well.
                    ptrToFirstChar = (byte *)Win32Native.SysAllocStringByteLen(null, lengthInBytes).ToPointer();

                    if (ptrToFirstChar == null) 
                    {
                        throw new OutOfMemoryException();
                    }
                }

                // copy characters from the managed string
                fixed (char* ch = strManaged)
                {
                    Buffer.Memcpy(
                        ptrToFirstChar,
                        (byte *)ch,
                        (strManaged.Length + 1) * 2);
                }

                // copy the trail byte if present
                if (hasTrailByte)
                {
                    ptrToFirstChar[lengthInBytes - 1] = trailByte;
                }

                // return ptr to first character
                return (IntPtr)ptrToFirstChar;
            }
        }

        [System.Security.SecurityCritical]  // auto-generated
        static internal unsafe string ConvertToManaged(IntPtr bstr)
        {
            if (IntPtr.Zero == bstr)
            {
                return null;
            }
            else
            {
                uint length = Win32Native.SysStringByteLen(bstr);

                // Intentionally checking the number of bytes not characters to match the behavior
                // of ML marshalers. This prevents roundtripping of very large strings as the check
                // in the managed->native direction is done on String length but considering that
                // it's completely moot on 32-bit and not expected to be important on 64-bit either,
                // the ability to catch random garbage in the BSTR's length field outweighs this
                // restriction. If an ordinary null-terminated string is passed instead of a BSTR,
                // chances are that the length field - possibly being unallocated memory - contains
                // a heap fill pattern that will have the highest bit set, caught by the check.
                StubHelpers.CheckStringLength(length);

                string ret;
                if (length == 1)
                {
                    // In the empty string case, we need to use FastAllocateString rather than the
                    // String .ctor, since newing up a 0 sized string will always return String.Emtpy.
                    // When we marshal that out as a bstr, it can wind up getting modified which
                    // corrupts String.Empty.
                    ret = String.FastAllocateString(0);
                }
                else
                {
                    ret = new String((char*)bstr, 0, (int)(length / 2));
                }

                if ((length & 1) == 1)
                {
                    // odd-sized strings need to have the trailing byte saved in their sync block
                    ret.SetTrailByte(((byte *)bstr.ToPointer())[length - 1]);
                }

                return ret;
            }
        }

        [System.Security.SecurityCritical]  // auto-generated
        static internal void ClearNative(IntPtr pNative)
        {
            if (IntPtr.Zero != pNative)
            {
                Win32Native.SysFreeString(pNative);
            }
        }
    }  // class BSTRMarshaler

#endif // FEATURE_COMINTEROP


    [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
    internal static class VBByValStrMarshaler
    {
        [System.Security.SecurityCritical]  // auto-generated
        static internal unsafe IntPtr ConvertToNative(string strManaged, bool fBestFit, bool fThrowOnUnmappableChar, ref int cch)
        {
            if (null == strManaged)
            {
                return IntPtr.Zero;
            }

            byte* pNative;
            
            cch = strManaged.Length;

            StubHelpers.CheckStringLength(cch);

            // length field at negative offset + (# of characters incl. the terminator) * max ANSI char size
            int nbytes = sizeof(uint) + ((cch + 1) * Marshal.SystemMaxDBCSCharSize);

            pNative = (byte*)Marshal.AllocCoTaskMem(nbytes);
            int* pLength = (int*)pNative;
            
            pNative = pNative + sizeof(uint);

            if (0 == cch)
            {
                *pNative = 0;
                *pLength = 0;
            }
            else
            {
                int nbytesused;
                byte[] bytes = AnsiCharMarshaler.DoAnsiConversion(strManaged, fBestFit, fThrowOnUnmappableChar, out nbytesused);

                BCLDebug.Assert(nbytesused < nbytes, "Insufficient buffer allocated in VBByValStrMarshaler.ConvertToNative");
                Buffer.Memcpy(pNative, 0, bytes, 0, nbytesused);

                pNative[nbytesused] = 0;
                *pLength = nbytesused;
            }

            return new IntPtr(pNative);
        }

        [System.Security.SecurityCritical]  // auto-generated
        static internal unsafe string ConvertToManaged(IntPtr pNative, int cch)
        {
            if (IntPtr.Zero == pNative)
            {
                return null;
            }

            return new String((sbyte*)pNative, 0, cch);
        }
        
        [System.Security.SecurityCritical]  // auto-generated
        static internal unsafe void ClearNative(IntPtr pNative)
        {
            if (IntPtr.Zero != pNative)
            {
                Win32Native.CoTaskMemFree((IntPtr)(((long)pNative) - sizeof(uint)));
            }
        }
    }  // class VBByValStrMarshaler


#if FEATURE_COMINTEROP

    [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
    internal static class AnsiBSTRMarshaler
    {
        [System.Security.SecurityCritical]  // auto-generated
        static internal unsafe IntPtr ConvertToNative(int flags, string strManaged)
        {
            if (null == strManaged)
            {
                return IntPtr.Zero;
            }

            int length = strManaged.Length;

            StubHelpers.CheckStringLength(length);

            byte[]  bytes = null;
            int     nb = 0;

            if (length > 0)
            {
                bytes = AnsiCharMarshaler.DoAnsiConversion(strManaged, 0 != (flags & 0xFF), 0 != (flags >> 8), out nb);
            }

            return Win32Native.SysAllocStringByteLen(bytes, (uint)nb);
        }

        [System.Security.SecurityCritical]  // auto-generated
        static internal unsafe string ConvertToManaged(IntPtr bstr)
        {
            if (IntPtr.Zero == bstr)
            {
                return null;
            }
            else
            {
                // We intentionally ignore the length field of the BSTR for back compat reasons.
                // Unfortunately VB.NET uses Ansi BSTR marshaling when a string is passed ByRef
                // and we cannot afford to break this common scenario.
                return new String((sbyte*)bstr);
            }
        }

        [System.Security.SecurityCritical]  // auto-generated
        static internal unsafe void ClearNative(IntPtr pNative)
        {
            if (IntPtr.Zero != pNative)
            {
                Win32Native.SysFreeString(pNative);
            }
        }
    }  // class AnsiBSTRMarshaler

#endif // FEATURE_COMINTEROP


    [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
    internal static class WSTRBufferMarshaler
    {
        static internal IntPtr ConvertToNative(string strManaged)
        {
            Contract.Assert(false, "NYI");
            return IntPtr.Zero;
        }

        static internal unsafe string ConvertToManaged(IntPtr bstr)
        {
            Contract.Assert(false, "NYI");
            return null;
        }

        static internal void ClearNative(IntPtr pNative)
        {
            Contract.Assert(false, "NYI");
        }
    }  // class WSTRBufferMarshaler


#if FEATURE_COMINTEROP


    [StructLayout(LayoutKind.Sequential)]
    internal struct DateTimeNative
    {
        public Int64 UniversalTime;
    };

    [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
    internal static class DateTimeOffsetMarshaler {

        // Numer of ticks counted between 0001-01-01, 00:00:00 and 1601-01-01, 00:00:00.
        // You can get this through:  (new DateTimeOffset(1601, 1, 1, 0, 0, 1, TimeSpan.Zero)).Ticks;
        private const Int64 ManagedUtcTicksAtNativeZero = 504911232000000000;

        [SecurityCritical]
        internal static void ConvertToNative(ref DateTimeOffset managedDTO, out DateTimeNative dateTime) {

            Int64 managedUtcTicks = managedDTO.UtcTicks;
            dateTime.UniversalTime = managedUtcTicks - ManagedUtcTicksAtNativeZero;
        }

        [SecurityCritical]
        internal static void ConvertToManaged(out DateTimeOffset managedLocalDTO, ref DateTimeNative nativeTicks) {

            Int64 managedUtcTicks = ManagedUtcTicksAtNativeZero + nativeTicks.UniversalTime;
            DateTimeOffset managedUtcDTO = new DateTimeOffset(managedUtcTicks, TimeSpan.Zero);
            
            // Some Utc times cannot be represented in local time in certain timezones. E.g. 0001-01-01 12:00:00 AM cannot 
            // be represented in any timezones with a negative offset from Utc. We throw an ArgumentException in that case.
            managedLocalDTO = managedUtcDTO.ToLocalTime(true);
        }
    }  // class DateTimeOffsetMarshaler

#endif  // FEATURE_COMINTEROP


#if FEATURE_COMINTEROP
    [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
    internal static class HStringMarshaler
    {
        // Slow-path, which requires making a copy of the managed string into the resulting HSTRING
        [SecurityCritical]
        internal static unsafe IntPtr ConvertToNative(string managed)
        {
            if (!Environment.IsWinRTSupported)
                throw new PlatformNotSupportedException(Environment.GetResourceString("PlatformNotSupported_WinRT"));
            if (managed == null)
                throw new ArgumentNullException(); // We don't have enough information to get the argument name 

            IntPtr hstring;
            int hrCreate = System.Runtime.InteropServices.WindowsRuntime.UnsafeNativeMethods.WindowsCreateString(managed, managed.Length, &hstring);
            Marshal.ThrowExceptionForHR(hrCreate, new IntPtr(-1));
            return hstring;
        }

        // Fast-path, which creates a reference over a pinned managed string.  This may only be used if the
        // pinned string and HSTRING_HEADER will outlive the HSTRING produced (for instance, as an in parameter).
        //
        // Note that the managed string input to this method MUST be pinned, and stay pinned for the lifetime of
        // the returned HSTRING object.  If the string is not pinned, or becomes unpinned before the HSTRING's
        // lifetime ends, the HSTRING instance will be corrupted.
        [SecurityCritical]
        internal static unsafe IntPtr ConvertToNativeReference(string managed,
                                                               [Out] HSTRING_HEADER *hstringHeader)
        {
            if (!Environment.IsWinRTSupported)
                throw new PlatformNotSupportedException(Environment.GetResourceString("PlatformNotSupported_WinRT"));
            if (managed == null)
                throw new ArgumentNullException();  // We don't have enough information to get the argument name 

            // The string must also be pinned by the caller to ConvertToNativeReference, which also owns
            // the HSTRING_HEADER.
            fixed (char *pManaged = managed)
            {
                IntPtr hstring;
                int hrCreate = System.Runtime.InteropServices.WindowsRuntime.UnsafeNativeMethods.WindowsCreateStringReference(pManaged, managed.Length, hstringHeader, &hstring);
                Marshal.ThrowExceptionForHR(hrCreate, new IntPtr(-1));
                return hstring;
            }
        }

        [SecurityCritical]
        internal static string ConvertToManaged(IntPtr hstring)
        {
            if (!Environment.IsWinRTSupported)
            {
                throw new PlatformNotSupportedException(Environment.GetResourceString("PlatformNotSupported_WinRT"));
            }

            return WindowsRuntimeMarshal.HStringToString(hstring);
        }

        [SecurityCritical]
        internal static void ClearNative(IntPtr hstring)
        {
            Contract.Assert(Environment.IsWinRTSupported);

            if (hstring != IntPtr.Zero)
            {
                System.Runtime.InteropServices.WindowsRuntime.UnsafeNativeMethods.WindowsDeleteString(hstring);
            }
        }
    }  // class HStringMarshaler

    [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
    internal static class ObjectMarshaler
    {
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        static internal extern void ConvertToNative(object objSrc, IntPtr pDstVariant);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        static internal extern object ConvertToManaged(IntPtr pSrcVariant);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        static internal extern void ClearNative(IntPtr pVariant);
    }  // class ObjectMarshaler

#endif // FEATURE_COMINTEROP

    [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
    internal static class ValueClassMarshaler
    {
        [SecurityCritical]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        static internal extern void ConvertToNative(IntPtr dst, IntPtr src, IntPtr pMT, ref CleanupWorkList pCleanupWorkList);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        static internal extern void ConvertToManaged(IntPtr dst, IntPtr src, IntPtr pMT);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        static internal extern void ClearNative(IntPtr dst, IntPtr pMT);
    }  // class ValueClassMarshaler

    [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
    internal static class DateMarshaler
    {
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        static internal extern double ConvertToNative(DateTime managedDate);

        // The return type is really DateTime but we use long to avoid the pain associated with returning structures.
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        static internal extern long ConvertToManaged(double nativeDate);
    }  // class DateMarshaler

#if FEATURE_COMINTEROP
    [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
    [FriendAccessAllowed]
    internal static class InterfaceMarshaler
    {
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        static internal extern IntPtr ConvertToNative(object objSrc, IntPtr itfMT, IntPtr classMT, int flags);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        static internal extern object ConvertToManaged(IntPtr pUnk, IntPtr itfMT, IntPtr classMT, int flags);

        [SecurityCritical]
        [DllImport(JitHelpers.QCall), SuppressUnmanagedCodeSecurity]
        static internal extern void ClearNative(IntPtr pUnk);

        [FriendAccessAllowed]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        static internal extern object ConvertToManagedWithoutUnboxing(IntPtr pNative);
    }  // class InterfaceMarshaler
#endif // FEATURE_COMINTEROP

#if FEATURE_COMINTEROP
    [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
    internal static class UriMarshaler
    {
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        static internal extern string GetRawUriFromNative(IntPtr pUri);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [System.Security.SecurityCritical]
        static unsafe internal extern IntPtr CreateNativeUriInstanceHelper(char* rawUri, int strLen);
      
    [System.Security.SecurityCritical]
        static unsafe internal IntPtr CreateNativeUriInstance(string rawUri)
        {
            fixed(char* pManaged = rawUri)
            {
                return CreateNativeUriInstanceHelper(pManaged, rawUri.Length);
            }
        }

    }  // class InterfaceMarshaler

    [FriendAccessAllowed]
    internal static class EventArgsMarshaler
    {
        [SecurityCritical]
        [FriendAccessAllowed]
        static internal IntPtr CreateNativeNCCEventArgsInstance(int action, object newItems, object oldItems, int newIndex, int oldIndex)
        {
            IntPtr newItemsIP = IntPtr.Zero;
            IntPtr oldItemsIP = IntPtr.Zero;

            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                if (newItems != null)
                    newItemsIP = Marshal.GetComInterfaceForObject(newItems, typeof(IBindableVector));
                if (oldItems != null)
                    oldItemsIP = Marshal.GetComInterfaceForObject(oldItems, typeof(IBindableVector));

                return CreateNativeNCCEventArgsInstanceHelper(action, newItemsIP, oldItemsIP, newIndex, oldIndex);
            }
            finally
            {
                if (!oldItemsIP.IsNull())
                    Marshal.Release(oldItemsIP);
                if (!newItemsIP.IsNull())
                    Marshal.Release(newItemsIP);
            }
        }

        [SecurityCritical]
        [FriendAccessAllowed]
        [DllImport(JitHelpers.QCall), SuppressUnmanagedCodeSecurity]
        static extern internal IntPtr CreateNativePCEventArgsInstance([MarshalAs(UnmanagedType.HString)]string name);

        [SecurityCritical]
        [DllImport(JitHelpers.QCall), SuppressUnmanagedCodeSecurity]
        static extern internal IntPtr CreateNativeNCCEventArgsInstanceHelper(int action, IntPtr newItem, IntPtr oldItem, int newIndex, int oldIndex);
    }
#endif // FEATURE_COMINTEROP

    [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
    internal static class MngdNativeArrayMarshaler
    {
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        static internal extern void CreateMarshaler(IntPtr pMarshalState, IntPtr pMT, int dwFlags);
        
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        static internal extern void ConvertSpaceToNative(IntPtr pMarshalState, ref object pManagedHome, IntPtr pNativeHome);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        static internal extern void ConvertContentsToNative(IntPtr pMarshalState, ref object pManagedHome, IntPtr pNativeHome);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        static internal extern void ConvertSpaceToManaged(IntPtr pMarshalState, ref object pManagedHome, IntPtr pNativeHome,
                                                          int cElements);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        static internal extern void ConvertContentsToManaged(IntPtr pMarshalState, ref object pManagedHome, IntPtr pNativeHome);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        static internal extern void ClearNative(IntPtr pMarshalState, IntPtr pNativeHome, int cElements);
        
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        static internal extern void ClearNativeContents(IntPtr pMarshalState, IntPtr pNativeHome, int cElements);
    }  // class MngdNativeArrayMarshaler

#if FEATURE_COMINTEROP
    [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
    internal static class MngdSafeArrayMarshaler
    {
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        static internal extern void CreateMarshaler(IntPtr pMarshalState, IntPtr pMT, int iRank, int dwFlags);
        
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        static internal extern void ConvertSpaceToNative(IntPtr pMarshalState, ref object pManagedHome, IntPtr pNativeHome);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        static internal extern void ConvertContentsToNative(IntPtr pMarshalState, ref object pManagedHome, IntPtr pNativeHome, object pOriginalManaged);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        static internal extern void ConvertSpaceToManaged(IntPtr pMarshalState, ref object pManagedHome, IntPtr pNativeHome);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        static internal extern void ConvertContentsToManaged(IntPtr pMarshalState, ref object pManagedHome, IntPtr pNativeHome);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        static internal extern void ClearNative(IntPtr pMarshalState, ref object pManagedHome, IntPtr pNativeHome);
    }  // class MngdSafeArrayMarshaler

    [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
    internal static class MngdHiddenLengthArrayMarshaler
    {
        [SecurityCritical]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        static internal extern void CreateMarshaler(IntPtr pMarshalState, IntPtr pMT, IntPtr cbElementSize, ushort vt);
        
        [SecurityCritical]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void ConvertSpaceToNative(IntPtr pMarshalState, ref object pManagedHome, IntPtr pNativeHome);

        [SecurityCritical]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void ConvertContentsToNative(IntPtr pMarshalState, ref object pManagedHome, IntPtr pNativeHome);

        [SecurityCritical]
        internal static unsafe void ConvertContentsToNative_DateTime(ref DateTimeOffset[] managedArray, IntPtr pNativeHome)
        {
            if (managedArray != null)
            {
                DateTimeNative *nativeBuffer = *(DateTimeNative **)pNativeHome;
                for (int i = 0; i < managedArray.Length; i++)
                {
                    DateTimeOffsetMarshaler.ConvertToNative(ref managedArray[i], out nativeBuffer[i]);
                }
            }
        }

        [SecurityCritical]
        internal static unsafe void ConvertContentsToNative_Type(ref System.Type[] managedArray, IntPtr pNativeHome)
        {
            if (managedArray != null)
            {
                TypeNameNative *nativeBuffer = *(TypeNameNative **)pNativeHome;
                for (int i = 0; i < managedArray.Length; i++)
                {
                    SystemTypeMarshaler.ConvertToNative(managedArray[i], &nativeBuffer[i]);
                }
            }
        }

        [SecurityCritical]
        internal static unsafe void ConvertContentsToNative_Exception(ref Exception[] managedArray, IntPtr pNativeHome)
        {
            if (managedArray != null)
            {
                Int32 *nativeBuffer = *(Int32 **)pNativeHome;
                for (int i = 0; i < managedArray.Length; i++)
                {
                    nativeBuffer[i] = HResultExceptionMarshaler.ConvertToNative(managedArray[i]);
                }
            }
        }

        [SecurityCritical]
        internal static unsafe void ConvertContentsToNative_Nullable<T>(ref Nullable<T>[] managedArray, IntPtr pNativeHome)
            where T : struct
        {
            if (managedArray != null)
            {
                IntPtr *nativeBuffer = *(IntPtr **)pNativeHome;
                for (int i = 0; i < managedArray.Length; i++)
                {
                    nativeBuffer[i] = NullableMarshaler.ConvertToNative<T>(ref managedArray[i]);
                }
            }
        }

        [SecurityCritical]
        internal static unsafe void ConvertContentsToNative_KeyValuePair<K, V>(ref KeyValuePair<K, V>[] managedArray, IntPtr pNativeHome)
        {
            if (managedArray != null)
            {
                IntPtr *nativeBuffer = *(IntPtr **)pNativeHome;
                for (int i = 0; i < managedArray.Length; i++)
                {
                    nativeBuffer[i] = KeyValuePairMarshaler.ConvertToNative<K, V>(ref managedArray[i]);
                }
            }
        }

        [SecurityCritical]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void ConvertSpaceToManaged(IntPtr pMarshalState, ref object pManagedHome, IntPtr pNativeHome, int elementCount);

        [SecurityCritical]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void ConvertContentsToManaged(IntPtr pMarshalState, ref object pManagedHome, IntPtr pNativeHome);

        [SecurityCritical]
        internal static unsafe void ConvertContentsToManaged_DateTime(ref DateTimeOffset[] managedArray, IntPtr pNativeHome)
        {
            if (managedArray != null)
            {
                DateTimeNative *nativeBuffer = *(DateTimeNative **)pNativeHome;
                for (int i = 0; i < managedArray.Length; i++)
                {
                    DateTimeOffsetMarshaler.ConvertToManaged(out managedArray[i], ref nativeBuffer[i]);
                }
            }
        }

        [SecurityCritical]
        internal static unsafe void ConvertContentsToManaged_Type(ref System.Type[] managedArray, IntPtr pNativeHome)
        {
            if (managedArray != null)
            {
                TypeNameNative *nativeBuffer = *(TypeNameNative **)pNativeHome;
                for (int i = 0; i < managedArray.Length; i++)
                {
                    SystemTypeMarshaler.ConvertToManaged(&nativeBuffer[i], ref managedArray[i]);
                }
            }
        }

        [SecurityCritical]
        internal static unsafe void ConvertContentsToManaged_Exception(ref Exception[] managedArray, IntPtr pNativeHome)
        {
            if (managedArray != null)
            {
                Int32 *nativeBuffer = *(Int32 **)pNativeHome;
                for (int i = 0; i < managedArray.Length; i++)
                {
                    managedArray[i] = HResultExceptionMarshaler.ConvertToManaged(nativeBuffer[i]);
                }
            }
        }

        [SecurityCritical]
        internal static unsafe void ConvertContentsToManaged_Nullable<T>(ref Nullable<T>[] managedArray, IntPtr pNativeHome)
            where T : struct
        {
            if (managedArray != null)
            {
                IntPtr *nativeBuffer = *(IntPtr **)pNativeHome;
                for (int i = 0; i < managedArray.Length; i++)
                {
                    managedArray[i] = NullableMarshaler.ConvertToManaged<T>(nativeBuffer[i]);
                }
            }
        }

        [SecurityCritical]
        internal static unsafe void ConvertContentsToManaged_KeyValuePair<K, V>(ref KeyValuePair<K, V>[] managedArray, IntPtr pNativeHome)
        {
            if (managedArray != null)
            {
                IntPtr *nativeBuffer = *(IntPtr **)pNativeHome;
                for (int i = 0; i < managedArray.Length; i++)
                {
                    managedArray[i] = KeyValuePairMarshaler.ConvertToManaged<K, V>(nativeBuffer[i]);
                }
            }
        }

        [SecurityCritical]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void ClearNativeContents(IntPtr pMarshalState, IntPtr pNativeHome, int cElements);

        [SecurityCritical]
        internal static unsafe void ClearNativeContents_Type(IntPtr pNativeHome, int cElements)
        {
            Contract.Assert(Environment.IsWinRTSupported);

            TypeNameNative *pNativeTypeArray = *(TypeNameNative **)pNativeHome;
            if (pNativeTypeArray != null)
            {
                for (int i = 0; i < cElements; ++i)
                {
                    SystemTypeMarshaler.ClearNative(pNativeTypeArray);
                    pNativeTypeArray++;
                }
            }
        }
    }  // class MngdHiddenLengthArrayMarshaler

#endif // FEATURE_COMINTEROP

    [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
    internal static class MngdRefCustomMarshaler
    {
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        static internal extern void CreateMarshaler(IntPtr pMarshalState, IntPtr pCMHelper);
        
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        static internal extern void ConvertContentsToNative(IntPtr pMarshalState, ref object pManagedHome, IntPtr pNativeHome);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        static internal extern void ConvertContentsToManaged(IntPtr pMarshalState, ref object pManagedHome, IntPtr pNativeHome);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        static internal extern void ClearNative(IntPtr pMarshalState, ref object pManagedHome, IntPtr pNativeHome);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        static internal extern void ClearManaged(IntPtr pMarshalState, ref object pManagedHome, IntPtr pNativeHome);
    }  // class MngdRefCustomMarshaler

    [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
    [System.Security.SecurityCritical]
    internal struct AsAnyMarshaler
    {
        private const ushort VTHACK_ANSICHAR = 253;
        private const ushort VTHACK_WINBOOL  = 254;

        private enum BackPropAction
        {
            None,
            Array,
            Layout,
            StringBuilderAnsi,
            StringBuilderUnicode
        }

        // Pointer to MngdNativeArrayMarshaler, ownership not assumed.
        private IntPtr pvArrayMarshaler;

        // Type of action to perform after the CLR-to-unmanaged call.
        private BackPropAction backPropAction;

        // The managed layout type for BackPropAction.Layout.
        private Type layoutType;

        // Cleanup list to be destroyed when clearing the native view (for layouts with SafeHandles).
        private CleanupWorkList cleanupWorkList;

        private static bool IsIn(int dwFlags)      { return ((dwFlags & 0x10000000) != 0); }
        private static bool IsOut(int dwFlags)     { return ((dwFlags & 0x20000000) != 0); }
        private static bool IsAnsi(int dwFlags)    { return ((dwFlags & 0x00FF0000) != 0); }
        private static bool IsThrowOn(int dwFlags) { return ((dwFlags & 0x0000FF00) != 0); }
        private static bool IsBestFit(int dwFlags) { return ((dwFlags & 0x000000FF) != 0); }

        internal AsAnyMarshaler(IntPtr pvArrayMarshaler)
        {
            // we need this in case the value being marshaled turns out to be array
            BCLDebug.Assert(pvArrayMarshaler != IntPtr.Zero, "pvArrayMarshaler must not be null");

            this.pvArrayMarshaler = pvArrayMarshaler;
            this.backPropAction = BackPropAction.None;
            this.layoutType = null;
            this.cleanupWorkList = null;
        }

        #region ConvertToNative helpers

        [System.Security.SecurityCritical]
        private unsafe IntPtr ConvertArrayToNative(object pManagedHome, int dwFlags)
        {
            Type elementType = pManagedHome.GetType().GetElementType();
            VarEnum vt = VarEnum.VT_EMPTY;

            switch (Type.GetTypeCode(elementType))
            {
                case TypeCode.SByte:   vt = VarEnum.VT_I1;  break;
                case TypeCode.Byte:    vt = VarEnum.VT_UI1; break;
                case TypeCode.Int16:   vt = VarEnum.VT_I2;  break;
                case TypeCode.UInt16:  vt = VarEnum.VT_UI2; break;
                case TypeCode.Int32:   vt = VarEnum.VT_I4;  break;
                case TypeCode.UInt32:  vt = VarEnum.VT_UI4; break;
                case TypeCode.Int64:   vt = VarEnum.VT_I8;  break;
                case TypeCode.UInt64:  vt = VarEnum.VT_UI8; break;
                case TypeCode.Single:  vt = VarEnum.VT_R4;  break;
                case TypeCode.Double:  vt = VarEnum.VT_R8;  break;
                case TypeCode.Char:    vt = (IsAnsi(dwFlags) ? (VarEnum)VTHACK_ANSICHAR : VarEnum.VT_UI2); break;
                case TypeCode.Boolean: vt = (VarEnum)VTHACK_WINBOOL; break;

                case TypeCode.Object:
                {
                    if (elementType == typeof(IntPtr))
                    {
                        vt = (IntPtr.Size == 4 ? VarEnum.VT_I4 : VarEnum.VT_I8);
                    }
                    else if (elementType == typeof(UIntPtr))
                    {
                        vt = (IntPtr.Size == 4 ? VarEnum.VT_UI4 : VarEnum.VT_UI8);
                    }
                    else goto default;
                    break;
                }

                default:
                    throw new ArgumentException(Environment.GetResourceString("Arg_NDirectBadObject"));
            }

            // marshal the object as C-style array (UnmanagedType.LPArray)
            int dwArrayMarshalerFlags = (int)vt;
            if (IsBestFit(dwFlags)) dwArrayMarshalerFlags |= (1 << 16);
            if (IsThrowOn(dwFlags)) dwArrayMarshalerFlags |= (1 << 24);

            MngdNativeArrayMarshaler.CreateMarshaler(
                pvArrayMarshaler,
                IntPtr.Zero,      // not needed as we marshal primitive VTs only
                dwArrayMarshalerFlags);

            IntPtr pNativeHome;
            IntPtr pNativeHomeAddr = new IntPtr(&pNativeHome);

            MngdNativeArrayMarshaler.ConvertSpaceToNative(
                pvArrayMarshaler,
                ref pManagedHome,
                pNativeHomeAddr);

            if (IsIn(dwFlags))
            {
                MngdNativeArrayMarshaler.ConvertContentsToNative(
                    pvArrayMarshaler,
                    ref pManagedHome,
                    pNativeHomeAddr);
            }
            if (IsOut(dwFlags))
            {
                backPropAction = BackPropAction.Array;
            }

            return pNativeHome;
        }

        [System.Security.SecurityCritical]
        private static IntPtr ConvertStringToNative(string pManagedHome, int dwFlags)
        {
            IntPtr pNativeHome;

            // IsIn, IsOut are ignored for strings - they're always in-only
            if (IsAnsi(dwFlags))
            {
                // marshal the object as Ansi string (UnmanagedType.LPStr)
                pNativeHome = CSTRMarshaler.ConvertToNative(
                    dwFlags & 0xFFFF, // (throw on unmappable char << 8 | best fit)
                    pManagedHome,     //
                    IntPtr.Zero);     // unmanaged buffer will be allocated
            }
            else
            {
                // marshal the object as Unicode string (UnmanagedType.LPWStr)
                StubHelpers.CheckStringLength(pManagedHome.Length);

                int allocSize = (pManagedHome.Length + 1) * 2;
                pNativeHome = Marshal.AllocCoTaskMem(allocSize);

                String.InternalCopy(pManagedHome, pNativeHome, allocSize);
            }

            return pNativeHome;
        }

        [System.Security.SecurityCritical]
        private unsafe IntPtr ConvertStringBuilderToNative(StringBuilder pManagedHome, int dwFlags)
        {
            IntPtr pNativeHome;

            // P/Invoke can be used to call Win32 apis that don't strictly follow CLR in/out semantics and thus may
            // leave garbage in the buffer in circumstances that we can't detect. To prevent us from crashing when
            // converting the contents back to managed, put a hidden NULL terminator past the end of the official buffer.

            // Unmanaged layout:
            // +====================================+
            // | Extra hidden NULL                  |
            // +====================================+ \
            // |                                    | |
            // | [Converted] NULL-terminated string | |- buffer that the target may change
            // |                                    | |
            // +====================================+ / <-- native home

            // Note that StringBuilder.Capacity is the number of characters NOT including any terminators.

            if (IsAnsi(dwFlags))
            {
                StubHelpers.CheckStringLength(pManagedHome.Capacity);

                // marshal the object as Ansi string (UnmanagedType.LPStr)
                int allocSize = (pManagedHome.Capacity * Marshal.SystemMaxDBCSCharSize) + 4;
                pNativeHome = Marshal.AllocCoTaskMem(allocSize);

                byte* ptr = (byte*)pNativeHome;
                *(ptr + allocSize - 3) = 0;
                *(ptr + allocSize - 2) = 0;
                *(ptr + allocSize - 1) = 0;

                if (IsIn(dwFlags))
                {
                    int length = pManagedHome.ToString().ConvertToAnsi(
                        ptr, allocSize,
                        IsBestFit(dwFlags),
                        IsThrowOn(dwFlags));
                    Contract.Assert(length < allocSize, "Expected a length less than the allocated size");
                }
                if (IsOut(dwFlags))
                {
                    backPropAction = BackPropAction.StringBuilderAnsi;
                }
            }
            else
            {
                // marshal the object as Unicode string (UnmanagedType.LPWStr)
                int allocSize = (pManagedHome.Capacity * 2) + 4;
                pNativeHome = Marshal.AllocCoTaskMem(allocSize);

                byte* ptr = (byte*)pNativeHome;
                *(ptr + allocSize - 1) = 0;
                *(ptr + allocSize - 2) = 0;

                if (IsIn(dwFlags))
                {
                    int length = pManagedHome.Length * 2;
                    pManagedHome.InternalCopy(pNativeHome, length);

                    // null-terminate the native string
                    *(ptr + length + 0) = 0;
                    *(ptr + length + 1) = 0;
                }
                if (IsOut(dwFlags))
                {
                    backPropAction = BackPropAction.StringBuilderUnicode;
                }
            }

            return pNativeHome;
        }

        [System.Security.SecurityCritical]
        private unsafe IntPtr ConvertLayoutToNative(object pManagedHome, int dwFlags)
        {
            // Note that the following call will not throw exception if the type
            // of pManagedHome is not marshalable. That's intentional because we
            // want to maintain the original behavior where this was indicated
            // by TypeLoadException during the actual field marshaling.
            int allocSize = Marshal.SizeOfHelper(pManagedHome.GetType(), false);
            IntPtr pNativeHome = Marshal.AllocCoTaskMem(allocSize);

            // marshal the object as class with layout (UnmanagedType.LPStruct)
            if (IsIn(dwFlags))
            {
                StubHelpers.FmtClassUpdateNativeInternal(pManagedHome, (byte *)pNativeHome.ToPointer(), ref cleanupWorkList);
            }
            if (IsOut(dwFlags))
            {
                backPropAction = BackPropAction.Layout;
            }
            layoutType = pManagedHome.GetType();

            return pNativeHome;
        }

        #endregion

        [System.Security.SecurityCritical]
        internal IntPtr ConvertToNative(object pManagedHome, int dwFlags)
        {
            if (pManagedHome == null)
                return IntPtr.Zero;

            if (pManagedHome is ArrayWithOffset)
                throw new ArgumentException(Environment.GetResourceString("Arg_MarshalAsAnyRestriction"));

            IntPtr pNativeHome;

            if (pManagedHome.GetType().IsArray)
            {
                // array (LPArray)
                pNativeHome = ConvertArrayToNative(pManagedHome, dwFlags);
            }
            else
            {
                string strValue;
                StringBuilder sbValue;

                if ((strValue = pManagedHome as string) != null)
                {
                    // string (LPStr or LPWStr)
                    pNativeHome = ConvertStringToNative(strValue, dwFlags);
                }
                else if ((sbValue = pManagedHome as StringBuilder) != null)
                {
                    // StringBuilder (LPStr or LPWStr)
                    pNativeHome = ConvertStringBuilderToNative(sbValue, dwFlags);
                }
                else if (pManagedHome.GetType().IsLayoutSequential || pManagedHome.GetType().IsExplicitLayout)
                {
                    // layout (LPStruct)
                    pNativeHome = ConvertLayoutToNative(pManagedHome, dwFlags);
                }
                else
                {
                    // this type is not supported for AsAny marshaling
                    throw new ArgumentException(Environment.GetResourceString("Arg_NDirectBadObject"));
                }
            }

            return pNativeHome;
        }

        [System.Security.SecurityCritical]
        internal unsafe void ConvertToManaged(object pManagedHome, IntPtr pNativeHome)
        {
            switch (backPropAction)
            {
                case BackPropAction.Array:
                {
                    MngdNativeArrayMarshaler.ConvertContentsToManaged(
                        pvArrayMarshaler,
                        ref pManagedHome,
                        new IntPtr(&pNativeHome));
                    break;
                }

                case BackPropAction.Layout:
                {
                    StubHelpers.FmtClassUpdateCLRInternal(pManagedHome, (byte *)pNativeHome.ToPointer());
                    break;
                }

                case BackPropAction.StringBuilderAnsi:
                {
                    sbyte* ptr = (sbyte*)pNativeHome.ToPointer();
                    ((StringBuilder)pManagedHome).ReplaceBufferAnsiInternal(ptr, Win32Native.lstrlenA(pNativeHome));
                    break;
                }

                case BackPropAction.StringBuilderUnicode:
                {
                    char* ptr = (char*)pNativeHome.ToPointer();
                    ((StringBuilder)pManagedHome).ReplaceBufferInternal(ptr, Win32Native.lstrlenW(pNativeHome));
                    break;
                }

                // nothing to do for BackPropAction.None
            }
        }

        [System.Security.SecurityCritical]
        internal void ClearNative(IntPtr pNativeHome)
        {
            if (pNativeHome != IntPtr.Zero)
            {
                if (layoutType != null)
                {
                    // this must happen regardless of BackPropAction
                    Marshal.DestroyStructure(pNativeHome, layoutType);
                }
                Win32Native.CoTaskMemFree(pNativeHome);
            }
            StubHelpers.DestroyCleanupList(ref cleanupWorkList);
        }
    }  // struct AsAnyMarshaler

#if FEATURE_COMINTEROP
    [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
    internal static class NullableMarshaler
    {    
        [SecurityCritical]
        static internal IntPtr ConvertToNative<T>(ref Nullable<T> pManaged) where T : struct
        {
            if (pManaged.HasValue)
            {
                object impl = IReferenceFactory.CreateIReference(pManaged);
                return Marshal.GetComInterfaceForObject(impl, typeof(IReference<T>));
            }
            else
            {
                return IntPtr.Zero;
            }
        }
        
        [SecurityCritical]
        static internal void ConvertToManagedRetVoid<T>(IntPtr pNative, ref Nullable<T> retObj) where T : struct
        {
            retObj = ConvertToManaged<T>(pNative);
        }


        [SecurityCritical]
        static internal Nullable<T> ConvertToManaged<T>(IntPtr pNative) where T : struct
        {
            if (pNative != IntPtr.Zero)
            {
                object wrapper = InterfaceMarshaler.ConvertToManagedWithoutUnboxing(pNative);
                return (Nullable<T>)CLRIReferenceImpl<T>.UnboxHelper(wrapper);
            }
            else
            {
                return new Nullable<T>();
            }
        }
    }  // class NullableMarshaler

    // Corresponds to Windows.UI.Xaml.Interop.TypeName
    [StructLayout(LayoutKind.Sequential)]
    internal struct TypeNameNative
    {

        internal IntPtr     typeName;           // HSTRING
        internal TypeKind   typeKind;           // TypeKind enum
    }

    // Corresponds to Windows.UI.Xaml.TypeSource
    internal enum TypeKind
    {
        Primitive,
        Metadata,
        Projection
    };

    internal static class WinRTTypeNameConverter
    {
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern string ConvertToWinRTTypeName(System.Type managedType, out bool isPrimitive);
        
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern System.Type GetTypeFromWinRTTypeName(string typeName, out bool isPrimitive);
    }
    
    [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
    internal static class SystemTypeMarshaler
    {   
        [SecurityCritical]
        internal static unsafe void ConvertToNative(System.Type managedType, TypeNameNative *pNativeType)
        {
            if (!Environment.IsWinRTSupported)
            {
                throw new PlatformNotSupportedException(Environment.GetResourceString("PlatformNotSupported_WinRT"));
            }
            
            string typeName;
            if (managedType != null)
            {
                if (managedType.GetType() != typeof(System.RuntimeType))
                {   // The type should be exactly System.RuntimeType (and not its child System.ReflectionOnlyType, or other System.Type children)
                    throw new ArgumentException(Environment.GetResourceString("Argument_WinRTSystemRuntimeType", managedType.GetType().ToString()));
                }

                bool isPrimitive;
                string winrtTypeName = WinRTTypeNameConverter.ConvertToWinRTTypeName(managedType, out isPrimitive);
                if (winrtTypeName != null)
                {
                    // Must be a WinRT type, either in a WinMD or a Primitive
                    typeName = winrtTypeName;
                    if (isPrimitive)
                        pNativeType->typeKind = TypeKind.Primitive;
                    else
                        pNativeType->typeKind = TypeKind.Metadata;
                }
                else
                {
                    // Custom .NET type
                    typeName = managedType.AssemblyQualifiedName;
                    pNativeType->typeKind = TypeKind.Projection;
                }
            }
            else
            {   // Marshal null as empty string + Projection
                typeName = "";
                pNativeType->typeKind = TypeKind.Projection;
            }

            int hrCreate = System.Runtime.InteropServices.WindowsRuntime.UnsafeNativeMethods.WindowsCreateString(typeName, typeName.Length, &pNativeType->typeName);
            Marshal.ThrowExceptionForHR(hrCreate, new IntPtr(-1));
        }
        
        [SecurityCritical]
        internal static unsafe void ConvertToManaged(TypeNameNative *pNativeType, ref System.Type managedType)
        {
            if (!Environment.IsWinRTSupported)
            {
                throw new PlatformNotSupportedException(Environment.GetResourceString("PlatformNotSupported_WinRT"));
            }
            
            string typeName = WindowsRuntimeMarshal.HStringToString(pNativeType->typeName);
            if (String.IsNullOrEmpty(typeName))
            {
                managedType = null;
                return;
            }

            if (pNativeType->typeKind == TypeKind.Projection)
            {
                managedType = Type.GetType(typeName, /* throwOnError = */ true);
            }
            else
            {
                bool isPrimitive;
                managedType = WinRTTypeNameConverter.GetTypeFromWinRTTypeName(typeName, out isPrimitive);

                // TypeSource must match
                if (isPrimitive != (pNativeType->typeKind == TypeKind.Primitive))
                    throw new ArgumentException(Environment.GetResourceString("Argument_Unexpected_TypeSource"));
            }
        }
        
        [SecurityCritical]
        internal static unsafe void ClearNative(TypeNameNative *pNativeType)
        {
            Contract.Assert(Environment.IsWinRTSupported);

            if (pNativeType->typeName != IntPtr.Zero)
            {
                System.Runtime.InteropServices.WindowsRuntime.UnsafeNativeMethods.WindowsDeleteString(pNativeType->typeName);
            }
        }
    }  // class SystemTypeMarshaler

    // For converting WinRT's Windows.Foundation.HResult into System.Exception and vice versa.
    [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
    internal static class HResultExceptionMarshaler
    {
        static internal unsafe int ConvertToNative(Exception ex)
        {
            if (!Environment.IsWinRTSupported)
            {
                throw new PlatformNotSupportedException(Environment.GetResourceString("PlatformNotSupported_WinRT"));
            }

            if (ex == null)
                return 0;  // S_OK;

            return ex._HResult;
        }

        [SecuritySafeCritical]
        static internal unsafe Exception ConvertToManaged(int hr)
        {
            Contract.Ensures(Contract.Result<Exception>() != null || hr >= 0);

            if (!Environment.IsWinRTSupported)
            {
                throw new PlatformNotSupportedException(Environment.GetResourceString("PlatformNotSupported_WinRT"));
            }

            Exception e = null;
            if (hr < 0)
            {
                e = StubHelpers.InternalGetCOMHRExceptionObject(hr, IntPtr.Zero, null, /* fForWinRT */ true);
            }

            // S_OK should be marshaled as null.  WinRT API's should not return S_FALSE by convention.
            // We've chosen to treat S_FALSE as success and return null.
            Contract.Assert(e != null || hr == 0 || hr == 1, "Unexpected HRESULT - it is a success HRESULT (without the high bit set) other than S_OK & S_FALSE.");
            return e;
        }
    }  // class HResultExceptionMarshaler

    [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
    internal static class KeyValuePairMarshaler
    {    
        [SecurityCritical]
        internal static IntPtr ConvertToNative<K, V>([In] ref KeyValuePair<K, V> pair)
        {
            IKeyValuePair<K, V> impl = new CLRIKeyValuePairImpl<K, V>(ref pair);
            return Marshal.GetComInterfaceForObject(impl, typeof(IKeyValuePair<K, V>));
        }
        
        [SecurityCritical]
        internal static KeyValuePair<K, V> ConvertToManaged<K, V>(IntPtr pInsp)
        {
            object obj = InterfaceMarshaler.ConvertToManagedWithoutUnboxing(pInsp);

            IKeyValuePair<K, V> pair = (IKeyValuePair<K, V>)obj;
            return new KeyValuePair<K, V>(pair.Key, pair.Value);
        }

        // Called from COMInterfaceMarshaler
        [SecurityCritical]
        internal static object ConvertToManagedBox<K, V>(IntPtr pInsp)
        {
            return (object)ConvertToManaged<K, V>(pInsp);
        }
    }  // class KeyValuePairMarshaler

#endif // FEATURE_COMINTEROP

    [StructLayout(LayoutKind.Sequential)]
    internal struct NativeVariant
    {
        ushort vt;
        ushort wReserved1;
        ushort wReserved2;
        ushort wReserved3;

        // The union portion of the structure contains at least one 64-bit type that on some 32-bit platforms
        // (notably  ARM) requires 64-bit alignment. So on 32-bit platforms we'll actually size the variant
        // portion of the struct with an Int64 so the type loader notices this requirement (a no-op on x86,
        // but on ARM it will allow us to correctly determine the layout of native argument lists containing
        // VARIANTs). Note that the field names here don't matter: none of the code refers to these fields,
        // the structure just exists to provide size information to the IL marshaler.
#if BIT64
        IntPtr data1;
        IntPtr data2;
#else
        Int64  data1;
#endif
    }  // struct NativeVariant

#if !BIT64 && !FEATURE_CORECLR
    // Structure filled by IL stubs if copy constructor(s) and destructor(s) need to be called
    // on value types pushed on the stack. The structure is stored in s_copyCtorStubDesc by
    // SetCopyCtorCookieChain and fetched by CopyCtorCallStubWorker. Must be stack-allocated.
    [StructLayout(LayoutKind.Sequential)]
    unsafe internal struct CopyCtorStubCookie
    {
        public void SetData(IntPtr srcInstancePtr, uint dstStackOffset, IntPtr ctorPtr, IntPtr dtorPtr)
        {
            m_srcInstancePtr = srcInstancePtr;
            m_dstStackOffset = dstStackOffset;
            m_ctorPtr = ctorPtr;
            m_dtorPtr = dtorPtr;
        }

        public void SetNext(IntPtr pNext)
        {
            m_pNext = pNext;
        }

        public IntPtr m_srcInstancePtr; // pointer to the source instance
        public uint   m_dstStackOffset; // offset from the start of stack arguments of the pushed 'this' instance

        public IntPtr m_ctorPtr;        // fnptr to the managed copy constructor, result of ldftn
        public IntPtr m_dtorPtr;        // fnptr to the managed destructor, result of ldftn

        public IntPtr m_pNext;          // pointer to next cookie in the chain or IntPtr.Zero
    }  // struct CopyCtorStubCookie

    // Aggregates pointer to CopyCtorStubCookie and the target of the interop call.
    [StructLayout(LayoutKind.Sequential)]
    unsafe internal struct CopyCtorStubDesc
    {
        public IntPtr m_pCookie;
        public IntPtr m_pTarget;
    }  // struct CopyCtorStubDes
#endif // !BIT64 && !FEATURE_CORECLR

    // Aggregates SafeHandle and the "owned" bit which indicates whether the SafeHandle
    // has been successfully AddRef'ed. This allows us to do realiable cleanup (Release)
    // if and only if it is needed.
    [System.Security.SecurityCritical]
    internal sealed class CleanupWorkListElement
    {
        public CleanupWorkListElement(SafeHandle handle)
        {
            m_handle = handle;
        }

        public SafeHandle m_handle;

        // This field is passed by-ref to SafeHandle.DangerousAddRef.
        // CleanupWorkList.Destroy ignores this element if m_owned is not set to true.
        public bool m_owned;
    }  // class CleanupWorkListElement

    [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
    [System.Security.SecurityCritical]
    internal sealed class CleanupWorkList
    {
        private List<CleanupWorkListElement> m_list = new List<CleanupWorkListElement>();
        
        public void Add(CleanupWorkListElement elem)
        {
            BCLDebug.Assert(elem.m_owned == false, "m_owned is supposed to be false and set later by DangerousAddRef");
            m_list.Add(elem);
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        public void Destroy()
        {
            for (int i = m_list.Count - 1; i >= 0; i--)
            {
                if (m_list[i].m_owned)
                    StubHelpers.SafeHandleRelease(m_list[i].m_handle);
            }
        }
    }  // class CleanupWorkList

    [System.Security.SecurityCritical]  // auto-generated
    [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
    [SuppressUnmanagedCodeSecurityAttribute()]
    internal static class StubHelpers
    {
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        static internal extern bool IsQCall(IntPtr pMD);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        static internal extern void InitDeclaringType(IntPtr pMD);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        static internal extern IntPtr GetNDirectTarget(IntPtr pMD);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        static internal extern IntPtr GetDelegateTarget(Delegate pThis, ref IntPtr pStubArg);

#if !BIT64 && !FEATURE_CORECLR
        // Written to by a managed stub helper, read by CopyCtorCallStubWorker in VM.
        [ThreadStatic]
        static CopyCtorStubDesc s_copyCtorStubDesc;

        static internal void SetCopyCtorCookieChain(IntPtr pStubArg, IntPtr pUnmngThis, int dwStubFlags, IntPtr pCookie)
        {
            // we store both the cookie chain head and the target of the copy ctor stub to a thread
            // static field to be accessed by the copy ctor (see code:CopyCtorCallStubWorker)
            s_copyCtorStubDesc.m_pCookie = pCookie;
            s_copyCtorStubDesc.m_pTarget = GetFinalStubTarget(pStubArg, pUnmngThis, dwStubFlags);
        }

        // Returns the final unmanaged stub target, ignores interceptors.
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        static internal extern IntPtr GetFinalStubTarget(IntPtr pStubArg, IntPtr pUnmngThis, int dwStubFlags);
#endif // !FEATURE_CORECLR && !BIT64

#if !FEATURE_CORECLR
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        static internal extern void DemandPermission(IntPtr pNMD);
#endif // !FEATURE_CORECLR

#if FEATURE_CORECLR
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        static internal extern void ClearLastError();
#endif

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        static internal extern void SetLastError();

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        static internal extern void ThrowInteropParamException(int resID, int paramIdx);

        [System.Security.SecurityCritical]
        static internal IntPtr AddToCleanupList(ref CleanupWorkList pCleanupWorkList, SafeHandle handle)
        {
            if (pCleanupWorkList == null)
                pCleanupWorkList = new CleanupWorkList();

            CleanupWorkListElement element = new CleanupWorkListElement(handle);
            pCleanupWorkList.Add(element);

            // element.m_owned will be true iff the AddRef succeeded
            return SafeHandleAddRef(handle, ref element.m_owned);
        }

        [System.Security.SecurityCritical]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        static internal void DestroyCleanupList(ref CleanupWorkList pCleanupWorkList)
        {
            if (pCleanupWorkList != null)
            {
                pCleanupWorkList.Destroy();
                pCleanupWorkList = null;
            }
        }

        static internal Exception GetHRExceptionObject(int hr)
        {
            Exception ex = InternalGetHRExceptionObject(hr);
            ex.InternalPreserveStackTrace();
            return ex;
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        static internal extern Exception InternalGetHRExceptionObject(int hr);

#if FEATURE_COMINTEROP
        static internal Exception GetCOMHRExceptionObject(int hr, IntPtr pCPCMD, object pThis)
        {
            Exception ex = InternalGetCOMHRExceptionObject(hr, pCPCMD, pThis, false);
            ex.InternalPreserveStackTrace();
            return ex;
        }

        static internal Exception GetCOMHRExceptionObject_WinRT(int hr, IntPtr pCPCMD, object pThis)
        {
            Exception ex = InternalGetCOMHRExceptionObject(hr, pCPCMD, pThis, true);
            ex.InternalPreserveStackTrace();
            return ex;
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        static internal extern Exception InternalGetCOMHRExceptionObject(int hr, IntPtr pCPCMD, object pThis, bool fForWinRT);

#endif // FEATURE_COMINTEROP

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        static internal extern IntPtr CreateCustomMarshalerHelper(IntPtr pMD, int paramToken, IntPtr hndManagedType);

        //-------------------------------------------------------
        // SafeHandle Helpers
        //-------------------------------------------------------
        
        // AddRefs the SH and returns the underlying unmanaged handle.
        [System.Security.SecurityCritical]  // auto-generated
        static internal IntPtr SafeHandleAddRef(SafeHandle pHandle, ref bool success)
        {
            if (pHandle == null)
            {
                throw new ArgumentNullException("pHandle", Environment.GetResourceString("ArgumentNull_SafeHandle"));
            }
            Contract.EndContractBlock();

            pHandle.DangerousAddRef(ref success);

            return (success ? pHandle.DangerousGetHandle() : IntPtr.Zero);
        }

        // Releases the SH (to be called from finally block).
        [System.Security.SecurityCritical]  // auto-generated
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        static internal void SafeHandleRelease(SafeHandle pHandle)
        {
            if (pHandle == null)
            {
                throw new ArgumentNullException("pHandle", Environment.GetResourceString("ArgumentNull_SafeHandle"));
            }
            Contract.EndContractBlock();

            try
            {
                pHandle.DangerousRelease();
            }
#if MDA_SUPPORTED
            catch (Exception ex)
            {
                Mda.ReportErrorSafeHandleRelease(ex);
            }
#else // MDA_SUPPORTED
            catch (Exception)
            { }
#endif // MDA_SUPPORTED
        }

#if FEATURE_COMINTEROP
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        static internal extern IntPtr GetCOMIPFromRCW(object objSrc, IntPtr pCPCMD, out IntPtr ppTarget, out bool pfNeedsRelease);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        static internal extern IntPtr GetCOMIPFromRCW_WinRT(object objSrc, IntPtr pCPCMD, out IntPtr ppTarget);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        static internal extern IntPtr GetCOMIPFromRCW_WinRTSharedGeneric(object objSrc, IntPtr pCPCMD, out IntPtr ppTarget);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        static internal extern IntPtr GetCOMIPFromRCW_WinRTDelegate(object objSrc, IntPtr pCPCMD, out IntPtr ppTarget);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        static internal extern bool ShouldCallWinRTInterface(object objSrc, IntPtr pCPCMD);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        static internal extern Delegate GetTargetForAmbiguousVariantCall(object objSrc, IntPtr pMT, out bool fUseString);

        //-------------------------------------------------------
        // Helper for the MDA RaceOnRCWCleanup
        //-------------------------------------------------------
        
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        static internal extern void StubRegisterRCW(object pThis);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        static internal extern void StubUnregisterRCW(object pThis);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        static internal extern IntPtr GetDelegateInvokeMethod(Delegate pThis);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [System.Security.SecurityCritical]
        static internal extern object GetWinRTFactoryObject(IntPtr pCPCMD);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [System.Security.SecurityCritical]
        static internal extern IntPtr GetWinRTFactoryReturnValue(object pThis, IntPtr pCtorEntry);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [System.Security.SecurityCritical]
        static internal extern IntPtr GetOuterInspectable(object pThis, IntPtr pCtorMD);

#if MDA_SUPPORTED
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        static internal extern Exception TriggerExceptionSwallowedMDA(Exception ex, IntPtr pManagedTarget);
#endif // MDA_SUPPORTED

#endif // FEATURE_COMINTEROP

#if MDA_SUPPORTED
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        static internal extern void CheckCollectedDelegateMDA(IntPtr pEntryThunk);
#endif // MDA_SUPPORTED

        //-------------------------------------------------------
        // Profiler helpers
        //-------------------------------------------------------
#if PROFILING_SUPPORTED
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        static internal extern IntPtr ProfilerBeginTransitionCallback(IntPtr pSecretParam, IntPtr pThread, object pThis);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        static internal extern void ProfilerEndTransitionCallback(IntPtr pMD, IntPtr pThread);
#endif // PROFILING_SUPPORTED

        //------------------------------------------------------
        // misc
        //------------------------------------------------------
        static internal void CheckStringLength(int length)
        {
            CheckStringLength((uint)length);
        }

        static internal void CheckStringLength(uint length)
        {
            if (length > 0x7ffffff0)
            {
                throw new MarshalDirectiveException(Environment.GetResourceString("Marshaler_StringTooLong"));
            }
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        static internal unsafe extern int strlen(sbyte* ptr);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        static internal extern void DecimalCanonicalizeInternal(ref Decimal dec);
        
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        static internal unsafe extern void FmtClassUpdateNativeInternal(object obj, byte* pNative, ref CleanupWorkList pCleanupWorkList);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        static internal unsafe extern void FmtClassUpdateCLRInternal(object obj, byte* pNative);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        static internal unsafe extern void LayoutDestroyNativeInternal(byte* pNative, IntPtr pMT);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        static internal extern object AllocateInternal(IntPtr typeHandle);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        static internal extern void MarshalToUnmanagedVaListInternal(IntPtr va_list, uint vaListSize, IntPtr pArgIterator);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        static internal extern void MarshalToManagedVaListInternal(IntPtr va_list, IntPtr pArgIterator);
        
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        static internal extern uint CalcVaListSize(IntPtr va_list);
        
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        static internal extern void ValidateObject(object obj, IntPtr pMD, object pThis);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        static internal extern void LogPinnedArgument(IntPtr localDesc, IntPtr nativeArg);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        static internal extern void ValidateByref(IntPtr byref, IntPtr pMD, object pThis); // the byref is pinned so we can safely "cast" it to IntPtr

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        static internal extern IntPtr GetStubContext();

#if BIT64
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        static internal extern IntPtr GetStubContextAddr();
#endif // BIT64

#if MDA_SUPPORTED
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        static internal extern void TriggerGCForMDA();        
#endif // MDA_SUPPORTED

#if FEATURE_ARRAYSTUB_AS_IL
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void ArrayTypeCheck(object o, Object[] arr);
#endif

#if FEATURE_STUBS_AS_IL
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void MulticastDebuggerTraceHelper(object o, Int32 count);
#endif
    }  // class StubHelpers
}
