// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml.Serialization;

namespace System.Configuration
{
    public class SettingsPropertyValue
    {
        private object _value = null;
        private object _serializedValue = null;
        private bool _changedSinceLastSerialized = false;

        public string Name => Property.Name;
        public bool IsDirty { get; set; }
        public SettingsProperty Property { get; private set; }
        public bool UsingDefaultValue { get; private set; }
        public bool Deserialized { get; set; }

        public SettingsPropertyValue(SettingsProperty property)
        {
            Property = property;
        }

        public object PropertyValue
        {
            get
            {
                if (!Deserialized)
                {
                    _value = Deserialize();
                    Deserialized = true;
                }

                if (_value != null && !Property.PropertyType.IsPrimitive && !(_value is string) && !(_value is DateTime))
                {
                    UsingDefaultValue = false;
                    _changedSinceLastSerialized = true;
                    IsDirty = true;
                }

                return _value;
            }
            set
            {
                _value = value;
                IsDirty = true;
                _changedSinceLastSerialized = true;
                Deserialized = true;
                UsingDefaultValue = false;
            }
        }

        public object SerializedValue
        {
            get
            {
                if (_changedSinceLastSerialized)
                {
                    _changedSinceLastSerialized = false;
                    _serializedValue = SerializePropertyValue();
                }
                return _serializedValue;
            }
            set
            {
                UsingDefaultValue = false;
                _serializedValue = value;
            }
        }

        private bool IsHostedInAspnet()
        {
            // See System.Web.Hosting.ApplicationManager::PopulateDomainBindings
            return AppDomain.CurrentDomain.GetData(".appDomain") != null;
        }

        private object Deserialize()
        {
            object value = null;

            // Attempt 1: Try creating from SerializedValue
            if (SerializedValue != null)
            {
                try
                {
                    if (SerializedValue is string)
                    {
                        value = GetObjectFromString(Property.PropertyType, Property.SerializeAs, (string)SerializedValue);
                    }
                    else
                    {
                        using (MemoryStream ms = new MemoryStream((byte[])SerializedValue))
                        {
                            value = (new BinaryFormatter()).Deserialize(ms);
                        }
                    }
                }
                catch (Exception exception)
                {
                    try
                    {
                        if (IsHostedInAspnet())
                        {
                            object[] args = new object[] { Property, this, exception };

                            const string webBaseEventTypeName = "System.Web.Management.WebBaseEvent, System.Web";
                            Type type = Type.GetType(webBaseEventTypeName, true);

                            type.InvokeMember("RaisePropertyDeserializationWebErrorEvent",
                                BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.InvokeMethod,
                                null, null, args, CultureInfo.InvariantCulture);
                        }
                    }
                    catch
                    {
                    }
                }

                if (value != null && !Property.PropertyType.IsAssignableFrom(value.GetType())) // is it the correct type
                    value = null;
            }

            // Attempt 2: Try creating from default value
            if (value == null)
            {
                UsingDefaultValue = true;
                if (Property.DefaultValue == null || Property.DefaultValue.ToString() == "[null]")
                {
                    if (Property.PropertyType.IsValueType)
                        return TypeUtil.CreateInstance(Property.PropertyType);
                    else
                        return null;
                }
                if (!(Property.DefaultValue is string))
                {
                    value = Property.DefaultValue;
                }
                else
                {
                    try
                    {
                        value = GetObjectFromString(Property.PropertyType, Property.SerializeAs, (string)Property.DefaultValue);
                    }
                    catch (Exception e)
                    {
                        throw new ArgumentException(SR.Format(SR.Could_not_create_from_default_value, Property.Name, e.Message));
                    }
                }

                if (value != null && !Property.PropertyType.IsAssignableFrom(value.GetType())) // is it the correct type
                    throw new ArgumentException(SR.Format(SR.Could_not_create_from_default_value_2, Property.Name));
            }

            // Attempt 3: Create via the parameterless constructor
            if (value == null)
            {
                if (Property.PropertyType == typeof(string))
                {
                    value = string.Empty;
                }
                else
                {
                    try
                    {
                        value = TypeUtil.CreateInstance(Property.PropertyType);
                    }
                    catch { }
                }
            }

            return value;
        }

        private static object GetObjectFromString(Type type, SettingsSerializeAs serializeAs, string serializedValue)
        {
            // Deal with string types
            if (type == typeof(string) && (serializedValue == null || serializedValue.Length < 1 || serializeAs == SettingsSerializeAs.String))
                return serializedValue;

            // Return null if there is nothing to convert
            if (serializedValue == null || serializedValue.Length < 1)
                return null;

            // Convert based on the serialized type
            switch (serializeAs)
            {
                case SettingsSerializeAs.Binary:
                    byte[] buffer = Convert.FromBase64String(serializedValue);
                    using (MemoryStream ms = new MemoryStream(buffer))
                    {
                        return (new BinaryFormatter()).Deserialize(ms);
                    }
                case SettingsSerializeAs.Xml:
                    StringReader sr = new StringReader(serializedValue);
                    XmlSerializer xs = new XmlSerializer(type);
                    return xs.Deserialize(sr);
                case SettingsSerializeAs.String:
                    TypeConverter converter = TypeDescriptor.GetConverter(type);
                    if (converter != null && converter.CanConvertTo(typeof(string)) && converter.CanConvertFrom(typeof(string)))
                        return converter.ConvertFromInvariantString(serializedValue);
                    throw new ArgumentException(SR.Format(SR.Unable_to_convert_type_from_string, type), nameof(type));
                default:
                    return null;
            }
        }

        private object SerializePropertyValue()
        {
            if (_value == null)
                return null;

            if (Property.SerializeAs != SettingsSerializeAs.Binary)
                return ConvertObjectToString(_value, Property.PropertyType, Property.SerializeAs, Property.ThrowOnErrorSerializing);

            using (MemoryStream ms = new MemoryStream())
            {
                BinaryFormatter bf = new BinaryFormatter();
                bf.Serialize(ms, _value);
                return ms.ToArray();
            }
        }

        private static string ConvertObjectToString(object propertyValue, Type type, SettingsSerializeAs serializeAs, bool throwOnError)
        {
            if (serializeAs == SettingsSerializeAs.ProviderSpecific)
            {
                if (type == typeof(string) || type.IsPrimitive)
                    serializeAs = SettingsSerializeAs.String;
                else
                    serializeAs = SettingsSerializeAs.Xml;
            }

            try
            {
                switch (serializeAs)
                {
                    case SettingsSerializeAs.String:
                        TypeConverter converter = TypeDescriptor.GetConverter(type);
                        if (converter != null && converter.CanConvertTo(typeof(string)) && converter.CanConvertFrom(typeof(string)))
                            return converter.ConvertToInvariantString(propertyValue);
                        throw new ArgumentException(SR.Format(SR.Unable_to_convert_type_to_string, type), nameof(type));
                    case SettingsSerializeAs.Xml:
                        XmlSerializer xs = new XmlSerializer(type);
                        StringWriter sw = new StringWriter(CultureInfo.InvariantCulture);

                        xs.Serialize(sw, propertyValue);
                        return sw.ToString();
                    case SettingsSerializeAs.Binary:
                        Debug.Fail("Should not have gotten here with Binary formatting");
                        break;
                }
            }
            catch (Exception)
            {
                if (throwOnError)
                    throw;
            }
            return null;
        }
    }
}
