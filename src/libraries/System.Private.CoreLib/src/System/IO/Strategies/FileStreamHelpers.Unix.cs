// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO.Strategies
{
    // this type defines a set of stateless FileStream/FileStreamStrategy helper methods
    internal static partial class FileStreamHelpers
    {
        // in the future we are most probably going to introduce more strategies (io_uring etc)
        private static FileStreamStrategy ChooseStrategyCore(SafeFileHandle handle, FileAccess access, FileShare share, int bufferSize, bool isAsync)
            => new Net5CompatFileStreamStrategy(handle, access, bufferSize, isAsync);

        private static FileStreamStrategy ChooseStrategyCore(string path, FileMode mode, FileAccess access, FileShare share, int bufferSize, FileOptions options, long preallocationSize)
            => new Net5CompatFileStreamStrategy(path, mode, access, share, bufferSize, options, preallocationSize);

        internal static SafeFileHandle OpenHandle(string path, FileMode mode, FileAccess access, FileShare share, FileOptions options, long preallocationSize)
        {
            // Translate the arguments into arguments for an open call.
            Interop.Sys.OpenFlags openFlags = PreOpenConfigurationFromOptions(mode, access, share, options);

            // If the file gets created a new, we'll select the permissions for it.  Most Unix utilities by default use 666 (read and
            // write for all), so we do the same (even though this doesn't match Windows, where by default it's possible to write out
            // a file and then execute it). No matter what we choose, it'll be subject to the umask applied by the system, such that the
            // actual permissions will typically be less than what we select here.
            const Interop.Sys.Permissions OpenPermissions =
                Interop.Sys.Permissions.S_IRUSR | Interop.Sys.Permissions.S_IWUSR |
                Interop.Sys.Permissions.S_IRGRP | Interop.Sys.Permissions.S_IWGRP |
                Interop.Sys.Permissions.S_IROTH | Interop.Sys.Permissions.S_IWOTH;

            return SafeFileHandle.Open(path!, openFlags, (int)OpenPermissions);
        }

        internal static bool GetDefaultIsAsync(SafeFileHandle handle, bool defaultIsAsync) => handle.IsAsync ?? defaultIsAsync;

        /// <summary>Translates the FileMode, FileAccess, and FileOptions values into flags to be passed when opening the file.</summary>
        /// <param name="mode">The FileMode provided to the stream's constructor.</param>
        /// <param name="access">The FileAccess provided to the stream's constructor</param>
        /// <param name="share">The FileShare provided to the stream's constructor</param>
        /// <param name="options">The FileOptions provided to the stream's constructor</param>
        /// <returns>The flags value to be passed to the open system call.</returns>
        private static Interop.Sys.OpenFlags PreOpenConfigurationFromOptions(FileMode mode, FileAccess access, FileShare share, FileOptions options)
        {
            // Translate FileMode.  Most of the values map cleanly to one or more options for open.
            Interop.Sys.OpenFlags flags = default;
            switch (mode)
            {
                default:
                case FileMode.Open: // Open maps to the default behavior for open(...).  No flags needed.
                case FileMode.Truncate: // We truncate the file after getting the lock
                    break;

                case FileMode.Append: // Append is the same as OpenOrCreate, except that we'll also separately jump to the end later
                case FileMode.OpenOrCreate:
                case FileMode.Create: // We truncate the file after getting the lock
                    flags |= Interop.Sys.OpenFlags.O_CREAT;
                    break;

                case FileMode.CreateNew:
                    flags |= (Interop.Sys.OpenFlags.O_CREAT | Interop.Sys.OpenFlags.O_EXCL);
                    break;
            }

            // Translate FileAccess.  All possible values map cleanly to corresponding values for open.
            switch (access)
            {
                case FileAccess.Read:
                    flags |= Interop.Sys.OpenFlags.O_RDONLY;
                    break;

                case FileAccess.ReadWrite:
                    flags |= Interop.Sys.OpenFlags.O_RDWR;
                    break;

                case FileAccess.Write:
                    flags |= Interop.Sys.OpenFlags.O_WRONLY;
                    break;
            }

            // Handle Inheritable, other FileShare flags are handled by Init
            if ((share & FileShare.Inheritable) == 0)
            {
                flags |= Interop.Sys.OpenFlags.O_CLOEXEC;
            }

            // Translate some FileOptions; some just aren't supported, and others will be handled after calling open.
            // - Asynchronous: Handled in ctor, setting _useAsync and SafeFileHandle.IsAsync to true
            // - DeleteOnClose: Doesn't have a Unix equivalent, but we approximate it in Dispose
            // - Encrypted: No equivalent on Unix and is ignored
            // - RandomAccess: Implemented after open if posix_fadvise is available
            // - SequentialScan: Implemented after open if posix_fadvise is available
            // - WriteThrough: Handled here
            if ((options & FileOptions.WriteThrough) != 0)
            {
                flags |= Interop.Sys.OpenFlags.O_SYNC;
            }

            return flags;
        }
    }
}
