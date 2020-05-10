// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace System.Net.Http
{
    internal sealed partial class Http2Connection
    {
        private sealed class Http2Stream : IValueTaskSource, IHttpTrace, IHttpHeadersHandler
        {
            private const int InitialStreamBufferSize =
#if DEBUG
                10;
#else
                1024;
#endif

            private static readonly byte[] s_statusHeaderName = Encoding.ASCII.GetBytes(":status");

            private readonly Http2Connection _connection;
            private readonly int _streamId;
            private readonly HttpRequestMessage _request;
            private HttpResponseMessage? _response;
            /// <summary>Stores any trailers received after returning the response content to the caller.</summary>
            private List<KeyValuePair<HeaderDescriptor, string>>? _trailers;

            private ArrayBuffer _responseBuffer; // mutable struct, do not make this readonly
            private int _pendingWindowUpdate;
            private CancelableCreditWaiter? _creditWaiter;
            private int _availableCredit;

            private StreamCompletionState _requestCompletionState;
            private StreamCompletionState _responseCompletionState;
            private ResponseProtocolState _responseProtocolState;

            // If this is not null, then we have received a reset from the server
            // (i.e. RST_STREAM or general IO error processing the connection)
            private Exception? _resetException;
            private bool _canRetry;             // if _resetException != null, this indicates the stream was refused and so the request is retryable

            // This flag indicates that, per section 8.1 of the RFC, the server completed the response and then sent a RST_STREAM with error = NO_ERROR.
            // This is a signal to stop sending the request body, but the request is still considered successful.
            private bool _requestBodyAbandoned;

            /// <summary>
            /// The core logic for the IValueTaskSource implementation.
            ///
            /// Thread-safety:
            /// _waitSource is used to coordinate between a producer indicating that something is available to process (either the connection's event loop
            /// or a cancellation request) and a consumer doing that processing.  There must only ever be a single consumer, namely this stream reading
            /// data associated with the response.  Because there is only ever at most one consumer, producers can trust that if _hasWaiter is true,
            /// until the _waitSource is then set, no consumer will attempt to reset the _waitSource.  A producer must still take SyncObj in order to
            /// coordinate with other producers (e.g. a race between data arriving from the event loop and cancellation being requested), but while holding
            /// the lock it can check whether _hasWaiter is true, and if it is, set _hasWaiter to false, exit the lock, and then set the _waitSource. Another
            /// producer coming along will then see _hasWaiter as false and will not attempt to concurrently set _waitSource (which would violate _waitSource's
            /// thread-safety), and no other consumer could come along in the interim, because _hasWaiter being true means that a consumer is already waiting
            /// for _waitSource to be set, and legally there can only be one consumer.  Once this producer sets _waitSource, the consumer could quickly loop
            /// around to wait again, but invariants have all been maintained in the interim, and the consumer would need to take the SyncObj lock in order to
            /// Reset _waitSource.
            /// </summary>
            private ManualResetValueTaskSourceCore<bool> _waitSource = new ManualResetValueTaskSourceCore<bool> { RunContinuationsAsynchronously = true }; // mutable struct, do not make this readonly
            /// <summary>Cancellation registration used to cancel the <see cref="_waitSource"/>.</summary>
            private CancellationTokenRegistration _waitSourceCancellation;
            /// <summary>
            /// Whether code has requested or is about to request a wait be performed and thus requires a call to SetResult to complete it.
            /// This is read and written while holding the lock so that most operations on _waitSource don't need to be.
            /// </summary>
            private bool _hasWaiter;

            private readonly CancellationTokenSource? _requestBodyCancellationSource;

            // This is a linked token combining the above source and the user-supplied token to SendRequestBodyAsync
            private CancellationToken _requestBodyCancellationToken;

            private readonly TaskCompletionSource<bool>? _expect100ContinueWaiter;

            private int _headerBudgetRemaining;

            private const int StreamWindowSize = DefaultInitialWindowSize;

            // See comment on ConnectionWindowThreshold.
            private const int StreamWindowThreshold = StreamWindowSize / 8;

            public Http2Stream(HttpRequestMessage request, Http2Connection connection, int streamId, int initialWindowSize)
            {
                _request = request;
                _connection = connection;
                _streamId = streamId;

                _requestCompletionState = StreamCompletionState.InProgress;
                _responseCompletionState = StreamCompletionState.InProgress;

                _responseProtocolState = ResponseProtocolState.ExpectingStatus;

                _responseBuffer = new ArrayBuffer(InitialStreamBufferSize, usePool: true);

                _pendingWindowUpdate = 0;
                _availableCredit = initialWindowSize;

                _headerBudgetRemaining = connection._pool.Settings._maxResponseHeadersLength * 1024;

                if (_request.Content == null)
                {
                    _requestCompletionState = StreamCompletionState.Completed;
                }
                else
                {
                    // Create this here because it can be canceled before SendRequestBodyAsync is even called.
                    // To avoid race conditions that can result in this being disposed in response to a server reset
                    // and then used to issue cancellation, we simply avoid disposing it; that's fine as long as we don't
                    // construct this via CreateLinkedTokenSource, in which case disposal is necessary to avoid a potential
                    // leak.  If how this is constructed ever changes, we need to revisit disposing it, such as by
                    // using synchronization (e.g. using an Interlocked.Exchange to "consume" the _requestBodyCancellationSource
                    // for either disposal or issuing cancellation).
                    _requestBodyCancellationSource = new CancellationTokenSource();

                    if (_request.HasHeaders && _request.Headers.ExpectContinue == true)
                    {
                        // Create a TCS for handling Expect: 100-continue semantics. See WaitFor100ContinueAsync.
                        // Note we need to create this in the constructor, because we can receive a 100 Continue response at any time after the constructor finishes.
                        _expect100ContinueWaiter = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                    }
                }

                if (NetEventSource.IsEnabled) Trace($"{request}, {nameof(initialWindowSize)}={initialWindowSize}");
            }

            private object SyncObject => this; // this isn't handed out to code that may lock on it

            public int StreamId => _streamId;

            public HttpResponseMessage GetAndClearResponse()
            {
                // Once SendAsync completes, the Http2Stream should no longer hold onto the response message.
                // Since the Http2Stream is rooted by the Http2Connection dictionary, doing so would prevent
                // the response stream from being collected and finalized if it were to be dropped without
                // being disposed first.
                Debug.Assert(_response != null);
                HttpResponseMessage r = _response;
                _response = null;
                return r;
            }

            public async Task SendRequestBodyAsync(CancellationToken cancellationToken)
            {
                if (_request.Content == null)
                {
                    Debug.Assert(_requestCompletionState == StreamCompletionState.Completed);
                    return;
                }

                if (NetEventSource.IsEnabled) Trace($"{_request.Content}");

                Debug.Assert(_requestBodyCancellationSource != null);

                // Create a linked cancellation token source so that we can cancel the request in the event of receiving RST_STREAM
                // and similiar situations where we need to cancel the request body (see Cancel method).
                _requestBodyCancellationToken = cancellationToken.CanBeCanceled ?
                    CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _requestBodyCancellationSource.Token).Token :
                    _requestBodyCancellationSource.Token;

                try
                {
                    bool sendRequestContent = true;
                    if (_expect100ContinueWaiter != null)
                    {
                        sendRequestContent = await WaitFor100ContinueAsync(_requestBodyCancellationToken).ConfigureAwait(false);
                    }

                    if (sendRequestContent)
                    {
                        using (Http2WriteStream writeStream = new Http2WriteStream(this))
                        {
                            await _request.Content.InternalCopyToAsync(writeStream, null, _requestBodyCancellationToken).ConfigureAwait(false);
                        }
                    }

                    if (NetEventSource.IsEnabled) Trace($"Finished sending request body.");
                }
                catch (Exception e)
                {
                    if (NetEventSource.IsEnabled) Trace($"Failed to send request body: {e}");

                    bool signalWaiter = false;
                    bool sendReset = false;

                    Debug.Assert(!Monitor.IsEntered(SyncObject));
                    lock (SyncObject)
                    {
                        Debug.Assert(_requestCompletionState == StreamCompletionState.InProgress, $"Request already completed with state={_requestCompletionState}");

                        if (_requestBodyAbandoned)
                        {
                            // See comments on _requestBodyAbandoned.
                            // In this case, the request is still considered successful and we do not want to send a RST_STREAM,
                            // and we also don't want to propagate any error to the caller, in particular for non-duplex scenarios.
                            Debug.Assert(_responseCompletionState == StreamCompletionState.Completed);
                            _requestCompletionState = StreamCompletionState.Completed;
                            Complete();
                            return;
                        }

                        // This should not cause RST_STREAM to be sent because the request is still marked as in progress.
                        (signalWaiter, sendReset) = CancelResponseBody();
                        Debug.Assert(!sendReset);

                        _requestCompletionState = StreamCompletionState.Failed;
                        sendReset = true;
                        Complete();
                    }

                    if (sendReset)
                    {
                        SendReset();
                    }

                    if (signalWaiter)
                    {
                        _waitSource.SetResult(true);
                    }

                    throw;
                }

                // New scope here to avoid variable name conflict on "sendReset"
                {
                    Debug.Assert(!Monitor.IsEntered(SyncObject));
                    bool sendReset = false;
                    lock (SyncObject)
                    {
                        Debug.Assert(_requestCompletionState == StreamCompletionState.InProgress, $"Request already completed with state={_requestCompletionState}");

                        _requestCompletionState = StreamCompletionState.Completed;
                        if (_responseCompletionState == StreamCompletionState.Failed)
                        {
                            // Note, we can reach this point if the response stream failed but cancellation didn't propagate before we finished.
                            sendReset = true;
                            Complete();
                        }
                        else
                        {
                            if (_responseCompletionState == StreamCompletionState.Completed)
                            {
                                Complete();
                            }
                        }
                    }

                    if (sendReset)
                    {
                        SendReset();
                    }
                    else
                    {
                        // Send EndStream asynchronously and without cancellation.
                        // If this fails, it means that the connection is aborting and we will be reset.
                        _connection.LogExceptions(_connection.SendEndStreamAsync(_streamId));
                    }
                }
            }

            // Delay sending request body if we sent Expect: 100-continue.
            // We can either get 100 response from server and send body
            // or we may exceed timeout and send request body anyway.
            // If we get response status >= 300, we will not send the request body.
            public async ValueTask<bool> WaitFor100ContinueAsync(CancellationToken cancellationToken)
            {
                Debug.Assert(_request.Content != null);
                if (NetEventSource.IsEnabled) Trace($"Waiting to send request body content for 100-Continue.");

                // use TCS created in constructor. It will complete when one of two things occurs:
                // 1. if a timer fires before we receive the relevant response from the server.
                // 2. if we receive the relevant response from the server before a timer fires.
                // In the first case, we could run this continuation synchronously, but in the latter, we shouldn't,
                // as we could end up starting the body copy operation on the main event loop thread, which could
                // then starve the processing of other requests.  So, we make the TCS RunContinuationsAsynchronously.
                bool sendRequestContent;
                TaskCompletionSource<bool> waiter = _expect100ContinueWaiter!;
                using (var expect100Timer = new Timer(s =>
                {
                    var thisRef = (Http2Stream)s!;
                    if (NetEventSource.IsEnabled) thisRef.Trace($"100-Continue timer expired.");
                    thisRef._expect100ContinueWaiter?.TrySetResult(true);
                }, this, _connection._pool.Settings._expect100ContinueTimeout, Timeout.InfiniteTimeSpan))
                {
                    sendRequestContent = await waiter.Task.ConfigureAwait(false);
                    // By now, either we got a response from the server or the timer expired.
                }

                return sendRequestContent;
            }

            private void SendReset()
            {
                Debug.Assert(!Monitor.IsEntered(SyncObject));
                Debug.Assert(_requestCompletionState != StreamCompletionState.InProgress);
                Debug.Assert(_responseCompletionState != StreamCompletionState.InProgress);
                Debug.Assert(_requestCompletionState == StreamCompletionState.Failed || _responseCompletionState == StreamCompletionState.Failed,
                    "Reset called but neither request nor response is failed");

                if (NetEventSource.IsEnabled) Trace($"Stream reset. Request={_requestCompletionState}, Response={_responseCompletionState}.");

                // Don't send a RST_STREAM if we've already received one from the server.
                if (_resetException == null)
                {
                    _connection.LogExceptions(_connection.SendRstStreamAsync(_streamId, Http2ProtocolErrorCode.Cancel));
                }
            }

            private void Complete()
            {
                Debug.Assert(Monitor.IsEntered(SyncObject));
                Debug.Assert(_requestCompletionState != StreamCompletionState.InProgress);
                Debug.Assert(_responseCompletionState != StreamCompletionState.InProgress);

                if (NetEventSource.IsEnabled) Trace($"Stream complete. Request={_requestCompletionState}, Response={_responseCompletionState}.");

                _connection.RemoveStream(this);

                CreditWaiter? w = _creditWaiter;
                if (w != null)
                {
                    w.Dispose();
                    _creditWaiter = null;
                }
            }

            private void Cancel()
            {
                if (NetEventSource.IsEnabled) Trace("");

                CancellationTokenSource? requestBodyCancellationSource = null;
                bool signalWaiter = false;
                bool sendReset = false;

                Debug.Assert(!Monitor.IsEntered(SyncObject));
                lock (SyncObject)
                {
                    if (_requestCompletionState == StreamCompletionState.InProgress)
                    {
                        requestBodyCancellationSource = _requestBodyCancellationSource;
                        Debug.Assert(requestBodyCancellationSource != null);
                    }

                    (signalWaiter, sendReset) = CancelResponseBody();
                }

                if (requestBodyCancellationSource != null)
                {
                    // When cancellation propagates, SendRequestBodyAsync will set _requestCompletionState to Failed
                    requestBodyCancellationSource.Cancel();
                }

                if (sendReset)
                {
                    SendReset();
                }

                if (signalWaiter)
                {
                    _waitSource.SetResult(true);
                }
            }

            // Returns whether the waiter should be signalled or not.
            private (bool signalWaiter, bool sendReset) CancelResponseBody()
            {
                Debug.Assert(Monitor.IsEntered(SyncObject));

                bool sendReset = false;

                if (_responseCompletionState == StreamCompletionState.InProgress)
                {
                    _responseCompletionState = StreamCompletionState.Failed;
                    if (_requestCompletionState != StreamCompletionState.InProgress)
                    {
                        sendReset = true;
                        Complete();
                    }
                }

                // Discard any remaining buffered response data
                if (_responseBuffer.ActiveLength != 0)
                {
                    _responseBuffer.Discard(_responseBuffer.ActiveLength);
                }

                _responseProtocolState = ResponseProtocolState.Aborted;

                bool signalWaiter = _hasWaiter;
                _hasWaiter = false;

                return (signalWaiter, sendReset);
            }

            public void OnWindowUpdate(int amount)
            {
                lock (SyncObject)
                {
                    _availableCredit = checked(_availableCredit + amount);
                    if (_availableCredit > 0 && _creditWaiter != null && _creditWaiter.IsPending)
                    {
                        int granted = Math.Min(_availableCredit, _creditWaiter.Amount);
                        if (_creditWaiter.TrySetResult(granted))
                        {
                            _availableCredit -= granted;
                        }
                    }
                }
            }

            void IHttpHeadersHandler.OnStaticIndexedHeader(int index)
            {
                // TODO: https://github.com/dotnet/runtime/issues/1505
                Debug.Fail("Currently unused by HPACK, this should never be called.");
            }

            void IHttpHeadersHandler.OnStaticIndexedHeader(int index, ReadOnlySpan<byte> value)
            {
                // TODO: https://github.com/dotnet/runtime/issues/1505
                Debug.Fail("Currently unused by HPACK, this should never be called.");
            }

            public void OnHeader(ReadOnlySpan<byte> name, ReadOnlySpan<byte> value)
            {
                if (NetEventSource.IsEnabled) Trace($"{Encoding.ASCII.GetString(name)}: {Encoding.ASCII.GetString(value)}");
                Debug.Assert(name != null && name.Length > 0);

                _headerBudgetRemaining -= name.Length + value.Length;
                if (_headerBudgetRemaining < 0)
                {
                    throw new HttpRequestException(SR.Format(SR.net_http_response_headers_exceeded_length, _connection._pool.Settings._maxResponseHeadersLength * 1024L));
                }

                Debug.Assert(!Monitor.IsEntered(SyncObject));
                lock (SyncObject)
                {
                    if (_responseProtocolState == ResponseProtocolState.Aborted)
                    {
                        // We could have aborted while processing the header block.
                        return;
                    }

                    if (name[0] == (byte)':')
                    {
                        if (_responseProtocolState != ResponseProtocolState.ExpectingHeaders && _responseProtocolState != ResponseProtocolState.ExpectingStatus)
                        {
                            // Pseudo-headers are allowed only in header block
                            if (NetEventSource.IsEnabled) Trace($"Pseudo-header received in {_responseProtocolState} state.");
                            throw new HttpRequestException(SR.net_http_invalid_response_pseudo_header_in_trailer);
                        }

                        if (name.SequenceEqual(s_statusHeaderName))
                        {
                            if (_responseProtocolState != ResponseProtocolState.ExpectingStatus)
                            {
                                if (NetEventSource.IsEnabled) Trace("Received extra status header.");
                                throw new HttpRequestException(SR.Format(SR.net_http_invalid_response_status_code, "duplicate status"));
                            }

                            int statusValue = ParseStatusCode(value);
                            _response = new HttpResponseMessage()
                            {
                                Version = HttpVersion.Version20,
                                RequestMessage = _request,
                                Content = new HttpConnectionResponseContent(),
                                StatusCode = (HttpStatusCode)statusValue
                            };

                            if (statusValue < 200)
                            {
                                // We do not process headers from 1xx responses.
                                _responseProtocolState = ResponseProtocolState.ExpectingIgnoredHeaders;

                                if (_response.StatusCode == HttpStatusCode.Continue && _expect100ContinueWaiter != null)
                                {
                                    if (NetEventSource.IsEnabled) Trace("Received 100-Continue status.");
                                    _expect100ContinueWaiter.TrySetResult(true);
                                }
                            }
                            else
                            {
                                _responseProtocolState = ResponseProtocolState.ExpectingHeaders;

                                // If we are waiting for a 100-continue response, signal the waiter now.
                                if (_expect100ContinueWaiter != null)
                                {
                                    // If the final status code is >= 300, skip sending the body.
                                    bool shouldSendBody = (statusValue < 300);

                                    if (NetEventSource.IsEnabled) Trace($"Expecting 100 Continue but received final status {statusValue}.");
                                    _expect100ContinueWaiter.TrySetResult(shouldSendBody);
                                }
                            }
                        }
                        else
                        {
                            if (NetEventSource.IsEnabled) Trace($"Invalid response pseudo-header '{Encoding.ASCII.GetString(name)}'.");
                            throw new HttpRequestException(SR.net_http_invalid_response);
                        }
                    }
                    else
                    {
                        if (_responseProtocolState == ResponseProtocolState.ExpectingIgnoredHeaders)
                        {
                            // for 1xx response we ignore all headers.
                            return;
                        }

                        if (_responseProtocolState != ResponseProtocolState.ExpectingHeaders && _responseProtocolState != ResponseProtocolState.ExpectingTrailingHeaders)
                        {
                            if (NetEventSource.IsEnabled) Trace("Received header before status.");
                            throw new HttpRequestException(SR.net_http_invalid_response);
                        }

                        if (!HeaderDescriptor.TryGet(name, out HeaderDescriptor descriptor))
                        {
                            // Invalid header name
                            throw new HttpRequestException(SR.Format(SR.net_http_invalid_response_header_name, Encoding.ASCII.GetString(name)));
                        }

                        // Note we ignore the return value from TryAddWithoutValidation;
                        // if the header can't be added, we silently drop it.
                        if (_responseProtocolState == ResponseProtocolState.ExpectingTrailingHeaders)
                        {
                            Debug.Assert(_trailers != null);
                            string headerValue = descriptor.GetHeaderValue(value);
                            _trailers.Add(KeyValuePair.Create((descriptor.HeaderType & HttpHeaderType.Request) == HttpHeaderType.Request ? descriptor.AsCustomHeader() : descriptor, headerValue));
                        }
                        else if ((descriptor.HeaderType & HttpHeaderType.Content) == HttpHeaderType.Content)
                        {
                            Debug.Assert(_response != null && _response.Content != null);
                            string headerValue = descriptor.GetHeaderValue(value);
                            _response.Content.Headers.TryAddWithoutValidation(descriptor, headerValue);
                        }
                        else
                        {
                            Debug.Assert(_response != null);
                            string headerValue = _connection.GetResponseHeaderValueWithCaching(descriptor, value);
                            _response.Headers.TryAddWithoutValidation((descriptor.HeaderType & HttpHeaderType.Request) == HttpHeaderType.Request ? descriptor.AsCustomHeader() : descriptor, headerValue);
                        }
                    }
                }
            }

            public void OnHeadersStart()
            {
                Debug.Assert(!Monitor.IsEntered(SyncObject));
                lock (SyncObject)
                {
                    if (_responseProtocolState == ResponseProtocolState.Aborted)
                    {
                        return;
                    }

                    if (_responseProtocolState != ResponseProtocolState.ExpectingStatus && _responseProtocolState != ResponseProtocolState.ExpectingData)
                    {
                        throw new Http2ConnectionException(Http2ProtocolErrorCode.ProtocolError);
                    }

                    if (_responseProtocolState == ResponseProtocolState.ExpectingData)
                    {
                        _responseProtocolState = ResponseProtocolState.ExpectingTrailingHeaders;
                        _trailers ??= new List<KeyValuePair<HeaderDescriptor, string>>();
                    }
                }
            }

            public void OnHeadersComplete(bool endStream)
            {
                Debug.Assert(!Monitor.IsEntered(SyncObject));
                bool signalWaiter;
                lock (SyncObject)
                {
                    if (_responseProtocolState == ResponseProtocolState.Aborted)
                    {
                        return;
                    }

                    if (_responseProtocolState != ResponseProtocolState.ExpectingHeaders && _responseProtocolState != ResponseProtocolState.ExpectingTrailingHeaders && _responseProtocolState != ResponseProtocolState.ExpectingIgnoredHeaders)
                    {
                        throw new Http2ConnectionException(Http2ProtocolErrorCode.ProtocolError);
                    }

                    if (_responseProtocolState == ResponseProtocolState.ExpectingHeaders)
                    {
                        _responseProtocolState = endStream ? ResponseProtocolState.Complete : ResponseProtocolState.ExpectingData;
                    }
                    else if (_responseProtocolState == ResponseProtocolState.ExpectingTrailingHeaders)
                    {
                        if (!endStream)
                        {
                            if (NetEventSource.IsEnabled) Trace("Trailing headers received without endStream");
                            throw new Http2ConnectionException(Http2ProtocolErrorCode.ProtocolError);
                        }

                        _responseProtocolState = ResponseProtocolState.Complete;
                    }
                    else if (_responseProtocolState == ResponseProtocolState.ExpectingIgnoredHeaders)
                    {
                        if (endStream)
                        {
                            // we should not get endStream while processing 1xx response.
                            throw new Http2ConnectionException(Http2ProtocolErrorCode.ProtocolError);
                        }

                        _responseProtocolState = ResponseProtocolState.ExpectingStatus;
                        // We should wait for final response before signaling to waiter.
                        return;
                    }
                    else
                    {
                        _responseProtocolState = ResponseProtocolState.ExpectingData;
                    }

                    if (endStream)
                    {
                        Debug.Assert(_responseCompletionState == StreamCompletionState.InProgress, $"Response already completed with state={_responseCompletionState}");

                        _responseCompletionState = StreamCompletionState.Completed;
                        if (_requestCompletionState == StreamCompletionState.Completed)
                        {
                            Complete();
                        }

                        // We should never reach here with the request failed. It's only set to Failed in SendRequestBodyAsync after we've called Cancel,
                        // which will set the _responseCompletionState to Failed, meaning we'll never get here.
                        Debug.Assert(_requestCompletionState != StreamCompletionState.Failed);
                    }

                    signalWaiter = _hasWaiter;
                    _hasWaiter = false;
                }

                if (signalWaiter)
                {
                    _waitSource.SetResult(true);
                }
            }

            public void OnResponseData(ReadOnlySpan<byte> buffer, bool endStream)
            {
                Debug.Assert(!Monitor.IsEntered(SyncObject));
                bool signalWaiter;
                lock (SyncObject)
                {
                    if (_responseProtocolState == ResponseProtocolState.Aborted)
                    {
                        return;
                    }

                    if (_responseProtocolState != ResponseProtocolState.ExpectingData)
                    {
                        // Flow control messages are not valid in this state.
                        throw new Http2ConnectionException(Http2ProtocolErrorCode.ProtocolError);
                    }

                    if (_responseBuffer.ActiveLength + buffer.Length > StreamWindowSize)
                    {
                        // Window size exceeded.
                        throw new Http2ConnectionException(Http2ProtocolErrorCode.FlowControlError);
                    }

                    _responseBuffer.EnsureAvailableSpace(buffer.Length);
                    buffer.CopyTo(_responseBuffer.AvailableSpan);
                    _responseBuffer.Commit(buffer.Length);

                    if (endStream)
                    {
                        _responseProtocolState = ResponseProtocolState.Complete;

                        Debug.Assert(_responseCompletionState == StreamCompletionState.InProgress, $"Response already completed with state={_responseCompletionState}");

                        _responseCompletionState = StreamCompletionState.Completed;
                        if (_requestCompletionState == StreamCompletionState.Completed)
                        {
                            Complete();
                        }

                        // We should never reach here with the request failed. It's only set to Failed in SendRequestBodyAsync after we've called Cancel,
                        // which will set the _responseCompletionState to Failed, meaning we'll never get here.
                        Debug.Assert(_requestCompletionState != StreamCompletionState.Failed);
                    }

                    signalWaiter = _hasWaiter;
                    _hasWaiter = false;
                }

                if (signalWaiter)
                {
                    _waitSource.SetResult(true);
                }
            }

            // This is called in several different cases:
            // (1) Receiving RST_STREAM on this stream. If so, the resetStreamErrorCode will be non-null, and canRetry will be true only if the error code was REFUSED_STREAM.
            // (2) Receiving GOAWAY that indicates this stream has not been processed. If so, canRetry will be true.
            // (3) Connection IO failure or protocol violation. If so, resetException will contain the relevant exception and canRetry will be false.
            // (4) Receiving EOF from the server. If so, resetException will contain an exception like "expected 9 bytes of data", and canRetry will be false.
            public void OnReset(Exception resetException, Http2ProtocolErrorCode? resetStreamErrorCode = null, bool canRetry = false)
            {
                if (NetEventSource.IsEnabled) Trace($"{nameof(resetException)}={resetException}, {nameof(resetStreamErrorCode)}={resetStreamErrorCode}");

                bool cancel = false;
                CancellationTokenSource? requestBodyCancellationSource = null;

                Debug.Assert(!Monitor.IsEntered(SyncObject));
                lock (SyncObject)
                {
                    // If we've already finished, don't actually reset the stream.
                    // Otherwise, any waiters that haven't executed yet will see the _resetException and throw.
                    // This can happen, for example, when the server finishes the request and then closes the connection,
                    // but the waiter hasn't woken up yet.
                    if (_requestCompletionState == StreamCompletionState.Completed && _responseCompletionState == StreamCompletionState.Completed)
                    {
                        return;
                    }

                    // It's possible we could be called twice, e.g. we receive a RST_STREAM and then the whole connection dies
                    // before we have a chance to process cancellation and tear everything down. Just ignore this.
                    if (_resetException != null)
                    {
                        return;
                    }

                    // If the server told us the request has not been processed (via Last-Stream-ID on GOAWAY),
                    // but we've already received some response data from the server, then the server lied to us.
                    // In this case, don't allow the request to be retried.
                    if (canRetry && _responseProtocolState != ResponseProtocolState.ExpectingStatus)
                    {
                        canRetry = false;
                    }

                    // Per section 8.1 in the RFC:
                    // If the server has completed the response body (i.e. we've received EndStream)
                    // but the request body is still sending, and we then receive a RST_STREAM with errorCode = NO_ERROR,
                    // we treat this specially and simply cancel sending the request body, rather than treating
                    // the entire request as failed.
                    if (resetStreamErrorCode == Http2ProtocolErrorCode.NoError &&
                        _responseCompletionState == StreamCompletionState.Completed)
                    {
                        if (_requestCompletionState == StreamCompletionState.InProgress)
                        {
                            _requestBodyAbandoned = true;
                            requestBodyCancellationSource = _requestBodyCancellationSource;
                            Debug.Assert(requestBodyCancellationSource != null);
                        }
                    }
                    else
                    {
                        _resetException = resetException;
                        _canRetry = canRetry;
                        cancel = true;
                    }
                }

                if (requestBodyCancellationSource != null)
                {
                    Debug.Assert(_requestBodyAbandoned);
                    Debug.Assert(!cancel);
                    requestBodyCancellationSource.Cancel();
                }
                else
                {
                    Cancel();
                }
            }

            private void CheckResponseBodyState()
            {
                Debug.Assert(Monitor.IsEntered(SyncObject));

                if (_resetException != null)
                {
                    if (_canRetry)
                    {
                        throw new HttpRequestException(SR.net_http_request_aborted, _resetException, allowRetry: RequestRetryType.RetryOnSameOrNextProxy);
                    }

                    throw new IOException(SR.net_http_request_aborted, _resetException);
                }

                if (_responseProtocolState == ResponseProtocolState.Aborted)
                {
                    throw new IOException(SR.net_http_request_aborted);
                }
            }

            // Determine if we have enough data to process up to complete final response headers.
            private (bool wait, bool isEmptyResponse) TryEnsureHeaders()
            {
                Debug.Assert(!Monitor.IsEntered(SyncObject));
                lock (SyncObject)
                {
                    CheckResponseBodyState();

                    if (_responseProtocolState == ResponseProtocolState.ExpectingHeaders || _responseProtocolState == ResponseProtocolState.ExpectingIgnoredHeaders || _responseProtocolState == ResponseProtocolState.ExpectingStatus)
                    {
                        Debug.Assert(!_hasWaiter);
                        _hasWaiter = true;
                        _waitSource.Reset();
                        return (true, false);
                    }
                    else if (_responseProtocolState == ResponseProtocolState.ExpectingData || _responseProtocolState == ResponseProtocolState.ExpectingTrailingHeaders)
                    {
                        return (false, false);
                    }
                    else
                    {
                        Debug.Assert(_responseProtocolState == ResponseProtocolState.Complete);
                        return (false, _responseBuffer.ActiveLength == 0);
                    }
                }
            }

            public async Task ReadResponseHeadersAsync(CancellationToken cancellationToken)
            {
                bool emptyResponse;
                try
                {
                    // Wait for response headers to be read.
                    bool wait;

                    // Process all informational responses if any and wait for final status.
                    (wait, emptyResponse) = TryEnsureHeaders();
                    if (wait)
                    {
                        await WaitForDataAsync(cancellationToken).ConfigureAwait(false);

                        (wait, emptyResponse) = TryEnsureHeaders();
                        Debug.Assert(!wait);
                    }
                }
                catch
                {
                    Cancel();
                    throw;
                }

                Debug.Assert(_response != null && _response.Content != null);
                // Start to process the response body.
                var responseContent = (HttpConnectionResponseContent)_response.Content;
                if (emptyResponse)
                {
                    // If there are any trailers, copy them over to the response.  Normally this would be handled by
                    // the response stream hitting EOF, but if there is no response body, we do it here.
                    CopyTrailersToResponseMessage(_response);
                    responseContent.SetStream(EmptyReadStream.Instance);
                }
                else
                {
                    responseContent.SetStream(new Http2ReadStream(this));
                }

                // Process Set-Cookie headers.
                if (_connection._pool.Settings._useCookies)
                {
                    CookieHelper.ProcessReceivedCookies(_response, _connection._pool.Settings._cookieContainer!);
                }
            }

            private void ExtendWindow(int amount)
            {
                Debug.Assert(amount > 0);
                Debug.Assert(_pendingWindowUpdate < StreamWindowThreshold);

                if (_responseProtocolState != ResponseProtocolState.ExpectingData)
                {
                    // We are not expecting any more data (because we've either completed or aborted).
                    // So no need to send any more WINDOW_UPDATEs.
                    return;
                }

                _pendingWindowUpdate += amount;
                if (_pendingWindowUpdate < StreamWindowThreshold)
                {
                    return;
                }

                int windowUpdateSize = _pendingWindowUpdate;
                _pendingWindowUpdate = 0;

                _connection.LogExceptions(_connection.SendWindowUpdateAsync(_streamId, windowUpdateSize));
            }

            private (bool wait, int bytesRead) TryReadFromBuffer(Span<byte> buffer, bool partOfSyncRead = false)
            {
                Debug.Assert(buffer.Length > 0);

                Debug.Assert(!Monitor.IsEntered(SyncObject));
                lock (SyncObject)
                {
                    CheckResponseBodyState();

                    if (_responseBuffer.ActiveLength > 0)
                    {
                        int bytesRead = Math.Min(buffer.Length, _responseBuffer.ActiveLength);
                        _responseBuffer.ActiveSpan.Slice(0, bytesRead).CopyTo(buffer);
                        _responseBuffer.Discard(bytesRead);

                        return (false, bytesRead);
                    }
                    else if (_responseProtocolState == ResponseProtocolState.Complete)
                    {
                        return (false, 0);
                    }

                    Debug.Assert(_responseProtocolState == ResponseProtocolState.ExpectingData || _responseProtocolState == ResponseProtocolState.ExpectingTrailingHeaders);

                    Debug.Assert(!_hasWaiter);
                    _hasWaiter = true;
                    _waitSource.Reset();
                    _waitSource.RunContinuationsAsynchronously = !partOfSyncRead;
                    return (true, 0);
                }
            }

            public int ReadData(Span<byte> buffer, HttpResponseMessage responseMessage)
            {
                if (buffer.Length == 0)
                {
                    return 0;
                }

                (bool wait, int bytesRead) = TryReadFromBuffer(buffer, partOfSyncRead: true);
                if (wait)
                {
                    // Synchronously block waiting for data to be produced.
                    Debug.Assert(bytesRead == 0);
                    WaitForData();
                    (wait, bytesRead) = TryReadFromBuffer(buffer, partOfSyncRead: true);
                    Debug.Assert(!wait);
                }

                if (bytesRead != 0)
                {
                    ExtendWindow(bytesRead);
                }
                else
                {
                    // We've hit EOF.  Pull in from the Http2Stream any trailers that were temporarily stored there.
                    CopyTrailersToResponseMessage(responseMessage);
                }

                return bytesRead;
            }

            public async ValueTask<int> ReadDataAsync(Memory<byte> buffer, HttpResponseMessage responseMessage, CancellationToken cancellationToken)
            {
                if (buffer.Length == 0)
                {
                    return 0;
                }

                (bool wait, int bytesRead) = TryReadFromBuffer(buffer.Span);
                if (wait)
                {
                    Debug.Assert(bytesRead == 0);
                    await WaitForDataAsync(cancellationToken).ConfigureAwait(false);
                    (wait, bytesRead) = TryReadFromBuffer(buffer.Span);
                    Debug.Assert(!wait);
                }

                if (bytesRead != 0)
                {
                    ExtendWindow(bytesRead);
                }
                else
                {
                    // We've hit EOF.  Pull in from the Http2Stream any trailers that were temporarily stored there.
                    CopyTrailersToResponseMessage(responseMessage);
                }

                return bytesRead;
            }

            public void CopyTo(HttpResponseMessage responseMessage, Stream destination, int bufferSize)
            {
                byte[] buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
                try
                {
                    // Generallly the same logic as in ReadData, but wrapped in a loop where every read segment is written to the destination.
                    while (true)
                    {
                        (bool wait, int bytesRead) = TryReadFromBuffer(buffer, partOfSyncRead: true);
                        if (wait)
                        {
                            Debug.Assert(bytesRead == 0);
                            WaitForData();
                            (wait, bytesRead) = TryReadFromBuffer(buffer, partOfSyncRead: true);
                            Debug.Assert(!wait);
                        }

                        if (bytesRead != 0)
                        {
                            ExtendWindow(bytesRead);
                            destination.Write(new ReadOnlySpan<byte>(buffer, 0, bytesRead));
                        }
                        else
                        {
                            // We've hit EOF.  Pull in from the Http2Stream any trailers that were temporarily stored there.
                            CopyTrailersToResponseMessage(responseMessage);
                            return;
                        }
                    }
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }
            }

            public async Task CopyToAsync(HttpResponseMessage responseMessage, Stream destination, int bufferSize, CancellationToken cancellationToken)
            {
                byte[] buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
                try
                {
                    // Generallly the same logic as in ReadDataAsync, but wrapped in a loop where every read segment is written to the destination.
                    while (true)
                    {
                        (bool wait, int bytesRead) = TryReadFromBuffer(buffer);
                        if (wait)
                        {
                            Debug.Assert(bytesRead == 0);
                            await WaitForDataAsync(cancellationToken).ConfigureAwait(false);
                            (wait, bytesRead) = TryReadFromBuffer(buffer);
                            Debug.Assert(!wait);
                        }

                        if (bytesRead != 0)
                        {
                            ExtendWindow(bytesRead);
                            await destination.WriteAsync(new ReadOnlyMemory<byte>(buffer, 0, bytesRead), cancellationToken).ConfigureAwait(false);
                        }
                        else
                        {
                            // We've hit EOF.  Pull in from the Http2Stream any trailers that were temporarily stored there.
                            CopyTrailersToResponseMessage(responseMessage);
                            return;
                        }
                    }
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }
            }

            private void CopyTrailersToResponseMessage(HttpResponseMessage responseMessage)
            {
                if (_trailers != null && _trailers.Count > 0)
                {
                    foreach (KeyValuePair<HeaderDescriptor, string> trailer in _trailers)
                    {
                        responseMessage.TrailingHeaders.TryAddWithoutValidation(trailer.Key, trailer.Value);
                    }
                    _trailers.Clear();
                }
            }

            private async ValueTask SendDataAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
            {
                // Deal with [ActiveIssue("https://github.com/dotnet/runtime/issues/17492")]
                // Custom HttpContent classes do not get passed the cancellationToken.
                // So, inject the expected CancellationToken here, to ensure we can cancel the request body send if needed.
                CancellationTokenSource? customCancellationSource = null;
                if (!cancellationToken.CanBeCanceled)
                {
                    cancellationToken = _requestBodyCancellationToken;
                }
                else if (cancellationToken != _requestBodyCancellationToken)
                {
                    // User passed a custom CancellationToken.
                    // We can't tell if it includes our Token or not, so assume it doesn't.
                    customCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _requestBodyCancellationSource!.Token);
                    cancellationToken = customCancellationSource.Token;
                }

                try
                {
                    while (buffer.Length > 0)
                    {
                        int sendSize = -1;
                        lock (SyncObject)
                        {
                            if (_availableCredit > 0)
                            {
                                sendSize = Math.Min(buffer.Length, _availableCredit);
                                _availableCredit -= sendSize;
                            }
                            else
                            {
                                if (_creditWaiter is null)
                                {
                                    _creditWaiter = new CancelableCreditWaiter(SyncObject, cancellationToken);
                                }
                                else
                                {
                                    Debug.Assert(!_creditWaiter.IsPending);
                                    _creditWaiter.ResetForAwait(cancellationToken);
                                }
                                _creditWaiter.Amount = buffer.Length;
                            }
                        }

                        if (sendSize == -1)
                        {
                            Debug.Assert(_creditWaiter != null);
                            sendSize = await _creditWaiter.AsValueTask().ConfigureAwait(false);
                            _creditWaiter.CleanUp();
                        }

                        ReadOnlyMemory<byte> current;
                        (current, buffer) = SplitBuffer(buffer, sendSize);

                        await _connection.SendStreamDataAsync(_streamId, current, cancellationToken).ConfigureAwait(false);
                    }
                }
                finally
                {
                    customCancellationSource?.Dispose();
                }
            }

            private void CloseResponseBody()
            {
                // Check if the response body has been fully consumed.
                bool fullyConsumed = false;
                Debug.Assert(!Monitor.IsEntered(SyncObject));
                lock (SyncObject)
                {
                    if (_responseBuffer.ActiveLength == 0 && _responseProtocolState == ResponseProtocolState.Complete)
                    {
                        fullyConsumed = true;
                    }
                }

                // If the response body isn't completed, cancel it now.
                if (!fullyConsumed)
                {
                    Cancel();
                }

                _responseBuffer.Dispose();
            }

            // This object is itself usable as a backing source for ValueTask.  Since there's only ever one awaiter
            // for this object's state transitions at a time, we allow the object to be awaited directly. All functionality
            // associated with the implementation is just delegated to the ManualResetValueTaskSourceCore.
            ValueTaskSourceStatus IValueTaskSource.GetStatus(short token) => _waitSource.GetStatus(token);
            void IValueTaskSource.OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags) => _waitSource.OnCompleted(continuation, state, token, flags);
            void IValueTaskSource.GetResult(short token)
            {
                Debug.Assert(!Monitor.IsEntered(SyncObject));

                // Clean up the registration.  It's important to Dispose rather than Unregister, so that we wait
                // for any in-flight cancellation to complete.
                _waitSourceCancellation.Dispose();
                _waitSourceCancellation = default;

                // Propagate any exceptions if there were any.
                _waitSource.GetResult(token);
            }

            private void WaitForData()
            {
                // See comments in WaitAsync.
                Debug.Assert(!_waitSource.RunContinuationsAsynchronously);
                new ValueTask(this, _waitSource.Version).AsTask().GetAwaiter().GetResult();
            }

            private ValueTask WaitForDataAsync(CancellationToken cancellationToken)
            {
                Debug.Assert(_waitSource.RunContinuationsAsynchronously);

                // No locking is required here to access _waitSource.  To be here, we've already updated _hasWaiter (while holding the lock)
                // to indicate that we would be creating this waiter, and at that point the only code that could be await'ing _waitSource or
                // Reset'ing it is this code here.  It's possible for this to race with the _waitSource being completed, but that's ok and is
                // handled by _waitSource as one of its primary purposes.  We can't assert _hasWaiter here, though, as once we released the
                // lock, a producer could have seen _hasWaiter as true and both set it to false and signaled _waitSource.

                // With HttpClient, the supplied cancellation token will always be cancelable, as HttpClient supplies a token that
                // will have cancellation requested if CancelPendingRequests is called (or when a non-infinite Timeout expires).
                // However, this could still be non-cancelable if HttpMessageInvoker was used, at which point this will only be
                // cancelable if the caller's token was cancelable.

                _waitSourceCancellation = cancellationToken.UnsafeRegister(s =>
                {
                    var thisRef = (Http2Stream)s!;

                    bool signalWaiter;
                    Debug.Assert(!Monitor.IsEntered(thisRef.SyncObject));
                    lock (thisRef.SyncObject)
                    {
                        signalWaiter = thisRef._hasWaiter;
                        thisRef._hasWaiter = false;
                    }

                    if (signalWaiter)
                    {
                        // Wake up the wait.  It will then immediately check whether cancellation was requested and throw if it was.
                        thisRef._waitSource.SetException(ExceptionDispatchInfo.SetCurrentStackTrace(
                            CancellationHelper.CreateOperationCanceledException(null, _waitSourceCancellation.Token)));
                    }
                }, this);

                // There's a race condition in UnsafeRegister above.  If cancellation is requested prior to UnsafeRegister,
                // the delegate may be invoked synchronously as part of the UnsafeRegister call.  In that case, it will execute
                // before _waitSourceCancellation has been set, which means UnsafeRegister will have set a cancellation
                // exception into the wait source with a default token rather than the ideal one.  To handle that,
                // we check for cancellation again, and throw here with the right token.  Worst case, if cancellation is
                // requested prior to here, we end up allocating an extra OCE object.
                CancellationHelper.ThrowIfCancellationRequested(cancellationToken);

                return new ValueTask(this, _waitSource.Version);
            }

            public void Trace(string message, [CallerMemberName] string? memberName = null) =>
                _connection.Trace(_streamId, message, memberName);

            private enum ResponseProtocolState : byte
            {
                ExpectingStatus,
                ExpectingIgnoredHeaders,
                ExpectingHeaders,
                ExpectingData,
                ExpectingTrailingHeaders,
                Complete,
                Aborted
            }

            private enum StreamCompletionState : byte
            {
                InProgress,
                Completed,
                Failed
            }

            private sealed class Http2ReadStream : HttpBaseStream
            {
                private Http2Stream? _http2Stream;
                private readonly HttpResponseMessage _responseMessage;

                public Http2ReadStream(Http2Stream http2Stream)
                {
                    Debug.Assert(http2Stream != null);
                    Debug.Assert(http2Stream._response != null);
                    _http2Stream = http2Stream;
                    _responseMessage = _http2Stream._response;
                }

                ~Http2ReadStream()
                {
                    if (NetEventSource.IsEnabled) _http2Stream?.Trace("");
                    try
                    {
                        Dispose(disposing: false);
                    }
                    catch (Exception e)
                    {
                        if (NetEventSource.IsEnabled) _http2Stream?.Trace($"Error: {e}");
                    }
                }

                protected override void Dispose(bool disposing)
                {
                    Http2Stream? http2Stream = Interlocked.Exchange(ref _http2Stream, null);
                    if (http2Stream == null)
                    {
                        return;
                    }

                    // Technically we shouldn't be doing the following work when disposing == false,
                    // as the following work relies on other finalizable objects.  But given the HTTP/2
                    // protocol, we have little choice: if someone drops the Http2ReadStream without
                    // disposing of it, we need to a) signal to the server that the stream is being
                    // canceled, and b) clean up the associated state in the Http2Connection.

                    http2Stream.CloseResponseBody();

                    base.Dispose(disposing);
                }

                public override bool CanRead => true;
                public override bool CanWrite => false;

                public override int Read(Span<byte> destination)
                {
                    Http2Stream http2Stream = _http2Stream ?? throw new ObjectDisposedException(nameof(Http2ReadStream));

                    return http2Stream.ReadData(destination, _responseMessage);
                }

                public override ValueTask<int> ReadAsync(Memory<byte> destination, CancellationToken cancellationToken)
                {
                    Http2Stream? http2Stream = _http2Stream;

                    if (http2Stream == null)
                    {
                        return new ValueTask<int>(Task.FromException<int>(ExceptionDispatchInfo.SetCurrentStackTrace(new ObjectDisposedException(nameof(Http2ReadStream)))));
                    }

                    if (cancellationToken.IsCancellationRequested)
                    {
                        return new ValueTask<int>(Task.FromCanceled<int>(cancellationToken));
                    }

                    return http2Stream.ReadDataAsync(destination, _responseMessage, cancellationToken);
                }

                public override void CopyTo(Stream destination, int bufferSize)
                {
                    StreamHelpers.ValidateCopyToArgs(this, destination, bufferSize);
                    Http2Stream http2Stream = _http2Stream ?? throw ExceptionDispatchInfo.SetCurrentStackTrace(new ObjectDisposedException(nameof(Http2ReadStream)));
                    http2Stream.CopyTo(_responseMessage, destination, bufferSize);
                }

                public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
                {
                    StreamHelpers.ValidateCopyToArgs(this, destination, bufferSize);
                    Http2Stream? http2Stream = _http2Stream;
                    return
                        http2Stream is null ? Task.FromException<int>(ExceptionDispatchInfo.SetCurrentStackTrace(new ObjectDisposedException(nameof(Http2ReadStream)))) :
                        cancellationToken.IsCancellationRequested ? Task.FromCanceled<int>(cancellationToken) :
                        http2Stream.CopyToAsync(_responseMessage, destination, bufferSize, cancellationToken);
                }

                public override void Write(ReadOnlySpan<byte> buffer) => throw new NotSupportedException(SR.net_http_content_readonly_stream);

                public override ValueTask WriteAsync(ReadOnlyMemory<byte> destination, CancellationToken cancellationToken) => throw new NotSupportedException();
            }

            private sealed class Http2WriteStream : HttpBaseStream
            {
                private Http2Stream? _http2Stream;

                public Http2WriteStream(Http2Stream http2Stream)
                {
                    Debug.Assert(http2Stream != null);
                    _http2Stream = http2Stream;
                }

                protected override void Dispose(bool disposing)
                {
                    Http2Stream? http2Stream = Interlocked.Exchange(ref _http2Stream, null);
                    if (http2Stream == null)
                    {
                        return;
                    }

                    base.Dispose(disposing);
                }

                public override bool CanRead => false;
                public override bool CanWrite => true;

                public override int Read(Span<byte> buffer) => throw new NotSupportedException();

                public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken) => throw new NotSupportedException();

                public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
                {
                    Http2Stream? http2Stream = _http2Stream;

                    if (http2Stream == null)
                    {
                        return new ValueTask(Task.FromException(new ObjectDisposedException(nameof(Http2WriteStream))));
                    }

                    return http2Stream.SendDataAsync(buffer, cancellationToken);
                }

                public override Task FlushAsync(CancellationToken cancellationToken)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return Task.FromCanceled(cancellationToken);
                    }

                    Http2Stream? http2Stream = _http2Stream;

                    if (http2Stream == null)
                    {
                        return Task.CompletedTask;
                    }

                    // In order to flush this stream's previous writes, we need to flush the connection. We
                    // really only need to do any work here if the connection's buffer has any pending writes
                    // from this stream, but we currently lack a good/efficient/safe way of doing that.
                    return http2Stream._connection.FlushAsync(cancellationToken);
                }
            }
        }
    }
}
