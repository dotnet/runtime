// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

internal class Utils
{
    public static string GetEmbeddedResource(string file)
    {
        using Stream stream = typeof(Utils).Assembly
            .GetManifestResourceStream("AppleAppBuilder.Templates." + file)!;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    public static string RunProcess(
        string path,
        string args = "",
        IDictionary<string, string>? envVars = null,
        string? workingDir = null,
        bool ignoreErrors = false)
    {
        LogInfo($"Running: {path} {args}");
        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();
        var processStartInfo = new ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            Arguments = args,
        };

        if (workingDir != null)
        {
            processStartInfo.WorkingDirectory = workingDir;
        }

        if (envVars != null)
        {
            foreach (KeyValuePair<string, string> envVar in envVars)
            {
                processStartInfo.EnvironmentVariables[envVar.Key] = envVar.Value;
            }
        }

        Process process = Process.Start(processStartInfo)!;
        process.ErrorDataReceived += (sender, e) =>
        {
            LogError(e.Data);
            outputBuilder.AppendLine(e.Data);
            errorBuilder.AppendLine(e.Data);
        };
        process.OutputDataReceived += (sender, e) =>
        {
            LogInfo(e.Data);
            outputBuilder.AppendLine(e.Data);
        };
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();
        if (process.ExitCode != 0)
        {
            throw new Exception("Error: " + errorBuilder);
        }

        return outputBuilder.ToString().Trim('\r','\n');
    }

    public static TaskLoggingHelper? Logger { get; set; }

    public static void LogInfo(string? msg)
    {
        if (msg != null)
        {
            Logger?.LogMessage(MessageImportance.High, msg);
        }
    }

    public static void LogError(string? msg)
    {
        if (msg != null)
        {
            Logger?.LogError(msg);
        }
    }
}
