// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

// TODO: Once we upgrade to C# 6, remove all of these and simply import the libcurl class.
using CURLcode = Interop.libcurl.CURLcode;
using CURLINFO = Interop.libcurl.CURLINFO;
using CURLMcode = Interop.libcurl.CURLMcode;
using CURLoption = Interop.libcurl.CURLoption;
using SafeCurlMultiHandle = Interop.libcurl.SafeCurlMultiHandle;
using size_t = System.UInt64; // TODO: IntPtr

namespace System.Net.Http
{
    internal partial class CurlHandler : HttpMessageHandler
    {
        /// <summary>Provides a multi handle and the associated processing for all requests on the handle.</summary>
        private sealed class MultiAgent
        {
            private static readonly Interop.libcurl.curl_readwrite_callback s_receiveHeadersCallback = CurlReceiveHeadersCallback;
            private static readonly Interop.libcurl.curl_readwrite_callback s_sendCallback = CurlSendCallback;
            private static readonly Interop.libcurl.seek_callback s_seekCallback = CurlSeekCallback;
            private static readonly Interop.libcurl.curl_readwrite_callback s_receiveBodyCallback = CurlReceiveBodyCallback;
            
            /// <summary>
            /// A collection of not-yet-processed incoming requests for work to be done
            /// by this multi agent.  This can include making new requests, canceling
            /// active requests, or unpausing active requests.
            /// Protected by a lock on <see cref="_incomingRequests"/>.
            /// </summary>
            private readonly Queue<IncomingRequest> _incomingRequests = new Queue<IncomingRequest>();
         
            /// <summary>Map of activeOperations, indexed by a GCHandle ptr.</summary>
            private readonly Dictionary<IntPtr, ActiveRequest> _activeOperations = new Dictionary<IntPtr, ActiveRequest>();
            
            /// <summary>Buffer used to transfer data from a request content stream to libcurl.</summary>
            private readonly byte[] _transferBuffer = new byte[RequestBufferSize];

            /// <summary>
            /// Special file descriptor used to wake-up curl_multi_wait calls.  This is the read
            /// end of a pipe, with the write end written to when work is queued or when cancellation
            /// is requested. This is only valid while the worker is executing.
            /// </summary>
            private int _wakeupRequestedPipeFd;

            /// <summary>
            /// Write end of the pipe connected to <see cref="_wakeupRequestedPipeFd"/>.
            /// This is only valid while the worker is executing.
            /// </summary>
            private int _requestWakeupPipeFd;
            
            /// <summary>
            /// Task for the currently running worker, or null if there is no current worker.
            /// Protected by a lock on <see cref="_incomingRequests"/>.
            /// </summary>
            private Task _runningWorker;

            /// <summary>Queues a request for the multi handle to process.</summary>
            public void Queue(IncomingRequest request)
            {
                lock (_incomingRequests)
                {
                    // Add the request, then initiate processing.
                    _incomingRequests.Enqueue(request);
                    EnsureWorkerIsRunning();
                }
            }

            /// <summary>Gets the ID of the currently running worker, or null if there isn't one.</summary>
            internal int? RunningWorkerId { get { return _runningWorker != null ? (int?)_runningWorker.Id : null; } }

