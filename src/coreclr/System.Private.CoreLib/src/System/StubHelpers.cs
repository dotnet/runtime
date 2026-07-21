// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
#if FEATURE_COMINTEROP
using System.Runtime.InteropServices.CustomMarshalers;
#endif
using System.Runtime.InteropServices.Marshalling;
using System.Runtime.Versioning;
using System.Text;
using System.Threading;

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
            Debug.Assert(cbAllocLength <= 512); // Some arbitrary upper limit, in most cases SystemMaxDBCSCharSize is expected to be 1 or 2.
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
        internal static unsafe IntPtr ConvertToNative(int flags, string? strManaged, IntPtr pNativeBuffer)
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

                    SpanHelpers.Memmove(ref *pbNativeBuffer, ref MemoryMarshal.GetArrayDataReference(bytes), (nuint)nb);
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
        private sealed class TrailByte(byte trailByte)
        {
            public readonly byte Value = trailByte;
        }

        // In some early version of VB when there were no arrays developers used to use BSTR as arrays
        // The way this was done was by adding a trail byte at the end of the BSTR
        // To support this scenario, we need to use a ConditionalWeakTable for this special case and
        // save the trail character in here.
        // This stores the trail character when a BSTR is used as an array.
        private static ConditionalWeakTable<string, TrailByte>? s_trailByteTable;

        private static bool TryGetTrailByte(string strManaged, out byte trailByte)
        {
            if (s_trailByteTable?.TryGetValue(strManaged, out TrailByte? trailByteObj) == true)
            {
                trailByte = trailByteObj.Value;
                return true;
            }

            trailByte = 0;
            return false;
        }

        private static void SetTrailByte(string strManaged, byte trailByte)
        {
            if (s_trailByteTable == null)
            {
                Interlocked.CompareExchange(ref s_trailByteTable, new ConditionalWeakTable<string, TrailByte>(), null);
            }
            s_trailByteTable!.Add(strManaged, new TrailByte(trailByte));
        }

        internal static unsafe IntPtr ConvertToNative(string? strManaged, IntPtr pNativeBuffer)
        {
            if (null == strManaged)
            {
                return IntPtr.Zero;
            }
            else
            {
                bool hasTrailByte = TryGetTrailByte(strManaged, out byte trailByte);

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

        [UnmanagedCallersOnly]
        private static unsafe IntPtr ConvertToNative(string* pStr, Exception* pException)
        {
            try
            {
                return ConvertToNative(*pStr, IntPtr.Zero);
            }
            catch (Exception ex)
            {
                *pException = ex;
                return default;
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
                    SetTrailByte(ret, ((byte*)bstr)[length - 1]);
                }

                return ret;
            }
        }

        [UnmanagedCallersOnly]
        private static unsafe void ConvertToManaged(IntPtr bstr, string* pResult, Exception* pException)
        {
            try
            {
                *pResult = ConvertToManaged(bstr);
            }
            catch (Exception ex)
            {
                *pException = ex;
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

                SpanHelpers.Memmove(ref *pNative, ref MemoryMarshal.GetArrayDataReference(bytes), (nuint)nbytesused);

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
                SpanHelpers.Memmove(ref *(byte*)bstr, ref MemoryMarshal.GetArrayDataReference(bytes), length);
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
        internal static IntPtr ConvertSafeHandleToNative(SafeHandle? handle, ref CleanupWorkListElement? cleanupWorkList)
        {
            if (Unsafe.IsNullRef(ref cleanupWorkList))
            {
                throw new InvalidOperationException(SR.Interop_Marshal_SafeHandle_InvalidOperation);
            }

            ArgumentNullException.ThrowIfNull(handle);

            return StubHelpers.AddToCleanupList(ref cleanupWorkList, handle);
        }

        internal static void ThrowSafeHandleFieldChanged()
        {
            throw new NotSupportedException(SR.Interop_Marshal_CannotCreateSafeHandleField);
        }

        internal static void ThrowCriticalHandleFieldChanged()
        {
            throw new NotSupportedException(SR.Interop_Marshal_CannotCreateCriticalHandleField);
        }
    }

    internal sealed class DateMarshaler : IArrayElementMarshaler<DateTime, DateMarshaler>
    {
        internal static double ConvertToNative(DateTime managedDate)
        {
            return managedDate.ToOADate();
        }

        internal static long ConvertToManaged(double nativeDate)
        {
            return DateTime.DoubleDateToTicks(nativeDate);
        }

        static unsafe void IArrayElementMarshaler<DateTime, DateMarshaler>.ConvertToUnmanaged(ref DateTime managed, byte* unmanaged)
        {
            Unsafe.WriteUnaligned(unmanaged, ConvertToNative(managed));
        }

        static unsafe void IArrayElementMarshaler<DateTime, DateMarshaler>.ConvertToManaged(ref DateTime managed, byte* unmanaged)
        {
            managed = new DateTime(ConvertToManaged(Unsafe.ReadUnaligned<double>(unmanaged)));
        }

        static unsafe void IArrayElementMarshaler<DateTime, DateMarshaler>.Free(byte* unmanaged)
        {
        }

        static unsafe nuint IArrayElementMarshaler<DateTime, DateMarshaler>.UnmanagedSize => (nuint)sizeof(double);
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

        internal static object GetObjectForComCallableWrapperIUnknown(IntPtr unk)
        {
            object? retObject = null;
            GetObjectForComCallableWrapperIUnknown(unk, ObjectHandleOnStack.Create(ref retObject));
            return retObject!;
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "InterfaceMarshaler_GetObjectForComCallableWrapperIUnknown")]
        private static partial void GetObjectForComCallableWrapperIUnknown(IntPtr unk, ObjectHandleOnStack retObject);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "InterfaceMarshaler_ValidateComVisibilityForIUnknown")]
        internal static partial void ValidateComVisibilityForIUnknown(IntPtr unk);
    }  // class InterfaceMarshaler
#endif // FEATURE_COMINTEROP

