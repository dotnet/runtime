// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;

namespace System.DirectoryServices.ActiveDirectory
{
    public class AttributeMetadataCollection : ReadOnlyCollectionBase
    {
        internal AttributeMetadataCollection() { }

        public AttributeMetadata this[int index] => (AttributeMetadata)InnerList[index]!;

        public bool Contains(AttributeMetadata metadata)
        {
            if (metadata == null)
                throw new ArgumentNullException(nameof(metadata));

            for (int i = 0; i < InnerList.Count; i++)
            {
                AttributeMetadata tmp = (AttributeMetadata)InnerList[i]!;
                string name = tmp.Name;

                if (Utils.Compare(name, metadata.Name) == 0)
                {
                    return true;
                }
            }
            return false;
        }

        public int IndexOf(AttributeMetadata metadata)
        {
            if (metadata == null)
                throw new ArgumentNullException(nameof(metadata));

            for (int i = 0; i < InnerList.Count; i++)
            {
                AttributeMetadata tmp = (AttributeMetadata)InnerList[i]!;

                if (Utils.Compare(tmp.Name, metadata.Name) == 0)
                {
                    return i;
                }
            }
            return -1;
        }

        public void CopyTo(AttributeMetadata[] metadata, int index)
        {
            InnerList.CopyTo(metadata, index);
        }

        internal int Add(AttributeMetadata metadata) => InnerList.Add(metadata);
    }
}
