// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using Xunit;

namespace System.IO.FileSystem.Tests
{
    public class DirectoryInfo_GetFiles : Directory_GetFiles_str
    {
        #region Utilities

        public override string[] GetEntries(string path)
        {
            return ((new DirectoryInfo(path).GetFiles().Select(x => x.FullName)).ToArray());
        }

        #endregion
    }
}
