// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

#if !NETCOREAPP
using System.Buffers;
#endif

namespace System.IO
{
    internal static class TextWriterExtensions
    {
        /// <summary>
        /// Writes a partial string (given offset and count) to the underlying TextWriter.
        /// </summary>
        public static void WritePartialString(this TextWriter writer, string value, int offset, int count)
        {
            Debug.Assert(writer != null);
            Debug.Assert(value != null);

            if (offset == 0 && count == value.Length)
            {
                // on all platforms, prefer TextWriter.Write(string) if no slicing is required
                writer.Write(value);
            }
            else
            {
                // if slicing is required, call TextWriter.Write(ROS<char>) if available;
                // otherwise rent an array and implement the Write routine ourselves
                ReadOnlySpan<char> sliced = value.AsSpan(offset, count);
#if NETCOREAPP
                writer.Write(sliced);
#else
                char[] rented = ArrayPool<char>.Shared.Rent(sliced.Length);
                sliced.CopyTo(rented);
                writer.Write(rented, 0, sliced.Length);
                ArrayPool<char>.Shared.Return(rented);
#endif
            }
        }
    }
}
