// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace System.Text.Json.Serialization
{
    /// <summary>
    /// todo
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    // todo: change to struct?
    internal sealed class SerializerReadAsyncEnumerator<TValue> : IAsyncEnumerator<TValue>
    {
        private Stream _utf8Json;
        private ReadAsyncState _asyncState;
        private Queue<TValue>? _valuesToReturn;
        private TValue? _current;
        private bool _isFinalBlock;
        private bool _doneProcessing;

        public SerializerReadAsyncEnumerator(Stream utf8Json, JsonSerializerOptions? options)
        {
            _utf8Json = utf8Json;
            _asyncState = new ReadAsyncState(typeof(Queue<TValue>), cancellationToken: default, options);
        }

        public TValue Current
        {
            get
            {
                return _current!;
            }
        }

        /// <summary>
        /// todo
        /// </summary>
        /// <returns></returns>
        public ValueTask DisposeAsync()
        {
            return default;
        }

        /// <summary>
        /// todo
        /// </summary>
        /// <returns></returns>
        public async ValueTask<bool> MoveNextAsync()
        {
            if (_doneProcessing)
            {
                return false;
                // todo: throw InvalidOperationException if MoveNext is called again?
            }

            if (HasLocalDataToReturn())
            {
                return true;
            }

            // Read additional data.
            await ContinueRead().ConfigureAwait(false);
            return ApplyReturnValue();

            bool HasLocalDataToReturn()
            {
                if (ApplyReturnValue())
                {
                    return true;
                }

                if (_isFinalBlock)
                {
                    _doneProcessing = true;
                    _asyncState.Dispose();
                    _asyncState = null!;
                }

                return false;
            }

            bool ApplyReturnValue()
            {
                if (_valuesToReturn?.Count > 0)
                {
                    _current = _valuesToReturn.Dequeue();
                    return true;
                }

                return false;
            }
        }

        private async ValueTask<bool> ContinueRead()
        {
            while (!HaveDataToReturn())
            {
                _isFinalBlock = await JsonSerializer.ReadFromStream(_utf8Json, _asyncState).ConfigureAwait(false);
                JsonSerializer.ContinueDeserialize<Queue<TValue>>(_asyncState, _isFinalBlock);

                // Obtain the partial collection.
                _valuesToReturn = (Queue<TValue>?)_asyncState.ReadStack.Current.ReturnValue;
            }

            return true;

            bool HaveDataToReturn() => _isFinalBlock || _valuesToReturn?.Count > 0;
        }
    }
}
