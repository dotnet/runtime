// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Text;

namespace System.IO
{
    public static partial class Directory
    {
#pragma warning disable IDE0060
        private static DirectoryInfo CreateDirectoryCore(string path, UnixFileMode unixCreateMode)
            => throw new PlatformNotSupportedException(SR.PlatformNotSupported_UnixFileMode);
#pragma warning restore IDE0060

        private static unsafe string CreateTempSubdirectoryCore(string? prefix)
        {
            ValueStringBuilder builder = new ValueStringBuilder(stackalloc char[PathInternal.MaxShortPath]);
            Path.GetTempPath(ref builder);

            // ensure the base TEMP directory exists
            CreateDirectory(PathHelper.Normalize(ref builder));

            builder.Append(prefix);

            const int RandomFileNameLength = 12; // 12 == 8 + 1 (for period) + 3
            int initialTempPathLength = builder.Length;
            builder.EnsureCapacity(initialTempPathLength + RandomFileNameLength);

            // For generating random file names
            // 8 random bytes provides 12 chars in our encoding for the 8.3 name.
            const int RandomKeyLength = 8;
            byte* pKey = stackalloc byte[RandomKeyLength];

            // to avoid an infinite loop, only try as many as GetTempFileNameW will create
            const int MaxAttempts = ushort.MaxValue;
            int attempts = 0;
            while (attempts < MaxAttempts)
            {
                // simulate a call to Path.GetRandomFileName() without allocating an intermediate string
                Interop.GetRandomBytes(pKey, RandomKeyLength);
                Path.Populate83FileNameFromRandomBytes(pKey, RandomKeyLength, builder.RawChars.Slice(builder.Length, RandomFileNameLength));
                builder.Length += RandomFileNameLength;

                string path = PathHelper.Normalize(ref builder);

                bool directoryCreated = Interop.Kernel32.CreateDirectory(path, null);
                if (!directoryCreated)
                {
                    // in the off-chance that the directory already exists, try again
                    int error = Marshal.GetLastPInvokeError();
                    if (error == Interop.Errors.ERROR_ALREADY_EXISTS)
                    {
                        builder.Length = initialTempPathLength;
                        attempts++;
                        continue;
                    }

                    throw Win32Marshal.GetExceptionForWin32Error(error, path);
                }

                builder.Dispose();
                return path;
            }

            throw new IOException(SR.IO_MaxAttemptsReached);
        }
    }
}
