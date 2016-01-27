// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** 
** 
**
**
** Purpose: Enumerates files and dirs
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

namespace System.IO
{

    // Overview:
    // The key methods instantiate FileSystemEnumerableIterators. These compose the iterator with search result
    // handlers that instantiate the FileInfo, DirectoryInfo, String, etc. The handlers then perform any
    // additional required permission demands. 
    internal static class FileSystemEnumerableFactory
    {
        internal static IEnumerable<String> CreateFileNameIterator(String path, String originalUserPath, String searchPattern,
                                                                    bool includeFiles, bool includeDirs, SearchOption searchOption, bool checkHost)
        {
            Contract.Requires(path != null);
            Contract.Requires(originalUserPath != null);
            Contract.Requires(searchPattern != null);

            SearchResultHandler<String> handler = new StringResultHandler(includeFiles, includeDirs);
            return new FileSystemEnumerableIterator<String>(path, originalUserPath, searchPattern, searchOption, handler, checkHost);
        }

        internal static IEnumerable<FileInfo> CreateFileInfoIterator(String path, String originalUserPath, String searchPattern, SearchOption searchOption)
        {
            Contract.Requires(path != null);
            Contract.Requires(originalUserPath != null);
            Contract.Requires(searchPattern != null);

            SearchResultHandler<FileInfo> handler = new FileInfoResultHandler();
            return new FileSystemEnumerableIterator<FileInfo>(path, originalUserPath, searchPattern, searchOption, handler, true);
        }

        internal static IEnumerable<DirectoryInfo> CreateDirectoryInfoIterator(String path, String originalUserPath, String searchPattern, SearchOption searchOption)
        {

            Contract.Requires(path != null);
            Contract.Requires(originalUserPath != null);
            Contract.Requires(searchPattern != null);

            SearchResultHandler<DirectoryInfo> handler = new DirectoryInfoResultHandler();
            return new FileSystemEnumerableIterator<DirectoryInfo>(path, originalUserPath, searchPattern, searchOption, handler, true);
        }

        internal static IEnumerable<FileSystemInfo> CreateFileSystemInfoIterator(String path, String originalUserPath, String searchPattern, SearchOption searchOption)
        {
            Contract.Requires(path != null);
            Contract.Requires(originalUserPath != null);
            Contract.Requires(searchPattern != null);

            SearchResultHandler<FileSystemInfo> handler = new FileSystemInfoResultHandler();
            return new FileSystemEnumerableIterator<FileSystemInfo>(path, originalUserPath, searchPattern, searchOption, handler, true);
        }
    }

    // Abstract Iterator, borrowed from Linq. Used in anticipation of need for similar enumerables
    // in the future
    abstract internal class Iterator<TSource> : IEnumerable<TSource>, IEnumerator<TSource>
    {
        int threadId;
        internal int state;
        internal TSource current;

        public Iterator()
        {
            threadId = Thread.CurrentThread.ManagedThreadId;
        }

        public TSource Current
        {
            get { return current; }
        }

        protected abstract Iterator<TSource> Clone();

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            current = default(TSource);
            state = -1;
        }

        public IEnumerator<TSource> GetEnumerator()
        {
            if (threadId == Thread.CurrentThread.ManagedThreadId && state == 0)
            {
                state = 1;
                return this;
            }

            Iterator<TSource> duplicate = Clone();
            duplicate.state = 1;
            return duplicate;
        }

        public abstract bool MoveNext();

        object IEnumerator.Current
        {
            get { return Current; }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        void IEnumerator.Reset()
        {
            throw new NotSupportedException();
        }

    }

    // Overview:
    // Enumerates file system entries matching the search parameters. For recursive searches this
    // searches through all the sub dirs and executes the search criteria against every dir.
    // 
    // Generic implementation:
    // FileSystemEnumerableIterator is generic. When it gets a WIN32_FIND_DATA, it calls the 
    // result handler to create an instance of the generic type. 
    // 
    // Usage:
    // Use FileSystemEnumerableFactory to obtain FSEnumerables that can enumerate file system 
    // entries as String path names, FileInfos, DirectoryInfos, or FileSystemInfos.
    // 
    // Security:
    // For all the dirs/files returned, demands path discovery permission for their parent folders
    internal class FileSystemEnumerableIterator<TSource> : Iterator<TSource>
    {

