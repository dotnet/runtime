// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
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

    public static (int exitCode, string output) RunShellCommand(string command,
                                        IDictionary<string, string> envVars,
                                        string workingDir,
                                        MessageImportance debugMessageImportance=MessageImportance.Low)
    {
        string scriptFileName = CreateTemporaryBatchFile(command);
        (string shell, string args) = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                                                    ? ("cmd", $"/c \"{scriptFileName}\"")
                                                    : ("/bin/sh", $"\"{scriptFileName}\"");

        Logger?.LogMessage(debugMessageImportance, $"Running {command} via script {scriptFileName}:");
        Logger?.LogMessage(debugMessageImportance, File.ReadAllText(scriptFileName));

        return TryRunProcess(shell,
                             args,
                             envVars,
                             workingDir,
                             silent: false,
                             debugMessageImportance);

        static string CreateTemporaryBatchFile(string command)
        {
            string extn = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".cmd" : ".sh";
            string file = Path.Combine(Path.GetTempPath(), $"tmp{Guid.NewGuid():N}{extn}");

            using StreamWriter sw = new(file);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                sw.WriteLine("setlocal");
                sw.WriteLine("set errorlevel=dummy");
                sw.WriteLine("set errorlevel=");
            }
            else
            {
                // Use sh rather than bash, as not all 'nix systems necessarily have Bash installed
                sw.WriteLine("#!/bin/sh");
            }

            sw.WriteLine(command);
            return file;
        }
    }

    public static string RunProcess(
        string path,
        string args = "",
        IDictionary<string, string>? envVars = null,
        string? workingDir = null,
        bool ignoreErrors = false,
        bool silent = true,
        MessageImportance debugMessageImportance=MessageImportance.High)
    {
        (int exitCode, string output) = TryRunProcess(
                                            path,
                                            args,
                                            envVars,
                                            workingDir,
                                            silent,
                                            debugMessageImportance);

        if (exitCode != 0 && !ignoreErrors)
            throw new Exception("Error: Process returned non-zero exit code: " + output);

        return output;
    }

    public static (int, string) TryRunProcess(
        string path,
        string args = "",
        IDictionary<string, string>? envVars = null,
        string? workingDir = null,
        bool silent = true,
        MessageImportance debugMessageImportance=MessageImportance.High)
    {
        Logger?.LogMessage(debugMessageImportance, $"Running: {path} {args}");
        var outputBuilder = new StringBuilder();
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

        Logger?.LogMessage(debugMessageImportance, $"Using working directory: {workingDir ?? Environment.CurrentDirectory}");

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
                if (string.IsNullOrEmpty(e.Data))
                    return;

                if (!silent)
                    LogWarning(e.Data);
                outputBuilder.AppendLine(e.Data);
            }
        };
        process.OutputDataReceived += (sender, e) =>
        {
            lock (s_SyncObj)
            {
                if (string.IsNullOrEmpty(e.Data))
                    return;

                if (!silent)
                    Logger?.LogMessage(debugMessageImportance, e.Data);
                outputBuilder.AppendLine(e.Data);
            }
        };
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();

        Logger?.LogMessage(debugMessageImportance, $"Exit code: {process.ExitCode}");
        return (process.ExitCode, outputBuilder.ToString().Trim('\r', '\n'));
    }

    internal static string CreateTemporaryBatchFile(string command)
    {
        string extn = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".cmd" : ".sh";
        string file = Path.Combine(Path.GetTempPath(), $"tmp{Guid.NewGuid():N}{extn}");

        using StreamWriter sw = new(file);
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
                sw.WriteLine("setlocal");
                sw.WriteLine("set errorlevel=dummy");
                sw.WriteLine("set errorlevel=");
        }
        else
        {
            // Use sh rather than bash, as not all 'nix systems necessarily have Bash installed
            sw.WriteLine("#!/bin/sh");
        }

        sw.WriteLine(command);

        return file;
    }

#if NETCOREAPP
    public static void DirectoryCopy(string sourceDir, string destDir, Func<string, bool>? predicate=null)
    {
        string[] files = Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories);
        foreach (string file in files)
        {
            if (predicate != null && !predicate(file))
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
