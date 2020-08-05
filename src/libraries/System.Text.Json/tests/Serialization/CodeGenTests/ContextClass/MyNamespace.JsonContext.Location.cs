// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Code-gen'd

using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace MyNamespace
{
    public partial class JsonContext : JsonSerializerContext
    {
        private LocationTypeInfo _location;
        public JsonTypeInfo<Location> Location
        {
            get
            {
                if (_location == null)
                {
                    _location = new LocationTypeInfo(this);
                }

                return _location.TypeInfo;
            }
        }

        private class LocationTypeInfo
        {
            public JsonTypeInfo<Location> TypeInfo { get; private set; }

            private JsonPropertyInfo<int> _property_Id;
            private JsonPropertyInfo<string> _property_Address1;
            private JsonPropertyInfo<string> _property_Address2;
            private JsonPropertyInfo<string> _property_City;
            private JsonPropertyInfo<string> _property_State;
            private JsonPropertyInfo<string> _property_PostalCode;
            private JsonPropertyInfo<string> _property_Name;
            private JsonPropertyInfo<string> _property_PhoneNumber;
            private JsonPropertyInfo<string> _property_Country;

            public LocationTypeInfo(JsonContext context)
            {
                var typeInfo = new JsonObjectInfo<Location>(CreateObjectFunc, SerializeFunc, DeserializeFunc, context.GetOptions());

                _property_Id = typeInfo.AddProperty(nameof(MyNamespace.Location.Id),
                    (obj) => { return ((Location)obj).Id; },
                    (obj, value) => { ((Location)obj).Id = value; },
                    context.Int32);

                _property_Address1 = typeInfo.AddProperty(nameof(MyNamespace.Location.Address1),
                    (obj) => { return ((Location)obj).Address1; },
                    (obj, value) => { ((Location)obj).Address1 = value; },
                    context.String);

                _property_Address2 = typeInfo.AddProperty(nameof(MyNamespace.Location.Address2),
                    (obj) => { return ((Location)obj).Address2; },
                    (obj, value) => { ((Location)obj).Address2 = value; },
                    context.String);

                _property_City = typeInfo.AddProperty(nameof(MyNamespace.Location.City),
                    (obj) => { return ((Location)obj).City; },
                    (obj, value) => { ((Location)obj).City = value; },
                    context.String);

                _property_State = typeInfo.AddProperty(nameof(MyNamespace.Location.State),
                    (obj) => { return ((Location)obj).State; },
                    (obj, value) => { ((Location)obj).State = value; },
                    context.String);

                _property_PostalCode = typeInfo.AddProperty(nameof(MyNamespace.Location.PostalCode),
                    (obj) => { return ((Location)obj).PostalCode; },
                    (obj, value) => { ((Location)obj).PostalCode = value; },
                    context.String);

                _property_Name = typeInfo.AddProperty(nameof(MyNamespace.Location.Name),
                    (obj) => { return ((Location)obj).Name; },
                    (obj, value) => { ((Location)obj).Name = value; },
                    context.String);

                _property_PhoneNumber = typeInfo.AddProperty(nameof(MyNamespace.Location.PhoneNumber),
                    (obj) => { return ((Location)obj).PhoneNumber; },
                    (obj, value) => { ((Location)obj).PhoneNumber = value; },
                    context.String);

                _property_Country = typeInfo.AddProperty(nameof(MyNamespace.Location.Country),
                    (obj) => { return ((Location)obj).Country; },
                    (obj, value) => { ((Location)obj).Country = value; },
                    context.String);

                typeInfo.CompleteInitialization();
                TypeInfo = typeInfo;
            }

            private object CreateObjectFunc()
            {
                return new Location();
            }

            private void SerializeFunc(Utf8JsonWriter writer, object value, ref WriteStack writeStack, JsonSerializerOptions options)
            {
                Location obj = (Location)value;

                _property_Id.WriteValue(obj.Id, ref writeStack, writer);
                _property_Address1.WriteValue(obj.Address1, ref writeStack, writer);
                _property_Address2.WriteValue(obj.Address2, ref writeStack, writer);
                _property_City.WriteValue(obj.City, ref writeStack, writer);
                _property_State.WriteValue(obj.State, ref writeStack, writer);
                _property_PostalCode.WriteValue(obj.PostalCode, ref writeStack, writer);
                _property_Name.WriteValue(obj.Name, ref writeStack, writer);
                _property_PhoneNumber.WriteValue(obj.PhoneNumber, ref writeStack, writer);
                _property_Country.WriteValue(obj.Country, ref writeStack, writer);
            }

            private Location DeserializeFunc(ref Utf8JsonReader reader, ref ReadStack readStack, JsonSerializerOptions options)
            {
                bool ReadPropertyName(ref Utf8JsonReader reader)
                {
                    return reader.Read() && reader.TokenType == JsonTokenType.PropertyName;
                }

                ReadOnlySpan<byte> propertyName;
                Location obj = new Location();

                if (!ReadPropertyName(ref reader)) goto Done;
                propertyName = reader.ValueSpan;
                if (propertyName.SequenceEqual(_property_Id.NameAsUtf8Bytes))
                {
                    reader.Read();
                    _property_Id.ReadValueAndSetMember(ref reader, ref readStack, obj);
                    if (!ReadPropertyName(ref reader)) goto Done;
                    propertyName = reader.ValueSpan;
                }

                if (propertyName.SequenceEqual(_property_Address1.NameAsUtf8Bytes))
                {
                    reader.Read();
                    _property_Address1.ReadValueAndSetMember(ref reader, ref readStack, obj);
                    if (!ReadPropertyName(ref reader)) goto Done;
                    propertyName = reader.ValueSpan;
                }

                if (propertyName.SequenceEqual(_property_Address2.NameAsUtf8Bytes))
                {
                    reader.Read();
                    _property_Address2.ReadValueAndSetMember(ref reader, ref readStack, obj);
                    if (!ReadPropertyName(ref reader)) goto Done;
                    propertyName = reader.ValueSpan;
                }

                if (propertyName.SequenceEqual(_property_City.NameAsUtf8Bytes))
                {
                    reader.Read();
                    _property_City.ReadValueAndSetMember(ref reader, ref readStack, obj);
                    if (!ReadPropertyName(ref reader)) goto Done;
                    propertyName = reader.ValueSpan;
                }

                if (propertyName.SequenceEqual(_property_State.NameAsUtf8Bytes))
                {
                    reader.Read();
                    _property_State.ReadValueAndSetMember(ref reader, ref readStack, obj);
                    if (!ReadPropertyName(ref reader)) goto Done;
                    propertyName = reader.ValueSpan;
                }

                if (propertyName.SequenceEqual(_property_PostalCode.NameAsUtf8Bytes))
                {
                    reader.Read();
                    _property_PostalCode.ReadValueAndSetMember(ref reader, ref readStack, obj);
                    if (!ReadPropertyName(ref reader)) goto Done;
                    propertyName = reader.ValueSpan;
                }

                if (propertyName.SequenceEqual(_property_Name.NameAsUtf8Bytes))
                {
                    reader.Read();
                    _property_Name.ReadValueAndSetMember(ref reader, ref readStack, obj);
                    if (!ReadPropertyName(ref reader)) goto Done;
                    propertyName = reader.ValueSpan;
                }

                if (propertyName.SequenceEqual(_property_PhoneNumber.NameAsUtf8Bytes))
                {
                    reader.Read();
                    _property_PhoneNumber.ReadValueAndSetMember(ref reader, ref readStack, obj);
                    if (!ReadPropertyName(ref reader)) goto Done;
                    propertyName = reader.ValueSpan;
                }

                if (propertyName.SequenceEqual(_property_Country.NameAsUtf8Bytes))
                {
                    reader.Read();
                    _property_Country.ReadValueAndSetMember(ref reader, ref readStack, obj);
                    if (!ReadPropertyName(ref reader)) goto Done;
                    propertyName = reader.ValueSpan;
                }

                // todo:if all properties looped through, call the helper that finishes

                reader.Read();

            Done:
                if (reader.TokenType != JsonTokenType.EndObject)
                {
                    throw new JsonException("todo");
                }

                return obj;
            }
        }
    }
}
