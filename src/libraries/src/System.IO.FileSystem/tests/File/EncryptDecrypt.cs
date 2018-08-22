// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Security;
using Xunit;

namespace System.IO.Tests
{
    public class EncryptDecrypt : FileSystemTest
    {
        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetcoreCoreRT)]
        [SkipOnTargetFramework(TargetFrameworkMonikers.Uap, "File encryption is not supported on this platform.")]
        public static void NullArg_ThrowsException()
        {
            AssertExtensions.Throws<ArgumentNullException>("path", () => File.Encrypt(null));
            AssertExtensions.Throws<ArgumentNullException>("path", () => File.Decrypt(null));
        }

        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        [SkipOnTargetFramework(TargetFrameworkMonikers.Netcoreapp)]
        [Fact]
        public static void EncryptDecrypt_NotSupported()
        {
            Assert.Throws<PlatformNotSupportedException>(() => File.Encrypt("path"));
            Assert.Throws<PlatformNotSupportedException>(() => File.Decrypt("path"));

            Assert.Throws<PlatformNotSupportedException>(() => File.Encrypt(null));
            Assert.Throws<PlatformNotSupportedException>(() => File.Decrypt(null));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        [SkipOnTargetFramework(TargetFrameworkMonikers.Uap, "File encryption is not supported on this platform.")]
        [PlatformSpecific(TestPlatforms.Windows)]
        public static void EncryptDecrypt_Read()
        {
            string tmpFileName = Path.GetTempFileName();
            string textContentToEncrypt = "Content to encrypt";
            File.WriteAllText(tmpFileName, textContentToEncrypt);
            try
            {
                string fileContentRead = File.ReadAllText(tmpFileName);
                Assert.Equal(textContentToEncrypt, fileContentRead);

                File.Encrypt(tmpFileName);
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
    }
}
