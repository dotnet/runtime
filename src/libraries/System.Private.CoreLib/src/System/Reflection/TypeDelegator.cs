// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// TypeDelegator
//
// This class wraps a Type object and delegates all methods to that Type.

using System.Diagnostics.CodeAnalysis;
using CultureInfo = System.Globalization.CultureInfo;

namespace System.Reflection
{
    public class TypeDelegator : TypeInfo
    {
        public override bool IsAssignableFrom([NotNullWhen(true)] TypeInfo? typeInfo)
        {
            if (typeInfo == null)
                return false;
            return IsAssignableFrom(typeInfo.AsType());
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        protected Type typeImpl = null!;

        protected TypeDelegator() { }

        // NOTE: delegatingType is marked as DynamicallyAccessedMemberTypes.All, but analysis tools special case
        // calls to this constructor and propagate the existing dataflow metadata from delegatingType to this
        // TypeDelegator. The only purpose of the annotation here is to avoid dataflow warnings _within_ this type.
        public TypeDelegator([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type delegatingType)
        {
            ArgumentNullException.ThrowIfNull(delegatingType);

            typeImpl = delegatingType;
        }

        public override Guid GUID => typeImpl.GUID;
        public override int MetadataToken => typeImpl.MetadataToken;

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        public override object? InvokeMember(string name, BindingFlags invokeAttr, Binder? binder, object? target,
            object?[]? args, ParameterModifier[]? modifiers, CultureInfo? culture, string[]? namedParameters)
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

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
        protected override ConstructorInfo? GetConstructorImpl(BindingFlags bindingAttr, Binder? binder,
                CallingConventions callConvention, Type[] types, ParameterModifier[]? modifiers)
        {
            return typeImpl.GetConstructor(bindingAttr, binder, callConvention, types, modifiers);
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
        public override ConstructorInfo[] GetConstructors(BindingFlags bindingAttr) => typeImpl.GetConstructors(bindingAttr);

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
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

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
        public override MethodInfo[] GetMethods(BindingFlags bindingAttr) => typeImpl.GetMethods(bindingAttr);

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)]
        public override FieldInfo? GetField(string name, BindingFlags bindingAttr) => typeImpl.GetField(name, bindingAttr);

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)]
        public override FieldInfo[] GetFields(BindingFlags bindingAttr) => typeImpl.GetFields(bindingAttr);

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
        public override Type? GetInterface(string name, bool ignoreCase) => typeImpl.GetInterface(name, ignoreCase);

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
        public override Type[] GetInterfaces() => typeImpl.GetInterfaces();

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents)]
        public override EventInfo? GetEvent(string name, BindingFlags bindingAttr) => typeImpl.GetEvent(name, bindingAttr);

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents)]
        public override EventInfo[] GetEvents() => typeImpl.GetEvents();

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
        protected override PropertyInfo? GetPropertyImpl(string name, BindingFlags bindingAttr, Binder? binder,
                        Type? returnType, Type[]? types, ParameterModifier[]? modifiers)
        {
            if (returnType == null && types == null)
                return typeImpl.GetProperty(name, bindingAttr);
            else
                return typeImpl.GetProperty(name, bindingAttr, binder, returnType, types!, modifiers);
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
        public override PropertyInfo[] GetProperties(BindingFlags bindingAttr) => typeImpl.GetProperties(bindingAttr);

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents)]
        public override EventInfo[] GetEvents(BindingFlags bindingAttr) => typeImpl.GetEvents(bindingAttr);

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.NonPublicNestedTypes)]
        public override Type[] GetNestedTypes(BindingFlags bindingAttr) => typeImpl.GetNestedTypes(bindingAttr);

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.NonPublicNestedTypes)]
        public override Type? GetNestedType(string name, BindingFlags bindingAttr) => typeImpl.GetNestedType(name, bindingAttr);

        [DynamicallyAccessedMembers(GetAllMembers)]
        public override MemberInfo[] GetMember(string name, MemberTypes type, BindingFlags bindingAttr) => typeImpl.GetMember(name, type, bindingAttr);

        [DynamicallyAccessedMembers(GetAllMembers)]
        public override MemberInfo[] GetMembers(BindingFlags bindingAttr) => typeImpl.GetMembers(bindingAttr);

        public override MemberInfo GetMemberWithSameMetadataDefinitionAs(MemberInfo member) => typeImpl.GetMemberWithSameMetadataDefinitionAs(member);

        protected override TypeAttributes GetAttributeFlagsImpl() => typeImpl.Attributes;

        public override bool IsTypeDefinition => typeImpl.IsTypeDefinition;
        public override bool IsSZArray => typeImpl.IsSZArray;
        public override bool IsVariableBoundArray => typeImpl.IsVariableBoundArray;

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
        public override InterfaceMapping GetInterfaceMap([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)] Type interfaceType) => typeImpl.GetInterfaceMap(interfaceType);
    }
}
