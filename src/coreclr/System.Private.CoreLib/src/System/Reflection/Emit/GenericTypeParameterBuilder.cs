// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace System.Reflection.Emit
{
    public sealed class GenericTypeParameterBuilder : TypeInfo
    {
        public override bool IsAssignableFrom([NotNullWhen(true)] TypeInfo? typeInfo)
        {
            if (typeInfo == null) return false;
            return IsAssignableFrom(typeInfo.AsType());
        }

        #region Private Data Members
        internal TypeBuilder m_type;
        #endregion

        #region Constructor
        internal GenericTypeParameterBuilder(TypeBuilder type)
        {
            m_type = type;
        }
        #endregion

        #region Object Overrides
        public override string ToString()
        {
            return m_type.Name;
        }
        public override bool Equals(object? o)
        {
            GenericTypeParameterBuilder? g = o as GenericTypeParameterBuilder;

            if (g == null)
                return false;

            return object.ReferenceEquals(g.m_type, m_type);
        }
        public override int GetHashCode() { return m_type.GetHashCode(); }
        #endregion

        #region MemberInfo Overrides
        public override Type? DeclaringType => m_type.DeclaringType;

        public override Type? ReflectedType => m_type.ReflectedType;

        public override string Name => m_type.Name;

        public override Module Module => m_type.Module;

        public override bool IsByRefLike => false;

        public override int MetadataToken => m_type.MetadataToken;
        #endregion

        #region Type Overrides

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
            SymbolType? st = SymbolType.FormCompoundType(s, this, 0) as SymbolType;
            return st!;
        }

        public override Guid GUID => throw new NotSupportedException();

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        public override object InvokeMember(string name, BindingFlags invokeAttr, Binder? binder, object? target, object?[]? args, ParameterModifier[]? modifiers, CultureInfo? culture, string[]? namedParameters) { throw new NotSupportedException(); }

        public override Assembly Assembly => m_type.Assembly;

        public override RuntimeTypeHandle TypeHandle => throw new NotSupportedException();

        public override string? FullName => null;

        public override string? Namespace => null;

        public override string? AssemblyQualifiedName => null;

        public override Type? BaseType => m_type.BaseType;

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
        protected override ConstructorInfo GetConstructorImpl(BindingFlags bindingAttr, Binder? binder, CallingConventions callConvention, Type[] types, ParameterModifier[]? modifiers) { throw new NotSupportedException(); }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
        public override ConstructorInfo[] GetConstructors(BindingFlags bindingAttr) { throw new NotSupportedException(); }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
        protected override MethodInfo GetMethodImpl(string name, BindingFlags bindingAttr, Binder? binder, CallingConventions callConvention, Type[]? types, ParameterModifier[]? modifiers) { throw new NotSupportedException(); }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
        public override MethodInfo[] GetMethods(BindingFlags bindingAttr) { throw new NotSupportedException(); }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)]
        public override FieldInfo GetField(string name, BindingFlags bindingAttr) { throw new NotSupportedException(); }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)]
        public override FieldInfo[] GetFields(BindingFlags bindingAttr) { throw new NotSupportedException(); }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2063:UnrecognizedReflectionPattern",
            Justification = "Linker doesn't recognize always throwing method. https://github.com/mono/linker/issues/2025")]
        public override Type GetInterface(string name, bool ignoreCase) { throw new NotSupportedException(); }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
        public override Type[] GetInterfaces() { throw new NotSupportedException(); }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents)]
        public override EventInfo GetEvent(string name, BindingFlags bindingAttr) { throw new NotSupportedException(); }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents)]
        public override EventInfo[] GetEvents() { throw new NotSupportedException(); }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
        protected override PropertyInfo GetPropertyImpl(string name, BindingFlags bindingAttr, Binder? binder, Type? returnType, Type[]? types, ParameterModifier[]? modifiers) { throw new NotSupportedException(); }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
        public override PropertyInfo[] GetProperties(BindingFlags bindingAttr) { throw new NotSupportedException(); }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.NonPublicNestedTypes)]
        public override Type[] GetNestedTypes(BindingFlags bindingAttr) { throw new NotSupportedException(); }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.NonPublicNestedTypes)]
        public override Type GetNestedType(string name, BindingFlags bindingAttr) { throw new NotSupportedException(); }

        [DynamicallyAccessedMembers(GetAllMembers)]
        public override MemberInfo[] GetMember(string name, MemberTypes type, BindingFlags bindingAttr) { throw new NotSupportedException(); }

        public override InterfaceMapping GetInterfaceMap([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)] Type interfaceType) { throw new NotSupportedException(); }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents)]
        public override EventInfo[] GetEvents(BindingFlags bindingAttr) { throw new NotSupportedException(); }

        [DynamicallyAccessedMembers(GetAllMembers)]
        public override MemberInfo[] GetMembers(BindingFlags bindingAttr) { throw new NotSupportedException(); }

        protected override TypeAttributes GetAttributeFlagsImpl() { return TypeAttributes.Public; }

        public override bool IsTypeDefinition => false;

        public override bool IsSZArray => false;

        protected override bool IsArrayImpl() { return false; }

        protected override bool IsByRefImpl() { return false; }

        protected override bool IsPointerImpl() { return false; }

        protected override bool IsPrimitiveImpl() { return false; }

        protected override bool IsCOMObjectImpl() { return false; }

        public override Type GetElementType() { throw new NotSupportedException(); }

        protected override bool HasElementTypeImpl() { return false; }

        public override Type UnderlyingSystemType => this;

        public override Type[] GetGenericArguments() { throw new InvalidOperationException(); }

        public override bool IsGenericTypeDefinition => false;

        public override bool IsGenericType => false;

        public override bool IsGenericParameter => true;

        public override bool IsConstructedGenericType => false;

        public override int GenericParameterPosition => m_type.GenericParameterPosition;

        public override bool ContainsGenericParameters => m_type.ContainsGenericParameters;

        public override GenericParameterAttributes GenericParameterAttributes => m_type.GenericParameterAttributes;

        public override MethodBase? DeclaringMethod => m_type.DeclaringMethod;

        public override Type GetGenericTypeDefinition() { throw new InvalidOperationException(); }

        [RequiresDynamicCode("The native code for this instantiation might not be available at runtime.")]
        [RequiresUnreferencedCode("If some of the generic arguments are annotated (either with DynamicallyAccessedMembersAttribute, or generic constraints), trimming can't validate that the requirements of those annotations are met.")]
        public override Type MakeGenericType(params Type[] typeArguments) { throw new InvalidOperationException(SR.Format(SR.Arg_NotGenericTypeDefinition, this)); }

        protected override bool IsValueTypeImpl() { return false; }

        public override bool IsAssignableFrom([NotNullWhen(true)] Type? c) { throw new NotSupportedException(); }

        public override bool IsSubclassOf(Type c) { throw new NotSupportedException(); }
        #endregion

        #region ICustomAttributeProvider Implementation
        public override object[] GetCustomAttributes(bool inherit) { throw new NotSupportedException(); }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit) { throw new NotSupportedException(); }

        public override bool IsDefined(Type attributeType, bool inherit) { throw new NotSupportedException(); }
        #endregion

        #region Public Members
        public void SetCustomAttribute(ConstructorInfo con, byte[] binaryAttribute)
        {
            m_type.SetGenParamCustomAttribute(con, binaryAttribute);
        }

        public void SetCustomAttribute(CustomAttributeBuilder customBuilder)
        {
            m_type.SetGenParamCustomAttribute(customBuilder);
        }

        public void SetBaseTypeConstraint([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type? baseTypeConstraint)
        {
            m_type.SetParent(baseTypeConstraint);
        }

        public void SetInterfaceConstraints(params Type[]? interfaceConstraints)
        {
            m_type.SetInterfaces(interfaceConstraints);
        }

        public void SetGenericParameterAttributes(GenericParameterAttributes genericParameterAttributes)
        {
            m_type.SetGenParamAttributes(genericParameterAttributes);
        }
        #endregion
    }
}
