// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace System.IO.FileSystem.Tests
{
    public class FileInfo_Open_fm : FileStream_ctor_str_fm
    {
        protected override FileStream CreateFileStream(string path, FileMode mode)
        {
            return new FileInfo(path).Open(mode);
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
            return new FileInfo(path).Open(mode, mode == FileMode.Append ? FileAccess.Write : FileAccess.ReadWrite, FileShare.ReadWrite | FileShare.Delete | FileShare.Inheritable);
        }

        protected override FileStream CreateFileStream(string path, FileMode mode, FileAccess access)
        {
            return new FileInfo(path).Open(mode, access, FileShare.ReadWrite | FileShare.Delete | FileShare.Inheritable);
        }

        protected override FileStream CreateFileStream(string path, FileMode mode, FileAccess access, FileShare share)
        {
            return new FileInfo(path).Open(mode, access, share);
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
