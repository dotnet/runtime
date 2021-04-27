// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
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
            set => Interop.CheckIo(Interop.Sys.ChDir(value), value, isDirectory: true);
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

        public static int SystemPageSize => CheckedSysConf(Interop.Sys.SysConfName._SC_PAGESIZE);

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
    }
}
