// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;

namespace SuperPmi;

public static class ExceptionCodes
{
    // DebugBreakorAV is the base of a range; each call site adds a unique number.
    public const uint DebugBreakorAV = 0xe0421000;
    public const uint DebugBreakorAV_MAX = 0xe0422000;

    public const uint MC = 0xe0422000;
    public const uint LWM = 0xe0423000;
    public const uint CALLUTILS = 0xe0426000;
    public const uint TYPEUTILS = 0xe0427000;

    // Rethrows an exception that the EE threw during recording. Not an SPMI exception, and
    // not a replay failure.
    public const uint RECORDED_EXCEPTION = 0xe0428000;
    public const uint ASSERT = 0xe0440000;

    public static bool IsSuperPMIException(uint code) => code switch
    {
        MC or LWM or CALLUTILS or TYPEUTILS or ASSERT => true,
        _ => code >= DebugBreakorAV && code < DebugBreakorAV_MAX,
    };
}

public sealed class SpmiException : Exception
{
    public SpmiException(uint code, string message)
        : base(message)
    {
        Code = code;
    }

    public uint Code { get; }
}

public static class ErrorHandling
{
    [DoesNotReturn]
    public static void LogException(uint code, string message)
    {
        Logger.LogExceptionMessage(code, message);
        throw new SpmiException(code, message);
    }

    public static void AssertCodeMsg([DoesNotReturnIf(false)] bool condition, uint code, string expr, string message)
    {
        if (!condition)
            LogException(code, $"SuperPMI assertion '{expr}' failed ({message})");
    }

    public static void AssertCode([DoesNotReturnIf(false)] bool condition, uint code, string expr)
    {
        if (!condition)
            LogException(code, $"SuperPMI assertion '{expr}' failed");
    }

    public static void AssertMsg([DoesNotReturnIf(false)] bool condition, string expr, string message)
        => AssertCodeMsg(condition, ExceptionCodes.ASSERT, expr, message);

    // The error code of a failed file operation, as GetLastError() (or errno, for the common errors) reports it.
    public static uint LastError(Exception e) => (uint)e.HResult & 0xFFFF;
}
