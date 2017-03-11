// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;

namespace System.Data.SqlClient
{
    internal class TdsParserSessionPool
    {
        // NOTE: This is a very simplistic, lightweight pooler.  It wasn't
        //       intended to handle huge number of items, just to keep track
        //       of the session objects to ensure that they're cleaned up in
        //       a timely manner, to avoid holding on to an unacceptable 
        //       amount of server-side resources in the event that consumers
        //       let their data readers be GC'd, instead of explicitly 
        //       closing or disposing of them

        private const int MaxInactiveCount = 10; // pick something, preferably small...


        private readonly TdsParser _parser;       // parser that owns us
        private readonly List<TdsParserStateObject> _cache;        // collection of all known sessions 
        private int _cachedCount;  // lock-free _cache.Count
        private TdsParserStateObject[] _freeStateObjects; // collection of all sessions available for reuse
        private int _freeStateObjectCount; // Number of available free sessions

        internal TdsParserSessionPool(TdsParser parser)
        {
            _parser = parser;
            _cache = new List<TdsParserStateObject>();
            _freeStateObjects = new TdsParserStateObject[MaxInactiveCount];
            _freeStateObjectCount = 0;
        }

        private bool IsDisposed
        {
            get
            {
                return (null == _freeStateObjects);
            }
        }


        internal void Deactivate()
        {
            // When being deactivated, we check all the sessions in the
            // cache to make sure they're cleaned up and then we dispose of
            // sessions that are past what we want to keep around.

            lock (_cache)
            {
                // NOTE: The PutSession call below may choose to remove the 
                //       session from the cache, which will throw off our 
                //       enumerator.  We avoid that by simply indexing backward
                //       through the array.

                for (int i = _cache.Count - 1; i >= 0; i--)
                {
                    TdsParserStateObject session = _cache[i];

                    if (null != session)
                    {
                        if (session.IsOrphaned)
                        {
                            // TODO: consider adding a performance counter for the number of sessions we reclaim


                            PutSession(session);
                        }
                    }
                }
                // TODO: re-enable this assert when the connection isn't doomed.
                //Debug.Assert (_cachedCount < MaxInactiveCount, "non-orphaned connection past initial allocation?");
            }
        }

        internal void Dispose()
        {
            lock (_cache)
            {
                // Dispose free sessions
                for (int i = 0; i < _freeStateObjectCount; i++)
                {
                    if (_freeStateObjects[i] != null)
                    {
                        _freeStateObjects[i].Dispose();
                    }
                }
                _freeStateObjects = null;
                _freeStateObjectCount = 0;

                // Dispose orphaned sessions
                for (int i = 0; i < _cache.Count; i++)
                {
                    if (_cache[i] != null)
                    {
                        if (_cache[i].IsOrphaned)
                        {
                            _cache[i].Dispose();
                        }
                        else
                        {
                            // Remove the "initial" callback (this will allow the stateObj to be GC collected if need be)
                            _cache[i].DecrementPendingCallbacks(false);
                        }
                    }
                }
                _cache.Clear();
                _cachedCount = 0;
                // Any active sessions will take care of themselves
                // (It's too dangerous to dispose them, as this can cause AVs)
            }
        }

        internal TdsParserStateObject GetSession(object owner)
        {
            TdsParserStateObject session;
            lock (_cache)
            {
                if (IsDisposed)
                {
                    throw ADP.ClosedConnectionError();
                }
                else if (_freeStateObjectCount > 0)
                {
                    // Free state object - grab it
                    _freeStateObjectCount--;
                    session = _freeStateObjects[_freeStateObjectCount];
                    _freeStateObjects[_freeStateObjectCount] = null;
                    Debug.Assert(session != null, "There was a null session in the free session list?");
                }
                else
                {
                    // No free objects, create a new one
                    session = _parser.CreateSession();


                    _cache.Add(session);
                    _cachedCount = _cache.Count;
                }

                session.Activate(owner);
            }


            return session;
        }

        internal void PutSession(TdsParserStateObject session)
        {
            Debug.Assert(null != session, "null session?");
            //Debug.Assert(null != session.Owner, "session without owner?");

            bool okToReuse = session.Deactivate();

            lock (_cache)
            {
                if (IsDisposed)
                {
                    // We're diposed - just clean out the session
                    Debug.Assert(_cachedCount == 0, "SessionPool is disposed, but there are still sessions in the cache?");
                    session.Dispose();
                }
                else if ((okToReuse) && (_freeStateObjectCount < MaxInactiveCount))
                {
                    // Session is good to re-use and our cache has space
                    Debug.Assert(!session._pendingData, "pending data on a pooled session?");

                    _freeStateObjects[_freeStateObjectCount] = session;
                    _freeStateObjectCount++;
                }
                else
                {
                    // Either the session is bad, or we have no cache space - so dispose the session and remove it

                    bool removed = _cache.Remove(session);
                    Debug.Assert(removed, "session not in pool?");
                    _cachedCount = _cache.Count;
                    session.Dispose();
                }

                session.RemoveOwner();
            }
        }


        internal int ActiveSessionsCount
        {
            get
            {
                return _cachedCount - _freeStateObjectCount;
            }
        }
    }
}