            /// <summary>Schedules the processing worker if one hasn't already been scheduled.</summary>
            private void EnsureWorkerIsRunning()
            {
                Debug.Assert(Monitor.IsEntered(_incomingRequests), "Needs to be called under _incomingRequests lock");

                if (_runningWorker == null)
                {
                    VerboseTrace("MultiAgent worker queued");

                    // Create pipe used to forcefully wake up curl_multi_wait calls when something important changes.
                    // This is created here rather than in Process so that the pipe is available immediately
                    // for subsequent queue calls to use.
                    Debug.Assert(_wakeupRequestedPipeFd == 0, "Read pipe should have been cleared");
                    Debug.Assert(_requestWakeupPipeFd == 0, "Write pipe should have been cleared");
                    unsafe
                    {
                        int* fds = stackalloc int[2];
                        while (Interop.CheckIo(Interop.Sys.Pipe(fds))) ;
                        _wakeupRequestedPipeFd = fds[Interop.Sys.ReadEndOfPipe];
                        _requestWakeupPipeFd = fds[Interop.Sys.WriteEndOfPipe];
                    }

                    // Kick off the processing task.  It's "DenyChildAttach" to avoid any surprises if
                    // code happens to create attached tasks, and it's LongRunning because this thread
                    // is likely going to sit around for a while in a wait loop (and the more requests
                    // are concurrently issued to the same agent, the longer the thread will be around).
                    const TaskCreationOptions Options = TaskCreationOptions.DenyChildAttach | TaskCreationOptions.LongRunning;
                    _runningWorker = new Task(s =>
                    {
                        VerboseTrace("MultiAgent worker starting");
                        var thisRef = (MultiAgent)s;
                        try
                        {
                            // Do the actual processing
                            thisRef.Process();
                        }
                        catch (Exception exc)
                        {
                            Debug.Fail("Unexpected exception from processing loop: " + exc.ToString());
                        }
                        finally
                        {
                            VerboseTrace("MultiAgent worker shutting down");
                            lock (thisRef._incomingRequests)
                            {
                                // Close our wakeup pipe (ignore close errors).
                                // This is done while holding the lock to prevent
                                // subsequent Queue calls to see an improperly configured
                                // set of descriptors.
                                Interop.Sys.Close(thisRef._wakeupRequestedPipeFd);
                                thisRef._wakeupRequestedPipeFd = 0;
                                Interop.Sys.Close(thisRef._requestWakeupPipeFd);
                                thisRef._requestWakeupPipeFd = 0;

                                // In the time between we stopped processing and now,
                                // more requests could have been added.  If they were
                                // kick off another processing loop.
                                thisRef._runningWorker = null;
                                if (thisRef._incomingRequests.Count > 0)
                                {
                                    thisRef.EnsureWorkerIsRunning();
                                }
                            }
                        }
                    }, this, CancellationToken.None, Options);
                    _runningWorker.Start(TaskScheduler.Default); // started after _runningWorker field set to avoid race conditions
                }
                else // _workerRunning == true
                {
                    // The worker is already running.  If there are already queued requests, we're done.
                    // However, if there aren't any queued requests, Process could be blocked inside of
                    // curl_multi_wait, and we want to make sure it wakes up to see that there additional
                    // requests waiting to be handled.  So we write to the wakeup pipe.
                    Debug.Assert(_incomingRequests.Count >= 1, "We just queued a request, so the count should be at least 1");
                    if (_incomingRequests.Count == 1)
                    {
                        RequestWakeup();
                    }
                }
            }

            /// <summary>Write a byte to the wakeup pipe.</summary>
            private void RequestWakeup()
            {
                unsafe
                {
                    VerboseTrace("Writing to wakeup pipe");
                    byte b = 1;
                    while ((Interop.CheckIo((long)Interop.libc.write(_requestWakeupPipeFd, &b, (IntPtr)1)))) ;
                }
            }

            /// <summary>Clears data from the wakeup pipe.</summary>
            /// <remarks>
            /// This must only be called when we know there's data to be read.
            /// The MultiAgent could easily deadlock if it's called when there's no data in the pipe.
            /// </remarks>
            private unsafe void ReadFromWakeupPipeWhenKnownToContainData()
            {
                // It's possible but unlikely that there will be tons of extra data in the pipe, 
                // more than we end up reading out here (it's unlikely because we only write a byte to the pipe when 
                // transitioning from 0 to 1 incoming request).  In that unlikely event, the worst
                // case will be that the next one or more waits will wake up immediately, with each one
                // subsequently clearing out more of the pipe.
                const int ClearBufferSize = 64; // sufficiently large to clear the pipe in any normal case
                byte* clearBuf = stackalloc byte[ClearBufferSize];
                long bytesRead;
                while (Interop.CheckIo(bytesRead = (long)Interop.libc.read(_wakeupRequestedPipeFd, clearBuf, (IntPtr)ClearBufferSize))) ;
                VerboseTraceIf(bytesRead > 1, "Read more than one byte from wakeup pipe: " + bytesRead);
            }

