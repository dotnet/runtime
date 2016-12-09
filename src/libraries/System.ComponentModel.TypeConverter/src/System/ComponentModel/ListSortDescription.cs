// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Security.Permissions;

namespace System.ComponentModel
{
    /// <summary>
    ///    <para>[To be supplied.]</para>
    /// </summary>
    public class ListSortDescription
    {
        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        public ListSortDescription(PropertyDescriptor property, ListSortDirection direction)
        {
            PropertyDescriptor = property;
            SortDirection = direction;
        }

        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        public PropertyDescriptor PropertyDescriptor { get; set; }

        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        public ListSortDirection SortDirection { get; set; }
    }
}
