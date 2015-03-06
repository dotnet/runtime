// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security;
using System.Text;
using System.Threading;

using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;

namespace System.IO
{
    // Overview:
    // The key methods instantiate Win32FileSystemEnumerableIterators. These compose the iterator with search result
    // handlers that instantiate the FileInfo, DirectoryInfo, String, etc. The handlers then perform any
    // additional required permission demands. 
    internal static class Win32FileSystemEnumerableFactory
    {
        internal static IEnumerable<String> CreateFileNameIterator(String path, String originalUserPath, String searchPattern,
                                                                    bool includeFiles, bool includeDirs, SearchOption searchOption)
        {
            Contract.Requires(path != null);
            Contract.Requires(originalUserPath != null);
            Contract.Requires(searchPattern != null);

            SearchResultHandler<String> handler = new StringResultHandler(includeFiles, includeDirs);
            return new Win32FileSystemEnumerableIterator<String>(path, originalUserPath, searchPattern, searchOption, handler);
        }

        internal static IEnumerable<FileInfo> CreateFileInfoIterator(String path, String originalUserPath, String searchPattern, SearchOption searchOption)
        {
            Contract.Requires(path != null);
            Contract.Requires(originalUserPath != null);
            Contract.Requires(searchPattern != null);

            SearchResultHandler<FileInfo> handler = new FileInfoResultHandler();
            return new Win32FileSystemEnumerableIterator<FileInfo>(path, originalUserPath, searchPattern, searchOption, handler);
        }

        internal static IEnumerable<DirectoryInfo> CreateDirectoryInfoIterator(String path, String originalUserPath, String searchPattern, SearchOption searchOption)
        {
            Contract.Requires(path != null);
            Contract.Requires(originalUserPath != null);
            Contract.Requires(searchPattern != null);

            SearchResultHandler<DirectoryInfo> handler = new DirectoryInfoResultHandler();
            return new Win32FileSystemEnumerableIterator<DirectoryInfo>(path, originalUserPath, searchPattern, searchOption, handler);
        }

        internal static IEnumerable<FileSystemInfo> CreateFileSystemInfoIterator(String path, String originalUserPath, String searchPattern, SearchOption searchOption)
        {
            Contract.Requires(path != null);
            Contract.Requires(originalUserPath != null);
            Contract.Requires(searchPattern != null);

            SearchResultHandler<FileSystemInfo> handler = new FileSystemInfoResultHandler();
            return new Win32FileSystemEnumerableIterator<FileSystemInfo>(path, originalUserPath, searchPattern, searchOption, handler);
        }
    }

    // Overview:
    // Enumerates file system entries matching the search parameters. For recursive searches this
    // searches through all the sub dirs and executes the search criteria against every dir.
    // 
    // Generic implementation:
    // Win32FileSystemEnumerableIterator is generic. When it gets a WIN32_FIND_DATA, it calls the 
    // result handler to create an instance of the generic type. 
    // 
    // Usage:
    // Use Win32FileSystemEnumerableFactory to obtain FSEnumerables that can enumerate file system 
    // entries as String path names, FileInfos, DirectoryInfos, or FileSystemInfos.
    // 
    // Security:
    // For all the dirs/files returned, demands path discovery permission for their parent folders
    internal class Win32FileSystemEnumerableIterator<TSource> : Iterator<TSource>
    {
        private const int STATE_INIT = 1;
        private const int STATE_SEARCH_NEXT_DIR = 2;
        private const int STATE_FIND_NEXT_FILE = 3;
        private const int STATE_FINISH = 4;

        private SearchResultHandler<TSource> _resultHandler;
        private List<Directory.SearchData> _searchStack;
        private Directory.SearchData _searchData;
        private String _searchCriteria;
        [System.Security.SecurityCritical]
        private SafeFindHandle _hnd = null;

        // empty means we know in advance that we won’t find any search results, which can happen if:
        // 1. we don’t have a search pattern
        // 2. we’re enumerating only the top directory and found no matches during the first call
        // This flag allows us to return early for these cases. We can’t know this in advance for
        // SearchOption.AllDirectories because we do a “*” search for subdirs and then use the
        // searchPattern at each directory level.
        private bool _empty;

