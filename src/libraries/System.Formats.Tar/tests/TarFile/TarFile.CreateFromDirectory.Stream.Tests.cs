// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using Xunit;

namespace System.Formats.Tar.Tests
{
    public class TarFile_CreateFromDirectory_Stream_Tests : TarTestsBase
    {
        [Fact]
        public void InvalidPath_Throws()
        {
            using MemoryStream archive = new MemoryStream();
            Assert.Throws<ArgumentNullException>(() => TarFile.CreateFromDirectory(sourceDirectoryName: null,destination: archive, includeBaseDirectory: false));
            Assert.Throws<ArgumentException>(() => TarFile.CreateFromDirectory(sourceDirectoryName: string.Empty,destination: archive, includeBaseDirectory: false));
        }

        [Fact]
        public void NullStream_Throws()
        {
            using MemoryStream archive = new MemoryStream();
            Assert.Throws<ArgumentNullException>(() => TarFile.CreateFromDirectory(sourceDirectoryName: "path",destination: null, includeBaseDirectory: false));
        }

        [Fact]
        public void UnwritableStream_Throws()
        {
            using MemoryStream archive = new MemoryStream();
            using WrappedStream unwritable = new WrappedStream(archive, canRead: true, canWrite: false, canSeek: true);
            Assert.Throws<IOException>(() => TarFile.CreateFromDirectory(sourceDirectoryName: "path",destination: unwritable, includeBaseDirectory: false));
        }

        [Fact]
        public void NonExistentDirectory_Throws()
        {
            using TempDirectory root = new TempDirectory();
            string dirPath = Path.Join(root.Path, "dir");

            using MemoryStream archive = new MemoryStream();
            Assert.Throws<DirectoryNotFoundException>(() => TarFile.CreateFromDirectory(sourceDirectoryName: dirPath, destination: archive, includeBaseDirectory: false));
        }
    }
}