// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
 * 
// 
// 
 *
 *
 * Purpose: Provides access to Application files and folders
 *
 *
 ===========================================================*/
namespace System.IO.IsolatedStorage {
    using System;
    using System.Text;
    using System.IO;
    using Microsoft.Win32;
    using Microsoft.Win32.SafeHandles;
    using System.Collections;
    using System.Collections.Generic;
    using System.Security;
    using System.Threading;
    using System.Security.Policy;
    using System.Security.Permissions;
    using System.Runtime.InteropServices;
    using System.Runtime.CompilerServices;
#if FEATURE_CORRUPTING_EXCEPTIONS
    using System.Runtime.ExceptionServices;
#endif // FEATURE_CORRUPTING_EXCEPTIONS

    using System.Runtime.ConstrainedExecution;
    using System.Runtime.Versioning;
    using System.Globalization;
    using System.Collections.ObjectModel;
    using System.Diagnostics.Contracts;
    using System.Security.AccessControl;
    using System.Security.Principal;
    using System.Security.Util;
    
    public sealed class IsolatedStorageFile : IDisposable {
        private const int s_BlockSize = 1024;
        private const int s_DirSize = s_BlockSize;
#if !FEATURE_LEGACYNETCF
        internal const string s_GroupPathPrefix = "g";
        internal const string s_StorePathPrefix = "s";
        internal const string s_FilesPathPrefix = "f";
        internal const string s_LockPathPrefix = "l";
        internal const string s_GroupFileName = "group.dat";
        internal const string s_IdFileName = "id.dat";
        internal const string s_CleanupFileName = "pendingcleanup.dat";
        internal const string c_VersionPrefix = "1";
#endif // !FEATURE_LEGACYNETCF
        internal const string c_DisabledFileName = "disabled.dat";

#if !FEATURE_LEGACYNETCF
        private string m_GroupPath;
        private string m_StorePath;
#endif
        private string m_AppFilesPath;
        private string m_GroupName;

        private FileIOAccess m_AppFilesPathAccess;

        private bool m_bDisposed;
        private bool m_closed;

        private object m_internalLock = new object();

        private static string s_RootFromHost;
        private static string s_IsolatedStorageRoot;

#if FEATURE_LEGACYNETCF
        private static Lazy<IsolatedStorageFileIOHelperBase> s_IsoStoreFileIOHelper;

        [SecuritySafeCritical]
        static IsolatedStorageFile() {
            // IsolatedStorageFile is on the dangerous list, so we can't construct a delegate to GetIsolatedStorageFileIOHelper in partial trust
            // unless we have ReflectionPermission.
            (new ReflectionPermission(PermissionState.Unrestricted)).Assert();
            s_IsoStoreFileIOHelper = new Lazy<IsolatedStorageFileIOHelperBase>(GetIsolatedStorageFileIOHelper);
        }
#endif

#if !FEATURE_LEGACYNETCF
        private IsolatedStorageAccountingInfo m_accountingInfo;
#endif // !FEATURE_LEGACYNETCF

        /*
         * Constructors
         */
        internal IsolatedStorageFile() { }

#if !FEATURE_LEGACYNETCF
        #if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
        #endif
        private FileLock GetGroupFileLock() {
            return FileLock.GetFileLock(Path.Combine(IsolatedStorageRoot, s_LockPathPrefix), s_GroupPathPrefix + "-" + DirectoryInfo.UnsafeCreateDirectoryInfo(m_GroupPath).Name);
        }
#endif

        /*
         * Public Static Properties
         */
        public static Int64 DefaultQuota {
            get {
#if FEATURE_LEGACYNETCF
                return Int64.MaxValue;
#else
                return 1024 * 1024;
#endif // FEATURE_LEGACYNETCF
            }
        }

        public static Boolean IsEnabled {
            #if FEATURE_CORECLR
            [System.Security.SecuritySafeCritical] // auto-generated
            #endif
            get {
                return IsolatedStorageGroup.Enabled;
            }
        }

        /*
         * Public Instance Properties
         */
        public Int64 UsedSize {
            #if FEATURE_CORECLR
            [System.Security.SecuritySafeCritical] // auto-generated
            #endif
            get {
#if !FEATURE_LEGACYNETCF
                lock (m_internalLock) {

                    EnsureStoreIsValid();
                    FileLock groupLock = GetGroupFileLock();

                    try {
                        groupLock.Lock();
                        return m_accountingInfo.UsedSize;
                    } catch (IOException e) {
                        throw GetIsolatedStorageException("IsolatedStorage_Operation", e);
                    } catch (UnauthorizedAccessException e) {
                        throw GetIsolatedStorageException("IsolatedStorage_Operation", e);
                    } finally {
                        groupLock.Unlock();
                    }
                }
#else // !FEATURE_LEGACYNETCF
                return 0;            
#endif // !FEATURE_LEGACYNETCF
            }
        }

        public Int64 Quota {
            #if FEATURE_CORECLR
            [System.Security.SecuritySafeCritical] // auto-generated
            #endif
            get
            {
#if !FEATURE_LEGACYNETCF
                lock(m_internalLock) {
                    
                    EnsureStoreIsValid();
                    FileLock groupLock = GetGroupFileLock();

                    try {
                        groupLock.Lock();
                        return m_accountingInfo.Quota;
                    } catch(IOException e) {
                        throw GetIsolatedStorageException("IsolatedStorage_Operation", e);
                    } catch (UnauthorizedAccessException e) {
                        throw GetIsolatedStorageException("IsolatedStorage_Operation", e);
                    } finally {
                        groupLock.Unlock();
                    }
                }
#else // !FEATURE_LEGACYNETCF
                EnsureStoreIsValid();

                return Int64.MaxValue;
#endif
            }
        }

        public Int64 AvailableFreeSpace {
#if FEATURE_LEGACYNETCF
        [SecuritySafeCritical]
#endif
            get {
#if FEATURE_LEGACYNETCF
                IsolatedStorageSecurityState s = IsolatedStorageSecurityState.CreateStateToGetAvailableFreeSpace();
                if(s.IsStateAvailable())
                {
                    if (s.AvailableFreeSpaceComputed) {
                        return s.AvailableFreeSpace;
                    } else {
                        return Quota - UsedSize;
                    }
                } else {
                    return Quota - UsedSize;
                }
#else // FEATURE_LEGACYNETCF
                return Quota - UsedSize;
#endif // FEATURE_LEGACYNETCF
            }
        }

        /*
         * Private Properties
         */
        internal string RootDirectory {
            get {
                return m_AppFilesPath;
            }
        }

        internal bool Disposed {
            get {
                return m_bDisposed;
            }
        }

#if FEATURE_LEGACYNETCF
        internal string GroupName {
            get {
                return m_GroupName;
            }
        }
#endif

        internal static string IsolatedStorageRoot {
            #if FEATURE_CORECLR
            [System.Security.SecurityCritical] // auto-generated
            #endif
            get {
                if (s_IsolatedStorageRoot == null) {
                    // No need to lock here, FetchOrCreateRoot is idempotent.
                    s_IsolatedStorageRoot = FetchOrCreateRoot();
                }

                return s_IsolatedStorageRoot;
            }

            private set {
                s_IsolatedStorageRoot = value;
            }
        }

        internal bool IsDeleted {
            #if FEATURE_CORECLR
            [System.Security.SecurityCritical] // auto-generated
            #endif
            get {
                try {
#if !FEATURE_LEGACYNETCF
                    return !(Directory.UnsafeExists(m_StorePath) && !File.UnsafeExists(Path.Combine(m_StorePath, s_CleanupFileName)) &&
                             !File.UnsafeExists(Path.Combine(m_GroupPath, s_CleanupFileName)));
#else // !FEATURE_LEGACYNETCF
                    return !Directory.UnsafeExists(IsolatedStorageRoot);
#endif // !FEATURE_LEGACYNETCF
                } catch(IOException) {
                    // It's better to assume the IsoStore is gone if we can't prove it is there.
                    return true;
                } catch(UnauthorizedAccessException) {
                    // It's better to assume the IsoStore is gone if we can't prove it is there.
                    return true;
                }
            }
        }

        /*
         * Public Instance Methods
         */
        #if FEATURE_CORECLR
        [System.Security.SecuritySafeCritical] // auto-generated
        #endif
        public void Remove() {
#if !FEATURE_LEGACYNETCF
            FileLock groupLock = GetGroupFileLock();

            try {
                groupLock.Lock();
                EnsureStoreIsValid();
                TryRemove();
            } finally {
                groupLock.Unlock();
            }
#else
            CleanDirectoryNoUnreserve(m_AppFilesPath);
#endif
        }

#if !FEATURE_LEGACYNETCF
        #if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
        #endif
        internal bool TryRemove() {
            FileLock groupLock = GetGroupFileLock();

            try {
                groupLock.Lock();

                bool removedAll = false;

                try {
                    TouchFile(Path.Combine(m_StorePath, s_CleanupFileName));
                    removedAll = CleanDirectory(Path.Combine(m_StorePath, s_FilesPathPrefix));

                    if (removedAll) {
                        Directory.UnsafeDelete(Path.Combine(m_StorePath, s_FilesPathPrefix), false);
                        File.UnsafeDelete(Path.Combine(m_StorePath, s_GroupFileName));
                        File.UnsafeDelete(Path.Combine(m_StorePath, s_IdFileName));
                        File.UnsafeDelete(Path.Combine(m_StorePath, s_CleanupFileName));
                        Directory.UnsafeDelete(m_StorePath, false);
                        Unreserve(s_DirSize);
                    }

                } catch (IOException e) {
                    throw GetIsolatedStorageException("IsolatedStorage_Operation", e);
                } catch (UnauthorizedAccessException e) {
                    throw GetIsolatedStorageException("IsolatedStorage_Operation", e);
                }

                Close();

                return removedAll;
            } finally {
                groupLock.Unlock();
            }
        }
#endif // !FEATURE_LEGACYNETCF


