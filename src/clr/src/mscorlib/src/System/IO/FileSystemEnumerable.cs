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

using System.Collections;
using System.Collections.Generic;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;
using System.Globalization;
using System.Runtime.Versioning;
using System.Diagnostics;
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
        SafeFindHandle _hnd = null;

        // empty means we know in advance that we won't find any search results, which can happen if:
        // 1. we don't have a search pattern
        // 2. we're enumerating only the top directory and found no matches during the first call
        // This flag allows us to return early for these cases. We can't know this in advance for
        // SearchOption.AllDirectories because we do a "*" search for subdirs and then use the
        // searchPattern at each directory level.
        bool empty;

        private String userPath;
        private SearchOption searchOption;
        private String fullPath;
        private String normalizedSearchPath;
        private int oldMode;

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

                fullPath = Path.GetFullPath(path);
                String fullSearchString = GetFullSearchString(fullPath, normalizedSearchPattern);
                normalizedSearchPath = Path.GetDirectoryName(fullSearchString);

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

        private void CommonInit()
        {
            Debug.Assert(searchCriteria != null && searchData != null, "searchCriteria and searchData should be initialized");

            // Execute searchCriteria against the current directory
            String searchPath = Path.Combine(searchData.fullPath, searchCriteria);

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

        private FileSystemEnumerableIterator(String fullPath, String normalizedSearchPath, String searchCriteria, String userPath, SearchOption searchOption, SearchResultHandler<TSource> resultHandler)
        {
            this.fullPath = fullPath;
            this.normalizedSearchPath = normalizedSearchPath;
            this.searchCriteria = searchCriteria;
            this._resultHandler = resultHandler;
            this.userPath = userPath;
            this.searchOption = searchOption;

            searchStack = new List<Directory.SearchData>();

            if (searchCriteria != null)
            {
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
            return new FileSystemEnumerableIterator<TSource>(fullPath, normalizedSearchPath, searchCriteria, userPath, searchOption, _resultHandler);
        }

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
                        Debug.Assert(searchData.searchOption != SearchOption.TopDirectoryOnly, "should not reach this code path if searchOption == TopDirectoryOnly");
                        // Traverse directory structure. We need to get '*'
                        while (searchStack.Count > 0)
                        {
                            searchData = searchStack[0];
                            Debug.Assert((searchData.fullPath != null), "fullpath can't be null!");
                            searchStack.RemoveAt(0);

                            // Traverse the subdirs
                            AddSearchableDirsToStack(searchData);

                            // Execute searchCriteria against the current directory
                            String searchPath = Path.Combine(searchData.fullPath, searchCriteria);

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
                            SearchResult searchResult = CreateSearchResult(searchData, data);
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
                        if (searchData != null && _hnd != null)
                        {
                            // Keep asking for more matching files/dirs, add it to the list 
                            while (Win32Native.FindNextFile(_hnd, data))
                            {
                                SearchResult searchResult = CreateSearchResult(searchData, data);
                                if (_resultHandler.IsResultIncluded(searchResult))
                                {
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

        private SearchResult CreateSearchResult(Directory.SearchData localSearchData, Win32Native.WIN32_FIND_DATA findData)
        {
            String userPathFinal = Path.Combine(localSearchData.userPath, findData.cFileName);
            String fullPathFinal = Path.Combine(localSearchData.fullPath, findData.cFileName);
            return new SearchResult(fullPathFinal, userPathFinal, findData);
        }

        private void HandleError(int hr, String path)
        {
            Dispose();
            __Error.WinIOError(hr, path);
        }

        private void AddSearchableDirsToStack(Directory.SearchData localSearchData)
        {
            Contract.Requires(localSearchData != null);

            String searchPath = Path.Combine(localSearchData.fullPath, "*");
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
                        String tempFullPath = Path.Combine(localSearchData.fullPath, data.cFileName);
                        String tempUserPath = Path.Combine(localSearchData.userPath, data.cFileName);

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

        private static String NormalizeSearchPattern(String searchPattern)
        {
            Contract.Requires(searchPattern != null);

            // Win32 normalization trims only U+0020.
            String tempSearchPattern = searchPattern.TrimEnd(PathInternal.s_trimEndChars);

            // Make this corner case more useful, like dir
            if (tempSearchPattern.Equals("."))
            {
                tempSearchPattern = "*";
            }

            PathInternal.CheckSearchPattern(tempSearchPattern);
            return tempSearchPattern;
        }

        private static String GetNormalizedSearchCriteria(String fullSearchString, String fullPathMod)
        {
            Contract.Requires(fullSearchString != null);
            Contract.Requires(fullPathMod != null);
            Contract.Requires(fullSearchString.Length >= fullPathMod.Length);

            String searchCriteria = null;
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

        private static String GetFullSearchString(String fullPath, String searchPattern)
        {
            Contract.Requires(fullPath != null);
            Contract.Requires(searchPattern != null);

            String tempStr = Path.Combine(fullPath, searchPattern);

            // If path ends in a trailing slash (\), append a * or we'll get a "Cannot find the file specified" exception
            char lastChar = tempStr[tempStr.Length - 1];
            if (PathInternal.IsDirectorySeparator(lastChar) || lastChar == Path.VolumeSeparatorChar)
            {
                tempStr = tempStr + '*';
            }

            return tempStr;
        }
    }

    internal abstract class SearchResultHandler<TSource>
    {

        internal abstract bool IsResultIncluded(SearchResult result);

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

        internal override bool IsResultIncluded(SearchResult result)
        {
            bool includeFile = _includeFiles && FileSystemEnumerableHelpers.IsFile(result.FindData);
            bool includeDir = _includeDirs && FileSystemEnumerableHelpers.IsDir(result.FindData);
            Debug.Assert(!(includeFile && includeDir), result.FindData.cFileName + ": current item can't be both file and dir!");
            return (includeFile || includeDir);
        }

        internal override String CreateObject(SearchResult result)
        {
            return result.UserPath;
        }
    }

    internal sealed class SearchResult
    {
        private String fullPath;     // fully-qualifed path
        private String userPath;     // user-specified path
        private Win32Native.WIN32_FIND_DATA findData;

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
            get { return findData; }
        }
    }

    internal static class FileSystemEnumerableHelpers
    {
        internal static bool IsDir(Win32Native.WIN32_FIND_DATA data)
        {
            // Don't add "." nor ".."
            return (0 != (data.dwFileAttributes & Win32Native.FILE_ATTRIBUTE_DIRECTORY))
                                                && !data.cFileName.Equals(".") && !data.cFileName.Equals("..");
        }

        internal static bool IsFile(Win32Native.WIN32_FIND_DATA data)
        {
            return 0 == (data.dwFileAttributes & Win32Native.FILE_ATTRIBUTE_DIRECTORY);
        }

    }
}

