// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;

namespace System.ComponentModel
{
    [SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix")]
    public interface IBindingListView : IBindingList
    {
        void ApplySort(ListSortDescriptionCollection sorts);

        string Filter { get; set; }

        ListSortDescriptionCollection SortDescriptions { get; }

        void RemoveFilter();

        bool SupportsAdvancedSorting { get; }
        bool SupportsFiltering { get; }
    }
}
