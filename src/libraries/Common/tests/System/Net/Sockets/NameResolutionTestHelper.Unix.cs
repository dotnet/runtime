using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Net.NameResolution.Tests
{
    partial static class NameResolutionTestHelper
    {
        public static void EnsureOsNameResolutionWorks(string hostName)
        {
            int err = CanResolveHostByOs(hostName);
            if (err != 0)
            {
                // We allow the tests fail for now to gather diagnostics from the CI environment.
                // We should terminate (success or skip), when we collected enough information from possible future failures.
                throw new Exception($"Failed to resolve '{hostName}'! getaddrinfo error: {err}{Environment.NewLine}{LogUnixInfo()}");
            }
        }

        private static unsafe int CanResolveHostByOs(string hostName)
        {
            addrinfo hint = default;
            hint.ai_family = 0; // AF_UNSPEC
            hint.ai_flags = 2; // AI_CANONNAME
            hint.ai_socktype = 1; // SOCK_STREAM;

            addrinfo* res = default;

            int err = getaddrinfo(hostName, null, &hint, &res);
            freeaddrinfo(res);
            return err;
        }

        private static StringBuilder LogUnixInfo()
        {
            StringBuilder bld = new StringBuilder();
            bld.AppendLine($"Dns.GetHostName() == {Dns.GetHostName()}");
            bld.AppendLine("--- /etc/hosts ---");
            bld.AppendLine(File.ReadAllText("/etc/hosts"));
            bld.AppendLine("--- /etc/resolv.conf ---");
            bld.AppendLine(File.ReadAllText("/etc/resolv.conf"));
            bld.AppendLine("------");
            return bld;
        }

#pragma warning disable 169
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
