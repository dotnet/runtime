// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using Microsoft.Build.Utilities;

internal sealed class JarBuilder
{
    private readonly string _workingDir;
    private readonly TaskLoggingHelper _logger;

    public JarBuilder(TaskLoggingHelper logger, string workingDir)
    {
        _workingDir = workingDir;
        _logger = logger;
    }

    public void Build(string inputDir, string outputFileName)
    {
        string[] classFiles = Directory.GetFiles(Path.Combine(_workingDir, inputDir), "*.class", SearchOption.AllDirectories);
        Utils.RunProcess(_logger, "jar", $"-cf {outputFileName} {string.Join(" ", classFiles)}", workingDir: _workingDir);
    }
}
