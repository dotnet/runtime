// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
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

        /// <summary>
        /// Add all resources from a source PE file. It is assumed
        /// that the input is a valid PE file. If it is not, an
        /// exception will be thrown. This will not modify the target
        /// until Update() is called.
        /// Throws an InvalidOperationException if Update() was already called.
        /// </summary>
        public ResourceUpdater AddResourcesFromPEImage(string peFile)
        {
            if (hUpdate.IsInvalid)
            {
                ThrowExceptionForInvalidUpdate();
            }

            // Using both flags lets the OS loader decide how to load
            // it most efficiently. Either mode will prevent other
            // processes from modifying the module while it is loaded.
            IntPtr hModule = Kernel32.LoadLibraryEx(peFile, IntPtr.Zero,
                                                    Kernel32.LoadLibraryFlags.LOAD_LIBRARY_AS_DATAFILE_EXCLUSIVE |
                                                    Kernel32.LoadLibraryFlags.LOAD_LIBRARY_AS_IMAGE_RESOURCE);
            if (hModule == IntPtr.Zero)
            {
                ThrowExceptionForLastWin32Error();
            }

            var enumTypesCallback = new Kernel32.EnumResTypeProc(EnumAndUpdateTypesCallback);
            var errorInfo = new EnumResourcesErrorInfo();
            GCHandle errorInfoHandle = GCHandle.Alloc(errorInfo);
            var errorInfoPtr = GCHandle.ToIntPtr(errorInfoHandle);

            try
            {
                if (!Kernel32.EnumResourceTypes(hModule, enumTypesCallback, errorInfoPtr))
                {
                    if (Marshal.GetHRForLastWin32Error() != Kernel32.ResourceDataNotFoundHRESULT)
                    {
                        CaptureEnumResourcesErrorInfo(errorInfoPtr);
                        errorInfo.ThrowException();
                    }
                }
            }
            finally
            {
                errorInfoHandle.Free();

                if (!Kernel32.FreeLibrary(hModule))
                {
                    ThrowExceptionForLastWin32Error();
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
            // TODO: write to file
        }

        private bool EnumAndUpdateTypesCallback(IntPtr hModule, IntPtr lpType, IntPtr lParam)
        {
            var enumNamesCallback = new Kernel32.EnumResNameProc(EnumAndUpdateNamesCallback);
            if (!Kernel32.EnumResourceNames(hModule, lpType, enumNamesCallback, lParam))
            {
                CaptureEnumResourcesErrorInfo(lParam);
                return false;
            }
            return true;
        }

        private bool EnumAndUpdateNamesCallback(IntPtr hModule, IntPtr lpType, IntPtr lpName, IntPtr lParam)
        {
            var enumLanguagesCallback = new Kernel32.EnumResLangProc(EnumAndUpdateLanguagesCallback);
            if (!Kernel32.EnumResourceLanguages(hModule, lpType, lpName, enumLanguagesCallback, lParam))
            {
                CaptureEnumResourcesErrorInfo(lParam);
                return false;
            }
            return true;
        }

        private bool EnumAndUpdateLanguagesCallback(IntPtr hModule, IntPtr lpType, IntPtr lpName, ushort wLang, IntPtr lParam)
        {
            IntPtr hResource = Kernel32.FindResourceEx(hModule, lpType, lpName, wLang);
            if (hResource == IntPtr.Zero)
            {
                CaptureEnumResourcesErrorInfo(lParam);
                return false;
            }

            // hResourceLoaded is just a handle to the resource, which
            // can be used to get the resource data
            IntPtr hResourceLoaded = Kernel32.LoadResource(hModule, hResource);
            if (hResourceLoaded == IntPtr.Zero)
            {
                CaptureEnumResourcesErrorInfo(lParam);
                return false;
            }

            // This doesn't actually lock memory. It just retrieves a
            // pointer to the resource data. The pointer is valid
            // until the module is unloaded.
            IntPtr lpResourceData = Kernel32.LockResource(hResourceLoaded);
            if (lpResourceData == IntPtr.Zero)
            {
                ((EnumResourcesErrorInfo)GCHandle.FromIntPtr(lParam).Target).failedToLockResource = true;
            }

            if (!Kernel32.UpdateResource(hUpdate, lpType, lpName, wLang, lpResourceData, Kernel32.SizeofResource(hModule, hResource)))
            {
                CaptureEnumResourcesErrorInfo(lParam);
                return false;
            }

            return true;
        }

        private sealed class EnumResourcesErrorInfo
        {
            public int hResult;
            public bool failedToLockResource;

            public void ThrowException()
            {
                if (failedToLockResource)
                {
                    Debug.Assert(hResult == 0);
                    throw new ResourceNotAvailableException("Failed to lock resource");
                }

                Debug.Assert(hResult != 0);
                throw new HResultException(hResult);
            }
        }

        private static void CaptureEnumResourcesErrorInfo(IntPtr errorInfoPtr)
        {
            int hResult = Marshal.GetHRForLastWin32Error();
            if (hResult != Kernel32.UserStoppedResourceEnumerationHRESULT)
            {
                GCHandle errorInfoHandle = GCHandle.FromIntPtr(errorInfoPtr);
                var errorInfo = (EnumResourcesErrorInfo)errorInfoHandle.Target;
                errorInfo.hResult = hResult;
            }
        }

        private sealed class ResourceNotAvailableException : Exception
        {
            public ResourceNotAvailableException(string message) : base(message)
            {
            }
        }

        private static void ThrowExceptionForLastWin32Error()
        {
            throw new HResultException(Marshal.GetHRForLastWin32Error());
        }

        private static void ThrowExceptionForInvalidUpdate()
        {
            throw new InvalidOperationException("Update handle is invalid. This instance may not be used for further updates");
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
