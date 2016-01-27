// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** 
** 
**
**
** Purpose: Exposes routines for enumerating through a 
** directory.
**
**          April 11,2000
**
===========================================================*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Security;
#if FEATURE_MACL
using System.Security.AccessControl;
#endif
using System.Security.Permissions;
using Microsoft.Win32;
using System.Text;
using System.Runtime.InteropServices;
using System.Globalization;
using System.Runtime.Serialization;
using System.Runtime.Versioning;
using System.Diagnostics.Contracts;

namespace System.IO {
    [Serializable]
    [ComVisible(true)]
    public sealed class DirectoryInfo : FileSystemInfo {
        private String[] demandDir;

#if FEATURE_CORECLR
         // Migrating InheritanceDemands requires this default ctor, so we can annotate it.
#if FEATURE_CORESYSTEM
        [System.Security.SecurityCritical]
#else
        [System.Security.SecuritySafeCritical]
#endif //FEATURE_CORESYSTEM
        private DirectoryInfo(){}


        [System.Security.SecurityCritical]
        public static DirectoryInfo UnsafeCreateDirectoryInfo(String path)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            Contract.EndContractBlock();

            DirectoryInfo di = new DirectoryInfo();
            di.Init(path, false);
            return di;
        }
#endif

        [System.Security.SecuritySafeCritical]
        public DirectoryInfo(String path)
        {
            if (path==null)
                throw new ArgumentNullException("path");
            Contract.EndContractBlock();

            Init(path, true);
        }

        [System.Security.SecurityCritical]
        private void Init(String path, bool checkHost)
        {
            // Special case "<DriveLetter>:" to point to "<CurrentDirectory>" instead
            if ((path.Length == 2) && (path[1] == ':'))
            {
                OriginalPath = ".";
            }
            else
            {
                OriginalPath = path;
            }

            // Must fully qualify the path for the security check
            String fullPath = Path.GetFullPathInternal(path);

            demandDir = new String[] {Directory.GetDemandDir(fullPath, true)};
#if FEATURE_CORECLR
            if (checkHost)
            {
                FileSecurityState state = new FileSecurityState(FileSecurityStateAccess.Read, OriginalPath, fullPath);
                state.EnsureState();
            }
#else
            new FileIOPermission(FileIOPermissionAccess.Read, demandDir, false, false ).Demand();
#endif

            FullPath = fullPath;
            DisplayPath = GetDisplayName(OriginalPath, FullPath);
        }

#if FEATURE_CORESYSTEM
        [System.Security.SecuritySafeCritical]
#endif //FEATURE_CORESYSTEM
        internal DirectoryInfo(String fullPath, bool junk)
        {
            Contract.Assert(Path.GetRootLength(fullPath) > 0, "fullPath must be fully qualified!");
            // Fast path when we know a DirectoryInfo exists.
            OriginalPath = Path.GetFileName(fullPath);

            FullPath = fullPath;
            DisplayPath = GetDisplayName(OriginalPath, FullPath);
            demandDir = new String[] {Directory.GetDemandDir(fullPath, true)};
        }

        [System.Security.SecurityCritical]  // auto-generated
        private DirectoryInfo(SerializationInfo info, StreamingContext context) : base(info, context)
        {
#if !FEATURE_CORECLR
            demandDir = new String[] {Directory.GetDemandDir(FullPath, true)};
            new FileIOPermission(FileIOPermissionAccess.Read, demandDir, false, false ).Demand();
#endif
            DisplayPath = GetDisplayName(OriginalPath, FullPath);
        }

        public override String Name {
            get 
            {
#if FEATURE_CORECLR
                // DisplayPath is dir name for coreclr
                return DisplayPath;
#else
                // Return just dir name
                return GetDirName(FullPath);
#endif
            }
        }

