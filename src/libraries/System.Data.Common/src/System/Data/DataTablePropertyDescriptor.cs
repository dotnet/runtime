// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace System.Data
{
    internal sealed class DataTablePropertyDescriptor : PropertyDescriptor
    {
        public DataTable Table { get; }

        internal DataTablePropertyDescriptor(DataTable dataTable) : base(dataTable.TableName, null)
        {
            Table = dataTable;
        }

        public override Type ComponentType => typeof(DataRowView);

        public override bool IsReadOnly => false;

        public override Type PropertyType => typeof(IBindingList);

        public override bool Equals([NotNullWhen(true)] object? other) =>
            other is DataTablePropertyDescriptor descriptor &&
            descriptor.Table == Table;

        public override int GetHashCode() => Table.GetHashCode();

        public override bool CanResetValue(object component) => false;

        public override object GetValue(object? component)
        {
            DataViewManagerListItemTypeDescriptor dataViewManagerListItem = (DataViewManagerListItemTypeDescriptor)component!;
            return dataViewManagerListItem.GetDataView(Table);
        }

        public override void ResetValue(object component) { }

        public override void SetValue(object? component, object? value) { }

        public override bool ShouldSerializeValue(object component) => false;
    }
}
