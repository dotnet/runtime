// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.IO.Tests
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
