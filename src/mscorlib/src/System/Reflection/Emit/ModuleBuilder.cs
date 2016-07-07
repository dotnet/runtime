// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// 

namespace System.Reflection.Emit 
{
    using System.Runtime.InteropServices;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.SymbolStore;
    using System.Globalization;
    using System.Reflection;
    using System.Diagnostics;
    using System.IO;
    using System.Resources;
    using System.Security;
    using System.Security.Permissions;
    using System.Runtime.Serialization;
    using System.Text;
    using System.Threading;
    using System.Runtime.Versioning;
    using System.Runtime.CompilerServices;
    using System.Diagnostics.Contracts;

    internal sealed class InternalModuleBuilder : RuntimeModule
    {
        #region Private Data Members
        // WARNING!! WARNING!!
        // InternalModuleBuilder should not contain any data members as its reflectbase is the same as Module.
        #endregion

        private InternalModuleBuilder() { }

        #region object overrides
        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            if (obj is InternalModuleBuilder)
                return ((object)this == obj);

            return obj.Equals(this);
        }
        // Need a dummy GetHashCode to pair with Equals
        public override int GetHashCode() { return base.GetHashCode(); }
        #endregion
    }

    // deliberately not [serializable]
    [HostProtection(MayLeakOnAbort = true)]
    [ClassInterface(ClassInterfaceType.None)]
    [ComDefaultInterface(typeof(_ModuleBuilder))]
    [System.Runtime.InteropServices.ComVisible(true)]
    public class ModuleBuilder : Module, _ModuleBuilder
    {
        #region FCalls

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern IntPtr nCreateISymWriterForDynamicModule(Module module, String filename);

        #endregion

        #region Internal Static Members
        static internal String UnmangleTypeName(String typeName)
        {
            // Gets the original type name, without '+' name mangling.

            int i = typeName.Length - 1;
            while (true)
            {
                i = typeName.LastIndexOf('+', i);
                if (i == -1)
                    break;

                bool evenSlashes = true;
                int iSlash = i;
                while (typeName[--iSlash] == '\\')
                    evenSlashes = !evenSlashes;

                // Even number of slashes means this '+' is a name separator
                if (evenSlashes)
                    break;

                i = iSlash;
            }

            return typeName.Substring(i + 1);
        }

        #endregion

        #region Intenral Data Members
        // m_TypeBuilder contains both TypeBuilder and EnumBuilder objects
        private Dictionary<string, Type> m_TypeBuilderDict;
        private ISymbolWriter m_iSymWriter;
        internal ModuleBuilderData m_moduleData;
#if !FEATURE_CORECLR
        private MethodToken m_EntryPoint;
#endif //!FEATURE_CORECLR
        internal InternalModuleBuilder m_internalModuleBuilder;
        // This is the "external" AssemblyBuilder
        // only the "external" ModuleBuilder has this set
        private AssemblyBuilder m_assemblyBuilder;
        internal AssemblyBuilder ContainingAssemblyBuilder { get { return m_assemblyBuilder; } }
        #endregion

        #region Constructor
        internal ModuleBuilder(AssemblyBuilder assemblyBuilder, InternalModuleBuilder internalModuleBuilder)
        {
            m_internalModuleBuilder = internalModuleBuilder;
            m_assemblyBuilder = assemblyBuilder;
        }
        #endregion

        #region Private Members
        internal void AddType(string name, Type type)
        {
            m_TypeBuilderDict.Add(name, type);
        }

        internal void CheckTypeNameConflict(String strTypeName, Type enclosingType)
        {
            Type foundType = null;
            if (m_TypeBuilderDict.TryGetValue(strTypeName, out foundType) &&
                object.ReferenceEquals(foundType.DeclaringType, enclosingType))
            {
                // Cannot have two types with the same name
                throw new ArgumentException(Environment.GetResourceString("Argument_DuplicateTypeName"));
            }
        }

        private Type GetType(String strFormat, Type baseType)
        {
            // This function takes a string to describe the compound type, such as "[,][]", and a baseType.

            if (strFormat == null || strFormat.Equals(String.Empty))
            {
                return baseType;
            }

            // convert the format string to byte array and then call FormCompoundType
            return SymbolType.FormCompoundType(strFormat, baseType, 0);

        }
        
        
        internal void CheckContext(params Type[][] typess)
        {
            ContainingAssemblyBuilder.CheckContext(typess);
        }
        internal void CheckContext(params Type[] types)
        {
            ContainingAssemblyBuilder.CheckContext(types);
        }


        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private extern static int GetTypeRef(RuntimeModule module, String strFullName, RuntimeModule refedModule, String strRefedModuleFileName, int tkResolution);

        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private extern static int GetMemberRef(RuntimeModule module, RuntimeModule refedModule, int tr, int defToken);

        [System.Security.SecurityCritical]  // auto-generated
        private int GetMemberRef(Module refedModule, int tr, int defToken)
        {
            return GetMemberRef(GetNativeHandle(), GetRuntimeModuleFromModule(refedModule).GetNativeHandle(), tr, defToken);
        }

        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private extern static int GetMemberRefFromSignature(RuntimeModule module, int tr, String methodName, byte[] signature, int length);

        [System.Security.SecurityCritical]  // auto-generated
        private int GetMemberRefFromSignature(int tr, String methodName, byte[] signature, int length)
        {
            return GetMemberRefFromSignature(GetNativeHandle(), tr, methodName, signature, length);
        }

        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private extern static int GetMemberRefOfMethodInfo(RuntimeModule module, int tr, IRuntimeMethodInfo method);

        [System.Security.SecurityCritical]  // auto-generated
        private int GetMemberRefOfMethodInfo(int tr, RuntimeMethodInfo method)
        {
            Contract.Assert(method != null);

#if FEATURE_APPX
            if (ContainingAssemblyBuilder.ProfileAPICheck)
            {
                if ((method.InvocationFlags & INVOCATION_FLAGS.INVOCATION_FLAGS_NON_W8P_FX_API) != 0)
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_APIInvalidForCurrentContext", method.FullName));
            }
#endif

            return GetMemberRefOfMethodInfo(GetNativeHandle(), tr, method);
        }

        [System.Security.SecurityCritical]  // auto-generated
        private int GetMemberRefOfMethodInfo(int tr, RuntimeConstructorInfo method)
        {
            Contract.Assert(method != null);

#if FEATURE_APPX
            if (ContainingAssemblyBuilder.ProfileAPICheck)
            {
                if ((method.InvocationFlags & INVOCATION_FLAGS.INVOCATION_FLAGS_NON_W8P_FX_API) != 0)
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_APIInvalidForCurrentContext", method.FullName));
            }
#endif

            return GetMemberRefOfMethodInfo(GetNativeHandle(), tr, method);
        }

        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private extern static int GetMemberRefOfFieldInfo(RuntimeModule module, int tkType, RuntimeTypeHandle declaringType, int tkField);

        [System.Security.SecurityCritical]  // auto-generated
        private int GetMemberRefOfFieldInfo(int tkType, RuntimeTypeHandle declaringType, RuntimeFieldInfo runtimeField)
        {
            Contract.Assert(runtimeField != null);

#if FEATURE_APPX
            if (ContainingAssemblyBuilder.ProfileAPICheck)
            {
                RtFieldInfo rtField = runtimeField as RtFieldInfo;
                if (rtField != null && (rtField.InvocationFlags & INVOCATION_FLAGS.INVOCATION_FLAGS_NON_W8P_FX_API) != 0)
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_APIInvalidForCurrentContext", rtField.FullName));
            }
#endif

            return GetMemberRefOfFieldInfo(GetNativeHandle(), tkType, declaringType, runtimeField.MetadataToken);
        }

        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private extern static int GetTokenFromTypeSpec(RuntimeModule pModule, byte[] signature, int length);

        [System.Security.SecurityCritical]  // auto-generated
        private int GetTokenFromTypeSpec(byte[] signature, int length)
        {
            return GetTokenFromTypeSpec(GetNativeHandle(), signature, length);
        }

        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private extern static int GetArrayMethodToken(RuntimeModule module, int tkTypeSpec, String methodName, byte[] signature, int sigLength);

        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private extern static int GetStringConstant(RuntimeModule module, String str, int length);

        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private extern static void PreSavePEFile(RuntimeModule module, int portableExecutableKind, int imageFileMachine);

        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private extern static void SavePEFile(RuntimeModule module, String fileName, int entryPoint, int isExe, bool isManifestFile);

        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private extern static void AddResource(
            RuntimeModule module, String strName, 
            byte[] resBytes, int resByteCount, int tkFile, int attribute,
            int portableExecutableKind, int imageFileMachine);

        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private extern static void SetModuleName(RuntimeModule module, String strModuleName);

        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        internal extern static void SetFieldRVAContent(RuntimeModule module, int fdToken, byte[] data, int length);

        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private extern static void DefineNativeResourceFile(RuntimeModule module, 
                                                            String strFilename, 
                                                            int portableExecutableKind, 
                                                            int ImageFileMachine);

        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private extern static void DefineNativeResourceBytes(RuntimeModule module,
                                                             byte[] pbResource, int cbResource, 
                                                             int portableExecutableKind, 
                                                             int imageFileMachine);

        [System.Security.SecurityCritical]  // auto-generated
        internal void DefineNativeResource(PortableExecutableKinds portableExecutableKind, ImageFileMachine imageFileMachine)
        {
            string strResourceFileName = m_moduleData.m_strResourceFileName;
            byte[] resourceBytes = m_moduleData.m_resourceBytes;

            if (strResourceFileName != null)
            {
                DefineNativeResourceFile(GetNativeHandle(),
                    strResourceFileName,
                    (int)portableExecutableKind, (int)imageFileMachine);
            }
            else
            if (resourceBytes != null)
            {
                DefineNativeResourceBytes(GetNativeHandle(),
                    resourceBytes, resourceBytes.Length,
                    (int)portableExecutableKind, (int)imageFileMachine);
            }
        }

        #endregion

        #region Internal Members
        internal virtual Type FindTypeBuilderWithName(String strTypeName, bool ignoreCase)
        {
            if (ignoreCase)
            {
                foreach (string name in m_TypeBuilderDict.Keys)
                {
                    if (String.Compare(name, strTypeName, (StringComparison.OrdinalIgnoreCase)) == 0)
                        return m_TypeBuilderDict[name];
                }
            }
            else
            {
                Type foundType;
                if (m_TypeBuilderDict.TryGetValue(strTypeName, out foundType))
                    return foundType;
            }

            return null;
        }
        
#if !FEATURE_CORECLR
        internal void SetEntryPoint(MethodToken entryPoint)
        {           
            // Sets the entry point of the module to be a given method.  If no entry point
            // is specified, calling EmitPEFile will generate a dll.
            // AssemblyBuilder.SetEntryPoint has already demanded required permission
            m_EntryPoint = entryPoint;
        }
#endif //!FEATURE_CORECLR


#if !FEATURE_CORECLR
        // This is a helper called by AssemblyBuilder save to presave information for the persistable modules.
        // no need to lock here because we have already taken the lock in AssemblyBuilder.Save
        [System.Security.SecurityCritical]  // auto-generated
        internal void PreSave(String fileName,
            PortableExecutableKinds portableExecutableKind, ImageFileMachine imageFileMachine)
        {
            if (m_moduleData.m_isSaved == true)
            {
                // can only save once
                throw new InvalidOperationException(String.Format(CultureInfo.InvariantCulture,
                    Environment.GetResourceString("InvalidOperation_ModuleHasBeenSaved"),
                    m_moduleData.m_strModuleName));
            }
        
            if (m_moduleData.m_fGlobalBeenCreated == false && m_moduleData.m_fHasGlobal == true)
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_GlobalFunctionNotBaked")); 

            TypeBuilder typeBuilder;
            foreach (Type item in m_TypeBuilderDict.Values)
            {
                if (item is TypeBuilder)
                {
                    typeBuilder = (TypeBuilder)item;
                }
                else
                {
                    EnumBuilder enumBuilder = (EnumBuilder)item;
                    typeBuilder = enumBuilder.m_typeBuilder;
                }

                if (!typeBuilder.IsCreated())
                {
                    // cannot save to PE file without creating all of the types first 
                    throw new NotSupportedException(String.Format(CultureInfo.InvariantCulture,
                        Environment.GetResourceString("NotSupported_NotAllTypesAreBaked"), 
                        typeBuilder.FullName)); 
                }
            }

            PreSavePEFile(GetNativeHandle(), (int)portableExecutableKind, (int)imageFileMachine);
        }

        // no need to lock here because we have already taken the lock in AssemblyBuilder.Save
        [System.Security.SecurityCritical]  // auto-generated
        internal void Save(String fileName, bool isAssemblyFile, PortableExecutableKinds portableExecutableKind, 
            ImageFileMachine imageFileMachine)
        {
            // This is a helper called by AssemblyBuilder save to save information for the persistable modules.
            if (m_moduleData.m_embeddedRes != null)
            {
                // There are embedded resources for this module
                ResWriterData   resWriter;

                // Add each resource content into the to be saved PE file
                for (resWriter = m_moduleData.m_embeddedRes; resWriter != null; resWriter = resWriter.m_nextResWriter)
                {
                    if (resWriter.m_resWriter != null)
                        resWriter.m_resWriter.Generate();                    
                    
                    byte[] resBytes = new byte[resWriter.m_memoryStream.Length];
                    resWriter.m_memoryStream.Flush();
                    resWriter.m_memoryStream.Position = 0;
                    resWriter.m_memoryStream.Read(resBytes, 0, resBytes.Length);

                    AddResource(GetNativeHandle(),
                                resWriter.m_strName, 
                                resBytes,
                                resBytes.Length,
                                m_moduleData.FileToken,
                                (int)resWriter.m_attribute, 
                                (int)portableExecutableKind,
                                (int)imageFileMachine);
                }
            }

            DefineNativeResource(portableExecutableKind, imageFileMachine);

            PEFileKinds pekind = isAssemblyFile ? ContainingAssemblyBuilder.m_assemblyData.m_peFileKind : PEFileKinds.Dll;

            SavePEFile(GetNativeHandle(), fileName, m_EntryPoint.Token, (int)pekind, isAssemblyFile); 

            m_moduleData.m_isSaved = true;
        }
#endif // !FEATURE_CORECLR

        [System.Security.SecurityCritical]  // auto-generated
        private int GetTypeRefNested(Type type, Module refedModule, String strRefedModuleFileName)
        {
            // This function will generate correct TypeRef token for top level type and nested type.

            Type enclosingType = type.DeclaringType;
            int tkResolution = 0;
            String typeName = type.FullName;

            if (enclosingType != null)
            {
                tkResolution = GetTypeRefNested(enclosingType, refedModule, strRefedModuleFileName);
                typeName = UnmangleTypeName(typeName);
            }

            Contract.Assert(!type.IsByRef, "Must not be ByRef.");
            Contract.Assert(!type.IsGenericType || type.IsGenericTypeDefinition, "Must not have generic arguments.");

#if FEATURE_APPX
            if (ContainingAssemblyBuilder.ProfileAPICheck)
            {
                RuntimeType rtType = type as RuntimeType;
                if (rtType != null && (rtType.InvocationFlags & INVOCATION_FLAGS.INVOCATION_FLAGS_NON_W8P_FX_API) != 0)
                {
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_APIInvalidForCurrentContext", rtType.FullName));
                }
            }
#endif

            return GetTypeRef(GetNativeHandle(), typeName, GetRuntimeModuleFromModule(refedModule).GetNativeHandle(), strRefedModuleFileName, tkResolution);
        }

        [System.Security.SecurityCritical]  // auto-generated
        internal MethodToken InternalGetConstructorToken(ConstructorInfo con, bool usingRef)
        {
            // Helper to get constructor token. If usingRef is true, we will never use the def token

            if (con == null)
                throw new ArgumentNullException("con");
            Contract.EndContractBlock();

            int tr;
            int mr = 0;

            ConstructorBuilder conBuilder = null;
            ConstructorOnTypeBuilderInstantiation conOnTypeBuilderInst = null;
            RuntimeConstructorInfo rtCon = null;

            if ( (conBuilder = con as ConstructorBuilder) != null )
            {
                if (usingRef == false && conBuilder.Module.Equals(this))
                    return conBuilder.GetToken();

                // constructor is defined in a different module
                tr = GetTypeTokenInternal(con.ReflectedType).Token;
                mr = GetMemberRef(con.ReflectedType.Module, tr, conBuilder.GetToken().Token);
            }
            else if ( (conOnTypeBuilderInst = con as ConstructorOnTypeBuilderInstantiation) != null )
            {
                if (usingRef == true) throw new InvalidOperationException();

                tr = GetTypeTokenInternal(con.DeclaringType).Token;
                mr = GetMemberRef(con.DeclaringType.Module, tr, conOnTypeBuilderInst.MetadataTokenInternal);
            }
            else if ( (rtCon = con as RuntimeConstructorInfo) != null && con.ReflectedType.IsArray == false)
            {
                // constructor is not a dynamic field
                // We need to get the TypeRef tokens

                tr = GetTypeTokenInternal(con.ReflectedType).Token;
                mr = GetMemberRefOfMethodInfo(tr, rtCon);
            }
            else
            {
                // some user derived ConstructorInfo
                // go through the slower code path, i.e. retrieve parameters and form signature helper.
                ParameterInfo[] parameters = con.GetParameters();
                if (parameters == null)
                    throw new ArgumentException(Environment.GetResourceString("Argument_InvalidConstructorInfo"));

                int count = parameters.Length;
                Type[] parameterTypes = new Type[count];
                Type[][] requiredCustomModifiers = new Type[count][];
                Type[][] optionalCustomModifiers = new Type[count][];

                for (int i = 0; i < count; i++)
                {
                    if (parameters[i] == null)
                        throw new ArgumentException(Environment.GetResourceString("Argument_InvalidConstructorInfo"));

                    parameterTypes[i] = parameters[i].ParameterType;
                    requiredCustomModifiers[i] = parameters[i].GetRequiredCustomModifiers();
                    optionalCustomModifiers[i] = parameters[i].GetOptionalCustomModifiers();
                }

                tr = GetTypeTokenInternal(con.ReflectedType).Token;

                SignatureHelper sigHelp = SignatureHelper.GetMethodSigHelper(this, con.CallingConvention, null, null, null, parameterTypes, requiredCustomModifiers, optionalCustomModifiers);
                int length;
                byte[] sigBytes = sigHelp.InternalGetSignature(out length);

                mr = GetMemberRefFromSignature(tr, con.Name, sigBytes, length);
            }
            
            return new MethodToken( mr );
        }

        [System.Security.SecurityCritical]  // auto-generated
        internal void Init(String strModuleName, String strFileName, int tkFile)
        {
            m_moduleData = new ModuleBuilderData(this, strModuleName, strFileName, tkFile);
            m_TypeBuilderDict = new Dictionary<string, Type>();
        }

        // This is a method for changing module and file name of the manifest module (created by default for 
        // each assembly).
        [System.Security.SecurityCritical]  // auto-generated
        internal void ModifyModuleName(string name)
        {
            // Reset the names in the managed ModuleBuilderData
            m_moduleData.ModifyModuleName(name);

            // Reset the name in the underlying metadata
            ModuleBuilder.SetModuleName(GetNativeHandle(), name);
        }

        internal void SetSymWriter(ISymbolWriter writer)
        {
            m_iSymWriter = writer;
        }

        internal object SyncRoot
        {
            get
            {
                return ContainingAssemblyBuilder.SyncRoot;
            }
        }

        #endregion
            
        #region Module Overrides
            
        // m_internalModuleBuilder is null iff this is a "internal" ModuleBuilder
        internal InternalModuleBuilder InternalModule
        {
            get
            {
                return m_internalModuleBuilder;
            }
        }

        internal override ModuleHandle GetModuleHandle()
        {
            return new ModuleHandle(GetNativeHandle());
        }

        internal RuntimeModule GetNativeHandle()
        {
            return InternalModule.GetNativeHandle();
        }

        private static RuntimeModule GetRuntimeModuleFromModule(Module m)
        {
            ModuleBuilder mb = m as ModuleBuilder;
            if (mb != null)
            {
                return mb.InternalModule;
            }

            return m as RuntimeModule;
        }

        [System.Security.SecurityCritical]  // auto-generated
        private int GetMemberRefToken(MethodBase method, IEnumerable<Type> optionalParameterTypes)
        {
            Type[] parameterTypes;
            Type returnType;
            int tkParent;
            int cGenericParameters = 0;

            if (method.IsGenericMethod)
            {
                if (!method.IsGenericMethodDefinition)
                    throw new InvalidOperationException();

                cGenericParameters = method.GetGenericArguments().Length;
            }

            if (optionalParameterTypes != null)
            {
                if ((method.CallingConvention & CallingConventions.VarArgs) == 0)
                {
                    // Client should not supply optional parameter in default calling convention
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_NotAVarArgCallingConvention"));
                }
            }

            MethodInfo masmi = method as MethodInfo;

            if (method.DeclaringType.IsGenericType)
            {
                MethodBase methDef = null; // methodInfo = G<Foo>.M<Bar> ==> methDef = G<T>.M<S>

                MethodOnTypeBuilderInstantiation motbi;
                ConstructorOnTypeBuilderInstantiation cotbi;

                if ((motbi = method as MethodOnTypeBuilderInstantiation) != null)
                {
                    methDef = motbi.m_method;
                }
                else if ((cotbi = method as ConstructorOnTypeBuilderInstantiation) != null)
                {
                    methDef = cotbi.m_ctor;
                }
                else if (method is MethodBuilder || method is ConstructorBuilder)
                {
                    // methodInfo must be GenericMethodDefinition; trying to emit G<?>.M<S>
                    methDef = method;
                }
                else
                {
                    Contract.Assert(method is RuntimeMethodInfo || method is RuntimeConstructorInfo);

                    if (method.IsGenericMethod)
                    {
                        Contract.Assert(masmi != null);

                        methDef = masmi.GetGenericMethodDefinition();
                        methDef = methDef.Module.ResolveMethod(
                            method.MetadataToken,
                            methDef.DeclaringType != null ? methDef.DeclaringType.GetGenericArguments() : null,
                            methDef.GetGenericArguments());
                    }
                    else
                    {
                        methDef = method.Module.ResolveMethod(
                            method.MetadataToken,
                            method.DeclaringType != null ? method.DeclaringType.GetGenericArguments() : null,
                            null);
                    }
                }

                parameterTypes = methDef.GetParameterTypes();
                returnType = MethodBuilder.GetMethodBaseReturnType(methDef);
            }
            else
            {
                parameterTypes = method.GetParameterTypes();
                returnType = MethodBuilder.GetMethodBaseReturnType(method);
            }

            int sigLength;
            byte[] sigBytes = GetMemberRefSignature(method.CallingConvention, returnType, parameterTypes,
                optionalParameterTypes, cGenericParameters).InternalGetSignature(out sigLength);

            if (method.DeclaringType.IsGenericType)
            {
                int length;
                byte[] sig = SignatureHelper.GetTypeSigToken(this, method.DeclaringType).InternalGetSignature(out length);
                tkParent = GetTokenFromTypeSpec(sig, length);
            }
            else if (!method.Module.Equals(this))
            {
                // Use typeRef as parent because the method's declaringType lives in a different assembly                
                tkParent = GetTypeToken(method.DeclaringType).Token;
            }
            else
            {
                // Use methodDef as parent because the method lives in this assembly and its declaringType has no generic arguments
                if (masmi != null)
                    tkParent = GetMethodToken(masmi).Token;
                else
                    tkParent = GetConstructorToken(method as ConstructorInfo).Token;
            }

            return GetMemberRefFromSignature(tkParent, method.Name, sigBytes, sigLength);
        }

        [System.Security.SecurityCritical]  // auto-generated
        internal SignatureHelper GetMemberRefSignature(CallingConventions call, Type returnType,
            Type[] parameterTypes, IEnumerable<Type> optionalParameterTypes, int cGenericParameters) 
        {
            int cParams = (parameterTypes == null) ? 0 : parameterTypes.Length;
            SignatureHelper sig = SignatureHelper.GetMethodSigHelper(this, call, returnType, cGenericParameters);

            for (int i = 0; i < cParams; i++)
            {
                sig.AddArgument(parameterTypes[i]);
            }

            if (optionalParameterTypes != null) {
                int i = 0;
                foreach (Type type in optionalParameterTypes)
                {
                    // add the sentinel
                    if (i == 0)
                    {
                        sig.AddSentinel();
                    }

                    sig.AddArgument(type);
                    i++;
                }
            }

            return sig;
        }

        #endregion

        #region object overrides
        public override bool Equals(object obj)
        {
            return InternalModule.Equals(obj);
        }
        // Need a dummy GetHashCode to pair with Equals
        public override int GetHashCode() { return InternalModule.GetHashCode(); }
        #endregion

        #region ICustomAttributeProvider Members
        public override Object[] GetCustomAttributes(bool inherit)
        {
            return InternalModule.GetCustomAttributes(inherit);
        }

        public override Object[] GetCustomAttributes(Type attributeType, bool inherit)
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

        public override Type[] GetTypes()
        {
            lock(SyncRoot)
            {
                return GetTypesNoLock();
            }
        }

        internal Type[] GetTypesNoLock()
        {
            int size = m_TypeBuilderDict.Count;
            Type[] typeList = new Type[m_TypeBuilderDict.Count];
            int i = 0;

            foreach (Type builder in m_TypeBuilderDict.Values)
            {
                EnumBuilder enumBldr = builder as EnumBuilder;
                TypeBuilder tmpTypeBldr;

                if (enumBldr != null)
                    tmpTypeBldr = enumBldr.m_typeBuilder;
                else
                    tmpTypeBldr = (TypeBuilder)builder;
                    
                // We should not return TypeBuilders.
                // Otherwise anyone can emit code in it.
                if (tmpTypeBldr.IsCreated())
                    typeList[i++] = tmpTypeBldr.UnderlyingSystemType;
                else
                    typeList[i++] = builder;
            }

            return typeList;
        }

        [System.Runtime.InteropServices.ComVisible(true)]
        public override Type GetType(String className)
        {
            return GetType(className, false, false);
        }
        
        [System.Runtime.InteropServices.ComVisible(true)]
        public override Type GetType(String className, bool ignoreCase)
        {
            return GetType(className, false, ignoreCase);
        }
        
        [System.Runtime.InteropServices.ComVisible(true)]
        public override Type GetType(String className, bool throwOnError, bool ignoreCase)
        {
            lock(SyncRoot)
            {
                return GetTypeNoLock(className, throwOnError, ignoreCase);
            }
        }

        private Type GetTypeNoLock(String className, bool throwOnError, bool ignoreCase)
        {
            // public API to to a type. The reason that we need this function override from module
            // is because clients might need to get foo[] when foo is being built. For example, if 
            // foo class contains a data member of type foo[].
            // This API first delegate to the Module.GetType implementation. If succeeded, great! 
            // If not, we have to look up the current module to find the TypeBuilder to represent the base
            // type and form the Type object for "foo[,]".
                
            // Module.GetType() will verify className.                
            Type baseType = InternalModule.GetType(className, throwOnError, ignoreCase);
            if (baseType != null)
                return baseType;

            // Now try to see if we contain a TypeBuilder for this type or not.
            // Might have a compound type name, indicated via an unescaped
            // '[', '*' or '&'. Split the name at this point.
            String baseName = null;
            String parameters = null;
            int startIndex = 0;

            while (startIndex <= className.Length)
            {
                // Are there any possible special characters left?
                int i = className.IndexOfAny(new char[]{'[', '*', '&'}, startIndex);
                if (i == -1)
                {
                    // No, type name is simple.
                    baseName = className;
                    parameters = null;
                    break;
                }

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

            baseName = baseName.Replace(@"\\",@"\").Replace(@"\[",@"[").Replace(@"\*",@"*").Replace(@"\&",@"&");

            if (parameters != null)
            {
                // try to see if reflection can find the base type. It can be such that reflection
                // does not support the complex format string yet!

                baseType = InternalModule.GetType(baseName, false, ignoreCase);
            }

            if (baseType == null)
            {
                // try to find it among the unbaked types.
                // starting with the current module first of all.
                baseType = FindTypeBuilderWithName(baseName, ignoreCase);
                if (baseType == null && Assembly is AssemblyBuilder)
                {
                    // now goto Assembly level to find the type.
                    int size;
                    List<ModuleBuilder> modList;

                    modList = ContainingAssemblyBuilder.m_assemblyData.m_moduleBuilderList;
                    size = modList.Count;
                    for (int i = 0; i < size && baseType == null; i++)
                    {
                        ModuleBuilder mBuilder = modList[i];
                        baseType = mBuilder.FindTypeBuilderWithName(baseName, ignoreCase);
                    }
                }
                if (baseType == null)
                    return null;
            }

            if (parameters == null)         
                return baseType;
        
            return GetType(parameters, baseType);
        }

        public override String FullyQualifiedName
        {
#if FEATURE_CORECLR
            [System.Security.SecurityCritical] // auto-generated
#else
            [System.Security.SecuritySafeCritical]
#endif
            get
            {
                String fullyQualifiedName = m_moduleData.m_strFileName;
                if (fullyQualifiedName == null)
                    return null;
                if (ContainingAssemblyBuilder.m_assemblyData.m_strDir != null)
                {
                    fullyQualifiedName = Path.Combine(ContainingAssemblyBuilder.m_assemblyData.m_strDir, fullyQualifiedName);
                    fullyQualifiedName = Path.UnsafeGetFullPath(fullyQualifiedName);
                }
                
                if (ContainingAssemblyBuilder.m_assemblyData.m_strDir != null && fullyQualifiedName != null) 
                {
                    new FileIOPermission( FileIOPermissionAccess.PathDiscovery, fullyQualifiedName ).Demand();
                }

                return fullyQualifiedName;
            }
        }

        public override byte[] ResolveSignature(int metadataToken)
        {
            return InternalModule.ResolveSignature(metadataToken);
        }

        public override MethodBase ResolveMethod(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments)
        {
            return InternalModule.ResolveMethod(metadataToken, genericTypeArguments, genericMethodArguments);
        }

        public override FieldInfo ResolveField(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments)
        {
            return InternalModule.ResolveField(metadataToken, genericTypeArguments, genericMethodArguments);
        }

        public override Type ResolveType(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments)
        {
            return InternalModule.ResolveType(metadataToken, genericTypeArguments, genericMethodArguments);
        }

        public override MemberInfo ResolveMember(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments)
        {
            return InternalModule.ResolveMember(metadataToken, genericTypeArguments, genericMethodArguments);
        }

        public override string ResolveString(int metadataToken)
        {
            return InternalModule.ResolveString(metadataToken);
        }

        public override void GetPEKind(out PortableExecutableKinds peKind, out ImageFileMachine machine)
        {
            InternalModule.GetPEKind(out peKind, out machine);
        }

        public override int MDStreamVersion
        {
            get
            {
                return InternalModule.MDStreamVersion;
            }
        }

        public override Guid ModuleVersionId
        {
            get
            {
                return InternalModule.ModuleVersionId;
            }
        }

        public override int MetadataToken
        {
            get
            {
                return InternalModule.MetadataToken;
            }
        }

        public override bool IsResource()
        {
            return InternalModule.IsResource();
        }

        public override FieldInfo[] GetFields(BindingFlags bindingFlags)
        {
            return InternalModule.GetFields(bindingFlags);
        }

        public override FieldInfo GetField(String name, BindingFlags bindingAttr)
        {
            return InternalModule.GetField(name, bindingAttr);
        }

        public override MethodInfo[] GetMethods(BindingFlags bindingFlags)
        {
            return InternalModule.GetMethods(bindingFlags);
        }

        protected override MethodInfo GetMethodImpl(String name, BindingFlags bindingAttr, Binder binder,
            CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
        {
            // Cannot call InternalModule.GetMethods because it doesn't allow types to be null
            return InternalModule.GetMethodInternal(name, bindingAttr, binder, callConvention, types, modifiers);
        }

        public override String ScopeName
        {
            get
            {
                return InternalModule.ScopeName;
            }
        }

        public override String Name
        {
            get
            {
                return InternalModule.Name;
            }
        }

        public override Assembly Assembly
        {
            [Pure]
            get
            {
                return m_assemblyBuilder;
            }
        }

#if FEATURE_X509 && FEATURE_CAS_POLICY
        public override System.Security.Cryptography.X509Certificates.X509Certificate GetSignerCertificate()
        {
            return InternalModule.GetSignerCertificate();
        }
#endif // FEATURE_X509 && FEATURE_CAS_POLICY
        #endregion

        #region Public Members

        #region Define Type
        [System.Security.SecuritySafeCritical]  // auto-generated
        public TypeBuilder DefineType(String name)
        {
            Contract.Ensures(Contract.Result<TypeBuilder>() != null);

            lock(SyncRoot)
            {
                return DefineTypeNoLock(name, TypeAttributes.NotPublic, null, null, PackingSize.Unspecified, TypeBuilder.UnspecifiedTypeSize);
            }
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public TypeBuilder DefineType(String name, TypeAttributes attr)
        {
            Contract.Ensures(Contract.Result<TypeBuilder>() != null);

            lock(SyncRoot)
            {
                return DefineTypeNoLock(name, attr, null, null, PackingSize.Unspecified, TypeBuilder.UnspecifiedTypeSize);
            }
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public TypeBuilder DefineType(String name, TypeAttributes attr, Type parent)
        {
            Contract.Ensures(Contract.Result<TypeBuilder>() != null);

            lock(SyncRoot)
            {
                // Why do we only call CheckContext here? Why don't we call it in the other overloads?
                CheckContext(parent);

                return DefineTypeNoLock(name, attr, parent, null, PackingSize.Unspecified, TypeBuilder.UnspecifiedTypeSize);
            }
        }

#if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
#else
        [System.Security.SecuritySafeCritical]
#endif
        public TypeBuilder DefineType(String name, TypeAttributes attr, Type parent, int typesize)
        {
            Contract.Ensures(Contract.Result<TypeBuilder>() != null);

            lock(SyncRoot)
            {
                return DefineTypeNoLock(name, attr, parent, null, PackingSize.Unspecified, typesize);
            }
        }

#if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
#else
        [System.Security.SecuritySafeCritical]
#endif
        public TypeBuilder DefineType(String name, TypeAttributes attr, Type parent, PackingSize packingSize, int typesize)
        {
            Contract.Ensures(Contract.Result<TypeBuilder>() != null);

            lock (SyncRoot)
            {
                return DefineTypeNoLock(name, attr, parent, null, packingSize, typesize);
            }
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        [System.Runtime.InteropServices.ComVisible(true)]
        public TypeBuilder DefineType(String name, TypeAttributes attr, Type parent, Type[] interfaces)
        {
            Contract.Ensures(Contract.Result<TypeBuilder>() != null);

            lock(SyncRoot)
            {
                return DefineTypeNoLock(name, attr, parent, interfaces, PackingSize.Unspecified, TypeBuilder.UnspecifiedTypeSize);
            }
        }

        [System.Security.SecurityCritical]  // auto-generated
        private TypeBuilder DefineTypeNoLock(String name, TypeAttributes attr, Type parent, Type[] interfaces, PackingSize packingSize, int typesize)
        {
            Contract.Ensures(Contract.Result<TypeBuilder>() != null);

            return new TypeBuilder(name, attr, parent, interfaces, this, packingSize, typesize, null); ;
        }

#if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
#else
        [System.Security.SecuritySafeCritical]
#endif
        public TypeBuilder DefineType(String name, TypeAttributes attr, Type parent, PackingSize packsize)
        {
            Contract.Ensures(Contract.Result<TypeBuilder>() != null);

            lock(SyncRoot)
            {
                return DefineTypeNoLock(name, attr, parent, packsize);
            }
        }

        [System.Security.SecurityCritical]  // auto-generated
        private TypeBuilder DefineTypeNoLock(String name, TypeAttributes attr, Type parent, PackingSize packsize)
        {
            Contract.Ensures(Contract.Result<TypeBuilder>() != null);

            return new TypeBuilder(name, attr, parent, null, this, packsize, TypeBuilder.UnspecifiedTypeSize, null);
        }

        #endregion

        #region Define Enum

        // This API can only be used to construct a top-level (not nested) enum type.
        // Nested enum types can be defined manually using ModuleBuilder.DefineType.
        [System.Security.SecuritySafeCritical]  // auto-generated
        public EnumBuilder DefineEnum(String name, TypeAttributes visibility, Type underlyingType)
        {
            Contract.Ensures(Contract.Result<EnumBuilder>() != null);

            CheckContext(underlyingType);
            lock(SyncRoot)
            {
                EnumBuilder enumBuilder = DefineEnumNoLock(name, visibility, underlyingType);

                // This enum is not generic, nested, and cannot have any element type.
                Contract.Assert(name == enumBuilder.FullName);

                // Replace the TypeBuilder object in m_TypeBuilderDict with this EnumBuilder object.
                Contract.Assert(enumBuilder.m_typeBuilder == m_TypeBuilderDict[name]);
                m_TypeBuilderDict[name] = enumBuilder;

                return enumBuilder;
            }
        }

        [System.Security.SecurityCritical]  // auto-generated
        private EnumBuilder DefineEnumNoLock(String name, TypeAttributes visibility, Type underlyingType)
        {
            Contract.Ensures(Contract.Result<EnumBuilder>() != null);

            return new EnumBuilder(name, underlyingType, visibility, this);
        }
    
        #endregion

        #region Define Resource
#if !FEATURE_CORECLR
        public IResourceWriter DefineResource(String name, String description)
        {
            // Define embedded managed resource to be stored in this module
            Contract.Ensures(Contract.Result<IResourceWriter>() != null);
             
            return DefineResource(name, description, ResourceAttributes.Public);
        }

        public IResourceWriter DefineResource(String name, String description, ResourceAttributes attribute)
        {
            // Define embedded managed resource to be stored in this module
            Contract.Ensures(Contract.Result<IResourceWriter>() != null);

            lock(SyncRoot)
            {
                return DefineResourceNoLock(name, description, attribute);
            }
        }

        private IResourceWriter DefineResourceNoLock(String name, String description, ResourceAttributes attribute)
        {
            // Define embedded managed resource to be stored in this module

            if (IsTransient())
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_BadResourceContainer"));

            if (name == null)
                throw new ArgumentNullException("name");
            if (name.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyName"), "name");
            Contract.Ensures(Contract.Result<IResourceWriter>() != null);
            Contract.EndContractBlock();

            if (m_assemblyBuilder.IsPersistable())
            {
                m_assemblyBuilder.m_assemblyData.CheckResNameConflict(name);

                    MemoryStream stream = new MemoryStream();
                    ResourceWriter resWriter = new ResourceWriter(stream);
                    ResWriterData resWriterData = new ResWriterData( resWriter, stream, name, String.Empty, String.Empty, attribute);

                // chain it to the embedded resource list
                resWriterData.m_nextResWriter = m_moduleData.m_embeddedRes;
                m_moduleData.m_embeddedRes = resWriterData;
                return resWriter;
            }
            else
            {
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_BadResourceContainer"));
            }
        }
#endif // !FEATURE_CORECLR

#if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
#endif
        public void DefineManifestResource(String name, Stream stream, ResourceAttributes attribute)
        {
            if (name == null)
                throw new ArgumentNullException("name");
            
            if (stream == null)
                throw new ArgumentNullException("stream");
            Contract.EndContractBlock();

            // Define embedded managed resource to be stored in this module

            lock(SyncRoot)
            {
                DefineManifestResourceNoLock(name, stream, attribute);
            }
        }

        private void DefineManifestResourceNoLock(String name, Stream stream, ResourceAttributes attribute)
        {
            // Define embedded managed resource to be stored in this module
           if (IsTransient())
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_BadResourceContainer"));
           Contract.EndContractBlock();

#if !FEATURE_CORECLR
            if (name == null)
                throw new ArgumentNullException("name");
            if (name.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyName"), "name");
        
            if (m_assemblyBuilder.IsPersistable())
            {
                m_assemblyBuilder.m_assemblyData.CheckResNameConflict(name);

                ResWriterData resWriterData = new ResWriterData( null, stream, name, String.Empty, String.Empty, attribute);
    
                // chain it to the embedded resource list
                resWriterData.m_nextResWriter = m_moduleData.m_embeddedRes;
                m_moduleData.m_embeddedRes = resWriterData;
            }
            else
            { 
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_BadResourceContainer"));
            }
#endif // !FEATURE_CORECLR
        }


#if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
#endif
        public void DefineUnmanagedResource(Byte[] resource)
        {
            lock(SyncRoot)
            {
                DefineUnmanagedResourceInternalNoLock(resource);
            }
        }

        internal void DefineUnmanagedResourceInternalNoLock(Byte[] resource)
        {
            if (resource == null)
                throw new ArgumentNullException("resource");
            Contract.EndContractBlock();

            if (m_moduleData.m_strResourceFileName != null || m_moduleData.m_resourceBytes != null)
                throw new ArgumentException(Environment.GetResourceString("Argument_NativeResourceAlreadyDefined"));
                        
            m_moduleData.m_resourceBytes = new byte[resource.Length];
            Buffer.BlockCopy(resource, 0, m_moduleData.m_resourceBytes, 0, resource.Length);
        }

#if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
#else
        [System.Security.SecuritySafeCritical]
#endif
        public void DefineUnmanagedResource(String resourceFileName)
        {
            lock(SyncRoot)
            {
                DefineUnmanagedResourceFileInternalNoLock(resourceFileName);
            }
        }

        [System.Security.SecurityCritical]  // auto-generated
        internal void DefineUnmanagedResourceFileInternalNoLock(String resourceFileName)
        {
            if (resourceFileName == null)
                throw new ArgumentNullException("resourceFileName");
            Contract.EndContractBlock();

            if (m_moduleData.m_resourceBytes != null || m_moduleData.m_strResourceFileName != null)
                throw new ArgumentException(Environment.GetResourceString("Argument_NativeResourceAlreadyDefined"));

            // Check caller has the right to read the file.
            string strFullFileName;
            strFullFileName = Path.UnsafeGetFullPath(resourceFileName);
            new FileIOPermission(FileIOPermissionAccess.Read, strFullFileName).Demand();

            new EnvironmentPermission(PermissionState.Unrestricted).Assert();
            try
            {
                if (File.UnsafeExists(resourceFileName) == false)
                    throw new FileNotFoundException(Environment.GetResourceString(
                        "IO.FileNotFound_FileName",
                        resourceFileName), resourceFileName);
            }
            finally
            {
                CodeAccessPermission.RevertAssert();
            }

            m_moduleData.m_strResourceFileName = strFullFileName;
        }
        #endregion

        #region Define Global Method
        public MethodBuilder DefineGlobalMethod(String name, MethodAttributes attributes, Type returnType, Type[] parameterTypes)
        {
            Contract.Ensures(Contract.Result<MethodBuilder>() != null);

            return DefineGlobalMethod(name, attributes, CallingConventions.Standard, returnType, parameterTypes);
        }

        public MethodBuilder DefineGlobalMethod(String name, MethodAttributes attributes, CallingConventions callingConvention, 
            Type returnType, Type[] parameterTypes)
        {
            Contract.Ensures(Contract.Result<MethodBuilder>() != null);

            return DefineGlobalMethod(name, attributes, callingConvention, returnType, null, null, parameterTypes, null, null);
        }

        public MethodBuilder DefineGlobalMethod(String name, MethodAttributes attributes, CallingConventions callingConvention, 
            Type returnType, Type[] requiredReturnTypeCustomModifiers, Type[] optionalReturnTypeCustomModifiers,
            Type[] parameterTypes, Type[][] requiredParameterTypeCustomModifiers, Type[][] optionalParameterTypeCustomModifiers)
        {
            lock(SyncRoot)
            {
                return DefineGlobalMethodNoLock(name, attributes, callingConvention, returnType, 
                                                requiredReturnTypeCustomModifiers, optionalReturnTypeCustomModifiers,
                                                parameterTypes, requiredParameterTypeCustomModifiers, optionalParameterTypeCustomModifiers);
            }
        }

        private MethodBuilder DefineGlobalMethodNoLock(String name, MethodAttributes attributes, CallingConventions callingConvention, 
            Type returnType, Type[] requiredReturnTypeCustomModifiers, Type[] optionalReturnTypeCustomModifiers,
            Type[] parameterTypes, Type[][] requiredParameterTypeCustomModifiers, Type[][] optionalParameterTypeCustomModifiers)
        {
            if (m_moduleData.m_fGlobalBeenCreated == true)
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_GlobalsHaveBeenCreated"));
        
            if (name == null)
                throw new ArgumentNullException("name");

            if (name.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyName"), "name");
        
            if ((attributes & MethodAttributes.Static) == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_GlobalFunctionHasToBeStatic"));
            Contract.Ensures(Contract.Result<MethodBuilder>() != null);
            Contract.EndContractBlock();

            CheckContext(returnType);
            CheckContext(requiredReturnTypeCustomModifiers, optionalReturnTypeCustomModifiers, parameterTypes);
            CheckContext(requiredParameterTypeCustomModifiers);
            CheckContext(optionalParameterTypeCustomModifiers);

            m_moduleData.m_fHasGlobal = true;

            return m_moduleData.m_globalTypeBuilder.DefineMethod(name, attributes, callingConvention, 
                returnType, requiredReturnTypeCustomModifiers, optionalReturnTypeCustomModifiers, 
                parameterTypes, requiredParameterTypeCustomModifiers, optionalParameterTypeCustomModifiers);
        }
        
#if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
#endif
        public MethodBuilder DefinePInvokeMethod(String name, String dllName, MethodAttributes attributes, 
            CallingConventions callingConvention, Type returnType, Type[] parameterTypes, 
            CallingConvention nativeCallConv, CharSet nativeCharSet)
        {
            Contract.Ensures(Contract.Result<MethodBuilder>() != null);

            return DefinePInvokeMethod(name, dllName, name, attributes, callingConvention, returnType, parameterTypes, nativeCallConv, nativeCharSet);
        }

#if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
#endif
        public MethodBuilder DefinePInvokeMethod(String name, String dllName, String entryName, MethodAttributes attributes, 
            CallingConventions callingConvention, Type returnType, Type[] parameterTypes, CallingConvention nativeCallConv, 
            CharSet nativeCharSet)
        {
            Contract.Ensures(Contract.Result<MethodBuilder>() != null);

            lock(SyncRoot)
            {
                return DefinePInvokeMethodNoLock(name, dllName, entryName, attributes, callingConvention, 
                                                 returnType, parameterTypes, nativeCallConv, nativeCharSet);
            }
        }

#if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
#endif
        private MethodBuilder DefinePInvokeMethodNoLock(String name, String dllName, String entryName, MethodAttributes attributes, 
            CallingConventions callingConvention, Type returnType, Type[] parameterTypes, CallingConvention nativeCallConv, 
            CharSet nativeCharSet)
        {
            //Global methods must be static.        
            if ((attributes & MethodAttributes.Static) == 0)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_GlobalFunctionHasToBeStatic"));
            }
            Contract.Ensures(Contract.Result<MethodBuilder>() != null);
            Contract.EndContractBlock();

            CheckContext(returnType);
            CheckContext(parameterTypes);

            m_moduleData.m_fHasGlobal = true;
            return m_moduleData.m_globalTypeBuilder.DefinePInvokeMethod(name, dllName, entryName, attributes, callingConvention, returnType, parameterTypes, nativeCallConv, nativeCharSet);
        }
        
        public void CreateGlobalFunctions()
        {
            lock(SyncRoot)
            {
                CreateGlobalFunctionsNoLock();
            }
        }

        private void CreateGlobalFunctionsNoLock()
        {
            if (m_moduleData.m_fGlobalBeenCreated)
            {
                // cannot create globals twice
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_NotADebugModule"));
            }
            m_moduleData.m_globalTypeBuilder.CreateType();
            m_moduleData.m_fGlobalBeenCreated = true;
        }
    
        #endregion

        #region Define Data

#if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
#endif
        public FieldBuilder DefineInitializedData(String name, byte[] data, FieldAttributes attributes)
        {
            // This method will define an initialized Data in .sdata. 
            // We will create a fake TypeDef to represent the data with size. This TypeDef
            // will be the signature for the Field.         
            Contract.Ensures(Contract.Result<FieldBuilder>() != null);

            lock(SyncRoot)
            {
                return DefineInitializedDataNoLock(name, data, attributes);
            }
        }

#if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
#endif
        private FieldBuilder DefineInitializedDataNoLock(String name, byte[] data, FieldAttributes attributes)
        {
            // This method will define an initialized Data in .sdata. 
            // We will create a fake TypeDef to represent the data with size. This TypeDef
            // will be the signature for the Field.
            if (m_moduleData.m_fGlobalBeenCreated == true)
            {
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_GlobalsHaveBeenCreated"));
            }
            Contract.Ensures(Contract.Result<FieldBuilder>() != null);
            Contract.EndContractBlock();
        
            m_moduleData.m_fHasGlobal = true;
            return m_moduleData.m_globalTypeBuilder.DefineInitializedData(name, data, attributes);
        }
        
#if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
#endif
        public FieldBuilder DefineUninitializedData(String name, int size, FieldAttributes attributes)
        {
            Contract.Ensures(Contract.Result<FieldBuilder>() != null);

            lock(SyncRoot)
            {
                return DefineUninitializedDataNoLock(name, size, attributes);
            }
        }

#if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
#endif
        private FieldBuilder DefineUninitializedDataNoLock(String name, int size, FieldAttributes attributes)
        {
            // This method will define an uninitialized Data in .sdata. 
            // We will create a fake TypeDef to represent the data with size. This TypeDef
            // will be the signature for the Field. 

            if (m_moduleData.m_fGlobalBeenCreated == true)
            {
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_GlobalsHaveBeenCreated"));
            }
            Contract.Ensures(Contract.Result<FieldBuilder>() != null);
            Contract.EndContractBlock();
        
            m_moduleData.m_fHasGlobal = true;
            return m_moduleData.m_globalTypeBuilder.DefineUninitializedData(name, size, attributes);
        }
                
        #endregion

        #region GetToken
        // For a generic type definition, we should return the token for the generic type definition itself in two cases: 
        //   1. GetTypeToken
        //   2. ldtoken (see ILGenerator)
        // For all other occasions we should return the generic type instantiated on its formal parameters.
        [System.Security.SecurityCritical]  // auto-generated
        internal TypeToken GetTypeTokenInternal(Type type)
        {
            return GetTypeTokenInternal(type, false);
        }

        [System.Security.SecurityCritical]  // auto-generated
        private TypeToken GetTypeTokenInternal(Type type, bool getGenericDefinition)
        {
            lock(SyncRoot)
            {
                return GetTypeTokenWorkerNoLock(type, getGenericDefinition);
            }
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public TypeToken GetTypeToken(Type type)
        {        
            return GetTypeTokenInternal(type, true);
        }

        [System.Security.SecurityCritical]  // auto-generated
        private TypeToken GetTypeTokenWorkerNoLock(Type type, bool getGenericDefinition)
        {
            if (type == null)
                throw new ArgumentNullException("type");
            Contract.EndContractBlock();

            CheckContext(type);
            
            // Return a token for the class relative to the Module.  Tokens
            // are used to indentify objects when the objects are used in IL
            // instructions.  Tokens are always relative to the Module.  For example,
            // the token value for System.String is likely to be different from
            // Module to Module.  Calling GetTypeToken will cause a reference to be
            // added to the Module.  This reference becomes a perminate part of the Module,
            // multiple calles to this method with the same class have no additional side affects.
            // This function is optimized to use the TypeDef token if Type is within the same module.
            // We should also be aware of multiple dynamic modules and multiple implementation of Type!!!

            if (type.IsByRef)
                throw new ArgumentException(Environment.GetResourceString("Argument_CannotGetTypeTokenForByRef"));

            if ((type.IsGenericType && (!type.IsGenericTypeDefinition || !getGenericDefinition)) ||
                type.IsGenericParameter ||
                type.IsArray ||
                type.IsPointer)
            {
                int length;
                byte[] sig = SignatureHelper.GetTypeSigToken(this, type).InternalGetSignature(out length);
                return new TypeToken(GetTokenFromTypeSpec(sig, length));
            }

            Module refedModule = type.Module;

            if (refedModule.Equals(this))
            {
                // no need to do anything additional other than defining the TypeRef Token
                TypeBuilder typeBuilder = null;
                GenericTypeParameterBuilder paramBuilder = null;

                EnumBuilder enumBuilder = type as EnumBuilder;
                if (enumBuilder != null)
                    typeBuilder = enumBuilder.m_typeBuilder;
                else
                    typeBuilder = type as TypeBuilder;

                if (typeBuilder != null)
                {
                    // optimization: if the type is defined in this module,
                    // just return the token
                    //
                    return typeBuilder.TypeToken;
                }
                else if ((paramBuilder = type as GenericTypeParameterBuilder) != null)
                {
                    return new TypeToken(paramBuilder.MetadataTokenInternal);
                }
                
                return new TypeToken(GetTypeRefNested(type, this, String.Empty));
            }
                    
            // After this point, the referenced module is not the same as the referencing
            // module.
            //
            ModuleBuilder refedModuleBuilder = refedModule as ModuleBuilder;

#if !FEATURE_CORECLR
            Contract.Assert(refedModuleBuilder != null || refedModule is RuntimeModule);
            bool isRefedModuleTransient = refedModuleBuilder != null ?
                                          refedModuleBuilder.IsTransient() :
                                          ((RuntimeModule)refedModule).IsTransientInternal();

            // We cannot have a non-transient module referencing to a transient module.
            if (IsTransient() == false && isRefedModuleTransient)
            {
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_BadTransientModuleReference"));
            }
#endif // !FEATURE_CORECLR

            String strRefedModuleFileName = String.Empty;
            if (refedModule.Assembly.Equals(this.Assembly))
            {
                // if the referenced module is in the same assembly, the resolution
                // scope of the type token will be a module ref, we will need
                // the file name of the referenced module for that.
                // if the refed module is in a different assembly, the resolution
                // scope of the type token will be an assembly ref. We don't need
                // the file name of the referenced module.
                if (refedModuleBuilder == null)
                {
                    refedModuleBuilder = this.ContainingAssemblyBuilder.GetModuleBuilder((InternalModuleBuilder)refedModule);
                }
                strRefedModuleFileName = refedModuleBuilder.m_moduleData.m_strFileName;
            }

            return new TypeToken(GetTypeRefNested(type, refedModule, strRefedModuleFileName));
        }
        
        public TypeToken GetTypeToken(String name)
        {
            // Return a token for the class relative to the Module. 
            // Module.GetType() verifies name
            
            // Unfortunately, we will need to load the Type and then call GetTypeToken in 
            // order to correctly track the assembly reference information.
            
            return GetTypeToken(InternalModule.GetType(name, false, true));
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public MethodToken GetMethodToken(MethodInfo method)
        {
            lock(SyncRoot)
            {
                return GetMethodTokenNoLock(method, true);
            }
        }

        [System.Security.SecurityCritical]  // auto-generated
        internal MethodToken GetMethodTokenInternal(MethodInfo method)
        {
            lock(SyncRoot)
            {
                return GetMethodTokenNoLock(method, false);
            }
        }

        // For a method on a generic type, we should return the methoddef token on the generic type definition in two cases
        //   1. GetMethodToken
        //   2. ldtoken (see ILGenerator)
        // For all other occasions we should return the method on the generic type instantiated on the formal parameters.
        [System.Security.SecurityCritical]  // auto-generated
        private MethodToken GetMethodTokenNoLock(MethodInfo method, bool getGenericTypeDefinition)
        {
            // Return a MemberRef token if MethodInfo is not defined in this module. Or 
            // return the MethodDef token. 
            if (method == null)
                throw new ArgumentNullException("method");
            Contract.EndContractBlock();

            int tr;
            int mr = 0;
            
            SymbolMethod symMethod = null;
            MethodBuilder methBuilder = null;

            if ( (methBuilder = method as MethodBuilder) != null )
            {
                int methodToken = methBuilder.MetadataTokenInternal;
                if (method.Module.Equals(this))
                    return new MethodToken(methodToken);

                if (method.DeclaringType == null)
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_CannotImportGlobalFromDifferentModule"));

                // method is defined in a different module
                tr = getGenericTypeDefinition ? GetTypeToken(method.DeclaringType).Token : GetTypeTokenInternal(method.DeclaringType).Token;
                mr = GetMemberRef(method.DeclaringType.Module, tr, methodToken);
            }
            else if (method is MethodOnTypeBuilderInstantiation)
            {
                return new MethodToken(GetMemberRefToken(method, null));
            }
            else if ((symMethod = method as SymbolMethod) != null)
            {
                if (symMethod.GetModule() == this)
                    return symMethod.GetToken();

                // form the method token
                return symMethod.GetToken(this);
            }
            else
            {
                Type declaringType = method.DeclaringType;

                // We need to get the TypeRef tokens
                if (declaringType == null)
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_CannotImportGlobalFromDifferentModule"));

                RuntimeMethodInfo rtMeth = null;

                if (declaringType.IsArray == true)
                {
                    // use reflection to build signature to work around the E_T_VAR problem in EEClass
                    ParameterInfo[] paramInfo = method.GetParameters();
                    
                    Type[] tt = new Type[paramInfo.Length];
                    
                    for (int i = 0; i < paramInfo.Length; i++)
                        tt[i] = paramInfo[i].ParameterType;

                    return GetArrayMethodToken(declaringType, method.Name, method.CallingConvention, method.ReturnType, tt);
                }
                else if ( (rtMeth = method as RuntimeMethodInfo) != null )
                {
                    tr = getGenericTypeDefinition ? GetTypeToken(method.DeclaringType).Token : GetTypeTokenInternal(method.DeclaringType).Token;
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
          
                    tr = getGenericTypeDefinition ? GetTypeToken(method.DeclaringType).Token : GetTypeTokenInternal(method.DeclaringType).Token;

                    SignatureHelper sigHelp;

                    try 
                    {
                        sigHelp = SignatureHelper.GetMethodSigHelper(
                        this, method.CallingConvention, method.ReturnType, 
                        method.ReturnParameter.GetRequiredCustomModifiers(), method.ReturnParameter.GetOptionalCustomModifiers(), 
                        parameterTypes, requiredCustomModifiers, optionalCustomModifiers);
                    } 
                    catch(NotImplementedException)
                    {
                        // Legacy code deriving from MethodInfo may not have implemented ReturnParameter.
                        sigHelp = SignatureHelper.GetMethodSigHelper(this, method.ReturnType, parameterTypes);
                    }

                    int length;                                           
                    byte[] sigBytes = sigHelp.InternalGetSignature(out length);
                    mr = GetMemberRefFromSignature(tr, method.Name, sigBytes, length);
                }
            }

            return new MethodToken(mr);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public MethodToken GetConstructorToken(ConstructorInfo constructor, IEnumerable<Type> optionalParameterTypes)
        {
            if (constructor == null)
            {
                throw new ArgumentNullException("constructor");
            }

            lock (SyncRoot)
            {
                // useMethodDef is not applicable - constructors aren't generic
                return new MethodToken(GetMethodTokenInternal(constructor, optionalParameterTypes, false));
            }
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public MethodToken GetMethodToken(MethodInfo method, IEnumerable<Type> optionalParameterTypes)
        {
            if (method == null)
            {
                throw new ArgumentNullException("method");
            }

            // useMethodDef flag only affects the result if we pass in a generic method definition. 
            // If the caller is looking for a token for an ldtoken/ldftn/ldvirtftn instruction and passes in a generic method definition info/builder, 
            // we correclty return the MethodDef/Ref token of the generic definition that can be used with ldtoken/ldftn/ldvirtftn. 
            //
            // If the caller is looking for a token for a call/callvirt/jmp instruction and passes in a generic method definition info/builder,
            // we also return the generic MethodDef/Ref token, which is indeed not acceptable for call/callvirt/jmp instruction.
            // But the caller can always instantiate the info/builder and pass it in. Then we build the right MethodSpec.

            lock (SyncRoot)
            {
                return new MethodToken(GetMethodTokenInternal(method, optionalParameterTypes, true));
            }
        }

        [System.Security.SecurityCritical]  // auto-generated
        internal int GetMethodTokenInternal(MethodBase method, IEnumerable<Type> optionalParameterTypes, bool useMethodDef)
        {
            int tk = 0;
            MethodInfo methodInfo = method as MethodInfo;

            if (method.IsGenericMethod)
            {
                // Constructors cannot be generic.
                Contract.Assert(methodInfo != null);

                // Given M<Bar> unbind to M<S>
                MethodInfo methodInfoUnbound = methodInfo;
                bool isGenericMethodDef = methodInfo.IsGenericMethodDefinition;

                if (!isGenericMethodDef)
                {
                    methodInfoUnbound = methodInfo.GetGenericMethodDefinition();
                }

                if (!this.Equals(methodInfoUnbound.Module)
                    || (methodInfoUnbound.DeclaringType != null && methodInfoUnbound.DeclaringType.IsGenericType))
                {
                    tk = GetMemberRefToken(methodInfoUnbound, null);
                }
                else
                {
                    tk = GetMethodTokenInternal(methodInfoUnbound).Token;
                }

                // For Ldtoken, Ldftn, and Ldvirtftn, we should emit the method def/ref token for a generic method definition.
                if (isGenericMethodDef && useMethodDef)
                {
                    return tk;
                }

                // Create signature of method instantiation M<Bar>
                int sigLength;
                byte[] sigBytes = SignatureHelper.GetMethodSpecSigHelper(
                    this, methodInfo.GetGenericArguments()).InternalGetSignature(out sigLength);

                // Create MethodSepc M<Bar> with parent G?.M<S> 
                tk = TypeBuilder.DefineMethodSpec(this.GetNativeHandle(), tk, sigBytes, sigLength);
            }
            else
            {
                if (((method.CallingConvention & CallingConventions.VarArgs) == 0) &&
                    (method.DeclaringType == null || !method.DeclaringType.IsGenericType))
                {
                    if (methodInfo != null)
                    {
                        tk = GetMethodTokenInternal(methodInfo).Token;
                    }
                    else
                    {
                        tk = GetConstructorToken(method as ConstructorInfo).Token;
                    }
                }
                else
                {
                    tk = GetMemberRefToken(method, optionalParameterTypes);
                }
            }

            return tk;
        }
    
        [System.Security.SecuritySafeCritical]  // auto-generated
        public MethodToken GetArrayMethodToken(Type arrayClass, String methodName, CallingConventions callingConvention, 
            Type returnType, Type[] parameterTypes)
        {
            lock(SyncRoot)
            {
                return GetArrayMethodTokenNoLock(arrayClass, methodName, callingConvention, returnType, parameterTypes);
            }
        }

        [System.Security.SecurityCritical]  // auto-generated
        private MethodToken GetArrayMethodTokenNoLock(Type arrayClass, String methodName, CallingConventions callingConvention, 
            Type returnType, Type[] parameterTypes)
        {
            if (arrayClass == null)
                throw new ArgumentNullException("arrayClass");

            if (methodName == null)
                throw new ArgumentNullException("methodName");

            if (methodName.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyName"), "methodName");

            if (arrayClass.IsArray == false)
                throw new ArgumentException(Environment.GetResourceString("Argument_HasToBeArrayClass")); 
            Contract.EndContractBlock();

            CheckContext(returnType, arrayClass);
            CheckContext(parameterTypes);

            // Return a token for the MethodInfo for a method on an Array.  This is primarily
            // used to get the LoadElementAddress method. 

            int length;

            SignatureHelper sigHelp = SignatureHelper.GetMethodSigHelper(
                this, callingConvention, returnType, null, null, parameterTypes, null, null);

            byte[] sigBytes = sigHelp.InternalGetSignature(out length);

            TypeToken typeSpec = GetTypeTokenInternal(arrayClass);

            return new MethodToken(GetArrayMethodToken(GetNativeHandle(),
                typeSpec.Token, methodName, sigBytes, length));
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public MethodInfo GetArrayMethod(Type arrayClass, String methodName, CallingConventions callingConvention, 
            Type returnType, Type[] parameterTypes)
        {
            CheckContext(returnType, arrayClass);
            CheckContext(parameterTypes);

            // GetArrayMethod is useful when you have an array of a type whose definition has not been completed and 
            // you want to access methods defined on Array. For example, you might define a type and want to define a 
            // method that takes an array of the type as a parameter. In order to access the elements of the array, 
            // you will need to call methods of the Array class.

            MethodToken token = GetArrayMethodToken(arrayClass, methodName, callingConvention, returnType, parameterTypes);

            return new SymbolMethod(this, token, arrayClass, methodName, callingConvention, returnType, parameterTypes);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        [System.Runtime.InteropServices.ComVisible(true)]
        public MethodToken GetConstructorToken(ConstructorInfo con)
        {
            // Return a token for the ConstructorInfo relative to the Module. 
            return InternalGetConstructorToken(con, false);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public FieldToken GetFieldToken(FieldInfo field) 
        {
            lock(SyncRoot)
            {
                return GetFieldTokenNoLock(field);
            }
        }

        [System.Security.SecurityCritical]  // auto-generated
        private FieldToken GetFieldTokenNoLock(FieldInfo field) 
        {
            if (field == null) {
                throw new ArgumentNullException("con");
            }
            Contract.EndContractBlock();

            int     tr;
            int     mr = 0;

            FieldBuilder fdBuilder = null;
            RuntimeFieldInfo rtField = null;
            FieldOnTypeBuilderInstantiation fOnTB = null;

            if ((fdBuilder = field as FieldBuilder) != null)
            {
                if (field.DeclaringType != null && field.DeclaringType.IsGenericType)
                {
                    int length;
                    byte[] sig = SignatureHelper.GetTypeSigToken(this, field.DeclaringType).InternalGetSignature(out length);
                    tr = GetTokenFromTypeSpec(sig, length);
                    mr = GetMemberRef(this, tr, fdBuilder.GetToken().Token);
                }
                else if (fdBuilder.Module.Equals(this))
                {
                    // field is defined in the same module
                    return fdBuilder.GetToken();
                }
                else
                {
                    // field is defined in a different module
                    if (field.DeclaringType == null)
                    {
                        throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_CannotImportGlobalFromDifferentModule"));
                    }
                    tr = GetTypeTokenInternal(field.DeclaringType).Token;
                    mr = GetMemberRef(field.ReflectedType.Module, tr, fdBuilder.GetToken().Token);
                }
            }
            else if ( (rtField = field as RuntimeFieldInfo) != null)
            {
                // FieldInfo is not an dynamic field
                
                // We need to get the TypeRef tokens
                if (field.DeclaringType == null)
                {
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_CannotImportGlobalFromDifferentModule"));
                }
                
                if (field.DeclaringType != null && field.DeclaringType.IsGenericType)
                {
                    int length;
                    byte[] sig = SignatureHelper.GetTypeSigToken(this, field.DeclaringType).InternalGetSignature(out length);
                    tr = GetTokenFromTypeSpec(sig, length);
                    mr = GetMemberRefOfFieldInfo(tr, field.DeclaringType.GetTypeHandleInternal(), rtField);
                }
                else
                {
                    tr = GetTypeTokenInternal(field.DeclaringType).Token;       
                    mr = GetMemberRefOfFieldInfo(tr, field.DeclaringType.GetTypeHandleInternal(), rtField);
                }
            }
            else if ( (fOnTB = field as FieldOnTypeBuilderInstantiation) != null)
            {
                FieldInfo fb = fOnTB.FieldInfo;
                int length;
                byte[] sig = SignatureHelper.GetTypeSigToken(this, field.DeclaringType).InternalGetSignature(out length);
                tr = GetTokenFromTypeSpec(sig, length);
                mr = GetMemberRef(fb.ReflectedType.Module, tr, fOnTB.MetadataTokenInternal);
            }
            else
            {
                // user defined FieldInfo
                tr = GetTypeTokenInternal(field.ReflectedType).Token;

                SignatureHelper sigHelp = SignatureHelper.GetFieldSigHelper(this);

                sigHelp.AddArgument(field.FieldType, field.GetRequiredCustomModifiers(), field.GetOptionalCustomModifiers());

                int length;
                byte[] sigBytes = sigHelp.InternalGetSignature(out length);

                mr = GetMemberRefFromSignature(tr, field.Name, sigBytes, length);
            }
            
            return new FieldToken(mr, field.GetType());
        }
        
        [System.Security.SecuritySafeCritical]  // auto-generated
        public StringToken GetStringConstant(String str) 
        {
            if (str == null)
            {
                throw new ArgumentNullException("str");
            }
            Contract.EndContractBlock();

            // Returns a token representing a String constant.  If the string 
            // value has already been defined, the existing token will be returned.
            return new StringToken(GetStringConstant(GetNativeHandle(), str, str.Length));
        }
    
        [System.Security.SecuritySafeCritical]  // auto-generated
        public SignatureToken GetSignatureToken(SignatureHelper sigHelper)
        {
            // Define signature token given a signature helper. This will define a metadata
            // token for the signature described by SignatureHelper.

            if (sigHelper == null)
            {
                throw new ArgumentNullException("sigHelper");
            }
            Contract.EndContractBlock();

            int sigLength;
            byte[] sigBytes;
    
            // get the signature in byte form
            sigBytes = sigHelper.InternalGetSignature(out sigLength);
            return new SignatureToken(TypeBuilder.GetTokenFromSig(GetNativeHandle(), sigBytes, sigLength), this);
        }           
        [System.Security.SecuritySafeCritical]  // auto-generated
        public SignatureToken GetSignatureToken(byte[] sigBytes, int sigLength)
        {
            if (sigBytes == null)
                throw new ArgumentNullException("sigBytes");
            Contract.EndContractBlock();

            byte[] localSigBytes = new byte[sigBytes.Length];
            Buffer.BlockCopy(sigBytes, 0, localSigBytes, 0, sigBytes.Length);

            return new SignatureToken(TypeBuilder.GetTokenFromSig(GetNativeHandle(), localSigBytes, sigLength), this);
        }
    
        #endregion

        #region Other

#if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
#else
        [System.Security.SecuritySafeCritical]
#endif
        [System.Runtime.InteropServices.ComVisible(true)]
        public void SetCustomAttribute(ConstructorInfo con, byte[] binaryAttribute)
        {
            if (con == null)
                throw new ArgumentNullException("con");
            if (binaryAttribute == null)
                throw new ArgumentNullException("binaryAttribute");
            Contract.EndContractBlock();
            
            TypeBuilder.DefineCustomAttribute(
                this,
                1,                                          // This is hard coding the module token to 1
                this.GetConstructorToken(con).Token,
                binaryAttribute,
                false, false);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public void SetCustomAttribute(CustomAttributeBuilder customBuilder)
        {
            if (customBuilder == null)
            {
                throw new ArgumentNullException("customBuilder");
            }
            Contract.EndContractBlock();

            customBuilder.CreateCustomAttribute(this, 1);   // This is hard coding the module token to 1
        }

        // This API returns the symbol writer being used to write debug symbols for this 
        // module (if any).
        // 
        // WARNING: It is unlikely this API can be used correctly by applications in any 
        // reasonable way.  It may be called internally from within TypeBuilder.CreateType.
        // 
        // Specifically:
        // 1. The underlying symbol writer (written in unmanaged code) is not necessarily well
        // hardenned and fuzz-tested against malicious API calls.  The security of partial-trust
        // symbol writing is improved by restricting usage of the writer APIs to the well-structured
        // uses in ModuleBuilder. 
        // 2. TypeBuilder.CreateType emits all the symbols for the type.  This will effectively 
        // overwrite anything someone may have written manually about the type (specifically 
        // ISymbolWriter.OpenMethod is specced to clear anything previously written for the 
        // specified method)
        // 3. Someone could technically update the symbols for a method after CreateType is 
        // called, but the debugger (which uses these symbols) assumes that they are only 
        // updated at TypeBuilder.CreateType time.  The changes wouldn't be visible (committed 
        // to the underlying stream) until another type was baked.
        // 4. Access to the writer is supposed to be synchronized (the underlying COM API is 
        // not thread safe, and these are only thin wrappers on top of them).  Exposing this 
        // directly allows the synchronization to be violated.  We know that concurrent symbol 
        // writer access can cause AVs and other problems.  The writer APIs should not be callable
        // directly by partial-trust code, but if they could this would be a security hole.  
        // Regardless, this is a reliability bug.  
        // 
        // For these reasons, we should consider making this API internal in Arrowhead
        // (as it is in Silverlight), and consider validating that we're within a call
        // to TypeBuilder.CreateType whenever this is used.
        public ISymbolWriter GetSymWriter()
        {
            return m_iSymWriter;
        }

#if FEATURE_CORECLR
        [System.Security.SecuritySafeCritical] 
#endif
        public ISymbolDocumentWriter DefineDocument(String url, Guid language, Guid languageVendor, Guid documentType)
        {
            // url cannot be null but can be an empty string 
            if (url == null)
                throw new ArgumentNullException("url");
            Contract.EndContractBlock();

            lock(SyncRoot)
            {
                return DefineDocumentNoLock(url, language, languageVendor, documentType);
            }
        }

#if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
#endif
        private ISymbolDocumentWriter DefineDocumentNoLock(String url, Guid language, Guid languageVendor, Guid documentType)
        {
            if (m_iSymWriter == null)
            {
                // Cannot DefineDocument when it is not a debug module
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_NotADebugModule"));
            }

            return m_iSymWriter.DefineDocument(url, language, languageVendor, documentType);
        }
    
#if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
#else
        [System.Security.SecuritySafeCritical]
#endif
        public void SetUserEntryPoint(MethodInfo entryPoint)
        {
            lock(SyncRoot)
            {
                SetUserEntryPointNoLock(entryPoint);
            }
        }

        [System.Security.SecurityCritical]  // auto-generated
        private void SetUserEntryPointNoLock(MethodInfo entryPoint)
        {
            // Set the user entry point. Compiler may generate startup stub before calling user main.
            // The startup stub will be the entry point. While the user "main" will be the user entry
            // point so that debugger will not step into the compiler entry point.

            if (entryPoint == null)
            {
                throw new ArgumentNullException("entryPoint");
            }
            Contract.EndContractBlock();
        
            if (m_iSymWriter == null)
            {
                // Cannot set entry point when it is not a debug module
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_NotADebugModule"));
            }

            if (entryPoint.DeclaringType != null)
            {
                if (!entryPoint.Module.Equals(this))
                {
                    // you cannot pass in a MethodInfo that is not contained by this ModuleBuilder
                    throw new InvalidOperationException(Environment.GetResourceString("Argument_NotInTheSameModuleBuilder"));
                }
            }
            else
            {
                // unfortunately this check is missing for global function passed in as RuntimeMethodInfo. 
                // The problem is that Reflection does not 
                // allow us to get the containing module giving a global function
                MethodBuilder mb = entryPoint as MethodBuilder;
                if (mb != null && mb.GetModuleBuilder() != this)
                {
                    // you cannot pass in a MethodInfo that is not contained by this ModuleBuilder
                    throw new InvalidOperationException(Environment.GetResourceString("Argument_NotInTheSameModuleBuilder"));                    
                }                    
            }
                
            // get the metadata token value and create the SymbolStore's token value class
            SymbolToken       tkMethod = new SymbolToken(GetMethodTokenInternal(entryPoint).Token);

            // set the UserEntryPoint
            m_iSymWriter.SetUserEntryPoint(tkMethod);
        }
    
        public void SetSymCustomAttribute(String name, byte[] data)
        {
            lock(SyncRoot)
            {
                SetSymCustomAttributeNoLock(name, data);
            }
        }

        private void SetSymCustomAttributeNoLock(String name, byte[] data)
        {
            if (m_iSymWriter == null)
            {
                // Cannot SetSymCustomAttribute when it is not a debug module
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_NotADebugModule"));
            }

            // This API has never worked.  It seems like we might want to call m_iSymWriter.SetSymAttribute,
            // but we don't have a metadata token to associate the attribute with.  Instead 
            // MethodBuilder.SetSymCustomAttribute could be used to associate a symbol attribute with a specific method.
        }
    
        [Pure]
        public bool IsTransient()
        {
            return InternalModule.IsTransientInternal();
        }

        #endregion

        #endregion    

#if !FEATURE_CORECLR
        void _ModuleBuilder.GetTypeInfoCount(out uint pcTInfo)
        {
            throw new NotImplementedException();
        }

        void _ModuleBuilder.GetTypeInfo(uint iTInfo, uint lcid, IntPtr ppTInfo)
        {
            throw new NotImplementedException();
        }

        void _ModuleBuilder.GetIDsOfNames([In] ref Guid riid, IntPtr rgszNames, uint cNames, uint lcid, IntPtr rgDispId)
        {
            throw new NotImplementedException();
        }

        void _ModuleBuilder.Invoke(uint dispIdMember, [In] ref Guid riid, uint lcid, short wFlags, IntPtr pDispParams, IntPtr pVarResult, IntPtr pExcepInfo, IntPtr puArgErr)
        {
            throw new NotImplementedException();
        }
#endif
    }
}
