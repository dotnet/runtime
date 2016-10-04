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
        private PropertyDescriptor _property;
        private ListSortDirection _sortDirection;
        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        public ListSortDescription(PropertyDescriptor property, ListSortDirection direction)
        {
            _property = property;
            _sortDirection = direction;
        }

        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        public PropertyDescriptor PropertyDescriptor
        {
            get
            {
                return _property;
            }
            set
            {
                _property = value;
            }
        }

        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        public ListSortDirection SortDirection
        {
            get
            {
                return _sortDirection;
            }
            set
            {
                _sortDirection = value;
            }
        }
    }
}
