// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace System.IO.FileSystem.Tests
{
    public class DirectoryInfo_Delete : Directory_Delete_str
    {
        public override void Delete(string path)
        {
            new DirectoryInfo(path).Delete();
        }

        [Fact]
        public void ExistsDoesntRefreshOnDelete()
        {
            DirectoryInfo dir = Directory.CreateDirectory(Path.Combine(TestDirectory, Path.GetRandomFileName()));

            Assert.True(dir.Exists);

            dir.Delete();

            Assert.True(dir.Exists);
            dir.Refresh();
            Assert.False(dir.Exists);
        }
    }

    public class DirectoryInfo_Delete_bool : Directory_Delete_str_bool
    {
        public override void Delete(string path)
        {
            new DirectoryInfo(path).Delete(false);
        }

        public override void Delete(string path, bool recursive)
        {
            new DirectoryInfo(path).Delete(recursive);
        }
    }
}
