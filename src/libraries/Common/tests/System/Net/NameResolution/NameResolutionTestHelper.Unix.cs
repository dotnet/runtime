#nullable enable
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Xunit.Abstractions;

namespace System.Net.NameResolution.Tests
{
    internal static class NameResolutionTestHelper
    {
        /// <summary>
        /// Check if name resolution works at OS level.
        /// In case of failure we may force the tests fail, so we can gather diagnostics from the CI environment.
        /// After collecting enough information we shall alter call sites to use throwOnFailure:false,
        /// since the failure does not indicate an error within the NCL product code.
        /// </summary>
        public static unsafe bool EnsureNameToAddressWorks(string hostName, ITestOutputHelper? testOutput, bool throwOnFailure)
        {
            if (hostName == "")
            {
                // For hostName == "" we should be able to successfully call gethostname(char *name, size_t len) on Unix
                // We use the shim from pal_networking.c (SystemNative_GetHostName)
                byte* name = stackalloc byte[256];
                int res = GetHostName(name, 256);
                if (res != 0) throw new Exception("GetHostName failed.");
                name[255] = 0;
                hostName = Marshal.PtrToStringAnsi((IntPtr)name)!;
            }

            IntPtr hostEntry = gethostbyname(hostName);
            if (hostEntry == IntPtr.Zero)
            {
                string failureInfo =
                    $"Failed to resolve '{hostName}'! {Environment.NewLine}{LogUnixInfo()}";
                testOutput?.WriteLine(failureInfo);
                if (throwOnFailure)
                {
                    throw new Exception(failureInfo);
                }

                return false;
            }

            return true;
        }

        private static string LogUnixInfo()
        {
            StringBuilder bld = new StringBuilder();
            bld.AppendLine($"Dns.GetHostName() == {Dns.GetHostName()}");
            bld.AppendLine("--- /etc/hosts ---");

            string etcHosts;
            try
            {
                etcHosts = File.ReadAllText("/etc/hosts");
            }
            catch (Exception ex)
            {
                etcHosts = $"Failed to retrieve /etc/hosts: {ex.Message}";
            }

            string resolvConf;
            try
            {
                resolvConf = File.ReadAllText("/etc/resolv.conf");
            }
            catch (Exception ex)
            {
                resolvConf = $"Failed to retrieve /etc/resolv.conf: {ex.Message}";
            }

            bld.AppendLine(etcHosts);
            bld.AppendLine("--- /etc/resolv.conf ---");
            bld.AppendLine(resolvConf);
            bld.AppendLine("------");
            return bld.ToString();
        }

        [DllImport("libc")]
        private static extern IntPtr gethostbyname(string name);

        [DllImport("libSystem.Native", EntryPoint = "SystemNative_GetHostName", SetLastError = true)]
        private static extern unsafe int GetHostName(byte* name, int nameLength);
    }
}
