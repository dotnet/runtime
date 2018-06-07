// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel
{
    public class ListSortDescription
    {
        public ListSortDescription(PropertyDescriptor property, ListSortDirection direction)
        {
            PropertyDescriptor = property;
            SortDirection = direction;
        }

        public PropertyDescriptor PropertyDescriptor { get; set; }

        public ListSortDirection SortDirection { get; set; }
    }
}
