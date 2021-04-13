// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Microsoft.NET.HostModel.ComHost
{
    public class ComHost
    {
        // These need to match RESOURCEID_CLSIDMAP and RESOURCETYPE_CLSIDMAP defined in comhost.h.
        private const int ClsidmapResourceId = 64;
        private const int ClsidmapResourceType = 1024;

        /// <summary>
        /// Create a ComHost with an embedded CLSIDMap file to map CLSIDs to .NET Classes.
        /// </summary>
        /// <param name="comHostSourceFilePath">The path of Apphost template, which has the place holder</param>
        /// <param name="comHostDestinationFilePath">The destination path for desired location to place, including the file name</param>
        /// <param name="clsidmapFilePath">The path to the *.clsidmap file.</param>
        /// <param name="typeLibraries">Resource ids for tlbs and paths to the tlb files to be embedded.</param>
        public static void Create(
            string comHostSourceFilePath,
            string comHostDestinationFilePath,
            string clsidmapFilePath,
            IReadOnlyDictionary<int, string> typeLibraries = null)
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
                if (typeLibraries is not null)
                {
                    foreach (var typeLibrary in typeLibraries)
                    {
                        if (!ResourceUpdater.IsIntResource((IntPtr)typeLibrary.Key))
                        {
                            throw new InvalidTypeLibraryIdException(typeLibrary.Value, typeLibrary.Key);
                        }

                        try
                        {
                            byte[] tlbFileBytes = File.ReadAllBytes(typeLibrary.Value);
                            updater.AddResource(tlbFileBytes, "typelib", (IntPtr)typeLibrary.Key);
                        }
                        catch (FileNotFoundException ex)
                        {
                            throw new TypeLibraryDoesNotExistException(typeLibrary.Value, ex);
                        }
                    }
                }
                updater.Update();
            }
        }
    }
}
