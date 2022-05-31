// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Reflection.TypeLoading;

namespace System.Reflection
{
    /// <summary>
    /// A function pointer type that is modified and maintains its inner modified parameter types and return type.
    /// </summary>
    internal sealed class RoModifiedGenericType : RoModifiedType
    {
        private readonly RoModifiedType[] _argumentTypes;

        public RoModifiedGenericType(RoConstructedGenericType genericType) : base(genericType)
        {
            Debug.Assert(genericType.IsGenericType);

            Type[] unmodifiedTypes = genericType.GetGenericTypeArgumentsNoCopy();
            int count = unmodifiedTypes.Length;

            RoModifiedType[] argumentTypes = new RoModifiedType[count];
            for (int i = 0; i < count; i++)
            {
                RoModifiedType argument = Create((RoType)unmodifiedTypes[i]);
                argumentTypes[i] = argument;
            }

            _argumentTypes = argumentTypes;
        }

        protected internal sealed override RoType[] GetGenericArgumentsNoCopy() => _argumentTypes;
    }
}
