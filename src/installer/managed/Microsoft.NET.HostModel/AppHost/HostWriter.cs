// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Text;

namespace Microsoft.NET.HostModel.AppHost
{
    /// <summary>
    /// Embeds the App Name into the AppHost.exe
    /// If an apphost is a single-file bundle, updates the location of the bundle headers.
    /// </summary>
    public static class HostWriter
    {
        /// <summary>
        /// hash value embedded in default apphost executable in a place where the path to the app binary should be stored.
        /// </summary>
        private const string AppBinaryPathPlaceholder = "c3ab8ff13720e8ad9047dd39466b3c8974e592c2fa383d4a3960714caef0c4f2";
        private static readonly byte[] AppBinaryPathPlaceholderSearchValue = Encoding.UTF8.GetBytes(AppBinaryPathPlaceholder);

        /// <summary>
        /// Create an AppHost with embedded configuration of app binary location
        /// </summary>
        /// <param name="appHostSourceFilePath">The path of Apphost template, which has the place holder</param>
        /// <param name="appHostDestinationFilePath">The destination path for desired location to place, including the file name</param>
        /// <param name="appBinaryFilePath">Full path to app binary or relative path to the result apphost file</param>
        /// <param name="windowsGraphicalUserInterface">Specify whether to set the subsystem to GUI. Only valid for PE apphosts.</param>
        /// <param name="assemblyToCopyResorcesFrom">Path to the intermediate assembly, used for copying resources to PE apphosts.</param>
        /// <param name="enableMacOSCodeSign">Sign the app binary using codesign with an anonymous certificate.</param>
        public static void CreateAppHost(
            string appHostSourceFilePath,
            string appHostDestinationFilePath,
            string appBinaryFilePath,
            bool windowsGraphicalUserInterface = false,
            string assemblyToCopyResorcesFrom = null,
            bool enableMacOSCodeSign = false)
        {
            var bytesToWrite = Encoding.UTF8.GetBytes(appBinaryFilePath);
            if (bytesToWrite.Length > 1024)
            {
                throw new AppNameTooLongException(appBinaryFilePath);
            }

            bool appHostIsPEImage = false;

            void RewriteAppHost(MemoryMappedViewAccessor accessor)
            {
                // Re-write the destination apphost with the proper contents.
                BinaryUtils.SearchAndReplace(accessor, AppBinaryPathPlaceholderSearchValue, bytesToWrite);

                appHostIsPEImage = PEUtils.IsPEImage(accessor);

                if (windowsGraphicalUserInterface)
                {
                    if (!appHostIsPEImage)
                    {
                        throw new AppHostNotPEFileException();
                    }

                    PEUtils.SetWindowsGraphicalUserInterfaceBit(accessor);
                }
            }

            void UpdateResources()
            {
                if (assemblyToCopyResorcesFrom != null && appHostIsPEImage)
                {
                    if (ResourceUpdater.IsSupportedOS())
                    {
                        // Copy resources from managed dll to the apphost
                        new ResourceUpdater(appHostDestinationFilePath)
                            .AddResourcesFromPEImage(assemblyToCopyResorcesFrom)
                            .Update();
                    }
                    else
                    {
                        throw new AppHostCustomizationUnsupportedOSException();
                    }
                }
            }

            try
            {
                RetryUtil.RetryOnIOError(() =>
                {
                    FileStream appHostSourceStream = null;
                    MemoryMappedFile memoryMappedFile = null;
                    MemoryMappedViewAccessor memoryMappedViewAccessor = null;
                    try
                    {
                        // Open the source host file.
                        appHostSourceStream = new FileStream(appHostSourceFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                        memoryMappedFile = MemoryMappedFile.CreateFromFile(appHostSourceStream, null, 0, MemoryMappedFileAccess.Read, HandleInheritability.None, true);
                        memoryMappedViewAccessor = memoryMappedFile.CreateViewAccessor(0, 0, MemoryMappedFileAccess.CopyOnWrite);

                        // Get the size of the source app host to ensure that we don't write extra data to the destination.
                        // On Windows, the size of the view accessor is rounded up to the next page boundary.
                        long sourceAppHostLength = appHostSourceStream.Length;

                        // Transform the host file in-memory.
                        RewriteAppHost(memoryMappedViewAccessor);

                        // Save the transformed host.
                        using (FileStream fileStream = new FileStream(appHostDestinationFilePath, FileMode.Create))
                        {
                            BinaryUtils.WriteToStream(memoryMappedViewAccessor, fileStream, sourceAppHostLength);

                            // Remove the signature from MachO hosts.
                            if (!appHostIsPEImage)
                            {
                                MachOUtils.RemoveSignature(fileStream);
                            }
                        }
                    }
                    finally
                    {
                        memoryMappedViewAccessor?.Dispose();
                        memoryMappedFile?.Dispose();
                        appHostSourceStream?.Dispose();
                    }
                });

                RetryUtil.RetryOnWin32Error(UpdateResources);

                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    var filePermissionOctal = Convert.ToInt32("755", 8); // -rwxr-xr-x
                    const int EINTR = 4;
                    int chmodReturnCode = 0;

                    do
                    {
                        chmodReturnCode = chmod(appHostDestinationFilePath, filePermissionOctal);
                    }
                    while (chmodReturnCode == -1 && Marshal.GetLastWin32Error() == EINTR);

                    if (chmodReturnCode == -1)
                    {
                        throw new Win32Exception(Marshal.GetLastWin32Error(), $"Could not set file permission {filePermissionOctal} for {appHostDestinationFilePath}.");
                    }

                    if (enableMacOSCodeSign && RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                        CodeSign(appHostDestinationFilePath);
                }
            }
            catch (Exception ex)
            {
                // Delete the destination file so we don't leave an unmodified apphost
                try
                {
                    File.Delete(appHostDestinationFilePath);
                }
                catch (Exception failedToDeleteEx)
                {
                    throw new AggregateException(ex, failedToDeleteEx);
                }

                throw;
            }
        }

        /// <summary>
        /// Set the current AppHost as a single-file bundle.
        /// </summary>
        /// <param name="appHostPath">The path of Apphost template, which has the place holder</param>
        /// <param name="bundleHeaderOffset">The offset to the location of bundle header</param>
        public static void SetAsBundle(
            string appHostPath,
            long bundleHeaderOffset)
        {
            byte[] bundleHeaderPlaceholder = {
                // 8 bytes represent the bundle header-offset
                // Zero for non-bundle apphosts (default).
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                // 32 bytes represent the bundle signature: SHA-256 for ".net core bundle"
                0x8b, 0x12, 0x02, 0xb9, 0x6a, 0x61, 0x20, 0x38,
                0x72, 0x7b, 0x93, 0x02, 0x14, 0xd7, 0xa0, 0x32,
                0x13, 0xf5, 0xb9, 0xe6, 0xef, 0xae, 0x33, 0x18,
                0xee, 0x3b, 0x2d, 0xce, 0x24, 0xb3, 0x6a, 0xae
            };

            // Re-write the destination apphost with the proper contents.
            RetryUtil.RetryOnIOError(() =>
                BinaryUtils.SearchAndReplace(appHostPath,
                                             bundleHeaderPlaceholder,
                                             BitConverter.GetBytes(bundleHeaderOffset),
                                             pad0s: false));

            RetryUtil.RetryOnIOError(() =>
                MachOUtils.AdjustHeadersForBundle(appHostPath));

            // Memory-mapped write does not updating last write time
            RetryUtil.RetryOnIOError(() =>
                File.SetLastWriteTimeUtc(appHostPath, DateTime.UtcNow));
        }

        /// <summary>
        /// Check if the an AppHost is a single-file bundle
        /// </summary>
        /// <param name="appHostFilePath">The path of Apphost to check</param>
        /// <param name="bundleHeaderOffset">An out parameter containing the offset of the bundle header (if any)</param>
        /// <returns>True if the AppHost is a single-file bundle, false otherwise</returns>
        public static bool IsBundle(string appHostFilePath, out long bundleHeaderOffset)
        {
            byte[] bundleSignature = {
                // 32 bytes represent the bundle signature: SHA-256 for ".net core bundle"
                0x8b, 0x12, 0x02, 0xb9, 0x6a, 0x61, 0x20, 0x38,
                0x72, 0x7b, 0x93, 0x02, 0x14, 0xd7, 0xa0, 0x32,
                0x13, 0xf5, 0xb9, 0xe6, 0xef, 0xae, 0x33, 0x18,
                0xee, 0x3b, 0x2d, 0xce, 0x24, 0xb3, 0x6a, 0xae
            };

            long headerOffset = 0;
            void FindBundleHeader()
            {
                using (var memoryMappedFile = MemoryMappedFile.CreateFromFile(appHostFilePath))
                {
                    using (MemoryMappedViewAccessor accessor = memoryMappedFile.CreateViewAccessor())
                    {
                        int position = BinaryUtils.SearchInFile(accessor, bundleSignature);
                        if (position == -1)
                        {
                            throw new PlaceHolderNotFoundInAppHostException(bundleSignature);
                        }

                        headerOffset = accessor.ReadInt64(position - sizeof(long));
                    }
                }
            }

            RetryUtil.RetryOnIOError(FindBundleHeader);
            bundleHeaderOffset = headerOffset;

            return headerOffset != 0;
        }

        private static void CodeSign(string appHostPath)
        {
            Debug.Assert(RuntimeInformation.IsOSPlatform(OSPlatform.OSX));
            const string codesign = @"/usr/bin/codesign";
            if (!File.Exists(codesign))
                return;

            var psi = new ProcessStartInfo()
            {
                Arguments = $"-s - \"{appHostPath}\"",
                FileName = codesign,
                RedirectStandardError = true,
                UseShellExecute = false,
            };

            using (var p = Process.Start(psi))
            {
                p.WaitForExit();
                if (p.ExitCode != 0)
                    throw new AppHostSigningException(p.ExitCode, p.StandardError.ReadToEnd());
            }
        }

        [DllImport("libc", SetLastError = true)]
        private static extern int chmod(string pathname, int mode);
    }
}
