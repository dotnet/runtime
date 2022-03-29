// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using Internal.Runtime.CompilerServices;

namespace System.StubHelpers
{
    internal static class AnsiCharMarshaler
    {
        // The length of the returned array is an approximation based on the length of the input string and the system
        // character set. It is only guaranteed to be larger or equal to cbLength, don't depend on the exact value.
        internal static unsafe byte[] DoAnsiConversion(string str, bool fBestFit, bool fThrowOnUnmappableChar, out int cbLength)
        {
            byte[] buffer = new byte[checked((str.Length + 1) * Marshal.SystemMaxDBCSCharSize)];
            fixed (byte* bufferPtr = &buffer[0])
            {
                cbLength = Marshal.StringToAnsiString(str, bufferPtr, buffer.Length, fBestFit, fThrowOnUnmappableChar);
            }
            return buffer;
        }

        internal static unsafe byte ConvertToNative(char managedChar, bool fBestFit, bool fThrowOnUnmappableChar)
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

                bool didAlloc = false;

                // Use the pre-allocated buffer (allocated by localloc IL instruction) if not NULL,
                // otherwise fallback to AllocCoTaskMem
                if (pbNativeBuffer == null)
                {
                    pbNativeBuffer = (byte*)Marshal.AllocCoTaskMem(nb);
                    didAlloc = true;
                }

                try
                {
                    nb = Marshal.StringToAnsiString(strManaged, pbNativeBuffer, nb,
                        bestFit: 0 != (flags & 0xFF), throwOnUnmappableChar: 0 != (flags >> 8));
                }
                catch (Exception) when (didAlloc)
                {
                    Marshal.FreeCoTaskMem((IntPtr)pbNativeBuffer);
                    throw;
                }
            }
            else
            {
                if (strManaged.Length == 0)
                {
                    nb = 0;
                    pbNativeBuffer = (byte*)Marshal.AllocCoTaskMem(2);
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

                    Buffer.Memmove(ref *pbNativeBuffer, ref MemoryMarshal.GetArrayDataReference(bytes), (nuint)nb);
                }
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
            Marshal.FreeCoTaskMem(pNative);
        }

        internal static unsafe void ConvertFixedToNative(int flags, string strManaged, IntPtr pNativeBuffer, int length)
        {
            if (strManaged == null)
            {
                if (length > 0)
                    *(byte*)pNativeBuffer = 0;
                return;
            }

            int numChars = strManaged.Length;
            if (numChars >= length)
            {
                numChars = length - 1;
            }

            byte* buffer = (byte*)pNativeBuffer;

            // Flags defined in ILFixedCSTRMarshaler::EmitConvertContentsCLRToNative(ILCodeStream* pslILEmit).
            bool throwOnUnmappableChar = 0 != (flags >> 8);
            bool bestFit = 0 != (flags & 0xFF);
            uint defaultCharUsed = 0;

            int cbWritten;

            fixed (char* pwzChar = strManaged)
            {
#if TARGET_WINDOWS
                cbWritten = Interop.Kernel32.WideCharToMultiByte(
                    Interop.Kernel32.CP_ACP,
                    bestFit ? 0 : Interop.Kernel32.WC_NO_BEST_FIT_CHARS,
                    pwzChar,
                    numChars,
                    buffer,
                    length,
                    IntPtr.Zero,
                    throwOnUnmappableChar ? new IntPtr(&defaultCharUsed) : IntPtr.Zero);
#else
                cbWritten = Encoding.UTF8.GetBytes(pwzChar, numChars, buffer, length);
#endif
            }

            if (defaultCharUsed != 0)
            {
                throw new ArgumentException(SR.Interop_Marshal_Unmappable_Char);
            }

            if (cbWritten == (int)length)
            {
                cbWritten--;
            }

            buffer[cbWritten] = 0;
        }

        internal static unsafe string ConvertFixedToManaged(IntPtr cstr, int length)
        {
            int end = SpanHelpers.IndexOf(ref *(byte*)cstr, 0, length);
            if (end != -1)
            {
                length = end;
            }

            return new string((sbyte*)cstr, 0, length);
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
            Marshal.FreeCoTaskMem(pNative);
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
                bool hasTrailByte = strManaged.TryGetTrailByte(out byte trailByte);

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
                    // If not provided, allocate the buffer using Marshal.AllocBSTRByteLen so
                    // that odd-sized strings will be handled as well.
                    ptrToFirstChar = (byte*)Marshal.AllocBSTRByteLen(lengthInBytes);
                }

