// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Xunit;

namespace System.IO.Tests
{
    #region EnumerateFiles

    public class DirectoryInfo_EnumerateFiles_Str : Directory_GetFiles_str
    {
        public override string[] GetEntries(string path)
        {
            return ((new DirectoryInfo(path).EnumerateFiles().Select(x => x.FullName)).ToArray());
        }
    }

    public class DirectoryInfo_EnumerateFiles_str_str : Directory_GetFiles_str_str
    {
        public override string[] GetEntries(string path)
        {
            return ((new DirectoryInfo(path).EnumerateFiles("*").Select(x => x.FullName)).ToArray());
        }

        public override string[] GetEntries(string path, string searchPattern)
        {
            return ((new DirectoryInfo(path).EnumerateFiles(searchPattern).Select(x => x.FullName)).ToArray());
        }
    }

    public class DirectoryInfo_EnumerateFiles_str_str_so : Directory_GetFiles_str_str_so
    {
        public override bool IsDirectoryInfo => true;

        public override string[] GetEntries(string path)
        {
            return ((new DirectoryInfo(path).EnumerateFiles("*", SearchOption.TopDirectoryOnly).Select(x => x.FullName)).ToArray());
        }

        public override string[] GetEntries(string path, string searchPattern)
        {
            return ((new DirectoryInfo(path).EnumerateFiles(searchPattern, SearchOption.TopDirectoryOnly).Select(x => x.FullName)).ToArray());
        }

        public override string[] GetEntries(string path, string searchPattern, SearchOption option)
        {
            return ((new DirectoryInfo(path).EnumerateFiles(searchPattern, option).Select(x => x.FullName)).ToArray());
        }
    }

    #endregion

    #region EnumerateFileSystemInfos

    public class DirectoryInfo_EnumFSI_str : Directory_GetFileSystemEntries_str
    {
        public override string[] GetEntries(string path)
        {
            return ((new DirectoryInfo(path).EnumerateFileSystemInfos().Select(x => x.FullName)).ToArray());
        }
    }

    public class DirectoryInfo_EnumFSI_str_str : Directory_GetFileSystemEntries_str_str
    {
        public override string[] GetEntries(string path)
        {
            return ((new DirectoryInfo(path).EnumerateFileSystemInfos("*").Select(x => x.FullName)).ToArray());
        }

        public override string[] GetEntries(string path, string searchPattern)
        {
            return ((new DirectoryInfo(path).EnumerateFileSystemInfos(searchPattern).Select(x => x.FullName)).ToArray());
        }

    }

    public class DirectoryInfo_EnumFSI_str_str_so : Directory_GetFileSystemEntries_str_str_so
    {
        public override string[] GetEntries(string path)
        {
            return ((new DirectoryInfo(path).EnumerateFileSystemInfos("*", SearchOption.TopDirectoryOnly).Select(x => x.FullName)).ToArray());
        }

        public override string[] GetEntries(string path, string searchPattern)
        {
            return ((new DirectoryInfo(path).EnumerateFileSystemInfos(searchPattern, SearchOption.TopDirectoryOnly).Select(x => x.FullName)).ToArray());
        }

        public override string[] GetEntries(string path, string searchPattern, SearchOption option)
        {
            return ((new DirectoryInfo(path).EnumerateFileSystemInfos(searchPattern, option).Select(x => x.FullName)).ToArray());
        }
    }

    #endregion

    #region EnumerateDirectories

    public class DirectoryInfo_EnumDir_Str : Directory_GetDirectories_str
    {
        public override string[] GetEntries(string path)
        {
            return ((new DirectoryInfo(path).EnumerateDirectories().Select(x => x.FullName)).ToArray());
        }
    }

    public class DirectoryInfo_EnumDir_str_str : Directory_GetDirectories_str_str
    {
        public override string[] GetEntries(string path)
        {
            return ((new DirectoryInfo(path).EnumerateDirectories("*").Select(x => x.FullName)).ToArray());
        }

        public override string[] GetEntries(string path, string searchPattern)
        {
            return ((new DirectoryInfo(path).EnumerateDirectories(searchPattern).Select(x => x.FullName)).ToArray());
        }
    }

    public class DirectoryInfo_EnumDir_str_str_so : Directory_GetDirectories_str_str_so
    {
        public override string[] GetEntries(string path)
        {
            return ((new DirectoryInfo(path).EnumerateDirectories("*", SearchOption.TopDirectoryOnly).Select(x => x.FullName)).ToArray());
        }

        public override string[] GetEntries(string path, string searchPattern)
        {
            return ((new DirectoryInfo(path).EnumerateDirectories(searchPattern, SearchOption.TopDirectoryOnly).Select(x => x.FullName)).ToArray());
        }

        public override string[] GetEntries(string path, string searchPattern, SearchOption option)
        {
            return ((new DirectoryInfo(path).EnumerateDirectories(searchPattern, option).Select(x => x.FullName)).ToArray());
        }
    }

    #endregion
}
