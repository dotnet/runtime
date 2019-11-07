// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Reflection;

namespace System
{
    public class NonRuntimeType : Type
    {
        public override Assembly Assembly => null;
        public override string AssemblyQualifiedName => null;
        public override Type BaseType => null;
        public override string FullName => null;
        public override Guid GUID => Guid.Empty;
        public override Module Module => null;
        public override string Namespace => null;
        public override Type UnderlyingSystemType => null;
        public override string Name => null;
        public override ConstructorInfo[] GetConstructors(BindingFlags bindingAttr) => null;
        public override object[] GetCustomAttributes(bool inherit) => null;
        public override object[] GetCustomAttributes(Type attributeType, bool inherit) => null;
        public override Type GetElementType() => null;
        public override EventInfo GetEvent(string name, BindingFlags bindingAttr) => null;
        public override EventInfo[] GetEvents(BindingFlags bindingAttr) => null;
        public override FieldInfo GetField(string name, BindingFlags bindingAttr) => null;
        public override FieldInfo[] GetFields(BindingFlags bindingAttr) => null;
        public override Type GetInterface(string name, bool ignoreCase) => null;
        public override Type[] GetInterfaces() => null;
        public override MemberInfo[] GetMembers(BindingFlags bindingAttr) => null;
        public override MethodInfo[] GetMethods(BindingFlags bindingAttr) => null;
        public override Type GetNestedType(string name, BindingFlags bindingAttr) => null;
        public override Type[] GetNestedTypes(BindingFlags bindingAttr) => null;
        public override PropertyInfo[] GetProperties(BindingFlags bindingAttr) => null;
        public override object InvokeMember(string name, BindingFlags invokeAttr, Binder binder, object target, object[] args, ParameterModifier[] modifiers, CultureInfo culture, string[] namedParameters) => null;
        public override bool IsDefined(Type attributeType, bool inherit) => false;
        protected override TypeAttributes GetAttributeFlagsImpl() => TypeAttributes.NotPublic;
        protected override ConstructorInfo GetConstructorImpl(BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers) => null;
        protected override MethodInfo GetMethodImpl(string name, BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers) => null;
        protected override PropertyInfo GetPropertyImpl(string name, BindingFlags bindingAttr, Binder binder, Type returnType, Type[] types, ParameterModifier[] modifiers) => null;
        protected override bool HasElementTypeImpl() => false;
        protected override bool IsArrayImpl() => false;
        protected override bool IsByRefImpl() => false;
        protected override bool IsCOMObjectImpl() => false;
        protected override bool IsPointerImpl() => false;
        protected override bool IsPrimitiveImpl() => false;
    }
}