// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
namespace System.StubHelpers
{
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
    using System.Diagnostics;

    internal static class AnsiCharMarshaler
    {
        // The length of the returned array is an approximation based on the length of the input string and the system
        // character set. It is only guaranteed to be larger or equal to cbLength, don't depend on the exact value.
        unsafe internal static byte[] DoAnsiConversion(string str, bool fBestFit, bool fThrowOnUnmappableChar, out int cbLength)
        {
            byte[] buffer = new byte[checked((str.Length + 1) * Marshal.SystemMaxDBCSCharSize)];
            fixed (byte* bufferPtr = &buffer[0])
            {
                cbLength = Marshal.StringToAnsiString(str, bufferPtr, buffer.Length, fBestFit, fThrowOnUnmappableChar);
            }
            return buffer;
        }

        unsafe internal static byte ConvertToNative(char managedChar, bool fBestFit, bool fThrowOnUnmappableChar)
        {
            int cbAllocLength = (1 + 1) * Marshal.SystemMaxDBCSCharSize;
            byte* bufferPtr = stackalloc byte[cbAllocLength];

            int cbLength = Marshal.StringToAnsiString(managedChar.ToString(), bufferPtr, cbAllocLength, fBestFit, fThrowOnUnmappableChar);

            Debug.Assert(cbLength > 0, "Zero bytes returned from DoAnsiConversion in AnsiCharMarshaler.ConvertToNative");
            return bufferPtr[0];
        }

        internal static char ConvertToManaged(byte nativeChar)
        {
            var bytes = new ReadOnlySpan<byte>(ref nativeChar, 1);
            string str = Encoding.Default.GetString(bytes);
            return str[0];
        }
    }  // class AnsiCharMarshaler

    internal static class CSTRMarshaler
    {
        internal static unsafe IntPtr ConvertToNative(int flags, string strManaged, IntPtr pNativeBuffer)
        {
            if (null == strManaged)
            {
                return IntPtr.Zero;
            }

            int nb;
            byte* pbNativeBuffer = (byte*)pNativeBuffer;

            if (pbNativeBuffer != null || Marshal.SystemMaxDBCSCharSize == 1)
            {
                // If we are marshaling into a stack buffer or we can accurately estimate the size of the required heap
                // space, we will use a "1-pass" mode where we convert the string directly into the unmanaged buffer.

                // + 1 for the null character from the user.  + 1 for the null character we put in.
                nb = checked((strManaged.Length + 1) * Marshal.SystemMaxDBCSCharSize + 1);

                // Use the pre-allocated buffer (allocated by localloc IL instruction) if not NULL, 
                // otherwise fallback to AllocCoTaskMem
                if (pbNativeBuffer == null)
                {
                    pbNativeBuffer = (byte*)Marshal.AllocCoTaskMem(nb);
                }

                nb = Marshal.StringToAnsiString(strManaged, pbNativeBuffer, nb,
                    bestFit: 0 != (flags & 0xFF), throwOnUnmappableChar: 0 != (flags >> 8));
            }
            else
            {
                // Otherwise we use a slower "2-pass" mode where we first marshal the string into an intermediate buffer
                // (managed byte array) and then allocate exactly the right amount of unmanaged memory. This is to avoid
                // wasting memory on systems with multibyte character sets where the buffer we end up with is often much
                // smaller than the upper bound for the given managed string.

                byte[] bytes = AnsiCharMarshaler.DoAnsiConversion(strManaged,
                    fBestFit: 0 != (flags & 0xFF), fThrowOnUnmappableChar: 0 != (flags >> 8), out nb);

                // + 1 for the null character from the user.  + 1 for the null character we put in.
                pbNativeBuffer = (byte*)Marshal.AllocCoTaskMem(nb + 2);

                Buffer.Memcpy(pbNativeBuffer, 0, bytes, 0, nb);
            }

            pbNativeBuffer[nb] = 0x00;
            pbNativeBuffer[nb + 1] = 0x00;

            return (IntPtr)pbNativeBuffer;
        }

        internal static unsafe string? ConvertToManaged(IntPtr cstr)
        {
            if (IntPtr.Zero == cstr)
                return null;
            else
                return new string((sbyte*)cstr);
        }

        internal static void ClearNative(IntPtr pNative)
        {
            Interop.Ole32.CoTaskMemFree(pNative);
        }
    }  // class CSTRMarshaler

    internal static class UTF8Marshaler
    {
        private const int MAX_UTF8_CHAR_SIZE = 3;
        internal static unsafe IntPtr ConvertToNative(int flags, string strManaged, IntPtr pNativeBuffer)
        {
            if (null == strManaged)
            {
                return IntPtr.Zero;
            }

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

        internal static unsafe string? ConvertToManaged(IntPtr cstr)
        {
            if (IntPtr.Zero == cstr)
                return null;

            byte* pBytes = (byte*)cstr;
            int nbBytes = string.strlen(pBytes);
            return string.CreateStringFromEncoding(pBytes, nbBytes, Encoding.UTF8);
        }

        internal static void ClearNative(IntPtr pNative)
        {
            if (pNative != IntPtr.Zero)
            {
                Interop.Ole32.CoTaskMemFree(pNative);
            }
        }
    }

    internal static class UTF8BufferMarshaler
    {
        internal static unsafe IntPtr ConvertToNative(StringBuilder sb, IntPtr pNativeBuffer, int flags)
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

        internal static unsafe void ConvertToManaged(StringBuilder sb, IntPtr pNative)
        {
            if (pNative == IntPtr.Zero)
                return;

            byte* pBytes = (byte*)pNative;
            int nbBytes = string.strlen(pBytes);
            sb.ReplaceBufferUtf8Internal(new ReadOnlySpan<byte>(pBytes, nbBytes));
        }
    }

