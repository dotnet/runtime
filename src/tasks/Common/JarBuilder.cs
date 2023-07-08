// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Build.Utilities;

internal sealed class JarBuilder
{
    private readonly TaskLoggingHelper _logger;

    public JarBuilder(TaskLoggingHelper logger)
    {
        _logger = logger;
    }

    public void Build(string inputDir, string outputFileName)
    {
        IEnumerable<string> classFiles =
            Directory.GetFiles(inputDir, "*.class", SearchOption.AllDirectories)
                .Select(classFile => Path.GetRelativePath(inputDir, classFile));

        Utils.RunProcess(_logger, "jar", $"-cf {outputFileName} {string.Join(" ", classFiles)}", workingDir: inputDir);
    }
}
