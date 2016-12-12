//------------------------------------------------------------------------------
// <copyright file="ReplicationOperation.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

/*
 */

 namespace System.DirectoryServices.ActiveDirectory {
    using System;
    using System.Runtime.InteropServices;
    using System.Collections;
    using System.Diagnostics;

    public class ReplicationOperation{
        DateTime timeEnqueued;
        int serialNumber;           
        int priority;                  
        ReplicationOperationType operationType;          
        string namingContext;  
        string dsaDN;           
        Guid uuidDsaObjGuid;

        private DirectoryServer server = null;
        private string sourceServer = null;
        private Hashtable nameTable = null;
        
        internal ReplicationOperation(IntPtr addr, DirectoryServer server, Hashtable table)
        {
            DS_REPL_OP operation = new DS_REPL_OP();
            Marshal.PtrToStructure(addr, operation);

            // get the enqueued time
            timeEnqueued = DateTime.FromFileTime(operation.ftimeEnqueued);

            // get the operation identifier
            serialNumber = operation.ulSerialNumber;

            // get the priority
            priority = operation.ulPriority;

            // get the operation type
            operationType = operation.OpType;

            // get the partition name
            namingContext = Marshal.PtrToStringUni(operation.pszNamingContext);

            // get the dsaDN
            dsaDN = Marshal.PtrToStringUni(operation.pszDsaDN);            

            // get the dsaobject guid
            uuidDsaObjGuid = operation.uuidDsaObjGuid;

            this.server = server;
            this.nameTable = table;
        }

        public DateTime TimeEnqueued {
            get {
                return timeEnqueued;
            }
        }

        public int OperationNumber {
            get {
                return serialNumber;
            }
        }

        public int Priority {
            get {
                return priority;
            }
        }

        public ReplicationOperationType OperationType {
            get {
                return operationType;
            }
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
                    if(nameTable.Contains(SourceServerGuid))
                    {  
                        sourceServer = (string) nameTable[SourceServerGuid];
                    }
                    else if(dsaDN != null)
                    {
                        sourceServer = Utils.GetServerNameFromInvocationID(dsaDN, SourceServerGuid, server);
                        // add it to the hashtable
                        nameTable.Add(SourceServerGuid, sourceServer);                        
                    }
                }

                return sourceServer;
            }
        }
        
        private Guid SourceServerGuid {
            get {
                return uuidDsaObjGuid;
            }
        }
    }
}