    internal static class BSTRMarshaler
    {
        internal static unsafe IntPtr ConvertToNative(string strManaged, IntPtr pNativeBuffer)
        {
            if (null == strManaged)
            {
                return IntPtr.Zero;
            }
            else
            {
                byte trailByte;
                bool hasTrailByte = strManaged.TryGetTrailByte(out trailByte);

                uint lengthInBytes = (uint)strManaged.Length * 2;

                if (hasTrailByte)
                {
                    // this is an odd-sized string with a trailing byte stored in its sync block
                    lengthInBytes++;
                }

                byte* ptrToFirstChar;

                if (pNativeBuffer != IntPtr.Zero)
                {
                    // If caller provided a buffer, construct the BSTR manually. The size
                    // of the buffer must be at least (lengthInBytes + 6) bytes.
#if DEBUG
                    uint length = *((uint*)pNativeBuffer);
                    Debug.Assert(length >= lengthInBytes + 6, "BSTR localloc'ed buffer is too small");
#endif

                    // set length
                    *((uint*)pNativeBuffer) = lengthInBytes;

                    ptrToFirstChar = (byte*)pNativeBuffer + 4;
                }
                else
                {
                    // If not provided, allocate the buffer using SysAllocStringByteLen so
                    // that odd-sized strings will be handled as well.
                    ptrToFirstChar = (byte*)Interop.OleAut32.SysAllocStringByteLen(null, lengthInBytes);

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
                        (byte*)ch,
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

        internal static unsafe string? ConvertToManaged(IntPtr bstr)
        {
            if (IntPtr.Zero == bstr)
            {
                return null;
            }
            else
            {
                uint length = Marshal.SysStringByteLen(bstr);

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
                    // corrupts string.Empty.
                    ret = string.FastAllocateString(0);
                }
                else
                {
                    ret = new string((char*)bstr, 0, (int)(length / 2));
                }

                if ((length & 1) == 1)
                {
                    // odd-sized strings need to have the trailing byte saved in their sync block
                    ret.SetTrailByte(((byte*)bstr)[length - 1]);
                }

                return ret;
            }
        }

        internal static void ClearNative(IntPtr pNative)
        {
            if (IntPtr.Zero != pNative)
            {
                Interop.OleAut32.SysFreeString(pNative);
            }
        }
    }  // class BSTRMarshaler

    internal static class VBByValStrMarshaler
    {
        internal static unsafe IntPtr ConvertToNative(string strManaged, bool fBestFit, bool fThrowOnUnmappableChar, ref int cch)
        {
            if (null == strManaged)
            {
                return IntPtr.Zero;
            }

            byte* pNative;

            cch = strManaged.Length;

            // length field at negative offset + (# of characters incl. the terminator) * max ANSI char size
            int nbytes = checked(sizeof(uint) + ((cch + 1) * Marshal.SystemMaxDBCSCharSize));

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

                Debug.Assert(nbytesused < nbytes, "Insufficient buffer allocated in VBByValStrMarshaler.ConvertToNative");
                Buffer.Memcpy(pNative, 0, bytes, 0, nbytesused);

                pNative[nbytesused] = 0;
                *pLength = nbytesused;
            }

            return new IntPtr(pNative);
        }

        internal static unsafe string? ConvertToManaged(IntPtr pNative, int cch)
        {
            if (IntPtr.Zero == pNative)
            {
                return null;
            }

            return new string((sbyte*)pNative, 0, cch);
        }

        internal static void ClearNative(IntPtr pNative)
        {
            if (IntPtr.Zero != pNative)
            {
                Interop.Ole32.CoTaskMemFree((IntPtr)(((long)pNative) - sizeof(uint)));
            }
        }
    }  // class VBByValStrMarshaler

    internal static class AnsiBSTRMarshaler
    {
        internal static IntPtr ConvertToNative(int flags, string strManaged)
        {
            if (null == strManaged)
            {
                return IntPtr.Zero;
            }

            byte[]? bytes = null;
            int nb = 0;

            if (strManaged.Length > 0)
            {
                bytes = AnsiCharMarshaler.DoAnsiConversion(strManaged, 0 != (flags & 0xFF), 0 != (flags >> 8), out nb);
            }

            return Interop.OleAut32.SysAllocStringByteLen(bytes, (uint)nb);
        }

        internal static unsafe string? ConvertToManaged(IntPtr bstr)
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
                return new string((sbyte*)bstr);
            }
        }

        internal static void ClearNative(IntPtr pNative)
        {
            if (IntPtr.Zero != pNative)
            {
                Interop.OleAut32.SysFreeString(pNative);
            }
        }
    }  // class AnsiBSTRMarshaler

    internal static class WSTRBufferMarshaler
    {
        internal static IntPtr ConvertToNative(string strManaged)
        {
            Debug.Fail("NYI");
            return IntPtr.Zero;
        }

        internal static string? ConvertToManaged(IntPtr bstr)
        {
            Debug.Fail("NYI");
            return null;
        }

        internal static void ClearNative(IntPtr pNative)
        {
            Debug.Fail("NYI");
        }
    }  // class WSTRBufferMarshaler


#if FEATURE_COMINTEROP


    [StructLayout(LayoutKind.Sequential)]
    internal struct DateTimeNative
    {
        public long UniversalTime;
    };

    internal static class DateTimeOffsetMarshaler
    {
        // Numer of ticks counted between 0001-01-01, 00:00:00 and 1601-01-01, 00:00:00.
        // You can get this through:  (new DateTimeOffset(1601, 1, 1, 0, 0, 1, TimeSpan.Zero)).Ticks;
        private const long ManagedUtcTicksAtNativeZero = 504911232000000000;