#if FEATURE_COMINTEROP
    internal static partial class MngdSafeArrayMarshaler
    {

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "MngdSafeArrayMarshaler_CreateMarshaler")]
        [SuppressGCTransition]
        internal static partial void CreateMarshaler(IntPtr pMarshalState, IntPtr pMT, int iRank, int dwFlags, IntPtr pConvertToNative, IntPtr pConvertToManaged);

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
        [UnmanagedCallersOnly]
        internal static void ConvertContentsToNative(ICustomMarshaler* pMarshaler, object* pManagedHome, IntPtr* pNativeHome, Exception* pException)
        {
            try
            {
                ConvertContentsToNative(*pMarshaler, in *pManagedHome, pNativeHome);
            }
            catch (Exception ex)
            {
                *pException = ex;
            }
        }

        internal static void ConvertContentsToNative(ICustomMarshaler marshaler, in object pManagedHome, IntPtr* pNativeHome)
        {
            // COMPAT: We never pass null to MarshalManagedToNative.
            if (pManagedHome is null)
            {
                *pNativeHome = IntPtr.Zero;
                return;
            }

            *pNativeHome = marshaler.MarshalManagedToNative(pManagedHome);
        }

        [UnmanagedCallersOnly]
        internal static void ConvertContentsToManaged(ICustomMarshaler* pMarshaler, object* pManagedHome, IntPtr* pNativeHome, Exception* pException)
        {
            try
            {
                ConvertContentsToManaged(*pMarshaler, ref *pManagedHome, pNativeHome);
            }
            catch (Exception ex)
            {
                *pException = ex;
            }
        }

        internal static void ConvertContentsToManaged(ICustomMarshaler marshaler, ref object? pManagedHome, IntPtr* pNativeHome)
        {
            // COMPAT: We never pass null to MarshalNativeToManaged.
            if (*pNativeHome == IntPtr.Zero)
            {
                pManagedHome = null;
                return;
            }

            pManagedHome = marshaler.MarshalNativeToManaged(*pNativeHome);
        }

        [UnmanagedCallersOnly]
        internal static void ClearNative(ICustomMarshaler* pMarshaler, object* pManagedHome, IntPtr* pNativeHome, Exception* pException)
        {
            try
            {
                ClearNative(*pMarshaler, ref *pManagedHome, pNativeHome);
            }
            catch (Exception ex)
            {
                *pException = ex;
            }
        }

        internal static void ClearNative(ICustomMarshaler marshaler, ref object _, IntPtr* pNativeHome)
        {
            // COMPAT: We never pass null to CleanUpNativeData.
            if (*pNativeHome == IntPtr.Zero)
            {
                return;
            }

            try
            {
                marshaler.CleanUpNativeData(*pNativeHome);
            }
            catch
            {
                // COMPAT: We need to swallow all exceptions thrown by CleanUpNativeData.
            }
        }

        [UnmanagedCallersOnly]
        internal static void ClearManaged(ICustomMarshaler* pMarshaler, object* pManagedHome, IntPtr* pNativeHome, Exception* pException)
        {
            try
            {
                ClearManaged(*pMarshaler, in *pManagedHome, pNativeHome);
            }
            catch (Exception ex)
            {
                *pException = ex;
            }
        }

        internal static void ClearManaged(ICustomMarshaler marshaler, in object pManagedHome, IntPtr* _)
        {
            // COMPAT: We never pass null to CleanUpManagedData.
            if (pManagedHome is null)
            {
                return;
            }

            marshaler.CleanUpManagedData(pManagedHome);
        }

        [UnmanagedCallersOnly]
        [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Custom marshaler GetInstance method is preserved by ILLink (see MarkCustomMarshalerGetInstance).")]
        internal static void GetCustomMarshalerInstance(void* pMT, byte* pCookie, int cCookieBytes, object* pResult, Exception* pException)
        {
            try
            {
                RuntimeType marshalerType = RuntimeTypeHandle.GetRuntimeType((MethodTable*)pMT);

                MethodInfo? method = marshalerType.GetMethod(
                    "GetInstance",
                    Reflection.BindingFlags.Static | Reflection.BindingFlags.Public | Reflection.BindingFlags.NonPublic,
                    [typeof(string)]);

                if (method is null || typeof(ICustomMarshaler) != method.ReturnType)
                {
                    throw new ApplicationException(SR.Format(SR.CustomMarshaler_NoGetInstanceMethod, marshalerType.FullName));
                }

                var getInstance = method.CreateDelegate<Func<string, ICustomMarshaler>>();
                string cookie = Text.Encoding.UTF8.GetString(new ReadOnlySpan<byte>(pCookie, cCookieBytes));
                object? result = getInstance(cookie);
                if (result is null)
                {
                    throw new ApplicationException(SR.Format(SR.CustomMarshaler_NullReturnForGetInstance, marshalerType.FullName));
                }

                *pResult = result;
            }
            catch (Exception ex)
            {
                *pException = ex;
            }
        }
    }  // class MngdRefCustomMarshaler

    internal struct AsAnyMarshaler
    {
        private AsAnyMarshalerImplementation? _impl;

        private abstract class AsAnyMarshalerImplementation
        {
            public abstract IntPtr ConvertToNative(object managed, int dwFlags);
            public abstract void ConvertToManaged(object managed, IntPtr native);
            public abstract void ClearNative(IntPtr native);
        }

        private sealed class ArrayImplementation<T, TMarshaler> : AsAnyMarshalerImplementation
            where TMarshaler : IArrayMarshaler<T, TMarshaler>
        {
            private readonly bool _isOut;

            public ArrayImplementation(bool isOut) { _isOut = isOut; }

            public override unsafe IntPtr ConvertToNative(object managed, int dwFlags)
            {
                Array array = (Array)managed;
                byte* pNative = TMarshaler.AllocateSpaceForUnmanaged(array);
                try
                {
                    if (IsIn(dwFlags))
                        TMarshaler.ConvertContentsToUnmanaged(array, pNative, array.Length);
                }
                catch
                {
                    Marshal.FreeCoTaskMem((IntPtr)pNative);
                    throw;
                }

                return (IntPtr)pNative;
            }

            public override unsafe void ConvertToManaged(object managed, IntPtr native)
            {
                if (!_isOut) return;
                Array array = (Array)managed;
                TMarshaler.ConvertContentsToManaged(array, (byte*)native, array.Length);
            }

            public override void ClearNative(IntPtr native)
            {
                if (native != IntPtr.Zero)
                    Marshal.FreeCoTaskMem(native);
            }
        }

        private sealed class StringImplementation : AsAnyMarshalerImplementation
        {
            public override unsafe IntPtr ConvertToNative(object managed, int dwFlags)
            {
                string str = (string)managed;

                // IsIn, IsOut are ignored for strings - they're always in-only
                if (IsAnsi(dwFlags))
                {
                    return CSTRMarshaler.ConvertToNative(
                        dwFlags & 0xFFFF, // (throw on unmappable char << 8 | best fit)
                        str,
                        IntPtr.Zero);     // unmanaged buffer will be allocated
                }

                int allocSize = (str.Length + 1) * 2;
                IntPtr pNative = Marshal.AllocCoTaskMem(allocSize);
                Buffer.Memmove(ref *(char*)pNative, ref str.GetRawStringData(), (nuint)str.Length + 1);

                return pNative;
            }

            public override void ConvertToManaged(object managed, IntPtr native) { }

            public override void ClearNative(IntPtr native)
            {
                if (native != IntPtr.Zero)
                    Marshal.FreeCoTaskMem(native);
            }
        }

        private sealed class LayoutImplementation : AsAnyMarshalerImplementation
        {
            private readonly Type _layoutType;
            private readonly bool _isOut;
            internal CleanupWorkListElement? _cleanupWorkList;

            public LayoutImplementation(Type layoutType, bool isOut)
            {
                _layoutType = layoutType;
                _isOut = isOut;
            }

            public override unsafe IntPtr ConvertToNative(object managed, int dwFlags)
            {
                // Note that the following call will not throw exception if the type
                // of managed is not marshalable. That's intentional because we
                // want to maintain the original behavior where this was indicated
                // by TypeLoadException during the actual field marshaling.
                int allocSize = Marshal.SizeOfHelper((RuntimeType)_layoutType, false);
                IntPtr pNative = Marshal.AllocCoTaskMem(allocSize);

                if (IsIn(dwFlags))
                {
                    StubHelpers.LayoutTypeConvertToUnmanaged(managed, (byte*)pNative, ref _cleanupWorkList);
                }

                return pNative;
            }

            public override unsafe void ConvertToManaged(object managed, IntPtr native)
            {
                if (_isOut)
                    StubHelpers.LayoutTypeConvertToManaged(managed, (byte*)native);
            }

            public override void ClearNative(IntPtr native)
            {
                if (native != IntPtr.Zero)
                {
                    Marshal.DestroyStructure(native, _layoutType);
                    Marshal.FreeCoTaskMem(native);
                }
                StubHelpers.DestroyCleanupList(ref _cleanupWorkList);
            }
        }

        private sealed class StringBuilderAnsiImplementation : AsAnyMarshalerImplementation
        {
            private readonly bool _isOut;

            public StringBuilderAnsiImplementation(bool isOut) { _isOut = isOut; }

            public override unsafe IntPtr ConvertToNative(object managed, int dwFlags)
            {
                StringBuilder sb = (StringBuilder)managed;

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
                int capacity = sb.Capacity;
                int length = sb.Length;
                if (length > capacity)
                    ThrowHelper.ThrowInvalidOperationException();

                // Note that StringBuilder.Capacity is the number of characters NOT including any terminators.
                StubHelpers.CheckStringLength(capacity);

                int allocSize = checked((capacity * Marshal.SystemMaxDBCSCharSize) + 4);
                IntPtr pNative = Marshal.AllocCoTaskMem(allocSize);

                byte* ptr = (byte*)pNative;
                *(ptr + allocSize - 3) = 0;
                *(ptr + allocSize - 2) = 0;
                *(ptr + allocSize - 1) = 0;

                if (IsIn(dwFlags))
                {
                    int len = Marshal.StringToAnsiString(sb.ToString(),
                        ptr, allocSize,
                        IsBestFit(dwFlags),
                        IsThrowOn(dwFlags));
                    Debug.Assert(len < allocSize, "Expected a length less than the allocated size");
                }

                return pNative;
            }

            public override unsafe void ConvertToManaged(object managed, IntPtr native)
            {
                if (!_isOut) return;
                int length = native == IntPtr.Zero ? 0 : string.strlen((byte*)native);
                ((StringBuilder)managed).ReplaceBufferAnsiInternal((sbyte*)native, length);
            }

            public override void ClearNative(IntPtr native)
            {
                if (native != IntPtr.Zero)
                    Marshal.FreeCoTaskMem(native);
            }
        }

        private sealed class StringBuilderUnicodeImplementation : AsAnyMarshalerImplementation
        {
            private readonly bool _isOut;

            public StringBuilderUnicodeImplementation(bool isOut) { _isOut = isOut; }

            public override unsafe IntPtr ConvertToNative(object managed, int dwFlags)
            {
                StringBuilder sb = (StringBuilder)managed;

                // See StringBuilderAnsiImplementation.ConvertToNative for buffer layout explanation.

                // Cache StringBuilder capacity and length to ensure we don't allocate a certain amount of
                // native memory and then walk beyond its end if the StringBuilder concurrently grows erroneously.
                int capacity = sb.Capacity;
                int length = sb.Length;
                if (length > capacity)
                    ThrowHelper.ThrowInvalidOperationException();

                // Note that StringBuilder.Capacity is the number of characters NOT including any terminators.
                int allocSize = checked((capacity * 2) + 4);
                IntPtr pNative = Marshal.AllocCoTaskMem(allocSize);

                byte* ptr = (byte*)pNative;
                *(ptr + allocSize - 1) = 0;
                *(ptr + allocSize - 2) = 0;

                if (IsIn(dwFlags))
                {
                    sb.InternalCopy(pNative, length);

                    int byteLen = length * 2;
                    *(ptr + byteLen + 0) = 0;
                    *(ptr + byteLen + 1) = 0;
                }

                return pNative;
            }

            public override unsafe void ConvertToManaged(object managed, IntPtr native)
            {
                if (!_isOut) return;
                int length = native == IntPtr.Zero ? 0 : string.wcslen((char*)native);
                ((StringBuilder)managed).ReplaceBufferInternal((char*)native, length);
            }

            public override void ClearNative(IntPtr native)
            {
                if (native != IntPtr.Zero)
                    Marshal.FreeCoTaskMem(native);
            }
        }

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

        private static AsAnyMarshalerImplementation CreateAnsiCharArrayImplementation(bool isOut, int dwFlags)
        {
            return (IsBestFit(dwFlags), IsThrowOn(dwFlags)) switch
            {
                (true, true) => new ArrayImplementation<char, AnsiCharArrayMarshaler<IMarshalerOption.EnabledOption, IMarshalerOption.EnabledOption>>(isOut),
                (true, false) => new ArrayImplementation<char, AnsiCharArrayMarshaler<IMarshalerOption.EnabledOption, IMarshalerOption.DisabledOption>>(isOut),
                (false, true) => new ArrayImplementation<char, AnsiCharArrayMarshaler<IMarshalerOption.DisabledOption, IMarshalerOption.EnabledOption>>(isOut),
                (false, false) => new ArrayImplementation<char, AnsiCharArrayMarshaler<IMarshalerOption.DisabledOption, IMarshalerOption.DisabledOption>>(isOut),
            };
        }

        internal AsAnyMarshaler(object? pManagedHome, int dwFlags)
        {
            _impl = null;

            if (pManagedHome is null)
                return;

            if (pManagedHome is ArrayWithOffset)
                throw new ArgumentException(SR.Arg_MarshalAsAnyRestriction);

            if (pManagedHome.GetType().IsArray)
            {
                Type elementType = pManagedHome.GetType().GetElementType()!;
                bool isOut = IsOut(dwFlags);
                _impl = Type.GetTypeCode(elementType) switch
                {
                    TypeCode.SByte => new ArrayImplementation<sbyte, BlittableArrayMarshaler<sbyte>>(isOut),
                    TypeCode.Byte => new ArrayImplementation<byte, BlittableArrayMarshaler<byte>>(isOut),
                    TypeCode.Int16 => new ArrayImplementation<short, BlittableArrayMarshaler<short>>(isOut),
                    TypeCode.UInt16 => new ArrayImplementation<ushort, BlittableArrayMarshaler<ushort>>(isOut),
                    TypeCode.Int32 => new ArrayImplementation<int, BlittableArrayMarshaler<int>>(isOut),
                    TypeCode.UInt32 => new ArrayImplementation<uint, BlittableArrayMarshaler<uint>>(isOut),
                    TypeCode.Int64 => new ArrayImplementation<long, BlittableArrayMarshaler<long>>(isOut),
                    TypeCode.UInt64 => new ArrayImplementation<ulong, BlittableArrayMarshaler<ulong>>(isOut),
                    TypeCode.Single => new ArrayImplementation<float, BlittableArrayMarshaler<float>>(isOut),
                    TypeCode.Double => new ArrayImplementation<double, BlittableArrayMarshaler<double>>(isOut),
                    TypeCode.Object when elementType == typeof(nint) => new ArrayImplementation<nint, BlittableArrayMarshaler<nint>>(isOut),
                    TypeCode.Object when elementType == typeof(nuint) => new ArrayImplementation<nuint, BlittableArrayMarshaler<nuint>>(isOut),
                    TypeCode.Char when !IsAnsi(dwFlags) => new ArrayImplementation<char, BlittableArrayMarshaler<char>>(isOut),
                    TypeCode.Char when IsAnsi(dwFlags) => CreateAnsiCharArrayImplementation(isOut, dwFlags),
                    TypeCode.Boolean => new ArrayImplementation<bool, BoolMarshaler<int>>(isOut),
                    _ => throw new ArgumentException(SR.Arg_PInvokeBadObject)
                };
            }
            else if (pManagedHome is string)
            {
                _impl = new StringImplementation();
            }
            else if (pManagedHome is StringBuilder)
            {
                bool isOut = IsOut(dwFlags);
                _impl = IsAnsi(dwFlags)
                    ? new StringBuilderAnsiImplementation(isOut)
                    : new StringBuilderUnicodeImplementation(isOut);
            }
            else if (pManagedHome.GetType().IsLayoutSequential || pManagedHome.GetType().IsExplicitLayout)
            {
                _impl = new LayoutImplementation(pManagedHome.GetType(), IsOut(dwFlags));
            }
            else
            {
                throw new ArgumentException(SR.Arg_PInvokeBadObject);
            }
        }

        internal IntPtr ConvertToNative(object pManagedHome, int dwFlags)
        {
            return _impl?.ConvertToNative(pManagedHome, dwFlags) ?? IntPtr.Zero;
        }

        internal void ConvertToManaged(object pManagedHome, IntPtr pNativeHome)
        {
            _impl?.ConvertToManaged(pManagedHome, pNativeHome);
        }

        internal void ClearNative(IntPtr pNativeHome)
        {
            if (_impl is not null)
            {
                _impl.ClearNative(pNativeHome);
            }
            else if (pNativeHome != IntPtr.Zero)
            {
                Marshal.FreeCoTaskMem(pNativeHome);
            }
        }
    }  // struct AsAnyMarshaler

    internal interface IArrayMarshaler<T, TSelf>
        where TSelf : IArrayMarshaler<T, TSelf>
    {
        static abstract unsafe void ConvertContentsToUnmanaged(Array managedArray, byte* unmanaged, int length);
        static abstract unsafe void ConvertContentsToManaged(Array managedArray, byte* unmanaged, int length);
        static abstract unsafe void FreeContents(byte* unmanaged, int length);
        static abstract unsafe byte* AllocateSpaceForUnmanaged(Array? managedArray);
        static abstract unsafe Array? AllocateSpaceForManaged(byte* unmanaged, int length);
    }

    internal interface IArrayElementMarshaler<T, TSelf> : IArrayMarshaler<T, TSelf>
        where TSelf : IArrayElementMarshaler<T, TSelf>, IArrayMarshaler<T, TSelf>
    {
        static unsafe void IArrayMarshaler<T, TSelf>.ConvertContentsToManaged(Array managedArray, byte* unmanaged, int length)
        {
            Span<T> elements = new(ref Unsafe.As<byte, T>(ref MemoryMarshal.GetArrayDataReference(managedArray)), managedArray.Length);
            for (int i = 0; i < length; i++)
            {
                TSelf.ConvertToManaged(ref elements[i], unmanaged);
                unmanaged += TSelf.UnmanagedSize;
            }
        }

        static unsafe void IArrayMarshaler<T, TSelf>.ConvertContentsToUnmanaged(Array managedArray, byte* unmanaged, int length)
        {
            Span<T> elements = new(ref Unsafe.As<byte, T>(ref MemoryMarshal.GetArrayDataReference(managedArray)), managedArray.Length);
            for (int i = 0; i < length; i++)
            {
                TSelf.ConvertToUnmanaged(ref elements[i], unmanaged);
                unmanaged += TSelf.UnmanagedSize;
            }
        }

        static unsafe void IArrayMarshaler<T, TSelf>.FreeContents(byte* unmanaged, int length)
        {
            for (int i = 0; i < length; i++)
            {
                TSelf.Free(unmanaged);
                unmanaged += TSelf.UnmanagedSize;
            }
        }

        static unsafe byte* IArrayMarshaler<T, TSelf>.AllocateSpaceForUnmanaged(Array? managedArray)
        {
            if (managedArray is null)
            {
                return null;
            }
            else
            {
                const nuint MaxSizeForInterop = 0x7ffffff0u;
                nuint elementCount = (nuint)(uint)managedArray.Length;
                nuint elementSize = TSelf.UnmanagedSize;
                if (elementCount != 0 && elementSize > MaxSizeForInterop / elementCount)
                    throw new ArgumentException(SR.Argument_StructArrayTooLarge);
                nuint nativeBytes = elementCount * elementSize;
                byte* pNative = (byte*)Marshal.AllocCoTaskMem((int)nativeBytes);
                NativeMemory.Clear(pNative, nativeBytes);
                return pNative;
            }
        }

        static unsafe Array? IArrayMarshaler<T, TSelf>.AllocateSpaceForManaged(byte* unmanaged, int length)
        {
            if (unmanaged is null)
            {
                return null;
            }
            else
            {
                return new T[length];
            }
        }

        static abstract unsafe void ConvertToUnmanaged(ref T managed, byte* unmanaged);
        static abstract unsafe void ConvertToManaged(ref T managed, byte* unmanaged);
        static abstract unsafe void Free(byte* unmanaged);

        static abstract nuint UnmanagedSize { get; }
    }

    // Constants for direction argument of struct marshalling stub.
    internal static class MarshalOperation
    {
        internal const int ConvertToUnmanaged = 0;
        internal const int ConvertToManaged = 1;
        internal const int Free = 2;
    }

    internal sealed class BlittableArrayMarshaler<T> : IArrayMarshaler<T, BlittableArrayMarshaler<T>>
        where T : unmanaged
    {
        static unsafe void IArrayMarshaler<T, BlittableArrayMarshaler<T>>.ConvertContentsToUnmanaged(Array managedArray, byte* unmanaged, int length)
        {
            SpanHelpers.Memmove(ref *unmanaged, ref MemoryMarshal.GetArrayDataReference(managedArray), (nuint)length * (nuint)sizeof(T));
        }

        static unsafe void IArrayMarshaler<T, BlittableArrayMarshaler<T>>.ConvertContentsToManaged(Array managedArray, byte* unmanaged, int length)
        {
            SpanHelpers.Memmove(ref MemoryMarshal.GetArrayDataReference(managedArray), ref *unmanaged, (nuint)length * (nuint)sizeof(T));
        }

        static unsafe void IArrayMarshaler<T, BlittableArrayMarshaler<T>>.FreeContents(byte* unmanaged, int length)
        {
        }

        static unsafe byte* IArrayMarshaler<T, BlittableArrayMarshaler<T>>.AllocateSpaceForUnmanaged(Array? managedArray)
        {
            if (managedArray is null)
                return null;

            const nuint MaxSizeForInterop = 0x7ffffff0u;
            nuint elementCount = (nuint)(uint)managedArray.Length;
            nuint elementSize = (nuint)sizeof(T);
            if (elementCount != 0 && elementSize > MaxSizeForInterop / elementCount)
                throw new ArgumentException(SR.Argument_StructArrayTooLarge);
            nuint nativeBytes = elementCount * elementSize;
            byte* pNative = (byte*)Marshal.AllocCoTaskMem((int)nativeBytes);
            NativeMemory.Clear(pNative, nativeBytes);

            return pNative;
        }

        static unsafe Array? IArrayMarshaler<T, BlittableArrayMarshaler<T>>.AllocateSpaceForManaged(byte* unmanaged, int length)
        {
            if (unmanaged is null)
                return null;

            return new T[length];
        }
    }

    internal sealed unsafe class StructureMarshaler<T> : IArrayElementMarshaler<T, StructureMarshaler<T>> where T : notnull
    {
        static unsafe void IArrayElementMarshaler<T, StructureMarshaler<T>>.ConvertToManaged(ref T managed, byte* unmanaged)
        {
            ConvertToManaged(ref managed, unmanaged, ref Unsafe.NullRef<CleanupWorkListElement?>());
        }

        static unsafe void IArrayElementMarshaler<T, StructureMarshaler<T>>.ConvertToUnmanaged(ref T managed, byte* unmanaged)
        {
            ConvertToUnmanaged(ref managed, unmanaged, ref Unsafe.NullRef<CleanupWorkListElement?>());
        }

        static unsafe void IArrayElementMarshaler<T, StructureMarshaler<T>>.Free(byte* unmanaged)
        {
            Free(ref Unsafe.NullRef<T>(), unmanaged, ref Unsafe.NullRef<CleanupWorkListElement?>());
        }

        static nuint IArrayElementMarshaler<T, StructureMarshaler<T>>.UnmanagedSize => (nuint)UnmanagedSize;

        private static class SizeHolder
        {
            public static readonly int UnmanagedSize = typeof(T).IsEnum ? Marshal.SizeOf(Enum.GetUnderlyingType(typeof(T))) : Marshal.SizeOf<T>();
        }

        private static int UnmanagedSize
        {
            get
            {
                try
                {
                    return SizeHolder.UnmanagedSize;
                }
                catch (TypeInitializationException ex)
                {
                    ExceptionDispatchInfo.Capture(ex.InnerException ?? ex).Throw();
                    return 0;
                }
            }
        }

        [Conditional("DEBUG")]
        private static void Validate()
        {
            Debug.Assert(typeof(T).IsValueType, "StructureMarshaler can only be used for value types");
            RuntimeType type = (RuntimeType)typeof(T);
            bool hasLayout = Marshal.HasLayout(new QCallTypeHandle(ref type), out bool isBlittable, out int _);
            Debug.Assert(hasLayout, "Non-layout classes should not use the layout class marshaler.");
            Debug.Assert(isBlittable, "Non-blittable structs should have a custom IL body generated with the marshaling logic.");
        }

        [Intrinsic]
        private static void ConvertToUnmanagedCore(ref T managed, byte* unmanaged, ref CleanupWorkListElement? cleanupWorkList)
        {
            Validate();
            _ = ref cleanupWorkList;
            SpanHelpers.Memmove(ref *unmanaged, ref Unsafe.As<T, byte>(ref managed), (nuint)sizeof(T));
        }

        public static void ConvertToUnmanaged(ref T managed, byte* unmanaged, ref CleanupWorkListElement? cleanupWorkList)
        {
            try
            {
                NativeMemory.Clear(unmanaged, (nuint)UnmanagedSize);
                ConvertToUnmanagedCore(ref managed, unmanaged, ref cleanupWorkList);
            }
            catch (Exception)
            {
                // If Free throws an exception (which it shouldn't as it can leak)
                // let that exception supercede the exception from ConvertToUnmanagedCore.
                Free(ref managed, unmanaged, ref cleanupWorkList);
                throw;
            }
        }

        [Intrinsic]
        public static void ConvertToManaged(ref T managed, byte* unmanaged, ref CleanupWorkListElement? cleanupWorkList)
        {
            Validate();
            _ = ref cleanupWorkList;
            SpanHelpers.Memmove(ref Unsafe.As<T, byte>(ref managed), ref *unmanaged, (nuint)sizeof(T));
        }

        [Intrinsic]
        private static void FreeCore(ref T managed, byte* unmanaged, ref CleanupWorkListElement? cleanupWorkList)
        {
            Validate();
#nullable disable warnings // https://github.com/dotnet/roslyn/issues/82919
            _ = ref managed;
#nullable restore warnings
            _ = unmanaged;
            _ = ref cleanupWorkList;
        }

        public static void Free(ref T managed, byte* unmanaged, ref CleanupWorkListElement? cleanupWorkList)
        {
            if (unmanaged != null)
            {
                FreeCore(ref managed, unmanaged, ref cleanupWorkList);
                NativeMemory.Clear(unmanaged, (nuint)UnmanagedSize);
            }
        }
    }

    internal sealed unsafe class LayoutClassMarshaler<T> : IArrayElementMarshaler<T, LayoutClassMarshaler<T>> where T : notnull
    {
        // We use a nested Methods class with properties that unwrap the TypeInitializationException
        // to ensure that users see a TypeLoadException if the type has a recursive native layout.
        // This also ensures that we don't leak internal implementation details about how we generate marshalling stubs.
        private static class Methods
        {
            private static readonly delegate*<ref byte, byte*, ref CleanupWorkListElement?, void> _convertToUnmanaged;
            private static readonly delegate*<ref byte, byte*, ref CleanupWorkListElement?, void> _convertToManaged;
            private static readonly delegate*<ref byte, byte*, ref CleanupWorkListElement?, void> _free;

            private static readonly nuint s_unmanagedSize;

#pragma warning disable CA1810 // Static constructor is required to initialize with the out parameters
            static Methods()
            {
                RuntimeTypeHandle th = typeof(T).TypeHandle;
                bool hasLayout = Marshal.HasLayout(new QCallTypeHandle(ref th), out bool isBlittable, out int nativeSize);
                Debug.Assert(hasLayout, "Non-layout classes should not use the layout class marshaler.");
                s_unmanagedSize = (nuint)nativeSize;
                if (isBlittable)
                {
                    _convertToUnmanaged = &BlittableConvertToUnmanaged;
                    _convertToManaged = &BlittableConvertToManaged;
                    _free = &BlittableFree;
                }
                else
                {
                    StubHelpers.CreateLayoutClassMarshalStubs(new QCallTypeHandle(ref th), out _convertToUnmanaged, out _convertToManaged, out _free);
                }
            }
#pragma warning restore CA1810

            private static void BlittableConvertToUnmanaged(ref byte managed, byte* unmanaged, ref CleanupWorkListElement? cleanupWorkList)
            {
                SpanHelpers.Memmove(ref *unmanaged, ref managed, s_unmanagedSize);
            }

            private static void BlittableConvertToManaged(ref byte managed, byte* unmanaged, ref CleanupWorkListElement? cleanupWorkList)
            {
                SpanHelpers.Memmove(ref managed, ref *unmanaged, s_unmanagedSize);
            }

            private static void BlittableFree(ref byte managed, byte* unmanaged, ref CleanupWorkListElement? cleanupWorkList)
            {
                // Nothing to do for blittable types.
            }

            internal static delegate*<ref byte, byte*, ref CleanupWorkListElement?, void> ConvertToUnmanaged => _convertToUnmanaged;

            internal static delegate*<ref byte, byte*, ref CleanupWorkListElement?, void> ConvertToManaged => _convertToManaged;

            internal static delegate*<ref byte, byte*, ref CleanupWorkListElement?, void> Free => _free;

            internal static nuint UnmanagedSize => s_unmanagedSize;
        }

        private static void ConvertToUnmanagedCore(T managed, byte* unmanaged, ref CleanupWorkListElement? cleanupWorkList)
        {
            try
            {
                CallConvertToUnmanaged(ref managed.GetRawData(), unmanaged, ref cleanupWorkList);
            }
            catch (TypeInitializationException ex)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException ?? ex).Throw();
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static void CallConvertToUnmanaged(ref byte managed, byte* unmanaged, ref CleanupWorkListElement? cleanupWorkList)
            {
                Methods.ConvertToUnmanaged(ref managed, unmanaged, ref cleanupWorkList);
            }
        }

        public static void ConvertToUnmanaged(T managed, byte* unmanaged, ref CleanupWorkListElement? cleanupWorkList)
        {
            try
            {
                NativeMemory.Clear(unmanaged, UnmanagedSize);
                ConvertToUnmanagedCore(managed, unmanaged, ref cleanupWorkList);
            }
            catch (Exception)
            {
                // If Free throws an exception (which it shouldn't as it can leak)
                // let that exception supercede the exception from ConvertToUnmanagedCore.
                Free(managed, unmanaged, ref cleanupWorkList);
                throw;
            }
        }

        public static void ConvertToManaged(T managed, byte* unmanaged, ref CleanupWorkListElement? cleanupWorkList)
        {
            try
            {
                CallConvertToManaged(ref managed.GetRawData(), unmanaged, ref cleanupWorkList);
            }
            catch (TypeInitializationException ex)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException ?? ex).Throw();
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static void CallConvertToManaged(ref byte managed, byte* unmanaged, ref CleanupWorkListElement? cleanupWorkList)
            {
                Methods.ConvertToManaged(ref managed, unmanaged, ref cleanupWorkList);
            }
        }

        private static void FreeCore(T? managed, byte* unmanaged, ref CleanupWorkListElement? cleanupWorkList)
        {
            try
            {
                CallFree(managed, unmanaged, ref cleanupWorkList);
            }
            catch (TypeInitializationException ex)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException ?? ex).Throw();
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static void CallFree(T? managed, byte* unmanaged, ref CleanupWorkListElement? cleanupWorkList)
            {
                if (managed is null)
                {
                    Methods.Free(ref Unsafe.NullRef<byte>(), unmanaged, ref cleanupWorkList);
                }
                else
                {
                    Methods.Free(ref managed.GetRawData(), unmanaged, ref cleanupWorkList);
                }
            }
        }
        public static void Free(T? managed, byte* unmanaged, ref CleanupWorkListElement? cleanupWorkList)
        {
            if (unmanaged != null)
            {
                FreeCore(managed, unmanaged, ref cleanupWorkList);
                NativeMemory.Clear(unmanaged, UnmanagedSize);
            }
        }

        private static nuint UnmanagedSize
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            get
            {
                try
                {
                    return Methods.UnmanagedSize;
                }
                catch (TypeInitializationException ex)
                {
                    ExceptionDispatchInfo.Capture(ex.InnerException ?? ex).Throw();
                    // Unreachable
                    return 0;
                }
            }
        }

        static unsafe void IArrayElementMarshaler<T, LayoutClassMarshaler<T>>.ConvertToManaged(ref T managed, byte* unmanaged)
        {
            ConvertToManaged(managed, unmanaged, ref Unsafe.NullRef<CleanupWorkListElement?>());
        }

        static unsafe void IArrayElementMarshaler<T, LayoutClassMarshaler<T>>.ConvertToUnmanaged(ref T managed, byte* unmanaged)
        {
            ConvertToUnmanaged(managed, unmanaged, ref Unsafe.NullRef<CleanupWorkListElement?>());
        }

        static unsafe void IArrayElementMarshaler<T, LayoutClassMarshaler<T>>.Free(byte* unmanaged)
        {
            Free(default, unmanaged, ref Unsafe.NullRef<CleanupWorkListElement?>());
        }

        static nuint IArrayElementMarshaler<T, LayoutClassMarshaler<T>>.UnmanagedSize => UnmanagedSize;
    }

    // Marshaller for layout classes and boxed structs.
    internal static unsafe class BoxedLayoutTypeMarshaler<T> where T : notnull
    {
        public static void ConvertToUnmanaged(object managed, byte* unmanaged, ref CleanupWorkListElement? cleanupWorkList)
        {
            if (typeof(T).IsValueType)
            {
                StructureMarshaler<T>.ConvertToUnmanaged(ref Unsafe.As<byte, T>(ref managed.GetRawData()), unmanaged, ref cleanupWorkList);
            }
            else
            {
                LayoutClassMarshaler<T>.ConvertToUnmanaged(Unsafe.As<object, T>(ref managed), unmanaged, ref cleanupWorkList);
            }
        }

        public static void ConvertToManaged(object managed, byte* unmanaged, ref CleanupWorkListElement? cleanupWorkList)
        {
            if (typeof(T).IsValueType)
            {
                StructureMarshaler<T>.ConvertToManaged(ref Unsafe.As<byte, T>(ref managed.GetRawData()), unmanaged, ref cleanupWorkList);
            }
            else
            {
                LayoutClassMarshaler<T>.ConvertToManaged(Unsafe.As<object, T>(ref managed), unmanaged, ref cleanupWorkList);
            }
        }

        public static void Free(object? managed, byte* unmanaged, ref CleanupWorkListElement? cleanupWorkList)
        {
            if (typeof(T).IsValueType)
            {
                ref byte managedRef = ref Unsafe.NullRef<byte>();

                if (managed != null)
                {
                    managedRef = ref managed.GetRawData();
                }

                StructureMarshaler<T>.Free(ref Unsafe.As<byte, T>(ref managedRef), unmanaged, ref cleanupWorkList);
            }
            else
            {
                LayoutClassMarshaler<T>.Free(Unsafe.As<object?, T?>(ref managed), unmanaged, ref cleanupWorkList);
            }
        }
    }

    internal sealed class VariantBoolMarshaler : IArrayElementMarshaler<bool, VariantBoolMarshaler>
    {
        private const ushort VARIANT_TRUE = unchecked((ushort)-1);
        private const ushort VARIANT_FALSE = 0;
        public static unsafe void ConvertToUnmanaged(ref bool managed, byte* unmanaged)
        {
            *(ushort*)unmanaged = managed ? VARIANT_TRUE : VARIANT_FALSE;
        }

        public static unsafe void ConvertToManaged(ref bool managed, byte* unmanaged)
        {
            managed = (*(ushort*)unmanaged) != VARIANT_FALSE;
        }

        public static unsafe void Free(byte* unmanaged)
        {
            _ = unmanaged;
            // Nothing to free for VARIANT_BOOL.
        }

        static nuint IArrayElementMarshaler<bool, VariantBoolMarshaler>.UnmanagedSize => (nuint)sizeof(short);
    }

    internal sealed class BoolMarshaler<TUnmanaged> : IArrayElementMarshaler<bool, BoolMarshaler<TUnmanaged>> where TUnmanaged : unmanaged, INumberBase<TUnmanaged>
    {
        public static unsafe void ConvertToUnmanaged(ref bool managed, byte* unmanaged)
        {
            TUnmanaged value = managed ? TUnmanaged.One : TUnmanaged.Zero;
            Unsafe.WriteUnaligned(unmanaged, value);
        }

        public static unsafe void ConvertToManaged(ref bool managed, byte* unmanaged)
        {
            TUnmanaged value = Unsafe.ReadUnaligned<TUnmanaged>(unmanaged);
            managed = !value.Equals(TUnmanaged.Zero);
        }

        public static unsafe void Free(byte* unmanaged)
        {
            _ = unmanaged;
            // Nothing to free for boolean values.
        }

        static unsafe nuint IArrayElementMarshaler<bool, BoolMarshaler<TUnmanaged>>.UnmanagedSize => (nuint)sizeof(TUnmanaged);
    }

    internal sealed class LPWSTRMarshaler : IArrayElementMarshaler<string?, LPWSTRMarshaler>
    {
        public static unsafe void ConvertToUnmanaged(ref string? managed, byte* unmanaged)
        {
            IntPtr native = IntPtr.Zero;

            if (managed is not null)
            {
                int allocSize = (managed.Length + 1) * sizeof(char);
                native = Marshal.AllocCoTaskMem(allocSize);
                string.InternalCopy(managed, native, allocSize);
            }

            *(IntPtr*)unmanaged = native;
        }

        public static unsafe void ConvertToManaged(ref string? managed, byte* unmanaged)
        {
            IntPtr native = *(IntPtr*)unmanaged;
            if (native == IntPtr.Zero)
            {
                managed = null;
            }
            else
            {
                StubHelpers.CheckStringLength((uint)string.wcslen((char*)native));
                managed = new string((char*)native);
            }
        }

        public static unsafe void Free(byte* unmanaged)
        {
            IntPtr pNativeHome = *(IntPtr*)unmanaged;
            if (pNativeHome != IntPtr.Zero)
            {
                Marshal.FreeCoTaskMem(pNativeHome);
            }
        }

        static unsafe nuint IArrayElementMarshaler<string?, LPWSTRMarshaler>.UnmanagedSize => (nuint)sizeof(IntPtr);
    }

    internal sealed class AnsiCharArrayMarshaler<TBestFit, TThrowOnUnmappable> : IArrayMarshaler<char, AnsiCharArrayMarshaler<TBestFit, TThrowOnUnmappable>>
        where TBestFit : IMarshalerOption
        where TThrowOnUnmappable : IMarshalerOption
    {
        public static unsafe void ConvertContentsToUnmanaged(Array managedArray, byte* unmanaged, int length)
        {
            fixed (byte* pCharBytes = &MemoryMarshal.GetArrayDataReference(managedArray))
            {
                char* pChars = (char*)pCharBytes;
#if TARGET_WINDOWS
                uint flags = TBestFit.Enabled ? 0 : Interop.Kernel32.WC_NO_BEST_FIT_CHARS;
                Interop.BOOL defaultCharUsed = Interop.BOOL.FALSE;
                int result = Interop.Kernel32.WideCharToMultiByte(
                    Interop.Kernel32.CP_ACP,
                    flags,
                    pChars,
                    length,
                    unmanaged,
                    length,
                    null,
                    TThrowOnUnmappable.Enabled ? &defaultCharUsed : null);

                if (result == 0 && length > 0)
                {
                    throw new ArgumentException(SR.Interop_Marshal_Unmappable_Char);
                }

                if (defaultCharUsed != Interop.BOOL.FALSE)
                {
                    throw new ArgumentException(SR.Interop_Marshal_Unmappable_Char);
                }
#else
                Encoding.UTF8.GetBytes(pChars, length, unmanaged, length);
#endif
            }

        }

        public static unsafe void ConvertContentsToManaged(Array managedArray, byte* unmanaged, int length)
        {
            fixed (byte* pCharBytes = &MemoryMarshal.GetArrayDataReference(managedArray))
            {
                char* pChars = (char*)pCharBytes;
#if TARGET_WINDOWS
                int result = Interop.Kernel32.MultiByteToWideChar(
                    Interop.Kernel32.CP_ACP,
                    Interop.Kernel32.MB_PRECOMPOSED,
                    unmanaged,
                    length,
                    pChars,
                    length);

                if (result == 0 && length > 0)
                {
                    Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
                }
#else
                Encoding.UTF8.GetChars(unmanaged, length, pChars, length);
#endif
            }
        }

        public static unsafe void FreeContents(byte* unmanaged, int length)
        {
        }

        public static unsafe byte* AllocateSpaceForUnmanaged(Array? managedArray)
        {
            if (managedArray is null)
            {
                return null;
            }

            // Native layout for ANSI char arrays uses 1 byte per element.
            int allocSize = managedArray.Length;
            byte* pNative = (byte*)Marshal.AllocCoTaskMem(allocSize);
            NativeMemory.Clear(pNative, (nuint)allocSize);
            return pNative;
        }

        public static unsafe Array? AllocateSpaceForManaged(byte* unmanaged, int length)
        {
            if (unmanaged is null)
            {
                return null;
            }

            return new char[length];
        }
    }

    internal sealed class LPSTRArrayElementMarshaler<TBestFit, TThrowOnUnmappable> : IArrayElementMarshaler<string?, LPSTRArrayElementMarshaler<TBestFit, TThrowOnUnmappable>>
        where TBestFit : IMarshalerOption
        where TThrowOnUnmappable : IMarshalerOption
    {
        public static unsafe void ConvertToUnmanaged(ref string? managed, byte* unmanaged)
        {
            int flags = (TBestFit.Enabled ? 0xFF : 0) | (TThrowOnUnmappable.Enabled ? 0xFF00 : 0);
            *(IntPtr*)unmanaged = CSTRMarshaler.ConvertToNative(flags, managed, IntPtr.Zero);
        }

        public static unsafe void ConvertToManaged(ref string? managed, byte* unmanaged)
        {
            managed = CSTRMarshaler.ConvertToManaged(*(IntPtr*)unmanaged);
        }

        public static unsafe void Free(byte* unmanaged)
        {
            IntPtr ptr = *(IntPtr*)unmanaged;
            if (ptr != IntPtr.Zero)
            {
                Marshal.FreeCoTaskMem(ptr);
            }
        }

        static unsafe nuint IArrayElementMarshaler<string?, LPSTRArrayElementMarshaler<TBestFit, TThrowOnUnmappable>>.UnmanagedSize => (nuint)sizeof(IntPtr);
    }

    internal sealed class BSTRArrayElementMarshaler : IArrayElementMarshaler<string?, BSTRArrayElementMarshaler>
    {
        public static unsafe void ConvertToUnmanaged(ref string? managed, byte* unmanaged)
        {
            *(IntPtr*)unmanaged = BSTRMarshaler.ConvertToNative(managed, IntPtr.Zero);
        }

        public static unsafe void ConvertToManaged(ref string? managed, byte* unmanaged)
        {
            managed = BSTRMarshaler.ConvertToManaged(*(IntPtr*)unmanaged);
        }

        public static unsafe void Free(byte* unmanaged)
        {
            IntPtr bstr = *(IntPtr*)unmanaged;
            if (bstr != IntPtr.Zero)
            {
                BSTRMarshaler.ClearNative(bstr);
            }
        }

        static unsafe nuint IArrayElementMarshaler<string?, BSTRArrayElementMarshaler>.UnmanagedSize => (nuint)sizeof(IntPtr);
    }

