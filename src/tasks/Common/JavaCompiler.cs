// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Microsoft.Build.Utilities;

internal sealed class JavaCompiler
{
    private readonly string _javaCompilerArgs;
    private readonly string _workingDir;
    private readonly TaskLoggingHelper _logger;

    public JavaCompiler(
        TaskLoggingHelper logger,
        AndroidSdkHelper androidSdk,
        string workingDir,
        string javaVersion = "1.8")
    {
        _javaCompilerArgs = $"-classpath src -bootclasspath \"{androidSdk.AndroidJarPath}\" -source {javaVersion} -target {javaVersion}";
        _workingDir = workingDir;
        _logger = logger;
    }

    public void Compile(string javaSourceFile, string outputDir)
    {
        Utils.RunProcess(_logger, "javac", $"{_javaCompilerArgs} -d {outputDir} {javaSourceFile}", workingDir: _workingDir);
    }
}
