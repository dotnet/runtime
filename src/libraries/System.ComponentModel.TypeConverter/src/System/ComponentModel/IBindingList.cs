// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix", Scope = "type", Target = "System.ComponentModel.IBindingList")]

namespace System.ComponentModel
{
    /// <summary>
    ///    <para>[To be supplied.]</para>
    /// </summary>
    public interface IBindingList : IList
    {
        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        bool AllowNew { get; }
        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        object AddNew();
        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>

        bool AllowEdit { get; }
        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        bool AllowRemove { get; }
        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>

        bool SupportsChangeNotification { get; }
        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>

        bool SupportsSearching { get; }
        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>

        bool SupportsSorting { get; }
        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>

        bool IsSorted { get; }
        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        PropertyDescriptor SortProperty { get; }
        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        ListSortDirection SortDirection { get; }
        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>

        event ListChangedEventHandler ListChanged;
        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>

        void AddIndex(PropertyDescriptor property);
        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        void ApplySort(PropertyDescriptor property, ListSortDirection direction);
        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        int Find(PropertyDescriptor property, object key);
        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        void RemoveIndex(PropertyDescriptor property);
        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        void RemoveSort();
    }
}

