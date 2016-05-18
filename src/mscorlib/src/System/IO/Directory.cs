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
using System.Security.Permissions;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;
using System.Text;
using System.Runtime.InteropServices;
using System.Globalization;
using System.Runtime.Versioning;
using System.Diagnostics.Contracts;
using System.Threading;

#if FEATURE_MACL
using System.Security.AccessControl;
#endif

namespace System.IO {
    [ComVisible(true)]
    public static class Directory {
        public static DirectoryInfo GetParent(String path)
        {
            if (path==null)
                throw new ArgumentNullException("path");

            if (path.Length==0)
                throw new ArgumentException(Environment.GetResourceString("Argument_PathEmpty"), "path");
            Contract.EndContractBlock();

            String fullPath = Path.GetFullPathInternal(path);
                    
            String s = Path.GetDirectoryName(fullPath);
            if (s==null)
                 return null;
            return new DirectoryInfo(s);
        }

        [System.Security.SecuritySafeCritical]
        public static DirectoryInfo CreateDirectory(String path) {
            if (path == null)
                throw new ArgumentNullException("path");
            if (path.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_PathEmpty"));
            Contract.EndContractBlock();

            return InternalCreateDirectoryHelper(path, true);
        }

        [System.Security.SecurityCritical]
        internal static DirectoryInfo UnsafeCreateDirectory(String path)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            if (path.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_PathEmpty"));
            Contract.EndContractBlock();

            return InternalCreateDirectoryHelper(path, false);
        }

        [System.Security.SecurityCritical] 
        internal static DirectoryInfo InternalCreateDirectoryHelper(String path, bool checkHost)
        {
            Contract.Requires(path != null);
            Contract.Requires(path.Length != 0);

            String fullPath = Path.GetFullPathInternal(path);

            // You need read access to the directory to be returned back and write access to all the directories 
            // that you need to create. If we fail any security checks we will not create any directories at all.
            // We attempt to create directories only after all the security checks have passed. This is avoid doing
            // a demand at every level.
            String demandDir = GetDemandDir(fullPath, true);

#if FEATURE_CORECLR
            if (checkHost)
            {
                FileSecurityState state = new FileSecurityState(FileSecurityStateAccess.Read, path, demandDir);
                state.EnsureState(); // do the check on the AppDomainManager to make sure this is allowed  
            }
#else
            FileIOPermission.QuickDemand(FileIOPermissionAccess.Read, demandDir, false, false);
#endif

            InternalCreateDirectory(fullPath, path, null, checkHost);

            return new DirectoryInfo(fullPath, false);
        }

#if FEATURE_MACL
        [System.Security.SecuritySafeCritical]  // auto-generated
        public static DirectoryInfo CreateDirectory(String path, DirectorySecurity directorySecurity) {
            if (path==null)
                throw new ArgumentNullException("path");
            if (path.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_PathEmpty"));
            Contract.EndContractBlock();
            
            String fullPath = Path.GetFullPathInternal(path);

            // You need read access to the directory to be returned back and write access to all the directories 
            // that you need to create. If we fail any security checks we will not create any directories at all.
            // We attempt to create directories only after all the security checks have passed. This is avoid doing
            // a demand at every level.
            String demandDir = GetDemandDir(fullPath, true); 
            FileIOPermission.QuickDemand(FileIOPermissionAccess.Read, demandDir, false, false );

            InternalCreateDirectory(fullPath, path, directorySecurity);
            
            return new DirectoryInfo(fullPath, false);
        }
#endif // FEATURE_MACL

        // Input to this method should already be fullpath. This method will ensure that we append 
        // the trailing slash only when appropriate and when thisDirOnly is specified append a "." 
        // at the end of the path to indicate that the demand is only for the fullpath and not 
        // everything underneath it.
        internal static String GetDemandDir(string fullPath, bool thisDirOnly)
        {
            String demandPath;

            if (thisDirOnly) {
                if (fullPath.EndsWith( Path.DirectorySeparatorChar ) 
                    || fullPath.EndsWith( Path.AltDirectorySeparatorChar ) )
                    demandPath = fullPath + ".";
                else
                    demandPath = fullPath + Path.DirectorySeparatorCharAsString + ".";
            }
            else {
                if (!(fullPath.EndsWith( Path.DirectorySeparatorChar ) 
                    || fullPath.EndsWith( Path.AltDirectorySeparatorChar )) )
                    demandPath = fullPath + Path.DirectorySeparatorCharAsString;
                else
                    demandPath = fullPath;
            }
            return demandPath;
        }

        internal static void InternalCreateDirectory(String fullPath, String path, Object dirSecurityObj)
        {
            InternalCreateDirectory(fullPath, path, dirSecurityObj, false);
        }


        [System.Security.SecuritySafeCritical]
        internal unsafe static void InternalCreateDirectory(String fullPath, String path, Object dirSecurityObj, bool checkHost)
        {
#if FEATURE_MACL
            DirectorySecurity dirSecurity = (DirectorySecurity)dirSecurityObj;
#endif // FEATURE_MACL

            int length = fullPath.Length;

            // We need to trim the trailing slash or the code will try to create 2 directories of the same name.
            if (length >= 2 && Path.IsDirectorySeparator(fullPath[length - 1]))
                length--;
            
            int lengthRoot = Path.GetRootLength(fullPath);

            // For UNC paths that are only // or /// 
            if (length == 2 && Path.IsDirectorySeparator(fullPath[1]))
                throw new IOException(Environment.GetResourceString("IO.IO_CannotCreateDirectory", path));

            // We can save a bunch of work if the directory we want to create already exists.  This also
            // saves us in the case where sub paths are inaccessible (due to ERROR_ACCESS_DENIED) but the
            // final path is accessable and the directory already exists.  For example, consider trying
            // to create c:\Foo\Bar\Baz, where everything already exists but ACLS prevent access to c:\Foo
            // and c:\Foo\Bar.  In that case, this code will think it needs to create c:\Foo, and c:\Foo\Bar
            // and fail to due so, causing an exception to be thrown.  This is not what we want.
            if (InternalExists(fullPath)) {
                return;
            }

            List<string> stackDir = new List<string>();

            // Attempt to figure out which directories don't exist, and only
            // create the ones we need.  Note that InternalExists may fail due
            // to Win32 ACL's preventing us from seeing a directory, and this
            // isn't threadsafe.

            bool somepathexists = false;

            if (length > lengthRoot) { // Special case root (fullpath = X:\\)
                int i = length-1;
                while (i >= lengthRoot && !somepathexists) {
                    String dir = fullPath.Substring(0, i+1);
                        
                    if (!InternalExists(dir)) // Create only the ones missing
                        stackDir.Add(dir);
                    else
                        somepathexists = true;
                    
                    while (i > lengthRoot && fullPath[i] != Path.DirectorySeparatorChar && fullPath[i] != Path.AltDirectorySeparatorChar) i--;
                    i--;
                }
            }

            int count = stackDir.Count;

            if (stackDir.Count != 0
#if FEATURE_CAS_POLICY
                // All demands in full trust domains are no-ops, so skip
                //
                // The full path went through validity checks by being passed through FileIOPermissions already.
                // As a sub string of the full path can't fail the checks if the full path passes.
                && !CodeAccessSecurityEngine.QuickCheckForAllDemands()
#endif
            )
            {
                String[] securityList = new String[stackDir.Count];
                stackDir.CopyTo(securityList, 0);
                for (int j = 0 ; j < securityList.Length; j++)
                    securityList[j] += "\\."; // leaf will never have a slash at the end

                // Security check for all directories not present only.
#if FEATURE_MACL
                AccessControlActions control = (dirSecurity == null) ? AccessControlActions.None : AccessControlActions.Change;
                FileIOPermission.QuickDemand(FileIOPermissionAccess.Write, control, securityList, false, false);
#else
#if FEATURE_CORECLR
                if (checkHost)
                {
                    foreach (String demandPath in securityList) 
                    {
                        FileSecurityState state = new FileSecurityState(FileSecurityStateAccess.Write, String.Empty, demandPath);
                        state.EnsureState();
                    }
                }
#else
                FileIOPermission.QuickDemand(FileIOPermissionAccess.Write, securityList, false, false);
#endif
#endif //FEATURE_MACL
            }

            // If we were passed a DirectorySecurity, convert it to a security
            // descriptor and set it in he call to CreateDirectory.
            Win32Native.SECURITY_ATTRIBUTES secAttrs = null;
#if FEATURE_MACL
            if (dirSecurity != null) {
                secAttrs = new Win32Native.SECURITY_ATTRIBUTES();
                secAttrs.nLength = (int)Marshal.SizeOf(secAttrs);

                // For ACL's, get the security descriptor from the FileSecurity.
                byte[] sd = dirSecurity.GetSecurityDescriptorBinaryForm();
                byte * bytesOnStack = stackalloc byte[sd.Length];
                Buffer.Memcpy(bytesOnStack, 0, sd, 0, sd.Length);
                secAttrs.pSecurityDescriptor = bytesOnStack;
            }
#endif

            bool r = true;
            int firstError = 0;
            String errorString = path;
            // If all the security checks succeeded create all the directories
            while (stackDir.Count > 0) {
                String name = stackDir[stackDir.Count - 1];
                stackDir.RemoveAt(stackDir.Count - 1);
                if (PathInternal.IsDirectoryTooLong(name))
                    throw new PathTooLongException(Environment.GetResourceString("IO.PathTooLong"));
                r = Win32Native.CreateDirectory(name, secAttrs);
                if (!r && (firstError == 0)) {
                    int currentError = Marshal.GetLastWin32Error();
                    // While we tried to avoid creating directories that don't
                    // exist above, there are at least two cases that will 
                    // cause us to see ERROR_ALREADY_EXISTS here.  InternalExists 
                    // can fail because we didn't have permission to the 
                    // directory.  Secondly, another thread or process could
                    // create the directory between the time we check and the
                    // time we try using the directory.  Thirdly, it could
                    // fail because the target does exist, but is a file.
                    if (currentError != Win32Native.ERROR_ALREADY_EXISTS)
                        firstError = currentError;
                    else {
                        // If there's a file in this directory's place, or if we have ERROR_ACCESS_DENIED when checking if the directory already exists throw.
                        if (File.InternalExists(name) || (!InternalExists(name, out currentError) && currentError == Win32Native.ERROR_ACCESS_DENIED)) {
                            firstError = currentError;
                            // Give the user a nice error message, but don't leak path information.
                            try {
#if FEATURE_CORECLR
                                if (checkHost)
                                {
                                    FileSecurityState state = new FileSecurityState(FileSecurityStateAccess.PathDiscovery, String.Empty, GetDemandDir(name, true));
                                    state.EnsureState();
                                }
#else
                                FileIOPermission.QuickDemand(FileIOPermissionAccess.PathDiscovery, GetDemandDir(name, true));
#endif // FEATURE_CORECLR
                                errorString = name;
                            }
                            catch(SecurityException) {}
                        }
                    }
                }
            }

            // We need this check to mask OS differences
            // Handle CreateDirectory("X:\\foo") when X: doesn't exist. Similarly for n/w paths.
            if ((count == 0) && !somepathexists) {
                String root = InternalGetDirectoryRoot(fullPath);
                if (!InternalExists(root)) {
                    // Extract the root from the passed in path again for security.
                    __Error.WinIOError(Win32Native.ERROR_PATH_NOT_FOUND, InternalGetDirectoryRoot(path));
                }
                return;
            }

            // Only throw an exception if creating the exact directory we 
            // wanted failed to work correctly.
            if (!r && (firstError != 0)) {
                __Error.WinIOError(firstError, errorString);
            }
        }
      
       
        // Tests if the given path refers to an existing DirectoryInfo on disk.
        // 
        // Your application must have Read permission to the directory's
        // contents.
        //
        [System.Security.SecuritySafeCritical]  // auto-generated
        public static bool Exists(String path)
        {
            return InternalExistsHelper(path, true);
        }

        [System.Security.SecurityCritical]
        internal static bool UnsafeExists(String path)
        {
            return InternalExistsHelper(path, false);
        }

        [System.Security.SecurityCritical]
        internal static bool InternalExistsHelper(String path, bool checkHost) {
            try
            {
                if (path == null)
                    return false;
                if (path.Length == 0)
                    return false;

                // Get fully qualified file name ending in \* for security check

                String fullPath = Path.GetFullPathInternal(path);
                String demandPath = GetDemandDir(fullPath, true);

#if FEATURE_CORECLR
                if (checkHost)
                {
                    FileSecurityState state = new FileSecurityState(FileSecurityStateAccess.Read, path, demandPath);
                    state.EnsureState();
                }
#else
                FileIOPermission.QuickDemand(FileIOPermissionAccess.Read, demandPath, false, false);
#endif


                return InternalExists(fullPath);
            }
            catch (ArgumentException) { }
            catch (NotSupportedException) { }  // Security can throw this on ":"
            catch (SecurityException) { }
            catch (IOException) { }
            catch (UnauthorizedAccessException)
            {
                Contract.Assert(false, "Ignore this assert and send a repro to Microsoft. This assert was tracking purposes only.");
            }
            return false;
        }

        // Determine whether path describes an existing directory
        // on disk, avoiding security checks.
        [System.Security.SecurityCritical]  // auto-generated
        internal static bool InternalExists(String path) {
            int lastError = Win32Native.ERROR_SUCCESS;
            return InternalExists(path, out lastError);
        }

        // Determine whether path describes an existing directory
        // on disk, avoiding security checks.
        [System.Security.SecurityCritical]  // auto-generated
        internal static bool InternalExists(String path, out int lastError) {
            Win32Native.WIN32_FILE_ATTRIBUTE_DATA data = new Win32Native.WIN32_FILE_ATTRIBUTE_DATA();
            lastError = File.FillAttributeInfo(path, ref data, false, true);

            return (lastError == 0) && (data.fileAttributes != -1)
                    && ((data.fileAttributes & Win32Native.FILE_ATTRIBUTE_DIRECTORY) != 0);
        }

        public static void SetCreationTime(String path,DateTime creationTime)
        {
            SetCreationTimeUtc(path, creationTime.ToUniversalTime());
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public unsafe static void SetCreationTimeUtc(String path,DateTime creationTimeUtc)
        {
            using (SafeFileHandle handle = Directory.OpenHandle(path)) {
                Win32Native.FILE_TIME fileTime = new Win32Native.FILE_TIME(creationTimeUtc.ToFileTimeUtc());
                bool r = Win32Native.SetFileTime(handle, &fileTime, null, null);
                if (!r)
                {
                    int errorCode = Marshal.GetLastWin32Error();
                    __Error.WinIOError(errorCode, path);
                }
            }
        }

        public static DateTime GetCreationTime(String path)
        {
            return File.GetCreationTime(path);
        }

        public static DateTime GetCreationTimeUtc(String path)
        {
            return File.GetCreationTimeUtc(path);
        }

        public static void SetLastWriteTime(String path,DateTime lastWriteTime)
        {
            SetLastWriteTimeUtc(path, lastWriteTime.ToUniversalTime());
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public unsafe static void SetLastWriteTimeUtc(String path,DateTime lastWriteTimeUtc)
        {
            using (SafeFileHandle handle = Directory.OpenHandle(path)) {
                Win32Native.FILE_TIME fileTime = new Win32Native.FILE_TIME(lastWriteTimeUtc.ToFileTimeUtc());
                bool r = Win32Native.SetFileTime(handle,  null, null, &fileTime);
                if (!r)
                {
                    int errorCode = Marshal.GetLastWin32Error();
                    __Error.WinIOError(errorCode, path);
                }
            }
        }

        public static DateTime GetLastWriteTime(String path)
        {
            return File.GetLastWriteTime(path);
        }

        public static DateTime GetLastWriteTimeUtc(String path)
        {
            return File.GetLastWriteTimeUtc(path);
        }

        public static void SetLastAccessTime(String path,DateTime lastAccessTime)
        {
            SetLastAccessTimeUtc(path, lastAccessTime.ToUniversalTime());
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public unsafe static void SetLastAccessTimeUtc(String path,DateTime lastAccessTimeUtc)
        {
            using (SafeFileHandle handle = Directory.OpenHandle(path)) {
                Win32Native.FILE_TIME fileTime = new Win32Native.FILE_TIME(lastAccessTimeUtc.ToFileTimeUtc());
                bool r = Win32Native.SetFileTime(handle,  null, &fileTime, null);
                if (!r)
                {
                    int errorCode = Marshal.GetLastWin32Error();
                    __Error.WinIOError(errorCode, path);
                }
            }
        }

        public static DateTime GetLastAccessTime(String path)
        {
            return File.GetLastAccessTime(path);
        }

        public static DateTime GetLastAccessTimeUtc(String path)
        {
            return File.GetLastAccessTimeUtc(path);
        }       
 
#if FEATURE_MACL
        public static DirectorySecurity GetAccessControl(String path)
        {
            return new DirectorySecurity(path, AccessControlSections.Access | AccessControlSections.Owner | AccessControlSections.Group);
        }

        public static DirectorySecurity GetAccessControl(String path, AccessControlSections includeSections)
        {
            return new DirectorySecurity(path, includeSections);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public static void SetAccessControl(String path, DirectorySecurity directorySecurity)
        {
            if (directorySecurity == null)
                throw new ArgumentNullException("directorySecurity");
            Contract.EndContractBlock();

            String fullPath = Path.GetFullPathInternal(path);
            directorySecurity.Persist(fullPath);
        }
#endif

        // Returns an array of filenames in the DirectoryInfo specified by path
        public static String[] GetFiles(String path)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            Contract.Ensures(Contract.Result<String[]>() != null);
            Contract.EndContractBlock();

            return InternalGetFiles(path, "*", SearchOption.TopDirectoryOnly);
        }

        // Returns an array of Files in the current DirectoryInfo matching the 
        // given search pattern (ie, "*.txt").
        public static String[] GetFiles(String path, String searchPattern)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            if (searchPattern == null)
                throw new ArgumentNullException("searchPattern");
            Contract.Ensures(Contract.Result<String[]>() != null);
            Contract.EndContractBlock();

            return InternalGetFiles(path, searchPattern, SearchOption.TopDirectoryOnly);
        }

        // Returns an array of Files in the current DirectoryInfo matching the 
        // given search pattern (ie, "*.txt") and search option
        public static String[] GetFiles(String path, String searchPattern, SearchOption searchOption)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            if (searchPattern == null)
                throw new ArgumentNullException("searchPattern");
            if ((searchOption != SearchOption.TopDirectoryOnly) && (searchOption != SearchOption.AllDirectories))
                throw new ArgumentOutOfRangeException("searchOption", Environment.GetResourceString("ArgumentOutOfRange_Enum"));
            Contract.Ensures(Contract.Result<String[]>() != null);
            Contract.EndContractBlock();
            
            return InternalGetFiles(path, searchPattern, searchOption);
        }

        // Returns an array of Files in the current DirectoryInfo matching the 
        // given search pattern (ie, "*.txt") and search option
        private static String[] InternalGetFiles(String path, String searchPattern, SearchOption searchOption)
        {
            Contract.Requires(path != null);
            Contract.Requires(searchPattern != null);
            Contract.Requires(searchOption == SearchOption.AllDirectories || searchOption == SearchOption.TopDirectoryOnly);

            return InternalGetFileDirectoryNames(path, path, searchPattern, true, false, searchOption, true);
        }

        [System.Security.SecurityCritical]
        internal static String[] UnsafeGetFiles(String path, String searchPattern, SearchOption searchOption)
        {
            Contract.Requires(path != null);
            Contract.Requires(searchPattern != null);
            Contract.Requires(searchOption == SearchOption.AllDirectories || searchOption == SearchOption.TopDirectoryOnly);

            return InternalGetFileDirectoryNames(path, path, searchPattern, true, false, searchOption, false);
        }

        // Returns an array of Directories in the current directory.
        public static String[] GetDirectories(String path)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            Contract.Ensures(Contract.Result<String[]>() != null);
            Contract.EndContractBlock();

            return InternalGetDirectories(path, "*", SearchOption.TopDirectoryOnly);
        }

        // Returns an array of Directories in the current DirectoryInfo matching the 
        // given search criteria (ie, "*.txt").
        public static String[] GetDirectories(String path, String searchPattern)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            if (searchPattern == null)
                throw new ArgumentNullException("searchPattern");
            Contract.Ensures(Contract.Result<String[]>() != null);
            Contract.EndContractBlock();

            return InternalGetDirectories(path, searchPattern, SearchOption.TopDirectoryOnly);
        }

        // Returns an array of Directories in the current DirectoryInfo matching the 
        // given search criteria (ie, "*.txt").
        public static String[] GetDirectories(String path, String searchPattern, SearchOption searchOption)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            if (searchPattern == null)
                throw new ArgumentNullException("searchPattern");
            if ((searchOption != SearchOption.TopDirectoryOnly) && (searchOption != SearchOption.AllDirectories))
                throw new ArgumentOutOfRangeException("searchOption", Environment.GetResourceString("ArgumentOutOfRange_Enum"));
            Contract.Ensures(Contract.Result<String[]>() != null);
            Contract.EndContractBlock();

            return InternalGetDirectories(path, searchPattern, searchOption);
        }

