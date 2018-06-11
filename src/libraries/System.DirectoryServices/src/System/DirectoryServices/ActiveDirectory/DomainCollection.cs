// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;

namespace System.DirectoryServices.ActiveDirectory
{
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

        public Domain this[int index] => (Domain)InnerList[index];

        public bool Contains(Domain domain)
        {
            if (domain == null)
                throw new ArgumentNullException(nameof(domain));

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
                throw new ArgumentNullException(nameof(domain));

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

        internal int Add(Domain domain) => InnerList.Add(domain);

        internal void Clear() => InnerList.Clear();
    }
}
