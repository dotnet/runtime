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

using System.Collections.Generic;
using System.Security;
using System.Security.Permissions;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Diagnostics.Contracts;

namespace System.IO
{
    [ComVisible(true)]
    public static class Directory {
        public static DirectoryInfo GetParent(String path)
        {
            if (path==null)
                throw new ArgumentNullException(nameof(path));

            if (path.Length==0)
                throw new ArgumentException(Environment.GetResourceString("Argument_PathEmpty"), nameof(path));
            Contract.EndContractBlock();

            string fullPath = Path.GetFullPath(path);

            string s = Path.GetDirectoryName(fullPath);
            if (s==null)
                 return null;
            return new DirectoryInfo(s);
        }

        public static DirectoryInfo CreateDirectory(String path) {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            if (path.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_PathEmpty"));
            Contract.EndContractBlock();

            return InternalCreateDirectoryHelper(path);
        }

        internal static DirectoryInfo InternalCreateDirectoryHelper(String path)
        {
            Contract.Requires(path != null);
            Contract.Requires(path.Length != 0);

            String fullPath = Path.GetFullPath(path);

            InternalCreateDirectory(fullPath, path, null);

            return new DirectoryInfo(fullPath, false);
        }

        internal unsafe static void InternalCreateDirectory(String fullPath, String path, Object dirSecurityObj)
        {
            int length = fullPath.Length;

            // We need to trim the trailing slash or the code will try to create 2 directories of the same name.
            if (length >= 2 && PathInternal.IsDirectorySeparator(fullPath[length - 1]))
                length--;
            
            int lengthRoot = PathInternal.GetRootLength(fullPath);

            // For UNC paths that are only // or /// 
            if (length == 2 && PathInternal.IsDirectorySeparator(fullPath[1]))
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

            // If we were passed a DirectorySecurity, convert it to a security
            // descriptor and set it in he call to CreateDirectory.
            Win32Native.SECURITY_ATTRIBUTES secAttrs = null;

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
                        if (File.InternalExists(name) || (!InternalExists(name, out currentError) && currentError == Win32Native.ERROR_ACCESS_DENIED))
                        {
                            firstError = currentError;
                            errorString = name;
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
        public static bool Exists(String path)
        {
            return InternalExistsHelper(path);
        }

        internal static bool InternalExistsHelper(String path) {
            try
            {
                if (path == null)
                    return false;
                if (path.Length == 0)
                    return false;

                return InternalExists(Path.GetFullPath(path));
            }
            catch (ArgumentException) { }
            catch (NotSupportedException) { }  // Security can throw this on ":"
            catch (SecurityException) { }
            catch (IOException) { }
            catch (UnauthorizedAccessException)
            {
                Debug.Assert(false, "Ignore this assert and send a repro to Microsoft. This assert was tracking purposes only.");
            }
            return false;
        }

        // Determine whether path describes an existing directory
        // on disk, avoiding security checks.
        internal static bool InternalExists(String path) {
            int lastError = Win32Native.ERROR_SUCCESS;
            return InternalExists(path, out lastError);
        }

        // Determine whether path describes an existing directory
        // on disk, avoiding security checks.
        internal static bool InternalExists(String path, out int lastError) {
            Win32Native.WIN32_FILE_ATTRIBUTE_DATA data = new Win32Native.WIN32_FILE_ATTRIBUTE_DATA();
            lastError = File.FillAttributeInfo(path, ref data, false, true);

            return (lastError == 0) && (data.fileAttributes != -1)
                    && ((data.fileAttributes & Win32Native.FILE_ATTRIBUTE_DIRECTORY) != 0);
        }

        public static DateTime GetCreationTime(String path)
        {
            return File.GetCreationTime(path);
        }

        public static DateTime GetCreationTimeUtc(String path)
        {
            return File.GetCreationTimeUtc(path);
        }

        public static DateTime GetLastWriteTime(String path)
        {
            return File.GetLastWriteTime(path);
        }

        public static DateTime GetLastWriteTimeUtc(String path)
        {
            return File.GetLastWriteTimeUtc(path);
        }

        public static DateTime GetLastAccessTime(String path)
        {
            return File.GetLastAccessTime(path);
        }

        public static DateTime GetLastAccessTimeUtc(String path)
        {
            return File.GetLastAccessTimeUtc(path);
        }

        // Returns an array of filenames in the DirectoryInfo specified by path
        public static String[] GetFiles(String path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            Contract.Ensures(Contract.Result<String[]>() != null);
            Contract.EndContractBlock();

            return InternalGetFiles(path, "*", SearchOption.TopDirectoryOnly);
        }

        // Returns an array of Files in the current DirectoryInfo matching the 
        // given search pattern (ie, "*.txt").
        public static String[] GetFiles(String path, String searchPattern)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            if (searchPattern == null)
                throw new ArgumentNullException(nameof(searchPattern));
            Contract.Ensures(Contract.Result<String[]>() != null);
            Contract.EndContractBlock();

            return InternalGetFiles(path, searchPattern, SearchOption.TopDirectoryOnly);
        }

        // Returns an array of Files in the current DirectoryInfo matching the 
        // given search pattern (ie, "*.txt") and search option
        public static String[] GetFiles(String path, String searchPattern, SearchOption searchOption)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            if (searchPattern == null)
                throw new ArgumentNullException(nameof(searchPattern));
            if ((searchOption != SearchOption.TopDirectoryOnly) && (searchOption != SearchOption.AllDirectories))
                throw new ArgumentOutOfRangeException(nameof(searchOption), Environment.GetResourceString("ArgumentOutOfRange_Enum"));
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
                throw new ArgumentNullException(nameof(path));
            Contract.Ensures(Contract.Result<String[]>() != null);
            Contract.EndContractBlock();

            return InternalGetDirectories(path, "*", SearchOption.TopDirectoryOnly);
        }

        // Returns an array of Directories in the current DirectoryInfo matching the 
        // given search criteria (ie, "*.txt").
        public static String[] GetDirectories(String path, String searchPattern)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            if (searchPattern == null)
                throw new ArgumentNullException(nameof(searchPattern));
            Contract.Ensures(Contract.Result<String[]>() != null);
            Contract.EndContractBlock();

            return InternalGetDirectories(path, searchPattern, SearchOption.TopDirectoryOnly);
        }

