// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using Xunit.Abstractions;

namespace DebuggerTests;

public class TestEnvironment : IDisposable
{
    private bool _disposed;
    private readonly string _tempPath;
    private readonly ITestOutputHelper _testOutput;

    public TestEnvironment(ITestOutputHelper testOutput)
    {
        _testOutput = testOutput;
        _tempPath = Path.Combine(DebuggerTestBase.TempPath, Guid.NewGuid().ToString());
        if (Directory.Exists(_tempPath))
            Directory.Delete(_tempPath, recursive: true);

        Directory.CreateDirectory(_tempPath);
    }

    public void Dispose()
    {
        if (_disposed || EnvironmentVariables.SkipCleanup)
            return;

        Directory.Delete(_tempPath, recursive: true);
        _disposed = true;
    }

    public string CreateTempDirectory(string relativeDir, params string[] relativePathParts)
    {
        string newPath = Path.Combine(_tempPath, relativeDir, Path.Combine(relativePathParts));
        Directory.CreateDirectory(newPath);
        return newPath;
    }
}
