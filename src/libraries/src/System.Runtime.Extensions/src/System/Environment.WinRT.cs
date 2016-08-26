// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace System
{
    public static partial class Environment
    {
        public static int ExitCode
        {
            get { return 0; }
            set { throw new PlatformNotSupportedException(); }
        }

        private static string ExpandEnvironmentVariablesCore(string name) => name;

        private static string GetEnvironmentVariableCore(string variable) => string.Empty;

        private static string GetEnvironmentVariableCore(string variable, EnvironmentVariableTarget target) => string.Empty;

        private static IDictionary GetEnvironmentVariablesCore() => new LowLevelDictionary<string, string>();

        private static IDictionary GetEnvironmentVariablesCore(EnvironmentVariableTarget target) => new LowLevelDictionary<string, string>();

        private static string GetFolderPathCore(SpecialFolder folder, SpecialFolderOption option)
        {
            return string.Empty;
        }

        private static bool Is64BitOperatingSystemWhen32BitProcess => false;

        public static string MachineName { get { throw new PlatformNotSupportedException(); } }

        private static Lazy<OperatingSystem> s_osVersion = new Lazy<OperatingSystem>(() =>
        {
            // GetVersionExW isn't available.  We could throw a PlatformNotSupportedException, but we can
            // at least hand back Win32NT to highlight that we're on Windows rather than Unix.
            return new OperatingSystem(PlatformID.Win32NT, new Version(0, 0));
        });

        public static int ProcessorCount => ProcessorCountFromSystemInfo;

        private static void SetEnvironmentVariableCore(string variable, string value)
        {
            throw new PlatformNotSupportedException();
        }

        private static void SetEnvironmentVariableCore(string variable, string value, EnvironmentVariableTarget target)
        {
            throw new PlatformNotSupportedException();
        }

        public static string SystemDirectory { get { throw new PlatformNotSupportedException(); } }

        public static string UserName { get { throw new PlatformNotSupportedException(); } }

        public static string UserDomainName { get { throw new PlatformNotSupportedException(); } }
    }
}
