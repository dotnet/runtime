// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

        public DisplayColumnAttribute(string displayColumn, string? sortColumn)
            : this(displayColumn, sortColumn, false)
        {
        }

        public DisplayColumnAttribute(string displayColumn, string? sortColumn, bool sortDescending)
        {
            DisplayColumn = displayColumn;
            SortColumn = sortColumn;
            SortDescending = sortDescending;
        }

        public string DisplayColumn { get; }

        public string? SortColumn { get; }

        public bool SortDescending { get; }
    }
}
