// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;

namespace System.ComponentModel
{
    /// <summary>
    ///    <para>[To be supplied.]</para>
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix")]
    public interface IBindingListView : IBindingList
    {
        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        void ApplySort(ListSortDescriptionCollection sorts);
        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        string Filter { get; set; }
        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        ListSortDescriptionCollection SortDescriptions { get; }
        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        void RemoveFilter();
        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        bool SupportsAdvancedSorting { get; }
        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        bool SupportsFiltering { get; }
    }
}

