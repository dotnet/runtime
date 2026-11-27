// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.ComponentModel;
using System.IO;
using System.Runtime.Versioning;
using System.Text;
using System.Threading;

namespace System.Diagnostics
{
    public partial class Process : IDisposable
    {
        /// <summary>Computes a time based on a number of ticks since boot.</summary>
        /// <param name="timespanAfterBoot">The timespan since boot.</param>
        /// <returns>The converted time.</returns>
        internal static DateTime BootTimeToDateTime(TimeSpan timespanAfterBoot)
        {
            // And use that to determine the absolute time for timespan.
            DateTime dt = BootTime + timespanAfterBoot;

            // The return value is expected to be in the local time zone.
            // It is converted here (rather than starting with DateTime.Now) to avoid DST issues.
            return dt.ToLocalTime();
        }

        private static long s_bootTimeTicks;
        /// <summary>Gets the system boot time.</summary>
        private static DateTime BootTime
        {
            get
            {
               long bootTimeTicks = Interlocked.Read(ref s_bootTimeTicks);
               if (bootTimeTicks == 0)
               {
                    bootTimeTicks = Interop.Sys.GetBootTimeTicks();
                    long oldValue = Interlocked.CompareExchange(ref s_bootTimeTicks, bootTimeTicks, 0);
                    if (oldValue != 0) // a different thread has managed to update the ticks first
                    {
                        bootTimeTicks = oldValue; // consistency
                    }
               }
               return new DateTime(bootTimeTicks);
           }
       }

        /// <summary>Counts the entries in a directory, excluding "." and "..", without stat'ing them.</summary>
        /// <returns>The number of entries, or -1 if the directory does not exist (for example, because the process exited).</returns>
        private static unsafe int CountDirectoryEntries(string path, bool excludeOwnDescriptor)
        {
            IntPtr dir = Interop.Sys.OpenDir(path);
            if (dir == IntPtr.Zero)
            {
                return HandleDirectoryError(Interop.Sys.GetLastErrorInfo(), path);
            }

            try
            {
                int count = 0;
                Interop.Sys.DirectoryEntry entry;
                int result;
                while ((result = Interop.Sys.ReadDir(dir, &entry)) == 0)
                {
                    // Skip the "." and ".." entries. Name points at the NUL-terminated d_name of the dirent, and a name
                    // cannot be empty, so name[0] can always be read. Each subsequent byte is only read once the one
                    // before it is known not to be the terminator (name[1] only if name[0] is '.', and name[2] only
                    // if name[1] is also '.'), so this never reads past the end of the name.
                    byte* name = entry.Name;
                    if (name[0] == (byte)'.' && (name[1] == 0 || (name[1] == (byte)'.' && name[2] == 0)))
                    {
                        continue;
                    }

                    count++;
                }

                // ReadDir returns -1 at the end of the directory, and an errno value if the enumeration failed.
                // Don't report a partial count in the latter case.
                if (result != -1)
                {
                    return HandleDirectoryError(new Interop.ErrorInfo(result), path);
                }

                return excludeOwnDescriptor ? count - 1 : count;
            }
            finally
            {
                Interop.Sys.CloseDir(dir);
            }

            // Returns -1 if the directory no longer exists (for example, because the process exited), otherwise throws.
            static int HandleDirectoryError(Interop.ErrorInfo error, string path)
            {
                if (error.Error is Interop.Error.ENOENT or Interop.Error.ESRCH)
                {
                    return -1;
                }

                // Directory.GetFiles threw UnauthorizedAccessException when the directory could not be read.
                if (error.Error is Interop.Error.EACCES or Interop.Error.EBADF or Interop.Error.EPERM)
                {
                    throw new UnauthorizedAccessException(error.GetErrorMessage(), new Win32Exception(error.RawErrno));
                }

                throw new IOException($"{error.GetErrorMessage()} : '{path}'", error.RawErrno);
            }
        }

