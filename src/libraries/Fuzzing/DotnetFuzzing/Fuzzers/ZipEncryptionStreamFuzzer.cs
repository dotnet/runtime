// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading.Tasks;

namespace DotnetFuzzing.Fuzzers;

// Exercises every ZIP entry-encryption method through a single round-trip: legacy ZipCrypto
// (ZipCryptoStream) and WinZip AES-128/192/256 (WinZipAesStream). The two ciphers share the same
// ZipArchiveEntry entry points and stream shape, so one fuzzer covers both encryption paths.
[UnsupportedOSPlatform("browser")]
internal sealed class ZipEncryptionStreamFuzzer : IFuzzer
{
    public string[] TargetAssemblies { get; } = ["System.IO.Compression"];
    public string[] TargetCoreLibPrefixes => [];

    // The entry name is not part of the encryption-stream surface this fuzzer targets: it only feeds
    // the generic ZIP name-encoding logic (ASCII-vs-UTF8 detection and the UTF-8 general-purpose-bit
    // flag), which is independent of the AES/ZipCrypto encryption path. Fuzzing it here would add
    // coverage outside the target, so it is hardcoded; entry-name encoding belongs in a ZIP fuzzer.
    private const string EntryName = "entry";

    // Used only when the fuzzed password slice is empty; a non-empty password is required to encrypt.
    private const string FallbackPassword = "fuzz-password";

    private static readonly ZipEncryptionMethod[] s_methods =
    [
        ZipEncryptionMethod.ZipCrypto,
        ZipEncryptionMethod.Aes128,
        ZipEncryptionMethod.Aes192,
        ZipEncryptionMethod.Aes256,
    ];

    public void FuzzTarget(ReadOnlySpan<byte> bytes)
    {
        // The first byte is consumed below as the encryption-method selector, so an empty
        // input has no byte to interpret and there is nothing to fuzz.
        if (bytes.Length < 2)
        {
            return;
        }

        // Use the first byte to select the encryption method so all variants get exercised.
        ZipEncryptionMethod method = s_methods[bytes[0] % s_methods.Length];
        ReadOnlySpan<byte> payload = bytes.Slice(2); // Slice 2 to keep MemoryMarshal.Cast<byte, char> below aligned

        // Split the rest of the input into a password and the entry content: everything up to the
        // first newline is the password, the remainder is the content. Deriving the password from the
        // input drives the key derivation and the UTF-8 password-encoding path instead of always using
        // a fixed key. The content — the remainder — is what drives the cipher chunking (and, for AES,
        // the trailing HMAC), so it stays the bulk of the input.
        int newlineIndex = payload.IndexOf((byte)'\n');
        ReadOnlySpan<byte> passwordBytes = newlineIndex < 0 ? ReadOnlySpan<byte>.Empty : payload.Slice(0, newlineIndex);
        ReadOnlySpan<byte> contentBytes = newlineIndex < 0 ? payload : payload.Slice(newlineIndex + 1);

        // An empty entry is stored unencrypted (no ciphertext is produced and IsEncrypted is false on
        // read-back), so it does not exercise the encryption stream this fuzzer targets. Skip it.
        if (contentBytes.IsEmpty)
        {
            return;
        }

        // Interpret the password bytes as UTF-16 so arbitrary char values (including lone surrogates)
        // exercise the UTF-8 encoding of the password during key derivation.
        string password = new string(MemoryMarshal.Cast<byte, char>(passwordBytes));

        // A non-empty password is required to create an encrypted entry (CreateEntry throws otherwise).
        // The fuzzed slice may yield an empty string (no newline, or a single byte that casts to zero
        // chars), so fall back to a fixed non-empty password to keep exercising the key-derivation path.
        if (password.Length == 0)
        {
            password = FallbackPassword;
        }

        // Poison the page immediately after the content so any over-read while encrypting or
        // decrypting faults immediately instead of silently reading adjacent memory.
        using PooledBoundedMemory<byte> buffer = PooledBoundedMemory<byte>.Rent(contentBytes, PoisonPagePlacement.After);
        RoundTrip(buffer.Memory, password, method, async: false).GetAwaiter().GetResult();
        RoundTrip(buffer.Memory, password, method, async: true).GetAwaiter().GetResult();
    }

    private static async Task RoundTrip(ReadOnlyMemory<byte> content, string password, ZipEncryptionMethod method, bool async)
    {
        using var archiveStream = new MemoryStream();

        // Encrypt the fuzz input, exercising the encryption stream (and HMAC path for AES).
        ZipArchive writeArchive = async
            ? await ZipArchive.CreateAsync(archiveStream, ZipArchiveMode.Create, leaveOpen: true, entryNameEncoding: null)
            : new ZipArchive(archiveStream, ZipArchiveMode.Create, leaveOpen: true, entryNameEncoding: null);

        ZipArchiveEntry writeEntry = writeArchive.CreateEntry(EntryName, password.AsSpan(), method);

        Stream entryWriteStream = async ? await writeEntry.OpenAsync() : writeEntry.Open();
        if (async)
        {
            await entryWriteStream.WriteAsync(content);
            await entryWriteStream.DisposeAsync();
            await writeArchive.DisposeAsync();
        }
        else
        {
            entryWriteStream.Write(content.Span);
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
            Stream entryReadStream = async ? await readEntry.OpenAsync(password.AsSpan()) : readEntry.Open(password.AsSpan());
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

            Assert.SequenceEqual(content.Span, decrypted.GetBuffer().AsSpan(0, (int)decrypted.Length));
        }

        // The AES variant of ZIP encryption uses a form of HMAC that makes accidentally accepting the wrong key
        // statistically unlikely, so a wrong password must fail with InvalidDataException.
        // ZipCrypto only has a weak 1-byte verifier over a stream cipher, so a wrong key is rejected
        // with roughly 255/256 probability and may occasionally decrypt to garbage without throwing;
        // both outcomes are acceptable and cannot be distinguished reliably on fuzzed input. Any
        // other exception type would indicate a real bug and is intentionally left to propagate.
        // Append a byte so the wrong password is guaranteed to differ from the fuzzed one above.
        bool authenticated = method != ZipEncryptionMethod.ZipCrypto;
        string wrongPassword = password + "!";
        bool wrongPasswordRejected = false;
        try
        {
            if (async)
            {
                await using Stream wrongStream = await readEntry.OpenAsync(wrongPassword.AsSpan());
                await wrongStream.CopyToAsync(Stream.Null);
            }
            else
            {
                using Stream wrongStream = readEntry.Open(wrongPassword.AsSpan());
                wrongStream.CopyTo(Stream.Null);
            }
        }
        catch (InvalidDataException)
        {
            // Expected: the AES password verifier / HMAC (or the ZipCrypto check byte) rejects the wrong key.
            wrongPasswordRejected = true;
        }

        if (authenticated)
        {
            Assert.True(wrongPasswordRejected);
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
}
