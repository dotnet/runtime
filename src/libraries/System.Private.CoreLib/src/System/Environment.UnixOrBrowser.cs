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

        private static int GetCurrentProcessId() => Interop.Sys.GetPid();

        internal const string NewLineConst = "\n";

        public static string SystemDirectory => GetFolderPathCore(SpecialFolder.System, SpecialFolderOption.None);

        public static int SystemPageSize => CheckedSysConf(Interop.Sys.SysConfName._SC_PAGESIZE);

        private static unsafe bool TryGetUserNameFromPasswd(byte* buf, int bufLen, out string? username)
        {
            // Call getpwuid_r to get the passwd struct
            Interop.Sys.Passwd passwd;
            int error = Interop.Sys.GetPwUidR(Interop.Sys.GetEUid(), out passwd, buf, bufLen);

            // If the call succeeds, give back the user name retrieved
            if (error == 0)
            {
                Debug.Assert(passwd.Name != null);
                username = Marshal.PtrToStringAnsi((IntPtr)passwd.Name);
                return true;
            }

            // If the current user's entry could not be found, give back null,
            // but still return true (false indicates the buffer was too small).
            if (error == -1)
            {
                username = null;
                return true;
            }

            var errorInfo = new Interop.ErrorInfo(error);

            // If the call failed because the buffer was too small, return false to
            // indicate the caller should try again with a larger buffer.
            if (errorInfo.Error == Interop.Error.ERANGE)
            {
                username = null;
                return false;
            }

            // Otherwise, fail.
            throw new IOException(errorInfo.GetErrorMessage(), errorInfo.RawErrno);
        }

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
