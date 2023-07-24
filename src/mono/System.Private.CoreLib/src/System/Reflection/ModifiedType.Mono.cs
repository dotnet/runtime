// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Reflection
{
    internal partial class ModifiedType
    {
        internal struct TypeSignature
        {
        }

#pragma warning disable IDE0060
        internal Type GetTypeParameter(Type unmodifiedType, int index) => throw new NotSupportedException();

        internal SignatureCallingConvention GetCallingConventionFromFunctionPointer() => throw new NotSupportedException();

        private Type[] GetCustomModifiers(bool required) => throw new NotSupportedException();
#pragma warning restore IDE0060
    }
}