        internal static void ConvertToNative(ref DateTimeOffset managedDTO, out DateTimeNative dateTime)
        {
            long managedUtcTicks = managedDTO.UtcTicks;
            dateTime.UniversalTime = managedUtcTicks - ManagedUtcTicksAtNativeZero;
        }

        internal static void ConvertToManaged(out DateTimeOffset managedLocalDTO, ref DateTimeNative nativeTicks)
        {
            long managedUtcTicks = ManagedUtcTicksAtNativeZero + nativeTicks.UniversalTime;
            DateTimeOffset managedUtcDTO = new DateTimeOffset(managedUtcTicks, TimeSpan.Zero);

            // Some Utc times cannot be represented in local time in certain timezones. E.g. 0001-01-01 12:00:00 AM cannot 
            // be represented in any timezones with a negative offset from Utc. We throw an ArgumentException in that case.
            managedLocalDTO = managedUtcDTO.ToLocalTime(true);
        }
    }  // class DateTimeOffsetMarshaler

#endif  // FEATURE_COMINTEROP


#if FEATURE_COMINTEROP
    internal static class HStringMarshaler
    {
        // Slow-path, which requires making a copy of the managed string into the resulting HSTRING
        internal static unsafe IntPtr ConvertToNative(string managed)
        {
            if (!Environment.IsWinRTSupported)
                throw new PlatformNotSupportedException(SR.PlatformNotSupported_WinRT);
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
        internal static unsafe IntPtr ConvertToNativeReference(string managed,
                                                               [Out] HSTRING_HEADER* hstringHeader)
        {
            if (!Environment.IsWinRTSupported)
                throw new PlatformNotSupportedException(SR.PlatformNotSupported_WinRT);
            if (managed == null)
                throw new ArgumentNullException();  // We don't have enough information to get the argument name 

            // The string must also be pinned by the caller to ConvertToNativeReference, which also owns
            // the HSTRING_HEADER.
            fixed (char* pManaged = managed)
            {
                IntPtr hstring;
                int hrCreate = System.Runtime.InteropServices.WindowsRuntime.UnsafeNativeMethods.WindowsCreateStringReference(pManaged, managed.Length, hstringHeader, &hstring);
                Marshal.ThrowExceptionForHR(hrCreate, new IntPtr(-1));
                return hstring;
            }
        }

        internal static string ConvertToManaged(IntPtr hstring)
        {
            if (!Environment.IsWinRTSupported)
            {
                throw new PlatformNotSupportedException(SR.PlatformNotSupported_WinRT);
            }

            return WindowsRuntimeMarshal.HStringToString(hstring);
        }

        internal static void ClearNative(IntPtr hstring)
        {
            Debug.Assert(Environment.IsWinRTSupported);

            if (hstring != IntPtr.Zero)
            {
                System.Runtime.InteropServices.WindowsRuntime.UnsafeNativeMethods.WindowsDeleteString(hstring);
            }
        }
    }  // class HStringMarshaler

    internal static class ObjectMarshaler
    {
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void ConvertToNative(object objSrc, IntPtr pDstVariant);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern object ConvertToManaged(IntPtr pSrcVariant);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void ClearNative(IntPtr pVariant);
    }  // class ObjectMarshaler

#endif // FEATURE_COMINTEROP

    internal static class ValueClassMarshaler
    {
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void ConvertToNative(IntPtr dst, IntPtr src, IntPtr pMT, ref CleanupWorkListElement pCleanupWorkList);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void ConvertToManaged(IntPtr dst, IntPtr src, IntPtr pMT);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void ClearNative(IntPtr dst, IntPtr pMT);
    }  // class ValueClassMarshaler

    internal static class DateMarshaler
    {
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern double ConvertToNative(DateTime managedDate);

        // The return type is really DateTime but we use long to avoid the pain associated with returning structures.
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern long ConvertToManaged(double nativeDate);
    }  // class DateMarshaler

#if FEATURE_COMINTEROP
    internal static class InterfaceMarshaler
    {
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern IntPtr ConvertToNative(object objSrc, IntPtr itfMT, IntPtr classMT, int flags);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern object ConvertToManaged(IntPtr pUnk, IntPtr itfMT, IntPtr classMT, int flags);

        [DllImport(JitHelpers.QCall)]
        internal static extern void ClearNative(IntPtr pUnk);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern object ConvertToManagedWithoutUnboxing(IntPtr pNative);
    }  // class InterfaceMarshaler
#endif // FEATURE_COMINTEROP

#if FEATURE_COMINTEROP
    internal static class UriMarshaler
    {
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern string GetRawUriFromNative(IntPtr pUri);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern unsafe IntPtr CreateNativeUriInstanceHelper(char* rawUri, int strLen);

        internal static unsafe IntPtr CreateNativeUriInstance(string rawUri)
        {
            fixed (char* pManaged = rawUri)
            {
                return CreateNativeUriInstanceHelper(pManaged, rawUri.Length);
            }
        }
    }  // class InterfaceMarshaler

#endif // FEATURE_COMINTEROP

