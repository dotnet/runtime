// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Runtime.InteropServices;

namespace System.DirectoryServices.ActiveDirectory
{
    public class ReplicationFailureCollection : ReadOnlyCollectionBase
    {
        private readonly DirectoryServer _server = null;
        private readonly Hashtable _nameTable = null;

        internal ReplicationFailureCollection(DirectoryServer server)
        {
            _server = server;
            Hashtable tempNameTable = new Hashtable();
            _nameTable = Hashtable.Synchronized(tempNameTable);
        }

        public ReplicationFailure this[int index] => (ReplicationFailure)InnerList[index];

        public bool Contains(ReplicationFailure failure)
        {
            if (failure == null)
                throw new ArgumentNullException("failure");

            return InnerList.Contains(failure);
        }

        public int IndexOf(ReplicationFailure failure)
        {
            if (failure == null)
                throw new ArgumentNullException("failure");

            return InnerList.IndexOf(failure);
        }

        public void CopyTo(ReplicationFailure[] failures, int index)
        {
            InnerList.CopyTo(failures, index);
        }

        private int Add(ReplicationFailure failure) => InnerList.Add(failure);

        internal void AddHelper(DS_REPL_KCC_DSA_FAILURES failures, IntPtr info)
        {
            // get the count
            int count = failures.cNumEntries;

            IntPtr addr = (IntPtr)0;

            for (int i = 0; i < count; i++)
            {
                addr = IntPtr.Add(info, Marshal.SizeOf(typeof(int)) * 2 + i * Marshal.SizeOf(typeof(DS_REPL_KCC_DSA_FAILURE)));

                ReplicationFailure managedFailure = new ReplicationFailure(addr, _server, _nameTable);

                // in certain scenario, KCC returns some failure records that we need to process it first before returning
                if (managedFailure.LastErrorCode == 0)
                {
                    // we change the error code to some generic one
                    managedFailure.lastResult = ExceptionHelper.ERROR_DS_UNKNOWN_ERROR;
                }

                Add(managedFailure);
            }
        }
    }
}