            private void Process()
            {
                Debug.Assert(!Monitor.IsEntered(_incomingRequests), "No locks should be held while invoking Process");
                Debug.Assert(_runningWorker != null && _runningWorker.Id == Task.CurrentId, "This is the worker, so it must be running");
                Debug.Assert(_wakeupRequestedPipeFd != 0, "Should have a valid pipe for wake ups");

                // Create the multi handle to use for this round of processing.  This one handle will be used
                // to service all easy requests currently available and all those that come in while
                // we're processing other requests.  Once the work quiesces and there are no more requests
                // to process, this multi handle will be released as the worker goes away.  The next
                // time a request arrives and a new worker is spun up, a new multi handle will be created.
                SafeCurlMultiHandle multiHandle = Interop.libcurl.curl_multi_init();
                if (multiHandle.IsInvalid)
                {
                    throw CreateHttpRequestException();
                }

                // Clear our active operations table.  This should already be clear, either because
                // all previous operations completed without unexpected exception, or in the case of an
                // unexpected exception we should have cleaned up gracefully anyway.  But just in case...
                Debug.Assert(_activeOperations.Count == 0, "We shouldn't have any active operations when starting processing.");
                _activeOperations.Clear();

                bool endingSuccessfully = false;
                try
                {
                    // Continue processing as long as there are any active operations
                    while (true)
                    {
                        // First handle any requests in the incoming requests queue.
                        while (true)
                        {
                            IncomingRequest request;
                            lock (_incomingRequests)
                            {
                                if (_incomingRequests.Count == 0) break;
                                request = _incomingRequests.Dequeue();
                            }
                            HandleIncomingRequest(multiHandle, request);
                        }

                        // If we have no active operations, we're done.
                        if (_activeOperations.Count == 0)
                        {
                            endingSuccessfully = true;
                            return;
                        }

                        // We have one or more active operations. Run any work that needs to be run.
                        int running_handles;
                        ThrowIfCURLMError(Interop.libcurl.curl_multi_perform(multiHandle, out running_handles));

                        // Complete and remove any requests that have finished being processed.
                        int pendingMessages;
                        IntPtr messagePtr;
                        while ((messagePtr = Interop.libcurl.curl_multi_info_read(multiHandle, out pendingMessages)) != IntPtr.Zero)
                        {
                            Interop.libcurl.CURLMsg message = Marshal.PtrToStructure<Interop.libcurl.CURLMsg>(messagePtr);
                            Debug.Assert(message.msg == Interop.libcurl.CURLMSG.CURLMSG_DONE, "CURLMSG_DONE is supposed to be the only message type");

                            IntPtr gcHandlePtr;
                            ActiveRequest completedOperation;
                            if (message.msg == Interop.libcurl.CURLMSG.CURLMSG_DONE)
                            {
                                int getInfoResult = Interop.libcurl.curl_easy_getinfo(message.easy_handle, CURLINFO.CURLINFO_PRIVATE, out gcHandlePtr);
                                Debug.Assert(getInfoResult == CURLcode.CURLE_OK, "Failed to get info on a completing easy handle");
                                if (getInfoResult == CURLcode.CURLE_OK)
                                {
                                    bool gotActiveOp = _activeOperations.TryGetValue(gcHandlePtr, out completedOperation);
                                    Debug.Assert(gotActiveOp, "Expected to find GCHandle ptr in active operations table");
                                    if (gotActiveOp)
                                    {
                                        DeactivateActiveRequest(multiHandle, completedOperation.Easy, gcHandlePtr, completedOperation.CancellationRegistration);
                                        FinishRequest(completedOperation.Easy, message.result);
                                    }
                                }
                            }
                        }

                        // Wait for more things to do.  Even with our cancellation mechanism, we specify a timeout so that
                        // just in case something goes wrong we can recover gracefully.  This timeout is relatively long.
                        // Note, though, that libcurl has its own internal timeout, which can be requested separately
                        // via curl_multi_timeout, but which is used implicitly by curl_multi_wait if it's shorter
                        // than the value we provide.
                        const int FailsafeTimeoutMilliseconds = 1000;
                        int numFds;
                        unsafe
                        {
                            Interop.libcurl.curl_waitfd extraFds = new Interop.libcurl.curl_waitfd {
                                fd = _wakeupRequestedPipeFd,
                                events = Interop.libcurl.CURL_WAIT_POLLIN,
                                revents = 0
                            };
                            ThrowIfCURLMError(Interop.libcurl.curl_multi_wait(multiHandle, &extraFds, 1, FailsafeTimeoutMilliseconds, out numFds));
                            if ((extraFds.revents & Interop.libcurl.CURL_WAIT_POLLIN) != 0)
                            {
                                // We woke up (at least in part) because a wake-up was requested.  
                                // Read the data out of the pipe to clear it.
                                Debug.Assert(numFds >= 1, "numFds should have been at least one, as the extraFd was set");
                                VerboseTrace("curl_multi_wait wake-up notification");
                                ReadFromWakeupPipeWhenKnownToContainData();
                            }
                            VerboseTraceIf(numFds == 0, "curl_multi_wait timeout");
                        }

                        // PERF NOTE: curl_multi_wait uses poll (assuming it's available), which is O(N) in terms of the number of fds 
                        // being waited on. If this ends up being a scalability bottleneck, we can look into using the curl_multi_socket_* 
                        // APIs, which would let us switch to using epoll by being notified when sockets file descriptors are added or 
                        // removed and configuring the epoll context with EPOLL_CTL_ADD/DEL, which at the expense of a lot of additional
                        // complexity would let us turn the O(N) operation into an O(1) operation.  The additional complexity would come
                        // not only in the form of additional callbacks and managing the socket collection, but also in the form of timer
                        // management, which is necessary when using the curl_multi_socket_* APIs and which we avoid by using just
                        // curl_multi_wait/perform.
                    }
                }
                finally
                {
                    // If we got an unexpected exception, something very bad happened. We may have some 
                    // operations that we initiated but that weren't completed. Make sure to clean up any 
                    // such operations, failing them and releasing their resources.
                    if (_activeOperations.Count > 0)
                    {
                        Debug.Assert(!endingSuccessfully, "We should only have remaining operations if we got an unexpected exception");
                        foreach (KeyValuePair<IntPtr, ActiveRequest> pair in _activeOperations)
                        {
                            ActiveRequest failingOperation = pair.Value;
                            IntPtr failingOperationGcHandle = pair.Key;

                            DeactivateActiveRequest(multiHandle, failingOperation.Easy, failingOperationGcHandle, failingOperation.CancellationRegistration);

                            // Complete the operation's task and clean up any of its resources
                            failingOperation.Easy.FailRequest(CreateHttpRequestException());
                            failingOperation.Easy.Cleanup(); // no active processing remains, so cleanup
                        }

                        // Clear the table.
                        _activeOperations.Clear();
                    }

                    // Finally, dispose of the multi handle.
                    multiHandle.Dispose();
                }
            }

