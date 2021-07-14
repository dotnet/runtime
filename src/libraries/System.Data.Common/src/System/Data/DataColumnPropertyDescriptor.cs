// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.ComponentModel;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;

namespace System.Data
{
    internal sealed class DataColumnPropertyDescriptor : PropertyDescriptor
    {
        internal DataColumnPropertyDescriptor(DataColumn dataColumn) : base(dataColumn.ColumnName, null)
        {
            Column = dataColumn;
        }

        public override AttributeCollection Attributes
        {
            get
            {
                if (typeof(IList).IsAssignableFrom(PropertyType))
                {
                    Attribute[] attrs = new Attribute[base.Attributes.Count + 1];
                    base.Attributes.CopyTo(attrs, 0);

                    // we don't want to show the columns which are of type IList in the designer
                    attrs[attrs.Length - 1] = new ListBindableAttribute(false);
                    return new AttributeCollection(attrs);
                }
                else
                {
                    return base.Attributes;
                }
            }
        }

        internal DataColumn Column { get; }

        public override Type ComponentType => typeof(DataRowView);

        public override bool IsReadOnly => Column.ReadOnly;

        public override Type PropertyType => Column.DataType;

        public override bool Equals([NotNullWhen(true)] object? other)
        {
            if (other is DataColumnPropertyDescriptor)
            {
                DataColumnPropertyDescriptor descriptor = (DataColumnPropertyDescriptor)other;
                return (descriptor.Column == Column);
            }

            return false;
        }

        public override int GetHashCode() => Column.GetHashCode();

        public override bool CanResetValue(object component)
        {
            DataRowView dataRowView = (DataRowView)component;
            if (!Column.IsSqlType)
            {
                return (dataRowView.GetColumnValue(Column) != DBNull.Value);
            }

            return (!DataStorage.IsObjectNull(dataRowView.GetColumnValue(Column)));
        }

        public override object GetValue(object? component)
        {
            DataRowView dataRowView = (DataRowView)component!;
            return dataRowView.GetColumnValue(Column);
        }

        public override void ResetValue(object component)
        {
            DataRowView dataRowView = (DataRowView)component;
            dataRowView.SetColumnValue(Column, DBNull.Value); // no need to ccheck for the col type and set Sql...Null!
        }

        public override void SetValue(object? component, object? value)
        {
            DataRowView dataRowView = (DataRowView)component!;
            dataRowView.SetColumnValue(Column, value);
            OnValueChanged(component, EventArgs.Empty);
        }

        public override bool ShouldSerializeValue(object component) => false;

        public override bool IsBrowsable => Column.ColumnMapping == MappingType.Hidden ? false : base.IsBrowsable;
    }
}
