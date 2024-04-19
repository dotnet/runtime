// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Reflection.Emit
{
    internal sealed partial class RuntimeModuleBuilder : ModuleBuilder
    {
        internal static string UnmangleTypeName(string typeName)
        {
            // Gets the original type name, without '+' name mangling.

            int i = typeName.Length - 1;
            while (true)
            {
                i = typeName.LastIndexOf('+', i);
                if (i < 0)
                {
                    break;
                }

                bool evenSlashes = true;
                int iSlash = i;
                while (typeName[--iSlash] == '\\')
                {
                    evenSlashes = !evenSlashes;
                }

                // Even number of slashes means this '+' is a name separator
                if (evenSlashes)
                {
                    break;
                }

                i = iSlash;
            }

            return typeName.Substring(i + 1);
        }

        #region Internal Data Members

        // _typeBuilderDict contains both TypeBuilder and EnumBuilder objects
        private readonly Dictionary<string, Type> _typeBuilderDict;
        private readonly TypeBuilder _globalTypeBuilder;
        private bool _hasGlobalBeenCreated;

        internal readonly RuntimeModule _internalModule;
        // This is the "external" AssemblyBuilder
        // only the "external" ModuleBuilder has this set
        private readonly RuntimeAssemblyBuilder _assemblyBuilder;
        internal RuntimeAssemblyBuilder ContainingAssemblyBuilder => _assemblyBuilder;

        internal const string ManifestModuleName = "RefEmit_InMemoryManifestModule";

        #endregion

        #region Constructor

        internal RuntimeModuleBuilder(RuntimeAssemblyBuilder assemblyBuilder, RuntimeModule internalModule)
        {
            _internalModule = internalModule;
            _assemblyBuilder = assemblyBuilder;

            _globalTypeBuilder = new RuntimeTypeBuilder(this);
            _typeBuilderDict = new Dictionary<string, Type>();
        }

        #endregion

        #region Private Members
        internal void AddType(string name, Type type) => _typeBuilderDict.Add(name, type);

        internal void CheckTypeNameConflict(string strTypeName, Type? enclosingType)
        {
            if (_typeBuilderDict.TryGetValue(strTypeName, out Type? foundType) &&
                ReferenceEquals(foundType.DeclaringType, enclosingType))
            {
                // Cannot have two types with the same name
                throw new ArgumentException(SR.Argument_DuplicateTypeName);
            }
        }

        private static Type? GetType(string strFormat, Type baseType)
        {
            // This function takes a string to describe the compound type, such as "[,][]", and a baseType.
            if (string.IsNullOrEmpty(strFormat))
            {
                return baseType;
            }

            // convert the format string to byte array and then call FormCompoundType
            return SymbolType.FormCompoundType(strFormat, baseType, 0);
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "ModuleBuilder_GetTypeRef", StringMarshalling = StringMarshalling.Utf16)]
        private static partial int GetTypeRef(QCallModule module, string strFullName, QCallModule refedModule, int tkResolution);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "ModuleBuilder_GetMemberRef")]
        private static partial int GetMemberRef(QCallModule module, QCallModule refedModule, int tr, int defToken);

        private int GetMemberRef(Module? refedModule, int tr, int defToken)
        {
            RuntimeModuleBuilder thisModule = this;
            RuntimeModule refedRuntimeModule = GetRuntimeModuleFromModule(refedModule);

            return GetMemberRef(new QCallModule(ref thisModule), new QCallModule(ref refedRuntimeModule), tr, defToken);
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "ModuleBuilder_GetMemberRefFromSignature", StringMarshalling = StringMarshalling.Utf16)]
        private static partial int GetMemberRefFromSignature(QCallModule module, int tr, string methodName, byte[] signature, int length);

        private int GetMemberRefFromSignature(int tr, string methodName, byte[] signature, int length)
        {
            RuntimeModuleBuilder thisModule = this;
            return GetMemberRefFromSignature(new QCallModule(ref thisModule), tr, methodName, signature, length);
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "ModuleBuilder_GetMemberRefOfMethodInfo")]
        private static partial int GetMemberRefOfMethodInfo(QCallModule module, int tr, RuntimeMethodHandleInternal method);

        private int GetMemberRefOfMethodInfo(int tr, RuntimeMethodInfo method)
        {
            Debug.Assert(method != null);

            RuntimeModuleBuilder thisModule = this;
            int result = GetMemberRefOfMethodInfo(new QCallModule(ref thisModule), tr, ((IRuntimeMethodInfo)method).Value);
            GC.KeepAlive(method);
            return result;
        }

        private int GetMemberRefOfMethodInfo(int tr, RuntimeConstructorInfo method)
        {
            Debug.Assert(method != null);

            RuntimeModuleBuilder thisModule = this;
            int result = GetMemberRefOfMethodInfo(new QCallModule(ref thisModule), tr, ((IRuntimeMethodInfo)method).Value);
            GC.KeepAlive(method);
            return result;
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "ModuleBuilder_GetMemberRefOfFieldInfo")]
        private static partial int GetMemberRefOfFieldInfo(QCallModule module, int tkType, QCallTypeHandle declaringType, int tkField);

        private int GetMemberRefOfFieldInfo(int tkType, RuntimeTypeHandle declaringType, RuntimeFieldInfo runtimeField)
        {
            Debug.Assert(runtimeField != null);

            RuntimeModuleBuilder thisModule = this;
            return GetMemberRefOfFieldInfo(new QCallModule(ref thisModule), tkType, new QCallTypeHandle(ref declaringType), runtimeField.MetadataToken);
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "ModuleBuilder_GetTokenFromTypeSpec")]
        private static partial int GetTokenFromTypeSpec(QCallModule pModule, byte[] signature, int length);

        private int GetTokenFromTypeSpec(byte[] signature, int length)
        {
            RuntimeModuleBuilder thisModule = this;
            return GetTokenFromTypeSpec(new QCallModule(ref thisModule), signature, length);
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "ModuleBuilder_GetArrayMethodToken", StringMarshalling = StringMarshalling.Utf16)]
        private static partial int GetArrayMethodToken(QCallModule module, int tkTypeSpec, string methodName, byte[] signature, int sigLength);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "ModuleBuilder_GetStringConstant", StringMarshalling = StringMarshalling.Utf16)]
        private static partial int GetStringConstant(QCallModule module, string str, int length);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "ModuleBuilder_SetFieldRVAContent")]
        internal static partial void SetFieldRVAContent(QCallModule module, int fdToken, byte[]? data, int length);

        #endregion

        #region Internal Members

        internal Type? FindTypeBuilderWithName(string strTypeName, bool ignoreCase)
        {
            if (ignoreCase)
            {
                foreach (string name in _typeBuilderDict.Keys)
                {
                    if (string.Equals(name, strTypeName, StringComparison.OrdinalIgnoreCase))
                    {
                        return _typeBuilderDict[name];
                    }
                }
            }
            else
            {
                if (_typeBuilderDict.TryGetValue(strTypeName, out Type? foundType))
                {
                    return foundType;
                }
            }

            return null;
        }

        private int GetTypeRefNested(Type type, Module? refedModule)
        {
            // This function will generate correct TypeRef token for top level type and nested type.
            Type? enclosingType = type.DeclaringType;
            int tkResolution = 0;
            string typeName = type.FullName!;

            if (enclosingType != null)
            {
                tkResolution = GetTypeRefNested(enclosingType, refedModule);
                typeName = UnmangleTypeName(typeName);
            }

            Debug.Assert(!type.IsByRef, "Must not be ByRef. Get token from TypeSpec.");
            Debug.Assert(!type.IsGenericType || type.IsGenericTypeDefinition, "Must not have generic arguments.");

            RuntimeModuleBuilder thisModule = this;
            RuntimeModule refedRuntimeModule = GetRuntimeModuleFromModule(refedModule);
            return GetTypeRef(new QCallModule(ref thisModule), typeName, new QCallModule(ref refedRuntimeModule), tkResolution);
        }

        public override int GetMethodMetadataToken(ConstructorInfo constructor)
        {
            // Helper to get constructor token.
            int tr;
            int mr;

            if (constructor is ConstructorBuilder conBuilder)
            {
                if (conBuilder.Module.Equals(this))
                    return conBuilder.MetadataToken;

                // constructor is defined in a different module
                tr = GetTypeTokenInternal(constructor.ReflectedType!);
                mr = GetMemberRef(constructor.ReflectedType!.Module, tr, conBuilder.MetadataToken);
            }
            else if (constructor is ConstructorOnTypeBuilderInstantiation conOnTypeBuilderInst)
            {
                tr = GetTypeTokenInternal(constructor.DeclaringType!);
                mr = GetMemberRef(constructor.DeclaringType!.Module, tr, conOnTypeBuilderInst.MetadataToken);
            }
            else if (constructor is RuntimeConstructorInfo rtCon && !constructor.ReflectedType!.IsArray)
            {
                // constructor is not a dynamic field
                // We need to get the TypeRef tokens
                tr = GetTypeTokenInternal(constructor.ReflectedType);
                mr = GetMemberRefOfMethodInfo(tr, rtCon);
            }
            else
            {
                // some user derived ConstructorInfo
                // go through the slower code path, i.e. retrieve parameters and form signature helper.
                ParameterInfo[] parameters = constructor.GetParameters();
                if (parameters == null)
                {
                    throw new ArgumentException(SR.Argument_InvalidConstructorInfo);
                }

                Type[] parameterTypes = new Type[parameters.Length];
                Type[][] requiredCustomModifiers = new Type[parameters.Length][];
                Type[][] optionalCustomModifiers = new Type[parameters.Length][];

                for (int i = 0; i < parameters.Length; i++)
                {
                    if (parameters[i] == null)
                    {
                        throw new ArgumentException(SR.Argument_InvalidConstructorInfo);
                    }

                    parameterTypes[i] = parameters[i].ParameterType;
                    requiredCustomModifiers[i] = parameters[i].GetRequiredCustomModifiers();
                    optionalCustomModifiers[i] = parameters[i].GetOptionalCustomModifiers();
                }

                tr = GetTypeTokenInternal(constructor.ReflectedType!);

                SignatureHelper sigHelp = SignatureHelper.GetMethodSigHelper(this, constructor.CallingConvention, null, null, null, parameterTypes, requiredCustomModifiers, optionalCustomModifiers);
                byte[] sigBytes = sigHelp.InternalGetSignature(out int length);
                mr = GetMemberRefFromSignature(tr, constructor.Name, sigBytes, length);
            }

            return mr;
        }

        internal object SyncRoot => ContainingAssemblyBuilder.SyncRoot;

        #endregion

        #region Module Overrides

        internal RuntimeModule InternalModule => _internalModule;

        private protected override ModuleHandle GetModuleHandleImpl() => new ModuleHandle(InternalModule);

        internal static RuntimeModule GetRuntimeModuleFromModule(Module? m)
        {
            if (m is RuntimeModuleBuilder mb)
            {
                return mb.InternalModule;
            }

            return (m as RuntimeModule)!;
        }

        private int GetMemberRefToken(MethodBase method, Type[]? optionalParameterTypes)
        {
            int tkParent;
            int cGenericParameters = 0;
            SignatureHelper sigHelp;

            if (method.IsGenericMethod)
            {
                if (!method.IsGenericMethodDefinition)
                {
                    throw new InvalidOperationException();
                }

                cGenericParameters = method.GetGenericArguments().Length;
            }

            if (optionalParameterTypes != null)
            {
                if ((method.CallingConvention & CallingConventions.VarArgs) == 0)
                {
                    // Client should not supply optional parameter in default calling convention
                    throw new InvalidOperationException(SR.InvalidOperation_NotAVarArgCallingConvention);
                }
            }

            MethodInfo? masmi = method as MethodInfo;

            if (method.DeclaringType!.IsGenericType)
            {
                MethodBase methDef = GetGenericMethodBaseDefinition(method);

                sigHelp = GetMemberRefSignature(methDef, cGenericParameters);
            }
            else
            {
                sigHelp = GetMemberRefSignature(method, cGenericParameters);
            }

            if (optionalParameterTypes?.Length > 0)
            {
                sigHelp.AddSentinel();
                sigHelp.AddArguments(optionalParameterTypes, null, null);
            }

            byte[] sigBytes = sigHelp.InternalGetSignature(out int sigLength);

            if (method.DeclaringType!.IsGenericType)
            {
                byte[] sig = SignatureHelper.GetTypeSigToken(this, method.DeclaringType).InternalGetSignature(out int length);
                tkParent = GetTokenFromTypeSpec(sig, length);
            }
            else if (!method.Module.Equals(this))
            {
                // Use typeRef as parent because the method's declaringType lives in a different assembly
                tkParent = GetTypeMetadataToken(method.DeclaringType);
            }
            else
            {
                // Use methodDef as parent because the method lives in this assembly and its declaringType has no generic arguments
                if (masmi != null)
                    tkParent = GetMethodMetadataToken(masmi);
                else
                    tkParent = GetMethodMetadataToken((method as ConstructorInfo)!);
            }

            return GetMemberRefFromSignature(tkParent, method.Name, sigBytes, sigLength);
        }

        internal SignatureHelper GetMemberRefSignature(CallingConventions call, Type? returnType,
            Type[]? parameterTypes, Type[][]? requiredCustomModifiers, Type[][]? optionalCustomModifiers,
            Type[]? optionalParameterTypes, int cGenericParameters)
        {
            SignatureHelper sig = SignatureHelper.GetMethodSigHelper(this, call, cGenericParameters, returnType, null, null, parameterTypes, requiredCustomModifiers, optionalCustomModifiers);

            if (optionalParameterTypes != null && optionalParameterTypes.Length != 0)
            {
                sig.AddSentinel();
                sig.AddArguments(optionalParameterTypes, null, null);
            }

            return sig;
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "Module.ResolveMethod is marked as RequiresUnreferencedCode because it relies on tokens " +
                            "which are not guaranteed to be stable across trimming. So if somebody hardcodes a token it could break. " +
                            "The usage here is not like that as all these tokens come from existing metadata loaded from some IL " +
                            "and so trimming has no effect (the tokens are read AFTER trimming occurred).")]
        private static MethodBase GetGenericMethodBaseDefinition(MethodBase methodBase)
        {
            // methodInfo = G<Foo>.M<Bar> ==> methDef = G<T>.M<S>
            MethodInfo? masmi = methodBase as MethodInfo;
            MethodBase methDef;

            if (methodBase is MethodOnTypeBuilderInstantiation motbi)
            {
                methDef = motbi._method;
            }
            else if (methodBase is ConstructorOnTypeBuilderInstantiation cotbi)
            {
                methDef = cotbi._ctor;
            }
            else if (methodBase is MethodBuilder || methodBase is ConstructorBuilder)
            {
                // methodInfo must be GenericMethodDefinition; trying to emit G<?>.M<S>
                methDef = methodBase;
            }
            else
            {
                Debug.Assert(methodBase is RuntimeMethodInfo || methodBase is RuntimeConstructorInfo);

                if (methodBase.IsGenericMethod)
                {
                    Debug.Assert(masmi != null);

                    methDef = masmi.GetGenericMethodDefinition()!;
                    methDef = methDef.Module.ResolveMethod(
                        methodBase.MetadataToken,
                        methDef.DeclaringType?.GetGenericArguments(),
                        methDef.GetGenericArguments())!;
                }
                else
                {
                    methDef = methodBase.Module.ResolveMethod(
                        methodBase.MetadataToken,
                        methodBase.DeclaringType?.GetGenericArguments(),
                        null)!;
                }
            }

            return methDef;
        }

        internal SignatureHelper GetMemberRefSignature(MethodBase? method, int cGenericParameters)
        {
            switch (method)
            {
                case RuntimeMethodBuilder methodBuilder:
                    return methodBuilder.GetMethodSignature();
                case RuntimeConstructorBuilder constructorBuilder:
                    return constructorBuilder.GetMethodSignature();
                case MethodOnTypeBuilderInstantiation motbi when motbi._method is RuntimeMethodBuilder methodBuilder:
                    return methodBuilder.GetMethodSignature();
                case MethodOnTypeBuilderInstantiation motbi:
                    method = motbi._method;
                    break;
                case ConstructorOnTypeBuilderInstantiation cotbi when cotbi._ctor is RuntimeConstructorBuilder constructorBuilder:
                    return constructorBuilder.GetMethodSignature();
                case ConstructorOnTypeBuilderInstantiation cotbi:
                    method = cotbi._ctor;
                    break;
            }

            Debug.Assert(method is RuntimeMethodInfo || method is RuntimeConstructorInfo);
            ReadOnlySpan<ParameterInfo> parameters = method.GetParametersAsSpan();

            Type[] parameterTypes = new Type[parameters.Length];
            Type[][] requiredCustomModifiers = new Type[parameterTypes.Length][];
            Type[][] optionalCustomModifiers = new Type[parameterTypes.Length][];

            for (int i = 0; i < parameters.Length; i++)
            {
                parameterTypes[i] = parameters[i].ParameterType;
                requiredCustomModifiers[i] = parameters[i].GetRequiredCustomModifiers();
                optionalCustomModifiers[i] = parameters[i].GetOptionalCustomModifiers();
            }

            ParameterInfo? returnParameter = method is MethodInfo mi ? mi.ReturnParameter : null;
            SignatureHelper sigHelp = SignatureHelper.GetMethodSigHelper(this, method.CallingConvention, cGenericParameters, returnParameter?.ParameterType, returnParameter?.GetRequiredCustomModifiers(), returnParameter?.GetOptionalCustomModifiers(), parameterTypes, requiredCustomModifiers, optionalCustomModifiers);
            return sigHelp;
        }

        #endregion

        public override bool Equals(object? obj) => base.Equals(obj);

        public override int GetHashCode() => base.GetHashCode();

        #region ICustomAttributeProvider Members

        public override object[] GetCustomAttributes(bool inherit)
        {
            return InternalModule.GetCustomAttributes(inherit);
        }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            return InternalModule.GetCustomAttributes(attributeType, inherit);
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            return InternalModule.IsDefined(attributeType, inherit);
        }

        public override IList<CustomAttributeData> GetCustomAttributesData()
        {
            return InternalModule.GetCustomAttributesData();
        }

        #endregion

        #region Module Overrides

        [RequiresUnreferencedCode("Types might be removed")]
        public override Type[] GetTypes()
        {
            lock (SyncRoot)
            {
                return GetTypesNoLock();
            }
        }

        internal Type[] GetTypesNoLock()
        {
            Type[] typeList = new Type[_typeBuilderDict.Count];
            int i = 0;

            foreach (Type builder in _typeBuilderDict.Values)
            {
                RuntimeTypeBuilder tmpTypeBldr;

                if (builder is RuntimeEnumBuilder enumBldr)
                    tmpTypeBldr = enumBldr.m_typeBuilder;
                else
                    tmpTypeBldr = (RuntimeTypeBuilder)builder;

                // We should not return TypeBuilders.
                // Otherwise anyone can emit code in it.
                if (tmpTypeBldr.IsCreated())
                    typeList[i++] = tmpTypeBldr.UnderlyingSystemType;
                else
                    typeList[i++] = builder;
            }

            return typeList;
        }

        [RequiresUnreferencedCode("Types might be removed by trimming. If the type name is a string literal, consider using Type.GetType instead.")]
        public override Type? GetType(string className)
        {
            return GetType(className, false, false);
        }

        [RequiresUnreferencedCode("Types might be removed by trimming. If the type name is a string literal, consider using Type.GetType instead.")]
        public override Type? GetType(string className, bool ignoreCase)
        {
            return GetType(className, false, ignoreCase);
        }

        [RequiresUnreferencedCode("Types might be removed by trimming. If the type name is a string literal, consider using Type.GetType instead.")]
        public override Type? GetType(string className, bool throwOnError, bool ignoreCase)
        {
            lock (SyncRoot)
            {
                return GetTypeNoLock(className, throwOnError, ignoreCase);
            }
        }

        [RequiresUnreferencedCode("Types might be removed")]
        private Type? GetTypeNoLock(string className, bool throwOnError, bool ignoreCase)
        {
            // public API to a type. The reason that we need this function override from module
            // is because clients might need to get foo[] when foo is being built. For example, if
            // foo class contains a data member of type foo[].
            // This API first delegate to the Module.GetType implementation. If succeeded, great!
            // If not, we have to look up the current module to find the TypeBuilder to represent the base
            // type and form the Type object for "foo[,]".

            // Module.GetType() will verify className.
            Type? baseType = InternalModule.GetType(className, throwOnError, ignoreCase);
            if (baseType != null)
                return baseType;

            // Now try to see if we contain a TypeBuilder for this type or not.
            // Might have a compound type name, indicated via an unescaped
            // '[', '*' or '&'. Split the name at this point.
            string? baseName = null;
            string? parameters = null;
            int startIndex = 0;

            while (startIndex <= className.Length)
            {
                // Are there any possible special characters left?
                int i = className.AsSpan(startIndex).IndexOfAny('[', '*', '&');
                if (i < 0)
                {
                    // No, type name is simple.
                    baseName = className;
                    parameters = null;
                    break;
                }
                i += startIndex;

                // Found a potential special character, but it might be escaped.
                int slashes = 0;
                for (int j = i - 1; j >= 0 && className[j] == '\\'; j--)
                    slashes++;

                // Odd number of slashes indicates escaping.
                if (slashes % 2 == 1)
                {
                    startIndex = i + 1;
                    continue;
                }

                // Found the end of the base type name.
                baseName = className.Substring(0, i);
                parameters = className.Substring(i);
                break;
            }

            // If we didn't find a basename yet, the entire class name is
            // the base name and we don't have a composite type.
            if (baseName == null)
            {
                baseName = className;
                parameters = null;
            }

            baseName = baseName.Replace(@"\\", @"\").Replace(@"\[", "[").Replace(@"\*", "*").Replace(@"\&", "&");

            if (parameters != null)
            {
                // try to see if reflection can find the base type. It can be such that reflection
                // does not support the complex format string yet!
                baseType = InternalModule.GetType(baseName, false, ignoreCase);
            }

            if (baseType == null)
            {
                // try to find it among the unbaked types.
                baseType = FindTypeBuilderWithName(baseName, ignoreCase);
                if (baseType == null)
                {
                    return null;
                }
            }

            if (parameters == null)
            {
                return baseType;
            }

            return GetType(parameters, baseType);
        }

        [RequiresAssemblyFiles(UnknownStringMessageInRAF)]
        public override string FullyQualifiedName => ManifestModuleName;

        [RequiresUnreferencedCode("Trimming changes metadata tokens")]
        public override byte[] ResolveSignature(int metadataToken)
        {
            return InternalModule.ResolveSignature(metadataToken);
        }

        [RequiresUnreferencedCode("Trimming changes metadata tokens")]
        public override MethodBase? ResolveMethod(int metadataToken, Type[]? genericTypeArguments, Type[]? genericMethodArguments)
        {
            return InternalModule.ResolveMethod(metadataToken, genericTypeArguments, genericMethodArguments);
        }

        [RequiresUnreferencedCode("Trimming changes metadata tokens")]
        public override FieldInfo? ResolveField(int metadataToken, Type[]? genericTypeArguments, Type[]? genericMethodArguments)
        {
            return InternalModule.ResolveField(metadataToken, genericTypeArguments, genericMethodArguments);
        }

        [RequiresUnreferencedCode("Trimming changes metadata tokens")]
        public override Type ResolveType(int metadataToken, Type[]? genericTypeArguments, Type[]? genericMethodArguments)
        {
            return InternalModule.ResolveType(metadataToken, genericTypeArguments, genericMethodArguments);
        }

        [RequiresUnreferencedCode("Trimming changes metadata tokens")]
        public override MemberInfo? ResolveMember(int metadataToken, Type[]? genericTypeArguments, Type[]? genericMethodArguments)
        {
            return InternalModule.ResolveMember(metadataToken, genericTypeArguments, genericMethodArguments);
        }

        [RequiresUnreferencedCode("Trimming changes metadata tokens")]
        public override string ResolveString(int metadataToken)
        {
            return InternalModule.ResolveString(metadataToken);
        }

        public override void GetPEKind(out PortableExecutableKinds peKind, out ImageFileMachine machine)
        {
            InternalModule.GetPEKind(out peKind, out machine);
        }

        public override int MDStreamVersion => InternalModule.MDStreamVersion;

        public override Guid ModuleVersionId => InternalModule.ModuleVersionId;

        public override int MetadataToken => InternalModule.MetadataToken;

        public override bool IsResource() => InternalModule.IsResource();

        [RequiresUnreferencedCode("Fields might be removed")]
        public override FieldInfo[] GetFields(BindingFlags bindingFlags)
        {
            return InternalModule.GetFields(bindingFlags);
        }

        [RequiresUnreferencedCode("Fields might be removed")]
        public override FieldInfo? GetField(string name, BindingFlags bindingAttr)
        {
            return InternalModule.GetField(name, bindingAttr);
        }

        [RequiresUnreferencedCode("Methods might be removed")]
        public override MethodInfo[] GetMethods(BindingFlags bindingFlags)
        {
            return InternalModule.GetMethods(bindingFlags);
        }

        [RequiresUnreferencedCode("Methods might be removed")]
        protected override MethodInfo? GetMethodImpl(string name, BindingFlags bindingAttr, Binder? binder,
            CallingConventions callConvention, Type[]? types, ParameterModifier[]? modifiers)
        {
            // Cannot call InternalModule.GetMethods because it doesn't allow types to be null
            return InternalModule.GetMethodInternal(name, bindingAttr, binder, callConvention, types, modifiers);
        }

        public override string ScopeName => InternalModule.ScopeName;

        [RequiresAssemblyFiles(UnknownStringMessageInRAF)]
        public override string Name => InternalModule.Name;

        public override Assembly Assembly => _assemblyBuilder;

        #endregion

        #region Public Members

        #region Define Type

        protected override TypeBuilder DefineTypeCore(string name, TypeAttributes attr, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type? parent, Type[]? interfaces, PackingSize packingSize, int typesize)
        {
            lock (SyncRoot)
            {
                return new RuntimeTypeBuilder(name, attr, parent, interfaces, this, packingSize, typesize, null);
            }
        }

        #endregion

        #region Define Enum

        // This API can only be used to construct a top-level (not nested) enum type.
        // Nested enum types can be defined manually using ModuleBuilder.DefineType.
        protected override EnumBuilder DefineEnumCore(string name, TypeAttributes visibility, Type underlyingType)
        {
            lock (SyncRoot)
            {
                RuntimeEnumBuilder enumBuilder = new RuntimeEnumBuilder(name, underlyingType, visibility, this);

                // This enum is not generic, nested, and cannot have any element type.
                // Replace the TypeBuilder object in _typeBuilderDict with this EnumBuilder object.
                _typeBuilderDict[name] = enumBuilder;

                return enumBuilder;
            }
        }

        #endregion

        #region Define Global Method

        [RequiresUnreferencedCode("P/Invoke marshalling may dynamically access members that could be trimmed.")]
        protected override MethodBuilder DefinePInvokeMethodCore(string name, string dllName, string entryName,
            MethodAttributes attributes, CallingConventions callingConvention, Type? returnType,
            Type[]? parameterTypes, CallingConvention nativeCallConv, CharSet nativeCharSet)
        {
            lock (SyncRoot)
            {
                // Global methods must be static.
                if ((attributes & MethodAttributes.Static) == 0)
                {
                    throw new ArgumentException(SR.Argument_GlobalMembersMustBeStatic);
                }

                return _globalTypeBuilder.DefinePInvokeMethod(name, dllName, entryName, attributes, callingConvention, returnType, parameterTypes, nativeCallConv, nativeCharSet);
            }
        }

        protected override MethodBuilder DefineGlobalMethodCore(string name, MethodAttributes attributes, CallingConventions callingConvention,
            Type? returnType, Type[]? requiredReturnTypeCustomModifiers, Type[]? optionalReturnTypeCustomModifiers,
            Type[]? parameterTypes, Type[][]? requiredParameterTypeCustomModifiers, Type[][]? optionalParameterTypeCustomModifiers)
        {
            lock (SyncRoot)
            {
                return DefineGlobalMethodNoLock(name, attributes, callingConvention, returnType,
                                                requiredReturnTypeCustomModifiers, optionalReturnTypeCustomModifiers,
                                                parameterTypes, requiredParameterTypeCustomModifiers, optionalParameterTypeCustomModifiers);
            }
        }

        private MethodBuilder DefineGlobalMethodNoLock(string name, MethodAttributes attributes, CallingConventions callingConvention,
            Type? returnType, Type[]? requiredReturnTypeCustomModifiers, Type[]? optionalReturnTypeCustomModifiers,
            Type[]? parameterTypes, Type[][]? requiredParameterTypeCustomModifiers, Type[][]? optionalParameterTypeCustomModifiers)
        {
            if (_hasGlobalBeenCreated)
            {
                throw new InvalidOperationException(SR.InvalidOperation_GlobalsHaveBeenCreated);
            }

            if ((attributes & MethodAttributes.Static) == 0)
            {
                throw new ArgumentException(SR.Argument_GlobalMembersMustBeStatic);
            }

            return _globalTypeBuilder.DefineMethod(name, attributes, callingConvention,
                returnType, requiredReturnTypeCustomModifiers, optionalReturnTypeCustomModifiers,
                parameterTypes, requiredParameterTypeCustomModifiers, optionalParameterTypeCustomModifiers);
        }

        protected override void CreateGlobalFunctionsCore()
        {
            lock (SyncRoot)
            {
                if (_hasGlobalBeenCreated)
                {
                    // cannot create globals twice
                    throw new InvalidOperationException(SR.InvalidOperation_GlobalsHaveBeenCreated);
                }
                _globalTypeBuilder.CreateType();
                _hasGlobalBeenCreated = true;
            }
        }

        #endregion

        #region Define Data

        protected override FieldBuilder DefineInitializedDataCore(string name, byte[] data, FieldAttributes attributes)
        {
            lock (SyncRoot)
            {
                // This method will define an initialized Data in .sdata.
                // We will create a fake TypeDef to represent the data with size. This TypeDef
                // will be the signature for the Field.
                if (_hasGlobalBeenCreated)
                {
                    throw new InvalidOperationException(SR.InvalidOperation_GlobalsHaveBeenCreated);
                }

                return _globalTypeBuilder.DefineInitializedData(name, data, attributes);
            }
        }

        protected override FieldBuilder DefineUninitializedDataCore(string name, int size, FieldAttributes attributes)
        {
            lock (SyncRoot)
            {
                // This method will define an uninitialized Data in .sdata.
                // We will create a fake TypeDef to represent the data with size. This TypeDef
                // will be the signature for the Field.

                if (_hasGlobalBeenCreated)
                {
                    throw new InvalidOperationException(SR.InvalidOperation_GlobalsHaveBeenCreated);
                }

                return _globalTypeBuilder.DefineUninitializedData(name, size, attributes);
            }
        }

        #endregion

        #region GetToken

        // For a generic type definition, we should return the token for the generic type definition itself in two cases:
        //   1. GetTypeToken
        //   2. ldtoken (see ILGenerator)
        // For all other occasions we should return the generic type instantiated on its formal parameters.
        internal int GetTypeTokenInternal(Type type, bool getGenericDefinition = false)
        {
            lock (SyncRoot)
            {
                return GetTypeTokenWorkerNoLock(type, getGenericDefinition);
            }
        }

        public override int GetTypeMetadataToken(Type type)
        {
            return GetTypeTokenInternal(type, getGenericDefinition: true);
        }

        private int GetTypeTokenWorkerNoLock(Type type, bool getGenericDefinition)
        {
            ArgumentNullException.ThrowIfNull(type);

            // Return a token for the class relative to the Module.  Tokens
            // are used to indentify objects when the objects are used in IL
            // instructions.  Tokens are always relative to the Module.  For example,
            // the token value for System.String is likely to be different from
            // Module to Module.  Calling GetTypeToken will cause a reference to be
            // added to the Module.  This reference becomes a permanent part of the Module,
            // multiple calls to this method with the same class have no additional side-effects.
            // This function is optimized to use the TypeDef token if the Type is within the
            // same module. We should also be aware of multiple dynamic modules and multiple
            // implementations of a Type.
            if ((type.IsGenericType && (!type.IsGenericTypeDefinition || !getGenericDefinition))
                || type.IsGenericParameter
                || type.IsArray
                || type.IsPointer
                || type.IsByRef)
            {
                byte[] sig = SignatureHelper.GetTypeSigToken(this, type).InternalGetSignature(out int length);
                return GetTokenFromTypeSpec(sig, length);
            }

            Module refedModule = type.Module;

            if (refedModule.Equals(this))
            {
                // no need to do anything additional other than defining the TypeRef Token
                RuntimeTypeBuilder? typeBuilder;

                typeBuilder = type is RuntimeEnumBuilder enumBuilder ? enumBuilder.m_typeBuilder : type as RuntimeTypeBuilder;

                if (typeBuilder != null)
                {
                    // If the type is defined in this module, just return the token.
                    return typeBuilder.TypeToken;
                }
                else if (type is GenericTypeParameterBuilder paramBuilder)
                {
                    return paramBuilder.MetadataToken;
                }

                return GetTypeRefNested(type, this);
            }

            return GetTypeRefNested(type, refedModule);
        }

        public override int GetMethodMetadataToken(MethodInfo method)
        {
            lock (SyncRoot)
            {
                return GetMethodTokenNoLock(method, false);
            }
        }

        // For a method on a generic type, we should return the methoddef token on the generic type definition in two cases
        //   1. GetMethodToken
        //   2. ldtoken (see ILGenerator)
        // For all other occasions we should return the method on the generic type instantiated on the formal parameters.
        private int GetMethodTokenNoLock(MethodInfo method, bool getGenericTypeDefinition)
        {
            ArgumentNullException.ThrowIfNull(method);

            // Return a MemberRef token if MethodInfo is not defined in this module. Or
            // return the MethodDef token.
            int tr;
            int mr;

            if (method is MethodBuilder methBuilder)
            {
                int methodToken = methBuilder.MetadataToken;
                if (method.Module.Equals(this))
                {
                    return methodToken;
                }

                if (method.DeclaringType == null)
                {
                    throw new InvalidOperationException(SR.InvalidOperation_CannotImportGlobalFromDifferentModule);
                }

                // method is defined in a different module
                tr = getGenericTypeDefinition ? GetTypeMetadataToken(method.DeclaringType) : GetTypeTokenInternal(method.DeclaringType);
                mr = GetMemberRef(method.DeclaringType.Module, tr, methodToken);
            }
            else if (method is MethodOnTypeBuilderInstantiation)
            {
                return GetMemberRefToken(method, null);
            }
            else if (method is SymbolMethod symMethod)
            {
                if (symMethod.GetModule() == this)
                    return symMethod.MetadataToken;

                // form the method token
                return symMethod.GetToken(this);
            }
            else
            {
                Type? declaringType = method.DeclaringType;

                // We need to get the TypeRef tokens
                if (declaringType == null)
                {
                    throw new InvalidOperationException(SR.InvalidOperation_CannotImportGlobalFromDifferentModule);
                }

                if (declaringType.IsArray)
                {
                    // use reflection to build signature to work around the E_T_VAR problem in EEClass
                    ParameterInfo[] paramInfo = method.GetParameters();

                    Type[] tt = new Type[paramInfo.Length];

                    for (int i = 0; i < paramInfo.Length; i++)
                        tt[i] = paramInfo[i].ParameterType;

                    return GetArrayMethodToken(declaringType, method.Name, method.CallingConvention, method.ReturnType, tt);
                }
                else if (method is RuntimeMethodInfo rtMeth)
                {
                    tr = getGenericTypeDefinition ? GetTypeMetadataToken(declaringType) : GetTypeTokenInternal(declaringType);
                    mr = GetMemberRefOfMethodInfo(tr, rtMeth);
                }
                else
                {
                    // some user derived ConstructorInfo
                    // go through the slower code path, i.e. retrieve parameters and form signature helper.
                    ParameterInfo[] parameters = method.GetParameters();

                    Type[] parameterTypes = new Type[parameters.Length];
                    Type[][] requiredCustomModifiers = new Type[parameterTypes.Length][];
                    Type[][] optionalCustomModifiers = new Type[parameterTypes.Length][];

                    for (int i = 0; i < parameters.Length; i++)
                    {
                        parameterTypes[i] = parameters[i].ParameterType;
                        requiredCustomModifiers[i] = parameters[i].GetRequiredCustomModifiers();
                        optionalCustomModifiers[i] = parameters[i].GetOptionalCustomModifiers();
                    }

                    tr = getGenericTypeDefinition ? GetTypeMetadataToken(declaringType) : GetTypeTokenInternal(declaringType);

                    SignatureHelper sigHelp;

                    try
                    {
                        sigHelp = SignatureHelper.GetMethodSigHelper(
                        this, method.CallingConvention, method.ReturnType,
                        method.ReturnParameter.GetRequiredCustomModifiers(), method.ReturnParameter.GetOptionalCustomModifiers(),
                        parameterTypes, requiredCustomModifiers, optionalCustomModifiers);
                    }
                    catch (NotImplementedException)
                    {
                        // Legacy code deriving from MethodInfo may not have implemented ReturnParameter.
                        sigHelp = SignatureHelper.GetMethodSigHelper(this, method.ReturnType, parameterTypes);
                    }

                    byte[] sigBytes = sigHelp.InternalGetSignature(out int length);
                    mr = GetMemberRefFromSignature(tr, method.Name, sigBytes, length);
                }
            }

            return mr;
        }

        internal int GetMethodTokenInternal(MethodBase method, Type[]? optionalParameterTypes, bool useMethodDef)
        {
            int tk;
            MethodInfo? methodInfo = method as MethodInfo;

            if (method.IsGenericMethod)
            {
                // Constructors cannot be generic.
                Debug.Assert(methodInfo != null);

                // Given M<Bar> unbind to M<S>
                MethodInfo methodInfoUnbound = methodInfo;
                bool isGenericMethodDef = methodInfo.IsGenericMethodDefinition;

                if (!isGenericMethodDef)
                {
                    methodInfoUnbound = methodInfo.GetGenericMethodDefinition()!;
                }

                if (!Equals(methodInfoUnbound.Module)
                    || (methodInfoUnbound.DeclaringType != null && methodInfoUnbound.DeclaringType.IsGenericType))
                {
                    tk = GetMemberRefToken(methodInfoUnbound, null);
                }
                else
                {
                    tk = GetMethodMetadataToken(methodInfoUnbound);
                }

                // For Ldtoken, Ldftn, and Ldvirtftn, we should emit the method def/ref token for a generic method definition.
                if (isGenericMethodDef && useMethodDef)
                {
                    return tk;
                }

                // Create signature of method instantiation M<Bar>
                // Create MethodSepc M<Bar> with parent G?.M<S>
                byte[] sigBytes = SignatureHelper.GetMethodSpecSigHelper(
                    this, methodInfo.GetGenericArguments()).InternalGetSignature(out int sigLength);
                RuntimeModuleBuilder thisModule = this;
                tk = RuntimeTypeBuilder.DefineMethodSpec(new QCallModule(ref thisModule), tk, sigBytes, sigLength);
            }
            else
            {
                if (((method.CallingConvention & CallingConventions.VarArgs) == 0) &&
                    (method.DeclaringType == null || !method.DeclaringType.IsGenericType))
                {
                    if (methodInfo != null)
                    {
                        tk = GetMethodMetadataToken(methodInfo);
                    }
                    else
                    {
                        tk = GetMethodMetadataToken((method as ConstructorInfo)!);
                    }
                }
                else
                {
                    tk = GetMemberRefToken(method, optionalParameterTypes);
                }
            }

            return tk;
        }

        internal int GetArrayMethodToken(Type arrayClass, string methodName, CallingConventions callingConvention,
            Type? returnType, Type[]? parameterTypes)
        {
            lock (SyncRoot)
            {
                return GetArrayMethodTokenNoLock(arrayClass, methodName, callingConvention, returnType, parameterTypes);
            }
        }

        private int GetArrayMethodTokenNoLock(Type arrayClass, string methodName, CallingConventions callingConvention,
            Type? returnType, Type[]? parameterTypes)
        {
            if (!arrayClass.IsArray)
            {
                throw new ArgumentException(SR.Argument_HasToBeArrayClass);
            }

            // Return a token for the MethodInfo for a method on an Array.  This is primarily
            // used to get the LoadElementAddress method.
            SignatureHelper sigHelp = SignatureHelper.GetMethodSigHelper(
                this, callingConvention, returnType, null, null, parameterTypes, null, null);
            byte[] sigBytes = sigHelp.InternalGetSignature(out int length);
            int typeSpec = GetTypeTokenInternal(arrayClass);

            RuntimeModuleBuilder thisModule = this;
            return GetArrayMethodToken(new QCallModule(ref thisModule),
                typeSpec, methodName, sigBytes, length);
        }

        protected override MethodInfo GetArrayMethodCore(Type arrayClass, string methodName, CallingConventions callingConvention,
            Type? returnType, Type[]? parameterTypes)
        {
            // GetArrayMethod is useful when you have an array of a type whose definition has not been completed and
            // you want to access methods defined on Array. For example, you might define a type and want to define a
            // method that takes an array of the type as a parameter. In order to access the elements of the array,
            // you will need to call methods of the Array class.

            int token = GetArrayMethodToken(arrayClass, methodName, callingConvention, returnType, parameterTypes);

            return new SymbolMethod(this, token, arrayClass, methodName, callingConvention, returnType, parameterTypes);
        }

        public override int GetFieldMetadataToken(FieldInfo field)
        {
            lock (SyncRoot)
            {
                return GetFieldTokenNoLock(field);
            }
        }

        private int GetFieldTokenNoLock(FieldInfo field)
        {
            ArgumentNullException.ThrowIfNull(field);

            int tr;
            int mr;

            if (field is FieldBuilder fdBuilder)
            {
                if (field.DeclaringType != null && field.DeclaringType.IsGenericType)
                {
                    byte[] sig = SignatureHelper.GetTypeSigToken(this, field.DeclaringType).InternalGetSignature(out int length);
                    tr = GetTokenFromTypeSpec(sig, length);
                    mr = GetMemberRef(this, tr, fdBuilder.MetadataToken);
                }
                else if (fdBuilder.Module.Equals(this))
                {
                    // field is defined in the same module
                    return fdBuilder.MetadataToken;
                }
                else
                {
                    // field is defined in a different module
                    if (field.DeclaringType == null)
                    {
                        throw new InvalidOperationException(SR.InvalidOperation_CannotImportGlobalFromDifferentModule);
                    }
                    tr = GetTypeTokenInternal(field.DeclaringType);
                    mr = GetMemberRef(field.ReflectedType!.Module, tr, fdBuilder.MetadataToken);
                }
            }
            else if (field is RuntimeFieldInfo rtField)
            {
                // FieldInfo is not an dynamic field
                // We need to get the TypeRef tokens
                if (field.DeclaringType == null)
                {
                    throw new InvalidOperationException(SR.InvalidOperation_CannotImportGlobalFromDifferentModule);
                }

                if (field.DeclaringType != null && field.DeclaringType.IsGenericType)
                {
                    byte[] sig = SignatureHelper.GetTypeSigToken(this, field.DeclaringType).InternalGetSignature(out int length);
                    tr = GetTokenFromTypeSpec(sig, length);
                    mr = GetMemberRefOfFieldInfo(tr, field.DeclaringType.TypeHandle, rtField);
                }
                else
                {
                    tr = GetTypeTokenInternal(field.DeclaringType!);
                    mr = GetMemberRefOfFieldInfo(tr, field.DeclaringType!.TypeHandle, rtField);
                }
            }
            else if (field is FieldOnTypeBuilderInstantiation fOnTB)
            {
                FieldInfo fb = fOnTB.FieldInfo;
                byte[] sig = SignatureHelper.GetTypeSigToken(this, field.DeclaringType!).InternalGetSignature(out int length);
                tr = GetTokenFromTypeSpec(sig, length);
                mr = GetMemberRef(fb.ReflectedType!.Module, tr, fOnTB.MetadataToken);
            }
            else
            {
                // user defined FieldInfo
                tr = GetTypeTokenInternal(field.ReflectedType!);

                SignatureHelper sigHelp = SignatureHelper.GetFieldSigHelper(this);

                sigHelp.AddArgument(field.FieldType, field.GetRequiredCustomModifiers(), field.GetOptionalCustomModifiers());

                byte[] sigBytes = sigHelp.InternalGetSignature(out int length);
                mr = GetMemberRefFromSignature(tr, field.Name, sigBytes, length);
            }

            return mr;
        }

        public override int GetStringMetadataToken(string stringConstant)
        {
            ArgumentNullException.ThrowIfNull(stringConstant);

            // Returns a token representing a String constant.  If the string
            // value has already been defined, the existing token will be returned.
            RuntimeModuleBuilder thisModule = this;
            return GetStringConstant(new QCallModule(ref thisModule), stringConstant, stringConstant.Length);
        }

        public override int GetSignatureMetadataToken(SignatureHelper signature)
        {
            ArgumentNullException.ThrowIfNull(signature);

            // Define signature token given a signature helper. This will define a metadata
            // token for the signature described by SignatureHelper.
            // Get the signature in byte form.
            byte[] sigBytes = signature.InternalGetSignature(out int sigLength);
            RuntimeModuleBuilder thisModule = this;
            return RuntimeTypeBuilder.GetTokenFromSig(new QCallModule(ref thisModule), sigBytes, sigLength);
        }

        internal int GetSignatureToken(byte[] sigBytes, int sigLength)
        {
            ArgumentNullException.ThrowIfNull(sigBytes);

            byte[] localSigBytes = new byte[sigBytes.Length];
            Buffer.BlockCopy(sigBytes, 0, localSigBytes, 0, sigBytes.Length);

            RuntimeModuleBuilder thisModule = this;
            return RuntimeTypeBuilder.GetTokenFromSig(new QCallModule(ref thisModule), localSigBytes, sigLength);
        }

        #endregion

        #region Other

        protected override void SetCustomAttributeCore(ConstructorInfo con, ReadOnlySpan<byte> binaryAttribute)
        {
            RuntimeTypeBuilder.DefineCustomAttribute(
                this,
                1,                                          // This is hard coding the module token to 1
                GetMethodMetadataToken(con),
                binaryAttribute);
        }

        #endregion

        #endregion
    }
}
