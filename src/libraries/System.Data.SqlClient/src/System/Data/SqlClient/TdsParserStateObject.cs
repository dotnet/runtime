// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Text;

namespace System.Data.SqlClient
{
    sealed internal class LastIOTimer
    {
        internal long _value;
    }

    internal abstract class TdsParserStateObject
    {
        private const int AttentionTimeoutSeconds = 5;

        // Ticks to consider a connection "good" after a successful I/O (10,000 ticks = 1 ms)
        // The resolution of the timer is typically in the range 10 to 16 milliseconds according to msdn.
        // We choose a value that is smaller than the likely timer resolution, but
        // large enough to ensure that check connection execution will be 0.1% or less
        // of very small open, query, close loops.
        private const long CheckConnectionWindow = 50000;


        protected readonly TdsParser _parser;                            // TdsParser pointer  
        private readonly WeakReference _owner = new WeakReference(null);   // the owner of this session, used to track when it's been orphaned
        internal SqlDataReader.SharedState _readerState;                    // susbset of SqlDataReader state (if it is the owner) necessary for parsing abandoned results in TDS
        private int _activateCount;                     // 0 when we're in the pool, 1 when we're not, all others are an error

        // Two buffers exist in tdsparser, an in buffer and an out buffer.  For the out buffer, only
        // one bookkeeping variable is needed, the number of bytes used in the buffer.  For the in buffer,
        // three variables are actually needed.  First, we need to record from the netlib how many bytes it
        // read from the netlib, this variable is _inBytesRead.  Then, we need to also keep track of how many
        // bytes we have used as we consume the bytes from the buffer, that variable is _inBytesUsed.  Third,
        // we need to keep track of how many bytes are left in the packet, so that we know when we have reached
        // the end of the packet and so we need to consume the next header.  That variable is _inBytesPacket.

        // Header length constants
        internal readonly int _inputHeaderLen = TdsEnums.HEADER_LEN;
        internal readonly int _outputHeaderLen = TdsEnums.HEADER_LEN;

        // Out buffer variables
        internal byte[] _outBuff;                                     // internal write buffer - initialize on login
        internal int _outBytesUsed = TdsEnums.HEADER_LEN; // number of bytes used in internal write buffer -
                                                          // - initialize past header
                                                          // In buffer variables
        protected byte[] _inBuff;                         // internal read buffer - initialize on login
        internal int _inBytesUsed = 0;                   // number of bytes used in internal read buffer
        internal int _inBytesRead = 0;                   // number of bytes read into internal read buffer

        internal int _inBytesPacket = 0;                   // number of bytes left in packet

        // Packet state variables
        internal byte _outputMessageType = 0;                   // tds header type
        internal byte _messageStatus;                               // tds header status
        internal byte _outputPacketNumber = 1;                   // number of packets sent to server
                                                                 // in message - start at 1 per ramas
        internal bool _pendingData = false;
        internal volatile bool _fResetEventOwned = false;               // ResetEvent serializing call to sp_reset_connection
        internal volatile bool _fResetConnectionSent = false;               // For multiple packet execute

        internal bool _errorTokenReceived = false;               // Keep track of whether an error was received for the result.
                                                                 // This is reset upon each done token - there can be

        internal bool _bulkCopyOpperationInProgress = false;        // Set to true during bulk copy and used to turn toggle write timeouts.
        internal bool _bulkCopyWriteTimeout = false;                // Set to trun when _bulkCopyOpeperationInProgress is trun and write timeout happens

        // SNI variables                                                     // multiple resultsets in one batch.
        
        protected readonly object _writePacketLockObject = new object();        // Used to synchronize access to _writePacketCache and _pendingWritePackets

        // Async variables
        private int _pendingCallbacks;                            // we increment this before each async read/write call and decrement it in the callback.  We use this to determine when to release the GcHandle...

        // Timeout variables
        private long _timeoutMilliseconds;
        private long _timeoutTime;                                 // variable used for timeout computations, holds the value of the hi-res performance counter at which this request should expire
        internal volatile bool _attentionSent = false;               // true if we sent an Attention to the server
        internal bool _attentionReceived = false;               // NOTE: Received is not volatile as it is only ever accessed\modified by TryRun its callees (i.e. single threaded access)
        internal volatile bool _attentionSending = false;
        internal bool _internalTimeout = false;               // an internal timeout occurred

        private readonly LastIOTimer _lastSuccessfulIOTimer;


        // This variable is used to track whether another thread has requested a cancel.  The
        // synchronization points are
        //   On the user's execute thread:
        //     1) the first packet write
        //     2) session close - return this stateObj to the session pool
        //   On cancel thread we only have the cancel call.
        // Currently all access to this variable is inside a lock, The state diagram is:
        // 1) pre first packet write, if cancel is requested, set variable so exception is triggered
        //    on user thread when first packet write is attempted
        // 2) post first packet write, but before session return - a call to cancel will send an
        //    attention to the server
        // 3) post session close - no attention is allowed
        private bool _cancelled;
        private const int _waitForCancellationLockPollTimeout = 100;
        private WeakReference _cancellationOwner = new WeakReference(null);

        internal bool _hasOpenResult = false;
        // Cache the transaction for which this command was executed so upon completion we can
        // decrement the appropriate result count.
        internal SqlInternalTransaction _executedUnderTransaction = null;

        // TDS stream processing variables
        internal ulong _longlen;                                     // plp data length indicator
        internal ulong _longlenleft;                                 // Length of data left to read (64 bit lengths)
        internal int[] _decimalBits = null;                // scratch buffer for decimal/numeric data
        internal byte[] _bTmp = new byte[TdsEnums.YUKON_HEADER_LEN];  // Scratch buffer for misc use
        internal int _bTmpRead = 0;                   // Counter for number of temporary bytes read
        internal Decoder _plpdecoder = null;             // Decoder object to process plp character data
        internal bool _accumulateInfoEvents = false;               // TRUE - accumulate info messages during TdsParser.Run, FALSE - fire them
        internal List<SqlError> _pendingInfoEvents = null;

        //
        // DO NOT USE THIS BUFFER FOR OTHER THINGS.
        // ProcessHeader can be called ANYTIME while doing network reads.
        private byte[] _partialHeaderBuffer = new byte[TdsEnums.HEADER_LEN];   // Scratch buffer for ProcessHeader
        internal int _partialHeaderBytesRead = 0;

        internal _SqlMetaDataSet _cleanupMetaData = null;
        internal _SqlMetaDataSetCollection _cleanupAltMetaDataSetArray = null;


        internal bool _receivedColMetaData;      // Used to keep track of when to fire StatementCompleted  event.

        private SniContext _sniContext = SniContext.Undefined;
#if DEBUG
        private SniContext _debugOnlyCopyOfSniContext = SniContext.Undefined;
#endif

        private bool _bcpLock = false;

        // Null bitmap compression (NBC) information for the current row
        private NullBitmap _nullBitmapInfo;

        // Async
        internal TaskCompletionSource<object> _networkPacketTaskSource;
        private Timer _networkPacketTimeout;
        internal bool _syncOverAsync = true;
        private bool _snapshotReplay = false;
        private StateSnapshot _snapshot;
        internal ExecutionContext _executionContext;
        internal bool _asyncReadWithoutSnapshot = false;
#if DEBUG
        // Used to override the assert than ensures that the stacktraces on subsequent replays are the same
        // This is useful is you are purposefully running the replay from a different thread (e.g. during SqlDataReader.Close)
        internal bool _permitReplayStackTraceToDiffer = false;

        // Used to indicate that the higher level object believes that this stateObj has enough data to complete an operation
        // If this stateObj has to read, then it will raise an assert
        internal bool _shouldHaveEnoughData = false;
#endif

        // local exceptions to cache warnings and errors
        internal SqlErrorCollection _errors;
        internal SqlErrorCollection _warnings;
        internal object _errorAndWarningsLock = new object();
        private bool _hasErrorOrWarning = false;

        // local exceptions to cache warnings and errors that occurred prior to sending attention
        internal SqlErrorCollection _preAttentionErrors;
        internal SqlErrorCollection _preAttentionWarnings;

        private volatile TaskCompletionSource<object> _writeCompletionSource = null;
        protected volatile int _asyncWriteCount = 0;
        private volatile Exception _delayedWriteAsyncCallbackException = null; // set by write async callback if completion source is not yet created

        // _readingcount is incremented when we are about to read.
        // We check the parser state afterwards.
        // When the read is completed, we decrement it before handling errors
        // as the error handling may end up calling Dispose.
        private int _readingCount;

        // Test hooks
#if DEBUG
        // This is a test hook to enable testing of the retry paths.
        // When set to true, almost every possible retry point will be attempted.
        // This will drastically impact performance.
        //
        // Sample code to enable:
        //
        //    Type type = typeof(SqlDataReader).Assembly.GetType("System.Data.SqlClient.TdsParserStateObject");
        //    System.Reflection.FieldInfo field = type.GetField("_forceAllPends", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        //    if (field != null) {
        //        field.SetValue(null, true);
        //    }
        //
        internal static bool _forceAllPends = false;

        // set this while making a call that should not block.
        // instead of blocking it will fail.
        internal static bool _failAsyncPends = false;

        // If this is set and an async read is made, then 
        // we will switch to syncOverAsync mode for the 
        // remainder of the async operation.
        internal static bool _forceSyncOverAsyncAfterFirstPend = false;

        // Requests to send attention will be ignored when _skipSendAttention is true.
        // This is useful to simulate circumstances where timeouts do not recover.
        internal static bool _skipSendAttention = false;

        // Prevents any pending read from completing until the user signals it using
        // CompletePendingReadWithSuccess() or CompletePendingReadWithFailure(int errorCode) in SqlCommand\SqlDataReader
        internal static bool _forcePendingReadsToWaitForUser;
        internal TaskCompletionSource<object> _realNetworkPacketTaskSource = null;

        // Field is never assigned to, and will always have its default value
#pragma warning disable 0649
        // Set to true to enable checking the call stacks match when packet retry occurs.
        internal static bool _checkNetworkPacketRetryStacks;
#pragma warning restore 0649
#endif

        //////////////////
        // Constructors //
        //////////////////

        internal TdsParserStateObject(TdsParser parser)
        {
            // Construct a physical connection
            Debug.Assert(null != parser, "no parser?");
            _parser = parser;

            // For physical connection, initialize to default login packet size.
            SetPacketSize(TdsEnums.DEFAULT_LOGIN_PACKET_SIZE);

            // we post a callback that represents the call to dispose; once the
            // object is disposed, the next callback will cause the GC Handle to
            // be released.
            IncrementPendingCallbacks();
            _lastSuccessfulIOTimer = new LastIOTimer();
        }

        internal TdsParserStateObject(TdsParser parser, TdsParserStateObject physicalConnection, bool async)
        {
            // Construct a MARS session
            Debug.Assert(null != parser, "no parser?");
            _parser = parser;
            SniContext = SniContext.Snix_GetMarsSession;

            Debug.Assert(null != _parser._physicalStateObj, "no physical session?");
            Debug.Assert(null != _parser._physicalStateObj._inBuff, "no in buffer?");
            Debug.Assert(null != _parser._physicalStateObj._outBuff, "no out buffer?");
            Debug.Assert(_parser._physicalStateObj._outBuff.Length ==
                         _parser._physicalStateObj._inBuff.Length, "Unexpected unequal buffers.");

            // Determine packet size based on physical connection buffer lengths.
            SetPacketSize(_parser._physicalStateObj._outBuff.Length);

            CreateSessionHandle(physicalConnection, async);

            if (IsFailedHandle())
            {
                AddError(parser.ProcessSNIError(this));
                ThrowExceptionAndWarning();
            }

            // we post a callback that represents the call to dispose; once the
            // object is disposed, the next callback will cause the GC Handle to
            // be released.
            IncrementPendingCallbacks();
            _lastSuccessfulIOTimer = parser._physicalStateObj._lastSuccessfulIOTimer;
        }

        ////////////////
        // Properties //
        ////////////////

        // BcpLock - use to lock this object if there is a potential risk of using this object
        // between tds packets
        internal bool BcpLock
        {
            get
            {
                return _bcpLock;
            }
            set
            {
                _bcpLock = value;
            }
        }

#if DEBUG
        internal SniContext DebugOnlyCopyOfSniContext
        {
            get
            {
                return _debugOnlyCopyOfSniContext;
            }
        }
#endif

        internal bool HasOpenResult
        {
            get
            {
                return _hasOpenResult;
            }
        }

#if DEBUG
        internal void InvalidateDebugOnlyCopyOfSniContext()
        {
            _debugOnlyCopyOfSniContext = SniContext.Undefined;
        }
#endif

        internal bool IsOrphaned
        {
            get
            {
                Debug.Assert((0 == _activateCount && !_owner.IsAlive) // in pool
                             || (1 == _activateCount && _owner.IsAlive && _owner.Target != null)
                             || (1 == _activateCount && !_owner.IsAlive), "Unknown state on TdsParserStateObject.IsOrphaned!");
                return (0 != _activateCount && !_owner.IsAlive);
            }
        }

        internal object Owner
        {
            set
            {
                Debug.Assert(value == null || !_owner.IsAlive || ((value is SqlDataReader) && (((SqlDataReader)value).Command == _owner.Target)), "Should not be changing the owner of an owned stateObj");
                SqlDataReader reader = value as SqlDataReader;
                if (reader == null)
                {
                    _readerState = null;
                }
                else
                {
                    _readerState = reader._sharedState;
                }
                _owner.Target = value;
            }
        }

        internal abstract uint DisabeSsl();

        internal bool HasOwner
        {
            get
            {
                return _owner.IsAlive;
            }
        }

        internal TdsParser Parser
        {
            get
            {
                return _parser;
            }
        }

        internal abstract uint EnableMars(ref UInt32 info);

        internal SniContext SniContext
        {
            get
            {
                return _sniContext;
            }
            set
            {
                _sniContext = value;
#if DEBUG
                _debugOnlyCopyOfSniContext = value;
#endif
            }
        }

        internal abstract UInt32 Status
        {
            get;
        }

        internal abstract object SessionHandle
        {
            get;
        }

        internal bool TimeoutHasExpired
        {
            get
            {
                Debug.Assert(0 == _timeoutMilliseconds || 0 == _timeoutTime, "_timeoutTime hasn't been reset");
                return TdsParserStaticMethods.TimeoutHasExpired(_timeoutTime);
            }
        }

        internal long TimeoutTime
        {
            get
            {
                if (0 != _timeoutMilliseconds)
                {
                    _timeoutTime = TdsParserStaticMethods.GetTimeout(_timeoutMilliseconds);
                    _timeoutMilliseconds = 0;
                }
                return _timeoutTime;
            }
            set
            {
                _timeoutMilliseconds = 0;
                _timeoutTime = value;
            }
        }

        internal int GetTimeoutRemaining()
        {
            int remaining;
            if (0 != _timeoutMilliseconds)
            {
                remaining = (int)Math.Min((long)Int32.MaxValue, _timeoutMilliseconds);
                _timeoutTime = TdsParserStaticMethods.GetTimeout(_timeoutMilliseconds);
                _timeoutMilliseconds = 0;
            }
            else
            {
                remaining = TdsParserStaticMethods.GetTimeoutMilliseconds(_timeoutTime);
            }
            return remaining;
        }

        internal bool TryStartNewRow(bool isNullCompressed, int nullBitmapColumnsCount = 0)
        {
            Debug.Assert(!isNullCompressed || nullBitmapColumnsCount > 0, "Null-Compressed row requires columns count");

            if (_snapshot != null)
            {
                _snapshot.CloneNullBitmapInfo();
            }

            // initialize or unset null bitmap information for the current row
            if (isNullCompressed)
            {
                // assert that NBCROW is not in use by Yukon or before
                Debug.Assert(_parser.IsKatmaiOrNewer, "NBCROW is sent by pre-Katmai server");

                if (!_nullBitmapInfo.TryInitialize(this, nullBitmapColumnsCount))
                {
                    return false;
                }
            }
            else
            {
                _nullBitmapInfo.Clean();
            }

            return true;
        }