    internal static class MngdNativeArrayMarshaler
    {
        // Needs to match exactly with MngdNativeArrayMarshaler in ilmarshalers.h
        internal struct MarshalerState
        {
            IntPtr m_pElementMT;
            IntPtr m_Array;
            int m_NativeDataValid;
            int m_BestFitMap;
            int m_ThrowOnUnmappableChar;
            short m_vt;
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void CreateMarshaler(IntPtr pMarshalState, IntPtr pMT, int dwFlags);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void ConvertSpaceToNative(IntPtr pMarshalState, ref object pManagedHome, IntPtr pNativeHome);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void ConvertContentsToNative(IntPtr pMarshalState, ref object pManagedHome, IntPtr pNativeHome);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void ConvertSpaceToManaged(IntPtr pMarshalState, ref object pManagedHome, IntPtr pNativeHome,
                                                          int cElements);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void ConvertContentsToManaged(IntPtr pMarshalState, ref object pManagedHome, IntPtr pNativeHome);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void ClearNative(IntPtr pMarshalState, IntPtr pNativeHome, int cElements);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void ClearNativeContents(IntPtr pMarshalState, IntPtr pNativeHome, int cElements);
    }  // class MngdNativeArrayMarshaler

#if FEATURE_COMINTEROP
    internal static class MngdSafeArrayMarshaler
    {
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void CreateMarshaler(IntPtr pMarshalState, IntPtr pMT, int iRank, int dwFlags);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void ConvertSpaceToNative(IntPtr pMarshalState, ref object pManagedHome, IntPtr pNativeHome);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void ConvertContentsToNative(IntPtr pMarshalState, ref object pManagedHome, IntPtr pNativeHome, object pOriginalManaged);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void ConvertSpaceToManaged(IntPtr pMarshalState, ref object pManagedHome, IntPtr pNativeHome);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void ConvertContentsToManaged(IntPtr pMarshalState, ref object pManagedHome, IntPtr pNativeHome);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void ClearNative(IntPtr pMarshalState, ref object pManagedHome, IntPtr pNativeHome);
    }  // class MngdSafeArrayMarshaler

    internal static class MngdHiddenLengthArrayMarshaler
    {
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void CreateMarshaler(IntPtr pMarshalState, IntPtr pMT, IntPtr cbElementSize, ushort vt);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void ConvertSpaceToNative(IntPtr pMarshalState, ref object pManagedHome, IntPtr pNativeHome);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void ConvertContentsToNative(IntPtr pMarshalState, ref object pManagedHome, IntPtr pNativeHome);

        internal static unsafe void ConvertContentsToNative_DateTime(ref DateTimeOffset[]? managedArray, IntPtr pNativeHome)
        {
            if (managedArray != null)
            {
                DateTimeNative* nativeBuffer = *(DateTimeNative**)pNativeHome;
                for (int i = 0; i < managedArray.Length; i++)
                {
                    DateTimeOffsetMarshaler.ConvertToNative(ref managedArray[i], out nativeBuffer[i]);
                }
            }
        }

        internal static unsafe void ConvertContentsToNative_Type(ref System.Type[]? managedArray, IntPtr pNativeHome)
        {
            if (managedArray != null)
            {
                TypeNameNative* nativeBuffer = *(TypeNameNative**)pNativeHome;
                for (int i = 0; i < managedArray.Length; i++)
                {
                    SystemTypeMarshaler.ConvertToNative(managedArray[i], &nativeBuffer[i]);
                }
            }
        }

        internal static unsafe void ConvertContentsToNative_Exception(ref Exception[]? managedArray, IntPtr pNativeHome)
        {
            if (managedArray != null)
            {
                int* nativeBuffer = *(int**)pNativeHome;
                for (int i = 0; i < managedArray.Length; i++)
                {
                    nativeBuffer[i] = HResultExceptionMarshaler.ConvertToNative(managedArray[i]);
                }
            }
        }

        internal static unsafe void ConvertContentsToNative_Nullable<T>(ref Nullable<T>[]? managedArray, IntPtr pNativeHome)
            where T : struct
        {
            if (managedArray != null)
            {
                IntPtr* nativeBuffer = *(IntPtr**)pNativeHome;
                for (int i = 0; i < managedArray.Length; i++)
                {
                    nativeBuffer[i] = NullableMarshaler.ConvertToNative<T>(ref managedArray[i]);
                }
            }
        }

