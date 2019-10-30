// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Collections;

namespace System.DirectoryServices.ActiveDirectory
{
    public class ReplicationOperation
    {
        private readonly string _dsaDN;

        private readonly DirectoryServer _server = null;
        private string _sourceServer = null;
        private readonly Hashtable _nameTable = null;

        internal ReplicationOperation(IntPtr addr, DirectoryServer server, Hashtable table)
        {
            DS_REPL_OP operation = new DS_REPL_OP();
            Marshal.PtrToStructure(addr, operation);

            // get the enqueued time
            TimeEnqueued = DateTime.FromFileTime(operation.ftimeEnqueued);

            // get the operation identifier
            OperationNumber = operation.ulSerialNumber;

            // get the priority
            Priority = operation.ulPriority;

            // get the operation type
            OperationType = operation.OpType;

            // get the partition name
            PartitionName = Marshal.PtrToStringUni(operation.pszNamingContext);

            // get the dsaDN
            _dsaDN = Marshal.PtrToStringUni(operation.pszDsaDN);

            // get the dsaobject guid
            SourceServerGuid = operation.uuidDsaObjGuid;

            _server = server;
            _nameTable = table;
        }

        public DateTime TimeEnqueued { get; }

        public int OperationNumber { get; }

        public int Priority { get; }

        public ReplicationOperationType OperationType { get; }

        public string PartitionName { get; }

        public string SourceServer
        {
            get
            {
                if (_sourceServer == null)
                {
                    // check whether we have got it before
                    if (_nameTable.Contains(SourceServerGuid))
                    {
                        _sourceServer = (string)_nameTable[SourceServerGuid];
                    }
                    else if (_dsaDN != null)
                    {
                        _sourceServer = Utils.GetServerNameFromInvocationID(_dsaDN, SourceServerGuid, _server);
                        // add it to the hashtable
                        _nameTable.Add(SourceServerGuid, _sourceServer);
                    }
                }

                return _sourceServer;
            }
        }

        private Guid SourceServerGuid { get; }
    }
}