        private String _userPath;
        private SearchOption _searchOption;
        private String _fullPath;
        private String _normalizedSearchPath;
        private uint _oldMode;

        [System.Security.SecuritySafeCritical]
        internal Win32FileSystemEnumerableIterator(String path, String originalUserPath, String searchPattern, SearchOption searchOption, SearchResultHandler<TSource> resultHandler)
        {
            Contract.Requires(path != null);
            Contract.Requires(originalUserPath != null);
            Contract.Requires(searchPattern != null);
            Contract.Requires(searchOption == SearchOption.AllDirectories || searchOption == SearchOption.TopDirectoryOnly);
            Contract.Requires(resultHandler != null);

            _oldMode = Interop.mincore.SetErrorMode(Interop.SEM_FAILCRITICALERRORS);

            _searchStack = new List<Directory.SearchData>();

            String normalizedSearchPattern = NormalizeSearchPattern(searchPattern);

            if (normalizedSearchPattern.Length == 0)
            {
                _empty = true;
            }
            else
            {
                _resultHandler = resultHandler;
                this._searchOption = searchOption;

                _fullPath = Path.GetFullPath(path);
                String fullSearchString = GetFullSearchString(_fullPath, normalizedSearchPattern);
                _normalizedSearchPath = Path.GetDirectoryName(fullSearchString);

                // normalize search criteria
                _searchCriteria = GetNormalizedSearchCriteria(fullSearchString, _normalizedSearchPath);

                // fix up user path
                String searchPatternDirName = Path.GetDirectoryName(normalizedSearchPattern);
                String userPathTemp = originalUserPath;
                if (searchPatternDirName != null && searchPatternDirName.Length != 0)
                {
                    userPathTemp = Path.Combine(userPathTemp, searchPatternDirName);
                }
                this._userPath = userPathTemp;

                _searchData = new Directory.SearchData(_normalizedSearchPath, this._userPath, searchOption);

                CommonInit();
            }
        }

        [System.Security.SecurityCritical]
        private void CommonInit()
        {
            Debug.Assert(_searchCriteria != null && _searchData != null, "searchCriteria and searchData should be initialized");

            // Execute searchCriteria against the current directory
            PathHelpers.ThrowIfEmptyOrRootedPath(_searchCriteria);
            String searchPath = Path.Combine(_searchData.fullPath, _searchCriteria);

            Interop.WIN32_FIND_DATA data = new Interop.WIN32_FIND_DATA();

            // Open a Find handle
            _hnd = Interop.mincore.FindFirstFile(searchPath, ref data);

            if (_hnd.IsInvalid)
            {
                int errorCode = Marshal.GetLastWin32Error();
                if (errorCode != Interop.ERROR_FILE_NOT_FOUND && errorCode != Interop.ERROR_NO_MORE_FILES)
                {
                    HandleError(errorCode, _searchData.fullPath);
                }
                else
                {
                    // flag this as empty only if we're searching just top directory
                    // Used in fast path for top directory only
                    _empty = _searchData.searchOption == SearchOption.TopDirectoryOnly;
                }
            }
            // fast path for TopDirectoryOnly. If we have a result, go ahead and set it to 
            // current. If empty, dispose handle.
            if (_searchData.searchOption == SearchOption.TopDirectoryOnly)
            {
                if (_empty)
                {
                    _hnd.Dispose();
                }
                else
                {
                    SearchResult searchResult = CreateSearchResult(_searchData, data);
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
                _searchStack.Add(_searchData);
            }
        }

        [System.Security.SecuritySafeCritical]
        private Win32FileSystemEnumerableIterator(String fullPath, String normalizedSearchPath, String searchCriteria, String userPath, SearchOption searchOption, SearchResultHandler<TSource> resultHandler)
        {
            this._fullPath = fullPath;
            this._normalizedSearchPath = normalizedSearchPath;
            this._searchCriteria = searchCriteria;
            this._resultHandler = resultHandler;
            this._userPath = userPath;
            this._searchOption = searchOption;

            _searchStack = new List<Directory.SearchData>();

            if (searchCriteria != null)
            {
                PathHelpers.CheckInvalidPathChars(fullPath, true);

                _searchData = new Directory.SearchData(normalizedSearchPath, userPath, searchOption);
                CommonInit();
            }
            else
            {
                _empty = true;
            }
        }

        protected override Iterator<TSource> Clone()
        {
            return new Win32FileSystemEnumerableIterator<TSource>(_fullPath, _normalizedSearchPath, _searchCriteria, _userPath, _searchOption, _resultHandler);
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
                Interop.mincore.SetErrorMode(_oldMode);
                base.Dispose(disposing);
            }
        }

