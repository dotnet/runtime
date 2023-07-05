// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using Xunit;

namespace System.Security.Cryptography.Tests
{
    public static partial class SP800108HmacCounterKdfTests
    {
        private const string Label = "label";
        private const string Context = "contextHeadercontext";
        private static readonly byte[] s_labelBytes = "label"u8.ToArray();
        private static readonly byte[] s_kdk = "kdk"u8.ToArray();
        private static readonly byte[] s_contextBytes = "contextHeadercontext"u8.ToArray();
        private static readonly HashAlgorithmName s_unknownHash = HashAlgorithmName.MD5;
        private static readonly HashAlgorithmName s_nullHash = new HashAlgorithmName(null);
        private static readonly HashAlgorithmName s_emptyHash = new HashAlgorithmName("");

        private static void VerifyKbkdfBytes(byte[] expected, byte[] key, HashAlgorithmName hashAlgorithm, byte[] labelBytes, byte[] contextBytes)
        {
            byte[] result;

            using (SP800108HmacCounterKdf kdf = new SP800108HmacCounterKdf(key, hashAlgorithm))
            {
                result = kdf.DeriveKey(labelBytes, contextBytes, expected.Length);
                Assert.Equal(expected, result);
            }

            using (SP800108HmacCounterKdf kdf = new SP800108HmacCounterKdf(key, hashAlgorithm))
            {
                result = kdf.DeriveKey(new ReadOnlySpan<byte>(labelBytes), new ReadOnlySpan<byte>(contextBytes), expected.Length);
                Assert.Equal(expected, result);
            }

            using (SP800108HmacCounterKdf kdf = new SP800108HmacCounterKdf(key, hashAlgorithm))
            {
                result = new byte[expected.Length];
                kdf.DeriveKey(new ReadOnlySpan<byte>(labelBytes), new ReadOnlySpan<byte>(contextBytes), result);
                Assert.Equal(expected, result);
            }

            result = SP800108HmacCounterKdf.DeriveBytes(
                key,
                hashAlgorithm,
                labelBytes,
                contextBytes,
                expected.Length);
            Assert.Equal(expected, result);

            result = SP800108HmacCounterKdf.DeriveBytes(
                new ReadOnlySpan<byte>(key),
                hashAlgorithm,
                new ReadOnlySpan<byte>(labelBytes),
                new ReadOnlySpan<byte>(contextBytes),
                expected.Length);
            Assert.Equal(expected, result);

            result = new byte[expected.Length];
            SP800108HmacCounterKdf.DeriveBytes(
                new ReadOnlySpan<byte>(key),
                hashAlgorithm,
                new ReadOnlySpan<byte>(labelBytes),
                new ReadOnlySpan<byte>(contextBytes),
                result);
            Assert.Equal(expected, result);
        }

        private static void VerifyKbkdf(byte[] expected, byte[] key, HashAlgorithmName hashAlgorithm, char[] label, char[] context)
        {
            // The actual implementation uses a stricter UTF8 encoding/decoding but we know our test data does not contain
            // invalid UTF8.
            byte[] labelBytes = System.Text.Encoding.UTF8.GetBytes(label);
            byte[] contextBytes = System.Text.Encoding.UTF8.GetBytes(context);
            byte[] result;

            VerifyKbkdfBytes(expected, key, hashAlgorithm, labelBytes, contextBytes);

            using (SP800108HmacCounterKdf kdf = new SP800108HmacCounterKdf(key, hashAlgorithm))
            {
                result = kdf.DeriveKey(new ReadOnlySpan<char>(label), new ReadOnlySpan<char>(context), expected.Length);
                Assert.Equal(expected, result);
            }

            using (SP800108HmacCounterKdf kdf = new SP800108HmacCounterKdf(key, hashAlgorithm))
            {
                result = new byte[expected.Length];
                kdf.DeriveKey(new ReadOnlySpan<char>(label), new ReadOnlySpan<char>(context), result);
                Assert.Equal(expected, result);
            }

            using (SP800108HmacCounterKdf kdf = new SP800108HmacCounterKdf(key, hashAlgorithm))
            {
                result = kdf.DeriveKey(new string(label), new string(context), expected.Length);
                Assert.Equal(expected, result);
            }

            result = SP800108HmacCounterKdf.DeriveBytes(
                key,
                hashAlgorithm,
                new string(label),
                new string(context),
                expected.Length);
            Assert.Equal(expected, result);

            result = SP800108HmacCounterKdf.DeriveBytes(
                new ReadOnlySpan<byte>(key),
                hashAlgorithm,
                new ReadOnlySpan<char>(label),
                new ReadOnlySpan<char>(context),
                expected.Length);
            Assert.Equal(expected, result);

            result = new byte[expected.Length];
            SP800108HmacCounterKdf.DeriveBytes(
                new ReadOnlySpan<byte>(key),
                hashAlgorithm,
                new ReadOnlySpan<char>(label),
                new ReadOnlySpan<char>(context),
                result);
            Assert.Equal(expected, result);
        }

        private static void RaceCalls(byte[] expected1, byte[] expected2, bool isDisposing, Func<int, byte[]> call1, Func<int, byte[]> call2)
        {
            const int Iterations = 1_000;

            void ThreadCallback(object state)
            {
                (Func<int, byte[]> act, byte[] expected) = ((Func<int, byte[]>, byte[]))state;
                byte[][] results = new byte[Iterations][];

                for (int i = 0; i < Iterations; i++)
                {
                    // defer asserting until after the loop so that the assert doesn't dominate the work of the
                    // threads interacting.
                    try
                    {
                        results[i] = act(i);
                    }
                    catch (ObjectDisposedException) when (isDisposing)
                    {
                        results[i] = null;
                    }
                }

                for (int i = 0; i < Iterations; i++)
                {
                    byte[] result = results[i];

                    if (result is not null)
                    {
                        Assert.Equal(expected, result);
                    }
                }
            }

            Thread t1 = new Thread(ThreadCallback);
            Thread t2 = new Thread(ThreadCallback);

            t1.Start((call1, expected1));
            t2.Start((call2, expected2));
            t1.Join();
            t2.Join();
        }
    }
}
