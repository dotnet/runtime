// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using Xunit;

namespace System.IO.FileSystem.Tests
{
    public class DirectoryInfo_GetFileSystemInfos_str : Directory_GetFileSystemEntries_str_str
    {
        #region Utilities

        public override string[] GetEntries(string path)
        {
            return ((new DirectoryInfo(path).GetFileSystemInfos("*").Select(x => x.FullName)).ToArray());
        }

        public override string[] GetEntries(string path, string searchPattern)
        {
            return ((new DirectoryInfo(path).GetFileSystemInfos(searchPattern).Select(x => x.FullName)).ToArray());
        }

        #endregion
    }
}
