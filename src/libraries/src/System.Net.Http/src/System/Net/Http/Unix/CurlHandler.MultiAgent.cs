// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

using CURLcode = Interop.Http.CURLcode;
using CURLMcode = Interop.Http.CURLMcode;
using CURLINFO = Interop.Http.CURLINFO;

namespace System.Net.Http
{
    internal partial class CurlHandler : HttpMessageHandler
    {
        /// <summary>Provides a multi handle and the associated processing for all requests on the handle.</summary>
        private sealed class MultiAgent : IDisposable
        {
            /// <summary>
            /// Amount of time in milliseconds to keep a multiagent worker alive when there's no work to be done.
            /// Increasing this value makes it more likely that a worker will end up getting reused for subsequent
            /// requests, saving on the costs associated with spinning up a new worker, but at the same time it
            /// burns a thread for that period of time.
            /// </summary>
            private const int KeepAliveMilliseconds = 50;

            private static readonly Interop.Http.ReadWriteCallback s_receiveHeadersCallback = CurlReceiveHeadersCallback;
            private static readonly Interop.Http.ReadWriteCallback s_sendCallback = CurlSendCallback;
            private static readonly Interop.Http.SeekCallback s_seekCallback = CurlSeekCallback;
            private static readonly Interop.Http.ReadWriteCallback s_receiveBodyCallback = CurlReceiveBodyCallback;
            private static readonly Interop.Http.DebugCallback s_debugCallback = CurlDebugFunction;

            /// <summary>CurlHandler that created this MultiAgent.  If null, this is a shared handler.</summary>
            private readonly CurlHandler _creatingHandler;

            /// <summary>
            /// A collection of not-yet-processed incoming requests for work to be done
            /// by this multi agent.  This can include making new requests, canceling
            /// active requests, or unpausing active requests.
            /// Protected by a lock on <see cref="_incomingRequests"/>.
            /// </summary>
            private readonly Queue<IncomingRequest> _incomingRequests = new Queue<IncomingRequest>();

            /// <summary>Map of activeOperations, indexed by a GCHandle to a StrongToWeakReference{EasyRequest}.</summary>
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

            /// <summary>
            /// Multi handle used to service all requests on this agent.  It's lazily
            /// created when it's first needed, so that it can utilize all of the settings
            /// from the associated handler, and it's kept open for the duration of this
            /// agent so that all of the resources it pools (connection pool, DNS cache, etc.)
            /// can be used for all requests on this agent.
            /// </summary>
            private Interop.Http.SafeCurlMultiHandle _multiHandle;

            /// <summary>Set when Dispose has been called.</summary>
            private bool _disposed;

            /// <summary>Initializes the MultiAgent.</summary>
            /// <param name="handler">The handler that created this agent, or null if it's shared.</param>
            public MultiAgent(CurlHandler handler)
            {
                _creatingHandler = handler;
            }

            /// <summary>Disposes of the agent.</summary>
            public void Dispose()
            {
                EventSourceTrace(null);
                _disposed = true;
                QueueIfRunning(new IncomingRequest { Type = IncomingRequestType.Shutdown });
                _multiHandle?.Dispose();
            }

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

            /// <summary>Queues a request for the multi handle to process, but only if there's already an active worker running.</summary>
            public void QueueIfRunning(IncomingRequest request)
            {
                lock (_incomingRequests)
                {
                    if (_runningWorker != null)
                    {
                        _incomingRequests.Enqueue(request);
                        if (_incomingRequests.Count == 1)
                        {
                            RequestWakeup();
                        }
                    }
                }
            }

            /// <summary>Gets the ID of the currently running worker, or null if there isn't one.</summary>
            internal int? RunningWorkerId => _runningWorker?.Id;

            /// <summary>Schedules the processing worker if one hasn't already been scheduled.</summary>
            private void EnsureWorkerIsRunning()
            {
                Debug.Assert(Monitor.IsEntered(_incomingRequests), "Needs to be called under _incomingRequests lock");

                if (_runningWorker == null)
                {
                    EventSourceTrace("MultiAgent worker queueing");

                    // Ensure we've created the multi handle for this agent.
                    if (_multiHandle == null)
                    {
                        _multiHandle = CreateAndConfigureMultiHandle();
                    }

                    // Create pipe used to forcefully wake up curl_multi_wait calls when something important changes.
                    // This is created here so that the pipe is available immediately for subsequent queue calls to use.
                    Debug.Assert(_wakeupRequestedPipeFd == null, "Read pipe should have been cleared");
                    Debug.Assert(_requestWakeupPipeFd == null, "Write pipe should have been cleared");
                    unsafe
                    {
                        int* fds = stackalloc int[2];
                        Interop.CheckIo(Interop.Sys.Pipe(fds));
                        _wakeupRequestedPipeFd = new SafeFileHandle((IntPtr)fds[Interop.Sys.ReadEndOfPipe], true);
                        _requestWakeupPipeFd = new SafeFileHandle((IntPtr)fds[Interop.Sys.WriteEndOfPipe], true);
                    }

                    // Create the processing task.  It's "DenyChildAttach" to avoid any surprises if
                    // code happens to create attached tasks, and it's LongRunning because this thread
                    // is likely going to sit around for a while in a wait loop (and the more requests
                    // are concurrently issued to the same agent, the longer the thread will be around).
                    _runningWorker = new Task(s => ((MultiAgent)s).WorkerBody(), this, 
                        CancellationToken.None, TaskCreationOptions.DenyChildAttach | TaskCreationOptions.LongRunning);

                    // We want to avoid situations where a Dispose occurs while we're in the middle
                    // of processing requests and causes us to tear out the multi handle while it's
                    // in active use.  To avoid that, we add-ref it here, and release it at the end
                    // of the worker loop.
                    bool ignored = false;
                    _multiHandle.DangerousAddRef(ref ignored);

                    // Kick off the processing task.  This is done after both setting _runningWorker
                    // to non-null and add-refing the handle, both to avoid race conditions.  The worker
                    // body needs to see _runningWorker as non-null and assumes that it's free to use
                    // the multi handle, without fear of it having been disposed.
                    _runningWorker.Start(TaskScheduler.Default);
                }
                else // _runningWorker != null
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
            private unsafe void RequestWakeup()
            {
                EventSourceTrace(null);
                byte b = 1;
                Interop.CheckIo(Interop.Sys.Write(_requestWakeupPipeFd, &b, 1));
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
                QueueIfRunning(new IncomingRequest { Easy = easy, Type = IncomingRequestType.Unpause });
            }

            /// <summary>Requests that the request associated with the easy operation be canceled.</summary>
            internal void RequestCancel(EasyRequest easy)
            {
                EventSourceTrace(null, easy: easy);
                QueueIfRunning(new IncomingRequest { Easy = easy, Type = IncomingRequestType.Cancel });
            }

            /// <summary>Creates and configures a new multi handle.</summary>
            private Interop.Http.SafeCurlMultiHandle CreateAndConfigureMultiHandle()
            {
                // Create the new handle
                Interop.Http.SafeCurlMultiHandle multiHandle = Interop.Http.MultiCreate();
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
                    EventSourceTrace("Set multiplexing on multi handle");
                }

                // Configure max connections per host if it was changed from the default.  In shared mode,
                // this will be pulled from the handler that first created the agent; the setting from subsequent
                // handlers that use this will be ignored.
                if (_creatingHandler != null)
                {
                    int maxConnections = _creatingHandler.MaxConnectionsPerServer;
                    if (maxConnections < int.MaxValue) // int.MaxValue considered infinite, mapping to libcurl default of 0
                    {
                        CURLMcode code = Interop.Http.MultiSetOptionLong(multiHandle, Interop.Http.CURLMoption.CURLMOPT_MAX_HOST_CONNECTIONS, maxConnections);
                        switch (code)
                        {
                            case CURLMcode.CURLM_OK:
                                EventSourceTrace("Set max host connections to {0}", maxConnections);
                                break;
                            default:
                                // Treat failures as non-fatal in release; worst case is we employ more connections than desired.
                                EventSourceTrace("Setting CURLMOPT_MAX_HOST_CONNECTIONS failed: {0}. Ignoring option.", code);
                                break;
                        }
                    }
                }

                return multiHandle;
            }

