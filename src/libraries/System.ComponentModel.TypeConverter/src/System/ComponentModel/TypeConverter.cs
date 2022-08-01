// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.ComponentModel.Design.Serialization;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace System.ComponentModel
{
    /// <summary>
    /// Converts the value of an object into a different data type.
    /// </summary>
    public class TypeConverter
    {
        internal const string RequiresUnreferencedCodeMessage = "Generic TypeConverters may require the generic types to be annotated. For example, NullableConverter requires the underlying type to be DynamicallyAccessedMembers All.";

        /// <summary>
        /// Gets a value indicating whether this converter can convert an object in the
        /// given source type to the native type of the converter.
        /// </summary>
        public bool CanConvertFrom(Type sourceType) => CanConvertFrom(null, sourceType);

        /// <summary>
        /// Gets a value indicating whether this converter can convert an object in the given
        /// source type to the native type of the converter using the context.
        /// </summary>
        public virtual bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
            => sourceType == typeof(InstanceDescriptor);

        /// <summary>
        /// Gets a value indicating whether this converter can convert an object to the given
        /// destination type using the context.
        /// </summary>
        public bool CanConvertTo([NotNullWhen(true)] Type? destinationType) => CanConvertTo(null, destinationType);

        /// <summary>
        /// Gets a value indicating whether this converter can convert an object to the given
        /// destination type using the context.
        /// </summary>
        public virtual bool CanConvertTo(ITypeDescriptorContext? context, [NotNullWhen(true)] Type? destinationType)
        {
            return destinationType == typeof(string);
        }

        /// <summary>
        /// Converts the given value to the converter's native type.
        /// </summary>
        public object? ConvertFrom(object value) => ConvertFrom(null, CultureInfo.CurrentCulture, value);

        /// <summary>
        /// Converts the given object to the converter's native type.
        /// </summary>
        public virtual object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
        {
            if (value is InstanceDescriptor instanceDescriptor)
            {
                return instanceDescriptor.Invoke();
            }
            throw GetConvertFromException(value);
        }

        /// <summary>
        /// Converts the given string to the converter's native type using the invariant culture.
        /// </summary>
        public object? ConvertFromInvariantString(string text)
        {
            return ConvertFromString(null, CultureInfo.InvariantCulture, text);
        }

        /// <summary>
        /// Converts the given string to the converter's native type using the invariant culture.
        /// </summary>
        public object? ConvertFromInvariantString(ITypeDescriptorContext? context, string text)
        {
            return ConvertFromString(context, CultureInfo.InvariantCulture, text);
        }

        /// <summary>
        /// Converts the specified text into an object.
        /// </summary>
        public object? ConvertFromString(string text) => ConvertFrom(null, null, text);

        /// <summary>
        /// Converts the specified text into an object.
        /// </summary>
        public object? ConvertFromString(ITypeDescriptorContext? context, string text)
        {
            return ConvertFrom(context, CultureInfo.CurrentCulture, text);
        }

        /// <summary>
        /// Converts the specified text into an object.
        /// </summary>
        public object? ConvertFromString(ITypeDescriptorContext? context, CultureInfo? culture, string text)
        {
            return ConvertFrom(context, culture, text);
        }

        /// <summary>
        /// Converts the given
        /// value object to the specified destination type using the arguments.
        /// </summary>
        public object? ConvertTo(object? value, Type destinationType)
        {
            return ConvertTo(null, null, value, destinationType);
        }

        /// <summary>
        /// Converts the given value object to
        /// the specified destination type using the specified context and arguments.
        /// </summary>
        public virtual object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
        {
            ArgumentNullException.ThrowIfNull(destinationType);

            if (destinationType == typeof(string))
            {
                if (value == null)
                {
                    return string.Empty;
                }

                if (culture != null && culture != CultureInfo.CurrentCulture)
                {
                    if (value is IFormattable formattable)
                    {
                        return formattable.ToString(format: null, formatProvider: culture);
                    }
                }
                return value.ToString();
            }
            throw GetConvertToException(value, destinationType);
        }

        /// <summary>
        /// Converts the specified value to a culture-invariant string representation.
        /// </summary>
        public string? ConvertToInvariantString(object? value)
        {
            return ConvertToString(null, CultureInfo.InvariantCulture, value);
        }

        /// <summary>
        /// Converts the specified value to a culture-invariant string representation.
        /// </summary>
        public string? ConvertToInvariantString(ITypeDescriptorContext? context, object? value)
        {
            return ConvertToString(context, CultureInfo.InvariantCulture, value);
        }

        /// <summary>
        /// Converts the specified value to a string representation.
        /// </summary>
        public string? ConvertToString(object? value)
        {
            return (string?)ConvertTo(null, CultureInfo.CurrentCulture, value, typeof(string));
        }

        /// <summary>
        /// Converts the specified value to a string representation.
        /// </summary>
        public string? ConvertToString(ITypeDescriptorContext? context, object? value)
        {
            return (string?)ConvertTo(context, CultureInfo.CurrentCulture, value, typeof(string));
        }

        /// <summary>
        /// Converts the specified value to a string representation.
        /// </summary>
        public string? ConvertToString(ITypeDescriptorContext? context, CultureInfo? culture, object? value)
        {
            return (string?)ConvertTo(context, culture, value, typeof(string));
        }

        /// <summary>
        /// Re-creates an <see cref='object'/> given a set of property values for the object.
        /// </summary>
        public object? CreateInstance(IDictionary propertyValues)
        {
            return CreateInstance(null, propertyValues);
        }

        /// <summary>
        /// Re-creates an <see cref='object'/> given a set of property values for the object.
        /// </summary>
        public virtual object? CreateInstance(ITypeDescriptorContext? context, IDictionary propertyValues) => null;

        /// <summary>
        /// Gets a suitable exception to throw when a conversion cannot be performed.
        /// </summary>
        protected Exception GetConvertFromException(object? value)
        {
            string? valueTypeName = value == null ? SR.GetResourceString(nameof(SR.Null), "(null)") : value.GetType().FullName;
            throw new NotSupportedException(SR.Format(SR.ConvertFromException, GetType().Name, valueTypeName));
        }

        /// <summary>
        /// Retrieves a suitable exception to throw when a conversion cannot
        /// be performed.
        /// </summary>
        protected Exception GetConvertToException(object? value, Type destinationType)
        {
            string? valueTypeName = value == null ? SR.GetResourceString(nameof(SR.Null), "(null)") : value.GetType().FullName;
            throw new NotSupportedException(SR.Format(SR.ConvertToException, GetType().Name, valueTypeName, destinationType.FullName));
        }

        /// <summary>
        /// Gets a value indicating whether changing a value on this object requires a call to
        /// <see cref='System.ComponentModel.TypeConverter.CreateInstance(IDictionary)'/> to create a new value.
        /// </summary>
        public bool GetCreateInstanceSupported() => GetCreateInstanceSupported(null);

        /// <summary>
        ///
        /// Gets a value indicating whether changing a value on this object requires a call to
        /// <see cref='System.ComponentModel.TypeConverter.CreateInstance(IDictionary)'/> to create a new value,
        /// using the specified context.
        ///
        /// </summary>
        public virtual bool GetCreateInstanceSupported(ITypeDescriptorContext? context) => false;

        /// <summary>
        /// Gets a collection of properties for the type of array specified by the value parameter.
        /// </summary>
        [RequiresUnreferencedCode("The Type of value cannot be statically discovered.")]
        public PropertyDescriptorCollection? GetProperties(object value) => GetProperties(null, value);

        /// <summary>
        ///
        /// Gets a collection of properties for the type of array specified by the value parameter using
        /// the specified context.
        ///
        /// </summary>
        [RequiresUnreferencedCode("The Type of value cannot be statically discovered.")]
        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor | DynamicallyAccessedMemberTypes.PublicFields, typeof(BrowsableAttribute))]
        public PropertyDescriptorCollection? GetProperties(ITypeDescriptorContext? context, object value)
        {
            return GetProperties(context, value, new Attribute[] { BrowsableAttribute.Yes });
        }

        /// <summary>
        ///
        /// Gets a collection of properties for the type of array specified by the value parameter using
        /// the specified context and attributes.
        ///
        /// </summary>
        [RequiresUnreferencedCode("The Type of value cannot be statically discovered. " + AttributeCollection.FilterRequiresUnreferencedCodeMessage)]
        public virtual PropertyDescriptorCollection? GetProperties(ITypeDescriptorContext? context, object value, Attribute[]? attributes)
        {
            return null;
        }

        /// <summary>
        /// Gets a value indicating whether this object supports properties.
        /// </summary>
        public bool GetPropertiesSupported() => GetPropertiesSupported(null);

        /// <summary>
        /// Gets a value indicating whether this object supports properties using the specified context.
        /// </summary>
        public virtual bool GetPropertiesSupported(ITypeDescriptorContext? context) => false;

        /// <summary>
        /// Gets a collection of standard values for the data type this type converter is designed for.
        /// </summary>
        public ICollection? GetStandardValues() => GetStandardValues(null);

        /// <summary>
        /// Gets a collection of standard values for the data type this type converter is designed for.
        /// </summary>
        public virtual StandardValuesCollection? GetStandardValues(ITypeDescriptorContext? context) => null;

        /// <summary>
        /// Gets a value indicating whether the collection of standard values returned from
        /// <see cref='System.ComponentModel.TypeConverter.GetStandardValues()'/> is an exclusive list.
        /// </summary>
        public bool GetStandardValuesExclusive() => GetStandardValuesExclusive(null);

        /// <summary>
        /// Gets a value indicating whether the collection of standard values returned from
        /// <see cref='System.ComponentModel.TypeConverter.GetStandardValues()'/> is an exclusive
        /// list of possible values, using the specified context.
        /// </summary>
        public virtual bool GetStandardValuesExclusive(ITypeDescriptorContext? context) => false;

        /// <summary>
        /// Gets a value indicating whether this object supports a standard set of values
        /// that can be picked from a list.
        /// </summary>
        public bool GetStandardValuesSupported() => GetStandardValuesSupported(null);

        /// <summary>
        /// Gets a value indicating whether this object supports a standard set of values that can be picked
        /// from a list using the specified context.
        /// </summary>
        public virtual bool GetStandardValuesSupported(ITypeDescriptorContext? context) => false;

        /// <summary>
        /// Gets a value indicating whether the given value object is valid for this type.
        /// </summary>
        public bool IsValid(object value) => IsValid(null, value);

        /// <summary>
        /// Gets a value indicating whether the given value object is valid for this type.
        /// </summary>
        public virtual bool IsValid(ITypeDescriptorContext? context, object value)
        {
            bool isValid = true;
            try
            {
                // Because null doesn't have a type, so we couldn't pass this to CanConvertFrom.
                // Meanwhile, we couldn't silence null value here, such as type converter like
                // NullableConverter would consider null value as a valid value.
                if (value == null || CanConvertFrom(context, value.GetType()))
                {
                    ConvertFrom(context, CultureInfo.InvariantCulture, value!);
                }
                else
                {
                    isValid = false;
                }
            }
            catch
            {
                isValid = false;
            }

            return isValid;
        }

        /// <summary>
        /// Sorts a collection of properties.
        /// </summary>
        protected PropertyDescriptorCollection SortProperties(PropertyDescriptorCollection props, string[] names)
        {
            props.Sort(names);
            return props;
        }

        /// <summary>
        /// An <see langword='abstract '/> class that provides properties for objects that do not have properties.
        /// </summary>
        protected abstract class SimplePropertyDescriptor : PropertyDescriptor
        {
            /// <summary>
            /// Initializes a new instance of the <see cref='System.ComponentModel.TypeConverter.SimplePropertyDescriptor'/> class.
            /// </summary>
            protected SimplePropertyDescriptor(Type componentType, string name, Type propertyType) : this(componentType, name, propertyType, Array.Empty<Attribute>())
            {
            }

            /// <summary>
            /// Initializes a new instance of the <see cref='System.ComponentModel.TypeConverter.SimplePropertyDescriptor'/> class.
            /// </summary>
            protected SimplePropertyDescriptor(Type componentType, string name, Type propertyType, Attribute[]? attributes) : base(name, attributes)
            {
                ComponentType = componentType;
                PropertyType = propertyType;
            }

            /// <summary>
            /// Gets the type of the component this property description is bound to.
            /// </summary>
            public override Type ComponentType { get; }

            /// <summary>
            /// Gets a value indicating whether this property is read-only.
            /// </summary>
            public override bool IsReadOnly
            {
                [DynamicDependency(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor | DynamicallyAccessedMemberTypes.PublicFields, typeof(ReadOnlyAttribute))]
                [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "The DynamicDependency ensures the correct members are preserved.")]
                get { return Attributes.Contains(ReadOnlyAttribute.Yes); }
            }

            /// <summary>
            /// Gets the type of the property.
            /// </summary>
            public override Type PropertyType { get; }

            /// <summary>
            /// Gets a value indicating whether resetting the component will change the value of the component.
            /// </summary>
            public override bool CanResetValue(object component)
            {
                DefaultValueAttribute? attr = (DefaultValueAttribute?)Attributes[typeof(DefaultValueAttribute)];
                if (attr == null)
                {
                    return false;
                }

                return attr.Value!.Equals(GetValue(component));
            }

            /// <summary>
            /// Resets the value for this property of the component.
            /// </summary>
            public override void ResetValue(object component)
            {
                DefaultValueAttribute? attr = (DefaultValueAttribute?)Attributes[typeof(DefaultValueAttribute)];
                if (attr != null)
                {
                    SetValue(component, attr.Value);
                }
            }

            /// <summary>
            /// Gets a value indicating whether the value of this property needs to be persisted.
            /// </summary>
            public override bool ShouldSerializeValue(object component) => false;
        }

        /// <summary>
        /// Represents a collection of values.
        /// </summary>
        public class StandardValuesCollection : ICollection
        {
            private readonly ICollection _values;
            private Array? _valueArray;

            /// <summary>
            ///
            /// Initializes a new instance of the <see cref='System.ComponentModel.TypeConverter.StandardValuesCollection'/> class.
            ///
            /// </summary>
            public StandardValuesCollection(ICollection? values)
            {
                values ??= Array.Empty<object>();

                if (values is Array a)
                {
                    _valueArray = a;
                }

                _values = values;
            }

            /// <summary>
            ///
            /// Gets the number of objects in the collection.
            ///
            /// </summary>
            public int Count
            {
                get
                {
                    if (_valueArray != null)
                    {
                        return _valueArray.Length;
                    }
                    else
                    {
                        return _values.Count;
                    }
                }
            }

            /// <summary>
            /// Gets the object at the specified index number.
            /// </summary>
            public object? this[int index]
            {
                get
                {
                    if (_valueArray != null)
                    {
                        return _valueArray.GetValue(index);
                    }
                    if (_values is IList list)
                    {
                        return list[index];
                    }
                    // No other choice but to enumerate the collection.
                    //
                    _valueArray = new object[_values.Count];
                    _values.CopyTo(_valueArray, 0);
                    return _valueArray.GetValue(index);
                }
            }

            /// <summary>
            /// Copies the contents of this collection to an array.
            /// </summary>
            public void CopyTo(Array array, int index) => _values.CopyTo(array, index);

            /// <summary>
            /// Gets an enumerator for this collection.
            /// </summary>
            public IEnumerator GetEnumerator() => _values.GetEnumerator();

            /// <summary>
            /// Determines if this collection is synchronized. The ValidatorCollection is not synchronized for
            /// speed. Also, since it is read-only, there is no need to synchronize it.
            /// </summary>
            bool ICollection.IsSynchronized => false;

            /// <summary>
            /// Retrieves the synchronization root for this collection. Because we are not synchronized,
            /// this returns null.
            /// </summary>
            object ICollection.SyncRoot => null!;
        }
    }
}
