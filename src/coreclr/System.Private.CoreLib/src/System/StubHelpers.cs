// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text;

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
            var bytes = new ReadOnlySpan<byte>(in nativeChar);
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
            Interop.BOOL defaultCharUsed = Interop.BOOL.FALSE;

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
                    null,
                    throwOnUnmappableChar ? &defaultCharUsed : null);
#else
                cbWritten = Encoding.UTF8.GetBytes(pwzChar, numChars, buffer, length);
#endif
            }

            if (defaultCharUsed != Interop.BOOL.FALSE)
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
            int end = new ReadOnlySpan<byte>((byte*)cstr, length).IndexOf((byte)0);
            if (end >= 0)
            {
                length = end;
            }

            return new string((sbyte*)cstr, 0, length);
        }
    }  // class CSTRMarshaler

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
                bool hasTrailByte = StubHelpers.TryGetStringTrailByte(strManaged, out byte trailByte);

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
                    // String .ctor, since newing up a 0 sized string will always return String.Empty.
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
                    StubHelpers.SetStringTrailByte(ret, ((byte*)bstr)[length - 1]);
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
                Marshal.FreeCoTaskMem(pNative - sizeof(uint));
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
            int end = new ReadOnlySpan<char>((char*)nativeHome, length).IndexOf('\0');
            if (end >= 0)
            {
                length = end;
            }

            return new string((char*)nativeHome, 0, length);
        }
    }  // class WSTRBufferMarshaler
#if FEATURE_COMINTEROP

    internal static partial class ObjectMarshaler
    {
        internal static void ConvertToNative(object objSrc, IntPtr pDstVariant)
        {
            ConvertToNative(ObjectHandleOnStack.Create(ref objSrc), pDstVariant);
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "ObjectMarshaler_ConvertToNative")]
        private static partial void ConvertToNative(ObjectHandleOnStack objSrc, IntPtr pDstVariant);

        internal static object ConvertToManaged(IntPtr pSrcVariant)
        {
            object? retObject = null;
            ConvertToManaged(pSrcVariant, ObjectHandleOnStack.Create(ref retObject));
            return retObject!;
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "ObjectMarshaler_ConvertToManaged")]
        private static partial void ConvertToManaged(IntPtr pSrcVariant, ObjectHandleOnStack retObject);

        internal static unsafe void ClearNative(IntPtr pVariant)
        {
            if (pVariant != IntPtr.Zero)
            {
                Interop.OleAut32.VariantClear(pVariant);

                // VariantClear resets the instance to VT_EMPTY (0)
                // COMPAT: Clear the remaining memory for compat. The instance remains set to VT_EMPTY (0).
                *(ComVariant*)pVariant = default;
            }
        }
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

            ArgumentNullException.ThrowIfNull(handle);

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
        internal static IntPtr ConvertToNative(object? objSrc, IntPtr itfMT, IntPtr classMT, int flags)
        {
            if (objSrc == null)
                return IntPtr.Zero;

            return ConvertToNative(ObjectHandleOnStack.Create(ref objSrc), itfMT, classMT, flags);
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "InterfaceMarshaler_ConvertToNative")]
        private static partial IntPtr ConvertToNative(ObjectHandleOnStack objSrc, IntPtr itfMT, IntPtr classMT, int flag);

        internal static object? ConvertToManaged(ref IntPtr ppUnk, IntPtr itfMT, IntPtr classMT, int flags)
        {
            if (ppUnk == IntPtr.Zero)
                return null;

            object? retObject = null;
            ConvertToManaged(ref ppUnk, itfMT, classMT, flags, ObjectHandleOnStack.Create(ref retObject));
            return retObject;
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "InterfaceMarshaler_ConvertToManaged")]
        private static partial void ConvertToManaged(ref IntPtr ppUnk, IntPtr itfMT, IntPtr classMT, int flags, ObjectHandleOnStack retObject);

        internal static void ClearNative(IntPtr pUnk)
        {
            if (pUnk != IntPtr.Zero)
            {
                Marshal.Release(pUnk);
            }
        }
    }  // class InterfaceMarshaler
