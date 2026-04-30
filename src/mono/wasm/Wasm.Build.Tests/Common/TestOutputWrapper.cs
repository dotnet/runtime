// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;
using Xunit;
#nullable enable

namespace Wasm.Build.Tests;

public class TestOutputWrapper(ITestOutputHelper baseOutput) : ITestOutputHelper
{
    private readonly StringBuilder _outputBuffer = new StringBuilder();

    public string Output => _outputBuffer.ToString();

    public void Write(string message)
    {
        baseOutput.Write(message);
        _outputBuffer.Append(message);
        if (EnvironmentVariables.ShowBuildOutput)
            Console.Write(message);
    }

    public void Write(string format, params object[] args)
    {
        baseOutput.Write(format, args);
        _outputBuffer.AppendFormat(format, args);
        if (EnvironmentVariables.ShowBuildOutput)
            Console.Write(format, args);
    }

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
