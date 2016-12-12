// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.DirectoryServices.ActiveDirectory
{
    using System;
    using System.Runtime.InteropServices;
    using System.Collections;
    using System.Diagnostics;

    public class ReplicationOperation
    {
        private DateTime _timeEnqueued;
        private int _serialNumber;
        private int _priority;
        private ReplicationOperationType _operationType;
        private string _namingContext;
        private string _dsaDN;
        private Guid _uuidDsaObjGuid;

        private DirectoryServer _server = null;
        private string _sourceServer = null;
        private Hashtable _nameTable = null;

        internal ReplicationOperation(IntPtr addr, DirectoryServer server, Hashtable table)
        {
            DS_REPL_OP operation = new DS_REPL_OP();
            Marshal.PtrToStructure(addr, operation);

            // get the enqueued time
            _timeEnqueued = DateTime.FromFileTime(operation.ftimeEnqueued);

            // get the operation identifier
            _serialNumber = operation.ulSerialNumber;

            // get the priority
            _priority = operation.ulPriority;

            // get the operation type
            _operationType = operation.OpType;

            // get the partition name
            _namingContext = Marshal.PtrToStringUni(operation.pszNamingContext);

            // get the dsaDN
            _dsaDN = Marshal.PtrToStringUni(operation.pszDsaDN);

            // get the dsaobject guid
            _uuidDsaObjGuid = operation.uuidDsaObjGuid;

            _server = server;
            _nameTable = table;
        }

        public DateTime TimeEnqueued
        {
            get
            {
                return _timeEnqueued;
            }
        }

        public int OperationNumber
        {
            get
            {
                return _serialNumber;
            }
        }

        public int Priority
        {
            get
            {
                return _priority;
            }
        }

        public ReplicationOperationType OperationType
        {
            get
            {
                return _operationType;
            }
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

        private Guid SourceServerGuid
        {
            get
            {
                return _uuidDsaObjGuid;
            }
        }
    }
}
