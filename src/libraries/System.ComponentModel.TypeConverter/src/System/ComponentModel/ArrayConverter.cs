// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace System.ComponentModel
{
    /// <summary>
    /// Provides a type converter to convert <see cref='System.Array'/>
    /// objects to and from various other representations.
    /// </summary>
    public class ArrayConverter : CollectionConverter
    {
        /// <summary>
        /// Converts the given value object to the specified destination type.
        /// </summary>
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string) && value is Array)
            {
                return SR.Format(SR.Array, value.GetType().Name);
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }

        /// <summary>
        /// Gets a collection of properties for the type of array specified by the value parameter.
        /// </summary>
        [RequiresUnreferencedCode("The Type of value cannot be statically discovered. " + AttributeCollection.FilterRequiresUnreferencedCodeMessage)]
        public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext context, object value, Attribute[] attributes)
        {
            if (value == null)
            {
                return null;
            }
            if (!(value is Array valueArray))
            {
                return new PropertyDescriptorCollection(null);
            }

            int length = valueArray.GetLength(0);
            PropertyDescriptor[] props = new PropertyDescriptor[length];

            Type arrayType = value.GetType();
            Type elementType = arrayType.GetElementType();

            for (int i = 0; i < length; i++)
            {
                props[i] = new ArrayPropertyDescriptor(arrayType, elementType, i);
            }

            return new PropertyDescriptorCollection(props);
        }

        /// <summary>
        /// Gets a value indicating whether this object supports properties.
        /// </summary>
        public override bool GetPropertiesSupported(ITypeDescriptorContext context) => true;

        private sealed class ArrayPropertyDescriptor : SimplePropertyDescriptor
        {
            private readonly int _index;

            public ArrayPropertyDescriptor(Type arrayType, Type elementType, int index)
                : base(arrayType, "[" + index + "]", elementType, null)
            {
                _index = index;
            }

            public override object GetValue(object instance)
            {
                if (instance is Array array && array.GetLength(0) > _index)
                {
                    return array.GetValue(_index);
                }

                return null;
            }

            public override void SetValue(object instance, object value)
            {
                if (instance is Array array)
                {
                    if (array.GetLength(0) > _index)
                    {
                        array.SetValue(value, _index);
                    }

                    OnValueChanged(instance, EventArgs.Empty);
                }
            }
        }
    }
}
