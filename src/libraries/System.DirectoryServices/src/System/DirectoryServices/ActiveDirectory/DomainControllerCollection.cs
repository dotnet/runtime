// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;

namespace System.DirectoryServices.ActiveDirectory
{
    public class DomainControllerCollection : ReadOnlyCollectionBase
    {
        internal DomainControllerCollection() { }

        internal DomainControllerCollection(ArrayList values)
        {
            if (values != null)
            {
                InnerList.AddRange(values);
            }
        }

        public DomainController this[int index] => (DomainController)InnerList[index];

        public bool Contains(DomainController domainController)
        {
            if (domainController == null)
                throw new ArgumentNullException("domainController");

            for (int i = 0; i < InnerList.Count; i++)
            {
                DomainController tmp = (DomainController)InnerList[i];
                if (Utils.Compare(tmp.Name, domainController.Name) == 0)
                {
                    return true;
                }
            }
            return false;
        }

        public int IndexOf(DomainController domainController)
        {
            if (domainController == null)
                throw new ArgumentNullException("domainController");

            for (int i = 0; i < InnerList.Count; i++)
            {
                DomainController tmp = (DomainController)InnerList[i];
                if (Utils.Compare(tmp.Name, domainController.Name) == 0)
                {
                    return i;
                }
            }
            return -1;
        }

        public void CopyTo(DomainController[] domainControllers, int index)
        {
            InnerList.CopyTo(domainControllers, index);
        }
    }
}
