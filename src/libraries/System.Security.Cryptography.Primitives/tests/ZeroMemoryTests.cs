// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Reflection;
using Xunit;

namespace System.Security.Cryptography.Primitives.Tests
{
    public static class ZeroMemoryTests
    {
        [Theory]
        [InlineData(1)]
        [InlineData(128 / 8)]
        [InlineData(256 / 8)]
        [InlineData(512 / 8)]
        [InlineData(96)]
        [InlineData(1024)]
        public static void MemoryGetsCleared(int byteLength)
        {
            byte[] rented = ArrayPool<byte>.Shared.Rent(byteLength);
            Span<byte> testSpan = new Span<byte>(rented, 0, byteLength);

            bool hasData = false;

            // i should really only iterate when byteLength is 1, and then
            // only 1/256 executions.
            //
            // The chances of this failing are 1 in 1.2e24, unless the RNG is broken.
            for (int i = 0; i < 10 && !hasData; i++)
            {
                RandomNumberGenerator.Fill(testSpan);

                for (int j = 0; j < testSpan.Length; j++)
                {
                    if (testSpan[j] != 0)
                    {
                        hasData = true;
                        break;
                    }
                }
            }

            if (!hasData)
            {
                throw new InvalidOperationException("RNG provided all zero-values");
            }

            // This test cannot guarantee the effect of the memory being cleared
            // on an otherwise abandoned reference; since the act of measuring it
            // changes what the optimizer could have done.
            //
            // But it can check for it calling clear.
            CryptographicOperations.ZeroMemory(testSpan);

            for (int i = 0; i < testSpan.Length; i++)
            {
                Assert.Equal(0, testSpan[i]);
            }
        }

        [Fact]
        public static void HasCorrectMethodImpl()
        {
            Type t = typeof(CryptographicOperations);
            MethodInfo mi = t.GetMethod(nameof(CryptographicOperations.ZeroMemory));

            // This method cannot be optimized, or the optimizer can decide that a call to Clear
            // is unnecessary.
            // It cannot be inlined, or it loses its no-optimization guarantee.
            Assert.Equal(
                MethodImplAttributes.NoInlining | MethodImplAttributes.NoOptimization,
                mi.MethodImplementationFlags);
        }
    }
}
