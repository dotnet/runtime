// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace System.Reflection
{
    internal class RuntimeModule : Module
    {
        internal RuntimeModule() { throw new NotSupportedException(); }

        #region FCalls
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern void GetType(QCallModule module, string className, bool throwOnError, bool ignoreCase, ObjectHandleOnStack type, ObjectHandleOnStack keepAlive);

        [DllImport(JitHelpers.QCall)]
        private static extern bool nIsTransientInternal(QCallModule module);

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern void GetScopeName(QCallModule module, StringHandleOnStack retString);

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern void GetFullyQualifiedName(QCallModule module, StringHandleOnStack retString);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern RuntimeType[] GetTypes(RuntimeModule module);

        internal RuntimeType[] GetDefinedTypes()
        {
            return GetTypes(GetNativeHandle());
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern bool IsResource(RuntimeModule module);
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
                typeHandleArgs[i] = typeArg.GetTypeHandleInternal();
            }
            return typeHandleArgs;
        }

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

        public override MethodBase? ResolveMethod(int metadataToken, Type[]? genericTypeArguments, Type[]? genericMethodArguments)
        {
            MetadataToken tk = new MetadataToken(metadataToken);

            if (!MetadataImport.IsValidToken(tk))
                throw new ArgumentOutOfRangeException(nameof(metadataToken),
                    SR.Format(SR.Argument_InvalidToken, tk, this));

            RuntimeTypeHandle[]? typeArgs = ConvertToTypeHandleArray(genericTypeArguments);
            RuntimeTypeHandle[]? methodArgs = ConvertToTypeHandleArray(genericMethodArguments);

            try
            {
                if (!tk.IsMethodDef && !tk.IsMethodSpec)
                {
                    if (!tk.IsMemberRef)
                        throw new ArgumentException(SR.Format(SR.Argument_ResolveMethod, tk, this),
                            nameof(metadataToken));

                    unsafe
                    {
                        ConstArray sig = MetadataImport.GetMemberRefProps(tk);

                        if (*(MdSigCallingConvention*)sig.Signature.ToPointer() == MdSigCallingConvention.Field)
                            throw new ArgumentException(SR.Format(SR.Argument_ResolveMethod, tk, this),
                                nameof(metadataToken));
                    }
                }

                IRuntimeMethodInfo methodHandle = ModuleHandle.ResolveMethodHandleInternal(GetNativeHandle(), tk, typeArgs, methodArgs);
                Type declaringType = RuntimeMethodHandle.GetDeclaringType(methodHandle);

                if (declaringType.IsGenericType || declaringType.IsArray)
                {
                    MetadataToken tkDeclaringType = new MetadataToken(MetadataImport.GetParentToken(tk));

                    if (tk.IsMethodSpec)
                        tkDeclaringType = new MetadataToken(MetadataImport.GetParentToken(tkDeclaringType));

                    declaringType = ResolveType(tkDeclaringType, genericTypeArguments, genericMethodArguments);
                }

                return System.RuntimeType.GetMethodBase(declaringType as RuntimeType, methodHandle);
            }
            catch (BadImageFormatException e)
            {
                throw new ArgumentException(SR.Argument_BadImageFormatExceptionResolve, e);
            }
        }

        private FieldInfo? ResolveLiteralField(int metadataToken, Type[]? genericTypeArguments, Type[]? genericMethodArguments)
        {
            MetadataToken tk = new MetadataToken(metadataToken);

            if (!MetadataImport.IsValidToken(tk) || !tk.IsFieldDef)
                throw new ArgumentOutOfRangeException(nameof(metadataToken),
                    SR.Format(SR.Argument_InvalidToken, tk, this));

            int tkDeclaringType;
            string fieldName;

            fieldName = MetadataImport.GetName(tk).ToString();
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

        public override FieldInfo? ResolveField(int metadataToken, Type[]? genericTypeArguments, Type[]? genericMethodArguments)
        {
            MetadataToken tk = new MetadataToken(metadataToken);

            if (!MetadataImport.IsValidToken(tk))
                throw new ArgumentOutOfRangeException(nameof(metadataToken),
                    SR.Format(SR.Argument_InvalidToken, tk, this));

            RuntimeTypeHandle[]? typeArgs = ConvertToTypeHandleArray(genericTypeArguments);
            RuntimeTypeHandle[]? methodArgs = ConvertToTypeHandleArray(genericMethodArguments);

            try
            {
                IRuntimeFieldInfo fieldHandle;

                if (!tk.IsFieldDef)
                {
                    if (!tk.IsMemberRef)
                        throw new ArgumentException(SR.Format(SR.Argument_ResolveField, tk, this),
                            nameof(metadataToken));

                    unsafe
                    {
                        ConstArray sig = MetadataImport.GetMemberRefProps(tk);

                        if (*(MdSigCallingConvention*)sig.Signature.ToPointer() != MdSigCallingConvention.Field)
                            throw new ArgumentException(SR.Format(SR.Argument_ResolveField, tk, this),
                                nameof(metadataToken));
                    }

                    fieldHandle = ModuleHandle.ResolveFieldHandleInternal(GetNativeHandle(), tk, typeArgs, methodArgs);
                }

                fieldHandle = ModuleHandle.ResolveFieldHandleInternal(GetNativeHandle(), metadataToken, typeArgs, methodArgs);
                RuntimeType declaringType = RuntimeFieldHandle.GetApproxDeclaringType(fieldHandle.Value);

                if (declaringType.IsGenericType || declaringType.IsArray)
                {
                    int tkDeclaringType = ModuleHandle.GetMetadataImport(GetNativeHandle()).GetParentToken(metadataToken);
                    declaringType = (RuntimeType)ResolveType(tkDeclaringType, genericTypeArguments, genericMethodArguments);
                }

                return System.RuntimeType.GetFieldInfo(declaringType, fieldHandle);
            }
            catch (MissingFieldException)
            {
                return ResolveLiteralField(tk, genericTypeArguments, genericMethodArguments);
            }
            catch (BadImageFormatException e)
            {
                throw new ArgumentException(SR.Argument_BadImageFormatExceptionResolve, e);
            }
        }

        public override Type ResolveType(int metadataToken, Type[]? genericTypeArguments, Type[]? genericMethodArguments)
        {
            MetadataToken tk = new MetadataToken(metadataToken);

            if (tk.IsGlobalTypeDefToken)
                throw new ArgumentException(SR.Format(SR.Argument_ResolveModuleType, tk), nameof(metadataToken));

            if (!MetadataImport.IsValidToken(tk))
                throw new ArgumentOutOfRangeException(nameof(metadataToken),
                    SR.Format(SR.Argument_InvalidToken, tk, this));

            if (!tk.IsTypeDef && !tk.IsTypeSpec && !tk.IsTypeRef)
                throw new ArgumentException(SR.Format(SR.Argument_ResolveType, tk, this), nameof(metadataToken));

            RuntimeTypeHandle[]? typeArgs = ConvertToTypeHandleArray(genericTypeArguments);
            RuntimeTypeHandle[]? methodArgs = ConvertToTypeHandleArray(genericMethodArguments);

            try
            {
                Type t = GetModuleHandleImpl().ResolveTypeHandle(metadataToken, typeArgs, methodArgs).GetRuntimeType();

                if (t == null)
                    throw new ArgumentException(SR.Format(SR.Argument_ResolveType, tk, this), nameof(metadataToken));

                return t;
            }
            catch (BadImageFormatException e)
            {
                throw new ArgumentException(SR.Argument_BadImageFormatExceptionResolve, e);
            }
        }

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
                    if (*(MdSigCallingConvention*)sig.Signature.ToPointer() == MdSigCallingConvention.Field)
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
            ModuleHandle.GetPEKind(GetNativeHandle(), out peKind, out machine);
        }

        public override int MDStreamVersion
        {
            get
            {
                return ModuleHandle.GetMDStreamVersion(GetNativeHandle());
            }
        }
        #endregion

        #region Data Members
#pragma warning disable 169
        // If you add any data members, you need to update the native declaration ReflectModuleBaseObject.
        private RuntimeType m_runtimeType;
        private RuntimeAssembly m_runtimeAssembly;
        private IntPtr m_pRefClass;
        private IntPtr m_pData;
        private IntPtr m_pGlobals;
        private IntPtr m_pFields;
#pragma warning restore 169
        #endregion

        #region Protected Virtuals
        protected override MethodInfo? GetMethodImpl(string name, BindingFlags bindingAttr, Binder? binder,
            CallingConventions callConvention, Type[]? types, ParameterModifier[]? modifiers)
        {
            return GetMethodInternal(name, bindingAttr, binder, callConvention, types, modifiers);
        }

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
        internal RuntimeType RuntimeType
        {
            get
            {
                if (m_runtimeType == null)
                    m_runtimeType = ModuleHandle.GetModuleType(this);

                return m_runtimeType;
            }
        }

        internal bool IsTransientInternal()
        {
            RuntimeModule thisAsLocal = this;
            return RuntimeModule.nIsTransientInternal(JitHelpers.GetQCallModuleOnStack(ref thisAsLocal));
        }

        internal MetadataImport MetadataImport
        {
            get
            {
                unsafe
                {
                    return ModuleHandle.GetMetadataImport(this);
                }
            }
        }
        #endregion

        #region ICustomAttributeProvider Members
        public override object[] GetCustomAttributes(bool inherit)
        {
            return CustomAttribute.GetCustomAttributes(this, (typeof(object) as RuntimeType)!);
        }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            if (attributeType == null)
                throw new ArgumentNullException(nameof(attributeType));

            RuntimeType? attributeRuntimeType = attributeType.UnderlyingSystemType as RuntimeType;

            if (attributeRuntimeType == null)
                throw new ArgumentException(SR.Arg_MustBeType, nameof(attributeType));

            return CustomAttribute.GetCustomAttributes(this, attributeRuntimeType);
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            if (attributeType == null)
                throw new ArgumentNullException(nameof(attributeType));

            RuntimeType? attributeRuntimeType = attributeType.UnderlyingSystemType as RuntimeType;

            if (attributeRuntimeType == null)
                throw new ArgumentException(SR.Arg_MustBeType, nameof(attributeType));

            return CustomAttribute.IsDefined(this, attributeRuntimeType);
        }

        public override IList<CustomAttributeData> GetCustomAttributesData()
        {
            return CustomAttributeData.GetCustomAttributesInternal(this);
        }
        #endregion

        #region Public Virtuals
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            throw new PlatformNotSupportedException();
        }

        public override Type? GetType(string className, bool throwOnError, bool ignoreCase)
        {
            // throw on null strings regardless of the value of "throwOnError"
            if (className == null)
                throw new ArgumentNullException(nameof(className));

            RuntimeType? retType = null;
            object? keepAlive = null;
            RuntimeModule thisAsLocal = this;
            GetType(JitHelpers.GetQCallModuleOnStack(ref thisAsLocal), className, throwOnError, ignoreCase, JitHelpers.GetObjectHandleOnStack(ref retType), JitHelpers.GetObjectHandleOnStack(ref keepAlive));
            GC.KeepAlive(keepAlive);
            return retType;
        }

        internal string GetFullyQualifiedName()
        {
            string? fullyQualifiedName = null;
            RuntimeModule thisAsLocal = this;
            GetFullyQualifiedName(JitHelpers.GetQCallModuleOnStack(ref thisAsLocal), JitHelpers.GetStringHandleOnStack(ref fullyQualifiedName));
            return fullyQualifiedName!;
        }

        public override string FullyQualifiedName
        {
            get
            {
                return GetFullyQualifiedName();
            }
        }

        public override Type[] GetTypes()
        {
            return GetTypes(GetNativeHandle());
        }

        #endregion

        #region Public Members

        public override Guid ModuleVersionId
        {
            get
            {
                unsafe
                {
                    Guid mvid;
                    MetadataImport.GetScopeProps(out mvid);
                    return mvid;
                }
            }
        }

        public override int MetadataToken
        {
            get
            {
                return ModuleHandle.GetToken(GetNativeHandle());
            }
        }

        public override bool IsResource()
        {
            return IsResource(GetNativeHandle());
        }

        public override FieldInfo[] GetFields(BindingFlags bindingFlags)
        {
            if (RuntimeType == null)
                return new FieldInfo[0];

            return RuntimeType.GetFields(bindingFlags);
        }

        public override FieldInfo? GetField(string name, BindingFlags bindingAttr)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            if (RuntimeType == null)
                return null;

            return RuntimeType.GetField(name, bindingAttr);
        }

        public override MethodInfo[] GetMethods(BindingFlags bindingFlags)
        {
            if (RuntimeType == null)
                return new MethodInfo[0];

            return RuntimeType.GetMethods(bindingFlags);
        }

        public override string ScopeName
        {
            get
            {
                string? scopeName = null;
                RuntimeModule thisAsLocal = this;
                GetScopeName(JitHelpers.GetQCallModuleOnStack(ref thisAsLocal),JitHelpers.GetStringHandleOnStack(ref scopeName));
                return scopeName!;
            }
        }

        public override string Name
        {
            get
            {
                string s = GetFullyQualifiedName();

#if !FEATURE_PAL
                int i = s.LastIndexOf('\\');
#else
                int i = s.LastIndexOf(System.IO.Path.DirectorySeparatorChar);
#endif
                if (i == -1)
                    return s;

                return s.Substring(i + 1);
            }
        }

        public override Assembly Assembly
        {
            get
            {
                return GetRuntimeAssembly();
            }
        }

        internal RuntimeAssembly GetRuntimeAssembly()
        {
            return m_runtimeAssembly;
        }

        protected override ModuleHandle GetModuleHandleImpl()
        {
            return new ModuleHandle(this);
        }

        internal RuntimeModule GetNativeHandle()
        {
            return this;
        }

        internal IntPtr GetUnderlyingNativeHandle()
        {
            return m_pData;
        }
        #endregion
    }
}
