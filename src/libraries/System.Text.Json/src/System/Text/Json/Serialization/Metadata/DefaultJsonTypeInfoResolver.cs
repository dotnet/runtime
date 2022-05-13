// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.ExceptionServices;

namespace System.Text.Json.Serialization.Metadata
{
    internal sealed class DefaultJsonTypeInfoResolver : IJsonTypeInfoResolver
    {
        [RequiresUnreferencedCode(JsonSerializer.SerializationUnreferencedCodeMessage)]
        [RequiresDynamicCode(JsonSerializer.SerializationRequiresDynamicCodeMessage)]
        public DefaultJsonTypeInfoResolver()
        {
        }

        public JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options)
        {
            JsonTypeInfo.ValidateType(type, null, null, options);
            JsonTypeInfo typeInfo = CreateJsonTypeInfo(type, options);

            foreach (Action<JsonTypeInfo> modifier in Modifiers)
            {
                modifier(typeInfo);
            }

            return typeInfo;
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "The ctor is marked RequiresUnreferencedCode.")]
        [UnconditionalSuppressMessage("AotAnalysis", "IL3050:RequiresDynamicCode",
            Justification = "The ctor is marked RequiresDynamicCode.")]
        private JsonTypeInfo CreateJsonTypeInfo(Type type, JsonSerializerOptions options)
        {
            MethodInfo methodInfo = typeof(JsonSerializerOptions).GetMethod(nameof(JsonSerializerOptions.CreateReflectionJsonTypeInfo), BindingFlags.NonPublic | BindingFlags.Instance)!;
#if NETCOREAPP
            return (JsonTypeInfo)methodInfo.MakeGenericMethod(type).Invoke(options, BindingFlags.NonPublic | BindingFlags.DoNotWrapExceptions, null, null, null)!;
#else
            try
            {
                return (JsonTypeInfo)methodInfo.MakeGenericMethod(type).Invoke(options, null)!;
            }
            catch (TargetInvocationException ex)
            {
                // Some of the validation is done during construction (i.e. validity of JsonConverter, inner types etc.)
                // therefore we need to unwrap TargetInvocationException for better user experience
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                throw null!;
            }
#endif
        }

        public IList<Action<JsonTypeInfo>> Modifiers { get; } = new List<Action<JsonTypeInfo>>(); // TODO
    }
}
