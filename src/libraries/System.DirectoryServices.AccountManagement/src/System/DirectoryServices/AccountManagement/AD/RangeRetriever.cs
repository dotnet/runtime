
/*++

Copyright (c) 2004  Microsoft Corporation

Module Name:

    RangeRetriever.cs

Abstract:

    Implements the RangeRetriever class
    for performing DirectoryEntry-based range-retrieval.

History:

    03-Sept-2004    MattRim     Created

--*/

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
    // <SecurityKernel Critical="True" Ring="0">
    // <Asserts Name="Declarative: [DirectoryServicesPermission(SecurityAction.Assert, Unrestricted = true)]" />
    // </SecurityKernel>
#pragma warning disable 618    // Have not migrated to v4 transparency yet
    [System.Security.SecurityCritical(System.Security.SecurityCriticalScope.Everything)]
#pragma warning restore 618
    [DirectoryServicesPermission(System.Security.Permissions.SecurityAction.Assert, Unrestricted=true)]
    class RangeRetriever : CollectionBase, IEnumerable, IEnumerator, IDisposable
    {
        /// 
        /// <summary>
        /// Creates a new RangeRetriever object.
        /// </summary>
        /// <param name="de">DirectoryEntry object whose attribute needs to be range retrieved</param>
        /// <param name="propertyName">name of the attribute that needs to be range retrieved, ex: "memberOf"</param>
        /// <param name="disposeDirEntry">
        /// If set to true, the supplied DirectoryEntry will be diposed, 
        /// by this object's Dispose() method
        /// </param>
        /// 
        public RangeRetriever(DirectoryEntry de, string propertyName, bool disposeDirEntry)
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "RangeRetriever", "RangeRetriever: de.Path={0}, propertyName={1}", de.Path, propertyName);                        
        
            this.de = de;
            this.propertyName = propertyName;
            this.disposeDirEntry = disposeDirEntry;
        }

        new public IEnumerator GetEnumerator()
        {
                return this;
        }

        public void Reset()
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "RangeRetriever", "Reset");                        
        
            this.endReached = false;
            this.lowRange = 0;
            this.currentResult = null;
            //this.currentEnumerator = null;
            this.currentIndex = 0;
        }

        public bool MoveNext()
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "RangeRetriever", "Entering MoveNext");

            if (endReached)
                return false;

            // Determine if we have already visited the current object.
            if (currentIndex < InnerList.Count)
            {
                this.currentResult = InnerList[currentIndex];
                currentIndex++;
                return true;
            }
            else if (cacheFilled)
            {
                // We have just walked the entire cache.  No need to visit the directory
                // since we cached everything that is out there.
                return false;
            }

            if (!endReached && this.currentEnumerator == null)
            {
                // First time we're being called
                GlobalDebug.WriteLineIf(GlobalDebug.Info, "RangeRetriever", "MoveNext: first time");
                
                this.currentEnumerator = GetNextChunk();

                if (this.currentEnumerator == null)
                {
                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "RangeRetriever", "MoveNext: got null enumerator for first time");                
                    endReached = true;
                }
            }


            if (endReached)
                return false;

            bool needToRetry;
            bool f;

            do
            {
                needToRetry = false;

                f = this.currentEnumerator.MoveNext();

                if (f)
                {
                    // Got a result, prepare to return it
                    this.currentResult = this.currentEnumerator.Current;
                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "RangeRetriever", "MoveNext: got a result '{0}'", this.currentResult.ToString());
                }
                else
                {
                    // Ran out of results in this range, try the next chunk
                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "RangeRetriever", "MoveNext: retrieving next range");                    
                    this.currentEnumerator = GetNextChunk();

                    if (this.currentEnumerator == null)
                    {
                        // No more chunks remain
                        GlobalDebug.WriteLineIf(GlobalDebug.Info, "RangeRetriever", "MoveNext: end reached");                    
                        endReached = true;
                        cacheFilled = cacheValues; //Set cachedFilled boolean to cacheValues flags.
                    }
                    else
                    {
                        // Got the next chunk, try pulling a result out of it
                        needToRetry = true;
                    }
                }
            }
            while (needToRetry);

            if ( f )
            {
                if (cacheValues)
                {
                    InnerList.Add(this.currentResult);
                }
                currentIndex++;
            }

            return f;
        }

        void IDisposable.Dispose()
        {
            if (!this.disposed)
            {
                if (this.disposeDirEntry)
                {
                    this.de.Dispose();
                }
            }
            this.disposed = true;
        }

        // returns null if no more results
        IEnumerator GetNextChunk()
        {
            string rangedAttribute = String.Format(
                                        CultureInfo.InvariantCulture,
                                        "{0};range={1}-*",
                                        this.propertyName, 
                                        this.lowRange);
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "RangeRetriever", "GetNextChunk: rangedAttribute={0}", rangedAttribute);

            try
            {
                // Pull in the next chunk of results
                this.de.RefreshCache(new string[]{rangedAttribute, this.propertyName});
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
            
            PropertyValueCollection pvc = this.de.Properties[this.propertyName];

            if (pvc == null || pvc.Count == 0)
            {
                // No results (the property may have been empty)
                GlobalDebug.WriteLineIf(GlobalDebug.Info, "RangeRetriever", "GetNextChunk: empty property?");                
                return null;
            }
            else
            {
                this.lowRange = this.lowRange + pvc.Count;

                GlobalDebug.WriteLineIf(GlobalDebug.Info,
                                        "RangeRetriever",
                                        "GetNextChunk: new lowRange={0}",
                                        this.lowRange);
            
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
            set { cacheValues = value; }
        }

        public object Current
        {
            get
            {            
                // Technically, should throw an InvalidOperationException if the enumerator is positioned before 
                // the beginning or after the end, but this will only be used internally.
                Debug.Assert(this.currentResult != null && this.endReached == false);
                GlobalDebug.WriteLineIf(GlobalDebug.Info, "RangeRetriever", "Current: currentResult={0}", this.currentResult.ToString());                        
                return this.currentResult;
            }
        }

        bool disposed = false;                  // keeps track of whether this object was disposed or not.
        bool disposeDirEntry = false;           // If set to true then the RangeRetriever object will own the directory entry
                                                // supplied to it in the constructor and will be responsible for disposing this entry
                                                // when Dispose() is called on this object.
        bool cacheValues = false;               // If set to true then the attribute values will be cached in the InnerList
                                                // By default caching is turned off.
        DirectoryEntry de;                      // the DirectoryEntry whose property we're retrieving the values of
        string propertyName;                    // the property we're retrieving the values of

        bool endReached = false;                // if true, all property values (from all chunks) have been retrieved

        int lowRange = 0;                       // the lower-bound of the current chunk
        int currentIndex = 0;                   // Index of the next object we will return.
        bool cacheFilled = false;               // Set to true after we have visited all attributes in the directory

        object currentResult = null;            // the property value the enumerator is positioned at
        IEnumerator currentEnumerator = null;   // the PropertyValueCollection enumerator for the current chunk
    }
}