        #if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
        #endif
        private static bool CleanDirectoryNoUnreserve(string targetDirectory) {
            bool noErrors = true;

            foreach (string f in Directory.UnsafeGetFiles(targetDirectory, "*", SearchOption.TopDirectoryOnly)) {
                try {
                    File.UnsafeDelete(Path.Combine(targetDirectory, f));
                } catch (IOException) {
                    noErrors = false;
                } catch (UnauthorizedAccessException) {
                    noErrors = false;
                }
            }

            foreach (string d in Directory.UnsafeGetDirectories(targetDirectory, "*", SearchOption.TopDirectoryOnly)) {
                if (CleanDirectoryNoUnreserve(d)) {
                    try {
                        Directory.UnsafeDelete(d, false);
                    } catch (IOException) {
                        noErrors = false;
                    } catch (UnauthorizedAccessException) {
                        noErrors = false;
                    }
                } else {
                    noErrors = false;
                }
            }
            return noErrors;
        }

#if FEATURE_ISOLATED_STORAGE_QUOTA_ENFORCEMENT
        #if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
        #endif
        private bool CleanDirectory(string targetDirectory) {
            bool noErrors = true;

            foreach (string f in Directory.UnsafeGetFiles(targetDirectory, "*", SearchOption.TopDirectoryOnly)) {
                try {
                    long fileLength = FileInfo.UnsafeCreateFileInfo(f).Length;
                    File.UnsafeDelete(Path.Combine(targetDirectory, f));
                    Unreserve(RoundToBlockSize((ulong)fileLength));
                } catch (IOException) {
                    noErrors = false;
                } catch (UnauthorizedAccessException) {
                    noErrors = false;
                }
            }

            foreach (string d in Directory.UnsafeGetDirectories(targetDirectory, "*", SearchOption.TopDirectoryOnly)) {
                if (CleanDirectory(d)) {
                    try {
                        Directory.UnsafeDelete(d, false);
                        Unreserve(s_DirSize);
                    } catch (IOException) {
                        noErrors = false;
                    } catch (UnauthorizedAccessException) {
                        noErrors = false;
                    }
                } else {
                    noErrors = false;
                }
            }
            return noErrors;
        }
#endif // FEATURE_ISOLATED_STORAGE_QUOTA_ENFORCEMENT        

        public void Close() {
            lock(m_internalLock) {

                if(!m_closed) {
                    m_closed = true;
#if !FEATURE_LEGACYNETCF
                    m_accountingInfo.Dispose();
#endif
                    GC.SuppressFinalize(this);
                }
            }
        }

        #if FEATURE_CORECLR
        [System.Security.SecuritySafeCritical] // auto-generated
        #endif
        public bool IncreaseQuotaTo(Int64 newQuotaSize) {
            if(newQuotaSize <= Quota) {
                throw new ArgumentException(Environment.GetResourceString("IsolatedStorage_OldQuotaLarger"));
            }
            Contract.EndContractBlock();

            EnsureStoreIsValid();

            IsolatedStorageSecurityState s = IsolatedStorageSecurityState.CreateStateToIncreaseQuotaForGroup(m_GroupName, newQuotaSize, UsedSize);
            if(!s.IsStateAvailable())
            {
                return false;
            }

#if !FEATURE_LEGACYNETCF
            FileLock groupLock = GetGroupFileLock();

            try {
                groupLock.Lock();
                m_accountingInfo.Quota = s.Quota;
                return true;
            } catch(IOException e) {
                throw GetIsolatedStorageException("IsolatedStorage_Operation", e);
            } catch (UnauthorizedAccessException e) {
                throw GetIsolatedStorageException("IsolatedStorage_Operation", e);
            } finally {
                groupLock.Unlock();
            }
#else // !FEATURE_LEGACYNETCF
            return true;
#endif
        }

        #if FEATURE_CORECLR
        [System.Security.SecuritySafeCritical] // auto-generated
        #endif
        public void DeleteFile(String file) {
            if(file == null)
                throw new ArgumentNullException("file");
            Contract.EndContractBlock();

            EnsureStoreIsValid();

#if FEATURE_ISOLATED_STORAGE_QUOTA_ENFORCEMENT
            long oldLen = 0;

            bool locked = false;
            RuntimeHelpers.PrepareConstrainedRegions();
            try {
                Lock(ref locked); // protect oldLen
#endif // FEATURE_ISOLATED_STORAGE_QUOTA_ENFORCEMENT
                
                try {

                    String fullPath = GetFullPath(file);

                    Demand(fullPath);

#if FEATURE_ISOLATED_STORAGE_QUOTA_ENFORCEMENT
                    FileInfo f = FileInfo.UnsafeCreateFileInfo(fullPath);
                    oldLen = f.Length;
#endif // FEATURE_ISOLATED_STORAGE_QUOTA_ENFORCEMENT

                    File.UnsafeDelete(fullPath);
                } catch (Exception e) {
                    throw GetIsolatedStorageException("IsolatedStorage_DeleteFile", e);
                }

#if FEATURE_ISOLATED_STORAGE_QUOTA_ENFORCEMENT
                Unreserve(RoundToBlockSize((ulong)oldLen));
            } finally {
                if(locked)
                    Unlock();
            }
#endif // FEATURE_ISOLATED_STORAGE_QUOTA_ENFORCEMENT
        }

        #if FEATURE_CORECLR
        [System.Security.SecuritySafeCritical] // auto-generated
        #endif
        public bool FileExists(string path) {

            if(path == null) {
                if(CompatibilitySwitches.IsAppEarlierThanWindowsPhone8) { 
                    return false;
                }

                throw new ArgumentNullException("path");
            }

            EnsureStoreIsValid();

            String isPath = GetFullPath(path); // Prepend IS root
            String fullPath = Path.GetFullPathInternal(isPath);

            // Make sure that we have permission to check the file so we don't
            // paths like ..\..\..\..\Windows
            try {
                Demand(fullPath);
            } catch {
                // File.UnsafeExists returns false if the demand fails as well.
                return false;
            }

            bool ret = File.UnsafeExists(fullPath);

            return ret;
        }

        #if FEATURE_CORECLR
        [System.Security.SecuritySafeCritical] // auto-generated
        #endif
        public bool DirectoryExists(string path) {
            if (path == null)
                throw new ArgumentNullException("path");
            Contract.EndContractBlock();

            EnsureStoreIsValid();

            String isPath = GetFullPath(path); // Prepend IS root
            String fullPath = Path.GetFullPathInternal(isPath);

            if (isPath.EndsWith(Path.DirectorySeparatorChar + ".", StringComparison.Ordinal)) {
                if (fullPath.EndsWith(Path.DirectorySeparatorChar)) {
                    fullPath += ".";
                } else {
                    fullPath += Path.DirectorySeparatorChar + ".";
                }
            }

            // Make sure that we have permission to check the directory so we don't
            // paths like ..\..\..\..\Windows
            try {
                Demand(fullPath);
            } catch {
                // Directory.UnsafeExists returns false if the demand fails as well.
                return false;
            }

            bool ret = Directory.UnsafeExists(fullPath);

            return ret;
        }

