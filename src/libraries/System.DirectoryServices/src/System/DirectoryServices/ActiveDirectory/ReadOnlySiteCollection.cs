// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.DirectoryServices.ActiveDirectory
{
    using System;
    using System.Runtime.InteropServices;
    using System.Collections;
    using System.DirectoryServices;
    using System.Diagnostics;
    using System.Globalization;

    public class ReadOnlySiteCollection : ReadOnlyCollectionBase
    {
        internal ReadOnlySiteCollection() { }

        internal ReadOnlySiteCollection(ArrayList sites)
        {
            for (int i = 0; i < sites.Count; i++)
            {
                Add((ActiveDirectorySite)sites[i]);
            }
        }

        public ActiveDirectorySite this[int index]
        {
            get
            {
                return (ActiveDirectorySite)InnerList[index];
            }
        }

        public bool Contains(ActiveDirectorySite site)
        {
            if (site == null)
                throw new ArgumentNullException("site");

            string dn = (string)PropertyManager.GetPropertyValue(site.context, site.cachedEntry, PropertyManager.DistinguishedName);

            for (int i = 0; i < InnerList.Count; i++)
            {
                ActiveDirectorySite tmp = (ActiveDirectorySite)InnerList[i];

                string tmpDn = (string)PropertyManager.GetPropertyValue(tmp.context, tmp.cachedEntry, PropertyManager.DistinguishedName);

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
                throw new ArgumentNullException("site");

            string dn = (string)PropertyManager.GetPropertyValue(site.context, site.cachedEntry, PropertyManager.DistinguishedName);

            for (int i = 0; i < InnerList.Count; i++)
            {
                ActiveDirectorySite tmp = (ActiveDirectorySite)InnerList[i];

                string tmpDn = (string)PropertyManager.GetPropertyValue(tmp.context, tmp.cachedEntry, PropertyManager.DistinguishedName);

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

        internal int Add(ActiveDirectorySite site)
        {
            return InnerList.Add(site);
        }

        internal void Clear()
        {
            InnerList.Clear();
        }
    }
}
