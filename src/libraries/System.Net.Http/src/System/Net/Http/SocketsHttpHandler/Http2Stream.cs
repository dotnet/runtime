// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http.Headers;
using System.Net.Http.HPack;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace System.Net.Http
{
    internal sealed partial class Http2Connection
    {
        private sealed class Http2Stream : IValueTaskSource, IHttpStreamHeadersHandler, IHttpTrace
        {
            private const int InitialStreamBufferSize =
#if DEBUG
                10;
#else
                1024;
#endif

            private static ReadOnlySpan<byte> StatusHeaderName => ":status"u8;

            private readonly Http2Connection _connection;
            private readonly HttpRequestMessage _request;
            private HttpResponseMessage? _response;
            /// <summary>Stores any trailers received after returning the response content to the caller.</summary>
            private HttpResponseHeaders? _trailers;

            private MultiArrayBuffer _responseBuffer; // mutable struct, do not make this readonly
            private Http2StreamWindowManager _windowManager;
            private CreditWaiter? _creditWaiter;
            private int _availableCredit;
            private readonly object _creditSyncObject = new object(); // split from SyncObject to avoid lock ordering problems with Http2Connection.SyncObject

            private StreamCompletionState _requestCompletionState;
            private StreamCompletionState _responseCompletionState;
            private ResponseProtocolState _responseProtocolState;
            private bool _responseHeadersReceived;

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

            private readonly TaskCompletionSource<bool>? _expect100ContinueWaiter;

            private int _headerBudgetRemaining;

            private bool _sendRstOnResponseClose;

            public Http2Stream(HttpRequestMessage request, Http2Connection connection)
            {
                _request = request;
                _connection = connection;

                _requestCompletionState = StreamCompletionState.InProgress;
                _responseCompletionState = StreamCompletionState.InProgress;

                _responseProtocolState = ResponseProtocolState.ExpectingStatus;

                _responseBuffer = new MultiArrayBuffer(InitialStreamBufferSize);

                _windowManager = new Http2StreamWindowManager(connection, this);

                _headerBudgetRemaining = connection._pool.Settings.MaxResponseHeadersByteLength;

                if (_request.Content == null)
                {
                    _requestCompletionState = StreamCompletionState.Completed;
                    if (_request.IsExtendedConnectRequest)
                    {
                        _requestBodyCancellationSource = new CancellationTokenSource();
                    }
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

                _response = new HttpResponseMessage()
                {
                    Version = HttpVersion.Version20,
                    RequestMessage = _request,
                    Content = new HttpConnectionResponseContent()
                };
            }

            private object SyncObject => this; // this isn't handed out to code that may lock on it

            public void Initialize(int streamId, int initialWindowSize)
            {
                StreamId = streamId;
                _availableCredit = initialWindowSize;
                if (NetEventSource.Log.IsEnabled()) Trace($"{nameof(initialWindowSize)}={initialWindowSize}");
            }

            public int StreamId { get; private set; }

            public bool SendRequestFinished => _requestCompletionState != StreamCompletionState.InProgress;

            public bool ExpectResponseData => _responseProtocolState == ResponseProtocolState.ExpectingData;

            public Http2Connection Connection => _connection;

            public bool ConnectProtocolEstablished { get; private set; }

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

                if (NetEventSource.Log.IsEnabled()) Trace($"{_request.Content}");
                Debug.Assert(_requestBodyCancellationSource != null);

                // Cancel the request body sending if cancellation is requested on the supplied cancellation token.
                // Normally we might create a linked token, but once cancellation is requested, we can't recover anyway,
                // so it's fine to cancel the source representing the whole request body, and doing so allows us to avoid
                // creating another CTS instance and the associated nodes inside of it.  With this, cancellation will be
                // requested on _requestBodyCancellationSource when we need to cancel the request stream for any reason,
                // such as receiving an RST_STREAM or when the passed in token has cancellation requested. However, to
                // avoid unnecessarily registering with the cancellation token unless we have to, we wait to do so until
                // either we know we need to do a Expect: 100-continue send or until we know that the copying of our
                // content completed asynchronously.
                CancellationTokenRegistration linkedRegistration = default;
                bool sendRequestContent = true;
                try
                {
                    if (_expect100ContinueWaiter != null)
                    {
                        linkedRegistration = RegisterRequestBodyCancellation(cancellationToken);
                        sendRequestContent = await WaitFor100ContinueAsync(_requestBodyCancellationSource.Token).ConfigureAwait(false);
                    }

                    if (sendRequestContent)
                    {
                        using var writeStream = new Http2WriteStream(this, _request.Content.Headers.ContentLength.GetValueOrDefault(-1));

                        if (HttpTelemetry.Log.IsEnabled()) HttpTelemetry.Log.RequestContentStart();

                        ValueTask vt = _request.Content.InternalCopyToAsync(writeStream, context: null, _requestBodyCancellationSource.Token);
                        if (vt.IsCompleted)
                        {
                            vt.GetAwaiter().GetResult();
                        }
                        else
                        {
                            if (linkedRegistration.Equals(default))
                            {
                                linkedRegistration = RegisterRequestBodyCancellation(cancellationToken);
                            }

                            await vt.ConfigureAwait(false);
                        }

                        if (writeStream.BytesWritten < writeStream.ContentLength)
                        {
                            // The number of bytes we actually sent doesn't match the advertised Content-Length
                            throw new HttpRequestException(SR.Format(SR.net_http_request_content_length_mismatch, writeStream.BytesWritten, writeStream.ContentLength));
                        }

                        if (HttpTelemetry.Log.IsEnabled()) HttpTelemetry.Log.RequestContentStop(writeStream.BytesWritten);
                    }

                    if (NetEventSource.Log.IsEnabled()) Trace($"Finished sending request body.");
                }
                catch (Exception e)
                {
                    if (NetEventSource.Log.IsEnabled()) Trace($"Failed to send request body: {e}");
                    bool signalWaiter;

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
                        bool sendReset;
                        (signalWaiter, sendReset) = CancelResponseBody();
                        Debug.Assert(!sendReset);

                        _requestCompletionState = StreamCompletionState.Failed;
                        SendReset();
                        Complete();
                    }

                    if (signalWaiter)
                    {
                        _waitSource.SetResult(true);
                    }

                    throw;
                }
                finally
                {
                    linkedRegistration.Dispose();
                }

                // New scope here to avoid variable name conflict on "sendReset"
                {
                    Debug.Assert(!Monitor.IsEntered(SyncObject));
                    bool sendReset = false;
                    lock (SyncObject)
                    {
                        Debug.Assert(_requestCompletionState == StreamCompletionState.InProgress, $"Request already completed with state={_requestCompletionState}");
                        _requestCompletionState = StreamCompletionState.Completed;

                        bool complete = false;
                        if (_responseCompletionState != StreamCompletionState.InProgress)
                        {
                            // Note, we can reach this point if the response stream failed but cancellation didn't propagate before we finished.
                            sendReset = _responseCompletionState == StreamCompletionState.Failed;
                            complete = true;
                        }

                        if (sendReset)
                        {
                            SendReset();
                        }
                        else if (!sendRequestContent)
                        {
                            // Request body hasn't been sent, so we need to notify the server that it won't
                            // get the body. However, we cannot do it right here because the server can
                            // reset the whole stream before we will have a chance to read the response body.
                            _sendRstOnResponseClose = true;
                        }
                        else
                        {
                            // Send EndStream asynchronously and without cancellation.
                            // If this fails, it means that the connection is aborting and we will be reset.
                            _connection.LogExceptions(_connection.SendEndStreamAsync(StreamId));
                        }

                        if (complete)
                        {
                            Complete();
                        }
                    }
                }
            }

            // Delay sending request body if we sent Expect: 100-continue.
            // We can either get 100 response from server and send body
            // or we may exceed timeout and send request body anyway.
            // If we get response status >= 300, we will not send the request body.
            public async ValueTask<bool> WaitFor100ContinueAsync(CancellationToken cancellationToken)
            {
                Debug.Assert(_request?.Content != null);
                if (NetEventSource.Log.IsEnabled()) Trace($"Waiting to send request body content for 100-Continue.");

                // Use TCS created in constructor. It will complete when one of three things occurs:
                // 1. we receive the relevant response from the server.
                // 2. the timer fires before we receive the relevant response from the server.
                // 3. cancellation is requested before we receive the relevant response from the server.
                // We need to run the continuation asynchronously for cases 1 and 3 (for 1 so that we don't starve the body copy operation, and
                // for 3 so that we don't run a lot of work as part of code calling Cancel), so the TCS is created to run continuations asynchronously.
                // We await the created Timer's disposal so that we ensure any work associated with it has quiesced prior to this method
                // returning, just in case this object is pooled and potentially reused for another operation in the future.
                TaskCompletionSource<bool> waiter = _expect100ContinueWaiter!;
                using (cancellationToken.UnsafeRegister(static s => ((TaskCompletionSource<bool>)s!).TrySetResult(false), waiter))
                await using (new Timer(static s =>
                {
                    var thisRef = (Http2Stream)s!;
                    if (NetEventSource.Log.IsEnabled()) thisRef.Trace($"100-Continue timer expired.");
                    thisRef._expect100ContinueWaiter?.TrySetResult(true);
                }, this, _connection._pool.Settings._expect100ContinueTimeout, Timeout.InfiniteTimeSpan).ConfigureAwait(false))
                {
                    bool shouldSendContent = await waiter.Task.ConfigureAwait(false);
                    // By now, either we got a response from the server or the timer expired or cancellation was requested.
                    CancellationHelper.ThrowIfCancellationRequested(cancellationToken);
                    return shouldSendContent;
                }
            }

            private void SendReset()
            {
                Debug.Assert(Monitor.IsEntered(SyncObject));
                Debug.Assert(_requestCompletionState != StreamCompletionState.InProgress);
                Debug.Assert(_responseCompletionState != StreamCompletionState.InProgress);
                Debug.Assert(_requestCompletionState == StreamCompletionState.Failed || _responseCompletionState == StreamCompletionState.Failed,
                    "Reset called but neither request nor response is failed");

                if (NetEventSource.Log.IsEnabled()) Trace($"Stream reset. Request={_requestCompletionState}, Response={_responseCompletionState}.");

                // Don't send a RST_STREAM if we've already received one from the server.
                if (_resetException == null)
                {
                    // If execution reached this line, it's guaranteed that
                    // _requestCompletionState == StreamCompletionState.Failed or _responseCompletionState == StreamCompletionState.Failed
                    _connection.LogExceptions(_connection.SendRstStreamAsync(StreamId, Http2ProtocolErrorCode.Cancel));
                }
            }

            private void Complete()
            {
                Debug.Assert(Monitor.IsEntered(SyncObject));
                Debug.Assert(_requestCompletionState != StreamCompletionState.InProgress);
                Debug.Assert(_responseCompletionState != StreamCompletionState.InProgress);

                if (NetEventSource.Log.IsEnabled()) Trace($"Stream complete. Request={_requestCompletionState}, Response={_responseCompletionState}.");

                _connection.RemoveStream(this);

                lock (_creditSyncObject)
                {
                    CreditWaiter? waiter = _creditWaiter;
                    if (waiter != null)
                    {
                        waiter.Dispose();
                        _creditWaiter = null;
                    }
                }
            }

            private void Cancel()
            {
                if (NetEventSource.Log.IsEnabled()) Trace("");

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

                // When cancellation propagates, SendRequestBodyAsync will set _requestCompletionState to Failed
                requestBodyCancellationSource?.Cancel();

                lock (SyncObject)
                {
                    if (sendReset)
                    {
                        SendReset();
                        Complete();
                    }
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

                bool sendReset = _sendRstOnResponseClose;

                if (_responseCompletionState == StreamCompletionState.InProgress)
                {
                    _responseCompletionState = StreamCompletionState.Failed;
                    if (_requestCompletionState != StreamCompletionState.InProgress)
                    {
                        sendReset = true;
                    }
                }

                // Discard any remaining buffered response data
                _responseBuffer.DiscardAll();

                _responseProtocolState = ResponseProtocolState.Aborted;

                bool signalWaiter = _hasWaiter;
                _hasWaiter = false;

                return (signalWaiter, sendReset);
            }

            public void OnWindowUpdate(int amount)
            {
                lock (_creditSyncObject)
                {
                    _availableCredit = checked(_availableCredit + amount);
                    if (_availableCredit > 0 && _creditWaiter != null)
                    {
                        int granted = Math.Min(_availableCredit, _creditWaiter.Amount);
                        if (_creditWaiter.TrySetResult(granted))
                        {
                            _availableCredit -= granted;
                        }
                    }
                }
            }

            private const int FirstHPackRequestPseudoHeaderId = 1;
            private const int LastHPackRequestPseudoHeaderId = 7;
            private const int FirstHPackStatusPseudoHeaderId = 8;
            private const int LastHPackStatusPseudoHeaderId = 14;
            private const int FirstHPackNormalHeaderId = 15;
            private const int LastHPackNormalHeaderId = 61;

            private static ReadOnlySpan<int> HpackStaticStatusCodeTable => new int[LastHPackStatusPseudoHeaderId - FirstHPackStatusPseudoHeaderId + 1] { 200, 204, 206, 304, 400, 404, 500 };

            private static readonly (HeaderDescriptor descriptor, byte[] value)[] s_hpackStaticHeaderTable = new (HeaderDescriptor, byte[])[LastHPackNormalHeaderId - FirstHPackNormalHeaderId + 1]
            {
                (KnownHeaders.AcceptCharset.Descriptor, Array.Empty<byte>()),
                (KnownHeaders.AcceptEncoding.Descriptor, "gzip, deflate"u8.ToArray()),
                (KnownHeaders.AcceptLanguage.Descriptor, Array.Empty<byte>()),
                (KnownHeaders.AcceptRanges.Descriptor, Array.Empty<byte>()),
                (KnownHeaders.Accept.Descriptor, Array.Empty<byte>()),
                (KnownHeaders.AccessControlAllowOrigin.Descriptor, Array.Empty<byte>()),
                (KnownHeaders.Age.Descriptor, Array.Empty<byte>()),
                (KnownHeaders.Allow.Descriptor, Array.Empty<byte>()),
                (KnownHeaders.Authorization.Descriptor, Array.Empty<byte>()),
                (KnownHeaders.CacheControl.Descriptor, Array.Empty<byte>()),
                (KnownHeaders.ContentDisposition.Descriptor, Array.Empty<byte>()),
                (KnownHeaders.ContentEncoding.Descriptor, Array.Empty<byte>()),
                (KnownHeaders.ContentLanguage.Descriptor, Array.Empty<byte>()),
                (KnownHeaders.ContentLength.Descriptor, Array.Empty<byte>()),
                (KnownHeaders.ContentLocation.Descriptor, Array.Empty<byte>()),
                (KnownHeaders.ContentRange.Descriptor, Array.Empty<byte>()),
                (KnownHeaders.ContentType.Descriptor, Array.Empty<byte>()),
                (KnownHeaders.Cookie.Descriptor, Array.Empty<byte>()),
                (KnownHeaders.Date.Descriptor, Array.Empty<byte>()),
                (KnownHeaders.ETag.Descriptor, Array.Empty<byte>()),
                (KnownHeaders.Expect.Descriptor, Array.Empty<byte>()),
                (KnownHeaders.Expires.Descriptor, Array.Empty<byte>()),
                (KnownHeaders.From.Descriptor, Array.Empty<byte>()),
                (KnownHeaders.Host.Descriptor, Array.Empty<byte>()),
                (KnownHeaders.IfMatch.Descriptor, Array.Empty<byte>()),
                (KnownHeaders.IfModifiedSince.Descriptor, Array.Empty<byte>()),
                (KnownHeaders.IfNoneMatch.Descriptor, Array.Empty<byte>()),
                (KnownHeaders.IfRange.Descriptor, Array.Empty<byte>()),
                (KnownHeaders.IfUnmodifiedSince.Descriptor, Array.Empty<byte>()),
                (KnownHeaders.LastModified.Descriptor, Array.Empty<byte>()),
                (KnownHeaders.Link.Descriptor, Array.Empty<byte>()),
                (KnownHeaders.Location.Descriptor, Array.Empty<byte>()),
                (KnownHeaders.MaxForwards.Descriptor, Array.Empty<byte>()),
                (KnownHeaders.ProxyAuthenticate.Descriptor, Array.Empty<byte>()),
                (KnownHeaders.ProxyAuthorization.Descriptor, Array.Empty<byte>()),
                (KnownHeaders.Range.Descriptor, Array.Empty<byte>()),
                (KnownHeaders.Referer.Descriptor, Array.Empty<byte>()),
                (KnownHeaders.Refresh.Descriptor, Array.Empty<byte>()),
                (KnownHeaders.RetryAfter.Descriptor, Array.Empty<byte>()),
                (KnownHeaders.Server.Descriptor, Array.Empty<byte>()),
                (KnownHeaders.SetCookie.Descriptor, Array.Empty<byte>()),
                (KnownHeaders.StrictTransportSecurity.Descriptor, Array.Empty<byte>()),
                (KnownHeaders.TransferEncoding.Descriptor, Array.Empty<byte>()),
                (KnownHeaders.UserAgent.Descriptor, Array.Empty<byte>()),
                (KnownHeaders.Vary.Descriptor, Array.Empty<byte>()),
                (KnownHeaders.Via.Descriptor, Array.Empty<byte>()),
                (KnownHeaders.WWWAuthenticate.Descriptor, Array.Empty<byte>()),
            };

            void IHttpStreamHeadersHandler.OnStaticIndexedHeader(int index)
            {
                Debug.Assert(index >= FirstHPackRequestPseudoHeaderId && index <= LastHPackNormalHeaderId);

                if (index <= LastHPackRequestPseudoHeaderId)
                {
                    if (NetEventSource.Log.IsEnabled()) Trace($"Invalid request pseudo-header ID {index}.");
                    throw new HttpRequestException(SR.net_http_invalid_response, httpRequestError: HttpRequestError.InvalidResponse);
                }
                else if (index <= LastHPackStatusPseudoHeaderId)
                {
                    int statusCode = HpackStaticStatusCodeTable[index - FirstHPackStatusPseudoHeaderId];

                    OnStatus(statusCode);
                }
                else
                {
                    (HeaderDescriptor descriptor, byte[] value) = s_hpackStaticHeaderTable[index - FirstHPackNormalHeaderId];

                    OnHeader(descriptor, value);
                }
            }

            void IHttpStreamHeadersHandler.OnStaticIndexedHeader(int index, ReadOnlySpan<byte> value)
            {
                Debug.Assert(index >= FirstHPackRequestPseudoHeaderId && index <= LastHPackNormalHeaderId);

                if (index <= LastHPackRequestPseudoHeaderId)
                {
                    if (NetEventSource.Log.IsEnabled()) Trace($"Invalid request pseudo-header ID {index}.");
                    throw new HttpRequestException(SR.net_http_invalid_response, httpRequestError: HttpRequestError.InvalidResponse);
                }
                else if (index <= LastHPackStatusPseudoHeaderId)
                {
                    int statusCode = ParseStatusCode(value);

                    OnStatus(statusCode);
                }
                else
                {
                    (HeaderDescriptor descriptor, _) = s_hpackStaticHeaderTable[index - FirstHPackNormalHeaderId];

                    OnHeader(descriptor, value);
                }
            }

            void IHttpStreamHeadersHandler.OnDynamicIndexedHeader(int? index, ReadOnlySpan<byte> name, ReadOnlySpan<byte> value)
            {
                OnHeader(name, value);
            }

            private void AdjustHeaderBudget(int amount)
            {
                _headerBudgetRemaining -= amount;
                if (_headerBudgetRemaining < 0)
                {
                    throw new HttpRequestException(SR.Format(SR.net_http_response_headers_exceeded_length, _connection._pool.Settings.MaxResponseHeadersByteLength), httpRequestError: HttpRequestError.ConfigurationLimitExceeded);
                }
            }

            private void OnStatus(int statusCode)
            {
                if (NetEventSource.Log.IsEnabled()) Trace($"Status code is {statusCode}");

                AdjustHeaderBudget(10); // for ":status" plus 3-digit status code

                Debug.Assert(!Monitor.IsEntered(SyncObject));
                lock (SyncObject)
                {
                    if (_responseProtocolState == ResponseProtocolState.Aborted)
                    {
                        // We could have aborted while processing the header block.
                        return;
                    }

                    if (_responseProtocolState == ResponseProtocolState.ExpectingHeaders)
                    {
                        if (NetEventSource.Log.IsEnabled()) Trace("Received extra status header.");
                        throw new HttpRequestException(SR.net_http_invalid_response_multiple_status_codes, httpRequestError: HttpRequestError.ConfigurationLimitExceeded);
                    }

                    if (_responseProtocolState != ResponseProtocolState.ExpectingStatus)
                    {
                        // Pseudo-headers are allowed only in header block
                        if (NetEventSource.Log.IsEnabled()) Trace($"Status pseudo-header received in {_responseProtocolState} state.");
                        throw new HttpRequestException(SR.net_http_invalid_response_pseudo_header_in_trailer, httpRequestError: HttpRequestError.InvalidResponse);
                    }

                    Debug.Assert(_response != null);
                    _response.StatusCode = (HttpStatusCode)statusCode;

                    if (statusCode < 200)
                    {
                        // We do not process headers from 1xx responses.
                        _responseProtocolState = ResponseProtocolState.ExpectingIgnoredHeaders;

                        if (_response.StatusCode == HttpStatusCode.Continue && _expect100ContinueWaiter != null)
                        {
                            if (NetEventSource.Log.IsEnabled()) Trace("Received 100-Continue status.");
                            _expect100ContinueWaiter.TrySetResult(true);
                        }
                    }
                    else
                    {
                        if (statusCode == 200 && _response.RequestMessage!.IsExtendedConnectRequest)
                        {
                            ConnectProtocolEstablished = true;
                        }

                        _responseProtocolState = ResponseProtocolState.ExpectingHeaders;

                        // If we are waiting for a 100-continue response, signal the waiter now.
                        if (_expect100ContinueWaiter != null)
                        {
                            // If the final status code is >= 300, skip sending the body.
                            bool shouldSendBody = (statusCode < 300);

                            if (NetEventSource.Log.IsEnabled()) Trace($"Expecting 100 Continue but received final status {statusCode}.");
                            _expect100ContinueWaiter.TrySetResult(shouldSendBody);
                        }
                    }
                }
            }

            private void OnHeader(HeaderDescriptor descriptor, ReadOnlySpan<byte> value)
            {
                if (NetEventSource.Log.IsEnabled()) Trace($"{descriptor.Name}: {Encoding.ASCII.GetString(value)}");

                AdjustHeaderBudget(descriptor.Name.Length + value.Length);

                Debug.Assert(!Monitor.IsEntered(SyncObject));
                lock (SyncObject)
                {
                    if (_responseProtocolState == ResponseProtocolState.Aborted)
                    {
                        // We could have aborted while processing the header block.
                        return;
                    }

                    if (_responseProtocolState == ResponseProtocolState.ExpectingIgnoredHeaders)
                    {
                        // for 1xx response we ignore all headers.
                        return;
                    }

                    if (_responseProtocolState != ResponseProtocolState.ExpectingHeaders && _responseProtocolState != ResponseProtocolState.ExpectingTrailingHeaders)
                    {
                        if (NetEventSource.Log.IsEnabled()) Trace("Received header before status.");
                        throw new HttpRequestException(SR.net_http_invalid_response, httpRequestError: HttpRequestError.InvalidResponse);
                    }

                    Encoding? valueEncoding = _connection._pool.Settings._responseHeaderEncodingSelector?.Invoke(descriptor.Name, _request);

                    // Note we ignore the return value from TryAddWithoutValidation;
                    // if the header can't be added, we silently drop it.
                    if (_responseProtocolState == ResponseProtocolState.ExpectingTrailingHeaders)
                    {
                        Debug.Assert(_trailers != null);
                        string headerValue = descriptor.GetHeaderValue(value, valueEncoding);
                        _trailers.TryAddWithoutValidation((descriptor.HeaderType & HttpHeaderType.Request) == HttpHeaderType.Request ? descriptor.AsCustomHeader() : descriptor, headerValue);
                    }
                    else if ((descriptor.HeaderType & HttpHeaderType.Content) == HttpHeaderType.Content)
                    {
                        Debug.Assert(_response != null && _response.Content != null);
                        string headerValue = descriptor.GetHeaderValue(value, valueEncoding);
                        _response.Content.Headers.TryAddWithoutValidation(descriptor, headerValue);
                    }
                    else
                    {
                        Debug.Assert(_response != null);
                        string headerValue = _connection.GetResponseHeaderValueWithCaching(descriptor, value, valueEncoding);
                        _response.Headers.TryAddWithoutValidation((descriptor.HeaderType & HttpHeaderType.Request) == HttpHeaderType.Request ? descriptor.AsCustomHeader() : descriptor, headerValue);
                    }
                }
            }

            public void OnHeader(ReadOnlySpan<byte> name, ReadOnlySpan<byte> value)
            {
                Debug.Assert(name.Length > 0);

                if (name[0] == (byte)':')
                {
                    // Pseudo-header
                    if (name.SequenceEqual(StatusHeaderName))
                    {
                        int statusCode = ParseStatusCode(value);

                        OnStatus(statusCode);
                    }
                    else
                    {
                        if (NetEventSource.Log.IsEnabled()) Trace($"Invalid response pseudo-header '{Encoding.ASCII.GetString(name)}'.");
                        throw new HttpRequestException(SR.net_http_invalid_response, httpRequestError: HttpRequestError.InvalidResponse);
                    }
                }
                else
                {
                    // Regular header
                    if (!HeaderDescriptor.TryGet(name, out HeaderDescriptor descriptor))
                    {
                        // Invalid header name
                        throw new HttpRequestException(SR.Format(SR.net_http_invalid_response_header_name, Encoding.ASCII.GetString(name)), httpRequestError: HttpRequestError.InvalidResponse);
                    }

                    OnHeader(descriptor, value);
                }
            }

            public void OnHeadersStart()
            {
                Debug.Assert(!Monitor.IsEntered(SyncObject));
                lock (SyncObject)
                {
                    switch (_responseProtocolState)
                    {
                        case ResponseProtocolState.ExpectingStatus:
                        case ResponseProtocolState.Aborted:
                            break;

                        case ResponseProtocolState.ExpectingData:
                            _responseProtocolState = ResponseProtocolState.ExpectingTrailingHeaders;
                            _trailers ??= new HttpResponseHeaders(containsTrailingHeaders: true);
                            break;

                        default:
                            ThrowProtocolError();
                            break;
                    }
                }
            }

            public void OnHeadersComplete(bool endStream)
            {
                Debug.Assert(!Monitor.IsEntered(SyncObject));
                bool signalWaiter;
                lock (SyncObject)
                {
                    switch (_responseProtocolState)
                    {
                        case ResponseProtocolState.Aborted:
                            return;

                        case ResponseProtocolState.ExpectingHeaders:
                            _responseProtocolState = endStream ? ResponseProtocolState.Complete : ResponseProtocolState.ExpectingData;
                            _responseHeadersReceived = true;
                            break;

                        case ResponseProtocolState.ExpectingTrailingHeaders:
                            if (!endStream)
                            {
                                if (NetEventSource.Log.IsEnabled()) Trace("Trailing headers received without endStream");
                                ThrowProtocolError();
                            }
                            _responseProtocolState = ResponseProtocolState.Complete;
                            break;

                        case ResponseProtocolState.ExpectingIgnoredHeaders:
                            if (endStream)
                            {
                                // we should not get endStream while processing 1xx response.
                                ThrowProtocolError();
                            }

                            // We should wait for final response before signaling to waiter.
                            _responseProtocolState = ResponseProtocolState.ExpectingStatus;
                            return;

                        default:
                            ThrowProtocolError();
                            break;
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

                    if (_responseProtocolState == ResponseProtocolState.ExpectingData)
                    {
                        _windowManager.Start();
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
                    switch (_responseProtocolState)
                    {
                        case ResponseProtocolState.ExpectingData:
                            break;

                        case ResponseProtocolState.Aborted:
                            return;

                        default:
                            // Flow control messages are not valid in this state.
                            ThrowProtocolError();
                            break;
                    }

                    if (_responseBuffer.ActiveMemory.Length + buffer.Length > _windowManager.StreamWindowSize)
                    {
                        // Window size exceeded.
                        ThrowProtocolError(Http2ProtocolErrorCode.FlowControlError);
                    }

                    _responseBuffer.EnsureAvailableSpace(buffer.Length);
                    _responseBuffer.AvailableMemory.CopyFrom(buffer);
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
                if (NetEventSource.Log.IsEnabled()) Trace($"{nameof(resetException)}={resetException}, {nameof(resetStreamErrorCode)}={resetStreamErrorCode}");

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

                if (_resetException is Exception resetException)
                {
                    if (_canRetry)
                    {
                        ThrowRetry(SR.net_http_request_aborted, resetException);
                    }

                    ThrowRequestAborted(resetException);
                }

                if (_responseProtocolState == ResponseProtocolState.Aborted)
                {
                    ThrowRequestAborted();
                }
            }

            // Determine if we have enough data to process up to complete final response headers.
            private (bool wait, bool isEmptyResponse) TryEnsureHeaders()
            {
                Debug.Assert(!Monitor.IsEntered(SyncObject));
                lock (SyncObject)
                {
                    if (!_responseHeadersReceived)
                    {
                        CheckResponseBodyState();
                        Debug.Assert(!_hasWaiter);
                        _hasWaiter = true;
                        _waitSource.Reset();
                        return (true, false);
                    }

                    return (false, _responseProtocolState == ResponseProtocolState.Complete && _responseBuffer.IsEmpty);
                }
            }

            public async Task ReadResponseHeadersAsync(CancellationToken cancellationToken)
            {
                bool emptyResponse;
                try
                {
                    if (HttpTelemetry.Log.IsEnabled()) HttpTelemetry.Log.ResponseHeadersStart();

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

                    Debug.Assert(_response is not null);
                    if (HttpTelemetry.Log.IsEnabled()) HttpTelemetry.Log.ResponseHeadersStop((int)_response.StatusCode);
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
                    MoveTrailersToResponseMessage(_response);
                    responseContent.SetStream(EmptyReadStream.Instance);
                }
                else if (ConnectProtocolEstablished)
                {
                    responseContent.SetStream(new Http2ReadWriteStream(this));
                }
                else
                {
                    responseContent.SetStream(new Http2ReadStream(this));
                }
                if (NetEventSource.Log.IsEnabled()) Trace($"Received response: {_response}");

                // Process Set-Cookie headers.
                if (_connection._pool.Settings._useCookies)
                {
                    CookieHelper.ProcessReceivedCookies(_response, _connection._pool.Settings._cookieContainer!);
                }
            }

            private (bool wait, int bytesRead) TryReadFromBuffer(Span<byte> buffer, bool partOfSyncRead = false)
            {
                Debug.Assert(!Monitor.IsEntered(SyncObject));
                lock (SyncObject)
                {
                    CheckResponseBodyState();

                    if (!_responseBuffer.IsEmpty)
                    {
                        MultiMemory activeBuffer = _responseBuffer.ActiveMemory;
                        int bytesRead = Math.Min(buffer.Length, activeBuffer.Length);
                        activeBuffer.Slice(0, bytesRead).CopyTo(buffer);
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
                    _windowManager.AdjustWindow(bytesRead, this);
                }
                else if (buffer.Length != 0)
                {
                    // We've hit EOF.  Pull in from the Http2Stream any trailers that were temporarily stored there.
                    MoveTrailersToResponseMessage(responseMessage);
                }

                return bytesRead;
            }

            public async ValueTask<int> ReadDataAsync(Memory<byte> buffer, HttpResponseMessage responseMessage, CancellationToken cancellationToken)
            {
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
                    _windowManager.AdjustWindow(bytesRead, this);
                }
                else if (buffer.Length != 0)
                {
                    // We've hit EOF.  Pull in from the Http2Stream any trailers that were temporarily stored there.
                    MoveTrailersToResponseMessage(responseMessage);
                }

                return bytesRead;
            }

            public void CopyTo(HttpResponseMessage responseMessage, Stream destination, int bufferSize)
            {
                byte[] buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
                try
                {
                    // Generally the same logic as in ReadData, but wrapped in a loop where every read segment is written to the destination.
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
                            _windowManager.AdjustWindow(bytesRead, this);
                            destination.Write(new ReadOnlySpan<byte>(buffer, 0, bytesRead));
                        }
                        else
                        {
                            // We've hit EOF.  Pull in from the Http2Stream any trailers that were temporarily stored there.
                            MoveTrailersToResponseMessage(responseMessage);
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
                    // Generally the same logic as in ReadDataAsync, but wrapped in a loop where every read segment is written to the destination.
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
                            _windowManager.AdjustWindow(bytesRead, this);
                            await destination.WriteAsync(new ReadOnlyMemory<byte>(buffer, 0, bytesRead), cancellationToken).ConfigureAwait(false);
                        }
                        else
                        {
                            // We've hit EOF.  Pull in from the Http2Stream any trailers that were temporarily stored there.
                            MoveTrailersToResponseMessage(responseMessage);
                            return;
                        }
                    }
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }
            }

            private void MoveTrailersToResponseMessage(HttpResponseMessage responseMessage)
            {
                if (_trailers != null)
                {
                    responseMessage.StoreReceivedTrailingHeaders(_trailers);
                }
            }

            private async ValueTask SendDataAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
            {
                Debug.Assert(_requestBodyCancellationSource != null);

                // Cancel the request body sending if cancellation is requested on the supplied cancellation token.
                CancellationTokenRegistration linkedRegistration = cancellationToken.CanBeCanceled && cancellationToken != _requestBodyCancellationSource.Token ?
                    RegisterRequestBodyCancellation(cancellationToken) :
                    default;

                try
                {
                    while (buffer.Length > 0)
                    {
                        int sendSize = -1;
                        bool flush = false;
                        lock (_creditSyncObject)
                        {
                            if (_availableCredit > 0)
                            {
                                sendSize = Math.Min(buffer.Length, _availableCredit);
                                _availableCredit -= sendSize;

                                // Force a flush if we are out of credit, because we don't know that we will be sending more data any time soon
                                if (_availableCredit == 0)
                                {
                                    flush = true;
                                }
                            }
                            else
                            {
                                if (_creditWaiter is null)
                                {
                                    _creditWaiter = new CreditWaiter(_requestBodyCancellationSource.Token);
                                }
                                else
                                {
                                    _creditWaiter.ResetForAwait(_requestBodyCancellationSource.Token);
                                }
                                _creditWaiter.Amount = buffer.Length;
                            }
                        }

                        if (sendSize == -1)
                        {
                            // Logically this is part of the else block above, but we can't await while holding the lock.
                            Debug.Assert(_creditWaiter != null);
                            sendSize = await _creditWaiter.AsValueTask().ConfigureAwait(false);

                            lock (_creditSyncObject)
                            {
                                // Force a flush if we are out of credit, because we don't know that we will be sending more data any time soon
                                if (_availableCredit == 0)
                                {
                                    flush = true;
                                }
                            }
                        }

                        Debug.Assert(sendSize > 0);

                        ReadOnlyMemory<byte> current;
                        (current, buffer) = SplitBuffer(buffer, sendSize);

                        await _connection.SendStreamDataAsync(StreamId, current, flush, _requestBodyCancellationSource.Token).ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException e) when (e.CancellationToken == _requestBodyCancellationSource.Token)
                {
                    lock (SyncObject)
                    {
                        if (_resetException is Exception resetException)
                        {
                            if (_canRetry)
                            {
                                ThrowRetry(SR.net_http_request_aborted, resetException);
                            }

                            ThrowRequestAborted(resetException);
                        }
                    }

                    throw;
                }
                finally
                {
                    linkedRegistration.Dispose();
                }
            }

            private void CloseResponseBody()
            {
                // Check if the response body has been fully consumed.
                bool fullyConsumed = false;
                Debug.Assert(!Monitor.IsEntered(SyncObject));
                lock (SyncObject)
                {
                    if (_responseBuffer.IsEmpty && _responseProtocolState == ResponseProtocolState.Complete)
                    {
                        fullyConsumed = true;
                    }
                }

                // If the response body isn't completed, cancel it now.
                if (!fullyConsumed)
                {
                    Cancel();
                }
                else if (_sendRstOnResponseClose)
                {
                    // Send RST_STREAM with CANCEL to notify the server that it shouldn't
                    // expect the request body.
                    // If this fails, it means that the connection is aborting and we will be reset.
                    _connection.LogExceptions(_connection.SendRstStreamAsync(StreamId, Http2ProtocolErrorCode.Cancel));
                }

                lock (SyncObject)
                {
                    _responseBuffer.Dispose();
                }
            }

            private CancellationTokenRegistration RegisterRequestBodyCancellation(CancellationToken cancellationToken) =>
                cancellationToken.UnsafeRegister(static s => ((CancellationTokenSource)s!).Cancel(), _requestBodyCancellationSource);

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

                _waitSourceCancellation = cancellationToken.UnsafeRegister(static (s, cancellationToken) =>
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
                            CancellationHelper.CreateOperationCanceledException(null, cancellationToken)));
                    }
                }, this);

                return new ValueTask(this, _waitSource.Version);
            }

            public void Trace(string message, [CallerMemberName] string? memberName = null) =>
                _connection.Trace(StreamId, message, memberName);

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

            private sealed class Http2ReadStream : Http2ReadWriteStream
            {
                public Http2ReadStream(Http2Stream http2Stream) : base(http2Stream)
                {
                    base.CloseResponseBodyOnDispose = true;
                }

                public override bool CanWrite => false;

                public override void Write(ReadOnlySpan<byte> buffer) => throw new NotSupportedException(SR.net_http_content_readonly_stream);

                public override ValueTask WriteAsync(ReadOnlyMemory<byte> destination, CancellationToken cancellationToken) => ValueTask.FromException(new NotSupportedException(SR.net_http_content_readonly_stream));
            }

            private sealed class Http2WriteStream : Http2ReadWriteStream
            {
                public long BytesWritten { get; private set; }

                public long ContentLength { get; }

                public Http2WriteStream(Http2Stream http2Stream, long contentLength) : base(http2Stream)
                {
                    Debug.Assert(contentLength >= -1);
                    ContentLength = contentLength;
                }

                public override bool CanRead => false;

                public override int Read(Span<byte> buffer) => throw new NotSupportedException(SR.net_http_content_writeonly_stream);

                public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken) => ValueTask.FromException<int>(new NotSupportedException(SR.net_http_content_writeonly_stream));

                public override void CopyTo(Stream destination, int bufferSize) => throw new NotSupportedException(SR.net_http_content_writeonly_stream);

                public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken) => Task.FromException(new NotSupportedException(SR.net_http_content_writeonly_stream));

                public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
                {
                    BytesWritten += buffer.Length;

                    if ((ulong)BytesWritten > (ulong)ContentLength) // If ContentLength == -1, this will always be false
                    {
                        return ValueTask.FromException(new HttpRequestException(SR.net_http_content_write_larger_than_content_length));
                    }

                    return base.WriteAsync(buffer, cancellationToken);
                }
            }

            public class Http2ReadWriteStream : HttpBaseStream
            {
                private Http2Stream? _http2Stream;
                private readonly HttpResponseMessage _responseMessage;

                public Http2ReadWriteStream(Http2Stream http2Stream)
                {
                    Debug.Assert(http2Stream != null);
                    Debug.Assert(http2Stream._response != null);
                    _http2Stream = http2Stream;
                    _responseMessage = _http2Stream._response;
                }

                ~Http2ReadWriteStream()
                {
                    if (NetEventSource.Log.IsEnabled()) _http2Stream?.Trace("");
                    try
                    {
                        Dispose(disposing: false);
                    }
                    catch (Exception e)
                    {
                        if (NetEventSource.Log.IsEnabled()) _http2Stream?.Trace($"Error: {e}");
                    }
                }

                protected bool CloseResponseBodyOnDispose { get; set; }

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
                    if (CloseResponseBodyOnDispose)
                    {
                        http2Stream.CloseResponseBody();
                    }

                    base.Dispose(disposing);
                }

                public override bool CanRead => _http2Stream != null;
                public override bool CanWrite => _http2Stream != null;

                public override int Read(Span<byte> destination)
                {
                    Http2Stream? http2Stream = _http2Stream;
                    ObjectDisposedException.ThrowIf(http2Stream is null, this);

                    return http2Stream.ReadData(destination, _responseMessage);
                }

                public override ValueTask<int> ReadAsync(Memory<byte> destination, CancellationToken cancellationToken)
                {
                    Http2Stream? http2Stream = _http2Stream;

                    if (http2Stream == null)
                    {
                        return ValueTask.FromException<int>(ExceptionDispatchInfo.SetCurrentStackTrace(new ObjectDisposedException(nameof(Http2ReadStream))));
                    }

                    if (cancellationToken.IsCancellationRequested)
                    {
                        return ValueTask.FromCanceled<int>(cancellationToken);
                    }

                    return http2Stream.ReadDataAsync(destination, _responseMessage, cancellationToken);
                }

                public override void CopyTo(Stream destination, int bufferSize)
                {
                    ValidateCopyToArguments(destination, bufferSize);
                    Http2Stream http2Stream = _http2Stream ?? throw ExceptionDispatchInfo.SetCurrentStackTrace(new ObjectDisposedException(nameof(Http2ReadStream)));
                    http2Stream.CopyTo(_responseMessage, destination, bufferSize);
                }

                public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
                {
                    ValidateCopyToArguments(destination, bufferSize);
                    Http2Stream? http2Stream = _http2Stream;
                    return
                        http2Stream is null ? Task.FromException<int>(ExceptionDispatchInfo.SetCurrentStackTrace(new ObjectDisposedException(nameof(Http2ReadStream)))) :
                        cancellationToken.IsCancellationRequested ? Task.FromCanceled<int>(cancellationToken) :
                        http2Stream.CopyToAsync(_responseMessage, destination, bufferSize, cancellationToken);
                }

                public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
                {

                    Http2Stream? http2Stream = _http2Stream;

                    if (http2Stream == null)
                    {
                        return ValueTask.FromException(new ObjectDisposedException(nameof(Http2WriteStream)));
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