        #if FEATURE_CORECLR
        [System.Security.SecuritySafeCritical] // auto-generated
        #endif
        public void CreateDirectory(String dir) {
            if(dir == null)
                throw new ArgumentNullException("dir");
            Contract.EndContractBlock();

            EnsureStoreIsValid();

            String isPath = GetFullPath(dir); // Prepend IS root
            String fullPath = Path.GetFullPathInternal(isPath);


            // Make sure that we have permission to create the directory, so that we don't try to process
            // paths like ..\..\..\..\Windows
            try {
                Demand(fullPath);
            } catch (Exception e) {
                throw GetIsolatedStorageException("IsolatedStorage_CreateDirectory", e);
            }

            // We can save a bunch of work if the directory we want to create already exists.  This also
            // saves us in the case where sub paths are inaccessible (due to ERROR_ACCESS_DENIED) but the
            // final path is accessable and the directory already exists.  For example, consider trying
            // to create c:\Foo\Bar\Baz, where everything already exists but ACLS prevent access to c:\Foo
            // and c:\Foo\Bar.  In that case, this code will think it needs to create c:\Foo, and c:\Foo\Bar
            // and fail to due so, causing an exception to be thrown.  This is not what we want.
            if(Directory.InternalExists(fullPath)) {
                return;
            }

            String[] dirList = DirectoriesToCreate(fullPath);
            
            // Nothing to create, return.
            if(dirList == null) {
                return;
            }

#if FEATURE_ISOLATED_STORAGE_QUOTA_ENFORCEMENT
            Reserve(s_DirSize * ((ulong)dirList.Length));
#endif // FEATURE_ISOLATED_STORAGE_QUOTA_ENFORCEMENT

            try {
                Directory.UnsafeCreateDirectory(dirList[dirList.Length - 1]);
            } catch (Exception e) {
#if FEATURE_ISOLATED_STORAGE_QUOTA_ENFORCEMENT
                Unreserve(s_DirSize * ((ulong)dirList.Length));
#endif // FEATURE_ISOLATED_STORAGE_QUOTA_ENFORCEMENT

                // force delete any new directories we created
                try {
                    Directory.UnsafeDelete(dirList[0], true);
                } catch {
                    // If the above failed (on index 0) then this could fail as well.
                }
                throw GetIsolatedStorageException("IsolatedStorage_CreateDirectory", e);
            }
        }

        #if FEATURE_CORECLR
        [System.Security.SecuritySafeCritical] // auto-generated
        #endif
        public void DeleteDirectory(String dir) {
            if(dir == null)
                throw new ArgumentNullException("dir");
            Contract.EndContractBlock();

            EnsureStoreIsValid();

#if FEATURE_ISOLATED_STORAGE_QUOTA_ENFORCEMENT
            bool locked = false;
            RuntimeHelpers.PrepareConstrainedRegions();
            try {
                Lock(ref locked); // Delete *.*, will beat quota enforcement without this lock
#endif // FEATURE_ISOLATED_STORAGE_QUOTA_ENFORCEMENT               
                try {
                    string fullPath = GetFullPath(dir);
                    Demand(fullPath);
                    Directory.UnsafeDelete(fullPath, false);
                } catch (Exception e) {
                    throw GetIsolatedStorageException("IsolatedStorage_DeleteDirectory", e);
                }
#if FEATURE_ISOLATED_STORAGE_QUOTA_ENFORCEMENT
                Unreserve(s_DirSize);
            } finally {
                if(locked)
                    Unlock();
            }
#endif // FEATURE_ISOLATED_STORAGE_QUOTA_ENFORCEMENT
        }

        public String[] GetFileNames() {
            return GetFileNames("*");
        }

        /*
         * foo\abc*.txt will give all abc*.txt files in foo directory
         */
        #if FEATURE_CORECLR
        [System.Security.SecuritySafeCritical] // auto-generated
        #endif
        public String[] GetFileNames(String searchPattern) {
            if(searchPattern == null)
                throw new ArgumentNullException("searchPattern");
            Contract.EndContractBlock();

            EnsureStoreIsValid();

            String[] retVal = GetFileDirectoryNames(GetFullPath(searchPattern), searchPattern, true, this);
            return retVal;
        }

        public String[] GetDirectoryNames() {
            return GetDirectoryNames("*");
        }

        /*
         * foo\data* will give all directory names in foo directory that 
         * starts with data
         */
        #if FEATURE_CORECLR
        [System.Security.SecuritySafeCritical] // auto-generated
        #endif
        public String[] GetDirectoryNames(String searchPattern) {
            if(searchPattern == null)
                throw new ArgumentNullException("searchPattern");
            Contract.EndContractBlock();

            EnsureStoreIsValid();

            String[] retVal = GetFileDirectoryNames(GetFullPath(searchPattern), searchPattern, false, this);
            return retVal;
        }

        public IsolatedStorageFileStream OpenFile(string path, FileMode mode) {

            EnsureStoreIsValid();
            return new IsolatedStorageFileStream(path, mode, this);

        }

        public IsolatedStorageFileStream OpenFile(string path, FileMode mode, FileAccess access) {

            EnsureStoreIsValid();
            return new IsolatedStorageFileStream(path, mode, access, this);
        }

        public IsolatedStorageFileStream OpenFile(string path, FileMode mode, FileAccess access, FileShare share) {

            EnsureStoreIsValid();
            return new IsolatedStorageFileStream(path, mode, access, share, this);
        }

        public IsolatedStorageFileStream CreateFile(string path) {

            EnsureStoreIsValid();
            return new IsolatedStorageFileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.None, this);
        }

        #if FEATURE_CORECLR
        [System.Security.SecuritySafeCritical] // auto-generated
        #endif
        public DateTimeOffset GetCreationTime(string path) {

            if (path == null)
                throw new ArgumentNullException("path");

            if (path == String.Empty) {
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyPath"), "path");
            }

            Contract.EndContractBlock();

            EnsureStoreIsValid();

            String isPath = GetFullPath(path); // Prepend IS root
            String fullPath = Path.GetFullPathInternal(isPath);

            // Make sure that we have permission to check the directory so we don't
            // paths like ..\..\..\..\Windows
            try {
                Demand(fullPath);
            } catch {
                return new DateTimeOffset(1601, 1, 1, 0, 0, 0, TimeSpan.Zero).ToLocalTime();
            }

            DateTimeOffset ret = new DateTimeOffset(File.GetCreationTimeUtc(fullPath)).ToLocalTime();

            return ret;
        }

        #if FEATURE_CORECLR
        [System.Security.SecuritySafeCritical] // auto-generated
        #endif
        public DateTimeOffset GetLastAccessTime(string path) {

            if (path == null)
                throw new ArgumentNullException("path");

            if (path == String.Empty) {
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyPath"), "path");
            }

            Contract.EndContractBlock();

            EnsureStoreIsValid();

            String isPath = GetFullPath(path); // Prepend IS root
            String fullPath = Path.GetFullPathInternal(isPath);

            // Make sure that we have permission to check the directory so we don't
            // paths like ..\..\..\..\Windows
            try {
                Demand(fullPath);
            } catch {
                return new DateTimeOffset(1601, 1, 1, 0, 0, 0, TimeSpan.Zero).ToLocalTime();
            }

            DateTimeOffset ret = new DateTimeOffset(File.GetLastAccessTimeUtc(fullPath)).ToLocalTime();

            return ret;
        }

        #if FEATURE_CORECLR
        [System.Security.SecuritySafeCritical] // auto-generated
        #endif
        public DateTimeOffset GetLastWriteTime(string path) {

            if (path == null)
                throw new ArgumentNullException("path");

            if (path == String.Empty) {
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyPath"), "path");
            }

            Contract.EndContractBlock();

            EnsureStoreIsValid();

            String isPath = GetFullPath(path); // Prepend IS root
            String fullPath = Path.GetFullPathInternal(isPath);

            // Make sure that we have permission to check the directory so we don't
            // paths like ..\..\..\..\Windows
            try {
                Demand(fullPath);
            } catch {
                return new DateTimeOffset(1601, 1, 1, 0, 0, 0, TimeSpan.Zero).ToLocalTime();
            }

            DateTimeOffset ret = new DateTimeOffset(File.GetLastWriteTimeUtc(fullPath)).ToLocalTime();

            return ret;
        }


        public void CopyFile(string sourceFileName, string destinationFileName) {

            if (sourceFileName == null)
                throw new ArgumentNullException("sourceFileName");

            if (destinationFileName == null)
                throw new ArgumentNullException("destinationFileName");

            if (sourceFileName == String.Empty) {
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyPath"), "sourceFileName");
            }

            if (destinationFileName == String.Empty) {
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyPath"), "destinationFileName");
            }

            Contract.EndContractBlock();

            CopyFile(sourceFileName, destinationFileName, false);
        }

