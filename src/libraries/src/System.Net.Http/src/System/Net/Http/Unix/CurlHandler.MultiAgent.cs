// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

// TODO: Once we upgrade to C# 6, remove all of these and simply import the Http class.
using CURLAUTH = Interop.Http.CURLAUTH;
using CURLcode = Interop.Http.CURLcode;
using CURLINFO = Interop.Http.CURLINFO;
using CURLMcode = Interop.Http.CURLMcode;
using CURLMSG = Interop.Http.CURLMSG;
using CURLoption = Interop.Http.CURLoption;
using SafeCurlMultiHandle = Interop.Http.SafeCurlMultiHandle;
using CurlSeekResult = Interop.Http.CurlSeekResult;

namespace System.Net.Http
{
    internal partial class CurlHandler : HttpMessageHandler
    {
        /// <summary>Provides a multi handle and the associated processing for all requests on the handle.</summary>
        private sealed class MultiAgent
        {
            private static readonly Interop.Http.ReadWriteCallback s_receiveHeadersCallback = CurlReceiveHeadersCallback;
            private static readonly Interop.Http.ReadWriteCallback s_sendCallback = CurlSendCallback;
            private static readonly Interop.Http.SeekCallback s_seekCallback = CurlSeekCallback;
            private static readonly Interop.Http.ReadWriteCallback s_receiveBodyCallback = CurlReceiveBodyCallback;
            private static readonly Interop.Http.DebugCallback s_debugCallback = CurlDebugFunction;

            /// <summary>
            /// A collection of not-yet-processed incoming requests for work to be done
            /// by this multi agent.  This can include making new requests, canceling
            /// active requests, or unpausing active requests.
            /// Protected by a lock on <see cref="_incomingRequests"/>.
            /// </summary>
            private readonly Queue<IncomingRequest> _incomingRequests = new Queue<IncomingRequest>();
         
            /// <summary>Map of activeOperations, indexed by a GCHandle ptr.</summary>
            private readonly Dictionary<IntPtr, ActiveRequest> _activeOperations = new Dictionary<IntPtr, ActiveRequest>();

            /// <summary>
            /// Special file descriptor used to wake-up curl_multi_wait calls.  This is the read
            /// end of a pipe, with the write end written to when work is queued or when cancellation
            /// is requested. This is only valid while the worker is executing.
            /// </summary>
            private SafeFileHandle _wakeupRequestedPipeFd;

            /// <summary>
            /// Write end of the pipe connected to <see cref="_wakeupRequestedPipeFd"/>.
            /// This is only valid while the worker is executing.
            /// </summary>
            private SafeFileHandle _requestWakeupPipeFd;
            
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
                    EventSourceTrace("MultiAgent worker queueing");

                    // Create pipe used to forcefully wake up curl_multi_wait calls when something important changes.
                    // This is created here rather than in Process so that the pipe is available immediately
                    // for subsequent queue calls to use.
                    Debug.Assert(_wakeupRequestedPipeFd == null, "Read pipe should have been cleared");
                    Debug.Assert(_requestWakeupPipeFd == null, "Write pipe should have been cleared");
                    unsafe
                    {
                        int* fds = stackalloc int[2];
                        Interop.CheckIo(Interop.Sys.Pipe(fds));
                        _wakeupRequestedPipeFd = new SafeFileHandle((IntPtr)fds[Interop.Sys.ReadEndOfPipe], true);
                        _requestWakeupPipeFd = new SafeFileHandle((IntPtr)fds[Interop.Sys.WriteEndOfPipe], true);
                    }

