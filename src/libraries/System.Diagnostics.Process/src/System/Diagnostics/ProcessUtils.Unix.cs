// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security;
using System.Text;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace System.Diagnostics
{
    internal static partial class ProcessUtils
    {
        private static volatile bool s_initialized;
        private static readonly object s_initializedGate = new object();

        internal static bool SupportsAtomicNonInheritablePipeCreation => Interop.Sys.IsAtomicNonInheritablePipeCreationSupported;

        private static bool IsExecutable(string fullPath)
        {
            Interop.Sys.FileStatus fileinfo;

            if (Interop.Sys.Stat(fullPath, out fileinfo) < 0)
            {
                return false;
            }

            // Check if the path is a directory.
            if ((fileinfo.Mode & Interop.Sys.FileTypes.S_IFMT) == Interop.Sys.FileTypes.S_IFDIR)
            {
                return false;
            }

            const UnixFileMode AllExecute = UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute;

            UnixFileMode permissions = ((UnixFileMode)fileinfo.Mode) & AllExecute;

            // Avoid checking user/group when permission.
            if (permissions == AllExecute)
            {
                return true;
            }
            else if (permissions == 0)
            {
                return false;
            }

            uint euid = Interop.Sys.GetEUid();

            if (euid == 0)
            {
                return true; // We're root.
            }

            if (euid == fileinfo.Uid)
            {
                // We own the file.
                return (permissions & UnixFileMode.UserExecute) != 0;
            }

            bool groupCanExecute = (permissions & UnixFileMode.GroupExecute) != 0;
            bool otherCanExecute = (permissions & UnixFileMode.OtherExecute) != 0;

            // Avoid group check when group and other have same permissions.
            if (groupCanExecute == otherCanExecute)
            {
                return groupCanExecute;
            }

            if (Interop.Sys.IsMemberOfGroup(fileinfo.Gid))
            {
                return groupCanExecute;
            }
            else
            {
                return otherCanExecute;
            }
        }

        internal static unsafe void EnsureInitialized()
        {
            if (s_initialized)
            {
                return;
            }

            lock (s_initializedGate)
            {
                if (!s_initialized)
                {
                    if (!Interop.Sys.InitializeTerminalAndSignalHandling())
                    {
                        throw new Win32Exception();
                    }

                    // Register our callback.
                    Interop.Sys.RegisterForSigChld(&OnSigChild);
                    SetDelayedSigChildConsoleConfigurationHandler();

                    s_initialized = true;
                }
            }
        }

        internal static (uint userId, uint groupId, uint[] groups) GetUserAndGroupIds(ProcessStartInfo startInfo)
        {
            Debug.Assert(!string.IsNullOrEmpty(startInfo.UserName));

            (uint? userId, uint? groupId) = GetUserAndGroupIds(startInfo.UserName);

            Debug.Assert(userId.HasValue == groupId.HasValue, "userId and groupId both need to have values, or both need to be null.");
            if (!userId.HasValue)
            {
                throw new Win32Exception(SR.Format(SR.UserDoesNotExist, startInfo.UserName));
            }

            uint[]? groups = Interop.Sys.GetGroupList(startInfo.UserName, groupId!.Value);
            if (groups == null)
            {
                throw new Win32Exception(SR.Format(SR.UserGroupsCannotBeDetermined, startInfo.UserName));
            }

            return (userId.Value, groupId.Value, groups);
        }

        private static unsafe (uint? userId, uint? groupId) GetUserAndGroupIds(string userName)
        {
            Interop.Sys.Passwd? passwd;
            // First try with a buffer that should suffice for 99% of cases.
            // Note: on CentOS/RedHat 7.1 systems, getpwnam_r returns 'user not found' if the buffer is too small
            // see https://bugs.centos.org/view.php?id=7324
            const int BufLen = Interop.Sys.Passwd.InitialBufferSize;
            byte* stackBuf = stackalloc byte[BufLen];
            if (TryGetPasswd(userName, stackBuf, BufLen, out passwd))
            {
                if (passwd == null)
                {
                    return (null, null);
                }
                return (passwd.Value.UserId, passwd.Value.GroupId);
            }

            // Fallback to heap allocations if necessary, growing the buffer until
            // we succeed.  TryGetPasswd will throw if there's an unexpected error.
            int lastBufLen = BufLen;
            while (true)
            {
                lastBufLen *= 2;
                byte[] heapBuf = new byte[lastBufLen];
                fixed (byte* buf = &heapBuf[0])
                {
                    if (TryGetPasswd(userName, buf, heapBuf.Length, out passwd))
                    {
                        if (passwd == null)
                        {
                            return (null, null);
                        }
                        return (passwd.Value.UserId, passwd.Value.GroupId);
                    }
                }
            }
        }

        private static unsafe bool TryGetPasswd(string name, byte* buf, int bufLen, out Interop.Sys.Passwd? passwd)
        {
            // Call getpwnam_r to get the passwd struct
            Interop.Sys.Passwd tempPasswd;
            int error = Interop.Sys.GetPwNamR(name, out tempPasswd, buf, bufLen);

            // If the call succeeds, give back the passwd retrieved
            if (error == 0)
            {
                passwd = tempPasswd;
                return true;
            }

            // If the current user's entry could not be found, give back null,
            // but still return true as false indicates the buffer was too small.
            if (error == -1)
            {
                passwd = null;
                return true;
            }

            var errorInfo = new Interop.ErrorInfo(error);

            // If the call failed because the buffer was too small, return false to
            // indicate the caller should try again with a larger buffer.
            if (errorInfo.Error == Interop.Error.ERANGE)
            {
                passwd = null;
                return false;
            }

            // Otherwise, fail.
            throw new Win32Exception(errorInfo.RawErrno, errorInfo.GetErrorMessage());
        }

        internal static string? ResolveExecutableForShellExecute(string filename, string? workingDirectory)
        {
            // Determine if filename points to an executable file.
            // filename may be an absolute path, a relative path or a uri.

            string? resolvedFilename = null;
            // filename is an absolute path
            if (Path.IsPathRooted(filename))
            {
                if (File.Exists(filename))
                {
                    resolvedFilename = filename;
                }
            }
            // filename is a uri
            else if (Uri.TryCreate(filename, UriKind.Absolute, out Uri? uri))
            {
                if (uri.IsFile && uri.Host == "" && File.Exists(uri.LocalPath))
                {
                    resolvedFilename = uri.LocalPath;
                }
            }
            // filename is relative
            else
            {
                // The WorkingDirectory property specifies the location of the executable.
                // If WorkingDirectory is an empty string, the current directory is understood to contain the executable.
                workingDirectory = workingDirectory != null ? Path.GetFullPath(workingDirectory) :
                                                              Directory.GetCurrentDirectory();
                string filenameInWorkingDirectory = Path.Combine(workingDirectory, filename);
                // filename is a relative path in the working directory
                if (File.Exists(filenameInWorkingDirectory))
                {
                    resolvedFilename = filenameInWorkingDirectory;
                }
                // find filename on PATH
                else
                {
                    resolvedFilename = FindProgramInPath(filename);
                }
            }

            if (resolvedFilename == null)
            {
                return null;
            }

            if (Interop.Sys.Access(resolvedFilename, Interop.Sys.AccessMode.X_OK) == 0)
            {
                return resolvedFilename;
            }
            else
            {
                return null;
            }
        }

        [UnmanagedCallersOnly]
        private static int OnSigChild(int reapAll, int configureConsole)
        {
            // configureConsole is non zero when there are PosixSignalRegistrations that
            // may Cancel the terminal configuration that happens when there are no more
            // children using the terminal.
            // When the registrations don't cancel the terminal configuration,
            // DelayedSigChildConsoleConfiguration will be called.

            // Lock to avoid races with Process.Start
            s_processStartLock.EnterWriteLock();
            try
            {
                bool childrenUsingTerminalPre = AreChildrenUsingTerminal;
                ProcessWaitState.CheckChildren(reapAll != 0, configureConsole != 0);
                bool childrenUsingTerminalPost = AreChildrenUsingTerminal;

                // return whether console configuration was skipped.
                return childrenUsingTerminalPre && !childrenUsingTerminalPost && configureConsole == 0 ? 1 : 0;
            }
            finally
            {
                s_processStartLock.ExitWriteLock();
            }
        }

        /// <summary>Converts the filename and arguments information from a ProcessStartInfo into an argv array.</summary>
        /// <param name="psi">The ProcessStartInfo.</param>
        /// <param name="resolvedExe">Resolved executable to open ProcessStartInfo.FileName</param>
        /// <param name="ignoreArguments">Don't pass ProcessStartInfo.Arguments</param>
        /// <returns>The argv array.</returns>
        internal static string[] ParseArgv(ProcessStartInfo psi, string? resolvedExe = null, bool ignoreArguments = false)
        {
            if (string.IsNullOrEmpty(resolvedExe) &&
                (ignoreArguments || (string.IsNullOrEmpty(psi.Arguments) && !psi.HasArgumentList)))
            {
                return new string[] { psi.FileName };
            }

            var argvList = new List<string>();
            if (!string.IsNullOrEmpty(resolvedExe))
            {
                argvList.Add(resolvedExe);
                if (resolvedExe.Contains("kfmclient"))
                {
                    argvList.Add("openURL"); // kfmclient needs OpenURL
                }
            }

            argvList.Add(psi.FileName);

            if (!ignoreArguments)
            {
                if (!string.IsNullOrEmpty(psi.Arguments))
                {
                    ParseArgumentsIntoList(psi.Arguments, argvList);
                }
                else if (psi.HasArgumentList)
                {
                    argvList.AddRange(psi.ArgumentList);
                }
            }
            return argvList.ToArray();
        }

        /// <summary>Resolves a path to the filename passed to ProcessStartInfo. </summary>
        /// <param name="filename">The filename.</param>
        /// <returns>The resolved path. It can return null in case of URLs.</returns>
        internal static string? ResolvePath(string filename)
        {
            // Follow the same resolution that Windows uses with CreateProcess:
            // 1. First try the exact path provided
            // 2. Then try the file relative to the executable directory
            // 3. Then try the file relative to the current directory
            // 4. then try the file in each of the directories specified in PATH
            // Windows does additional Windows-specific steps between 3 and 4,
            // and we ignore those here.

            // If the filename is a complete path, use it, regardless of whether it exists.
            if (Path.IsPathRooted(filename))
            {
                // In this case, it doesn't matter whether the file exists or not;
                // it's what the caller asked for, so it's what they'll get
                return filename;
            }

            // Then check the executable's directory
            string? path = Environment.ProcessPath;
            if (path != null)
            {
                try
                {
                    path = Path.Combine(Path.GetDirectoryName(path)!, filename);
                    if (File.Exists(path))
                    {
                        return path;
                    }
                }
                catch (ArgumentException) { } // ignore any errors in data that may come from the exe path
            }

            // Then check the current directory
            path = Path.Combine(Directory.GetCurrentDirectory(), filename);
            if (File.Exists(path))
            {
                return path;
            }

            // Then check each directory listed in the PATH environment variables
            return FindProgramInPath(filename);
        }

        /// <summary>Parses a command-line argument string into a list of arguments.</summary>
        /// <param name="arguments">The argument string.</param>
        /// <param name="results">The list into which the component arguments should be stored.</param>
        /// <remarks>
        /// This follows the rules outlined in "Parsing C++ Command-Line Arguments" at
        /// https://msdn.microsoft.com/en-us/library/17w5ykft.aspx.
        /// </remarks>
        private static void ParseArgumentsIntoList(string arguments, List<string> results)
        {
            // Iterate through all of the characters in the argument string.
            for (int i = 0; i < arguments.Length; i++)
            {
                while (i < arguments.Length && (arguments[i] == ' ' || arguments[i] == '\t'))
                    i++;

                if (i == arguments.Length)
                    break;

                results.Add(GetNextArgument(arguments, ref i));
            }
        }

        private static string GetNextArgument(string arguments, ref int i)
        {
            var currentArgument = new ValueStringBuilder(stackalloc char[256]);
            bool inQuotes = false;

            while (i < arguments.Length)
            {
                // From the current position, iterate through contiguous backslashes.
                int backslashCount = 0;
                while (i < arguments.Length && arguments[i] == '\\')
                {
                    i++;
                    backslashCount++;
                }

                if (backslashCount > 0)
                {
                    if (i >= arguments.Length || arguments[i] != '"')
                    {
                        // Backslashes not followed by a double quote:
                        // they should all be treated as literal backslashes.
                        currentArgument.Append('\\', backslashCount);
                    }
                    else
                    {
                        // Backslashes followed by a double quote:
                        // - Output a literal slash for each complete pair of slashes
                        // - If one remains, use it to make the subsequent quote a literal.
                        currentArgument.Append('\\', backslashCount / 2);
                        if (backslashCount % 2 != 0)
                        {
                            currentArgument.Append('"');
                            i++;
                        }
                    }

                    continue;
                }

                char c = arguments[i];

                // If this is a double quote, track whether we're inside of quotes or not.
                // Anything within quotes will be treated as a single argument, even if
                // it contains spaces.
                if (c == '"')
                {
                    if (inQuotes && i < arguments.Length - 1 && arguments[i + 1] == '"')
                    {
                        // Two consecutive double quotes inside an inQuotes region should result in a literal double quote
                        // (the parser is left in the inQuotes region).
                        // This behavior is not part of the spec of code:ParseArgumentsIntoList, but is compatible with CRT
                        // and .NET Framework.
                        currentArgument.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }

                    i++;
                    continue;
                }

                // If this is a space/tab and we're not in quotes, we're done with the current
                // argument, it should be added to the results and then reset for the next one.
                if ((c == ' ' || c == '\t') && !inQuotes)
                {
                    break;
                }

                // Nothing special; add the character to the current argument.
                currentArgument.Append(c);
                i++;
            }

            return currentArgument.ToString();
        }
    }
}
