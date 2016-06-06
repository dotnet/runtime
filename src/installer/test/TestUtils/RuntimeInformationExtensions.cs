using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Microsoft.DotNet.CoreSetup.Test
{
    public static class RuntimeInformationExtensions
    {
        public static string GetExeExtensionForCurrentOSPlatform()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return ".exe";
            }
            else
            {
                return "";
            }
        }

        public static string GetSharedLibraryExtensionForCurrentPlatform()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return ".dll";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return ".dylib";
            }
            else
            {
                return ".so";
            }
        }

        public static string GetSharedLibraryPrefixForCurrentPlatform()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return "";
            }
            else
            {
                return "lib";
            }
        }

        public static string GetDefaultNugetDirectoryForCurrentPlatform()
        {
            string userHome = null;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                userHome = Environment.GetEnvironmentVariable("USERPROFILE");
                if (userHome == null)
                {
                    throw new Exception("Unable to determine default nuget directory. USERPROFILE environment variable null");
                }
            }
            else
            {
                userHome = Environment.GetEnvironmentVariable("HOME");
                if (userHome == null)
                {
                    throw new Exception("Unable to determine default nuget directory. HOME environment variable null");
                }
            }

            return $"{userHome}/.nuget/packages";
        }
    }
}