        internal bool IsRowTokenReady()
        {
            // Removing one byte since TryReadByteArray\TryReadByte will aggressively read the next packet if there is no data left - so we need to ensure there is a spare byte
            int bytesRemaining = Math.Min(_inBytesPacket, _inBytesRead - _inBytesUsed) - 1;

            if (bytesRemaining > 0)
            {
                if (_inBuff[_inBytesUsed] == TdsEnums.SQLROW)
                {
                    // At a row token, so we're ready
                    return true;
                }
                else if (_inBuff[_inBytesUsed] == TdsEnums.SQLNBCROW)
                {
                    // NBC row token, ensure that we have enough data for the bitmap
                    // SQLNBCROW + Null Bitmap (copied from NullBitmap.TryInitialize)
                    int bytesToRead = 1 + (_cleanupMetaData.Length + 7) / 8;
                    return (bytesToRead <= bytesRemaining);
                }
            }

            // No data left, or not at a row token
            return false;
        }

        internal bool IsNullCompressionBitSet(int columnOrdinal)
        {
            return _nullBitmapInfo.IsGuaranteedNull(columnOrdinal);
        }

        private struct NullBitmap
        {
            private byte[] _nullBitmap;
            private int _columnsCount; // set to 0 if not used or > 0 for NBC rows

            internal bool TryInitialize(TdsParserStateObject stateObj, int columnsCount)
            {
                _columnsCount = columnsCount;
                // 1-8 columns need 1 byte
                // 9-16: 2 bytes, and so on
                int bitmapArrayLength = (columnsCount + 7) / 8;

                // allow reuse of previously allocated bitmap
                if (_nullBitmap == null || _nullBitmap.Length != bitmapArrayLength)
                {
                    _nullBitmap = new byte[bitmapArrayLength];
                }

                // read the null bitmap compression information from TDS
                if (!stateObj.TryReadByteArray(_nullBitmap, 0, _nullBitmap.Length))
                {
                    return false;
                }


                return true;
            }

            internal bool ReferenceEquals(NullBitmap obj)
            {
                return object.ReferenceEquals(_nullBitmap, obj._nullBitmap);
            }

            internal NullBitmap Clone()
            {
                NullBitmap newBitmap = new NullBitmap();
                newBitmap._nullBitmap = _nullBitmap == null ? null : (byte[])_nullBitmap.Clone();
                newBitmap._columnsCount = _columnsCount;
                return newBitmap;
            }

            internal void Clean()
            {
                _columnsCount = 0;
                // no need to free _nullBitmap array - it is cached for the next row
            }

            /// <summary>
            /// If this method returns true, the value is guaranteed to be null. This is not true vice versa: 
            /// if the bitmap value is false (if this method returns false), the value can be either null or non-null - no guarantee in this case.
            /// To determine whether it is null or not, read it from the TDS (per NBCROW design spec, for IMAGE/TEXT/NTEXT columns server might send
            /// bitmap = 0, when the actual value is null).
            /// </summary>
            internal bool IsGuaranteedNull(int columnOrdinal)
            {
                if (_columnsCount == 0)
                {
                    // not an NBC row
                    return false;
                }

                Debug.Assert(columnOrdinal >= 0 && columnOrdinal < _columnsCount, "Invalid column ordinal");

                byte testBit = (byte)(1 << (columnOrdinal & 0x7)); // columnOrdinal & 0x7 == columnOrdinal MOD 0x7
                byte testByte = _nullBitmap[columnOrdinal >> 3];
                return (testBit & testByte) != 0;
            }
        }

        /////////////////////
        // General methods //
        /////////////////////

        // If this object is part of a TdsParserSessionPool, then this *must* be called inside the pool's lock
        internal void Activate(object owner)
        {
            Debug.Assert(_parser.MARSOn, "Can not activate a non-MARS connection");
            Owner = owner; // must assign an owner for reclamation to work
            int result = Interlocked.Increment(ref _activateCount);   // must have non-zero activation count for reclamation to work too.
            Debug.Assert(result == 1, "invalid deactivate count");
        }

