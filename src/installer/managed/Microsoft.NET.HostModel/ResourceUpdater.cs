// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Mono.Cecil.Binary;

namespace Microsoft.NET.HostModel
{
    /// <summary>
    /// Provides methods for modifying the embedded native resources
    /// in a PE image. It currently only works on Windows, because it
    /// requires various kernel32 APIs.
    /// </summary>
    public class ResourceUpdater : IDisposable
    {
        private readonly FileStream stream;
        private readonly Image image;

        ///<summary>
        /// Determines if the ResourceUpdater is supported by the current operating system.
        /// Some versions of Windows, such as Nano Server, do not support the needed APIs.
        /// </summary>
        public static bool IsSupportedOS()
        {
            return true;
        }

        /// <summary>
        /// Create a resource updater for the given PE file. This will
        /// acquire a native resource update handle for the file,
        /// preparing it for updates. Resources can be added to this
        /// updater, which will queue them for update. The target PE
        /// file will not be modified until Update() is called, after
        /// which the ResourceUpdater can not be used for further
        /// updates.
        /// </summary>
        public ResourceUpdater(string peFile)
        {
            stream = null;
            try
            {
                stream = new FileStream(peFile, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
                image = ImageReader.Read(stream).Image;
            }
            catch (Exception)
            {
                stream?.Dispose();
                throw;
            }
        }

        private static ResourceDirectoryEntry FindOrCreateEntry(ResourceDirectoryTable table, int key)
        {
            foreach (ResourceDirectoryEntry tableEntry in table.Entries)
            {
                if (!tableEntry.IdentifiedByName && tableEntry.ID == key)
                    return tableEntry;
            }

            var newEntry = new ResourceDirectoryEntry(key);
            table.Entries.Add(newEntry);
            return newEntry;
        }

        private static ResourceDirectoryTable FindOrCreateChildDirectory(ResourceDirectoryTable table, int key)
        {
            var entry = FindOrCreateEntry(table, key);
            if (entry.Child is ResourceDirectoryTable directory)
                return directory;
            if (entry.Child != null)
                throw new InvalidOperationException("Found entry is not Directory");
            directory = new ResourceDirectoryTable { MajorVersion = 4, MinorVersion = 0, };
            entry.Child = directory;
            return directory;
        }

        private static ResourceDirectoryEntry FindOrCreateEntry(ResourceDirectoryTable table, string key)
        {
            foreach (ResourceDirectoryEntry tableEntry in table.Entries)
            {
                if (tableEntry.IdentifiedByName && tableEntry.Name.String == key)
                    return tableEntry;
            }

            var newEntry = new ResourceDirectoryEntry(new ResourceDirectoryString(key));
            table.Entries.Add(newEntry);
            return newEntry;
        }

        private static ResourceDirectoryTable FindOrCreateChildDirectory(ResourceDirectoryTable table, string key)
        {
            var entry = FindOrCreateEntry(table, key);
            if (entry.Child is ResourceDirectoryTable directory)
                return directory;
            if (entry.Child != null)
                throw new InvalidOperationException("Found entry is not Directory");
            directory = new ResourceDirectoryTable { MajorVersion = 4, MinorVersion = 0, };
            entry.Child = directory;
            return directory;
        }

        private static ResourceDirectoryTable FindOrCreateChildDirectory(ResourceDirectoryTable table, ResourceDirectoryEntry keyHolder)
        {
            return keyHolder.IdentifiedByName
                ? FindOrCreateChildDirectory(table, keyHolder.Name.String)
                : FindOrCreateChildDirectory(table, keyHolder.ID);
        }

        /// <summary>
        /// Add all resources from a source PE file. It is assumed
        /// that the input is a valid PE file. If it is not, an
        /// exception will be thrown. This will not modify the target
        /// until Update() is called.
        /// Throws an InvalidOperationException if Update() was already called.
        /// </summary>
        public ResourceUpdater AddResourcesFromPEImage(string peFile)
        {
            var module = ImageReader.Read(peFile).Image;

            foreach (ResourceDirectoryEntry lpType in module.ResourceDirectoryRoot.Entries)
            {
                var typeDirectory = FindOrCreateChildDirectory(image.ResourceDirectoryRoot, lpType);
                foreach (ResourceDirectoryEntry lpName in ((ResourceDirectoryTable)lpType.Child).Entries)
                {
                    var nameDirectory = FindOrCreateChildDirectory(typeDirectory, lpName);
                    foreach (ResourceDirectoryEntry wLang in ((ResourceDirectoryTable)lpName.Child).Entries)
                    {
                        var hResource = (ResourceDataEntry)wLang.Child;
                        var entry = FindOrCreateEntry(nameDirectory, wLang.ID);
                        entry.Child = new ResourceDataEntry
                        {
                            Codepage = hResource.Codepage,
                            Reserved = hResource.Reserved,
                            ResourceData = hResource.ResourceData,
                        };
                    }
                }
            }

            return this;
        }

        internal static bool IsIntResource(IntPtr lpType)
        {
            return ((uint)lpType >> 16) == 0;
        }

        private const int LangID_LangNeutral_SublangNeutral = 0;

        /// <summary>
        /// Add a language-neutral integer resource from a byte[] with
        /// a particular type and name. This will not modify the
        /// target until Update() is called.
        /// Throws an InvalidOperationException if Update() was already called.
        /// </summary>
        public ResourceUpdater AddResource(byte[] data, IntPtr lpType, IntPtr lpName)
        {
            if (!IsIntResource(lpType) || !IsIntResource(lpName))
            {
                throw new ArgumentException("AddResource can only be used with integer resource types");
            }

            var typeDirectory = FindOrCreateChildDirectory(image.ResourceDirectoryRoot, (int)lpType);
            var nameDirectory = FindOrCreateChildDirectory(typeDirectory, (int)lpName);
            var entry = FindOrCreateEntry(nameDirectory, LangID_LangNeutral_SublangNeutral);
            entry.Child = new ResourceDataEntry
            {
                Codepage = 1252, // TODO?
                ResourceData = data,
            };

            return this;
        }

        /// <summary>
        /// Add a language-neutral integer resource from a byte[] with
        /// a particular type and name. This will not modify the
        /// target until Update() is called.
        /// Throws an InvalidOperationException if Update() was already called.
        /// </summary>
        public ResourceUpdater AddResource(byte[] data, string lpType, IntPtr lpName)
        {
            if (!IsIntResource(lpName))
            {
                throw new ArgumentException("AddResource can only be used with integer resource names");
            }

            var typeDirectory = FindOrCreateChildDirectory(image.ResourceDirectoryRoot, lpType);
            var nameDirectory = FindOrCreateChildDirectory(typeDirectory, (int)lpName);
            var entry = FindOrCreateEntry(nameDirectory, LangID_LangNeutral_SublangNeutral);
            entry.Child = new ResourceDataEntry
            {
                Codepage = 1252, // TODO?
                ResourceData = data,
            };

            return this;
        }

        /// <summary>
        /// Write the pending resource updates to the target PE
        /// file. After this, the ResourceUpdater no longer maintains
        /// an update handle, and can not be used for further updates.
        /// Throws an InvalidOperationException if Update() was already called.
        /// </summary>
        public void Update()
        {
            var writer = new ImageWriter(image, new BinaryWriter(stream));
            writer.Initialize();
            image.Accept(writer);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Dispose(bool disposing)
        {
            if (disposing)
            {
                stream.Dispose();
            }
        }
    }
}
