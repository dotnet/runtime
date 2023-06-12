// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Reflection.PortableExecutable;
using System.Reflection.Metadata;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

internal static class Utils
{
    public static string WebcilInWasmExtension = ".wasm";

    private static readonly object s_SyncObj = new object();

    public static string GetEmbeddedResource(string file)
    {
        using Stream stream = typeof(Utils).Assembly
            .GetManifestResourceStream($"{typeof(Utils).Assembly.GetName().Name}.Templates.{file}")!;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    public static bool IsNewerThan(string inFile, string outFile)
        => !File.Exists(inFile) || !File.Exists(outFile) ||
                (File.GetLastWriteTimeUtc(inFile) > File.GetLastWriteTimeUtc(outFile));

    public static (int exitCode, string output) RunShellCommand(
                                        TaskLoggingHelper logger,
                                        string command,
                                        IDictionary<string, string> envVars,
                                        string workingDir,
                                        bool silent=false,
                                        bool logStdErrAsMessage=false,
                                        MessageImportance debugMessageImportance=MessageImportance.Low,
                                        string? label=null)
    {
        string scriptFileName = CreateTemporaryBatchFile(command);
        (string shell, string args) = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                                                    ? ("cmd", $"/c \"{scriptFileName}\"")
                                                    : ("/bin/sh", $"\"{scriptFileName}\"");

        string msgPrefix = label == null ? string.Empty : $"[{label}] ";
        logger.LogMessage(debugMessageImportance, $"{msgPrefix}Running {command} via script {scriptFileName}:", msgPrefix);
        logger.LogMessage(debugMessageImportance, File.ReadAllText(scriptFileName), msgPrefix);

        return TryRunProcess(logger,
                             shell,
                             args,
                             envVars,
                             workingDir,
                             silent: silent,
                             logStdErrAsMessage: logStdErrAsMessage,
                             label: label,
                             debugMessageImportance: debugMessageImportance);

        static string CreateTemporaryBatchFile(string command)
        {
            string extn = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".cmd" : ".sh";
            string file = Path.Combine(Path.GetTempPath(), $"tmp{Guid.NewGuid():N}{extn}");

            using StreamWriter sw = new(file);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // set encoding to UTF-8 -> full Unicode support is needed for usernames -
                // `command` contains tmp dir path with the username
                sw.WriteLine(@"%SystemRoot%\System32\chcp.com 65001>nul");
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
        TaskLoggingHelper logger,
        string path,
        string args = "",
        IDictionary<string, string>? envVars = null,
        string? workingDir = null,
        bool ignoreErrors = false,
        bool silent = true,
        MessageImportance debugMessageImportance=MessageImportance.High)
    {
        (int exitCode, string output) = TryRunProcess(
                                            logger,
                                            path,
                                            args,
                                            envVars,
                                            workingDir,
                                            silent: silent,
                                            debugMessageImportance: debugMessageImportance);

        if (exitCode != 0 && !ignoreErrors)
            throw new Exception("Error: Process returned non-zero exit code: " + output);

        return output;
    }

    public static (int, string) TryRunProcess(
        TaskLoggingHelper logger,
        string path,
        string args = "",
        IDictionary<string, string>? envVars = null,
        string? workingDir = null,
        bool silent = true,
        bool logStdErrAsMessage = false,
        MessageImportance debugMessageImportance=MessageImportance.High,
        string? label=null,
        Action<Stream>? inputProvider = null)
    {
        string msgPrefix = label == null ? string.Empty : $"[{label}] ";
        logger.LogMessage(debugMessageImportance, $"{msgPrefix}Running: {path} {args}");
        var outputBuilder = new StringBuilder();
        var processStartInfo = new ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            RedirectStandardInput = inputProvider != null,
            Arguments = args,
        };

        if (workingDir != null)
            processStartInfo.WorkingDirectory = workingDir;

        logger.LogMessage(debugMessageImportance, $"{msgPrefix}Using working directory: {workingDir ?? Environment.CurrentDirectory}", msgPrefix);

        if (envVars != null)
        {
            if (envVars.Count > 0)
                logger.LogMessage(MessageImportance.Low, $"{msgPrefix}Setting environment variables for execution:", msgPrefix);

            foreach (KeyValuePair<string, string> envVar in envVars)
            {
                processStartInfo.EnvironmentVariables[envVar.Key] = envVar.Value;
                logger.LogMessage(MessageImportance.Low, $"{msgPrefix}\t{envVar.Key} = {envVar.Value}");
            }
        }

        Process? process = Process.Start(processStartInfo);
        if (process == null)
            throw new ArgumentException($"{msgPrefix}Process.Start({path} {args}) returned null process");

        process.ErrorDataReceived += (sender, e) =>
        {
            lock (s_SyncObj)
            {
                if (string.IsNullOrEmpty(e.Data))
                    return;

                string msg = $"{msgPrefix}{e.Data}";
                if (!silent)
                {
                    if (logStdErrAsMessage)
                        logger.LogMessage(debugMessageImportance, e.Data, msgPrefix);
                    else
                        logger.LogWarning(msg);
                }
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
                    logger.LogMessage(debugMessageImportance, e.Data, msgPrefix);
                outputBuilder.AppendLine(e.Data);
            }
        };
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        inputProvider?.Invoke(process.StandardInput.BaseStream);
        process.WaitForExit();

        logger.LogMessage(debugMessageImportance, $"{msgPrefix}Exit code: {process.ExitCode}");
        return (process.ExitCode, outputBuilder.ToString().Trim('\r', '\n'));
    }

    public static bool CopyIfDifferent(string src, string dst, bool useHash)
    {
        if (!File.Exists(src))
            throw new ArgumentException($"Cannot find {src} file to copy", nameof(src));

        bool areDifferent = !File.Exists(dst) ||
                                (useHash && ComputeHash(src) != ComputeHash(dst)) ||
                                (File.ReadAllText(src) != File.ReadAllText(dst));

        if (areDifferent)
            File.Copy(src, dst, true);

        return areDifferent;
    }

    public static string ComputeHash(string filepath)
    {
        using var stream = File.OpenRead(filepath);
        using HashAlgorithm hashAlgorithm = SHA512.Create();

        byte[] hash = hashAlgorithm.ComputeHash(stream);
        return Convert.ToBase64String(hash);
    }

    public static string ComputeIntegrity(string filepath)
    {
        using var stream = File.OpenRead(filepath);
        using HashAlgorithm hashAlgorithm = SHA256.Create();

        byte[] hash = hashAlgorithm.ComputeHash(stream);
        return "sha256-" + Convert.ToBase64String(hash);
    }

    public static string ComputeIntegrity(byte[] bytes)
    {
        using HashAlgorithm hashAlgorithm = SHA256.Create();

        byte[] hash = hashAlgorithm.ComputeHash(bytes);
        return "sha256-" + Convert.ToBase64String(hash);
    }

    public static string ComputeTextIntegrity(string str)
    {
        using HashAlgorithm hashAlgorithm = SHA256.Create();

        var bytes = Encoding.UTF8.GetBytes(str);
        byte[] hash = hashAlgorithm.ComputeHash(bytes);
        return "sha256-" + Convert.ToBase64String(hash);
    }

#if NETCOREAPP
    public static void DirectoryCopy(string sourceDir, string destDir, Func<string, bool>? predicate=null)
    {
        if (!Directory.Exists(destDir))
            Directory.CreateDirectory(destDir);

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

    public static bool IsManagedAssembly(string filePath)
    {
        if (!File.Exists(filePath))
            return false;

        // Try to read CLI metadata from the PE file.
        using FileStream fileStream = File.OpenRead(filePath);
        using PEReader peReader = new(fileStream, PEStreamOptions.Default);
        return IsManagedAssembly(peReader);
    }

    public static bool IsManagedAssembly(byte[] bytes)
    {
        using var peReader = new PEReader(ImmutableArray.Create(bytes));
        return IsManagedAssembly(peReader);
    }

    private static bool IsManagedAssembly(PEReader peReader)
    {
        try
        {
            if (!peReader.HasMetadata)
            {
                return false; // File does not have CLI metadata.
            }

            // Check that file has an assembly manifest.
            MetadataReader reader = peReader.GetMetadataReader();
            return reader.IsAssembly;
        }
        catch (BadImageFormatException)
        {
            return false;
        }
        catch (FileNotFoundException)
        {
            return false;
        }
    }
}
