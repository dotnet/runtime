// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using CultureInfo = System.Globalization.CultureInfo;
using System.Diagnostics.SymbolStore;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace System.Reflection.Emit
{
    public sealed class MethodBuilder : MethodInfo
    {
        #region Private Data Members
        // Identity
        internal string m_strName; // The name of the method
        private MethodToken m_tkMethod; // The token of this method
        private ModuleBuilder m_module;
        internal TypeBuilder m_containingType;

        // IL
        private int[]? m_mdMethodFixups;              // The location of all of the token fixups. Null means no fixups.
        private byte[]? m_localSignature;             // Local signature if set explicitly via DefineBody. Null otherwise.
        internal LocalSymInfo? m_localSymInfo;        // keep track debugging local information
        internal ILGenerator? m_ilGenerator;          // Null if not used.
        private byte[]? m_ubBody;                     // The IL for the method
        private ExceptionHandler[]? m_exceptions; // Exception handlers or null if there are none.
        private const int DefaultMaxStack = 16;
        private int m_maxStack = DefaultMaxStack;

        // Flags
        internal bool m_bIsBaked;
        private bool m_bIsGlobalMethod;
        private bool m_fInitLocals; // indicating if the method stack frame will be zero initialized or not.

        // Attributes
        private MethodAttributes m_iAttributes;
        private CallingConventions m_callingConvention;
        private MethodImplAttributes m_dwMethodImplFlags;

        // Parameters
        private SignatureHelper? m_signature;
        internal Type[]? m_parameterTypes;
        private Type m_returnType;
        private Type[]? m_returnTypeRequiredCustomModifiers;
        private Type[]? m_returnTypeOptionalCustomModifiers;
        private Type[][]? m_parameterTypeRequiredCustomModifiers;
        private Type[][]? m_parameterTypeOptionalCustomModifiers;

        // Generics
        private GenericTypeParameterBuilder[]? m_inst;
        private bool m_bIsGenMethDef;
        #endregion

        #region Constructor

        internal MethodBuilder(string name, MethodAttributes attributes, CallingConventions callingConvention,
            Type? returnType, Type[]? returnTypeRequiredCustomModifiers, Type[]? returnTypeOptionalCustomModifiers,
            Type[]? parameterTypes, Type[][]? parameterTypeRequiredCustomModifiers, Type[][]? parameterTypeOptionalCustomModifiers,
            ModuleBuilder mod, TypeBuilder type, bool bIsGlobalMethod)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            if (name.Length == 0)
                throw new ArgumentException(SR.Argument_EmptyName, nameof(name));

            if (name[0] == '\0')
                throw new ArgumentException(SR.Argument_IllegalName, nameof(name));

            if (mod == null)
                throw new ArgumentNullException(nameof(mod));

            if (parameterTypes != null)
            {
                foreach (Type t in parameterTypes)
                {
                    if (t == null)
                        throw new ArgumentNullException(nameof(parameterTypes));
                }
            }

            m_strName = name;
            m_module = mod;
            m_containingType = type;
            m_returnType = returnType ?? typeof(void);

            if ((attributes & MethodAttributes.Static) == 0)
            {
                // turn on the has this calling convention
                callingConvention = callingConvention | CallingConventions.HasThis;
            }
            else if ((attributes & MethodAttributes.Virtual) != 0)
            {
                // A method can't be both static and virtual
                throw new ArgumentException(SR.Arg_NoStaticVirtual);
            }

#if !FEATURE_DEFAULT_INTERFACES
            if ((attributes & MethodAttributes.SpecialName) != MethodAttributes.SpecialName)
            {
                if ((type.Attributes & TypeAttributes.Interface) == TypeAttributes.Interface)
                {
                    // methods on interface have to be abstract + virtual except special name methods such as type initializer
                    if ((attributes & (MethodAttributes.Abstract | MethodAttributes.Virtual)) !=
                        (MethodAttributes.Abstract | MethodAttributes.Virtual) &&
                        (attributes & MethodAttributes.Static) == 0)
                        throw new ArgumentException(SR.Argument_BadAttributeOnInterfaceMethod);
                }
            }
#endif

            m_callingConvention = callingConvention;

            if (parameterTypes != null)
            {
                m_parameterTypes = new Type[parameterTypes.Length];
                Array.Copy(parameterTypes, 0, m_parameterTypes, 0, parameterTypes.Length);
            }
            else
            {
                m_parameterTypes = null;
            }

            m_returnTypeRequiredCustomModifiers = returnTypeRequiredCustomModifiers;
            m_returnTypeOptionalCustomModifiers = returnTypeOptionalCustomModifiers;
            m_parameterTypeRequiredCustomModifiers = parameterTypeRequiredCustomModifiers;
            m_parameterTypeOptionalCustomModifiers = parameterTypeOptionalCustomModifiers;

            //            m_signature = SignatureHelper.GetMethodSigHelper(mod, callingConvention, 
            //                returnType, returnTypeRequiredCustomModifiers, returnTypeOptionalCustomModifiers,
            //                parameterTypes, parameterTypeRequiredCustomModifiers, parameterTypeOptionalCustomModifiers);

            m_iAttributes = attributes;
            m_bIsGlobalMethod = bIsGlobalMethod;
            m_bIsBaked = false;
            m_fInitLocals = true;

            m_localSymInfo = new LocalSymInfo();
            m_ubBody = null;
            m_ilGenerator = null;

            // Default is managed IL. Manged IL has bit flag 0x0020 set off
            m_dwMethodImplFlags = MethodImplAttributes.IL;
        }

        #endregion

        #region Internal Members

        internal void CheckContext(params Type[]?[]? typess)
        {
            m_module.CheckContext(typess);
        }

        internal void CheckContext(params Type?[]? types)
        {
            m_module.CheckContext(types);
        }

        internal void CreateMethodBodyHelper(ILGenerator il)
        {
            // Sets the IL of the method.  An ILGenerator is passed as an argument and the method
            // queries this instance to get all of the information which it needs.
            if (il == null)
            {
                throw new ArgumentNullException(nameof(il));
            }

            __ExceptionInfo[] excp;
            int counter = 0;
            int[] filterAddrs;
            int[] catchAddrs;
            int[] catchEndAddrs;
            Type[] catchClass;
            int[] type;
            int numCatch;
            int start, end;
            ModuleBuilder dynMod = (ModuleBuilder)m_module;

            m_containingType.ThrowIfCreated();

            if (m_bIsBaked)
            {
                throw new InvalidOperationException(SR.InvalidOperation_MethodHasBody);
            }

            if (il.m_methodBuilder != this && il.m_methodBuilder != null)
            {
                // you don't need to call DefineBody when you get your ILGenerator
                // through MethodBuilder::GetILGenerator.
                //

                throw new InvalidOperationException(SR.InvalidOperation_BadILGeneratorUsage);
            }

            ThrowIfShouldNotHaveBody();

            if (il.m_ScopeTree.m_iOpenScopeCount != 0)
            {
                // There are still unclosed local scope
                throw new InvalidOperationException(SR.InvalidOperation_OpenLocalVariableScope);
            }


            m_ubBody = il.BakeByteArray();

            m_mdMethodFixups = il.GetTokenFixups();

            //Okay, now the fun part.  Calculate all of the exceptions.
            excp = il.GetExceptions()!;
            int numExceptions = CalculateNumberOfExceptions(excp);
            if (numExceptions > 0)
            {
                m_exceptions = new ExceptionHandler[numExceptions];

                for (int i = 0; i < excp.Length; i++)
                {
                    filterAddrs = excp[i].GetFilterAddresses();
                    catchAddrs = excp[i].GetCatchAddresses();
                    catchEndAddrs = excp[i].GetCatchEndAddresses();
                    catchClass = excp[i].GetCatchClass();

                    numCatch = excp[i].GetNumberOfCatches();
                    start = excp[i].GetStartAddress();
                    end = excp[i].GetEndAddress();
                    type = excp[i].GetExceptionTypes();
                    for (int j = 0; j < numCatch; j++)
                    {
                        int tkExceptionClass = 0;
                        if (catchClass[j] != null)
                        {
                            tkExceptionClass = dynMod.GetTypeTokenInternal(catchClass[j]).Token;
                        }

                        switch (type[j])
                        {
                            case __ExceptionInfo.None:
                            case __ExceptionInfo.Fault:
                            case __ExceptionInfo.Filter:
                                m_exceptions[counter++] = new ExceptionHandler(start, end, filterAddrs[j], catchAddrs[j], catchEndAddrs[j], type[j], tkExceptionClass);
                                break;

                            case __ExceptionInfo.Finally:
                                m_exceptions[counter++] = new ExceptionHandler(start, excp[i].GetFinallyEndAddress(), filterAddrs[j], catchAddrs[j], catchEndAddrs[j], type[j], tkExceptionClass);
                                break;
                        }
                    }
                }
            }


            m_bIsBaked = true;

            if (dynMod.GetSymWriter() != null)
            {
                // set the debugging information such as scope and line number
                // if it is in a debug module
                //
                SymbolToken tk = new SymbolToken(MetadataTokenInternal);
                ISymbolWriter symWriter = dynMod.GetSymWriter()!;

                // call OpenMethod to make this method the current method
                symWriter.OpenMethod(tk);

                // call OpenScope because OpenMethod no longer implicitly creating
                // the top-levelsmethod scope
                //
                symWriter.OpenScope(0);

                if (m_symCustomAttrs != null)
                {
                    foreach (SymCustomAttr symCustomAttr in m_symCustomAttrs)
                        dynMod.GetSymWriter()!.SetSymAttribute(
                        new SymbolToken(MetadataTokenInternal),
                            symCustomAttr.m_name,
                            symCustomAttr.m_data);
                }

                if (m_localSymInfo != null)
                    m_localSymInfo.EmitLocalSymInfo(symWriter);
                il.m_ScopeTree.EmitScopeTree(symWriter);
                il.m_LineNumberInfo.EmitLineNumberInfo(symWriter);
                symWriter.CloseScope(il.ILOffset);
                symWriter.CloseMethod();
            }
        }

        // This is only called from TypeBuilder.CreateType after the method has been created
        internal void ReleaseBakedStructures()
        {
            if (!m_bIsBaked)
            {
                // We don't need to do anything here if we didn't baked the method body
                return;
            }

            m_ubBody = null;
            m_localSymInfo = null;
            m_mdMethodFixups = null;
            m_localSignature = null;
            m_exceptions = null;
        }

        internal override Type[] GetParameterTypes()
        {
            if (m_parameterTypes == null)
                m_parameterTypes = Array.Empty<Type>();

            return m_parameterTypes;
        }

        internal static Type? GetMethodBaseReturnType(MethodBase? method)
        {
            if (method is MethodInfo mi)
            {
                return mi.ReturnType;
            }
            else if (method is ConstructorInfo ci)
            {
                return ci.GetReturnType();
            }
            else
            {
                Debug.Fail("We should never get here!");
                return null;
            }
        }

        internal void SetToken(MethodToken token)
        {
            m_tkMethod = token;
        }

        internal byte[]? GetBody()
        {
            // Returns the il bytes of this method.
            // This il is not valid until somebody has called BakeByteArray
            return m_ubBody;
        }

        internal int[]? GetTokenFixups()
        {
            return m_mdMethodFixups;
        }

        internal SignatureHelper GetMethodSignature()
        {
            if (m_parameterTypes == null)
                m_parameterTypes = Array.Empty<Type>();

            m_signature = SignatureHelper.GetMethodSigHelper(m_module, m_callingConvention, m_inst != null ? m_inst.Length : 0,
                m_returnType, m_returnTypeRequiredCustomModifiers, m_returnTypeOptionalCustomModifiers,
                m_parameterTypes, m_parameterTypeRequiredCustomModifiers, m_parameterTypeOptionalCustomModifiers);

            return m_signature;
        }

        // Returns a buffer whose initial signatureLength bytes contain encoded local signature.
        internal byte[] GetLocalSignature(out int signatureLength)
        {
            if (m_localSignature != null)
            {
                signatureLength = m_localSignature.Length;
                return m_localSignature;
            }

            if (m_ilGenerator != null)
            {
                if (m_ilGenerator.m_localCount != 0)
                {
                    // If user is using ILGenerator::DeclareLocal, then get local signaturefrom there.
                    return m_ilGenerator.m_localSignature.InternalGetSignature(out signatureLength);
                }
            }

            return SignatureHelper.GetLocalVarSigHelper(m_module).InternalGetSignature(out signatureLength);
        }

        internal int GetMaxStack()
        {
            if (m_ilGenerator != null)
            {
                return m_ilGenerator.GetMaxStackSize() + ExceptionHandlerCount;
            }
            else
            {
                // this is the case when client provide an array of IL byte stream rather than going through ILGenerator.
                return m_maxStack;
            }
        }

        internal ExceptionHandler[]? GetExceptionHandlers()
        {
            return m_exceptions;
        }

        internal int ExceptionHandlerCount
        {
            get { return m_exceptions != null ? m_exceptions.Length : 0; }
        }

        internal int CalculateNumberOfExceptions(__ExceptionInfo[]? excp)
        {
            int num = 0;

            if (excp == null)
            {
                return 0;
            }

            for (int i = 0; i < excp.Length; i++)
            {
                num += excp[i].GetNumberOfCatches();
            }

            return num;
        }

        internal bool IsTypeCreated()
        {
            return (m_containingType != null && m_containingType.IsCreated());
        }

        internal TypeBuilder GetTypeBuilder()
        {
            return m_containingType;
        }

        internal ModuleBuilder GetModuleBuilder()
        {
            return m_module;
        }
        #endregion

        #region Object Overrides
        public override bool Equals(object? obj)
        {
            if (!(obj is MethodBuilder))
            {
                return false;
            }
            if (!(this.m_strName.Equals(((MethodBuilder)obj).m_strName)))
            {
                return false;
            }

            if (m_iAttributes != (((MethodBuilder)obj).m_iAttributes))
            {
                return false;
            }

            SignatureHelper thatSig = ((MethodBuilder)obj).GetMethodSignature();
            if (thatSig.Equals(GetMethodSignature()))
            {
                return true;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return this.m_strName.GetHashCode();
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder(1000);
            sb.Append("Name: ").Append(m_strName).AppendLine(" ");
            sb.Append("Attributes: ").Append((int)m_iAttributes).AppendLine();
            sb.Append("Method Signature: ").Append(GetMethodSignature()).AppendLine();
            sb.Append(Environment.NewLine);
            return sb.ToString();
        }

        #endregion

        #region MemberInfo Overrides
        public override string Name
        {
            get
            {
                return m_strName;
            }
        }

        internal int MetadataTokenInternal
        {
            get
            {
                return GetToken().Token;
            }
        }

        public override Module Module
        {
            get
            {
                return m_containingType.Module;
            }
        }

        public override Type? DeclaringType
        {
            get
            {
                if (m_containingType.m_isHiddenGlobalType == true)
                    return null;
                return m_containingType;
            }
        }

#pragma warning disable CS8609 // TODO-NULLABLE: Covariant return types (https://github.com/dotnet/roslyn/issues/23268)
        public override ICustomAttributeProvider? ReturnTypeCustomAttributes
        {
            get
            {
                return null;
            }
        }
#pragma warning restore CS8609

        public override Type? ReflectedType
        {
            get
            {
                return DeclaringType;
            }
        }

        #endregion

        #region MethodBase Overrides
        public override object Invoke(object? obj, BindingFlags invokeAttr, Binder? binder, object?[]? parameters, CultureInfo? culture)
        {
            throw new NotSupportedException(SR.NotSupported_DynamicModule);
        }

        public override MethodImplAttributes GetMethodImplementationFlags()
        {
            return m_dwMethodImplFlags;
        }

        public override MethodAttributes Attributes
        {
            get { return m_iAttributes; }
        }

        public override CallingConventions CallingConvention
        {
            get { return m_callingConvention; }
        }

        public override RuntimeMethodHandle MethodHandle
        {
            get { throw new NotSupportedException(SR.NotSupported_DynamicModule); }
        }

        public override bool IsSecurityCritical
        {
            get { return true; }
        }

        public override bool IsSecuritySafeCritical
        {
            get { return false; }
        }

        public override bool IsSecurityTransparent
        {
            get { return false; }
        }
        #endregion

        #region MethodInfo Overrides
        public override MethodInfo GetBaseDefinition()
        {
            return this;
        }

        public override Type ReturnType
        {
            get
            {
                return m_returnType;
            }
        }

        public override ParameterInfo[] GetParameters()
        {
            if (!m_bIsBaked || m_containingType == null || m_containingType.BakedRuntimeType == null)
                throw new NotSupportedException(SR.InvalidOperation_TypeNotCreated);

            MethodInfo rmi = m_containingType.GetMethod(m_strName, m_parameterTypes!)!;

            return rmi.GetParameters();
        }

        public override ParameterInfo ReturnParameter
        {
            get
            {
                if (!m_bIsBaked || m_containingType == null || m_containingType.BakedRuntimeType == null)
                    throw new InvalidOperationException(SR.InvalidOperation_TypeNotCreated);

                MethodInfo rmi = m_containingType.GetMethod(m_strName, m_parameterTypes!)!;

                return rmi.ReturnParameter;
            }
        }
        #endregion

        #region ICustomAttributeProvider Implementation
        public override object[] GetCustomAttributes(bool inherit)
        {
            throw new NotSupportedException(SR.NotSupported_DynamicModule);
        }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            throw new NotSupportedException(SR.NotSupported_DynamicModule);
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            throw new NotSupportedException(SR.NotSupported_DynamicModule);
        }

        #endregion

        #region Generic Members
        public override bool IsGenericMethodDefinition { get { return m_bIsGenMethDef; } }

        public override bool ContainsGenericParameters { get { throw new NotSupportedException(); } }

        public override MethodInfo GetGenericMethodDefinition() { if (!IsGenericMethod) throw new InvalidOperationException(); return this; }

        public override bool IsGenericMethod { get { return m_inst != null; } }

        public override Type[] GetGenericArguments() => m_inst ?? Array.Empty<Type>();

        public override MethodInfo MakeGenericMethod(params Type[] typeArguments)
        {
            return MethodBuilderInstantiation.MakeGenericMethod(this, typeArguments);
        }


        public GenericTypeParameterBuilder[] DefineGenericParameters(params string[] names)
        {
            if (names == null)
                throw new ArgumentNullException(nameof(names));

            if (names.Length == 0)
                throw new ArgumentException(SR.Arg_EmptyArray, nameof(names));

            if (m_inst != null)
                throw new InvalidOperationException(SR.InvalidOperation_GenericParametersAlreadySet);

            for (int i = 0; i < names.Length; i++)
                if (names[i] == null)
                    throw new ArgumentNullException(nameof(names));

            if (m_tkMethod.Token != 0)
                throw new InvalidOperationException(SR.InvalidOperation_MethodBuilderBaked);

            m_bIsGenMethDef = true;
            m_inst = new GenericTypeParameterBuilder[names.Length];
            for (int i = 0; i < names.Length; i++)
                m_inst[i] = new GenericTypeParameterBuilder(new TypeBuilder(names[i], i, this));

            return m_inst;
        }

        internal void ThrowIfGeneric() { if (IsGenericMethod && !IsGenericMethodDefinition) throw new InvalidOperationException(); }
        #endregion

        #region Public Members
        public MethodToken GetToken()
        {
            // We used to always "tokenize" a MethodBuilder when it is constructed. After change list 709498
            // we only "tokenize" a method when requested. But the order in which the methods are tokenized
            // didn't change: the same order the MethodBuilders are constructed. The recursion introduced
            // will overflow the stack when there are many methods on the same type (10000 in my experiment).
            // The change also introduced race conditions. Before the code change GetToken is called from
            // the MethodBuilder .ctor which is protected by lock(ModuleBuilder.SyncRoot). Now it
            // could be called more than once on the the same method introducing duplicate (invalid) tokens.
            // I don't fully understand this change. So I will keep the logic and only fix the recursion and 
            // the race condition.

            if (m_tkMethod.Token != 0)
            {
                return m_tkMethod;
            }

            MethodBuilder? currentMethod = null;
            MethodToken currentToken = new MethodToken(0);
            int i;

            // We need to lock here to prevent a method from being "tokenized" twice.
            // We don't need to synchronize this with Type.DefineMethod because it only appends newly
            // constructed MethodBuilders to the end of m_listMethods
            lock (m_containingType.m_listMethods)
            {
                if (m_tkMethod.Token != 0)
                {
                    return m_tkMethod;
                }

                // If m_tkMethod is still 0 when we obtain the lock, m_lastTokenizedMethod must be smaller
                // than the index of the current method.
                for (i = m_containingType.m_lastTokenizedMethod + 1; i < m_containingType.m_listMethods.Count; ++i)
                {
                    currentMethod = m_containingType.m_listMethods[i];
                    currentToken = currentMethod.GetTokenNoLock();

                    if (currentMethod == this)
                        break;
                }

                m_containingType.m_lastTokenizedMethod = i;
            }

            Debug.Assert(currentMethod == this, "We should have found this method in m_containingType.m_listMethods");
            Debug.Assert(currentToken.Token != 0, "The token should not be 0");

            return currentToken;
        }

        private MethodToken GetTokenNoLock()
        {
            Debug.Assert(m_tkMethod.Token == 0, "m_tkMethod should not have been initialized");

            int sigLength;
            byte[] sigBytes = GetMethodSignature().InternalGetSignature(out sigLength);
            ModuleBuilder module = m_module;

            int token = TypeBuilder.DefineMethod(JitHelpers.GetQCallModuleOnStack(ref module), m_containingType.MetadataTokenInternal, m_strName, sigBytes, sigLength, Attributes);
            m_tkMethod = new MethodToken(token);

            if (m_inst != null)
                foreach (GenericTypeParameterBuilder tb in m_inst)
                    if (!tb.m_type.IsCreated()) tb.m_type.CreateType();

            TypeBuilder.SetMethodImpl(JitHelpers.GetQCallModuleOnStack(ref module), token, m_dwMethodImplFlags);

            return m_tkMethod;
        }

        public void SetParameters(params Type[]? parameterTypes)
        {
            CheckContext(parameterTypes);

            SetSignature(null, null, null, parameterTypes, null, null);
        }

        public void SetReturnType(Type? returnType)
        {
            CheckContext(returnType);

            SetSignature(returnType, null, null, null, null, null);
        }

        public void SetSignature(
            Type? returnType, Type[]? returnTypeRequiredCustomModifiers, Type[]? returnTypeOptionalCustomModifiers,
            Type[]? parameterTypes, Type[][]? parameterTypeRequiredCustomModifiers, Type[][]? parameterTypeOptionalCustomModifiers)
        {
            // We should throw InvalidOperation_MethodBuilderBaked here if the method signature has been baked.
            // But we cannot because that would be a breaking change from V2.
            if (m_tkMethod.Token != 0)
                return;

            CheckContext(returnType);
            CheckContext(returnTypeRequiredCustomModifiers, returnTypeOptionalCustomModifiers, parameterTypes);
            CheckContext(parameterTypeRequiredCustomModifiers);
            CheckContext(parameterTypeOptionalCustomModifiers);

            ThrowIfGeneric();

            if (returnType != null)
            {
                m_returnType = returnType;
            }

            if (parameterTypes != null)
            {
                m_parameterTypes = new Type[parameterTypes.Length];
                Array.Copy(parameterTypes, 0, m_parameterTypes, 0, parameterTypes.Length);
            }

            m_returnTypeRequiredCustomModifiers = returnTypeRequiredCustomModifiers;
            m_returnTypeOptionalCustomModifiers = returnTypeOptionalCustomModifiers;
            m_parameterTypeRequiredCustomModifiers = parameterTypeRequiredCustomModifiers;
            m_parameterTypeOptionalCustomModifiers = parameterTypeOptionalCustomModifiers;
        }


        public ParameterBuilder DefineParameter(int position, ParameterAttributes attributes, string? strParamName)
        {
            if (position < 0)
                throw new ArgumentOutOfRangeException(SR.ArgumentOutOfRange_ParamSequence);

            ThrowIfGeneric();
            m_containingType.ThrowIfCreated();

            if (position > 0 && (m_parameterTypes == null || position > m_parameterTypes.Length))
                throw new ArgumentOutOfRangeException(SR.ArgumentOutOfRange_ParamSequence);

            attributes = attributes & ~ParameterAttributes.ReservedMask;
            return new ParameterBuilder(this, position, attributes, strParamName);
        }

        private List<SymCustomAttr>? m_symCustomAttrs;
        private struct SymCustomAttr
        {
            public string m_name;
            public byte[] m_data;
        }

        public void SetImplementationFlags(MethodImplAttributes attributes)
        {
            ThrowIfGeneric();

            m_containingType.ThrowIfCreated();

            m_dwMethodImplFlags = attributes;

            m_canBeRuntimeImpl = true;

            ModuleBuilder module = m_module;
            TypeBuilder.SetMethodImpl(JitHelpers.GetQCallModuleOnStack(ref module), MetadataTokenInternal, attributes);
        }

        public ILGenerator GetILGenerator()
        {
            ThrowIfGeneric();
            ThrowIfShouldNotHaveBody();

            if (m_ilGenerator == null)
                m_ilGenerator = new ILGenerator(this);
            return m_ilGenerator;
        }

        public ILGenerator GetILGenerator(int size)
        {
            ThrowIfGeneric();
            ThrowIfShouldNotHaveBody();

            if (m_ilGenerator == null)
                m_ilGenerator = new ILGenerator(this, size);
            return m_ilGenerator;
        }

        private void ThrowIfShouldNotHaveBody()
        {
            if ((m_dwMethodImplFlags & MethodImplAttributes.CodeTypeMask) != MethodImplAttributes.IL ||
                (m_dwMethodImplFlags & MethodImplAttributes.Unmanaged) != 0 ||
                (m_iAttributes & MethodAttributes.PinvokeImpl) != 0 ||
                m_isDllImport)
            {
                // cannot attach method body if methodimpl is marked not marked as managed IL
                //
                throw new InvalidOperationException(SR.InvalidOperation_ShouldNotHaveMethodBody);
            }
        }


        public bool InitLocals
        {
            // Property is set to true if user wishes to have zero initialized stack frame for this method. Default to false.
            get { ThrowIfGeneric(); return m_fInitLocals; }
            set { ThrowIfGeneric(); m_fInitLocals = value; }
        }

        public Module GetModule()
        {
            return GetModuleBuilder();
        }

        public string Signature
        {
            get
            {
                return GetMethodSignature().ToString();
            }
        }


        public void SetCustomAttribute(ConstructorInfo con, byte[] binaryAttribute)
        {
            if (con is null)
                throw new ArgumentNullException(nameof(con));
            if (binaryAttribute is null)
                throw new ArgumentNullException(nameof(binaryAttribute));

            ThrowIfGeneric();

            TypeBuilder.DefineCustomAttribute(m_module, MetadataTokenInternal,
                ((ModuleBuilder)m_module).GetConstructorToken(con).Token,
                binaryAttribute,
                false, false);

            if (IsKnownCA(con))
                ParseCA(con, binaryAttribute);
        }

        public void SetCustomAttribute(CustomAttributeBuilder customBuilder)
        {
            if (customBuilder == null)
                throw new ArgumentNullException(nameof(customBuilder));

            ThrowIfGeneric();

            customBuilder.CreateCustomAttribute((ModuleBuilder)m_module, MetadataTokenInternal);

            if (IsKnownCA(customBuilder.m_con))
                ParseCA(customBuilder.m_con, customBuilder.m_blob);
        }

        // this method should return true for any and every ca that requires more work
        // than just setting the ca
        private bool IsKnownCA(ConstructorInfo con)
        {
            Type? caType = con.DeclaringType;
            if (caType == typeof(System.Runtime.CompilerServices.MethodImplAttribute)) return true;
            else if (caType == typeof(DllImportAttribute)) return true;
            else return false;
        }

        private void ParseCA(ConstructorInfo con, byte[]? blob)
        {
            Type? caType = con.DeclaringType;
            if (caType == typeof(System.Runtime.CompilerServices.MethodImplAttribute))
            {
                // dig through the blob looking for the MethodImplAttributes flag
                // that must be in the MethodCodeType field

                // for now we simply set a flag that relaxes the check when saving and
                // allows this method to have no body when any kind of MethodImplAttribute is present
                m_canBeRuntimeImpl = true;
            }
            else if (caType == typeof(DllImportAttribute))
            {
                m_canBeRuntimeImpl = true;
                m_isDllImport = true;
            }
        }

        internal bool m_canBeRuntimeImpl = false;
        internal bool m_isDllImport = false;

        #endregion
    }

    internal class LocalSymInfo
    {
        // This class tracks the local variable's debugging information 
        // and namespace information with a given active lexical scope.

        #region Internal Data Members
        internal string[] m_strName = null!;  //All these arrys initialized in helper method
        internal byte[][] m_ubSignature = null!;
        internal int[] m_iLocalSlot = null!;
        internal int[] m_iStartOffset = null!;
        internal int[] m_iEndOffset = null!;
        internal int m_iLocalSymCount;         // how many entries in the arrays are occupied
        internal string[] m_namespace = null!;
        internal int m_iNameSpaceCount;
        internal const int InitialSize = 16;
        #endregion

        #region Constructor
        internal LocalSymInfo()
        {
            // initialize data variables
            m_iLocalSymCount = 0;
            m_iNameSpaceCount = 0;
        }
        #endregion

        #region Private Members
        private void EnsureCapacityNamespace()
        {
            if (m_iNameSpaceCount == 0)
            {
                m_namespace = new string[InitialSize];
            }
            else if (m_iNameSpaceCount == m_namespace.Length)
            {
                string[] strTemp = new string[checked(m_iNameSpaceCount * 2)];
                Array.Copy(m_namespace, 0, strTemp, 0, m_iNameSpaceCount);
                m_namespace = strTemp;
            }
        }

        private void EnsureCapacity()
        {
            if (m_iLocalSymCount == 0)
            {
                // First time. Allocate the arrays.
                m_strName = new string[InitialSize];
                m_ubSignature = new byte[InitialSize][];
                m_iLocalSlot = new int[InitialSize];
                m_iStartOffset = new int[InitialSize];
                m_iEndOffset = new int[InitialSize];
            }
            else if (m_iLocalSymCount == m_strName.Length)
            {
                // the arrays are full. Enlarge the arrays
                // why aren't we just using lists here?
                int newSize = checked(m_iLocalSymCount * 2);
                int[] temp = new int[newSize];
                Array.Copy(m_iLocalSlot, 0, temp, 0, m_iLocalSymCount);
                m_iLocalSlot = temp;

                temp = new int[newSize];
                Array.Copy(m_iStartOffset, 0, temp, 0, m_iLocalSymCount);
                m_iStartOffset = temp;

                temp = new int[newSize];
                Array.Copy(m_iEndOffset, 0, temp, 0, m_iLocalSymCount);
                m_iEndOffset = temp;

                string[] strTemp = new string[newSize];
                Array.Copy(m_strName, 0, strTemp, 0, m_iLocalSymCount);
                m_strName = strTemp;

                byte[][] ubTemp = new byte[newSize][];
                Array.Copy(m_ubSignature, 0, ubTemp, 0, m_iLocalSymCount);
                m_ubSignature = ubTemp;
            }
        }

        #endregion

        #region Internal Members
        internal void AddLocalSymInfo(string strName, byte[] signature, int slot, int startOffset, int endOffset)
        {
            // make sure that arrays are large enough to hold addition info
            EnsureCapacity();
            m_iStartOffset[m_iLocalSymCount] = startOffset;
            m_iEndOffset[m_iLocalSymCount] = endOffset;
            m_iLocalSlot[m_iLocalSymCount] = slot;
            m_strName[m_iLocalSymCount] = strName;
            m_ubSignature[m_iLocalSymCount] = signature;
            checked { m_iLocalSymCount++; }
        }

        internal void AddUsingNamespace(string strNamespace)
        {
            EnsureCapacityNamespace();
            m_namespace[m_iNameSpaceCount] = strNamespace;
            checked { m_iNameSpaceCount++; }
        }

        internal virtual void EmitLocalSymInfo(ISymbolWriter symWriter)
        {
            int i;

            for (i = 0; i < m_iLocalSymCount; i++)
            {
                symWriter.DefineLocalVariable(
                            m_strName[i],
                            FieldAttributes.PrivateScope,
                            m_ubSignature[i],
                            SymAddressKind.ILOffset,
                            m_iLocalSlot[i],
                            0,          // addr2 is not used yet
                            0,          // addr3 is not used
                            m_iStartOffset[i],
                            m_iEndOffset[i]);
            }
            for (i = 0; i < m_iNameSpaceCount; i++)
            {
                symWriter.UsingNamespace(m_namespace[i]);
            }
        }

        #endregion
    }

    /// <summary>
    /// Describes exception handler in a method body.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct ExceptionHandler : IEquatable<ExceptionHandler>
    {
        // Keep in sync with unmanged structure. 
        internal readonly int m_exceptionClass;
        internal readonly int m_tryStartOffset;
        internal readonly int m_tryEndOffset;
        internal readonly int m_filterOffset;
        internal readonly int m_handlerStartOffset;
        internal readonly int m_handlerEndOffset;
        internal readonly ExceptionHandlingClauseOptions m_kind;

        #region Constructors

        internal ExceptionHandler(int tryStartOffset, int tryEndOffset, int filterOffset, int handlerStartOffset, int handlerEndOffset,
            int kind, int exceptionTypeToken)
        {
            Debug.Assert(tryStartOffset >= 0);
            Debug.Assert(tryEndOffset >= 0);
            Debug.Assert(filterOffset >= 0);
            Debug.Assert(handlerStartOffset >= 0);
            Debug.Assert(handlerEndOffset >= 0);
            Debug.Assert(IsValidKind((ExceptionHandlingClauseOptions)kind));
            Debug.Assert(kind != (int)ExceptionHandlingClauseOptions.Clause || (exceptionTypeToken & 0x00FFFFFF) != 0);

            m_tryStartOffset = tryStartOffset;
            m_tryEndOffset = tryEndOffset;
            m_filterOffset = filterOffset;
            m_handlerStartOffset = handlerStartOffset;
            m_handlerEndOffset = handlerEndOffset;
            m_kind = (ExceptionHandlingClauseOptions)kind;
            m_exceptionClass = exceptionTypeToken;
        }

        private static bool IsValidKind(ExceptionHandlingClauseOptions kind)
        {
            switch (kind)
            {
                case ExceptionHandlingClauseOptions.Clause:
                case ExceptionHandlingClauseOptions.Filter:
                case ExceptionHandlingClauseOptions.Finally:
                case ExceptionHandlingClauseOptions.Fault:
                    return true;

                default:
                    return false;
            }
        }

        #endregion

        #region Equality

        public override int GetHashCode()
        {
            return m_exceptionClass ^ m_tryStartOffset ^ m_tryEndOffset ^ m_filterOffset ^ m_handlerStartOffset ^ m_handlerEndOffset ^ (int)m_kind;
        }

        public override bool Equals(object? obj)
        {
            return obj is ExceptionHandler && Equals((ExceptionHandler)obj);
        }

        public bool Equals(ExceptionHandler other)
        {
            return
                other.m_exceptionClass == m_exceptionClass &&
                other.m_tryStartOffset == m_tryStartOffset &&
                other.m_tryEndOffset == m_tryEndOffset &&
                other.m_filterOffset == m_filterOffset &&
                other.m_handlerStartOffset == m_handlerStartOffset &&
                other.m_handlerEndOffset == m_handlerEndOffset &&
                other.m_kind == m_kind;
        }

        public static bool operator ==(ExceptionHandler left, ExceptionHandler right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ExceptionHandler left, ExceptionHandler right)
        {
            return !left.Equals(right);
        }

        #endregion
    }
}










