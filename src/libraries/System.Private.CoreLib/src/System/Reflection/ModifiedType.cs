// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using CultureInfo = System.Globalization.CultureInfo;

namespace System.Reflection
{
    /// <summary>
    /// Base class for modified types and standalone modified type.
    /// Design supports code sharing between different runtimes and lazy loading of custom modifiers.
    /// </summary>
    internal partial class ModifiedType : Type
    {
        private readonly TypeSignature _typeSignature;
        private readonly Type _unmodifiedType;

        internal ModifiedType(Type unmodifiedType, TypeSignature typeSignature)
        {
            _unmodifiedType = unmodifiedType;
            _typeSignature = typeSignature;
        }

        /// <summary>
        /// Factory to create a node recursively based on the underlying, unmodified type.
        /// A type tree is formed due to arrays and pointers having an element type, function pointers
        /// having a return type and parameter types, and generic types having argument types.
        /// </summary>
        protected static Type Create(Type unmodifiedType, TypeSignature typeSignature)
        {
            Type modifiedType;
            if (unmodifiedType.IsFunctionPointer)
            {
                modifiedType = new ModifiedFunctionPointerType(unmodifiedType, typeSignature);
            }
            else if (unmodifiedType.HasElementType)
            {
                modifiedType = new ModifiedHasElementType(unmodifiedType, typeSignature);
            }
            else if (unmodifiedType.IsGenericType)
            {
                modifiedType = new ModifiedGenericType(unmodifiedType, typeSignature);
            }
            else
            {
                modifiedType = new ModifiedType(unmodifiedType, typeSignature);
            }
            return modifiedType;
        }

        protected Type UnmodifiedType => _unmodifiedType;

        // Below are the multitude of Type overloads. We throw NSE for members that would return an unmodified Type
        // directly or indirectly. We do this in case we want to support returning modified types instead.

        public override Type[] GetRequiredCustomModifiers()
        {
            // No caching is performed; as is the case with FieldInfo.GetCustomModifiers and friends.
            return GetCustomModifiers(required: true);
        }

        public override Type[] GetOptionalCustomModifiers()
        {
            // No caching is performed; as is the case with FieldInfo.GetCustomModifiers and friends.
            return GetCustomModifiers(required: false);
        }

        // Modified types do not support Equals. That would need to include custom modifiers and any parameterized types recursively.
        // UnderlyingSystemType.Equals() should should be used if basic equality is necessary.
        public override bool Equals([NotNullWhen(true)] object? obj) => throw new NotSupportedException(SR.NotSupported_ModifiedType);
        public override bool Equals(Type? other) => throw new NotSupportedException(SR.NotSupported_ModifiedType);
        public override int GetHashCode() => throw new NotSupportedException(SR.NotSupported_ModifiedType);

        public override string ToString() => _unmodifiedType.ToString();
        public override Type UnderlyingSystemType => _unmodifiedType;

        public override GenericParameterAttributes GenericParameterAttributes => _unmodifiedType.GenericParameterAttributes;
        public override bool ContainsGenericParameters => _unmodifiedType.ContainsGenericParameters;
        public override Type GetGenericTypeDefinition() => _unmodifiedType.GetGenericTypeDefinition();
        public override bool IsGenericType => _unmodifiedType.IsGenericType;

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        public override object? InvokeMember(string name, BindingFlags invokeAttr, Binder? binder, object? target,
            object?[]? args, ParameterModifier[]? modifiers, CultureInfo? culture, string[]? namedParameters)
            => throw new NotSupportedException(SR.NotSupported_ModifiedType);

