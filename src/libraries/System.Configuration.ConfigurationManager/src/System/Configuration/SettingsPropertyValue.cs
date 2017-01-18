// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
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

        public string Name { get; private set; }
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
            object val = null;

            // Step 1: Try creating from Serailized value
            if (SerializedValue != null)
            {
                try
                {
                    if (SerializedValue is string)
                    {
                        val = GetObjectFromString(Property.PropertyType, Property.SerializeAs, (string)SerializedValue);
                    }
                    else
                    {
                        MemoryStream ms = new System.IO.MemoryStream((byte[])SerializedValue);
                        try
                        {
                            val = (new BinaryFormatter()).Deserialize(ms);
                        }
                        finally
                        {
                            ms.Close();
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

                if (val != null && !Property.PropertyType.IsAssignableFrom(val.GetType())) // is it the correct type
                    val = null;
            }

            // Step 2: Try creating from default value
            if (val == null)
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
                    val = Property.DefaultValue;
                }
                else
                {
                    try
                    {
                        val = GetObjectFromString(Property.PropertyType, Property.SerializeAs, (string)Property.DefaultValue);
                    }
                    catch (Exception e)
                    {
                        throw new ArgumentException(string.Format(SR.Could_not_create_from_default_value, Property.Name, e.Message));
                    }
                }
                if (val != null && !Property.PropertyType.IsAssignableFrom(val.GetType())) // is it the correct type
                    throw new ArgumentException(string.Format(SR.Could_not_create_from_default_value_2, Property.Name));
            }

            // Step 3: Create a new one by calling the parameterless constructor
            if (val == null)
            {
                if (Property.PropertyType == typeof(string))
                {
                    val = "";
                }
                else
                {
                    try
                    {
                        val = TypeUtil.CreateInstance(Property.PropertyType);
                    }
                    catch { }
                }
            }

            return val;
        }

        private static object GetObjectFromString(Type type, SettingsSerializeAs serializeAs, string attValue)
        {
            // Deal with string types
            if (type == typeof(string) && (attValue == null || attValue.Length < 1 || serializeAs == SettingsSerializeAs.String))
                return attValue;

            // Return null if there is nothing to convert
            if (attValue == null || attValue.Length < 1)
                return null;

            // Convert based on the serialized type
            switch (serializeAs)
            {
                case SettingsSerializeAs.Binary:
                    byte[] buf = Convert.FromBase64String(attValue);
                    MemoryStream ms = null;
                    try
                    {
                        ms = new MemoryStream(buf);
                        return (new BinaryFormatter()).Deserialize(ms);
                    }
                    finally
                    {
                        if (ms != null)
                            ms.Close();
                    }
                case SettingsSerializeAs.Xml:
                    StringReader sr = new StringReader(attValue);
                    XmlSerializer xs = new XmlSerializer(type);
                    return xs.Deserialize(sr);
                case SettingsSerializeAs.String:
                    TypeConverter converter = TypeDescriptor.GetConverter(type);
                    if (converter != null && converter.CanConvertTo(typeof(string)) && converter.CanConvertFrom(typeof(string)))
                        return converter.ConvertFromInvariantString(attValue);
                    throw new ArgumentException(string.Format(SR.Unable_to_convert_type_from_string, type.ToString()), nameof(type));
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

            MemoryStream ms = new System.IO.MemoryStream();
            try
            {
                BinaryFormatter bf = new BinaryFormatter();
                bf.Serialize(ms, _value);
                return ms.ToArray();
            }
            finally
            {
                ms.Close();
            }
        }


        private static string ConvertObjectToString(object propValue, Type type, SettingsSerializeAs serializeAs, bool throwOnError)
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
                            return converter.ConvertToInvariantString(propValue);
                        throw new ArgumentException(string.Format(SR.Unable_to_convert_type_to_string, type.ToString()), nameof(type));
                    case SettingsSerializeAs.Binary:
                        MemoryStream ms = new System.IO.MemoryStream();
                        try
                        {
                            BinaryFormatter bf = new BinaryFormatter();
                            bf.Serialize(ms, propValue);
                            byte[] buffer = ms.ToArray();
                            return Convert.ToBase64String(buffer);
                        }
                        finally
                        {
                            ms.Close();
                        }
                    case SettingsSerializeAs.Xml:
                        XmlSerializer xs = new XmlSerializer(type);
                        StringWriter sw = new StringWriter(CultureInfo.InvariantCulture);

                        xs.Serialize(sw, propValue);
                        return sw.ToString();
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
