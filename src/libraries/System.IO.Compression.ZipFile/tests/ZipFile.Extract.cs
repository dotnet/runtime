// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Sdk;

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

        [Fact]
        public void ExtractEncryptedEntryToFile_WithWrongPassword_ShouldThrow()
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
                entry.ExtractToFile(tempFile, overwrite: true, password: "wrongpass");
            });
        }

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
        public async Task ExtractToFileAsync_WithCancellation_ShouldCancel()
        {
            string zipPath = @"C:\Users\spahontu\Downloads\test.zip";
            Assert.True(File.Exists(zipPath), $"Test ZIP not found at {zipPath}");

            string tempFile = Path.Combine(Path.GetTempPath(), "hello_async_cancel.txt");
            if (File.Exists(tempFile)) File.Delete(tempFile);

            using var archive = ZipFile.OpenRead(zipPath);
            var entry = archive.Entries.First(e => e.FullName.EndsWith("hello.txt", StringComparison.OrdinalIgnoreCase));

            using var cts = new CancellationTokenSource();
            cts.Cancel(); // Cancel immediately
            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            {
                await entry.ExtractToFileAsync(tempFile, overwrite: true, cts.Token, password: "123456789");
            });
        }

        [Fact]
        public void OpenEncryptedJpeg_ShouldDecryptAndMatchOriginal()
        {
            // Arrange
            string zipPath = @"C:\Users\spahontu\Downloads\jpg.zip";
            string originalPath = @"C:\Users\spahontu\Downloads\test.jpg"; // original JPEG for comparison
            Assert.True(File.Exists(zipPath), $"Encrypted ZIP not found at {zipPath}");
            Assert.True(File.Exists(originalPath), $"Original JPEG not found at {originalPath}");

            using var archive = ZipFile.OpenRead(zipPath);
            var entry = archive.Entries.First(e => e.FullName.EndsWith("test.jpg", StringComparison.OrdinalIgnoreCase));

            // Act: open decrypted + decompressed stream
            using var stream = entry.Open("123456789");

            // Read all bytes
            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            byte[] actualBytes = ms.ToArray();

            // Optional: compare with original file
            byte[] expectedBytes = File.ReadAllBytes(originalPath);
            Assert.Equal(expectedBytes.Length, actualBytes.Length);
            Assert.Equal(expectedBytes, actualBytes);
        }


        [Fact]
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
                var entry = za.CreateEntry(entryName, password, ZipArchiveEntry.EncryptionMethod.ZipCrypto);

                using var writer = new StreamWriter(entry.Open(), Encoding.UTF8, bufferSize: 1024, leaveOpen: false);
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
            const ZipArchiveEntry.EncryptionMethod enc = ZipArchiveEntry.EncryptionMethod.ZipCrypto;

            // Act 1: Create with same password for all
            using (var za = ZipFile.Open(zipPath, ZipArchiveMode.Create))
            {
                foreach (var it in items)
                {
                    var entry = za.CreateEntry(it.Name, password, enc);
                    using var w = new StreamWriter(entry.Open(), Encoding.UTF8, bufferSize: 1024, leaveOpen: false);
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
            const ZipArchiveEntry.EncryptionMethod enc = ZipArchiveEntry.EncryptionMethod.ZipCrypto;

            // Act 1: Create, each entry with its own password
            using (var za = ZipFile.Open(zipPath, ZipArchiveMode.Create))
            {
                foreach (var it in items)
                {
                    var entry = za.CreateEntry(it.Name, it.Password, enc);
                    using var w = new StreamWriter(entry.Open(), Encoding.UTF8, bufferSize: 1024, leaveOpen: false);
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

        [Fact]
        public async Task ZipCrypto_Mixed_EncryptedAndPlainEntries_AllRoundTrip()
        {
            // Arrange
            Directory.CreateDirectory(DownloadsDir);
            string zipPath = NewPath("zipcrypto_mixed.zip");
            if (File.Exists(zipPath)) File.Delete(zipPath);

            const string encPw = "EncOnly123!";
            const ZipArchiveEntry.EncryptionMethod enc = ZipArchiveEntry.EncryptionMethod.ZipCrypto;

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
                    var entry = za.CreateEntry(it.Name, encPw, enc);
                    using var w = new StreamWriter(entry.Open(), Encoding.UTF8, bufferSize: 1024, leaveOpen: false);
                    await w.WriteAsync(it.Content);
                }

                // Plain
                foreach (var it in plainItems)
                {
                    var entry = za.CreateEntry(it.Name); // default: no encryption
                    using var w = new StreamWriter(entry.Open(), Encoding.UTF8, bufferSize: 1024, leaveOpen: false);
                    await w.WriteAsync(it.Content);
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



        [Fact]
        public async Task Update_AddEncryptedEntry_RoundTrip()
        {
            // Arrange
            Directory.CreateDirectory(DownloadsDir);
            string zipPath = NewPath("update_add.zip");
            if (File.Exists(zipPath)) File.Delete(zipPath);

            // Create initial archive with one plain entry
            using (var za = ZipFile.Open(zipPath, ZipArchiveMode.Create))
            {
                var e = za.CreateEntry("plain.txt");
                using var w = new StreamWriter(e.Open(), Encoding.UTF8);
                await w.WriteAsync("plain content");
            }

            // Act: Open in Update mode and add encrypted entry
            using (var za = ZipFile.Open(zipPath, ZipArchiveMode.Update))
            {
                var encEntry = za.CreateEntry("secure/new.txt", "pw123", ZipArchiveEntry.EncryptionMethod.ZipCrypto);
                using var w = new StreamWriter(encEntry.Open(), Encoding.UTF8);
                await w.WriteAsync("secret data");
            }

            // Assert: Verify both entries exist and encrypted one decrypts correctly
            using (var za = ZipFile.Open(zipPath, ZipArchiveMode.Read))
            {
                var plain = za.GetEntry("plain.txt");
                Assert.NotNull(plain);
                using (var r = new StreamReader(plain!.Open(), Encoding.UTF8))
                    Assert.Equal("plain content", await r.ReadToEndAsync());

                var secure = za.GetEntry("secure/new.txt");
                Assert.NotNull(secure);
                using (var r = new StreamReader(secure!.Open("pw123"), Encoding.UTF8))
                    Assert.Equal("secret data", await r.ReadToEndAsync());
            }
        }

        [Fact]
        public async Task Update_DeleteEncryptedEntry_RemovesSuccessfully()
        {
            // Arrange
            string zipPath = NewPath("update_delete.zip");
            if (File.Exists(zipPath)) File.Delete(zipPath);

            using (var za = ZipFile.Open(zipPath, ZipArchiveMode.Create))
            {
                var e = za.CreateEntry("secure/delete.txt", "delpw", ZipArchiveEntry.EncryptionMethod.ZipCrypto);
                using var w = new StreamWriter(e.Open(), Encoding.UTF8);
                await w.WriteAsync("to be deleted");
            }

            // Act: Delete the encrypted entry
            using (var za = ZipFile.Open(zipPath, ZipArchiveMode.Update))
            {
                var e = za.GetEntry("secure/delete.txt");
                Assert.NotNull(e);
                e!.Delete();
            }

            // Assert: Entry should not exist
            using (var za = ZipFile.Open(zipPath, ZipArchiveMode.Read))
            {
                Assert.Null(za.GetEntry("secure/delete.txt"));
            }
        }

        [Fact]
        public async Task Update_CopyEncryptedEntry_ToNewName_RoundTrip()
        {
            // Arrange
            string zipPath = NewPath("update_copy.zip");
            if (File.Exists(zipPath)) File.Delete(zipPath);

            const string pw = "copy-pw";
            using (var za = ZipFile.Open(zipPath, ZipArchiveMode.Create))
            {
                var e = za.CreateEntry("secure/original.txt", pw, ZipArchiveEntry.EncryptionMethod.ZipCrypto);
                using var w = new StreamWriter(e.Open(), Encoding.UTF8);
                w.Write("original content");
            }

            // Act: Copy encrypted entry to new name
            using (var za = ZipFile.Open(zipPath, ZipArchiveMode.Update))
            {
                var src = za.GetEntry("secure/original.txt");
                Assert.NotNull(src);

                // Read original
                string content;
                using (var r = new StreamReader(src!.Open(pw), Encoding.UTF8))
                    content = r.ReadToEnd();

                // Create new entry with same password
                var dst = za.CreateEntry("secure/copy.txt", pw, ZipArchiveEntry.EncryptionMethod.ZipCrypto);
                using var w = new StreamWriter(dst.Open(), Encoding.UTF8);
                w.Write(content);
            }

            // Assert: Both entries exist and decrypt correctly
            using (var za = ZipFile.Open(zipPath, ZipArchiveMode.Read))
            {
                var orig = za.GetEntry("secure/original.txt");
                var copy = za.GetEntry("secure/copy.txt");
                Assert.NotNull(orig);
                Assert.NotNull(copy);

                using (var r1 = new StreamReader(orig!.Open(pw), Encoding.UTF8))
                    Assert.Equal("original content", await r1.ReadToEndAsync());

                using (var r2 = new StreamReader(copy!.Open(pw), Encoding.UTF8))
                    Assert.Equal("original content", await r2.ReadToEndAsync());
            }
        }


        [Fact]
        public async Task Update_CopyEncryptedEntry_ToNewName_RoundTrip_2()
        {
            // Arrange
            Directory.CreateDirectory(DownloadsDir);
            string zipPath = NewPath("update_copy.zip");
            if (File.Exists(zipPath)) File.Delete(zipPath);

            const string pw = "copy-pw";
            const string originalName = "secure/original.txt";
            const string copyName = "secure/copy.txt";
            const string payload = "original content";

            // Create archive and a single encrypted entry
            using (var za = ZipFile.Open(zipPath, ZipArchiveMode.Create))
            {
                var e = za.CreateEntry(originalName, pw, ZipArchiveEntry.EncryptionMethod.ZipCrypto);
                using var w = new StreamWriter(e.Open(), Encoding.UTF8, bufferSize: 1024, leaveOpen: false);
                await w.WriteAsync(payload);
            }

            // Act: Open in Update mode and copy encrypted entry to a new name
            using (var za = ZipFile.Open(zipPath, ZipArchiveMode.Update))
            {
                var src = za.GetEntry(originalName);
                Assert.NotNull(src);

                // READ-ONLY decrypt in Update mode (Option A): Open(password) returns a readable stream,
                // does NOT mark the entry as modified, and does NOT materialize to an edit buffer.
                string content;
                using (var r = new StreamReader(src!.Open(pw), Encoding.UTF8, detectEncodingFromByteOrderMarks: true))
                    content = await r.ReadToEndAsync();

                // Optional: wrong password should fail early
                Assert.ThrowsAny<Exception>(() =>
                {
                    using var _ = src.Open("WRONG");
                });

                // Create the destination entry with the same password and write the copied content.
                var dst = za.CreateEntry(copyName, pw, ZipArchiveEntry.EncryptionMethod.ZipCrypto);
                using var w = new StreamWriter(dst.Open(), Encoding.UTF8, bufferSize: 1024, leaveOpen: false);
                await w.WriteAsync(content);
            }

            // Assert: Both entries exist and decrypt to the expected content
            using (var za = ZipFile.Open(zipPath, ZipArchiveMode.Read))
            {
                var orig = za.GetEntry(originalName);
                var copy = za.GetEntry(copyName);
                Assert.NotNull(orig);
                Assert.NotNull(copy);

                using (var r1 = new StreamReader(orig!.Open(pw), Encoding.UTF8, detectEncodingFromByteOrderMarks: true))
                {
                    var text = await r1.ReadToEndAsync();
                    Assert.Equal(payload, text);
                }

                using (var r2 = new StreamReader(copy!.Open(pw), Encoding.UTF8, detectEncodingFromByteOrderMarks: true))
                {
                    var text = await r2.ReadToEndAsync();
                    Assert.Equal(payload, text);
                }
            }
        }


        [Fact]
        public void Update_OpenEncryptedEntry_WrongPassword_Throws()
        {
            string zipPath = NewPath("update_wrong_pw.zip");
            const string pw = "correct-pw";

            if (File.Exists(zipPath)) File.Delete(zipPath);

            using (var za = ZipFile.Open(zipPath, ZipArchiveMode.Create))
            {
                var e = za.CreateEntry("secure/file.txt", pw, ZipArchiveEntry.EncryptionMethod.ZipCrypto);
                using var w = new StreamWriter(e.Open(), Encoding.UTF8);
                w.Write("secret");
            }

            using (var za = ZipFile.Open(zipPath, ZipArchiveMode.Update))
            {
                var e = za.GetEntry("secure/file.txt");
                Assert.NotNull(e);
                Assert.ThrowsAny<Exception>(() =>
                {
                    using var _ = e.Open("wrong-pw");
                });
            }
        }


        [Fact]
        public async Task Update_EditPlainEntry_RoundTrip()
        {
            string zipPath = NewPath("update_edit_plain.zip");
            if (File.Exists(zipPath)) File.Delete(zipPath);

            // Create plain entry
            using (var za = ZipFile.Open(zipPath, ZipArchiveMode.Create))
            {
                var e = za.CreateEntry("plain.txt");
                using var w = new StreamWriter(e.Open(), Encoding.UTF8);
                await w.WriteAsync("original");
            }

            // Edit in Update mode
            using (var za = ZipFile.Open(zipPath, ZipArchiveMode.Update))
            {
                var e = za.GetEntry("plain.txt");
                Assert.NotNull(e);

                using var w = new StreamWriter(e.Open(), Encoding.UTF8);
                await w.WriteAsync("modified");
            }

            // Verify updated content
            using (var za = ZipFile.Open(zipPath, ZipArchiveMode.Read))
            {
                var e = za.GetEntry("plain.txt");
                using var r = new StreamReader(e.Open(), Encoding.UTF8);
                Assert.Equal("modified", await r.ReadToEndAsync());
            }
        }



        [Fact]
        public void Update_EditEncryptedEntryWithoutPassword_Throws()
        {
            string zipPath = NewPath("update_edit_encrypted.zip");
            const string pw = "edit-pw";

            if (File.Exists(zipPath)) File.Delete(zipPath);

            using (var za = ZipFile.Open(zipPath, ZipArchiveMode.Create))
            {
                var e = za.CreateEntry("secure/edit.txt", pw, ZipArchiveEntry.EncryptionMethod.ZipCrypto);
                using var w = new StreamWriter(e.Open(), Encoding.UTF8);
                w.Write("secret");
            }

            using (var za = ZipFile.Open(zipPath, ZipArchiveMode.Update))
            {
                var e = za.GetEntry("secure/edit.txt");
                Assert.NotNull(e);

                // Should throw because edit-in-place for encrypted entries is not supported
                Assert.Throws<InvalidOperationException>(() =>
                {
                    using var _ = e.Open(); // no password
                });
            }
        }


        [Fact]
        public async Task Update_MixedEntries_ReadEncrypted_EditPlain()
        {
            string zipPath = NewPath("update_mixed.zip");
            const string pw = "mixed-pw";

            if (File.Exists(zipPath)) File.Delete(zipPath);

            // Create initial zip with encrypted and plain entries
            using (var za = ZipFile.Open(zipPath, ZipArchiveMode.Create))
            {
                var encEntry = za.CreateEntry("secure/data.txt", pw, ZipArchiveEntry.EncryptionMethod.ZipCrypto);
                using (var w = new StreamWriter(encEntry.Open(), Encoding.UTF8))
                    await w.WriteAsync("encrypted");

                var plainEntry = za.CreateEntry("plain.txt");
                using (var w = new StreamWriter(plainEntry.Open(), Encoding.UTF8))
                    await w.WriteAsync("original");
            }

            // First update: read encrypted, modify plain
            using (var za = ZipFile.Open(zipPath, ZipArchiveMode.Update))
            {
                var enc = za.GetEntry("secure/data.txt");
                Assert.NotNull(enc);

                string encryptedContent;
                using (var r = new StreamReader(enc.Open(pw), Encoding.UTF8))
                    encryptedContent = await r.ReadToEndAsync();

                var plain = za.GetEntry("plain.txt");
                using var w = new StreamWriter(plain.Open(), Encoding.UTF8);
                await w.WriteAsync("modified");
            }

            // Second update: verify encrypted, re-modify plain
            using (var za = ZipFile.Open(zipPath, ZipArchiveMode.Update))
            {
                var enc = za.GetEntry("secure/data.txt");
                using (var r = new StreamReader(enc.Open(pw), Encoding.UTF8))
                    Assert.Equal("encrypted", await r.ReadToEndAsync());

                var plain = za.GetEntry("plain.txt");
                using var w = new StreamWriter(plain.Open(), Encoding.UTF8);
                await w.WriteAsync("modified");
            }

            // Final read: verify both entries
            using (var za = ZipFile.Open(zipPath, ZipArchiveMode.Read))
            {
                using (var r1 = new StreamReader(za.GetEntry("secure/data.txt").Open(pw), Encoding.UTF8))
                    Assert.Equal("encrypted", await r1.ReadToEndAsync());

                using (var r2 = new StreamReader(za.GetEntry("plain.txt").Open(), Encoding.UTF8))
                    Assert.Equal("modified", await r2.ReadToEndAsync());
            }
        }



        [Fact]
        public async Task Update_ModifySameEncryptedEntryMultipleTimes()
        {
            string zipPath = NewPath("update_modify_multiple.zip");
            const string pw = "multi-pw";

            if (File.Exists(zipPath)) File.Delete(zipPath);

            // Create initial encrypted entry
            using (var za = ZipFile.Open(zipPath, ZipArchiveMode.Create))
            {
                var e = za.CreateEntry("secure/data.txt", pw, ZipArchiveEntry.EncryptionMethod.ZipCrypto);
                using var w = new StreamWriter(e.Open(), Encoding.UTF8);
                await w.WriteAsync("version1");
            }

            // Modify entry multiple times
            for (int i = 2; i <= 3; i++)
            {
                using (var za = ZipFile.Open(zipPath, ZipArchiveMode.Update))
                {
                    var e = za.GetEntry("secure/data.txt");
                    Assert.NotNull(e);

                    string oldContent;
                    using (var r = new StreamReader(e!.Open(pw), Encoding.UTF8))
                        oldContent = await r.ReadToEndAsync();

                    e.Delete(); // remove old entry

                    var newEntry = za.CreateEntry("secure/data.txt", pw, ZipArchiveEntry.EncryptionMethod.ZipCrypto);
                    using var w = new StreamWriter(newEntry.Open(), Encoding.UTF8);
                    await w.WriteAsync($"{oldContent}-version{i}");
                }
            }

            // Assert final content
            using (var za = ZipFile.Open(zipPath, ZipArchiveMode.Read))
            {
                var e = za.GetEntry("secure/data.txt");
                Assert.NotNull(e);
                using var r = new StreamReader(e!.Open(pw), Encoding.UTF8);
                var text = await r.ReadToEndAsync();
                Assert.Equal("version1-version2-version3", text);
            }
        }


        [Fact]
        public async Task Update_CopyEncryptedEntryToPlainEntry()
        {
            string zipPath = NewPath("update_copy_to_plain.zip");
            const string pw = "plain-copy";

            if (File.Exists(zipPath)) File.Delete(zipPath);

            // Create encrypted entry
            using (var za = ZipFile.Open(zipPath, ZipArchiveMode.Create))
            {
                var e = za.CreateEntry("secure/original.txt", pw, ZipArchiveEntry.EncryptionMethod.ZipCrypto);
                using var w = new StreamWriter(e.Open(), Encoding.UTF8);
                await w.WriteAsync("secret content");
            }

            // Copy encrypted content to a plain entry
            using (var za = ZipFile.Open(zipPath, ZipArchiveMode.Update))
            {
                var src = za.GetEntry("secure/original.txt");
                Assert.NotNull(src);

                string content;
                using (var r = new StreamReader(src!.Open(pw), Encoding.UTF8))
                    content = await r.ReadToEndAsync();

                var plainEntry = za.CreateEntry("public/copy.txt"); // no encryption
                using var w = new StreamWriter(plainEntry.Open(), Encoding.UTF8);
                await w.WriteAsync(content);
            }

            // Assert both entries exist and content matches
            using (var za = ZipFile.Open(zipPath, ZipArchiveMode.Read))
            {
                var enc = za.GetEntry("secure/original.txt");
                var plain = za.GetEntry("public/copy.txt");
                Assert.NotNull(enc);
                Assert.NotNull(plain);

                using (var r1 = new StreamReader(enc!.Open(pw), Encoding.UTF8))
                    Assert.Equal("secret content", await r1.ReadToEndAsync());

                using (var r2 = new StreamReader(plain!.Open(), Encoding.UTF8))
                    Assert.Equal("secret content", await r2.ReadToEndAsync());
            }
        }



        [Fact]
        public void CreateEntryFromFile_WithPassword_WrongPassword_Throws()
        {
            // Arrange
            Directory.CreateDirectory(DownloadsDir);
            string srcPath = NewPath("source_wrong_pw.txt");
            string zipPath = NewPath("create_from_file_encrypted_wrongpw.zip");
            const string entryName = "secure/wrong.txt";
            const string correctPassword = "correct!";
            const string badPassword = "wrong!";
            const string payload = "secret data";

            if (File.Exists(srcPath)) File.Delete(srcPath);
            if (File.Exists(zipPath)) File.Delete(zipPath);

            File.WriteAllText(srcPath, payload, new UTF8Encoding(false));

            using (var za = ZipFile.Open(zipPath, ZipArchiveMode.Create))
            {
                var e = za.CreateEntryFromFile(
                    sourceFileName: srcPath,
                    entryName: entryName,
                    compressionLevel: CompressionLevel.Optimal,
                    password: correctPassword,
                    encryption: ZipArchiveEntry.EncryptionMethod.ZipCrypto);
            }

            // Act & Assert
            using (var za = ZipFile.Open(zipPath, ZipArchiveMode.Read))
            {
                var e = za.GetEntry(entryName);
                Assert.NotNull(e);

                Assert.ThrowsAny<Exception>(() =>
                {
                    using var _ = e!.Open(badPassword);
                });
            }
        }


        [Fact]
        public async Task CreateEntryFromFile_WithEncryption_RoundTrip()
        {
            // Arrange
            Directory.CreateDirectory(DownloadsDir);
            string srcPath = NewPath("source_plain.txt");
            string zipPath = NewPath("create_from_file_plain.zip");
            const string entryName = "plain/copy.txt";
            const string payload = "this is plain";
            const string pwd = "anything";

            if (File.Exists(srcPath)) File.Delete(srcPath);
            if (File.Exists(zipPath)) File.Delete(zipPath);

            await File.WriteAllTextAsync(srcPath, payload, new UTF8Encoding(false));

            using (var za = ZipFile.Open(zipPath, ZipArchiveMode.Create))
            {
                var e = za.CreateEntryFromFile(
                    sourceFileName: srcPath,
                    entryName: entryName,
                    compressionLevel: CompressionLevel.Optimal,
                    password: pwd,
                    encryption: ZipArchiveEntry.EncryptionMethod.ZipCrypto);
            }

            using (var za = ZipFile.Open(zipPath, ZipArchiveMode.Read))
            {
                var e = za.GetEntry(entryName);
                Assert.NotNull(e);

                using var r = new StreamReader(e!.Open(pwd), Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
                string text = await r.ReadToEndAsync();
                Assert.Equal(payload, text);

                // Opening a plain entry with a password should throw
                Assert.ThrowsAny<Exception>(() =>
                {
                    using var _ = e.Open("some-password");
                });
            }
        }




        [Fact]
        public void CreateEntry_UsesArchiveDefaults_WhenNotOverridden()
        {
            Directory.CreateDirectory(DownloadsDir);
            var zipPath = NewPath("defaults_apply.zip");
            if (File.Exists(zipPath)) File.Delete(zipPath);

            const string defaultPassword = "archive-pw";
            const string payload = "default encryption content";
            const string entryName = "secure/default.txt";

            using (var zipFs = File.Create(zipPath))
            using (var za = new ZipArchive(zipFs,
                                           ZipArchiveMode.Create,
                                           leaveOpen: false,
                                           entryNameEncoding: Encoding.UTF8,
                                           defaultPassword: defaultPassword,
                                           defaultEncryption: ZipArchiveEntry.EncryptionMethod.ZipCrypto))
            {
                var e = za.CreateEntry(entryName);

                // OPEN → WRITE → DISPOSE (single scope)
                using (var es = e.Open())
                {
                    var bytes = Encoding.UTF8.GetBytes(payload);
                    es.Write(bytes, 0, bytes.Length);
                }
                // no other entry opened while this one was open
            }

            // Verify with the archive default password
            using (var za = ZipFile.Open(zipPath, ZipArchiveMode.Read))
            {
                var e = za.GetEntry(entryName);
                Assert.NotNull(e);
                using var r = new StreamReader(e!.Open(defaultPassword), Encoding.UTF8);
                Assert.Equal(payload, r.ReadToEnd());
            }
        }

        [Fact]
        public async Task CreateMode_DefaultPassword_AppliesToMultipleEntries()
        {
            string zipPath = NewPath("defaults_multiple.zip");
            if (File.Exists(zipPath)) File.Delete(zipPath);

            const string defaultPassword = "archive-pw";

            using (var zipFs = File.Create(zipPath))
            using (var za = new ZipArchive(zipFs,
                                           ZipArchiveMode.Create,
                                           leaveOpen: false,
                                           entryNameEncoding: Encoding.UTF8,
                                           defaultPassword: defaultPassword,
                                           defaultEncryption: ZipArchiveEntry.EncryptionMethod.ZipCrypto))
            {
                var e1 = za.CreateEntry("secure/one.txt");
                using (var s1 = e1.Open())
                {
                    var b = Encoding.UTF8.GetBytes("ONE");
                    s1.Write(b, 0, b.Length);
                }

                var e2 = za.CreateEntry("secure/two.txt");
                using (var s2 = e2.Open())
                {
                    var b = Encoding.UTF8.GetBytes("TWO");
                    s2.Write(b, 0, b.Length);
                }
            }

            using (var za = ZipFile.Open(zipPath, ZipArchiveMode.Read))
            {
                using (var r1 = new StreamReader(za.GetEntry("secure/one.txt")!.Open(defaultPassword), Encoding.UTF8))
                    Assert.Equal("ONE", await r1.ReadToEndAsync());

                using (var r2 = new StreamReader(za.GetEntry("secure/two.txt")!.Open(defaultPassword), Encoding.UTF8))
                    Assert.Equal("TWO", await r2.ReadToEndAsync());
            }
        }

        [Fact]
        public async Task CreateEntry_WithExplicitPassword_OverridesDefaultPassword()
        {
            string zipPath = NewPath("override_default.zip");
            if (File.Exists(zipPath)) File.Delete(zipPath);

            const string archivePassword = "archive-pw";
            const string entryPassword = "entry-pw";

            using (var zipFs = File.Create(zipPath))
            using (var za = new ZipArchive(zipFs,
                                           ZipArchiveMode.Create,
                                           leaveOpen: false,
                                           entryNameEncoding: Encoding.UTF8,
                                           defaultPassword: archivePassword,
                                           defaultEncryption: ZipArchiveEntry.EncryptionMethod.ZipCrypto))
            {
                var e = za.CreateEntry("secure/override.txt", entryPassword, ZipArchiveEntry.EncryptionMethod.ZipCrypto);
                using (var s = e.Open())
                {
                    var b = Encoding.UTF8.GetBytes("OVERRIDE");
                    s.Write(b, 0, b.Length);
                }
            }

            using (var za = ZipFile.Open(zipPath, ZipArchiveMode.Read))
            {
                var e = za.GetEntry("secure/override.txt");
                Assert.NotNull(e);

                // Should succeed with entry password
                using (var rOk = new StreamReader(e!.Open(entryPassword), Encoding.UTF8))
                    Assert.Equal("OVERRIDE", await rOk.ReadToEndAsync());

                // Wrong: using archive default should fail
                Assert.ThrowsAny<Exception>(() =>
                {
                    using var _ = e.Open(archivePassword);
                });
            }
        }

    }


}
