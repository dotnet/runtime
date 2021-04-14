// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class KeyValuePairConverter<TKey, TValue> :
        SmallObjectWithParameterizedConstructorConverter<KeyValuePair<TKey, TValue>, TKey, TValue, object, object>
    {
        private const string KeyNameCLR = "Key";
        private const string ValueNameCLR = "Value";

        private const int NumProperties = 2;

        // Property name for "Key" and "Value" with Options.PropertyNamingPolicy applied.
        private string _keyName = null!;
        private string _valueName = null!;

        private static readonly ConstructorInfo s_constructorInfo =
            typeof(KeyValuePair<TKey, TValue>).GetConstructor(new[] { typeof(TKey), typeof(TValue) })!;

        internal override void Initialize(JsonSerializerOptions options)
        {
            JsonNamingPolicy? namingPolicy = options.PropertyNamingPolicy;
            if (namingPolicy == null)
            {
                _keyName = KeyNameCLR;
                _valueName = ValueNameCLR;
            }
            else
            {
                _keyName = namingPolicy.ConvertName(KeyNameCLR);
                _valueName = namingPolicy.ConvertName(ValueNameCLR);

                // Validation for the naming policy will occur during JsonPropertyInfo creation.
            }

            ConstructorInfo = s_constructorInfo;
            Debug.Assert(ConstructorInfo != null);
        }

        /// <summary>
        /// Lookup the constructor parameter given its name in the reader.
        /// </summary>
        protected override bool TryLookupConstructorParameter(
            ref ReadStack state,
            ref Utf8JsonReader reader,
            JsonSerializerOptions options,
            out JsonParameterInfo? jsonParameterInfo)
        {
            JsonTypeInfo typeInfo = state.Current.JsonTypeInfo;
            ArgumentState? argState = state.Current.CtorArgumentState;

            Debug.Assert(typeInfo.PropertyInfoForTypeInfo.ConverterStrategy == ConverterStrategy.Object);
            Debug.Assert(argState != null);
            Debug.Assert(_keyName != null);
            Debug.Assert(_valueName != null);

            bool caseInsensitiveMatch = options.PropertyNameCaseInsensitive;

            string propertyName = reader.GetString()!;
            state.Current.JsonPropertyNameAsString = propertyName;

            if (!argState.FoundKey &&
                FoundKeyProperty(propertyName, caseInsensitiveMatch))
            {
                jsonParameterInfo = typeInfo.ParameterCache![_keyName];
                argState.FoundKey = true;
            }
            else if (!argState.FoundValue &&
                FoundValueProperty(propertyName, caseInsensitiveMatch))
            {
                jsonParameterInfo = typeInfo.ParameterCache![_valueName];
                argState.FoundValue = true;
            }
            else
            {
                ThrowHelper.ThrowJsonException();
                jsonParameterInfo = null;
                return false;
            }

            Debug.Assert(jsonParameterInfo != null);
            argState.ParameterIndex++;
            argState.JsonParameterInfo = jsonParameterInfo;
            state.Current.NumberHandling = jsonParameterInfo.NumberHandling;
            return true;
        }

        protected override void EndRead(ref ReadStack state)
        {
            Debug.Assert(state.Current.PropertyIndex == 0);

            if (state.Current.CtorArgumentState!.ParameterIndex != NumProperties)
            {
                ThrowHelper.ThrowJsonException();
            }
        }

        private bool FoundKeyProperty(string propertyName, bool caseInsensitiveMatch)
        {
            return propertyName == _keyName ||
                (caseInsensitiveMatch && string.Equals(propertyName, _keyName, StringComparison.OrdinalIgnoreCase)) ||
                propertyName == KeyNameCLR;
        }

        private bool FoundValueProperty(string propertyName, bool caseInsensitiveMatch)
        {
            return propertyName == _valueName ||
                (caseInsensitiveMatch && string.Equals(propertyName, _valueName, StringComparison.OrdinalIgnoreCase)) ||
                propertyName == ValueNameCLR;
        }
    }
}
