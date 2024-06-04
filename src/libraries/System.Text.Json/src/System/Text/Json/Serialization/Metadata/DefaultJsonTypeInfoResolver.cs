// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace System.Text.Json.Serialization.Metadata
{
    /// <summary>
    /// Defines the default, reflection-based JSON contract resolver used by System.Text.Json.
    /// </summary>
    /// <remarks>
    /// The contract resolver used by <see cref="JsonSerializerOptions.Default"/>.
    /// </remarks>
    public partial class DefaultJsonTypeInfoResolver : IJsonTypeInfoResolver, IBuiltInJsonTypeInfoResolver
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
            typeInfo.OriginatingResolver = this;

            // We've finished configuring the metadata, brand the instance as user-unmodified.
            // This should be the last update operation in the resolver to avoid resetting the flag.
            typeInfo.IsCustomized = false;

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
            JsonConverter converter = GetConverterForType(type, options);
            return CreateTypeInfoCore(type, converter, options);
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

            public override bool IsReadOnly => !_resolver._mutable;
            protected override void OnCollectionModifying()
            {
                if (!_resolver._mutable)
                {
                    ThrowHelper.ThrowInvalidOperationException_DefaultTypeInfoResolverImmutable();
                }
            }
        }

        bool IBuiltInJsonTypeInfoResolver.IsCompatibleWithOptions(JsonSerializerOptions _)
            // Metadata generated by the default resolver is compatible by definition,
            // provided that no user extensions have been made on the class.
            => _modifiers is null or { Count: 0 } && GetType() == typeof(DefaultJsonTypeInfoResolver);

        internal static DefaultJsonTypeInfoResolver DefaultInstance
        {
            [RequiresUnreferencedCode(JsonSerializer.SerializationUnreferencedCodeMessage)]
            [RequiresDynamicCode(JsonSerializer.SerializationRequiresDynamicCodeMessage)]
            get
            {
                if (s_defaultInstance is DefaultJsonTypeInfoResolver result)
                {
                    return result;
                }

                var newInstance = new DefaultJsonTypeInfoResolver(mutable: false);
                return Interlocked.CompareExchange(ref s_defaultInstance, newInstance, comparand: null) ?? newInstance;
            }
        }

        private static DefaultJsonTypeInfoResolver? s_defaultInstance;
    }
}
