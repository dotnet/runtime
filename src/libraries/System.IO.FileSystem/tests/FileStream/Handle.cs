// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.IO.Tests
{
    public class FileStream_Handle : FileSystemTest
    {
        [Fact]
        public void Handle_SameAsSafeFileHandle()
        {
            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create))
            {
#pragma warning disable CS0618
                Assert.Equal(fs.SafeFileHandle.DangerousGetHandle(), fs.Handle);
#pragma warning restore CS0618
            }
        }
    }
}