            private void HandleIncomingRequest(SafeCurlMultiHandle multiHandle, IncomingRequest request)
            {
                Debug.Assert(!Monitor.IsEntered(_incomingRequests), "Incoming requests lock should only be held while accessing the queue");
                VerboseTrace("Type: " + request.Type, easy: request.Easy);

                EasyRequest easy = request.Easy;
                switch (request.Type)
                {
                    case IncomingRequestType.New:
                        ActivateNewRequest(multiHandle, easy);
                        break;

                    case IncomingRequestType.Cancel:
                        Debug.Assert(easy._associatedMultiAgent == this, "Should only cancel associated easy requests");
                        Debug.Assert(easy._cancellationToken.IsCancellationRequested, "Cancellation should have been requested");
                        FindAndFailActiveRequest(multiHandle, easy, new OperationCanceledException(easy._cancellationToken));
                        break;

                    case IncomingRequestType.Unpause:
                        Debug.Assert(easy._associatedMultiAgent == this, "Should only unpause associated easy requests");
                        if (!easy._easyHandle.IsClosed)
                        {
                            IntPtr gcHandlePtr;
                            ActiveRequest ar;
                            Debug.Assert(FindActiveRequest(easy, out gcHandlePtr, out ar), "Couldn't find active request for unpause");
                            Debug.Assert(easy._paused == EasyRequest.PauseState.UnpauseRequestIssued, "Unpause requests only make sense when a request has been issued");

                            easy._paused = EasyRequest.PauseState.Unpaused;
                            int unpauseResult = Interop.libcurl.curl_easy_pause(easy._easyHandle, Interop.libcurl.CURLPAUSE_CONT);
                            if (unpauseResult != CURLcode.CURLE_OK)
                            {
                                FindAndFailActiveRequest(multiHandle, easy, new CurlException(unpauseResult, isMulti: false));
                            }
                        }
                        break;

                    default:
                        Debug.Fail("Invalid request type: " + request.Type);
                        break;
                }
            }

