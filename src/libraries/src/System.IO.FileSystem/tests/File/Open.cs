// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.IO.Tests
{
    public class File_Open_str_fm : FileStream_ctor_str_fm
    {
        protected override FileStream CreateFileStream(string path, FileMode mode)
        {
            return File.Open(path, mode);
        }

    }

    public class File_Open_str_fm_fa : FileStream_ctor_str_fm_fa
    {
        protected override FileStream CreateFileStream(string path, FileMode mode)
        {
            return File.Open(path, mode, mode == FileMode.Append ? FileAccess.Write : FileAccess.ReadWrite);
        }

        protected override FileStream CreateFileStream(string path, FileMode mode, FileAccess access)
        {
            return File.Open(path, mode, access);
        }
    }

    public class File_Open_str_fm_fa_fs : FileStream_ctor_str_fm_fa_fs
    {
        protected override FileStream CreateFileStream(string path, FileMode mode)
        {
            return File.Open(path, mode, mode == FileMode.Append ? FileAccess.Write : FileAccess.ReadWrite, FileShare.ReadWrite | FileShare.Delete);
        }

        protected override FileStream CreateFileStream(string path, FileMode mode, FileAccess access)
        {
            return File.Open(path, mode, access, FileShare.ReadWrite | FileShare.Delete);
        }

        protected override FileStream CreateFileStream(string path, FileMode mode, FileAccess access, FileShare share)
        {
            return File.Open(path, mode, access, share);
        }
    }

    public class File_OpenSpecial : FileStream_ctor_str_fm_fa_fs
    {
        protected override FileStream CreateFileStream(string path, FileMode mode, FileAccess access)
        {
            if (mode == FileMode.Open && access == FileAccess.Read)
                return File.OpenRead(path);
            else if (mode == FileMode.OpenOrCreate && access == FileAccess.Write)
                return File.OpenWrite(path);
            else
                return File.Open(path, mode, access);
        }

        protected override FileStream CreateFileStream(string path, FileMode mode, FileAccess access, FileShare share)
        {
            if (mode == FileMode.Open && access == FileAccess.Read && share == FileShare.Read)
                return File.OpenRead(path);
            else if (mode == FileMode.OpenOrCreate && access == FileAccess.Write && share == FileShare.None)
                return File.OpenWrite(path);
            else
                return File.Open(path, mode, access, share);
        }
    }

    public class File_CreateText : File_ReadWriteAllText
    {
        protected override void Write(string path, string content)
        {
            var writer = File.CreateText(path);
            writer.Write(content);
            writer.Dispose();
        }
    }

    public class File_OpenText : File_ReadWriteAllText
    {
        protected override string Read(string path)
        {
            using (var reader = File.OpenText(path))
                return reader.ReadToEnd();
        }
    }

}
