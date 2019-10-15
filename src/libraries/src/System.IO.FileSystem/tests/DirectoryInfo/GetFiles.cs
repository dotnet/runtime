// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Xunit;

namespace System.IO.Tests
{
    public class DirectoryInfo_GetFiles : Directory_GetFiles_str
    {
        public override string[] GetEntries(string path)
        {
            return ((new DirectoryInfo(path).GetFiles().Select(x => x.FullName)).ToArray());
        }
    }

    public class DirectoryInfo_GetFiles_str : Directory_GetFiles_str_str
    {
        public override string[] GetEntries(string path)
        {
            return ((new DirectoryInfo(path).GetFiles("*").Select(x => x.FullName)).ToArray());
        }

        public override string[] GetEntries(string path, string searchPattern)
        {
            return ((new DirectoryInfo(path).GetFiles(searchPattern).Select(x => x.FullName)).ToArray());
        }
    }

    public class DirectoryInfo_GetFiles_str_so : Directory_GetFiles_str_str_so
    {
        public override bool IsDirectoryInfo => true;

        public override string[] GetEntries(string path)
        {
            return ((new DirectoryInfo(path).GetFiles("*", SearchOption.TopDirectoryOnly).Select(x => x.FullName)).ToArray());
        }

        public override string[] GetEntries(string path, string searchPattern)
        {
            return ((new DirectoryInfo(path).GetFiles(searchPattern, SearchOption.TopDirectoryOnly).Select(x => x.FullName)).ToArray());
        }

        public override string[] GetEntries(string path, string searchPattern, SearchOption option)
        {
            return ((new DirectoryInfo(path).GetFiles(searchPattern, option).Select(x => x.FullName)).ToArray());
        }
    }
}
