// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;

namespace Microsoft.DotNet.Diagnostics.DataContract.BuildTool;

public class DirectoryBaselines
{
    private const string BaselineGlob = "*.json*"; // .json and .jsonc

    private readonly string _baselinesDir;

    public DirectoryBaselines(string baselinesDir)
    {
        _baselinesDir = baselinesDir;
    }

    public string[] BaselineNames => GetBaselineNames(_baselinesDir);

    private static string[] GetBaselineNames(string baselineDir)
    {

        var baselineNames = new List<string>();
        foreach (var file in Directory.EnumerateFiles(baselineDir, BaselineGlob))
        {
            var name = Path.GetFileNameWithoutExtension(file);
            baselineNames.Add(name);
        }
        return baselineNames.ToArray();
    }
}
