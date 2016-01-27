// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.IO;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;
using System.Security;
using System.Security.Permissions;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace System.IO.IsolatedStorage {

    internal class IsolatedStorageAccountingInfo : IDisposable {

        private const string c_QuotaFileName = "quota.dat";
        private const string c_UsedFileName = "used.dat";

        string m_RootDirectory;

        [SecurityCritical]
        SafeFileMappingHandle m_QuotaFileMapping;
        [SecurityCritical]
        SafeFileMappingHandle m_UsedSizeFileMapping;

        IntPtr m_QuotaView;
        IntPtr m_UsedSizeView;

        FileStream m_QuotaFileStream;
        FileStream m_UsedSizeFileStream;

        bool m_Disposed;

        public long Quota {

            [SecurityCritical]
            get {
                lock (this) {
                    long value;

                    Map();
                    unsafe {
                        value = *((long*)m_QuotaView);
                    }
                    Unmap();
                    return value;
                }
            }

            [SecurityCritical]
            set {
                lock (this) {
                    Map();
                    unsafe {
                        *((long*)m_QuotaView) = value;
                    }
                    Unmap();
                }
            }
        }


        public long UsedSize {
            [SecurityCritical]
            get {
                lock (this) {
                    long value;

                    Map();
                    try {
                        unsafe {
                            value = *((long*)m_UsedSizeView);
                        }
                    } finally {
                        Unmap();
                    }
                    return value;
                }
            }

            [SecurityCritical]
            set {
                lock(this) {
                    Map();
                    try {
                        unsafe {
                            *((long*)m_UsedSizeView) = value;
                        }
                    } finally {
                        Unmap();
                    }
                }
            }
        }

        ~IsolatedStorageAccountingInfo() {
            Dispose(false);
        }

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        [SecuritySafeCritical]
        public void Dispose(bool disposing) {
            lock(this) {
                if (!m_Disposed) {
                    if (disposing) {
                        if (m_QuotaFileStream != null) {
                            m_QuotaFileStream.Dispose();
                            m_QuotaFileStream = null;
                        }

                        if (m_UsedSizeFileStream != null) {
                            m_UsedSizeFileStream.Dispose();
                            m_UsedSizeFileStream = null;
                        }

                        if (m_QuotaFileMapping != null) {
                            m_QuotaFileMapping.Dispose();
                            m_QuotaFileMapping = null;
                        }

                        if (m_UsedSizeFileMapping != null) {
                            m_UsedSizeFileMapping.Dispose();
                            m_UsedSizeFileMapping = null;
                        }
                    }

                    if (m_QuotaView != IntPtr.Zero) {
                        Win32Native.UnmapViewOfFile(m_QuotaView);
                        m_QuotaView = IntPtr.Zero;
                    }

                    if (m_UsedSizeView != IntPtr.Zero) {
                        Win32Native.UnmapViewOfFile(m_UsedSizeView);
                        m_UsedSizeView = IntPtr.Zero;
                    }

                    m_Disposed = true;
                }
            }
        }

        [SecurityCritical]
        public IsolatedStorageAccountingInfo(string rootDirectory) {
            m_RootDirectory = rootDirectory;
            Init();
        }

        [SecurityCritical]
        private void Init() {
            new FileIOPermission(FileIOPermissionAccess.AllAccess, m_RootDirectory).Assert();
            m_QuotaFileStream = new FileStream(Path.Combine(m_RootDirectory, c_QuotaFileName), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite, FileStream.DefaultBufferSize, FileOptions.None, c_QuotaFileName, false, false, false);
            m_UsedSizeFileStream = new FileStream(Path.Combine(m_RootDirectory, c_UsedFileName), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite, FileStream.DefaultBufferSize, FileOptions.None, c_UsedFileName, false, false, false);

            if (m_QuotaFileStream.Length < sizeof(long)) {
                m_QuotaFileStream.Write(BitConverter.GetBytes(IsolatedStorageFile.DefaultQuota), 0, sizeof(long));
            }

            if (m_UsedSizeFileStream.Length < sizeof(long)) {
                m_UsedSizeFileStream.Write(BitConverter.GetBytes((long)0), 0, sizeof(long));
            }
        }

        [SecurityCritical]
        private void Map() {
            if (m_Disposed) {
                // This can happen in the case where an IsolatedStorageFileStream is used after the underlying IsolatedStorageFile object is disposed.
                // which we allowed in Silverlight 3.  If this is the case, we will rebuild our state.
                Init();
                m_Disposed = false;
                GC.ReRegisterForFinalize(this);
            }

            if (m_QuotaFileMapping == null || m_QuotaFileMapping.IsInvalid) {
                m_QuotaFileMapping = Win32Native.CreateFileMapping(m_QuotaFileStream.SafeFileHandle, IntPtr.Zero, Win32Native.PAGE_READWRITE, 0, 0, null);

                if (m_QuotaFileMapping.IsInvalid) {
                    throw Marshal.GetExceptionForHR(Marshal.GetLastWin32Error());
                }
            }

            if (m_UsedSizeFileMapping == null || m_UsedSizeFileMapping.IsInvalid) {
                m_UsedSizeFileMapping = Win32Native.CreateFileMapping(m_UsedSizeFileStream.SafeFileHandle, IntPtr.Zero, Win32Native.PAGE_READWRITE, 0, 0, null);

                if (m_UsedSizeFileMapping.IsInvalid) {
                    throw Marshal.GetExceptionForHR(Marshal.GetLastWin32Error());
                }
            }

            m_QuotaView = Win32Native.MapViewOfFile(m_QuotaFileMapping, Win32Native.FILE_MAP_WRITE | Win32Native.FILE_MAP_READ, 0, 0, UIntPtr.Zero);
            
            if (m_QuotaView == IntPtr.Zero) {
                throw Marshal.GetExceptionForHR(Marshal.GetLastWin32Error());
            }

            m_UsedSizeView = Win32Native.MapViewOfFile(m_UsedSizeFileMapping, Win32Native.FILE_MAP_WRITE | Win32Native.FILE_MAP_READ, 0, 0, UIntPtr.Zero);

            if (m_UsedSizeView == IntPtr.Zero) {
                throw Marshal.GetExceptionForHR(Marshal.GetLastWin32Error());
            }
        }

        [SecurityCritical]
        private void Unmap() {
            Win32Native.UnmapViewOfFile(m_QuotaView);
            m_QuotaView = IntPtr.Zero;
            Win32Native.UnmapViewOfFile(m_UsedSizeView);
            m_UsedSizeView = IntPtr.Zero;
        }

        [SecurityCritical]
        private static bool IsFileValid(string fileName) {
            return File.UnsafeExists(fileName) && (FileInfo.UnsafeCreateFileInfo(fileName).Length == sizeof(long));
        }

        [SecurityCritical]
        public static bool IsAccountingInfoValid(string rootDirectory) {
            new FileIOPermission(FileIOPermissionAccess.AllAccess, rootDirectory).Assert();
            return IsFileValid(Path.Combine(rootDirectory, c_QuotaFileName)) && IsFileValid(Path.Combine(rootDirectory, c_UsedFileName));
        }

        [SecurityCritical]
        public static void RemoveAccountingInfo(string rootDirectory) {
            new FileIOPermission(FileIOPermissionAccess.AllAccess, rootDirectory).Assert();
            File.UnsafeDelete(Path.Combine(rootDirectory, c_QuotaFileName));
            File.UnsafeDelete(Path.Combine(rootDirectory, c_UsedFileName));
        }
    }
}