        private const int STATE_INIT = 1;
        private const int STATE_SEARCH_NEXT_DIR = 2;
        private const int STATE_FIND_NEXT_FILE = 3;
        private const int STATE_FINISH = 4;

        private SearchResultHandler<TSource> _resultHandler;
        private List<Directory.SearchData> searchStack;
        private Directory.SearchData searchData;
        private String searchCriteria;
        [System.Security.SecurityCritical]
        SafeFindHandle _hnd = null;
        bool needsParentPathDiscoveryDemand;

        // empty means we know in advance that we won’t find any search results, which can happen if:
        // 1. we don’t have a search pattern
        // 2. we’re enumerating only the top directory and found no matches during the first call
        // This flag allows us to return early for these cases. We can’t know this in advance for
        // SearchOption.AllDirectories because we do a “*” search for subdirs and then use the
        // searchPattern at each directory level.
        bool empty;

        private String userPath;
        private SearchOption searchOption;
        private String fullPath;
        private String normalizedSearchPath;
        private int oldMode;
        private bool _checkHost;

        [System.Security.SecuritySafeCritical]
        internal FileSystemEnumerableIterator(String path, String originalUserPath, String searchPattern, SearchOption searchOption, SearchResultHandler<TSource> resultHandler, bool checkHost)
        {
            Contract.Requires(path != null);
            Contract.Requires(originalUserPath != null);
            Contract.Requires(searchPattern != null);
            Contract.Requires(searchOption == SearchOption.AllDirectories || searchOption == SearchOption.TopDirectoryOnly);
            Contract.Requires(resultHandler != null);

            oldMode = Win32Native.SetErrorMode(Win32Native.SEM_FAILCRITICALERRORS);

            searchStack = new List<Directory.SearchData>();

            String normalizedSearchPattern = NormalizeSearchPattern(searchPattern);

            if (normalizedSearchPattern.Length == 0)
            {
                empty = true;
            }
            else
            {
                _resultHandler = resultHandler;
                this.searchOption = searchOption;

                fullPath = Path.GetFullPathInternal(path);
                String fullSearchString = GetFullSearchString(fullPath, normalizedSearchPattern);
                normalizedSearchPath = Path.GetDirectoryName(fullSearchString);

                // permission demands
                String[] demandPaths = new String[2];
                // Any illegal chars such as *, ? will be caught by FileIOPermission.HasIllegalCharacters
                demandPaths[0] = Directory.GetDemandDir(fullPath, true);
                // For filters like foo\*.cs we need to verify if the directory foo is not denied access.
                // Do a demand on the combined path so that we can fail early in case of deny
                demandPaths[1] = Directory.GetDemandDir(normalizedSearchPath, true);
                _checkHost = checkHost;
#if FEATURE_CORECLR
                if (checkHost)
                {
                    FileSecurityState state1 = new FileSecurityState(FileSecurityStateAccess.PathDiscovery, String.Empty, demandPaths[0]);
                    state1.EnsureState();
                    FileSecurityState state2 = new FileSecurityState(FileSecurityStateAccess.PathDiscovery, String.Empty, demandPaths[1]);
                    state2.EnsureState();
                }
#else
                new FileIOPermission(FileIOPermissionAccess.PathDiscovery, demandPaths, false, false).Demand();
#endif

                // normalize search criteria
                searchCriteria = GetNormalizedSearchCriteria(fullSearchString, normalizedSearchPath);

                // fix up user path
                String searchPatternDirName = Path.GetDirectoryName(normalizedSearchPattern);
                String userPathTemp = originalUserPath;
                if (searchPatternDirName != null && searchPatternDirName.Length != 0)
                {
                    userPathTemp = Path.Combine(userPathTemp, searchPatternDirName);
                }
                this.userPath = userPathTemp;

                searchData = new Directory.SearchData(normalizedSearchPath, this.userPath, searchOption);

                CommonInit();
            }

        }

