// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.IO.Compression.Tests
{
    public partial class ZipTest : IDisposable
    {
        private const string DirPrefix = "ZipTests";

        private readonly DirectoryInfo _rootTestDirectory;
        private int _dirCount = 1;

        public ZipTest()
        {
            _rootTestDirectory = new DirectoryInfo(Path.Combine(Path.GetTempPath(), 
                DirPrefix + "Root_" + Guid.NewGuid().ToString("N")));
            _rootTestDirectory.Create();
        }

        public void Dispose()
        {
            _rootTestDirectory.Delete(recursive: true);
        }

        private string GetTmpFilePath()
        {
            return Path.Combine(_rootTestDirectory.FullName, "tmp" + Guid.NewGuid().ToString("N"));
        }

        private string GetTmpDirPath(bool create = false)
        {
            string tempPath = Path.Combine(_rootTestDirectory.FullName, DirPrefix + _dirCount++);
            if (create)
            {
                Directory.CreateDirectory(tempPath);
            }
            return tempPath;
        }

        private string CreateTempCopyFile(string path)
        {
            string newPath = GetTmpFilePath();
            File.Copy(path, newPath, overwrite: false);
            return newPath;
        }
    }
}
