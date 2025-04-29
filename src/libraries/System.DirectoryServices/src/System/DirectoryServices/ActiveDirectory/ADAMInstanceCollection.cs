// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;

namespace System.DirectoryServices.ActiveDirectory
{
    public class AdamInstanceCollection : ReadOnlyCollectionBase
    {
        internal AdamInstanceCollection() { }

        internal AdamInstanceCollection(ArrayList values)
        {
            if (values != null)
            {
                InnerList.AddRange(values);
            }
        }

        public AdamInstance this[int index] => (AdamInstance)InnerList[index]!;

        public bool Contains(AdamInstance adamInstance)
        {
            if (adamInstance == null)
            {
                throw new ArgumentNullException(nameof(adamInstance));
            }

            for (int i = 0; i < InnerList.Count; i++)
            {
                AdamInstance tmp = (AdamInstance)InnerList[i]!;
                if (Utils.Compare(tmp.Name, adamInstance.Name) == 0)
                {
                    return true;
                }
            }
            return false;
        }

        public int IndexOf(AdamInstance adamInstance)
        {
            if (adamInstance == null)
            {
                throw new ArgumentNullException(nameof(adamInstance));
            }

            for (int i = 0; i < InnerList.Count; i++)
            {
                AdamInstance tmp = (AdamInstance)InnerList[i]!;
                if (Utils.Compare(tmp.Name, adamInstance.Name) == 0)
                {
                    return i;
                }
            }

            return -1;
        }

        public void CopyTo(AdamInstance[] adamInstances, int index)
        {
            InnerList.CopyTo(adamInstances, index);
        }
    }
}
