// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization.Converters
{
    /// <summary>
    /// Implementation of <cref>JsonObjectConverter{T}</cref> that supports the deserialization
    /// of JSON objects using parameterized constructors.
    /// </summary>
    internal class LargeObjectWithParameterizedConstructorConverter<T> : ObjectWithParameterizedConstructorConverter<T> where T : notnull
    {
        protected sealed override bool ReadAndCacheConstructorArgument(ref ReadStack state, ref Utf8JsonReader reader, JsonParameterInfo jsonParameterInfo)
        {
            Debug.Assert(jsonParameterInfo.ShouldDeserialize);
            Debug.Assert(jsonParameterInfo.Options != null);

            bool success = jsonParameterInfo.ConverterBase.TryReadAsObject(ref reader, jsonParameterInfo.Options!, ref state, out object? arg);

            if (success && !(arg == null && jsonParameterInfo.IgnoreDefaultValuesOnRead))
            {
                ((object[])state.Current.CtorArgumentState!.Arguments)[jsonParameterInfo.ClrInfo.Position] = arg!;
            }

            return success;
        }

        protected sealed override object CreateObject(ref ReadStackFrame frame)
        {
            object[] arguments = (object[])frame.CtorArgumentState!.Arguments;
            frame.CtorArgumentState.Arguments = null!;

            var createObject = (Func<object[], T>?)frame.JsonTypeInfo.CreateObjectWithArgs;

            if (createObject == null)
            {
                // This means this constructor has more than 64 parameters.
                ThrowHelper.ThrowNotSupportedException_ConstructorMaxOf64Parameters(TypeToConvert);
            }

            object obj = createObject(arguments);

            ArrayPool<object>.Shared.Return(arguments, clearArray: true);
            return obj;
        }

        protected sealed override void InitializeConstructorArgumentCaches(ref ReadStack state, JsonSerializerOptions options)
        {
            JsonTypeInfo typeInfo = state.Current.JsonTypeInfo;

            // Ensure property cache has been initialized.
            Debug.Assert(typeInfo.PropertyCache != null);

            if (typeInfo.ParameterCache == null)
            {
                typeInfo.InitializePropCache();
            }

            List<KeyValuePair<string, JsonParameterInfo?>> cache = typeInfo.ParameterCache!.List;
            object?[] arguments = ArrayPool<object>.Shared.Rent(cache.Count);

            for (int i = 0; i < typeInfo.ParameterCount; i++)
            {
                JsonParameterInfo? parameterInfo = cache[i].Value;
                Debug.Assert(parameterInfo != null);

                arguments[parameterInfo.ClrInfo.Position] = parameterInfo.ShouldDeserialize
                    ? parameterInfo.DefaultValue
                    : parameterInfo.ClrDefaultValue;
            }

            state.Current.CtorArgumentState!.Arguments = arguments;
        }
    }
}