        [System.Security.SecurityCritical]
        private void CommonInit()
        {
            Contract.Assert(searchCriteria != null && searchData != null, "searchCriteria and searchData should be initialized");

            // Execute searchCriteria against the current directory
            String searchPath = Path.InternalCombine(searchData.fullPath, searchCriteria);

            Win32Native.WIN32_FIND_DATA data = new Win32Native.WIN32_FIND_DATA();

            // Open a Find handle
            _hnd = Win32Native.FindFirstFile(searchPath, data);

            if (_hnd.IsInvalid)
            {
                int hr = Marshal.GetLastWin32Error();
                if (hr != Win32Native.ERROR_FILE_NOT_FOUND && hr != Win32Native.ERROR_NO_MORE_FILES)
                {
                    HandleError(hr, searchData.fullPath);
                }
                else
                {
                    // flag this as empty only if we're searching just top directory
                    // Used in fast path for top directory only
                    empty = searchData.searchOption == SearchOption.TopDirectoryOnly;
                }
            }
            // fast path for TopDirectoryOnly. If we have a result, go ahead and set it to 
            // current. If empty, dispose handle.
            if (searchData.searchOption == SearchOption.TopDirectoryOnly)
            {
                if (empty)
                {
                    _hnd.Dispose();
                }
                else
                {
                    SearchResult searchResult = CreateSearchResult(searchData, data);
                    if (_resultHandler.IsResultIncluded(searchResult))
                    {
                        current = _resultHandler.CreateObject(searchResult);
                    }
                }
            }
            // for AllDirectories, we first recurse into dirs, so cleanup and add searchData 
            // to the stack
            else
            {
                _hnd.Dispose();
                searchStack.Add(searchData);
            }
        }

        [System.Security.SecuritySafeCritical]
        private FileSystemEnumerableIterator(String fullPath, String normalizedSearchPath, String searchCriteria, String userPath, SearchOption searchOption, SearchResultHandler<TSource> resultHandler, bool checkHost)
        {
            this.fullPath = fullPath;
            this.normalizedSearchPath = normalizedSearchPath;
            this.searchCriteria = searchCriteria;
            this._resultHandler = resultHandler;
            this.userPath = userPath;
            this.searchOption = searchOption;
            this._checkHost = checkHost;

            searchStack = new List<Directory.SearchData>();

            if (searchCriteria != null)
            {
                // permission demands
                String[] demandPaths = new String[2];
                // Any illegal chars such as *, ? will be caught by FileIOPermission.HasIllegalCharacters
                demandPaths[0] = Directory.GetDemandDir(fullPath, true);
                // For filters like foo\*.cs we need to verify if the directory foo is not denied access.
                // Do a demand on the combined path so that we can fail early in case of deny
                demandPaths[1] = Directory.GetDemandDir(normalizedSearchPath, true);
#if FEATURE_CORECLR
                if (checkHost) 
                {
                    FileSecurityState state1 = new FileSecurityState(FileSecurityStateAccess.PathDiscovery, String.Empty, demandPaths[0]);
                    state1.EnsureState();
                    FileSecurityState state2 = new FileSecurityState(FileSecurityStateAccess.PathDiscovery, String.Empty, demandPaths[1]);
                    state2.EnsureState();
                }
#else
                new FileIOPermission(FileIOPermissionAccess.PathDiscovery, demandPaths, false, false).Demand();
#endif
                searchData = new Directory.SearchData(normalizedSearchPath, userPath, searchOption);
                CommonInit();
            }
            else
            {
                empty = true;
            }
        }

        protected override Iterator<TSource> Clone()
        {
            return new FileSystemEnumerableIterator<TSource>(fullPath, normalizedSearchPath, searchCriteria, userPath, searchOption, _resultHandler, _checkHost);
        }

        [System.Security.SecuritySafeCritical]
        protected override void Dispose(bool disposing)
        {
            try
            {
                if (_hnd != null)
                {
                    _hnd.Dispose();
                }
            }
            finally
            {
                Win32Native.SetErrorMode(oldMode);
                base.Dispose(disposing);
            }
        }

