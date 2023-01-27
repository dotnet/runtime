// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.TypeLoading;

namespace System.Reflection
{
    /// <summary>
    /// Base type for modified types obtained through FieldInfo.GetModifiedFieldInfo(), PropertyInfo.GetModifiedPropertyInfo()
    /// and ParameterInfo.GetModifiedParameterInfo().
    /// </summary>
    internal abstract class RoModifiedType : RoTypeDelegator
    {
        private List<Type>? _requiredModifiersBuilder;
        private List<Type>? _optionalModifiersBuilder;
        private Type[]? _requiredModifiers;
        private Type[]? _optionalModifiers;

        protected RoModifiedType(RoType unmodifiedType) : base(unmodifiedType) { }

        public static RoModifiedType Create(RoType unmodifiedType)
        {
            RoModifiedType modifiedType;

            if (unmodifiedType is RoModifiedType mod)
            {
                // A nested type, such as a function pointer in 'delegate*<void>[]' array, may already be modified.
                modifiedType = mod;
            }
            else if (unmodifiedType.IsFunctionPointer)
            {
                modifiedType = new RoModifiedFunctionPointerType((RoFunctionPointerType)unmodifiedType);
            }
            else if (unmodifiedType.IsGenericType)
            {
                modifiedType = new RoModifiedGenericType((RoConstructedGenericType)unmodifiedType);
            }
            else if (unmodifiedType.HasElementType)
            {
                modifiedType = new RoModifiedHasElementType(unmodifiedType);
            }
            else
            {
                modifiedType = new RoModifiedStandaloneType(unmodifiedType);
            }

            return modifiedType;
        }

        public void AddRequiredModifier(Type type)
        {
            Debug.Assert(_requiredModifiers == null);
            _requiredModifiersBuilder ??= new List<Type>();
            _requiredModifiersBuilder.Add(type);
        }

        public void AddOptionalModifier(Type type)
        {
            Debug.Assert(_optionalModifiers == null);
            _optionalModifiersBuilder ??= new List<Type>();
            _optionalModifiersBuilder.Add(type);
        }

        public override Type[] GetRequiredCustomModifiers()
        {
            if (_requiredModifiers == null)
            {
                if (_requiredModifiersBuilder == null)
                {
                    _requiredModifiers = EmptyTypes;
                }
                else
                {
                    _requiredModifiers = _requiredModifiersBuilder.ToArray();
                    _requiredModifiersBuilder = null;
                }
            }

            return Helpers.CloneArray(_requiredModifiers);
        }

        public override Type[] GetOptionalCustomModifiers()
        {
            if (_optionalModifiers == null)
            {
                if (_optionalModifiersBuilder == null)
                {
                    _optionalModifiers = EmptyTypes;
                }
                else
                {
                    _optionalModifiers = _optionalModifiersBuilder.ToArray();
                    _optionalModifiersBuilder = null;
                }
            }

            return Helpers.CloneArray(_optionalModifiers);
        }

        public sealed override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (!(obj is RoFunctionPointerType otherModifiedType))
                return false;

            return ReferenceEquals(this, otherModifiedType);
        }

        public override int GetHashCode() => base.GetHashCode();

        // TypeDelegator doesn't forward the way we want.
        public override Type UnderlyingSystemType => TypeImpl;
        protected sealed override RoType? ComputeDeclaringType() => null;
    }
}
