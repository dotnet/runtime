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

        public override Type MakePointerType()
        {
            return SymbolType.FormCompoundType("*", this, 0)!;
        }

        public override Type MakeByRefType()
        {
            return SymbolType.FormCompoundType("&", this, 0)!;
        }

        [RequiresDynamicCode("The code for an array of the specified type might not be available.")]
        public override Type MakeArrayType()
        {
            return SymbolType.FormCompoundType("[]", this, 0)!;
        }

        [RequiresDynamicCode("The code for an array of the specified type might not be available.")]
        public override Type MakeArrayType(int rank)
        {
            string s = GetRankString(rank);
            return SymbolType.FormCompoundType(s, this, 0)!;
        }
    }
}
