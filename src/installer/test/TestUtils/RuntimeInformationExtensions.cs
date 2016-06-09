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
    }
}
