// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Text;

namespace System
{
    public partial class String
    {
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern string FastAllocateString(int length);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "String_Intern")]
        private static partial void Intern(StringHandleOnStack src);

        public static string Intern(string str)
        {
            ArgumentNullException.ThrowIfNull(str);
            Intern(new StringHandleOnStack(ref str!));
            return str!;
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "String_IsInterned")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial void IsInterned(StringHandleOnStack src);

        public static string? IsInterned(string str)
        {
            ArgumentNullException.ThrowIfNull(str);
            Intern(new StringHandleOnStack(ref str!));
            return str;
        }

        // Copies the source String (byte buffer) to the destination IntPtr memory allocated with len bytes.
        // Used by ilmarshalers.cpp
        internal static unsafe void InternalCopy(string src, IntPtr dest, int len)
        {
            if (len != 0)
            {
                Buffer.Memmove(ref *(byte*)dest, ref Unsafe.As<char, byte>(ref src.GetRawStringData()), (nuint)len);
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
