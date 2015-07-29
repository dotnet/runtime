// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using Xunit;

namespace System.IO.FileSystem.Tests
{
    public class DirectoryInfo_GetDirectories : Directory_GetDirectories_str
    {
        public override string[] GetEntries(string path)
        {
            return ((new DirectoryInfo(path).GetDirectories().Select(x => x.FullName)).ToArray());
        }
    }

    public class DirectoryInfo_GetDirectories_str : Directory_GetDirectories_str_str
    {
        public override string[] GetEntries(string path)
        {
            return ((new DirectoryInfo(path).GetDirectories("*").Select(x => x.FullName)).ToArray());
        }

        public override string[] GetEntries(string path, string searchPattern)
        {
            return ((new DirectoryInfo(path).GetDirectories(searchPattern).Select(x => x.FullName)).ToArray());
        }
    }

    public class DirectoryInfo_GetDirectories_str_so : Directory_GetDirectories_str_str_so
    {
        public override string[] GetEntries(string path)
        {
            return ((new DirectoryInfo(path).GetDirectories("*", SearchOption.TopDirectoryOnly).Select(x => x.FullName)).ToArray());
        }

        public override string[] GetEntries(string path, string searchPattern)
        {
            return ((new DirectoryInfo(path).GetDirectories(searchPattern, SearchOption.TopDirectoryOnly).Select(x => x.FullName)).ToArray());
        }

        public override string[] GetEntries(string path, string searchPattern, SearchOption option)
        {
            return ((new DirectoryInfo(path).GetDirectories(searchPattern, option).Select(x => x.FullName)).ToArray());
        }
    }
}
