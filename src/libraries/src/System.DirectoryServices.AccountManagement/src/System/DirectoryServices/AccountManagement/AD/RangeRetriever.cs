// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Diagnostics;
using System.Globalization;

namespace System.DirectoryServices.AccountManagement
{
    /// 
    /// <summary>
    /// Retrieves all the values of the specified attribute using the supplied DirectoryEntry object.
    /// This function would additionally dispose the supplied DirectoryEntry object in its Dispose() method
    /// if disposeDirEntry parameter is set to true in its constructor.
    /// </summary>
    /// 
    internal class RangeRetriever : CollectionBase, IEnumerable, IEnumerator, IDisposable
    {
        /// 
        /// <summary>
        /// Creates a new RangeRetriever object.
        /// </summary>
        /// <param name="de">DirectoryEntry object whose attribute needs to be range retrieved</param>
        /// <param name="propertyName">name of the attribute that needs to be range retrieved, ex: "memberOf"</param>
        /// <param name="disposeDirEntry">
        /// If set to true, the supplied DirectoryEntry will be disposed, 
        /// by this object's Dispose() method
        /// </param>
        /// 
        public RangeRetriever(DirectoryEntry de, string propertyName, bool disposeDirEntry)
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "RangeRetriever", "RangeRetriever: de.Path={0}, propertyName={1}", de.Path, propertyName);

            _de = de;
            _propertyName = propertyName;
            _disposeDirEntry = disposeDirEntry;
        }

        new public IEnumerator GetEnumerator()
        {
            return this;
        }

        public void Reset()
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "RangeRetriever", "Reset");

            _endReached = false;
            _lowRange = 0;
            _currentResult = null;
            //this.currentEnumerator = null;
            _currentIndex = 0;
        }

        public bool MoveNext()
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "RangeRetriever", "Entering MoveNext");

            if (_endReached)
                return false;

            // Determine if we have already visited the current object.
            if (_currentIndex < InnerList.Count)
            {
                _currentResult = InnerList[_currentIndex];
                _currentIndex++;
                return true;
            }
            else if (_cacheFilled)
            {
                // We have just walked the entire cache.  No need to visit the directory
                // since we cached everything that is out there.
                return false;
            }

            if (!_endReached && _currentEnumerator == null)
            {
                // First time we're being called
                GlobalDebug.WriteLineIf(GlobalDebug.Info, "RangeRetriever", "MoveNext: first time");

                _currentEnumerator = GetNextChunk();

                if (_currentEnumerator == null)
                {
                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "RangeRetriever", "MoveNext: got null enumerator for first time");
                    _endReached = true;
                }
            }

            if (_endReached)
                return false;

            bool needToRetry;
            bool f;

            do
            {
                needToRetry = false;

                f = _currentEnumerator.MoveNext();

                if (f)
                {
                    // Got a result, prepare to return it
                    _currentResult = _currentEnumerator.Current;
                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "RangeRetriever", "MoveNext: got a result '{0}'", _currentResult.ToString());
                }
                else
                {
                    // Ran out of results in this range, try the next chunk
                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "RangeRetriever", "MoveNext: retrieving next range");
                    _currentEnumerator = GetNextChunk();

                    if (_currentEnumerator == null)
                    {
                        // No more chunks remain
                        GlobalDebug.WriteLineIf(GlobalDebug.Info, "RangeRetriever", "MoveNext: end reached");
                        _endReached = true;
                        _cacheFilled = _cacheValues; //Set cachedFilled boolean to cacheValues flags.
                    }
                    else
                    {
                        // Got the next chunk, try pulling a result out of it
                        needToRetry = true;
                    }
                }
            }
            while (needToRetry);

            if (f)
            {
                if (_cacheValues)
                {
                    InnerList.Add(_currentResult);
                }
                _currentIndex++;
            }

            return f;
        }

        void IDisposable.Dispose()
        {
            if (!_disposed)
            {
                if (_disposeDirEntry)
                {
                    _de.Dispose();
                }
            }
            _disposed = true;
        }

        // returns null if no more results
        private IEnumerator GetNextChunk()
        {
            string rangedAttribute = string.Format(
                                        CultureInfo.InvariantCulture,
                                        "{0};range={1}-*",
                                        _propertyName,
                                        _lowRange);
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "RangeRetriever", "GetNextChunk: rangedAttribute={0}", rangedAttribute);

            try
            {
                // Pull in the next chunk of results
                _de.RefreshCache(new string[] { rangedAttribute, _propertyName });
            }
            catch (System.Runtime.InteropServices.COMException e)
            {
                if (e.ErrorCode == unchecked((int)0x80072020))
                {
                    // ran out of results to retrieve
                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "RangeRetriever", "GetNextChunk: no more results");
                    return null;
                }

                // unknown failure, don't want to suppress it
                GlobalDebug.WriteLineIf(GlobalDebug.Error, "RangeRetriever", "GetNextChunk: caught COMException, ErrorCode={0}", e.ErrorCode);

                throw;
            }

            PropertyValueCollection pvc = _de.Properties[_propertyName];

            if (pvc == null || pvc.Count == 0)
            {
                // No results (the property may have been empty)
                GlobalDebug.WriteLineIf(GlobalDebug.Info, "RangeRetriever", "GetNextChunk: empty property?");
                return null;
            }
            else
            {
                _lowRange = _lowRange + pvc.Count;

                GlobalDebug.WriteLineIf(GlobalDebug.Info,
                                        "RangeRetriever",
                                        "GetNextChunk: new lowRange={0}",
                                        _lowRange);

                return pvc.GetEnumerator();
            }
        }

        /// 
        /// <summary>
        /// If set to true then the attribute values will be cached in the InnerList
        /// </summary>
        /// <remarks>
        /// By default caching is turned off.
        /// </remarks>
        public bool CacheValues
        {
            set { _cacheValues = value; }
        }

        public object Current
        {
            get
            {
                // Technically, should throw an InvalidOperationException if the enumerator is positioned before 
                // the beginning or after the end, but this will only be used internally.
                Debug.Assert(_currentResult != null && _endReached == false);
                GlobalDebug.WriteLineIf(GlobalDebug.Info, "RangeRetriever", "Current: currentResult={0}", _currentResult.ToString());
                return _currentResult;
            }
        }

        private bool _disposed = false;                  // keeps track of whether this object was disposed or not.
        private bool _disposeDirEntry = false;           // If set to true then the RangeRetriever object will own the directory entry
                                                         // supplied to it in the constructor and will be responsible for disposing this entry
                                                         // when Dispose() is called on this object.
        private bool _cacheValues = false;               // If set to true then the attribute values will be cached in the InnerList
                                                         // By default caching is turned off.
        private DirectoryEntry _de;                      // the DirectoryEntry whose property we're retrieving the values of
        private string _propertyName;                    // the property we're retrieving the values of

        private bool _endReached = false;                // if true, all property values (from all chunks) have been retrieved

        private int _lowRange = 0;                       // the lower-bound of the current chunk
        private int _currentIndex = 0;                   // Index of the next object we will return.
        private bool _cacheFilled = false;               // Set to true after we have visited all attributes in the directory

        private object _currentResult = null;            // the property value the enumerator is positioned at
        private IEnumerator _currentEnumerator = null;   // the PropertyValueCollection enumerator for the current chunk
    }
}

