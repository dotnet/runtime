// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Globalization;

namespace System.Data
{
    internal sealed class RelationshipConverter : ExpandableObjectConverter
    {
        // converter classes should have public ctor
        public RelationshipConverter()
        {
        }

        /// <summary>
        /// Gets a value indicating whether this converter can
        /// convert an object to the given destination type using the context.
        /// </summary>
        public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType)
        {
            if (destinationType == typeof(InstanceDescriptor))
            {
                return true;
            }
            return base.CanConvertTo(context, destinationType);
        }

        /// <summary>
        ///      Converts the given object to another type.  The most common types to convert
        ///      are to and from a string object.  The default implementation will make a call
        ///      to ToString on the object if the object is valid and if the destination
        ///      type is string.  If this cannot convert to the destination type, this will
        ///      throw a NotSupportedException.
        /// </summary>
        public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
        {
            if (destinationType == null)
            {
                throw new ArgumentNullException(nameof(destinationType));
            }

            System.Reflection.ConstructorInfo? ctor = null;
            object[]? values = null;

            if (destinationType == typeof(InstanceDescriptor) && value is DataRelation)
            {
                DataRelation rel = (DataRelation)value;
                DataTable parentTable = rel.ParentKey.Table;
                DataTable childTable = rel.ChildKey.Table;

                if (string.IsNullOrEmpty(parentTable.Namespace) && string.IsNullOrEmpty(childTable.Namespace))
                {
                    ctor = typeof(DataRelation).GetConstructor(new Type[] { typeof(string) /*relationName*/, typeof(string) /*parentTableName*/, typeof(string) /*childTableName */,
                        typeof(string[]) /*parentColumnNames */, typeof(string[])  /*childColumnNames*/, typeof(bool) /*nested*/ })!;

                    values = new object[] { rel.RelationName, rel.ParentKey.Table.TableName, rel.ChildKey.Table.TableName, rel.ParentColumnNames, rel.ChildColumnNames, rel.Nested };
                }
                else
                {
                    ctor = typeof(DataRelation).GetConstructor(new Type[] { typeof(string)/*relationName*/, typeof(string)/*parentTableName*/, typeof(string)/*parentTableNamespace*/,
                        typeof(string)/*childTableName */, typeof(string)/*childTableNamespace */,
                        typeof(string[])/*parentColumnNames */, typeof(string[]) /*childColumnNames*/, typeof(bool) /*nested*/})!;

                    values = new object[] { rel.RelationName, rel.ParentKey.Table.TableName, rel.ParentKey.Table.Namespace, rel.ChildKey.Table.TableName,
                        rel.ChildKey.Table.Namespace, rel.ParentColumnNames, rel.ChildColumnNames, rel.Nested };
                }

                return new InstanceDescriptor(ctor, values);
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}