        #if FEATURE_CORECLR
        [System.Security.SecuritySafeCritical] // auto-generated
        #endif
        public void CopyFile(string sourceFileName, string destinationFileName, bool overwrite) {

            if (sourceFileName == null)
                throw new ArgumentNullException("sourceFileName");

            if (destinationFileName == null)
                throw new ArgumentNullException("destinationFileName");

            if (sourceFileName == String.Empty) {
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyPath"), "sourceFileName");
            }

            if (destinationFileName == String.Empty) {
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyPath"), "destinationFileName");
            }

            Contract.EndContractBlock();

            EnsureStoreIsValid();

            String sourceFileNameFullPath = Path.GetFullPathInternal(GetFullPath(sourceFileName));
            String destinationFileNameFullPath = Path.GetFullPathInternal(GetFullPath(destinationFileName));

            // Make sure that we have permission to check the directory so we don't
            // paths like ..\..\..\..\Windows
            try {
                Demand(sourceFileNameFullPath);
                Demand(destinationFileNameFullPath);
            } catch (Exception e) {
                throw GetIsolatedStorageException("IsolatedStorage_Operation", e);
            }

#if FEATURE_ISOLATED_STORAGE_QUOTA_ENFORCEMENT
            bool isLocked = false;

            RuntimeHelpers.PrepareConstrainedRegions();
            try {
                Lock(ref isLocked);

                FileInfo sourceInfo = FileInfo.UnsafeCreateFileInfo(sourceFileNameFullPath);
                FileInfo destInfo = FileInfo.UnsafeCreateFileInfo(destinationFileNameFullPath);

                long fileLen = sourceInfo.Length;
                long destLen = destInfo.Exists ? destInfo.Length : 0;

                if (destLen < fileLen) {
                    Reserve(RoundToBlockSize((ulong)(fileLen - destLen)));
                }
#endif // FEATURE_ISOLATED_STORAGE_QUOTA_ENFORCEMENT

                try {
                    File.UnsafeCopy(sourceFileNameFullPath, destinationFileNameFullPath, overwrite);
                } catch (FileNotFoundException) {

#if FEATURE_ISOLATED_STORAGE_QUOTA_ENFORCEMENT
                    // Copying the file failed, undo our reserve.
                    if (destLen < fileLen) {
                        Unreserve(RoundToBlockSize((ulong)(fileLen - destLen)));
                    }
#endif // FEATURE_ISOLATED_STORAGE_QUOTA_ENFORCEMENT

                    throw new FileNotFoundException(Environment.GetResourceString("IO.PathNotFound_Path", sourceFileName));
                } catch (Exception e) {

#if FEATURE_ISOLATED_STORAGE_QUOTA_ENFORCEMENT
                    // Copying the file failed, undo our reserve.
                    if (destLen < fileLen) {
                        Unreserve(RoundToBlockSize((ulong)(fileLen - destLen)));
                    }
#endif // FEATURE_ISOLATED_STORAGE_QUOTA_ENFORCEMENT

                    throw GetIsolatedStorageException("IsolatedStorage_Operation", e);
                } 

#if FEATURE_ISOLATED_STORAGE_QUOTA_ENFORCEMENT
                // If the file we we overwrote was larger than the source file, then we can free some used blocks.
                if (destLen > fileLen && overwrite) {
                    Unreserve(RoundToBlockSizeFloor((ulong)(destLen - fileLen)));
                }

            } finally {
                if (isLocked) {
                    Unlock();
                }
            }
#endif // FEATURE_ISOLATED_STORAGE_QUOTA_ENFORCEMENT


        }

        #if FEATURE_CORECLR
        [System.Security.SecuritySafeCritical] // auto-generated
        #endif
        public void MoveFile(string sourceFileName, string destinationFileName) {

            if (sourceFileName == null)
                throw new ArgumentNullException("sourceFileName");

            if (destinationFileName == null)
                throw new ArgumentNullException("destinationFileName");

            if (sourceFileName == String.Empty) {
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyPath"), "sourceFileName");
            }

            if (destinationFileName == String.Empty) {
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyPath"), "destinationFileName");
            }

            Contract.EndContractBlock();

            EnsureStoreIsValid();

            String sourceFileNameFullPath = Path.GetFullPathInternal(GetFullPath(sourceFileName));
            String destinationFileNameFullPath = Path.GetFullPathInternal(GetFullPath(destinationFileName));

            // Make sure that we have permission to check the directory so we don't
            // paths like ..\..\..\..\Windows
            try {
                Demand(sourceFileNameFullPath);
                Demand(destinationFileNameFullPath);
            } catch (Exception e) {
                throw GetIsolatedStorageException("IsolatedStorage_Operation", e);
            }

            try {
#if !FEATURE_LEGACYNETCF
                File.UnsafeMove(sourceFileNameFullPath, destinationFileNameFullPath);
#else
                // We use the WinRT methods instead of Win32 APIs as this will fix up ACLs when you move the file
                // which matches what phone did.
                s_IsoStoreFileIOHelper.Value.UnsafeMoveFile(sourceFileNameFullPath, destinationFileNameFullPath);
#endif
            } catch (FileNotFoundException) {
                throw new FileNotFoundException(Environment.GetResourceString("IO.PathNotFound_Path", sourceFileName));
            } catch (Exception e) {
                throw GetIsolatedStorageException("IsolatedStorage_Operation", e);
            } 
        }

        #if FEATURE_CORECLR
        [System.Security.SecuritySafeCritical] // auto-generated
        #endif
        public void MoveDirectory(string sourceDirectoryName, string destinationDirectoryName) {

            if (sourceDirectoryName == null)
                throw new ArgumentNullException("sourceDirectoryName");

            if (destinationDirectoryName == null)
                throw new ArgumentNullException("destinationDirectoryName");

            if (sourceDirectoryName == String.Empty) {
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyPath"), "sourceDirectoryName");
            }

            if (destinationDirectoryName == String.Empty) {
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyPath"), "destinationDirectoryName");
            }

            Contract.EndContractBlock();

            EnsureStoreIsValid();

            String sourceDirectoryNameFullPath = Path.GetFullPathInternal(GetFullPath(sourceDirectoryName));
            String destinationDirectoryNameFullPath = Path.GetFullPathInternal(GetFullPath(destinationDirectoryName));

            // Make sure that we have permission to check the directory so we don't
            // paths like ..\..\..\..\Windows
            try {
                Demand(sourceDirectoryNameFullPath);
                Demand(destinationDirectoryNameFullPath);
            } catch (Exception e) {
                throw GetIsolatedStorageException("IsolatedStorage_Operation", e);
            }

            try {
                Directory.UnsafeMove(sourceDirectoryNameFullPath, destinationDirectoryNameFullPath);
            } catch (DirectoryNotFoundException) {
                throw new DirectoryNotFoundException(Environment.GetResourceString("IO.PathNotFound_Path", sourceDirectoryName));
            } catch (Exception e) {
                throw GetIsolatedStorageException("IsolatedStorage_Operation", e);
            }
        }


        /*
         * Public Static Methods
         */
        #if FEATURE_CORECLR
        [System.Security.SecuritySafeCritical] // auto-generated
        #endif
        public static IsolatedStorageFile GetUserStoreForApplication() {
            IsolatedStorageSecurityState s = IsolatedStorageSecurityState.CreateStateToGetGroupAndIdForApplication();
            s.EnsureState();
            return GetUserStore(s.Group, s.Id);
        }

        #if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
        #endif
        internal static IsolatedStorageFile GetUserStore(string group, string id) {

            // This forces the random directories under the root directory given to us by the host to be recreated if they have been
            // deleted.  Pre v4.0 we did this anytime the IsolatedStorageRoot property was accessed, but this was expensive.
            IsolatedStorageRoot = FetchOrCreateRoot();

            if (!IsolatedStorageGroup.Enabled) {
                throw new IsolatedStorageException(Environment.GetResourceString("IsolatedStorage_Init"));
            }

#if !FEATURE_LEGACYNETCF
            IsolatedStorageFile isf = new IsolatedStorageFile();
            isf.m_GroupPath = FetchOrCreateGroup(group, out isf.m_accountingInfo);
            isf.m_GroupName = group;
            isf.m_StorePath = FetchOrCreateStore(group, id, isf);
            isf.m_AppFilesPath = Path.Combine(isf.m_StorePath, s_FilesPathPrefix);
#else
            IsolatedStorageFile isf = new IsolatedStorageFile();
            isf.m_GroupName = group;
            isf.m_AppFilesPath = IsolatedStorageRoot;
#endif
            
            isf.m_AppFilesPathAccess = FileIOAccessFromPath(isf.m_AppFilesPath);

            return isf;
        }

#if !FEATURE_LEGACYNETCF
        #if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
        #endif
        internal static IsolatedStorageFile GetUserStoreFromGroupAndStorePath(string group, string storePath) {
            IsolatedStorageFile isf = new IsolatedStorageFile();
            isf.m_GroupPath = GetGroupPathFromName(group);
            isf.m_GroupName = group;
            isf.m_accountingInfo = new IsolatedStorageAccountingInfo(isf.m_GroupPath);
            isf.m_StorePath = storePath;
            isf.m_AppFilesPath = Path.Combine(isf.m_StorePath, s_FilesPathPrefix);
            isf.m_AppFilesPathAccess = FileIOAccessFromPath(isf.m_AppFilesPath);

            return isf;
        }
#endif

        #if FEATURE_CORECLR
        [System.Security.SecuritySafeCritical] // auto-generated
        #endif
        public static IsolatedStorageFile GetUserStoreForSite() {

#if !FEATURE_LEGACYNETCF
            IsolatedStorageSecurityState s = IsolatedStorageSecurityState.CreateStateToGetGroupAndIdForSite();
            s.EnsureState();
            return GetUserStore(s.Group, s.Id);
#else // !FEATURE_LEGACYNETCF
            // Legacy NetCF didn't make a distinction between Apps and Sites, they both used the Application identity.
            return GetUserStoreForApplication();
#endif // !FEATURE_LEGACYNETCF

        }


