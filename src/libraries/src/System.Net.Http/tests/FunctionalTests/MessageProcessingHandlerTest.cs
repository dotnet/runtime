// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

namespace System.Net.Http.Functional.Tests
{
    public class MessageProcessingHandlerTest
    {
        [Fact]
        public void Ctor_CreateDispose_Success()
        {
            using (var handler = new MockHandler())
            {
                Assert.Null(handler.InnerHandler);
            }
        }

        [Fact]
        public void Ctor_CreateWithHandlerDispose_Success()
        {
            using (var handler = new MockHandler(new MockTransportHandler()))
            {
                Assert.NotNull(handler.InnerHandler);
            }
        }

        [Fact]
        public void Ctor_CreateWithNullHandler_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new MockHandler(null));
        }

        [Fact]
        public void SendAsync_NullRequest_ThrowsArgumentNullException()
        {
            var transport = new MockTransportHandler();
            var handler = new MockHandler(transport);

            Assert.Throws<ArgumentNullException>(() => { Task t = handler.TestSendAsync(null, CancellationToken.None); });
        }

        [Fact]
        public async Task SendAsync_CallMethod_ProcessRequestAndProcessResponseCalled()
        {
            var transport = new MockTransportHandler();
            var handler = new MockHandler(transport);

            await handler.TestSendAsync(new HttpRequestMessage(), CancellationToken.None);

            Assert.Equal(1, handler.ProcessRequestCount);
            Assert.Equal(1, handler.ProcessResponseCount);
        }

        [Fact]
        public async Task SendAsync_InnerHandlerThrows_ThrowWithoutCallingProcessRequest()
        {
            var transport = new MockTransportHandler(true); // Throw if Send/SendAsync() is called.
            var handler = new MockHandler(transport);

            await Assert.ThrowsAsync<MockException>(() => handler.TestSendAsync(new HttpRequestMessage(), CancellationToken.None));

            Assert.Equal(1, handler.ProcessRequestCount);
            Assert.Equal(0, handler.ProcessResponseCount);
        }

        [Fact]
        public async Task SendAsync_InnerHandlerReturnsNullResponse_ThrowInvalidOperationExceptionWithoutCallingProcessRequest()
        {
            var transport = new MockTransportHandler(() => { return null; });
            var handler = new MockHandler(transport);

            await Assert.ThrowsAsync<InvalidOperationException>(() => handler.TestSendAsync(new HttpRequestMessage(), CancellationToken.None));

            Assert.Equal(1, handler.ProcessRequestCount);
            Assert.Equal(0, handler.ProcessResponseCount);
        }

        [Fact]
        public async Task SendAsync_ProcessRequestThrows_ThrowWithoutCallingProcessRequestNorInnerHandler()
        {
            var transport = new MockTransportHandler();
            // ProcessRequest() throws exception.
            var handler = new MockHandler(transport, true, () => { throw new MockException(); }); 

            // Note that ProcessRequest() is called by SendAsync(). However, the exception is not thrown
            // by SendAsync(). Instead, the returned Task is marked as faulted and contains the exception. 
            await Assert.ThrowsAsync<MockException>(() => handler.TestSendAsync(new HttpRequestMessage(), CancellationToken.None));

            Assert.Equal(0, transport.SendAsyncCount);
            Assert.Equal(1, handler.ProcessRequestCount);
            Assert.Equal(0, handler.ProcessResponseCount);
        }

        [Fact]
        public async Task SendAsync_ProcessResponseThrows_TaskIsFaulted()
        {
            var transport = new MockTransportHandler();
            // ProcessResponse() throws exception.
            var handler = new MockHandler(transport, false, () => { throw new MockException(); }); 

            // Throwing an exception in ProcessResponse() will cause the Task to complete as 'faulted'.
            await Assert.ThrowsAsync<MockException>(() => handler.TestSendAsync(new HttpRequestMessage(), CancellationToken.None));

            Assert.Equal(1, transport.SendAsyncCount);
            Assert.Equal(1, handler.ProcessRequestCount);
            Assert.Equal(1, handler.ProcessResponseCount);
        }

        [Fact]
        public async Task SendAsync_OperationCanceledWhileInnerHandlerIsWorking_TaskSetToIsCanceled()
        {
            var cts = new CancellationTokenSource();
            // We simulate a cancellation happening while the inner handler was processing the request, by letting
            // the inner mock handler call Cancel() and behave like if another thread called cancel while it was
            // processing.
            var transport = new MockTransportHandler(cts); // inner handler will cancel.
            var handler = new MockHandler(transport);

            await Assert.ThrowsAsync<TaskCanceledException>(() => handler.TestSendAsync(new HttpRequestMessage(), cts.Token));
            Assert.Equal(0, handler.ProcessResponseCount);
        }

        [Fact]
        public async Task SendAsync_OperationCanceledWhileProcessRequestIsExecuted_TaskSetToIsCanceled()
        {
            var cts = new CancellationTokenSource();
            var transport = new MockTransportHandler();
            // ProcessRequest will cancel.
            var handler = new MockHandler(transport, true,
                () => { cts.Cancel(); cts.Token.ThrowIfCancellationRequested(); });

            // Note that even ProcessMessage() is called on the same thread, we don't expect SendAsync() to throw.
            // SendAsync() must complete successfully, but the Task will be canceled. 
            await Assert.ThrowsAsync<TaskCanceledException>(() => handler.TestSendAsync(new HttpRequestMessage(), cts.Token));
            Assert.Equal(0, handler.ProcessResponseCount);
        }

        [Fact]
        public async Task SendAsync_OperationCanceledWhileProcessResponseIsExecuted_TaskSetToIsCanceled()
        {
            var cts = new CancellationTokenSource();
            var transport = new MockTransportHandler();
            // ProcessResponse will cancel.
            var handler = new MockHandler(transport, false,
                () => { cts.Cancel(); cts.Token.ThrowIfCancellationRequested(); });

            await Assert.ThrowsAsync<TaskCanceledException>(() => handler.TestSendAsync(new HttpRequestMessage(), cts.Token));
        }

        [Fact]
        public async Task SendAsync_ProcessRequestThrowsOperationCanceledExceptionNotBoundToCts_TaskSetToIsFaulted()
        {
            var cts = new CancellationTokenSource();
            var transport = new MockTransportHandler();
            // ProcessRequest will throw a random OperationCanceledException() not related to cts. We also cancel
            // the cts to make sure the code behaves correctly even if cts is canceled & an OperationCanceledException
            // was thrown.
            var handler = new MockHandler(transport, true,
                () => { cts.Cancel(); throw new OperationCanceledException("custom"); });

            await Assert.ThrowsAsync<OperationCanceledException>(() => handler.TestSendAsync(new HttpRequestMessage(), cts.Token));

            Assert.Equal(0, handler.ProcessResponseCount);
        }

        [Fact]
        public async Task SendAsync_ProcessResponseThrowsOperationCanceledExceptionNotBoundToCts_TaskSetToIsFaulted()
        {
            var cts = new CancellationTokenSource();
            var transport = new MockTransportHandler();
            // ProcessResponse will throw a random OperationCanceledException() not related to cts. We also cancel
            // the cts to make sure the code behaves correctly even if cts is canceled & an OperationCanceledException
            // was thrown.
            var handler = new MockHandler(transport, false,
                () => { cts.Cancel(); throw new OperationCanceledException("custom"); });

            await Assert.ThrowsAsync<OperationCanceledException>(() => handler.TestSendAsync(new HttpRequestMessage(), cts.Token));

            Assert.Equal(1, handler.ProcessResponseCount);
        }
        
        [Fact]
        public async Task SendAsync_ProcessRequestThrowsOperationCanceledExceptionUsingOtherCts_TaskSetToIsFaulted()
        {
            var cts = new CancellationTokenSource();
            var otherCts = new CancellationTokenSource();
            var transport = new MockTransportHandler();
            // ProcessRequest will throw a random OperationCanceledException() not related to cts. We also cancel
            // the cts to make sure the code behaves correctly even if cts is canceled & an OperationCanceledException
            // was thrown.
            var handler = new MockHandler(transport, true,
                () => { cts.Cancel(); throw new OperationCanceledException("custom", otherCts.Token); });

            await Assert.ThrowsAsync<OperationCanceledException>(() => handler.TestSendAsync(new HttpRequestMessage(), cts.Token));

            Assert.Equal(0, handler.ProcessResponseCount);
        }

        [Fact]
        public async Task SendAsync_ProcessResponseThrowsOperationCanceledExceptionUsingOtherCts_TaskSetToIsFaulted()
        {
            var cts = new CancellationTokenSource();
            var otherCts = new CancellationTokenSource();
            var transport = new MockTransportHandler();
            // ProcessResponse will throw a random OperationCanceledException() not related to cts. We also cancel
            // the cts to make sure the code behaves correctly even if cts is canceled & an OperationCanceledException
            // was thrown.
            var handler = new MockHandler(transport, false,
                () => { cts.Cancel(); throw new OperationCanceledException("custom", otherCts.Token); });

            await Assert.ThrowsAsync<OperationCanceledException>(() => handler.TestSendAsync(new HttpRequestMessage(), cts.Token));

            Assert.Equal(1, handler.ProcessResponseCount);
        }

        #region Helper methods

        public class MockException : Exception
        {
            public MockException() { }
            public MockException(string message) : base(message) { }
            public MockException(string message, Exception inner) : base(message, inner) { }
        }

        private class MockHandler : MessageProcessingHandler
        {
            private bool _callInProcessRequest;
            private Action _customAction;

            public int ProcessRequestCount { get; private set; }
            public int ProcessResponseCount { get; private set; }

            public MockHandler()
                : base()
            {
            }

            public MockHandler(HttpMessageHandler innerHandler)
                : this(innerHandler, true, null)
            {
            }

            public MockHandler(HttpMessageHandler innerHandler, bool callInProcessRequest, Action customAction)
                : base(innerHandler)
            {
                _customAction = customAction;
                _callInProcessRequest = callInProcessRequest;
            }

            public Task<HttpResponseMessage> TestSendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return SendAsync(request, cancellationToken);
            }
            
            protected override HttpRequestMessage ProcessRequest(HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                ProcessRequestCount++;
                Assert.NotNull(request);

                if (_callInProcessRequest && (_customAction != null))
                {
                    _customAction();
                }

                return request;
            }

            protected override HttpResponseMessage ProcessResponse(HttpResponseMessage response,
                CancellationToken cancellationToken)
            {
                ProcessResponseCount++;
                Assert.NotNull(response);

                if (!_callInProcessRequest && (_customAction != null))
                {
                    _customAction();
                }

                return response;
            }
        }

        private class MockTransportHandler : HttpMessageHandler
        {
            private bool _alwaysThrow;
            private CancellationTokenSource _cts;
            private Func<HttpResponseMessage> _mockResultDelegate;

            public int SendAsyncCount { get; private set; }

            public MockTransportHandler(Func<HttpResponseMessage> mockResultDelegate)
            {
                _mockResultDelegate = mockResultDelegate;
            }

            public MockTransportHandler()
            {
            }

            public MockTransportHandler(bool alwaysThrow)
            {
                _alwaysThrow = alwaysThrow;
            }

            public MockTransportHandler(CancellationTokenSource cts)
            {
                _cts = cts;
            }
                        
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                SendAsyncCount++;

                TaskCompletionSource<HttpResponseMessage> tcs = new TaskCompletionSource<HttpResponseMessage>();

                if (_cts != null)
                {
                    _cts.Cancel();
                    tcs.TrySetCanceled();
                }

                if (_alwaysThrow)
                {
                    tcs.TrySetException(new MockException());
                }
                else
                {
                    if (_mockResultDelegate == null)
                    {
                        tcs.TrySetResult(new HttpResponseMessage());
                    }
                    else
                    {
                        tcs.TrySetResult(_mockResultDelegate());
                    }
                }

                return tcs.Task;
            }
        }

        #endregion
    }
}
