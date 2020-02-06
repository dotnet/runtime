// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Reflection;

namespace System.Text.Json.Serialization.Converters
{
    /// <summary>
    /// Implementation of <cref>JsonObjectConverter{T}</cref> that supports the deserialization
    /// of JSON objects using parameterized constructors.
    /// </summary>
    internal sealed class SmallObjectWithParameterizedConstructorConverter<T, TArg0, TArg1, TArg2, TArg3> :
        ObjectWithParameterizedConstructorConverter<T>
    {
        private JsonClassInfo.ParameterizedConstructorDelegate<T, TArg0, TArg1, TArg2, TArg3>? _createObject;

        internal override void Initialize(ConstructorInfo constructor, JsonSerializerOptions options)
        {
            base.Initialize(constructor, options);

            _createObject = options.MemberAccessorStrategy.CreateParameterizedConstructor<T, TArg0, TArg1, TArg2, TArg3>(constructor)!;
        }

        protected override object CreateObject(ref ReadStack state)
        {
            var argCache = (ArgumentCache<TArg0, TArg1, TArg2, TArg3>)state.Current.ConstructorArguments!;
            return _createObject!(argCache.Arg0, argCache.Arg1, argCache.Arg2, argCache.Arg3)!;
        }

        protected override bool ReadAndCacheConstructorArgument(ref ReadStack state, ref Utf8JsonReader reader, JsonParameterInfo jsonParameterInfo, JsonSerializerOptions options)
        {
            Debug.Assert(state.Current.ConstructorArguments != null);
            var arguments = (ArgumentCache<TArg0, TArg1, TArg2, TArg3>)state.Current.ConstructorArguments;

            bool success;

            switch (jsonParameterInfo.Position)
            {
                case 0:
                    success = ((JsonParameterInfo<TArg0>)jsonParameterInfo).ReadJsonTyped(ref state, ref reader, options, out TArg0 arg0);
                    if (success)
                    {
                        arguments.Arg0 = arg0;
                    }
                    break;
                case 1:
                    success = ((JsonParameterInfo<TArg1>)jsonParameterInfo).ReadJsonTyped(ref state, ref reader, options, out TArg1 arg1);
                    if (success)
                    {
                        arguments.Arg1 = arg1;
                    }
                    break;
                case 2:
                    success = ((JsonParameterInfo<TArg2>)jsonParameterInfo).ReadJsonTyped(ref state, ref reader, options, out TArg2 arg2);
                    if (success)
                    {
                        arguments.Arg2 = arg2;
                    }
                    break;
                case 3:
                    success = ((JsonParameterInfo<TArg3>)jsonParameterInfo).ReadJsonTyped(ref state, ref reader, options, out TArg3 arg3);
                    if (success)
                    {
                        arguments.Arg3 = arg3;
                    }
                    break;
                default:
                    Debug.Fail("This should never happen.");
                    throw new InvalidOperationException();
            }

            return success;
        }

        protected override void InitializeConstructorArgumentCaches(ref ReadStackFrame frame, JsonSerializerOptions options)
        {
            if (ParameterCount != ParameterCache.Count)
            {
                ThrowHelper.ThrowInvalidOperationException_ConstructorParameterIncompleteBinding(ConstructorInfo, TypeToConvert);
            }

            var arguments = new ArgumentCache<TArg0, TArg1, TArg2, TArg3>();

            foreach (JsonParameterInfo parameterInfo in ParameterCache.Values)
            {
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
                            Debug.Fail("We should never get here.");
                            break;
                    }
                }
            }

            frame.ConstructorArguments = arguments;
        }
    }
}