        /*
         * Private Instance Methods
         */
        internal string GetFullPath(string partialPath) {

            Contract.Assert(partialPath != null, "partialPath should be non null");

            int i;

            // Chop off directory separator characters at the start of the string because they counfuse Path.Combine.
            for(i = 0; i < partialPath.Length; i++) {
                if(partialPath[i] != Path.DirectorySeparatorChar && partialPath[i] != Path.AltDirectorySeparatorChar) {
                    break;
                }
            }

            partialPath = partialPath.Substring(i);

            return Path.Combine(m_AppFilesPath, partialPath);
        }

#if FEATURE_ISOLATED_STORAGE_QUOTA_ENFORCEMENT

        #if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
        #endif
        internal void Reserve(ulong lReserve) {

            lock(m_internalLock) {

                FileLock groupLock = GetGroupFileLock();

                try {
                    groupLock.Lock();
                    long oldUsed = m_accountingInfo.UsedSize;
                    long quota = m_accountingInfo.Quota;
                    if(oldUsed + (long) lReserve > quota) {
                        throw new IsolatedStorageException(Environment.GetResourceString("IsolatedStorage_UsageWillExceedQuota"));
                    }
                    long newUsed = oldUsed + (long) lReserve;
                    m_accountingInfo.UsedSize = newUsed;
                } catch(IOException e) {
                    throw GetIsolatedStorageException("IsolatedStorage_Operation", e);
                } catch (UnauthorizedAccessException e) {
                    throw GetIsolatedStorageException("IsolatedStorage_Operation", e);
                } finally {
                    groupLock.Unlock();
                }
            }
        }

        #if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
        #endif
        internal void Unreserve(ulong lFree) {

            lock(m_internalLock) {

                FileLock groupLock = GetGroupFileLock();

                try {
                    groupLock.Lock();
                    long oldUsed = m_accountingInfo.UsedSize;
                    long newUsed = oldUsed - (long) lFree;
                    Contract.Assert(newUsed >= 0, "Unreserve is making quota negative!");
                    m_accountingInfo.UsedSize = newUsed;
                } catch(IOException e) {
                    throw GetIsolatedStorageException("IsolatedStorage_Operation", e);
                } catch (UnauthorizedAccessException e) {
                    throw GetIsolatedStorageException("IsolatedStorage_Operation", e);
                } finally {
                    groupLock.Unlock();
                }                
            }
        }


        #if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
        #endif
        internal void Lock(ref bool locked)
        {
            locked = false;

            FileLock groupLock = GetGroupFileLock();
            
            lock (m_internalLock) {               
                groupLock.Lock();
                locked = true;
            }
        }

        #if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
        #endif
        internal void Unlock() {

            FileLock groupLock = GetGroupFileLock();

            lock(m_internalLock) {
                groupLock.Unlock();
            }
        }

#endif // FEATURE_ISOLATED_STORAGE_QUOTA_ENFORCEMENT

        /*
         * Private Static Methods
         */
        #if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
        #endif
        private static void CreatePathPrefixIfNeeded(string path) {

            string root = Path.GetPathRoot(path);

            Contract.Assert(!String.IsNullOrEmpty(root), "Path.GetPathRoot returned null or empty for: " + path);

            try {

                if (!Directory.UnsafeExists(path)) {
                    Directory.UnsafeCreateDirectory(path);
                }

            } catch (IOException e) {
                throw GetIsolatedStorageException("IsolatedStorage_Operation", e);
            } catch (UnauthorizedAccessException e) {
                throw GetIsolatedStorageException("IsolatedStorage_Operation", e);
            }

        }
 
        #if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
        #endif
        internal static string FetchOrCreateRoot() {

            string rootFromHost = s_RootFromHost;

            if (s_RootFromHost == null) {
                IsolatedStorageSecurityState s = IsolatedStorageSecurityState.CreateStateToGetRootUserDirectory();
                s.EnsureState();
                string root = s.RootUserDirectory;

#if FEATURE_LEGACYNETCF            
                IsolatedStorageSecurityState s2 = IsolatedStorageSecurityState.CreateStateForIsolatedStorageFolderName();
                if(s2.IsStateAvailable()) {
                    if(s2.IsolatedStorageFolderName != null) {
                        root = Path.Combine(root , s2.IsolatedStorageFolderName);
                    } else {
                        root = Path.Combine(root , "IsolatedStore");
                    } 
                } else {
                    root = Path.Combine(root, "IsolatedStore");
                }            
#endif
                s_RootFromHost = root;
            }

            CreatePathPrefixIfNeeded(s_RootFromHost);

#if !FEATURE_LEGACYNETCF
            FileLock rootLock = null;
            try {
                rootLock = FileLock.GetFileLock(s_RootFromHost);
                rootLock.Lock();

                string obfuscatedRootDir = GetRandomDirectory(s_RootFromHost);

                if (obfuscatedRootDir == null) {
                    obfuscatedRootDir = CreateRandomDirectory(s_RootFromHost);
                }

                obfuscatedRootDir = Path.Combine(obfuscatedRootDir, c_VersionPrefix);

                if (!Directory.UnsafeExists(Path.Combine(Path.Combine(s_RootFromHost, obfuscatedRootDir), s_LockPathPrefix))) {
                    Directory.UnsafeCreateDirectory(Path.Combine(Path.Combine(s_RootFromHost, obfuscatedRootDir), s_LockPathPrefix));
                }

                return Path.Combine(s_RootFromHost, obfuscatedRootDir);

            } catch (IOException e) {
                // We don't want to leak any information here
                // Throw a store initialization exception instead
                throw GetIsolatedStorageException("IsolatedStorage_Init", e);
            } catch (UnauthorizedAccessException e) {
                // We don't want to leak any information here
                // Throw a store initialization exception instead
                throw GetIsolatedStorageException("IsolatedStorage_Init", e);
            } finally {
                if (rootLock != null) {
                    rootLock.Unlock();
                }
            }
#else // !FEATURE_LEGACYNETCF
            return s_RootFromHost;
#endif // !FEATURE_LEGACYNETCF

        }

        // creates and returns the relative path to the random directory string without the path separator
#if FEATURE_CORRUPTING_EXCEPTIONS
        #if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
        #else
        [System.Security.SecuritySafeCritical]
        #endif
        
        [HandleProcessCorruptedStateExceptions] 
#endif // FEATURE_CORRUPTING_EXCEPTIONS
        internal static string CreateRandomDirectory(String rootDir) {
            string rndName;
            string dirToCreate;
            do {
                rndName = Path.Combine(Path.GetRandomFileName(), Path.GetRandomFileName());
                dirToCreate = Path.Combine(rootDir, rndName);
            } while(Directory.UnsafeExists(dirToCreate));
            // Note that there is still a small window (between where we check for .Exists and execute the .CreateDirectory)
            // when another process can come up with the same random name and create that directory.
            // That's potentially a security hole, but the odds of that are low enough that the risk is acceptable.
            try {
                Directory.UnsafeCreateDirectory(dirToCreate);
            } catch (Exception e) {
                // We don't want to leak any information here
                // Throw a store initialization exception instead
                throw GetIsolatedStorageException("IsolatedStorage_Init", e);
            }
            return rndName;
        }

        // returns the relative path to the current random directory string if one is there without the path separator
        #if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
        #endif
        internal static string GetRandomDirectory(String rootDir) {
            String[] nodes1 = GetFileDirectoryNames(Path.Combine(rootDir, "*"), "*", false, null);
            // First see if there is a new store 
            for(int i = 0; i < nodes1.Length; ++i) {
                if(nodes1[i].Length == 12) {
                    String[] nodes2 = GetFileDirectoryNames(Path.Combine(Path.Combine(rootDir, nodes1[i]), "*"), "*", false, null);
                    for(int j = 0; j < nodes2.Length; ++j) {
                        if(nodes2[j].Length == 12) {
                            return (Path.Combine(nodes1[i], nodes2[j])); // Get the first directory
                        }
                    }
                }
            }

            return null;
        }
        

#if !FEATURE_LEGACYNETCF
        #if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
        #endif
        private static string FetchOrCreateGroup(string groupName, out IsolatedStorageAccountingInfo accountInfo) {
            return FetchOrCreateGroup(groupName, out accountInfo, true);
        }

        #if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
        #endif
        private static string GetGroupPathFromName(string groupName) {
            string obfuscatedGroupName = GetHash(groupName);
            string groupRootPath = Path.Combine(IsolatedStorageRoot, Path.Combine(s_GroupPathPrefix, obfuscatedGroupName));

            return groupRootPath;
        }

        #if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
        #endif
        private static bool DeleteStoresForGroup(string obfuscatedGroupName) {
            if (Directory.UnsafeExists(Path.Combine(IsolatedStorageRoot, s_StorePathPrefix))) {
                foreach (string storePath in Directory.UnsafeGetDirectories(Path.Combine(IsolatedStorageRoot, s_StorePathPrefix), "*", SearchOption.TopDirectoryOnly)) {
                    if (File.UnsafeExists(Path.Combine(storePath, s_GroupFileName)) && (File.UnsafeReadAllText(Path.Combine(storePath, s_GroupFileName))) == obfuscatedGroupName) {
                        if (!CleanDirectoryNoUnreserve(storePath)) {
                            // We couldn't clean an existing store that belongs to this group.  So we will fail to create the group, doing
                            // so would mess up our bookkeeping information.
                            return false;
                        }

                        Directory.UnsafeDelete(storePath, false);
                    }
                }
            }

            return true;
        }

