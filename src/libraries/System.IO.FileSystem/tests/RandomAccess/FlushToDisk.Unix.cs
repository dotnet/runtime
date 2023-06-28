// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography;
using Microsoft.Win32.SafeHandles;
using Xunit;

namespace System.IO.Tests
{
    public partial class RandomAccess_FlushToDisk : RandomAccess_Base<long>
    {
        [Fact]
        public void FlushFileOpenedForReading()
        {
            // Save test file path so we can refer to the same file throughout the test.
            string testFilePath = GetTestFilePath();

            // Write random bytes to file so it exists for reading later below.
            const int FileByteCount = 100;
            File.WriteAllBytes(testFilePath, RandomNumberGenerator.GetBytes(FileByteCount));

            // Open the file for reading.
            using (SafeFileHandle handle = File.OpenHandle(testFilePath, FileMode.Open, FileAccess.Read))
            {
                // On non-Windows platforms (notably Unix), flushing a file opened for reading should succeed.
                // It appears that these platforms initially behaved like Windows but then changed their
                // behavior (see https://www.austingroupbugs.net/view.php?id=671 for the discussion and
                // https://pubs.opengroup.org/onlinepubs/9699919799/functions/aio_fsync.html for the spec
                // containing the words "Note that even if the file descriptor is not open for writing...").
                RandomAccess.FlushToDisk(handle);

                // The file length should be unchanged after flushing to disk since we have not written
                // anything else to the file.
                Assert.Equal(FileByteCount, RandomAccess.GetLength(handle));
            }
        }
    }
}
