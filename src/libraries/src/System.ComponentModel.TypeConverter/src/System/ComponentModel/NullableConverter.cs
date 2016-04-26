// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Globalization;
using System.Reflection;

namespace System.ComponentModel
{
    /// <devdoc>
    /// TypeConverter to convert Nullable types to and from strings or the underlying simple type.
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

        /// <include file='doc\NullableConverter.uex' path='docs/doc[@for="NullableConverter.CreateInstance"]/*' />
        /// <devdoc>
        /// </devdoc>
        public override object CreateInstance(ITypeDescriptorContext context, IDictionary propertyValues)
        {
            if (_simpleTypeConverter != null)
            {
                object instance = _simpleTypeConverter.CreateInstance(context, propertyValues);
                return instance;
            }

            return base.CreateInstance(context, propertyValues);
        }

        /// <devdoc>
        ///    <para>
        ///        Gets a value indicating whether changing a value on this object requires a call to
        ///        <see cref='System.ComponentModel.TypeConverter.CreateInstance'/> to create a new value,
        ///        using the specified context.
        ///    </para>
        /// </devdoc>
        public override bool GetCreateInstanceSupported(ITypeDescriptorContext context)
        {
            if (_simpleTypeConverter != null)
            {
                return _simpleTypeConverter.GetCreateInstanceSupported(context);
            }

            return base.GetCreateInstanceSupported(context);
        }

#if !NETSTANDARD10
        /// <devdoc>
        ///    <para>
        ///        Gets a collection of properties for the type of array specified by the value
        ///        parameter using the specified context and attributes.
        ///    </para>
        /// </devdoc>
        public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext context, object value, Attribute[] attributes)
        {
            if (_simpleTypeConverter != null)
            {
                object unwrappedValue = value;
                return _simpleTypeConverter.GetProperties(context, unwrappedValue, attributes);
            }

            return base.GetProperties(context, value, attributes);
        }
#endif // !NETSTANDARD10

        /// <devdoc>
        ///    <para>Gets a value indicating whether this object supports properties using the specified context.</para>
        /// </devdoc>
        public override bool GetPropertiesSupported(ITypeDescriptorContext context)
        {
            if (_simpleTypeConverter != null)
            {
                return _simpleTypeConverter.GetPropertiesSupported(context);
            }

            return base.GetPropertiesSupported(context);
        }

        /// <devdoc>
        ///    <para>Gets a collection of standard values for the data type this type converter is designed for.</para>
        /// </devdoc>
        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            if (_simpleTypeConverter != null)
            {
                StandardValuesCollection values = _simpleTypeConverter.GetStandardValues(context);
                if (GetStandardValuesSupported(context) && values != null)
                {
                    // Create a set of standard values around nullable instances.  
                    object[] wrappedValues = new object[values.Count + 1];
                    int idx = 0;

                    wrappedValues[idx++] = null;
                    foreach (object value in values)
                    {
                        wrappedValues[idx++] = value;
                    }

                    return new StandardValuesCollection(wrappedValues);
                }
            }

            return base.GetStandardValues(context);
        }

        /// <devdoc>
        ///    <para>
        ///        Gets a value indicating whether the collection of standard values returned from
        ///        <see cref='System.ComponentModel.TypeConverter.GetStandardValues'/> is an exclusive 
        ///        list of possible values, using the specified context.
        ///    </para>
        /// </devdoc>
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
        {
            if (_simpleTypeConverter != null)
            {
                return _simpleTypeConverter.GetStandardValuesExclusive(context);
            }

            return base.GetStandardValuesExclusive(context);
        }

        /// <devdoc>
        ///    <para>
        ///        Gets a value indicating whether this object supports a standard set of values that can
        ///        be picked from a list using the specified context.
        ///    </para>
        /// </devdoc>
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            if (_simpleTypeConverter != null)
            {
                return _simpleTypeConverter.GetStandardValuesSupported(context);
            }

            return base.GetStandardValuesSupported(context);
        }

        /// <devdoc>
        ///    <para>Gets a value indicating whether the given value object is valid for this type.</para>
        /// </devdoc>
        public override bool IsValid(ITypeDescriptorContext context, object value)
        {
            if (_simpleTypeConverter != null)
            {
                object unwrappedValue = value;
                if (unwrappedValue == null)
                {
                    return true; // null is valid for nullable.
                }
                else
                {
                    return _simpleTypeConverter.IsValid(context, unwrappedValue);
                }
            }

            return base.IsValid(context, value);
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
