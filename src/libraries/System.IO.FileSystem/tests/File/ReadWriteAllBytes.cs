// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using System.IO.Pipes;
using Microsoft.DotNet.XUnitExtensions;

namespace System.IO.Tests
{
    public class File_ReadWriteAllBytes : FileSystemTest
    {
        [Fact]
        public void NullParameters()
        {
            string path = GetTestFilePath();
            Assert.Throws<ArgumentNullException>(() => File.WriteAllBytes(null, new byte[0]));
            Assert.Throws<ArgumentNullException>(() => File.WriteAllBytes(path, null));
            Assert.Throws<ArgumentNullException>(() => File.ReadAllBytes(null));
        }

        [Fact]
        public void InvalidParameters()
        {
            Assert.Throws<ArgumentException>(() => File.WriteAllBytes(string.Empty, new byte[0]));
            Assert.Throws<ArgumentException>(() => File.ReadAllBytes(string.Empty));
        }

        [Fact]
        public void Read_FileNotFound()
        {
            string path = GetTestFilePath();
            Assert.Throws<FileNotFoundException>(() => File.ReadAllBytes(path));
        }

        [Fact]
        public void EmptyContentCreatesFile()
        {
            string path = GetTestFilePath();
            File.WriteAllBytes(path, new byte[0]);
            Assert.True(File.Exists(path));
            Assert.Empty(File.ReadAllText(path));
            File.Delete(path);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(100)]
        public void ValidWrite(int size)
        {
            string path = GetTestFilePath();
            byte[] buffer = Encoding.UTF8.GetBytes(new string('c', size));
            File.WriteAllBytes(path, buffer);
            Assert.Equal(buffer, File.ReadAllBytes(path));
            File.Delete(path);
        }