            /// <summary>Thread work item entrypoint for a multiagent worker.</summary>
            private void WorkerBody()
            {
                Debug.Assert(!Monitor.IsEntered(_incomingRequests), $"No locks should be held while invoking {nameof(WorkerBody)}");
                Debug.Assert(_runningWorker != null && _runningWorker.Id == Task.CurrentId, "This is the worker, so it must be running");

                EventSourceTrace("MultiAgent worker running");
                try
                {
                    try
                    {
                        // Do the actual processing
                        WorkerBodyLoop();
                    }
                    finally
                    {
                        EventSourceTrace("MultiAgent worker shutting down");

                        // The multi handle's reference count was increased prior to launching
                        // this processing task.  Release that reference; any Dispose operations
                        // that occurred during the worker's processing will now be allowed to
                        // proceed to clean up the multi handle.
                        _multiHandle.DangerousRelease();

                        lock (_incomingRequests)
                        {
                            // Close our wakeup pipe (ignore close errors).
                            // This is done while holding the lock to prevent
                            // subsequent Queue calls to see an improperly configured
                            // set of descriptors.
                            _wakeupRequestedPipeFd.Dispose();
                            _wakeupRequestedPipeFd = null;
                            _requestWakeupPipeFd.Dispose();
                            _requestWakeupPipeFd = null;

                            // In the time between we stopped processing and taking the lock,
                            // more requests could have been added.  If they were,
                            // kick off another processing loop.
                            _runningWorker = null;
                            if (_incomingRequests.Count > 0 && !_disposed)
                            {
                                EnsureWorkerIsRunning();
                            }
                        }
                    }
                }
                catch (Exception exc)
                {
                    // Something went very wrong.  In general this should not happen.  The only time it might reasonably
                    // happen is if CurlHandler is disposed of while it's actively processing, in which case we could
                    // get an ObjectDisposedException.
                    EventSourceTrace("Unexpected worker failure: {0}", exc);
                    Debug.Assert(exc is ObjectDisposedException, $"Unexpected exception from processing loop: {exc}");

                    // At this point if there any queued requests but there's no worker,
                    // those queued requests are potentially going to sit there waiting forever,
                    // resulting in a hang. Instead, fail those requests.
                    lock (_incomingRequests)
                    {
                        if (_runningWorker == null)
                        {
                            while (_incomingRequests.Count > 0)
                            {
                                _incomingRequests.Dequeue().Easy.CleanupAndFailRequest(exc);
                            }
                        }
                    }
                }
            }

            /// <summary>Main processing loop employed by the multiagent worker body.</summary>
            private void WorkerBodyLoop()
            {
                Debug.Assert(_wakeupRequestedPipeFd != null, "Should have a non-null pipe for wake ups");
                Debug.Assert(!_wakeupRequestedPipeFd.IsInvalid, "Should have a valid pipe for wake ups");
                Debug.Assert(!_wakeupRequestedPipeFd.IsClosed, "Should have an open pipe for wake ups");

                Debug.Assert(_multiHandle != null, "Should have a non-null multi handle");
                Debug.Assert(!_multiHandle.IsInvalid, "Should have a valid multi handle");
                Debug.Assert(!_multiHandle.IsClosed, "Should have an open multi handle");

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
                        // (This method is factored out mostly to keep this loop concise, but also partly 
                        // to avoid keeping any references to EasyRequests rooted by the stack and thus 
                        // preventing them from being GC'd and the response stream finalized.  That's mainly
                        // a concern for debug builds, where the JIT may extend a local's lifetime.  The same 
                        // logic applies to some of the other helpers used later in this loop.)
                        HandleIncomingRequests();

                        // If we have no active operations, there's no work to do right now.
                        if (_activeOperations.Count == 0)
                        {
                            // Setting up a MultiAgent processing loop involves creating a new MultiAgent, creating a task
                            // and a thread to process it, creating a new pipe, etc., which has non-trivial cost associated 
                            // with it.  Thus, to avoid repeatedly spinning up and down these workers, we can keep this worker 
                            // alive for a little while longer in case another request comes in within some reasonably small 
                            // amount of time.  This helps with the case where a sequence of requests is made serially rather 
                            // than in parallel, avoiding the likely case of spinning up a new multiagent for each.
                            Interop.Sys.PollEvents triggered;
                            Interop.Error pollRv = Interop.Sys.Poll(_wakeupRequestedPipeFd, Interop.Sys.PollEvents.POLLIN, KeepAliveMilliseconds, out triggered);
                            if (pollRv == Interop.Error.SUCCESS && (triggered & Interop.Sys.PollEvents.POLLIN) != 0)
                            {
                                // Another request came in while we were waiting. Clear the pipe and loop around to continue processing.
                                ReadFromWakeupPipeWhenKnownToContainData();
                                continue;
                            }

                            // We're done.  Exit the multiagent.
                            return;
                        }

                        // We have one or more active operations. Run any work that needs to be run.
                        PerformCurlWork();

                        // Complete and remove any requests that have finished being processed.
                        Interop.Http.CURLMSG message;
                        IntPtr easyHandle;
                        CURLcode result;
                        while (Interop.Http.MultiInfoRead(_multiHandle, out message, out easyHandle, out result))
                        {
                            HandleCurlMessage(message, easyHandle, result);
                        }

                        // If there are any active operations, wait for more things to do.
                        if (_activeOperations.Count > 0)
                        {
                            WaitForWork();
                        }
                    }
                }
                catch (Exception exc)
                {
                    eventLoopError = exc;
                    throw;
                }
                finally
                {
                    // There may still be active operations, if  an unexpected exception occurred.
                    // Make sure to clean up any remaining operations, failing them and releasing their resources.
                    if (_activeOperations.Count > 0)
                    {
                        CleanUpRemainingActiveOperations(eventLoopError);
                    }
                }
            }

