// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;

namespace System.Reflection.Emit
{
    internal sealed class TypeBuilderImpl : TypeBuilder
    {
        private readonly ModuleBuilderImpl _module;
        private readonly string _name;
        private readonly string? _namespace;
        private string? _strFullName;
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        private Type? _typeParent;
        private readonly TypeBuilderImpl? _declaringType;
        private GenericTypeParameterBuilderImpl[]? _typeParameters;
        private TypeAttributes _attributes;
        private PackingSize _packingSize;
        private int _typeSize;
        private Type? _enumUnderlyingType;
        private bool _isCreated;

        internal bool _isHiddenGlobalType;
        internal TypeDefinitionHandle _handle;
        internal int _firstFieldToken;
        internal int _firstMethodToken;
        internal int _firstPropertyToken;
        internal int _firstEventToken;
        internal readonly List<MethodBuilderImpl> _methodDefinitions = new();
        internal readonly List<FieldBuilderImpl> _fieldDefinitions = new();
        internal readonly List<ConstructorBuilderImpl> _constructorDefinitions = new();
        internal List<Type>? _interfaces;
        internal readonly List<PropertyBuilderImpl> _propertyDefinitions = new();
        internal readonly List<EventBuilderImpl> _eventDefinitions = new();
        internal List<CustomAttributeWrapper>? _customAttributes;
        internal Dictionary<Type, List<(MethodInfo ifaceMethod, MethodInfo targetMethod)>>? _methodOverrides;

        // Only for creating the global type
        internal TypeBuilderImpl(ModuleBuilderImpl module)
        {
            _name = "<Module>"; // Defined in ECMA-335 II.10.8
            _module = module;
            _isHiddenGlobalType = true;
            _handle = MetadataTokens.TypeDefinitionHandle(1);
        }

        internal TypeBuilderImpl(string fullName, TypeAttributes typeAttributes,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type? parent, ModuleBuilderImpl module,
            Type[]? interfaces, PackingSize packingSize, int typeSize, TypeBuilderImpl? enclosingType)
        {
            _name = fullName;
            _module = module;
            _attributes = typeAttributes;
            _packingSize = packingSize;
            _typeSize = typeSize;
            SetParent(parent);
            _declaringType = enclosingType;

            // Extract namespace from fullName
            int idx = _name.LastIndexOf('.');
            if (idx != -1)
            {
                _namespace = _name[..idx];
                _name = _name[(idx + 1)..];
            }

            if (interfaces != null)
            {
                _interfaces = new List<Type>();
                for (int i = 0; i < interfaces.Length; i++)
                {
                    Type @interface = interfaces[i];
                    // cannot contain null in the interface list
                    ArgumentNullException.ThrowIfNull(@interface, nameof(interfaces));
                    _interfaces.Add(@interface);
                }
            }
        }

        internal ModuleBuilderImpl GetModuleBuilder() => _module;
        protected override PackingSize PackingSizeCore => _packingSize;
        protected override int SizeCore => _typeSize;

