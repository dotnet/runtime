// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.IO.Tests
{
    public class EnabledFileLockingSwitchTests
    {
        [Fact]
        public static void ConfigSwitchIsHonored()
        {
            Assert.True(PlatformDetection.IsFileLockingEnabled);

            string path = Path.GetTempFileName();
            try
            {
                using FileStream stream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                Assert.Throws<IOException>(() => new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None).Dispose());
            }
            finally
            {
                File.Delete(path);
            }
        }
    }
}
