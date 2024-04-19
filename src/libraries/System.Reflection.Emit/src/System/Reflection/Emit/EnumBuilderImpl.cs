// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Reflection.Metadata;

namespace System.Reflection.Emit
{
    internal sealed class EnumBuilderImpl : EnumBuilder
    {
        private readonly FieldBuilder _underlyingField;
        internal readonly TypeBuilderImpl _typeBuilder;

        internal EnumBuilderImpl(string name, Type underlyingType, TypeAttributes visibility, ModuleBuilderImpl module)
        {
            if ((visibility & ~TypeAttributes.VisibilityMask) != 0)
                throw new ArgumentException(SR.Argument_ShouldOnlySetVisibilityFlags, nameof(name));

            _typeBuilder = new TypeBuilderImpl(name, visibility | TypeAttributes.Sealed, typeof(Enum), module,
                interfaces: null, PackingSize.Unspecified, TypeBuilder.UnspecifiedTypeSize, enclosingType: null);

            // Define the underlying field for the enum. It will be a non-static, private field with special name bit set.
            _underlyingField = _typeBuilder.DefineField("value__", underlyingType, FieldAttributes.Public | FieldAttributes.SpecialName | FieldAttributes.RTSpecialName);
        }

        protected override FieldBuilder UnderlyingFieldCore => _underlyingField;

        [return: DynamicallyAccessedMembers((DynamicallyAccessedMemberTypes)(-1))]
        protected override TypeInfo CreateTypeInfoCore() => _typeBuilder.CreateTypeInfo();

        protected override FieldBuilder DefineLiteralCore(string literalName, object? literalValue)
        {
            FieldBuilder fieldBuilder = _typeBuilder.DefineField(
                literalName,
                this,
                FieldAttributes.Public | FieldAttributes.Static | FieldAttributes.Literal);
            fieldBuilder.SetConstant(literalValue);
            return fieldBuilder;
        }

        protected override void SetCustomAttributeCore(ConstructorInfo con, ReadOnlySpan<byte> binaryAttribute) =>
            _typeBuilder.SetCustomAttribute(con, binaryAttribute);

        public override Guid GUID => _typeBuilder.GUID;

        public override string Name => _typeBuilder.Name;

        public override Module Module => _typeBuilder.Module;

        public override Assembly Assembly => _typeBuilder.Assembly;

        public override RuntimeTypeHandle TypeHandle => _typeBuilder.TypeHandle;

        public override string? FullName => _typeBuilder.FullName;

        public override string? AssemblyQualifiedName => _typeBuilder.AssemblyQualifiedName;

        public override string? Namespace => _typeBuilder.Namespace;

        public override Type? BaseType => _typeBuilder.BaseType;

        public override bool IsByRefLike => false;

        public override bool IsTypeDefinition => true;

        public override bool IsSZArray => false;

        public override bool IsConstructedGenericType => false;

        public override Type? DeclaringType => _typeBuilder.DeclaringType;

        public override Type? ReflectedType => _typeBuilder.ReflectedType;

        public override Type UnderlyingSystemType => GetEnumUnderlyingType();

        public override Type GetEnumUnderlyingType() => _underlyingField.FieldType;

        protected override bool IsArrayImpl() => false;

        protected override bool IsPrimitiveImpl() => false;

        protected override bool IsValueTypeImpl() => true;

        protected override bool IsByRefImpl() => false;

        protected override bool IsPointerImpl() => false;

        protected override bool IsCOMObjectImpl() => false;

        public override Type? GetElementType() => _typeBuilder.GetElementType();

        protected override bool HasElementTypeImpl() => _typeBuilder.HasElementType;

        protected override TypeAttributes GetAttributeFlagsImpl() => _typeBuilder.Attributes;

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
        public override Type[] GetInterfaces() => EmptyTypes;

        public override bool IsDefined(Type attributeType, bool inherit) => throw new NotImplementedException();
        public override object[] GetCustomAttributes(bool inherit) => throw new NotImplementedException();
        public override object[] GetCustomAttributes(Type attributeType, bool inherit) => throw new NotImplementedException();
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        public override object? InvokeMember(string name, BindingFlags invokeAttr, Binder? binder, object? target,
                    object?[]? args, ParameterModifier[]? modifiers, Globalization.CultureInfo? culture, string[]? namedParameters) => throw new NotSupportedException();
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
        protected override ConstructorInfo? GetConstructorImpl(BindingFlags bindingAttr, Binder? binder,
                CallingConventions callConvention, Type[] types, ParameterModifier[]? modifiers) => throw new NotSupportedException();
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
        public override ConstructorInfo[] GetConstructors(BindingFlags bindingAttr) => throw new NotSupportedException();
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents)]
        public override EventInfo[] GetEvents() => throw new NotSupportedException();
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents)]
        public override EventInfo[] GetEvents(BindingFlags bindingAttr) => throw new NotSupportedException();
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents)]
        public override EventInfo? GetEvent(string name, BindingFlags bindingAttr) => throw new NotSupportedException();
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
        public override MethodInfo[] GetMethods(BindingFlags bindingAttr) => throw new NotSupportedException();
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
        protected override MethodInfo? GetMethodImpl(string name, BindingFlags bindingAttr, Binder? binder,
                CallingConventions callConvention, Type[]? types, ParameterModifier[]? modifiers) => throw new NotSupportedException();
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)]
        public override FieldInfo? GetField(string name, BindingFlags bindingAttr) => throw new NotSupportedException();
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)]
        public override FieldInfo[] GetFields(BindingFlags bindingAttr) => throw new NotSupportedException();
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
        public override Type? GetInterface(string name, bool ignoreCase) => throw new NotSupportedException();
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
        public override PropertyInfo[] GetProperties(BindingFlags bindingAttr) => throw new NotSupportedException();
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
        protected override PropertyInfo GetPropertyImpl(string name, BindingFlags bindingAttr, Binder? binder,
                Type? returnType, Type[]? types, ParameterModifier[]? modifiers) => throw new NotSupportedException();
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.NonPublicNestedTypes)]
        public override Type[] GetNestedTypes(BindingFlags bindingAttr) => throw new NotSupportedException();
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.NonPublicNestedTypes)]
        public override Type? GetNestedType(string name, BindingFlags bindingAttr) => throw new NotSupportedException();
        [DynamicallyAccessedMembers(TypeBuilderImpl.GetAllMembers)]
        public override MemberInfo[] GetMember(string name, MemberTypes type, BindingFlags bindingAttr) => throw new NotSupportedException();
        public override InterfaceMapping GetInterfaceMap([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)] Type interfaceType)
            => throw new NotSupportedException();
        [DynamicallyAccessedMembers(TypeBuilderImpl.GetAllMembers)]
        public override MemberInfo[] GetMembers(BindingFlags bindingAttr) => throw new NotSupportedException();
        public override bool IsAssignableFrom([NotNullWhen(true)] Type? c) => throw new NotSupportedException();
    }
}
