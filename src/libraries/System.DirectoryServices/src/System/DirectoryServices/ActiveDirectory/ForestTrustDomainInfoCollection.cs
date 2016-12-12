// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.DirectoryServices.ActiveDirectory
{
    using System;
    using System.Runtime.InteropServices;
    using System.Collections;
    using System.Globalization;

    public class ForestTrustDomainInfoCollection : ReadOnlyCollectionBase
    {
        internal ForestTrustDomainInfoCollection() { }

        public ForestTrustDomainInformation this[int index]
        {
            get
            {
                return (ForestTrustDomainInformation)InnerList[index];
            }
        }

        public bool Contains(ForestTrustDomainInformation information)
        {
            if (information == null)
                throw new ArgumentNullException("information");

            return InnerList.Contains(information);
        }

        public int IndexOf(ForestTrustDomainInformation information)
        {
            if (information == null)
                throw new ArgumentNullException("information");

            return InnerList.IndexOf(information);
        }

        public void CopyTo(ForestTrustDomainInformation[] array, int index)
        {
            InnerList.CopyTo(array, index);
        }

        internal int Add(ForestTrustDomainInformation info)
        {
            return InnerList.Add(info);
        }
    }
}
