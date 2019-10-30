// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace System.Net.NetworkInformation
{
    public class IPAddressCollection : ICollection<IPAddress>
    {
        protected internal IPAddressCollection()
        {
        }

        public virtual void CopyTo(IPAddress[] array, int offset)
        {
            throw NotImplemented.ByDesign;
        }

        public virtual int Count
        {
            get
            {
                throw NotImplemented.ByDesign;
            }
        }

        public virtual bool IsReadOnly
        {
            get
            {
                return true;
            }
        }

        public virtual void Add(IPAddress address)
        {
            throw new NotSupportedException(SR.net_collection_readonly);
        }

        public virtual bool Contains(IPAddress address)
        {
            throw NotImplemented.ByDesign;
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public virtual IEnumerator<IPAddress> GetEnumerator()
        {
            throw NotImplemented.ByDesign;
        }

        public virtual IPAddress this[int index]
        {
            get
            {
                throw NotImplemented.ByDesign;
            }
        }

        public virtual bool Remove(IPAddress address)
        {
            throw new NotSupportedException(SR.net_collection_readonly);
        }

        public virtual void Clear()
        {
            throw new NotSupportedException(SR.net_collection_readonly);
        }
    }
}
