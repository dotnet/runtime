// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;

namespace System.DirectoryServices.ActiveDirectory
{
    public class ReadOnlySiteCollection : ReadOnlyCollectionBase
    {
        internal ReadOnlySiteCollection() { }

        internal ReadOnlySiteCollection(ArrayList sites)
        {
            for (int i = 0; i < sites.Count; i++)
            {
                Add((ActiveDirectorySite)sites[i]!);
            }
        }

        public ActiveDirectorySite this[int index] => (ActiveDirectorySite)InnerList[index]!;

        public bool Contains(ActiveDirectorySite site)
        {
            if (site == null)
                throw new ArgumentNullException(nameof(site));

            string? dn = (string?)PropertyManager.GetPropertyValue(site.context, site.cachedEntry, PropertyManager.DistinguishedName);

            for (int i = 0; i < InnerList.Count; i++)
            {
                ActiveDirectorySite tmp = (ActiveDirectorySite)InnerList[i]!;

                string? tmpDn = (string?)PropertyManager.GetPropertyValue(tmp.context, tmp.cachedEntry, PropertyManager.DistinguishedName);

                if (Utils.Compare(tmpDn, dn) == 0)
                {
                    return true;
                }
            }
            return false;
        }

        public int IndexOf(ActiveDirectorySite site)
        {
            if (site == null)
                throw new ArgumentNullException(nameof(site));

            string? dn = (string?)PropertyManager.GetPropertyValue(site.context, site.cachedEntry, PropertyManager.DistinguishedName);

            for (int i = 0; i < InnerList.Count; i++)
            {
                ActiveDirectorySite tmp = (ActiveDirectorySite)InnerList[i]!;

                string? tmpDn = (string?)PropertyManager.GetPropertyValue(tmp.context, tmp.cachedEntry, PropertyManager.DistinguishedName);

                if (Utils.Compare(tmpDn, dn) == 0)
                {
                    return i;
                }
            }
            return -1;
        }

        public void CopyTo(ActiveDirectorySite[] sites, int index)
        {
            InnerList.CopyTo(sites, index);
        }

        internal int Add(ActiveDirectorySite site) => InnerList.Add(site);

        internal void Clear() => InnerList.Clear();
    }
}
