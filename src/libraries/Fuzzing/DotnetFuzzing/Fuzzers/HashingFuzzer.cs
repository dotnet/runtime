// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.IO.Hashing;

namespace DotnetFuzzing.Fuzzers;

/// <summary>
/// Differential fuzzer for System.IO.Hashing. CRC-32/CRC-64 (well-known and fuzzer-chosen parameter sets) and Adler-32 are
/// compared with straightforward reference implementations (the Rocksoft CRC model and the RFC 1950 Adler-32 definition);
/// every algorithm, including the XxHash family, must give the same result one-shot, through the static byte APIs, and through
/// the instance API fed in fuzzer-chosen chunks with a Clone() along the way. Inputs can be tiled to megabytes so the vectorized
/// paths and the Adler-32 overflow limits get exercised.
/// </summary>
/// <remarks>
/// Input layout: [0] algorithm, [1..8] CRC polynomial, [9..16] initial value, [17..24] final xor, [25] flags (bit0 reflect,
/// bit1 well-known set, bit2 tile), [26] chunk seed, [27] tile factor, [28..] data.
/// </remarks>
internal sealed class HashingFuzzer : IFuzzer
{
    private const int HeaderSize = 28;

    // Known issues on main, tolerated unless TENSOR_FUZZ_STRICT=1 (shared switch name with the other new fuzzers):
    // * Crc32ParameterSet/Crc64ParameterSet.Create(..., reflectValues: true) load the initial value into the reflected register
    //   as-is, while the Rocksoft/reveng CRC model defines it unreflected; results differ for non-palindromic initial values.
    private static readonly bool s_strict = Environment.GetEnvironmentVariable("TENSOR_FUZZ_STRICT") == "1";

    public string[] TargetAssemblies { get; } = ["System.IO.Hashing"];
    public string[] TargetCoreLibPrefixes => [];

    private static bool s_catalogueChecked;

    // Runs on the first input rather than in a static constructor: the harness creates every fuzzer before libFuzzer maps the
    // SharpFuzz coverage memory, and instrumented code must not run before that.
    private static void CheckCatalogue()
    {
        // Pin the reference model to the CRC catalogue (check value = CRC of "123456789"), and .NET's well-known sets to it.
        byte[] check = "123456789"u8.ToArray();
        CheckValue(Crc32Reference(0x04C11DB7, 0xFFFFFFFF, 0xFFFFFFFF, true, check) == 0xCBF43926, "reference CRC-32");
        CheckValue(Crc32Reference(0x1EDC6F41, 0xFFFFFFFF, 0xFFFFFFFF, true, check) == 0xE3069283, "reference CRC-32C");
        CheckValue(Crc32Reference(0x04C11DB7, 0xFFFFFFFF, 0x00000000, false, check) == 0x0376E6E7, "reference CRC-32/MPEG-2");
        CheckValue(Crc32Reference(0x04C11DB7, 0x00000000, 0xFFFFFFFF, false, check) == 0x765E7680, "reference CRC-32/CKSUM");
        CheckValue(Crc64Reference(0x42F0E1EBA9EA3693, 0, 0, false, check) == 0x6C40DF5F0B497347, "reference CRC-64/ECMA-182");
        CheckValue(Crc64Reference(0x42F0E1EBA9EA3693, ulong.MaxValue, ulong.MaxValue, true, check) == 0x995DC9BBDF1939FA, "reference CRC-64/XZ");
        CheckValue(AdlerReference(check) == 0x091E01DE, "reference Adler-32");

        CheckValue(Crc32.HashToUInt32(check) == 0xCBF43926, "Crc32.HashToUInt32(\"123456789\")");
        CheckValue(Crc64.HashToUInt64(check) == 0x6C40DF5F0B497347, "Crc64.HashToUInt64(\"123456789\")");
        foreach ((uint poly, uint init, uint xor, bool reflect, uint expected, string name) in new[]
        {
            (0x04C11DB7u, 0xFFFFFFFFu, 0x00000000u, false, 0x0376E6E7u, "CRC-32/MPEG-2"),
            (0x04C11DB7u, 0x00000000u, 0xFFFFFFFFu, false, 0x765E7680u, "CRC-32/CKSUM"),
            (0x04C11DB7u, 0xFFFFFFFFu, 0xFFFFFFFFu, false, 0xFC891918u, "CRC-32/BZIP2"),
            (0x04C11DB7u, 0xFFFFFFFFu, 0x00000000u, true, 0x340BC6D9u, "CRC-32/JAMCRC"),
            (0x814141ABu, 0x00000000u, 0x00000000u, false, 0x3010BF7Fu, "CRC-32/AIXM"),
            (0xA833982Bu, 0xFFFFFFFFu, 0xFFFFFFFFu, true, 0x87315576u, "CRC-32/BASE91-D"),
        })
        {
            var crc = new Crc32(Crc32ParameterSet.Create(poly, init, xor, reflect));
            crc.Append(check);
            CheckValue(crc.GetCurrentHashAsUInt32() == expected, $"Crc32ParameterSet {name}: got 0x{crc.GetCurrentHashAsUInt32():X8}, catalogue 0x{expected:X8}");
        }
    }

