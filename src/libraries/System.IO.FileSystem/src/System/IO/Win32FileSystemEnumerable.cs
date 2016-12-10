// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security;

using Microsoft.Win32.SafeHandles;

namespace System.IO
{
    // Overview:
    // The key methods instantiate Win32FileSystemEnumerableIterators. These compose the iterator with search result
    // handlers that instantiate the FileInfo, DirectoryInfo, string, etc. The handlers then perform any
    // additional required permission demands.
    internal static class Win32FileSystemEnumerableFactory
    {
        internal static IEnumerable<string> CreateFileNameIterator(string path, string originalUserPath, string searchPattern,
                                                                    bool includeFiles, bool includeDirs, SearchOption searchOption)
        {
            Debug.Assert(path != null);
            Debug.Assert(originalUserPath != null);
            Debug.Assert(searchPattern != null);

            SearchResultHandler<string> handler;

            if (includeFiles && includeDirs)
            {
                handler = SearchResultHandler.FileSystemPath;
            }
            else if (includeFiles)
            {
                handler = SearchResultHandler.FilePath;
            }
            else
            {
                Debug.Assert(includeDirs, "Should never be excluding both files and directories.");
                handler = SearchResultHandler.DirectoryPath;
            }

            return new Win32FileSystemEnumerableIterator<string>(path, originalUserPath, searchPattern, searchOption, handler);
        }

        internal static IEnumerable<FileInfo> CreateFileInfoIterator(string path, string originalUserPath, string searchPattern, SearchOption searchOption)
        {
            Debug.Assert(path != null);
            Debug.Assert(originalUserPath != null);
            Debug.Assert(searchPattern != null);

            return new Win32FileSystemEnumerableIterator<FileInfo>(path, originalUserPath, searchPattern, searchOption, SearchResultHandler.FileInfo);
        }

        internal static IEnumerable<DirectoryInfo> CreateDirectoryInfoIterator(string path, string originalUserPath, string searchPattern, SearchOption searchOption)
        {
            Debug.Assert(path != null);
            Debug.Assert(originalUserPath != null);
            Debug.Assert(searchPattern != null);

            return new Win32FileSystemEnumerableIterator<DirectoryInfo>(path, originalUserPath, searchPattern, searchOption, SearchResultHandler.DirectoryInfo);
        }