        // This method is only called by the command or datareader as a result of a user initiated
        // cancel request.
        internal void Cancel(object caller)
        {
            Debug.Assert(caller != null, "Null caller for Cancel!");
            Debug.Assert(caller is SqlCommand || caller is SqlDataReader, "Calling API with invalid caller type: " + caller.GetType());

            bool hasLock = false;
            try
            {
                // Keep looping until we either grabbed the lock (and therefore sent attention) or the connection closes\breaks
                while ((!hasLock) && (_parser.State != TdsParserState.Closed) && (_parser.State != TdsParserState.Broken))
                {
                    Monitor.TryEnter(this, _waitForCancellationLockPollTimeout, ref hasLock);
                    if (hasLock)
                    { // Lock for the time being - since we need to synchronize the attention send.
                      // This lock is also protecting against concurrent close and async continuations

                        // Ensure that, once we have the lock, that we are still the owner
                        if ((!_cancelled) && (_cancellationOwner.Target == caller))
                        {
                            _cancelled = true;

                            if (_pendingData && !_attentionSent)
                            {
                                bool hasParserLock = false;
                                // Keep looping until we have the parser lock (and so are allowed to write), or the connection closes\breaks
                                while ((!hasParserLock) && (_parser.State != TdsParserState.Closed) && (_parser.State != TdsParserState.Broken))
                                {
                                    try
                                    {
                                        _parser.Connection._parserLock.Wait(canReleaseFromAnyThread: false, timeout: _waitForCancellationLockPollTimeout, lockTaken: ref hasParserLock);
                                        if (hasParserLock)
                                        {
                                            _parser.Connection.ThreadHasParserLockForClose = true;
                                            SendAttention();
                                        }
                                    }
                                    finally
                                    {
                                        if (hasParserLock)
                                        {
                                            if (_parser.Connection.ThreadHasParserLockForClose)
                                            {
                                                _parser.Connection.ThreadHasParserLockForClose = false;
                                            }
                                            _parser.Connection._parserLock.Release();
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            finally
            {
                if (hasLock)
                {
                    Monitor.Exit(this);
                }
            }
        }

        // CancelRequest - use to cancel while writing a request to the server
        //
        // o none of the request might have been sent to the server, simply reset the buffer,
        //   sending attention does not hurt
        // o the request was partially written. Send an ignore header to the server. attention is
        //   required if the server was waiting for data (e.g. insert bulk rows)
        // o the request was completely written out and the server started to process the request.
        //   attention is required to have the server stop processing.
        //
        internal void CancelRequest()
        {
            ResetBuffer();    // clear out unsent buffer
            // If the first sqlbulkcopy timeout, _outputPacketNumber may not be 1, 
            // the next sqlbulkcopy (same connection string) requires this to be 1, hence reset 
            // it here when exception happens in the first sqlbulkcopy
            _outputPacketNumber = 1;

            // VSDD#907507, if bulkcopy write timeout happens, it already sent the attention, 
            // so no need to send it again
            if (!_bulkCopyWriteTimeout)
            {
                SendAttention();
                Parser.ProcessPendingAck(this);
            }
        }

        public void CheckSetResetConnectionState(UInt32 error, CallbackType callbackType)
        {
            // Should only be called for MARS - that is the only time we need to take
            // the ResetConnection lock!

            // It was raised in a security review by Microsoft questioning whether
            // we need to actually process the resulting packet (sp_reset ack or error) to know if the
            // reset actually succeeded.  There was a concern that if the reset failed and we proceeded
            // there might be a security issue present.  We have been assured by the server that if
            // sp_reset fails, they guarantee they will kill the resulting connection.  So - it is
            // safe for us to simply receive the packet and then consume the pre-login later.

            Debug.Assert(_parser.MARSOn, "Should not be calling CheckSetResetConnectionState on non MARS connection");

            if (_fResetEventOwned)
            {
                if (callbackType == CallbackType.Read && TdsEnums.SNI_SUCCESS == error)
                {
                    // RESET SUCCEEDED!
                    // If we are on read callback and no error occurred (and we own reset event) -
                    // then we sent the sp_reset_connection and so we need to reset sp_reset_connection
                    // flag to false, and then release the ResetEvent.
                    _parser._fResetConnection = false;
                    _fResetConnectionSent = false;
                    _fResetEventOwned = !_parser._resetConnectionEvent.Set();
                    Debug.Assert(!_fResetEventOwned, "Invalid AutoResetEvent state!");
                }

                if (TdsEnums.SNI_SUCCESS != error)
                {
                    // RESET FAILED!

                    // If write or read failed with reset, we need to clear event but not mark connection
                    // as reset.
                    _fResetConnectionSent = false;
                    _fResetEventOwned = !_parser._resetConnectionEvent.Set();
                    Debug.Assert(!_fResetEventOwned, "Invalid AutoResetEvent state!");
                }
            }
        }

        internal void CloseSession()
        {
            ResetCancelAndProcessAttention();
#if DEBUG
            InvalidateDebugOnlyCopyOfSniContext();
#endif
            Parser.PutSession(this);
        }

        private void ResetCancelAndProcessAttention()
        {
            // This method is shared by CloseSession initiated by DataReader.Close or completed
            // command execution, as well as the session reclamation code for cases where the
            // DataReader is opened and then GC'ed.
            lock (this)
            {
                // Reset cancel state.
                _cancelled = false;
                _cancellationOwner.Target = null;

                if (_attentionSent)
                {
                    // Make sure we're cleaning up the AttentionAck if Cancel happened before taking the lock.
                    // We serialize Cancel/CloseSession to prevent a race condition between these two states.
                    // The problem is that both sending and receiving attentions are time taking
                    // operations.
                    Parser.ProcessPendingAck(this);
                }
                _internalTimeout = false;
            }
        }

        internal abstract void CreatePhysicalSNIHandle(string serverName, bool ignoreSniOpenTimeout, long timerExpire, out byte[] instanceName, ref byte[] spnBuffer, bool flushCache, bool async, bool fParallel, bool isIntegratedSecurity = false);

        internal abstract uint SniGetConnectionId(ref Guid clientConnectionId);

        internal abstract bool IsFailedHandle();

        protected abstract void CreateSessionHandle(TdsParserStateObject physicalConnection, bool async);

        protected abstract void FreeGcHandle(int remaining, bool release);

        internal abstract uint EnableSsl(ref uint info);

        internal abstract uint WaitForSSLHandShakeToComplete();

        internal abstract void Dispose();

        internal abstract void DisposePacketCache();

        internal abstract bool IsPacketEmpty(object readPacket);

        internal abstract object ReadSyncOverAsync(int timeoutRemaining, bool isMarsOn, out uint error);

        internal abstract object ReadAsync(out uint error, ref object handle);

        internal abstract uint CheckConnection();

        internal abstract uint SetConnectionBufferSize(ref uint unsignedPacketSize);

        internal abstract void ReleasePacket(object syncReadPacket);

        protected abstract uint SNIPacketGetData(object packet, byte[] _inBuff, ref uint dataSize);

        internal abstract object GetResetWritePacket();

        internal abstract void ClearAllWritePackets();

        internal abstract object AddPacketToPendingList(object packet);

        protected abstract void RemovePacketFromPendingList(object pointer);

        internal abstract uint GenerateSspiClientContext(byte[] receivedBuff, uint receivedLength, ref byte[] sendBuff, ref uint sendLength, byte[] _sniSpnBuffer);

        internal bool Deactivate()
        {
            bool goodForReuse = false;

            try
            {
                TdsParserState state = Parser.State;
                if (state != TdsParserState.Broken && state != TdsParserState.Closed)
                {
                    if (_pendingData)
                    {
                        Parser.DrainData(this); // This may throw - taking us to catch block.c
                    }

                    if (HasOpenResult)
                    {
                        DecrementOpenResultCount();
                    }

                    ResetCancelAndProcessAttention();
                    goodForReuse = true;
                }
            }
            catch (Exception e)
            {
                if (!ADP.IsCatchableExceptionType(e))
                {
                    throw;
                }
            }
            return goodForReuse;
        }

        // If this object is part of a TdsParserSessionPool, then this *must* be called inside the pool's lock
        internal void RemoveOwner()
        {
            if (_parser.MARSOn)
            {
                // We only care about the activation count for MARS connections
                int result = Interlocked.Decrement(ref _activateCount);   // must have non-zero activation count for reclamation to work too.
                Debug.Assert(result == 0, "invalid deactivate count");
            }
            Owner = null;
        }

        internal void DecrementOpenResultCount()
        {
            if (_executedUnderTransaction == null)
            {
                // If we were not executed under a transaction - decrement the global count
                // on the parser.
                _parser.DecrementNonTransactedOpenResultCount();
            }
            else
            {
                // If we were executed under a transaction - decrement the count on the transaction.
                _executedUnderTransaction.DecrementAndObtainOpenResultCount();
                _executedUnderTransaction = null;
            }
            _hasOpenResult = false;
        }
        
        internal int DecrementPendingCallbacks(bool release)
        {
            int remaining = Interlocked.Decrement(ref _pendingCallbacks);

            FreeGcHandle(remaining, release);

            // NOTE: TdsParserSessionPool may call DecrementPendingCallbacks on a TdsParserStateObject which is already disposed
            // This is not dangerous (since the stateObj is no longer in use), but we need to add a workaround in the assert for it
            Debug.Assert((remaining == -1 && SessionHandle == null) || (0 <= remaining && remaining < 3), string.Format("_pendingCallbacks values is invalid after decrementing: {0}", remaining));
            return remaining;
        }

        internal void DisposeCounters()
        {
            Timer networkPacketTimeout = _networkPacketTimeout;
            if (networkPacketTimeout != null)
            {
                _networkPacketTimeout = null;
                networkPacketTimeout.Dispose();
            }

            Debug.Assert(Volatile.Read(ref _readingCount) >= 0, "_readingCount is negative");
            if (Volatile.Read(ref _readingCount) > 0)
            {
                // if _reading is true, we need to wait for it to complete
                // if _reading is false, then future read attempts will
                // already see the null _sessionHandle and abort.

                // We block after nulling _sessionHandle but before disposing it
                // to give a chance for a read that has already grabbed the
                // handle to complete.
                SpinWait.SpinUntil(() => Volatile.Read(ref _readingCount) == 0);
            }
        }

        internal Int32 IncrementAndObtainOpenResultCount(SqlInternalTransaction transaction)
        {
            _hasOpenResult = true;

            if (transaction == null)
            {
                // If we are not passed a transaction, we are not executing under a transaction
                // and thus we should increment the global connection result count.
                return _parser.IncrementNonTransactedOpenResultCount();
            }
            else
            {
                // If we are passed a transaction, we are executing under a transaction
                // and thus we should increment the transaction's result count.
                _executedUnderTransaction = transaction;
                return transaction.IncrementAndObtainOpenResultCount();
            }
        }

        internal int IncrementPendingCallbacks()
        {
            int remaining = Interlocked.Increment(ref _pendingCallbacks);
            Debug.Assert(0 < remaining && remaining <= 3, string.Format("_pendingCallbacks values is invalid after incrementing: {0}", remaining));
            return remaining;
        }

        internal void SetTimeoutSeconds(int timeout)
        {
            SetTimeoutMilliseconds((long)timeout * 1000L);
        }

        internal void SetTimeoutMilliseconds(long timeout)
        {
            if (timeout <= 0)
            {
                // 0 or less (i.e. Timespan.Infinite) == infinite (which is represented by Int64.MaxValue)
                _timeoutMilliseconds = 0;
                _timeoutTime = long.MaxValue;
            }
            else
            {
                _timeoutMilliseconds = timeout;
                _timeoutTime = 0;
            }
        }

        internal void StartSession(object cancellationOwner)
        {
            _cancellationOwner.Target = cancellationOwner;
        }

        internal void ThrowExceptionAndWarning(bool callerHasConnectionLock = false, bool asyncClose = false)
        {
            _parser.ThrowExceptionAndWarning(this, callerHasConnectionLock, asyncClose);
        }

        ////////////////////////////////////////////
        // TDS Packet/buffer manipulation methods //
        ////////////////////////////////////////////

        internal Task ExecuteFlush()
        {
            lock (this)
            {
                if (_cancelled && 1 == _outputPacketNumber)
                {
                    ResetBuffer();
                    _cancelled = false;
                    throw SQL.OperationCancelled();
                }
                else
                {
                    Task writePacketTask = WritePacket(TdsEnums.HARDFLUSH);
                    if (writePacketTask == null)
                    {
                        _pendingData = true;
                        _messageStatus = 0;
                        return null;
                    }
                    else
                    {
                        return AsyncHelper.CreateContinuationTask(writePacketTask, () => { _pendingData = true; _messageStatus = 0; });
                    }
                }
            }
        }

        // Processes the tds header that is present in the buffer
        internal bool TryProcessHeader()
        {
            Debug.Assert(_inBytesPacket == 0, "there should not be any bytes left in packet when ReadHeader is called");

            // if the header splits buffer reads - special case!
            if ((_partialHeaderBytesRead > 0) || (_inBytesUsed + _inputHeaderLen > _inBytesRead))
            {
                // VSTS 219884: when some kind of MITM (man-in-the-middle) tool splits the network packets, the message header can be split over 
                // several network packets.
                // Note: cannot use ReadByteArray here since it uses _inBytesPacket which is not set yet.
                do
                {
                    int copy = Math.Min(_inBytesRead - _inBytesUsed, _inputHeaderLen - _partialHeaderBytesRead);
                    Debug.Assert(copy > 0, "ReadNetworkPacket read empty buffer");

                    Buffer.BlockCopy(_inBuff, _inBytesUsed, _partialHeaderBuffer, _partialHeaderBytesRead, copy);
                    _partialHeaderBytesRead += copy;
                    _inBytesUsed += copy;

                    Debug.Assert(_partialHeaderBytesRead <= _inputHeaderLen, "Read more bytes for header than required");
                    if (_partialHeaderBytesRead == _inputHeaderLen)
                    {
                        // All read
                        _partialHeaderBytesRead = 0;
                        _inBytesPacket = ((int)_partialHeaderBuffer[TdsEnums.HEADER_LEN_FIELD_OFFSET] << 8 |
                                  (int)_partialHeaderBuffer[TdsEnums.HEADER_LEN_FIELD_OFFSET + 1]) - _inputHeaderLen;

                        _messageStatus = _partialHeaderBuffer[1];
                    }
                    else
                    {
                        Debug.Assert(_inBytesUsed == _inBytesRead, "Did not use all data while reading partial header");

                        // Require more data
                        if (_parser.State == TdsParserState.Broken || _parser.State == TdsParserState.Closed)
                        {
                            // NOTE: ReadNetworkPacket does nothing if the parser state is closed or broken
                            // to avoid infinite loop, we raise an exception
                            ThrowExceptionAndWarning();
                            return true;
                        }

                        if (!TryReadNetworkPacket())
                        {
                            return false;
                        }

                        if (_internalTimeout)
                        {
                            ThrowExceptionAndWarning();
                            return true;
                        }
                    }
                } while (_partialHeaderBytesRead != 0); // This is reset to 0 once we have read everything that we need

                AssertValidState();
            }
            else
            {
                // normal header processing...
                _messageStatus = _inBuff[_inBytesUsed + 1];
                _inBytesPacket = ((int)_inBuff[_inBytesUsed + TdsEnums.HEADER_LEN_FIELD_OFFSET] << 8 |
                                              (int)_inBuff[_inBytesUsed + TdsEnums.HEADER_LEN_FIELD_OFFSET + 1]) - _inputHeaderLen;
                _inBytesUsed += _inputHeaderLen;

                AssertValidState();
            }

            if (_inBytesPacket < 0)
            {
                // either TDS stream is corrupted or there is multithreaded misuse of connection
                throw SQL.ParsingError();
            }

            return true;
        }

        // This ensure that there is data available to be read in the buffer and that the header has been processed
        // NOTE: This method (and all it calls) should be retryable without replaying a snapshot
        internal bool TryPrepareBuffer()
        {
            Debug.Assert(_inBuff != null, "packet buffer should not be null!");

            // Header spans packets, or we haven't read the header yet - process header
            if ((_inBytesPacket == 0) && (_inBytesUsed < _inBytesRead))
            {
                if (!TryProcessHeader())
                {
                    return false;
                }
                Debug.Assert(_inBytesPacket != 0, "_inBytesPacket cannot be 0 after processing header!");
                AssertValidState();
            }

            // If we're out of data, need to read more
            if (_inBytesUsed == _inBytesRead)
            {
                // If the _inBytesPacket is not zero, then we have data left in the packet, but the data in the packet
                // spans the buffer, so we can read any amount of data possible, and we do not need to call ProcessHeader
                // because there isn't a header at the beginning of the data that we are reading.
                if (_inBytesPacket > 0)
                {
                    if (!TryReadNetworkPacket())
                    {
                        return false;
                    }
                }
                else if (_inBytesPacket == 0)
                {
                    // Else we have finished the packet and so we must read as much data as possible
                    if (!TryReadNetworkPacket())
                    {
                        return false;
                    }

                    if (!TryProcessHeader())
                    {
                        return false;
                    }

                    Debug.Assert(_inBytesPacket != 0, "_inBytesPacket cannot be 0 after processing header!");
                    if (_inBytesUsed == _inBytesRead)
                    {
                        // we read a header but didn't get anything else except it
                        // VSTS 219884: it can happen that the TDS packet header and its data are split across two network packets.
                        // Read at least one more byte to get/cache the first data portion of this TDS packet
                        if (!TryReadNetworkPacket())
                        {
                            return false;
                        }
                    }
                }
                else
                {
                    Debug.Assert(false, "entered negative _inBytesPacket loop");
                }
                AssertValidState();
            }

            return true;
        }

        internal void ResetBuffer()
        {
            _outBytesUsed = _outputHeaderLen;
        }

        internal bool SetPacketSize(int size)
        {
            if (size > TdsEnums.MAX_PACKET_SIZE)
            {
                throw SQL.InvalidPacketSize();
            }
            Debug.Assert(size >= 1, "Cannot set packet size to less than 1.");
            Debug.Assert((_outBuff == null && _inBuff == null) ||
                          (_outBuff.Length == _inBuff.Length),
                          "Buffers are not in consistent state");
            Debug.Assert((_outBuff == null && _inBuff == null) ||
                          this == _parser._physicalStateObj,
                          "SetPacketSize should only be called on a stateObj with null buffers on the physicalStateObj!");
            Debug.Assert(_inBuff == null
                          || (
                          _outBytesUsed == (_outputHeaderLen + BitConverter.ToInt32(_outBuff, _outputHeaderLen)) &&
                          _outputPacketNumber == 1)
                          ||
                          (_outBytesUsed == _outputHeaderLen && _outputPacketNumber == 1),
                          "SetPacketSize called with data in the buffer!");

            if (_inBuff == null || _inBuff.Length != size)
            { // We only check _inBuff, since two buffers should be consistent.
                // Allocate or re-allocate _inBuff.
                if (_inBuff == null)
                {
                    _inBuff = new byte[size];
                    _inBytesRead = 0;
                    _inBytesUsed = 0;
                }
                else if (size != _inBuff.Length)
                {
                    // If new size is other than existing...
                    if (_inBytesRead > _inBytesUsed)
                    {
                        // if we still have data left in the buffer we must keep that array reference and then copy into new one
                        byte[] temp = _inBuff;

                        _inBuff = new byte[size];

                        // copy remainder of unused data
                        int remainingData = _inBytesRead - _inBytesUsed;
                        if ((temp.Length < _inBytesUsed + remainingData) || (_inBuff.Length < remainingData))
                        {
                            string errormessage = SR.GetString(SR.SQL_InvalidInternalPacketSize) + ' ' + temp.Length + ", " + _inBytesUsed + ", " + remainingData + ", " + _inBuff.Length;
                            throw SQL.InvalidInternalPacketSize(errormessage);
                        }
                        Buffer.BlockCopy(temp, _inBytesUsed, _inBuff, 0, remainingData);

                        _inBytesRead = _inBytesRead - _inBytesUsed;
                        _inBytesUsed = 0;

                        AssertValidState();
                    }
                    else
                    {
                        // buffer is empty - just create the new one that is double the size of the old one
                        _inBuff = new byte[size];
                        _inBytesRead = 0;
                        _inBytesUsed = 0;
                    }
                }

                // Always re-allocate _outBuff - assert is above to verify state.
                _outBuff = new byte[size];
                _outBytesUsed = _outputHeaderLen;

                AssertValidState();
                return true;
            }

            return false;
        }

        ///////////////////////////////////////
        // Buffer read methods - data values //
        ///////////////////////////////////////

        // look at the next byte without pulling it off the wire, don't just return _inBytesUsed since we may
        // have to go to the network to get the next byte.
        internal bool TryPeekByte(out byte value)
        {
            if (!TryReadByte(out value))
            {
                return false;
            }

            // now do fixup
            _inBytesPacket++;
            _inBytesUsed--;

            AssertValidState();
            return true;
        }

        // Takes a byte array, an offset, and a len and fills the array from the offset to len number of
        // bytes from the in buffer.
        public bool TryReadByteArray(byte[] buff, int offset, int len)
        {
            int ignored;
            return TryReadByteArray(buff, offset, len, out ignored);
        }

        // NOTE: This method must be retriable WITHOUT replaying a snapshot
        // Every time you call this method increment the offset and decrease len by the value of totalRead
        public bool TryReadByteArray(byte[] buff, int offset, int len, out int totalRead)
        {
            totalRead = 0;

#if DEBUG
            if (_snapshot != null && _snapshot.DoPend())
            {
                _networkPacketTaskSource = new TaskCompletionSource<object>();
                Interlocked.MemoryBarrier();

                if (_forcePendingReadsToWaitForUser)
                {
                    _realNetworkPacketTaskSource = new TaskCompletionSource<object>();
                    _realNetworkPacketTaskSource.SetResult(null);
                }
                else
                {
                    _networkPacketTaskSource.TrySetResult(null);
                }
                return false;
            }
#endif

            Debug.Assert(buff == null || buff.Length >= len, "Invalid length sent to ReadByteArray()!");

            // loop through and read up to array length
            while (len > 0)
            {
                if ((_inBytesPacket == 0) || (_inBytesUsed == _inBytesRead))
                {
                    if (!TryPrepareBuffer())
                    {
                        return false;
                    }
                }

                int bytesToRead = Math.Min(len, Math.Min(_inBytesPacket, _inBytesRead - _inBytesUsed));
                Debug.Assert(bytesToRead > 0, "0 byte read in TryReadByteArray");
                if (buff != null)
                {
                    Buffer.BlockCopy(_inBuff, _inBytesUsed, buff, offset + totalRead, bytesToRead);
                }

                totalRead += bytesToRead;
                _inBytesUsed += bytesToRead;
                _inBytesPacket -= bytesToRead;
                len -= bytesToRead;

                AssertValidState();
            }

            if ((_messageStatus != TdsEnums.ST_EOM) && ((_inBytesPacket == 0) || (_inBytesUsed == _inBytesRead)))
            {
                if (!TryPrepareBuffer())
                {
                    return false;
                }
            }

            AssertValidState();

            return true;
        }

        // Takes no arguments and returns a byte from the buffer.  If the buffer is empty, it is filled
        // before the byte is returned.
        internal bool TryReadByte(out byte value)
        {
            Debug.Assert(_inBytesUsed >= 0 && _inBytesUsed <= _inBytesRead, "ERROR - TDSParser: _inBytesUsed < 0 or _inBytesUsed > _inBytesRead");
            value = 0;

#if DEBUG
            if (_snapshot != null && _snapshot.DoPend())
            {
                _networkPacketTaskSource = new TaskCompletionSource<object>();
                Interlocked.MemoryBarrier();

                if (_forcePendingReadsToWaitForUser)
                {
                    _realNetworkPacketTaskSource = new TaskCompletionSource<object>();
                    _realNetworkPacketTaskSource.SetResult(null);
                }
                else
                {
                    _networkPacketTaskSource.TrySetResult(null);
                }
                return false;
            }
#endif

            if ((_inBytesPacket == 0) || (_inBytesUsed == _inBytesRead))
            {
                if (!TryPrepareBuffer())
                {
                    return false;
                }
            }

            // decrement the number of bytes left in the packet
            _inBytesPacket--;

            Debug.Assert(_inBytesPacket >= 0, "ERROR - TDSParser: _inBytesPacket < 0");

            // return the byte from the buffer and increment the counter for number of bytes used in the in buffer
            value = (_inBuff[_inBytesUsed++]);

            AssertValidState();
            return true;
        }

        internal bool TryReadChar(out char value)
        {
            Debug.Assert(_syncOverAsync || !_asyncReadWithoutSnapshot, "This method is not safe to call when doing sync over async");

            byte[] buffer;
            int offset;
            if (((_inBytesUsed + 2) > _inBytesRead) || (_inBytesPacket < 2))
            {
                // If the char isn't fully in the buffer, or if it isn't fully in the packet,
                // then use ReadByteArray since the logic is there to take care of that.

                if (!TryReadByteArray(_bTmp, 0, 2))
                {
                    value = '\0';
                    return false;
                }

                buffer = _bTmp;
                offset = 0;
            }
            else
            {
                // The entire char is in the packet and in the buffer, so just return it
                // and take care of the counters.

                buffer = _inBuff;
                offset = _inBytesUsed;

                _inBytesUsed += 2;
                _inBytesPacket -= 2;
            }

            AssertValidState();
            value = (char)((buffer[offset + 1] << 8) + buffer[offset]);
            return true;
        }

        internal bool TryReadInt16(out short value)
        {
            Debug.Assert(_syncOverAsync || !_asyncReadWithoutSnapshot, "This method is not safe to call when doing sync over async");

            byte[] buffer;
            int offset;
            if (((_inBytesUsed + 2) > _inBytesRead) || (_inBytesPacket < 2))
            {
                // If the int16 isn't fully in the buffer, or if it isn't fully in the packet,
                // then use ReadByteArray since the logic is there to take care of that.

                if (!TryReadByteArray(_bTmp, 0, 2))
                {
                    value = default(short);
                    return false;
                }

                buffer = _bTmp;
                offset = 0;
            }
            else
            {
                // The entire int16 is in the packet and in the buffer, so just return it
                // and take care of the counters.

                buffer = _inBuff;
                offset = _inBytesUsed;

                _inBytesUsed += 2;
                _inBytesPacket -= 2;
            }

            AssertValidState();
            value = (short)((buffer[offset + 1] << 8) + buffer[offset]);
            return true;
        }

        internal bool TryReadInt32(out int value)
        {
            Debug.Assert(_syncOverAsync || !_asyncReadWithoutSnapshot, "This method is not safe to call when doing sync over async");
            if (((_inBytesUsed + 4) > _inBytesRead) || (_inBytesPacket < 4))
            {
                // If the int isn't fully in the buffer, or if it isn't fully in the packet,
                // then use ReadByteArray since the logic is there to take care of that.

                if (!TryReadByteArray(_bTmp, 0, 4))
                {
                    value = 0;
                    return false;
                }

                AssertValidState();
                value = BitConverter.ToInt32(_bTmp, 0);
                return true;
            }
            else
            {
                // The entire int is in the packet and in the buffer, so just return it
                // and take care of the counters.

                value = BitConverter.ToInt32(_inBuff, _inBytesUsed);

                _inBytesUsed += 4;
                _inBytesPacket -= 4;

                AssertValidState();
                return true;
            }
        }

        // This method is safe to call when doing async without snapshot
        internal bool TryReadInt64(out long value)
        {
            if ((_inBytesPacket == 0) || (_inBytesUsed == _inBytesRead))
            {
                if (!TryPrepareBuffer())
                {
                    value = 0;
                    return false;
                }
            }

            if ((_bTmpRead > 0) || (((_inBytesUsed + 8) > _inBytesRead) || (_inBytesPacket < 8)))
            {
                // If the long isn't fully in the buffer, or if it isn't fully in the packet,
                // then use ReadByteArray since the logic is there to take care of that.

                int bytesRead = 0;
                if (!TryReadByteArray(_bTmp, _bTmpRead, 8 - _bTmpRead, out bytesRead))
                {
                    Debug.Assert(_bTmpRead + bytesRead <= 8, "Read more data than required");
                    _bTmpRead += bytesRead;
                    value = 0;
                    return false;
                }
                else
                {
                    Debug.Assert(_bTmpRead + bytesRead == 8, "TryReadByteArray returned true without reading all data required");
                    _bTmpRead = 0;
                    AssertValidState();
                    value = BitConverter.ToInt64(_bTmp, 0);
                    return true;
                }
            }
            else
            {
                // The entire long is in the packet and in the buffer, so just return it
                // and take care of the counters.

                value = BitConverter.ToInt64(_inBuff, _inBytesUsed);

                _inBytesUsed += 8;
                _inBytesPacket -= 8;

                AssertValidState();
                return true;
            }
        }

        internal bool TryReadUInt16(out ushort value)
        {
            Debug.Assert(_syncOverAsync || !_asyncReadWithoutSnapshot, "This method is not safe to call when doing sync over async");

            byte[] buffer;
            int offset;
            if (((_inBytesUsed + 2) > _inBytesRead) || (_inBytesPacket < 2))
            {
                // If the uint16 isn't fully in the buffer, or if it isn't fully in the packet,
                // then use ReadByteArray since the logic is there to take care of that.

                if (!TryReadByteArray(_bTmp, 0, 2))
                {
                    value = default(ushort);
                    return false;
                }

                buffer = _bTmp;
                offset = 0;
            }
            else
            {
                // The entire uint16 is in the packet and in the buffer, so just return it
                // and take care of the counters.

                buffer = _inBuff;
                offset = _inBytesUsed;

                _inBytesUsed += 2;
                _inBytesPacket -= 2;
            }

            AssertValidState();
            value = (ushort)((buffer[offset + 1] << 8) + buffer[offset]);
            return true;
        }

        // This method is safe to call when doing async without replay
        internal bool TryReadUInt32(out uint value)
        {
            if ((_inBytesPacket == 0) || (_inBytesUsed == _inBytesRead))
            {
                if (!TryPrepareBuffer())
                {
                    value = 0;
                    return false;
                }
            }

            if ((_bTmpRead > 0) || (((_inBytesUsed + 4) > _inBytesRead) || (_inBytesPacket < 4)))
            {
                // If the int isn't fully in the buffer, or if it isn't fully in the packet,
                // then use ReadByteArray since the logic is there to take care of that.

                int bytesRead = 0;
                if (!TryReadByteArray(_bTmp, _bTmpRead, 4 - _bTmpRead, out bytesRead))
                {
                    Debug.Assert(_bTmpRead + bytesRead <= 4, "Read more data than required");
                    _bTmpRead += bytesRead;
                    value = 0;
                    return false;
                }
                else
                {
                    Debug.Assert(_bTmpRead + bytesRead == 4, "TryReadByteArray returned true without reading all data required");
                    _bTmpRead = 0;
                    AssertValidState();
                    value = BitConverter.ToUInt32(_bTmp, 0);
                    return true;
                }
            }
            else
            {
                // The entire int is in the packet and in the buffer, so just return it
                // and take care of the counters.

                value = BitConverter.ToUInt32(_inBuff, _inBytesUsed);

                _inBytesUsed += 4;
                _inBytesPacket -= 4;

                AssertValidState();
                return true;
            }
        }

        internal bool TryReadSingle(out float value)
        {
            Debug.Assert(_syncOverAsync || !_asyncReadWithoutSnapshot, "This method is not safe to call when doing sync over async");
            if (((_inBytesUsed + 4) > _inBytesRead) || (_inBytesPacket < 4))
            {
                // If the float isn't fully in the buffer, or if it isn't fully in the packet,
                // then use ReadByteArray since the logic is there to take care of that.

                if (!TryReadByteArray(_bTmp, 0, 4))
                {
                    value = default(float);
                    return false;
                }

                AssertValidState();
                value = BitConverter.ToSingle(_bTmp, 0);
                return true;
            }
            else
            {
                // The entire float is in the packet and in the buffer, so just return it
                // and take care of the counters.

                value = BitConverter.ToSingle(_inBuff, _inBytesUsed);

                _inBytesUsed += 4;
                _inBytesPacket -= 4;

                AssertValidState();
                return true;
            }
        }

        internal bool TryReadDouble(out double value)
        {
            Debug.Assert(_syncOverAsync || !_asyncReadWithoutSnapshot, "This method is not safe to call when doing sync over async");
            if (((_inBytesUsed + 8) > _inBytesRead) || (_inBytesPacket < 8))
            {
                // If the double isn't fully in the buffer, or if it isn't fully in the packet,
                // then use ReadByteArray since the logic is there to take care of that.

                if (!TryReadByteArray(_bTmp, 0, 8))
                {
                    value = default(double);
                    return false;
                }

                AssertValidState();
                value = BitConverter.ToDouble(_bTmp, 0);
                return true;
            }
            else
            {
                // The entire double is in the packet and in the buffer, so just return it
                // and take care of the counters.

                value = BitConverter.ToDouble(_inBuff, _inBytesUsed);

                _inBytesUsed += 8;
                _inBytesPacket -= 8;

                AssertValidState();
                return true;
            }
        }

        internal bool TryReadString(int length, out string value)
        {
            Debug.Assert(_syncOverAsync || !_asyncReadWithoutSnapshot, "This method is not safe to call when doing sync over async");
            int cBytes = length << 1;
            byte[] buf;
            int offset = 0;

            if (((_inBytesUsed + cBytes) > _inBytesRead) || (_inBytesPacket < cBytes))
            {
                if (_bTmp == null || _bTmp.Length < cBytes)
                {
                    _bTmp = new byte[cBytes];
                }

                if (!TryReadByteArray(_bTmp, 0, cBytes))
                {
                    value = null;
                    return false;
                }

                // assign local to point to parser scratch buffer
                buf = _bTmp;

                AssertValidState();
            }
            else
            {
                // assign local to point to _inBuff
                buf = _inBuff;
                offset = _inBytesUsed;
                _inBytesUsed += cBytes;
                _inBytesPacket -= cBytes;

                AssertValidState();
            }

            value = System.Text.Encoding.Unicode.GetString(buf, offset, cBytes);
            return true;
        }

        internal bool TryReadStringWithEncoding(int length, System.Text.Encoding encoding, bool isPlp, out string value)
        {
            Debug.Assert(_syncOverAsync || !_asyncReadWithoutSnapshot, "This method is not safe to call when doing sync over async");

            if (null == encoding)
            {
                // Need to skip the current column before throwing the error - this ensures that the state shared between this and the data reader is consistent when calling DrainData
                if (isPlp)
                {
                    ulong ignored;
                    if (!_parser.TrySkipPlpValue((ulong)length, this, out ignored))
                    {
                        value = null;
                        return false;
                    }
                }
                else
                {
                    if (!TrySkipBytes(length))
                    {
                        value = null;
                        return false;
                    }
                }

                _parser.ThrowUnsupportedCollationEncountered(this);
            }
            byte[] buf = null;
            int offset = 0;

            if (isPlp)
            {
                if (!TryReadPlpBytes(ref buf, 0, Int32.MaxValue, out length))
                {
                    value = null;
                    return false;
                }

                AssertValidState();
            }
            else
            {
                if (((_inBytesUsed + length) > _inBytesRead) || (_inBytesPacket < length))
                {
                    if (_bTmp == null || _bTmp.Length < length)
                    {
                        _bTmp = new byte[length];
                    }

                    if (!TryReadByteArray(_bTmp, 0, length))
                    {
                        value = null;
                        return false;
                    }

                    // assign local to point to parser scratch buffer
                    buf = _bTmp;

                    AssertValidState();
                }
                else
                {
                    // assign local to point to _inBuff
                    buf = _inBuff;
                    offset = _inBytesUsed;
                    _inBytesUsed += length;
                    _inBytesPacket -= length;

                    AssertValidState();
                }
            }

            // BCL optimizes to not use char[] underneath
            value = encoding.GetString(buf, offset, length);
            return true;
        }

        internal ulong ReadPlpLength(bool returnPlpNullIfNull)
        {
            ulong value;
            Debug.Assert(_syncOverAsync, "Should not attempt pends in a synchronous call");
            bool result = TryReadPlpLength(returnPlpNullIfNull, out value);
            if (!result) { throw SQL.SynchronousCallMayNotPend(); }
            return value;
        }

        // Reads the length of either the entire data or the length of the next chunk in a
        //   partially length prefixed data
        // After this call, call  ReadPlpBytes/ReadPlpUnicodeChars until the specified length of data
        // is consumed. Repeat this until ReadPlpLength returns 0 in order to read the
        // entire stream.
        // When this function returns 0, it means the data stream is read completely and the
        // plp state in the tdsparser is cleaned.
        internal bool TryReadPlpLength(bool returnPlpNullIfNull, out ulong lengthLeft)
        {
            uint chunklen;
            // bool firstchunk = false;
            bool isNull = false;

            Debug.Assert(_longlenleft == 0, "Out of synch length read request");
            if (_longlen == 0)
            {
                // First chunk is being read. Find out what type of chunk it is
                long value;
                if (!TryReadInt64(out value))
                {
                    lengthLeft = 0;
                    return false;
                }
                _longlen = (ulong)value;
                // firstchunk = true;
            }

            if (_longlen == TdsEnums.SQL_PLP_NULL)
            {
                _longlen = 0;
                _longlenleft = 0;
                isNull = true;
            }
            else
            {
                // Data is coming in uint chunks, read length of next chunk
                if (!TryReadUInt32(out chunklen))
                {
                    lengthLeft = 0;
                    return false;
                }
                if (chunklen == TdsEnums.SQL_PLP_CHUNK_TERMINATOR)
                {
                    _longlenleft = 0;
                    _longlen = 0;
                }
                else
                {
                    _longlenleft = (ulong)chunklen;
                }
            }

            AssertValidState();

            if (isNull && returnPlpNullIfNull)
            {
                lengthLeft = TdsEnums.SQL_PLP_NULL;
                return true;
            }

            lengthLeft = _longlenleft;
            return true;
        }

        internal int ReadPlpBytesChunk(byte[] buff, int offset, int len)
        {
            Debug.Assert(_syncOverAsync, "Should not attempt pends in a synchronous call");
            Debug.Assert(_longlenleft > 0, "Read when no data available");

            int value;
            int bytesToRead = (int)Math.Min(_longlenleft, (ulong)len);
            bool result = TryReadByteArray(buff, offset, bytesToRead, out value);
            _longlenleft -= (ulong)bytesToRead;
            if (!result) { throw SQL.SynchronousCallMayNotPend(); }
            return value;
        }

        // Reads the requested number of bytes from a plp data stream, or the entire data if
        // requested length is -1 or larger than the actual length of data. First call to this method
        //  should be preceded by a call to ReadPlpLength or ReadDataLength.
        // Returns the actual bytes read.
        // NOTE: This method must be retriable WITHOUT replaying a snapshot
        // Every time you call this method increment the offset and decrease len by the value of totalBytesRead
        internal bool TryReadPlpBytes(ref byte[] buff, int offset, int len, out int totalBytesRead)
        {
            int bytesRead = 0;
            int bytesLeft;
            byte[] newbuf;
            ulong ignored;

            if (_longlen == 0)
            {
                Debug.Assert(_longlenleft == 0);
                if (buff == null)
                {
                    buff = Array.Empty<byte>();
                }

                AssertValidState();
                totalBytesRead = 0;
                return true;       // No data
            }

            Debug.Assert((_longlen != TdsEnums.SQL_PLP_NULL),
                    "Out of sync plp read request");

            Debug.Assert((buff == null && offset == 0) || (buff.Length >= offset + len), "Invalid length sent to ReadPlpBytes()!");
            bytesLeft = len;

            // If total length is known up front, allocate the whole buffer in one shot instead of realloc'ing and copying over each time
            if (buff == null && _longlen != TdsEnums.SQL_PLP_UNKNOWNLEN)
            {
                buff = new byte[(int)Math.Min((int)_longlen, len)];
            }

            if (_longlenleft == 0)
            {
                if (!TryReadPlpLength(false, out ignored))
                {
                    totalBytesRead = 0;
                    return false;
                }
                if (_longlenleft == 0)
                { // Data read complete
                    totalBytesRead = 0;
                    return true;
                }
            }

            if (buff == null)
            {
                buff = new byte[_longlenleft];
            }

            totalBytesRead = 0;

            while (bytesLeft > 0)
            {
                int bytesToRead = (int)Math.Min(_longlenleft, (ulong)bytesLeft);
                if (buff.Length < (offset + bytesToRead))
                {
                    // Grow the array
                    newbuf = new byte[offset + bytesToRead];
                    Buffer.BlockCopy(buff, 0, newbuf, 0, offset);
                    buff = newbuf;
                }

                bool result = TryReadByteArray(buff, offset, bytesToRead, out bytesRead);
                Debug.Assert(bytesRead <= bytesLeft, "Read more bytes than we needed");
                Debug.Assert((ulong)bytesRead <= _longlenleft, "Read more bytes than is available");

                bytesLeft -= bytesRead;
                offset += bytesRead;
                totalBytesRead += bytesRead;
                _longlenleft -= (ulong)bytesRead;
                if (!result)
                {
                    return false;
                }

                if (_longlenleft == 0)
                { // Read the next chunk or cleanup state if hit the end
                    if (!TryReadPlpLength(false, out ignored))
                    {
                        return false;
                    }
                }

                AssertValidState();

                // Catch the point where we read the entire plp data stream and clean up state
                if (_longlenleft == 0)   // Data read complete
                    break;
            }
            return true;
        }


        /////////////////////////////////////////
        // Value Skip Logic                    //
        /////////////////////////////////////////


        // Reads bytes from the buffer but doesn't return them, in effect simply deleting them.
        // Does not handle plp fields, need to use SkipPlpBytesValue for those.
        // Does not handle null values or NBC bitmask, ensure the value is not null before calling this method
        internal bool TrySkipLongBytes(long num)
        {
            Debug.Assert(_syncOverAsync || !_asyncReadWithoutSnapshot, "This method is not safe to call when doing sync over async");
            int cbSkip = 0;

            while (num > 0)
            {
                cbSkip = (int)Math.Min((long)Int32.MaxValue, num);
                if (!TryReadByteArray(null, 0, cbSkip))
                {
                    return false;
                }
                num -= (long)cbSkip;
            }

            return true;
        }

        // Reads bytes from the buffer but doesn't return them, in effect simply deleting them.
        internal bool TrySkipBytes(int num)
        {
            Debug.Assert(_syncOverAsync || !_asyncReadWithoutSnapshot, "This method is not safe to call when doing sync over async");
            return TryReadByteArray(null, 0, num);
        }

        /////////////////////////////////////////
        // Network/Packet Reading & Processing //
        /////////////////////////////////////////

        internal void SetSnapshot()
        {
            _snapshot = new StateSnapshot(this);
            _snapshot.Snap();
            _snapshotReplay = false;
        }

        internal void ResetSnapshot()
        {
            _snapshot = null;
            _snapshotReplay = false;
        }

#if DEBUG
        private string _lastStack;
#endif

        internal bool TryReadNetworkPacket()
        {
#if DEBUG
            Debug.Assert(!_shouldHaveEnoughData || _attentionSent, "Caller said there should be enough data, but we are currently reading a packet");
#endif

            if (_snapshot != null)
            {
                if (_snapshotReplay)
                {
                    if (_snapshot.Replay())
                    {
#if DEBUG
                        if (_checkNetworkPacketRetryStacks)
                        {
                            _snapshot.CheckStack(Environment.StackTrace);
                        }
#endif
                        return true;
                    }
#if DEBUG
                    else
                    {
                        if (_checkNetworkPacketRetryStacks)
                        {
                            _lastStack = Environment.StackTrace;
                        }
                    }
#endif
                }

                // previous buffer is in snapshot
                _inBuff = new byte[_inBuff.Length];
            }

            if (_syncOverAsync)
            {
                ReadSniSyncOverAsync();
                return true;
            }

            ReadSni(new TaskCompletionSource<object>());

#if DEBUG
            if (_failAsyncPends)
            {
                throw new InvalidOperationException("Attempted to pend a read when _failAsyncPends test hook was enabled");
            }
            if (_forceSyncOverAsyncAfterFirstPend)
            {
                _syncOverAsync = true;
            }
#endif
            Debug.Assert((_snapshot != null) ^ _asyncReadWithoutSnapshot, "Must have either _snapshot set up or _asyncReadWithoutSnapshot enabled (but not both) to pend a read");

            return false;
        }

        internal void PrepareReplaySnapshot()
        {
            _networkPacketTaskSource = null;
            _snapshot.PrepareReplay();
        }

        internal void ReadSniSyncOverAsync()
        {
            if (_parser.State == TdsParserState.Broken || _parser.State == TdsParserState.Closed)
            {
                throw ADP.ClosedConnectionError();
            }

            object readPacket = null;

            UInt32 error;

            bool shouldDecrement = false;
            try
            {
                Interlocked.Increment(ref _readingCount);
                shouldDecrement = true;

                readPacket = ReadSyncOverAsync(GetTimeoutRemaining(), false, out error);

                Interlocked.Decrement(ref _readingCount);
                shouldDecrement = false;

                if (_parser.MARSOn)
                { // Only take reset lock on MARS and Async.
                    CheckSetResetConnectionState(error, CallbackType.Read);
                }

                if (TdsEnums.SNI_SUCCESS == error)
                { // Success - process results!

                    Debug.Assert(!IsPacketEmpty(readPacket), "ReadNetworkPacket cannot be null in synchronous operation!");

                    ProcessSniPacket(readPacket, 0);
#if DEBUG
                    if (_forcePendingReadsToWaitForUser)
                    {
                        _networkPacketTaskSource = new TaskCompletionSource<object>();
                        Interlocked.MemoryBarrier();
                        _networkPacketTaskSource.Task.Wait();
                        _networkPacketTaskSource = null;
                    }
#endif
                }
                else
                { // Failure!

                    Debug.Assert(!IsValidPacket(readPacket), "unexpected readPacket without corresponding SNIPacketRelease");
                    
                    ReadSniError(this, error);
                }
            }
            finally
            {
                if (shouldDecrement)
                {
                    Interlocked.Decrement(ref _readingCount);
                }

                if (!IsPacketEmpty(readPacket))
                {
                    ReleasePacket(readPacket);
                }

                AssertValidState();
            }
        }

        internal void OnConnectionClosed()
        {
            // the stateObj is not null, so the async invocation that registered this callback
            // via the SqlReferenceCollection has not yet completed.  We will look for a 
            // _networkPacketTaskSource and mark it faulted.  If we don't find it, then
            // TdsParserStateObject.ReadSni will abort when it does look to see if the parser
            // is open.  If we do, then when the call that created it completes and a continuation
            // is registered, we will ensure the completion is called.

            // Note, this effort is necessary because when the app domain is being unloaded,
            // we don't get callback from SNI.

            // first mark parser broken.  This is to ensure that ReadSni will abort if it has
            // not yet executed.
            Parser.State = TdsParserState.Broken;
            Parser.Connection.BreakConnection();

            // Ensure that changing state occurs before checking _networkPacketTaskSource 
            Interlocked.MemoryBarrier();

            // then check for networkPacketTaskSource
            var taskSource = _networkPacketTaskSource;
            if (taskSource != null)
            {
                taskSource.TrySetException(ADP.ExceptionWithStackTrace(ADP.ClosedConnectionError()));
            }

            taskSource = _writeCompletionSource;
            if (taskSource != null)
            {
                taskSource.TrySetException(ADP.ExceptionWithStackTrace(ADP.ClosedConnectionError()));
            }
        }

        private void OnTimeout(object state)
        {
            if (!_internalTimeout)
            {
                _internalTimeout = true;
                // lock protects against Close and Cancel
                lock (this)
                {
                    if (!_attentionSent)
                    {
                        AddError(new SqlError(TdsEnums.TIMEOUT_EXPIRED, (byte)0x00, TdsEnums.MIN_ERROR_CLASS, _parser.Server, _parser.Connection.TimeoutErrorInternal.GetErrorMessage(), "", 0, TdsEnums.SNI_WAIT_TIMEOUT));

                        // Grab a reference to the _networkPacketTaskSource in case it becomes null while we are trying to use it
                        TaskCompletionSource<object> source = _networkPacketTaskSource;

                        if (_parser.Connection.IsInPool)
                        {
                            // We should never timeout if the connection is currently in the pool: the safest thing to do here is to doom the connection to avoid corruption
                            Debug.Assert(_parser.Connection.IsConnectionDoomed, "Timeout occurred while the connection is in the pool");
                            _parser.State = TdsParserState.Broken;
                            _parser.Connection.BreakConnection();
                            if (source != null)
                            {
                                source.TrySetCanceled();
                            }
                        }
                        else if (_parser.State == TdsParserState.OpenLoggedIn)
                        {
                            try
                            {
                                SendAttention(mustTakeWriteLock: true);
                            }
                            catch (Exception e)
                            {
                                if (!ADP.IsCatchableExceptionType(e))
                                {
                                    throw;
                                }
                                // if unable to send attention, cancel the _networkPacketTaskSource to
                                // request the parser be broken.  SNIWritePacket errors will already
                                // be in the _errors collection.
                                if (source != null)
                                {
                                    source.TrySetCanceled();
                                }
                            }
                        }

                        // If we still haven't received a packet then we don't want to actually close the connection
                        // from another thread, so complete the pending operation as cancelled, informing them to break it
                        if (source != null)
                        {
                            Task.Delay(AttentionTimeoutSeconds * 1000).ContinueWith(_ =>
                            {
                                // Only break the connection if the read didn't finish
                                if (!source.Task.IsCompleted)
                                {
                                    int pendingCallback = IncrementPendingCallbacks();
                                    try
                                    {
                                        // If pendingCallback is at 3, then ReadAsyncCallback hasn't been called yet
                                        // So it is safe for us to break the connection and cancel the Task (since we are not sure that ReadAsyncCallback will ever be called)
                                        if ((pendingCallback == 3) && (!source.Task.IsCompleted))
                                        {
                                            Debug.Assert(source == _networkPacketTaskSource, "_networkPacketTaskSource which is being timed is not the current task source");

                                            // Try to throw the timeout exception and store it in the task
                                            bool exceptionStored = false;
                                            try
                                            {
                                                CheckThrowSNIException();
                                            }
                                            catch (Exception ex)
                                            {
                                                if (source.TrySetException(ex))
                                                {
                                                    exceptionStored = true;
                                                }
                                            }

                                            // Ensure that the connection is no longer usable 
                                            // This is needed since the timeout error added above is non-fatal (and so throwing it won't break the connection)
                                            _parser.State = TdsParserState.Broken;
                                            _parser.Connection.BreakConnection();

                                            // If we didn't get an exception (something else observed it?) then ensure that the task is cancelled
                                            if (!exceptionStored)
                                            {
                                                source.TrySetCanceled();
                                            }
                                        }
                                    }
                                    finally
                                    {
                                        DecrementPendingCallbacks(release: false);
                                    }
                                }
                            });
                        }
                    }
                }
            }
        }

        internal void ReadSni(TaskCompletionSource<object> completion)
        {

            Debug.Assert(_networkPacketTaskSource == null || ((_asyncReadWithoutSnapshot) && (_networkPacketTaskSource.Task.IsCompleted)), "Pending async call or failed to replay snapshot when calling ReadSni");
            _networkPacketTaskSource = completion;

            // Ensure that setting the completion source is completed before checking the state
            Interlocked.MemoryBarrier();

            // We must check after assigning _networkPacketTaskSource to avoid races with
            // SqlCommand.OnConnectionClosed
            if (_parser.State == TdsParserState.Broken || _parser.State == TdsParserState.Closed)
            {
                throw ADP.ClosedConnectionError();
            }

#if DEBUG
            if (_forcePendingReadsToWaitForUser)
            {
                _realNetworkPacketTaskSource = new TaskCompletionSource<object>();
            }
#endif


            object readPacket = null;

            UInt32 error = 0;

            try
            {
                Debug.Assert(completion != null, "Async on but null asyncResult passed");

                if (_networkPacketTimeout == null)
                {
                    _networkPacketTimeout = new Timer(OnTimeout, null, Timeout.Infinite, Timeout.Infinite);
                }

                // -1 == Infinite
                //  0 == Already timed out (NOTE: To simulate the same behavior as sync we will only timeout on 0 if we receive an IO Pending from SNI)
                // >0 == Actual timeout remaining
                int msecsRemaining = GetTimeoutRemaining();
                if (msecsRemaining > 0)
                {
                    ChangeNetworkPacketTimeout(msecsRemaining, Timeout.Infinite);
                }

                object handle = null;

                Interlocked.Increment(ref _readingCount);

                handle = SessionHandle;
                if (handle != null)
                {
                    IncrementPendingCallbacks();

                    readPacket = ReadAsync(out error, ref handle);

                    if (!(TdsEnums.SNI_SUCCESS == error || TdsEnums.SNI_SUCCESS_IO_PENDING == error))
                    {
                        DecrementPendingCallbacks(false); // Failure - we won't receive callback!
                    }
                }

                Interlocked.Decrement(ref _readingCount);
                
                if (handle == null)
                {
                    throw ADP.ClosedConnectionError();
                }

                if (TdsEnums.SNI_SUCCESS == error)
                { // Success - process results!
                    Debug.Assert(IsValidPacket(readPacket), "ReadNetworkPacket should not have been null on this async operation!");
                    // Evaluate this condition for MANAGED_SNI. This may not be needed because the network call is happening Async and only the callback can receive a success.
                    ReadAsyncCallback(IntPtr.Zero, readPacket, 0);
                }
                else if (TdsEnums.SNI_SUCCESS_IO_PENDING != error)
                { // FAILURE!
                    Debug.Assert(IsPacketEmpty(readPacket), "unexpected readPacket without corresponding SNIPacketRelease");

                    ReadSniError(this, error);
#if DEBUG
                    if ((_forcePendingReadsToWaitForUser) && (_realNetworkPacketTaskSource != null))
                    {
                        _realNetworkPacketTaskSource.TrySetResult(null);
                    }
                    else
#endif
                    {
                        _networkPacketTaskSource.TrySetResult(null);
                    }
                    // Disable timeout timer on error
                    ChangeNetworkPacketTimeout(Timeout.Infinite, Timeout.Infinite);
                }
                else if (msecsRemaining == 0)
                { // Got IO Pending, but we have no time left to wait
                    // Immediately schedule the timeout timer to fire
                    ChangeNetworkPacketTimeout(0, Timeout.Infinite);
                }
                // DO NOT HANDLE PENDING READ HERE - which is TdsEnums.SNI_SUCCESS_IO_PENDING state.
                // That is handled by user who initiated async read, or by ReadNetworkPacket which is sync over async.
            }
            finally
            {
                if (!TdsParserStateObjectFactory.UseManagedSNI)
                {
                    if (!IsPacketEmpty(readPacket))
                    {
                        // Be sure to release packet, otherwise it will be leaked by native.
                        ReleasePacket(readPacket);
                    }
                }
                AssertValidState();
            }
        }

        /// <summary>
        /// Checks to see if the underlying connection is still alive (used by connection pool resiliency)
        /// NOTE: This is not safe to do on a connection that is currently in use
        /// NOTE: This will mark the connection as broken if it is found to be dead
        /// </summary>
        /// <param name="throwOnException">If true then an exception will be thrown if the connection is found to be dead, otherwise no exception will be thrown</param>
        /// <returns>True if the connection is still alive, otherwise false</returns>
        internal bool IsConnectionAlive(bool throwOnException)
        {
            Debug.Assert(_parser.Connection == null || _parser.Connection.Pool != null, "Shouldn't be calling IsConnectionAlive on non-pooled connections");
            bool isAlive = true;

            if (DateTime.UtcNow.Ticks - _lastSuccessfulIOTimer._value > CheckConnectionWindow)
            {
                if ((_parser == null) || ((_parser.State == TdsParserState.Broken) || (_parser.State == TdsParserState.Closed)))
                {
                    isAlive = false;
                    if (throwOnException)
                    {
                        throw SQL.ConnectionDoomed();
                    }
                }
                else if ((_pendingCallbacks > 1) || ((_parser.Connection != null) && (!_parser.Connection.IsInPool)))
                {
                    // This connection is currently in use, assume that the connection is 'alive'
                    // NOTE: SNICheckConnection is not currently supported for connections that are in use
                    Debug.Assert(true, "Call to IsConnectionAlive while connection is in use");
                }
                else
                {
                    UInt32 error;

                    object readPacket = EmptyReadPacket;

                    try
                    {
                        SniContext = SniContext.Snix_Connect;

                        error = CheckConnection();

                        if ((error != TdsEnums.SNI_SUCCESS) && (error != TdsEnums.SNI_WAIT_TIMEOUT))
                        {
                            // Connection is dead
                            isAlive = false;
                            if (throwOnException)
                            {
                                // Get the error from SNI so that we can throw the correct exception
                                AddError(_parser.ProcessSNIError(this));
                                ThrowExceptionAndWarning();
                            }
                        }
                        else
                        {
                            _lastSuccessfulIOTimer._value = DateTime.UtcNow.Ticks;
                        }
                    }
                    finally
                    {
                        if (!IsPacketEmpty(readPacket))
                        {
                            ReleasePacket(readPacket);
                        }
                    }
                }
            }

            return isAlive;
        }

        /// <summary>
        /// Checks to see if the underlying connection is still valid (used by idle connection resiliency - for active connections) 
        /// NOTE: This is not safe to do on a connection that is currently in use
        /// NOTE: This will mark the connection as broken if it is found to be dead
        /// </summary>
        /// <returns>True if the connection is still alive, otherwise false</returns>
        internal bool ValidateSNIConnection()
        {
            if ((_parser == null) || ((_parser.State == TdsParserState.Broken) || (_parser.State == TdsParserState.Closed)))
            {
                return false;
            }

            if (DateTime.UtcNow.Ticks - _lastSuccessfulIOTimer._value <= CheckConnectionWindow)
            {
                return true;
            }

            UInt32 error = TdsEnums.SNI_SUCCESS;
            SniContext = SniContext.Snix_Connect;
            try
            {
                Interlocked.Increment(ref _readingCount);
                error = CheckConnection();
            }
            finally
            {
                Interlocked.Decrement(ref _readingCount);
            }
            return (error == TdsEnums.SNI_SUCCESS) || (error == TdsEnums.SNI_WAIT_TIMEOUT);
        }

        // This method should only be called by ReadSni!  If not - it may have problems with timeouts!
        private void ReadSniError(TdsParserStateObject stateObj, UInt32 error)
        {
            if (TdsEnums.SNI_WAIT_TIMEOUT == error)
            {
                Debug.Assert(_syncOverAsync, "Should never reach here with async on!");
                bool fail = false;

                if (_internalTimeout)
                { // This is now our second timeout - time to give up.
                    fail = true;
                }
                else
                {
                    stateObj._internalTimeout = true;
                    Debug.Assert(_parser.Connection != null, "SqlConnectionInternalTds handler can not be null at this point.");
                    AddError(new SqlError(TdsEnums.TIMEOUT_EXPIRED, (byte)0x00, TdsEnums.MIN_ERROR_CLASS, _parser.Server, _parser.Connection.TimeoutErrorInternal.GetErrorMessage(), "", 0, TdsEnums.SNI_WAIT_TIMEOUT));

                    if (!stateObj._attentionSent)
                    {
                        if (stateObj.Parser.State == TdsParserState.OpenLoggedIn)
                        {
                            stateObj.SendAttention(mustTakeWriteLock: true);

                            object syncReadPacket = null;

                            bool shouldDecrement = false;
                            try
                            {
                                Interlocked.Increment(ref _readingCount);
                                shouldDecrement = true;

                                syncReadPacket = ReadSyncOverAsync(stateObj.GetTimeoutRemaining(), _parser.MARSOn, out error);

                                Interlocked.Decrement(ref _readingCount);
                                shouldDecrement = false;

                                if (TdsEnums.SNI_SUCCESS == error)
                                {
                                    // We will end up letting the run method deal with the expected done:done_attn token stream.
                                    stateObj.ProcessSniPacket(syncReadPacket, 0);
                                    return;
                                }
                                else
                                {
                                    Debug.Assert(!IsValidPacket(syncReadPacket), "unexpected syncReadPacket without corresponding SNIPacketRelease");
                                    fail = true; // Subsequent read failed, time to give up.
                                }
                            }
                            finally
                            {
                                if (shouldDecrement)
                                {
                                    Interlocked.Decrement(ref _readingCount);
                                }

                                if (!IsPacketEmpty(syncReadPacket))
                                {
                                    ReleasePacket(syncReadPacket);
                                }
                            }
                        }
                        else
                        {
                            if (_parser._loginWithFailover)
                            {
                                // For DbMirroring Failover during login, never break the connection, just close the TdsParser
                                _parser.Disconnect();
                            }
                            else if ((_parser.State == TdsParserState.OpenNotLoggedIn) && (_parser.Connection.ConnectionOptions.MultiSubnetFailover))
                            {
                                // For MultiSubnet Failover during login, never break the connection, just close the TdsParser
                                _parser.Disconnect();
                            }
                            else
                                fail = true; // We aren't yet logged in - just fail.
                        }
                    }
                }

                if (fail)
                {
                    _parser.State = TdsParserState.Broken; // We failed subsequent read, we have to quit!
                    _parser.Connection.BreakConnection();
                }
            }
            else
            {
                // Caution: ProcessSNIError  always  returns a fatal error!
                AddError(_parser.ProcessSNIError(stateObj));
            }
            ThrowExceptionAndWarning();

            AssertValidState();
        }

        public void ProcessSniPacket(object packet, UInt32 error)
        {
            if (error != 0)
            {
                if ((_parser.State == TdsParserState.Closed) || (_parser.State == TdsParserState.Broken))
                {
                    // Do nothing with callback if closed or broken and error not 0 - callback can occur
                    // after connection has been closed.  PROBLEM IN NETLIB - DESIGN FLAW.
                    return;
                }

                AddError(_parser.ProcessSNIError(this));
                AssertValidState();
            }
            else
            {
                UInt32 dataSize = 0;

                UInt32 getDataError = SNIPacketGetData(packet, _inBuff, ref dataSize);

                if (getDataError == TdsEnums.SNI_SUCCESS)
                {
                    if (_inBuff.Length < dataSize)
                    {
                        Debug.Assert(true, "Unexpected dataSize on Read");
                        throw SQL.InvalidInternalPacketSize(SR.GetString(SR.SqlMisc_InvalidArraySizeMessage));
                    }

                    _lastSuccessfulIOTimer._value = DateTime.UtcNow.Ticks;
                    _inBytesRead = (int)dataSize;
                    _inBytesUsed = 0;

                    if (_snapshot != null)
                    {
                        _snapshot.PushBuffer(_inBuff, _inBytesRead);
                        if (_snapshotReplay)
                        {
                            _snapshot.Replay();
#if DEBUG
                            _snapshot.AssertCurrent();
#endif
                        }
                    }

                    SniReadStatisticsAndTracing();


                    AssertValidState();
                }
                else
                {
                    throw SQL.ParsingError();
                }
            }
        }

        private void ChangeNetworkPacketTimeout(int dueTime, int period)
        {
            Timer networkPacketTimeout = _networkPacketTimeout;
            if (networkPacketTimeout != null)
            {
                try
                {
                    networkPacketTimeout.Change(dueTime, period);
                }
                catch (ObjectDisposedException)
                {
                    // _networkPacketTimeout is set to null before Disposing, but there is still a slight chance
                    // that object was disposed after we took a copy
                }
            }
        }

        public void ReadAsyncCallback<T>(T packet, UInt32 error)
        {
            ReadAsyncCallback(IntPtr.Zero, packet, error);
        }


        public void ReadAsyncCallback<T>(IntPtr key, T packet, UInt32 error)
        {
            // Key never used.
            // Note - it's possible that when native calls managed that an asynchronous exception
            // could occur in the native->managed transition, which would
            // have two impacts:
            // 1) user event not called
            // 2) DecrementPendingCallbacks not called, which would mean this object would be leaked due
            //    to the outstanding GCRoot until AppDomain.Unload.
            // We live with the above for the time being due to the constraints of the current
            // reliability infrastructure provided by the CLR.

            TaskCompletionSource<object> source = _networkPacketTaskSource;
#if DEBUG
            if ((_forcePendingReadsToWaitForUser) && (_realNetworkPacketTaskSource != null))
            {
                source = _realNetworkPacketTaskSource;
            }
#endif

            // The mars physical connection can get a callback
            // with a packet but no result after the connection is closed.
            if (source == null && _parser._pMarsPhysicalConObj == this)
            {
                return;
            }

            bool processFinallyBlock = true;
            try
            {
                Debug.Assert(CheckPacket(packet, source) && source != null, "AsyncResult null on callback");

                if (_parser.MARSOn)
                { // Only take reset lock on MARS and Async.
                    CheckSetResetConnectionState(error, CallbackType.Read);
                }

                ChangeNetworkPacketTimeout(Timeout.Infinite, Timeout.Infinite);

                ProcessSniPacket(packet, error);
            }
            catch (Exception e)
            {
                processFinallyBlock = ADP.IsCatchableExceptionType(e);
                throw;
            }
            finally
            {
                // pendingCallbacks may be 2 after decrementing, this indicates that a fatal timeout is occurring, and therefore we shouldn't complete the task
                int pendingCallbacks = DecrementPendingCallbacks(false); // may dispose of GC handle.
                if ((processFinallyBlock) && (source != null) && (pendingCallbacks < 2))
                {
                    if (error == 0)
                    {
                        if (_executionContext != null)
                        {
                            ExecutionContext.Run(_executionContext, (state) => source.TrySetResult(null), null);
                        }
                        else
                        {
                            source.TrySetResult(null);
                        }
                    }
                    else
                    {
                        if (_executionContext != null)
                        {
                            ExecutionContext.Run(_executionContext, (state) => ReadAsyncCallbackCaptureException(source), null);
                        }
                        else
                        {
                            ReadAsyncCallbackCaptureException(source);
                        }
                    }
                }

                AssertValidState();
            }
        }

        protected abstract bool CheckPacket(object packet, TaskCompletionSource<object> source);

        private void ReadAsyncCallbackCaptureException(TaskCompletionSource<object> source)
        {
            bool captureSuccess = false;
            try
            {
                if (_hasErrorOrWarning)
                {
                    // Do the close on another thread, since we don't want to block the callback thread
                    ThrowExceptionAndWarning(asyncClose: true);
                }
                else if ((_parser.State == TdsParserState.Closed) || (_parser.State == TdsParserState.Broken))
                {
                    // Connection was closed by another thread before we parsed the packet, so no error was added to the collection
                    throw ADP.ClosedConnectionError();
                }
            }
            catch (Exception ex)
            {
                if (source.TrySetException(ex))
                {
                    // There was an exception, and it was successfully stored in the task
                    captureSuccess = true;
                }
            }

            if (!captureSuccess)
            {
                // Either there was no exception, or the task was already completed
                // This is unusual, but possible if a fatal timeout occurred on another thread (which should mean that the connection is now broken)
                Debug.Assert(_parser.State == TdsParserState.Broken || _parser.State == TdsParserState.Closed || _parser.Connection.IsConnectionDoomed, "Failed to capture exception while the connection was still healthy");

                // The safest thing to do is to ensure that the connection is broken and attempt to cancel the task
                // This must be done from another thread to not block the callback thread                
                Task.Factory.StartNew(() =>
                {
                    _parser.State = TdsParserState.Broken;
                    _parser.Connection.BreakConnection();
                    source.TrySetCanceled();
                });
            }
        }

#pragma warning disable 0420 // a reference to a volatile field will not be treated as volatile

        public void WriteAsyncCallback<T>(T packet, UInt32 sniError)
        {
            WriteAsyncCallback(IntPtr.Zero, packet, sniError);
        }

        public void WriteAsyncCallback<T>(IntPtr key, T packet, UInt32 sniError)
        { // Key never used.
            RemovePacketFromPendingList(packet);
            try
            {
                if (sniError != TdsEnums.SNI_SUCCESS)
                {
                    try
                    {
                        AddError(_parser.ProcessSNIError(this));
                        ThrowExceptionAndWarning(asyncClose: true);
                    }
                    catch (Exception e)
                    {
                        var writeCompletionSource = _writeCompletionSource;
                        if (writeCompletionSource != null)
                        {
                            writeCompletionSource.TrySetException(e);
                        }
                        else
                        {
                            _delayedWriteAsyncCallbackException = e;

                            // Ensure that _delayedWriteAsyncCallbackException is set before checking _writeCompletionSource
                            Interlocked.MemoryBarrier();

                            // Double check that _writeCompletionSource hasn't been created in the meantime
                            writeCompletionSource = _writeCompletionSource;
                            if (writeCompletionSource != null)
                            {
                                var delayedException = Interlocked.Exchange(ref _delayedWriteAsyncCallbackException, null);
                                if (delayedException != null)
                                {
                                    writeCompletionSource.TrySetException(delayedException);
                                }
                            }
                        }

                        return;
                    }
                }
                else
                {
                    _lastSuccessfulIOTimer._value = DateTime.UtcNow.Ticks;
                }
            }
            finally
            {
#if DEBUG
                if (SqlCommand.DebugForceAsyncWriteDelay > 0)
                {
                    new Timer(obj =>
                    {
                        Interlocked.Decrement(ref _asyncWriteCount);
                        var writeCompletionSource = _writeCompletionSource;
                        if (_asyncWriteCount == 0 && writeCompletionSource != null)
                        {
                            writeCompletionSource.TrySetResult(null);
                        }
                    }, null, SqlCommand.DebugForceAsyncWriteDelay, Timeout.Infinite);
                }
                else
                {
#else
                {
#endif
                    Interlocked.Decrement(ref _asyncWriteCount);
                }
            }
#if DEBUG
            if (SqlCommand.DebugForceAsyncWriteDelay > 0)
            {
                return;
            }
#endif
            var completionSource = _writeCompletionSource;
            if (_asyncWriteCount == 0 && completionSource != null)
            {
                completionSource.TrySetResult(null);
            }
        }

#pragma warning restore 0420

        /////////////////////////////////////////
        // Network/Packet Writing & Processing //
        /////////////////////////////////////////



        internal Task WaitForAccumulatedWrites()
        {
            // Checked for stored exceptions
#pragma warning disable 420 // A reference to a volatile field will not be treated as volatile - Disabling since the Interlocked APIs are volatile aware
            var delayedException = Interlocked.Exchange(ref _delayedWriteAsyncCallbackException, null);
            if (delayedException != null)
            {
                throw delayedException;
            }
#pragma warning restore 420

            if (_asyncWriteCount == 0)
            {
                return null;
            }

            _writeCompletionSource = new TaskCompletionSource<object>();
            Task task = _writeCompletionSource.Task;

            // Ensure that _writeCompletionSource is set before checking state
            Interlocked.MemoryBarrier();

            // Now that we have set _writeCompletionSource, check if parser is closed or broken
            if ((_parser.State == TdsParserState.Closed) || (_parser.State == TdsParserState.Broken))
            {
                throw ADP.ClosedConnectionError();
            }

            // Check for stored exceptions
#pragma warning disable 420 // A reference to a volatile field will not be treated as volatile - Disabling since the Interlocked APIs are volatile aware
            delayedException = Interlocked.Exchange(ref _delayedWriteAsyncCallbackException, null);
            if (delayedException != null)
            {
                throw delayedException;
            }
#pragma warning restore 420

            // If there are no outstanding writes, see if we can shortcut and return null
            if ((_asyncWriteCount == 0) && ((!task.IsCompleted) || (task.Exception == null)))
            {
                task = null;
            }

            return task;
        }

        // Takes in a single byte and writes it to the buffer.  If the buffer is full, it is flushed
        // and then the buffer is re-initialized in flush() and then the byte is put in the buffer.
        internal void WriteByte(byte b)
        {
            Debug.Assert(_outBytesUsed <= _outBuff.Length, "ERROR - TDSParser: _outBytesUsed > _outBuff.Length");

            // check to make sure we haven't used the full amount of space available in the buffer, if so, flush it
            if (_outBytesUsed == _outBuff.Length)
            {
                WritePacket(TdsEnums.SOFTFLUSH, canAccumulate: true);
            }
            // set byte in buffer and increment the counter for number of bytes used in the out buffer
            _outBuff[_outBytesUsed++] = b;
        }

        //
        // Takes a byte array and writes it to the buffer.
        //
        internal Task WriteByteArray(Byte[] b, int len, int offsetBuffer, bool canAccumulate = true, TaskCompletionSource<object> completion = null)
        {
            try
            {
                bool async = _parser._asyncWrite;  // NOTE: We are capturing this now for the assert after the Task is returned, since WritePacket will turn off async if there is an exception
                Debug.Assert(async || _asyncWriteCount == 0);
                // Do we have to send out in packet size chunks, or can we rely on netlib layer to break it up?
                // would prefer to do something like:
                //
                // if (len > what we have room for || len > out buf)
                //   flush buffer
                //   UnsafeNativeMethods.Write(b)
                //

                int offset = offsetBuffer;

                Debug.Assert(b.Length >= len, "Invalid length sent to WriteByteArray()!");

                // loop through and write the entire array
                do
                {
                    if ((_outBytesUsed + len) > _outBuff.Length)
                    {
                        // If the remainder of the string won't fit into the buffer, then we have to put
                        // whatever we can into the buffer, and flush that so we can then put more into
                        // the buffer on the next loop of the while.

                        int remainder = _outBuff.Length - _outBytesUsed;

                        // write the remainder
                        Buffer.BlockCopy(b, offset, _outBuff, _outBytesUsed, remainder);

                        // handle counters
                        offset += remainder;
                        _outBytesUsed += remainder;
                        len -= remainder;

                        Task packetTask = WritePacket(TdsEnums.SOFTFLUSH, canAccumulate);

                        if (packetTask != null)
                        {
                            Task task = null;
                            Debug.Assert(async, "Returned task in sync mode");
                            if (completion == null)
                            {
                                completion = new TaskCompletionSource<object>();
                                task = completion.Task; // we only care about return from topmost call, so do not access Task property in other cases
                            }
                            WriteByteArraySetupContinuation(b, len, completion, offset, packetTask);
                            return task;
                        }
                    }
                    else
                    { //((stateObj._outBytesUsed + len) <= stateObj._outBuff.Length )
                        // Else the remainder of the string will fit into the buffer, so copy it into the
                        // buffer and then break out of the loop.

                        Buffer.BlockCopy(b, offset, _outBuff, _outBytesUsed, len);

                        // handle out buffer bytes used counter
                        _outBytesUsed += len;
                        break;
                    }
                } while (len > 0);

                if (completion != null)
                {
                    completion.SetResult(null);
                }
                return null;
            }
            catch (Exception e)
            {
                if (completion != null)
                {
                    completion.SetException(e);
                    return null;
                }
                else
                {
                    throw;
                }
            }
        }

        // This is in its own method to avoid always allocating the lambda in WriteByteArray
        private void WriteByteArraySetupContinuation(Byte[] b, int len, TaskCompletionSource<object> completion, int offset, Task packetTask)
        {
            AsyncHelper.ContinueTask(packetTask, completion,
                () => WriteByteArray(b, len: len, offsetBuffer: offset, canAccumulate: false, completion: completion),
                connectionToDoom: _parser.Connection);
        }

        // Dumps contents of buffer to SNI for network write.
        internal Task WritePacket(byte flushMode, bool canAccumulate = false)
        {
            if ((_parser.State == TdsParserState.Closed) || (_parser.State == TdsParserState.Broken))
            {
                throw ADP.ClosedConnectionError();
            }

            if (
                // This appears to be an optimization to avoid writing empty packets in Yukon
                // However, since we don't know the version prior to login IsYukonOrNewer was always false prior to login
                // So removing the IsYukonOrNewer check causes issues since the login packet happens to meet the rest of the conditions below
                // So we need to avoid this check prior to login completing
                _parser.State == TdsParserState.OpenLoggedIn &&
                !_bulkCopyOpperationInProgress && // ignore the condition checking for bulk copy
                    _outBytesUsed == (_outputHeaderLen + BitConverter.ToInt32(_outBuff, _outputHeaderLen))
                    && _outputPacketNumber == 1
                || _outBytesUsed == _outputHeaderLen
                    && _outputPacketNumber == 1)
            {
                return null;
            }

            byte status;
            byte packetNumber = _outputPacketNumber;

            // Set Status byte based whether this is end of message or not
            bool willCancel = (_cancelled) && (_parser._asyncWrite);
            if (willCancel)
            {
                status = TdsEnums.ST_EOM | TdsEnums.ST_IGNORE;
                _outputPacketNumber = 1;
            }
            else if (TdsEnums.HARDFLUSH == flushMode)
            {
                status = TdsEnums.ST_EOM;
                _outputPacketNumber = 1;              // end of message - reset to 1 - per ramas                
            }
            else if (TdsEnums.SOFTFLUSH == flushMode)
            {
                status = TdsEnums.ST_BATCH;
                _outputPacketNumber++;
            }
            else
            {
                status = TdsEnums.ST_EOM;
                Debug.Assert(false, String.Format((IFormatProvider)null, "Unexpected argument {0,-2:x2} to WritePacket", flushMode));
            }

            _outBuff[0] = _outputMessageType;         // Message Type
            _outBuff[1] = status;
            _outBuff[2] = (byte)(_outBytesUsed >> 8); // length - upper byte
            _outBuff[3] = (byte)(_outBytesUsed & 0xff); // length - lower byte
            _outBuff[4] = 0;                          // channel
            _outBuff[5] = 0;
            _outBuff[6] = packetNumber;               // packet
            _outBuff[7] = 0;                          // window

            Task task = null;
            _parser.CheckResetConnection(this);       // HAS SIDE EFFECTS - re-org at a later time if possible

            task = WriteSni(canAccumulate);
            AssertValidState();

            if (willCancel)
            {
                // If we have been cancelled, then ensure that we write the ATTN packet as well
                task = AsyncHelper.CreateContinuationTask(task, CancelWritePacket, _parser.Connection);
            }

            return task;
        }

        private void CancelWritePacket()
        {
            Debug.Assert(_cancelled, "Should not call CancelWritePacket if _cancelled is not set");

            _parser.Connection.ThreadHasParserLockForClose = true;      // In case of error, let the connection know that we are holding the lock
            try
            {
                // Send the attention and wait for the ATTN_ACK
                SendAttention();
                ResetCancelAndProcessAttention();

                // Let the caller know that we've given up
                throw SQL.OperationCancelled();
            }
            finally
            {
                _parser.Connection.ThreadHasParserLockForClose = false;
            }
        }

#pragma warning disable 0420 // a reference to a volatile field will not be treated as volatile

        private Task SNIWritePacket(object packet, out UInt32 sniError, bool canAccumulate, bool callerHasConnectionLock)
        {
            // Check for a stored exception
            var delayedException = Interlocked.Exchange(ref _delayedWriteAsyncCallbackException, null);
            if (delayedException != null)
            {
                throw delayedException;
            }

            Task task = null;
            _writeCompletionSource = null;

            object packetPointer = EmptyReadPacket;

            bool sync = !_parser._asyncWrite;
            if (sync && _asyncWriteCount > 0)
            { // for example, SendAttention while there are writes pending
                Task waitForWrites = WaitForAccumulatedWrites();
                if (waitForWrites != null)
                {
                    try
                    {
                        waitForWrites.Wait();
                    }
                    catch (AggregateException ae)
                    {
                        throw ae.InnerException;
                    }
                }
                Debug.Assert(_asyncWriteCount == 0, "All async write should be finished");
            }
            if (!sync)
            {
                // Add packet to the pending list (since the callback can happen any time after we call SNIWritePacket)
                packetPointer = AddPacketToPendingList(packet);
            }
            // Async operation completion may be delayed (success pending).
            try
            {
            }
            finally
            {

                sniError = WritePacket(packet, sync);
            }
            if (sniError == TdsEnums.SNI_SUCCESS_IO_PENDING)
            {
                Debug.Assert(!sync, "Completion should be handled in SniManagedWwrapper");
                Interlocked.Increment(ref _asyncWriteCount);
                Debug.Assert(_asyncWriteCount >= 0);
                if (!canAccumulate)
                {
                    // Create completion source (for callback to complete)
                    _writeCompletionSource = new TaskCompletionSource<object>();
                    task = _writeCompletionSource.Task;

                    // Ensure that setting _writeCompletionSource completes before checking _delayedWriteAsyncCallbackException
                    Interlocked.MemoryBarrier();

                    // Check for a stored exception
                    delayedException = Interlocked.Exchange(ref _delayedWriteAsyncCallbackException, null);
                    if (delayedException != null)
                    {
                        throw delayedException;
                    }

                    // If there are no outstanding writes, see if we can shortcut and return null
                    if ((_asyncWriteCount == 0) && ((!task.IsCompleted) || (task.Exception == null)))
                    {
                        task = null;
                    }
                }
            }
#if DEBUG
            else if (!sync && !canAccumulate && SqlCommand.DebugForceAsyncWriteDelay > 0)
            {
                // Executed synchronously - callback will not be called 
                TaskCompletionSource<object> completion = new TaskCompletionSource<object>();
                uint error = sniError;
                new Timer(obj =>
                {
                    try
                    {
                        if (_parser.MARSOn)
                        { // Only take reset lock on MARS.
                            CheckSetResetConnectionState(error, CallbackType.Write);
                        }

                        if (error != TdsEnums.SNI_SUCCESS)
                        {
                            AddError(_parser.ProcessSNIError(this));
                            ThrowExceptionAndWarning();
                        }
                        AssertValidState();
                        completion.SetResult(null);
                    }
                    catch (Exception e)
                    {
                        completion.SetException(e);
                    }
                }, null, SqlCommand.DebugForceAsyncWriteDelay, Timeout.Infinite);
                task = completion.Task;
            }
#endif
            else
            {
                if (_parser.MARSOn)
                { // Only take reset lock on MARS.
                    CheckSetResetConnectionState(sniError, CallbackType.Write);
                }

                if (sniError == TdsEnums.SNI_SUCCESS)
                {
                    _lastSuccessfulIOTimer._value = DateTime.UtcNow.Ticks;

                    if (!sync)
                    {
                        // Since there will be no callback, remove the packet from the pending list
                        Debug.Assert(IsValidPacket(packetPointer), "Packet added to list has an invalid pointer, can not remove from pending list");
                        RemovePacketFromPendingList(packetPointer);
                    }
                }
                else
                {
                    AddError(_parser.ProcessSNIError(this));
                    ThrowExceptionAndWarning(callerHasConnectionLock);
                }
                AssertValidState();
            }
            return task;
        }

        internal abstract bool IsValidPacket(object packetPointer);
        internal abstract uint WritePacket(object packet, bool sync);

#pragma warning restore 0420

        // Sends an attention signal - executing thread will consume attn.
        internal void SendAttention(bool mustTakeWriteLock = false)
        {
            if (!_attentionSent)
            {
                // Dumps contents of buffer to OOB write (currently only used for
                // attentions.  There is no body for this message
                // Doesn't touch this._outBytesUsed
                if (_parser.State == TdsParserState.Closed || _parser.State == TdsParserState.Broken)
                {
                    return;
                }

                object attnPacket = CreateAndSetAttentionPacket();

                try
                {
                    // Dev11 #344723: SqlClient stress hang System_Data!Tcp::ReadSync via a call to SqlDataReader::Close
                    // Set _attentionSending to true before sending attention and reset after setting _attentionSent
                    // This prevents a race condition between receiving the attention ACK and setting _attentionSent
                    _attentionSending = true;

#if DEBUG
                    if (!_skipSendAttention)
                    {
#endif
                        // Take lock and send attention
                        bool releaseLock = false;
                        if ((mustTakeWriteLock) && (!_parser.Connection.ThreadHasParserLockForClose))
                        {
                            releaseLock = true;
                            _parser.Connection._parserLock.Wait(canReleaseFromAnyThread: false);
                            _parser.Connection.ThreadHasParserLockForClose = true;
                        }
                        try
                        {
                            // Check again (just in case the connection was closed while we were waiting)
                            if (_parser.State == TdsParserState.Closed || _parser.State == TdsParserState.Broken)
                            {
                                return;
                            }

                            UInt32 sniError;
                            _parser._asyncWrite = false; // stop async write 
                            SNIWritePacket(attnPacket, out sniError, canAccumulate: false, callerHasConnectionLock: false);
                        }
                        finally
                        {
                            if (releaseLock)
                            {
                                _parser.Connection.ThreadHasParserLockForClose = false;
                                _parser.Connection._parserLock.Release();
                            }
                        }
#if DEBUG
                    }
#endif

                    SetTimeoutSeconds(AttentionTimeoutSeconds); // Initialize new attention timeout of 5 seconds.
                    _attentionSent = true;
                }
                finally
                {
                    _attentionSending = false;
                }


                AssertValidState();
            }
        }

        internal abstract object CreateAndSetAttentionPacket();

        internal abstract void SetPacketData(object packet, byte[] buffer, int bytesUsed);

        private Task WriteSni(bool canAccumulate)
        {
            // Prepare packet, and write to packet.
            object packet = GetResetWritePacket();

            SetPacketData(packet, _outBuff, _outBytesUsed);

            uint sniError;
            Debug.Assert(Parser.Connection._parserLock.ThreadMayHaveLock(), "Thread is writing without taking the connection lock");
            Task task = SNIWritePacket(packet, out sniError, canAccumulate, callerHasConnectionLock: true);

            // Check to see if the timeout has occurred.  This time out code is special case code to allow BCP writes to timeout. Eventually we should make all writes timeout.
            if (_bulkCopyOpperationInProgress && 0 == GetTimeoutRemaining())
            {
                _parser.Connection.ThreadHasParserLockForClose = true;
                try
                {
                    Debug.Assert(_parser.Connection != null, "SqlConnectionInternalTds handler can not be null at this point.");
                    AddError(new SqlError(TdsEnums.TIMEOUT_EXPIRED, (byte)0x00, TdsEnums.MIN_ERROR_CLASS, _parser.Server, _parser.Connection.TimeoutErrorInternal.GetErrorMessage(), "", 0, TdsEnums.SNI_WAIT_TIMEOUT));
                    _bulkCopyWriteTimeout = true;
                    SendAttention();
                    _parser.ProcessPendingAck(this);
                    ThrowExceptionAndWarning();
                }
                finally
                {
                    _parser.Connection.ThreadHasParserLockForClose = false;
                }
            }

            // Special case logic for encryption removal.
            if (_parser.State == TdsParserState.OpenNotLoggedIn &&
                _parser.EncryptionOptions == EncryptionOptions.LOGIN)
            {
                // If no error occurred, and we are Open but not logged in, and
                // our encryptionOption state is login, remove the SSL Provider.
                // We only need encrypt the very first packet of the login message to the server.

                // We wanted to encrypt entire login channel, but there is
                // currently no mechanism to communicate this.  Removing encryption post 1st packet
                // is a hard-coded agreement between client and server.  We need some mechanism or
                // common change to be able to make this change in a non-breaking fashion.
                _parser.RemoveEncryption();                        // Remove the SSL Provider.
                _parser.EncryptionOptions = EncryptionOptions.OFF; // Turn encryption off.

                // Since this packet was associated with encryption, dispose and re-create.
                ClearAllWritePackets();
            }

            SniWriteStatisticsAndTracing();

            ResetBuffer();

            AssertValidState();
            return task;
        }

        //////////////////////////////////////////////
        // Statistics, Tracing, and related methods //
        //////////////////////////////////////////////

        private void SniReadStatisticsAndTracing()
        {
            SqlStatistics statistics = Parser.Statistics;
            if (null != statistics)
            {
                if (statistics.WaitForReply)
                {
                    statistics.SafeIncrement(ref statistics._serverRoundtrips);
                    statistics.ReleaseAndUpdateNetworkServerTimer();
                }

                statistics.SafeAdd(ref statistics._bytesReceived, _inBytesRead);
                statistics.SafeIncrement(ref statistics._buffersReceived);
            }
        }

        private void SniWriteStatisticsAndTracing()
        {
            SqlStatistics statistics = _parser.Statistics;
            if (null != statistics)
            {
                statistics.SafeIncrement(ref statistics._buffersSent);
                statistics.SafeAdd(ref statistics._bytesSent, _outBytesUsed);
                statistics.RequestNetworkServerTimer();
            }
        }
        [Conditional("DEBUG")]

        private void AssertValidState()
        {
            string assertMessage = null;

            if (_inBytesUsed < 0 || _inBytesRead < 0)
            {
                assertMessage = string.Format(
                    CultureInfo.InvariantCulture,
                    "either _inBytesUsed or _inBytesRead is negative: {0}, {1}",
                    _inBytesUsed, _inBytesRead);
            }
            else if (_inBytesUsed > _inBytesRead)
            {
                assertMessage = string.Format(
                    CultureInfo.InvariantCulture,
                    "_inBytesUsed > _inBytesRead: {0} > {1}",
                    _inBytesUsed, _inBytesRead);
            }

            if (assertMessage != null)
            {
                Debug.Assert(false, "Invalid TDS Parser State: " + assertMessage);
            }

            Debug.Assert(_inBytesPacket >= 0, "Packet must not be negative");
        }


        //////////////////////////////////////////////
        // Errors and Warnings                      //
        //////////////////////////////////////////////

        /// <summary>
        /// True if there is at least one error or warning (not counting the pre-attention errors\warnings)
        /// </summary>
        internal bool HasErrorOrWarning
        {
            get
            {
                return _hasErrorOrWarning;
            }
        }

        /// <summary>
        /// Adds an error to the error collection
        /// </summary>
        /// <param name="error"></param>
        internal void AddError(SqlError error)
        {
            Debug.Assert(error != null, "Trying to add a null error");

            // Switch to sync once we see an error
            _syncOverAsync = true;

            lock (_errorAndWarningsLock)
            {
                _hasErrorOrWarning = true;
                if (_errors == null)
                {
                    _errors = new SqlErrorCollection();
                }
                _errors.Add(error);
            }
        }

        /// <summary>
        /// Gets the number of errors currently in the error collection
        /// </summary>
        internal int ErrorCount
        {
            get
            {
                int count = 0;
                lock (_errorAndWarningsLock)
                {
                    if (_errors != null)
                    {
                        count = _errors.Count;
                    }
                }
                return count;
            }
        }

        /// <summary>
        /// Adds an warning to the warning collection
        /// </summary>
        /// <param name="error"></param>
        internal void AddWarning(SqlError error)
        {
            Debug.Assert(error != null, "Trying to add a null error");

            // Switch to sync once we see a warning
            _syncOverAsync = true;

            lock (_errorAndWarningsLock)
            {
                _hasErrorOrWarning = true;
                if (_warnings == null)
                {
                    _warnings = new SqlErrorCollection();
                }
                _warnings.Add(error);
            }
        }

        /// <summary>
        /// Gets the number of warnings currently in the warning collection
        /// </summary>
        internal int WarningCount
        {
            get
            {
                int count = 0;
                lock (_errorAndWarningsLock)
                {
                    if (_warnings != null)
                    {
                        count = _warnings.Count;
                    }
                }
                return count;
            }
        }

        protected abstract object EmptyReadPacket { get; }

        /// <summary>
        /// Gets the full list of errors and warnings (including the pre-attention ones), then wipes all error and warning lists
        /// </summary>
        /// <param name="broken">If true, the connection should be broken</param>
        /// <returns>An array containing all of the errors and warnings</returns>
        internal SqlErrorCollection GetFullErrorAndWarningCollection(out bool broken)
        {
            SqlErrorCollection allErrors = new SqlErrorCollection();
            broken = false;

            lock (_errorAndWarningsLock)
            {
                _hasErrorOrWarning = false;

                // Merge all error lists, then reset them
                AddErrorsToCollection(_errors, ref allErrors, ref broken);
                AddErrorsToCollection(_warnings, ref allErrors, ref broken);
                _errors = null;
                _warnings = null;

                // We also process the pre-attention error lists here since, if we are here and they are populated, then an error occurred while sending attention so we should show the errors now (otherwise they'd be lost)
                AddErrorsToCollection(_preAttentionErrors, ref allErrors, ref broken);
                AddErrorsToCollection(_preAttentionWarnings, ref allErrors, ref broken);
                _preAttentionErrors = null;
                _preAttentionWarnings = null;
            }

            return allErrors;
        }

        private void AddErrorsToCollection(SqlErrorCollection inCollection, ref SqlErrorCollection collectionToAddTo, ref bool broken)
        {
            if (inCollection != null)
            {
                foreach (SqlError error in inCollection)
                {
                    collectionToAddTo.Add(error);
                    broken |= (error.Class >= TdsEnums.FATAL_ERROR_CLASS);
                }
            }
        }

        /// <summary>
        /// Stores away current errors and warnings so that an attention can be processed
        /// </summary>
        internal void StoreErrorAndWarningForAttention()
        {
            lock (_errorAndWarningsLock)
            {
                Debug.Assert(_preAttentionErrors == null && _preAttentionWarnings == null, "Can't store errors for attention because there are already errors stored");

                _hasErrorOrWarning = false;

                _preAttentionErrors = _errors;
                _preAttentionWarnings = _warnings;

                _errors = null;
                _warnings = null;
            }
        }

        /// <summary>
        /// Restores errors and warnings that were stored in order to process an attention
        /// </summary>
        internal void RestoreErrorAndWarningAfterAttention()
        {
            lock (_errorAndWarningsLock)
            {
                Debug.Assert(_errors == null && _warnings == null, "Can't restore errors after attention because there are already other errors");

                _hasErrorOrWarning = (((_preAttentionErrors != null) && (_preAttentionErrors.Count > 0)) || ((_preAttentionWarnings != null) && (_preAttentionWarnings.Count > 0)));

                _errors = _preAttentionErrors;
                _warnings = _preAttentionWarnings;

                _preAttentionErrors = null;
                _preAttentionWarnings = null;
            }
        }

        /// <summary>
        /// Checks if an error is stored in _error and, if so, throws an error
        /// </summary>
        internal void CheckThrowSNIException()
        {
            if (HasErrorOrWarning)
            {
                ThrowExceptionAndWarning();
            }
        }

        /// <summary>
        /// Debug Only: Ensures that the TdsParserStateObject has no lingering state and can safely be re-used
        /// </summary>
        [Conditional("DEBUG")]
        internal void AssertStateIsClean()
        {
            // If our TdsParser is closed or broken, then we don't really care about our state
            var parser = _parser;
            if ((parser != null) && (parser.State != TdsParserState.Closed) && (parser.State != TdsParserState.Broken))
            {
                // Async reads
                Debug.Assert(_snapshot == null && !_snapshotReplay, "StateObj has leftover snapshot state");
                Debug.Assert(!_asyncReadWithoutSnapshot, "StateObj has AsyncReadWithoutSnapshot still enabled");
                Debug.Assert(_executionContext == null, "StateObj has a stored execution context from an async read");
                // Async writes
                Debug.Assert(_asyncWriteCount == 0, "StateObj still has outstanding async writes");
                Debug.Assert(_delayedWriteAsyncCallbackException == null, "StateObj has an unobserved exceptions from an async write");
                // Attention\Cancellation\Timeouts
                Debug.Assert(!_attentionReceived && !_attentionSent && !_attentionSending, string.Format("StateObj is still dealing with attention: Sent: {0}, Received: {1}, Sending: {2}", _attentionSent, _attentionReceived, _attentionSending));
                Debug.Assert(!_cancelled, "StateObj still has cancellation set");
                Debug.Assert(!_internalTimeout, "StateObj still has internal timeout set");
                // Errors and Warnings
                Debug.Assert(!_hasErrorOrWarning, "StateObj still has stored errors or warnings");
            }
        }

#if DEBUG
        internal void CompletePendingReadWithSuccess(bool resetForcePendingReadsToWait)
        {
            var realNetworkPacketTaskSource = _realNetworkPacketTaskSource;
            var networkPacketTaskSource = _networkPacketTaskSource;

            Debug.Assert(_forcePendingReadsToWaitForUser, "Not forcing pends to wait for user - can't force complete");
            Debug.Assert(networkPacketTaskSource != null, "No pending read to complete");

            try
            {
                if (realNetworkPacketTaskSource != null)
                {
                    // Wait for the real read to complete
                    realNetworkPacketTaskSource.Task.Wait();
                }
            }
            finally
            {
                if (networkPacketTaskSource != null)
                {
                    if (resetForcePendingReadsToWait)
                    {
                        _forcePendingReadsToWaitForUser = false;
                    }

                    networkPacketTaskSource.TrySetResult(null);
                }
            }
        }

        internal void CompletePendingReadWithFailure(int errorCode, bool resetForcePendingReadsToWait)
        {
            var realNetworkPacketTaskSource = _realNetworkPacketTaskSource;
            var networkPacketTaskSource = _networkPacketTaskSource;

            Debug.Assert(_forcePendingReadsToWaitForUser, "Not forcing pends to wait for user - can't force complete");
            Debug.Assert(networkPacketTaskSource != null, "No pending read to complete");

            try
            {
                if (realNetworkPacketTaskSource != null)
                {
                    // Wait for the real read to complete
                    realNetworkPacketTaskSource.Task.Wait();
                }
            }
            finally
            {
                if (networkPacketTaskSource != null)
                {
                    if (resetForcePendingReadsToWait)
                    {
                        _forcePendingReadsToWaitForUser = false;
                    }

                    AddError(new SqlError(errorCode, 0x00, TdsEnums.FATAL_ERROR_CLASS, _parser.Server, string.Empty, string.Empty, 0));
                    try
                    {
                        ThrowExceptionAndWarning();
                    }
                    catch (Exception ex)
                    {
                        networkPacketTaskSource.TrySetException(ex);
                    }
                }
            }
        }
#endif

        internal void CloneCleanupAltMetaDataSetArray()
        {
            if (_snapshot != null)
            {
                _snapshot.CloneCleanupAltMetaDataSetArray();
            }
        }

        private class PacketData
        {
            public byte[] Buffer;
            public int Read;
#if DEBUG
            public string Stack;
#endif
        }

        private class StateSnapshot
        {
            private List<PacketData> _snapshotInBuffs;
            private int _snapshotInBuffCurrent = 0;
            private int _snapshotInBytesUsed = 0;
            private int _snapshotInBytesPacket = 0;
            private bool _snapshotPendingData = false;
            private bool _snapshotErrorTokenReceived = false;
            private bool _snapshotHasOpenResult = false;
            private bool _snapshotReceivedColumnMetadata = false;
            private bool _snapshotAttentionReceived;
            private byte _snapshotMessageStatus;

            private NullBitmap _snapshotNullBitmapInfo;
            private ulong _snapshotLongLen;
            private ulong _snapshotLongLenLeft;
            private _SqlMetaDataSet _snapshotCleanupMetaData;
            private _SqlMetaDataSetCollection _snapshotCleanupAltMetaDataSetArray;

            private readonly TdsParserStateObject _stateObj;

            public StateSnapshot(TdsParserStateObject state)
            {
                _snapshotInBuffs = new List<PacketData>();
                _stateObj = state;
            }

#if DEBUG
            private int _rollingPend = 0;
            private int _rollingPendCount = 0;

            internal bool DoPend()
            {
                if (_failAsyncPends || !_forceAllPends)
                {
                    return false;
                }

                if (_rollingPendCount == _rollingPend)
                {
                    _rollingPend++;
                    _rollingPendCount = 0;
                    return true;
                }

                _rollingPendCount++;
                return false;
            }
#endif

            internal void CloneNullBitmapInfo()
            {
                if (_stateObj._nullBitmapInfo.ReferenceEquals(_snapshotNullBitmapInfo))
                {
                    _stateObj._nullBitmapInfo = _stateObj._nullBitmapInfo.Clone();
                }
            }

            internal void CloneCleanupAltMetaDataSetArray()
            {
                if (_stateObj._cleanupAltMetaDataSetArray != null && object.ReferenceEquals(_snapshotCleanupAltMetaDataSetArray, _stateObj._cleanupAltMetaDataSetArray))
                {
                    _stateObj._cleanupAltMetaDataSetArray = (_SqlMetaDataSetCollection)_stateObj._cleanupAltMetaDataSetArray.Clone();
                }
            }

            internal void PushBuffer(byte[] buffer, int read)
            {
                Debug.Assert(!_snapshotInBuffs.Any(b => object.ReferenceEquals(b.Buffer, buffer)));

                PacketData packetData = new PacketData();
                packetData.Buffer = buffer;
                packetData.Read = read;
#if DEBUG
                packetData.Stack = _stateObj._lastStack;
#endif

                _snapshotInBuffs.Add(packetData);
            }

#if DEBUG
            internal void AssertCurrent()
            {
                Debug.Assert(_snapshotInBuffCurrent == _snapshotInBuffs.Count, "Should not be reading new packets when not replaying last packet");
            }
            internal void CheckStack(string trace)
            {
                PacketData prev = _snapshotInBuffs[_snapshotInBuffCurrent - 1];
                if (prev.Stack == null)
                {
                    prev.Stack = trace;
                }
                else
                {
                    Debug.Assert(_stateObj._permitReplayStackTraceToDiffer || prev.Stack.ToString() == trace.ToString(), "The stack trace on subsequent replays should be the same");
                }
            }
#endif

            internal bool Replay()
            {
                if (_snapshotInBuffCurrent < _snapshotInBuffs.Count)
                {
                    PacketData next = _snapshotInBuffs[_snapshotInBuffCurrent];
                    _stateObj._inBuff = next.Buffer;
                    _stateObj._inBytesUsed = 0;
                    _stateObj._inBytesRead = next.Read;
                    _snapshotInBuffCurrent++;
                    return true;
                }

                return false;
            }

            internal void Snap()
            {
                _snapshotInBuffs.Clear();
                _snapshotInBuffCurrent = 0;
                _snapshotInBytesUsed = _stateObj._inBytesUsed;
                _snapshotInBytesPacket = _stateObj._inBytesPacket;
                _snapshotPendingData = _stateObj._pendingData;
                _snapshotErrorTokenReceived = _stateObj._errorTokenReceived;
                _snapshotMessageStatus = _stateObj._messageStatus;
                // _nullBitmapInfo must be cloned before it is updated
                _snapshotNullBitmapInfo = _stateObj._nullBitmapInfo;
                _snapshotLongLen = _stateObj._longlen;
                _snapshotLongLenLeft = _stateObj._longlenleft;
                _snapshotCleanupMetaData = _stateObj._cleanupMetaData;
                // _cleanupAltMetaDataSetArray must be cloned before it is updated
                _snapshotCleanupAltMetaDataSetArray = _stateObj._cleanupAltMetaDataSetArray;
                _snapshotHasOpenResult = _stateObj._hasOpenResult;
                _snapshotReceivedColumnMetadata = _stateObj._receivedColMetaData;
                _snapshotAttentionReceived = _stateObj._attentionReceived;
#if DEBUG
                _rollingPend = 0;
                _rollingPendCount = 0;
                _stateObj._lastStack = null;
                Debug.Assert(_stateObj._bTmpRead == 0, "Has partially read data when snapshot taken");
                Debug.Assert(_stateObj._partialHeaderBytesRead == 0, "Has partially read header when snapshot taken");
#endif

                PushBuffer(_stateObj._inBuff, _stateObj._inBytesRead);
            }

            internal void ResetSnapshotState()
            {
                // go back to the beginning
                _snapshotInBuffCurrent = 0;

                Replay();

                _stateObj._inBytesUsed = _snapshotInBytesUsed;
                _stateObj._inBytesPacket = _snapshotInBytesPacket;
                _stateObj._pendingData = _snapshotPendingData;
                _stateObj._errorTokenReceived = _snapshotErrorTokenReceived;
                _stateObj._messageStatus = _snapshotMessageStatus;
                _stateObj._nullBitmapInfo = _snapshotNullBitmapInfo;
                _stateObj._cleanupMetaData = _snapshotCleanupMetaData;
                _stateObj._cleanupAltMetaDataSetArray = _snapshotCleanupAltMetaDataSetArray;
                _stateObj._hasOpenResult = _snapshotHasOpenResult;
                _stateObj._receivedColMetaData = _snapshotReceivedColumnMetadata;
                _stateObj._attentionReceived = _snapshotAttentionReceived;

                // Reset partially read state (these only need to be maintained if doing async without snapshot)
                _stateObj._bTmpRead = 0;
                _stateObj._partialHeaderBytesRead = 0;

                // reset plp state
                _stateObj._longlen = _snapshotLongLen;
                _stateObj._longlenleft = _snapshotLongLenLeft;

                _stateObj._snapshotReplay = true;

                _stateObj.AssertValidState();
            }

            internal void PrepareReplay()
            {
                ResetSnapshotState();
            }
        }

        /*

        // leave this in. comes handy if you have to do Console.WriteLine style debugging ;)
                private void DumpBuffer() {
                    Console.WriteLine("dumping buffer");
                    Console.WriteLine("_inBytesRead = {0}", _inBytesRead);
                    Console.WriteLine("_inBytesUsed = {0}", _inBytesUsed);
                    int cc = 0; // character counter
                    int i;
                    Console.WriteLine("used buffer:");
                    for (i=0; i< _inBytesUsed; i++) {
                        if (cc==16) {
                            Console.WriteLine();
                            cc = 0;
                        }
                        Console.Write("{0,-2:X2} ", _inBuff[i]);
                        cc++;
                    }
                    if (cc>0) {
                        Console.WriteLine();
                    }

                    cc = 0;
                    Console.WriteLine("unused buffer:");
                    for (i=_inBytesUsed; i<_inBytesRead; i++) {
                        if (cc==16) {
                            Console.WriteLine();
                            cc = 0;
                        }
                        Console.Write("{0,-2:X2} ", _inBuff[i]);
                        cc++;
                    }
                    if (cc>0) {
                        Console.WriteLine();
                    }
                }
        */
    }
}
