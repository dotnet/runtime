// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Globalization;
using System.Data.SqlTypes;
using System.Reflection;

namespace System.Data
{
    /// <summary>
    /// Provides a type converter that can be used to populate a list box with available types.
    /// </summary>
    internal sealed class ColumnTypeConverter : TypeConverter
    {
        private static readonly Type[] s_types = new Type[] {
            typeof(bool),
            typeof(byte),
            typeof(byte[]),
            typeof(char),
            typeof(DateTime),
            typeof(decimal),
            typeof(double),
            typeof(Guid),
            typeof(short),
            typeof(int),
            typeof(long),
            typeof(object),
            typeof(sbyte),
            typeof(float),
            typeof(string),
            typeof(TimeSpan),
            typeof(ushort),
            typeof(uint),
            typeof(ulong),
            typeof(SqlInt16),
            typeof(SqlInt32),
            typeof(SqlInt64),
            typeof(SqlDecimal),
            typeof(SqlSingle),
            typeof(SqlDouble),
            typeof(SqlString),
            typeof(SqlBoolean),
            typeof(SqlBinary),
            typeof(SqlByte),
            typeof(SqlDateTime),
            typeof(SqlGuid),
            typeof(SqlMoney),
            typeof(SqlBytes),
            typeof(SqlChars),
            typeof(SqlXml)
        };
        private StandardValuesCollection _values;

        public ColumnTypeConverter() { }

        /// <summary>
        /// Gets a value indicating whether this converter can convert an object to the given destination type using the context.
        /// </summary>
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) =>
            destinationType == typeof(InstanceDescriptor) ||
            base.CanConvertTo(context, destinationType);

        /// <summary>
        /// Converts the given value object to the specified destination type.
        /// </summary>
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == null)
            {
                throw new ArgumentNullException(nameof(destinationType));
            }

            if (destinationType == typeof(string))
            {
                return value != null ? value.ToString() : string.Empty;
            }

            if (value != null && destinationType == typeof(InstanceDescriptor))
            {
                object newValue = value;
                if (value is string)
                {
                    for (int i = 0; i < s_types.Length; i++)
                    {
                        if (s_types[i].ToString().Equals(value))
                        {
                            newValue = s_types[i];
                        }
                    }
                }

                if (value is Type || value is string)
                {
                    MethodInfo method = typeof(Type).GetMethod("GetType", new Type[] { typeof(string) });
                    if (method != null)
                    {
                        return new InstanceDescriptor(method, new object[] { ((Type)newValue).AssemblyQualifiedName });
                    }
                }
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }

        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) =>
            sourceType == typeof(string) ||
            base.CanConvertTo(context, sourceType);

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value != null && value.GetType() == typeof(string))
            {
                for (int i = 0; i < s_types.Length; i++)
                {
                    if (s_types[i].ToString().Equals(value))
                    {
                        return s_types[i];
                    }
                }

                return typeof(string);
            }

            return base.ConvertFrom(context, culture, value);
        }

        /// <summary>
        /// Gets a collection of standard values for the data type this validator is designed for.
        /// </summary>
        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            if (_values == null)
            {
                object[] objTypes;

                if (s_types != null)
                {
                    objTypes = new object[s_types.Length];
                    Array.Copy(s_types, 0, objTypes, 0, s_types.Length);
                }
                else
                {
                    objTypes = null;
                }

                _values = new StandardValuesCollection(objTypes);
            }
            return _values;
        }

        /// <summary>
        /// Gets a value indicating whether the list of standard values returned from
        /// <see cref='System.ComponentModel.TypeListConverter.GetStandardValues'/> is an exclusive list. 
        /// </summary>
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) => true;

        /// <summary>
        /// Gets a value indicating whether this object supports a
        /// standard set of values that can be picked from a list using the specified context.
        /// </summary>
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context) => true;
    }
}