#if FEATURE_COMINTEROP
    internal sealed class CurrencyArrayElementMarshaler : IArrayElementMarshaler<decimal, CurrencyArrayElementMarshaler>
    {
        public static unsafe void ConvertToUnmanaged(ref decimal managed, byte* unmanaged)
        {
            *(Currency*)unmanaged = new Currency(managed);
        }

        public static unsafe void ConvertToManaged(ref decimal managed, byte* unmanaged)
        {
            managed = new decimal(*(Currency*)unmanaged);
        }

        public static unsafe void Free(byte* unmanaged)
        {
        }

        static unsafe nuint IArrayElementMarshaler<decimal, CurrencyArrayElementMarshaler>.UnmanagedSize => (nuint)sizeof(Currency);
    }

    [SupportedOSPlatform("windows")]
    internal sealed class InterfaceArrayElementMarshaler<TIsDispatch> : IArrayElementMarshaler<object?, InterfaceArrayElementMarshaler<TIsDispatch>>
        where TIsDispatch : IMarshalerOption
    {
        public static unsafe void ConvertToUnmanaged(ref object? managed, byte* unmanaged)
        {
            if (managed is null)
            {
                *(IntPtr*)unmanaged = IntPtr.Zero;
            }
            else if (TIsDispatch.Enabled)
            {
                *(IntPtr*)unmanaged = Marshal.GetIDispatchForObject(managed);
            }
            else
            {
                *(IntPtr*)unmanaged = Marshal.GetIUnknownForObject(managed);
            }
        }

        public static unsafe void ConvertToManaged(ref object? managed, byte* unmanaged)
        {
            IntPtr pUnk = *(IntPtr*)unmanaged;
            if (pUnk == IntPtr.Zero)
            {
                managed = null;
            }
            else
            {
                managed = Marshal.GetObjectForIUnknown(pUnk);
            }
        }

        public static unsafe void Free(byte* unmanaged)
        {
            IntPtr pUnk = *(IntPtr*)unmanaged;
            if (pUnk != IntPtr.Zero)
            {
                Marshal.Release(pUnk);
            }
        }

        static unsafe nuint IArrayElementMarshaler<object?, InterfaceArrayElementMarshaler<TIsDispatch>>.UnmanagedSize => (nuint)sizeof(IntPtr);
    }

    [SupportedOSPlatform("windows")]
    internal sealed class TypedInterfaceArrayElementMarshaler<T> : IArrayElementMarshaler<T?, TypedInterfaceArrayElementMarshaler<T>>
        where T : class
    {
        public static unsafe void ConvertToUnmanaged(ref T? managed, byte* unmanaged)
        {
            if (managed is null)
            {
                *(IntPtr*)unmanaged = IntPtr.Zero;
            }
            else
            {
                *(IntPtr*)unmanaged = Marshal.GetComInterfaceForObject(managed, typeof(T));
            }
        }

        public static unsafe void ConvertToManaged(ref T? managed, byte* unmanaged)
        {
            IntPtr pUnk = *(IntPtr*)unmanaged;
            if (pUnk == IntPtr.Zero)
            {
                managed = null;
            }
            else
            {
                managed = (T)Marshal.GetObjectForIUnknown(pUnk);
            }
        }

        public static unsafe void Free(byte* unmanaged)
        {
            IntPtr pUnk = *(IntPtr*)unmanaged;
            if (pUnk != IntPtr.Zero)
            {
                Marshal.Release(pUnk);
            }
        }

        static unsafe nuint IArrayElementMarshaler<T?, TypedInterfaceArrayElementMarshaler<T>>.UnmanagedSize => (nuint)sizeof(IntPtr);
    }

    [SupportedOSPlatform("windows")]
    internal sealed class HeterogeneousInterfaceArrayElementMarshaler : IArrayElementMarshaler<object?, HeterogeneousInterfaceArrayElementMarshaler>
    {
        public static unsafe void ConvertToUnmanaged(ref object? managed, byte* unmanaged)
        {
            if (managed is null)
            {
                *(IntPtr*)unmanaged = IntPtr.Zero;
            }
            else
            {
                // Resolve the default COM interface for each element based on its runtime type.
                // This matches the heterogeneous path in MarshalInterfaceArrayComToOleHelper
                // where GetDefaultInterfaceMTForClass is called per-element.
                *(IntPtr*)unmanaged = Marshal.GetComInterfaceForObject(managed, managed.GetType());
            }
        }

        public static unsafe void ConvertToManaged(ref object? managed, byte* unmanaged)
        {
            IntPtr pUnk = *(IntPtr*)unmanaged;
            if (pUnk == IntPtr.Zero)
            {
                managed = null;
            }
            else
            {
                managed = Marshal.GetObjectForIUnknown(pUnk);
            }
        }

        public static unsafe void Free(byte* unmanaged)
        {
            IntPtr pUnk = *(IntPtr*)unmanaged;
            if (pUnk != IntPtr.Zero)
            {
                Marshal.Release(pUnk);
            }
        }

        static unsafe nuint IArrayElementMarshaler<object?, HeterogeneousInterfaceArrayElementMarshaler>.UnmanagedSize => (nuint)sizeof(IntPtr);
    }

    internal sealed class VariantArrayElementMarshaler<TNativeDataValid> : IArrayElementMarshaler<object?, VariantArrayElementMarshaler<TNativeDataValid>>
        where TNativeDataValid : IMarshalerOption
    {
        public static unsafe void ConvertToUnmanaged(ref object? managed, byte* unmanaged)
        {
            if (!TNativeDataValid.Enabled)
            {
                // Native buffer is uninitialized — zero it so ConvertToNative
                // doesn't see garbage VT_BYREF bits.
                *(ComVariant*)unmanaged = default;
            }
            // When TNativeDataValid is enabled, the existing VARIANT may have
            // VT_BYREF set. ConvertToNative checks vt & VT_BYREF and calls
            // MarshalOleRefVariantForObject to write through the byref pointer.
            ObjectMarshaler.ConvertToNative(managed!, (IntPtr)unmanaged);
        }

        public static unsafe void ConvertToManaged(ref object? managed, byte* unmanaged)
        {
            managed = ObjectMarshaler.ConvertToManaged((IntPtr)unmanaged);
        }

        public static unsafe void Free(byte* unmanaged)
        {
            ObjectMarshaler.ClearNative((IntPtr)unmanaged);
        }

        static unsafe nuint IArrayElementMarshaler<object?, VariantArrayElementMarshaler<TNativeDataValid>>.UnmanagedSize => (nuint)sizeof(ComVariant);
    }
