// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Emit;
using System.Linq;
using System.Runtime.InteropServices;
using static System.Reflection.Metadata.Experiment.EntityWrappers;
using System.Globalization;

namespace System.Reflection.Metadata.Experiment
{
    internal sealed class PersistableTypeBuilder : TypeBuilder
    {
        public override string Name => _strName!;
        public override Module Module => _module;
        internal TypeAttributes UserTypeAttribute { get; set; }
        internal List<PersistableMethodBuilder> _methodDefStore = new();
        internal List<PersistableFieldBuilder> _fieldDefStore = new();
        internal List<CustomAttributeWrapper> _customAttributes = new();
        private readonly PersistableModuleBuilder _module;
        private readonly string? _strName;

        internal PersistableTypeBuilder(string name, PersistableModuleBuilder module, TypeAttributes typeAttributes)
        {
            _strName = name;
            _module = module;
            UserTypeAttribute = typeAttributes;
            //Extract namespace from name
            int idx = _strName.LastIndexOf('.');

            if (idx != -1)
            {
                Namespace = _strName[..idx];
                _strName = _strName[(idx + 1)..];
            }
        }

        internal PersistableModuleBuilder GetModuleBuilder() => _module;

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            throw new NotImplementedException();
        }
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
            var field = new PersistableFieldBuilder(this, fieldName, type, requiredCustomModifiers, optionalCustomModifiers, attributes);
            _fieldDefStore.Add(field);
           return field;
        }
        protected override GenericTypeParameterBuilder[] DefineGenericParametersCore(params string[] names) => throw new NotImplementedException();
        protected override FieldBuilder DefineInitializedDataCore(string name, byte[] data, FieldAttributes attributes) => throw new NotImplementedException();
        protected override MethodBuilder DefineMethodCore(string name, MethodAttributes attributes, CallingConventions callingConvention, Type? returnType, Type[]? returnTypeRequiredCustomModifiers, Type[]? returnTypeOptionalCustomModifiers, Type[]? parameterTypes, Type[][]? parameterTypeRequiredCustomModifiers, Type[][]? parameterTypeOptionalCustomModifiers)
        {
            PersistableMethodBuilder methodBuilder = new(name, attributes, callingConvention, returnType, parameterTypes, _module, this);
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
        protected override void SetCustomAttributeCore(ConstructorInfo constructorInfo, byte[] binaryAttribute)
        {
            if (constructorInfo == null)
            {
                throw new ArgumentNullException(nameof(constructorInfo));
            }

            if (binaryAttribute == null)
            {
                throw new ArgumentNullException(nameof(binaryAttribute)); // This is incorrect
            }

            if (constructorInfo.DeclaringType == null)
            {
                throw new ArgumentException("Attribute constructor has no type.");
            }

            //We check whether the custom attribute is actually a pseudo-custom attribute.
            //(We have only done ComImport for the prototype, eventually all pseudo-custom attributes will be hard-coded.)
            //If it is, simply alter the TypeAttributes.
            //We want to handle this before the type metadata is generated.

            if (constructorInfo.DeclaringType.Name.Equals("ComImportAttribute"))
            {
                Debug.WriteLine("Modifying internal flags");
                UserTypeAttribute |= TypeAttributes.Import;
            }
            else
            {
                AssemblyReferenceWrapper assemblyReference = new AssemblyReferenceWrapper(constructorInfo.DeclaringType.Assembly);
                TypeReferenceWrapper typeReference = new TypeReferenceWrapper(constructorInfo.DeclaringType);
                MethodReferenceWrapper methodReference = new MethodReferenceWrapper(constructorInfo);
                CustomAttributeWrapper customAttribute = new CustomAttributeWrapper(constructorInfo, binaryAttribute);

                if (!_module._assemblyRefStore.Contains(assemblyReference)) // Avoid adding the same assembly twice
                {
                    _module._assemblyRefStore.Add(assemblyReference);
                    typeReference.parentToken = _module._nextAssemblyRefRowId++;
                }
                else
                {
                    typeReference.parentToken = _module._assemblyRefStore.IndexOf(assemblyReference) + 1; // Add 1 to account for zero based indexing
                }

                if (!_module._typeRefStore.Contains(typeReference)) // Avoid adding the same type twice
                {
                    _module._typeRefStore.Add(typeReference);
                    methodReference.parentToken = _module._nextTypeRefRowId++;
                }
                else
                {
                    methodReference.parentToken = _module._typeRefStore.IndexOf(typeReference) + 1;
                }

                if (!_module._methodRefStore.Contains(methodReference)) // Avoid add the same method twice
                {
                    _module._methodRefStore.Add(methodReference);
                    customAttribute.conToken = _module._nextMethodRefRowId++;
                }
                else
                {
                    customAttribute.conToken = _module._methodRefStore.IndexOf(methodReference) + 1;
                }

                _customAttributes.Add(customAttribute);
            }
        }
        protected override void SetCustomAttributeCore(CustomAttributeBuilder customBuilder)
        {
            //SetCustomAttribute(customBuilder.Constructor, customBuilder._blob);
        }
        protected override void SetParentCore([DynamicallyAccessedMembers((DynamicallyAccessedMemberTypes)(-1))] Type? parent) => throw new NotImplementedException();

        public override object[] GetCustomAttributes(bool inherit)
        {
            return _customAttributes.ToArray();
        }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            ArgumentNullException.ThrowIfNull(attributeType);
            List<CustomAttributeWrapper> copy = new();
           foreach (var customAttribute in _customAttributes)
           {
                if (customAttribute.constructorInfo.DeclaringType == attributeType)
                    copy.Add(customAttribute);
           }
           return copy.ToArray();
        }
        public override Type GetElementType() => throw new NotSupportedException();
        public override string? AssemblyQualifiedName => throw new NotSupportedException();
        public override string? FullName => throw new NotSupportedException();
        public override string? Namespace { get; }
        public override Assembly Assembly => _module.Assembly;
        public override Type UnderlyingSystemType => throw new NotSupportedException();
        public override Guid GUID => throw new NotSupportedException();
        public override Type? BaseType => throw new NotSupportedException();

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        public override object? InvokeMember(string name, BindingFlags invokeAttr, Binder? binder, object? target,
            object?[]? args, ParameterModifier[]? modifiers, CultureInfo? culture, string[]? namedParameters) => throw new NotSupportedException();
        protected override bool IsArrayImpl() => false;
        protected override bool IsByRefImpl() => false;
        protected override bool IsPointerImpl() => false;
        protected override bool IsPrimitiveImpl() => false;
        protected override bool HasElementTypeImpl() => false;
        protected override TypeAttributes GetAttributeFlagsImpl() => UserTypeAttribute;
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

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2070:UnrecognizedReflectionPattern",
            Justification = "The GetInterfaces technically requires all interfaces to be preserved" +
                "But in this case it acts only on TypeBuilder which is never trimmed (as it's runtime created).")]
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
