// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Reflection.Emit
{
    internal sealed class CustomAttributeWrapper
    {
        internal ConstructorInfo constructorInfo;
        internal byte[] binaryAttribute;

        public CustomAttributeWrapper(ConstructorInfo constructorInfo, byte[] binaryAttribute)
        {
            this.constructorInfo = constructorInfo;
            this.binaryAttribute = binaryAttribute;
        }
    }
}
