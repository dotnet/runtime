// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Collections;

namespace System.DirectoryServices.ActiveDirectory
{
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

        private readonly string? _sourceServerDN;

        private readonly DirectoryServer _server;
        private string? _sourceServer;
        private readonly Hashtable _nameTable;

        internal ReplicationNeighbor(IntPtr addr, DirectoryServer server, Hashtable table)
        {
            DS_REPL_NEIGHBOR neighbor = new DS_REPL_NEIGHBOR();
            Marshal.PtrToStructure(addr, neighbor);

            PartitionName = Marshal.PtrToStringUni(neighbor.pszNamingContext);
            _sourceServerDN = Marshal.PtrToStringUni(neighbor.pszSourceDsaDN);

            string? transportDN = Marshal.PtrToStringUni(neighbor.pszAsyncIntersiteTransportDN);
            if (transportDN != null)
            {
                string rdn = Utils.GetRdnFromDN(transportDN);
                string? transport = (Utils.GetDNComponents(rdn))[0].Value;

                if (string.Equals(transport, "SMTP", StringComparison.OrdinalIgnoreCase))
                    TransportType = ActiveDirectoryTransportType.Smtp;
                else
                    TransportType = ActiveDirectoryTransportType.Rpc;
            }

            ReplicationNeighborOption = (ReplicationNeighborOptions)neighbor.dwReplicaFlags;
            SourceInvocationId = neighbor.uuidSourceDsaInvocationID;
            UsnLastObjectChangeSynced = neighbor.usnLastObjChangeSynced;
            UsnAttributeFilter = neighbor.usnAttributeFilter;
            LastSuccessfulSync = DateTime.FromFileTime(neighbor.ftimeLastSyncSuccess);
            LastAttemptedSync = DateTime.FromFileTime(neighbor.ftimeLastSyncAttempt);
            LastSyncResult = neighbor.dwLastSyncResult;
            ConsecutiveFailureCount = neighbor.cNumConsecutiveSyncFailures;

            _server = server;
            _nameTable = table;
        }

        public string? PartitionName { get; }

        public string? SourceServer
        {
            get
            {
                if (_sourceServer == null)
                {
                    // check whether we have got it before
                    if (_nameTable.Contains(SourceInvocationId))
                    {
                        _sourceServer = (string?)_nameTable[SourceInvocationId];
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

        public ActiveDirectoryTransportType TransportType { get; }

        public ReplicationNeighborOptions ReplicationNeighborOption { get; }

        public Guid SourceInvocationId { get; }

        public long UsnLastObjectChangeSynced { get; }

        public long UsnAttributeFilter { get; }

        public DateTime LastSuccessfulSync { get; }

        public DateTime LastAttemptedSync { get; }

        public int LastSyncResult { get; }

        public string LastSyncMessage => ExceptionHelper.GetErrorMessage(LastSyncResult, false);

        public int ConsecutiveFailureCount { get; }
    }
}
