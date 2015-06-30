// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using Xunit;

public class DirectoryInfo_GetFiles : Directory_GetFiles_str
{
    public override String[] GetFiles(String path)
    {
        return ((new DirectoryInfo(path).GetFiles().Select(x => x.FullName)).ToArray());
    }
}

