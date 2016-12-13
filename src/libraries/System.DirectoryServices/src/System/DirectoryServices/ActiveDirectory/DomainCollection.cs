// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.DirectoryServices.ActiveDirectory
{
    using System;
    using System.Collections;
    using System.Globalization;
    using System.DirectoryServices;

    public class DomainCollection : ReadOnlyCollectionBase
    {
        internal DomainCollection() { }

        internal DomainCollection(ArrayList values)
        {
            if (values != null)
            {
                for (int i = 0; i < values.Count; i++)
                {
                    Add((Domain)values[i]);
                }
            }
        }

        public Domain this[int index]
        {
            get
            {
                return (Domain)InnerList[index];
            }
        }

        public bool Contains(Domain domain)
        {
            if (domain == null)
                throw new ArgumentNullException("domain");

            for (int i = 0; i < InnerList.Count; i++)
            {
                Domain tmp = (Domain)InnerList[i];
                if (Utils.Compare(tmp.Name, domain.Name) == 0)
                {
                    return true;
                }
            }
            return false;
        }

        public int IndexOf(Domain domain)
        {
            if (domain == null)
                throw new ArgumentNullException("domain");

            for (int i = 0; i < InnerList.Count; i++)
            {
                Domain tmp = (Domain)InnerList[i];
                if (Utils.Compare(tmp.Name, domain.Name) == 0)
                {
                    return i;
                }
            }
            return -1;
        }

        public void CopyTo(Domain[] domains, int index)
        {
            InnerList.CopyTo(domains, index);
        }

        internal int Add(Domain domain)
        {
            return InnerList.Add(domain);
        }
        internal void Clear()
        {
            InnerList.Clear();
        }
    }
}
