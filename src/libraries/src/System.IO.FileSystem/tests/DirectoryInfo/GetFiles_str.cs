// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.IO.FileSystem.Tests;
using System.Linq;
using System.Runtime.CompilerServices;
using Xunit;

public class DirectoryInfo_GetFiles_str : Directory_GetFiles_str_str
{
    #region Utilities

    public override String[] GetFiles(String path)
    {
        return ((new DirectoryInfo(path).GetFiles("*").Select(x => x.FullName)).ToArray());
    }

    public override String[] GetFiles(String path, String searchPattern)
    {
        return ((new DirectoryInfo(path).GetFiles(searchPattern).Select(x => x.FullName)).ToArray());
    }

    #endregion
}