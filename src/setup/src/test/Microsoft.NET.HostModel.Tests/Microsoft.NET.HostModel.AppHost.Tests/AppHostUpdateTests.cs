using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using FluentAssertions;
using Xunit;
using Microsoft.NET.HostModel.AppHost;
using Microsoft.DotNet.CoreSetup.Test;

namespace Microsoft.NET.HostModel.Tests
{
    public class AppHostUpdateTests
    {
        /// <summary>
        /// hash value embedded in default apphost executable in a place where the path to the app binary should be stored.
        /// </summary>
        private const string AppBinaryPathPlaceholder = "c3ab8ff13720e8ad9047dd39466b3c8974e592c2fa383d4a3960714caef0c4f2";
        private readonly static byte[] AppBinaryPathPlaceholderSearchValue = Encoding.UTF8.GetBytes(AppBinaryPathPlaceholder);

        [Fact]
        public void ItEmbedsAppBinaryPath()
        {
            using (TestDirectory testDirectory = TestDirectory.Create())
            {
                string sourceAppHostMock = PrepareAppHostMockFile(testDirectory);
                string destinationFilePath = Path.Combine(testDirectory.Path, "DestinationAppHost.exe.mock");
                string appBinaryFilePath = "Test/App/Binary/Path.dll";

                HostWriter.CreateAppHost(
                    sourceAppHostMock,
                    destinationFilePath,
                    appBinaryFilePath);

                byte[] binaryPathBlob = Encoding.UTF8.GetBytes(appBinaryFilePath);
                byte[] result = File.ReadAllBytes(destinationFilePath);
                result
                    .Skip(WindowsFileHeader.Length)
                    .Take(binaryPathBlob.Length)
                    .Should()
                    .BeEquivalentTo(binaryPathBlob);

                BitConverter
                    .ToUInt16(result, SubsystemOffset)
                    .Should()
                    .Be(3);
            }
        }

        [Fact]
        public void ItFailsToEmbedAppBinaryIfHashIsWrong()
        {
            using (TestDirectory testDirectory = TestDirectory.Create())
            {
                string sourceAppHostMock = PrepareAppHostMockFile(testDirectory, content =>
                {
                    // Corrupt the hash value
                    content[WindowsFileHeader.Length + 1]++;
                });
                string destinationFilePath = Path.Combine(testDirectory.Path, "DestinationAppHost.exe.mock");
                string appBinaryFilePath = "Test/App/Binary/Path.dll";

                Assert.Throws<PlaceHolderNotFoundInAppHostException>(() =>
                    HostWriter.CreateAppHost(
                        sourceAppHostMock,
                        destinationFilePath,
                        appBinaryFilePath));

                File.Exists(destinationFilePath).Should().BeFalse();
            }
        }

        [Fact]
        public void ItFailsToEmbedTooLongAppBinaryPath()
        {
            using (TestDirectory testDirectory = TestDirectory.Create())
            {
                string sourceAppHostMock = PrepareAppHostMockFile(testDirectory);
                string destinationFilePath = Path.Combine(testDirectory.Path, "DestinationAppHost.exe.mock");
                string appBinaryFilePath = new string('a', 1024 + 5);

                Assert.Throws<AppNameTooLongException>(() =>
                    HostWriter.CreateAppHost(
                        sourceAppHostMock,
                        destinationFilePath,
                        appBinaryFilePath));

                File.Exists(destinationFilePath).Should().BeFalse();
            }
        }

        [Fact]
        public void ItCanSetWindowsGUISubsystem()
        {
            using (TestDirectory testDirectory = TestDirectory.Create())
            {
                string sourceAppHostMock = PrepareAppHostMockFile(testDirectory);
                string destinationFilePath = Path.Combine(testDirectory.Path, "DestinationAppHost.exe.mock");
                string appBinaryFilePath = "Test/App/Binary/Path.dll";

                HostWriter.CreateAppHost(
                    sourceAppHostMock,
                    destinationFilePath,
                    appBinaryFilePath,
                    windowsGraphicalUserInterface: true);

                BitConverter
                    .ToUInt16(File.ReadAllBytes(destinationFilePath), SubsystemOffset)
                    .Should()
                    .Be(2);
            }
        }

        [Fact]
        public void ItFailsToSetGUISubsystemOnNonWindowsPEFile()
        {
            using (TestDirectory testDirectory = TestDirectory.Create())
            {
                string sourceAppHostMock = PrepareAppHostMockFile(testDirectory, content =>
                {
                    // Windows PE files must start with 0x5A4D, so write some other value here.
                    content[0] = 1;
                    content[1] = 2;
                });
                string destinationFilePath = Path.Combine(testDirectory.Path, "DestinationAppHost.exe.mock");
                string appBinaryFilePath = "Test/App/Binary/Path.dll";

                Assert.Throws<AppHostNotPEFileException>(() =>
                    HostWriter.CreateAppHost(
                        sourceAppHostMock,
                        destinationFilePath,
                        appBinaryFilePath,
                        windowsGraphicalUserInterface: true));

                File.Exists(destinationFilePath).Should().BeFalse();
            }
        }