            /// <summary>
            /// Drains the incoming requests queue, dequeuing each request and handling it according to its type.
            /// </summary>
            private void HandleIncomingRequests()
            {
                Debug.Assert(!Monitor.IsEntered(_incomingRequests), "Incoming requests lock should only be held while accessing the queue");
                EventSourceTrace(null);

                while (true)
                {
                    // Get the next request
                    IncomingRequest request;
                    lock (_incomingRequests)
                    {
                        if (_incomingRequests.Count == 0)
                        {
                            return;
                        }

                        request = _incomingRequests.Dequeue();
                    }

                    // Process the request
                    EasyRequest easy = request.Easy;
                    EventSourceTrace("Type: {0}", request.Type, easy: easy);
                    switch (request.Type)
                    {
                        case IncomingRequestType.New:
                            ActivateNewRequest(easy);
                            break;

                        case IncomingRequestType.Cancel:
                            Debug.Assert(easy._associatedMultiAgent == this, "Should only cancel associated easy requests");
                            FindFailAndCleanupActiveRequest(easy, new OperationCanceledException(easy._cancellationToken));
                            break;

                        case IncomingRequestType.Unpause:
                            Debug.Assert(easy._associatedMultiAgent == this, "Should only unpause associated easy requests");
                            if (!easy._easyHandle.IsClosed)
                            {
                                IntPtr gcHandlePtr;
                                ActiveRequest ar;
                                Debug.Assert(FindActiveRequest(easy, out gcHandlePtr, out ar), "Couldn't find active request for unpause");

                                try
                                {
                                    ThrowIfCURLEError(Interop.Http.EasyUnpause(easy._easyHandle));
                                }
                                catch (Exception exc)
                                {
                                    FindFailAndCleanupActiveRequest(easy, exc);
                                }
                            }
                            break;

                        case IncomingRequestType.Shutdown:
                            // When we get a shutdown request, we want to stop all operations that haven't had
                            // their response message published.  Other operations may continue.
                            Debug.Assert(easy == null, "Expected null easy for a Shutdown request");
                            CleanUpRemainingActiveOperations(
                                new OperationCanceledException(SR.net_http_unix_handler_disposed), 
                                onlyIfResponseMessageNotPublished: true);
                            break;

                        default:
                            Debug.Fail("Invalid request type: " + request.Type);
                            break;
                    }
                }
            }

            /// <summary>Tell libcurl to perform any available processing on the easy handles associated with this agent's multi handle.</summary>
            private void PerformCurlWork()
            {
                CURLMcode performResult;
                EventSourceTrace("Ask libcurl to perform any available work...");
                while ((performResult = Interop.Http.MultiPerform(_multiHandle)) == CURLMcode.CURLM_CALL_MULTI_PERFORM) ;
                EventSourceTrace("...done performing work: {0}", performResult);
                ThrowIfCURLMError(performResult);
            }

