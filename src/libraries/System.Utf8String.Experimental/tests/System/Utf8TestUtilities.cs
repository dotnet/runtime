// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
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

        public unsafe static bool IsNull(this Utf8Span span)
        {
            return Unsafe.AreSame(ref Unsafe.AsRef<byte>(null), ref MemoryMarshal.GetReference(span.Bytes));
        }

        /// <summary>
        /// Parses an expression of the form "a..b" and returns a <see cref="Range"/>.
        /// </summary>
        public static Range ParseRangeExpr(string expression) => ParseRangeExpr(expression.AsSpan());

        /// <summary>
        /// Parses an expression of the form "a..b" and returns a <see cref="Range"/>.
        /// </summary>
        public static Range ParseRangeExpr(ReadOnlySpan<char> expression)
        {
            int idxOfDots = expression.IndexOf("..".AsSpan(), StringComparison.Ordinal);
            if (idxOfDots < 0)
            {
                goto Error;
            }

            ReadOnlySpan<char> firstPart = expression[..idxOfDots].Trim();
            Index firstIndex = Index.Start;

            if (!firstPart.IsWhiteSpace())
            {
                bool fromEnd = false;

                if (!firstPart.IsEmpty && firstPart[0] == '^')
                {
                    fromEnd = true;
                    firstPart = firstPart[1..];
                }

                if (!int.TryParse(firstPart.ToString(), NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite, CultureInfo.InvariantCulture, out int startIndex))
                {
                    goto Error;
                }

                firstIndex = new Index(startIndex, fromEnd);
            }

            ReadOnlySpan<char> secondPart = expression[(idxOfDots + 2)..].Trim();
            Index secondIndex = Index.End;

            if (!secondPart.IsWhiteSpace())
            {
                bool fromEnd = false;

                if (!secondPart.IsEmpty && secondPart[0] == '^')
                {
                    fromEnd = true;
                    secondPart = secondPart[1..];
                }

                if (!int.TryParse(secondPart.ToString(), NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite, CultureInfo.InvariantCulture, out int endIndex))
                {
                    goto Error;
                }

                secondIndex = new Index(endIndex, fromEnd);
            }

            return new Range(firstIndex, secondIndex);

        Error:
            throw new ArgumentException($"Range expression '{expression.ToString()}' is invalid.");
        }

        public static void AssertRangesEqual(int originalLength, Range expected, Range actual)
        {
            Assert.Equal(expected, actual, new RangeEqualityComparer(originalLength));
        }

        /// <summary>
        /// Runs this test on its own dedicated thread; allows for setting CurrentCulture and other thread-statics.
        /// </summary>
        /// <param name="testCode"></param>
        public static void RunOnDedicatedThread(Action testCode)
        {
            Assert.NotNull(testCode);

            ExceptionDispatchInfo edi = default;
            Thread newThread = new Thread(() =>
            {
                try
                {
                    testCode();
                }
                catch (Exception ex)
                {
                    edi = ExceptionDispatchInfo.Capture(ex);
                }
            });

            newThread.Start();
            newThread.Join();

            if (edi != null)
            {
                edi.Throw();
            }
        }

        /// <summary>
        /// Mimics returning a literal <see cref="Utf8String"/> instance.
        /// </summary>
        public static Utf8String u8(string str)
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

            byte[] utf8Bytes = new byte[4]; // 4 UTF-8 code units is the largest any scalar value can be encoded as

            int index = 0;
            while (index < str.Length)
            {
                if (Rune.TryGetRuneAt(str, index, out Rune value) && value.TryEncodeToUtf8(utf8Bytes, out int bytesWritten))
                {
                    memStream.Write(utf8Bytes, 0, bytesWritten);
                    index += value.Utf16SequenceLength;
                }
                else
                {
                    throw new ArgumentException($"String '{str}' is not a well-formed UTF-16 string.");
                }
            }

            Assert.True(memStream.TryGetBuffer(out ArraySegment<byte> buffer));

            // Now allocate a UTF-8 string instance and set this as the contents.

            return Utf8String.UnsafeCreateWithoutValidation(buffer);
        }

        public unsafe static Range GetRangeOfSubspan<T>(ReadOnlySpan<T> outerSpan, ReadOnlySpan<T> innerSpan)
        {
            ulong byteOffset = (ulong)(void*)Unsafe.ByteOffset(ref MemoryMarshal.GetReference(outerSpan), ref MemoryMarshal.GetReference(innerSpan));
            ulong elementOffset = byteOffset / (uint)Unsafe.SizeOf<T>();

            checked
            {
                int elementOffsetAsInt = (int)elementOffset;
                Range retVal = elementOffsetAsInt..(elementOffsetAsInt + innerSpan.Length);

                _ = outerSpan[retVal]; // call the real slice logic to make sure we're really within the outer span
                return retVal;
            }
        }

        public static Range GetRangeOfSubspan(Utf8Span outerSpan, Utf8Span innerSpan)
        {
            return GetRangeOfSubspan(outerSpan.Bytes, innerSpan.Bytes);
        }

        public static bool IsEmpty(this Range range, int length)
        {
            (_, int actualLength) = range.GetOffsetAndLength(length);
            return (actualLength == 0);
        }

        public static bool IsTryFindSupported(StringComparison comparison) =>
            !PlatformDetection.IsNetFramework ||
                comparison == StringComparison.Ordinal ||
                comparison == StringComparison.OrdinalIgnoreCase;
    }
}
