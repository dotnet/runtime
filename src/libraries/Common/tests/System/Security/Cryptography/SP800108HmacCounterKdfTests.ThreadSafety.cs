// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Security.Cryptography.Tests
{
    public static partial class SP800108HmacCounterKdfTests
    {
        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/68162", TestPlatforms.Browser)] // wasm threading support
        public static void Race_ReusingOneInstance_Allocating()
        {
            using (SP800108HmacCounterKdf kdf = new("kdf"u8, HashAlgorithmName.SHA256))
            {
                RaceCalls(
                    new byte[] { 0xDC, 0xD6, 0x23, 0xE8, 0x59, 0xB8, 0x4B, 0x95, 0xBF, 0x44, 0x32, 0x6E, 0x2B, 0xA6, 0x34, 0xF0 },
                    new byte[] { 0x92, 0xB0, 0xD7, 0xDA, 0x2C, 0xB1, 0xAA, 0x8C, 0xD5, 0xDF, 0x97, 0x9E, 0x61, 0xA3, 0x57, 0xD6 },
                    isDisposing: false,
                    _ => kdf.DeriveKey("label"u8, "context"u8, derivedKeyLengthInBytes: 16),
                    _ => kdf.DeriveKey("label"u8, "bananas"u8, derivedKeyLengthInBytes: 16));
            }

            using (SP800108HmacCounterKdf kdf = new("kdf"u8, HashAlgorithmName.SHA256))
            {
                RaceCalls(
                    new byte[] { 0xDC, 0xD6, 0x23, 0xE8, 0x59, 0xB8, 0x4B, 0x95, 0xBF, 0x44, 0x32, 0x6E, 0x2B, 0xA6, 0x34, 0xF0 },
                    new byte[] { 0x92, 0xB0, 0xD7, 0xDA, 0x2C, 0xB1, 0xAA, 0x8C, 0xD5, 0xDF, 0x97, 0x9E, 0x61, 0xA3, 0x57, 0xD6 },
                    isDisposing: false,
                    _ => kdf.DeriveKey("label"u8.ToArray(), "context"u8.ToArray(), derivedKeyLengthInBytes: 16),
                    _ => kdf.DeriveKey("label"u8.ToArray(), "bananas"u8.ToArray(), derivedKeyLengthInBytes: 16));
            }

            using (SP800108HmacCounterKdf kdf = new("kdf"u8, HashAlgorithmName.SHA256))
            {
                RaceCalls(
                    new byte[] { 0xDC, 0xD6, 0x23, 0xE8, 0x59, 0xB8, 0x4B, 0x95, 0xBF, 0x44, 0x32, 0x6E, 0x2B, 0xA6, 0x34, 0xF0 },
                    new byte[] { 0x92, 0xB0, 0xD7, 0xDA, 0x2C, 0xB1, 0xAA, 0x8C, 0xD5, 0xDF, 0x97, 0x9E, 0x61, 0xA3, 0x57, 0xD6 },
                    isDisposing: false,
                    _ => kdf.DeriveKey("label", "context", derivedKeyLengthInBytes: 16),
                    _ => kdf.DeriveKey("label", "bananas", derivedKeyLengthInBytes: 16));
            }

            using (SP800108HmacCounterKdf kdf = new("kdf"u8, HashAlgorithmName.SHA256))
            {
                RaceCalls(
                    new byte[] { 0xDC, 0xD6, 0x23, 0xE8, 0x59, 0xB8, 0x4B, 0x95, 0xBF, 0x44, 0x32, 0x6E, 0x2B, 0xA6, 0x34, 0xF0 },
                    new byte[] { 0x92, 0xB0, 0xD7, 0xDA, 0x2C, 0xB1, 0xAA, 0x8C, 0xD5, 0xDF, 0x97, 0x9E, 0x61, 0xA3, 0x57, 0xD6 },
                    isDisposing: false,
                    _ => kdf.DeriveKey("label".AsSpan(), "context".AsSpan(), derivedKeyLengthInBytes: 16),
                    _ => kdf.DeriveKey("label".AsSpan(), "bananas".AsSpan(), derivedKeyLengthInBytes: 16));
            }
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/68162", TestPlatforms.Browser)] // wasm threading support
        public static void Race_ReusingOneInstance_Buffering()
        {
            using (SP800108HmacCounterKdf kdf = new("kdf"u8, HashAlgorithmName.SHA256))
            {
                RaceCalls(
                    new byte[] { 0xDC, 0xD6, 0x23, 0xE8, 0x59, 0xB8, 0x4B, 0x95, 0xBF, 0x44, 0x32, 0x6E, 0x2B, 0xA6, 0x34, 0xF0 },
                    new byte[] { 0x92, 0xB0, 0xD7, 0xDA, 0x2C, 0xB1, 0xAA, 0x8C, 0xD5, 0xDF, 0x97, 0x9E, 0x61, 0xA3, 0x57, 0xD6 },
                    isDisposing: false,
                    _ => {
                        byte[] result = new byte[16];
                        kdf.DeriveKey("label"u8, "context"u8, result);
                        return result;
                    },
                    _ => {
                        byte[] result = new byte[16];
                        kdf.DeriveKey("label"u8, "bananas"u8, result);
                        return result;
                    });
            }

            using (SP800108HmacCounterKdf kdf = new("kdf"u8, HashAlgorithmName.SHA256))
            {
                RaceCalls(
                    new byte[] { 0xDC, 0xD6, 0x23, 0xE8, 0x59, 0xB8, 0x4B, 0x95, 0xBF, 0x44, 0x32, 0x6E, 0x2B, 0xA6, 0x34, 0xF0 },
                    new byte[] { 0x92, 0xB0, 0xD7, 0xDA, 0x2C, 0xB1, 0xAA, 0x8C, 0xD5, 0xDF, 0x97, 0x9E, 0x61, 0xA3, 0x57, 0xD6 },
                    isDisposing: false,
                    _ => {
                        byte[] result = new byte[16];
                        kdf.DeriveKey("label"u8.ToArray(), "context"u8.ToArray(), result);
                        return result;
                    },
                    _ => {
                        byte[] result = new byte[16];
                        kdf.DeriveKey("label"u8.ToArray(), "bananas"u8.ToArray(), result);
                        return result;
                    });
            }

            using (SP800108HmacCounterKdf kdf = new("kdf"u8, HashAlgorithmName.SHA256))
            {
                RaceCalls(
                    new byte[] { 0xDC, 0xD6, 0x23, 0xE8, 0x59, 0xB8, 0x4B, 0x95, 0xBF, 0x44, 0x32, 0x6E, 0x2B, 0xA6, 0x34, 0xF0 },
                    new byte[] { 0x92, 0xB0, 0xD7, 0xDA, 0x2C, 0xB1, 0xAA, 0x8C, 0xD5, 0xDF, 0x97, 0x9E, 0x61, 0xA3, 0x57, 0xD6 },
                    isDisposing: false,
                    _ => {
                        return kdf.DeriveKey("label", "context", 16);
                    },
                    _ => {
                        return kdf.DeriveKey("label", "bananas", 16);
                    });
            }

            using (SP800108HmacCounterKdf kdf = new("kdf"u8, HashAlgorithmName.SHA256))
            {
                RaceCalls(
                    new byte[] { 0xDC, 0xD6, 0x23, 0xE8, 0x59, 0xB8, 0x4B, 0x95, 0xBF, 0x44, 0x32, 0x6E, 0x2B, 0xA6, 0x34, 0xF0 },
                    new byte[] { 0x92, 0xB0, 0xD7, 0xDA, 0x2C, 0xB1, 0xAA, 0x8C, 0xD5, 0xDF, 0x97, 0x9E, 0x61, 0xA3, 0x57, 0xD6 },
                    isDisposing: false,
                    _ => {
                        byte[] result = new byte[16];
                        kdf.DeriveKey("label".AsSpan(), "context".AsSpan(), result);
                        return result;
                    },
                    _ => {
                        byte[] result = new byte[16];
                        kdf.DeriveKey("label".AsSpan(), "bananas".AsSpan(), result);
                        return result;
                    });
            }
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/68162", TestPlatforms.Browser)] // wasm threading support
        public static void Race_UseAndDisposeOneInstance_Allocating()
        {
            SP800108HmacCounterKdf kdf;

            kdf = new SP800108HmacCounterKdf("kdf"u8, HashAlgorithmName.SHA256);
            RaceCalls(
                new byte[] { 0xDC, 0xD6, 0x23, 0xE8, 0x59, 0xB8, 0x4B, 0x95, 0xBF, 0x44, 0x32, 0x6E, 0x2B, 0xA6, 0x34, 0xF0 },
                new byte[] { 0x92, 0xB0, 0xD7, 0xDA, 0x2C, 0xB1, 0xAA, 0x8C, 0xD5, 0xDF, 0x97, 0x9E, 0x61, 0xA3, 0x57, 0xD6 },
                isDisposing: true,
                _ => kdf.DeriveKey("label"u8, "context"u8, derivedKeyLengthInBytes: 16),
                i => {
                    if (i == 50)
                    {
                        kdf.Dispose();
                        kdf.Dispose();
                    }

                    return null;
                });

            kdf = new SP800108HmacCounterKdf("kdf"u8, HashAlgorithmName.SHA256);
            RaceCalls(
                new byte[] { 0xDC, 0xD6, 0x23, 0xE8, 0x59, 0xB8, 0x4B, 0x95, 0xBF, 0x44, 0x32, 0x6E, 0x2B, 0xA6, 0x34, 0xF0 },
                new byte[] { 0x92, 0xB0, 0xD7, 0xDA, 0x2C, 0xB1, 0xAA, 0x8C, 0xD5, 0xDF, 0x97, 0x9E, 0x61, 0xA3, 0x57, 0xD6 },
                isDisposing: true,
                _ => kdf.DeriveKey("label"u8.ToArray(), "context"u8.ToArray(), derivedKeyLengthInBytes: 16),
                i => {
                    if (i == 50)
                    {
                        kdf.Dispose();
                        kdf.Dispose();
                    }

                    return null;
                });

            kdf = new SP800108HmacCounterKdf("kdf"u8, HashAlgorithmName.SHA256);
            RaceCalls(
                new byte[] { 0xDC, 0xD6, 0x23, 0xE8, 0x59, 0xB8, 0x4B, 0x95, 0xBF, 0x44, 0x32, 0x6E, 0x2B, 0xA6, 0x34, 0xF0 },
                new byte[] { 0x92, 0xB0, 0xD7, 0xDA, 0x2C, 0xB1, 0xAA, 0x8C, 0xD5, 0xDF, 0x97, 0x9E, 0x61, 0xA3, 0x57, 0xD6 },
                isDisposing: true,
                _ => kdf.DeriveKey("label", "context", derivedKeyLengthInBytes: 16),
                i => {
                    if (i == 50)
                    {
                        kdf.Dispose();
                        kdf.Dispose();
                    }

                    return null;
                });

            kdf = new SP800108HmacCounterKdf("kdf"u8, HashAlgorithmName.SHA256);
            RaceCalls(
                new byte[] { 0xDC, 0xD6, 0x23, 0xE8, 0x59, 0xB8, 0x4B, 0x95, 0xBF, 0x44, 0x32, 0x6E, 0x2B, 0xA6, 0x34, 0xF0 },
                new byte[] { 0x92, 0xB0, 0xD7, 0xDA, 0x2C, 0xB1, 0xAA, 0x8C, 0xD5, 0xDF, 0x97, 0x9E, 0x61, 0xA3, 0x57, 0xD6 },
                isDisposing: true,
                _ => kdf.DeriveKey("label".AsSpan(), "context".AsSpan(), derivedKeyLengthInBytes: 16),
                i => {
                    if (i == 50)
                    {
                        kdf.Dispose();
                        kdf.Dispose();
                    }

                    return null;
                });
        }
    }
}
