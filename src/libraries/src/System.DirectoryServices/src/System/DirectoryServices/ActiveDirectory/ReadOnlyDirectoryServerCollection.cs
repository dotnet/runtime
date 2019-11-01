// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;

namespace System.DirectoryServices.ActiveDirectory
{
    public class ReadOnlyDirectoryServerCollection : ReadOnlyCollectionBase
    {
        internal ReadOnlyDirectoryServerCollection() { }

        internal ReadOnlyDirectoryServerCollection(ArrayList values)
        {
            if (values != null)
            {
                for (int i = 0; i < values.Count; i++)
                {
                    Add((DirectoryServer)values[i]);
                }
            }
        }

        public DirectoryServer this[int index] => (DirectoryServer)InnerList[index];

        public bool Contains(DirectoryServer directoryServer)
        {
            if (directoryServer == null)
                throw new ArgumentNullException(nameof(directoryServer));

            for (int i = 0; i < InnerList.Count; i++)
            {
                DirectoryServer tmp = (DirectoryServer)InnerList[i];
                if (Utils.Compare(tmp.Name, directoryServer.Name) == 0)
                {
                    return true;
                }
            }
            return false;
        }

        public int IndexOf(DirectoryServer directoryServer)
        {
            if (directoryServer == null)
                throw new ArgumentNullException(nameof(directoryServer));

            for (int i = 0; i < InnerList.Count; i++)
            {
                DirectoryServer tmp = (DirectoryServer)InnerList[i];
                if (Utils.Compare(tmp.Name, directoryServer.Name) == 0)
                {
                    return i;
                }
            }
            return -1;
        }

        public void CopyTo(DirectoryServer[] directoryServers, int index)
        {
            InnerList.CopyTo(directoryServers, index);
        }

        internal int Add(DirectoryServer server) => InnerList.Add(server);

        internal void AddRange(ICollection servers) => InnerList.AddRange(servers);

        internal void Clear() => InnerList.Clear();
    }
}
