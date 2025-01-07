// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.NET.HostModel.MachO;

namespace Microsoft.NET.HostModel.AppHost
{
    /// <summary>
    /// Embeds the App Name into the AppHost.exe
    /// If an apphost is a single-file bundle, updates the location of the bundle headers.
    /// </summary>
    public static partial class HostWriter
    {
        /// <summary>
        /// hash value embedded in default apphost executable in a place where the path to the app binary should be stored.
        /// </summary>
        private const string AppBinaryPathPlaceholder = "c3ab8ff13720e8ad9047dd39466b3c8974e592c2fa383d4a3960714caef0c4f2";
        private static readonly byte[] AppBinaryPathPlaceholderSearchValue = Encoding.UTF8.GetBytes(AppBinaryPathPlaceholder);

        // See placeholder array in corehost.cpp
        private const int MaxAppBinaryPathSizeInBytes = 1024;

        /// <summary>
        /// Value embedded in default apphost executable for configuration of how it will search for the .NET install
        /// </summary>
        private const string DotNetSearchPlaceholder = "\0\019ff3e9c3602ae8e841925bb461a0adb064a1f1903667a5e0d87e8f608f425ac";
        private static readonly byte[] DotNetSearchPlaceholderSearchValue = Encoding.UTF8.GetBytes(DotNetSearchPlaceholder);

        // See placeholder array in hostfxr_resolver.cpp
        private const int MaxDotNetSearchSizeInBytes = 512;
        private const int MaxAppRelativeDotNetSizeInBytes = MaxDotNetSearchSizeInBytes - 3; // -2 for search location + null, -1 for null terminator

        public class DotNetSearchOptions
        {
            // Keep in sync with fxr_resolver::search_location in fxr_resolver.h
            [Flags]
            public enum SearchLocation : byte
            {
                Default,
                AppLocal = 1 << 0,
                AppRelative = 1 << 1,
                EnvironmentVariable = 1 << 2,
                Global = 1 << 3,
            }

            public SearchLocation Location { get; set; } = SearchLocation.Default;
            public string AppRelativeDotNet { get; set; }
        }