            /// <summary>
            /// Tell libcurl to block waiting for work to be ready to handle.  It'll return when there's work to be
            /// performed, when a timeout has occurred, or when new requests have entered our incoming requests queue.
            /// </summary>
            private void WaitForWork()
            {
                // Ask libcurl to wait for more things to do.  We pass in our wakeup-requested pipe handle so that libcurl
                // will wait on that file descriptor as well and wake up if an incoming request arrived into our queue.
                bool isWakeupRequestedPipeActive;
                bool isTimeout;
                ThrowIfCURLMError(Interop.Http.MultiWait(_multiHandle, _wakeupRequestedPipeFd, out isWakeupRequestedPipeActive, out isTimeout));

                if (isWakeupRequestedPipeActive)
                {
                    // We woke up (at least in part) because a wake-up was requested.  
                    // Read the data out of the pipe to clear it.
                    Debug.Assert(!isTimeout, $"Should not have timed out when {nameof(isWakeupRequestedPipeActive)} is true");
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

            /// <summary>Handle a libcurl message received as part of processing work.  This should signal a completed operation.</summary>
            private void HandleCurlMessage(Interop.Http.CURLMSG message, IntPtr easyHandle, CURLcode result)
            {
                if (message != Interop.Http.CURLMSG.CURLMSG_DONE)
                {
                    Debug.Fail($"CURLMSG_DONE is supposed to be the only message type, but got {message}");
                    EventSourceTrace("Unexpected CURLMSG: {0}", message);
                    return;
                }

                // Get the GCHandle pointer from the easy handle's state
                IntPtr gcHandlePtr;
                CURLcode getInfoResult = Interop.Http.EasyGetInfoPointer(easyHandle, CURLINFO.CURLINFO_PRIVATE, out gcHandlePtr);
                Debug.Assert(getInfoResult == CURLcode.CURLE_OK, $"Failed to get info on a completing easy handle: {getInfoResult}");
                if (getInfoResult == CURLcode.CURLE_OK)
                {
                    // Use the GCHandle to look up the associated ActiveRequest
                    ActiveRequest completedOperation;
                    bool gotActiveOp = _activeOperations.TryGetValue(gcHandlePtr, out completedOperation);
                    Debug.Assert(gotActiveOp, "Expected to find GCHandle ptr in active operations table");

                    // Deactivate the easy handle and finish all processing related to the request
                    DeactivateActiveRequest(completedOperation, gcHandlePtr);
                    FinishRequest(completedOperation.EasyWrapper, result);
                }
            }

            /// <summary>When shutting down the multi agent worker, ensure any active operations are forcibly completed.</summary>
            /// <param name="error">The error to use to complete any remaining operations.</param>
            /// <param name="onlyIfResponseMessageNotPublished">
            /// true if the only active operations that should be canceled and cleaned up are those which have not
            /// yet had their response message published. false if all active operations should be canceled regardless
            /// of where they are in processing.
            /// </param>
            private void CleanUpRemainingActiveOperations(Exception error, bool onlyIfResponseMessageNotPublished = false)
            {
                EventSourceTrace("Shutting down {0} active operations.", _activeOperations.Count);
                try
                {
                    // Copy the operations to a tmp array so that we don't try to modify the dictionary while enumerating it
                    var activeOps = new KeyValuePair<IntPtr, ActiveRequest>[_activeOperations.Count];
                    ((IDictionary<IntPtr, ActiveRequest>)_activeOperations).CopyTo(activeOps, 0);

                    // Fail all active ops.
                    Exception lastError = null;
                    foreach (KeyValuePair<IntPtr, ActiveRequest> pair in activeOps)
                    {
                        try
                        {
                            IntPtr failingOperationGcHandle = pair.Key;
                            ActiveRequest failingActiveRequest = pair.Value;
                            EasyRequest easy = failingActiveRequest.EasyWrapper.Target; // may be null if the EasyRequest was already collected
                            if (!onlyIfResponseMessageNotPublished || (easy != null && !easy.Task.IsCompleted))
                            {
                                // Deactivate the request, removing it from the multi handle and allowing it to be cleaned up
                                DeactivateActiveRequest(failingActiveRequest, failingOperationGcHandle);

                                // Complete the operation's task and clean up any of its resources, if it still exists.
                                easy?.CleanupAndFailRequest(CreateHttpRequestException(error));
                            }
                        }
                        catch (Exception e)
                        {
                            // We don't want a spurious failure while cleaning up one request to prevent us from trying
                            // to clean up the rest of them.
                            lastError = e;
                        }
                    }

                    // Now propagate any failure that may have occurred while cleaning up
                    if (lastError != null)
                    {
                        ExceptionDispatchInfo.Throw(lastError);
                    }
                }
                finally
                {
                    if (!onlyIfResponseMessageNotPublished)
                    {
                        // Ensure the table is now cleared.
                        _activeOperations.Clear();
                    }
                }
            }

            /// <summary>
            /// Activates the request represented by the EasyRequest.  This includes creating the libcurl easy handle,
            /// configuring it, and associating it with the multi handle so that it may be processed.
            /// </summary>
            private void ActivateNewRequest(EasyRequest easy)
            {
                Debug.Assert(easy != null, "We should never get a null request");
                Debug.Assert(easy._associatedMultiAgent == this, "Request should be associated with this agent");

                // If cancellation has been requested, complete the request proactively
                if (easy._cancellationToken.IsCancellationRequested)
                {
                    easy.CleanupAndFailRequest(new OperationCanceledException(easy._cancellationToken));
                    return;
                }

                // We need to create a GCHandle that we can pass to libcurl to let it keep associated managed
                // state alive and help us to determine which state corresponds to the particular request.  However,
                // having a GCHandle that keeps an EasyRequest alive will prevent finalization of anything to do with
                // that EasyRequest, which means we could end up in a situation where code creates and then drops a
                // request, but then libcurl ends up keeping the state alive (until the reuest/response eventually times
                // out, assuming the timeout wasn't set to infinite).  To address this, we create a GCHandle to a wrapper
                // object.  At first, that wrapper object wraps a strong reference to the EasyRequest, since until a
                // response comes back, the caller doesn't actually have a reference to anything related to the request.
                // Then once a response comes back and the caller is responsible for keeping the request/response alive,
                // we replace the wrapped state with a weak reference to the EasyRequest.  That way, if the user then
                // drops the response, we can allow it to be finalized and not keep it alive indefinitely by the
                // native reference.  Finalization of the response stream will cause all of the relevant state to be
                // closed, including closing out the native easy session and then free'ing this GCHandle.
                easy._selfStrongToWeakReference = new StrongToWeakReference<EasyRequest>(easy); // store wrapper onto the easy so that it can transition it to weak and then lose the ref
                GCHandle gcHandle = GCHandle.Alloc(easy._selfStrongToWeakReference);
                IntPtr gcHandlePtr = GCHandle.ToIntPtr(gcHandle);

                // Configure the easy request and add it to the multi handle.
                bool addedRef = false;
                try
                {
                    easy.InitializeCurl();

                    easy.SetCurlOption(Interop.Http.CURLoption.CURLOPT_PRIVATE, gcHandlePtr);
                    easy.SetCurlCallbacks(gcHandlePtr, s_receiveHeadersCallback, s_sendCallback, s_seekCallback, s_receiveBodyCallback, s_debugCallback);

                    // Make sure that as long as the easy handle is referenced by the multi handle that
                    // it doesn't get finalized.  Doing so can lead to serious problems like seg faults,
                    // for example if the multi handle is trying to access the easy handle on one thread
                    // while it's being finalized on another.
                    easy._easyHandle.DangerousAddRef(ref addedRef);

                    // Finally, register the easy handle with the multi handle
                    ThrowIfCURLMError(Interop.Http.MultiAddHandle(_multiHandle, easy._easyHandle));
                }
                catch (Exception exc)
                {
                    if (addedRef)
                    {
                        easy._easyHandle.DangerousRelease();
                    }
                    gcHandle.Free();
                    easy.CleanupAndFailRequest(exc);
                    return;
                }

                // And if cancellation can be requested, hook up a cancellation callback.
                // This callback will put the easy request back into the queue, which will
                // ensure that a wake-up request has been issued.
                var cancellationReg = default(CancellationTokenRegistration);
                if (easy._cancellationToken.CanBeCanceled)
                {
                    // To avoid keeping the EasyRequest rooted in the associated CancellationTokenSource,
                    // the cancellation registration is given the wrapper rather than the object directly.
                    cancellationReg = easy._cancellationToken.Register(s =>
                    {
                        var wrapper = (StrongToWeakReference<EasyRequest>)s;
                        EasyRequest e = wrapper.Target; // may be null if already collected
                        e?._associatedMultiAgent.RequestCancel(e);
                    }, easy._selfStrongToWeakReference);
                }

                // Finally, add it to our map.
                _activeOperations.Add(gcHandlePtr, new ActiveRequest
                {
                    EasyWrapper = easy._selfStrongToWeakReference,
                    EasyHandle = easy._easyHandle,
                    CancellationRegistration = cancellationReg,
                });
            }

            /// <summary>Extract the EasyRequest from the GCHandle pointer to it.</summary>
            internal static bool TryGetEasyRequestFromGCHandle(IntPtr gcHandlePtr, out EasyRequest easy)
            {
                // Get the EasyRequest from the context
                try
                {
                    GCHandle handle = GCHandle.FromIntPtr(gcHandlePtr);
                    easy = (handle.Target as StrongToWeakReference<EasyRequest>)?.Target;
                    return easy != null;
                }
                catch (Exception e) when (e is InvalidCastException || e is InvalidOperationException)
                {
                    Debug.Fail($"Error accessing GCHandle: {e}");
                }

                easy = null;
                return false;
            }

            /// <summary>
            /// Corresponding to ActivateNewRequest, removes the active request from the multi handle, frees the GCHandle,
            /// removes the request from our tracking table, and ensures cancellation has been unregistered.
            /// </summary>
            private void DeactivateActiveRequest(ActiveRequest activeRequest, IntPtr gcHandlePtr)
            {
                try
                {
                    // Remove the operation from the multi handle so we can shut down the multi handle cleanly
                    CURLMcode removeResult = Interop.Http.MultiRemoveHandle(_multiHandle, activeRequest.EasyHandle);
                    Debug.Assert(removeResult == CURLMcode.CURLM_OK, "Failed to remove easy handle"); // ignore cleanup errors in release

                    // Release the associated GCHandle so that it's not kept alive forever
                    if (gcHandlePtr != IntPtr.Zero)
                    {
                        try
                        {
                            GCHandle.FromIntPtr(gcHandlePtr).Free();
                            bool removed = _activeOperations.Remove(gcHandlePtr);
                            Debug.Assert(removed, "Expected GCHandle to still be referenced by active operations table");
                        }
                        catch (InvalidOperationException)
                        {
                            Debug.Fail("Couldn't get/free the GCHandle for an active operation while shutting down due to failure");
                        }
                    }

                    // Undo cancellation registration
                    activeRequest.CancellationRegistration.Dispose();
                }
                finally
                {
                    // We previously AddRef'd the easy handle to ensure that it wasn't finalized
                    // while it was still registered with the multi handle.  Now that it's been removed,
                    // we need to remove the reference.
                    activeRequest.EasyHandle.DangerousRelease();
                }
            }

            /// <summary>
            /// Looks up an ActiveRequest in the active operations table by EasyRequest.  This is a linear operation
            /// and should not be used on hot paths.
            /// </summary>
            private bool FindActiveRequest(EasyRequest easy, out IntPtr gcHandlePtr, out ActiveRequest activeRequest)
            {
                // We maintain an IntPtr=>ActiveRequest mapping, which makes it cheap to look-up by GCHandle ptr but
                // expensive to look up by EasyRequest.  If we find this becoming a bottleneck, we can add a reverse
                // map that stores the other direction as well.  It should only be used on slow paths, such as when
                // completing an operation due to failure.
                foreach (KeyValuePair<IntPtr, ActiveRequest> pair in _activeOperations)
                {
                    if (pair.Value.EasyWrapper.Target == easy)
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

            /// <summary>
            /// Finds in the active operations table the operation for the specified easy request,
            /// and then assuming it's found, deactivates and fails it with the specified exception.
            /// </summary>
            private void FindFailAndCleanupActiveRequest(EasyRequest easy, Exception error)
            {
                EventSourceTrace("Error: {0}", error, easy: easy);

                IntPtr gcHandlePtr;
                ActiveRequest activeRequest;
                if (FindActiveRequest(easy, out gcHandlePtr, out activeRequest))
                {
                    DeactivateActiveRequest(activeRequest, gcHandlePtr);
                    easy.CleanupAndFailRequest(error);
                }
                else
                {
                    Debug.Assert(easy.Task.IsCompleted, "We should only not be able to find the request if it failed or we started to send back the response.");
                }
            }

            /// <summary>Finishes the processing of a completed easy operation.</summary>
            private void FinishRequest(StrongToWeakReference<EasyRequest> easyWrapper, CURLcode messageResult)
            {
                EasyRequest completedOperation = easyWrapper.Target;
                EventSourceTrace("Curl result: {0}", messageResult, easy: completedOperation);

                if (completedOperation == null)
                {
                    // Already collected; nothing more to do.
                    return;
                }

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
                                completedOperation._requestMessage.RequestUri, (Interop.Http.CURLAUTH)authAvailable);
                        }
                        // Ignore errors: no need to fail for the sake of putting the credentials into the cache
                    }
                }

                // Complete or fail the request
                try
                {
                    // At this point, we've completed processing the entire request, either due to error
                    // or due to completing the entire response.
                    completedOperation.Cleanup();

                    // libcurl will return CURLE_UNSUPPORTED_PROTOCOL if the url it tried to go to had an unsupported protocol.
                    // This could be the original url provided or one provided in a Location header for a redirect.  Since
                    // we vet the original url passed in, such an error here must be for a redirect, in which case we want to
                    // ignore it and treat such failures as successes, to match the Windows behavior.
                    if (messageResult != CURLcode.CURLE_UNSUPPORTED_PROTOCOL)
                    {
                        // libcurl will return CURLE_RECV_ERROR (56) if proxy authentication failed when connecting to a https server,
                        // whereas it returns CURLE_OK for a http server proxy authentication failure. We ignore this curl behavior error,
                        // and let the user rely on response message status code to match the Windows behavior.
                        if (messageResult != CURLcode.CURLE_RECV_ERROR ||
                                completedOperation._responseMessage.StatusCode != HttpStatusCode.ProxyAuthenticationRequired)
                        {
                            ThrowIfCURLEError(messageResult);
                        }
                    }

                    // Make sure the response message is published, in case it wasn't already, and since we're done processing
                    // everything to do with this request, make sure the response stream is marked complete as well.
                    completedOperation.EnsureResponseMessagePublished();
                    completedOperation._responseMessage.ResponseStream.SignalComplete();
                }
                catch (Exception exc)
                {
                    completedOperation.FailRequest(exc);
                }
            }

