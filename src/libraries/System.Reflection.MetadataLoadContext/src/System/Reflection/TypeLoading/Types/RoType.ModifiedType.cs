// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Reflection.TypeLoading
{
    internal partial class RoType
    {
        private List<Type>? _requiredModifiersBuilder;
        private List<Type>? _optionalModifiersBuilder;
        private Type[]? _requiredModifiers;
        private Type[]? _optionalModifiers;

        public void InitializeModifiers(Type[] requiredModifiers, Type[] optionalModifiers)
        {
            Debug.Assert(_requiredModifiersBuilder == null );
            Debug.Assert(_optionalModifiersBuilder == null);
            Debug.Assert(_requiredModifiers == null);
            Debug.Assert(_optionalModifiers == null);

            _requiredModifiers = requiredModifiers;
            _optionalModifiers = optionalModifiers;
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
    }
}
