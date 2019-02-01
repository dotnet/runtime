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
        Assert.StartsWith("S-1-", sddl); // sid prefix, version 1
        Assert.NotEqual('-', sddl[sddl.Length - 1]);

        string[] parts = sddl.Substring(4).Split('-');

        Assert.NotNull(parts);
        Assert.NotEmpty(parts);
        Assert.InRange(parts.Length, 2, 16); // 1 + MaxSubAuthorities

        string identityPart = parts[0];
        string[] ridParts = new string[parts.Length - 1];
        Array.Copy(parts, 1, ridParts, 0, ridParts.Length);
        long identity = 0;

        Assert.True(long.TryParse(identityPart, out identity));
        Assert.InRange(identity, 0, 0x0000_FFFF_FFFF_FFFF); // 6 bytes, 48 bit number max value

        Assert.All(ridParts, part => uint.TryParse(part, out uint _));
    }

    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer), nameof(PlatformDetection.IsNotWindowsServerCore))]
    public void ValidateToStringUsingWhoami()
    {
        string librarySid = null;
        using (WindowsIdentity me = WindowsIdentity.GetCurrent(false))
        {
            librarySid = me.User.ToString();
        }

        Assert.NotNull(librarySid);
        Assert.NotEmpty(librarySid);

        string output = null;
        string windowsSid = null;
        string processArguments = " /user";
        // Call whois to get current sid
        using (var whoamiProcess = new Process())
        {
            whoamiProcess.StartInfo.FileName = "whoami.exe"; // whoami.exe is in system32, should already be on path
            whoamiProcess.StartInfo.Arguments = " " + processArguments;
            whoamiProcess.StartInfo.CreateNoWindow = true;
            whoamiProcess.StartInfo.UseShellExecute = false;
            whoamiProcess.StartInfo.RedirectStandardOutput = true;
            whoamiProcess.Start();
            output = whoamiProcess.StandardOutput.ReadToEnd();
            whoamiProcess.WaitForExit();

            Assert.Equal(0, whoamiProcess.ExitCode);
        }

        int startSid = output.IndexOf("S-1-");
        Assert.InRange(startSid, 0, int.MaxValue);
        int length = Math.Min(output.Length - startSid, librarySid.Length);
        windowsSid = output.Substring(startSid, length);
        if (output.Length > startSid + length)
        {
            Assert.True(char.IsWhiteSpace(output[startSid + length]));
        }

        Assert.NotNull(windowsSid);
        Assert.Equal(windowsSid, librarySid);
        
    }
}

