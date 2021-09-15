// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Globalization;

namespace System.Data
{
    internal sealed class ConstraintConverter : ExpandableObjectConverter
    {
        // converter classes should have public ctor
        public ConstraintConverter() { }

        /// <summary>
        /// Gets a value indicating whether this converter can
        /// convert an object to the given destination type using the context.
        /// </summary>
        public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType) =>
            destinationType == typeof(InstanceDescriptor) ||
            base.CanConvertTo(context, destinationType);

        /// <summary>
        /// Converts the given object to another type.  The most common types to convert
        /// are to and from a string object.  The default implementation will make a call
        /// to ToString on the object if the object is valid and if the destination
        /// type is string.  If this cannot convert to the destination type, this will
        /// throw a NotSupportedException.
        /// </summary>
        public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
        {
            if (destinationType == null)
            {
                throw new ArgumentNullException(nameof(destinationType));
            }

            if (destinationType == typeof(InstanceDescriptor) && value is Constraint)
            {
                if (value is UniqueConstraint)
                {
                    UniqueConstraint constr = (UniqueConstraint)value;
                    Reflection.ConstructorInfo ctor = typeof(UniqueConstraint).GetConstructor(new Type[] { typeof(string), typeof(string[]), typeof(bool) })!;
                    if (ctor != null)
                    {
                        return new InstanceDescriptor(ctor, new object[] { constr.ConstraintName, constr.ColumnNames, constr.IsPrimaryKey });
                    }
                }
                else
                {
                    ForeignKeyConstraint constr = (ForeignKeyConstraint)value;
                    System.Reflection.ConstructorInfo ctor =
                        typeof(ForeignKeyConstraint).GetConstructor(new Type[] { typeof(string), typeof(string), typeof(string[]),
                            typeof(string[]), typeof(AcceptRejectRule), typeof(Rule), typeof(Rule) })!;
                    if (ctor != null)
                    {
                        return new InstanceDescriptor(ctor, new object[] { constr.ConstraintName, constr.ParentKey.Table.TableName, constr.ParentColumnNames,
                            constr.ChildColumnNames, constr.AcceptRejectRule, constr.DeleteRule, constr.UpdateRule });
                    }
                }
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}
