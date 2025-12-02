// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Microsoft.Build.Utilities;
using Microsoft.Build.Framework;

#nullable enable

namespace WasmAppBuilder;

public sealed class LogAdapter
{
    public bool HasLoggedErrors
    {
        get => _helper?.HasLoggedErrors ?? _hasLoggedErrors;
    }

    private bool _hasLoggedErrors;
    private TaskLoggingHelper? _helper;
    private TextWriter? _output, _errorOutput;

    public LogAdapter(TaskLoggingHelper helper)
    {
        _helper = helper;
        _output = null;
        _errorOutput = null;
    }

    public LogAdapter()
    {
        _helper = null;
        _output = Console.Out;
        _errorOutput = Console.Error;
    }

    private static string AutoFormat(string s, object[] o)
    {
        if ((o?.Length ?? 0) > 0)
            return string.Format(s!, o!);
        else
            return s;
    }

    public void LogMessage(string s, params object[] o)
    {
        _helper?.LogMessage(s, o);
        _output?.WriteLine(AutoFormat(s, o));
    }

    public void LogMessage(MessageImportance mi, string s, params object[] o)
    {
        _helper?.LogMessage(mi, s, o);
        _output?.WriteLine(AutoFormat(s, o));
    }

    public void InfoHigh(string code, string message, params object[] args)
    {
        // We use MessageImportance.High to ensure this appears in build output, since
        //  warnaserror makes warnings hard to use
        _helper?.LogMessage(null, code, null, null, 0, 0, 0, 0, MessageImportance.High, message, args);
        _output?.WriteLine($"info : {code}: {AutoFormat(message, args)}");
    }

    public void Warning(string code, string message, params object[] args)
    {
        _helper?.LogWarning(null, code, null, null, 0, 0, 0, 0, message, args);
        _errorOutput?.WriteLine($"warning : {code}: {AutoFormat(message, args)}");
    }

    public void Error(string message)
    {
        _helper?.LogError(message);
        _errorOutput?.WriteLine($"error : {message}");
        _hasLoggedErrors = true;
    }

    public void Error(string code, string message, params object[] args)
    {
        _helper?.LogError(null, code, null, null, 0, 0, 0, 0, message, args);
        _errorOutput?.WriteLine($"error : {code}: {AutoFormat(message, args)}");
        _hasLoggedErrors = true;
    }
}
