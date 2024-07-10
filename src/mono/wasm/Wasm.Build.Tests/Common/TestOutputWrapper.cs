// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;
using Xunit.Abstractions;

#nullable enable

namespace Wasm.Build.Tests;

public class TestOutputWrapper(ITestOutputHelper baseOutput) : ITestOutputHelper
{
    private readonly StringBuilder _outputBuffer = new StringBuilder();

    public void WriteLine(string message)
    {
        baseOutput.WriteLine(message);
        _outputBuffer.AppendLine(message);
        if (EnvironmentVariables.ShowBuildOutput)
            Console.WriteLine(message);
    }

    public void WriteLine(string format, params object[] args)
    {
        baseOutput.WriteLine(format, args);
        _outputBuffer.AppendFormat(format, args).AppendLine();
        if (EnvironmentVariables.ShowBuildOutput)
            Console.WriteLine(format, args);
    }

    public override string ToString() => _outputBuffer.ToString();
}
