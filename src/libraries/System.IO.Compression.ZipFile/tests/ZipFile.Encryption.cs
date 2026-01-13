// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Compression.Tests
{
    public class ZipFile_EncryptionTests : ZipFileTestBase
    {
        public static IEnumerable<object[]> EncryptionMethodAndBoolTestData()
        {
            foreach (var method in new[]
            {
                ZipArchiveEntry.EncryptionMethod.ZipCrypto,
                ZipArchiveEntry.EncryptionMethod.Aes128,
                ZipArchiveEntry.EncryptionMethod.Aes192,
                ZipArchiveEntry.EncryptionMethod.Aes256
            })
            {
                yield return new object[] { method, false };
                yield return new object[] { method, true };
            }
        }

        [Theory]
        [MemberData(nameof(EncryptionMethodAndBoolTestData))]
        public async Task Encryption_SingleEntry_RoundTrip(ZipArchiveEntry.EncryptionMethod encryptionMethod, bool async)
        {
            string archivePath = GetTempArchivePath();
            string entryName = "test.txt";
            string content = "Secret Content";
            string password = "password123";

            var entries = new[] { (entryName, content, (string?)password, (ZipArchiveEntry.EncryptionMethod?)encryptionMethod) };

            await CreateArchiveWithEntries(archivePath, entries, async);

            using (ZipArchive archive = await CallZipFileOpenRead(async, archivePath))
            {
                ZipArchiveEntry entry = archive.GetEntry(entryName);
                Assert.NotNull(entry);

                await AssertEntryTextEquals(entry, content, password, async);
            }
        }

        [Theory]
        [MemberData(nameof(EncryptionMethodAndBoolTestData))]
        public async Task Encryption_MultipleEntries_SamePassword_RoundTrip(ZipArchiveEntry.EncryptionMethod encryptionMethod, bool async)
        {
            string archivePath = GetTempArchivePath();
            string password = "SharedPassword";
            var entries = new[]
            {
                ("file1.txt", "Content 1", (string?)password, (ZipArchiveEntry.EncryptionMethod?)encryptionMethod),
                ("folder/file2.txt", "Content 2", (string?)password, (ZipArchiveEntry.EncryptionMethod?)encryptionMethod)
            };

            await CreateArchiveWithEntries(archivePath, entries, async);

            using (ZipArchive archive = await CallZipFileOpenRead(async, archivePath))
            {
                foreach (var (name, content, pwd, _) in entries)
                {
                    var entry = archive.GetEntry(name);
                    Assert.NotNull(entry);
                    await AssertEntryTextEquals(entry, content, pwd, async);
                }
            }
        }

        [Theory]
        [MemberData(nameof(EncryptionMethodAndBoolTestData))]
        public async Task Encryption_MultipleEntries_DifferentPasswords_RoundTrip(ZipArchiveEntry.EncryptionMethod encryptionMethod, bool async)
        {
            string archivePath = GetTempArchivePath();
            var entries = new[]
            {
                ("file1.txt", "Content 1", (string?)"pass1", (ZipArchiveEntry.EncryptionMethod?)encryptionMethod),
                ("file2.txt", "Content 2", (string?)"pass2", (ZipArchiveEntry.EncryptionMethod?)encryptionMethod)
            };

            await CreateArchiveWithEntries(archivePath, entries, async);

            using (ZipArchive archive = await CallZipFileOpenRead(async, archivePath))
            {
                foreach (var (name, content, pwd, _) in entries)
                {
                    var entry = archive.GetEntry(name);
                    Assert.NotNull(entry);
                    await AssertEntryTextEquals(entry, content, pwd, async);
                }
            }
        }

        [Theory]
        [MemberData(nameof(EncryptionMethodAndBoolTestData))]
        public async Task Encryption_MixedPlainAndEncrypted_RoundTrip(ZipArchiveEntry.EncryptionMethod encryptionMethod, bool async)
        {
            string archivePath = GetTempArchivePath();
            var entries = new[]
            {
                ("plain.txt", "Plain Content", (string?)null, (ZipArchiveEntry.EncryptionMethod?)null),
                ("encrypted.txt", "Encrypted Content", (string?)"pass", (ZipArchiveEntry.EncryptionMethod?)encryptionMethod)
            };

            await CreateArchiveWithEntries(archivePath, entries, async);

            using (ZipArchive archive = await CallZipFileOpenRead(async, archivePath))
            {
                // Check plain
                var plainEntry = archive.GetEntry("plain.txt");
                Assert.NotNull(plainEntry);
                await AssertEntryTextEquals(plainEntry, "Plain Content", null, async);

                // Check encrypted
                var encEntry = archive.GetEntry("encrypted.txt");
                Assert.NotNull(encEntry);
                await AssertEntryTextEquals(encEntry, "Encrypted Content", "pass", async);
            }
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public async Task Encryption_Combinations_RoundTrip(bool async)
        {
            string archivePath = GetTempArchivePath();
            var entries = new[]
            {
                ("zipcrypto.txt", "ZipCrypto Content", (string?)"pass1", (ZipArchiveEntry.EncryptionMethod?)ZipArchiveEntry.EncryptionMethod.ZipCrypto),
                ("aes128.txt", "AES128 Content", (string?)"pass2", (ZipArchiveEntry.EncryptionMethod?)ZipArchiveEntry.EncryptionMethod.Aes128),
                ("aes256.txt", "AES256 Content", (string?)"pass3", (ZipArchiveEntry.EncryptionMethod?)ZipArchiveEntry.EncryptionMethod.Aes256)
            };

            await CreateArchiveWithEntries(archivePath, entries, async);

            using (ZipArchive archive = await CallZipFileOpenRead(async, archivePath))
            {
                foreach (var (name, content, pwd, _) in entries)
                {
                    var entry = archive.GetEntry(name);
                    Assert.NotNull(entry);
                    await AssertEntryTextEquals(entry, content, pwd, async);
                }
            }
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public async Task Encryption_LargeFile_RoundTrip(bool async)
        {
            string archivePath = GetTempArchivePath();
            string entryName = "large.bin";
            int size = 1024 * 1024; // 1MB
            byte[] content = new byte[size];
            new Random(42).NextBytes(content);
            string password = "password123";
            var encryptionMethod = ZipArchiveEntry.EncryptionMethod.Aes256;

            using (ZipArchive archive = await CallZipFileOpen(async, archivePath, ZipArchiveMode.Create))
            {
                ZipArchiveEntry entry = archive.CreateEntry(entryName);
                Stream s = entry.Open(password, encryptionMethod);
                using (s)
                {
                    if (async)
                        await s.WriteAsync(content, 0, content.Length);
                    else
                        s.Write(content, 0, content.Length);
                }
            }

            using (ZipArchive archive = await CallZipFileOpenRead(async, archivePath))
            {
                ZipArchiveEntry entry = archive.GetEntry(entryName);
                Assert.NotNull(entry);

                Stream s = entry.Open(password);
                using (s)
                using (MemoryStream ms = new MemoryStream())
                {
                    if (async)
                        await s.CopyToAsync(ms);
                    else
                        s.CopyTo(ms);

                    Assert.Equal(content, ms.ToArray());
                }
            }
        }

        [Fact]
        public void Negative_WrongPassword_Throws_InvalidDataException()
        {
            string archivePath = GetTempArchivePath();
            string password = "correct";
            var entries = new[] { ("test.txt", "content", (string?)password, (ZipArchiveEntry.EncryptionMethod?)ZipArchiveEntry.EncryptionMethod.Aes256) };
            CreateArchiveWithEntries(archivePath, entries, async: false).GetAwaiter().GetResult();

            using (ZipArchive archive = ZipFile.OpenRead(archivePath))
            {
                var entry = archive.GetEntry("test.txt");
                Assert.Throws<InvalidDataException>(() => entry.Open("wrong"));
            }
        }

        [Fact]
        public void Negative_MissingPassword_Throws_InvalidDataException()
        {
            string archivePath = GetTempArchivePath();
            string password = "correct";
            var entries = new[] { ("test.txt", "content", (string?)password, (ZipArchiveEntry.EncryptionMethod?)ZipArchiveEntry.EncryptionMethod.Aes256) };
            CreateArchiveWithEntries(archivePath, entries, async: false).GetAwaiter().GetResult();

            using (ZipArchive archive = ZipFile.OpenRead(archivePath))
            {
                var entry = archive.GetEntry("test.txt");
                Assert.Throws<InvalidDataException>(() => entry.Open());
            }
        }

        [Fact]
        public void Negative_OpeningPlainEntryWithPassword_Throws()
        {
            string archivePath = GetTempArchivePath();
            var entries = new[] { ("plain.txt", "content", (string?)null, (ZipArchiveEntry.EncryptionMethod?)null) };
            CreateArchiveWithEntries(archivePath, entries, async: false).GetAwaiter().GetResult();

            using (ZipArchive archive = ZipFile.OpenRead(archivePath))
            {
                var entry = archive.GetEntry("plain.txt");
                Assert.ThrowsAny<Exception>(() => entry.Open("password"));
            }
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public async Task ExtractToFile_Encrypted_Success(bool async)
        {
            string archivePath = GetTempArchivePath();
            string password = "pass";
            var entries = new[] { ("test.txt", "content", (string?)password, (ZipArchiveEntry.EncryptionMethod?)ZipArchiveEntry.EncryptionMethod.Aes256) };
            await CreateArchiveWithEntries(archivePath, entries, async);

            using (ZipArchive archive = await CallZipFileOpenRead(async, archivePath))
            {
                var entry = archive.GetEntry("test.txt");
                string destFile = GetTestFilePath();

                if (async)
                {
                    await entry.ExtractToFileAsync(destFile, overwrite: true, password: password);
                    Assert.Equal("content", await File.ReadAllTextAsync(destFile));
                }
                else
                {
                    entry.ExtractToFile(destFile, overwrite: true, password: password);
                    Assert.Equal("content", File.ReadAllText(destFile));
                }
            }
        }

        private string GetTempArchivePath() => GetTestFilePath();

        private async Task CreateArchiveWithEntries(string archivePath, (string Name, string Content, string? Password, ZipArchiveEntry.EncryptionMethod? Encryption)[] entries, bool async)
        {
            using (ZipArchive archive = await CallZipFileOpen(async, archivePath, ZipArchiveMode.Create))
            {
                foreach (var (name, content, password, encryption) in entries)
                {
                    ZipArchiveEntry entry = archive.CreateEntry(name);
                    Stream s;
                    if (password != null && encryption.HasValue)
                    {
                        s = entry.Open(password, encryption.Value);
                    }
                    else
                    {
                        s = await OpenEntryStream(async, entry);
                    }

                    using (s)
                    using (StreamWriter w = new StreamWriter(s, Encoding.UTF8))
                    {
                        if (async)
                            await w.WriteAsync(content);
                        else
                            w.Write(content);
                    }
                }
            }
        }

        private async Task AssertEntryTextEquals(ZipArchiveEntry entry, string expected, string? password, bool async)
        {
            Stream s;
            if (password != null)
            {
                s = entry.Open(password);
            }
            else
            {
                s = await OpenEntryStream(async, entry);
            }

            using (s)
            using (StreamReader r = new StreamReader(s, Encoding.UTF8))
            {
                string actual;
                if (async)
                    actual = await r.ReadToEndAsync();
                else
                    actual = r.ReadToEnd();

                Assert.Equal(expected, actual);
            }
        }

        #region Update Mode Tests for Encrypted Entries

        [Theory]
        [MemberData(nameof(EncryptionMethodAndBoolTestData))]
        public async Task UpdateMode_ModifyEncryptedEntry_RoundTrip(ZipArchiveEntry.EncryptionMethod encryptionMethod, bool async)
        {
            string archivePath = GetTempArchivePath();
            string entryName = "test.txt";
            string originalContent = "Original Content";
            string modifiedContent = "Modified Content After Update";
            string password = "password123";

            // Create archive with encrypted entry
            var entries = new[] { (entryName, originalContent, (string?)password, (ZipArchiveEntry.EncryptionMethod?)encryptionMethod) };
            await CreateArchiveWithEntries(archivePath, entries, async);

            // Verify original content
            using (ZipArchive archive = await CallZipFileOpenRead(async, archivePath))
            {
                ZipArchiveEntry entry = archive.GetEntry(entryName);
                Assert.NotNull(entry);
                Assert.True(entry.IsEncrypted);
                await AssertEntryTextEquals(entry, originalContent, password, async);
            }

            // Open in Update mode and modify the encrypted entry
            using (ZipArchive archive = await CallZipFileOpen(async, archivePath, ZipArchiveMode.Update))
            {
                ZipArchiveEntry entry = archive.GetEntry(entryName);
                Assert.NotNull(entry);
                Assert.True(entry.IsEncrypted);

                // Open with password for editing
                using (Stream stream = entry.Open(password))
                {
                    // Clear existing content and write new content
                    stream.SetLength(0);
                    byte[] newContentBytes = Encoding.UTF8.GetBytes(modifiedContent);
                    if (async)
                        await stream.WriteAsync(newContentBytes, 0, newContentBytes.Length);
                    else
                        stream.Write(newContentBytes, 0, newContentBytes.Length);
                }
            }

            // Verify modified content
            using (ZipArchive archive = await CallZipFileOpenRead(async, archivePath))
            {
                ZipArchiveEntry entry = archive.GetEntry(entryName);
                Assert.NotNull(entry);
                Assert.True(entry.IsEncrypted);
                await AssertEntryTextEquals(entry, modifiedContent, password, async);
            }
        }

        [Theory]
        [MemberData(nameof(EncryptionMethodAndBoolTestData))]
        public async Task UpdateMode_AppendToEncryptedEntry_RoundTrip(ZipArchiveEntry.EncryptionMethod encryptionMethod, bool async)
        {
            string archivePath = GetTempArchivePath();
            string entryName = "test.txt";
            string originalContent = "Original Content";
            string appendedContent = " - Appended Text";
            string password = "password123";

            // Create archive with encrypted entry
            var entries = new[] { (entryName, originalContent, (string?)password, (ZipArchiveEntry.EncryptionMethod?)encryptionMethod) };
            await CreateArchiveWithEntries(archivePath, entries, async);

            // Open in Update mode and append to the encrypted entry
            using (ZipArchive archive = await CallZipFileOpen(async, archivePath, ZipArchiveMode.Update))
            {
                ZipArchiveEntry entry = archive.GetEntry(entryName);
                Assert.NotNull(entry);

                using (Stream stream = entry.Open(password))
                {
                    // Seek to end and append
                    stream.Seek(0, SeekOrigin.End);
                    byte[] appendBytes = Encoding.UTF8.GetBytes(appendedContent);
                    if (async)
                        await stream.WriteAsync(appendBytes, 0, appendBytes.Length);
                    else
                        stream.Write(appendBytes, 0, appendBytes.Length);
                }
            }

            // Verify content has original + appended
            using (ZipArchive archive = await CallZipFileOpenRead(async, archivePath))
            {
                ZipArchiveEntry entry = archive.GetEntry(entryName);
                Assert.NotNull(entry);
                Assert.True(entry.IsEncrypted);
                await AssertEntryTextEquals(entry, originalContent + appendedContent, password, async);
            }
        }

        [Theory]
        [MemberData(nameof(EncryptionMethodAndBoolTestData))]
        public async Task UpdateMode_ReadOnlyEncryptedEntry_NoModification(ZipArchiveEntry.EncryptionMethod encryptionMethod, bool async)
        {
            string archivePath = GetTempArchivePath();
            string entryName = "test.txt";
            string content = "Unmodified Content";
            string password = "password123";

            // Create archive with encrypted entry
            var entries = new[] { (entryName, content, (string?)password, (ZipArchiveEntry.EncryptionMethod?)encryptionMethod) };
            await CreateArchiveWithEntries(archivePath, entries, async);

            // Open in Update mode, read the entry but don't modify it
            using (ZipArchive archive = await CallZipFileOpen(async, archivePath, ZipArchiveMode.Update))
            {
                ZipArchiveEntry entry = archive.GetEntry(entryName);
                Assert.NotNull(entry);

                using (Stream stream = entry.Open(password))
                using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                {
                    string readContent = async ? await reader.ReadToEndAsync() : reader.ReadToEnd();
                    Assert.Equal(content, readContent);
                }
            }

            // Verify content is still intact after update mode
            using (ZipArchive archive = await CallZipFileOpenRead(async, archivePath))
            {
                ZipArchiveEntry entry = archive.GetEntry(entryName);
                Assert.NotNull(entry);
                Assert.True(entry.IsEncrypted);
                await AssertEntryTextEquals(entry, content, password, async);
            }
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public async Task UpdateMode_MultipleEncryptedEntries_ModifyOne(bool async)
        {
            string archivePath = GetTempArchivePath();
            string password = "password123";
            var encryptionMethod = ZipArchiveEntry.EncryptionMethod.Aes256;

            var entries = new[]
            {
                ("file1.txt", "Content 1", (string?)password, (ZipArchiveEntry.EncryptionMethod?)encryptionMethod),
                ("file2.txt", "Content 2", (string?)password, (ZipArchiveEntry.EncryptionMethod?)encryptionMethod),
                ("file3.txt", "Content 3", (string?)password, (ZipArchiveEntry.EncryptionMethod?)encryptionMethod)
            };

            await CreateArchiveWithEntries(archivePath, entries, async);

            // Modify only file2.txt
            using (ZipArchive archive = await CallZipFileOpen(async, archivePath, ZipArchiveMode.Update))
            {
                ZipArchiveEntry entry = archive.GetEntry("file2.txt");
                Assert.NotNull(entry);

                using (Stream stream = entry.Open(password))
                {
                    stream.SetLength(0);
                    byte[] newContent = Encoding.UTF8.GetBytes("Modified Content 2");
                    if (async)
                        await stream.WriteAsync(newContent, 0, newContent.Length);
                    else
                        stream.Write(newContent, 0, newContent.Length);
                }
            }

            // Verify: file1 and file3 unchanged, file2 modified
            using (ZipArchive archive = await CallZipFileOpenRead(async, archivePath))
            {
                await AssertEntryTextEquals(archive.GetEntry("file1.txt"), "Content 1", password, async);
                await AssertEntryTextEquals(archive.GetEntry("file2.txt"), "Modified Content 2", password, async);
                await AssertEntryTextEquals(archive.GetEntry("file3.txt"), "Content 3", password, async);
            }
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public async Task UpdateMode_MixedEncryption_ModifyEncrypted(bool async)
        {
            string archivePath = GetTempArchivePath();
            string password = "password123";

            var entries = new[]
            {
                ("plain.txt", "Plain Content", (string?)null, (ZipArchiveEntry.EncryptionMethod?)null),
                ("encrypted.txt", "Encrypted Content", (string?)password, (ZipArchiveEntry.EncryptionMethod?)ZipArchiveEntry.EncryptionMethod.Aes256)
            };

            await CreateArchiveWithEntries(archivePath, entries, async);

            // Modify the encrypted entry
            using (ZipArchive archive = await CallZipFileOpen(async, archivePath, ZipArchiveMode.Update))
            {
                ZipArchiveEntry entry = archive.GetEntry("encrypted.txt");
                Assert.NotNull(entry);
                Assert.True(entry.IsEncrypted);

                using (Stream stream = entry.Open(password))
                {
                    stream.SetLength(0);
                    byte[] newContent = Encoding.UTF8.GetBytes("Modified Encrypted Content");
                    if (async)
                        await stream.WriteAsync(newContent, 0, newContent.Length);
                    else
                        stream.Write(newContent, 0, newContent.Length);
                }
            }

            // Verify both entries
            using (ZipArchive archive = await CallZipFileOpenRead(async, archivePath))
            {
                // Plain entry should be unchanged
                var plainEntry = archive.GetEntry("plain.txt");
                Assert.NotNull(plainEntry);
                Assert.False(plainEntry.IsEncrypted);
                await AssertEntryTextEquals(plainEntry, "Plain Content", null, async);

                // Encrypted entry should be modified
                var encryptedEntry = archive.GetEntry("encrypted.txt");
                Assert.NotNull(encryptedEntry);
                Assert.True(encryptedEntry.IsEncrypted);
                await AssertEntryTextEquals(encryptedEntry, "Modified Encrypted Content", password, async);
            }
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public async Task UpdateMode_LargeEncryptedEntry_Modify(bool async)
        {
            string archivePath = GetTempArchivePath();
            string entryName = "large.bin";
            int originalSize = 512 * 1024; // 512KB
            int modifiedSize = 768 * 1024; // 768KB
            byte[] originalContent = new byte[originalSize];
            byte[] modifiedContent = new byte[modifiedSize];
            new Random(42).NextBytes(originalContent);
            new Random(43).NextBytes(modifiedContent);
            string password = "password123";
            var encryptionMethod = ZipArchiveEntry.EncryptionMethod.Aes256;

            // Create archive with large encrypted entry
            using (ZipArchive archive = await CallZipFileOpen(async, archivePath, ZipArchiveMode.Create))
            {
                ZipArchiveEntry entry = archive.CreateEntry(entryName);
                using (Stream s = entry.Open(password, encryptionMethod))
                {
                    if (async)
                        await s.WriteAsync(originalContent, 0, originalContent.Length);
                    else
                        s.Write(originalContent, 0, originalContent.Length);
                }
            }

            // Update with different content
            using (ZipArchive archive = await CallZipFileOpen(async, archivePath, ZipArchiveMode.Update))
            {
                ZipArchiveEntry entry = archive.GetEntry(entryName);
                Assert.NotNull(entry);

                using (Stream stream = entry.Open(password))
                {
                    stream.SetLength(0);
                    if (async)
                        await stream.WriteAsync(modifiedContent, 0, modifiedContent.Length);
                    else
                        stream.Write(modifiedContent, 0, modifiedContent.Length);
                }
            }

            // Verify modified content
            using (ZipArchive archive = await CallZipFileOpenRead(async, archivePath))
            {
                ZipArchiveEntry entry = archive.GetEntry(entryName);
                Assert.NotNull(entry);
                Assert.True(entry.IsEncrypted);

                using (Stream s = entry.Open(password))
                using (MemoryStream ms = new MemoryStream())
                {
                    if (async)
                        await s.CopyToAsync(ms);
                    else
                        s.CopyTo(ms);

                    Assert.Equal(modifiedContent, ms.ToArray());
                }
            }
        }

        [Theory]
        [MemberData(nameof(EncryptionMethodAndBoolTestData))]
        public async Task UpdateMode_EncryptedEntry_EmptyAfterModification(ZipArchiveEntry.EncryptionMethod encryptionMethod, bool async)
        {
            string archivePath = GetTempArchivePath();
            string entryName = "test.txt";
            string originalContent = "Original Content";
            string password = "password123";

            // Create archive with encrypted entry
            var entries = new[] { (entryName, originalContent, (string?)password, (ZipArchiveEntry.EncryptionMethod?)encryptionMethod) };
            await CreateArchiveWithEntries(archivePath, entries, async);

            // Open in Update mode and clear the content
            using (ZipArchive archive = await CallZipFileOpen(async, archivePath, ZipArchiveMode.Update))
            {
                ZipArchiveEntry entry = archive.GetEntry(entryName);
                Assert.NotNull(entry);

                using (Stream stream = entry.Open(password))
                {
                    stream.SetLength(0); // Make it empty
                }
            }

            // Verify entry is now empty
            using (ZipArchive archive = await CallZipFileOpenRead(async, archivePath))
            {
                ZipArchiveEntry entry = archive.GetEntry(entryName);
                Assert.NotNull(entry);
                Assert.True(entry.IsEncrypted);
                await AssertEntryTextEquals(entry, "", password, async);
            }
        }

        [Fact]
        public void UpdateMode_EncryptedEntry_WrongPassword_Throws()
        {
            string archivePath = GetTempArchivePath();
            string password = "correct";
            var entries = new[] { ("test.txt", "content", (string?)password, (ZipArchiveEntry.EncryptionMethod?)ZipArchiveEntry.EncryptionMethod.Aes256) };
            CreateArchiveWithEntries(archivePath, entries, async: false).GetAwaiter().GetResult();

            using (ZipArchive archive = ZipFile.Open(archivePath, ZipArchiveMode.Update))
            {
                var entry = archive.GetEntry("test.txt");
                Assert.NotNull(entry);
                Assert.Throws<InvalidDataException>(() => entry.Open("wrong"));
            }
        }

        [Fact]
        public void UpdateMode_EncryptedEntry_NoPassword_Throws()
        {
            string archivePath = GetTempArchivePath();
            string password = "correct";
            var entries = new[] { ("test.txt", "content", (string?)password, (ZipArchiveEntry.EncryptionMethod?)ZipArchiveEntry.EncryptionMethod.Aes256) };
            CreateArchiveWithEntries(archivePath, entries, async: false).GetAwaiter().GetResult();

            using (ZipArchive archive = ZipFile.Open(archivePath, ZipArchiveMode.Update))
            {
                var entry = archive.GetEntry("test.txt");
                Assert.NotNull(entry);
                // Opening an encrypted entry without password in update mode should throw
                Assert.ThrowsAny<Exception>(() => entry.Open());
            }
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public async Task UpdateMode_ZipCryptoToAes_PreservesEncryption(bool async)
        {
            // This test verifies that modifying a ZipCrypto entry preserves encryption
            string archivePath = GetTempArchivePath();
            string entryName = "test.txt";
            string originalContent = "Original ZipCrypto Content";
            string modifiedContent = "Modified Content";
            string password = "password123";

            // Create archive with ZipCrypto encrypted entry
            var entries = new[] { (entryName, originalContent, (string?)password, (ZipArchiveEntry.EncryptionMethod?)ZipArchiveEntry.EncryptionMethod.ZipCrypto) };
            await CreateArchiveWithEntries(archivePath, entries, async);

            // Modify in Update mode
            using (ZipArchive archive = await CallZipFileOpen(async, archivePath, ZipArchiveMode.Update))
            {
                ZipArchiveEntry entry = archive.GetEntry(entryName);
                Assert.NotNull(entry);
                Assert.True(entry.IsEncrypted);

                using (Stream stream = entry.Open(password))
                {
                    stream.SetLength(0);
                    byte[] newContent = Encoding.UTF8.GetBytes(modifiedContent);
                    if (async)
                        await stream.WriteAsync(newContent, 0, newContent.Length);
                    else
                        stream.Write(newContent, 0, newContent.Length);
                }
            }

            // Verify entry is still encrypted and can be read with original password
            using (ZipArchive archive = await CallZipFileOpenRead(async, archivePath))
            {
                ZipArchiveEntry entry = archive.GetEntry(entryName);
                Assert.NotNull(entry);
                Assert.True(entry.IsEncrypted);
                await AssertEntryTextEquals(entry, modifiedContent, password, async);
            }
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public async Task UpdateMode_MixedEncryption_ModifyAllEntries(bool async)
        {
            string archivePath = GetTempArchivePath();
            string password = "password123";

            var entries = new[]
            {
                ("plain1.txt", "Plain Content 1", (string?)null, (ZipArchiveEntry.EncryptionMethod?)null),
                ("encrypted1.txt", "Encrypted Content 1", (string?)password, (ZipArchiveEntry.EncryptionMethod?)ZipArchiveEntry.EncryptionMethod.Aes256),
                ("plain2.txt", "Plain Content 2", (string?)null, (ZipArchiveEntry.EncryptionMethod?)null),
                ("encrypted2.txt", "Encrypted Content 2", (string?)password, (ZipArchiveEntry.EncryptionMethod?)ZipArchiveEntry.EncryptionMethod.ZipCrypto)
            };

            await CreateArchiveWithEntries(archivePath, entries, async);

            // Modify all entries in Update mode
            using (ZipArchive archive = await CallZipFileOpen(async, archivePath, ZipArchiveMode.Update))
            {
                // Modify plain1.txt
                ZipArchiveEntry plain1 = archive.GetEntry("plain1.txt");
                Assert.NotNull(plain1);
                Assert.False(plain1.IsEncrypted);
                using (Stream stream = await OpenEntryStream(async, plain1))
                {
                    stream.SetLength(0);
                    byte[] content = Encoding.UTF8.GetBytes("Modified Plain Content 1");
                    if (async)
                        await stream.WriteAsync(content, 0, content.Length);
                    else
                        stream.Write(content, 0, content.Length);
                }

                // Modify encrypted1.txt (AES)
                ZipArchiveEntry encrypted1 = archive.GetEntry("encrypted1.txt");
                Assert.NotNull(encrypted1);
                Assert.True(encrypted1.IsEncrypted);
                using (Stream stream = encrypted1.Open(password))
                {
                    stream.SetLength(0);
                    byte[] content = Encoding.UTF8.GetBytes("Modified Encrypted Content 1");
                    if (async)
                        await stream.WriteAsync(content, 0, content.Length);
                    else
                        stream.Write(content, 0, content.Length);
                }

                // Modify plain2.txt
                ZipArchiveEntry plain2 = archive.GetEntry("plain2.txt");
                Assert.NotNull(plain2);
                Assert.False(plain2.IsEncrypted);
                using (Stream stream = await OpenEntryStream(async, plain2))
                {
                    stream.SetLength(0);
                    byte[] content = Encoding.UTF8.GetBytes("Modified Plain Content 2");
                    if (async)
                        await stream.WriteAsync(content, 0, content.Length);
                    else
                        stream.Write(content, 0, content.Length);
                }

                // Modify encrypted2.txt (ZipCrypto)
                ZipArchiveEntry encrypted2 = archive.GetEntry("encrypted2.txt");
                Assert.NotNull(encrypted2);
                Assert.True(encrypted2.IsEncrypted);
                using (Stream stream = encrypted2.Open(password))
                {
                    stream.SetLength(0);
                    byte[] content = Encoding.UTF8.GetBytes("Modified Encrypted Content 2");
                    if (async)
                        await stream.WriteAsync(content, 0, content.Length);
                    else
                        stream.Write(content, 0, content.Length);
                }
            }

            // Verify all modifications
            using (ZipArchive archive = await CallZipFileOpenRead(async, archivePath))
            {
                var plain1 = archive.GetEntry("plain1.txt");
                Assert.NotNull(plain1);
                Assert.False(plain1.IsEncrypted);
                await AssertEntryTextEquals(plain1, "Modified Plain Content 1", null, async);

                var encrypted1 = archive.GetEntry("encrypted1.txt");
                Assert.NotNull(encrypted1);
                Assert.True(encrypted1.IsEncrypted);
                await AssertEntryTextEquals(encrypted1, "Modified Encrypted Content 1", password, async);

                var plain2 = archive.GetEntry("plain2.txt");
                Assert.NotNull(plain2);
                Assert.False(plain2.IsEncrypted);
                await AssertEntryTextEquals(plain2, "Modified Plain Content 2", null, async);

                var encrypted2 = archive.GetEntry("encrypted2.txt");
                Assert.NotNull(encrypted2);
                Assert.True(encrypted2.IsEncrypted);
                await AssertEntryTextEquals(encrypted2, "Modified Encrypted Content 2", password, async);
            }
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public async Task UpdateMode_DeleteEntryAndModifyAnother(bool async)
        {
            string archivePath = GetTempArchivePath();
            string password = "password123";

            var entries = new[]
            {
                ("keep.txt", "Keep This Content", (string?)password, (ZipArchiveEntry.EncryptionMethod?)ZipArchiveEntry.EncryptionMethod.Aes256),
                ("delete.txt", "Delete This Content", (string?)password, (ZipArchiveEntry.EncryptionMethod?)ZipArchiveEntry.EncryptionMethod.Aes256),
                ("modify.txt", "Original Content", (string?)password, (ZipArchiveEntry.EncryptionMethod?)ZipArchiveEntry.EncryptionMethod.Aes256)
            };

            await CreateArchiveWithEntries(archivePath, entries, async);

            // Verify initial state
            using (ZipArchive archive = await CallZipFileOpenRead(async, archivePath))
            {
                Assert.Equal(3, archive.Entries.Count);
            }

            // Delete one entry and modify another
            using (ZipArchive archive = await CallZipFileOpen(async, archivePath, ZipArchiveMode.Update))
            {
                // Delete delete.txt
                ZipArchiveEntry deleteEntry = archive.GetEntry("delete.txt");
                Assert.NotNull(deleteEntry);
                deleteEntry.Delete();

                // Modify modify.txt
                ZipArchiveEntry modifyEntry = archive.GetEntry("modify.txt");
                Assert.NotNull(modifyEntry);
                using (Stream stream = modifyEntry.Open(password))
                {
                    stream.SetLength(0);
                    byte[] content = Encoding.UTF8.GetBytes("Modified Content");
                    if (async)
                        await stream.WriteAsync(content, 0, content.Length);
                    else
                        stream.Write(content, 0, content.Length);
                }
            }

            // Verify final state
            using (ZipArchive archive = await CallZipFileOpenRead(async, archivePath))
            {
                Assert.Equal(2, archive.Entries.Count);

                // Verify deleted entry is gone
                Assert.Null(archive.GetEntry("delete.txt"));

                // Verify kept entry is unchanged
                var keepEntry = archive.GetEntry("keep.txt");
                Assert.NotNull(keepEntry);
                Assert.True(keepEntry.IsEncrypted);
                await AssertEntryTextEquals(keepEntry, "Keep This Content", password, async);

                // Verify modified entry
                var modifyEntry = archive.GetEntry("modify.txt");
                Assert.NotNull(modifyEntry);
                Assert.True(modifyEntry.IsEncrypted);
                await AssertEntryTextEquals(modifyEntry, "Modified Content", password, async);
            }
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public async Task UpdateMode_DeleteEncryptedAndModifyPlain(bool async)
        {
            string archivePath = GetTempArchivePath();
            string password = "password123";

            var entries = new[]
            {
                ("plain.txt", "Plain Content", (string?)null, (ZipArchiveEntry.EncryptionMethod?)null),
                ("encrypted.txt", "Encrypted Content", (string?)password, (ZipArchiveEntry.EncryptionMethod?)ZipArchiveEntry.EncryptionMethod.Aes256)
            };

            await CreateArchiveWithEntries(archivePath, entries, async);

            // Delete encrypted entry and modify plain entry
            using (ZipArchive archive = await CallZipFileOpen(async, archivePath, ZipArchiveMode.Update))
            {
                // Delete encrypted entry
                ZipArchiveEntry encryptedEntry = archive.GetEntry("encrypted.txt");
                Assert.NotNull(encryptedEntry);
                encryptedEntry.Delete();

                // Modify plain entry
                ZipArchiveEntry plainEntry = archive.GetEntry("plain.txt");
                Assert.NotNull(plainEntry);
                using (Stream stream = await OpenEntryStream(async, plainEntry))
                {
                    stream.SetLength(0);
                    byte[] content = Encoding.UTF8.GetBytes("Modified Plain Content");
                    if (async)
                        await stream.WriteAsync(content, 0, content.Length);
                    else
                        stream.Write(content, 0, content.Length);
                }
            }

            // Verify
            using (ZipArchive archive = await CallZipFileOpenRead(async, archivePath))
            {
                Assert.Single(archive.Entries);
                Assert.Null(archive.GetEntry("encrypted.txt"));

                var plainEntry = archive.GetEntry("plain.txt");
                Assert.NotNull(plainEntry);
                Assert.False(plainEntry.IsEncrypted);
                await AssertEntryTextEquals(plainEntry, "Modified Plain Content", null, async);
            }
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public async Task UpdateMode_DeletePlainAndModifyEncrypted(bool async)
        {
            string archivePath = GetTempArchivePath();
            string password = "password123";

            var entries = new[]
            {
                ("plain.txt", "Plain Content", (string?)null, (ZipArchiveEntry.EncryptionMethod?)null),
                ("encrypted.txt", "Encrypted Content", (string?)password, (ZipArchiveEntry.EncryptionMethod?)ZipArchiveEntry.EncryptionMethod.Aes256)
            };

            await CreateArchiveWithEntries(archivePath, entries, async);

            // Delete plain entry and modify encrypted entry
            using (ZipArchive archive = await CallZipFileOpen(async, archivePath, ZipArchiveMode.Update))
            {
                // Delete plain entry
                ZipArchiveEntry plainEntry = archive.GetEntry("plain.txt");
                Assert.NotNull(plainEntry);
                plainEntry.Delete();

                // Modify encrypted entry
                ZipArchiveEntry encryptedEntry = archive.GetEntry("encrypted.txt");
                Assert.NotNull(encryptedEntry);
                using (Stream stream = encryptedEntry.Open(password))
                {
                    stream.SetLength(0);
                    byte[] content = Encoding.UTF8.GetBytes("Modified Encrypted Content");
                    if (async)
                        await stream.WriteAsync(content, 0, content.Length);
                    else
                        stream.Write(content, 0, content.Length);
                }
            }

            // Verify
            using (ZipArchive archive = await CallZipFileOpenRead(async, archivePath))
            {
                Assert.Single(archive.Entries);
                Assert.Null(archive.GetEntry("plain.txt"));

                var encryptedEntry = archive.GetEntry("encrypted.txt");
                Assert.NotNull(encryptedEntry);
                Assert.True(encryptedEntry.IsEncrypted);
                await AssertEntryTextEquals(encryptedEntry, "Modified Encrypted Content", password, async);
            }
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public async Task UpdateMode_DeleteMultipleEntriesAndModifyRemaining(bool async)
        {
            string archivePath = GetTempArchivePath();
            string password = "password123";

            var entries = new[]
            {
                ("keep1.txt", "Keep 1", (string?)null, (ZipArchiveEntry.EncryptionMethod?)null),
                ("delete1.txt", "Delete 1", (string?)password, (ZipArchiveEntry.EncryptionMethod?)ZipArchiveEntry.EncryptionMethod.Aes256),
                ("keep2.txt", "Keep 2", (string?)password, (ZipArchiveEntry.EncryptionMethod?)ZipArchiveEntry.EncryptionMethod.ZipCrypto),
                ("delete2.txt", "Delete 2", (string?)null, (ZipArchiveEntry.EncryptionMethod?)null),
                ("modify.txt", "Original", (string?)password, (ZipArchiveEntry.EncryptionMethod?)ZipArchiveEntry.EncryptionMethod.Aes128)
            };

            await CreateArchiveWithEntries(archivePath, entries, async);

            // Delete some entries and modify one
            using (ZipArchive archive = await CallZipFileOpen(async, archivePath, ZipArchiveMode.Update))
            {
                archive.GetEntry("delete1.txt")?.Delete();
                archive.GetEntry("delete2.txt")?.Delete();

                ZipArchiveEntry modifyEntry = archive.GetEntry("modify.txt");
                Assert.NotNull(modifyEntry);
                using (Stream stream = modifyEntry.Open(password))
                {
                    stream.SetLength(0);
                    byte[] content = Encoding.UTF8.GetBytes("Modified");
                    if (async)
                        await stream.WriteAsync(content, 0, content.Length);
                    else
                        stream.Write(content, 0, content.Length);
                }
            }

            // Verify
            using (ZipArchive archive = await CallZipFileOpenRead(async, archivePath))
            {
                Assert.Equal(3, archive.Entries.Count);

                Assert.Null(archive.GetEntry("delete1.txt"));
                Assert.Null(archive.GetEntry("delete2.txt"));

                await AssertEntryTextEquals(archive.GetEntry("keep1.txt"), "Keep 1", null, async);
                await AssertEntryTextEquals(archive.GetEntry("keep2.txt"), "Keep 2", password, async);
                await AssertEntryTextEquals(archive.GetEntry("modify.txt"), "Modified", password, async);
            }
        }

        [Theory]
        [MemberData(nameof(EncryptionMethodAndBoolTestData))]
        public async Task UpdateMode_AllEncryptionTypes_EditAllEntries(ZipArchiveEntry.EncryptionMethod encryptionMethod, bool async)
        {
            string archivePath = GetTempArchivePath();
            string password = "password123";

            // Create archive with multiple entries using the same encryption method
            var entries = new[]
            {
                ("entry1.txt", "Content 1", (string?)password, (ZipArchiveEntry.EncryptionMethod?)encryptionMethod),
                ("entry2.txt", "Content 2", (string?)password, (ZipArchiveEntry.EncryptionMethod?)encryptionMethod),
                ("entry3.txt", "Content 3", (string?)password, (ZipArchiveEntry.EncryptionMethod?)encryptionMethod)
            };

            await CreateArchiveWithEntries(archivePath, entries, async);

            // Edit all entries
            using (ZipArchive archive = await CallZipFileOpen(async, archivePath, ZipArchiveMode.Update))
            {
                for (int i = 1; i <= 3; i++)
                {
                    ZipArchiveEntry entry = archive.GetEntry($"entry{i}.txt");
                    Assert.NotNull(entry);
                    Assert.True(entry.IsEncrypted);

                    using (Stream stream = entry.Open(password))
                    {
                        stream.SetLength(0);
                        byte[] content = Encoding.UTF8.GetBytes($"Modified Content {i}");
                        if (async)
                            await stream.WriteAsync(content, 0, content.Length);
                        else
                            stream.Write(content, 0, content.Length);
                    }
                }
            }

            // Verify all modifications
            using (ZipArchive archive = await CallZipFileOpenRead(async, archivePath))
            {
                for (int i = 1; i <= 3; i++)
                {
                    ZipArchiveEntry entry = archive.GetEntry($"entry{i}.txt");
                    Assert.NotNull(entry);
                    Assert.True(entry.IsEncrypted);
                    await AssertEntryTextEquals(entry, $"Modified Content {i}", password, async);
                }
            }
        }

        #endregion
    }
}
