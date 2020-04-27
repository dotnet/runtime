// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        bool silent = false)
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
            if (!silent)
            {
                LogError(e.Data);
                outputBuilder.AppendLine(e.Data);
                errorBuilder.AppendLine(e.Data);
            }
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
        {
            throw new Exception("Error: " + errorBuilder);
        }

        return outputBuilder.ToString().Trim('\r','\n');
    }

    public static void DirectoryCopy(string sourceDirName, string destDirName, Func<string, bool> predicate)
    {
        var dir = new DirectoryInfo(sourceDirName);
        DirectoryInfo[] dirs = dir.GetDirectories();
        if (!Directory.Exists(destDirName))
        {
            Directory.CreateDirectory(destDirName);
        }
        FileInfo[] files = dir.GetFiles();
        foreach (FileInfo file in files)
        {
            if (predicate(file.FullName))
            {
                file.CopyTo(Path.Combine(destDirName, file.Name), false);
            }
        }
        foreach (DirectoryInfo subdir in dirs)
        {
            if (subdir.FullName != destDirName)
            {
                DirectoryCopy(subdir.FullName, Path.Combine(destDirName, subdir.Name), predicate);
            }
        }
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
