// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Security.Principal;
using Xunit;

public class SecurityIdentifierTests
{
    [Fact]
    public void ValidateGetCurrentUser()
    {
        using (WindowsIdentity identity = WindowsIdentity.GetCurrent(false))
        {
            Assert.NotNull(identity.User);
        }
    }

    [Fact]
    public void ValidateToString()
    {
        string sddl = null;
        using (WindowsIdentity me = WindowsIdentity.GetCurrent(false))
        {
            sddl = me.User.ToString();
        }

        Assert.NotNull(sddl);
        Assert.NotEmpty(sddl);
        Assert.StartsWith("S-1-5-", sddl); // sid prefix, version 1, user account type
        Assert.NotEqual('-', sddl[sddl.Length - 1]);

        string[] parts = sddl.Substring(6).Split('-');

        Assert.NotNull(parts);
        Assert.NotEmpty(parts);

        Assert.All(parts, part => uint.TryParse(part, out uint _));
    }

    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer), nameof(PlatformDetection.IsNotWindowsServerCore))]
    public void ValidateToStringUsingWhoami()
    {
        string librarySid = null;
        using (WindowsIdentity me = WindowsIdentity.GetCurrent(false))
        {
            librarySid = me.User.ToString();
        }

        string windowsSid = null;
        string processArguments = " /user";
        // Call whois to get current sid
        Process whoamiProcess = new Process();
        whoamiProcess.StartInfo.FileName = "whoami.exe"; // whoami.exe is in system32, should already be on path
        whoamiProcess.StartInfo.Arguments = " " + processArguments;
        whoamiProcess.StartInfo.CreateNoWindow = true;
        whoamiProcess.StartInfo.UseShellExecute = false;
        whoamiProcess.StartInfo.RedirectStandardOutput = true;
        whoamiProcess.Start();
        string output = whoamiProcess.StandardOutput.ReadToEnd();
        whoamiProcess.WaitForExit();

        if (whoamiProcess.ExitCode == 0)
        {
            int startSid = output.IndexOf("S-1-5-");
            int endSid = 0;
            if (startSid >= 0)
            {
                int length = Math.Min(output.Length - startSid, librarySid.Length);
                windowsSid = output.Substring(startSid, length);

                if (output.Length > startSid + length)
                {
                    Assert.True(char.IsWhiteSpace(output[startSid + length]));
                }
            }
            if (endSid > startSid)
            {
                windowsSid = output.Substring(startSid, (endSid - startSid));
            }
        }
        Assert.NotNull(windowsSid);
        Assert.NotEmpty(windowsSid);
        Assert.NotNull(librarySid);
        Assert.NotEmpty(librarySid);

        Assert.Equal(windowsSid, librarySid);
    }
}

