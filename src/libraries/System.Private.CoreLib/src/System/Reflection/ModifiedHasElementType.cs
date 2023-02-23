// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Threading;

namespace System.Reflection
{
    /// <summary>
    /// An array, pointer or reference type.
    /// </summary>
    internal sealed class ModifiedHasElementType : ModifiedType
    {
        private Type? _elementType;

        internal ModifiedHasElementType(Type unmodifiedType, TypeSignature typeSignature)
            : base(unmodifiedType, typeSignature)
        {
            Debug.Assert(unmodifiedType.HasElementType);
        }

        public override Type? GetElementType()
        {
            return _elementType ?? Initialize();

            Type Initialize()
            {
                Interlocked.CompareExchange(ref _elementType, GetTypeParameter(UnmodifiedType.GetElementType()!, 0), null);
                return _elementType!;
            }
        }
    }
}
