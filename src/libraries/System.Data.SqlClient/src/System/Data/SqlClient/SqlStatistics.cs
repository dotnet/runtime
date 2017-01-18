// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



//------------------------------------------------------------------------------

using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;

namespace System.Data.SqlClient
{
    internal sealed class SqlStatistics
    {
        internal static SqlStatistics StartTimer(SqlStatistics statistics)
        {
            if ((null != statistics) && !statistics.RequestExecutionTimer())
            {
                // we're re-entrant -- don't bother.
                statistics = null;
            }
            return statistics;
        }

        internal static void StopTimer(SqlStatistics statistics)
        {
            if (null != statistics)
            {
                statistics.ReleaseAndUpdateExecutionTimer();
            }
        }

        // internal values that are not exposed through properties
        internal long _closeTimestamp;
        internal long _openTimestamp;
        internal long _startExecutionTimestamp;
        internal long _startFetchTimestamp;
        internal long _startNetworkServerTimestamp;

        // internal values that are exposed through properties
        internal long _buffersReceived;
        internal long _buffersSent;
        internal long _bytesReceived;
        internal long _bytesSent;
        internal long _connectionTime;
        internal long _cursorOpens;
        internal long _executionTime;
        internal long _iduCount;
        internal long _iduRows;
        internal long _networkServerTime;
        internal long _preparedExecs;
        internal long _prepares;
        internal long _selectCount;
        internal long _selectRows;
        internal long _serverRoundtrips;
        internal long _sumResultSets;
        internal long _transactions;
        internal long _unpreparedExecs;

        // these flags are required if statistics is turned on/off in the middle of command execution
        private bool _waitForDoneAfterRow;
        private bool _waitForReply;


        internal bool WaitForDoneAfterRow
        {
            get
            {
                return _waitForDoneAfterRow;
            }
            set
            {
                _waitForDoneAfterRow = value;
            }
        }

        internal bool WaitForReply
        {
            get
            {
                return _waitForReply;
            }
        }

        internal SqlStatistics()
        {
        }

        internal void ContinueOnNewConnection()
        {
            _startExecutionTimestamp = 0;
            _startFetchTimestamp = 0;
            _waitForDoneAfterRow = false;
            _waitForReply = false;
        }

        internal IDictionary GetDictionary()
        {
            const int Count = 18;
            var dictionary = new StatisticsDictionary(Count)
            {
                { "BuffersReceived", _buffersReceived },
                { "BuffersSent", _buffersSent },
                { "BytesReceived", _bytesReceived },
                { "BytesSent", _bytesSent },
                { "CursorOpens", _cursorOpens },
                { "IduCount", _iduCount },
                { "IduRows", _iduRows },
                { "PreparedExecs", _preparedExecs },
                { "Prepares", _prepares },
                { "SelectCount", _selectCount },
                { "SelectRows", _selectRows },
                { "ServerRoundtrips", _serverRoundtrips },
                { "SumResultSets", _sumResultSets },
                { "Transactions", _transactions },
                { "UnpreparedExecs", _unpreparedExecs },

                { "ConnectionTime", ADP.TimerToMilliseconds(_connectionTime) },
                { "ExecutionTime", ADP.TimerToMilliseconds(_executionTime) },
                { "NetworkServerTime", ADP.TimerToMilliseconds(_networkServerTime) }
            };
            Debug.Assert(dictionary.Count == Count);
            return dictionary;
        }

        internal bool RequestExecutionTimer()
        {
            if (_startExecutionTimestamp == 0)
            {
                ADP.TimerCurrent(out _startExecutionTimestamp);
                return true;
            }
            return false;
        }

        internal void RequestNetworkServerTimer()
        {
            Debug.Assert(_startExecutionTimestamp != 0, "No network time expected outside execution period");
            if (_startNetworkServerTimestamp == 0)
            {
                ADP.TimerCurrent(out _startNetworkServerTimestamp);
            }
            _waitForReply = true;
        }

        internal void ReleaseAndUpdateExecutionTimer()
        {
            if (_startExecutionTimestamp > 0)
            {
                _executionTime += (ADP.TimerCurrent() - _startExecutionTimestamp);
                _startExecutionTimestamp = 0;
            }
        }

        internal void ReleaseAndUpdateNetworkServerTimer()
        {
            if (_waitForReply && _startNetworkServerTimestamp > 0)
            {
                _networkServerTime += (ADP.TimerCurrent() - _startNetworkServerTimestamp);
                _startNetworkServerTimestamp = 0;
            }
            _waitForReply = false;
        }

        internal void Reset()
        {
            _buffersReceived = 0;
            _buffersSent = 0;
            _bytesReceived = 0;
            _bytesSent = 0;
            _connectionTime = 0;
            _cursorOpens = 0;
            _executionTime = 0;
            _iduCount = 0;
            _iduRows = 0;
            _networkServerTime = 0;
            _preparedExecs = 0;
            _prepares = 0;
            _selectCount = 0;
            _selectRows = 0;
            _serverRoundtrips = 0;
            _sumResultSets = 0;
            _transactions = 0;
            _unpreparedExecs = 0;
            _waitForDoneAfterRow = false;
            _waitForReply = false;
            _startExecutionTimestamp = 0;
            _startNetworkServerTimestamp = 0;
        }