            /// <summary>Callback invoked by libcurl when debug information is available.</summary>
            private static void CurlDebugFunction(IntPtr curl, Interop.Http.CurlInfoType type, IntPtr data, ulong size, IntPtr context)
            {
                EasyRequest easy;
                TryGetEasyRequestFromGCHandle(context, out easy);
                // If we're unable to get an associated request, we simply trace without it.

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

            /// <summary>Callback invoked by libcurl for each response header received.</summary>
            private static ulong CurlReceiveHeadersCallback(IntPtr buffer, ulong size, ulong nitems, IntPtr context)
            {
                // The callback is invoked once per header; multi-line headers get merged into a single line.

                size *= nitems;
                Debug.Assert(size <= Interop.Http.CURL_MAX_HTTP_HEADER, $"Expected header size <= {Interop.Http.CURL_MAX_HTTP_HEADER}, got {size}");

                EasyRequest easy;
                if (TryGetEasyRequestFromGCHandle(context, out easy))
                {
                    CurlHandler.EventSourceTrace("Size: {0}", size, easy: easy);
                    try
                    {
                        if (size == 0)
                        {
                            return 0;
                        }

                        // Make sure we've not yet published the response. This could happen with trailer headers,
                        // in which case we just ignore them (we don't want to add them to the response headers at
                        // this point, as it'd contribute to a race condition, both in terms of headers appearing
                        // "randomly" and in terms of accessing a non-thread-safe data structure from this thread
                        // while the consumer might be accessing / mutating it elsewhere.)
                        if (easy.Task.IsCompleted)
                        {
                            CurlHandler.EventSourceTrace("Response already published. Ignoring headers.", easy: easy);
                            return size;
                        }

                        CurlResponseMessage response = easy._responseMessage;
                        CurlResponseHeaderReader reader = new CurlResponseHeaderReader(buffer, size);

                        // Validate that we haven't received too much header data.
                        // MaxResponseHeadersLength property is in units in K (1024) bytes.
                        ulong headerBytesReceived = response._headerBytesReceived + size;
                        if (headerBytesReceived > (ulong)(easy._handler.MaxResponseHeadersLength * 1024))
                        {
                            throw new HttpRequestException(
                                SR.Format(SR.net_http_response_headers_exceeded_length, easy._handler.MaxResponseHeadersLength));
                        }
                        response._headerBytesReceived = (uint)headerBytesReceived;

                        // Parse the header
                        if (reader.ReadStatusLine(response))
                        {
                            CurlHandler.EventSourceTrace("Received status line", easy: easy);

                            // Clear the headers when the status line is received. This may happen multiple times if there are multiple response headers (like in redirection).
                            response.Headers.Clear();
                            response.Content.Headers.Clear();
                            response._headerBytesReceived = (uint)size;

                            // Update the request message with the Uri
                            easy.StoreLastEffectiveUri();
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
                                else if ((int)response.StatusCode >= 300 && (int)response.StatusCode < 400 &&
                                    easy._handler.AllowAutoRedirect &&
                                    string.Equals(headerName, HttpKnownHeaderNames.Location, StringComparison.OrdinalIgnoreCase))
                                {
                                    // A "Location" header field can mean different things for different status codes.  For 3xx status codes,
                                    // it implies a redirect.  As such, if we got a 3xx status code and we support automatically redirecting,
                                    // reconfigure the easy handle under the assumption that libcurl will redirect.  If it does redirect, we'll
                                    // be prepared; if it doesn't (e.g. it doesn't treat some particular 3xx as a redirect, if we've reached
                                    // our redirect limit, etc.), this will have been unnecessary work in reconfiguring the easy handle, but 
                                    // nothing incorrect, as we'll tear down the handle once the request finishes, anyway, and all of the configuration
                                    // we're doing is about initiating a new request.
                                    easy.SetPossibleRedirectForLocationHeader(headerValue);
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

                // Returning a value other than size fails the callback and forces
                // request completion with an error
                CurlHandler.EventSourceTrace("Aborting request", easy: easy);
                return size - 1;
            }

            /// <summary>Callback invoked by libcurl for body data received.</summary>
            private static ulong CurlReceiveBodyCallback(
                IntPtr buffer, ulong size, ulong nitems, IntPtr context)
            {
                size *= nitems;

                EasyRequest easy;
                if (TryGetEasyRequestFromGCHandle(context, out easy))
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
                            return easy._responseMessage.ResponseStream.TransferDataToResponseStream(buffer, (long)size);
                        }
                    }
                    catch (Exception ex)
                    {
                        easy.FailRequest(ex); // cleanup will be handled by main processing loop
                    }
                }

                // Returning a value other than size fails the callback and forces
                // request completion with an error.
                CurlHandler.EventSourceTrace("Aborting request", easy: easy);
                return (size > 0) ? size - 1 : 1;
            }

            /// <summary>Callback invoked by libcurl to read request data.</summary>
            private static ulong CurlSendCallback(IntPtr buffer, ulong size, ulong nitems, IntPtr context)
            {
                int length = checked((int)(size * nitems));
                Debug.Assert(length <= MaxRequestBufferSize, $"length {length} should not be larger than RequestBufferSize {MaxRequestBufferSize}");

                EasyRequest easy;
                if (TryGetEasyRequestFromGCHandle(context, out easy))
                {
                    CurlHandler.EventSourceTrace("Size: {0}", length, easy: easy);

                    if (length == 0)
                    {
                        return 0;
                    }

                    Debug.Assert(easy._requestMessage.Content != null, "We should only be in the send callback if we have request content");
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
                    if (sts.Task != null)
                    {
                        if (!sts.Task.IsCompleted)
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
                        int bytesRead = sts.Task.GetAwaiter().GetResult(); // will throw if read failed
                        Debug.Assert(bytesRead >= 0 && bytesRead <= sts.Buffer.Length, $"ReadAsync returned an invalid result length: {bytesRead}");
                        if (bytesRead == 0)
                        {
                            sts.SetTaskOffsetCount(null, 0, 0);
                            return 0;
                        }

                        // If Count is still 0, then this is the first time after the task completed
                        // that we're examining the data: transfer the bytesRead to the Count.
                        if (sts.Count == 0)
                        {
                            multi.EventSourceTrace("ReadAsync completed with bytes: {0}", bytesRead, easy: easy);
                            sts.Count = bytesRead;
                        }

                        // Now Offset and Count are both accurate.  Determine how much data we can copy to libcurl...
                        int availableData = sts.Count - sts.Offset;
                        Debug.Assert(availableData > 0, "There must be some data still available.");

                        // ... and copy as much of that as libcurl will allow.
                        int bytesToCopy = Math.Min(availableData, length);
                        Marshal.Copy(sts.Buffer, sts.Offset, buffer, bytesToCopy);
                        multi.EventSourceTrace("Copied {0} bytes from request stream", bytesToCopy, easy: easy);

                        // Update the offset.  If we've gone through all of the data, reset the state 
                        // so that the next time we're called back we'll do a new read.
                        sts.Offset += bytesToCopy;
                        Debug.Assert(sts.Offset <= sts.Count, "Offset should never exceed count");
                        if (sts.Offset == sts.Count)
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
                    Debug.Assert(easy._requestMessage.Content != null, "Content shouldn't be null, since we already got a content request stream");
                    long bufferSize = easy._requestMessage.Content.Headers.ContentLength.GetValueOrDefault();
                    if (bufferSize <= 0 || bufferSize > MaxRequestBufferSize)
                    {
                        bufferSize = MaxRequestBufferSize;
                    }
                    easy._sendTransferState = sts = new EasyRequest.SendTransferState((int)bufferSize);
                }

                Debug.Assert(sts != null, "By this point we should have a transfer object");
                Debug.Assert(sts.Task == null, "There shouldn't be a task now.");
                Debug.Assert(sts.Count == 0, "Count should be zero.");
                Debug.Assert(sts.Offset == 0, "Offset should be zero.");

                // If we get here, there was no previously read data available to copy.

                // Make sure we actually have a stream to read from.  This will be null if either
                // this is the first time we're reading it, or if the stream was reset as part
                // of curl trying to rewind.  Then do the read.
                ValueTask<int> asyncRead;
                if (easy._requestContentStream == null)
                {
                    multi.EventSourceTrace("Calling ReadAsStreamAsync to get new request stream", easy: easy);
                    Task<Stream> readAsStreamTask = easy._requestMessage.Content.ReadAsStreamAsync();
                    asyncRead = readAsStreamTask.IsCompleted ?
                        StoreRetrievedContentStreamAndReadAsync(readAsStreamTask, easy, sts, length) :
                        new ValueTask<int>(easy._requestMessage.Content.ReadAsStreamAsync().ContinueWith((t, s) =>
                        {
                            var stateAndRequest = (Tuple<int, EasyRequest.SendTransferState, EasyRequest>)s;
                            return StoreRetrievedContentStreamAndReadAsync(t,
                                stateAndRequest.Item3, stateAndRequest.Item2, stateAndRequest.Item1).AsTask();
                        }, Tuple.Create(length, sts, easy), CancellationToken.None,
                        TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.DenyChildAttach, TaskScheduler.Default).Unwrap());
                }
                else
                {
                    multi.EventSourceTrace("Starting async read", easy: easy);
                    asyncRead = easy._requestContentStream.ReadAsync(
                       new Memory<byte>(sts.Buffer, 0, Math.Min(sts.Buffer.Length, length)), easy._cancellationToken);
                }
                Debug.Assert(asyncRead != null, "Badly implemented stream returned a null task from ReadAsync");

                // Even though it's "Async", it's possible this read could complete synchronously or extremely quickly.  
                // Check to see if it did, in which case we can also satisfy the libcurl request synchronously in this callback.
                if (asyncRead.IsCompleted)
                {
                    multi.EventSourceTrace("Async read completed immediately", easy: easy);

                    // Get the amount of data read.
                    int bytesRead = asyncRead.GetAwaiter().GetResult(); // will throw if read failed
                    if (bytesRead == 0)
                    {
                        multi.EventSourceTrace("Read 0 bytes", easy: easy);
                        return 0;
                    }

                    // Copy as much as we can.
                    int bytesToCopy = Math.Min(bytesRead, length);
                    Debug.Assert(bytesToCopy > 0 && bytesToCopy <= sts.Buffer.Length, $"ReadAsync quickly returned an invalid result length: {bytesToCopy}");
                    Marshal.Copy(sts.Buffer, 0, buffer, bytesToCopy);
                    multi.EventSourceTrace("Read {0} bytes", bytesToCopy, easy: easy);

                    // If we read more than we were able to copy, stash it away for the next read.
                    if (bytesToCopy < bytesRead)
                    {
                        multi.EventSourceTrace("Storing {0} bytes for later", bytesRead - bytesToCopy, easy: easy);
                        sts.SetTaskOffsetCount(asyncRead.AsTask(), bytesToCopy, bytesRead);
                    }

                    // Return the number of bytes read.
                    return (ulong)bytesToCopy;
                }

                // Otherwise, the read completed asynchronously.  Store the task, and hook up a continuation 
                // such that the connection will be unpaused once the task completes.
                sts.SetTaskOffsetCount(asyncRead.AsTask(), 0, 0);
                sts.Task.ContinueWith((t, s) =>
                {
                    EasyRequest easyRef = (EasyRequest)s;
                    easyRef._associatedMultiAgent.RequestUnpause(easyRef);
                }, easy, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

                // Then pause the connection.
                multi.EventSourceTrace("Pausing transfer from request stream", easy: easy);
                return Interop.Http.CURL_READFUNC_PAUSE;
            }

            /// <summary>
            /// Given a completed task used to retrieve the content stream asynchronously, extracts the stream,
            /// stores it into <see cref="EasyRequest._requestContentStream"/>, and does an initial read on it.
            /// </summary>
            private static ValueTask<int> StoreRetrievedContentStreamAndReadAsync(
                Task<Stream> readAsStreamTask, EasyRequest easy, EasyRequest.SendTransferState sts, int length)
            {
                Debug.Assert(readAsStreamTask.IsCompleted, $"Expected {nameof(readAsStreamTask)} to be completed, got {readAsStreamTask.Status}");
                try
                {
                    MultiAgent multi = easy._associatedMultiAgent;
                    multi.EventSourceTrace("Async operation completed: {0}", readAsStreamTask.Status, easy: easy);

                    // Get and store the resulting stream
                    easy._requestContentStream = readAsStreamTask.GetAwaiter().GetResult();
                    multi.EventSourceTrace("Got stream: {0}", easy._requestContentStream.GetType(), easy: easy);

                    // If the stream is seekable, store its original position.  We'll use this any time we need to seek
                    // back to the "beginning", as it's possible the stream isn't at position 0.
                    if (easy._requestContentStream.CanSeek)
                    {
                        long startingPos = easy._requestContentStream.Position;
                        easy._requestContentStreamStartingPosition = startingPos;
                        CurlHandler.EventSourceTrace("Stream starting position: {0}", startingPos, easy: easy);
                    }

                    // Now that we have a stream, do the desired read
                    multi.EventSourceTrace("Starting async read", easy: easy);
                    return easy._requestContentStream.ReadAsync(new Memory<byte>(sts.Buffer, 0, Math.Min(sts.Buffer.Length, length)), easy._cancellationToken);
                }
                catch (OperationCanceledException oce)
                {
                    return new ValueTask<int>(oce.CancellationToken.IsCancellationRequested ?
                        Task.FromCanceled<int>(oce.CancellationToken) :
                        Task.FromCanceled<int>(new CancellationToken(true)));
                }
                catch (Exception exc)
                {
                    return new ValueTask<int>(Task.FromException<int>(exc));
                }
            }

            /// <summary>Callback invoked by libcurl to seek to a position within the request stream.</summary>
            private static Interop.Http.CurlSeekResult CurlSeekCallback(IntPtr context, long offset, int origin)
            {
                EasyRequest easy;
                if (TryGetEasyRequestFromGCHandle(context, out easy))
                {
                    CurlHandler.EventSourceTrace("Offset: {0}, Origin: {1}", offset, origin, 0, easy: easy);
                    try
                    {
                        // If we don't have a stream yet, we can't seek.
                        if (easy._requestContentStream == null)
                        {
                            CurlHandler.EventSourceTrace("No request stream exists yet. Can't seek", easy: easy);
                            return Interop.Http.CurlSeekResult.CURL_SEEKFUNC_CANTSEEK;
                        }

                        // If the stream is seekable, which is a very common case, everyone is happy.
                        // Simply seek on the stream.
                        if (easy._requestContentStream.CanSeek)
                        {
                            CurlHandler.EventSourceTrace("Seeking on the existing stream", easy: easy);
                            SeekOrigin seek = (SeekOrigin)origin;
                            if (seek == SeekOrigin.Begin)
                            {
                                Debug.Assert(easy._requestContentStreamStartingPosition.HasValue);
                                easy._requestContentStream.Position = easy._requestContentStreamStartingPosition.GetValueOrDefault();
                            }
                            else
                            {
                                easy._requestContentStream.Seek(offset, seek);
                            }
                            return Interop.Http.CurlSeekResult.CURL_SEEKFUNC_OK;
                        }

                        // The stream isn't seekable.  Now we start getting into shakier ground.
                        // Most of the time the seek callback is used, it's because libcurl is rewinding
                        // to the beginning of the stream due to a redirect, an auth challenge, etc. (other
                        // cases where it might try to seek elsewhere would be, e.g., with a Range header).
                        // In such cases, we can't seek, but we can simply re-read the stream from the content.
                        // In most cases this will "just work." There are corner cases, however, where it'll
                        // fail but we won't yet know it failed, e.g. if a StreamContent is used, ReadAsStreamAsync
                        // will give us back a wrapper stream over the same original underlying stream and without
                        // having changed its position (it's not seekable).  At that point we'll think
                        // we have a new stream, but when reading starts happening, it'll be at the existing
                        // position, and we'll only end up sending part of the data (or none in the common case
                        // where we'd already read ot the end).  As a workaround for that, we can at least special case
                        // the StreamContent type, for which we know this will be an issue.  It won't help with other
                        // corner -case contents like this, but for such contents, we would still end up failing the
                        // request, just sooner.
                        if (offset == 0 && origin == (int)SeekOrigin.Begin && 
                            !(easy._requestMessage.Content is StreamContent)) // avoid known problematic case
                        {
                            CurlHandler.EventSourceTrace("Removing the existing request stream, to be replaced on subsequent read", easy: easy);
                            easy._requestContentStream = null;
                        }

                        // Can't seek.  Let libcurl know: it may still be able to recover.
                        CurlHandler.EventSourceTrace("Can't seek", easy: easy);
                        return Interop.Http.CurlSeekResult.CURL_SEEKFUNC_CANTSEEK;
                    }
                    catch (Exception ex)
                    {
                        easy.FailRequest(ex); // cleanup will be handled by main processing loop
                    }
                }

                // Something went wrong
                CurlHandler.EventSourceTrace("Seek failed", easy: easy);
                return Interop.Http.CurlSeekResult.CURL_SEEKFUNC_FAIL;
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
                public StrongToWeakReference<EasyRequest> EasyWrapper;
                public Interop.Http.SafeCurlHandle EasyHandle;
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
                Unpause,
                /// <summary>A request to shutdown the agent and all active operations.  No easy request is associated with this type.</summary>
                Shutdown
            }
        }

    }
}
