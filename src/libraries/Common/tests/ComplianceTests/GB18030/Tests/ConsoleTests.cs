// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace GB18030.Tests;

public class ConsoleTests
{
    protected static readonly int WaitInMS = 30 * 1000 * PlatformDetection.SlowRuntimeTimeoutModifier;

    [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
    [MemberData(nameof(TestHelper.DecodedMemberData), MemberType = typeof(TestHelper))]
    public void StandardOutput(string decodedText)
    {
        var remoteOptions = new RemoteInvokeOptions();
        remoteOptions.StartInfo.RedirectStandardOutput = true;
        remoteOptions.StartInfo.StandardOutputEncoding = TestHelper.GB18030Encoding;

        using RemoteInvokeHandle remoteHandle = RemoteExecutor.Invoke(line =>
        {
            Console.OutputEncoding = TestHelper.GB18030Encoding;
            Console.Write(line);

            return 42;
        }, decodedText, remoteOptions);


        Assert.Equal(decodedText, remoteHandle.Process.StandardOutput.ReadToEnd());
        Assert.True(remoteHandle.Process.WaitForExit(WaitInMS));
    }

    [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
    [MemberData(nameof(TestHelper.DecodedMemberData), MemberType = typeof(TestHelper))]
    public void StandardInput(string decodedText)
    {
        var remoteOptions = new RemoteInvokeOptions();
        remoteOptions.StartInfo.RedirectStandardInput = true;
#if !NETFRAMEWORK
        remoteOptions.StartInfo.StandardInputEncoding = TestHelper.GB18030Encoding;
#endif

        using RemoteInvokeHandle remoteHandle = RemoteExecutor.Invoke(line =>
        {
            Console.InputEncoding = TestHelper.GB18030Encoding;
            Assert.Equal(line, Console.In.ReadToEnd());

            return 42;
        }, decodedText, remoteOptions);

        if (PlatformDetection.IsNetFramework)
        {
            // there's no StandardInputEncoding in .NET Framework, re-encode and write.
            byte[] encoded = TestHelper.GB18030Encoding.GetBytes(decodedText);
            remoteHandle.Process.StandardInput.BaseStream.Write(encoded, 0, encoded.Length);
            remoteHandle.Process.StandardInput.Close();
        }
        else
        {
            remoteHandle.Process.StandardInput.Write(decodedText);
            remoteHandle.Process.StandardInput.Close();
        }

        Assert.True(remoteHandle.Process.WaitForExit(WaitInMS));
    }

    [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
    [MemberData(nameof(TestHelper.DecodedMemberData), MemberType = typeof(TestHelper))]
    public void StandardError(string decodedText)
    {
        var remoteOptions = new RemoteInvokeOptions();
        remoteOptions.StartInfo.RedirectStandardError = true;
        remoteOptions.StartInfo.StandardErrorEncoding = TestHelper.GB18030Encoding;

        using RemoteInvokeHandle remoteHandle = RemoteExecutor.Invoke(line =>
        {
            Console.OutputEncoding = TestHelper.GB18030Encoding;
            Console.Error.Write(line);

            return 42;
        }, decodedText, remoteOptions);


        Assert.Equal(decodedText, remoteHandle.Process.StandardError.ReadToEnd());
        Assert.True(remoteHandle.Process.WaitForExit(WaitInMS));
    }
}
