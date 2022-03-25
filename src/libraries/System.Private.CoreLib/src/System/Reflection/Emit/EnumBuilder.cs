// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.Reflection.Emit
{
    public abstract partial class EnumBuilder : TypeInfo
    {
        protected EnumBuilder()
        {
        }

        public virtual FieldBuilder UnderlyingField
            => UnderlyingField;

        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        public Type? CreateType()
            => CreateTypeInfo();

        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        public virtual TypeInfo? CreateTypeInfo()
            => CreateTypeInfo();

        public virtual FieldBuilder DefineLiteral(string literalName, object literalValue)
            => DefineLiteral(literalName, literalValue);

        public virtual void SetCustomAttribute(ConstructorInfo con, byte[] binaryAttribute)
            => SetCustomAttribute(con, binaryAttribute);

        public virtual void SetCustomAttribute(CustomAttributeBuilder customBuilder)
            => SetCustomAttribute(customBuilder);
    }
}
