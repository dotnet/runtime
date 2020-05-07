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
        /// Check if name resolution (getaddrinfo) works within the executing OS & environment.
        /// In case of failure we may force the tests fail, so we can gather diagnostics from the CI environment.
        /// After collecting enough information we shall alter call sites to use throwOnFailure:false,
        /// since the failure does not indicate an error within the NCL product code.
        /// </summary>
        public static unsafe bool EnsureNameToAddressWorks(string hostName, ITestOutputHelper? testOutput, bool throwOnFailure)
        {
            addrinfo hint = default;
            hint.ai_family = 0; // AF_UNSPEC
            hint.ai_flags = 2; // AI_CANONNAME
            hint.ai_socktype = 1; // SOCK_STREAM;

            addrinfo* res = default;

            int err1 = getaddrinfo(hostName, null, &hint, &res);
            freeaddrinfo(res);
            int err = err1;
            if (err != 0)
            {
                string failureInfo =
                    $"Failed to resolve '{hostName}'! getaddrinfo error: {err}{Environment.NewLine}{LogUnixInfo()}";
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

#pragma warning disable 169
        [StructLayout(LayoutKind.Sequential)]
        unsafe struct addrinfo
        {
            public int ai_flags;
            public int ai_family;
            public int ai_socktype;
            int ai_protocol;
            uint ai_addrlen;
            UIntPtr ai_addr;
            UIntPtr ai_canonname;
            addrinfo* ai_next;
        };
#pragma warning restore 169

        [DllImport("libc")]
        private static extern unsafe int getaddrinfo(string node, string service, addrinfo* hints, addrinfo** res);

        [DllImport("libc")]
        private static extern unsafe void freeaddrinfo(addrinfo* res);
    }
}
