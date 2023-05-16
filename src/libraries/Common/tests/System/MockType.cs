// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;

namespace System
{
    internal class MockType : Type
    {
        public override Assembly Assembly => throw Unexpected;
        public override string AssemblyQualifiedName => throw Unexpected;
        public override Type BaseType => throw Unexpected;
        public override string FullName => throw Unexpected;
        public override Guid GUID => throw Unexpected;
        public override Module Module => throw Unexpected;
        public override string Namespace => throw Unexpected;
        public override Type UnderlyingSystemType => throw Unexpected;
        public override string Name => throw Unexpected;
        public override ConstructorInfo[] GetConstructors(BindingFlags bindingAttr) => throw Unexpected;
        public override object[] GetCustomAttributes(bool inherit) => throw Unexpected;
        public override object[] GetCustomAttributes(Type attributeType, bool inherit) => throw Unexpected;
        public override Type GetElementType() => throw Unexpected;
        public override EventInfo GetEvent(string name, BindingFlags bindingAttr) => throw Unexpected;
        public override EventInfo[] GetEvents(BindingFlags bindingAttr) => throw Unexpected;
        public override FieldInfo GetField(string name, BindingFlags bindingAttr) => throw Unexpected;
        public override FieldInfo[] GetFields(BindingFlags bindingAttr) => throw Unexpected;
        public override Type GetInterface(string name, bool ignoreCase) => throw Unexpected;
        public override Type[] GetInterfaces() => throw Unexpected;
        public override MemberInfo[] GetMembers(BindingFlags bindingAttr) => throw Unexpected;
        public override MethodInfo[] GetMethods(BindingFlags bindingAttr) => throw Unexpected;
        public override Type GetNestedType(string name, BindingFlags bindingAttr) => throw Unexpected;
        public override Type[] GetNestedTypes(BindingFlags bindingAttr) => throw Unexpected;
        public override PropertyInfo[] GetProperties(BindingFlags bindingAttr) => throw Unexpected;
        public override object InvokeMember(string name, BindingFlags invokeAttr, Binder binder, object target, object[] args, ParameterModifier[] modifiers, CultureInfo culture, string[] namedParameters) => throw Unexpected;
        public override bool IsDefined(Type attributeType, bool inherit) => throw Unexpected;
        protected override TypeAttributes GetAttributeFlagsImpl() => throw Unexpected;
        protected override ConstructorInfo GetConstructorImpl(BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers) => throw Unexpected;
        protected override MethodInfo GetMethodImpl(string name, BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers) => throw Unexpected;
        protected override PropertyInfo GetPropertyImpl(string name, BindingFlags bindingAttr, Binder binder, Type returnType, Type[] types, ParameterModifier[] modifiers) => throw Unexpected;
        protected override bool HasElementTypeImpl() => throw Unexpected;
        protected override bool IsArrayImpl() => throw Unexpected;
        protected override bool IsByRefImpl() => throw Unexpected;
        protected override bool IsCOMObjectImpl() => throw Unexpected;
        protected override bool IsPointerImpl() => throw Unexpected;
        protected override bool IsPrimitiveImpl() => throw Unexpected;
        public override bool ContainsGenericParameters => throw Unexpected;
        public override IEnumerable<CustomAttributeData> CustomAttributes => throw Unexpected;
        public override MethodBase DeclaringMethod => throw Unexpected;
        public override Type DeclaringType => throw Unexpected;
        public override bool Equals(object o) => throw Unexpected;
        public override bool Equals(Type o) => throw Unexpected;
        public override Type[] FindInterfaces(TypeFilter filter, object filterCriteria) => throw Unexpected;
        public override MemberInfo[] FindMembers(MemberTypes memberType, BindingFlags bindingAttr, MemberFilter filter, object filterCriteria) => throw Unexpected;
        public override GenericParameterAttributes GenericParameterAttributes => throw Unexpected;
        public override int GenericParameterPosition => throw Unexpected;
        public override Type[] GenericTypeArguments => throw Unexpected;
        public override int GetArrayRank() => throw Unexpected;
        public override IList<CustomAttributeData> GetCustomAttributesData() => throw Unexpected;
        public override MemberInfo[] GetDefaultMembers() => throw Unexpected;
        public override string GetEnumName(object value) => throw Unexpected;
        public override string[] GetEnumNames() => throw Unexpected;
        public override Type GetEnumUnderlyingType() => throw Unexpected;
        public override Array GetEnumValues() => throw Unexpected;
        public override EventInfo[] GetEvents() => throw Unexpected;
        public override Type[] GetGenericArguments() => throw Unexpected;
        public override Type[] GetGenericParameterConstraints() => throw Unexpected;
        public override Type GetGenericTypeDefinition() => throw Unexpected;
        public override int GetHashCode() => throw Unexpected;
        public override InterfaceMapping GetInterfaceMap(Type interfaceType) => throw Unexpected;
        public override MemberInfo[] GetMember(string name, BindingFlags bindingAttr) => throw Unexpected;
        public override MemberInfo[] GetMember(string name, MemberTypes type, BindingFlags bindingAttr) => throw Unexpected;
        protected override TypeCode GetTypeCodeImpl() => throw Unexpected;
        public override bool IsConstructedGenericType => throw Unexpected;
        protected override bool IsContextfulImpl() => throw Unexpected;
        public override bool IsEnum => throw Unexpected;
        public override bool IsEnumDefined(object value) => throw Unexpected;
        public override bool IsEquivalentTo(Type other) => throw Unexpected;
        public override bool IsGenericParameter => throw Unexpected;
        public override bool IsGenericType => throw Unexpected;
        public override bool IsGenericTypeDefinition => throw Unexpected;
        public override bool IsInstanceOfType(object o) => throw Unexpected;
        protected override bool IsMarshalByRefImpl() => throw Unexpected;
        public override bool IsSecurityCritical => throw Unexpected;
        public override bool IsSecuritySafeCritical => throw Unexpected;
        public override bool IsSecurityTransparent => throw Unexpected;
#if NET8_0_OR_GREATER
        [Obsolete("Formatter-based serialization is obsolete and should not be used.", DiagnosticId = "SYSLIB0050", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
#endif
        public override bool IsSerializable => throw Unexpected;
        public override bool IsSubclassOf(Type c) => throw Unexpected;
        protected override bool IsValueTypeImpl() => throw Unexpected;
        public override Type MakeArrayType() => throw Unexpected;
        public override Type MakeArrayType(int rank) => throw Unexpected;
        public override Type MakeByRefType() => throw Unexpected;
        public override Type MakeGenericType(params Type[] typeArguments) => throw Unexpected;
        public override Type MakePointerType() => throw Unexpected;
        public override MemberTypes MemberType => throw Unexpected;
        public override int MetadataToken => throw Unexpected;
        public override Type ReflectedType => throw Unexpected;
        public override StructLayoutAttribute StructLayoutAttribute => throw Unexpected;
        public override string ToString() => throw Unexpected;
        public override RuntimeTypeHandle TypeHandle => throw Unexpected;

        protected virtual Exception Unexpected => new Exception("Did not expect to be called.");
    }
}
