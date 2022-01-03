// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.XUnitExtensions;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Security;
using System.ServiceProcess;
using Xunit;
using Xunit.Abstractions;

namespace System.IO.Tests
{
    [ActiveIssue("https://github.com/dotnet/runtime/issues/34582", TestPlatforms.Windows, TargetFrameworkMonikers.Netcoreapp, TestRuntimes.Mono)]
    public partial class EncryptDecrypt : FileSystemTest
    {
        private readonly ITestOutputHelper _output;

        public EncryptDecrypt(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void NullArg_ThrowsException()
        {
            AssertExtensions.Throws<ArgumentNullException>("path", () => File.Encrypt(null));
            AssertExtensions.Throws<ArgumentNullException>("path", () => File.Decrypt(null));
        }

        [SkipOnTargetFramework(TargetFrameworkMonikers.Netcoreapp)]
        [Fact]
        public void EncryptDecrypt_NotSupported()
        {
            Assert.Throws<PlatformNotSupportedException>(() => File.Encrypt("path"));
            Assert.Throws<PlatformNotSupportedException>(() => File.Decrypt("path"));
        }

        // On Windows Nano Server and Home Edition, file encryption with File.Encrypt(string path) throws an IOException
        // because EFS (Encrypted File System), its underlying technology, is not available on these operating systems.
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer), nameof(PlatformDetection.IsNotWindowsHomeEdition))]
        [PlatformSpecific(TestPlatforms.Windows)]
        [OuterLoop] // Occasional failures: https://github.com/dotnet/runtime/issues/12339
        public void EncryptDecrypt_Read()
        {
            string tmpFileName = Path.GetTempFileName();
            string textContentToEncrypt = "Content to encrypt";
            File.WriteAllText(tmpFileName, textContentToEncrypt);
            try
            {
                string fileContentRead = File.ReadAllText(tmpFileName);
                Assert.Equal(textContentToEncrypt, fileContentRead);

                EnsureEFSServiceStarted();

                try
                {
                    File.Encrypt(tmpFileName);
                }
                catch (IOException e) when (e.HResult == unchecked((int)0x80070490) ||
                                           (e.HResult == unchecked((int)0x80071776)))
                {
                    // Ignore ERROR_NOT_FOUND 1168 (0x490). It is reported when EFS is disabled by domain policy.
                    // Ignore ERROR_NO_USER_KEYS (0x1776). This occurs when no user key exists to encrypt with.
                    throw new SkipTestException($"Encrypt not available. Error 0x{e.HResult:X}");
                }
                catch (IOException e)
                {
                    _output.WriteLine($"Encrypt failed with {e.Message} 0x{e.HResult:X}");
                    LogEFSDiagnostics();
                    throw;
                }

                Assert.Equal(fileContentRead, File.ReadAllText(tmpFileName));
                Assert.Equal(FileAttributes.Encrypted, (FileAttributes.Encrypted & File.GetAttributes(tmpFileName)));

                File.Decrypt(tmpFileName);
                Assert.Equal(fileContentRead, File.ReadAllText(tmpFileName));
                Assert.NotEqual(FileAttributes.Encrypted, (FileAttributes.Encrypted & File.GetAttributes(tmpFileName)));
            }
            finally
            {
                File.Delete(tmpFileName);
            }
        }

        partial void EnsureEFSServiceStarted(); // no-op on Unix

        partial void LogEFSDiagnostics(); // no-op on Unix currently
    }
}