        [Fact]
        public void ItFailsToSetGUISubsystemWithWrongDefault()
        {
            using (TestDirectory testDirectory = TestDirectory.Create())
            {
                string sourceAppHostMock = PrepareAppHostMockFile(testDirectory, content =>
                {
                    // Corrupt the value of the subsystem (the default should be 3)
                    content[SubsystemOffset] = 42;
                });
                string destinationFilePath = Path.Combine(testDirectory.Path, "DestinationAppHost.exe.mock");
                string appBinaryFilePath = "Test/App/Binary/Path.dll";

                Assert.Throws<AppHostNotCUIException>(() =>
                    HostWriter.CreateAppHost(
                        sourceAppHostMock,
                        destinationFilePath,
                        appBinaryFilePath,
                        windowsGraphicalUserInterface: true));

                File.Exists(destinationFilePath).Should().BeFalse();
            }
        }

        private string PrepareAppHostMockFile(TestDirectory testDirectory, Action<byte[]> customize = null)
        {
            // For now we're testing the AppHost on Windows PE files only.
            // The only customization which we do on non-Windows files is the embedding
            // of the binary path, which works the same regardless of the file format.

            int size = WindowsFileHeader.Length + AppBinaryPathPlaceholderSearchValue.Length;
            byte[] content = new byte[size];
            Array.Copy(WindowsFileHeader, 0, content, 0, WindowsFileHeader.Length);
            Array.Copy(AppBinaryPathPlaceholderSearchValue, 0, content, WindowsFileHeader.Length, AppBinaryPathPlaceholderSearchValue.Length);

            customize?.Invoke(content);

            string filePath = Path.Combine(testDirectory.Path, "SourceAppHost.exe.mock");
            File.WriteAllBytes(filePath, content);
            return filePath;
        }

        private const int SubsystemOffset = 0xF0 + 0x5C;

        // This is a dump of first 350 bytes of a windows apphost.exe
        // This includes the PE header and part of the Optional header
        private static readonly byte[] WindowsFileHeader = new byte[] {
            77, 90, 144, 0, 3, 0, 0, 0, 4, 0, 0, 0, 255, 255, 0, 0, 184,
            0, 0, 0, 0, 0, 0, 0, 64, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            240, 0, 0, 0, 14, 31, 186, 14, 0, 180, 9, 205,
            33, 184, 1, 76, 205, 33, 84, 104, 105, 115, 32, 112, 114, 111,
            103, 114, 97, 109, 32, 99, 97, 110, 110, 111, 116, 32, 98, 101,
            32, 114, 117, 110, 32, 105, 110, 32, 68, 79, 83, 32, 109, 111,
            100, 101, 46, 13, 13, 10, 36, 0, 0, 0, 0, 0, 0, 0, 30, 91, 134,
            254, 90, 58, 232, 173, 90, 58, 232, 173, 90, 58, 232, 173, 97,
            100, 235, 172, 93, 58, 232, 173, 97, 100, 237, 172, 99, 58,
            232, 173, 97, 100, 236, 172, 123, 58, 232, 173, 83, 66, 123,
            173, 72, 58, 232, 173, 135, 197, 35, 173, 89, 58, 232, 173,
            90, 58, 233, 173, 204, 58, 232, 173, 205, 100, 237, 172, 92,
            58, 232, 173, 200, 100, 23, 173, 91, 58, 232, 173, 205, 100, 234,
            172, 91, 58, 232, 173, 82, 105, 99, 104, 90, 58, 232, 173, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            80, 69, 0, 0, 100, 134, 7, 0, 29, 151, 54, 91, 0, 0, 0, 0, 0, 0,
            0, 0, 240, 0, 34, 0, 11, 2, 14, 0, 0, 28, 1, 0, 0, 8, 1, 0, 0, 0,
            0, 0, 80, 231, 0, 0, 0, 16, 0, 0, 0, 0, 0, 64, 1, 0, 0, 0, 0, 16,
            0, 0, 0, 2, 0, 0, 6, 0, 0, 0, 0, 0, 0, 0, 6, 0, 0, 0, 0, 0, 0, 0,
            0, 112, 2, 0, 0, 4, 0, 0, 0, 0, 0, 0, 3, 0, 96, 193, 0, 0, 24,
            0, 0, 0, 0, 0, 0, 16, 0, 0, 0, 0 };

        private class TestDirectory : IDisposable
        {
            public string Path { get; private set; }

            private TestDirectory(string path)
            {
                Path = path;
                Directory.CreateDirectory(path);
            }

            public static TestDirectory Create([CallerMemberName] string callingMethod = "")
            {
                string path = System.IO.Path.Combine(
                    System.IO.Path.GetTempPath(),
                    "dotNetSdkUnitTest_" + callingMethod + (Guid.NewGuid().ToString().Substring(0, 8)));
                return new TestDirectory(path);
            }

            public void Dispose()
            {
                if (Directory.Exists(Path))
                {
                    Directory.Delete(Path, true);
                }
            }
        }
    }
}