// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.ComponentModel.DataAnnotations
{
    /// <summary>
    ///     Sets the display column, the sort column, and the sort order for when a table is used as a parent table in FK
    ///     relationships.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public class DisplayColumnAttribute : Attribute
    {
        public DisplayColumnAttribute(string displayColumn)
            : this(displayColumn, null)
        {
        }

        public DisplayColumnAttribute(string displayColumn, string sortColumn)
            : this(displayColumn, sortColumn, false)
        {
        }

        public DisplayColumnAttribute(string displayColumn, string sortColumn, bool sortDescending)
        {
            DisplayColumn = displayColumn;
            SortColumn = sortColumn;
            SortDescending = sortDescending;
        }

        public string DisplayColumn { get; private set; }

        public string SortColumn { get; private set; }

        public bool SortDescending { get; private set; }
    }
}