        internal static unsafe void ConvertContentsToNative_KeyValuePair<K, V>(ref KeyValuePair<K, V>[]? managedArray, IntPtr pNativeHome) 
        {
            if (managedArray != null)
            {
                IntPtr* nativeBuffer = *(IntPtr**)pNativeHome;
                for (int i = 0; i < managedArray.Length; i++)
                {
                    nativeBuffer[i] = KeyValuePairMarshaler.ConvertToNative<K, V>(ref managedArray[i]);
                }
            }
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void ConvertSpaceToManaged(IntPtr pMarshalState, ref object pManagedHome, IntPtr pNativeHome, int elementCount);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void ConvertContentsToManaged(IntPtr pMarshalState, ref object pManagedHome, IntPtr pNativeHome);

        internal static unsafe void ConvertContentsToManaged_DateTime(ref DateTimeOffset[]? managedArray, IntPtr pNativeHome)
        {
            if (managedArray != null)
            {
                DateTimeNative* nativeBuffer = *(DateTimeNative**)pNativeHome;
                for (int i = 0; i < managedArray.Length; i++)
                {
                    DateTimeOffsetMarshaler.ConvertToManaged(out managedArray[i], ref nativeBuffer[i]);
                }
            }
        }

        internal static unsafe void ConvertContentsToManaged_Type(ref System.Type?[]? managedArray, IntPtr pNativeHome)
        {
            if (managedArray != null)
            {
                TypeNameNative* nativeBuffer = *(TypeNameNative**)pNativeHome;
                for (int i = 0; i < managedArray.Length; i++)
                {
                    SystemTypeMarshaler.ConvertToManaged(&nativeBuffer[i], ref managedArray[i]);
                }
            }
        }

        internal static unsafe void ConvertContentsToManaged_Exception(ref Exception?[]? managedArray, IntPtr pNativeHome)
        {
            if (managedArray != null)
            {
                int* nativeBuffer = *(int**)pNativeHome;
                for (int i = 0; i < managedArray.Length; i++)
                {
                    managedArray[i] = HResultExceptionMarshaler.ConvertToManaged(nativeBuffer[i]);
                }
            }
        }

        internal static unsafe void ConvertContentsToManaged_Nullable<T>(ref Nullable<T>[]? managedArray, IntPtr pNativeHome)
            where T : struct
        {
            if (managedArray != null)
            {
                IntPtr* nativeBuffer = *(IntPtr**)pNativeHome;
                for (int i = 0; i < managedArray.Length; i++)
                {
                    managedArray[i] = NullableMarshaler.ConvertToManaged<T>(nativeBuffer[i]);
                }
            }
        }

        internal static unsafe void ConvertContentsToManaged_KeyValuePair<K, V>(ref KeyValuePair<K, V>[]? managedArray, IntPtr pNativeHome)
        {
            if (managedArray != null)
            {
                IntPtr* nativeBuffer = *(IntPtr**)pNativeHome;
                for (int i = 0; i < managedArray.Length; i++)
                {
                    managedArray[i] = KeyValuePairMarshaler.ConvertToManaged<K, V>(nativeBuffer[i]);
                }
            }
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void ClearNativeContents(IntPtr pMarshalState, IntPtr pNativeHome, int cElements);

        internal static unsafe void ClearNativeContents_Type(IntPtr pNativeHome, int cElements)
        {
            Debug.Assert(Environment.IsWinRTSupported);

            TypeNameNative* pNativeTypeArray = *(TypeNameNative**)pNativeHome;
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

    internal static class MngdRefCustomMarshaler
    {
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void CreateMarshaler(IntPtr pMarshalState, IntPtr pCMHelper);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void ConvertContentsToNative(IntPtr pMarshalState, ref object pManagedHome, IntPtr pNativeHome);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void ConvertContentsToManaged(IntPtr pMarshalState, ref object pManagedHome, IntPtr pNativeHome);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void ClearNative(IntPtr pMarshalState, ref object pManagedHome, IntPtr pNativeHome);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void ClearManaged(IntPtr pMarshalState, ref object pManagedHome, IntPtr pNativeHome);
    }  // class MngdRefCustomMarshaler

    internal struct AsAnyMarshaler
    {
        private const ushort VTHACK_ANSICHAR = 253;
        private const ushort VTHACK_WINBOOL = 254;

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
        private Type? layoutType;

        // Cleanup list to be destroyed when clearing the native view (for layouts with SafeHandles).
        private CleanupWorkListElement? cleanupWorkList;

        [Flags]
        internal enum AsAnyFlags
        {
            In = 0x10000000,
            Out = 0x20000000,
            IsAnsi = 0x00FF0000,
            IsThrowOn = 0x0000FF00,
            IsBestFit = 0x000000FF
        }

        private static bool IsIn(int dwFlags) { return ((dwFlags & (int)AsAnyFlags.In) != 0); }
        private static bool IsOut(int dwFlags) { return ((dwFlags & (int)AsAnyFlags.Out) != 0); }
        private static bool IsAnsi(int dwFlags) { return ((dwFlags & (int)AsAnyFlags.IsAnsi) != 0); }
        private static bool IsThrowOn(int dwFlags) { return ((dwFlags & (int)AsAnyFlags.IsThrowOn) != 0); }
        private static bool IsBestFit(int dwFlags) { return ((dwFlags & (int)AsAnyFlags.IsBestFit) != 0); }

        internal AsAnyMarshaler(IntPtr pvArrayMarshaler)
        {
            // we need this in case the value being marshaled turns out to be array
            Debug.Assert(pvArrayMarshaler != IntPtr.Zero, "pvArrayMarshaler must not be null");

            this.pvArrayMarshaler = pvArrayMarshaler;
            backPropAction = BackPropAction.None;
            layoutType = null;
            cleanupWorkList = null;
        }

        #region ConvertToNative helpers

        private unsafe IntPtr ConvertArrayToNative(object pManagedHome, int dwFlags)
        {
            Type elementType = pManagedHome.GetType().GetElementType()!;
            VarEnum vt = VarEnum.VT_EMPTY;

            switch (Type.GetTypeCode(elementType))
            {
                case TypeCode.SByte: vt = VarEnum.VT_I1; break;
                case TypeCode.Byte: vt = VarEnum.VT_UI1; break;
                case TypeCode.Int16: vt = VarEnum.VT_I2; break;
                case TypeCode.UInt16: vt = VarEnum.VT_UI2; break;
                case TypeCode.Int32: vt = VarEnum.VT_I4; break;
                case TypeCode.UInt32: vt = VarEnum.VT_UI4; break;
                case TypeCode.Int64: vt = VarEnum.VT_I8; break;
                case TypeCode.UInt64: vt = VarEnum.VT_UI8; break;
                case TypeCode.Single: vt = VarEnum.VT_R4; break;
                case TypeCode.Double: vt = VarEnum.VT_R8; break;
                case TypeCode.Char: vt = (IsAnsi(dwFlags) ? (VarEnum)VTHACK_ANSICHAR : VarEnum.VT_UI2); break;
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
                    throw new ArgumentException(SR.Arg_NDirectBadObject);
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

                int allocSize = (pManagedHome.Length + 1) * 2;
                pNativeHome = Marshal.AllocCoTaskMem(allocSize);

                string.InternalCopy(pManagedHome, pNativeHome, allocSize);
            }

            return pNativeHome;
        }

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
                int allocSize = checked((pManagedHome.Capacity * Marshal.SystemMaxDBCSCharSize) + 4);
                pNativeHome = Marshal.AllocCoTaskMem(allocSize);

                byte* ptr = (byte*)pNativeHome;
                *(ptr + allocSize - 3) = 0;
                *(ptr + allocSize - 2) = 0;
                *(ptr + allocSize - 1) = 0;

                if (IsIn(dwFlags))
                {
                    int length = Marshal.StringToAnsiString(pManagedHome.ToString(),
                        ptr, allocSize,
                        IsBestFit(dwFlags),
                        IsThrowOn(dwFlags));
                    Debug.Assert(length < allocSize, "Expected a length less than the allocated size");
                }
                if (IsOut(dwFlags))
                {
                    backPropAction = BackPropAction.StringBuilderAnsi;
                }
            }
            else
            {
                // marshal the object as Unicode string (UnmanagedType.LPWStr)
                int allocSize = checked((pManagedHome.Capacity * 2) + 4);
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
                StubHelpers.FmtClassUpdateNativeInternal(pManagedHome, (byte*)pNativeHome, ref cleanupWorkList);
            }
            if (IsOut(dwFlags))
            {
                backPropAction = BackPropAction.Layout;
            }
            layoutType = pManagedHome.GetType();

            return pNativeHome;
        }

        #endregion

        internal IntPtr ConvertToNative(object pManagedHome, int dwFlags)
        {
            if (pManagedHome == null)
                return IntPtr.Zero;

            if (pManagedHome is ArrayWithOffset)
                throw new ArgumentException(SR.Arg_MarshalAsAnyRestriction);

            IntPtr pNativeHome;

            if (pManagedHome.GetType().IsArray)
            {
                // array (LPArray)
                pNativeHome = ConvertArrayToNative(pManagedHome, dwFlags);
            }
            else
            {
                string? strValue;
                StringBuilder? sbValue;

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
                    throw new ArgumentException(SR.Arg_NDirectBadObject);
                }
            }

            return pNativeHome;
        }

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
                        StubHelpers.FmtClassUpdateCLRInternal(pManagedHome, (byte*)pNativeHome);
                        break;
                    }