    public void FuzzTarget(ReadOnlySpan<byte> bytes)
    {
        if (!s_catalogueChecked)
        {
            CheckCatalogue();
            s_catalogueChecked = true;
        }

        if (bytes.Length < HeaderSize)
        {
            return;
        }

        ulong poly = BinaryPrimitives.ReadUInt64LittleEndian(bytes.Slice(1)) | 1; // CRC polynomials have the x^0 term
        ulong init = BinaryPrimitives.ReadUInt64LittleEndian(bytes.Slice(9));
        ulong xor = BinaryPrimitives.ReadUInt64LittleEndian(bytes.Slice(17));
        byte flags = bytes[25];
        uint chunkSeed = bytes[26] * 2654435761u + 1;
        bool reflect = (flags & 1) != 0, wellKnown = (flags & 2) != 0;

        byte[] data = bytes.Slice(HeaderSize).ToArray();
        if ((flags & 4) != 0 && data.Length > 0)
        {
            // Tile up to ~1 MB, e.g. to push Adler-32 lanes past the 5552-byte NMAX blocking.
            int length = Math.Min(data.Length * (1 + bytes[27] * 16), 1 << 20);
            byte[] tiled = new byte[length];
            for (int i = 0; i < length; i++)
            {
                tiled[i] = data[i % data.Length];
            }

            data = tiled;
        }

        switch (bytes[0] % 7)
        {
            case 0:
            {
                Crc32ParameterSet set = wellKnown ? ((flags & 8) != 0 ? Crc32ParameterSet.Crc32C : Crc32ParameterSet.Crc32) : Crc32ParameterSet.Create((uint)poly, (uint)init, (uint)xor, reflect);
                uint referenceInit = set.ReflectValues && !s_strict ? ReverseBits(set.InitialValue) : set.InitialValue;
                uint expected = Crc32Reference(set.Polynomial, referenceInit, set.FinalXorValue, set.ReflectValues, data);
                var crc = new Crc32(set);
                CheckStreaming(crc, data, chunkSeed, () => crc.Clone(), c => ((Crc32)c).GetCurrentHashAsUInt32(), expected, $"Crc32(poly 0x{set.Polynomial:X8}, init 0x{set.InitialValue:X8}, xor 0x{set.FinalXorValue:X8}, reflect {set.ReflectValues})");
                CheckValue(Crc32.Hash(set, data).AsSpan().SequenceEqual(crc.GetCurrentHash()), "Crc32.Hash(set, data) differs from the instance hash bytes");
                break;
            }

            case 1:
            {
                Crc64ParameterSet set = wellKnown ? ((flags & 8) != 0 ? Crc64ParameterSet.Nvme : Crc64ParameterSet.Crc64) : Crc64ParameterSet.Create(poly, init, xor, reflect);
                ulong referenceInit = set.ReflectValues && !s_strict ? ReverseBits(set.InitialValue) : set.InitialValue;
                ulong expected = Crc64Reference(set.Polynomial, referenceInit, set.FinalXorValue, set.ReflectValues, data);
                var crc = new Crc64(set);
                CheckStreaming(crc, data, chunkSeed, () => crc.Clone(), c => ((Crc64)c).GetCurrentHashAsUInt64(), expected, $"Crc64(poly 0x{set.Polynomial:X16}, init 0x{set.InitialValue:X16}, xor 0x{set.FinalXorValue:X16}, reflect {set.ReflectValues})");
                CheckValue(Crc64.Hash(set, data).AsSpan().SequenceEqual(crc.GetCurrentHash()), "Crc64.Hash(set, data) differs from the instance hash bytes");
                break;
            }

            case 2:
            {
                uint expected = AdlerReference(data);
                var adler = new Adler32();
                CheckStreaming(adler, data, chunkSeed, () => adler.Clone(), a => ((Adler32)a).GetCurrentHashAsUInt32(), expected, "Adler32");
                CheckValue(Adler32.HashToUInt32(data) == expected, $"Adler32.HashToUInt32 = 0x{Adler32.HashToUInt32(data):X8}, expected 0x{expected:X8} (length {data.Length})");
                break;
            }

            case 3:
            {
                var xx = new XxHash32((int)init);
                ulong expected = XxHash32.HashToUInt32(data, (int)init);
                CheckStreaming(xx, data, chunkSeed, () => xx.Clone(), x => ((XxHash32)x).GetCurrentHashAsUInt32(), expected, "XxHash32");
                break;
            }

            case 4:
            {
                var xx = new XxHash64((long)init);
                ulong expected = XxHash64.HashToUInt64(data, (long)init);
                CheckStreaming(xx, data, chunkSeed, () => xx.Clone(), x => ((XxHash64)x).GetCurrentHashAsUInt64(), expected, "XxHash64");
                break;
            }

            case 5:
            {
                var xx = new XxHash3((long)init);
                ulong expected = XxHash3.HashToUInt64(data, (long)init);
                CheckStreaming(xx, data, chunkSeed, () => xx.Clone(), x => ((XxHash3)x).GetCurrentHashAsUInt64(), expected, "XxHash3");
                break;
            }

            default:
            {
                var xx = new XxHash128((long)init);
                UInt128 value = XxHash128.HashToUInt128(data, (long)init);
                ulong expected = (ulong)value ^ (ulong)(value >> 64);
                CheckStreaming(xx, data, chunkSeed, () => xx.Clone(), x => { UInt128 v = ((XxHash128)x).GetCurrentHashAsUInt128(); return (ulong)v ^ (ulong)(v >> 64); }, expected, "XxHash128");
                break;
            }
        }
    }

