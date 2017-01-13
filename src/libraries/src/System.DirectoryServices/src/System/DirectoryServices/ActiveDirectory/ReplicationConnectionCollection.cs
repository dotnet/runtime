// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.DirectoryServices.ActiveDirectory
{
    using System;
    using System.Collections;
    using System.Globalization;

    public class ReplicationConnectionCollection : ReadOnlyCollectionBase
    {
        internal ReplicationConnectionCollection() { }

        public ReplicationConnection this[int index]
        {
            get
            {
                return (ReplicationConnection)InnerList[index];
            }
        }

        public bool Contains(ReplicationConnection connection)
        {
            if (connection == null)
                throw new ArgumentNullException("connection");

            if (!connection.existingConnection)
                throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, SR.ConnectionNotCommitted , connection.Name));

            string dn = (string)PropertyManager.GetPropertyValue(connection.context, connection.cachedDirectoryEntry, PropertyManager.DistinguishedName);

            for (int i = 0; i < InnerList.Count; i++)
            {
                ReplicationConnection tmp = (ReplicationConnection)InnerList[i];
                string tmpDn = (string)PropertyManager.GetPropertyValue(tmp.context, tmp.cachedDirectoryEntry, PropertyManager.DistinguishedName);

                if (Utils.Compare(tmpDn, dn) == 0)
                {
                    return true;
                }
            }
            return false;
        }

        public int IndexOf(ReplicationConnection connection)
        {
            if (connection == null)
                throw new ArgumentNullException("connection");

            if (!connection.existingConnection)
                throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, SR.ConnectionNotCommitted , connection.Name));

            string dn = (string)PropertyManager.GetPropertyValue(connection.context, connection.cachedDirectoryEntry, PropertyManager.DistinguishedName);

            for (int i = 0; i < InnerList.Count; i++)
            {
                ReplicationConnection tmp = (ReplicationConnection)InnerList[i];
                string tmpDn = (string)PropertyManager.GetPropertyValue(tmp.context, tmp.cachedDirectoryEntry, PropertyManager.DistinguishedName);

                if (Utils.Compare(tmpDn, dn) == 0)
                {
                    return i;
                }
            }
            return -1;
        }

        public void CopyTo(ReplicationConnection[] connections, int index)
        {
            InnerList.CopyTo(connections, index);
        }

        internal int Add(ReplicationConnection value)
        {
            return InnerList.Add(value);
        }
    }
}
