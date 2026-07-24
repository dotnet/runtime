// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.IO.Compression;
using System.Runtime.Versioning;
using System.Threading.Tasks;

namespace DotnetFuzzing.Fuzzers;

[UnsupportedOSPlatform("browser")]
internal sealed class WinZipAesStreamFuzzer : IFuzzer
{
    public string[] TargetAssemblies { get; } = ["System.IO.Compression"];
    public string[] TargetCoreLibPrefixes => [];

    private const string Password = "fuzz-password";
    private const string EntryName = "entry";

    private static readonly ZipEncryptionMethod[] s_aesMethods =
    [
        ZipEncryptionMethod.Aes128,
        ZipEncryptionMethod.Aes192,
        ZipEncryptionMethod.Aes256,
    ];

    public void FuzzTarget(ReadOnlySpan<byte> bytes)
    {
        // The encryption streams only produce ciphertext for non-empty content; an empty
        // entry is stored unencrypted, so it does not exercise the WinZipAesStream at all.
        if (bytes.IsEmpty)
        {
            return;
        }

        // Use the first byte to select the AES key strength so all three variants get exercised.
        ZipEncryptionMethod method = s_aesMethods[bytes[0] % s_aesMethods.Length];

        byte[] content = CopyToRentedArray(bytes);
        try
        {
            RoundTrip(content, bytes.Length, method, async: false).GetAwaiter().GetResult();
            RoundTrip(content, bytes.Length, method, async: true).GetAwaiter().GetResult();
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(content);
        }
    }

    private static async Task RoundTrip(byte[] content, int length, ZipEncryptionMethod method, bool async)
    {
        using var archiveStream = new MemoryStream();

        // Encrypt the fuzz input using WinZip AES, exercising the WinZipAesStream encryption + HMAC path.
        ZipArchive writeArchive = async
            ? await ZipArchive.CreateAsync(archiveStream, ZipArchiveMode.Create, leaveOpen: true, entryNameEncoding: null)
            : new ZipArchive(archiveStream, ZipArchiveMode.Create, leaveOpen: true, entryNameEncoding: null);

        ZipArchiveEntry writeEntry = writeArchive.CreateEntry(EntryName, Password.AsSpan(), method);

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
        Assert.Equal(method, readEntry.EncryptionMethod);

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

            Assert.SequenceEqual(content.AsSpan(0, length), decrypted.ToArray());
        }

        // Decrypting with a wrong password must fail cleanly with InvalidDataException, never crash.
        try
        {
            using Stream stream = readEntry.Open("wrong-password".AsSpan());
            stream.CopyTo(Stream.Null);
        }
        catch (InvalidDataException)
        {
            // Expected: the AES password verifier / HMAC rejects the wrong key.
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
