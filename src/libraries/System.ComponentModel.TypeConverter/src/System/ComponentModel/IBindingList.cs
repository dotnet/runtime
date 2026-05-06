// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace System.ComponentModel
{
    public interface IBindingList : IList
    {
        bool AllowNew { get; }

        object? AddNew();

        bool AllowEdit { get; }

        bool AllowRemove { get; }

        bool SupportsChangeNotification { get; }

        bool SupportsSearching { get; }

        bool SupportsSorting { get; }

        bool IsSorted { get; }

        PropertyDescriptor? SortProperty { get; }

        ListSortDirection SortDirection { get; }

        event ListChangedEventHandler ListChanged;

        void AddIndex(PropertyDescriptor property);

        void ApplySort(PropertyDescriptor property, ListSortDirection direction);

        int Find(PropertyDescriptor property, object key);

        void RemoveIndex(PropertyDescriptor property);

        void RemoveSort();
    }
}