                    // Kick off the processing task.  It's "DenyChildAttach" to avoid any surprises if
                    // code happens to create attached tasks, and it's LongRunning because this thread
                    // is likely going to sit around for a while in a wait loop (and the more requests
                    // are concurrently issued to the same agent, the longer the thread will be around).
                    const TaskCreationOptions Options = TaskCreationOptions.DenyChildAttach | TaskCreationOptions.LongRunning;
                    _runningWorker = new Task(s =>
                    {
                        var thisRef = (MultiAgent)s;
                        try
                        {
                            // Do the actual processing
                            thisRef.EventSourceTrace("MultiAgent worker running");
                            thisRef.WorkerLoop();
                        }
                        catch (Exception exc)
                        {
                            thisRef.EventSourceTrace("Unexpected worker failure: {0}", exc);
                            Debug.Fail("Unexpected exception from processing loop: " + exc.ToString());
                        }
                        finally
                        {
                            thisRef.EventSourceTrace("MultiAgent worker shutting down");
                            lock (thisRef._incomingRequests)
                            {
                                // Close our wakeup pipe (ignore close errors).
                                // This is done while holding the lock to prevent
                                // subsequent Queue calls to see an improperly configured
                                // set of descriptors.
                                thisRef._wakeupRequestedPipeFd.Dispose();
                                thisRef._wakeupRequestedPipeFd = null;
                                thisRef._requestWakeupPipeFd.Dispose();
                                thisRef._requestWakeupPipeFd = null;

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
                    EventSourceTrace(null);
                    byte b = 1;
                    Interop.CheckIo(Interop.Sys.Write(_requestWakeupPipeFd, &b, 1));
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
                Interop.CheckIo(Interop.Sys.Read(_wakeupRequestedPipeFd, clearBuf, ClearBufferSize));
            }

            /// <summary>Requests that libcurl unpause the connection associated with this request.</summary>
            internal void RequestUnpause(EasyRequest easy)
            {
                EventSourceTrace(null, easy: easy);
                Queue(new IncomingRequest { Easy = easy, Type = IncomingRequestType.Unpause });
            }

            /// <summary>Creates and configures a new multi handle.</summary>
            private SafeCurlMultiHandle CreateAndConfigureMultiHandle()
            {
                // Create the new handle
                SafeCurlMultiHandle multiHandle = Interop.Http.MultiCreate();
                if (multiHandle.IsInvalid)
                {
                    throw CreateHttpRequestException(new CurlException((int)CURLcode.CURLE_FAILED_INIT, isMulti: false));
                }

                // In support of HTTP/2, enable HTTP/2 connections to be multiplexed if possible.
                // We must only do this if the version of libcurl being used supports HTTP/2 multiplexing.
                // Due to a change in a libcurl signature, if we try to make this call on an older libcurl, 
                // we'll end up accidentally and unconditionally enabling HTTP 1.1 pipelining.
                if (s_supportsHttp2Multiplexing)
                {
                    ThrowIfCURLMError(Interop.Http.MultiSetOptionLong(multiHandle,
                        Interop.Http.CURLMoption.CURLMOPT_PIPELINING,
                        (long)Interop.Http.CurlPipe.CURLPIPE_MULTIPLEX));
                }
                
                return multiHandle;
            }

            private void WorkerLoop()
            {
                Debug.Assert(!Monitor.IsEntered(_incomingRequests), "No locks should be held while invoking Process");
                Debug.Assert(_runningWorker != null && _runningWorker.Id == Task.CurrentId, "This is the worker, so it must be running");
                Debug.Assert(_wakeupRequestedPipeFd != null && !_wakeupRequestedPipeFd.IsInvalid, "Should have a valid pipe for wake ups");

                // Create the multi handle to use for this round of processing.  This one handle will be used
                // to service all easy requests currently available and all those that come in while
                // we're processing other requests.  Once the work quiesces and there are no more requests
                // to process, this multi handle will be released as the worker goes away.  The next
                // time a request arrives and a new worker is spun up, a new multi handle will be created.
                SafeCurlMultiHandle multiHandle = CreateAndConfigureMultiHandle();

                // Clear our active operations table.  This should already be clear, either because
                // all previous operations completed without unexpected exception, or in the case of an
                // unexpected exception we should have cleaned up gracefully anyway.  But just in case...
                Debug.Assert(_activeOperations.Count == 0, "We shouldn't have any active operations when starting processing.");
                _activeOperations.Clear();

                Exception eventLoopError = null;
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
                            return;
                        }

                        // We have one or more active operations. Run any work that needs to be run.
                        CURLMcode performResult;
                        while ((performResult = Interop.Http.MultiPerform(multiHandle)) == CURLMcode.CURLM_CALL_MULTI_PERFORM);
                        ThrowIfCURLMError(performResult);

                        // Complete and remove any requests that have finished being processed.
                        CURLMSG message;
                        IntPtr easyHandle;
                        CURLcode result;
                        while (Interop.Http.MultiInfoRead(multiHandle, out message, out easyHandle, out result))
                        {
                            Debug.Assert(message == CURLMSG.CURLMSG_DONE, "CURLMSG_DONE is supposed to be the only message type");

                            if (message == CURLMSG.CURLMSG_DONE)
                            {
                                IntPtr gcHandlePtr;
                                CURLcode getInfoResult = Interop.Http.EasyGetInfoPointer(easyHandle, CURLINFO.CURLINFO_PRIVATE, out gcHandlePtr);
                                Debug.Assert(getInfoResult == CURLcode.CURLE_OK, "Failed to get info on a completing easy handle");
                                if (getInfoResult == CURLcode.CURLE_OK)
                                {
                                    ActiveRequest completedOperation;
                                    bool gotActiveOp = _activeOperations.TryGetValue(gcHandlePtr, out completedOperation);
                                    Debug.Assert(gotActiveOp, "Expected to find GCHandle ptr in active operations table");
                                    if (gotActiveOp)
                                    {
                                        DeactivateActiveRequest(multiHandle, completedOperation.Easy, gcHandlePtr, completedOperation.CancellationRegistration);
                                        FinishRequest(completedOperation.Easy, result);
                                    }
                                }
                            }
                        }

                        // Wait for more things to do.
                        bool isWakeupRequestedPipeActive;
                        bool isTimeout;
                        ThrowIfCURLMError(Interop.Http.MultiWait(multiHandle, _wakeupRequestedPipeFd, out isWakeupRequestedPipeActive, out isTimeout));
                        if (isWakeupRequestedPipeActive)
                        {
                            // We woke up (at least in part) because a wake-up was requested.  
                            // Read the data out of the pipe to clear it.
                            Debug.Assert(!isTimeout, "should not have timed out if isExtraFileDescriptorActive");
                            EventSourceTrace("Wait wake-up");
                            ReadFromWakeupPipeWhenKnownToContainData();
                        }
                        if (isTimeout)
                        {
                            EventSourceTrace("Wait timeout");
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
                catch (Exception exc)
                {
                    eventLoopError = exc;
                    throw;
                }
                finally
                {
                    // If we got an unexpected exception, something very bad happened. We may have some 
                    // operations that we initiated but that weren't completed. Make sure to clean up any 
                    // such operations, failing them and releasing their resources.
                    if (_activeOperations.Count > 0)
                    {
                        Debug.Assert(eventLoopError != null, "We should only have remaining operations if we got an unexpected exception");
                        foreach (KeyValuePair<IntPtr, ActiveRequest> pair in _activeOperations)
                        {
                            ActiveRequest failingOperation = pair.Value;
                            IntPtr failingOperationGcHandle = pair.Key;

                            DeactivateActiveRequest(multiHandle, failingOperation.Easy, failingOperationGcHandle, failingOperation.CancellationRegistration);

                            // Complete the operation's task and clean up any of its resources
                            failingOperation.Easy.FailRequest(CreateHttpRequestException(eventLoopError));
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
                EventSourceTrace("Type: {0}", request.Type, easy: request.Easy);

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

                            CURLcode unpauseResult = Interop.Http.EasyUnpause(easy._easyHandle);
                            try
                            {
                                ThrowIfCURLEError(unpauseResult);
                            }
                            catch (Exception exc)
                            {
                                FindAndFailActiveRequest(multiHandle, easy, exc);
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
                    easy.SetCurlCallbacks(gcHandlePtr, s_receiveHeadersCallback, s_sendCallback, s_seekCallback, s_receiveBodyCallback, s_debugCallback);
                    ThrowIfCURLMError(Interop.Http.MultiAddHandle(multiHandle, easy._easyHandle));
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
                CURLMcode removeResult = Interop.Http.MultiRemoveHandle(multiHandle, easy._easyHandle);
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
                EventSourceTrace("Error: {0}", error, easy: easy);

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

            private void FinishRequest(EasyRequest completedOperation, CURLcode messageResult)
            {
                EventSourceTrace("Curl result: {0}", messageResult, easy: completedOperation);

                if (completedOperation._responseMessage.StatusCode != HttpStatusCode.Unauthorized)
                {
                    // If preauthentication is enabled, then we want to transfer credentials to the handler's credential cache.
                    // That entails asking the easy operation which auth types are supported, and then giving that info to the
                    // handler, which along with the request URI and its server credentials will populate the cache appropriately.
                    if (completedOperation._handler.PreAuthenticate)
                    {
                        long authAvailable;
                        if (Interop.Http.EasyGetInfoLong(completedOperation._easyHandle, CURLINFO.CURLINFO_HTTPAUTH_AVAIL, out authAvailable) == CURLcode.CURLE_OK)
                        {
                            completedOperation._handler.TransferCredentialsToCache(
                                completedOperation._requestMessage.RequestUri, (CURLAUTH)authAvailable);
                        }
                        // Ignore errors: no need to fail for the sake of putting the credentials into the cache
                    }
                }

                // Complete or fail the request
                try
                {
                    bool unsupportedProtocolRedirect = messageResult == CURLcode.CURLE_UNSUPPORTED_PROTOCOL && completedOperation._isRedirect;
                    if (!unsupportedProtocolRedirect)
                    {
                        ThrowIfCURLEError(messageResult);
                    }
                    completedOperation.EnsureResponseMessagePublished();
                }
                catch (Exception exc)
                {
                    completedOperation.FailRequest(exc);
                }

                // At this point, we've completed processing the entire request, either due to error
                // or due to completing the entire response.
                completedOperation.Cleanup();
            }

            private static void CurlDebugFunction(IntPtr curl, Interop.Http.CurlInfoType type, IntPtr data, ulong size, IntPtr context)
            {
                EasyRequest easy;
                TryGetEasyRequestFromContext(context, out easy);

                try
                {
                    switch (type)
                    {
                        case Interop.Http.CurlInfoType.CURLINFO_TEXT:
                        case Interop.Http.CurlInfoType.CURLINFO_HEADER_IN:
                        case Interop.Http.CurlInfoType.CURLINFO_HEADER_OUT:
                            string text = Marshal.PtrToStringAnsi(data, (int)size).Trim();
                            if (text.Length > 0)
                            {
                                CurlHandler.EventSourceTrace("{0}: {1}", type, text, 0, easy: easy);
                            }
                            break;

                        default:
                            CurlHandler.EventSourceTrace("{0}: {1} bytes", type, size, 0, easy: easy);
                            break;
                    }
                }
                catch (Exception exc)
                {
                    CurlHandler.EventSourceTrace("Error: {0}", exc, easy: easy);
                }
            }

            private static ulong CurlReceiveHeadersCallback(IntPtr buffer, ulong size, ulong nitems, IntPtr context)
            {
                // The callback is invoked once per header; multi-line headers get merged into a single line.

                size *= nitems;
                if (size == 0)
                {
                    return 0;
                }

                EasyRequest easy;
                if (TryGetEasyRequestFromContext(context, out easy))
                {
                    CurlHandler.EventSourceTrace("Size: {0}", size, easy: easy);
                    try
                    {
                        HttpResponseMessage response = easy._responseMessage;

                        CurlResponseHeaderReader reader = new CurlResponseHeaderReader(buffer, size);

                        if (reader.ReadStatusLine(response))
                        {
                            // Clear the header if status line is received again. This signifies that there are multiple response headers (like in redirection).
                            response.Headers.Clear();
                            response.Content.Headers.Clear();

                            easy._isRedirect = easy._handler.AutomaticRedirection &&
                                         (response.StatusCode == HttpStatusCode.Redirect ||
                                         response.StatusCode == HttpStatusCode.RedirectKeepVerb ||
                                         response.StatusCode == HttpStatusCode.RedirectMethod);
                        }
                        else
                        {
                            string headerName;
                            string headerValue;

                            if (reader.ReadHeader(out headerName, out headerValue))
                            {
                                if (!response.Headers.TryAddWithoutValidation(headerName, headerValue))
                                {
                                    response.Content.Headers.TryAddWithoutValidation(headerName, headerValue);
                                }
                                else if (easy._isRedirect && string.Equals(headerName, HttpKnownHeaderNames.Location, StringComparison.OrdinalIgnoreCase))
                                {
                                    HandleRedirectLocationHeader(easy, headerValue);
                                }
                                else if (string.Equals(headerName, HttpKnownHeaderNames.SetCookie, StringComparison.OrdinalIgnoreCase))
                                {
                                    easy._handler.AddResponseCookies(easy, headerValue);
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
                CurlHandler.EventSourceTrace("Aborting request", easy: easy);
                return size - 1;
            }

            private static ulong CurlReceiveBodyCallback(
                IntPtr buffer, ulong size, ulong nitems, IntPtr context)
            {
                size *= nitems;

                EasyRequest easy;
                if (TryGetEasyRequestFromContext(context, out easy))
                {
                    CurlHandler.EventSourceTrace("Size: {0}", size, easy: easy);
                    try
                    {
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
                CurlHandler.EventSourceTrace("Aborting request", easy: easy);
                return (size > 0) ? size - 1 : 1;
            }

            private static ulong CurlSendCallback(IntPtr buffer, ulong size, ulong nitems, IntPtr context)
            {
                int length = checked((int)(size * nitems));
                Debug.Assert(length <= RequestBufferSize, "length " + length + " should not be larger than RequestBufferSize " + RequestBufferSize);
                if (length == 0)
                {
                    return 0;
                }

                EasyRequest easy;
                if (TryGetEasyRequestFromContext(context, out easy))
                {
                    CurlHandler.EventSourceTrace("Size: {0}", length, easy: easy);
                    Debug.Assert(easy._requestContentStream != null, "We should only be in the send callback if we have a request content stream");
                    Debug.Assert(easy._associatedMultiAgent != null, "The request should be associated with a multi agent.");

                    try
                    {
                        // Transfer data from the request's content stream to libcurl
                        return TransferDataFromRequestStream(buffer, length, easy);
                    }
                    catch (Exception ex)
                    {
                        easy.FailRequest(ex); // cleanup will be handled by main processing loop
                    }
                }

                // Something went wrong.
                CurlHandler.EventSourceTrace("Aborting request", easy: easy);
                return Interop.Http.CURL_READFUNC_ABORT;
            }

            /// <summary>
            /// Transfers up to <paramref name="length"/> data from the <paramref name="easy"/>'s
            /// request content (non-memory) stream to the buffer.
            /// </summary>
            /// <returns>The number of bytes transferred.</returns>
            private static ulong TransferDataFromRequestStream(IntPtr buffer, int length, EasyRequest easy)
            {
                CurlHandler.EventSourceTrace("Length: {0}", length, easy: easy);

                MultiAgent multi = easy._associatedMultiAgent;

                // First check to see whether there's any data available from a previous asynchronous read request.
                // If there is, the transfer state's Task field will be non-null, with its Result representing
                // the number of bytes read.  The Buffer will then contain all of that read data.  If the Count
                // is 0, then this is the first time we're checking that Task, and so we populate the Count
                // from that read result.  After that, we can transfer as much data remains between Offset and
                // Count.  Multiple callbacks may pull from that one read.

                EasyRequest.SendTransferState sts = easy._sendTransferState;
                if (sts != null)
                {
                    // Is there a previous read that may still have data to be consumed?
                    if (sts._task != null)
                    {
                        if (!sts._task.IsCompleted)
                        {
                            // We have a previous read that's not yet completed.  This should be quite rare, but it can
                            // happen when we're unpaused prematurely, potentially due to the request still finishing
                            // being sent as the server starts to send a response.  Since we still have the outstanding
                            // read, we simply re-pause.  When the task completes (which could have happened immediately
                            // after the check). the continuation we previously created will fire and queue an unpause.
                            // Since all of this processing is single-threaded on the current thread, that unpause request 
                            // is guaranteed to happen after this re-pause.
                            multi.EventSourceTrace("Re-pausing reading after a spurious un-pause", easy: easy);
                            return Interop.Http.CURL_READFUNC_PAUSE;
                        }

                        // Determine how many bytes were read on the last asynchronous read.
                        // If nothing was read, then we're done and can simply return 0 to indicate
                        // the end of the stream.
                        int bytesRead = sts._task.GetAwaiter().GetResult(); // will throw if read failed
                        Debug.Assert(bytesRead >= 0 && bytesRead <= sts._buffer.Length, "ReadAsync returned an invalid result length: " + bytesRead);
                        if (bytesRead == 0)
                        {
                            sts.SetTaskOffsetCount(null, 0, 0);
                            return 0;
                        }

                        // If Count is still 0, then this is the first time after the task completed
                        // that we're examining the data: transfer the bytesRead to the Count.
                        if (sts._count == 0)
                        {
                            multi.EventSourceTrace("ReadAsync completed with bytes: {0}", bytesRead, easy: easy);
                            sts._count = bytesRead;
                        }

                        // Now Offset and Count are both accurate.  Determine how much data we can copy to libcurl...
                        int availableData = sts._count - sts._offset;
                        Debug.Assert(availableData > 0, "There must be some data still available.");

                        // ... and copy as much of that as libcurl will allow.
                        int bytesToCopy = Math.Min(availableData, length);
                        Marshal.Copy(sts._buffer, sts._offset, buffer, bytesToCopy);
                        multi.EventSourceTrace("Copied {0} bytes from request stream", bytesToCopy, easy: easy);

                        // Update the offset.  If we've gone through all of the data, reset the state 
                        // so that the next time we're called back we'll do a new read.
                        sts._offset += bytesToCopy;
                        Debug.Assert(sts._offset <= sts._count, "Offset should never exceed count");
                        if (sts._offset == sts._count)
                        {
                            sts.SetTaskOffsetCount(null, 0, 0);
                        }

                        // Return the amount of data copied
                        Debug.Assert(bytesToCopy > 0, "We should never return 0 bytes here.");
                        return (ulong)bytesToCopy;
                    }

                    // sts was non-null but sts.Task was null, meaning there was no previous task/data
                    // from which to satisfy any of this request.
                }
                else // sts == null
                {
                    // Allocate a transfer state object to use for the remainder of this request.
                    easy._sendTransferState = sts = new EasyRequest.SendTransferState();
                }

                Debug.Assert(sts != null, "By this point we should have a transfer object");
                Debug.Assert(sts._task == null, "There shouldn't be a task now.");
                Debug.Assert(sts._count == 0, "Count should be zero.");
                Debug.Assert(sts._offset == 0, "Offset should be zero.");

                // If we get here, there was no previously read data available to copy.
                // Initiate a new asynchronous read.
                Task<int> asyncRead = easy._requestContentStream.ReadAsyncInternal(
                    sts._buffer, 0, Math.Min(sts._buffer.Length, length), easy._cancellationToken);
                Debug.Assert(asyncRead != null, "Badly implemented stream returned a null task from ReadAsync");

                // Even though it's "Async", it's possible this read could complete synchronously or extremely quickly.  
                // Check to see if it did, in which case we can also satisfy the libcurl request synchronously in this callback.
                if (asyncRead.IsCompleted)
                {
                    // Get the amount of data read.
                    int bytesRead = asyncRead.GetAwaiter().GetResult(); // will throw if read failed
                    if (bytesRead == 0)
                    {
                        return 0;
                    }

                    // Copy as much as we can.
                    int bytesToCopy = Math.Min(bytesRead, length);
                    Debug.Assert(bytesToCopy > 0 && bytesToCopy <= sts._buffer.Length, "ReadAsync quickly returned an invalid result length: " + bytesToCopy);
                    Marshal.Copy(sts._buffer, 0, buffer, bytesToCopy);

                    // If we read more than we were able to copy, stash it away for the next read.
                    if (bytesToCopy < bytesRead)
                    {
                        sts.SetTaskOffsetCount(asyncRead, bytesToCopy, bytesRead);
                    }

                    // Return the number of bytes read.
                    return (ulong)bytesToCopy;
                }

                // Otherwise, the read completed asynchronously.  Store the task, and hook up a continuation 
                // such that the connection will be unpaused once the task completes.
                sts.SetTaskOffsetCount(asyncRead, 0, 0);
                asyncRead.ContinueWith((t, s) =>
                {
                    EasyRequest easyRef = (EasyRequest)s;
                    easyRef._associatedMultiAgent.RequestUnpause(easyRef);
                }, easy, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

                // Then pause the connection.
                multi.EventSourceTrace("Pausing the connection.", easy: easy);
                return Interop.Http.CURL_READFUNC_PAUSE;
            }

            private static CurlSeekResult CurlSeekCallback(IntPtr context, long offset, int origin)
            {
                CurlHandler.EventSourceTrace("Offset: {0}, Origin: {1}", offset, origin, 0);
                EasyRequest easy;
                if (TryGetEasyRequestFromContext(context, out easy))
                {
                    try
                    {
                        // If libcul is requesting we seek back to the beginning and if the request
                        // content stream is in a position to reset itself, reset and let libcurl
                        // know we did the seek; otherwise, let it know we can't seek.
                        if (offset == 0 && origin == (int)SeekOrigin.Begin && 
                            easy._requestContentStream != null && easy._requestContentStream.TryReset())
                        {
                            // Dump any state associated with the old stream's position
                            if (easy._sendTransferState != null)
                            {
                                easy._sendTransferState.SetTaskOffsetCount(null, 0, 0);
                            }

                            // Restart the transfer
                            easy._requestContentStream.Run();

                            return CurlSeekResult.CURL_SEEKFUNC_OK;
                        }
                        else
                        {
                            return CurlSeekResult.CURL_SEEKFUNC_CANTSEEK;
                        }
                    }
                    catch (Exception ex)
                    {
                        easy.FailRequest(ex); // cleanup will be handled by main processing loop
                    }
                }

                // Something went wrong
                return CurlSeekResult.CURL_SEEKFUNC_FAIL;
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

            private void EventSourceTrace<TArg0>(string formatMessage, TArg0 arg0, EasyRequest easy = null, [CallerMemberName] string memberName = null)
            {
                CurlHandler.EventSourceTrace(formatMessage, arg0, this, easy, memberName);
            }

            private void EventSourceTrace(string message, EasyRequest easy = null, [CallerMemberName] string memberName = null)
            {
                CurlHandler.EventSourceTrace(message, this, easy, memberName);
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
            internal enum IncomingRequestType : byte
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
