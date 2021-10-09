// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace System.Net.Http
{
    public abstract class MessageProcessingHandler : DelegatingHandler
    {
        protected MessageProcessingHandler()
        {
        }

        protected MessageProcessingHandler(HttpMessageHandler innerHandler)
            : base(innerHandler)
        {
        }

        protected abstract HttpRequestMessage ProcessRequest(HttpRequestMessage request,
            CancellationToken cancellationToken);
        protected abstract HttpResponseMessage ProcessResponse(HttpResponseMessage response,
            CancellationToken cancellationToken);

        protected internal sealed override HttpResponseMessage Send(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request), SR.net_http_handler_norequest);
            }

            // Since most of the SendAsync code is just Task handling, there's no reason to share the code.
            HttpRequestMessage newRequestMessage = ProcessRequest(request, cancellationToken);
            HttpResponseMessage response = base.Send(newRequestMessage, cancellationToken);
            HttpResponseMessage newResponseMessage = ProcessResponse(response, cancellationToken);
            return newResponseMessage;
        }

        protected internal sealed override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request), SR.net_http_handler_norequest);
            }

            // ProcessRequest() and ProcessResponse() are supposed to be fast, so we call ProcessRequest() on the same
            // thread SendAsync() was invoked to avoid context switches. However, if ProcessRequest() throws, we have
            // to catch the exception since the caller doesn't expect exceptions when calling SendAsync(): The
            // expectation is that the returned task will get faulted on errors, but the async call to SendAsync()
            // should complete.
            var tcs = new SendState(this, cancellationToken);
            try
            {
                HttpRequestMessage newRequestMessage = ProcessRequest(request, cancellationToken);
                Task<HttpResponseMessage> sendAsyncTask = base.SendAsync(newRequestMessage, cancellationToken);

                // We schedule a continuation task once the inner handler completes in order to trigger the response
                // processing method. ProcessResponse() is only called if the task wasn't canceled before.
                sendAsyncTask.ContinueWith(static (task, state) =>
                {
                    var sendState = (SendState)state!;
                    MessageProcessingHandler self = sendState._handler;
                    CancellationToken token = sendState._token;

                    if (task.IsFaulted)
                    {
                        sendState.TrySetException(task.Exception!.GetBaseException());
                        return;
                    }

                    if (task.IsCanceled)
                    {
                        sendState.TrySetCanceled(token);
                        return;
                    }

                    if (task.Result == null)
                    {
                        sendState.TrySetException(ExceptionDispatchInfo.SetCurrentStackTrace(new InvalidOperationException(SR.net_http_handler_noresponse)));
                        return;
                    }

                    try
                    {
                        HttpResponseMessage responseMessage = self.ProcessResponse(task.Result, token);
                        sendState.TrySetResult(responseMessage);
                    }
                    catch (OperationCanceledException e)
                    {
                        // If ProcessResponse() throws an OperationCanceledException check whether it is related to
                        // the cancellation token we received from the user. If so, cancel the Task.
                        HandleCanceledOperations(token, sendState, e);
                    }
                    catch (Exception e)
                    {
                        sendState.TrySetException(e);
                    }
                    // We don't pass the cancellation token to the continuation task, since we want to get called even
                    // if the operation was canceled: We'll set the Task returned to the user to canceled. Passing the
                    // cancellation token here would result in the continuation task to not be called at all. I.e. we
                    // would never complete the task returned to the caller of SendAsync().
                }, tcs, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
            }
            catch (OperationCanceledException e)
            {
                HandleCanceledOperations(cancellationToken, tcs, e);
            }
            catch (Exception e)
            {
                tcs.TrySetException(e);
            }

            return tcs.Task;
        }

        private static void HandleCanceledOperations(CancellationToken cancellationToken,
            TaskCompletionSource<HttpResponseMessage> tcs, OperationCanceledException e)
        {
            // Check if the exception was due to a cancellation. If so, check if the OperationCanceledException is
            // related to our CancellationToken. If it was indeed caused due to our cancellation token being
            // canceled, set the Task as canceled. Set it to faulted otherwise, since the OperationCanceledException
            // is not related to our cancellation token.
            if (cancellationToken.IsCancellationRequested && (e.CancellationToken == cancellationToken))
            {
                tcs.TrySetCanceled(cancellationToken);
            }
            else
            {
                tcs.TrySetException(e);
            }
        }

        // Private class used to capture the SendAsync state in
        // a closure, while simultaneously avoiding a tuple allocation.
        private sealed class SendState : TaskCompletionSource<HttpResponseMessage>
        {
            internal readonly MessageProcessingHandler _handler;
            internal readonly CancellationToken _token;

            public SendState(MessageProcessingHandler handler, CancellationToken token)
            {
                Debug.Assert(handler != null);

                _handler = handler;
                _token = token;
            }
        }
    }
}
