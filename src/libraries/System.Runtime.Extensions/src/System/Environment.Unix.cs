// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Internal.Runtime.Augments;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace System
{
    public static partial class Environment
    {
        private readonly unsafe static Lazy<LowLevelDictionary<string, string>> s_environ = new Lazy<LowLevelDictionary<string, string>>(() =>
        {
            var results = new LowLevelDictionary<string, string>();
            byte** environ = Interop.Sys.GetEnviron();
            if (environ != null)
            {
                for (byte** ptr = environ; *ptr != null; ptr++)
                {
                    string entry = Marshal.PtrToStringAnsi((IntPtr)(*ptr));
                    int equalsPos = entry.IndexOf('=');
                    if (equalsPos != -1)
                    {
                        results.Add(entry.Substring(0, equalsPos), entry.Substring(equalsPos + 1));
                    }
                    else
                    {
                        results.Add(entry, string.Empty);
                    }
                }
            }
            return results;
        });

        private static string CurrentDirectoryCore
        {
            get { return Interop.Sys.GetCwd(); }
            set { Interop.CheckIo(Interop.Sys.ChDir(value), value, isDirectory: true); }
        }

        public static int ExitCode { get { return EnvironmentAugments.ExitCode; } set { EnvironmentAugments.ExitCode = value; } }

        private static string ExpandEnvironmentVariablesCore(string name)
        {
            StringBuilder result = StringBuilderCache.Acquire();

            int lastPos = 0, pos;
            while (lastPos < name.Length && (pos = name.IndexOf('%', lastPos + 1)) >= 0)
            {
                if (name[lastPos] == '%')
                {
                    string key = name.Substring(lastPos + 1, pos - lastPos - 1);
                    string value = GetEnvironmentVariable(key);
                    if (value != null)
                    {
                        result.Append(value);
                        lastPos = pos + 1;
                        continue;
                    }
                }
                result.Append(name.Substring(lastPos, pos - lastPos));
                lastPos = pos;
            }
            result.Append(name.Substring(lastPos));

            return StringBuilderCache.GetStringAndRelease(result);
        }

        private static string GetEnvironmentVariableCore(string variable)
        {
            // Ensure variable doesn't include a null char
            int nullEnd = variable.IndexOf('\0');
            if (nullEnd != -1)
            {
                variable = variable.Substring(0, nullEnd);
            }

            // Get the value of the variable
            lock (s_environ)
            {
                string value;
                return s_environ.Value.TryGetValue(variable, out value) ? value : null;
            }
        }

        private static string GetEnvironmentVariableCore(string variable, EnvironmentVariableTarget target)
        {
            return target == EnvironmentVariableTarget.Process ?
                GetEnvironmentVariableCore(variable) :
                null;
        }

        private static IDictionary GetEnvironmentVariablesCore()
        {
            lock (s_environ)
            {
                return s_environ.Value.Clone();
            }
        }

        private static IDictionary GetEnvironmentVariablesCore(EnvironmentVariableTarget target)
        {
            return target == EnvironmentVariableTarget.Process ?
                GetEnvironmentVariablesCore() :
                new LowLevelDictionary<string, string>();
        }

        private static string GetFolderPathCore(SpecialFolder folder, SpecialFolderOption option)
        {
            string home = GetEnvironmentVariable("HOME");

            switch (folder)
            {
                case SpecialFolder.Personal: // same as SpecialFolder.MyDocuments
                    if (!string.IsNullOrEmpty(home))
                    {
                        return home;
                    }
                    break;

                // TODO: Add more special folder handling
            }

            throw new PlatformNotSupportedException();
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

        public static string NewLine => "\n";

        private static Lazy<OperatingSystem> s_osVersion = new Lazy<OperatingSystem>(() =>
        {
            int major = 0, minor = 0, build = 0, revision = 0;

            // Get the uname's utsname.release.  Then parse it for the first four numbers found.
            // This isn't perfect, but Version already doesn't map exactly to all possible release
            // formats, e.g. 
            string release = Interop.Sys.GetUnixRelease();
            if (release != null)
            {
                int i = 0;
                major = FindAndParseNextNumber(release, ref i);
                minor = FindAndParseNextNumber(release, ref i);
                build = FindAndParseNextNumber(release, ref i);
                revision = FindAndParseNextNumber(release, ref i);
            }

            return new OperatingSystem(PlatformID.Unix, new Version(major, minor, build, revision));
        });

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
                if ('0' <= c && c <= '9')
                {
                    num = (num * 10) + (c - '0');
                }
                else break;
            }
            return num;
        }

        public static int ProcessorCount => (int)Interop.Sys.SysConf(Interop.Sys.SysConfName._SC_NPROCESSORS_ONLN);

        private static void SetEnvironmentVariableCore(string variable, string value)
        {
            int nullEnd;

            // Ensure variable doesn't include a null char
            nullEnd = variable.IndexOf('\0');
            if (nullEnd != -1)
            {
                variable = variable.Substring(0, nullEnd);
            }

            // Ensure value doesn't include a null char
            if (value != null)
            {
                nullEnd = value.IndexOf('\0');
                if (nullEnd != -1)
                {
                    value = value.Substring(0, nullEnd);
                }
            }

            lock (s_environ)
            {
                // Remove the entry if the value is null, otherwise add/overwrite it
                if (value == null)
                {
                    s_environ.Value.Remove(variable);
                }
                else
                {
                    s_environ.Value[variable] = value;
                }
            }
        }

        private static void SetEnvironmentVariableCore(string variable, string value, EnvironmentVariableTarget target)
        {
            if (target == EnvironmentVariableTarget.Process)
            {
                SetEnvironmentVariableCore(variable, value);
            }
            // other targets ignored
        }

        public static string SystemDirectory => GetFolderPathCore(SpecialFolder.System, SpecialFolderOption.None);

        public static int SystemPageSize => (int)Interop.Sys.SysConf(Interop.Sys.SysConfName._SC_PAGESIZE);

        public static unsafe string UserName
        {
            get
            {
                // First try with a buffer that should suffice for 99% of cases.
                string username;
                const int BufLen = 1024;
                byte* stackBuf = stackalloc byte[BufLen];
                if (TryGetUserNameFromPasswd(stackBuf, BufLen, out username))
                {
                    return username;
                }

                // Fallback to heap allocations if necessary, growing the buffer until
                // we succeed.  TryGetHomeDirectory will throw if there's an unexpected error.
                int lastBufLen = BufLen;
                while (true)
                {
                    lastBufLen *= 2;
                    byte[] heapBuf = new byte[lastBufLen];
                    fixed (byte* buf = heapBuf)
                    {
                        if (TryGetUserNameFromPasswd(buf, heapBuf.Length, out username))
                        {
                            return username;
                        }
                    }
                }

            }
        }

        private static unsafe bool TryGetUserNameFromPasswd(byte* buf, int bufLen, out string path)
        {
            // Call getpwuid_r to get the passwd struct
            Interop.Sys.Passwd passwd;
            int error = Interop.Sys.GetPwUidR(Interop.Sys.GetEUid(), out passwd, buf, bufLen);

            // If the call succeeds, give back the user name retrieved
            if (error == 0)
            {
                Debug.Assert(passwd.Name != null);
                path = Marshal.PtrToStringAnsi((IntPtr)passwd.Name);
                return true;
            }

            // If the current user's entry could not be found, give back null,
            // but still return true as false indicates the buffer was too small.
            if (error == -1)
            {
                path = null;
                return true;
            }

            var errorInfo = new Interop.ErrorInfo(error);

            // If the call failed because the buffer was too small, return false to 
            // indicate the caller should try again with a larger buffer.
            if (errorInfo.Error == Interop.Error.ERANGE)
            {
                path = null;
                return false;
            }

            // Otherwise, fail.
            throw new IOException(errorInfo.GetErrorMessage(), errorInfo.RawErrno);
        }

        public static string UserDomainName => MachineName;
    }
}
