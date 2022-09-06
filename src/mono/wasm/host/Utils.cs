// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.WebAssembly.AppHost;

#nullable enable

public class Utils
{
    public static async Task<int> TryRunProcess(
        ProcessStartInfo psi,
        ILogger logger,
        Action<string?>? logStdOut = null,
        Action<string?>? logStdErr = null,
        string? label = null)
    {
        string msgPrefix = label == null ? string.Empty : $"[{label}] ";
        logger.LogInformation($"{msgPrefix}Running: {psi.FileName} {string.Join(" ", psi.ArgumentList)}");

        psi.UseShellExecute = false;
        psi.CreateNoWindow = true;
        if (logStdOut != null)
            psi.RedirectStandardOutput = true;
        if (logStdErr != null)
            psi.RedirectStandardError = true;

        logger.LogDebug($"{msgPrefix}Using working directory: {psi.WorkingDirectory ?? Environment.CurrentDirectory}", msgPrefix);

        // if (psi.EnvironmentVariables.Count > 0)
            // logger.LogDebug($"{msgPrefix}Setting environment variables for execution:", msgPrefix);

        // foreach (string key in psi.EnvironmentVariables.Keys)
            // logger.LogDebug($"{msgPrefix}\t{key} = {psi.EnvironmentVariables[key]}");

        Process? process = Process.Start(psi);
        if (process == null)
            throw new ArgumentException($"{msgPrefix}Process.Start({psi.FileName} {string.Join(" ", psi.ArgumentList)}) returned null process");

        if (logStdErr != null)
            process.ErrorDataReceived += (sender, e) => logStdErr!.Invoke(e.Data);
        if (logStdOut != null)
            process.OutputDataReceived += (sender, e) => logStdOut!.Invoke(e.Data);

        if (logStdOut != null)
            process.BeginOutputReadLine();
        if (logStdErr != null)
            process.BeginErrorReadLine();

        await process.WaitForExitAsync();
        // Ensure all async handlers have been called
        process.WaitForExit();
        return process.ExitCode;
    }
}
