// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using Xunit;

namespace System.IO.FileSystem.Tests
{
    #region EnumerateFiles

    public class Directory_EnumFiles_Str : Directory_GetFiles_str
    {
        public override string[] GetEntries(string path)
        {
            return Directory.EnumerateFiles(path).ToArray();
        }
    }

    public class Directory_EnumFiles_str_str : Directory_GetFiles_str_str
    {
        public override string[] GetEntries(string path)
        {
            return Directory.EnumerateFiles(path, "*").ToArray();
        }

        public override string[] GetEntries(string path, string searchPattern)
        {
            return Directory.EnumerateFiles(path, searchPattern).ToArray();
        }
    }

    public class Directory_EnumFiles_str_str_so : Directory_GetFiles_str_str_so
    {
        public override string[] GetEntries(string path)
        {
            return Directory.EnumerateFiles(path, "*", SearchOption.TopDirectoryOnly).ToArray();
        }

        public override string[] GetEntries(string path, string searchPattern)
        {
            return Directory.EnumerateFiles(path, searchPattern, SearchOption.TopDirectoryOnly).ToArray();
        }

        public override string[] GetEntries(string path, string searchPattern, SearchOption option)
        {
            return Directory.EnumerateFiles(path, searchPattern, option).ToArray();
        }
    }
    #endregion

    #region EnumerateFileSystemEntries

    public class Directory_EnumFSE_str : Directory_GetFileSystemEntries_str
    {
        public override string[] GetEntries(string dirName)
        {
            return Directory.EnumerateFileSystemEntries(dirName).ToArray();
        }
    }

    public class Directory_EnumFSE_str_str : Directory_GetFileSystemEntries_str_str
    {
        public override string[] GetEntries(string dirName)
        {
            return Directory.EnumerateFileSystemEntries(dirName, "*").ToArray();
        }

        public override string[] GetEntries(string dirName, string searchPattern)
        {
            return Directory.EnumerateFileSystemEntries(dirName, searchPattern).ToArray();
        }
    }

    public class Directory_EnumFSE_str_str_so : Directory_GetFileSystemEntries_str_str_so
    {
        public override string[] GetEntries(string dirName)
        {
            return Directory.EnumerateFileSystemEntries(dirName, "*", SearchOption.TopDirectoryOnly).ToArray();
        }

        public override string[] GetEntries(string dirName, string searchPattern)
        {
            return Directory.EnumerateFileSystemEntries(dirName, searchPattern, SearchOption.TopDirectoryOnly).ToArray();
        }

        public override string[] GetEntries(string dirName, string searchPattern, SearchOption option)
        {
            return Directory.EnumerateFileSystemEntries(dirName, searchPattern, option).ToArray();
        }
    }

    #endregion

    #region EnumerateDirectories

    public class Directory_EnumDir_str : Directory_GetDirectories_str
    {
        public override string[] GetEntries(string dirName)
        {
            return Directory.EnumerateDirectories(dirName).ToArray();
        }
    }

    public class Directory_EnumDir_str_str : Directory_GetDirectories_str_str
    {
        public override string[] GetEntries(string dirName)
        {
            return Directory.EnumerateDirectories(dirName, "*").ToArray();
        }

        public override string[] GetEntries(string dirName, string searchPattern)
        {
            return Directory.EnumerateDirectories(dirName, searchPattern).ToArray();
        }
    }

    public class Directory_EnumDir_str_str_so : Directory_GetDirectories_str_str_so
    {
        public override string[] GetEntries(string dirName)
        {
            return Directory.EnumerateDirectories(dirName, "*", SearchOption.TopDirectoryOnly).ToArray();
        }

        public override string[] GetEntries(string dirName, string searchPattern)
        {
            return Directory.EnumerateDirectories(dirName, searchPattern, SearchOption.TopDirectoryOnly).ToArray();
        }

        public override string[] GetEntries(string dirName, string searchPattern, SearchOption option)
        {
            return Directory.EnumerateDirectories(dirName, searchPattern, option).ToArray();
        }
    }

    #endregion
}
