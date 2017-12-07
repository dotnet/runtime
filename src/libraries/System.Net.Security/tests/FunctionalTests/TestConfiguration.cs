// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

using Xunit;

namespace System.Net.Security.Tests
{
    internal static class TestConfiguration
    {
        public const int PassingTestTimeoutMilliseconds = 4 * 60 * 1000;
        public const int FailingTestTimeoutMiliseconds = 250;

        public const string Realm = "TEST.COREFX.NET";
        public const string KerberosUser = "krb_user";
        public const string DefaultPassword = "password";
        public const string HostTarget = "TESTHOST/testfqdn.test.corefx.net";
        public const string HttpTarget = "TESTHTTP@localhost";
        public const string Domain = "TEST";
        public const string NtlmUser = "ntlm_user";
        public const string NtlmPassword = "ntlm_password";
        public const string NtlmUserFilePath = "/var/tmp/ntlm_user_file";

        public static bool SupportsNullEncryption { get { return s_supportsNullEncryption.Value; } }

        public static Task WhenAllOrAnyFailedWithTimeout(params Task[] tasks) 
            => tasks.WhenAllOrAnyFailed(PassingTestTimeoutMilliseconds);

        private static Lazy<bool> s_supportsNullEncryption = new Lazy<bool>(() =>
        {
            // On Windows, null ciphers (no encryption) are supported.
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return true;
            }

            // On macOS, the null cipher (no encryption) is not supported.
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return false;
            }

            // On Unix, it depends on how openssl was built.  So we ask openssl if it has any.
            try
            {
                using (Process p = Process.Start(new ProcessStartInfo("openssl", "ciphers NULL") { RedirectStandardOutput = true, RedirectStandardError = true }))
                {
                    // On some platforms (openSUSE 13.2 is one example), doing this query can print error messages to standard error
                    // when the tests are run via MSBuild, this error message gets picked up and treated as an error from the test itself
                    // causing the task to fail.  We don't actually care about the error text at all, so we just ignore it.
                    p.ErrorDataReceived += ((object sendingProcess, DataReceivedEventArgs errorText) => { /* ignore */ });
                    p.BeginErrorReadLine();

                    return p.StandardOutput.ReadToEnd().Trim().Length > 0;
                }
            }
            catch { return false; }
        });
    }
}
