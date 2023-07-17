// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit.Abstractions;

#nullable enable

namespace Wasm.Build.Tests;

public class TestOutputWrapper(ITestOutputHelper baseOutput) : ITestOutputHelper
{
    public void WriteLine(string message)
    {
        baseOutput.WriteLine(message);
        if (EnvironmentVariables.ShowBuildOutput)
            Console.WriteLine(message);
    }

    public void WriteLine(string format, params object[] args)
    {
        baseOutput.WriteLine(format, args);
        if (EnvironmentVariables.ShowBuildOutput)
            Console.WriteLine(format, args);
    }
}
