// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;

namespace System.IO.FileSystem.Tests
{
    public class DirectoryInfo_GetFiles_str : Directory_GetFiles_str_str
    {
        #region Utilities

        public override string[] GetEntries(string path)
        {
            return ((new DirectoryInfo(path).GetFiles("*").Select(x => x.FullName)).ToArray());
        }

        public override string[] GetEntries(string path, string searchPattern)
        {
            return ((new DirectoryInfo(path).GetFiles(searchPattern).Select(x => x.FullName)).ToArray());
        }

        #endregion
    }
}