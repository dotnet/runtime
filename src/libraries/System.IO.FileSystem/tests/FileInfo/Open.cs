// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.IO.Tests
{
    public class FileInfo_Open_fm : FileStream_ctor_str_fm
    {
        protected override FileStream CreateFileStream(string path, FileMode mode)
        {
            return new FileInfo(path).Open(mode);
        }

        public override void FileModeAppend(string streamSpecifier)
        {
            using (FileStream fs = CreateFileStream(GetTestFilePath() + streamSpecifier, FileMode.Append))
            {
                Assert.False(fs.CanRead);
                Assert.True(fs.CanWrite);
            }
        }

        public override void FileModeAppendExisting(string streamSpecifier)
        {
            string fileName = GetTestFilePath() + streamSpecifier;
            using (FileStream fs = CreateFileStream(fileName, FileMode.Create))
            {
                fs.WriteByte(0);
            }

            using (FileStream fs = CreateFileStream(fileName, FileMode.Append))
            {
                // Ensure that the file was re-opened and position set to end
                Assert.Equal(1L, fs.Length);
                Assert.Equal(1L, fs.Position);
                Assert.False(fs.CanRead);
                Assert.True(fs.CanSeek);
                Assert.True(fs.CanWrite);
                Assert.Throws<IOException>(() => fs.Seek(-1, SeekOrigin.Current));
                Assert.Throws<NotSupportedException>(() => fs.ReadByte());
            }
        }
    }

    public class FileInfo_Open_fm_fa : FileStream_ctor_str_fm_fa
    {
        protected override FileStream CreateFileStream(string path, FileMode mode)
        {
            return new FileInfo(path).Open(mode, mode == FileMode.Append ? FileAccess.Write : FileAccess.ReadWrite);
        }

        protected override FileStream CreateFileStream(string path, FileMode mode, FileAccess access)
        {
            return new FileInfo(path).Open(mode, access);
        }
    }

    public class FileInfo_Open_fm_fa_fs : FileStream_ctor_str_fm_fa_fs
    {
        protected override FileStream CreateFileStream(string path, FileMode mode)
        {
            return new FileInfo(path).Open(mode, mode == FileMode.Append ? FileAccess.Write : FileAccess.ReadWrite, FileShare.ReadWrite | FileShare.Delete);
        }

        protected override FileStream CreateFileStream(string path, FileMode mode, FileAccess access)
        {
            return new FileInfo(path).Open(mode, access, FileShare.ReadWrite | FileShare.Delete);
        }

        protected override FileStream CreateFileStream(string path, FileMode mode, FileAccess access, FileShare share)
        {
            return new FileInfo(path).Open(mode, access, share);
        }
    }

    public class FileInfo_Open_options_as : FileStream_ctor_options_as
    {
        protected override FileStream CreateFileStream(string path, FileMode mode)
        {
            return new FileInfo(path).Open(
                new FileStreamOptions {
                    Mode = mode,
                    Access = mode == FileMode.Append ? FileAccess.Write : FileAccess.ReadWrite,
                    PreallocationSize = PreallocationSize
                });
        }

        protected override FileStream CreateFileStream(string path, FileMode mode, FileAccess access)
        {
            return new FileInfo(path).Open(
                new FileStreamOptions {
                    Mode = mode,
                    Access = access,
                    PreallocationSize = PreallocationSize
                });
        }

        protected override FileStream CreateFileStream(string path, FileMode mode, FileAccess access, FileShare share, int bufferSize, FileOptions options)
        {
            return new FileInfo(path).Open(
                new FileStreamOptions {
                    Mode = mode,
                    Access = access,
                    Share = share,
                    Options = options,
                    BufferSize = bufferSize,
                    PreallocationSize = PreallocationSize
                });
        }
    }

    public class FileInfo_OpenSpecial : FileStream_ctor_str_fm_fa_fs
    {
        protected override FileStream CreateFileStream(string path, FileMode mode, FileAccess access)
        {
            if (mode == FileMode.Open && access == FileAccess.Read)
                return new FileInfo(path).OpenRead();
            else if (mode == FileMode.OpenOrCreate && access == FileAccess.Write)
                return new FileInfo(path).OpenWrite();
            else
                return new FileInfo(path).Open(mode, access);
        }

        protected override FileStream CreateFileStream(string path, FileMode mode, FileAccess access, FileShare share)
        {
            if (mode == FileMode.Open && access == FileAccess.Read && share == FileShare.Read)
                return new FileInfo(path).OpenRead();
            else if (mode == FileMode.OpenOrCreate && access == FileAccess.Write && share == FileShare.None)
                return new FileInfo(path).OpenWrite();
            else
                return new FileInfo(path).Open(mode, access, share);
        }
    }

    public class FileInfo_CreateText : File_ReadWriteAllText
    {
        protected override void Write(string path, string content)
        {
            var writer = new FileInfo(path).CreateText();
            writer.Write(content);
            writer.Dispose();
        }
    }

    public class FileInfo_OpenText : File_ReadWriteAllText
    {
        protected override string Read(string path)
        {
            using (var reader = new FileInfo(path).OpenText())
                return reader.ReadToEnd();
        }
    }
}