#endif // FEATURE_COMINTEROP

    internal interface IMarshalerOption
    {
        static abstract bool Enabled { get; }

        public sealed class EnabledOption : IMarshalerOption
        {
            public static bool Enabled => true;
        }

        public sealed class DisabledOption : IMarshalerOption
        {
            public static bool Enabled => false;
        }
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
            Exception ex = Marshal.GetExceptionForHR(hr)!;
            ex.InternalPreserveStackTrace();
            return ex;
        }

#if FEATURE_COMINTEROP
        internal static unsafe Exception GetCOMHRExceptionObject(int hr, IntPtr pCPCMD, IntPtr pUnk)
        {
            Debug.Assert(pCPCMD != IntPtr.Zero);
            MethodTable* interfaceType = GetComInterfaceFromMethodDesc(pCPCMD);
            RuntimeType declaringType = RuntimeTypeHandle.GetRuntimeType(interfaceType);
            Exception ex = Marshal.GetExceptionForHR(hr, declaringType.GUID, pUnk)!;
            ex.InternalPreserveStackTrace();
            return ex;
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern unsafe MethodTable* GetComInterfaceFromMethodDesc(IntPtr pCPCMD);
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

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "StubHelpers_CreateCustomMarshaler")]
        internal static partial void CreateCustomMarshaler(IntPtr pMD, int paramToken, IntPtr hndManagedType, ObjectHandleOnStack customMarshaler);

