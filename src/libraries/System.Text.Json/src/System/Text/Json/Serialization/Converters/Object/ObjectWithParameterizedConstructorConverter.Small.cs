// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization.Converters
{
    /// <summary>
    /// Implementation of <cref>JsonObjectConverter{T}</cref> that supports the deserialization
    /// of JSON objects using parameterized constructors.
    /// </summary>
    internal sealed class SmallObjectWithParameterizedConstructorConverter<T, TArg0, TArg1, TArg2, TArg3> : ObjectWithParameterizedConstructorConverter<T> where T : notnull
    {
        protected override object CreateObject(ref ReadStackFrame frame)
        {
            var createObject = (JsonTypeInfo.ParameterizedConstructorDelegate<T, TArg0, TArg1, TArg2, TArg3>)
                frame.JsonTypeInfo.CreateObjectWithArgs!;
            var arguments = (Arguments<TArg0, TArg1, TArg2, TArg3>)frame.CtorArgumentState!.Arguments;
            return createObject!(arguments.Arg0, arguments.Arg1, arguments.Arg2, arguments.Arg3);
        }

        protected override bool ReadAndCacheConstructorArgument(
            scoped ref ReadStack state,
            ref Utf8JsonReader reader,
            JsonParameterInfo jsonParameterInfo)
        {
            Debug.Assert(state.Current.CtorArgumentState!.Arguments != null);
            var arguments = (Arguments<TArg0, TArg1, TArg2, TArg3>)state.Current.CtorArgumentState.Arguments;

            bool success;

            switch (jsonParameterInfo.Position)
            {
                case 0:
                    success = TryRead(ref state, ref reader, jsonParameterInfo, out arguments.Arg0);
                    break;
                case 1:
                    success = TryRead(ref state, ref reader, jsonParameterInfo, out arguments.Arg1);
                    break;
                case 2:
                    success = TryRead(ref state, ref reader, jsonParameterInfo, out arguments.Arg2);
                    break;
                case 3:
                    success = TryRead(ref state, ref reader, jsonParameterInfo, out arguments.Arg3);
                    break;
                default:
                    Debug.Fail("More than 4 params: we should be in override for LargeObjectWithParameterizedConstructorConverter.");
                    throw new InvalidOperationException();
            }

            return success;
        }

        private static bool TryRead<TArg>(
            scoped ref ReadStack state,
            ref Utf8JsonReader reader,
            JsonParameterInfo jsonParameterInfo,
            out TArg? arg)
        {
            Debug.Assert(jsonParameterInfo.ShouldDeserialize);

            var info = (JsonParameterInfo<TArg>)jsonParameterInfo;

            bool success = info.EffectiveConverter.TryRead(ref reader, info.ParameterType, info.Options, ref state, out TArg? value, out _);

            if (success)
            {
                if (value is null)
                {
                    if (info.IgnoreNullTokensOnRead)
                    {
                        // Use default value specified on parameter, if any.
                        value = info.EffectiveDefaultValue;
                    }
                    else if (!info.IsNullable && info.Options.RespectNullableAnnotations)
                    {
                        ThrowHelper.ThrowJsonException_ConstructorParameterDisallowNull(info.Name, state.Current.JsonTypeInfo.Type);
                    }
                }

                state.Current.MarkRequiredPropertyAsRead(jsonParameterInfo.MatchingProperty);
            }

            arg = value;
            return success;
        }

        protected override void InitializeConstructorArgumentCaches(ref ReadStack state, JsonSerializerOptions options)
        {
            JsonTypeInfo typeInfo = state.Current.JsonTypeInfo;

            Debug.Assert(typeInfo.CreateObjectWithArgs != null);

            var arguments = new Arguments<TArg0, TArg1, TArg2, TArg3>();

            foreach (JsonParameterInfo parameterInfo in typeInfo.ParameterCache)
            {
                switch (parameterInfo.Position)
                {
                    case 0:
                        arguments.Arg0 = ((JsonParameterInfo<TArg0>)parameterInfo).EffectiveDefaultValue;
                        break;
                    case 1:
                        arguments.Arg1 = ((JsonParameterInfo<TArg1>)parameterInfo).EffectiveDefaultValue;
                        break;
                    case 2:
                        arguments.Arg2 = ((JsonParameterInfo<TArg2>)parameterInfo).EffectiveDefaultValue;
                        break;
                    case 3:
                        arguments.Arg3 = ((JsonParameterInfo<TArg3>)parameterInfo).EffectiveDefaultValue;
                        break;
                    default:
                        Debug.Fail("More than 4 params: we should be in override for LargeObjectWithParameterizedConstructorConverter.");
                        break;
                }
            }

            state.Current.CtorArgumentState!.Arguments = arguments;
        }

        [RequiresUnreferencedCode(JsonSerializer.SerializationUnreferencedCodeMessage)]
        [RequiresDynamicCode(JsonSerializer.SerializationRequiresDynamicCodeMessage)]
        internal override void ConfigureJsonTypeInfoUsingReflection(JsonTypeInfo jsonTypeInfo, JsonSerializerOptions options)
        {
            jsonTypeInfo.CreateObjectWithArgs = DefaultJsonTypeInfoResolver.MemberAccessor.CreateParameterizedConstructor<T, TArg0, TArg1, TArg2, TArg3>(ConstructorInfo!);
        }
    }
}
