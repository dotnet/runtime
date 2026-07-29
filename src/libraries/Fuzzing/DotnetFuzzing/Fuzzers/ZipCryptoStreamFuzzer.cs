// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.IO.Compression;
using System.Threading.Tasks;

namespace DotnetFuzzing.Fuzzers;

internal sealed class ZipCryptoStreamFuzzer : IFuzzer
{
    public string[] TargetAssemblies { get; } = ["System.IO.Compression"];
    public string[] TargetCoreLibPrefixes => [];

    private const string Password = "fuzz-password";
    private const string EntryName = "entry";

    public void FuzzTarget(ReadOnlySpan<byte> bytes)
    {
        // The encryption streams only produce ciphertext for non-empty content; an empty
        // entry is stored unencrypted, so it does not exercise the ZipCryptoStream at all.
        if (bytes.IsEmpty)
        {
            return;
        }

        byte[] content = CopyToRentedArray(bytes);
        try
        {
            RoundTrip(content, bytes.Length, async: false).GetAwaiter().GetResult();
            RoundTrip(content, bytes.Length, async: true).GetAwaiter().GetResult();
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(content);
        }
    }

    private static async Task RoundTrip(byte[] content, int length, bool async)
    {
        using var archiveStream = new MemoryStream();

        // Encrypt the fuzz input using legacy ZipCrypto, exercising the ZipCryptoStream encryption path.
        ZipArchive writeArchive = async
            ? await ZipArchive.CreateAsync(archiveStream, ZipArchiveMode.Create, leaveOpen: true, entryNameEncoding: null)
            : new ZipArchive(archiveStream, ZipArchiveMode.Create, leaveOpen: true, entryNameEncoding: null);

        ZipArchiveEntry writeEntry = writeArchive.CreateEntry(EntryName, Password.AsSpan(), ZipEncryptionMethod.ZipCrypto);

        Stream entryWriteStream = async ? await writeEntry.OpenAsync() : writeEntry.Open();
        if (async)
        {
            await entryWriteStream.WriteAsync(content.AsMemory(0, length));
            await entryWriteStream.DisposeAsync();
            await writeArchive.DisposeAsync();
        }
        else
        {
            entryWriteStream.Write(content, 0, length);
            entryWriteStream.Dispose();
            writeArchive.Dispose();
        }

        archiveStream.Position = 0;

        // Decrypt with the correct password and verify the round-trip is lossless.
        ZipArchive readArchive = async
            ? await ZipArchive.CreateAsync(archiveStream, ZipArchiveMode.Read, leaveOpen: true, entryNameEncoding: null)
            : new ZipArchive(archiveStream, ZipArchiveMode.Read, leaveOpen: true, entryNameEncoding: null);

        ZipArchiveEntry readEntry = readArchive.GetEntry(EntryName)!;
        Assert.True(readEntry.IsEncrypted);
        Assert.Equal(ZipEncryptionMethod.ZipCrypto, readEntry.EncryptionMethod);

        using (var decrypted = new MemoryStream())
        {
            Stream entryReadStream = async ? await readEntry.OpenAsync(Password.AsSpan()) : readEntry.Open(Password.AsSpan());
            if (async)
            {
                await entryReadStream.CopyToAsync(decrypted);
                await entryReadStream.DisposeAsync();
            }
            else
            {
                entryReadStream.CopyTo(decrypted);
                entryReadStream.Dispose();
            }

            Assert.SequenceEqual(content.AsSpan(0, length), decrypted.GetBuffer().AsSpan(0, (int)decrypted.Length));
        }

        // Exercise the wrong-password failure path. ZipCrypto only has a weak 1-byte password
        // verifier over a stream cipher, so a wrong key is rejected with InvalidDataException with
        // roughly 255/256 probability and may occasionally decrypt to garbage without throwing;
        // both outcomes are acceptable and cannot be distinguished reliably on fuzzed input. Any
        // other exception type would indicate a real bug and is intentionally left to propagate.
        try
        {
            Stream wrongStream = async
                ? await readEntry.OpenAsync("wrong-password".AsSpan())
                : readEntry.Open("wrong-password".AsSpan());
            try
            {
                if (async)
                {
                    await wrongStream.CopyToAsync(Stream.Null);
                }
                else
                {
                    wrongStream.CopyTo(Stream.Null);
                }
            }
            finally
            {
                if (async)
                {
                    await wrongStream.DisposeAsync();
                }
                else
                {
                    wrongStream.Dispose();
                }
            }
        }
        catch (InvalidDataException)
        {
            // Expected: the check byte rejects the wrong key.
        }

        if (async)
        {
            await readArchive.DisposeAsync();
        }
        else
        {
            readArchive.Dispose();
        }
    }

    private static byte[] CopyToRentedArray(ReadOnlySpan<byte> bytes)
    {
        byte[] buffer = ArrayPool<byte>.Shared.Rent(bytes.Length);
        bytes.CopyTo(buffer);
        return buffer;
    }
}