#if FEATURE_COMINTEROP
        [SupportedOSPlatform("windows")]
        internal static object GetIEnumeratorToEnumVariantMarshaler() => EnumeratorToEnumVariantMarshaler.GetInstance(string.Empty);

        [SupportedOSPlatform("windows")]
        [UnmanagedCallersOnly]
        private static unsafe void GetIEnumeratorToEnumVariantMarshaler(object* pResult, Exception* pException)
        {
            try
            {
                *pResult = GetIEnumeratorToEnumVariantMarshaler();
            }
            catch (Exception ex)
            {
                *pException = ex;
            }
        }

        [SupportedOSPlatform("windows")]
        [UnmanagedCallersOnly]
        private static unsafe int CallICustomQueryInterface(ICustomQueryInterface* pObject, Guid* pIid, IntPtr* ppObject, Exception* pException)
        {
            try
            {
                return (int)(*pObject).GetInterface(ref *pIid, out *ppObject);
            }
            catch (Exception ex)
            {
                *pException = ex;
                return 0;
            }
        }

        [SupportedOSPlatform("windows")]
        [UnmanagedCallersOnly]
        private static unsafe void InvokeConnectionPointProviderMethod(
            object* pProvider,
            delegate*<object, object?, void> providerMethodEntryPoint,
            object* pDelegate,
            delegate*<object, object?, nint, void> delegateCtorMethodEntryPoint,
            object* pSubscriber,
            nint pEventMethodCodePtr,
            Exception* pException)
        {
            try
            {
                // Construct the delegate before invoking the provider method.
                delegateCtorMethodEntryPoint(*pDelegate, *pSubscriber, pEventMethodCodePtr);

                providerMethodEntryPoint(*pProvider, *pDelegate);
            }
            catch (Exception ex)
            {
                *pException = ex;
            }
        }
