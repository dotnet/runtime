// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;

namespace System.Text.Json.Serialization.Converters
{
    /// <summary>
    /// Implementation of <cref>JsonObjectConverter{T}</cref> that supports the deserialization
    /// of JSON objects using parameterized constructors.
    /// </summary>
    internal sealed class LargeObjectWithParameterizedConstructorConverter<T> : ObjectWithParameterizedConstructorConverter<T> where T : notnull
    {
        protected override bool ReadAndCacheConstructorArgument(ref ReadStack state, ref Utf8JsonReader reader, JsonParameterInfo jsonParameterInfo)
        {
            Debug.Assert(jsonParameterInfo.ShouldDeserialize);
            Debug.Assert(jsonParameterInfo.Options != null);

            bool success = jsonParameterInfo.ConverterBase.TryReadAsObject(ref reader, jsonParameterInfo.Options!, ref state, out object? arg);

            if (success && !(arg == null && jsonParameterInfo.IgnoreDefaultValuesOnRead))
            {
                ((object[])state.Current.CtorArgumentState!.Arguments)[jsonParameterInfo.Position] = arg!;
            }

            return success;
        }

        protected override object CreateObject(ref ReadStackFrame frame)
        {
            object[] arguments = (object[])frame.CtorArgumentState!.Arguments;

            var createObject = (JsonClassInfo.ParameterizedConstructorDelegate<T>?)frame.JsonClassInfo.CreateObjectWithArgs;

            if (createObject == null)
            {
                // This means this constructor has more than 64 parameters.
                ThrowHelper.ThrowNotSupportedException_ConstructorMaxOf64Parameters(ConstructorInfo!, TypeToConvert);
            }

            object obj = createObject(arguments);

            ArrayPool<object>.Shared.Return(arguments, clearArray: true);
            return obj;
        }

        protected override void InitializeConstructorArgumentCaches(ref ReadStack state, JsonSerializerOptions options)
        {
            JsonClassInfo classInfo = state.Current.JsonClassInfo;

            if (classInfo.CreateObjectWithArgs == null)
            {
                classInfo.CreateObjectWithArgs = options.MemberAccessorStrategy.CreateParameterizedConstructor<T>(ConstructorInfo!);
            }

            object[] arguments = ArrayPool<object>.Shared.Rent(classInfo.ParameterCount);
            foreach (JsonParameterInfo jsonParameterInfo in classInfo.ParameterCache!.Values)
            {
                if (jsonParameterInfo.ShouldDeserialize)
                {
                    arguments[jsonParameterInfo.Position] = jsonParameterInfo.DefaultValue!;
                }
            }

            state.Current.CtorArgumentState!.Arguments = arguments;
        }
    }
}