        // Returns an array of Directories in the current DirectoryInfo matching the 
        // given search criteria (ie, "*.txt").
        private static String[] InternalGetDirectories(String path, String searchPattern, SearchOption searchOption)
        {
            Contract.Requires(path != null);
            Contract.Requires(searchPattern != null);
            Contract.Requires(searchOption == SearchOption.AllDirectories || searchOption == SearchOption.TopDirectoryOnly);
            Contract.Ensures(Contract.Result<String[]>() != null);

            return InternalGetFileDirectoryNames(path, path, searchPattern, false, true, searchOption, true);
        }

        [System.Security.SecurityCritical]
        internal static String[] UnsafeGetDirectories(String path, String searchPattern, SearchOption searchOption)
        {
            Contract.Requires(path != null);
            Contract.Requires(searchPattern != null);
            Contract.Requires(searchOption == SearchOption.AllDirectories || searchOption == SearchOption.TopDirectoryOnly);
            Contract.Ensures(Contract.Result<String[]>() != null);

            return InternalGetFileDirectoryNames(path, path, searchPattern, false, true, searchOption, false);
        }
            
        // Returns an array of strongly typed FileSystemInfo entries in the path
        public static String[] GetFileSystemEntries(String path)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            Contract.Ensures(Contract.Result<String[]>() != null);
            Contract.EndContractBlock();