#endif

        internal static object CreateCustomMarshaler(IntPtr pMD, int paramToken, IntPtr hndManagedType)
        {
#if FEATURE_COMINTEROP
            if (OperatingSystem.IsWindows()
                && hndManagedType == typeof(System.Collections.IEnumerator).TypeHandle.Value)
            {
                return GetIEnumeratorToEnumVariantMarshaler();
            }
#endif

            object? retVal = null;
            CreateCustomMarshaler(pMD, paramToken, hndManagedType, ObjectHandleOnStack.Create(ref retVal));
            return retVal!;
        }

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
        private static extern IntPtr GetCOMIPFromRCW(object objSrc, IntPtr pCPCMD, out IntPtr ppTarget);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "StubHelpers_GetCOMIPFromRCWSlow")]
        private static partial IntPtr GetCOMIPFromRCWSlow(ObjectHandleOnStack objSrc, IntPtr pCPCMD, out IntPtr ppTarget, [MarshalAs(UnmanagedType.Bool)] out bool pfNeedsRelease);

        internal static IntPtr GetCOMIPFromRCW(object objSrc, IntPtr pCPCMD, out IntPtr ppTarget, out bool pfNeedsRelease)
        {
            IntPtr rcw = GetCOMIPFromRCW(objSrc, pCPCMD, out ppTarget);
            if (rcw != IntPtr.Zero)
            {
                pfNeedsRelease = false;
                return rcw;
            }

            // The slow path may create OLE TLS and then still resolve the interface via the RCW cache.
            // Let the slow path tell us whether it returned an owned pointer that requires cleanup.
            return GetCOMIPFromRCWWorker(objSrc, pCPCMD, out ppTarget, out pfNeedsRelease);

            [MethodImpl(MethodImplOptions.NoInlining)]
            static IntPtr GetCOMIPFromRCWWorker(object objSrc, IntPtr pCPCMD, out IntPtr ppTarget, out bool pfNeedsRelease)
                => GetCOMIPFromRCWSlow(ObjectHandleOnStack.Create(ref objSrc), pCPCMD, out ppTarget, out pfNeedsRelease);
        }
