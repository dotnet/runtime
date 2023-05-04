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

        public FieldBuilder UnderlyingField
            => UnderlyingFieldCore;

        protected abstract FieldBuilder UnderlyingFieldCore { get; }

        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        public Type CreateType()
            => CreateTypeInfoCore();

        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        public TypeInfo CreateTypeInfo()
            => CreateTypeInfoCore();

        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        protected abstract TypeInfo CreateTypeInfoCore();

        public FieldBuilder DefineLiteral(string literalName, object? literalValue)
            => DefineLiteralCore(literalName, literalValue);

        protected abstract FieldBuilder DefineLiteralCore(string literalName, object? literalValue);

        public void SetCustomAttribute(ConstructorInfo con, byte[] binaryAttribute)
            => SetCustomAttributeCore(con, binaryAttribute);

        protected abstract void SetCustomAttributeCore(ConstructorInfo con, ReadOnlySpan<byte> binaryAttribute);

        public void SetCustomAttribute(CustomAttributeBuilder customBuilder)
            => SetCustomAttributeCore(customBuilder.Ctor, customBuilder.Data);
    }
}
