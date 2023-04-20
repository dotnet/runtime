// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection.Metadata;

namespace System.Reflection.Emit
{
    internal sealed class TypeDefinitionWrapper
    {
        internal TypeBuilderImpl typeBuilder;
        internal TypeDefinitionHandle handle;

        public TypeDefinitionWrapper(TypeBuilderImpl typeBuilder, TypeDefinitionHandle handle)
        {
            this.typeBuilder = typeBuilder;
            this.handle = handle;
        }
    }
}