        internal void SafeAdd(ref long value, long summand)
        {
            if (long.MaxValue - value > summand)
            {
                value += summand;
            }
            else
            {
                value = long.MaxValue;
            }
        }

        internal long SafeIncrement(ref long value)
        {
            if (value < long.MaxValue) value++;
            return value;
        }

        internal void UpdateStatistics()
        {
            // update connection time
            if (_closeTimestamp >= _openTimestamp)
            {
                SafeAdd(ref _connectionTime, _closeTimestamp - _openTimestamp);
            }
            else
            {
                _connectionTime = long.MaxValue;
            }
        }

        // We subclass Dictionary to provide our own implementation of GetEnumerator, CopyTo, Keys.CopyTo,
        // and Values.CopyTo to match the behavior of Hashtable, which is used in the full framework:
        //
        //  - Hashtable's IEnumerator.GetEnumerator enumerator yields DictionaryEntry entries whereas
        //    Dictionary's yields KeyValuePair entries.
        //
        //  - When arrayIndex > array.Length, Hashtable throws ArgumentException whereas Dictionary
        //    throws ArgumentOutOfRangeException.
        //
        //  - Hashtable specifies the ArgumentOutOfRangeException paramName as "arrayIndex" whereas
        //    Dictionary uses "index".
        //
        //  - When the array is of a mismatched type, Hashtable throws InvalidCastException whereas
        //    Dictionary throws ArrayTypeMismatchException.
        //
        //  - Hashtable allows copying values to a long[] array via Values.CopyTo, whereas Dictionary
        //    throws ArgumentException due to the "Target array type is not compatible with type of
        //    items in the collection" (when Dictionary<object, object> is used).
        //
        // Ideally this would derive from Dictionary<string, long>, but that would break compatibility
        // with the full framework, which allows adding keys/values of any type.
        private sealed class StatisticsDictionary : Dictionary<object, object>, IDictionary, IEnumerable
        {
            private Collection _keys;
            private Collection _values;

            public StatisticsDictionary(int capacity) : base(capacity) { }

            ICollection IDictionary.Keys => _keys ?? (_keys = new Collection(this, Keys));

            ICollection IDictionary.Values => _values ?? (_values = new Collection(this, Values));

            // Return a DictionaryEntry enumerator instead of a KeyValuePair enumerator.
            IEnumerator IEnumerable.GetEnumerator() => ((IDictionary)this).GetEnumerator();

            void ICollection.CopyTo(Array array, int arrayIndex)
            {
                ValidateCopyToArguments(array, arrayIndex);

                foreach (KeyValuePair<object, object> pair in this)
                {
                    var entry = new DictionaryEntry(pair.Key, pair.Value);
                    array.SetValue(entry, arrayIndex++);
                }
            }

            private void CopyKeys(Array array, int arrayIndex)
            {
                ValidateCopyToArguments(array, arrayIndex);

                foreach (KeyValuePair<object, object> pair in this)
                {
                    array.SetValue(pair.Key, arrayIndex++);
                }
            }

            private void CopyValues(Array array, int arrayIndex)
            {
                ValidateCopyToArguments(array, arrayIndex);

                foreach (KeyValuePair<object, object> pair in this)
                {
                    array.SetValue(pair.Value, arrayIndex++);
                }
            }

            private void ValidateCopyToArguments(Array array, int arrayIndex)
            {
                if (array == null)
                    throw new ArgumentNullException(nameof(array));
                if (array.Rank != 1)
                    throw new ArgumentException(SR.Arg_RankMultiDimNotSupported);
                if (arrayIndex < 0)
                    throw new ArgumentOutOfRangeException(nameof(arrayIndex), SR.ArgumentOutOfRange_NeedNonNegNum);
                if (array.Length - arrayIndex < Count)
                    throw new ArgumentException(SR.Arg_ArrayPlusOffTooSmall);
            }

            private sealed class Collection : ICollection
            {
                private readonly StatisticsDictionary _dictionary;
                private readonly ICollection _collection;

                public Collection(StatisticsDictionary dictionary, ICollection collection)
                {
                    Debug.Assert(dictionary != null);
                    Debug.Assert(collection != null);
                    Debug.Assert((collection is KeyCollection) || (collection is ValueCollection));

                    _dictionary = dictionary;
                    _collection = collection;
                }

                int ICollection.Count => _collection.Count;

                bool ICollection.IsSynchronized => _collection.IsSynchronized;

                object ICollection.SyncRoot => _collection.SyncRoot;

                void ICollection.CopyTo(Array array, int arrayIndex)
                {
                    if (_collection is KeyCollection)
                    {
                        _dictionary.CopyKeys(array, arrayIndex);
                    }
                    else
                    {
                        _dictionary.CopyValues(array, arrayIndex);
                    }
                }

                IEnumerator IEnumerable.GetEnumerator() => _collection.GetEnumerator();
            }
        }
    }
}
