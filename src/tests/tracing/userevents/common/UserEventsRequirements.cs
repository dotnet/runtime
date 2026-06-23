// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;

namespace Tracing.UserEvents.Tests.Common
{
    internal static class UserEventsRequirements
    {
        private static readonly Version s_minKernelVersion = new(6, 4);
        private const string TracefsPath = "/sys/kernel/tracing";
        private const string UserEventsDataPath = "/sys/kernel/tracing/user_events_data";
        // https://github.com/microsoft/one-collect/issues/225
        private static readonly Version s_minGlibcVersion = new(2, 35);

        internal static bool IsSupported()
        {
            if (Environment.OSVersion.Version < s_minKernelVersion)
            {
                Console.WriteLine($"Kernel version '{Environment.OSVersion.Version}' is less than the minimum required '{s_minKernelVersion}'");
                return false;
            }

            return IsGlibcAtLeast(s_minGlibcVersion) &&
                   IsTracefsMounted() &&
                   IsUserEventsDataAvailable();
        }

        private static bool IsTracefsMounted()
        {
            ProcessStartInfo checkTracefsStartInfo = new();
            checkTracefsStartInfo.FileName = "sudo";
            checkTracefsStartInfo.Arguments = $"-n test -d {TracefsPath}";
            checkTracefsStartInfo.UseShellExecute = false;
            checkTracefsStartInfo.RedirectStandardOutput = true;
            checkTracefsStartInfo.RedirectStandardError = true;

            ProcessTextOutput result = Process.RunAndCaptureText(checkTracefsStartInfo);
            foreach (string rawLine in result.StandardOutput.Split('\n'))
            {
                string line = rawLine.TrimEnd('\r');
                if (!string.IsNullOrEmpty(line))
                {
                    Console.WriteLine($"[tracefs-check] {line}");
                }
            }
            foreach (string rawLine in result.StandardError.Split('\n'))
            {
                string line = rawLine.TrimEnd('\r');
                if (!string.IsNullOrEmpty(line))
                {
                    Console.Error.WriteLine($"[tracefs-check] {line}");
                }
            }

            if (result.ExitStatus.ExitCode == 0)
            {
                return true;
            }

            Console.WriteLine($"Tracefs not mounted at '{TracefsPath}'.");
            return false;
        }

        private static bool IsUserEventsDataAvailable()
        {
            ProcessStartInfo checkUserEventsDataStartInfo = new();
            checkUserEventsDataStartInfo.FileName = "sudo";
            checkUserEventsDataStartInfo.Arguments = $"-n test -e {UserEventsDataPath}";
            checkUserEventsDataStartInfo.UseShellExecute = false;
            checkUserEventsDataStartInfo.RedirectStandardOutput = true;
            checkUserEventsDataStartInfo.RedirectStandardError = true;

            ProcessTextOutput result = Process.RunAndCaptureText(checkUserEventsDataStartInfo);
            foreach (string rawLine in result.StandardOutput.Split('\n'))
            {
                string line = rawLine.TrimEnd('\r');
                if (!string.IsNullOrEmpty(line))
                {
                    Console.WriteLine($"[user-events-check] {line}");
                }
            }
            foreach (string rawLine in result.StandardError.Split('\n'))
            {
                string line = rawLine.TrimEnd('\r');
                if (!string.IsNullOrEmpty(line))
                {
                    Console.Error.WriteLine($"[user-events-check] {line}");
                }
            }

            if (result.ExitStatus.ExitCode == 0)
            {
                return true;
            }

            Console.WriteLine($"User events data not available at '{UserEventsDataPath}'.");
            return false;
        }

        private static bool IsGlibcAtLeast(Version minVersion)
        {
            ProcessStartInfo lddStartInfo = new();
            lddStartInfo.FileName = "ldd";
            lddStartInfo.Arguments = "--version";
            lddStartInfo.UseShellExecute = false;
            lddStartInfo.RedirectStandardOutput = true;
            lddStartInfo.RedirectStandardError = true;

            ProcessTextOutput result;
            try
            {
                result = Process.RunAndCaptureText(lddStartInfo);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to start 'ldd --version': {ex.Message}");
                return false;
            }

            foreach (string rawLine in result.StandardError.Split('\n'))
            {
                string line = rawLine.TrimEnd('\r');
                if (!string.IsNullOrEmpty(line))
                {
                    Console.Error.WriteLine($"[ldd] {line}");
                }
            }

            string? detectedVersionLine = null;
            foreach (string rawLine in result.StandardOutput.Split('\n'))
            {
                string line = rawLine.TrimEnd('\r');
                if (!string.IsNullOrEmpty(line))
                {
                    detectedVersionLine = line;
                    break;
                }
            }

            if (result.ExitStatus.ExitCode != 0)
            {
                Console.WriteLine($"'ldd --version' exited with code {result.ExitStatus.ExitCode}.");
                return false;
            }

            if (string.IsNullOrEmpty(detectedVersionLine))
            {
                Console.WriteLine("Could not read glibc version from 'ldd --version' output.");
                return false;
            }

            string[] tokens = detectedVersionLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            Version? glibcVersion = null;
            foreach (string token in tokens)
            {
                if (Version.TryParse(token, out Version? parsedVersion))
                {
                    glibcVersion = parsedVersion;
                    break;
                }
            }

            if (glibcVersion is null)
            {
                Console.WriteLine($"Failed to parse glibc version from 'ldd --version' output line: {detectedVersionLine}");
                return false;
            }

            if (glibcVersion < minVersion)
            {
                Console.WriteLine($"glibc version '{glibcVersion}' is less than required '{minVersion}'.");
                return false;
            }

            return true;
        }
    }
}
