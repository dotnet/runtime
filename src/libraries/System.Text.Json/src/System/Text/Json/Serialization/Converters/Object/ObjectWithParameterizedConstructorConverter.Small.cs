// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization.Converters
{
    /// <summary>
    /// Implementation of <cref>JsonObjectConverter{T}</cref> that supports the deserialization
    /// of JSON objects using parameterized constructors.
    /// </summary>
    internal class SmallObjectWithParameterizedConstructorConverter<T, TArg0, TArg1, TArg2, TArg3> : ObjectWithParameterizedConstructorConverter<T> where T : notnull
    {
        protected override object CreateObject(ref ReadStackFrame frame)
        {
            var createObject = (JsonTypeInfo.ParameterizedConstructorDelegate<T, TArg0, TArg1, TArg2, TArg3>)
                frame.JsonTypeInfo.CreateObjectWithArgs!;
            var arguments = (Arguments<TArg0, TArg1, TArg2, TArg3>)frame.CtorArgumentState!.Arguments;
            return createObject!(arguments.Arg0, arguments.Arg1, arguments.Arg2, arguments.Arg3);
        }

        protected override bool ReadAndCacheConstructorArgument(
            ref ReadStack state,
            ref Utf8JsonReader reader,
            JsonParameterInfo jsonParameterInfo)
        {
            Debug.Assert(state.Current.CtorArgumentState!.Arguments != null);
            var arguments = (Arguments<TArg0, TArg1, TArg2, TArg3>)state.Current.CtorArgumentState.Arguments;

            bool success;

            switch (jsonParameterInfo.Position)
            {
                case 0:
                    success = TryRead<TArg0>(ref state, ref reader, jsonParameterInfo, out arguments.Arg0);
                    break;
                case 1:
                    success = TryRead<TArg1>(ref state, ref reader, jsonParameterInfo, out arguments.Arg1);
                    break;
                case 2:
                    success = TryRead<TArg2>(ref state, ref reader, jsonParameterInfo, out arguments.Arg2);
                    break;
                case 3:
                    success = TryRead<TArg3>(ref state, ref reader, jsonParameterInfo, out arguments.Arg3);
                    break;
                default:
                    Debug.Fail("More than 4 params: we should be in override for LargeObjectWithParameterizedConstructorConverter.");
                    throw new InvalidOperationException();
            }

            return success;
        }

        private bool TryRead<TArg>(
            ref ReadStack state,
            ref Utf8JsonReader reader,
            JsonParameterInfo jsonParameterInfo,
            out TArg arg)
        {
            Debug.Assert(jsonParameterInfo.ShouldDeserialize);
            Debug.Assert(jsonParameterInfo.Options != null);

            var info = (JsonParameterInfo<TArg>)jsonParameterInfo;
            var converter = (JsonConverter<TArg>)jsonParameterInfo.ConverterBase;

            bool success = converter.TryRead(ref reader, info.RuntimePropertyType, info.Options!, ref state, out TArg? value);

            arg = value == null && jsonParameterInfo.IgnoreDefaultValuesOnRead
                ? (TArg?)info.DefaultValue! // Use default value specified on parameter, if any.
                : value!;

            return success;
        }

        protected override void InitializeConstructorArgumentCaches(ref ReadStack state, JsonSerializerOptions options)
        {
            JsonTypeInfo typeInfo = state.Current.JsonTypeInfo;

            if (typeInfo.CreateObjectWithArgs == null)
            {
                typeInfo.CreateObjectWithArgs =
                    options.MemberAccessorStrategy.CreateParameterizedConstructor<T, TArg0, TArg1, TArg2, TArg3>(ConstructorInfo!);
            }

            var arguments = new Arguments<TArg0, TArg1, TArg2, TArg3>();

            List<KeyValuePair<string, JsonParameterInfo?>> cache = typeInfo.ParameterCache!.List;
            for (int i = 0; i < typeInfo.ParameterCount; i++)
            {
                JsonParameterInfo? parameterInfo = cache[i].Value;
                Debug.Assert(parameterInfo != null);

                if (parameterInfo.ShouldDeserialize)
                {
                    int position = parameterInfo.Position;

                    switch (position)
                    {
                        case 0:
                            arguments.Arg0 = ((JsonParameterInfo<TArg0>)parameterInfo).TypedDefaultValue!;
                            break;
                        case 1:
                            arguments.Arg1 = ((JsonParameterInfo<TArg1>)parameterInfo).TypedDefaultValue!;
                            break;
                        case 2:
                            arguments.Arg2 = ((JsonParameterInfo<TArg2>)parameterInfo).TypedDefaultValue!;
                            break;
                        case 3:
                            arguments.Arg3 = ((JsonParameterInfo<TArg3>)parameterInfo).TypedDefaultValue!;
                            break;
                        default:
                            Debug.Fail("More than 4 params: we should be in override for LargeObjectWithParameterizedConstructorConverter.");
                            throw new InvalidOperationException();
                    }
                }
            }

            state.Current.CtorArgumentState!.Arguments = arguments;
        }
    }
}