                case BackPropAction.StringBuilderAnsi:
                    {
                        int length;
                        if (pNativeHome == IntPtr.Zero)
                        {
                            length = 0;
                        }
                        else
                        {
                            length = string.strlen((byte*)pNativeHome);
                        }

                        ((StringBuilder)pManagedHome).ReplaceBufferAnsiInternal((sbyte*)pNativeHome, length);
                        break;
                    }

                case BackPropAction.StringBuilderUnicode:
                    {
                        int length;
                        if (pNativeHome == IntPtr.Zero)
                        {
                            length = 0;
                        }
                        else
                        {
                            length = string.wcslen((char*)pNativeHome);
                        }

                        ((StringBuilder)pManagedHome).ReplaceBufferInternal((char*)pNativeHome, length);
                        break;
                    }

                    // nothing to do for BackPropAction.None
            }
        }

        internal void ClearNative(IntPtr pNativeHome)
        {
            if (pNativeHome != IntPtr.Zero)
            {
                if (layoutType != null)
                {
                    // this must happen regardless of BackPropAction
                    Marshal.DestroyStructure(pNativeHome, layoutType);
                }
                Interop.Ole32.CoTaskMemFree(pNativeHome);
            }
            StubHelpers.DestroyCleanupList(ref cleanupWorkList);
        }
    }  // struct AsAnyMarshaler

#if FEATURE_COMINTEROP
    internal static class NullableMarshaler
    {
        internal static IntPtr ConvertToNative<T>(ref Nullable<T> pManaged) where T : struct
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

        internal static void ConvertToManagedRetVoid<T>(IntPtr pNative, ref Nullable<T> retObj) where T : struct
        {
            retObj = ConvertToManaged<T>(pNative);
        }


        internal static Nullable<T> ConvertToManaged<T>(IntPtr pNative) where T : struct
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
        internal IntPtr typeName;           // HSTRING
        internal TypeKind typeKind;           // TypeKind enum
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

