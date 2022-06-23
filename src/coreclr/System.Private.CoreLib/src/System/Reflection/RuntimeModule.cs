// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace System.Reflection
{
    internal sealed partial class RuntimeModule : Module
    {
        internal RuntimeModule() { throw new NotSupportedException(); }

        #region FCalls
        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "RuntimeModule_GetType", StringMarshalling = StringMarshalling.Utf16)]
        private static partial void GetType(QCallModule module, string className, [MarshalAs(UnmanagedType.Bool)] bool throwOnError, [MarshalAs(UnmanagedType.Bool)] bool ignoreCase, ObjectHandleOnStack type, ObjectHandleOnStack keepAlive);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "RuntimeModule_GetScopeName")]
        private static partial void GetScopeName(QCallModule module, StringHandleOnStack retString);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "RuntimeModule_GetFullyQualifiedName")]
        private static partial void GetFullyQualifiedName(QCallModule module, StringHandleOnStack retString);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern RuntimeType[] GetTypes(RuntimeModule module);

        internal RuntimeType[] GetDefinedTypes()
        {
            return GetTypes(this);
        }
        #endregion

        #region Module overrides
        private static RuntimeTypeHandle[]? ConvertToTypeHandleArray(Type[]? genericArguments)
        {
            if (genericArguments == null)
                return null;

            int size = genericArguments.Length;
            RuntimeTypeHandle[] typeHandleArgs = new RuntimeTypeHandle[size];
            for (int i = 0; i < size; i++)
            {
                Type typeArg = genericArguments[i];
                if (typeArg == null)
                    throw new ArgumentException(SR.Argument_InvalidGenericInstArray);
                typeArg = typeArg.UnderlyingSystemType;
                if (typeArg == null)
                    throw new ArgumentException(SR.Argument_InvalidGenericInstArray);
                if (!(typeArg is RuntimeType))
                    throw new ArgumentException(SR.Argument_InvalidGenericInstArray);
                typeHandleArgs[i] = typeArg.TypeHandle;
            }
            return typeHandleArgs;
        }

        [RequiresUnreferencedCode("Trimming changes metadata tokens")]
        public override byte[] ResolveSignature(int metadataToken)
        {
            MetadataToken tk = new MetadataToken(metadataToken);

            if (!MetadataImport.IsValidToken(tk))
                throw new ArgumentOutOfRangeException(nameof(metadataToken),
                    SR.Format(SR.Argument_InvalidToken, tk, this));

            if (!tk.IsMemberRef && !tk.IsMethodDef && !tk.IsTypeSpec && !tk.IsSignature && !tk.IsFieldDef)
                throw new ArgumentException(SR.Format(SR.Argument_InvalidToken, tk, this),
                                            nameof(metadataToken));

            ConstArray signature;
            if (tk.IsMemberRef)
                signature = MetadataImport.GetMemberRefProps(metadataToken);
            else
                signature = MetadataImport.GetSignatureFromToken(metadataToken);

            byte[] sig = new byte[signature.Length];

            for (int i = 0; i < signature.Length; i++)
                sig[i] = signature[i];

            return sig;
        }

        [RequiresUnreferencedCode("Trimming changes metadata tokens")]
        public override MethodBase? ResolveMethod(int metadataToken, Type[]? genericTypeArguments, Type[]? genericMethodArguments)
        {
            try
            {
                MetadataToken tk = new MetadataToken(metadataToken);
                if (!tk.IsMethodDef && !tk.IsMethodSpec)
                {
                    if (!tk.IsMemberRef)
                        throw new ArgumentException(SR.Format(SR.Argument_ResolveMethod, tk, this),
                            nameof(metadataToken));

                    unsafe
                    {
                        ConstArray sig = MetadataImport.GetMemberRefProps(tk);

                        if (*(MdSigCallingConvention*)sig.Signature == MdSigCallingConvention.Field)
                            throw new ArgumentException(SR.Format(SR.Argument_ResolveMethod, tk, this),
                                nameof(metadataToken));
                    }
                }

                RuntimeTypeHandle[]? typeArgs = null;
                RuntimeTypeHandle[]? methodArgs = null;
                if (genericTypeArguments?.Length > 0)
                {
                    typeArgs = ConvertToTypeHandleArray(genericTypeArguments);
                }
                if (genericMethodArguments?.Length > 0)
                {
                    methodArgs = ConvertToTypeHandleArray(genericMethodArguments);
                }

                ModuleHandle moduleHandle = new ModuleHandle(this);
                IRuntimeMethodInfo methodHandle = moduleHandle.ResolveMethodHandle(tk, typeArgs, methodArgs).GetMethodInfo();

                Type declaringType = RuntimeMethodHandle.GetDeclaringType(methodHandle);

                if (declaringType.IsGenericType || declaringType.IsArray)
                {
                    MetadataToken tkDeclaringType = new MetadataToken(MetadataImport.GetParentToken(tk));

                    if (tk.IsMethodSpec)
                        tkDeclaringType = new MetadataToken(MetadataImport.GetParentToken(tkDeclaringType));

                    declaringType = ResolveType(tkDeclaringType, genericTypeArguments, genericMethodArguments);
                }

                return RuntimeType.GetMethodBase(declaringType as RuntimeType, methodHandle);
            }
            catch (BadImageFormatException e)
            {
                throw new ArgumentException(SR.Argument_BadImageFormatExceptionResolve, e);
            }
        }

        [RequiresUnreferencedCode("Trimming changes metadata tokens")]
        private FieldInfo? ResolveLiteralField(int metadataToken, Type[]? genericTypeArguments, Type[]? genericMethodArguments)
        {
            MetadataToken tk = new MetadataToken(metadataToken);

            if (!MetadataImport.IsValidToken(tk) || !tk.IsFieldDef)
                throw new ArgumentOutOfRangeException(nameof(metadataToken),
                    SR.Format(SR.Argument_InvalidToken, tk, this));

            int tkDeclaringType;
            string fieldName = MetadataImport.GetName(tk).ToString();
            tkDeclaringType = MetadataImport.GetParentToken(tk);

            Type declaringType = ResolveType(tkDeclaringType, genericTypeArguments, genericMethodArguments);

            declaringType.GetFields();

            try
            {
                return declaringType.GetField(fieldName,
                    BindingFlags.Static | BindingFlags.Instance |
                    BindingFlags.Public | BindingFlags.NonPublic |
                    BindingFlags.DeclaredOnly);
            }
            catch
            {
                throw new ArgumentException(SR.Format(SR.Argument_ResolveField, tk, this), nameof(metadataToken));
            }
        }

        [RequiresUnreferencedCode("Trimming changes metadata tokens")]
        public override FieldInfo? ResolveField(int metadataToken, Type[]? genericTypeArguments, Type[]? genericMethodArguments)
        {
            try
            {
                MetadataToken tk = new MetadataToken(metadataToken);

                if (!MetadataImport.IsValidToken(tk))
                    throw new ArgumentOutOfRangeException(nameof(metadataToken),
                        SR.Format(SR.Argument_InvalidToken, tk, this));

                RuntimeTypeHandle[]? typeArgs = null;
                RuntimeTypeHandle[]? methodArgs = null;
                if (genericTypeArguments?.Length > 0)
                {
                    typeArgs = ConvertToTypeHandleArray(genericTypeArguments);
                }
                if (genericMethodArguments?.Length > 0)
                {
                    methodArgs = ConvertToTypeHandleArray(genericMethodArguments);
                }

                ModuleHandle moduleHandle = new ModuleHandle(this);
                if (!tk.IsFieldDef)
                {
                    if (!tk.IsMemberRef)
                        throw new ArgumentException(SR.Format(SR.Argument_ResolveField, tk, this),
                            nameof(metadataToken));

                    unsafe
                    {
                        ConstArray sig = MetadataImport.GetMemberRefProps(tk);

                        if (*(MdSigCallingConvention*)sig.Signature != MdSigCallingConvention.Field)
                            throw new ArgumentException(SR.Format(SR.Argument_ResolveField, tk, this),
                                nameof(metadataToken));
                    }
                }

                IRuntimeFieldInfo fieldHandle = moduleHandle.ResolveFieldHandle(metadataToken, typeArgs, methodArgs).GetRuntimeFieldInfo();

                RuntimeType declaringType = RuntimeFieldHandle.GetApproxDeclaringType(fieldHandle.Value);

                if (declaringType.IsGenericType || declaringType.IsArray)
                {
                    int tkDeclaringType = ModuleHandle.GetMetadataImport(this).GetParentToken(metadataToken);
                    declaringType = (RuntimeType)ResolveType(tkDeclaringType, genericTypeArguments, genericMethodArguments);
                }

                return RuntimeType.GetFieldInfo(declaringType, fieldHandle);
            }
            catch (MissingFieldException)
            {
                return ResolveLiteralField(metadataToken, genericTypeArguments, genericMethodArguments);
            }
            catch (BadImageFormatException e)
            {
                throw new ArgumentException(SR.Argument_BadImageFormatExceptionResolve, e);
            }
        }

        [RequiresUnreferencedCode("Trimming changes metadata tokens")]
        public override Type ResolveType(int metadataToken, Type[]? genericTypeArguments, Type[]? genericMethodArguments)
        {
            try
            {
                MetadataToken tk = new MetadataToken(metadataToken);

                if (tk.IsGlobalTypeDefToken)
                    throw new ArgumentException(SR.Format(SR.Argument_ResolveModuleType, tk), nameof(metadataToken));

                if (!tk.IsTypeDef && !tk.IsTypeSpec && !tk.IsTypeRef)
                    throw new ArgumentException(SR.Format(SR.Argument_ResolveType, tk, this), nameof(metadataToken));

                RuntimeTypeHandle[]? typeArgs = null;
                RuntimeTypeHandle[]? methodArgs = null;
                if (genericTypeArguments?.Length > 0)
                {
                    typeArgs = ConvertToTypeHandleArray(genericTypeArguments);
                }
                if (genericMethodArguments?.Length > 0)
                {
                    methodArgs = ConvertToTypeHandleArray(genericMethodArguments);
                }

                return GetModuleHandleImpl().ResolveTypeHandle(metadataToken, typeArgs, methodArgs).GetRuntimeType();
            }
            catch (BadImageFormatException e)
            {
                throw new ArgumentException(SR.Argument_BadImageFormatExceptionResolve, e);
            }
        }

        [RequiresUnreferencedCode("Trimming changes metadata tokens")]
        public override MemberInfo? ResolveMember(int metadataToken, Type[]? genericTypeArguments, Type[]? genericMethodArguments)
        {
            MetadataToken tk = new MetadataToken(metadataToken);

            if (tk.IsProperty)
                throw new ArgumentException(SR.InvalidOperation_PropertyInfoNotAvailable);

            if (tk.IsEvent)
                throw new ArgumentException(SR.InvalidOperation_EventInfoNotAvailable);

            if (tk.IsMethodSpec || tk.IsMethodDef)
                return ResolveMethod(metadataToken, genericTypeArguments, genericMethodArguments);

            if (tk.IsFieldDef)
                return ResolveField(metadataToken, genericTypeArguments, genericMethodArguments);

            if (tk.IsTypeRef || tk.IsTypeDef || tk.IsTypeSpec)
                return ResolveType(metadataToken, genericTypeArguments, genericMethodArguments);

            if (tk.IsMemberRef)
            {
                if (!MetadataImport.IsValidToken(tk))
                    throw new ArgumentOutOfRangeException(nameof(metadataToken),
                        SR.Format(SR.Argument_InvalidToken, tk, this));

                ConstArray sig = MetadataImport.GetMemberRefProps(tk);

                unsafe
                {
                    if (*(MdSigCallingConvention*)sig.Signature == MdSigCallingConvention.Field)
                    {
                        return ResolveField(tk, genericTypeArguments, genericMethodArguments);
                    }
                    else
                    {
                        return ResolveMethod(tk, genericTypeArguments, genericMethodArguments);
                    }
                }
            }

            throw new ArgumentException(SR.Format(SR.Argument_ResolveMember, tk, this),
                nameof(metadataToken));
        }

        [RequiresUnreferencedCode("Trimming changes metadata tokens")]
        public override string ResolveString(int metadataToken)
        {
            MetadataToken tk = new MetadataToken(metadataToken);
            if (!tk.IsString)
                throw new ArgumentException(
                    SR.Format(SR.Argument_ResolveString, metadataToken, this));

            if (!MetadataImport.IsValidToken(tk))
                throw new ArgumentOutOfRangeException(nameof(metadataToken),
                    SR.Format(SR.Argument_InvalidToken, tk, this));

            string? str = MetadataImport.GetUserString(metadataToken);

            if (str == null)
                throw new ArgumentException(
                    SR.Format(SR.Argument_ResolveString, metadataToken, this));

            return str;
        }

        public override void GetPEKind(out PortableExecutableKinds peKind, out ImageFileMachine machine)
        {
            ModuleHandle.GetPEKind(this, out peKind, out machine);
        }

        public override int MDStreamVersion => ModuleHandle.GetMDStreamVersion(this);
        #endregion

        #region Data Members
