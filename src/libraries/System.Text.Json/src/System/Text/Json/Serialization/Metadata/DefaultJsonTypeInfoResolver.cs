// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Reflection;
using System.Threading;

namespace System.Text.Json.Serialization.Metadata
{
    /// <summary>
    /// Defines the default, reflection-based JSON contract resolver used by System.Text.Json.
    /// </summary>
    /// <remarks>
    /// The contract resolver used by <see cref="JsonSerializerOptions.Default"/>.
    /// </remarks>
    public partial class DefaultJsonTypeInfoResolver : IJsonTypeInfoResolver
    {
        private bool _mutable;

        /// <summary>
        /// Creates a mutable <see cref="DefaultJsonTypeInfoResolver"/> instance.
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

        /// <summary>
        /// Resolves a JSON contract for a given <paramref name="type"/> and <paramref name="options"/> configuration.
        /// </summary>
        /// <param name="type">The type for which to resolve a JSON contract.</param>
        /// <param name="options">A <see cref="JsonSerializerOptions"/> instance used to determine contract configuration.</param>
        /// <returns>A <see cref="JsonTypeInfo"/> defining a reflection-derived JSON contract for <paramref name="type"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="type"/> or <paramref name="options"/> is <see langword="null"/>.</exception>
        /// <remarks>
        /// The base implementation of this method will produce a reflection-derived contract
        /// and apply any callbacks from the <see cref="Modifiers"/> list.
        /// </remarks>
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
            JsonTypeInfo jsonTypeInfo;
            JsonConverter converter = GetConverterForType(type, options);

            if (converter.TypeToConvert == type)
            {
                // For performance, avoid doing a reflection-based instantiation
                // if the converter type matches that of the declared type.
                jsonTypeInfo = converter.CreateReflectionJsonTypeInfo(options);
            }
            else
            {
                Type jsonTypeInfoType = typeof(ReflectionJsonTypeInfo<>).MakeGenericType(type);
                jsonTypeInfo = (JsonTypeInfo)jsonTypeInfoType.CreateInstanceNoWrapExceptions(
                    parameterTypes: new Type[] { typeof(JsonConverter), typeof(JsonSerializerOptions) },
                    parameters: new object[] { converter, options })!;
            }

            Debug.Assert(jsonTypeInfo.Type == type);
            return jsonTypeInfo;
        }

        /// <summary>
        /// Gets a list of user-defined callbacks that can be used to modify the initial contract.
        /// </summary>
        /// <remarks>
        /// The modifier list will be rendered immutable after the first <see cref="GetTypeInfo(Type, JsonSerializerOptions)"/> invocation.
        ///
        /// Modifier callbacks are called consecutively in the order in which they are specified in the list.
        /// </remarks>
        public IList<Action<JsonTypeInfo>> Modifiers => _modifiers ??= new ModifierCollection(this);
        private ModifierCollection? _modifiers;

        private sealed class ModifierCollection : ConfigurationList<Action<JsonTypeInfo>>
        {
            private readonly DefaultJsonTypeInfoResolver _resolver;

            public ModifierCollection(DefaultJsonTypeInfoResolver resolver)
            {
                _resolver = resolver;
            }

            protected override bool IsImmutable => !_resolver._mutable;
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
