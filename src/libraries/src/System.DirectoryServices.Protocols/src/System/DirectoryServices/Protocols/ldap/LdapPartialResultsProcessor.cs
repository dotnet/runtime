// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.DirectoryServices.Protocols
{
    using System;
    using System.Threading;
    using System.Collections;
    using System.Diagnostics;

    internal class LdapPartialResultsProcessor
    {
        private ArrayList _resultList = null;
        private ManualResetEvent _workThreadWaitHandle = null;
        private bool _workToDo = false;
        private int _currentIndex = 0;

        internal LdapPartialResultsProcessor(ManualResetEvent eventHandle)
        {
            _resultList = new ArrayList();
            _workThreadWaitHandle = eventHandle;
        }

        public void Add(LdapPartialAsyncResult asyncResult)
        {
            lock (this)
            {
                _resultList.Add(asyncResult);

                if (!_workToDo)
                {
                    // need to wake up the workthread if it is not running already
                    _workThreadWaitHandle.Set();
                    _workToDo = true;
                }
            }
        }

        public void Remove(LdapPartialAsyncResult asyncResult)
        {
            // called by Abort operation
            lock (this)
            {
                if (!_resultList.Contains(asyncResult))
                    throw new ArgumentException(Res.GetString(Res.InvalidAsyncResult));

                // remove this async operation from the list
                _resultList.Remove(asyncResult);
            }
        }

        public void RetrievingSearchResults()
        {
            int count = 0;
            int i = 0;
            LdapPartialAsyncResult asyncResult = null;
            AsyncCallback tmpCallback = null;

            lock (this)
            {
                count = _resultList.Count;

                if (count == 0)
                {
                    // no asynchronous operation pending, begin to wait                    
                    _workThreadWaitHandle.Reset();
                    _workToDo = false;
                    return;
                }

                // might have work to do
                while (true)
                {
                    if (_currentIndex >= count)
                    {
                        // some element is moved after last iteration                        
                        _currentIndex = 0;
                    }

                    asyncResult = (LdapPartialAsyncResult)_resultList[_currentIndex];
                    i++;
                    _currentIndex++;

                    // have work to do
                    if (asyncResult.resultStatus != ResultsStatus.Done)
                        break;

                    if (i >= count)
                    {
                        // all the operations are done just waiting for the user to pick up the results                        
                        _workToDo = false;
                        _workThreadWaitHandle.Reset();
                        return;
                    }
                }

                // try to get the results availabe for this asynchronous operation                
                GetResultsHelper(asyncResult);

                // if we are done with the asynchronous search, we need to fire callback and signal the waitable object
                if (asyncResult.resultStatus == ResultsStatus.Done)
                {
                    asyncResult.manualResetEvent.Set();
                    asyncResult.completed = true;
                    if (asyncResult.callback != null)
                    {
                        tmpCallback = asyncResult.callback;
                    }
                }
                else if (asyncResult.callback != null && asyncResult.partialCallback)
                {
                    // if user specify callback to be called even when partial results become available                    
                    if (asyncResult.response != null && (asyncResult.response.Entries.Count > 0 || asyncResult.response.References.Count > 0))
                    {
                        tmpCallback = asyncResult.callback;
                    }
                }
            }

            if (tmpCallback != null)
                tmpCallback((IAsyncResult)asyncResult);
        }

        private void GetResultsHelper(LdapPartialAsyncResult asyncResult)
        {
            LdapConnection con = asyncResult.con;
            IntPtr ldapResult = (IntPtr)0;
            IntPtr entryMessage = (IntPtr)0;
            ResultAll resultType = ResultAll.LDAP_MSG_RECEIVED;

            if (asyncResult.resultStatus == ResultsStatus.CompleteResult)
                resultType = ResultAll.LDAP_MSG_POLLINGALL;

            try
            {
                SearchResponse response = (SearchResponse)con.ConstructResponse(asyncResult.messageID, LdapOperation.LdapSearch, resultType, asyncResult.requestTimeout, false);
                // this should only happen in the polling thread case                    
                if (response == null)
                {
                    // only when request time out has not yet expiered
                    if ((asyncResult.startTime.Ticks + asyncResult.requestTimeout.Ticks) > DateTime.Now.Ticks)
                    {
                        // this is expected, just the client does not have the result yet 
                        return;
                    }
                    else
                    {
                        // time out, now we need to throw proper exception                   
                        throw new LdapException((int)LdapError.TimeOut, LdapErrorMappings.MapResultCode((int)LdapError.TimeOut));
                    }
                }

                if (asyncResult.response != null)
                    AddResult(asyncResult.response, response);
                else
                    asyncResult.response = response;

                // if search is done, set the flag
                if (response.searchDone)
                    asyncResult.resultStatus = ResultsStatus.Done;
            }
            catch (Exception e)
            {
                if (e is DirectoryOperationException)
                {
                    SearchResponse response = (SearchResponse)(((DirectoryOperationException)e).Response);

                    if (asyncResult.response != null)
                        AddResult(asyncResult.response, response);
                    else
                        asyncResult.response = response;

                    // set the response back to the exception so it holds all the results up to now
                    ((DirectoryOperationException)e).response = asyncResult.response;
                }
                else if (e is LdapException)
                {
                    LdapException ldapE = (LdapException)e;
                    LdapError errorCode = (LdapError)ldapE.ErrorCode;

                    if (asyncResult.response != null)
                    {
                        // add previous retrieved entries if available
                        if (asyncResult.response.Entries != null)
                        {
                            for (int i = 0; i < asyncResult.response.Entries.Count; i++)
                            {
                                ldapE.results.Add(asyncResult.response.Entries[i]);
                            }
                        }

                        // add previous retrieved references if available
                        if (asyncResult.response.References != null)
                        {
                            for (int i = 0; i < asyncResult.response.References.Count; i++)
                            {
                                ldapE.results.Add(asyncResult.response.References[i]);
                            }
                        }
                    }
                }

                // exception occurs, this operation is done.
                asyncResult.exception = e;
                asyncResult.resultStatus = ResultsStatus.Done;

                // need to abandon this request
                Wldap32.ldap_abandon(con.ldapHandle, asyncResult.messageID);
            }
        }

        public void NeedCompleteResult(LdapPartialAsyncResult asyncResult)
        {
            lock (this)
            {
                if (_resultList.Contains(asyncResult))
                {
                    // we don't need partial results anymore, polling for complete results
                    if (asyncResult.resultStatus == ResultsStatus.PartialResult)
                        asyncResult.resultStatus = ResultsStatus.CompleteResult;
                }
                else
                    throw new ArgumentException(Res.GetString(Res.InvalidAsyncResult));
            }
        }

        public PartialResultsCollection GetPartialResults(LdapPartialAsyncResult asyncResult)
        {
            lock (this)
            {
                if (!_resultList.Contains(asyncResult))
                    throw new ArgumentException(Res.GetString(Res.InvalidAsyncResult));

                if (asyncResult.exception != null)
                {
                    // remove this async operation

                    // the async operation basically failed, we won't do it any more, so throw exception to the user and remove it from the list
                    _resultList.Remove(asyncResult);
                    throw asyncResult.exception;
                }

                PartialResultsCollection collection = new PartialResultsCollection();

                if (asyncResult.response != null)
                {
                    if (asyncResult.response.Entries != null)
                    {
                        for (int i = 0; i < asyncResult.response.Entries.Count; i++)
                            collection.Add(asyncResult.response.Entries[i]);

                        asyncResult.response.Entries.Clear();
                    }

                    if (asyncResult.response.References != null)
                    {
                        for (int i = 0; i < asyncResult.response.References.Count; i++)
                            collection.Add(asyncResult.response.References[i]);

                        asyncResult.response.References.Clear();
                    }
                }

                return collection;
            }
        }

        public DirectoryResponse GetCompleteResult(LdapPartialAsyncResult asyncResult)
        {
            lock (this)
            {
                if (!_resultList.Contains(asyncResult))
                    throw new ArgumentException(Res.GetString(Res.InvalidAsyncResult));

                Debug.Assert(asyncResult.resultStatus == ResultsStatus.Done);

                _resultList.Remove(asyncResult);

                if (asyncResult.exception != null)
                {
                    throw asyncResult.exception;
                }
                else
                {
                    return asyncResult.response;
                }
            }
        }

        private void AddResult(SearchResponse partialResults, SearchResponse newResult)
        {
            if (newResult == null)
                return;

            if (newResult.Entries != null)
            {
                for (int i = 0; i < newResult.Entries.Count; i++)
                {
                    partialResults.Entries.Add(newResult.Entries[i]);
                }
            }

            if (newResult.References != null)
            {
                for (int i = 0; i < newResult.References.Count; i++)
                {
                    partialResults.References.Add(newResult.References[i]);
                }
            }
        }
    }

    internal class PartialResultsRetriever
    {
        private ManualResetEvent _workThreadWaitHandle = null;
        private Thread _oThread = null;
        private LdapPartialResultsProcessor _processor = null;
        internal PartialResultsRetriever(ManualResetEvent eventHandle, LdapPartialResultsProcessor processor)
        {
            _workThreadWaitHandle = eventHandle;
            _processor = processor;
            _oThread = new Thread(new ThreadStart(ThreadRoutine));
            _oThread.IsBackground = true;

            // start the thread
            _oThread.Start();
        }

        private void ThreadRoutine()
        {
            while (true)
            {
                // make sure there is work to do
                _workThreadWaitHandle.WaitOne();

                // do the real work
                try
                {
                    _processor.RetrievingSearchResults();
                }
                catch (Exception e)
                {
                    // we catch the exception here as we don't really want our worker thread to die because it
                    // encounter certain exception when processing a single async operation.
                    Debug.WriteLine(e.Message);
                }

                // Voluntarily gives up the CPU time
                Thread.Sleep(250);
            }
        }
    }
}
