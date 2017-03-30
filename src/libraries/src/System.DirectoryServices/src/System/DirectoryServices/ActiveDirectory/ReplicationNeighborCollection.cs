// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.DirectoryServices.ActiveDirectory
{
    using System;
    using System.Collections;
    using System.Runtime.InteropServices;

    public class ReplicationNeighborCollection : ReadOnlyCollectionBase
    {
        private DirectoryServer _server = null;
        private Hashtable _nameTable = null;

        internal ReplicationNeighborCollection(DirectoryServer server)
        {
            _server = server;
            Hashtable tempNameTable = new Hashtable();
            _nameTable = Hashtable.Synchronized(tempNameTable);
        }

        public ReplicationNeighbor this[int index]
        {
            get
            {
                return (ReplicationNeighbor)InnerList[index];
            }
        }

        public bool Contains(ReplicationNeighbor neighbor)
        {
            if (neighbor == null)
                throw new ArgumentNullException("neighbor");

            return InnerList.Contains(neighbor);
        }

        public int IndexOf(ReplicationNeighbor neighbor)
        {
            if (neighbor == null)
                throw new ArgumentNullException("neighbor");

            return InnerList.IndexOf(neighbor);
        }

        public void CopyTo(ReplicationNeighbor[] neighbors, int index)
        {
            InnerList.CopyTo(neighbors, index);
        }

        private int Add(ReplicationNeighbor neighbor)
        {
            return InnerList.Add(neighbor);
        }

        internal void AddHelper(DS_REPL_NEIGHBORS neighbors, IntPtr info)
        {
            // get the count
            int count = neighbors.cNumNeighbors;

            IntPtr addr = (IntPtr)0;

            for (int i = 0; i < count; i++)
            {
                addr = IntPtr.Add(info, Marshal.SizeOf(typeof(int)) * 2 + i * Marshal.SizeOf(typeof(DS_REPL_NEIGHBOR)));

                ReplicationNeighbor managedNeighbor = new ReplicationNeighbor(addr, _server, _nameTable);

                Add(managedNeighbor);
            }
        }
    }
}

