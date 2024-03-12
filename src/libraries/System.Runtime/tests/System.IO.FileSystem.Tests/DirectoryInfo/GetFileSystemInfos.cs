// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Xunit;

namespace System.IO.Tests
{
    public class DirectoryInfo_GetFileSystemInfos : Directory_GetFileSystemEntries_str
    {
        public override string[] GetEntries(string path)
        {
            return ((new DirectoryInfo(path).GetFileSystemInfos().Select(x => x.FullName)).ToArray());
        }
    }

    public class DirectoryInfo_GetFileSystemInfos_str : Directory_GetFileSystemEntries_str_str
    {
        public override string[] GetEntries(string path)
        {
            return ((new DirectoryInfo(path).GetFileSystemInfos("*").Select(x => x.FullName)).ToArray());
        }

        public override string[] GetEntries(string path, string searchPattern)
        {
            return ((new DirectoryInfo(path).GetFileSystemInfos(searchPattern).Select(x => x.FullName)).ToArray());
        }
    }

    public class DirectoryInfo_GetFileSystemInfos_str_so : Directory_GetFileSystemEntries_str_str_so
    {
        public override string[] GetEntries(string path)
        {
            return ((new DirectoryInfo(path).GetFileSystemInfos("*", SearchOption.TopDirectoryOnly).Select(x => x.FullName)).ToArray());
        }

        public override string[] GetEntries(string path, string searchPattern)
        {
            return ((new DirectoryInfo(path).GetFileSystemInfos(searchPattern, SearchOption.TopDirectoryOnly).Select(x => x.FullName)).ToArray());
        }

        public override string[] GetEntries(string path, string searchPattern, SearchOption option)
        {
            return ((new DirectoryInfo(path).GetFileSystemInfos(searchPattern, option).Select(x => x.FullName)).ToArray());
        }
    }
}
