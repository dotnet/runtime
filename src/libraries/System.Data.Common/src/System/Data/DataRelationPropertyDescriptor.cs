// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace System.Data
{
    internal sealed class DataRelationPropertyDescriptor : PropertyDescriptor
    {
        internal DataRelationPropertyDescriptor(DataRelation dataRelation) : base(dataRelation.RelationName, null)
        {
            Relation = dataRelation;
        }

        internal DataRelation Relation { get; }

        public override Type ComponentType => typeof(DataRowView);

        public override bool IsReadOnly => false;

        public override Type PropertyType => typeof(IBindingList);

        public override bool Equals([NotNullWhen(true)] object? other)
        {
            if (other is DataRelationPropertyDescriptor)
            {
                DataRelationPropertyDescriptor descriptor = (DataRelationPropertyDescriptor)other;
                return (descriptor.Relation == Relation);
            }
            return false;
        }

        public override int GetHashCode() => Relation.GetHashCode();

        public override bool CanResetValue(object component) => false;

        public override object GetValue(object? component)
        {
            DataRowView dataRowView = (DataRowView)component!;
            return dataRowView.CreateChildView(Relation);
        }

        public override void ResetValue(object component) { }

        public override void SetValue(object? component, object? value) { }

        public override bool ShouldSerializeValue(object component) => false;
    }
}
