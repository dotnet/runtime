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
                    // MacOS requires a new inode to be created when updating a signed file, so we'll delete the file and create a new one.
                    if (File.Exists(appHostDestinationFilePath))
                        File.Delete(appHostDestinationFilePath);

                    long appHostSourceLength = new FileInfo(appHostSourceFilePath).Length;
                    string destinationFileName = Path.GetFileName(appHostDestinationFilePath);
                    // Memory-mapped files cannot be resized, so calculate
                    // the maximum length of the destination file upfront.
                    long appHostDestinationLength = enableMacOSCodeSign ?
                        appHostSourceLength + MachObjectFile.GetSignatureSizeEstimate((uint)appHostSourceLength, destinationFileName)
                        : appHostSourceLength;
                    using (MemoryMappedFile appHostDestinationMap = MemoryMappedFile.CreateNew(null, appHostDestinationLength))
                    {
                        using (MemoryMappedViewStream appHostDestinationStream = appHostDestinationMap.CreateViewStream())
                        using (FileStream appHostSourceStream = new(appHostSourceFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 1))
                        {
                            isMachOImage = MachObjectFile.IsMachOImage(appHostSourceStream);
                            if (!isMachOImage && enableMacOSCodeSign)
                            {
                                throw new InvalidDataException("Cannot sign a non-Mach-O file.");
                            }
                            appHostSourceStream.CopyTo(appHostDestinationStream);
                        }

                        using (MemoryMappedViewAccessor memoryMappedViewAccessor = appHostDestinationMap.CreateViewAccessor())
                        {
                            // Transform the host file in-memory.
                            RewriteAppHost(appHostDestinationMap, memoryMappedViewAccessor);
                            if (isMachOImage)
                            {
                                IMachOFileAccess file = new MemoryMappedMachOViewAccessor(memoryMappedViewAccessor);
                                MachObjectFile machObjectFile = MachObjectFile.Create(file);
                                if (enableMacOSCodeSign)
                                {
                                    appHostDestinationLength = machObjectFile.AdHocSignFile(file, destinationFileName);
                                }
                                else if (machObjectFile.RemoveCodeSignatureIfPresent(file, out long? length))
                                {
                                    appHostDestinationLength = length.Value;
                                }
                            }
                        }
                        using (FileStream appHostDestinationStream = new FileStream(appHostDestinationFilePath, FileMode.Create, FileAccess.ReadWrite, FileShare.None, bufferSize: 1))
                        using (MemoryMappedViewAccessor appHostAccessor = appHostDestinationMap.CreateViewAccessor(0, appHostDestinationLength, MemoryMappedFileAccess.Read))
                        {
                            // Write the final content to the destination file, only up to the total length of the host, not the entire mapped file.
                            // On Windows, memory-mapped files are rounded up to the next page size.
                            // On MacOS, the memory-mapped file is created with a conservative estimate of the size of the signature.
                            BinaryUtils.WriteToStream(appHostAccessor, appHostDestinationStream, appHostDestinationLength);
                            // TODO: This could be moved to work on the MemoryMappedFile if we can precalculate the size required.
                            if (assemblyToCopyResourcesFrom != null && appHostIsPEImage)
                            {
                                using ResourceUpdater updater = new ResourceUpdater(appHostDestinationStream, leaveOpen: true);
                                updater.AddResourcesFromPEImage(assemblyToCopyResourcesFrom);
                                updater.Update();
                            }
                        }
                    }
                });
                Chmod755(appHostDestinationFilePath);
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

        internal static void Chmod755(string pathName)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return;
            var filePermissionOctal = Convert.ToInt32("755", 8); // -rwxr-xr-x
            const int EINTR = 4;
            int chmodReturnCode;

            do
            {
                chmodReturnCode = chmod(pathName, filePermissionOctal);
            }
            while (chmodReturnCode == -1 && Marshal.GetLastWin32Error() == EINTR);

            if (chmodReturnCode == -1)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), $"Could not set file permission {Convert.ToString(filePermissionOctal, 8)} for {pathName}.");
            }
        }

        [LibraryImport("libc", SetLastError = true)]
        private static partial int chmod([MarshalAs(UnmanagedType.LPStr)] string pathname, int mode);
    }
}