            return InternalGetFileSystemEntries(path, "*", SearchOption.TopDirectoryOnly);
        }

        // Returns an array of strongly typed FileSystemInfo entries in the path with the
        // given search criteria (ie, "*.txt"). We disallow .. as a part of the search criteria
        public static String[] GetFileSystemEntries(String path, String searchPattern)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            if (searchPattern == null)
                throw new ArgumentNullException("searchPattern");
            Contract.Ensures(Contract.Result<String[]>() != null);
            Contract.EndContractBlock();

            return InternalGetFileSystemEntries(path, searchPattern, SearchOption.TopDirectoryOnly);
        }

        // Returns an array of strongly typed FileSystemInfo entries in the path with the
        // given search criteria (ie, "*.txt"). We disallow .. as a part of the search criteria
        public static String[] GetFileSystemEntries(String path, String searchPattern, SearchOption searchOption)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            if (searchPattern == null)
                throw new ArgumentNullException("searchPattern");
            if ((searchOption != SearchOption.TopDirectoryOnly) && (searchOption != SearchOption.AllDirectories))
                throw new ArgumentOutOfRangeException("searchOption", Environment.GetResourceString("ArgumentOutOfRange_Enum"));
            Contract.Ensures(Contract.Result<String[]>() != null);
            Contract.EndContractBlock();

            return InternalGetFileSystemEntries(path, searchPattern, searchOption);
        }

        private static String[] InternalGetFileSystemEntries(String path, String searchPattern, SearchOption searchOption)
        {
            Contract.Requires(path != null);
            Contract.Requires(searchPattern != null);
            Contract.Requires(searchOption == SearchOption.AllDirectories || searchOption == SearchOption.TopDirectoryOnly);

            return InternalGetFileDirectoryNames(path, path, searchPattern, true, true, searchOption, true);
        }


        // Private class that holds search data that is passed around 
        // in the heap based stack recursion
        internal sealed class SearchData
        {
            public SearchData(String fullPath, String userPath, SearchOption searchOption)
            {
                Contract.Requires(fullPath != null && fullPath.Length > 0);
                Contract.Requires(userPath != null && userPath.Length > 0);
                Contract.Requires(searchOption == SearchOption.AllDirectories || searchOption == SearchOption.TopDirectoryOnly);

                this.fullPath = fullPath;
                this.userPath = userPath;
                this.searchOption = searchOption;
            }

            public readonly string fullPath;     // Fully qualified search path excluding the search criteria in the end (ex, c:\temp\bar\foo)
            public readonly string userPath;     // User specified path (ex, bar\foo)
            public readonly SearchOption searchOption;
        }


        // Returns fully qualified user path of dirs/files that matches the search parameters. 
        // For recursive search this method will search through all the sub dirs  and execute 
        // the given search criteria against every dir.
        // For all the dirs/files returned, it will then demand path discovery permission for 
        // their parent folders (it will avoid duplicate permission checks)
        internal static String[] InternalGetFileDirectoryNames(String path, String userPathOriginal, String searchPattern, bool includeFiles, bool includeDirs, SearchOption searchOption, bool checkHost)
        {
            Contract.Requires(path != null);
            Contract.Requires(userPathOriginal != null);
            Contract.Requires(searchPattern != null);
            Contract.Requires(searchOption == SearchOption.AllDirectories || searchOption == SearchOption.TopDirectoryOnly);

            IEnumerable<String> enble = FileSystemEnumerableFactory.CreateFileNameIterator(
                                                                                path, userPathOriginal, searchPattern,
                                                                                includeFiles, includeDirs, searchOption, checkHost);
            List<String> fileList = new List<String>(enble);
            return fileList.ToArray();
        }

        public static IEnumerable<String> EnumerateDirectories(String path)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            Contract.EndContractBlock();

            return InternalEnumerateDirectories(path, "*", SearchOption.TopDirectoryOnly);
        }

        public static IEnumerable<String> EnumerateDirectories(String path, String searchPattern)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            if (searchPattern == null)
                throw new ArgumentNullException("searchPattern");
            Contract.EndContractBlock();

            return InternalEnumerateDirectories(path, searchPattern, SearchOption.TopDirectoryOnly);
        }

        public static IEnumerable<String> EnumerateDirectories(String path, String searchPattern, SearchOption searchOption)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            if (searchPattern == null)
                throw new ArgumentNullException("searchPattern");
            if ((searchOption != SearchOption.TopDirectoryOnly) && (searchOption != SearchOption.AllDirectories))
                throw new ArgumentOutOfRangeException("searchOption", Environment.GetResourceString("ArgumentOutOfRange_Enum"));
            Contract.EndContractBlock();

            return InternalEnumerateDirectories(path, searchPattern, searchOption);
        }

        private static IEnumerable<String> InternalEnumerateDirectories(String path, String searchPattern, SearchOption searchOption)
        {
            Contract.Requires(path != null);
            Contract.Requires(searchPattern != null);
            Contract.Requires(searchOption == SearchOption.AllDirectories || searchOption == SearchOption.TopDirectoryOnly);

            return EnumerateFileSystemNames(path, searchPattern, searchOption, false, true);
        }

        public static IEnumerable<String> EnumerateFiles(String path)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            Contract.Ensures(Contract.Result<IEnumerable<String>>() != null);
            Contract.EndContractBlock();

            return InternalEnumerateFiles(path, "*", SearchOption.TopDirectoryOnly);
        }

        public static IEnumerable<String> EnumerateFiles(String path, String searchPattern)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            if (searchPattern == null)
                throw new ArgumentNullException("searchPattern");
            Contract.Ensures(Contract.Result<IEnumerable<String>>() != null);
            Contract.EndContractBlock();

            return InternalEnumerateFiles(path, searchPattern, SearchOption.TopDirectoryOnly);
        }

        public static IEnumerable<String> EnumerateFiles(String path, String searchPattern, SearchOption searchOption)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            if (searchPattern == null)
                throw new ArgumentNullException("searchPattern");
            if ((searchOption != SearchOption.TopDirectoryOnly) && (searchOption != SearchOption.AllDirectories))
                throw new ArgumentOutOfRangeException("searchOption", Environment.GetResourceString("ArgumentOutOfRange_Enum"));
            Contract.Ensures(Contract.Result<IEnumerable<String>>() != null);
            Contract.EndContractBlock();

            return InternalEnumerateFiles(path, searchPattern, searchOption);
        }

        private static IEnumerable<String> InternalEnumerateFiles(String path, String searchPattern, SearchOption searchOption)
        {
            Contract.Requires(path != null);
            Contract.Requires(searchPattern != null);
            Contract.Requires(searchOption == SearchOption.AllDirectories || searchOption == SearchOption.TopDirectoryOnly);
            Contract.Ensures(Contract.Result<IEnumerable<String>>() != null);

            return EnumerateFileSystemNames(path, searchPattern, searchOption, true, false);
        }

        public static IEnumerable<String> EnumerateFileSystemEntries(String path)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            Contract.Ensures(Contract.Result<IEnumerable<String>>() != null);
            Contract.EndContractBlock();

            return InternalEnumerateFileSystemEntries(path, "*", SearchOption.TopDirectoryOnly);
        }

        public static IEnumerable<String> EnumerateFileSystemEntries(String path, String searchPattern)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            if (searchPattern == null)
                throw new ArgumentNullException("searchPattern");
            Contract.Ensures(Contract.Result<IEnumerable<String>>() != null);
            Contract.EndContractBlock();

            return InternalEnumerateFileSystemEntries(path, searchPattern, SearchOption.TopDirectoryOnly);
        }

        public static IEnumerable<String> EnumerateFileSystemEntries(String path, String searchPattern, SearchOption searchOption)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            if (searchPattern == null)
                throw new ArgumentNullException("searchPattern");
            if ((searchOption != SearchOption.TopDirectoryOnly) && (searchOption != SearchOption.AllDirectories))
                throw new ArgumentOutOfRangeException("searchOption", Environment.GetResourceString("ArgumentOutOfRange_Enum"));
            Contract.Ensures(Contract.Result<IEnumerable<String>>() != null);
            Contract.EndContractBlock();

            return InternalEnumerateFileSystemEntries(path, searchPattern, searchOption);
        }

        private static IEnumerable<String> InternalEnumerateFileSystemEntries(String path, String searchPattern, SearchOption searchOption)
        {
            Contract.Requires(path != null);
            Contract.Requires(searchPattern != null);
            Contract.Requires(searchOption == SearchOption.AllDirectories || searchOption == SearchOption.TopDirectoryOnly);
            Contract.Ensures(Contract.Result<IEnumerable<String>>() != null);

            return EnumerateFileSystemNames(path, searchPattern, searchOption, true, true);
        }

        private static IEnumerable<String> EnumerateFileSystemNames(String path, String searchPattern, SearchOption searchOption,
                                                            bool includeFiles, bool includeDirs)
        {
            Contract.Requires(path != null);
            Contract.Requires(searchPattern != null);
            Contract.Requires(searchOption == SearchOption.AllDirectories || searchOption == SearchOption.TopDirectoryOnly);
            Contract.Ensures(Contract.Result<IEnumerable<String>>() != null);

            return FileSystemEnumerableFactory.CreateFileNameIterator(path, path, searchPattern,
                                                                        includeFiles, includeDirs, searchOption, true);
        }

        // Retrieves the names of the logical drives on this machine in the 
        // form "C:\". 
        // 
        // Your application must have System Info permission.
        // 
        [System.Security.SecuritySafeCritical]  // auto-generated
        public static String[] GetLogicalDrives()
        {
            Contract.Ensures(Contract.Result<String[]>() != null);

#pragma warning disable 618
            new SecurityPermission(SecurityPermissionFlag.UnmanagedCode).Demand();
#pragma warning restore 618
                                 
            int drives = Win32Native.GetLogicalDrives();
            if (drives==0)
                __Error.WinIOError();
            uint d = (uint)drives;
            int count = 0;
            while (d != 0) {
                if (((int)d & 1) != 0) count++;
                d >>= 1;
            }
            String[] result = new String[count];
            char[] root = new char[] {'A', ':', '\\'};
            d = (uint)drives;
            count = 0;
            while (d != 0) {
                if (((int)d & 1) != 0) {
                    result[count++] = new String(root);
                }
                d >>= 1;
                root[0]++;
            }
            return result;
        }

        [System.Security.SecuritySafeCritical]
        public static String GetDirectoryRoot(String path) {
            if (path==null)
                throw new ArgumentNullException("path");
            Contract.EndContractBlock();
            
            String fullPath = Path.GetFullPathInternal(path);
            String root = fullPath.Substring(0, Path.GetRootLength(fullPath));
            String demandPath = GetDemandDir(root, true);

#if FEATURE_CORECLR
            FileSecurityState state = new FileSecurityState(FileSecurityStateAccess.PathDiscovery, path, demandPath);
            state.EnsureState();
#else
            FileIOPermission.QuickDemand(FileIOPermissionAccess.PathDiscovery, demandPath, false, false);
#endif
                     
            return root;
        }

        internal static String InternalGetDirectoryRoot(String path) {
              if (path == null) return null;
            return path.Substring(0, Path.GetRootLength(path));
        }

         /*===============================CurrentDirectory===============================
        **Action:  Provides a getter and setter for the current directory.  The original
        **         current DirectoryInfo is the one from which the process was started.  
        **Returns: The current DirectoryInfo (from the getter).  Void from the setter.
        **Arguments: The current DirectoryInfo to which to switch to the setter.
        **Exceptions: 
        ==============================================================================*/
        [System.Security.SecuritySafeCritical]
        public static String GetCurrentDirectory()
        {
            return InternalGetCurrentDirectory(true);
        }

        [System.Security.SecurityCritical]
        internal static String UnsafeGetCurrentDirectory()
        {
            return InternalGetCurrentDirectory(false);
        }

        [System.Security.SecuritySafeCritical]
        private static string InternalGetCurrentDirectory(bool checkHost)
        {
            string currentDirectory = (
#if FEATURE_PATHCOMPAT
                AppContextSwitches.UseLegacyPathHandling ? LegacyGetCurrentDirectory() : 
#endif
                NewGetCurrentDirectory());

            string demandPath = GetDemandDir(currentDirectory, true);

#if FEATURE_CORECLR
            if (checkHost)
            {
                FileSecurityState state = new FileSecurityState(FileSecurityStateAccess.PathDiscovery, String.Empty, demandPath);
                state.EnsureState();
            }
#else
            FileIOPermission.QuickDemand(FileIOPermissionAccess.PathDiscovery, demandPath, false, false);
#endif
            return currentDirectory;
        }

