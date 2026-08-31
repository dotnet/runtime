// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
#if NETFRAMEWORK
using FileInfo = Microsoft.IO.FileInfo;
#endif


namespace Microsoft.NET.HostModel
{
    internal static class HostModelUtils
    {
#if NET
        // -rwxr-xr-x: the permissions an apphost or single-file bundle should have.
        private const UnixFileMode AppHostFileMode =
            UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
            UnixFileMode.GroupRead | UnixFileMode.GroupExecute |
            UnixFileMode.OtherRead | UnixFileMode.OtherExecute;
#endif

        /// <summary>
        /// Creates a <see cref="FileStream"/> for writing an apphost or bundle, requesting the desired
        /// Unix permissions at creation time on platforms that support them.
        /// When <paramref name="bufferSize"/> is <see langword="null"/>, the default buffer size is used.
        /// </summary>
        public static FileStream CreateFileStreamForHost(
            string path,
            FileAccess access,
            FileShare share,
            int? bufferSize = null)
        {
#if NET
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                FileStreamOptions options = new()
                {
                    Mode = FileMode.Create,
                    Access = access,
                    Share = share,
                    UnixCreateMode = AppHostFileMode,
                };

                if (bufferSize is int size)
                {
                    options.BufferSize = size;
                }

                return new FileStream(path, options);
            }
#endif
            return bufferSize.HasValue
                ? new FileStream(path, FileMode.Create, access, share, bufferSize.Value)
                : new FileStream(path, FileMode.Create, access, share);
        }

        /// <summary>
        /// Ensures a host file has the required permissions.
        /// </summary>
        public static void SetPermissionsForHost(string filePath)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return;

#if NET
            // File already has required permissions
            if ((File.GetUnixFileMode(filePath) & AppHostFileMode) == AppHostFileMode)
                return;

            File.SetUnixFileMode(filePath, AppHostFileMode);
#endif
        }

        private const string CodesignPath = @"/usr/bin/codesign";

        public static bool IsCodesignAvailable() => File.Exists(CodesignPath);

        public static (int ExitCode, string StdErr) RunCodesign(string args, string appHostPath)
        {
            Debug.Assert(RuntimeInformation.IsOSPlatform(OSPlatform.OSX));
            Debug.Assert(IsCodesignAvailable());

            var psi = new ProcessStartInfo()
            {
                Arguments = $"{args} \"{appHostPath}\"",
                FileName = CodesignPath,
                RedirectStandardError = true,
                UseShellExecute = false,
            };

            using (var p = Process.Start(psi))
            {
                p.WaitForExit();
                return (p.ExitCode, p.StandardError.ReadToEnd());
            }
        }

        public static long GetFileLength(string path)
        {
            var info = new FileInfo(path);
            return ((FileInfo)info.ResolveLinkTarget(true) ?? info).Length;
        }
    }
}
