// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Microsoft.NET.HostModel
{
    /// <summary>
    /// Provides methods for modifying the embedded native resources
    /// in a PE image. It currently only works on Windows, because it
    /// requires various kernel32 APIs.
    /// </summary>
    public class ResourceUpdater : IDisposable
    {
        private sealed class Kernel32
        {
            //
            // Native methods for updating resources
            //

            [DllImport(nameof(Kernel32), CharSet = CharSet.Unicode, SetLastError=true)]
            public static extern SafeUpdateHandle BeginUpdateResource(string pFileName,
                                                                      [MarshalAs(UnmanagedType.Bool)]bool bDeleteExistingResources);

            // Update a resource with data from an IntPtr
            [DllImport(nameof(Kernel32), SetLastError=true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool UpdateResource(SafeUpdateHandle hUpdate,
                                                     IntPtr lpType,
                                                     IntPtr lpName,
                                                     ushort wLanguage,
                                                     IntPtr lpData,
                                                     uint cbData);

            // Update a resource with data from a managed byte[]
            [DllImport(nameof(Kernel32), SetLastError=true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool UpdateResource(SafeUpdateHandle hUpdate,
                                                     IntPtr lpType,
                                                     IntPtr lpName,
                                                     ushort wLanguage,
                                                     [MarshalAs(UnmanagedType.LPArray, SizeParamIndex=5)] byte[] lpData,
                                                     uint cbData);

            // Update a resource with data from a managed byte[]
            [DllImport(nameof(Kernel32), SetLastError=true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool UpdateResource(SafeUpdateHandle hUpdate,
                                                     string lpType,
                                                     IntPtr lpName,
                                                     ushort wLanguage,
                                                     [MarshalAs(UnmanagedType.LPArray, SizeParamIndex=5)] byte[] lpData,
                                                     uint cbData);

            [DllImport(nameof(Kernel32), SetLastError=true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool EndUpdateResource(SafeUpdateHandle hUpdate,
                                                        bool fDiscard);

            // The IntPtr version of this dllimport is used in the
            // SafeHandle implementation
            [DllImport(nameof(Kernel32), SetLastError=true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool EndUpdateResource(IntPtr hUpdate,
                                                        bool fDiscard);

            public const ushort LangID_LangNeutral_SublangNeutral = 0;

            //
            // Native methods used to read resources from a PE file
            //

            // Loading and freeing PE files

            public enum LoadLibraryFlags : uint
            {
                LOAD_LIBRARY_AS_DATAFILE_EXCLUSIVE = 0x00000040,
                LOAD_LIBRARY_AS_IMAGE_RESOURCE = 0x00000020
            }

            [DllImport(nameof(Kernel32), CharSet = CharSet.Unicode, SetLastError=true)]
            public static extern IntPtr LoadLibraryEx(string lpFileName,
                                                      IntPtr hReservedNull,
                                                      LoadLibraryFlags dwFlags);

            [DllImport(nameof(Kernel32), SetLastError=true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool FreeLibrary(IntPtr hModule);

            // Enumerating resources

            public delegate bool EnumResTypeProc(IntPtr hModule,
                                                 IntPtr lpType,
                                                 IntPtr lParam);

            public delegate bool EnumResNameProc(IntPtr hModule,
                                                 IntPtr lpType,
                                                 IntPtr lpName,
                                                 IntPtr lParam);

            public delegate bool EnumResLangProc(IntPtr hModule,
                                                 IntPtr lpType,
                                                 IntPtr lpName,
                                                 ushort wLang,
                                                 IntPtr lParam);

            [DllImport(nameof(Kernel32), SetLastError=true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool EnumResourceTypes(IntPtr hModule,
                                                         EnumResTypeProc lpEnumFunc,
                                                         IntPtr lParam);

            [DllImport(nameof(Kernel32), SetLastError=true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool EnumResourceNames(IntPtr hModule,
                                                         IntPtr lpType,
                                                         EnumResNameProc lpEnumFunc,
                                                         IntPtr lParam);

            [DllImport(nameof(Kernel32), SetLastError=true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool EnumResourceLanguages(IntPtr hModule,
                                                            IntPtr lpType,
                                                            IntPtr lpName,
                                                            EnumResLangProc lpEnumFunc,
                                                            IntPtr lParam);

            public const int UserStoppedResourceEnumerationHRESULT = unchecked((int)0x80073B02);
            public const int ResourceDataNotFoundHRESULT = unchecked((int)0x80070714);

            // Querying and loading resources

            [DllImport(nameof(Kernel32), SetLastError=true)]
            public static extern IntPtr FindResourceEx(IntPtr hModule,
                                                       IntPtr lpType,
                                                       IntPtr lpName,
                                                       ushort wLanguage);

            [DllImport(nameof(Kernel32), SetLastError=true)]
            public static extern IntPtr LoadResource(IntPtr hModule,
                                                     IntPtr hResInfo);

            [DllImport(nameof(Kernel32))] // does not call SetLastError
            public static extern IntPtr LockResource(IntPtr hResData);

            [DllImport(nameof(Kernel32), SetLastError=true)]
            public static extern uint SizeofResource(IntPtr hModule,
                                                     IntPtr hResInfo);

            public const int ERROR_CALL_NOT_IMPLEMENTED = 0x78;
        }

        /// <summary>
        /// Holds the update handle returned by BeginUpdateResource.
        /// Normally, native resources for the update handle are
        /// released by a call to ResourceUpdater.Update(). In case
        /// this doesn't happen, the SafeUpdateHandle will release the
        /// native resources for the update handle without updating
        /// the target file.
        /// </summary>
        private sealed class SafeUpdateHandle : SafeHandle
        {
            public SafeUpdateHandle() : base(IntPtr.Zero, true)
            {
            }

            public override bool IsInvalid => handle == IntPtr.Zero;

            protected override bool ReleaseHandle()
            {
                // discard pending updates without writing them
                return Kernel32.EndUpdateResource(handle, true);
            }
        }

        /// <summary>
        /// Holds the native handle for the resource update.
        /// </summary>
        private readonly SafeUpdateHandle hUpdate;

        ///<summary>
        /// Determines if the ResourceUpdater is supported by the current operating system.
        /// Some versions of Windows, such as Nano Server, do not support the needed APIs.
        /// </summary>
        public static bool IsSupportedOS()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return false;
            }

            try
            {
                // On Nano Server 1709+, `BeginUpdateResource` is exported but returns a null handle with a zero error
                // Try to call `BeginUpdateResource` with an invalid parameter; the error should be non-zero if supported
                // On Nano Server 20213, `BeginUpdateResource` fails with ERROR_CALL_NOT_IMPLEMENTED
                using (var handle = Kernel32.BeginUpdateResource("", false))
                {
                    int lastWin32Error = Marshal.GetLastWin32Error();

                    if (handle.IsInvalid && (lastWin32Error == 0 || lastWin32Error == Kernel32.ERROR_CALL_NOT_IMPLEMENTED))
                    {
                        return false;
                    }
                }
            }
            catch (EntryPointNotFoundException)
            {
                // BeginUpdateResource isn't exported from Kernel32
                return false;
            }

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
            hUpdate = Kernel32.BeginUpdateResource(peFile, false);
            if (hUpdate.IsInvalid)
            {
                ThrowExceptionForLastWin32Error();
            }
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

        /// <summary>
        /// Add a language-neutral integer resource from a byte[] with
        /// a particular type and name. This will not modify the
        /// target until Update() is called.
        /// Throws an InvalidOperationException if Update() was already called.
        /// </summary>
        public ResourceUpdater AddResource(byte[] data, IntPtr lpType, IntPtr lpName)
        {
            if (hUpdate.IsInvalid)
            {
                ThrowExceptionForInvalidUpdate();
            }

            if (!IsIntResource(lpType) || !IsIntResource(lpName))
            {
                throw new ArgumentException("AddResource can only be used with integer resource types");
            }

            if (!Kernel32.UpdateResource(hUpdate, lpType, lpName, Kernel32.LangID_LangNeutral_SublangNeutral, data, (uint)data.Length))
            {
                ThrowExceptionForLastWin32Error();
            }

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
            if (hUpdate.IsInvalid)
            {
                ThrowExceptionForInvalidUpdate();
            }

            if (!IsIntResource(lpName))
            {
                throw new ArgumentException("AddResource can only be used with integer resource names");
            }

            if (!Kernel32.UpdateResource(hUpdate, lpType, lpName, Kernel32.LangID_LangNeutral_SublangNeutral, data, (uint)data.Length))
            {
                ThrowExceptionForLastWin32Error();
            }

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
            if (hUpdate.IsInvalid)
            {
                ThrowExceptionForInvalidUpdate();
            }

            try
            {
                if (!Kernel32.EndUpdateResource(hUpdate, false))
                {
                    ThrowExceptionForLastWin32Error();
                }
            }
            finally
            {
                hUpdate.SetHandleAsInvalid();
            }
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
                hUpdate.Dispose();
            }
        }
    }
}