                // copy characters from the managed string
                Buffer.Memmove(ref *(char*)ptrToFirstChar, ref strManaged.GetRawStringData(), (nuint)strManaged.Length + 1);

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
            Marshal.FreeBSTR(pNative);
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

            pNative += sizeof(uint);

            if (0 == cch)
            {
                *pNative = 0;
                *pLength = 0;
            }
            else
            {
                byte[] bytes = AnsiCharMarshaler.DoAnsiConversion(strManaged, fBestFit, fThrowOnUnmappableChar, out int nbytesused);

                Debug.Assert(nbytesused >= 0 && nbytesused < nbytes, "Insufficient buffer allocated in VBByValStrMarshaler.ConvertToNative");

                Buffer.Memmove(ref *pNative, ref MemoryMarshal.GetArrayDataReference(bytes), (nuint)nbytesused);

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
                Marshal.FreeCoTaskMem((IntPtr)(((long)pNative) - sizeof(uint)));
            }
        }
    }  // class VBByValStrMarshaler

    internal static class AnsiBSTRMarshaler
    {
        internal static unsafe IntPtr ConvertToNative(int flags, string strManaged)
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

            uint length = (uint)nb;
            IntPtr bstr = Marshal.AllocBSTRByteLen(length);
            if (bytes != null)
            {
                Buffer.Memmove(ref *(byte*)bstr, ref MemoryMarshal.GetArrayDataReference(bytes), length);
            }

            return bstr;
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
            Marshal.FreeBSTR(pNative);
        }
    }  // class AnsiBSTRMarshaler

    internal static class FixedWSTRMarshaler
    {
        internal static unsafe void ConvertToNative(string? strManaged, IntPtr nativeHome, int length)
        {
            ReadOnlySpan<char> managed = strManaged;
            Span<char> native = new Span<char>((char*)nativeHome, length);

            int numChars = Math.Min(managed.Length, length - 1);

            managed.Slice(0, numChars).CopyTo(native);
            native[numChars] = '\0';
        }

        internal static unsafe string ConvertToManaged(IntPtr nativeHome, int length)
        {
            int end = SpanHelpers.IndexOf(ref *(char*)nativeHome, '\0', length);
            if (end != -1)
            {
                length = end;
            }

            return new string((char*)nativeHome, 0, length);
        }
    }  // class WSTRBufferMarshaler
#if FEATURE_COMINTEROP

    internal static class ObjectMarshaler
    {
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern void ConvertToNative(object objSrc, IntPtr pDstVariant);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern object ConvertToManaged(IntPtr pSrcVariant);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern void ClearNative(IntPtr pVariant);
    }  // class ObjectMarshaler

#endif // FEATURE_COMINTEROP

    internal sealed class HandleMarshaler
    {
        internal static unsafe IntPtr ConvertSafeHandleToNative(SafeHandle? handle, ref CleanupWorkListElement? cleanupWorkList)
        {
            if (Unsafe.IsNullRef(ref cleanupWorkList))
            {
                throw new InvalidOperationException(SR.Interop_Marshal_SafeHandle_InvalidOperation);
            }

            if (handle is null)
            {
                throw new ArgumentNullException(nameof(handle));
            }

            return StubHelpers.AddToCleanupList(ref cleanupWorkList, handle);
        }

        internal static unsafe void ThrowSafeHandleFieldChanged()
        {
            throw new NotSupportedException(SR.Interop_Marshal_CannotCreateSafeHandleField);
        }

        internal static unsafe void ThrowCriticalHandleFieldChanged()
        {
            throw new NotSupportedException(SR.Interop_Marshal_CannotCreateCriticalHandleField);
        }
    }

    internal static class DateMarshaler
    {
        internal static double ConvertToNative(DateTime managedDate)
        {
            return managedDate.ToOADate();
        }

        internal static long ConvertToManaged(double nativeDate)
        {
            return DateTime.DoubleDateToTicks(nativeDate);
        }
    }  // class DateMarshaler