        public override Guid GUID => _unmodifiedType.GUID;
        public override int MetadataToken => throw new NotSupportedException(SR.NotSupported_ModifiedType);
        public override Module Module => _unmodifiedType.Module;
        public override Assembly Assembly => _unmodifiedType.Assembly;
        public override RuntimeTypeHandle TypeHandle => throw new NotSupportedException(SR.NotSupported_ModifiedType);
        public override string Name => _unmodifiedType.Name;
        public override string? FullName => _unmodifiedType.FullName;
        public override string? Namespace => _unmodifiedType.Namespace;
        public override string? AssemblyQualifiedName => _unmodifiedType.AssemblyQualifiedName;
        public override Type? BaseType => throw new NotSupportedException(SR.NotSupported_ModifiedType);
        public override Type? DeclaringType => throw new NotSupportedException(SR.NotSupported_ModifiedType);
        public override Type? ReflectedType => throw new NotSupportedException(SR.NotSupported_ModifiedType);

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
        protected override ConstructorInfo? GetConstructorImpl(BindingFlags bindingAttr, Binder? binder,
                CallingConventions callConvention, Type[] types, ParameterModifier[]? modifiers) => throw new NotSupportedException(SR.NotSupported_ModifiedType);

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
        public override ConstructorInfo[] GetConstructors(BindingFlags bindingAttr) => throw new NotSupportedException(SR.NotSupported_ModifiedType);

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
        protected override MethodInfo? GetMethodImpl(string name, BindingFlags bindingAttr, Binder? binder,
                CallingConventions callConvention, Type[]? types, ParameterModifier[]? modifiers)
            => throw new NotSupportedException(SR.NotSupported_ModifiedType);

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
        public override MethodInfo[] GetMethods(BindingFlags bindingAttr) => throw new NotSupportedException(SR.NotSupported_ModifiedType);

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)]
        public override FieldInfo? GetField(string name, BindingFlags bindingAttr) => throw new NotSupportedException(SR.NotSupported_ModifiedType);

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)]
        public override FieldInfo[] GetFields(BindingFlags bindingAttr) => throw new NotSupportedException(SR.NotSupported_ModifiedType);

        public override Type[] GetFunctionPointerCallingConventions() => _unmodifiedType.GetFunctionPointerCallingConventions();
        public override Type[] GetFunctionPointerParameterTypes() => _unmodifiedType.GetFunctionPointerParameterTypes();
        public override Type GetFunctionPointerReturnType() => _unmodifiedType.GetFunctionPointerReturnType();

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
        public override Type? GetInterface(string name, bool ignoreCase) => throw new NotSupportedException(SR.NotSupported_ModifiedType);

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
        public override Type[] GetInterfaces() => throw new NotSupportedException(SR.NotSupported_ModifiedType);
        public override InterfaceMapping GetInterfaceMap([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)] Type interfaceType) => _unmodifiedType.GetInterfaceMap(interfaceType);

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents)]
        public override EventInfo? GetEvent(string name, BindingFlags bindingAttr) => throw new NotSupportedException(SR.NotSupported_ModifiedType);

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents)]
        public override EventInfo[] GetEvents() => throw new NotSupportedException(SR.NotSupported_ModifiedType);

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
        protected override PropertyInfo? GetPropertyImpl(string name, BindingFlags bindingAttr, Binder? binder,
                        Type? returnType, Type[]? types, ParameterModifier[]? modifiers)
            => throw new NotSupportedException(SR.NotSupported_ModifiedType);

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
        public override PropertyInfo[] GetProperties(BindingFlags bindingAttr) => throw new NotSupportedException(SR.NotSupported_ModifiedType);

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents)]
        public override EventInfo[] GetEvents(BindingFlags bindingAttr) => throw new NotSupportedException(SR.NotSupported_ModifiedType);

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.NonPublicNestedTypes)]
        public override Type[] GetNestedTypes(BindingFlags bindingAttr) => throw new NotSupportedException(SR.NotSupported_ModifiedType);

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.NonPublicNestedTypes)]
        public override Type? GetNestedType(string name, BindingFlags bindingAttr) => throw new NotSupportedException(SR.NotSupported_ModifiedType);

        [DynamicallyAccessedMembers(GetAllMembers)]
        public override MemberInfo[] GetMember(string name, MemberTypes type, BindingFlags bindingAttr) => throw new NotSupportedException(SR.NotSupported_ModifiedType);

        [DynamicallyAccessedMembers(GetAllMembers)]
        public override MemberInfo[] GetMembers(BindingFlags bindingAttr) => throw new NotSupportedException(SR.NotSupported_ModifiedType);

        public override MemberInfo GetMemberWithSameMetadataDefinitionAs(MemberInfo member) => throw new NotSupportedException(SR.NotSupported_ModifiedType);

        protected override TypeAttributes GetAttributeFlagsImpl() => _unmodifiedType.Attributes;
        public override int GetArrayRank() => _unmodifiedType.GetArrayRank();

        public override bool IsTypeDefinition => _unmodifiedType.IsTypeDefinition;
        public override bool IsSZArray => _unmodifiedType.IsSZArray;
        public override bool IsVariableBoundArray => _unmodifiedType.IsVariableBoundArray;

        protected override bool IsArrayImpl() => _unmodifiedType.IsArray;
        public override bool IsEnum => _unmodifiedType.IsEnum;
        protected override bool IsPrimitiveImpl() => _unmodifiedType.IsPrimitive;
        protected override bool IsByRefImpl() => _unmodifiedType.IsByRef;
        public override bool IsGenericTypeParameter => _unmodifiedType.IsGenericTypeParameter;
        public override bool IsGenericMethodParameter => _unmodifiedType.IsGenericMethodParameter;
        protected override bool IsPointerImpl() => _unmodifiedType.IsPointer;
        protected override bool IsValueTypeImpl() => _unmodifiedType.IsValueType;
        protected override bool IsCOMObjectImpl() => _unmodifiedType.IsCOMObject;
        public override bool IsByRefLike => _unmodifiedType.IsByRefLike;
        public override bool IsConstructedGenericType => _unmodifiedType.IsConstructedGenericType;

        public override bool IsCollectible => _unmodifiedType.IsCollectible;

        public override bool IsFunctionPointer => _unmodifiedType.IsFunctionPointer;
        public override bool IsUnmanagedFunctionPointer => _unmodifiedType.IsUnmanagedFunctionPointer;

        public override bool IsSecurityCritical => _unmodifiedType.IsSecurityCritical;
        public override bool IsSecuritySafeCritical => _unmodifiedType.IsSecuritySafeCritical;
        public override bool IsSecurityTransparent => _unmodifiedType.IsSecurityTransparent;

        public override Type? GetElementType() => _unmodifiedType.GetElementType(); // Supported
        protected override bool HasElementTypeImpl() => _unmodifiedType.HasElementType;

        // ICustomAttributeProvider
        public override object[] GetCustomAttributes(bool inherit) => _unmodifiedType.GetCustomAttributes(inherit);
        public override object[] GetCustomAttributes(Type attributeType, bool inherit) => _unmodifiedType.GetCustomAttributes(attributeType, inherit);
        public override bool IsDefined(Type attributeType, bool inherit) => _unmodifiedType.IsDefined(attributeType, inherit);
    }
}
