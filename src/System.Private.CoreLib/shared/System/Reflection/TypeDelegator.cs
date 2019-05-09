// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// TypeDelegator
// 
// This class wraps a Type object and delegates all methods to that Type.

using CultureInfo = System.Globalization.CultureInfo;

namespace System.Reflection
{
    public class TypeDelegator : TypeInfo
    {
        public override bool IsAssignableFrom(TypeInfo? typeInfo)
        {
            if (typeInfo == null)
                return false;
            return IsAssignableFrom(typeInfo.AsType());
        }

        protected Type typeImpl = null!;

        protected TypeDelegator() { }

        public TypeDelegator(Type delegatingType)
        {
            if (delegatingType is null)
                throw new ArgumentNullException(nameof(delegatingType));

            typeImpl = delegatingType;
        }

        public override Guid GUID => typeImpl.GUID;
        public override int MetadataToken => typeImpl.MetadataToken;

        public override object? InvokeMember(string name, BindingFlags invokeAttr, Binder? binder, object? target,
            object[]? args, ParameterModifier[]? modifiers, CultureInfo? culture, string[]? namedParameters)
        {
            return typeImpl.InvokeMember(name, invokeAttr, binder, target, args, modifiers, culture, namedParameters);
        }

        public override Module Module => typeImpl.Module;
        public override Assembly Assembly => typeImpl.Assembly;
        public override RuntimeTypeHandle TypeHandle => typeImpl.TypeHandle;
        public override string Name => typeImpl.Name;
        public override string? FullName => typeImpl.FullName;
        public override string? Namespace => typeImpl.Namespace;
        public override string? AssemblyQualifiedName => typeImpl.AssemblyQualifiedName;
        public override Type? BaseType => typeImpl.BaseType;

        protected override ConstructorInfo? GetConstructorImpl(BindingFlags bindingAttr, Binder? binder,
                CallingConventions callConvention, Type[] types, ParameterModifier[]? modifiers)
        {
            return typeImpl.GetConstructor(bindingAttr, binder, callConvention, types, modifiers);
        }

        public override ConstructorInfo[] GetConstructors(BindingFlags bindingAttr) => typeImpl.GetConstructors(bindingAttr);

        protected override MethodInfo? GetMethodImpl(string name, BindingFlags bindingAttr, Binder? binder,
                CallingConventions callConvention, Type[]? types, ParameterModifier[]? modifiers)
        {
            // This is interesting there are two paths into the impl.  One that validates
            //  type as non-null and one where type may be null.
            if (types == null)
                return typeImpl.GetMethod(name, bindingAttr);
            else
                return typeImpl.GetMethod(name, bindingAttr, binder, callConvention, types, modifiers);
        }

        public override MethodInfo[] GetMethods(BindingFlags bindingAttr) => typeImpl.GetMethods(bindingAttr);

        public override FieldInfo? GetField(string name, BindingFlags bindingAttr) => typeImpl.GetField(name, bindingAttr);
        public override FieldInfo[] GetFields(BindingFlags bindingAttr) => typeImpl.GetFields(bindingAttr);

        public override Type? GetInterface(string name, bool ignoreCase) => typeImpl.GetInterface(name, ignoreCase);

        public override Type[] GetInterfaces() => typeImpl.GetInterfaces();

        public override EventInfo? GetEvent(string name, BindingFlags bindingAttr) => typeImpl.GetEvent(name, bindingAttr);

        public override EventInfo[] GetEvents() => typeImpl.GetEvents();

        protected override PropertyInfo? GetPropertyImpl(string name, BindingFlags bindingAttr, Binder? binder,
                        Type? returnType, Type[]? types, ParameterModifier[]? modifiers)
        {
            if (returnType == null && types == null)
                return typeImpl.GetProperty(name, bindingAttr);
            else
                return typeImpl.GetProperty(name, bindingAttr, binder, returnType, types!, modifiers);
        }

        public override PropertyInfo[] GetProperties(BindingFlags bindingAttr) => typeImpl.GetProperties(bindingAttr);
        public override EventInfo[] GetEvents(BindingFlags bindingAttr) => typeImpl.GetEvents(bindingAttr);
        public override Type[] GetNestedTypes(BindingFlags bindingAttr) => typeImpl.GetNestedTypes(bindingAttr);
        public override Type? GetNestedType(string name, BindingFlags bindingAttr) => typeImpl.GetNestedType(name, bindingAttr);
        public override MemberInfo[] GetMember(string name, MemberTypes type, BindingFlags bindingAttr) => typeImpl.GetMember(name, type, bindingAttr);
        public override MemberInfo[] GetMembers(BindingFlags bindingAttr) => typeImpl.GetMembers(bindingAttr);

        protected override TypeAttributes GetAttributeFlagsImpl() => typeImpl.Attributes;

        public override bool IsTypeDefinition => typeImpl.IsTypeDefinition;
        public override bool IsSZArray => typeImpl.IsSZArray;

        protected override bool IsArrayImpl() => typeImpl.IsArray;
        protected override bool IsPrimitiveImpl() => typeImpl.IsPrimitive;
        protected override bool IsByRefImpl() => typeImpl.IsByRef;
        public override bool IsGenericTypeParameter => typeImpl.IsGenericTypeParameter;
        public override bool IsGenericMethodParameter => typeImpl.IsGenericMethodParameter;
        protected override bool IsPointerImpl() => typeImpl.IsPointer;
        protected override bool IsValueTypeImpl() => typeImpl.IsValueType;
        protected override bool IsCOMObjectImpl() => typeImpl.IsCOMObject;
        public override bool IsByRefLike => typeImpl.IsByRefLike;
        public override bool IsConstructedGenericType => typeImpl.IsConstructedGenericType;

        public override bool IsCollectible => typeImpl.IsCollectible;

        public override Type? GetElementType() => typeImpl.GetElementType();
        protected override bool HasElementTypeImpl() => typeImpl.HasElementType;

        public override Type UnderlyingSystemType => typeImpl.UnderlyingSystemType;

        // ICustomAttributeProvider
        public override object[] GetCustomAttributes(bool inherit) => typeImpl.GetCustomAttributes(inherit);
        public override object[] GetCustomAttributes(Type attributeType, bool inherit) => typeImpl.GetCustomAttributes(attributeType, inherit);

        public override bool IsDefined(Type attributeType, bool inherit) => typeImpl.IsDefined(attributeType, inherit);
        public override InterfaceMapping GetInterfaceMap(Type interfaceType) => typeImpl.GetInterfaceMap(interfaceType);
    }
}