#if FEATURE_COMINTEROP
    internal static partial class InterfaceMarshaler
    {
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern IntPtr ConvertToNative(object objSrc, IntPtr itfMT, IntPtr classMT, int flags);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern object ConvertToManaged(ref IntPtr ppUnk, IntPtr itfMT, IntPtr classMT, int flags);

        [GeneratedDllImport(RuntimeHelpers.QCall, EntryPoint = "InterfaceMarshaler__ClearNative")]
        internal static partial void ClearNative(IntPtr pUnk);
    }  // class InterfaceMarshaler
#endif // FEATURE_COMINTEROP

    internal static class MngdNativeArrayMarshaler
    {
        // Needs to match exactly with MngdNativeArrayMarshaler in ilmarshalers.h
        internal struct MarshalerState
        {
#pragma warning disable CA1823 // not used by managed code
            private IntPtr m_pElementMT;
            private IntPtr m_Array;
            private IntPtr m_pManagedNativeArrayMarshaler;
            private int m_NativeDataValid;
            private int m_BestFitMap;
            private int m_ThrowOnUnmappableChar;
            private short m_vt;
#pragma warning restore CA1823
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern void CreateMarshaler(IntPtr pMarshalState, IntPtr pMT, int dwFlags, IntPtr pManagedMarshaler);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern void ConvertSpaceToNative(IntPtr pMarshalState, ref object pManagedHome, IntPtr pNativeHome);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern void ConvertContentsToNative(IntPtr pMarshalState, ref object pManagedHome, IntPtr pNativeHome);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern void ConvertSpaceToManaged(IntPtr pMarshalState, ref object pManagedHome, IntPtr pNativeHome,
                                                          int cElements);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern void ConvertContentsToManaged(IntPtr pMarshalState, ref object pManagedHome, IntPtr pNativeHome);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern void ClearNative(IntPtr pMarshalState, ref object pManagedHome, IntPtr pNativeHome, int cElements);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern void ClearNativeContents(IntPtr pMarshalState, ref object pManagedHome, IntPtr pNativeHome, int cElements);
    }  // class MngdNativeArrayMarshaler

    internal static class MngdFixedArrayMarshaler
    {
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern void CreateMarshaler(IntPtr pMarshalState, IntPtr pMT, int dwFlags, int cElements, IntPtr pManagedMarshaler);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern void ConvertSpaceToNative(IntPtr pMarshalState, ref object pManagedHome, IntPtr pNativeHome);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern void ConvertContentsToNative(IntPtr pMarshalState, ref object pManagedHome, IntPtr pNativeHome);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern void ConvertSpaceToManaged(IntPtr pMarshalState, ref object pManagedHome, IntPtr pNativeHome);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern void ConvertContentsToManaged(IntPtr pMarshalState, ref object pManagedHome, IntPtr pNativeHome);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern void ClearNativeContents(IntPtr pMarshalState, ref object pManagedHome, IntPtr pNativeHome);
    }  // class MngdFixedArrayMarshaler

#if FEATURE_COMINTEROP
    internal static class MngdSafeArrayMarshaler
    {
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern void CreateMarshaler(IntPtr pMarshalState, IntPtr pMT, int iRank, int dwFlags, IntPtr pManagedMarshaler);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern void ConvertSpaceToNative(IntPtr pMarshalState, ref object pManagedHome, IntPtr pNativeHome);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern void ConvertContentsToNative(IntPtr pMarshalState, ref object pManagedHome, IntPtr pNativeHome, object pOriginalManaged);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern void ConvertSpaceToManaged(IntPtr pMarshalState, ref object pManagedHome, IntPtr pNativeHome);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern void ConvertContentsToManaged(IntPtr pMarshalState, ref object pManagedHome, IntPtr pNativeHome);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern void ClearNative(IntPtr pMarshalState, ref object pManagedHome, IntPtr pNativeHome);
    }  // class MngdSafeArrayMarshaler
#endif // FEATURE_COMINTEROP

    internal static class MngdRefCustomMarshaler
    {
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern void CreateMarshaler(IntPtr pMarshalState, IntPtr pCMHelper);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern void ConvertContentsToNative(IntPtr pMarshalState, ref object pManagedHome, IntPtr pNativeHome);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern void ConvertContentsToManaged(IntPtr pMarshalState, ref object pManagedHome, IntPtr pNativeHome);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern void ClearNative(IntPtr pMarshalState, ref object pManagedHome, IntPtr pNativeHome);

        [MethodImpl(MethodImplOptions.InternalCall)]
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

        private static bool IsIn(int dwFlags) => (dwFlags & (int)AsAnyFlags.In) != 0;
        private static bool IsOut(int dwFlags) => (dwFlags & (int)AsAnyFlags.Out) != 0;
        private static bool IsAnsi(int dwFlags) => (dwFlags & (int)AsAnyFlags.IsAnsi) != 0;
        private static bool IsThrowOn(int dwFlags) => (dwFlags & (int)AsAnyFlags.IsThrowOn) != 0;
        private static bool IsBestFit(int dwFlags) => (dwFlags & (int)AsAnyFlags.IsBestFit) != 0;

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
            VarEnum vt;

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
                dwArrayMarshalerFlags,
                IntPtr.Zero);     // not needed as we marshal primitive VTs only

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
                unsafe
                {
                    Buffer.Memmove(ref *(char*)pNativeHome, ref pManagedHome.GetRawStringData(), (nuint)pManagedHome.Length + 1);
                }
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

            // Cache StringBuilder capacity and length to ensure we don't allocate a certain amount of
            // native memory and then walk beyond its end if the StringBuilder concurrently grows erroneously.
            int pManagedHomeCapacity = pManagedHome.Capacity;
            int pManagedHomeLength = pManagedHome.Length;
            if (pManagedHomeLength > pManagedHomeCapacity)
            {
                ThrowHelper.ThrowInvalidOperationException();
            }

            // Note that StringBuilder.Capacity is the number of characters NOT including any terminators.

            if (IsAnsi(dwFlags))
            {
                StubHelpers.CheckStringLength(pManagedHomeCapacity);

                // marshal the object as Ansi string (UnmanagedType.LPStr)
                int allocSize = checked((pManagedHomeCapacity * Marshal.SystemMaxDBCSCharSize) + 4);
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
                int allocSize = checked((pManagedHomeCapacity * 2) + 4);
                pNativeHome = Marshal.AllocCoTaskMem(allocSize);

                byte* ptr = (byte*)pNativeHome;
                *(ptr + allocSize - 1) = 0;
                *(ptr + allocSize - 2) = 0;

                if (IsIn(dwFlags))
                {
                    pManagedHome.InternalCopy(pNativeHome, pManagedHomeLength);

                    // null-terminate the native string
                    int length = pManagedHomeLength * 2;
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
                if (pManagedHome is string strValue)
                {
                    // string (LPStr or LPWStr)
                    pNativeHome = ConvertStringToNative(strValue, dwFlags);
                }
                else if (pManagedHome is StringBuilder sbValue)
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
                Marshal.FreeCoTaskMem(pNativeHome);
            }
            StubHelpers.DestroyCleanupList(ref cleanupWorkList);
        }
    }  // struct AsAnyMarshaler

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
#if TARGET_64BIT
        private IntPtr data1;
        private IntPtr data2;