            private void ActivateNewRequest(SafeCurlMultiHandle multiHandle, EasyRequest easy)
            {
                Debug.Assert(easy != null, "We should never get a null request");
                Debug.Assert(easy._associatedMultiAgent == null, "New requests should not be associated with an agent yet");

                // If cancellation has been requested, complete the request proactively
                if (easy._cancellationToken.IsCancellationRequested)
                {
                    easy.FailRequest(new OperationCanceledException(easy._cancellationToken));
                    easy.Cleanup(); // no active processing remains, so cleanup
                    return;
                }

                // Otherwise, configure it.  Most of the configuration was already done when the EasyRequest
                // was created, but there's additional configuration we need to do specific to this
                // multi agent, specifically telling the easy request about its own GCHandle and setting
                // up callbacks for data processing.  Once it's configured, add it to the multi handle.
                GCHandle gcHandle = GCHandle.Alloc(easy);
                IntPtr gcHandlePtr = GCHandle.ToIntPtr(gcHandle);
                try
                {
                    easy._associatedMultiAgent = this;
                    easy.SetCurlOption(CURLoption.CURLOPT_PRIVATE, gcHandlePtr);
                    SetCurlCallbacks(easy, gcHandlePtr);
                    ThrowIfCURLMError(Interop.libcurl.curl_multi_add_handle(multiHandle, easy._easyHandle));
                }
                catch (Exception exc)
                {
                    gcHandle.Free();
                    easy.FailRequest(exc);
                    easy.Cleanup();  // no active processing remains, so cleanup
                    return;
                }

                // And if cancellation can be requested, hook up a cancellation callback.
                // This callback will put the easy request back into the queue, which will
                // ensure that a wake-up request has been issued.  When we pull
                // the easy request out of the request queue, we'll see that it's already
                // associated with this agent, meaning that it's a cancellation request,
                // and we'll deal with it appropriately.
                var cancellationReg = default(CancellationTokenRegistration);
                if (easy._cancellationToken.CanBeCanceled)
                {
                    cancellationReg = easy._cancellationToken.Register(s =>
                    {
                        var state = (Tuple<MultiAgent, EasyRequest>)s;
                        state.Item1.Queue(new IncomingRequest { Easy = state.Item2, Type = IncomingRequestType.Cancel });
                    }, Tuple.Create<MultiAgent, EasyRequest>(this, easy));
                }

                // Finally, add it to our map.
                _activeOperations.Add(
                    gcHandlePtr, 
                    new ActiveRequest { Easy = easy, CancellationRegistration = cancellationReg });
            }