        internal static IEnumerable<FileSystemInfo> CreateFileSystemInfoIterator(string path, string originalUserPath, string searchPattern, SearchOption searchOption)
        {
            Debug.Assert(path != null);
            Debug.Assert(originalUserPath != null);
            Debug.Assert(searchPattern != null);

            return new Win32FileSystemEnumerableIterator<FileSystemInfo>(path, originalUserPath, searchPattern, searchOption, SearchResultHandler.FileSystemInfo);
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
    // entries as string path names, FileInfos, DirectoryInfos, or FileSystemInfos.
    //
    // Security:
    // For all the dirs/files returned, demands path discovery permission for their parent folders
    internal class Win32FileSystemEnumerableIterator<TSource> : Iterator<TSource>
    {
        private const int STATE_INIT = 1;
        private const int STATE_SEARCH_NEXT_DIR = 2;
        private const int STATE_FIND_NEXT_FILE = 3;
        private const int STATE_FINISH = 4;

        private readonly SearchResultHandler<TSource> _resultHandler;
        private List<PathPair> _searchList;
        private PathPair _searchData;
        private readonly string _searchCriteria;
        [SecurityCritical]
        private SafeFindHandle _hnd = null;

        // empty means we know in advance that we won?t find any search results, which can happen if:
        // 1. we don?t have a search pattern
        // 2. we?re enumerating only the top directory and found no matches during the first call
        // This flag allows us to return early for these cases. We can?t know this in advance for
        // SearchOption.AllDirectories because we do a ?*? search for subdirs and then use the
        // searchPattern at each directory level.
        private bool _empty;

        private readonly string _userPath;
        private readonly SearchOption _searchOption;
        private readonly string _fullPath;
        private readonly string _normalizedSearchPath;
        private readonly uint _oldMode;

        [SecuritySafeCritical]
        internal Win32FileSystemEnumerableIterator(string path, string originalUserPath, string searchPattern, SearchOption searchOption, SearchResultHandler<TSource> resultHandler)
        {
            Debug.Assert(path != null);
            Debug.Assert(originalUserPath != null);
            Debug.Assert(searchPattern != null);
            Debug.Assert(searchOption == SearchOption.AllDirectories || searchOption == SearchOption.TopDirectoryOnly);
            Debug.Assert(resultHandler != null);

            _oldMode = Interop.Kernel32.SetErrorMode(Interop.Kernel32.SEM_FAILCRITICALERRORS);

            string normalizedSearchPattern = PathHelpers.NormalizeSearchPattern(searchPattern);

            if (normalizedSearchPattern.Length == 0)
            {
                _empty = true;
            }
            else
            {
                _resultHandler = resultHandler;
                _searchOption = searchOption;

                _fullPath = Path.GetFullPath(path);
                string fullSearchString = PathHelpers.GetFullSearchString(_fullPath, normalizedSearchPattern);
                _normalizedSearchPath = Path.GetDirectoryName(fullSearchString);

                // normalize search criteria
                _searchCriteria = GetNormalizedSearchCriteria(fullSearchString, _normalizedSearchPath);

                // fix up user path
                string searchPatternDirName = Path.GetDirectoryName(normalizedSearchPattern);
                _userPath = string.IsNullOrEmpty(searchPatternDirName) ?
                    originalUserPath :
                    Path.Combine(originalUserPath, searchPatternDirName);

                _searchData = new PathPair(_userPath, _normalizedSearchPath);

                CommonInit();
            }
        }

        [SecurityCritical]
        private void CommonInit()
        {
            Debug.Assert(_searchCriteria != null, "searchCriteria should be initialized");

            // Execute searchCriteria against the current directory
            PathHelpers.ThrowIfEmptyOrRootedPath(_searchCriteria);
            string searchPath = Path.Combine(_searchData.FullPath, _searchCriteria);

            Interop.Kernel32.WIN32_FIND_DATA data = new Interop.Kernel32.WIN32_FIND_DATA();

            // Open a Find handle
            _hnd = Interop.Kernel32.FindFirstFile(searchPath, ref data);

            if (_hnd.IsInvalid)
            {
                int errorCode = Marshal.GetLastWin32Error();
                if (errorCode != Interop.Errors.ERROR_FILE_NOT_FOUND && errorCode != Interop.Errors.ERROR_NO_MORE_FILES)
                {
                    HandleError(errorCode, _searchData.FullPath);
                }
                else
                {
                    // flag this as empty only if we're searching just top directory
                    // Used in fast path for top directory only
                    _empty = _searchOption == SearchOption.TopDirectoryOnly;
                }
            }
            // fast path for TopDirectoryOnly. If we have a result, go ahead and set it to
            // current. If empty, dispose handle.
            if (_searchOption == SearchOption.TopDirectoryOnly)
            {
                if (_empty)
                {
                    _hnd.Dispose();
                }
                else
                {
                    TSource result;
                    if (IsResultIncluded(ref data, out result))
                    {
                        current = result;
                    }
                }
            }
            // for AllDirectories, we first recurse into dirs, so cleanup and add searchData
            // to the list
            else
            {
                _hnd.Dispose();
                _searchList = new List<PathPair>();
                _searchList.Add(_searchData);
            }
        }

        [SecuritySafeCritical]
        private Win32FileSystemEnumerableIterator(string fullPath, string normalizedSearchPath, string searchCriteria, string userPath, SearchOption searchOption, SearchResultHandler<TSource> resultHandler)
        {
            _fullPath = fullPath;
            _normalizedSearchPath = normalizedSearchPath;
            _searchCriteria = searchCriteria;
            _resultHandler = resultHandler;
            _userPath = userPath;
            _searchOption = searchOption;

            if (searchCriteria != null)
            {
                PathInternal.CheckInvalidPathChars(fullPath);
                if (PathInternal.HasWildCardCharacters(fullPath))
                    throw new ArgumentException(SR.Argument_InvalidPathChars, nameof(fullPath));

                _searchData = new PathPair(userPath, normalizedSearchPath);
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

        [SecuritySafeCritical]
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
                Interop.Kernel32.SetErrorMode(_oldMode);
                base.Dispose(disposing);
            }
        }

        [SecuritySafeCritical]
        public override bool MoveNext()
        {
            Interop.Kernel32.WIN32_FIND_DATA data = new Interop.Kernel32.WIN32_FIND_DATA();
            switch (state)
            {
                case STATE_INIT:
                    {
                        if (_empty)
                        {
                            state = STATE_FINISH;
                            goto case STATE_FINISH;
                        }
                        if (_searchOption == SearchOption.TopDirectoryOnly)
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
                        Debug.Assert(_searchOption != SearchOption.TopDirectoryOnly, "should not reach this code path if searchOption == TopDirectoryOnly");
                        Debug.Assert(_searchList != null, "_searchList should not be null");
                        // Traverse directory structure. We need to get '*'
                        while (_searchList.Count > 0)
                        {
                            int index = _searchList.Count - 1;
                            _searchData = _searchList[index];
                            Debug.Assert((_searchData.FullPath != null), "fullpath can't be null!");
                            _searchList.RemoveAt(index);

                            // Traverse the subdirs
                            AddSearchableDirsToList(_searchData);

                            // Execute searchCriteria against the current directory
                            string searchPath = Path.Combine(_searchData.FullPath, _searchCriteria);

                            // Open a Find handle
                            _hnd = Interop.Kernel32.FindFirstFile(searchPath, ref data);
                            if (_hnd.IsInvalid)
                            {
                                int errorCode = Marshal.GetLastWin32Error();
                                if (errorCode == Interop.Errors.ERROR_FILE_NOT_FOUND || errorCode == Interop.Errors.ERROR_NO_MORE_FILES || errorCode == Interop.Errors.ERROR_PATH_NOT_FOUND)
                                    continue;

                                _hnd.Dispose();
                                HandleError(errorCode, _searchData.FullPath);
                            }

                            state = STATE_FIND_NEXT_FILE;

                            TSource result;
                            if (IsResultIncluded(ref data, out result))
                            {
                                current = result;
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
                        if (_hnd != null)
                        {
                            // Keep asking for more matching files/dirs, add it to the list
                            while (Interop.Kernel32.FindNextFile(_hnd, ref data))
                            {
                                TSource result;
                                if (IsResultIncluded(ref data, out result))
                                {
                                    current = result;
                                    return true;
                                }
                            }

                            // Make sure we quit with a sensible error.
                            int errorCode = Marshal.GetLastWin32Error();

                            if (_hnd != null)
                                _hnd.Dispose();

                            // ERROR_FILE_NOT_FOUND is valid here because if the top level
                            // dir doesn't contain any subdirs and matching files then
                            // we will get here with this errorcode from the _searchList walk
                            if ((errorCode != 0) && (errorCode != Interop.Errors.ERROR_NO_MORE_FILES)
                                && (errorCode != Interop.Errors.ERROR_FILE_NOT_FOUND))
                            {
                                HandleError(errorCode, _searchData.FullPath);
                            }
                        }
                        if (_searchOption == SearchOption.TopDirectoryOnly)
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

        [SecurityCritical]
        private bool IsResultIncluded(ref Interop.Kernel32.WIN32_FIND_DATA findData, out TSource result)
        {
            Debug.Assert(findData.cFileName.Length != 0 && !Path.IsPathRooted(findData.cFileName),
                "Expected file system enumeration to not have empty file/directory name and not have rooted name");

            return _resultHandler.IsResultIncluded(_searchData.FullPath, _searchData.UserPath, ref findData, out result);
        }

        [SecurityCritical]
        private void HandleError(int errorCode, string path)
        {
            Dispose();
            throw Win32Marshal.GetExceptionForWin32Error(errorCode, path);
        }

        [SecurityCritical]  // auto-generated
        private void AddSearchableDirsToList(PathPair localSearchData)
        {
            string searchPath = Path.Combine(localSearchData.FullPath, "*");
            SafeFindHandle hnd = null;
            Interop.Kernel32.WIN32_FIND_DATA data = new Interop.Kernel32.WIN32_FIND_DATA();
            try
            {
                // Get all files and dirs
                hnd = Interop.Kernel32.FindFirstFile(searchPath, ref data);

                if (hnd.IsInvalid)
                {
                    int errorCode = Marshal.GetLastWin32Error();

                    // This could happen if the dir doesn't contain any files.
                    // Continue with the recursive search though, eventually
                    // _searchList will become empty
                    if (errorCode == Interop.Errors.ERROR_FILE_NOT_FOUND || errorCode == Interop.Errors.ERROR_NO_MORE_FILES || errorCode == Interop.Errors.ERROR_PATH_NOT_FOUND)
                        return;

                    HandleError(errorCode, localSearchData.FullPath);
                }

                // Add subdirs to _searchList. Exempt ReparsePoints as appropriate
                Debug.Assert(_searchList != null, "_searchList should not be null");
                int initialCount = _searchList.Count;
                do
                {
                    if (Win32FileSystemEnumerableHelpers.IsDir(ref data))
                    {
                        Debug.Assert(data.cFileName.Length != 0 && !Path.IsPathRooted(data.cFileName),
                            "Expected file system enumeration to not have empty file/directory name and not have rooted name");

                        string tempFullPath = Path.Combine(localSearchData.FullPath, data.cFileName);
                        string tempUserPath = Path.Combine(localSearchData.UserPath, data.cFileName);

                        // Setup search data for the sub directory and push it into the list
                        PathPair searchDataSubDir = new PathPair(tempUserPath, tempFullPath);

                        _searchList.Add(searchDataSubDir);
                    }
                } while (Interop.Kernel32.FindNextFile(hnd, ref data));

                // Reverse the items just added to maintain FIFO order
                if (_searchList.Count > initialCount)
                {
                    _searchList.Reverse(initialCount, _searchList.Count - initialCount);
                }

                // We don't care about errors here
            }
            finally
            {
                if (hnd != null)
                    hnd.Dispose();
            }
        }

        private static string GetNormalizedSearchCriteria(string fullSearchString, string fullPathMod)
        {
            Debug.Assert(fullSearchString != null);
            Debug.Assert(fullPathMod != null);
            Debug.Assert(fullSearchString.Length >= fullPathMod.Length);

            string searchCriteria = null;
            char lastChar = fullPathMod[fullPathMod.Length - 1];
            if (PathInternal.IsDirectorySeparator(lastChar))
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
    }

    internal abstract class SearchResultHandler<TSource>
    {
        /// <summary>
        /// Returns true if the result should be included. If true, the <paramref name="result"/> parameter
        /// is set to the created result object, otherwise it is set to null.
        /// </summary>
        [SecurityCritical]
        internal abstract bool IsResultIncluded(string fullPath, string userPath, ref Interop.Kernel32.WIN32_FIND_DATA findData, out TSource result);
    }

    internal static class SearchResultHandler
    {
        private static SearchResultHandler<string> s_filePath;
        private static SearchResultHandler<string> s_directoryPath;
        private static SearchResultHandler<string> s_fileSystemPath;
        private static SearchResultHandler<FileInfo> s_fileInfo;
        private static SearchResultHandler<DirectoryInfo> s_directoryInfo;
        private static SearchResultHandler<FileSystemInfo> s_fileSystemInfo;

        internal static SearchResultHandler<string> FilePath
        {
            get { return s_filePath ?? (s_filePath = new StringResultHandler(includeFiles: true, includeDirs: false)); }
        }

        internal static SearchResultHandler<string> DirectoryPath
        {
            get { return s_directoryPath ?? (s_directoryPath = new StringResultHandler(includeFiles: false, includeDirs: true)); }
        }

        internal static SearchResultHandler<string> FileSystemPath
        {
            get { return s_fileSystemPath ?? (s_fileSystemPath = new StringResultHandler(includeFiles: true, includeDirs: true)); }
        }

        internal static SearchResultHandler<FileInfo> FileInfo
        {
            get { return s_fileInfo ?? (s_fileInfo = new FileInfoResultHandler()); }
        }

        internal static SearchResultHandler<DirectoryInfo> DirectoryInfo
        {
            get { return s_directoryInfo ?? (s_directoryInfo = new DirectoryInfoResultHandler()); }
        }

        internal static SearchResultHandler<FileSystemInfo> FileSystemInfo
        {
            get { return s_fileSystemInfo ?? (s_fileSystemInfo = new FileSystemInfoResultHandler()); }
        }

        private sealed class StringResultHandler : SearchResultHandler<string>
        {
            private readonly bool _includeFiles;
            private readonly bool _includeDirs;

            internal StringResultHandler(bool includeFiles, bool includeDirs)
            {
                _includeFiles = includeFiles;
                _includeDirs = includeDirs;
            }

            [SecurityCritical]
            internal override bool IsResultIncluded(string fullPath, string userPath, ref Interop.Kernel32.WIN32_FIND_DATA findData, out string result)
            {
                if ((_includeFiles && Win32FileSystemEnumerableHelpers.IsFile(ref findData)) ||
                    (_includeDirs && Win32FileSystemEnumerableHelpers.IsDir(ref findData)))
                {
                    result = Path.Combine(userPath, findData.cFileName);
                    return true;
                }

                result = null;
                return false;
            }
        }

        private sealed class FileInfoResultHandler : SearchResultHandler<FileInfo>
        {
            [SecurityCritical]
            internal override bool IsResultIncluded(string fullPath, string userPath, ref Interop.Kernel32.WIN32_FIND_DATA findData, out FileInfo result)
            {
                if (Win32FileSystemEnumerableHelpers.IsFile(ref findData))
                {
                    string fullPathFinal = Path.Combine(fullPath, findData.cFileName);
                    result = new FileInfo(fullPathFinal, ref findData);
                    return true;
                }

                result = null;
                return false;
            }
        }

        private sealed class DirectoryInfoResultHandler : SearchResultHandler<DirectoryInfo>
        {
            [SecurityCritical]
            internal override bool IsResultIncluded(string fullPath, string userPath, ref Interop.Kernel32.WIN32_FIND_DATA findData, out DirectoryInfo result)
            {
                if (Win32FileSystemEnumerableHelpers.IsDir(ref findData))
                {
                    string fullPathFinal = Path.Combine(fullPath, findData.cFileName);
                    result = new DirectoryInfo(fullPathFinal, ref findData);
                    return true;
                }

                result = null;
                return false;
            }
        }

        private sealed class FileSystemInfoResultHandler : SearchResultHandler<FileSystemInfo>
        {
            [SecurityCritical]
            internal override bool IsResultIncluded(string fullPath, string userPath, ref Interop.Kernel32.WIN32_FIND_DATA findData, out FileSystemInfo result)
            {
                if (Win32FileSystemEnumerableHelpers.IsFile(ref findData))
                {
                    string fullPathFinal = Path.Combine(fullPath, findData.cFileName);
                    result = new FileInfo(fullPathFinal, ref findData);
                    return true;
                }
                else if (Win32FileSystemEnumerableHelpers.IsDir(ref findData))
                {
                    string fullPathFinal = Path.Combine(fullPath, findData.cFileName);
                    result = new DirectoryInfo(fullPathFinal, ref findData);
                    return true;
                }

                result = null;
                return false;
            }
        }
    }

    internal static class Win32FileSystemEnumerableHelpers
    {
        [SecurityCritical]  // auto-generated
        internal static bool IsDir(ref Interop.Kernel32.WIN32_FIND_DATA data)
        {
            // Don't add "." nor ".."
            return (0 != (data.dwFileAttributes & Interop.Kernel32.FileAttributes.FILE_ATTRIBUTE_DIRECTORY))
                                                && !data.cFileName.Equals(".") && !data.cFileName.Equals("..");
        }

        [SecurityCritical]  // auto-generated
        internal static bool IsFile(ref Interop.Kernel32.WIN32_FIND_DATA data)
        {
            return 0 == (data.dwFileAttributes & Interop.Kernel32.FileAttributes.FILE_ATTRIBUTE_DIRECTORY);
        }
    }
}