    /// <summary>
    /// Feeds the data in pseudo-random chunks; halfway through, a clone is taken and fed the rest separately. Both must end
    /// at the expected value, and Reset + one Append must too.
    /// </summary>
    private static void CheckStreaming(NonCryptographicHashAlgorithm algorithm, byte[] data, uint seed, Func<NonCryptographicHashAlgorithm> clone, Func<NonCryptographicHashAlgorithm, ulong> value, ulong expected, string name)
    {
        ReadOnlySpan<byte> rest = data;
        NonCryptographicHashAlgorithm? copy = null;
        while (!rest.IsEmpty)
        {
            seed = seed * 1103515245 + 12345;
            int size = (int)((seed >> 16) % 4) switch
            {
                0 => (int)(seed >> 20) % 16,
                1 => (int)(seed >> 20) % 256,
                2 => (int)(seed >> 20) % 4096,
                _ => rest.Length,
            };
            size = Math.Min(size, rest.Length);
            algorithm.Append(rest.Slice(0, size));
            rest = rest.Slice(size);
            if (copy is null && rest.Length <= data.Length / 2)
            {
                copy = clone();
                copy.Append(rest);
            }
        }

        ulong actual = value(algorithm);
        CheckValue(actual == expected, $"{name} over {data.Length} bytes fed in chunks = 0x{actual:X}, expected 0x{expected:X}");
        if (copy is not null)
        {
            CheckValue(value(copy) == expected, $"{name}: a clone taken mid-stream ends at 0x{value(copy):X}, expected 0x{expected:X}");
        }

        algorithm.Reset();
        algorithm.Append(data);
        CheckValue(value(algorithm) == expected, $"{name} after Reset() = 0x{value(algorithm):X}, expected 0x{expected:X}");
    }

    private static uint Crc32Reference(uint poly, uint init, uint xor, bool reflect, ReadOnlySpan<byte> data)
    {
        // Rocksoft model, MSB-first register; reflection applied to input bytes and to the final register.
        uint[] table = new uint[256];
        for (uint i = 0; i < 256; i++)
        {
            uint r = i << 24;
            for (int k = 0; k < 8; k++)
            {
                r = (r & 0x80000000) != 0 ? (r << 1) ^ poly : r << 1;
            }

            table[i] = r;
        }

        uint reg = init;
        foreach (byte b in data)
        {
            byte input = reflect ? Reverse8(b) : b;
            reg = (reg << 8) ^ table[(reg >> 24) ^ input];
        }

        if (reflect)
        {
            reg = ReverseBits(reg);
        }

        return reg ^ xor;
    }

    private static ulong Crc64Reference(ulong poly, ulong init, ulong xor, bool reflect, ReadOnlySpan<byte> data)
    {
        ulong[] table = new ulong[256];
        for (ulong i = 0; i < 256; i++)
        {
            ulong r = i << 56;
            for (int k = 0; k < 8; k++)
            {
                r = (r & 0x8000000000000000) != 0 ? (r << 1) ^ poly : r << 1;
            }

            table[i] = r;
        }

        ulong reg = init;
        foreach (byte b in data)
        {
            byte input = reflect ? Reverse8(b) : b;
            reg = (reg << 8) ^ table[(reg >> 56) ^ input];
        }

        if (reflect)
        {
            reg = ReverseBits(reg);
        }

        return reg ^ xor;
    }

    private static uint AdlerReference(ReadOnlySpan<byte> data)
    {
        uint a = 1, b = 0;
        foreach (byte d in data)
        {
            a = (a + d) % 65521;
            b = (b + a) % 65521;
        }

        return (b << 16) | a;
    }

    private static byte Reverse8(byte b) => (byte)(ReverseBits((uint)b) >> 24);

    private static uint ReverseBits(uint v)
    {
        uint r = 0;
        for (int i = 0; i < 32; i++)
        {
            r = (r << 1) | ((v >> i) & 1);
        }

        return r;
    }

    private static ulong ReverseBits(ulong v)
    {
        ulong r = 0;
        for (int i = 0; i < 64; i++)
        {
            r = (r << 1) | ((v >> i) & 1);
        }

        return r;
    }

    private static void CheckValue(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
