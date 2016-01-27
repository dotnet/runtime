// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.Contracts;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security;
using System.Threading;
using System.Threading.Tasks;

namespace System.Threading.Tasks {

/// <summary>
/// Provides an adapter to make Begin/End pairs awaitable.
/// In general, Task.Factory.FromAsync should be used for this purpose.
/// However, for cases where absolute minimal overhead is required, this type
/// may be used to making APM pairs awaitable while minimizing overhead.
/// (APM = Asynchronous Programming Model  or the Begin/End pattern.)
/// </summary>
/// <remarks>
/// This instance may be reused repeatedly.  However, it must only be used
/// by a single APM invocation at a time.  It's state will automatically be reset
/// when the await completes.
/// </remarks>
/// <example>
/// Usage sample:
/// <code>
///     static async Task CopyStreamAsync(Stream source, Stream dest) {
///     
///         BeginEndAwaitableAdapter adapter = new BeginEndAwaitableAdapter();
///         Byte[] buffer = new Byte[0x1000];
///      
///         while (true) {
///     
///             source.BeginRead(buffer, 0, buffer.Length, BeginEndAwaitableAdapter.Callback, adapter);
///             Int32 numRead = source.EndRead(await adapter);
///             if (numRead == 0)
///                 break;
///      
///             dest.BeginWrite(buffer, 0, numRead, BeginEndAwaitableAdapter.Callback, adapter);
///             dest.EndWrite(await adapter);
///         }
///     }
/// </code>
/// </example>
internal sealed class BeginEndAwaitableAdapter : ICriticalNotifyCompletion {

    /// <summary>A sentinel marker used to communicate between OnCompleted and the APM callback
    /// that the callback has already run, and thus OnCompleted needs to execute the callback.</summary>
    private readonly static Action CALLBACK_RAN = () => { };

    /// <summary>The IAsyncResult for the APM operation.</summary>
    private IAsyncResult _asyncResult;

    /// <summary>The continuation delegate provided to the awaiter.</summary>
    private Action _continuation;


    /// <summary>A callback to be passed as the AsyncCallback to an APM pair.
    /// It expects that an BeginEndAwaitableAdapter instance was supplied to the APM Begin method as the object state.</summary>
    public readonly static AsyncCallback Callback = (asyncResult) => {

        Contract.Assert(asyncResult != null);
        Contract.Assert(asyncResult.IsCompleted);
        Contract.Assert(asyncResult.AsyncState is BeginEndAwaitableAdapter);

        // Get the adapter object supplied as the "object state" to the Begin method
        BeginEndAwaitableAdapter adapter = (BeginEndAwaitableAdapter) asyncResult.AsyncState;

        // Store the IAsyncResult into it so that it's available to the awaiter
        adapter._asyncResult = asyncResult;

        // If the _continuation has already been set to the actual continuation by OnCompleted, then invoke the continuation.
        // Set _continuation to the CALLBACK_RAN sentinel so that IsCompleted returns true and OnCompleted sees the sentinel
        // and knows to invoke the callback.
        // Due to some known incorrect implementations of IAsyncResult in the Framework where CompletedSynchronously is lazily
        // set to true if it is first invoked after IsCompleted is true, we cannot rely here on CompletedSynchronously for
        // synchronization between the caller and the callback, and thus do not use CompletedSynchronously at all.
        Action continuation = Interlocked.Exchange(ref adapter._continuation, CALLBACK_RAN);
        if (continuation != null) {
        
            Contract.Assert(continuation != CALLBACK_RAN);
            continuation();
        }        
    };


    /// <summary>Gets an awaiter.</summary>
    /// <returns>Returns itself as the awaiter.</returns>
    public BeginEndAwaitableAdapter GetAwaiter() {

        return this;
    }


    /// <summary>Gets whether the awaited APM operation completed.</summary>
    public bool IsCompleted {
        get {
        
            // We are completed if the callback was called and it set the continuation to the CALLBACK_RAN sentinel.
            // If the operation completes asynchronously, there's still a chance we'll see CALLBACK_RAN here, in which
            // case we're still good to keep running synchronously.           
            return (_continuation == CALLBACK_RAN);            
        }
    }

    /// <summary>Schedules the continuation to run when the operation completes.</summary>
    /// <param name="continuation">The continuation.</param>
    [SecurityCritical]
    public void UnsafeOnCompleted(Action continuation) {

        Contract.Assert(continuation != null);
        OnCompleted(continuation); 
    }


    /// <summary>Schedules the continuation to run when the operation completes.</summary>
    /// <param name="continuation">The continuation.</param>
    public void OnCompleted(Action continuation) {

        Contract.Assert(continuation != null);

        // If the continuation field is null, then set it to be the target continuation
        // so that when the operation completes, it'll invoke the continuation.  If it's non-null,
        // it was already set to the CALLBACK_RAN-sentinel by the Callback, in which case we hit a very rare race condition
        // where the operation didn't complete synchronously but completed asynchronously between our
        // calls to IsCompleted and OnCompleted... in that case, just schedule a task to run the continuation.
        if (_continuation == CALLBACK_RAN
                || Interlocked.CompareExchange(ref _continuation, continuation, null) == CALLBACK_RAN) {

            Task.Run(continuation); // must run async at this point, or else we'd risk stack diving
        }
    }


    /// <summary>Gets the IAsyncResult for the APM operation after the operation completes, and then resets the adapter.</summary>
    /// <returns>The IAsyncResult for the operation.</returns>
    public IAsyncResult GetResult() {

        Contract.Assert(_asyncResult != null && _asyncResult.IsCompleted);

        // Get the IAsyncResult
        IAsyncResult result = _asyncResult;

        // Reset the adapter
        _asyncResult = null;
        _continuation = null;

        // Return the result
        return result;
    }

}  // class BeginEndAwaitableAdapter

}  // namespace
