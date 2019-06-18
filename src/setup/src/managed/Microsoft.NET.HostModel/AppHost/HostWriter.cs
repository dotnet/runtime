// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.IO.MemoryMappedFiles;
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
        private readonly static byte[] AppBinaryPathPlaceholderSearchValue = Encoding.UTF8.GetBytes(AppBinaryPathPlaceholder);

        private const string BundleHeaderPlaceholder = "db2a6C16fec7fbebe3539d534a3471e95ea6e85c718cba293996a8ac85F90427";
        private readonly static byte[] BundleHeaderPlaceholderSearchValue = Encoding.UTF8.GetBytes(BundleHeaderPlaceholder);

        /// <summary>
        /// Create an AppHost with embedded configuration of app binary location
        /// </summary>
        /// <param name="appHostSourceFilePath">The path of Apphost template, which has the place holder</param>
        /// <param name="appHostDestinationFilePath">The destination path for desired location to place, including the file name</param>
        /// <param name="appBinaryFilePath">Full path to app binary or relative path to the result apphost file</param>
        /// <param name="windowsGraphicalUserInterface">Specify whether to set the subsystem to GUI. Only valid for PE apphosts.</param>
        /// <param name="assemblyToCopyResorcesFrom">Path to the intermediate assembly, used for copying resources to PE apphosts.</param>
        public static void CreateAppHost(
            string appHostSourceFilePath,
            string appHostDestinationFilePath,
            string appBinaryFilePath,
            bool windowsGraphicalUserInterface = false,
            string assemblyToCopyResorcesFrom = null)
        {
            var bytesToWrite = Encoding.UTF8.GetBytes(appBinaryFilePath);
            if (bytesToWrite.Length > 1024)
            {
                throw new AppNameTooLongException(appBinaryFilePath);
            }

            CopyAppHost(appHostSourceFilePath, appHostDestinationFilePath);

            // Re-write the destination apphost with the proper contents.
            bool appHostIsPEImage = false;
            using (var memoryMappedFile = MemoryMappedFile.CreateFromFile(appHostDestinationFilePath))
            {
                using (MemoryMappedViewAccessor accessor = memoryMappedFile.CreateViewAccessor())
                {
                    BinaryUtils.SearchAndReplace(accessor, AppBinaryPathPlaceholderSearchValue, bytesToWrite);

                    appHostIsPEImage = BinaryUtils.IsPEImage(accessor);

                    if (windowsGraphicalUserInterface)
                    {
                        if (!appHostIsPEImage)
                        {
                            throw new AppHostNotPEFileException();
                        }

                        BinaryUtils.SetWindowsGraphicalUserInterfaceBit(accessor);
                    }
                }
            }

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

            // Memory-mapped write does not updating last write time
            File.SetLastWriteTimeUtc(appHostDestinationFilePath, DateTime.UtcNow);
        }

        /// <summary>
        /// Create an AppHost configured to be a single-file bundle.
        /// </summary>
        /// <param name="appHostSourceFilePath">The path of Apphost template, which has the place holder</param>
        /// <param name="appHostDestinationFilePath">The destination path for desired location to place, including the file name</param>
        /// <param name="bundleHeaderOffset">The offset to the location of bundle header</param>
        public static void CreateBundle(
            string appHostSourceFilePath,
            string appHostDestinationFilePath,
            long bundleHeaderOffset)
        {
            CopyAppHost(appHostSourceFilePath, appHostDestinationFilePath);

            // Re-write the destination apphost with the proper contents.
            BinaryUtils.SearchAndReplace(appHostDestinationFilePath, BundleHeaderPlaceholderSearchValue, BitConverter.GetBytes(bundleHeaderOffset));

            // Memory-mapped write does not updating last write time
            File.SetLastWriteTimeUtc(appHostDestinationFilePath, DateTime.UtcNow);
        }

        private static void CopyAppHost(
            string appHostSourceFilePath,
            string appHostDestinationFilePath)
        {
            var destinationDirectory = new FileInfo(appHostDestinationFilePath).Directory.FullName;
            if (!Directory.Exists(destinationDirectory))
            {
                Directory.CreateDirectory(destinationDirectory);
            }

            // Copy apphost to destination path so it inherits the same attributes/permissions.
            File.Copy(appHostSourceFilePath, appHostDestinationFilePath, overwrite: true);
        }
    }
}