#endif // FEATURE_COMINTEROP

#if PROFILING_SUPPORTED
        //-------------------------------------------------------
        // Profiler helpers
        //-------------------------------------------------------
        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "StubHelpers_ProfilerBeginTransitionCallback")]
        internal static unsafe partial void* ProfilerBeginTransitionCallback(void* pTargetMD);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "StubHelpers_ProfilerEndTransitionCallback")]
        internal static unsafe partial void ProfilerEndTransitionCallback(void* pTargetMD);
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

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "StubHelpers_CreateLayoutClassMarshalStubs")]
        internal static unsafe partial void CreateLayoutClassMarshalStubs(QCallTypeHandle th, out delegate*<ref byte, byte*, ref CleanupWorkListElement?, void> pConvertToUnmanaged, out delegate*<ref byte, byte*, ref CleanupWorkListElement?, void> pConvertToManaged, out delegate*<ref byte, byte*, ref CleanupWorkListElement?, void> pFree);

        internal static unsafe void LayoutTypeConvertToUnmanaged(object obj, byte* pNative, ref CleanupWorkListElement? pCleanupWorkList)
        {
            RuntimeType type = (RuntimeType)obj.GetType();
            Marshal.LayoutTypeMarshalerMethods methods = Marshal.LayoutTypeMarshalerMethods.GetMarshalMethodsForType(type);

            methods.ConvertToUnmanaged(obj, pNative, ref pCleanupWorkList);
        }

        [UnmanagedCallersOnly]
        internal static unsafe void LayoutTypeConvertToUnmanaged(object* obj, byte* pNative, Exception* pException)
        {
            try
            {
                LayoutTypeConvertToUnmanaged(*obj, pNative, ref Unsafe.NullRef<CleanupWorkListElement?>());
            }
            catch (Exception ex)
            {
                *pException = ex;
            }
        }

        internal static unsafe void LayoutTypeConvertToManaged(object obj, byte* pNative)
        {
            RuntimeType type = (RuntimeType)obj.GetType();
            Marshal.LayoutTypeMarshalerMethods methods = Marshal.LayoutTypeMarshalerMethods.GetMarshalMethodsForType(type);

            methods.ConvertToManaged(obj, pNative);
        }

        [UnmanagedCallersOnly]
        internal static unsafe void LayoutTypeConvertToManaged(object* obj, byte* pNative, Exception* pException)
        {
            try
            {
                LayoutTypeConvertToManaged(*obj, pNative);
            }
            catch (Exception ex)
            {
                *pException = ex;
            }
        }

        public static unsafe void ConvertArrayContentsToUnmanaged<T, TMarshaler>(Array managed, byte* pNative, int numElements)
            where TMarshaler : IArrayMarshaler<T, TMarshaler>
        {
            // Assert that the array is actually an array of compatible type.
            Debug.Assert(managed is not null);
            Debug.Assert(managed.GetType().GetElementType()!.MakeArrayType().IsAssignableTo(typeof(T[])), $"Managed array type {managed.GetType()} is not compatible with expected element type {typeof(T)}");
            TMarshaler.ConvertContentsToUnmanaged(managed, pNative, numElements);
        }

        public static unsafe void ConvertArrayContentsToManaged<T, TMarshaler>(Array managed, byte* pNative, int numElements)
            where TMarshaler : IArrayMarshaler<T, TMarshaler>
        {
            // Assert that the array is actually an array of compatible type.
            Debug.Assert(managed is not null);
            Debug.Assert(managed.GetType().GetElementType()!.MakeArrayType().IsAssignableTo(typeof(T[])), $"Managed array type {managed.GetType()} is not compatible with expected element type {typeof(T)}");
            TMarshaler.ConvertContentsToManaged(managed, pNative, numElements);
        }

        [UnmanagedCallersOnly]
        internal static unsafe void InvokeArrayContentsConverter(
            Array* pManagedArray,
            byte* pNative,
            int numElements,
            delegate*<Array, byte*, int, void> pConvertMethod,
            Exception* pException)
        {
            try
            {
                pConvertMethod(*pManagedArray, pNative, numElements);
            }
            catch (Exception ex)
            {
                *pException = ex;
            }
        }

        internal static unsafe void FreeArrayContents<T, TMarshaler>(byte* pNative, int length) where TMarshaler : IArrayMarshaler<T, TMarshaler>
        {
            TMarshaler.FreeContents(pNative, length);
        }

        public static unsafe byte* ConvertArraySpaceToNative<T, TMarshaler>(Array? managed)
            where TMarshaler : IArrayMarshaler<T, TMarshaler>
        {
            return TMarshaler.AllocateSpaceForUnmanaged(managed);
        }

        internal static unsafe Array? ConvertArraySpaceToManaged<T, TMarshaler>(byte* pNativeHome, int cElements)
            where TMarshaler : IArrayMarshaler<T, TMarshaler>
        {
            return TMarshaler.AllocateSpaceForManaged(pNativeHome, cElements);
        }

        internal static unsafe void ClearArrayNative<T, TMarshaler>(byte* pNativeHome, int cElements)
            where TMarshaler : IArrayMarshaler<T, TMarshaler>
        {
            if (pNativeHome != null)
            {
                FreeArrayContents<T, TMarshaler>(pNativeHome, cElements);
                Marshal.FreeCoTaskMem((IntPtr)pNativeHome);
            }
        }

        internal static void ThrowWrongSizeArrayInNativeStruct()
        {
            throw new ArgumentException(SR.Argument_WrongSizeArrayInNativeStruct);
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint="StubHelpers_MarshalToManagedVaList")]
        internal static partial void MarshalToManagedVaList(IntPtr va_list, IntPtr pArgIterator);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint="StubHelpers_MarshalToUnmanagedVaList")]
        internal static partial void MarshalToUnmanagedVaList(IntPtr va_list, uint vaListSize, IntPtr pArgIterator);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern uint CalcVaListSize(IntPtr va_list);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern void LogPinnedArgument(IntPtr localDesc, IntPtr nativeArg);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint="StubHelpers_ValidateObject")]
        private static partial void ValidateObject(ObjectHandleOnStack obj, IntPtr pMD);

        internal static void ValidateObject(object obj, IntPtr pMD)
            => ValidateObject(ObjectHandleOnStack.Create(ref obj), pMD);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint="StubHelpers_ValidateByref")]
        internal static partial void ValidateByref(IntPtr byref, IntPtr pMD); // the byref is pinned so we can safely "cast" it to IntPtr

        [Intrinsic]
        internal static IntPtr GetStubContext() => throw new UnreachableException(); // Unconditionally expanded intrinsic

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void MulticastDebuggerTraceHelper(object o, int count)
        {
            MulticastDebuggerTraceHelperQCall(ObjectHandleOnStack.Create(ref o), count);
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint="StubHelpers_MulticastDebuggerTraceHelper")]
        private static partial void MulticastDebuggerTraceHelperQCall(ObjectHandleOnStack obj, int count);

        [Intrinsic]
        internal static IntPtr NextCallReturnAddress() => throw new UnreachableException(); // Unconditionally expanded intrinsic
    }  // class StubHelpers