        #if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
        #endif
        private static string FetchOrCreateGroup(string groupName, out IsolatedStorageAccountingInfo accountInfo, bool retry) {
            string obfuscatedGroupName = GetHash(groupName);
            string groupRootPath = GetGroupPathFromName(groupName);

            FileLock rootLock = FileLock.GetFileLock(IsolatedStorageRoot);

            try {
                rootLock.Lock();

                if(Directory.UnsafeExists(groupRootPath)) {
                    if(File.UnsafeExists(Path.Combine(groupRootPath, s_CleanupFileName))) {
                        if (retry) {
                            // The IsolatedStorageGroup object we construct here has dummy data for the Quota and Used Size,
                            // But it doesn't really matter, since we won't use that information for anything.  We just want
                            // to use the Group's Remove method.
                            (new IsolatedStorageGroup(groupName, 0, 0, groupRootPath)).Remove();
                            return FetchOrCreateGroup(groupName, out accountInfo, false);
                        } else {
                            throw new IsolatedStorageException(Environment.GetResourceString("IsolatedStorage_Init"));
                        }
                    } else {

                        // We should ensure that the id.dat, quota.dat and used.dat files exist.
                        if (!File.UnsafeExists(Path.Combine(groupRootPath, s_IdFileName))) {
                            // We can simply recreate the id.dat file if it is missing
                            File.UnsafeWriteAllText(Path.Combine(groupRootPath, s_IdFileName), groupName);
                        } else {
                            // Check for colision.
                            if (!groupName.Equals(File.UnsafeReadAllText(Path.Combine(groupRootPath, s_IdFileName)))) {
                                throw new IsolatedStorageException(Environment.GetResourceString("IsolatedStorage_Init"));
                            }
                        }

                        if (!IsolatedStorageAccountingInfo.IsAccountingInfoValid(groupRootPath)) {
                            // We'll delete all the stuff tied to this group and recreate these files.

                            if (!DeleteStoresForGroup(obfuscatedGroupName)) {
                                // We couldn't clean an existing store that belongs to this group.  So we will fail to create the group, doing
                                // so would mess up our bookkeeping information.
                                throw new IsolatedStorageException(Environment.GetResourceString("IsolatedStorage_Init"));
                            }
                        }

                        accountInfo = new IsolatedStorageAccountingInfo(groupRootPath);

                        return groupRootPath;
                    }
                } else {
                    // We might be in a weird state where the "g" directory got deleted but there are stores still in that group around
                    // This could happen if a someone deleted the "g" directory with Windows Explorer or Finder or something.  We should 
                    // ensure there are no stores for this group here.
                    if (!DeleteStoresForGroup(obfuscatedGroupName)) {
                        // We couldn't clean an existing store that belongs to this group.  So we will fail to create the group, doing
                        // so would mess up our bookkeeping information.
                        throw new IsolatedStorageException(Environment.GetResourceString("IsolatedStorage_Init"));
                    }

                    Directory.UnsafeCreateDirectory(groupRootPath);
                    TouchFile(Path.Combine(groupRootPath, s_CleanupFileName));
                    
                    File.UnsafeWriteAllText(Path.Combine(groupRootPath, s_IdFileName), groupName);
                    File.UnsafeDelete(Path.Combine(groupRootPath, s_CleanupFileName));

                    accountInfo = new IsolatedStorageAccountingInfo(groupRootPath);

                    return groupRootPath;
                }
            } catch(IOException e) {
                throw GetIsolatedStorageException("IsolatedStorage_Init", e);
            } catch (UnauthorizedAccessException e) {
                throw GetIsolatedStorageException("IsolatedStorage_Init", e);
            } finally {
                rootLock.Unlock();
            }
        }

        #if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
        #endif
        private static string FetchOrCreateStore(string groupName, string storeName, IsolatedStorageFile isf) {
            string groupRootPath = GetGroupPathFromName(groupName);
            string obfuscatedStoreName = GetHash(storeName);
            string obfuscatedGroupName = GetHash(groupName);
            string storeRootPath = Path.Combine(IsolatedStorageRoot, Path.Combine(s_StorePathPrefix, obfuscatedStoreName));

            FileLock rootLock = FileLock.GetFileLock(IsolatedStorageRoot);

            try {
                rootLock.Lock();

                if(Directory.UnsafeExists(storeRootPath)) {

                    if (!File.UnsafeExists(Path.Combine(storeRootPath, s_IdFileName))) {
                        File.UnsafeWriteAllText(Path.Combine(storeRootPath, s_IdFileName), storeName);
                    } else {
                        if (!storeName.Equals(File.UnsafeReadAllText(Path.Combine(storeRootPath, s_IdFileName)))) {
                            throw new IsolatedStorageException(Environment.GetResourceString("IsolatedStorage_Init"));
                        }
                    }

                    File.UnsafeWriteAllText(Path.Combine(storeRootPath, s_GroupFileName), obfuscatedGroupName);
                    

                    if (!Directory.UnsafeExists(Path.Combine(storeRootPath, s_FilesPathPrefix))) {
                        Directory.UnsafeCreateDirectory(Path.Combine(storeRootPath, s_FilesPathPrefix));
                    }

                    if(File.UnsafeExists(Path.Combine(storeRootPath, s_CleanupFileName))) {
                        bool removedAll = isf.CleanDirectory(Path.Combine(storeRootPath, s_FilesPathPrefix));

                        if(removedAll) {
                            File.UnsafeDelete(Path.Combine(storeRootPath, s_CleanupFileName));
                            return storeRootPath;
                        } else {
                            throw new IsolatedStorageException(Environment.GetResourceString("IsolatedStorage_Init"));
                        }
                    } else {
                        return storeRootPath;
                    }
                } else {
                    isf.Reserve(s_DirSize);
                    Directory.UnsafeCreateDirectory(storeRootPath);
                    TouchFile(Path.Combine(storeRootPath, s_CleanupFileName));

                    Directory.UnsafeCreateDirectory(Path.Combine(storeRootPath, s_FilesPathPrefix));
                    File.UnsafeWriteAllText(Path.Combine(storeRootPath, s_GroupFileName), obfuscatedGroupName);
                    File.UnsafeWriteAllText(Path.Combine(storeRootPath, s_IdFileName), storeName);
                    File.UnsafeDelete(Path.Combine(storeRootPath, s_CleanupFileName));
                    return storeRootPath;
                }
            } catch(IOException e) {
                throw GetIsolatedStorageException("IsolatedStorage_Init", e);
            } catch (UnauthorizedAccessException e) {
                throw GetIsolatedStorageException("IsolatedStorage_Init", e);
            } finally {
                if(rootLock != null) {
                    rootLock.Unlock();
                }
            }
        }
#endif // !FEATURE_LEGACYNETCF

        #if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
        #endif
        internal static void TouchFile(string pathToFile) {
            using (FileStream fs = new FileStream(pathToFile, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None, FileStream.DefaultBufferSize, false)) {
                // We just need the file to be created.
            }
        }

        #if FEATURE_CORECLR
        [System.Security.SecuritySafeCritical] // auto-generated
        #endif
        internal void EnsureStoreIsValid() {
            if(Disposed)
                throw new ObjectDisposedException(null, Environment.GetResourceString("IsolatedStorage_StoreNotOpen"));
            Contract.EndContractBlock();

            if(IsDeleted) {
                throw new IsolatedStorageException(Environment.GetResourceString("IsolatedStorage_StoreNotOpen"));
            }

            if(m_closed)
                throw new InvalidOperationException(Environment.GetResourceString("IsolatedStorage_StoreNotOpen"));

            if (!IsolatedStorageGroup.Enabled) {
                throw new IsolatedStorageException(Environment.GetResourceString("IsolatedStorage_StoreNotOpen"));
            }
        }

#if FEATURE_ISOLATED_STORAGE_QUOTA_ENFORCEMENT
        // Utility Functions (Common With IsolatedStorageFile):
        #if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
        #endif
        internal void UndoReserveOperation(ulong oldLen, ulong newLen) {
            oldLen = RoundToBlockSize(oldLen);
            if(newLen > oldLen)
                Unreserve(RoundToBlockSize(newLen - oldLen));
        }

        #if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
        #endif
        internal void Reserve(ulong oldLen, ulong newLen) {
            oldLen = RoundToBlockSize(oldLen);
            if(newLen > oldLen)
                Reserve(RoundToBlockSize(newLen - oldLen));
        }

        #if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
        #endif
        internal void ReserveOneBlock() {
            Reserve(s_BlockSize);
        }

        #if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
        #endif
        internal void UnreserveOneBlock() {
            Unreserve(s_BlockSize);
        }
#endif // FEATURE_ISOLATED_STORAGE_QUOTA_ENFORCEMENT

        internal static ulong RoundToBlockSize(ulong num) {
            if(num < s_BlockSize)
                return s_BlockSize;

            ulong rem = (num % s_BlockSize);

            if(rem != 0)
                num += (s_BlockSize - rem);

            return num;
        }

