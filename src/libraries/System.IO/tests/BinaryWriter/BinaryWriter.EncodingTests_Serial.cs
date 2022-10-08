// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;

namespace System.IO.Tests
{
    // WriteChars_VeryLargeArray_DoesNotOverflow allocates a lot of memory and can cause OOM,
    // it should not be executed in parallel with other tests
    [Collection(nameof(DisableParallelization))]
    public class BinaryWriter_EncodingTests_Serial
    {
        [OuterLoop("Allocates a lot of memory")]
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.Is64BitProcess))]
        [SkipOnPlatform(TestPlatforms.Android, "OOM on Android could be uncatchable & kill the test runner")]
        public unsafe void WriteChars_VeryLargeArray_DoesNotOverflow()
        {
            const nuint INT32_OVERFLOW_SIZE = (nuint)int.MaxValue + 3;

            SafeBuffer unmanagedBuffer = null;
            try
            {
                try
                {
                    unmanagedBuffer = SafeBufferUtil.CreateSafeBuffer(INT32_OVERFLOW_SIZE * sizeof(byte));
                }
                catch (OutOfMemoryException)
                {
                    throw new SkipTestException($"Unable to execute {nameof(WriteChars_VeryLargeArray_DoesNotOverflow)} due to OOM"); // skip test in low-mem conditions
                }

                Assert.True((long)unmanagedBuffer.ByteLength > int.MaxValue);

                // reuse same memory for input and output to avoid allocating more memory and OOMs
                Span<char> span = new Span<char>((char*)unmanagedBuffer.DangerousGetHandle(), (int)(INT32_OVERFLOW_SIZE / sizeof(char)));
                span.Fill('\u0224'); // LATIN CAPITAL LETTER Z WITH HOOK
                Stream outStream = new UnmanagedMemoryStream(unmanagedBuffer, 0, (long)unmanagedBuffer.ByteLength, FileAccess.ReadWrite);
                BinaryWriter writer = new BinaryWriter(outStream);

                writer.Write(span); // will write slightly more than int.MaxValue bytes to the output

                Assert.Equal((long)INT32_OVERFLOW_SIZE, outStream.Position);
            }
            finally
            {
                unmanagedBuffer?.Dispose();
            }
        }
    }
}
