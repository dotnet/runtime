// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Globalization;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace System.Reflection.Emit
{
    internal sealed class TypeBuilderImpl : TypeBuilder
    {
        internal List<MethodBuilderImpl> _methodDefStore = new();
        internal List<FieldBuilderImpl> _fieldDefStore = new();
        private readonly ModuleBuilderImpl _module;
        private readonly string _name;
        private readonly string? _namespace;
        internal readonly TypeDefinitionHandle _handle;
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        private Type? _typeParent;
        private TypeAttributes _attributes;

        internal TypeBuilderImpl(string fullName, TypeAttributes typeAttributes,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type? parent, ModuleBuilderImpl module, TypeDefinitionHandle handle)
        {
            _name = fullName;
            _module = module;
            _attributes = typeAttributes;
            SetParent(parent);
            _handle = handle;

            // Extract namespace from fullName
            int idx = _name.LastIndexOf('.');
            if (idx != -1)
            {
                _namespace = _name[..idx];
                _name = _name[(idx + 1)..];
            }
        }

        internal ModuleBuilderImpl GetModuleBuilder() => _module;
        protected override PackingSize PackingSizeCore => throw new NotImplementedException();
        protected override int SizeCore => throw new NotImplementedException();
        protected override void AddInterfaceImplementationCore([DynamicallyAccessedMembers((DynamicallyAccessedMemberTypes)(-1))] Type interfaceType) => throw new NotImplementedException();
        [return: DynamicallyAccessedMembers((DynamicallyAccessedMemberTypes)(-1))]
        protected override TypeInfo CreateTypeInfoCore() => throw new NotImplementedException();
        protected override ConstructorBuilder DefineConstructorCore(MethodAttributes attributes, CallingConventions callingConvention, Type[]? parameterTypes, Type[][]? requiredCustomModifiers, Type[][]? optionalCustomModifiers) => throw new NotImplementedException();
        protected override ConstructorBuilder DefineDefaultConstructorCore(MethodAttributes attributes) => throw new NotImplementedException();
        protected override EventBuilder DefineEventCore(string name, EventAttributes attributes, Type eventtype) => throw new NotImplementedException();
        protected override FieldBuilder DefineFieldCore(string fieldName, Type type, Type[]? requiredCustomModifiers, Type[]? optionalCustomModifiers, FieldAttributes attributes)
        {
            var field = new FieldBuilderImpl(this, fieldName, type, attributes);
            _fieldDefStore.Add(field);
            return field;
        }
        protected override GenericTypeParameterBuilder[] DefineGenericParametersCore(params string[] names) => throw new NotImplementedException();
        protected override FieldBuilder DefineInitializedDataCore(string name, byte[] data, FieldAttributes attributes) => throw new NotImplementedException();
        protected override MethodBuilder DefineMethodCore(string name, MethodAttributes attributes, CallingConventions callingConvention, Type? returnType, Type[]? returnTypeRequiredCustomModifiers, Type[]? returnTypeOptionalCustomModifiers, Type[]? parameterTypes, Type[][]? parameterTypeRequiredCustomModifiers, Type[][]? parameterTypeOptionalCustomModifiers)
        {
            MethodBuilderImpl methodBuilder = new(name, attributes, callingConvention, returnType, parameterTypes, _module, this);
            _methodDefStore.Add(methodBuilder);
            return methodBuilder;
        }

        protected override void DefineMethodOverrideCore(MethodInfo methodInfoBody, MethodInfo methodInfoDeclaration) => throw new NotImplementedException();
        protected override TypeBuilder DefineNestedTypeCore(string name, TypeAttributes attr, [DynamicallyAccessedMembers((DynamicallyAccessedMemberTypes)(-1))] Type? parent, Type[]? interfaces, Emit.PackingSize packSize, int typeSize) => throw new NotImplementedException();
        [RequiresUnreferencedCode("P/Invoke marshalling may dynamically access members that could be trimmed.")]
        protected override MethodBuilder DefinePInvokeMethodCore(string name, string dllName, string entryName, MethodAttributes attributes, CallingConventions callingConvention, Type? returnType, Type[]? returnTypeRequiredCustomModifiers, Type[]? returnTypeOptionalCustomModifiers, Type[]? parameterTypes, Type[][]? parameterTypeRequiredCustomModifiers, Type[][]? parameterTypeOptionalCustomModifiers, CallingConvention nativeCallConv, CharSet nativeCharSet) => throw new NotImplementedException();
        protected override PropertyBuilder DefinePropertyCore(string name, PropertyAttributes attributes, CallingConventions callingConvention, Type returnType, Type[]? returnTypeRequiredCustomModifiers, Type[]? returnTypeOptionalCustomModifiers, Type[]? parameterTypes, Type[][]? parameterTypeRequiredCustomModifiers, Type[][]? parameterTypeOptionalCustomModifiers) => throw new NotImplementedException();
        protected override ConstructorBuilder DefineTypeInitializerCore() => throw new NotImplementedException();
        protected override FieldBuilder DefineUninitializedDataCore(string name, int size, FieldAttributes attributes) => throw new NotImplementedException();
        protected override bool IsCreatedCore() => throw new NotImplementedException();
        protected override void SetCustomAttributeCore(ConstructorInfo con, byte[] binaryAttribute) => throw new NotImplementedException();
        protected override void SetCustomAttributeCore(CustomAttributeBuilder customBuilder) => throw new NotImplementedException();

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2074:DynamicallyAccessedMembers",
            Justification = "TODO: Need to figure out how to preserve System.Object public constructor")]
        protected override void SetParentCore([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type? parent)
        {
            if (parent != null)
            {
                if (parent.IsInterface)
                {
                    throw new ArgumentException(SR.Argument_CannotSetParentToInterface);
                }

                _typeParent = parent;
            }
            else
            {
                if ((_attributes & TypeAttributes.Interface) != TypeAttributes.Interface)
                {
                    _typeParent = _module.GetTypeFromCoreAssembly(CoreTypeId.Object);
                }
                else
                {
                    if ((_attributes & TypeAttributes.Abstract) == 0)
                    {
                        throw new InvalidOperationException(SR.InvalidOperation_BadInterfaceNotAbstract);
                    }

                    // There is no extends for interface class.
                    _typeParent = null;
                }
            }
        }
        public override string Name => _name;
        public override bool IsDefined(Type attributeType, bool inherit) => throw new NotImplementedException();
        public override object[] GetCustomAttributes(bool inherit) => throw new NotImplementedException();
        public override object[] GetCustomAttributes(Type attributeType, bool inherit) => throw new NotImplementedException();
        public override Type GetElementType() => throw new NotSupportedException();
        public override string? AssemblyQualifiedName => throw new NotSupportedException();
        public override string? FullName => throw new NotSupportedException();
        public override string? Namespace => _namespace;
        public override Assembly Assembly => _module.Assembly;
        public override Module Module => _module;
        public override Type UnderlyingSystemType => this;
        public override Guid GUID => throw new NotSupportedException();
        public override Type? BaseType => _typeParent;
        public override int MetadataToken => MetadataTokens.GetToken(_handle);
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        public override object? InvokeMember(string name, BindingFlags invokeAttr, Binder? binder, object? target,
            object?[]? args, ParameterModifier[]? modifiers, CultureInfo? culture, string[]? namedParameters) => throw new NotSupportedException();
        protected override bool IsArrayImpl() => false;
        protected override bool IsByRefImpl() => false;
        protected override bool IsPointerImpl() => false;
        protected override bool IsPrimitiveImpl() => false;
        protected override bool HasElementTypeImpl() => false;
        protected override TypeAttributes GetAttributeFlagsImpl() => _attributes;
        protected override bool IsCOMObjectImpl()
        {
            return ((GetAttributeFlagsImpl() & TypeAttributes.Import) != 0) ? true : false;
        }
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

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
        public override Type[] GetInterfaces() => throw new NotSupportedException();
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
        public override PropertyInfo[] GetProperties(BindingFlags bindingAttr) => throw new NotSupportedException();
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
        protected override PropertyInfo GetPropertyImpl(string name, BindingFlags bindingAttr, Binder? binder,
                Type? returnType, Type[]? types, ParameterModifier[]? modifiers) => throw new NotSupportedException();

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.NonPublicNestedTypes)]
        public override Type[] GetNestedTypes(BindingFlags bindingAttr) => throw new NotSupportedException();

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.NonPublicNestedTypes)]
        public override Type? GetNestedType(string name, BindingFlags bindingAttr) => throw new NotSupportedException();

        [DynamicallyAccessedMembers(GetAllMembers)]
        public override MemberInfo[] GetMember(string name, MemberTypes type, BindingFlags bindingAttr) => throw new NotSupportedException();

        public override InterfaceMapping GetInterfaceMap([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)] Type interfaceType)
            => throw new NotSupportedException();
        [DynamicallyAccessedMembers(GetAllMembers)]
        public override MemberInfo[] GetMembers(BindingFlags bindingAttr) => throw new NotSupportedException();

        public override bool IsAssignableFrom([NotNullWhen(true)] Type? c) => throw new NotSupportedException();
        public override Type MakePointerType() => throw new NotSupportedException();
        public override Type MakeByRefType() => throw new NotSupportedException();
        [RequiresDynamicCode("The code for an array of the specified type might not be available.")]
        public override Type MakeArrayType() => throw new NotSupportedException();
        [RequiresDynamicCode("The code for an array of the specified type might not be available.")]
        public override Type MakeArrayType(int rank) => throw new NotSupportedException();

        internal const DynamicallyAccessedMemberTypes GetAllMembers = DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields |
            DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods |
            DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents |
            DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties |
            DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors |
            DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.NonPublicNestedTypes;
    }
}
