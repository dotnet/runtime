// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Reflection;
using System.Text;
using Xunit;

namespace System.Tests
{
    public static class Utf8TestUtilities
    {
        private static readonly Lazy<Func<int, Utf8String>> _utf8StringFactory = CreateUtf8StringFactory();

        private static Lazy<Func<int, Utf8String>> CreateUtf8StringFactory()
        {
            return new Lazy<Func<int, Utf8String>>(() =>
            {
                MethodInfo fastAllocateMethod = typeof(Utf8String).GetMethod("FastAllocate", BindingFlags.NonPublic | BindingFlags.Static, null, new[] { typeof(int) }, null);
                Assert.NotNull(fastAllocateMethod);
                return (Func<int, Utf8String>)fastAllocateMethod.CreateDelegate(typeof(Func<int, Utf8String>));
            });
        }

        /// <summary>
        /// Mimics returning a literal <see cref="Utf8String"/> instance.
        /// </summary>
        public unsafe static Utf8String u8(string str)
        {
            if (str is null)
            {
                return null;
            }
            else if (str.Length == 0)
            {
                return Utf8String.Empty;
            }

            // First, transcode UTF-16 to UTF-8. We use direct by-scalar transcoding here
            // because we have good reference implementation tests for this and it'll help
            // catch any errors we introduce to our bulk transcoding implementations.

            MemoryStream memStream = new MemoryStream();

            Span<byte> utf8Bytes = stackalloc byte[4]; // 4 UTF-8 code units is the largest any scalar value can be encoded as

            int index = 0;
            while (index < str.Length)
            {
                if (Rune.TryGetRuneAt(str, index, out Rune value) && value.TryEncodeToUtf8(utf8Bytes, out int bytesWritten))
                {
                    memStream.Write(utf8Bytes.Slice(0, bytesWritten));
                    index += value.Utf16SequenceLength;
                }
                else
                {
                    throw new ArgumentException($"String '{str}' is not a well-formed UTF-16 string.");
                }
            }

            Assert.True(memStream.TryGetBuffer(out ArraySegment<byte> buffer));

            // Now allocate a UTF-8 string instance and set this as the contents.
            // We do it this way rather than go through a public ctor because we don't
            // want the "control" part of our unit tests to depend on the code under test.

            Utf8String newUtf8String = _utf8StringFactory.Value(buffer.Count);
            fixed (byte* pNewUtf8String = newUtf8String)
            {
                buffer.AsSpan().CopyTo(new Span<byte>(pNewUtf8String, newUtf8String.Length));
            }

            return newUtf8String;
        }
    }
}