        [System.Security.SecuritySafeCritical]
        public override bool MoveNext()
        {
            Win32Native.WIN32_FIND_DATA data = new Win32Native.WIN32_FIND_DATA();
            switch (state)
            {
                case STATE_INIT:
                    {
                        if (empty)
                        {
                            state = STATE_FINISH;
                            goto case STATE_FINISH;
                        }
                        if (searchData.searchOption == SearchOption.TopDirectoryOnly)
                        {
                            state = STATE_FIND_NEXT_FILE;
                            if (current != null)
                            {
                                return true;
                            }
                            else
                            {
                                goto case STATE_FIND_NEXT_FILE;
                            }
                        }
                        else
                        {
                            state = STATE_SEARCH_NEXT_DIR;
                            goto case STATE_SEARCH_NEXT_DIR;
                        }
                    }
                case STATE_SEARCH_NEXT_DIR:
                    {
                        Contract.Assert(searchData.searchOption != SearchOption.TopDirectoryOnly, "should not reach this code path if searchOption == TopDirectoryOnly");
                        // Traverse directory structure. We need to get '*'
                        while (searchStack.Count > 0)
                        {
                            searchData = searchStack[0];
                            Contract.Assert((searchData.fullPath != null), "fullpath can't be null!");
                            searchStack.RemoveAt(0);

                            // Traverse the subdirs
                            AddSearchableDirsToStack(searchData);

                            // Execute searchCriteria against the current directory
                            String searchPath = Path.InternalCombine(searchData.fullPath, searchCriteria);

                            // Open a Find handle
                            _hnd = Win32Native.FindFirstFile(searchPath, data);
                            if (_hnd.IsInvalid)
                            {
                                int hr = Marshal.GetLastWin32Error();
                                if (hr == Win32Native.ERROR_FILE_NOT_FOUND || hr == Win32Native.ERROR_NO_MORE_FILES || hr == Win32Native.ERROR_PATH_NOT_FOUND)
                                    continue;

                                _hnd.Dispose();
                                HandleError(hr, searchData.fullPath);
                            }

                            state = STATE_FIND_NEXT_FILE;
                            needsParentPathDiscoveryDemand = true;
                            SearchResult searchResult = CreateSearchResult(searchData, data);
                            if (_resultHandler.IsResultIncluded(searchResult))
                            {
                                if (needsParentPathDiscoveryDemand)
                                {
                                    DoDemand(searchData.fullPath);
                                    needsParentPathDiscoveryDemand = false;
                                }
                                current = _resultHandler.CreateObject(searchResult);
                                return true;
                            }
                            else
                            {
                                goto case STATE_FIND_NEXT_FILE;
                            }
                        }
                        state = STATE_FINISH;
                        goto case STATE_FINISH;
                    }
                case STATE_FIND_NEXT_FILE:
                    {
                        if (searchData != null && _hnd != null)
                        {
                            // Keep asking for more matching files/dirs, add it to the list 
                            while (Win32Native.FindNextFile(_hnd, data))
                            {
                                SearchResult searchResult = CreateSearchResult(searchData, data);
                                if (_resultHandler.IsResultIncluded(searchResult))
                                {
                                    if (needsParentPathDiscoveryDemand)
                                    {
                                        DoDemand(searchData.fullPath);
                                        needsParentPathDiscoveryDemand = false;
                                    }
                                    current = _resultHandler.CreateObject(searchResult);
                                    return true;
                                }
                            }

                            // Make sure we quit with a sensible error.
                            int hr = Marshal.GetLastWin32Error();

                            if (_hnd != null)
                                _hnd.Dispose();

                            // ERROR_FILE_NOT_FOUND is valid here because if the top level
                            // dir doen't contain any subdirs and matching files then 
                            // we will get here with this errorcode from the searchStack walk
                            if ((hr != 0) && (hr != Win32Native.ERROR_NO_MORE_FILES)
                                && (hr != Win32Native.ERROR_FILE_NOT_FOUND))
                            {
                                HandleError(hr, searchData.fullPath);
                            }
                        }
                        if (searchData.searchOption == SearchOption.TopDirectoryOnly)
                        {
                            state = STATE_FINISH;
                            goto case STATE_FINISH;
                        }
                        else
                        {
                            state = STATE_SEARCH_NEXT_DIR;
                            goto case STATE_SEARCH_NEXT_DIR;
                        }
                    }
                case STATE_FINISH:
                    {
                        Dispose();
                        break;
                    }
            }
            return false;
        }

