// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32.SafeHandles;
using System;
using System.IO;
using Xunit;

namespace System.IO.FileSystem.Tests
{
    partial class FileStream_CanSeek
    {
        internal const int STD_OUTPUT_HANDLE = -11;

        [Fact]
        public void CanSeekReturnsFalseForPipe()
        {
            SafeFileHandle handle = new SafeFileHandle((IntPtr)STD_OUTPUT_HANDLE, false);
            using (FileStream fs = new FileStream(handle, FileAccess.Write, 1, false))
            {
                Assert.False(fs.CanSeek);
            }
        }
    }
}