        internal static ulong RoundToBlockSizeFloor(ulong num) {
            if (num < s_BlockSize)
                return 0;

            ulong rem = (num % s_BlockSize);
            num -= rem;

            return num;
        }

        // Given a path to a dir to create, will return the list of directories to create and the last one in the array is the actual dir to create.
        // for example if dir is a\\b\\c and none of them exist, the list returned will be a, a\\b, a\\b\\c.
        #if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
        #endif
        private String[] DirectoriesToCreate(String fullPath) {
            Contract.Ensures(Contract.Result<string[]>() == null || Contract.Result<string[]>().Length > 0);

            List<String> list = new List<String>();
            int length = fullPath.Length;

            // We need to trim the trailing slash or the code will try to create 2 directories of the same name.
            if(length >= 2 && fullPath[length - 1] == Path.DirectorySeparatorChar)
                length--;
            int i = Path.GetRootLength(fullPath);

            // Attempt to figure out which directories don't exist
            while(i < length) {
                i++;
                while(i < length && fullPath[i] != Path.DirectorySeparatorChar)
                    i++;
                String currDir = fullPath.Substring(0, i);

                if(!Directory.InternalExists(currDir)) { // Create only the ones missing
                    list.Add(currDir);
                }
            }

            if(list.Count != 0) {
                return list.ToArray();
            }
            return null;
        }


        [System.Security.SecurityCritical]
        private static FileIOAccess FileIOAccessFromPath(string fullPath) {
            FileIOAccess access = new FileIOAccess();

#if FEATURE_WINDOWSPHONE
            // Due to an interaction between the ACLs on the phone for the directories isolated storage lives in
            // and GetLongPathNameW (which StringExpressionSet.CreateListFromExpressions ends up calling if the
            // path name has a ~ in it), we remove all ~'s before constructing the path to demand.
            //
            // The risk here is the case where there's a short file name that resolves to a path not under the root
            // of Isolated Storage, but in that case either the Win32 ACLs will prevent access or the user could
            // have accessed the files anyway.  Since Isolated Storage is not a security boundary in Windows Phone
            // we are not concerned about this case.
            fullPath = fullPath.Replace("~", "");
#endif

            ArrayList expressions = StringExpressionSet.CreateListFromExpressions(new string[] { fullPath }, true);
            access.AddExpressions(expressions, false);
            return access;
        }

        [System.Security.SecurityCritical]
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal void Demand(String pathToDemand) {
            try {
                FileIOAccess target = FileIOAccessFromPath(pathToDemand);
                if(!target.IsSubsetOf(m_AppFilesPathAccess))
                {
                    throw new SecurityException();
                }
            } catch (Exception) {
                // We couldn't construct a FileIOAccess object because the path was bad.
                throw new SecurityException();
            }
        }

        internal static String GetHash(String s) {
            byte[] preHash = (new System.Security.Cryptography.SHA256Managed()).ComputeHash(Encoding.Unicode.GetBytes(s));
            if (preHash.Length % 5 != 0) {
                byte[] b = new byte[preHash.Length + (5 - (preHash.Length % 5))];
                for (int i = 0; i < preHash.Length; i++) {
                    b[i] = preHash[i];
                }

                preHash = b;
            }
            return Path.ToBase32StringSuitableForDirName(preHash);
        }
        
        // From IO.Directory class (make that internal if possible)
        #if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
        #endif
        private static String[] GetFileDirectoryNames(String path, String msg, bool file, IsolatedStorageFile isf) {
            int hr;

            if(path == null) throw new ArgumentNullException("path", Environment.GetResourceString("ArgumentNull_Path"));
            Contract.EndContractBlock();

            bool fEndsWithDirectory = false;
            char lastChar = path[path.Length - 1];
            if(lastChar == Path.DirectorySeparatorChar ||
                lastChar == Path.AltDirectorySeparatorChar ||
                lastChar == '.')
                fEndsWithDirectory = true;


            // Get an absolute path and do a security check
            String fullPath = Path.GetFullPathInternal(path);

            // GetFullPath() removes '\', "\." etc from path, we will restore 
            // it here. If path ends in a trailing slash (\), append a * 
            // or we'll  get a "Cannot find the file specified" exception
            if((fEndsWithDirectory) &&
                (fullPath[fullPath.Length - 1] != lastChar))
                fullPath += "\\*";

            // Check for read permission to the directory, not to the contents.
            String dir = Path.GetDirectoryName(fullPath);

            if(dir != null)
                dir += "\\";

            if(isf != null) {
                try {
                    isf.Demand(dir == null ? fullPath : dir);
                } catch (Exception e) {
                    throw GetIsolatedStorageException("IsolatedStorage_Operation", e);
                }
            }

            if(CompatibilitySwitches.IsAppEarlierThanWindowsPhoneMango)
            {
                // Pre Mango Windows Phone had very odd behavior for this function.  It would take the parent directory of the search pattern and do a *
                // in there.  That means something like GetDirectories("Dir1") would be treated as GetDirectories("*") and GetDirectories("Dir2\Dir3") would be
                // treated as GetDirectories("Dir2\*").

                // This also means that GetDirectories("") returned "IsolatedStorage" since it was looking at the directory above the root of Isolated Storage.
                fullPath = Path.Combine(Path.GetDirectoryName(fullPath), "*");                
            }

            String[] list = new String[10];
            int listSize = 0;
            Win32Native.WIN32_FIND_DATA data = new Win32Native.WIN32_FIND_DATA();

            // Open a Find handle 
            SafeFindHandle hnd = Win32Native.FindFirstFile(fullPath, data);
            if(hnd.IsInvalid) {
                // Calls to GetLastWin32Error overwrites HResult.  Store HResult.
                hr = Marshal.GetLastWin32Error();
                if(hr == Win32Native.ERROR_FILE_NOT_FOUND)
                    return new String[0];

                // Mango would throw DirectoryNotFoundException if we got ERROR_PATH_NOT_FOUND instead of IsolatedStorageException
                if(CompatibilitySwitches.IsAppEarlierThanWindowsPhone8 && hr == Win32Native.ERROR_PATH_NOT_FOUND)
                    __Error.WinIOError(hr, msg);

#if FEATURE_ISOSTORE_LIGHT
                throw GetIsolatedStorageException("IsolatedStorage_Operation", Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error(), new IntPtr(-1)));
#else
                __Error.WinIOError(hr, msg);
#endif
            }

            // Keep asking for more matching files, adding file names to list
            int numEntries = 0;  // Number of directory entities we see.
            do {
                bool includeThis;  // Should this file/directory be included in the output?
                if(file)
                    includeThis = (0 == (data.dwFileAttributes & Win32Native.FILE_ATTRIBUTE_DIRECTORY));
                else {
                    includeThis = (0 != (data.dwFileAttributes & Win32Native.FILE_ATTRIBUTE_DIRECTORY));
                    // Don't add "." nor ".."
                    if(includeThis && (data.cFileName.Equals(".") || data.cFileName.Equals("..")))
                        includeThis = false;
                }

                if(includeThis) {
                    numEntries++;
                    if(listSize == list.Length) {
                        String[] newList = new String[list.Length * 2];
                        Array.Copy(list, 0, newList, 0, listSize);
                        list = newList;
                    }
                    list[listSize++] = data.cFileName;
                }

            } while(Win32Native.FindNextFile(hnd, data));

            // Make sure we quit with a sensible error.
            hr = Marshal.GetLastWin32Error();
            hnd.Close();  // Close Find handle in all cases.
            if(hr != 0 && hr != Win32Native.ERROR_NO_MORE_FILES) __Error.WinIOError(hr, msg);

            // Check for a string such as "C:\tmp", in which case we return
            // just the directory name.  FindNextFile fails first time, and
            // data still contains a directory.
            if(!file && numEntries == 1 && (0 != (data.dwFileAttributes & Win32Native.FILE_ATTRIBUTE_DIRECTORY))) {
                String[] sa = new String[1];
                sa[0] = data.cFileName;
                return sa;
            }

            // Return list of files/directories as an array of strings
            if(listSize == list.Length)
                return list;
            String[] items = new String[listSize];
            Array.Copy(list, 0, items, 0, listSize);
            return items;
        }

        public void Dispose() {
            Close();
            m_bDisposed = true;
        }

        [SecurityCritical]
        internal static Exception GetIsolatedStorageException(string exceptionKey, Exception rootCause) {
#if DEBUG
            IsolatedStorageException e = new IsolatedStorageException(Environment.GetResourceString(exceptionKey), rootCause);
#else
            Exception innerException = null;

#if !FEATURE_LEGACYNETCF
            if (IsolatedStorageSecurityState.CreateStateToCheckSetInnerException().IsStateAvailable()) {
                innerException = rootCause;
            }
#endif

            IsolatedStorageException e = new IsolatedStorageException(Environment.GetResourceString(exceptionKey), innerException);
#endif
            e.m_UnderlyingException = rootCause;

            return e;
        }

