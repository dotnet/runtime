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
            string testFilePath = GetTestFilePath();

            File.WriteAllBytes(testFilePath, RandomNumberGenerator.GetBytes(count: 100));

            using (SafeFileHandle handle = File.OpenHandle(testFilePath, FileMode.Open, FileAccess.Read))
            {
                // On Windows, flushing a file opened for reading should throw an exception. The docs
                // for FlushFileBuffers() say the handle must have been created with the GENERIC_WRITE
                // access right but that is not the case here since we opened the file for reading.
                Assert.Throws<UnauthorizedAccessException>(() => RandomAccess.FlushToDisk(handle));
            }
        }
    }
}
