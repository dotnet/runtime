// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class IAsyncEnumerableOfTConverter<TAsyncEnumerable, TElement>
        : IEnumerableDefaultConverter<TAsyncEnumerable, TElement>
        where TAsyncEnumerable : IAsyncEnumerable<TElement>
    {
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

            if (state.Current.AsyncEnumerator is null)
            {
                enumerator = value.GetAsyncEnumerator(state.CancellationToken);
                moveNextTask = enumerator.MoveNextAsync();
                // we always need to attach the enumerator to the stack
                // since it will need to be disposed asynchronously.
                state.Current.AsyncEnumerator = enumerator;
            }
            else
            {
                Debug.Assert(state.Current.AsyncEnumerator is IAsyncEnumerator<TElement>);
                enumerator = (IAsyncEnumerator<TElement>)state.Current.AsyncEnumerator;

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

        private sealed class BufferedAsyncEnumerable : IAsyncEnumerable<TElement>
        {
            public readonly List<TElement> _buffer = new();

            public IAsyncEnumerator<TElement> GetAsyncEnumerator(CancellationToken _) =>
                new BufferedAsyncEnumerator(_buffer);

            private class BufferedAsyncEnumerator : IAsyncEnumerator<TElement>
            {
                private readonly List<TElement> _buffer;
                private int _index;

                public BufferedAsyncEnumerator(List<TElement> buffer)
                {
                    _buffer = buffer;
                    _index = -1;
                }

                public TElement Current => _index < 0 ? default! : _buffer[_index];
                public ValueTask DisposeAsync() => default;
                public ValueTask<bool> MoveNextAsync()
                {
                    if (_index == _buffer.Count - 1)
                    {
                        return new ValueTask<bool>(false);
                    }

                    _index++;
                    return new ValueTask<bool>(true);
                }
            }
        }
    }
}
