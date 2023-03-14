// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Reflection.Emit.Experiment
{
    /* The purpose of  this class is to provide wrappers for entities that are referenced in metadata.
        *  Most of the types from prototype removed as not needed anymore, in the future we might add Types with different shape.
        * */
    internal sealed class EntityWrappers
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
}
