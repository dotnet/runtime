// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

        public static string[] GetLogicalDrives() => Interop.Sys.GetAllMountPoints();

        private static bool Is64BitOperatingSystemWhen32BitProcess => false;

        public static string MachineName
        {
            get
            {
                string hostName = Interop.Sys.GetHostName();
                int dotPos = hostName.IndexOf('.');
                return dotPos == -1 ? hostName : hostName.Substring(0, dotPos);
            }
        }

        internal const string NewLineConst = "\n";

        private static OperatingSystem GetOSVersion() => GetOperatingSystem(Interop.Sys.GetUnixRelease());

        // Tests exercise this method for corner cases via private reflection
        private static OperatingSystem GetOperatingSystem(string release)
        {
            int major = 0, minor = 0, build = 0, revision = 0;

            // Parse the uname's utsname.release for the first four numbers found.
            // This isn't perfect, but Version already doesn't map exactly to all possible release
            // formats, e.g. 2.6.19-1.2895.fc6
            if (release != null)
            {
                int i = 0;
                major = FindAndParseNextNumber(release, ref i);
                minor = FindAndParseNextNumber(release, ref i);
                build = FindAndParseNextNumber(release, ref i);
                revision = FindAndParseNextNumber(release, ref i);
            }

            // For compatibility reasons with Mono, PlatformID.Unix is returned on MacOSX. PlatformID.MacOSX
            // is hidden from the editor and shouldn't be used.
            return new OperatingSystem(PlatformID.Unix, new Version(major, minor, build, revision));
        }

        private static int FindAndParseNextNumber(string text, ref int pos)
        {
            // Move to the beginning of the number
            for (; pos < text.Length; pos++)
            {
                char c = text[pos];
                if ('0' <= c && c <= '9')
                {
                    break;
                }
            }

            // Parse the number;
            int num = 0;
            for (; pos < text.Length; pos++)
            {
                char c = text[pos];
                if ('0' > c || c > '9')
                    break;

                try
                {
                    num = checked((num * 10) + (c - '0'));
                }
                // Integer overflow can occur for example with:
                //     Linux nelknet 4.15.0-24201807041620-generic
                // To form a valid Version, num must be positive.
                catch (OverflowException)
                {
                    return int.MaxValue;
                }
            }

            return num;
        }

        public static string SystemDirectory => GetFolderPathCore(SpecialFolder.System, SpecialFolderOption.None);

        public static int SystemPageSize => CheckedSysConf(Interop.Sys.SysConfName._SC_PAGESIZE);

        public static unsafe string UserName
        {
            get
            {
                // First try with a buffer that should suffice for 99% of cases.
                string? username;
                const int BufLen = Interop.Sys.Passwd.InitialBufferSize;
                byte* stackBuf = stackalloc byte[BufLen];
                if (TryGetUserNameFromPasswd(stackBuf, BufLen, out username))
                {
                    return username ?? string.Empty;
                }

                // Fallback to heap allocations if necessary, growing the buffer until
                // we succeed.  TryGetUserNameFromPasswd will throw if there's an unexpected error.
                int lastBufLen = BufLen;
                while (true)
                {
                    lastBufLen *= 2;
                    byte[] heapBuf = new byte[lastBufLen];
                    fixed (byte* buf = &heapBuf[0])
                    {
                        if (TryGetUserNameFromPasswd(buf, heapBuf.Length, out username))
                        {
                            return username ?? string.Empty;
                        }
                    }
                }

            }
        }

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

        public static long WorkingSet
        {
            get
            {
                Type? processType = Type.GetType("System.Diagnostics.Process, System.Diagnostics.Process, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", throwOnError: false);
                if (processType?.GetMethod("GetCurrentProcess")?.Invoke(null, BindingFlags.DoNotWrapExceptions, null, null, null) is IDisposable currentProcess)
                {
                    using (currentProcess)
                    {
                        if (processType!.GetMethod("get_WorkingSet64")?.Invoke(currentProcess, BindingFlags.DoNotWrapExceptions, null, null, null) is long result)
                            return result;
                    }
                }

                // Could not get the current working set.
                return 0;
            }
        }
    }
}