#if FEATURE_PATHCOMPAT
        [System.Security.SecurityCritical]
        private static String LegacyGetCurrentDirectory()
        {
            StringBuilder sb = StringBuilderCache.Acquire(Path.MaxPath + 1);
            if (Win32Native.GetCurrentDirectory(sb.Capacity, sb) == 0)
                __Error.WinIOError();
            String currentDirectory = sb.ToString();
            // Note that if we have somehow put our command prompt into short
            // file name mode (ie, by running edlin or a DOS grep, etc), then
            // this will return a short file name.
            if (currentDirectory.IndexOf('~') >= 0) {
                int r = Win32Native.GetLongPathName(currentDirectory, sb, sb.Capacity);
                if (r == 0 || r >= Path.MaxPath) {
                    int errorCode = Marshal.GetLastWin32Error();
                    if (r >= Path.MaxPath)
                        errorCode = Win32Native.ERROR_FILENAME_EXCED_RANGE;
                    if (errorCode != Win32Native.ERROR_FILE_NOT_FOUND &&
                        errorCode != Win32Native.ERROR_PATH_NOT_FOUND &&
                        errorCode != Win32Native.ERROR_INVALID_FUNCTION &&  // by design - enough said.
                        errorCode != Win32Native.ERROR_ACCESS_DENIED)
                        __Error.WinIOError(errorCode, String.Empty);
                }
                currentDirectory = sb.ToString();
            }
            StringBuilderCache.Release(sb);
            String demandPath = GetDemandDir(currentDirectory, true);

            return currentDirectory;
        }