        public DirectoryInfo Parent {
            [System.Security.SecuritySafeCritical]
            get {
                String parentName;
                // FullPath might be either "c:\bar" or "c:\bar\".  Handle 
                // those cases, as well as avoiding mangling "c:\".
                String s = FullPath;
                if (s.Length > 3 && s.EndsWith(Path.DirectorySeparatorChar))
                    s = FullPath.Substring(0, FullPath.Length - 1);                
                parentName = Path.GetDirectoryName(s);
                if (parentName==null)
                    return null;
                DirectoryInfo dir = new DirectoryInfo(parentName,false);
#if FEATURE_CORECLR
                FileSecurityState state = new FileSecurityState(FileSecurityStateAccess.PathDiscovery | FileSecurityStateAccess.Read, String.Empty, dir.demandDir[0]);
                state.EnsureState();
#else
                new FileIOPermission(FileIOPermissionAccess.PathDiscovery | FileIOPermissionAccess.Read, dir.demandDir, false, false).Demand();
#endif
                return dir;
            }
        }

      
#if FEATURE_CORECLR
        [System.Security.SecuritySafeCritical]
#endif
        public DirectoryInfo CreateSubdirectory(String path) {
            if (path == null)
                throw new ArgumentNullException("path");
            Contract.EndContractBlock();

            return CreateSubdirectory(path, null);
        }

#if FEATURE_MACL
        [System.Security.SecuritySafeCritical]  // auto-generated
        public DirectoryInfo CreateSubdirectory(String path, DirectorySecurity directorySecurity)
        {
            return CreateSubdirectoryHelper(path, directorySecurity);
        }
#else  // FEATURE_MACL
        #if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
        #endif
        public DirectoryInfo CreateSubdirectory(String path, Object directorySecurity)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            Contract.EndContractBlock();

            return CreateSubdirectoryHelper(path, directorySecurity);
        }
#endif // FEATURE_MACL

        [System.Security.SecurityCritical]  // auto-generated
        private DirectoryInfo CreateSubdirectoryHelper(String path, Object directorySecurity)
        {
            Contract.Requires(path != null);

            String newDirs = Path.InternalCombine(FullPath, path);
            String fullPath = Path.GetFullPathInternal(newDirs);

            if (0!=String.Compare(FullPath,0,fullPath,0, FullPath.Length,StringComparison.OrdinalIgnoreCase)) {
                String displayPath = __Error.GetDisplayablePath(DisplayPath, false);
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidSubPath", path, displayPath));
            }

            // Ensure we have permission to create this subdirectory.
            String demandDirForCreation = Directory.GetDemandDir(fullPath, true);
#if FEATURE_CORECLR
            FileSecurityState state = new FileSecurityState(FileSecurityStateAccess.Write, OriginalPath, demandDirForCreation);
            state.EnsureState();
#else
            new FileIOPermission(FileIOPermissionAccess.Write, new String[] { demandDirForCreation }, false, false).Demand();
#endif

            Directory.InternalCreateDirectory(fullPath, path, directorySecurity);

            // Check for read permission to directory we hand back by calling this constructor.
            return new DirectoryInfo(fullPath);
        }

        public void Create()
        {
            Directory.InternalCreateDirectory(FullPath, OriginalPath, null, true);
        }

#if FEATURE_MACL
        public void Create(DirectorySecurity directorySecurity)
        {
            Directory.InternalCreateDirectory(FullPath, OriginalPath, directorySecurity, true);
        }
#endif

        // Tests if the given path refers to an existing DirectoryInfo on disk.
        // 
        // Your application must have Read permission to the directory's
        // contents.
        //
        public override bool Exists {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get
            {
                try
                {
                    if (_dataInitialised == -1)
                        Refresh();
                    if (_dataInitialised != 0) // Refresh was unable to initialise the data
                        return false;
                   
                    return _data.fileAttributes != -1 && (_data.fileAttributes & Win32Native.FILE_ATTRIBUTE_DIRECTORY) != 0;
                }
                catch
                {
                    return false;
                }
            }
        }
      
#if FEATURE_MACL
        public DirectorySecurity GetAccessControl()
        {
            return Directory.GetAccessControl(FullPath, AccessControlSections.Access | AccessControlSections.Owner | AccessControlSections.Group);
        }

        public DirectorySecurity GetAccessControl(AccessControlSections includeSections)
        {
            return Directory.GetAccessControl(FullPath, includeSections);
        }

        public void SetAccessControl(DirectorySecurity directorySecurity)
        {
            Directory.SetAccessControl(FullPath, directorySecurity);
        }
