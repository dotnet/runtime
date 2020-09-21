// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;

namespace System.Reflection
{
    //
    // Signature Types are highly restricted Type objects that can be passed in the Type[] parameter to Type.GetMethod(). Their primary
    // use is to pass types representing generic parameters defined by the method, or types composed from them. Passing in the normal
    // generic parameter Type obtained from MethodInfo.GetGenericArguments() is usually impractical (if you had the method, you wouldn't
    // be looking for it!)
    //
    internal abstract class SignatureType : Type
    {
        public sealed override bool IsSignatureType => true;

        // Type flavor predicates
        public abstract override bool IsTypeDefinition { get; }
        protected abstract override bool HasElementTypeImpl();
        protected abstract override bool IsArrayImpl();
        public abstract override bool IsSZArray { get; }
        public abstract override bool IsVariableBoundArray { get; }
        protected abstract override bool IsByRefImpl();
        public abstract override bool IsByRefLike { get; }
        protected abstract override bool IsPointerImpl();
        public sealed override bool IsGenericType => IsGenericTypeDefinition || IsConstructedGenericType;
        public abstract override bool IsGenericTypeDefinition { get; }
        public abstract override bool IsConstructedGenericType { get; }
        public abstract override bool IsGenericParameter { get; }
        public abstract override bool IsGenericTypeParameter { get; }
        public abstract override bool IsGenericMethodParameter { get; }
        public abstract override bool ContainsGenericParameters { get; }
        public sealed override MemberTypes MemberType => MemberTypes.TypeInfo;

        // Compositors
        public sealed override Type MakeArrayType() => new SignatureArrayType(this, rank: 1, isMultiDim: false);
        public sealed override Type MakeArrayType(int rank)
        {
            if (rank <= 0)
                throw new IndexOutOfRangeException();
            return new SignatureArrayType(this, rank: rank, isMultiDim: true);
        }
        public sealed override Type MakeByRefType() => new SignatureByRefType(this);
        public sealed override Type MakePointerType() => new SignaturePointerType(this);
        public sealed override Type MakeGenericType(params Type[] typeArguments) => throw new NotSupportedException(SR.NotSupported_SignatureType); // There is no SignatureType for type definition types so it would never be legal to call this.

        // Dissectors
        public sealed override Type? GetElementType() => ElementType;
        public abstract override int GetArrayRank();
        public abstract override Type GetGenericTypeDefinition();
        public abstract override Type[] GenericTypeArguments { get; }
        public abstract override Type[] GetGenericArguments();
        public abstract override int GenericParameterPosition { get; }
        internal abstract SignatureType? ElementType { get; }

        // Identity
#if DEBUG
        public sealed override bool Equals(object? o) => base.Equals(o);
        public sealed override bool Equals(Type? o) => base.Equals(o);
        public sealed override int GetHashCode() => base.GetHashCode();
#endif
        public sealed override Type UnderlyingSystemType => this;  // Equals(Type) depends on this.

        // Naming and diagnostics
        public abstract override string Name { get; }
        public abstract override string? Namespace { get; }
        public sealed override string? FullName => null;
        public sealed override string? AssemblyQualifiedName => null;
        public abstract override string ToString();

        // Not supported on Signature Types
        public sealed override Assembly Assembly => throw new NotSupportedException(SR.NotSupported_SignatureType);
        public sealed override Module Module => throw new NotSupportedException(SR.NotSupported_SignatureType);

        public sealed override Type ReflectedType => throw new NotSupportedException(SR.NotSupported_SignatureType);
        public sealed override Type BaseType => throw new NotSupportedException(SR.NotSupported_SignatureType);

        public sealed override Type[] GetInterfaces() => throw new NotSupportedException(SR.NotSupported_SignatureType);
        public sealed override bool IsAssignableFrom([NotNullWhen(true)] Type? c) => throw new NotSupportedException(SR.NotSupported_SignatureType);
        public sealed override int MetadataToken => throw new NotSupportedException(SR.NotSupported_SignatureType);
        public sealed override bool HasSameMetadataDefinitionAs(MemberInfo other) => throw new NotSupportedException(SR.NotSupported_SignatureType);

        public sealed override Type DeclaringType => throw new NotSupportedException(SR.NotSupported_SignatureType);
        public sealed override MethodBase DeclaringMethod => throw new NotSupportedException(SR.NotSupported_SignatureType);

