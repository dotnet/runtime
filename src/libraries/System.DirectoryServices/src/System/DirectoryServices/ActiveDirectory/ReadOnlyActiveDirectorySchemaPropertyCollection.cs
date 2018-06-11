// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;

namespace System.DirectoryServices.ActiveDirectory
{
    public class ReadOnlyActiveDirectorySchemaPropertyCollection : ReadOnlyCollectionBase
    {
        internal ReadOnlyActiveDirectorySchemaPropertyCollection() { }

        internal ReadOnlyActiveDirectorySchemaPropertyCollection(ArrayList values)
        {
            if (values != null)
            {
                InnerList.AddRange(values);
            }
        }

        public ActiveDirectorySchemaProperty this[int index]
        {
            get => (ActiveDirectorySchemaProperty)InnerList[index];
        }

        public bool Contains(ActiveDirectorySchemaProperty schemaProperty)
        {
            if (schemaProperty == null)
                throw new ArgumentNullException(nameof(schemaProperty));
            for (int i = 0; i < InnerList.Count; i++)
            {
                ActiveDirectorySchemaProperty tmp = (ActiveDirectorySchemaProperty)InnerList[i];
                if (Utils.Compare(tmp.Name, schemaProperty.Name) == 0)
                {
                    return true;
                }
            }
            return false;
        }

        public int IndexOf(ActiveDirectorySchemaProperty schemaProperty)
        {
            if (schemaProperty == null)
                throw new ArgumentNullException(nameof(schemaProperty));

            for (int i = 0; i < InnerList.Count; i++)
            {
                ActiveDirectorySchemaProperty tmp = (ActiveDirectorySchemaProperty)InnerList[i];
                if (Utils.Compare(tmp.Name, schemaProperty.Name) == 0)
                {
                    return i;
                }
            }
            return -1;
        }

        public void CopyTo(ActiveDirectorySchemaProperty[] properties, int index)
        {
            InnerList.CopyTo(properties, index);
        }
    }
}
