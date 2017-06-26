// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Security.Permissions;

namespace System.Drawing.Design
{
    [PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
    public sealed class CategoryNameCollection : ReadOnlyCollectionBase
    {
        public CategoryNameCollection(CategoryNameCollection value) => InnerList.AddRange(value);

        public CategoryNameCollection(string[] value) => InnerList.AddRange(value);

        public string this[int index] => ((string)(InnerList[index]));

        public bool Contains(string value) => InnerList.Contains(value);

        public void CopyTo(string[] array, int index) => InnerList.CopyTo(array, index);

        public int IndexOf(string value) => InnerList.IndexOf(value);
    }
}