#endif // FEATURE_PATHCOMPAT

        [System.Security.SecurityCritical]
        private static string NewGetCurrentDirectory()
        {
            using (StringBuffer buffer = new StringBuffer(PathInternal.MaxShortPath))
            {
                uint result = 0;
                while ((result = Win32Native.GetCurrentDirectoryW(buffer.CharCapacity, buffer.GetHandle())) > buffer.CharCapacity)
                {
                    // Reported size is greater than the buffer size. Increase the capacity.
                    // The size returned includes the null only if more space is needed (this case).
                    buffer.EnsureCharCapacity(result);
                }

                if (result == 0)
                    __Error.WinIOError();

                buffer.Length = result;

#if !PLATFORM_UNIX
                if (buffer.Contains('~'))
                    return LongPathHelper.GetLongPathName(buffer);
#endif

                return buffer.ToString();
            }
        }

        #if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
        #else
        [System.Security.SecuritySafeCritical]
        #endif
        public static void SetCurrentDirectory(String path)
        {
            if (path==null)
                throw new ArgumentNullException("value");
            if (path.Length==0)
                throw new ArgumentException(Environment.GetResourceString("Argument_PathEmpty"));
            Contract.EndContractBlock();
            if (path.Length >= Path.MaxPath)
                throw new PathTooLongException(Environment.GetResourceString("IO.PathTooLong"));
                
            // This will have some large effects on the rest of the runtime
            // and other appdomains in this process.  Demand unmanaged code.
#pragma warning disable 618
            new SecurityPermission(SecurityPermissionFlag.UnmanagedCode).Demand();
#pragma warning restore 618

            String fulldestDirName = Path.GetFullPathInternal(path);
            
            if (!Win32Native.SetCurrentDirectory(fulldestDirName)) {
                // If path doesn't exist, this sets last error to 2 (File 
                // not Found).  LEGACY: This may potentially have worked correctly
                // on Win9x, maybe.
                int errorCode = Marshal.GetLastWin32Error();
                if (errorCode == Win32Native.ERROR_FILE_NOT_FOUND)
                    errorCode = Win32Native.ERROR_PATH_NOT_FOUND;
                __Error.WinIOError(errorCode, fulldestDirName);
            }
        }

        [System.Security.SecuritySafeCritical]
        public static void Move(String sourceDirName,String destDirName) {
            InternalMove(sourceDirName, destDirName, true);
        }

        [System.Security.SecurityCritical]
        internal static void UnsafeMove(String sourceDirName,String destDirName) {
            InternalMove(sourceDirName, destDirName, false);
        }

        [System.Security.SecurityCritical]
        private static void InternalMove(String sourceDirName,String destDirName,bool checkHost) {
            if (sourceDirName==null)
                throw new ArgumentNullException("sourceDirName");
            if (sourceDirName.Length==0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyFileName"), "sourceDirName");
            
            if (destDirName==null)
                throw new ArgumentNullException("destDirName");
            if (destDirName.Length==0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyFileName"), "destDirName");
            Contract.EndContractBlock();

            String fullsourceDirName = Path.GetFullPathInternal(sourceDirName);
            String sourcePath = GetDemandDir(fullsourceDirName, false);

            if (PathInternal.IsDirectoryTooLong(sourcePath))
                throw new PathTooLongException(Environment.GetResourceString("IO.PathTooLong"));

            String fulldestDirName = Path.GetFullPathInternal(destDirName);
            String destPath = GetDemandDir(fulldestDirName, false);

            if (PathInternal.IsDirectoryTooLong(destPath))
                throw new PathTooLongException(Environment.GetResourceString("IO.PathTooLong"));

#if FEATURE_CORECLR
            if (checkHost) {
                FileSecurityState sourceState = new FileSecurityState(FileSecurityStateAccess.Write | FileSecurityStateAccess.Read, sourceDirName, sourcePath);
                FileSecurityState destState = new FileSecurityState(FileSecurityStateAccess.Write, destDirName, destPath);
                sourceState.EnsureState();
                destState.EnsureState();
            }
#else
            FileIOPermission.QuickDemand(FileIOPermissionAccess.Write | FileIOPermissionAccess.Read, sourcePath, false, false);
            FileIOPermission.QuickDemand(FileIOPermissionAccess.Write, destPath, false, false);
#endif

            if (String.Compare(sourcePath, destPath, StringComparison.OrdinalIgnoreCase) == 0)
                throw new IOException(Environment.GetResourceString("IO.IO_SourceDestMustBeDifferent"));

            String sourceRoot = Path.GetPathRoot(sourcePath);
            String destinationRoot = Path.GetPathRoot(destPath);
            if (String.Compare(sourceRoot, destinationRoot, StringComparison.OrdinalIgnoreCase) != 0)
                throw new IOException(Environment.GetResourceString("IO.IO_SourceDestMustHaveSameRoot"));
    
            if (!Win32Native.MoveFile(sourceDirName, destDirName))
            {
                int hr = Marshal.GetLastWin32Error();
                if (hr == Win32Native.ERROR_FILE_NOT_FOUND) // Source dir not found
                {
                    hr = Win32Native.ERROR_PATH_NOT_FOUND;
                    __Error.WinIOError(hr, fullsourceDirName);
                }
                // This check was originally put in for Win9x (unfortunately without special casing it to be for Win9x only). We can't change the NT codepath now for backcomp reasons.
                if (hr == Win32Native.ERROR_ACCESS_DENIED) // WinNT throws IOException. This check is for Win9x. We can't change it for backcomp.
                    throw new IOException(Environment.GetResourceString("UnauthorizedAccess_IODenied_Path", sourceDirName), Win32Native.MakeHRFromErrorCode(hr));
                __Error.WinIOError(hr, String.Empty);
            }
        }

        [System.Security.SecuritySafeCritical]
        public static void Delete(String path)
        {
            String fullPath = Path.GetFullPathInternal(path);
            Delete(fullPath, path, false, true);
        }

        [System.Security.SecuritySafeCritical]
        public static void Delete(String path, bool recursive)
        {
            String fullPath = Path.GetFullPathInternal(path);
            Delete(fullPath, path, recursive, true);
        }

        [System.Security.SecurityCritical] 
        internal static void UnsafeDelete(String path, bool recursive)
        {
            String fullPath = Path.GetFullPathInternal(path);
            Delete(fullPath, path, recursive, false);
        }

        // Called from DirectoryInfo as well.  FullPath is fully qualified,
        // while the user path is used for feedback in exceptions.
        [System.Security.SecurityCritical]  // auto-generated
        internal static void Delete(String fullPath, String userPath, bool recursive, bool checkHost)
        {
            String demandPath;
            
            // If not recursive, do permission check only on this directory
            // else check for the whole directory structure rooted below 
            demandPath = GetDemandDir(fullPath, !recursive);
            
#if FEATURE_CORECLR
            if (checkHost) 
            {
                FileSecurityState state = new FileSecurityState(FileSecurityStateAccess.Write, userPath, demandPath);
                state.EnsureState();
            }
#else
            // Make sure we have write permission to this directory
            new FileIOPermission(FileIOPermissionAccess.Write, new String[] { demandPath }, false, false ).Demand();
#endif

            // Do not recursively delete through reparse points.  Perhaps in a 
            // future version we will add a new flag to control this behavior, 
            // but for now we're much safer if we err on the conservative side.
            // This applies to symbolic links and mount points.
            Win32Native.WIN32_FILE_ATTRIBUTE_DATA data = new Win32Native.WIN32_FILE_ATTRIBUTE_DATA();
            int dataInitialised = File.FillAttributeInfo(fullPath, ref data, false, true);
            if (dataInitialised != 0) {
                // Ensure we throw a DirectoryNotFoundException.
                if (dataInitialised == Win32Native.ERROR_FILE_NOT_FOUND)
                    dataInitialised = Win32Native.ERROR_PATH_NOT_FOUND;
                __Error.WinIOError(dataInitialised, fullPath);
            }

            if (((FileAttributes)data.fileAttributes & FileAttributes.ReparsePoint) != 0)
                recursive = false;

            DeleteHelper(fullPath, userPath, recursive, true);
        }

        // Note that fullPath is fully qualified, while userPath may be 
        // relative.  Use userPath for all exception messages to avoid leaking
        // fully qualified path information.
        [System.Security.SecurityCritical]  // auto-generated
        private static void DeleteHelper(String fullPath, String userPath, bool recursive, bool throwOnTopLevelDirectoryNotFound)
        {
            bool r;
            int hr;
            Exception ex = null;

            // Do not recursively delete through reparse points.  Perhaps in a 
            // future version we will add a new flag to control this behavior, 
            // but for now we're much safer if we err on the conservative side.
            // This applies to symbolic links and mount points.
            // Note the logic to check whether fullPath is a reparse point is
            // in Delete(String, String, bool), and will set "recursive" to false.
            // Note that Win32's DeleteFile and RemoveDirectory will just delete
            // the reparse point itself.

            if (recursive) {
                Win32Native.WIN32_FIND_DATA data = new Win32Native.WIN32_FIND_DATA();
                
                // Open a Find handle
                using (SafeFindHandle hnd = Win32Native.FindFirstFile(fullPath+Path.DirectorySeparatorCharAsString+"*", data)) {
                    if (hnd.IsInvalid) {
                        hr = Marshal.GetLastWin32Error();
                        __Error.WinIOError(hr, fullPath);
                    }
        
                    do {
                        bool isDir = (0!=(data.dwFileAttributes & Win32Native.FILE_ATTRIBUTE_DIRECTORY));
                        if (isDir) {
                            // Skip ".", "..".
                            if (data.cFileName.Equals(".") || data.cFileName.Equals(".."))
                                continue;

                            // Recurse for all directories, unless they are 
                            // reparse points.  Do not follow mount points nor
                            // symbolic links, but do delete the reparse point 
                            // itself.
                            bool shouldRecurse = (0 == (data.dwFileAttributes & (int) FileAttributes.ReparsePoint));
                            if (shouldRecurse) {
                                String newFullPath = Path.InternalCombine(fullPath, data.cFileName);
                                String newUserPath = Path.InternalCombine(userPath, data.cFileName);                        
                                try {
                                    DeleteHelper(newFullPath, newUserPath, recursive, false);
                                }
                                catch(Exception e) {
                                    if (ex == null) {
                                        ex = e;
                                    }
                                }
                            }
                            else {
                                // Check to see if this is a mount point, and
                                // unmount it.
                                if (data.dwReserved0 == Win32Native.IO_REPARSE_TAG_MOUNT_POINT) {
                                    // Use full path plus a trailing '\'
                                    String mountPoint = Path.InternalCombine(fullPath, data.cFileName + Path.DirectorySeparatorChar);
                                    r = Win32Native.DeleteVolumeMountPoint(mountPoint);
                                    if (!r) {
                                        hr = Marshal.GetLastWin32Error();
                                        if (hr != Win32Native.ERROR_PATH_NOT_FOUND) {
                                            try {
                                                __Error.WinIOError(hr, data.cFileName);
                                            }
                                            catch(Exception e) {
                                                if (ex == null) {
                                                    ex = e;
                                                }
                                            }
                                        }
                                    }
                                }

                                // RemoveDirectory on a symbolic link will
                                // remove the link itself.
                                String reparsePoint = Path.InternalCombine(fullPath, data.cFileName);
                                r = Win32Native.RemoveDirectory(reparsePoint);
                                if (!r) {
                                    hr = Marshal.GetLastWin32Error();
                                    if (hr != Win32Native.ERROR_PATH_NOT_FOUND) {
                                        try {
                                            __Error.WinIOError(hr, data.cFileName);
                                        }
                                        catch(Exception e) {
                                            if (ex == null) {
                                                ex = e;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        else {
                            String fileName = Path.InternalCombine(fullPath, data.cFileName);
                            r = Win32Native.DeleteFile(fileName);
                            if (!r) {
                                hr = Marshal.GetLastWin32Error();
                                if (hr != Win32Native.ERROR_FILE_NOT_FOUND) {
                                    try {
                                        __Error.WinIOError(hr, data.cFileName);
                                    }
                                    catch (Exception e) {
                                        if (ex == null) {
                                            ex = e;
                                        }
                                    }
                                }
                            }
                        }
                    } while (Win32Native.FindNextFile(hnd, data));
                    // Make sure we quit with a sensible error.
                    hr = Marshal.GetLastWin32Error();
                }

                if (ex != null) 
                    throw ex;
                if (hr!=0 && hr!=Win32Native.ERROR_NO_MORE_FILES) 
                    __Error.WinIOError(hr, userPath);
            }

            r = Win32Native.RemoveDirectory(fullPath);
            
            if (!r) {
                hr = Marshal.GetLastWin32Error();
                if (hr == Win32Native.ERROR_FILE_NOT_FOUND) // A dubious error code.
                    hr = Win32Native.ERROR_PATH_NOT_FOUND;
                // This check was originally put in for Win9x (unfortunately without special casing it to be for Win9x only). We can't change the NT codepath now for backcomp reasons.
                if (hr == Win32Native.ERROR_ACCESS_DENIED) 
                    throw new IOException(Environment.GetResourceString("UnauthorizedAccess_IODenied_Path", userPath));

                // don't throw the DirectoryNotFoundException since this is a subdir and there could be a race condition
                // between two Directory.Delete callers
                if (hr == Win32Native.ERROR_PATH_NOT_FOUND && !throwOnTopLevelDirectoryNotFound)
                    return;  

                __Error.WinIOError(hr, fullPath);
            }
        }

        // WinNT only. Win9x this code will not work.
        [System.Security.SecurityCritical]  // auto-generated
        private static SafeFileHandle OpenHandle(String path)
        {
            String fullPath = Path.GetFullPathInternal(path);
            String root = Path.GetPathRoot(fullPath);
            if (root == fullPath && root[1] == Path.VolumeSeparatorChar)
                throw new ArgumentException(Environment.GetResourceString("Arg_PathIsVolume"));

#if !FEATURE_CORECLR
            FileIOPermission.QuickDemand(FileIOPermissionAccess.Write, GetDemandDir(fullPath, true), false, false);
#endif

            SafeFileHandle handle = Win32Native.SafeCreateFile (
                fullPath,
                GENERIC_WRITE,
                (FileShare) (FILE_SHARE_WRITE|FILE_SHARE_DELETE),
                null,
                FileMode.Open,
                FILE_FLAG_BACKUP_SEMANTICS,
                IntPtr.Zero
            );

            if (handle.IsInvalid) {
                int hr = Marshal.GetLastWin32Error();
                __Error.WinIOError(hr, fullPath);
            }
            return handle;
        }

        private const int FILE_ATTRIBUTE_DIRECTORY = 0x00000010;    
        private const int GENERIC_WRITE = unchecked((int)0x40000000);
        private const int FILE_SHARE_WRITE = 0x00000002;
        private const int FILE_SHARE_DELETE = 0x00000004;
        private const int OPEN_EXISTING = 0x00000003;
        private const int FILE_FLAG_BACKUP_SEMANTICS = 0x02000000;
    }

}