        protected override void AddInterfaceImplementationCore([DynamicallyAccessedMembers((DynamicallyAccessedMemberTypes.All))] Type interfaceType)
        {
            ThrowIfCreated();

            _interfaces ??= new List<Type>();
            _interfaces.Add(interfaceType);
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2083:DynamicallyAccessedMembers", Justification = "Not sure how to handle warning on 'this'")]
        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        protected override TypeInfo CreateTypeInfoCore()
        {
            if (_isCreated)
            {
                return this;
            }

            if (_isHiddenGlobalType)
            {
                _isCreated = true;
                return null!;
            }

            // Create a public default constructor if this class has no constructor. Except the type is Interface, ValueType, Enum, or a static class.
            // (TypeAttributes.Abstract | TypeAttributes.Sealed) determines if the type is static
            if (_constructorDefinitions.Count == 0 && (_attributes & TypeAttributes.Interface) == 0 && !IsValueType &&
                ((_attributes & (TypeAttributes.Abstract | TypeAttributes.Sealed)) != (TypeAttributes.Abstract | TypeAttributes.Sealed)))
            {
                DefineDefaultConstructor(MethodAttributes.Public);
            }

            ValidateMethods();
            _isCreated = true;

            return this;
        }

        private void ValidateMethods()
        {
            for (int i = 0; i < _methodDefinitions.Count; i++)
            {
                MethodBuilderImpl method = _methodDefinitions[i];
                MethodAttributes methodAttrs = method.Attributes;

                // Any of these flags in the implementation flags is set, we will not check the IL method body
                if (((method.GetMethodImplementationFlags() & (MethodImplAttributes.CodeTypeMask | MethodImplAttributes.PreserveSig | MethodImplAttributes.Unmanaged)) != MethodImplAttributes.IL) ||
                    ((methodAttrs & MethodAttributes.PinvokeImpl) != 0))
                {
                    continue;
                }

                ILGeneratorImpl? body = method.ILGeneratorImpl;
                if ((methodAttrs & MethodAttributes.Abstract) != 0)
                {
                    // Check if an abstract method declared on a non-abstract class
                    if ((_attributes & TypeAttributes.Abstract) == 0)
                    {
                        throw new InvalidOperationException(SR.InvalidOperation_BadTypeAttributesNotAbstract);
                    }

                    // If this is an abstract method or an interface should not have body.
                    if (body != null && body.ILOffset > 0)
                    {
                        throw new InvalidOperationException(SR.Format(SR.InvalidOperation_BadMethodBody, method.Name));
                    }
                }
                else if ((body == null || body.ILOffset == 0) && !method._canBeRuntimeImpl)
                {
                    throw new InvalidOperationException(SR.Format(SR.InvalidOperation_BadEmptyMethodBody, method.Name));
                }
            }
        }

        internal void ThrowIfCreated()
        {
            if (_isCreated)
            {
                throw new InvalidOperationException(SR.InvalidOperation_TypeHasBeenCreated);
            }
        }

        protected override ConstructorBuilder DefineConstructorCore(MethodAttributes attributes, CallingConventions callingConvention,
            Type[]? parameterTypes, Type[][]? requiredCustomModifiers, Type[][]? optionalCustomModifiers)
        {
            if ((_attributes & TypeAttributes.Interface) == TypeAttributes.Interface && (attributes & MethodAttributes.Static) != MethodAttributes.Static)
            {
                throw new InvalidOperationException(SR.InvalidOperation_ConstructorNotAllowedOnInterface);
            }

            ThrowIfCreated();

            string name;
            if ((attributes & MethodAttributes.Static) == 0)
            {
                name = ConstructorInfo.ConstructorName;
            }
            else
            {
                name = ConstructorInfo.TypeConstructorName;
            }

            attributes |= MethodAttributes.SpecialName | MethodAttributes.RTSpecialName;
            ConstructorBuilderImpl constBuilder = new ConstructorBuilderImpl(name, attributes, callingConvention,
                parameterTypes, requiredCustomModifiers, optionalCustomModifiers, _module, this);
            _constructorDefinitions.Add(constBuilder);
            return constBuilder;
        }

        protected override ConstructorBuilder DefineDefaultConstructorCore(MethodAttributes attributes)
        {
            if ((_attributes & TypeAttributes.Interface) == TypeAttributes.Interface)
            {
                throw new InvalidOperationException(SR.InvalidOperation_ConstructorNotAllowedOnInterface);
            }

            return DefineDefaultConstructorInternal(attributes);
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2075:UnrecognizedReflectionPattern",
            Justification = "GetConstructor is only called on a TypeBuilderInstantiation which is not subject to trimming")]
        private ConstructorBuilderImpl DefineDefaultConstructorInternal(MethodAttributes attributes)
        {
            // Get the parent class's default constructor and add it to the IL
            ConstructorInfo? con;
            if (_typeParent!.IsConstructedGenericType && _typeParent.GetGenericTypeDefinition() is TypeBuilderImpl typeBuilder)
            {
                con = GetConstructor(_typeParent, typeBuilder.GetConstructor(
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, EmptyTypes, null)!);
            }
            else
            {
                con = _typeParent.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, EmptyTypes, null);
            }

            if (con == null)
            {
                throw new NotSupportedException(SR.NotSupported_NoParentDefaultConstructor);
            }

            ConstructorBuilderImpl constBuilder = (ConstructorBuilderImpl)DefineConstructorCore(attributes, CallingConventions.Standard, null, null, null);

            // generate the code to call the parent's default constructor
            ILGenerator il = constBuilder.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Call, con);
            il.Emit(OpCodes.Ret);

