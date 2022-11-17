// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices.Marshalling
{
    /// <summary>
    /// Marshaller for UTF-16 strings.
    /// </summary>
    [CLSCompliant(false)]
    [CustomMarshaller(typeof(string), MarshalMode.Default, typeof(Utf16StringMarshaller))]
    public static unsafe class Utf16StringMarshaller
    {
        /// <summary>
        /// Converts a string to an unmanaged version.
        /// </summary>
        /// <param name="managed">The managed string to convert.</param>
        /// <returns>An unmanaged string.</returns>
        public static ushort* ConvertToUnmanaged(string? managed)
            => (ushort*)Marshal.StringToCoTaskMemUni(managed);

        /// <summary>
        /// Converts an unmanaged string to a managed version.
        /// </summary>
        /// <param name="unmanaged">The unmanaged string to convert.</param>
        /// <returns>A managed string.</returns>
        public static string? ConvertToManaged(ushort* unmanaged)
            => Marshal.PtrToStringUni((IntPtr)unmanaged);

        /// <summary>
        /// Frees the memory for the unmanaged string.
        /// </summary>
        /// <param name="unmanaged">The memory allocated for the unmanaged string.</param>
        public static void Free(ushort* unmanaged)
            => Marshal.FreeCoTaskMem((IntPtr)unmanaged);

        /// <summary>
        /// Gets a pinnable reference for the specified string.
        /// </summary>
        /// <param name="str">The string to get a reference for.</param>
        /// <returns>A pinnable reference.</returns>
        public static ref readonly char GetPinnableReference(string? str)
            => ref str is null ? ref *(char*)0 : ref str.GetPinnableReference();
    }
}
