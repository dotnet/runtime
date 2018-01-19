// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Reflection
{
    internal sealed class SignatureGenericMethodParameterType : SignatureGenericParameterType
    {
        internal SignatureGenericMethodParameterType(int position)
            : base(position)
        {
        }

        public sealed override bool IsGenericTypeParameter => false;
        public sealed override bool IsGenericMethodParameter => true;
    
        public sealed override string Name => "!!" + GenericParameterPosition;
    }
}