        [System.Security.SecuritySafeCritical]
        public override bool MoveNext()
        {
            Interop.WIN32_FIND_DATA data = new Interop.WIN32_FIND_DATA();
            switch (state)
            {
                case STATE_INIT:
                    {
                        if (_empty)
                        {
                            state = STATE_FINISH;
                            goto case STATE_FINISH;
                        }
                        if (_searchData.searchOption == SearchOption.TopDirectoryOnly)
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
                        Debug.Assert(_searchData.searchOption != SearchOption.TopDirectoryOnly, "should not reach this code path if searchOption == TopDirectoryOnly");
                        // Traverse directory structure. We need to get '*'
                        while (_searchStack.Count > 0)
                        {
                            _searchData = _searchStack[0];
                            Debug.Assert((_searchData.fullPath != null), "fullpath can't be null!");
                            _searchStack.RemoveAt(0);

                            // Traverse the subdirs
                            AddSearchableDirsToStack(_searchData);

                            // Execute searchCriteria against the current directory
                            String searchPath = Path.Combine(_searchData.fullPath, _searchCriteria);

                            // Open a Find handle
                            _hnd = Interop.mincore.FindFirstFile(searchPath, ref data);
                            if (_hnd.IsInvalid)
                            {
                                int errorCode = Marshal.GetLastWin32Error();
                                if (errorCode == Interop.ERROR_FILE_NOT_FOUND || errorCode == Interop.ERROR_NO_MORE_FILES || errorCode == Interop.ERROR_PATH_NOT_FOUND)
                                    continue;

                                _hnd.Dispose();
                                HandleError(errorCode, _searchData.fullPath);
                            }

                            state = STATE_FIND_NEXT_FILE;

                            SearchResult searchResult = CreateSearchResult(_searchData, data);
                            if (_resultHandler.IsResultIncluded(searchResult))
                            {
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
                        if (_searchData != null && _hnd != null)
                        {
                            // Keep asking for more matching files/dirs, add it to the list 
                            while (Interop.mincore.FindNextFile(_hnd, ref data))
                            {
                                SearchResult searchResult = CreateSearchResult(_searchData, data);
                                if (_resultHandler.IsResultIncluded(searchResult))
                                {
                                    current = _resultHandler.CreateObject(searchResult);
                                    return true;
                                }
                            }

                            // Make sure we quit with a sensible error.
                            int errorCode = Marshal.GetLastWin32Error();

                            if (_hnd != null)
                                _hnd.Dispose();

                            // ERROR_FILE_NOT_FOUND is valid here because if the top level
                            // dir doesn't contain any subdirs and matching files then 
                            // we will get here with this errorcode from the searchStack walk
                            if ((errorCode != 0) && (errorCode != Interop.ERROR_NO_MORE_FILES)
                                && (errorCode != Interop.ERROR_FILE_NOT_FOUND))
                            {
                                HandleError(errorCode, _searchData.fullPath);
                            }
                        }
                        if (_searchData.searchOption == SearchOption.TopDirectoryOnly)
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
        private SearchResult CreateSearchResult(Directory.SearchData localSearchData, Interop.WIN32_FIND_DATA findData)
        {
            string findData_fileName = findData.cFileName;
            Contract.Requires(findData_fileName.Length != 0 && !Path.IsPathRooted(findData_fileName),
                "Expected file system enumeration to not have empty file/directory name and not have rooted name");

            String userPathFinal = Path.Combine(localSearchData.userPath, findData_fileName);
            String fullPathFinal = Path.Combine(localSearchData.fullPath, findData_fileName);
            return new SearchResult(fullPathFinal, userPathFinal, findData);
        }

        [System.Security.SecurityCritical]
        private void HandleError(int errorCode, String path)
        {
            Dispose();
            throw Win32Marshal.GetExceptionForWin32Error(errorCode, path);
        }

        [System.Security.SecurityCritical]  // auto-generated
        private void AddSearchableDirsToStack(Directory.SearchData localSearchData)
        {
            Contract.Requires(localSearchData != null);

            String searchPath = Path.Combine(localSearchData.fullPath, "*");
            SafeFindHandle hnd = null;
            Interop.WIN32_FIND_DATA data = new Interop.WIN32_FIND_DATA();
            try
            {
                // Get all files and dirs
                hnd = Interop.mincore.FindFirstFile(searchPath, ref data);

                if (hnd.IsInvalid)
                {
                    int errorCode = Marshal.GetLastWin32Error();

                    // This could happen if the dir doesn't contain any files.
                    // Continue with the recursive search though, eventually
                    // searchStack will become empty
                    if (errorCode == Interop.ERROR_FILE_NOT_FOUND || errorCode == Interop.ERROR_NO_MORE_FILES || errorCode == Interop.ERROR_PATH_NOT_FOUND)
                        return;

                    HandleError(errorCode, localSearchData.fullPath);
                }

                // Add subdirs to searchStack. Exempt ReparsePoints as appropriate
                int incr = 0;
                do
                {
                    if (Win32FileSystemEnumerableHelpers.IsDir(data))
                    {
                        Debug.Assert(data.cFileName.Length != 0 && !Path.IsPathRooted(data.cFileName),
                            "Expected file system enumeration to not have empty file/directory name and not have rooted name");

                        String tempFullPath = Path.Combine(localSearchData.fullPath, data.cFileName);
                        String tempUserPath = Path.Combine(localSearchData.userPath, data.cFileName);

                        SearchOption option = localSearchData.searchOption;

                        // Setup search data for the sub directory and push it into the stack
                        Directory.SearchData searchDataSubDir = new Directory.SearchData(tempFullPath, tempUserPath, option);

                        _searchStack.Insert(incr++, searchDataSubDir);
                    }
                } while (Interop.mincore.FindNextFile(hnd, ref data));
                // We don't care about errors here
            }
            finally
            {
                if (hnd != null)
                    hnd.Dispose();
            }
        }

        private static String NormalizeSearchPattern(String searchPattern)
        {
            Contract.Requires(searchPattern != null);

            // Win32 normalization trims only U+0020. 
            String tempSearchPattern = searchPattern.TrimEnd(PathHelpers.TrimEndChars);

            // Make this corner case more useful, like dir
            if (tempSearchPattern.Equals("."))
            {
                tempSearchPattern = "*";
            }

            PathHelpers.CheckSearchPattern(tempSearchPattern);
            return tempSearchPattern;
        }

        private static String GetNormalizedSearchCriteria(String fullSearchString, String fullPathMod)
        {
            Contract.Requires(fullSearchString != null);
            Contract.Requires(fullPathMod != null);
            Contract.Requires(fullSearchString.Length >= fullPathMod.Length);

            String searchCriteria = null;
            char lastChar = fullPathMod[fullPathMod.Length - 1];
            if (PathHelpers.IsDirectorySeparator(lastChar))
            {
                // Can happen if the path is C:\temp, in which case GetDirectoryName would return C:\
                searchCriteria = fullSearchString.Substring(fullPathMod.Length);
            }
            else
            {
                Debug.Assert(fullSearchString.Length > fullPathMod.Length);
                searchCriteria = fullSearchString.Substring(fullPathMod.Length + 1);
            }
            return searchCriteria;
        }

        private static String GetFullSearchString(String fullPath, String searchPattern)
        {
            Contract.Requires(fullPath != null);
            Contract.Requires(searchPattern != null);

            PathHelpers.ThrowIfEmptyOrRootedPath(searchPattern);
            String tempStr = Path.Combine(fullPath, searchPattern);

            // If path ends in a trailing slash (\), append a * or we'll get a "Cannot find the file specified" exception
            char lastChar = tempStr[tempStr.Length - 1];
            if (PathHelpers.IsDirectorySeparator(lastChar) || lastChar == Path.VolumeSeparatorChar)
            {
                tempStr = tempStr + "*";
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
            bool includeFile = _includeFiles && Win32FileSystemEnumerableHelpers.IsFile(result.FindData);
            bool includeDir = _includeDirs && Win32FileSystemEnumerableHelpers.IsDir(result.FindData);
            Debug.Assert(!(includeFile && includeDir), result.FindData.cFileName + ": current item can't be both file and dir!");
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
            return Win32FileSystemEnumerableHelpers.IsFile(result.FindData);
        }

        [System.Security.SecurityCritical]
        internal override FileInfo CreateObject(SearchResult result)
        {
            String name = result.FullPath;
            IFileSystemObject fileSystemObject = new Win32FileSystem.Win32FileSystemObject(name, result.FindData, asDirectory: false);
            FileInfo fi = new FileInfo(name, fileSystemObject);
            return fi;
        }
    }

    internal class DirectoryInfoResultHandler : SearchResultHandler<DirectoryInfo>
    {
        [System.Security.SecurityCritical]
        internal override bool IsResultIncluded(SearchResult result)
        {
            return Win32FileSystemEnumerableHelpers.IsDir(result.FindData);
        }

        [System.Security.SecurityCritical]
        internal override DirectoryInfo CreateObject(SearchResult result)
        {
            String name = result.FullPath;
            IFileSystemObject fileSystemObject = new Win32FileSystem.Win32FileSystemObject(name, result.FindData, asDirectory: true);
            DirectoryInfo di = new DirectoryInfo(name, fileSystemObject);
            return di;
        }
    }

    internal class FileSystemInfoResultHandler : SearchResultHandler<FileSystemInfo>
    {
        [System.Security.SecurityCritical]
        internal override bool IsResultIncluded(SearchResult result)
        {
            bool includeFile = Win32FileSystemEnumerableHelpers.IsFile(result.FindData);
            bool includeDir = Win32FileSystemEnumerableHelpers.IsDir(result.FindData);
            Debug.Assert(!(includeFile && includeDir), result.FindData.cFileName + ": current item can't be both file and dir!");

            return (includeDir || includeFile);
        }

        [System.Security.SecurityCritical]
        internal override FileSystemInfo CreateObject(SearchResult result)
        {
            bool isFile = Win32FileSystemEnumerableHelpers.IsFile(result.FindData);
            bool isDir = Win32FileSystemEnumerableHelpers.IsDir(result.FindData);
            String name = result.FullPath;

            if (isDir)
            {
                IFileSystemObject fileSystemObject = new Win32FileSystem.Win32FileSystemObject(name, result.FindData, asDirectory: true);
                DirectoryInfo di = new DirectoryInfo(name, fileSystemObject);
                return di;
            }
            else
            {
                Debug.Assert(isFile);
                IFileSystemObject fileSystemObject = new Win32FileSystem.Win32FileSystemObject(name, result.FindData, asDirectory: false);
                FileInfo fi = new FileInfo(name, fileSystemObject);
                return fi;
            }
        }
    }

    internal sealed class SearchResult
    {
        private String _fullPath;     // fully-qualified path
        private String _userPath;     // user-specified path
        [System.Security.SecurityCritical]
        private Interop.WIN32_FIND_DATA _findData;

        [System.Security.SecurityCritical]
        internal SearchResult(String fullPath, String userPath, Interop.WIN32_FIND_DATA findData)
        {
            Contract.Requires(fullPath != null);
            Contract.Requires(userPath != null);

            this._fullPath = fullPath;
            this._userPath = userPath;
            this._findData = findData;
        }

        internal String FullPath
        {
            get { return _fullPath; }
        }

        internal String UserPath
        {
            get { return _userPath; }
        }

        internal Interop.WIN32_FIND_DATA FindData
        {
            [System.Security.SecurityCritical]
            get
            { return _findData; }
        }
    }

    internal static class Win32FileSystemEnumerableHelpers
    {
        [System.Security.SecurityCritical]  // auto-generated
        internal static bool IsDir(Interop.WIN32_FIND_DATA data)
        {
            // Don't add "." nor ".."
            return (0 != (data.dwFileAttributes & Interop.FILE_ATTRIBUTE_DIRECTORY))
                                                && !data.cFileName.Equals(".") && !data.cFileName.Equals("..");
        }

        [System.Security.SecurityCritical]  // auto-generated
        internal static bool IsFile(Interop.WIN32_FIND_DATA data)
        {
            return 0 == (data.dwFileAttributes & Interop.FILE_ATTRIBUTE_DIRECTORY);
        }
    }
}

