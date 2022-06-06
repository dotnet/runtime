// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Diagnostics;

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
        internal static unsafe nint ConvertToNative(int flags, string strManaged, nint pNativeBuffer)
        {
            if (null == strManaged)
            {
                return 0;
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
                    Marshal.FreeCoTaskMem((nint)pbNativeBuffer);
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

            return (nint)pbNativeBuffer;
        }

        internal static unsafe string? ConvertToManaged(nint cstr)
        {
            if (0 == cstr)
                return null;
            else
                return new string((sbyte*)cstr);
        }

        internal static unsafe void ConvertFixedToNative(int flags, string strManaged, nint pNativeBuffer, int length)
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

        internal static unsafe string ConvertFixedToManaged(nint cstr, int length)
        {
            int end = SpanHelpers.IndexOf(ref *(byte*)cstr, 0, length);
            if (end >= 0)
            {
                length = end;
            }

            return new string((sbyte*)cstr, 0, length);
        }
    }  // class CSTRMarshaler

    internal static class UTF8BufferMarshaler
    {
        internal static unsafe nint ConvertToNative(StringBuilder sb, nint pNativeBuffer, int flags)
        {
            if (null == sb)
            {
                return 0;
            }

            // Convert to string first
            string strManaged = sb.ToString();

            // Get byte count
            int nb = Encoding.UTF8.GetByteCount(strManaged);

            // EmitConvertSpaceCLRToNative allocates memory
            byte* pbNativeBuffer = (byte*)pNativeBuffer;
            nb = strManaged.GetBytesFromEncoding(pbNativeBuffer, nb, Encoding.UTF8);

            pbNativeBuffer[nb] = 0x0;
            return (nint)pbNativeBuffer;
        }

        internal static unsafe void ConvertToManaged(StringBuilder sb, nint pNative)
        {
            if (pNative == 0)
                return;

            byte* pBytes = (byte*)pNative;
            int nbBytes = string.strlen(pBytes);
            sb.ReplaceBufferUtf8Internal(new ReadOnlySpan<byte>(pBytes, nbBytes));
        }
    }

    internal static class BSTRMarshaler
    {
        internal static unsafe nint ConvertToNative(string strManaged, nint pNativeBuffer)
        {
            if (null == strManaged)
            {
                return 0;
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

                if (pNativeBuffer != 0)
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
                return (nint)ptrToFirstChar;
            }
        }

        internal static unsafe string? ConvertToManaged(nint bstr)
        {
            if (0 == bstr)
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

        internal static void ClearNative(nint pNative)
        {
            Marshal.FreeBSTR(pNative);
        }
    }  // class BSTRMarshaler

    internal static class VBByValStrMarshaler
    {
        internal static unsafe nint ConvertToNative(string strManaged, bool fBestFit, bool fThrowOnUnmappableChar, ref int cch)
        {
            if (null == strManaged)
            {
                return 0;
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

            return (nint)pNative;
        }

        internal static unsafe string? ConvertToManaged(nint pNative, int cch)
        {
            if (0 == pNative)
            {
                return null;
            }

            return new string((sbyte*)pNative, 0, cch);
        }

        internal static void ClearNative(nint pNative)
        {
            if (0 != pNative)
            {
                Marshal.FreeCoTaskMem((nint)(((long)pNative) - sizeof(uint)));
            }
        }
    }  // class VBByValStrMarshaler

    internal static class AnsiBSTRMarshaler
    {
        internal static unsafe nint ConvertToNative(int flags, string strManaged)
        {
            if (null == strManaged)
            {
                return 0;
            }

            byte[]? bytes = null;
            int nb = 0;

            if (strManaged.Length > 0)
            {
                bytes = AnsiCharMarshaler.DoAnsiConversion(strManaged, 0 != (flags & 0xFF), 0 != (flags >> 8), out nb);
            }

            uint length = (uint)nb;
            nint bstr = Marshal.AllocBSTRByteLen(length);
            if (bytes != null)
            {
                Buffer.Memmove(ref *(byte*)bstr, ref MemoryMarshal.GetArrayDataReference(bytes), length);
            }

            return bstr;
        }

        internal static unsafe string? ConvertToManaged(nint bstr)
        {
            if (0 == bstr)
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

        internal static void ClearNative(nint pNative)
        {
            Marshal.FreeBSTR(pNative);
        }
    }  // class AnsiBSTRMarshaler

    internal static class FixedWSTRMarshaler
    {
        internal static unsafe void ConvertToNative(string? strManaged, nint nativeHome, int length)
        {
            ReadOnlySpan<char> managed = strManaged;
            Span<char> native = new Span<char>((char*)nativeHome, length);

            int numChars = Math.Min(managed.Length, length - 1);

            managed.Slice(0, numChars).CopyTo(native);
            native[numChars] = '\0';
        }

        internal static unsafe string ConvertToManaged(nint nativeHome, int length)
        {
            int end = SpanHelpers.IndexOf(ref *(char*)nativeHome, '\0', length);
            if (end >= 0)
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
        internal static extern void ConvertToNative(object objSrc, nint pDstVariant);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern object ConvertToManaged(nint pSrcVariant);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern void ClearNative(nint pVariant);
    }  // class ObjectMarshaler

#endif // FEATURE_COMINTEROP

    internal sealed class HandleMarshaler
    {
        internal static unsafe nint ConvertSafeHandleToNative(SafeHandle? handle, ref CleanupWorkListElement? cleanupWorkList)
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
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern nint ConvertToNative(object objSrc, nint itfMT, nint classMT, int flags);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern object ConvertToManaged(ref nint ppUnk, nint itfMT, nint classMT, int flags);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "InterfaceMarshaler__ClearNative")]
        internal static partial void ClearNative(nint pUnk);
    }  // class InterfaceMarshaler
#endif // FEATURE_COMINTEROP

    internal static class MngdNativeArrayMarshaler
    {
        // Needs to match exactly with MngdNativeArrayMarshaler in ilmarshalers.h
        internal struct MarshalerState
        {
#pragma warning disable CA1823 // not used by managed code
            private nint m_pElementMT;
            private nint m_Array;
            private nint m_pManagedNativeArrayMarshaler;
            private int m_NativeDataValid;
            private int m_BestFitMap;
            private int m_ThrowOnUnmappableChar;
            private short m_vt;
#pragma warning restore CA1823
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern void CreateMarshaler(nint pMarshalState, nint pMT, int dwFlags, nint pManagedMarshaler);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern void ConvertSpaceToNative(nint pMarshalState, ref object pManagedHome, nint pNativeHome);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern void ConvertContentsToNative(nint pMarshalState, ref object pManagedHome, nint pNativeHome);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern void ConvertSpaceToManaged(nint pMarshalState, ref object pManagedHome, nint pNativeHome,
                                                          int cElements);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern void ConvertContentsToManaged(nint pMarshalState, ref object pManagedHome, nint pNativeHome);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern void ClearNative(nint pMarshalState, ref object pManagedHome, nint pNativeHome, int cElements);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern void ClearNativeContents(nint pMarshalState, ref object pManagedHome, nint pNativeHome, int cElements);
    }  // class MngdNativeArrayMarshaler

    internal static class MngdFixedArrayMarshaler
    {
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern void CreateMarshaler(nint pMarshalState, nint pMT, int dwFlags, int cElements, nint pManagedMarshaler);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern void ConvertSpaceToNative(nint pMarshalState, ref object pManagedHome, nint pNativeHome);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern void ConvertContentsToNative(nint pMarshalState, ref object pManagedHome, nint pNativeHome);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern void ConvertSpaceToManaged(nint pMarshalState, ref object pManagedHome, nint pNativeHome);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern void ConvertContentsToManaged(nint pMarshalState, ref object pManagedHome, nint pNativeHome);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern void ClearNativeContents(nint pMarshalState, ref object pManagedHome, nint pNativeHome);
    }  // class MngdFixedArrayMarshaler

#if FEATURE_COMINTEROP
    internal static class MngdSafeArrayMarshaler
    {
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern void CreateMarshaler(nint pMarshalState, nint pMT, int iRank, int dwFlags, nint pManagedMarshaler);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern void ConvertSpaceToNative(nint pMarshalState, ref object pManagedHome, nint pNativeHome);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern void ConvertContentsToNative(nint pMarshalState, ref object pManagedHome, nint pNativeHome, object pOriginalManaged);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern void ConvertSpaceToManaged(nint pMarshalState, ref object pManagedHome, nint pNativeHome);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern void ConvertContentsToManaged(nint pMarshalState, ref object pManagedHome, nint pNativeHome);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern void ClearNative(nint pMarshalState, ref object pManagedHome, nint pNativeHome);
    }  // class MngdSafeArrayMarshaler
#endif // FEATURE_COMINTEROP

    internal static class MngdRefCustomMarshaler
    {
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern void CreateMarshaler(nint pMarshalState, nint pCMHelper);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern void ConvertContentsToNative(nint pMarshalState, ref object pManagedHome, nint pNativeHome);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern void ConvertContentsToManaged(nint pMarshalState, ref object pManagedHome, nint pNativeHome);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern void ClearNative(nint pMarshalState, ref object pManagedHome, nint pNativeHome);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern void ClearManaged(nint pMarshalState, ref object pManagedHome, nint pNativeHome);
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
        private nint pvArrayMarshaler;

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

        internal AsAnyMarshaler(nint pvArrayMarshaler)
        {
            // we need this in case the value being marshaled turns out to be array
            Debug.Assert(pvArrayMarshaler != 0, "pvArrayMarshaler must not be null");

            this.pvArrayMarshaler = pvArrayMarshaler;
            backPropAction = BackPropAction.None;
            layoutType = null;
            cleanupWorkList = null;
        }

        #region ConvertToNative helpers

        private unsafe nint ConvertArrayToNative(object pManagedHome, int dwFlags)
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
                        if (elementType == typeof(nint))
                        {
                            vt = (sizeof(nint) == 4 ? VarEnum.VT_I4 : VarEnum.VT_I8);
                        }
                        else if (elementType == typeof(nuint))
                        {
                            vt = (sizeof(nint) == 4 ? VarEnum.VT_UI4 : VarEnum.VT_UI8);
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
                0,      // not needed as we marshal primitive VTs only
                dwArrayMarshalerFlags,
                0);     // not needed as we marshal primitive VTs only

            nint pNativeHome;
            nint pNativeHomeAddr = (nint)(&pNativeHome);

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

        private static nint ConvertStringToNative(string pManagedHome, int dwFlags)
        {
            nint pNativeHome;

            // IsIn, IsOut are ignored for strings - they're always in-only
            if (IsAnsi(dwFlags))
            {
                // marshal the object as Ansi string (UnmanagedType.LPStr)
                pNativeHome = CSTRMarshaler.ConvertToNative(
                    dwFlags & 0xFFFF, // (throw on unmappable char << 8 | best fit)
                    pManagedHome,     //
                    0);     // unmanaged buffer will be allocated
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

        private unsafe nint ConvertStringBuilderToNative(StringBuilder pManagedHome, int dwFlags)
        {
            nint pNativeHome;

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

        private unsafe nint ConvertLayoutToNative(object pManagedHome, int dwFlags)
        {
            // Note that the following call will not throw exception if the type
            // of pManagedHome is not marshalable. That's intentional because we
            // want to maintain the original behavior where this was indicated
            // by TypeLoadException during the actual field marshaling.
            int allocSize = Marshal.SizeOfHelper(pManagedHome.GetType(), false);
            nint pNativeHome = Marshal.AllocCoTaskMem(allocSize);

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

        internal nint ConvertToNative(object pManagedHome, int dwFlags)
        {
            if (pManagedHome == null)
                return 0;

            if (pManagedHome is ArrayWithOffset)
                throw new ArgumentException(SR.Arg_MarshalAsAnyRestriction);

            nint pNativeHome;

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

        internal unsafe void ConvertToManaged(object pManagedHome, nint pNativeHome)
        {
            switch (backPropAction)
            {
                case BackPropAction.Array:
                    {
                        MngdNativeArrayMarshaler.ConvertContentsToManaged(
                            pvArrayMarshaler,
                            ref pManagedHome,
                            (nint)(&pNativeHome));
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
                        if (pNativeHome == 0)
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
                        if (pNativeHome == 0)
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

        internal void ClearNative(nint pNativeHome)
        {
            if (pNativeHome != 0)
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
        private nint data1;
        private nint data2;
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

        public nint AddRef()
        {
            // element.m_owned will be true iff the AddRef succeeded
            return StubHelpers.SafeHandleAddRef(m_handle, ref m_owned);
        }
    }  // class CleanupWorkListElement

    internal static class StubHelpers
    {
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern nint GetNDirectTarget(nint pMD);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern nint GetDelegateTarget(Delegate pThis);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern void ClearLastError();

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern void SetLastError();

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern void ThrowInteropParamException(int resID, int paramIdx);

        internal static nint AddToCleanupList(ref CleanupWorkListElement? pCleanupWorkList, SafeHandle handle)
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
        internal static Exception GetCOMHRExceptionObject(int hr, nint pCPCMD, object pThis)
        {
            Exception ex = InternalGetCOMHRExceptionObject(hr, pCPCMD, pThis);
            ex.InternalPreserveStackTrace();
            return ex;
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern Exception InternalGetCOMHRExceptionObject(int hr, nint pCPCMD, object? pThis);

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
        internal static extern nint CreateCustomMarshalerHelper(nint pMD, int paramToken, nint hndManagedType);

        //-------------------------------------------------------
        // SafeHandle Helpers
        //-------------------------------------------------------

        // AddRefs the SH and returns the underlying unmanaged handle.
        internal static nint SafeHandleAddRef(SafeHandle pHandle, ref bool success)
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
        internal static extern nint GetCOMIPFromRCW(object objSrc, nint pCPCMD, out nint ppTarget, out bool pfNeedsRelease);
#endif // FEATURE_COMINTEROP

        //-------------------------------------------------------
        // Profiler helpers
        //-------------------------------------------------------
#if PROFILING_SUPPORTED
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern nint ProfilerBeginTransitionCallback(nint pSecretParam, nint pThread, object pThis);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern void ProfilerEndTransitionCallback(nint pMD, nint pThread);
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
        internal static extern object AllocateInternal(nint typeHandle);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern void MarshalToUnmanagedVaListInternal(nint va_list, uint vaListSize, nint pArgIterator);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern void MarshalToManagedVaListInternal(nint va_list, nint pArgIterator);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern uint CalcVaListSize(nint va_list);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern void ValidateObject(object obj, nint pMD, object pThis);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern void LogPinnedArgument(nint localDesc, nint nativeArg);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern void ValidateByref(nint byref, nint pMD, object pThis); // the byref is pinned so we can safely "cast" it to nint

        [Intrinsic]
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern nint GetStubContext();

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
        internal static extern nint NextCallReturnAddress();
    }  // class StubHelpers
}
