// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace System.DirectoryServices
{
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public class SortOption
    {
        private string? _propertyName;
        private SortDirection _sortDirection;

        public SortOption()
        {
        }

        public SortOption(string propertyName, SortDirection direction)
        {
            PropertyName = propertyName;
            Direction = direction;
        }

        [DefaultValue(null)]
        [DisallowNull]
        public string? PropertyName
        {
            get => _propertyName;
            set => _propertyName = value ?? throw new ArgumentNullException(nameof(value));
        }

        [DefaultValue(SortDirection.Ascending)]
        public SortDirection Direction
        {
            get => _sortDirection;
            set
            {
                if (value < SortDirection.Ascending || value > SortDirection.Descending)
                {
                    throw new InvalidEnumArgumentException(nameof(value), (int)value, typeof(SortDirection));
                }

                _sortDirection = value;
            }
        }
    }
}
