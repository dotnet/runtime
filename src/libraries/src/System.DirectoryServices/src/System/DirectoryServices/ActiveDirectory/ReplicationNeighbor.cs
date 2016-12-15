// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.DirectoryServices.ActiveDirectory
{
    using System;
    using System.Runtime.InteropServices;
    using System.Collections;
    using System.Globalization;

    public enum ActiveDirectoryTransportType
    {
        Rpc = 0,
        Smtp = 1
    }

    public class ReplicationNeighbor
    {
        [Flags]
        public enum ReplicationNeighborOptions : long
        {
            Writeable = 0x10,
            SyncOnStartup = 0x20,
            ScheduledSync = 0x40,
            UseInterSiteTransport = 0x80,
            TwoWaySync = 0x200,
            ReturnObjectParent = 0x800,
            FullSyncInProgress = 0x10000,
            FullSyncNextPacket = 0x20000,
            NeverSynced = 0x200000,
            Preempted = 0x01000000,
            IgnoreChangeNotifications = 0x04000000,
            DisableScheduledSync = 0x08000000,
            CompressChanges = 0x10000000,
            NoChangeNotifications = 0x20000000,
            PartialAttributeSet = 0x40000000
        }

        private string _namingContext;
        private string _sourceServerDN;
        private ActiveDirectoryTransportType _transportType;
        private ReplicationNeighborOptions _replicaFlags;
        private Guid _uuidSourceDsaInvocationID;
        private long _usnLastObjChangeSynced;
        private long _usnAttributeFilter;
        private DateTime _timeLastSyncSuccess;
        private DateTime _timeLastSyncAttempt;
        private int _lastSyncResult;
        private int _consecutiveSyncFailures;

        private DirectoryServer _server = null;
        private string _sourceServer = null;
        private Hashtable _nameTable = null;

        internal ReplicationNeighbor(IntPtr addr, DirectoryServer server, Hashtable table)
        {
            DS_REPL_NEIGHBOR neighbor = new DS_REPL_NEIGHBOR();
            Marshal.PtrToStructure(addr, neighbor);

            _namingContext = Marshal.PtrToStringUni(neighbor.pszNamingContext);
            _sourceServerDN = Marshal.PtrToStringUni(neighbor.pszSourceDsaDN);

            string transportDN = Marshal.PtrToStringUni(neighbor.pszAsyncIntersiteTransportDN);
            if (transportDN != null)
            {
                string rdn = Utils.GetRdnFromDN(transportDN);
                string transport = (Utils.GetDNComponents(rdn))[0].Value;

                if (String.Compare(transport, "SMTP", StringComparison.OrdinalIgnoreCase) == 0)
                    _transportType = ActiveDirectoryTransportType.Smtp;
                else
                    _transportType = ActiveDirectoryTransportType.Rpc;
            }

            _replicaFlags = (ReplicationNeighborOptions)neighbor.dwReplicaFlags;
            _uuidSourceDsaInvocationID = neighbor.uuidSourceDsaInvocationID;
            _usnLastObjChangeSynced = neighbor.usnLastObjChangeSynced;
            _usnAttributeFilter = neighbor.usnAttributeFilter;
            _timeLastSyncSuccess = DateTime.FromFileTime(neighbor.ftimeLastSyncSuccess);
            _timeLastSyncAttempt = DateTime.FromFileTime(neighbor.ftimeLastSyncAttempt);
            _lastSyncResult = neighbor.dwLastSyncResult;
            _consecutiveSyncFailures = neighbor.cNumConsecutiveSyncFailures;

            _server = server;
            _nameTable = table;
        }

        public string PartitionName
        {
            get
            {
                return _namingContext;
            }
        }

        public string SourceServer
        {
            get
            {
                if (_sourceServer == null)
                {
                    // check whether we have got it before
                    if (_nameTable.Contains(SourceInvocationId))
                    {
                        _sourceServer = (string)_nameTable[SourceInvocationId];
                    }
                    else if (_sourceServerDN != null)
                    {
                        _sourceServer = Utils.GetServerNameFromInvocationID(_sourceServerDN, SourceInvocationId, _server);
                        // add it to the hashtable
                        _nameTable.Add(SourceInvocationId, _sourceServer);
                    }
                }

                return _sourceServer;
            }
        }

        public ActiveDirectoryTransportType TransportType
        {
            get
            {
                return _transportType;
            }
        }

        public ReplicationNeighborOptions ReplicationNeighborOption
        {
            get
            {
                return _replicaFlags;
            }
        }

        public Guid SourceInvocationId
        {
            get
            {
                return _uuidSourceDsaInvocationID;
            }
        }

        public long UsnLastObjectChangeSynced
        {
            get
            {
                return _usnLastObjChangeSynced;
            }
        }

        public long UsnAttributeFilter
        {
            get
            {
                return _usnAttributeFilter;
            }
        }

        public DateTime LastSuccessfulSync
        {
            get
            {
                return _timeLastSyncSuccess;
            }
        }

        public DateTime LastAttemptedSync
        {
            get
            {
                return _timeLastSyncAttempt;
            }
        }

        public int LastSyncResult
        {
            get
            {
                return _lastSyncResult;
            }
        }

        public string LastSyncMessage
        {
            get
            {
                return ExceptionHelper.GetErrorMessage(_lastSyncResult, false);
            }
        }

        public int ConsecutiveFailureCount
        {
            get
            {
                return _consecutiveSyncFailures;
            }
        }
    }
}
