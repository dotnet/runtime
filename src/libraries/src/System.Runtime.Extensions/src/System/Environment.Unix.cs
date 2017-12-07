// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Internal.Runtime.Augments;
using System.Collections;
using System.Collections.Generic;
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
        internal static readonly bool IsMac = Interop.Sys.GetUnixName() == "OSX";
        private static Func<string, IEnumerable<string>> s_fileReadLines;
        private static Action<string> s_directoryCreateDirectory;

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

        private static string GetFolderPathCore(SpecialFolder folder, SpecialFolderOption option)
        {
            // Get the path for the SpecialFolder
            string path = GetFolderPathCoreWithoutValidation(folder);
            Debug.Assert(path != null);

            // If we didn't get one, or if we got one but we're not supposed to verify it,
            // or if we're supposed to verify it and it passes verification, return the path.
            if (path.Length == 0 ||
                option == SpecialFolderOption.DoNotVerify ||
                Interop.Sys.Access(path, Interop.Sys.AccessMode.R_OK) == 0)
            {
                return path;
            }

            // Failed verification.  If None, then we're supposed to return an empty string.
            // If Create, we're supposed to create it and then return the path.
            if (option == SpecialFolderOption.None)
            {
                return string.Empty;
            }
            else
            {
                Debug.Assert(option == SpecialFolderOption.Create);

                // TODO #11151: Replace with Directory.CreateDirectory once we have access to System.IO.FileSystem here.
                Action<string> createDirectory = LazyInitializer.EnsureInitialized(ref s_directoryCreateDirectory, () =>
                {
                    Type dirType = Type.GetType("System.IO.Directory, System.IO.FileSystem, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", throwOnError: true);
                    MethodInfo mi = dirType.GetTypeInfo().GetDeclaredMethod("CreateDirectory");
                    return (Action<string>)mi.CreateDelegate(typeof(Action<string>));
                });
                createDirectory(path);

                return path;
            }
        }

        private static string GetFolderPathCoreWithoutValidation(SpecialFolder folder)
        {
            // First handle any paths that involve only static paths, avoiding the overheads of getting user-local paths.
            // https://www.freedesktop.org/software/systemd/man/file-hierarchy.html
            switch (folder)
            {
                case SpecialFolder.CommonApplicationData: return "/usr/share";
                case SpecialFolder.CommonTemplates: return "/usr/share/templates";
            }
            if (IsMac)
            {
                switch (folder)
                {
                    case SpecialFolder.ProgramFiles: return "/Applications";
                    case SpecialFolder.System: return "/System";
                }
            }

            // All other paths are based on the XDG Base Directory Specification:
            // https://specifications.freedesktop.org/basedir-spec/latest/
            string home;
            try
            {
                home = PersistedFiles.GetHomeDirectory();
            }
            catch (Exception exc)
            {
                Debug.Fail($"Unable to get home directory: {exc}");
                home = Path.GetTempPath();
            }
            Debug.Assert(!string.IsNullOrEmpty(home), "Expected non-null or empty HOME");

            // TODO: Consider caching (or precomputing and caching) all subsequent results.
            // This would significantly improve performance for repeated access, at the expense
            // of not being responsive to changes in the underlying environment variables,
            // configuration files, etc.

            switch (folder)
            {
                case SpecialFolder.UserProfile:
                case SpecialFolder.MyDocuments: // same value as Personal
                    return home;
                case SpecialFolder.ApplicationData:
                    return GetXdgConfig(home);
                case SpecialFolder.LocalApplicationData:
                    // "$XDG_DATA_HOME defines the base directory relative to which user specific data files should be stored."
                    // "If $XDG_DATA_HOME is either not set or empty, a default equal to $HOME/.local/share should be used."
                    string data = GetEnvironmentVariable("XDG_DATA_HOME");
                    if (string.IsNullOrEmpty(data) || data[0] != '/')
                    {
                        data = Path.Combine(home, ".local", "share");
                    }
                    return data;

                case SpecialFolder.Desktop:
                case SpecialFolder.DesktopDirectory:
                    return ReadXdgDirectory(home, "XDG_DESKTOP_DIR", "Desktop");
                case SpecialFolder.Templates:
                    return ReadXdgDirectory(home, "XDG_TEMPLATES_DIR", "Templates");
                case SpecialFolder.MyVideos:
                    return ReadXdgDirectory(home, "XDG_VIDEOS_DIR", "Videos");

                case SpecialFolder.MyMusic:
                    return IsMac ? Path.Combine(home, "Music") : ReadXdgDirectory(home, "XDG_MUSIC_DIR", "Music");
                case SpecialFolder.MyPictures:
                    return IsMac ? Path.Combine(home, "Pictures") : ReadXdgDirectory(home, "XDG_PICTURES_DIR", "Pictures");
                case SpecialFolder.Fonts:
                    return IsMac ? Path.Combine(home, "Library", "Fonts") : Path.Combine(home, ".fonts");

                case SpecialFolder.Favorites:
                    if (IsMac) return Path.Combine(home, "Library", "Favorites");
                    break;
                case SpecialFolder.InternetCache:
                    if (IsMac) return Path.Combine(home, "Library", "Caches");
                    break;
            }

            // No known path for the SpecialFolder
            return string.Empty;
        }

        private static string GetXdgConfig(string home)
        {
            // "$XDG_CONFIG_HOME defines the base directory relative to which user specific configuration files should be stored."
            // "If $XDG_CONFIG_HOME is either not set or empty, a default equal to $HOME/.config should be used."
            string config = GetEnvironmentVariable("XDG_CONFIG_HOME");
            if (string.IsNullOrEmpty(config) || config[0] != '/')
            {
                config = Path.Combine(home, ".config");
            }
            return config;
        }

        private static string ReadXdgDirectory(string homeDir, string key, string fallback)
        {
            Debug.Assert(!string.IsNullOrEmpty(homeDir), $"Expected non-empty homeDir");
            Debug.Assert(!string.IsNullOrEmpty(key), $"Expected non-empty key");
            Debug.Assert(!string.IsNullOrEmpty(fallback), $"Expected non-empty fallback");

            string envPath = GetEnvironmentVariable(key);
            if (!string.IsNullOrEmpty(envPath) && envPath[0] == '/')
            {
                return envPath;
            }

            // Use the user-dirs.dirs file to look up the right config.
            // Note that the docs also highlight a list of directories in which to look for this file:
            // "$XDG_CONFIG_DIRS defines the preference-ordered set of base directories to search for configuration files in addition
            //  to the $XDG_CONFIG_HOME base directory. The directories in $XDG_CONFIG_DIRS should be separated with a colon ':'. If
            //  $XDG_CONFIG_DIRS is either not set or empty, a value equal to / etc / xdg should be used."
            // For simplicity, we don't currently do that.  We can add it if/when necessary.

            string userDirsPath = Path.Combine(GetXdgConfig(homeDir), "user-dirs.dirs");
            if (Interop.Sys.Access(userDirsPath, Interop.Sys.AccessMode.R_OK) == 0)
            {
                try
                {
                    // TODO #11151: Replace with direct usage of File.ReadLines or equivalent once we have access to System.IO.FileSystem here.
                    Func<string, IEnumerable<string>> readLines = LazyInitializer.EnsureInitialized(ref s_fileReadLines, () =>
                    {
                        Type fileType = Type.GetType("System.IO.File, System.IO.FileSystem, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", throwOnError: false);
                        if (fileType != null)
                        {
                            foreach (MethodInfo mi in fileType.GetTypeInfo().GetDeclaredMethods("ReadLines"))
                            {
                                if (mi.GetParameters().Length == 1)
                                {
                                    return (Func<string, IEnumerable<string>>)mi.CreateDelegate(typeof(Func<string, IEnumerable<string>>));
                                }
                            }
                        }
                        return null;
                    });

                    IEnumerable<string> lines = readLines?.Invoke(userDirsPath);
                    if (lines != null)
                    {
                        foreach (string line in lines)
                        {
                            // Example lines:
                            // XDG_DESKTOP_DIR="$HOME/Desktop"
                            // XDG_PICTURES_DIR = "/absolute/path"

                            // Skip past whitespace at beginning of line
                            int pos = 0;
                            SkipWhitespace(line, ref pos);
                            if (pos >= line.Length) continue;

                            // Skip past requested key name
                            if (string.CompareOrdinal(line, pos, key, 0, key.Length) != 0) continue;
                            pos += key.Length;

                            // Skip past whitespace and past '='
                            SkipWhitespace(line, ref pos);
                            if (pos >= line.Length - 4 || line[pos] != '=') continue; // 4 for ="" and at least one char between quotes
                            pos++; // skip past '='

                            // Skip past whitespace and past first quote
                            SkipWhitespace(line, ref pos);
                            if (pos >= line.Length - 3 || line[pos] != '"') continue; // 3 for "" and at least one char between quotes
                            pos++; // skip past opening '"'

                            // Skip past relative prefix if one exists
                            bool relativeToHome = false;
                            const string RelativeToHomePrefix = "$HOME/";
                            if (string.CompareOrdinal(line, pos, RelativeToHomePrefix, 0, RelativeToHomePrefix.Length) == 0)
                            {
                                relativeToHome = true;
                                pos += RelativeToHomePrefix.Length;
                            }
                            else if (line[pos] != '/') // if not relative to home, must be absolute path
                            {
                                continue;
                            }

                            // Find end of path
                            int endPos = line.IndexOf('"', pos);
                            if (endPos <= pos) continue;

                            // Got we need.  Now extract it.
                            string path = line.Substring(pos, endPos - pos);
                            return relativeToHome ?
                                Path.Combine(homeDir, path) :
                                path;
                        }
                    }
                }
                catch (Exception exc)
                {
                    // assembly not found, file not found, errors reading file, etc. Just eat everything.
                    Debug.Fail($"Failed reading {userDirsPath}: {exc}");
                }
            }

            return Path.Combine(homeDir, fallback);
        }

        private static void SkipWhitespace(string line, ref int pos)
        {
            while (pos < line.Length && char.IsWhiteSpace(line[pos])) pos++;
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

            // For compatibility reasons with Mono, PlatformID.Unix is returned on MacOSX. PlatformID.MacOSX
            // is hidden from the editor and shouldn't be used.
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

        public static string SystemDirectory => GetFolderPathCore(SpecialFolder.System, SpecialFolderOption.None);

        public static int SystemPageSize => CheckedSysConf(Interop.Sys.SysConfName._SC_PAGESIZE);

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
