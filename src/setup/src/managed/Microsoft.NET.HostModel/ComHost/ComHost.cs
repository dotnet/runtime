﻿using System;
using System.IO;
using System.Text;

namespace Microsoft.NET.HostModel.ComHost
{
    class ComHost
    {
        // These need to match RESOURCEID_CLISDMAP and RESOURCETYPE_CLSIDMAP defined in comhost.h.
        private const int ClsidmapResourceId = 64;
        private const int ClsidmapResourceType = 1024;

        /// <summary>
        /// Create an ComHost with an embedded CLSIDMap file to map CLSIDs to .NET Classes.
        /// </summary>
        /// <param name="comHostSourceFilePath">The path of Apphost template, which has the place holder</param>
        /// <param name="comHostDestinationFilePath">The destination path for desired location to place, including the file name</param>
        /// <param name="clsidmapFilePath">The path to the *.clsidmap file.</param>
        public static void Create(
            string comHostSourceFilePath,
            string comHostDestinationFilePath,
            string clsidmapFilePath)
        {
            var destinationDirectory = new FileInfo(comHostDestinationFilePath).Directory.FullName;
            if (!Directory.Exists(destinationDirectory))
            {
                Directory.CreateDirectory(destinationDirectory);
            }

            // Copy apphost to destination path so it inherits the same attributes/permissions.
            File.Copy(comHostSourceFilePath, comHostDestinationFilePath, overwrite: true);

            if (!ResourceUpdater.IsSupportedOS())
            {
                throw new ComHostCustomizationUnsupportedOSException();
            }

            string clsidMap = File.ReadAllText(clsidmapFilePath);
            byte[] clsidMapBytes = Encoding.UTF8.GetBytes(clsidMap);

            using (ResourceUpdater updater = new ResourceUpdater(comHostDestinationFilePath))
            {
                updater.AddResource(clsidMapBytes, (IntPtr)ClsidmapResourceType, (IntPtr)ClsidmapResourceId);
                updater.Update();
            }
        }
    }
}
