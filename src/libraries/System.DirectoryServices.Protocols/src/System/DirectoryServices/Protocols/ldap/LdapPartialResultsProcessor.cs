// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace System.DirectoryServices.Protocols
{
    internal sealed class LdapPartialResultsProcessor
    {
        private readonly ArrayList _resultList = new ArrayList();
        private readonly ManualResetEvent _workThreadWaitHandle;
        private bool _workToDo;
        private int _currentIndex;

        internal LdapPartialResultsProcessor(ManualResetEvent eventHandle)
        {
            _workThreadWaitHandle = eventHandle;
            _ = new PartialResultsRetriever(eventHandle, this);
        }

        public void Add(LdapPartialAsyncResult asyncResult)
        {
            lock (this)
            {
                _resultList.Add(asyncResult);

                if (!_workToDo)
                {
                    // Need to wake up the workthread if it is not running already.
                    _workThreadWaitHandle.Set();
                    _workToDo = true;
                }
            }
        }

        public void Remove(LdapPartialAsyncResult asyncResult)
        {
            // Called by Abort operation.
            lock (this)
            {
                if (!_resultList.Contains(asyncResult))
                {
                    throw new ArgumentException(SR.InvalidAsyncResult);
                }

                // Remove this async operation from the list.
                _resultList.Remove(asyncResult);
            }
        }

        public void RetrievingSearchResults()
        {
            LdapPartialAsyncResult asyncResult = null;
            AsyncCallback tmpCallback = null;

            lock (this)
            {
                int count = _resultList.Count;

                if (count == 0)
                {
                    // No asynchronous operation pending, begin to wait.
                    _workThreadWaitHandle.Reset();
                    _workToDo = false;
                    return;
                }

                // Might have work to do.
                int i = 0;
                while (true)
                {
                    if (_currentIndex >= count)
                    {
                        // Some element is moved after last iteration.
                        _currentIndex = 0;
                    }

                    asyncResult = (LdapPartialAsyncResult)_resultList[_currentIndex];
                    i++;
                    _currentIndex++;

                    // Have work to do.
                    if (asyncResult._resultStatus != ResultsStatus.Done)
                    {
                        break;
                    }

                    if (i >= count)
                    {
                        // All the operations are done just waiting for the user to pick up the results.
                        _workToDo = false;
                        _workThreadWaitHandle.Reset();
                        return;
                    }
                }

                // Try to get the results available for this asynchronous operation  .
                GetResultsHelper(asyncResult);

                // If we are done with the asynchronous search, we need to fire callback and signal the waitable object.
                if (asyncResult._resultStatus == ResultsStatus.Done)
                {
                    asyncResult._manualResetEvent.Set();
                    asyncResult._completed = true;
                    if (asyncResult._callback != null)
                    {
                        tmpCallback = asyncResult._callback;
                    }
                }
                else if (asyncResult._callback != null && asyncResult._partialCallback)
                {
                    // The user specified a callback to be called even when partial results become available.
                    if (asyncResult._response != null && (asyncResult._response.Entries.Count > 0 || asyncResult._response.References.Count > 0))
                    {
                        tmpCallback = asyncResult._callback;
                    }
                }
            }

            tmpCallback?.Invoke(asyncResult);
        }

        private static void GetResultsHelper(LdapPartialAsyncResult asyncResult)
        {
            LdapConnection connection = asyncResult._con;
            ResultAll resultType = ResultAll.LDAP_MSG_RECEIVED;

            if (asyncResult._resultStatus == ResultsStatus.CompleteResult)
            {
                resultType = ResultAll.LDAP_MSG_POLLINGALL;
            }

            try
            {
                ValueTask<DirectoryResponse> vt = connection.ConstructResponseAsync(asyncResult._messageID, LdapOperation.LdapSearch, resultType, asyncResult._requestTimeout, false, sync: true);
                Debug.Assert(vt.IsCompleted);
                SearchResponse response = (SearchResponse)vt.GetAwaiter().GetResult();

                // This should only happen in the polling thread case.
                if (response == null)
                {
                    // Only when request time out has not yet expiered.
                    if ((asyncResult._startTime.Ticks + asyncResult._requestTimeout.Ticks) > DateTime.Now.Ticks)
                    {
                        // This is expected, just the client does not have the result yet .
                        return;
                    }
                    else
                    {
                        // time out, now we need to throw proper exception
                        throw new LdapException((int)LdapError.TimeOut, LdapErrorMappings.MapResultCode((int)LdapError.TimeOut));
                    }
                }

                if (asyncResult._response != null)
                {
                    AddResult(asyncResult._response, response);
                }
                else
                {
                    asyncResult._response = response;
                }

                // If search is done, set the flag.
                if (response.searchDone)
                {
                    asyncResult._resultStatus = ResultsStatus.Done;
                }
            }
            catch (Exception exception)
            {
                if (exception is DirectoryOperationException directoryOperationException)
                {
                    SearchResponse response = (SearchResponse)directoryOperationException.Response;
                    if (asyncResult._response != null)
                    {
                        AddResult(asyncResult._response, response);
                    }
                    else
                    {
                        asyncResult._response = response;
                    }

                    // Set the response back to the exception so it holds all the results up to now.
                    directoryOperationException.Response = asyncResult._response;
                }
                else if (exception is LdapException ldapException)
                {
                    if (asyncResult._response != null)
                    {
                        // add previous retrieved entries if available
                        if (asyncResult._response.Entries != null)
                        {
                            for (int i = 0; i < asyncResult._response.Entries.Count; i++)
                            {
                                ldapException.PartialResults.Add(asyncResult._response.Entries[i]);
                            }
                        }

                        // add previous retrieved references if available
                        if (asyncResult._response.References != null)
                        {
                            for (int i = 0; i < asyncResult._response.References.Count; i++)
                            {
                                ldapException.PartialResults.Add(asyncResult._response.References[i]);
                            }
                        }
                    }
                }

                // Exception occurs, this operation is done.
                asyncResult._exception = exception;
                asyncResult._resultStatus = ResultsStatus.Done;

                // Need to abandon this request.
                LdapPal.CancelDirectoryAsyncOperation(connection._ldapHandle, asyncResult._messageID);
            }
        }

        public void NeedCompleteResult(LdapPartialAsyncResult asyncResult)
        {
            lock (this)
            {
                if (_resultList.Contains(asyncResult))
                {
                    // We don't need partial results anymore, polling for complete results.
                    if (asyncResult._resultStatus == ResultsStatus.PartialResult)
                        asyncResult._resultStatus = ResultsStatus.CompleteResult;
                }
                else
                {
                    throw new ArgumentException(SR.InvalidAsyncResult);
                }
            }
        }

        public PartialResultsCollection GetPartialResults(LdapPartialAsyncResult asyncResult)
        {
            lock (this)
            {
                if (!_resultList.Contains(asyncResult))
                {
                    throw new ArgumentException(SR.InvalidAsyncResult);
                }

                if (asyncResult._exception != null)
                {
                    // Remove this async operation
                    // The async operation basically failed, we won't do it any more, so throw
                    // exception to the user and remove it from the list.
                    _resultList.Remove(asyncResult);
                    throw asyncResult._exception;
                }

                var collection = new PartialResultsCollection();
                if (asyncResult._response != null)
                {
                    if (asyncResult._response.Entries != null)
                    {
                        for (int i = 0; i < asyncResult._response.Entries.Count; i++)
                        {
                            collection.Add(asyncResult._response.Entries[i]);
                        }

                        asyncResult._response.Entries.Clear();
                    }

                    if (asyncResult._response.References != null)
                    {
                        for (int i = 0; i < asyncResult._response.References.Count; i++)
                        {
                            collection.Add(asyncResult._response.References[i]);
                        }

                        asyncResult._response.References.Clear();
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
                {
                    throw new ArgumentException(SR.InvalidAsyncResult);
                }

                Debug.Assert(asyncResult._resultStatus == ResultsStatus.Done);

                _resultList.Remove(asyncResult);

                if (asyncResult._exception != null)
                {
                    throw asyncResult._exception;
                }
                else
                {
                    return asyncResult._response;
                }
            }
        }

        private static void AddResult(SearchResponse partialResults, SearchResponse newResult)
        {
            if (newResult == null)
            {
                return;
            }

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

    internal sealed class PartialResultsRetriever
    {
        private readonly ManualResetEvent _workThreadWaitHandle;
        private readonly LdapPartialResultsProcessor _processor;

        internal PartialResultsRetriever(ManualResetEvent eventHandle, LdapPartialResultsProcessor processor)
        {
            _workThreadWaitHandle = eventHandle;
            _processor = processor;

            // Start the thread.
            var thread = new Thread(new ThreadStart(ThreadRoutine))
            {
                IsBackground = true,
                Name = ".NET LDAP Results Retriever"
            };
            thread.Start();
        }

        private void ThreadRoutine()
        {
            while (true)
            {
                // Make sure there is work to do.
                _workThreadWaitHandle.WaitOne();

                // Do the real work.
                try
                {
                    _processor.RetrievingSearchResults();
                }
                catch (Exception e)
                {
                    // We catch the exception here as we don't really want our worker thread to die because it
                    // encounter certain exception when processing a single async operation.
                    Debug.WriteLine(e.Message);
                }

                // Voluntarily gives up the CPU time.
                Thread.Sleep(250);
            }
        }
    }
}