        [System.Security.SecurityCritical]
        private SearchResult CreateSearchResult(Directory.SearchData localSearchData, Win32Native.WIN32_FIND_DATA findData)
        {
            String userPathFinal = Path.InternalCombine(localSearchData.userPath, findData.cFileName);
            String fullPathFinal = Path.InternalCombine(localSearchData.fullPath, findData.cFileName);
            return new SearchResult(fullPathFinal, userPathFinal, findData);
        }

        [System.Security.SecurityCritical]
        private void HandleError(int hr, String path)
        {
            Dispose();
            __Error.WinIOError(hr, path);
        }

        [System.Security.SecurityCritical]  // auto-generated
        private void AddSearchableDirsToStack(Directory.SearchData localSearchData)
        {
            Contract.Requires(localSearchData != null);

            String searchPath = Path.InternalCombine(localSearchData.fullPath, "*");
            SafeFindHandle hnd = null;
            Win32Native.WIN32_FIND_DATA data = new Win32Native.WIN32_FIND_DATA();
            try
            {
                // Get all files and dirs
                hnd = Win32Native.FindFirstFile(searchPath, data);

                if (hnd.IsInvalid)
                {
                    int hr = Marshal.GetLastWin32Error();

                    // This could happen if the dir doesn't contain any files.
                    // Continue with the recursive search though, eventually
                    // searchStack will become empty
                    if (hr == Win32Native.ERROR_FILE_NOT_FOUND || hr == Win32Native.ERROR_NO_MORE_FILES || hr == Win32Native.ERROR_PATH_NOT_FOUND)
                        return;

                    HandleError(hr, localSearchData.fullPath);
                }

                // Add subdirs to searchStack. Exempt ReparsePoints as appropriate
                int incr = 0;
                do
                {
                    if (FileSystemEnumerableHelpers.IsDir(data))
                    {
                        String tempFullPath = Path.InternalCombine(localSearchData.fullPath, data.cFileName);
                        String tempUserPath = Path.InternalCombine(localSearchData.userPath, data.cFileName);

                        SearchOption option = localSearchData.searchOption;

#if EXCLUDE_REPARSEPOINTS
                        // Traverse reparse points depending on the searchoption specified
                        if ((searchDataSubDir.searchOption == SearchOption.AllDirectories) && (0 != (data.dwFileAttributes & Win32Native.FILE_ATTRIBUTE_REPARSE_POINT)))
                            option = SearchOption.TopDirectoryOnly; 
#endif
                        // Setup search data for the sub directory and push it into the stack
                        Directory.SearchData searchDataSubDir = new Directory.SearchData(tempFullPath, tempUserPath, option);

                        searchStack.Insert(incr++, searchDataSubDir);
                    }
                } while (Win32Native.FindNextFile(hnd, data));
                // We don't care about errors here
            }
            finally
            {
                if (hnd != null)
                    hnd.Dispose();
            }
        }

        [System.Security.SecurityCritical]
        internal void DoDemand(String fullPathToDemand)
        {
#if FEATURE_CORECLR
            if(_checkHost) {
                String demandDir = Directory.GetDemandDir(fullPathToDemand, true);
                FileSecurityState state = new FileSecurityState(FileSecurityStateAccess.PathDiscovery, String.Empty, demandDir);
                state.EnsureState();
            }
#else
            String demandDir = Directory.GetDemandDir(fullPathToDemand, true);
            String[] demandPaths = new String[] { demandDir };
            new FileIOPermission(FileIOPermissionAccess.PathDiscovery, demandPaths, false, false).Demand();
#endif
        }