#endif // FEATURE_COMINTEROP

    internal static partial class MngdNativeArrayMarshaler
    {
        // Needs to match exactly with MngdNativeArrayMarshaler in ilmarshalers.h
        internal struct MarshalerState
        {
            internal IntPtr m_pElementMT;
            internal TypeHandle m_Array;
            internal IntPtr m_pManagedNativeArrayMarshaler;
            internal int m_NativeDataValid;
            internal int m_BestFitMap;
            internal int m_ThrowOnUnmappableChar;
            internal short m_vt;
        }

        internal static unsafe void CreateMarshaler(IntPtr pMarshalState, IntPtr pMT, int dwFlags, bool nativeDataValid, IntPtr pManagedMarshaler)
        {
            MarshalerState* pState = (MarshalerState*)pMarshalState;
            pState->m_pElementMT = pMT;
            pState->m_Array = default;
            pState->m_pManagedNativeArrayMarshaler = pManagedMarshaler;
            pState->m_NativeDataValid = nativeDataValid ? 1 : 0;
            pState->m_BestFitMap = (byte)(dwFlags >> 16);
            pState->m_ThrowOnUnmappableChar = (byte)(dwFlags >> 24);
            pState->m_vt = (short)dwFlags;
        }

        internal static void ConvertSpaceToNative(IntPtr pMarshalState, in object pManagedHome, IntPtr pNativeHome)
        {
            object managedHome = pManagedHome;
            ConvertSpaceToNative(pMarshalState, ObjectHandleOnStack.Create(ref managedHome), pNativeHome);
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "MngdNativeArrayMarshaler_ConvertSpaceToNative")]
        private static partial void ConvertSpaceToNative(IntPtr pMarshalState, ObjectHandleOnStack pManagedHome, IntPtr pNativeHome);

        internal static void ConvertContentsToNative(IntPtr pMarshalState, in object pManagedHome, IntPtr pNativeHome)
        {
            object managedHome = pManagedHome;
            ConvertContentsToNative(pMarshalState, ObjectHandleOnStack.Create(ref managedHome), pNativeHome);
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "MngdNativeArrayMarshaler_ConvertContentsToNative")]
        private static partial void ConvertContentsToNative(IntPtr pMarshalState, ObjectHandleOnStack pManagedHome, IntPtr pNativeHome);

        internal static void ConvertSpaceToManaged(IntPtr pMarshalState, ref object? pManagedHome, IntPtr pNativeHome,
                                                          int cElements)
        {
            object? managedHome = null;
            ConvertSpaceToManaged(pMarshalState, ObjectHandleOnStack.Create(ref managedHome), pNativeHome, cElements);
            pManagedHome = managedHome;
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "MngdNativeArrayMarshaler_ConvertSpaceToManaged")]
        private static partial void ConvertSpaceToManaged(IntPtr pMarshalState, ObjectHandleOnStack pManagedHome, IntPtr pNativeHome,
                                                         int cElements);

        internal static void ConvertContentsToManaged(IntPtr pMarshalState, in object pManagedHome, IntPtr pNativeHome)
        {
            object managedHome = pManagedHome;
            ConvertContentsToManaged(pMarshalState, ObjectHandleOnStack.Create(ref managedHome), pNativeHome);
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "MngdNativeArrayMarshaler_ConvertContentsToManaged")]
        private static partial void ConvertContentsToManaged(IntPtr pMarshalState, ObjectHandleOnStack pManagedHome, IntPtr pNativeHome);

        internal static unsafe void ClearNative(IntPtr pMarshalState, IntPtr pNativeHome, int cElements)
        {
            IntPtr nativeHome = *(IntPtr*)pNativeHome;

            if (nativeHome != IntPtr.Zero)
            {
                ClearNativeContents(pMarshalState, pNativeHome, cElements);
                Marshal.FreeCoTaskMem(nativeHome);
            }
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "MngdNativeArrayMarshaler_ClearNativeContents")]
        internal static partial void ClearNativeContents(IntPtr pMarshalState, IntPtr pNativeHome, int cElements);
    }  // class MngdNativeArrayMarshaler

    internal static partial class MngdFixedArrayMarshaler
    {
        // Needs to match exactly with MngdFixedArrayMarshaler in ilmarshalers.h
        private struct MarshalerState
        {
#pragma warning disable CA1823, IDE0044 // not used by managed code
            internal IntPtr m_pElementMT;
            internal IntPtr m_pManagedElementMarshaler;
            internal IntPtr m_Array;
            internal int m_BestFitMap;
            internal int m_ThrowOnUnmappableChar;
            internal ushort m_vt;
            internal uint m_cElements;
#pragma warning restore CA1823, IDE0044
        }

        internal static unsafe void CreateMarshaler(IntPtr pMarshalState, IntPtr pMT, int dwFlags, int cElements, IntPtr pManagedMarshaler)
        {
            MarshalerState* pState = (MarshalerState*)pMarshalState;
            pState->m_pElementMT = pMT;
            pState->m_Array = default;
            pState->m_pManagedElementMarshaler = pManagedMarshaler;
            pState->m_BestFitMap = (byte)(dwFlags >> 16);
            pState->m_ThrowOnUnmappableChar = (byte)(dwFlags >> 24);
            pState->m_vt = (ushort)dwFlags;
            pState->m_cElements = (uint)cElements;
        }

        internal static unsafe void ConvertSpaceToNative(IntPtr pMarshalState, in object pManagedHome, IntPtr pNativeHome)
        {
            // We don't actually need to allocate native space here as the space is inline in the native layout.
            // However, we need to validate that we can fit the contents of the managed array in the native space.
            Array arr = (Array)pManagedHome;
            MarshalerState* pState = (MarshalerState*)pMarshalState;

            if (arr is not null && (uint)arr.Length < pState->m_cElements)
            {
                throw new ArgumentException(SR.Argument_WrongSizeArrayInNativeStruct);
            }
        }

        internal static void ConvertContentsToNative(IntPtr pMarshalState, in object pManagedHome, IntPtr pNativeHome)
        {
            object managedHome = pManagedHome;
            ConvertContentsToNative(pMarshalState, ObjectHandleOnStack.Create(ref managedHome), pNativeHome);
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "MngdFixedArrayMarshaler_ConvertContentsToNative")]
        private static partial void ConvertContentsToNative(IntPtr pMarshalState, ObjectHandleOnStack pManagedHome, IntPtr pNativeHome);

        internal static void ConvertSpaceToManaged(IntPtr pMarshalState, ref object pManagedHome, IntPtr pNativeHome)
        {
            object managedHome = pManagedHome;
            ConvertSpaceToManaged(pMarshalState, ObjectHandleOnStack.Create(ref managedHome), pNativeHome);
            pManagedHome = managedHome;
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "MngdFixedArrayMarshaler_ConvertSpaceToManaged")]
        private static partial void ConvertSpaceToManaged(IntPtr pMarshalState, ObjectHandleOnStack pManagedHome, IntPtr pNativeHome);

        internal static void ConvertContentsToManaged(IntPtr pMarshalState, in object pManagedHome, IntPtr pNativeHome)
        {
            object managedHome = pManagedHome;
            ConvertContentsToManaged(pMarshalState, ObjectHandleOnStack.Create(ref managedHome), pNativeHome);
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "MngdFixedArrayMarshaler_ConvertContentsToManaged")]
        private static partial void ConvertContentsToManaged(IntPtr pMarshalState, ObjectHandleOnStack pManagedHome, IntPtr pNativeHome);

