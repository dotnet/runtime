// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.JavaScript;
using System.Text;

namespace System;

public static partial class Console
{
    [FeatureSwitchDefinition("System.Console.BrowserHost")]
    internal static bool BrowserHostHasJavaScriptOutput { get; } =
        !RuntimeFeature.IsMultithreadingSupported &&
        AppContext.TryGetSwitch("System.Console.BrowserHost", out bool isBrowserHost) &&
        isBrowserHost;

    private static Stream CreateStandardOutputStream(bool isError) =>
        BrowserHostHasJavaScriptOutput
            ? new BrowserDirectConsoleStream(isError)
            : isError ? ConsolePal.OpenStandardError() : ConsolePal.OpenStandardOutput();

    private static void ClearConsole()
    {
        if (BrowserHostHasJavaScriptOutput)
        {
            BrowserConsoleInterop.Clear();
        }
        else
        {
            ConsolePal.Clear();
        }
    }

    static partial void ValidateOutputEncoding(Encoding encoding)
    {
        if (BrowserHostHasJavaScriptOutput && encoding.CodePage != Encoding.UTF8.CodePage)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_ConsoleOutputEncoding);
        }
    }
}

internal sealed class BrowserDirectConsoleStream : ConsoleStream
{
    private readonly bool _isError;

    internal BrowserDirectConsoleStream(bool isError) : base(FileAccess.Write) => _isError = isError;

    public override int Read(Span<byte> buffer) => throw Error.GetReadNotSupported();

    public override void Write(byte[] buffer, int offset, int count)
    {
        ValidateWrite(buffer, offset, count);
        if (count != 0)
        {
            BrowserConsoleInterop.WriteBytes(buffer.AsSpan(offset, count), _isError);
        }
    }

    public override void Write(ReadOnlySpan<byte> buffer)
    {
        if (!buffer.IsEmpty)
        {
            ObjectDisposedException.ThrowIf(!CanWrite, this);
            BrowserConsoleInterop.WriteBytes(buffer, _isError);
        }
    }

    public override void Flush()
    {
        if (!CanWrite)
        {
            throw Error.GetFileNotOpen();
        }
    }
}

internal static partial class BrowserConsoleInterop
{
    [JSImport("INTERNAL.consoleClear")]
    internal static partial void Clear();

    [LibraryImport(Interop.Libraries.SystemBrowserNative, EntryPoint = "SystemJS_ConsoleWrite")]
    private static unsafe partial nint WriteBytesCore(byte* value, int length, int isError);

    internal static unsafe void WriteBytes(ReadOnlySpan<byte> value, bool isError)
    {
        nint error;
        fixed (byte* address = value)
        {
            error = WriteBytesCore(address, value.Length, isError ? 1 : 0);
        }
        if (error != 0)
        {
            ThrowWriteError(error);
        }
    }

    private static void ThrowWriteError(nint error)
    {
        try
        {
            throw new IOException(SR.IO_ConsoleWriteFailed, new JSException(Marshal.PtrToStringUTF8(error)!));
        }
        finally
        {
            Marshal.FreeHGlobal(error);
        }
    }
}
