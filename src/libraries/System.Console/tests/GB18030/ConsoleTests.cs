// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Text;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace GB18030.Tests;

public class ConsoleTests
{
    protected static readonly int WaitInMS = 30 * 1000 * PlatformDetection.SlowRuntimeTimeoutModifier;

    public static bool IsSupported { get; } =
        RemoteExecutor.IsSupported &&
        TestHelper.IsGB18030Supported &&
        (!PlatformDetection.IsWindows || IsConsoleInputEncodingSupported());

    private static bool IsConsoleInputEncodingSupported()
    {
        Encoding? originalEncoding = null;

        try
        {
            originalEncoding = Console.InputEncoding;
            Console.InputEncoding = TestHelper.GB18030Encoding;
            return Console.InputEncoding.CodePage == TestHelper.GB18030Encoding.CodePage;
        }
        catch (IOException)
        {
            return false;
        }
        finally
        {
            if (originalEncoding is not null)
            {
                Console.InputEncoding = originalEncoding;
            }
        }
    }

    [ConditionalTheory(typeof(ConsoleTests), nameof(IsSupported))]
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

    [ConditionalTheory(typeof(ConsoleTests), nameof(IsSupported))]
    [MemberData(nameof(TestHelper.DecodedMemberData), MemberType = typeof(TestHelper))]
    public void StandardInput(string decodedText)
    {
        var remoteOptions = new RemoteInvokeOptions();
        remoteOptions.StartInfo.RedirectStandardInput = true;
        remoteOptions.StartInfo.StandardInputEncoding = TestHelper.GB18030Encoding;

        using RemoteInvokeHandle remoteHandle = RemoteExecutor.Invoke(line =>
        {
            Console.InputEncoding = TestHelper.GB18030Encoding;
            Assert.Equal(line, Console.In.ReadToEnd());

            return 42;
        }, decodedText, remoteOptions);

        remoteHandle.Process.StandardInput.Write(decodedText);
        remoteHandle.Process.StandardInput.Close();

        Assert.True(remoteHandle.Process.WaitForExit(WaitInMS));
    }

    [ConditionalTheory(typeof(ConsoleTests), nameof(IsSupported))]
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
