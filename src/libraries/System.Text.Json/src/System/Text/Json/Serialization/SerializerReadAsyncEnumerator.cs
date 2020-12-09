// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
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
            }

            if (HasLocalDataToReturn())
            {
                return true;
            }

            await ContinueRead().ConfigureAwait(false);
            HasLocalDataToReturn();
            return true;

            bool HasLocalDataToReturn()
            {
                if (_valuesToReturn?.Count > 1)
                {
                    _current = _valuesToReturn.Dequeue();
                    return true;
                }

                if (_isFinalBlock)
                {
                    if (_valuesToReturn?.Count >= 1)
                    {
                        _current = _valuesToReturn.Dequeue();
                    }

                    _doneProcessing = true;
                    _asyncState.Dispose();
                    _asyncState = null!;

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
                _valuesToReturn = JsonSerializer.ContinueDeserialize<Queue<TValue>>(_asyncState, _isFinalBlock);
            }

            return true;

            // If .Count is 1, the item may be a partial object.
            // todo: if feasible, have a better way to detect that the item is finished.
            bool HaveDataToReturn() => _isFinalBlock || _valuesToReturn?.Count > 1;
        }
    }
}
