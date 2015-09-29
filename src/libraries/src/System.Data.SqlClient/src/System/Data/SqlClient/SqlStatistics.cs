// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.



//------------------------------------------------------------------------------

using System.Collections;
using System.Data.Common;
using System.Diagnostics;

namespace System.Data.SqlClient
{
    internal sealed class SqlStatistics
    {
        static internal SqlStatistics StartTimer(SqlStatistics statistics)
        {
            if ((null != statistics) && !statistics.RequestExecutionTimer())
            {
                // we're re-entrant -- don't bother.
                statistics = null;
            }
            return statistics;
        }

        static internal void StopTimer(SqlStatistics statistics)
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

        internal IDictionary GetHashtable()
        {
            Hashtable ht = new Hashtable();

            ht.Add("BuffersReceived", _buffersReceived);
            ht.Add("BuffersSent", _buffersSent);
            ht.Add("BytesReceived", _bytesReceived);
            ht.Add("BytesSent", _bytesSent);
            ht.Add("CursorOpens", _cursorOpens);
            ht.Add("IduCount", _iduCount);
            ht.Add("IduRows", _iduRows);
            ht.Add("PreparedExecs", _preparedExecs);
            ht.Add("Prepares", _prepares);
            ht.Add("SelectCount", _selectCount);
            ht.Add("SelectRows", _selectRows);
            ht.Add("ServerRoundtrips", _serverRoundtrips);
            ht.Add("SumResultSets", _sumResultSets);
            ht.Add("Transactions", _transactions);
            ht.Add("UnpreparedExecs", _unpreparedExecs);

            ht.Add("ConnectionTime", ADP.TimerToMilliseconds(_connectionTime));
            ht.Add("ExecutionTime", ADP.TimerToMilliseconds(_executionTime));
            ht.Add("NetworkServerTime", ADP.TimerToMilliseconds(_networkServerTime));

            return ht;
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
    }
}