        private static String NormalizeSearchPattern(String searchPattern)
        {
            Contract.Requires(searchPattern != null);

            // Win32 normalization trims only U+0020. 
            String tempSearchPattern = searchPattern.TrimEnd(Path.TrimEndChars);

            // Make this corner case more useful, like dir
            if (tempSearchPattern.Equals("."))
            {
                tempSearchPattern = "*";
            }

            Path.CheckSearchPattern(tempSearchPattern);
            return tempSearchPattern;
        }

        private static String GetNormalizedSearchCriteria(String fullSearchString, String fullPathMod)
        {
            Contract.Requires(fullSearchString != null);
            Contract.Requires(fullPathMod != null);
            Contract.Requires(fullSearchString.Length >= fullPathMod.Length);

            String searchCriteria = null;
            char lastChar = fullPathMod[fullPathMod.Length - 1];
            if (Path.IsDirectorySeparator(lastChar))
            {
                // Can happen if the path is C:\temp, in which case GetDirectoryName would return C:\
                searchCriteria = fullSearchString.Substring(fullPathMod.Length);
            }
            else
            {
                Contract.Assert(fullSearchString.Length > fullPathMod.Length);
                searchCriteria = fullSearchString.Substring(fullPathMod.Length + 1);
            }
            return searchCriteria;
        }

        private static String GetFullSearchString(String fullPath, String searchPattern)
        {
            Contract.Requires(fullPath != null);
            Contract.Requires(searchPattern != null);

            String tempStr = Path.InternalCombine(fullPath, searchPattern);

            // If path ends in a trailing slash (\), append a * or we'll get a "Cannot find the file specified" exception
            char lastChar = tempStr[tempStr.Length - 1];
            if (Path.IsDirectorySeparator(lastChar) || lastChar == Path.VolumeSeparatorChar)
            {
                tempStr = tempStr + '*';
            }

            return tempStr;
        }
    }

    internal abstract class SearchResultHandler<TSource>
    {

        [System.Security.SecurityCritical]
        internal abstract bool IsResultIncluded(SearchResult result);

        [System.Security.SecurityCritical]
        internal abstract TSource CreateObject(SearchResult result);

    }

    internal class StringResultHandler : SearchResultHandler<String>
    {
        private bool _includeFiles;
        private bool _includeDirs;

        internal StringResultHandler(bool includeFiles, bool includeDirs)
        {
            _includeFiles = includeFiles;
            _includeDirs = includeDirs;
        }

        [System.Security.SecurityCritical]
        internal override bool IsResultIncluded(SearchResult result)
        {
            bool includeFile = _includeFiles && FileSystemEnumerableHelpers.IsFile(result.FindData);
            bool includeDir = _includeDirs && FileSystemEnumerableHelpers.IsDir(result.FindData);
            Contract.Assert(!(includeFile && includeDir), result.FindData.cFileName + ": current item can't be both file and dir!");
            return (includeFile || includeDir);
        }

        [System.Security.SecurityCritical]
        internal override String CreateObject(SearchResult result)
        {
            return result.UserPath;
        }
    }

    internal class FileInfoResultHandler : SearchResultHandler<FileInfo>
    {
        [System.Security.SecurityCritical]
        internal override bool IsResultIncluded(SearchResult result)
        {
            return FileSystemEnumerableHelpers.IsFile(result.FindData);
        }

        [System.Security.SecurityCritical]
        internal override FileInfo CreateObject(SearchResult result)
        {
            String name = result.FullPath;
#if FEATURE_CORECLR
            FileSecurityState state = new FileSecurityState(FileSecurityStateAccess.Read, String.Empty, name);
            state.EnsureState();
#else
            String[] names = new String[] { name };
            new FileIOPermission(FileIOPermissionAccess.Read, names, false, false).Demand();
#endif
            FileInfo fi = new FileInfo(name, false);
            fi.InitializeFrom(result.FindData);
            return fi;
        }
    }

    internal class DirectoryInfoResultHandler : SearchResultHandler<DirectoryInfo>
    {
        [System.Security.SecurityCritical]
        internal override bool IsResultIncluded(SearchResult result)
        {
            return FileSystemEnumerableHelpers.IsDir(result.FindData);
        }

