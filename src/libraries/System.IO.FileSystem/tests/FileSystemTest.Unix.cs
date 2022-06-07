// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Xunit;

namespace System.IO.Tests
{
    public abstract partial class FileSystemTest
    {
        [LibraryImport("libc", SetLastError = true)]
        protected static partial int geteuid();

        [LibraryImport("libc", StringMarshalling = StringMarshalling.Utf8, SetLastError = true)]
        protected static partial int mkfifo(string path, int mode);

        internal const UnixFileMode AllAccess =
                UnixFileMode.UserRead |
                UnixFileMode.UserWrite |
                UnixFileMode.UserExecute |
                UnixFileMode.GroupRead |
                UnixFileMode.GroupWrite |
                UnixFileMode.GroupExecute |
                UnixFileMode.OtherRead |
                UnixFileMode.OtherWrite |
                UnixFileMode.OtherExecute;

        public static IEnumerable<object[]> TestUnixFileModes
        {
            get
            {
                // Make combinations of the enum with 0, 1 and 2 bits set.
                UnixFileMode[] modes = Enum.GetValues<UnixFileMode>();
                for (int i = 0; i < modes.Length; i++)
                {
                    for (int j = i; j < modes.Length; j++)
                    {
                        yield return new object[] { modes[i] | modes[j] };
                    }
                }
            }
        }

        private static UnixFileMode s_umask = (UnixFileMode)(-1);

        protected static UnixFileMode GetUmask()
        {
            if (s_umask == (UnixFileMode)(-1))
            {
                // The umask can't be retrieved without changing it.
                // We launch a child process to get its value.
                using Process px = Process.Start(new ProcessStartInfo
                {
                    FileName = "umask",
                    RedirectStandardOutput = true
                });
                string stdout = px.StandardOutput.ReadToEnd().Trim();
                s_umask = (UnixFileMode)Convert.ToInt32(stdout, 8);
            }
            return s_umask;
        }
    }
}
