// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.InteropServices
{

    /// <summary>Provides a typed factory associated with a type-map target.</summary>
    /// <typeparam name="TContract">The contract returned by the factory.</typeparam>
    /// <remarks>This attribute does not register a type-map entry or define an instance caching policy.</remarks>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
    public abstract class TypeMapFactoryAttribute<TContract> : Attribute
    {
        /// <summary>Initializes a new instance of the <see cref="TypeMapFactoryAttribute{TContract}"/> class.</summary>
        protected TypeMapFactoryAttribute() { }

        /// <summary>Creates an instance of the factory's contract.</summary>
        /// <returns>An instance implementing <typeparamref name="TContract"/>.</returns>
        public abstract TContract CreateInstance();
    }

    /// <summary>Provides a factory that constructs a specified implementation using its public parameterless constructor.</summary>
    /// <typeparam name="TContract">The contract returned by the factory.</typeparam>
    /// <typeparam name="TImplementation">The implementation to construct.</typeparam>
    /// <remarks>Each call to <see cref="CreateInstance"/> constructs a new instance. The attributed type need not be the implementation type.</remarks>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
    public sealed class TypeMapFactoryAttribute<TContract, TImplementation> : TypeMapFactoryAttribute<TContract>
        where TImplementation : TContract, new()
    {
        /// <summary>Initializes a new instance of the <see cref="TypeMapFactoryAttribute{TContract, TImplementation}"/> class.</summary>
        public TypeMapFactoryAttribute() { }

        /// <inheritdoc />
        public override TContract CreateInstance() => new TImplementation();
    }
}
