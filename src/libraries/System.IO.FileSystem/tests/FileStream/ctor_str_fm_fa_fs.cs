// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.IO.Tests
{
    public partial class FileStream_ctor_str_fm_fa_fs : FileStream_ctor_str_fm_fa
    {
        protected override FileStream CreateFileStream(string path, FileMode mode, FileAccess access) =>
            CreateFileStream(path, mode, access, FileShare.Read);

        protected virtual FileStream CreateFileStream(string path, FileMode mode, FileAccess access, FileShare share) =>
            new FileStream(path, mode, access, share);

        [Fact]
        public void InvalidShareThrows()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("share", () => CreateFileStream(GetTestFilePath(), FileMode.Create, FileAccess.ReadWrite, ~FileShare.None));
        }

        private static readonly FileShare[] s_shares =
        {
            FileShare.None,
            FileShare.Delete,
            FileShare.Read, FileShare.Read | FileShare.Delete,
            FileShare.Write, FileShare.Write | FileShare.Delete,
            FileShare.ReadWrite, FileShare.ReadWrite | FileShare.Delete

            // Does not include FileShare.Inheritable, as doing so when other tests concurrently spawn processes
            // results in those file handles being inherited, which then keeps the file open longer
            // than expected by the test, resulting in subsequent sharing violations.
        };

        [Fact]
        public void FileShareOpen()
        {
            // create the file
            string fileName = GetTestFilePath();
            CreateFileStream(fileName, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.ReadWrite | FileShare.Delete).Dispose();

            // just check that the inputs are accepted, actual sharing varies by platform so we separate the behavior testing
            foreach (FileAccess access in new[] { FileAccess.ReadWrite, FileAccess.Write, FileAccess.Read })
            {
                foreach (FileShare share in s_shares)
                {
                    try
                    {
                        CreateFileStream(fileName, FileMode.Open, access, share).Dispose();
                    }
                    catch (Exception e)
                    {
                        throw new Exception($"Failed with FileAccess {access} and FileShare {share}", e);
                    }
                }
            }
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void FileShareOpen_Inheritable()
        {
            RemoteExecutor.Invoke(() =>
            {
                int i = 0;
                foreach (FileAccess access in new[] { FileAccess.ReadWrite, FileAccess.Write, FileAccess.Read })
                {
                    foreach (FileShare share in s_shares)
                    {
                        string fileName = GetTestFilePath(i++);
                        CreateFileStream(fileName, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.ReadWrite | FileShare.Delete).Dispose();
                        CreateFileStream(fileName, FileMode.Open, access, share | FileShare.Inheritable).Dispose();
                    }
                }
            }).Dispose();
        }

        [Fact]
        public void FileShareCreate()
        {
            // just check that the inputs are accepted, actual sharing varies by platform so we separate the behavior testing
            int i = 0;
            foreach (FileAccess access in new[] { FileAccess.ReadWrite, FileAccess.Write })
            {
                foreach (FileShare share in s_shares)
                {
                    try
                    {
                        CreateFileStream(GetTestFilePath(i++), FileMode.Create, access, share).Dispose();
                    }
                    catch (Exception e)
                    {
                        throw new Exception($"Failed with FileAccess {access} and FileShare {share}", e);
                    }
                }
            }
        }

        [Fact]
        public void FileShareOpenOrCreate()
        {
            // just check that the inputs are accepted, actual sharing varies by platform so we separate the behavior testing
            int i = 0;
            foreach (FileAccess access in new[] { FileAccess.ReadWrite, FileAccess.Write, FileAccess.Read })
            {
                foreach (FileShare share in s_shares)
                {
                    try
                    {
                        CreateFileStream(GetTestFilePath(i++), FileMode.OpenOrCreate, access, share).Dispose();
                    }
                    catch (Exception e)
                    {
                        throw new Exception($"Failed with FileAccess {access} and FileShare {share}", e);
                    }
                }
            }
        }

        [Theory]
        [InlineData(FileMode.Create)]
        [InlineData(FileMode.Truncate)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/40065", TestPlatforms.Browser)]
        public void NoTruncateOnFileShareViolation(FileMode fileMode)
        {
            string fileName = GetTestFilePath();

            using (FileStream fs = CreateFileStream(fileName, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
            {
                fs.Write(new byte[] { 42 }, 0, 1);
                fs.Flush();
                FSAssert.ThrowsSharingViolation(() => CreateFileStream(fileName, fileMode, FileAccess.Write, FileShare.None).Dispose());
            }
            using (FileStream reader = CreateFileStream(fileName, FileMode.Open, FileAccess.Read))
            {
                byte[] buf = new byte[1];
                Assert.Equal(1, reader.Read(buf, 0, 1));
                Assert.Equal(42, buf[0]);
            }
        }
    }
}
