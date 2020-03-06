// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.IO.Tests
{
    public class EncryptDecrypt : FileSystemTest
    {
        // On Windows Nano Server and Home Edition, file encryption with File.Encrypt(string path) throws an IOException
        // because EFS (Encrypted File System), its underlying technology, is not available on these operating systems.
        private static bool IsEncryptionSupported => PlatformDetection.IsNotWindowsNanoServer && PlatformDetection.IsNotWindowsHomeEdition;

        [ConditionalFact(typeof(EncryptDecrypt), nameof(IsEncryptionSupported))]
        public static void NullArg_ThrowsException()
        {
            AssertExtensions.Throws<ArgumentNullException>("path", () => File.Encrypt(null));
            AssertExtensions.Throws<ArgumentNullException>("path", () => File.Decrypt(null));
        }

        [ConditionalFact(typeof(EncryptDecrypt), nameof(IsEncryptionSupported))]
        public static void EncryptDecrypt_NotFound()
        {
            Assert.Throws<FileNotFoundException>(() => File.Encrypt("path"));
            Assert.Throws<FileNotFoundException>(() => File.Decrypt("path"));
        }

        [ConditionalFact(typeof(EncryptDecrypt), nameof(IsEncryptionSupported))]
        public static void EncryptDecrypt_Read()
        {
            string tmpFileName = Path.GetTempFileName();
            string textContentToEncrypt = "Content to encrypt";
            File.WriteAllText(tmpFileName, textContentToEncrypt);
            try
            {
                string fileContentRead = File.ReadAllText(tmpFileName);
                Assert.Equal(textContentToEncrypt, fileContentRead);

                try
                {
                    File.Encrypt(tmpFileName);
                }
                catch (IOException e) when (e.HResult == unchecked((int)0x80070490))
                {
                    // Ignore ERROR_NOT_FOUND 1168 (0x490). It is reported when EFS is disabled by domain policy.
                    return;
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
    }
}