#else
        private long data1;
#endif
    }  // struct NativeVariant

    // This NativeDecimal type is very similar to the System.Decimal type, except it requires an 8-byte alignment
    // like the native DECIMAL type instead of the 4-byte requirement of the System.Decimal type.
    [StructLayout(LayoutKind.Sequential)]
    internal struct NativeDecimal
    {
        private ushort reserved;
        private ushort signScale;
        private uint hi32;
        private ulong lo64;
    }

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

        public static void AddToCleanupList(ref CleanupWorkListElement? list, CleanupWorkListElement newElement)
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

    // Keeps an object instance alive across the full Managed->Native call.
    // This ensures that users don't have to call GC.KeepAlive after passing a struct or class
    // that has a delegate field to native code.
    internal sealed class KeepAliveCleanupWorkListElement : CleanupWorkListElement
    {
        public KeepAliveCleanupWorkListElement(object obj)
        {
            m_obj = obj;
        }

        private object m_obj;

        protected override void DestroyCore()
        {
            GC.KeepAlive(m_obj);
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
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern IntPtr GetNDirectTarget(IntPtr pMD);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern IntPtr GetDelegateTarget(Delegate pThis);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern void ClearLastError();

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern void SetLastError();

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern void ThrowInteropParamException(int resID, int paramIdx);

        internal static IntPtr AddToCleanupList(ref CleanupWorkListElement? pCleanupWorkList, SafeHandle handle)
        {
            SafeHandleCleanupWorkListElement element = new SafeHandleCleanupWorkListElement(handle);
            CleanupWorkListElement.AddToCleanupList(ref pCleanupWorkList, element);
            return element.AddRef();
        }

        internal static void KeepAliveViaCleanupList(ref CleanupWorkListElement? pCleanupWorkList, object obj)
        {
            KeepAliveCleanupWorkListElement element = new KeepAliveCleanupWorkListElement(obj);
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

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern Exception InternalGetHRExceptionObject(int hr);

#if FEATURE_COMINTEROP
        internal static Exception GetCOMHRExceptionObject(int hr, IntPtr pCPCMD, object pThis)
        {
            Exception ex = InternalGetCOMHRExceptionObject(hr, pCPCMD, pThis);
            ex.InternalPreserveStackTrace();
            return ex;
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern Exception InternalGetCOMHRExceptionObject(int hr, IntPtr pCPCMD, object? pThis);

#endif // FEATURE_COMINTEROP

        [ThreadStatic]
        private static Exception? s_pendingExceptionObject;

        internal static Exception? GetPendingExceptionObject()
        {
            Exception? ex = s_pendingExceptionObject;
            if (ex != null)
                ex.InternalPreserveStackTrace();

            s_pendingExceptionObject = null;
            return ex;
        }

        internal static void SetPendingExceptionObject(Exception? exception)
        {
            s_pendingExceptionObject = exception;
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
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

            pHandle.DangerousAddRef(ref success);
            return pHandle.DangerousGetHandle();
        }

        // Releases the SH (to be called from finally block).
        internal static void SafeHandleRelease(SafeHandle pHandle)
        {
            if (pHandle == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.pHandle, ExceptionResource.ArgumentNull_SafeHandle);
            }

            pHandle.DangerousRelease();
        }

#if FEATURE_COMINTEROP
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern IntPtr GetCOMIPFromRCW(object objSrc, IntPtr pCPCMD, out IntPtr ppTarget, out bool pfNeedsRelease);
#endif // FEATURE_COMINTEROP

        //-------------------------------------------------------
        // Profiler helpers
        //-------------------------------------------------------
#if PROFILING_SUPPORTED
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern IntPtr ProfilerBeginTransitionCallback(IntPtr pSecretParam, IntPtr pThread, object pThis);

        [MethodImpl(MethodImplOptions.InternalCall)]
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

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern unsafe void FmtClassUpdateNativeInternal(object obj, byte* pNative, ref CleanupWorkListElement? pCleanupWorkList);
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern unsafe void FmtClassUpdateCLRInternal(object obj, byte* pNative);
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern unsafe void LayoutDestroyNativeInternal(object obj, byte* pNative);
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern object AllocateInternal(IntPtr typeHandle);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern void MarshalToUnmanagedVaListInternal(IntPtr va_list, uint vaListSize, IntPtr pArgIterator);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern void MarshalToManagedVaListInternal(IntPtr va_list, IntPtr pArgIterator);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern uint CalcVaListSize(IntPtr va_list);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern void ValidateObject(object obj, IntPtr pMD, object pThis);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern void LogPinnedArgument(IntPtr localDesc, IntPtr nativeArg);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern void ValidateByref(IntPtr byref, IntPtr pMD, object pThis); // the byref is pinned so we can safely "cast" it to IntPtr

        [Intrinsic]
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern IntPtr GetStubContext();

#if FEATURE_ARRAYSTUB_AS_IL
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern void ArrayTypeCheck(object o, object[] arr);
#endif

#if FEATURE_MULTICASTSTUB_AS_IL
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern void MulticastDebuggerTraceHelper(object o, int count);
#endif

        [Intrinsic]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern IntPtr NextCallReturnAddress();
    }  // class StubHelpers
}