        [System.Security.SecurityCritical]
        internal override DirectoryInfo CreateObject(SearchResult result)
        {
            String name = result.FullPath;
            String permissionName = name + "\\.";
            
#if FEATURE_CORECLR
            FileSecurityState state = new FileSecurityState(FileSecurityStateAccess.Read, String.Empty, permissionName);
            state.EnsureState();
#else
            String[] permissionNames = new String[] { permissionName };
            new FileIOPermission(FileIOPermissionAccess.Read, permissionNames, false, false).Demand();
#endif
            DirectoryInfo di = new DirectoryInfo(name, false);
            di.InitializeFrom(result.FindData);
            return di;
        }
    }

    internal class FileSystemInfoResultHandler : SearchResultHandler<FileSystemInfo>
    {

        [System.Security.SecurityCritical]
        internal override bool IsResultIncluded(SearchResult result)
        {
            bool includeFile = FileSystemEnumerableHelpers.IsFile(result.FindData);
            bool includeDir = FileSystemEnumerableHelpers.IsDir(result.FindData);
            Contract.Assert(!(includeFile && includeDir), result.FindData.cFileName + ": current item can't be both file and dir!");

            return (includeDir || includeFile);
        }

        [System.Security.SecurityCritical]
        internal override FileSystemInfo CreateObject(SearchResult result)
        {
            bool isFile = FileSystemEnumerableHelpers.IsFile(result.FindData);
            bool isDir = FileSystemEnumerableHelpers.IsDir(result.FindData);

            if (isDir)
            {
                String name = result.FullPath;
                String permissionName = name + "\\.";

#if FEATURE_CORECLR
                FileSecurityState state = new FileSecurityState(FileSecurityStateAccess.Read, String.Empty, permissionName);
                state.EnsureState();
#else
                String[] permissionNames = new String[] { permissionName };
                new FileIOPermission(FileIOPermissionAccess.Read, permissionNames, false, false).Demand();
#endif
                DirectoryInfo di = new DirectoryInfo(name, false);
                di.InitializeFrom(result.FindData);
                return di;
            }
            else
            {
                Contract.Assert(isFile);
                String name = result.FullPath;

#if FEATURE_CORECLR
                FileSecurityState state = new FileSecurityState(FileSecurityStateAccess.Read, String.Empty, name);
                state.EnsureState();
#else
                String[] names = new String[] { name };
                new FileIOPermission(FileIOPermissionAccess.Read, names, false, false).Demand();
#endif
                FileInfo fi = new FileInfo(name, false);
                fi.InitializeFrom(result.FindData);
                return fi;
            }
        }

    }

    internal sealed class SearchResult
    {
        private String fullPath;     // fully-qualifed path
        private String userPath;     // user-specified path
        [System.Security.SecurityCritical]
        private Win32Native.WIN32_FIND_DATA findData;

        [System.Security.SecurityCritical]
        internal SearchResult(String fullPath, String userPath, Win32Native.WIN32_FIND_DATA findData)
        {
            Contract.Requires(fullPath != null);
            Contract.Requires(userPath != null);

            this.fullPath = fullPath;
            this.userPath = userPath;
            this.findData = findData;
        }

        internal String FullPath
        {
            get { return fullPath; }
        }

        internal String UserPath
        {
            get { return userPath; }
        }

        internal Win32Native.WIN32_FIND_DATA FindData
        {
            [System.Security.SecurityCritical]
            get { return findData; }
        }

    }

    internal static class FileSystemEnumerableHelpers
    {
        [System.Security.SecurityCritical]  // auto-generated
        internal static bool IsDir(Win32Native.WIN32_FIND_DATA data)
        {
            // Don't add "." nor ".."
            return (0 != (data.dwFileAttributes & Win32Native.FILE_ATTRIBUTE_DIRECTORY))
                                                && !data.cFileName.Equals(".") && !data.cFileName.Equals("..");
        }

        [System.Security.SecurityCritical]  // auto-generated
        internal static bool IsFile(Win32Native.WIN32_FIND_DATA data)
        {
            return 0 == (data.dwFileAttributes & Win32Native.FILE_ATTRIBUTE_DIRECTORY);
        }

    }
}

