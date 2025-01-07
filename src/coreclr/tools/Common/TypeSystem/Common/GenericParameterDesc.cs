// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Internal.TypeSystem
{
    public enum GenericParameterKind
    {
        Type,
        Method,
    }

    /// <summary>
    /// Describes the variance on a generic type parameter of a generic type or method.
    /// </summary>
    public enum GenericVariance
    {
        None = 0,

        /// <summary>
        /// The generic type parameter is covariant. A covariant type parameter can appear
        /// as the result type of a method, the type of a read-only field, a declared base
        /// type, or an implemented interface.
        /// </summary>
        Covariant = 1,

        /// <summary>
        /// The generic type parameter is contravariant. A contravariant type parameter can
        /// appear as a parameter type in method signatures.
        /// </summary>
        Contravariant = 2
    }

    /// <summary>
    /// Describes the constraints on a generic type parameter of a generic type or method.
    /// </summary>
    [Flags]
    public enum GenericConstraints
    {
        None = 0,

        /// <summary>
        /// A type can be substituted for the generic type parameter only if it is a reference type.
        /// </summary>
        ReferenceTypeConstraint = 0x04,

        /// <summary>
        /// A type can be substituted for the generic type parameter only if it is a value
        /// type and is not nullable.
        /// </summary>
        NotNullableValueTypeConstraint = 0x08,

        /// <summary>
        /// A type can be substituted for the generic type parameter only if it has a parameterless
        /// constructor.
        /// </summary>
        DefaultConstructorConstraint = 0x10,

        /// <summary>
        /// A type is permitted to be ByRefLike.
        /// </summary>
        AllowByRefLike = 0x20,
    }

    public abstract partial class GenericParameterDesc : TypeDesc
    {
        /// <summary>
        /// Gets the name of the generic parameter as defined in the metadata.
        /// </summary>
        public virtual string Name
        {
            get
            {
                return string.Concat("T", Index.ToStringInvariant());
            }
        }

        /// <summary>
        /// Gets a value indicating whether this is a type or method generic parameter.
        /// </summary>
        public abstract GenericParameterKind Kind { get; }

        /// <summary>
        /// Gets the zero based index of the generic parameter within the declaring type or method.
        /// </summary>
        public abstract int Index { get; }

        /// <summary>
        /// The associated type or method which defines this Generic Parameter
        /// </summary>
        public abstract TypeSystemEntity AssociatedTypeOrMethod { get; }

        /// <summary>
        /// Gets a value indicating the variance of this generic parameter.
        /// </summary>
        public virtual GenericVariance Variance
        {
            get
            {
                return GenericVariance.None;
            }
        }

        /// <summary>
        /// Gets a value indicating generic constraints of this generic parameter.
        /// </summary>
        public virtual GenericConstraints Constraints
        {
            get
            {
                return GenericConstraints.None;
            }
        }

        /// <summary>
        /// Gets type constraints imposed on substitutions.
        /// </summary>
        public virtual IEnumerable<TypeDesc> TypeConstraints
        {
            get
            {
                return EmptyTypes;
            }
        }

        /// <summary>
        /// Does this generic parameter have the NotNullableValueType constraint flag
        /// </summary>
        public bool HasNotNullableValueTypeConstraint
        {
            get
            {
                return (Constraints & GenericConstraints.NotNullableValueTypeConstraint) != 0;
            }
        }

        /// <summary>
        /// Does this generic parameter have the ReferenceType constraint flag
        /// </summary>
        public bool HasReferenceTypeConstraint
        {
            get
            {
                return (Constraints & GenericConstraints.ReferenceTypeConstraint) != 0;
            }
        }

        /// <summary>
        /// Does this generic parameter have the DefaultConstructor constraint flag
        /// </summary>
        public bool HasDefaultConstructorConstraint
        {
            get
            {
                return (Constraints & GenericConstraints.DefaultConstructorConstraint) != 0;
            }
        }

        /// <summary>
        /// Does this generic parameter have the AllowByRefLike flag
        /// </summary>
        public bool HasAllowByRefLikeConstraint
        {
            get
            {
                return (Constraints & GenericConstraints.AllowByRefLike) != 0;
            }
        }

        /// <summary>
        /// Is this generic parameter Covariant
        /// </summary>
        public bool IsCovariant
        {
            get
            {
                return (Variance & GenericVariance.Covariant) != 0;
            }
        }

        /// <summary>
        /// Is this generic parameter Contravariant
        /// </summary>
        public bool IsContravariant
        {
            get
            {
                return (Variance & GenericVariance.Contravariant) != 0;
            }
        }

        protected sealed override TypeFlags ComputeTypeFlags(TypeFlags mask)
        {
            TypeFlags flags = 0;

            flags |= TypeFlags.GenericParameter;

            flags |= TypeFlags.HasGenericVarianceComputed;

            flags |= TypeFlags.AttributeCacheComputed;

            return flags;
        }

        public sealed override int GetHashCode()
        {
            // TODO: Determine what a the right hash function should be. Use stable hashcode based on the type name?
            // For now, use the same hash as a SignatureVariable type.
            return Internal.NativeFormat.TypeHashingAlgorithms.ComputeSignatureVariableHashCode(Index, Kind == GenericParameterKind.Method);
        }
    }
}
