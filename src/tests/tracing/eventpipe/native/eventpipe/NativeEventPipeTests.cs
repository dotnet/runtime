// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Xunit;

public class NativeEventPipeTests
{
    [Theory]
    [MemberData(nameof(TestGroups))]
    public static void RunSuite(string group)
    {
        (int exitCode, string output, string error) = NativeTestProcess.Run("NativeEventPipeTestsDriver", group);
        Console.Write(output);
        Console.Error.Write(error);
        Assert.Equal(0, exitCode);
    }

    public static IEnumerable<object[]> TestGroups()
    {
        (int exitCode, string output, string error) = NativeTestProcess.Run("NativeEventPipeTestsDriver", "--list");
        Assert.True(exitCode == 0, output + error);
        string[] groups = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.NotEmpty(groups);

        foreach (string group in groups)
        {
            yield return new object[] { group };
        }
    }
}
