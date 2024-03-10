// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace Wasm.Tests.Internal;

internal static class BrowserLocator
{
    public static string FindChrome(string artifactsBinDir, string envVarName)
        => GetBrowserPath(GetChromePathsToProbe(artifactsBinDir), envVarName);

    public static string FindFirefox(string artifactsBinDir, string envVarName)
        => GetBrowserPath(GetFirefoxPathsToProbe(artifactsBinDir), envVarName);

    private static string GetBrowserPath(IEnumerable<string> pathsToProbe, string envVarName)
    {
        string? browserPath = FindBrowserPath();
        if (!string.IsNullOrEmpty(browserPath))
            return Path.GetFullPath(browserPath);

        throw new Exception($"Could not find an installed browser to use. Tried paths: {string.Join(", ", pathsToProbe)}");

        string? FindBrowserPath()
        {
            if (!string.IsNullOrEmpty(envVarName) &&
                Environment.GetEnvironmentVariable(envVarName) is string _browserPath_env_var &&
                !string.IsNullOrEmpty(_browserPath_env_var))
            {
                if (File.Exists(_browserPath_env_var))
                    return _browserPath_env_var;

                Console.WriteLine ($"warning: Could not find {envVarName}={_browserPath_env_var}");
            }

            return pathsToProbe.FirstOrDefault(p => File.Exists(p));
        }
    }

    private static IEnumerable<string> GetChromePathsToProbe(string artifactsBinDir)
    {
        List<string> paths = new();
        if (!string.IsNullOrEmpty(artifactsBinDir))
        {
            // Look for a browser installed in artifacts, for local runs
            paths.Add(Path.Combine(artifactsBinDir, "chrome", "chrome-linux", "chrome"));
            paths.Add(Path.Combine(artifactsBinDir, "chrome", "chrome-win", "chrome.exe"));
        }

        paths.AddRange(new[]
        {
            "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome",
            "/Applications/Microsoft Edge.app/Contents/MacOS/Microsoft Edge",
            "/Applications/Google Chrome Canary.app/Contents/MacOS/Google Chrome Canary",
            "/usr/bin/chromium",
            "C:/Program Files/Google/Chrome/Application/chrome.exe",
            "/usr/bin/chromium-browser"
        });

        return paths;
    }

    private static IEnumerable<string> GetFirefoxPathsToProbe(string artifactsBinDir)
    {
        List<string> paths = new();
        if (!string.IsNullOrEmpty(artifactsBinDir))
        {
            // Look for a browser installed in artifacts, for local runs
            paths.Add(Path.Combine(artifactsBinDir, "firefox", "firefox", "firefox"));
            paths.Add(Path.Combine(artifactsBinDir, "firefox", "firefox", "firefox.exe"));
        }

        paths.AddRange(new[]
        {
            "C:/Program Files/Mozilla Firefox/firefox.exe",
            "/Applications/Firefox.app/Contents/MacOS/firefox",
        });

        return paths;
    }
}
