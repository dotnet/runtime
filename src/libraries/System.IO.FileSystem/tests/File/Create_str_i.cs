// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace System.IO.FileSystem.Tests
{
    public class File_Create_str_i : File_Create_str
    {
        #region Utilities

        protected const int DefaultBufferSize = 4096;

        public override FileStream Create(string path)
        {
            return File.Create(path, DefaultBufferSize);
        }

        public virtual FileStream Create(string path, int bufferSize)
        {
            return File.Create(path, bufferSize);
        }

        #endregion

        #region UniversalTests

        [Fact]
        public void NegativeBuffer()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Create(GetTestFilePath(), -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => Create(GetTestFilePath(), -100));
        }

        #endregion
    }
}
