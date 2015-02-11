// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
        private Type nullableType;
        private Type simpleType;
        private TypeConverter simpleTypeConverter;

        /// <devdoc>
        /// Nullable converter is initialized with the underlying simple type.
        /// </devdoc>
        public NullableConverter(Type type)
        {
            this.nullableType = type;

            this.simpleType = Nullable.GetUnderlyingType(type);
            if (this.simpleType == null)
            {
                throw new ArgumentException(SR.NullableConverterBadCtorArg, "type");
            }

            this.simpleTypeConverter = TypeDescriptor.GetConverter(this.simpleType);
        }

        /// <devdoc>
        ///    <para>Gets a value indicating whether this converter can convert an object in the
        ///       given source type to the underlying simple type or a null.</para>
        /// </devdoc>
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType == this.simpleType)
            {
                return true;
            }
            else if (this.simpleTypeConverter != null)
            {
                return this.simpleTypeConverter.CanConvertFrom(context, sourceType);
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
            if (value == null || value.GetType() == this.simpleType)
            {
                return value;
            }
            else if (value is String && String.IsNullOrEmpty(value as String))
            {
                return null;
            }
            else if (this.simpleTypeConverter != null)
            {
                return this.simpleTypeConverter.ConvertFrom(context, culture, value);
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
            if (destinationType == this.simpleType)
            {
                return true;
            }
            else if (this.simpleTypeConverter != null)
            {
                return this.simpleTypeConverter.CanConvertTo(context, destinationType);
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
                throw new ArgumentNullException("destinationType");
            }

            if (destinationType == this.simpleType && value != null && this.nullableType.GetTypeInfo().IsAssignableFrom(value.GetType().GetTypeInfo()))
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
            else if (this.simpleTypeConverter != null)
            {
                return this.simpleTypeConverter.ConvertTo(context, culture, value, destinationType);
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
                return this.nullableType;
            }
        }

        /// <devdoc>
        /// The simple type that is represented as a nullable.
        /// </devdoc>
        public Type UnderlyingType
        {
            get
            {
                return this.simpleType;
            }
        }

        /// <devdoc>
        /// Converter associated with the underlying simple type.
        /// </devdoc>
        public TypeConverter UnderlyingTypeConverter
        {
            get
            {
                return this.simpleTypeConverter;
            }
        }
    }
}
