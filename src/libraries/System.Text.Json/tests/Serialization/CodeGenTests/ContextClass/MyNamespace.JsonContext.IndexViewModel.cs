// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Code-gen'd

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Text.Json.Serialization.Tests;

namespace MyNamespace
{
    public partial class JsonContext : JsonSerializerContext
    {
        private IndexViewModelTypeInfo _indexViewModel;
        public JsonTypeInfo<IndexViewModel> IndexViewModel
        {
            get
            {
                if (_indexViewModel == null)
                {
                    _indexViewModel = new IndexViewModelTypeInfo(this);
                }

                return _indexViewModel.TypeInfo;
            }
        }

        private class IndexViewModelTypeInfo
        {
            public JsonTypeInfo<IndexViewModel> TypeInfo { get; private set; }

            private JsonPropertyInfo<List<ActiveOrUpcomingEvent>> _property_ActiveOrUpcomingEvents;
            private JsonPropertyInfo<CampaignSummaryViewModel> _property_FeaturedCampaign;
            private JsonPropertyInfo<bool> _property_IsNewAccount;

            public IndexViewModelTypeInfo(JsonContext context)
            {
                var typeInfo = new JsonObjectInfo<IndexViewModel>(CreateObjectFunc, SerializeFunc, DeserializeFunc, context.GetOptions());

                _property_ActiveOrUpcomingEvents = typeInfo.AddProperty(nameof(System.Text.Json.Serialization.Tests.IndexViewModel.ActiveOrUpcomingEvents),
                    (obj) => { return ((IndexViewModel)obj).ActiveOrUpcomingEvents; },
                    (obj, value) => { ((IndexViewModel)obj).ActiveOrUpcomingEvents = value; },
                    KnownCollectionTypeInfos<ActiveOrUpcomingEvent>.GetList(context.ActiveOrUpcomingEvent, context));

                _property_FeaturedCampaign = typeInfo.AddProperty(nameof(System.Text.Json.Serialization.Tests.IndexViewModel.FeaturedCampaign),
                    (obj) => { return ((IndexViewModel)obj).FeaturedCampaign; },
                    (obj, value) => { ((IndexViewModel)obj).FeaturedCampaign = value; },
                    context.CampaignSummaryViewModel);

                _property_IsNewAccount = typeInfo.AddProperty(nameof(System.Text.Json.Serialization.Tests.IndexViewModel.IsNewAccount),
                    (obj) => { return ((IndexViewModel)obj).IsNewAccount; },
                    (obj, value) => { ((IndexViewModel)obj).IsNewAccount = value; },
                    context.Boolean);

                typeInfo.CompleteInitialization();
                TypeInfo = typeInfo;
            }

            private object CreateObjectFunc()
            {
                return new IndexViewModel();
            }

            private void SerializeFunc(Utf8JsonWriter writer, object value, ref WriteStack writeStack, JsonSerializerOptions options)
            {
                IndexViewModel obj = (IndexViewModel)value;

                _property_ActiveOrUpcomingEvents.Write(obj.ActiveOrUpcomingEvents, ref writeStack, writer);
                _property_FeaturedCampaign.Write(obj.FeaturedCampaign, ref writeStack, writer);
                _property_IsNewAccount.WriteValue(obj.IsNewAccount, ref writeStack, writer);
            }

            private IndexViewModel DeserializeFunc(ref Utf8JsonReader reader, ref ReadStack readStack, JsonSerializerOptions options)
            {
                bool ReadPropertyName(ref Utf8JsonReader reader)
                {
                    return reader.Read() && reader.TokenType == JsonTokenType.PropertyName;
                }

                ReadOnlySpan<byte> propertyName;
                IndexViewModel obj = new IndexViewModel();

                if (!ReadPropertyName(ref reader)) goto Done;
                propertyName = reader.ValueSpan;
                if (propertyName.SequenceEqual(_property_ActiveOrUpcomingEvents.NameAsUtf8Bytes))
                {
                    reader.Read();
                    _property_ActiveOrUpcomingEvents.ReadAndSetMember(ref reader, ref readStack, obj);
                    if (!ReadPropertyName(ref reader)) goto Done;
                    propertyName = reader.ValueSpan;
                }

                if (propertyName.SequenceEqual(_property_FeaturedCampaign.NameAsUtf8Bytes))
                {
                    reader.Read();
                    _property_FeaturedCampaign.ReadAndSetMember(ref reader, ref readStack, obj);
                    if (!ReadPropertyName(ref reader)) goto Done;
                    propertyName = reader.ValueSpan;
                }

                if (propertyName.SequenceEqual(_property_IsNewAccount.NameAsUtf8Bytes))
                {
                    reader.Read();
                    _property_IsNewAccount.ReadValueAndSetMember(ref reader, ref readStack, obj);
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
