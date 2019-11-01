// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Runtime.InteropServices;

namespace System.DirectoryServices.ActiveDirectory
{
    public class ReplicationOperationCollection : ReadOnlyCollectionBase
    {
        private readonly DirectoryServer _server = null;
        private readonly Hashtable _nameTable = null;

        internal ReplicationOperationCollection(DirectoryServer server)
        {
            _server = server;
            Hashtable tempNameTable = new Hashtable();
            _nameTable = Hashtable.Synchronized(tempNameTable);
        }

        public ReplicationOperation this[int index] => (ReplicationOperation)InnerList[index];

        public bool Contains(ReplicationOperation operation)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));

            return InnerList.Contains(operation);
        }

        public int IndexOf(ReplicationOperation operation)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));

            return InnerList.IndexOf(operation);
        }

        public void CopyTo(ReplicationOperation[] operations, int index)
        {
            InnerList.CopyTo(operations, index);
        }

        private int Add(ReplicationOperation operation) => InnerList.Add(operation);

        internal void AddHelper(DS_REPL_PENDING_OPS operations, IntPtr info)
        {
            // get the count
            int count = operations.cNumPendingOps;

            IntPtr addr = (IntPtr)0;

            for (int i = 0; i < count; i++)
            {
                addr = IntPtr.Add(info, Marshal.SizeOf(typeof(DS_REPL_PENDING_OPS)) + i * Marshal.SizeOf(typeof(DS_REPL_OP)));
                ReplicationOperation managedOperation = new ReplicationOperation(addr, _server, _nameTable);

                Add(managedOperation);
            }
        }

        internal ReplicationOperation GetFirstOperation()
        {
            ReplicationOperation op = (ReplicationOperation)InnerList[0];
            InnerList.RemoveAt(0);

            return op;
        }
    }
}