        /// <summary>
        /// Create an AppHost with embedded configuration of app binary location
        /// </summary>
        /// <param name="appHostSourceFilePath">The path of Apphost template, which has the place holder</param>
        /// <param name="appHostDestinationFilePath">The destination path for desired location to place, including the file name</param>
        /// <param name="appBinaryFilePath">Full path to app binary or relative path to the result apphost file</param>
        /// <param name="windowsGraphicalUserInterface">Specify whether to set the subsystem to GUI. Only valid for PE apphosts.</param>
        /// <param name="assemblyToCopyResourcesFrom">Path to the intermediate assembly, used for copying resources to PE apphosts.</param>
        /// <param name="enableMacOSCodeSign">Sign the app binary with an anonymous certificate. Only use when the AppHost is a Mach-O file built for MacOS.</param>
        /// <param name="disableCetCompat">Remove CET Shadow Stack compatibility flag if set</param>
        /// <param name="dotNetSearchOptions">Options for how the created apphost should look for the .NET install</param>
        public static void CreateAppHost(
            string appHostSourceFilePath,
            string appHostDestinationFilePath,
            string appBinaryFilePath,
            bool windowsGraphicalUserInterface = false,
            string assemblyToCopyResourcesFrom = null,
            bool enableMacOSCodeSign = false,
            bool disableCetCompat = false,
            DotNetSearchOptions dotNetSearchOptions = null)
        {
            byte[] appPathBytes = Encoding.UTF8.GetBytes(appBinaryFilePath);
            if (appPathBytes.Length > MaxAppBinaryPathSizeInBytes)
            {
                throw new AppNameTooLongException(appBinaryFilePath, MaxAppBinaryPathSizeInBytes);
            }

            byte[] searchOptionsBytes = dotNetSearchOptions != null
                ? GetSearchOptionBytes(dotNetSearchOptions)
                : null;

            bool appHostIsPEImage = false;

            void RewriteAppHost(MemoryMappedFile mappedFile, MemoryMappedViewAccessor accessor)
            {
                // Re-write the destination apphost with the proper contents.
                BinaryUtils.SearchAndReplace(accessor, AppBinaryPathPlaceholderSearchValue, appPathBytes);

                // Update the .NET search configuration
                if (searchOptionsBytes != null)
                {
                    BinaryUtils.SearchAndReplace(accessor, DotNetSearchPlaceholderSearchValue, searchOptionsBytes);
                }

                appHostIsPEImage = PEUtils.IsPEImage(accessor);

                if (windowsGraphicalUserInterface)
                {
                    if (!appHostIsPEImage)
                    {
                        throw new AppHostNotPEFileException("PE file signature not found.");
                    }

                    PEUtils.SetWindowsGraphicalUserInterfaceBit(accessor);
                }

                if (disableCetCompat && appHostIsPEImage)
                {
                    PEUtils.RemoveCetCompatBit(mappedFile, accessor);
                }
            }

            try
            {
                RetryUtil.RetryOnIOError(() =>
                {
                    bool isMachOImage;
                    using (FileStream appHostDestinationStream = new FileStream(appHostDestinationFilePath, FileMode.Create, FileAccess.ReadWrite))
                    {
                        using (FileStream appHostSourceStream = new(appHostSourceFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 1))
                        {
                            isMachOImage = MachObjectFile.IsMachOImage(appHostSourceStream);
                            if (!isMachOImage && enableMacOSCodeSign)
                            {
                                throw new InvalidDataException("Cannot sign a non-Mach-O file.");
                            }
                            appHostSourceStream.CopyTo(appHostDestinationStream);
                        }
                        // Get the size of the source app host to ensure that we don't write extra data to the destination.
                        // On Windows, the size of the view accessor is rounded up to the next page boundary.
                        long appHostLength = appHostDestinationStream.Length;
                        string destinationFileName = Path.GetFileName(appHostDestinationFilePath);
                        // On Mac, we need to extend the file size to accommodate the signature.
                        long appHostTmpCapacity = enableMacOSCodeSign ?
                            appHostLength + MachObjectFile.GetSignatureSizeEstimate((uint)appHostLength, destinationFileName)
                            : appHostLength;

                        using (MemoryMappedFile memoryMappedFile = MemoryMappedFile.CreateFromFile(appHostDestinationStream, null, appHostTmpCapacity, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, true))
                        using (MemoryMappedViewAccessor memoryMappedViewAccessor = memoryMappedFile.CreateViewAccessor(0, appHostTmpCapacity, MemoryMappedFileAccess.ReadWrite))
                        {
                            // Transform the host file in-memory.
                            RewriteAppHost(memoryMappedFile, memoryMappedViewAccessor);
                            if (isMachOImage)
                            {
                                if (enableMacOSCodeSign)
                                {
                                    string fileName = Path.GetFileName(appHostDestinationFilePath);
                                    MachObjectFile machObjectFile = MachObjectFile.Create(memoryMappedViewAccessor);
                                    appHostLength = machObjectFile.CreateAdHocSignature(memoryMappedViewAccessor, fileName);
                                }
                                else if (MachObjectFile.RemoveCodeSignatureIfPresent(memoryMappedViewAccessor, out long? length))
                                {
                                    appHostLength = length.Value;
                                }
                            }
                        }
                        appHostDestinationStream.SetLength(appHostLength);

                        if (assemblyToCopyResourcesFrom != null && appHostIsPEImage)
                        {
                            using var updater = new ResourceUpdater(appHostDestinationStream, true);
                            updater.AddResourcesFromPEImage(assemblyToCopyResourcesFrom);
                            updater.Update();
                        }
                    }
                });

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
                        throw new Win32Exception(Marshal.GetLastWin32Error(), $"Could not set file permission {Convert.ToString(filePermissionOctal, 8)} for {appHostDestinationFilePath}.");
                    }
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
                using (var memoryMappedFile = MemoryMappedFile.CreateFromFile(appHostFilePath, FileMode.Open, null, 0, MemoryMappedFileAccess.Read))
                {
                    using (MemoryMappedViewAccessor accessor = memoryMappedFile.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read))
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

        private static byte[] GetSearchOptionBytes(DotNetSearchOptions searchOptions)
        {
            if (Path.IsPathRooted(searchOptions.AppRelativeDotNet))
                throw new AppRelativePathRootedException(searchOptions.AppRelativeDotNet);

            byte[] pathBytes = searchOptions.AppRelativeDotNet != null
                ? Encoding.UTF8.GetBytes(searchOptions.AppRelativeDotNet)
                : [];

            if (pathBytes.Length > MaxAppRelativeDotNetSizeInBytes)
                throw new AppRelativePathTooLongException(searchOptions.AppRelativeDotNet, MaxAppRelativeDotNetSizeInBytes);

            // <search_location> 0 <app_relative_dotnet_root> 0
            byte[] searchOptionsBytes = new byte[pathBytes.Length + 3]; // +2 for search location + null, +1 for null terminator
            searchOptionsBytes[0] = (byte)searchOptions.Location;
            searchOptionsBytes[1] = 0;
            searchOptionsBytes[searchOptionsBytes.Length - 1] = 0;
            if (pathBytes.Length > 0)
                pathBytes.CopyTo(searchOptionsBytes, 2);

            return searchOptionsBytes;
        }

        [LibraryImport("libc", SetLastError = true)]
        private static partial int chmod([MarshalAs(UnmanagedType.LPStr)] string pathname, int mode);
    }
}