#pragma warning disable IDE0060 // Remove unused parameter. These APIs need to match a the shape of a "managed" marshaler.
        internal static void ClearNativeContents(IntPtr pMarshalState, in object pManagedHome, IntPtr pNativeHome)
        {
            ClearNativeContents(pMarshalState, pNativeHome);
        }
#pragma warning restore IDE0060

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "MngdFixedArrayMarshaler_ClearNativeContents")]
        private static partial void ClearNativeContents(IntPtr pMarshalState, IntPtr pNativeHome);
    }  // class MngdFixedArrayMarshaler

#if FEATURE_COMINTEROP
    internal static partial class MngdSafeArrayMarshaler
    {

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "MngdSafeArrayMarshaler_CreateMarshaler")]
        [SuppressGCTransition]
        internal static partial void CreateMarshaler(IntPtr pMarshalState, IntPtr pMT, int iRank, int dwFlags, IntPtr pManagedMarshaler);

        internal static void ConvertSpaceToNative(IntPtr pMarshalState, in object pManagedHome, IntPtr pNativeHome)
        {
            object managedHome = pManagedHome;
            ConvertSpaceToNative(pMarshalState, ObjectHandleOnStack.Create(ref managedHome), pNativeHome);
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "MngdSafeArrayMarshaler_ConvertSpaceToNative")]
        private static partial void ConvertSpaceToNative(IntPtr pMarshalState, ObjectHandleOnStack pManagedHome, IntPtr pNativeHome);

        internal static void ConvertContentsToNative(IntPtr pMarshalState, in object pManagedHome, IntPtr pNativeHome, object pOriginalManagedObject)
        {
            object managedHome = pManagedHome;
            object originalManagedObject = pOriginalManagedObject;
            ConvertContentsToNative(pMarshalState, ObjectHandleOnStack.Create(ref managedHome), pNativeHome, ObjectHandleOnStack.Create(ref originalManagedObject));
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "MngdSafeArrayMarshaler_ConvertContentsToNative")]
        private static partial void ConvertContentsToNative(IntPtr pMarshalState, ObjectHandleOnStack pManagedHome, IntPtr pNativeHome, ObjectHandleOnStack pOriginalManagedObject);

        internal static void ConvertSpaceToManaged(IntPtr pMarshalState, ref object? pManagedHome, IntPtr pNativeHome)
        {
            object? managedHome = null;
            ConvertSpaceToManaged(pMarshalState, ObjectHandleOnStack.Create(ref managedHome), pNativeHome);
            pManagedHome = managedHome;
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "MngdSafeArrayMarshaler_ConvertSpaceToManaged")]
        private static partial void ConvertSpaceToManaged(IntPtr pMarshalState, ObjectHandleOnStack pManagedHome, IntPtr pNativeHome);

        internal static void ConvertContentsToManaged(IntPtr pMarshalState, in object pManagedHome, IntPtr pNativeHome)
        {
            object managedHome = pManagedHome;
            ConvertContentsToManaged(pMarshalState, ObjectHandleOnStack.Create(ref managedHome), pNativeHome);
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "MngdSafeArrayMarshaler_ConvertContentsToManaged")]
        private static partial void ConvertContentsToManaged(IntPtr pMarshalState, ObjectHandleOnStack pManagedHome, IntPtr pNativeHome);

