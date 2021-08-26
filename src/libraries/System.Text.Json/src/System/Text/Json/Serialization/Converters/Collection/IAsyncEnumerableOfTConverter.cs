// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class IAsyncEnumerableOfTConverter<TAsyncEnumerable, TElement>
        : JsonCollectionConverter<TAsyncEnumerable, TElement>
        where TAsyncEnumerable : IAsyncEnumerable<TElement>
    {
        internal override bool OnTryRead(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options, ref ReadStack state, out TAsyncEnumerable value)
        {
            if (!typeToConvert.IsAssignableFrom(typeof(IAsyncEnumerable<TElement>)))
            {
                ThrowHelper.ThrowNotSupportedException_CannotPopulateCollection(TypeToConvert, ref reader, ref state);
            }

            return base.OnTryRead(ref reader, typeToConvert, options, ref state, out value!);
        }

        protected override void Add(in TElement value, ref ReadStack state)
        {
            ((BufferedAsyncEnumerable)state.Current.ReturnValue!)._buffer.Add(value);
        }

        protected override void CreateCollection(ref Utf8JsonReader reader, ref ReadStack state, JsonSerializerOptions options)
        {
            state.Current.ReturnValue = new BufferedAsyncEnumerable();
        }

        internal override bool OnTryWrite(Utf8JsonWriter writer, TAsyncEnumerable value, JsonSerializerOptions options, ref WriteStack state)
        {
            if (!state.SupportContinuation)
            {
                ThrowHelper.ThrowNotSupportedException_TypeRequiresAsyncSerialization(TypeToConvert);
            }

            return base.OnTryWrite(writer, value, options, ref state);
        }

        [Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "Converter needs to consume ValueTask's in a non-async context")]
        protected override bool OnWriteResume(Utf8JsonWriter writer, TAsyncEnumerable value, JsonSerializerOptions options, ref WriteStack state)
        {
            IAsyncEnumerator<TElement> enumerator;
            ValueTask<bool> moveNextTask;

            if (state.Current.AsyncDisposable is null)
            {
                enumerator = value.GetAsyncEnumerator(state.CancellationToken);
                // async enumerators can only be disposed asynchronously;
                // store in the WriteStack for future disposal
                // by the root async serialization context.
                state.Current.AsyncDisposable = enumerator;
                // enumerator.MoveNextAsync() calls can throw,
                // ensure the enumerator already is stored
                // in the WriteStack for proper disposal.
                moveNextTask = enumerator.MoveNextAsync();
            }
            else
            {
                Debug.Assert(state.Current.AsyncDisposable is IAsyncEnumerator<TElement>);
                enumerator = (IAsyncEnumerator<TElement>)state.Current.AsyncDisposable;

                if (state.Current.AsyncEnumeratorIsPendingCompletion)
                {
                    // converter was previously suspended due to a pending MoveNextAsync() task
                    Debug.Assert(state.PendingTask is Task<bool> && state.PendingTask.IsCompleted);
                    moveNextTask = new ValueTask<bool>((Task<bool>)state.PendingTask);
                    state.Current.AsyncEnumeratorIsPendingCompletion = false;
                    state.PendingTask = null;
                }
                else
                {
                    // converter was suspended for a different reason;
                    // the last MoveNextAsync() call can only have completed with 'true'.
                    moveNextTask = new ValueTask<bool>(true);
                }
            }

            JsonConverter<TElement> converter = GetElementConverter(ref state);

            // iterate through the enumerator while elements are being returned synchronously
            for (; moveNextTask.IsCompleted; moveNextTask = enumerator.MoveNextAsync())
            {
                if (!moveNextTask.Result)
                {
                    // we have completed serialization for the enumerator,
                    // clear from the stack and schedule for async disposal.
                    state.Current.AsyncDisposable = null;
                    state.AddCompletedAsyncDisposable(enumerator);
                    return true;
                }

                if (ShouldFlush(writer, ref state))
                {
                    return false;
                }

                TElement element = enumerator.Current;
                if (!converter.TryWrite(writer, element, options, ref state))
                {
                    return false;
                }
            }

            // we have a pending MoveNextAsync() call;
            // wrap inside a regular task so that it can be awaited multiple times;
            // mark the current stackframe as pending completion.
            Debug.Assert(state.PendingTask is null);
            state.PendingTask = moveNextTask.AsTask();
            state.Current.AsyncEnumeratorIsPendingCompletion = true;
            return false;
        }

        private sealed class BufferedAsyncEnumerable : IAsyncEnumerable<TElement>
        {
            public readonly List<TElement> _buffer = new();

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
            public async IAsyncEnumerator<TElement> GetAsyncEnumerator(CancellationToken _)
            {
                foreach (TElement element in _buffer)
                {
                    yield return element;
                }
            }
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        }
    }
}