            private void DeactivateActiveRequest(
                SafeCurlMultiHandle multiHandle, EasyRequest easy, 
                IntPtr gcHandlePtr, CancellationTokenRegistration cancellationRegistration)
            {
                // Remove the operation from the multi handle so we can shut down the multi handle cleanly
                int removeResult = Interop.libcurl.curl_multi_remove_handle(multiHandle, easy._easyHandle);
                Debug.Assert(removeResult == CURLMcode.CURLM_OK, "Failed to remove easy handle"); // ignore cleanup errors in release

                // Release the associated GCHandle so that it's not kept alive forever
                if (gcHandlePtr != IntPtr.Zero)
                {
                    try
                    {
                        GCHandle.FromIntPtr(gcHandlePtr).Free();
                        _activeOperations.Remove(gcHandlePtr);
                    }
                    catch (InvalidOperationException)
                    {
                        Debug.Fail("Couldn't get/free the GCHandle for an active operation while shutting down due to failure");
                    }
                }

                // Undo cancellation registration
                cancellationRegistration.Dispose();
            }

            private bool FindActiveRequest(EasyRequest easy, out IntPtr gcHandlePtr, out ActiveRequest activeRequest)
            {
                // We maintain an IntPtr=>ActiveRequest mapping, which makes it cheap to look-up by GCHandle ptr but
                // expensive to look up by EasyRequest.  If we find this becoming a bottleneck, we can add a reverse
                // map that stores the other direction as well.
                foreach (KeyValuePair<IntPtr, ActiveRequest> pair in _activeOperations)
                {
                    if (pair.Value.Easy == easy)
                    {
                        gcHandlePtr = pair.Key;
                        activeRequest = pair.Value;
                        return true;
                    }
                }
                gcHandlePtr = IntPtr.Zero;
                activeRequest = default(ActiveRequest);
                return false;
            }

            private void FindAndFailActiveRequest(SafeCurlMultiHandle multiHandle, EasyRequest easy, Exception error)
            {
                VerboseTrace("Error: " + error.Message, easy: easy);

                IntPtr gcHandlePtr;
                ActiveRequest activeRequest;
                if (FindActiveRequest(easy, out gcHandlePtr, out activeRequest))
                {
                    DeactivateActiveRequest(multiHandle, easy, gcHandlePtr, activeRequest.CancellationRegistration);
                    easy.FailRequest(error);
                    easy.Cleanup(); // no active processing remains, so we can cleanup
                }
                else
                {
                    Debug.Assert(easy.Task.IsCompleted, "We should only not be able to find the request if it failed or we started to send back the response.");
                }
            }

            private void FinishRequest(EasyRequest completedOperation, int messageResult)
            {
                VerboseTrace("messageResult: " + messageResult, easy: completedOperation);

                if (completedOperation._responseMessage.StatusCode != HttpStatusCode.Unauthorized && completedOperation._handler.PreAuthenticate)
                {
                    ulong availedAuth;
                    if (Interop.libcurl.curl_easy_getinfo(completedOperation._easyHandle, CURLINFO.CURLINFO_HTTPAUTH_AVAIL, out availedAuth) == CURLcode.CURLE_OK)
                    {
                        // TODO: fix locking in AddCredentialToCache
                        completedOperation._handler.AddCredentialToCache(
                            completedOperation._requestMessage.RequestUri, availedAuth, completedOperation._networkCredential);
                    }
                    // Ignore errors: no need to fail for the sake of putting the credentials into the cache
                }

                switch (messageResult)
                {
                    case CURLcode.CURLE_OK:
                        completedOperation.EnsureResponseMessagePublished();
                        break;
                    default:
                        completedOperation.FailRequest(CreateHttpRequestException(new CurlException(messageResult, isMulti: false)));
                        break;
                }

                // At this point, we've completed processing the entire request, either due to error
                // or due to completing the entire response.
                completedOperation.Cleanup();
            }
            
