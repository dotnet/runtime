// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization
{
    /// <summary>
    /// Defines polymorphic type discriminator configuration for a given type.
    /// </summary>
    public class TypeDiscriminatorConfiguration : IReadOnlyCollection<KeyValuePair<Type, string>>
    {
        private Dictionary<Type, string> _knownTypes = new();

        /// <summary>
        /// Creates a new type discriminator configuration instance for a given base type.
        /// </summary>
        /// <param name="baseType">The base type for which to configure polymorphic serialization.</param>
        public TypeDiscriminatorConfiguration(Type baseType)
        {
            if (!SupportsTypeDiscriminators(baseType))
            {
                throw new ArgumentException("The specified base type does not support known types configuration.", nameof(baseType));
            }

            BaseType = baseType;
        }

        /// <summary>The base type for which type discriminator configuration is specified.</summary>
        public Type BaseType { get; }

        /// <summary>
        /// Associates specified derived type with supplied string identifier.
        /// </summary>
        /// <param name="derivedType">The derived type with which to associate a type identifier.</param>
        /// <param name="identifier">The type identifier to use for the specified dervied type.</param>
        /// <returns>The same <see cref="TypeDiscriminatorConfiguration"/> instance after it has been updated.</returns>
        public TypeDiscriminatorConfiguration WithKnownType(Type derivedType, string identifier)
        {
            VerifyMutable();

            if (!BaseType.IsAssignableFrom(derivedType))
            {
                throw new ArgumentException("Specified type is not assignable to the base type.", nameof(derivedType));
            }

            // TODO: this check might be removed depending on final type discriminator semantics
            if (derivedType == BaseType)
            {
                throw new ArgumentException("Specified type must be a proper subtype of the base type.", nameof(derivedType));
            }

            if (_knownTypes.ContainsKey(derivedType))
            {
                throw new ArgumentException("Specified type has already been assigned as a known type.", nameof(derivedType));
            }

            // linear traversal is probably appropriate here, but might consider using a HashSet storing the id's
            foreach (string id in _knownTypes.Values)
            {
                if (id == identifier)
                {
                    throw new ArgumentException("A subtype with specified identifier has already been registered.", nameof(identifier));
                }
            }

            _knownTypes.Add(derivedType, identifier);

            return this;
        }

        internal static bool TryCreateFromKnownTypeAttributes(Type baseType, [NotNullWhen(true)] out TypeDiscriminatorConfiguration? config)
        {
            if (!SupportsTypeDiscriminators(baseType))
            {
                config = null;
                return false;
            }

            object[] attributes = baseType.GetCustomAttributes(typeof(JsonKnownTypeAttribute), inherit: false);

            if (attributes.Length == 0)
            {
                config = null;
                return false;
            }

            var cfg = new TypeDiscriminatorConfiguration(baseType);
            foreach (JsonKnownTypeAttribute attribute in attributes)
            {
                cfg.WithKnownType(attribute.Subtype, attribute.Identifier);
            }

            config = cfg;
            return true;
        }

        internal bool IsAssignedToOptionsInstance { get; set; }

        private void VerifyMutable()
        {
            if (IsAssignedToOptionsInstance)
            {
                ThrowHelper.ThrowInvalidOperationException_SerializerOptionsImmutable(null);
            }
        }

        private static bool SupportsTypeDiscriminators(Type type) => !type.IsGenericTypeDefinition && !type.IsValueType && !type.IsSealed && type != JsonTypeInfo.ObjectType;

        int IReadOnlyCollection<KeyValuePair<Type, string>>.Count => _knownTypes.Count;
        IEnumerator<KeyValuePair<Type, string>> IEnumerable<KeyValuePair<Type, string>>.GetEnumerator() => _knownTypes.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _knownTypes.GetEnumerator();
    }

    /// <summary>
    /// Contains type discriminator metadata configuration for a given type.
    /// </summary>
    /// <typeparam name="TBaseType">The type for which type discriminator configuration is provided.</typeparam>
    public class TypeDiscriminatorConfiguration<TBaseType> : TypeDiscriminatorConfiguration where TBaseType : class
    {
        /// <summary>
        /// Creates a new type discriminator metadata configuration instance for a given type.
        /// </summary>
        public TypeDiscriminatorConfiguration() : base(typeof(TBaseType))
        {
        }

        /// <summary>
        /// Associates specified derived type with supplied string identifier.
        /// </summary>
        /// <typeparam name="TDerivedType">The derived type with which to associate a type identifier.</typeparam>
        /// <param name="identifier">The type identifier to use for the specified dervied type.</param>
        /// <returns>The same <see cref="TypeDiscriminatorConfiguration"/> instance after it has been updated.</returns>
        public TypeDiscriminatorConfiguration<TBaseType> WithKnownType<TDerivedType>(string identifier) where TDerivedType : TBaseType
        {
            WithKnownType(typeof(TDerivedType), identifier);
            return this;
        }
    }
}