    internal static class SystemTypeMarshaler
    {
        internal static unsafe void ConvertToNative(System.Type managedType, TypeNameNative* pNativeType)
        {
            if (!Environment.IsWinRTSupported)
            {
                throw new PlatformNotSupportedException(SR.PlatformNotSupported_WinRT);
            }

            string typeName;
            if (managedType != null)
            {
                if (managedType.GetType() != typeof(System.RuntimeType))
                {   // The type should be exactly System.RuntimeType (and not its child System.ReflectionOnlyType, or other System.Type children)
                    throw new ArgumentException(SR.Format(SR.Argument_WinRTSystemRuntimeType, managedType.GetType()));
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
                    typeName = managedType.AssemblyQualifiedName!;
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

        internal static unsafe void ConvertToManaged(TypeNameNative* pNativeType, ref System.Type? managedType)
        {
            if (!Environment.IsWinRTSupported)
            {
                throw new PlatformNotSupportedException(SR.PlatformNotSupported_WinRT);
            }

            string typeName = WindowsRuntimeMarshal.HStringToString(pNativeType->typeName);
            if (string.IsNullOrEmpty(typeName))
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
                    throw new ArgumentException(SR.Argument_Unexpected_TypeSource);
            }
        }

        internal static unsafe void ClearNative(TypeNameNative* pNativeType)
        {
            Debug.Assert(Environment.IsWinRTSupported);

            if (pNativeType->typeName != IntPtr.Zero)
            {
                System.Runtime.InteropServices.WindowsRuntime.UnsafeNativeMethods.WindowsDeleteString(pNativeType->typeName);
            }
        }
    }  // class SystemTypeMarshaler

    // For converting WinRT's Windows.Foundation.HResult into System.Exception and vice versa.
    internal static class HResultExceptionMarshaler
    {
        internal static int ConvertToNative(Exception ex)
        {
            if (!Environment.IsWinRTSupported)
            {
                throw new PlatformNotSupportedException(SR.PlatformNotSupported_WinRT);
            }

            if (ex == null)
                return 0;  // S_OK;

            return ex.HResult;
        }

        internal static Exception? ConvertToManaged(int hr)
        {
            if (!Environment.IsWinRTSupported)
            {
                throw new PlatformNotSupportedException(SR.PlatformNotSupported_WinRT);
            }

            Exception? e = null;
            if (hr < 0)
            {
                e = StubHelpers.InternalGetCOMHRExceptionObject(hr, IntPtr.Zero, null, /* fForWinRT */ true);
            }

            // S_OK should be marshaled as null.  WinRT API's should not return S_FALSE by convention.
            // We've chosen to treat S_FALSE as success and return null.
            Debug.Assert(e != null || hr == 0 || hr == 1, "Unexpected HRESULT - it is a success HRESULT (without the high bit set) other than S_OK & S_FALSE.");
            return e;
        }
    }  // class HResultExceptionMarshaler

    internal static class KeyValuePairMarshaler
    {
        internal static IntPtr ConvertToNative<K, V>([In] ref KeyValuePair<K, V> pair)
        {
            IKeyValuePair<K, V> impl = new CLRIKeyValuePairImpl<K, V>(ref pair);
            return Marshal.GetComInterfaceForObject(impl, typeof(IKeyValuePair<K, V>));
        }

        internal static KeyValuePair<K, V> ConvertToManaged<K, V>(IntPtr pInsp)
        {
            object obj = InterfaceMarshaler.ConvertToManagedWithoutUnboxing(pInsp);

            IKeyValuePair<K, V> pair = (IKeyValuePair<K, V>)obj;
            return new KeyValuePair<K, V>(pair.Key, pair.Value);
        }

        // Called from COMInterfaceMarshaler
        internal static object ConvertToManagedBox<K, V>(IntPtr pInsp)
        {
            return (object)ConvertToManaged<K, V>(pInsp);
        }
    }  // class KeyValuePairMarshaler

#endif // FEATURE_COMINTEROP

    [StructLayout(LayoutKind.Sequential)]
    internal struct NativeVariant
    {
        private ushort vt;
        private ushort wReserved1;
        private ushort wReserved2;
        private ushort wReserved3;

        // The union portion of the structure contains at least one 64-bit type that on some 32-bit platforms
        // (notably  ARM) requires 64-bit alignment. So on 32-bit platforms we'll actually size the variant
        // portion of the struct with an Int64 so the type loader notices this requirement (a no-op on x86,
        // but on ARM it will allow us to correctly determine the layout of native argument lists containing
        // VARIANTs). Note that the field names here don't matter: none of the code refers to these fields,
        // the structure just exists to provide size information to the IL marshaler.
#if BIT64
        private IntPtr data1;
        private IntPtr data2;
#else
        long data1;
#endif
    }  // struct NativeVariant

    internal abstract class CleanupWorkListElement
    {
        private CleanupWorkListElement? m_Next;
        protected abstract void DestroyCore();

        public void Destroy()
        {
            DestroyCore();
            CleanupWorkListElement? next = m_Next;
            while (next != null)
            {
                next.DestroyCore();
                next = next.m_Next;
            }
        }
        
        public static void AddToCleanupList(ref CleanupWorkListElement list, CleanupWorkListElement newElement)
        {
            if (list == null)
            {
                list = newElement;
            }
            else
            {
                newElement.m_Next = list;
                list = newElement;
            }
        }
    }

    // Keeps a Delegate instance alive across the full Managed->Native call.
    // This ensures that users don't have to call GC.KeepAlive after passing a struct or class
    // that has a delegate field to native code.
    internal sealed class DelegateCleanupWorkListElement : CleanupWorkListElement
    {
        public DelegateCleanupWorkListElement(Delegate del)
        {
            m_del = del;
        }

        private Delegate m_del;

        protected override void DestroyCore()
        {
            GC.KeepAlive(m_del);
        }
    }

    // Aggregates SafeHandle and the "owned" bit which indicates whether the SafeHandle
    // has been successfully AddRef'ed. This allows us to do realiable cleanup (Release)
    // if and only if it is needed.
    internal sealed class SafeHandleCleanupWorkListElement : CleanupWorkListElement
    {
        public SafeHandleCleanupWorkListElement(SafeHandle handle)
        {
            m_handle = handle;
        }

        private SafeHandle m_handle;

        // This field is passed by-ref to SafeHandle.DangerousAddRef.
        // DestroyCore ignores this element if m_owned is not set to true.
        private bool m_owned;

        protected override void DestroyCore()
        {
            if (m_owned)
                StubHelpers.SafeHandleRelease(m_handle);
        }

        public IntPtr AddRef()
        {
            // element.m_owned will be true iff the AddRef succeeded
            return StubHelpers.SafeHandleAddRef(m_handle, ref m_owned);
        }
    }  // class CleanupWorkListElement

    internal static class StubHelpers
    {
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern bool IsQCall(IntPtr pMD);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void InitDeclaringType(IntPtr pMD);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern IntPtr GetNDirectTarget(IntPtr pMD);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern IntPtr GetDelegateTarget(Delegate pThis, ref IntPtr pStubArg);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void ClearLastError();

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void SetLastError();

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void ThrowInteropParamException(int resID, int paramIdx);

        internal static IntPtr AddToCleanupList(ref CleanupWorkListElement pCleanupWorkList, SafeHandle handle)
        {
            SafeHandleCleanupWorkListElement element = new SafeHandleCleanupWorkListElement(handle);
            CleanupWorkListElement.AddToCleanupList(ref pCleanupWorkList, element);
            return element.AddRef();
        }

        internal static void AddToCleanupList(ref CleanupWorkListElement pCleanupWorkList, Delegate del)
        {
            DelegateCleanupWorkListElement element = new DelegateCleanupWorkListElement(del);
            CleanupWorkListElement.AddToCleanupList(ref pCleanupWorkList, element);
        }

        internal static void DestroyCleanupList(ref CleanupWorkListElement? pCleanupWorkList)
        {
            if (pCleanupWorkList != null)
            {
                pCleanupWorkList.Destroy();
                pCleanupWorkList = null;
            }
        }

        internal static Exception GetHRExceptionObject(int hr)
        {
            Exception ex = InternalGetHRExceptionObject(hr);
            ex.InternalPreserveStackTrace();
            return ex;
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern Exception InternalGetHRExceptionObject(int hr);

#if FEATURE_COMINTEROP
        internal static Exception GetCOMHRExceptionObject(int hr, IntPtr pCPCMD, object pThis)
        {
            Exception ex = InternalGetCOMHRExceptionObject(hr, pCPCMD, pThis, false);
            ex.InternalPreserveStackTrace();
            return ex;
        }

        internal static Exception GetCOMHRExceptionObject_WinRT(int hr, IntPtr pCPCMD, object pThis)
        {
            Exception ex = InternalGetCOMHRExceptionObject(hr, pCPCMD, pThis, true);
            ex.InternalPreserveStackTrace();
            return ex;
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern Exception InternalGetCOMHRExceptionObject(int hr, IntPtr pCPCMD, object? pThis, bool fForWinRT);

#endif // FEATURE_COMINTEROP

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern IntPtr CreateCustomMarshalerHelper(IntPtr pMD, int paramToken, IntPtr hndManagedType);

        //-------------------------------------------------------
        // SafeHandle Helpers
        //-------------------------------------------------------

        // AddRefs the SH and returns the underlying unmanaged handle.
        internal static IntPtr SafeHandleAddRef(SafeHandle pHandle, ref bool success)
        {
            if (pHandle == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.pHandle, ExceptionResource.ArgumentNull_SafeHandle);
            }

            pHandle!.DangerousAddRef(ref success); // TODO-NULLABLE: https://github.com/dotnet/csharplang/issues/538
            return pHandle.DangerousGetHandle();
        }

        // Releases the SH (to be called from finally block).
        internal static void SafeHandleRelease(SafeHandle pHandle)
        {
            if (pHandle == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.pHandle, ExceptionResource.ArgumentNull_SafeHandle);
            }

            try
            {
                pHandle!.DangerousRelease(); // TODO-NULLABLE: https://github.com/dotnet/csharplang/issues/538
            }
            catch
            {
            }
        }

#if FEATURE_COMINTEROP
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern IntPtr GetCOMIPFromRCW(object objSrc, IntPtr pCPCMD, out IntPtr ppTarget, out bool pfNeedsRelease);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern IntPtr GetCOMIPFromRCW_WinRT(object objSrc, IntPtr pCPCMD, out IntPtr ppTarget);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern IntPtr GetCOMIPFromRCW_WinRTSharedGeneric(object objSrc, IntPtr pCPCMD, out IntPtr ppTarget);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern IntPtr GetCOMIPFromRCW_WinRTDelegate(object objSrc, IntPtr pCPCMD, out IntPtr ppTarget);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern bool ShouldCallWinRTInterface(object objSrc, IntPtr pCPCMD);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern Delegate GetTargetForAmbiguousVariantCall(object objSrc, IntPtr pMT, out bool fUseString);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern IntPtr GetDelegateInvokeMethod(Delegate pThis);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern object GetWinRTFactoryObject(IntPtr pCPCMD);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern IntPtr GetWinRTFactoryReturnValue(object pThis, IntPtr pCtorEntry);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern IntPtr GetOuterInspectable(object pThis, IntPtr pCtorMD);

#endif // FEATURE_COMINTEROP

        //-------------------------------------------------------
        // Profiler helpers
        //-------------------------------------------------------
#if PROFILING_SUPPORTED
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern IntPtr ProfilerBeginTransitionCallback(IntPtr pSecretParam, IntPtr pThread, object pThis);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void ProfilerEndTransitionCallback(IntPtr pMD, IntPtr pThread);
#endif // PROFILING_SUPPORTED

        //------------------------------------------------------
        // misc
        //------------------------------------------------------
        internal static void CheckStringLength(int length)
        {
            CheckStringLength((uint)length);
        }

        internal static void CheckStringLength(uint length)
        {
            if (length > 0x7ffffff0)
            {
                throw new MarshalDirectiveException(SR.Marshaler_StringTooLong);
            }
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern unsafe void FmtClassUpdateNativeInternal(object obj, byte* pNative, ref CleanupWorkListElement? pCleanupWorkList);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern unsafe void FmtClassUpdateCLRInternal(object obj, byte* pNative);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern unsafe void LayoutDestroyNativeInternal(byte* pNative, IntPtr pMT);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern object AllocateInternal(IntPtr typeHandle);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void MarshalToUnmanagedVaListInternal(IntPtr va_list, uint vaListSize, IntPtr pArgIterator);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void MarshalToManagedVaListInternal(IntPtr va_list, IntPtr pArgIterator);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern uint CalcVaListSize(IntPtr va_list);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void ValidateObject(object obj, IntPtr pMD, object pThis);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void LogPinnedArgument(IntPtr localDesc, IntPtr nativeArg);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void ValidateByref(IntPtr byref, IntPtr pMD, object pThis); // the byref is pinned so we can safely "cast" it to IntPtr

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern IntPtr GetStubContext();

#if BIT64
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern IntPtr GetStubContextAddr();
#endif // BIT64

#if FEATURE_ARRAYSTUB_AS_IL
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void ArrayTypeCheck(object o, Object[] arr);
#endif

#if FEATURE_MULTICASTSTUB_AS_IL
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void MulticastDebuggerTraceHelper(object o, Int32 count);
#endif
    }  // class StubHelpers
}
