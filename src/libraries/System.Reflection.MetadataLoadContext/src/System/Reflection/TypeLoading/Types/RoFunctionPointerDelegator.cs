// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Reflection.TypeLoading
{
    // The implementation of RoTypeDelegator for function pointers.
    internal sealed class RoFunctionPointerDelegator : RoTypeDelegator
    {
        internal List<Type>? _requiredModifiers;
        internal List<Type>? _optionalModifiers;

        public RoFunctionPointerDelegator(RoType actualType) : base(actualType) { }

        public void AddRequiredModifier(Type type)
        {
            _requiredModifiers ??= new List<Type>();
            _requiredModifiers.Add(type);
        }
        public void AddOptionalModifier(Type type)
        {
            _optionalModifiers ??= new List<Type>();
            _optionalModifiers.Add(type);
        }
    }
}
