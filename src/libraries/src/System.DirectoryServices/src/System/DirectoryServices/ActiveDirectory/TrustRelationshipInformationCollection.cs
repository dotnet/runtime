// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;

namespace System.DirectoryServices.ActiveDirectory
{
    public class TrustRelationshipInformationCollection : ReadOnlyCollectionBase
    {
        internal TrustRelationshipInformationCollection() { }

        internal TrustRelationshipInformationCollection(DirectoryContext context, string source, ArrayList trusts)
        {
            for (int i = 0; i < trusts.Count; i++)
            {
                TrustObject obj = (TrustObject)trusts[i];
                // we don't need self and forest trust
                if ((obj.TrustType == TrustType.Forest) || ((int)obj.TrustType == 7))
                {
                    continue;
                }

                TrustRelationshipInformation info = new TrustRelationshipInformation(context, source, obj);
                Add(info);
            }
        }

        public TrustRelationshipInformation this[int index]
        {
            get => (TrustRelationshipInformation)InnerList[index];
        }

        public bool Contains(TrustRelationshipInformation information)
        {
            if (information == null)
                throw new ArgumentNullException("information");

            return InnerList.Contains(information);
        }

        public int IndexOf(TrustRelationshipInformation information)
        {
            if (information == null)
                throw new ArgumentNullException("information");

            return InnerList.IndexOf(information);
        }

        public void CopyTo(TrustRelationshipInformation[] array, int index)
        {
            InnerList.CopyTo(array, index);
        }

        internal int Add(TrustRelationshipInformation info) => InnerList.Add(info);
    }
}
