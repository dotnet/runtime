// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using Xunit;

namespace System.IO.Tests
{
    public class RandomAccess_GetLength : RandomAccess_Base<long>
    {
        protected override long MethodUnderTest(SafeFileHandle handle, byte[] bytes, long fileOffset)
            => RandomAccess.GetLength(handle);

        protected override bool UsesOffsets => false;

        [Theory]
        [MemberData(nameof(GetSyncAsyncOptions))]
        public void ReturnsZeroForEmptyFile(FileOptions options)
        {
            using (SafeFileHandle handle = File.OpenHandle(GetTestFilePath(), FileMode.CreateNew, FileAccess.Write, options: options))
            {
                Assert.Equal(0, RandomAccess.GetLength(handle));
            }
        }

        [Theory]
        [MemberData(nameof(GetSyncAsyncOptions))]
        public void ReturnsExactSizeForNonEmptyFiles(FileOptions options)
        {
            const int fileSize = 123;
            string filePath = GetTestFilePath();
            File.WriteAllBytes(filePath, new byte[fileSize]);

            using (SafeFileHandle handle = File.OpenHandle(filePath, FileMode.Open, options: options))
            {
                Assert.Equal(fileSize, RandomAccess.GetLength(handle));
            }
        }

        [PlatformSpecific(TestPlatforms.Windows)]
        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsPrivilegedProcess))]
        [MemberData(nameof(GetSyncAsyncOptions))]
        public void ReturnsActualLengthForDevices(FileOptions options)
        {
            // Both File.Exists and Path.Exists return false when "\\?\PhysicalDrive0" exists
            // that is why we just try and swallow the exception when it occurs.
            // Exception can be also thrown when the file is in use (#73925).
            try
            {
                using (SafeFileHandle handle = File.OpenHandle(@"\\?\PhysicalDrive0", FileMode.Open, options: options))
                {
                    long length = RandomAccess.GetLength(handle);
                    Assert.True(length > 0);
                }
            }
            catch (IOException) { }
        }
    }
}
