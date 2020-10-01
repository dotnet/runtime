// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using Xunit;
using Xunit.Abstractions;

public partial class ReadKey
{
    private readonly ITestOutputHelper _output;

    public ReadKey(ITestOutputHelper output)
    {
        _output = output;
    }

    private bool IsHandleRedirected(IntPtr handle, string desc)
    {
        // If handle is not to a character device, we must be redirected:
        uint fileType = Interop.Kernel32.GetFileType(handle);
        _output.WriteLine($"fileTypeOutput '{desc}': {fileType}");


        if ((fileType & Interop.Kernel32.FileTypes.FILE_TYPE_CHAR) != Interop.Kernel32.FileTypes.FILE_TYPE_CHAR)
            return true;
        
        // We are on a char device if GetConsoleMode succeeds and so we are not redirected.
        return (!Interop.Kernel32.IsGetConsoleModeCallSuccessful(handle));
    }

    [Fact]
    public void LogKeyAvailable()
    {
       if (PlatformDetection.IsWindows)
        {
            IntPtr inputHandle = Interop.Kernel32.GetStdHandle(Interop.Kernel32.HandleTypes.STD_INPUT_HANDLE);
            _output.WriteLine("IsInputHandleRedirected: " + IsHandleRedirected(inputHandle, "input"));
            IntPtr OutputHandle = Interop.Kernel32.GetStdHandle(Interop.Kernel32.HandleTypes.STD_OUTPUT_HANDLE);
            _output.WriteLine("IsOutputHandleRedirected: " + IsHandleRedirected(OutputHandle, "output"));
        }
    }
}
