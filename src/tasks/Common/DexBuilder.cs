// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using Microsoft.Build.Utilities;

internal sealed class DexBuilder
{
    private readonly string _workingDir;
    private readonly AndroidSdkHelper _androidSdk;
    private readonly TaskLoggingHelper _logger;

    public DexBuilder(
        TaskLoggingHelper logger,
        AndroidSdkHelper buildTools,
        string workingDir)
    {
        _androidSdk = buildTools;
        _workingDir = workingDir;
        _logger = logger;
    }

    public void Build(string inputDir, string outputFileName)
    {
        if (_androidSdk.HasD8)
        {
            BuildUsingD8(inputDir, outputFileName);
        }
        else
        {
            BuildUsingDx(inputDir, outputFileName);
        }
    }

    private void BuildUsingD8(string inputDir, string outputFilePath)
    {
        string[] classFiles = Directory.GetFiles(inputDir, "*.class", SearchOption.AllDirectories);

        if (classFiles.Length == 0)
            throw new InvalidOperationException("Didn't find any .class files");

        Utils.RunProcess(_logger, _androidSdk.D8Path, $"--no-desugaring {string.Join(" ", classFiles)}", workingDir: _workingDir);

        File.Move(
            sourceFileName: Path.Combine(_workingDir, "classes.dex"),
            destFileName: outputFilePath,
            overwrite: true);
    }

    private void BuildUsingDx(string inputDir, string outputFileName)
    {
        Utils.RunProcess(_logger, _androidSdk.DxPath, $"--dex --output={outputFileName} {inputDir}", workingDir: _workingDir);
    }
}