#pragma warning disable IDE0060 // Remove unused parameter. These APIs need to match a the shape of a "managed" marshaler.
        internal static void ClearNative(IntPtr pMarshalState, in object pManagedHome, IntPtr pNativeHome)
        {
            ClearNative(pMarshalState, pNativeHome);
        }
#pragma warning restore IDE0060

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "MngdSafeArrayMarshaler_ClearNative")]
        private static partial void ClearNative(IntPtr pMarshalState, IntPtr pNativeHome);
    }  // class MngdSafeArrayMarshaler
#endif // FEATURE_COMINTEROP

    internal static unsafe partial class MngdRefCustomMarshaler
    {
        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "CustomMarshaler_GetMarshalerObject")]
        private static partial void GetMarshaler(IntPtr pCMHelper, ObjectHandleOnStack retMarshaler);

        internal static ICustomMarshaler GetMarshaler(IntPtr pCMHelper)
        {
            ICustomMarshaler? marshaler = null;
            GetMarshaler(pCMHelper, ObjectHandleOnStack.Create(ref marshaler));
            return marshaler!;
        }

        internal static unsafe void ConvertContentsToNative(ICustomMarshaler marshaler, ref object pManagedHome, IntPtr* pNativeHome)
        {
            *pNativeHome = marshaler.MarshalManagedToNative(pManagedHome);
        }

        internal static void ConvertContentsToManaged(ICustomMarshaler marshaler, ref object pManagedHome, IntPtr* pNativeHome)
        {
            pManagedHome = marshaler.MarshalNativeToManaged(*pNativeHome);
        }

#pragma warning disable IDE0060 // Remove unused parameter. These APIs need to match a the shape of a "managed" marshaler.
        internal static void ClearNative(ICustomMarshaler marshaler, ref object pManagedHome, IntPtr* pNativeHome)
        {
            try
            {
                marshaler.CleanUpNativeData(*pNativeHome);
            }
            catch
            {
                // COMPAT: We need to swallow all exceptions thrown by CleanUpNativeData.
            }
        }

        internal static void ClearManaged(ICustomMarshaler marshaler, ref object pManagedHome, IntPtr* pNativeHome)
        {
            marshaler.CleanUpManagedData(pManagedHome);
        }
