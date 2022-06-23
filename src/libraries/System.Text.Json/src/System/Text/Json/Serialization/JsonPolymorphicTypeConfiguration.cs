// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization
{
    /// <summary>
    /// Defines polymorphic configuration for a specified base type.
    /// </summary>
    public class JsonPolymorphicTypeConfiguration : IJsonPolymorphicTypeConfiguration, ICollection<(Type DerivedType, object? TypeDiscriminator)>
    {
        private readonly List<(Type DerivedType, object? TypeDiscriminator)> _derivedTypes = new();
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
        public string? TypeDiscriminatorPropertyName
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
        /// <returns>The same <see cref="JsonPolymorphicTypeConfiguration"/> instance after it has been updated.</returns>
        public JsonPolymorphicTypeConfiguration WithDerivedType(Type derivedType)
            => WithDerivedTypeCore(derivedType, null);

        /// <summary>
        /// Opts in polymorphic serialization for the specified derived type.
        /// </summary>
        /// <param name="derivedType">The derived type for which to enable polymorphism.</param>
        /// <param name="typeDiscriminator">The type discriminator id to use for the specified derived type.</param>
        /// <returns>The same <see cref="JsonPolymorphicTypeConfiguration"/> instance after it has been updated.</returns>
        public JsonPolymorphicTypeConfiguration WithDerivedType(Type derivedType, string typeDiscriminator) =>
            WithDerivedTypeCore(derivedType, typeDiscriminator);

        /// <summary>
        /// Opts in polymorphic serialization for the specified derived type.
        /// </summary>
        /// <param name="derivedType">The derived type for which to enable polymorphism.</param>
        /// <param name="typeDiscriminator">The type discriminator id to use for the specified derived type.</param>
        /// <returns>The same <see cref="JsonPolymorphicTypeConfiguration"/> instance after it has been updated.</returns>
        public JsonPolymorphicTypeConfiguration WithDerivedType(Type derivedType, int typeDiscriminator) =>
            WithDerivedTypeCore(derivedType, typeDiscriminator);

        private JsonPolymorphicTypeConfiguration WithDerivedTypeCore(Type derivedType, object? typeDiscriminator)
        {
            Debug.Assert(typeDiscriminator is null or string or int);

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
            foreach ((Type DerivedType, object? TypeDiscriminator) entry in _derivedTypes)
            {
                if (entry.DerivedType == derivedType)
                {
                    throw new ArgumentException(SR.Format(SR.Polymorphism_DerivedTypeIsAlreadySpecified, BaseType, derivedType), nameof(derivedType));
                }

                if (typeDiscriminator != null && typeDiscriminator.Equals(entry.TypeDiscriminator))
                {
                    throw new ArgumentException(SR.Format(SR.Polymorphism_TypeDicriminatorIdIsAlreadySpecified, BaseType, typeDiscriminator), nameof(typeDiscriminator));
                }
            }

            // Validation complete; update the configuration state.
            _derivedTypes.Add((derivedType, typeDiscriminator));
            return this;
        }

        IEnumerable<(Type DerivedType, object? TypeDiscriminator)> IJsonPolymorphicTypeConfiguration.GetSupportedDerivedTypes()
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

        bool ICollection<(Type DerivedType, object? TypeDiscriminator)>.Contains((Type DerivedType, object? TypeDiscriminator) item) => _derivedTypes.Contains(item);
        void ICollection<(Type DerivedType, object? TypeDiscriminator)>.CopyTo((Type DerivedType, object? TypeDiscriminator)[] array, int arrayIndex) => _derivedTypes.CopyTo(array, arrayIndex);
        bool ICollection<(Type DerivedType, object? TypeDiscriminator)>.Remove((Type DerivedType, object? TypeDiscriminator) item)
        {
            VerifyMutable();
            return _derivedTypes.Remove(item);
        }

        bool ICollection<(Type DerivedType, object? TypeDiscriminator)>.IsReadOnly => IsAssignedToOptionsInstance;
        int ICollection<(Type DerivedType, object? TypeDiscriminator)>.Count => _derivedTypes.Count;
        void ICollection<(Type DerivedType, object? TypeDiscriminator)>.Add((Type DerivedType, object? TypeDiscriminator) item)
        {
            if (item.TypeDiscriminator is not (null or string or int))
            {
                throw new ArgumentException(nameof(item));
            }

            WithDerivedTypeCore(item.DerivedType, item.TypeDiscriminator);
        }

        void ICollection<(Type DerivedType, object? TypeDiscriminator)>.Clear()
        {
            VerifyMutable();
            _derivedTypes.Clear();
        }

        IEnumerator<(Type DerivedType, object? TypeDiscriminator)> IEnumerable<(Type DerivedType, object? TypeDiscriminator)>.GetEnumerator() => _derivedTypes.GetEnumerator();
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
        /// <returns>The same <see cref="JsonPolymorphicTypeConfiguration"/> instance after it has been updated.</returns>
        public JsonPolymorphicTypeConfiguration<TBaseType> WithDerivedType<TDerivedType>() where TDerivedType : TBaseType
        {
            WithDerivedType(typeof(TDerivedType));
            return this;
        }

        /// <summary>
        /// Associates specified derived type with supplied string identifier.
        /// </summary>
        /// <typeparam name="TDerivedType">The derived type with which to associate a type identifier.</typeparam>
        /// <param name="typeDiscriminator">The type identifier to use for the specified derived type.</param>
        /// <returns>The same <see cref="JsonPolymorphicTypeConfiguration"/> instance after it has been updated.</returns>
        public JsonPolymorphicTypeConfiguration<TBaseType> WithDerivedType<TDerivedType>(string typeDiscriminator) where TDerivedType : TBaseType
        {
            WithDerivedType(typeof(TDerivedType), typeDiscriminator);
            return this;
        }

        /// <summary>
        /// Associates specified derived type with supplied string identifier.
        /// </summary>
        /// <typeparam name="TDerivedType">The derived type with which to associate a type identifier.</typeparam>
        /// <param name="typeDiscriminator">The type identifier to use for the specified derived type.</param>
        /// <returns>The same <see cref="JsonPolymorphicTypeConfiguration"/> instance after it has been updated.</returns>
        public JsonPolymorphicTypeConfiguration<TBaseType> WithDerivedType<TDerivedType>(int typeDiscriminator) where TDerivedType : TBaseType
        {
            WithDerivedType(typeof(TDerivedType), typeDiscriminator);
            return this;
        }
    }
}