#pragma warning disable CA1823, 169
        // If you add any data members, you need to update the native declaration ReflectModuleBaseObject.
        private RuntimeType m_runtimeType;
        private RuntimeAssembly m_runtimeAssembly;
        private IntPtr m_pRefClass;
        private IntPtr m_pData;
        private IntPtr m_pGlobals;
        private IntPtr m_pFields;
#pragma warning restore CA1823, 169
        #endregion

        #region Protected Virtuals
        [RequiresUnreferencedCode("Methods might be removed because Module methods can't currently be annotated for dynamic access.")]
        protected override MethodInfo? GetMethodImpl(string name, BindingFlags bindingAttr, Binder? binder,
            CallingConventions callConvention, Type[]? types, ParameterModifier[]? modifiers)
        {
            return GetMethodInternal(name, bindingAttr, binder, callConvention, types, modifiers);
        }

        [RequiresUnreferencedCode("Methods might be removed because Module methods can't currently be annotated for dynamic access.")]
        internal MethodInfo? GetMethodInternal(string name, BindingFlags bindingAttr, Binder? binder,
            CallingConventions callConvention, Type[]? types, ParameterModifier[]? modifiers)
        {
            if (RuntimeType == null)
                return null;

            if (types == null)
            {
                return RuntimeType.GetMethod(name, bindingAttr);
            }
            else
            {
                return RuntimeType.GetMethod(name, bindingAttr, binder, callConvention, types, modifiers);
            }
        }
        #endregion

        #region Internal Members
        internal RuntimeType RuntimeType => m_runtimeType ??= ModuleHandle.GetModuleType(this);

        internal MetadataImport MetadataImport => ModuleHandle.GetMetadataImport(this);
        #endregion

        #region ICustomAttributeProvider Members
        public override object[] GetCustomAttributes(bool inherit)
        {
            return CustomAttribute.GetCustomAttributes(this, (typeof(object) as RuntimeType)!);
        }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            ArgumentNullException.ThrowIfNull(attributeType);

            if (attributeType.UnderlyingSystemType is not RuntimeType attributeRuntimeType)
                throw new ArgumentException(SR.Arg_MustBeType, nameof(attributeType));

            return CustomAttribute.GetCustomAttributes(this, attributeRuntimeType);
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            ArgumentNullException.ThrowIfNull(attributeType);

            if (attributeType.UnderlyingSystemType is not RuntimeType attributeRuntimeType)
                throw new ArgumentException(SR.Arg_MustBeType, nameof(attributeType));

            return CustomAttribute.IsDefined(this, attributeRuntimeType);
        }

        public override IList<CustomAttributeData> GetCustomAttributesData()
        {
            return RuntimeCustomAttributeData.GetCustomAttributesInternal(this);
        }
        #endregion

        #region Public Virtuals
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            throw new PlatformNotSupportedException();
        }

        [RequiresUnreferencedCode("Types might be removed")]
        public override Type? GetType(
            string className, // throw on null strings regardless of the value of "throwOnError"
            bool throwOnError, bool ignoreCase)
        {
            ArgumentNullException.ThrowIfNull(className);

            RuntimeType? retType = null;
            object? keepAlive = null;
            RuntimeModule thisAsLocal = this;
            GetType(new QCallModule(ref thisAsLocal), className, throwOnError, ignoreCase, ObjectHandleOnStack.Create(ref retType), ObjectHandleOnStack.Create(ref keepAlive));
            GC.KeepAlive(keepAlive);
            return retType;
        }

        [RequiresAssemblyFiles(UnknownStringMessageInRAF)]
        internal string GetFullyQualifiedName()
        {
            string? fullyQualifiedName = null;
            RuntimeModule thisAsLocal = this;
            GetFullyQualifiedName(new QCallModule(ref thisAsLocal), new StringHandleOnStack(ref fullyQualifiedName));
            return fullyQualifiedName!;
        }

        [RequiresAssemblyFiles(UnknownStringMessageInRAF)]
        public override string FullyQualifiedName => GetFullyQualifiedName();

        [RequiresUnreferencedCode("Types might be removed")]
        public override Type[] GetTypes()
        {
            return GetTypes(this);
        }

        #endregion

        #region Public Members

        public override Guid ModuleVersionId
        {
            get
            {
                MetadataImport.GetScopeProps(out Guid mvid);
                return mvid;
            }
        }

        public override int MetadataToken => ModuleHandle.GetToken(this);

        public override bool IsResource()
        {
            // CoreClr does not support resource-only modules.
            return false;
        }

        [RequiresUnreferencedCode("Fields might be removed")]
        public override FieldInfo[] GetFields(BindingFlags bindingFlags)
        {
            if (RuntimeType == null)
                return Array.Empty<FieldInfo>();

            return RuntimeType.GetFields(bindingFlags);
        }

        [RequiresUnreferencedCode("Fields might be removed")]
        public override FieldInfo? GetField(string name, BindingFlags bindingAttr)
        {
            ArgumentNullException.ThrowIfNull(name);

            return RuntimeType?.GetField(name, bindingAttr);
        }

        [RequiresUnreferencedCode("Methods might be removed")]
        public override MethodInfo[] GetMethods(BindingFlags bindingFlags)
        {
            if (RuntimeType == null)
                return Array.Empty<MethodInfo>();

            return RuntimeType.GetMethods(bindingFlags);
        }

        public override string ScopeName
        {
            get
            {
                string? scopeName = null;
                RuntimeModule thisAsLocal = this;
                GetScopeName(new QCallModule(ref thisAsLocal), new StringHandleOnStack(ref scopeName));
                return scopeName!;
            }
        }

        [RequiresAssemblyFiles(UnknownStringMessageInRAF)]
        public override string Name
        {
            get
            {
                string s = GetFullyQualifiedName();

                int i = s.LastIndexOf(System.IO.Path.DirectorySeparatorChar);

                if (i < 0)
                    return s;

                return s.Substring(i + 1);
            }
        }

        public override Assembly Assembly => GetRuntimeAssembly();

        internal RuntimeAssembly GetRuntimeAssembly()
        {
            return m_runtimeAssembly;
        }

        protected override ModuleHandle GetModuleHandleImpl()
        {
            return new ModuleHandle(this);
        }

        internal IntPtr GetUnderlyingNativeHandle()
        {
            return m_pData;
        }
        #endregion
    }
}