#if FEATURE_LEGACYNETCF
        [SecuritySafeCritical]
        internal static IsolatedStorageFileIOHelperBase GetIsolatedStorageFileIOHelper()
        {
            Type WinRTResourceManagerType = Type.GetType("System.IO.IsolatedStorage.IsolatedStorageFileIOHelper, " + AssemblyRef.SystemRuntimeWindowsRuntime, true);
            return (IsolatedStorageFileIOHelperBase)Activator.CreateInstance(WinRTResourceManagerType, true);
        }
#endif // FEATURE_LEGACYNETCF

    }


#if FEATURE_LEGACYNETCF
    //
    // This is implemented in System.Runtime.WindowsRuntime as function System.IO.IsolatedStorage.IsolatedStorageFileIOHelper,
    // allowing us to use WinRT to implement MoveFile.
    // Ideally this would be an interface, or at least an abstract class - but neither seems to play nice with FriendAccessAllowed.
    //
    public class IsolatedStorageFileIOHelperBase
    {
        [SecurityCritical]
        public virtual void UnsafeMoveFile(string sourceFileName, string destinationFileName) { }
    }
#endif

#if !FEATURE_LEGACYNETCF
    internal class FileLock {

        private const string s_LockFileName = "lock.dat";

        private string m_FileLockName;
        private Mutex m_Mutex;
        private bool m_HaveLock;

        private int m_LockCount = 0; 

        [ThreadStatic]
        private static Dictionary<string, FileLock> cache;

        private static object s_LockObject = new object();

        [SecurityCritical]
        private FileLock(string pathToLock, string lockFileName) {
            m_FileLockName = Path.Combine(pathToLock, lockFileName);
            m_Mutex = GetMutexWithAcl(IsolatedStorageFile.GetHash(m_FileLockName));
        }

        [SecurityCritical]
        private static unsafe Mutex GetMutexWithAcl(string mutexName) {
            // We need to set at a DACL on the Mutex we're going to create to allow it to be shared.  
            // The DACL here grants MUTEX_ALL_ACCESS (0x001F0001) to the current user.  This allows hosts
            // like sllaucher.exe which strip the admin sections of the current user token to access the
            // Mutex.
            IntPtr byteArray = IntPtr.Zero;
            uint byteArraySize = 0;
            try {
                if (Win32Native.ConvertStringSdToSd(String.Format(CultureInfo.InvariantCulture, "D:(A;;0x001F0001;;;{0})", GetCurrentSIDAsString()), 1 /*SDDL_REVISION_1*/, out byteArray, ref byteArraySize) == 0) {
                    Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error(), new IntPtr(-1));
                }

                Win32Native.SECURITY_ATTRIBUTES secAttrs = new Win32Native.SECURITY_ATTRIBUTES();
                secAttrs.nLength = (int)Marshal.SizeOf(secAttrs);
                // The Win32 function ConvertStringSecurityDescriptorToSecurityDescriptor allocated the buffer pointed to by byteArray
                // it isn't tracked by the GC so we don't need to pin it.
                secAttrs.pSecurityDescriptor = (byte*)byteArray.ToPointer();
                bool createdNew; /* not used */
                return new Mutex(false, mutexName, out createdNew, secAttrs);
            } finally {
                if (byteArray != IntPtr.Zero) {
                    Win32Native.LocalFree(byteArray);
                }
            }
        }

        // This code mimics the behavior of WindowsIdentity.GetCurrent().User.ToString() but doesn't take a
        // dependency on the WindowsIdentity class because it depends on code not in Silverlight.
        //
        // The function works by getting the current token secruity token and getting the TOKEN_USER
        // structure from the token.  This contains the SID, which we can pass to ConvertSidToStringSid.
        //
        [SecurityCritical]
        private static string GetCurrentSIDAsString() {
            using (SafeTokenHandle hToken = GetCurrentToken(TokenAccessLevels.Query)) {
                using (SafeLocalAllocHandle tokenUser = GetTokenUserFromToken(hToken)) {
                    IntPtr pStrSid = IntPtr.Zero;
                    try {
                        if (!Win32Native.ConvertSidToStringSid(Marshal.ReadIntPtr(tokenUser.DangerousGetHandle()), ref pStrSid)) {
                            Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error(), new IntPtr(-1));
                        }
                        return Marshal.PtrToStringUni(pStrSid);
                    } finally {
                        if (pStrSid != IntPtr.Zero) {
                            Win32Native.LocalFree(pStrSid);
                        }
                    }
                }
            }
        }

        [SecurityCritical]  // auto-generated
        private static SafeLocalAllocHandle GetTokenUserFromToken(SafeTokenHandle tokenHandle) {
            SafeLocalAllocHandle safeLocalAllocHandle = SafeLocalAllocHandle.InvalidHandle;
            uint dwLength = (uint)Marshal.SizeOf(typeof(uint));
            bool result = Win32Native.GetTokenInformation(tokenHandle,
                                                          1 /* TokenInformationClass.TokenUser */,
                                                          safeLocalAllocHandle,
                                                          0,
                                                          out dwLength);
            int dwErrorCode = Marshal.GetLastWin32Error();
            switch (dwErrorCode) {
                case Win32Native.ERROR_INSUFFICIENT_BUFFER:
                    // ptrLength is an [In] param to LocalAlloc 
                    UIntPtr ptrLength = new UIntPtr(dwLength);
                    safeLocalAllocHandle = Win32Native.LocalAlloc(Win32Native.LMEM_FIXED, ptrLength);
                    if (safeLocalAllocHandle == null || safeLocalAllocHandle.IsInvalid)
                        throw new OutOfMemoryException();
                    safeLocalAllocHandle.Initialize(dwLength);

                    result = Win32Native.GetTokenInformation(tokenHandle,
                                                             1 /* TokenInformationClass.TokenUser */,
                                                             safeLocalAllocHandle,
                                                             dwLength,
                                                             out dwLength);
                    if (!result) {
                        Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error(), new IntPtr(-1));
                    }
                    break;
                default:
                    Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error(), new IntPtr(-1));
                    break;
            }
            return safeLocalAllocHandle;
        }

        [SecurityCritical]  // auto-generated
        private static SafeTokenHandle GetCurrentToken(TokenAccessLevels desiredAccess) {
            int lastError;
            SafeTokenHandle safeTokenHandle = GetCurrentThreadToken(desiredAccess, out lastError);
            if (safeTokenHandle.IsInvalid && lastError == Win32Native.ERROR_NO_TOKEN) {
                safeTokenHandle = GetCurrentProcessToken(desiredAccess, out lastError);
                if (safeTokenHandle.IsInvalid) {
                    Marshal.ThrowExceptionForHR(lastError);
                }
            }
            return safeTokenHandle;
        }

        [SecurityCritical]  // auto-generated
        private static SafeTokenHandle GetCurrentProcessToken(TokenAccessLevels desiredAccess, out int lastError) {
            SafeTokenHandle safeTokenHandle;
            Win32Native.OpenProcessToken(Win32Native.GetCurrentProcess(), desiredAccess, out safeTokenHandle);
            lastError = Marshal.GetLastWin32Error();
            return safeTokenHandle;
        }

        [SecurityCritical]  // auto-generated
        private static SafeTokenHandle GetCurrentThreadToken(TokenAccessLevels desiredAccess, out int lastError) {
            SafeTokenHandle safeTokenHandle;
            Win32Native.OpenThreadToken(Win32Native.GetCurrentThread(), desiredAccess, true, out safeTokenHandle);
            lastError = Marshal.GetLastWin32Error();
            return safeTokenHandle;
        }

        [SecurityCritical]
        public static FileLock GetFileLock(string pathToLock) {
            return GetFileLock(pathToLock, s_LockFileName);
        }

        [SecurityCritical]
        public static FileLock GetFileLock(string pathToLock, string lockFileName) {
            lock (s_LockObject) {
                if (cache == null) {
                    cache = new Dictionary<string, FileLock>();
                }

                if (!cache.ContainsKey(pathToLock)) {
                    cache[pathToLock] = new FileLock(pathToLock, lockFileName);
                }

                return cache[pathToLock];
            }
        }

        #if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
        #endif
        public void Lock() {
            try {
                lock (this) {
                    if (m_LockCount == 0) {
                        m_HaveLock = m_Mutex.WaitOne(5000);
                        if (!m_HaveLock) {
                            // Couldn't obtain lock!
                            BCLDebug.Assert(false, "Couldn't obtain Lock on: " + m_FileLockName);
                            throw new IsolatedStorageException(Environment.GetResourceString("IsolatedStorage_Operation"));
                        }
                    } 
                }
            } finally {
                // We increment m_LockCount even in the case where we throw for Lock because upstack code will do an Unlock call in their finally block.
                m_LockCount++;
            }
        }

        #if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
        #endif
        public void Unlock() {
            lock (this) {
                if (m_LockCount == 1) {
                    if (m_HaveLock) {
                        m_Mutex.ReleaseMutex();
                        m_HaveLock = false;
                    }
                }

                m_LockCount--;
            }
        }
    }
#endif // !FEATURE_LEGACYNETCF
}

