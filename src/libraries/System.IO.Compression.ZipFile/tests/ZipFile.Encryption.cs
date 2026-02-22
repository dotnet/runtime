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
                EncryptionMethod.ZipCrypto,
                EncryptionMethod.Aes128,
                EncryptionMethod.Aes192,
                EncryptionMethod.Aes256
            })
            {
                yield return new object[] { method, false };
                yield return new object[] { method, true };
            }
        }

        [Theory]
        [MemberData(nameof(EncryptionMethodAndBoolTestData))]
        [SkipOnPlatform(TestPlatforms.Browser, "WinZip AES encryption is not supported on Browser")]
        public async Task Encryption_SingleEntry_RoundTrip(EncryptionMethod encryptionMethod, bool async)
        {
            string archivePath = GetTempArchivePath();
            string entryName = "test.txt";
            string content = "Secret Content";
            string password = "password123";

            var entries = new[] { (entryName, content, (string?)password, (EncryptionMethod?)encryptionMethod) };

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
        [SkipOnPlatform(TestPlatforms.Browser, "WinZip AES encryption is not supported on Browser")]
        public async Task Encryption_MultipleEntries_SamePassword_RoundTrip(EncryptionMethod encryptionMethod, bool async)
        {
            string archivePath = GetTempArchivePath();
            string password = "SharedPassword";
            var entries = new[]
            {
                ("file1.txt", "Content 1", (string?)password, (EncryptionMethod?)encryptionMethod),
                ("folder/file2.txt", "Content 2", (string?)password, (EncryptionMethod?)encryptionMethod)
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
        [SkipOnPlatform(TestPlatforms.Browser, "WinZip AES encryption is not supported on Browser")]
        public async Task Encryption_MixedPlainAndEncrypted_RoundTrip(EncryptionMethod encryptionMethod, bool async)
        {
            string archivePath = GetTempArchivePath();
            var entries = new[]
            {
                ("plain.txt", "Plain Content", (string?)null, (EncryptionMethod?)null),
                ("encrypted.txt", "Encrypted Content", (string?)"pass", (EncryptionMethod?)encryptionMethod)
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
        [SkipOnPlatform(TestPlatforms.Browser, "WinZip AES encryption is not supported on Browser")]
        public async Task Encryption_Combinations_RoundTrip(bool async)
        {
            string archivePath = GetTempArchivePath();
            var entries = new[]
            {
                ("zipcrypto.txt", "ZipCrypto Content", (string?)"pass1", (EncryptionMethod?)EncryptionMethod.ZipCrypto),
                ("aes128.txt", "AES128 Content", (string?)"pass2", (EncryptionMethod?)EncryptionMethod.Aes128),
                ("aes256.txt", "AES256 Content", (string?)"pass3", (EncryptionMethod?)EncryptionMethod.Aes256)
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
        [SkipOnPlatform(TestPlatforms.Browser, "WinZip AES encryption is not supported on Browser")]
        public async Task Encryption_LargeFile_RoundTrip(bool async)
        {
            string archivePath = GetTempArchivePath();
            string entryName = "large.bin";
            int size = 1024 * 1024; // 1MB
            byte[] content = new byte[size];
            new Random(42).NextBytes(content);
            string password = "password123";
            var encryptionMethod = EncryptionMethod.Aes256;

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
        [SkipOnPlatform(TestPlatforms.Browser, "WinZip AES encryption is not supported on Browser")]
        public void WrongPassword_Throws_InvalidDataException()
        {
            string archivePath = GetTempArchivePath();
            string password = "correct";
            var entries = new[] { ("test.txt", "content", (string?)password, (EncryptionMethod?)EncryptionMethod.Aes256) };
            CreateArchiveWithEntries(archivePath, entries, async: false).GetAwaiter().GetResult();

            using (ZipArchive archive = ZipFile.OpenRead(archivePath))
            {
                var entry = archive.GetEntry("test.txt");
                Assert.Throws<InvalidDataException>(() => entry.Open("wrong"));
            }
        }

        [Fact]
        public void MissingPassword_Throws_InvalidDataException()
        {
            string archivePath = GetTempArchivePath();
            string password = "correct";
            var entries = new[] { ("test.txt", "content", (string?)password, (EncryptionMethod?)EncryptionMethod.ZipCrypto) };
            CreateArchiveWithEntries(archivePath, entries, async: false).GetAwaiter().GetResult();

            using (ZipArchive archive = ZipFile.OpenRead(archivePath))
            {
                var entry = archive.GetEntry("test.txt");
                Assert.Throws<InvalidDataException>(() => entry.Open());
            }
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public async Task OpeningPlainEntryWithPassword_Throws(bool async)
        {
            string archivePath = GetTempArchivePath();
            var entries = new[] { ("plain.txt", "content", (string?)null, (EncryptionMethod?)null) };
            await CreateArchiveWithEntries(archivePath, entries, async);

            using (ZipArchive archive = await CallZipFileOpenRead(async, archivePath))
            {
                ZipArchiveEntry entry = archive.GetEntry("plain.txt");
                Assert.NotNull(entry);
                Assert.Throws<InvalidDataException>(() => entry.Open("password"));
            }
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        [SkipOnPlatform(TestPlatforms.Browser, "WinZip AES encryption is not supported on Browser")]
        public async Task ExtractToFile_Encrypted_Success(bool async)
        {
            string archivePath = GetTempArchivePath();
            string password = "pass";
            var entries = new[] { ("test.txt", "content", (string?)password, (EncryptionMethod?)EncryptionMethod.Aes256) };
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

        private async Task CreateArchiveWithEntries(string archivePath, (string Name, string Content, string? Password, EncryptionMethod? Encryption)[] entries, bool async)
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
        [SkipOnPlatform(TestPlatforms.Browser, "WinZip AES encryption is not supported on Browser")]
        public async Task UpdateMode_ModifyEncryptedEntry_RoundTrip(EncryptionMethod encryptionMethod, bool async)
        {
            string archivePath = GetTempArchivePath();
            string entryName = "test.txt";
            string originalContent = "Original Content";
            string modifiedContent = "Modified Content After Update";
            string password = "password123";

            // Create archive with encrypted entry
            var entries = new[] { (entryName, originalContent, (string?)password, (EncryptionMethod?)encryptionMethod) };
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
        [SkipOnPlatform(TestPlatforms.Browser, "WinZip AES encryption is not supported on Browser")]
        public async Task UpdateMode_ReadOnlyEncryptedEntry_NoModification(EncryptionMethod encryptionMethod, bool async)
        {
            string archivePath = GetTempArchivePath();
            string entryName = "test.txt";
            string content = "Unmodified Content";
            string password = "password123";

            // Create archive with encrypted entry
            var entries = new[] { (entryName, content, (string?)password, (EncryptionMethod?)encryptionMethod) };
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
        [SkipOnPlatform(TestPlatforms.Browser, "WinZip AES encryption is not supported on Browser")]
        public async Task UpdateMode_MultipleEncryptedEntries_ModifyOne(bool async)
        {
            string archivePath = GetTempArchivePath();
            string password = "password123";
            var encryptionMethod = EncryptionMethod.Aes256;

            var entries = new[]
            {
                ("file1.txt", "Content 1", (string?)password, (EncryptionMethod?)encryptionMethod),
                ("file2.txt", "Content 2", (string?)password, (EncryptionMethod?)encryptionMethod),
                ("file3.txt", "Content 3", (string?)password, (EncryptionMethod?)encryptionMethod)
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
        [SkipOnPlatform(TestPlatforms.Browser, "WinZip AES encryption is not supported on Browser")]
        public async Task UpdateMode_MixedEncryption_ModifyEncrypted(bool async)
        {
            string archivePath = GetTempArchivePath();
            string password = "password123";

            var entries = new[]
            {
                ("plain.txt", "Plain Content", (string?)null, (EncryptionMethod?)null),
                ("encrypted.txt", "Encrypted Content", (string?)password, (EncryptionMethod?)EncryptionMethod.Aes256)
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
        [SkipOnPlatform(TestPlatforms.Browser, "WinZip AES encryption is not supported on Browser")]
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
            var encryptionMethod = EncryptionMethod.Aes256;

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
        [SkipOnPlatform(TestPlatforms.Browser, "WinZip AES encryption is not supported on Browser")]
        public async Task UpdateMode_EncryptedEntry_EmptyAfterModification(EncryptionMethod encryptionMethod, bool async)
        {
            string archivePath = GetTempArchivePath();
            string entryName = "test.txt";
            string originalContent = "Original Content";
            string password = "password123";

            // Create archive with encrypted entry
            var entries = new[] { (entryName, originalContent, (string?)password, (EncryptionMethod?)encryptionMethod) };
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
        [SkipOnPlatform(TestPlatforms.Browser, "WinZip AES encryption is not supported on Browser")]
        public void UpdateMode_EncryptedEntry_WrongPassword_Throws()
        {
            string archivePath = GetTempArchivePath();
            string password = "correct";
            var entries = new[] { ("test.txt", "content", (string?)password, (EncryptionMethod?)EncryptionMethod.Aes256) };
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
            var entries = new[] { ("test.txt", "content", (string?)password, (EncryptionMethod?)EncryptionMethod.ZipCrypto) };
            CreateArchiveWithEntries(archivePath, entries, async: false).GetAwaiter().GetResult();

            using (ZipArchive archive = ZipFile.Open(archivePath, ZipArchiveMode.Update))
            {
                var entry = archive.GetEntry("test.txt");
                Assert.NotNull(entry);
                // Opening an encrypted entry without password in update mode should throw
                Assert.ThrowsAny<ArgumentException>(() => entry.Open());
            }
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        [SkipOnPlatform(TestPlatforms.Browser, "WinZip AES encryption is not supported on Browser")]
        public async Task UpdateMode_DeleteEntryAndModifyAnother(bool async)
        {
            string archivePath = GetTempArchivePath();
            string password = "password123";

            var entries = new[]
            {
                ("keep.txt", "Keep This Content", (string?)password, (EncryptionMethod?)EncryptionMethod.Aes256),
                ("delete.txt", "Delete This Content", (string?)password, (EncryptionMethod?)EncryptionMethod.Aes256),
                ("modify.txt", "Original Content", (string?)password, (EncryptionMethod?)EncryptionMethod.Aes256)
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
        [SkipOnPlatform(TestPlatforms.Browser, "WinZip AES encryption is not supported on Browser")]
        public async Task UpdateMode_DeleteEncryptedAndModifyPlain(bool async)
        {
            string archivePath = GetTempArchivePath();
            string password = "password123";

            var entries = new[]
            {
                ("plain.txt", "Plain Content", (string?)null, (EncryptionMethod?)null),
                ("encrypted.txt", "Encrypted Content", (string?)password, (EncryptionMethod?)EncryptionMethod.Aes256)
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
        [SkipOnPlatform(TestPlatforms.Browser, "WinZip AES encryption is not supported on Browser")]
        public async Task UpdateMode_DeletePlainAndModifyEncrypted(bool async)
        {
            string archivePath = GetTempArchivePath();
            string password = "password123";

            var entries = new[]
            {
                ("plain.txt", "Plain Content", (string?)null, (EncryptionMethod?)null),
                ("encrypted.txt", "Encrypted Content", (string?)password, (EncryptionMethod?)EncryptionMethod.Aes256)
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
        [SkipOnPlatform(TestPlatforms.Browser, "WinZip AES encryption is not supported on Browser")]
        public async Task UpdateMode_DeleteMultipleEntriesAndModifyRemaining(bool async)
        {
            string archivePath = GetTempArchivePath();
            string password = "password123";

            var entries = new[]
            {
                ("keep1.txt", "Keep 1", (string?)null, (EncryptionMethod?)null),
                ("delete1.txt", "Delete 1", (string?)password, (EncryptionMethod?)EncryptionMethod.Aes256),
                ("keep2.txt", "Keep 2", (string?)password, (EncryptionMethod?)EncryptionMethod.ZipCrypto),
                ("delete2.txt", "Delete 2", (string?)null, (EncryptionMethod?)null),
                ("modify.txt", "Original", (string?)password, (EncryptionMethod?)EncryptionMethod.Aes128)
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
        [SkipOnPlatform(TestPlatforms.Browser, "WinZip AES encryption is not supported on Browser")]
        public async Task UpdateMode_AllEncryptionTypes_EditAllEntries(EncryptionMethod encryptionMethod, bool async)
        {
            string archivePath = GetTempArchivePath();
            string password = "password123";

            // Create archive with multiple entries using the same encryption method
            var entries = new[]
            {
                ("entry1.txt", "Content 1", (string?)password, (EncryptionMethod?)encryptionMethod),
                ("entry2.txt", "Content 2", (string?)password, (EncryptionMethod?)encryptionMethod),
                ("entry3.txt", "Content 3", (string?)password, (EncryptionMethod?)encryptionMethod)
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

        #region CompressionMethod Property Tests for Encrypted Entries

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        [SkipOnPlatform(TestPlatforms.Browser, "WinZip AES encryption is not supported on Browser")]
        public async Task CompressionMethod_AesEncryptedEntries_ReturnsActualCompressionMethod(bool async)
        {
            string archivePath = GetTempArchivePath();
            string password = "password123";

            // Create archive with entries using different AES strengths
            var entries = new[]
            {
                ("aes128.txt", "AES-128 content", (string?)password, (EncryptionMethod?)EncryptionMethod.Aes128),
                ("aes192.txt", "AES-192 content", (string?)password, (EncryptionMethod?)EncryptionMethod.Aes192),
                ("aes256.txt", "AES-256 content", (string?)password, (EncryptionMethod?)EncryptionMethod.Aes256),
                ("zipcrypto.txt", "ZipCrypto content", (string?)password, (EncryptionMethod?)EncryptionMethod.ZipCrypto),
                ("plain.txt", "Plain content", (string?)null, (EncryptionMethod?)null)
            };

            await CreateArchiveWithEntries(archivePath, entries, async);

            // Verify CompressionMethod without opening entry streams
            using (ZipArchive archive = await CallZipFileOpenRead(async, archivePath))
            {
                // AES entries should report the actual compression method from the AES extra field (Deflate)
                Assert.Equal(ZipCompressionMethod.Deflate, archive.GetEntry("aes128.txt")!.CompressionMethod);
                Assert.Equal(ZipCompressionMethod.Deflate, archive.GetEntry("aes192.txt")!.CompressionMethod);
                Assert.Equal(ZipCompressionMethod.Deflate, archive.GetEntry("aes256.txt")!.CompressionMethod);

                // ZipCrypto uses actual compression method (Deflate)
                Assert.Equal(ZipCompressionMethod.Deflate, archive.GetEntry("zipcrypto.txt")!.CompressionMethod);

                // Plain entry uses actual compression method (Deflate)
                Assert.Equal(ZipCompressionMethod.Deflate, archive.GetEntry("plain.txt")!.CompressionMethod);
            }
        }

        #endregion

        #region Zip64 Tests for Encrypted Entries

        [Theory]
        [SkipOnCI("Takes significant time and disk space to create 4GB+ files")]
        [MemberData(nameof(EncryptionMethodAndBoolTestData))]
        public async Task Encryption_TrueZip64_LargeEntry_RoundTrip(EncryptionMethod encryptionMethod, bool async)
        {
            string archivePath = GetTempArchivePath();

            try
            {
                // Skip if insufficient disk space
                long requiredSpace = 5L * 1024 * 1024 * 1024; // 5GB
                string? root = Path.GetPathRoot(Path.GetFullPath(archivePath));
                if (root is null)
                    return;

                DriveInfo drive = new DriveInfo(root);
                if (drive.AvailableFreeSpace < requiredSpace * 2)
                    return;

                string entryName = "zip64_large.bin";
                long size = (long)uint.MaxValue + (1024 * 1024); // Just over 4GB
                string password = "Zip64Password!";
                int bufferSize = 64 * 1024 * 1024; // 64MB buffer

                byte[] buffer = new byte[bufferSize];
                new Random(42).NextBytes(buffer);

                // Create archive
                using (ZipArchive archive = await CallZipFileOpen(async, archivePath, ZipArchiveMode.Create))
                {
                    ZipArchiveEntry entry = archive.CreateEntry(entryName, CompressionLevel.NoCompression);
                    using (Stream s = entry.Open(password, encryptionMethod))
                    {
                        long written = 0;
                        while (written < size)
                        {
                            int toWrite = (int)Math.Min(buffer.Length, size - written);
                            if (async)
                                await s.WriteAsync(buffer.AsMemory(0, toWrite));
                            else
                                s.Write(buffer, 0, toWrite);
                            written += toWrite;
                        }
                    }
                }

                // Verify
                using (ZipArchive archive = await CallZipFileOpenRead(async, archivePath))
                {
                    ZipArchiveEntry entry = archive.GetEntry(entryName);
                    Assert.NotNull(entry);
                    Assert.True(entry.IsEncrypted);
                    Assert.Equal(size, entry.Length);

                    // Only verify first and last chunks to reduce test time
                    using (Stream s = entry.Open(password))
                    {
                        byte[] readBuffer = new byte[bufferSize];

                        // Verify first chunk
                        int firstRead = async
                            ? await s.ReadAsync(readBuffer)
                            : s.Read(readBuffer);
                        Assert.Equal(bufferSize, firstRead);
                        Assert.True(readBuffer.AsSpan().SequenceEqual(buffer.AsSpan()));

                        // Skip to near the end (seek not supported, so we read through)
                        long toSkip = size - (2L * bufferSize);
                        while (toSkip > 0)
                        {
                            int skipRead = async
                                ? await s.ReadAsync(readBuffer.AsMemory(0, (int)Math.Min(bufferSize, toSkip)))
                                : s.Read(readBuffer, 0, (int)Math.Min(bufferSize, toSkip));
                            if (skipRead == 0) break;
                            toSkip -= skipRead;
                        }

                        // Verify we can still read (stream integrity)
                        int lastRead = async
                            ? await s.ReadAsync(readBuffer)
                            : s.Read(readBuffer);
                        Assert.True(lastRead > 0);
                    }
                }
            }
            finally
            {
                if (File.Exists(archivePath))
                    File.Delete(archivePath);
            }
        }

        [Theory]
        [SkipOnCI("Takes significant time and disk space to create 4GB+ files")]
        [MemberData(nameof(EncryptionMethodAndBoolTestData))]
        public async Task Encryption_TrueZip64_LargeEntry_UpdateMode_Throws(EncryptionMethod encryptionMethod, bool async)
        {
            string archivePath = GetTempArchivePath();

            try
            {
                long requiredSpace = 5L * 1024 * 1024 * 1024;
                string? root = Path.GetPathRoot(Path.GetFullPath(archivePath));
                if (root is null)
                    return;

                DriveInfo drive = new DriveInfo(root);
                if (drive.AvailableFreeSpace < requiredSpace * 2)
                    return;

                string entryName = "zip64_large.bin";
                long size = (long)uint.MaxValue + (1024 * 1024);
                string password = "Zip64Password!";
                int bufferSize = 64 * 1024 * 1024;

                byte[] buffer = new byte[bufferSize];
                new Random(42).NextBytes(buffer);

                using (ZipArchive archive = await CallZipFileOpen(async, archivePath, ZipArchiveMode.Create))
                {
                    ZipArchiveEntry entry = archive.CreateEntry(entryName, CompressionLevel.NoCompression);
                    using (Stream s = entry.Open(password, encryptionMethod))
                    {
                        long written = 0;
                        while (written < size)
                        {
                            int toWrite = (int)Math.Min(buffer.Length, size - written);
                            if (async)
                                await s.WriteAsync(buffer.AsMemory(0, toWrite));
                            else
                                s.Write(buffer, 0, toWrite);
                            written += toWrite;
                        }
                    }
                }

                using (ZipArchive archive = await CallZipFileOpen(async, archivePath, ZipArchiveMode.Update))
                {
                    ZipArchiveEntry entry = archive.GetEntry(entryName);
                    Assert.NotNull(entry);
                    Assert.True(entry.IsEncrypted);
                    Assert.Throws<InvalidDataException>(() => entry.Open(password));
                }
            }
            finally
            {
                if (File.Exists(archivePath))
                    File.Delete(archivePath);
            }
        }

        #endregion

        #region ExtractToFile Tests for Encrypted Entries

        [Theory]
        [MemberData(nameof(EncryptionMethodAndBoolTestData))]
        [SkipOnPlatform(TestPlatforms.Browser, "WinZip AES encryption is not supported on Browser")]
        public async Task ExtractToFile_EncryptedEntry_Success(EncryptionMethod encryptionMethod, bool async)
        {
            string archivePath = GetTempArchivePath();
            string password = "TestPassword123";
            string content = "Encrypted content for ExtractToFile test";
            var entries = new[] { ("encrypted.txt", content, (string?)password, (EncryptionMethod?)encryptionMethod) };
            await CreateArchiveWithEntries(archivePath, entries, async);

            using (ZipArchive archive = await CallZipFileOpenRead(async, archivePath))
            {
                ZipArchiveEntry entry = archive.GetEntry("encrypted.txt");
                Assert.NotNull(entry);
                Assert.True(entry.IsEncrypted);

                string destFile = GetTestFilePath();

                if (async)
                {
                    await entry.ExtractToFileAsync(destFile, overwrite: false, password: password);
                    Assert.Equal(content, await File.ReadAllTextAsync(destFile));
                }
                else
                {
                    entry.ExtractToFile(destFile, overwrite: false, password: password);
                    Assert.Equal(content, File.ReadAllText(destFile));
                }
            }
        }

        [Theory]
        [MemberData(nameof(EncryptionMethodAndBoolTestData))]
        [SkipOnPlatform(TestPlatforms.Browser, "WinZip AES encryption is not supported on Browser")]
        public async Task ExtractToFile_EncryptedEntry_Overwrite_Success(EncryptionMethod encryptionMethod, bool async)
        {
            string archivePath = GetTempArchivePath();
            string password = "TestPassword123";
            string content = "Updated encrypted content";
            var entries = new[] { ("encrypted.txt", content, (string?)password, (EncryptionMethod?)encryptionMethod) };
            await CreateArchiveWithEntries(archivePath, entries, async);

            string destFile = GetTestFilePath();
            // Create an existing file to be overwritten
            File.WriteAllText(destFile, "Original content to be overwritten");

            using (ZipArchive archive = await CallZipFileOpenRead(async, archivePath))
            {
                ZipArchiveEntry entry = archive.GetEntry("encrypted.txt");
                Assert.NotNull(entry);

                if (async)
                {
                    await entry.ExtractToFileAsync(destFile, overwrite: true, password: password);
                    Assert.Equal(content, await File.ReadAllTextAsync(destFile));
                }
                else
                {
                    entry.ExtractToFile(destFile, overwrite: true, password: password);
                    Assert.Equal(content, File.ReadAllText(destFile));
                }
            }
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        [SkipOnPlatform(TestPlatforms.Browser, "WinZip AES encryption is not supported on Browser")]
        public async Task ExtractToFile_EncryptedEntry_WrongPassword_Throws(bool async)
        {
            string archivePath = GetTempArchivePath();
            string password = "CorrectPassword";
            var entries = new[] { ("encrypted.txt", "content", (string?)password, (EncryptionMethod?)EncryptionMethod.Aes256) };
            await CreateArchiveWithEntries(archivePath, entries, async);

            using (ZipArchive archive = await CallZipFileOpenRead(async, archivePath))
            {
                ZipArchiveEntry entry = archive.GetEntry("encrypted.txt");
                Assert.NotNull(entry);
                string destFile = GetTestFilePath();

                if (async)
                {
                    await Assert.ThrowsAsync<InvalidDataException>(() => entry.ExtractToFileAsync(destFile, overwrite: false, password: "WrongPassword"));
                }
                else
                {
                    Assert.Throws<InvalidDataException>(() => entry.ExtractToFile(destFile, overwrite: false, password: "WrongPassword"));
                }
            }
        }

        #endregion

        #region ExtractToDirectory Tests for Encrypted Entries

        [Theory]
        [MemberData(nameof(EncryptionMethodAndBoolTestData))]
        [SkipOnPlatform(TestPlatforms.Browser, "WinZip AES encryption is not supported on Browser")]
        public async Task ExtractToDirectory_MultipleEncryptedEntries_SamePassword_Success(EncryptionMethod encryptionMethod, bool async)
        {
            string archivePath = GetTempArchivePath();
            string password = "SharedPassword";
            var entries = new[]
            {
                ("file1.txt", "Content 1", (string?)password, (EncryptionMethod?)encryptionMethod),
                ("file2.txt", "Content 2", (string?)password, (EncryptionMethod?)encryptionMethod),
                ("subfolder/file3.txt", "Content 3", (string?)password, (EncryptionMethod?)encryptionMethod),
                ("subfolder/nested/file4.txt", "Content 4", (string?)password, (EncryptionMethod?)encryptionMethod)
            };
            await CreateArchiveWithEntries(archivePath, entries, async);

            using TempDirectory tempDir = new TempDirectory(GetTestFilePath());

            using (ZipArchive archive = await CallZipFileOpenRead(async, archivePath))
            {
                if (async)
                {
                    await archive.ExtractToDirectoryAsync(tempDir.Path, overwriteFiles: false, password);
                }
                else
                {
                    archive.ExtractToDirectory(tempDir.Path, overwriteFiles: false, password);
                }
            }

            // Verify all files were extracted correctly
            foreach (var (name, content, _, _) in entries)
            {
                string extractedPath = Path.Combine(tempDir.Path, name.Replace('/', Path.DirectorySeparatorChar));
                Assert.True(File.Exists(extractedPath), $"File {name} should exist");
                Assert.Equal(content, File.ReadAllText(extractedPath));
            }
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        [SkipOnPlatform(TestPlatforms.Browser, "WinZip AES encryption is not supported on Browser")]
        public async Task ExtractToDirectory_MultipleEntries_DifferentPasswords_Throws(bool async)
        {
            string archivePath = GetTempArchivePath();
            // Create entries with different passwords
            var entries = new[]
            {
                ("file1.txt", "Content 1", (string?)"Password1", (EncryptionMethod?)EncryptionMethod.Aes256),
                ("file2.txt", "Content 2", (string?)"Password2", (EncryptionMethod?)EncryptionMethod.Aes256),
                ("file3.txt", "Content 3", (string?)"Password3", (EncryptionMethod?)EncryptionMethod.Aes256)
            };
            await CreateArchiveWithEntries(archivePath, entries, async);

            using TempDirectory tempDir = new TempDirectory(GetTestFilePath());

            using (ZipArchive archive = await CallZipFileOpenRead(async, archivePath))
            {
                // Using Password1 should fail for file2 and file3
                if (async)
                {
                    await Assert.ThrowsAsync<InvalidDataException>(() =>
                        archive.ExtractToDirectoryAsync(tempDir.Path, overwriteFiles: false, "Password1"));
                }
                else
                {
                    Assert.Throws<InvalidDataException>(() =>
                        archive.ExtractToDirectory(tempDir.Path, overwriteFiles: false, "Password1"));
                }
            }
        }

        [Theory]
        [MemberData(nameof(EncryptionMethodAndBoolTestData))]
        [SkipOnPlatform(TestPlatforms.Browser, "WinZip AES encryption is not supported on Browser")]
        public async Task ExtractToDirectory_EncryptedWithOverwrite_Success(EncryptionMethod encryptionMethod, bool async)
        {
            string archivePath = GetTempArchivePath();
            string password = "TestPassword";
            var entries = new[]
            {
                ("file1.txt", "New Content 1", (string?)password, (EncryptionMethod?)encryptionMethod),
                ("file2.txt", "New Content 2", (string?)password, (EncryptionMethod?)encryptionMethod)
            };
            await CreateArchiveWithEntries(archivePath, entries, async);

            using TempDirectory tempDir = new TempDirectory(GetTestFilePath());

            // Create existing files to be overwritten
            File.WriteAllText(Path.Combine(tempDir.Path, "file1.txt"), "Old Content 1");
            File.WriteAllText(Path.Combine(tempDir.Path, "file2.txt"), "Old Content 2");

            using (ZipArchive archive = await CallZipFileOpenRead(async, archivePath))
            {
                if (async)
                {
                    await archive.ExtractToDirectoryAsync(tempDir.Path, overwriteFiles: true, password);
                }
                else
                {
                    archive.ExtractToDirectory(tempDir.Path, overwriteFiles: true, password);
                }
            }

            // Verify files were overwritten with new content
            Assert.Equal("New Content 1", File.ReadAllText(Path.Combine(tempDir.Path, "file1.txt")));
            Assert.Equal("New Content 2", File.ReadAllText(Path.Combine(tempDir.Path, "file2.txt")));
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        [SkipOnPlatform(TestPlatforms.Browser, "WinZip AES encryption is not supported on Browser")]
        public async Task ExtractToDirectory_EncryptedWithoutOverwrite_ExistingFile_Throws(bool async)
        {
            string archivePath = GetTempArchivePath();
            string password = "TestPassword";
            var entries = new[]
            {
                ("file1.txt", "New Content", (string?)password, (EncryptionMethod?)EncryptionMethod.Aes256)
            };
            await CreateArchiveWithEntries(archivePath, entries, async);

            using TempDirectory tempDir = new TempDirectory(GetTestFilePath());

            // Create existing file that should not be overwritten
            File.WriteAllText(Path.Combine(tempDir.Path, "file1.txt"), "Existing Content");

            using (ZipArchive archive = await CallZipFileOpenRead(async, archivePath))
            {
                if (async)
                {
                    await Assert.ThrowsAsync<IOException>(() =>
                        archive.ExtractToDirectoryAsync(tempDir.Path, overwriteFiles: false, password));
                }
                else
                {
                    Assert.Throws<IOException>(() =>
                        archive.ExtractToDirectory(tempDir.Path, overwriteFiles: false, password));
                }
            }

            // Verify existing file was not modified
            Assert.Equal("Existing Content", File.ReadAllText(Path.Combine(tempDir.Path, "file1.txt")));
        }

        #endregion

        #region Open(FileAccess, ...) Tests

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public async Task Open_FileAccess_ReadMode_WriteAccess_Throws(bool async)
        {
            string archivePath = GetTempArchivePath();
            string password = "secret";
            var entries = new[] { ("test.txt", "content", (string?)password, (EncryptionMethod?)EncryptionMethod.ZipCrypto) };
            await CreateArchiveWithEntries(archivePath, entries, async);

            using (ZipArchive archive = await CallZipFileOpenRead(async, archivePath))
            {
                ZipArchiveEntry entry = archive.GetEntry("test.txt");
                Assert.NotNull(entry);

                if (async)
                {
                    await Assert.ThrowsAsync<InvalidOperationException>(() => entry.OpenAsync(FileAccess.Write, password));
                    await Assert.ThrowsAsync<InvalidOperationException>(() => entry.OpenAsync(FileAccess.ReadWrite, password));
                }
                else
                {
                    Assert.Throws<InvalidOperationException>(() => entry.Open(FileAccess.Write, password));
                    Assert.Throws<InvalidOperationException>(() => entry.Open(FileAccess.ReadWrite, password));
                }
            }
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        [SkipOnPlatform(TestPlatforms.Browser, "WinZip AES encryption is not supported on Browser")]
        public async Task Open_FileAccess_CreateMode_InvalidAccess_Throws(bool async)
        {
            string archivePath = GetTempArchivePath();

            using (ZipArchive archive = await CallZipFileOpen(async, archivePath, ZipArchiveMode.Create))
            {
                ZipArchiveEntry entry = archive.CreateEntry("test.txt");

                if (async)
                {
                    // Read access in create mode throws
                    await Assert.ThrowsAsync<InvalidOperationException>(() => entry.OpenAsync(FileAccess.Read, "password"));
                    // Encryption without password throws
                    await Assert.ThrowsAsync<ArgumentNullException>(() => entry.OpenAsync(FileAccess.Write, null!, EncryptionMethod.Aes256));
                    await Assert.ThrowsAsync<ArgumentNullException>(() => entry.OpenAsync(FileAccess.Write, "", EncryptionMethod.Aes256));
                }
                else
                {
                    Assert.Throws<InvalidOperationException>(() => entry.Open(FileAccess.Read, "password"));
                    Assert.Throws<InvalidOperationException>(() => entry.Open(FileAccess.Write, null!, EncryptionMethod.Aes256));
                    Assert.Throws<IOException>(() => entry.Open(FileAccess.Write, "", EncryptionMethod.Aes256));
                }
            }
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        [SkipOnPlatform(TestPlatforms.Browser, "WinZip AES encryption is not supported on Browser")]
        public async Task Open_FileAccess_UpdateMode_EncryptedEntry_NoPassword_Throws(bool async)
        {
            string archivePath = GetTempArchivePath();
            string password = "secret";
            var entries = new[] { ("test.txt", "content", (string?)password, (EncryptionMethod?)EncryptionMethod.Aes256) };
            await CreateArchiveWithEntries(archivePath, entries, async);

            using (ZipArchive archive = await CallZipFileOpen(async, archivePath, ZipArchiveMode.Update))
            {
                ZipArchiveEntry entry = archive.GetEntry("test.txt");
                Assert.NotNull(entry);
                Assert.True(entry.IsEncrypted);

                if (async)
                {
                    await Assert.ThrowsAnyAsync<ArgumentNullException>(() => entry.OpenAsync(FileAccess.Read, null!));
                    await Assert.ThrowsAnyAsync<ArgumentNullException>(() => entry.OpenAsync(FileAccess.Read, ""));
                    await Assert.ThrowsAnyAsync<ArgumentException>(() => entry.OpenAsync(FileAccess.ReadWrite, null!));
                    await Assert.ThrowsAnyAsync<ArgumentException>(() => entry.OpenAsync(FileAccess.ReadWrite, ""));
                }
                else
                {
                    Assert.ThrowsAny<ArgumentNullException>(() => entry.Open(FileAccess.Read, null!));
                    Assert.ThrowsAny<ArgumentNullException>(() => entry.Open(FileAccess.Read, ""));
                    Assert.ThrowsAny<ArgumentException>(() => entry.Open(FileAccess.ReadWrite, null!));
                    Assert.ThrowsAny<ArgumentException>(() => entry.Open(FileAccess.ReadWrite, ""));
                }
            }
        }

        #endregion

        #region CreateEntryFromFile Overload Tests

        public static IEnumerable<object[]> CreateEntryFromFile_Encrypted_TestData()
        {
            foreach (var row in EncryptionMethodAndBoolTestData())
            {
                var method = (EncryptionMethod)row[0];
                var async = (bool)row[1];

                yield return new object[] { method, false, async }; // no compression overload
                yield return new object[] { method, true, async };  // compression overload
            }
        }

        [Theory]
        [MemberData(nameof(CreateEntryFromFile_Encrypted_TestData))]
        [SkipOnPlatform(TestPlatforms.Browser, "WinZip AES encryption is not supported on Browser")]
        public async Task CreateEntryFromFile_Encrypted_RoundTrip(EncryptionMethod encryptionMethod, bool useCompression, bool async)
        {
            string archivePath = GetTempArchivePath();
            string sourcePath = GetTestFilePath();
            string entryName = useCompression ? "fromfile-compressed.txt" : "fromfile.txt";
            string password = "password123";
            string content = useCompression ? "Secret content with compression from file" : "Secret content from file";

            if (async)
                await File.WriteAllTextAsync(sourcePath, content, Encoding.UTF8);
            else
                File.WriteAllText(sourcePath, content, Encoding.UTF8);

            using (ZipArchive archive = await CallZipFileOpen(async, archivePath, ZipArchiveMode.Create))
            {
                if (async)
                {
                    if (useCompression)
                        await archive.CreateEntryFromFileAsync(sourcePath, entryName, CompressionLevel.Optimal, password, encryptionMethod);
                    else
                        await archive.CreateEntryFromFileAsync(sourcePath, entryName, password, encryptionMethod);
                }
                else
                {
                    if (useCompression)
                        archive.CreateEntryFromFile(sourcePath, entryName, CompressionLevel.Optimal, password, encryptionMethod);
                    else
                        archive.CreateEntryFromFile(sourcePath, entryName, password, encryptionMethod);
                }
            }

            using (ZipArchive archive = await CallZipFileOpenRead(async, archivePath))
            {
                ZipArchiveEntry entry = archive.GetEntry(entryName);
                Assert.NotNull(entry);
                Assert.True(entry.IsEncrypted);

                await AssertEntryTextEquals(entry, content, password, async);
            }
        }

        #endregion

        #region Browser Platform Tests

        public static IEnumerable<object[]> AesEncryptionMethodAndBoolTestData()
        {
            foreach (var method in new[]
            {
                EncryptionMethod.Aes128,
                EncryptionMethod.Aes192,
                EncryptionMethod.Aes256
            })
            {
                yield return new object[] { method, false };
                yield return new object[] { method, true };
            }
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        [PlatformSpecific(TestPlatforms.Browser)]
        public async Task Browser_ZipCrypto_Encryption_Works(bool async)
        {
            string archivePath = GetTempArchivePath();
            string entryName = "test.txt";
            string content = "ZipCrypto Content on Browser";
            string password = "password123";

            using (ZipArchive archive = await CallZipFileOpen(async, archivePath, ZipArchiveMode.Create))
            {
                ZipArchiveEntry entry = archive.CreateEntry(entryName);
                using (Stream s = entry.Open(password, EncryptionMethod.ZipCrypto))
                using (StreamWriter w = new StreamWriter(s, Encoding.UTF8))
                {
                    if (async)
                        await w.WriteAsync(content);
                    else
                        w.Write(content);
                }
            }

            using (ZipArchive archive = await CallZipFileOpenRead(async, archivePath))
            {
                ZipArchiveEntry entry = archive.GetEntry(entryName);
                Assert.NotNull(entry);
                Assert.True(entry.IsEncrypted);
                await AssertEntryTextEquals(entry, content, password, async);
            }
        }

        [Theory]
        [MemberData(nameof(AesEncryptionMethodAndBoolTestData))]
        [PlatformSpecific(TestPlatforms.Browser)]
        public async Task Browser_AesEncryption_Throws_PlatformNotSupportedException(EncryptionMethod encryptionMethod, bool async)
        {
            string archivePath = GetTempArchivePath();
            string entryName = "test.txt";
            string password = "password123";

            using (ZipArchive archive = await CallZipFileOpen(async, archivePath, ZipArchiveMode.Create))
            {
                ZipArchiveEntry entry = archive.CreateEntry(entryName);
                Assert.Throws<PlatformNotSupportedException>(() => entry.Open(password, encryptionMethod));
            }
        }

        #endregion

    }
}
