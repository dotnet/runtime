//------------------------------------------------------------------------------
// <copyright file="ReplicationNeighbor.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

/*
 */

  namespace System.DirectoryServices.ActiveDirectory {
    using System;
    using System.Runtime.InteropServices;
    using System.Collections;
    using System.Globalization;

    public enum ActiveDirectoryTransportType {
        Rpc = 0,
        Smtp = 1
    }

    public class ReplicationNeighbor{

        [Flags]
        public enum ReplicationNeighborOptions : long{
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

        string namingContext;  
        string sourceServerDN;          
        ActiveDirectoryTransportType transportType; 
        ReplicationNeighborOptions replicaFlags;                         
        Guid uuidSourceDsaInvocationID;          
        long usnLastObjChangeSynced; 
        long usnAttributeFilter;  
        DateTime timeLastSyncSuccess;  
        DateTime timeLastSyncAttempt;  
        int lastSyncResult;  
        int consecutiveSyncFailures;

        private DirectoryServer server = null;
        private string sourceServer = null;
        private Hashtable nameTable = null;        
        
        internal ReplicationNeighbor(IntPtr addr, DirectoryServer server, Hashtable table)
        {
            DS_REPL_NEIGHBOR neighbor = new DS_REPL_NEIGHBOR();
            Marshal.PtrToStructure(addr, neighbor);

            namingContext = Marshal.PtrToStringUni(neighbor.pszNamingContext);
            sourceServerDN = Marshal.PtrToStringUni(neighbor.pszSourceDsaDN);            

            string transportDN = Marshal.PtrToStringUni(neighbor.pszAsyncIntersiteTransportDN);
            if(transportDN != null)
            {
                string rdn = Utils.GetRdnFromDN(transportDN);
                string transport = (Utils.GetDNComponents(rdn))[0].Value;

                if(String.Compare(transport, "SMTP", StringComparison.OrdinalIgnoreCase) == 0)
                    transportType = ActiveDirectoryTransportType.Smtp;
                else
                    transportType = ActiveDirectoryTransportType.Rpc;
            }

            replicaFlags = (ReplicationNeighborOptions) neighbor.dwReplicaFlags;                        
            uuidSourceDsaInvocationID = neighbor.uuidSourceDsaInvocationID;            
            usnLastObjChangeSynced = neighbor.usnLastObjChangeSynced;
            usnAttributeFilter = neighbor.usnAttributeFilter;
            timeLastSyncSuccess = DateTime.FromFileTime(neighbor.ftimeLastSyncSuccess);
            timeLastSyncAttempt = DateTime.FromFileTime(neighbor.ftimeLastSyncAttempt);
            lastSyncResult = neighbor.dwLastSyncResult;
            consecutiveSyncFailures = neighbor.cNumConsecutiveSyncFailures;            

            this.server = server;
            this.nameTable = table;
            
        }

        public string PartitionName {
            get {
                return namingContext;
            }
        }

        public string SourceServer {
            get {
                if(sourceServer == null)
                {
                    // check whether we have got it before
                    if(nameTable.Contains(SourceInvocationId))
                    {  
                        sourceServer = (string) nameTable[SourceInvocationId];
                    }
                    else if(sourceServerDN != null)
                    {
                        sourceServer = Utils.GetServerNameFromInvocationID(sourceServerDN, SourceInvocationId, server);
                        // add it to the hashtable
                        nameTable.Add(SourceInvocationId, sourceServer);
                    }
                }

                return sourceServer;
            }
        }        

        public ActiveDirectoryTransportType TransportType {
            get {
                return transportType;
            }
        }

        public ReplicationNeighborOptions ReplicationNeighborOption {
            get {
                return replicaFlags;
            }
        }              

        public Guid SourceInvocationId {
            get {
                return uuidSourceDsaInvocationID;
            }
        }        

        public long UsnLastObjectChangeSynced {
            get {
                return usnLastObjChangeSynced;
            }
        }

        public long UsnAttributeFilter {
            get {
                return usnAttributeFilter;
            }
        }

        public DateTime LastSuccessfulSync {
            get {
                return timeLastSyncSuccess;
            }
        }

        public DateTime LastAttemptedSync {
            get {
                return timeLastSyncAttempt;
            }
        }

        public int LastSyncResult {
            get {
                return lastSyncResult;
            }
        }

        public string LastSyncMessage {
            get {
                return ExceptionHelper.GetErrorMessage(lastSyncResult, false);
            }
        }

        public int ConsecutiveFailureCount {
            get {
                return consecutiveSyncFailures;
            }
        }
        
    }
}
