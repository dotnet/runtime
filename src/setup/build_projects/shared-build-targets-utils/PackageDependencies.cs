using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Cli.Build
{
    public class PackageDependencies
    {
        public static string[] DebianPackageBuildDependencies
        {
            get
            {
                return new string[] 
                {
                    "devscripts",
                    "debhelper",
                    "build-essential"
                };

            }
        }

        public static string[] UbuntuCoreclrAndCoreFxDependencies
        {
            get
            {
               return new string[]
               {
                    "libc6",
                    "libedit2",
                    "libffi6",
                    "libgcc1",
                    "libicu52",
                    "liblldb-3.6",
                    "libllvm3.6",
                    "liblttng-ust0",
                    "liblzma5",
                    "libncurses5",
                    "libpython2.7",
                    "libstdc++6",
                    "libtinfo5",
                    "libunwind8",
                    "liburcu1",
                    "libuuid1",
                    "zlib1g",
                    "libasn1-8-heimdal",
                    "libcomerr2",
                    "libcurl3",
                    "libgcrypt11",
                    "libgnutls26",
                    "libgpg-error0",
                    "libgssapi3-heimdal",
                    "libgssapi-krb5-2",
                    "libhcrypto4-heimdal",
                    "libheimbase1-heimdal",
                    "libheimntlm0-heimdal",
                    "libhx509-5-heimdal",
                    "libidn11",
                    "libk5crypto3",
                    "libkeyutils1",
                    "libkrb5-26-heimdal",
                    "libkrb5-3",
                    "libkrb5support0",
                    "libldap-2.4-2",
                    "libp11-kit0",
                    "libroken18-heimdal",
                    "librtmp0",
                    "libsasl2-2",
                    "libsqlite3-0",
                    "libssl1.0.0",
                    "libtasn1-6",
                    "libwind0-heimdal"
               };
            }
        }

        public static string[] CentosCoreclrAndCoreFxDependencies
        {
            get
            {
               return new string[]
               {
                    "libunwind",
                    "gettext",
                    "libcurl-devel",
                    "openssl-devel",
                    "zlib",
                    "libicu-devel"
               };
            }
        }

    }
}
