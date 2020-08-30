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
        bool silent = true)
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
            processStartInfo.WorkingDirectory = workingDir;

        if (envVars != null)
        {
            foreach (KeyValuePair<string, string> envVar in envVars)
                processStartInfo.EnvironmentVariables[envVar.Key] = envVar.Value;
        }

        Process? process = Process.Start(processStartInfo);
        if (process == null)
            throw new ArgumentException($"Process.Start({path} {args}) returned null process");

        process.ErrorDataReceived += (sender, e) =>
        {
            if (!silent)
            {
                LogError(e.Data);
                outputBuilder.AppendLine(e.Data);
            }
            errorBuilder.AppendLine(e.Data);
        };
        process.OutputDataReceived += (sender, e) =>
        {
            if (!silent)
            {
                LogInfo(e.Data);
                outputBuilder.AppendLine(e.Data);
            }
        };
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();

        if (!ignoreErrors && process.ExitCode != 0)
            throw new Exception("Error: " + errorBuilder);

        return outputBuilder.ToString().Trim('\r','\n');
    }

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

    public static TaskLoggingHelper? Logger { get; set; }

    public static void LogInfo(string? msg)
    {
        if (msg != null)
            Logger?.LogMessage(MessageImportance.High, msg);
    }

    public static void LogError(string? msg)
    {
        if (msg != null)
            Logger?.LogError(msg);
    }
}
