// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Globalization;
using System.Reflection;

namespace System.ComponentModel
{
    /// <devdoc>
    /// TypeConverter to convert Nullable types to adn from strings or the underlying simple type.
    /// </devdoc>
    public class NullableConverter : TypeConverter
    {
        private Type _nullableType;
        private Type _simpleType;
        private TypeConverter _simpleTypeConverter;

        /// <devdoc>
        /// Nullable converter is initialized with the underlying simple type.
        /// </devdoc>
        public NullableConverter(Type type)
        {
            _nullableType = type;

            _simpleType = Nullable.GetUnderlyingType(type);
            if (_simpleType == null)
            {
                throw new ArgumentException(SR.NullableConverterBadCtorArg, nameof(type));
            }

            _simpleTypeConverter = TypeDescriptor.GetConverter(_simpleType);
        }

        /// <devdoc>
        ///    <para>Gets a value indicating whether this converter can convert an object in the
        ///       given source type to the underlying simple type or a null.</para>
        /// </devdoc>
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType == _simpleType)
            {
                return true;
            }
            else if (_simpleTypeConverter != null)
            {
                return _simpleTypeConverter.CanConvertFrom(context, sourceType);
            }
            else
            {
                return base.CanConvertFrom(context, sourceType);
            }
        }

        /// <devdoc>
        ///    Converts the given value to the converter's underlying simple type or a null.
        /// </devdoc>
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value == null || value.GetType() == _simpleType)
            {
                return value;
            }
            else if (value is string && string.IsNullOrEmpty(value as string))
            {
                return null;
            }
            else if (_simpleTypeConverter != null)
            {
                return _simpleTypeConverter.ConvertFrom(context, culture, value);
            }
            else
            {
                return base.ConvertFrom(context, culture, value);
            }
        }

        /// <devdoc>
        /// Gets a value indicating whether this converter can convert a value object to the destination type.
        /// </devdoc>
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            if (destinationType == _simpleType)
            {
                return true;
            }
            else if (_simpleTypeConverter != null)
            {
                return _simpleTypeConverter.CanConvertTo(context, destinationType);
            }
            else
            {
                return base.CanConvertTo(context, destinationType);
            }
        }

        /// <devdoc>
        /// Converts the given value object to the destination type.
        /// </devdoc>
        public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == null)
            {
                throw new ArgumentNullException(nameof(destinationType));
            }

            if (destinationType == _simpleType && value != null && _nullableType.GetTypeInfo().IsAssignableFrom(value.GetType().GetTypeInfo()))
            {
                return value;
            }
            else if (value == null)
            {
                // Handle our own nulls here
                if (destinationType == typeof(string))
                {
                    return string.Empty;
                }
            }
            else if (_simpleTypeConverter != null)
            {
                return _simpleTypeConverter.ConvertTo(context, culture, value, destinationType);
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }

        /// <devdoc>
        /// The type this converter was initialized with.
        /// </devdoc>
        public Type NullableType
        {
            get
            {
                return _nullableType;
            }
        }

        /// <devdoc>
        /// The simple type that is represented as a nullable.
        /// </devdoc>
        public Type UnderlyingType
        {
            get
            {
                return _simpleType;
            }
        }

        /// <devdoc>
        /// Converter associated with the underlying simple type.
        /// </devdoc>
        public TypeConverter UnderlyingTypeConverter
        {
            get
            {
                return _simpleTypeConverter;
            }
        }
    }
}
