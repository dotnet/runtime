// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using System.Text;

namespace System
{
    public partial class String
    {
        // The Empty constant holds the empty string value. It is initialized by the EE during startup.
        // It is treated as intrinsic by the JIT as so the static constructor would never run.
        // Leaving it uninitialized would confuse debuggers.
        //
        // We need to call the String constructor so that the compiler doesn't mark this as a literal.
        // Marking this as a literal would mean that it doesn't show up as a field which we can access
        // from native.
#pragma warning disable CS8618 // compiler sees this non-nullable static string as uninitialized
        [Intrinsic]
        public static readonly string Empty;
#pragma warning restore CS8618

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
            [MethodImpl(MethodImplOptions.InternalCall)]
            get;
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern string FastAllocateString(int length);

        // Set extra byte for odd-sized strings that came from interop as BSTR.
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal extern void SetTrailByte(byte data);
        // Try to retrieve the extra byte - returns false if not present.
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal extern bool TryGetTrailByte(out byte data);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern string Intern();
        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern string? IsInterned();

        public static string Intern(string str)
        {
            if (str == null)
            {
                throw new ArgumentNullException(nameof(str));
            }

            return str.Intern();
        }

        public static string? IsInterned(string str)
        {
            if (str == null)
            {
                throw new ArgumentNullException(nameof(str));
            }

            return str.IsInterned();
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
    }
}
