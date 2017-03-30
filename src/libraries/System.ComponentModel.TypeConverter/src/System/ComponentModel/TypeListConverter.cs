// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.Design.Serialization;
using System.Globalization;

namespace System.ComponentModel
{
    /// <summary>
    ///    <para>Provides a type
    ///       converter that can be used to populate a list box with available types.</para>
    /// </summary>
    public abstract class TypeListConverter : TypeConverter
    {
        private readonly Type[] _types;
        private StandardValuesCollection _values;

        /// <summary>
        /// <para>Initializes a new instance of the <see cref='System.ComponentModel.TypeListConverter'/> class using
        ///    the type array as the available types.</para>
        /// </summary>
        protected TypeListConverter(Type[] types)
        {
            _types = types;
        }

        /// <internalonly/>
        /// <summary>
        ///    <para>Gets a value indicating whether this converter
        ///       can convert an object in the given source type to an enumeration object using
        ///       the specified context.</para>
        /// </summary>
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType == typeof(string))
            {
                return true;
            }
            return base.CanConvertFrom(context, sourceType);
        }

        /// <summary>
        ///    <para>
        ///        Gets a value indicating whether this converter can convert an object
        ///        to the given destination type using the context.
        ///    </para>
        /// </summary>
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            if (destinationType == typeof(InstanceDescriptor))
            {
                return true;
            }
            return base.CanConvertTo(context, destinationType);
        }

        /// <internalonly/>
        /// <summary>
        ///    <para>Converts the specified value object to an enumeration object.</para>
        /// </summary>
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is string)
            {
                foreach (Type t in _types)
                {
                    if (value.Equals(t.FullName))
                    {
                        return t;
                    }
                }
            }

            return base.ConvertFrom(context, culture, value);
        }

        /// <internalonly/>
        /// <summary>
        ///    <para>Converts the given value object to the specified destination type.</para>
        /// </summary>
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == null)
            {
                throw new ArgumentNullException(nameof(destinationType));
            }

            if (destinationType == typeof(string))
            {
                if (value == null)
                {
                    return SR.none;
                }
                else
                {
                    return ((Type)value).FullName;
                }
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }
        /// <internalonly/>
        /// <summary>
        ///    <para>Gets a collection of standard values for the data type this validator is designed for.</para>
        /// </summary>
        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            if (_values == null)
            {
                object[] objTypes;

                if (_types != null)
                {
                    objTypes = new object[_types.Length];
                    Array.Copy(_types, objTypes, _types.Length);
                }
                else
                {
                    objTypes = null;
                }

                _values = new StandardValuesCollection(objTypes);
            }
            return _values;
        }

        /// <internalonly/>
        /// <summary>
        ///    <para>Gets a value indicating whether the list of standard values returned from
        ///    <see cref='System.ComponentModel.TypeListConverter.GetStandardValues'/> is an exclusive list. </para>
        /// </summary>
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
        {
            return true;
        }

        /// <internalonly/>
        /// <summary>
        ///    <para>
        ///        Gets a value indicating whether this object supports a standard set of values that can be
        ///        picked from a list using the specified context.
        ///    </para>
        /// </summary>
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return true;
        }
    }
}