#endif

        // Returns an array of Files in the current DirectoryInfo matching the 
        // given search criteria (ie, "*.txt").
        public FileInfo[] GetFiles(String searchPattern)
        {
            if (searchPattern == null)
                throw new ArgumentNullException("searchPattern");
            Contract.EndContractBlock();

            return InternalGetFiles(searchPattern, SearchOption.TopDirectoryOnly);
        }

        // Returns an array of Files in the current DirectoryInfo matching the 
        // given search criteria (ie, "*.txt").
        public FileInfo[] GetFiles(String searchPattern, SearchOption searchOption)
        {
            if (searchPattern == null)
                throw new ArgumentNullException("searchPattern");
            if ((searchOption != SearchOption.TopDirectoryOnly) && (searchOption != SearchOption.AllDirectories))
                throw new ArgumentOutOfRangeException("searchOption", Environment.GetResourceString("ArgumentOutOfRange_Enum"));
            Contract.EndContractBlock();

            return InternalGetFiles(searchPattern, searchOption);
        }

        // Returns an array of Files in the current DirectoryInfo matching the 
        // given search criteria (ie, "*.txt").
        private FileInfo[] InternalGetFiles(String searchPattern, SearchOption searchOption)
        {
            Contract.Requires(searchPattern != null);
            Contract.Requires(searchOption == SearchOption.AllDirectories || searchOption == SearchOption.TopDirectoryOnly);

            IEnumerable<FileInfo> enble = FileSystemEnumerableFactory.CreateFileInfoIterator(FullPath, OriginalPath, searchPattern, searchOption);
            List<FileInfo> fileList = new List<FileInfo>(enble);
            return fileList.ToArray();
        }

        // Returns an array of Files in the DirectoryInfo specified by path
        public FileInfo[] GetFiles()
        {
            return InternalGetFiles("*", SearchOption.TopDirectoryOnly);
        }

        // Returns an array of Directories in the current directory.
        public DirectoryInfo[] GetDirectories()
        {
            return InternalGetDirectories("*", SearchOption.TopDirectoryOnly);
        }

        // Returns an array of strongly typed FileSystemInfo entries in the path with the
        // given search criteria (ie, "*.txt").
        public FileSystemInfo[] GetFileSystemInfos(String searchPattern)
        {
            if (searchPattern == null)
                throw new ArgumentNullException("searchPattern");
            Contract.EndContractBlock();

            return InternalGetFileSystemInfos(searchPattern, SearchOption.TopDirectoryOnly);
        }

        // Returns an array of strongly typed FileSystemInfo entries in the path with the
        // given search criteria (ie, "*.txt").
        public FileSystemInfo[] GetFileSystemInfos(String searchPattern, SearchOption searchOption)
        {
            if (searchPattern == null)
                throw new ArgumentNullException("searchPattern");
            if ((searchOption != SearchOption.TopDirectoryOnly) && (searchOption != SearchOption.AllDirectories))
                throw new ArgumentOutOfRangeException("searchOption", Environment.GetResourceString("ArgumentOutOfRange_Enum"));
            Contract.EndContractBlock();

            return InternalGetFileSystemInfos(searchPattern, searchOption);
        }

        // Returns an array of strongly typed FileSystemInfo entries in the path with the
        // given search criteria (ie, "*.txt").
        private FileSystemInfo[] InternalGetFileSystemInfos(String searchPattern, SearchOption searchOption)
        {
            Contract.Requires(searchPattern != null);
            Contract.Requires(searchOption == SearchOption.AllDirectories || searchOption == SearchOption.TopDirectoryOnly);

            IEnumerable<FileSystemInfo> enble = FileSystemEnumerableFactory.CreateFileSystemInfoIterator(FullPath, OriginalPath, searchPattern, searchOption);
            List<FileSystemInfo> fileList = new List<FileSystemInfo>(enble);
            return fileList.ToArray();
        }

        // Returns an array of strongly typed FileSystemInfo entries which will contain a listing
        // of all the files and directories.
        public FileSystemInfo[] GetFileSystemInfos()
        {
            return InternalGetFileSystemInfos("*", SearchOption.TopDirectoryOnly);
        }

        // Returns an array of Directories in the current DirectoryInfo matching the 
        // given search criteria (ie, "System*" could match the System & System32
        // directories).
        public DirectoryInfo[] GetDirectories(String searchPattern)
        {
            if (searchPattern == null)
                throw new ArgumentNullException("searchPattern");
            Contract.EndContractBlock();

            return InternalGetDirectories(searchPattern, SearchOption.TopDirectoryOnly);
        }

        // Returns an array of Directories in the current DirectoryInfo matching the 
        // given search criteria (ie, "System*" could match the System & System32
        // directories).
        public DirectoryInfo[] GetDirectories(String searchPattern, SearchOption searchOption)
        {
            if (searchPattern == null)
                throw new ArgumentNullException("searchPattern");
            if ((searchOption != SearchOption.TopDirectoryOnly) && (searchOption != SearchOption.AllDirectories))
                throw new ArgumentOutOfRangeException("searchOption", Environment.GetResourceString("ArgumentOutOfRange_Enum"));
            Contract.EndContractBlock();

            return InternalGetDirectories(searchPattern, searchOption);
        }

        // Returns an array of Directories in the current DirectoryInfo matching the 
        // given search criteria (ie, "System*" could match the System & System32
        // directories).
        private DirectoryInfo[] InternalGetDirectories(String searchPattern, SearchOption searchOption)
        {
            Contract.Requires(searchPattern != null);
            Contract.Requires(searchOption == SearchOption.AllDirectories || searchOption == SearchOption.TopDirectoryOnly);

            IEnumerable<DirectoryInfo> enble = FileSystemEnumerableFactory.CreateDirectoryInfoIterator(FullPath, OriginalPath, searchPattern, searchOption);
            List<DirectoryInfo> fileList = new List<DirectoryInfo>(enble);
            return fileList.ToArray();
        }

        public IEnumerable<DirectoryInfo> EnumerateDirectories()
        {
            return InternalEnumerateDirectories("*", SearchOption.TopDirectoryOnly);
        }

        public IEnumerable<DirectoryInfo> EnumerateDirectories(String searchPattern)
        {
            if (searchPattern == null)
                throw new ArgumentNullException("searchPattern");
            Contract.EndContractBlock();

            return InternalEnumerateDirectories(searchPattern, SearchOption.TopDirectoryOnly);
        }

        public IEnumerable<DirectoryInfo> EnumerateDirectories(String searchPattern, SearchOption searchOption)
        {
            if (searchPattern == null)
                throw new ArgumentNullException("searchPattern");
            if ((searchOption != SearchOption.TopDirectoryOnly) && (searchOption != SearchOption.AllDirectories))
                throw new ArgumentOutOfRangeException("searchOption", Environment.GetResourceString("ArgumentOutOfRange_Enum"));
            Contract.EndContractBlock();

            return InternalEnumerateDirectories(searchPattern, searchOption);
        }

        private IEnumerable<DirectoryInfo> InternalEnumerateDirectories(String searchPattern, SearchOption searchOption)
        {
            Contract.Requires(searchPattern != null);
            Contract.Requires(searchOption == SearchOption.AllDirectories || searchOption == SearchOption.TopDirectoryOnly);

            return FileSystemEnumerableFactory.CreateDirectoryInfoIterator(FullPath, OriginalPath, searchPattern, searchOption);
        }

        public IEnumerable<FileInfo> EnumerateFiles()
        {
            return InternalEnumerateFiles("*", SearchOption.TopDirectoryOnly);
        }

        public IEnumerable<FileInfo> EnumerateFiles(String searchPattern)
        {
            if (searchPattern == null)
                throw new ArgumentNullException("searchPattern");
            Contract.EndContractBlock();

            return InternalEnumerateFiles(searchPattern, SearchOption.TopDirectoryOnly);
        }

        public IEnumerable<FileInfo> EnumerateFiles(String searchPattern, SearchOption searchOption)
        {
            if (searchPattern == null)
                throw new ArgumentNullException("searchPattern");
            if ((searchOption != SearchOption.TopDirectoryOnly) && (searchOption != SearchOption.AllDirectories))
                throw new ArgumentOutOfRangeException("searchOption", Environment.GetResourceString("ArgumentOutOfRange_Enum"));
            Contract.EndContractBlock();

            return InternalEnumerateFiles(searchPattern, searchOption);
        }

        private IEnumerable<FileInfo> InternalEnumerateFiles(String searchPattern, SearchOption searchOption)
        {
            Contract.Requires(searchPattern != null);
            Contract.Requires(searchOption == SearchOption.AllDirectories || searchOption == SearchOption.TopDirectoryOnly);

            return FileSystemEnumerableFactory.CreateFileInfoIterator(FullPath, OriginalPath, searchPattern, searchOption);
        }

        public IEnumerable<FileSystemInfo> EnumerateFileSystemInfos()
        {
            return InternalEnumerateFileSystemInfos("*", SearchOption.TopDirectoryOnly);
        }

        public IEnumerable<FileSystemInfo> EnumerateFileSystemInfos(String searchPattern)
        {
            if (searchPattern == null)
                throw new ArgumentNullException("searchPattern");
            Contract.EndContractBlock();

            return InternalEnumerateFileSystemInfos(searchPattern, SearchOption.TopDirectoryOnly);
        }

        public IEnumerable<FileSystemInfo> EnumerateFileSystemInfos(String searchPattern, SearchOption searchOption)
        {
            if (searchPattern == null)
                throw new ArgumentNullException("searchPattern");
            if ((searchOption != SearchOption.TopDirectoryOnly) && (searchOption != SearchOption.AllDirectories))
                throw new ArgumentOutOfRangeException("searchOption", Environment.GetResourceString("ArgumentOutOfRange_Enum"));
            Contract.EndContractBlock();

            return InternalEnumerateFileSystemInfos(searchPattern, searchOption);
        }

        private IEnumerable<FileSystemInfo> InternalEnumerateFileSystemInfos(String searchPattern, SearchOption searchOption)
        {
            Contract.Requires(searchPattern != null);
            Contract.Requires(searchOption == SearchOption.AllDirectories || searchOption == SearchOption.TopDirectoryOnly);

            return FileSystemEnumerableFactory.CreateFileSystemInfoIterator(FullPath, OriginalPath, searchPattern, searchOption);
        }
        
        // Returns the root portion of the given path. The resulting string
        // consists of those rightmost characters of the path that constitute the
        // root of the path. Possible patterns for the resulting string are: An
        // empty string (a relative path on the current drive), "\" (an absolute
        // path on the current drive), "X:" (a relative path on a given drive,
        // where X is the drive letter), "X:\" (an absolute path on a given drive),
        // and "\\server\share" (a UNC path for a given server and share name).
        // The resulting string is null if path is null.
        //

        public DirectoryInfo Root {
            [System.Security.SecuritySafeCritical]
            get
            {
                String demandPath;
                int rootLength = Path.GetRootLength(FullPath);
                String rootPath = FullPath.Substring(0, rootLength);
                demandPath = Directory.GetDemandDir(rootPath, true);

#if FEATURE_CORECLR
                FileSecurityState sourceState = new FileSecurityState(FileSecurityStateAccess.PathDiscovery, String.Empty, demandPath);
                sourceState.EnsureState();
#else
                new FileIOPermission(FileIOPermissionAccess.PathDiscovery, new String[] { demandPath }, false, false).Demand();
#endif
                return new DirectoryInfo(rootPath);
            }
        }

        [System.Security.SecuritySafeCritical]
        public void MoveTo(String destDirName) {
            if (destDirName==null)
                throw new ArgumentNullException("destDirName");
            if (destDirName.Length==0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyFileName"), "destDirName");
            Contract.EndContractBlock();
            
#if FEATURE_CORECLR
            FileSecurityState sourceState = new FileSecurityState(FileSecurityStateAccess.Write | FileSecurityStateAccess.Read, DisplayPath, Directory.GetDemandDir(FullPath, true));
            sourceState.EnsureState();
#else
            new FileIOPermission(FileIOPermissionAccess.Write | FileIOPermissionAccess.Read, demandDir, false, false).Demand();
#endif
            String fullDestDirName = Path.GetFullPathInternal(destDirName);
            String demandPath;
            if (!fullDestDirName.EndsWith(Path.DirectorySeparatorChar))
                fullDestDirName = fullDestDirName + Path.DirectorySeparatorChar;

            demandPath = fullDestDirName + '.';

            // Demand read & write permission to destination.  The reason is
            // we hand back a DirectoryInfo to the destination that would allow
            // you to read a directory listing from that directory.  Sure, you 
            // had the ability to read the file contents in the old location,
            // but you technically also need read permissions to the new 
            // location as well, and write is not a true superset of read.
#if FEATURE_CORECLR
            FileSecurityState destState = new FileSecurityState(FileSecurityStateAccess.Write, destDirName, demandPath);
            destState.EnsureState();
#else
            new FileIOPermission(FileIOPermissionAccess.Write | FileIOPermissionAccess.Read, demandPath).Demand();
#endif
            
            String fullSourcePath;
            if (FullPath.EndsWith(Path.DirectorySeparatorChar))
                fullSourcePath = FullPath;
            else
                fullSourcePath = FullPath + Path.DirectorySeparatorChar;

            if (String.Compare(fullSourcePath, fullDestDirName, StringComparison.OrdinalIgnoreCase) == 0)
                throw new IOException(Environment.GetResourceString("IO.IO_SourceDestMustBeDifferent"));

            String sourceRoot = Path.GetPathRoot(fullSourcePath);
            String destinationRoot = Path.GetPathRoot(fullDestDirName);

            if (String.Compare(sourceRoot, destinationRoot, StringComparison.OrdinalIgnoreCase) != 0)
                throw new IOException(Environment.GetResourceString("IO.IO_SourceDestMustHaveSameRoot"));
                       
            if (!Win32Native.MoveFile(FullPath, destDirName))
            {
                int hr = Marshal.GetLastWin32Error();
                if (hr == Win32Native.ERROR_FILE_NOT_FOUND) // A dubious error code
                {
                    hr = Win32Native.ERROR_PATH_NOT_FOUND;
                    __Error.WinIOError(hr, DisplayPath);
                }
                
                if (hr == Win32Native.ERROR_ACCESS_DENIED) // We did this for Win9x. We can't change it for backcomp. 
                    throw new IOException(Environment.GetResourceString("UnauthorizedAccess_IODenied_Path", DisplayPath));
            
                __Error.WinIOError(hr,String.Empty);
            }
            FullPath = fullDestDirName;
            OriginalPath = destDirName;
            DisplayPath = GetDisplayName(OriginalPath, FullPath);
            demandDir = new String[] { Directory.GetDemandDir(FullPath, true) };

            // Flush any cached information about the directory.
            _dataInitialised = -1;
        }

        [System.Security.SecuritySafeCritical]
        public override void Delete()
        {
            Directory.Delete(FullPath, OriginalPath, false, true);
        }

        [System.Security.SecuritySafeCritical]
        public void Delete(bool recursive)
        {
            Directory.Delete(FullPath, OriginalPath, recursive, true);
        }

        // Returns the fully qualified path
        public override String ToString()
        {
            return DisplayPath;
        }

        private static String GetDisplayName(String originalPath, String fullPath)
        {
            Contract.Assert(originalPath != null);
            Contract.Assert(fullPath != null);

            String displayName = "";

            // Special case "<DriveLetter>:" to point to "<CurrentDirectory>" instead
            if ((originalPath.Length == 2) && (originalPath[1] == ':'))
            {
                displayName = ".";
            }
            else 
            {
#if FEATURE_CORECLR
                displayName = GetDirName(fullPath);
#else
                displayName = originalPath;
#endif
            }
            return displayName;
        }

        private static String GetDirName(String fullPath)
        {
            Contract.Assert(fullPath != null);

            String dirName = null;
            if (fullPath.Length > 3)
            {
                String s = fullPath;
                if (fullPath.EndsWith(Path.DirectorySeparatorChar))
                {
                    s = fullPath.Substring(0, fullPath.Length - 1);
                }
                dirName = Path.GetFileName(s);
            }
            else
            {
                dirName = fullPath;  // For rooted paths, like "c:\"
            }
            return dirName;
        }

    }       
}