            constBuilder._isDefaultConstructor = true;
            return constBuilder;
        }

        protected override EventBuilder DefineEventCore(string name, EventAttributes attributes, Type eventtype)
        {
            ArgumentNullException.ThrowIfNull(eventtype);
            ThrowIfCreated();

            EventBuilderImpl eventBuilder = new EventBuilderImpl(name, attributes, eventtype, this);
            _eventDefinitions.Add(eventBuilder);
            return eventBuilder;
        }

        protected override FieldBuilder DefineFieldCore(string fieldName, Type type, Type[]? requiredCustomModifiers, Type[]? optionalCustomModifiers, FieldAttributes attributes)
        {
            ThrowIfCreated();

            if (_enumUnderlyingType == null && IsEnum)
            {
                if ((attributes & FieldAttributes.Static) == 0)
                {
                    // remember the underlying type for enum type
                    _enumUnderlyingType = type;
                }
            }

            var field = new FieldBuilderImpl(this, fieldName, type, attributes, requiredCustomModifiers, optionalCustomModifiers);
            _fieldDefinitions.Add(field);
            return field;
        }

        protected override GenericTypeParameterBuilder[] DefineGenericParametersCore(params string[] names)
        {
            if (_typeParameters != null)
            {
                throw new InvalidOperationException();
            }

            var typeParameters = new GenericTypeParameterBuilderImpl[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                string name = names[i];
                ArgumentNullException.ThrowIfNull(name, nameof(names));
                typeParameters[i] = new GenericTypeParameterBuilderImpl(name, i, this);
            }

            return _typeParameters = typeParameters;
        }

        protected override FieldBuilder DefineInitializedDataCore(string name, byte[] data, FieldAttributes attributes)
        {
            if (data.Length <= 0 || data.Length >= 0x003f0000)
            {
                throw new ArgumentException(SR.Argument_BadSizeForData, nameof(data.Length));
            }

            // This method will define an initialized Data in .sdata.
            // We will create a fake TypeDef to represent the data with size. This TypeDef
            // will be the signature for the Field.
            return DefineDataHelper(name, data, data.Length, attributes);
        }

        protected override MethodBuilder DefineMethodCore(string name, MethodAttributes attributes, CallingConventions callingConvention,
            Type? returnType, Type[]? returnTypeRequiredCustomModifiers, Type[]? returnTypeOptionalCustomModifiers, Type[]? parameterTypes,
            Type[][]? parameterTypeRequiredCustomModifiers, Type[][]? parameterTypeOptionalCustomModifiers)
        {
            ThrowIfCreated();


            MethodBuilderImpl methodBuilder = new(name, attributes, callingConvention, returnType, returnTypeRequiredCustomModifiers,
                returnTypeOptionalCustomModifiers, parameterTypes, parameterTypeRequiredCustomModifiers, parameterTypeOptionalCustomModifiers, _module, this);
            _methodDefinitions.Add(methodBuilder);
            return methodBuilder;
        }

        protected override void DefineMethodOverrideCore(MethodInfo methodInfoBody, MethodInfo methodInfoDeclaration)
        {
            ThrowIfCreated();
            if (!ReferenceEquals(methodInfoBody.DeclaringType, this))
            {
                // Loader restriction: body method has to be from this class
                throw new ArgumentException(SR.ArgumentException_BadMethodImplBody);
            }
            Type baseType = methodInfoDeclaration.DeclaringType!;
            ValidateBaseType(baseType, methodInfoDeclaration.Name);
            ValidateImplementedMethod(methodInfoBody, methodInfoDeclaration);
            _methodOverrides ??= new();

            if (_methodOverrides.TryGetValue(baseType, out List<(MethodInfo ifaceMethod, MethodInfo targetMethod)>? im))
            {
                if (im.Exists(pair => pair.ifaceMethod.Equals(methodInfoDeclaration)))
                {
                    throw new ArgumentException(SR.Format(SR.Argument_MethodOverriden, methodInfoBody.Name, FullName), nameof(methodInfoDeclaration));
                }

                im.Add((methodInfoDeclaration, methodInfoBody));
            }
            else
            {
                im = new List<(MethodInfo ifaceMethod, MethodInfo targetMethod)>();
                im.Add((methodInfoDeclaration, methodInfoBody));
                _methodOverrides.Add(baseType, im);
            }
        }

        private void ValidateBaseType(Type baseType, string methodName)
        {
            if (baseType.IsInterface)
            {
                if (!IsInterfaceImplemented(baseType))
                {
                    throw ArgumentExceptionInvalidMethodOverride(methodName);
                }

                return;
            }

            if (!baseType.IsAssignableFrom(_typeParent))
            {
                throw ArgumentExceptionInvalidMethodOverride(methodName);
            }
        }

        private bool IsInterfaceImplemented(Type interfaceType)
        {
            if (_interfaces != null)
            {
                foreach (Type iface in _interfaces)
                {
                    if (interfaceType.IsAssignableFrom(iface))
                    {
                        return true;
                    }
                }
            }

            if (_typeParent != null)
            {
                foreach (Type @interface in _typeParent.GetInterfaces())
                {
                    if (interfaceType.IsAssignableFrom(@interface))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private ArgumentException ArgumentExceptionInvalidMethodOverride(string methodName) =>
            new ArgumentException(SR.Format(SR.Argument_InvalidMethodOverride, FullName, methodName), "methodInfoBody");

        private void ValidateImplementedMethod(MethodInfo methodInfoBody, MethodInfo methodInfoDeclaration)
        {
            if ((methodInfoBody.IsVirtual || methodInfoBody.IsStatic) &&
                (methodInfoDeclaration.IsAbstract || methodInfoDeclaration.IsVirtual) &&
                methodInfoBody.ReturnType.Equals(methodInfoDeclaration.ReturnType))
            {
                ParameterInfo[] bodyParameters = methodInfoBody.GetParameters();
                ParameterInfo[] declarationParameters = methodInfoDeclaration.GetParameters();
                if (bodyParameters.Length == declarationParameters.Length)
                {
                    for (int i = 0; i < bodyParameters.Length; i++)
                    {
                        Type? bodyType = bodyParameters[i].ParameterType;
                        Type? declType = declarationParameters[i].ParameterType;

                        if (bodyType.IsArray != declType.IsArray ||
                            bodyType.IsByRef != declType.IsByRef ||
                            bodyType.IsPointer != declType.IsPointer)
                        {
                            throw ArgumentExceptionInvalidMethodOverride(methodInfoDeclaration.Name);
                        }

                        if (bodyType.HasElementType || declType.HasElementType)
                        {
                            bodyType = bodyType.GetElementType();
                            declType = declType.GetElementType();
                        }

                        if (bodyType == null || !bodyType.Equals(declType))
                        {
                            throw ArgumentExceptionInvalidMethodOverride(methodInfoDeclaration.Name);
                        }
                    }

                    return;
                }
            }

            throw ArgumentExceptionInvalidMethodOverride(methodInfoDeclaration.Name);
        }

        protected override TypeBuilder DefineNestedTypeCore(string name, TypeAttributes attr,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type? parent, Type[]? interfaces, PackingSize packSize, int typeSize)
        {
            return _module.DefineNestedType(name, attr, parent, interfaces, packSize, typeSize, this);
        }

        [RequiresUnreferencedCode("P/Invoke marshalling may dynamically access members that could be trimmed.")]
        protected override MethodBuilder DefinePInvokeMethodCore(string name, string dllName, string entryName, MethodAttributes attributes,
            CallingConventions callingConvention, Type? returnType, Type[]? returnTypeRequiredCustomModifiers, Type[]? returnTypeOptionalCustomModifiers, Type[]? parameterTypes,
            Type[][]? parameterTypeRequiredCustomModifiers, Type[][]? parameterTypeOptionalCustomModifiers, CallingConvention nativeCallConv, CharSet nativeCharSet)
        {
            if ((attributes & MethodAttributes.Abstract) != 0)
            {
                throw new ArgumentException(SR.Argument_BadPInvokeMethod);
            }

            if ((_attributes & TypeAttributes.ClassSemanticsMask) == TypeAttributes.Interface)
            {
                throw new ArgumentException(SR.Argument_BadPInvokeOnInterface);
            }

            ThrowIfCreated();

            attributes |= MethodAttributes.PinvokeImpl;
            MethodBuilderImpl method = new MethodBuilderImpl(name, attributes, callingConvention, returnType, returnTypeRequiredCustomModifiers,
                returnTypeOptionalCustomModifiers, parameterTypes, parameterTypeRequiredCustomModifiers, parameterTypeOptionalCustomModifiers, _module, this);
            method.CreateDllImportData(dllName, entryName, nativeCallConv, nativeCharSet);

            if (_methodDefinitions.Contains(method))
            {
                throw new ArgumentException(SR.Argument_MethodRedefined);
            }
            _methodDefinitions.Add(method);

            return method;
        }

        protected override PropertyBuilder DefinePropertyCore(string name, PropertyAttributes attributes, CallingConventions callingConvention,
            Type returnType, Type[]? returnTypeRequiredCustomModifiers, Type[]? returnTypeOptionalCustomModifiers, Type[]? parameterTypes,
            Type[][]? parameterTypeRequiredCustomModifiers, Type[][]? parameterTypeOptionalCustomModifiers)
        {
            PropertyBuilderImpl property = new PropertyBuilderImpl(name, attributes, callingConvention, returnType, returnTypeRequiredCustomModifiers,
                returnTypeOptionalCustomModifiers, parameterTypes, parameterTypeRequiredCustomModifiers, parameterTypeOptionalCustomModifiers, this);
            _propertyDefinitions.Add(property);
            return property;
        }

        protected override ConstructorBuilder DefineTypeInitializerCore()
        {
            ThrowIfCreated();

            const MethodAttributes attr = MethodAttributes.Private | MethodAttributes.Static | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName;

            return new ConstructorBuilderImpl(ConstructorInfo.TypeConstructorName, attr, CallingConventions.Standard, null, null, null, _module, this);
        }

        protected override FieldBuilder DefineUninitializedDataCore(string name, int size, FieldAttributes attributes)
        {
            if (size <= 0 || size >= 0x003f0000)
            {
                throw new ArgumentException(SR.Argument_BadSizeForData, nameof(size));
            }

            // This method will define an uninitialized Data in .sdata.
            // We will create a fake TypeDef to represent the data with size. This TypeDef
            // will be the signature for the Field.
            return DefineDataHelper(name, new byte[size], size, attributes);
        }

        private FieldBuilder DefineDataHelper(string name, byte[] data, int size, FieldAttributes attributes)
        {
            ArgumentException.ThrowIfNullOrEmpty(name);

            ThrowIfCreated();

            // form the value class name
            string strValueClassName = $"$ArrayType${size}";

            // Is this already defined in this module?
            TypeBuilderImpl? valueClassType = _module.FindTypeBuilderWithName(strValueClassName, false);

            if (valueClassType == null)
            {
                TypeAttributes typeAttributes = TypeAttributes.Public | TypeAttributes.ExplicitLayout | TypeAttributes.Class | TypeAttributes.Sealed | TypeAttributes.AnsiClass;

                // Define the backing value class
                valueClassType = (TypeBuilderImpl)_module.DefineType(strValueClassName, typeAttributes, typeof(ValueType), PackingSize.Size1, size);
                valueClassType.CreateType();
            }

            FieldBuilder fdBuilder = DefineField(name, valueClassType, attributes | FieldAttributes.Static | FieldAttributes.HasFieldRVA);

            // now we need to set the RVA
            ((FieldBuilderImpl)fdBuilder).SetData(data);
            return fdBuilder;
        }

        protected override bool IsCreatedCore() => _isCreated;

        protected override void SetCustomAttributeCore(ConstructorInfo con, ReadOnlySpan<byte> binaryAttribute)
        {
            // Handle pseudo custom attributes
            switch (con.ReflectedType!.FullName)
            {
                case "System.Runtime.InteropServices.StructLayoutAttribute":
                    ParseStructLayoutAttribute(con, binaryAttribute);
                    return;
                case "System.Runtime.CompilerServices.SpecialNameAttribute":
                    _attributes |= TypeAttributes.SpecialName;
                    return;
                case "System.SerializableAttribute":
#pragma warning disable SYSLIB0050 // 'TypeAttributes.Serializable' is obsolete: 'Formatter-based serialization is obsolete and should not be used'.
                    _attributes |= TypeAttributes.Serializable;
#pragma warning restore SYSLIB0050
                    return;
                case "System.Runtime.InteropServices.ComImportAttribute":
                    _attributes |= TypeAttributes.Import;
                    return;
                case "System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeImportAttribute":
                    _attributes |= TypeAttributes.WindowsRuntime;
                    return;
                case "System.Security.SuppressUnmanagedCodeSecurityAttribute": // It says has no effect in .NET Core, maybe remove?
                    _attributes |= TypeAttributes.HasSecurity;
                    break;
            }

            _customAttributes ??= new List<CustomAttributeWrapper>();
            _customAttributes.Add(new CustomAttributeWrapper(con, binaryAttribute));
        }

        internal void SetCustomAttribute(ConstructorInfo con, ReadOnlySpan<byte> binaryAttribute)
        {
            SetCustomAttributeCore(con, binaryAttribute);
        }

        private void ParseStructLayoutAttribute(ConstructorInfo con, ReadOnlySpan<byte> binaryAttribute)
        {
            CustomAttributeInfo attributeInfo = CustomAttributeInfo.DecodeCustomAttribute(con, binaryAttribute);
            LayoutKind layoutKind = (LayoutKind)attributeInfo._ctorArgs[0]!;
            _attributes &= ~TypeAttributes.LayoutMask;
            _attributes |= layoutKind switch
            {
                LayoutKind.Auto => TypeAttributes.AutoLayout,
                LayoutKind.Explicit => TypeAttributes.ExplicitLayout,
                LayoutKind.Sequential => TypeAttributes.SequentialLayout,
                _ => TypeAttributes.AutoLayout,
            };

            for (int i = 0; i < attributeInfo._namedParamNames.Length; ++i)
            {
                string name = attributeInfo._namedParamNames[i];
                int value = (int)attributeInfo._namedParamValues[i]!;

                switch (name)
                {
                    case "CharSet":
                        switch ((CharSet)value)
                        {
                            case CharSet.None:
                            case CharSet.Ansi:
                                _attributes &= ~(TypeAttributes.UnicodeClass | TypeAttributes.AutoClass);
                                break;
                            case CharSet.Unicode:
                                _attributes &= ~TypeAttributes.AutoClass;
                                _attributes |= TypeAttributes.UnicodeClass;
                                break;
                            case CharSet.Auto:
                                _attributes &= ~TypeAttributes.UnicodeClass;
                                _attributes |= TypeAttributes.AutoClass;
                                break;
                        }
                        break;
                    case "Pack":
                        _packingSize = (PackingSize)value;
                        break;
                    case "Size":
                        _typeSize = value;
                        break;
                    default:
                        throw new ArgumentException(SR.Format(SR.Argument_UnknownNamedType, con.DeclaringType, name), nameof(binaryAttribute));
                }
            }
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2074:DynamicallyAccessedMembers",
            Justification = "System.Object type is preserved via ModulBuilderImpl.s_coreTypes")]
        protected override void SetParentCore([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type? parent)
        {
            ThrowIfCreated();

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
        public override Type? DeclaringType => _declaringType;
        public override Type? ReflectedType => _declaringType;
        public override bool IsGenericTypeDefinition => IsGenericType;
        public override bool IsConstructedGenericType => false;
        public override bool IsGenericType => _typeParameters != null;
        // Not returning a copy for compat with existing runtime behavior
        public override Type[] GenericTypeParameters => _typeParameters ?? EmptyTypes;
        public override Type[] GetGenericArguments() => _typeParameters ?? EmptyTypes;
        public override Type GetGenericTypeDefinition()
        {
            if (IsGenericTypeDefinition)
            {
                return this;
            }

            throw new InvalidOperationException();
        }
        public override bool IsDefined(Type attributeType, bool inherit) => throw new NotImplementedException();
        public override object[] GetCustomAttributes(bool inherit) => throw new NotImplementedException();
        public override object[] GetCustomAttributes(Type attributeType, bool inherit) => throw new NotImplementedException();
        // You will never have to deal with a TypeBuilder if you are just referring to arrays.
        public override Type GetElementType() => throw new NotSupportedException(SR.NotSupported_DynamicModule);
        public override string? AssemblyQualifiedName => throw new NotSupportedException();
        public override string? FullName => _strFullName ??= TypeNameBuilder.ToString(this, TypeNameBuilder.Format.FullName);
        public override string? Namespace => _namespace;
        public override Assembly Assembly => _module.Assembly;
        public override Module Module => _module;
        public override Type UnderlyingSystemType
        {
            get
            {
                if (IsEnum)
                {
                    if (_enumUnderlyingType == null)
                    {
                        throw new InvalidOperationException(SR.InvalidOperation_NoUnderlyingTypeOnEnum);
                    }

                    return _enumUnderlyingType;
                }
                else
                {
                    return this;
                }
            }
        }
        public override bool IsSZArray => false;
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

        internal void ThrowIfNotCreated()
        {
            if (!_isCreated)
            {
                throw new NotSupportedException(SR.NotSupported_TypeNotYetCreated);
            }
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
        protected override ConstructorInfo? GetConstructorImpl(BindingFlags bindingAttr, Binder? binder,
                CallingConventions callConvention, Type[] types, ParameterModifier[]? _)
        {
            ThrowIfNotCreated();
            ArgumentNullException.ThrowIfNull(types);

            for (int i = 0; i < types.Length; i++)
            {
                ArgumentNullException.ThrowIfNull(types[i], nameof(types));
            }

            foreach (ConstructorBuilderImpl con in _constructorDefinitions)
            {
                if (MatchesTheFilter(con._methodBuilder, GetBindingFlags(con._methodBuilder), bindingAttr, callConvention, types))
                {
                    return con;
                }
            }

            return null;
        }

        internal static BindingFlags GetBindingFlags(MethodInfo method)
        {
            BindingFlags bindingFlags = (method.Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Public ?
                BindingFlags.Public : BindingFlags.NonPublic;
            bindingFlags |= (method.Attributes & MethodAttributes.Static) != 0 ? BindingFlags.Static : BindingFlags.Instance;

            return bindingFlags;
        }

        private static bool MatchesTheFilter(MethodBuilderImpl method, BindingFlags methodFlags, BindingFlags bindingFlags, CallingConventions callConv, Type[]? argumentTypes)
        {
            if ((bindingFlags & methodFlags) != methodFlags)
            {
                return false;
            }

            if ((callConv & CallingConventions.Any) == 0)
            {
                if ((callConv & CallingConventions.VarArgs) != 0 && (method.CallingConvention & CallingConventions.VarArgs) == 0)
                {
                    return false;
                }

                if ((callConv & CallingConventions.Standard) != 0 && (method.CallingConvention & CallingConventions.Standard) == 0)
                {
                    return false;
                }
            }

            if (argumentTypes == null)
            {
                // Not filtering by parameter types
                return true;
            }

            Type[] parameterTypes = method.ParameterTypes ?? EmptyTypes;
            if (argumentTypes.Length != parameterTypes.Length)
            {
                return false;
            }

            for (int i = 0; i < parameterTypes.Length; i++)
            {
                Type? argType = argumentTypes[i];
                Type? paramType = parameterTypes[i];

                if (argType.IsArray != paramType.IsArray ||
                    argType.IsByRef != paramType.IsByRef ||
                    argType.IsPointer != argType.IsPointer)
                {
                    return false;
                }

                if (argType.HasElementType || paramType.HasElementType)
                {
                    argType = argType.GetElementType();
                    paramType = paramType.GetElementType();
                }

                if (argType == null || !argType.Equals(paramType))
                {
                    return false;
                }
            }

            return true;
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
        public override ConstructorInfo[] GetConstructors(BindingFlags bindingAttr)
        {
            ThrowIfNotCreated();

            List<ConstructorInfo> ctors = new();
            foreach (ConstructorBuilderImpl con in _constructorDefinitions)
            {
                if (MatchesTheFilter(con._methodBuilder, GetBindingFlags(con._methodBuilder), bindingAttr, CallingConventions.Any, con._methodBuilder.ParameterTypes))
                {
                    ctors.Add(con);
                }
            }

            return ctors.ToArray();
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents)]
        public override EventInfo[] GetEvents() => throw new NotSupportedException();
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents)]
        public override EventInfo[] GetEvents(BindingFlags bindingAttr) => throw new NotSupportedException();
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents)]
        public override EventInfo? GetEvent(string name, BindingFlags bindingAttr) => throw new NotSupportedException();

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
        public override MethodInfo[] GetMethods(BindingFlags bindingAttr)
        {
            ThrowIfNotCreated();

            List<MethodInfo> methods = new();
            foreach (MethodBuilderImpl method in _methodDefinitions)
            {
                if (!method.IsConstructor && MatchesTheFilter(method, GetBindingFlags(method), bindingAttr, CallingConventions.Any, method.ParameterTypes))
                {
                    methods.Add(method);
                }
            }

            if (!bindingAttr.HasFlag(BindingFlags.DeclaredOnly) && _typeParent != null)
            {
                methods.AddRange(_typeParent.GetMethods(bindingAttr));
            }

            return methods.ToArray();
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
        protected override MethodInfo? GetMethodImpl(string name, BindingFlags bindingAttr, Binder? binder,
                CallingConventions callConvention, Type[]? types, ParameterModifier[]? modifiers)
        {
            ThrowIfNotCreated();

            MethodInfo? found = null;
            foreach (MethodBuilderImpl method in _methodDefinitions)
            {
                if (name.Equals(method.Name) && MatchesTheFilter(method, GetBindingFlags(method), bindingAttr, callConvention, types))
                {
                    if (found != null)
                    {
                        throw new AmbiguousMatchException(SR.Format(SR.AmbiguousMatch_MemberInfo, method.DeclaringType, method.Name));
                    }

                    found = method;
                }
            }

            if (found == null && !bindingAttr.HasFlag(BindingFlags.DeclaredOnly) && _typeParent != null)
            {
                if (types == null)
                {
                    found = _typeParent.GetMethod(name, bindingAttr);
                }
                else
                {
                    found = _typeParent.GetMethod(name, bindingAttr, binder, callConvention, types, modifiers);
                }
            }

            return found;
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)]
        public override FieldInfo? GetField(string name, BindingFlags bindingFlags)
        {
            ArgumentNullException.ThrowIfNull(name);
            ThrowIfNotCreated();

            FieldInfo? match = null;
            StringComparison compare = (bindingFlags & BindingFlags.IgnoreCase) != 0 ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            foreach (FieldBuilderImpl fieldInfo in _fieldDefinitions)
            {
                BindingFlags fieldFlags = GetBindingFlags(fieldInfo);
                if (name.Equals(fieldInfo.Name, compare) && (bindingFlags & fieldFlags) == fieldFlags)
                {
                    if (match != null && ReferenceEquals(fieldInfo.DeclaringType, match.DeclaringType))
                    {
                        // TypeBuilder doesn't validate for duplicates when fields are defined, throw if duplicates found.
                        throw new AmbiguousMatchException(SR.Format(SR.AmbiguousMatch_MemberInfo, fieldInfo.DeclaringType, fieldInfo.Name));
                    }

                    match = fieldInfo;
                }
            }

            if (match == null && !bindingFlags.HasFlag(BindingFlags.DeclaredOnly) && _typeParent != null)
            {
                match = _typeParent.GetField(name, bindingFlags);
            }

            return match;
        }

        private static BindingFlags GetBindingFlags(FieldBuilderImpl field)
        {
            BindingFlags bindingFlags = (field.Attributes & FieldAttributes.FieldAccessMask) == FieldAttributes.Public ?
                BindingFlags.Public : BindingFlags.NonPublic;

            bindingFlags |= (field.Attributes & FieldAttributes.Static) != 0 ? BindingFlags.Static : BindingFlags.Instance;

            return bindingFlags;
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)]
        public override FieldInfo[] GetFields(BindingFlags bindingAttr)
        {
            ThrowIfNotCreated();

            List<FieldBuilderImpl> candidates = new List<FieldBuilderImpl>(_fieldDefinitions.Count);
            for (int i = 0; i < _fieldDefinitions.Count; i++)
            {
                FieldBuilderImpl fieldInfo = _fieldDefinitions[i];
                BindingFlags fieldFlags = GetBindingFlags(fieldInfo);
                if ((bindingAttr & fieldFlags) == fieldFlags)
                {
                    candidates.Add(fieldInfo);
                }
            }

            return candidates.ToArray();
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
        public override Type? GetInterface(string name, bool ignoreCase) => throw new NotSupportedException();

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
        public override Type[] GetInterfaces() => _interfaces == null ? EmptyTypes : _interfaces.ToArray();

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

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2085:DynamicallyAccessedMembers", Justification = "Methods are loaded from this TypeBuilder")]
        public override InterfaceMapping GetInterfaceMap([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)] Type interfaceType)
        {
            ThrowIfNotCreated();
            ArgumentNullException.ThrowIfNull(interfaceType);
            ValidateInterfaceType(interfaceType);

            MethodInfo[] interfaceMethods = interfaceType.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            InterfaceMapping im = new InterfaceMapping
            {
                InterfaceType = interfaceType,
                TargetType = this,
                InterfaceMethods = new MethodInfo[interfaceMethods.Length],
                TargetMethods = new MethodInfo[interfaceMethods.Length]
            };

            for (int i = 0; i < interfaceMethods.Length; i++)
            {
                MethodInfo interfaceMethod = interfaceMethods[i];
                MethodInfo? targetMethod = GetMethodImpl(interfaceMethod.Name, GetBindingFlags(interfaceMethod), null, interfaceMethod.CallingConvention, GetParameterTypes(interfaceMethod.GetParameters()), null);

                im.InterfaceMethods[i] = interfaceMethod;
                im.TargetMethods[i] = targetMethod!;
            }

            if (_methodOverrides == null)
            {
                return im;
            }

            // Add the interface implementations defined with 'DefineMethodOverride',
            // this overrides default implementations if there is any
            if (_methodOverrides.TryGetValue(interfaceType, out var mapping))
            {
                for (int i = 0; i < interfaceMethods.Length; i++)
                {
                    for (int j = 0; j < mapping.Count; j++)
                    {
                        if (im.InterfaceMethods[i].Equals(mapping[j].ifaceMethod))
                        {
                            im.TargetMethods[i] = mapping[j].targetMethod;
                        }
                    }
                }
            }

            return im;
        }

        private static Type[] GetParameterTypes(ParameterInfo[] parameterInfos)
        {
            Type[] parameterTypes = new Type[parameterInfos.Length];
            for (int i = 0; i < parameterInfos.Length; i++)
            {
                parameterTypes[i] = parameterInfos[i].ParameterType;
            }
            return parameterTypes;
        }

        private void ValidateInterfaceType(Type interfaceType)
        {
            if (!interfaceType.IsInterface)
            {
                throw new ArgumentException(SR.Argument_MustBeInterface, nameof(interfaceType));
            }

            if (!IsInterfaceImplemented(interfaceType))
            {
                throw new ArgumentException(SR.Argument_InterfaceNotFound, nameof(interfaceType));
            }

            if (IsGenericParameter)
            {
                throw new InvalidOperationException(SR.Argument_GenericParameter);
            }
        }

        [DynamicallyAccessedMembers(GetAllMembers)]
        public override MemberInfo[] GetMembers(BindingFlags bindingAttr) => throw new NotSupportedException();

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2070:UnrecognizedReflectionPattern",
            Justification = "The GetInterfaces technically requires all interfaces to be preserved" +
                "But in this case it acts only on TypeBuilder which is never trimmed (as it's runtime created).")]
        public override bool IsAssignableFrom([NotNullWhen(true)] Type? c)
        {
            if (AreTypesEqual(this, c))
            {
                return true;
            }

            if (c is TypeBuilderImpl tb)
            {
                if (tb.IsSubclassOf(this))
                {
                    return true;
                }

                // in order to be a parent of interface this should be an interface
                if (!IsInterface)
                {
                    return false;
                }

                Type[] interfaces = tb.GetInterfaces();
                for (int i = 0; i < interfaces.Length; i++)
                {
                    // IsSubclassOf does not cover the case when they are the same type.
                    if (AreTypesEqual(interfaces[i], this))
                    {
                        return true;
                    }

                    if (interfaces[i].IsSubclassOf(this))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        internal static bool AreTypesEqual(Type t1, Type? t2)
        {
            if (t1 == t2)
            {
                return true;
            }

            if (t1 is TypeBuilderImpl rtb1)
            {
                if (t2 is TypeBuilderImpl tb && ReferenceEquals(rtb1, tb))
                {
                    return true;
                }
            }

            return false;
        }

        public override bool IsSubclassOf(Type c)
        {
            Type? p = this;

            if (AreTypesEqual(p, c))
            {
                return false;
            }

            p = p.BaseType;

            while (p != null)
            {
                if (AreTypesEqual(p, c))
                {
                    return true;
                }

                p = p.BaseType;
            }

            return false;
        }

        internal const DynamicallyAccessedMemberTypes GetAllMembers = DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields |
            DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods |
            DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents |
            DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties |
            DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors |
            DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.NonPublicNestedTypes;
    }
}
