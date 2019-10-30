// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;

namespace System.DirectoryServices.ActiveDirectory
{
    public class ReadOnlyActiveDirectorySchemaClassCollection : ReadOnlyCollectionBase
    {
        internal ReadOnlyActiveDirectorySchemaClassCollection() { }

        internal ReadOnlyActiveDirectorySchemaClassCollection(ICollection values)
        {
            if (values != null)
            {
                InnerList.AddRange(values);
            }
        }

        public ActiveDirectorySchemaClass this[int index]
        {
            get => (ActiveDirectorySchemaClass)InnerList[index];
        }

        public bool Contains(ActiveDirectorySchemaClass schemaClass)
        {
            if (schemaClass == null)
                throw new ArgumentNullException(nameof(schemaClass));

            for (int i = 0; i < InnerList.Count; i++)
            {
                ActiveDirectorySchemaClass tmp = (ActiveDirectorySchemaClass)InnerList[i];
                if (Utils.Compare(tmp.Name, schemaClass.Name) == 0)
                {
                    return true;
                }
            }
            return false;
        }

        public int IndexOf(ActiveDirectorySchemaClass schemaClass)
        {
            if (schemaClass == null)
                throw new ArgumentNullException(nameof(schemaClass));

            for (int i = 0; i < InnerList.Count; i++)
            {
                ActiveDirectorySchemaClass tmp = (ActiveDirectorySchemaClass)InnerList[i];
                if (Utils.Compare(tmp.Name, schemaClass.Name) == 0)
                {
                    return i;
                }
            }
            return -1;
        }

        public void CopyTo(ActiveDirectorySchemaClass[] classes, int index)
        {
            InnerList.CopyTo(classes, index);
        }
    }
}
