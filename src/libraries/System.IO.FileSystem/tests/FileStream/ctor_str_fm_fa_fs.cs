// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using Xunit;

namespace System.IO.Tests
{
    public partial class FileStream_ctor_str_fm_fa_fs : FileStream_ctor_str_fm_fa
    {
        protected override FileStream CreateFileStream(string path, FileMode mode, FileAccess access)
        {
            return CreateFileStream(path, mode, access, FileShare.Read);
        }

        protected virtual FileStream CreateFileStream(string path, FileMode mode, FileAccess access, FileShare share)
        {
            return new FileStream(path, mode, access, share);
        }

        [Fact]
        public void InvalidShareThrows()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("share", () => CreateFileStream(GetTestFilePath(), FileMode.Create, FileAccess.ReadWrite, ~FileShare.None));
        }

        static readonly FileShare[] shares = 
            { 
                FileShare.None,
                FileShare.Inheritable,
                FileShare.Delete, FileShare.Delete | FileShare.Inheritable, 
                FileShare.Read, FileShare.Read | FileShare.Inheritable, FileShare.Read | FileShare.Delete, FileShare.Read | FileShare.Delete | FileShare.Inheritable,
                FileShare.Write, FileShare.Write | FileShare.Inheritable, FileShare.Write | FileShare.Delete, FileShare.Write | FileShare.Delete | FileShare.Inheritable,
                FileShare.ReadWrite, FileShare.ReadWrite | FileShare.Inheritable, FileShare.ReadWrite | FileShare.Delete, FileShare.ReadWrite | FileShare.Delete | FileShare.Inheritable
            };

        [Fact]
        public void FileShareOpen()
        {
            // create the file
            string fileName = GetTestFilePath();
            using (FileStream fs = CreateFileStream(fileName, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite | FileShare.Delete))
            { }

            // just check that the inputs are accepted, actual sharing varies by platform so we separate the behavior testing
            foreach(FileShare share in shares)
            {
                using (FileStream fs = CreateFileStream(fileName, FileMode.Open, FileAccess.ReadWrite, share))
                { }

                using (FileStream fs = CreateFileStream(fileName, FileMode.Open, FileAccess.Write, share))
                { }

                using (FileStream fs = CreateFileStream(fileName, FileMode.Open, FileAccess.Read, share))
                { }
            }
        }

        [Fact]
        public void FileShareCreate()
        {
            // just check that the inputs are accepted, actual sharing varies by platform so we separate the behavior testing
            int i = 0;
            foreach (FileShare share in shares)
            {
                using (FileStream fs = CreateFileStream(GetTestFilePath(i++), FileMode.Create, FileAccess.ReadWrite, share))
                { }

                using (FileStream fs = CreateFileStream(GetTestFilePath(i++), FileMode.Create, FileAccess.Write, share))
                { }
            }
        }

        [Fact]
        public void FileShareOpenOrCreate()
        {
            // just check that the inputs are accepted, actual sharing varies by platform so we separate the behavior testing
            int i = 0;
            foreach (FileShare share in shares)
            {
                using (FileStream fs = CreateFileStream(GetTestFilePath(i++), FileMode.OpenOrCreate, FileAccess.ReadWrite, share))
                { }

                using (FileStream fs = CreateFileStream(GetTestFilePath(i++), FileMode.OpenOrCreate, FileAccess.Write, share))
                { }

                using (FileStream fs = CreateFileStream(GetTestFilePath(i++), FileMode.OpenOrCreate, FileAccess.Read, share))
                { }
            }
        }
    }
}