        [Fact]
        public void Overwrite()
        {
            string path = GetTestFilePath();
            byte[] bytes = Encoding.UTF8.GetBytes(new string('c', 100));
            byte[] overwriteBytes = Encoding.UTF8.GetBytes(new string('b', 50));
            File.WriteAllBytes(path, bytes);
            File.WriteAllBytes(path, overwriteBytes);
            Assert.Equal(overwriteBytes, File.ReadAllBytes(path));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsFileLockingEnabled))]
        public void OpenFile_ThrowsIOException()
        {
            string path = GetTestFilePath();
            byte[] bytes = Encoding.UTF8.GetBytes(new string('c', 100));
            using (File.Create(path))
            {
                Assert.Throws<IOException>(() => File.WriteAllBytes(path, bytes));
                Assert.Throws<IOException>(() => File.ReadAllBytes(path));
            }
        }

        /// <summary>
        /// On Unix, modifying a file that is ReadOnly will fail under normal permissions.
        /// If the test is being run under the superuser, however, modification of a ReadOnly
        /// file is allowed. On Windows, modifying a file that is ReadOnly will always fail.
        /// </summary>
        [Fact]
        public void WriteToReadOnlyFile()
        {
            string path = GetTestFilePath();
            File.Create(path).Dispose();
            File.SetAttributes(path, FileAttributes.ReadOnly);
            try
            {
                if (PlatformDetection.IsNotWindows && PlatformDetection.IsPrivilegedProcess)
                {
                    File.WriteAllBytes(path, "text"u8.ToArray());
                    Assert.Equal("text"u8.ToArray(), File.ReadAllBytes(path));
                }
                else
                    Assert.Throws<UnauthorizedAccessException>(() => File.WriteAllBytes(path, "text"u8.ToArray()));
            }
            finally
            {
                File.SetAttributes(path, FileAttributes.Normal);
            }
        }

        [Fact]
        public void EmptyFile_ReturnsEmptyArray()
        {
            string path = GetTestFilePath();
            File.Create(path).Dispose();
            Assert.Equal(0, File.ReadAllBytes(path).Length);
        }

        [Theory]
        [PlatformSpecific(TestPlatforms.Linux)]
        [InlineData("/proc/cmdline")]
        [InlineData("/proc/version")]
        [InlineData("/proc/filesystems")]
        public void ProcFs_EqualsReadAllText(string path)
        {
            byte[] bytes = null;
            string text = null;

            const int NumTries = 3; // some of these could theoretically change between reads, so allow retries just in case
            for (int i = 1; i <= NumTries; i++)
            {
                try
                {
                    bytes = File.ReadAllBytes(path);
                    text = File.ReadAllText(path);
                    Assert.Equal(text, Encoding.UTF8.GetString(bytes));
                }
                catch when (i < NumTries) { }
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Linux)]
        public void ReadAllBytes_ProcFs_Uptime_ContainsTwoNumbers()
        {
            string text = Encoding.UTF8.GetString(File.ReadAllBytes("/proc/uptime"));
            string[] parts = text.Split(new [] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            Assert.Equal(2, parts.Length);
            Assert.True(double.TryParse(parts[0].Trim(), out _));
            Assert.True(double.TryParse(parts[1].Trim(), out _));
        }

        [Theory]
        [PlatformSpecific(TestPlatforms.Linux)]
        [InlineData("/proc/meminfo")]
        [InlineData("/proc/stat")]
        [InlineData("/proc/cpuinfo")]
        public void ProcFs_NotEmpty(string path)
        {
            Assert.InRange(File.ReadAllBytes(path).Length, 1, int.MaxValue);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // DOS device paths (\\.\ and \\?\) are a Windows concept
        [ActiveIssue("https://github.com/dotnet/runtime/issues/60427")]
        public async Task ReadAllBytes_NonSeekableFileStream_InWindows()
        {
            string pipeName = GetNamedPipeServerStreamName();
            string pipePath = Path.GetFullPath($@"\\.\pipe\{pipeName}");

            var namedPipeWriterStream = new NamedPipeServerStream(pipeName, PipeDirection.Out);
            var contentBytes = new byte[] { 1, 2, 3 };

            using (var cts = new CancellationTokenSource())
            {
                Task writingServerTask = WaitConnectionAndWritePipeStreamAsync(namedPipeWriterStream, contentBytes, cts.Token);
                Task<byte[]> readTask = Task.Run(() => File.ReadAllBytes(pipePath), cts.Token);
                cts.CancelAfter(TimeSpan.FromSeconds(3));

                await writingServerTask;
                byte[] readBytes = await readTask;
                Assert.Equal<byte>(contentBytes, readBytes);
            }

            static async Task WaitConnectionAndWritePipeStreamAsync(NamedPipeServerStream namedPipeWriterStream, byte[] contentBytes, CancellationToken cancellationToken)
            {
                await using (namedPipeWriterStream)
                {
                    await namedPipeWriterStream.WaitForConnectionAsync(cancellationToken);
                    await namedPipeWriterStream.WriteAsync(contentBytes, cancellationToken);
                }
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix & ~TestPlatforms.Browser & ~TestPlatforms.iOS & ~TestPlatforms.tvOS)]
        public async Task ReadAllBytes_NonSeekableFileStream_InUnix()
        {
            string fifoPath = GetTestFilePath();
            Assert.Equal(0, mkfifo(fifoPath, 438 /* 666 in octal */ ));

            var contentBytes = new byte[] { 1, 2, 3 };

            await Task.WhenAll(
                Task.Run(() =>
                {
                    byte[] readBytes = File.ReadAllBytes(fifoPath);
                    Assert.Equal<byte>(contentBytes, readBytes);
                }),
                Task.Run(() =>
                {
                    using var fs = new FileStream(fifoPath, FileMode.Open, FileAccess.Write, FileShare.Read);
                    foreach (byte content in contentBytes)
                    {
                        fs.WriteByte(content);
                    }
                }));
        }
    }
}
