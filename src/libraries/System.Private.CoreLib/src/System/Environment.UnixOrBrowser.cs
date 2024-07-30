// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using System.Threading;

namespace System
{
    public static partial class Environment
    {
        public static bool UserInteractive => true;

        private static string CurrentDirectoryCore
        {
            get => Interop.Sys.GetCwd();
            set => Interop.CheckIo(Interop.Sys.ChDir(value), value, isDirError: true);
        }

        private static string ExpandEnvironmentVariablesCore(string name)
        {
            var result = new ValueStringBuilder(stackalloc char[128]);

            int lastPos = 0, pos;
            while (lastPos < name.Length && (pos = name.IndexOf('%', lastPos + 1)) >= 0)
            {
                if (name[lastPos] == '%')
                {
                    string key = name.Substring(lastPos + 1, pos - lastPos - 1);
                    string? value = GetEnvironmentVariable(key);
                    if (value != null)
                    {
                        result.Append(value);
                        lastPos = pos + 1;
                        continue;
                    }
                }
                result.Append(name.AsSpan(lastPos, pos - lastPos));
                lastPos = pos;
            }
            result.Append(name.AsSpan(lastPos));

            return result.ToString();
        }

        private static bool Is64BitOperatingSystemWhen32BitProcess => false;

        internal const string NewLineConst = "\n";

        public static string SystemDirectory => GetFolderPathCore(SpecialFolder.System, SpecialFolderOption.None);

        private static int GetSystemPageSize() => CheckedSysConf(Interop.Sys.SysConfName._SC_PAGESIZE);

        public static string UserDomainName => MachineName;

        /// <summary>Invoke <see cref="Interop.Sys.SysConf"/>, throwing if it fails.</summary>
        private static int CheckedSysConf(Interop.Sys.SysConfName name)
        {
            long result = Interop.Sys.SysConf(name);
            if (result == -1)
            {
                Interop.ErrorInfo errno = Interop.Sys.GetLastErrorInfo();
                throw errno.Error == Interop.Error.EINVAL ?
                    new ArgumentOutOfRangeException(nameof(name), name, errno.GetErrorMessage()) :
                    Interop.GetIOException(errno);
            }
            return (int)result;
        }

        /// <summary>
        /// Get the CPU usage, including the process time spent running the application code, the process time spent running the operating system code,
        /// and the total time spent running both the application and operating system code.
        /// </summary>
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [UnsupportedOSPlatform("browser")]
        [SupportedOSPlatform("maccatalyst")]
        public static ProcessCpuUsage CpuUsage
        {
            get
            {
                Interop.Sys.ProcessCpuInformation cpuInfo = default;
                Interop.Sys.GetCpuUtilization(ref cpuInfo);

                // Division by 100 is to convert the nanoseconds to 100-nanoseconds to match .NET time units (100-nanoseconds).
                ulong userTime100Nanoseconds = Math.Min(cpuInfo.lastRecordedUserTime / 100, (ulong)long.MaxValue);
                ulong kernelTime100Nanoseconds = Math.Min(cpuInfo.lastRecordedKernelTime / 100, (ulong)long.MaxValue);

                return new ProcessCpuUsage { UserTime = new TimeSpan((long)userTime100Nanoseconds), PrivilegedTime = new TimeSpan((long)kernelTime100Nanoseconds) };
            }
        }
    }
}