        /// <summary>Gets the path to the executable for the process, or null if it could not be retrieved.</summary>
        /// <param name="procPid">The pid for the target process.</param>
        internal static string? GetExePath(Interop.procfs.ProcPid procPid)
        {
            return procPid == Interop.procfs.ProcPid.Self ? Environment.ProcessPath :
                Interop.Sys.ReadLink(Interop.procfs.GetExeFilePathForProcess(procPid));
        }

        /// <summary>Gets the name that was used to start the process, or null if it could not be retrieved.</summary>
        /// <param name="procPid">The pid for the target process.</param>
        /// <param name="stat">The stat for the target process.</param>
        internal static unsafe string GetUntruncatedProcessName(Interop.procfs.ProcPid procPid, ref Interop.procfs.ParsedStat stat)
        {
            string cmdLineFilePath = Interop.procfs.GetCmdLinePathForProcess(procPid);

            byte[]? rentedArray = null;
            try
            {
                // bufferSize == 1 used to avoid unnecessary buffer in FileStream
                using (var fs = new FileStream(cmdLineFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 1, useAsync: false))
                {
                    Span<byte> buffer = stackalloc byte[512];
                    int bytesRead = 0;
                    while (true)
                    {
                        // Resize buffer if it was too small.
                        if (bytesRead == buffer.Length)
                        {
                            uint newLength = (uint)buffer.Length * 2;

                            byte[] tmp = ArrayPool<byte>.Shared.Rent((int)newLength);
                            buffer.CopyTo(tmp);
                            byte[]? toReturn = rentedArray;
                            buffer = rentedArray = tmp;
                            if (toReturn != null)
                            {
                                ArrayPool<byte>.Shared.Return(toReturn);
                            }
                        }

                        Debug.Assert(bytesRead < buffer.Length);
                        int n = fs.Read(buffer.Slice(bytesRead));
                        bytesRead += n;

                        // cmdline contains the argv array separated by '\0' bytes.
                        // stat.comm contains a possibly truncated version of the process name.
                        // When the program is a native executable, the process name will be in argv[0].
                        // When the program is a script, argv[0] contains the interpreter, and argv[1] contains the script name.
                        ReadOnlySpan<byte> argRemainder = buffer.Slice(0, bytesRead);
                        int argEnd = argRemainder.IndexOf((byte)'\0');
                        if (argEnd != -1)
                        {
                            // Check if argv[0] has the process name.
                            string? name = GetUntruncatedNameFromArg(argRemainder.Slice(0, argEnd), prefix: stat.comm);
                            if (name != null)
                            {
                                return name;
                            }

                            // Check if argv[1] has the process name.
                            argRemainder = argRemainder.Slice(argEnd + 1);
                            argEnd = argRemainder.IndexOf((byte)'\0');
                            if (argEnd != -1)
                            {
                                name = GetUntruncatedNameFromArg(argRemainder.Slice(0, argEnd), prefix: stat.comm);
                                return name ?? stat.comm;
                            }
                        }

                        if (n == 0)
                        {
                            return stat.comm;
                        }
                    }
                }
            }
            catch (IOException)
            {
                return stat.comm;
            }
            finally
            {
                if (rentedArray != null)
                {
                    ArrayPool<byte>.Shared.Return(rentedArray);
                }
            }

            static string? GetUntruncatedNameFromArg(ReadOnlySpan<byte> arg, string prefix)
            {
                // Strip directory names from arg.
                int nameStart = arg.LastIndexOf((byte)'/') + 1;
                string argString = Encoding.UTF8.GetString(arg.Slice(nameStart));

                if (argString.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    return argString;
                }
                else
                {
                    return null;
                }
            }
        }

        // ----------------------------------
        // ---- Unix PAL layer ends here ----
        // ----------------------------------

        /// <summary>Reads the stats information for this process from the procfs file system.</summary>
        private Interop.procfs.ParsedStat GetStat()
        {
            EnsureState(State.HaveNonExitedId);
            Interop.procfs.ParsedStat stat;
            if (!ProcessManager.TryReadStatFile(_processId, out stat))
            {
                throw new Win32Exception(SR.ProcessInformationUnavailable);
            }
            return stat;
        }
    }
}
