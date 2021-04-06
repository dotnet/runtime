// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace System.Text.Json.Serialization
{
    internal sealed class AsyncEnumerableStreamingDeserializer<TValue> : IAsyncEnumerable<TValue>
    {
        private readonly Stream _utf8Json;
        private readonly JsonSerializerOptions? _options;

        public AsyncEnumerableStreamingDeserializer(Stream utf8Json, JsonSerializerOptions? options)
        {
            _utf8Json = utf8Json;
            _options = options;
        }

        public async IAsyncEnumerator<TValue> GetAsyncEnumerator(CancellationToken cancellationToken)
        {
            using var asyncState = new ReadAsyncState(typeof(Queue<TValue>), _options, cancellationToken);
            bool isFinalBlock = false;
            do
            {
                isFinalBlock = await JsonSerializer.ReadFromStream(_utf8Json, asyncState).ConfigureAwait(false);
                JsonSerializer.ContinueDeserialize<Queue<TValue>>(asyncState, isFinalBlock);
                if (asyncState.ReadStack.Current.ReturnValue is Queue<TValue> queue)
                {
                    while (queue.Count > 0)
                    {
                        yield return queue.Dequeue();
                    }
                }
            }
            while (!isFinalBlock);
        }
    }
}
