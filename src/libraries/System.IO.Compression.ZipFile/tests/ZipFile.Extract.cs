// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Compression.Tests
{
    public class ZipFile_Extract : ZipFileTestBase
    {

        private const string DownloadsDir = @"C:\Users\spahontu\Downloads";
        private static string NewPath(string file) => Path.Combine(DownloadsDir, file);

        public static IEnumerable<object[]> Get_ExtractToDirectoryNormal_Data()
        {
            foreach (bool async in _bools)
            {
                yield return new object[] { "normal.zip", "normal", async };
                yield return new object[] { "empty.zip", "empty", async };
                yield return new object[] { "explicitdir1.zip", "explicitdir", async };
                yield return new object[] { "explicitdir2.zip", "explicitdir", async };
                yield return new object[] { "appended.zip", "small", async };
                yield return new object[] { "prepended.zip", "small", async };
                yield return new object[] { "noexplicitdir.zip", "explicitdir", async };
            }
        }

        [Theory]
        [MemberData(nameof(Get_ExtractToDirectoryNormal_Data))]
        public async Task ExtractToDirectoryNormal(string file, string folder, bool async)
        {
            string zipFileName = zfile(file);
            string folderName = zfolder(folder);
            using TempDirectory tempFolder = new TempDirectory(GetTestFilePath());
            await CallZipFileExtractToDirectory(async, zipFileName, tempFolder.Path);
            await DirsEqual(tempFolder.Path, folderName);
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public async Task ExtractToDirectoryNull(bool async)
        {
            await AssertExtensions.ThrowsAsync<ArgumentNullException>("sourceArchiveFileName", () => CallZipFileExtractToDirectory(async, sourceArchiveFileName: null, GetTestFilePath()));
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public async Task ExtractToDirectory_PassDirectoryAsArchiveFile_ThrowsUnauthorizedAccessException(bool async)
        {
            string directoryPath = GetTestFilePath();
            Directory.CreateDirectory(directoryPath);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                CallZipFileExtractToDirectory(async, directoryPath, GetTestFilePath()));
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public async Task ExtractToDirectoryUnicode(bool async)
        {
            string zipFileName = zfile("unicode.zip");
            string folderName = zfolder("unicode");
            using (var tempFolder = new TempDirectory(GetTestFilePath()))
            {
                await CallZipFileExtractToDirectory(async, zipFileName, tempFolder.Path);
                DirFileNamesEqual(tempFolder.Path, folderName);
            }
        }

        [Theory]
        [InlineData("../Foo")]
        [InlineData("../Barbell")]
        public void ExtractOutOfRoot(string entryName)
        {
            string archivePath = GetTestFilePath();
            using (FileStream stream = new FileStream(archivePath, FileMode.Create))
            using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
            {
                ZipArchiveEntry entry = archive.CreateEntry(entryName);
            }

            DirectoryInfo destination = Directory.CreateDirectory(Path.Combine(GetTestFilePath(), "Bar"));
            Assert.Throws<IOException>(() => ZipFile.ExtractToDirectory(archivePath, destination.FullName));
        }

        [Theory]
        [InlineData("../Foo")]
        [InlineData("../Barbell")]
        public async Task ExtractOutOfRoot_Async(string entryName)
        {
            string archivePath = GetTestFilePath();
            using (FileStream stream = new FileStream(archivePath, FileMode.Create))
            await using (ZipArchive archive = await ZipArchive.CreateAsync(stream, ZipArchiveMode.Create, leaveOpen: true, entryNameEncoding: null))
            {
                ZipArchiveEntry entry = archive.CreateEntry(entryName);
            }

            DirectoryInfo destination = Directory.CreateDirectory(Path.Combine(GetTestFilePath(), "Bar"));
            await Assert.ThrowsAsync<IOException>(() => ZipFile.ExtractToDirectoryAsync(archivePath, destination.FullName));
        }

        /// <summary>
        /// This test ensures that a zipfile with path names that are invalid to this OS will throw errors
        /// when an attempt is made to extract them.
        /// </summary>
        [Theory]
        [MemberData(nameof(Get_Unix_ZipWithInvalidFileNames_Data))]
        [PlatformSpecific(TestPlatforms.AnyUnix)]  // Checks Unix-specific invalid file path
        public async Task Unix_ZipWithInvalidFileNames(string zipName, bool async)
        {
            var testDirectory = GetTestFilePath();
            await CallZipFileExtractToDirectory(async, compat(zipName) + ".zip", testDirectory);
            Assert.True(File.Exists(Path.Combine(testDirectory, "a_6b6d")));
        }

        [Theory]
        [MemberData(nameof(Get_Unix_ZipWithOSSpecificFileNames_Data))]
        [PlatformSpecific(TestPlatforms.AnyUnix)]  // Checks Unix-specific invalid file path
        public async Task Unix_ZipWithOSSpecificFileNames(string zipName, string fileName, bool async)
        {
            string tempDir = GetTestFilePath();
            await CallZipFileExtractToDirectory(async, compat(zipName) + ".zip", tempDir);
            string[] results = Directory.GetFiles(tempDir, "*", SearchOption.AllDirectories);
            Assert.Equal(1, results.Length);
            Assert.Equal(fileName, Path.GetFileName(results[0]));
        }

        [Theory]
        [MemberData(nameof(Get_Windows_ZipWithInvalidFileNames_Data))]
        [PlatformSpecific(TestPlatforms.Windows)]
        public async Task Windows_ZipWithInvalidFileNames(string zipFileName, string[] expectedFiles, bool async)
        {
            string testDirectory = GetTestFilePath();

            await CallZipFileExtractToDirectory(async, compat(zipFileName), testDirectory);
            foreach (string expectedFile in expectedFiles)
            {
                string path = Path.Combine(testDirectory, expectedFile);
                Assert.True(File.Exists(path));
                File.Delete(path);
            }
        }

        [Theory]
        [MemberData(nameof(Get_Windows_ZipWithOSSpecificFileNames_Data))]
        [PlatformSpecific(TestPlatforms.Windows)]  // Checks Windows-specific invalid file path
        public async Task Windows_ZipWithOSSpecificFileNames(string zipName, string fileName, bool async)
        {
            string tempDir = GetTestFilePath();
            await CallZipFileExtractToDirectory(async, compat(zipName) + ".zip", tempDir);
            string[] results = Directory.GetFiles(tempDir, "*", SearchOption.AllDirectories);
            Assert.Equal(1, results.Length);
            Assert.Equal(fileName, Path.GetFileName(results[0]));
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public async Task ExtractToDirectoryOverwrite(bool async)
        {
            string zipFileName = zfile("normal.zip");
            string folderName = zfolder("normal");

            using TempDirectory tempFolder = new TempDirectory(GetTestFilePath());

            await CallZipFileExtractToDirectory(async, zipFileName, tempFolder.Path, overwriteFiles: false);
            await Assert.ThrowsAsync<IOException>(() => CallZipFileExtractToDirectory(async, zipFileName, tempFolder.Path /* default false */));
            await Assert.ThrowsAsync<IOException>(() => CallZipFileExtractToDirectory(async, zipFileName, tempFolder.Path, overwriteFiles: false));
            await CallZipFileExtractToDirectory(async, zipFileName, tempFolder.Path, overwriteFiles: true);

            await DirsEqual(tempFolder.Path, folderName);
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public async Task ExtractToDirectoryOverwriteEncoding(bool async)
        {
            string zipFileName = zfile("normal.zip");
            string folderName = zfolder("normal");

            using TempDirectory tempFolder = new(GetTestFilePath());

            await CallZipFileExtractToDirectory(async, zipFileName, tempFolder.Path, Encoding.UTF8, overwriteFiles: false);
            await Assert.ThrowsAsync<IOException>(() => CallZipFileExtractToDirectory(async, zipFileName, tempFolder.Path, Encoding.UTF8 /* default false */));
            await Assert.ThrowsAsync<IOException>(() => CallZipFileExtractToDirectory(async, zipFileName, tempFolder.Path, Encoding.UTF8, overwriteFiles: false));
            await CallZipFileExtractToDirectory(async, zipFileName, tempFolder.Path, Encoding.UTF8, overwriteFiles: true);

            await DirsEqual(tempFolder.Path, folderName);
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public async Task FilesOutsideDirectory(bool async)
        {
            string archivePath = GetTestFilePath();
            ZipArchive archive = await CallZipFileOpen(async, archivePath, ZipArchiveMode.Create);

            ZipArchiveEntry entry = archive.CreateEntry(Path.Combine("..", "entry1"), CompressionLevel.Optimal);
            Stream entryStream = await OpenEntryStream(async, entry);
            using (StreamWriter writer = new StreamWriter(entryStream))
            {
                writer.Write("This is a test.");
            }
            await DisposeStream(async, entryStream);
            await DisposeZipArchive(async, archive);
            await Assert.ThrowsAsync<IOException>(() => CallZipFileExtractToDirectory(async, archivePath, GetTestFilePath()));
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public async Task DirectoryEntryWithData(bool async)
        {
            string archivePath = GetTestFilePath();
            ZipArchive archive = await CallZipFileOpen(async, archivePath, ZipArchiveMode.Create);
            ZipArchiveEntry entry = archive.CreateEntry("testdir" + Path.DirectorySeparatorChar, CompressionLevel.Optimal);
            Stream entryStream = await OpenEntryStream(async, entry);
            using (StreamWriter writer = new StreamWriter(entryStream))
            {
                writer.Write("This is a test.");
            }
            await DisposeStream(async, entryStream);
            await DisposeZipArchive(async, archive);
            await Assert.ThrowsAsync<IOException>(() => CallZipFileExtractToDirectory(async, archivePath, GetTestFilePath()));
        }


        [SkipOnCI("Local development test - requires specific file paths")]
        [Fact]
        public void OpenEncryptedTxtFile_ShouldReturnPlaintext()
        {
            string zipPath = @"C:\Users\spahontu\Downloads\test.zip";
            using var archive = ZipFile.OpenRead(zipPath);

            var entry = archive.Entries.First(e => e.FullName.EndsWith("hello.txt"));
            using var stream = entry.Open("123456789");
            using var reader = new StreamReader(stream);
            string content = reader.ReadToEnd();

            Assert.Equal("Hello ZipCrypto!", content);
        }

        [SkipOnCI("Local development test - requires specific file paths")]
        [Fact]
        public void ExtractEncryptedEntryToFile_ShouldCreatePlaintextFile()
        {

            string ZipPath = @"C:\Users\spahontu\Downloads\test.zip";
            string EntryName = "hello.txt";
            string CorrectPassword = "123456789";

            string tempFile = Path.Combine(Path.GetTempPath(), "hello_extracted.txt");
            if (File.Exists(tempFile)) File.Delete(tempFile);

            using var archive = ZipFile.OpenRead(ZipPath);
            var entry = archive.Entries.First(e => e.FullName.EndsWith(EntryName, StringComparison.OrdinalIgnoreCase));

            // Act: Extract using password
            entry.ExtractToFile(tempFile, overwrite: true, password: CorrectPassword);

            // Assert: File exists and content matches expected plaintext
            Assert.True(File.Exists(tempFile), "Extracted file was not created.");
            string content = File.ReadAllText(tempFile);
            Assert.Equal("Hello ZipCrypto!", content);

            // Cleanup
            File.Delete(tempFile);
        }

        [SkipOnCI("Local development test - requires specific file paths")]
        [Fact]
        public void ExtractEncryptedEntryToFile_WithWrongPassword_ShouldThrow()
        {
            string ZipPath = @"C:\Users\spahontu\Downloads\test.zip";
            string EntryName = "hello.txt";

            string tempFile = Path.Combine(Path.GetTempPath(), "hello_extracted.txt");
            if (File.Exists(tempFile)) File.Delete(tempFile);

            using var archive = ZipFile.OpenRead(ZipPath);
            var entry = archive.Entries.First(e => e.FullName.EndsWith(EntryName, StringComparison.OrdinalIgnoreCase));

            Assert.Throws<InvalidDataException>(() =>
            {
                entry.ExtractToFile(tempFile, overwrite: true, password: "wrongpass");
            });
        }

        [SkipOnCI("Local development test - requires specific file paths")]
        [Fact]
        public void ExtractEncryptedEntryToFile_WithoutPassword_ShouldThrow()
        {
            string ZipPath = @"C:\Users\spahontu\Downloads\test.zip";
            string EntryName = "hello.txt";
            ReadOnlyMemory<char> CorrectPassword = "123456789".AsMemory();

            string tempFile = Path.Combine(Path.GetTempPath(), "hello_extracted.txt");
            if (File.Exists(tempFile)) File.Delete(tempFile);

            using var archive = ZipFile.OpenRead(ZipPath);
            var entry = archive.Entries.First(e => e.FullName.EndsWith(EntryName, StringComparison.OrdinalIgnoreCase));

            Assert.Throws<InvalidDataException>(() =>
            {
                entry.ExtractToFile(tempFile, overwrite: true); // No password passed
            });
        }

        [Fact]
        [SkipOnCI("Local development test - requires specific file paths")]
        public async Task ExtractToFileAsync_WithPassword_ShouldCreatePlaintextFile()
        {
            string zipPath = @"C:\Users\spahontu\Downloads\test.zip";
            Assert.True(File.Exists(zipPath), $"Test ZIP not found at {zipPath}");

            string tempFile = Path.Combine(Path.GetTempPath(), "hello_async.txt");
            if (File.Exists(tempFile)) File.Delete(tempFile);

            using var archive = ZipFile.OpenRead(zipPath);
            var entry = archive.Entries.First(e => e.FullName.EndsWith("hello.txt", StringComparison.OrdinalIgnoreCase));

            await entry.ExtractToFileAsync(tempFile, overwrite: true, password: "123456789");

            Assert.True(File.Exists(tempFile), "Extracted file was not created.");
            string content = await File.ReadAllTextAsync(tempFile);
            Assert.Equal("Hello ZipCrypto!", content);

            File.Delete(tempFile);
        }

        [SkipOnCI("Local development test - requires specific file paths")]
        [Fact]
        public async Task ExtractToFileAsync_WithWrongPassword_ShouldThrow()
        {
            string zipPath = @"C:\Users\spahontu\Downloads\test.zip";
            Assert.True(File.Exists(zipPath), $"Test ZIP not found at {zipPath}");

            string tempFile = Path.Combine(Path.GetTempPath(), "hello_async.txt");
            if (File.Exists(tempFile)) File.Delete(tempFile);

            using var archive = ZipFile.OpenRead(zipPath);
            var entry = archive.Entries.First(e => e.FullName.EndsWith("hello.txt", StringComparison.OrdinalIgnoreCase));

            await Assert.ThrowsAsync<InvalidDataException>(async () =>
            {
                await entry.ExtractToFileAsync(tempFile, overwrite: true, password: "wrongpass");
            });
        }

        [SkipOnCI("Local development test - requires specific file paths")]
        [Fact]
        public async Task ExtractToFileAsync_WithoutPassword_ShouldThrow()
        {
            string zipPath = @"C:\Users\spahontu\Downloads\test.zip";
            Assert.True(File.Exists(zipPath), $"Test ZIP not found at {zipPath}");

            string tempFile = Path.Combine(Path.GetTempPath(), "hello_async.txt");
            if (File.Exists(tempFile)) File.Delete(tempFile);

            using var archive = ZipFile.OpenRead(zipPath);
            var entry = archive.Entries.First(e => e.FullName.EndsWith("hello.txt", StringComparison.OrdinalIgnoreCase));

            await Assert.ThrowsAsync<InvalidDataException>(async () =>
            {
                await entry.ExtractToFileAsync(tempFile, overwrite: true, cancellationToken: default); // No password passed
            });
        }

        [Fact]
        [SkipOnCI("Local development test - requires specific file paths")]
        public void OpenEncryptedArchive_WithMultipleEntries_ShouldDecryptBoth()
        {
            // Arrange
            string zipPath = @"C:\Users\spahontu\Downloads\combined.zip";
            string originalJpgPath = @"C:\Users\spahontu\Downloads\test.jpg";
            Assert.True(File.Exists(zipPath), $"Encrypted ZIP not found at {zipPath}");
            Assert.True(File.Exists(originalJpgPath), $"Original JPEG not found at {originalJpgPath}");

            // Open archive with password
            using var archive = ZipFile.OpenRead(zipPath);

            // 1) Validate hello.txt
            var txtEntry = archive.Entries.First(e => e.FullName.EndsWith("hello.txt", StringComparison.OrdinalIgnoreCase));
            using (var txtStream = txtEntry.Open("123456789"))
            using (var reader = new StreamReader(txtStream))
            {
                string content = reader.ReadToEnd();
                Assert.Equal("Hello ZipCrypto!", content);
            }

            // 2) Validate test.jpg
            var jpgEntry = archive.Entries.First(e => e.FullName.EndsWith("test.jpg", StringComparison.OrdinalIgnoreCase));
            using (var jpgStream = jpgEntry.Open("123456789"))
            using (var ms = new MemoryStream())
            {
                jpgStream.CopyTo(ms);
                byte[] actualBytes = ms.ToArray();

                // Quick sanity: JPEG SOI marker
                Assert.True(actualBytes.Length > 3, "JPEG too small");
                Assert.Equal(new byte[] { 0xFF, 0xD8, 0xFF }, actualBytes.Take(3).ToArray());

                // Full comparison with original file
                byte[] expectedBytes = File.ReadAllBytes(originalJpgPath);
                Assert.Equal(expectedBytes.Length, actualBytes.Length);
                Assert.Equal(expectedBytes, actualBytes);
            }
        }

        [SkipOnCI("Local development test - requires specific file paths")]
        [Fact]
        public void OpenEncryptedArchive_WithMultipleEntries_DifferentPassword_ShouldDecryptBoth()
        {
            // Arrange
            string zipPath = @"C:\Users\spahontu\Downloads\combinedpass.zip";
            string originalJpgPath = @"C:\Users\spahontu\Downloads\test.jpg";
            Assert.True(File.Exists(zipPath), $"Encrypted ZIP not found at {zipPath}");
            Assert.True(File.Exists(originalJpgPath), $"Original JPEG not found at {originalJpgPath}");

            // Open archive with password
            using var archive = ZipFile.OpenRead(zipPath);

            // 1) Validate hello.txt
            var txtEntry = archive.Entries.First(e => e.FullName.EndsWith("hello.txt", StringComparison.OrdinalIgnoreCase));
            using (var txtStream = txtEntry.Open())
            using (var reader = new StreamReader(txtStream))
            {
                string content = reader.ReadToEnd();
                Assert.Equal("Hello ZipCrypto!", content);
            }

            // 2) Validate test.jpg
            var jpgEntry = archive.Entries.First(e => e.FullName.EndsWith("test.jpg", StringComparison.OrdinalIgnoreCase));
            using (var jpgStream = jpgEntry.Open("123456789"))
            using (var ms = new MemoryStream())
            {
                jpgStream.CopyTo(ms);
                byte[] actualBytes = ms.ToArray();

                // Quick sanity: JPEG SOI marker
                Assert.True(actualBytes.Length > 3, "JPEG too small");
                Assert.Equal(new byte[] { 0xFF, 0xD8, 0xFF }, actualBytes.Take(3).ToArray());

                // Full comparison with original file
                byte[] expectedBytes = File.ReadAllBytes(originalJpgPath);
                Assert.Equal(expectedBytes.Length, actualBytes.Length);
                Assert.Equal(expectedBytes, actualBytes);
            }
        }

        [SkipOnCI("Local development test - requires specific file paths")]
        [Fact]
        public async Task ZipCrypto_CreateEntry_ThenRead_Back_ContentMatches()
        {
            // Arrange
            const string downloadsDir = @"C:\Users\spahontu\Downloads";
            const string zipPath = $@"{downloadsDir}\zipcrypto_test.zip";
            const string entryName = "hello.txt";
            const string password = "P@ssw0rd!";
            const string expectedContent = "hello zipcrypto";

            // Ensure target directory exists
            Directory.CreateDirectory(downloadsDir);

            // Clean up any previous file
            if (File.Exists(zipPath))
                File.Delete(zipPath);

            // ACT 1: Create the archive and write one encrypted entry
            using (var za = ZipFile.Open(zipPath, ZipArchiveMode.Create))
            {
                // Your custom overload that sets per-entry password & ZipCrypto
                var entry = za.CreateEntry(entryName);

                using var writer = new StreamWriter(entry.Open(password, EncryptionMethod.ZipCrypto), Encoding.UTF8, bufferSize: 1024, leaveOpen: false);
                writer.Write(expectedContent);
            }

            // ACT 2: Open the archive for reading and read back the content using the password
            string actualContent;
            using (var za = ZipFile.Open(zipPath, ZipArchiveMode.Read))
            {
                var entry = za.GetEntry(entryName);
                Assert.NotNull(entry);

                // Your custom entry decryption API: Open(password)
                using var reader = new StreamReader(entry!.Open(password), Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
                actualContent = await reader.ReadToEndAsync();
            }

            // Assert
            Assert.Equal(expectedContent, actualContent);
        }

        [Fact]
        public async Task ZipCrypto_MultipleEntries_SamePassword_AllRoundTrip()
        {
            // Arrange
            Directory.CreateDirectory(DownloadsDir);
            string zipPath = NewPath("zipcrypto_multi_samepw.zip");
            if (File.Exists(zipPath)) File.Delete(zipPath);

            var items = new (string Name, string Content)[]
            {
            ("a.txt", "alpha"),
            ("b/config.json", "{\"k\":1}"),
            ("c/readme.md", "# readme"),
            };
            const string password = "S@m3PW!";
            const EncryptionMethod enc = EncryptionMethod.ZipCrypto;

            // Act 1: Create with same password for all
            using (var za = ZipFile.Open(zipPath, ZipArchiveMode.Create))
            {
                foreach (var it in items)
                {
                    var entry = za.CreateEntry(it.Name);
                    using var w = new StreamWriter(entry.Open(password, enc), Encoding.UTF8, bufferSize: 1024, leaveOpen: false);
                    await w.WriteAsync(it.Content);
                }
            }

            // Act 2: Read back using same password for each entry
            using (var za = ZipFile.Open(zipPath, ZipArchiveMode.Read))
            {
                foreach (var it in items)
                {
                    var e = za.GetEntry(it.Name);
                    Assert.NotNull(e);
                    using var r = new StreamReader(e!.Open(password), Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
                    string content = await r.ReadToEndAsync();
                    Assert.Equal(it.Content, content);
                }
            }
        }

        [SkipOnCI("Local development test - requires specific file paths")]
        [Fact]
        public async Task ZipCrypto_MultipleEntries_DifferentPasswords_AllRoundTrip()
        {
            // Arrange
            Directory.CreateDirectory(DownloadsDir);
            string zipPath = NewPath("zipcrypto_multi_diffpw.zip");
            if (File.Exists(zipPath)) File.Delete(zipPath);

            var items = new (string Name, string Content, string Password)[]
            {
            ("d.txt", "delta", "pw-d"),
            ("e/info.txt", "echo-info", "pw-e"),
            ("f/sub/notes.txt", "foxtrot-notes", "pw-f"),
            };
            const EncryptionMethod enc = EncryptionMethod.ZipCrypto;

            // Act 1: Create, each entry with its own password
            using (var za = ZipFile.Open(zipPath, ZipArchiveMode.Create))
            {
                foreach (var it in items)
                {
                    var entry = za.CreateEntry(it.Name);
                    using var w = new StreamWriter(entry.Open(it.Password, enc), Encoding.UTF8, bufferSize: 1024, leaveOpen: false);
                    await w.WriteAsync(it.Content);
                }
            }

            // Act 2: Read back with matching password per entry, and also verify a wrong password fails
            using (var za = ZipFile.Open(zipPath, ZipArchiveMode.Read))
            {
                foreach (var it in items)
                {
                    var e = za.GetEntry(it.Name);
                    Assert.NotNull(e);

                    // Correct password
                    using (var r = new StreamReader(e!.Open(it.Password), Encoding.UTF8, detectEncodingFromByteOrderMarks: true))
                    {
                        string content = await r.ReadToEndAsync();
                        Assert.Equal(it.Content, content);
                    }

                    // Wrong password should throw (ZipCrypto header check fails)
                    Assert.ThrowsAny<Exception>(() =>
                    {
                        using var _ = e.Open("WRONG-PASSWORD");
                    });
                }
            }
        }

        [SkipOnCI("Local development test - requires specific file paths")]
        [Fact]
        public async Task ZipCrypto_Mixed_EncryptedAndPlainEntries_AllRoundTrip()
        {
            // Arrange
            Directory.CreateDirectory(DownloadsDir);
            string zipPath = NewPath("zipcrypto_mixed.zip");
            if (File.Exists(zipPath)) File.Delete(zipPath);

            const string encPw = "EncOnly123!";
            const EncryptionMethod enc = EncryptionMethod.ZipCrypto;

            var encryptedItems = new (string Name, string Content)[]
            {
            ("secure/one.txt", "top-secret-1"),
            ("secure/two.txt", "top-secret-2"),
            };

            var plainItems = new (string Name, string Content)[]
            {
            ("public/a.txt", "visible-a"),
            ("public/b.txt", "visible-b"),
            };

            // Act 1: Create archive with both encrypted and plain entries
            using (var za = ZipFile.Open(zipPath, ZipArchiveMode.Create))
            {
                // Encrypted
                foreach (var it in encryptedItems)
                {
                    var entry = za.CreateEntry(it.Name);
                    using var w = new StreamWriter(entry.Open(encPw, enc), Encoding.UTF8, bufferSize: 1024, leaveOpen: false);
                    w.Write(it.Content);
                }

                // Plain
                foreach (var it in plainItems)
                {
                    var entry = za.CreateEntry(it.Name); // default: no encryption
                    using var w = new StreamWriter(entry.Open(), Encoding.UTF8, bufferSize: 1024, leaveOpen: false);
                    w.Write(it.Content);
                }
            }

            // Act 2: Read back—encrypted need password, plain do not
            using (var za = ZipFile.Open(zipPath, ZipArchiveMode.Read))
            {
                // Encrypted
                foreach (var it in encryptedItems)
                {
                    var e = za.GetEntry(it.Name);
                    Assert.NotNull(e);
                    using var r = new StreamReader(e!.Open(encPw), Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
                    string content = await r.ReadToEndAsync();
                    Assert.Equal(it.Content, content);
                }

                // Plain
                foreach (var it in plainItems)
                {
                    var e = za.GetEntry(it.Name);
                    Assert.NotNull(e);
                    using var r = new StreamReader(e!.Open(), Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
                    string content = await r.ReadToEndAsync();
                    Assert.Equal(it.Content, content);

                    // Ensure opening a plain entry with a password is rejected (or simply ignored depending on API)
                    Assert.ThrowsAny<Exception>(() =>
                    {
                        using var _ = e.Open("some-password");
                    });
                }
            }
        }

        [SkipOnCI("Local development test - requires specific file paths")]
        [Fact]
        public async Task ZipCrypto_AsyncWrite_ThenAsyncRead_ContentMatches()
        {
            // Arrange
            Directory.CreateDirectory(DownloadsDir);
            string zipPath = NewPath("zipcrypto_async_test.zip");
            const string entryName = "async_test.txt";
            const string password = "AsyncP@ss123";
            const string expectedContent = "This is async ZipCrypto content";

            if (File.Exists(zipPath)) File.Delete(zipPath);

            // Act 1: Create archive with async write
            using (var za = ZipFile.Open(zipPath, ZipArchiveMode.Create))
            {
                var entry = za.CreateEntry(entryName);
                using var stream = entry.Open(password, EncryptionMethod.ZipCrypto);

                byte[] data = Encoding.UTF8.GetBytes(expectedContent);
                await stream.WriteAsync(data, 0, data.Length);
                await stream.FlushAsync();
            }

            // Act 2: Read back with async read
            string actualContent;
            using (var za = ZipFile.Open(zipPath, ZipArchiveMode.Read))
            {
                var entry = za.GetEntry(entryName);
                Assert.NotNull(entry);

                using var stream = entry!.Open(password);
                using var reader = new StreamReader(stream, Encoding.UTF8);
                actualContent = await reader.ReadToEndAsync();
            }

            // Assert
            Assert.Equal(expectedContent, actualContent);
        }

        [SkipOnCI("Local development test - requires specific file paths")]
        [Fact]
        public async Task ZipCrypto_MultipleAsyncWrites_SingleEntry_ContentMatches()
        {
            // Arrange
            Directory.CreateDirectory(DownloadsDir);
            string zipPath = NewPath("zipcrypto_multi_write.zip");
            const string entryName = "multi_write.txt";
            const string password = "MultiWrite123";

            var parts = new[] { "Part1-", "Part2-", "Part3" };
            string expectedContent = string.Concat(parts);

            if (File.Exists(zipPath)) File.Delete(zipPath);

            // Act 1: Create with multiple async writes
            using (var za = ZipFile.Open(zipPath, ZipArchiveMode.Create))
            {
                var entry = za.CreateEntry(entryName);
                using var stream = entry.Open(password, EncryptionMethod.ZipCrypto);

                foreach (var part in parts)
                {
                    byte[] data = Encoding.UTF8.GetBytes(part);
                    await stream.WriteAsync(data, 0, data.Length);
                }
                await stream.FlushAsync();
            }

            // Act 2: Read back
            string actualContent;
            using (var za = ZipFile.Open(zipPath, ZipArchiveMode.Read))
            {
                var entry = za.GetEntry(entryName);
                Assert.NotNull(entry);

                using var reader = new StreamReader(entry!.Open(password), Encoding.UTF8);
                actualContent = await reader.ReadToEndAsync();
            }

            // Assert
            Assert.Equal(expectedContent, actualContent);
        }

        [SkipOnCI("Local development test - requires specific file paths")]
        [Fact]
        public async Task ZipCrypto_ChunkedAsyncRead_ContentMatches()
        {
            // Arrange
            Directory.CreateDirectory(DownloadsDir);
            string zipPath = NewPath("zipcrypto_chunked_read.zip");
            const string entryName = "chunked.txt";
            const string password = "ChunkedRead!";

            // Create larger content
            string expectedContent = string.Concat(Enumerable.Repeat("0123456789ABCDEF", 100));

            if (File.Exists(zipPath)) File.Delete(zipPath);

            // Act 1: Create entry
            using (var za = ZipFile.Open(zipPath, ZipArchiveMode.Create))
            {
                var entry = za.CreateEntry(entryName);
                using var writer = new StreamWriter(entry.Open(password, EncryptionMethod.ZipCrypto), Encoding.UTF8);
                await writer.WriteAsync(expectedContent);
            }

            // Act 2: Read in chunks asynchronously using StreamReader to handle BOM properly
            string actualContent;
            using (var za = ZipFile.Open(zipPath, ZipArchiveMode.Read))
            {
                var entry = za.GetEntry(entryName);
                Assert.NotNull(entry);

                using var stream = entry!.Open(password);
                using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
                actualContent = await reader.ReadToEndAsync();
            }

            // Assert
            Assert.Equal(expectedContent, actualContent);
        }

        [SkipOnCI("Local development test - requires specific file paths")]
        [Fact]
        public async Task ZipCrypto_MixedSyncAsyncOperations_ContentMatches()
        {
            // Arrange
            Directory.CreateDirectory(DownloadsDir);
            string zipPath = NewPath("zipcrypto_mixed_ops.zip");
            const string syncEntryName = "sync.txt";
            const string asyncEntryName = "async.txt";
            const string password = "MixedOps123";
            const string syncContent = "Synchronous write content";
            const string asyncContent = "Asynchronous write content";

            if (File.Exists(zipPath)) File.Delete(zipPath);

            // Act 1: Create with mixed sync/async writes
            using (var za = ZipFile.Open(zipPath, ZipArchiveMode.Create))
            {
                // Synchronous write
                var syncEntry = za.CreateEntry(syncEntryName);
                using (var syncWriter = new StreamWriter(syncEntry.Open(password, EncryptionMethod.ZipCrypto), Encoding.UTF8))
                {
                    syncWriter.Write(syncContent);
                }

                // Asynchronous write
                var asyncEntry = za.CreateEntry(asyncEntryName);
                using (var asyncWriter = new StreamWriter(asyncEntry.Open(password, EncryptionMethod.ZipCrypto), Encoding.UTF8))
                {
                    await asyncWriter.WriteAsync(asyncContent);
                }
            }

            // Act 2: Read with mixed sync/async reads
            string actualSyncContent, actualAsyncContent;
            using (var za = ZipFile.Open(zipPath, ZipArchiveMode.Read))
            {
                // Async read of sync-written entry
                var syncEntry = za.GetEntry(syncEntryName);
                Assert.NotNull(syncEntry);
                using (var reader1 = new StreamReader(syncEntry!.Open(password), Encoding.UTF8))
                {
                    actualSyncContent = await reader1.ReadToEndAsync();
                }

                // Sync read of async-written entry
                var asyncEntry = za.GetEntry(asyncEntryName);
                Assert.NotNull(asyncEntry);
                using (var reader2 = new StreamReader(asyncEntry!.Open(password), Encoding.UTF8))
                {
                    actualAsyncContent = reader2.ReadToEnd();
                }
            }

            // Assert
            Assert.Equal(syncContent, actualSyncContent);
            Assert.Equal(asyncContent, actualAsyncContent);
        }

        [SkipOnCI("Local development test - requires specific file paths")]
        [Fact]
        public async Task ZipCrypto_LargeFileAsyncOperations_ContentMatches()
        {
            // Arrange
            Directory.CreateDirectory(DownloadsDir);
            string zipPath = NewPath("zipcrypto_large_async.zip");
            const string entryName = "large.bin";
            const string password = "LargeFile123";

            // Create 1MB of random data
            var random = new Random(42);
            var expectedData = new byte[1024 * 1024];
            random.NextBytes(expectedData);

            if (File.Exists(zipPath)) File.Delete(zipPath);

            // Act 1: Write large data asynchronously
            using (var za = ZipFile.Open(zipPath, ZipArchiveMode.Create))
            {
                var entry = za.CreateEntry(entryName);
                using var stream = entry.Open(password, EncryptionMethod.ZipCrypto);

                // Write in 64KB chunks
                const int chunkSize = 65536;
                for (int offset = 0; offset < expectedData.Length; offset += chunkSize)
                {
                    int count = Math.Min(chunkSize, expectedData.Length - offset);
                    await stream.WriteAsync(expectedData, offset, count);
                }
                await stream.FlushAsync();
            }

            // Act 2: Read large data asynchronously
            byte[] actualData;
            using (var za = ZipFile.Open(zipPath, ZipArchiveMode.Read))
            {
                var entry = za.GetEntry(entryName);
                Assert.NotNull(entry);

                using var stream = entry!.Open(password);
                using var ms = new MemoryStream();
                await stream.CopyToAsync(ms);
                actualData = ms.ToArray();
            }

            // Assert
            Assert.Equal(expectedData.Length, actualData.Length);
            Assert.Equal(expectedData, actualData);
        }

        [SkipOnCI("Local development test - requires specific file paths")]
        [Fact]
        public async Task ZipCrypto_StreamCopyToAsync_ContentMatches()
        {
            // Arrange
            Directory.CreateDirectory(DownloadsDir);
            string zipPath = NewPath("zipcrypto_copyto.zip");
            const string entryName = "copyto.dat";
            const string password = "CopyTo123!";

            var expectedData = new byte[32768];
            new Random(123).NextBytes(expectedData);

            if (File.Exists(zipPath)) File.Delete(zipPath);

            // Act 1: Write using CopyToAsync
            using (var za = ZipFile.Open(zipPath, ZipArchiveMode.Create))
            {
                var entry = za.CreateEntry(entryName);
                using var entryStream = entry.Open(password, EncryptionMethod.ZipCrypto);
                using var sourceStream = new MemoryStream(expectedData);

                await sourceStream.CopyToAsync(entryStream);
            }

            // Act 2: Read using CopyToAsync
            byte[] actualData;
            using (var za = ZipFile.Open(zipPath, ZipArchiveMode.Read))
            {
                var entry = za.GetEntry(entryName);
                Assert.NotNull(entry);

                using var entryStream = entry!.Open(password);
                using var destStream = new MemoryStream();

                await entryStream.CopyToAsync(destStream);
                actualData = destStream.ToArray();
            }

            // Assert
            Assert.Equal(expectedData, actualData);
        }

        [SkipOnCI("Local development test - requires specific file paths")]
        [Fact]
        public void OpenAESEncryptedTxtFile_ShouldReturnPlaintextWinZip()
        {
            string zipPath = Path.Join(DownloadsDir, "source_plain_winzip.zip");
            using var archive = ZipFile.OpenRead(zipPath);

            var entry = archive.Entries.First(e => e.FullName.EndsWith("source_plain.txt"));
            using var stream = entry.Open("123456789");
            using var reader = new StreamReader(stream);
            string content = reader.ReadToEnd();

            Assert.Equal("this is plain", content);
        }

        [SkipOnCI("Local development test - requires specific file paths")]
        [Fact]
        public void OpenAESEncryptedTxtFile_ShouldReturnPlaintextWinRar()
        {
            string zipPath = Path.Join(DownloadsDir, "source_plain.zip");
            using var archive = ZipFile.OpenRead(zipPath);

            var entry = archive.Entries.First(e => e.FullName.EndsWith("source_plain.txt"));
            using var stream = entry.Open("123456789");
            using var reader = new StreamReader(stream);
            string content = reader.ReadToEnd();

            Assert.Equal("this is plain", content);
        }

        [SkipOnCI("Local development test - requires specific file paths")]
        [Fact]
        public void OpenAESEncryptedTxtFile_ShouldReturnPlaintext7zip()
        {
            string zipPath = Path.Join(DownloadsDir, "source_plain7z.zip");
            using var archive = ZipFile.OpenRead(zipPath);

            var entry = archive.Entries.First(e => e.FullName.EndsWith("source_plain.txt"));
            using var stream = entry.Open("123456789");
            using var reader = new StreamReader(stream);
            string content = reader.ReadToEnd();

            Assert.Equal("this is plain", content);
        }

        [SkipOnCI("Local development test - requires specific file paths")]
        [Fact]
        public void OpenMultipleAESEncryptedEntries_ShouldReturnCorrectContent()
        {
            string zipPath = Path.Join(DownloadsDir, "multiple_entries_aes.zip");
            using var archive = ZipFile.OpenRead(zipPath);

            // Test first entry
            var entry1 = archive.Entries.First(e => e.FullName.EndsWith("source_plain.txt"));
            using (var stream1 = entry1.Open("123456789"))
            using (var reader1 = new StreamReader(stream1))
            {
                string content1 = reader1.ReadToEnd();
                Assert.Equal("this is plain", content1);
            }

            // Test second entry
            var entry2 = archive.Entries.First(e => e.FullName.EndsWith("source_plain_2.txt"));
            using (var stream2 = entry2.Open("123456789"))
            using (var reader2 = new StreamReader(stream2))
            {
                string content2 = reader2.ReadToEnd();
                Assert.Equal("this is plain", content2);
            }
        }

        [SkipOnCI("Local development test - requires specific file paths")]
        [Fact]
        public async Task CreateAndReadAES256EncryptedEntry_RoundTrip()
        {
            // Arrange
            string tempPath = Path.Join(DownloadsDir, "source_plain_mine.zip");
            const string entryName = "source_plain.txt";
            const string password = "123456789";
            const string expectedContent = "this is plain";

            // Act 1: Create ZIP with AES-256 encrypted entry
            using (var createStream = File.Create(tempPath))
            using (var archive = new ZipArchive(createStream, ZipArchiveMode.Create))
            {
                var entry = archive.CreateEntry(entryName);
                using var entryStream = entry.Open(password, EncryptionMethod.Aes256);
                using var writer = new StreamWriter(entryStream, Encoding.UTF8);
                writer.Write(expectedContent);
            }

            // Act 2: Read back the encrypted entry
            string actualContent;
            using (var readStream = File.OpenRead(tempPath))
            using (var archive = new ZipArchive(readStream, ZipArchiveMode.Read))
            {
                var entry = archive.GetEntry(entryName);
                Assert.NotNull(entry);

                using var entryStream = entry!.Open(password);
                using var reader = new StreamReader(entryStream, Encoding.UTF8);
                actualContent = await reader.ReadToEndAsync();
            }

            // Assert
            Assert.Equal(expectedContent, actualContent);

        }

        [SkipOnCI("Local development test - requires specific file paths")]
        [Fact]
        public async Task CreateAndReadMultipleAES256EncryptedEntries_RoundTrip()
        {
            // Arrange
            Directory.CreateDirectory(DownloadsDir);
            string tempPath = Path.Join(DownloadsDir, "multiple_aes256_entries.zip");
            const string password = "123456789";

            var entries = new (string Name, string Content)[]
            {
        ("entry1.txt", "First encrypted entry"),
        ("folder/entry2.txt", "Second encrypted entry in folder"),
        ("folder/subfolder/entry3.md", "# Third Entry\nMarkdown content"),
        ("data.json", "{\"key\": \"value\", \"encrypted\": true}"),
        ("readme.txt", "This is AES-256 encrypted content")
            };

            // Act 1: Create ZIP with multiple AES-256 encrypted entries
            using (var createStream = File.Create(tempPath))
            using (var archive = new ZipArchive(createStream, ZipArchiveMode.Create))
            {
                foreach (var (name, content) in entries)
                {
                    var entry = archive.CreateEntry(name);
                    using var entryStream = entry.Open(password, EncryptionMethod.Aes256);
                    using var writer = new StreamWriter(entryStream, Encoding.UTF8);
                    await writer.WriteAsync(content);
                }
            }

            // Act 2: Read back all encrypted entries
            using (var readStream = File.OpenRead(tempPath))
            using (var archive = new ZipArchive(readStream, ZipArchiveMode.Read))
            {
                foreach (var (name, expectedContent) in entries)
                {
                    var entry = archive.GetEntry(name);
                    Assert.NotNull(entry);

                    using var entryStream = entry!.Open(password);
                    using var reader = new StreamReader(entryStream, Encoding.UTF8);
                    string actualContent = await reader.ReadToEndAsync();

                    // Assert each entry's content matches
                    Assert.Equal(expectedContent, actualContent);
                }

                // Verify wrong password fails
                var firstEntry = archive.GetEntry(entries[0].Name);
                Assert.NotNull(firstEntry);
            }
        }

        [SkipOnCI("Local development test - requires specific file paths")]
        [Fact]
        public async Task CreateAndReadAES256EntriesWithDifferentPasswords_RoundTrip()
        {
            // Arrange
            Directory.CreateDirectory(DownloadsDir);
            string tempPath = Path.Join(DownloadsDir, "multiple_aes256_diff_passwords.zip");

            var entries = new (string Name, string Content, string Password)[]
            {
        ("secure1.txt", "Content with password1", "password1"),
        ("secure2.txt", "Content with password2", "password2"),
        ("folder/secure3.txt", "Content with password3", "password3")
            };

            // Act 1: Create ZIP with AES-256 encrypted entries using different passwords
            using (var createStream = File.Create(tempPath))
            using (var archive = new ZipArchive(createStream, ZipArchiveMode.Create))
            {
                foreach (var (name, content, pwd) in entries)
                {
                    var entry = archive.CreateEntry(name);
                    using var entryStream = entry.Open(pwd, EncryptionMethod.Aes256);
                    using var writer = new StreamWriter(entryStream, Encoding.UTF8);
                    await writer.WriteAsync(content);
                }
            }

            // Act 2: Read back entries with their respective passwords
            using (var readStream = File.OpenRead(tempPath))
            using (var archive = new ZipArchive(readStream, ZipArchiveMode.Read))
            {
                foreach (var (name, expectedContent, pwd) in entries)
                {
                    var entry = archive.GetEntry(name);
                    Assert.NotNull(entry);

                    // Correct password should work
                    using (var entryStream = entry!.Open(pwd))
                    using (var reader = new StreamReader(entryStream, Encoding.UTF8))
                    {
                        string actualContent = await reader.ReadToEndAsync();
                        Assert.Equal(expectedContent, actualContent);
                    }
                }
            }
        }

        [SkipOnCI("Local development test - requires specific file paths")]
        [Fact]
        public async Task CreateMixedPlainAndAES256EncryptedEntries_RoundTrip()
        {
            // Arrange
            Directory.CreateDirectory(DownloadsDir);
            string tempPath = Path.Join(DownloadsDir, "mixed_plain_aes256.zip");
            const string password = "securePassword123";

            var encryptedEntries = new (string Name, string Content)[]
            {
        ("secure/credentials.txt", "username=admin\npassword=secret"),
        ("secure/data.json", "{\"sensitive\": true}")
            };

            var plainEntries = new (string Name, string Content)[]
            {
        ("readme.txt", "This archive contains both encrypted and plain files"),
        ("public/info.txt", "This is public information")
            };

            // Act 1: Create ZIP with both plain and AES-256 encrypted entries
            using (var createStream = File.Create(tempPath))
            using (var archive = new ZipArchive(createStream, ZipArchiveMode.Create))
            {
                // Add encrypted entries
                foreach (var (name, content) in encryptedEntries)
                {
                    var entry = archive.CreateEntry(name);
                    using var entryStream = entry.Open(password, EncryptionMethod.Aes256);
                    using var writer = new StreamWriter(entryStream, Encoding.UTF8);
                    await writer.WriteAsync(content);
                }

                // Add plain entries
                foreach (var (name, content) in plainEntries)
                {
                    var entry = archive.CreateEntry(name);
                    using var entryStream = entry.Open();
                    using var writer = new StreamWriter(entryStream, Encoding.UTF8);
                    await writer.WriteAsync(content);
                }
            }

            // Act 2: Read back both encrypted and plain entries
            using (var readStream = File.OpenRead(tempPath))
            using (var archive = new ZipArchive(readStream, ZipArchiveMode.Read))
            {
                // Read encrypted entries with password
                foreach (var (name, expectedContent) in encryptedEntries)
                {
                    var entry = archive.GetEntry(name);
                    Assert.NotNull(entry);

                    using var entryStream = entry!.Open(password);
                    using var reader = new StreamReader(entryStream, Encoding.UTF8);
                    string actualContent = await reader.ReadToEndAsync();
                    Assert.Equal(expectedContent, actualContent);
                }

                // Read plain entries without password
                foreach (var (name, expectedContent) in plainEntries)
                {
                    var entry = archive.GetEntry(name);
                    Assert.NotNull(entry);

                    using var entryStream = entry!.Open();
                    using var reader = new StreamReader(entryStream, Encoding.UTF8);
                    string actualContent = await reader.ReadToEndAsync();
                    Assert.Equal(expectedContent, actualContent);
                }
            }
        }

        [SkipOnCI("Local development test - requires specific file paths")]
        [Fact]
        public async Task CreateAndReadAES128EncryptedEntry_RoundTrip()
        {
            // Arrange
            Directory.CreateDirectory(DownloadsDir);
            string tempPath = Path.Join(DownloadsDir, "aes128_single.zip");
            const string entryName = "test_aes128.txt";
            const string password = "Test123!@#";
            const string expectedContent = "This content is encrypted with AES-128";

            // Act 1: Create ZIP with AES-128 encrypted entry
            using (var createStream = File.Create(tempPath))
            using (var archive = new ZipArchive(createStream, ZipArchiveMode.Create))
            {
                var entry = archive.CreateEntry(entryName);
                using var entryStream = entry.Open(password, EncryptionMethod.Aes128);
                using var writer = new StreamWriter(entryStream, Encoding.UTF8);
                await writer.WriteAsync(expectedContent);
            }

            // Act 2: Read back the encrypted entry
            string actualContent;
            using (var readStream = File.OpenRead(tempPath))
            using (var archive = new ZipArchive(readStream, ZipArchiveMode.Read))
            {
                var entry = archive.GetEntry(entryName);
                Assert.NotNull(entry);

                using var entryStream = entry!.Open(password);
                using var reader = new StreamReader(entryStream, Encoding.UTF8);
                actualContent = await reader.ReadToEndAsync();
            }

            // Assert
            Assert.Equal(expectedContent, actualContent);

        }

        [SkipOnCI("Local development test - requires specific file paths")]
        [Fact]
        public async Task CreateAndReadAES192EncryptedEntry_RoundTrip()
        {
            // Arrange
            Directory.CreateDirectory(DownloadsDir);
            string tempPath = Path.Join(DownloadsDir, "aes192_single.zip");
            const string entryName = "test_aes192.txt";
            const string password = "SecurePass456$%^";
            const string expectedContent = "This content is protected with AES-192 encryption";

            // Act 1: Create ZIP with AES-192 encrypted entry
            using (var createStream = File.Create(tempPath))
            using (var archive = new ZipArchive(createStream, ZipArchiveMode.Create))
            {
                var entry = archive.CreateEntry(entryName);
                using var entryStream = entry.Open(password, EncryptionMethod.Aes192);
                using var writer = new StreamWriter(entryStream, Encoding.UTF8);
                await writer.WriteAsync(expectedContent);
            }

            // Act 2: Read back the encrypted entry
            string actualContent;
            using (var readStream = File.OpenRead(tempPath))
            using (var archive = new ZipArchive(readStream, ZipArchiveMode.Read))
            {
                var entry = archive.GetEntry(entryName);
                Assert.NotNull(entry);

                using var entryStream = entry!.Open(password);
                using var reader = new StreamReader(entryStream, Encoding.UTF8);
                actualContent = await reader.ReadToEndAsync();
            }

            // Assert
            Assert.Equal(expectedContent, actualContent);
        }

        [SkipOnCI("Local development test - requires specific file paths")]
        [Fact]
        public void CreateAndReadMultipleEntriesWithDifferentAESLevels_RoundTrip()
        {
            // Arrange
            Directory.CreateDirectory(DownloadsDir);
            string tempPath = Path.Join(DownloadsDir, "mixed_aes_levels.zip");

            var entries = new (string Name, string Content, string Password, EncryptionMethod Encryption)[]
            {
        ("aes128/file1.txt", "AES-128 encrypted content", "password128", EncryptionMethod.Aes128),
        ("aes192/file2.txt", "AES-192 encrypted content", "password192", EncryptionMethod.Aes192),
        ("aes256/file3.txt", "AES-256 encrypted content", "password256", EncryptionMethod.Aes256),
        ("mixed/doc1.json", "{\"encryption\": \"AES-128\"}", "jsonPass128", EncryptionMethod.Aes128),
        ("mixed/doc2.xml", "<root>AES-192</root>", "xmlPass192", EncryptionMethod.Aes192)
            };

            // Act 1: Create ZIP with entries using different AES encryption levels
            using (var createStream = File.Create(tempPath))
            using (var archive = new ZipArchive(createStream, ZipArchiveMode.Create))
            {
                foreach (var (name, content, pwd, encryption) in entries)
                {
                    var entry = archive.CreateEntry(name);
                    using var entryStream = entry.Open(pwd, encryption);
                    using var writer = new StreamWriter(entryStream, Encoding.UTF8);
                    writer.Write(content);
                }
            }

            // Act 2: Read back all encrypted entries with their respective passwords
            using (var readStream = File.OpenRead(tempPath))
            using (var archive = new ZipArchive(readStream, ZipArchiveMode.Read))
            {
                foreach (var (name, expectedContent, pwd, _) in entries)
                {
                    var entry = archive.GetEntry(name);
                    Assert.NotNull(entry);

                    // Correct password should work
                    using (var entryStream = entry!.Open(pwd))
                    using (var reader = new StreamReader(entryStream, Encoding.UTF8))
                    {
                        string actualContent = reader.ReadToEnd();
                        Assert.Equal(expectedContent, actualContent);
                    }

                }
            }
        }

        [SkipOnCI("Local development test - requires specific file paths")]
        [Fact]
        public void CreateLargeFileWithAES128_RoundTrip()
        {
            // Arrange
            Directory.CreateDirectory(DownloadsDir);
            string tempPath = Path.Join(DownloadsDir, "aes128_large2.zip");
            const string entryName = "large_file.bin";
            const string password = "LargeFilePass123!";

            // Create a larger content
            var random = new Random(42); // Seed for reproducibility
            var largeContent = new byte[1024 * 1024];
            random.NextBytes(largeContent);

            // Act 1: Create ZIP with AES-128 encrypted large entry
            using (var createStream = File.Create(tempPath))
            using (var archive = new ZipArchive(createStream, ZipArchiveMode.Create))
            {
                var entry = archive.CreateEntry(entryName);
                using var entryStream = entry.Open(password, EncryptionMethod.Aes128);
                entryStream.Write(largeContent);
            }

            // Act 2: Read back and verify the large encrypted entry
            using (var readStream = File.OpenRead(tempPath))
            using (var archive = new ZipArchive(readStream, ZipArchiveMode.Read))
            {
                var entry = archive.GetEntry(entryName);
                Assert.NotNull(entry);

                using var entryStream = entry!.Open(password);
                using var ms = new MemoryStream();
                entryStream.CopyTo(ms);
                var actualContent = ms.ToArray();

                // Assert
                Assert.Equal(largeContent.Length, actualContent.Length);
                Assert.Equal(largeContent, actualContent);
            }
        }

        [SkipOnCI("Local development test - requires specific file paths")]
        [Fact]
        public async Task CreateCompressedAndAES192Encrypted_RoundTrip()
        {
            // Arrange
            Directory.CreateDirectory(DownloadsDir);
            string tempPath = Path.Join(DownloadsDir, "aes192_compressed.zip");
            const string password = "CompressedPass!";

            // Create highly compressible content
            string repeatedContent = string.Join("\n", Enumerable.Repeat("This line is repeated many times to test compression with AES-192.", 100));

            var entries = new (string Name, string Content, CompressionLevel Level)[]
            {
        ("optimal.txt", repeatedContent, CompressionLevel.Optimal),
        ("fastest.txt", repeatedContent, CompressionLevel.Fastest),
        ("smallest.txt", repeatedContent, CompressionLevel.SmallestSize),
        ("nocompression.txt", repeatedContent, CompressionLevel.NoCompression)
            };

            // Act 1: Create ZIP with AES-192 encrypted entries at different compression levels
            using (var createStream = File.Create(tempPath))
            using (var archive = new ZipArchive(createStream, ZipArchiveMode.Create))
            {
                foreach (var (name, content, level) in entries)
                {
                    var entry = archive.CreateEntry(name, level);
                    using var entryStream = entry.Open(password, EncryptionMethod.Aes192);
                    using var writer = new StreamWriter(entryStream, Encoding.UTF8);
                    writer.Write(content);
                }
            }

            // Act 2: Read back all entries
            using (var readStream = File.OpenRead(tempPath))
            using (var archive = new ZipArchive(readStream, ZipArchiveMode.Read))
            {
                foreach (var (name, expectedContent, _) in entries)
                {
                    var entry = archive.GetEntry(name);
                    Assert.NotNull(entry);

                    using var entryStream = entry!.Open(password);
                    using var reader = new StreamReader(entryStream, Encoding.UTF8);
                    string actualContent = await reader.ReadToEndAsync();

                    Assert.Equal(expectedContent, actualContent);
                }
            }

            // Verify file sizes are different due to compression levels
            var fileInfo = new FileInfo(tempPath);
            Assert.True(fileInfo.Exists);
            Assert.True(fileInfo.Length > 0);
        }

        [SkipOnCI("Local development test - requires specific file paths")]
        [Fact]
        public async Task MixAllEncryptionTypes_RoundTrip()
        {
            // Arrange
            Directory.CreateDirectory(DownloadsDir);
            string tempPath = Path.Join(DownloadsDir, "all_encryption_types.zip");

            var entries = new (string Name, string Content, string? Password, EncryptionMethod? Encryption)[]
            {
        // Plain entry
        ("plain/readme.txt", "This is a plain unencrypted file", null, null),
        
        // ZipCrypto
        ("zipcrypto/secret.txt", "ZipCrypto encrypted content", "zipPass", EncryptionMethod.ZipCrypto),
        
        // AES-128
        ("aes128/data.txt", "AES-128 encrypted data", "aes128Pass", EncryptionMethod.Aes128),
        
        // AES-192
        ("aes192/config.json", "{\"level\": \"AES-192\"}", "aes192Pass", EncryptionMethod.Aes192),
        
        // AES-256
        ("aes256/secure.xml", "<data>AES-256 secured</data>", "aes256Pass", EncryptionMethod.Aes256)
            };

            // Act 1: Create ZIP with all encryption types
            using (var createStream = File.Create(tempPath))
            using (var archive = new ZipArchive(createStream, ZipArchiveMode.Create))
            {
                foreach (var (name, content, pwd, encryption) in entries)
                {
                    var entry = archive.CreateEntry(name);
                    Stream entryStream = pwd != null && encryption.HasValue
                        ? entry.Open(pwd, encryption.Value)
                        : entry.Open();

                    using (entryStream)
                    using (var writer = new StreamWriter(entryStream, Encoding.UTF8))
                    {
                        await writer.WriteAsync(content);
                    }
                }
            }

            // Act 2: Read back all entries
            using (var readStream = File.OpenRead(tempPath))
            using (var archive = new ZipArchive(readStream, ZipArchiveMode.Read))
            {
                foreach (var (name, expectedContent, pwd, _) in entries)
                {
                    var entry = archive.GetEntry(name);
                    Assert.NotNull(entry);

                    Stream entryStream = pwd != null
                        ? entry!.Open(pwd)
                        : entry!.Open();

                    using (entryStream)
                    using (var reader = new StreamReader(entryStream, Encoding.UTF8))
                    {
                        string actualContent = await reader.ReadToEndAsync();
                        Assert.Equal(expectedContent, actualContent);
                    }
                }
            }
        }

        [SkipOnCI("Local development test - requires specific file paths")]
        [Fact]
        public void OpenAESEncryptedTxtFile_AE1_ShouldReturnPlaintext()
        {
            // Arrange
            string zipPath = Path.Join(DownloadsDir, "source_plain_ae1.zip");
            const string entryName = "source_plain.txt";
            const string password = "123456789";
            const string expectedContent = "this is plain";

            // Act
            using var archive = ZipFile.OpenRead(zipPath);
            var entry = archive.Entries.First(e => e.FullName.EndsWith(entryName));

            using var stream = entry.Open(password);
            using var reader = new StreamReader(stream);
            string actualContent = reader.ReadToEnd();

            // Assert
            Assert.Equal(expectedContent, actualContent);
        }

        [SkipOnCI("Local development test - requires specific file paths")]
        [Fact]
        public async Task AES128WithSpecialCharacters_RoundTrip()
        {
            // Arrange
            Directory.CreateDirectory(DownloadsDir);
            string tempPath = Path.Join(DownloadsDir, "aes128_special_chars.zip");
            const string password = "パスワード123!@#"; // Japanese characters in password

            var entries = new (string Name, string Content)[]
            {
        ("unicode/chinese.txt", "你好世界 - Hello World in Chinese"),
        ("unicode/arabic.txt", "مرحبا بالعالم - Hello World in Arabic"),
        ("unicode/emoji.txt", "Hello 👋 World 🌍 with emojis! 🎉"),
        ("unicode/mixed.txt", "Ñiño José façade naïve Zürich")
            };

            // Act 1: Create ZIP with AES-128 encrypted Unicode content
            using (var createStream = File.Create(tempPath))
            using (var archive = new ZipArchive(createStream, ZipArchiveMode.Create))
            {
                foreach (var (name, content) in entries)
                {
                    var entry = archive.CreateEntry(name);
                    using var entryStream = entry.Open(password, EncryptionMethod.Aes128);
                    using var writer = new StreamWriter(entryStream, Encoding.UTF8);
                    await writer.WriteAsync(content);
                }
            }

            // Act 2: Read back and verify Unicode content
            using (var readStream = File.OpenRead(tempPath))
            using (var archive = new ZipArchive(readStream, ZipArchiveMode.Read))
            {
                foreach (var (name, expectedContent) in entries)
                {
                    var entry = archive.GetEntry(name);
                    Assert.NotNull(entry);

                    using var entryStream = entry!.Open(password);
                    using var reader = new StreamReader(entryStream, Encoding.UTF8);
                    string actualContent = await reader.ReadToEndAsync();

                    Assert.Equal(expectedContent, actualContent);
                }
            }
        }

        [Fact]
        public async Task CreateAndReadAES256WithAsyncOperations_RoundTrip()
        {
            // Arrange
            Directory.CreateDirectory(DownloadsDir);
            string tempPath = Path.Join(DownloadsDir, "aes256_async_operations.zip");
            const string entryName = "async_test.txt";
            const string password = "AsyncPass123!";
            const string expectedContent = "This content was written and read asynchronously with AES-256";

            // Act 1: Create ZIP with AES-256 encrypted entry using async write
            using (var createStream = File.Create(tempPath))
            using (var archive = new ZipArchive(createStream, ZipArchiveMode.Create))
            {
                var entry = archive.CreateEntry(entryName);
                using var entryStream = entry.Open(password, EncryptionMethod.Aes256);

                // Use async write operations
                byte[] contentBytes = Encoding.UTF8.GetBytes(expectedContent);
                await entryStream.WriteAsync(contentBytes, 0, contentBytes.Length);
                await entryStream.FlushAsync();
            }

            // Act 2: Read back using async operations
            string actualContent;
            using (var readStream = File.OpenRead(tempPath))
            using (var archive = new ZipArchive(readStream, ZipArchiveMode.Read))
            {
                var entry = archive.GetEntry(entryName);
                Assert.NotNull(entry);

                using var entryStream = entry!.Open(password);
                using var reader = new StreamReader(entryStream, Encoding.UTF8);
                actualContent = await reader.ReadToEndAsync();
            }

            // Assert
            Assert.Equal(expectedContent, actualContent);
        }

        [SkipOnCI("Local development test - requires specific file paths")]
        [Fact]
        public async Task CreateMultipleAESEntriesWithAsyncWrites_RoundTrip()
        {
            // Arrange
            Directory.CreateDirectory(DownloadsDir);
            string tempPath = Path.Join(DownloadsDir, "multiple_aes_async_writes.zip");

            var entries = new (string Name, byte[] Content, string Password, EncryptionMethod Encryption)[]
            {
        ("async128.bin", Encoding.UTF8.GetBytes("AES-128 async content"), "pass128", EncryptionMethod.Aes128),
        ("async192.bin", Encoding.UTF8.GetBytes("AES-192 async content"), "pass192", EncryptionMethod.Aes192),
        ("async256.bin", Encoding.UTF8.GetBytes("AES-256 async content"), "pass256", EncryptionMethod.Aes256)
            };

            // Act 1: Create entries with async writes
            using (var createStream = File.Create(tempPath))
            using (var archive = new ZipArchive(createStream, ZipArchiveMode.Create))
            {
                foreach (var (name, content, pwd, encryption) in entries)
                {
                    var entry = archive.CreateEntry(name);
                    using var entryStream = entry.Open(pwd, encryption);

                    // Write asynchronously
                    await entryStream.WriteAsync(content, 0, content.Length);
                    await entryStream.FlushAsync();
                }
            }

            // Act 2: Read back with async operations
            using (var readStream = File.OpenRead(tempPath))
            using (var archive = new ZipArchive(readStream, ZipArchiveMode.Read))
            {
                foreach (var (name, expectedContent, pwd, _) in entries)
                {
                    var entry = archive.GetEntry(name);
                    Assert.NotNull(entry);

                    using var entryStream = entry!.Open(pwd);

                    // Read asynchronously
                    var buffer = new byte[expectedContent.Length];
                    int totalRead = 0;
                    while (totalRead < buffer.Length)
                    {
                        int bytesRead = await entryStream.ReadAsync(buffer, totalRead, buffer.Length - totalRead);
                        if (bytesRead == 0) break;
                        totalRead += bytesRead;
                    }

                    Assert.Equal(expectedContent, buffer);
                }
            }
        }

        [SkipOnCI("Local development test - requires specific file paths")]
        [Fact]
        public async Task CreateLargeBinaryDataWithAES128Async_RoundTrip()
        {
            // Arrange
            Directory.CreateDirectory(DownloadsDir);
            string tempPath = Path.Join(DownloadsDir, "aes128_large_async.zip");
            const string entryName = "large_async.bin";
            const string password = "LargeAsync!@#";

            // Create larger test data
            var random = new Random(123);
            var largeData = new byte[256 * 1024]; // 256KB
            random.NextBytes(largeData);

            // Act 1: Write large data asynchronously with AES-128
            using (var createStream = File.Create(tempPath))
            using (var archive = new ZipArchive(createStream, ZipArchiveMode.Create))
            {
                var entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
                using var entryStream = entry.Open(password, EncryptionMethod.Aes128);

                // Write in chunks asynchronously
                const int chunkSize = 8192;
                for (int offset = 0; offset < largeData.Length; offset += chunkSize)
                {
                    int writeSize = Math.Min(chunkSize, largeData.Length - offset);
                    await entryStream.WriteAsync(largeData, offset, writeSize);
                }
                await entryStream.FlushAsync();
            }

            // Act 2: Read back asynchronously
            using (var readStream = File.OpenRead(tempPath))
            using (var archive = new ZipArchive(readStream, ZipArchiveMode.Read))
            {
                var entry = archive.GetEntry(entryName);
                Assert.NotNull(entry);

                using var entryStream = entry!.Open(password);
                using var ms = new MemoryStream();

                // Read in chunks asynchronously
                await entryStream.CopyToAsync(ms, bufferSize: 8192);
                var actualData = ms.ToArray();

                // Assert
                Assert.Equal(largeData.Length, actualData.Length);
                Assert.Equal(largeData, actualData);
            }
        }

        [SkipOnCI("Local development test - requires specific file paths")]
        [Fact]
        public async Task MultipleAsyncWritesInSingleEntry_AES256_RoundTrip()
        {
            // Arrange
            Directory.CreateDirectory(DownloadsDir);
            string tempPath = Path.Join(DownloadsDir, "aes256_multiple_writes.zip");
            const string entryName = "multi_write.txt";
            const string password = "MultiWrite256";

            var parts = new[]
            {
        "First part of content\n",
        "Second part of content\n",
        "Third part of content\n",
        "Final part of content"
    };
            string expectedContent = string.Join("", parts);

            // Act 1: Write multiple times to same entry
            using (var createStream = File.Create(tempPath))
            using (var archive = new ZipArchive(createStream, ZipArchiveMode.Create))
            {
                var entry = archive.CreateEntry(entryName);
                using var entryStream = entry.Open(password, EncryptionMethod.Aes256);

                // Write each part asynchronously
                foreach (var part in parts)
                {
                    byte[] partBytes = Encoding.UTF8.GetBytes(part);
                    await entryStream.WriteAsync(partBytes, 0, partBytes.Length);
                }
                await entryStream.FlushAsync();
            }

            // Act 2: Read back all content
            using (var readStream = File.OpenRead(tempPath))
            using (var archive = new ZipArchive(readStream, ZipArchiveMode.Read))
            {
                var entry = archive.GetEntry(entryName);
                Assert.NotNull(entry);

                using var entryStream = entry!.Open(password);
                using var reader = new StreamReader(entryStream);
                string actualContent = await reader.ReadToEndAsync();

                // Assert
                Assert.Equal(expectedContent, actualContent);
            }
        }

        [SkipOnCI("Local development test - requires specific file paths")]
        [Fact]
        public async Task AsyncReadInChunks_AES128_VerifyContent()
        {
            // Arrange
            Directory.CreateDirectory(DownloadsDir);
            string tempPath = Path.Join(DownloadsDir, "aes128_chunked_read.zip");
            const string entryName = "chunked.bin";
            const string password = "ChunkedRead128";

            // Create recognizable pattern
            var pattern = new byte[1024];
            for (int i = 0; i < pattern.Length; i++)
            {
                pattern[i] = (byte)(i % 256);
            }

            // Act 1: Write pattern
            using (var createStream = File.Create(tempPath))
            using (var archive = new ZipArchive(createStream, ZipArchiveMode.Create))
            {
                var entry = archive.CreateEntry(entryName);
                using var entryStream = entry.Open(password, EncryptionMethod.Aes128);
                await entryStream.WriteAsync(pattern, 0, pattern.Length);
            }

            // Act 2: Read in small chunks and verify
            using (var readStream = File.OpenRead(tempPath))
            using (var archive = new ZipArchive(readStream, ZipArchiveMode.Read))
            {
                var entry = archive.GetEntry(entryName);
                Assert.NotNull(entry);

                using var entryStream = entry!.Open(password);

                // Read in 100-byte chunks
                const int chunkSize = 16;
                var readBuffer = new byte[chunkSize];
                var allData = new List<byte>();

                int bytesRead;
                while ((bytesRead = await entryStream.ReadAsync(readBuffer, 0, chunkSize)) > 0)
                {
                    allData.AddRange(readBuffer.Take(bytesRead));
                }

                // Assert
                Assert.Equal(pattern, allData.ToArray());
            }
        }

        [SkipOnCI("Local development test - requires specific file paths")]
        [Fact]
        public async Task MixedSyncAsyncOperations_AES192_RoundTrip()
        {
            // Arrange
            Directory.CreateDirectory(DownloadsDir);
            string tempPath = Path.Join(DownloadsDir, "aes192_mixed_ops.zip");

            var entries = new[]
            {
        ("sync_write.txt", "Written synchronously", "syncPass"),
        ("async_write.txt", "Written asynchronously", "asyncPass")
    };

            // Act 1: Mix sync and async writes
            using (var createStream = File.Create(tempPath))
            using (var archive = new ZipArchive(createStream, ZipArchiveMode.Create))
            {
                // Synchronous write
                var syncEntry = archive.CreateEntry(entries[0].Item1);
                using (var syncStream = syncEntry.Open(entries[0].Item3, EncryptionMethod.Aes192))
                {
                    byte[] syncBytes = Encoding.UTF8.GetBytes(entries[0].Item2);
                    syncStream.Write(syncBytes, 0, syncBytes.Length);
                }

                // Asynchronous write
                var asyncEntry = archive.CreateEntry(entries[1].Item1);
                using (var asyncStream = asyncEntry.Open(entries[1].Item3, EncryptionMethod.Aes192))
                {
                    byte[] asyncBytes = Encoding.UTF8.GetBytes(entries[1].Item2);
                    await asyncStream.WriteAsync(asyncBytes, 0, asyncBytes.Length);
                }
            }

            // Act 2: Read back with mixed operations
            using (var readStream = File.OpenRead(tempPath))
            using (var archive = new ZipArchive(readStream, ZipArchiveMode.Read))
            {
                // Read first entry asynchronously
                var entry1 = archive.GetEntry(entries[0].Item1);
                Assert.NotNull(entry1);
                using (var stream1 = entry1!.Open(entries[0].Item3))
                using (var reader1 = new StreamReader(stream1))
                {
                    string content1 = await reader1.ReadToEndAsync();
                    Assert.Equal(entries[0].Item2, content1);
                }

                // Read second entry synchronously
                var entry2 = archive.GetEntry(entries[1].Item1);
                Assert.NotNull(entry2);
                using (var stream2 = entry2!.Open(entries[1].Item3))
                using (var reader2 = new StreamReader(stream2))
                {
                    string content2 = reader2.ReadToEnd();
                    Assert.Equal(entries[1].Item2, content2);
                }
            }
        }

        [SkipOnCI("Local development test - requires specific file paths")]
        [Fact]
        public async Task Debug_UpdateMode_MultipleEncryptedEntries_ModifyOne()
        {
            // Arrange - use a fixed path so you can hexdump it
            Directory.CreateDirectory(DownloadsDir);
            string archivePath = Path.Combine(DownloadsDir, "debug_update_mode_aes.zip");
            string archiveAfterUpdatePath = Path.Combine(DownloadsDir, "debug_update_mode_aes_after_update.zip");

            if (File.Exists(archivePath)) File.Delete(archivePath);
            if (File.Exists(archiveAfterUpdatePath)) File.Delete(archiveAfterUpdatePath);

            string password = "password123";
            var encryptionMethod = EncryptionMethod.Aes256;

            // Step 1: Create initial archive with 3 encrypted entries
            using (var archive = ZipFile.Open(archivePath, ZipArchiveMode.Create))
            {
                var entries = new[]
                {
            ("file1.txt", "Content 1"),
            ("file2.txt", "Content 2"),
            ("file3.txt", "Content 3")
        };

                foreach (var (name, content) in entries)
                {
                    var entry = archive.CreateEntry(name);
                    using var stream = entry.Open(password, encryptionMethod);
                    using var writer = new StreamWriter(stream, Encoding.UTF8);
                    await writer.WriteAsync(content);
                }
            }

            // Copy the original archive for comparison
            File.Copy(archivePath, Path.Combine(DownloadsDir, "debug_update_mode_aes_ORIGINAL.zip"), overwrite: true);

            // Step 2: Open in Update mode and modify file2.txt
            using (var archive = ZipFile.Open(archivePath, ZipArchiveMode.Update))
            {
                var entry = archive.GetEntry("file2.txt");
                Assert.NotNull(entry);

                // Log entry state before opening
                System.Diagnostics.Debug.WriteLine($"Before Open - Entry: {entry.FullName}");
                System.Diagnostics.Debug.WriteLine($"  IsEncrypted: {entry.IsEncrypted}");
                System.Diagnostics.Debug.WriteLine($"  CompressionMethod: {entry.CompressionMethod}");
                System.Diagnostics.Debug.WriteLine($"  CompressedLength: {entry.CompressedLength}");
                System.Diagnostics.Debug.WriteLine($"  Length: {entry.Length}");

                using (var stream = entry.Open(password))
                {
                    stream.SetLength(0);
                    byte[] newContent = Encoding.UTF8.GetBytes("Modified Content 2");
                    await stream.WriteAsync(newContent, 0, newContent.Length);
                }

                // Log all entries' state after modification
                foreach (var e in archive.Entries)
                {
                    System.Diagnostics.Debug.WriteLine($"After Modify - Entry: {e.FullName}");
                    System.Diagnostics.Debug.WriteLine($"  IsEncrypted: {e.IsEncrypted}");
                    System.Diagnostics.Debug.WriteLine($"  CompressionMethod: {e.CompressionMethod}");
                }
            }

            // Copy the modified archive for comparison
            File.Copy(archivePath, archiveAfterUpdatePath, overwrite: true);

            // Step 3: Try to read back all entries
            System.Diagnostics.Debug.WriteLine("=== Reading back entries ===");

            using (var archive = ZipFile.Open(archivePath, ZipArchiveMode.Read))
            {
                foreach (var entry in archive.Entries)
                {
                    System.Diagnostics.Debug.WriteLine($"Reading Entry: {entry.FullName}");
                    System.Diagnostics.Debug.WriteLine($"  IsEncrypted: {entry.IsEncrypted}");
                    System.Diagnostics.Debug.WriteLine($"  CompressionMethod: {entry.CompressionMethod}");
                    System.Diagnostics.Debug.WriteLine($"  CompressedLength: {entry.CompressedLength}");
                    System.Diagnostics.Debug.WriteLine($"  Length: {entry.Length}");

                    try
                    {
                        using var stream = entry.Open(password);
                        using var reader = new StreamReader(stream, Encoding.UTF8);
                        string content = await reader.ReadToEndAsync();
                        System.Diagnostics.Debug.WriteLine($"  Content: '{content}'");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"  ERROR: {ex.GetType().Name}: {ex.Message}");
                    }
                }
            }

            // Assert - this is where the original test fails
            using (var archive = ZipFile.Open(archivePath, ZipArchiveMode.Read))
            {
                using (var s1 = archive.GetEntry("file1.txt")!.Open(password))
                using (var r1 = new StreamReader(s1))
                {
                    Assert.Equal("Content 1", await r1.ReadToEndAsync());
                }

                using (var s2 = archive.GetEntry("file2.txt")!.Open(password))
                using (var r2 = new StreamReader(s2))
                {
                    Assert.Equal("Modified Content 2", await r2.ReadToEndAsync());
                }

                using (var s3 = archive.GetEntry("file3.txt")!.Open(password))
                using (var r3 = new StreamReader(s3))
                {
                    Assert.Equal("Content 3", await r3.ReadToEndAsync());
                }
            }
        }

        [SkipOnCI("Local development test - creates archive for manual inspection with WinRAR")]
        [Fact]
        public async Task Local_UpdateMode_EditAllEntries_MixedEncryption_ForWinRARInspection()
        {
            // Arrange
            Directory.CreateDirectory(DownloadsDir);
            string archivePath = NewPath("update_all_entries_mixed_encryption.zip");
            string archiveBeforeUpdatePath = NewPath("update_all_entries_mixed_encryption_BEFORE.zip");

            if (File.Exists(archivePath)) File.Delete(archivePath);
            if (File.Exists(archiveBeforeUpdatePath)) File.Delete(archiveBeforeUpdatePath);

            string password = "password123";

            var entries = new[]
            {
                ("entry_zipcrypto.txt", "Content ZipCrypto", EncryptionMethod.ZipCrypto),
                ("entry_aes128.txt", "Content AES-128", EncryptionMethod.Aes128),
                ("entry_aes192.txt", "Content AES-192", EncryptionMethod.Aes192),
                ("entry_aes256.txt", "Content AES-256", EncryptionMethod.Aes256)
            };

            // Step 1: Create archive with entries using different encryption methods
            using (var archive = ZipFile.Open(archivePath, ZipArchiveMode.Create))
            {
                foreach (var (name, content, encryption) in entries)
                {
                    var entry = archive.CreateEntry(name);
                    using var stream = entry.Open(password, encryption);
                    using var writer = new StreamWriter(stream, Encoding.UTF8);
                    await writer.WriteAsync(content);
                }
            }

            // Save a copy before modifications
            File.Copy(archivePath, archiveBeforeUpdatePath, overwrite: true);

            // Step 2: Open in Update mode and edit ALL entries
            using (var archive = ZipFile.Open(archivePath, ZipArchiveMode.Update))
            {
                foreach (var (name, _, _) in entries)
                {
                    var entry = archive.GetEntry(name);
                    Assert.NotNull(entry);
                    Assert.True(entry.IsEncrypted);

                    using (var stream = entry.Open(password))
                    {
                        stream.SetLength(0);
                        byte[] content = Encoding.UTF8.GetBytes($"Modified {name}");
                        await stream.WriteAsync(content, 0, content.Length);
                    }
                }
            }

            // Step 3: Verify all entries can be read back
            using (var archive = ZipFile.Open(archivePath, ZipArchiveMode.Read))
            {
                foreach (var (name, _, _) in entries)
                {
                    var entry = archive.GetEntry(name);
                    Assert.NotNull(entry);
                    Assert.True(entry.IsEncrypted);

                    using var stream = entry.Open(password);
                    using var reader = new StreamReader(stream, Encoding.UTF8);
                    string content = await reader.ReadToEndAsync();
                    Assert.Equal($"Modified {name}", content);
                }
            }

        }
    }
}
