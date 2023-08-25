// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.Reflection.Emit
{
    public abstract partial class EnumBuilder : TypeInfo
    {
        /// <summary>
        /// Initializes a new instance of <see cref="EnumBuilder"/> class.
        /// </summary>
        /// <remarks>
        /// This constructor is invoked by derived classes.
        /// </remarks>
        protected EnumBuilder()
        {
        }

        public FieldBuilder UnderlyingField
            => UnderlyingFieldCore;

        /// <summary>
        /// When overridden in a derived class, returns the underlying field for this enum.
        /// </summary>
        /// <value>Read-only. The underlying field for this enum.</value>
        protected abstract FieldBuilder UnderlyingFieldCore { get; }

        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        public Type CreateType()
            => CreateTypeInfoCore();

        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        public TypeInfo CreateTypeInfo()
            => CreateTypeInfoCore();

        /// <summary>
        /// When overridden in a derived class, gets a <see cref="TypeInfo"/> object that represents this enumeration.
        /// </summary>
        /// <returns>A <see cref="TypeInfo"/> object that represents this enumeration.</returns>
        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        protected abstract TypeInfo CreateTypeInfoCore();

        public FieldBuilder DefineLiteral(string literalName, object? literalValue)
            => DefineLiteralCore(literalName, literalValue);

        /// <summary>
        /// When overridden in a derived class, defines the named static field in an enumeration type with the specified constant value.
        /// </summary>
        /// <param name="literalName">The name of the static field.</param>
        /// <param name="literalValue">The constant value of the literal.</param>
        /// <returns>The defined field.</returns>
        protected abstract FieldBuilder DefineLiteralCore(string literalName, object? literalValue);

        public void SetCustomAttribute(ConstructorInfo con, byte[] binaryAttribute)
            => SetCustomAttributeCore(con, binaryAttribute);

        /// <summary>
        /// When overridden in a derived class, sets a custom attribute on this assembly.
        /// </summary>
        /// <param name="con">The constructor for the custom attribute.</param>
        /// <param name="binaryAttribute">A <see cref="ReadOnlySpan{T}"/> of bytes representing the attribute.</param>
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