            private static size_t CurlReceiveHeadersCallback(IntPtr buffer, size_t size, size_t nitems, IntPtr context)
            {
                CurlHandler.VerboseTrace("size: " + size + ", nitems: " + nitems);
                size *= nitems;
                if (size == 0)
                {
                    return 0;
                }

                EasyRequest easy;
                if (TryGetEasyRequestFromContext(context, out easy))
                {
                    try
                    {
                        // The callback is invoked once per header; multi-line headers get merged into a single line.
                        string responseHeader = Marshal.PtrToStringAnsi(buffer).Trim();
                        HttpResponseMessage response = easy._responseMessage;

                        if (!TryParseStatusLine(response, responseHeader, easy))
                        {
                            int colonIndex = responseHeader.IndexOf(':');

                            // Skip malformed header lines that are missing the colon character.
                            if (colonIndex > 0)
                            {
                                string headerName = responseHeader.Substring(0, colonIndex);
                                string headerValue = responseHeader.Substring(colonIndex + 1);

                                if (!response.Headers.TryAddWithoutValidation(headerName, headerValue))
                                {
                                    response.Content.Headers.TryAddWithoutValidation(headerName, headerValue);
                                }
                            }
                        }

                        return size;
                    }
                    catch (Exception ex)
                    {
                        easy.FailRequest(ex); // cleanup will be handled by main processing loop
                    }
                }

                // Returing a value other than size fails the callback and forces
                // request completion with an error
                return size - 1;
            }

            private static size_t CurlReceiveBodyCallback(
                IntPtr buffer, size_t size, size_t nitems, IntPtr context)
            {
                CurlHandler.VerboseTrace("size: " + size + ", nitems: " + nitems);
                size *= nitems;

                EasyRequest easy;
                if (TryGetEasyRequestFromContext(context, out easy))
                {
                    try
                    {
                        Debug.Assert(easy._paused == EasyRequest.PauseState.Unpaused, "We shouldn't be getting callbacks for a paused request.");
                        if (!(easy.Task.IsCanceled || easy.Task.IsFaulted))
                        {
                            // Complete the task if it hasn't already been.  This will make the
                            // stream available to consumers.  A previous write callback
                            // may have already completed the task to publish the response.
                            easy.EnsureResponseMessagePublished();

                            // Try to transfer the data to a reader.  This will return either the
                            // amount of data transferred (equal to the amount requested
                            // to be transferred), or it will return a pause request.
                            return easy._responseMessage.ResponseStream.TransferDataToStream(buffer, (long)size);
                        }
                    }
                    catch (Exception ex)
                    {
                        easy.FailRequest(ex); // cleanup will be handled by main processing loop
                    }
                }

                // Returing a value other than size fails the callback and forces
                // request completion with an error.
                CurlHandler.VerboseTrace("Error: returning a bad size to abort the request");
                return (size > 0) ? size - 1 : 1;
            }

            private static size_t CurlSendCallback(IntPtr buffer, size_t size, size_t nitems, IntPtr context)
            {
                CurlHandler.VerboseTrace("size: " + size + ", nitems: " + nitems);
                size *= nitems;
                if (size == 0)
                {
                    return 0;
                }

                EasyRequest easy;
                if (TryGetEasyRequestFromContext(context, out easy))
                {
                    Debug.Assert(easy._requestContentStream != null, "We should only be in the send callback if we have a request content stream");
                    Debug.Assert(easy._associatedMultiAgent != null, "The request should be associated with a multi agent.");

                    // Transfer data from the request's content stream to libcurl
                    try
                    {
                        byte[] arr = easy._associatedMultiAgent._transferBuffer;
                        int numBytes = easy._requestContentStream.Read(arr, 0, Math.Min(arr.Length, (int)size)); // TODO: Fix potential blocking here
                        Debug.Assert(numBytes >= 0 && (ulong)numBytes <= size, "Read more bytes than requested");
                        if (numBytes > 0)
                        {
                            Marshal.Copy(arr, 0, buffer, numBytes);
                        }

                        return (size_t)numBytes;
                    }
                    catch (Exception ex)
                    {
                        easy.FailRequest(ex); // cleanup will be handled by main processing loop
                    }
                }

                // Something went wrong.
                return CURLcode.CURLE_ABORTED_BY_CALLBACK;
            }

