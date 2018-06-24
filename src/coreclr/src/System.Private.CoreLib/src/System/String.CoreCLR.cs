// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace System
{
    public partial class String
    {
        //
        // These fields map directly onto the fields in an EE StringObject.  See object.h for the layout.
        //
        [NonSerialized] private int _stringLength;

        // For empty strings, this will be '\0' since
        // strings are both null-terminated and length prefixed
        [NonSerialized] private char _firstChar;

        // The Empty constant holds the empty string value. It is initialized by the EE during startup.
        // It is treated as intrinsic by the JIT as so the static constructor would never run.
        // Leaving it uninitialized would confuse debuggers.
        //
        // We need to call the String constructor so that the compiler doesn't mark this as a literal.
        // Marking this as a literal would mean that it doesn't show up as a field which we can access 
        // from native.
        public static readonly string Empty;

        // Gets the character at a specified position.
        //
        [System.Runtime.CompilerServices.IndexerName("Chars")]
        public extern char this[int index]
        {
            [MethodImpl(MethodImplOptions.InternalCall)]
            get;
        }

        // Gets the length of this string
        //
        // This is a EE implemented function so that the JIT can recognise it specially
        // and eliminate checks on character fetches in a loop like:
        //        for(int i = 0; i < str.Length; i++) str[i]
        // The actual code generated for this will be one instruction and will be inlined.
        //
        public extern int Length
        {
            [MethodImplAttribute(MethodImplOptions.InternalCall)]
            get;
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern string FastAllocateString(int length);

        // Is this a string that can be compared quickly (that is it has only characters > 0x80 
        // and not a - or '
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal extern bool IsFastSort();
        // Is this a string that only contains characters < 0x80.
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal extern bool IsAscii();

#if FEATURE_COMINTEROP
        // Set extra byte for odd-sized strings that came from interop as BSTR.
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal extern void SetTrailByte(byte data);
        // Try to retrieve the extra byte - returns false if not present.
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal extern bool TryGetTrailByte(out byte data);
#endif

        public static string Intern(string str)
        {
            if (str == null)
            {
                throw new ArgumentNullException(nameof(str));
            }

            return Thread.GetDomain().GetOrInternString(str);
        }

        public static string IsInterned(string str)
        {
            if (str == null)
            {
                throw new ArgumentNullException(nameof(str));
            }

            return Thread.GetDomain().IsStringInterned(str);
        }

        // Copies the source String (byte buffer) to the destination IntPtr memory allocated with len bytes.
        internal static unsafe void InternalCopy(string src, IntPtr dest, int len)
        {
            if (len == 0)
                return;
            fixed (char* charPtr = &src._firstChar)
            {
                byte* srcPtr = (byte*)charPtr;
                byte* dstPtr = (byte*)dest;
                Buffer.Memcpy(dstPtr, srcPtr, len);
            }
        }

        internal unsafe int GetBytesFromEncoding(byte* pbNativeBuffer, int cbNativeBuffer, Encoding encoding)
        {
            // encoding == Encoding.UTF8
            fixed (char* pwzChar = &_firstChar)
            {
                return encoding.GetBytes(pwzChar, Length, pbNativeBuffer, cbNativeBuffer);
            }
        }

        internal unsafe int ConvertToAnsi(byte* pbNativeBuffer, int cbNativeBuffer, bool fBestFit, bool fThrowOnUnmappableChar)
        {
            Debug.Assert(cbNativeBuffer >= (Length + 1) * Marshal.SystemMaxDBCSCharSize, "Insufficient buffer length passed to ConvertToAnsi");

            const uint CP_ACP = 0;
            int nb;

            const uint WC_NO_BEST_FIT_CHARS = 0x00000400;

            uint flgs = (fBestFit ? 0 : WC_NO_BEST_FIT_CHARS);
            uint DefaultCharUsed = 0;

            fixed (char* pwzChar = &_firstChar)
            {
                nb = Win32Native.WideCharToMultiByte(
                    CP_ACP,
                    flgs,
                    pwzChar,
                    this.Length,
                    pbNativeBuffer,
                    cbNativeBuffer,
                    IntPtr.Zero,
                    (fThrowOnUnmappableChar ? new IntPtr(&DefaultCharUsed) : IntPtr.Zero));
            }

            if (0 != DefaultCharUsed)
            {
                throw new ArgumentException(SR.Interop_Marshal_Unmappable_Char);
            }

            pbNativeBuffer[nb] = 0;
            return nb;
        }
    }
}
