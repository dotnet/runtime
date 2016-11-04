// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Serialization.Configuration
{
    using System;
    using System.Configuration;
    using System.ComponentModel;
    using System.Globalization;
    using System.Reflection;

    internal sealed class SchemaImporterExtensionElement
    {
        public SchemaImporterExtensionElement()
        {
        }

        public SchemaImporterExtensionElement(string name, string type) : this()
        {
        }

        public SchemaImporterExtensionElement(string name, Type type) : this()
        {
        }

        public string Name
        {
            get { return null; }
            set { }
        }

        [TypeConverter(typeof(TypeTypeConverter))]
        public Type Type
        {
            get { return null; }
            set { }
        }

        internal string Key
        {
            get { return this.Name; }
        }

        private class TypeAndName
        {
            public TypeAndName(string name)
            {
                this.type = Type.GetType(name, true, true);
                this.name = name;
            }

            public TypeAndName(Type type)
            {
                this.type = type;
            }

            public override int GetHashCode()
            {
                return type.GetHashCode();
            }

            public override bool Equals(object comparand)
            {
                return type.Equals(((TypeAndName)comparand).type);
            }

            public readonly Type type;
            public readonly string name;
        }

        private class TypeTypeConverter : TypeConverter
        {
            public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
            {
                if (sourceType == typeof(string))
                {
                    return true;
                }
                return base.CanConvertFrom(context, sourceType);
            }

            public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
            {
                if (value is string)
                {
                    return new TypeAndName((string)value);
                }

                return base.ConvertFrom(context, culture, value);
            }

            public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
            {
                if (destinationType == typeof(string))
                {
                    TypeAndName castedValue = (TypeAndName)value;
                    return castedValue.name == null ? castedValue.type.AssemblyQualifiedName : castedValue.name;
                }

                return base.ConvertTo(context, culture, value, destinationType);
            }
        }
    }
}