        // Returns an array of Directories in the current DirectoryInfo matching the 
        // given search criteria (ie, "*.txt").
        public static String[] GetDirectories(String path, String searchPattern, SearchOption searchOption)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            if (searchPattern == null)
                throw new ArgumentNullException(nameof(searchPattern));
            if ((searchOption != SearchOption.TopDirectoryOnly) && (searchOption != SearchOption.AllDirectories))
                throw new ArgumentOutOfRangeException(nameof(searchOption), Environment.GetResourceString("ArgumentOutOfRange_Enum"));
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
                throw new ArgumentNullException(nameof(path));
            Contract.Ensures(Contract.Result<String[]>() != null);
            Contract.EndContractBlock();

            return InternalGetFileSystemEntries(path, "*", SearchOption.TopDirectoryOnly);
        }

        // Returns an array of strongly typed FileSystemInfo entries in the path with the
        // given search criteria (ie, "*.txt"). We disallow .. as a part of the search criteria
        public static String[] GetFileSystemEntries(String path, String searchPattern)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            if (searchPattern == null)
                throw new ArgumentNullException(nameof(searchPattern));
            Contract.Ensures(Contract.Result<String[]>() != null);
            Contract.EndContractBlock();

            return InternalGetFileSystemEntries(path, searchPattern, SearchOption.TopDirectoryOnly);
        }

        // Returns an array of strongly typed FileSystemInfo entries in the path with the
        // given search criteria (ie, "*.txt"). We disallow .. as a part of the search criteria
        public static String[] GetFileSystemEntries(String path, String searchPattern, SearchOption searchOption)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            if (searchPattern == null)
                throw new ArgumentNullException(nameof(searchPattern));
            if ((searchOption != SearchOption.TopDirectoryOnly) && (searchOption != SearchOption.AllDirectories))
                throw new ArgumentOutOfRangeException(nameof(searchOption), Environment.GetResourceString("ArgumentOutOfRange_Enum"));
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
                throw new ArgumentNullException(nameof(path));
            Contract.EndContractBlock();

            return InternalEnumerateDirectories(path, "*", SearchOption.TopDirectoryOnly);
        }

        public static IEnumerable<String> EnumerateDirectories(String path, String searchPattern)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            if (searchPattern == null)
                throw new ArgumentNullException(nameof(searchPattern));
            Contract.EndContractBlock();

            return InternalEnumerateDirectories(path, searchPattern, SearchOption.TopDirectoryOnly);
        }

        public static IEnumerable<String> EnumerateDirectories(String path, String searchPattern, SearchOption searchOption)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            if (searchPattern == null)
                throw new ArgumentNullException(nameof(searchPattern));
            if ((searchOption != SearchOption.TopDirectoryOnly) && (searchOption != SearchOption.AllDirectories))
                throw new ArgumentOutOfRangeException(nameof(searchOption), Environment.GetResourceString("ArgumentOutOfRange_Enum"));
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
                throw new ArgumentNullException(nameof(path));
            Contract.Ensures(Contract.Result<IEnumerable<String>>() != null);
            Contract.EndContractBlock();

            return InternalEnumerateFiles(path, "*", SearchOption.TopDirectoryOnly);
        }

        public static IEnumerable<String> EnumerateFiles(String path, String searchPattern)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            if (searchPattern == null)
                throw new ArgumentNullException(nameof(searchPattern));
            Contract.Ensures(Contract.Result<IEnumerable<String>>() != null);
            Contract.EndContractBlock();

            return InternalEnumerateFiles(path, searchPattern, SearchOption.TopDirectoryOnly);
        }

        public static IEnumerable<String> EnumerateFiles(String path, String searchPattern, SearchOption searchOption)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            if (searchPattern == null)
                throw new ArgumentNullException(nameof(searchPattern));
            if ((searchOption != SearchOption.TopDirectoryOnly) && (searchOption != SearchOption.AllDirectories))
                throw new ArgumentOutOfRangeException(nameof(searchOption), Environment.GetResourceString("ArgumentOutOfRange_Enum"));
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
                throw new ArgumentNullException(nameof(path));
            Contract.Ensures(Contract.Result<IEnumerable<String>>() != null);
            Contract.EndContractBlock();

            return InternalEnumerateFileSystemEntries(path, "*", SearchOption.TopDirectoryOnly);
        }

        public static IEnumerable<String> EnumerateFileSystemEntries(String path, String searchPattern)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            if (searchPattern == null)
                throw new ArgumentNullException(nameof(searchPattern));
            Contract.Ensures(Contract.Result<IEnumerable<String>>() != null);
            Contract.EndContractBlock();

            return InternalEnumerateFileSystemEntries(path, searchPattern, SearchOption.TopDirectoryOnly);
        }

        public static IEnumerable<String> EnumerateFileSystemEntries(String path, String searchPattern, SearchOption searchOption)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            if (searchPattern == null)
                throw new ArgumentNullException(nameof(searchPattern));
            if ((searchOption != SearchOption.TopDirectoryOnly) && (searchOption != SearchOption.AllDirectories))
                throw new ArgumentOutOfRangeException(nameof(searchOption), Environment.GetResourceString("ArgumentOutOfRange_Enum"));
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

        public static String GetDirectoryRoot(String path) {
            if (path==null)
                throw new ArgumentNullException(nameof(path));
            Contract.EndContractBlock();

            string fullPath = Path.GetFullPath(path);
            string root = fullPath.Substring(0, PathInternal.GetRootLength(fullPath));

            return root;
        }

        internal static String InternalGetDirectoryRoot(String path) {
              if (path == null) return null;
            return path.Substring(0, PathInternal.GetRootLength(path));
        }

         /*===============================CurrentDirectory===============================
        **Action:  Provides a getter and setter for the current directory.  The original
        **         current DirectoryInfo is the one from which the process was started.  
        **Returns: The current DirectoryInfo (from the getter).  Void from the setter.
        **Arguments: The current DirectoryInfo to which to switch to the setter.
        **Exceptions: 
        ==============================================================================*/
        public static String GetCurrentDirectory()
        {
            // Start with a buffer the size of MAX_PATH
            using (StringBuffer buffer = new StringBuffer(260))
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
                    return Path.GetFullPath(buffer.ToString());
#endif

                return buffer.ToString();
            }
        }

        public static void SetCurrentDirectory(String path)
        {
            if (path==null)
                throw new ArgumentNullException("value");
            if (path.Length==0)
                throw new ArgumentException(Environment.GetResourceString("Argument_PathEmpty"));
            if (path.Length >= Path.MaxPath)
                throw new PathTooLongException(Environment.GetResourceString("IO.PathTooLong"));

            String fulldestDirName = Path.GetFullPath(path);
            
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

        public static void Move(String sourceDirName,String destDirName)
        {
            if (sourceDirName==null)
                throw new ArgumentNullException(nameof(sourceDirName));
            if (sourceDirName.Length==0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyFileName"), nameof(sourceDirName));
            if (destDirName==null)
                throw new ArgumentNullException(nameof(destDirName));
            if (destDirName.Length==0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyFileName"), nameof(destDirName));

            String sourcePath = Path.GetFullPath(sourceDirName);
            String destPath = Path.GetFullPath(destDirName);

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
                    __Error.WinIOError(hr, sourcePath);
                }
                // This check was originally put in for Win9x (unfortunately without special casing it to be for Win9x only). We can't change the NT codepath now for backcomp reasons.
                if (hr == Win32Native.ERROR_ACCESS_DENIED) // WinNT throws IOException. This check is for Win9x. We can't change it for backcomp.
                    throw new IOException(Environment.GetResourceString("UnauthorizedAccess_IODenied_Path", sourceDirName), Win32Native.MakeHRFromErrorCode(hr));
                __Error.WinIOError(hr, String.Empty);
            }
        }

        public static void Delete(String path)
        {
            String fullPath = Path.GetFullPath(path);
            Delete(fullPath, path, false);
        }

        public static void Delete(String path, bool recursive)
        {
            String fullPath = Path.GetFullPath(path);
            Delete(fullPath, path, recursive);
        }

         // Called from DirectoryInfo as well.  FullPath is fully qualified,
        // while the user path is used for feedback in exceptions.
        internal static void Delete(String fullPath, String userPath, bool recursive)
        {
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

        // Note that fullPath is fully qualified, while userPath may be relative.
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
                using (SafeFindHandle hnd = Win32Native.FindFirstFile(fullPath + Path.DirectorySeparatorChar + "*", data)) {
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
                                String newFullPath = Path.Combine(fullPath, data.cFileName);
                                String newUserPath = Path.Combine(userPath, data.cFileName);
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
                                    String mountPoint = Path.Combine(fullPath, data.cFileName + Path.DirectorySeparatorChar);
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
                                String reparsePoint = Path.Combine(fullPath, data.cFileName);
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
                            String fileName = Path.Combine(fullPath, data.cFileName);
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
    }
}