            private static int CurlSeekCallback(IntPtr context, long offset, int origin)
            {
                CurlHandler.VerboseTrace("offset: " + offset + ", origin: " + origin);
                EasyRequest easy;
                if (TryGetEasyRequestFromContext(context, out easy))
                {
                    try
                    {
                        if (easy._requestContentStream.CanSeek)
                        {
                            easy._requestContentStream.Seek(offset, (SeekOrigin)origin);
                            return Interop.libcurl.CURL_SEEKFUNC.CURL_SEEKFUNC_OK;
                        }
                        else
                        {
                            return Interop.libcurl.CURL_SEEKFUNC.CURL_SEEKFUNC_CANTSEEK;
                        }
                    }
                    catch (Exception ex)
                    {
                        easy.FailRequest(ex); // cleanup will be handled by main processing loop
                    }
                }

                // Something went wrong
                return Interop.libcurl.CURL_SEEKFUNC.CURL_SEEKFUNC_FAIL;
            }

            private static bool TryGetEasyRequestFromContext(IntPtr context, out EasyRequest easy)
            {
                // Get the EasyRequest from the context
                try
                {
                    GCHandle handle = GCHandle.FromIntPtr(context);
                    easy = (EasyRequest)handle.Target;
                    Debug.Assert(easy != null, "Expected non-null EasyRequest in GCHandle");
                    return easy != null;
                }
                catch (InvalidCastException)
                {
                    Debug.Fail("EasyRequest wasn't the GCHandle's Target");
                }
                catch (InvalidOperationException)
                {
                    Debug.Fail("Invalid GCHandle");
                }

                easy = null;
                return false;
            }

            private static void SetCurlCallbacks(EasyRequest easy, IntPtr easyGCHandle)
            {
                // Add callback for processing headers
                easy.SetCurlOption(CURLoption.CURLOPT_HEADERFUNCTION, s_receiveHeadersCallback);
                easy.SetCurlOption(CURLoption.CURLOPT_HEADERDATA, easyGCHandle);

                // If we're sending data as part of the request, add callbacks for sending request data
                if (easy._requestMessage.Content != null)
                {
                    easy.SetCurlOption(CURLoption.CURLOPT_READFUNCTION, s_sendCallback);
                    easy.SetCurlOption(CURLoption.CURLOPT_READDATA, easyGCHandle);

                    easy.SetCurlOption(CURLoption.CURLOPT_SEEKFUNCTION, s_seekCallback);
                    easy.SetCurlOption(CURLoption.CURLOPT_SEEKDATA, easyGCHandle);
                }

                // If we're expecting any data in response, add a callback for receiving body data
                if (easy._requestMessage.Method != HttpMethod.Head)
                {
                    easy.SetCurlOption(CURLoption.CURLOPT_WRITEFUNCTION, s_receiveBodyCallback);
                    easy.SetCurlOption(CURLoption.CURLOPT_WRITEDATA, easyGCHandle);
                }
            }

            [Conditional(VerboseDebuggingConditional)]
            private void VerboseTrace(string text = null, [CallerMemberName] string memberName = null, EasyRequest easy = null)
            {
                CurlHandler.VerboseTrace(text, memberName, easy, agent: this);
            }


            /// <summary>Represents an active request currently being processed by the agent.</summary>
            private struct ActiveRequest
            {
                public EasyRequest Easy;
                public CancellationTokenRegistration CancellationRegistration;
            }

            /// <summary>Represents an incoming request to be processed by the agent.</summary>
            internal struct IncomingRequest
            {
                public IncomingRequestType Type;
                public EasyRequest Easy;
            }

            /// <summary>The type of an incoming request to be processed by the agent.</summary>
            internal enum IncomingRequestType
            {
                /// <summary>A new request that's never been submitted to an agent.</summary>
                New,
                /// <summary>A request to cancel a request previously submitted to the agent.</summary>
                Cancel,
                /// <summary>A request to unpause the connection associated with a request previously submitted to the agent.</summary>
                Unpause
            }
        }

    }
}
