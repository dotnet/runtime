// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;

namespace System.Reflection.Emit
{
    public sealed class GenericTypeParameterBuilder : TypeInfo
    {
        public override bool IsAssignableFrom(System.Reflection.TypeInfo? typeInfo)
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
        public override Type? DeclaringType { get { return m_type.DeclaringType; } }

        public override Type? ReflectedType { get { return m_type.ReflectedType; } }

        public override string Name { get { return m_type.Name; } }

        public override Module Module { get { return m_type.Module; } }

        internal int MetadataTokenInternal { get { return m_type.MetadataTokenInternal; } }
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

        public override Type MakeArrayType()
        {
            return SymbolType.FormCompoundType("[]", this, 0)!;
        }

        public override Type MakeArrayType(int rank)
        {
            string s = GetRankString(rank);
            SymbolType? st = SymbolType.FormCompoundType(s, this, 0) as SymbolType;
            return st!;
        }

        public override Guid GUID { get { throw new NotSupportedException(); } }

        public override object InvokeMember(string name, BindingFlags invokeAttr, Binder? binder, object? target, object?[]? args, ParameterModifier[]? modifiers, CultureInfo? culture, string[]? namedParameters) { throw new NotSupportedException(); }

        public override Assembly Assembly { get { return m_type.Assembly; } }

        public override RuntimeTypeHandle TypeHandle { get { throw new NotSupportedException(); } }

        public override string? FullName { get { return null; } }

        public override string? Namespace { get { return null; } }

        public override string? AssemblyQualifiedName { get { return null; } }

        public override Type? BaseType { get { return m_type.BaseType; } }

        protected override ConstructorInfo GetConstructorImpl(BindingFlags bindingAttr, Binder? binder, CallingConventions callConvention, Type[] types, ParameterModifier[]? modifiers) { throw new NotSupportedException(); }

        public override ConstructorInfo[] GetConstructors(BindingFlags bindingAttr) { throw new NotSupportedException(); }

        protected override MethodInfo GetMethodImpl(string name, BindingFlags bindingAttr, Binder? binder, CallingConventions callConvention, Type[]? types, ParameterModifier[]? modifiers) { throw new NotSupportedException(); }

        public override MethodInfo[] GetMethods(BindingFlags bindingAttr) { throw new NotSupportedException(); }

        public override FieldInfo GetField(string name, BindingFlags bindingAttr) { throw new NotSupportedException(); }

        public override FieldInfo[] GetFields(BindingFlags bindingAttr) { throw new NotSupportedException(); }

        public override Type GetInterface(string name, bool ignoreCase) { throw new NotSupportedException(); }

        public override Type[] GetInterfaces() { throw new NotSupportedException(); }

        public override EventInfo GetEvent(string name, BindingFlags bindingAttr) { throw new NotSupportedException(); }

        public override EventInfo[] GetEvents() { throw new NotSupportedException(); }

        protected override PropertyInfo GetPropertyImpl(string name, BindingFlags bindingAttr, Binder? binder, Type? returnType, Type[]? types, ParameterModifier[]? modifiers) { throw new NotSupportedException(); }

        public override PropertyInfo[] GetProperties(BindingFlags bindingAttr) { throw new NotSupportedException(); }

        public override Type[] GetNestedTypes(BindingFlags bindingAttr) { throw new NotSupportedException(); }

        public override Type GetNestedType(string name, BindingFlags bindingAttr) { throw new NotSupportedException(); }

        public override MemberInfo[] GetMember(string name, MemberTypes type, BindingFlags bindingAttr) { throw new NotSupportedException(); }

        public override InterfaceMapping GetInterfaceMap(Type interfaceType) { throw new NotSupportedException(); }

        public override EventInfo[] GetEvents(BindingFlags bindingAttr) { throw new NotSupportedException(); }

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

        public override Type UnderlyingSystemType { get { return this; } }

        public override Type[] GetGenericArguments() { throw new InvalidOperationException(); }

        public override bool IsGenericTypeDefinition { get { return false; } }

        public override bool IsGenericType { get { return false; } }

        public override bool IsGenericParameter { get { return true; } }

        public override bool IsConstructedGenericType { get { return false; } }

        public override int GenericParameterPosition { get { return m_type.GenericParameterPosition; } }

        public override bool ContainsGenericParameters { get { return m_type.ContainsGenericParameters; } }

        public override GenericParameterAttributes GenericParameterAttributes { get { return m_type.GenericParameterAttributes; } }

        public override MethodBase? DeclaringMethod { get { return m_type.DeclaringMethod; } }

        public override Type GetGenericTypeDefinition() { throw new InvalidOperationException(); }

        public override Type MakeGenericType(params Type[] typeArguments) { throw new InvalidOperationException(SR.Format(SR.Arg_NotGenericTypeDefinition, this)); }

        protected override bool IsValueTypeImpl() { return false; }

        public override bool IsAssignableFrom(Type? c) { throw new NotSupportedException(); }

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

        public void SetBaseTypeConstraint(Type? baseTypeConstraint)
        {
            m_type.CheckContext(baseTypeConstraint);
            m_type.SetParent(baseTypeConstraint);
        }

        public void SetInterfaceConstraints(params Type[]? interfaceConstraints)
        {
            m_type.CheckContext(interfaceConstraints);
            m_type.SetInterfaces(interfaceConstraints);
        }

        public void SetGenericParameterAttributes(GenericParameterAttributes genericParameterAttributes)
        {
            m_type.SetGenParamAttributes(genericParameterAttributes);
        }
        #endregion
    }
}

