//------------------------------------------------------------------------------
// <copyright file="ReplicationCursor.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

/*
 */

 namespace System.DirectoryServices.ActiveDirectory {
    using System;
    using System.Runtime.InteropServices;

    public class ReplicationCursor{
        string partition;
        Guid invocationID;
        long USN;
        string serverDN = null;
        DateTime syncTime;
        bool advanced = false;
        string sourceServer = null;

        private DirectoryServer server = null;
        
        private ReplicationCursor() {}        

        internal ReplicationCursor(DirectoryServer server, string partition, Guid guid, long filter, long time, IntPtr dn)
        {
            this.partition = partition;
            invocationID = guid;
            USN = filter;

            // convert filetime to DateTime
            syncTime = DateTime.FromFileTime(time);

            // get the dn
            serverDN = Marshal.PtrToStringUni(dn);

            advanced = true;

            this.server = server;
        }

        internal ReplicationCursor(DirectoryServer server, string partition, Guid guid, long filter)
        {
            this.partition = partition;
            invocationID = guid;
            USN = filter;

            this.server = server;
        }

        public string PartitionName {
            get {
                return partition;
            }
        }

        public Guid SourceInvocationId {
            get {
                return invocationID;
            }
        }

        public long UpToDatenessUsn {
            get {
                return USN;
            }
        }

        public string SourceServer{
            get {
               // get the source server name if we are on win2k, or above win2k and serverDN is not NULL (means KCC translation is successful)
               if(!advanced || (advanced && serverDN != null))
               {
                   sourceServer = Utils.GetServerNameFromInvocationID(serverDN, SourceInvocationId, server);
               }

               return sourceServer;
            }
        }

        public DateTime LastSuccessfulSyncTime {
            get {
                if(advanced)
                    return syncTime;
                else
                {
                    // win2k client machine does not support this
                    if((Environment.OSVersion.Version.Major == 5) && (Environment.OSVersion.Version.Minor == 0))
                        throw new PlatformNotSupportedException(Res.GetString(Res.DSNotSupportOnClient));
                    else
                        throw new PlatformNotSupportedException(Res.GetString(Res.DSNotSupportOnDC));
                }
            }
        }
        
    }
}
