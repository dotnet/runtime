// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Threading;

namespace System.Reflection
{
    internal sealed partial class ModifiedGenericType : ModifiedType
    {
        private Type[]? _genericArguments;

        internal ModifiedGenericType(Type unmodifiedType, TypeSignature typeSignature)
            : base(unmodifiedType, typeSignature)
        {
            Debug.Assert(unmodifiedType.IsGenericType);
        }

        public override Type[] GetGenericArguments()
        {
            return (Type[])(_genericArguments ?? Initialize()).Clone();

            Type[] Initialize()
            {
                Type[] genericArguments = UnmodifiedType.GetGenericArguments();
                for (int i = 0; i < genericArguments.Length; i++)
                {
                    genericArguments[i] = GetTypeParameter(genericArguments[i], i);
                }
                Interlocked.CompareExchange(ref _genericArguments, genericArguments, null);
                return _genericArguments!;
            }
        }
    }
}