        public sealed override Type[] GetGenericParameterConstraints() => throw new NotSupportedException(SR.NotSupported_SignatureType);
        public sealed override GenericParameterAttributes GenericParameterAttributes => throw new NotSupportedException(SR.NotSupported_SignatureType);
        public sealed override bool IsEnumDefined(object value) => throw new NotSupportedException(SR.NotSupported_SignatureType);
        public sealed override string GetEnumName(object value) => throw new NotSupportedException(SR.NotSupported_SignatureType);
        public sealed override string[] GetEnumNames() => throw new NotSupportedException(SR.NotSupported_SignatureType);
        public sealed override Type GetEnumUnderlyingType() => throw new NotSupportedException(SR.NotSupported_SignatureType);
        public sealed override Array GetEnumValues() => throw new NotSupportedException(SR.NotSupported_SignatureType);
        public sealed override Guid GUID => throw new NotSupportedException(SR.NotSupported_SignatureType);
        protected sealed override TypeCode GetTypeCodeImpl() => throw new NotSupportedException(SR.NotSupported_SignatureType);
        protected sealed override TypeAttributes GetAttributeFlagsImpl() => throw new NotSupportedException(SR.NotSupported_SignatureType);

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
        public sealed override ConstructorInfo[] GetConstructors(BindingFlags bindingAttr) => throw new NotSupportedException(SR.NotSupported_SignatureType);

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents)]
        public sealed override EventInfo GetEvent(string name, BindingFlags bindingAttr) => throw new NotSupportedException(SR.NotSupported_SignatureType);

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents)]
        public sealed override EventInfo[] GetEvents(BindingFlags bindingAttr) => throw new NotSupportedException(SR.NotSupported_SignatureType);

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)]
        public sealed override FieldInfo GetField(string name, BindingFlags bindingAttr) => throw new NotSupportedException(SR.NotSupported_SignatureType);

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)]
        public sealed override FieldInfo[] GetFields(BindingFlags bindingAttr) => throw new NotSupportedException(SR.NotSupported_SignatureType);

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        public sealed override MemberInfo[] GetMembers(BindingFlags bindingAttr) => throw new NotSupportedException(SR.NotSupported_SignatureType);

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
        public sealed override MethodInfo[] GetMethods(BindingFlags bindingAttr) => throw new NotSupportedException(SR.NotSupported_SignatureType);

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.NonPublicNestedTypes)]
        public sealed override Type GetNestedType(string name, BindingFlags bindingAttr) => throw new NotSupportedException(SR.NotSupported_SignatureType);

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.NonPublicNestedTypes)]
        public sealed override Type[] GetNestedTypes(BindingFlags bindingAttr) => throw new NotSupportedException(SR.NotSupported_SignatureType);

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
        public sealed override PropertyInfo[] GetProperties(BindingFlags bindingAttr) => throw new NotSupportedException(SR.NotSupported_SignatureType);

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        public sealed override object InvokeMember(string name, BindingFlags invokeAttr, Binder? binder, object? target, object?[]? args, ParameterModifier[]? modifiers, CultureInfo? culture, string[]? namedParameters) => throw new NotSupportedException(SR.NotSupported_SignatureType);

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
        protected sealed override MethodInfo GetMethodImpl(string name, BindingFlags bindingAttr, Binder? binder, CallingConventions callConvention, Type[]? types, ParameterModifier[]? modifiers) => throw new NotSupportedException(SR.NotSupported_SignatureType);

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
        protected sealed override MethodInfo GetMethodImpl(string name, int genericParameterCount, BindingFlags bindingAttr, Binder? binder, CallingConventions callConvention, Type[]? types, ParameterModifier[]? modifiers) => throw new NotSupportedException(SR.NotSupported_SignatureType);

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
        protected sealed override PropertyInfo GetPropertyImpl(string name, BindingFlags bindingAttr, Binder? binder, Type? returnType, Type[]? types, ParameterModifier[]? modifiers) => throw new NotSupportedException(SR.NotSupported_SignatureType);

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        public sealed override MemberInfo[] FindMembers(MemberTypes memberType, BindingFlags bindingAttr, MemberFilter? filter, object? filterCriteria) => throw new NotSupportedException(SR.NotSupported_SignatureType);

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        public sealed override MemberInfo[] GetMember(string name, BindingFlags bindingAttr) => throw new NotSupportedException(SR.NotSupported_SignatureType);

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        public sealed override MemberInfo[] GetMember(string name, MemberTypes type, BindingFlags bindingAttr) => throw new NotSupportedException(SR.NotSupported_SignatureType);

        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicFields
            | DynamicallyAccessedMemberTypes.PublicMethods
            | DynamicallyAccessedMemberTypes.PublicEvents
            | DynamicallyAccessedMemberTypes.PublicProperties
            | DynamicallyAccessedMemberTypes.PublicConstructors
            | DynamicallyAccessedMemberTypes.PublicNestedTypes)]
        public sealed override MemberInfo[] GetDefaultMembers() => throw new NotSupportedException(SR.NotSupported_SignatureType);

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents)]
        public sealed override EventInfo[] GetEvents() => throw new NotSupportedException(SR.NotSupported_SignatureType);

        public sealed override object[] GetCustomAttributes(bool inherit) => throw new NotSupportedException(SR.NotSupported_SignatureType);
        public sealed override object[] GetCustomAttributes(Type attributeType, bool inherit) => throw new NotSupportedException(SR.NotSupported_SignatureType);
        public sealed override bool IsDefined(Type attributeType, bool inherit) => throw new NotSupportedException(SR.NotSupported_SignatureType);
        public sealed override IList<CustomAttributeData> GetCustomAttributesData() => throw new NotSupportedException(SR.NotSupported_SignatureType);
        public sealed override Type GetInterface(string name, bool ignoreCase) => throw new NotSupportedException(SR.NotSupported_SignatureType);

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
        protected sealed override ConstructorInfo GetConstructorImpl(BindingFlags bindingAttr, Binder? binder, CallingConventions callConvention, Type[] types, ParameterModifier[]? modifiers) => throw new NotSupportedException(SR.NotSupported_SignatureType);

        protected sealed override bool IsCOMObjectImpl() => throw new NotSupportedException(SR.NotSupported_SignatureType);
        protected sealed override bool IsPrimitiveImpl() => throw new NotSupportedException(SR.NotSupported_SignatureType);
        public sealed override IEnumerable<CustomAttributeData> CustomAttributes => throw new NotSupportedException(SR.NotSupported_SignatureType);
        public sealed override Type[] FindInterfaces(TypeFilter filter, object? filterCriteria) => throw new NotSupportedException(SR.NotSupported_SignatureType);
        public sealed override InterfaceMapping GetInterfaceMap(Type interfaceType) => throw new NotSupportedException(SR.NotSupported_SignatureType);
        protected sealed override bool IsContextfulImpl() => throw new NotSupportedException(SR.NotSupported_SignatureType);
        public sealed override bool IsEnum => throw new NotSupportedException(SR.NotSupported_SignatureType);
        public sealed override bool IsEquivalentTo([NotNullWhen(true)] Type? other) => throw new NotSupportedException(SR.NotSupported_SignatureType);
        public sealed override bool IsInstanceOfType([NotNullWhen(true)] object? o) => throw new NotSupportedException(SR.NotSupported_SignatureType);
        protected sealed override bool IsMarshalByRefImpl() => throw new NotSupportedException(SR.NotSupported_SignatureType);
        public sealed override bool IsSecurityCritical => throw new NotSupportedException(SR.NotSupported_SignatureType);
        public sealed override bool IsSecuritySafeCritical => throw new NotSupportedException(SR.NotSupported_SignatureType);
        public sealed override bool IsSecurityTransparent => throw new NotSupportedException(SR.NotSupported_SignatureType);
        public sealed override bool IsSerializable => throw new NotSupportedException(SR.NotSupported_SignatureType);
        public sealed override bool IsSubclassOf(Type c) => throw new NotSupportedException(SR.NotSupported_SignatureType);
        protected sealed override bool IsValueTypeImpl() => throw new NotSupportedException(SR.NotSupported_SignatureType);

        public sealed override StructLayoutAttribute StructLayoutAttribute => throw new NotSupportedException(SR.NotSupported_SignatureType);

        public sealed override RuntimeTypeHandle TypeHandle => throw new NotSupportedException(SR.NotSupported_SignatureType);
    }
}
