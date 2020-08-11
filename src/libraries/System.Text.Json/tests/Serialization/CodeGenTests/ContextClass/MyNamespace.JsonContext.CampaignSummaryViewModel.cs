// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Code-gen'd

using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Text.Json.Serialization.Tests;

namespace MyNamespace
{
    public partial class JsonContext : JsonSerializerContext
    {
        private CampaignSummaryViewModelTypeInfo _typeInfo;
        public JsonTypeInfo<CampaignSummaryViewModel> CampaignSummaryViewModel
        {
            get
            {
                if (_typeInfo == null)
                {
                    _typeInfo = new CampaignSummaryViewModelTypeInfo(this);
                }

                return _typeInfo.TypeInfo;
            }
        }

        private class CampaignSummaryViewModelTypeInfo
        {
            public JsonTypeInfo<CampaignSummaryViewModel> TypeInfo { get; private set; }

            private JsonPropertyInfo<int> _property_Id;
            private JsonPropertyInfo<string> _property_Title;
            private JsonPropertyInfo<string> _property_Description;
            private JsonPropertyInfo<string> _property_ImageUrl;
            private JsonPropertyInfo<string> _property_OrganizationName;
            private JsonPropertyInfo<string> _property_Headline;

            public CampaignSummaryViewModelTypeInfo(JsonContext context)
            {
                var typeInfo = new JsonObjectInfo<CampaignSummaryViewModel>(CreateObjectFunc, SerializeFunc, DeserializeFunc, context.GetOptions());

                _property_Id = typeInfo.AddProperty(nameof(System.Text.Json.Serialization.Tests.CampaignSummaryViewModel.Id),
                    (obj) => { return ((CampaignSummaryViewModel)obj).Id; },
                    (obj, value) => { ((CampaignSummaryViewModel)obj).Id = value; },
                    context.Int32);

                _property_Title = typeInfo.AddProperty(nameof(System.Text.Json.Serialization.Tests.CampaignSummaryViewModel.Title),
                    (obj) => { return ((CampaignSummaryViewModel)obj).Title; },
                    (obj, value) => { ((CampaignSummaryViewModel)obj).Title = value; },
                    context.String);

                _property_Description = typeInfo.AddProperty(nameof(System.Text.Json.Serialization.Tests.CampaignSummaryViewModel.Description),
                    (obj) => { return ((CampaignSummaryViewModel)obj).Description; },
                    (obj, value) => { ((CampaignSummaryViewModel)obj).Description = value; },
                    context.String);

                _property_ImageUrl = typeInfo.AddProperty(nameof(System.Text.Json.Serialization.Tests.CampaignSummaryViewModel.ImageUrl),
                    (obj) => { return ((CampaignSummaryViewModel)obj).ImageUrl; },
                    (obj, value) => { ((CampaignSummaryViewModel)obj).ImageUrl = value; },
                    context.String);

                _property_OrganizationName = typeInfo.AddProperty(nameof(System.Text.Json.Serialization.Tests.CampaignSummaryViewModel.OrganizationName),
                    (obj) => { return ((CampaignSummaryViewModel)obj).OrganizationName; },
                    (obj, value) => { ((CampaignSummaryViewModel)obj).OrganizationName = value; },
                    context.String);

                _property_Headline = typeInfo.AddProperty(nameof(System.Text.Json.Serialization.Tests.CampaignSummaryViewModel.Headline),
                    (obj) => { return ((CampaignSummaryViewModel)obj).Headline; },
                    (obj, value) => { ((CampaignSummaryViewModel)obj).Headline = value; },
                    context.String);

                typeInfo.CompleteInitialization();
                TypeInfo = typeInfo;
            }

            private object CreateObjectFunc()
            {
                return new CampaignSummaryViewModel();
            }

            private void SerializeFunc(Utf8JsonWriter writer, object value, ref WriteStack writeStack, JsonSerializerOptions options)
            {
                CampaignSummaryViewModel obj = (CampaignSummaryViewModel)value;

                _property_Id.WriteValue(obj.Id, ref writeStack, writer);
                _property_Title.WriteValue(obj.Title, ref writeStack, writer);
                _property_Description.WriteValue(obj.Description, ref writeStack, writer);
                _property_ImageUrl.WriteValue(obj.ImageUrl, ref writeStack, writer);
                _property_OrganizationName.WriteValue(obj.OrganizationName, ref writeStack, writer);
                _property_Headline.WriteValue(obj.Headline, ref writeStack, writer);
            }

            private CampaignSummaryViewModel DeserializeFunc(ref Utf8JsonReader reader, ref ReadStack readStack, JsonSerializerOptions options)
            {
                bool ReadPropertyName(ref Utf8JsonReader reader)
                {
                    return reader.Read() && reader.TokenType == JsonTokenType.PropertyName;
                }

                ReadOnlySpan<byte> propertyName;
                CampaignSummaryViewModel obj = new CampaignSummaryViewModel();

                if (!ReadPropertyName(ref reader)) goto Done;
                propertyName = reader.ValueSpan;
                if (propertyName.SequenceEqual(_property_Id.NameAsUtf8Bytes))
                {
                    reader.Read();
                    _property_Id.ReadValueAndSetMember(ref reader, ref readStack, obj);
                    if (!ReadPropertyName(ref reader)) goto Done;
                    propertyName = reader.ValueSpan;
                }

                if (propertyName.SequenceEqual(_property_Title.NameAsUtf8Bytes))
                {
                    reader.Read();
                    _property_Title.ReadValueAndSetMember(ref reader, ref readStack, obj);
                    if (!ReadPropertyName(ref reader)) goto Done;
                    propertyName = reader.ValueSpan;
                }

                if (propertyName.SequenceEqual(_property_Description.NameAsUtf8Bytes))
                {
                    reader.Read();
                    _property_Description.ReadValueAndSetMember(ref reader, ref readStack, obj);
                    if (!ReadPropertyName(ref reader)) goto Done;
                    propertyName = reader.ValueSpan;
                }

                if (propertyName.SequenceEqual(_property_ImageUrl.NameAsUtf8Bytes))
                {
                    reader.Read();
                    _property_ImageUrl.ReadValueAndSetMember(ref reader, ref readStack, obj);
                    if (!ReadPropertyName(ref reader)) goto Done;
                    propertyName = reader.ValueSpan;
                }

                if (propertyName.SequenceEqual(_property_OrganizationName.NameAsUtf8Bytes))
                {
                    reader.Read();
                    _property_OrganizationName.ReadValueAndSetMember(ref reader, ref readStack, obj);
                    if (!ReadPropertyName(ref reader)) goto Done;
                    propertyName = reader.ValueSpan;
                }

                if (propertyName.SequenceEqual(_property_Headline.NameAsUtf8Bytes))
                {
                    reader.Read();
                    _property_Headline.ReadValueAndSetMember(ref reader, ref readStack, obj);
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