#pragma warning restore IDE0060
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
        private readonly IntPtr pvArrayMarshaler;

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
                nativeDataValid: false,
                IntPtr.Zero);     // not needed as we marshal primitive VTs only

            IntPtr pNativeHome;
            IntPtr pNativeHomeAddr = new IntPtr(&pNativeHome);

            MngdNativeArrayMarshaler.ConvertSpaceToNative(
                pvArrayMarshaler,
                in pManagedHome,
                pNativeHomeAddr);

            if (IsIn(dwFlags))
            {
                MngdNativeArrayMarshaler.ConvertContentsToNative(
                    pvArrayMarshaler,
                    in pManagedHome,
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
            int allocSize = Marshal.SizeOfHelper((RuntimeType)pManagedHome.GetType(), false);
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
                            in pManagedHome,
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

    // Constants for direction argument of struct marshalling stub.
    internal static class MarshalOperation
    {
        internal const int Marshal = 0;
        internal const int Unmarshal = 1;
        internal const int Cleanup = 2;
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

        private readonly object m_obj;

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

        private readonly SafeHandle m_handle;

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

    internal static partial class StubHelpers
    {
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern IntPtr GetDelegateTarget(Delegate pThis);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern void ClearLastError();

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern void SetLastError();

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "StubHelpers_ThrowInteropParamException")]
        internal static partial void ThrowInteropParamException(int resID, int paramIdx);

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
            {
                ex.InternalPreserveStackTrace();
                s_pendingExceptionObject = null;
            }

            return ex;
        }

        internal static void SetPendingExceptionObject(Exception? exception)
        {
            s_pendingExceptionObject = exception;
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "StubHelpers_CreateCustomMarshalerHelper")]
        internal static partial IntPtr CreateCustomMarshalerHelper(IntPtr pMD, int paramToken, IntPtr hndManagedType);

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

        // Try to retrieve the extra byte - returns false if not present.
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern bool TryGetStringTrailByte(string str, out byte data);

        // Set extra byte for odd-sized strings that came from interop as BSTR.
        internal static void SetStringTrailByte(string str, byte data)
        {
            SetStringTrailByte(new StringHandleOnStack(ref str!), data);
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "StubHelpers_SetStringTrailByte")]
        private static partial void SetStringTrailByte(StringHandleOnStack str, byte data);

        internal static unsafe void FmtClassUpdateNativeInternal(object obj, byte* pNative, ref CleanupWorkListElement? pCleanupWorkList)
        {
            MethodTable* pMT = RuntimeHelpers.GetMethodTable(obj);

            delegate*<ref byte, byte*, int, ref CleanupWorkListElement?, void> structMarshalStub;
            nuint size;
            bool success = Marshal.TryGetStructMarshalStub((IntPtr)pMT, &structMarshalStub, &size);
            Debug.Assert(success);

            if (structMarshalStub != null)
            {
                structMarshalStub(ref obj.GetRawData(), pNative, MarshalOperation.Marshal, ref pCleanupWorkList);
            }
            else
            {
                Buffer.Memmove(ref *pNative, ref obj.GetRawData(), size);
            }
        }

        internal static unsafe void FmtClassUpdateCLRInternal(object obj, byte* pNative)
        {
            MethodTable* pMT = RuntimeHelpers.GetMethodTable(obj);

            delegate*<ref byte, byte*, int, ref CleanupWorkListElement?, void> structMarshalStub;
            nuint size;
            bool success = Marshal.TryGetStructMarshalStub((IntPtr)pMT, &structMarshalStub, &size);
            Debug.Assert(success);

            if (structMarshalStub != null)
            {
                structMarshalStub(ref obj.GetRawData(), pNative, MarshalOperation.Unmarshal, ref Unsafe.NullRef<CleanupWorkListElement?>());
            }
            else
            {
                Buffer.Memmove(ref obj.GetRawData(), ref *pNative, size);
            }
        }

        internal static unsafe void LayoutDestroyNativeInternal(object obj, byte* pNative)
        {
            MethodTable* pMT = RuntimeHelpers.GetMethodTable(obj);

            delegate*<ref byte, byte*, int, ref CleanupWorkListElement?, void> structMarshalStub;
            nuint size;
            bool success = Marshal.TryGetStructMarshalStub((IntPtr)pMT, &structMarshalStub, &size);
            Debug.Assert(success);

            if (structMarshalStub != null)
            {
                structMarshalStub(ref obj.GetRawData(), pNative, MarshalOperation.Cleanup, ref Unsafe.NullRef<CleanupWorkListElement?>());
            }
        }

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
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern IntPtr NextCallReturnAddress();
    }  // class StubHelpers
}
