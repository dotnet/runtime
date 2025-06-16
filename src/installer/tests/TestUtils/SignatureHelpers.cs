// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

public class SignatureHelpers
{
    public static bool HasEntitlements(string path)
    {
        ProcessStartInfo psi = new ProcessStartInfo
        {
            FileName = "codesign",
            Arguments = $"-d --entitlements - \"{path}\" --xml",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using (Process process = Process.Start(psi))
        {
            process.WaitForExit();
            var entitlements = process.StandardOutput.ReadToEnd();
            return !string.IsNullOrEmpty(entitlements);
        }
    }
}
