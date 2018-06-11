// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;

namespace System.DirectoryServices.ActiveDirectory
{
    public class ReadOnlySiteLinkCollection : ReadOnlyCollectionBase
    {
        internal ReadOnlySiteLinkCollection() { }

        public ActiveDirectorySiteLink this[int index]
        {
            get => (ActiveDirectorySiteLink)InnerList[index];
        }

        public bool Contains(ActiveDirectorySiteLink link)
        {
            if (link == null)
                throw new ArgumentNullException(nameof(link));

            string dn = (string)PropertyManager.GetPropertyValue(link.context, link.cachedEntry, PropertyManager.DistinguishedName);

            for (int i = 0; i < InnerList.Count; i++)
            {
                ActiveDirectorySiteLink tmp = (ActiveDirectorySiteLink)InnerList[i];
                string tmpDn = (string)PropertyManager.GetPropertyValue(tmp.context, tmp.cachedEntry, PropertyManager.DistinguishedName);

                if (Utils.Compare(tmpDn, dn) == 0)
                {
                    return true;
                }
            }
            return false;
        }

        public int IndexOf(ActiveDirectorySiteLink link)
        {
            if (link == null)
                throw new ArgumentNullException(nameof(link));

            string dn = (string)PropertyManager.GetPropertyValue(link.context, link.cachedEntry, PropertyManager.DistinguishedName);

            for (int i = 0; i < InnerList.Count; i++)
            {
                ActiveDirectorySiteLink tmp = (ActiveDirectorySiteLink)InnerList[i];
                string tmpDn = (string)PropertyManager.GetPropertyValue(tmp.context, tmp.cachedEntry, PropertyManager.DistinguishedName);

                if (Utils.Compare(tmpDn, dn) == 0)
                {
                    return i;
                }
            }
            return -1;
        }

        public void CopyTo(ActiveDirectorySiteLink[] links, int index)
        {
            InnerList.CopyTo(links, index);
        }

        internal int Add(ActiveDirectorySiteLink link) => InnerList.Add(link);

        internal void Clear() => InnerList.Clear();
    }
}
