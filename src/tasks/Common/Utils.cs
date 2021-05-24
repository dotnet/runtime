// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

internal static class Utils
{
    private static readonly object s_SyncObj = new object();

    public static string GetEmbeddedResource(string file)
    {
        using Stream stream = typeof(Utils).Assembly
            .GetManifestResourceStream($"{typeof(Utils).Assembly.GetName().Name}.Templates.{file}")!;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    public static string RunProcess(
        string path,
        string args = "",
        IDictionary<string, string>? envVars = null,
        string? workingDir = null,
        bool ignoreErrors = false,
        bool silent = true,
        MessageImportance outputMessageImportance=MessageImportance.High,
        MessageImportance debugMessageImportance=MessageImportance.High)
    {
        LogInfo($"Running: {path} {args}", debugMessageImportance);
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
            processStartInfo.WorkingDirectory = workingDir;

        LogInfo($"Using working directory: {workingDir ?? Environment.CurrentDirectory}", debugMessageImportance);

        if (envVars != null)
        {
            if (envVars.Count > 0)
                Logger?.LogMessage(MessageImportance.Low, "Setting environment variables for execution:");

            foreach (KeyValuePair<string, string> envVar in envVars)
            {
                processStartInfo.EnvironmentVariables[envVar.Key] = envVar.Value;
                Logger?.LogMessage(MessageImportance.Low, $"\t{envVar.Key} = {envVar.Value}");
            }
        }

        Process? process = Process.Start(processStartInfo);
        if (process == null)
            throw new ArgumentException($"Process.Start({path} {args}) returned null process");

        process.ErrorDataReceived += (sender, e) =>
        {
            lock (s_SyncObj)
            {
                if (!silent)
                {
                    LogWarning(e.Data);
                    outputBuilder.AppendLine(e.Data);
                }
                errorBuilder.AppendLine(e.Data);
            }
        };
        process.OutputDataReceived += (sender, e) =>
        {
            lock (s_SyncObj)
            {
                if (!silent)
                {
                    LogInfo(e.Data, outputMessageImportance);
                    outputBuilder.AppendLine(e.Data);
                }
            }
        };
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            Logger?.LogMessage(MessageImportance.High, $"Exit code: {process.ExitCode}");
            if (!ignoreErrors)
                throw new Exception("Error: Process returned non-zero exit code: " + errorBuilder);
        }

        return outputBuilder.ToString().Trim('\r', '\n');
    }

#if NETCOREAPP
    public static void DirectoryCopy(string sourceDir, string destDir, Func<string, bool> predicate)
    {
        string[] files = Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories);
        foreach (string file in files)
        {
            if (!predicate(file))
                continue;

            string relativePath = Path.GetRelativePath(sourceDir, file);
            string? relativeDir = Path.GetDirectoryName(relativePath);
            if (!string.IsNullOrEmpty(relativeDir))
                Directory.CreateDirectory(Path.Combine(destDir, relativeDir));

            File.Copy(file, Path.Combine(destDir, relativePath), true);
        }
    }
#endif

    public static TaskLoggingHelper? Logger { get; set; }

    public static void LogInfo(string? msg, MessageImportance importance=MessageImportance.High)
    {
        if (msg != null)
            Logger?.LogMessage(importance, msg);
    }

    public static void LogWarning(string? msg)
    {
        if (msg != null)
            Logger?.LogWarning(msg);
    }

    public static void LogError(string? msg)
    {
        if (msg != null)
            Logger?.LogError(msg);
    }
}
