// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization
{
    /// <summary>
    /// Defines polymorphic configuration for a specified base type.
    /// </summary>
    public class JsonPolymorphicTypeConfiguration : IJsonPolymorphicTypeConfiguration, ICollection<(Type DerivedType, string? TypeDiscriminatorId)>
    {
        private readonly List<(Type DerivedType, string? TypeDiscriminatorId)> _derivedTypes = new();
        private string? _customTypeDiscriminatorPropertyName;
        private JsonUnknownDerivedTypeHandling _unknownDerivedTypeHandling;
        private bool _ignoreUnrecognizedTypeDiscriminators;

        /// <summary>
        /// Creates a new polymorphic configuration instance for a given base type.
        /// </summary>
        /// <param name="baseType">The base type for which to configure polymorphic serialization.</param>
        public JsonPolymorphicTypeConfiguration(Type baseType)
        {
            if (baseType is null)
            {
                throw new ArgumentNullException(nameof(baseType));
            }

            if (!PolymorphicTypeResolver.IsSupportedPolymorphicBaseType(baseType))
            {
                throw new ArgumentException(SR.Format(SR.Polymorphism_TypeDoesNotSupportPolymorphism, baseType), nameof(baseType));
            }

            BaseType = baseType;
        }

        /// <summary>
        /// Gets the base type for which polymorphic serialization is being configured.
        /// </summary>
        public Type BaseType { get; }

        /// <summary>
        /// Gets or sets the behavior when serializing an undeclared derived runtime type.
        /// </summary>
        public JsonUnknownDerivedTypeHandling UnknownDerivedTypeHandling
        {
            get => _unknownDerivedTypeHandling;
            set
            {
                VerifyMutable();
                _unknownDerivedTypeHandling = value;
            }
        }

        /// <summary>
        /// When set to <see langword="true"/>, instructs the serializer to ignore any
        /// unrecognized type discriminator id's and reverts to the contract of the base type.
        /// Otherwise, it will fail the deserialization.
        /// </summary>
        public bool IgnoreUnrecognizedTypeDiscriminators
        {
            get => _ignoreUnrecognizedTypeDiscriminators;
            set
            {
                VerifyMutable();
                _ignoreUnrecognizedTypeDiscriminators = value;
            }
        }

        /// <summary>
        /// Gets or sets a custom type discriminator property name for the polymorhic type.
        /// Uses the default '$type' property name if left unset.
        /// </summary>
        public string? CustomTypeDiscriminatorPropertyName
        {
            get => _customTypeDiscriminatorPropertyName;
            set
            {
                VerifyMutable();
                _customTypeDiscriminatorPropertyName = value;
            }
        }

        /// <summary>
        /// Opts in polymorphic serialization for the specified derived type.
        /// </summary>
        /// <param name="derivedType">The derived type for which to enable polymorphism.</param>
        /// <param name="typeDiscriminatorId">The type discriminator id to use for the specified derived type.</param>
        /// <returns>The same <see cref="JsonPolymorphicTypeConfiguration"/> instance after it has been updated.</returns>
        public JsonPolymorphicTypeConfiguration WithDerivedType(Type derivedType, string? typeDiscriminatorId = null)
        {
            VerifyMutable();

            if (derivedType is null)
            {
                throw new ArgumentNullException(nameof(derivedType));
            }

            if (!PolymorphicTypeResolver.IsSupportedDerivedType(BaseType, derivedType))
            {
                throw new ArgumentException(SR.Format(SR.Polymorphism_DerivedTypeIsNotSupported, derivedType, BaseType), nameof(derivedType));
            }

            // Perform a linear traversal to determine any duplicate derived types or discriminator Id's
            // The assumption is that each type maintains a small number of subtypes so this is preferable
            // to maintaing hashtables to existing entries.
            foreach ((Type DerivedType, string? TypeDiscriminatorId) entry in _derivedTypes)
            {
                if (entry.DerivedType == derivedType)
                {
                    throw new ArgumentException(SR.Format(SR.Polymorphism_DerivedTypeIsAlreadySpecified, BaseType, derivedType), nameof(derivedType));
                }

                if (typeDiscriminatorId != null && entry.TypeDiscriminatorId == typeDiscriminatorId)
                {
                    throw new ArgumentException(SR.Format(SR.Polymorphism_TypeDicriminatorIdIsAlreadySpecified, BaseType, typeDiscriminatorId), nameof(typeDiscriminatorId));
                }
            }

            // Validation complete; update the configuration state.
            _derivedTypes.Add((derivedType, typeDiscriminatorId));
            return this;
        }

        IEnumerable<(Type DerivedType, string? TypeDiscriminatorId)> IJsonPolymorphicTypeConfiguration.GetSupportedDerivedTypes()
        {
            foreach ((Type, string?) entry in _derivedTypes)
            {
                yield return entry;
            }
        }

        internal bool IsAssignedToOptionsInstance { get; set; }

        private void VerifyMutable()
        {
            if (IsAssignedToOptionsInstance)
            {
                ThrowHelper.ThrowInvalidOperationException_SerializerOptionsImmutable(context: null);
            }
        }

        bool ICollection<(Type DerivedType, string? TypeDiscriminatorId)>.Contains((Type DerivedType, string? TypeDiscriminatorId) item) => _derivedTypes.Contains(item);
        void ICollection<(Type DerivedType, string? TypeDiscriminatorId)>.CopyTo((Type DerivedType, string? TypeDiscriminatorId)[] array, int arrayIndex) => _derivedTypes.CopyTo(array, arrayIndex);
        bool ICollection<(Type DerivedType, string? TypeDiscriminatorId)>.Remove((Type DerivedType, string? TypeDiscriminatorId) item)
        {
            VerifyMutable();
            return _derivedTypes.Remove(item);
        }

        bool ICollection<(Type DerivedType, string? TypeDiscriminatorId)>.IsReadOnly => IsAssignedToOptionsInstance;
        int ICollection<(Type DerivedType, string? TypeDiscriminatorId)>.Count => _derivedTypes.Count;
        void ICollection<(Type DerivedType, string? TypeDiscriminatorId)>.Add((Type DerivedType, string? TypeDiscriminatorId) item) => WithDerivedType(item.DerivedType, item.TypeDiscriminatorId);
        void ICollection<(Type DerivedType, string? TypeDiscriminatorId)>.Clear()
        {
            VerifyMutable();
            _derivedTypes.Clear();
        }

        IEnumerator<(Type DerivedType, string? TypeDiscriminatorId)> IEnumerable<(Type DerivedType, string? TypeDiscriminatorId)>.GetEnumerator() => _derivedTypes.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _derivedTypes.GetEnumerator();
    }

    /// <summary>
    /// Defines polymorphic type configuration for a given type.
    /// </summary>
    /// <typeparam name="TBaseType">The type for which polymorphic configuration is provided.</typeparam>
    public class JsonPolymorphicTypeConfiguration<TBaseType> : JsonPolymorphicTypeConfiguration where TBaseType : class
    {
        /// <summary>
        /// Creates a new polymorphic configuration instance for a given base type.
        /// </summary>
        public JsonPolymorphicTypeConfiguration() : base(typeof(TBaseType))
        {
        }

        /// <summary>
        /// Associates specified derived type with supplied string identifier.
        /// </summary>
        /// <typeparam name="TDerivedType">The derived type with which to associate a type identifier.</typeparam>
        /// <param name="typeDiscriminatorId">The type identifier to use for the specified derived type.</param>
        /// <returns>The same <see cref="JsonPolymorphicTypeConfiguration"/> instance after it has been updated.</returns>
        public JsonPolymorphicTypeConfiguration<TBaseType> WithDerivedType<TDerivedType>(string? typeDiscriminatorId = null) where TDerivedType : TBaseType
        {
            WithDerivedType(typeof(TDerivedType), typeDiscriminatorId);
            return this;
        }
    }
}
