// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json.Reflection;
using System.Threading;

namespace System.Text.Json.Serialization.Metadata
{
    /// <summary>
    /// Default JsonTypeInfo resolver.
    /// </summary>
    public partial class DefaultJsonTypeInfoResolver : IJsonTypeInfoResolver
    {
        private bool _mutable;

        /// <summary>
        /// Constructs DefaultJsonTypeInfoResolver.
        /// </summary>
        [RequiresUnreferencedCode(JsonSerializer.SerializationUnreferencedCodeMessage)]
        [RequiresDynamicCode(JsonSerializer.SerializationRequiresDynamicCodeMessage)]
        public DefaultJsonTypeInfoResolver() : this(mutable: true)
        {
        }

        [RequiresUnreferencedCode(JsonSerializer.SerializationUnreferencedCodeMessage)]
        [RequiresDynamicCode(JsonSerializer.SerializationRequiresDynamicCodeMessage)]
        private DefaultJsonTypeInfoResolver(bool mutable)
        {
            _mutable = mutable;

            s_defaultFactoryConverters ??= GetDefaultFactoryConverters();
            s_defaultSimpleConverters ??= GetDefaultSimpleConverters();
        }

        /// <inheritdoc/>
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "The ctor is marked RequiresUnreferencedCode.")]
        [UnconditionalSuppressMessage("AotAnalysis", "IL3050:RequiresDynamicCode",
            Justification = "The ctor is marked RequiresDynamicCode.")]
        public virtual JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options)
        {
            if (type == null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(type));
            }

            if (options == null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(options));
            }

            _mutable = false;

            JsonTypeInfo.ValidateType(type);
            JsonTypeInfo typeInfo = CreateJsonTypeInfo(type, options);

            if (_modifiers != null)
            {
                foreach (Action<JsonTypeInfo> modifier in _modifiers)
                {
                    modifier(typeInfo);
                }
            }

            return typeInfo;
        }

        [RequiresUnreferencedCode(JsonSerializer.SerializationUnreferencedCodeMessage)]
        [RequiresDynamicCode(JsonSerializer.SerializationRequiresDynamicCodeMessage)]
        private static JsonTypeInfo CreateJsonTypeInfo(Type type, JsonSerializerOptions options)
        {
            s_createReflectionJsonTypeInfoMethodInfo ??= typeof(DefaultJsonTypeInfoResolver).GetMethod(nameof(CreateReflectionJsonTypeInfo), BindingFlags.NonPublic | BindingFlags.Static)!;
            return (JsonTypeInfo)s_createReflectionJsonTypeInfoMethodInfo.MakeGenericMethod(type)
                .InvokeNoWrapExceptions(null, new[] { options })!;
        }

        [RequiresUnreferencedCode(JsonSerializer.SerializationUnreferencedCodeMessage)]
        [RequiresDynamicCode(JsonSerializer.SerializationRequiresDynamicCodeMessage)]
        private static JsonTypeInfo<T> CreateReflectionJsonTypeInfo<T>(JsonSerializerOptions options)
        {
            JsonConverter converter = GetConverterForType(typeof(T), options);
            return new ReflectionJsonTypeInfo<T>(converter, options);
        }

        private static MethodInfo? s_createReflectionJsonTypeInfoMethodInfo;

        /// <summary>
        /// List of JsonTypeInfo modifiers. Modifying callbacks are called consecutively after initial resolution
        /// and cannot be changed after GetTypeInfo is called.
        /// </summary>
        public IList<Action<JsonTypeInfo>> Modifiers => _modifiers ??= new ModifierCollection(this);
        private ModifierCollection? _modifiers;

        private sealed class ModifierCollection : ConfigurationList<Action<JsonTypeInfo>>
        {
            private readonly DefaultJsonTypeInfoResolver _resolver;

            public ModifierCollection(DefaultJsonTypeInfoResolver resolver)
            {
                _resolver = resolver;
            }

            protected override bool IsLockedInstance => !_resolver._mutable;
            protected override void VerifyMutable()
            {
                if (!_resolver._mutable)
                {
                    ThrowHelper.ThrowInvalidOperationException_TypeInfoResolverImmutable();
                }
            }
        }

        internal static bool IsDefaultInstanceRooted => s_defaultInstance is not null;
        private static DefaultJsonTypeInfoResolver? s_defaultInstance;

        [RequiresUnreferencedCode(JsonSerializer.SerializationUnreferencedCodeMessage)]
        [RequiresDynamicCode(JsonSerializer.SerializationRequiresDynamicCodeMessage)]
        internal static DefaultJsonTypeInfoResolver RootDefaultInstance()
        {
            if (s_defaultInstance is DefaultJsonTypeInfoResolver result)
            {
                return result;
            }

            var newInstance = new DefaultJsonTypeInfoResolver(mutable: false);
            DefaultJsonTypeInfoResolver? originalInstance = Interlocked.CompareExchange(ref s_defaultInstance, newInstance, comparand: null);
            return originalInstance ?? newInstance;
        }
    }
}