#if FEATURE_COMINTEROP
    internal static class CultureInfoMarshaler
    {
        [UnmanagedCallersOnly]
        internal static unsafe void GetCurrentCulture(bool bUICulture, object* pResult, Exception* pException)
        {
            try
            {
                *pResult = bUICulture
                    ? Globalization.CultureInfo.CurrentUICulture
                    : Globalization.CultureInfo.CurrentCulture;
            }
            catch (Exception ex)
            {
                *pException = ex;
            }
        }

        [UnmanagedCallersOnly]
        internal static unsafe void SetCurrentCulture(bool bUICulture, Globalization.CultureInfo* pValue, Exception* pException)
        {
            try
            {
                if (bUICulture)
                    Globalization.CultureInfo.CurrentUICulture = *pValue;
                else
                    Globalization.CultureInfo.CurrentCulture = *pValue;
            }
            catch (Exception ex)
            {
                *pException = ex;
            }
        }

        [UnmanagedCallersOnly]
        internal static unsafe void CreateCultureInfo(int culture, object* pResult, Exception* pException)
        {
            try
            {
                // Consider calling CultureInfo.GetCultureInfo that returns a cached instance to avoid this expensive creation.
                *pResult = new Globalization.CultureInfo(culture);
            }
            catch (Exception ex)
            {
                *pException = ex;
            }
        }
    }

    internal static class ColorMarshaler
    {
        private static readonly MethodInvoker s_oleColorToDrawingColorMethod;
        private static readonly MethodInvoker s_drawingColorToOleColorMethod;

        internal static readonly IntPtr s_colorType;

#pragma warning disable CA1810 // explicit static cctor
        static ColorMarshaler()
        {
            Type colorTranslatorType = Type.GetType("System.Drawing.ColorTranslator, System.Drawing.Primitives", throwOnError: true)!;
            Type colorType = Type.GetType("System.Drawing.Color, System.Drawing.Primitives", throwOnError: true)!;

            s_colorType = colorType.TypeHandle.Value;

            s_oleColorToDrawingColorMethod = MethodInvoker.Create(colorTranslatorType.GetMethod("FromOle", [typeof(int)])!);
            s_drawingColorToOleColorMethod = MethodInvoker.Create(colorTranslatorType.GetMethod("ToOle", [colorType])!);
        }
#pragma warning restore CA1810 // explicit static cctor

        internal static object ConvertToManaged(int managedColor)
        {
            return s_oleColorToDrawingColorMethod.Invoke(null, managedColor)!;
        }

        internal static int ConvertToNative(object? managedColor)
        {
            return (int)s_drawingColorToOleColorMethod.Invoke(null, managedColor)!;
        }

        [UnmanagedCallersOnly]
        internal static unsafe void ConvertToManaged(int oleColor, object* pResult, Exception* pException)
        {
            try
            {
                *pResult = ConvertToManaged(oleColor);
            }
            catch (Exception ex)
            {
                *pException = ex;
            }
        }

        [UnmanagedCallersOnly]
        internal static unsafe void ConvertToNative(object* pSrcObj, int* pResult, Exception* pException)
        {
            try
            {
                *pResult = ConvertToNative(*pSrcObj);
            }
            catch (Exception ex)
            {
                *pException = ex;
            }
        }
    }
#endif
}
